// LevityFramework - 通用 Unity 游戏框架
// 核心服务模块 - InputService 输入服务

using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 输入服务：统一的输入服务层
/// 基于 Unity Input System，提供机动、油门、视角、武器输入接口
/// </summary>
public class InputService : ILogic
{
    // 输入配置（可在 Inspector 或代码中配置）
    private PlayerInput playerInput;
    private InputActionAsset inputActions;

    // 输入状态
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool FirePressed { get; private set; }
    public bool FireHeld { get; private set; }
    public bool InteractPressed { get; private set; }

    // 输入启用状态
    public bool InputEnabled { get; private set; } = true;

    public void OnInit()
    {
        // 可以在这里初始化 Input System
        // 如果使用 PlayerInput 组件，可以在 GameRoot 中引用
    }

    public void OnEnterState() { }

    public void OnUpdate()
    {
        if (!InputEnabled) return;

        // 使用旧输入系统的默认实现（可根据项目替换为 Input System）
        UpdateInputs();
    }

    public void UnInit() { }

    /// <summary>
    /// 更新输入状态（使用旧输入系统作为默认实现）
    /// </summary>
    private void UpdateInputs()
    {
        // 移动输入
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        MoveInput = new Vector2(horizontal, vertical);

        // 视角输入
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        LookInput = new Vector2(mouseX, mouseY);

        // 跳跃
        JumpPressed = Input.GetButtonDown("Jump");
        JumpHeld = Input.GetButton("Jump");

        // 开火
        FirePressed = Input.GetButtonDown("Fire1");
        FireHeld = Input.GetButton("Fire1");

        // 交互
        InteractPressed = Input.GetKeyDown(KeyCode.E);
    }

    /// <summary>
    /// 启用输入
    /// </summary>
    public void EnableInput()
    {
        InputEnabled = true;
    }

    /// <summary>
    /// 禁用输入
    /// </summary>
    public void DisableInput()
    {
        InputEnabled = false;
        ResetInputs();
    }

    /// <summary>
    /// 重置所有输入状态
    /// </summary>
    private void ResetInputs()
    {
        MoveInput = Vector2.zero;
        LookInput = Vector2.zero;
        JumpPressed = false;
        JumpHeld = false;
        FirePressed = false;
        FireHeld = false;
        InteractPressed = false;
    }

    /// <summary>
    /// 设置 PlayerInput 组件引用
    /// </summary>
    public void SetPlayerInput(PlayerInput input)
    {
        playerInput = input;
    }
}
