using NUnit.Framework;

namespace Levity.Stage.Tests
{
    public sealed class StageRegistryTests
    {
        [Test]
        public void DuplicateRegistrationFailsAndPreservesTheOriginalDescriptor()
        {
            var stageId = new StageId("mission-one");
            var original = new StageDescriptor(stageId, "Scenes/MissionOne");
            var registry = new StageRegistry();
            registry.Register(original);

            var exception = Assert.Throws<DuplicateStageRegistrationException>(() =>
                registry.Register(new StageDescriptor(stageId, "Scenes/AccidentalReplacement")));

            Assert.That(exception.StageId, Is.EqualTo(stageId));
            Assert.That(registry.Resolve(stageId).Descriptor, Is.SameAs(original));
        }

        [Test]
        public void ExplicitReplacementChangesTheRegisteredDescriptor()
        {
            var stageId = new StageId("mission-one");
            var replacement = new StageDescriptor(stageId, "Scenes/MissionOneRevised");
            var registry = new StageRegistry();
            registry.Register(new StageDescriptor(stageId, "Scenes/MissionOne"));

            registry.Replace(replacement);

            Assert.That(registry.Resolve(stageId).Descriptor, Is.SameAs(replacement));
        }

        [Test]
        public void UnknownStageIdReturnsATypedNotFoundFailure()
        {
            var missingStageId = new StageId("missing-stage");
            var registry = new StageRegistry();

            var result = registry.Resolve(missingStageId);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Descriptor, Is.Null);
            Assert.That(result.Failure.Code, Is.EqualTo(StageResolutionFailureCode.StageNotFound));
            Assert.That(result.Failure.StageId, Is.EqualTo(missingStageId));
        }

        [Test]
        public void LegacySlotAndGameModeResolveTheDescriptorMappedByStageId()
        {
            var stageId = new StageId("mission-one");
            var descriptor = new StageDescriptor(stageId, "Scenes/MissionOne");
            var registry = new StageRegistry();
            registry.Register(descriptor);
            var compatibility = new Compatibility.LegacyStageCompatibility(registry);
            compatibility.MapSlot(7, stageId);
            compatibility.MapMode(GameMode.GamePlay, stageId);

#pragma warning disable CS0618
            var slotResult = compatibility.ResolveSlot(7);
            var modeResult = compatibility.ResolveMode(GameMode.GamePlay);
#pragma warning restore CS0618

            Assert.That(slotResult.Descriptor, Is.SameAs(descriptor));
            Assert.That(modeResult.Descriptor, Is.SameAs(descriptor));
        }

        [Test]
        public void UnmappedLegacyValuesReturnTypedFailures()
        {
            var compatibility = new Compatibility.LegacyStageCompatibility(new StageRegistry());

#pragma warning disable CS0618
            var slotResult = compatibility.ResolveSlot(404);
            var modeResult = compatibility.ResolveMode(GameMode.Training);
#pragma warning restore CS0618

            Assert.That(slotResult.IsSuccess, Is.False);
            Assert.That(
                slotResult.Failure.Code,
                Is.EqualTo(Compatibility.LegacyStageResolutionFailureCode.SlotNotMapped));
            Assert.That(slotResult.Failure.LegacyValue, Is.EqualTo("404"));
            Assert.That(modeResult.IsSuccess, Is.False);
            Assert.That(
                modeResult.Failure.Code,
                Is.EqualTo(Compatibility.LegacyStageResolutionFailureCode.ModeNotMapped));
            Assert.That(modeResult.Failure.LegacyValue, Is.EqualTo("Training"));
        }
    }
}
