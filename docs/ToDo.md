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
