using System.Collections.Generic;
using Oyung.SharedKernel;

namespace Oyung.Interaction
{
    // CLAUDE: something in the world the player can interact with (the
    // CLAUDE: parents' voices, a door, a flower). Knows: what it offers,
    // CLAUDE: in priority order. Implemented by the Unity side's
    // CLAUDE: component; the core never knows it is a GameObject.
    internal interface IInteractable
    {
        /// <summary>In priority order: the first available one is what
        /// the prompt offers ("Listen" before "Cry").</summary>
        IReadOnlyList<IInteraction> Interactions { get; }
    }

    // CLAUDE: one thing the player can do with an interactable. The
    // CLAUDE: verb the prompt shows lives on the Unity side (localized).
    internal interface IInteraction
    {
        /// <summary>Null: always available.</summary>
        Condition AvailableWhen { get; }

        IReadOnlyList<Instruction> OnUse { get; }
    }
}
