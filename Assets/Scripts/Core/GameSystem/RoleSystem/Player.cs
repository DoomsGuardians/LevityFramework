// LevityFramework - 通用 Unity 游戏框架
// 核心系统模块 - Player 玩家角色基类

using UnityEngine;

/// <summary>
/// 玩家角色的基类实现
/// </summary>
public class Player : MonoBehaviour, IMonoLogic
{
    protected GameRoot gameRoot;
    protected InputService inputService;
    protected TimerService timerService;

    public virtual void OnAwake()
    {
        gameRoot = GameRoot.Instance;
        inputService = gameRoot.inputService;
        timerService = gameRoot.timerService;
    }

    public virtual void OnShow() { }

    public virtual void OnExit() { }

    public virtual void UnInit() { }

    protected virtual void Update() { }

    protected virtual void FixedUpdate() { }

    protected virtual void LateUpdate() { }
}
