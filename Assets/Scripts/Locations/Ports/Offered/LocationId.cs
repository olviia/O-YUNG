using Oyung.SharedKernel.Unity;
using UnityEngine;

namespace Oyung.Locations
{
    /// <summary>
    /// Offered port: a place's id. Whoever needs the place holds
    /// the id and finds the live location through it (Current).
    /// </summary>
    [CreateAssetMenu(menuName = "Oyung/Locations/Location Id",
        fileName = "NewLocationId")]
    public sealed class LocationId : SceneElementId<ILocation> { }
}
