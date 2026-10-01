using System;
using Oyung.Input;
using UnityEngine;

namespace Oyung.Eyes
{
    /// Assembles the Eyes module. Consumer only: needs input, offers nothing yet.
    public class EyesInstaller : MonoBehaviour, IDisposable
    {
        // Inspector reference INSIDE the module boundary. That's fine.
        [SerializeField] private LookCamera lookCamera;

        /// Its parameter list *is* its needs list. Nothing hidden.
        /// Offers nothing → returns nothing.
        public void Build(IGameInput input)
        {
            lookCamera.Init(input);
        }

        public void Dispose() { /* nothing to clean yet */ }
    }
}