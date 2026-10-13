using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oyung.Quests.Unity
{
    // CLAUDE: knows every quest asset; fills itself in the editor, checks
    // CLAUDE: ids are unique. Same shape as CutsceneCatalog.
    // CLAUDE: Used by the installer (bind, definitions) and QuestTexts.
    [CreateAssetMenu(menuName = "Oyung/Quests/Catalog",
        fileName = "QuestCatalog")]
    internal sealed class QuestCatalog : ScriptableObject
    {
        [SerializeField] private List<QuestAsset> quests =
            new List<QuestAsset>();

        public IReadOnlyList<QuestAsset> Quests => quests;

        /// <summary>The quest with this id; throws if none.</summary>
        public QuestAsset Find(string id) =>
            quests.FirstOrDefault(q => q.Name == id) ??
            throw new KeyNotFoundException(
                $"No quest '{id}' in the catalog.");

#if UNITY_EDITOR
        // CLAUDE: same as GlobalCatalog: refresh when loaded/inspected.
        private void OnValidate() =>
            UnityEditor.EditorApplication.delayCall += Refresh;

        [ContextMenu("Refresh")]
        private void Refresh()
        {
            if (this == null) return; // destroyed before the delayed call
            var found = UnityEditor.AssetDatabase
                .FindAssets("t:" + nameof(QuestAsset))
                .Select(UnityEditor.AssetDatabase.GUIDToAssetPath)
                .Select(UnityEditor.AssetDatabase
                    .LoadAssetAtPath<QuestAsset>)
                .Where(asset => asset != null)
                .OrderBy(asset => asset.name)
                .ToList();

            if (!found.SequenceEqual(quests))
            {
                quests = found;
                UnityEditor.EditorUtility.SetDirty(this);
            }

            foreach (var duplicate in quests.GroupBy(q => q.Name)
                         .Where(g => g.Count() > 1))
                Debug.LogError(
                    $"Quest id '{duplicate.Key}' is used by " +
                    $"{duplicate.Count()} quests; ids must be unique.",
                    this);
        }
#endif
    }
}
