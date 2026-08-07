// LevityFramework - 通用 Unity 游戏框架
// 核心服务模块 - TimerService 定时器服务

using System;
using UnityEngine;

/// <summary>
/// 定时器服务：支持循环定时、延迟执行、计时调整
/// </summary>
public class TimerService : ILogic
{
    public TickTimer tickTimer { get; private set; }
    public PrecisionTimer precisionTimer { get; private set; }

    public void OnInit()
    {
        precisionTimer = new PrecisionTimer(0);
        precisionTimer.logFunc = Debug.Log;
        precisionTimer.warnFunc = Debug.LogWarning;
        precisionTimer.errorFunc = Debug.LogError;
    }

    public void OnEnterState() { }

    public void OnUpdate()
    {
        precisionTimer.UpdateTime();
    }

    public void UnInit()
    {
        precisionTimer.ResetTimer();
    }

    /// <summary>
    /// 添加循环定时器
    /// </summary>
    public int AddLoopTimer(TimerType timerType, int delayMs, Action taskCB, Action cancelCB = null, Action loopEndCB = null, int count = 1)
    {
        return precisionTimer.AddTimer(delayMs, taskCB, cancelCB, loopEndCB, count, timerType);
    }

    /// <summary>
    /// 添加定时器（指定类型）
    /// </summary>
    public int AddTimer(TimerType timerType, int delayMs, Action taskCB, Action cancelCB = null, int count = 1)
    {
        return precisionTimer.AddTimer(delayMs, taskCB, cancelCB, null, count, timerType);
    }

    /// <summary>
    /// 添加定时器（默认使用 RealTime）
    /// </summary>
    public int AddTimer(int delay, Action taskCB, Action cancelCB = null, int count = 1)
    {
        return precisionTimer.AddTimer(delay, taskCB, cancelCB, count);
    }

    /// <summary>
    /// 移除定时器
    /// </summary>
    public void RemoveTimer(int tid)
    {
        precisionTimer.DeleteTimer(tid);
    }

    /// <summary>
    /// 调整定时器时间
    /// </summary>
    public bool AdjustTimer(int tid, int delayOffset)
    {
        return precisionTimer.AdjustTime(tid, delayOffset);
    }

    /// <summary>
    /// 暂停定时器
    /// </summary>
    public bool StopTimer(int tid)
    {
        return precisionTimer.SetPaused(tid, true);
    }

    /// <summary>
    /// 恢复定时器
    /// </summary>
    public bool EnableTimer(int tid)
    {
        return precisionTimer.SetPaused(tid, false);
    }

    /// <summary>
    /// 查询剩余时间
    /// </summary>
    public int QueryRemaining(int tid)
    {
        return precisionTimer.QueryRemaining(tid);
    }
}
