using System;
using UnityEngine;

namespace Oyung.Locomotion
{
    /// <summary>
    /// Makes a body's authored walk fit how it really moves and the
    /// ground under it. Reads only how the body moved since last frame
    /// (Walker, a script, a teleport) and drives the Animator: Speed
    /// blends stand into walk, WalkRate plays the walk at the pace the
    /// body travels, so the feet don't slide.
    /// Then fits the feet to the ground the way action games do (a
    /// trace per foot, the foot offset by the ground's height, the body
    /// lowered to the lower foot, all smoothed), with one difference:
    /// our legs are soft bendy tubes, not two bones, so a leg reaches a
    /// nearer foot by bowing more and a farther one by straightening,
    /// solved directly each frame (no iterations, so no jitter).
    /// </summary>
    [DefaultExecutionOrder(10)]
    public class Gait : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int RateId = Animator.StringToHash("WalkRate");

        [Serializable]
        private class Leg
        {
            [Tooltip("First leg segment, at the hip.")]
            public Transform root;
            [Tooltip("The foot bone; its head is the ankle.")]
            public Transform foot;
            [NonSerialized] public Transform[] chain;
            [NonSerialized] public Quaternion[] rotations;
            [NonSerialized] public float groundY;
            [NonSerialized] public Vector3 normal = Vector3.up;
        }

        [Tooltip("The animated child; it sinks when a foot reaches down.")]
        [SerializeField] private Transform model;
        [SerializeField] private Animator animator;
        [SerializeField] private Leg leftLeg = new();
        [SerializeField] private Leg rightLeg = new();

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
        [Tooltip("How fast feet follow the ground (1/s).")]
        [SerializeField] private float follow = 14f;
        [Tooltip("How fast the body follows the height it walks at "
                 + "(1/s): steps and edges in the ground ease in.")]
        [SerializeField] private float bodyFollow = 10f;
        [Tooltip("Most a foot tilts to match a slope (degrees).")]
        [SerializeField] private float maxTilt = 30f;
        [Tooltip("Most extra bow a leg takes to reach a nearer foot "
                 + "(degrees at the hip).")]
        [SerializeField] private float maxBow = 70f;

        private readonly RaycastHit[] hits = new RaycastHit[8];
        private Vector3 modelRest;
        private Vector3 lastPosition;
        private float speed01;
        private float drop;
        // Smoothed world height of the body; the body itself snaps to
        // the ground it walks on.
        private float bodyY;
        private bool snap = true;

        private void Awake()
        {
            modelRest = model.localPosition;
            Prepare(leftLeg);
            Prepare(rightLeg);
        }

        private void OnEnable()
        {
            lastPosition = transform.position;
            snap = true;
        }

        private static void Prepare(Leg leg)
        {
            var count = 1;
            for (var t = leg.foot; t != leg.root; t = t.parent)
                count++;
            // Segments from the hip down, the foot not included.
            leg.chain = new Transform[count - 1];
            var i = count - 2;
            for (var t = leg.foot.parent; i >= 0; t = t.parent)
                leg.chain[i--] = t;
            leg.rotations = new Quaternion[leg.chain.Length];
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
                moved = Vector3.zero;
                snap = true;
            }
            moved.y = 0f;

