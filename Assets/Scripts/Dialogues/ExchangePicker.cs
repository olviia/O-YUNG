using System;

namespace Oyung.Dialogues
{
    /// <summary>Picks which exchange of a pool plays next: at random, but
    /// never the same one twice in a row.</summary>
    internal sealed class ExchangePicker
    {
        private readonly int _count;
        private readonly Func<int, int> _random;
        private int _last = -1;

        /// <param name="count">How many exchanges the pool has.</param>
        /// <param name="random">Given n, a whole number from 0 to n - 1.
        /// </param>
        internal ExchangePicker(int count, Func<int, int> random)
        {
            if (count < 1)
                throw new ArgumentException("A pool needs an exchange.");
            _count = count;
            _random = random;
        }

        internal int Next()
        {
            if (_count == 1) return 0;
            // Rolls among the others, then steps over the last one.
            var pick = _random(_last < 0 ? _count : _count - 1);
            if (_last >= 0 && pick >= _last) pick++;
            _last = pick;
            return pick;
        }
    }
}
