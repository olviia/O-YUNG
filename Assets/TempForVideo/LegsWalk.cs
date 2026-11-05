using UnityEngine;

namespace TempForVideo
{
    // Throwaway: plays Dad's walk on the borrowed legs while the body
    // moves, at the pace it moves (Speed blends stand into walk,
    // WalkRate scales playback). Not part of the game.
    public sealed class LegsWalk : MonoBehaviour
    {
        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int RateId = Animator.StringToHash("WalkRate");

        [SerializeField] Animator animator;
        [Tooltip("Speed the walk travels at this scale (m/s).")]
        [SerializeField] float walkSpeed = 0.72f;
        [SerializeField] float blendSpeed = 4f;

        Vector3 lastPosition;
        float speed01;

        void OnEnable() => lastPosition = transform.position;

        void Update()
        {
            var dt = Time.deltaTime;
            if (dt <= 0f) return;
            var moved = transform.position - lastPosition;
            lastPosition = transform.position;
            moved.y = 0f;
            var relative = moved.magnitude / dt / walkSpeed;
            speed01 = Mathf.MoveTowards(speed01, Mathf.Min(relative, 1f),
                blendSpeed * dt);
            animator.SetFloat(SpeedId, speed01);
            animator.SetFloat(RateId, Mathf.Max(relative, 0.6f));
        }
    }
}
