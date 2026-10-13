using System;
using System.Collections.Generic;

namespace Oyung.Quests
{
    /// <summary>
    /// Port for what quests announce and answer to other modules (HUD,
    /// Hints). Read-only: quests move only by their own conditions;
    /// nobody outside starts or completes them directly.
    /// Ids are the quest asset's name and the ids typed in it.
    /// </summary>
    public interface IQuests
    {
        /// <summary>A quest started (questId).</summary>
        event Action<string> QuestStarted;

        /// <summary>An objective changed state (questId, objectiveId,
        /// new state: Shown, Done or Failed).</summary>
        event Action<string, string, ObjectiveState> ObjectiveChanged;

        /// <summary>A quest ended (questId, outcomeId).</summary>
        event Action<string, string> QuestClosed;

        /// <summary>Started, not yet closed quests, in start order.
        /// </summary>
        IReadOnlyList<string> ActiveQuests { get; }

        QuestState StateOf(string questId);

        /// <summary>The outcome the quest closed with; null until closed.
        /// </summary>
        string OutcomeOf(string questId);

        /// <summary>All objectives of the quest in designer order,
        /// hidden ones included; the reader filters by state.</summary>
        IReadOnlyList<ObjectiveInfo> ObjectivesOf(string questId);
    }
}
