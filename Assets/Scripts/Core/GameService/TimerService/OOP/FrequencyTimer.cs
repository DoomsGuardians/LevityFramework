// LevityFramework - 通用 Unity 游戏框架
// OOP 定时器模块 - FrequencyTimer 频率计时器

using System;

/// <summary>
/// 频率计时器
/// 按指定频率（每秒 N 次）触发回调
/// </summary>
/// <example>
/// // 每秒触发 10 次
/// var timer = new FrequencyTimer(10);
/// timer.OnTick += () => Debug.Log("Tick!");
/// timer.Start();
/// </example>
public class FrequencyTimer : Timer
{
    /// <summary>
    /// 每秒触发次数
    /// </summary>
    public int TicksPerSecond { get; private set; }

    /// <summary>
    /// 每次 Tick 触发
    /// </summary>
    public event Action OnTick;

    private float timeThreshold;
    private int tickCount;

    /// <summary>
    /// 已触发的总次数
    /// </summary>
    public int TotalTicks => tickCount;

    public FrequencyTimer(int ticksPerSecond) : base(0)
    {
        SetFrequency(ticksPerSecond);
    }

    /// <summary>
    /// 设置新的频率
    /// </summary>
    public void SetFrequency(int ticksPerSecond)
    {
        TicksPerSecond = ticksPerSecond;
        timeThreshold = ticksPerSecond > 0 ? 1f / ticksPerSecond : 1f;
    }

    public override void Tick()
    {
        if (!IsRunning || IsPaused) return;

        CurrentTime += DeltaTime;

        // 可能在一帧内触发多次（如果帧率低于目标频率）
        while (CurrentTime >= timeThreshold)
        {
            CurrentTime -= timeThreshold;
            tickCount++;
            OnTick?.Invoke();
        }
    }

    /// <summary>
    /// 频率计时器永远不会自动完成
    /// </summary>
    public override bool IsFinished => false;

    public override void Reset()
    {
        base.Reset();
        tickCount = 0;
    }

    public override void Reset(float newTime)
    {
        base.Reset(newTime);
        tickCount = 0;
    }

    /// <summary>
    /// 重置并设置新频率
    /// </summary>
    public void Reset(int newTicksPerSecond)
    {
        SetFrequency(newTicksPerSecond);
        CurrentTime = 0;
        tickCount = 0;
    }
}
