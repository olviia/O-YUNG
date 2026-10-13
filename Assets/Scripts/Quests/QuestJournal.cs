using System;
using System.Collections.Generic;

namespace Oyung.Quests
{
    // CLAUDE: knows the state of every quest and objective and the outcome
    // CLAUDE: each closed quest ended with. Does: records changes and
    // CLAUDE: announces them (events for IQuests; per-quest listeners for
    // CLAUDE: quest assets, same shape as CutsceneStore). Knows no rules:
    // CLAUDE: QuestRun decides, the journal only remembers.
    // CLAUDE: Still missing: give/take the save section. Designed together
    // CLAUDE: with Save (participant port), as for GlobalStore.
    internal sealed class QuestJournal
    {
        private readonly Dictionary<string, QuestState> _quests =
            new Dictionary<string, QuestState>();

        private readonly Dictionary<string, string> _outcomes =
            new Dictionary<string, string>();

        private readonly Dictionary<(string, string), ObjectiveState>
            _objectives = new Dictionary<(string, string), ObjectiveState>();

        private readonly List<string> _active = new List<string>();

        private readonly Dictionary<string, Action> _listeners =
            new Dictionary<string, Action>();

        internal event Action<string> QuestStarted;
        internal event Action<string, string, ObjectiveState> ObjectiveChanged;
        internal event Action<string, string> QuestClosed;

        internal IReadOnlyList<string> ActiveQuests => _active;

        /// <summary>A quest never seen reads NotStarted.</summary>
        internal QuestState StateOf(string questId) =>
            _quests.TryGetValue(questId, out var state)
                ? state
                : QuestState.NotStarted;

        internal string OutcomeOf(string questId) =>
            _outcomes.TryGetValue(questId, out var outcome) ? outcome : null;

        /// <summary>An objective never seen reads Hidden.</summary>
        internal ObjectiveState StateOf(string questId, string objectiveId) =>
            _objectives.TryGetValue((questId, objectiveId), out var state)
                ? state
                : ObjectiveState.Hidden;

        internal void Start(string questId)
        {
            Require(StateOf(questId) == QuestState.NotStarted,
                $"Quest '{questId}' started twice.");
            _quests[questId] = QuestState.Active;
            _active.Add(questId);
            QuestStarted?.Invoke(questId);
            Notify(questId);
        }

        internal void SetObjective(string questId, string objectiveId,
            ObjectiveState state)
        {
            Require(StateOf(questId) == QuestState.Active,
                $"Objective '{objectiveId}' changed outside active " +
                $"quest '{questId}'.");
            // CLAUDE: sticky: states only move forward
            // CLAUDE: (Hidden < Shown < Done/Failed).
            var current = StateOf(questId, objectiveId);
            Require(state > current && current < ObjectiveState.Done,
                $"Objective '{questId}/{objectiveId}' can't go from " +
                $"{current} to {state}.");
            _objectives[(questId, objectiveId)] = state;
            ObjectiveChanged?.Invoke(questId, objectiveId, state);
            Notify(questId);
        }

        internal void Close(string questId, string outcomeId)
        {
            Require(StateOf(questId) == QuestState.Active,
                $"Quest '{questId}' closed while not active.");
            _quests[questId] = QuestState.Closed;
            _outcomes[questId] = outcomeId;
            _active.Remove(questId);
            QuestClosed?.Invoke(questId, outcomeId);
            Notify(questId);
        }

        /// <summary>Calls the listener on any change of this quest: its
        /// own state or one of its objectives.</summary>
        internal void Subscribe(string questId, Action listener)
        {
            _listeners.TryGetValue(questId, out var current);
            _listeners[questId] = current + listener;
        }

        internal void Unsubscribe(string questId, Action listener)
        {
            if (!_listeners.TryGetValue(questId, out var current)) return;
            current -= listener;
            if (current == null) _listeners.Remove(questId);
            else _listeners[questId] = current;
        }

        private void Notify(string questId)
        {
            if (_listeners.TryGetValue(questId, out var listeners))
                listeners();
        }

        // CLAUDE: QuestRun never breaks these; if it does, it's our bug.
        private static void Require(bool holds, string problem)
        {
            if (!holds) throw new InvalidOperationException(problem);
        }
    }
}
