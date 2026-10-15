using System;
using System.Collections.Generic;

namespace Oyung.Interaction
{
    // CLAUDE: the player's side of interacting. Knows: the current
    // CLAUDE: target and what it offers. Does: each frame picks the
    // CLAUDE: target among the sightings; on a press, carries out the
    // CLAUDE: offer. Polls, never subscribes: See runs every frame
    // CLAUDE: anyway, and IsMet is cheap, so a fact changed from
    // CLAUDE: anywhere shows on the next frame.
    internal sealed class Interactor
    {
        private float _enterAngle;
        private float _leaveAngle;
        private float _stickiness;

        internal Interactor(float enterAngle, float leaveAngle,
            float stickiness) =>
            Configure(enterAngle, leaveAngle, stickiness);

        /// <summary>Raised when Target changes (to another or to none),
        /// e.g. to move a highlight.</summary>
        internal event Action TargetChanged;

        /// <summary>Null when nothing is targeted.</summary>
        internal IInteractable Target { get; private set; }

        /// <summary>What a press would do now; null exactly when
        /// Target is null.</summary>
        internal IInteraction Offer { get; private set; }

        // CLAUDE: hysteresis: a new target must come within enterAngle;
        // CLAUDE: the current one is kept up to leaveAngle, so the
        // CLAUDE: prompt doesn't flicker at the edge. Stickiness does
        // CLAUDE: the same between two targets: the current one counts
        // CLAUDE: as that many degrees closer, so near-ties don't swap.
        internal void Configure(float enterAngle, float leaveAngle,
            float stickiness)
        {
            if (enterAngle <= 0 || leaveAngle < enterAngle || stickiness < 0)
                throw new ArgumentException(
                    "Interactor needs 0 < enter angle <= leave angle " +
                    "and stickiness >= 0.");
            _enterAngle = enterAngle;
            _leaveAngle = leaveAngle;
            _stickiness = stickiness;
        }

        /// <summary>This frame's candidates. Picks the one closest to the
        /// view centre that offers something; refreshes Offer.</summary>
        internal void See(IReadOnlyList<Sighting> sightings)
        {
            IInteractable best = null;
            IInteraction bestOffer = null;
            var bestScore = float.MaxValue;

            foreach (var sighting in sightings)
            {
                var isCurrent = sighting.Target == Target;
                var limit = isCurrent ? _leaveAngle : _enterAngle;
                if (sighting.Angle > limit) continue;

                var score = sighting.Angle - (isCurrent ? _stickiness : 0);
                if (score >= bestScore) continue;

                // CLAUDE: something that offers nothing right now is not
                // CLAUDE: a target at all: no prompt, and it doesn't hide
                // CLAUDE: a flower behind it that does offer something.
                var offer = FirstAvailable(sighting.Target);
                if (offer == null) continue;

                best = sighting.Target;
                bestOffer = offer;
                bestScore = score;
            }

            SetTarget(best, bestOffer);
        }

        /// <summary>The player pressed Interact: carries out the offer,
        /// if it is still available. The next offer shows at once.
        /// </summary>
        internal void Use()
        {
            if (Target == null) return;
            // CLAUDE: re-asked: a fact may have changed since See.
            var offer = FirstAvailable(Target);
            if (offer == null)
            {
                SetTarget(null, null);
                return;
            }

            foreach (var instruction in offer.OnUse) instruction.Execute();

            var next = FirstAvailable(Target);
            SetTarget(next == null ? null : Target, next);
        }

        private static IInteraction FirstAvailable(IInteractable target)
        {
            foreach (var interaction in target.Interactions)
                if (interaction.AvailableWhen == null ||
                    interaction.AvailableWhen.IsMet)
                    return interaction;
            return null;
        }

        private void SetTarget(IInteractable target, IInteraction offer)
        {
            Offer = offer;
            if (target == Target) return;
            Target = target;
            TargetChanged?.Invoke();
        }
    }
}
