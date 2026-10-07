using System;
using UnityEngine;

namespace Oyung.SharedKernel.Unity
{
    /// <summary>
    /// Composite condition: met when its one child is NOT met.
    /// Knows only its child (any Condition, from any module).
    /// </summary>
    [Serializable]
    public sealed class Not : Condition
    {
        [SerializeReference] private Condition child;

        public override bool IsMet => !Child.IsMet;

        /// <summary>Passes subscriptions straight to the child.</summary>
        public override event Action Changed
        {
            add => Child.Changed += value;
            remove => Child.Changed -= value;
        }

        // CLAUDE: an empty slot is a designer mistake: fail fast.
        private Condition Child => child ??
            throw new InvalidOperationException("Not has no condition.");
    }
}
