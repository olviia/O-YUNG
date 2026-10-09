using System;
using System.Collections.Generic;

namespace Oyung.Cutscenes
{
    // CLAUDE: class comment (your words, CRC). Draft: knows which ids
    // CLAUDE: have played and which one is playing now. Decides whether
    // CLAUDE: a "play me" is allowed (once, one at a time) and tells
    // CLAUDE: ICutscenePlayer to play; marks the id played when it ends.
    // CLAUDE: Used by CutsceneAsset (proxy) and CutscenesInstaller.
    internal sealed class CutsceneStore
    {
        private readonly ICutscenePlayer _player;

        private readonly HashSet<string> _played = new HashSet<string>();

        // CLAUDE: same shape as GlobalStore: listeners by id.
        private readonly Dictionary<string, Action> _listeners =
            new Dictionary<string, Action>();

        // CLAUDE: null while nothing plays.
        private string _playing;

        internal CutsceneStore(ICutscenePlayer player) => _player = player;

        internal bool IsPlayed(string id) => _played.Contains(id);

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

        /// <summary>"Play me" from a cutscene. Ignored if it already
        /// played or another cutscene is playing.</summary>
        internal void RequestPlay(string id)
        {
            // CLAUDE: a request during another cutscene is dropped, not
            // CLAUDE: queued. Its condition must fire again. Fine for one
            // CLAUDE: intro; a queue when a story needs it.
            if (_playing != null || IsPlayed(id)) return;
            _playing = id;
            _player.Play(id, () => OnEnded(id));
        }

        private void OnEnded(string id)
        {
            _playing = null;
            _played.Add(id);
            if (_listeners.TryGetValue(id, out var listeners)) listeners();
        }
    }
}
