#if NANINOVEL
using System;
using System.Threading;
using System.Threading.Tasks;
using Levity.Narrative.Core;
using Levity.Narrative.Flow;
using Levity.Narrative.Integration;
using Levity.Narrative.Naninovel;
using Levity.Narrative.Placeholder;
using Levity.Stage;
using UnityEditor;
using UnityEngine;

public sealed class IntegrationWorkspaceWindow : EditorWindow
{
    private readonly StageDescriptor stage =
        new StageDescriptor(new StageId("mission"), "Workspace/Mission");
    private readonly NarrativeSequenceId sequenceId =
        new NarrativeSequenceId("stage.mission.accept");
    private IntegrationWorkspace workspace;
    private PlaceholderNarrativeBackend placeholder;
    private NarrativeFlowNode<BriefingOutcome> flow;
    private IntegrationNarrativeBackend selectedBackend;
    private BriefingOutcome selectedOutcome;
    private string lastResult = "Not played";

    [MenuItem("Levity/Integration Workspace")]
    public static void Open() => GetWindow<IntegrationWorkspaceWindow>("Integration Workspace");

    private void OnEnable() => RebuildWorkspace();

    private void OnGUI()
    {
        if (workspace == null) RebuildWorkspace();

        EditorGUILayout.LabelField("Unchanged integration data", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Stage", stage.Id.Value);
        EditorGUILayout.LabelField("Sequence", flow.SequenceId.Value);

        EditorGUILayout.Space();
        selectedBackend = (IntegrationNarrativeBackend)EditorGUILayout.EnumPopup(
            "Narrative Backend",
            selectedBackend);
        selectedOutcome = (BriefingOutcome)EditorGUILayout.EnumPopup("Equivalent outcome", selectedOutcome);
        if (GUILayout.Button("Run unchanged Flow"))
            Play();
        if (GUILayout.Button("Reset fixture"))
            RebuildWorkspace();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Last branch", lastResult);
    }

    private async void Play()
    {
        try
        {
            workspace.UseBackend(selectedBackend);
            var commands = new GameplayCommandExecutor();
            commands.Register("accept", () => { });
            commands.Register("decline", () => { });
            var play = workspace.PlayAsync(flow, commands);
            if (selectedBackend == IntegrationNarrativeBackend.Placeholder)
                placeholder.SelectOutcome(selectedOutcome);
            var result = await play;
            lastResult = result.Status == IntegrationWorkspacePlayStatus.Completed
                ? result.Flow.BranchId
                : result.Failure.Message;
            Repaint();
        }
        catch (Exception exception)
        {
            lastResult = exception.Message;
        }
    }

    private void RebuildWorkspace()
    {
        placeholder = new PlaceholderNarrativeBackend();
        placeholder.RegisterSequence(sequenceId, BriefingOutcome.Accept, BriefingOutcome.Decline);
        var mappings = new NarrativeSequenceRegistry();
        mappings.Register(sequenceId, new NaninovelSequence("Mission/Briefing"));
        var naninovel = new NaninovelNarrativeBackend(mappings, new EditorNaninovelPlayer(() => selectedOutcome));
        workspace = new IntegrationWorkspace(placeholder, naninovel);
        flow = NarrativeFlowNode<BriefingOutcome>.Create(sequenceId)
            .On(BriefingOutcome.Accept, "accept", new GameplayCommandExecutionId("integration:accept"), "launch")
            .On(BriefingOutcome.Decline, "decline", new GameplayCommandExecutionId("integration:decline"), "menu");
        selectedBackend = IntegrationNarrativeBackend.Placeholder;
        selectedOutcome = BriefingOutcome.Accept;
        lastResult = "Not played";
    }

    private enum BriefingOutcome { Accept, Decline }

    private sealed class EditorNaninovelPlayer : INaninovelPlayer
    {
        private readonly Func<object> outcome;
        public EditorNaninovelPlayer(Func<object> outcome) => this.outcome = outcome;
        public SaveAvailability SaveAvailability => SaveAvailability.Allowed;
        public Task<object> PlayAsync(
            NaninovelPlaybackRequest request,
            CancellationToken cancellationToken) => Task.FromResult(outcome());
    }
}
#endif
