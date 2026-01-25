// LevityFramework - 通用 Unity 游戏框架
// 工具类 - DOTween 扩展方法

using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 滑动方向
/// </summary>
public enum SlideDirection
{
    Left,
    Right,
    Up,
    Down
}

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

    /// <summary>
    /// 滑入动画
    /// </summary>
    /// <param name="rectTransform">目标 RectTransform</param>
    /// <param name="direction">滑入方向（从哪个方向滑入）</param>
    /// <param name="duration">动画时长</param>
    /// <param name="offsetMultiplier">偏移倍数（相对于屏幕尺寸）</param>
    /// <returns>Tweener</returns>
    public static Tweener DOSlideIn(this RectTransform rectTransform, SlideDirection direction, float duration = 0.3f, float offsetMultiplier = 1.2f)
    {
        Vector2 originalPos = rectTransform.anchoredPosition;
        Vector2 startPos = originalPos;

        switch (direction)
        {
            case SlideDirection.Left:
                startPos.x = -Screen.width * offsetMultiplier;
                break;
            case SlideDirection.Right:
                startPos.x = Screen.width * offsetMultiplier;
                break;
            case SlideDirection.Up:
                startPos.y = Screen.height * offsetMultiplier;
                break;
            case SlideDirection.Down:
                startPos.y = -Screen.height * offsetMultiplier;
                break;
        }

        rectTransform.anchoredPosition = startPos;
        return rectTransform.DOAnchorPos(originalPos, duration).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 滑出动画
    /// </summary>
    /// <param name="rectTransform">目标 RectTransform</param>
    /// <param name="direction">滑出方向（向哪个方向滑出）</param>
    /// <param name="duration">动画时长</param>
    /// <param name="offsetMultiplier">偏移倍数（相对于屏幕尺寸）</param>
    /// <returns>Tweener</returns>
    public static Tweener DOSlideOut(this RectTransform rectTransform, SlideDirection direction, float duration = 0.3f, float offsetMultiplier = 1.2f)
    {
        Vector2 endPos = rectTransform.anchoredPosition;

        switch (direction)
        {
            case SlideDirection.Left:
                endPos.x = -Screen.width * offsetMultiplier;
                break;
            case SlideDirection.Right:
                endPos.x = Screen.width * offsetMultiplier;
                break;
            case SlideDirection.Up:
                endPos.y = Screen.height * offsetMultiplier;
                break;
            case SlideDirection.Down:
                endPos.y = -Screen.height * offsetMultiplier;
                break;
        }

        return rectTransform.DOAnchorPos(endPos, duration).SetEase(Ease.InQuad);
    }

    /// <summary>
    /// 弹入动画（缩放 + 淡入）
    /// </summary>
    /// <param name="transform">目标 Transform</param>
    /// <param name="canvasGroup">CanvasGroup（用于淡入，可选）</param>
    /// <param name="duration">动画时长</param>
    /// <returns>Sequence</returns>
    public static Sequence DOPopIn(this Transform transform, CanvasGroup canvasGroup = null, float duration = 0.3f)
    {
        Vector3 originalScale = transform.localScale;
        transform.localScale = originalScale * 0.5f;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(originalScale * 1.05f, duration * 0.6f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOScale(originalScale, duration * 0.4f).SetEase(Ease.InOutQuad));

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            seq.Insert(0, canvasGroup.DOFade(1f, duration * 0.5f));
        }

        return seq;
    }

    /// <summary>
    /// 弹出动画（缩放 + 淡出）
    /// </summary>
    /// <param name="transform">目标 Transform</param>
    /// <param name="canvasGroup">CanvasGroup（用于淡出，可选）</param>
    /// <param name="duration">动画时长</param>
    /// <returns>Sequence</returns>
    public static Sequence DOPopOut(this Transform transform, CanvasGroup canvasGroup = null, float duration = 0.2f)
    {
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack));

        if (canvasGroup != null)
        {
            seq.Join(canvasGroup.DOFade(0f, duration));
        }

        return seq;
    }

    /// <summary>
    /// 弹跳缩放动画
    /// </summary>
    /// <param name="transform">目标 Transform</param>
    /// <param name="duration">动画时长</param>
    /// <returns>Tweener</returns>
    public static Tweener DOBounceIn(this Transform transform, float duration = 0.5f)
    {
        Vector3 originalScale = transform.localScale;
        transform.localScale = Vector3.zero;
        return transform.DOScale(originalScale, duration).SetEase(Ease.OutBounce);
    }

    /// <summary>
    /// 抖动效果
    /// </summary>
    /// <param name="transform">目标 Transform</param>
    /// <param name="strength">抖动强度</param>
    /// <param name="duration">动画时长</param>
    /// <param name="vibrato">震动次数</param>
    /// <returns>Tweener</returns>
    public static Tweener DOShake(this Transform transform, float strength = 10f, float duration = 0.5f, int vibrato = 10)
    {
        return transform.DOShakePosition(duration, strength, vibrato);
    }

    /// <summary>
    /// UI 抖动效果（RectTransform）
    /// </summary>
    /// <param name="rectTransform">目标 RectTransform</param>
    /// <param name="strength">抖动强度</param>
    /// <param name="duration">动画时长</param>
    /// <param name="vibrato">震动次数</param>
    /// <returns>Tweener</returns>
    public static Tweener DOShakeUI(this RectTransform rectTransform, float strength = 10f, float duration = 0.5f, int vibrato = 10)
    {
        return rectTransform.DOShakeAnchorPos(duration, strength, vibrato);
    }

    /// <summary>
    /// 心跳效果（周期性缩放）
    /// </summary>
    /// <param name="transform">目标 Transform</param>
    /// <param name="scale">缩放大小</param>
    /// <param name="duration">单次动画时长</param>
    /// <returns>Sequence（设置为无限循环）</returns>
    public static Sequence DOHeartbeat(this Transform transform, float scale = 1.1f, float duration = 0.5f)
    {
        Vector3 originalScale = transform.localScale;
        return DOTween.Sequence()
            .Append(transform.DOScale(originalScale * scale, duration * 0.3f).SetEase(Ease.OutQuad))
            .Append(transform.DOScale(originalScale, duration * 0.7f).SetEase(Ease.InOutQuad))
            .SetLoops(-1);
    }

    /// <summary>
    /// 数字滚动动画
    /// </summary>
    /// <param name="text">目标 TMP_Text</param>
    /// <param name="startValue">起始值</param>
    /// <param name="endValue">结束值</param>
    /// <param name="duration">动画时长</param>
    /// <param name="format">数字格式（如 "N0", "F2"）</param>
    /// <returns>Tweener</returns>
    public static Tweener DOCountTo(this TMP_Text text, float startValue, float endValue, float duration, string format = "N0")
    {
        return DOTween.To(
            () => startValue,
            value => text.text = value.ToString(format),
            endValue,
            duration
        ).SetEase(Ease.Linear);
    }
}
