namespace Oyung.Cutscenes
{
    /// <summary>
    /// port for reading and changing how cutscenes play,
    /// e.g. from a settings menu or the HUD
    /// </summary>
    public interface ICutsceneSettings
    {
        /// <summary>
        /// playback speed when the player switches to fast
        /// </summary>
        float FastSpeed { get; set; }
    }
}
