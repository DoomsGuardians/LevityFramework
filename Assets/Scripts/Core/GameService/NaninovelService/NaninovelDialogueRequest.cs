// LevityFramework - 通用 Unity 游戏框架
// 核心服务模块 - NaninovelDialogueRequest 对话请求

#if NANINOVEL
/// <summary>
/// Naninovel 对话请求，用于从游戏逻辑触发 AVG 脚本播放
/// </summary>
public readonly struct NaninovelDialogueRequest
{
    public NaninovelDialogueRequest(
        string scriptName,
        string startLabel = null,
        bool waitForCompletion = true,
        bool pauseGameplayInput = true,
        bool stopCurrentPlayback = false,
        int priority = 0,
        object payload = null,
        object param1 = null,
        object param2 = null)
    {
        ScriptName = scriptName;
        StartLabel = startLabel;
        WaitForCompletion = waitForCompletion;
        PauseGameplayInput = pauseGameplayInput;
        StopCurrentPlayback = stopCurrentPlayback;
        Priority = priority;
        Payload = payload;
        Param1 = param1;
        Param2 = param2;
    }

    public string ScriptName { get; }
    public string StartLabel { get; }
    public bool WaitForCompletion { get; }
    public bool PauseGameplayInput { get; }
    public bool StopCurrentPlayback { get; }
    public int Priority { get; }
    public object Payload { get; }
    public object Param1 { get; }
    public object Param2 { get; }

    public bool IsValid => !string.IsNullOrWhiteSpace(ScriptName);
}
#endif
