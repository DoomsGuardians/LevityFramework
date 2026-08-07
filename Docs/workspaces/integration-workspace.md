# Integration Workspace

## Status

Supported authoring workspace (Slice C). It runs one unchanged Game Flow against either the Placeholder or Naninovel Narrative Backend while preserving the same stable Narrative Sequence ID.

## Open the workspace

Open the Unity project and choose **Levity > Integration Workspace**. The fixture displays its fixed Stage ID and Sequence ID. Choose a backend and equivalent typed outcome, then choose **Run unchanged Flow**. Both backends select the same game-owned branch for the same outcome.

Backend selection is workspace state, not Stage or Flow data. `UseBackend` changes only which registered Narrative Backend receives the next request. The `NarrativeFlowNode`, its `SequenceId`, Gameplay Command IDs, execution IDs, and branch IDs are reused unchanged:

```csharp
var workspace = new IntegrationWorkspace(placeholderBackend, naninovelBackend);

workspace.UseBackend(IntegrationNarrativeBackend.Placeholder);
var placeholderRun = workspace.PlayAsync(flow, placeholderCommands);
placeholderBackend.SelectOutcome("accept");
var placeholderResult = await placeholderRun;

workspace.UseBackend(IntegrationNarrativeBackend.Naninovel);
var naninovelResult = await workspace.PlayAsync(flow, naninovelCommands);
```

Both production backends implement `INarrativeSequenceMapping`. Before starting a Narrative Session, the workspace validates that the selected backend maps `flow.SequenceId`. A missing mapping returns `IntegrationWorkspacePlayStatus.ValidationFailed` with `MissingBackendMapping`, the selected backend, and the missing Sequence ID. Narrative playback and Gameplay Commands do not start after validation failure.
