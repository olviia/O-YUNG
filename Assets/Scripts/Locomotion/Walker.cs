using System;
using UnityEngine;
using UnityEngine.AI;

namespace Oyung.Locomotion
{
    /// <summary>
    /// Takes its body to a destination over the NavMesh at
    /// a calm pace: turns towards where it goes, slows down
    /// before arriving, optionally turns to a final facing,
    /// then reports Arrived. Moves only the body's transform;
    /// legs are Gait's job, which reads the motion.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class Walker : MonoBehaviour
    {
        [SerializeField] private float speed = 0.7f;
        [SerializeField] private float acceleration = 1.2f;
        [Tooltip("Degrees per second.")]
        [SerializeField] private float turnSpeed = 140f;
        [Tooltip("How close counts as there, in metres.")]
        [SerializeField] private float arriveDistance = 0.02f;
        [Tooltip("How far up or down it looks for the NavMesh to stand "
                 + "on. Exact on a surface built with a height mesh.")]
        [SerializeField] private float groundSnap = 0.5f;

        // Native object: Unity forbids creating it in a
        // field initializer, so Awake does.
        private NavMeshPath path;
        private Vector3[] corners = Array.Empty<Vector3>();
        private int nextCorner;
        private Quaternion? facing;
        private float currentSpeed;
        private bool walking;

        /// <summary>Raised once the body stands at the
        /// destination, facing as asked.</summary>
        public event Action Arrived;

        public bool IsWalking => walking;

        private void Awake()
        {
            path = new NavMeshPath();
        }

        /// <summary>
        /// Starts walking; replaces any walk in progress.
        /// Throws if the NavMesh has no path there.
        /// </summary>
        /// <param name="destination">Where to stand.</param>
        /// <param name="finalFacing">Rotation to turn to on
        /// arrival; null keeps the walking direction.</param>
        public void WalkTo(Vector3 destination, Quaternion? finalFacing = null)
        {
            var found = NavMesh.CalculatePath(
                transform.position, destination, NavMesh.AllAreas, path);
            if (!found || path.status == NavMeshPathStatus.PathInvalid)
                throw new InvalidOperationException(
                    $"{name}: no NavMesh path from {transform.position} " +
                    $"to {destination}. Is the NavMesh baked?");

            corners = path.corners;
            nextCorner = 1;
            facing = finalFacing;
            walking = true;
        }

        /// <summary>Puts the body where the current walk
        /// would end, at once (skips, loading a save).
        /// Gait sees the jump and resets the feet.</summary>
        public void FinishNow()
        {
            if (!walking)
                return;
            transform.position = corners[^1];
            if (facing.HasValue)
                transform.rotation = facing.Value;
            Arrive();
        }

        /// <summary>Stops where it is, without Arrived.</summary>
        public void Stop()
        {
            walking = false;
            currentSpeed = 0f;
        }

        private void Update()
        {
            if (!walking)
                return;

            if (nextCorner < corners.Length)
                MoveAlongPath(Time.deltaTime);
            else
                TurnToFacing(Time.deltaTime);
        }

        private void MoveAlongPath(float dt)
        {
            var pos = transform.position;
            var target = corners[nextCorner];
            var toTarget = Flat(target - pos);
            var distance = toTarget.magnitude;
            var isLast = nextCorner == corners.Length - 1;

            if (distance <= arriveDistance || (!isLast && distance < 0.1f))
            {
                nextCorner++;
                if (nextCorner >= corners.Length)
                    currentSpeed = 0f;
                return;
            }

            var dir = toTarget / distance;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, Quaternion.LookRotation(dir),
                turnSpeed * dt);

            // Slows down to stop exactly at the end:
            // v = sqrt(2 * a * remaining).
            var brake = Mathf.Sqrt(2f * acceleration * Remaining(pos));
            var wanted = Mathf.Min(speed, brake);
            currentSpeed = Mathf.MoveTowards(
                currentSpeed, wanted, acceleration * dt);

            // Facing away, it turns first and walks after.
            var align = Mathf.Clamp01(Vector3.Dot(transform.forward, dir));
            var step = Mathf.Min(currentSpeed * align * dt, distance);
            var next = pos + dir * step;
            next.y = Mathf.Lerp(pos.y, target.y, step / distance);
            // Path corners only mark turns; between them the ground can
            // rise and fall (bumps, stairs), so stand on the NavMesh.
            if (NavMesh.SamplePosition(next, out var hit, groundSnap,
                    NavMesh.AllAreas))
                next.y = hit.position.y;
            transform.position = next;
        }

        private void TurnToFacing(float dt)
        {
            if (!facing.HasValue
                || Quaternion.Angle(transform.rotation, facing.Value) < 0.5f)
            {
                Arrive();
                return;
            }
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, facing.Value, turnSpeed * dt);
        }

        private void Arrive()
        {
            walking = false;
            currentSpeed = 0f;
            Arrived?.Invoke();
        }

        private float Remaining(Vector3 pos)
        {
            var sum = Flat(corners[nextCorner] - pos).magnitude;
            for (var i = nextCorner; i < corners.Length - 1; i++)
                sum += Flat(corners[i + 1] - corners[i]).magnitude;
            return sum;
        }

        private static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);
    }
}
