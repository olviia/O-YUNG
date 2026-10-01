using System;
using UnityEngine;

namespace Oyung.Input.Unity
{
    /// <summary>
    /// Gets Monobehavior references for Input module and hands ports to root
    /// </summary>
    public class InputInstaller: MonoBehaviour, IDisposable
    {
        [SerializeField] private float mouseDegreesPerPixel = 0.1f;
        [SerializeField] private float stickDegreesPerSecond = 120f;

        private InputSystemAdapter adapter;   // kept only so Dispose can clean it up

        /// Needs nothing from other modules → no parameters.
        /// Offers two ports → returns both (a tuple: two values in one return).
        public (IGameInput gameInput, IInputModes modes) Build()
        {
            var modes = new InputModeStack();
            adapter = new InputSystemAdapter(modes, mouseDegreesPerPixel, stickDegreesPerSecond);
            return (adapter, modes);
        }

        /// The shared part every installer has: cleanup.
        public void Dispose()
        {
            adapter?.Dispose();
        }
    }
}