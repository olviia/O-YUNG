using System.Collections.Generic;
using NUnit.Framework;

namespace Oyung.Globals.Tests
{
    public class GlobalStoreSaveTests
    {
        [Test]
        public void Give_holds_every_kind_of_value()
        {
            var store = new GlobalStore();
            store.Set("heard", true);
            store.Set("flowers", 3);
            store.Set("name", "Xzqk");

            var section = store.Give();

            Assert.AreEqual(true, section["heard"]);
            Assert.AreEqual(3, section["flowers"]);
            Assert.AreEqual("Xzqk", section["name"]);
        }

        [Test]
        public void A_whole_number_back_from_a_save_reads_as_an_int()
        {
            var store = new GlobalStore();

            store.Take(new Dictionary<string, object> { ["flowers"] = 3L });

            Assert.AreEqual(3, store.Get<int>("flowers"));
        }

        [Test]
        public void Bool_and_string_come_back()
        {
            var store = new GlobalStore();

            store.Take(new Dictionary<string, object>
            {
                ["heard"] = true,
                ["name"] = "Xzqk"
            });

            Assert.IsTrue(store.Get<bool>("heard"));
            Assert.AreEqual("Xzqk", store.Get<string>("name"));
        }

        [Test]
        public void Take_is_silent()
        {
            var store = new GlobalStore();
            var calls = 0;
            store.Subscribe("heard", () => calls++);

            store.Take(new Dictionary<string, object> { ["heard"] = true });

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Take_replaces_what_was_there()
        {
            var store = new GlobalStore();
            store.Set("old", true);

            store.Take(new Dictionary<string, object>());

            Assert.IsFalse(store.Get<bool>("old"));
        }

        [Test]
        public void A_name_never_set_reads_null_as_a_string()
        {
            Assert.IsNull(new GlobalStore().Get<string>("name"));
        }
    }
}
