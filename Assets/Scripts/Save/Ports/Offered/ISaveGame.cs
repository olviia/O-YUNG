using System;
using System.Collections.Generic;

namespace Oyung.Save
{
    /// <summary>
    /// Offered port: commands a save and announces how it went. Knows
    /// nothing about when a good moment is; callers decide (a key, a menu
    /// button, sleep).
    /// </summary>
    public interface ISaveGame
    {
        /// <summary>Writes the game now, or refuses if something holds a
        /// block. Always answers through exactly one of the events.
        /// </summary>
        void Save();

        /// <summary>The game was written.</summary>
        event Action Saved;

        /// <summary>Refused: these reasons hold a block.</summary>
        event Action<IReadOnlyList<string>> Blocked;

        /// <summary>Storage could not write; says why.</summary>
        event Action<string> WriteFailed;
    }
}
