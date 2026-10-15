using System.Collections.Generic;
using UnityEngine;

namespace Oyung.Interaction.Unity
{
    // CLAUDE: the player's interacting, driven by where the eyes look.
    // CLAUDE: Knows: the eyes, the angles, what blocks sight, the prompt.
    // CLAUDE: Does each frame: finds interactables near the eyes, keeps
    // CLAUDE: those in range and in sight, measures each one's angle to
    // CLAUDE: its nearest point, and lets the core Interactor pick; then
    // CLAUDE: shows or hides the prompt. On a press: Interactor.Use.
    // CLAUDE: No registry of interactables: physics finds them.
    [AddComponentMenu("Oyung/Interaction/Gaze Interactor")]
    internal sealed class GazeInteractor : MonoBehaviour
    {
        [Tooltip("The baby's eyes (the object LookCamera turns).")]
        [SerializeField] private Transform eyes;

        [Tooltip("A target is picked when it comes this close to the " +
                 "view centre (degrees).")]
        [SerializeField] private float enterAngle = 8f;

        [Tooltip("The current target is kept until it is this far from " +
                 "the view centre (degrees). Keeps the prompt from " +
                 "flickering at the edge.")]
        [SerializeField] private float leaveAngle = 12f;

        [Tooltip("Between two targets, the current one counts as this " +
                 "many degrees closer, so near-ties don't swap.")]
        [SerializeField] private float stickiness = 1f;

        [Tooltip("Metres around the eyes searched for interactables; " +
                 "keep it at least the largest Interactable range.")]
        [SerializeField] private float searchRadius = 5f;

        [Tooltip("What blocks sight. Trigger colliders never do.")]
        [SerializeField] private LayerMask blocksSight = ~0;

        [SerializeField] private InteractPrompt prompt;

        private readonly Collider[] _nearby = new Collider[64];
        private readonly HashSet<Interactable> _measured =
            new HashSet<Interactable>();
        private readonly List<Sighting> _sightings = new List<Sighting>();
        private readonly Dictionary<IInteractable, Vector3> _points =
            new Dictionary<IInteractable, Vector3>();

        private Interactor _interactor;
        private IInteractionInput _input;

        internal void Init(IInteractionInput input)
        {
            _interactor = new Interactor(enterAngle, leaveAngle, stickiness);
            _input = input;
            _input.InteractPressed += OnInteractPressed;
            prompt.Hide();
        }

        internal void Release()
        {
            if (_input != null) _input.InteractPressed -= OnInteractPressed;
            _input = null;
            _interactor = null;
        }

        // CLAUDE: angles are tunable while playing.
        private void OnValidate()
        {
            enterAngle = Mathf.Max(0.1f, enterAngle);
            leaveAngle = Mathf.Max(enterAngle, leaveAngle);
            stickiness = Mathf.Max(0f, stickiness);
            _interactor?.Configure(enterAngle, leaveAngle, stickiness);
        }

        private void Update()
        {
            if (_interactor == null) return;
            Collect();
            _interactor.See(_sightings);
            ShowPrompt();
        }

        private void OnInteractPressed()
        {
            if (_interactor == null || !_input.CanInteract) return;
            _interactor.Use();
            ShowPrompt();
        }

        private void Collect()
        {
            _sightings.Clear();
            _points.Clear();
            _measured.Clear();
            if (!_input.CanInteract) return;

            var origin = eyes.position;
            var count = Physics.OverlapSphereNonAlloc(origin, searchRadius,
                _nearby, ~0, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
            {
                var interactable = _nearby[i].GetComponentInParent<Interactable>();
                if (interactable == null || !interactable.isActiveAndEnabled)
                    continue;
                if (!_measured.Add(interactable)) continue;
                if (Measure(interactable, out var angle, out var point))
                {
                    _sightings.Add(new Sighting(interactable, angle));
                    _points[interactable] = point;
                }
            }
        }

        // CLAUDE: the nearest point to where we look, over all its
        // CLAUDE: colliders: a wide crib or door counts from its edge,
        // CLAUDE: not its centre. Approximation: the point on the view
        // CLAUDE: ray level with the collider's centre, pulled onto the
        // CLAUDE: collider by ClosestPoint. Good enough for picking.
        private bool Measure(Interactable interactable, out float bestAngle,
            out Vector3 bestPoint)
        {
            var origin = eyes.position;
            var forward = eyes.forward;
            bestAngle = float.MaxValue;
            bestPoint = default;

            foreach (var collider in interactable.Colliders)
            {
                if (collider == null || !collider.enabled) continue;
                var along = Mathf.Max(0f,
                    Vector3.Dot(collider.bounds.center - origin, forward));
                var point = collider.ClosestPoint(origin + forward * along);
                var toPoint = point - origin;
                if (toPoint.magnitude > interactable.Range) continue;

                var angle = toPoint.sqrMagnitude < 1e-6f
                    ? 0f
                    : Vector3.Angle(forward, toPoint);
                if (angle >= bestAngle) continue;
                if (!InSight(interactable, origin, point)) continue;

                bestAngle = angle;
                bestPoint = point;
            }
            return bestAngle < float.MaxValue;
        }

        // CLAUDE: blocked only by something that isn't the target itself.
        private bool InSight(Interactable interactable, Vector3 origin,
            Vector3 point)
        {
            if (!Physics.Linecast(origin, point, out var hit, blocksSight,
                    QueryTriggerInteraction.Ignore))
                return true;
            return hit.collider.GetComponentInParent<Interactable>() ==
                   interactable;
        }

        private void ShowPrompt()
        {
            if (_interactor.Offer is Interactable.InteractionData offer &&
                _points.TryGetValue(_interactor.Target, out var point))
                prompt.Show(offer.Verb, point);
            else
                prompt.Hide();
        }
    }
}
