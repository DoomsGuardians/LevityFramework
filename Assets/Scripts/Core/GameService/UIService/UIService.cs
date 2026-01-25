// LevityFramework - 通用 Unity 游戏框架
// 核心服务模块 - UIService UI 服务

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// UI 服务：管理 UI 窗口的生命周期、显示隐藏、层级管理
/// 集成了层级管理、Order 分配、覆盖管理等功能
/// </summary>
public class UIService : ILogic
{
    private Dictionary<string, WindowBase> windowDic = new Dictionary<string, WindowBase>();
    private List<WindowBase> windowList = new List<WindowBase>();

    // 新增管理器
    private UILayerManager layerManager;
    private UIOrderManager orderManager;
    private UIOcclusionManager occlusionManager;

    // UI 根节点
    private Transform uiRoot;
    private Camera uiCamera;

    /// <summary>
    /// 层级管理器
    /// </summary>
    public UILayerManager LayerManager => layerManager;

    /// <summary>
    /// Order 管理器
    /// </summary>
    public UIOrderManager OrderManager => orderManager;

    /// <summary>
    /// 覆盖管理器
    /// </summary>
    public UIOcclusionManager OcclusionManager => occlusionManager;

    public void OnInit()
    {
        windowDic.Clear();
        windowList.Clear();

        // 初始化管理器
        orderManager = new UIOrderManager();
        occlusionManager = new UIOcclusionManager();
    }

