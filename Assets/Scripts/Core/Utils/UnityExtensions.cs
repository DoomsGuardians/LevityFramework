// LevityFramework - 通用 Unity 游戏框架
// 工具类 - 扩展方法

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Unity 扩展方法
/// </summary>
public static class UnityExtensions
{
    #region Unity Object Null 处理

    /// <summary>
    /// 返回对象本身（如果存在），否则返回 null
    /// 用于处理 Unity 特殊的 null 比较问题
    /// </summary>
    /// <remarks>
    /// Unity 重写了 == null 运算符，导致已销毁的对象可能返回 false
    /// 使用此方法可以确保得到真正的 null 引用
    /// </remarks>
    public static T OrNull<T>(this T obj) where T : Object
    {
        return obj ? obj : null;
    }

    /// <summary>
    /// 检查对象是否真的为 null 或已销毁
    /// </summary>
    public static bool IsNullOrDestroyed(this Object obj)
    {
        return obj == null;
    }

    /// <summary>
    /// 检查对象是否存在且未销毁
    /// </summary>
    public static bool IsValid(this Object obj)
    {
        return obj != null;
    }

    #endregion

    #region GameObject 显示/隐藏
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

    #endregion

    #region GameObject 激活/隐藏链式调用

    /// <summary>
    /// 激活 GameObject 并返回组件（链式调用）
    /// </summary>
    public static T SetActive<T>(this T component) where T : Component
    {
        component.gameObject.SetActive(true);
        return component;
    }

    /// <summary>
    /// 隐藏 GameObject 并返回组件（链式调用）
    /// </summary>
    public static T SetInactive<T>(this T component) where T : Component
    {
        component.gameObject.SetActive(false);
        return component;
    }

    /// <summary>
    /// 设置 GameObject 激活状态并返回组件（链式调用）
    /// </summary>
    public static T SetActive<T>(this T component, bool active) where T : Component
    {
        component.gameObject.SetActive(active);
        return component;
    }

    #endregion

    #region Hierarchy 操作

    /// <summary>
    /// 在 Hierarchy 中隐藏 GameObject
    /// </summary>
    public static void HideInHierarchy(this GameObject go)
    {
        go.hideFlags = HideFlags.HideInHierarchy;
    }

    /// <summary>
    /// 在 Hierarchy 中显示 GameObject
    /// </summary>
    public static void ShowInHierarchy(this GameObject go)
    {
        go.hideFlags = HideFlags.None;
    }

    /// <summary>
    /// 获取 GameObject 的完整层级路径
    /// </summary>
    public static string GetPath(this GameObject go)
    {
        return "/" + string.Join("/",
            go.GetComponentsInParent<Transform>()
              .Select(t => t.name)
              .Reverse()
              .ToArray());
    }

    /// <summary>
    /// 获取 GameObject 的完整路径（包含自身）
    /// </summary>
    public static string GetFullPath(this GameObject go)
    {
        return go.GetPath() + "/" + go.name;
    }

    /// <summary>
    /// 获取 Transform 的完整层级路径
    /// </summary>
    public static string GetPath(this Transform t)
    {
        return t.gameObject.GetPath();
    }

    #endregion

    #region 子对象启用/禁用

    /// <summary>
    /// 启用所有子对象
    /// </summary>
    public static void EnableChildren(this Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            parent.GetChild(i).gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 禁用所有子对象
    /// </summary>
    public static void DisableChildren(this Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            parent.GetChild(i).gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 启用所有子对象（GameObject 版本）
    /// </summary>
    public static void EnableChildren(this GameObject go)
    {
        go.transform.EnableChildren();
    }

    /// <summary>
    /// 禁用所有子对象（GameObject 版本）
    /// </summary>
    public static void DisableChildren(this GameObject go)
    {
        go.transform.DisableChildren();
    }

    /// <summary>
    /// 销毁所有子对象（GameObject 版本）
    /// </summary>
    public static void DestroyChildren(this GameObject go)
    {
        TransformExtensions.DestroyAllChildren(go.transform);
    }

    #endregion

    #region 距离与范围检测

    /// <summary>
    /// 检查 Transform 是否在目标的指定距离和角度范围内
    /// </summary>
    /// <param name="source">源 Transform</param>
    /// <param name="target">目标 Transform</param>
    /// <param name="maxDistance">最大距离</param>
    /// <param name="maxAngle">最大角度（度数，360 = 不限制角度）</param>
    public static bool InRangeOf(this Transform source, Transform target, float maxDistance, float maxAngle = 360f)
    {
        Vector3 directionToTarget = (target.position - source.position).WithY(0);
        return directionToTarget.magnitude <= maxDistance
               && Vector3.Angle(source.forward, directionToTarget) <= maxAngle / 2;
    }

    /// <summary>
    /// 计算到目标的距离
    /// </summary>
    public static float DistanceTo(this Transform source, Transform target)
    {
        return Vector3.Distance(source.position, target.position);
    }

    /// <summary>
    /// 计算到目标的距离（XZ 平面）
    /// </summary>
    public static float DistanceToXZ(this Transform source, Transform target)
    {
        return source.position.DistanceXZ(target.position);
    }

    #endregion

    #region 获取子对象迭代器

    /// <summary>
    /// 获取所有直接子对象（LINQ 友好）
    /// </summary>
    public static IEnumerable<Transform> Children(this Transform parent)
    {
        foreach (Transform child in parent)
        {
            yield return child;
        }
    }

    /// <summary>
    /// 获取所有子孙对象（递归）
    /// </summary>
    public static IEnumerable<Transform> Descendants(this Transform parent)
    {
        foreach (Transform child in parent)
        {
            yield return child;
            foreach (var descendant in child.Descendants())
            {
                yield return descendant;
            }
        }
    }

    #endregion
}
