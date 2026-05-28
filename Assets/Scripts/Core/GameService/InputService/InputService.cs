// LevityFramework - 通用 Unity 游戏框架
// 核心服务模块 - InputService 输入服务

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 输入服务：统一的输入服务层
/// 基于 Unity New Input System，通过 PlayerInput 组件驱动
/// </summary>
public class InputService : ILogic
{
    private PlayerInput playerInput;

    // 输入状态（由外部通过 InputAction callbacks 或 PlayerInput Messages 写入）
    public Vector2 MoveInput { get; set; }
    public Vector2 LookInput { get; set; }
    public bool JumpPressed { get; set; }
    public bool JumpHeld { get; set; }
    public bool FirePressed { get; set; }
    public bool FireHeld { get; set; }
    public bool InteractPressed { get; set; }

    public void OnInit() { }
    public void OnEnterState() { }
    public void OnUpdate() { }
    public void UnInit() { }

    /// <summary>
    /// 设置 PlayerInput 组件引用（由 GameRoot 在场景初始化时注入）
    /// </summary>
    public void SetPlayerInput(PlayerInput input)
    {
        playerInput = input;
    }

    #region UI 命中查询 / Action Map 切换

    /// <summary>
    /// 当前指针（鼠标/触摸）是否悬停在任何 UI 元素之上。
    /// 适用于判断点击/拖拽是否被 UI 消费，以避免穿透到游戏世界。
    /// 注意：仅检测 EventSystem 当前处理的 UI；未挂 GraphicRaycaster 的 Canvas 不会命中。
    /// </summary>
    public static bool IsPointerOverUI()
    {
        var es = EventSystem.current;
        return es != null && es.IsPointerOverGameObject();
    }

    /// <summary>
    /// 切换 PlayerInput 当前生效的 Action Map（例如 "Gameplay" / "UI" / "Menu"）。
    /// 切换前需调用 <see cref="SetPlayerInput"/> 注入 PlayerInput 实例。
    /// </summary>
    /// <returns>切换是否成功；当 PlayerInput 未设置或 mapName 不存在时返回 false。</returns>
    public bool SwitchActionMap(string mapName)
    {
        if (playerInput == null)
        {
            Debug.LogWarning("[InputService] SwitchActionMap 失败：PlayerInput 未设置");
            return false;
        }
        if (string.IsNullOrEmpty(mapName)) return false;

        var asset = playerInput.actions;
        if (asset == null || asset.FindActionMap(mapName, throwIfNotFound: false) == null)
        {
            Debug.LogWarning($"[InputService] SwitchActionMap 失败：Action Map '{mapName}' 不存在");
            return false;
        }

        playerInput.SwitchCurrentActionMap(mapName);
        return true;
    }

    /// <summary>
    /// 当前 PlayerInput 生效的 Action Map 名称；未设置 PlayerInput 时返回 null。
    /// </summary>
    public string CurrentActionMap => playerInput != null ? playerInput.currentActionMap?.name : null;

    /// <summary>
    /// 启用 PlayerInput（恢复所有输入处理）。
    /// </summary>
    public void EnableInput()
    {
        if (playerInput != null) playerInput.enabled = true;
    }

    /// <summary>
    /// 禁用 PlayerInput（停止所有输入处理）。
    /// </summary>
    public void DisableInput()
    {
        if (playerInput != null) playerInput.enabled = false;
    }

    #endregion
}
