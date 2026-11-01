namespace Oyung.Speech
{
    /// <summary>One spoken syllable of a planned line: when it sounds, which
    /// syllable it is and how its pitch moves. Playback, talk motion and
    /// letters all read the same events, so they stay in sync.</summary>
    public readonly struct SyllableEvent
    {
        public SyllableEvent(float start, float duration, string syllable,
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
        public float Start { get; }

        /// <summary>Seconds until the next syllable may start.</summary>
        public float Duration { get; }

        /// <summary>Syllable id, e.g. "mo"; names the sound to play.</summary>
        public string Syllable { get; }

        /// <summary>Pitch at the start, as a ratio of the voice's base pitch.
        /// </summary>
        public float PitchFrom { get; }

        /// <summary>Pitch at the end; differs from PitchFrom on glides
        /// (a question rising, an exclamation falling).</summary>
        public float PitchTo { get; }

        /// <summary>First syllable of a content word: louder, higher.</summary>
        public bool Stressed { get; }

        /// <summary>Index of the English word in the line, from 0.</summary>
        public int Word { get; }
    }
}
