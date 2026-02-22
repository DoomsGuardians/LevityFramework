// LevityFramework - 通用 Unity 游戏框架
// 核心指令模块 - WindowBase UI 窗口基类

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI 窗口的抽象基类
/// 包含完整的事件管理、UI Listener 绑定、生命周期管理
/// 所有的 UI 窗口都要继承这个基类
/// </summary>
public class WindowBase : WindowBehaviour
{
    private List<Button> buttonList = new List<Button>();
    private List<InputField> inputFieldList = new List<InputField>();
    private List<Toggle> toggleList = new List<Toggle>();

    protected GameRoot gameRoot;
    protected UIService uIService;
    protected ResService resService;
    protected AudioService audioService;
    protected DataService dataService;
    protected RoleSystem roleSystem;

    #region 层级与显示属性
    /// <summary>
    /// UI 层级（新版）
    /// </summary>
    [Header("UI 层级设置")]
    public UILayer uiLayer = UILayer.Normal;

    /// <summary>
    /// 旧版层级（已弃用，请使用 uiLayer）
    /// </summary>
    [System.Obsolete("请使用 uiLayer 属性")]
    public WindowLayer windowLayer;

    /// <summary>
    /// 是否为全屏窗口
    /// 全屏窗口打开时会自动隐藏下层 UI 以减少 DrawCall
    /// </summary>
    [Tooltip("是否为全屏窗口（会触发覆盖管理）")]
    public bool IsFullScreen { get; set; } = false;

    /// <summary>
    /// 是否始终可见
    /// 标记为始终可见的窗口不会被全屏窗口遮挡
    /// </summary>
    [Tooltip("是否始终可见（不会被全屏窗口遮挡）")]
    public bool IsAlwaysVisible { get; set; } = false;

    /// <summary>
    /// 窗口名称（用于调试和管理，隐藏基类同名属性）
    /// </summary>
    public new string Name { get; set; }

    /// <summary>
    /// 窗口的 Canvas 引用（用于覆盖管理，隐藏基类同名属性）
    /// </summary>
    public new Canvas canvas { get; private set; }

    /// <summary>
    /// 窗口的 CanvasGroup 引用（用于动画）
    /// </summary>
    public CanvasGroup canvasGroup { get; private set; }

    /// <summary>
    /// UI 动画器引用
    /// </summary>
    public UIAnimator uiAnimator { get; private set; }

    /// <summary>
    /// 分配的 Order In Layer
    /// </summary>
    public int AllocatedOrder { get; set; }
    #endregion

    #region 生命周期
    public override void OnAwake()
    {
        gameRoot = GameRoot.Instance;
        uIService = gameRoot.uIService;
        resService = gameRoot.resService;
        audioService = gameRoot.audioService;
        dataService = gameRoot.dataService;
        roleSystem = gameRoot.roleSystem;

        // 初始化组件引用（使用 gameObject.GetComponent）
        canvas = gameObject.GetComponent<Canvas>();
        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        uiAnimator = gameObject.GetComponent<UIAnimator>();

        // 如果没有名称，使用 GameObject 名称
        if (string.IsNullOrEmpty(Name))
        {
            Name = gameObject.name;
        }
    }

    public override void OnHide() { }

    public override void OnShow() { }

    public override void OnUpdate() { }

    public override void OnDestroy()
    {
        RemoveToggleListener();
        RemoveAllButtonListener();
        RemoveInputFieldListener();
        toggleList.Clear();
        buttonList.Clear();
        inputFieldList.Clear();
        GameObject.Destroy(gameObject);
    }
    #endregion

    #region 事件管理（程序自动绑定）
    public void AddButtonListener(Button button, UnityAction unityAction)
    {
        if (button != null)
        {
            if (!buttonList.Contains(button))
            {
                buttonList.Add(button);
            }
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(unityAction);
        }
    }

    public void RemoveAllButtonListener()
    {
        foreach (var btn in buttonList)
        {
            btn.onClick.RemoveAllListeners();
        }
    }

