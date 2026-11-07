using UnityEngine;

namespace Oyung.Actors
{
    /// <summary>
    /// Offered port: an actor with a body standing somewhere in the world.
    /// </summary>
    public interface IPhysicalActor : IActor
    {
        /// <summary>Where its feet are.</summary>
        Vector3 Position { get; }

        /// <summary>From its feet to the top of its body, in metres.
        /// </summary>
        float Height { get; }
    }
}
