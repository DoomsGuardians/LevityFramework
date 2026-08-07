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
        public IEnumerator OverlappingRequestIsRejectedWithoutDisturbingActiveSession()
        {
            if (Engine.Initialized || Engine.Initializing) Engine.Destroy();
            var backend = NaninovelRuntimeBinding.CreateDefault().Module;
            var first = backend.PlayAsync<string>(new NarrativeRequest(
                new NarrativeSequenceId("stage.mission.accept")));

            IChoiceHandlerActor handler = null;
            for (var frame = 0; frame < 600 && handler == null; frame++)
            {
                handler = FindPresentedChoice();
                yield return null;
            }
            Assert.That(handler, Is.Not.Null, "The first session did not reach its choice.");

            var competingBackend = NaninovelRuntimeBinding.CreateDefault().Module;
            var second = competingBackend.PlayAsync<string>(new NarrativeRequest(
                new NarrativeSequenceId("stage.mission.accept")));
            for (var frame = 0; frame < 60 && !second.IsCompleted; frame++) yield return null;

            Assert.That(second.IsCompleted, Is.True, "Reject policy did not return immediately.");
            Assert.That(second.Result.Status, Is.EqualTo(NarrativeSessionStatus.Failed));
            Assert.That(second.Result.Failure.Code, Is.EqualTo(NarrativeFailureCode.ConcurrentSession));
            Assert.That(first.IsCompleted, Is.False);

            handler.HandleChoice(handler.Choices[0].Id);
            for (var frame = 0; frame < 600 && !first.IsCompleted; frame++) yield return null;
            Assert.That(first.Result.Status, Is.EqualTo(NarrativeSessionStatus.Completed));
            Assert.That(first.Result.Outcome, Is.EqualTo("Accept"));
        }

        [UnityTest]
        public IEnumerator RuntimePlayerIsTheDocumentedSharedInitializationOwner()
        {
            if (Engine.Initialized || Engine.Initializing) Engine.Destroy();
            var initialization = InitializeRuntimeTwiceAsync();
            for (var frame = 0; frame < 600 && !initialization.IsCompleted; frame++)
                yield return null;

            Assert.That(
                initialization.IsCompletedSuccessfully,
                Is.True,
                initialization.Exception?.ToString());
            Assert.That(Engine.Initialized, Is.True);
            Assert.That(
                Engine.GetConfiguration<EngineConfiguration>().InitializeOnApplicationLoad,
                Is.False,
                "Naninovel automatic initialization competes with the runtime player owner.");
            var legacyService = Type.GetType("NaninovelService, Assembly-CSharp", false);
            if (legacyService != null)
            {
                var diagnostic = legacyService.GetCustomAttribute<ObsoleteAttribute>();
                Assert.That(diagnostic.Message, Does.Contain("NaninovelRuntimePlayer"));
                Assert.That(
                    legacyService.GetMethod("InitializeAsync", BindingFlags.Instance | BindingFlags.NonPublic),
                    Is.Null,
                    "The legacy utility still owns a second initialization entry point.");
            }
        }

        [UnityTest]
        public IEnumerator CancelPolicyCancelsNewRequestAndLeavesActiveSessionRunning()
        {
            if (Engine.Initialized || Engine.Initializing) Engine.Destroy();
            var backend = NaninovelRuntimeBinding.CreateDefault().Module;
            var first = backend.PlayAsync<string>(new NarrativeRequest(
                new NarrativeSequenceId("stage.mission.accept")));
            IChoiceHandlerActor handler = null;
            for (var frame = 0; frame < 600 && handler == null; frame++)
            {
                handler = FindPresentedChoice();
                yield return null;
            }

            var cancelled = backend.PlayAsync<string>(new NarrativeRequest(
                new NarrativeSequenceId("stage.mission.accept"),
                concurrentPolicy: ConcurrentRequestPolicy.Cancel));
            for (var frame = 0; frame < 60 && !cancelled.IsCompleted; frame++) yield return null;

            Assert.That(cancelled.Result.Status, Is.EqualTo(NarrativeSessionStatus.Cancelled));
            Assert.That(first.IsCompleted, Is.False);
            handler.HandleChoice(handler.Choices[0].Id);
            for (var frame = 0; frame < 600 && !first.IsCompleted; frame++) yield return null;
            Assert.That(first.Result.Status, Is.EqualTo(NarrativeSessionStatus.Completed));
        }

        [UnityTest]
        public IEnumerator WaitPolicyStartsOnlyAfterActiveSessionCompletes()
        {
            if (Engine.Initialized || Engine.Initializing) Engine.Destroy();
            var backend = NaninovelRuntimeBinding.CreateDefault().Module;
            var first = backend.PlayAsync<string>(new NarrativeRequest(
                new NarrativeSequenceId("stage.mission.accept")));
            IChoiceHandlerActor firstHandler = null;
            for (var frame = 0; frame < 600 && firstHandler == null; frame++)
            {
                firstHandler = FindPresentedChoice();
                yield return null;
            }

            var waiting = backend.PlayAsync<string>(new NarrativeRequest(
                new NarrativeSequenceId("stage.mission.accept"),
                concurrentPolicy: ConcurrentRequestPolicy.Wait));
            yield return null;
            Assert.That(waiting.IsCompleted, Is.False);
            firstHandler.HandleChoice(firstHandler.Choices[0].Id);
            for (var frame = 0; frame < 600 && !first.IsCompleted; frame++) yield return null;
            Assert.That(first.Result.Status, Is.EqualTo(NarrativeSessionStatus.Completed));

            IChoiceHandlerActor waitingHandler = null;
            for (var frame = 0; frame < 600 && waitingHandler == null; frame++)
            {
                waitingHandler = FindPresentedChoice();
                yield return null;
            }
            Assert.That(waitingHandler, Is.Not.Null, "The waiting session never started.");
            waitingHandler.HandleChoice(waitingHandler.Choices[0].Id);
            for (var frame = 0; frame < 600 && !waiting.IsCompleted; frame++) yield return null;
            Assert.That(waiting.Result.Status, Is.EqualTo(NarrativeSessionStatus.Completed));
        }

        [UnityTest]
        public IEnumerator ReplacePolicyCancelsActiveSessionBeforeStartingReplacement()
        {
            if (Engine.Initialized || Engine.Initializing) Engine.Destroy();
            var backend = NaninovelRuntimeBinding.CreateDefault().Module;
            var first = backend.PlayAsync<string>(new NarrativeRequest(
                new NarrativeSequenceId("stage.mission.accept")));
            IChoiceHandlerActor firstHandler = null;
            for (var frame = 0; frame < 600 && firstHandler == null; frame++)
            {
                firstHandler = FindPresentedChoice();
                yield return null;
            }

            var replacement = backend.PlayAsync<string>(new NarrativeRequest(
                new NarrativeSequenceId("stage.mission.accept"),
                concurrentPolicy: ConcurrentRequestPolicy.Replace));
            for (var frame = 0; frame < 600 && !first.IsCompleted; frame++) yield return null;
            Assert.That(first.Result.Status, Is.EqualTo(NarrativeSessionStatus.Cancelled));

            IChoiceHandlerActor replacementHandler = null;
            for (var frame = 0; frame < 600 && replacementHandler == null; frame++)
            {
                replacementHandler = FindPresentedChoice();
                yield return null;
            }
            Assert.That(replacementHandler, Is.Not.Null, "The replacement session never started.");
            replacementHandler.HandleChoice(replacementHandler.Choices[0].Id);
            for (var frame = 0; frame < 600 && !replacement.IsCompleted; frame++) yield return null;
            Assert.That(replacement.Result.Status, Is.EqualTo(NarrativeSessionStatus.Completed));
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

        private static async Task InitializeRuntimeTwiceAsync()
        {
            var first = NaninovelRuntimePlayer.EnsureInitializedAsync();
            var second = NaninovelRuntimePlayer.EnsureInitializedAsync();
            await first;
            await second;
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
