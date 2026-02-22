# LevityFramework

通用 Unity 游戏框架，从 LevityProject 项目中提取的可复用核心架构。

## 框架特点

- **服务定位器模式**：通过 GameRoot 单例统一管理所有服务
- **模块化架构**：服务（Service）、系统（System）、管理器（Manager）分层清晰
- **生命周期管理**：统一的 ILogic 和 IMonoLogic 接口
- **事件系统**：支持同步和分帧队列事件
- **定时器系统**：支持多种时间类型（RealTime、ScaledTime、UnscaledTime）
- **UI 窗口管理**：完整的 UI 生命周期和事件绑定
- **状态机系统**：通用的有限状态机实现
- **对象池**：内置对象池支持
- **数据存档**：简单的 JSON 序列化存档系统

---

## 架构概览

```
GameRoot (单例服务定位器)
    │
    ├─── Services (服务层) ─────────────────────────────────────┐
    │    ├── InputService    (输入管理)                         │
    │    ├── EventService    (事件系统)                         │
    │    ├── ResService      (资源加载/对象池)                  │ 全局单例
    │    ├── AudioService    (音频播放)                         │ 跨场景存在
    │    ├── TimerService    (定时器)                           │ 程序启动时初始化
    │    ├── DataService     (存档管理)                         │
    │    ├── UIService       (UI 窗口管理)                      │
    │    └── ManagerService  (Manager 聚合)                     │
    │                                                           │
    ├─── Systems (系统层) ──────────────────────────────────────┤
    │    ├── RoleSystem      (角色管理)                         │ 全局单例
    │    ├── StageSystem     (关卡管理)                         │ 跨场景存在
    │    └── MonoItemSystem  (场景物件管理)                     │
    │                                                           │
    └─── GameModes (游戏模式) ──────────────────────────────────┘
         └── DefaultGameMode (默认模式)

    ┌─── Managers (场景管理器) ────────────────────────────────┐
    │    通过 ManagerService 动态注册                          │ 场景级别
    │    跟随场景生命周期                                       │ 场景切换时重置
    └──────────────────────────────────────────────────────────┘
```

### 层级职责

| 层级 | 生命周期 | 职责 | 示例 |
|------|----------|------|------|
| **Service** | 全局 | 提供基础设施服务，不包含业务逻辑 | 事件、定时器、资源加载 |
| **System** | 全局 | 管理全局游戏数据和跨场景状态 | 角色系统、关卡系统 |
| **Manager** | 场景级 | 处理特定场景的业务逻辑 | 战斗管理器、UI管理器 |
| **GameMode** | 全局 | 控制游戏的整体状态流转 | 主菜单、游戏中、暂停 |

---

## 目录结构

```
Assets/Scripts/Core/
├── GameCommand/              # 核心指令模块
│   ├── Interface/            # 接口定义
│   │   ├── ILogic.cs         # 服务/系统生命周期接口
│   │   └── IMonoLogic.cs     # MonoBehaviour 生命周期接口
│   │
│   ├── GameTool/             # 工具类
│   │   ├── Singleton/        # 单例模式
│   │   ├── BindableProperty/ # 可观察属性
│   │   └── ToolFunction/     # 工具函数
│   │
│   ├── GameConfig/           # 配置和枚举
│   │   └── GameEnum.cs       # 通用枚举定义
│   │
│   ├── GameMode/             # 游戏模式
│   │   └── GameModeBase.cs   # 游戏模式基类
│   │
│   ├── Manager/              # 管理器
│   │   └── ManagerBase.cs    # 管理器基类
│   │
│   ├── Window/               # UI 窗口
│   │   ├── WindowBase.cs     # 窗口基类
│   │   ├── WindowBehaviour.cs
│   │   └── UIListener.cs     # UI 事件监听器
│   │
│   └── GameRoot.cs           # 游戏根节点（单例）
│
├── GameService/              # 服务层
│   ├── EventService/         # 事件服务
│   ├── TimerService/         # 定时器服务
│   ├── UIService/            # UI 服务
│   ├── ResService/           # 资源服务
│   ├── AudioService/         # 音频服务
│   ├── DataService/          # 数据存档服务
│   ├── InputService.cs       # 输入服务
│   └── ManagerService.cs     # Manager 管理服务
│
├── GameSystem/               # 系统层
│   ├── RoleSystem/           # 角色系统
│   ├── StageSystem/          # 关卡系统
│   └── MonoItemSystem/       # 场景物件系统
│
├── Interaction/              # 交互模块
│   └── FSM/                  # 状态机
│       ├── IState.cs
│       └── StateMachineBase.cs
│
└── Utils/                    # 工具扩展
    ├── LogExtensions.cs      # 日志扩展
    ├── DOTweenExtensions.cs  # DOTween 扩展
    └── UnityExtensions.cs    # Unity 扩展方法
```

