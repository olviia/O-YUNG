using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Oyung.Save.Adapters;

namespace Oyung.Save.Tests
{
    using Section = IReadOnlyDictionary<string, object>;

    public class SaveFileTests
    {
        private enum Mood { Happy }

        private string _folder;

        [SetUp]
        public void SetUp() => _folder = Path.Combine(
            Path.GetTempPath(), Path.GetRandomFileName());

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
        }

        private static Dictionary<string, Section> Globals(string key,
            object value) => new Dictionary<string, Section>
        {
            ["globals"] = new Dictionary<string, object> { [key] = value }
        };

        private static object RoundTrip(object value) => SaveFileJson
            .Parse(SaveFileJson.Compose(Globals("key", value)))
            ["globals"]["key"];

        [Test]
        public void Bool_and_string_come_back_as_they_went_in()
        {
            Assert.AreEqual(true, RoundTrip(true));
            Assert.AreEqual("Xzqk", RoundTrip("Xzqk"));
        }

        [Test]
        public void Whole_numbers_come_back_as_long()
        {
            Assert.IsInstanceOf<long>(RoundTrip(3));
            Assert.AreEqual(3L, RoundTrip(3));
        }

        [Test]
        public void A_decimal_comes_back_as_double_that_fits_its_float()
        {
            var back = RoundTrip(0.1f);

            Assert.IsInstanceOf<double>(back);
            Assert.AreEqual(0.1f, (float)(double)back);
        }

        [Test]
        public void A_whole_valued_decimal_stays_a_decimal()
        {
            Assert.IsInstanceOf<double>(RoundTrip(3.0f));
        }

        [Test]
        public void Nested_sections_and_lists_round_trip()
        {
            var back = (Section)RoundTrip(new Dictionary<string, object>
            {
                ["active"] = new List<object> { "first_quest", 2 }
            });

            var active = (List<object>)back["active"];
            Assert.AreEqual("first_quest", active[0]);
            Assert.AreEqual(2L, active[1]);
        }

        [Test]
        public void A_list_of_strings_is_a_list()
        {
            var back = (List<object>)RoundTrip(new List<string> { "a" });

            Assert.AreEqual("a", back[0]);
        }

        [Test]
        public void A_value_that_looks_like_a_date_stays_as_it_was()
        {
            const string text = "2020-01-01T00:00:00";

            Assert.AreEqual(text, RoundTrip(text));
        }

        [Test]
        public void A_value_a_save_cant_hold_throws_naming_its_type()
        {
            var problem = Assert.Throws<ArgumentException>(
                () => SaveFileJson.Compose(Globals("mood", Mood.Happy)));

            StringAssert.Contains(nameof(Mood), problem.Message);
        }

        [Test]
        public void File_is_nested_json_a_person_can_read()
        {
            var text = SaveFileJson.Compose(Globals("flowers", 3));

            StringAssert.Contains("\"flowers\": 3", text);
            StringAssert.DoesNotContain("\\\"", text);
        }

        [TestCase("not json at all")]
        [TestCase("[1,2]")]
        [TestCase("{\"sections\":{}}")]
        [TestCase("{\"version\":\"one\",\"sections\":{}}")]
        [TestCase("{\"version\":1}")]
        [TestCase("{\"version\":1,\"sections\":{\"globals\":3}}")]
        public void Text_that_is_not_a_save_throws(string text)
        {
            Assert.That(() => SaveFileJson.Parse(text), Throws.Exception);
        }

        [Test]
        public void A_save_from_a_newer_build_throws()
        {
            Assert.Throws<InvalidDataException>(() => SaveFileJson.Parse(
                "{\"version\":999,\"sections\":{}}"));
        }

        [Test]
        public void Storage_creates_the_folder_and_replaces_the_old_save()
        {
            var path = Path.Combine(_folder, "deep", "save_test.json");
            var storage = new FileSaveStorage(path);

            storage.Write(Globals("flowers", 1));
            storage.Write(Globals("flowers", 2));

            var sections = SaveFileJson.Parse(File.ReadAllText(path));
            Assert.AreEqual(2L, sections["globals"]["flowers"]);
            Assert.IsFalse(File.Exists(path + ".tmp"));
        }

        [Test]
        public void A_bad_value_leaves_the_old_save_untouched()
        {
            var path = Path.Combine(_folder, "save_test.json");
            var storage = new FileSaveStorage(path);
            storage.Write(Globals("flowers", 1));

            Assert.Throws<ArgumentException>(
                () => storage.Write(Globals("mood", Mood.Happy)));

            var sections = SaveFileJson.Parse(File.ReadAllText(path));
            Assert.AreEqual(1L, sections["globals"]["flowers"]);
        }
    }
}
