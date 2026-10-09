using System;
using UnityEngine;

namespace Oyung.Cutscenes.Unity
{
    /// <summary>
    /// Cutscenes: creates the store with the scene's player and binds
    /// every catalog cutscene to it. Start lets them play.
    /// </summary>
    public class CutscenesInstaller : MonoBehaviour, IDisposable
    {
        [SerializeField] private CutsceneCatalog catalog;
        [SerializeField] private TimelineCutscenePlayer player;

        public void Build()
        {
            player.Use(catalog);
            var store = new CutsceneStore(player);
            foreach (var cutscene in catalog.Cutscenes) cutscene.Bind(store);
        }

        /// <summary>Start phase, after load: cutscenes whose condition
        /// is met may play.</summary>
        public void StartListening()
        {
            foreach (var cutscene in catalog.Cutscenes)
                cutscene.StartListening();
        }

        public void Dispose()
        {
            foreach (var cutscene in catalog.Cutscenes) cutscene.Unbind();
        }
    }
}
