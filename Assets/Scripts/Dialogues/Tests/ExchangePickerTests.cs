using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Oyung.Dialogues.Tests
{
    internal sealed class ExchangePickerTests
    {
        [Test]
        public void Never_picks_the_same_exchange_twice_in_a_row()
        {
            var random = new Random(1);
            var picker = new ExchangePicker(4, random.Next);
            var last = picker.Next();
            for (var i = 0; i < 1000; i++)
            {
                var next = picker.Next();
                Assert.AreNotEqual(last, next);
                last = next;
            }
        }

        [Test]
        public void Reaches_every_exchange()
        {
            var random = new Random(1);
            var picker = new ExchangePicker(4, random.Next);
            var seen = new HashSet<int>();
            for (var i = 0; i < 1000; i++) seen.Add(picker.Next());
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3 }, seen);
        }

        [Test]
        public void A_pool_of_one_repeats_it() =>
            Assert.AreEqual(0, new ExchangePicker(1, n =>
                throw new InvalidOperationException("Nothing to roll."))
                .Next());

        [Test]
        public void An_empty_pool_throws() =>
            Assert.Throws<ArgumentException>(
                () => new ExchangePicker(0, n => 0));
    }
}
