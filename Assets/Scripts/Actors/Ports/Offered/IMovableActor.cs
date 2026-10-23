using System;
using UnityEngine;

namespace Oyung.Actors
{
    /// <summary>
    /// Offered port: an actor that can go places. How it gets there
    /// (walking, rolling) is its own business.
    /// </summary>
    public interface IMovableActor : IActor
    {
        /// <summary>Goes to the position, ends facing as given, then
        /// calls arrived once. A new order replaces the old one, whose
        /// arrived is never called.</summary>
        void WalkTo(Vector3 position, Quaternion facing, Action arrived);

        /// <summary>Stands at the position at once, facing as given.
        /// </summary>
        void JumpTo(Vector3 position, Quaternion facing);
    }
}
