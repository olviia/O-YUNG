using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Oyung.Speech
{
    /// <summary>Turns an English line into the syllable events of the
    /// made-up language. The same word always becomes the same syllables,
    /// so the gibberish feels like a language; punctuation and stress give
    /// it its melody.</summary>
    // CLAUDE: English text is only the hidden source: words are hashed,
    // CLAUDE: never shown. Letters other than a-z and ' are ignored.
    internal sealed class LinePlanner
    {
        private static readonly Regex Phrases =
            new Regex(@"([^.!?]+)([.!?]?)", RegexOptions.Compiled);
        private static readonly Regex Words =
            new Regex(@"[a-z']+", RegexOptions.Compiled);
        private static readonly Regex VowelGroups =
            new Regex(@"[aeiouy]+", RegexOptions.Compiled);

        private readonly Language _language;

        internal LinePlanner(Language language) => _language = language;

        internal IReadOnlyList<SyllableEvent> Plan(string text)
        {
            var events = new List<SyllableEvent>();
            if (string.IsNullOrEmpty(text)) return events;
            var time = 0f;
            var wordsBefore = 0;
            foreach (Match phrase in Phrases.Matches(text.ToLowerInvariant()))
            {
                var words = new List<string>();
                foreach (Match w in Words.Matches(phrase.Groups[1].Value))
                    words.Add(w.Value);
                if (words.Count == 0) continue;
                time = PlanPhrase(words, phrase.Groups[2].Value, wordsBefore,
                    time, events);
                wordsBefore += words.Count;
                time += _language.PhraseGap;
            }
            return events;
        }

        /// <summary>Syllables a word becomes; stable across runs and
        /// platforms.</summary>
        internal IReadOnlyList<string> WordSyllables(string word)
        {
            var all = _language.Syllables;
            var result = new string[CountSyllables(word)];
            for (var i = 0; i < result.Length; i++)
                result[i] = all[(int)(Fnv1a(word + "#" + i) % (uint)all.Count)];
            return result;
        }

        /// <summary>Rough English syllable count: vowel groups, minus a
        /// silent final e ("name" = 1). Good enough for rhythm.</summary>
        internal static int CountSyllables(string word)
        {
            var n = VowelGroups.Matches(word).Count;
            if (word.EndsWith("e") && n > 1) n--;
            return n < 1 ? 1 : n;
        }

        /// <summary>32-bit FNV-1a over UTF-8. string.GetHashCode is not
        /// stable across runtimes, and the bake script must agree.</summary>
        internal static uint Fnv1a(string text)
        {
            var hash = 2166136261u;
            foreach (var b in Encoding.UTF8.GetBytes(text))
                hash = unchecked((hash ^ b) * 16777619u);
            return hash;
        }

        private float PlanPhrase(List<string> words, string mark,
            int wordsBefore, float time, List<SyllableEvent> events)
        {
            var syllables = new List<(int word, string syllable, bool stress)>();
            for (var w = 0; w < words.Count; w++)
            {
                var content = !_language.FunctionWords.Contains(words[w]);
                var parts = WordSyllables(words[w]);
                for (var j = 0; j < parts.Count; j++)
                    syllables.Add((w, parts[j], j == 0 && content));
            }

            var lang = _language;
            var lift = mark == "!" ? lang.ExclaimLift : 1f;
            for (var k = 0; k < syllables.Count; k++)
            {
                var (word, syllable, stressed) = syllables[k];
                if (k > 0 && syllables[k - 1].word != word)
                    time += lang.WordGap;
                var pitch = lift * (1 - lang.Drift * k) +
                            (stressed ? lang.Stress : 0);
                var from = pitch;
                var to = pitch;
                var duration = stressed
                    ? lang.StressedDuration
                    : lang.PlainDuration;
                if (k == syllables.Count - 1)
                {
                    // CLAUDE: the phrase's last syllable carries its tune.
                    duration = lang.FinalDuration;
                    if (mark == "?") to = pitch * lang.QuestionRise;
                    else if (mark == "!")
                    {
                        from = pitch * lang.ExclaimFrom;
                        to = pitch * lang.ExclaimTo;
                    }
                    else to = pitch * lang.StatementFall;
                }
                events.Add(new SyllableEvent(time, duration, syllable, from,
                    to, stressed, wordsBefore + word));
                time += duration;
            }
            return time;
        }
    }
}
