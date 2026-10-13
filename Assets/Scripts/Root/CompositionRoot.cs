using Oyung.Cutscenes.Unity;
using Oyung.Eyes;
using Oyung.Globals.Unity;
using Oyung.Input.Unity;
using Oyung.Quests.Unity;
using Oyung.Root.Adapters;
using Oyung.Scratch;
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
        [SerializeField] private GlobalsInstaller globalsInstaller;
        [SerializeField] private InputInstaller inputInstaller;
        [SerializeField] private EyesInstaller eyesInstaller;
        [SerializeField] private VisionInstaller visionInstaller;
        [SerializeField] private CutscenesInstaller cutscenesInstaller;
        [SerializeField] private QuestsInstaller questsInstaller;

        // CLAUDE: dev tools: optional, stand in for modules that don't exist yet.
        [Header("Dev tools")]
        [SerializeField] private VisionTestSlider visionTestSlider;

        private CutsceneInputModeAdapter cutsceneInputMode;

        private void Awake()
        {
            // 1. Create, in dependency order.
            globalsInstaller.Build();
            var (gameInput, inputModes) = inputInstaller.Build();
            eyesInstaller.Build(gameInput);
            var vision = visionInstaller.Build();
            var cutscenes = cutscenesInstaller.Build(new CutsceneInputAdapter(gameInput));
            cutsceneInputMode = new CutsceneInputModeAdapter(cutscenes, inputModes);
            // CLAUDE: quests and texts go to Hints and Hud once they exist.
            var (quests, questTexts) = questsInstaller.Build();

            // 2. Load: nothing saved yet.

            // 3. Start.
            cutscenesInstaller.StartListening();
            questsInstaller.StartListening();
            if (visionTestSlider != null) visionTestSlider.Use(vision, inputModes);

            // CLAUDE: temporary, so the mouse stays in
            // CLAUDE: the window. Belongs in a future
            // CLAUDE: "application/cursor" module.
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void OnDestroy()
        {
            // Reverse order of creation.
            questsInstaller.Dispose();
            cutsceneInputMode?.Dispose();
            cutscenesInstaller.Dispose();
            visionInstaller.Dispose();
            eyesInstaller.Dispose();
            inputInstaller.Dispose();
            globalsInstaller.Dispose();
        }
    }
}
