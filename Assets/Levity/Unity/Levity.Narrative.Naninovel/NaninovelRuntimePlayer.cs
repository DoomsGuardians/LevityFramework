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
        private static readonly object Sync = new object();
        private static ActiveSession activeSession;

        /// <summary>
        /// Authoritative Naninovel runtime initialization entry point. Legacy application
        /// utilities delegate here and never initialize or destroy the engine themselves.
        /// </summary>
        public static async UniTask EnsureInitializedAsync()
        {
            if (!Engine.Initialized) await RuntimeInitializer.Initialize();
        }

        public SaveAvailability SaveAvailability
        {
            get
            {
                lock (Sync)
                    return activeSession != null
                        ? SaveAvailability.Blocked(UnsafePresentationReason)
                        : SaveAvailability.Allowed;
            }
        }

        public async Task<object> PlayAsync(
            NaninovelPlaybackRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var session = await AcquireSessionAsync(request.ConcurrentPolicy, cancellationToken);
            return await RunSessionAsync(session, request, cancellationToken);
        }

        private async Task<object> RunSessionAsync(
            ActiveSession session,
            NaninovelPlaybackRequest request,
            CancellationToken callerCancellation)
        {
            IDisposable outcomeAttachment = null;
            try
            {
                await EnsureInitializedAsync();
                if (session.Cancellation.IsCancellationRequested)
                {
                    if (callerCancellation.IsCancellationRequested)
                        throw new OperationCanceledException(callerCancellation);
                    throw new NaninovelSessionCancelledException();
                }

                var player = Engine.GetServiceOrErr<IScriptPlayer>();
                outcomeAttachment = NaninovelOutcomeRouter.Attach(session);
                if (string.IsNullOrWhiteSpace(request.EntryPoint))
                    await player.LoadAndPlay(request.ScriptPath);
                else
                    await player.LoadAndPlayAtLabel(request.ScriptPath, request.EntryPoint);

                var cancellation = Task.Delay(Timeout.Infinite, session.Cancellation.Token);
                await Task.WhenAny(session.Outcome.Task, cancellation);
                if (session.Cancellation.IsCancellationRequested)
                {
                    player.Stop();
                    if (callerCancellation.IsCancellationRequested)
                        throw new OperationCanceledException(callerCancellation);
                    throw new NaninovelSessionCancelledException();
                }

                while (player.Playing || player.Completing)
                {
                    if (session.Cancellation.IsCancellationRequested)
                    {
                        player.Stop();
                        if (callerCancellation.IsCancellationRequested)
                            throw new OperationCanceledException(callerCancellation);
                        throw new NaninovelSessionCancelledException();
                    }
                    await AsyncUtils.WaitEndOfFrame();
                }

                return await session.Outcome.Task;
            }
            finally
            {
                outcomeAttachment?.Dispose();
                lock (Sync)
                {
                    if (ReferenceEquals(activeSession, session)) activeSession = null;
                }
                session.Completion.TrySetResult(true);
                session.Dispose();
            }
        }

        private async Task<ActiveSession> AcquireSessionAsync(
            ConcurrentRequestPolicy policy,
            CancellationToken cancellationToken)
        {
            while (true)
            {
                ActiveSession current;
                var replace = false;
                lock (Sync)
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(cancellationToken);
                    current = activeSession;
                    if (current == null)
                    {
                        var acquired = new ActiveSession(cancellationToken);
                        activeSession = acquired;
                        return acquired;
                    }

                    switch (policy)
                    {
                        case ConcurrentRequestPolicy.Reject:
                            throw new NaninovelConcurrentSessionException();
                        case ConcurrentRequestPolicy.Cancel:
                            throw new NaninovelSessionCancelledException();
                        case ConcurrentRequestPolicy.Replace:
                            replace = true;
                            break;
                        case ConcurrentRequestPolicy.Wait:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(policy));
                    }
                }

                if (replace) current.Cancellation.Cancel();
                if (!await WaitForCompletionAsync(current, cancellationToken))
                    throw new OperationCanceledException(cancellationToken);
            }
        }

        private static async Task<bool> WaitForCompletionAsync(
            ActiveSession session,
            CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
            {
                await session.Completion.Task;
                return true;
            }

            var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (cancellationToken.Register(() => cancelled.TrySetResult(true)))
                return await Task.WhenAny(session.Completion.Task, cancelled.Task) ==
                    session.Completion.Task;
        }

        private sealed class ActiveSession : INaninovelOutcomeSink, IDisposable
        {
            public ActiveSession(CancellationToken cancellationToken)
            {
                Cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                Outcome = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                Completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public CancellationTokenSource Cancellation { get; }
            public TaskCompletionSource<string> Outcome { get; }
            public TaskCompletionSource<bool> Completion { get; }
            public void Publish(string value) => Outcome.TrySetResult(value);
            public void Dispose() => Cancellation.Dispose();
        }
    }
}
