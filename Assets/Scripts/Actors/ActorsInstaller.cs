using System;
using Oyung.SharedKernel;
using UnityEngine;

namespace Oyung.Actors
{
    /// <summary>
    /// Actors: needs nothing from other modules. Creates the store and gives
    /// it to every actor id, so an actor can place itself from it.
    /// </summary>
    public class ActorsInstaller : MonoBehaviour, IDisposable
    {
        [SerializeField] private ActorCatalog catalog;

        private ActorStore _store;

        public ISaveable Build()
        {
            _store = new ActorStore(WriteDownLiveActors);
            foreach (var id in catalog.Items) id.Use(_store);
            return _store;
        }

        /// <summary>After load: places the actors that are already here.
        /// Whichever arrives second does the placing, whatever order Unity
        /// woke things in; one enabling later places itself.</summary>
        public void PlaceActors()
        {
            foreach (var id in catalog.Items)
                if (id.Current is Actor actor) actor.Place();
        }

        public void Dispose()
        {
            foreach (var id in catalog.Items) id.Use(null);
            _store = null;
        }

        private void WriteDownLiveActors()
        {
            foreach (var id in catalog.Items)
                if (id.Current is Actor actor) _store.Set(id.name, actor.State);
        }
    }
}
