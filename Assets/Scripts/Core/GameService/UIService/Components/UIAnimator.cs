// LevityFramework - 通用 Unity 游戏框架
// UI 组件 - UIAnimator UI 动画组件

using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// UI 动画类型
/// </summary>
public enum UIAnimationType
{
    /// <summary>无动画</summary>
    None,
    /// <summary>淡入淡出</summary>
    Fade,
    /// <summary>缩放</summary>
    Scale,
    /// <summary>从左滑入</summary>
    SlideLeft,
    /// <summary>从右滑入</summary>
    SlideRight,
    /// <summary>从下滑入</summary>
    SlideUp,
    /// <summary>从上滑入</summary>
    SlideDown,
    /// <summary>缩放 + 淡入</summary>
    ScaleFade,
    /// <summary>弹出效果</summary>
    PopIn,
    /// <summary>弹跳效果</summary>
    Bounce
}

/// <summary>
/// UI 动画器
/// 提供预设的 UI 动画效果，基于 DOTween 实现
/// 挂载到 UI 窗口的根节点上
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class UIAnimator : MonoBehaviour
{
    [Header("显示动画")]
    [SerializeField] private UIAnimationType showAnimation = UIAnimationType.Fade;
    [SerializeField] private float showDuration = 0.3f;
    [SerializeField] private Ease showEase = Ease.OutQuad;
    [SerializeField] private float showDelay = 0f;

    [Header("隐藏动画")]
    [SerializeField] private UIAnimationType hideAnimation = UIAnimationType.Fade;
    [SerializeField] private float hideDuration = 0.2f;
    [SerializeField] private Ease hideEase = Ease.InQuad;
    [SerializeField] private float hideDelay = 0f;

    [Header("滑动设置")]
    [Tooltip("滑动动画的偏移量（相对于屏幕）")]
    [SerializeField] private float slideOffset = 1.2f;

    [Header("缩放设置")]
    [Tooltip("缩放动画的起始/结束缩放")]
    [SerializeField] private float scaleFrom = 0.8f;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 originalAnchoredPosition;
    private Vector3 originalScale;
    private Sequence currentSequence;
    private bool isInitialized;

    private void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// 初始化组件
    /// </summary>
    private void Initialize()
    {
        if (isInitialized) return;

        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        originalAnchoredPosition = rectTransform.anchoredPosition;
        originalScale = rectTransform.localScale;
        isInitialized = true;
    }

    /// <summary>
    /// 播放显示动画
    /// </summary>
    /// <param name="onComplete">完成回调</param>
    /// <returns>动画 Sequence</returns>
    public Sequence PlayShowAnimation(Action onComplete = null)
    {
        Initialize();
        KillCurrentAnimation();
        ResetToHiddenState();

        currentSequence = CreateAnimation(showAnimation, true, showDuration, showEase, showDelay, onComplete);
        return currentSequence;
    }

    /// <summary>
    /// 播放隐藏动画
    /// </summary>
    /// <param name="onComplete">完成回调</param>
    /// <returns>动画 Sequence</returns>
    public Sequence PlayHideAnimation(Action onComplete = null)
    {
        Initialize();
        KillCurrentAnimation();

        currentSequence = CreateAnimation(hideAnimation, false, hideDuration, hideEase, hideDelay, onComplete);
        return currentSequence;
    }

    /// <summary>
    /// 创建动画 Sequence
    /// </summary>
    private Sequence CreateAnimation(UIAnimationType type, bool isShow, float duration, Ease ease, float delay, Action onComplete)
    {
        Sequence seq = DOTween.Sequence();

        if (delay > 0)
        {
            seq.AppendInterval(delay);
        }

        switch (type)
        {
            case UIAnimationType.None:
                canvasGroup.alpha = isShow ? 1f : 0f;
                break;

            case UIAnimationType.Fade:
                seq.Append(canvasGroup.DOFade(isShow ? 1f : 0f, duration).SetEase(ease));
                break;

            case UIAnimationType.Scale:
                Vector3 targetScale = isShow ? originalScale : Vector3.one * scaleFrom;
                seq.Append(rectTransform.DOScale(targetScale, duration).SetEase(ease));
                break;

            case UIAnimationType.SlideLeft:
                {
                    float offset = Screen.width * slideOffset;
                    Vector2 targetPos = isShow ? originalAnchoredPosition : originalAnchoredPosition + new Vector2(-offset, 0);
                    seq.Append(rectTransform.DOAnchorPos(targetPos, duration).SetEase(ease));
                }
                break;

            case UIAnimationType.SlideRight:
                {
                    float offset = Screen.width * slideOffset;
                    Vector2 targetPos = isShow ? originalAnchoredPosition : originalAnchoredPosition + new Vector2(offset, 0);
                    seq.Append(rectTransform.DOAnchorPos(targetPos, duration).SetEase(ease));
                }
                break;

            case UIAnimationType.SlideUp:
                {
                    float offset = Screen.height * slideOffset;
                    Vector2 targetPos = isShow ? originalAnchoredPosition : originalAnchoredPosition + new Vector2(0, -offset);
                    seq.Append(rectTransform.DOAnchorPos(targetPos, duration).SetEase(ease));
                }
                break;

            case UIAnimationType.SlideDown:
                {
                    float offset = Screen.height * slideOffset;
                    Vector2 targetPos = isShow ? originalAnchoredPosition : originalAnchoredPosition + new Vector2(0, offset);
                    seq.Append(rectTransform.DOAnchorPos(targetPos, duration).SetEase(ease));
                }
                break;

            case UIAnimationType.ScaleFade:
                seq.Append(canvasGroup.DOFade(isShow ? 1f : 0f, duration).SetEase(ease));
                seq.Join(rectTransform.DOScale(isShow ? originalScale : originalScale * scaleFrom, duration).SetEase(ease));
                break;

            case UIAnimationType.PopIn:
                if (isShow)
                {
                    rectTransform.localScale = originalScale * 0.5f;
                    canvasGroup.alpha = 0f;
                    seq.Append(rectTransform.DOScale(originalScale * 1.05f, duration * 0.6f).SetEase(Ease.OutQuad));
                    seq.Append(rectTransform.DOScale(originalScale, duration * 0.4f).SetEase(Ease.InOutQuad));
                    seq.Insert(0, canvasGroup.DOFade(1f, duration * 0.5f));
                }
                else
                {
                    seq.Append(rectTransform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack));
                    seq.Join(canvasGroup.DOFade(0f, duration));
                }
                break;

            case UIAnimationType.Bounce:
                if (isShow)
                {
                    rectTransform.localScale = Vector3.zero;
                    canvasGroup.alpha = 1f;
                    seq.Append(rectTransform.DOScale(originalScale, duration).SetEase(Ease.OutBounce));
                }
                else
                {
                    seq.Append(rectTransform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack));
                }
                break;
        }

        seq.SetUpdate(true); // 不受 TimeScale 影响
        if (onComplete != null)
        {
            seq.OnComplete(() => onComplete());
        }

        return seq;
    }

    /// <summary>
    /// 重置到隐藏状态（用于播放显示动画前）
    /// </summary>
    private void ResetToHiddenState()
    {
        switch (showAnimation)
        {
            case UIAnimationType.Fade:
                canvasGroup.alpha = 0f;
                break;

            case UIAnimationType.Scale:
            case UIAnimationType.PopIn:
            case UIAnimationType.Bounce:
                rectTransform.localScale = Vector3.one * scaleFrom;
                canvasGroup.alpha = showAnimation == UIAnimationType.Scale ? 1f : 0f;
                break;

            case UIAnimationType.ScaleFade:
                canvasGroup.alpha = 0f;
                rectTransform.localScale = originalScale * scaleFrom;
                break;

            case UIAnimationType.SlideLeft:
                rectTransform.anchoredPosition = originalAnchoredPosition + new Vector2(-Screen.width * slideOffset, 0);
                break;

            case UIAnimationType.SlideRight:
                rectTransform.anchoredPosition = originalAnchoredPosition + new Vector2(Screen.width * slideOffset, 0);
                break;

            case UIAnimationType.SlideUp:
                rectTransform.anchoredPosition = originalAnchoredPosition + new Vector2(0, -Screen.height * slideOffset);
                break;

            case UIAnimationType.SlideDown:
                rectTransform.anchoredPosition = originalAnchoredPosition + new Vector2(0, Screen.height * slideOffset);
                break;
        }
    }

    /// <summary>
    /// 重置到显示状态
    /// </summary>
    public void ResetToShowState()
    {
        Initialize();
        canvasGroup.alpha = 1f;
        rectTransform.localScale = originalScale;
        rectTransform.anchoredPosition = originalAnchoredPosition;
    }

    /// <summary>
    /// 终止当前动画
    /// </summary>
    public void KillCurrentAnimation()
    {
        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
            currentSequence = null;
        }
    }

    /// <summary>
    /// 动画是否正在播放
    /// </summary>
    public bool IsPlaying => currentSequence != null && currentSequence.IsActive() && currentSequence.IsPlaying();

    /// <summary>
    /// 设置显示动画参数
    /// </summary>
    public void SetShowAnimation(UIAnimationType type, float duration = 0.3f, Ease ease = Ease.OutQuad)
    {
        showAnimation = type;
        showDuration = duration;
        showEase = ease;
    }

    /// <summary>
    /// 设置隐藏动画参数
    /// </summary>
    public void SetHideAnimation(UIAnimationType type, float duration = 0.2f, Ease ease = Ease.InQuad)
    {
        hideAnimation = type;
        hideDuration = duration;
        hideEase = ease;
    }

    private void OnDestroy()
    {
        KillCurrentAnimation();
    }

#if UNITY_EDITOR
    [ContextMenu("Preview Show Animation")]
    private void PreviewShowAnimation()
    {
        Initialize();
        PlayShowAnimation();
    }

    [ContextMenu("Preview Hide Animation")]
    private void PreviewHideAnimation()
    {
        Initialize();
        PlayHideAnimation();
    }

    [ContextMenu("Reset State")]
    private void ResetState()
    {
        Initialize();
        KillCurrentAnimation();
        ResetToShowState();
    }
#endif
}
