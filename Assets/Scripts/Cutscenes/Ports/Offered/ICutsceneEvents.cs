using System;

namespace Oyung.Cutscenes
{
    /// <summary>
    /// port for what cutscenes announce to other modules
    /// </summary>
    public interface ICutsceneEvents
    {
        /// <summary>
        /// a cutscene began playing
        /// </summary>
        event Action Started;

        /// <summary>
        /// the playing cutscene finished or was skipped
        /// </summary>
        event Action Ended;
    }
}
