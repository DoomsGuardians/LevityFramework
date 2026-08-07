using System;
using System.Collections.Generic;

namespace Levity.Stage
{
    public readonly struct StageId : IEquatable<StageId>
    {
        public StageId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A Stage ID cannot be empty.", nameof(value));

            Value = value;
        }

        public string Value { get; }

        public bool Equals(StageId other) =>
            StringComparer.Ordinal.Equals(Value, other.Value);

        public override bool Equals(object obj) =>
            obj is StageId other && Equals(other);

        public override int GetHashCode() =>
            Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);

        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(StageId left, StageId right) => left.Equals(right);
        public static bool operator !=(StageId left, StageId right) => !left.Equals(right);
    }

    public sealed class StageDescriptor
    {
        public StageDescriptor(StageId id, string loadKey)
        {
            if (string.IsNullOrWhiteSpace(loadKey))
                throw new ArgumentException("A Stage load key cannot be empty.", nameof(loadKey));

            Id = id;
            LoadKey = loadKey;
        }

        public StageId Id { get; }
        public string LoadKey { get; }
    }

    public sealed class DuplicateStageRegistrationException : InvalidOperationException
    {
        public DuplicateStageRegistrationException(StageId stageId)
            : base($"Stage ID '{stageId}' is already registered.")
        {
            StageId = stageId;
        }

        public StageId StageId { get; }
    }

    public enum StageResolutionFailureCode
    {
        StageNotFound
    }

    public sealed class StageResolutionFailure
    {
        internal StageResolutionFailure(StageResolutionFailureCode code, StageId stageId)
        {
            Code = code;
            StageId = stageId;
        }

        public StageResolutionFailureCode Code { get; }
        public StageId StageId { get; }
    }

    public readonly struct StageResolutionResult
    {
        private StageResolutionResult(
            StageDescriptor descriptor,
            StageResolutionFailure failure)
        {
            Descriptor = descriptor;
            Failure = failure;
        }

        public bool IsSuccess => Descriptor != null;
        public StageDescriptor Descriptor { get; }
        public StageResolutionFailure Failure { get; }

        internal static StageResolutionResult Found(StageDescriptor descriptor) =>
            new StageResolutionResult(descriptor, null);

        internal static StageResolutionResult NotFound(StageId stageId) =>
            new StageResolutionResult(
                null,
                new StageResolutionFailure(StageResolutionFailureCode.StageNotFound, stageId));
    }

    public sealed class StageRegistry
    {
        private readonly Dictionary<StageId, StageDescriptor> descriptors =
            new Dictionary<StageId, StageDescriptor>();

        public void Register(StageDescriptor descriptor)
        {
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));
            if (descriptors.ContainsKey(descriptor.Id))
                throw new DuplicateStageRegistrationException(descriptor.Id);

            descriptors.Add(descriptor.Id, descriptor);
        }

        public void Replace(StageDescriptor descriptor)
        {
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));
            descriptors[descriptor.Id] = descriptor;
        }

        public StageResolutionResult Resolve(StageId stageId) =>
            descriptors.TryGetValue(stageId, out var descriptor)
                ? StageResolutionResult.Found(descriptor)
                : StageResolutionResult.NotFound(stageId);
    }
}
