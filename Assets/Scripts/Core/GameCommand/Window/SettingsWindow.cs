// LevityFramework - 通用 Unity 游戏框架
// 核心指令模块 - SettingsWindow 设置窗口

#if NANINOVEL
using Naninovel;
using Naninovel.Async;
#endif
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 设置窗口：读写 Naninovel 音量、文字速度、Auto 延迟等设置项。
/// 关闭时自动调用 IStateManager.SaveSettings() 持久化。
/// </summary>
public class SettingsWindow : WindowBase<SettingsWindowBinder>
{
    public override void OnAwake()
    {
        base.OnAwake();

        // 从 Binder 取组件引用，注册监听
        AddSliderListener(Binder.bgmVolumeSlider,   OnBgmVolumeChanged);
        AddSliderListener(Binder.sfxVolumeSlider,   OnSfxVolumeChanged);
        AddSliderListener(Binder.voiceVolumeSlider, OnVoiceVolumeChanged);
        AddSliderListener(Binder.textSpeedSlider,   OnTextSpeedChanged);
        AddSliderListener(Binder.autoDelaySlider,   OnAutoDelayChanged);
        AddButtonListener(Binder.closeButton,       OnCloseClicked);
    }

    public override void OnShow()
    {
        base.OnShow();
        RefreshFromNaninovel();
    }

    public override void OnHide()
    {
        SaveNaninovelSettings();
        base.OnHide();
    }

    // ── 读取当前值到 UI ────────────────────────────────────────────────────────
    private void RefreshFromNaninovel()
    {
#if NANINOVEL
        if (!Engine.Initialized) return;

        var audio = Engine.GetService<IAudioManager>();
        if (audio != null)
        {
            SetSliderWithoutNotify(Binder.bgmVolumeSlider,   audio.BgmVolume);
            SetSliderWithoutNotify(Binder.sfxVolumeSlider,   audio.SfxVolume);
            SetSliderWithoutNotify(Binder.voiceVolumeSlider, audio.VoiceVolume);
        }

        var printer = Engine.GetService<ITextPrinterManager>();
        if (printer != null)
        {
            SetSliderWithoutNotify(Binder.textSpeedSlider, printer.BaseRevealSpeed);
            SetSliderWithoutNotify(Binder.autoDelaySlider, printer.BaseAutoDelay);
        }
#endif
    }

    // ── 回调：slider 值变化 ───────────────────────────────────────────────────
    private void OnBgmVolumeChanged(float value)
    {
#if NANINOVEL
        if (Engine.Initialized)
            Engine.GetService<IAudioManager>()?.SetBgmVolumeAsync(value).Forget();
#endif
    }

    private void OnSfxVolumeChanged(float value)
    {
#if NANINOVEL
        if (Engine.Initialized)
            Engine.GetService<IAudioManager>()?.SetSfxVolumeAsync(value).Forget();
#endif
    }

    private void OnVoiceVolumeChanged(float value)
    {
#if NANINOVEL
        if (Engine.Initialized)
            Engine.GetService<IAudioManager>()?.SetVoiceVolumeAsync(value).Forget();
#endif
    }

    private void OnTextSpeedChanged(float value)
    {
#if NANINOVEL
        if (Engine.Initialized)
        {
            var printer = Engine.GetService<ITextPrinterManager>();
            if (printer != null) printer.BaseRevealSpeed = value;
        }
#endif
    }

    private void OnAutoDelayChanged(float value)
    {
#if NANINOVEL
        if (Engine.Initialized)
        {
            var printer = Engine.GetService<ITextPrinterManager>();
            if (printer != null) printer.BaseAutoDelay = value;
        }
#endif
    }

    private void OnCloseClicked()
    {
        _ = dataService?.SaveToSlot(0);   // 顺便触发游戏侧存档
        uIService?.HideWindow(Name);
    }

    // ── 持久化 ────────────────────────────────────────────────────────────────
    private void SaveNaninovelSettings()
    {
#if NANINOVEL
        if (Engine.Initialized)
            Engine.GetService<IStateManager>()?.SaveSettings().Forget();
#endif
    }

    // ── 辅助 ─────────────────────────────────────────────────────────────────
    private static void AddSliderListener(Slider slider, UnityEngine.Events.UnityAction<float> action)
    {
        if (slider != null) slider.onValueChanged.AddListener(action);
    }

    private static void SetSliderWithoutNotify(Slider slider, float value)
    {
        if (slider != null) slider.SetValueWithoutNotify(value);
    }
}
