using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Levity.Narrative.Core;

namespace Levity.Narrative.Placeholder
{
    /// <summary>
    /// Production authoring backend that advertises typed outcomes and waits for an operator selection.
    /// </summary>
    public sealed class PlaceholderNarrativeBackend : INarrativeModule
    {
        private readonly object sync = new object();
        private readonly FakeNarrativeBackend backend = new FakeNarrativeBackend();
        private readonly Dictionary<NarrativeSequenceId, PlaceholderSequenceDescriptor> sequences =
            new Dictionary<NarrativeSequenceId, PlaceholderSequenceDescriptor>();
        private readonly List<NarrativeSequenceId> registrationOrder =
            new List<NarrativeSequenceId>();
        private PendingSelection active;

        public SaveAvailability SaveAvailability => backend.SaveAvailability;

        public IReadOnlyList<PlaceholderSequenceDescriptor> Sequences
        {
            get
            {
                lock (sync)
                    return registrationOrder.Select(id => sequences[id]).ToArray();
            }
        }

        public PlaceholderSequenceDescriptor ActiveSequence
        {
            get
            {
                lock (sync) return active?.Descriptor;
            }
        }

        public void RegisterSequence<TOutcome>(
            NarrativeSequenceId sequenceId,
            params TOutcome[] outcomes)
        {
            if (outcomes == null) throw new ArgumentNullException(nameof(outcomes));
            if (outcomes.Length == 0)
                throw new ArgumentException("A placeholder sequence must advertise at least one outcome.", nameof(outcomes));

            var descriptor = new PlaceholderSequenceDescriptor(
                sequenceId,
                typeof(TOutcome),
                outcomes.Cast<object>().ToArray());
            lock (sync)
            {
                if (sequences.ContainsKey(sequenceId))
                    throw new InvalidOperationException(
                        $"Placeholder Narrative Sequence '{sequenceId}' is already registered.");
                sequences.Add(sequenceId, descriptor);
                registrationOrder.Add(sequenceId);
            }

            backend.RegisterSequence<TOutcome>(
                sequenceId,
                cancellationToken => WaitForSelectionAsync<TOutcome>(descriptor, cancellationToken));
        }

        public Task<NarrativeSessionResult<TOutcome>> PlayAsync<TOutcome>(
            NarrativeRequest request,
            CancellationToken cancellationToken = default) =>
            backend.PlayAsync<TOutcome>(request, cancellationToken);

        public void SelectOutcome(object outcome)
        {
            PendingSelection selection;
            lock (sync)
            {
                selection = active ?? throw new InvalidOperationException(
                    "No placeholder Narrative Sequence is waiting for an outcome.");
                if (!selection.Descriptor.Accepts(outcome))
                {
                    throw new ArgumentException(
                        $"Outcome must be one of the advertised {selection.Descriptor.OutcomeType.Name} values.",
                        nameof(outcome));
                }
            }

            if (!selection.Completion.TrySetResult(outcome))
                throw new InvalidOperationException("The active placeholder outcome was already selected.");
        }

        private async Task<TOutcome> WaitForSelectionAsync<TOutcome>(
            PlaceholderSequenceDescriptor descriptor,
            CancellationToken cancellationToken)
        {
            var selection = new PendingSelection(descriptor);
            lock (sync)
            {
                if (active != null)
                    throw new InvalidOperationException("A placeholder Narrative Sequence is already active.");
                active = selection;
            }

            try
            {
                using (cancellationToken.Register(() => selection.Completion.TrySetCanceled()))
                    return (TOutcome)await selection.Completion.Task.ConfigureAwait(false);
            }
            finally
            {
                lock (sync)
                {
                    if (ReferenceEquals(active, selection)) active = null;
                }
            }
        }

        private sealed class PendingSelection
        {
            public PendingSelection(PlaceholderSequenceDescriptor descriptor)
            {
                Descriptor = descriptor;
                Completion = new TaskCompletionSource<object>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public PlaceholderSequenceDescriptor Descriptor { get; }
            public TaskCompletionSource<object> Completion { get; }
        }
    }

    public sealed class PlaceholderSequenceDescriptor
    {
        internal PlaceholderSequenceDescriptor(
            NarrativeSequenceId sequenceId,
            Type outcomeType,
            IReadOnlyList<object> outcomes)
        {
            SequenceId = sequenceId;
            OutcomeType = outcomeType;
            Outcomes = outcomes;
        }

        public NarrativeSequenceId SequenceId { get; }
        public Type OutcomeType { get; }
        public IReadOnlyList<object> Outcomes { get; }

        internal bool Accepts(object outcome) =>
            outcome != null &&
            OutcomeType.IsInstanceOfType(outcome) &&
            Outcomes.Contains(outcome);
    }
}
