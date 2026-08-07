// LevityFramework - 通用 Unity 游戏框架
// 工具模块 - ColorExtensions 颜色扩展方法

using System;
using UnityEngine;

/// <summary>
/// Color 和 Color32 扩展方法
/// 提供便捷的颜色操作
/// </summary>
public static class ColorExtensions
{
    #region Alpha 操作

    /// <summary>
    /// 设置 Alpha 值并返回新颜色
    /// </summary>
    public static Color WithAlpha(this Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }

    /// <summary>
    /// 设置 Alpha 值并返回新颜色（Color32 版本）
    /// </summary>
    public static Color32 WithAlpha(this Color32 color, byte alpha)
    {
        return new Color32(color.r, color.g, color.b, alpha);
    }

    /// <summary>
    /// 淡入效果（增加 Alpha）
    /// </summary>
    public static Color FadeIn(this Color color, float amount)
    {
        return color.WithAlpha(Mathf.Clamp01(color.a + amount));
    }

    /// <summary>
    /// 淡出效果（减少 Alpha）
    /// </summary>
    public static Color FadeOut(this Color color, float amount)
    {
        return color.WithAlpha(Mathf.Clamp01(color.a - amount));
    }

    #endregion

    #region 颜色混合

    /// <summary>
    /// 混合两个颜色
    /// </summary>
    /// <param name="color1">第一个颜色</param>
    /// <param name="color2">第二个颜色</param>
    /// <param name="ratio">混合比例（0-1），0 = color1，1 = color2</param>
    public static Color Blend(this Color color1, Color color2, float ratio)
    {
        ratio = Mathf.Clamp01(ratio);
        return new Color(
            color1.r * (1 - ratio) + color2.r * ratio,
            color1.g * (1 - ratio) + color2.g * ratio,
            color1.b * (1 - ratio) + color2.b * ratio,
            color1.a * (1 - ratio) + color2.a * ratio
        );
    }

    /// <summary>
    /// 加法混合颜色（结果 clamp 到 0-1）
    /// </summary>
    public static Color Add(this Color thisColor, Color otherColor)
    {
        return (thisColor + otherColor).Clamp01();
    }

    /// <summary>
    /// 减法混合颜色（结果 clamp 到 0-1）
    /// </summary>
    public static Color Subtract(this Color thisColor, Color otherColor)
    {
        return (thisColor - otherColor).Clamp01();
    }

    /// <summary>
    /// 将颜色各分量限制在 0-1 范围内
    /// </summary>
    public static Color Clamp01(this Color color)
    {
        return new Color(
            Mathf.Clamp01(color.r),
            Mathf.Clamp01(color.g),
            Mathf.Clamp01(color.b),
            Mathf.Clamp01(color.a)
        );
    }

    #endregion

    #region 颜色变换

    /// <summary>
    /// 反转颜色（保留 Alpha）
    /// </summary>
    public static Color Invert(this Color color)
    {
        return new Color(1f - color.r, 1f - color.g, 1f - color.b, color.a);
    }

    /// <summary>
    /// 转换为灰度
    /// </summary>
    public static Color ToGrayscale(this Color color)
    {
        float gray = color.grayscale;
        return new Color(gray, gray, gray, color.a);
    }

    /// <summary>
    /// 调整亮度
    /// </summary>
    /// <param name="color">原颜色</param>
    /// <param name="brightness">亮度调整值（-1 到 1，0 = 不变）</param>
    public static Color AdjustBrightness(this Color color, float brightness)
    {
        return new Color(
            Mathf.Clamp01(color.r + brightness),
            Mathf.Clamp01(color.g + brightness),
            Mathf.Clamp01(color.b + brightness),
            color.a
        );
    }

    /// <summary>
    /// 调整饱和度
    /// </summary>
    /// <param name="color">原颜色</param>
    /// <param name="saturation">饱和度系数（0 = 灰度，1 = 原色，>1 = 更饱和）</param>
    public static Color AdjustSaturation(this Color color, float saturation)
    {
        float gray = color.grayscale;
        return new Color(
            Mathf.Clamp01(gray + (color.r - gray) * saturation),
            Mathf.Clamp01(gray + (color.g - gray) * saturation),
            Mathf.Clamp01(gray + (color.b - gray) * saturation),
            color.a
        );
    }

    #endregion

    #region 十六进制转换

    /// <summary>
    /// 转换为十六进制字符串（#RRGGBBAA）
    /// </summary>
    public static string ToHex(this Color color)
    {
        return $"#{ColorUtility.ToHtmlStringRGBA(color)}";
    }

    /// <summary>
    /// 转换为十六进制字符串（#RRGGBB，忽略 Alpha）
    /// </summary>
    public static string ToHexRGB(this Color color)
    {
        return $"#{ColorUtility.ToHtmlStringRGB(color)}";
    }

    /// <summary>
    /// 从十六进制字符串解析颜色
    /// </summary>
    /// <param name="hex">十六进制字符串（支持 #RGB, #RGBA, #RRGGBB, #RRGGBBAA）</param>
    /// <returns>解析后的颜色，解析失败返回白色</returns>
    public static Color FromHex(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return Color.white;

        if (!hex.StartsWith("#"))
            hex = "#" + hex;

        if (ColorUtility.TryParseHtmlString(hex, out Color color))
        {
            return color;
        }

        Debug.LogWarning($"无法解析颜色: {hex}");
        return Color.white;
    }

    /// <summary>
    /// 尝试从十六进制字符串解析颜色
    /// </summary>
    public static bool TryFromHex(string hex, out Color color)
    {
        color = Color.white;
        if (string.IsNullOrEmpty(hex)) return false;

        if (!hex.StartsWith("#"))
            hex = "#" + hex;

        return ColorUtility.TryParseHtmlString(hex, out color);
    }

    #endregion

    #region With 方法

    /// <summary>
    /// 替换 R 分量
    /// </summary>
    public static Color WithR(this Color color, float r)
    {
        return new Color(r, color.g, color.b, color.a);
    }

    /// <summary>
    /// 替换 G 分量
    /// </summary>
    public static Color WithG(this Color color, float g)
    {
        return new Color(color.r, g, color.b, color.a);
    }

    /// <summary>
    /// 替换 B 分量
    /// </summary>
    public static Color WithB(this Color color, float b)
    {
        return new Color(color.r, color.g, b, color.a);
    }

    #endregion

    #region HSV 操作

    /// <summary>
    /// 获取 HSV 值
    /// </summary>
    public static void ToHSV(this Color color, out float h, out float s, out float v)
    {
        Color.RGBToHSV(color, out h, out s, out v);
    }

    /// <summary>
    /// 从 HSV 创建颜色
    /// </summary>
    public static Color FromHSV(float h, float s, float v, float a = 1f)
    {
        Color color = Color.HSVToRGB(h, s, v);
        color.a = a;
        return color;
    }

    /// <summary>
    /// 调整色相
    /// </summary>
    /// <param name="color">原颜色</param>
    /// <param name="hueShift">色相偏移（0-1 循环）</param>
    public static Color ShiftHue(this Color color, float hueShift)
    {
        Color.RGBToHSV(color, out float h, out float s, out float v);
        h = (h + hueShift) % 1f;
        if (h < 0) h += 1f;
        Color result = Color.HSVToRGB(h, s, v);
        result.a = color.a;
        return result;
    }

    #endregion
}