    /// <summary>
    /// 初始化 UI 层级系统
    /// 需要在场景准备好后调用
    /// </summary>
    /// <param name="uiRoot">UI 根节点</param>
    /// <param name="uiCamera">UI 相机（可选，用于 Screen Space - Camera 模式）</param>
    public void InitLayerSystem(Transform uiRoot, Camera uiCamera = null)
    {
        this.uiRoot = uiRoot;
        this.uiCamera = uiCamera;

        layerManager = new UILayerManager(uiRoot, uiCamera);
        layerManager.Initialize();

        Debug.Log("[UIService] 层级系统初始化完成");
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

        // 清理管理器
        layerManager?.Cleanup();
        occlusionManager?.Clear();
        orderManager?.ResetAll();
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
            window.Name = name;
            window.OnAwake();

            // 分配 Order
            if (orderManager != null)
            {
                window.AllocatedOrder = orderManager.AllocateOrder(window.uiLayer);
                if (window.canvas != null)
                {
                    window.canvas.sortingOrder = window.AllocatedOrder;
                }
            }
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

            // 处理全屏窗口覆盖
            if (window.IsFullScreen && occlusionManager != null)
            {
                var visibleWindows = GetVisibleWindows();
                occlusionManager.OnFullScreenWindowOpened(window, visibleWindows);
            }

            return window as T;
        }
        return null;
    }

    /// <summary>
    /// 显示窗口（带动画）
    /// </summary>
    public T ShowWindowWithAnimation<T>(string name, Action onComplete = null) where T : WindowBase
    {
        if (windowDic.TryGetValue(name, out var window))
        {
            window.SetVisible(true);
            window.OnShow();

            // 处理全屏窗口覆盖
            if (window.IsFullScreen && occlusionManager != null)
            {
                var visibleWindows = GetVisibleWindows();
                occlusionManager.OnFullScreenWindowOpened(window, visibleWindows);
            }

            // 播放显示动画
            window.PlayShowAnimation(onComplete);

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
            // 处理全屏窗口关闭
            if (window.IsFullScreen && occlusionManager != null)
            {
                occlusionManager.OnFullScreenWindowClosed(window);
            }

            window.OnHide();
            window.SetVisible(false);
        }
    }

    /// <summary>
    /// 隐藏窗口（带动画）
    /// </summary>
    public void HideWindowWithAnimation(string name, Action onComplete = null)
    {
        if (windowDic.TryGetValue(name, out var window))
        {
            // 播放隐藏动画，动画完成后再隐藏
            window.PlayHideAnimation(() =>
            {
                // 处理全屏窗口关闭
                if (window.IsFullScreen && occlusionManager != null)
                {
                    occlusionManager.OnFullScreenWindowClosed(window);
                }

                window.OnHide();
                window.SetVisible(false);
                onComplete?.Invoke();
            });
        }
        else
        {
            onComplete?.Invoke();
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
            // 释放 Order
            if (orderManager != null)
            {
                orderManager.ReleaseOrder(window.uiLayer, window.AllocatedOrder);
            }

            window.OnDestroy();
            windowDic.Remove(name);
            windowList.Remove(window);
        }
    }

    #region 新增功能方法

    /// <summary>
    /// 获取层级根节点
    /// </summary>
    /// <param name="layer">UI 层级</param>
    /// <returns>层级的 Transform</returns>
    public Transform GetLayerRoot(UILayer layer)
    {
        return layerManager?.GetLayerRoot(layer);
    }

    /// <summary>
    /// 获取层级 Canvas
    /// </summary>
    /// <param name="layer">UI 层级</param>
    /// <returns>层级的 Canvas</returns>
    public Canvas GetLayerCanvas(UILayer layer)
    {
        return layerManager?.GetLayerCanvas(layer);
    }

    /// <summary>
    /// 分配 Order
    /// </summary>
    /// <param name="layer">UI 层级</param>
    /// <returns>分配的 Order 值</returns>
    public int AllocateOrder(UILayer layer)
    {
        return orderManager?.AllocateOrder(layer) ?? (int)layer;
    }

    /// <summary>
    /// 释放 Order
    /// </summary>
    /// <param name="layer">UI 层级</param>
    /// <param name="order">要释放的 Order 值</param>
    public void ReleaseOrder(UILayer layer, int order)
    {
        orderManager?.ReleaseOrder(layer, order);
    }

    /// <summary>
    /// 将窗口置顶
    /// </summary>
    /// <param name="name">窗口名称</param>
    public void BringWindowToTop(string name)
    {
        if (windowDic.TryGetValue(name, out var window) && orderManager != null)
        {
            int newOrder = orderManager.BringToTop(window.uiLayer, window.AllocatedOrder);
            window.AllocatedOrder = newOrder;
            if (window.canvas != null)
            {
                window.canvas.sortingOrder = newOrder;
            }
        }
    }

    /// <summary>
    /// 获取所有可见窗口
    /// </summary>
    /// <returns>可见窗口列表</returns>
    public List<WindowBase> GetVisibleWindows()
    {
        return windowList.Where(w => w.isVisible).ToList();
    }

    /// <summary>
    /// 获取所有窗口
    /// </summary>
    /// <returns>窗口列表</returns>
    public List<WindowBase> GetAllWindows()
    {
        return new List<WindowBase>(windowList);
    }

    /// <summary>
    /// 检查是否有全屏窗口正在显示
    /// </summary>
    public bool HasFullScreenWindow => occlusionManager?.HasFullScreenWindow ?? false;

    /// <summary>
    /// 获取当前全屏窗口
    /// </summary>
    public WindowBase CurrentFullScreenWindow => occlusionManager?.CurrentFullScreenWindow;

    /// <summary>
    /// 隐藏所有窗口
    /// </summary>
    public void HideAllWindows()
    {
        foreach (var window in windowList)
        {
            if (window.isVisible)
            {
                window.OnHide();
                window.SetVisible(false);
            }
        }
    }

    /// <summary>
    /// 隐藏指定层级的所有窗口
    /// </summary>
    /// <param name="layer">UI 层级</param>
    public void HideAllWindowsInLayer(UILayer layer)
    {
        foreach (var window in windowList)
        {
            if (window.isVisible && window.uiLayer == layer)
            {
                window.OnHide();
                window.SetVisible(false);
            }
        }
    }

    #endregion
}
