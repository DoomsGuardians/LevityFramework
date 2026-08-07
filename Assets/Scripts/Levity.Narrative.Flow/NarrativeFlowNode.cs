using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Levity.Narrative.Core;

namespace Levity.Narrative.Flow
{
    /// <summary>Waits for a typed Narrative Outcome and selects a game-owned branch.</summary>
    public sealed class NarrativeFlowNode<TOutcome>
    {
        private readonly NarrativeSequenceId sequenceId;
        private readonly Dictionary<TOutcome, Branch> branches = new Dictionary<TOutcome, Branch>();
        private readonly Dictionary<NarrativeFailureCode, string> failureBranches =
            new Dictionary<NarrativeFailureCode, string>();
        private string cancelledBranchId;
        private string failureBranchId;

        private NarrativeFlowNode(NarrativeSequenceId sequenceId) => this.sequenceId = sequenceId;

        public static NarrativeFlowNode<TOutcome> Create(NarrativeSequenceId sequenceId) =>
            new NarrativeFlowNode<TOutcome>(sequenceId);

        public NarrativeFlowNode<TOutcome> On(
            TOutcome outcome,
            string commandId,
            GameplayCommandExecutionId executionId,
            string branchId)
        {
            if (string.IsNullOrWhiteSpace(branchId))
                throw new ArgumentException("A Flow branch ID cannot be empty.", nameof(branchId));
            branches[outcome] = new Branch(commandId, executionId, branchId);
            return this;
        }

        public NarrativeFlowNode<TOutcome> OnCancelled(string branchId)
        {
            if (string.IsNullOrWhiteSpace(branchId))
                throw new ArgumentException("A Flow branch ID cannot be empty.", nameof(branchId));
            cancelledBranchId = branchId;
            return this;
        }

        public NarrativeFlowNode<TOutcome> OnFailed(NarrativeFailureCode code, string branchId)
        {
            if (string.IsNullOrWhiteSpace(branchId))
                throw new ArgumentException("A Flow branch ID cannot be empty.", nameof(branchId));
            failureBranches[code] = branchId;
            return this;
        }

        public NarrativeFlowNode<TOutcome> OnFailed(string branchId)
        {
            if (string.IsNullOrWhiteSpace(branchId))
                throw new ArgumentException("A Flow branch ID cannot be empty.", nameof(branchId));
            failureBranchId = branchId;
            return this;
        }

        public async Task<NarrativeFlowResult<TOutcome>> PlayAsync(
            INarrativeModule narrative,
            GameplayCommandExecutor commands,
            CancellationToken cancellationToken = default)
        {
            if (narrative == null) throw new ArgumentNullException(nameof(narrative));
            if (commands == null) throw new ArgumentNullException(nameof(commands));

            var result = await narrative.PlayAsync<TOutcome>(
                new NarrativeRequest(sequenceId), cancellationToken).ConfigureAwait(false);
            if (result.Status == NarrativeSessionStatus.Cancelled)
            {
                if (cancelledBranchId == null)
                    throw new NarrativeFlowException(sequenceId, null, "The cancelled Narrative session has no Flow branch.");
                return new NarrativeFlowResult<TOutcome>(
                    NarrativeSessionStatus.Cancelled, default, null, cancelledBranchId);
            }
            if (result.Status == NarrativeSessionStatus.Failed &&
                result.Failure != null &&
                failureBranches.TryGetValue(result.Failure.Code, out var exactFailureBranchId))
            {
                return new NarrativeFlowResult<TOutcome>(
                    NarrativeSessionStatus.Failed, default, result.Failure, exactFailureBranchId);
            }
            if (result.Status == NarrativeSessionStatus.Failed && failureBranchId != null)
            {
                return new NarrativeFlowResult<TOutcome>(
                    NarrativeSessionStatus.Failed, default, result.Failure, failureBranchId);
            }
            if (result.Status != NarrativeSessionStatus.Completed)
                throw new NarrativeFlowException(sequenceId, result.Failure);
            if (!branches.TryGetValue(result.Outcome, out var branch))
                throw new NarrativeFlowException(sequenceId, null, "The Narrative Outcome has no Flow branch.");

            commands.Execute(branch.CommandId, branch.ExecutionId);
            return new NarrativeFlowResult<TOutcome>(result.Outcome, branch.BranchId);
        }

        private readonly struct Branch
        {
            public Branch(string commandId, GameplayCommandExecutionId executionId, string branchId)
            {
                CommandId = commandId;
                ExecutionId = executionId;
                BranchId = branchId;
            }

            public string CommandId { get; }
            public GameplayCommandExecutionId ExecutionId { get; }
            public string BranchId { get; }
        }
    }

    public readonly struct NarrativeFlowResult<TOutcome>
    {
        public NarrativeFlowResult(TOutcome outcome, string branchId)
            : this(NarrativeSessionStatus.Completed, outcome, null, branchId)
        {
        }

        public NarrativeFlowResult(
            NarrativeSessionStatus status,
            TOutcome outcome,
            NarrativeFailure failure,
            string branchId)
        {
            Status = status;
            Outcome = outcome;
            Failure = failure;
            BranchId = branchId;
        }

        public NarrativeSessionStatus Status { get; }
        public TOutcome Outcome { get; }
        public NarrativeFailure Failure { get; }
        public string BranchId { get; }
    }

    public sealed class NarrativeFlowException : InvalidOperationException
    {
        public NarrativeFlowException(
            NarrativeSequenceId sequenceId,
            NarrativeFailure failure,
            string message = null)
            : base(message ?? failure?.Message ?? $"Narrative sequence '{sequenceId}' did not complete.", failure?.Exception)
        {
            SequenceId = sequenceId;
            Failure = failure;
        }

        public NarrativeSequenceId SequenceId { get; }
        public NarrativeFailure Failure { get; }
    }
}
