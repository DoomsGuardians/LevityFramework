// LevityFramework - 通用 Unity 游戏框架
// UI 服务模块 - UILayerManager 层级管理器

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 层级管理器
/// 管理 6 层 Canvas 结构，每层对应一个 UILayer 枚举值
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
    /// 初始化所有层级 Canvas
    /// </summary>
    public void Initialize()
    {
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

        // Scene 层使用 World Space，其他层使用 Screen Space - Overlay
        if (layer == UILayer.Scene)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            if (uiCamera != null)
            {
                canvas.worldCamera = uiCamera;
            }
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
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
    /// 从旧版 WindowLayer 转换到新版 UILayer
    /// </summary>
    /// <param name="windowLayer">旧版层级</param>
    /// <returns>新版层级</returns>
#pragma warning disable CS0618 // 忽略过时警告
    public static UILayer ConvertFromWindowLayer(WindowLayer windowLayer)
    {
        return windowLayer switch
        {
            WindowLayer.Base => UILayer.Normal,
            WindowLayer.Pop => UILayer.Top,
            WindowLayer.Top => UILayer.Tip,
            _ => UILayer.Normal
        };
    }
#pragma warning restore CS0618

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
