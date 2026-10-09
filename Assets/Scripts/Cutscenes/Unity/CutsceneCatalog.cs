using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oyung.Cutscenes.Unity
{
    // CLAUDE: class comment (your words). Draft: knows every cutscene
    // CLAUDE: asset; fills itself in the editor, checks ids are unique.
    // CLAUDE: Used by the installer (bind) and the player (id -> prefab).
    [CreateAssetMenu(menuName = "Oyung/Cutscenes/Catalog",
        fileName = "CutsceneCatalog")]
    internal sealed class CutsceneCatalog : ScriptableObject
    {
        [SerializeField] private List<CutsceneAsset> cutscenes =
            new List<CutsceneAsset>();

        public IReadOnlyList<CutsceneAsset> Cutscenes => cutscenes;

        /// <summary>The cutscene with this id; throws if none.</summary>
        public CutsceneAsset Find(string id) =>
            cutscenes.FirstOrDefault(c => c.Name == id) ??
            throw new KeyNotFoundException(
                $"No cutscene '{id}' in the catalog.");

#if UNITY_EDITOR
        // CLAUDE: same as GlobalCatalog: refresh when loaded/inspected.
        private void OnValidate() =>
            UnityEditor.EditorApplication.delayCall += Refresh;

        [ContextMenu("Refresh")]
        private void Refresh()
        {
            if (this == null) return; // destroyed before the delayed call
            var found = UnityEditor.AssetDatabase
                .FindAssets("t:" + nameof(CutsceneAsset))
                .Select(UnityEditor.AssetDatabase.GUIDToAssetPath)
                .Select(UnityEditor.AssetDatabase
                    .LoadAssetAtPath<CutsceneAsset>)
                .Where(asset => asset != null)
                .OrderBy(asset => asset.name)
                .ToList();

            if (!found.SequenceEqual(cutscenes))
            {
                cutscenes = found;
                UnityEditor.EditorUtility.SetDirty(this);
            }

            foreach (var duplicate in cutscenes.GroupBy(c => c.Name)
                         .Where(g => g.Count() > 1))
                Debug.LogError(
                    $"Cutscene id '{duplicate.Key}' is used by " +
                    $"{duplicate.Count()} cutscenes; ids must be unique.",
                    this);
        }
#endif
    }
}
