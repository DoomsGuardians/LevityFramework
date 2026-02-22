# Naninovel Integration Enhancement - Development Roadmap

## 1. 概述

### 1.1 当前实现状态

LevityFramework 已实现基础的 Naninovel 集成，包括：
- 服务初始化与生命周期管理
- 基本的对话播放功能
- 简单的事件回调机制

### 1.2 功能成熟度矩阵

| 模块 | 当前状态 | 目标状态 | 差距评估 |
|------|----------|----------|----------|
| 核心增强 | 基础 | 完善 | 需要事件系统和参数传递 |
| 存档系统 | 未集成 | 完整集成 | 需要完整实现 |
| UI集成 | 默认 | 可定制 | 需要桥接层 |
| 设置系统 | 未集成 | 双向同步 | 需要完整实现 |
| 演出系统 | 基础 | 扩展 | 需要封装层 |
| 本地化 | 未集成 | 完整支持 | 需要完整实现 |
| 调试工具 | 无 | 完整 | 需要完整实现 |
| 高级功能 | 无 | 预留接口 | 需要设计 |

### 1.3 优先级说明

- **P0 (必须)**: 核心功能，没有则无法正常使用
- **P1 (重要)**: 提升体验的关键功能
- **P2 (一般)**: 有则更好，可延后实现
- **P3 (低)**: 未来扩展，预留接口即可

---

## 2. 核心增强模块

### 2.1 选择系统事件集成

**状态**: [ ] TODO
**优先级**: P0 (必须)
**复杂度**: 中 (3-5天)
**依赖**: 无

#### 功能描述
将 Naninovel 的选项选择事件与游戏逻辑系统对接，支持在选择发生时触发游戏侧逻辑（如好感度变化、解锁内容等）。

#### 接口设计
```csharp
/// <summary>
/// 选择系统事件服务接口
/// </summary>
public interface IChoiceEventService
{
    /// <summary>
    /// 当玩家做出选择时触发
    /// </summary>
    event Action<ChoiceEventArgs> OnChoiceMade;

    /// <summary>
    /// 当选项即将显示时触发（可用于动态过滤选项）
    /// </summary>
    event Action<ChoiceDisplayEventArgs> OnChoiceDisplaying;

    /// <summary>
    /// 注册选择结果处理器
    /// </summary>
    void RegisterHandler(string choiceId, Action<int> handler);

    /// <summary>
    /// 移除选择结果处理器
    /// </summary>
    void UnregisterHandler(string choiceId);
}

public class ChoiceEventArgs
{
    public string ChoiceId { get; set; }
    public int SelectedIndex { get; set; }
    public string SelectedText { get; set; }
    public string ScriptName { get; set; }
    public int LineIndex { get; set; }
}

public class ChoiceDisplayEventArgs
{
    public string ChoiceId { get; set; }
    public List<ChoiceOption> Options { get; set; }
    public bool Cancel { get; set; }
}

public class ChoiceOption
{
    public string Text { get; set; }
    public bool IsVisible { get; set; }
    public bool IsEnabled { get; set; }
    public string CustomData { get; set; }
}
```

#### 使用示例
```csharp
public class RelationshipManager : MonoBehaviour
{
    [Inject] private IChoiceEventService _choiceService;

    private void Start()
    {
        _choiceService.OnChoiceMade += HandleChoice;

        // 注册特定选择点的处理器
        _choiceService.RegisterHandler("date_location", OnDateLocationChosen);
    }

    private void HandleChoice(ChoiceEventArgs args)
    {
        // 根据选择ID和索引处理好感度变化
        if (args.ChoiceId.StartsWith("affection_"))
        {
            var characterId = args.ChoiceId.Replace("affection_", "");
            var delta = GetAffectionDelta(args.SelectedIndex);
            AffectionSystem.AddAffection(characterId, delta);
        }
    }

    private void OnDateLocationChosen(int selectedIndex)
    {
        // 处理约会地点选择
        var location = selectedIndex switch
        {
            0 => "cafe",
            1 => "park",
            2 => "cinema",
            _ => "default"
        };
        GameEvents.Trigger("DateLocationSelected", location);
    }
}
```

#### 实现要点
- 监听 `Naninovel.ChoiceHandlerPanel` 的选择事件
- 通过 `CustomVariableManager` 同步选择结果
- 支持选择ID的命名约定以便自动路由
- 考虑异步选择场景（超时、外部取消）

#### 验证方法
- 创建包含多个选项的测试脚本
- 验证选择事件正确触发
- 验证好感度等游戏数据正确变化

---

### 2.2 对话队列系统

**状态**: [ ] TODO
**优先级**: P1 (重要)
**复杂度**: 高 (1-2周)
**依赖**: 无

#### 功能描述
实现对话任务的队列管理，支持多个对话请求的有序执行、优先级排序、中断恢复等功能。

#### 接口设计
```csharp
/// <summary>
/// 对话队列管理服务
/// </summary>
public interface IDialogueQueueService
{
    /// <summary>
    /// 当前队列中的任务数量
    /// </summary>
    int QueueCount { get; }

    /// <summary>
    /// 当前是否有对话正在播放
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// 将对话任务加入队列
    /// </summary>
    void Enqueue(DialogueTask task);

    /// <summary>
    /// 将高优先级对话插入队首
    /// </summary>
    void EnqueuePriority(DialogueTask task);

    /// <summary>
    /// 清空队列（不影响当前播放）
    /// </summary>
    void ClearQueue();

    /// <summary>
    /// 中断当前对话并清空队列
    /// </summary>
    UniTask InterruptAll();

    /// <summary>
    /// 暂停队列处理
    /// </summary>
    void Pause();

    /// <summary>
    /// 恢复队列处理
    /// </summary>
    void Resume();

    /// <summary>
    /// 队列状态变化事件
    /// </summary>
    event Action<QueueStateChangedEventArgs> OnQueueStateChanged;
}

public class DialogueTask
{
    public string ScriptName { get; set; }
    public string Label { get; set; }
    public Dictionary<string, string> Parameters { get; set; }
    public DialoguePriority Priority { get; set; }
    public Action OnComplete { get; set; }
    public Action OnInterrupted { get; set; }
}

public enum DialoguePriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3  // 会中断当前对话
}

public class QueueStateChangedEventArgs
{
    public DialogueTask CurrentTask { get; set; }
    public int RemainingCount { get; set; }
    public QueueState State { get; set; }
}

public enum QueueState
{
    Idle,
    Playing,
    Paused,
    Interrupted
}
```

#### 使用示例
```csharp
public class NPCInteractionHandler : MonoBehaviour
{
    [Inject] private IDialogueQueueService _dialogueQueue;

    public void OnNPCClicked(string npcId)
    {
        // 普通NPC对话加入队列
        _dialogueQueue.Enqueue(new DialogueTask
        {
            ScriptName = $"NPC/{npcId}",
            Label = "greeting",
            Priority = DialoguePriority.Normal,
            OnComplete = () => Debug.Log($"Finished talking to {npcId}")
        });
    }

    public void TriggerUrgentEvent(string eventId)
    {
        // 紧急事件中断当前对话
        _dialogueQueue.EnqueuePriority(new DialogueTask
        {
            ScriptName = "Events/Urgent",
            Label = eventId,
            Priority = DialoguePriority.Critical,
            OnInterrupted = () => {
                // 如果连紧急事件都被中断了，记录状态
                SaveInterruptedState(eventId);
            }
        });
    }

    public async void OnEnterCombat()
    {
        // 进入战斗时中断所有对话
        await _dialogueQueue.InterruptAll();
    }
}
```

#### 实现要点
- 使用优先级队列数据结构
- 实现对话中断时的状态保存
- 处理快速连续请求的防抖
- 考虑与存档系统的协作（中断时是否自动存档）

#### 验证方法
- 连续触发多个对话请求，验证顺序执行
- 测试高优先级任务的插队行为
- 测试中断恢复功能

---

### 2.3 参数传递机制

**状态**: [ ] TODO
**优先级**: P0 (必须)
**复杂度**: 中 (3-5天)
**依赖**: 无

#### 功能描述
实现游戏逻辑与 Naninovel 脚本之间的双向参数传递，支持启动脚本时传入参数，以及在脚本执行过程中读取/写入游戏变量。

#### 接口设计
```csharp
/// <summary>
/// 参数传递服务接口
/// </summary>
public interface INaninovelParameterService
{
    /// <summary>
    /// 设置将传递给下一个脚本的参数
    /// </summary>
    void SetParameter(string key, object value);

    /// <summary>
    /// 批量设置参数
    /// </summary>
    void SetParameters(Dictionary<string, object> parameters);

    /// <summary>
    /// 获取脚本中设置的输出参数
    /// </summary>
    T GetOutput<T>(string key, T defaultValue = default);

    /// <summary>
    /// 清除所有待传递参数
    /// </summary>
    void ClearParameters();

    /// <summary>
    /// 注册游戏变量绑定（自动双向同步）
    /// </summary>
    void BindVariable<T>(string naniKey, Func<T> getter, Action<T> setter);

    /// <summary>
    /// 移除变量绑定
    /// </summary>
    void UnbindVariable(string naniKey);

    /// <summary>
    /// 强制同步所有绑定变量到Naninovel
    /// </summary>
    void SyncToNaninovel();

    /// <summary>
    /// 强制从Naninovel同步所有绑定变量
    /// </summary>
    void SyncFromNaninovel();
}
```

#### 使用示例
```csharp
public class GameStateSync : MonoBehaviour
{
    [Inject] private INaninovelParameterService _paramService;
    [Inject] private IPlayerData _playerData;

    private void Start()
    {
        // 绑定玩家名称（双向同步）
        _paramService.BindVariable<string>(
            "PlayerName",
            () => _playerData.Name,
            value => _playerData.Name = value
        );

        // 绑定金币数量（只读，脚本中可显示但不能修改）
        _paramService.BindVariable<int>(
            "Gold",
            () => _playerData.Gold,
            null  // setter为null表示只读
        );
    }

    public async void StartShopDialogue(string shopkeeperId)
    {
        // 传递参数给脚本
        _paramService.SetParameters(new Dictionary<string, object>
        {
            { "ShopkeeperId", shopkeeperId },
            { "PlayerGold", _playerData.Gold },
            { "HasVIPCard", _playerData.Inventory.Contains("vip_card") }
        });

        await NaninovelService.PlayScript("Shop/Main");

        // 读取脚本输出
        var purchasedItems = _paramService.GetOutput<List<string>>("PurchasedItems");
        var totalSpent = _paramService.GetOutput<int>("TotalSpent", 0);

        // 应用到游戏逻辑
        foreach (var item in purchasedItems)
        {
            _playerData.Inventory.Add(item);
        }
        _playerData.Gold -= totalSpent;
    }
}
```

#### Naninovel脚本示例
```nani
# Shop/Main

; 读取传入参数
@set shopId="{ShopkeeperId}"
@set playerMoney={PlayerGold}
@set isVIP={HasVIPCard}

; 根据参数调整对话
@if isVIP
    店主: 欢迎光临，尊贵的VIP客户！
@else
    店主: 欢迎光临，请随便看看。
@endif

; 购物逻辑...

; 设置输出参数
@set PurchasedItems=["potion","sword"]
@set TotalSpent=500
```

#### 实现要点
- 利用 `CustomVariableManager` 实现参数注入
- 实现类型自动转换（支持基础类型、List、Dictionary）
- 变量绑定需要处理脚本执行期间的同步时机
- 注意线程安全问题

#### 验证方法
- 传递各种类型参数验证正确接收
- 测试双向绑定的实时同步
- 验证类型转换边界情况

---

### 2.4 初始化重试机制

**状态**: [ ] TODO
**优先级**: P1 (重要)
**复杂度**: 低 (1-2天)
**依赖**: 无

#### 功能描述
当 Naninovel 初始化失败时自动重试，提供初始化状态监控和错误恢复能力。

#### 接口设计
```csharp
/// <summary>
/// 初始化管理服务
/// </summary>
public interface INaninovelInitService
{
    /// <summary>
    /// 当前初始化状态
    /// </summary>
    InitializationState State { get; }

    /// <summary>
    /// 最近一次初始化错误
    /// </summary>
    Exception LastError { get; }

    /// <summary>
    /// 重试次数
    /// </summary>
    int RetryCount { get; }

    /// <summary>
    /// 手动触发初始化（带重试）
    /// </summary>
    UniTask<bool> InitializeAsync(InitOptions options = null);

    /// <summary>
    /// 强制重新初始化
    /// </summary>
    UniTask<bool> ReinitializeAsync();

    /// <summary>
    /// 初始化状态变化事件
    /// </summary>
    event Action<InitializationState, Exception> OnStateChanged;
}

public enum InitializationState
{
    NotStarted,
    Initializing,
    Initialized,
    Failed,
    Retrying
}

public class InitOptions
{
    public int MaxRetries { get; set; } = 3;
    public float RetryDelaySeconds { get; set; } = 1f;
    public bool ExponentialBackoff { get; set; } = true;
    public Action<int, Exception> OnRetry { get; set; }
}
```

#### 使用示例
```csharp
public class GameBootstrap : MonoBehaviour
{
    [Inject] private INaninovelInitService _initService;

    private async void Start()
    {
        _initService.OnStateChanged += OnInitStateChanged;

        var success = await _initService.InitializeAsync(new InitOptions
        {
            MaxRetries = 5,
            RetryDelaySeconds = 2f,
            ExponentialBackoff = true,
            OnRetry = (attempt, error) => {
                Debug.LogWarning($"Naninovel init retry {attempt}: {error.Message}");
                ShowRetryUI(attempt);
            }
        });

        if (!success)
        {
            ShowFatalErrorDialog("Failed to initialize dialogue system");
            return;
        }

        // 继续游戏启动流程
        await StartGame();
    }

    private void OnInitStateChanged(InitializationState state, Exception error)
    {
        switch (state)
        {
            case InitializationState.Initialized:
                HideLoadingUI();
                break;
            case InitializationState.Retrying:
                ShowLoadingUI("Retrying...");
                break;
            case InitializationState.Failed:
                LogError(error);
                break;
        }
    }
}
```

#### 实现要点
- 使用指数退避策略避免频繁重试
- 区分可恢复错误和致命错误
- 提供初始化进度回调
- 考虑网络问题导致的资源加载失败

#### 验证方法
- 模拟初始化失败场景
- 验证重试逻辑和延迟
- 测试最大重试次数后的失败处理

---

### 2.5 等待优化（事件驱动）

**状态**: [ ] TODO
**优先级**: P1 (重要)
**复杂度**: 中 (3-5天)
**依赖**: 2.4 初始化重试机制

#### 功能描述
将轮询等待改为事件驱动，优化初始化和状态检查的性能开销。

#### 接口设计
```csharp
/// <summary>
/// Naninovel状态监听服务
/// </summary>
public interface INaninovelStateService
{
    /// <summary>
    /// 引擎是否已初始化
    /// </summary>
    bool IsEngineInitialized { get; }

    /// <summary>
    /// 脚本播放器是否就绪
    /// </summary>
    bool IsPlayerReady { get; }

    /// <summary>
    /// 当前是否正在播放
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// 等待引擎初始化完成
    /// </summary>
    UniTask WaitForInitialization(CancellationToken ct = default);

    /// <summary>
    /// 等待脚本播放器就绪
    /// </summary>
    UniTask WaitForPlayerReady(CancellationToken ct = default);

    /// <summary>
    /// 等待当前对话结束
    /// </summary>
    UniTask WaitForDialogueEnd(CancellationToken ct = default);

    /// <summary>
    /// 引擎状态变化事件
    /// </summary>
    event Action<EngineState> OnEngineStateChanged;

    /// <summary>
    /// 播放状态变化事件
    /// </summary>
    event Action<PlaybackState> OnPlaybackStateChanged;
}

public enum EngineState
{
    NotInitialized,
    Initializing,
    Ready,
    Error
}

public enum PlaybackState
{
    Idle,
    Loading,
    Playing,
    Paused,
    WaitingForInput,
    Finished
}
```

#### 使用示例
```csharp
public class CutsceneController : MonoBehaviour
{
    [Inject] private INaninovelStateService _stateService;
    [Inject] private INaninovelService _naniService;

    public async UniTask PlayCutscene(string scriptName)
    {
        // 事件驱动等待，而非轮询
        await _stateService.WaitForInitialization();
        await _stateService.WaitForPlayerReady();

        // 订阅播放状态变化
        _stateService.OnPlaybackStateChanged += OnPlaybackChanged;

        try
        {
            await _naniService.PlayScript(scriptName);
            await _stateService.WaitForDialogueEnd();
        }
        finally
        {
            _stateService.OnPlaybackStateChanged -= OnPlaybackChanged;
        }
    }

    private void OnPlaybackChanged(PlaybackState state)
    {
        switch (state)
        {
            case PlaybackState.WaitingForInput:
                ShowContinuePrompt();
                break;
            case PlaybackState.Finished:
                HideContinuePrompt();
                break;
        }
    }
}
```

#### 实现要点
- 订阅 Naninovel 内部事件而非轮询状态
- 使用 `UniTaskCompletionSource` 实现事件转 Task
- 处理超时和取消场景
- 避免事件订阅内存泄漏

#### 验证方法
- 对比轮询和事件驱动的性能差异
- 测试各种状态转换场景
- 验证取消令牌正常工作

---

## 3. 存档系统模块

### 3.1 存档同步机制

**状态**: [ ] TODO
**优先级**: P0 (必须)
**复杂度**: 高 (1-2周)
**依赖**: 无

#### 功能描述
将 Naninovel 的存档系统与游戏主存档系统统一，实现单一存档文件包含完整游戏状态。

#### 接口设计
```csharp
/// <summary>
/// 存档同步服务主接口
/// </summary>
public interface INaninovelSaveIntegration
{
    /// <summary>
    /// 捕获当前Naninovel状态用于保存
    /// </summary>
    NaninovelSaveData CaptureState();

    /// <summary>
    /// 从存档数据恢复Naninovel状态
    /// </summary>
    UniTask RestoreState(NaninovelSaveData data);

    /// <summary>
    /// 检查存档数据兼容性
    /// </summary>
    SaveCompatibility CheckCompatibility(NaninovelSaveData data);

    /// <summary>
    /// 尝试迁移旧版存档
    /// </summary>
    NaninovelSaveData MigrateFromVersion(NaninovelSaveData data, int targetVersion);

    /// <summary>
    /// 存档数据版本
    /// </summary>
    int CurrentVersion { get; }
}

/// <summary>
/// Naninovel存档数据结构
/// </summary>
[Serializable]
public class NaninovelSaveData
{
    /// <summary>数据版本号</summary>
    public int Version;

    /// <summary>当前脚本名</summary>
    public string CurrentScriptName;

    /// <summary>播放位置（行号）</summary>
    public int PlaybackSpot;

    /// <summary>当前标签</summary>
    public string CurrentLabel;

    /// <summary>自定义变量</summary>
    public Dictionary<string, string> CustomVariables;

    /// <summary>已解锁的脚本列表</summary>
    public List<string> UnlockedScripts;

    /// <summary>已读文本哈希（用于跳过已读）</summary>
    public HashSet<int> ReadTextHashes;

    /// <summary>CG回廊解锁状态</summary>
    public List<string> UnlockedCGs;

    /// <summary>音乐回廊解锁状态</summary>
    public List<string> UnlockedBGMs;

    /// <summary>设置数据</summary>
    public NaninovelSettingsData Settings;

    /// <summary>存档时间戳</summary>
    public long TimestampTicks;

    /// <summary>存档截图（Base64）</summary>
    public string ScreenshotBase64;
}

[Serializable]
public class NaninovelSettingsData
{
    public float TextSpeed;
    public float AutoPlayDelay;
    public int SkipMode;
    public float VoiceVolume;
    public float BgmVolume;
    public float SeVolume;
    public int FontSize;
    public float DialogueOpacity;
}

public enum SaveCompatibility
{
    Compatible,
    NeedsMigration,
    Incompatible
}
```

