// LevityFramework - 通用 Unity 游戏框架
// 核心指令模块 - ToolFunction 工具函数库

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 静态工具函数库
/// </summary>
public static class ToolFunction
{
    /// <summary>
    /// 获取 Manager（需要 GameRoot 和 ManagerService 支持）
    /// </summary>
    public static T GetManager<T>() where T : ManagerBase
    {
        return GameRoot.Instance.managerService.GetManager<T>() as T;
    }

    /// <summary>
    /// 使用 Vector3 设置 Image 的颜色
    /// </summary>
    /// <param name="img">需要设置颜色的 Image 组件</param>
    /// <param name="colorVector">包含 RGB 值的 Vector3 (0-1)</param>
    /// <param name="alpha">透明度值（0-1）</param>
    public static void SetImageColorFromVector3(Image img, Vector3 colorVector, float alpha)
    {
        if (img == null)
        {
            Debug.LogWarning("Image is null!");
            return;
        }
        alpha = Mathf.Clamp01(alpha);
        Color newColor = new Color(colorVector.x, colorVector.y, colorVector.z, alpha);
        img.color = newColor;
    }

    /// <summary>
    /// 使用 Vector3 设置 RawImage 的颜色
    /// </summary>
    public static void SetRawImageColorFromVector3(RawImage img, Vector3 colorVector, float alpha)
    {
        if (img == null)
        {
            Debug.LogWarning("RawImage is null!");
            return;
        }
        alpha = Mathf.Clamp01(alpha);
        Color newColor = new Color(colorVector.x, colorVector.y, colorVector.z, alpha);
        img.color = newColor;
    }

    /// <summary>
    /// 计算角色正前方和目标角度的夹角，范围（-180，180）
    /// </summary>
    public static float GetDeltaAngle(Transform player, float targetAngle)
    {
        Vector3 targetDir = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;
        return GetDeltaAngle(player, targetDir);
    }

    /// <summary>
    /// 计算角色正前方和目标方向的夹角，范围（-180，180）
    /// </summary>
    public static float GetDeltaAngle(Transform player, Vector3 toDir)
    {
        return GetDeltaAngle(player.forward, toDir);
    }

    /// <summary>
    /// 计算两个向量的夹角，忽略 Y 向量，范围（-180，180）
    /// </summary>
    public static float GetDeltaAngle(Vector3 startDir, Vector3 toDir)
    {
        float playerAngle = Mathf.Atan2(startDir.x, startDir.z) * Mathf.Rad2Deg;
        float targetAngle = Mathf.Atan2(toDir.x, toDir.z) * Mathf.Rad2Deg;
        float angleDelta = Mathf.DeltaAngle(targetAngle, playerAngle);
        return angleDelta; // 正负代表右左
    }

    /// <summary>
    /// 计算跳跃的初速度
    /// </summary>
    public static float GetJumpInitVelocity(float jumpMaxHeight, float gravity)
    {
        return Mathf.Sqrt(-2 * gravity * jumpMaxHeight);
    }

    /// <summary>
    /// 递归查找符合层级的根父对象
    /// </summary>
    public static void FindRootFather(Transform child, LayerMask layerMask, ref Transform parent)
    {
        if (child == null)
        {
            parent = null;
            return;
        }

        if (child.parent == null || ((layerMask & 1 << child.parent.gameObject.layer)) == 0)
        {
            parent = child;
        }
        else
        {
            FindRootFather(child.parent, layerMask, ref parent);
        }
    }

    /// <summary>
    /// 从列表中查找最近的目标
    /// </summary>
    public static Transform FindNearest(Transform owner, List<Transform> targetList)
    {
        Transform target = null;
        float minValue = float.MaxValue;
        for (int i = 0; i < targetList.Count; i++)
        {
            float distance = Vector3.Distance(owner.position, targetList[i].position);
            if (distance < minValue)
            {
                minValue = distance;
                target = targetList[i];
            }
        }
        return target;
    }

    /// <summary>
    /// 投射物抛物线发射（投篮 API）
    /// </summary>
    /// <param name="forceManitugde">力的大小</param>
    /// <param name="origin">起点</param>
    /// <param name="target">目标点</param>
    /// <param name="rb">刚体</param>
    /// <param name="isLowToss">是否低抛（低抛时间更短）</param>
    public static void ProjectileShooterShoot(float forceManitugde, Vector3 origin, Vector3 target, Rigidbody rb, bool isLowToss = true)
    {
        Vector3 delta = target - origin;
        float h = delta.y;
        Vector3 horizontalDelta = new Vector3(delta.x, 0, delta.z);
        float d = horizontalDelta.magnitude;
        float g = Mathf.Abs(Physics.gravity.y);
        float v0 = forceManitugde / rb.mass;
        float a = (g * d * d) / (2 * v0 * v0);
        float b = -d;
        float c = h + a;
        float discriminant = b * b - 4 * a * c;
        Vector3 launchDir;
        Vector3 horizontalDir = horizontalDelta.normalized;

        if (discriminant >= 0 && d > 0.001f)
        {
            float sqrtD = Mathf.Sqrt(discriminant);
            float t1 = (-b + sqrtD) / (2 * a);
            float t2 = (-b - sqrtD) / (2 * a);
            float t = isLowToss ? Mathf.Min(t1, t2) : Mathf.Max(t1, t2);
            float theta = Mathf.Atan(t);
            launchDir = horizontalDir * Mathf.Cos(theta) + Vector3.up * Mathf.Sin(theta);
        }
        else
        {
            launchDir = horizontalDir * Mathf.Cos(45) + Vector3.up * Mathf.Sin(45);
        }

        rb.transform.position = origin;
        rb.velocity = Vector3.zero;
        rb.AddForce(launchDir * forceManitugde, ForceMode.Impulse);
    }
}
