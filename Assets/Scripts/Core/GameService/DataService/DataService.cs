// LevityFramework - 通用 Unity 游戏框架
// 核心服务模块 - DataService 数据存档服务

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 数据服务：管理游戏存档、加载、保存。
/// 采用 Provider 钩子机制，允许子系统（如 NaninovelService）注册自己的存档/加载逻辑。
/// 对外暴露多槽位接口 SaveToSlot / LoadFromSlot / DeleteSlot。
/// </summary>
public class DataService : ILogic
{
    // ── 存档路径 ─────────────────────────────────────────────────────────────
    private string savePath;

    // ── Provider 钩子 ─────────────────────────────────────────────────────────
    private readonly Dictionary<string, Action<GameSaveData>> saveProviders
        = new Dictionary<string, Action<GameSaveData>>();

    private readonly Dictionary<string, Func<GameSaveData, Task>> loadProviders
        = new Dictionary<string, Func<GameSaveData, Task>>();

    private readonly Dictionary<string, Action<int>> deleteProviders
        = new Dictionary<string, Action<int>>();

    // ── 当前内存数据 ──────────────────────────────────────────────────────────
    public GameData gameData { get; private set; }

    // ── ILogic ────────────────────────────────────────────────────────────────
    public void OnInit()
    {
        savePath = Path.Combine(Application.persistentDataPath, "saves");
        if (!Directory.Exists(savePath))
            Directory.CreateDirectory(savePath);

        gameData = new GameData();
    }

    public void OnEnterState() { }
    public void OnUpdate() { }
    public void UnInit() { }

    // ── Provider 注册接口 ─────────────────────────────────────────────────────

    /// <summary>
    /// 注册存档 Provider。每个 key 唯一；重复注册会覆盖旧 Provider。
    /// 在 DataService.SaveToSlot 时，<paramref name="onSave"/> 会被调用，
    /// 可将自定义数据写入 <see cref="GameSaveData"/>，也可以触发外部存档（如 Naninovel SaveGame）。
    /// </summary>
    public void AddSaveProvider(string key, Action<GameSaveData> onSave)
    {
        saveProviders[key] = onSave;
    }

    /// <summary>
    /// 注册加载 Provider。每个 key 唯一；重复注册会覆盖旧 Provider。
    /// 在 DataService.LoadFromSlot 时，<paramref name="onLoad"/> 会被 await，
    /// 可从 <see cref="GameSaveData"/> 恢复数据，也可触发外部加载（如 Naninovel LoadGame）。
    /// </summary>
    public void AddLoadProvider(string key, Func<GameSaveData, Task> onLoad)
    {
        loadProviders[key] = onLoad;
    }

    /// <summary>
    /// 注册删除 Provider。在 <see cref="DeleteSlot"/> 时一并删除子系统的存档文件。
    /// </summary>
    public void AddDeleteProvider(string key, Action<int> onDelete)
    {
        deleteProviders[key] = onDelete;
    }

    /// <summary>
    /// 移除 Provider（三类同时移除）。
    /// </summary>
    public void RemoveProvider(string key)
    {
        saveProviders.Remove(key);
        loadProviders.Remove(key);
        deleteProviders.Remove(key);
    }

    // ── 多槽位存档接口 ────────────────────────────────────────────────────────

    /// <summary>
    /// 保存到指定槽位。
    /// 1. 调用所有注册的 save providers（可附加外部存档逻辑）。
    /// 2. 将 gameData（JSON）写入 slot_{slot}.json。
    /// </summary>
    public async Task SaveToSlot(int slot)
    {
        var saveData = new GameSaveData { slotId = slot, gameData = gameData };

        // 通知各子系统写入自己的数据
        foreach (var kv in saveProviders)
        {
            try { kv.Value.Invoke(saveData); }
            catch (Exception ex) { Debug.LogError($"[DataService] SaveProvider '{kv.Key}' threw: {ex}"); }
        }

        // 持久化主存档 JSON
        try
        {
            string filePath = GetSlotPath(slot);
            string json = JsonUtility.ToJson(saveData, true);
            await File.WriteAllTextAsync(filePath, json);
            Debug.Log($"[DataService] Saved slot {slot} → {filePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DataService] Failed to write slot {slot}: {ex}");
        }
    }

