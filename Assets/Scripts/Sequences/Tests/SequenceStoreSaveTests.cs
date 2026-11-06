using System.Collections.Generic;
using NUnit.Framework;

namespace Oyung.Sequences.Tests
{
    public class SequenceStoreSaveTests
    {
        [Test]
        public void Give_holds_each_sequences_step()
        {
            var store = new SequenceStore();
            store.SetStep("mum_arrives", 2);

            Assert.AreEqual(2, store.Give()["mum_arrives"]);
        }

        [Test]
        public void A_step_back_from_a_save_is_read_from_its_long()
        {
            var store = new SequenceStore();

            store.Take(new Dictionary<string, object>
            {
                ["mum_arrives"] = 2L
            });

            Assert.AreEqual(2, store.StepOf("mum_arrives"));
        }

        [Test]
        public void Take_replaces_what_was_there()
        {
            var store = new SequenceStore();
            store.SetStep("old", 3);

            store.Take(new Dictionary<string, object>());

            Assert.AreEqual(0, store.StepOf("old"));
        }
    }
}
