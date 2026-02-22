// LevityFramework - 通用 Unity 游戏框架
// 核心指令模块 - PersistentSingleton 持久化单例

using UnityEngine;

/// <summary>
/// 持久化单例基类
/// 场景切换时保留，自动从父节点分离
/// </summary>
/// <remarks>
/// 与 MonoSingleton 的区别：
/// - 自动从父节点分离（AutoUnparentOnAwake）
/// - 提供 HasInstance 和 TryGetInstance 静态方法
/// </remarks>
public class PersistentSingleton<T> : MonoBehaviour where T : Component
{
    /// <summary>
    /// Awake 时是否自动从父节点分离
    /// </summary>
    public bool AutoUnparentOnAwake = true;

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
        InitializeSingleton();
    }

    protected virtual void InitializeSingleton()
    {
        if (!Application.isPlaying) return;

        if (AutoUnparentOnAwake)
        {
            transform.SetParent(null);
        }

        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject);
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
