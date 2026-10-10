using System;
using UnityEngine;
using UnityEngine.Playables;

namespace Oyung.Cutscenes.Unity
{
    // CLAUDE: class comment (your words). Draft: plays cutscenes with
    // CLAUDE: Unity Timeline. Takes the id's prefab from the catalog,
    // CLAUDE: plays it, hears skip/speed while playing, announces start
    // CLAUDE: and end, marks the id played in the store, removes it.
    public class TimelineCutscenePlayer : MonoBehaviour,
        ICutscenePlayer, ICutsceneEvents
    {
        private CutsceneCatalog _catalog;
        private ICutsceneSettings _settings;
        private CutsceneStore _store;
        private ICutsceneInput _input;

        // CLAUDE: the playing cutscene; null between them.
        private PlayableDirector _director;
        private bool _fast;

        public event Action Started;
        public event Action Ended;

        internal void Use(CutsceneCatalog catalog, ICutsceneSettings settings,
            CutsceneStore store, ICutsceneInput input)
        {
            _catalog = catalog;
            _settings = settings;
            _store = store;
            _input = input;
        }

        void ICutscenePlayer.Play(string id)
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
            _director = director;
            _fast = false;
            director.Play();

            _input.SkipPressed += SkipToEnd;
            _input.SpeedTogglePressed += ToggleSpeed;
            Started?.Invoke();

            void OnStopped(PlayableDirector _)
            {
                director.stopped -= OnStopped;
                _input.SkipPressed -= SkipToEnd;
                _input.SpeedTogglePressed -= ToggleSpeed;
                _director = null;
                Destroy(instance);
                Ended?.Invoke();
                // CLAUDE: last: the fact may start the next cutscene.
                _store.MarkPlayed(id);
            }
        }

        // CLAUDE: Stop fires stopped, so the end runs through OnStopped.
        private void SkipToEnd() => _director.Stop();

        private void ToggleSpeed()
        {
            _fast = !_fast;
            // CLAUDE: speed must go on the graph's root playable;
            // CLAUDE: Timeline audio follows it, Time.time does not.
            _director.playableGraph.GetRootPlayable(0)
                .SetSpeed(_fast ? _settings.FastSpeed : 1f);
        }
    }
}
