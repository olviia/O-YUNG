using System;
using Oyung.SharedKernel;
using UnityEngine;

namespace Oyung.Globals.Unity
{
    // CLAUDE: knows a global string fact; does Execute(value): sets it,
    // CLAUDE: throws if the fact is missing. Used by whoever learns a
    // CLAUDE: text while playing (the name minigame's done).
    [Serializable]
    public sealed class SetGlobalString : StringInstruction
    {
        [SerializeField] private GlobalStringFact _fact;

        public override void Execute(string value)
        {
            if (_fact == null)
                throw new InvalidOperationException(
                    $"{nameof(SetGlobalString)}: no fact assigned.");
            _fact.Set(value);
        }
    }
}
