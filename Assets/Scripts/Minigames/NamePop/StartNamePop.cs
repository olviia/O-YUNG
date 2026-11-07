using System;
using Oyung.SharedKernel;
using UnityEngine;

namespace Oyung.Minigames.NamePop
{
    // CLAUDE: starts the minigame from anything that runs instructions in
    // CLAUDE: the scene (the parents' Listen). Holds a scene reference, so
    // CLAUDE: it can't live on an asset yet; nothing needs that so far.
    [Serializable]
    public sealed class StartNamePop : Instruction
    {
        [SerializeField] private NamePopGame _game;

        public override void Execute()
        {
            if (_game == null)
                throw new InvalidOperationException(
                    $"{nameof(StartNamePop)}: no game assigned.");
            _game.Play();
        }
    }
}
