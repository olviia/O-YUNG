using UnityEngine;

namespace Oyung.Speech.Unity
{
    // CLAUDE: class comment (your words). Draft: moves the body along with
    // CLAUDE: its voice. Each syllable pushes a damped spring: the chest
    // CLAUDE: squashes, the head nods, stressed syllables harder, then both
    // CLAUDE: settle by themselves. No start or stop signals, so a line cut
    // CLAUDE: short can't leave the body stuck talking.
    // CLAUDE: Optional: a character without it speaks without moving. Adds
    // CLAUDE: on top of the pose the Animator wrote this frame, so walking
    // CLAUDE: and held poses keep going. The Animator rewrites rotation
    // CLAUDE: every frame but not a constant scale, so the squash remembers
    // CLAUDE: the scale it started from, or it would pile up.
    // CLAUDE: Runs after WalkMotion (order 10), which also writes bones.
    [DefaultExecutionOrder(20)]
    internal sealed class TalkMotion : MonoBehaviour
    {
        // Below this the spring counts as still, and the component sleeps.
        private const float Still = 0.001f;
        // A long frame must not make the spring blow up.
        private const float LongestStep = 1f / 30f;

        [SerializeField] private Speaker speaker;
        [Tooltip("Squashes on each syllable; everything above it follows.")]
        [SerializeField] private Transform chest;
        [SerializeField] private Transform head;
        [Tooltip("How far a stressed syllable squashes the chest (0.1 = "
                 + "10%).")]
        [Range(0f, 0.3f)]
        [SerializeField] private float squash = 0.06f;
        [Tooltip("How far a stressed syllable nods the head, in degrees.")]
        [Range(0f, 30f)]
        [SerializeField] private float nod = 6f;
        [Tooltip("How hard a plain syllable pushes, next to a stressed one.")]
        [Range(0f, 1f)]
        [SerializeField] private float plainStrength = 0.55f;
        [Tooltip("How fast the spring swings, per second.")]
        [Min(0.1f)]
        [SerializeField] private float frequency = 3f;
        [Tooltip("How fast the swing dies: 0 forever, 1 no overshoot.")]
        [Range(0f, 1f)]
        [SerializeField] private float damping = 0.4f;

        private float _position;
        private float _velocity;
        private Vector3 _chestScale;
        private Vector3 _writtenScale;

        // Awake and OnDestroy, not OnEnable: it sleeps by disabling itself
        // and must still hear the next syllable.
        private void Awake()
        {
            // Checked here, not left to a NullReferenceException later.
            Require(speaker, nameof(speaker));
            Require(chest, nameof(chest));
            Require(head, nameof(head));
            _chestScale = chest.localScale;
            _writtenScale = _chestScale;
            speaker.SyllableStarted += OnSyllableStarted;
        }

        private void OnDestroy()
        {
            if (speaker != null)
                speaker.SyllableStarted -= OnSyllableStarted;
        }

        private void OnSyllableStarted(SyllableEvent syllable)
        {
            _velocity += (syllable.Stressed ? 1f : plainStrength)
                         * AngularFrequency;
            enabled = true;
        }

        private void LateUpdate()
        {
            var step = Mathf.Min(Time.deltaTime, LongestStep);
            var w = AngularFrequency;
            _velocity -= (w * w * _position + 2f * damping * w * _velocity)
                         * step;
            _position += _velocity * step;

            var settled = Mathf.Abs(_position) < Still
                          && Mathf.Abs(_velocity) < Still;
            if (settled)
            {
                _position = 0f;
                _velocity = 0f;
                enabled = false;
            }

            // Written once more when settled, to leave the rest scale.
            Squash();
            // The body's right, not the bone's: the bones face backwards.
            head.rotation = Quaternion.AngleAxis(nod * _position,
                transform.right) * head.rotation;
        }

        private void Squash()
        {
            // Changed since our last write: a clip animates it, go from that.
            if (chest.localScale != _writtenScale)
                _chestScale = chest.localScale;
            var down = squash * _position;
            _writtenScale = Vector3.Scale(_chestScale, new Vector3(
                1f + down / 2f, 1f - down, 1f + down / 2f));
            chest.localScale = _writtenScale;
        }

        private float AngularFrequency => 2f * Mathf.PI * frequency;

        private void Require(Object part, string field)
        {
            if (part == null)
                throw new System.InvalidOperationException(
                    $"TalkMotion '{name}' has no {field}; set it in the "
                    + "Inspector.");
        }
    }
}
