using System.Collections.Generic;
using NUnit.Framework;
using Oyung.SharedKernel;

namespace Oyung.Quests.Tests
{
    public sealed class QuestJournalSaveTests
    {
        private static QuestJournal Restored(QuestJournal saved)
        {
            var restored = new QuestJournal();
            restored.Take(saved.Give());
            return restored;
        }

        [Test]
        public void Every_state_comes_back()
        {
            var saved = new QuestJournal();
            saved.Start("open");
            saved.SetObjective("open", "a", ObjectiveState.Shown);
            saved.Start("closed");
            saved.SetObjective("closed", "b", ObjectiveState.Failed);
            saved.Close("closed", "lost");

            var loaded = Restored(saved);

            Assert.AreEqual(QuestState.Active, loaded.StateOf("open"));
            Assert.AreEqual(ObjectiveState.Shown,
                loaded.StateOf("open", "a"));
            Assert.AreEqual(QuestState.Closed, loaded.StateOf("closed"));
            Assert.AreEqual(ObjectiveState.Failed,
                loaded.StateOf("closed", "b"));
            Assert.AreEqual("lost", loaded.OutcomeOf("closed"));
        }

        [Test]
        public void Active_quests_keep_their_start_order()
        {
            var saved = new QuestJournal();
            saved.Start("second_by_name");
            saved.Start("first_by_name");

            CollectionAssert.AreEqual(
                new[] { "second_by_name", "first_by_name" },
                Restored(saved).ActiveQuests);
        }

        [Test]
        public void States_are_saved_by_name()
        {
            var saved = new QuestJournal();
            saved.Start("q");

            var quests = (IReadOnlyDictionary<string, object>)
                saved.Give()["quests"];

            Assert.AreEqual("Active", quests["q"]);
        }

        [Test]
        public void Take_is_silent()
        {
            var saved = new QuestJournal();
            saved.Start("q");
            var loaded = new QuestJournal();
            var calls = 0;
            loaded.QuestStarted += _ => calls++;
            loaded.Subscribe("q", () => calls++);

            loaded.Take(saved.Give());

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void A_restored_quest_runs_no_instruction_twice_and_carries_on()
        {
            var onStart = new FakeInstruction();
            var onShown = new FakeInstruction();
            var onDone = new FakeInstruction();
            var met = new FakeCondition();
            met.Set(true);
            var done = new FakeCondition();
            QuestDefinition Definition() => new QuestDefinition("q", met,
                new Instruction[] { onStart },
                new[]
                {
                    new ObjectiveDefinition("a", false, null, done, null,
                        new Instruction[] { onShown },
                        new Instruction[] { onDone }, null)
                },
                new[] { new OutcomeDefinition("end", null, null) });

            var saved = new QuestJournal();
            var before = new Quests(new[] { Definition() }, saved);
            before.Start();
            before.Dispose();
            Assert.AreEqual(1, onStart.Count);
            Assert.AreEqual(1, onShown.Count);

            var loaded = Restored(saved);
            var after = new Quests(new[] { Definition() }, loaded);
            after.Start();

            Assert.AreEqual(1, onStart.Count, "start ran again");
            Assert.AreEqual(1, onShown.Count, "shown ran again");

            done.Set(true);

            Assert.AreEqual(1, onDone.Count);
            Assert.AreEqual(QuestState.Closed, loaded.StateOf("q"));
            after.Dispose();
        }
    }
}
