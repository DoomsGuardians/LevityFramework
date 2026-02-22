// LevityFramework - 通用 Unity 游戏框架
// 事件系统 - EventBus 泛型事件总线

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 泛型事件总线
/// 类型安全的事件分发系统，推荐作为主要事件系统使用
/// </summary>
/// <typeparam name="T">事件类型，必须实现 IEvent 接口</typeparam>
/// <example>
/// // 1. 定义事件
/// public struct PlayerDamagedEvent : IEvent
/// {
///     public int Damage;
///     public int RemainingHealth;
/// }
///
/// // 2. 注册监听
/// EventBinding&lt;PlayerDamagedEvent&gt; binding;
///
/// void OnEnable()
/// {
///     binding = EventBus&lt;PlayerDamagedEvent&gt;.Register(OnPlayerDamaged);
/// }
///
/// void OnDisable()
/// {
///     binding?.Dispose();
/// }
///
/// // 3. 触发事件
/// EventBus&lt;PlayerDamagedEvent&gt;.Raise(new PlayerDamagedEvent { Damage = 10 });
/// </example>
public static class EventBus<T> where T : struct, IEvent
{
    // 使用 SortedList 支持优先级排序
    private static readonly SortedList<int, HashSet<EventBinding<T>>> priorityBindings
        = new SortedList<int, HashSet<EventBinding<T>>>(new DescendingComparer());

    private static readonly List<(EventBinding<T> binding, int priority)> bindingsToAdd = new List<(EventBinding<T>, int)>();
    private static readonly List<EventBinding<T>> bindingsToRemove = new List<EventBinding<T>>();
    private static readonly List<EventBinding<T>> onceBindings = new List<EventBinding<T>>();
    private static bool isRaising;

    // 优先级降序比较器
    private class DescendingComparer : IComparer<int>
    {
        public int Compare(int x, int y) => y.CompareTo(x);
    }

    /// <summary>
    /// 当前注册的绑定总数
    /// </summary>
    public static int BindingCount
    {
        get
        {
            int count = 0;
            foreach (var set in priorityBindings.Values)
                count += set.Count;
            return count;
        }
    }

    /// <summary>
    /// 是否有任何绑定
    /// </summary>
    public static bool HasBindings => BindingCount > 0;

    #region 注册方法

    /// <summary>
    /// 注册事件绑定
    /// </summary>
    /// <param name="binding">事件绑定</param>
    /// <param name="priority">优先级（越大越先执行，默认 0）</param>
    public static EventBinding<T> Register(EventBinding<T> binding, int priority = 0)
    {
        if (binding == null || binding.IsDisposed) return binding;

        if (isRaising)
        {
            bindingsToAdd.Add((binding, priority));
        }
        else
        {
            AddBinding(binding, priority);
        }
        return binding;
    }

    /// <summary>
    /// 快速注册（带参数回调）
    /// </summary>
    public static EventBinding<T> Register(Action<T> onEvent, int priority = 0)
    {
        return Register(new EventBinding<T>(onEvent), priority);
    }

    /// <summary>
    /// 快速注册（无参数回调）
    /// </summary>
    public static EventBinding<T> Register(Action onEventNoArgs, int priority = 0)
    {
        return Register(new EventBinding<T>(onEventNoArgs), priority);
    }

    /// <summary>
    /// 注册一次性事件（触发一次后自动注销）
    /// </summary>
    public static EventBinding<T> RegisterOnce(Action<T> onEvent, int priority = 0)
    {
        var binding = Register(onEvent, priority);
        onceBindings.Add(binding);
        return binding;
    }

    /// <summary>
    /// 注册一次性事件（无参数）
    /// </summary>
    public static EventBinding<T> RegisterOnce(Action onEventNoArgs, int priority = 0)
    {
        var binding = Register(onEventNoArgs, priority);
        onceBindings.Add(binding);
        return binding;
    }

    private static void AddBinding(EventBinding<T> binding, int priority)
    {
        if (!priorityBindings.TryGetValue(priority, out var set))
        {
            set = new HashSet<EventBinding<T>>();
            priorityBindings[priority] = set;
        }
        set.Add(binding);
    }

    #endregion

    #region 注销方法

    /// <summary>
    /// 注销事件绑定
    /// </summary>
    public static void Unregister(EventBinding<T> binding)
    {
        if (binding == null) return;

        if (isRaising)
        {
            if (!bindingsToRemove.Contains(binding))
                bindingsToRemove.Add(binding);
        }
        else
        {
            RemoveBinding(binding);
        }
    }

    private static void RemoveBinding(EventBinding<T> binding)
    {
        foreach (var set in priorityBindings.Values)
        {
            if (set.Remove(binding))
                break;
        }
        onceBindings.Remove(binding);
    }

    #endregion

    #region 触发方法

    /// <summary>
    /// 触发事件
    /// </summary>
    public static void Raise(T eventData)
    {
        isRaising = true;

        // 按优先级顺序执行（高优先级先执行）
        foreach (var kvp in priorityBindings)
        {
            foreach (var binding in kvp.Value)
            {
                if (binding != null && !binding.IsDisposed)
                {
                    try
                    {
                        binding.Invoke(eventData);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }

        isRaising = false;

        // 处理一次性绑定
        foreach (var binding in onceBindings)
        {
            binding?.Dispose();
        }
        onceBindings.Clear();

        // 处理延迟添加
        foreach (var (binding, priority) in bindingsToAdd)
        {
            AddBinding(binding, priority);
        }
        bindingsToAdd.Clear();

        // 处理延迟移除
        foreach (var binding in bindingsToRemove)
        {
            RemoveBinding(binding);
        }
        bindingsToRemove.Clear();
    }

    /// <summary>
    /// 触发事件（使用默认值）
    /// </summary>
    public static void Raise() => Raise(default);

    #endregion

    #region 清理方法

    /// <summary>
    /// 清除所有绑定
    /// </summary>
    internal static void Clear()
    {
        priorityBindings.Clear();
        bindingsToAdd.Clear();
        bindingsToRemove.Clear();
        onceBindings.Clear();
    }

    #endregion
}
