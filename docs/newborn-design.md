# Newborn stage — design structure

Status: draft, 2026-09-26. Game-design elements only; the code structure (classes, flow) is derived later with the CLAUDE.md workflow.

## Decisions from the first scenario (2026-09-27) — these override anything below
- Opening = eyes-opening cutscene. No nurse.
- Camera fixed at the eyes; rotation follows mouse/controller **strictly**, only within the eyes' range. **No sway** (motion sickness). Clumsiness comes from range limits and blur.
- Focus = minigame: trace the outline of the object. Then a soft popup shows it; the blur distance moves further out.
- Feeding: no minigame. Feeding gives Nourishment → fills a **Growth bar**.
- Traits from how the baby was cared for (e.g. needs met on time → Happy baby; often hungry → Chubby), lasting the whole life. The player shapes growth through play, not menus.
- Being carried changes the setting (kitchen, garden, square) → new things to learn before the baby can crawl.
- Days follow introduce → practise → twist → combine: each day one new mechanic, old ones in new settings.

## Core idea
The challenge is your own body: intention vs. clumsiness, control improving little by little. Failing is funny, never punishing. No fail states.

## Player verbs (all input in this stage)
| Verb | What it does | Grows with |
|---|---|---|
| Look | Move gaze (limited range, short duration at first) | Neck |
| Focus | Hold gaze on a target → it sharpens → learned | Eyes |
| Reach | Try to grab the gaze target; may miss or bop yourself | Arms |
| Cry | Express a need to caretakers | Voice |
| Tap / Hold | One-button reflex actions (grip, suck, head lift) | the matching skill |

## Core loop
Time passes → **Needs** rise → the **Body** shows signals → player **Cries** → **Caretaker** interprets (right or wrong) → acts, often starting a **Reflex** action → need satisfied → **Skill** XP.
In between: **Look / Focus** at the living room → things are learned into the **Collection** → new **Curiosity goals** → **Reach**.
Skills + key goals unlock the **Milestone** → growth → next stage.

## Elements

### 1. Body skills (RPG layer)
- Eyes, Neck, Arms, Voice. Levels grow through use (XP on successful actions).
- During play shown only through the body; the tree itself lives in the Baby Book (see Skill tree):
  - Eyes → blur radius shrinks.
  - Neck → look range and duration grow.
  - Arms → reach wobble shrinks, fewer self-bops.
  - Voice → cries become clearer (cry → coo → babble).
- Talks to: verbs (read skill levels), Milestones (requirements).

### 2. Needs (simulation layer)
- Hunger, Tiredness, Discomfort (dirty), Loneliness (want to be held).
- Rise over time and through events; lowered by caretaker actions.
- No bars. Diegetic signals: tummy rumble, heavy dim vision, fidgeting, whimpering.
- Tiredness at maximum ends the day (sleep).
- Talks to: Body signals, Caretakers, Day cycle.

### 3. Cry language
- One cry type per need. The caretaker understands correctly with a chance based on Voice.
- Wrong interpretation is comedy, not failure: you get changed instead of fed, and try again.
- Input and branching: see Controls and Skill tree.
- Talks to: Needs, Caretakers, Voice skill.

### 4. Gaze & focus
- The baby cannot move; looking is the main interaction.
- Focus = hold gaze → target sharpens → after enough time it is learned.
- Focus also selects the Reach target.
- Talks to: Eyes/Neck skills, Collection, Goals.

### 5. Reach
- Attempt to grab the focused target within arm range.
- Outcome by Arms skill: grab / miss / bop yourself.
- Talks to: Arms skill, Collection (touch), Goals.

### 6. Reflex actions
Short (seconds), juicy, one button, each started by a situation:
| Reflex | Trigger | Input | Result |
|---|---|---|---|
| Grip | a finger or object in the palm | tap fast | hold on; grip strength = taps |
| Suck | feeding | tap in rhythm | hunger drops faster |
| Head lift | tummy time | hold, release before tiring | see the room; Neck XP |
| Rooting | smell of food nearby | turn toward it | feeding starts sooner |
Theme: at first reflexes happen by themselves; skill turns them into deliberate actions.

### 7. Collection (visible hoarding)
- Categories: faces, creatures, objects, sounds, tastes.
- Proposal: shown in the world as ornaments on the crib mobile — each learned thing adds one.
- Talks to: Gaze, Reach, Goals.

### 8. Goals (quests)
- Sources: Needs ("I'm hungry"), Curiosity ("that glowing thing — must touch it"), Inner voice ("where am I?"), Milestone.
- At most 2–3 visible, as thought bubbles. No quest log.
- Goal = condition + reward (skill XP, collection entry, story beat, inner-voice line).

### 9. Milestone
- Requirements: skill levels + key goals.
- Completion: growth cutscene → next stage: new verbs, larger reachable area, new events.
- Newborn → **Roll over**.

### 10. Caretakers & living room (world events)
- Parent routine: chores, feeding, changing, carrying, singing.
- Responds to cries (via Cry language).
- Visitors, the cat, weather at the window, a creature on the sill, a dragon's shadow.
- An event director schedules events per day and reacts to the baby.

### 11. Inner voice
- Short text lines from the adult mind (isekai humour), triggered by events and failures.
- Cheapest content; carries tone and goals.

### 12. Day cycle
- Wake → several events → sleep. Sleep = save point, a clean place to stop.
- Newborn stage total ≈ 20–40 min.

---

# Detailed spec (all numbers are starting values for tuning)

