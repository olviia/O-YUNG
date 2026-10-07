# Conditions + instructions — thinking file (delete when built)
Decision: docs/architecture.md "Conditions and instructions".

## Map (arrows = "depends on")
    Story ──────────┐
    NPC AI ─────────┼──> Contract (Condition, Instruction)
    Facts.Unity ────┘         ^
      FactCondition ──────────┘ (subclass)
      FactsInstaller ──Bind(reader)──> each FactCondition asset
    Facts (core) knows nothing of the contract.

## CRC: Condition (abstract, contract)
Knows:
- has to hold all types of facts from all modules that will have facts? so we can drag and drop a fact in the condition and put the value? ahh no, it will be in the implementing scriptable objects. so example: a quest asks if this condition is met, quest doesnt know what condition it is, and only gets true or false. so it knows nothing
Does:
- so there is a public api method bool IsMet and an event raised when condition fact was changed, so the quest can subscribe on the event and when the event is risen, quest can ask if the condition is met
Collaborators:
- none, only implementors?

## CRC: Instruction (abstract, contract) all same as for condition
Knows:
- nothing
Does:
- allows other modules to interact with any type of fact-like information from any module that has this implemented?
Collaborators:
- same as condition

## Wishful thinking — write the calls you wish existed
Story, checking a quest step:
-
Story, finishing a quest step:
-
FactsInstaller, at startup:
- gets the collection of factsconditions scriptable objects, and passes through the constructor of some class in facts that does something so that the classes who reach through the interface know about what scriptable objects they are subscribing to? 

## Questions
1. Contract assembly name? (Oyung.Rules, Oyung.Conditions, ...) what is the standard? is there any standard at all? maybe it would be some abstract connectors? something that isnt very characteristic for hexagonal architecture, because it is sort of a 'core' module more, that just allow different modules to interact in this nxm design type? to avoid reimplementation of the same things in the multiple modules and separate communication between modules?
   DECIDED: Oyung.SharedKernel (plain C#, no .Unity: see exploration). DDD strategic terms only (context
   map), no tactical DDD. Rule: only abstract types 2+ modules agree on.
2. Does a consumer need to hear "I might have changed" from a
   condition, or does it re-ask (polling)? Who would raise it? consumer has to hear, otherwise we would have poll every frame or so, and it is bad from optimization point of view i think
3. Progress ("2/3"): on every Condition, or only some? maybe we dont need progress in condition, condition maybe should know only when it is met, so, for example, hud would know what condition id is and from what module to pull the current value fact, or something like that
4. AND / OR / NOT: a composite condition in the contract? i think we should have a single condition, and leave and/or/not for those who work with those conditions. like quest should define if it needs both conditions or one of them, i think
5. Unbound condition asked (installer not run): throw or false? i dont know... maybe a runtime or if possible, compilation error, so we know straight away and fix it, because in our design there shouldnt be unbound conditions at all as far as i understand

## Exploration: bind data assets, inline conditions (CLAUDE draft)
Test case: "the door opens when intro_seen is true; opening it
sets door_opened".

### What the designer sees (door's inspector, no extra files)
    Open when:  [Fact is      v]  fact: (intro_seen)   equals: [x]
    On open:    [Set fact     v]  fact: (door_opened)  value:  [x]
`[...v]` = dropdown of every Condition / Instruction subclass.
`(...)` = dragged FactAsset. AllOf / AnyOf / Not appear in the same
dropdown and nest inline.

### What happens at runtime (wishful thinking)
1. FactsInstaller: factCatalog.BindAll(store)
   -> every FactAsset now holds the live store.
2. Door asks: openWhen.IsMet()
   -> FactIsCondition: fact.Value == expected
   -> FactAsset: store.Get(Id)
3. Door listens: openWhen.Changed += Recheck
   -> FactIsCondition forwards fact.Changed
   -> FactAsset filters store.Changed by its own name
   (this IS the per-fact Watch we postponed; it falls out for free)
4. Door opens: onOpen.Execute()
   -> SetFactInstruction: fact.Set(value)   (internal, see Q-b)
5. FactsInstaller.Dispose: factCatalog.UnbindAll()

### CRC drafts
Condition (kernel, plain [Serializable] C# class, not an asset)
- Knows: nothing.  Does: IsMet(); raises Changed ("ask again").
FactAsset<T> (Facts.Unity, asset)
- Knows: stable name; the bound store (after Bind).
- Does: Value; Changed (this fact only); internal Set (only
  Facts.Unity instructions may write).
- Collaborators: store ports, FactCatalog.
FactCatalog (Facts.Unity, asset, kept up to date by the editor)
- Knows: every FactAsset.  Does: BindAll / UnbindAll.
- Bonus: Save and the Yarn checker get "all fact names" from it.
FactIsCondition / SetFactInstruction (Facts.Unity, inline)
- Knows: one FactAsset + a value.  Does: IsMet / Execute.

### What changes vs the current plan
- Binding moves from conditions (many) to fact assets (one per fact).
- One asset per fact, zero per condition / instruction.
- The kernel needs no UnityEngine: [Serializable] is plain C#.
  -> could be `Oyung.SharedKernel` (no .Unity), and even core
  assemblies could hold Conditions. Re-check the name decision.
- Same shape for every provider: Inventory binds ItemAssets, a
  HasItemCondition holds an ItemAsset. One pattern to learn.

### Questions to settle
a. FactAsset now carries runtime state (the bound store, not the
   value). OK? Values still live only in the store.
b. Write protection: FactAsset.Set internal -> only Facts.Unity
   instructions write; everyone else reads. Keeps the read/write
   split?
c. Domain reload is OFF in this project (EnterPlayModeOptions=0):
   asset state survives between plays. UnbindAll is mandatory, and
   Value on an unbound fact throws. Enough?
d. Dropdown tool: Unity has none built in. Candidate: open-source
   SubclassSelector (mackysoft SerializeReferenceExtensions).
   Check it works on Unity 6.4 before deciding.
e. Inline objects are copied per owner (two doors = two separate
   conditions). Any case where we WANT a shared, reusable condition
   asset? (e.g. "player is in chapter 2" used in 30 places)
f. Bool vs int: two classes each (FlagIs / CounterIs, SetFlag /
   AddToCounter), or does something smarter exist?

## Settled (2026-10-06) -> written into docs/architecture.md
- Inline conditions/instructions, bind data assets: YES.
- (b) data assets + their conditions internal; only installer public.
- (c) UnbindAll mandatory, reverse shutdown order.
- (e) no reusable condition assets.
- (f) dissolved: no central blackboard; each module owns its state.
  Facts -> PlotFlags (narrative memory; bool/number/string).
- Instruction: strict Execute, no TryExecute.
- Composites AllOf/AnyOf/Not in the kernel.
Next: what of today's Facts code to strip/rename; kernel CRC final.
- Condition = abstract class; IsMet property; Watch(callback) ->
  IDisposable handle (replaces Changed event). Composites return one
  handle that disposes all children's handles.
