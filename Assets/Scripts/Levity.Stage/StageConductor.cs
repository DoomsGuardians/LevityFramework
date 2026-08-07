using System;
using System.Threading;
using System.Threading.Tasks;

namespace Levity.Stage
{
    public interface IStageLoader
    {
        StageLoadValidation Validate(StageDescriptor target);

        Task<IStageHandle> PrepareAsync(
            StageDescriptor target,
            CancellationToken cancellationToken);
    }

    public interface IStageHandle
    {
        StageDescriptor Descriptor { get; }

        Task ActivateAsync(CancellationToken cancellationToken);

        Task ReleaseAsync(CancellationToken cancellationToken);
    }

    public readonly struct StageLoadValidation
    {
        private StageLoadValidation(bool isValid, string message)
        {
            IsValid = isValid;
            Message = message;
        }

        public bool IsValid { get; }
        public string Message { get; }

        public static StageLoadValidation Valid() => new StageLoadValidation(true, null);

        public static StageLoadValidation Invalid(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Invalid Stage loading requires a reason.", nameof(message));

            return new StageLoadValidation(false, message);
        }
    }

    public enum StageChangeStatus
    {
        Completed,
        Cancelled,
        Failed
    }

    public enum StageChangePhase
    {
        Acquire,
        Validate,
        Prepare,
        Activate,
        Commit,
        ReleasePrevious
    }

    public enum StageChangeFailureCode
    {
        StageNotFound,
        ValidationFailed,
        PreparationFailed,
        ActivationFailed,
        ReleasePreviousFailed,
        ChangeInProgress,
        UnexpectedFailure
    }

    public sealed class StageChangeFailure
    {
        internal StageChangeFailure(
            StageChangeFailureCode code,
            StageChangePhase phase,
            StageId requestedStageId,
            string message,
            Exception exception = null,
            Exception cleanupException = null)
        {
            Code = code;
            Phase = phase;
            RequestedStageId = requestedStageId;
            Message = message;
            Exception = exception;
            CleanupException = cleanupException;
        }

        public StageChangeFailureCode Code { get; }
        public StageChangePhase Phase { get; }
        public StageId RequestedStageId { get; }
        public string Message { get; }
        public Exception Exception { get; }
        public Exception CleanupException { get; }
    }

    public readonly struct StageChangeResult
    {
        private StageChangeResult(StageChangeStatus status, StageChangeFailure failure)
        {
            Status = status;
            Failure = failure;
        }

        public StageChangeStatus Status { get; }
        public StageChangeFailure Failure { get; }

        internal static StageChangeResult Failed(StageChangeFailure failure) =>
            new StageChangeResult(StageChangeStatus.Failed, failure);

        internal static StageChangeResult Completed() =>
            new StageChangeResult(StageChangeStatus.Completed, null);

        internal static StageChangeResult Cancelled() =>
            new StageChangeResult(StageChangeStatus.Cancelled, null);
    }

    public sealed class StageConductor
    {
        private readonly StageRegistry registry;
        private readonly IStageLoader loader;
        private readonly object flightSync = new object();
        private IStageHandle current;
        private bool changeInProgress;

        public StageConductor(
            StageRegistry registry,
            IStageLoader loader,
            IStageHandle current = null)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
            this.loader = loader ?? throw new ArgumentNullException(nameof(loader));
            this.current = current;
        }

        public StageDescriptor CurrentStage => current?.Descriptor;

        public Task<StageChangeResult> ChangeAsync(
            StageId targetStageId,
            CancellationToken cancellationToken = default)
        {
            lock (flightSync)
            {
                if (changeInProgress)
                {
                    return Task.FromResult(StageChangeResult.Failed(new StageChangeFailure(
                        StageChangeFailureCode.ChangeInProgress,
                        StageChangePhase.Acquire,
                        targetStageId,
                        "Another Stage change is already in progress.")));
                }

                changeInProgress = true;
            }

            return RunSingleFlightAsync(targetStageId, cancellationToken);
        }

        private async Task<StageChangeResult> RunSingleFlightAsync(
            StageId targetStageId,
            CancellationToken cancellationToken)
        {
            try
            {
                return await ChangeCoreAsync(targetStageId, cancellationToken);
            }
            finally
            {
                lock (flightSync) changeInProgress = false;
            }
        }

