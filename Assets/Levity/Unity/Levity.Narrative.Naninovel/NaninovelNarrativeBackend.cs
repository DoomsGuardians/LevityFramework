using System;
using System.Threading;
using System.Threading.Tasks;
using Levity.Narrative.Core;

namespace Levity.Narrative.Naninovel
{
    /// <summary>Adapts a Naninovel player extension to Levity's backend-neutral narrative contract.</summary>
    public sealed class NaninovelNarrativeBackend : INarrativeModule, INarrativeSequenceMapping
    {
        private readonly NarrativeSequenceRegistry registry;
        private readonly INaninovelPlayer player;

        public NaninovelNarrativeBackend(NarrativeSequenceRegistry registry, INaninovelPlayer player)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
            this.player = player ?? throw new ArgumentNullException(nameof(player));
        }

        public SaveAvailability SaveAvailability => player.SaveAvailability;

        public bool Contains(NarrativeSequenceId sequenceId) =>
            registry.TryResolve(sequenceId, out _);

        public async Task<NarrativeSessionResult<TOutcome>> PlayAsync<TOutcome>(
            NarrativeRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            if (!registry.TryResolve(request.SequenceId, out var sequence))
            {
                return NarrativeSessionResult<TOutcome>.Failed(new NarrativeFailure(
                    NarrativeFailureCode.SequenceNotFound,
                    $"Narrative sequence '{request.SequenceId}' is not registered for Naninovel."));
            }

            var entryPoint = string.IsNullOrWhiteSpace(request.EntryPoint)
                ? sequence.EntryPoint
                : request.EntryPoint;

            try
            {
                var outcome = await player.PlayAsync(
                    new NaninovelPlaybackRequest(
                        sequence.ScriptPath,
                        entryPoint,
                        request.Parameters,
                        request.ConcurrentPolicy),
                    cancellationToken).ConfigureAwait(false);

                if (outcome is TOutcome typedOutcome)
                    return NarrativeSessionResult<TOutcome>.Completed(typedOutcome);

                if (outcome == null && (object)default(TOutcome) == null)
                    return NarrativeSessionResult<TOutcome>.Completed(default);

                return NarrativeSessionResult<TOutcome>.Failed(new NarrativeFailure(
                    NarrativeFailureCode.InvalidOutcome,
                    $"Naninovel sequence '{request.SequenceId}' returned an incompatible outcome."));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return NarrativeSessionResult<TOutcome>.Cancelled();
            }
            catch (NaninovelSessionCancelledException)
            {
                return NarrativeSessionResult<TOutcome>.Cancelled();
            }
            catch (NaninovelConcurrentSessionException exception)
            {
                return NarrativeSessionResult<TOutcome>.Failed(new NarrativeFailure(
                    NarrativeFailureCode.ConcurrentSession,
                    exception.Message,
                    exception));
            }
            catch (NaninovelUnavailableException exception)
            {
                return NarrativeSessionResult<TOutcome>.Failed(new NarrativeFailure(
                    NarrativeFailureCode.BackendUnavailable,
                    exception.Message,
                    exception));
            }
            catch (Exception exception)
            {
                return NarrativeSessionResult<TOutcome>.Failed(new NarrativeFailure(
                    NarrativeFailureCode.BackendFailure,
                    exception.Message,
                    exception));
            }
        }
    }
}
