using System;
using UnityEngine.InputSystem;
using Vector2 = System.Numerics.Vector2;

namespace Oyung.Input.Unity
{
    /// <summary>
    /// Implements IGameInput with Unity's Input System.
    /// The only game-logic class that knows the Input System
    /// exists; HUD prompts read actions directly for glyphs.
    /// Enables the action map named like the active
    /// InputMode; Menu has no map yet, so all is off.
    /// </summary>
    internal class InputSystemAdapter : IGameInput, IDisposable
    {
        // The project asset itself, not a copy: HUD prompts that pick
        // actions from it see the same enabled maps and holds.
        private readonly InputActionAsset asset;
        private readonly InputAction lookPointer;
        private readonly InputAction lookStick;
        private readonly InputAction skip;
        private readonly InputAction speed;
        private readonly IInputModes modes;
        private readonly float mouseDegreesPerPixel;
        private readonly float stickDegreesPerSecond;

        public event Action SkipCutscenePressed;
        public event Action ToggleCutsceneSpeedPressed;

        /// <param name="modes">Decides which map is on.</param>
        /// <param name="asset">OyungActions.inputactions itself.</param>
        /// <param name="mouseDegreesPerPixel">Mouse sensitivity.</param>
        /// <param name="stickDegreesPerSecond">Stick turn speed.</param>
        public InputSystemAdapter(IInputModes modes, InputActionAsset asset,
            float mouseDegreesPerPixel, float stickDegreesPerSecond)
        {
            this.modes = modes;
            this.asset = asset;
            this.mouseDegreesPerPixel = mouseDegreesPerPixel;
            this.stickDegreesPerSecond = stickDegreesPerSecond;

            // Every action name lives here; a rename fails at startup.
            lookPointer = Find("Gameplay/LookPointer");
            lookStick = Find("Gameplay/LookStick");
            skip = Find("Cutscene/Skip");
            speed = Find("Cutscene/Speed");

            skip.performed += OnSkipPerformed;
            speed.performed += OnSpeedPerformed;
            modes.ModeChanged += EnableMapFor;
            EnableMapFor(modes.ActiveMode);
        }

        /// <summary>
        /// Degrees this frame: X = left/right, Y = up/down.
        /// Mouse gives pixels, stick gives a -1..1 position;
        /// both become degrees here. Zero when the
        /// Gameplay map is off (cutscene, menu).
        /// </summary>
        public Vector2 LookDelta
        {
            get
            {
                UnityEngine.Vector2 pointer =
                    lookPointer.ReadValue<UnityEngine.Vector2>()
                    * mouseDegreesPerPixel;
                UnityEngine.Vector2 stick =
                    lookStick.ReadValue<UnityEngine.Vector2>()
                    * (stickDegreesPerSecond * UnityEngine.Time.deltaTime);
                UnityEngine.Vector2 total = pointer + stick;
                return new Vector2(total.x, total.y);
            }
        }

        public void Dispose()
        {
            modes.ModeChanged -= EnableMapFor;
            skip.performed -= OnSkipPerformed;
            speed.performed -= OnSpeedPerformed;
            // Only disable: the asset belongs to the project, not to us.
            asset.Disable();
        }

        private InputAction Find(string path) =>
            asset.FindAction(path, throwIfNotFound: true);

        private void EnableMapFor(InputMode mode)
        {
            asset.Disable();
            // Map named like the mode; Menu has none yet, so all stays off.
            asset.FindActionMap(mode.ToString())?.Enable();
        }

        private void OnSkipPerformed(InputAction.CallbackContext _)
        {
            SkipCutscenePressed?.Invoke();
        }

        private void OnSpeedPerformed(InputAction.CallbackContext _)
        {
            ToggleCutsceneSpeedPressed?.Invoke();
        }
    }
}
