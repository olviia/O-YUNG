6. cutscenes: two buttons while a cutscene plays: Skip, and Speed x2
   (like Crimson Desert). Speed = director playback speed; anything
   timed (shader pulse, audio) must follow the director's time.
7. game music after the intro: a blend of white noise, fantasy bard
   melody, forest sounds and a lullaby. Plays when the eyes open.
   Belongs to a future audio/music piece, not to cutscenes.
8. tests with Unity Test Framework: EditMode tests for the pure C#
   modules (CutsceneStore, InputModeStack, conditions); PlayMode tests
   for cutscene play / skip / speed, with InputTestFixture faking input.
9. colour palette for the whole game, so UI and world stay consistent:
   warm, gender neutral, not overly cozy (not farm-game cozy), adequate.
   The hold-fill amber on button icons is a placeholder until then.
10. LastUsedDevice is static (mirrors the static Input System). Make it
    an injected service, so tests can fake a gamepad. Do it with item 8.
11. an explicit "app is shutting down" state every module can ask,
    owned by CompositionRoot. Today TimelineCutscenePlayer guesses it
    from scene.isLoaded so quitting mid-cutscene isn't an ending; any
    module reacting to "stopped/ended" events will hit the same trap.
12. first quest: teaches moving the head without saying "look around".
    Objective 1 done when the player focuses the parents' voices;
    objective 2 "Eavesdrop your name"; quest done when the name is
    chosen. One slice per session:
    A. gaze + focus: Eyes knows what it looks at, Focus input (single
       press), prompt that shows/hides with the gaze.
    B. name: letters pop up in the parents' speech bubbles, the player
       pops them to build the name; must be clear that this IS
       choosing your name. Plus a string fact for the name.
    C. NPC stub: hardcoded parents walking to the crib, marked as a
       temporary file to be replaced by a real NPC module. Muffled
       voices, sound waves at the voice source. No Timeline.
    D. the quest asset tying it together; quest runs invisibly at
       first, minimal Hud later.