        private async Task<StageChangeResult> ChangeCoreAsync(
            StageId targetStageId,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return StageChangeResult.Cancelled();

            var resolution = registry.Resolve(targetStageId);
            if (!resolution.IsSuccess)
            {
                return StageChangeResult.Failed(new StageChangeFailure(
                    StageChangeFailureCode.StageNotFound,
                    StageChangePhase.Validate,
                    targetStageId,
                    $"Stage '{targetStageId}' is not registered."));
            }

            StageLoadValidation validation;
            try
            {
                validation = loader.Validate(resolution.Descriptor);
            }
            catch (Exception exception)
            {
                return StageChangeResult.Failed(new StageChangeFailure(
                    StageChangeFailureCode.UnexpectedFailure,
                    StageChangePhase.Validate,
                    targetStageId,
                    $"Validating Stage '{targetStageId}' failed unexpectedly.",
                    exception));
            }

            if (!validation.IsValid)
            {
                return StageChangeResult.Failed(new StageChangeFailure(
                    StageChangeFailureCode.ValidationFailed,
                    StageChangePhase.Validate,
                    targetStageId,
                    validation.Message));
            }

            IStageHandle candidate;
            try
            {
                candidate = await loader.PrepareAsync(resolution.Descriptor, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return StageChangeResult.Cancelled();
            }
            catch (Exception exception)
            {
                return StageChangeResult.Failed(new StageChangeFailure(
                    StageChangeFailureCode.PreparationFailed,
                    StageChangePhase.Prepare,
                    targetStageId,
                    $"Preparing Stage '{targetStageId}' failed.",
                    exception));
            }

            if (candidate == null)
            {
                return StageChangeResult.Failed(new StageChangeFailure(
                    StageChangeFailureCode.PreparationFailed,
                    StageChangePhase.Prepare,
                    targetStageId,
                    $"Preparing Stage '{targetStageId}' returned no candidate handle."));
            }

            try
            {
                await candidate.ActivateAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                var cleanupException = await RollbackCandidateAsync(candidate);
                if (cleanupException != null)
                {
                    return StageChangeResult.Failed(new StageChangeFailure(
                        StageChangeFailureCode.UnexpectedFailure,
                        StageChangePhase.Activate,
                        targetStageId,
                        $"Cancelling Stage '{targetStageId}' failed to restore the prior Stage.",
                        cleanupException));
                }
                return StageChangeResult.Cancelled();
            }
            catch (Exception exception)
            {
                var cleanupException = await RollbackCandidateAsync(candidate);
                return StageChangeResult.Failed(new StageChangeFailure(
                    StageChangeFailureCode.ActivationFailed,
                    StageChangePhase.Activate,
                    targetStageId,
                    $"Activating Stage '{targetStageId}' failed.",
                    exception,
                    cleanupException));
            }

            var previous = current;
            current = candidate;
            if (previous != null)
            {
                try
                {
                    await previous.ReleaseAsync(CancellationToken.None);
                }
                catch (Exception exception)
                {
                    return StageChangeResult.Failed(new StageChangeFailure(
                        StageChangeFailureCode.ReleasePreviousFailed,
                        StageChangePhase.ReleasePrevious,
                        targetStageId,
                        $"Stage '{targetStageId}' committed, but releasing the previous Stage failed.",
                        exception));
                }
            }

            return StageChangeResult.Completed();
        }

        private async Task<Exception> RollbackCandidateAsync(IStageHandle candidate)
        {
            Exception restoreException = null;
            if (current != null)
            {
                try
                {
                    await current.ActivateAsync(CancellationToken.None);
                }
                catch (Exception exception)
                {
                    restoreException = exception;
                }
            }

            Exception releaseException = null;
            try
            {
                await candidate.ReleaseAsync(CancellationToken.None);
            }
            catch (Exception exception)
            {
                releaseException = exception;
            }

            if (restoreException != null && releaseException != null)
                return new AggregateException(restoreException, releaseException);
            return restoreException ?? releaseException;
        }
    }
}
