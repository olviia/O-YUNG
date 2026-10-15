using System;

namespace Oyung.Interaction
{
    /// <summary>
    /// port for the player's interact press; implemented outside this
    /// module
    /// </summary>
    public interface IInteractionInput
    {
        /// <summary>
        /// the player pressed Interact
        /// </summary>
        event Action InteractPressed;

        /// <summary>
        /// false while the player can't interact (cutscene, menu): no
        /// target, no prompt
        /// </summary>
        bool CanInteract { get; }
    }
}
