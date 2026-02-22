# LevityFramework 优化方案

## 一、代码结构优化

### 1.1 当前问题

| 问题 | 影响 | 优先级 |
|------|------|--------|
| Utils 文件夹扁平化 | 文件多时难以查找 | 中 |
| EventService 双轨制不统一 | 新手学习成本高 | 中 |
| MonoSingleton 功能单一 | 缺少变体（如非持久单例） | 低 |
| Interface 位置分散 | ILogic/IMonoLogic 在 GameCommand | 低 |

### 1.2 建议的目录重构

```
Core/
├── Architecture/              # 架构基础（重命名自 GameCommand）
│   ├── Bootstrap/             # 启动引导
│   │   └── GameRoot.cs
│   ├── Config/                # 配置
│   │   ├── GameEnum.cs
│   │   └── GameResPathConfig.cs
│   ├── Interfaces/            # 核心接口（统一位置）
│   │   ├── ILogic.cs
│   │   ├── IMonoLogic.cs
│   │   └── IState.cs
│   ├── Patterns/              # 设计模式
│   │   ├── Singleton/
│   │   │   ├── MonoSingleton.cs
│   │   │   └── PersistentSingleton.cs  # 新增
│   │   └── Observable/
│   │       ├── BindableProperty.cs
│   │       └── ObservableList.cs
│   └── ScriptableObjects/     # SO 配置
│
├── Services/                  # 服务（重命名自 GameService）
│   └── ...
│
├── Systems/                   # 系统（重命名自 GameSystem）
│   └── ...
│
├── UI/                        # UI 模块（从 GameCommand 提取）
│   ├── Base/
│   │   ├── WindowBase.cs
│   │   ├── WindowBehaviour.cs
│   │   └── UIListener.cs
│   ├── Components/
│   └── Managers/
│
├── Gameplay/                  # 游戏玩法（从各处提取）
│   ├── GameMode/
│   ├── Manager/
│   └── FSM/
│
└── Utils/                     # 工具库（分类整理）
    ├── Extensions/
    │   ├── ColorExtensions.cs
    │   ├── ListExtensions.cs
    │   ├── TransformExtensions.cs
    │   ├── UnityExtensions.cs
    │   └── VectorExtensions.cs
    ├── Coroutines/
    │   └── WaitFor.cs
    ├── DOTween/
    │   └── DOTweenExtensions.cs
    └── Debug/
        └── LogExtensions.cs
```

---

## 二、功能增强建议

### 2.1 单例系统增强

```csharp
// 新增：非持久单例（场景切换销毁）
public class SceneSingleton<T> : MonoBehaviour where T : Component
{
    // 不使用 DontDestroyOnLoad
}

// 新增：惰性单例（首次访问时创建）
public class LazySingleton<T> where T : class, new()
{
    private static readonly Lazy<T> instance = new Lazy<T>(() => new T());
    public static T Instance => instance.Value;
}
```

### 2.2 对象池系统增强

```csharp
// 当前：简单对象池
// 建议：增强版对象池
public interface IPoolable
{
    void OnSpawn();      // 从池中取出时
    void OnDespawn();    // 返回池中时
}

public class ObjectPool<T> where T : class, IPoolable
{
    public int ActiveCount { get; }
    public int AvailableCount { get; }
    public int TotalCount { get; }

    public T Get();
    public void Return(T item);
    public void Prewarm(int count);      // 预热
    public void Trim(int maxIdle);       // 修剪空闲对象
    public void Clear();
}
```

### 2.3 依赖注入增强

```csharp
// 当前：手动注入
// 建议：特性注入
[Inject] private AudioService audioService;
[Inject] private UIService uiService;

// ServiceLocator 增强
public static class Services
{
    public static T Get<T>() where T : class, ILogic;
    public static void Register<T>(T service) where T : class, ILogic;
    public static bool TryGet<T>(out T service) where T : class, ILogic;
}
```

### 2.4 事件系统统一

```csharp
// 建议：统一为泛型事件，废弃枚举事件
// 提供迁移指南：
// EventID.OnGamePlayOver → GamePlayOverEvent
// EventID.OnHitTarget → HitTargetEvent

// 新增：事件优先级
EventBus<T>.Register(handler, priority: 10);

// 新增：一次性事件
EventBus<T>.RegisterOnce(handler);

// 新增：条件过滤
EventBus<T>.Register(handler, filter: e => e.Damage > 10);
```

### 2.5 异步支持增强

```csharp
// 当前：协程为主
// 建议：增加 async/await 支持

public class AsyncResService
{
    public async Task<T> LoadAsync<T>(string path);
    public async Task LoadSceneAsync(string sceneName, Action<float> onProgress = null);
}

public class AsyncUIService
{
    public async Task<T> ShowAsync<T>() where T : WindowBase;
    public async Task HideAsync<T>() where T : WindowBase;
}

// 定时器 async 支持
public static class TimerExtensions
{
    public static async Task WaitAsync(float seconds);
    public static async Task WaitUntilAsync(Func<bool> condition);
}
```

