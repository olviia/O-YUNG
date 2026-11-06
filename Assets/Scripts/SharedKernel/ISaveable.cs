using System.Collections.Generic;

namespace Oyung.SharedKernel
{
    /// <summary>
    /// Shared Kernel contract: a module that keeps state across saves. A
    /// section is a dictionary of plain values: bool, int, long, float,
    /// double, string, a nested section, or a list of those. Anything else
    /// (an enum: write its name) throws on save. Numbers come back wider
    /// than they went in: whole numbers as long, decimals as double.
    /// </summary>
    public interface ISaveable
    {
        /// <summary>The section's stable name in the save.</summary>
        string Section { get; }

        /// <summary>The module's current state.</summary>
        IReadOnlyDictionary<string, object> Give();

        /// <summary>Puts back what Give returned, silently: runs in the
        /// load phase, before anything listens. Not called if the save has
        /// no such section; the module then simply starts fresh.</summary>
        void Take(IReadOnlyDictionary<string, object> section);
    }
}
