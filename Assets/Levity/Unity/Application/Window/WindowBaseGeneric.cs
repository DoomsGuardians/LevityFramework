// LevityFramework - 通用 Unity 游戏框架
// 核心指令模块 - WindowBase<TBinder> 带 Binder 的窗口基类

using UnityEngine;

/// <summary>
/// 带 Binder 的 UI 窗口基类。继承自 <see cref="WindowBase"/>，自动 GetComponent&lt;TBinder&gt;() 注入引用。
/// 字段赋值仍由派生类手动完成（保持显式绑定的清晰）。
/// </summary>
/// <typeparam name="TBinder">挂在窗口 GameObject 上的 Binder MonoBehaviour</typeparam>
/// <example>
/// public class SettingsWindow : WindowBase&lt;SettingsWindowBinder&gt;
/// {
///     private Button confirmBtn;
///     public override void OnAwake()
///     {
///         base.OnAwake();          // 此时 Binder 已被注入
///         confirmBtn = Binder.confirmBtn;
///         AddButtonListener(confirmBtn, OnConfirm);
///     }
/// }
/// </example>
public abstract class WindowBase<TBinder> : WindowBase where TBinder : MonoBehaviour
{
    /// <summary>
    /// 窗口 prefab 上挂载的 Binder 组件引用。OnAwake 调用 base 后即可使用。
    /// </summary>
    protected TBinder Binder { get; private set; }

    public override void OnAwake()
    {
        base.OnAwake();

        Binder = gameObject.GetComponent<TBinder>();
        if (Binder == null)
        {
            Debug.LogError($"[{GetType().Name}] 缺少 {typeof(TBinder).Name} 组件，请在窗口 prefab 上挂载。");
        }
    }
}
