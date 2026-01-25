// LevityFramework - 通用 Unity 游戏框架
// 核心系统模块 - StageSystem 关卡系统

using System;
using UnityEngine;

/// <summary>
/// 关卡系统：加载关卡、初始化 Manager、处理场景转换、执行屏幕特效
/// </summary>
public class StageSystem : ILogic
{
    private GameRoot gameRoot;
    private ResService resService;
    private ManagerService managerService;
    private RoleSystem roleSystem;
    private UIService uiService;
    private EventService eventService;
    private DataService dataService;
    private MonoItemSystem monoItemSystem;

    private GameStageConfig gameStageConfig;
    public StageConfigItem currentStageConfig;

#if NANINOVEL
    private NaninovelService naninovelService;
#endif

    public void OnInit()
    {
        gameRoot = GameRoot.Instance;
        resService = gameRoot.resService;
        managerService = gameRoot.managerService;
        roleSystem = gameRoot.roleSystem;
        uiService = gameRoot.uIService;
        eventService = gameRoot.eventService;
        dataService = gameRoot.dataService;
        monoItemSystem = gameRoot.monoItemSystem;

#if NANINOVEL
        naninovelService = gameRoot.naninovelService;
#endif
    }

    public void OnUpdate() { }

    public void OnEnterState() { }

    public void UnInit() { }

    /// <summary>
    /// 加载关卡（通过 StageID）
    /// </summary>
    /// <param name="stageID">关卡 ID</param>
    /// <param name="doneLoadTask">加载完成回调</param>
    /// <param name="gameDataID">游戏存档 ID（-1 表示不更新）</param>
    public void LoadStage(int stageID, Action doneLoadTask = null, int gameDataID = -1)
    {
        // 禁用输入
        gameRoot.CancelInput();

        // 加载游戏关卡配置 SO
        gameStageConfig = resService.LoadResource<GameStageConfig>(GameResPathConfig.StageConfig);
        if (gameStageConfig == null)
        {
            Debug.LogError($"[StageSystem] Failed to load GameStageConfig from: {GameResPathConfig.StageConfig}");
            gameRoot.ResetInput();
            return;
        }

        // 获取具体关卡配置信息
        currentStageConfig = gameStageConfig.GetStageItem(stageID);
        if (currentStageConfig == null)
        {
            Debug.LogError($"[StageSystem] Stage {stageID} not found in config!");
            gameRoot.ResetInput();
            return;
        }

        // 通知 Manager 和 RoleSystem 退出
        managerService.OnSceneExit();

        // 卸载当前场景的 MonoItem
        monoItemSystem?.UnloadMonoItems();

        // 更新当前游戏存档 ID
        if (gameDataID != -1 && dataService != null)
        {
            // dataService.UpdateCurrentGameDataID(gameDataID);
        }

        // 构建加载完成回调
        doneLoadTask += () =>
        {
            OnStageLoaded();
        };

        // 执行退出场景的屏幕特效
        if (currentStageConfig.loadSceneFXRegister != null && currentStageConfig.loadSceneFXRegister.exitStateLogic != null)
        {
            currentStageConfig.loadSceneFXRegister.exitStateLogic.Execute(
                () => SetStage(currentStageConfig, doneLoadTask), -1, null);
        }
        else
        {
            SetStage(currentStageConfig, doneLoadTask);
        }
    }

    /// <summary>
    /// 场景加载完成后的初始化
    /// </summary>
    private void OnStageLoaded()
    {
        // 执行进入场景的屏幕特效
        if (currentStageConfig.loadSceneFXRegister != null && currentStageConfig.loadSceneFXRegister.enterStageLogic != null)
        {
            currentStageConfig.loadSceneFXRegister.enterStageLogic.Execute(() =>
            {
                this.LogYellow($"Started Game Mode: {currentStageConfig.gameMode}");
                gameRoot.currentGameMode?.StartGame();
            }, 1, null);
        }

        // 服务层进入状态
        gameRoot.OnEnterState();

        // 切换游戏模式
        gameRoot.ChangeGameMode(currentStageConfig.gameMode);

        // 恢复输入
        gameRoot.ResetInput();

        // UI 窗口初始化
        // uiService.SwitchBaseLayerUI(currentStageConfig.UIWindowBase);

        // 玩家角色初始化
        // roleSystem?.LoadRole(currentStageConfig.RoleConfig);

        // Manager 初始化
        // managerService.UpdateManager(currentStageConfig.GameMangers);

#if NANINOVEL
        // Naninovel 配置应用
        naninovelService?.ApplyStageConfig(currentStageConfig);
#endif

        // MonoItem 初始化
        monoItemSystem?.InitMonoItem();

        // 发送初始化完成事件
        eventService?.SendMessage(EventID.OnInitDone, null, null);
    }

    /// <summary>
    /// 处理场景的加载
    /// </summary>
    private void SetStage(StageConfigItem stageConfigItem, Action doneLoadTask)
    {
        resService.LoadScene(stageConfigItem.sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single, () =>
        {
            this.Log($"Loaded scene: {stageConfigItem.sceneName}");
            doneLoadTask?.Invoke();
        });
    }

    /// <summary>
    /// 重新加载当前关卡
    /// </summary>
    public void ReloadCurrentStage()
    {
        if (currentStageConfig != null)
        {
            LoadStage(currentStageConfig.stageID);
        }
    }
}
