using UnityEngine.Localization;

namespace Oyung.Quests.Unity
{
    /// <summary>
    /// The player-facing texts of quests, by the ids IQuests announces.
    /// For presentation (HUD); the core knows no texts.
    /// </summary>
    public sealed class QuestTexts
    {
        private readonly QuestCatalog _catalog;

        internal QuestTexts(QuestCatalog catalog) => _catalog = catalog;

        public LocalizedString Title(string questId) =>
            _catalog.Find(questId).Title;

        public LocalizedString Objective(string questId,
            string objectiveId) =>
            _catalog.Find(questId).ObjectiveText(objectiveId);
    }
}
