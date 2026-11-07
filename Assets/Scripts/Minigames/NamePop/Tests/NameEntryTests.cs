using NUnit.Framework;

namespace Oyung.Minigames.NamePop.Tests
{
    internal sealed class NameEntryTests
    {
        [Test]
        public void Letters_add_up_in_order()
        {
            var entry = new NameEntry(12);
            entry.Add('B');
            entry.Add('O');
            entry.Add('B');
            Assert.AreEqual("BOB", entry.Text);
        }

        [Test]
        public void A_full_name_does_not_take_a_letter()
        {
            var entry = new NameEntry(2);
            Assert.IsTrue(entry.Add('A'));
            Assert.IsTrue(entry.Add('B'));
            Assert.IsFalse(entry.Add('C'));
            Assert.AreEqual("AB", entry.Text);
        }

        [Test]
        public void Remove_takes_the_last_letter_back()
        {
            var entry = new NameEntry(12);
            entry.Add('A');
            entry.Add('B');
            Assert.IsTrue(entry.RemoveLast());
            Assert.AreEqual("A", entry.Text);
        }

        [Test]
        public void Remove_on_an_empty_name_does_nothing()
        {
            var entry = new NameEntry(12);
            Assert.IsFalse(entry.RemoveLast());
            Assert.AreEqual("", entry.Text);
        }

        [Test]
        public void An_empty_name_cannot_finish_any_other_can()
        {
            var entry = new NameEntry(12);
            Assert.IsFalse(entry.CanFinish);
            entry.Add('Z');
            Assert.IsTrue(entry.CanFinish);
            entry.RemoveLast();
            Assert.IsFalse(entry.CanFinish);
        }
    }
}
