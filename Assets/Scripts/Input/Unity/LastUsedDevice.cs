using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace Oyung.Input.Unity
{
    /// <summary>
    /// Knows which device the player used last (keyboard, mouse, gamepad),
    /// so on-screen prompts can show that device's buttons. Shared and
    /// static like the Input System itself; mirrors device state only,
    /// never game state. Starts by itself, read-only from outside.
    /// </summary>
    public static class LastUsedDevice
    {
        private static InputDevice _current;
        private static IDisposable _buttonListener;

        /// <summary>The device used last; null if none is connected.
        /// </summary>
        public static InputDevice Current => _current;

        /// <summary>Raised when Current changes; read Current again.
        /// </summary>
        public static event Action Changed;

        // Statics survive play sessions when domain reload is off;
        // start clean every time.
        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Start()
        {
            _buttonListener?.Dispose();
            InputSystem.onDeviceChange -= OnDeviceChange;
            Changed = null;

            _current = Default();
            // Buttons only for now; stick and mouse moves don't switch.
            _buttonListener = InputSystem.onAnyButtonPress
                .Call(control => Set(control.device));
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        // Before any press: a connected gamepad wins, else the keyboard.
        private static InputDevice Default() =>
            (InputDevice)Gamepad.current ?? Keyboard.current;

        private static void OnDeviceChange(InputDevice device,
            InputDeviceChange change)
        {
            if (change == InputDeviceChange.Removed && device == _current)
                Set(Default());
        }

        private static void Set(InputDevice device)
        {
            if (device == _current) return;
            _current = device;
            Changed?.Invoke();
        }
    }
}
