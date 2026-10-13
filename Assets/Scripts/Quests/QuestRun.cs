using System;
using System.Collections.Generic;
using System.Linq;
using Oyung.SharedKernel;

namespace Oyung.Quests
{
    // CLAUDE: the rules for one quest. Knows its definition, the journal,
    // CLAUDE: and which conditions it listens to right now. Does: on
    // CLAUDE: Evaluate, moves the quest as far as its conditions allow
    // CLAUDE: (start, show, done/fail, close), runs the instructions, then
    // CLAUDE: listens only to the conditions that can move it next.
    // CLAUDE: Never evaluates on its own: a Changed only asks Quests to
    // CLAUDE: schedule it (see Quests: why a queue).
    internal sealed class QuestRun
    {
        private readonly QuestDefinition _definition;
        private readonly QuestJournal _journal;
        private readonly Action _wake;
        private readonly List<Condition> _watched = new List<Condition>();

        internal QuestRun(QuestDefinition definition, QuestJournal journal,
            Action<QuestRun> schedule)
        {
            _definition = definition;
            _journal = journal;
            // CLAUDE: one delegate instance, so -= removes what += added.
            _wake = () => schedule(this);
        }

        internal string Id => _definition.Id;

        internal void Evaluate()
        {
            // CLAUDE: stop listening while we move: our own instructions
            // CLAUDE: may change what our conditions read. The loop below
            // CLAUDE: re-checks after every change, so nothing is missed.
            Unwatch();

            if (State == QuestState.NotStarted)
            {
                if (!_definition.StartWhen.IsMet)
                {
                    Watch(_definition.StartWhen);
                    return;
                }
                _journal.Start(Id);
                Instructions.ExecuteAll(_definition.OnStart);
            }

            if (State != QuestState.Active) return;

            while (AdvanceObjectives())
            {
            }

            var reached = _definition.Outcomes.FirstOrDefault(IsReached);
            if (reached != null)
            {
                _journal.Close(Id, reached.Id);
                Instructions.ExecuteAll(reached.OnReached);
                return;
            }

            WatchActive();
        }

        internal void Stop() => Unwatch();

        private QuestState State => _journal.StateOf(Id);

        // CLAUDE: one pass over the objectives in order; true if anything
        // CLAUDE: moved, so the caller passes again ("after previous"
        // CLAUDE: chains and instructions may unlock more).
        private bool AdvanceObjectives()
        {
            var moved = false;
            var objectives = _definition.Objectives;
            for (var i = 0; i < objectives.Count; i++)
            {
                var objective = objectives[i];
                var state = StateOf(objective);

                if (state == ObjectiveState.Hidden && Appears(i))
                {
                    Set(objective, ObjectiveState.Shown, objective.OnShown);
                    state = ObjectiveState.Shown;
                    moved = true;
                }

                // CLAUDE: checked right on appearing: done before asked
                // CLAUDE: (KCD: you already have the item) completes at
                // CLAUDE: once. Done wins over failed when both hold.
                if (state != ObjectiveState.Shown) continue;
                if (objective.DoneWhen.IsMet)
                {
                    Set(objective, ObjectiveState.Done, objective.OnDone);
                    moved = true;
                }
                else if (objective.FailsWhen != null &&
                         objective.FailsWhen.IsMet)
                {
                    Set(objective, ObjectiveState.Failed, objective.OnFailed);
                    moved = true;
                }
            }
            return moved;
        }

        private bool Appears(int index)
        {
            var objective = _definition.Objectives[index];
            if (objective.AppearsWhen != null)
                return objective.AppearsWhen.IsMet;
            if (index == 0) return true;
            var previous = StateOf(_definition.Objectives[index - 1]);
            return previous == ObjectiveState.Done ||
                   previous == ObjectiveState.Failed;
        }

        private bool IsReached(OutcomeDefinition outcome) =>
            outcome.ReachedWhen?.IsMet ??
            _definition.Objectives
                .Where(o => !o.IsOptional)
                .All(o => StateOf(o) == ObjectiveState.Done);

        private void Set(ObjectiveDefinition objective, ObjectiveState state,
            IReadOnlyList<Instruction> instructions)
        {
            _journal.SetObjective(Id, objective.Id, state);
            Instructions.ExecuteAll(instructions);
        }

        private ObjectiveState StateOf(ObjectiveDefinition objective) =>
            _journal.StateOf(Id, objective.Id);

        // CLAUDE: only what can move the quest next. "After previous" and
        // CLAUDE: default outcomes have no condition: they follow from
        // CLAUDE: objective changes, which happen inside Evaluate.
        private void WatchActive()
        {
            foreach (var objective in _definition.Objectives)
            {
                var state = StateOf(objective);
                if (state == ObjectiveState.Hidden)
                {
                    if (objective.AppearsWhen != null)
                        Watch(objective.AppearsWhen);
                }
                else if (state == ObjectiveState.Shown)
                {
                    Watch(objective.DoneWhen);
                    if (objective.FailsWhen != null)
                        Watch(objective.FailsWhen);
                }
            }
            foreach (var outcome in _definition.Outcomes)
                if (outcome.ReachedWhen != null)
                    Watch(outcome.ReachedWhen);
        }

        private void Watch(Condition condition)
        {
            condition.Changed += _wake;
            _watched.Add(condition);
        }

        private void Unwatch()
        {
            foreach (var condition in _watched) condition.Changed -= _wake;
            _watched.Clear();
        }
    }
}
