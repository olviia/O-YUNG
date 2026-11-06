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

        public override void Execute(IActor actor, Action done) =>
            Require<IMovableActor>(actor).WalkTo(To, done);

        // CLAUDE: an empty slot is a designer mistake: fail fast.
        private LocationId To => to != null
            ? to
            : throw new InvalidOperationException(
                "WalkTo has no location to walk to.");
    }
}
