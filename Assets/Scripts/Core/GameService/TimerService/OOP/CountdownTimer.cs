// LevityFramework - 通用 Unity 游戏框架
// OOP 定时器模块 - CountdownTimer 倒计时器

using System;

/// <summary>
/// 倒计时定时器
/// 从指定时间倒计时到零，完成后自动停止
/// </summary>
/// <example>
/// var timer = new CountdownTimer(3f);
/// timer.OnTimerStop += () => Debug.Log("3秒倒计时结束！");
/// timer.Start();
/// </example>
public class CountdownTimer : Timer
{
    /// <summary>
    /// 倒计时完成时触发（与 OnTimerStop 等价，语义更清晰）
    /// </summary>
    public event Action OnCountdownComplete;

    /// <summary>
    /// 剩余时间
    /// </summary>
    public float RemainingTime => CurrentTime;

    /// <summary>
    /// 已用时间
    /// </summary>
    public float ElapsedTime => initialTime - CurrentTime;

    /// <summary>
    /// 倒计时进度（0 = 开始，1 = 完成）
    /// </summary>
    public float CountdownProgress => initialTime > 0 ? 1f - (CurrentTime / initialTime) : 1f;

    public CountdownTimer(float duration) : base(duration) { }

    public override void Tick()
    {
        if (!IsRunning || IsPaused) return;

        if (CurrentTime > 0)
        {
            CurrentTime -= DeltaTime;
        }

        if (CurrentTime <= 0)
        {
            CurrentTime = 0;
            OnCountdownComplete?.Invoke();
            Stop();
        }
    }

    public override bool IsFinished => CurrentTime <= 0;
}
