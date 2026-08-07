#if UNITY_INCLUDE_TESTS
/// <summary>Test-only Manager used by the Stage scope adapter contract.</summary>
public sealed class ScopedManagerTestProbe : ManagerBase
{
    public int AwakeCount { get; private set; }
    public int ExitCount { get; private set; }
    public int UnInitCount { get; private set; }

    public override void OnAwake() => AwakeCount++;

    public override void OnExit() => ExitCount++;

    public override void UnInit() => UnInitCount++;
}
#endif
