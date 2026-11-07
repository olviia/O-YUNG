using Oyung.Interaction.Unity;
using UnityEngine;

namespace Oyung.Minigames.NamePop
{
    // CLAUDE: one key of the bubble keyboard, reused for its whole life:
    // CLAUDE: waits, flies from the parents to its place, bobs there until
    // CLAUDE: popped, then the game sends it again. The gaze finds it as an
    // CLAUDE: ordinary Interactable; on the way its collider is off, so it
    // CLAUDE: can't be popped. Popping comes in through PopBubble.
    internal sealed class LetterBubble : MonoBehaviour
    {
        internal enum Kind { Letter, Remove, Done }

        private enum State { Hidden, Waiting, Flying, Resting }

        [SerializeField] private Interactable interactable;
        [SerializeField] private SphereCollider hitArea;
        [Tooltip("Holds everything that shows; grows and shrinks.")]
        [SerializeField] private Transform look;
        [SerializeField] private Transform sphere;
        [SerializeField] private MeshFilter letterMesh;
        [SerializeField] private MeshRenderer letterRenderer;

        private NamePopGame _game;
        private NamePopConfig _config;
        private Color _colour;
        private Vector3 _slot;
        private State _state = State.Hidden;
        private float _waitLeft;
        private float _flightSeconds;
        private float _flightTime;
        private Vector3 _from;
        private bool _growsInPlace;
        private Vector3 _sideways;
        private float _bobPhase;
        private bool _focused;
        private float _focus;

        internal Kind Type { get; private set; }

        internal char Letter { get; private set; }

        private bool IsResting => _state == State.Resting;

        internal void Init(NamePopGame game, NamePopConfig config, Kind type,
            char letter, Mesh mesh, Color colour, Vector3 slot)
        {
            _game = game;
            _config = config;
            Type = type;
            Letter = letter;
            _colour = colour;
            _slot = slot;
            name = type == Kind.Letter ? $"Bubble {letter}" : $"Bubble {type}";
            _bobPhase = Random.value * Mathf.PI * 2f;

            letterMesh.sharedMesh = mesh;
            hitArea.radius = config.BubbleDiameter / 2f;
            sphere.localScale = Vector3.one * config.BubbleDiameter;
            letterMesh.transform.localScale =
                Vector3.one * config.LetterHeightInBubble;
            interactable.FocusChanged += OnFocusChanged;
            Hide();
        }

        /// <summary>Flies in from a point near the parents after the
        /// delay.</summary>
        internal void SendFrom(Vector3 from, float delay, float seconds)
        {
            _from = from;
            _growsInPlace = false;
            _sideways = Random.insideUnitSphere * _config.FlightArc;
            Begin(delay, seconds);
        }

        /// <summary>Grows where it belongs, without a flight.</summary>
        internal void GrowInPlace(float seconds)
        {
            _growsInPlace = true;
            Begin(0f, seconds);
        }

        internal void Hide()
        {
            _state = State.Hidden;
            _focused = false;
            _focus = 0f;
            hitArea.enabled = false;
            look.gameObject.SetActive(false);
        }

        internal bool IsShown => look.gameObject.activeSelf;

        /// <summary>The player popped it (see PopBubble).</summary>
        internal void Pop()
        {
            if (!IsResting) return;
            _game.OnPopped(this);
        }

        private void Begin(float delay, float seconds)
        {
            Hide();
            _waitLeft = delay;
            _flightSeconds = seconds;
            _flightTime = 0f;
            _state = State.Waiting;
        }

        private void OnFocusChanged(bool focused) => _focused = focused;

        private void Update()
        {
            switch (_state)
            {
                case State.Waiting: Wait(); break;
                case State.Flying: Fly(); break;
                case State.Resting: Bob(); break;
                default: return;
            }
            ShowFocus();
        }

        private void Wait()
        {
            _waitLeft -= Time.deltaTime;
            if (_waitLeft > 0f) return;
            _state = State.Flying;
            look.gameObject.SetActive(true);
            Fly();
        }

        private void Fly()
        {
            _flightTime += Time.deltaTime;
            var t = Mathf.Clamp01(_flightTime / _flightSeconds);
            if (t >= 1f)
            {
                _state = State.Resting;
                hitArea.enabled = true;
                look.localScale = Vector3.one;
                Bob();
                return;
            }

            var eased = Mathf.SmoothStep(0f, 1f, t);
            var target = transform.parent.TransformPoint(_slot);
            look.localScale = Vector3.one * (_growsInPlace
                ? eased
                : Mathf.Lerp(0.3f, 1f, eased));
            // CLAUDE: an arc, so bubbles don't arrive along straight lines.
            transform.position = _growsInPlace
                ? target
                : Vector3.Lerp(_from, target, eased)
                  + (_sideways + Vector3.up * _config.FlightArc)
                  * Mathf.Sin(eased * Mathf.PI);
        }

        private void Bob()
        {
            var height = Mathf.Sin(Time.time * _config.BobSpeed + _bobPhase)
                         * _config.BobHeight;
            transform.localPosition = _slot + Vector3.up * height;
            transform.localRotation = Quaternion.identity;
        }

        private void ShowFocus()
        {
            _focus = Mathf.MoveTowards(_focus, _focused ? 1f : 0f,
                Time.deltaTime / _config.FocusSeconds);
            if (_state == State.Resting)
                look.localScale = Vector3.one *
                                  Mathf.Lerp(1f, _config.FocusScale, _focus);
            // CLAUDE: only the look moves and grows: the collider stays the
            // CLAUDE: same, so a larger bubble doesn't steal the gaze.
            look.localPosition = Vector3.back * (_focus * _config.FocusForward);
            ToyLetterLook.Apply(letterRenderer, _colour, _focus);
        }

        private void OnDestroy()
        {
            if (interactable != null)
                interactable.FocusChanged -= OnFocusChanged;
        }
    }
}
