using System;
using Oyung.Actors;
using Oyung.Locations;
using UnityEngine;

namespace Oyung.Sequences.Unity
{
    /// <summary>The actor goes to a location and ends facing its way.
    /// </summary>
    [Serializable]
    public sealed class WalkTo : SequenceStep
    {
        [SerializeField] private LocationId to;

        public override void Execute(IActor actor, Action done)
        {
            var spot = Spot();
            Require<IMovableActor>(actor)
                .WalkTo(spot.Position, spot.Facing, done);
        }

        public override void JumpToEnd(IActor actor)
        {
            var spot = Spot();
            Require<IMovableActor>(actor).JumpTo(spot.Position, spot.Facing);
        }

        private ILocation Spot()
        {
            if (to == null)
                throw new InvalidOperationException(
                    "WalkTo has no location to walk to.");
            return to.Current ?? throw new InvalidOperationException(
                $"Location '{to.name}' is not in any loaded scene.");
        }
    }
}
