using Oyung.Actors;
using Oyung.Cutscenes.Unity;
using Oyung.Eyes;
using Oyung.Globals.Unity;
using Oyung.Input;
using Oyung.Input.Unity;
using Oyung.Interaction.Unity;
using Oyung.Quests.Unity;
using Oyung.Root.Adapters;
using Oyung.Save.Unity;
using Oyung.Scratch;
using Oyung.Sequences.Unity;
using Oyung.Vision;
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
        [SerializeField] private SaveInstaller saveInstaller;
        [SerializeField] private GlobalsInstaller globalsInstaller;
        [SerializeField] private InputInstaller inputInstaller;
        [SerializeField] private EyesInstaller eyesInstaller;
        [SerializeField] private VisionInstaller visionInstaller;
        [SerializeField] private CutscenesInstaller cutscenesInstaller;
        [SerializeField] private QuestsInstaller questsInstaller;
        [SerializeField] private InteractionInstaller interactionInstaller;
        [SerializeField] private ActorsInstaller actorsInstaller;
        [SerializeField] private SequencesInstaller sequencesInstaller;

        // CLAUDE: dev tools: optional, stand in for modules that don't exist yet.
        [Header("Dev tools")]
        [SerializeField] private VisionTestSlider visionTestSlider;

        private CutsceneInputModeAdapter cutsceneInputMode;
        private QuickSaveAdapter quickSave;
        private IVision vision;
        private IInputModes inputModes;

        private void Awake()
        {
            // 1. Create, in dependency order.
            var (saveGame, saveBlocker) = saveInstaller.Build();
            var globalsSave = globalsInstaller.Build();
            var (gameInput, modes) = inputInstaller.Build();
            inputModes = modes;
            quickSave = new QuickSaveAdapter(gameInput, saveGame);
            eyesInstaller.Build(gameInput);
            vision = visionInstaller.Build();
            var (cutscenes, cutscenesSave) = cutscenesInstaller.Build(
                new CutsceneInputAdapter(gameInput), saveBlocker);
            cutsceneInputMode = new CutsceneInputModeAdapter(cutscenes, inputModes);
            // CLAUDE: quests and texts go to Hints and Hud once they exist.
            var (quests, questTexts, questsSave) = questsInstaller.Build();
            interactionInstaller.Build(
                new InteractionInputAdapter(gameInput, inputModes));
            var actorsSave = actorsInstaller.Build();
            var sequencesSave = sequencesInstaller.Build();
            saveInstaller.Add(globalsSave, cutscenesSave, questsSave,
                actorsSave, sequencesSave);

            // 2. Load.
            saveInstaller.Load();
            actorsInstaller.PlaceActors();
        }

        // CLAUDE: Start, not Awake: Unity has now run Awake and OnEnable on
        // CLAUDE: everything in the scene, so a sequence that starts at once
        // CLAUDE: after a load finds every actor and location registered.
        private void Start()
        {
            // 3. Start.
            cutscenesInstaller.StartListening();
            questsInstaller.StartListening();
            sequencesInstaller.StartListening();
            if (visionTestSlider != null) visionTestSlider.Use(vision, inputModes);

            // CLAUDE: temporary, so the mouse stays in
            // CLAUDE: the window. Belongs in a future
            // CLAUDE: "application/cursor" module.
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void OnDestroy()
        {
            // Reverse order of creation.
            sequencesInstaller.Dispose();
            actorsInstaller.Dispose();
            interactionInstaller.Dispose();
            questsInstaller.Dispose();
            cutsceneInputMode?.Dispose();
            cutscenesInstaller.Dispose();
            visionInstaller.Dispose();
            eyesInstaller.Dispose();
            quickSave?.Dispose();
            inputInstaller.Dispose();
            globalsInstaller.Dispose();
            saveInstaller.Dispose();
        }
    }
}
