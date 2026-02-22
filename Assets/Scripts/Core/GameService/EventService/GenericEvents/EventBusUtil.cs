// LevityFramework - 通用 Unity 游戏框架
// 事件系统 - EventBusUtil 事件总线工具类

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// EventBus 工具类
/// 自动发现所有事件类型并管理清理
/// </summary>
public static class EventBusUtil
{
    /// <summary>
    /// 所有已发现的事件类型
    /// </summary>
    public static IReadOnlyList<Type> EventTypes { get; private set; }

    /// <summary>
    /// 所有已初始化的 EventBus 类型
    /// </summary>
    public static IReadOnlyList<Type> EventBusTypes { get; private set; }

    private static bool isInitialized;

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器 PlayMode 状态
    /// </summary>
    public static PlayModeStateChange PlayModeState { get; private set; }

    [InitializeOnLoadMethod]
    private static void InitializeEditor()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        PlayModeState = state;
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            ClearAllBuses();
            isInitialized = false;
        }
    }
#endif

    /// <summary>
    /// 运行时初始化（场景加载前自动调用）
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (isInitialized) return;
        isInitialized = true;

        EventTypes = FindAllEventTypes();
        EventBusTypes = InitializeAllBuses();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[EventBus] 已发现 {EventTypes.Count} 个事件类型");
#endif
    }

    /// <summary>
    /// 查找所有实现 IEvent 的类型
    /// </summary>
    private static List<Type> FindAllEventTypes()
    {
        var eventTypes = new List<Type>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            // 只扫描项目程序集
            var assemblyName = assembly.GetName().Name;
            if (!IsProjectAssembly(assemblyName)) continue;

            try
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (type != typeof(IEvent) &&
                        typeof(IEvent).IsAssignableFrom(type) &&
                        type.IsValueType) // 只接受 struct
                    {
                        eventTypes.Add(type);
                    }
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // 忽略加载错误
            }
        }

        return eventTypes;
    }

    /// <summary>
    /// 判断是否为项目程序集
    /// </summary>
    private static bool IsProjectAssembly(string assemblyName)
    {
        return assemblyName switch
        {
            "Assembly-CSharp" => true,
            "Assembly-CSharp-firstpass" => true,
            _ => assemblyName.StartsWith("Assembly-CSharp") ||
                 assemblyName.Contains("Game") ||
                 assemblyName.Contains("Levity")
        };
    }

    /// <summary>
    /// 初始化所有 EventBus 类型
    /// </summary>
    private static List<Type> InitializeAllBuses()
    {
        var busTypes = new List<Type>();
        var genericBusType = typeof(EventBus<>);

        foreach (var eventType in EventTypes)
        {
            var busType = genericBusType.MakeGenericType(eventType);
            busTypes.Add(busType);
        }

        return busTypes;
    }

    /// <summary>
    /// 清除所有 EventBus 的绑定
    /// </summary>
    public static void ClearAllBuses()
    {
        if (EventBusTypes == null) return;

        foreach (var busType in EventBusTypes)
        {
            var clearMethod = busType.GetMethod("Clear", BindingFlags.Static | BindingFlags.NonPublic);
            clearMethod?.Invoke(null, null);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[EventBus] 已清除所有事件绑定");
#endif
    }

    /// <summary>
    /// 手动触发初始化（如果需要在 RuntimeInitializeOnLoadMethod 之前使用）
    /// </summary>
    public static void EnsureInitialized()
    {
        if (!isInitialized)
        {
            Initialize();
        }
    }

    /// <summary>
    /// 获取指定事件类型的绑定数量
    /// </summary>
    public static int GetBindingCount<T>() where T : struct, IEvent
    {
        return EventBus<T>.BindingCount;
    }

    /// <summary>
    /// 获取所有事件的总绑定数量
    /// </summary>
    public static int GetTotalBindingCount()
    {
        if (EventBusTypes == null) return 0;

        int total = 0;
        foreach (var busType in EventBusTypes)
        {
            var prop = busType.GetProperty("BindingCount", BindingFlags.Static | BindingFlags.Public);
            if (prop != null)
            {
                total += (int)prop.GetValue(null);
            }
        }
        return total;
    }
}
