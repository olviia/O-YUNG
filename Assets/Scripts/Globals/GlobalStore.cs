using System;
using System.Collections.Generic;
using Oyung.SharedKernel;

namespace Oyung.Globals
{
    /// <summary>
    /// Leftover memory store that does not belong to any other modules
    /// </summary>
    internal sealed class GlobalStore : ISaveable
    {
        // CLAUDE: one dictionary for every kind, keyed by name. Values are
        // CLAUDE: boxed (bool/int/string as object); the catalog checks
        // CLAUDE: that names are unique, so each name has one kind.
        private readonly Dictionary<string, object> _values =
            new Dictionary<string, object>();

        // CLAUDE: listeners by name, so a change calls only the ones
        // CLAUDE: that care (no "is it me?" check in every fact).
        private readonly Dictionary<string, Action> _listeners =
            new Dictionary<string, Action>();

        /// <summary>Calls the listener whenever this name's value really
        /// changes.</summary>
        internal void Subscribe(string name, Action listener)
        {
            _listeners.TryGetValue(name, out var current);
            _listeners[name] = current + listener;
        }

        internal void Unsubscribe(string name, Action listener)
        {
            if (!_listeners.TryGetValue(name, out var current)) return;
            current -= listener;
            if (current == null) _listeners.Remove(name);
            else _listeners[name] = current;
        }

        /// <summary>Current value; a name never set reads default
        /// (false / 0 / null).</summary>
        internal T Get<T>(string name) =>
            _values.TryGetValue(name, out var value) && value is T typed
                ? typed
                : default;

        internal void Set<T>(string name, T value)
        {
            // CLAUDE: no event when nothing changes (true over true, or
            // CLAUDE: false over a never-set flag).
            if (EqualityComparer<T>.Default.Equals(Get<T>(name), value))
                return;
            _values[name] = value;
            if (_listeners.TryGetValue(name, out var listeners)) listeners();
        }

        internal void Add(string name, int amount) =>
            Set(name, Get<int>(name) + amount);

        public string Section => "globals";

        public IReadOnlyDictionary<string, object> Give() =>
            new Dictionary<string, object>(_values);

        public void Take(IReadOnlyDictionary<string, object> section)
        {
            _values.Clear();
            foreach (var entry in section)
                _values[entry.Key] = Narrowed(entry.Value);
        }

        // CLAUDE: a save returns whole numbers as long; Get<int> would not
        // CLAUDE: recognise one and quietly read 0. A float fact would need
        // CLAUDE: the same for double.
        private static object Narrowed(object value) =>
            value is long whole ? checked((int)whole) : value;
    }
}
