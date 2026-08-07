using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Levity.Narrative.Core;
using NUnit.Framework;

namespace Levity.Narrative.Flow.Tests
{
    public sealed class NarrativeFlowTracerBulletTests
    {
        [Test]
        public void RestoredTracerBulletTakesTypedBranchWithoutRepeatingCommittedGameplayCommand()
        {
            var sequenceId = new NarrativeSequenceId("mission-briefing");
            var executionId = new GameplayCommandExecutionId("mission-briefing:grant-key");
            var sideEffectCount = 0;
            var firstBackend = new CheckpointBackend(sequenceId, BriefingOutcome.Accept);
            var firstCommands = new GameplayCommandExecutor();
            firstCommands.Register("grant-key", () => sideEffectCount++);
            var flow = NarrativeFlowNode<BriefingOutcome>.Create(sequenceId)
                .On(BriefingOutcome.Accept, "grant-key", executionId, "rescue-route");

            var firstResult = flow.PlayAsync(firstBackend, firstCommands).GetAwaiter().GetResult();
            var checkpoint = NarrativeFlowCheckpoint.CaptureAsync(firstBackend, firstCommands)
                .GetAwaiter().GetResult();

            Assert.That(firstResult.BranchId, Is.EqualTo("rescue-route"));
            Assert.That(firstResult.Status, Is.EqualTo(NarrativeSessionStatus.Completed));
            Assert.That(firstResult.Outcome, Is.EqualTo(BriefingOutcome.Accept));
            Assert.That(firstResult.Failure, Is.Null);
            Assert.That(sideEffectCount, Is.EqualTo(1));

            var restoredBackend = new CheckpointBackend(sequenceId, BriefingOutcome.Accept);
            var restoredCommands = new GameplayCommandExecutor();
            restoredCommands.Register("grant-key", () => sideEffectCount++);
            checkpoint.RestoreAsync(restoredBackend, restoredCommands).GetAwaiter().GetResult();

            var restoredResult = flow.PlayAsync(restoredBackend, restoredCommands).GetAwaiter().GetResult();

            Assert.That(restoredBackend.RestoredState, Is.EqualTo("choice:Accept"));
            Assert.That(restoredResult.BranchId, Is.EqualTo("rescue-route"));
            Assert.That(sideEffectCount, Is.EqualTo(1));
        }

        [Test]
        public void CancelledSessionSelectsConfiguredFlowBranch()
        {
            var sequenceId = new NarrativeSequenceId("mission-briefing");
            var flow = NarrativeFlowNode<BriefingOutcome>.Create(sequenceId)
                .OnCancelled("briefing-menu");

            var result = flow.PlayAsync(new CancelledBackend(), new GameplayCommandExecutor())
                .GetAwaiter().GetResult();

            Assert.That(result.Status, Is.EqualTo(NarrativeSessionStatus.Cancelled));
            Assert.That(result.BranchId, Is.EqualTo("briefing-menu"));
            Assert.That(result.Failure, Is.Null);
        }

        [Test]
        public void FailedSessionSelectsBranchConfiguredForItsFailureCode()
        {
            var sequenceId = new NarrativeSequenceId("mission-briefing");
            var failure = new NarrativeFailure(
                NarrativeFailureCode.SequenceNotFound,
                "Missing mission briefing.");
            var flow = NarrativeFlowNode<BriefingOutcome>.Create(sequenceId)
                .OnFailed(NarrativeFailureCode.SequenceNotFound, "missing-content")
                .OnFailed("narrative-error");

            var result = flow.PlayAsync(new FailedBackend(failure), new GameplayCommandExecutor())
                .GetAwaiter().GetResult();

            Assert.That(result.Status, Is.EqualTo(NarrativeSessionStatus.Failed));
            Assert.That(result.BranchId, Is.EqualTo("missing-content"));
            Assert.That(result.Failure, Is.SameAs(failure));
        }

