using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Oyung.SharedKernel;

namespace Oyung.Save
{
    // CLAUDE: class comment (your words). Draft: knows the saveables and
    // CLAUDE: the blocks being held. Does: on Save collects every section
    // CLAUDE: and hands them to storage, unless a block is held; on Load
    // CLAUDE: gives each saveable its section back. Knows no module and
    // CLAUDE: never reads a section. Used by whoever commands a save
    // CLAUDE: (ISaveGame) and by modules with a busy moment (ISaveBlocker).
    internal sealed class SaveSystem : ISaveGame, ISaveBlocker
    {
        private readonly ISaveStorage _storage;

        private readonly Dictionary<string, ISaveable> _saveables =
            new Dictionary<string, ISaveable>();

        // CLAUDE: a list, not a set: the same reason can be held twice.
        private readonly List<string> _blocks = new List<string>();

        internal SaveSystem(ISaveStorage storage) => _storage = storage;

        public event Action Saved;
        public event Action<IReadOnlyList<string>> Blocked;
        public event Action<string> WriteFailed;

        internal void Add(ISaveable saveable)
        {
            if (_saveables.ContainsKey(saveable.Section))
                throw new InvalidOperationException(
                    $"Save section '{saveable.Section}' is used twice.");
            _saveables[saveable.Section] = saveable;
        }

        /// <summary>Load phase: gives each saveable its section. Returns
        /// the sections nobody claimed, e.g. from a removed module.
        /// </summary>
        internal IReadOnlyList<string> Load(IReadOnlyDictionary<string,
            IReadOnlyDictionary<string, object>> sections)
        {
            foreach (var saveable in _saveables.Values)
                if (sections.TryGetValue(saveable.Section, out var section))
                    saveable.Take(section);

            return sections.Keys
                .Where(name => !_saveables.ContainsKey(name))
                .ToList();
        }

        public void Save()
        {
            if (_blocks.Count > 0)
            {
                Blocked?.Invoke(_blocks.ToList());
                return;
            }

            var sections = _saveables.ToDictionary(
                p => p.Key, p => p.Value.Give());

            // CLAUDE: only what the outside world can cause (disk full, no
            // CLAUDE: permission). A bad value in a section is our bug and
            // CLAUDE: stays loud.
            try
            {
                _storage.Write(sections);
            }
            catch (IOException problem)
            {
                WriteFailed?.Invoke(problem.Message);
                return;
            }
            catch (UnauthorizedAccessException problem)
            {
                WriteFailed?.Invoke(problem.Message);
                return;
            }
            Saved?.Invoke();
        }

        public void Block(string reason)
        {
            if (string.IsNullOrEmpty(reason))
                throw new ArgumentException(
                    "A save block needs a reason, so a stuck one can be " +
                    "traced.", nameof(reason));
            _blocks.Add(reason);
        }

        public void Unblock(string reason)
        {
            if (!_blocks.Remove(reason))
                throw new InvalidOperationException(
                    $"Save block '{reason}' released but not held.");
        }
    }
}
