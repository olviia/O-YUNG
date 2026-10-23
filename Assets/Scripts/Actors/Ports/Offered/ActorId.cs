using Oyung.SharedKernel.Unity;
using UnityEngine;

namespace Oyung.Actors
{
    /// <summary>
    /// Offered port: an actor's id. Whoever directs actors holds
    /// the id and finds the live actor through it (Current, Changed).
    /// </summary>
    [CreateAssetMenu(menuName = "Oyung/Actors/Actor Id",
        fileName = "NewActorId")]
    public sealed class ActorId : SceneElementId<IActor> { }
}
