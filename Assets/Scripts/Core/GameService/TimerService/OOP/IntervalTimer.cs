// LevityFramework - 通用 Unity 游戏框架
// OOP 定时器模块 - IntervalTimer 间隔计时器

using System;

/// <summary>
/// 间隔计时器
/// 在总时间内，每隔指定间隔触发一次事件
/// </summary>
/// <example>
/// // 5秒内每隔1秒触发一次
/// var timer = new IntervalTimer(5f, 1f);
/// timer.OnInterval += () => Debug.Log("每秒触发");
/// timer.OnCountdownComplete += () => Debug.Log("全部完成");
/// timer.Start();
/// </example>
public class IntervalTimer : Timer
{
    /// <summary>
    /// 间隔时间
    /// </summary>
    public float Interval { get; private set; }

    /// <summary>
    /// 每次间隔触发
    /// </summary>
    public event Action OnInterval;

    /// <summary>
    /// 倒计时完成时触发
    /// </summary>
    public event Action OnCountdownComplete;

    private float nextIntervalTime;
    private int intervalCount;

    /// <summary>
    /// 已触发的间隔次数
    /// </summary>
    public int IntervalCount => intervalCount;

    /// <summary>
    /// 剩余时间
    /// </summary>
    public float RemainingTime => CurrentTime;

    public IntervalTimer(float totalTime, float interval) : base(totalTime)
    {
        Interval = interval;
        nextIntervalTime = totalTime - interval;
    }

    public override void Tick()
    {
        if (!IsRunning || IsPaused) return;

        if (CurrentTime > 0)
        {
            CurrentTime -= DeltaTime;

            // 检查是否触发间隔事件
            while (CurrentTime <= nextIntervalTime && nextIntervalTime >= 0)
            {
                intervalCount++;
                OnInterval?.Invoke();
                nextIntervalTime -= Interval;
            }
        }

        if (CurrentTime <= 0)
        {
            CurrentTime = 0;
            OnCountdownComplete?.Invoke();
            Stop();
        }
    }

    public override bool IsFinished => CurrentTime <= 0;

    public override void Reset()
    {
        base.Reset();
        nextIntervalTime = initialTime - Interval;
        intervalCount = 0;
    }

    public override void Reset(float newTime)
    {
        base.Reset(newTime);
        nextIntervalTime = initialTime - Interval;
        intervalCount = 0;
    }

    /// <summary>
    /// 重置并设置新的时间和间隔
    /// </summary>
    public void Reset(float newTime, float newInterval)
    {
        initialTime = newTime;
        CurrentTime = newTime;
        Interval = newInterval;
        nextIntervalTime = newTime - newInterval;
        intervalCount = 0;
    }
}
