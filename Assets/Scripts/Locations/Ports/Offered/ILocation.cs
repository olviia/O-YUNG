using UnityEngine;

namespace Oyung.Locations
{
    /// <summary>
    /// Offered port: a named spot in the world, with the way to face
    /// when standing there.
    /// </summary>
    public interface ILocation
    {
        LocationId Id { get; }

        Vector3 Position { get; }

        Quaternion Facing { get; }
    }
}
