# Game brief

Status: direction, evolving. Rewritten 2026-09-26. Plan only the beginning; the rest grows with content.

## One line
Reborn as a fantasy creature baby in a fantasy settlement — a long, funny story of growing up, told through many small quests. Isekai-reincarnation feel.

## Pillars
- **Growth is the progression.** Stages (baby → … → adult). Each stage ends with a milestone quest; completing it = you grow.
- **Learning is the tutorial.** Every ability is earned like a real baby earns it, adjusted to a fantasy creature: open eyes, move, cry for food, ask to be cleaned, ask to be carried, fight the parent's finger grip.
- **The world grows with you.** Small place first; what you can reach expands as your body does. Your changes stay visible (what you built, found, placed, changed). Map can be extended later with new parts, like Genshin regions.
- **Load at MySims level.** Simple controls, nothing to relearn after a break. Never bored through variety and density, not complexity.
- **Real game, not just a loop.** Systems + core loop + polish. Reuse existing tools wherever possible.

## Production stance
No placeholder prototypes: build beautiful and complete from the start (placeholders cause doubt and frustration). Hexagonal architecture keeps mechanics easy to add and remove.

## Opening
Cutscene: reddish pulsing noise texture, slowly lightening. A timed choice "Cry" (~5 s).
- Not pressed: camera moves from first person out to the room; a nurse lightly slaps the baby, it cries.
- Pressed: the same cutscene, starting from the cry, skipping the nurse.

## Stage 1 — baby (first scope)
Sluggish, blurry vision. Quests unlock basic abilities one by one: open eyes, move, cry and request food, request cleaning, request being carried, grip fight with the parent's finger.

How the newborn plays (systems, verbs, loop): see `newborn-design.md`.

Theme: newborns act on reflex; growing up = gaining control. Early actions happen by themselves; quests turn reflexes into deliberate actions.
Milestones (**bold**) = growth moments that also widen the map: crib → room → house → settlement.

**Newborn (0–1 mo)** — blur beyond ~25 cm, knows parent's voice/smell, reflexes, sleeps a lot.
- Focus on the parent's face (blur sharpens only up close).
- Rooting: turn head to find food.
- Grip fight: grasp reflex — first tap mechanic.
- Recognise the parent's voice among many.
- Startle reflex at a loud noise (a dragon passing over).

**1–3 mo** — social smile, cooing, eye tracking, head lift, discovers hands.
- Track a firefly / mobile with your eyes.
- Tummy time: hold your head up long enough to see the room (charge mechanic).
- Discover your hands ("…these are mine?").
- First smile: settlers light up — first effect on the world.

**4–6 mo** — rolls, reaches and grabs, everything in the mouth, laughs, first solids, teething.
- **Roll over** — first real movement.
- Taste everything: a visible taste collection, not a menu.
- First solid food: spit it at someone.
- Teething: horn buds / a spark on sneezing.

**6–9 mo** — sits, crawls, peekaboo (object permanence), stranger wariness, drops things to watch them fall.
- **Crawl out of the room** — map grows past the nursery.
- Hide; peekaboo.
- Meet a villager: stranger now, friend later.
- High-chair game: drop the spoon, parent picks it up, repeat.
- Bang pots: first "music".

**9–12 mo** — pulls to stand, cruises furniture, points, first words, waves, first steps.
- Point to request things — pointing becomes the interaction verb.
- First word, chosen by the player; settlers remember it.
- Cruise along furniture.
- **First steps** — you become a toddler; the settlement opens.

**Next stage — toddler (1–2 y)**: walking, running, climbing, stacking, "no!", tantrums, imitating adults, hiding, eating the cat's food, eating soil.

## Combat idea
Charge by tapping fast for ~1 s, release into an ultrafast burst: N stabs = N taps. Second button: shield, strength = taps. Possibly chaining between enemies like Path of Exile's Lightning Arc (a favourite).
The baby's finger-grip fight can be the first, gentlest version of this mechanic.
Known risk: mashing is tiring/inaccessible — plan a hold/auto alternative.

## Loved references
Witcher 3 (never bored — a mix of things), Path of Exile Lightning Arc, Spore (growth), The Sims (life stages), MySims (load level), old Shrek / Ice Age games (simplicity).

## Don'ts
- No precision jumping / platformer puzzles in 3D.
- No investigation / uncovering-mystery story structure.
- Not "cute but something is off" — no eerie undertone.

## Content rule
New content extends the top (a new stage or area) or works at any stage — never only inside a stage players have already outgrown.

## Open questions
- Does the player shape growth (Spore-like choices at milestones) or is it fixed?
- Where does the creature come from, and who is the family / settlement?
- Does the baby have an inner voice (previous-life memories, isekai humour)?
- Camera: baby's-eye view (cheap: parent = hands and face) or third person?
- Art cost per stage: one rig with changing proportions?
