using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Oyung.SharedKernel;

namespace Oyung.Quests.Tests
{
    public sealed class QuestsTests
    {
        private Quests _quests;

        [TearDown]
        public void TearDown() => _quests?.Dispose();

        // --- start ---

        [Test]
        public void Starts_only_when_start_condition_met()
        {
            var start = new FakeCondition();
            Build(Quest("q", start, Objective("a")));

            Assert.AreEqual(QuestState.NotStarted, _quests.StateOf("q"));
            start.Set(true);
            Assert.AreEqual(QuestState.Active, _quests.StateOf("q"));
            CollectionAssert.AreEqual(new[] { "q" }, _quests.ActiveQuests);
        }

        [Test]
        public void Nothing_happens_before_Start()
        {
            var start = new FakeCondition();
            Build(Quest("q", start, Objective("a")), startNow: false);

            start.Set(true);
            Assert.AreEqual(QuestState.NotStarted, _quests.StateOf("q"));
            _quests.Start();
            Assert.AreEqual(QuestState.Active, _quests.StateOf("q"));
        }

        [Test]
        public void Start_condition_turning_false_does_not_unstart()
        {
            var start = new FakeCondition();
            Build(Quest("q", start, Objective("a")));
            start.Set(true);
            start.Set(false);
            Assert.AreEqual(QuestState.Active, _quests.StateOf("q"));
        }

        // --- objectives ---

        [Test]
        public void Objectives_appear_one_after_another_by_default()
        {
            var a = new FakeCondition();
            Build(Quest("q", Met(), Objective("a", done: a),
                Objective("b")));

            Assert.AreEqual(ObjectiveState.Shown, StateOf("q", "a"));
            Assert.AreEqual(ObjectiveState.Hidden, StateOf("q", "b"));
            a.Set(true);
            Assert.AreEqual(ObjectiveState.Done, StateOf("q", "a"));
            Assert.AreEqual(ObjectiveState.Shown, StateOf("q", "b"));
        }

        [Test]
        public void Objective_with_own_appears_when_shows_in_parallel()
        {
            var appears = new FakeCondition();
            Build(Quest("q", Met(), Objective("a"),
                Objective("b", appears: appears)));

            Assert.AreEqual(ObjectiveState.Hidden, StateOf("q", "b"));
            appears.Set(true);
            Assert.AreEqual(ObjectiveState.Shown, StateOf("q", "a"));
            Assert.AreEqual(ObjectiveState.Shown, StateOf("q", "b"));
        }

        [Test]
        public void Objective_done_before_asked_completes_on_appearing()
        {
            var seen = new List<ObjectiveState>();
            Build(Quest("q", Met(), Objective("a", done: Met()),
                Objective("b")), startNow: false);
            _quests.ObjectiveChanged += (quest, id, state) =>
            {
                if (id == "a") seen.Add(state);
            };
            _quests.Start();

            CollectionAssert.AreEqual(
                new[] { ObjectiveState.Shown, ObjectiveState.Done }, seen);
            Assert.AreEqual(ObjectiveState.Shown, StateOf("q", "b"));
        }

        [Test]
        public void Objective_fails_when_fail_condition_met()
        {
            var fails = new FakeCondition();
            Build(Quest("q", Met(), Objective("a", fails: fails)));
            fails.Set(true);
            Assert.AreEqual(ObjectiveState.Failed, StateOf("q", "a"));
        }

        [Test]
        public void Done_wins_when_done_and_failed_hold_together()
        {
            Build(Quest("q", Met(),
                Objective("a", done: Met(), fails: Met()),
                Objective("b")));
            Assert.AreEqual(ObjectiveState.Done, StateOf("q", "a"));
        }

        [Test]
        public void Failed_objective_lets_the_next_one_appear()
        {
            var fails = new FakeCondition();
            Build(Quest("q", Met(), Objective("a", fails: fails),
                Objective("b")));
            fails.Set(true);
            Assert.AreEqual(ObjectiveState.Shown, StateOf("q", "b"));
        }

        [Test]
        public void Done_condition_turning_false_keeps_it_done()
        {
            var done = new FakeCondition();
            Build(Quest("q", Met(), Objective("a", done: done),
                Objective("b")));
            done.Set(true);
            done.Set(false);
            Assert.AreEqual(ObjectiveState.Done, StateOf("q", "a"));
        }

        // --- outcomes ---

        [Test]
        public void Default_outcome_closes_when_required_objectives_done()
        {
            var a = new FakeCondition();
            Build(Quest("q", Met(), new[]
                {
                    Objective("a", done: a),
                    Objective("extra", optional: true, appears: Met())
                },
                new[] { Outcome("completed") }));

            Assert.AreEqual(QuestState.Active, _quests.StateOf("q"));
            a.Set(true);
            Assert.AreEqual(QuestState.Closed, _quests.StateOf("q"));
            Assert.AreEqual("completed", _quests.OutcomeOf("q"));
            CollectionAssert.IsEmpty(_quests.ActiveQuests);
        }

