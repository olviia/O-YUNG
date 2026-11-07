using System.Collections.Generic;
using UnityEngine;

namespace Oyung.Minigames.NamePop
{
    // CLAUDE: the name as it grows: the first letter stands at this anchor
    // CLAUDE: and the rest follow to the right.
    internal sealed class NameDisplay : MonoBehaviour
    {
        [SerializeField] private NameLetter letterPrefab;

        private readonly List<NameLetter> _letters = new List<NameLetter>();
        private readonly List<float> _advances = new List<float>();
        private float _cursor;

        internal void Add(Mesh mesh, Color colour, NamePopConfig config)
        {
            var width = mesh.bounds.size.x * config.NameLetterHeight;
            var letter = Instantiate(letterPrefab, transform);
            letter.Land(mesh, colour, config.NameLetterHeight,
                new Vector3(_cursor + width / 2f, 0f, 0f),
                config.LandingDrop, config.LandingSeconds);
            var advance = width + config.NameLetterGap;
            _letters.Add(letter);
            _advances.Add(advance);
            _cursor += advance;
        }

        internal void RemoveLast()
        {
            var last = _letters.Count - 1;
            _letters[last].Leave();
            _cursor -= _advances[last];
            _letters.RemoveAt(last);
            _advances.RemoveAt(last);
        }

        internal void Clear()
        {
            while (_letters.Count > 0) RemoveLast();
            _cursor = 0f;
        }
    }
}
