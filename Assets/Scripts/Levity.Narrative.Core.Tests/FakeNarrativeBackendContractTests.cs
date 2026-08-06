using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Levity.Narrative.Core.Tests
{
    public sealed class FakeNarrativeBackendContractTests
    {
        [Test]
        public void ConcurrentRequestIsRejectedByDefault()
        {
            var backend = new FakeNarrativeBackend();
            var activeSequence = new NarrativeSequenceId("active-sequence");
            var nextSequence = new NarrativeSequenceId("next-sequence");
            var activeCompletion = new TaskCompletionSource<string>();
            backend.RegisterSequence(activeSequence, _ => activeCompletion.Task);
            backend.RegisterSequence(nextSequence, _ => Task.FromResult("next-outcome"));

            var activeSession = backend.PlayAsync<string>(new NarrativeRequest(activeSequence));
            var rejectedSession = backend.PlayAsync<string>(new NarrativeRequest(nextSequence))
                .GetAwaiter().GetResult();

            Assert.That(rejectedSession.Status, Is.EqualTo(NarrativeSessionStatus.Failed));
            Assert.That(rejectedSession.Failure.Code, Is.EqualTo(NarrativeFailureCode.ConcurrentSession));

            activeCompletion.SetResult("active-outcome");
            Assert.That(activeSession.GetAwaiter().GetResult().Outcome, Is.EqualTo("active-outcome"));
        }

        [Test]
        public void WaitPolicyStartsAfterTheActiveSessionCompletes()
        {
            var backend = new FakeNarrativeBackend();
            var activeSequence = new NarrativeSequenceId("active-sequence");
            var waitingSequence = new NarrativeSequenceId("waiting-sequence");
            var activeCompletion = new TaskCompletionSource<string>();
            backend.RegisterSequence(activeSequence, _ => activeCompletion.Task);
            backend.RegisterSequence(waitingSequence, _ => Task.FromResult("waiting-outcome"));

            var activeSession = backend.PlayAsync<string>(new NarrativeRequest(activeSequence));
            var waitingSession = backend.PlayAsync<string>(new NarrativeRequest(
                waitingSequence,
                concurrentPolicy: ConcurrentRequestPolicy.Wait));

            Assert.That(waitingSession.IsCompleted, Is.False);
            activeCompletion.SetResult("active-outcome");

            Assert.That(activeSession.GetAwaiter().GetResult().Status, Is.EqualTo(NarrativeSessionStatus.Completed));
            Assert.That(waitingSession.GetAwaiter().GetResult().Outcome, Is.EqualTo("waiting-outcome"));
        }

        [Test]
        public void ReplacePolicyCancelsTheActiveSessionAndStartsTheReplacement()
        {
            var backend = new FakeNarrativeBackend();
            var activeSequence = new NarrativeSequenceId("active-sequence");
            var replacementSequence = new NarrativeSequenceId("replacement-sequence");
            backend.RegisterSequence<string>(activeSequence, cancellationToken =>
                Task.Delay(Timeout.Infinite, cancellationToken).ContinueWith(
                    _ => "unreachable",
                    cancellationToken));
            backend.RegisterSequence(replacementSequence, _ => Task.FromResult("replacement-outcome"));

            var activeSession = backend.PlayAsync<string>(new NarrativeRequest(activeSequence));
            var replacementSession = backend.PlayAsync<string>(new NarrativeRequest(
                replacementSequence,
                concurrentPolicy: ConcurrentRequestPolicy.Replace));

            Assert.That(replacementSession.GetAwaiter().GetResult().Outcome, Is.EqualTo("replacement-outcome"));
            Assert.That(activeSession.GetAwaiter().GetResult().Status, Is.EqualTo(NarrativeSessionStatus.Cancelled));
        }

        [Test]
        public void CancelPolicyCancelsTheNewRequestWithoutInterruptingTheActiveSession()
        {
            var backend = new FakeNarrativeBackend();
            var activeSequence = new NarrativeSequenceId("active-sequence");
            var cancelledSequence = new NarrativeSequenceId("cancelled-sequence");
            var activeCompletion = new TaskCompletionSource<string>();
            backend.RegisterSequence(activeSequence, _ => activeCompletion.Task);
            backend.RegisterSequence(cancelledSequence, _ => Task.FromResult("must-not-run"));

            var activeSession = backend.PlayAsync<string>(new NarrativeRequest(activeSequence));
            var cancelledSession = backend.PlayAsync<string>(new NarrativeRequest(
                cancelledSequence,
                concurrentPolicy: ConcurrentRequestPolicy.Cancel)).GetAwaiter().GetResult();

            Assert.That(cancelledSession.Status, Is.EqualTo(NarrativeSessionStatus.Cancelled));
            Assert.That(activeSession.IsCompleted, Is.False);

            activeCompletion.SetResult("active-outcome");
            Assert.That(activeSession.GetAwaiter().GetResult().Status, Is.EqualTo(NarrativeSessionStatus.Completed));
        }

        [Test]
        public void CancellationTokenReturnsACancelledResult()
        {
            var backend = new FakeNarrativeBackend();
            var sequence = new NarrativeSequenceId("cancellable-sequence");
            backend.RegisterSequence<string>(sequence, cancellationToken =>
                Task.Delay(Timeout.Infinite, cancellationToken).ContinueWith(
                    _ => "unreachable",
                    cancellationToken));
            var cancellation = new CancellationTokenSource();

            var session = backend.PlayAsync<string>(new NarrativeRequest(sequence), cancellation.Token);
            cancellation.Cancel();

            Assert.That(session.GetAwaiter().GetResult().Status, Is.EqualTo(NarrativeSessionStatus.Cancelled));
        }

        [Test]
        public void BackendExceptionReturnsATypedFailure()
        {
            var backend = new FakeNarrativeBackend();
            var sequence = new NarrativeSequenceId("failing-sequence");
            backend.RegisterSequence<string>(sequence, _ =>
                Task.FromException<string>(new InvalidOperationException("backend broke")));

            var result = backend.PlayAsync<string>(new NarrativeRequest(sequence)).GetAwaiter().GetResult();

            Assert.That(result.Status, Is.EqualTo(NarrativeSessionStatus.Failed));
            Assert.That(result.Failure.Code, Is.EqualTo(NarrativeFailureCode.BackendFailure));
            Assert.That(result.Failure.Exception, Is.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void UnknownSequenceReturnsATypedFailureInsteadOfCompletingSilently()
        {
            var backend = new FakeNarrativeBackend();

            var result = backend.PlayAsync<string>(
                new NarrativeRequest(new NarrativeSequenceId("missing-sequence"))).GetAwaiter().GetResult();

            Assert.That(result.Status, Is.EqualTo(NarrativeSessionStatus.Failed));
            Assert.That(result.Failure.Code, Is.EqualTo(NarrativeFailureCode.SequenceNotFound));
        }

        [Test]
        public void FakeBackendAllowsSavingByDefault()
        {
            INarrativeModule backend = new FakeNarrativeBackend();

            Assert.That(backend.SaveAvailability.CanSave, Is.True);
            Assert.That(backend.SaveAvailability.BlockedReason, Is.Null);
        }
    }
}