#### 使用示例
```csharp
public class GameSaveManager : MonoBehaviour
{
    [Inject] private INaninovelSaveIntegration _naniSave;
    [Inject] private IGameSaveService _gameSave;

    public async UniTask SaveGame(int slotId)
    {
        var gameSaveData = new GameSaveData
        {
            // 游戏数据
            PlayerData = CapturePlayerData(),
            InventoryData = CaptureInventory(),
            QuestData = CaptureQuests(),

            // Naninovel数据
            NaninovelData = _naniSave.CaptureState()
        };

        await _gameSave.SaveToSlot(slotId, gameSaveData);
    }

    public async UniTask LoadGame(int slotId)
    {
        var gameSaveData = await _gameSave.LoadFromSlot(slotId);

        // 检查Naninovel数据兼容性
        var compatibility = _naniSave.CheckCompatibility(gameSaveData.NaninovelData);

        switch (compatibility)
        {
            case SaveCompatibility.Compatible:
                await _naniSave.RestoreState(gameSaveData.NaninovelData);
                break;

            case SaveCompatibility.NeedsMigration:
                var migratedData = _naniSave.MigrateFromVersion(
                    gameSaveData.NaninovelData,
                    _naniSave.CurrentVersion
                );
                await _naniSave.RestoreState(migratedData);
                break;

            case SaveCompatibility.Incompatible:
                ShowWarning("此存档的对话进度无法恢复，将从头开始。");
                break;
        }

        // 恢复游戏数据
        RestorePlayerData(gameSaveData.PlayerData);
        RestoreInventory(gameSaveData.InventoryData);
        RestoreQuests(gameSaveData.QuestData);
    }
}
```

#### 实现要点
- 使用 `IStateManager.SaveGame/LoadGame` 获取/设置状态
- 序列化需要考虑二进制大小（截图压缩）
- 版本迁移策略设计
- 处理脚本文件变更后的兼容性

#### 验证方法
- 保存/加载循环测试
- 跨版本存档加载测试
- 大量变量的性能测试

---

### 3.2 对话进度保存/恢复

**状态**: [ ] TODO
**优先级**: P0 (必须)
**复杂度**: 中 (3-5天)
**依赖**: 3.1 存档同步机制

#### 功能描述
精确保存和恢复对话播放位置，支持恢复到任意对话节点。

#### 接口设计
```csharp
/// <summary>
/// 对话进度服务
/// </summary>
public interface IDialogueProgressService
{
    /// <summary>
    /// 获取当前播放位置
    /// </summary>
    DialoguePosition GetCurrentPosition();

    /// <summary>
    /// 恢复到指定位置
    /// </summary>
    UniTask RestorePosition(DialoguePosition position);

    /// <summary>
    /// 检查位置是否有效（脚本是否存在）
    /// </summary>
    bool IsPositionValid(DialoguePosition position);

    /// <summary>
    /// 获取位置的可读描述（用于存档列表显示）
    /// </summary>
    string GetPositionDescription(DialoguePosition position);

    /// <summary>
    /// 位置变化事件（用于实时追踪）
    /// </summary>
    event Action<DialoguePosition> OnPositionChanged;
}

[Serializable]
public struct DialoguePosition
{
    /// <summary>脚本资源路径</summary>
    public string ScriptPath;

    /// <summary>行索引</summary>
    public int LineIndex;

    /// <summary>内联索引（一行内可能有多个命令）</summary>
    public int InlineIndex;

    /// <summary>标签名（可选，用于快速定位）</summary>
    public string Label;

    /// <summary>位置哈希（用于快速比较）</summary>
    public int Hash => HashCode.Combine(ScriptPath, LineIndex, InlineIndex);
}
```

#### 使用示例
```csharp
public class SaveSlotUI : MonoBehaviour
{
    [Inject] private IDialogueProgressService _progressService;

    public void UpdateSlotDisplay(int slotId, NaninovelSaveData saveData)
    {
        var position = new DialoguePosition
        {
            ScriptPath = saveData.CurrentScriptName,
            LineIndex = saveData.PlaybackSpot,
            Label = saveData.CurrentLabel
        };

        // 显示存档描述
        var description = _progressService.GetPositionDescription(position);
        slotDescriptionText.text = description; // 例如: "第三章 - 与艾丽丝的对话"

        // 检查存档是否可用
        var isValid = _progressService.IsPositionValid(position);
        loadButton.interactable = isValid;

        if (!isValid)
        {
            slotDescriptionText.text += " (脚本已更新，可能无法正确加载)";
        }
    }
}
```

#### 实现要点
- 使用 `IScriptPlayer.PlaybackSpot` 获取精确位置
- 恢复时需要先加载脚本再跳转
- 处理脚本修改后位置失效的情况
- 考虑分支路径的位置表示

#### 验证方法
- 在各种位置保存并恢复
- 修改脚本后加载旧存档
- 测试分支路径的保存恢复

---

### 3.3 自动存档点

**状态**: [ ] TODO
**优先级**: P1 (重要)
**复杂度**: 中 (3-5天)
**依赖**: 3.1 存档同步机制

#### 功能描述
在关键对话节点自动创建存档，支持通过脚本标记或事件触发。

#### 接口设计
```csharp
/// <summary>
/// 自动存档服务
/// </summary>
public interface IAutoSaveService
{
    /// <summary>
    /// 自动存档是否启用
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// 创建自动存档点
    /// </summary>
    UniTask CreateAutoSave(string label = null);

    /// <summary>
    /// 获取最近的自动存档
    /// </summary>
    AutoSaveInfo GetLatestAutoSave();

    /// <summary>
    /// 获取所有自动存档
    /// </summary>
    List<AutoSaveInfo> GetAllAutoSaves();

    /// <summary>
    /// 加载自动存档
    /// </summary>
    UniTask LoadAutoSave(string saveId);

    /// <summary>
    /// 清理旧自动存档（保留最近N个）
    /// </summary>
    void CleanupOldSaves(int keepCount);

    /// <summary>
    /// 自动存档创建事件
    /// </summary>
    event Action<AutoSaveInfo> OnAutoSaveCreated;
}

public class AutoSaveInfo
{
    public string SaveId { get; set; }
    public string Label { get; set; }
    public DateTime Timestamp { get; set; }
    public string ScriptName { get; set; }
    public string Description { get; set; }
    public byte[] Thumbnail { get; set; }
}

/// <summary>
/// 自动存档触发配置
/// </summary>
public class AutoSaveConfig
{
    /// <summary>最大自动存档数量</summary>
    public int MaxAutoSaves { get; set; } = 10;

    /// <summary>进入新脚本时自动存档</summary>
    public bool SaveOnScriptEnter { get; set; } = true;

    /// <summary>到达标签时自动存档</summary>
    public bool SaveOnLabel { get; set; } = true;

    /// <summary>需要触发存档的标签前缀</summary>
    public string AutoSaveLabelPrefix { get; set; } = "save_";

    /// <summary>选择后自动存档</summary>
    public bool SaveAfterChoice { get; set; } = true;

    /// <summary>最小存档间隔（秒）</summary>
    public float MinSaveInterval { get; set; } = 30f;
}
```

#### 使用示例
```csharp
// 配置自动存档
public class AutoSaveSetup : MonoBehaviour
{
    [Inject] private IAutoSaveService _autoSave;

    private void Start()
    {
        // 启用自动存档
        _autoSave.IsEnabled = true;

        // 监听自动存档事件
        _autoSave.OnAutoSaveCreated += info => {
            ShowToast($"已自动保存: {info.Label}");
        };
    }
}

// Naninovel脚本中触发
// 在脚本中使用特定标签触发自动存档
```

```nani
# Chapter1

; 章节开始自动存档（标签以save_开头）
# save_chapter1_start
旁白: 故事从这里开始...

; 普通标签不触发存档
# meeting_alice
爱丽丝: 你好！

; 重要选择前自动存档
# save_important_choice
旁白: 这是一个重要的选择...
@choice "选项A" goto:.choice_a "选项B" goto:.choice_b
```

#### 实现要点
- 监听脚本播放事件检测标签
- 使用防抖避免频繁存档
- 存档时捕获屏幕缩略图
- 后台异步执行避免卡顿

#### 验证方法
- 验证各种触发条件
- 测试存档数量限制
- 验证加载自动存档功能

---

### 3.4 存档槽位管理

**状态**: [ ] TODO
**优先级**: P1 (重要)
**复杂度**: 中 (3-5天)
**依赖**: 3.1 存档同步机制

#### 功能描述
提供统一的存档槽位管理，支持手动存档和自动存档的分类管理。

#### 接口设计
```csharp
/// <summary>
/// 存档槽位管理服务
/// </summary>
public interface ISaveSlotService
{
    /// <summary>
    /// 手动存档槽位数量
    /// </summary>
    int ManualSlotCount { get; }

    /// <summary>
    /// 获取槽位信息
    /// </summary>
    SaveSlotInfo GetSlotInfo(int slotId);

    /// <summary>
    /// 获取所有槽位信息
    /// </summary>
    List<SaveSlotInfo> GetAllSlots();

    /// <summary>
    /// 保存到指定槽位
    /// </summary>
    UniTask SaveToSlot(int slotId);

    /// <summary>
    /// 从指定槽位加载
    /// </summary>
    UniTask LoadFromSlot(int slotId);

    /// <summary>
    /// 删除槽位存档
    /// </summary>
    UniTask DeleteSlot(int slotId);

    /// <summary>
    /// 检查槽位是否有存档
    /// </summary>
    bool HasSave(int slotId);

    /// <summary>
    /// 获取推荐的保存槽位（最旧或空槽位）
    /// </summary>
    int GetRecommendedSlot();

    /// <summary>
    /// 槽位变化事件
    /// </summary>
    event Action<int, SaveSlotInfo> OnSlotChanged;
}

public class SaveSlotInfo
{
    public int SlotId { get; set; }
    public bool IsEmpty { get; set; }
    public DateTime? SaveTime { get; set; }
    public string ChapterName { get; set; }
    public string SceneName { get; set; }
    public TimeSpan PlayTime { get; set; }
    public Texture2D Thumbnail { get; set; }
    public NaninovelSaveData NaninovelData { get; set; }
}
```

#### 使用示例
```csharp
public class SaveLoadUI : MonoBehaviour
{
    [Inject] private ISaveSlotService _slotService;

    [SerializeField] private Transform _slotContainer;
    [SerializeField] private SaveSlotUIItem _slotPrefab;

    private void Start()
    {
        RefreshSlotList();
        _slotService.OnSlotChanged += OnSlotChanged;
    }

    private void RefreshSlotList()
    {
        // 清空现有UI
        foreach (Transform child in _slotContainer)
            Destroy(child.gameObject);

        // 创建槽位UI
        var slots = _slotService.GetAllSlots();
        foreach (var slot in slots)
        {
            var item = Instantiate(_slotPrefab, _slotContainer);
            item.Setup(slot, OnSlotClicked);
        }
    }

    private async void OnSlotClicked(SaveSlotInfo slot, bool isSaveMode)
    {
        if (isSaveMode)
        {
            if (!slot.IsEmpty)
            {
                var confirmed = await ShowConfirmDialog("是否覆盖此存档？");
                if (!confirmed) return;
            }
            await _slotService.SaveToSlot(slot.SlotId);
            ShowToast("保存成功");
        }
        else
        {
            if (slot.IsEmpty)
            {
                ShowToast("此槽位为空");
                return;
            }
            await _slotService.LoadFromSlot(slot.SlotId);
        }
    }

    private void OnSlotChanged(int slotId, SaveSlotInfo info)
    {
        // 更新对应槽位的UI
        RefreshSlotList();
    }
}
```

#### 实现要点
- 统一管理Naninovel和游戏存档
- 存档文件命名和路径规范
- 缩略图生成和压缩
- 存档文件完整性校验

#### 验证方法
- 测试所有槽位操作
- 验证覆盖存档功能
- 测试存档损坏处理

---

### 3.5 云存档预留接口

**状态**: [ ] TODO
**优先级**: P3 (低)
**复杂度**: 中 (3-5天)
**依赖**: 3.1 存档同步机制, 3.4 存档槽位管理

#### 功能描述
为未来云存档功能预留接口，支持存档的序列化和跨设备同步。

#### 接口设计
```csharp
/// <summary>
/// 云存档服务接口（预留）
/// </summary>
public interface ICloudSaveService
{
    /// <summary>
    /// 云存档是否可用
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// 同步状态
    /// </summary>
    CloudSyncState SyncState { get; }

    /// <summary>
    /// 上传存档到云端
    /// </summary>
    UniTask<CloudSaveResult> UploadSave(int slotId);

    /// <summary>
    /// 从云端下载存档
    /// </summary>
    UniTask<CloudSaveResult> DownloadSave(int slotId);

    /// <summary>
    /// 获取云端存档列表
    /// </summary>
    UniTask<List<CloudSaveInfo>> GetCloudSaves();

    /// <summary>
    /// 解决存档冲突
    /// </summary>
    UniTask<CloudSaveResult> ResolveConflict(int slotId, ConflictResolution resolution);

    /// <summary>
    /// 同步状态变化事件
    /// </summary>
    event Action<CloudSyncState> OnSyncStateChanged;

    /// <summary>
    /// 检测到冲突事件
    /// </summary>
    event Action<SaveConflictInfo> OnConflictDetected;
}

public enum CloudSyncState
{
    Idle,
    Syncing,
    Uploading,
    Downloading,
    Conflict,
    Error
}

public enum ConflictResolution
{
    UseLocal,
    UseCloud,
    UseMostRecent,
    Merge
}

public class CloudSaveInfo
{
    public string CloudId { get; set; }
    public int LocalSlotId { get; set; }
    public DateTime CloudTimestamp { get; set; }
    public DateTime? LocalTimestamp { get; set; }
    public bool HasConflict { get; set; }
    public long SizeBytes { get; set; }
}

public class CloudSaveResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public CloudSaveInfo SaveInfo { get; set; }
}

public class SaveConflictInfo
{
    public int SlotId { get; set; }
    public SaveSlotInfo LocalSave { get; set; }
    public CloudSaveInfo CloudSave { get; set; }
}
```

#### 使用示例
```csharp
// 预留实现，当前返回不可用状态
public class CloudSaveServiceStub : ICloudSaveService
{
    public bool IsAvailable => false;
    public CloudSyncState SyncState => CloudSyncState.Idle;

    public UniTask<CloudSaveResult> UploadSave(int slotId)
    {
        return UniTask.FromResult(new CloudSaveResult
        {
            Success = false,
            ErrorMessage = "Cloud save not implemented"
        });
    }

    // ... 其他方法类似返回未实现状态
}

// 未来实现示例
public class SteamCloudSaveService : ICloudSaveService
{
    public bool IsAvailable => SteamClient.IsValid;

    public async UniTask<CloudSaveResult> UploadSave(int slotId)
    {
        var localData = await _slotService.GetSlotData(slotId);
        var bytes = SerializeSaveData(localData);

        var result = await SteamRemoteStorage.FileWriteAsync($"save_{slotId}.dat", bytes);

        return new CloudSaveResult
        {
            Success = result,
            SaveInfo = new CloudSaveInfo { LocalSlotId = slotId }
        };
    }
}
```

#### 实现要点
- 当前实现存根版本
- 定义清晰的序列化格式
- 设计冲突解决策略
- 考虑带宽和存储限制

#### 验证方法
- 验证接口定义完整性
- 存根实现正确返回不可用状态
- 模拟云存档流程测试

---

## 4. UI集成模块

### 4.1 对话框样式系统

**状态**: [ ] TODO
**优先级**: P1 (重要)
**复杂度**: 中 (3-5天)
**依赖**: 无

#### 功能描述
提供对话框外观的运行时定制能力，支持多种预设样式和自定义配置。

#### 接口设计
```csharp
/// <summary>
/// 对话框样式服务
/// </summary>
public interface IDialogueStyleService
{
    /// <summary>
    /// 当前样式配置
    /// </summary>
    DialogueStyleConfig CurrentStyle { get; }

    /// <summary>
    /// 应用样式配置
    /// </summary>
    void ApplyStyle(DialogueStyleConfig config);

    /// <summary>
    /// 应用预设样式
    /// </summary>
    void ApplyPreset(string presetName);

    /// <summary>
    /// 获取所有可用预设
    /// </summary>
    List<string> GetAvailablePresets();

    /// <summary>
    /// 保存当前样式为预设
    /// </summary>
    void SaveAsPreset(string presetName);

    /// <summary>
    /// 重置为默认样式
    /// </summary>
    void ResetToDefault();

    /// <summary>
    /// 样式变化事件
    /// </summary>
    event Action<DialogueStyleConfig> OnStyleChanged;
}

[Serializable]
public class DialogueStyleConfig
{
    [Header("背景")]
    public Color BackgroundColor = new Color(0, 0, 0, 0.8f);
    public Sprite BackgroundSprite;
    public float BackgroundOpacity = 0.8f;

    [Header("文字")]
    public Font TextFont;
    public int TextSize = 24;
    public Color TextColor = Color.white;
    public float LineSpacing = 1.2f;

    [Header("角色名")]
    public Font NameFont;
    public int NameSize = 28;
    public Color NameColor = Color.yellow;

    [Header("边框")]
    public Color BorderColor = Color.white;
    public float BorderWidth = 2f;

    [Header("位置")]
    public DialoguePosition Position = DialoguePosition.Bottom;
    public Vector2 Padding = new Vector2(20, 15);
    public Vector2 Margin = new Vector2(50, 30);

    [Header("动画")]
    public float FadeInDuration = 0.2f;
    public float FadeOutDuration = 0.15f;
    public AnimationCurve FadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
}

public enum DialoguePosition
{
    Bottom,
    Top,
    Center,
    FullScreen
}
```

#### 使用示例
```csharp
public class DialogueStyleSettings : MonoBehaviour
{
    [Inject] private IDialogueStyleService _styleService;

    [SerializeField] private Slider _opacitySlider;
    [SerializeField] private Dropdown _presetDropdown;
    [SerializeField] private Slider _fontSizeSlider;

    private void Start()
    {
        // 初始化预设下拉框
        var presets = _styleService.GetAvailablePresets();
        _presetDropdown.AddOptions(presets);
        _presetDropdown.onValueChanged.AddListener(OnPresetSelected);

        // 初始化滑块
        var style = _styleService.CurrentStyle;
        _opacitySlider.value = style.BackgroundOpacity;
        _fontSizeSlider.value = style.TextSize;

        _opacitySlider.onValueChanged.AddListener(OnOpacityChanged);
        _fontSizeSlider.onValueChanged.AddListener(OnFontSizeChanged);
    }

    private void OnPresetSelected(int index)
    {
        var presets = _styleService.GetAvailablePresets();
        _styleService.ApplyPreset(presets[index]);
    }

    private void OnOpacityChanged(float value)
    {
        var style = _styleService.CurrentStyle;
        style.BackgroundOpacity = value;
        _styleService.ApplyStyle(style);
    }

    private void OnFontSizeChanged(float value)
    {
        var style = _styleService.CurrentStyle;
        style.TextSize = Mathf.RoundToInt(value);
        _styleService.ApplyStyle(style);
    }
}
```

#### 实现要点
- 通过 `IUIManager` 访问对话UI
- 修改UI元素的RectTransform和样式组件
- 预设存储在ScriptableObject中
- 实时预览样式变化

#### 验证方法
- 测试各种样式参数的应用
- 验证预设保存和加载
- 测试样式持久化

---

### 4.2 选项按钮定制

**状态**: [ ] TODO
**优先级**: P2 (一般)
**复杂度**: 中 (3-5天)
**依赖**: 4.1 对话框样式系统

#### 功能描述
允许定制选择按钮的外观、布局和交互效果。

