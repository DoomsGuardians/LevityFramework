// LevityFramework - 通用 Unity 游戏框架
// 核心服务模块 - EventHandler 泛型事件处理器

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 事件消息数据结构
/// </summary>
public class EventMessage
{
    public Action<object, object> action;
    public object param1;
    public object param2;

    public EventMessage(Action<object, object> action, object param1, object param2)
    {
        this.action = action;
        this.param1 = param1;
        this.param2 = param2;
    }
}

/// <summary>
/// 泛型事件处理器，管理事件的注册、触发和分帧处理
/// </summary>
public class EventHandler<T>
{
    private readonly EventHandlerBase<T> eventHandlerBase = new EventHandlerBase<T>();
    private readonly Queue<EventMessage> eventQueue = new Queue<EventMessage>();

    /// <summary>
    /// 初始化事件处理器
    /// </summary>
    public void OnEventInit()
    {
        eventQueue.Clear();
    }

    /// <summary>
    /// 每帧处理队列中的事件（分帧处理）
    /// </summary>
    public void OnEventUpdate()
    {
        if (eventQueue.Count > 0)
        {
            var eventMessage = eventQueue.Dequeue();
            TriggerEvent(eventMessage);
        }
    }

    /// <summary>
    /// 添加事件监听
    /// </summary>
    public void AddEventListening(T id, Action<object, object> action)
    {
        eventHandlerBase.AddEventHandler(id, action);
    }

    /// <summary>
    /// 通过 ID 移除某一类事件（会将所有地方注册的该类事件都注销）
    /// </summary>
    public void RemoveEventListeningByID(T id)
    {
        eventHandlerBase.RemoveEventByID(id);
    }

    /// <summary>
    /// 清除该对象所有注册的事件
    /// </summary>
    public void RemoveEventListeningByTarget(object target)
    {
        eventHandlerBase.RemoveEventByTarget(target);
    }

    /// <summary>
    /// 立即发送消息（同步处理）
    /// </summary>
    public void SentMessage(T id, object param1, object param2)
    {
        List<Action<object, object>> actions = eventHandlerBase.GetEvent(id);
        if (actions == null) return;

        // 创建副本避免迭代时修改
        List<Action<object, object>> actionsCopy = new List<Action<object, object>>(actions);
        foreach (var action in actionsCopy)
        {
            EventMessage eventMessage = new EventMessage(action, param1, param2);
            TriggerEvent(eventMessage);
        }
    }

    /// <summary>
    /// 通过队列发送消息（分帧处理）
    /// </summary>
    public void SentMessageByQue(T id, object param1, object param2)
    {
        List<Action<object, object>> actions = eventHandlerBase.GetEvent(id);
        if (actions == null) return;

        foreach (var action in actions)
        {
            EventMessage eventMessage = new EventMessage(action, param1, param2);
            eventQueue.Enqueue(eventMessage);
        }
    }

    private void TriggerEvent(EventMessage eventMessage)
    {
        eventMessage.action?.Invoke(eventMessage.param1, eventMessage.param2);
    }
}
