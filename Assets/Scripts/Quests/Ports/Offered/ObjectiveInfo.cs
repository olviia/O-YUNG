namespace Oyung.Quests
{
    /// <summary>
    /// What others may know about one objective right now (e.g. the HUD
    /// listing a quest's shown objectives).
    /// </summary>
    public readonly struct ObjectiveInfo
    {
        public ObjectiveInfo(string id, bool isOptional, ObjectiveState state)
        {
            Id = id;
            IsOptional = isOptional;
            State = state;
        }

        public string Id { get; }

        /// <summary>Optional objectives never hold up the quest's default
        /// outcome.</summary>
        public bool IsOptional { get; }

        public ObjectiveState State { get; }
    }
}
