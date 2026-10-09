using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace Oyung.Cutscenes.Unity
{
    /// <summary>
    /// Timeline row that animates a UI image's material: its clips are
    /// poses (named shader values); overlapping clips crossfade.
    /// Optionally writes the Timeline's time into a shader property, so
    /// shader motion follows Speed x2 and scrubbing.
    /// </summary>
    [TrackColor(0.85f, 0.35f, 0.3f)]
    [TrackBindingType(typeof(Graphic))]
    [TrackClipType(typeof(MaterialPropertyClip))]
    public sealed class MaterialPropertyTrack : TrackAsset
    {
        [Tooltip("Shader float that receives the Timeline's time in " +
                 "seconds. Empty: none.")]
        [SerializeField] private string timeProperty = "_T";

        public override Playable CreateTrackMixer(
            PlayableGraph graph, GameObject go, int inputCount)
        {
            var mixer = ScriptPlayable<MaterialPropertyMixer>
                .Create(graph, inputCount);
            mixer.GetBehaviour().TimeProperty = timeProperty;
            return mixer;
        }
    }
}
