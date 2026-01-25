// LevityFramework - 通用 Unity 游戏框架
// 泛型事件系统 - EventBinding 事件绑定

using System;

/// <summary>
/// 事件绑定接口
/// </summary>
public interface IEventBinding<T> where T : struct, IEvent
{
    Action<T> OnEvent { get; set; }
    Action OnEventNoArgs { get; set; }
}

/// <summary>
/// 事件绑定实现
/// 管理单个事件监听的生命周期，支持 IDisposable 自动注销
/// </summary>
/// <typeparam name="T">事件类型，必须是实现 IEvent 的 struct</typeparam>
/// <example>
/// // 创建绑定
/// var binding = new EventBinding&lt;PlayerDamagedEvent&gt;(OnPlayerDamaged);
/// EventBus&lt;PlayerDamagedEvent&gt;.Register(binding);
///
/// // 或者使用 using 语句自动注销
/// using var binding = EventBus&lt;PlayerDamagedEvent&gt;.Register(OnPlayerDamaged);
/// </example>
public class EventBinding<T> : IEventBinding<T>, IDisposable where T : struct, IEvent
{
    private Action<T> onEvent;
    private Action onEventNoArgs;
    private bool isDisposed;

    /// <summary>
    /// 带参数的事件回调
    /// </summary>
    public Action<T> OnEvent
    {
        get => onEvent;
        set => onEvent = value;
    }

    /// <summary>
    /// 无参数的事件回调（仅作为通知使用）
    /// </summary>
    public Action OnEventNoArgs
    {
        get => onEventNoArgs;
        set => onEventNoArgs = value;
    }

    /// <summary>
    /// 创建带参数回调的事件绑定
    /// </summary>
    public EventBinding(Action<T> onEvent)
    {
        this.onEvent = onEvent;
    }

    /// <summary>
    /// 创建无参数回调的事件绑定（仅作为通知）
    /// </summary>
    public EventBinding(Action onEventNoArgs)
    {
        this.onEventNoArgs = onEventNoArgs;
    }

    /// <summary>
    /// 创建同时支持带参和无参回调的事件绑定
    /// </summary>
    public EventBinding(Action<T> onEvent, Action onEventNoArgs)
    {
        this.onEvent = onEvent;
        this.onEventNoArgs = onEventNoArgs;
    }

    /// <summary>
    /// 触发事件
    /// </summary>
    public void Invoke(T eventData)
    {
        if (isDisposed) return;
        onEvent?.Invoke(eventData);
        onEventNoArgs?.Invoke();
    }

    /// <summary>
    /// 添加带参数回调
    /// </summary>
    public void Add(Action<T> action)
    {
        onEvent += action;
    }

    /// <summary>
    /// 移除带参数回调
    /// </summary>
    public void Remove(Action<T> action)
    {
        onEvent -= action;
    }

    /// <summary>
    /// 添加无参数回调
    /// </summary>
    public void Add(Action action)
    {
        onEventNoArgs += action;
    }

    /// <summary>
    /// 移除无参数回调
    /// </summary>
    public void Remove(Action action)
    {
        onEventNoArgs -= action;
    }

    /// <summary>
    /// 释放绑定并从 EventBus 注销
    /// </summary>
    public void Dispose()
    {
        if (isDisposed) return;
        isDisposed = true;
        EventBus<T>.Unregister(this);
        onEvent = null;
        onEventNoArgs = null;
    }

    /// <summary>
    /// 是否已释放
    /// </summary>
    public bool IsDisposed => isDisposed;
}
