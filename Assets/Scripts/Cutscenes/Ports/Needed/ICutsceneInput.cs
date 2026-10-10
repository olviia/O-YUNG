using System;

namespace Oyung.Cutscenes
{
    /// <summary>
    /// port for the player's requests while a cutscene plays;
    /// implemented outside this module
    /// </summary>
    public interface ICutsceneInput
    {
        /// <summary>
        /// the player pressed skip
        /// </summary>
        event Action SkipPressed;

        /// <summary>
        /// the player pressed the speed toggle
        /// </summary>
        event Action SpeedTogglePressed;
    }
}
