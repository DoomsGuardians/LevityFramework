using System;
using System.Collections.Generic;
using System.Linq;

namespace Levity.Narrative.Flow
{
    /// <summary>Stable identity for one stateful Gameplay Command execution.</summary>
    public readonly struct GameplayCommandExecutionId : IEquatable<GameplayCommandExecutionId>
    {
        public GameplayCommandExecutionId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A Gameplay Command execution ID cannot be empty.", nameof(value));
            Value = value;
        }

        public string Value { get; }
        public bool Equals(GameplayCommandExecutionId other) =>
            StringComparer.Ordinal.Equals(Value, other.Value);
        public override bool Equals(object obj) =>
            obj is GameplayCommandExecutionId other && Equals(other);
        public override int GetHashCode() =>
            Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    /// <summary>Executes registered game-owned commands once per stable execution identity.</summary>
    public sealed class GameplayCommandExecutor
    {
        private readonly Dictionary<string, Action> commands =
            new Dictionary<string, Action>(StringComparer.Ordinal);
        private readonly HashSet<GameplayCommandExecutionId> committed =
            new HashSet<GameplayCommandExecutionId>();

        public void Register(string commandId, Action command)
        {
            if (string.IsNullOrWhiteSpace(commandId))
                throw new ArgumentException("A Gameplay Command ID cannot be empty.", nameof(commandId));
            commands[commandId] = command ?? throw new ArgumentNullException(nameof(command));
        }

        public bool Execute(string commandId, GameplayCommandExecutionId executionId)
        {
            if (committed.Contains(executionId)) return false;
            if (!commands.TryGetValue(commandId, out var command))
                throw new InvalidOperationException($"Gameplay Command '{commandId}' is not registered.");

            command();
            committed.Add(executionId);
            return true;
        }

        public IReadOnlyList<string> CaptureCommittedExecutionIds() =>
            committed.Select(id => id.Value).OrderBy(value => value, StringComparer.Ordinal).ToArray();

        public void RestoreCommittedExecutionIds(IEnumerable<string> executionIds)
        {
            if (executionIds == null) throw new ArgumentNullException(nameof(executionIds));
            committed.Clear();
            foreach (var executionId in executionIds)
                committed.Add(new GameplayCommandExecutionId(executionId));
        }
    }
}
