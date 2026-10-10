using System;
using Oyung.Cutscenes;
using Oyung.Input;

namespace Oyung.Root.Adapters
{
    // CLAUDE: class comment (your words). Draft: translates Input's
    // CLAUDE: cutscene presses into Cutscenes' needed ICutsceneInput.
    // CLAUDE: Pure forwarding: subscribing here subscribes to Input.
    internal sealed class CutsceneInputAdapter : ICutsceneInput
    {
        private readonly IGameInput _gameInput;

        internal CutsceneInputAdapter(IGameInput gameInput) => _gameInput = gameInput;

        public event Action SkipPressed
        {
            add => _gameInput.SkipCutscenePressed += value;
            remove => _gameInput.SkipCutscenePressed -= value;
        }

        public event Action SpeedTogglePressed
        {
            add => _gameInput.ToggleCutsceneSpeedPressed += value;
            remove => _gameInput.ToggleCutsceneSpeedPressed -= value;
        }
    }
}
