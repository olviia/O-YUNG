using System;
using System.Collections.Generic;

namespace Oyung.Actors
{
    /// <summary>
    /// Offered port: an actor that can hold authored poses ("Hug").
    /// </summary>
    public interface IAnimatedActor : IActor
    {
        /// <summary>Names of every pose this actor has.</summary>
        IReadOnlyList<string> Poses { get; }

        /// <summary>Blends into the pose over fade seconds, then calls
        /// done once. Throws if the actor has no such pose.</summary>
        void Pose(string pose, float fade, Action done);

        /// <summary>Holds the pose at once, without blending.</summary>
        void JumpToPose(string pose);
    }
}
