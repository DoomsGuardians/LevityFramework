// LevityFramework - 通用 Unity 游戏框架
// UI 服务模块 - UIOrderManager Order In Layer 管理器

using System;
using System.Collections.Generic;

/// <summary>
/// UI Order In Layer 自动分配管理器
/// 每个层级内自动分配递增的 Order，确保新打开的窗口显示在最上层
/// </summary>
public class UIOrderManager
{
    private readonly Dictionary<UILayer, int> currentOrders = new Dictionary<UILayer, int>();
    private readonly Dictionary<UILayer, Stack<int>> recycledOrders = new Dictionary<UILayer, Stack<int>>();
    private readonly Dictionary<UILayer, HashSet<int>> allocatedOrders = new Dictionary<UILayer, HashSet<int>>();

    /// <summary>
    /// 每个窗口之间的 Order 间隔
    /// 预留空间用于窗口内的子元素（如特效、弹出菜单等）
    /// </summary>
    public const int ORDER_GAP = 10;

    /// <summary>
    /// 每层的最大 Order 范围
    /// </summary>
    public const int LAYER_ORDER_RANGE = 100;

    public UIOrderManager()
    {
        Initialize();
    }

    /// <summary>
    /// 初始化所有层级的 Order 计数器
    /// </summary>
    private void Initialize()
    {
        foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
        {
            currentOrders[layer] = (int)layer;
            recycledOrders[layer] = new Stack<int>();
            allocatedOrders[layer] = new HashSet<int>();
        }
    }

    /// <summary>
    /// 分配一个新的 Order 值
    /// 优先使用回收的 Order，否则分配新的
    /// </summary>
    /// <param name="layer">UI 层级</param>
    /// <returns>分配的 Order 值</returns>
    public int AllocateOrder(UILayer layer)
    {
        int order;

        // 优先使用回收的 Order
        if (recycledOrders[layer].Count > 0)
        {
            order = recycledOrders[layer].Pop();
        }
        else
        {
            order = currentOrders[layer];
            currentOrders[layer] += ORDER_GAP;

            // 检查是否超出层级范围
            int maxOrder = (int)layer + LAYER_ORDER_RANGE - 1;
            if (currentOrders[layer] > maxOrder)
            {
                UnityEngine.Debug.LogWarning($"[UIOrderManager] Layer {layer} Order 超出范围，可能影响其他层级显示");
            }
        }

        allocatedOrders[layer].Add(order);
        return order;
    }

    /// <summary>
    /// 回收 Order 值（窗口关闭时调用）
    /// </summary>
    /// <param name="layer">UI 层级</param>
    /// <param name="order">要回收的 Order 值</param>
    public void ReleaseOrder(UILayer layer, int order)
    {
        if (allocatedOrders[layer].Contains(order))
        {
            allocatedOrders[layer].Remove(order);
            recycledOrders[layer].Push(order);
        }
    }

    /// <summary>
    /// 将指定 Order 提升到该层最顶部
    /// 用于将已打开的窗口置顶
    /// </summary>
    /// <param name="layer">UI 层级</param>
    /// <param name="currentOrder">当前 Order</param>
    /// <returns>新的 Order 值</returns>
    public int BringToTop(UILayer layer, int currentOrder)
    {
        // 先回收当前 Order
        ReleaseOrder(layer, currentOrder);
        // 分配新的最高 Order
        return AllocateOrder(layer);
    }

    /// <summary>
    /// 获取当前层级的最高 Order
    /// </summary>
    /// <param name="layer">UI 层级</param>
    /// <returns>当前最高 Order 值</returns>
    public int GetCurrentMaxOrder(UILayer layer)
    {
        return currentOrders[layer] - ORDER_GAP;
    }

    /// <summary>
    /// 获取层级已分配的 Order 数量
    /// </summary>
    /// <param name="layer">UI 层级</param>
    /// <returns>已分配的数量</returns>
    public int GetAllocatedCount(UILayer layer)
    {
        return allocatedOrders[layer].Count;
    }

    /// <summary>
    /// 重置指定层级的 Order 分配
    /// </summary>
    /// <param name="layer">UI 层级</param>
    public void ResetLayer(UILayer layer)
    {
        currentOrders[layer] = (int)layer;
        recycledOrders[layer].Clear();
        allocatedOrders[layer].Clear();
    }

    /// <summary>
    /// 重置所有层级的 Order 分配
    /// </summary>
    public void ResetAll()
    {
        foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
        {
            ResetLayer(layer);
        }
    }

    /// <summary>
    /// 检查 Order 是否在指定层级范围内
    /// </summary>
    /// <param name="layer">UI 层级</param>
    /// <param name="order">Order 值</param>
    /// <returns>是否在范围内</returns>
    public bool IsOrderInLayerRange(UILayer layer, int order)
    {
        int baseOrder = (int)layer;
        return order >= baseOrder && order < baseOrder + LAYER_ORDER_RANGE;
    }
}
