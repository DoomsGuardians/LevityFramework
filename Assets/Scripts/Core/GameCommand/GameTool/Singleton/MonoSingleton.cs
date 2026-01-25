// LevityFramework - 通用 Unity 游戏框架
// 核心指令模块 - MonoSingleton 单例基类

using UnityEngine;

/// <summary>
/// 泛型单例基类，自动管理 DontDestroyOnLoad
/// 注意：在物体销毁时访问此对象最好加一个 Null 的判断，防止程序退出时此物体被销毁
/// </summary>
public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = GameObject.FindAnyObjectByType<T>();
                if (instance == null)
                {
                    var go = new GameObject(typeof(T).Name);
                    instance = go.AddComponent<T>();
                }
            }
            return instance;
        }
    }

    protected virtual void Awake()
    {
        // 默认放到 DontDestroyOnLoad 场景中
        if (instance == null)
        {
            instance = (T)this;
            DontDestroyOnLoad(instance.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
