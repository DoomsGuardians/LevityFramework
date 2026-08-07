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

        bool success = false;
        switch (channel)
        {
            case InputChannel.Gameplay:
                success = TryToggleGameplayInput(on);
                break;
            case InputChannel.UI:
                success = TryToggleUIInput(on);
                break;
            case InputChannel.Naninovel:
                success = TryToggleNaninovelInput(on);
                break;
        }

        // 只有实际执行成功才更新状态，避免状态与实际不同步
        if (success)
        {
            enabled[channel] = on;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[InputRouter] {channel} -> {(on ? "Enabled" : "Disabled")}");
#endif
        }
    }

    /// <summary>
    /// 尝试切换 Gameplay 输入
    /// </summary>
    /// <returns>是否成功执行</returns>
    private static bool TryToggleGameplayInput(bool on)
    {
        // 安全检查：避免在 GameRoot 未初始化时访问导致意外行为
        if (!IsGameRootInitialized())
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("[InputRouter] GameRoot 或 InputService 未初始化，跳过输入切换");
#endif
            return false;
        }

        var svc = GameRoot.Instance.inputService;
        try
        {
            if (on)
                svc.EnableInput();
            else
                svc.DisableInput();
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[InputRouter] ToggleGameplayInput failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 检查 GameRoot 是否已初始化且 InputService 可用
    /// 使用 FindAnyObjectByType 避免触发 MonoSingleton 自动创建
    /// </summary>
    private static bool IsGameRootInitialized()
    {
        var gameRoot = UnityEngine.Object.FindAnyObjectByType<GameRoot>();
        return gameRoot != null && gameRoot.inputService != null;
    }

    /// <summary>
    /// 尝试切换 UI 输入
    /// </summary>
    /// <returns>是否成功执行</returns>
    private static bool TryToggleUIInput(bool on)
    {
        // 可根据项目需要实现 UI 输入的启停
        // 例如通过 EventSystem 或 UI InputActionMap
        return true; // 默认成功（空实现）
    }

    /// <summary>
    /// 尝试切换 Naninovel 输入
    /// </summary>
    /// <returns>是否成功执行</returns>
    private static bool TryToggleNaninovelInput(bool on)
    {
#if NANINOVEL
        try
        {
            if (!Naninovel.Engine.Initialized) return false;
            var m = Naninovel.Engine.GetService<Naninovel.IInputManager>();
            if (m == null) return false;
            m.ProcessInput = on;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[InputRouter] ToggleNaninovel failed: {ex.Message}");
            return false;
        }
#else
        return true; // 无 Naninovel 时默认成功
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
