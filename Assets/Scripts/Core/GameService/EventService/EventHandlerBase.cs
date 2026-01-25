// LevityFramework - 通用 Unity 游戏框架
// 核心服务模块 - EventHandlerBase 事件处理基础实现

using System;
using System.Collections.Generic;

/// <summary>
/// 事件处理的基础实现
/// 支持一个事件名注册多个事件，且事件的传参依据各类事件函数来选择
/// </summary>
public class EventHandlerBase<T>
{
    private readonly Dictionary<T, List<Action<object, object>>> eventDic = new Dictionary<T, List<Action<object, object>>>();
    private readonly Dictionary<object, List<T>> targetEventDic = new Dictionary<object, List<T>>();

    /// <summary>
    /// 添加事件监听
    /// </summary>
    public void AddEventHandler(T id, Action<object, object> action)
    {
        if (!eventDic.ContainsKey(id))
        {
            eventDic[id] = new List<Action<object, object>>();
        }

        // 避免重复注册相同的事件
        List<Action<object, object>> eventList = eventDic[id];
        Action<object, object> checkAction = eventList.Find(i => i == action);
        if (checkAction != null)
        {
            return;
        }

        eventList.Add(action);

        // 更新从属者信息
        object target = action.Target;
        if (!targetEventDic.ContainsKey(target))
        {
            targetEventDic[target] = new List<T>();
        }
        targetEventDic[target].Add(id);
    }

    /// <summary>
    /// 通过事件 ID 移除事件
    /// </summary>
    public void RemoveEventByID(T id)
    {
        if (eventDic.ContainsKey(id))
        {
            List<Action<object, object>> actions = eventDic[id];
            foreach (var action in actions)
            {
                object target = action.Target;
                if (target != null && targetEventDic.ContainsKey(target))
                {
                    List<T> idList = targetEventDic[target];
                    idList.RemoveAll(eventId => eventId.Equals(id));
                    if (idList.Count == 0)
                    {
                        targetEventDic.Remove(target);
                    }
                }
            }
            eventDic.Remove(id);
        }
    }

    /// <summary>
    /// 通过目标对象移除其所有注册的事件
    /// </summary>
    public void RemoveEventByTarget(object target)
    {
        if (targetEventDic.ContainsKey(target))
        {
            List<T> idList = targetEventDic[target];
            foreach (var id in idList)
            {
                if (eventDic.ContainsKey(id))
                {
                    List<Action<object, object>> actions = eventDic[id];
                    actions.RemoveAll(action => action.Target == target);
                    if (actions.Count == 0)
                    {
                        eventDic.Remove(id);
                    }
                }
            }
            targetEventDic.Remove(target);
        }
    }

    /// <summary>
    /// 获取指定事件 ID 的所有回调
    /// </summary>
    public List<Action<object, object>> GetEvent(T id)
    {
        if (eventDic.ContainsKey(id))
        {
            return eventDic[id];
        }
        return null;
    }
}
