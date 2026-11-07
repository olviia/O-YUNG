using System;
using Oyung.SharedKernel;
using UnityEngine;

namespace Oyung.Minigames.NamePop
{
    // CLAUDE: what pressing Interact on a bubble does. Held by the bubble
    // CLAUDE: prefab's own Interactable, pointing at the same prefab.
    [Serializable]
    internal sealed class PopBubble : Instruction
    {
        [SerializeField] private LetterBubble _bubble;

        public override void Execute()
        {
            if (_bubble == null)
                throw new InvalidOperationException(
                    $"{nameof(PopBubble)}: no bubble assigned.");
            _bubble.Pop();
        }
    }
}
