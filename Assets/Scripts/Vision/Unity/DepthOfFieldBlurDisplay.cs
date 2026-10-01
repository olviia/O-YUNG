using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Oyung.Vision.Unity
{
    // CLAUDE: adapter — implements IBlurDisplay by driving URP's Depth of Field on a post-processing Volume in the scene.
    // CLAUDE: Bokeh mode: one sharp plane at clearDistance, blur grows with distance from it (also slightly nearer than it).
    // CLAUDE: Single writer of the Vision Volume; edits its runtime copy so the profile asset stays untouched.
    internal class DepthOfFieldBlurDisplay : MonoBehaviour, IBlurDisplay
    {
        //gets reference to volume as serialized field
        [SerializeField] private Volume volume;

        // CLAUDE: look tuning, owned here — the core never sees these.
        [Tooltip("Overall 'how dramatic' knob, in mm. Longer = blurrier at the same f-stop.")]
        [SerializeField, Range(1f, 300f)] private float focalLength = 50f;
        [Tooltip("f-stop at strength 0 (near sharp). Higher = less blur.")]
        [SerializeField, Range(1f, 32f)] private float sharpFStop = 32f;
        [Tooltip("f-stop at strength 1 (newborn maximum). Lower = more blur.")]
        [SerializeField, Range(1f, 32f)] private float blurryFStop = 1f;

        private VolumeProfile runtimeProfile;
        private DepthOfField depthOfField;

        // sets strength and distance of this volume
        public void Show(float clearDistance, float strength)
        {
            var dof = DepthOfFieldOverride();
            strength = Mathf.Clamp01(strength);

            // CLAUDE: strength 0 = truly sharp and free: the override is switched off.
            dof.active = strength > 0f;
            dof.mode.Override(DepthOfFieldMode.Bokeh);
            dof.focusDistance.Override(Mathf.Max(0.01f, clearDistance));
            dof.focalLength.Override(focalLength);
            dof.aperture.Override(Mathf.Lerp(sharpFStop, blurryFStop, strength));
        }

        // CLAUDE: lazy, because the installer may call Show before this object's Awake.
        private DepthOfField DepthOfFieldOverride()
        {
            if (depthOfField != null) return depthOfField;

            runtimeProfile = volume.profile; // CLAUDE: Unity clones the asset here; we own the clone.
            if (!runtimeProfile.TryGet(out depthOfField))
                depthOfField = runtimeProfile.Add<DepthOfField>(overrides: true);
            return depthOfField;
        }

        private void OnDestroy()
        {
            if (runtimeProfile != null) Destroy(runtimeProfile);
        }
    }
}
