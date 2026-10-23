using System;
using UnityEngine;

namespace Oyung.Sequences.Unity
{
    /// <summary>
    /// Sequences: needs nothing from other modules (actors meet their
    /// sequences through ActorId). Creates the store and binds every
    /// catalog sequence to it. Start lets them run.
    /// </summary>
    public class SequencesInstaller : MonoBehaviour, IDisposable
    {
        [SerializeField] private SequenceCatalog catalog;

        public void Build()
        {
            var store = new SequenceStore();
            foreach (var sequence in catalog.Items) sequence.Bind(store);
        }

        /// <summary>Start phase, after load: sequences whose condition
        /// is met, or that a save left halfway, run.</summary>
        public void StartListening()
        {
            foreach (var sequence in catalog.Items)
                sequence.StartListening();
        }

        public void Dispose()
        {
            foreach (var sequence in catalog.Items) sequence.Unbind();
        }
    }
}
