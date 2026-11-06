using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Oyung.SharedKernel;

namespace Oyung.Save.Tests
{
    using Section = IReadOnlyDictionary<string, object>;

    public class SaveSystemTests
    {
        private sealed class FakeStorage : ISaveStorage
        {
            public IReadOnlyDictionary<string, Section> Written;
            public int Writes;
            public Exception FailWith;

            public void Write(IReadOnlyDictionary<string, Section> sections)
            {
                if (FailWith != null) throw FailWith;
                Written = sections;
                Writes++;
            }
        }

        private sealed class FakeSaveable : ISaveable
        {
            public Section State;
            public int Takes;

            public FakeSaveable(string section, Section state = null)
            {
                Section = section;
                State = state ?? new Dictionary<string, object>();
            }

            public string Section { get; }
            public Section Give() => State;

            public void Take(Section section)
            {
                State = section;
                Takes++;
            }
        }

        private static Section One(string key, object value) =>
            new Dictionary<string, object> { [key] = value };

        private FakeStorage _storage;
        private SaveSystem _save;
        private int _saved;
        private IReadOnlyList<string> _blockedBy;
        private string _failure;

        [SetUp]
        public void SetUp()
        {
            _storage = new FakeStorage();
            _save = new SaveSystem(_storage);
            _saved = 0;
            _blockedBy = null;
            _failure = null;
            _save.Saved += () => _saved++;
            _save.Blocked += reasons => _blockedBy = reasons;
            _save.WriteFailed += why => _failure = why;
        }

        [Test]
        public void Save_writes_every_saveables_section()
        {
            _save.Add(new FakeSaveable("globals", One("a", 1)));
            _save.Add(new FakeSaveable("quests", One("b", 2)));

            _save.Save();

            Assert.AreEqual(1, _storage.Written["globals"]["a"]);
            Assert.AreEqual(2, _storage.Written["quests"]["b"]);
            Assert.AreEqual(1, _saved);
        }

        [Test]
        public void Block_refuses_and_names_every_reason()
        {
            _save.Block("cutscene");
            _save.Block("minigame");

            _save.Save();

            Assert.AreEqual(0, _storage.Writes);
            Assert.AreEqual(0, _saved);
            CollectionAssert.AreEqual(new[] { "cutscene", "minigame" },
                _blockedBy);
        }

        [Test]
        public void One_unblock_does_not_cancel_another_reason()
        {
            _save.Block("cutscene");
            _save.Block("minigame");
            _save.Unblock("cutscene");

            _save.Save();

            Assert.AreEqual(0, _storage.Writes);
            CollectionAssert.AreEqual(new[] { "minigame" }, _blockedBy);
        }

        [Test]
        public void Same_reason_held_twice_needs_two_unblocks()
        {
            _save.Block("cutscene");
            _save.Block("cutscene");
            _save.Unblock("cutscene");
            _save.Save();
            Assert.AreEqual(0, _storage.Writes);

            _save.Unblock("cutscene");
            _save.Save();
            Assert.AreEqual(1, _storage.Writes);
        }

        [Test]
        public void Unblock_not_held_throws()
        {
            Assert.Throws<InvalidOperationException>(
                () => _save.Unblock("cutscene"));
        }

        [Test]
        public void Block_without_a_reason_throws()
        {
            Assert.Throws<ArgumentException>(() => _save.Block(""));
            Assert.Throws<ArgumentException>(() => _save.Block(null));
        }

        [Test]
        public void Disk_failure_is_announced_not_thrown()
        {
            _storage.FailWith = new IOException("disk full");

            _save.Save();

            Assert.AreEqual("disk full", _failure);
            Assert.AreEqual(0, _saved);
        }

        [Test]
        public void No_permission_is_announced_not_thrown()
        {
            _storage.FailWith = new UnauthorizedAccessException("read only");

            _save.Save();

            Assert.AreEqual("read only", _failure);
        }

        [Test]
        public void A_bug_stays_loud_instead_of_looking_like_a_disk_problem()
        {
            _storage.FailWith = new ArgumentException("bad value");

            Assert.Throws<ArgumentException>(() => _save.Save());
            Assert.IsNull(_failure);
        }

        [Test]
        public void Load_gives_each_saveable_its_section()
        {
            var globals = new FakeSaveable("globals");
            _save.Add(globals);

            _save.Load(new Dictionary<string, Section>
            {
                ["globals"] = One("a", 1)
            });

            Assert.AreEqual(1, globals.State["a"]);
        }

        [Test]
        public void Saveable_missing_from_the_save_starts_fresh()
        {
            var fresh = One("a", 1);
            var added = new FakeSaveable("added_in_an_update", fresh);
            _save.Add(added);

            _save.Load(new Dictionary<string, Section>());

            Assert.AreEqual(0, added.Takes);
            Assert.AreSame(fresh, added.State);
        }

        [Test]
        public void Load_reports_sections_nobody_claimed()
        {
            _save.Add(new FakeSaveable("globals"));

            var unclaimed = _save.Load(new Dictionary<string, Section>
            {
                ["globals"] = One("a", 1),
                ["removed_module"] = One("b", 2)
            });

            CollectionAssert.AreEqual(new[] { "removed_module" }, unclaimed);
        }

        [Test]
        public void Same_section_twice_throws()
        {
            _save.Add(new FakeSaveable("globals"));

            Assert.Throws<InvalidOperationException>(
                () => _save.Add(new FakeSaveable("globals")));
        }
    }
}
