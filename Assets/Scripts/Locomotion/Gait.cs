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
            [NonSerialized] public Vector3 lastFoot;
            // Heel and toe of the sole, in the foot bone's space.
            [NonSerialized] public Vector3 heel;
            [NonSerialized] public Vector3 toe;
            // How high the sole sits above the body's ground at rest;
            // the model may stand a little above it.
            [NonSerialized] public float restSole;
            // 0 planted .. 1 swinging, read from how the walk moves it.
            [NonSerialized] public float swing;
            // How far this swing has carried the foot, and how far the
            // last whole swing did: the rest is where it will land.
            [NonSerialized] public float swung;
            [NonSerialized] public float lastSwing = -1f;
        }

        [Tooltip("The animated child; it sinks or rises to fit the feet.")]
        [SerializeField] private Transform model;
        [SerializeField] private Animator animator;
        [Tooltip("The skin; each foot finds its heel and toe in it.")]
        [SerializeField] private SkinnedMeshRenderer skin;
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
        [Header("Stepping")]
        [Tooltip("Most a swinging foot looks ahead for ground to lift "
                 + "over and land on (m); it looks only as far as it "
                 + "will land, learnt from its last swing.")]
        [SerializeField] private float lookAhead = 0.25f;
        [Tooltip("Extra lift of a swinging foot over what it passes (m).")]
        [SerializeField] private float clearance = 0.04f;
        [Tooltip("How fast a swinging foot follows the ground (1/s).")]
        [SerializeField] private float swingFollow = 40f;
        [Tooltip("Most a leg shortens by bowing (m); for a higher foot "
                 + "beyond that, the body rises.")]
        [SerializeField] private float maxShorten = 0.05f;
        [Tooltip("Most a leg squashes or stretches past its bow, as a "
                 + "share of its length.")]
        [SerializeField] private float maxSquash = 0.45f;
        [Tooltip("Most extra bow a leg takes to reach a nearer foot "
                 + "(degrees at the hip).")]
        [SerializeField] private float maxBow = 70f;

        private readonly RaycastHit[] hits = new RaycastHit[8];
        private Vector3 modelRest;
        private Vector3 lastPosition;
        private float speed01;
        // How far the body sits below (or above) its height to fit
        // the feet.
        private float bodyOffset;
        // Smoothed world height of the body; the body itself snaps to
        // the ground it walks on.
        private float bodyY;
        private bool snap = true;

        private void Awake()
        {
            modelRest = model.localPosition;
            Prepare(leftLeg);
            Prepare(rightLeg);
            FindSole(leftLeg);
            FindSole(rightLeg);
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

        // Heel and toe: the back- and front-most points of the foot, at
        // its sole, from the skin's vertices that follow the foot.
        // Read in the body's space, where up and forward are known.
        private void FindSole(Leg leg)
        {
            var mesh = skin.sharedMesh;
            var bone = Array.IndexOf(skin.bones, leg.foot);
            var bind = mesh.bindposes[bone];
            var weights = mesh.boneWeights;
            var vertices = mesh.vertices;
            var points = new System.Collections.Generic.List<Vector3>();
            for (var i = 0; i < vertices.Length; i++)
            {
                if (Weight(weights[i], bone) < 0.5f)
                    continue;
                var local = bind.MultiplyPoint3x4(vertices[i]);
                points.Add(transform.InverseTransformPoint(
                    leg.foot.TransformPoint(local)));
            }
            var bottom = float.MaxValue;
            foreach (var p in points)
                bottom = Mathf.Min(bottom, p.y);
            // A round foot bulges past its sole: take the whole foot's
            // front and back, at the height of the sole.
            Vector3 heel = Vector3.positiveInfinity, toe = Vector3.negativeInfinity;
            foreach (var p in points)
            {
                if (p.z < heel.z) heel = p;
                if (p.z > toe.z) toe = p;
            }
            heel.y = toe.y = bottom;
            leg.restSole = bottom;
            leg.heel = leg.foot.InverseTransformPoint(transform.TransformPoint(heel));
            leg.toe = leg.foot.InverseTransformPoint(transform.TransformPoint(toe));
        }

        private static float Weight(BoneWeight w, int bone) =>
            (w.boneIndex0 == bone ? w.weight0 : 0f)
            + (w.boneIndex1 == bone ? w.weight1 : 0f)
            + (w.boneIndex2 == bone ? w.weight2 : 0f)
            + (w.boneIndex3 == bone ? w.weight3 : 0f);

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
            var y = transform.position.y;
            bodyY = Mathf.Lerp(bodyY, y, kBody);
            Probe(leftLeg, k, dt);
            Probe(rightLeg, k, dt);
            snap = false;

            // Each foot's ground, measured from the smoothed body. The
            // body sinks for the lower foot, so that leg needn't stretch,
            // and rises for the higher one when it's more than a leg can
            // shorten by bowing. If both can't be met, the higher foot
            // wins: a stretched leg looks fine, a sunk foot doesn't.
            var riseL = leftLeg.groundY - bodyY;
            var riseR = rightLeg.groundY - bodyY;
            var lower = Mathf.Min(riseL, riseR);
            // A swinging foot clears what it passes by bending its own
            // leg; only planted feet push the body up.
            var higher = Mathf.Max(Mathf.Lerp(riseL, lower, leftLeg.swing),
                Mathf.Lerp(riseR, lower, rightLeg.swing));
            var wanted = Mathf.Max(Mathf.Min(0f, lower), higher - maxShorten);
            bodyOffset = Mathf.Lerp(bodyOffset, wanted, k);
            model.localPosition = modelRest + Vector3.up * (bodyY - y + bodyOffset);

            // The ankles moved with the body; each still needs its
            // ground's rise less the body's offset, and less the sole's
            // rest height, so the sole touches the ground.
            Reach(leftLeg, riseL - leftLeg.restSole - bodyOffset);
            Reach(rightLeg, riseR - rightLeg.restSole - bodyOffset);
        }

        // Finds the ground a foot should stand on, as a smoothed world
        // height (a jump of the body doesn't move it). A planted foot
        // stands on the ground under it. A swinging foot rises to the
        // highest ground on its way ahead, plus clearance, so it steps
        // over edges and onto steps instead of through them, and lands
        // on what it found there.
        private void Probe(Leg leg, float k, float dt)
        {
            var p = leg.foot.position;
            var velocity = snap ? Vector3.zero : (p - leg.lastFoot) / dt;
            leg.lastFoot = p;
            velocity.y = 0f;

            // The walk keeps a planted foot still in the world (its pace
            // matches the body's) and swings the other at about twice
            // the body's speed.
            var pace = Mathf.Max(walkSpeed * speed01, 0.05f);
            var swinging = Mathf.InverseLerp(0.4f * pace, 1.2f * pace,
                velocity.magnitude);
            var was = leg.swing;
            leg.swing = snap ? swinging
                : Mathf.MoveTowards(leg.swing, swinging, 20f * dt);
            if (leg.swing >= 0.5f)
                leg.swung += velocity.magnitude * dt;
            else if (was >= 0.5f)
            {
                leg.lastSwing = leg.swung;
                leg.swung = 0f;
            }

            // The foot stands on the highest ground under its sole, so
            // a toe over the next step doesn't sink into it.
            var heel = leg.foot.TransformPoint(leg.heel);
            var toe = leg.foot.TransformPoint(leg.toe);
            var under = Mathf.Max(GroundAt((heel + toe) * 0.5f, out var normal),
                Mathf.Max(GroundAt(heel, out _), GroundAt(toe, out _)));
            var highest = under;
            if (leg.swing > 0f)
            {
                var ahead = velocity.sqrMagnitude > 1e-6f
                    ? velocity.normalized
                    : transform.forward;
                var left = leg.lastSwing < 0f
                    ? lookAhead
                    : Mathf.Clamp(leg.lastSwing - leg.swung, 0f, lookAhead);
                for (var i = 1; i <= 4; i++)
                {
                    var q = toe + ahead * (left * i / 4f);
                    highest = Mathf.Max(highest, GroundAt(q, out _));
                }
            }
            var target = under
                + (highest - under + clearance) * leg.swing;
            var rate = Mathf.Lerp(k, 1f - Mathf.Exp(-swingFollow * dt),
                leg.swing);
            leg.groundY = snap ? target : Mathf.Lerp(leg.groundY, target, rate);

            var tilt = Vector3.RotateTowards(Vector3.up, normal,
                maxTilt * Mathf.Deg2Rad, 0f);
            tilt = Vector3.Slerp(tilt, Vector3.up, leg.swing);
            leg.normal = Vector3.Slerp(leg.normal, tilt, k);
        }

        // Height of the ground under a point, looking within reach of
        // the body's height; the body's own height if there's none.
        private float GroundAt(Vector3 point, out Vector3 normal)
        {
            var from = new Vector3(point.x, transform.position.y + reach,
                point.z);
            var count = Physics.RaycastNonAlloc(from, Vector3.down, hits,
                2f * reach, ground, QueryTriggerInteraction.Ignore);
            var best = float.MaxValue;
            var y = transform.position.y;
            normal = Vector3.up;
            for (var i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.transform.IsChildOf(transform) || hit.distance >= best)
                    continue;
                best = hit.distance;
                y = hit.point.y;
                normal = hit.normal;
            }
            return y;
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

            // Aim the bent leg at the goal. What bowing couldn't do, the
            // soft leg does by squashing or stretching (the foot keeps
            // its size).
            var reached = leg.foot.position - hip;
            leg.root.rotation =
                Quaternion.FromToRotation(reached, goal - hip)
                * leg.root.rotation;
            if (reached.magnitude > 1e-4f
                && Mathf.Abs(wanted - reached.magnitude) > 1e-4f)
            {
                var s = Mathf.Clamp(wanted / reached.magnitude,
                    1f - maxSquash, 1f + maxSquash);
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