---

## 三、性能优化建议

### 3.1 内存优化

| 优化点 | 当前状态 | 建议 |
|--------|----------|------|
| 字符串拼接 | 部分使用 + | 统一使用 StringBuilder 或插值 |
| 委托分配 | 每次创建新委托 | 缓存委托实例 |
| 临时数组 | 频繁创建 | 使用 ArrayPool<T> |
| LINQ 使用 | 部分热路径使用 | 热路径改用 for 循环 |

```csharp
// 示例：委托缓存
public class EventOptimized
{
    private static readonly Dictionary<object, Action> cachedActions = new();

    public static Action GetCached(object target, Action action)
    {
        if (!cachedActions.TryGetValue(target, out var cached))
        {
            cached = action;
            cachedActions[target] = cached;
        }
        return cached;
    }
}
```

### 3.2 Update 优化

```csharp
// 当前：每个服务独立 Update
// 建议：统一 Update 管理器

public class UpdateManager : MonoBehaviour
{
    private readonly List<IUpdatable> updatables = new();
    private readonly List<IFixedUpdatable> fixedUpdatables = new();
    private readonly List<ILateUpdatable> lateUpdatables = new();

    // 分帧更新（避免单帧卡顿）
    public void RegisterThrottled(IUpdatable updatable, int frameInterval);
}
```

### 3.3 UI 优化

```csharp
// 建议增加：
// 1. Canvas 分组管理（减少 Rebuild）
// 2. UI 对象池（频繁开关的窗口）
// 3. 文本本地化缓存

public class UIPoolService
{
    public T GetWindow<T>() where T : WindowBase;
    public void ReturnWindow<T>(T window) where T : WindowBase;
    public void PrewarmWindow<T>(int count) where T : WindowBase;
}
```

---

## 四、新功能建议

### 4.1 配置系统增强

```csharp
// 运行时配置热重载
public interface IConfigurable
{
    void OnConfigChanged();
}

public class ConfigService
{
    public T GetConfig<T>(string key);
    public void SetConfig<T>(string key, T value);
    public void ReloadAll();

    public event Action<string> OnConfigChanged;
}
```

### 4.2 命令系统

```csharp
// 支持撤销/重做
public interface ICommand
{
    void Execute();
    void Undo();
}

public class CommandService
{
    public void Execute(ICommand command);
    public void Undo();
    public void Redo();
    public bool CanUndo { get; }
    public bool CanRedo { get; }
}
```

### 4.3 状态持久化增强

```csharp
// 云存档支持
public interface ISaveProvider
{
    Task SaveAsync(string key, byte[] data);
    Task<byte[]> LoadAsync(string key);
    Task DeleteAsync(string key);
}

public class CloudSaveProvider : ISaveProvider { }
public class LocalSaveProvider : ISaveProvider { }

public class SaveService
{
    public ISaveProvider Provider { get; set; }
    public async Task SaveAsync<T>(string key, T data);
    public async Task<T> LoadAsync<T>(string key);
}
```

### 4.4 调试系统增强

```csharp
// 运行时控制台
public class DebugConsole : MonoSingleton<DebugConsole>
{
    public void RegisterCommand(string name, Action<string[]> handler);
    public void Log(string message, LogLevel level);
    public void ShowOverlay(string key, Func<string> valueGetter);
}

// 性能监控
public class PerformanceMonitor
{
    public float FPS { get; }
    public long MemoryUsage { get; }
    public int DrawCalls { get; }
    public int Triangles { get; }
}
```

---

## 五、实施优先级

### Phase 1（立即可做）
- [ ] Utils 分类整理
- [ ] 添加 PersistentSingleton / SceneSingleton
- [ ] 对象池接口增强（IPoolable）
- [ ] 事件系统优先级支持

### Phase 2（中期）
- [ ] 目录结构重构
- [ ] async/await 支持
- [ ] Update 管理器优化
- [ ] UI 对象池

### Phase 3（长期）
- [ ] 依赖注入框架
- [ ] 命令系统（撤销/重做）
- [ ] 云存档支持
- [ ] 调试控制台

---

## 六、迁移指南

### 从旧版迁移

```csharp
// WaitForSecondsCache → WaitFor
// 旧
yield return WaitForSecondsCache.Get(1f);
// 新（推荐）
yield return WaitFor.Seconds(1f);

// 枚举事件 → 泛型事件
// 旧
eventService.AddEventListening(EventID.OnGamePlayOver, handler);
eventService.SendMessage(EventID.OnGamePlayOver, data);
// 新（推荐）
var binding = EventBus<GamePlayOverEvent>.Register(handler);
EventBus<GamePlayOverEvent>.Raise(new GamePlayOverEvent { Data = data });
```
