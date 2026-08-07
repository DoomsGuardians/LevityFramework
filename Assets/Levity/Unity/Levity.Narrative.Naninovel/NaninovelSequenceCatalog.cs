using Levity.Narrative.Core;

namespace Levity.Narrative.Naninovel
{
    /// <summary>Registers production-owned stable sequence mappings.</summary>
    public static class NaninovelSequenceCatalog
    {
        public static NarrativeSequenceRegistry CreateDefault()
        {
            var registry = new NarrativeSequenceRegistry();
            registry.Register(
                new NarrativeSequenceId("stage.mission.accept"),
                new NaninovelSequence("TracerBullet"));
            return registry;
        }
    }
}
