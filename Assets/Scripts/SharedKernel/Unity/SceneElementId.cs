using System;
using UnityEngine;

namespace Oyung.SharedKernel.Unity
{
    // CLAUDE: class comment (your words). Draft: the id of a scene element
    // CLAUDE: as an asset, the meeting point of a live scene object and
    // CLAUDE: whatever looks for it. Knows the object holding it right
    // CLAUDE: now, or none while its chunk isn't loaded. The object puts
    // CLAUDE: itself here when it appears and takes itself out when it
    // CLAUDE: goes; others read Current and listen to Changed. Neither
    // CLAUDE: side knows the other, so objects can stream freely. Each
    // CLAUDE: module subclasses it in one line (ActorId, LocationId).
    public abstract class SceneElementId<T> : ScriptableObject where T : class
    {
        // Runtime only: never written into the asset.
        [NonSerialized] private T _current;

        /// <summary>The object holding this id now; null if none is
        /// loaded.</summary>
        public T Current => _current;

        /// <summary>Raised when an object takes or leaves this id.
        /// </summary>
        public event Action Changed;

        /// <summary>The object holds this id from now on. Throws if
        /// another one already holds it.</summary>
        public void Bind(T element)
        {
            if (_current == element) return;
            if (_current != null)
                throw new InvalidOperationException(
                    $"Id '{name}' is held by '{NameOf(_current)}' and " +
                    $"'{NameOf(element)}'; ids must be unique.");
            _current = element;
            Changed?.Invoke();
        }

        /// <summary>The object gives the id up; ignored if it doesn't
        /// hold it.</summary>
        public void Unbind(T element)
        {
            if (_current != element) return;
            _current = null;
            Changed?.Invoke();
        }

        private static string NameOf(T element) =>
            element is UnityEngine.Object o ? o.name : element.ToString();
    }
}
