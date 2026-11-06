using System;
using Oyung.SharedKernel;
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
    public sealed class ActorId : SceneElementId<IActor>, ICataloguedItem
    {
        // Runtime only: never written into the asset.
        [NonSerialized] private ActorStore _store;

        public string Name => name;

        // CLAUDE: null until the installer runs, and always in a scene
        // CLAUDE: with no root (Test): the actor then stays where it stands.
        internal ActorStore Store => _store;

        internal void Use(ActorStore store) => _store = store;
    }
}
