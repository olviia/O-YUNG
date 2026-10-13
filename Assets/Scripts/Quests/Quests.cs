using System;
using System.Collections.Generic;
using System.Linq;

namespace Oyung.Quests
{
    // CLAUDE: knows every quest's QuestRun and which of them must be
    // CLAUDE: evaluated. Does: schedules evaluations one at a time;
    // CLAUDE: answers IQuests from the journal. The rules themselves are
    // CLAUDE: in QuestRun, the state in QuestJournal.
    //
    // CLAUDE: Shape (KCD2's player-facing quests; their internals unknown):
    // CLAUDE: quest = start condition + objectives + outcomes.
    // CLAUDE: objective = appears when (empty: after previous), done
    // CLAUDE:   when, fails when (empty: never), optional.
    // CLAUDE: outcome = reached when (empty: all required objectives
    // CLAUDE:   done); the first reached closes the quest. A failure is
    // CLAUDE:   just an outcome, e.g. reached when an objective failed.
    // CLAUDE: Instructions run on start / shown / done / failed / outcome.
    // CLAUDE: Quests read each other through QuestCondition.
    // CLAUDE: Everything sticky; ids, not positions.
    // CLAUDE: Not here: quest log notes (newborn-design: no quest log),
    // CLAUDE: timers (a clock module offers a fact), save (with Save).
    //
    // CLAUDE: Why a queue: an instruction run by one quest can change a
    // CLAUDE: fact another quest (or the same) listens to. Evaluating
    // CLAUDE: right inside that Changed would nest evaluations and
    // CLAUDE: interleave half-done state. Instead every Changed only
    // CLAUDE: queues its quest; the queue drains one quest at a time.
    // CLAUDE: Same idea as mechaniqe/unity-quest-core's dirty queue.
    internal sealed class Quests : IQuests, IDisposable
    {
        // CLAUDE: quests waking each other forever (A's instruction meets
        // CLAUDE: B, B's meets A...) can't happen with sticky states, but a
        // CLAUDE: bug in a condition could; fail loudly instead of hanging.
        private const int MaxEvaluationsPerDrain = 10000;

        private readonly QuestJournal _journal;
        private readonly Dictionary<string, QuestRun> _runs;
        private readonly Dictionary<string, QuestDefinition> _definitions;
        private readonly Queue<QuestRun> _queue = new Queue<QuestRun>();
        private readonly HashSet<QuestRun> _queued = new HashSet<QuestRun>();
        private bool _started;
        private bool _draining;

        internal Quests(IEnumerable<QuestDefinition> definitions,
            QuestJournal journal)
        {
            _journal = journal;
            var list = definitions.ToList();
            var duplicate = list.GroupBy(d => d.Id)
                .FirstOrDefault(g => g.Count() > 1);
            if (duplicate != null)
                throw new InvalidOperationException(
                    $"Quest id '{duplicate.Key}' is used twice.");
            _definitions = list.ToDictionary(d => d.Id);
            _runs = list.ToDictionary(d => d.Id,
                d => new QuestRun(d, journal, Schedule));
        }

        public event Action<string> QuestStarted
        {
            add => _journal.QuestStarted += value;
            remove => _journal.QuestStarted -= value;
        }

        public event Action<string, string, ObjectiveState> ObjectiveChanged
        {
            add => _journal.ObjectiveChanged += value;
            remove => _journal.ObjectiveChanged -= value;
        }

        public event Action<string, string> QuestClosed
        {
            add => _journal.QuestClosed += value;
            remove => _journal.QuestClosed -= value;
        }

        public IReadOnlyList<string> ActiveQuests => _journal.ActiveQuests;

        public QuestState StateOf(string questId) =>
            _journal.StateOf(Definition(questId).Id);

        public string OutcomeOf(string questId) =>
            _journal.OutcomeOf(Definition(questId).Id);

        public IReadOnlyList<ObjectiveInfo> ObjectivesOf(string questId) =>
            Definition(questId).Objectives
                .Select(o => new ObjectiveInfo(o.Id, o.IsOptional,
                    _journal.StateOf(questId, o.Id)))
                .ToList();

        /// <summary>Start phase, after load: every quest checks its
        /// conditions once and from now on listens to them.</summary>
        internal void Start()
        {
            _started = true;
            foreach (var run in _runs.Values) Enqueue(run);
            Drain();
        }

        public void Dispose()
        {
            _started = false;
            foreach (var run in _runs.Values) run.Stop();
            _queue.Clear();
            _queued.Clear();
        }

        private void Schedule(QuestRun run)
        {
            if (!_started) return;
            Enqueue(run);
            Drain();
        }

        private void Enqueue(QuestRun run)
        {
            if (_queued.Add(run)) _queue.Enqueue(run);
        }

        // CLAUDE: re-entrant calls (a Changed raised while we evaluate)
        // CLAUDE: only enqueue; the outer loop picks them up.
        private void Drain()
        {
            if (_draining) return;
            _draining = true;
            try
            {
                var count = 0;
                while (_queue.Count > 0)
                {
                    if (++count > MaxEvaluationsPerDrain)
                        throw new InvalidOperationException(
                            "Quests keep waking each other; a condition " +
                            "that changes on every check?");
                    var run = _queue.Dequeue();
                    _queued.Remove(run);
                    run.Evaluate();
                }
            }
            catch
            {
                _queue.Clear();
                _queued.Clear();
                throw;
            }
            finally
            {
                _draining = false;
            }
        }

        // CLAUDE: an unknown id is a caller's mistake (typo, wrong asset):
        // CLAUDE: fail fast rather than answer "not started" forever.
        private QuestDefinition Definition(string questId) =>
            _definitions.TryGetValue(questId, out var definition)
                ? definition
                : throw new KeyNotFoundException(
                    $"No quest '{questId}'.");
    }
}
