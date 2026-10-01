# Architecture conventions

Hexagonal modular, level 3 of 4: every module is a small hexagon; the compiler enforces the boundaries.

## Terms
- **Inside** — a module's rules. Plain C#, no Unity (`noEngineReferences: true`).
- **Port** — an interface owned by the inside, in the inside's own language (never technology words).
  - *Offered port*: what the module does for others (`IFacts`, `IQuests`). Methods and/or events.
  - *Required port*: what the module needs from outside (`ISaveStorage`, `IBlurDisplay`, `ICutscenePlayer`).
  - Offered/required is decided by **who uses** the port, not who implements it. `IGameInput` is offered
    (other modules use it) even though a Unity adapter implements it.
  - Same idea as hexagonal *driving/primary* (in) vs *driven/secondary* (out) ports, or UML provided/required interfaces.
- **Adapter** — outside code implementing a required port with a technology. Named after the technology (`FileSaveStorage`, `TimelineCutscenePlayer`, `InputSystemAdapter`). Plain class or MonoBehaviour.
- **Module installer** (`XInstaller`) — the module's own mini composition root. The only place that knows both the module's ports and its adapters and connects them.
- **Composition root** — connects *modules*, never their parts. Knows only installers and ports.

## Who knows whom
- Adapter → knows its port. Port → knows nothing outside. Inside → talks only to ports.
- Module → another module: only through that module's **ports**, one direction, no cycles.
- Nothing inside may reference presentation (HUD, camera). Presentation listens to ports.
- Unity-side modules may use each other's public API (e.g. HUD uses InputGlyphs).
- Only `Oyung.Root` references everything; nothing references the root.

## Layout per module
```
X/                    assembly Oyung.X        (inside, no Unity)
  Ports/              public interfaces — the module's contract (namespace stays Oyung.X)
    Offered/          what others may use: "how to use me"
    Required/         what must be plugged in: "what I need" (adapters implement these)
  *.cs                internal implementation
  AssemblyInfo.cs     [InternalsVisibleTo("Oyung.X.Unity")] — only the module's installer may create internals
  Unity/              assembly Oyung.X.Unity  (adapters; references Oyung.X + the technology)
    XInstaller.cs     public; the only public class here besides cross-module Unity APIs (adapters are internal)
```
Presentation-only modules (Hud, Eyes) have just a Unity assembly.

## Wiring: installers
Each module has one installer: a MonoBehaviour in the scene under `Modules/`.
- **Holds** the module's Unity pieces by inspector reference. Inspector references are fine *inside* a module boundary.
- **`Build(required ports…)`** creates the inside, creates the adapters, plugs them together.
  Its parameters *are* the module's needs; it **returns** what the module offers (a tuple when more than one port).
  `Build` is not shared between installers: each module needs different things.
- **`Dispose()`** (`IDisposable`) cleans up. This is the only part all installers share.
- Never talks to another installer; only the root calls it.

The root: one field per installer, `Build` in dependency order, `Dispose` in reverse. It never names a module's internal class.
Not used: DI containers and injecting ports into arbitrary MonoBehaviours (hides the map; service locator in disguise).
Modules own and drive their MonoBehaviours; runtime objects are spawned by the module that cares about them.

## Rules of thumb
- Inspector references only inside a module; ports cross modules only through the root.
- Startup: create (dependency order) → load save → start. Shutdown in reverse. Installers have only create + dispose until Save needs load/start.
- Core can't use Unity types (Vector2, Mathf, ScriptableObject): use plain floats / System.Numerics; ScriptableObject assets are translated to plain data by a Unity-side adapter.
- A new assembly only for an enforced boundary, not for every idea.
- Reusable modules get promoted to `Packages/com.olviia.*` after they prove themselves.

## Current modules (story 1)
| Module | Inside | Unity side | References |
|---|---|---|---|
| Facts | IFacts, Facts | — | — |
| Input | IGameInput, IInputModes, InputModeStack | InputInstaller, InputSystemAdapter, InputGlyphs | — |
| Save | ISaveStorage, SaveSystem | FileSaveStorage | Facts |
| Cutscenes | ICutscenePlayer, Cutscenes | TimelineCutscenePlayer | Facts, Input |
| Vision | IVision, IBlurDisplay, Vision | DepthOfFieldBlurDisplay | — (driven only through IVision) |
| Quests | IQuests, Quests, QuestTracker | (later: quest assets → data) | Facts, Input |
| Hints | IHints, Hints | — | Quests |
| Hud | — | Hud | Quests, Hints, Input.Unity |
| Eyes | — | EyesInstaller, LookCamera | Input |
| Root | — | CompositionRoot | installers' assemblies + ports |