---

## 快速开始

### 1. 创建 GameRoot

在场景中创建一个空 GameObject，添加 `GameRoot` 组件。GameRoot 会自动设置为 `DontDestroyOnLoad`。

### 2. 访问服务

```csharp
// 获取服务（推荐在 OnAwake/OnInit 中缓存引用）
var gameRoot = GameRoot.Instance;
var inputService = gameRoot.inputService;
var timerService = gameRoot.timerService;
var resService = gameRoot.resService;
var uiService = gameRoot.uIService;
```

---

## Service 服务层详解

Service 是框架的基础设施层，提供通用功能，不包含具体业务逻辑。

### EventBus 泛型事件系统

基于泛型的强类型事件系统，支持优先级、一次性订阅和自动清理。

```csharp
using LevityEvents;

// 1. 定义事件结构体（在 PredefinedEvents.cs 或自定义文件中）
public struct HitTargetEvent : IEvent
{
    public GameObject Attacker;
    public GameObject Target;
    public float Damage;
}

// 2. 注册事件监听
EventBinding<HitTargetEvent> binding = EventBus<HitTargetEvent>.Register(OnHitTarget);

// 3. 带优先级注册（数字越小越先执行）
EventBus<HitTargetEvent>.Register(OnHitTarget, priority: -10);

// 4. 一次性监听（触发一次后自动注销）
EventBus<HitTargetEvent>.RegisterOnce(OnHitTarget);

// 5. 发送事件
EventBus<HitTargetEvent>.Raise(new HitTargetEvent
{
    Attacker = attacker,
    Target = target,
    Damage = 50f
});

// 6. 无参事件
EventBus<InitDoneEvent>.Raise();

// 7. 事件处理函数
private void OnHitTarget(HitTargetEvent e)
{
    Debug.Log($"{e.Attacker.name} hit {e.Target.name} for {e.Damage} damage");
}

// 8. 注销事件
binding.Unregister();
```

### TimerService 定时器服务

支持多种时间类型的定时器系统。

```csharp
// 时间类型
// - TimerType.RealTime: 真实时间，不受 TimeScale 影响
// - TimerType.ScaledTime: 受 TimeScale 影响的游戏时间
// - TimerType.UnscaledTime: 不受 TimeScale 影响的游戏时间

// 1. 添加延迟调用（单位：毫秒）
int timerId = timerService.AddTimer(2000, () => Debug.Log("2秒后执行"));

// 2. 添加指定时间类型的定时器
int timerId = timerService.AddTimer(TimerType.ScaledTime, 1000, OnTimerTick);

// 3. 添加循环定时器（执行5次）
int loopId = timerService.AddLoopTimer(
    TimerType.ScaledTime,  // 时间类型
    1000,                  // 间隔（毫秒）
    OnTick,                // 每次回调
    OnCancel,              // 取消回调（可选）
    OnLoopEnd,             // 循环结束回调（可选）
    5                      // 循环次数
);

// 4. 控制定时器
timerService.RemoveTimer(timerId);           // 移除
timerService.StopTimer(timerId);             // 暂停
timerService.EnableTimer(timerId);           // 恢复
timerService.AdjustTimer(timerId, 500);      // 调整时间（+500ms）
int remaining = timerService.QueryRemaining(timerId);  // 查询剩余时间
```

