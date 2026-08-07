// LevityFramework - 通用 Unity 游戏框架
// 交互接口模块 - IState 状态接口

/// <summary>
/// 有限状态机状态接口
/// </summary>
public interface IState
{
    /// <summary>进入状态</summary>
    void OnEnter();

    /// <summary>退出状态</summary>
    void OnExit();

    /// <summary>每帧更新</summary>
    void OnUpdate();

    /// <summary>动画更新</summary>
    void OnAnimatorUpdate();

    /// <summary>固定物理更新</summary>
    void OnFixedUpdate();

    /// <summary>动画结束</summary>
    void OnAnimationEnd();
}