#### 接口设计
```csharp
/// <summary>
/// 选项按钮样式服务
/// </summary>
public interface IChoiceButtonStyleService
{
    /// <summary>
    /// 应用按钮样式配置
    /// </summary>
    void ApplyStyle(ChoiceButtonStyleConfig config);

    /// <summary>
    /// 为特定选项设置样式（用于条件高亮等）
    /// </summary>
    void SetOptionStyle(int optionIndex, ChoiceOptionStyle style);

    /// <summary>
    /// 清除所有特定样式
    /// </summary>
    void ClearOptionStyles();

    /// <summary>
    /// 获取当前配置
    /// </summary>
    ChoiceButtonStyleConfig GetCurrentConfig();
}

[Serializable]
public class ChoiceButtonStyleConfig
{
    [Header("布局")]
    public ChoiceLayout Layout = ChoiceLayout.Vertical;
    public float Spacing = 10f;
    public TextAnchor Alignment = TextAnchor.MiddleCenter;

    [Header("按钮尺寸")]
    public Vector2 ButtonSize = new Vector2(400, 50);
    public bool AutoHeight = true;

    [Header("默认样式")]
    public ChoiceButtonAppearance Normal;
    public ChoiceButtonAppearance Highlighted;
    public ChoiceButtonAppearance Pressed;
    public ChoiceButtonAppearance Disabled;

    [Header("动画")]
    public float HoverScale = 1.05f;
    public float ClickScale = 0.95f;
    public float AnimationDuration = 0.1f;
}

[Serializable]
public class ChoiceButtonAppearance
{
    public Color BackgroundColor = Color.white;
    public Sprite BackgroundSprite;
    public Color TextColor = Color.black;
    public int FontSize = 20;
    public FontStyle FontStyle = FontStyle.Normal;
}

public enum ChoiceLayout
{
    Vertical,
    Horizontal,
    Grid
}

public class ChoiceOptionStyle
{
    public Color? BackgroundColor { get; set; }
    public Color? TextColor { get; set; }
    public string IconPath { get; set; }
    public bool IsHighlighted { get; set; }
    public string TooltipText { get; set; }
}
```

#### 使用示例
```csharp
public class ChoiceStyleController : MonoBehaviour
{
    [Inject] private IChoiceButtonStyleService _choiceStyle;
    [Inject] private IChoiceEventService _choiceEvent;

    private void Start()
    {
        // 配置默认样式
        var config = new ChoiceButtonStyleConfig
        {
            Layout = ChoiceLayout.Vertical,
            Spacing = 15f,
            Normal = new ChoiceButtonAppearance
            {
                BackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.9f),
                TextColor = Color.white,
                FontSize = 22
            },
            Highlighted = new ChoiceButtonAppearance
            {
                BackgroundColor = new Color(0.4f, 0.4f, 0.8f, 0.9f),
                TextColor = Color.yellow,
                FontSize = 24
            }
        };
        _choiceStyle.ApplyStyle(config);

        // 监听选项显示，动态设置样式
        _choiceEvent.OnChoiceDisplaying += OnChoiceDisplaying;
    }

    private void OnChoiceDisplaying(ChoiceDisplayEventArgs args)
    {
        // 根据游戏状态高亮某些选项
        for (int i = 0; i < args.Options.Count; i++)
        {
            var option = args.Options[i];

            // 例如：检查玩家是否满足选项条件
            if (option.CustomData == "requires_gold_100")
            {
                if (PlayerData.Gold < 100)
                {
                    _choiceStyle.SetOptionStyle(i, new ChoiceOptionStyle
                    {
                        BackgroundColor = Color.gray,
                        TextColor = Color.red,
                        TooltipText = "需要100金币"
                    });
                }
            }

            // 高亮推荐选项
            if (option.CustomData == "recommended")
            {
                _choiceStyle.SetOptionStyle(i, new ChoiceOptionStyle
                {
                    IsHighlighted = true,
                    IconPath = "Icons/star"
                });
            }
        }
    }
}
```

#### 实现要点
- 自定义 ChoiceHandlerButton 预制体
- 实现按钮池化以优化性能
- 支持图标和富文本
- 处理不同数量选项的布局

#### 验证方法
- 测试各种布局模式
- 验证动态样式切换
- 测试大量选项的显示

---

### 4.3 对话历史面板

**状态**: [ ] TODO
**优先级**: P1 (重要)
**复杂度**: 高 (1-2周)
**依赖**: 无

#### 功能描述
显示对话历史记录，支持滚动查看、语音重播、跳转到历史位置。

#### 接口设计
```csharp
/// <summary>
/// 对话历史服务
/// </summary>
public interface IDialogueHistoryService
{
    /// <summary>
    /// 历史记录数量
    /// </summary>
    int Count { get; }

    /// <summary>
    /// 最大历史记录数
    /// </summary>
    int MaxHistoryCount { get; set; }

    /// <summary>
    /// 获取历史记录
    /// </summary>
    List<DialogueHistoryEntry> GetHistory(int count = -1, int offset = 0);

    /// <summary>
    /// 清空历史
    /// </summary>
    void Clear();

    /// <summary>
    /// 显示历史面板
    /// </summary>
    UniTask ShowPanel();

    /// <summary>
    /// 隐藏历史面板
    /// </summary>
    UniTask HidePanel();

    /// <summary>
    /// 面板是否显示中
    /// </summary>
    bool IsPanelVisible { get; }

    /// <summary>
    /// 重播语音
    /// </summary>
    UniTask ReplayVoice(string voiceId);

    /// <summary>
    /// 跳转到历史位置（如果支持）
    /// </summary>
    UniTask JumpToEntry(DialogueHistoryEntry entry);

    /// <summary>
    /// 新记录添加事件
    /// </summary>
    event Action<DialogueHistoryEntry> OnEntryAdded;
}

[Serializable]
public class DialogueHistoryEntry
{
    /// <summary>唯一ID</summary>
    public string Id { get; set; }

    /// <summary>角色ID</summary>
    public string CharacterId { get; set; }

    /// <summary>角色显示名</summary>
    public string CharacterName { get; set; }

    /// <summary>对话文本</summary>
    public string Text { get; set; }

    /// <summary>语音资源ID</summary>
    public string VoiceId { get; set; }

    /// <summary>时间戳</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>脚本位置（用于跳转）</summary>
    public DialoguePosition Position { get; set; }

    /// <summary>是否为旁白</summary>
    public bool IsNarration { get; set; }

    /// <summary>角色头像资源路径</summary>
    public string AvatarPath { get; set; }
}
```

#### 使用示例
```csharp
public class HistoryPanelUI : MonoBehaviour
{
    [Inject] private IDialogueHistoryService _historyService;

    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private Transform _contentContainer;
    [SerializeField] private HistoryEntryUI _entryPrefab;
    [SerializeField] private Button _closeButton;

    private List<HistoryEntryUI> _entryPool = new List<HistoryEntryUI>();

    private void Start()
    {
        _closeButton.onClick.AddListener(OnCloseClicked);
        _historyService.OnEntryAdded += OnNewEntry;
    }

    public async void Show()
    {
        await _historyService.ShowPanel();
        RefreshContent();
    }

    private void RefreshContent()
    {
        var entries = _historyService.GetHistory();

        // 对象池处理
        EnsurePoolSize(entries.Count);

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var ui = _entryPool[i];
            ui.gameObject.SetActive(true);
            ui.Setup(entry, OnEntryClicked, OnVoiceReplayClicked);
        }

        // 隐藏多余的
        for (int i = entries.Count; i < _entryPool.Count; i++)
        {
            _entryPool[i].gameObject.SetActive(false);
        }

        // 滚动到底部
        Canvas.ForceUpdateCanvases();
        _scrollRect.verticalNormalizedPosition = 0;
    }

    private async void OnVoiceReplayClicked(DialogueHistoryEntry entry)
    {
        if (!string.IsNullOrEmpty(entry.VoiceId))
        {
            await _historyService.ReplayVoice(entry.VoiceId);
        }
    }

    private async void OnEntryClicked(DialogueHistoryEntry entry)
    {
        var confirmed = await ShowConfirmDialog("是否跳转到此处？（当前进度将丢失）");
        if (confirmed)
        {
            await _historyService.JumpToEntry(entry);
            await _historyService.HidePanel();
        }
    }

    private async void OnCloseClicked()
    {
        await _historyService.HidePanel();
    }
}
```

#### 实现要点
- 监听 `ITextPrinterActor` 的打印事件
- 使用虚拟滚动优化大量记录
- 语音资源的异步加载
- 历史记录的序列化存储

#### 验证方法
- 测试大量历史记录的显示性能
- 验证语音重播功能
- 测试跳转功能的准确性

---

### 4.4 快捷菜单系统

**状态**: [ ] TODO
**优先级**: P2 (一般)
**复杂度**: 中 (3-5天)
**依赖**: 无

#### 功能描述
提供对话过程中的快捷操作菜单，包括保存、加载、自动播放、跳过等功能。

#### 接口设计
```csharp
/// <summary>
/// 快捷菜单服务
/// </summary>
public interface IQuickMenuService
{
    /// <summary>
    /// 菜单是否可见
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// 显示快捷菜单
    /// </summary>
    void Show();

    /// <summary>
    /// 隐藏快捷菜单
    /// </summary>
    void Hide();

    /// <summary>
    /// 切换可见性
    /// </summary>
    void Toggle();

    /// <summary>
    /// 配置菜单项
    /// </summary>
    void Configure(QuickMenuConfig config);

    /// <summary>
    /// 设置按钮启用状态
    /// </summary>
    void SetButtonEnabled(QuickMenuButton button, bool enabled);

    /// <summary>
    /// 菜单项点击事件
    /// </summary>
    event Action<QuickMenuButton> OnButtonClicked;
}

public enum QuickMenuButton
{
    Save,
    Load,
    History,
    Auto,
    Skip,
    Settings,
    Hide,
    Title
}

[Serializable]
public class QuickMenuConfig
{
    /// <summary>显示的按钮列表</summary>
    public List<QuickMenuButton> VisibleButtons = new List<QuickMenuButton>
    {
        QuickMenuButton.Save,
        QuickMenuButton.Load,
        QuickMenuButton.History,
        QuickMenuButton.Auto,
        QuickMenuButton.Skip,
        QuickMenuButton.Settings,
        QuickMenuButton.Hide
    };

    /// <summary>菜单位置</summary>
    public QuickMenuPosition Position = QuickMenuPosition.TopRight;

    /// <summary>自动隐藏延迟（秒，0为不自动隐藏）</summary>
    public float AutoHideDelay = 0f;

    /// <summary>按钮样式</summary>
    public QuickMenuButtonStyle ButtonStyle = new QuickMenuButtonStyle();
}

public enum QuickMenuPosition
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

[Serializable]
public class QuickMenuButtonStyle
{
    public Vector2 ButtonSize = new Vector2(40, 40);
    public float Spacing = 5f;
    public Color NormalColor = Color.white;
    public Color HighlightColor = Color.yellow;
    public Color ActiveColor = Color.green; // Auto/Skip激活时
}
```

#### 使用示例
```csharp
public class QuickMenuController : MonoBehaviour
{
    [Inject] private IQuickMenuService _quickMenu;
    [Inject] private IAutoPlayService _autoPlay;
    [Inject] private ISkipService _skip;
    [Inject] private IDialogueHistoryService _history;

    private void Start()
    {
        // 配置菜单
        _quickMenu.Configure(new QuickMenuConfig
        {
            VisibleButtons = new List<QuickMenuButton>
            {
                QuickMenuButton.Save,
                QuickMenuButton.Load,
                QuickMenuButton.History,
                QuickMenuButton.Auto,
                QuickMenuButton.Skip,
                QuickMenuButton.Hide
            },
            Position = QuickMenuPosition.TopRight
        });

        // 监听按钮点击
        _quickMenu.OnButtonClicked += OnQuickMenuButtonClicked;
    }

    private async void OnQuickMenuButtonClicked(QuickMenuButton button)
    {
        switch (button)
        {
            case QuickMenuButton.Save:
                await ShowSavePanel();
                break;
            case QuickMenuButton.Load:
                await ShowLoadPanel();
                break;
            case QuickMenuButton.History:
                await _history.ShowPanel();
                break;
            case QuickMenuButton.Auto:
                _autoPlay.Toggle();
                break;
            case QuickMenuButton.Skip:
                _skip.Toggle();
                break;
            case QuickMenuButton.Hide:
                HideUI();
                break;
        }
    }

    // 鼠标右键显示/隐藏菜单
    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            _quickMenu.Toggle();
        }
    }
}
```

#### 实现要点
- 自定义UI预制体
- 支持键盘快捷键
- Auto/Skip状态的视觉反馈
- 响应式布局适配

#### 验证方法
- 测试所有按钮功能
- 验证不同位置配置
- 测试与其他UI的交互

---

### 4.5 角色名牌系统

**状态**: [ ] TODO
**优先级**: P2 (一般)
**复杂度**: 中 (3-5天)
**依赖**: 4.1 对话框样式系统

#### 功能描述
定制角色名称显示，支持颜色区分、图标、特效等。

#### 接口设计
```csharp
/// <summary>
/// 角色名牌服务
/// </summary>
public interface ICharacterNameplateService
{
    /// <summary>
    /// 设置角色名牌配置
    /// </summary>
    void SetNameplate(string characterId, NameplateConfig config);

    /// <summary>
    /// 获取角色名牌配置
    /// </summary>
    NameplateConfig GetNameplate(string characterId);

    /// <summary>
    /// 移除角色名牌配置（使用默认）
    /// </summary>
    void RemoveNameplate(string characterId);

    /// <summary>
    /// 设置默认名牌配置
    /// </summary>
    void SetDefaultNameplate(NameplateConfig config);

    /// <summary>
    /// 从配置文件加载所有名牌
    /// </summary>
    void LoadFromConfig(NameplateConfigAsset asset);
}

[Serializable]
public class NameplateConfig
{
    /// <summary>显示名称（null则使用角色默认名）</summary>
    public string DisplayName;

    /// <summary>名称颜色</summary>
    public Color NameColor = Color.white;

    /// <summary>使用渐变色</summary>
    public bool UseGradient = false;

    /// <summary>渐变起始色</summary>
    public Color GradientStart = Color.white;

    /// <summary>渐变结束色</summary>
    public Color GradientEnd = Color.white;

    /// <summary>名称前图标</summary>
    public Sprite PrefixIcon;

    /// <summary>名称后图标</summary>
    public Sprite SuffixIcon;

    /// <summary>字体</summary>
    public Font Font;

    /// <summary>字号</summary>
    public int FontSize = 28;

    /// <summary>是否显示背景</summary>
    public bool ShowBackground = false;

    /// <summary>背景颜色</summary>
    public Color BackgroundColor = new Color(0, 0, 0, 0.5f);

    /// <summary>背景Sprite</summary>
    public Sprite BackgroundSprite;

    /// <summary>显示特效</summary>
    public NameplateEffect Effect = NameplateEffect.None;
}

public enum NameplateEffect
{
    None,
    Glow,
    Shadow,
    Outline,
    Shake,
    Wave
}

[CreateAssetMenu(fileName = "NameplateConfig", menuName = "Naninovel/Nameplate Config")]
public class NameplateConfigAsset : ScriptableObject
{
    public NameplateConfig DefaultConfig;
    public List<CharacterNameplateEntry> CharacterConfigs;
}

[Serializable]
public class CharacterNameplateEntry
{
    public string CharacterId;
    public NameplateConfig Config;
}
```

#### 使用示例
```csharp
public class NameplateSetup : MonoBehaviour
{
    [Inject] private ICharacterNameplateService _nameplate;

    [SerializeField] private NameplateConfigAsset _configAsset;

    private void Start()
    {
        // 从配置加载
        _nameplate.LoadFromConfig(_configAsset);

        // 运行时动态设置
        _nameplate.SetNameplate("Hero", new NameplateConfig
        {
            DisplayName = "勇者",
            NameColor = Color.cyan,
            Effect = NameplateEffect.Glow,
            PrefixIcon = Resources.Load<Sprite>("Icons/sword")
        });

        _nameplate.SetNameplate("Villain", new NameplateConfig
        {
            DisplayName = "???",  // 未知时显示???
            NameColor = Color.red,
            Effect = NameplateEffect.Shadow
        });
    }

    // 剧情推进后揭示真名
    public void RevealVillainName()
    {
        var config = _nameplate.GetNameplate("Villain");
        config.DisplayName = "暗黑魔王";
        _nameplate.SetNameplate("Villain", config);
    }
}
```

#### 实现要点
- 扩展 Naninovel 的角色元数据
- 实现富文本标签生成
- 支持 TextMeshPro 效果
- 配置热重载

#### 验证方法
- 测试各种效果组合
- 验证运行时动态修改
- 测试未配置角色的默认处理

---

### 4.6 文字效果扩展

**状态**: [ ] TODO
**优先级**: P2 (一般)
**复杂度**: 高 (1-2周)
**依赖**: 无

#### 功能描述
扩展对话文字的显示效果，支持打字机效果变体、抖动、渐变等。

#### 接口设计
```csharp
/// <summary>
/// 文字效果服务
/// </summary>
public interface ITextEffectService
{
    /// <summary>
    /// 注册自定义效果
    /// </summary>
    void RegisterEffect(string effectName, ITextEffect effect);

    /// <summary>
    /// 移除自定义效果
    /// </summary>
    void UnregisterEffect(string effectName);

    /// <summary>
    /// 获取所有可用效果
    /// </summary>
    List<string> GetAvailableEffects();

    /// <summary>
    /// 设置默认打字效果
    /// </summary>
    void SetDefaultRevealEffect(RevealEffectConfig config);

    /// <summary>
    /// 暂时使用特定效果（下一条对话）
    /// </summary>
    void UseEffectOnce(string effectName);
}

/// <summary>
/// 文字效果接口
/// </summary>
public interface ITextEffect
{
    /// <summary>
    /// 效果名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 初始化效果
    /// </summary>
    void Initialize(TMP_Text textComponent);

    /// <summary>
    /// 每帧更新
    /// </summary>
    void Update(float deltaTime);

    /// <summary>
    /// 应用到指定字符范围
    /// </summary>
    void ApplyToRange(int startIndex, int endIndex, EffectParameters parameters);

    /// <summary>
    /// 清理效果
    /// </summary>
    void Cleanup();
}

[Serializable]
public class RevealEffectConfig
{
    public RevealType Type = RevealType.Typewriter;
    public float Speed = 50f; // 字符每秒
    public AnimationCurve SpeedCurve;

    public RevealEffectType Effect = RevealEffectType.None;
    public float EffectIntensity = 1f;
}

public enum RevealType
{
    Typewriter,     // 逐字显示
    Fade,           // 淡入
    Scale,          // 缩放
    Slide,          // 滑入
    Wave            // 波浪式显示
}

public enum RevealEffectType
{
    None,
    Shake,          // 抖动
    Rainbow,        // 彩虹色
    Glow,           // 发光
    Bounce,         // 弹跳
    Wobble          // 摇晃
}

/// <summary>
/// 内联效果标签参数
/// </summary>
public class EffectParameters
{
    public float Intensity { get; set; } = 1f;
    public float Speed { get; set; } = 1f;
    public Color Color { get; set; } = Color.white;
    public Dictionary<string, string> CustomParams { get; set; }
}
```

