using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

namespace Oyung.Cutscenes.Unity
{
    /// <summary>
    /// The on-screen panel while a cutscene plays: Speed and Skip prompts.
    /// Shown on start, hidden on end; the speed row says what pressing
    /// does next ("Faster" at normal speed, "Slower" when fast). Texts are
    /// picked in the Inspector; button icons are InputIcons inside it.
    /// </summary>
    public sealed class CutsceneUI : MonoBehaviour
    {
        [Tooltip("The speed row's text.")]
        [SerializeField] private LocalizeStringEvent speedLabel;

        [Tooltip("Shown at normal speed.")]
        [SerializeField] private LocalizedString faster;

        [Tooltip("Shown while fast.")]
        [SerializeField] private LocalizedString slower;

        private TimelineCutscenePlayer _player;

        // Bound by the installer, not in OnEnable/OnDestroy: the panel
        // starts disabled, and Unity skips those on objects never active.
        internal void Bind(TimelineCutscenePlayer player)
        {
            _player = player;
            _player.Started += Show;
            _player.Ended += Hide;
            _player.SpeedChanged += ShowSpeed;
            gameObject.SetActive(false);
        }

        internal void Unbind()
        {
            if (_player == null) return;
            _player.Started -= Show;
            _player.Ended -= Hide;
            _player.SpeedChanged -= ShowSpeed;
            _player = null;
        }

        private void Show()
        {
            ShowSpeed();
            gameObject.SetActive(true);
        }

        private void Hide() => gameObject.SetActive(false);

        private void ShowSpeed() =>
            speedLabel.StringReference = _player.IsFast ? slower : faster;
    }
}
