using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Levity.Narrative.Flow;
using Levity.Narrative.Placeholder;

namespace Levity.Stage.Workspace
{
    /// <summary>Loads a registered Stage transactionally, then runs its Flow with operator-selected outcomes.</summary>
    public sealed class StageWorkspace
    {
        private readonly StageRegistry registry;
        private readonly StageConductor conductor;

        public StageWorkspace(
            StageRegistry registry,
            IStageLoader loader,
            PlaceholderNarrativeBackend narrative,
            IStageHandle current = null)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
            Narrative = narrative ?? throw new ArgumentNullException(nameof(narrative));
            conductor = new StageConductor(registry, loader, current);
        }

        public IReadOnlyList<StageDescriptor> Stages => registry.Descriptors;
        public StageDescriptor CurrentStage => conductor.CurrentStage;
        public PlaceholderNarrativeBackend Narrative { get; }

        public async Task<StageWorkspacePlayResult<TOutcome>> PlayAsync<TOutcome>(
            StageId stageId,
            NarrativeFlowNode<TOutcome> flow,
            GameplayCommandExecutor commands,
            CancellationToken cancellationToken = default)
        {
            if (flow == null) throw new ArgumentNullException(nameof(flow));
            if (commands == null) throw new ArgumentNullException(nameof(commands));

            var stageChange = await conductor.ChangeAsync(stageId, cancellationToken)
                .ConfigureAwait(false);
            if (stageChange.Status != StageChangeStatus.Completed)
                return new StageWorkspacePlayResult<TOutcome>(stageChange, default, false);

            var flowResult = await flow.PlayAsync(Narrative, commands, cancellationToken)
                .ConfigureAwait(false);
            return new StageWorkspacePlayResult<TOutcome>(stageChange, flowResult, true);
        }
    }

    public readonly struct StageWorkspacePlayResult<TOutcome>
    {
        internal StageWorkspacePlayResult(
            StageChangeResult stageChange,
            NarrativeFlowResult<TOutcome> flow,
            bool flowStarted)
        {
            StageChange = stageChange;
            Flow = flow;
            FlowStarted = flowStarted;
        }

        public StageChangeResult StageChange { get; }
        public NarrativeFlowResult<TOutcome> Flow { get; }
        public bool FlowStarted { get; }
    }
}
