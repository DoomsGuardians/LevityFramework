# LevityFramework Core

Unity 通用游戏框架核心模块，提供完整的游戏开发基础设施。

## 目录结构

```
Core/
├── GameCommand/          # 游戏指令模块（核心架构）
│   ├── GameConfig/       # 全局配置（枚举、路径）
│   ├── GameMode/         # 游戏模式基类
│   ├── GameRoot.cs       # 框架入口（服务定位器）
│   ├── GameSO/           # ScriptableObject 配置
│   │   ├── LoadSceneFX/  # 场景转场特效
│   │   └── Stage/        # 关卡配置
│   ├── GameTool/         # 工具类
│   │   ├── BindableProperty/  # 数据绑定
│   │   ├── Singleton/    # 单例基类
│   │   └── ToolFunction/ # 通用工具函数
│   ├── Interface/        # 核心接口
│   ├── Manager/          # 管理器基类
│   └── Window/           # UI 窗口系统
│
├── GameService/          # 核心服务模块（8个服务）
│   ├── AudioService/     # 音频管理
│   ├── DataService/      # 数据存档
│   ├── EventService/     # 事件系统
│   │   └── GenericEvents/# 泛型事件总线（推荐）
│   ├── InputService/     # 输入管理
│   ├── ResService/       # 资源加载与对象池
│   ├── TimerService/     # 定时器系统
│   │   └── OOP/          # OOP 风格定时器
│   ├── UIService/        # UI 管理
│   │   └── Components/   # UI 组件
│   └── ManagerService.cs # Manager 聚合服务
│
├── GameSystem/           # 游戏系统模块（3个系统）
│   ├── MonoItemSystem/   # MonoBehaviour 管理
│   ├── RoleSystem/       # 角色系统
│   └── StageSystem/      # 关卡系统
│
├── Interaction/          # 交互模块
│   └── FSM/              # 有限状态机
│
└── Utils/                # 工具扩展库
    ├── ColorExtensions.cs
    ├── DOTweenExtensions.cs
    ├── ListExtensions.cs
    ├── LogExtensions.cs
    ├── TransformExtensions.cs
    ├── UnityExtensions.cs
    ├── VectorExtensions.cs
    └── WaitForSecondsCache.cs
```

## 架构概览

```
┌─────────────────────────────────────────────────────────────┐
│                        GameRoot                              │
│                    (服务定位器/单例)                          │
├─────────────────────────────────────────────────────────────┤
│  服务层 (Services)              │  系统层 (Systems)          │
│  ├── AudioService               │  ├── RoleSystem            │
│  ├── DataService                │  ├── StageSystem           │
│  ├── EventService               │  └── MonoItemSystem        │
│  ├── InputService               │                            │
│  ├── ResService                 ├──────────────────────────────│
│  ├── TimerService               │  内容层 (Content)           │
│  ├── UIService                  │  ├── GameMode              │
│  └── ManagerService             │  ├── Manager               │
│                                 │  ├── Window                │
│                                 │  └── Player                │
├─────────────────────────────────────────────────────────────┤
│                      Utils (工具扩展库)                       │
└─────────────────────────────────────────────────────────────┘
```

## 核心服务说明

| 服务 | 职责 | 关键 API |
|------|------|----------|
| **AudioService** | BGM/SFX 管理 | `PlayBGM()`, `PlaySFX()`, `SetVolume()` |
| **DataService** | 数据存档 | `Save<T>()`, `Load<T>()`, `Delete()` |
| **EventBus<T>** | 泛型事件（推荐） | `Register()`, `Raise()`, `RegisterOnce()` |
| **InputService** | 输入处理 | `MoveInput`, `JumpPressed`, `Enable()` |
| **ResService** | 资源/对象池 | `Load<T>()`, `GetFromPool()`, `ReturnToPool()` |
| **TimerService** | 定时器 | `AddTimer()`, `RemoveTimer()`, `AdjustTimer()` |
| **UIService** | UI 管理 | `Show<T>()`, `Hide<T>()`, `Get<T>()` |

## 快速开始

### 1. 访问服务

