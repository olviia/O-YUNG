namespace Oyung.Interaction
{
    // CLAUDE: one interactable the player could be looking at this
    // CLAUDE: frame. The Unity side already checked range and line of
    // CLAUDE: sight; the Interactor only compares angles.
    internal readonly struct Sighting
    {
        internal Sighting(IInteractable target, float angle)
        {
            Target = target;
            Angle = angle;
        }

        internal IInteractable Target { get; }

        /// <summary>Degrees between the view centre and the target's
        /// nearest point.</summary>
        internal float Angle { get; }
    }
}
