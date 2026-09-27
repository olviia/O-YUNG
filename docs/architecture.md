# Architecture conventions

Hexagonal modular, level 3 of 4: every module is a small hexagon; the compiler enforces the boundaries.

## Terms
- **Inside** — a module's rules. Plain C#, no Unity (`noEngineReferences: true`).
- **Port** — an interface owned by the inside, in the inside's own language (never technology words).
  - *Offered port*: what the module does for others (`IFacts`, `IQuests`). Methods and/or events.
  - *Required port*: what the module needs from outside (`ISaveStorage`, `IBlurDisplay`, `ICutscenePlayer`).
- **Adapter** — outside code implementing a required port with a technology. Named after the technology (`FileSaveStorage`, `TimelineCutscenePlayer`, `InputSystemAdapter`). Plain class or MonoBehaviour.
- **Composition root** — the only place that knows both ports and adapters and connects them.

## Who knows whom
- Adapter → knows its port. Port → knows nothing outside. Inside → talks only to ports.
- Module → another module: only through that module's **ports**, one direction, no cycles.
- Nothing inside may reference presentation (HUD, camera). Presentation listens to ports.
- Unity-side modules may use each other's public API (e.g. HUD uses InputGlyphs).
- Only `Oyung.Root` references everything; nothing references the root.

## Layout per module
```
X/                    assembly Oyung.X        (inside, no Unity)
  Ports/              public interfaces — the module's contract
  *.cs                internal implementation
  AssemblyInfo.cs     [InternalsVisibleTo("Oyung.Root")] — only the root may create internals
  Unity/              assembly Oyung.X.Unity  (adapters; references Oyung.X + the technology)
```
Presentation-only modules (Hud, Eyes) have just a Unity assembly.

## Rules of thumb
- Inspector references for parts of the same object/prefab; the root injects other modules.
- Startup: create (dependency order) → load save → start. Shutdown in reverse.
- Core can't use Unity types (Vector2, Mathf, ScriptableObject): use plain floats / System.Numerics; ScriptableObject assets are translated to plain data by a Unity-side adapter.
- A new assembly only for an enforced boundary, not for every idea.
- Reusable modules get promoted to `Packages/com.olviia.*` after they prove themselves.

## Current modules (story 1)
| Module | Inside | Unity side | References |
|---|---|---|---|
| Facts | IFacts, Facts | — | — |
| Input | IGameInput | InputSystemAdapter, InputGlyphs | — |
| Save | ISaveStorage, SaveSystem | FileSaveStorage | Facts |
| Cutscenes | ICutscenePlayer, Cutscenes | TimelineCutscenePlayer | Facts, Input |
| Vision | IBlurDisplay, Vision | DepthOfFieldBlurDisplay | Facts |
| Quests | IQuests, Quests, QuestTracker | (later: quest assets → data) | Facts, Input |
| Hints | IHints, Hints | — | Quests |
| Hud | — | Hud | Quests, Hints, Input.Unity |
| Eyes | — | LookCamera | Input |
| Root | — | CompositionRoot | everything |
