// LevityFramework - 通用 Unity 游戏框架
// 工具模块 - TransformExtensions Transform 扩展方法

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Transform 扩展方法
/// 提供便捷的位置、旋转、子对象操作
/// </summary>
public static class TransformExtensions
{
    #region 世界坐标位置操作

    /// <summary>设置世界坐标 X</summary>
    public static void SetPositionX(this Transform t, float x)
    {
        t.position = t.position.WithX(x);
    }

    /// <summary>设置世界坐标 Y</summary>
    public static void SetPositionY(this Transform t, float y)
    {
        t.position = t.position.WithY(y);
    }

    /// <summary>设置世界坐标 Z</summary>
    public static void SetPositionZ(this Transform t, float z)
    {
        t.position = t.position.WithZ(z);
    }

    /// <summary>增加世界坐标位置</summary>
    public static void AddPosition(this Transform t, Vector3 delta)
    {
        t.position += delta;
    }

    /// <summary>增加世界坐标 X</summary>
    public static void AddPositionX(this Transform t, float x)
    {
        t.position = t.position.WithX(t.position.x + x);
    }

    /// <summary>增加世界坐标 Y</summary>
    public static void AddPositionY(this Transform t, float y)
    {
        t.position = t.position.WithY(t.position.y + y);
    }

    /// <summary>增加世界坐标 Z</summary>
    public static void AddPositionZ(this Transform t, float z)
    {
        t.position = t.position.WithZ(t.position.z + z);
    }

    #endregion

    #region 本地坐标位置操作

    /// <summary>设置本地坐标 X</summary>
    public static void SetLocalPositionX(this Transform t, float x)
    {
        t.localPosition = t.localPosition.WithX(x);
    }

    /// <summary>设置本地坐标 Y</summary>
    public static void SetLocalPositionY(this Transform t, float y)
    {
        t.localPosition = t.localPosition.WithY(y);
    }

    /// <summary>设置本地坐标 Z</summary>
    public static void SetLocalPositionZ(this Transform t, float z)
    {
        t.localPosition = t.localPosition.WithZ(z);
    }

    /// <summary>增加本地坐标位置</summary>
    public static void AddLocalPosition(this Transform t, Vector3 delta)
    {
        t.localPosition += delta;
    }

    #endregion

    #region 缩放操作

    /// <summary>设置统一缩放</summary>
    public static void SetLocalScale(this Transform t, float scale)
    {
        t.localScale = new Vector3(scale, scale, scale);
    }

    /// <summary>设置本地缩放 X</summary>
    public static void SetLocalScaleX(this Transform t, float x)
    {
        t.localScale = t.localScale.WithX(x);
    }

    /// <summary>设置本地缩放 Y</summary>
    public static void SetLocalScaleY(this Transform t, float y)
    {
        t.localScale = t.localScale.WithY(y);
    }

    /// <summary>设置本地缩放 Z</summary>
    public static void SetLocalScaleZ(this Transform t, float z)
    {
        t.localScale = t.localScale.WithZ(z);
    }

    #endregion

    #region 旋转操作

    /// <summary>
    /// 看向目标（仅 Y 轴旋转，适合角色朝向）
    /// </summary>
    public static void LookAtY(this Transform t, Vector3 target)
    {
        Vector3 direction = (target - t.position).Flat();
        if (direction.sqrMagnitude > 0.001f)
        {
            t.rotation = Quaternion.LookRotation(direction);
        }
    }

    /// <summary>
    /// 看向目标 Transform（仅 Y 轴旋转）
    /// </summary>
    public static void LookAtY(this Transform t, Transform target)
    {
        t.LookAtY(target.position);
    }

    /// <summary>设置本地欧拉角 X</summary>
    public static void SetLocalEulerAngleX(this Transform t, float x)
    {
        Vector3 euler = t.localEulerAngles;
        euler.x = x;
        t.localEulerAngles = euler;
    }

    /// <summary>设置本地欧拉角 Y</summary>
    public static void SetLocalEulerAngleY(this Transform t, float y)
    {
        Vector3 euler = t.localEulerAngles;
        euler.y = y;
        t.localEulerAngles = euler;
    }

    /// <summary>设置本地欧拉角 Z</summary>
    public static void SetLocalEulerAngleZ(this Transform t, float z)
    {
        Vector3 euler = t.localEulerAngles;
        euler.z = z;
        t.localEulerAngles = euler;
    }

    /// <summary>增加本地欧拉角 Y（旋转）</summary>
    public static void AddLocalEulerAngleY(this Transform t, float y)
    {
        Vector3 euler = t.localEulerAngles;
        euler.y += y;
        t.localEulerAngles = euler;
    }

