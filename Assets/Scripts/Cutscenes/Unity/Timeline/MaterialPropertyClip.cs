using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Oyung.Cutscenes.Unity
{
    /// <summary>
    /// One pose on a MaterialPropertyTrack: the shader values it sets.
    /// A value the pose doesn't list keeps the material's own value.
    /// Saved in the timeline asset; never changed while playing.
    /// </summary>
    [Serializable]
    public sealed class MaterialPropertyClip : PlayableAsset,
        ITimelineClipAsset
    {
        [SerializeField] private List<FloatValue> floats =
            new List<FloatValue>();
        [SerializeField] private List<ColorValue> colors =
            new List<ColorValue>();
        [SerializeField] private List<VectorValue> vectors =
            new List<VectorValue>();

        internal IReadOnlyList<FloatValue> Floats => floats;
        internal IReadOnlyList<ColorValue> Colors => colors;
        internal IReadOnlyList<VectorValue> Vectors => vectors;

        public ClipCaps clipCaps => ClipCaps.Blending;

        public override double duration => 2;

        public override Playable CreatePlayable(
            PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<MaterialPropertyBehaviour>
                .Create(graph);
            playable.GetBehaviour().Pose = this;
            return playable;
        }

        [Serializable]
        internal struct FloatValue
        {
            public string name;
            public float value;
        }

        [Serializable]
        internal struct ColorValue
        {
            public string name;
            public Color value;
        }

        [Serializable]
        internal struct VectorValue
        {
            public string name;
            public Vector4 value;
        }
    }
}
