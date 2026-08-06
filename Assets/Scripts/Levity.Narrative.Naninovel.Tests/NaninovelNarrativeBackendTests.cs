using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Levity.Narrative.Core;
using NUnit.Framework;

namespace Levity.Narrative.Naninovel.Tests
{
    public sealed class NaninovelNarrativeBackendTests
    {
        [Test]
        public void StableSequenceIdResolvesToCurrentScriptAndReturnsTypedOutcome()
        {
            var sequenceId = new NarrativeSequenceId("mission-briefing");
            var registry = new NarrativeSequenceRegistry();
            registry.Register(sequenceId, new NaninovelSequence("Mission/Briefing", "choice"));
            var player = new RecordingPlayer("accept");
            INarrativeModule backend = new NaninovelNarrativeBackend(registry, player);

            var result = backend.PlayAsync<string>(new NarrativeRequest(sequenceId))
                .GetAwaiter().GetResult();

            Assert.That(player.Request.ScriptPath, Is.EqualTo("Mission/Briefing"));
            Assert.That(player.Request.EntryPoint, Is.EqualTo("choice"));
            Assert.That(result.Status, Is.EqualTo(NarrativeSessionStatus.Completed));
            Assert.That(result.Outcome, Is.EqualTo("accept"));
        }

        [Test]
        public void LegacyScriptFieldsMapToAStableSequenceWithoutChangingTheCaller()
        {
            var sequenceId = new NarrativeSequenceId("stage-one-intro");
            var registry = new NarrativeSequenceRegistry();
            registry.RegisterLegacy(sequenceId, "Legacy/StageOne", "intro");
            var player = new RecordingPlayer("complete");
            INarrativeModule backend = new NaninovelNarrativeBackend(registry, player);

            var result = backend.PlayAsync<string>(new NarrativeRequest(sequenceId))
                .GetAwaiter().GetResult();

            Assert.That(player.Request.ScriptPath, Is.EqualTo("Legacy/StageOne"));
            Assert.That(player.Request.EntryPoint, Is.EqualTo("intro"));
            Assert.That(result.Outcome, Is.EqualTo("complete"));
        }

        [Test]
        public void UnavailableNaninovelReturnsAnActionableStructuredFailure()
        {
            var sequenceId = new NarrativeSequenceId("required-sequence");
            var registry = new NarrativeSequenceRegistry();
            registry.Register(sequenceId, new NaninovelSequence("Required/Sequence"));
            INarrativeModule backend = new NaninovelNarrativeBackend(
                registry,
                new FailingPlayer(new NaninovelUnavailableException("Naninovel failed to initialize.")));

            var result = backend.PlayAsync<string>(new NarrativeRequest(sequenceId))
                .GetAwaiter().GetResult();

            Assert.That(result.Status, Is.EqualTo(NarrativeSessionStatus.Failed));
            Assert.That(result.Failure.Code, Is.EqualTo(NarrativeFailureCode.BackendUnavailable));
            Assert.That(result.Failure.Message, Does.Contain("failed to initialize"));
        }

        private sealed class RecordingPlayer : INaninovelPlayer
        {
            private readonly object outcome;

            public RecordingPlayer(object outcome) => this.outcome = outcome;

            public NaninovelPlaybackRequest Request { get; private set; }

            public SaveAvailability SaveAvailability => SaveAvailability.Allowed;

            public Task<object> PlayAsync(
                NaninovelPlaybackRequest request,
                CancellationToken cancellationToken)
            {
                Request = request;
                return Task.FromResult(outcome);
            }
        }

        private sealed class FailingPlayer : INaninovelPlayer
        {
            private readonly System.Exception failure;

            public FailingPlayer(System.Exception failure) => this.failure = failure;

            public SaveAvailability SaveAvailability => SaveAvailability.Allowed;

            public Task<object> PlayAsync(
                NaninovelPlaybackRequest request,
                CancellationToken cancellationToken) =>
                Task.FromException<object>(failure);
        }
    }
}
