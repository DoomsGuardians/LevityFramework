// LevityFramework - 通用 Unity 游戏框架
// 核心指令模块 - WindowBehaviour UI 窗口行为基类

using UnityEngine;

/// <summary>
/// UI 窗口的生命周期基类
/// </summary>
public abstract class WindowBehaviour
{
    public string Name { get; set; }
    public Transform transform { get; set; }
    public GameObject gameObject { get; set; }
    public Canvas canvas { get; set; }
    public bool isVisible { get; set; }

    public abstract void OnAwake();
    public abstract void OnShow();
    public abstract void OnUpdate();
    public abstract void OnHide();
    public abstract void OnDestroy();
}
