// LevityFramework - 通用 Unity 游戏框架
// 核心指令模块 - IMonoLogic 接口

/// <summary>
/// MonoBehaviour 类型逻辑的生命周期接口
/// </summary>
public interface IMonoLogic
{
    /// <summary>在产生时被上层逻辑调用</summary>
    void OnAwake();

    /// <summary>每次进入场景时被上层逻辑调用</summary>
    void OnShow();

    /// <summary>每次场景退出时调用</summary>
    void OnExit();

    /// <summary>在销毁时被上层逻辑调用</summary>
    void UnInit();
}
