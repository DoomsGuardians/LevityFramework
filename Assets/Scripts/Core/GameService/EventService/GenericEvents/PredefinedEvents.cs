// LevityFramework - 通用 Unity 游戏框架
// 事件系统 - 预定义框架事件

using UnityEngine;

/// <summary>
/// 框架预定义事件
/// 用于替代旧版 EventID 枚举
/// </summary>
namespace LevityEvents
{
    #region 核心生命周期事件

    /// <summary>
    /// 初始化完成事件（UI 初始化完成后触发）
    /// </summary>
    public struct InitDoneEvent : IEvent { }

    /// <summary>
    /// 游戏结束事件
    /// </summary>
    public struct GamePlayOverEvent : IEvent
    {
        public bool IsWin;
        public int Score;
        public float PlayTime;
    }

    #endregion

    #region 关卡系统事件

    /// <summary>
    /// 关卡开始加载
    /// </summary>
    public struct StageLoadStartEvent : IEvent
    {
        public int StageId;
        public string StageName;
    }

    /// <summary>
    /// 关卡加载完成
    /// </summary>
    public struct StageLoadCompleteEvent : IEvent
    {
        public int StageId;
        public string StageName;
    }

    /// <summary>
    /// 关卡卸载
    /// </summary>
    public struct StageUnloadEvent : IEvent
    {
        public int StageId;
    }

    #endregion

    #region 战斗/交互事件

    /// <summary>
    /// 命中目标事件
    /// </summary>
    public struct HitTargetEvent : IEvent
    {
        public GameObject Attacker;
        public GameObject Target;
        public float Damage;
        public Vector3 HitPoint;
    }

    /// <summary>
    /// 玩家受伤事件
    /// </summary>
    public struct PlayerDamagedEvent : IEvent
    {
        public float Damage;
        public float RemainingHealth;
        public GameObject DamageSource;
    }

    /// <summary>
    /// 玩家死亡事件
    /// </summary>
    public struct PlayerDeathEvent : IEvent
    {
        public GameObject Player;
        public GameObject Killer;
    }

    /// <summary>
    /// 敌人死亡事件
    /// </summary>
    public struct EnemyDeathEvent : IEvent
    {
        public GameObject Enemy;
        public GameObject Killer;
        public int RewardScore;
    }

    #endregion

    #region UI 事件

    /// <summary>
    /// 窗口打开事件
    /// </summary>
    public struct WindowOpenedEvent : IEvent
    {
        public string WindowName;
    }

    /// <summary>
    /// 窗口关闭事件
    /// </summary>
    public struct WindowClosedEvent : IEvent
    {
        public string WindowName;
    }

    #endregion

    #region 输入事件

    /// <summary>
    /// 暂停请求事件
    /// </summary>
    public struct PauseRequestEvent : IEvent { }

    /// <summary>
    /// 恢复请求事件
    /// </summary>
    public struct ResumeRequestEvent : IEvent { }

    #endregion

    #region 对话系统事件（Naninovel）

    /// <summary>
    /// 对话服务就绪
    /// </summary>
    public struct DialogueServiceReadyEvent : IEvent { }

    /// <summary>
    /// 对话开始
    /// </summary>
    public struct DialogueStartEvent : IEvent
    {
        public string ScriptName;
    }

    /// <summary>
    /// 对话结束
    /// </summary>
    public struct DialogueEndEvent : IEvent
    {
        public string ScriptName;
    }

    /// <summary>
    /// 玩家做出对话选择
    /// </summary>
    public struct DialogueChoiceEvent : IEvent
    {
        public int ChoiceIndex;
        public string ChoiceText;
    }

    #endregion
}
