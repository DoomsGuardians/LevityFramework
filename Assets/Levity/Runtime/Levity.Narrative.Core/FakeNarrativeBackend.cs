using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Levity.Narrative.Core
{
    public sealed class FakeNarrativeBackend : INarrativeModule
    {
        private readonly object sync = new object();
        private readonly Dictionary<NarrativeSequenceId, Func<CancellationToken, Task<object>>> sequences =
            new Dictionary<NarrativeSequenceId, Func<CancellationToken, Task<object>>>();

        private ActiveSession activeSession;

        public SaveAvailability SaveAvailability => SaveAvailability.Allowed;

        public void RegisterSequence<TOutcome>(
            NarrativeSequenceId sequenceId,
            Func<CancellationToken, Task<TOutcome>> play)
        {
            if (play == null) throw new ArgumentNullException(nameof(play));

            lock (sync)
            {
                sequences[sequenceId] = async cancellationToken =>
                    (object)await play(cancellationToken).ConfigureAwait(false);
            }
        }

        public Task<NarrativeSessionResult<TOutcome>> PlayAsync<TOutcome>(
            NarrativeRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return PlayCoreAsync<TOutcome>(request, cancellationToken);
        }

        private async Task<NarrativeSessionResult<TOutcome>> PlayCoreAsync<TOutcome>(
            NarrativeRequest request,
            CancellationToken cancellationToken)
        {
            ActiveSession session;

            while (true)
            {
                ActiveSession sessionToWaitFor;
                var replaceActiveSession = false;

                lock (sync)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return NarrativeSessionResult<TOutcome>.Cancelled();

                    sessionToWaitFor = activeSession;
                    if (sessionToWaitFor == null)
                    {
                        session = new ActiveSession(cancellationToken);
                        activeSession = session;
                        break;
                    }

                    switch (request.ConcurrentPolicy)
                    {
                        case ConcurrentRequestPolicy.Reject:
                            return NarrativeSessionResult<TOutcome>.Failed(new NarrativeFailure(
                                NarrativeFailureCode.ConcurrentSession,
                                "A narrative session is already active."));
                        case ConcurrentRequestPolicy.Cancel:
                            return NarrativeSessionResult<TOutcome>.Cancelled();
                        case ConcurrentRequestPolicy.Replace:
                            replaceActiveSession = true;
                            break;
                        case ConcurrentRequestPolicy.Wait:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                if (replaceActiveSession)
                    sessionToWaitFor.Cancellation.Cancel();

                if (!await WaitForSessionAsync(sessionToWaitFor, cancellationToken).ConfigureAwait(false))
                    return NarrativeSessionResult<TOutcome>.Cancelled();
            }

            try
            {
                Func<CancellationToken, Task<object>> play;
                lock (sync)
                {
                    sequences.TryGetValue(request.SequenceId, out play);
                }

                if (play == null)
                {
                    return NarrativeSessionResult<TOutcome>.Failed(new NarrativeFailure(
                        NarrativeFailureCode.SequenceNotFound,
                        $"Narrative sequence '{request.SequenceId}' is not registered."));
                }

                var outcome = await play(session.Cancellation.Token).ConfigureAwait(false);
                if (outcome is TOutcome typedOutcome)
                    return NarrativeSessionResult<TOutcome>.Completed(typedOutcome);

                if (outcome == null && (object)default(TOutcome) == null)
                    return NarrativeSessionResult<TOutcome>.Completed(default);

                return NarrativeSessionResult<TOutcome>.Failed(new NarrativeFailure(
                    NarrativeFailureCode.InvalidOutcome,
                    $"Narrative sequence '{request.SequenceId}' returned an incompatible outcome."));
            }
            catch (OperationCanceledException) when (session.Cancellation.IsCancellationRequested)
            {
                return NarrativeSessionResult<TOutcome>.Cancelled();
            }
            catch (Exception exception)
            {
                return NarrativeSessionResult<TOutcome>.Failed(new NarrativeFailure(
                    NarrativeFailureCode.BackendFailure,
                    exception.Message,
                    exception));
            }
            finally
            {
                lock (sync)
                {
                    if (ReferenceEquals(activeSession, session))
                        activeSession = null;
                }

                session.Completion.TrySetResult(true);
                session.Dispose();
            }
        }

        private static async Task<bool> WaitForSessionAsync(
            ActiveSession session,
            CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
            {
                await session.Completion.Task.ConfigureAwait(false);
                return true;
            }

            var cancelled = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(() => cancelled.TrySetResult(true)))
            {
                var completed = await Task.WhenAny(session.Completion.Task, cancelled.Task).ConfigureAwait(false);
                return completed == session.Completion.Task;
            }
        }

        private sealed class ActiveSession : IDisposable
        {
            public ActiveSession(CancellationToken cancellationToken)
            {
                Cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                Completion = new TaskCompletionSource<bool>();
            }

            public CancellationTokenSource Cancellation { get; }
            public TaskCompletionSource<bool> Completion { get; }

            public void Dispose() => Cancellation.Dispose();
        }
    }
}
