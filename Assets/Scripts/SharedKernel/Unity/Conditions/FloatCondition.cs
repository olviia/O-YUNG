using System;
using UnityEngine;

namespace Oyung.SharedKernel.Unity
{
    /// <summary>
    /// Met when a float fact compares to the target as chosen.
    /// </summary>
    [Serializable]
    public sealed class FloatCondition : FactCondition<float>
    {
        [SerializeField] private NumberComparison comparison;
        [SerializeField] private float target;

        // CLAUDE: floats are rarely exactly equal (0.1 + 0.2), so
        // CLAUDE: "nearly equal" counts as equal.
        public override bool IsMet
        {
            get
            {
                var value = Value;
                var order = Mathf.Approximately(value, target)
                    ? 0
                    : value.CompareTo(target);
                return comparison.Holds(order);
            }
        }
    }
}
