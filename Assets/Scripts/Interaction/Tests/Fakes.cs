using System;
using System.Collections.Generic;
using System.Linq;
using Oyung.SharedKernel;

namespace Oyung.Interaction.Tests
{
    // CLAUDE: same as the Quests tests' fakes; each test assembly keeps
    // CLAUDE: its own copy so test projects don't depend on each other.

    // CLAUDE: a condition the test flips by hand; raises Changed like a
    // CLAUDE: real fact would. Counts listeners to check unsubscribing.
    internal sealed class FakeCondition : Condition
    {
        private bool _isMet;
        private Action _changed;

        internal FakeCondition(bool isMet = false) => _isMet = isMet;

        public override bool IsMet => _isMet;

        public override event Action Changed
        {
            add => _changed += value;
            remove => _changed -= value;
        }

        internal int ListenerCount =>
            _changed?.GetInvocationList().Length ?? 0;

        internal void Set(bool isMet)
        {
            if (_isMet == isMet) return;
            _isMet = isMet;
            _changed?.Invoke();
        }
    }

    internal sealed class FakeInstruction : Instruction
    {
        private readonly Action _action;

        internal FakeInstruction(Action action = null) => _action = action;

        internal int Count { get; private set; }

        public override void Execute()
        {
            Count++;
            _action?.Invoke();
        }
    }

    internal sealed class FakeInteraction : IInteraction
    {
        internal FakeInteraction(Condition availableWhen = null,
            params Instruction[] onUse)
        {
            AvailableWhen = availableWhen;
            OnUse = onUse;
        }

        public Condition AvailableWhen { get; }
        public IReadOnlyList<Instruction> OnUse { get; }
    }

    internal sealed class FakeInteractable : IInteractable
    {
        internal FakeInteractable(params IInteraction[] interactions) =>
            Interactions = interactions.ToList();

        public IReadOnlyList<IInteraction> Interactions { get; }
    }

    // CLAUDE: "not met" over a fake, like SharedKernel.Unity's Not
    // CLAUDE: (which the core tests can't reference).
    internal sealed class NotFake : Condition
    {
        private readonly Condition _inner;

        internal NotFake(Condition inner) => _inner = inner;

        public override bool IsMet => !_inner.IsMet;

        public override event Action Changed
        {
            add => _inner.Changed += value;
            remove => _inner.Changed -= value;
        }
    }
}