```csharp
// 方式一：通过 Services 静态类（推荐）
var audio = Services.Audio;
var ui = Services.UI;

// 方式二：通过 GameRoot
var audioService = GameRoot.Instance.audioService;

// 方式三：在 WindowBase/ManagerBase 中已自动注入
public class MyWindow : WindowBase
{
    void Start()
    {
        audioService.PlaySFX(clickSound);
    }
}
```

### 2. 事件系统（泛型 EventBus）

```csharp
using LevityEvents;

// 1. 使用预定义事件
var binding = EventBus<PlayerDamagedEvent>.Register(OnPlayerDamaged);
EventBus<PlayerDamagedEvent>.Raise(new PlayerDamagedEvent { Damage = 10 });
binding.Dispose();

// 2. 定义自定义事件
public struct MyCustomEvent : IEvent
{
    public string Message;
    public int Value;
}

// 3. 注册（支持优先级，数值越大越先执行）
var binding = EventBus<MyCustomEvent>.Register(OnMyEvent, priority: 10);

// 4. 一次性事件（触发一次后自动注销）
EventBus<MyCustomEvent>.RegisterOnce(OnMyEventOnce);

// 5. 触发事件
EventBus<MyCustomEvent>.Raise(new MyCustomEvent { Message = "Hello" });
```

### 3. UI 窗口

```csharp
public class SettingsWindow : WindowBase
{
    public override void OnAwake()
    {
        base.OnAwake();
        // 初始化
    }

    public override void OnShow()
    {
        base.OnShow();
        // 显示时调用
    }
}

// 显示窗口
uiService.Show<SettingsWindow>();

// 隐藏窗口
uiService.Hide<SettingsWindow>();
```

### 4. 定时器

```csharp
// 服务式定时器
int timerId = timerService.AddTimer(1000, OnComplete, OnCancel, count: 3);
timerService.RemoveTimer(timerId);

// OOP 定时器
var countdown = new CountdownTimer(5f);
countdown.OnTimerStop += () => Debug.Log("完成!");
countdown.Start();

var stopwatch = new StopwatchTimer();
stopwatch.Start();
Debug.Log(stopwatch.GetFormattedTime()); // "00:05.32"
```

### 5. 协程等待缓存

```csharp
// 推荐方式（无 GC）
yield return WaitFor.Seconds(1f);
yield return WaitFor.SecondsRealtime(0.5f);
yield return WaitFor.FixedUpdate;
yield return WaitFor.EndOfFrame;

// 兼容旧 API
yield return WaitForSecondsCache.Get(1f);
```

### 6. 扩展方法

```csharp
// Vector
pos = pos.With(y: 0);
pos = pos.RandomPointInAnnulus(5f, 10f);
bool inRange = pos.InRangeOf(target, 10f);

// Transform
transform.LookAtY(target);
transform.ForEachChild(child => child.gameObject.SetActive(false));
var path = transform.GetPath();

// List
list.Shuffle();
var item = list.GetRandom();
if (list.IsNullOrEmpty()) { }

// Color
color = color.WithAlpha(0.5f);
color = color.Blend(Color.red, 0.5f);
string hex = color.ToHex();

// GameObject
obj.OrNull()?.DoSomething();
component.SetActive().DoSomething(); // 链式调用
```

## UI 层级系统

```
Layer        Order 范围    用途
─────────────────────────────────────
Scene        0-99         场景 UI
Background   100-199      背景层
Normal       200-299      普通窗口
Info         300-399      信息提示
Top          400-499      顶层窗口
Tip          500-599      提示/Toast
```

## 生命周期

### ILogic (服务/系统)
```
OnInit() → OnEnterState() → OnUpdate() → UnInit()
```

### IMonoLogic (Manager/Window)
```
OnAwake() → OnShow() → OnExit() → UnInit()
```

### GameMode
```
EnterGameMode() → StartGame() → OnUpdate() → UnOnInit()
```

## 依赖项

- **DOTween**: 动画系统
- **Odin Inspector** (可选): 编辑器增强
- **Unity Input System**: 输入处理
- **Naninovel** (可选): 对话系统

## 版本历史

- v1.0: 基础框架
- v1.1: 新增 OOP Timer、扩展方法增强、WaitFor 缓存重构
- v1.2: 事件系统重构为单轨泛型设计、Services 快捷访问、对象池增强、单例系统扩展
