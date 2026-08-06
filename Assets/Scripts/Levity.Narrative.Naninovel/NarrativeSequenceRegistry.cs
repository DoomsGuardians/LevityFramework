using System;
using System.Collections.Generic;
using Levity.Narrative.Core;

namespace Levity.Narrative.Naninovel
{
    /// <summary>Maps stable Levity sequence IDs to current Naninovel content locations.</summary>
    public sealed class NarrativeSequenceRegistry
    {
        private readonly Dictionary<NarrativeSequenceId, NaninovelSequence> sequences =
            new Dictionary<NarrativeSequenceId, NaninovelSequence>();

        public void Register(NarrativeSequenceId sequenceId, NaninovelSequence sequence)
        {
            if (sequence == null) throw new ArgumentNullException(nameof(sequence));
            sequences[sequenceId] = sequence;
        }

        /// <summary>
        /// Maps existing Naninovel script fields to a stable ID during incremental migration.
        /// New content should register a NaninovelSequence directly.
        /// </summary>
        public void RegisterLegacy(
            NarrativeSequenceId sequenceId,
            string scriptName,
            string startLabel = null) =>
            Register(sequenceId, new NaninovelSequence(scriptName, startLabel));

        public bool TryResolve(NarrativeSequenceId sequenceId, out NaninovelSequence sequence) =>
            sequences.TryGetValue(sequenceId, out sequence);
    }

    /// <summary>A backend mapping to a Naninovel script and optional entry point.</summary>
    public sealed class NaninovelSequence
    {
        public NaninovelSequence(string scriptPath, string entryPoint = null)
        {
            if (string.IsNullOrWhiteSpace(scriptPath))
                throw new ArgumentException("A Naninovel script path cannot be empty.", nameof(scriptPath));

            ScriptPath = scriptPath;
            EntryPoint = entryPoint;
        }

        public string ScriptPath { get; }
        public string EntryPoint { get; }
    }
}
