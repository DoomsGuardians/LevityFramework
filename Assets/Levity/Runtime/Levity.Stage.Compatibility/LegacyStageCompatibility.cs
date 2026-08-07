using System;
using System.Collections.Generic;

namespace Levity.Stage.Compatibility
{
    public enum LegacyStageResolutionFailureCode
    {
        SlotNotMapped,
        ModeNotMapped,
        StageNotFound
    }

    public sealed class LegacyStageResolutionFailure
    {
        internal LegacyStageResolutionFailure(
            LegacyStageResolutionFailureCode code,
            string legacyValue,
            StageId? mappedStageId = null)
        {
            Code = code;
            LegacyValue = legacyValue;
            MappedStageId = mappedStageId;
        }

        public LegacyStageResolutionFailureCode Code { get; }
        public string LegacyValue { get; }
        public StageId? MappedStageId { get; }
    }

    public readonly struct LegacyStageResolutionResult
    {
        private LegacyStageResolutionResult(
            StageDescriptor descriptor,
            LegacyStageResolutionFailure failure)
        {
            Descriptor = descriptor;
            Failure = failure;
        }

        public bool IsSuccess => Descriptor != null;
        public StageDescriptor Descriptor { get; }
        public LegacyStageResolutionFailure Failure { get; }

        internal static LegacyStageResolutionResult Found(StageDescriptor descriptor) =>
            new LegacyStageResolutionResult(descriptor, null);

        internal static LegacyStageResolutionResult NotMapped(
            LegacyStageResolutionFailureCode code,
            string legacyValue) =>
            new LegacyStageResolutionResult(
                null,
                new LegacyStageResolutionFailure(code, legacyValue));

        internal static LegacyStageResolutionResult TargetNotFound(
            string legacyValue,
            StageId stageId) =>
            new LegacyStageResolutionResult(
                null,
                new LegacyStageResolutionFailure(
                    LegacyStageResolutionFailureCode.StageNotFound,
                    legacyValue,
                    stageId));
    }

    /// <summary>
    /// Maps legacy application Stage slots and game modes to strong Stage IDs.
    /// New callers should resolve a StageId directly through StageRegistry.
    /// </summary>
    public sealed class LegacyStageCompatibility
    {
        private readonly StageRegistry registry;
        private readonly Dictionary<int, StageId> slots = new Dictionary<int, StageId>();
        private readonly Dictionary<GameMode, StageId> modes =
            new Dictionary<GameMode, StageId>();

        public LegacyStageCompatibility(StageRegistry registry)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public void MapSlot(int slot, StageId stageId) => slots[slot] = stageId;

        public void MapMode(GameMode mode, StageId stageId) => modes[mode] = stageId;

        [Obsolete(
            "Legacy integer Stage slots are deprecated. Resolve a StageId through StageRegistry instead.")]
        public LegacyStageResolutionResult ResolveSlot(int slot)
        {
            var legacyValue = slot.ToString();
            if (!slots.TryGetValue(slot, out var stageId))
                return LegacyStageResolutionResult.NotMapped(
                    LegacyStageResolutionFailureCode.SlotNotMapped,
                    legacyValue);

            return ResolveMapped(stageId, legacyValue);
        }

        [Obsolete(
            "Legacy GameMode Stage lookup is deprecated. Resolve a StageId through StageRegistry instead.")]
        public LegacyStageResolutionResult ResolveMode(GameMode mode)
        {
            var legacyValue = mode.ToString();
            if (!modes.TryGetValue(mode, out var stageId))
                return LegacyStageResolutionResult.NotMapped(
                    LegacyStageResolutionFailureCode.ModeNotMapped,
                    legacyValue);

            return ResolveMapped(stageId, legacyValue);
        }

        private LegacyStageResolutionResult ResolveMapped(StageId stageId, string legacyValue)
        {
            var result = registry.Resolve(stageId);
            return result.IsSuccess
                ? LegacyStageResolutionResult.Found(result.Descriptor)
                : LegacyStageResolutionResult.TargetNotFound(legacyValue, stageId);
        }
    }
}
