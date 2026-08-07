using System;

namespace Levity.Composition
{
    /// <summary>Reports invalid module registration or missing Composition dependencies.</summary>
    public sealed class CompositionException : InvalidOperationException
    {
        public CompositionException(string message) : base(message) { }
    }
}