            var relative = moved.magnitude / dt / walkSpeed;
            speed01 = Mathf.MoveTowards(speed01, Mathf.Min(relative, 1f),
                blendSpeed * dt);
            animator.SetFloat(SpeedId, speed01);
            animator.SetFloat(RateId, Mathf.Max(relative, minRate));
        }

        // After the Animator has posed this frame's walk.
        private void LateUpdate()
        {
            var dt = Time.deltaTime;
            if (dt <= 0f)
                return;
            var k = snap ? 1f : 1f - Mathf.Exp(-follow * dt);
            var kBody = snap ? 1f : 1f - Mathf.Exp(-bodyFollow * dt);
            snap = false;
            var y = transform.position.y;
            bodyY = Mathf.Lerp(bodyY, y, kBody);
            Probe(leftLeg, k);
            Probe(rightLeg, k);

            // Each foot's ground, measured from the smoothed body. The
            // body sinks by the lower foot's reach, so neither leg has
            // to stretch past its length.
            var riseL = leftLeg.groundY - bodyY;
            var riseR = rightLeg.groundY - bodyY;
            drop = Mathf.Lerp(drop, Mathf.Min(0f, Mathf.Min(riseL, riseR)), k);
            model.localPosition = modelRest + Vector3.up * (bodyY - y + drop);

            // The ankles moved with the body; each still needs its
            // ground's rise less that drop.
            Reach(leftLeg, riseL - drop);
            Reach(rightLeg, riseR - drop);
        }

        // Finds the ground under where the walk puts the foot, as a
        // smoothed world height: a jump of the body doesn't move it.
        private void Probe(Leg leg, float k)
        {
            var p = leg.foot.position;
            var from = new Vector3(p.x, transform.position.y + reach, p.z);
            var count = Physics.RaycastNonAlloc(from, Vector3.down, hits,
                2f * reach, ground, QueryTriggerInteraction.Ignore);

            var best = float.MaxValue;
            var groundY = transform.position.y;
            var normal = Vector3.up;
            for (var i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.transform.IsChildOf(transform) || hit.distance >= best)
                    continue;
                best = hit.distance;
                groundY = hit.point.y;
                normal = hit.normal;
            }

            leg.groundY = Mathf.Lerp(leg.groundY, groundY, k);
            var tilt = Vector3.RotateTowards(Vector3.up, normal,
                maxTilt * Mathf.Deg2Rad, 0f);
            leg.normal = Vector3.Slerp(leg.normal, tilt, k);
        }

        // Moves the ankle up by lift (down if negative) from where the
        // walk put it: bows or straightens the leg to change its reach,
        // turns the whole leg to aim at the goal, tilts the foot.
        private void Reach(Leg leg, float lift)
        {
            var footRotation = leg.foot.rotation;
            var hip = leg.root.position;
            var ankle = leg.foot.position;
            var goal = ankle + Vector3.up * lift;
            var chord = ankle - hip;
            var wanted = (goal - hip).magnitude;

            // Bow about the axis across the walking direction, turned so
            // the middle of the leg bulges forward like a soft knee.
            var axis = -transform.right;
            for (var i = 0; i < leg.chain.Length; i++)
                leg.rotations[i] = leg.chain[i].rotation;
            var bow = SolveBow(leg, axis, chord, wanted);
            Bend(leg, axis, bow);

            // Aim the bent leg at the goal; stretch if it can't reach.
            var reached = leg.foot.position - hip;
            leg.root.rotation =
                Quaternion.FromToRotation(reached, goal - hip)
                * leg.root.rotation;
            if (reached.magnitude > 1e-4f && wanted > reached.magnitude)
            {
                var s = wanted / reached.magnitude;
                leg.root.localScale *= s;
                leg.foot.localScale /= s;
            }

            var tilt = Quaternion.FromToRotation(Vector3.up, leg.normal);
            leg.foot.rotation = tilt * footRotation;
        }

        // Bow angle (degrees at the hip) that shortens the chord between
        // hip and ankle to wanted. A farther foot needs no bow: the body
        // sank for the lower foot, the rest is a slight stretch.
        // Bisection on the segment vectors only, so it's exact and the
        // same every frame.
        private float SolveBow(Leg leg, Vector3 axis, Vector3 chord,
            float wanted)
        {
            if (wanted >= chord.magnitude - 1e-4f)
                return 0f;
            float lo = 0f, hi = maxBow;
            for (var it = 0; it < 16; it++)
            {
                var mid = 0.5f * (lo + hi);
                // More bow, shorter chord.
                if (ChordAt(leg, axis, mid) > wanted)
                    lo = mid;
                else
                    hi = mid;
            }
            return 0.5f * (lo + hi);
        }

        private float ChordAt(Leg leg, Vector3 axis, float bow)
        {
            var sum = Vector3.zero;
            var n = leg.chain.Length;
            for (var i = 0; i < n; i++)
            {
                var next = i + 1 < n ? leg.chain[i + 1] : leg.foot;
                var segment = next.position - leg.chain[i].position;
                sum += Quaternion.AngleAxis(Turn(bow, i, n), axis) * segment;
            }
            return sum.magnitude;
        }

        // Each segment turns a little less than the one above it, from
        // +bow at the hip to -bow at the ankle: an even arc.
        private static float Turn(float bow, int i, int n) =>
            n > 1 ? bow * (1f - 2f * i / (n - 1)) : 0f;

        private static void Bend(Leg leg, Vector3 axis, float bow)
        {
            var n = leg.chain.Length;
            for (var i = 0; i < n; i++)
                leg.chain[i].rotation =
                    Quaternion.AngleAxis(Turn(bow, i, n), axis)
                    * leg.rotations[i];
        }
    }
}
