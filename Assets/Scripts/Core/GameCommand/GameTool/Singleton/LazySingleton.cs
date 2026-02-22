// LevityFramework - 通用 Unity 游戏框架
// 核心指令模块 - LazySingleton 惰性单例

using System;

/// <summary>
/// 惰性单例基类（非 MonoBehaviour）
/// 首次访问时创建实例，线程安全
/// </summary>
/// <remarks>
/// 适用于：
/// - 纯 C# 类的单例
/// - 不需要 Unity 生命周期的管理类
/// - 工具类、配置类等
/// </remarks>
public abstract class LazySingleton<T> where T : class, new()
{
    private static readonly Lazy<T> lazyInstance = new Lazy<T>(() => new T());

    /// <summary>
    /// 获取单例实例
    /// </summary>
    public static T Instance => lazyInstance.Value;

    /// <summary>
    /// 是否已创建实例
    /// </summary>
    public static bool IsCreated => lazyInstance.IsValueCreated;

    /// <summary>
    /// 受保护的构造函数，防止外部实例化
    /// </summary>
    protected LazySingleton() { }
}
