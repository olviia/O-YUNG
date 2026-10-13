namespace Oyung.Quests
{
    /// <summary>
    /// Where a quest is in its life. Every step is sticky: a quest never
    /// goes back.
    /// </summary>
    public enum QuestState
    {
        /// <summary>Start condition not met yet.</summary>
        NotStarted,

        /// <summary>Started; objectives appear and get done.</summary>
        Active,

        /// <summary>Ended with an outcome (success, failure, any
        /// ending).</summary>
        Closed
    }

    /// <summary>
    /// Where one objective of a quest is. Sticky like the quest.
    /// </summary>
    public enum ObjectiveState
    {
        /// <summary>Not shown to the player yet.</summary>
        Hidden,

        /// <summary>Shown; waiting to be done or failed.</summary>
        Shown,

        Done,
        Failed
    }
}