#### 使用示例
```csharp
// 注册自定义效果
public class CustomTextEffects : MonoBehaviour
{
    [Inject] private ITextEffectService _effectService;

    private void Start()
    {
        // 注册抖动效果
        _effectService.RegisterEffect("shake", new ShakeTextEffect());

        // 注册彩虹效果
        _effectService.RegisterEffect("rainbow", new RainbowTextEffect());

        // 设置默认打字效果
        _effectService.SetDefaultRevealEffect(new RevealEffectConfig
        {
            Type = RevealType.Typewriter,
            Speed = 60f
        });
    }
}

// 自定义抖动效果实现
public class ShakeTextEffect : ITextEffect
{
    public string Name => "shake";

    private TMP_Text _text;
    private Vector3[] _originalVertices;
    private float _time;
    private float _intensity = 2f;
    private float _speed = 50f;

    public void Initialize(TMP_Text textComponent)
    {
        _text = textComponent;
        _text.ForceMeshUpdate();
        // 保存原始顶点位置
        var info = _text.textInfo;
        _originalVertices = new Vector3[info.meshInfo[0].vertices.Length];
        Array.Copy(info.meshInfo[0].vertices, _originalVertices, _originalVertices.Length);
    }

    public void Update(float deltaTime)
    {
        _time += deltaTime;

        var info = _text.textInfo;
        for (int i = 0; i < info.characterCount; i++)
        {
            var charInfo = info.characterInfo[i];
            if (!charInfo.isVisible) continue;

            var offset = new Vector3(
                Mathf.Sin(_time * _speed + i) * _intensity,
                Mathf.Cos(_time * _speed + i) * _intensity,
                0
            );

            var vertexIndex = charInfo.vertexIndex;
            var meshInfo = info.meshInfo[charInfo.materialReferenceIndex];

            for (int j = 0; j < 4; j++)
            {
                meshInfo.vertices[vertexIndex + j] = _originalVertices[vertexIndex + j] + offset;
            }
        }

        _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }

    public void ApplyToRange(int startIndex, int endIndex, EffectParameters parameters)
    {
        _intensity = parameters.Intensity * 2f;
        _speed = parameters.Speed * 50f;
    }

    public void Cleanup()
    {
        // 恢复原始顶点
        if (_text != null && _originalVertices != null)
        {
            var info = _text.textInfo;
            Array.Copy(_originalVertices, info.meshInfo[0].vertices, _originalVertices.Length);
            _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }
    }
}
```

#### Naninovel脚本中使用
```nani
; 使用内联标签
角色: 这是<shake>抖动的文字</shake>，这是<rainbow>彩虹色</rainbow>！

; 整句使用效果
@style TextEffect:shake
角色: 这整句话都在抖动...
@style TextEffect:none

; 使用自定义参数
角色: 这是<shake intensity=3 speed=2>剧烈抖动</shake>的文字！
```

#### 实现要点
- 基于 TextMeshPro 顶点动画
- 实现富文本标签解析
- 与 Naninovel 打字机效果协作
- 性能优化（仅更新可见字符）

#### 验证方法
- 测试各种效果的视觉表现
- 验证与打字机效果的兼容性
- 测试长文本的性能

---

## 5. 设置系统模块

### 5.1 文字速度控制

**状态**: [ ] TODO
**优先级**: P0 (必须)
**复杂度**: 低 (1-2天)
**依赖**: 无

#### 功能描述
控制对话文字的显示速度，支持实时调整和持久化保存。

#### 接口设计
```csharp
/// <summary>
/// Naninovel设置服务主接口
/// </summary>
public interface INaninovelSettings
{
    /// <summary>
    /// 文字速度 (0-1, 0最慢, 1瞬间显示)
    /// </summary>
    float TextSpeed { get; set; }

    /// <summary>
    /// 自动播放延迟 (秒)
    /// </summary>
    float AutoPlayDelay { get; set; }

    /// <summary>
    /// 跳过模式
    /// </summary>
    SkipMode SkipMode { get; set; }

    /// <summary>
    /// 语音音量 (0-1)
    /// </summary>
    float VoiceVolume { get; set; }

    /// <summary>
    /// BGM音量 (0-1)
    /// </summary>
    float BgmVolume { get; set; }

    /// <summary>
    /// 音效音量 (0-1)
    /// </summary>
    float SeVolume { get; set; }

    /// <summary>
    /// 字体大小
    /// </summary>
    int FontSize { get; set; }

    /// <summary>
    /// 对话框透明度 (0-1)
    /// </summary>
    float DialogueOpacity { get; set; }

    /// <summary>
    /// 应用所有设置到Naninovel
    /// </summary>
    void Apply();

    /// <summary>
    /// 从Naninovel同步设置
    /// </summary>
    void SyncFromNaninovel();

    /// <summary>
    /// 重置为默认值
    /// </summary>
    void ResetToDefault();

    /// <summary>
    /// 保存设置
    /// </summary>
    void Save();

    /// <summary>
    /// 加载设置
    /// </summary>
    void Load();

    /// <summary>
    /// 设置变化事件
    /// </summary>
    event Action<string, object> OnSettingChanged;
}

public enum SkipMode
{
    /// <summary>禁用跳过</summary>
    Disabled = 0,

    /// <summary>仅跳过已读</summary>
    ReadOnly = 1,

    /// <summary>跳过所有</summary>
    All = 2
}
```

#### 使用示例
```csharp
public class TextSpeedSettingUI : MonoBehaviour
{
    [Inject] private INaninovelSettings _settings;

    [SerializeField] private Slider _speedSlider;
    [SerializeField] private Toggle _instantToggle;
    [SerializeField] private TMP_Text _previewText;

    private void Start()
    {
        // 初始化UI
        _speedSlider.value = _settings.TextSpeed;
        _instantToggle.isOn = _settings.TextSpeed >= 1f;

        // 绑定事件
        _speedSlider.onValueChanged.AddListener(OnSpeedChanged);
        _instantToggle.onValueChanged.AddListener(OnInstantToggled);

        // 监听设置变化
        _settings.OnSettingChanged += OnSettingChanged;
    }

    private void OnSpeedChanged(float value)
    {
        _settings.TextSpeed = value;
        _settings.Apply();
        UpdatePreview();
    }

    private void OnInstantToggled(bool isOn)
    {
        if (isOn)
        {
            _speedSlider.value = 1f;
            _settings.TextSpeed = 1f;
        }
        _speedSlider.interactable = !isOn;
        _settings.Apply();
    }

    private void UpdatePreview()
    {
        // 显示预览效果
        StartCoroutine(TypewriterPreview());
    }

    private IEnumerator TypewriterPreview()
    {
        var text = "这是文字速度预览效果...";
        _previewText.text = "";

        var charDelay = (1f - _settings.TextSpeed) * 0.1f;

        foreach (var c in text)
        {
            _previewText.text += c;
            if (charDelay > 0)
                yield return new WaitForSeconds(charDelay);
        }
    }

    private void OnSettingChanged(string key, object value)
    {
        if (key == nameof(INaninovelSettings.TextSpeed))
        {
            _speedSlider.SetValueWithoutNotify((float)value);
        }
    }

    private void OnDestroy()
    {
        _settings.Save();
    }
}
```

#### 实现要点
- 通过 `ITextPrinterManager` 设置打字速度
- 映射0-1值到实际字符延迟
- 支持即时显示模式
- 与游戏设置系统同步

#### 验证方法
- 测试不同速度值的实际效果
- 验证设置持久化
- 测试即时显示开关

---

### 5.2 自动播放设置

**状态**: [ ] TODO
**优先级**: P1 (重要)
**复杂度**: 低 (1-2天)
**依赖**: 5.1 文字速度控制

#### 功能描述
控制自动播放模式的行为，包括延迟时间、语音等待等。

#### 接口设计
```csharp
/// <summary>
/// 自动播放服务
/// </summary>
public interface IAutoPlayService
{
    /// <summary>
    /// 自动播放是否激活
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// 开始自动播放
    /// </summary>
    void Start();

    /// <summary>
    /// 停止自动播放
    /// </summary>
    void Stop();

    /// <summary>
    /// 切换自动播放
    /// </summary>
    void Toggle();

    /// <summary>
    /// 自动播放配置
    /// </summary>
    AutoPlayConfig Config { get; set; }

    /// <summary>
    /// 自动播放状态变化事件
    /// </summary>
    event Action<bool> OnActiveChanged;
}

[Serializable]
public class AutoPlayConfig
{
    /// <summary>基础延迟（秒）</summary>
    public float BaseDelay = 2f;

    /// <summary>每字符额外延迟（秒）</summary>
    public float DelayPerCharacter = 0.05f;

    /// <summary>等待语音播放完成</summary>
    public bool WaitForVoice = true;

    /// <summary>选择时暂停自动播放</summary>
    public bool PauseOnChoice = true;

    /// <summary>最小延迟（秒）</summary>
    public float MinDelay = 1f;

    /// <summary>最大延迟（秒）</summary>
    public float MaxDelay = 10f;
}
```

#### 使用示例
```csharp
public class AutoPlaySettingUI : MonoBehaviour
{
    [Inject] private IAutoPlayService _autoPlay;
    [Inject] private INaninovelSettings _settings;

    [SerializeField] private Slider _delaySlider;
    [SerializeField] private Toggle _waitVoiceToggle;
    [SerializeField] private Button _autoPlayButton;
    [SerializeField] private Image _autoPlayIcon;

    [SerializeField] private Color _activeColor = Color.green;
    [SerializeField] private Color _inactiveColor = Color.white;

    private void Start()
    {
        // 初始化
        var config = _autoPlay.Config;
        _delaySlider.value = config.BaseDelay;
        _waitVoiceToggle.isOn = config.WaitForVoice;

        // 绑定事件
        _delaySlider.onValueChanged.AddListener(OnDelayChanged);
        _waitVoiceToggle.onValueChanged.AddListener(OnWaitVoiceChanged);
        _autoPlayButton.onClick.AddListener(OnAutoPlayClicked);

        _autoPlay.OnActiveChanged += OnAutoPlayStateChanged;
        UpdateButtonVisual();
    }

    private void OnDelayChanged(float value)
    {
        var config = _autoPlay.Config;
        config.BaseDelay = value;
        _autoPlay.Config = config;
    }

    private void OnWaitVoiceChanged(bool value)
    {
        var config = _autoPlay.Config;
        config.WaitForVoice = value;
        _autoPlay.Config = config;
    }

    private void OnAutoPlayClicked()
    {
        _autoPlay.Toggle();
    }

    private void OnAutoPlayStateChanged(bool isActive)
    {
        UpdateButtonVisual();
    }

    private void UpdateButtonVisual()
    {
        _autoPlayIcon.color = _autoPlay.IsActive ? _activeColor : _inactiveColor;
    }
}
```

#### 实现要点
- 通过 `IScriptPlayer` 控制自动播放
- 计算实际延迟（基础 + 字符数 + 语音时长）
- 处理选择分支时的暂停
- 与快捷菜单状态同步

#### 验证方法
- 测试不同延迟配置
- 验证语音等待功能
- 测试选择时的暂停恢复

---

### 5.3 跳过模式管理

**状态**: [ ] TODO
**优先级**: P1 (重要)
**复杂度**: 中 (3-5天)
**依赖**: 5.1 文字速度控制

#### 功能描述
管理对话跳过功能，支持仅跳过已读内容或全部跳过。

#### 接口设计
```csharp
/// <summary>
/// 跳过服务
/// </summary>
public interface ISkipService
{
    /// <summary>
    /// 跳过是否激活
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// 当前跳过模式
    /// </summary>
    SkipMode Mode { get; set; }

    /// <summary>
    /// 开始跳过
    /// </summary>
    void Start();

    /// <summary>
    /// 停止跳过
    /// </summary>
    void Stop();

    /// <summary>
    /// 切换跳过
    /// </summary>
    void Toggle();

    /// <summary>
    /// 检查指定内容是否已读
    /// </summary>
    bool IsRead(string scriptName, int lineIndex);

    /// <summary>
    /// 标记内容为已读
    /// </summary>
    void MarkAsRead(string scriptName, int lineIndex);

    /// <summary>
    /// 清除已读记录
    /// </summary>
    void ClearReadHistory();

    /// <summary>
    /// 获取已读统计
    /// </summary>
    ReadStatistics GetStatistics();

    /// <summary>
    /// 跳过状态变化事件
    /// </summary>
    event Action<bool> OnActiveChanged;

    /// <summary>
    /// 遇到未读内容事件（ReadOnly模式时触发）
    /// </summary>
    event Action OnUnreadEncountered;
}

public class ReadStatistics
{
    public int TotalLines { get; set; }
    public int ReadLines { get; set; }
    public float ReadPercentage => TotalLines > 0 ? (float)ReadLines / TotalLines : 0;
    public Dictionary<string, int> ReadCountByScript { get; set; }
}
```

#### 使用示例
```csharp
public class SkipSettingUI : MonoBehaviour
{
    [Inject] private ISkipService _skip;

    [SerializeField] private Dropdown _modeDropdown;
    [SerializeField] private Button _skipButton;
    [SerializeField] private Image _skipIcon;
    [SerializeField] private TMP_Text _readProgressText;

    [SerializeField] private Color _activeColor = Color.yellow;
    [SerializeField] private Color _inactiveColor = Color.white;

    private void Start()
    {
        // 初始化下拉框
        _modeDropdown.ClearOptions();
        _modeDropdown.AddOptions(new List<string> { "禁用", "仅已读", "全部" });
        _modeDropdown.value = (int)_skip.Mode;

        // 绑定事件
        _modeDropdown.onValueChanged.AddListener(OnModeChanged);
        _skipButton.onClick.AddListener(OnSkipClicked);

        _skip.OnActiveChanged += OnSkipStateChanged;
        _skip.OnUnreadEncountered += OnUnreadEncountered;

        UpdateUI();
    }

    private void OnModeChanged(int value)
    {
        _skip.Mode = (SkipMode)value;

        // 如果切换到禁用，停止跳过
        if (_skip.Mode == SkipMode.Disabled && _skip.IsActive)
        {
            _skip.Stop();
        }
    }

    private void OnSkipClicked()
    {
        if (_skip.Mode == SkipMode.Disabled)
        {
            ShowToast("请先启用跳过模式");
            return;
        }
        _skip.Toggle();
    }

    private void OnSkipStateChanged(bool isActive)
    {
        UpdateUI();
    }

    private void OnUnreadEncountered()
    {
        // ReadOnly模式下遇到未读内容，自动停止
        ShowToast("遇到未读内容，已停止跳过");
        UpdateUI();
    }

    private void UpdateUI()
    {
        _skipIcon.color = _skip.IsActive ? _activeColor : _inactiveColor;

        var stats = _skip.GetStatistics();
        _readProgressText.text = $"已读: {stats.ReadPercentage:P0}";
    }
}
```

#### 实现要点
- 使用哈希集合存储已读行
- 与Naninovel的跳过系统集成
- 已读数据持久化
- 支持按脚本统计

#### 验证方法
- 测试ReadOnly模式的停止行为
- 验证已读记录的持久化
- 测试统计数据准确性

---

### 5.4 音量独立控制

**状态**: [ ] TODO
**优先级**: P1 (重要)
**复杂度**: 低 (1-2天)
**依赖**: 无

#### 功能描述
独立控制语音、BGM、音效的音量，并与游戏音频系统同步。

#### 接口设计
```csharp
/// <summary>
/// 音量控制服务
/// </summary>
public interface INaninovelAudioSettings
{
    /// <summary>主音量 (0-1)</summary>
    float MasterVolume { get; set; }

    /// <summary>语音音量 (0-1)</summary>
    float VoiceVolume { get; set; }

    /// <summary>BGM音量 (0-1)</summary>
    float BgmVolume { get; set; }

    /// <summary>音效音量 (0-1)</summary>
    float SeVolume { get; set; }

    /// <summary>语音是否静音</summary>
    bool IsVoiceMuted { get; set; }

    /// <summary>BGM是否静音</summary>
    bool IsBgmMuted { get; set; }

    /// <summary>音效是否静音</summary>
    bool IsSeMuted { get; set; }

    /// <summary>应用音量设置</summary>
    void Apply();

    /// <summary>与游戏音频系统同步</summary>
    void SyncWithGameAudio();

    /// <summary>音量变化事件</summary>
    event Action<AudioChannel, float> OnVolumeChanged;
}

public enum AudioChannel
{
    Master,
    Voice,
    Bgm,
    Se
}
```

#### 使用示例
```csharp
public class AudioSettingsUI : MonoBehaviour
{
    [Inject] private INaninovelAudioSettings _audioSettings;
    [Inject] private IGameAudioService _gameAudio;

    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _voiceSlider;
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _seSlider;

    [SerializeField] private Toggle _voiceMuteToggle;
    [SerializeField] private Toggle _bgmMuteToggle;
    [SerializeField] private Toggle _seMuteToggle;

    private void Start()
    {
        // 初始化滑块
        _masterSlider.value = _audioSettings.MasterVolume;
        _voiceSlider.value = _audioSettings.VoiceVolume;
        _bgmSlider.value = _audioSettings.BgmVolume;
        _seSlider.value = _audioSettings.SeVolume;

        // 初始化静音开关
        _voiceMuteToggle.isOn = _audioSettings.IsVoiceMuted;
        _bgmMuteToggle.isOn = _audioSettings.IsBgmMuted;
        _seMuteToggle.isOn = _audioSettings.IsSeMuted;

        // 绑定事件
        _masterSlider.onValueChanged.AddListener(v => SetVolume(AudioChannel.Master, v));
        _voiceSlider.onValueChanged.AddListener(v => SetVolume(AudioChannel.Voice, v));
        _bgmSlider.onValueChanged.AddListener(v => SetVolume(AudioChannel.Bgm, v));
        _seSlider.onValueChanged.AddListener(v => SetVolume(AudioChannel.Se, v));

        _voiceMuteToggle.onValueChanged.AddListener(v => _audioSettings.IsVoiceMuted = v);
        _bgmMuteToggle.onValueChanged.AddListener(v => _audioSettings.IsBgmMuted = v);
        _seMuteToggle.onValueChanged.AddListener(v => _audioSettings.IsSeMuted = v);

        // 与游戏音频同步
        _audioSettings.SyncWithGameAudio();
    }

    private void SetVolume(AudioChannel channel, float value)
    {
        switch (channel)
        {
            case AudioChannel.Master:
                _audioSettings.MasterVolume = value;
                break;
            case AudioChannel.Voice:
                _audioSettings.VoiceVolume = value;
                break;
            case AudioChannel.Bgm:
                _audioSettings.BgmVolume = value;
                break;
            case AudioChannel.Se:
                _audioSettings.SeVolume = value;
                break;
        }
        _audioSettings.Apply();
    }

    // 测试按钮
    public void PlayTestVoice()
    {
        _gameAudio.PlayVoice("test_voice");
    }

    public void PlayTestSe()
    {
        _gameAudio.PlaySe("test_se");
    }
}
```

#### 实现要点
- 通过 `IAudioManager` 控制各通道音量
- 实现与 Unity AudioMixer 的同步
- 支持游戏暂停时的音量处理
- 设置持久化

#### 验证方法
- 测试各通道音量调节
- 验证静音功能
- 测试与游戏音频的同步

---

### 5.5 字体大小调整

**状态**: [ ] TODO
**优先级**: P2 (一般)
**复杂度**: 低 (1-2天)
**依赖**: 4.1 对话框样式系统

#### 功能描述
调整对话文字的字体大小，支持预设和自定义大小。

#### 接口设计
```csharp
/// <summary>
/// 字体设置服务
/// </summary>
public interface IFontSettingsService
{
    /// <summary>当前字体大小</summary>
    int FontSize { get; set; }

    /// <summary>字体大小预设</summary>
    FontSizePreset SizePreset { get; set; }

    /// <summary>最小字体大小</summary>
    int MinFontSize { get; }

    /// <summary>最大字体大小</summary>
    int MaxFontSize { get; }

    /// <summary>应用字体设置</summary>
    void Apply();

    /// <summary>字体大小变化事件</summary>
    event Action<int> OnFontSizeChanged;
}

public enum FontSizePreset
{
    Small = 18,
    Medium = 24,
    Large = 30,
    ExtraLarge = 36,
    Custom = -1
}
```

