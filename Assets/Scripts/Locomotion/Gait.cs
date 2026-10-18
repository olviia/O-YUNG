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
    /// A planted foot is locked where it stands, whatever the body does
    /// (foot locking); a lifting foot plans its landing so the whole
    /// foot lands on flat ground (stride fitting). When the walk isn't
    /// stepping, as when the body turns on the spot, feet left behind
    /// step back under it on their own, one at a time.
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
            // Heel, toe and the two sides of the sole, in the foot
            // bone's space; and the same, flat, from the ankle in the
            // body's space, a little bigger: the spot a landing needs.
            [NonSerialized] public Vector3[] sole;
            [NonSerialized] public Vector3[] print;
            // How high the sole sits above the body's ground at rest;
            // the model may stand a little above it.
            [NonSerialized] public float restSole;
            // Where the walk put the ankle this frame, before fitting,
            // and the way it pointed the foot; the ankle in the body's
            // space last frame.
            [NonSerialized] public Vector3 ankle;
            [NonSerialized] public Vector3 walkFacing;
            [NonSerialized] public Vector3 lastLocal;
            // 0 planted .. 1 swinging: walkSwing as the walk moves the
            // foot, swing also counting a step of Gait's own.
            [NonSerialized] public float walkSwing;
            [NonSerialized] public float swing;
            // How far this swing has carried the foot, and how far the
            // last whole swing did: the rest is where it will land.
            [NonSerialized] public float swung;
            [NonSerialized] public float lastSwing = -1f;
            // Where the foot stands in the world while planted, and the
            // way it points; offset and twist take the walk's ankle
            // there. A swing carries them from what they were at lift
            // to shift and landTwist, which fit where it lands.
            [NonSerialized] public Vector3 plant;
            [NonSerialized] public Vector3 facing;
            [NonSerialized] public Vector3 offset;
            [NonSerialized] public float twist;
            [NonSerialized] public Vector3 liftOffset;
            [NonSerialized] public float liftTwist;
            [NonSerialized] public Vector3 shift;
            [NonSerialized] public float landTwist;
            [NonSerialized] public Vector3 landing;
            [NonSerialized] public Vector3 landFacing;
            [NonSerialized] public Quaternion landRotation;
            // A step Gait takes itself, when the body turns or shuffles
            // with no walk to carry the foot.
            [NonSerialized] public bool stepping;
            [NonSerialized] public float stepClock;

            public bool Planted => !stepping && walkSwing < 0.5f;
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
        [Tooltip("How far a swing carries the foot (m) until Gait has "
                 + "measured one; it then expects what the last did.")]
        [SerializeField] private float lookAhead = 0.25f;
        [Tooltip("Extra lift of a swinging foot over what it passes (m).")]
        [SerializeField] private float clearance = 0.04f;
        [Tooltip("How fast a swinging foot follows the ground (1/s).")]
        [SerializeField] private float swingFollow = 40f;
        [Tooltip("Most a foot lands farther or nearer than the walk "
                 + "would put it, to land whole on a step or slab (m).")]
        [SerializeField] private float maxShift = 0.08f;
        [Tooltip("Most the ground under a whole foot may rise and fall "
                 + "and still be a place to land (m).")]
        [SerializeField] private float flatness = 0.015f;
        [Tooltip("Room kept around the foot from an edge (m).")]
        [SerializeField] private float margin = 0.015f;
        [Tooltip("Most a leg shortens by bowing (m); for a higher foot "
                 + "beyond that, the body rises.")]
        [SerializeField] private float maxShorten = 0.05f;
        [Tooltip("Most a leg squashes or stretches past its bow, as a "
                 + "share of its length.")]
        [SerializeField] private float maxSquash = 0.45f;
        [Tooltip("Most extra bow a leg takes to reach a nearer foot "
                 + "(degrees at the hip).")]
        [SerializeField] private float maxBow = 70f;

        [Header("Turning")]
        [Tooltip("Below this share of the walk, feet step on their own "
                 + "when the body turns or shuffles away from them.")]
        [SerializeField] private float stepBelow = 0.25f;
        [Tooltip("How far a planted foot is left behind before it steps.")]
        [SerializeField] private float stepDistance = 0.06f;
        [Tooltip("How far a planted foot is turned before it steps "
                 + "(degrees).")]
        [SerializeField] private float stepTwist = 25f;
        [Tooltip("Most a planted foot stays turned; past it, it pivots "
                 + "on the spot (degrees).")]
        [SerializeField] private float maxTwist = 40f;
        [Tooltip("How long one step takes (s).")]
        [SerializeField] private float stepTime = 0.35f;

        private readonly RaycastHit[] hits = new RaycastHit[8];
        private Vector3 modelRest;
        private Vector3 lastPosition;
        private Vector3 lastForward;
        private Vector3 bodyVelocity;
        // Degrees per second the body turns, smoothed.
        private float turnRate;
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
            lastForward = Flat(transform.forward);
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

        // Heel, toe and sides: the back-, front- and side-most points of
        // the foot, at its sole, from the skin's vertices that follow
        // the foot. Read in the body's space, where up and forward are
        // known.
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
            // front, back and sides, at the height of the sole.
            var edge = new[]
            {
                Vector3.positiveInfinity, Vector3.negativeInfinity,
                Vector3.positiveInfinity, Vector3.negativeInfinity,
            };
            foreach (var p in points)
            {
                if (p.z < edge[0].z) edge[0] = p;
                if (p.z > edge[1].z) edge[1] = p;
                if (p.x < edge[2].x) edge[2] = p;
                if (p.x > edge[3].x) edge[3] = p;
            }
            leg.restSole = bottom;
            var ankle = transform.InverseTransformPoint(leg.foot.position);
            var grow = new[]
            {
                Vector3.back, Vector3.forward, Vector3.left, Vector3.right,
            };
            leg.sole = new Vector3[4];
            leg.print = new Vector3[4];
            for (var i = 0; i < 4; i++)
            {
                edge[i].y = bottom;
                leg.sole[i] = leg.foot.InverseTransformPoint(
                    transform.TransformPoint(edge[i]));
                var flat = edge[i] - ankle + grow[i] * margin;
                flat.y = 0f;
                leg.print[i] = flat;
            }
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
            bodyVelocity = moved / dt;

            var forward = Flat(transform.forward);
            var yaw = snap ? 0f
                : Vector3.SignedAngle(lastForward, forward, Vector3.up);
            lastForward = forward;
            turnRate = snap ? 0f
                : Mathf.Lerp(turnRate, yaw / dt, 1f - Mathf.Exp(-10f * dt));

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
            // Unseen, the Animator may skip posing the body to save time;
            // fitting last frame's fit again would compound it. Skip too,
            // and start the feet afresh when seen again.
            if (animator.cullingMode != AnimatorCullingMode.AlwaysAnimate
                && !skin.isVisible)
            {
                snap = true;
                return;
            }
            var k = snap ? 1f : 1f - Mathf.Exp(-follow * dt);
            var kBody = snap ? 1f : 1f - Mathf.Exp(-bodyFollow * dt);
            var y = transform.position.y;
            bodyY = Mathf.Lerp(bodyY, y, kBody);
            Probe(leftLeg, rightLeg, k, dt);
            Probe(rightLeg, leftLeg, k, dt);
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

            TryStep();
        }

        // Keeps a planted foot where it stands in the world, whatever
        // the body does, and carries a swinging one to where it lands;
        // then finds the ground it should stand on, as a smoothed world
        // height (a jump of the body doesn't move it). A planted foot
        // stands on the ground under it. A swinging foot rises to the
        // highest ground on its way to its landing, plus clearance, so
        // it steps over edges and onto steps instead of through them.
        private void Probe(Leg leg, Leg other, float k, float dt)
        {
            var a = leg.foot.position;
            leg.ankle = a;
            var local = transform.InverseTransformPoint(a);
            // The foot's own motion plus the body's travel, but not the
            // body's turning: a turn swings every foot around the body,
            // and that is no step.
            var velocity = snap ? Vector3.zero
                : bodyVelocity + transform.TransformVector(
                    (local - leg.lastLocal) / dt);
            leg.lastLocal = local;
            velocity.y = 0f;
            var facing = Facing(leg);
            leg.walkFacing = facing;
            if (snap)
            {
                leg.plant = a;
                leg.facing = facing;
                leg.offset = leg.shift = Vector3.zero;
                leg.twist = leg.landTwist = 0f;
                leg.stepping = false;
            }

            // The walk keeps a planted foot still in the world (its pace
            // matches the body's) and swings the other at about twice
            // the body's speed.
            // Barely walking, feet move with the blend between stand and
            // walk (both at once, no step): Gait's own steps take over.
            var pace = Mathf.Max(walkSpeed * speed01, 0.05f);
            var swinging = Mathf.InverseLerp(0.4f * pace, 1.2f * pace,
                    velocity.magnitude)
                * Mathf.InverseLerp(0.5f * stepBelow, stepBelow, speed01);
            // Nor does a walk swing both feet at once: one waits for the
            // other to land.
            if (leg.walkSwing < 0.5f && other.walkSwing >= 0.5f)
                swinging = 0f;
            var was = leg.walkSwing;
            leg.walkSwing = snap ? swinging
                : Mathf.MoveTowards(leg.walkSwing, swinging, 20f * dt);
            var landed = false;
            if (leg.walkSwing >= 0.5f)
            {
                if (was < 0.5f)
                {
                    // The walk takes over from a step of Gait's own.
                    leg.stepping = false;
                    var ahead = velocity.sqrMagnitude > 1e-6f
                        ? velocity.normalized
                        : Flat(transform.forward);
                    Lift(leg, a + ahead * Expected(leg), ahead,
                        transform.rotation, Vector3.zero, 0f);
                }
                leg.swung += velocity.magnitude * dt;
            }
            else if (was >= 0.5f)
            {
                leg.lastSwing = leg.swung;
                leg.swung = 0f;
                landed = true;
            }
            if (leg.stepping)
            {
                leg.stepClock += dt;
                if (leg.stepClock >= stepTime)
                {
                    leg.stepping = false;
                    landed = true;
                }
            }
            var stepSwing = leg.stepping
                ? Mathf.Sin(Mathf.PI * leg.stepClock / stepTime)
                : 0f;
            leg.swing = Mathf.Max(leg.walkSwing, stepSwing);

            if (leg.Planted)
            {
                if (landed)
                {
                    leg.plant = a + leg.offset;
                    leg.facing = Quaternion.AngleAxis(leg.twist, Vector3.up)
                        * facing;
                }
                leg.offset = Flat(leg.plant - a);
                leg.twist = Vector3.SignedAngle(facing, leg.facing,
                    Vector3.up);
                // Turned too far, a planted foot pivots on the spot.
                if (Mathf.Abs(leg.twist) > maxTwist)
                {
                    leg.twist = Mathf.Clamp(leg.twist, -maxTwist, maxTwist);
                    leg.facing = Quaternion.AngleAxis(leg.twist, Vector3.up)
                        * facing;
                }
            }
            else if (leg.stepping)
            {
                // A step of Gait's own goes from where the foot stood to
                // where it lands, in the world, easing in and out: the
                // body turning meanwhile doesn't drag it.
                var e = Mathf.SmoothStep(0f, 1f, Progress(leg));
                leg.offset = Flat(Vector3.Lerp(leg.plant, leg.landing, e) - a);
                leg.twist = Vector3.SignedAngle(facing,
                    Vector3.Slerp(leg.facing, leg.landFacing, e), Vector3.up);
            }
            else
            {
                // The walk carries a swinging foot; it only moves where
                // the swing ends.
                var e = Mathf.SmoothStep(0f, 1f, Progress(leg));
                leg.offset = Vector3.Lerp(leg.liftOffset, leg.shift, e);
                leg.twist = Mathf.Lerp(leg.liftTwist, leg.landTwist, e);
            }

            // The foot stands on the highest ground under its sole, so
            // a toe over the next step doesn't sink into it.
            var heel = leg.foot.TransformPoint(leg.sole[0]) + leg.offset;
            var toe = leg.foot.TransformPoint(leg.sole[1]) + leg.offset;
            var under = GroundAt((heel + toe) * 0.5f, out var normal);
            for (var i = 0; i < 4; i++)
                under = Mathf.Max(under, GroundAt(
                    leg.foot.TransformPoint(leg.sole[i]) + leg.offset,
                    out _));
            var highest = under;
            if (leg.swing > 0f)
            {
                var landToe = leg.landing + leg.landRotation * leg.print[1];
                for (var i = 1; i <= 4; i++)
                    highest = Mathf.Max(highest,
                        GroundAt(Vector3.Lerp(toe, landToe, i / 4f), out _));
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

        // How far a walk swing carries the foot, as far as Gait knows.
        private float Expected(Leg leg) =>
            leg.lastSwing > 0f ? leg.lastSwing : lookAhead;

        private float Progress(Leg leg) => leg.stepping
            ? leg.stepClock / stepTime
            : Mathf.Clamp01(leg.swung / Expected(leg));

        // A foot lifts: plans where it lands. The walk (or a step) would
        // put the ankle at landing, pointing as rotation; it lands
        // nearer or farther along ahead if that puts the whole foot on
        // flat ground, the way action games fit strides to stairs.
        private void Lift(Leg leg, Vector3 landing, Vector3 ahead,
            Quaternion rotation, Vector3 baseShift, float landTwist)
        {
            leg.liftOffset = leg.offset;
            leg.liftTwist = leg.twist;
            var along = FitLanding(leg, landing + baseShift, ahead, rotation);
            leg.shift = baseShift + ahead * along;
            leg.landTwist = landTwist;
            leg.landing = landing + leg.shift;
            leg.landRotation = rotation;
        }

        // The smallest move along ahead, up to maxShift, that lands the
        // foot's whole print on flat ground; none if there's no such.
        private float FitLanding(Leg leg, Vector3 ankle, Vector3 ahead,
            Quaternion rotation)
        {
            const float step = 0.01f;
            var tries = Mathf.FloorToInt(maxShift / step);
            for (var i = 0; i <= 2 * tries; i++)
            {
                // 0, +1, -1, +2, -2 ... steps: forward first, onto a step.
                var along = (i % 2 == 1 ? 1f : -1f) * ((i + 1) / 2) * step;
                if (IsFlat(leg, ankle + ahead * along, rotation))
                    return along;
            }
            return 0f;
        }

        private bool IsFlat(Leg leg, Vector3 ankle, Quaternion rotation)
        {
            float low = float.MaxValue, high = float.MinValue;
            for (var i = 0; i <= 4; i++)
            {
                var point = i < 4
                    ? leg.print[i]
                    : (leg.print[0] + leg.print[1]) * 0.5f;
                var y = GroundAt(ankle + rotation * point, out _);
                low = Mathf.Min(low, y);
                high = Mathf.Max(high, y);
            }
            return high - low <= flatness;
        }

        // When the walk isn't stepping, a foot left behind or turned
        // away by the body steps back under it: one foot at a time, the
        // one most out of place, while the other stands. A turning body
        // keeps turning during the step, so the foot lands ahead of the
        // turn, as people step into a turn.
        private void TryStep()
        {
            if (speed01 >= stepBelow || leftLeg.stepping || rightLeg.stepping
                || !leftLeg.Planted || !rightLeg.Planted)
                return;
            var needL = Need(leftLeg);
            var needR = Need(rightLeg);
            if (Mathf.Max(needL, needR) < 1f)
                return;
            var leg = needL >= needR ? leftLeg : rightLeg;

            var turn = turnRate * stepTime;
            var lead = Mathf.Clamp(0.5f * turn, -stepTwist, stepTwist);
            var end = Quaternion.AngleAxis(turn, Vector3.up);
            var land = Quaternion.AngleAxis(turn + lead, Vector3.up);
            var c = transform.position;
            var d = Flat(leg.ankle - c);
            leg.stepping = true;
            leg.stepClock = 0f;
            leg.landFacing = land * leg.walkFacing;
            Lift(leg, c + end * d, end * Flat(transform.forward),
                end * transform.rotation, land * d - end * d, lead);
        }

        // How out of place a planted foot is: 1 is time to step.
        private float Need(Leg leg) => Mathf.Max(
            (leg.offset - leg.shift).magnitude / stepDistance,
            Mathf.Abs(leg.twist - leg.landTwist) / stepTwist);

        // The way the walk points the foot, flat: heel to toe.
        private Vector3 Facing(Leg leg)
        {
            var along = Flat(leg.foot.TransformPoint(leg.sole[1])
                - leg.foot.TransformPoint(leg.sole[0]));
            return along.sqrMagnitude > 1e-6f
                ? along.normalized
                : Flat(transform.forward);
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

        // Moves the ankle up by lift (down if negative), and by the
        // leg's offset along the ground, from where the walk put it:
        // bows or straightens the leg to change its reach, turns the
        // whole leg to aim at the goal, tilts and twists the foot.
        private void Reach(Leg leg, float lift)
        {
            var footRotation = leg.foot.rotation;
            var hip = leg.root.position;
            var ankle = leg.foot.position;
            var goal = ankle + Vector3.up * lift + leg.offset;
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
            var twist = Quaternion.AngleAxis(leg.twist, Vector3.up);
            leg.foot.rotation = twist * tilt * footRotation;
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

        private static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);
    }
}
