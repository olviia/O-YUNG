using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oyung.Speech.Unity
{
    // CLAUDE: class comment (your words). Draft: the baked clips of a
    // CLAUDE: language's syllables, found by name ("ma_0"). Several voices
    // CLAUDE: share one bank and differ by pitch.
    [CreateAssetMenu(menuName = "Oyung/Speech/Bup Bank",
        fileName = "NewBupBank")]
    internal sealed class BupBank : ScriptableObject
    {
        [Tooltip("Named syllable_take, as tools/voice/bake_bups.py bakes "
                 + "them.")]
        [SerializeField] private AudioClip[] clips;
        [Tooltip("How many takes each syllable has.")]
        [Min(1)]
        [SerializeField] private int takes = 3;

        private Dictionary<string, AudioClip> _byName;

        /// <summary>A take of the syllable; any number works, it wraps
        /// around the takes there are. Throws if the bank lacks it.
        /// </summary>
        internal AudioClip Clip(string syllable, int number)
        {
            if (_byName == null)
            {
                _byName = new Dictionary<string, AudioClip>();
                foreach (var clip in clips) _byName[clip.name] = clip;
            }
            var clipName = $"{syllable}_{number % takes}";
            return _byName.TryGetValue(clipName, out var found)
                ? found
                : throw new InvalidOperationException(
                    $"Bup bank '{name}' has no clip '{clipName}'.");
        }
    }
}