## Controls (mouse + keyboard; gamepad later)
| Input | Action |
|---|---|
| Mouse move | Look. Gaze drifts and sways at low Neck; range is clamped. |
| Hold right mouse | Focus on what's under the gaze. |
| Left mouse | Reach toward the focused target. |
| Space | Cry. With only General Cry: just press. With specialised cries: hold → a small radial of cry pictograms appears around the cursor, move toward one, release. |
| Space during a reflex | The reflex input (tap / rhythm / hold). While a reflex runs, Space belongs to it. |
| Esc | Pause → Baby Book. |
Five inputs in total, one of them context-sensitive.

## HUD — almost none, diegetic
- **Needs**: screen-edge signals, 3 intensities each (at 40 / 70 / 90):
  - Hunger — warm pulsing vignette at the bottom + tummy rumble.
  - Tiredness — eyelids close from top and bottom, slowly.
  - Discomfort — greenish edge tint + fidgety camera jitter.
  - Loneliness — cold blue edges + whimper.
- **Goals**: thought bubbles top-left, max 3, pictogram + 2–4 words. Gold = story/milestone, sparkle = curiosity. Needs are never listed as goals — the body shows them.
- **Focus**: a soft ring around the gaze target fills while the image sharpens.
- **Reflex prompt**: one big soft glyph near the action (tap / rhythm / hold).
- **XP gain**: small glowing motes fly from the action into the baby's body + a soft chime. No numbers during play.

## Caretaker guesses and wrong actions
1. You cry → the parent shows a **guess bubble** above their head (bottle / diaper / moon / arms).
2. Right guess → you do nothing, the parent acts, the need signal fades, relief sound, XP motes.
3. Wrong guess → **cry again** before they start: they try the next guess. If they already started, the action plays out, the need signal does not fade, the baby grimaces, an inner-voice line appears ("That is not what I asked for."), the parent shows a "?" bubble and asks again.
4. No failure: a wrong guess only costs time while the need keeps rising.
- Guess accuracy: General Cry = random among the parent's likely guesses (~25–40 %). A specialised cry = ~90 % for its need.
- At need 100 the baby cries **by itself** (reflex cry, General Cry) — you lose control, which fits the reflex → control theme.

## Needs — numbers
| Need | Rises | Lowered by | Typical peaks per day |
|---|---|---|---|
| Hunger | ~12 / min | feeding (Suck reflex) | 1–2 |
| Tiredness | ~8 / min + effort | sleep (ends the day) | 1 |
| Discomfort | after feeding, ~10 / min | changing | 1 |
| Loneliness | when nobody is near, ~6 / min | being held / carried | 1–2 |
Scale 0–100.

## Skill tree — the Baby Book
- The tree is drawn as the parents' **Baby Book**: a growth chart with four branches. Opened at pause and at the end of each day.
- **XP is per branch and earned only by using it** (look → Neck, focus → Eyes, reach → Arms, cry → Voice).
- **New nodes are unlocked while sleeping**: at day's end the Baby Book opens, you spend the XP gathered that day, the parent writes an entry ("Day 3: followed the firefly!"). Real babies consolidate learning in sleep — and it keeps choices out of the action.
- Within a branch the player chooses the order; nodes need the previous tier.

| Branch | Tier 1 (30 XP) | Tier 2 (60 XP) | Tier 3 (100 XP) |
|---|---|---|---|
| **Voice** | Hungry Cry · Tired Cry · Dirty Cry · Lonely Cry (any order, each 30) | Coo — a happy sound: calms, makes the parent smile | Loud Cry — calls a caretaker from another room |
| **Eyes** | Sharper Sight — blur radius shrinks | Colour — the world starts high-contrast and near-grey; colour arrives | Tracking — follow moving things (fireflies, the cat) |
| **Neck** | Wider Look — larger gaze range | Hold Head — tummy time lasts longer | Turn to Sound — the gaze snaps toward interesting sounds |
| **Arms** | My Hands — hands visible and controllable | Swipe — reach hits targets more often | Grab — hold what you reach; hand-to-mouth for tasting |

Total: 16 nodes, ~800 XP.

## XP sources
| Action | XP |
|---|---|
| Learn a new thing by focusing | Eyes 10 |
| Look at a new part of the room | Neck 5 |
| Tummy time | Neck 2 / second held |
| Reach — hit / miss | Arms 10 / 3 (failing still teaches) |
| Cry understood / misunderstood | Voice 10 / 4 |
| Reflex completed | 8 to its skill |
| Goal completed | 20–40 to the related skill |
Target pace: 2–3 nodes per day, the whole tree in ~6–7 days.

## Days
- Day ≈ 5 min: wake → 3–5 events → tiredness → sleep → Baby Book.
- Newborn stage ≈ 6–7 days ≈ 30–40 min.

## Milestone — Roll over
- Requires: Neck **Hold Head**, Arms **Swipe**, and the goals *Who are you?* and *The spirit on the mobile*.
- Then the goal *Roll over* appears: tummy time + reach toward a toy just out of range → the growth cutscene.

## Quests (newborn)
Story / milestone (gold):
1. **First Breath** — the opening cry.
2. **Who are you?** — focus on the parent's face until it is sharp.
3. **Where am I?** — look at every part of the room.
4. **Roll over** — the milestone.

Curiosity (sparkle):
5. **The spirit on the mobile** — focus on it, then reach it.
6. **The finger** — the grip fight with the parent's finger.
7. **These are mine?** — discover your hands.
8. **First smile** — coo while the parent sings.
9. **The cat** — learn the cat; the cat learns you and starts sleeping near the crib.
10. **Shadow at the window** — a dragon passes; startle reflex; the parent comforts you.
11. **A visitor** — a grandparent or neighbour: a stranger's face to learn.
12. **The music box** — learn the sound; later coo along.
13. **First taste** — milk; the first entry in the taste collection.

## Open questions
- Day length in minutes (5 is a guess).
- Camera: first person only, or cutscenes in third person (the opening already switches).
- Does the parent have a mood that your behaviour affects?
