using System;
using UnityEngine;
using UnityEngine.Playables;

namespace Oyung.Cutscenes.Unity
{
    // CLAUDE: class comment (your words). Draft: adapter, plays
    // CLAUDE: cutscenes with Unity Timeline. Takes the id's prefab from
    // CLAUDE: the catalog, instantiates, plays, reports the end, removes.
    public class TimelineCutscenePlayer : MonoBehaviour, ICutscenePlayer
    {
        private CutsceneCatalog _catalog;

        internal void Use(CutsceneCatalog catalog) => _catalog = catalog;

        void ICutscenePlayer.Play(string id, Action ended)
        {
            var instance = Instantiate(_catalog.Find(id).Prefab);
            var director = instance.GetComponentInChildren<PlayableDirector>();
            if (director == null)
            {
                Destroy(instance);
                throw new InvalidOperationException(
                    $"Cutscene '{id}' prefab has no PlayableDirector.");
            }

            // CLAUDE: stopped fires at the end only with Wrap Mode None.
            director.extrapolationMode = DirectorWrapMode.None;
            director.stopped += OnStopped;
            director.Play();

            void OnStopped(PlayableDirector _)
            {
                director.stopped -= OnStopped;
                Destroy(instance);
                ended();
            }
        }
    }
}
