using System;
using Oyung.SharedKernel;
using Oyung.SharedKernel.Unity;
using UnityEngine;

namespace Oyung.Cutscenes.Unity
{
    // CLAUDE: class comment (your words). Draft: one cutscene: id,
    // CLAUDE: prefab, Play-when condition. Value = played (proxy to the
    // CLAUDE: store). While listening, says "play me" to the store when
    // CLAUDE: Play-when is met. Read by other modules via BoolCondition.
    [CreateAssetMenu(menuName = "Oyung/Cutscenes/Cutscene",
        fileName = "NewCutscene")]
    public sealed class CutsceneAsset : ModuleFact<bool>
    {
        [Tooltip("Prefab with a PlayableDirector (Wrap Mode: None).")]
        [SerializeField] private GameObject prefab;

        [SerializeReference] private Condition playWhen;

        private CutsceneStore _store;

        public override bool Value => Store.IsPlayed(Name);

        internal GameObject Prefab => prefab;

        // CLAUDE: create phase: read + Changed work, nothing plays yet.
        internal void Bind(CutsceneStore store)
        {
            _store = store;
            _store.Subscribe(Name, RaiseChanged);
        }

        // CLAUDE: start phase (after load): a condition already met at
        // CLAUDE: start (the intro) plays right away.
        internal void StartListening()
        {
            PlayWhen.Changed += OnPlayWhenChanged;
            OnPlayWhenChanged();
        }

        internal void Unbind()
        {
            if (playWhen != null) playWhen.Changed -= OnPlayWhenChanged;
            _store?.Unsubscribe(Name, RaiseChanged);
            _store = null;
        }

        private void OnPlayWhenChanged()
        {
            if (PlayWhen.IsMet) Store.RequestPlay(Name);
        }

        private Condition PlayWhen => playWhen ??
            throw new InvalidOperationException(
                $"Cutscene '{Name}' has no Play-when condition.");

        private CutsceneStore Store => _store ??
            throw new InvalidOperationException(
                $"Cutscene '{Name}' is not bound. Is it in the catalog?");
    }
}
