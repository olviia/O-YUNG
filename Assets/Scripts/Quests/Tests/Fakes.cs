using System;
using Oyung.SharedKernel;

namespace Oyung.Quests.Tests
{
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
}
