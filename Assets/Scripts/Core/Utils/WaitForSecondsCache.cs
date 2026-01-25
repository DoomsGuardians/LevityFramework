// LevityFramework - 通用 Unity 游戏框架
// 工具模块 - WaitForSecondsCache 协程等待缓存

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WaitForSeconds 缓存工具类
/// 避免每次 yield return new WaitForSeconds() 产生 GC 分配
/// </summary>
/// <example>
/// // 使用方式（替代 new WaitForSeconds）
/// yield return WaitForSecondsCache.Get(1f);
/// yield return WaitForSecondsCache.GetRealtime(0.5f);
/// </example>
public static class WaitForSecondsCache
{
    private static readonly Dictionary<float, WaitForSeconds> cache =
        new Dictionary<float, WaitForSeconds>();

    private static readonly Dictionary<float, WaitForSecondsRealtime> realtimeCache =
        new Dictionary<float, WaitForSecondsRealtime>();

    /// <summary>
    /// 获取缓存的 WaitForSeconds（受 TimeScale 影响）
    /// </summary>
    /// <param name="seconds">等待时间（秒）</param>
    /// <returns>缓存的 WaitForSeconds 实例</returns>
    public static WaitForSeconds Get(float seconds)
    {
        if (!cache.TryGetValue(seconds, out var wait))
        {
            wait = new WaitForSeconds(seconds);
            cache[seconds] = wait;
        }
        return wait;
    }

    /// <summary>
    /// 获取缓存的 WaitForSecondsRealtime（不受 TimeScale 影响）
    /// </summary>
    /// <param name="seconds">等待时间（秒）</param>
    /// <returns>缓存的 WaitForSecondsRealtime 实例</returns>
    public static WaitForSecondsRealtime GetRealtime(float seconds)
    {
        if (!realtimeCache.TryGetValue(seconds, out var wait))
        {
            wait = new WaitForSecondsRealtime(seconds);
            realtimeCache[seconds] = wait;
        }
        return wait;
    }

    /// <summary>
    /// 清除所有缓存（通常在场景切换时调用）
    /// </summary>
    public static void Clear()
    {
        cache.Clear();
        realtimeCache.Clear();
    }

    /// <summary>
    /// 获取当前缓存数量
    /// </summary>
    public static int CacheCount => cache.Count + realtimeCache.Count;
}
