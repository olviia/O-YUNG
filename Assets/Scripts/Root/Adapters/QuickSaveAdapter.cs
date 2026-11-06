using System;
using Oyung.Input;
using Oyung.Save;

namespace Oyung.Root.Adapters
{
    // CLAUDE: class comment (your words). Draft: the Quick Save press
    // CLAUDE: commands a save. Listens to IGameInput, tells ISaveGame. No
    // CLAUDE: decisions: whether saving is possible is Save's to answer.
    internal sealed class QuickSaveAdapter : IDisposable
    {
        private readonly IGameInput _gameInput;
        private readonly ISaveGame _saveGame;

        internal QuickSaveAdapter(IGameInput gameInput, ISaveGame saveGame)
        {
            _gameInput = gameInput;
            _saveGame = saveGame;
            _gameInput.QuickSavePressed += _saveGame.Save;
        }

        public void Dispose() => _gameInput.QuickSavePressed -= _saveGame.Save;
    }
}