#### 使用示例
```csharp
public class FontSettingsUI : MonoBehaviour
{
    [Inject] private IFontSettingsService _fontSettings;

    [SerializeField] private Dropdown _presetDropdown;
    [SerializeField] private Slider _customSlider;
    [SerializeField] private TMP_Text _previewText;

    private void Start()
    {
        // 初始化预设下拉框
        _presetDropdown.ClearOptions();
        _presetDropdown.AddOptions(new List<string> { "小", "中", "大", "特大", "自定义" });

        // 绑定事件
        _presetDropdown.onValueChanged.AddListener(OnPresetChanged);
        _customSlider.onValueChanged.AddListener(OnCustomSizeChanged);

        _fontSettings.OnFontSizeChanged += UpdatePreview;

        RefreshUI();
    }

    private void OnPresetChanged(int index)
    {
        var presets = new[] {
            FontSizePreset.Small,
            FontSizePreset.Medium,
            FontSizePreset.Large,
            FontSizePreset.ExtraLarge,
            FontSizePreset.Custom
        };

        _fontSettings.SizePreset = presets[index];

        // 自定义时显示滑块
        _customSlider.gameObject.SetActive(presets[index] == FontSizePreset.Custom);

        if (presets[index] != FontSizePreset.Custom)
        {
            _fontSettings.FontSize = (int)presets[index];
        }
        _fontSettings.Apply();
    }

    private void OnCustomSizeChanged(float value)
    {
        _fontSettings.FontSize = Mathf.RoundToInt(value);
        _fontSettings.Apply();
    }

    private void UpdatePreview(int fontSize)
    {
        _previewText.fontSize = fontSize;
    }

    private void RefreshUI()
    {
        _customSlider.minValue = _fontSettings.MinFontSize;
        _customSlider.maxValue = _fontSettings.MaxFontSize;
        _customSlider.value = _fontSettings.FontSize;
        UpdatePreview(_fontSettings.FontSize);
    }
}
```

#### 实现要点
- 修改 TextPrinter 组件的字体大小
- 同步修改角色名、选项等相关UI
- 考虑布局自适应
- 支持 TextMeshPro

#### 验证方法
- 测试各预设大小
- 验证自定义大小范围
- 测试布局适配

---

### 5.6 对话框透明度

**状态**: [ ] TODO
**优先级**: P2 (一般)
**复杂度**: 低 (1-2天)
**依赖**: 4.1 对话框样式系统

#### 功能描述
调整对话框背景的透明度，方便观看背景图或CG。

#### 接口设计
```csharp
/// <summary>
/// 对话框透明度服务
/// </summary>
public interface IDialogueOpacityService
{
    /// <summary>背景透明度 (0-1, 0完全透明)</summary>
    float Opacity { get; set; }

    /// <summary>透明度预设</summary>
    OpacityPreset Preset { get; set; }

    /// <summary>临时隐藏对话框（查看背景）</summary>
    void TemporaryHide();

    /// <summary>从临时隐藏恢复</summary>
    void RestoreFromHide();

    /// <summary>是否处于临时隐藏状态</summary>
    bool IsTemporarilyHidden { get; }

    /// <summary>应用透明度</summary>
    void Apply();

    /// <summary>透明度变化事件</summary>
    event Action<float> OnOpacityChanged;
}

public enum OpacityPreset
{
    Transparent = 0,    // 0%
    Light = 25,         // 25%
    Medium = 50,        // 50%
    Dark = 75,          // 75%
    Opaque = 100,       // 100%
    Custom = -1
}
```

#### 使用示例
```csharp
public class OpacitySettingsUI : MonoBehaviour
{
    [Inject] private IDialogueOpacityService _opacity;

    [SerializeField] private Slider _opacitySlider;
    [SerializeField] private Button[] _presetButtons;
    [SerializeField] private Image _previewBackground;

    private void Start()
    {
        _opacitySlider.value = _opacity.Opacity;
        _opacitySlider.onValueChanged.AddListener(OnOpacityChanged);

        // 预设按钮
        for (int i = 0; i < _presetButtons.Length; i++)
        {
            var preset = (OpacityPreset)(i * 25);
            _presetButtons[i].onClick.AddListener(() => ApplyPreset(preset));
        }

        _opacity.OnOpacityChanged += UpdatePreview;
    }

    private void OnOpacityChanged(float value)
    {
        _opacity.Opacity = value;
        _opacity.Apply();
    }

    private void ApplyPreset(OpacityPreset preset)
    {
        _opacity.Preset = preset;
        _opacitySlider.value = (int)preset / 100f;
        _opacity.Apply();
    }

    private void UpdatePreview(float opacity)
    {
        var color = _previewBackground.color;
        color.a = opacity;
        _previewBackground.color = color;
    }

    // 按住空格临时隐藏
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _opacity.TemporaryHide();
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            _opacity.RestoreFromHide();
        }
    }
}
```

#### 实现要点
- 修改对话框 CanvasGroup 的 alpha
- 临时隐藏使用淡出动画
- 保持文字可读性的最低透明度
- 与对话框样式系统协作

#### 验证方法
- 测试不同透明度效果
- 验证临时隐藏功能
- 测试设置持久化

---

## 6. 演出系统模块

### 6.1 角色立绘管理

**状态**: [ ] TODO
**优先级**: P1 (重要)
**复杂度**: 高 (1-2周)
**依赖**: 无

#### 功能描述
封装角色立绘的显示、切换、动画等功能，提供更友好的API。

#### 接口设计
```csharp
/// <summary>
/// 角色立绘管理服务
/// </summary>
public interface ICharacterSpriteService
{
    /// <summary>
    /// 显示角色
    /// </summary>
    UniTask ShowCharacter(string characterId, CharacterShowOptions options = null);

    /// <summary>
    /// 隐藏角色
    /// </summary>
    UniTask HideCharacter(string characterId, float duration = 0.3f);

    /// <summary>
    /// 隐藏所有角色
    /// </summary>
    UniTask HideAllCharacters(float duration = 0.3f);

    /// <summary>
    /// 切换角色表情
    /// </summary>
    UniTask SetExpression(string characterId, string expression, float duration = 0.1f);

    /// <summary>
    /// 切换角色服装
    /// </summary>
    UniTask SetOutfit(string characterId, string outfit);

    /// <summary>
    /// 移动角色位置
    /// </summary>
    UniTask MoveCharacter(string characterId, CharacterPosition position, float duration = 0.5f);

    /// <summary>
    /// 播放角色动画
    /// </summary>
    UniTask PlayAnimation(string characterId, string animationName);

    /// <summary>
    /// 设置角色高亮状态（说话时高亮）
    /// </summary>
    void SetHighlight(string characterId, bool highlighted);

    /// <summary>
    /// 获取当前显示的角色列表
    /// </summary>
    List<string> GetVisibleCharacters();

    /// <summary>
    /// 角色显示状态变化事件
    /// </summary>
    event Action<string, bool> OnCharacterVisibilityChanged;
}

public class CharacterShowOptions
{
    /// <summary>位置</summary>
    public CharacterPosition Position { get; set; } = CharacterPosition.Center;

    /// <summary>初始表情</summary>
    public string Expression { get; set; }

    /// <summary>服装</summary>
    public string Outfit { get; set; }

    /// <summary>淡入时间</summary>
    public float FadeDuration { get; set; } = 0.3f;

    /// <summary>初始透明度</summary>
    public float InitialAlpha { get; set; } = 0f;

    /// <summary>目标透明度</summary>
    public float TargetAlpha { get; set; } = 1f;

    /// <summary>入场动画</summary>
    public string EnterAnimation { get; set; }

    /// <summary>层级（越大越前）</summary>
    public int SortingOrder { get; set; } = 0;
}

public enum CharacterPosition
{
    Left,
    CenterLeft,
    Center,
    CenterRight,
    Right,
    OffscreenLeft,
    OffscreenRight
}
```

#### 使用示例
```csharp
public class SceneDirector : MonoBehaviour
{
    [Inject] private ICharacterSpriteService _character;

    public async UniTask PlayScene1()
    {
        // 艾丽丝从左侧入场
        await _character.ShowCharacter("Alice", new CharacterShowOptions
        {
            Position = CharacterPosition.Left,
            Expression = "smile",
            EnterAnimation = "slideIn"
        });

        // 显示对话...

        // 鲍勃从右侧入场
        await _character.ShowCharacter("Bob", new CharacterShowOptions
        {
            Position = CharacterPosition.Right,
            Expression = "neutral"
        });

        // 艾丽丝说话时高亮
        _character.SetHighlight("Alice", true);
        _character.SetHighlight("Bob", false);

        // 播放对话...

        // 艾丽丝表情变化
        await _character.SetExpression("Alice", "surprised");

        // 鲍勃移动到中间
        await _character.MoveCharacter("Bob", CharacterPosition.Center);

        // 场景结束，所有角色退场
        await _character.HideAllCharacters();
    }
}
```

#### 实现要点
- 封装 `ICharacterManager` 操作
- 预定义位置坐标映射
- 说话角色高亮效果（调暗其他角色）
- 表情资源命名约定

#### 验证方法
- 测试各位置角色显示
- 验证表情切换平滑
- 测试多角色同时动画

---

### 6.2 背景与场景集成

**状态**: [ ] TODO
**优先级**: P1 (重要)
**复杂度**: 中 (3-5天)
**依赖**: 无

#### 功能描述
封装背景图和场景切换功能，支持与游戏场景系统的联动。

#### 接口设计
```csharp
/// <summary>
/// 背景管理服务
/// </summary>
public interface IBackgroundService
{
    /// <summary>
    /// 切换背景
    /// </summary>
    UniTask ChangeBackground(string backgroundId, BackgroundTransition transition = null);

    /// <summary>
    /// 显示背景叠加层
    /// </summary>
    UniTask ShowOverlay(string overlayId, float alpha = 0.5f);

    /// <summary>
    /// 隐藏背景叠加层
    /// </summary>
    UniTask HideOverlay(float duration = 0.3f);

    /// <summary>
    /// 设置背景效果（模糊、调色等）
    /// </summary>
    void SetEffect(BackgroundEffect effect);

    /// <summary>
    /// 清除背景效果
    /// </summary>
    void ClearEffect();

    /// <summary>
    /// 当前背景ID
    /// </summary>
    string CurrentBackgroundId { get; }

    /// <summary>
    /// 背景变化事件
    /// </summary>
    event Action<string, string> OnBackgroundChanged;
}

public class BackgroundTransition
{
    /// <summary>过渡类型</summary>
    public TransitionType Type { get; set; } = TransitionType.Crossfade;

    /// <summary>过渡时长</summary>
    public float Duration { get; set; } = 0.5f;

    /// <summary>过渡曲线</summary>
    public AnimationCurve Curve { get; set; }

    /// <summary>自定义过渡材质</summary>
    public Material TransitionMaterial { get; set; }
}

public enum TransitionType
{
    Instant,
    Crossfade,
    FadeToBlack,
    FadeToWhite,
    SlideLeft,
    SlideRight,
    Dissolve,
    Custom
}

public class BackgroundEffect
{
    public float BlurAmount { get; set; } = 0f;
    public Color Tint { get; set; } = Color.white;
    public float Brightness { get; set; } = 1f;
    public float Saturation { get; set; } = 1f;
    public float Vignette { get; set; } = 0f;
}
```

#### 使用示例
```csharp
public class SceneController : MonoBehaviour
{
    [Inject] private IBackgroundService _background;
    [Inject] private IGameSceneService _gameScene;

    public async UniTask EnterSchool()
    {
        // 切换到学校背景
        await _background.ChangeBackground("school_entrance", new BackgroundTransition
        {
            Type = TransitionType.Crossfade,
            Duration = 1f
        });

        // 同步加载游戏场景（如果需要）
        await _gameScene.LoadSceneAdditive("School");
    }

    public async UniTask TimePassEffect()
    {
        // 时间流逝效果：淡出到黑色，换背景，淡入
        await _background.ChangeBackground("school_evening", new BackgroundTransition
        {
            Type = TransitionType.FadeToBlack,
            Duration = 1.5f
        });
    }

    public async UniTask FlashbackStart()
    {
        // 回忆效果：模糊 + 调色
        _background.SetEffect(new BackgroundEffect
        {
            BlurAmount = 2f,
            Saturation = 0.5f,
            Tint = new Color(0.9f, 0.85f, 0.8f)
        });

        await _background.ChangeBackground("past_location");
    }

    public async UniTask FlashbackEnd()
    {
        // 清除回忆效果
        _background.ClearEffect();
        await _background.ChangeBackground("current_location");
    }
}
```

#### 实现要点
- 封装 `IBackgroundManager` 操作
- 实现多种过渡效果
- 支持后处理效果
- 与 Unity 场景系统协作

#### 验证方法
- 测试各种过渡效果
- 验证效果叠加
- 测试背景加载性能

---

### 6.3 特效/转场封装

**状态**: [ ] TODO
**优先级**: P2 (一般)
**复杂度**: 中 (3-5天)
**依赖**: 6.2 背景与场景集成

#### 功能描述
封装常用特效和转场效果，提供简单的调用接口。

#### 接口设计
```csharp
/// <summary>
/// 特效服务
/// </summary>
public interface IEffectService
{
    /// <summary>
    /// 播放屏幕特效
    /// </summary>
    UniTask PlayScreenEffect(string effectId, EffectOptions options = null);

    /// <summary>
    /// 播放粒子特效
    /// </summary>
    UniTask PlayParticleEffect(string effectId, Vector2 position);

    /// <summary>
    /// 屏幕震动
    /// </summary>
    UniTask ShakeScreen(float intensity = 1f, float duration = 0.5f);

    /// <summary>
    /// 屏幕闪烁
    /// </summary>
    UniTask FlashScreen(Color color, float duration = 0.3f);

    /// <summary>
    /// 淡入淡出
    /// </summary>
    UniTask Fade(FadeDirection direction, float duration = 0.5f, Color? color = null);

    /// <summary>
    /// 停止所有特效
    /// </summary>
    void StopAllEffects();

    /// <summary>
    /// 注册自定义特效
    /// </summary>
    void RegisterEffect(string effectId, ICustomEffect effect);
}

public class EffectOptions
{
    public float Duration { get; set; } = 1f;
    public float Intensity { get; set; } = 1f;
    public bool Loop { get; set; } = false;
    public Dictionary<string, object> CustomParams { get; set; }
}

public enum FadeDirection
{
    In,
    Out
}

public interface ICustomEffect
{
    UniTask Play(EffectOptions options);
    void Stop();
}
```

#### 使用示例
```csharp
public class CutsceneEffects : MonoBehaviour
{
    [Inject] private IEffectService _effects;

    public async UniTask ExplosionScene()
    {
        // 闪白
        await _effects.FlashScreen(Color.white, 0.2f);

        // 屏幕震动
        await _effects.ShakeScreen(2f, 1f);

        // 粒子特效
        await _effects.PlayParticleEffect("explosion", new Vector2(0.5f, 0.5f));
    }

    public async UniTask RainEffect()
    {
        // 持续下雨效果
        await _effects.PlayScreenEffect("rain", new EffectOptions
        {
            Loop = true,
            Intensity = 0.8f
        });
    }

    public async UniTask SceneTransition()
    {
        // 淡出
        await _effects.Fade(FadeDirection.Out, 1f, Color.black);

        // 切换场景...

        // 淡入
        await _effects.Fade(FadeDirection.In, 1f, Color.black);
    }
}
```

#### 实现要点
- 封装 Naninovel 特效命令
- 支持自定义特效注册
- 效果队列和取消
- 性能优化

#### 验证方法
- 测试各种内置特效
- 验证自定义特效注册
- 测试特效叠加

---

### 6.4 CG回廊系统

**状态**: [ ] TODO
**优先级**: P2 (一般)
**复杂度**: 高 (1-2周)
**依赖**: 3.1 存档同步机制

#### 功能描述
实现CG回廊功能，记录和展示已解锁的CG图片。

#### 接口设计
```csharp
/// <summary>
/// CG回廊服务
/// </summary>
public interface ICGGalleryService
{
    /// <summary>
    /// 获取所有CG信息
    /// </summary>
    List<CGInfo> GetAllCGs();

    /// <summary>
    /// 获取已解锁的CG
    /// </summary>
    List<CGInfo> GetUnlockedCGs();

    /// <summary>
    /// 解锁CG
    /// </summary>
    void UnlockCG(string cgId);

    /// <summary>
    /// 检查CG是否已解锁
    /// </summary>
    bool IsUnlocked(string cgId);

    /// <summary>
    /// 获取解锁进度
    /// </summary>
    (int unlocked, int total) GetProgress();

    /// <summary>
    /// 显示CG查看器
    /// </summary>
    UniTask ShowViewer(string cgId);

    /// <summary>
    /// 关闭CG查看器
    /// </summary>
    void CloseViewer();

    /// <summary>
    /// CG解锁事件
    /// </summary>
    event Action<string> OnCGUnlocked;
}

public class CGInfo
{
    /// <summary>CG唯一ID</summary>
    public string Id { get; set; }

    /// <summary>显示名称</summary>
    public string DisplayName { get; set; }

    /// <summary>所属分类/章节</summary>
    public string Category { get; set; }

    /// <summary>缩略图路径</summary>
    public string ThumbnailPath { get; set; }

    /// <summary>完整图路径</summary>
    public string FullImagePath { get; set; }

    /// <summary>是否已解锁</summary>
    public bool IsUnlocked { get; set; }

    /// <summary>解锁条件描述（未解锁时显示）</summary>
    public string UnlockHint { get; set; }

    /// <summary>排序顺序</summary>
    public int SortOrder { get; set; }
}
```

#### 使用示例
```csharp
public class CGGalleryUI : MonoBehaviour
{
    [Inject] private ICGGalleryService _gallery;

    [SerializeField] private Transform _gridContainer;
    [SerializeField] private CGThumbnailItem _thumbnailPrefab;
    [SerializeField] private TMP_Text _progressText;

    private void Start()
    {
        RefreshGallery();
        _gallery.OnCGUnlocked += OnCGUnlocked;
    }

    private void RefreshGallery()
    {
        // 清空
        foreach (Transform child in _gridContainer)
            Destroy(child.gameObject);

        // 获取所有CG
        var allCGs = _gallery.GetAllCGs();

        // 按分类分组显示
        var grouped = allCGs.GroupBy(c => c.Category);

        foreach (var group in grouped)
        {
            // 创建分类标题...

            foreach (var cg in group.OrderBy(c => c.SortOrder))
            {
                var item = Instantiate(_thumbnailPrefab, _gridContainer);
                item.Setup(cg, OnThumbnailClicked);
            }
        }

        // 更新进度
        var (unlocked, total) = _gallery.GetProgress();
        _progressText.text = $"收集进度: {unlocked}/{total}";
    }

    private async void OnThumbnailClicked(CGInfo cg)
    {
        if (cg.IsUnlocked)
        {
            await _gallery.ShowViewer(cg.Id);
        }
        else
        {
            ShowToast(cg.UnlockHint ?? "尚未解锁");
        }
    }

    private void OnCGUnlocked(string cgId)
    {
        RefreshGallery();
        ShowToast("新CG已解锁！");
    }
}

// 在脚本中解锁CG
public class CGTrigger : MonoBehaviour
{
    [Inject] private ICGGalleryService _gallery;

    // 从Naninovel脚本调用
    [CommandAlias("unlockCG")]
    public class UnlockCGCommand : Command
    {
        [ParameterAlias("id")]
        public StringParameter CGId;

        public override UniTask ExecuteAsync(CancellationToken ct)
        {
            var gallery = Engine.GetService<ICGGalleryService>();
            gallery.UnlockCG(CGId);
            return UniTask.CompletedTask;
        }
    }
}
```

#### 实现要点
- CG资源管理和懒加载
- 缩略图生成和缓存
- 解锁状态持久化
- 查看器支持缩放和滑动

#### 验证方法
- 测试解锁功能
- 验证进度保存
- 测试大量CG的性能

---

### 6.5 音乐管理集成

**状态**: [ ] TODO
**优先级**: P2 (一般)
**复杂度**: 中 (3-5天)
**依赖**: 无

#### 功能描述
封装BGM和音效播放，支持音乐回廊功能。

