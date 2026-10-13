namespace Oyung.Quests
{
    // CLAUDE: knows quest definitions; decides when a quest starts,
    // CLAUDE: shows/completes objectives and closes with an outcome
    // CLAUDE: (via Conditions and Instructions). Implements IQuests.
    //
    // CLAUDE: DRAFT, not agreed yet. Target: KCD2's player-facing quest
    // CLAUDE: shape (we know little of their internals). Draft shape:
    // CLAUDE: quest = start condition + objectives + outcomes.
    // CLAUDE: objective = appears when (empty: after previous),
    // CLAUDE:   done when, optional. outcome = reached when (empty: all
    // CLAUDE:   required objectives done); first met closes the quest.
    // CLAUDE: Everything sticky; ids not positions.
    //
    // CLAUDE: KCD behaviour            | draft covers it?
    // CLAUDE: several objectives at once | yes, own "appears when"
    // CLAUDE: optional objectives        | yes
    // CLAUDE: several ways to do a thing | yes, AnyOf in "done when"
    // CLAUDE: endings, failure = ending  | yes, outcomes
    // CLAUDE: done before asked          | yes, checked on appearing
    // CLAUDE: quests connected           | yes, through conditions
    // CLAUDE: quest log grows            | later: notes
    // CLAUDE: timed quests               | when a clock offers a fact
    // CLAUDE: an objective fails         | NO: add "fails when"
    // CLAUDE: branches on objective state| PARTLY: QuestCondition must
    // CLAUDE:                            |   ask objective state too
    // CLAUDE: quest drives the world     | NO: add Instructions on
    // CLAUDE:                            |   start/shown/done/failed/
    // CLAUDE:                            |   outcome
    //
    // CLAUDE: Open: are the three additions wanted? How much to build
    // CLAUDE: now vs. leave room for.
    //
    // CLAUDE: Next session: read mechaniqe/unity-quest-core (MIT, v0.8)
    // CLAUDE: together: QuestManager, ObjectiveEvaluator,
    // CLAUDE: DirtyQueueProcessor, save/restore, tests. Close to this
    // CLAUDE: draft (fail conditions, prerequisites, optional, dirty
    // CLAUDE: queue) but no outcomes/start condition/instructions, and
    // CLAUDE: it runs on a static EventManager.Instance + own conditions.
    // CLAUDE: Then decide: own core / vendored fork adapted to the
    // CLAUDE: shared kernel / Pixel Crushers' Quest Machine ($65).
    internal class Quests : IQuests
    {
    }
}
