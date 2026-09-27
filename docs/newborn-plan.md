# Newborn stage — implementation plan

Epic: **Newborn stage** (design: `newborn-design.md`).
Each numbered item is a story: it ends playable, beautiful for what it covers, and a clean place to stop.

## Order
Rule: first what everything else depends on, then the core loop, then content around it.

1. **Crib room & baby eyes** — the finished nursery scene, first-person baby camera, Look with limited range (no sway).
2. **Focus & blur** — the blur that sharpens on focus; learning a thing. The signature visual.
3. **Needs & body signals** — four needs rising over time, screen-edge signals, involuntary cry at 100.
4. **Cry & caretaker** — General Cry, the parent's guess bubble, right/wrong responses.
5. **Reflexes** — Suck (with feeding), Grip (the finger), Head lift (tummy time).
6. **Days, XP & Baby Book** — day cycle, sleep, XP motes, the skill tree and unlocking nodes at sleep.
7. **Goals & inner voice** — thought bubbles, the goal system, inner-voice lines.
8. **Reach & hands** — hands, swipe, grab, bopping yourself.
9. **Collection mobile** — learned things appear as ornaments on the mobile.
10. **Living room events** — the cat, the dragon's shadow, the visitor, the music box.
11. **Quests** — the 13 newborn quests as content.
12. **Opening & milestone** — the opening cutscene, Roll over, the growth cutscene.
13. **Polish pass** — sound, feel, tuning the numbers.

## Scenario — first minutes (your words, 2026-09-27)
Cutscene: the eyes open. First-person view, camera fixed at the eyes, rotating only as far as eyes can rotate, following the mouse/controller strictly (no motion sickness). The baby lies in the crib; toys hang over it, very blurry.
Quest intro: "Look around", with the input icon. Next quest: "What is this thing?" → focus mechanic → minigame: trace the outline of one toy → soft popup showing the toy; the blur distance moves further out, the toys are visible now → notification: eyes became better.
Popup/cutscene "tired, sleep". Sleep is very short. Wake up unhappy, hungry → cry → the parent comes and feeds (no feeding minigame). Feeding gives "nourishment", which grows the Growth bar. Meanwhile the bottle can be studied and is added to memory like the toy, with a popup.
Open: how 0–3 months fits into ~20 minutes, varied — repeat mechanics, add new ones, reuse old ones in new settings. Traits from care (Happy baby, Chubby).
**Story 1 covers:** eyes-opening cutscene → Look → "Look around" quest done.

### Story 1 — modules (who knows / decides / shows)
| Module | Knows | Decides | Shows |
|---|---|---|---|
| Facts | all facts | nothing (store only) | — |
| Cutscenes | cutscene + its start condition | when to play (reads Facts); writes a fact when done | the cutscene |
| Vision | sight level | blur amount | the blur |
| Input | devices, bindings | which action was pressed | — |
| Input glyphs | icon per action per device | which icon/letter now | — (asked by others) |
| Quests | definitions: start condition, goal, completion fact | start / complete (reads/writes Facts) | — |
| Quest trackers | progress of one goal | when the goal is met | — |
| Hints | idle time per active quest | when to nudge | — (asks HUD) |
| HUD | no rules | presentation only | popup → snap to side, quest tab, completion particles |
| Save | where data lives | when to save | — |

### Story 1 — flow
1. Game start → Facts loaded (empty on new game).
2. Intro cutscene condition (`IntroSeen` false) → plays → sets `IntroSeen`.
3. Vision starts at maximum blur.
4. "Look around" start condition (`IntroSeen`) → quest starts → HUD popup centre → dissolves → snaps to the side.
5. Tracker measures looking; idle too long → Hints → HUD points at the quest and explains the input.
6. Enough looking → quest completes → sets `LookAroundDone` → HUD removes it with particles.

### Story 1 — existing tools to try first
- Blur: probably no custom shader. URP's built-in Depth of Field blurs
  by distance, and "the blur distance moves further out" is just
  changing its focus distance. Worth trying first.
