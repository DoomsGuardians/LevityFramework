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
    private static Camera defaultUICamera;

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
    /// <param name="uiCamera">UI 相机；传 null 时使用 <see cref="GetOrCreateDefaultUICamera"/> 提供的全局默认 UI Camera</param>
    /// <param name="layerRenderModes">层级 → RenderMode 的覆盖；未列出的层级使用默认配置</param>
    public void InitLayerSystem(Transform uiRoot, Camera uiCamera = null, IDictionary<UILayer, RenderMode> layerRenderModes = null)
    {
        this.uiRoot = uiRoot;
        this.uiCamera = uiCamera != null ? uiCamera : GetOrCreateDefaultUICamera();

        layerManager = new UILayerManager(uiRoot, this.uiCamera);
        layerManager.Initialize(layerRenderModes);

        Debug.Log("[UIService] 层级系统初始化完成");
    }

    /// <summary>
    /// 获取或创建框架默认的全局 UI Camera。
    /// 该相机带 DontDestroyOnLoad、不渲染任何 Layer（cullingMask = 0），仅作为 Screen Space - Camera Canvas 的承载相机。
    /// 适用于场景未自带 UI Camera 时的回退。
    /// </summary>
    public Camera GetOrCreateDefaultUICamera()
    {
        if (defaultUICamera != null) return defaultUICamera;

        var go = new GameObject("[UIService] DefaultUICamera");
        UnityEngine.Object.DontDestroyOnLoad(go);
        defaultUICamera = go.AddComponent<Camera>();
        defaultUICamera.clearFlags = CameraClearFlags.Depth;
        defaultUICamera.cullingMask = 0;            // 不渲染任何 Layer，仅作为 Canvas 的 worldCamera 引用
        defaultUICamera.orthographic = true;
        defaultUICamera.depth = 100;                 // 排在游戏主相机之后
        defaultUICamera.nearClipPlane = 0.1f;
        defaultUICamera.farClipPlane = 100f;
        return defaultUICamera;
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
        DestroyAllWindowsInternal(resetLayerSystem: true);
    }

    /// <summary>
    /// 关闭并销毁所有窗口，重置 Order / Occlusion 状态。
    /// 适用场景：切换主菜单 / 关卡 / 大场景前的硬清理。
    /// 与 <see cref="HideAllWindows"/> 的区别：HideAllWindows 保留窗口注册以便后续恢复，
    /// CloseAllWindows 则把窗口完全销毁、从注册表移除，需要再次显示时必须重新 RegisterWindow。
    /// </summary>
    /// <remarks>
    /// 层级系统（LayerManager）默认保留——层级根节点通常与场景共生，由场景管理；
    /// 如需连带重建层级，请在调用本方法后自行重新调用 <see cref="InitLayerSystem"/>。
    /// </remarks>
    public void CloseAllWindows()
    {
        DestroyAllWindowsInternal(resetLayerSystem: false);
    }

    private void DestroyAllWindowsInternal(bool resetLayerSystem)
    {
        // 复制一份避免在 OnDestroy 回调中修改 windowList 引发的迭代异常
        var snapshot = windowList.ToArray();
        foreach (var window in snapshot)
        {
            window.OnDestroy();
        }
        windowDic.Clear();
        windowList.Clear();

        occlusionManager?.Clear();
        orderManager?.ResetAll();

        if (resetLayerSystem)
        {
            layerManager?.Cleanup();
        }
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