    #endregion

    #region 子对象操作

    /// <summary>
    /// 获取所有直接子对象
    /// </summary>
    public static List<Transform> GetChildren(this Transform t)
    {
        List<Transform> children = new List<Transform>(t.childCount);
        for (int i = 0; i < t.childCount; i++)
        {
            children.Add(t.GetChild(i));
        }
        return children;
    }

    /// <summary>
    /// 遍历所有直接子对象执行操作
    /// </summary>
    public static void ForEachChild(this Transform t, Action<Transform> action)
    {
        for (int i = 0; i < t.childCount; i++)
        {
            action(t.GetChild(i));
        }
    }

    /// <summary>
    /// 遍历所有直接子对象执行操作（带索引）
    /// </summary>
    public static void ForEachChild(this Transform t, Action<Transform, int> action)
    {
        for (int i = 0; i < t.childCount; i++)
        {
            action(t.GetChild(i), i);
        }
    }

    /// <summary>
    /// 递归遍历所有子对象
    /// </summary>
    public static void ForEachChildRecursive(this Transform t, Action<Transform> action)
    {
        for (int i = 0; i < t.childCount; i++)
        {
            Transform child = t.GetChild(i);
            action(child);
            child.ForEachChildRecursive(action);
        }
    }

    /// <summary>
    /// 查找子对象，不存在则创建
    /// </summary>
    public static Transform FindOrCreate(this Transform t, string name)
    {
        Transform child = t.Find(name);
        if (child == null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(t, false);
            child = go.transform;
        }
        return child;
    }

    /// <summary>
    /// 安全查找子对象（支持路径，返回 null 而非异常）
    /// </summary>
    public static Transform FindSafe(this Transform t, string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        return t.Find(path);
    }

    /// <summary>
    /// 销毁所有子对象
    /// </summary>
    public static void DestroyAllChildren(this Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// 立即销毁所有子对象（编辑器模式下使用）
    /// </summary>
    public static void DestroyAllChildrenImmediate(this Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(t.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// 设置所有子对象的激活状态
    /// </summary>
    public static void SetChildrenActive(this Transform t, bool active)
    {
        for (int i = 0; i < t.childCount; i++)
        {
            t.GetChild(i).gameObject.SetActive(active);
        }
    }

    #endregion

    #region 层级和路径操作

    /// <summary>
    /// 获取完整层级路径
    /// </summary>
    public static string GetPath(this Transform t)
    {
        string path = t.name;
        Transform current = t.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }

    /// <summary>
    /// 获取相对于某个父节点的路径
    /// </summary>
    public static string GetRelativePath(this Transform t, Transform root)
    {
        if (t == root) return "";

        string path = t.name;
        Transform current = t.parent;

        while (current != null && current != root)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    /// <summary>
    /// 获取层级深度
    /// </summary>
    public static int GetDepth(this Transform t)
    {
        int depth = 0;
        Transform current = t.parent;
        while (current != null)
        {
            depth++;
            current = current.parent;
        }
        return depth;
    }

    /// <summary>
    /// 查找指定层级的根父对象
    /// </summary>
    public static Transform GetRoot(this Transform t)
    {
        while (t.parent != null)
        {
            t = t.parent;
        }
        return t;
    }

    /// <summary>
    /// 判断是否是某个 Transform 的子对象
    /// </summary>
    public static bool IsChildOf(this Transform t, Transform parent)
    {
        return t.IsChildOf(parent);
    }

    #endregion

    #region 重置操作

    // 注意：ResetLocal 方法已在 UnityExtensions.cs 中定义，此处不重复定义

    /// <summary>
    /// 重置本地位置
    /// </summary>
    public static void ResetLocalPosition(this Transform t)
    {
        t.localPosition = Vector3.zero;
    }

    /// <summary>
    /// 重置本地旋转
    /// </summary>
    public static void ResetLocalRotation(this Transform t)
    {
        t.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// 重置本地缩放
    /// </summary>
    public static void ResetLocalScale(this Transform t)
    {
        t.localScale = Vector3.one;
    }

    #endregion

    #region 设置父对象

    /// <summary>
    /// 设置父对象并重置本地变换
    /// </summary>
    public static void SetParentAndReset(this Transform t, Transform parent)
    {
        t.SetParent(parent, false);
        // 使用 UnityExtensions.ResetLocal
        UnityExtensions.ResetLocal(t);
    }

    /// <summary>
    /// 设置父对象并保持世界位置
    /// </summary>
    public static void SetParentKeepWorld(this Transform t, Transform parent)
    {
        t.SetParent(parent, true);
    }

    #endregion
}
