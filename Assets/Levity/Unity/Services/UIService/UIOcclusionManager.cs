// LevityFramework - 通用 Unity 游戏框架
// UI 服务模块 - UIOcclusionManager 覆盖管理器

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI 覆盖管理器
/// 当全屏 UI 打开时，自动隐藏被覆盖的 UI 以减少 DrawCall
/// 全屏窗口关闭时自动恢复下层 UI
/// </summary>
public class UIOcclusionManager
{
    /// <summary>
    /// 全屏窗口栈（最上面是当前显示的全屏窗口）
    /// </summary>
    private readonly Stack<WindowBase> fullScreenStack = new Stack<WindowBase>();

    /// <summary>
    /// 每个全屏窗口打开时被遮挡的窗口列表
    /// </summary>
    private readonly Dictionary<WindowBase, List<WindowBase>> occludedWindowsMap =
        new Dictionary<WindowBase, List<WindowBase>>();

    /// <summary>
    /// 当全屏窗口打开时调用
    /// 自动隐藏下层可见窗口以减少 DrawCall
    /// </summary>
    /// <param name="fullScreenWindow">打开的全屏窗口</param>
    /// <param name="allVisibleWindows">当前所有可见窗口列表</param>
    public void OnFullScreenWindowOpened(WindowBase fullScreenWindow, List<WindowBase> allVisibleWindows)
    {
        if (fullScreenWindow == null) return;

        List<WindowBase> toOcclude = new List<WindowBase>();

        foreach (WindowBase window in allVisibleWindows)
        {
            // 跳过自身、已隐藏的窗口、以及标记为始终可见的窗口
            if (window == fullScreenWindow) continue;
            if (window == null) continue;
            if (!window.isVisible) continue;
            if (window.IsAlwaysVisible) continue;

            // 禁用 Canvas 而不是 SetActive
            // 这样可以保持窗口状态，同时减少渲染开销
            if (window.canvas != null)
            {
                window.canvas.enabled = false;
            }

            // 通知窗口被覆盖（可选：让窗口执行暂停逻辑）
            window.OnPause();

            toOcclude.Add(window);
        }

        // 记录这个全屏窗口遮挡了哪些窗口
        occludedWindowsMap[fullScreenWindow] = toOcclude;
        fullScreenStack.Push(fullScreenWindow);

        Debug.Log($"[UIOcclusionManager] 全屏窗口 {fullScreenWindow.Name} 打开，遮挡了 {toOcclude.Count} 个窗口");
    }

    /// <summary>
    /// 当全屏窗口关闭时调用
    /// 自动恢复被遮挡的窗口
    /// </summary>
    /// <param name="fullScreenWindow">关闭的全屏窗口</param>
    public void OnFullScreenWindowClosed(WindowBase fullScreenWindow)
    {
        if (fullScreenWindow == null) return;

        // 从栈中移除
        if (fullScreenStack.Count > 0 && fullScreenStack.Peek() == fullScreenWindow)
        {
            fullScreenStack.Pop();
        }

        // 获取被这个窗口遮挡的窗口列表
        if (!occludedWindowsMap.TryGetValue(fullScreenWindow, out List<WindowBase> occludedWindows))
        {
            return;
        }

        // 只有当没有其他全屏窗口时才恢复
        // 如果还有其他全屏窗口，被遮挡的窗口应该继续保持隐藏
        if (fullScreenStack.Count == 0)
        {
            foreach (WindowBase window in occludedWindows)
            {
                if (window != null && window.canvas != null)
                {
                    window.canvas.enabled = true;
                    window.OnResume();
                }
            }

            Debug.Log($"[UIOcclusionManager] 全屏窗口 {fullScreenWindow.Name} 关闭，恢复了 {occludedWindows.Count} 个窗口");
        }
        else
        {
            // 如果还有其他全屏窗口，将被遮挡的窗口转移到下一个全屏窗口
            WindowBase nextFullScreen = fullScreenStack.Peek();
            if (occludedWindowsMap.TryGetValue(nextFullScreen, out List<WindowBase> nextOccluded))
            {
                // 合并列表，避免重复
                foreach (WindowBase window in occludedWindows)
                {
                    if (!nextOccluded.Contains(window))
                    {
                        nextOccluded.Add(window);
                    }
                }
            }

            Debug.Log($"[UIOcclusionManager] 全屏窗口 {fullScreenWindow.Name} 关闭，但仍有 {fullScreenStack.Count} 个全屏窗口，保持遮挡状态");
        }

        // 清理记录
        occludedWindowsMap.Remove(fullScreenWindow);
    }

    /// <summary>
    /// 检查是否有全屏窗口正在显示
    /// </summary>
    public bool HasFullScreenWindow => fullScreenStack.Count > 0;

    /// <summary>
    /// 获取当前全屏窗口
    /// </summary>
    public WindowBase CurrentFullScreenWindow => fullScreenStack.Count > 0 ? fullScreenStack.Peek() : null;

    /// <summary>
    /// 获取当前全屏窗口数量
    /// </summary>
    public int FullScreenWindowCount => fullScreenStack.Count;

    /// <summary>
    /// 强制恢复所有被遮挡的窗口
    /// 通常在场景切换或重置时调用
    /// </summary>
    public void ForceRestoreAll()
    {
        foreach (var kvp in occludedWindowsMap)
        {
            foreach (WindowBase window in kvp.Value)
            {
                if (window != null && window.canvas != null)
                {
                    window.canvas.enabled = true;
                    window.OnResume();
                }
            }
        }

        fullScreenStack.Clear();
        occludedWindowsMap.Clear();

        Debug.Log("[UIOcclusionManager] 强制恢复所有被遮挡的窗口");
    }

    /// <summary>
    /// 清理所有记录
    /// </summary>
    public void Clear()
    {
        fullScreenStack.Clear();
        occludedWindowsMap.Clear();
    }

    /// <summary>
    /// 检查窗口是否被遮挡
    /// </summary>
    /// <param name="window">要检查的窗口</param>
    /// <returns>是否被遮挡</returns>
    public bool IsWindowOccluded(WindowBase window)
    {
        foreach (var kvp in occludedWindowsMap)
        {
            if (kvp.Value.Contains(window))
            {
                return true;
            }
        }
        return false;
    }
}
