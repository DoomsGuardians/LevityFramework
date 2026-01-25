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
    protected EventService eventService;
    protected AudioService audioService;
    protected DataService dataService;
    protected RoleSystem roleSystem;
    public WindowLayer windowLayer;

    #region 生命周期
    public override void OnAwake()
    {
        gameRoot = GameRoot.Instance;
        uIService = gameRoot.uIService;
        resService = gameRoot.resService;
        eventService = gameRoot.eventService;
        audioService = gameRoot.audioService;
        dataService = gameRoot.dataService;
        roleSystem = gameRoot.roleSystem;
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
}
