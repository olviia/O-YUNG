using System;
using System.Collections.Generic;
using System.Linq;
using Oyung.SharedKernel;
using UnityEngine;
using UnityEngine.Localization;

namespace Oyung.Interaction.Unity
{
    // CLAUDE: makes a scene object interactable. Knows: its colliders
    // CLAUDE: (on it and its children: the parents are two bodies), its
    // CLAUDE: range, and its interactions as the designer writes them.
    // CLAUDE: Used by GazeInteractor (finds it, measures it) and the
    // CLAUDE: core Interactor (asks what it offers).
    // CLAUDE: Colliders must be primitive or convex: the nearest point
    // CLAUDE: comes from Collider.ClosestPoint.
    [AddComponentMenu("Oyung/Interaction/Interactable")]
    public sealed class Interactable : MonoBehaviour, IInteractable
    {
        [Tooltip("Metres from the eyes to its nearest point.")]
        [SerializeField] private float range = 3f;

        [Tooltip("In priority order: the first available one is what " +
                 "the prompt offers.")]
        [SerializeField] private List<InteractionData> interactions =
            new List<InteractionData>();

        private Collider[] _colliders;

        IReadOnlyList<IInteraction> IInteractable.Interactions =>
            interactions;

        internal float Range => range;

        internal IReadOnlyList<Collider> Colliders => _colliders;

        // CLAUDE: designer mistakes fail at startup, naming the object.
        private void Awake()
        {
            _colliders = GetComponentsInChildren<Collider>();
            if (_colliders.Length == 0)
                throw Invalid("has no collider on it or its children.");
            if (interactions.Count == 0)
                throw Invalid("has no interactions.");
            foreach (var interaction in interactions)
                interaction.Check(this);
        }

        private InvalidOperationException Invalid(string problem) =>
            new InvalidOperationException($"Interactable '{name}' {problem}");

        [Serializable]
        internal sealed class InteractionData : IInteraction
        {
            [Tooltip("What the prompt says, e.g. Listen, Open, Pet.")]
            [SerializeField] private LocalizedString verb;

            [Tooltip("Empty: always available.")]
            [SerializeReference] private Condition availableWhen;

            [SerializeReference] private List<Instruction> onUse =
                new List<Instruction>();

            internal LocalizedString Verb => verb;

            public Condition AvailableWhen => availableWhen;

            public IReadOnlyList<Instruction> OnUse => onUse;

            internal void Check(Interactable owner)
            {
                if (verb == null || verb.IsEmpty)
                    throw owner.Invalid("has an interaction without a verb.");
                if (onUse.Any(i => i == null))
                    throw owner.Invalid(
                        "has an empty On-use instruction slot.");
            }
        }
    }
}
