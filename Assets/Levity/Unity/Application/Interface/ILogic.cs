// LevityFramework - 通用 Unity 游戏框架
// 核心指令模块 - ILogic 接口

/// <summary>
/// 服务和系统的核心生命周期接口
/// </summary>
public interface ILogic
{
    /// <summary>程序开始时调用</summary>
    void OnInit();

    /// <summary>每次进入场景时调用</summary>
    void OnEnterState();

    /// <summary>每帧更新</summary>
    void OnUpdate();

    /// <summary>程序退出时调用</summary>
    void UnInit();
}