- Cutscene: Unity Timeline (already installed) plus Cinemachine.
- Input: Unity's Input System (already installed) makes keyboard, mouse
  and controller the same actions. For icons, Kenney's input prompts
  are a free CC0 set, and there are paid Asset Store packs that do
  device switching for you.
- Localised key letters: a subtlety. The letter printed on a key
  depends on the player's keyboard layout, not the game's language. A
  player with a French layout presses the key labelled "Z" where an
  English player has "W". The Input System already reports the layout's
  name for each key. So the setting you described would really be
  "show my keyboard's letters" versus "always show English (QWERTY)
  letters". That's worth deciding consciously.
- Popup animation: a tween library such as PrimeTween (free, fast) or
  DOTween.
- Particles on the UI: UIParticle by mob-sakai (free) lets normal
  particle effects render on the UI.
- Quests: for this game I'd write our own small quest module on top of
  Facts rather than buy a big quest asset. It's the heart of the game
  and your needs are specific, which is where "existing tools first"
  stops paying off. Everything around it can be bought or borrowed.


### Story 1 — build order
Rule: a module is built only after the modules it depends on; ties go to what is visible first.
0. **Wiring** — how modules get references to each other (decision + a tiny composition root). Every module depends on this choice.
1. **Input** (+ context stack) → **Look camera** — first visible result.
2. **Vision** — the blur.
3. **Facts** + **Save**.
4. **Cutscenes** (read/write Facts).
5. **Quests** + **tracker**.
6. **Input glyphs**.
7. **HUD** — popup, snap, quest tab, particles.
8. **Hints**.

### Story 1 — startup (step 0, your list, corrected)
Phase 1 — **create**, in dependency order:
1. Facts (depends on nothing).
2. Input (depends on nothing) — earlier than cutscenes: they push the Cutscene context and listen for Skip.
3. Save (needs Facts).
4. Cutscenes (needs Facts, Input).
5. Vision (needs Facts) — controls the blur. Not Input: the camera is what reacts to input.
6. Quests (needs Facts).
7. Quest trackers (needs Quests, Input).
8. Hints (needs Quests).
9. Input glyphs (needs Input).
10. HUD (listens to Quests, Hints, Glyphs — never reads Facts, so it never decides rules).
Scene objects receiving modules: camera ← Input; blur volume ← Vision; cutscene player (Timeline) ← Cutscenes; HUD prefab ← Quests, Hints, Glyphs. Inside the HUD prefab: plain Inspector references.

Phase 2 — **load** the save into Facts.
Phase 3 — **start**: only now cutscenes, quests and vision check their conditions (otherwise the intro plays before the save is loaded).
Shutdown: reverse order.

### Per step: reuse check
For each module, in this order: (1) does an existing tool do it? (2) does HaywireCleaner have it? (3) verdict: **adopt tool / port + clean / write new**.
HaywireCleaner code is ported only if: one clear responsibility, few dependencies, no hidden global state, readable after two months, not replacing an existing tool.

### Reuse review — Input (HaywireCleaner)
Keep:
- The idea: core knows intents/contexts, only the adapter knows Unity's Input System (hexagonal seam).
- `InputRouter` context stack (Gameplay / Cutscene / Menu).
- Glyph device switching: detect the device that acted, pick its control scheme, clear the cache.
Don't keep:
- Static event hubs (`ModuleInput`, `MenuInput`, `CutsceneInput`) and the static locator `GlyphInput` — hidden global state; anything can raise input.
- `InputReader` hand-wiring: every new action = new field + lookup + subscribe + unsubscribe + route. Unity can **generate a typed C# class** from the actions asset (existing tool) — no string lookups.
- Game logic in the adapter (Interact also raises StopCharging).
- `GetGlyph(string)` + `KeyFor(Intent)` double indirection; sprite never filled.

## Subtasks inside every story
1. Scenario in plain words.
2. Who knows / decides / shows → class names (CLAUDE.md workflow).
3. Responsibilities + signatures + `///` (you).
4. Search for existing tools/assets first.
5. Implementation (Claude), review.
6. Art / sound / animation for this story.
7. Play it; tune; note what was learned in the design doc.
