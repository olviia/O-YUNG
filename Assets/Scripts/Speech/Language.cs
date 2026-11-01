using System;
using System.Collections.Generic;

namespace Oyung.Speech
{
    /// <summary>The made-up language: which syllables exist, which English
    /// words stay unstressed, and the numbers that shape its melody.
    /// </summary>
    // CLAUDE: plain data so the Unity side can fill it from an asset. The
    // CLAUDE: defaults match tools/voice/preview_line.py, the reference the
    // CLAUDE: tests compare against; change both together.
    internal sealed class Language
    {
        internal Language(IReadOnlyList<string> syllables,
            IEnumerable<string> functionWords)
        {
            if (syllables == null || syllables.Count == 0)
                throw new ArgumentException("A language needs syllables.");
            Syllables = syllables;
            FunctionWords = new HashSet<string>(
                functionWords ?? Array.Empty<string>(), StringComparer.Ordinal);
        }

        internal IReadOnlyList<string> Syllables { get; }

        /// <summary>English words never stressed ("the", "is").</summary>
        internal ISet<string> FunctionWords { get; }

        /// <summary>Pitch added to a stressed syllable.</summary>
        internal float Stress { get; set; } = 0.12f;

        /// <summary>Pitch lost per syllable along a phrase.</summary>
        internal float Drift { get; set; } = 0.015f;

        /// <summary>Whole-phrase pitch of an exclamation.</summary>
        internal float ExclaimLift { get; set; } = 1.08f;

        internal float StressedDuration { get; set; } = 0.30f;
        internal float PlainDuration { get; set; } = 0.20f;
        internal float FinalDuration { get; set; } = 0.42f;
        internal float WordGap { get; set; } = 0.03f;
        internal float PhraseGap { get; set; } = 0.35f;

        /// <summary>How far the last syllable glides: up on "?", from high
        /// to low on "!", a little down on ".".</summary>
        internal float QuestionRise { get; set; } = 1.35f;
        internal float ExclaimFrom { get; set; } = 1.12f;
        internal float ExclaimTo { get; set; } = 0.88f;
        internal float StatementFall { get; set; } = 0.9f;
    }
}
