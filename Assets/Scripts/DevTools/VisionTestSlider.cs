using Oyung.Input;
using Oyung.Vision;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Oyung.DevTools
{
    // CLAUDE: dev-only stand-in for the future Progression module: drives IVision from a "Better eyes" slider.
    // CLAUDE: Talks only through public ports, so it behaves exactly like a real caller would.
    // CLAUDE: Tab frees the mouse (enters Menu mode, which turns looking off) so the slider can be dragged.
    public class VisionTestSlider : MonoBehaviour
    {
        // CLAUDE: stand-in growth curve — the real one will be Progression's knowledge.
        [SerializeField] private float newbornClearDistance = 0.05f;
        [SerializeField] private float grownClearDistance = 0.5f;

        private IVision vision;
        private IInputModes inputModes;
        private float betterEyes;
        private bool open;

        /// <summary>Called by the root, like a module receiving its required ports.</summary>
        public void Use(IVision vision, IInputModes inputModes)
        {
            this.vision = vision;
            this.inputModes = inputModes;
            Apply();
        }

        private void Update()
        {
            if (inputModes == null || Keyboard.current == null) return;
            if (!Keyboard.current.tabKey.wasPressedThisFrame) return;

            open = !open;
            if (open) inputModes.EnterMode(InputMode.Menu);
            else inputModes.ExitMode(InputMode.Menu);
            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        }

        private void OnGUI()
        {
            if (vision == null) return;

            GUILayout.BeginArea(new Rect(16, 16, 260, 90), GUI.skin.box);
            GUILayout.Label(open ? "Better eyes" : "Better eyes  (Tab to edit)");
            float value = GUILayout.HorizontalSlider(betterEyes, 0f, 1f);
            GUILayout.Label($"clear {ClearDistance():0.00} m   strength {Strength():0.00}");
            GUILayout.EndArea();

            if (!Mathf.Approximately(value, betterEyes))
            {
                betterEyes = value;
                Apply();
            }
        }

        private void Apply() => vision.SetBlur(ClearDistance(), Strength());

        private float ClearDistance() => Mathf.Lerp(newbornClearDistance, grownClearDistance, betterEyes);

        private float Strength() => 1f - betterEyes;
    }
}
