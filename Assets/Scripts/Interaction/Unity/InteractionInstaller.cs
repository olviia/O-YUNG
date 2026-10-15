using System;
using UnityEngine;

namespace Oyung.Interaction.Unity
{
    /// <summary>
    /// Interaction: needs the interact press. Interactables reach other
    /// modules through their conditions and instructions; offers
    /// nothing yet.
    /// </summary>
    public class InteractionInstaller : MonoBehaviour, IDisposable
    {
        [SerializeField] private GazeInteractor gazeInteractor;

        public void Build(IInteractionInput input) =>
            gazeInteractor.Init(input);

        public void Dispose() => gazeInteractor.Release();
    }
}
