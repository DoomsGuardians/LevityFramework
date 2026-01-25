// LevityFramework - 通用 Unity 游戏框架
// 核心服务模块 - DataService 数据存档服务

using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 数据服务：管理游戏存档、加载、保存
/// 可根据项目需要扩展序列化方式
/// </summary>
public class DataService : ILogic
{
    private string savePath;
    public GameData gameData { get; private set; }

    public void OnInit()
    {
        savePath = Path.Combine(Application.persistentDataPath, "saves");
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }
        gameData = new GameData();
    }

    public void OnEnterState() { }

    public void OnUpdate() { }

    public void UnInit() { }

    /// <summary>
    /// 保存数据到文件
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
    /// 从文件加载数据
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
    /// 删除存档
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

    /// <summary>
    /// 检查存档是否存在
    /// </summary>
    public bool SaveExists(string fileName)
    {
        string filePath = Path.Combine(savePath, fileName + ".json");
        return File.Exists(filePath);
    }

    /// <summary>
    /// 获取所有存档文件名
    /// </summary>
    public string[] GetAllSaveFiles()
    {
        var files = Directory.GetFiles(savePath, "*.json");
        for (int i = 0; i < files.Length; i++)
        {
            files[i] = Path.GetFileNameWithoutExtension(files[i]);
        }
        return files;
    }

    /// <summary>
    /// 重置游戏数据
    /// </summary>
    public void ResetData()
    {
        gameData = new GameData();
    }
}

/// <summary>
/// 游戏数据模型（可根据项目扩展）
/// </summary>
[Serializable]
public class GameData
{
    public string playerName = "Player";
    public int currentLevel = 1;
    public int score = 0;
    public float playTime = 0f;

    // 可根据项目需要添加更多字段
}
