using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Oyung.Interaction.Tests
{
    internal class InteractorTests
    {
        // CLAUDE: enter 8, leave 12, stickiness 1 in every test.
        private static Interactor NewInteractor() => new Interactor(8, 12, 1);

        private static FakeInteractable Always() =>
            new FakeInteractable(new FakeInteraction());

        private static List<Sighting> Seen(
            params (IInteractable target, float angle)[] seen)
        {
            var list = new List<Sighting>();
            foreach (var (target, angle) in seen)
                list.Add(new Sighting(target, angle));
            return list;
        }

        [Test]
        public void Nothing_seen_means_no_target()
        {
            var interactor = NewInteractor();
            interactor.See(Seen());
            Assert.IsNull(interactor.Target);
            Assert.IsNull(interactor.Offer);
        }

        [Test]
        public void Acquires_only_within_enter_angle()
        {
            var interactor = NewInteractor();
            var cube = Always();

            interactor.See(Seen((cube, 10)));
            Assert.IsNull(interactor.Target);

            interactor.See(Seen((cube, 7)));
            Assert.AreSame(cube, interactor.Target);
        }

        [Test]
        public void Keeps_current_target_until_leave_angle()
        {
            var interactor = NewInteractor();
            var cube = Always();
            interactor.See(Seen((cube, 7)));

            interactor.See(Seen((cube, 11)));
            Assert.AreSame(cube, interactor.Target, "between 8 and 12");

            interactor.See(Seen((cube, 13)));
            Assert.IsNull(interactor.Target);

            interactor.See(Seen((cube, 11)));
            Assert.IsNull(interactor.Target, "lost: must re-enter at 8");
        }

        [Test]
        public void Closest_to_centre_wins()
        {
            var interactor = NewInteractor();
            var far = Always();
            var near = Always();
            interactor.See(Seen((far, 6), (near, 2)));
            Assert.AreSame(near, interactor.Target);
        }

        [Test]
        public void Near_tie_keeps_current_clear_win_switches()
        {
            var interactor = NewInteractor();
            var first = Always();
            var second = Always();
            interactor.See(Seen((first, 3)));

            interactor.See(Seen((first, 3), (second, 2.5f)));
            Assert.AreSame(first, interactor.Target, "within stickiness");

            interactor.See(Seen((first, 3), (second, 1.5f)));
            Assert.AreSame(second, interactor.Target, "clearly closer");
        }

        [Test]
        public void Target_offering_nothing_is_skipped()
        {
            var interactor = NewInteractor();
            var closed = new FakeInteractable(
                new FakeInteraction(new FakeCondition(false)));
            var behind = Always();

            interactor.See(Seen((closed, 1), (behind, 5)));
            Assert.AreSame(behind, interactor.Target);
        }

        [Test]
        public void Offer_is_first_available_in_order()
        {
            var interactor = NewInteractor();
            var notYet = new FakeCondition(false);
            var listen = new FakeInteraction(notYet);
            var cry = new FakeInteraction();
            var parents = new FakeInteractable(listen, cry);

            interactor.See(Seen((parents, 1)));
            Assert.AreSame(cry, interactor.Offer);

            notYet.Set(true);
            interactor.See(Seen((parents, 1)));
            Assert.AreSame(listen, interactor.Offer, "polled next frame");
        }

        [Test]
        public void Polling_never_subscribes()
        {
            var interactor = NewInteractor();
            var condition = new FakeCondition(true);
            var target = new FakeInteractable(new FakeInteraction(condition));

            interactor.See(Seen((target, 1)));
            interactor.Use();

            Assert.AreEqual(0, condition.ListenerCount);
        }

        [Test]
        public void Use_runs_the_offer_once()
        {
            var interactor = NewInteractor();
            var instruction = new FakeInstruction();
            var target = new FakeInteractable(
                new FakeInteraction(null, instruction));
            interactor.See(Seen((target, 1)));

            interactor.Use();
            Assert.AreEqual(1, instruction.Count);
        }

        [Test]
        public void Use_without_target_does_nothing()
        {
            var interactor = NewInteractor();
            Assert.DoesNotThrow(interactor.Use);
        }

        [Test]
        public void Use_skips_an_offer_that_stopped_being_available()
        {
            var interactor = NewInteractor();
            var available = new FakeCondition(true);
            var instruction = new FakeInstruction();
            var target = new FakeInteractable(
                new FakeInteraction(available, instruction));
            interactor.See(Seen((target, 1)));

            available.Set(false);
            interactor.Use();

            Assert.AreEqual(0, instruction.Count);
            Assert.IsNull(interactor.Target);
        }

        [Test]
        public void After_use_the_next_offer_shows_at_once()
        {
            // CLAUDE: the parents: Listen sets "heard", which ends Listen
            // CLAUDE: and opens Cry, without waiting for the next frame.
            var interactor = NewInteractor();
            var heard = new FakeCondition(false);
            var listen = new FakeInteraction(new NotFake(heard),
                new FakeInstruction(() => heard.Set(true)));
            var cry = new FakeInteraction(heard);
            var parents = new FakeInteractable(listen, cry);
            interactor.See(Seen((parents, 1)));
            Assert.AreSame(listen, interactor.Offer);

            interactor.Use();

            Assert.AreSame(parents, interactor.Target);
            Assert.AreSame(cry, interactor.Offer);
        }

        [Test]
        public void After_last_use_the_target_is_released()
        {
            var interactor = NewInteractor();
            var used = new FakeCondition(false);
            var once = new FakeInteractable(new FakeInteraction(
                new NotFake(used), new FakeInstruction(() => used.Set(true))));
            var changes = 0;
            interactor.TargetChanged += () => changes++;
            interactor.See(Seen((once, 1)));

            interactor.Use();

            Assert.IsNull(interactor.Target);
            Assert.IsNull(interactor.Offer);
            Assert.AreEqual(2, changes, "acquired, then released");
        }

        [Test]
        public void TargetChanged_only_on_real_change()
        {
            var interactor = NewInteractor();
            var cube = Always();
            var changes = 0;
            interactor.TargetChanged += () => changes++;

            interactor.See(Seen((cube, 1)));
            interactor.See(Seen((cube, 2)));
            interactor.See(Seen((cube, 3)));

            Assert.AreEqual(1, changes);
        }

        [Test]
        public void Bad_angles_throw()
        {
            Assert.Throws<ArgumentException>(() => new Interactor(0, 12, 1));
            Assert.Throws<ArgumentException>(() => new Interactor(12, 8, 1));
            Assert.Throws<ArgumentException>(() => new Interactor(8, 12, -1));
        }
    }
}
