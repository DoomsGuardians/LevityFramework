// LevityFramework - 通用 Unity 游戏框架
// 资源服务模块 - IPoolable 对象池接口

/// <summary>
/// 对象池项接口
/// 实现此接口的对象可以被对象池正确管理生命周期
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// 从对象池取出时调用（重置状态）
    /// </summary>
    void OnSpawn();

    /// <summary>
    /// 返回对象池时调用（清理状态）
    /// </summary>
    void OnDespawn();
}

/// <summary>
/// 带初始化参数的对象池项接口
/// </summary>
/// <typeparam name="T">初始化参数类型</typeparam>
public interface IPoolable<T> : IPoolable
{
    /// <summary>
    /// 从对象池取出时调用（带参数初始化）
    /// </summary>
    void OnSpawn(T initData);
}
