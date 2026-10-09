using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace Oyung.Cutscenes.Unity
{
    /// <summary>
    /// Every frame: blends the active poses by their weights and writes
    /// the result into the bound image's material. Works on a private
    /// copy of the material (UI materials are shared) and puts the
    /// original back when the timeline stops.
    /// </summary>
    public sealed class MaterialPropertyMixer : PlayableBehaviour
    {
        internal string TimeProperty { get; set; }

        private Graphic _graphic;
        private Material _original;
        private Material _copy;

        // CLAUDE: reused every frame: no garbage while playing.
        private readonly Dictionary<string, (float sum, float weight)>
            _floats = new Dictionary<string, (float, float)>();
        private readonly Dictionary<string, (Vector4 sum, float weight)>
            _colors = new Dictionary<string, (Vector4, float)>();
        private readonly Dictionary<string, (Vector4 sum, float weight)>
            _vectors = new Dictionary<string, (Vector4, float)>();

        public override void ProcessFrame(
            Playable playable, FrameData info, object playerData)
        {
            if (!(playerData is Graphic graphic)) return;
            var material = CopyFor(graphic);
            if (material == null) return;

            Gather(playable);

            // CLAUDE: where poses cover less than 100%, the rest comes
            // CLAUDE: from the original material, so a pose fades in
            // CLAUDE: from the material's own values.
            foreach (var pair in _floats)
            {
                var (sum, weight) = pair.Value;
                material.SetFloat(pair.Key,
                    sum + _original.GetFloat(pair.Key) * (1 - weight));
            }
            foreach (var pair in _colors)
            {
                var (sum, weight) = pair.Value;
                material.SetColor(pair.Key, (Color)(sum +
                    (Vector4)_original.GetColor(pair.Key) * (1 - weight)));
            }
            foreach (var pair in _vectors)
            {
                var (sum, weight) = pair.Value;
                material.SetVector(pair.Key,
                    sum + _original.GetVector(pair.Key) * (1 - weight));
            }

            if (!string.IsNullOrEmpty(TimeProperty))
                material.SetFloat(TimeProperty, (float)playable.GetTime());
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            if (_graphic != null) _graphic.material = _original;
            if (_copy != null)
            {
                // CLAUDE: Timeline preview runs in edit mode too.
                if (Application.isPlaying) Object.Destroy(_copy);
                else Object.DestroyImmediate(_copy);
            }
            _graphic = null;
            _copy = null;
        }

        private Material CopyFor(Graphic graphic)
        {
            if (_graphic == graphic) return _copy;
            _graphic = graphic;
            _original = graphic.material;
            if (_original == null) return null;
            _copy = new Material(_original)
            {
                name = _original.name + " (Timeline)",
                hideFlags = HideFlags.DontSave
            };
            graphic.material = _copy;
            return _copy;
        }

        private void Gather(Playable playable)
        {
            _floats.Clear();
            _colors.Clear();
            _vectors.Clear();
            for (int i = 0; i < playable.GetInputCount(); i++)
            {
                float w = playable.GetInputWeight(i);
                if (w <= 0) continue;
                var pose = ((ScriptPlayable<MaterialPropertyBehaviour>)
                    playable.GetInput(i)).GetBehaviour().Pose;
                if (pose == null) continue;

                foreach (var v in pose.Floats)
                {
                    _floats.TryGetValue(v.name, out var acc);
                    _floats[v.name] = (acc.sum + v.value * w,
                        acc.weight + w);
                }
                foreach (var v in pose.Colors)
                {
                    _colors.TryGetValue(v.name, out var acc);
                    _colors[v.name] = (acc.sum + (Vector4)v.value * w,
                        acc.weight + w);
                }
                foreach (var v in pose.Vectors)
                {
                    _vectors.TryGetValue(v.name, out var acc);
                    _vectors[v.name] = (acc.sum + v.value * w,
                        acc.weight + w);
                }
            }
        }
    }
}
