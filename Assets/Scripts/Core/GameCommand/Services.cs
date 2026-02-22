// LevityFramework - 通用 Unity 游戏框架
// 服务定位器 - Services 静态访问类

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 服务定位器
/// 提供全局服务的快捷访问方式
/// </summary>
/// <example>
/// // 获取服务
/// var audio = Services.Get&lt;AudioService&gt;();
/// var ui = Services.UI;
///
/// // 注册自定义服务
/// Services.Register&lt;IMyService&gt;(myServiceInstance);
/// </example>
public static class Services
{
    private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
    private static bool isInitialized;

    #region 常用服务快捷访问

    /// <summary>
    /// 音频服务
    /// </summary>
    public static AudioService Audio => Get<AudioService>();

    /// <summary>
    /// UI 服务
    /// </summary>
    public static UIService UI => Get<UIService>();

    /// <summary>
    /// 输入服务
    /// </summary>
    public static InputService Input => Get<InputService>();

    /// <summary>
    /// 资源服务
    /// </summary>
    public static ResService Res => Get<ResService>();

    /// <summary>
    /// 定时器服务
    /// </summary>
    public static TimerService Timer => Get<TimerService>();

    /// <summary>
    /// 数据存档服务
    /// </summary>
    public static DataService Data => Get<DataService>();

    #endregion

    #region 系统快捷访问

    /// <summary>
    /// 角色系统
    /// </summary>
    public static RoleSystem Role => Get<RoleSystem>();

    /// <summary>
    /// 关卡系统
    /// </summary>
    public static StageSystem Stage => Get<StageSystem>();

    #endregion

    #region 通用方法

    /// <summary>
    /// 获取服务实例
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例，如果未注册返回 null</returns>
    public static T Get<T>() where T : class
    {
        EnsureInitialized();

        var type = typeof(T);
        if (services.TryGetValue(type, out var service))
        {
            return service as T;
        }

        // 尝试从 GameRoot 获取
        if (GameRoot.Instance != null)
        {
            var fromRoot = GetFromGameRoot<T>();
            if (fromRoot != null)
            {
                services[type] = fromRoot;
                return fromRoot;
            }
        }

        return null;
    }

    /// <summary>
    /// 尝试获取服务实例
    /// </summary>
    public static bool TryGet<T>(out T service) where T : class
    {
        service = Get<T>();
        return service != null;
    }

    /// <summary>
    /// 注册服务
    /// </summary>
    /// <typeparam name="T">服务接口类型</typeparam>
    /// <param name="service">服务实例</param>
    public static void Register<T>(T service) where T : class
    {
        if (service == null)
        {
            Debug.LogWarning($"[Services] 尝试注册空服务: {typeof(T).Name}");
            return;
        }

        var type = typeof(T);
        services[type] = service;
    }

    /// <summary>
    /// 注销服务
    /// </summary>
    public static void Unregister<T>() where T : class
    {
        services.Remove(typeof(T));
    }

    /// <summary>
    /// 检查服务是否已注册
    /// </summary>
    public static bool Has<T>() where T : class
    {
        return Get<T>() != null;
    }

    /// <summary>
    /// 清除所有注册的服务
    /// </summary>
    public static void Clear()
    {
        services.Clear();
        isInitialized = false;
    }

    #endregion

    #region 内部方法

    private static void EnsureInitialized()
    {
        if (isInitialized) return;
        isInitialized = true;

        // 可以在这里进行初始化逻辑
    }

    private static T GetFromGameRoot<T>() where T : class
    {
        var root = GameRoot.Instance;
        if (root == null) return null;

        // 通过反射或类型匹配获取服务
        var type = typeof(T);

        if (type == typeof(AudioService)) return root.audioService as T;
        if (type == typeof(UIService)) return root.uIService as T;
        if (type == typeof(InputService)) return root.inputService as T;
        if (type == typeof(ResService)) return root.resService as T;
        if (type == typeof(TimerService)) return root.timerService as T;
        if (type == typeof(DataService)) return root.dataService as T;
        if (type == typeof(RoleSystem)) return root.roleSystem as T;
        if (type == typeof(StageSystem)) return root.stageSystem as T;

        return null;
    }

    #endregion
}

/// <summary>
/// 服务注入特性（可选，用于未来依赖注入扩展）
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class InjectAttribute : Attribute { }
