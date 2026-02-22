// LevityFramework - 通用 Unity 游戏框架
// OOP 定时器模块 - StopwatchTimer 秒表计时器

/// <summary>
/// 秒表计时器
/// 从零开始计时，无限增长，适合测量持续时间
/// </summary>
/// <example>
/// var stopwatch = new StopwatchTimer();
/// stopwatch.Start();
/// // ... 一段时间后 ...
/// Debug.Log($"已运行 {stopwatch.CurrentTime} 秒");
/// </example>
public class StopwatchTimer : Timer
{
    /// <summary>
    /// 已用时间（与 CurrentTime 相同，语义更清晰）
    /// </summary>
    public float ElapsedTime => CurrentTime;

    public StopwatchTimer() : base(0) { }

    public override void Tick()
    {
        if (!IsRunning || IsPaused) return;

        CurrentTime += DeltaTime;
    }

    /// <summary>
    /// 秒表永远不会自动完成
    /// </summary>
    public override bool IsFinished => false;

    /// <summary>
    /// 获取格式化的时间字符串 (mm:ss.ff)
    /// </summary>
    public string GetFormattedTime()
    {
        int minutes = (int)(CurrentTime / 60);
        int seconds = (int)(CurrentTime % 60);
        int milliseconds = (int)((CurrentTime * 100) % 100);
        return $"{minutes:00}:{seconds:00}.{milliseconds:00}";
    }

    /// <summary>
    /// 获取格式化的时间字符串 (hh:mm:ss)
    /// </summary>
    public string GetFormattedTimeLong()
    {
        int hours = (int)(CurrentTime / 3600);
        int minutes = (int)((CurrentTime % 3600) / 60);
        int seconds = (int)(CurrentTime % 60);
        return $"{hours:00}:{minutes:00}:{seconds:00}";
    }
}
