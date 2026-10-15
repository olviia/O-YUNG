using System;
using Oyung.SharedKernel;
using UnityEngine;

namespace Oyung.Globals.Unity
{
    // CLAUDE: knows a GlobalFact<bool> and the value to set; does
    // CLAUDE: Execute(): sets it, throws if the fact is missing. Same
    // CLAUDE: shape as AddToGlobalInt. Used by deciders (Quest,
    // CLAUDE: Interactable) through [SerializeReference] fields.
    [Serializable]
    public sealed class SetGlobalBool : Instruction
    {
        [SerializeField] private GlobalBoolFact _fact;
        [SerializeField] private bool _value = true;

        public override void Execute()
        {
            if (_fact == null)
                throw new InvalidOperationException(
                    $"{nameof(SetGlobalBool)}: no fact assigned.");
            _fact.Set(_value);
        }
    }
}
