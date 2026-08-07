// LevityFramework - 通用 Unity 游戏框架
// 核心服务模块 - AudioService 音频服务

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音频播放服务：管理 BGM、SFX 的播放、停止、淡入/淡出
/// </summary>
public class AudioService : ILogic
{
    private AudioSource bgmSource;
    private List<AudioSource> sfxSources = new List<AudioSource>();
    private GameObject audioRoot;
    private int maxSfxSources = 10;

    public float BGMVolume { get; set; } = 1f;
    public float SFXVolume { get; set; } = 1f;

    public void OnInit()
    {
        audioRoot = new GameObject("AudioRoot");
        GameObject.DontDestroyOnLoad(audioRoot);

        // 创建 BGM 音源
        var bgmObj = new GameObject("BGM");
        bgmObj.transform.SetParent(audioRoot.transform);
        bgmSource = bgmObj.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        // 预创建 SFX 音源池
        for (int i = 0; i < maxSfxSources; i++)
        {
            CreateSfxSource();
        }
    }

    public void OnEnterState() { }

    public void OnUpdate() { }

    public void UnInit()
    {
        StopBGM();
        StopAllSFX();
        if (audioRoot != null)
        {
            GameObject.Destroy(audioRoot);
        }
    }

    private AudioSource CreateSfxSource()
    {
        var sfxObj = new GameObject("SFX");
        sfxObj.transform.SetParent(audioRoot.transform);
        var source = sfxObj.AddComponent<AudioSource>();
        source.loop = false;
        source.playOnAwake = false;
        sfxSources.Add(source);
        return source;
    }

    #region BGM
    /// <summary>
    /// 播放背景音乐
    /// </summary>
    public void PlayBGM(AudioClip clip, float volume = 1f)
    {
        if (bgmSource == null || clip == null) return;

        bgmSource.clip = clip;
        bgmSource.volume = volume * BGMVolume;
        bgmSource.Play();
    }

    /// <summary>
    /// 停止背景音乐
    /// </summary>
    public void StopBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    /// <summary>
    /// 暂停背景音乐
    /// </summary>
    public void PauseBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Pause();
        }
    }

    /// <summary>
    /// 恢复背景音乐
    /// </summary>
    public void ResumeBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.UnPause();
        }
    }

    /// <summary>
    /// 设置 BGM 音量
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        BGMVolume = Mathf.Clamp01(volume);
        if (bgmSource != null)
        {
            bgmSource.volume = BGMVolume;
        }
    }
    #endregion

    #region SFX
    /// <summary>
    /// 播放音效
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        var source = GetAvailableSfxSource();
        source.clip = clip;
        source.volume = volume * SFXVolume;
        source.Play();
    }

    /// <summary>
    /// 播放音效（在指定位置）
    /// </summary>
    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position, volume * SFXVolume);
    }

    /// <summary>
    /// 停止所有音效
    /// </summary>
    public void StopAllSFX()
    {
        foreach (var source in sfxSources)
        {
            source.Stop();
        }
    }

    /// <summary>
    /// 设置 SFX 音量
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        SFXVolume = Mathf.Clamp01(volume);
    }

    private AudioSource GetAvailableSfxSource()
    {
        foreach (var source in sfxSources)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }
        // 所有音源都在使用，创建新的
        return CreateSfxSource();
    }
    #endregion
}
