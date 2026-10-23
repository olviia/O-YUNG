using System;
using Oyung.Actors;
using UnityEngine;

namespace Oyung.Sequences.Unity
{
    /// <summary>The actor blends into an authored pose and holds it.
    /// </summary>
    [Serializable]
    public sealed class Pose : SequenceStep
    {
        [PoseName]
        [SerializeField] private string pose;
        [Tooltip("Seconds to blend into the pose.")]
        [Min(0f)]
        [SerializeField] private float fade = 0.6f;

        public override void Execute(IActor actor, Action done) =>
            Require<IAnimatedActor>(actor).Pose(pose, fade, done);

        public override void JumpToEnd(IActor actor) =>
            Require<IAnimatedActor>(actor).JumpToPose(pose);
    }
}
