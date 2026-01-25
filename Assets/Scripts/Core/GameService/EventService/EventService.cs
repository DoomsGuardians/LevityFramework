// LevityFramework - 通用 Unity 游戏框架
// 核心服务模块 - EventService 事件服务

using System;

/// <summary>
/// 事件 ID 枚举（可根据项目扩展）
/// </summary>
public enum EventID
{
    // 核心事件
    OnInitDone,         // UI 初始化需要调用其他下层的逻辑时
    OnHitTarget,        // 命中目标
    OnGamePlayOver,     // 游戏结束

    // 关卡系统事件
    OnStageLoadStart,   // 关卡开始加载
    OnStageLoadComplete,// 关卡加载完成
    OnStageUnload,      // 关卡卸载

    // Naninovel 对话系统事件（需要 NANINOVEL 宏）
    OnDialogueServiceReady,  // 对话服务初始化完成
    OnDialogueStart,         // 对话开始
    OnDialogueEnd,           // 对话结束
    OnDialogueChoice,        // 玩家做出选择

    // 可根据项目需要添加更多事件
}

/// <summary>
/// 事件服务：基于枚举的事件系统，支持即时事件和分帧队列事件
/// </summary>
public class EventService : ILogic
{
    private EventHandler<EventID> EventHandler = new EventHandler<EventID>();

    public void OnInit()
    {
        EventHandler.OnEventInit();
    }

    public void OnUpdate()
    {
        EventHandler.OnEventUpdate();
    }

    public void OnEnterState() { }

    public void UnInit() { }

    /// <summary>
    /// 添加事件监听（参数超过2个的，封装个类传递参数）
    /// </summary>
    public void AddEventListening(EventID eventID, Action<object, object> action)
    {
        EventHandler.AddEventListening(eventID, action);
    }

    /// <summary>
    /// 通过 ID 注销某一类事件（会将所有地方注册的该类事件都注销）
    /// </summary>
    public void RemoveEventListeningByID(EventID eventID)
    {
        EventHandler.RemoveEventListeningByID(eventID);
    }

    /// <summary>
    /// 清除该对象所有注册的事件
    /// </summary>
    public void RemoveEventListeningByTarget(object target)
    {
        EventHandler.RemoveEventListeningByTarget(target);
    }

    /// <summary>
    /// 立即处理事件
    /// </summary>
    public void SendMessage(EventID eventID, object param1, object param2)
    {
        EventHandler.SentMessage(eventID, param1, param2);
    }

    /// <summary>
    /// 分帧处理事件
    /// </summary>
    public void SendMessageByQue(EventID eventID, object param1, object param2)
    {
        EventHandler.SentMessageByQue(eventID, param1, param2);
    }
}
