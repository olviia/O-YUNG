namespace Oyung.Vision
{
    // CLAUDE: knows the baby's current sight (how far it sees clearly; later: where it focuses);
    // CLAUDE: decides how the world looks blurred; tells IBlurDisplay. Driven ONLY through IVision —
    // CLAUDE: reads nothing itself (no Facts). Internal.
    internal class Vision : IVision
    {
        private readonly IBlurDisplay display;

        /// <param name="display">Draws the blur Vision decides on.</param>
        internal Vision(IBlurDisplay display)
        {
            this.display = display;
        }

        public void SetBlur(float clearDistance, float strength)
        {
            display.Show(clearDistance, strength);
        }
    }
}
