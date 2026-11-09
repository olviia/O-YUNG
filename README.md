# O-YUNG

You are born as a fantasy creature baby in a fantasy settlement. You grow
up through many small quests, and growing up is the progression: every
ability is earned the way a real baby earns it. Opening your eyes, moving
your head and crying for food come first, and later the world opens up
from the crib to the room, the house and the settlement.

It is a growing-up RPG rather than a narrative game. The mood mixes the
lightness of MySims, the growth of Spore and the living, physical world of
Crimson Desert. It is for all ages, but it is written to be interesting
for adults, not made for children.

O-YUNG is a solo personal project in early development

## What works now

- The intro cutscene: from the womb through the delivery room, the corridor
  and the forest to the nursery, with Skip (hold) and Speed controls.
- The nursery and a first-person baby camera with limited eye range and a
  blur that sharpens as the eyes get better.
- The parents: procedural walking on the NavMesh, feet fitted to the
  ground, and speech in a made-up "bup" language baked from processed
  voice grains, with lines from localized string tables.
- The first quest steps: the parents chat at the crib, and a minigame where
  you pop soap bubbles to type the baby's name.
- Saving: F5 quick save, and test points to start the game from any saved
  state.

## Built with

- Unity 6000.4.10f1, URP, Input System, Cinemachine, Timeline, Localization
  and the Unity Test Framework.
- C#, arranged as DDD with hexagonal architecture. Each module (Quests,
  Cutscenes, Speech, Save, Locomotion, Vision and others) is its own
  assembly. Its core is plain C# behind ports, with Unity adapters at the
  edge, so modules can be reused in future games. See
  [docs/architecture.md](docs/architecture.md).
- [CodeMap](https://github.com/olviia/unity-codemap), my own Unity package
  that generates the API index in [docs/INDEX.md](docs/INDEX.md). It is
  included as a git submodule.
- Python tools for the art and audio pipelines: Blender scripts for models
  and textures in `tools/blender`, and voice baking in `tools/voice`.

## How to run

1. Clone with the submodule:
   `git clone --recurse-submodules https://github.com/olviia/O-YUNG.git`
2. Open the folder in Unity 6000.4.10f1 (Unity Hub, Add project from disk).
3. Open `Assets/Scenes/SampleScene` and press Play. To start from a saved
   moment, pick a test point on the `Test/TestStart` object.

## Plans

- [docs/game-brief.md](docs/game-brief.md): the direction of the whole
  game, its pillars and growth stages.
- [docs/newborn-design.md](docs/newborn-design.md): how the newborn stage
  plays (verbs, needs, core loop).
- [docs/newborn-plan.md](docs/newborn-plan.md): the newborn stage split
  into 13 playable stories. The to-do list is built on this plan.
- [docs/ToDo.md](docs/ToDo.md): the work in progress and next up.

## License

This project is under the [MIT License](LICENSE). Third-party material (letter
meshes, textures, sound sources, fonts) keeps its own license, listed in the
same file.