### UIService UI 服务

管理 UI 窗口的打开、关闭和层级。

```csharp
// 打开窗口
var window = uiService.OpenWindow<MyWindow>(WindowLayer.Normal);

// 关闭窗口
uiService.CloseWindow<MyWindow>();

// 获取已打开的窗口
var window = uiService.GetWindow<MyWindow>();
```

### ManagerService 管理器服务

动态注册和管理场景级别的 Manager。

```csharp
// 注册 Manager（通常在 Manager 的 Awake 中调用）
GameRoot.Instance.managerService.RegisterManager(this);

// 获取其他 Manager
var battleManager = managerService.GetManager<BattleManager>();

// 场景退出时通知所有 Manager
managerService.OnSceneExit();

// 清空所有 Manager
managerService.ClearAllManagers();
```

---

## System 系统层详解

System 管理全局游戏数据，跨场景持久存在。

### 创建自定义 System

```csharp
public class InventorySystem : ILogic
{
    private Dictionary<string, int> items = new Dictionary<string, int>();

    public void OnInit()
    {
        // 程序启动时初始化
        items.Clear();
    }

    public void OnEnterState()
    {
        // 每次进入场景时调用
    }

    public void OnUpdate()
    {
        // 每帧更新（如果需要）
    }

    public void UnInit()
    {
        // 程序退出时清理
    }

    // 自定义方法
    public void AddItem(string itemId, int count)
    {
        if (items.ContainsKey(itemId))
            items[itemId] += count;
        else
            items[itemId] = count;
    }

    public int GetItemCount(string itemId)
    {
        return items.TryGetValue(itemId, out var count) ? count : 0;
    }
}
```

在 GameRoot.cs 中注册：

```csharp
// 在 Start() 的系统初始化区域添加
public InventorySystem inventorySystem;

// 在 #region 初始化系统模块 中
inventorySystem = new InventorySystem();
systemList.Add(inventorySystem);
```

### RoleSystem 角色系统

管理玩家角色的注册、获取和卸载。

```csharp
// 注册玩家
roleSystem.RegisterPlayer("player_1", playerInstance);

// 设置当前玩家
roleSystem.SetCurrentPlayer("player_1");

// 获取当前玩家
var player = roleSystem.CurrentPlayer;

// 获取指定玩家
var player = roleSystem.GetPlayer("player_1");

// 获取所有玩家
var allPlayers = roleSystem.GetAllPlayers();

// 卸载玩家
roleSystem.UnloadPlayer("player_1");
roleSystem.UnloadAllPlayers();
```

---

## Manager 管理器层详解

Manager 处理特定场景的业务逻辑，生命周期跟随场景。

### 创建自定义 Manager

```csharp
using LevityEvents;

public class BattleManager : ManagerBase
{
    private int score;
    private bool isPaused;
    private EventBinding<HitTargetEvent> hitBinding;

    /// <summary>初始化时调用一次（注册后立即调用）</summary>
    public override void OnAwake()
    {
        base.OnAwake();  // 重要：调用基类以注入服务引用

        // 初始化逻辑
        score = 0;
        isPaused = false;

        // 注册事件
        hitBinding = EventBus<HitTargetEvent>.Register(OnHitTarget);
    }

    /// <summary>每次场景加载/切换时调用</summary>
    public override void OnShow()
    {
        // 场景显示时的逻辑
        ResetBattle();
    }

    /// <summary>场景退出时调用</summary>
    public override void OnExit()
    {
        // 场景退出时的清理
        SaveProgress();
    }

    /// <summary>Manager 注销时调用</summary>
    public override void UnInit()
    {
        // 最终清理，移除事件监听等
        hitBinding?.Unregister();
    }

    // 自定义方法
    private void OnHitTarget(HitTargetEvent e)
    {
        score += (int)e.Damage;
    }

    public void PauseBattle()
    {
        isPaused = true;
        gameRoot.CancelInput();  // 使用注入的 gameRoot
    }

    public void ResumeBattle()
    {
        isPaused = false;
        gameRoot.ResetInput();
    }
}
```

