// LevityFramework - 通用 Unity 游戏框架
// 核心服务模块 - UIService UI 服务

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI 服务：管理 UI 窗口的生命周期、显示隐藏、层级管理
/// </summary>
public class UIService : ILogic
{
    private Dictionary<string, WindowBase> windowDic = new Dictionary<string, WindowBase>();
    private List<WindowBase> windowList = new List<WindowBase>();

    public void OnInit()
    {
        windowDic.Clear();
        windowList.Clear();
    }

    public void OnEnterState() { }

    public void OnUpdate()
    {
        foreach (var window in windowList)
        {
            if (window.isVisible)
            {
                window.OnUpdate();
            }
        }
    }

    public void UnInit()
    {
        foreach (var window in windowList)
        {
            window.OnDestroy();
        }
        windowDic.Clear();
        windowList.Clear();
    }

    /// <summary>
    /// 注册窗口
    /// </summary>
    public void RegisterWindow(string name, WindowBase window)
    {
        if (!windowDic.ContainsKey(name))
        {
            windowDic[name] = window;
            windowList.Add(window);
            window.OnAwake();
        }
    }

    /// <summary>
    /// 显示窗口
    /// </summary>
    public T ShowWindow<T>(string name) where T : WindowBase
    {
        if (windowDic.TryGetValue(name, out var window))
        {
            window.SetVisible(true);
            window.OnShow();
            return window as T;
        }
        return null;
    }

    /// <summary>
    /// 隐藏窗口
    /// </summary>
    public void HideWindow(string name)
    {
        if (windowDic.TryGetValue(name, out var window))
        {
            window.OnHide();
            window.SetVisible(false);
        }
    }

    /// <summary>
    /// 获取窗口
    /// </summary>
    public T GetWindow<T>(string name) where T : WindowBase
    {
        if (windowDic.TryGetValue(name, out var window))
        {
            return window as T;
        }
        return null;
    }

    /// <summary>
    /// 销毁窗口
    /// </summary>
    public void DestroyWindow(string name)
    {
        if (windowDic.TryGetValue(name, out var window))
        {
            window.OnDestroy();
            windowDic.Remove(name);
            windowList.Remove(window);
        }
    }
}