        [Test]
        public void First_reached_outcome_in_order_wins()
        {
            var both = new FakeCondition();
            Build(Quest("q", Met(), new[] { Objective("a") },
                new[] { Outcome("good", both), Outcome("bad", both) }));
            both.Set(true);
            Assert.AreEqual("good", _quests.OutcomeOf("q"));
        }

        [Test]
        public void Failure_is_an_outcome_reached_by_a_failed_objective()
        {
            // The designer would use QuestCondition here; the fake reads
            // the same answer from the port.
            var fails = new FakeCondition();
            var failed = new FakeCondition();
            Build(Quest("q", Met(), new[] { Objective("a", fails: fails) },
                new[] { Outcome("done"), Outcome("failed", failed) }));
            _quests.ObjectiveChanged += (quest, id, state) =>
                failed.Set(state == ObjectiveState.Failed);

            fails.Set(true);
            Assert.AreEqual("failed", _quests.OutcomeOf("q"));
        }

        [Test]
        public void Closed_quest_stops_listening()
        {
            var start = new FakeCondition();
            var done = new FakeCondition();
            var ending = new FakeCondition();
            Build(Quest("q", start, new[] { Objective("a", done: done) },
                new[] { Outcome("end", ending) }));
            start.Set(true);
            Assert.AreEqual(1, done.ListenerCount);
            ending.Set(true);

            Assert.AreEqual(0, start.ListenerCount);
            Assert.AreEqual(0, done.ListenerCount);
            Assert.AreEqual(0, ending.ListenerCount);
            done.Set(true);
            Assert.AreEqual(ObjectiveState.Shown, StateOf("q", "a"));
        }

        [Test]
        public void Listens_only_to_what_can_move_it_next()
        {
            var start = new FakeCondition();
            var bDone = new FakeCondition();
            Build(Quest("q", start, Objective("a"),
                Objective("b", done: bDone)));
            Assert.AreEqual(1, start.ListenerCount);
            Assert.AreEqual(0, bDone.ListenerCount);
            start.Set(true);
            Assert.AreEqual(0, start.ListenerCount);
            Assert.AreEqual(0, bDone.ListenerCount); // b still hidden
        }

        [Test]
        public void Dispose_unsubscribes_everything()
        {
            var start = new FakeCondition();
            Build(Quest("q", start, Objective("a")));
            _quests.Dispose();
            Assert.AreEqual(0, start.ListenerCount);
            start.Set(true);
            Assert.AreEqual(QuestState.NotStarted, _quests.StateOf("q"));
        }

        // --- instructions and chains ---

        [Test]
        public void Instructions_run_once_at_each_step()
        {
            var onStart = new FakeInstruction();
            var onShown = new FakeInstruction();
            var onDone = new FakeInstruction();
            var onEnd = new FakeInstruction();
            var done = new FakeCondition();
            Build(new QuestDefinition("q", Met(), new[] { onStart },
                new[]
                {
                    new ObjectiveDefinition("a", false, null, done, null,
                        new[] { onShown }, new[] { onDone }, null)
                },
                new[]
                {
                    new OutcomeDefinition("end", null, new[] { onEnd })
                }));

            done.Set(true);
            done.Set(false);
            done.Set(true);
            Assert.AreEqual(new[] { 1, 1, 1, 1 }, new[]
            {
                onStart.Count, onShown.Count, onDone.Count, onEnd.Count
            });
        }

        [Test]
        public void Instruction_of_one_quest_can_start_another()
        {
            var bStart = new FakeCondition();
            var aDone = new FakeCondition();
            var a = new QuestDefinition("a", Met(), null,
                new[] { Objective("x", done: aDone) },
                new[]
                {
                    new OutcomeDefinition("end", null,
                        new[] { new FakeInstruction(() => bStart.Set(true)) })
                });
            Build(new[] { a, Quest("b", bStart, Objective("y")) });

            aDone.Set(true);
            Assert.AreEqual(QuestState.Closed, _quests.StateOf("a"));
            Assert.AreEqual(QuestState.Active, _quests.StateOf("b"));
        }

        [Test]
        public void Own_instruction_meeting_own_condition_is_not_missed()
        {
            // On-shown of "a" makes "a" done at once.
            var done = new FakeCondition();
            Build(new QuestDefinition("q", Met(), null,
                new[]
                {
                    new ObjectiveDefinition("a", false, null, done, null,
                        new[] { new FakeInstruction(() => done.Set(true)) },
                        null, null),
                    Objective("b")
                },
                new[] { Outcome("end") }));
            Assert.AreEqual(ObjectiveState.Done, StateOf("q", "a"));
            Assert.AreEqual(ObjectiveState.Shown, StateOf("q", "b"));
        }

