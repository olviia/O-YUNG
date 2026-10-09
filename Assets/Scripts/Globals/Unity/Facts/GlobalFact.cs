using System;
using Oyung.SharedKernel.Unity;

namespace Oyung.Globals.Unity
{
    // CLAUDE: class comment: knows / does / used by
    // CLAUDE: draft: a fact whose value lives in the GlobalStore it was
    // CLAUDE: given by the installer. Other modules see ModuleFact<T>.
    public abstract class GlobalFact<T> : ModuleFact<T>, IGlobalFact
    {
        private GlobalStore _store;

        public override T Value => Store.Get<T>(Name);

        /// <summary>Passes the new value to the GlobalStore, which holds
        /// it and raises Changed only if it really changed. Internal:
        /// only Globals instructions write; other modules just read.
        /// </summary>
        internal void Set(T value) => Store.Set(Name, value);

        // CLAUDE: explicit: only code that sees the internal IGlobalFact
        // CLAUDE: (the installer) can bind; other modules can't.
        void IGlobalFact.Bind(GlobalStore store)
        {
            _store = store;
            _store.Subscribe(Name, RaiseChanged);
        }

        void IGlobalFact.Unbind()
        {
            _store?.Unsubscribe(Name, RaiseChanged);
            _store = null;
        }

        // CLAUDE: unbound means the fact is missing from the catalog or
        // CLAUDE: read before the installer ran: fail fast.
        // CLAUDE: private protected: typed subclasses (GlobalIntFact)
        // CLAUDE: route their own operations; outside Globals unseen.
        private protected GlobalStore Store => _store ??
            throw new InvalidOperationException(
                $"Global fact '{Name}' is not bound. Is it in the catalog?");
    }
}
