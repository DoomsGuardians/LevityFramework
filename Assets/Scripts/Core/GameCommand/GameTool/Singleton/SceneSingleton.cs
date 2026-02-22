// LevityFramework - 通用 Unity 游戏框架
// 核心指令模块 - SceneSingleton 场景单例

using UnityEngine;

/// <summary>
/// 场景单例基类
/// 场景切换时自动销毁，不使用 DontDestroyOnLoad
/// </summary>
/// <remarks>
/// 适用于：
/// - 场景级别的管理器
/// - 只在特定场景存在的单例
/// - 不需要跨场景保持的对象
/// </remarks>
public class SceneSingleton<T> : MonoBehaviour where T : Component
{
    protected static T instance;

    /// <summary>
    /// 是否存在实例
    /// </summary>
    public static bool HasInstance => instance != null;

    /// <summary>
    /// 尝试获取实例（不会创建新实例）
    /// </summary>
    public static T TryGetInstance() => HasInstance ? instance : null;

    /// <summary>
    /// 获取单例实例
    /// </summary>
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<T>();
                if (instance == null)
                {
                    var go = new GameObject($"[{typeof(T).Name}]");
                    instance = go.AddComponent<T>();
                }
            }
            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
