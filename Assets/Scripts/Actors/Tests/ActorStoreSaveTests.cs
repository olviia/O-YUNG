using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Oyung.Actors.Tests
{
    public class ActorStoreSaveTests
    {
        private static ActorStore NewStore() => new ActorStore(() => { });

        private static readonly ActorState Hugging = new ActorState(
            new Vector3(1f, 2f, 3f), new Quaternion(0f, 0.6f, 0f, 0.8f),
            "Hug");

        [Test]
        public void State_comes_back()
        {
            var saved = NewStore();
            saved.Set("mum", Hugging);

            var loaded = NewStore();
            loaded.Take(saved.Give());

            Assert.IsTrue(loaded.TryGet("mum", out var state));
            Assert.AreEqual(Hugging.Position, state.Position);
            Assert.AreEqual(Hugging.Rotation, state.Rotation);
            Assert.AreEqual("Hug", state.Pose);
        }

        [Test]
        public void Numbers_as_a_save_returns_them_are_read()
        {
            var store = NewStore();

            store.Take(new Dictionary<string, object>
            {
                ["mum"] = new Dictionary<string, object>
                {
                    ["position"] = new List<object> { 1.5d, 3L, 0d },
                    ["rotation"] = new List<object> { 0d, 0d, 0d, 1L },
                    ["pose"] = null
                }
            });

            store.TryGet("mum", out var state);
            Assert.AreEqual(new Vector3(1.5f, 3f, 0f), state.Position);
            Assert.AreEqual(Quaternion.identity, state.Rotation);
            Assert.IsNull(state.Pose);
        }

        [Test]
        public void An_actor_never_seen_has_no_state()
        {
            Assert.IsFalse(NewStore().TryGet("mum", out _));
        }

        [Test]
        public void Give_asks_for_the_live_actors_first()
        {
            ActorStore store = null;
            store = new ActorStore(() => store.Set("mum", Hugging));

            Assert.IsTrue(store.Give().ContainsKey("mum"));
        }

        [Test]
        public void Take_replaces_what_was_there()
        {
            var store = NewStore();
            store.Set("old", Hugging);

            store.Take(new Dictionary<string, object>());

            Assert.IsFalse(store.TryGet("old", out _));
        }
    }
}
