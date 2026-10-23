using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oyung.SharedKernel.Unity
{
    // CLAUDE: class comment (your words). Draft: knows every asset of
    // CLAUDE: kind T in the project; fills itself in the editor and
    // CLAUDE: checks names are unique. Each module subclasses it in one
    // CLAUDE: line (Unity can't make an asset of an open generic type).
    // CLAUDE: Used by the module's installer (bind) and lookups by name.
    public abstract class Catalog<T> : ScriptableObject
        where T : class, ICataloguedItem
    {
        // CLAUDE: ScriptableObject because Unity can't serialize a list
        // CLAUDE: of an interface; every item is a T.
        [SerializeField] private List<ScriptableObject> items =
            new List<ScriptableObject>();

        public IEnumerable<T> Items => items.OfType<T>();

        /// <summary>The item with this name; throws if none.</summary>
        public T Find(string id) =>
            Items.FirstOrDefault(item => item.Name == id) ??
            throw new KeyNotFoundException(
                $"No {typeof(T).Name} '{id}' in {name}.");

#if UNITY_EDITOR
        // CLAUDE: runs when the catalog is loaded or inspected in the
        // CLAUDE: editor. An asset created later shows up the next time.
        // CLAUDE: Delayed: Unity forbids loading assets inside OnValidate.
        private void OnValidate() =>
            UnityEditor.EditorApplication.delayCall += Refresh;

        [ContextMenu("Refresh")]
        private void Refresh()
        {
            if (this == null) return; // destroyed before the delayed call
            // TypeCache leaves T itself out; T may also be an interface.
            var found = UnityEditor.TypeCache.GetTypesDerivedFrom<T>()
                .Append(typeof(T))
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .SelectMany(t => UnityEditor.AssetDatabase
                    .FindAssets("t:" + t.Name))
                .Distinct()
                .Select(UnityEditor.AssetDatabase.GUIDToAssetPath)
                .Select(UnityEditor.AssetDatabase
                    .LoadAssetAtPath<ScriptableObject>)
                .Where(asset => asset is T)
                .OrderBy(asset => asset.name)
                .ToList();

            if (!found.SequenceEqual(items))
            {
                items = found;
                UnityEditor.EditorUtility.SetDirty(this);
            }

            foreach (var duplicate in Items.GroupBy(item => item.Name)
                         .Where(g => g.Count() > 1))
                Debug.LogError(
                    $"{typeof(T).Name} name '{duplicate.Key}' is used " +
                    $"{duplicate.Count()} times; names must be unique.",
                    this);
        }
#endif
    }
}
