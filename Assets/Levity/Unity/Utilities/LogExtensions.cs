// LevityFramework - 通用 Unity 游戏框架
// 工具类 - 日志扩展方法

using UnityEngine;

/// <summary>
/// 日志扩展方法
/// </summary>
public static class LogExtensions
{
    public static void Log(this object obj, string message)
    {
        Debug.Log($"[{obj.GetType().Name}] {message}");
    }

    public static void LogYellow(this object obj, string message)
    {
        Debug.Log($"<color=yellow>[{obj.GetType().Name}] {message}</color>");
    }

    public static void LogGreen(this object obj, string message)
    {
        Debug.Log($"<color=green>[{obj.GetType().Name}] {message}</color>");
    }

    public static void LogRed(this object obj, string message)
    {
        Debug.Log($"<color=red>[{obj.GetType().Name}] {message}</color>");
    }

    public static void Warn(this object obj, string message)
    {
        Debug.LogWarning($"[{obj.GetType().Name}] {message}");
    }

    public static void Error(this object obj, string message)
    {
        Debug.LogError($"[{obj.GetType().Name}] {message}");
    }
}
