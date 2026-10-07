using System;
using UnityEngine;

namespace Oyung.SharedKernel.Unity
{
    /// <summary>
    /// Met when a string fact equals the target exactly (case matters).
    /// For "not equal", wrap it in Not.
    /// </summary>
    [Serializable]
    public sealed class StringCondition : FactCondition<string>
    {
        [SerializeField] private string target;

        public override bool IsMet =>
            string.Equals(Value, target, StringComparison.Ordinal);
    }
}
