// LevityFramework - 通用 Unity 游戏框架
// 交互接口模块 - StateMachineBase 状态机基类

/// <summary>
/// 有限状态机基类
/// </summary>
public class StateMachineBase
{
    public IState currentState;
    public IState lastState;

    /// <summary>
    /// 切换状态
    /// </summary>
    public void ChangeState(IState targetState)
    {
        if (targetState != currentState)
        {
            lastState = currentState;
        }
        currentState?.OnExit();
        currentState = targetState;
        currentState?.OnEnter();
    }

    /// <summary>
    /// 每帧更新
    /// </summary>
    public void UpdateState()
    {
        currentState?.OnUpdate();
    }

    /// <summary>
    /// 动画更新
    /// </summary>
    public void AnimatorUpdateState()
    {
        currentState?.OnAnimatorUpdate();
    }

    /// <summary>
    /// 固定物理更新
    /// </summary>
    public void FixedUpdateState()
    {
        currentState?.OnFixedUpdate();
    }

    /// <summary>
    /// 动画结束回调
    /// </summary>
    public void AnimationEnd()
    {
        currentState?.OnAnimationEnd();
    }
}
