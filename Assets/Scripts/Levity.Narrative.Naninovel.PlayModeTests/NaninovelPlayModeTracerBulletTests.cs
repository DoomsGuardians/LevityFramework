using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Levity.Narrative.Core;
using Levity.Narrative.Flow;
using Levity.UnifiedSave;
using Naninovel;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Levity.Narrative.Naninovel.Tests
{
    public sealed class NaninovelPlayModeTracerBulletTests
    {
        [TearDown]
        public void TearDown()
        {
            if (Engine.Initialized || Engine.Initializing) Engine.Destroy();
        }

        [UnityTest]
        public IEnumerator GameRootDiscoversTheInstalledNarrativeRuntime()
        {
            var rootType = Type.GetType("GameRoot, Assembly-CSharp", throwOnError: true);
            var gameObject = new GameObject("Production GameRoot composition test");
            var root = gameObject.AddComponent(rootType);
            rootType.GetField("TestStageID", BindingFlags.Instance | BindingFlags.Public)
                .SetValue(root, 0);

            yield return null;

            var moduleField = rootType.GetField(
                "narrativeModule", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(moduleField, Is.Not.Null,
                "GameRoot does not expose the backend-neutral Narrative Runtime.");
            Assert.That(moduleField.GetValue(root), Is.AssignableTo<INarrativeModule>());
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        [UnityTest]
        public IEnumerator GameRootBlocksSavingWhileItsNarrativeSessionIsActive()
        {
            const int slot = 987653;
            var rootType = Type.GetType("GameRoot, Assembly-CSharp", throwOnError: true);
            var gameObject = new GameObject("Production Narrative Runtime save test");
            var root = gameObject.AddComponent(rootType);
            rootType.GetField("TestStageID", BindingFlags.Instance | BindingFlags.Public)
                .SetValue(root, 0);
            yield return null;

            var module = (INarrativeModule)rootType
                .GetField("narrativeModule", BindingFlags.Instance | BindingFlags.Public)
                .GetValue(root);
            var play = module.PlayAsync<string>(new NarrativeRequest(
                new NarrativeSequenceId("stage.mission.accept")));
            IChoiceHandlerActor handler = null;
            for (var frame = 0; frame < 600 && handler == null; frame++)
            {
                handler = FindPresentedChoice();
                yield return null;
            }
            Assert.That(handler, Is.Not.Null, "The production Narrative Session did not present its choice.");

            var dataService = rootType
                .GetField("dataService", BindingFlags.Instance | BindingFlags.Public)
                .GetValue(root);
            var dataServiceType = dataService.GetType();
            dataServiceType.GetMethod("DeleteSlot", BindingFlags.Instance | BindingFlags.Public)
                .Invoke(dataService, new object[] { slot });
            var saving = (Task)dataServiceType
                .GetMethod("SaveToSlot", BindingFlags.Instance | BindingFlags.Public)
                .Invoke(dataService, new object[] { slot });
            while (!saving.IsCompleted) yield return null;

            var result = saving.GetType().GetProperty("Result").GetValue(saving);
            Assert.That(
                result.GetType().GetProperty("Status").GetValue(result).ToString(),
                Is.EqualTo("Blocked"));
            Assert.That(
                result.GetType().GetProperty("BlockedReason").GetValue(result),
                Is.Not.Empty);
            Assert.That(
                (bool)dataServiceType.GetMethod("SlotExists").Invoke(dataService, new object[] { slot }),
                Is.False);

            handler.HandleChoice(handler.Choices[0].Id);
            for (var frame = 0; frame < 600 && !play.IsCompleted; frame++) yield return null;
            Assert.That(play.IsCompletedSuccessfully, Is.True);
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        [UnityTest]
        public IEnumerator RealChoiceBlocksSavingAndReturnsTypedOutcome()
        {
            if (Engine.Initialized || Engine.Initializing) Engine.Destroy();
            var binding = NaninovelRuntimeBinding.CreateDefault();
            var backend = binding.Module;
            var play = backend.PlayAsync<string>(new NarrativeRequest(
                new NarrativeSequenceId("stage.mission.accept")));

            IChoiceHandlerActor handler = null;
            for (var frame = 0; frame < 600 && handler == null; frame++)
            {
                if (Engine.Initialized)
                {
                    var manager = Engine.GetService<IChoiceHandlerManager>();
                    var id = manager?.Configuration.DefaultHandlerId;
                    handler = string.IsNullOrEmpty(id) || !manager.ActorExists(id)
                        ? null
                        : manager.GetActor(id);
                    if (handler?.Choices.Count == 0) handler = null;
                }
                yield return null;
            }

            Assert.That(handler, Is.Not.Null, "The real .nani choice was not presented.");
            Assert.That(backend.SaveAvailability.CanSave, Is.False);
            Assert.That(backend.SaveAvailability.BlockedReason, Is.Not.Empty);
            handler.HandleChoice(handler.Choices[0].Id);
            for (var frame = 0; frame < 600 && !play.IsCompleted; frame++) yield return null;
            Assert.That(play.IsCompleted, Is.True, "The selected .nani choice did not complete the session.");
            Assert.That(play.Result.Status, Is.EqualTo(NarrativeSessionStatus.Completed));
            Assert.That(play.Result.Outcome, Is.EqualTo("Accept"));
            Assert.That(backend.SaveAvailability.CanSave, Is.True);
        }

        [UnityTest]
        public IEnumerator PlaySaveRebuildAndLoadDoesNotRepeatGameplaySideEffect()
        {
            if (Engine.Initialized || Engine.Initializing) Engine.Destroy();
            var sideEffectCount = 0;
            var executionId = new GameplayCommandExecutionId("stage.mission.accept:grant-key");
            var commands = new GameplayCommandExecutor();
            commands.Register("grant-key", () => sideEffectCount++);
            var flow = NarrativeFlowNode<string>.Create(new NarrativeSequenceId("stage.mission.accept"))
                .On("Accept", "grant-key", executionId, "mission-accepted");
            var binding = NaninovelRuntimeBinding.CreateDefault();
            var backend = binding.Module;
            var play = flow.PlayAsync(backend, commands);

            IChoiceHandlerActor handler = null;
            for (var frame = 0; frame < 600 && handler == null; frame++)
            {
                handler = FindPresentedChoice();
                yield return null;
            }
            Assert.That(handler, Is.Not.Null, "The real .nani choice was not presented.");
            handler.HandleChoice(handler.Choices[0].Id);
            for (var frame = 0; frame < 600 && !play.IsCompleted; frame++) yield return null;
            Assert.That(play.IsCompletedSuccessfully, Is.True);
            Assert.That(sideEffectCount, Is.EqualTo(1));

            var store = new MemoryStore();
            var save = new UnifiedSave.UnifiedSave(
                store,
                binding.SaveContributor,
                new CommandContributor(commands));
            var saving = save.TrySaveAsync("tracer");
            for (var frame = 0; frame < 600 && !saving.IsCompleted; frame++) yield return null;
            Assert.That(saving.IsCompleted, Is.True, "Unified Save capture did not complete.");
            Assert.That(saving.IsCompletedSuccessfully, Is.True);
            Assert.That(saving.Result.Status, Is.EqualTo(UnifiedSaveStatus.Saved));

            Engine.Destroy();
            yield return null;

            var restoredCommands = new GameplayCommandExecutor();
            restoredCommands.Register("grant-key", () => sideEffectCount++);
            var restoredBinding = NaninovelRuntimeBinding.CreateDefault();
            var restored = new UnifiedSave.UnifiedSave(
                store,
                restoredBinding.SaveContributor,
                new CommandContributor(restoredCommands));
            var loading = restored.LoadAsync("tracer");
            for (var frame = 0; frame < 600 && !loading.IsCompleted; frame++) yield return null;
            Assert.That(loading.IsCompleted, Is.True, "Unified Save restore did not complete.");
            Assert.That(
                loading.IsCompletedSuccessfully,
                Is.True,
                loading.Exception?.ToString());
            var variables = Engine.GetService<ICustomVariableManager>();
            Assert.That(variables.VariableExists("missionAccepted"), Is.True);
            Assert.That(variables.GetVariableValue("missionAccepted").Boolean, Is.True);
            Assert.That(restoredCommands.Execute("grant-key", executionId), Is.False);
            Assert.That(sideEffectCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DataServiceDoesNotCreateSlotWhenNarrativeBlocksSaving()
        {
            const int slot = 987654;
            var type = Type.GetType("DataService, Assembly-CSharp", throwOnError: true);
            var dataService = Activator.CreateInstance(type);
            type.GetMethod("OnInit", BindingFlags.Instance | BindingFlags.Public).Invoke(dataService, null);
            type.GetMethod("DeleteSlot", BindingFlags.Instance | BindingFlags.Public)
                .Invoke(dataService, new object[] { slot });
            type.GetMethod("SetSaveAvailabilitySource", BindingFlags.Instance | BindingFlags.Public)
                .Invoke(dataService, new object[] {
                    new Func<SaveAvailability>(() => SaveAvailability.Blocked("Unsafe cinematic presentation."))
                });

            var save = (Task)type.GetMethod("SaveToSlot", BindingFlags.Instance | BindingFlags.Public)
                .Invoke(dataService, new object[] { slot });
            while (!save.IsCompleted) yield return null;

            Assert.That(save.IsCompletedSuccessfully, Is.True);
            var result = save.GetType().GetProperty("Result").GetValue(save);
            Assert.That(result.GetType().GetProperty("Status").GetValue(result).ToString(), Is.EqualTo("Blocked"));
            Assert.That(result.GetType().GetProperty("BlockedReason").GetValue(result),
                Is.EqualTo("Unsafe cinematic presentation."));
            Assert.That((bool)type.GetMethod("SlotExists").Invoke(dataService, new object[] { slot }), Is.False);
        }

        private static IChoiceHandlerActor FindPresentedChoice()
        {
            if (!Engine.Initialized) return null;
            var manager = Engine.GetService<IChoiceHandlerManager>();
            var id = manager?.Configuration.DefaultHandlerId;
            if (string.IsNullOrEmpty(id) || !manager.ActorExists(id)) return null;
            var handler = manager.GetActor(id);
            return handler.Choices.Count == 0 ? null : handler;
        }

        private sealed class MemoryStore : IUnifiedSaveStore
        {
            private UnifiedSaveRecord record;
            public Task ReplaceAsync(string slotId, UnifiedSaveRecord value, CancellationToken cancellationToken = default)
            { record = value; return Task.CompletedTask; }
            public Task<UnifiedSaveRecord> ReadAsync(string slotId, CancellationToken cancellationToken = default) =>
                Task.FromResult(record);
        }

        private sealed class CommandContributor : IUnifiedSaveContributor
        {
            private readonly GameplayCommandExecutor commands;
            public CommandContributor(GameplayCommandExecutor commands) => this.commands = commands;
            public string Id => "commands";
            public int Version => 1;
            public Task<string> CaptureAsync(CancellationToken cancellationToken = default) =>
                Task.FromResult(string.Join("\n", commands.CaptureCommittedExecutionIds()));
            public Task RestoreAsync(int version, string state, CancellationToken cancellationToken = default)
            {
                commands.RestoreCommittedExecutionIds(string.IsNullOrEmpty(state)
                    ? new string[0]
                    : state.Split('\n'));
                return Task.CompletedTask;
            }
        }
    }
}
