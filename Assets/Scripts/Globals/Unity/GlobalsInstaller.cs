using System;
using UnityEngine;

namespace Oyung.Globals.Unity
{
    /// <summary>
    /// Globals: needs nothing from other modules. Creates the store and
    /// gives it to every fact in the catalog.
    /// </summary>
    public class GlobalsInstaller : MonoBehaviour, IDisposable
    {
        [SerializeField] private GlobalCatalog catalog;

        public void Build()
        {
            var store = new GlobalStore();
            foreach (var fact in catalog.Facts) fact.Bind(store);
        }

        public void Dispose()
        {
            foreach (var fact in catalog.Facts) fact.Unbind();
        }
    }
}
