// LevityFramework - 通用 Unity 游戏框架
// 工具类 - DOTween 扩展方法

using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DOTween 扩展方法
/// </summary>
public static class DOTweenExtensions
{
    /// <summary>
    /// TMP_Text 透明度动画
    /// </summary>
    public static Tweener DOAlpha(this TMP_Text text, float endValue, float duration)
    {
        return DOTween.To(
            () => text.color.a,
            alpha =>
            {
                Color newColor = text.color;
                newColor.a = alpha;
                text.color = newColor;
            },
            endValue,
            duration
        );
    }

    /// <summary>
    /// TMP_Text 颜色动画
    /// </summary>
    public static Tweener DOColor(this TMP_Text text, Color endValue, float duration)
    {
        return DOTween.To(
            () => text.color,
            color => text.color = color,
            endValue,
            duration
        );
    }

    /// <summary>
    /// CanvasGroup 透明度动画
    /// </summary>
    public static Tweener DOFade(this CanvasGroup canvasGroup, float endValue, float duration)
    {
        return DOTween.To(
            () => canvasGroup.alpha,
            alpha => canvasGroup.alpha = alpha,
            endValue,
            duration
        );
    }

    /// <summary>
    /// RectTransform 锚点位置动画
    /// </summary>
    public static Tweener DOAnchorPos(this RectTransform rectTransform, Vector2 endValue, float duration)
    {
        return DOTween.To(
            () => rectTransform.anchoredPosition,
            pos => rectTransform.anchoredPosition = pos,
            endValue,
            duration
        );
    }

    /// <summary>
    /// RectTransform 尺寸动画
    /// </summary>
    public static Tweener DOSizeDelta(this RectTransform rectTransform, Vector2 endValue, float duration)
    {
        return DOTween.To(
            () => rectTransform.sizeDelta,
            size => rectTransform.sizeDelta = size,
            endValue,
            duration
        );
    }

    /// <summary>
    /// Transform 缩放动画（统一缩放）
    /// </summary>
    public static Tweener DOScale(this Transform transform, float endValue, float duration)
    {
        return transform.DOScale(Vector3.one * endValue, duration);
    }

    /// <summary>
    /// 弹性缩放动画（按下效果）
    /// </summary>
    public static Sequence DOPunchScale(this Transform transform, float punch = 0.1f, float duration = 0.3f)
    {
        return DOTween.Sequence()
            .Append(transform.DOScale(1f - punch, duration * 0.5f).SetEase(Ease.OutQuad))
            .Append(transform.DOScale(1f, duration * 0.5f).SetEase(Ease.OutBack));
    }

    /// <summary>
    /// 淡入效果
    /// </summary>
    public static Sequence DOFadeIn(this CanvasGroup canvasGroup, float duration = 0.3f)
    {
        canvasGroup.alpha = 0f;
        return DOTween.Sequence()
            .Append(canvasGroup.DOFade(1f, duration).SetEase(Ease.OutQuad));
    }

    /// <summary>
    /// 淡出效果
    /// </summary>
    public static Sequence DOFadeOut(this CanvasGroup canvasGroup, float duration = 0.3f)
    {
        return DOTween.Sequence()
            .Append(canvasGroup.DOFade(0f, duration).SetEase(Ease.OutQuad));
    }
}