### Manager 的注册方式

**方式一：在场景中作为组件**

```csharp
public class BattleManager : ManagerBase
{
    private void Awake()
    {
        // 自动注册到 ManagerService
        GameRoot.Instance.managerService.RegisterManager(this);
    }
}
```

**方式二：在 GameMode 中动态创建**

```csharp
public class BattleGameMode : GameModeBase
{
    public override void EnterGameMode()
    {
        base.EnterGameMode();

        // 创建并注册 Manager
        var go = new GameObject("BattleManager");
        var manager = go.AddComponent<BattleManager>();
        managerService.RegisterManager(manager);
    }
}
```

---

## GameMode 游戏模式详解

GameMode 控制游戏的整体状态流转。

### 创建自定义 GameMode

```csharp
public class BattleGameMode : GameModeBase
{
    public BattleGameMode() : base(GameMode.GamePlay) { }

    public override void EnterGameMode()
    {
        base.EnterGameMode();

        // 进入战斗模式
        uIService.OpenWindow<BattleHUD>(WindowLayer.Normal);

        // 添加定时器
        timerService.AddLoopTimer(TimerType.ScaledTime, 1000, UpdateTimer, null, null, -1);
    }

    public override void OnUpdate()
    {
        // 每帧更新战斗逻辑
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GameRoot.ChangeGameMode(GameMode.Pause);
        }
    }

    public override void UnOnInit()
    {
        // 退出战斗模式时清理
        uIService.CloseWindow<BattleHUD>();
    }

    private void UpdateTimer()
    {
        // 更新游戏计时器
    }
}
```

### 注册和切换 GameMode

```csharp
// 1. 在 GameEnum.cs 中添加枚举值
public enum GameMode
{
    GameStart,
    MainMenu,
    GamePlay,
    Pause,
    GameOver,
}

// 2. 在 GameRoot.InitGameModes() 中注册
private void InitGameModes()
{
    RegisterGameMode(new DefaultGameMode());
    RegisterGameMode(new MainMenuGameMode());
    RegisterGameMode(new BattleGameMode());
    RegisterGameMode(new PauseGameMode());

    // 设置初始模式
    if (GameModeDic.TryGetValue(GameMode.GameStart, out var defaultMode))
    {
        currentGameMode = defaultMode;
        currentGameMode.EnterGameMode();
    }
}

// 3. 切换游戏模式
GameRoot.Instance.ChangeGameMode(GameMode.GamePlay);
```

---

## UI 窗口系统

### 创建自定义窗口

```csharp
public class SettingsWindow : WindowBase
{
    [SerializeField] private Button closeButton;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle musicToggle;

    public override void OnAwake()
    {
        base.OnAwake();  // 重要：注入服务引用

        // 绑定按钮事件
        AddButtonListener(closeButton, OnCloseClick);

        // 绑定 Toggle 事件
        AddToggleListener(musicToggle, OnMusicToggleChanged);
    }

    public override void OnShow()
    {
        // 窗口显示时刷新数据
        volumeSlider.value = dataService.GameData.volume;
        musicToggle.isOn = dataService.GameData.musicEnabled;
    }

    public override void OnHide()
    {
        // 窗口隐藏时保存数据
        dataService.SaveGameData();
    }

    public override void OnUpdate()
    {
        // 每帧更新（如果需要）
    }

    public override void OnDestroy()
    {
        // 窗口销毁时清理
        base.OnDestroy();  // 自动移除所有监听
    }

    private void OnCloseClick()
    {
        uIService.CloseWindow<SettingsWindow>();
    }

    private void OnMusicToggleChanged(Toggle toggle, bool isOn)
    {
        audioService.SetMusicEnabled(isOn);
    }
}
```

