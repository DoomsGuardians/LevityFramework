# LevityFramework

通用 Unity 游戏框架，从 LevityProject 项目中提取的可复用核心架构。

> [!NOTE]
> 本文描述当前代码。已接受但尚未完整实现的目标架构、历史提案和文档状态见 [`Docs/README.md`](Docs/README.md)。

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

## 能力成熟度清单

源码中存在某项能力，不代表它已经是稳定的公共架构。新增项目应优先采用 **Core**；**Toolkit** 需由项目主动选择；**Experimental** 尚未完成生产验证；**Deprecated** 仅用于兼容迁移，不应产生新的调用方。

| 能力 | 成熟度 | 当前使用建议 |
|------|--------|--------------|
| `Levity.Narrative.Core` 契约与 Fake Backend | **Core** | 后端中立的 Narrative Session 公共边界；可直接依赖。 |
| `StageSystem` | **Experimental** | 当前可运行，但异步事务、失败恢复和强类型 Stage ID 尚未落地。 |
| `RoleSystem` | **Experimental** | 仍依赖 `GameRoot` 与枚举角色类型；仅供现有项目迁移验证。 |
| `MonoItemSystem` | **Experimental** | 场景扫描式生命周期尚缺少完整的所有权与回归测试。 |
| `FSM`（`IState` / `StateMachineBase`） | **Toolkit** | 可选的通用状态机工具，不是框架强制的 Game Flow 模型。 |
| `BindableProperty` / `ObservableList` | **Toolkit** | 可按项目选用；不构成统一状态管理方案。 |
| `GenericPool` / `IPoolable` | **Toolkit** | 可选对象池；与资源加载职责保持分离。 |
| `LazySingleton<T>` | **Toolkit** | 仅用于确实需要进程级唯一实例的纯 C# 工具。 |
| `SceneSingleton<T>` | **Experimental** | 自动查找/创建会隐藏场景所有权；新代码优先显式 Composition。 |
| `MonoSingleton<T>` | **Deprecated** | 为 `GameRoot` 等旧入口保留；不要新增基于它的全局服务。 |
| `PersistentSingleton<T>` | **Deprecated** | 自动创建和 `DontDestroyOnLoad` 会隐藏生命周期；仅兼容旧调用。 |
| `UILayer` / `UILayerManager` | **Core** | 当前 UI 分层模型；新窗口使用 `UILayer`。 |
| `WindowLayer` | **Deprecated** | 旧三层枚举，仅由兼容转换使用；新代码改用 `UILayer`。 |
| Command ScriptableObjects | **Experimental** | 现有加载场景效果可用，但尚未形成稳定的公共命令契约。 |

### 已知未消费的 Stage 配置

以下字段当前会显示在 `GameStageConfig` Inspector 中，但 `StageSystem` 不读取它们。填写这些字段不会产生运行时行为；在消费路径实现或迁移完成前，不应把它们作为关卡能力使用。

| 字段 | 当前状态 |
|------|----------|
| `StageConfigItem.RoleConfig` | **未消费**：`RoleSystem` 不会在 Stage 加载时应用该配置。 |
| `StageConfigItem.preLoadItems` | **未消费**：Stage 加载流程不会预加载该资产。 |

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
Assets/Scripts/Levity.Narrative.Core/
├── NarrativeContracts.cs    # 后端中立的叙事会话、结果、并发与保存许可契约
└── FakeNarrativeBackend.cs  # EditMode 测试与 Placeholder Backend 共用实现

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
│   ├── InputService/         # 输入服务与输入通道路由
│   ├── NaninovelService/     # 当前 Naninovel 集成
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

// 3. 带优先级注册（数字越大越先执行）
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
// 下列回调由游戏项目提供。
// doc-lint: ignore OnTimerTick
// doc-lint: ignore OnCancel
// doc-lint: ignore OnLoopEnd
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
// MyWindow 是游戏项目自己的窗口类型。
// doc-lint: ignore MyWindow
// 窗口需要先以名称注册，再显示
uiService.RegisterWindow("settings", settingsWindow);
var window = uiService.ShowWindow<MyWindow>("settings");

// 关闭窗口
uiService.HideWindow("settings");

// 获取已打开的窗口
var window = uiService.GetWindow<MyWindow>("settings");
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

通过派生的项目 Composition Root 注册，不要修改框架的 `GameRoot.Start()`：

