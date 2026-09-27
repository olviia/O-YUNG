using Oyung.Eyes;
using Oyung.Input;
using Oyung.Input.Unity;
using UnityEngine;

namespace Oyung.Root
{
    /// <summary>
    /// The only place that creates modules and
    /// hands them what they need.
    /// Startup: create → load → start.
    /// Shutdown: reverse order.
    /// </summary>
    public class CompositionRoot : MonoBehaviour
    {
        [Header("Scene objects")]
        [SerializeField] private LookCamera lookCamera;

        [Header("Input settings")]
        [SerializeField] private float mouseDegreesPerPixel = 0.1f;
        [SerializeField] private float stickDegreesPerSecond = 120f;

        private InputSystemAdapter input;

        private void Awake()
        {
            // 1. Create, in dependency order.
            var inputModes = new InputModeStack();
            input = new InputSystemAdapter(inputModes, mouseDegreesPerPixel, stickDegreesPerSecond);

            // 2. Load: nothing saved yet.

            // 3. Start: hand scene objects their ports.
            lookCamera.Init(input);

            // CLAUDE: temporary, so the mouse stays in
            // CLAUDE: the window. Belongs in a future
            // CLAUDE: "application/cursor" module.
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void OnDestroy()
        {
            input?.Dispose();
        }
    }
}
