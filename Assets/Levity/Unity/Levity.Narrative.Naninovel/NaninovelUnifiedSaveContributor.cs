using System;
using System.Threading;
using System.Threading.Tasks;
using Levity.UnifiedSave;
using Naninovel;
using UnityEngine;

namespace Levity.Narrative.Naninovel
{
    /// <summary>Captures and restores Naninovel's complete game-scoped state map.</summary>
    public sealed class NaninovelUnifiedSaveContributor : IUnifiedSaveContributor
    {
        public string Id => "narrative";
        public int Version => 1;

        public async Task<string> CaptureAsync(CancellationToken cancellationToken = default)
        {
            await EnsureInitializedAsync();
            cancellationToken.ThrowIfCancellationRequested();
            var stateManager = Engine.GetServiceOrErr<IStateManager>();
            stateManager.PushRollbackSnapshot(allowPlayerRollback: false);
            var state = stateManager.PeekRollbackStack();
            if (state == null) throw new InvalidOperationException("Naninovel failed to capture narrative state.");
            return JsonUtility.ToJson(state);
        }

        public async Task RestoreAsync(
            int version,
            string state,
            CancellationToken cancellationToken = default)
        {
            if (version != Version) throw new InvalidOperationException($"Unsupported Naninovel save version {version}.");
            await EnsureInitializedAsync();
            cancellationToken.ThrowIfCancellationRequested();
            var map = JsonUtility.FromJson<GameStateMap>(state);
            if (map == null) throw new InvalidOperationException("Naninovel save contribution is invalid.");

            var stateManager = Engine.GetServiceOrErr<IStateManager>();
            var temporarySlot = $"levity-restore-{Guid.NewGuid():N}";
            try
            {
                await stateManager.GameSlotManager.Save(temporarySlot, map);
                await stateManager.LoadGame(temporarySlot);
            }
            finally
            {
                stateManager.GameSlotManager.DeleteSaveSlot(temporarySlot);
            }
        }

        private static async Task EnsureInitializedAsync()
        {
            await NaninovelRuntimePlayer.EnsureInitializedAsync();
        }
    }
}
