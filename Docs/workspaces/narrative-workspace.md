# Narrative Workspace

## Status

Supported authoring workspace (Slice A). It exercises Narrative Core with fake game state and fake Gameplay Commands. It does not load a production Stage, Game Flow asset, or Naninovel.

## Open the workspace

From a clean checkout, open the Unity project and choose **Levity > Narrative Workspace**. The included `stage.mission.accept` route demonstrates both typed outcomes. Select an outcome and choose **Play selected route**; the window shows the resulting fake state and the typed command invocation log.

The editor window is a thin example adapter. Game-owned workspace routes use the pure C# `Levity.Narrative.Workspace` module directly:

```csharp
var workspace = new NarrativeWorkspace();
workspace.State.Set("credits", 0);
workspace.Commands.Register<int>("grant-credits", (state, amount) =>
    state.Set("credits", state.Get<int>("credits") + amount));

var sequenceId = new NarrativeSequenceId("briefing.reward");
workspace.Register(
    NarrativeWorkspaceSequence<string>.Create(sequenceId)
        .On("accept", "grant-credits", 100));

var result = await workspace.PlayAsync(sequenceId, "accept");
```

`workspace.Sequences` is the operator-facing catalog. Each descriptor exposes its stable Sequence ID, outcome type, and all registered outcome values. `workspace.Commands.Invocations` records the command ID, payload type, and payload after each successful handler call. Tests and custom tooling use the same interface as the editor adapter.

Registration and execution errors are explicit: duplicate Sequence IDs throw without replacing the original route, while unknown Sequence IDs and invalid outcomes return typed narrative failures. Fake state is deliberately local to one workspace instance so authoring runs cannot touch production game state.
