using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Levity.Stage.Unity
{
    public interface IUnitySceneGateway
    {
        bool CanLoadAdditively(string loadKey);

        Task<IUnitySceneHandle> LoadAdditivelyAsync(
            string loadKey,
            CancellationToken cancellationToken);
    }

    public interface IUnitySceneHandle
    {
        Task ActivateAsync(CancellationToken cancellationToken);

        Task UnloadAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// Prepares Unity scenes additively so activating a candidate never destroys the prior Stage.
    /// </summary>
    public sealed class UnitySceneStageLoader : IStageLoader
    {
        private readonly IUnitySceneGateway gateway;

        public UnitySceneStageLoader() : this(new UnitySceneGateway()) { }

        public UnitySceneStageLoader(IUnitySceneGateway gateway)
        {
            this.gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        }

        public StageLoadValidation Validate(StageDescriptor target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            return gateway.CanLoadAdditively(target.LoadKey)
                ? StageLoadValidation.Valid()
                : StageLoadValidation.Invalid(
                    $"Scene '{target.LoadKey}' cannot be loaded additively; Stage rollback cannot be guaranteed.");
        }

        public async Task<IStageHandle> PrepareAsync(
            StageDescriptor target,
            CancellationToken cancellationToken)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (!gateway.CanLoadAdditively(target.LoadKey))
                throw new InvalidOperationException(
                    $"Scene '{target.LoadKey}' cannot be prepared additively.");

            var scene = await gateway.LoadAdditivelyAsync(target.LoadKey, cancellationToken);
            if (scene == null)
                throw new InvalidOperationException(
                    $"Scene gateway returned no handle for '{target.LoadKey}'.");

            return new UnityStageHandle(target, scene);
        }

        private sealed class UnityStageHandle : IStageHandle
        {
            private readonly IUnitySceneHandle scene;

            public UnityStageHandle(StageDescriptor descriptor, IUnitySceneHandle scene)
            {
                Descriptor = descriptor;
                this.scene = scene;
            }

            public StageDescriptor Descriptor { get; }

            public Task ActivateAsync(CancellationToken cancellationToken) =>
                scene.ActivateAsync(cancellationToken);

            public Task ReleaseAsync(CancellationToken cancellationToken) =>
                scene.UnloadAsync(cancellationToken);
        }
    }

    internal sealed class UnitySceneGateway : IUnitySceneGateway
    {
        public bool CanLoadAdditively(string loadKey) =>
            !string.IsNullOrWhiteSpace(loadKey) && Application.CanStreamedLevelBeLoaded(loadKey);

        public async Task<IUnitySceneHandle> LoadAdditivelyAsync(
            string loadKey,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var operation = SceneManager.LoadSceneAsync(loadKey, LoadSceneMode.Additive);
            if (operation == null)
                throw new InvalidOperationException($"Unity did not start loading scene '{loadKey}'.");

            while (!operation.isDone) await Task.Yield();

            var scene = SceneManager.GetSceneByPath(loadKey);
            if (!scene.IsValid()) scene = SceneManager.GetSceneByName(loadKey);
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException($"Unity loaded no scene for '{loadKey}'.");

            var handle = new LoadedUnityScene(scene);
            if (cancellationToken.IsCancellationRequested)
            {
                await handle.UnloadAsync(CancellationToken.None);
                cancellationToken.ThrowIfCancellationRequested();
            }

            return handle;
        }

        private sealed class LoadedUnityScene : IUnitySceneHandle
        {
            private readonly Scene scene;

            public LoadedUnityScene(Scene scene) => this.scene = scene;

            public Task ActivateAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!SceneManager.SetActiveScene(scene))
                    throw new InvalidOperationException(
                        $"Unity could not activate scene '{scene.path}'.");
                return Task.CompletedTask;
            }

            public async Task UnloadAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!scene.IsValid() || !scene.isLoaded) return;
                var operation = SceneManager.UnloadSceneAsync(scene);
                if (operation == null) return;
                while (!operation.isDone)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Yield();
                }
            }
        }
    }
}
