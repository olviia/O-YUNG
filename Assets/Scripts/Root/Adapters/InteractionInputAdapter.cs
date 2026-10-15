using System;
using Oyung.Input;
using Oyung.Interaction;

namespace Oyung.Root.Adapters
{
    // CLAUDE: translates Input's interact press into Interaction's needed
    // CLAUDE: IInteractionInput. Interacting is possible in Gameplay mode
    // CLAUDE: only (not in a cutscene or menu). Pure forwarding.
    internal sealed class InteractionInputAdapter : IInteractionInput
    {
        private readonly IGameInput _gameInput;
        private readonly IInputModes _modes;

        internal InteractionInputAdapter(IGameInput gameInput,
            IInputModes modes)
        {
            _gameInput = gameInput;
            _modes = modes;
        }

        public event Action InteractPressed
        {
            add => _gameInput.InteractPressed += value;
            remove => _gameInput.InteractPressed -= value;
        }

        public bool CanInteract => _modes.ActiveMode == InputMode.Gameplay;
    }
}
