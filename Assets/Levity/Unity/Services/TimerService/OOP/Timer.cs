// LevityFramework - 通用 Unity 游戏框架
// OOP 定时器模块 - Timer 基类

using System;
using UnityEngine;

/// <summary>
/// OOP 风格定时器基类
/// 提供面向对象的定时器实现，与 TimerService 的 ID 方式互补
/// </summary>
/// <example>
/// // 创建倒计时定时器
/// var timer = new CountdownTimer(5f);
/// timer.OnTimerStop += () => Debug.Log("倒计时结束！");
/// timer.Start();
///
/// // 在 Update 或协程中调用 Tick
/// timer.Tick();
///
/// // 或者使用 TimerManager 自动管理
/// var timer = new CountdownTimer(5f);
/// timer.Start(); // 自动注册到 TimerManager
/// </example>
public abstract class Timer : IDisposable
{
    #region 属性

    /// <summary>
    /// 当前时间值
    /// </summary>
    public float CurrentTime { get; protected set; }

    /// <summary>
    /// 初始时间值
    /// </summary>
    protected float initialTime;

    /// <summary>
    /// 是否正在运行
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// 是否已暂停
    /// </summary>
    public bool IsPaused { get; private set; }

    /// <summary>
    /// 进度（0-1）
    /// </summary>
    public float Progress => initialTime > 0 ? Mathf.Clamp01(CurrentTime / initialTime) : 0f;

    /// <summary>
    /// 是否使用非缩放时间（不受 Time.timeScale 影响）
    /// </summary>
    public bool UseUnscaledTime { get; set; }

    /// <summary>
    /// 获取当前帧的 deltaTime
    /// </summary>
    protected float DeltaTime => UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    #endregion

    #region 事件

    /// <summary>
    /// 定时器开始时触发
    /// </summary>
    public event Action OnTimerStart;

    /// <summary>
    /// 定时器停止时触发
    /// </summary>
    public event Action OnTimerStop;

    /// <summary>
    /// 定时器暂停时触发
    /// </summary>
    public event Action OnTimerPause;

    /// <summary>
    /// 定时器恢复时触发
    /// </summary>
    public event Action OnTimerResume;

    #endregion

    #region 构造函数

    protected Timer(float value)
    {
        initialTime = value;
        CurrentTime = value;
    }

    #endregion

    #region 控制方法

    /// <summary>
    /// 开始定时器
    /// </summary>
    public virtual void Start()
    {
        if (IsRunning) return;

        CurrentTime = initialTime;
        IsRunning = true;
        IsPaused = false;

        OOPTimerManager.RegisterTimer(this);
        OnTimerStart?.Invoke();
    }

    /// <summary>
    /// 停止定时器
    /// </summary>
    public virtual void Stop()
    {
        if (!IsRunning) return;

        IsRunning = false;
        IsPaused = false;

        OOPTimerManager.DeregisterTimer(this);
        OnTimerStop?.Invoke();
    }

    /// <summary>
    /// 暂停定时器
    /// </summary>
    public void Pause()
    {
        if (!IsRunning || IsPaused) return;

        IsPaused = true;
        OnTimerPause?.Invoke();
    }

    /// <summary>
    /// 恢复定时器
    /// </summary>
    public void Resume()
    {
        if (!IsRunning || !IsPaused) return;

        IsPaused = false;
        OnTimerResume?.Invoke();
    }

    /// <summary>
    /// 重置定时器到初始状态
    /// </summary>
    public virtual void Reset()
    {
        CurrentTime = initialTime;
    }

    /// <summary>
    /// 重置定时器并设置新的时间
    /// </summary>
    public virtual void Reset(float newTime)
    {
        initialTime = newTime;
        CurrentTime = newTime;
    }

    #endregion

    #region 抽象方法

    /// <summary>
    /// 每帧更新（由 TimerManager 或手动调用）
    /// </summary>
    public abstract void Tick();

    /// <summary>
    /// 是否已完成
    /// </summary>
    public abstract bool IsFinished { get; }

    #endregion

    #region IDisposable

    private bool disposed;

    ~Timer()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed) return;

        if (disposing)
        {
            Stop();
            OnTimerStart = null;
            OnTimerStop = null;
            OnTimerPause = null;
            OnTimerResume = null;
        }

        disposed = true;
    }

    #endregion
}
