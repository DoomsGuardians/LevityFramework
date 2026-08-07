// LevityFramework - 通用 Unity 游戏框架
// 核心指令模块 - CommandLogicSOBase 命令逻辑基类

using System;
using UnityEngine;

/// <summary>
/// 场景转换命令基类 ScriptableObject
/// 用于实现转场特效（淡入淡出、圆形遮罩等）
/// </summary>
public abstract class CommandLogicSOBase : ScriptableObject
{
    private Action doneCB;
    private object param;

    /// <summary>
    /// 执行命令
    /// </summary>
    /// <param name="doneCB">完成回调</param>
    /// <param name="delay">延迟时间（秒），-1 表示立即执行</param>
    /// <param name="param">可选参数</param>
    public void Execute(Action doneCB, float delay = -1, object param = null)
    {
        if (delay > 0)
        {
            this.doneCB = doneCB;
            this.param = param;
            GameRoot.Instance.timerService.AddTimer((int)(delay * 1000), OnExecuteDelay);
            return;
        }

        this.doneCB = null;
        this.param = null;
        OnExecute(doneCB, param);
    }

    private void OnExecuteDelay()
    {
        OnExecute(doneCB, param);
    }

    /// <summary>
    /// 子类实现具体的执行逻辑
    /// </summary>
    protected abstract void OnExecute(Action doneCB, object param = null);
}
