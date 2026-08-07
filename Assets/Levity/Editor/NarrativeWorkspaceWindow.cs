using System;
using System.Linq;
using Levity.Narrative.Core;
using Levity.Narrative.Workspace;
using UnityEditor;
using UnityEngine;

public sealed class NarrativeWorkspaceWindow : EditorWindow
{
    private NarrativeWorkspace workspace;
    private int sequenceIndex;
    private int outcomeIndex;
    private int acceptedMissions;
    private string declineReason = "not-ready";
    private string lastResult = "Not played";

    [MenuItem("Levity/Narrative Workspace")]
    public static void Open() => GetWindow<NarrativeWorkspaceWindow>("Narrative Workspace");

    private void OnEnable() => RebuildWorkspace();

    private void OnGUI()
    {
        if (workspace == null) RebuildWorkspace();

        EditorGUILayout.LabelField("Fake Game State", EditorStyles.boldLabel);
        acceptedMissions = EditorGUILayout.IntField("Accepted missions", acceptedMissions);
        declineReason = EditorGUILayout.TextField("Decline reason", declineReason);
        workspace.State.Set("acceptedMissions", acceptedMissions);
        workspace.State.Set("declineReason", declineReason);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Registered Sequences", EditorStyles.boldLabel);
        var sequences = workspace.Sequences;
        sequenceIndex = EditorGUILayout.Popup(
            "Sequence",
            sequenceIndex,
            sequences.Select(item => item.SequenceId.Value).ToArray());
        var selected = sequences[sequenceIndex];
        outcomeIndex = EditorGUILayout.Popup(
            "Outcome",
            Math.Min(outcomeIndex, selected.Outcomes.Count - 1),
            selected.Outcomes.Select(value => value.ToString()).ToArray());

        if (GUILayout.Button("Play selected route"))
            Play(selected.SequenceId, selected.Outcomes[outcomeIndex]);
        if (GUILayout.Button("Reset fake state and log"))
            RebuildWorkspace();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Last result", lastResult);
        EditorGUILayout.LabelField("Command invocations", EditorStyles.boldLabel);
        foreach (var invocation in workspace.Commands.Invocations)
            EditorGUILayout.LabelField(
                invocation.CommandId,
                $"{invocation.PayloadType.Name}: {invocation.Payload}");
    }

    private async void Play(NarrativeSequenceId sequenceId, object outcome)
    {
        try
        {
            var result = await workspace.PlayAsync(sequenceId, outcome);
            acceptedMissions = workspace.State.Get<int>("acceptedMissions");
            lastResult = result.Status.ToString();
            Repaint();
        }
        catch (Exception exception)
        {
            lastResult = exception.Message;
        }
    }

    private void RebuildWorkspace()
    {
        workspace = new NarrativeWorkspace();
        acceptedMissions = 0;
        declineReason = "not-ready";
        workspace.State.Set("acceptedMissions", acceptedMissions);
        workspace.State.Set("declineReason", declineReason);
        workspace.Commands.Register<int>("accept-mission", (state, amount) =>
            state.Set("acceptedMissions", state.Get<int>("acceptedMissions") + amount));
        workspace.Commands.Register<string>("decline-mission", (state, _) =>
            state.Set("lastDeclineReason", state.Get<string>("declineReason")));
        workspace.Register(
            NarrativeWorkspaceSequence<BriefingOutcome>
                .Create(new NarrativeSequenceId("stage.mission.accept"))
                .On(BriefingOutcome.Accept, "accept-mission", 1)
                .On(BriefingOutcome.Decline, "decline-mission", declineReason));
        sequenceIndex = 0;
        outcomeIndex = 0;
        lastResult = "Not played";
    }

    private enum BriefingOutcome
    {
        Accept,
        Decline
    }
}
