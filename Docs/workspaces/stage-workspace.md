# Stage Workspace

## Status

Supported authoring workspace (Slice B). It loads registered Stages through `StageConductor`, then runs their Game Flow with the production `PlaceholderNarrativeBackend`. It has no Naninovel dependency.

## Open the workspace

From a clean checkout, open the Unity project and choose **Levity > Stage Workspace**. The included fixture registers menu and mission Stage descriptors. Select either Stage and either advertised `BriefingOutcome`, then choose **Load Stage and play Flow**. The window reports the committed Stage and selected Flow branch.

Game-owned fixtures use the same pure C# interface:

```csharp
var registry = new StageRegistry();
registry.Register(new StageDescriptor(new StageId("mission"), "Scenes/Mission"));

var placeholder = new PlaceholderNarrativeBackend();
var sequenceId = new NarrativeSequenceId("mission.briefing");
placeholder.RegisterSequence(sequenceId, "accept", "decline");

var workspace = new StageWorkspace(registry, stageLoader, placeholder);
var play = workspace.PlayAsync(new StageId("mission"), flow, commands);
placeholder.SelectOutcome("accept");
var result = await play;
```

`workspace.Stages` preserves registry order for operator selection. A Stage change must complete before its Flow starts; validation, preparation, activation, rollback, and single-flight rejection retain the normal `StageConductor` behavior. `placeholder.Sequences` exposes the stable Narrative Sequence ID, outcome type, and possible typed values. While a sequence is active, `placeholder.ActiveSequence` identifies it and `SelectOutcome` completes it.

The placeholder backend wraps `FakeNarrativeBackend`, so Sequence-not-found, cancellation, incompatible outcome, and concurrent-session behavior continue to use the Narrative Core contract. It adds only the authoring interaction: advertising possible outcomes and waiting for the operator's selection.
