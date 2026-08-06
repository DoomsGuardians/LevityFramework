using System;
using Levity.Composition;

/// <summary>
/// Compatibility adapter that keeps existing ILogic implementations on the explicit Composition lifecycle.
/// New modules should implement ICompositionModule directly.
/// </summary>
internal sealed class LogicCompositionModule : ICompositionModule
{
    private readonly ILogic logic;

    public LogicCompositionModule(ILogic logic)
    {
        this.logic = logic ?? throw new ArgumentNullException(nameof(logic));
    }

    public void Initialize(ICompositionServices services) => logic.OnInit();

    public void Start() { }

    public void Shutdown() => logic.UnInit();
}
