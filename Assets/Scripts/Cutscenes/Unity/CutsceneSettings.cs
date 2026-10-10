using UnityEngine;

namespace Oyung.Cutscenes.Unity
{
    // CLAUDE: class comment (your words). Draft: knows how cutscenes
    // CLAUDE: play; tuned by a designer in the Inspector, read by the
    // CLAUDE: player, written by a settings menu later.
    public class CutsceneSettings : MonoBehaviour, ICutsceneSettings
    {
        [Tooltip("Playback speed when the player switches to fast.")]
        [SerializeField] private float fastSpeed = 2f;

        public float FastSpeed
        {
            get => fastSpeed;
            set => fastSpeed = value;
        }
    }
}
