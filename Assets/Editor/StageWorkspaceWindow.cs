using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Levity.Narrative.Core;
using Levity.Narrative.Flow;
using Levity.Narrative.Placeholder;
using Levity.Stage;
using Levity.Stage.Workspace;
using UnityEditor;
using UnityEngine;

public sealed class StageWorkspaceWindow : EditorWindow
{
    private StageWorkspace workspace;
    private NarrativeFlowNode<BriefingOutcome> flow;
    private GameplayCommandExecutor commands;
    private int stageIndex;
    private int outcomeIndex;
    private string lastResult = "Not played";

    [MenuItem("Levity/Stage Workspace")]
    public static void Open() => GetWindow<StageWorkspaceWindow>("Stage Workspace");

    private void OnEnable() => RebuildWorkspace();

    private void OnGUI()
    {
        if (workspace == null) RebuildWorkspace();

        EditorGUILayout.LabelField("Registered Stages", EditorStyles.boldLabel);
        stageIndex = EditorGUILayout.Popup(
            "Stage",
            stageIndex,
            workspace.Stages.Select(stage => stage.Id.Value).ToArray());

        var sequence = workspace.Narrative.Sequences.Single();
        outcomeIndex = EditorGUILayout.Popup(
            "Placeholder outcome",
            outcomeIndex,
            sequence.Outcomes.Select(outcome => outcome.ToString()).ToArray());

        if (GUILayout.Button("Load Stage and play Flow"))
            Play(workspace.Stages[stageIndex].Id, sequence.Outcomes[outcomeIndex]);
        if (GUILayout.Button("Reset fixture"))
            RebuildWorkspace();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Current Stage", workspace.CurrentStage?.Id.Value ?? "none");
        EditorGUILayout.LabelField("Last Flow branch", lastResult);
    }

    private async void Play(StageId stageId, object outcome)
    {
        try
        {
            var play = workspace.PlayAsync(stageId, flow, commands);
            workspace.Narrative.SelectOutcome(outcome);
            var result = await play;
            lastResult = result.FlowStarted
                ? result.Flow.BranchId
                : result.StageChange.Failure?.Message ?? result.StageChange.Status.ToString();
            Repaint();
        }
        catch (Exception exception)
        {
            lastResult = exception.Message;
        }
    }

    private void RebuildWorkspace()
    {
        var registry = new StageRegistry();
        registry.Register(new StageDescriptor(new StageId("menu"), "Workspace/Menu"));
        registry.Register(new StageDescriptor(new StageId("mission"), "Workspace/Mission"));
        var narrative = new PlaceholderNarrativeBackend();
        var sequenceId = new NarrativeSequenceId("stage.mission.accept");
        narrative.RegisterSequence(sequenceId, BriefingOutcome.Accept, BriefingOutcome.Decline);
        workspace = new StageWorkspace(registry, new EditorStageLoader(), narrative);
        commands = new GameplayCommandExecutor();
        commands.Register("accept", () => { });
        commands.Register("decline", () => { });
        flow = NarrativeFlowNode<BriefingOutcome>.Create(sequenceId)
            .On(BriefingOutcome.Accept, "accept", new GameplayCommandExecutionId("workspace:accept"), "launch")
            .On(BriefingOutcome.Decline, "decline", new GameplayCommandExecutionId("workspace:decline"), "menu");
        stageIndex = 0;
        outcomeIndex = 0;
        lastResult = "Not played";
    }

    private enum BriefingOutcome { Accept, Decline }

    private sealed class EditorStageLoader : IStageLoader
    {
        public StageLoadValidation Validate(StageDescriptor target) => StageLoadValidation.Valid();

        public Task<IStageHandle> PrepareAsync(
            StageDescriptor target,
            CancellationToken cancellationToken) =>
            Task.FromResult<IStageHandle>(new EditorStageHandle(target));
    }

    private sealed class EditorStageHandle : IStageHandle
    {
        public EditorStageHandle(StageDescriptor descriptor)
        {
            Descriptor = descriptor;
            Scope = new StageScope(descriptor.Id);
        }

        public StageDescriptor Descriptor { get; }
        public StageScope Scope { get; }
        public Task ActivateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ReleaseAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