    public void AddInputFieldListener(InputField inputField, UnityAction<string> onChanged, UnityAction<string> onEndChanged)
    {
        if (inputField != null)
        {
            if (!inputFieldList.Contains(inputField))
            {
                inputFieldList.Add(inputField);
            }
            inputField.onEndEdit.RemoveAllListeners();
            inputField.onValueChanged.RemoveAllListeners();
            inputField.onValueChanged.AddListener(onChanged);
            inputField.onEndEdit.AddListener(onEndChanged);
        }
    }

    public void RemoveInputFieldListener()
    {
        foreach (var input in inputFieldList)
        {
            input.onEndEdit.RemoveAllListeners();
            input.onValueChanged.RemoveAllListeners();
        }
    }

    public void AddToggleListener(Toggle toggle, UnityAction<Toggle, bool> action)
    {
        if (toggle != null)
        {
            if (!toggleList.Contains(toggle))
            {
                toggleList.Add(toggle);
            }
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener((isOn) => action.Invoke(toggle, isOn));
        }
    }

    public void RemoveToggleListener()
    {
        foreach (var toggle in toggleList)
        {
            toggle.onValueChanged.RemoveAllListeners();
        }
    }
    #endregion

    #region 自定义事件（子类初始化主动调用）
    public T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T t = target.GetComponent<T>();
        if (t == null)
        {
            t = target.AddComponent<T>();
        }
        return t;
    }

    /// <summary>绑定点击事件</summary>
    public void OnClick(UIListener uIListener, Action<PointerEventData, UIListener, object[]> clickCB, params object[] objects)
    {
        uIListener.onClick = clickCB;
        if (objects != null)
        {
            uIListener.args = objects;
        }
    }

    public void OnClickDown(UIListener uIListener, Action<PointerEventData, UIListener, object[]> clickCB, params object[] objects)
    {
        uIListener.onClickDown = clickCB;
        if (objects != null)
        {
            uIListener.args = objects;
        }
    }

    public void OnClickUp(UIListener uIListener, Action<PointerEventData, UIListener, object[]> clickCB, params object[] objects)
    {
        uIListener.onClickUp = clickCB;
        if (objects != null)
        {
            uIListener.args = objects;
        }
    }

    public void OnDrag(UIListener uIListener, Action<PointerEventData, UIListener, object[]> clickCB, params object[] objects)
    {
        uIListener.onDrag = clickCB;
        if (objects != null)
        {
            uIListener.args = objects;
        }
    }

    public void OnEnter(UIListener uIListener, Action<PointerEventData, UIListener, object[]> clickCB, params object[] objects)
    {
        uIListener.onEnter = clickCB;
        if (objects != null)
        {
            uIListener.args = objects;
        }
    }

    public void OnExit(UIListener uIListener, Action<PointerEventData, UIListener, object[]> clickCB, params object[] objects)
    {
        uIListener.onExit = clickCB;
        if (objects != null)
        {
            uIListener.args = objects;
        }
    }
    #endregion

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        gameObject.SetActive(visible);
    }

    #region 覆盖管理回调
    /// <summary>
    /// 当窗口被全屏窗口覆盖时调用
    /// 可重写此方法以暂停窗口逻辑（如动画、计时器等）
    /// </summary>
    public virtual void OnPause()
    {
        // 子类可重写
    }

    /// <summary>
    /// 当覆盖的全屏窗口关闭后恢复时调用
    /// 可重写此方法以恢复窗口逻辑
    /// </summary>
    public virtual void OnResume()
    {
        // 子类可重写
    }
    #endregion

    #region 动画支持
    /// <summary>
    /// 播放显示动画
    /// </summary>
    /// <param name="onComplete">完成回调</param>
    public void PlayShowAnimation(Action onComplete = null)
    {
        if (uiAnimator != null)
        {
            uiAnimator.PlayShowAnimation(onComplete);
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// 播放隐藏动画
    /// </summary>
    /// <param name="onComplete">完成回调</param>
    public void PlayHideAnimation(Action onComplete = null)
    {
        if (uiAnimator != null)
        {
            uiAnimator.PlayHideAnimation(onComplete);
        }
        else
        {
            onComplete?.Invoke();
        }
    }
    #endregion
}
