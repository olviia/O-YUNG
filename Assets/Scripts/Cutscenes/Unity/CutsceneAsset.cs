using System;
using Oyung.SharedKernel;
using Oyung.SharedKernel.Unity;
using UnityEngine;

namespace Oyung.Cutscenes.Unity
{
    // CLAUDE: class comment (your words). Draft: one cutscene: id,
    // CLAUDE: prefab, Play-when condition. Value = played (proxy to the
    // CLAUDE: store). While listening, asks the player to play when
    // CLAUDE: Play-when is met. Read by other modules via BoolCondition.
    [CreateAssetMenu(menuName = "Oyung/Cutscenes/Cutscene",
        fileName = "NewCutscene")]
    public sealed class CutsceneAsset : ModuleFact<bool>
    {
        [Tooltip("Prefab with a PlayableDirector (Wrap Mode: None).")]
        [SerializeField] private GameObject prefab;

        [SerializeReference] private Condition playWhen;

        private CutsceneStore _store;
        private ICutscenePlayer _player;

        public override bool Value => Store.IsPlayed(Name);

        internal GameObject Prefab => prefab;

        // CLAUDE: create phase: read + Changed work, nothing plays yet.
        internal void Bind(CutsceneStore store, ICutscenePlayer player)
        {
            _store = store;
            _player = player;
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
            _player = null;
        }

        private void OnPlayWhenChanged()
        {
            if (PlayWhen.IsMet) Player.Play(Name);
        }

        private Condition PlayWhen => playWhen ??
            throw new InvalidOperationException(
                $"Cutscene '{Name}' has no Play-when condition.");

        private ICutscenePlayer Player => _player ??
            throw new InvalidOperationException(
                $"Cutscene '{Name}' is not bound. Is it in the catalog?");

        private CutsceneStore Store => _store ??
            throw new InvalidOperationException(
                $"Cutscene '{Name}' is not bound. Is it in the catalog?");
    }
}
