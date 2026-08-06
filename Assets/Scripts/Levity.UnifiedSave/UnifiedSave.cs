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
            var record = await store.ReadAsync(slotId, cancellationToken).ConfigureAwait(false);
            var savedById = record.Contributions.ToDictionary(item => item.Id, StringComparer.Ordinal);

            if (savedById.Count != contributors.Count || contributors.Keys.Any(id => !savedById.ContainsKey(id)))
                throw new UnifiedSaveException("The save slot does not contain the complete contributor set.");

            foreach (var contributor in contributors.Values.OrderBy(item => item.Id, StringComparer.Ordinal))
            {
                var contribution = savedById[contributor.Id];
                await contributor.RestoreAsync(
                    contribution.Version,
                    contribution.State,
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public sealed class UnifiedSaveException : InvalidOperationException
    {
        public UnifiedSaveException(string message, Exception innerException = null)
            : base(message, innerException) { }
    }
}
