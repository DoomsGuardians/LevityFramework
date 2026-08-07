// LevityFramework - 通用 Unity 游戏框架
// 核心指令模块 - GameRoot 游戏根节点

using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Levity.Composition;
using Levity.Narrative.Core;
#if NANINOVEL
using Levity.Narrative.Naninovel;
#endif
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 核心指令模块：GameRoot
/// 游戏的全局管理器和服务定位器（单例模式）
/// 初始化所有服务、系统和游戏模式
/// </summary>
public class GameRoot : MonoSingleton<GameRoot>
{
    [TitleGroup("Startup Settings"), PropertyOrder(-20)]
    [LabelText("帧率设置")]
    public int GameFrameRate = 60;

    [TitleGroup("Startup Settings"), PropertyOrder(-19)]
    [LabelText("默认关卡 ID")]
    public int TestStageID = 1;

    [TitleGroup("Startup Settings"), PropertyOrder(-18)]
    [LabelText("是否启用存档系统")]
    public bool EnableGameSave = true;

    // 服务模块集合
    private readonly List<ILogic> serviceList = new List<ILogic>();
    public UIService uIService;
    public ResService resService;
    public InputService inputService;
    public TimerService timerService;
    public ManagerService managerService;
    public AudioService audioService;
    public DataService dataService;

    // 系统模块集合
    private readonly List<ILogic> systemList = new List<ILogic>();
    public RoleSystem roleSystem;
    public StageSystem stageSystem;
    public MonoItemSystem monoItemSystem;

#if NANINOVEL
    public NaninovelService naninovelService;
    public NarrativeSequenceRegistry narrativeSequenceRegistry;
    public INarrativeModule narrativeModule;
#endif

    /// <summary>
    /// 派生类覆写以注册项目特定的服务（例如自定义存档、网络服务等）。
    /// 调用时机：所有框架服务已加入 serviceList 但尚未 OnInit。
    /// 将服务加入传入的列表即可，GameRoot 会负责 OnInit/OnUpdate/UnInit 调度。
    /// </summary>
    protected virtual void RegisterCustomServices(List<ILogic> services) { }

    /// <summary>
    /// 派生类覆写以注册项目特定的系统（例如战斗系统、剧情系统等）。
    /// 调用时机：所有框架系统已加入 systemList 但尚未 OnInit。
    /// 将系统加入传入的列表即可，GameRoot 会负责 OnInit/OnUpdate/UnInit 调度。
    /// </summary>
    protected virtual void RegisterCustomSystems(List<ILogic> systems) { }

    /// <summary>
    /// 派生类覆写以注册项目特定的游戏模式。
    /// 调用时机：框架默认 GameMode 已注册之后、首个 GameMode EnterGameMode 之前。
    /// 通过 RegisterGameMode 注册即可。
    /// </summary>
    protected virtual void RegisterCustomGameModes() { }

    // 输入路由器（使用静态类 InputRouter）

    // Update 事件
    private Action updateAction;
    private Composition composition;

    // 游戏模式
    public GameModeBase currentGameMode;
    public GameModeBase lastGameMode;
    private Dictionary<GameMode, GameModeBase> GameModeDic = new Dictionary<GameMode, GameModeBase>();
    private EventSystem eventSystem;

    #region Inspector Debug (Odin)

    [ShowInInspector, ReadOnly, FoldoutGroup("Debug"), LabelText("Service Pipeline"), PropertyOrder(100)]
    private List<string> ServicePipeline => serviceList
        .Select(service => service?.GetType().Name ?? "Null")
        .ToList();

    [ShowInInspector, ReadOnly, FoldoutGroup("Debug"), LabelText("System Pipeline"), PropertyOrder(101)]
    private List<string> SystemPipeline => systemList
        .Select(system => system?.GetType().Name ?? "Null")
        .ToList();

    [ShowInInspector, ReadOnly, FoldoutGroup("Debug"), LabelText("Current Mode"), PropertyOrder(102)]
    private string CurrentMode => currentGameMode == null
        ? "Not Initialised"
        : $"{currentGameMode.currentGameMode} ({currentGameMode.GetType().Name})";

    [ShowInInspector, ReadOnly, FoldoutGroup("Debug"), LabelText("Last Mode"), PropertyOrder(103)]
    private string LastMode => lastGameMode == null
        ? "-"
        : $"{lastGameMode.currentGameMode} ({lastGameMode.GetType().Name})";

    [ShowInInspector, ReadOnly, FoldoutGroup("Debug"), DictionaryDrawerSettings(IsReadOnly = true, KeyLabel = "GameMode", ValueLabel = "Implementation"), PropertyOrder(104)]
    private Dictionary<GameMode, string> RegisteredGameModes => GameModeDic
        .ToDictionary(pair => pair.Key, pair => pair.Value?.GetType().Name ?? "Null");

    #endregion

    #region 生命周期
    public void OnEnterState()
    {
        for (int i = 0; i < serviceList.Count; i++)
        {
            serviceList[i].OnEnterState();
        }
        for (int i = 0; i < systemList.Count; i++)
        {
            systemList[i].OnEnterState();
        }
    }

    // Update 事件注册接口
    public void AddUpdateAction(Action action) => updateAction += action;
    public void RemoveUpdateAction(Action action) => updateAction -= action;

