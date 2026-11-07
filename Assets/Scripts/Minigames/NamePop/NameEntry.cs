using System;
using System.Text;

namespace Oyung.Minigames.NamePop
{
    // CLAUDE: the rules of the name being typed, with nothing of Unity:
    // CLAUDE: letters add up to a maximum, the last one can be taken back,
    // CLAUDE: and only a name with a letter can be finished.
    internal sealed class NameEntry
    {
        private readonly StringBuilder _letters = new StringBuilder();
        private readonly int _maxLength;

        internal NameEntry(int maxLength)
        {
            if (maxLength < 1)
                throw new ArgumentOutOfRangeException(nameof(maxLength));
            _maxLength = maxLength;
        }

        internal string Text => _letters.ToString();

        internal int Length => _letters.Length;

        internal bool CanFinish => _letters.Length > 0;

        /// <summary>False when the name is full: the letter doesn't
        /// attach.</summary>
        internal bool Add(char letter)
        {
            if (_letters.Length >= _maxLength) return false;
            _letters.Append(letter);
            return true;
        }

        /// <summary>False when there is nothing to take back.</summary>
        internal bool RemoveLast()
        {
            if (_letters.Length == 0) return false;
            _letters.Length--;
            return true;
        }
    }
}