#### 接口设计
```csharp
/// <summary>
/// 音乐管理服务
/// </summary>
public interface IMusicService
{
    /// <summary>
    /// 播放BGM
    /// </summary>
    UniTask PlayBGM(string musicId, MusicPlayOptions options = null);

    /// <summary>
    /// 停止BGM
    /// </summary>
    UniTask StopBGM(float fadeOut = 1f);

    /// <summary>
    /// 暂停BGM
    /// </summary>
    void PauseBGM();

    /// <summary>
    /// 恢复BGM
    /// </summary>
    void ResumeBGM();

    /// <summary>
    /// 播放音效
    /// </summary>
    void PlaySE(string seId, float volume = 1f);

    /// <summary>
    /// 当前播放的BGM
    /// </summary>
    string CurrentBGM { get; }

    /// <summary>
    /// 解锁音乐（用于音乐回廊）
    /// </summary>
    void UnlockMusic(string musicId);

    /// <summary>
    /// 获取已解锁音乐列表
    /// </summary>
    List<MusicInfo> GetUnlockedMusic();

    /// <summary>
    /// BGM变化事件
    /// </summary>
    event Action<string> OnBGMChanged;
}

public class MusicPlayOptions
{
    /// <summary>淡入时间</summary>
    public float FadeIn { get; set; } = 1f;

    /// <summary>是否循环</summary>
    public bool Loop { get; set; } = true;

    /// <summary>起始位置（秒）</summary>
    public float StartTime { get; set; } = 0f;

    /// <summary>音量</summary>
    public float Volume { get; set; } = 1f;

    /// <summary>交叉淡化（与当前BGM）</summary>
    public bool Crossfade { get; set; } = true;
}

public class MusicInfo
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Composer { get; set; }
    public float Duration { get; set; }
    public string Category { get; set; }
    public bool IsUnlocked { get; set; }
}
```

#### 使用示例
```csharp
public class MusicController : MonoBehaviour
{
    [Inject] private IMusicService _music;

    public async UniTask EnterBattleScene()
    {
        // 切换到战斗BGM
        await _music.PlayBGM("battle_theme", new MusicPlayOptions
        {
            FadeIn = 0.5f,
            Crossfade = true
        });

        // 播放战斗开始音效
        _music.PlaySE("battle_start");
    }

    public async UniTask VictoryScene()
    {
        // 停止战斗BGM
        await _music.StopBGM(0.5f);

        // 播放胜利音乐
        await _music.PlayBGM("victory", new MusicPlayOptions
        {
            Loop = false
        });

        // 解锁胜利曲
        _music.UnlockMusic("victory");
    }
}

// 音乐回廊UI
public class MusicGalleryUI : MonoBehaviour
{
    [Inject] private IMusicService _music;

    private void ShowMusicList()
    {
        var unlockedMusic = _music.GetUnlockedMusic();

        foreach (var music in unlockedMusic)
        {
            // 显示音乐列表项
            CreateMusicListItem(music);
        }
    }

    private void OnMusicSelected(MusicInfo music)
    {
        _music.PlayBGM(music.Id, new MusicPlayOptions { Loop = true });
    }
}
```

#### 实现要点
- 封装 `IAudioManager` 操作
- BGM交叉淡化实现
- 音乐解锁状态持久化
- 与游戏音频系统同步

#### 验证方法
- 测试BGM播放和切换
- 验证交叉淡化效果
- 测试音乐回廊功能

---

## 7. 本地化模块

### 7.1 多语言脚本切换

**状态**: [ ] TODO
**优先级**: P2 (一般)
**复杂度**: 高 (1-2周)
**依赖**: 无

#### 功能描述
支持多语言脚本的管理和切换，实现对话内容的本地化。

#### 接口设计
```csharp
/// <summary>
/// 本地化服务
/// </summary>
public interface INaninovelLocalizationService
{
    /// <summary>
    /// 当前语言
    /// </summary>
    string CurrentLanguage { get; }

    /// <summary>
    /// 支持的语言列表
    /// </summary>
    List<LanguageInfo> SupportedLanguages { get; }

    /// <summary>
    /// 切换语言
    /// </summary>
    UniTask SetLanguage(string languageCode);

    /// <summary>
    /// 获取本地化文本
    /// </summary>
    string GetLocalizedText(string key);

    /// <summary>
    /// 检查资源是否已下载（用于语言包）
    /// </summary>
    bool IsLanguageAvailable(string languageCode);

    /// <summary>
    /// 下载语言包
    /// </summary>
    UniTask DownloadLanguagePack(string languageCode, IProgress<float> progress = null);

    /// <summary>
    /// 语言变化事件
    /// </summary>
    event Action<string> OnLanguageChanged;
}

public class LanguageInfo
{
    /// <summary>语言代码 (如 "zh-CN", "en-US")</summary>
    public string Code { get; set; }

    /// <summary>本地化显示名 (如 "简体中文", "English")</summary>
    public string DisplayName { get; set; }

    /// <summary>原生名称 (如 "中文", "English")</summary>
    public string NativeName { get; set; }

    /// <summary>是否已下载</summary>
    public bool IsDownloaded { get; set; }

    /// <summary>语言包大小（字节）</summary>
    public long PackageSize { get; set; }
}
```

#### 使用示例
```csharp
public class LanguageSettingsUI : MonoBehaviour
{
    [Inject] private INaninovelLocalizationService _localization;

    [SerializeField] private Dropdown _languageDropdown;
    [SerializeField] private Button _downloadButton;
    [SerializeField] private Slider _downloadProgress;

    private LanguageInfo _selectedLanguage;

    private void Start()
    {
        RefreshLanguageList();
        _languageDropdown.onValueChanged.AddListener(OnLanguageSelected);
        _downloadButton.onClick.AddListener(OnDownloadClicked);

        _localization.OnLanguageChanged += OnLanguageChanged;
    }

    private void RefreshLanguageList()
    {
        _languageDropdown.ClearOptions();

        var languages = _localization.SupportedLanguages;
        var options = languages.Select(l =>
            l.IsDownloaded ? l.DisplayName : $"{l.DisplayName} (需下载)"
        ).ToList();

        _languageDropdown.AddOptions(options);

        // 选中当前语言
        var currentIndex = languages.FindIndex(l => l.Code == _localization.CurrentLanguage);
        if (currentIndex >= 0)
            _languageDropdown.value = currentIndex;
    }

    private void OnLanguageSelected(int index)
    {
        _selectedLanguage = _localization.SupportedLanguages[index];

        if (_localization.IsLanguageAvailable(_selectedLanguage.Code))
        {
            // 直接切换
            ApplyLanguage(_selectedLanguage.Code);
        }
        else
        {
            // 显示下载按钮
            _downloadButton.gameObject.SetActive(true);
            var sizeText = FormatSize(_selectedLanguage.PackageSize);
            _downloadButton.GetComponentInChildren<TMP_Text>().text = $"下载 ({sizeText})";
        }
    }

    private async void OnDownloadClicked()
    {
        _downloadButton.interactable = false;
        _downloadProgress.gameObject.SetActive(true);

        var progress = new Progress<float>(p => _downloadProgress.value = p);

        try
        {
            await _localization.DownloadLanguagePack(_selectedLanguage.Code, progress);
            ApplyLanguage(_selectedLanguage.Code);
        }
        catch (Exception e)
        {
            ShowError($"下载失败: {e.Message}");
        }
        finally
        {
            _downloadButton.interactable = true;
            _downloadProgress.gameObject.SetActive(false);
        }
    }

    private async void ApplyLanguage(string code)
    {
        await _localization.SetLanguage(code);
        ShowToast($"已切换到{_selectedLanguage.DisplayName}");
    }

    private void OnLanguageChanged(string newLanguage)
    {
        RefreshLanguageList();
        // 刷新UI文本...
    }
}
```

#### 实现要点
- 使用 Naninovel 的本地化系统
- 支持脚本文本和UI文本
- 语言包的下载和管理
- 字体回退支持

#### 验证方法
- 测试语言切换功能
- 验证脚本文本正确显示
- 测试语言包下载

---

### 7.2 字体回退机制

**状态**: [ ] TODO
**优先级**: P2 (一般)
**复杂度**: 中 (3-5天)
**依赖**: 7.1 多语言脚本切换

#### 功能描述
为不同语言配置适当的字体，支持字体回退以确保所有字符正确显示。

#### 接口设计
```csharp
/// <summary>
/// 字体管理服务
/// </summary>
public interface IFontService
{
    /// <summary>
    /// 设置当前语言的字体配置
    /// </summary>
    void SetFontConfig(string languageCode, FontConfig config);

    /// <summary>
    /// 获取语言的字体配置
    /// </summary>
    FontConfig GetFontConfig(string languageCode);

    /// <summary>
    /// 应用字体配置
    /// </summary>
    void ApplyFont(string languageCode);

    /// <summary>
    /// 添加回退字体
    /// </summary>
    void AddFallbackFont(TMP_FontAsset font, int priority = 0);

    /// <summary>
    /// 检查字体是否支持文本
    /// </summary>
    bool CanRenderText(string text, TMP_FontAsset font);

    /// <summary>
    /// 字体变化事件
    /// </summary>
    event Action<FontConfig> OnFontChanged;
}

[Serializable]
public class FontConfig
{
    /// <summary>主字体</summary>
    public TMP_FontAsset PrimaryFont { get; set; }

    /// <summary>回退字体列表</summary>
    public List<TMP_FontAsset> FallbackFonts { get; set; }

    /// <summary>字体大小缩放（不同语言可能需要不同大小）</summary>
    public float SizeScale { get; set; } = 1f;

    /// <summary>行间距调整</summary>
    public float LineSpacingAdjust { get; set; } = 0f;

    /// <summary>字符间距调整</summary>
    public float CharacterSpacingAdjust { get; set; } = 0f;
}
```

#### 使用示例
```csharp
public class FontManager : MonoBehaviour
{
    [Inject] private IFontService _fontService;
    [Inject] private INaninovelLocalizationService _localization;

    [SerializeField] private TMP_FontAsset _chineseFont;
    [SerializeField] private TMP_FontAsset _japaneseFont;
    [SerializeField] private TMP_FontAsset _englishFont;
    [SerializeField] private TMP_FontAsset _emojiFallback;

    private void Start()
    {
        // 配置各语言字体
        _fontService.SetFontConfig("zh-CN", new FontConfig
        {
            PrimaryFont = _chineseFont,
            FallbackFonts = new List<TMP_FontAsset> { _emojiFallback },
            SizeScale = 1f
        });

        _fontService.SetFontConfig("ja-JP", new FontConfig
        {
            PrimaryFont = _japaneseFont,
            FallbackFonts = new List<TMP_FontAsset> { _chineseFont, _emojiFallback },
            SizeScale = 0.95f  // 日文略小一点
        });

        _fontService.SetFontConfig("en-US", new FontConfig
        {
            PrimaryFont = _englishFont,
            FallbackFonts = new List<TMP_FontAsset> { _emojiFallback },
            SizeScale = 1.1f,  // 英文字体略大
            CharacterSpacingAdjust = -1f
        });

        // 监听语言变化
        _localization.OnLanguageChanged += OnLanguageChanged;

        // 应用当前语言字体
        _fontService.ApplyFont(_localization.CurrentLanguage);
    }

    private void OnLanguageChanged(string languageCode)
    {
        _fontService.ApplyFont(languageCode);
    }
}
```

#### 实现要点
- TMP_FontAsset 的运行时配置
- 字体回退链的正确设置
- 考虑字体加载性能
- 支持动态字体 (Dynamic SDF)

#### 验证方法
- 测试各语言字符显示
- 验证Emoji等特殊字符
- 测试混合语言文本

---

### 7.3 语音包热切换

**状态**: [ ] TODO
**优先级**: P3 (低)
**复杂度**: 高 (1-2周)
**依赖**: 7.1 多语言脚本切换

#### 功能描述
支持独立于文本语言的语音语言切换，实现语音的热更新。

#### 接口设计
```csharp
/// <summary>
/// 语音本地化服务
/// </summary>
public interface IVoiceLocalizationService
{
    /// <summary>
    /// 当前语音语言
    /// </summary>
    string CurrentVoiceLanguage { get; }

    /// <summary>
    /// 可用的语音语言列表
    /// </summary>
    List<VoiceLanguageInfo> AvailableVoiceLanguages { get; }

    /// <summary>
    /// 切换语音语言
    /// </summary>
    UniTask SetVoiceLanguage(string languageCode);

    /// <summary>
    /// 下载语音包
    /// </summary>
    UniTask DownloadVoicePack(string languageCode, IProgress<float> progress = null);

    /// <summary>
    /// 删除语音包
    /// </summary>
    UniTask DeleteVoicePack(string languageCode);

    /// <summary>
    /// 获取语音包状态
    /// </summary>
    VoicePackStatus GetPackStatus(string languageCode);

    /// <summary>
    /// 预加载角色语音
    /// </summary>
    UniTask PreloadCharacterVoices(string characterId);

    /// <summary>
    /// 语音语言变化事件
    /// </summary>
    event Action<string> OnVoiceLanguageChanged;
}

public class VoiceLanguageInfo
{
    public string Code { get; set; }
    public string DisplayName { get; set; }
    public long PackageSizeBytes { get; set; }
    public VoicePackStatus Status { get; set; }
}

public enum VoicePackStatus
{
    NotDownloaded,
    Downloading,
    Downloaded,
    UpdateAvailable
}
```

#### 使用示例
```csharp
public class VoiceSettingsUI : MonoBehaviour
{
    [Inject] private IVoiceLocalizationService _voiceLocalization;

    [SerializeField] private Dropdown _voiceLanguageDropdown;
    [SerializeField] private Button _downloadButton;
    [SerializeField] private Button _deleteButton;
    [SerializeField] private Slider _progressSlider;
    [SerializeField] private TMP_Text _sizeText;

    private VoiceLanguageInfo _selectedVoice;

    private void Start()
    {
        RefreshUI();
        _voiceLanguageDropdown.onValueChanged.AddListener(OnVoiceLanguageSelected);
        _downloadButton.onClick.AddListener(OnDownloadClicked);
        _deleteButton.onClick.AddListener(OnDeleteClicked);
    }

    private void RefreshUI()
    {
        var voices = _voiceLocalization.AvailableVoiceLanguages;

        _voiceLanguageDropdown.ClearOptions();
        _voiceLanguageDropdown.AddOptions(voices.Select(v =>
        {
            var status = v.Status == VoicePackStatus.Downloaded ? "✓" :
                         v.Status == VoicePackStatus.Downloading ? "..." : "";
            return $"{v.DisplayName} {status}";
        }).ToList());
    }

    private void OnVoiceLanguageSelected(int index)
    {
        _selectedVoice = _voiceLocalization.AvailableVoiceLanguages[index];

        var status = _voiceLocalization.GetPackStatus(_selectedVoice.Code);

        _downloadButton.gameObject.SetActive(status == VoicePackStatus.NotDownloaded);
        _deleteButton.gameObject.SetActive(status == VoicePackStatus.Downloaded);

        if (status == VoicePackStatus.NotDownloaded)
        {
            _sizeText.text = $"大小: {FormatSize(_selectedVoice.PackageSizeBytes)}";
        }

        if (status == VoicePackStatus.Downloaded)
        {
            // 直接切换
            ApplyVoiceLanguage(_selectedVoice.Code);
        }
    }

    private async void OnDownloadClicked()
    {
        _downloadButton.interactable = false;
        _progressSlider.gameObject.SetActive(true);

        var progress = new Progress<float>(p => _progressSlider.value = p);

        try
        {
            await _voiceLocalization.DownloadVoicePack(_selectedVoice.Code, progress);
            ApplyVoiceLanguage(_selectedVoice.Code);
        }
        catch (Exception e)
        {
            ShowError($"下载失败: {e.Message}");
        }
        finally
        {
            _downloadButton.interactable = true;
            _progressSlider.gameObject.SetActive(false);
            RefreshUI();
        }
    }

    private async void OnDeleteClicked()
    {
        var confirmed = await ShowConfirmDialog("确定删除此语音包？");
        if (confirmed)
        {
            await _voiceLocalization.DeleteVoicePack(_selectedVoice.Code);
            RefreshUI();
        }
    }

    private async void ApplyVoiceLanguage(string code)
    {
        await _voiceLocalization.SetVoiceLanguage(code);
        ShowToast($"语音已切换到{_selectedVoice.DisplayName}");
    }
}
```

#### 实现要点
- Addressable Assets 的运行时加载
- 语音包的下载和存储管理
- 语音文件映射（不同语言可能文件名不同）
- 支持部分下载（仅下载特定章节）

#### 验证方法
- 测试语音包下载和删除
- 验证语音切换不影响文本
- 测试断点续传

---

## 8. 调试与工具模块

### 8.1 脚本预加载策略

**状态**: [ ] TODO
**优先级**: P1 (重要)
**复杂度**: 中 (3-5天)
**依赖**: 无

#### 功能描述
优化脚本加载策略，支持预加载和缓存以减少运行时加载延迟。

#### 接口设计
```csharp
/// <summary>
/// 脚本预加载服务
/// </summary>
public interface IScriptPreloadService
{
    /// <summary>
    /// 预加载指定脚本
    /// </summary>
    UniTask PreloadScript(string scriptName);

    /// <summary>
    /// 批量预加载脚本
    /// </summary>
    UniTask PreloadScripts(IEnumerable<string> scriptNames, IProgress<float> progress = null);

    /// <summary>
    /// 预加载章节（包含所有相关脚本）
    /// </summary>
    UniTask PreloadChapter(string chapterId);

    /// <summary>
    /// 卸载已缓存的脚本
    /// </summary>
    void UnloadScript(string scriptName);

    /// <summary>
    /// 清理所有缓存
    /// </summary>
    void ClearCache();

    /// <summary>
    /// 获取缓存状态
    /// </summary>
    PreloadCacheStatus GetCacheStatus();

    /// <summary>
    /// 设置预加载策略
    /// </summary>
    void SetStrategy(PreloadStrategy strategy);

    /// <summary>
    /// 脚本加载事件
    /// </summary>
    event Action<string, float> OnScriptLoading;
}

public class PreloadCacheStatus
{
    public int CachedScriptCount { get; set; }
    public long MemoryUsageBytes { get; set; }
    public List<string> CachedScripts { get; set; }
    public int MaxCacheSize { get; set; }
}

public class PreloadStrategy
{
    /// <summary>自动预加载下一个可能的脚本</summary>
    public bool AutoPreloadNext { get; set; } = true;

    /// <summary>预加载相邻脚本数量</summary>
    public int AdjacentScriptCount { get; set; } = 2;

    /// <summary>最大缓存脚本数</summary>
    public int MaxCachedScripts { get; set; } = 10;

    /// <summary>低内存时自动清理</summary>
    public bool AutoCleanOnLowMemory { get; set; } = true;

    /// <summary>内存阈值（MB）</summary>
    public int MemoryThresholdMB { get; set; } = 100;
}
```

#### 使用示例
```csharp
public class ScriptLoadManager : MonoBehaviour
{
    [Inject] private IScriptPreloadService _preload;

    private void Start()
    {
        // 设置预加载策略
        _preload.SetStrategy(new PreloadStrategy
        {
            AutoPreloadNext = true,
            AdjacentScriptCount = 3,
            MaxCachedScripts = 15
        });

        // 监听加载进度
        _preload.OnScriptLoading += OnScriptLoading;
    }

    // 进入新章节时预加载
    public async UniTask OnChapterEnter(string chapterId)
    {
        ShowLoadingUI();

        var progress = new Progress<float>(p => UpdateLoadingProgress(p));
        await _preload.PreloadChapter(chapterId);

        HideLoadingUI();
    }

    // 游戏启动时预加载开场脚本
    public async UniTask PreloadStartupScripts()
    {
        await _preload.PreloadScripts(new[]
        {
            "Title/Main",
            "Chapter1/Opening",
            "Common/Tutorial"
        });
    }

    private void OnScriptLoading(string scriptName, float progress)
    {
        Debug.Log($"Loading {scriptName}: {progress:P0}");
    }

    // 内存紧张时清理
    private void OnLowMemoryWarning()
    {
        _preload.ClearCache();
    }
}
```

