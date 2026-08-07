using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Levity.UnifiedSave.Tests
{
    public sealed class UnifiedSaveAtomicityTests
    {
        [Test]
        public void FailedContributorPreservesThePreviousCompleteSlot()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"levity-unified-save-{Guid.NewGuid():N}");
            try
            {
                var store = new FileUnifiedSaveStore(directory);
                var gameplay = new StateContributor("gameplay", 1, "level:1");
                var narrative = new StateContributor("narrative", 2, "choice:accept");
                var settings = new StateContributor("settings", 1, "language:en");
                var commands = new StateContributor("commands", 1, "mission:grant-key");
                var save = new UnifiedSave(store, gameplay, narrative, settings, commands);
                save.SaveAsync("slot-1").GetAwaiter().GetResult();

                gameplay.State = "level:2";
                narrative.State = "choice:decline";
                settings.FailCapture = true;
                commands.State = "mission:grant-key,mission:award";

                Assert.Throws<UnifiedSaveException>(() =>
                    save.SaveAsync("slot-1").GetAwaiter().GetResult());

                var restoredGameplay = new StateContributor("gameplay", 1, null);
                var restoredNarrative = new StateContributor("narrative", 2, null);
                var restoredSettings = new StateContributor("settings", 1, null);
                var restoredCommands = new StateContributor("commands", 1, null);
                var restore = new UnifiedSave(
                    store,
                    restoredGameplay,
                    restoredNarrative,
                    restoredSettings,
                    restoredCommands);
                var loadResult = restore.TryLoadAsync("slot-1").GetAwaiter().GetResult();

                Assert.That(loadResult.Status, Is.EqualTo(UnifiedLoadStatus.Loaded));
                Assert.That(restoredGameplay.State, Is.EqualTo("level:1"));
                Assert.That(restoredNarrative.State, Is.EqualTo("choice:accept"));
                Assert.That(restoredSettings.State, Is.EqualTo("language:en"));
                Assert.That(restoredCommands.State, Is.EqualTo("mission:grant-key"));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Test]
        public void FailedRestoreRollsBackPreviouslyMutatedContributors()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"levity-unified-load-{Guid.NewGuid():N}");
            try
            {
                var store = new FileUnifiedSaveStore(directory);
                var first = new StateContributor("a-first", 1, "saved:first");
                var second = new StateContributor("b-second", 1, "saved:second");
                var save = new UnifiedSave(store, first, second);
                save.SaveAsync("slot-1").GetAwaiter().GetResult();

                first.State = "current:first";
                second.State = "current:second";
                second.FailRestoreState = "saved:second";

                var result = save.TryLoadAsync("slot-1").GetAwaiter().GetResult();
                Assert.That(result.Status, Is.EqualTo(UnifiedLoadStatus.FailedRolledBack));
                Assert.That(first.State, Is.EqualTo("current:first"));
                Assert.That(second.State, Is.EqualTo("current:second"));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Test]
        public void RollbackFailureIsCallerVisibleAndOtherContributorsStillRollBack()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"levity-unified-rollback-{Guid.NewGuid():N}");
            try
            {
                var store = new FileUnifiedSaveStore(directory);
                var first = new StateContributor("a-first", 1, "saved:first");
                var second = new StateContributor("b-second", 1, "saved:second");
                var save = new UnifiedSave(store, first, second);
                save.SaveAsync("slot-1").GetAwaiter().GetResult();

                first.State = "current:first";
                second.State = "current:second";
                first.FailRestoreState = "current:first";
                second.FailRestoreState = "saved:second";

                var result = save.TryLoadAsync("slot-1").GetAwaiter().GetResult();
                Assert.That(result.Status, Is.EqualTo(UnifiedLoadStatus.FailedRollback));
                Assert.That(result.Failure, Is.Not.Null);
                Assert.That(result.RollbackFailure, Is.Not.Null);
                Assert.That(first.State, Is.EqualTo("saved:first"));
                Assert.That(second.State, Is.EqualTo("current:second"));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        private sealed class StateContributor : IUnifiedSaveContributor
        {
            public StateContributor(string id, int version, string state)
            {
                Id = id;
                Version = version;
                State = state;
            }

            public string Id { get; }
            public int Version { get; }
            public string State { get; set; }
            public bool FailCapture { get; set; }
            public string FailRestoreState { get; set; }

            public Task<string> CaptureAsync(CancellationToken cancellationToken = default)
            {
                if (FailCapture) throw new InvalidOperationException($"{Id} capture failed");
                return Task.FromResult(State);
            }

            public Task RestoreAsync(
                int version,
                string state,
                CancellationToken cancellationToken = default)
            {
                if (state == FailRestoreState) throw new InvalidOperationException($"{Id} restore failed");
                State = state;
                return Task.CompletedTask;
            }
        }
    }
}
