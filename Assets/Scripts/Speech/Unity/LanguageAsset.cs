using UnityEngine;

namespace Oyung.Speech.Unity
{
    // CLAUDE: class comment (your words). Draft: a made-up language as an
    // CLAUDE: asset: its syllables and the English words that stay
    // CLAUDE: unstressed. Melody numbers stay Language's defaults, which
    // CLAUDE: tools/voice/preview_line.py mirrors, until one needs tuning.
    [CreateAssetMenu(menuName = "Oyung/Speech/Language",
        fileName = "NewLanguage")]
    internal sealed class LanguageAsset : ScriptableObject
    {
        [Tooltip("Must match the syllables the bups were baked for.")]
        [SerializeField] private string[] syllables;
        [Tooltip("English words never stressed (\"the\", \"is\").")]
        [SerializeField] private string[] functionWords;

        private LinePlanner _planner;

        internal LinePlanner Planner => _planner ??=
            new LinePlanner(new Language(syllables, functionWords));
    }
}
