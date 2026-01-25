// LevityFramework - 通用 Unity 游戏框架
// 核心服务模块 - NaninovelService Naninovel 集成服务
// 注意：此文件仅在定义了 NANINOVEL 符号时编译

#if NANINOVEL
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Naninovel;
using Naninovel.Async;
using UnityEngine;

/// <summary>
/// Naninovel 视觉小说框架的集成服务
/// 提供脚本播放、对话队列、输入管理等功能
/// </summary>
public class NaninovelService : ILogic
{
    private UniTask initializationTask = UniTask.CompletedTask;
    private bool initializationRequested;
    private bool initializationSucceeded;
    private bool initializationFailed;

    private IScriptPlayer scriptPlayer;

    private GameRoot gameRoot;
    private EventService eventService;
    private InputService inputService;

    private CancellationTokenSource lifetimeCts;

    private string pendingStartLabel;
    private string activePlaybackLabel;
    private string currentScriptPath;
    private bool releaseInputOnStop;
    private bool allowNaninovelCamera;

    public bool IsInitialized => initializationSucceeded && scriptPlayer != null && Engine.Initialized;
    public bool IsPlaying => scriptPlayer != null && (scriptPlayer.Playing || scriptPlayer.Completing);

    public Camera NaniCamera => TryGetNaninovelCamera(out var camera) ? camera : null;

    public void OnInit()
    {
        gameRoot = GameRoot.Instance;
        eventService = gameRoot?.eventService;
        inputService = gameRoot?.inputService;

        lifetimeCts = new CancellationTokenSource();
        initializationTask = InitializeAsync(lifetimeCts.Token);
        initializationRequested = true;
    }

    public void OnUpdate() { }

    public void OnEnterState() { }

    public void UnInit()
    {
        lifetimeCts?.Cancel();
        lifetimeCts?.Dispose();
        lifetimeCts = null;

        if (scriptPlayer != null)
        {
            scriptPlayer.OnPlay -= HandleScriptPlay;
            scriptPlayer.OnStop -= HandleScriptStop;
        }

        if (releaseInputOnStop)
        {
            RestoreGameplayInput();
            releaseInputOnStop = false;
        }

        scriptPlayer = null;
        initializationRequested = false;
        initializationSucceeded = false;
        initializationFailed = false;
        pendingStartLabel = null;
        activePlaybackLabel = null;
        currentScriptPath = null;
        allowNaninovelCamera = false;
        UpdateNaninovelCameraState(forceDisable: true);
    }

