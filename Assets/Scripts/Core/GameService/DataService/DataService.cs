// LevityFramework - 通用 Unity 游戏框架
// 核心服务模块 - DataService 数据存档服务

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Levity.Narrative.Core;
using Levity.UnifiedSave;
using UnityEngine;

/// <summary>
/// 数据服务：管理游戏存档、加载、保存。
/// 采用 Unified Save 事务，收集 versioned contributors 后原子替换单一槽位。
/// 对外暴露多槽位接口 SaveToSlot / LoadFromSlot / DeleteSlot。
/// </summary>
public class DataService : ILogic
{
    // ── 存档路径 ─────────────────────────────────────────────────────────────
    private string savePath;

    // ── Provider 钩子 ─────────────────────────────────────────────────────────
    private readonly Dictionary<string, Action<int>> deleteProviders
        = new Dictionary<string, Action<int>>();

    private readonly List<IUnifiedSaveContributor> unifiedContributors =
        new List<IUnifiedSaveContributor>();
    private UnifiedSave unifiedSave;
    private Func<SaveAvailability> saveAvailability = () => SaveAvailability.Allowed;

    // ── 当前内存数据 ──────────────────────────────────────────────────────────
    public GameData gameData { get; private set; }

    // ── ILogic ────────────────────────────────────────────────────────────────
    public void OnInit()
    {
        savePath = Path.Combine(Application.persistentDataPath, "saves");
        if (!Directory.Exists(savePath))
            Directory.CreateDirectory(savePath);

        gameData = new GameData();
        unifiedContributors.Add(new GameDataUnifiedSaveContributor(
            () => gameData,
            restored => gameData = restored));
        RebuildUnifiedSave();
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
    [Obsolete("Use AddUnifiedSaveContributor(IUnifiedSaveContributor). String-key providers cannot participate in atomic saves.")]
    public void AddSaveProvider(string key, Action<GameSaveData> onSave)
    {
        throw new NotSupportedException(
            "String-key save providers are not atomic. Implement IUnifiedSaveContributor and call AddUnifiedSaveContributor instead.");
    }

    /// <summary>
    /// 注册加载 Provider。每个 key 唯一；重复注册会覆盖旧 Provider。
    /// 在 DataService.LoadFromSlot 时，<paramref name="onLoad"/> 会被 await，
    /// 可从 <see cref="GameSaveData"/> 恢复数据，也可触发外部加载（如 Naninovel LoadGame）。
    /// </summary>
    [Obsolete("Use AddUnifiedSaveContributor(IUnifiedSaveContributor). String-key providers cannot participate in atomic loads.")]
    public void AddLoadProvider(string key, Func<GameSaveData, Task> onLoad)
    {
        throw new NotSupportedException(
            "String-key load providers are not atomic. Implement IUnifiedSaveContributor and call AddUnifiedSaveContributor instead.");
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
        deleteProviders.Remove(key);
    }

    /// <summary>Registers a versioned contributor in the single atomic save transaction.</summary>
    public void AddUnifiedSaveContributor(IUnifiedSaveContributor contributor)
    {
        if (contributor == null) throw new ArgumentNullException(nameof(contributor));
        unifiedContributors.Add(contributor);
        RebuildUnifiedSave();
    }

    /// <summary>Uses a game-owned source to decide whether a save may begin.</summary>
    public void SetSaveAvailabilitySource(Func<SaveAvailability> source)
    {
        saveAvailability = source ?? throw new ArgumentNullException(nameof(source));
    }

    // ── 多槽位存档接口 ────────────────────────────────────────────────────────

    /// <summary>
    /// 将全部 Unified Save contributors 原子保存到指定槽位。
    /// </summary>
    public async Task<SaveSlotResult> SaveToSlot(int slot)
    {
        var availability = saveAvailability();
        if (!availability.CanSave)
            return SaveSlotResult.Blocked(availability.BlockedReason);

        try
        {
            await unifiedSave.SaveAsync(GetUnifiedSlotId(slot));
            Debug.Log($"[DataService] Atomically saved Unified Save slot {slot}.");
            return SaveSlotResult.Saved();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DataService] Failed to write slot {slot}: {ex}");
            return SaveSlotResult.Failed(ex);
        }
    }

    /// <summary>
    /// 从一个 Unified Save 槽位恢复全部 contributors。
    /// </summary>
    public async Task<bool> LoadFromSlot(int slot)
    {
        if (!SlotExists(slot))
        {
            Debug.LogWarning($"[DataService] Unified Save slot {slot} not found.");
            return false;
        }

        try
        {
            await unifiedSave.LoadAsync(GetUnifiedSlotId(slot));
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DataService] Failed to read slot {slot}: {ex}");
            return false;
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
            string filePath = GetUnifiedSlotPath(slot);
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
    public bool SlotExists(int slot) => File.Exists(GetUnifiedSlotPath(slot));

    /// <summary>
    /// 获取所有已存在的槽位编号。
    /// </summary>
    public List<int> GetExistingSlots()
    {
        var result = new List<int>();
        var files = Directory.GetFiles(savePath, "slot_*.levity-save");
        foreach (var f in files)
        {
            var name = Path.GetFileNameWithoutExtension(f);
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
    private string GetUnifiedSlotId(int slot) => $"slot_{slot}";
    private string GetUnifiedSlotPath(int slot) =>
        Path.Combine(savePath, $"{GetUnifiedSlotId(slot)}.levity-save");

    private void RebuildUnifiedSave() =>
        unifiedSave = new UnifiedSave(new FileUnifiedSaveStore(savePath), unifiedContributors.ToArray());

    private sealed class GameDataUnifiedSaveContributor : IUnifiedSaveContributor
    {
        private readonly Func<GameData> capture;
        private readonly Action<GameData> restore;

        public GameDataUnifiedSaveContributor(Func<GameData> capture, Action<GameData> restore)
        {
            this.capture = capture;
            this.restore = restore;
        }

        public string Id => "gameplay";
        public int Version => 1;
        public Task<string> CaptureAsync(System.Threading.CancellationToken cancellationToken = default) =>
            Task.FromResult(JsonUtility.ToJson(capture()));
        public Task RestoreAsync(
            int version,
            string state,
            System.Threading.CancellationToken cancellationToken = default)
        {
            if (version != Version) throw new InvalidOperationException($"Unsupported gameplay save version {version}.");
            restore(JsonUtility.FromJson<GameData>(state) ?? new GameData());
            return Task.CompletedTask;
        }
    }
}

public enum SaveSlotStatus
{
    Saved,
    Blocked,
    Failed
}

public readonly struct SaveSlotResult
{
    private SaveSlotResult(SaveSlotStatus status, string blockedReason, Exception exception)
    {
        Status = status;
        BlockedReason = blockedReason;
        Exception = exception;
    }

    public SaveSlotStatus Status { get; }
    public string BlockedReason { get; }
    public Exception Exception { get; }

    public static SaveSlotResult Saved() => new SaveSlotResult(SaveSlotStatus.Saved, null, null);
    public static SaveSlotResult Blocked(string reason) =>
        new SaveSlotResult(SaveSlotStatus.Blocked, reason, null);
    public static SaveSlotResult Failed(Exception exception) =>
        new SaveSlotResult(SaveSlotStatus.Failed, null, exception ?? throw new ArgumentNullException(nameof(exception)));
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
