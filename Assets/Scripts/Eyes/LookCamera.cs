using Oyung.Input;
using UnityEngine;

namespace Oyung.Eyes
{
    /// <summary>
    /// The baby's eyes. Turns strictly with LookDelta,
    /// only within the eyes' range. No sway, no
    /// smoothing (motion sickness). The rotation the
    /// object has in the scene is the resting gaze.
    /// </summary>
    public class LookCamera : MonoBehaviour
    {
        [SerializeField] private float maxLeftRight = 60f;
        [SerializeField] private float maxUp = 40f;
        [SerializeField] private float maxDown = 30f;

        private IGameInput input;
        private Quaternion restingGaze;
        private float leftRight;
        private float upDown;

        /// <summary>Given by EyesInstaller.</summary>
        /// <param name="input">Where the look comes from.</param>
        public void Init(IGameInput input)
        {
            this.input = input;
        }

        private void Awake()
        {
            restingGaze = transform.localRotation;
        }

        private void Update()
        {
            if (input == null)
                return;

            var delta = input.LookDelta;
            leftRight = Mathf.Clamp(leftRight + delta.X, -maxLeftRight, maxLeftRight);
            upDown = Mathf.Clamp(upDown + delta.Y, -maxDown, maxUp);

            // Unity's X rotation is positive downwards,
            // so looking up is a negative angle.
            transform.localRotation = restingGaze * Quaternion.Euler(-upDown, leftRight, 0f);
        }
    }
}
