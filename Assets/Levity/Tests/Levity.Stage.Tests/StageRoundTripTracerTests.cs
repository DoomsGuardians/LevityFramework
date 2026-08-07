using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Levity.Narrative.Core;
using Levity.Narrative.Flow;
using Levity.UnifiedSave;
using NUnit.Framework;

namespace Levity.Stage.Tests
{
    public sealed class StageRoundTripTracerTests
    {
        [Test]
        public void MenuMissionSaveMenuLoadRestoresNarrativeWithoutRepeatingCommand()
        {
            var menu = new StageDescriptor(new StageId("menu"), "Scenes/Menu");
            var mission = new StageDescriptor(new StageId("mission"), "Scenes/Mission");
            var registry = new StageRegistry();
            registry.Register(menu);
            registry.Register(mission);
            var menuHandle = new TracerStageHandle(menu);
            var missionHandle = new TracerStageHandle(mission);
            var returnedMenuHandle = new TracerStageHandle(menu);
            var loader = new TracerStageLoader(missionHandle, returnedMenuHandle);
            var conductor = new StageConductor(registry, loader, menuHandle);
            var missionManagerLease = missionHandle.Scope.Register(
                new MissionManager(),
                _ => Task.CompletedTask);
            var sequenceId = new NarrativeSequenceId("mission-briefing");
            var executionId = new GameplayCommandExecutionId("mission-briefing:grant-key");
            var effectCount = 0;
            var backend = new CheckpointBackend(sequenceId, MissionOutcome.Accept);
            var commands = new GameplayCommandExecutor();
            commands.Register("grant-key", () => effectCount++);
            var flow = NarrativeFlowNode<MissionOutcome>.Create(sequenceId)
                .On(MissionOutcome.Accept, "grant-key", executionId, "mission-route");
            var store = new MemoryStore();
            var save = new UnifiedSave.UnifiedSave(
                store,
                backend,
                new CommandContributor(commands));

            Assert.That(conductor.ChangeAsync(mission.Id).GetAwaiter().GetResult().Status,
                Is.EqualTo(StageChangeStatus.Completed));
            Assert.That(flow.PlayAsync(backend, commands).GetAwaiter().GetResult().BranchId,
                Is.EqualTo("mission-route"));
            Assert.That(save.TrySaveAsync("round-trip").GetAwaiter().GetResult().Status,
                Is.EqualTo(UnifiedSaveStatus.Saved));
            Assert.That(conductor.ChangeAsync(menu.Id).GetAwaiter().GetResult().Status,
                Is.EqualTo(StageChangeStatus.Completed));
            Assert.Throws<ReleasedStageManagerAccessException>(() => _ = missionManagerLease.Value);

            var restoredBackend = new CheckpointBackend(sequenceId, MissionOutcome.Accept, "empty");
            var restoredCommands = new GameplayCommandExecutor();
            restoredCommands.Register("grant-key", () => effectCount++);
            var restoredSave = new UnifiedSave.UnifiedSave(
                store,
                restoredBackend,
                new CommandContributor(restoredCommands));

            Assert.That(restoredSave.TryLoadAsync("round-trip").GetAwaiter().GetResult().Status,
                Is.EqualTo(UnifiedLoadStatus.Loaded));
            Assert.That(restoredBackend.RestoredState, Is.EqualTo("choice:Accept"));
            Assert.That(flow.PlayAsync(restoredBackend, restoredCommands).GetAwaiter().GetResult().BranchId,
                Is.EqualTo("mission-route"));
            Assert.That(effectCount, Is.EqualTo(1));
        }

        private enum MissionOutcome { Accept }
        private sealed class MissionManager { }

        private sealed class TracerStageLoader : IStageLoader
        {
            private readonly Queue<IStageHandle> handles;
            public TracerStageLoader(params IStageHandle[] handles) =>
                this.handles = new Queue<IStageHandle>(handles);
            public StageLoadValidation Validate(StageDescriptor descriptor) =>
                StageLoadValidation.Valid();
            public Task<IStageHandle> PrepareAsync(StageDescriptor descriptor, CancellationToken cancellationToken) =>
                Task.FromResult(handles.Dequeue());
        }

        private sealed class TracerStageHandle : IStageHandle
        {
            public TracerStageHandle(StageDescriptor descriptor)
            {
                Descriptor = descriptor;
                Scope = new StageScope(descriptor.Id);
            }
            public StageDescriptor Descriptor { get; }
            public StageScope Scope { get; }
            public Task ActivateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task ReactivateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task ReleaseAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private sealed class CheckpointBackend : INarrativeModule, INarrativeCheckpointStore, IUnifiedSaveContributor
        {
            private readonly FakeNarrativeBackend backend = new FakeNarrativeBackend();
            public CheckpointBackend(NarrativeSequenceId id, MissionOutcome outcome, string state = null)
            {
                backend.RegisterSequence(id, _ => Task.FromResult(outcome));
                State = state ?? $"choice:{outcome}";
            }
            public string Id => "narrative";
            public int Version => 1;
            public string State { get; private set; }
            public string RestoredState { get; private set; }
            public SaveAvailability SaveAvailability => SaveAvailability.Allowed;
            public Task<NarrativeSessionResult<TOutcome>> PlayAsync<TOutcome>(NarrativeRequest request, CancellationToken cancellationToken = default) =>
                backend.PlayAsync<TOutcome>(request, cancellationToken);
            public Task<string> CaptureCheckpointAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);
            public Task RestoreCheckpointAsync(string checkpoint, CancellationToken cancellationToken = default)
            {
                State = checkpoint;
                RestoredState = checkpoint;
                return Task.CompletedTask;
            }
            public Task<string> CaptureAsync(CancellationToken cancellationToken = default) => CaptureCheckpointAsync(cancellationToken);
            public Task RestoreAsync(int version, string state, CancellationToken cancellationToken = default) => RestoreCheckpointAsync(state, cancellationToken);
        }

        private sealed class CommandContributor : IUnifiedSaveContributor
        {
            private readonly GameplayCommandExecutor commands;
            public CommandContributor(GameplayCommandExecutor commands) => this.commands = commands;
            public string Id => "gameplay-commands";
            public int Version => 1;
            public Task<string> CaptureAsync(CancellationToken cancellationToken = default) =>
                Task.FromResult(string.Join("\n", commands.CaptureCommittedExecutionIds()));
            public Task RestoreAsync(int version, string state, CancellationToken cancellationToken = default)
            {
                commands.RestoreCommittedExecutionIds(
                    string.IsNullOrEmpty(state) ? Array.Empty<string>() : state.Split('\n'));
                return Task.CompletedTask;
            }
        }

        private sealed class MemoryStore : IUnifiedSaveStore
        {
            private UnifiedSaveRecord record;
            public Task ReplaceAsync(string slotId, UnifiedSaveRecord value, CancellationToken cancellationToken = default)
            {
                record = value;
                return Task.CompletedTask;
            }
            public Task<UnifiedSaveRecord> ReadAsync(string slotId, CancellationToken cancellationToken = default) =>
                Task.FromResult(record);
        }
    }
}
