namespace Oyung.Vision
{
    /// <summary>
    /// Required port: whoever draws the blur (implemented by DepthOfFieldBlurDisplay).
    /// Receives the FINAL picture Vision decided, after its rules
    /// (e.g. a reduced-blur setting) — not the raw request from IVision.
    /// </summary>
    public interface IBlurDisplay
    {
        /// <summary>
        /// Shows the world blurred like this, as one picture.
        /// </summary>
        /// <param name="clearDistance">How far things look sharp, in metres.</param>
        /// <param name="strength">How blurry things beyond it are: 0 = sharp, 1 = newborn maximum.</param>
        void Show(float clearDistance, float strength);
    }
}