```csharp
public sealed class MyGameRoot : GameRoot
{
    protected override void RegisterCustomSystems(List<ILogic> systems)
    {
        systems.Add(new InventorySystem());
    }
}
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
    // ResetBattle / SaveProgress 是游戏项目自己的业务方法。
    // doc-lint: ignore ResetBattle
    // doc-lint: ignore SaveProgress
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
// BattleHUD 是游戏项目自己的窗口类型。
// doc-lint: ignore BattleHUD
public class BattleGameMode : GameModeBase
{
    public BattleGameMode() : base(GameMode.GamePlay) { }

    public override void EnterGameMode()
    {
        base.EnterGameMode();

        // 进入战斗模式
        uIService.ShowWindow<BattleHUD>("battle-hud");

        // 添加定时器
        timerService.AddLoopTimer(TimerType.ScaledTime, 1000, UpdateTimer, null, null, -1);
    }

    public override void OnUpdate()
    {
        // 每帧更新战斗逻辑
        if (inputService.JumpPressed)
        {
            GameRoot.ChangeGameMode(GameMode.Pause);
        }
    }

    public override void UnOnInit()
    {
        // 退出战斗模式时清理
        uIService.HideWindow("battle-hud");
    }

    private void UpdateTimer()
    {
        // 更新游戏计时器
    }
}
```

### 注册和切换 GameMode

```csharp
// MainMenuGameMode / PauseGameMode 是游戏项目自己的模式实现。
// doc-lint: ignore MainMenuGameMode
// doc-lint: ignore PauseGameMode
// 1. 在 GameEnum.cs 中添加枚举值
public enum GameMode
{
    GameStart,
    MainMenu,
    GamePlay,
    Pause,
    GameOver,
}

// 2. 在项目自己的 GameRoot 派生类中注册
public sealed class MyGameRoot : GameRoot
{
    protected override void RegisterCustomGameModes()
    {
        RegisterGameMode(new MainMenuGameMode());
        RegisterGameMode(new BattleGameMode());
        RegisterGameMode(new PauseGameMode());
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
        // 窗口显示时刷新当前服务状态
        volumeSlider.value = audioService.BGMVolume;
    }

    public override void OnHide()
    {
        // 窗口隐藏时执行必要清理
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
        uIService.HideWindow("settings");
    }

    private void OnMusicToggleChanged(Toggle toggle, bool isOn)
    {
        audioService.SetBGMVolume(isOn ? volumeSlider.value : 0f);
    }
}
```

### 使用 UIListener 绑定自定义事件

```csharp
public class ItemSlot : WindowBase
{
    // OnSlotEnter / OnSlotExit 是游戏项目自己的指针事件处理函数。
    // doc-lint: ignore OnSlotEnter
    // doc-lint: ignore OnSlotExit
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

### 添加新的事件

创建实现 `IEvent` 的结构体，通过 `EventBus<T>.Register` 和 `EventBus<T>.Raise` 使用。当前事件系统不再使用 `EventID` 枚举。

### 添加新的 GameMode

1. 创建继承 `GameModeBase` 的新类
2. 在 `GameEnum.cs` 中添加对应的枚举值
3. 在派生 `GameRoot` 的 `RegisterCustomGameModes()` 中调用 `RegisterGameMode()`

### 添加新的 Service

1. 创建实现 `ILogic` 接口的新类
2. 在派生 `GameRoot` 的 `RegisterCustomServices()` 中加入服务列表
3. 若需要全局访问，为该服务显式注册稳定接口；不要继续扩展 `Services.GetFromGameRoot()` 的类型分支

### 添加新的 System

1. 创建实现 `ILogic` 接口的新类
2. 在派生 `GameRoot` 的 `RegisterCustomSystems()` 中加入系统列表

### 扩展 GameData

在 `DataService.cs` 中的 `GameData` 类添加新的字段来保存游戏数据。

---

## 依赖项

- Unity 2022.3 LTS 或更高版本
- Unity Input System Package
- **DOTween** - 动画库 (Asset Store 或 OpenUPM)
- **Odin Inspector** - 当前源码使用其属性和 `SerializedScriptableObject`
- **Naninovel** - 视觉小说引擎 (可选，需要定义 NANINOVEL 宏)

> 当前仓库已经包含 Odin，并在多个类型中使用。移除 Odin 需要替换相关属性和基类，不只是删除 `GameRoot.cs` 的 Inspector 区域。

---

## 许可

MIT License