#### 实现要点
- 使用 `IScriptManager` 的预加载功能
- 实现LRU缓存策略
- 后台异步加载不阻塞主线程
- 监控内存使用

#### 验证方法
- 测试预加载后的加载速度
- 验证缓存策略正确
- 测试内存自动清理

---

### 8.2 性能监控

**状态**: [ ] TODO
**优先级**: P2 (一般)
**复杂度**: 中 (3-5天)
**依赖**: 无

#### 功能描述
提供Naninovel运行时的性能监控，包括帧率、内存、加载时间等指标。

#### 接口设计
```csharp
/// <summary>
/// 性能监控服务
/// </summary>
public interface INaninovelPerformanceMonitor
{
    /// <summary>
    /// 启用监控
    /// </summary>
    void Enable();

    /// <summary>
    /// 禁用监控
    /// </summary>
    void Disable();

    /// <summary>
    /// 是否启用
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// 获取当前性能指标
    /// </summary>
    PerformanceMetrics GetMetrics();

    /// <summary>
    /// 获取历史性能数据
    /// </summary>
    List<PerformanceSnapshot> GetHistory(TimeSpan duration);

    /// <summary>
    /// 设置性能警告阈值
    /// </summary>
    void SetWarningThresholds(PerformanceThresholds thresholds);

    /// <summary>
    /// 性能警告事件
    /// </summary>
    event Action<PerformanceWarning> OnPerformanceWarning;

    /// <summary>
    /// 导出性能报告
    /// </summary>
    string ExportReport();
}

public class PerformanceMetrics
{
    /// <summary>当前帧率</summary>
    public float CurrentFPS { get; set; }

    /// <summary>平均帧率</summary>
    public float AverageFPS { get; set; }

    /// <summary>Naninovel内存使用（MB）</summary>
    public float MemoryUsageMB { get; set; }

    /// <summary>已加载脚本数</summary>
    public int LoadedScriptCount { get; set; }

    /// <summary>已加载资源数</summary>
    public int LoadedAssetCount { get; set; }

    /// <summary>平均脚本加载时间（毫秒）</summary>
    public float AvgScriptLoadTimeMs { get; set; }

    /// <summary>当前场景角色数</summary>
    public int ActiveCharacterCount { get; set; }

    /// <summary>活动特效数</summary>
    public int ActiveEffectCount { get; set; }

    /// <summary>UI渲染批次</summary>
    public int UIBatchCount { get; set; }
}

public class PerformanceSnapshot
{
    public DateTime Timestamp { get; set; }
    public PerformanceMetrics Metrics { get; set; }
    public string CurrentScript { get; set; }
    public int CurrentLine { get; set; }
}

public class PerformanceThresholds
{
    public float MinFPS { get; set; } = 30f;
    public float MaxMemoryMB { get; set; } = 500f;
    public float MaxLoadTimeMs { get; set; } = 1000f;
}

public class PerformanceWarning
{
    public WarningType Type { get; set; }
    public string Message { get; set; }
    public PerformanceMetrics Metrics { get; set; }
}

public enum WarningType
{
    LowFPS,
    HighMemory,
    SlowLoading,
    TooManyActors
}
```

#### 使用示例
```csharp
public class PerformanceDebugUI : MonoBehaviour
{
    [Inject] private INaninovelPerformanceMonitor _perfMonitor;

    [SerializeField] private TMP_Text _fpsText;
    [SerializeField] private TMP_Text _memoryText;
    [SerializeField] private TMP_Text _loadTimeText;
    [SerializeField] private GameObject _warningPanel;

    private void Start()
    {
        #if DEVELOPMENT_BUILD || UNITY_EDITOR
        _perfMonitor.Enable();
        _perfMonitor.SetWarningThresholds(new PerformanceThresholds
        {
            MinFPS = 30f,
            MaxMemoryMB = 400f,
            MaxLoadTimeMs = 500f
        });

        _perfMonitor.OnPerformanceWarning += OnWarning;
        #else
        gameObject.SetActive(false);
        #endif
    }

    private void Update()
    {
        if (!_perfMonitor.IsEnabled) return;

        var metrics = _perfMonitor.GetMetrics();

        _fpsText.text = $"FPS: {metrics.CurrentFPS:F1} (avg: {metrics.AverageFPS:F1})";
        _memoryText.text = $"Memory: {metrics.MemoryUsageMB:F1} MB";
        _loadTimeText.text = $"Load: {metrics.AvgScriptLoadTimeMs:F0} ms";

        // 颜色警告
        _fpsText.color = metrics.CurrentFPS < 30 ? Color.red : Color.white;
        _memoryText.color = metrics.MemoryUsageMB > 400 ? Color.yellow : Color.white;
    }

    private void OnWarning(PerformanceWarning warning)
    {
        ShowWarningPanel(warning.Message);
        Debug.LogWarning($"[Performance] {warning.Type}: {warning.Message}");
    }

    // 导出报告供分析
    [ContextMenu("Export Performance Report")]
    private void ExportReport()
    {
        var report = _perfMonitor.ExportReport();
        var path = $"PerformanceReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        System.IO.File.WriteAllText(path, report);
        Debug.Log($"Report exported to: {path}");
    }
}
```

#### 实现要点
- 使用 Unity Profiler API
- 采样率可配置
- 仅开发环境启用
- 最小化监控开销

#### 验证方法
- 验证监控数据准确性
- 测试警告阈值触发
- 确认Release构建不包含

---

### 8.3 编辑器扩展

**状态**: [ ] TODO
**优先级**: P2 (一般)
**复杂度**: 高 (1-2周)
**依赖**: 无

#### 功能描述
提供Unity编辑器扩展工具，方便开发和调试。

#### 接口设计
```csharp
#if UNITY_EDITOR
/// <summary>
/// 编辑器工具窗口
/// </summary>
public class NaninovelIntegrationEditorWindow : EditorWindow
{
    [MenuItem("Tools/Levity/Naninovel Integration")]
    public static void ShowWindow()
    {
        GetWindow<NaninovelIntegrationEditorWindow>("Nani Integration");
    }

    // 功能选项卡
    private enum Tab
    {
        ScriptBrowser,
        VariableViewer,
        SaveDataViewer,
        PerformanceProfiler,
        Settings
    }
}

/// <summary>
/// 脚本浏览器
/// </summary>
public interface IScriptBrowserTool
{
    /// <summary>获取所有脚本</summary>
    List<ScriptInfo> GetAllScripts();

    /// <summary>搜索脚本内容</summary>
    List<SearchResult> SearchContent(string query);

    /// <summary>跳转到指定行</summary>
    void GotoLine(string scriptName, int lineNumber);

    /// <summary>验证脚本语法</summary>
    List<ScriptError> ValidateScript(string scriptName);

    /// <summary>批量验证所有脚本</summary>
    List<ScriptError> ValidateAllScripts();
}

public class ScriptInfo
{
    public string Name { get; set; }
    public string Path { get; set; }
    public int LineCount { get; set; }
    public DateTime LastModified { get; set; }
    public List<string> Labels { get; set; }
    public List<string> ReferencedCharacters { get; set; }
}

public class SearchResult
{
    public string ScriptName { get; set; }
    public int LineNumber { get; set; }
    public string LineContent { get; set; }
    public string MatchContext { get; set; }
}

public class ScriptError
{
    public string ScriptName { get; set; }
    public int LineNumber { get; set; }
    public string Message { get; set; }
    public ErrorSeverity Severity { get; set; }
}

public enum ErrorSeverity
{
    Info,
    Warning,
    Error
}
#endif
```

#### 使用示例
```csharp
#if UNITY_EDITOR
public class NaninovelIntegrationEditorWindow : EditorWindow
{
    private Tab _currentTab = Tab.ScriptBrowser;
    private string _searchQuery = "";
    private Vector2 _scrollPosition;
    private List<ScriptInfo> _scripts;
    private List<ScriptError> _errors;

    private void OnGUI()
    {
        DrawToolbar();

        switch (_currentTab)
        {
            case Tab.ScriptBrowser:
                DrawScriptBrowser();
                break;
            case Tab.VariableViewer:
                DrawVariableViewer();
                break;
            case Tab.SaveDataViewer:
                DrawSaveDataViewer();
                break;
            case Tab.PerformanceProfiler:
                DrawPerformanceProfiler();
                break;
            case Tab.Settings:
                DrawSettings();
                break;
        }
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Toggle(_currentTab == Tab.ScriptBrowser, "脚本", EditorStyles.toolbarButton))
            _currentTab = Tab.ScriptBrowser;
        if (GUILayout.Toggle(_currentTab == Tab.VariableViewer, "变量", EditorStyles.toolbarButton))
            _currentTab = Tab.VariableViewer;
        if (GUILayout.Toggle(_currentTab == Tab.SaveDataViewer, "存档", EditorStyles.toolbarButton))
            _currentTab = Tab.SaveDataViewer;
        if (GUILayout.Toggle(_currentTab == Tab.PerformanceProfiler, "性能", EditorStyles.toolbarButton))
            _currentTab = Tab.PerformanceProfiler;
        if (GUILayout.Toggle(_currentTab == Tab.Settings, "设置", EditorStyles.toolbarButton))
            _currentTab = Tab.Settings;

        EditorGUILayout.EndHorizontal();
    }

    private void DrawScriptBrowser()
    {
        EditorGUILayout.BeginHorizontal();
        _searchQuery = EditorGUILayout.TextField("搜索", _searchQuery);
        if (GUILayout.Button("搜索", GUILayout.Width(60)))
        {
            SearchScripts();
        }
        if (GUILayout.Button("验证全部", GUILayout.Width(80)))
        {
            ValidateAllScripts();
        }
        EditorGUILayout.EndHorizontal();

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        // 显示脚本列表或搜索结果
        if (_scripts != null)
        {
            foreach (var script in _scripts)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(script.Name);
                EditorGUILayout.LabelField($"{script.LineCount} lines", GUILayout.Width(80));
                if (GUILayout.Button("打开", GUILayout.Width(50)))
                {
                    OpenScript(script.Path);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        // 显示错误
        if (_errors != null && _errors.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("验证结果", EditorStyles.boldLabel);

            foreach (var error in _errors)
            {
                var icon = error.Severity == ErrorSeverity.Error ? "console.erroricon" :
                           error.Severity == ErrorSeverity.Warning ? "console.warnicon" : "console.infoicon";

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(EditorGUIUtility.IconContent(icon), GUILayout.Width(20));
                EditorGUILayout.LabelField($"{error.ScriptName}:{error.LineNumber}");
                EditorGUILayout.LabelField(error.Message);
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawVariableViewer()
    {
        // 显示Naninovel自定义变量
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("请在运行时查看变量", MessageType.Info);
            return;
        }

        var variableManager = Engine.GetService<ICustomVariableManager>();
        if (variableManager == null) return;

        foreach (var variable in variableManager.GetAllVariables())
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(variable.Name, GUILayout.Width(200));
            var newValue = EditorGUILayout.TextField(variable.Value);
            if (newValue != variable.Value)
            {
                variableManager.SetVariableValue(variable.Name, newValue);
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    // ... 其他绘制方法
}
#endif
```

#### 实现要点
- 使用 `EditorWindow` 和 `PropertyDrawer`
- 支持Play模式实时调试
- 脚本语法高亮
- 与Naninovel原生编辑器集成

#### 验证方法
- 测试所有编辑器功能
- 验证Play模式调试
- 测试脚本验证准确性

---

### 8.4 运行时调试面板

**状态**: [ ] TODO
**优先级**: P2 (一般)
**复杂度**: 中 (3-5天)
**依赖**: 无

#### 功能描述
提供游戏运行时的调试面板，用于测试和排查问题。

#### 接口设计
```csharp
/// <summary>
/// 运行时调试面板服务
/// </summary>
public interface IDebugPanelService
{
    /// <summary>
    /// 显示调试面板
    /// </summary>
    void Show();

    /// <summary>
    /// 隐藏调试面板
    /// </summary>
    void Hide();

    /// <summary>
    /// 切换显示
    /// </summary>
    void Toggle();

    /// <summary>
    /// 是否可见
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// 执行调试命令
    /// </summary>
    void ExecuteCommand(string command);

    /// <summary>
    /// 注册自定义调试命令
    /// </summary>
    void RegisterCommand(string name, Action<string[]> handler, string description);

    /// <summary>
    /// 添加日志条目
    /// </summary>
    void Log(string message, LogLevel level = LogLevel.Info);
}

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}
```

#### 使用示例
```csharp
public class DebugPanelUI : MonoBehaviour
{
    [Inject] private IDebugPanelService _debugPanel;
    [Inject] private INaninovelService _nani;
    [Inject] private INaninovelParameterService _params;

    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private TMP_InputField _commandInput;
    [SerializeField] private ScrollRect _logScroll;
    [SerializeField] private TMP_Text _logText;

    private void Start()
    {
        #if DEVELOPMENT_BUILD || UNITY_EDITOR
        RegisterBuiltInCommands();
        #else
        gameObject.SetActive(false);
        #endif
    }

    private void RegisterBuiltInCommands()
    {
        _debugPanel.RegisterCommand("play", args =>
        {
            if (args.Length > 0)
                _nani.PlayScript(args[0]).Forget();
        }, "播放脚本: play <scriptName> [label]");

        _debugPanel.RegisterCommand("goto", args =>
        {
            if (args.Length > 0)
                _nani.GotoLabel(args[0]).Forget();
        }, "跳转标签: goto <label>");

        _debugPanel.RegisterCommand("set", args =>
        {
            if (args.Length >= 2)
                _params.SetParameter(args[0], args[1]);
        }, "设置变量: set <key> <value>");

        _debugPanel.RegisterCommand("get", args =>
        {
            if (args.Length > 0)
            {
                var value = _params.GetOutput<string>(args[0], "undefined");
                _debugPanel.Log($"{args[0]} = {value}");
            }
        }, "获取变量: get <key>");

        _debugPanel.RegisterCommand("skip", args =>
        {
            // 跳过当前对话
        }, "跳过当前对话");

        _debugPanel.RegisterCommand("save", args =>
        {
            var slot = args.Length > 0 ? int.Parse(args[0]) : 0;
            // 保存到槽位
        }, "快速保存: save [slot]");

        _debugPanel.RegisterCommand("load", args =>
        {
            var slot = args.Length > 0 ? int.Parse(args[0]) : 0;
            // 从槽位加载
        }, "快速加载: load [slot]");

        _debugPanel.RegisterCommand("help", args =>
        {
            ShowHelp();
        }, "显示帮助");

        _debugPanel.RegisterCommand("clear", args =>
        {
            _logText.text = "";
        }, "清空日志");
    }

    private void Update()
    {
        // F12 切换调试面板
        if (Input.GetKeyDown(KeyCode.F12))
        {
            _debugPanel.Toggle();
        }
    }

    public void OnCommandSubmit()
    {
        var command = _commandInput.text.Trim();
        if (string.IsNullOrEmpty(command)) return;

        _debugPanel.Log($"> {command}", LogLevel.Debug);
        _debugPanel.ExecuteCommand(command);

        _commandInput.text = "";
        _commandInput.ActivateInputField();
    }

    private void ShowHelp()
    {
        _debugPanel.Log("=== 可用命令 ===");
        // 列出所有已注册命令
    }
}
```

#### 实现要点
- 仅在开发版本中可用
- 命令行风格的输入
- 日志级别过滤
- 命令历史记录

#### 验证方法
- 测试所有内置命令
- 验证自定义命令注册
- 测试Release版本不可用

---

## 9. 高级功能模块

### 9.1 条件分支联动

**状态**: [ ] TODO
**优先级**: P2 (一般)
**复杂度**: 高 (1-2周)
**依赖**: 2.3 参数传递机制

#### 功能描述
实现游戏状态与脚本分支的深度联动，支持复杂的条件判断和分支逻辑。

#### 接口设计
```csharp
/// <summary>
/// 条件分支服务
/// </summary>
public interface IConditionBranchService
{
    /// <summary>
    /// 注册条件函数
    /// </summary>
    void RegisterCondition(string name, Func<bool> condition);

    /// <summary>
    /// 注册带参数的条件函数
    /// </summary>
    void RegisterCondition<T>(string name, Func<T, bool> condition);

    /// <summary>
    /// 移除条件函数
    /// </summary>
    void UnregisterCondition(string name);

    /// <summary>
    /// 评估条件
    /// </summary>
    bool EvaluateCondition(string expression);

    /// <summary>
    /// 注册分支处理器
    /// </summary>
    void RegisterBranchHandler(string branchId, Action<BranchContext> handler);

    /// <summary>
    /// 获取分支统计
    /// </summary>
    BranchStatistics GetStatistics();
}

public class BranchContext
{
    public string BranchId { get; set; }
    public string ScriptName { get; set; }
    public int LineIndex { get; set; }
    public Dictionary<string, object> Variables { get; set; }
}

public class BranchStatistics
{
    public Dictionary<string, int> BranchHitCounts { get; set; }
    public Dictionary<string, List<string>> BranchPaths { get; set; }
    public int TotalBranches { get; set; }
    public int VisitedBranches { get; set; }
}
```

#### 使用示例
```csharp
public class GameConditionSetup : MonoBehaviour
{
    [Inject] private IConditionBranchService _conditions;
    [Inject] private IPlayerData _playerData;
    [Inject] private IAffectionSystem _affection;

    private void Start()
    {
        // 注册条件函数
        _conditions.RegisterCondition("hasItem", (string itemId) =>
            _playerData.Inventory.Contains(itemId));

        _conditions.RegisterCondition("affectionAbove", (string param) =>
        {
            var parts = param.Split(',');
            var characterId = parts[0];
            var threshold = int.Parse(parts[1]);
            return _affection.GetAffection(characterId) >= threshold;
        });

        _conditions.RegisterCondition("questCompleted", (string questId) =>
            _playerData.CompletedQuests.Contains(questId));

        _conditions.RegisterCondition("dayOfWeek", (string day) =>
            GameTime.CurrentDayOfWeek.ToString() == day);

        _conditions.RegisterCondition("isFirstPlay", () =>
            !PlayerPrefs.HasKey("has_played"));

        // 组合条件示例
        _conditions.RegisterCondition("canDateAlice", () =>
            _affection.GetAffection("Alice") >= 50 &&
            _playerData.CompletedQuests.Contains("alice_intro") &&
            !_playerData.Flags.Contains("alice_rejected"));
    }
}
```

#### Naninovel脚本中使用
```nani
; 使用自定义条件
@if hasItem("key_card")
    ; 有钥匙卡的分支
    @goto .has_key
@else
    ; 没有钥匙卡的分支
    守卫: 你没有通行证，不能进入。
    @stop
@endif

# has_key
守卫: 请进。

; 好感度分支
@if affectionAbove("Alice,80")
    爱丽丝: [开心] 太好了，你来了！
@elseif affectionAbove("Alice,50")
    爱丽丝: 哦，是你啊。
@else
    爱丽丝: ...你是谁？
@endif

; 复杂条件
@if canDateAlice
    ; 解锁约会事件
    @goto Events/AliceDate
@endif
```

#### 实现要点
- 扩展 Naninovel 的表达式系统
- 缓存条件评估结果
- 分支路径追踪
- 支持复杂表达式解析

#### 验证方法
- 测试各种条件组合
- 验证分支路径记录
- 测试条件评估性能

---

### 9.2 成就系统集成

**状态**: [ ] TODO
**优先级**: P3 (低)
**复杂度**: 中 (3-5天)
**依赖**: 2.1 选择系统事件集成

#### 功能描述
将对话事件与成就系统对接，支持对话相关成就的触发和追踪。