        [Test]
        public void FailedSessionUsesGenericFailureBranchWhenCodeHasNoSpecificRoute()
        {
            var sequenceId = new NarrativeSequenceId("mission-briefing");
            var failure = new NarrativeFailure(
                NarrativeFailureCode.BackendFailure,
                "Backend unavailable.");
            var flow = NarrativeFlowNode<BriefingOutcome>.Create(sequenceId)
                .OnFailed(NarrativeFailureCode.SequenceNotFound, "missing-content")
                .OnFailed("narrative-error");

            var result = flow.PlayAsync(new FailedBackend(failure), new GameplayCommandExecutor())
                .GetAwaiter().GetResult();

            Assert.That(result.Status, Is.EqualTo(NarrativeSessionStatus.Failed));
            Assert.That(result.BranchId, Is.EqualTo("narrative-error"));
            Assert.That(result.Failure, Is.SameAs(failure));
        }

        [Test]
        public void CancelledSessionWithoutConfiguredRouteIsAFlowConfigurationError()
        {
            var sequenceId = new NarrativeSequenceId("mission-briefing");
            var flow = NarrativeFlowNode<BriefingOutcome>.Create(sequenceId);

            var exception = Assert.Throws<NarrativeFlowException>(() =>
                flow.PlayAsync(new CancelledBackend(), new GameplayCommandExecutor())
                    .GetAwaiter().GetResult());

            Assert.That(exception.SequenceId, Is.EqualTo(sequenceId));
            Assert.That(exception.Message, Does.Contain("no Flow branch"));
        }

        [Test]
        public void FailedSessionWithoutConfiguredRouteIsAFlowConfigurationError()
        {
            var sequenceId = new NarrativeSequenceId("mission-briefing");
            var failure = new NarrativeFailure(
                NarrativeFailureCode.BackendFailure,
                "Backend unavailable.");
            var flow = NarrativeFlowNode<BriefingOutcome>.Create(sequenceId);

            var exception = Assert.Throws<NarrativeFlowException>(() =>
                flow.PlayAsync(new FailedBackend(failure), new GameplayCommandExecutor())
                    .GetAwaiter().GetResult());

            Assert.That(exception.SequenceId, Is.EqualTo(sequenceId));
            Assert.That(exception.Failure, Is.SameAs(failure));
        }

        private enum BriefingOutcome
        {
            Accept,
            Decline
        }

        private sealed class CheckpointBackend : INarrativeModule, INarrativeCheckpointStore
        {
            private readonly FakeNarrativeBackend backend = new FakeNarrativeBackend();

            public CheckpointBackend(NarrativeSequenceId sequenceId, BriefingOutcome outcome)
            {
                backend.RegisterSequence(sequenceId, _ => Task.FromResult(outcome));
                State = $"choice:{outcome}";
            }

            public string State { get; private set; }
            public string RestoredState { get; private set; }
            public SaveAvailability SaveAvailability => SaveAvailability.Allowed;

            public Task<NarrativeSessionResult<TOutcome>> PlayAsync<TOutcome>(
                NarrativeRequest request,
                CancellationToken cancellationToken = default) =>
                backend.PlayAsync<TOutcome>(request, cancellationToken);

            public Task<string> CaptureCheckpointAsync(CancellationToken cancellationToken = default) =>
                Task.FromResult(State);

            public Task RestoreCheckpointAsync(
                string checkpoint,
                CancellationToken cancellationToken = default)
            {
                State = checkpoint;
                RestoredState = checkpoint;
                return Task.CompletedTask;
            }
        }

        private sealed class CancelledBackend : INarrativeModule
        {
            public SaveAvailability SaveAvailability => SaveAvailability.Allowed;

            public Task<NarrativeSessionResult<TOutcome>> PlayAsync<TOutcome>(
                NarrativeRequest request,
                CancellationToken cancellationToken = default) =>
                Task.FromResult(NarrativeSessionResult<TOutcome>.Cancelled());
        }

        private sealed class FailedBackend : INarrativeModule
        {
            private readonly NarrativeFailure failure;

            public FailedBackend(NarrativeFailure failure) => this.failure = failure;

            public SaveAvailability SaveAvailability => SaveAvailability.Allowed;

            public Task<NarrativeSessionResult<TOutcome>> PlayAsync<TOutcome>(
                NarrativeRequest request,
                CancellationToken cancellationToken = default) =>
                Task.FromResult(NarrativeSessionResult<TOutcome>.Failed(failure));
        }
    }
}
