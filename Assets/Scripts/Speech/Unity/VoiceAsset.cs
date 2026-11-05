using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oyung.Speech.Unity
{
    // CLAUDE: class comment (your words). Draft: how one character sounds:
    // CLAUDE: the language they speak, the bups they speak it with and how
    // CLAUDE: high. Knows nobody who uses it; a character holds its voice.
    [CreateAssetMenu(menuName = "Oyung/Speech/Voice", fileName = "NewVoice")]
    internal sealed class VoiceAsset : ScriptableObject
    {
        [SerializeField] private LanguageAsset language;
        [SerializeField] private BupBank bank;
        [Tooltip("Pitch of the whole voice; 1 plays the bups as baked.")]
        [Min(0.1f)]
        [SerializeField] private float basePitch = 1f;

        internal float BasePitch => basePitch;

        internal IReadOnlyList<SyllableEvent> Plan(string text) =>
            Required(language, nameof(language)).Planner.Plan(text);

        internal AudioClip Clip(string syllable, int number) =>
            Required(bank, nameof(bank)).Clip(syllable, number);

        private T Required<T>(T part, string field) where T : UnityEngine.Object
            => part != null
                ? part
                : throw new InvalidOperationException(
                    $"Voice '{name}' has no {field}.");
    }
}
