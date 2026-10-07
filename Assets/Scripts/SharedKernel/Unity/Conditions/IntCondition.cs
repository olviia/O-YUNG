using System;
using UnityEngine;

namespace Oyung.SharedKernel.Unity
{
    /// <summary>
    /// Met when an int fact compares to the target as chosen
    /// ("flowers >= 3").
    /// </summary>
    [Serializable]
    public sealed class IntCondition : FactCondition<int>
    {
        [SerializeField] private NumberComparison comparison;
        [SerializeField] private int target;

        public override bool IsMet =>
            comparison.Holds(Value.CompareTo(target));
    }
}
