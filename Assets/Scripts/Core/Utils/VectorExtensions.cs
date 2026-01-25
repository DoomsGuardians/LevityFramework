// LevityFramework - 通用 Unity 游戏框架
// 工具模块 - VectorExtensions 向量扩展方法

using UnityEngine;

/// <summary>
/// Vector2/Vector3 扩展方法
/// 提供便捷的向量操作
/// </summary>
public static class VectorExtensions
{
    #region Vector2 With 方法

    /// <summary>替换 X 分量</summary>
    public static Vector2 WithX(this Vector2 v, float x) => new Vector2(x, v.y);

    /// <summary>替换 Y 分量</summary>
    public static Vector2 WithY(this Vector2 v, float y) => new Vector2(v.x, y);

    #endregion

    #region Vector3 With 方法

    /// <summary>替换 X 分量</summary>
    public static Vector3 WithX(this Vector3 v, float x) => new Vector3(x, v.y, v.z);

    /// <summary>替换 Y 分量</summary>
    public static Vector3 WithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);

    /// <summary>替换 Z 分量</summary>
    public static Vector3 WithZ(this Vector3 v, float z) => new Vector3(v.x, v.y, z);

    /// <summary>替换 XY 分量</summary>
    public static Vector3 WithXY(this Vector3 v, float x, float y) => new Vector3(x, y, v.z);

    /// <summary>替换 XZ 分量</summary>
    public static Vector3 WithXZ(this Vector3 v, float x, float z) => new Vector3(x, v.y, z);

    /// <summary>替换 YZ 分量</summary>
    public static Vector3 WithYZ(this Vector3 v, float y, float z) => new Vector3(v.x, y, z);

    #endregion

    #region Flat 方法（移除 Y 轴）

    /// <summary>
    /// 将 Vector3 压平到 XZ 平面（Y = 0）
    /// 常用于忽略高度的水平距离计算
    /// </summary>
    public static Vector3 Flat(this Vector3 v) => new Vector3(v.x, 0f, v.z);

    /// <summary>
    /// 将 Vector3 压平到 XZ 平面并指定 Y 值
    /// </summary>
    public static Vector3 FlatWithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);

    #endregion

    #region 类型转换

    /// <summary>
    /// Vector2 转 Vector3 (z = 0)
    /// </summary>
    public static Vector3 ToVector3(this Vector2 v) => new Vector3(v.x, v.y, 0f);

    /// <summary>
    /// Vector2 转 Vector3 (指定 z)
    /// </summary>
    public static Vector3 ToVector3(this Vector2 v, float z) => new Vector3(v.x, v.y, z);

    /// <summary>
    /// Vector2 转 Vector3 XZ 平面 (y = 0)
    /// </summary>
    public static Vector3 ToVector3XZ(this Vector2 v) => new Vector3(v.x, 0f, v.y);

    /// <summary>
    /// Vector3 转 Vector2 (忽略 z)
    /// </summary>
    public static Vector2 ToVector2(this Vector3 v) => new Vector2(v.x, v.y);

    /// <summary>
    /// Vector3 转 Vector2 XZ 平面 (忽略 y)
    /// </summary>
    public static Vector2 ToVector2XZ(this Vector3 v) => new Vector2(v.x, v.z);

    #endregion

    #region 距离计算

    /// <summary>
    /// 计算两点在 XZ 平面上的距离（忽略 Y 轴）
    /// </summary>
    public static float DistanceXZ(this Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    /// <summary>
    /// 计算两点在 XZ 平面上的距离平方（忽略 Y 轴）
    /// 比 DistanceXZ 更快，适合距离比较
    /// </summary>
    public static float SqrDistanceXZ(this Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }

    #endregion

    #region 方向计算

    /// <summary>
    /// 计算从当前点到目标点的方向向量（已归一化）
    /// </summary>
    public static Vector3 DirectionTo(this Vector3 from, Vector3 to)
    {
        return (to - from).normalized;
    }

    /// <summary>
    /// 计算从当前点到目标点在 XZ 平面上的方向向量（已归一化，Y = 0）
    /// </summary>
    public static Vector3 DirectionToXZ(this Vector3 from, Vector3 to)
    {
        return (to.Flat() - from.Flat()).normalized;
    }

    /// <summary>
    /// 计算从当前点到目标点的向量（未归一化）
    /// </summary>
    public static Vector3 VectorTo(this Vector3 from, Vector3 to)
    {
        return to - from;
    }

    #endregion

    #region 随机偏移

    /// <summary>
    /// 在球形范围内随机偏移
    /// </summary>
    public static Vector3 RandomOffset(this Vector3 v, float radius)
    {
        return v + Random.insideUnitSphere * radius;
    }

    /// <summary>
    /// 在 XZ 平面圆形范围内随机偏移
    /// </summary>
    public static Vector3 RandomOffsetXZ(this Vector3 v, float radius)
    {
        Vector2 offset = Random.insideUnitCircle * radius;
        return new Vector3(v.x + offset.x, v.y, v.z + offset.y);
    }

    /// <summary>
    /// 在球壳上随机偏移（固定距离）
    /// </summary>
    public static Vector3 RandomOffsetOnSphere(this Vector3 v, float radius)
    {
        return v + Random.onUnitSphere * radius;
    }

    #endregion

    #region 数学运算

    /// <summary>
    /// 分量相乘
    /// </summary>
    public static Vector3 Multiply(this Vector3 a, Vector3 b)
    {
        return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
    }

    /// <summary>
    /// 分量相除
    /// </summary>
    public static Vector3 Divide(this Vector3 a, Vector3 b)
    {
        return new Vector3(
            b.x != 0 ? a.x / b.x : 0,
            b.y != 0 ? a.y / b.y : 0,
            b.z != 0 ? a.z / b.z : 0
        );
    }

    /// <summary>
    /// 取绝对值
    /// </summary>
    public static Vector3 Abs(this Vector3 v)
    {
        return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }

    /// <summary>
    /// Clamp 到指定范围
    /// </summary>
    public static Vector3 Clamp(this Vector3 v, Vector3 min, Vector3 max)
    {
        return new Vector3(
            Mathf.Clamp(v.x, min.x, max.x),
            Mathf.Clamp(v.y, min.y, max.y),
            Mathf.Clamp(v.z, min.z, max.z)
        );
    }

    /// <summary>
    /// 限制向量长度
    /// </summary>
    public static Vector3 ClampMagnitude(this Vector3 v, float maxLength)
    {
        return Vector3.ClampMagnitude(v, maxLength);
    }

    #endregion

    #region 判断方法

    /// <summary>
    /// 判断向量是否接近零向量
    /// </summary>
    public static bool IsNearlyZero(this Vector3 v, float threshold = 0.0001f)
    {
        return v.sqrMagnitude < threshold * threshold;
    }

    /// <summary>
    /// 判断两个向量是否接近相等
    /// </summary>
    public static bool IsNearlyEqual(this Vector3 a, Vector3 b, float threshold = 0.0001f)
    {
        return (a - b).sqrMagnitude < threshold * threshold;
    }

    #endregion
}
