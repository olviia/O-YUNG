using System;
using UnityEngine;

namespace Oyung.Cutscenes.Unity
{
    /// <summary>
    /// Cutscenes: creates the store, hands it to the scene's player and
    /// binds every catalog cutscene to both. Start lets them play.
    /// </summary>
    public class CutscenesInstaller : MonoBehaviour, IDisposable
    {
        [SerializeField] private CutsceneCatalog catalog;
        [SerializeField] private TimelineCutscenePlayer player;
       
        [SerializeField] private CutsceneSettings settings;

        [Tooltip("Optional: on-screen Speed/Skip prompts.")]
        [SerializeField] private CutsceneUI ui;

        public ICutsceneEvents Build(ICutsceneInput input)
        {
            var store = new CutsceneStore();
            player.Use(catalog, settings, store, input);
            foreach (var cutscene in catalog.Cutscenes)
                cutscene.Bind(store, player);
            if (ui != null) ui.Bind(player);
            return player;
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
            if (ui != null) ui.Unbind();
        }
    }
}
