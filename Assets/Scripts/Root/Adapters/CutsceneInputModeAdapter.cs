using System;
using Oyung.Cutscenes;
using Oyung.Input;

namespace Oyung.Root.Adapters
{
    // CLAUDE: class comment (your words). Draft: puts Input into
    // CLAUDE: Cutscene mode while a cutscene plays. Listens to
    // CLAUDE: ICutsceneEvents, tells IInputModes. No decisions.
    internal sealed class CutsceneInputModeAdapter : IDisposable
    {
        private readonly ICutsceneEvents _cutscenes;
        private readonly IInputModes _modes;

        internal CutsceneInputModeAdapter(ICutsceneEvents cutscenes, IInputModes modes)
        {
            _cutscenes = cutscenes;
            _modes = modes;
            _cutscenes.Started += OnStarted;
            _cutscenes.Ended += OnEnded;
        }

        public void Dispose()
        {
            _cutscenes.Started -= OnStarted;
            _cutscenes.Ended -= OnEnded;
        }

        private void OnStarted() => _modes.EnterMode(InputMode.Cutscene);

        private void OnEnded() => _modes.ExitMode(InputMode.Cutscene);
    }
}