#### 接口设计
```csharp
/// <summary>
/// 成就集成服务
/// </summary>
public interface INaninovelAchievementIntegration
{
    /// <summary>
    /// 注册对话成就
    /// </summary>
    void RegisterDialogueAchievement(DialogueAchievementConfig config);

    /// <summary>
    /// 解锁成就
    /// </summary>
    void UnlockAchievement(string achievementId);

    /// <summary>
    /// 更新进度成就
    /// </summary>
    void UpdateProgress(string achievementId, int current, int total);

    /// <summary>
    /// 获取对话相关成就列表
    /// </summary>
    List<DialogueAchievementInfo> GetDialogueAchievements();

    /// <summary>
    /// 成就解锁事件
    /// </summary>
    event Action<string> OnAchievementUnlocked;
}

public class DialogueAchievementConfig
{
    public string AchievementId { get; set; }
    public DialogueAchievementType Type { get; set; }
    public string TargetId { get; set; }  // 脚本名、角色ID等
    public int RequiredCount { get; set; }
    public Func<bool> CustomCondition { get; set; }
}

public enum DialogueAchievementType
{
    /// <summary>完成指定脚本</summary>
    CompleteScript,

    /// <summary>完成所有角色对话</summary>
    AllCharacterDialogues,

    /// <summary>做出特定选择</summary>
    SpecificChoice,

    /// <summary>解锁特定结局</summary>
    UnlockEnding,

    /// <summary>收集所有CG</summary>
    CollectAllCGs,

    /// <summary>完成指定数量对话</summary>
    DialogueCount,

    /// <summary>自定义条件</summary>
    Custom
}

public class DialogueAchievementInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsUnlocked { get; set; }
    public int CurrentProgress { get; set; }
    public int RequiredProgress { get; set; }
}
```

#### 使用示例
```csharp
public class AchievementSetup : MonoBehaviour
{
    [Inject] private INaninovelAchievementIntegration _achievement;

    private void Start()
    {
        // 注册对话成就
        _achievement.RegisterDialogueAchievement(new DialogueAchievementConfig
        {
            AchievementId = "first_dialogue",
            Type = DialogueAchievementType.CompleteScript,
            TargetId = "Chapter1/Opening"
        });

        _achievement.RegisterDialogueAchievement(new DialogueAchievementConfig
        {
            AchievementId = "true_ending",
            Type = DialogueAchievementType.UnlockEnding,
            TargetId = "true_end"
        });

        _achievement.RegisterDialogueAchievement(new DialogueAchievementConfig
        {
            AchievementId = "all_cg",
            Type = DialogueAchievementType.CollectAllCGs,
            RequiredCount = 50
        });

        _achievement.RegisterDialogueAchievement(new DialogueAchievementConfig
        {
            AchievementId = "max_affection",
            Type = DialogueAchievementType.Custom,
            CustomCondition = () => IsAnyCharacterMaxAffection()
        });

        _achievement.OnAchievementUnlocked += OnUnlocked;
    }

    private void OnUnlocked(string achievementId)
    {
        ShowAchievementPopup(achievementId);
    }

    private bool IsAnyCharacterMaxAffection()
    {
        // 检查是否有角色好感度达到最大
        return false;
    }
}
```

#### 实现要点
- 监听脚本完成事件
- 与平台成就系统对接（Steam、PSN等）
- 成就状态持久化
- 支持隐藏成就

#### 验证方法
- 测试成就触发条件
- 验证进度追踪
- 测试平台同步

---

### 9.3 动态脚本生成

**状态**: [ ] TODO
**优先级**: P3 (低)
**复杂度**: 高 (1-2周)
**依赖**: 2.3 参数传递机制

#### 功能描述
支持在运行时动态生成和播放Naninovel脚本，用于程序化对话内容。

#### 接口设计
```csharp
/// <summary>
/// 动态脚本服务
/// </summary>
public interface IDynamicScriptService
{
    /// <summary>
    /// 创建脚本构建器
    /// </summary>
    IScriptBuilder CreateBuilder();

    /// <summary>
    /// 从构建器生成脚本
    /// </summary>
    Script BuildScript(IScriptBuilder builder, string scriptName);

    /// <summary>
    /// 播放动态生成的脚本
    /// </summary>
    UniTask PlayDynamicScript(Script script);

    /// <summary>
    /// 从模板生成脚本
    /// </summary>
    Script GenerateFromTemplate(string templateName, Dictionary<string, string> variables);

    /// <summary>
    /// 验证生成的脚本
    /// </summary>
    List<ScriptError> ValidateScript(Script script);
}

/// <summary>
/// 脚本构建器
/// </summary>
public interface IScriptBuilder
{
    /// <summary>添加标签</summary>
    IScriptBuilder AddLabel(string label);

    /// <summary>添加对话</summary>
    IScriptBuilder AddDialogue(string character, string text);

    /// <summary>添加旁白</summary>
    IScriptBuilder AddNarration(string text);

    /// <summary>添加选择</summary>
    IScriptBuilder AddChoice(params (string text, string gotoLabel)[] choices);

    /// <summary>添加命令</summary>
    IScriptBuilder AddCommand(string command);

    /// <summary>添加条件块</summary>
    IScriptBuilder AddIf(string condition, Action<IScriptBuilder> thenBranch, Action<IScriptBuilder> elseBranch = null);

    /// <summary>添加跳转</summary>
    IScriptBuilder AddGoto(string label);

    /// <summary>添加停止</summary>
    IScriptBuilder AddStop();

    /// <summary>构建为脚本文本</summary>
    string Build();
}
```

#### 使用示例
```csharp
public class ProceduralDialogueGenerator : MonoBehaviour
{
    [Inject] private IDynamicScriptService _scriptService;
    [Inject] private INPCDatabase _npcDatabase;
    [Inject] private IQuestSystem _quests;

    // 生成NPC的程序化对话
    public async UniTask GenerateNPCDialogue(string npcId)
    {
        var npc = _npcDatabase.GetNPC(npcId);
        var activeQuests = _quests.GetActiveQuestsForNPC(npcId);

        var builder = _scriptService.CreateBuilder();

        // 根据好感度选择问候语
        var greeting = GetGreetingByAffection(npc);
        builder.AddDialogue(npc.DisplayName, greeting);

        // 如果有任务
        if (activeQuests.Any())
        {
            builder.AddDialogue(npc.DisplayName, "对了，关于那件事...");

            var choices = new List<(string, string)>();
            foreach (var quest in activeQuests)
            {
                choices.Add(($"关于{quest.Title}", $"quest_{quest.Id}"));
            }
            choices.Add(("没什么", "goodbye"));

            builder.AddChoice(choices.ToArray());

            // 为每个任务生成对话
            foreach (var quest in activeQuests)
            {
                builder.AddLabel($"quest_{quest.Id}");
                GenerateQuestDialogue(builder, npc, quest);
                builder.AddGoto("after_quest");
            }

            builder.AddLabel("after_quest");
        }

        builder.AddLabel("goodbye");
        builder.AddDialogue(npc.DisplayName, GetFarewell(npc));
        builder.AddStop();

        var script = _scriptService.BuildScript(builder, $"Dynamic/NPC_{npcId}");

        // 验证生成的脚本
        var errors = _scriptService.ValidateScript(script);
        if (errors.Any(e => e.Severity == ErrorSeverity.Error))
        {
            Debug.LogError($"Generated script has errors: {errors[0].Message}");
            return;
        }

        await _scriptService.PlayDynamicScript(script);
    }

    private void GenerateQuestDialogue(IScriptBuilder builder, NPCData npc, Quest quest)
    {
        if (quest.IsComplete)
        {
            builder.AddDialogue(npc.DisplayName, quest.CompleteDialogue);
            builder.AddCommand($"@completeQuest {quest.Id}");
        }
        else
        {
            builder.AddDialogue(npc.DisplayName, quest.ProgressDialogue);
            builder.AddDialogue(npc.DisplayName, $"你还需要: {quest.GetRemainingObjectives()}");
        }
    }
}
```

#### 实现要点
- 使用 `ScriptAsset.FromText` 创建脚本
- 正确转义特殊字符
- 模板变量替换
- 生成脚本的缓存

#### 验证方法
- 测试各种构建器操作
- 验证生成脚本语法正确
- 测试复杂分支结构

---

### 9.4 AI对话预留

**状态**: [ ] TODO
**优先级**: P3 (低)
**复杂度**: 高 (1-2周)
**依赖**: 9.3 动态脚本生成

#### 功能描述
预留AI生成对话的接口，支持未来与LLM集成实现动态对话。

#### 接口设计
```csharp
/// <summary>
/// AI对话服务接口（预留）
/// </summary>
public interface IAIDialogueService
{
    /// <summary>
    /// 是否可用
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// 生成对话回复
    /// </summary>
    UniTask<AIDialogueResponse> GenerateResponse(AIDialogueRequest request);

    /// <summary>
    /// 生成对话选项
    /// </summary>
    UniTask<List<string>> GenerateChoices(AIDialogueContext context, int count = 3);

    /// <summary>
    /// 将AI回复转换为Naninovel脚本
    /// </summary>
    string ConvertToNaniScript(AIDialogueResponse response);

    /// <summary>
    /// 设置角色性格配置
    /// </summary>
    void SetCharacterPersona(string characterId, CharacterPersona persona);

    /// <summary>
    /// 获取对话上下文
    /// </summary>
    AIDialogueContext GetCurrentContext();
}

public class AIDialogueRequest
{
    /// <summary>角色ID</summary>
    public string CharacterId { get; set; }

    /// <summary>玩家输入</summary>
    public string PlayerInput { get; set; }

    /// <summary>对话上下文</summary>
    public AIDialogueContext Context { get; set; }

    /// <summary>生成参数</summary>
    public AIGenerationParams Params { get; set; }
}

public class AIDialogueResponse
{
    /// <summary>生成的对话文本</summary>
    public string Text { get; set; }

    /// <summary>情绪标签</summary>
    public string Emotion { get; set; }

    /// <summary>动作建议</summary>
    public List<string> SuggestedActions { get; set; }

    /// <summary>置信度</summary>
    public float Confidence { get; set; }
}

public class AIDialogueContext
{
    /// <summary>对话历史</summary>
    public List<DialogueHistoryEntry> History { get; set; }

    /// <summary>角色关系状态</summary>
    public Dictionary<string, int> Relationships { get; set; }

    /// <summary>当前场景</summary>
    public string CurrentScene { get; set; }

    /// <summary>游戏状态摘要</summary>
    public string GameStateSummary { get; set; }
}

public class CharacterPersona
{
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> PersonalityTraits { get; set; }
    public string SpeakingStyle { get; set; }
    public List<string> KnowledgeBase { get; set; }
}

public class AIGenerationParams
{
    public float Temperature { get; set; } = 0.7f;
    public int MaxTokens { get; set; } = 150;
    public List<string> StopSequences { get; set; }
}
```

#### 使用示例
```csharp
// 存根实现（当前不可用）
public class AIDialogueServiceStub : IAIDialogueService
{
    public bool IsAvailable => false;

    public UniTask<AIDialogueResponse> GenerateResponse(AIDialogueRequest request)
    {
        return UniTask.FromResult(new AIDialogueResponse
        {
            Text = "[AI对话功能未启用]",
            Confidence = 0f
        });
    }

    // ... 其他方法返回默认值
}

// 未来实现示例
public class OpenAIDialogueService : IAIDialogueService
{
    private readonly string _apiKey;
    private readonly HttpClient _client;

    public bool IsAvailable => !string.IsNullOrEmpty(_apiKey);

    public async UniTask<AIDialogueResponse> GenerateResponse(AIDialogueRequest request)
    {
        var persona = GetCharacterPersona(request.CharacterId);

        var systemPrompt = BuildSystemPrompt(persona, request.Context);
        var userMessage = request.PlayerInput;

        var response = await CallOpenAIAPI(systemPrompt, userMessage, request.Params);

        return ParseResponse(response);
    }

    private string BuildSystemPrompt(CharacterPersona persona, AIDialogueContext context)
    {
        return $@"
你是{persona.Name}，{persona.Description}

性格特点：{string.Join("、", persona.PersonalityTraits)}
说话风格：{persona.SpeakingStyle}

当前场景：{context.CurrentScene}
游戏状态：{context.GameStateSummary}

请根据角色设定，用角色的口吻回复玩家。回复应该简短（1-3句话），符合视觉小说的对话风格。
";
    }
}

// 使用AI对话的控制器
public class AIDialogueController : MonoBehaviour
{
    [Inject] private IAIDialogueService _ai;
    [Inject] private IDynamicScriptService _scriptService;
    [Inject] private INaninovelService _nani;

    [SerializeField] private TMP_InputField _playerInput;

    public async void OnPlayerSubmitInput()
    {
        if (!_ai.IsAvailable)
        {
            ShowMessage("AI对话功能未启用");
            return;
        }

        var input = _playerInput.text;
        _playerInput.text = "";

        var context = _ai.GetCurrentContext();

        var response = await _ai.GenerateResponse(new AIDialogueRequest
        {
            CharacterId = "CurrentNPC",
            PlayerInput = input,
            Context = context
        });

        if (response.Confidence < 0.5f)
        {
            // 置信度太低，使用后备对话
            await PlayFallbackDialogue();
            return;
        }

        // 转换为脚本并播放
        var scriptText = _ai.ConvertToNaniScript(response);
        var builder = _scriptService.CreateBuilder();
        builder.AddDialogue(response.CharacterId, response.Text);

        var script = _scriptService.BuildScript(builder, "Dynamic/AI_Response");
        await _scriptService.PlayDynamicScript(script);
    }
}
```

#### 实现要点
- 当前实现存根版本
- 定义清晰的接口以便未来集成
- 考虑API调用的异步和错误处理
- 设计内容安全过滤机制

#### 验证方法
- 验证接口定义完整性
- 存根实现正确返回
- 测试未来集成的可行性

---

## 10. 附录

### 10.1 依赖关系图

```
┌─────────────────────────────────────────────────────────────────┐
│                         核心增强模块                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │ 2.1 选择事件 │  │ 2.2 对话队列 │  │ 2.3 参数传递            │  │
│  └─────────────┘  └─────────────┘  └───────────┬─────────────┘  │
│         │                                       │                 │
│         └───────────────┬───────────────────────┘                 │
│                         │                                         │
│  ┌─────────────────────┴───────────────────────┐                 │
│  │ 2.4 初始化重试 ──────► 2.5 等待优化           │                 │
│  └─────────────────────────────────────────────┘                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                         存档系统模块                              │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │ 3.1 存档同步机制 ◄────────────────────────────────────────┤  │
│  └───────────┬───────────────────────────────────────────────┘  │
│              │                                                   │
│  ┌───────────▼───────────┐  ┌─────────────┐  ┌─────────────┐   │
│  │ 3.2 对话进度保存/恢复  │  │ 3.3 自动存档点│  │ 3.4 槽位管理 │   │
│  └───────────────────────┘  └─────────────┘  └──────┬──────┘   │
│                                                      │          │
│                              ┌───────────────────────▼──────┐   │
│                              │ 3.5 云存档预留                │   │
│                              └──────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                          UI集成模块                              │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ 4.1 对话框样式系统 ◄────────────────────────────────────┤    │
│  └───────────┬────────────────────────┬────────────────────┘    │
│              │                        │                          │
│  ┌───────────▼───────────┐  ┌─────────▼─────────┐               │
│  │ 4.2 选项按钮定制       │  │ 4.5 角色名牌系统   │               │
│  └───────────────────────┘  └───────────────────┘               │
│                                                                  │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │ 4.3 历史面板 │  │ 4.4 快捷菜单 │  │ 4.6 文字效果扩展        │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                         设置系统模块                              │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ 5.1 文字速度控制                                         │    │
│  └───────────┬────────────────────────┬────────────────────┘    │
│              │                        │                          │
│  ┌───────────▼───────────┐  ┌─────────▼─────────┐               │
│  │ 5.2 自动播放设置       │  │ 5.3 跳过模式管理   │               │
│  └───────────────────────┘  └───────────────────┘               │
│                                                                  │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │ 5.4 音量控制 │  │ 5.5 字体大小 │  │ 5.6 对话框透明度        │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### 10.2 迁移指南

#### 从基础集成升级

1. **更新依赖**
   ```
   - 确保 Naninovel 版本 >= 1.20
   - 更新 UniTask 到最新版本
   - 安装 TextMeshPro（如尚未安装）
   ```

2. **配置服务注册**
   ```csharp
   // 在游戏初始化时注册新服务
   public class NaninovelServicesInstaller : MonoInstaller
   {
       public override void InstallBindings()
       {
           // 核心服务
           Container.Bind<INaninovelService>().To<NaninovelService>().AsSingle();
           Container.Bind<IChoiceEventService>().To<ChoiceEventService>().AsSingle();
           Container.Bind<IDialogueQueueService>().To<DialogueQueueService>().AsSingle();

           // 存档服务
           Container.Bind<INaninovelSaveIntegration>().To<NaninovelSaveIntegration>().AsSingle();

           // UI服务
           Container.Bind<IDialogueStyleService>().To<DialogueStyleService>().AsSingle();

           // 设置服务
           Container.Bind<INaninovelSettings>().To<NaninovelSettingsService>().AsSingle();
       }
   }
   ```

3. **迁移存档数据**
   ```csharp
   // 旧存档格式迁移示例
   public NaninovelSaveData MigrateFromV1(OldSaveData oldData)
   {
       return new NaninovelSaveData
       {
           Version = 2,
           CurrentScriptName = oldData.ScriptName,
           PlaybackSpot = oldData.Line,
           CustomVariables = ConvertVariables(oldData.Variables)
       };
   }
   ```

4. **更新脚本引用**
   - 检查所有 `@goto` 和 `@gosub` 命令
   - 更新任何自定义命令

### 10.3 版本规划

#### v1.0 - 核心功能 (P0)
- [ ] 2.1 选择系统事件集成
- [ ] 2.3 参数传递机制
- [ ] 2.4 初始化重试机制
- [ ] 3.1 存档同步机制
- [ ] 3.2 对话进度保存/恢复
- [ ] 5.1 文字速度控制

#### v1.1 - 增强功能 (P1)
- [ ] 2.2 对话队列系统
- [ ] 2.5 等待优化
- [ ] 3.3 自动存档点
- [ ] 3.4 存档槽位管理
- [ ] 4.1 对话框样式系统
- [ ] 4.3 对话历史面板
- [ ] 5.2 自动播放设置
- [ ] 5.3 跳过模式管理
- [ ] 5.4 音量独立控制
- [ ] 6.1 角色立绘管理
- [ ] 6.2 背景与场景集成
- [ ] 8.1 脚本预加载策略

#### v1.2 - 完善功能 (P2)
- [ ] 4.2 选项按钮定制
- [ ] 4.4 快捷菜单系统
- [ ] 4.5 角色名牌系统
- [ ] 4.6 文字效果扩展
- [ ] 5.5 字体大小调整
- [ ] 5.6 对话框透明度
- [ ] 6.3 特效/转场封装
- [ ] 6.4 CG回廊系统
- [ ] 6.5 音乐管理集成
- [ ] 7.1 多语言脚本切换
- [ ] 7.2 字体回退机制
- [ ] 8.2 性能监控
- [ ] 8.3 编辑器扩展
- [ ] 8.4 运行时调试面板
- [ ] 9.1 条件分支联动

#### v1.3 - 高级功能 (P3)
- [ ] 3.5 云存档预留接口
- [ ] 7.3 语音包热切换
- [ ] 9.2 成就系统集成
- [ ] 9.3 动态脚本生成
- [ ] 9.4 AI对话预留

---

## 文档版本

- **创建日期**: 2026-02-22
- **最后更新**: 2026-02-22
- **文档版本**: 1.0
- **适用项目**: LevityFramework
- **Naninovel版本**: >= 1.20

---

*此文档由开发团队维护，如有问题或建议请提交Issue。*
