using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Levity.UnifiedSave
{
    public interface IUnifiedSaveContributor
    {
        string Id { get; }
        int Version { get; }
        Task<string> CaptureAsync(CancellationToken cancellationToken = default);
        Task RestoreAsync(int version, string state, CancellationToken cancellationToken = default);
    }

    /// <summary>Coordinates compatible contributor state into one committed save slot.</summary>
    public sealed class UnifiedSave
    {
        private readonly IUnifiedSaveStore store;
        private readonly IReadOnlyDictionary<string, IUnifiedSaveContributor> contributors;

        public UnifiedSave(IUnifiedSaveStore store, params IUnifiedSaveContributor[] contributors)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            if (contributors == null) throw new ArgumentNullException(nameof(contributors));

            var byId = new Dictionary<string, IUnifiedSaveContributor>(StringComparer.Ordinal);
            foreach (var contributor in contributors)
            {
                if (contributor == null) throw new ArgumentException("A Unified Save contributor cannot be null.");
                if (string.IsNullOrWhiteSpace(contributor.Id))
                    throw new ArgumentException("A Unified Save contributor ID cannot be empty.");
                if (contributor.Version <= 0)
                    throw new ArgumentException($"Contributor '{contributor.Id}' must have a positive version.");
                if (byId.ContainsKey(contributor.Id))
                    throw new ArgumentException($"Duplicate Unified Save contributor ID '{contributor.Id}'.");
                byId.Add(contributor.Id, contributor);
            }
            this.contributors = byId;
        }

        public async Task SaveAsync(string slotId, CancellationToken cancellationToken = default)
        {
            var contributions = new List<UnifiedSaveContribution>(contributors.Count);
            foreach (var contributor in contributors.Values.OrderBy(item => item.Id, StringComparer.Ordinal))
            {
                try
                {
                    var state = await contributor.CaptureAsync(cancellationToken).ConfigureAwait(false);
                    contributions.Add(new UnifiedSaveContribution(
                        contributor.Id,
                        contributor.Version,
                        state));
                }
                catch (Exception exception) when (!(exception is OperationCanceledException))
                {
                    throw new UnifiedSaveException(
                        $"Contributor '{contributor.Id}' failed to capture save state.",
                        exception);
                }
            }

            try
            {
                await store.ReplaceAsync(
                    slotId,
                    new UnifiedSaveRecord(contributions),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (!(exception is OperationCanceledException))
            {
                throw new UnifiedSaveException($"Failed to commit Unified Save slot '{slotId}'.", exception);
            }
        }

        public async Task LoadAsync(string slotId, CancellationToken cancellationToken = default)
        {
            var result = await TryLoadAsync(slotId, cancellationToken).ConfigureAwait(false);
            if (result.Status == UnifiedLoadStatus.Loaded) return;
            var inner = result.RollbackFailure == null
                ? result.Failure
                : new AggregateException(result.Failure, result.RollbackFailure);
            throw new UnifiedSaveException(result.Message, inner);
        }

        public async Task<UnifiedLoadResult> TryLoadAsync(
            string slotId,
            CancellationToken cancellationToken = default)
        {
            UnifiedSaveRecord record;
            Dictionary<string, UnifiedSaveContribution> savedById;
            var ordered = contributors.Values.OrderBy(item => item.Id, StringComparer.Ordinal).ToArray();
            var currentById = new Dictionary<string, UnifiedSaveContribution>(StringComparer.Ordinal);
            try
            {
                record = await store.ReadAsync(slotId, cancellationToken).ConfigureAwait(false);
                savedById = record.Contributions.ToDictionary(item => item.Id, StringComparer.Ordinal);
                if (savedById.Count != contributors.Count || contributors.Keys.Any(id => !savedById.ContainsKey(id)))
                    throw new UnifiedSaveException("The save slot does not contain the complete contributor set.");

                foreach (var contributor in ordered)
                {
                    currentById.Add(contributor.Id, new UnifiedSaveContribution(
                        contributor.Id,
                        contributor.Version,
                        await contributor.CaptureAsync(cancellationToken).ConfigureAwait(false)));
                }
            }
            catch (Exception exception) when (!(exception is OperationCanceledException))
            {
                return UnifiedLoadResult.FailedBeforeRestore(exception);
            }

            var attempted = new List<IUnifiedSaveContributor>(ordered.Length);
            try
            {
                foreach (var contributor in ordered)
                {
                    attempted.Add(contributor);
                    var contribution = savedById[contributor.Id];
                    await contributor.RestoreAsync(
                        contribution.Version,
                        contribution.State,
                        cancellationToken).ConfigureAwait(false);
                }
                return UnifiedLoadResult.Loaded();
            }
            catch (Exception restoreFailure) when (!(restoreFailure is OperationCanceledException))
            {
                var rollbackFailures = new List<Exception>();
                for (var index = attempted.Count - 1; index >= 0; index--)
                {
                    var contributor = attempted[index];
                    var current = currentById[contributor.Id];
                    try
                    {
                        await contributor.RestoreAsync(
                            current.Version,
                            current.State,
                            CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception rollbackFailure)
                    {
                        rollbackFailures.Add(rollbackFailure);
                    }
                }
                return rollbackFailures.Count == 0
                    ? UnifiedLoadResult.FailedRolledBack(restoreFailure)
                    : UnifiedLoadResult.FailedRollback(
                        restoreFailure,
                        new AggregateException("One or more contributors failed to roll back.", rollbackFailures));
            }
        }
    }

    public enum UnifiedLoadStatus
    {
        Loaded,
        FailedBeforeRestore,
        FailedRolledBack,
        FailedRollback
    }

    public readonly struct UnifiedLoadResult
    {
        private UnifiedLoadResult(
            UnifiedLoadStatus status,
            string message,
            Exception failure,
            Exception rollbackFailure)
        {
            Status = status;
            Message = message;
            Failure = failure;
            RollbackFailure = rollbackFailure;
        }

        public UnifiedLoadStatus Status { get; }
        public string Message { get; }
        public Exception Failure { get; }
        public Exception RollbackFailure { get; }

        public static UnifiedLoadResult Loaded() =>
            new UnifiedLoadResult(UnifiedLoadStatus.Loaded, null, null, null);
        public static UnifiedLoadResult FailedBeforeRestore(Exception failure) =>
            new UnifiedLoadResult(UnifiedLoadStatus.FailedBeforeRestore,
                "Unified Save failed before restore began.", failure, null);
        public static UnifiedLoadResult FailedRolledBack(Exception failure) =>
            new UnifiedLoadResult(UnifiedLoadStatus.FailedRolledBack,
                "Unified Save restore failed and prior state was restored.", failure, null);
        public static UnifiedLoadResult FailedRollback(Exception failure, Exception rollbackFailure) =>
            new UnifiedLoadResult(UnifiedLoadStatus.FailedRollback,
                "Unified Save restore failed and prior state could not be fully restored.",
                failure, rollbackFailure);
    }

    public sealed class UnifiedSaveException : InvalidOperationException
    {
        public UnifiedSaveException(string message, Exception innerException = null)
            : base(message, innerException) { }
    }
}
