using Levity.Narrative.Runtime;
using UnityEngine;

namespace Levity.Narrative.Naninovel
{
    /// <summary>Creates and registers the production Naninovel runtime contribution.</summary>
    public static class NaninovelRuntimeBinding
    {
        public static NarrativeRuntimeBinding CreateDefault()
        {
            var registry = NaninovelSequenceCatalog.CreateDefault();
            var module = new NaninovelNarrativeBackend(registry, new NaninovelRuntimePlayer());
            return new NarrativeRuntimeBinding(module, new NaninovelUnifiedSaveContributor());
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() => NarrativeRuntime.Register(CreateDefault);
    }
}
