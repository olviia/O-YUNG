using System;

namespace Oyung.SharedKernel.Unity
{
    /// <summary>
    /// How a number condition compares the fact's value with its target.
    /// Used by IntCondition and FloatCondition.
    /// </summary>
    public enum NumberComparison
    {
        Equal,
        NotEqual,
        Less,
        LessOrEqual,
        Greater,
        GreaterOrEqual
    }

    internal static class NumberComparisonExtensions
    {
        // CLAUDE: order = value.CompareTo(target): < 0, 0 or > 0. Each
        // CLAUDE: condition computes it its own way (float: approximate).
        public static bool Holds(this NumberComparison comparison, int order)
        {
            switch (comparison)
            {
                case NumberComparison.Equal: return order == 0;
                case NumberComparison.NotEqual: return order != 0;
                case NumberComparison.Less: return order < 0;
                case NumberComparison.LessOrEqual: return order <= 0;
                case NumberComparison.Greater: return order > 0;
                case NumberComparison.GreaterOrEqual: return order >= 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(comparison),
                        comparison, null);
            }
        }
    }
}
