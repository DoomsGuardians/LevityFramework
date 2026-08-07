// LevityFramework - 通用 Unity 游戏框架
// 核心指令模块 - GameModeBase 游戏模式基类

using UnityEngine;

/// <summary>
/// 游戏模式的抽象基类，定义模式的生命周期和服务注入
/// </summary>
public abstract class GameModeBase
{
    public GameRoot GameRoot { get; private set; }
    public GameMode currentGameMode;

    protected ResService resService;
    protected RoleSystem roleSystem;
    protected InputService inputService;
    protected TimerService timerService;
    protected ManagerService managerService;
    protected UIService uIService;
    protected DataService dataService;

    public GameModeBase(GameMode gameMode)
    {
        currentGameMode = gameMode;
        GameRoot = GameRoot.Instance;
        resService = GameRoot.resService;
        roleSystem = GameRoot.roleSystem;
        inputService = GameRoot.inputService;
        timerService = GameRoot.timerService;
        managerService = GameRoot.managerService;
        uIService = GameRoot.uIService;
        dataService = GameRoot.dataService;
    }

    /// <summary>每次进入游戏模式时调用</summary>
    public virtual void EnterGameMode()
    {
    }

    /// <summary>开始游戏时调用（屏幕后处理完成时调用）</summary>
    public virtual void StartGame()
    {
        Debug.Log($"====> Entered {currentGameMode} Mode <====");
    }

    /// <summary>每帧更新</summary>
    public abstract void OnUpdate();

    /// <summary>每次退出游戏模式时调用</summary>
    public abstract void UnOnInit();
}
