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
│   └── RoleSystem/           # 角色系统
│       ├── RoleSystem.cs
│       └── Player.cs
│
├── Interaction/              # 交互模块
│   └── FSM/                  # 状态机
│       ├── IState.cs
│       └── StateMachineBase.cs
│
└── Utils/                    # 工具扩展
    ├── LogExtensions.cs      # 日志扩展
    └── UnityExtensions.cs    # Unity 扩展方法
```

## 架构图

```
GameRoot (单例服务定位器)
    │
    ├─── Services (服务层)
    │    ├── InputService    (输入)
    │    ├── EventService    (事件系统)
    │    ├── ResService      (资源加载/对象池)
    │    ├── AudioService    (音频播放)
    │    ├── TimerService    (定时器)
    │    ├── DataService     (存档管理)
    │    ├── UIService       (UI 窗口管理)
    │    └── ManagerService  (Manager 聚合)
    │
    ├─── Systems (系统层)
    │    └── RoleSystem      (角色管理)
    │
    └─── GameModes (游戏模式)
         └── DefaultGameMode (默认模式)
```

## 快速开始

### 1. 创建 GameRoot

在场景中创建一个空 GameObject，添加 `GameRoot` 组件。GameRoot 会自动设置为 `DontDestroyOnLoad`。

### 2. 访问服务

```csharp
// 获取服务
var inputService = GameRoot.Instance.inputService;
var eventService = GameRoot.Instance.eventService;
var timerService = GameRoot.Instance.timerService;
```

### 3. 使用事件系统

```csharp
// 注册事件
eventService.AddEventListening(EventID.OnHitTarget, OnHitTarget);

// 发送事件
eventService.SendMessage(EventID.OnHitTarget, target, damage);

// 事件处理
private void OnHitTarget(object param1, object param2)
{
    var target = param1 as GameObject;
    var damage = (int)param2;
}
```

### 4. 使用定时器

```csharp
// 添加延迟调用
int timerId = timerService.AddTimer(2000, () => Debug.Log("2秒后执行"));

// 添加循环定时器
int loopId = timerService.AddLoopTimer(TimerType.ScaledTime, 1000, OnTick, null, OnLoopEnd, 5);

// 取消定时器
timerService.RemoveTimer(timerId);
```

### 5. 创建自定义 Manager

```csharp
public class MyManager : ManagerBase
{
    public override void OnAwake()
    {
        base.OnAwake();
        // 初始化逻辑
    }

    public override void OnShow()
    {
        // 每次场景加载时调用
    }
}
```

### 6. 创建自定义游戏模式

```csharp
public class MyGameMode : GameModeBase
{
    public MyGameMode() : base(GameMode.GamePlay) { }

    public override void EnterGameMode()
    {
        base.EnterGameMode();
        // 进入模式时的初始化
    }

    public override void OnUpdate()
    {
        // 每帧更新
    }

    public override void UnOnInit()
    {
        // 退出模式时的清理
    }
}
```

## 扩展指南

### 添加新的 EventID

在 `GameService/EventService/EventService.cs` 中的 `EventID` 枚举添加新的事件类型。

### 添加新的 GameMode

1. 创建继承 `GameModeBase` 的新类
2. 在 `GameRoot.InitGameModes()` 中注册
3. 在 `GameEnum.cs` 中添加对应的枚举值

### 扩展 GameData

在 `DataService.cs` 中的 `GameData` 类添加新的字段来保存游戏数据。

## 依赖项

- Unity 2022.3 LTS 或更高版本
- Unity Input System Package
- **DOTween** - 动画库 (Asset Store 或 OpenUPM)
- **Odin Inspector** - 编辑器增强 (Asset Store)

> 如果不想安装 Odin Inspector，可以移除 GameRoot.cs 中的 `#region Inspector Debug (Odin)` 区域和相关 using 语句。

## 许可

MIT License
