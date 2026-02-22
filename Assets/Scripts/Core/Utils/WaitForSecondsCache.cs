// LevityFramework - 通用 Unity 游戏框架
// 工具模块 - WaitFor 协程等待缓存

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 协程等待缓存工具类
/// 避免每次 yield return new WaitForXXX() 产生 GC 分配
/// </summary>
/// <example>
/// // 使用方式
/// yield return WaitFor.Seconds(1f);
/// yield return WaitFor.SecondsRealtime(0.5f);
/// yield return WaitFor.FixedUpdate;
/// yield return WaitFor.EndOfFrame;
/// </example>
public static class WaitFor
{
    #region 静态等待对象

    private static readonly WaitForFixedUpdate fixedUpdate = new WaitForFixedUpdate();
    private static readonly WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();

    /// <summary>
    /// 等待下一个 FixedUpdate（缓存实例，无 GC）
    /// </summary>
    public static WaitForFixedUpdate FixedUpdate => fixedUpdate;

    /// <summary>
    /// 等待当前帧渲染完成（缓存实例，无 GC）
    /// </summary>
    public static WaitForEndOfFrame EndOfFrame => endOfFrame;

    #endregion

    #region 时间等待缓存

    private static readonly Dictionary<float, WaitForSeconds> secondsCache =
        new Dictionary<float, WaitForSeconds>(100);

    private static readonly Dictionary<float, WaitForSecondsRealtime> realtimeCache =
        new Dictionary<float, WaitForSecondsRealtime>(50);

    /// <summary>
    /// 获取缓存的 WaitForSeconds（受 TimeScale 影响）
    /// </summary>
    /// <param name="seconds">等待时间（秒）</param>
    /// <returns>缓存的 WaitForSeconds 实例</returns>
    public static WaitForSeconds Seconds(float seconds)
    {
        // 极短时间返回 null（等同于不等待）
        if (seconds < 1f / Application.targetFrameRate) return null;

        if (!secondsCache.TryGetValue(seconds, out var wait))
        {
            wait = new WaitForSeconds(seconds);
            secondsCache[seconds] = wait;
        }
        return wait;
    }

    /// <summary>
    /// 获取缓存的 WaitForSecondsRealtime（不受 TimeScale 影响）
    /// </summary>
    /// <param name="seconds">等待时间（秒）</param>
    /// <returns>缓存的 WaitForSecondsRealtime 实例</returns>
    public static WaitForSecondsRealtime SecondsRealtime(float seconds)
    {
        if (!realtimeCache.TryGetValue(seconds, out var wait))
        {
            wait = new WaitForSecondsRealtime(seconds);
            realtimeCache[seconds] = wait;
        }
        return wait;
    }

    #endregion

    #region 缓存管理

    /// <summary>
    /// 清除时间等待缓存（通常在场景切换时调用）
    /// </summary>
    public static void ClearCache()
    {
        secondsCache.Clear();
        realtimeCache.Clear();
    }

    /// <summary>
    /// 获取当前缓存数量
    /// </summary>
    public static int CacheCount => secondsCache.Count + realtimeCache.Count;

    #endregion
}

/// <summary>
/// 旧版 API 兼容（建议使用 WaitFor 类）
/// </summary>
public static class WaitForSecondsCache
{
    /// <summary>
    /// 获取缓存的 WaitForSeconds（受 TimeScale 影响）
    /// </summary>
    public static WaitForSeconds Get(float seconds) => WaitFor.Seconds(seconds);

    /// <summary>
    /// 获取缓存的 WaitForSecondsRealtime（不受 TimeScale 影响）
    /// </summary>
    public static WaitForSecondsRealtime GetRealtime(float seconds) => WaitFor.SecondsRealtime(seconds);

    /// <summary>
    /// 清除所有缓存
    /// </summary>
    public static void Clear() => WaitFor.ClearCache();

    /// <summary>
    /// 获取当前缓存数量
    /// </summary>
    public static int CacheCount => WaitFor.CacheCount;
}
