// CLAUDE: SKETCH ONLY. Lives in docs/, so Unity does not compile it.
// CLAUDE: Shapes and signatures to look at, with bodies written as plain sentences.
// CLAUDE: Read it top to bottom: Input → Eyes → Root.

// ─────────────────────────────────────────────────────────────
// Assembly: Oyung.Input.Unity   (next to InputSystemAdapter)
// Scene:    GameObject "Modules/Input" (your "module installers" folder)
// ─────────────────────────────────────────────────────────────

/// Assembles the Input module and hands out what it offers.
public class InputInstaller : MonoBehaviour, IDisposable
{
    // Settings moved here from CompositionRoot. They belong to Input, not to the root.
    [SerializeField] private float mouseDegreesPerPixel = 0.1f;
    [SerializeField] private float stickDegreesPerSecond = 120f;

    private InputSystemAdapter adapter;   // kept only so Dispose can clean it up

    /// Needs nothing from other modules → no parameters.
    /// Offers two ports → returns both (a tuple: two values in one return).
    public (IGameInput gameInput, IInputModes modes) Build()
    {
        // make the core:     InputModeStack
        // make the adapter:  InputSystemAdapter, given the core + the two settings
        // return:            the adapter as IGameInput, the core as IInputModes
    }

    /// The shared part every installer has: cleanup.
    public void Dispose()
    {
        // dispose the adapter
    }
}

// ─────────────────────────────────────────────────────────────
// Assembly: Oyung.Eyes
// Scene:    GameObject "Modules/Eyes"
// ─────────────────────────────────────────────────────────────

/// Assembles the Eyes module. Consumer only: needs input, offers nothing yet.
public class EyesInstaller : MonoBehaviour, IDisposable
{
    // Inspector reference INSIDE the module boundary. That's fine.
    [SerializeField] private LookCamera lookCamera;

    /// Its parameter list *is* its needs list. Nothing hidden.
    /// Offers nothing → returns nothing.
    public void Build(IGameInput input)
    {
        // hand the input to lookCamera   (the line that used to sit in the root)
    }

    public void Dispose() { /* nothing to clean yet */ }
}

// ─────────────────────────────────────────────────────────────
// Assembly: Oyung.Root
// ─────────────────────────────────────────────────────────────

public class CompositionRoot : MonoBehaviour
{
    // One field per MODULE, never per object.
    [SerializeField] private InputInstaller inputInstaller;
    [SerializeField] private EyesInstaller  eyesInstaller;

    private void Awake()
    {
        // Read it out loud. These are the sentences you wrote earlier:

        // "Input, build. Give me what you offer."
        //      var (gameInput, modes) = inputInstaller.Build();

        // "Eyes, here's IGameInput. Build."
        //      eyesInstaller.Build(gameInput);

        // Order matters: Input first, because Eyes needs its port.
        // This ordering is the root's real job.
    }

    private void OnDestroy()
    {
        // Reverse order: eyes, then input.
        //      eyesInstaller.Dispose();
        //      inputInstaller.Dispose();
    }
}

// CLAUDE: Things to notice:
// 1. The root never says InputModeStack, InputSystemAdapter or LookCamera.
// 2. Build is different per installer (different needs). Dispose is the same
//    for all of them, so it's the shared interface. IDisposable already exists in .NET,
//    so we reuse it instead of inventing IModuleInstaller.
// 3. `modes` is unused for now. Cutscenes/Menu will take it later.
