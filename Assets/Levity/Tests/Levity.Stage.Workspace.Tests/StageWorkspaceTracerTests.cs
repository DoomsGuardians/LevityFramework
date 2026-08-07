using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Levity.Narrative.Core;
using Levity.Narrative.Flow;
using Levity.Narrative.Placeholder;
using NUnit.Framework;

namespace Levity.Stage.Workspace.Tests
{
    public sealed class StageWorkspaceTracerTests
    {
        [Test]
        public void SelectedStageLoadsTransactionallyAndEveryPlaceholderOutcomeCanDriveItsFlow()
        {
            var menu = new StageDescriptor(new StageId("menu"), "Scenes/Menu");
            var mission = new StageDescriptor(new StageId("mission"), "Scenes/Mission");
            var registry = new StageRegistry();
            registry.Register(menu);
            registry.Register(mission);
            var loader = new WorkspaceLoader();
            var narrative = new PlaceholderNarrativeBackend();
            var sequenceId = new NarrativeSequenceId("mission.briefing");
            narrative.RegisterSequence(sequenceId, BriefingOutcome.Accept, BriefingOutcome.Decline);
            var workspace = new StageWorkspace(registry, loader, narrative);
            var flow = NarrativeFlowNode<BriefingOutcome>.Create(sequenceId)
                .On(BriefingOutcome.Accept, "accept", new GameplayCommandExecutionId("accept:1"), "launch")
                .On(BriefingOutcome.Decline, "decline", new GameplayCommandExecutionId("decline:1"), "menu");
            var commands = new GameplayCommandExecutor();
            commands.Register("accept", () => { });
            commands.Register("decline", () => { });

            Assert.That(workspace.Stages.Select(stage => stage.Id),
                Is.EqualTo(new[] { menu.Id, mission.Id }));
            Assert.That(narrative.Sequences.Single().Outcomes,
                Is.EquivalentTo(new object[] { BriefingOutcome.Accept, BriefingOutcome.Decline }));

            var acceptedPlay = workspace.PlayAsync(mission.Id, flow, commands);
            Assert.That(narrative.ActiveSequence.SequenceId, Is.EqualTo(sequenceId));
            narrative.SelectOutcome(BriefingOutcome.Accept);
            var accepted = acceptedPlay.GetAwaiter().GetResult();

            Assert.That(accepted.StageChange.Status, Is.EqualTo(StageChangeStatus.Completed));
            Assert.That(accepted.Flow.BranchId, Is.EqualTo("launch"));
            Assert.That(workspace.CurrentStage.Id, Is.EqualTo(mission.Id));
            Assert.That(loader.PreparedStageIds, Is.EqualTo(new[] { mission.Id }));

            var declinedPlay = workspace.PlayAsync(menu.Id, flow, commands);
            Assert.That(narrative.ActiveSequence.SequenceId, Is.EqualTo(sequenceId));
            narrative.SelectOutcome(BriefingOutcome.Decline);
            var declined = declinedPlay.GetAwaiter().GetResult();

            Assert.That(declined.StageChange.Status, Is.EqualTo(StageChangeStatus.Completed));
            Assert.That(declined.Flow.BranchId, Is.EqualTo("menu"));
            Assert.That(workspace.CurrentStage.Id, Is.EqualTo(menu.Id));
            Assert.That(loader.PreparedStageIds, Is.EqualTo(new[] { mission.Id, menu.Id }));
        }

        private enum BriefingOutcome { Accept, Decline }

        private sealed class WorkspaceLoader : IStageLoader
        {
            private readonly System.Collections.Generic.List<StageId> prepared =
                new System.Collections.Generic.List<StageId>();

            public System.Collections.Generic.IReadOnlyList<StageId> PreparedStageIds => prepared;
            public StageLoadValidation Validate(StageDescriptor target) => StageLoadValidation.Valid();

            public Task<IStageHandle> PrepareAsync(
                StageDescriptor target,
                CancellationToken cancellationToken)
            {
                prepared.Add(target.Id);
                return Task.FromResult<IStageHandle>(new WorkspaceHandle(target));
            }
        }

        private sealed class WorkspaceHandle : IStageHandle
        {
            public WorkspaceHandle(StageDescriptor descriptor)
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
}
