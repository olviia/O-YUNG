using UnityEngine;

namespace Oyung.Eyes
{
    // CLAUDE: the baby's eyes in the scene. Rotates strictly with the Look input, only within the eyes' range. No sway.
    // CLAUDE: MonoBehaviour on the camera object; receives IGameInput (the port, never the adapter) from the composition root. Try Cinemachine first (already installed).
    public class LookCamera : MonoBehaviour
    {
    }
}
