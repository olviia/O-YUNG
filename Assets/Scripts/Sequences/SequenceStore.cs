using System.Collections.Generic;

namespace Oyung.Sequences
{
    // CLAUDE: class comment (your words, CRC). Draft: knows how far each
    // CLAUDE: sequence got: the index of the step it is on, by sequence
    // CLAUDE: name. Past the last step means finished. Knows nothing
    // CLAUDE: about steps or actors. Used by SequenceAsset (and later
    // CLAUDE: Save, so a reload resumes at the same step).
    internal sealed class SequenceStore
    {
        private readonly Dictionary<string, int> _steps =
            new Dictionary<string, int>();

        /// <summary>Index of the step the sequence is on; 0 if it never
        /// started.</summary>
        internal int StepOf(string id) =>
            _steps.TryGetValue(id, out var step) ? step : 0;

        internal void SetStep(string id, int step) => _steps[id] = step;
    }
}
