using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

namespace Oyung.Save.Unity
{
    /// <summary>
    /// The small note on the HUD after a save was asked for: saved,
    /// unavailable mid-action, or failed. Shown briefly, then hides itself.
    /// Texts are picked in the Inspector.
    /// </summary>
    public sealed class SaveNotice : MonoBehaviour
    {
        [Tooltip("The notice's text.")]
        [SerializeField] private LocalizeStringEvent label;

        [SerializeField] private LocalizedString saved;

        [Tooltip("Shown when something holds a save block.")]
        [SerializeField] private LocalizedString unavailable;

        [Tooltip("Shown when storage could not write.")]
        [SerializeField] private LocalizedString failed;

        [Tooltip("Seconds on screen, in real time: a paused game counts.")]
        [Min(0f)]
        [SerializeField] private float seconds = 2f;

        private ISaveGame _saveGame;
        private Coroutine _hiding;

        // Bound by the installer, not in OnEnable/OnDestroy: the notice
        // starts disabled, and Unity skips those on objects never active.
        internal void Bind(ISaveGame saveGame)
        {
            _saveGame = saveGame;
            _saveGame.Saved += OnSaved;
            _saveGame.Blocked += OnBlocked;
            _saveGame.WriteFailed += OnWriteFailed;
            gameObject.SetActive(false);
        }

        internal void Unbind()
        {
            if (_saveGame == null) return;
            _saveGame.Saved -= OnSaved;
            _saveGame.Blocked -= OnBlocked;
            _saveGame.WriteFailed -= OnWriteFailed;
            _saveGame = null;
        }

        private void OnSaved() => Show(saved);

        private void OnBlocked(IReadOnlyList<string> _) => Show(unavailable);

        private void OnWriteFailed(string _) => Show(failed);

        private void Show(LocalizedString text)
        {
            label.StringReference = text;
            gameObject.SetActive(true);
            // A second press restarts the clock.
            if (_hiding != null) StopCoroutine(_hiding);
            _hiding = StartCoroutine(HideLater());
        }

        private IEnumerator HideLater()
        {
            yield return new WaitForSecondsRealtime(seconds);
            _hiding = null;
            gameObject.SetActive(false);
        }
    }
}
