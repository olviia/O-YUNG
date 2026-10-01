using System;
using UnityEngine;

namespace Oyung.Vision.Unity
{
    /// <summary>
    /// Vision: needs nothing from other modules, offers IVision.
    /// </summary>
    public class VisionInstaller : MonoBehaviour, IDisposable
    {
        [SerializeField] private DepthOfFieldBlurDisplay depthOfFieldBlurDisplay;

        public IVision Build()
        {
            return new Vision(depthOfFieldBlurDisplay);
        }

        public void Dispose() { /* nothing to clean yet */ }
    }
}
