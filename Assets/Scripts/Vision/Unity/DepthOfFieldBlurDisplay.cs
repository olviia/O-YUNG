using UnityEngine;

namespace Oyung.Vision.Unity
{
    // CLAUDE: adapter — implements IBlurDisplay by driving URP's Depth of Field on a post-processing Volume in the scene.
    internal class DepthOfFieldBlurDisplay : MonoBehaviour, IBlurDisplay
    {
        // CLAUDE: stub until we walk through this adapter's card.
        public void Show(float clearDistance, float strength)
        {
            throw new System.NotImplementedException();
        }
    }
}
