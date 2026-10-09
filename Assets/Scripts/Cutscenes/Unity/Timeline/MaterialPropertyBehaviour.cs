using UnityEngine.Playables;

namespace Oyung.Cutscenes.Unity
{
    /// <summary>
    /// Runtime stand-in for one MaterialPropertyClip while the timeline
    /// plays. Only points at its pose; the mixer reads and blends.
    /// </summary>
    public sealed class MaterialPropertyBehaviour : PlayableBehaviour
    {
        internal MaterialPropertyClip Pose { get; set; }
    }
}
