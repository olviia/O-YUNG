using System;

namespace Oyung.Input
{
    /// <summary>
    /// port for selecting and changing the input modes
    /// </summary>
    public interface IInputModes
    {
        /// <summary>
        /// what is the current mode of the input, Gameplay by default
        /// </summary>
        InputMode ActiveMode { get; }
        
        /// <summary>
        /// Enters the input mode
        /// </summary>
        /// <param name="mode"> what mode </param>
        void EnterMode(InputMode mode);
        
        /// <summary>
        /// Exits the input mode
        /// </summary>
        /// <param name="mode">what mode</param>
        void ExitMode(InputMode mode);
        
        /// <summary>
        /// declares when a mode is changed and gives the InputMode to which
        /// </summary>
        event Action<InputMode> ModeChanged;
    }
}
