// LevityFramework - 通用 Unity 游戏框架
// 泛型事件系统 - EventBus 事件总线

using System;
using System.Collections.Generic;

/// <summary>
/// 泛型事件总线
/// 基于类型的事件分发系统，与现有 EventService 并存
/// 提供类型安全的事件注册、触发和注销
/// </summary>
/// <typeparam name="T">事件类型，必须是实现 IEvent 的 struct</typeparam>
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
/// void OnPlayerDamaged(PlayerDamagedEvent e)
/// {
///     Debug.Log($"受到 {e.Damage} 伤害，剩余 {e.RemainingHealth} 血量");
/// }
///
/// // 3. 触发事件
/// EventBus&lt;PlayerDamagedEvent&gt;.Raise(new PlayerDamagedEvent
/// {
///     Damage = 10,
///     RemainingHealth = 90
/// });
/// </example>
public static class EventBus<T> where T : struct, IEvent
{
    private static readonly HashSet<EventBinding<T>> bindings = new HashSet<EventBinding<T>>();
    private static readonly List<EventBinding<T>> bindingsToAdd = new List<EventBinding<T>>();
    private static readonly List<EventBinding<T>> bindingsToRemove = new List<EventBinding<T>>();
    private static bool isRaising;

    /// <summary>
    /// 当前注册的绑定数量
    /// </summary>
    public static int BindingCount => bindings.Count;

    /// <summary>
    /// 注册事件绑定
    /// </summary>
    /// <param name="binding">事件绑定实例</param>
    /// <returns>传入的绑定实例（方便链式调用）</returns>
    public static EventBinding<T> Register(EventBinding<T> binding)
    {
        if (binding == null || binding.IsDisposed) return binding;

        if (isRaising)
        {
            // 如果正在触发事件，延迟添加
            if (!bindingsToAdd.Contains(binding))
            {
                bindingsToAdd.Add(binding);
            }
        }
        else
        {
            bindings.Add(binding);
        }
        return binding;
    }

    /// <summary>
    /// 快速注册（创建并注册绑定，返回可 Dispose 的绑定）
    /// </summary>
    /// <param name="onEvent">事件回调</param>
    /// <returns>新创建的事件绑定</returns>
    public static EventBinding<T> Register(Action<T> onEvent)
    {
        var binding = new EventBinding<T>(onEvent);
        return Register(binding);
    }

    /// <summary>
    /// 快速注册无参数回调
    /// </summary>
    /// <param name="onEventNoArgs">无参数事件回调</param>
    /// <returns>新创建的事件绑定</returns>
    public static EventBinding<T> Register(Action onEventNoArgs)
    {
        var binding = new EventBinding<T>(onEventNoArgs);
        return Register(binding);
    }

    /// <summary>
    /// 注销事件绑定
    /// </summary>
    /// <param name="binding">要注销的绑定</param>
    public static void Unregister(EventBinding<T> binding)
    {
        if (binding == null) return;

        if (isRaising)
        {
            // 如果正在触发事件，延迟移除
            if (!bindingsToRemove.Contains(binding))
            {
                bindingsToRemove.Add(binding);
            }
        }
        else
        {
            bindings.Remove(binding);
        }
    }

    /// <summary>
    /// 触发事件
    /// </summary>
    /// <param name="eventData">事件数据</param>
    public static void Raise(T eventData)
    {
        isRaising = true;

        foreach (var binding in bindings)
        {
            if (binding != null && !binding.IsDisposed)
            {
                try
                {
                    binding.Invoke(eventData);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }
        }

        isRaising = false;

        // 处理延迟添加的绑定
        if (bindingsToAdd.Count > 0)
        {
            foreach (var binding in bindingsToAdd)
            {
                bindings.Add(binding);
            }
            bindingsToAdd.Clear();
        }

        // 处理延迟移除的绑定
        if (bindingsToRemove.Count > 0)
        {
            foreach (var binding in bindingsToRemove)
            {
                bindings.Remove(binding);
            }
            bindingsToRemove.Clear();
        }
    }

    /// <summary>
    /// 触发事件（使用默认值）
    /// </summary>
    public static void Raise()
    {
        Raise(default);
    }

    /// <summary>
    /// 清除所有绑定
    /// 通常在场景切换或游戏重置时调用
    /// </summary>
    public static void Clear()
    {
        // Dispose 所有绑定
        foreach (var binding in bindings)
        {
            if (binding != null && !binding.IsDisposed)
            {
                // 直接清理，不触发 Unregister（因为我们要清空整个集合）
            }
        }
        bindings.Clear();
        bindingsToAdd.Clear();
        bindingsToRemove.Clear();
    }

    /// <summary>
    /// 检查是否有任何绑定
    /// </summary>
    public static bool HasBindings => bindings.Count > 0;
}

/// <summary>
/// EventBus 工具类
/// 提供全局清理等功能
/// </summary>
public static class EventBusUtil
{
    private static readonly List<Action> clearActions = new List<Action>();

    /// <summary>
    /// 注册清理动作（由 EventBus&lt;T&gt; 内部使用）
    /// </summary>
    internal static void RegisterClearAction(Action action)
    {
        if (!clearActions.Contains(action))
        {
            clearActions.Add(action);
        }
    }

    /// <summary>
    /// 清除所有类型的 EventBus
    /// 通常在场景切换或游戏重置时调用
    /// </summary>
    public static void ClearAllBuses()
    {
        foreach (var action in clearActions)
        {
            action?.Invoke();
        }
    }
}
