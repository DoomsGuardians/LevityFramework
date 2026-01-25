// LevityFramework - 通用 Unity 游戏框架
// 核心服务模块 - GameTimerBase 定时器基类

using System;

/// <summary>
/// 游戏定时器基类
/// </summary>
public abstract class GameTimerBase
{
    public Action<string> logFunc;
    public Action<string> warnFunc;
    public Action<string> errorFunc;

    /// <summary>添加定时器</summary>
    public abstract int AddTimer(int time, Action taskCB, Action cancelCB, int count = 1);

    /// <summary>删除定时器</summary>
    public abstract bool DeleteTimer(int tid);

    /// <summary>重置所有定时器</summary>
    public abstract void ResetTimer();

    /// <summary>生成定时器 ID</summary>
    protected abstract int GenerateTid();

    protected int tid;
}
