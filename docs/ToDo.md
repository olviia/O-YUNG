
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
    C. parents' voices at the crib:
       4. muffled voices: AudioLowPassFilter on the speakers.
       5. sound waves at each mouth, readable through the blur.
    D. the quest asset tying it together; quest runs invisibly at
       first, minimal Hud later.
13. what characters say: a module for the phrases and the conditions
    they play under. Each line is one string table key: the voice always
    speaks its English entry, the display shows the localized one. One
    step per session:
    3. reactions: an event picks the most specific matching line by
       conditions; a row can play its lines in order, one per event
       (pinch mum: first, second, later). Built with the first real
       event. Reactions outrank chatter, which carries on afterwards.
    4. display: per conversation none, subtitles or dialogue box.
    5. graph: when a conversation really branches or offers choices.
       The full dialogue tool; ChatterAsset and ChatterPlayer are
       temporary and get deleted once it exists.
14. guard for starting activities (cutscene, minigame, dialogue): one
    exclusive activity slot. A met condition becomes a pending request
    that runs once, when the slot is free. Today two cutscenes meeting
    their condition at once throw in TimelineCutscenePlayer. Do it when
    the minigame arrives as the second activity.
15. main menu on Esc (gamepad: Start), action already in the always-on
    System map. In gameplay: stops the game and opens the menu. In the
    menu: closes it and resumes. In another menu: closes it, back to
    gameplay. In a cutscene: pauses it and opens the menu. Has a quick
    save button, the only way to save on a gamepad.
16. title screen in its own scene: New Game, Continue, and a dev-only
    list of test points.
