# Architecture conventions

Hexagonal modular, level 3 of 4: every module is a small hexagon; the compiler enforces the boundaries.

## Terms
- **Inside** — a module's rules. Plain C#, no Unity (`noEngineReferences: true`).
- **Port** — an interface owned by the inside, in the inside's own language (never technology words).
  - *Offered port*: what the module does for others (`IFactReader`, `IQuests`). Methods and/or events.
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

## State ownership (decided 2026-10-06; replaces the Facts blackboard)
No central blackboard. Every module owns its state and answers
questions about it (information hiding; DDD: each bounded context
owns its data). Quests own quest status, Inventory owns items,
Cutscenes own "played". Nobody else stores a copy.
- **"Fact"** is only a word for "any state a Condition can ask
  about". It is not a module and has no universal type system.
- **Globals module** (`Oyung.Globals`, was Facts; Bethesda's term):
  named values whose meaning lives only in content (conditions,
  instructions, dialogue); no code interprets them. Not "story":
  quests and cutscenes are story too. If code must interpret a value,
  it belongs to a module, not here. Kinds are a closed
  set: bool + int; a new kind is a deliberate decision. Dialogue tool
  not chosen (Yarn or other); it connects through an adapter that
  converts types. Anything richer is module state, not a global.
- Globals is also the first, simplest reference implementation of
  the provider template below.

## Conditions and instructions (decided 2026-10-06, not built)
Problem: Story, NPC AI, later others must ask "is X so?" and say
"make X so" about state owned by many modules, without knowing those
modules (N x M problem).
- **Shared Kernel (DDD)** `Oyung.SharedKernel`: plain C#
  `[Serializable]` abstract classes, no UnityEngine (so even cores may
  hold them). Rule (DDD Shared Kernel, refined): code that knows no
  module, is stable, and is needed by 2+ modules. Abstractions plus
  small helpers over them (precedent: Microsoft.Extensions.Primitives,
  IChangeToken + CompositeChangeToken). Consumers and providers
  reference only `Oyung.SharedKernel`. `Oyung.SharedKernel.Unity`
  (composites + internal HandleGroup) is a leaf nobody references:
  Unity serializes it, the dropdown finds it by type scan. Each
  provider makes its own Watch subscription object; extract a shared
  one only after three providers repeat it (Rule of Three).
  - `Condition` (abstract class: a serialized polymorphic family,
    not a port): `IsMet` property (cheap, no side effects);
    `Watch(callback)` returns an IDisposable handle; callback means
    "I might have changed, ask again", no value. Holder of the handle
    owns the subscription; the condition hooks its provider inside
    Watch, unhooks on Dispose. No events, no Start/Stop.
  - `Instruction`: Execute(), strict. Precondition broken -> throws,
    naming the asset (Design by Contract). "Can" is a Condition the
    designer puts next to it. No TryExecute / bool result.
  - Composites `AllOf` / `AnyOf` / `Not` in `.Unity` (need
    [SerializeReference]). Empty group / missing child throws.
    Later: editor validator blocks Play/build on empty slots.
- **Provider template** (every state owner; a Plug-in / Microkernel
  shape: kernel = extension points, modules plug in, found by type
  scan). Only the installer is public.
  Core (pure C#, internal):
  1. state + rules (Globals: name -> value; Quests: status;
     Inventory: counts, refuses negatives); internal change events.
  2. snapshot for Save (memento), later.
  Unity side (internal):
  3. data assets = designer handles (GlobalAsset, QuestAsset,
     ItemAsset): stable name, typed by class, bound to the live core.
  4. catalog (editor-maintained): BindAll / UnbindAll; also lists
     names for Save and the Yarn checker.
  5. kernel adapters, inline, one data asset + parameters each:
     Conditions (GlobalIs, QuestIs, HasItem), Instructions (SetGlobal,
     RemoveItem), later value readouts. Watch returns the module's
     own subscription object. Asking an unbound asset throws.
  6. installer: creates core, BindAll; Dispose = reverse.
  7. module-only adapters (Globals: Yarn variable storage).
  Kernel as generic ports of every owner: Instruction = write,
  Condition = yes/no read, future value readout = value read (HUD).
  No per-module public read/write interfaces, no ids crossing borders.
- **Consumers** (Story, NPC AI) hold `[SerializeReference]` Condition /
  Instruction fields, edited inline (dropdown of subclasses); no
  asset per condition. Reach the right class by polymorphism.
- **Domain reload is OFF** (EnterPlayModeOptions=0): asset state
  survives plays, so UnbindAll is mandatory. Shutdown = reverse of
  startup: consumers unsubscribe, then providers unbind.
- Later (editor polish): dropping a data asset into an empty slot
  picks the matching condition class (narrowed dropdown if several).
- Patterns: Specification + Composite (conditions), Command
  (instructions), Dependency Inversion, Shared Kernel (DDD); each
  condition class = adapter from a provider's state to the kernel.
- Open: dropdown tool (candidate: mackysoft SubclassSelector, check
  Unity 6.4); Yarn asking quest/item state (variables cover only
  Globals); event chain re-entrancy (instruction -> change ->
  instruction ...).
- Rejected (don't re-suggest): central blackboard of all state;
  universal fact type system (FactId<T> for everything); one
  FactAsset + Kind dropdown + FactValue (tangled); conditions as
  assets (asset explosion, 3 files per rule); reusable shared
  condition assets; conditions owned by a consumer (Quests) or a hub
  module that knows providers; Story as the only decider (NPC AI also
  decides); scoped static / type registry / service locator for
  binding; TryExecute returning bool; Changed event with lazy
  ref-counted hookup or explicit Start/Stop (Watch handle chosen);
  global change pulse; polling. R3 (reactive) = possible later
  upgrade, same shape.

## Save: still to build
- **Memento save.** Save knows *where* and *when*, never *what*. Each module with state hands Save its own sealed section and gets it back on load. **REVISIT:** avoid every provider's .Unity depending on Save; likely the same kernel/Bind approach. Module-private state (positions, masks) is saved by its owner, never as a global.
- **Dialogue tool** (Yarn Spinner or other, not chosen), installed when real dialogue appears. An adapter in Globals maps its variables onto globals (names = global names, types converted). Later: an editor check that every name used in scripts has an asset.

## Current modules (story 1)
| Module | Inside | Unity side | References |
|---|---|---|---|
| Globals | GlobalStore (internal); IFactReader/IFactWriter/FactId to delete | BoolGlobal/IntGlobal assets; planned catalog, GlobalIs/SetGlobal, installer | SharedKernel (planned) |
| Input | IGameInput, IInputModes, InputModeStack | InputInstaller, InputSystemAdapter, InputGlyphs | — |
| Save | ISaveStorage, SaveSystem | FileSaveStorage | — |
| Cutscenes | ICutscenePlayer, Cutscenes | TimelineCutscenePlayer | Input |
| Vision | IVision, IBlurDisplay, Vision | DepthOfFieldBlurDisplay | — (driven only through IVision) |
| Quests | IQuests, Quests, QuestTracker | (later: quest assets → data) | Input |
| Hints | IHints, Hints | — | Quests |
| Hud | — | Hud | Quests, Hints, Input.Unity |
| Eyes | — | EyesInstaller, LookCamera | Input |
| Root | — | CompositionRoot | installers' assemblies + ports |
