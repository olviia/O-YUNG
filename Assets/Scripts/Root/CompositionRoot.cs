using Oyung.Eyes;
using Oyung.Input.Unity;
using Oyung.Vision.Unity;
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
        [Header("Module installers")]
        [SerializeField] private InputInstaller inputInstaller;
        [SerializeField] private EyesInstaller eyesInstaller;
        [SerializeField] private VisionInstaller visionInstaller;

        private void Awake()
        {
            // 1. Create, in dependency order.
            var (gameInput, inputModes) = inputInstaller.Build();
            eyesInstaller.Build(gameInput);
            var vision = visionInstaller.Build();

            // 2. Load: nothing saved yet.

            // 3. Start: nothing yet.

            // CLAUDE: temporary, so the mouse stays in
            // CLAUDE: the window. Belongs in a future
            // CLAUDE: "application/cursor" module.
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void OnDestroy()
        {
            // Reverse order of creation.
            visionInstaller.Dispose();
            eyesInstaller.Dispose();
            inputInstaller.Dispose();
        }
    }
}
