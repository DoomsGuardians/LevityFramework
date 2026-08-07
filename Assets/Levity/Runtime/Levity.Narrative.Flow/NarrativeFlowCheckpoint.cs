using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Levity.Narrative.Core;

namespace Levity.Narrative.Flow
{
    /// <summary>In-memory tracer state contributed later to Unified Save orchestration.</summary>
    public sealed class NarrativeFlowCheckpoint
    {
        private readonly string narrativeState;
        private readonly IReadOnlyList<string> committedExecutionIds;

        private NarrativeFlowCheckpoint(
            string narrativeState,
            IReadOnlyList<string> committedExecutionIds)
        {
            this.narrativeState = narrativeState;
            this.committedExecutionIds = committedExecutionIds;
        }

        public static async Task<NarrativeFlowCheckpoint> CaptureAsync(
            INarrativeCheckpointStore narrative,
            GameplayCommandExecutor commands,
            CancellationToken cancellationToken = default)
        {
            if (narrative == null) throw new ArgumentNullException(nameof(narrative));
            if (commands == null) throw new ArgumentNullException(nameof(commands));
            var state = await narrative.CaptureCheckpointAsync(cancellationToken).ConfigureAwait(false);
            return new NarrativeFlowCheckpoint(state, commands.CaptureCommittedExecutionIds());
        }

        public async Task RestoreAsync(
            INarrativeCheckpointStore narrative,
            GameplayCommandExecutor commands,
            CancellationToken cancellationToken = default)
        {
            if (narrative == null) throw new ArgumentNullException(nameof(narrative));
            if (commands == null) throw new ArgumentNullException(nameof(commands));
            await narrative.RestoreCheckpointAsync(narrativeState, cancellationToken).ConfigureAwait(false);
            commands.RestoreCommittedExecutionIds(committedExecutionIds);
        }
    }
}
