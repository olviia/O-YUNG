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

        // CLAUDE: PLANNED, not implemented. Eyesight stages: Progression says WHICH stage, Vision knows what each stage LOOKS like.
        // CLAUDE:
        // CLAUDE:   void SetEyesightStage(int stage);   // 0 = base (newborn) ... StageCount - 1 = last
        // CLAUDE:   int StageCount { get; }             // so a caller can tell when the eyes are fully grown
        // CLAUDE:
        // CLAUDE: Stage table (Vision's own data, set in the inspector, handed to the core as plain numbers by the installer):
        // CLAUDE:   stage 0, base:              clear 0.14 m, strength 0.81
        // CLAUDE:   stage 1, first improvement: clear 0.23 m, strength 0.60
        // CLAUDE:   stage N-1, last:            clear ?    , strength ?     <- TO DECIDE
        // CLAUDE:   stages 2 .. N-2: spaced evenly between the first improvement and the last
        // CLAUDE:   N = stage count                                         <- TO DECIDE
        // CLAUDE:
        // CLAUDE: Open: does SetBlur stay (dev slider, cutscenes forcing a look) or get replaced by SetEyesightStage?
    }
}
