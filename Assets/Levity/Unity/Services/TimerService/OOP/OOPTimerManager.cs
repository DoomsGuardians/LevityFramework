// LevityFramework - 通用 Unity 游戏框架
// OOP 定时器模块 - OOPTimerManager 定时器管理器

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// OOP 定时器管理器
/// 自动更新所有注册的定时器
/// </summary>
/// <remarks>
/// 定时器在调用 Start() 时自动注册，Stop() 或 Dispose() 时自动注销。
/// 也可以手动调用 Timer.Tick() 而不使用管理器。
/// </remarks>
public static class OOPTimerManager
{
    private static readonly List<Timer> timers = new List<Timer>();
    private static readonly List<Timer> sweep = new List<Timer>();
    private static bool isInitialized;

    /// <summary>
    /// 当前管理的定时器数量
    /// </summary>
    public static int TimerCount => timers.Count;

    /// <summary>
    /// 注册定时器（由 Timer.Start() 自动调用）
    /// </summary>
    public static void RegisterTimer(Timer timer)
    {
        if (timer == null || timers.Contains(timer)) return;

        timers.Add(timer);
        EnsureInitialized();
    }

    /// <summary>
    /// 注销定时器（由 Timer.Stop() 自动调用）
    /// </summary>
    public static void DeregisterTimer(Timer timer)
    {
        if (timer == null) return;
        timers.Remove(timer);
    }

    /// <summary>
    /// 更新所有定时器（每帧调用）
    /// </summary>
    public static void UpdateTimers()
    {
        if (timers.Count == 0) return;

        // 使用临时列表避免迭代时修改
        sweep.Clear();
        sweep.AddRange(timers);

        foreach (var timer in sweep)
        {
            if (timer != null && timer.IsRunning)
            {
                timer.Tick();
            }
        }
    }

    /// <summary>
    /// 停止并清除所有定时器
    /// </summary>
    public static void Clear()
    {
        sweep.Clear();
        sweep.AddRange(timers);

        foreach (var timer in sweep)
        {
            timer?.Dispose();
        }

        timers.Clear();
        sweep.Clear();
    }

    /// <summary>
    /// 暂停所有定时器
    /// </summary>
    public static void PauseAll()
    {
        foreach (var timer in timers)
        {
            timer?.Pause();
        }
    }

    /// <summary>
    /// 恢复所有定时器
    /// </summary>
    public static void ResumeAll()
    {
        foreach (var timer in timers)
        {
            timer?.Resume();
        }
    }

    /// <summary>
    /// 确保管理器已初始化
    /// </summary>
    private static void EnsureInitialized()
    {
        if (isInitialized) return;
        isInitialized = true;

        // 可以在这里挂载到 PlayerLoop 或其他更新机制
        // 当前版本依赖 GameRoot 的 Update 调用
    }

    /// <summary>
    /// 重置管理器状态（场景切换时调用）
    /// </summary>
    public static void Reset()
    {
        Clear();
        isInitialized = false;
    }
}

/// <summary>
/// OOP 定时器自动更新组件
/// 挂载到场景中自动更新所有 OOP 定时器
/// </summary>
public class OOPTimerUpdater : MonoBehaviour
{
    private static OOPTimerUpdater instance;

    /// <summary>
    /// 确保更新器存在
    /// </summary>
    public static void EnsureExists()
    {
        if (instance != null) return;

        var go = new GameObject("[OOP Timer Updater]");
        instance = go.AddComponent<OOPTimerUpdater>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Update()
    {
        OOPTimerManager.UpdateTimers();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
