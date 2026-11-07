using UnityEngine;

namespace Oyung.Minigames.NamePop
{
    // CLAUDE: stays where it is in the world, but turns with the view, so
    // CLAUDE: rows laid out along its axes stay level on screen however the
    // CLAUDE: head turns or tilts; turning away slides them out of sight.
    // CLAUDE: Late, after Cinemachine has moved the camera this frame.
    [DefaultExecutionOrder(1000)]
    internal sealed class ViewAligned : MonoBehaviour
    {
        [SerializeField] private Transform view;

        private void LateUpdate() => transform.rotation = view.rotation;
    }
}
