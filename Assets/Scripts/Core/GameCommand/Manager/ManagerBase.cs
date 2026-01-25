// LevityFramework - 通用 Unity 游戏框架
// 核心指令模块 - ManagerBase 管理器基类

using UnityEngine;

/// <summary>
/// 场景级别管理器的基类
/// 提供服务注入和生命周期管理
/// </summary>
public class ManagerBase : MonoBehaviour, IMonoLogic
{
    protected GameRoot gameRoot;
    protected ResService resService;
    protected RoleSystem roleSystem;
    protected InputService inputService;
    protected TimerService timerService;
    protected ManagerService managerService;
    protected EventService eventService;
    protected DataService dataService;
    protected UIService uiService;

    /// <summary>只在初始化时调用一次</summary>
    public virtual void OnAwake()
    {
        gameRoot = GameRoot.Instance;
        resService = gameRoot.resService;
        roleSystem = gameRoot.roleSystem;
        inputService = gameRoot.inputService;
        timerService = gameRoot.timerService;
        managerService = gameRoot.managerService;
        eventService = gameRoot.eventService;
        dataService = gameRoot.dataService;
        uiService = gameRoot.uIService;
    }

    /// <summary>每次切换场景时调用</summary>
    public virtual void OnShow() { }

    /// <summary>每次场景退出时调用</summary>
    public virtual void OnExit() { }

    /// <summary>Manager 注销时调用（可能是切换场景导致的销毁、也可能是程序退出）</summary>
    public virtual void UnInit() { }
}
