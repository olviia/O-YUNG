using System;
using UnityEngine;

namespace Oyung.SharedKernel.Unity
{
    // CLAUDE: class comment: knows / does / used by
    // CLAUDE: draft: a named piece of some module's state, as an asset
    // CLAUDE: designers drag into conditions. Holds no value: each module
    // CLAUDE: subclasses it and reads Value from its own store.
    public abstract class ModuleFact<T> : ScriptableObject
    {
        [Tooltip("Stable name used by saves and dialogue. Filled from " +
                 "the file name once; renaming the file doesn't change it.")]
        [SerializeField] private string factName;

        /// <summary>Stable name; the module's store keys values by it.
        /// </summary>
        public string Name => factName;

        /// <summary>Current value, read from the owning module's store.
        /// </summary>
        public abstract T Value { get; }

        // CLAUDE: draft comment, rewrite in your words.
        /// <summary>Raised when this fact's value really changes; read
        /// <see cref="Value"/> again.</summary>
        public event Action Changed;

        // CLAUDE: protected: only the owning module's subclass decides
        // CLAUDE: when the value changed.
        protected void RaiseChanged() => Changed?.Invoke();

#if UNITY_EDITOR
        // CLAUDE: fill once, only when empty and only after the asset
        // CLAUDE: exists on disk (so not the "New..." placeholder that
        // CLAUDE: lives while you type the file name).
        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(factName)) return;
            if (!UnityEditor.AssetDatabase.Contains(this)) return;
            factName = name;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
