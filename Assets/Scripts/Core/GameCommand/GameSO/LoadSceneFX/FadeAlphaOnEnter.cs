// LevityFramework - 通用 Unity 游戏框架
// 核心指令模块 - FadeAlphaOnEnter 淡入命令

using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 淡入命令：场景进入时从黑屏淡入
/// </summary>
[CreateAssetMenu(fileName = "FadeAlphaOnEnter", menuName = "LevityFramework/Stage/FX/FadeAlphaOnEnter")]
public class FadeAlphaOnEnter : CommandLogicSOBase
{
    [Header("Settings")]
    [Tooltip("淡入持续时间")]
    public float duration = 1.5f;

    [Tooltip("开始前延迟")]
    public float delay = 0.5f;

    [Tooltip("缓动曲线")]
    public Ease ease = Ease.InOutQuad;

    protected override void OnExecute(Action doneCB, object param = null)
    {
        // 需要项目实现 AlphaMaskWindow 或类似的遮罩 UI
        // 这里提供一个通用的 CanvasGroup 淡入实现示例
        var maskCanvas = CreateOrGetMaskCanvas();
        if (maskCanvas != null)
        {
            maskCanvas.alpha = 1f;
            maskCanvas.DOFade(0f, duration)
                .SetDelay(delay)
                .SetEase(ease)
                .OnComplete(() =>
                {
                    maskCanvas.gameObject.SetActive(false);
                    doneCB?.Invoke();
                });
        }
        else
        {
            doneCB?.Invoke();
        }
    }

    private CanvasGroup CreateOrGetMaskCanvas()
    {
        var existing = GameObject.Find("ScreenFadeMask");
        if (existing != null)
        {
            existing.SetActive(true);
            return existing.GetComponent<CanvasGroup>();
        }

        // 创建一个简单的黑色遮罩
        var go = new GameObject("ScreenFadeMask");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        var canvasGroup = go.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;

        var imageGo = new GameObject("Background");
        imageGo.transform.SetParent(go.transform, false);
        var image = imageGo.AddComponent<Image>();
        image.color = Color.black;

        var rectTransform = imageGo.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;

        return canvasGroup;
    }
}
