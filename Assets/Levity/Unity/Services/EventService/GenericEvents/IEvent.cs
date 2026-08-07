// LevityFramework - 通用 Unity 游戏框架
// 泛型事件系统 - IEvent 事件标记接口

/// <summary>
/// 事件标记接口
/// 所有泛型事件必须实现此接口
/// 建议使用 struct 定义事件以避免 GC 分配
/// </summary>
/// <example>
/// public struct PlayerDamagedEvent : IEvent
/// {
///     public int Damage;
///     public int RemainingHealth;
///     public Vector3 HitPosition;
/// }
/// </example>
public interface IEvent { }
