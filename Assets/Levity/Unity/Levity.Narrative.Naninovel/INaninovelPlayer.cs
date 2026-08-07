using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Levity.Narrative.Core;

namespace Levity.Narrative.Naninovel
{
    /// <summary>Signals that the selected Naninovel backend package or engine is unavailable.</summary>
    public sealed class NaninovelUnavailableException : InvalidOperationException
    {
        public NaninovelUnavailableException(string message, Exception innerException = null)
            : base(message, innerException) { }
    }

    internal sealed class NaninovelConcurrentSessionException : InvalidOperationException
    {
        public NaninovelConcurrentSessionException()
            : base("A Naninovel narrative session is already active.") { }
    }

    internal sealed class NaninovelSessionCancelledException : OperationCanceledException { }

    /// <summary>Explicit extension surface implemented by the installed Naninovel integration.</summary>
    public interface INaninovelPlayer
    {
        SaveAvailability SaveAvailability { get; }

        Task<object> PlayAsync(
            NaninovelPlaybackRequest request,
            CancellationToken cancellationToken);
    }

    public sealed class NaninovelPlaybackRequest
    {
        public NaninovelPlaybackRequest(
            string scriptPath,
            string entryPoint,
            IReadOnlyDictionary<string, object> parameters,
            ConcurrentRequestPolicy concurrentPolicy)
        {
            ScriptPath = scriptPath ?? throw new ArgumentNullException(nameof(scriptPath));
            EntryPoint = entryPoint;
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            ConcurrentPolicy = concurrentPolicy;
        }

        public string ScriptPath { get; }
        public string EntryPoint { get; }
        public IReadOnlyDictionary<string, object> Parameters { get; }
        public ConcurrentRequestPolicy ConcurrentPolicy { get; }
    }
}
