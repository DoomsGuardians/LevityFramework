// LevityFramework - 通用 Unity 游戏框架
// 核心指令模块 - UIListener UI 事件监听器

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

/// <summary>
/// UI 事件监听器，处理点击、拖拽、进入/离开等事件
/// 可与按键绑定，通用 UI 交互（移动端、PC 端）
/// </summary>
[AddComponentMenu("Input/On-Screen UI Listener")]
public class UIListener : OnScreenControl,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerClickHandler,
    IDragHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [InputControl]
    [SerializeField, Header("关联按键（可选）")]
    private string m_ControlPath;

    [SerializeField]
    private bool isButton;

    protected override string controlPathInternal
    {
        get => m_ControlPath;
        set => m_ControlPath = value;
    }

    public Action<PointerEventData, UIListener, object[]> onClick;
    public Action<PointerEventData, UIListener, object[]> onClickDown;
    public Action<PointerEventData, UIListener, object[]> onClickUp;
    public Action<PointerEventData, UIListener, object[]> onDrag;
    public Action<PointerEventData, UIListener, object[]> onEnter;
    public Action<PointerEventData, UIListener, object[]> onExit;
    public object[] args;

    public virtual void OnDrag(PointerEventData eventData)
    {
        onDrag?.Invoke(eventData, this, args);
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        onClick?.Invoke(eventData, this, args);
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        onClickDown?.Invoke(eventData, this, args);
        if (isButton)
        {
            SendValueToControl(1.0f);
        }
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        onClickUp?.Invoke(eventData, this, args);
        if (isButton)
        {
            SendValueToControl(0.0f);
        }
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        onEnter?.Invoke(eventData, this, args);
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        onExit?.Invoke(eventData, this, args);
    }

    /// <summary>传递按键值</summary>
    public new void SendValueToControl<TValue>(TValue value) where TValue : struct
    {
        base.SendValueToControl(value);
    }
}
