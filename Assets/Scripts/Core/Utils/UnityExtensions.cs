// LevityFramework - 通用 Unity 游戏框架
// 工具类 - 扩展方法

using UnityEngine;

/// <summary>
/// Unity 扩展方法
/// </summary>
public static class UnityExtensions
{
    /// <summary>
    /// 检查当前动画的标签是否为指定标签
    /// </summary>
    public static bool AnimationAtTag(this Animator animator, string tag, int layer = 0)
    {
        return animator.GetCurrentAnimatorStateInfo(layer).IsTag(tag);
    }

    /// <summary>
    /// 安全获取组件（如果没有则添加）
    /// </summary>
    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        if (component == null)
        {
            component = go.AddComponent<T>();
        }
        return component;
    }

    /// <summary>
    /// 安全获取子对象组件
    /// </summary>
    public static T GetComponentInChildrenSafe<T>(this GameObject go, bool includeInactive = false) where T : Component
    {
        var component = go.GetComponentInChildren<T>(includeInactive);
        if (component == null)
        {
            Debug.LogWarning($"Component {typeof(T).Name} not found in children of {go.name}");
        }
        return component;
    }

    /// <summary>
    /// 设置 Layer（包含所有子对象）
    /// </summary>
    public static void SetLayerRecursively(this GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
        {
            child.gameObject.SetLayerRecursively(layer);
        }
    }

    /// <summary>
    /// 重置 Transform
    /// </summary>
    public static void ResetLocal(this Transform transform)
    {
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    /// <summary>
    /// 查找子对象（按路径）
    /// </summary>
    public static Transform FindChildByPath(this Transform parent, string path)
    {
        return parent.Find(path);
    }

    /// <summary>
    /// 销毁所有子对象
    /// </summary>
    public static void DestroyAllChildren(this Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Object.Destroy(parent.GetChild(i).gameObject);
        }
    }
}
