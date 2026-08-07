// LevityFramework - 通用 Unity 游戏框架
// UI 服务模块 - UILayerManager 层级管理器

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 层级管理器
/// 管理 6 层 Canvas 结构，每层对应一个 UILayer 枚举值
/// 每层独立配置 RenderMode（Overlay / Camera / WorldSpace）
/// </summary>
public class UILayerManager
{
    private readonly Dictionary<UILayer, Canvas> layerCanvases = new Dictionary<UILayer, Canvas>();
    private readonly Dictionary<UILayer, Transform> layerRoots = new Dictionary<UILayer, Transform>();
    private readonly Transform uiRoot;
    private readonly Camera uiCamera;

    /// <summary>
    /// 每层的基础 Sorting Order
    /// </summary>
    private static readonly Dictionary<UILayer, int> LayerBaseOrders = new Dictionary<UILayer, int>
    {
        { UILayer.Scene, 0 },
        { UILayer.Background, 100 },
        { UILayer.Normal, 200 },
        { UILayer.Info, 300 },
        { UILayer.Top, 400 },
        { UILayer.Tip, 500 }
    };

    /// <summary>
    /// 默认每层 RenderMode 配置。
    /// Scene 用 WorldSpace；其它默认 Overlay；可在 Initialize 时通过 layerRenderModes 覆盖。
    /// </summary>
    private static readonly Dictionary<UILayer, RenderMode> DefaultLayerRenderModes = new Dictionary<UILayer, RenderMode>
    {
        { UILayer.Scene, RenderMode.WorldSpace },
        { UILayer.Background, RenderMode.ScreenSpaceOverlay },
        { UILayer.Normal, RenderMode.ScreenSpaceOverlay },
        { UILayer.Info, RenderMode.ScreenSpaceOverlay },
        { UILayer.Top, RenderMode.ScreenSpaceOverlay },
        { UILayer.Tip, RenderMode.ScreenSpaceOverlay }
    };

    private Dictionary<UILayer, RenderMode> activeLayerRenderModes;

    /// <summary>
    /// 创建 UI 层级管理器
    /// </summary>
    /// <param name="uiRoot">UI 根节点</param>
    /// <param name="uiCamera">UI 相机（可选，用于 Screen Space - Camera 模式）</param>
    public UILayerManager(Transform uiRoot, Camera uiCamera = null)
    {
        this.uiRoot = uiRoot;
        this.uiCamera = uiCamera;
    }

    /// <summary>
    /// 初始化所有层级 Canvas（使用默认 RenderMode 配置）
    /// </summary>
    public void Initialize()
    {
        Initialize(null);
    }

    /// <summary>
    /// 初始化所有层级 Canvas，可按层级覆盖默认 RenderMode。
    /// 当指定为 ScreenSpaceCamera 时需要 uiCamera 已注入；否则该层会回退到 Overlay。
    /// </summary>
    /// <param name="layerRenderModes">层级 → RenderMode 的覆盖；未列出的层级用默认配置</param>
    public void Initialize(IDictionary<UILayer, RenderMode> layerRenderModes)
    {
        activeLayerRenderModes = new Dictionary<UILayer, RenderMode>(DefaultLayerRenderModes);
        if (layerRenderModes != null)
        {
            foreach (var kvp in layerRenderModes)
            {
                activeLayerRenderModes[kvp.Key] = kvp.Value;
            }
        }

        foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
        {
            CreateLayerCanvas(layer);
        }
    }

    /// <summary>
    /// 创建单个层级的 Canvas
    /// </summary>
    private void CreateLayerCanvas(UILayer layer)
    {
        // 创建层级 GameObject
        GameObject layerGo = new GameObject($"Layer_{layer}");
        layerGo.transform.SetParent(uiRoot, false);

        // 添加 RectTransform
        RectTransform rectTransform = layerGo.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        // 添加 Canvas
        Canvas canvas = layerGo.AddComponent<Canvas>();

        var renderMode = ResolveRenderMode(layer);
        canvas.renderMode = renderMode;
        if (renderMode == RenderMode.ScreenSpaceCamera || renderMode == RenderMode.WorldSpace)
        {
            if (uiCamera != null)
            {
                canvas.worldCamera = uiCamera;
            }
        }

        canvas.sortingOrder = LayerBaseOrders[layer];

        // 添加 CanvasScaler
        CanvasScaler scaler = layerGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        // 添加 GraphicRaycaster
        layerGo.AddComponent<GraphicRaycaster>();

        // 缓存引用
        layerCanvases[layer] = canvas;
        layerRoots[layer] = layerGo.transform;
    }

    /// <summary>
    /// 解析某层应该使用的 RenderMode：未配置 ScreenSpaceCamera 但又没有 uiCamera 时回退到 Overlay。
    /// </summary>
    private RenderMode ResolveRenderMode(UILayer layer)
    {
        var requested = activeLayerRenderModes != null && activeLayerRenderModes.TryGetValue(layer, out var rm)
            ? rm
            : DefaultLayerRenderModes[layer];

        if (requested == RenderMode.ScreenSpaceCamera && uiCamera == null)
        {
            Debug.LogWarning($"[UILayerManager] 层级 {layer} 配置为 ScreenSpaceCamera 但未注入 uiCamera，自动回退为 ScreenSpaceOverlay");
            return RenderMode.ScreenSpaceOverlay;
        }

        return requested;
    }

    /// <summary>
    /// 获取层级根节点
    /// </summary>
    /// <param name="layer">UI 层级</param>
    /// <returns>层级的 Transform，不存在则返回 null</returns>
    public Transform GetLayerRoot(UILayer layer)
    {
        return layerRoots.TryGetValue(layer, out Transform root) ? root : null;
    }

    /// <summary>
    /// 获取层级 Canvas
    /// </summary>
    /// <param name="layer">UI 层级</param>
    /// <returns>层级的 Canvas，不存在则返回 null</returns>
    public Canvas GetLayerCanvas(UILayer layer)
    {
        return layerCanvases.TryGetValue(layer, out Canvas canvas) ? canvas : null;
    }

    /// <summary>
    /// 获取层级的基础 Sorting Order
    /// </summary>
    /// <param name="layer">UI 层级</param>
    /// <returns>基础 Sorting Order 值</returns>
    public int GetLayerBaseOrder(UILayer layer)
    {
        return LayerBaseOrders.TryGetValue(layer, out int order) ? order : 0;
    }

    /// <summary>
    /// 设置层级的激活状态
    /// </summary>
    /// <param name="layer">UI 层级</param>
    /// <param name="active">是否激活</param>
    public void SetLayerActive(UILayer layer, bool active)
    {
        if (layerRoots.TryGetValue(layer, out Transform root))
        {
            root.gameObject.SetActive(active);
        }
    }

    /// <summary>
    /// 设置层级的射线检测开关
    /// </summary>
    /// <param name="layer">UI 层级</param>
    /// <param name="enable">是否启用</param>
    public void SetLayerRaycastEnabled(UILayer layer, bool enable)
    {
        if (layerCanvases.TryGetValue(layer, out Canvas canvas))
        {
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.enabled = enable;
            }
        }
    }

    /// <summary>
    /// 清理所有层级
    /// </summary>
    public void Cleanup()
    {
        foreach (var kvp in layerRoots)
        {
            if (kvp.Value != null)
            {
                UnityEngine.Object.Destroy(kvp.Value.gameObject);
            }
        }
        layerCanvases.Clear();
        layerRoots.Clear();
    }
}