    /// <summary>
    /// 播放 Naninovel 脚本
    /// </summary>
    public async UniTask PlayScriptAsync(
        string scriptName,
        string startLabel = null,
        bool waitForCompletion = true,
        bool pauseGameplayInput = true,
        bool stopCurrent = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scriptName))
        {
            Debug.LogWarning("[NaninovelService] Attempted to play script with empty name.");
            return;
        }

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            lifetimeCts?.Token ?? CancellationToken.None,
            cancellationToken);
        var token = linkedCts.Token;

        try
        {
            await EnsureInitializedAsync(token);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NaninovelService] Engine unavailable: {ex.Message}");
            return;
        }

        if (!IsInitialized) return;

        if (IsPlaying)
        {
            if (stopCurrent)
            {
                try { await scriptPlayer.Complete(); }
                catch (Exception ex) { Debug.LogWarning($"[NaninovelService] Failed to complete active script: {ex.Message}"); }
            }
            else
            {
                Debug.LogWarning($"[NaninovelService] Script already playing. Request for '{scriptName}' ignored.");
                return;
            }
        }

        bool lockedInputThisPlayback = false;

        try
        {
            pendingStartLabel = startLabel;

            if (pauseGameplayInput)
            {
                AcquireGameplayInputLock();
                lockedInputThisPlayback = true;
                releaseInputOnStop = true;
            }

            if (!string.IsNullOrEmpty(startLabel))
                await scriptPlayer.LoadAndPlayAtLabel(scriptName, startLabel);
            else
                await scriptPlayer.LoadAndPlay(scriptName);

            if (waitForCompletion)
            {
                while (!token.IsCancellationRequested && (scriptPlayer.Playing || scriptPlayer.Completing))
                    await AsyncUtils.WaitEndOfFrame();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Debug.LogError($"[NaninovelService] Failed to play script '{scriptName}': {ex}");
        }
        finally
        {
            pendingStartLabel = null;

            if (lockedInputThisPlayback && !IsPlaying && releaseInputOnStop)
            {
                ReleaseGameplayInputLock();
                releaseInputOnStop = false;
            }
        }
    }

    /// <summary>
    /// 播放 NaninovelStageFlow 配置的脚本
    /// </summary>
    public UniTask PlayScriptAsync(NaninovelStageFlow flow, CancellationToken cancellationToken = default)
    {
        if (flow == null) return UniTask.CompletedTask;

        return PlayScriptAsync(
            flow.scriptName,
            flow.startLabel,
            flow.waitForCompletion,
            flow.pauseGameplayInput,
            true,
            cancellationToken);
    }

    /// <summary>
    /// 应用关卡配置
    /// </summary>
    public void ApplyStageConfig(StageConfigItem stageConfig)
    {
        if (stageConfig == null) return;

        var flow = stageConfig.naninovelFlow;
        allowNaninovelCamera = flow?.useNaniCamera ?? false;

        if (!UpdateNaninovelCameraState() && allowNaninovelCamera && Engine.Behaviour != null)
        {
            Debug.LogWarning("[NaninovelService] No Naninovel camera found.");
        }

        if (flow != null && flow.autoPlayOnEnter && !string.IsNullOrWhiteSpace(flow.scriptName))
        {
            PlayScriptAsync(flow).Forget();
        }
    }

    /// <summary>
    /// 请求播放对话
    /// </summary>
    public void RequestDialogue(NaninovelDialogueRequest request)
    {
        if (!request.IsValid)
        {
            Debug.LogWarning("[NaninovelService] Invalid dialogue request.");
            return;
        }

        PlayScriptAsync(
            request.ScriptName,
            request.StartLabel,
            request.WaitForCompletion,
            request.PauseGameplayInput,
            request.StopCurrentPlayback).Forget();
    }

    private async UniTask InitializeAsync(CancellationToken token)
    {
        try
        {
            await RuntimeInitializer.Initialize();
            if (token.IsCancellationRequested) return;

            scriptPlayer = Engine.GetService<IScriptPlayer>();

            if (scriptPlayer == null)
            {
                try
                {
                    await UniTask.WaitUntil(() => Engine.GetService<IScriptPlayer>() != null, cancellationToken: token);
                    scriptPlayer = Engine.GetService<IScriptPlayer>();
                }
                catch (OperationCanceledException) { }
            }

            if (token.IsCancellationRequested) return;

            if (scriptPlayer == null)
                throw new InvalidOperationException("IScriptPlayer service unavailable.");

            scriptPlayer.OnPlay += HandleScriptPlay;
            scriptPlayer.OnStop += HandleScriptStop;

            UpdateNaninovelCameraState();

            initializationSucceeded = true;
            initializationFailed = false;
            eventService?.SendMessage(EventID.OnDialogueServiceReady, null, null);
        }
        catch (Exception ex)
        {
            initializationFailed = true;
            initializationSucceeded = false;
            Debug.LogError($"[NaninovelService] Initialization failed: {ex}");
            throw;
        }
    }

    private async UniTask EnsureInitializedAsync(CancellationToken token)
    {
        if (IsInitialized) return;

        if (initializationFailed)
            throw new InvalidOperationException("Naninovel engine failed to initialize.");

        if (!initializationRequested)
        {
            initializationTask = InitializeAsync(token);
            initializationRequested = true;
        }

        await initializationTask;
    }

    private void HandleScriptPlay(Script script)
    {
        currentScriptPath = script?.Path;
        activePlaybackLabel = pendingStartLabel;
        pendingStartLabel = null;
        eventService?.SendMessage(EventID.OnDialogueStart, currentScriptPath, activePlaybackLabel);
        UpdateNaninovelCameraState();
    }

    private void HandleScriptStop(Script script)
    {
        var path = script?.Path ?? currentScriptPath;
        eventService?.SendMessage(EventID.OnDialogueEnd, path, activePlaybackLabel);
        currentScriptPath = null;
        activePlaybackLabel = null;
        UpdateNaninovelCameraState(forceDisable: true);

        if (releaseInputOnStop)
        {
            ReleaseGameplayInputLock();
            releaseInputOnStop = false;
        }
    }

    private void AcquireGameplayInputLock()
    {
        InputRouter.Acquire(InputChannel.Gameplay, this);
    }

    private void ReleaseGameplayInputLock()
    {
        InputRouter.Release(InputChannel.Gameplay, this);
    }

    private void RestoreGameplayInput()
    {
        InputRouter.SetEnabled(InputChannel.Gameplay, true, this);
    }

    private bool UpdateNaninovelCameraState(bool forceDisable = false)
    {
        if (!TryGetNaninovelCamera(out var camera)) return false;

        bool shouldEnable = !forceDisable && allowNaninovelCamera && IsPlaying;
        if (camera.enabled != shouldEnable)
        {
            camera.enabled = shouldEnable;
        }

        return true;
    }

    private bool TryGetNaninovelCamera(out Camera camera)
    {
        camera = null;
        if (Engine.Behaviour == null) return false;

        var manager = Engine.GetService<ICameraManager>();
        if (manager == null) return false;

        camera = manager.Camera;
        return camera != null;
    }
}
#endif
