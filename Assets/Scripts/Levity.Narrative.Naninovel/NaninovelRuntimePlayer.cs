using System;
using System.Threading;
using System.Threading.Tasks;
using Levity.Narrative.Core;
using Naninovel;

namespace Levity.Narrative.Naninovel
{
    /// <summary>Runs registered sequences against the installed Naninovel engine.</summary>
    public sealed class NaninovelRuntimePlayer : INaninovelPlayer
    {
        private const string UnsafePresentationReason = "Narrative presentation is in progress.";
        private int activeSessions;

        public SaveAvailability SaveAvailability
        {
            get
            {
                return activeSessions > 0
                    ? SaveAvailability.Blocked(UnsafePresentationReason)
                    : SaveAvailability.Allowed;
            }
        }

        public async Task<object> PlayAsync(
            NaninovelPlaybackRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (!Engine.Initialized) await RuntimeInitializer.Initialize();

            var player = Engine.GetServiceOrErr<IScriptPlayer>();
            var outcome = NaninovelOutcomeBridge.Begin();
            Interlocked.Increment(ref activeSessions);
            try
            {
                if (string.IsNullOrWhiteSpace(request.EntryPoint))
                    await player.LoadAndPlay(request.ScriptPath);
                else
                    await player.LoadAndPlayAtLabel(request.ScriptPath, request.EntryPoint);

                var cancellation = Task.Delay(Timeout.Infinite, cancellationToken);
                if (await Task.WhenAny(outcome, cancellation) != outcome)
                {
                    NaninovelOutcomeBridge.Cancel();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                while (player.Playing || player.Completing)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await AsyncUtils.WaitEndOfFrame();
                }

                return await outcome;
            }
            finally
            {
                Interlocked.Decrement(ref activeSessions);
            }
        }
    }
}
