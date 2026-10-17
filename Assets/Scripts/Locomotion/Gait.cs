using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Oyung.Locomotion
{
    /// <summary>
    /// Procedural legs: steps the feet under a moving body
    /// and lets it bob, waddle, lean and swing its arms.
    /// Reads only how the body moved since last frame, so
    /// anything that moves the body gets legs (Walker, a
    /// script, a teleport). Moves IK targets; the Animation
    /// Rigging constraints on the model bend the limbs.
    /// Where the targets sit in the prefab is the resting
    /// stance: feet and hands return there when still.
    /// </summary>
    [DefaultExecutionOrder(10)]
    public class Gait : MonoBehaviour
    {
        [Serializable]
        private class Limb
        {
            public Transform target;
            [Tooltip("+1 right, -1 left.")]
            public float side = 1f;
            [NonSerialized] public Vector3 homePosition;
            [NonSerialized] public Quaternion homeRotation;
            [NonSerialized] public Vector3 planted;
            [NonSerialized] public Vector3 from;
            [NonSerialized] public float t;
            [NonSerialized] public bool stepping;
        }

        [Tooltip("The animated child; bob, waddle and lean move it.")]
        [SerializeField] private Transform model;
        [SerializeField] private Limb leftFoot = new() { side = -1f };
        [SerializeField] private Limb rightFoot = new() { side = 1f };
        [SerializeField] private Limb leftHand = new() { side = -1f };
        [SerializeField] private Limb rightHand = new() { side = 1f };
        [Tooltip("Fades out when still, so poses own the arms.")]
        [SerializeField] private Rig armsRig;

        [Header("Steps")]
        [Tooltip("A foot this far from its spot steps.")]
        [SerializeField] private float stepDistance = 0.16f;
        [Tooltip("Same, when the body is nearly still.")]
        [SerializeField] private float settleDistance = 0.03f;
        [SerializeField] private float stepDuration = 0.42f;
        [SerializeField] private float stepHeight = 0.07f;
        [Tooltip("Speed that counts as a full walk (m/s).")]
        [SerializeField] private float fullSpeed = 0.7f;
        [Tooltip("A jump bigger than this resets the feet.")]
        [SerializeField] private float teleportDistance = 0.5f;

        [Header("Body")]
        [SerializeField] private float bob = 0.03f;
        [Tooltip("Degrees of side-to-side roll per step.")]
        [SerializeField] private float waddle = 5f;
        [Tooltip("Degrees of forward lean at full speed.")]
        [SerializeField] private float lean = 4f;
        [SerializeField] private float armSwing = 0.07f;
        [Tooltip("How fast walk-ness follows speed changes.")]
        [SerializeField] private float blendSpeed = 3f;

        private Limb[] feet;
        private Limb[] hands;
        private Vector3 modelRestPosition;
        private Quaternion modelRestRotation;
        private Vector3 lastPosition;
        private float walkness;

        private void Awake()
        {
            modelRestPosition = model.localPosition;
            modelRestRotation = model.localRotation;
            feet = new[] { leftFoot, rightFoot };
            hands = new[] { leftHand, rightHand };
            CaptureHome(leftFoot, transform);
            CaptureHome(rightFoot, transform);
            CaptureHome(leftHand, model);
            CaptureHome(rightHand, model);
            ResetFeet();
        }

        private void OnEnable()
        {
            lastPosition = transform.position;
            ResetFeet();
        }

        /// <summary>Plants both feet on their spots at once.
        /// Called on its own after a teleport.</summary>
        public void ResetFeet()
        {
            foreach (var foot in feet)
            {
                foot.planted = transform.TransformPoint(foot.homePosition);
                foot.stepping = false;
            }
            lastPosition = transform.position;
        }

        private static void CaptureHome(Limb limb, Transform space)
        {
            limb.homePosition = space.InverseTransformPoint(limb.target.position);
            limb.homeRotation =
                Quaternion.Inverse(space.rotation) * limb.target.rotation;
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            if (dt <= 0f)
                return;

            var moved = transform.position - lastPosition;
            lastPosition = transform.position;
            if (moved.magnitude > teleportDistance)
            {
                ResetFeet();
                return;
            }

            var velocity = moved / dt;
            velocity.y = 0f;
            var target01 = Mathf.Clamp01(velocity.magnitude / fullSpeed);
            walkness = Mathf.MoveTowards(walkness, target01, blendSpeed * dt);

            StepFeet(velocity, dt);
            MoveBody();
            SwingArms();
        }

        private void StepFeet(Vector3 velocity, float dt)
        {
            // Lands a little ahead, so the foot isn't left
            // behind by the time it's down.
            var lead = velocity * (stepDuration * 0.5f);

            // One foot at a time: start a step only when
            // both are down, with the one furthest behind.
            if (!leftFoot.stepping && !rightFoot.stepping)
            {
                var threshold = walkness > 0.05f ? stepDistance : settleDistance;
                var dl = Behind(leftFoot);
                var dr = Behind(rightFoot);
                var foot = dl >= dr ? leftFoot : rightFoot;
                if (Mathf.Max(dl, dr) > threshold)
                {
                    foot.stepping = true;
                    foot.from = foot.planted;
                    foot.t = 0f;
                }
            }

            foreach (var foot in feet)
            {
                if (foot.stepping)
                {
                    foot.t = Mathf.Min(1f, foot.t + dt / stepDuration);
                    var to = transform.TransformPoint(foot.homePosition) + lead;
                    var eased = Mathf.SmoothStep(0f, 1f, foot.t);
                    foot.planted = Vector3.Lerp(foot.from, to, eased);
                    if (foot.t >= 1f)
                        foot.stepping = false;
                }

                var lift = foot.stepping
                    ? Mathf.Sin(foot.t * Mathf.PI) * stepHeight
                    : 0f;
                foot.target.SetPositionAndRotation(
                    foot.planted + Vector3.up * lift,
                    transform.rotation * foot.homeRotation);
            }
        }

        private float Behind(Limb foot) =>
            Vector3.Distance(foot.planted, transform.TransformPoint(foot.homePosition));

        // Phase of the step in flight (0..1..0) and its side.
        private (float arc, float side) Swing()
        {
            var foot = leftFoot.stepping ? leftFoot
                : rightFoot.stepping ? rightFoot : null;
            return foot == null ? (0f, 0f) : (Mathf.Sin(foot.t * Mathf.PI), foot.side);
        }

        private void MoveBody()
        {
            var (arc, side) = Swing();
            var up = arc * bob * walkness;
            // Rolls over the foot that stays down.
            var roll = -side * arc * waddle * walkness;
            var pitch = lean * walkness;
            model.localPosition = modelRestPosition + Vector3.up * up;
            model.localRotation =
                modelRestRotation * Quaternion.Euler(pitch, 0f, roll);
        }

        private void SwingArms()
        {
            var (arc, side) = Swing();
            foreach (var hand in hands)
            {
                // The hand opposite the stepping foot goes forward.
                var forward = side == 0f ? 0f : -side * hand.side * arc;
                var local = hand.homePosition
                    + Vector3.forward * (forward * armSwing * walkness);
                hand.target.SetPositionAndRotation(
                    model.TransformPoint(local),
                    model.rotation * hand.homeRotation);
            }
            if (armsRig != null)
                armsRig.weight = walkness;
        }
    }
}
