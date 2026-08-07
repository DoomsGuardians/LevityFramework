// LevityFramework - 通用 Unity 游戏框架
// UI 组件 - SafeAreaAdapter 安全区适配组件

using UnityEngine;

/// <summary>
/// 安全区适配组件
/// 自动调整 RectTransform 以适应设备安全区（刘海屏、挖孔屏等）
/// 挂载到需要适配安全区的 UI 根节点上
/// </summary>
/// <example>
/// 使用方式：
/// 1. 将此组件添加到 Canvas 下的全屏 Panel
/// 2. 根据需要勾选需要适配的边
/// 3. 组件会自动调整 RectTransform 的锚点以适应安全区
/// </example>
[RequireComponent(typeof(RectTransform))]
[ExecuteAlways]
public class SafeAreaAdapter : MonoBehaviour
{
    [Header("适配设置")]
    [Tooltip("适配顶部（通常用于刘海屏）")]
    [SerializeField] private bool applyTop = true;

    [Tooltip("适配底部（通常用于手势条区域）")]
    [SerializeField] private bool applyBottom = true;

    [Tooltip("适配左侧")]
    [SerializeField] private bool applyLeft = true;

    [Tooltip("适配右侧")]
    [SerializeField] private bool applyRight = true;

    [Header("调试")]
    [Tooltip("在编辑器中模拟安全区")]
    [SerializeField] private bool simulateInEditor = false;

    [Tooltip("模拟的安全区边距 (左, 下, 右, 上)")]
    [SerializeField] private Vector4 simulatedSafeAreaInsets = new Vector4(0, 34, 0, 44);

    private RectTransform rectTransform;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private ScreenOrientation lastOrientation;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        ApplySafeArea();
    }

    private void Update()
    {
        // 仅在安全区、屏幕尺寸或方向变化时更新
        Rect currentSafeArea = GetSafeArea();
        if (lastSafeArea != currentSafeArea ||
            lastScreenSize.x != Screen.width ||
            lastScreenSize.y != Screen.height ||
            lastOrientation != Screen.orientation)
        {
            ApplySafeArea();
        }
    }

    /// <summary>
    /// 获取当前安全区
    /// </summary>
    private Rect GetSafeArea()
    {
#if UNITY_EDITOR
        if (simulateInEditor)
        {
            // 在编辑器中模拟安全区
            return new Rect(
                simulatedSafeAreaInsets.x,
                simulatedSafeAreaInsets.y,
                Screen.width - simulatedSafeAreaInsets.x - simulatedSafeAreaInsets.z,
                Screen.height - simulatedSafeAreaInsets.y - simulatedSafeAreaInsets.w
            );
        }
#endif
        return Screen.safeArea;
    }

    /// <summary>
    /// 应用安全区适配
    /// </summary>
    private void ApplySafeArea()
    {
        Rect safeArea = GetSafeArea();
        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        lastOrientation = Screen.orientation;

        if (Screen.width == 0 || Screen.height == 0) return;

        // 计算锚点
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        // 根据设置应用各边的适配
        Vector2 originalAnchorMin = rectTransform.anchorMin;
        Vector2 originalAnchorMax = rectTransform.anchorMax;

        if (!applyLeft) anchorMin.x = originalAnchorMin.x;
        if (!applyBottom) anchorMin.y = originalAnchorMin.y;
        if (!applyRight) anchorMax.x = originalAnchorMax.x;
        if (!applyTop) anchorMax.y = originalAnchorMax.y;

        // 应用锚点
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;

        // 重置偏移
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// 强制刷新安全区适配
    /// </summary>
    public void Refresh()
    {
        lastSafeArea = Rect.zero;
        ApplySafeArea();
    }

    /// <summary>
    /// 设置适配边
    /// </summary>
    public void SetApplyEdges(bool top, bool bottom, bool left, bool right)
    {
        applyTop = top;
        applyBottom = bottom;
        applyLeft = left;
        applyRight = right;
        ApplySafeArea();
    }

#if UNITY_EDITOR
    [ContextMenu("Apply Safe Area Now")]
    private void ApplySafeAreaEditor()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    [ContextMenu("Reset to Full Screen")]
    private void ResetToFullScreen()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private void OnValidate()
    {
        // 编辑器中参数变化时自动刷新
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (gameObject.activeInHierarchy)
        {
            ApplySafeArea();
        }
    }
#endif
}
