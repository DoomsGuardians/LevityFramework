using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Levity.Narrative.Core
{
    public interface INarrativeModule
    {
        SaveAvailability SaveAvailability { get; }

        Task<NarrativeSessionResult<TOutcome>> PlayAsync<TOutcome>(
            NarrativeRequest request,
            CancellationToken cancellationToken = default);
    }

    public readonly struct NarrativeSequenceId : IEquatable<NarrativeSequenceId>
    {
        public NarrativeSequenceId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A narrative sequence ID cannot be empty.", nameof(value));

            Value = value;
        }

        public string Value { get; }

        public bool Equals(NarrativeSequenceId other) =>
            StringComparer.Ordinal.Equals(Value, other.Value);

        public override bool Equals(object obj) =>
            obj is NarrativeSequenceId other && Equals(other);

        public override int GetHashCode() =>
            Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);

        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(NarrativeSequenceId left, NarrativeSequenceId right) => left.Equals(right);
        public static bool operator !=(NarrativeSequenceId left, NarrativeSequenceId right) => !left.Equals(right);
    }

    public sealed class NarrativeRequest
    {
        private static readonly IReadOnlyDictionary<string, object> NoParameters =
            new Dictionary<string, object>();

        public NarrativeRequest(
            NarrativeSequenceId sequenceId,
            string entryPoint = null,
            IReadOnlyDictionary<string, object> parameters = null,
            ConcurrentRequestPolicy concurrentPolicy = ConcurrentRequestPolicy.Reject)
        {
            SequenceId = sequenceId;
            EntryPoint = entryPoint;
            Parameters = parameters ?? NoParameters;
            ConcurrentPolicy = concurrentPolicy;
        }

        public NarrativeSequenceId SequenceId { get; }
        public string EntryPoint { get; }
        public IReadOnlyDictionary<string, object> Parameters { get; }
        public ConcurrentRequestPolicy ConcurrentPolicy { get; }
    }

    public enum ConcurrentRequestPolicy
    {
        Reject,
        Wait,
        Replace,
        Cancel
    }

    public enum NarrativeSessionStatus
    {
        Completed,
        Cancelled,
        Failed
    }

    public enum NarrativeFailureCode
    {
        ConcurrentSession,
        SequenceNotFound,
        InvalidOutcome,
        BackendUnavailable,
        BackendFailure
    }

    public sealed class NarrativeFailure
    {
        public NarrativeFailure(NarrativeFailureCode code, string message, Exception exception = null)
        {
            Code = code;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Exception = exception;
        }

        public NarrativeFailureCode Code { get; }
        public string Message { get; }
        public Exception Exception { get; }
    }

    public readonly struct NarrativeSessionResult<TOutcome>
    {
        private NarrativeSessionResult(
            NarrativeSessionStatus status,
            TOutcome outcome,
            NarrativeFailure failure)
        {
            Status = status;
            Outcome = outcome;
            Failure = failure;
        }

        public NarrativeSessionStatus Status { get; }
        public TOutcome Outcome { get; }
        public NarrativeFailure Failure { get; }

        public static NarrativeSessionResult<TOutcome> Completed(TOutcome outcome) =>
            new NarrativeSessionResult<TOutcome>(NarrativeSessionStatus.Completed, outcome, null);

        public static NarrativeSessionResult<TOutcome> Cancelled() =>
            new NarrativeSessionResult<TOutcome>(NarrativeSessionStatus.Cancelled, default, null);

        public static NarrativeSessionResult<TOutcome> Failed(NarrativeFailure failure) =>
            new NarrativeSessionResult<TOutcome>(
                NarrativeSessionStatus.Failed,
                default,
                failure ?? throw new ArgumentNullException(nameof(failure)));
    }

    public readonly struct SaveAvailability
    {
        private SaveAvailability(bool canSave, string blockedReason)
        {
            CanSave = canSave;
            BlockedReason = blockedReason;
        }

        public bool CanSave { get; }
        public string BlockedReason { get; }

        public static SaveAvailability Allowed => new SaveAvailability(true, null);

        public static SaveAvailability Blocked(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("A blocked save must provide a reason.", nameof(reason));

            return new SaveAvailability(false, reason);
        }
    }
}
