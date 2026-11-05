namespace Oyung.Speech
{
    /// <summary>One spoken syllable of a planned line: when it sounds, which
    /// syllable it is and how its pitch moves. Playback, talk motion and
    /// letters all read the same events, so they stay in sync.</summary>
    internal readonly struct SyllableEvent
    {
        internal SyllableEvent(float start, float duration, string syllable,
            float pitchFrom, float pitchTo, bool stressed, int word)
        {
            Start = start;
            Duration = duration;
            Syllable = syllable;
            PitchFrom = pitchFrom;
            PitchTo = pitchTo;
            Stressed = stressed;
            Word = word;
        }

        /// <summary>Seconds from the start of the line.</summary>
        internal float Start { get; }

        /// <summary>Seconds until the next syllable may start.</summary>
        internal float Duration { get; }

        /// <summary>Syllable id, e.g. "mo"; names the sound to play.</summary>
        internal string Syllable { get; }

        /// <summary>Pitch at the start, as a ratio of the voice's base pitch.
        /// </summary>
        internal float PitchFrom { get; }

        /// <summary>Pitch at the end; differs from PitchFrom on glides
        /// (a question rising, an exclamation falling).</summary>
        internal float PitchTo { get; }

        /// <summary>First syllable of a content word: louder, higher.</summary>
        internal bool Stressed { get; }

        /// <summary>Index of the English word in the line, from 0.</summary>
        internal int Word { get; }
    }
}
