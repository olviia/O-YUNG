using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Oyung.Locomotion
{
    /// <summary>
    /// Makes a body's authored walk fit how it really moves and the
    /// ground under it. Reads only how the body moved since last frame
    /// (Walker, a script, a teleport) and drives the Animator: Speed
    /// blends stand into walk, WalkRate plays the walk at the pace the
    /// body travels, so the feet don't slide. Then fits each foot to
    /// the ground under it: lifted or lowered onto it and tilted to its
    /// slope, and the body sinks when a foot has to reach down. The
    /// walk animates control bones (one per foot) that say where each
    /// foot means to be; the leg IK reaches for a target under each.
    /// </summary>
    [DefaultExecutionOrder(10)]
    public class Gait : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int RateId = Animator.StringToHash("WalkRate");

        [Serializable]
        private class Foot
        {
            [Tooltip("Animated control bone: where the walk puts the foot.")]
            public Transform animated;
            [Tooltip("Leg IK target, a child of the animated bone.")]
            public Transform target;
            [NonSerialized] public float rise;
            [NonSerialized] public Vector3 normal = Vector3.up;
        }

        [Tooltip("The animated child; it sinks when a foot reaches down.")]
        [SerializeField] private Transform model;
        [SerializeField] private Animator animator;
        [SerializeField] private Foot leftFoot = new();
        [SerializeField] private Foot rightFoot = new();

        [Header("Walk")]
        [Tooltip("Speed the walk animation travels at (m/s), from its "
                 + "stride and step time. Walker should use the same.")]
        [SerializeField] private float walkSpeed = 0.3f;
        [Tooltip("Slowest playback while speeding up or slowing down, "
                 + "so the walk doesn't crawl in slow motion.")]
        [SerializeField] private float minRate = 0.6f;
        [Tooltip("How fast the walk blend follows speed changes.")]
        [SerializeField] private float blendSpeed = 4f;
        [Tooltip("A jump bigger than this is a teleport, not a walk.")]
        [SerializeField] private float teleportDistance = 0.5f;

        [Header("Ground")]
        [SerializeField] private LayerMask ground = ~0;
        [Tooltip("How far above and below the body a foot looks for "
                 + "ground: the tallest step it takes up or down.")]
        [SerializeField] private float reach = 0.35f;
        [Tooltip("How fast feet and body follow the ground (1/s).")]
        [SerializeField] private float follow = 14f;
        [Tooltip("Most a foot tilts to match a slope (degrees).")]
        [SerializeField] private float maxTilt = 30f;

        private readonly RaycastHit[] hits = new RaycastHit[8];
        private Vector3 modelRest;
        private Vector3 lastPosition;
        private float speed01;
        private float drop;

        private void Awake()
        {
            modelRest = model.localPosition;
        }

        private void OnEnable()
        {
            lastPosition = transform.position;
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            if (dt <= 0f)
                return;
            DriveWalk(dt);
            FitGround(dt);
        }

        private void DriveWalk(float dt)
        {
            var moved = transform.position - lastPosition;
            lastPosition = transform.position;
            if (moved.magnitude > teleportDistance)
                moved = Vector3.zero;
            moved.y = 0f;

            var relative = moved.magnitude / dt / walkSpeed;
            speed01 = Mathf.MoveTowards(speed01, Mathf.Min(relative, 1f),
                blendSpeed * dt);
            animator.SetFloat(SpeedId, speed01);
            animator.SetFloat(RateId, Mathf.Max(relative, minRate));
        }

        private void FitGround(float dt)
        {
            var k = 1f - Mathf.Exp(-follow * dt);
            Probe(leftFoot, k);
            Probe(rightFoot, k);

            // The body sinks by the lower foot's reach, so neither leg
            // has to stretch past its length.
            var wanted = Mathf.Min(0f, Mathf.Min(leftFoot.rise, rightFoot.rise));
            drop = Mathf.Lerp(drop, wanted, k);
            model.localPosition = modelRest + Vector3.up * drop;

            Place(leftFoot);
            Place(rightFoot);
        }

        // Finds the ground under where the walk last put the foot.
        private void Probe(Foot foot, float k)
        {
            var p = foot.animated.position;
            var from = new Vector3(p.x, transform.position.y + reach, p.z);
            var count = Physics.RaycastNonAlloc(from, Vector3.down, hits,
                2f * reach, ground, QueryTriggerInteraction.Ignore);

            var best = float.MaxValue;
            var rise = 0f;
            var normal = Vector3.up;
            for (var i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.transform.IsChildOf(transform) || hit.distance >= best)
                    continue;
                best = hit.distance;
                rise = hit.point.y - transform.position.y;
                normal = hit.normal;
            }

            foot.rise = Mathf.Lerp(foot.rise, rise, k);
            var tilt = Vector3.RotateTowards(Vector3.up, normal,
                maxTilt * Mathf.Deg2Rad, 0f);
            foot.normal = Vector3.Slerp(foot.normal, tilt, k);
        }

        // Moves the IK target off the animated foot by the ground's
        // rise (less what the body already sank) and tilts it to the
        // slope. Set in the animated bone's space, so it rides on this
        // frame's animation.
        private void Place(Foot foot)
        {
            var bone = foot.animated;
            var offset = Vector3.up * (foot.rise - drop);
            foot.target.localPosition = bone.InverseTransformVector(offset);
            var tilt = Quaternion.FromToRotation(Vector3.up, foot.normal);
            foot.target.localRotation =
                Quaternion.Inverse(bone.rotation) * tilt * bone.rotation;
        }
    }
}
