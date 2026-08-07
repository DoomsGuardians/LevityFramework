using System.Threading;
using System.Threading.Tasks;
using Levity.Narrative.Core;
using Levity.Narrative.Flow;
using Levity.Narrative.Naninovel;
using Levity.Narrative.Placeholder;
using NUnit.Framework;

namespace Levity.Narrative.Integration.Tests
{
    public sealed class IntegrationWorkspaceTests
    {
        [Test]
        public void SameFlowSelectsSameBranchWhenWorkspaceSwitchesNarrativeBackend()
        {
            var sequenceId = new NarrativeSequenceId("mission.briefing");
            var placeholder = new PlaceholderNarrativeBackend();
            placeholder.RegisterSequence(sequenceId, BriefingOutcome.Accept, BriefingOutcome.Decline);
            var naninovelMappings = new NarrativeSequenceRegistry();
            naninovelMappings.Register(sequenceId, new NaninovelSequence("Mission/Briefing"));
            var naninovel = new NaninovelNarrativeBackend(
                naninovelMappings,
                new OutcomePlayer(BriefingOutcome.Accept));
            var workspace = new IntegrationWorkspace(placeholder, naninovel);
            var flow = NarrativeFlowNode<BriefingOutcome>.Create(sequenceId)
                .On(BriefingOutcome.Accept, "accept", new GameplayCommandExecutionId("accept:1"), "launch")
                .On(BriefingOutcome.Decline, "decline", new GameplayCommandExecutionId("decline:1"), "menu");

            workspace.UseBackend(IntegrationNarrativeBackend.Placeholder);
            var placeholderPlay = workspace.PlayAsync(flow, CreateCommands());
            placeholder.SelectOutcome(BriefingOutcome.Accept);
            var placeholderResult = placeholderPlay.GetAwaiter().GetResult();

            workspace.UseBackend(IntegrationNarrativeBackend.Naninovel);
            var naninovelResult = workspace.PlayAsync(flow, CreateCommands()).GetAwaiter().GetResult();

            Assert.That(placeholderResult.Flow.BranchId, Is.EqualTo("launch"));
            Assert.That(naninovelResult.Flow.BranchId, Is.EqualTo("launch"));
            Assert.That(placeholderResult.Flow.Outcome, Is.EqualTo(BriefingOutcome.Accept));
            Assert.That(naninovelResult.Flow.Outcome, Is.EqualTo(BriefingOutcome.Accept));
            Assert.That(flow.SequenceId, Is.EqualTo(sequenceId));
        }

        [Test]
        public void MissingSelectedBackendMappingFailsValidationBeforeNarrativeStarts()
        {
            var sequenceId = new NarrativeSequenceId("mission.briefing");
            var placeholder = new PlaceholderNarrativeBackend();
            placeholder.RegisterSequence(sequenceId, BriefingOutcome.Accept);
            var player = new OutcomePlayer(BriefingOutcome.Accept);
            var naninovel = new NaninovelNarrativeBackend(new NarrativeSequenceRegistry(), player);
            var workspace = new IntegrationWorkspace(placeholder, naninovel);
            workspace.UseBackend(IntegrationNarrativeBackend.Naninovel);
            var flow = NarrativeFlowNode<BriefingOutcome>.Create(sequenceId)
                .On(BriefingOutcome.Accept, "accept", new GameplayCommandExecutionId("accept:missing"), "launch");

            var result = workspace.PlayAsync(flow, CreateCommands()).GetAwaiter().GetResult();

            Assert.That(result.Status, Is.EqualTo(IntegrationWorkspacePlayStatus.ValidationFailed));
            Assert.That(result.FlowStarted, Is.False);
            Assert.That(result.Failure.Code, Is.EqualTo(IntegrationValidationFailureCode.MissingBackendMapping));
            Assert.That(result.Failure.Backend, Is.EqualTo(IntegrationNarrativeBackend.Naninovel));
            Assert.That(result.Failure.SequenceId, Is.EqualTo(sequenceId));
            Assert.That(player.PlayCount, Is.Zero);
        }

        private static GameplayCommandExecutor CreateCommands()
        {
            var commands = new GameplayCommandExecutor();
            commands.Register("accept", () => { });
            commands.Register("decline", () => { });
            return commands;
        }

        private enum BriefingOutcome { Accept, Decline }

        private sealed class OutcomePlayer : INaninovelPlayer
        {
            private readonly object outcome;
            public OutcomePlayer(object outcome) => this.outcome = outcome;
            public int PlayCount { get; private set; }
            public SaveAvailability SaveAvailability => SaveAvailability.Allowed;
            public Task<object> PlayAsync(
                NaninovelPlaybackRequest request,
                CancellationToken cancellationToken)
            {
                PlayCount++;
                return Task.FromResult(outcome);
            }
        }
    }
}
