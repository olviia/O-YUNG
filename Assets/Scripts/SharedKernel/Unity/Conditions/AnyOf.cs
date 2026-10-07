using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oyung.SharedKernel.Unity
{
    /// <summary>
    /// Composite condition: met when at least one child is met.
    /// Knows only its children (any Condition, from any module). Lets
    /// designers combine conditions inline instead of every decider
    /// writing its own AND / OR.
    /// </summary>
    [Serializable]
    public sealed class AnyOf : Condition
    {
        [SerializeReference] private List<Condition> children =
            new List<Condition>();

        public override bool IsMet
        {
            get
            {
                ThrowIfIncomplete();
                foreach (var child in children)
                    if (child.IsMet) return true;
                return false;
            }
        }

        /// <summary>Subscribes to / unsubscribes from every child; keeps
        /// no listeners itself.</summary>
        public override event Action Changed
        {
            add
            {
                ThrowIfIncomplete();
                foreach (var child in children) child.Changed += value;
            }
            remove
            {
                foreach (var child in children) child.Changed -= value;
            }
        }

        // CLAUDE: an empty group would silently pass (AND) or fail (OR);
        // CLAUDE: a designer mistake, so fail fast instead.
        private void ThrowIfIncomplete()
        {
            if (children == null || children.Count == 0)
                throw new InvalidOperationException(
                    "AnyOf has no conditions.");
            if (children.Contains(null))
                throw new InvalidOperationException(
                    "AnyOf has an empty condition slot.");
        }
    }
}
