// LevityFramework - 通用 Unity 游戏框架
// 核心指令模块 - FadeAlphaOnExit 淡出命令

using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 淡出命令：场景退出时淡出到黑屏
/// </summary>
[CreateAssetMenu(fileName = "FadeAlphaOnExit", menuName = "LevityFramework/Stage/FX/FadeAlphaOnExit")]
public class FadeAlphaOnExit : CommandLogicSOBase
{
    [Header("Settings")]
    [Tooltip("淡出持续时间")]
    public float duration = 1f;

    [Tooltip("缓动曲线")]
    public Ease ease = Ease.InOutQuad;

    protected override void OnExecute(Action doneCB, object param = null)
    {
        var maskCanvas = CreateOrGetMaskCanvas();
        if (maskCanvas != null)
        {
            maskCanvas.gameObject.SetActive(true);
            maskCanvas.alpha = 0f;
            maskCanvas.DOFade(1f, duration)
                .SetEase(ease)
                .OnComplete(() =>
                {
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
            return existing.GetComponent<CanvasGroup>();
        }

        // 创建一个简单的黑色遮罩
        var go = new GameObject("ScreenFadeMask");
        UnityEngine.Object.DontDestroyOnLoad(go);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        var canvasGroup = go.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0f;

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
