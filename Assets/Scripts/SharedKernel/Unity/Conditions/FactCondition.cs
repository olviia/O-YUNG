using System;
using UnityEngine;

namespace Oyung.SharedKernel.Unity
{
    /// <summary>
    /// Base of conditions that read one fact (Bool/Int/Float/String).
    /// Knows: the fact dragged in the Inspector. Does: passes Changed
    /// straight to the fact, gives subclasses its value. Subclasses
    /// only decide how to compare (Template Method).
    /// </summary>
    [Serializable]
    public abstract class FactCondition<T> : Condition
    {
        [SerializeField] private ModuleFact<T> fact;

        public override event Action Changed
        {
            add => Fact.Changed += value;
            remove => Fact.Changed -= value;
        }

        /// <summary>The fact's current value, for comparing.</summary>
        protected T Value => Fact.Value;

        // CLAUDE: an empty slot is a designer mistake: fail fast.
        private ModuleFact<T> Fact => fact != null
            ? fact
            : throw new InvalidOperationException(
                GetType().Name + " has no fact.");
    }
}
