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

    /// <summary>
    /// 判断当前点是否在目标点的指定距离内
    /// </summary>
    public static bool InRangeOf(this Vector3 current, Vector3 target, float range)
    {
        return (current - target).sqrMagnitude <= range * range;
    }

    /// <summary>
    /// 判断 Vector2 是否在目标点的指定距离内
    /// </summary>
    public static bool InRangeOf(this Vector2 current, Vector2 target, float range)
    {
        return (current - target).sqrMagnitude <= range * range;
    }

    #endregion

    #region With 可选参数版本

    /// <summary>
    /// 设置任意 x/y/z 分量（可选参数版本）
    /// </summary>
    public static Vector3 With(this Vector3 v, float? x = null, float? y = null, float? z = null)
    {
        return new Vector3(x ?? v.x, y ?? v.y, z ?? v.z);
    }

    /// <summary>
    /// 设置任意 x/y 分量（可选参数版本）
    /// </summary>
    public static Vector2 With(this Vector2 v, float? x = null, float? y = null)
    {
        return new Vector2(x ?? v.x, y ?? v.y);
    }

    #endregion

    #region 增量操作

    /// <summary>
    /// 增加分量值
    /// </summary>
    public static Vector3 Add(this Vector3 v, float x = 0, float y = 0, float z = 0)
    {
        return new Vector3(v.x + x, v.y + y, v.z + z);
    }

    /// <summary>
    /// 增加分量值（Vector2）
    /// </summary>
    public static Vector2 Add(this Vector2 v, float x = 0, float y = 0)
    {
        return new Vector2(v.x + x, v.y + y);
    }

    #endregion

    #region 环形随机

    /// <summary>
    /// 在环形区域（圆环）内随机一个点
    /// 适用于生成怪物、道具等需要避开中心的场景
    /// </summary>
    /// <param name="origin">中心点</param>
    /// <param name="minRadius">最小半径</param>
    /// <param name="maxRadius">最大半径</param>
    /// <returns>XZ 平面上的随机点</returns>
    public static Vector3 RandomPointInAnnulus(this Vector3 origin, float minRadius, float maxRadius)
    {
        float angle = Random.value * Mathf.PI * 2f;
        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        // 使用平方根确保均匀分布
        float minRadiusSqr = minRadius * minRadius;
        float maxRadiusSqr = maxRadius * maxRadius;
        float distance = Mathf.Sqrt(Random.value * (maxRadiusSqr - minRadiusSqr) + minRadiusSqr);

        Vector3 position = new Vector3(direction.x, 0, direction.y) * distance;
        return origin + position;
    }

    /// <summary>
    /// 在圆形区域内随机一个点（XZ 平面）
    /// </summary>
    public static Vector3 RandomPointInCircle(this Vector3 origin, float radius)
    {
        Vector2 randomPoint = Random.insideUnitCircle * radius;
        return new Vector3(origin.x + randomPoint.x, origin.y, origin.z + randomPoint.y);
    }

    #endregion

    #region 量化与对齐

    /// <summary>
    /// 将向量量化（对齐到网格）
    /// 适用于 NavMesh 更新优化、格子对齐等场景
    /// </summary>
    /// <param name="position">原位置</param>
    /// <param name="gridSize">网格大小（各轴统一）</param>
    public static Vector3 Quantize(this Vector3 position, float gridSize)
    {
        return new Vector3(
            Mathf.Floor(position.x / gridSize) * gridSize,
            Mathf.Floor(position.y / gridSize) * gridSize,
            Mathf.Floor(position.z / gridSize) * gridSize
        );
    }

    /// <summary>
    /// 将向量量化（各轴不同网格大小）
    /// </summary>
    public static Vector3 Quantize(this Vector3 position, Vector3 gridSize)
    {
        return new Vector3(
            gridSize.x != 0 ? Mathf.Floor(position.x / gridSize.x) * gridSize.x : position.x,
            gridSize.y != 0 ? Mathf.Floor(position.y / gridSize.y) * gridSize.y : position.y,
            gridSize.z != 0 ? Mathf.Floor(position.z / gridSize.z) * gridSize.z : position.z
        );
    }

    /// <summary>
    /// 将向量四舍五入到最近的网格点
    /// </summary>
    public static Vector3 SnapToGrid(this Vector3 position, float gridSize)
    {
        return new Vector3(
            Mathf.Round(position.x / gridSize) * gridSize,
            Mathf.Round(position.y / gridSize) * gridSize,
            Mathf.Round(position.z / gridSize) * gridSize
        );
    }

    #endregion

    #region 角度计算

    /// <summary>
    /// 计算两个向量在 XZ 平面上的夹角（度数）
    /// </summary>
    public static float AngleXZ(this Vector3 from, Vector3 to)
    {
        Vector2 from2D = new Vector2(from.x, from.z);
        Vector2 to2D = new Vector2(to.x, to.z);
        return Vector2.SignedAngle(from2D, to2D);
    }

    /// <summary>
    /// 获取向量在 XZ 平面上的朝向角度（相对于正前方 Z 轴）
    /// </summary>
    public static float GetYaw(this Vector3 direction)
    {
        return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
    }

    #endregion
}
