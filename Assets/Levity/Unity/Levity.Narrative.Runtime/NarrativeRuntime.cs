using System;
using Levity.Narrative.Core;
using Levity.UnifiedSave;

namespace Levity.Narrative.Runtime
{
    /// <summary>The complete optional runtime contribution of one Narrative Backend.</summary>
    public sealed class NarrativeRuntimeBinding
    {
        public NarrativeRuntimeBinding(
            INarrativeModule module,
            IUnifiedSaveContributor saveContributor)
        {
            Module = module ?? throw new ArgumentNullException(nameof(module));
            SaveContributor = saveContributor ??
                throw new ArgumentNullException(nameof(saveContributor));
        }

        public INarrativeModule Module { get; }
        public IUnifiedSaveContributor SaveContributor { get; }
    }

    /// <summary>Discovers an installed optional Narrative Backend without referencing its adapter.</summary>
    public static class NarrativeRuntime
    {
        private static Func<NarrativeRuntimeBinding> createBinding;

        public static void Register(Func<NarrativeRuntimeBinding> factory) =>
            createBinding = factory ?? throw new ArgumentNullException(nameof(factory));

        public static bool TryCreate(out NarrativeRuntimeBinding binding)
        {
            binding = createBinding?.Invoke();
            return binding != null;
        }
    }
}
