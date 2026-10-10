using System;
using System.Numerics;

namespace Oyung.Input
{
    /// <summary>
    /// port for game input
    /// </summary>
    public interface IGameInput
    {
        /// <summary>
        /// is triggered during a cutscene when Skip is held long enough 
        /// </summary>
        event Action SkipCutscenePressed;

        /// <summary>
        /// is triggered during a cutscene if Speed input is pressed
        /// </summary>
        event Action ToggleCutsceneSpeedPressed;
        
        /// <summary>
        /// how fast and in what direction a player moves their look (eyes) by x and y axes per frame
        /// </summary>
        Vector2 LookDelta { get; }
    }
}
