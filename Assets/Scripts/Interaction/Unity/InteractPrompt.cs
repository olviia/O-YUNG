using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

namespace Oyung.Interaction.Unity
{
    /// <summary>
    /// The "[button] Listen" prompt on the HUD canvas, pinned to a point
    /// in the world (the target's point nearest to the gaze) and kept on
    /// screen. The button icon is an InputIcon inside it; the verb comes
    /// from the offered interaction. Shown and hidden by GazeInteractor.
    /// </summary>
    // CLAUDE: late, so it is placed after the camera moved this frame
    // CLAUDE: (Cinemachine moves it in LateUpdate); else it trails.
    [DefaultExecutionOrder(1000)]
    [AddComponentMenu("Oyung/Interaction/Interact Prompt")]
    internal sealed class InteractPrompt : MonoBehaviour
    {
        [Tooltip("The camera that renders the eyes' view.")]
        [SerializeField] private Camera view;

        [Tooltip("The verb's text.")]
        [SerializeField] private LocalizeStringEvent verbLabel;

        [Tooltip("Pixels kept between the prompt and the screen edge.")]
        [SerializeField] private float screenMargin = 48f;

        private Vector3 _worldPoint;
        private LocalizedString _verb;

        internal void Show(LocalizedString verb, Vector3 worldPoint)
        {
            _worldPoint = worldPoint;
            if (verb != _verb)
            {
                _verb = verb;
                verbLabel.StringReference = verb;
            }
            if (!gameObject.activeSelf) gameObject.SetActive(true);
        }

        internal void Hide()
        {
            _verb = null;
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        // CLAUDE: assumes a Screen Space Overlay canvas, where a
        // CLAUDE: RectTransform's position is in screen pixels.
        private void LateUpdate()
        {
            var screen = view.WorldToScreenPoint(_worldPoint);
            screen.x = Mathf.Clamp(screen.x, screenMargin,
                Screen.width - screenMargin);
            screen.y = Mathf.Clamp(screen.y, screenMargin,
                Screen.height - screenMargin);
            transform.position = new Vector3(screen.x, screen.y, 0f);
        }
    }
}
