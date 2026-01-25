// LevityFramework - 通用 Unity 游戏框架
// 核心服务模块 - InputRouter 输入通道路由器
// 作用：集中控制输入通道的启停；支持多来源叠加上锁/解锁，避免分散 Enable/Disable

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 输入通道路由器（静态工具类）
/// 支持多源锁定机制，管理 Gameplay/UI/Naninovel 三个输入通道
/// </summary>
public static class InputRouter
{
    // 通道 -> 锁拥有者集合（引用计数式开关）
    private static readonly Dictionary<InputChannel, HashSet<object>> locks = new()
    {
        [InputChannel.Gameplay] = new HashSet<object>(),
        [InputChannel.UI] = new HashSet<object>(),
        [InputChannel.Naninovel] = new HashSet<object>()
    };

    // 缓存通道当前状态，避免重复 Enable/Disable 造成不必要的开销
    private static readonly Dictionary<InputChannel, bool> enabled = new()
    {
        [InputChannel.Gameplay] = true,
        [InputChannel.UI] = true,
        [InputChannel.Naninovel] = true
    };

    /// <summary>
    /// 申请关闭某输入通道（上锁）。当首个上锁出现时，实际关闭该通道。
    /// </summary>
    public static void Acquire(InputChannel channel, object owner)
    {
        if (owner == null) owner = typeof(InputRouter);
        var set = locks[channel];
        bool wasEmpty = set.Count == 0;
        set.Add(owner);
        if (wasEmpty && set.Count == 1)
            ApplyEnabled(channel, false);
    }

    /// <summary>
    /// 释放对某通道的锁；当最后一个锁释放时，实际打开该通道。
    /// </summary>
    public static void Release(InputChannel channel, object owner)
    {
        if (owner == null) owner = typeof(InputRouter);
        var set = locks[channel];
        set.Remove(owner);
        if (set.Count == 0)
            ApplyEnabled(channel, true);
    }

    /// <summary>
    /// 直接设置通道开启状态（内部会转换为 Acquire/Release）。
    /// </summary>
    public static void SetEnabled(InputChannel channel, bool on, object owner = null)
    {
        if (on) Release(channel, owner); else Acquire(channel, owner);
    }

    public static bool IsEnabled(InputChannel channel) => enabled[channel];
    public static int LockCount(InputChannel channel) => locks[channel].Count;

    // 实际执行启停
    private static void ApplyEnabled(InputChannel channel, bool on)
    {
        if (enabled[channel] == on) return;

        switch (channel)
        {
            case InputChannel.Gameplay:
                ToggleGameplayInput(on);
                break;
            case InputChannel.UI:
                ToggleUIInput(on);
                break;
            case InputChannel.Naninovel:
                ToggleNaninovelInput(on);
                break;
        }

        enabled[channel] = on;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[InputRouter] {channel} -> {(on ? "Enabled" : "Disabled")}");
#endif
    }

    // 启停 Gameplay 输入
    private static void ToggleGameplayInput(bool on)
    {
        var svc = GameRoot.Instance?.inputService;
        if (svc == null) return;

        try
        {
            if (on)
                svc.EnableInput();
            else
                svc.DisableInput();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[InputRouter] ToggleGameplayInput failed: {ex.Message}");
        }
    }

    // 启停 UI 输入
    private static void ToggleUIInput(bool on)
    {
        // 可根据项目需要实现 UI 输入的启停
        // 例如通过 EventSystem 或 UI InputActionMap
    }

    // 启停 Naninovel 的输入
    private static void ToggleNaninovelInput(bool on)
    {
#if NANINOVEL
        try
        {
            if (!Naninovel.Engine.Initialized) return;
            var m = Naninovel.Engine.GetService<Naninovel.IInputManager>();
            if (m == null) return;
            m.ProcessInput = on;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[InputRouter] ToggleNaninovel failed: {ex.Message}");
        }
#endif
    }

    /// <summary>
    /// 强制清空指定通道的所有锁，并开启该通道。用于场景/Stage 快速切换后兜底恢复输入。
    /// </summary>
    public static void ClearChannel(InputChannel channel)
    {
        var set = locks[channel];
        if (set.Count > 0)
        {
            set.Clear();
        }
        ApplyEnabled(channel, true);
    }

    /// <summary>
    /// 强制清空所有通道的锁并恢复启用。慎用：仅在 Stage 重载、全局重置输入时调用。
    /// </summary>
    public static void ClearAll()
    {
        ClearChannel(InputChannel.Gameplay);
        ClearChannel(InputChannel.UI);
        ClearChannel(InputChannel.Naninovel);
    }
}
