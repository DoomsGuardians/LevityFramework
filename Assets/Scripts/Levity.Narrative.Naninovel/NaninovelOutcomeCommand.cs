using Naninovel;
using System.Threading.Tasks;

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
            NaninovelOutcomeBridge.Publish(Value);
            return UniTask.CompletedTask;
        }
    }

    internal static class NaninovelOutcomeBridge
    {
        private static TaskCompletionSource<string> pending;

        public static Task<string> Begin()
        {
            pending = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            return pending.Task;
        }

        public static void Publish(string value) => pending?.TrySetResult(value);
        public static void Cancel() => pending?.TrySetCanceled();
    }
}
