using System;
using System.Collections.Generic;
using System.Linq;
using Oyung.SharedKernel;

namespace Oyung.Cutscenes
{
    // CLAUDE: class comment (your words, CRC). Draft: knows which ids
    // CLAUDE: have played; tells each id's listeners (its fact) when it
    // CLAUDE: becomes played. Knows nothing about playing.
    // CLAUDE: Used by CutsceneAsset (fact value) and the player (marks).
    internal sealed class CutsceneStore : ISaveable
    {
        private const string PlayedKey = "played";

        private readonly HashSet<string> _played = new HashSet<string>();

        // CLAUDE: same shape as GlobalStore: listeners by id.
        private readonly Dictionary<string, Action> _listeners =
            new Dictionary<string, Action>();

        internal bool IsPlayed(string id) => _played.Contains(id);

        /// <summary>Remembers the id as played and tells its listeners,
        /// once; marking it again changes nothing.</summary>
        internal void MarkPlayed(string id)
        {
            if (!_played.Add(id)) return;
            if (_listeners.TryGetValue(id, out var listeners)) listeners();
        }

        /// <summary>Calls the listener when this id becomes played.
        /// </summary>
        internal void Subscribe(string id, Action listener)
        {
            _listeners.TryGetValue(id, out var current);
            _listeners[id] = current + listener;
        }

        internal void Unsubscribe(string id, Action listener)
        {
            if (!_listeners.TryGetValue(id, out var current)) return;
            current -= listener;
            if (current == null) _listeners.Remove(id);
            else _listeners[id] = current;
        }

        public string Section => "cutscenes";

        public IReadOnlyDictionary<string, object> Give() =>
            new Dictionary<string, object>
            {
                [PlayedKey] = _played.ToList<object>()
            };

        public void Take(IReadOnlyDictionary<string, object> section)
        {
            _played.Clear();
            foreach (var id in (IEnumerable<object>)section[PlayedKey])
                _played.Add((string)id);
        }
    }
}