        [Test]
        public void Evaluations_do_not_nest()
        {
            // While "a" closes, its first instruction wakes "b"; "b" must
            // be evaluated only after "a" has run all its instructions.
            var log = new List<string>();
            var trigger = new FakeCondition();
            var bStart = new FakeCondition();
            var a = new QuestDefinition("a", Met(), null,
                new[] { Objective("x", done: trigger) },
                new[]
                {
                    new OutcomeDefinition("end", null, new Instruction[]
                    {
                        new FakeInstruction(() => bStart.Set(true)),
                        new FakeInstruction(() => log.Add("a finished"))
                    })
                });
            Build(new[] { a, Quest("b", bStart, Objective("y")) },
                startNow: false);
            _quests.QuestStarted += id => log.Add("started " + id);
            _quests.Start();
            log.Clear();

            trigger.Set(true);
            CollectionAssert.AreEqual(
                new[] { "a finished", "started b" }, log);
        }

        [Test]
        public void Events_report_every_step()
        {
            var log = new List<string>();
            Build(Quest("q", Met(), Objective("a", done: Met())),
                startNow: false);
            _quests.QuestStarted += id => log.Add("start " + id);
            _quests.ObjectiveChanged += (quest, id, state) =>
                log.Add(id + " " + state);
            _quests.QuestClosed += (quest, outcome) =>
                log.Add("close " + outcome);
            _quests.Start();

            CollectionAssert.AreEqual(new[]
            {
                "start q", "a Shown", "a Done", "close end"
            }, log);
        }

        [Test]
        public void ObjectivesOf_lists_all_in_order_with_state()
        {
            Build(Quest("q", Met(), Objective("a"),
                Objective("b", optional: true, appears: Met())));
            var infos = _quests.ObjectivesOf("q");
            CollectionAssert.AreEqual(new[] { "a", "b" },
                infos.Select(i => i.Id));
            Assert.IsTrue(infos[1].IsOptional);
            Assert.AreEqual(ObjectiveState.Shown, infos[1].State);
        }

        // --- designer mistakes fail at build ---

        [Test]
        public void Bad_definitions_throw()
        {
            Assert.Throws<InvalidOperationException>(() =>
                Quest("q", null, Objective("a")));
            Assert.Throws<InvalidOperationException>(() =>
                new QuestDefinition("q", Met(), null,
                    new[] { Objective("a") }, null));
            Assert.Throws<InvalidOperationException>(() =>
                Quest("q", Met(), Objective("a"), Objective("a")));
            Assert.Throws<InvalidOperationException>(() =>
                Quest("q", Met(), Objective("a", optional: true)));
            Assert.Throws<InvalidOperationException>(() =>
                new ObjectiveDefinition("a", false, null, null, null,
                    null, null, null));
            Assert.Throws<InvalidOperationException>(() =>
                new OutcomeDefinition("o", null, new Instruction[] { null }));
            Assert.Throws<InvalidOperationException>(() =>
                new Quests(new[]
                {
                    Quest("q", Met(), Objective("a")),
                    Quest("q", Met(), Objective("a"))
                }, new QuestJournal()));
        }

        [Test]
        public void Unknown_quest_id_throws()
        {
            Build(Quest("q", Met(), Objective("a")));
            Assert.Throws<KeyNotFoundException>(() =>
                _quests.StateOf("nope"));
        }

        // --- helpers ---

        private void Build(QuestDefinition quest, bool startNow = true) =>
            Build(new[] { quest }, startNow);

        private void Build(IEnumerable<QuestDefinition> quests,
            bool startNow = true)
        {
            _quests = new Quests(quests, new QuestJournal());
            if (startNow) _quests.Start();
        }

        private ObjectiveState StateOf(string quest, string objective) =>
            _quests.ObjectivesOf(quest).Single(o => o.Id == objective).State;

        private static FakeCondition Met() => new FakeCondition(true);

        private static QuestDefinition Quest(string id, Condition start,
            params ObjectiveDefinition[] objectives) =>
            Quest(id, start, objectives, new[] { Outcome("end") });

        private static QuestDefinition Quest(string id, Condition start,
            ObjectiveDefinition[] objectives, OutcomeDefinition[] outcomes) =>
            new QuestDefinition(id, start, null, objectives, outcomes);

        private static ObjectiveDefinition Objective(string id,
            Condition done = null, Condition fails = null,
            Condition appears = null, bool optional = false) =>
            new ObjectiveDefinition(id, optional, appears,
                done ?? new FakeCondition(), fails, null, null, null);

        private static OutcomeDefinition Outcome(string id,
            Condition reached = null) =>
            new OutcomeDefinition(id, reached, null);
    }
}
