using System;
using System.Linq;
using UnityEngine;

namespace Oyung.Quests.Unity
{
    /// <summary>
    /// Quests: needs nothing from other modules (they reach it through
    /// conditions and instructions). Creates the journal, binds every
    /// catalog quest to it and builds the core from their definitions.
    /// Start lets quests move.
    /// </summary>
    public class QuestsInstaller : MonoBehaviour, IDisposable
    {
        [SerializeField] private QuestCatalog catalog;

        private Quests _quests;

        public (IQuests quests, QuestTexts texts) Build()
        {
            var journal = new QuestJournal();
            foreach (var quest in catalog.Items) quest.Bind(journal);
            _quests = new Quests(
                catalog.Items.Select(q => q.ToDefinition()), journal);
            return (_quests, new QuestTexts(catalog));
        }

        /// <summary>Start phase, after load: quests check their
        /// conditions and listen from now on.</summary>
        public void StartListening() => _quests.Start();

        public void Dispose()
        {
            _quests?.Dispose();
            _quests = null;
            foreach (var quest in catalog.Items) quest.Unbind();
        }
    }
}
