using System.Collections.Generic;
using NUnit.Framework;

namespace Oyung.Cutscenes.Tests
{
    public class CutsceneStoreSaveTests
    {
        [Test]
        public void Played_cutscenes_come_back()
        {
            var saved = new CutsceneStore();
            saved.MarkPlayed("intro");

            var loaded = new CutsceneStore();
            loaded.Take(saved.Give());

            Assert.IsTrue(loaded.IsPlayed("intro"));
            Assert.IsFalse(loaded.IsPlayed("other"));
        }

        [Test]
        public void Take_reads_a_list_as_a_save_returns_it()
        {
            var store = new CutsceneStore();

            store.Take(new Dictionary<string, object>
            {
                ["played"] = new List<object> { "intro" }
            });

            Assert.IsTrue(store.IsPlayed("intro"));
        }

        [Test]
        public void Take_is_silent()
        {
            var saved = new CutsceneStore();
            saved.MarkPlayed("intro");
            var loaded = new CutsceneStore();
            var calls = 0;
            loaded.Subscribe("intro", () => calls++);

            loaded.Take(saved.Give());

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Take_replaces_what_was_there()
        {
            var store = new CutsceneStore();
            store.MarkPlayed("old");

            store.Take(new CutsceneStore().Give());

            Assert.IsFalse(store.IsPlayed("old"));
        }
    }
}
