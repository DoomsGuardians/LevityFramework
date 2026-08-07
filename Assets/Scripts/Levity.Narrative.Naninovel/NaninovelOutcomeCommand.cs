using System;
using Naninovel;

namespace Levity.Narrative.Naninovel
{
    /// <summary>Returns a game-owned outcome value from a Naninovel script.</summary>
    [CommandAlias("levityOutcome")]
    public sealed class NaninovelOutcomeCommand : Command
    {
        [ParameterAlias(NamelessParameterAlias), RequiredParameter]
        public StringParameter Value;

        public override UniTask Execute(AsyncToken token = default)
        {
            NaninovelOutcomeRouter.Publish(Value);
            return UniTask.CompletedTask;
        }
    }

    internal interface INaninovelOutcomeSink
    {
        void Publish(string value);
    }

    internal static class NaninovelOutcomeRouter
    {
        private static readonly object Sync = new object();
        private static INaninovelOutcomeSink active;

        public static IDisposable Attach(INaninovelOutcomeSink sink)
        {
            lock (Sync)
            {
                if (active != null)
                    throw new InvalidOperationException("A Naninovel outcome sink is already attached.");
                active = sink ?? throw new ArgumentNullException(nameof(sink));
                return new Attachment(sink);
            }
        }

        public static void Publish(string value)
        {
            INaninovelOutcomeSink sink;
            lock (Sync) sink = active;
            sink?.Publish(value);
        }

        private sealed class Attachment : IDisposable
        {
            private readonly INaninovelOutcomeSink sink;
            public Attachment(INaninovelOutcomeSink sink) => this.sink = sink;
            public void Dispose()
            {
                lock (Sync)
                {
                    if (ReferenceEquals(active, sink)) active = null;
                }
            }
        }
    }
}
