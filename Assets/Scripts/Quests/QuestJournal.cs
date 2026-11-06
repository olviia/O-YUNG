using System;
using System.Collections.Generic;
using System.Linq;
using Oyung.SharedKernel;

namespace Oyung.Quests
{
    // CLAUDE: knows the state of every quest and objective and the outcome
    // CLAUDE: each closed quest ended with. Does: records changes and
    // CLAUDE: announces them (events for IQuests; per-quest listeners for
    // CLAUDE: quest assets, same shape as CutsceneStore). Knows no rules:
    // CLAUDE: QuestRun decides, the journal only remembers.
    internal sealed class QuestJournal : ISaveable
    {
        private const string QuestsKey = "quests";
        private const string OutcomesKey = "outcomes";
        private const string ObjectivesKey = "objectives";
        private const string ActiveKey = "active";

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

        public string Section => "quests";

        // CLAUDE: states by name, not number: a save stays readable and
        // CLAUDE: survives reordering the enums.
        public IReadOnlyDictionary<string, object> Give() =>
            new Dictionary<string, object>
            {
                [QuestsKey] = _quests.ToDictionary(
                    q => q.Key, q => (object)q.Value.ToString()),
                [OutcomesKey] = _outcomes.ToDictionary(
                    o => o.Key, o => (object)o.Value),
                [ObjectivesKey] = _objectives
                    .GroupBy(o => o.Key.Item1)
                    .ToDictionary(quest => quest.Key,
                        quest => (object)quest.ToDictionary(
                            o => o.Key.Item2,
                            o => (object)o.Value.ToString())),
                [ActiveKey] = _active.ToList<object>()
            };

        // CLAUDE: silent, no events: runs in the load phase. Quests then
        // CLAUDE: carry on from these states in Start, without rerunning
        // CLAUDE: the instructions of steps already passed.
        public void Take(IReadOnlyDictionary<string, object> section)
        {
            _quests.Clear();
            foreach (var quest in Nested(section, QuestsKey))
                _quests[quest.Key] = (QuestState)Enum.Parse(
                    typeof(QuestState), (string)quest.Value);

            _outcomes.Clear();
            foreach (var outcome in Nested(section, OutcomesKey))
                _outcomes[outcome.Key] = (string)outcome.Value;

            _objectives.Clear();
            foreach (var quest in Nested(section, ObjectivesKey))
            foreach (var objective in
                     (IReadOnlyDictionary<string, object>)quest.Value)
                _objectives[(quest.Key, objective.Key)] =
                    (ObjectiveState)Enum.Parse(typeof(ObjectiveState),
                        (string)objective.Value);

            _active.Clear();
            foreach (var id in (IEnumerable<object>)section[ActiveKey])
                _active.Add((string)id);
        }

        private static IReadOnlyDictionary<string, object> Nested(
            IReadOnlyDictionary<string, object> section, string key) =>
            (IReadOnlyDictionary<string, object>)section[key];

        // CLAUDE: QuestRun never breaks these; if it does, it's our bug.
        private static void Require(bool holds, string problem)
        {
            if (!holds) throw new InvalidOperationException(problem);
        }
    }
}
