using System.Linq;
using Levity.Narrative.Core;
using NUnit.Framework;

namespace Levity.Narrative.Workspace.Tests
{
    public sealed class NarrativeWorkspaceTests
    {
        [Test]
        public void RegisteredSequenceCanPlayEveryBranchAgainstInspectableFakeState()
        {
            var sequenceId = new NarrativeSequenceId("mission.briefing");
            var workspace = new NarrativeWorkspace();
            workspace.State.Set("acceptedMissions", 0);
            workspace.Commands.Register<int>("accept-mission", (state, amount) =>
                state.Set("acceptedMissions", state.Get<int>("acceptedMissions") + amount));
            workspace.Commands.Register<string>("decline-mission", (state, reason) =>
                state.Set("lastDeclineReason", reason));
            workspace.Register(
                NarrativeWorkspaceSequence<BriefingOutcome>.Create(sequenceId)
                    .On(BriefingOutcome.Accept, "accept-mission", 1)
                    .On(BriefingOutcome.Decline, "decline-mission", "not-ready"));

            var listed = workspace.Sequences.Single();
            Assert.That(listed.SequenceId, Is.EqualTo(sequenceId));
            Assert.That(listed.OutcomeType, Is.EqualTo(typeof(BriefingOutcome)));
            Assert.That(listed.Outcomes, Is.EquivalentTo(new object[]
            {
                BriefingOutcome.Accept,
                BriefingOutcome.Decline
            }));

            var accepted = workspace.PlayAsync(sequenceId, BriefingOutcome.Accept)
                .GetAwaiter().GetResult();
            var declined = workspace.PlayAsync(sequenceId, BriefingOutcome.Decline)
                .GetAwaiter().GetResult();

            Assert.That(accepted.Status, Is.EqualTo(NarrativeSessionStatus.Completed));
            Assert.That(declined.Status, Is.EqualTo(NarrativeSessionStatus.Completed));
            Assert.That(workspace.State.Get<int>("acceptedMissions"), Is.EqualTo(1));
            Assert.That(workspace.State.Get<string>("lastDeclineReason"), Is.EqualTo("not-ready"));
            Assert.That(workspace.Commands.Invocations.Select(call => call.CommandId),
                Is.EqualTo(new[] { "accept-mission", "decline-mission" }));
            Assert.That(workspace.Commands.Invocations[0].PayloadType, Is.EqualTo(typeof(int)));
            Assert.That(workspace.Commands.Invocations[0].PayloadAs<int>(), Is.EqualTo(1));
            Assert.That(workspace.Commands.Invocations[1].PayloadType, Is.EqualTo(typeof(string)));
        }

        [Test]
        public void UnknownOrWronglyTypedOutcomeFailsExplicitlyWithoutInvokingACommand()
        {
            var sequenceId = new NarrativeSequenceId("mission.briefing");
            var workspace = new NarrativeWorkspace();
            workspace.Commands.Register<int>("accept-mission", (_, __) => { });
            workspace.Register(
                NarrativeWorkspaceSequence<BriefingOutcome>.Create(sequenceId)
                    .On(BriefingOutcome.Accept, "accept-mission", 1));

            var wrongType = workspace.PlayAsync(sequenceId, "Accept").GetAwaiter().GetResult();
            var unknown = workspace.PlayAsync(
                new NarrativeSequenceId("missing.sequence"),
                BriefingOutcome.Accept).GetAwaiter().GetResult();

            Assert.That(wrongType.Status, Is.EqualTo(NarrativeSessionStatus.Failed));
            Assert.That(wrongType.Failure.Code, Is.EqualTo(NarrativeFailureCode.InvalidOutcome));
            Assert.That(unknown.Status, Is.EqualTo(NarrativeSessionStatus.Failed));
            Assert.That(unknown.Failure.Code, Is.EqualTo(NarrativeFailureCode.SequenceNotFound));
            Assert.That(workspace.Commands.Invocations, Is.Empty);
        }

        private enum BriefingOutcome
        {
            Accept,
            Decline
        }
    }
}
