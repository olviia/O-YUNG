using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oyung.Globals.Unity
{
    // CLAUDE: class comment: knows / does / used by
    // CLAUDE: draft: knows every global fact asset in the project. Fills
    // CLAUDE: itself in the editor and checks names are unique. Used by
    // CLAUDE: GlobalsInstaller (later: Save / Yarn lookup by name).
    [CreateAssetMenu(menuName = "Oyung/Globals/Catalog",
        fileName = "GlobalCatalog")]
    internal sealed class GlobalCatalog : ScriptableObject
    {
        // CLAUDE: ScriptableObject because Unity can't serialize an
        // CLAUDE: interface list; every item is an IGlobalFact.
        [SerializeField] private List<ScriptableObject> facts =
            new List<ScriptableObject>();

        public IEnumerable<IGlobalFact> Facts => facts.OfType<IGlobalFact>();

#if UNITY_EDITOR
        // CLAUDE: runs when the catalog is loaded or inspected in the
        // CLAUDE: editor. A fact created later shows up the next time.
        // CLAUDE: Delayed: Unity forbids loading assets inside OnValidate.
        private void OnValidate() =>
            UnityEditor.EditorApplication.delayCall += Refresh;

        [ContextMenu("Refresh")]
        private void Refresh()
        {
            if (this == null) return; // destroyed before the delayed call
            var found = UnityEditor.TypeCache
                .GetTypesDerivedFrom<IGlobalFact>()
                .Where(t => !t.IsAbstract)
                .SelectMany(t => UnityEditor.AssetDatabase
                    .FindAssets("t:" + t.Name))
                .Distinct()
                .Select(UnityEditor.AssetDatabase.GUIDToAssetPath)
                .Select(UnityEditor.AssetDatabase
                    .LoadAssetAtPath<ScriptableObject>)
                .Where(asset => asset is IGlobalFact)
                .OrderBy(asset => asset.name)
                .ToList();

            if (!found.SequenceEqual(facts))
            {
                facts = found;
                UnityEditor.EditorUtility.SetDirty(this);
            }

            foreach (var duplicate in Facts.GroupBy(f => f.Name)
                         .Where(g => g.Count() > 1))
                Debug.LogError(
                    $"Global fact name '{duplicate.Key}' is used by " +
                    $"{duplicate.Count()} facts; names must be unique.",
                    this);
        }
#endif
    }
}
