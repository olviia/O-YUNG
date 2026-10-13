using System;
using System.Collections.Generic;
using System.Linq;
using Oyung.SharedKernel;
using Oyung.SharedKernel.Unity;
using UnityEngine;
using UnityEngine.Localization;

namespace Oyung.Quests.Unity
{
    // CLAUDE: one quest as the designer writes it: texts, conditions and
    // CLAUDE: instructions inline. Does: translates itself to a plain
    // CLAUDE: QuestDefinition for the core (the core never sees Unity).
    // CLAUDE: Value = the quest's state (proxy to the journal), so other
    // CLAUDE: modules can read it; QuestCondition asks finer questions.
    [CreateAssetMenu(menuName = "Oyung/Quests/Quest",
        fileName = "NewQuest")]
    public sealed class QuestAsset : ModuleFact<QuestState>
    {
        [SerializeField] private LocalizedString title;

        [SerializeReference] private Condition startWhen;
        [SerializeReference] private List<Instruction> onStart =
            new List<Instruction>();

        [SerializeField] private List<ObjectiveData> objectives =
            new List<ObjectiveData>();

        [Tooltip("Ways the quest can end, in priority order: when several " +
                 "are reached at once, the first wins. Empty Reached-when " +
                 "= all required objectives done.")]
        [SerializeField] private List<OutcomeData> outcomes =
            new List<OutcomeData> { new OutcomeData() };

        private QuestJournal _journal;
        private QuestState _lastState;

        public override QuestState Value => Journal.StateOf(Name);

        /// <summary>Raised on any change of this quest, objectives
        /// included (Changed: only when Value changes).</summary>
        internal event Action Progressed;

        /// <summary>The outcome the quest closed with; null until closed.
        /// </summary>
        internal string Outcome => Journal.OutcomeOf(Name);

        internal ObjectiveState StateOf(string objectiveId) =>
            Journal.StateOf(Name, objectiveId);

        internal LocalizedString Title => title;

        internal LocalizedString ObjectiveText(string objectiveId) =>
            FindObjective(objectiveId).Text;

        internal bool HasObjective(string id) =>
            objectives.Any(o => o.Id == id);

        internal bool HasOutcome(string id) =>
            outcomes.Any(o => o.Id == id);

        /// <summary>The same quest as plain data. Throws on designer
        /// mistakes (missing conditions, duplicate ids).</summary>
        internal QuestDefinition ToDefinition()
        {
            try
            {
                return new QuestDefinition(Name, startWhen, onStart,
                    objectives.Select(o => o.ToDefinition()),
                    outcomes.Select(o => o.ToDefinition()));
            }
            catch (Exception e) when (e is InvalidOperationException ||
                                      e is ArgumentException)
            {
                // CLAUDE: name the asset, so the error leads straight to it.
                throw new InvalidOperationException(
                    $"Quest asset '{name}': {e.Message}", e);
            }
        }

        // CLAUDE: create phase: Value and Changed work; the core moves
        // CLAUDE: nothing until Quests.Start.
        internal void Bind(QuestJournal journal)
        {
            _journal = journal;
            _lastState = journal.StateOf(Name);
            _journal.Subscribe(Name, OnJournalChanged);
        }

        internal void Unbind()
        {
            _journal?.Unsubscribe(Name, OnJournalChanged);
            _journal = null;
        }

        private void OnJournalChanged()
        {
            Progressed?.Invoke();
            var state = Value;
            if (state == _lastState) return;
            _lastState = state;
            RaiseChanged();
        }

        private ObjectiveData FindObjective(string id) =>
            objectives.FirstOrDefault(o => o.Id == id) ??
            throw new KeyNotFoundException(
                $"Quest '{Name}' has no objective '{id}'.");

        private QuestJournal Journal => _journal ??
            throw new InvalidOperationException(
                $"Quest '{Name}' is not bound. Is it in the catalog?");

        [Serializable]
        private sealed class ObjectiveData
        {
            [Tooltip("Stable id, used by saves and QuestConditions.")]
            [SerializeField] private string id;
            [SerializeField] private LocalizedString text;
            [SerializeField] private bool optional;

            [Tooltip("Empty: appears once the previous objective is done " +
                     "or failed (the first: when the quest starts).")]
            [SerializeReference] private Condition appearsWhen;
            [SerializeReference] private Condition doneWhen;
            [Tooltip("Empty: can't fail.")]
            [SerializeReference] private Condition failsWhen;

            [SerializeReference] private List<Instruction> onShown =
                new List<Instruction>();
            [SerializeReference] private List<Instruction> onDone =
                new List<Instruction>();
            [SerializeReference] private List<Instruction> onFailed =
                new List<Instruction>();

            public string Id => id;
            public LocalizedString Text => text;

            public ObjectiveDefinition ToDefinition() =>
                new ObjectiveDefinition(id, optional, appearsWhen, doneWhen,
                    failsWhen, onShown, onDone, onFailed);
        }

        [Serializable]
        private sealed class OutcomeData
        {
            [Tooltip("Stable id, e.g. completed, failed, spared_the_cat.")]
            [SerializeField] private string id = "completed";
            [SerializeReference] private Condition reachedWhen;
            [SerializeReference] private List<Instruction> onReached =
                new List<Instruction>();

            public string Id => id;

            public OutcomeDefinition ToDefinition() =>
                new OutcomeDefinition(id, reachedWhen, onReached);
        }
    }
}