    /// <summary>
    /// 从指定槽位加载。
    /// 1. 读取 slot_{slot}.json 并反序列化。
    /// 2. 依次 await 所有注册的 load providers。
    /// </summary>
    public async Task<bool> LoadFromSlot(int slot)
    {
        string filePath = GetSlotPath(slot);
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"[DataService] Slot {slot} not found: {filePath}");
            return false;
        }

        GameSaveData saveData;
        try
        {
            string json = await File.ReadAllTextAsync(filePath);
            saveData = JsonUtility.FromJson<GameSaveData>(json);
            gameData = saveData.gameData ?? new GameData();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DataService] Failed to read slot {slot}: {ex}");
            return false;
        }

        // 通知各子系统恢复状态
        foreach (var kv in loadProviders)
        {
            try { await kv.Value.Invoke(saveData); }
            catch (Exception ex) { Debug.LogError($"[DataService] LoadProvider '{kv.Key}' threw: {ex}"); }
        }

        Debug.Log($"[DataService] Loaded slot {slot}");
        return true;
    }

    /// <summary>
    /// 删除指定槽位的所有存档文件（主文件 + 各 Provider 的外部文件）。
    /// </summary>
    public void DeleteSlot(int slot)
    {
        // 通知各子系统删除自己的文件
        foreach (var kv in deleteProviders)
        {
            try { kv.Value.Invoke(slot); }
            catch (Exception ex) { Debug.LogError($"[DataService] DeleteProvider '{kv.Key}' threw: {ex}"); }
        }

        // 删除主存档文件
        try
        {
            string filePath = GetSlotPath(slot);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log($"[DataService] Deleted slot {slot}: {filePath}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DataService] Failed to delete slot {slot}: {ex}");
        }
    }

    /// <summary>
    /// 检查指定槽位是否存在主存档文件。
    /// </summary>
    public bool SlotExists(int slot) => File.Exists(GetSlotPath(slot));

    /// <summary>
    /// 获取所有已存在的槽位编号。
    /// </summary>
    public List<int> GetExistingSlots()
    {
        var result = new List<int>();
        var files = Directory.GetFiles(savePath, "slot_*.json");
        foreach (var f in files)
        {
            var name = Path.GetFileNameWithoutExtension(f); // e.g. "slot_1"
            if (name.Length > 5 && int.TryParse(name.Substring(5), out int slot))
                result.Add(slot);
        }
        result.Sort();
        return result;
    }

    // ── 旧版兼容 API（保留，不移除）────────────────────────────────────────

    /// <summary>
    /// 保存数据到文件（旧版接口，使用默认 gameData）。
    /// </summary>
    public void SaveData(string fileName)
    {
        try
        {
            string filePath = Path.Combine(savePath, fileName + ".json");
            string json = JsonUtility.ToJson(gameData, true);
            File.WriteAllText(filePath, json);
            Debug.Log($"Data saved to: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save data: {e.Message}");
        }
    }

    /// <summary>
    /// 从文件加载数据（旧版接口）。
    /// </summary>
    public bool LoadData(string fileName)
    {
        try
        {
            string filePath = Path.Combine(savePath, fileName + ".json");
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                gameData = JsonUtility.FromJson<GameData>(json);
                Debug.Log($"Data loaded from: {filePath}");
                return true;
            }
            Debug.LogWarning($"Save file not found: {filePath}");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load data: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 删除存档（旧版接口）。
    /// </summary>
    public bool DeleteSave(string fileName)
    {
        try
        {
            string filePath = Path.Combine(savePath, fileName + ".json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log($"Save deleted: {filePath}");
                return true;
            }
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete save: {e.Message}");
            return false;
        }
    }

    public bool SaveExists(string fileName)
        => File.Exists(Path.Combine(savePath, fileName + ".json"));

    public string[] GetAllSaveFiles()
    {
        var files = Directory.GetFiles(savePath, "*.json");
        for (int i = 0; i < files.Length; i++)
            files[i] = Path.GetFileNameWithoutExtension(files[i]);
        return files;
    }

    public void ResetData() => gameData = new GameData();

    // ── 私有工具 ──────────────────────────────────────────────────────────────
    private string GetSlotPath(int slot) => Path.Combine(savePath, $"slot_{slot}.json");
}

// ── 数据模型 ───────────────────────────────────────────────────────────────────

/// <summary>
/// 多槽位存档容器，传递给各 Provider。
/// </summary>
[Serializable]
public class GameSaveData
{
    public int slotId;
    public GameData gameData;

    // Provider 扩展数据（key = provider key, value = 序列化 JSON 字符串）
    public List<ProviderData> providerExtras = new List<ProviderData>();

    public string GetExtra(string key)
    {
        var item = providerExtras.Find(p => p.key == key);
        return item?.value;
    }

    public void SetExtra(string key, string value)
    {
        var item = providerExtras.Find(p => p.key == key);
        if (item != null) item.value = value;
        else providerExtras.Add(new ProviderData { key = key, value = value });
    }

    [Serializable]
    public class ProviderData
    {
        public string key;
        public string value;
    }
}

/// <summary>
/// 游戏核心数据模型（可根据项目扩展）。
/// </summary>
[Serializable]
public class GameData
{
    public string playerName = "Player";
    public int currentLevel = 1;
    public int score = 0;
    public float playTime = 0f;
}
