using System;
using UnityEngine;

namespace Oyung.SharedKernel.Unity
{
    /// <summary>
    /// Met when a bool fact equals the expected value.
    /// </summary>
    [Serializable]
    public sealed class BoolCondition : FactCondition<bool>
    {
        [SerializeField] private bool expected = true;

        public override bool IsMet => Value == expected;
    }
}
