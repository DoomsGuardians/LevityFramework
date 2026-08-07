using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Levity.Narrative.Core;

namespace Levity.Narrative.Workspace
{
    /// <summary>
    /// Runs registered narrative routes against inspectable fake game state without a Stage or Flow asset.
    /// </summary>
    public sealed class NarrativeWorkspace
    {
        private readonly Dictionary<NarrativeSequenceId, IWorkspaceSequence> sequences =
            new Dictionary<NarrativeSequenceId, IWorkspaceSequence>();

        public NarrativeWorkspace()
        {
            State = new FakeGameState();
            Commands = new FakeGameplayCommands(State);
        }

        public FakeGameState State { get; }
        public FakeGameplayCommands Commands { get; }

        public IReadOnlyList<NarrativeWorkspaceSequenceDescriptor> Sequences =>
            sequences.Values
                .Select(sequence => sequence.Descriptor)
                .OrderBy(sequence => sequence.SequenceId.Value, StringComparer.Ordinal)
                .ToArray();

        public void Register<TOutcome>(NarrativeWorkspaceSequence<TOutcome> sequence)
        {
            if (sequence == null) throw new ArgumentNullException(nameof(sequence));
            if (sequences.ContainsKey(sequence.SequenceId))
                throw new InvalidOperationException(
                    $"Narrative Workspace sequence '{sequence.SequenceId}' is already registered.");
            sequences.Add(sequence.SequenceId, sequence);
        }

        public async Task<NarrativeSessionResult<object>> PlayAsync(
            NarrativeSequenceId sequenceId,
            object outcome,
            CancellationToken cancellationToken = default)
        {
            if (!sequences.TryGetValue(sequenceId, out var sequence))
            {
                return NarrativeSessionResult<object>.Failed(new NarrativeFailure(
                    NarrativeFailureCode.SequenceNotFound,
                    $"Narrative Workspace sequence '{sequenceId}' is not registered."));
            }

            if (!sequence.Accepts(outcome))
            {
                return NarrativeSessionResult<object>.Failed(new NarrativeFailure(
                    NarrativeFailureCode.InvalidOutcome,
                    $"Outcome for '{sequenceId}' must be a registered {sequence.OutcomeType.Name} value."));
            }

            return await sequence.PlayAsync(outcome, Commands, cancellationToken).ConfigureAwait(false);
        }
    }

    public sealed class NarrativeWorkspaceSequence<TOutcome> : IWorkspaceSequence
    {
        private readonly Dictionary<TOutcome, WorkspaceBranch> branches =
            new Dictionary<TOutcome, WorkspaceBranch>();

        private NarrativeWorkspaceSequence(NarrativeSequenceId sequenceId) => SequenceId = sequenceId;

        public NarrativeSequenceId SequenceId { get; }

        public static NarrativeWorkspaceSequence<TOutcome> Create(NarrativeSequenceId sequenceId) =>
            new NarrativeWorkspaceSequence<TOutcome>(sequenceId);

        public NarrativeWorkspaceSequence<TOutcome> On<TPayload>(
            TOutcome outcome,
            string commandId,
            TPayload payload)
        {
            branches[outcome] = new WorkspaceBranch(commandId, payload, typeof(TPayload));
            return this;
        }

        Type IWorkspaceSequence.OutcomeType => typeof(TOutcome);

        NarrativeWorkspaceSequenceDescriptor IWorkspaceSequence.Descriptor =>
            new NarrativeWorkspaceSequenceDescriptor(
                SequenceId,
                typeof(TOutcome),
                branches.Keys.Cast<object>().ToArray());

        bool IWorkspaceSequence.Accepts(object outcome) =>
            outcome is TOutcome typed && branches.ContainsKey(typed);

        async Task<NarrativeSessionResult<object>> IWorkspaceSequence.PlayAsync(
            object selectedOutcome,
            FakeGameplayCommands commands,
            CancellationToken cancellationToken)
        {
            var outcome = (TOutcome)selectedOutcome;
            var backend = new FakeNarrativeBackend();
            backend.RegisterSequence(SequenceId, _ => Task.FromResult(outcome));
            var result = await backend.PlayAsync<TOutcome>(
                new NarrativeRequest(SequenceId),
                cancellationToken).ConfigureAwait(false);

            if (result.Status != NarrativeSessionStatus.Completed)
            {
                return result.Status == NarrativeSessionStatus.Cancelled
                    ? NarrativeSessionResult<object>.Cancelled()
                    : NarrativeSessionResult<object>.Failed(result.Failure);
            }

            var branch = branches[outcome];
            commands.Execute(branch.CommandId, branch.Payload, branch.PayloadType);
            return NarrativeSessionResult<object>.Completed(result.Outcome);
        }
    }

    public sealed class NarrativeWorkspaceSequenceDescriptor
    {
        internal NarrativeWorkspaceSequenceDescriptor(
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
    }

    internal interface IWorkspaceSequence
    {
        NarrativeWorkspaceSequenceDescriptor Descriptor { get; }
        Type OutcomeType { get; }
        bool Accepts(object outcome);
        Task<NarrativeSessionResult<object>> PlayAsync(
            object outcome,
            FakeGameplayCommands commands,
            CancellationToken cancellationToken);
    }

    internal sealed class WorkspaceBranch
    {
        public WorkspaceBranch(string commandId, object payload, Type payloadType)
        {
            if (string.IsNullOrWhiteSpace(commandId))
                throw new ArgumentException("A fake Gameplay Command ID cannot be empty.", nameof(commandId));
            CommandId = commandId;
            Payload = payload;
            PayloadType = payloadType;
        }

        public string CommandId { get; }
        public object Payload { get; }
        public Type PayloadType { get; }
    }
}
