// LevityFramework - 通用 Unity 游戏框架
// 泛型事件系统 - EventBinding 自动注销扩展

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 将 EventBinding 的注销时机绑定到 GameObject 的生命周期。
/// 避免在每个订阅方的 OnDisable / OnDestroy 里手写 binding.Dispose() 样板。
/// </summary>
/// <example>
/// EventBus&lt;DialogueStartEvent&gt;.Register(OnDialogueStart)
///     .UnregisterWhenDestroyed(this);
/// </example>
public static class EventBindingExtensions
{
    /// <summary>
    /// 当 owner 所在的 GameObject 被销毁时，自动 Dispose 此绑定。
    /// </summary>
    public static EventBinding<T> UnregisterWhenDestroyed<T>(
        this EventBinding<T> self, Component owner) where T : struct, IEvent
    {
        if (self == null || owner == null) return self;
        var trigger = GetOrAddHidden<EventBindingDestroyTrigger>(owner.gameObject);
        trigger.Add(self);
        return self;
    }

    /// <summary>
    /// 当 owner 所在的 GameObject 被销毁时，自动 Dispose 此绑定。
    /// </summary>
    public static EventBinding<T> UnregisterWhenDestroyed<T>(
        this EventBinding<T> self, GameObject owner) where T : struct, IEvent
    {
        if (self == null || owner == null) return self;
        var trigger = GetOrAddHidden<EventBindingDestroyTrigger>(owner);
        trigger.Add(self);
        return self;
    }

    /// <summary>
    /// 当 owner 所在的 GameObject 被禁用时，自动 Dispose 此绑定。
    /// 注意：禁用后再启用不会自动重新订阅，需要重新 Register。
    /// </summary>
    public static EventBinding<T> UnregisterWhenDisabled<T>(
        this EventBinding<T> self, Component owner) where T : struct, IEvent
    {
        if (self == null || owner == null) return self;
        var trigger = GetOrAddHidden<EventBindingDisableTrigger>(owner.gameObject);
        trigger.Add(self);
        return self;
    }

    /// <summary>
    /// 当 owner 所在的 GameObject 被禁用时，自动 Dispose 此绑定。
    /// 注意：禁用后再启用不会自动重新订阅，需要重新 Register。
    /// </summary>
    public static EventBinding<T> UnregisterWhenDisabled<T>(
        this EventBinding<T> self, GameObject owner) where T : struct, IEvent
    {
        if (self == null || owner == null) return self;
        var trigger = GetOrAddHidden<EventBindingDisableTrigger>(owner);
        trigger.Add(self);
        return self;
    }

    private static T GetOrAddHidden<T>(GameObject go) where T : Component
    {
        if (!go.TryGetComponent<T>(out var c))
        {
            c = go.AddComponent<T>();
            c.hideFlags = HideFlags.HideInInspector;
        }
        return c;
    }
}

/// <summary>
/// 收集挂在同一 GameObject 上的所有 IDisposable，并在指定生命周期事件时统一释放。
/// </summary>
internal abstract class EventBindingTriggerBase : MonoBehaviour
{
    private readonly List<IDisposable> bindings = new List<IDisposable>();

    public void Add(IDisposable binding)
    {
        if (binding != null) bindings.Add(binding);
    }

    protected void DisposeAll()
    {
        for (int i = 0; i < bindings.Count; i++)
        {
            try { bindings[i]?.Dispose(); }
            catch (Exception e) { Debug.LogException(e); }
        }
        bindings.Clear();
    }
}

internal sealed class EventBindingDestroyTrigger : EventBindingTriggerBase
{
    private void OnDestroy() => DisposeAll();
}

internal sealed class EventBindingDisableTrigger : EventBindingTriggerBase
{
    private void OnDisable() => DisposeAll();
}
