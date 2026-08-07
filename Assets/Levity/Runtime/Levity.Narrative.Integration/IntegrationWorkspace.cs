using System;
using System.Threading;
using System.Threading.Tasks;
using Levity.Narrative.Core;
using Levity.Narrative.Flow;
using Levity.Narrative.Naninovel;
using Levity.Narrative.Placeholder;

namespace Levity.Narrative.Integration
{
    public enum IntegrationNarrativeBackend
    {
        Placeholder,
        Naninovel
    }

    public enum IntegrationWorkspacePlayStatus
    {
        Completed,
        ValidationFailed
    }

    public enum IntegrationValidationFailureCode
    {
        MissingBackendMapping
    }

    public sealed class IntegrationValidationFailure
    {
        internal IntegrationValidationFailure(
            IntegrationValidationFailureCode code,
            IntegrationNarrativeBackend backend,
            NarrativeSequenceId sequenceId,
            string message)
        {
            Code = code;
            Backend = backend;
            SequenceId = sequenceId;
            Message = message;
        }

        public IntegrationValidationFailureCode Code { get; }
        public IntegrationNarrativeBackend Backend { get; }
        public NarrativeSequenceId SequenceId { get; }
        public string Message { get; }
    }

    /// <summary>Runs unchanged Flow data against either supported Narrative Backend.</summary>
    public sealed class IntegrationWorkspace
    {
        private readonly PlaceholderNarrativeBackend placeholder;
        private readonly NaninovelNarrativeBackend naninovel;
        private IntegrationNarrativeBackend selectedBackend;

        public IntegrationWorkspace(
            PlaceholderNarrativeBackend placeholder,
            NaninovelNarrativeBackend naninovel)
        {
            this.placeholder = placeholder ?? throw new ArgumentNullException(nameof(placeholder));
            this.naninovel = naninovel ?? throw new ArgumentNullException(nameof(naninovel));
        }

        public IntegrationNarrativeBackend SelectedBackend => selectedBackend;

        public void UseBackend(IntegrationNarrativeBackend backend) => selectedBackend = backend;

        public async Task<IntegrationWorkspacePlayResult<TOutcome>> PlayAsync<TOutcome>(
            NarrativeFlowNode<TOutcome> flow,
            GameplayCommandExecutor commands,
            CancellationToken cancellationToken = default)
        {
            if (flow == null) throw new ArgumentNullException(nameof(flow));
            if (commands == null) throw new ArgumentNullException(nameof(commands));

            INarrativeModule backend;
            INarrativeSequenceMapping mapping;
            if (selectedBackend == IntegrationNarrativeBackend.Placeholder)
            {
                backend = placeholder;
                mapping = placeholder;
            }
            else
            {
                backend = naninovel;
                mapping = naninovel;
            }

            if (!mapping.Contains(flow.SequenceId))
            {
                return IntegrationWorkspacePlayResult<TOutcome>.ValidationFailed(
                    new IntegrationValidationFailure(
                        IntegrationValidationFailureCode.MissingBackendMapping,
                        selectedBackend,
                        flow.SequenceId,
                        $"Narrative Sequence '{flow.SequenceId}' has no {selectedBackend} backend mapping."));
            }

            var result = await flow.PlayAsync(backend, commands, cancellationToken)
                .ConfigureAwait(false);
            return IntegrationWorkspacePlayResult<TOutcome>.Completed(result);
        }
    }

    public readonly struct IntegrationWorkspacePlayResult<TOutcome>
    {
        private IntegrationWorkspacePlayResult(
            IntegrationWorkspacePlayStatus status,
            NarrativeFlowResult<TOutcome> flow,
            IntegrationValidationFailure failure,
            bool flowStarted)
        {
            Status = status;
            Flow = flow;
            Failure = failure;
            FlowStarted = flowStarted;
        }

        public IntegrationWorkspacePlayStatus Status { get; }
        public NarrativeFlowResult<TOutcome> Flow { get; }
        public IntegrationValidationFailure Failure { get; }
        public bool FlowStarted { get; }

        internal static IntegrationWorkspacePlayResult<TOutcome> Completed(
            NarrativeFlowResult<TOutcome> flow) =>
            new IntegrationWorkspacePlayResult<TOutcome>(
                IntegrationWorkspacePlayStatus.Completed,
                flow,
                null,
                true);

        internal static IntegrationWorkspacePlayResult<TOutcome> ValidationFailed(
            IntegrationValidationFailure failure) =>
            new IntegrationWorkspacePlayResult<TOutcome>(
                IntegrationWorkspacePlayStatus.ValidationFailed,
                default,
                failure,
                false);
    }
}