    private void Start()
    {
        #region 设置游戏帧率
        if (GameFrameRate != 0)
        {
            Application.targetFrameRate = GameFrameRate;
        }
        QualitySettings.vSyncCount = 0;
        #endregion

        #region 初始化服务模块
        dataService = new DataService();
        serviceList.Add(dataService);
        resService = new ResService();
        serviceList.Add(resService);
        uIService = new UIService();
        serviceList.Add(uIService);
        inputService = new InputService();
        serviceList.Add(inputService);
        managerService = new ManagerService();
        serviceList.Add(managerService);
        audioService = new AudioService();
        serviceList.Add(audioService);
        timerService = new TimerService();
        serviceList.Add(timerService);

#if NANINOVEL
        narrativeSequenceRegistry = NaninovelSequenceCatalog.CreateDefault();
        naninovelService = new NaninovelService();
        serviceList.Add(naninovelService);
        narrativeModule = new NaninovelNarrativeBackend(narrativeSequenceRegistry, naninovelService);
        dataService.SetSaveAvailabilitySource(() => narrativeModule.SaveAvailability);
#endif

        RegisterCustomServices(serviceList);

        #endregion

        #region 初始化系统模块
        roleSystem = new RoleSystem();
        systemList.Add(roleSystem);
        stageSystem = new StageSystem();
        systemList.Add(stageSystem);
        monoItemSystem = new MonoItemSystem();
        systemList.Add(monoItemSystem);

        RegisterCustomSystems(systemList);

        composition = new Composition();
        foreach (var service in serviceList)
        {
            composition.Register(new LogicCompositionModule(service));
        }
        foreach (var system in systemList)
        {
            composition.Register(new LogicCompositionModule(system));
        }
        composition.Start();
        #endregion

        InitGameModes();

        #region DOTween 初始化
        // 设置 DOTween 默认忽略 TimeScale 影响
        DOTween.defaultTimeScaleIndependent = true;
        // 提前初始化 DOTween，避免首次使用时报错
        DOTween.Init();
        #endregion

        eventSystem = FindAnyObjectByType<EventSystem>();

        this.LogGreen("GameRoot initialized successfully!");

        // 自动加载默认关卡
        if (TestStageID > 0)
        {
            stageSystem.LoadStage(TestStageID);
        }
    }

    private void Update()
    {
        updateAction?.Invoke();
        currentGameMode?.OnUpdate();
        for (int i = 0; i < serviceList.Count; i++)
        {
            serviceList[i].OnUpdate();
        }
        for (int i = 0; i < systemList.Count; i++)
        {
            systemList[i].OnUpdate();
        }
    }

    private void OnApplicationQuit()
    {
        composition?.Shutdown();
    }
    #endregion

    #region 游戏模式状态控制
    private void InitGameModes()
    {
        // 注册默认游戏模式（可根据项目扩展）
        RegisterGameMode(new DefaultGameMode());

        RegisterCustomGameModes();

        if (GameModeDic.TryGetValue(GameMode.GameStart, out var defaultMode))
        {
            currentGameMode = defaultMode;
            currentGameMode.EnterGameMode();
        }
    }

    public void RegisterGameMode(GameModeBase mode)
    {
        if (mode == null) return;
        GameModeDic[mode.currentGameMode] = mode;
    }

    public void ChangeGameMode(GameMode targetMode)
    {
        currentGameMode?.UnOnInit();
        lastGameMode = currentGameMode;
        if (GameModeDic.TryGetValue(targetMode, out var gameMode))
        {
            currentGameMode = gameMode;
            currentGameMode.EnterGameMode();
        }
        else
        {
            Debug.LogWarning($"targetMode:{targetMode} does not exist");
        }
    }
    #endregion

    #region 输入开关
    public void CancelInput()
    {
        if (eventSystem != null) eventSystem.enabled = false;
        InputRouter.Acquire(InputChannel.Gameplay, this);
        InputRouter.Acquire(InputChannel.UI, this);
    }

    public void ResetInput()
    {
        if (eventSystem != null) eventSystem.enabled = true;
        InputRouter.Release(InputChannel.Gameplay, this);
        InputRouter.Release(InputChannel.UI, this);
    }

    /// <summary>
    /// 锁定指定输入通道
    /// </summary>
    public void LockInputChannel(InputChannel channel, object owner)
    {
        InputRouter.Acquire(channel, owner);
    }

    /// <summary>
    /// 解锁指定输入通道
    /// </summary>
    public void UnlockInputChannel(InputChannel channel, object owner)
    {
        InputRouter.Release(channel, owner);
    }

    /// <summary>
    /// 检查输入通道是否可用
    /// </summary>
    public bool IsInputChannelAvailable(InputChannel channel)
    {
        return InputRouter.IsEnabled(channel);
    }
    #endregion

    #region Gizmos 绘制
    private int GizmosId = 0;
    private Dictionary<int, Action> gizmosDic = new Dictionary<int, Action>();

    public int AddDrawGizmos(Action drawAction)
    {
        int id = GenerateGizmosID();
        gizmosDic[id] = drawAction;
        return id;
    }

    public bool RemoveDrawGizmos(int drawId)
    {
        if (gizmosDic.ContainsKey(drawId))
        {
            gizmosDic.Remove(drawId);
            return true;
        }
        return false;
    }

    private int GenerateGizmosID()
    {
        while (true)
        {
            ++GizmosId;
            if (GizmosId > int.MaxValue)
            {
                GizmosId = 0;
            }
            if (!gizmosDic.ContainsKey(GizmosId))
            {
                return GizmosId;
            }
        }
    }

    private void OnDrawGizmos()
    {
        foreach (var cb in gizmosDic.Values)
        {
            cb?.Invoke();
        }
    }
    #endregion
}

/// <summary>
/// 默认游戏模式（示例实现）
/// </summary>
public class DefaultGameMode : GameModeBase
{
    public DefaultGameMode() : base(GameMode.GameStart) { }

    public override void OnUpdate() { }

    public override void UnOnInit() { }
}
