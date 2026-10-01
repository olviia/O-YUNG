namespace Oyung.Vision
{
    /// <summary>
    /// Offered port: the only way into Vision.
    /// Callers (Root at startup, later Progression) REQUEST how the baby sees;
    /// Vision applies its own rules before anything is shown.
    /// </summary>
    public interface IVision
    {
        /// <summary>
        /// Requests how well the baby sees. Both values change together:
        /// when eyes get better, the clear zone grows and the blur weakens.
        /// A grown child with normal sight: strength 0.
        /// </summary>
        /// <param name="clearDistance">How far the baby sees clearly, in metres.</param>
        /// <param name="strength">How blurry things beyond it are: 0 = sharp, 1 = newborn maximum.</param>
        void SetBlur(float clearDistance, float strength);
    }
}
