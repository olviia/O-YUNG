using System;
using System.Collections.Generic;

namespace Oyung.Input
{
    /// <summary>
    /// Implements IInputModes as a stack-like list:
    /// the last entered mode is active, but any
    /// mode can be exited, e.g. a cutscene ending
    /// while the menu is open.
    /// </summary>
    internal class InputModeStack : IInputModes
    {
        private readonly List<InputMode> entered = new List<InputMode>();

        public InputMode ActiveMode =>
            entered.Count > 0 ? entered[entered.Count - 1] : InputMode.Gameplay;

        public event Action<InputMode> ModeChanged;

        public void EnterMode(InputMode mode)
        {
            InputMode before = ActiveMode;
            entered.Add(mode);
            NotifyIfChanged(before);
        }

        public void ExitMode(InputMode mode)
        {
            int index = entered.LastIndexOf(mode);
            if (index < 0)
                throw new InvalidOperationException(
                    $"Exited input mode {mode} that was never entered.");

            InputMode before = ActiveMode;
            entered.RemoveAt(index);
            NotifyIfChanged(before);
        }

        // Exiting a mode below the top changes nothing
        // the player feels, so no event then.
        private void NotifyIfChanged(InputMode before)
        {
            if (ActiveMode != before)
                ModeChanged?.Invoke(ActiveMode);
        }
    }
}
