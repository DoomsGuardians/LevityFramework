using System;
using System.Collections.Generic;

namespace Levity.Narrative.Workspace
{
    public sealed class FakeGameState
    {
        private readonly Dictionary<string, object> values =
            new Dictionary<string, object>(StringComparer.Ordinal);

        public void Set<T>(string key, T value)
        {
            ValidateKey(key);
            values[key] = value;
        }

        public T Get<T>(string key)
        {
            ValidateKey(key);
            if (!values.TryGetValue(key, out var value))
                throw new KeyNotFoundException($"Fake game-state value '{key}' is not configured.");
            if (value is T typed) return typed;
            throw new InvalidOperationException(
                $"Fake game-state value '{key}' is {value?.GetType().Name ?? "null"}, not {typeof(T).Name}.");
        }

        private static void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("A fake game-state key cannot be empty.", nameof(key));
        }
    }

    public sealed class FakeGameplayCommands
    {
        private readonly FakeGameState state;
        private readonly Dictionary<string, ICommandHandler> handlers =
            new Dictionary<string, ICommandHandler>(StringComparer.Ordinal);
        private readonly List<FakeGameplayCommandInvocation> invocations =
            new List<FakeGameplayCommandInvocation>();

        internal FakeGameplayCommands(FakeGameState state) => this.state = state;

        public IReadOnlyList<FakeGameplayCommandInvocation> Invocations => invocations;

        public void Register<TPayload>(string commandId, Action<FakeGameState, TPayload> handler)
        {
            ValidateCommandId(commandId);
            handlers[commandId] = new CommandHandler<TPayload>(
                handler ?? throw new ArgumentNullException(nameof(handler)));
        }

        internal void Execute(string commandId, object payload, Type payloadType)
        {
            if (!handlers.TryGetValue(commandId, out var handler))
                throw new InvalidOperationException(
                    $"Fake Gameplay Command '{commandId}' is not registered.");
            if (handler.PayloadType != payloadType)
                throw new InvalidOperationException(
                    $"Fake Gameplay Command '{commandId}' expects {handler.PayloadType.Name}, not {payloadType.Name}.");

            handler.Execute(state, payload);
            invocations.Add(new FakeGameplayCommandInvocation(commandId, payload, payloadType));
        }

        private static void ValidateCommandId(string commandId)
        {
            if (string.IsNullOrWhiteSpace(commandId))
                throw new ArgumentException("A fake Gameplay Command ID cannot be empty.", nameof(commandId));
        }

        private interface ICommandHandler
        {
            Type PayloadType { get; }
            void Execute(FakeGameState state, object payload);
        }

        private sealed class CommandHandler<TPayload> : ICommandHandler
        {
            private readonly Action<FakeGameState, TPayload> handler;
            public CommandHandler(Action<FakeGameState, TPayload> handler) => this.handler = handler;
            public Type PayloadType => typeof(TPayload);
            public void Execute(FakeGameState state, object payload) => handler(state, (TPayload)payload);
        }
    }

    public sealed class FakeGameplayCommandInvocation
    {
        internal FakeGameplayCommandInvocation(string commandId, object payload, Type payloadType)
        {
            CommandId = commandId;
            Payload = payload;
            PayloadType = payloadType;
        }

        public string CommandId { get; }
        public object Payload { get; }
        public Type PayloadType { get; }
        public TPayload PayloadAs<TPayload>() => (TPayload)Payload;
    }
}
