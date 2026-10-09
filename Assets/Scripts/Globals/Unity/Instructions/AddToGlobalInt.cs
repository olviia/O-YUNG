using System;
using Oyung.SharedKernel;
using UnityEngine;

namespace Oyung.Globals.Unity
{
    // CLAUDE: class comment: knows / does / used by (your words, CRC):
    // CLAUDE: knows a GlobalFact<int> and an amount; does Execute():
    // CLAUDE: adds the amount, throws if the fact is missing; used by
    // CLAUDE: deciders (Quest, test) through [SerializeReference]
    // CLAUDE: Instruction fields.
    [Serializable]
    public sealed class AddToGlobalInt : Instruction
    {
        // CLAUDE: field comments (your words)
        [SerializeField] private GlobalIntFact _fact;
        [SerializeField] private int _amount;

        public override void Execute()
        {
            if (_fact == null)
                throw new InvalidOperationException(
                    $"{nameof(AddToGlobalInt)}: no fact assigned.");
            _fact.Add(_amount);
        }
    }
}
