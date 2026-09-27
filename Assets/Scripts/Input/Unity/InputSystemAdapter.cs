using System;
using UnityEngine.InputSystem;
using Vector2 = System.Numerics.Vector2;

namespace Oyung.Input.Unity
{
    /// <summary>
    /// Implements IGameInput with Unity's Input System.
    /// The only class that knows the Input System exists.
    /// Enables the action map named like the active
    /// InputMode; Menu has no map yet, so all is off.
    /// </summary>
    public class InputSystemAdapter : IGameInput, IDisposable
    {
        private readonly OyungActions actions = new OyungActions();
        private readonly IInputModes modes;
        private readonly float mouseDegreesPerPixel;
        private readonly float stickDegreesPerSecond;

        public event Action SkipCutsceneRequested;

        /// <param name="modes">Decides which map is on.</param>
        /// <param name="mouseDegreesPerPixel">Mouse sensitivity.</param>
        /// <param name="stickDegreesPerSecond">Stick turn speed.</param>
        public InputSystemAdapter(IInputModes modes,
            float mouseDegreesPerPixel, float stickDegreesPerSecond)
        {
            this.modes = modes;
            this.mouseDegreesPerPixel = mouseDegreesPerPixel;
            this.stickDegreesPerSecond = stickDegreesPerSecond;

            actions.Cutscene.Skip.performed += OnSkipPerformed;
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
                    actions.Gameplay.LookPointer.ReadValue<UnityEngine.Vector2>()
                    * mouseDegreesPerPixel;
                UnityEngine.Vector2 stick =
                    actions.Gameplay.LookStick.ReadValue<UnityEngine.Vector2>()
                    * (stickDegreesPerSecond * UnityEngine.Time.deltaTime);
                UnityEngine.Vector2 total = pointer + stick;
                return new Vector2(total.x, total.y);
            }
        }

        public void Dispose()
        {
            modes.ModeChanged -= EnableMapFor;
            actions.Cutscene.Skip.performed -= OnSkipPerformed;
            actions.Disable();
            actions.Dispose();
        }

        private void EnableMapFor(InputMode mode)
        {
            actions.Gameplay.Disable();
            actions.Cutscene.Disable();

            switch (mode)
            {
                case InputMode.Gameplay: actions.Gameplay.Enable(); break;
                case InputMode.Cutscene: actions.Cutscene.Enable(); break;
            }
        }

        private void OnSkipPerformed(InputAction.CallbackContext _)
        {
            SkipCutsceneRequested?.Invoke();
        }
    }
}