### 使用 UIListener 绑定自定义事件

```csharp
public class ItemSlot : WindowBase
{
    [SerializeField] private UIListener slotListener;

    public override void OnAwake()
    {
        base.OnAwake();

        // 绑定点击事件
        OnClick(slotListener, OnSlotClick, "itemId_001");

        // 绑定拖拽事件
        OnDrag(slotListener, OnSlotDrag);

        // 绑定鼠标进入/离开
        OnEnter(slotListener, OnSlotEnter);
        OnExit(slotListener, OnSlotExit);
    }

    private void OnSlotClick(PointerEventData eventData, UIListener listener, object[] args)
    {
        string itemId = args[0] as string;
        Debug.Log($"Clicked item: {itemId}");
    }

    private void OnSlotDrag(PointerEventData eventData, UIListener listener, object[] args)
    {
        // 处理拖拽
    }
}
```

---

## 生命周期图

```
程序启动
    │
    ▼
GameRoot.Start()
    │
    ├── Service.OnInit() ──────────────► 所有服务初始化
    │
    ├── System.OnInit() ───────────────► 所有系统初始化
    │
    └── GameMode.EnterGameMode() ──────► 进入初始游戏模式

每帧更新
    │
    ▼
GameRoot.Update()
    │
    ├── GameMode.OnUpdate()
    ├── Service.OnUpdate()
    └── System.OnUpdate()

场景切换
    │
    ▼
GameRoot.OnEnterState()
    │
    ├── Service.OnEnterState()
    ├── System.OnEnterState()
    └── Manager.OnShow() ──────────────► 通过 ManagerService

程序退出
    │
    ▼
GameRoot.OnApplicationQuit()
    │
    ├── System.UnInit() (逆序)
    └── Service.UnInit() (逆序)
```

---

## 输入控制

框架提供了输入通道系统，用于在不同状态下控制输入。

```csharp
// 锁定所有输入（如：过场动画）
gameRoot.CancelInput();

// 恢复输入
gameRoot.ResetInput();

// 锁定特定通道
gameRoot.LockInputChannel(InputChannel.Gameplay, this);
gameRoot.LockInputChannel(InputChannel.UI, this);

// 解锁特定通道
gameRoot.UnlockInputChannel(InputChannel.Gameplay, this);

// 检查通道是否可用
if (gameRoot.IsInputChannelAvailable(InputChannel.Gameplay))
{
    // 处理游戏输入
}
```

---

## 扩展指南

### 添加新的 EventID

在 `GameService/EventService/EventService.cs` 中的 `EventID` 枚举添加新的事件类型。

### 添加新的 GameMode

1. 创建继承 `GameModeBase` 的新类
2. 在 `GameRoot.InitGameModes()` 中注册
3. 在 `GameEnum.cs` 中添加对应的枚举值

### 添加新的 Service

1. 创建实现 `ILogic` 接口的新类
2. 在 `GameRoot.Start()` 中实例化并添加到 `serviceList`
3. 在 `GameRoot` 中添加公开字段以供访问

### 添加新的 System

1. 创建实现 `ILogic` 接口的新类
2. 在 `GameRoot.Start()` 中实例化并添加到 `systemList`
3. 在 `GameRoot` 中添加公开字段以供访问

### 扩展 GameData

在 `DataService.cs` 中的 `GameData` 类添加新的字段来保存游戏数据。

---

## 依赖项

- Unity 2022.3 LTS 或更高版本
- Unity Input System Package
- **DOTween** - 动画库 (Asset Store 或 OpenUPM)
- **Odin Inspector** - 编辑器增强 (Asset Store)
- **Naninovel** - 视觉小说引擎 (可选，需要定义 NANINOVEL 宏)

> 如果不想安装 Odin Inspector，可以移除 GameRoot.cs 中的 `#region Inspector Debug (Odin)` 区域和相关 using 语句。

---

## 许可

MIT License
