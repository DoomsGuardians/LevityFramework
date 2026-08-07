using System.Threading;
using System.Threading.Tasks;
using global::Levity.Stage.Unity;
using NUnit.Framework;

namespace Levity.Stage.Tests.Unity
{
    public sealed class UnitySceneStageLoaderTests
    {
        [Test]
        public void UnsupportedSceneFailsValidationBeforeAdditiveLoadingStarts()
        {
            var gateway = new RecordingSceneGateway { CanLoad = false };
            IStageLoader loader = new UnitySceneStageLoader(gateway);
            var target = new StageDescriptor(new StageId("mission"), "Scenes/Mission");

            var validation = loader.Validate(target);

            Assert.That(validation.IsValid, Is.False);
            Assert.That(validation.Message, Does.Contain("additive"));
            Assert.That(gateway.LoadCount, Is.Zero);
        }

        [Test]
        public void ActivatingAnAdditiveCandidateDoesNotUnloadItBeforeRelease()
        {
            var scene = new RecordingSceneHandle();
            var gateway = new RecordingSceneGateway { CanLoad = true, Scene = scene };
            IStageLoader loader = new UnitySceneStageLoader(gateway);
            var target = new StageDescriptor(new StageId("mission"), "Scenes/Mission");

            var candidate = loader.PrepareAsync(target, CancellationToken.None)
                .GetAwaiter().GetResult();
            candidate.ActivateAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(gateway.LoadCount, Is.EqualTo(1));
            Assert.That(scene.ActivateCount, Is.EqualTo(1));
            Assert.That(scene.UnloadCount, Is.Zero);

            candidate.ReleaseAsync(CancellationToken.None).GetAwaiter().GetResult();
            Assert.That(scene.UnloadCount, Is.EqualTo(1));
        }

        private sealed class RecordingSceneGateway : IUnitySceneGateway
        {
            public bool CanLoad { get; set; }
            public int LoadCount { get; private set; }
            public IUnitySceneHandle Scene { get; set; }

            public bool CanLoadAdditively(string loadKey) => CanLoad;

            public Task<IUnitySceneHandle> LoadAdditivelyAsync(
                string loadKey,
                CancellationToken cancellationToken)
            {
                LoadCount++;
                return Task.FromResult(Scene);
            }
        }

        private sealed class RecordingSceneHandle : IUnitySceneHandle
        {
            public int ActivateCount { get; private set; }
            public int UnloadCount { get; private set; }

            public Task ActivateAsync(CancellationToken cancellationToken)
            {
                ActivateCount++;
                return Task.CompletedTask;
            }

            public Task UnloadAsync(CancellationToken cancellationToken)
            {
                UnloadCount++;
                return Task.CompletedTask;
            }
        }
    }
}
