using System;
using System.Collections;
using System.Collections.Generic;
using Oyung.Locomotion;
using UnityEngine;

namespace Oyung.Actors
{
    // CLAUDE: class comment (your words). Draft: a character in the world
    // CLAUDE: that sequences can direct. Knows its id, its Walker (how it
    // CLAUDE: moves) and its Animator (its poses). Registers on its id while
    // CLAUDE: enabled, so a sequence finds it the moment its chunk loads.
    // CLAUDE: Poses are the states in the controller's "Poses" sub-state
    // CLAUDE: machine; walking crosses back to the locomotion state.
    public sealed class Actor : MonoBehaviour, IMovableActor, IAnimatedActor
    {
        private const string PosesMachine = "Poses";

        [SerializeField] private ActorId id;
        [SerializeField] private Walker walker;
        [SerializeField] private Animator animator;
        [Tooltip("The state Gait drives: stand and walk.")]
        [SerializeField] private string locomotionState = "Locomotion";
        [Tooltip("Seconds to blend from a pose back into walking.")]
        [SerializeField] private float unposeFade = 0.5f;
        [Tooltip("Filled from the controller's Poses sub-state machine "
                 + "whenever this actor is inspected.")]
        [SerializeField] private string[] poses = Array.Empty<string>();

        private Action _arrived;
        private Coroutine _posing;

        public ActorId Id => id;

        public IReadOnlyList<string> Poses => poses;

        private void OnEnable()
        {
            // Checked here, not left to a NullReferenceException later:
            // OnEnable runs before the actor can be found and directed.
            Require(id, nameof(id));
            Require(walker, nameof(walker));
            Require(animator, nameof(animator));
            walker.Arrived += OnArrived;
            id.Bind(this);
        }

        private void OnDisable()
        {
            if (id != null) id.Unbind(this);
            if (walker != null) walker.Arrived -= OnArrived;
            StopPosing();
            _arrived = null;
        }

        public void WalkTo(Vector3 position, Quaternion facing,
            Action arrived)
        {
            StopPosing();
            if (!InState(locomotionState))
                animator.CrossFadeInFixedTime(locomotionState, unposeFade);
            _arrived = arrived;
            walker.WalkTo(position, facing);
        }

        public void JumpTo(Vector3 position, Quaternion facing)
        {
            _arrived = null;
            walker.Stop();
            transform.SetPositionAndRotation(position, facing);
        }

        public void Pose(string pose, float fade, Action done)
        {
            StopPosing();
            animator.CrossFadeInFixedTime(PoseHash(pose), fade);
            _posing = StartCoroutine(AfterFade(fade, done));
        }

        public void JumpToPose(string pose)
        {
            StopPosing();
            animator.Play(PoseHash(pose));
        }

        private void OnArrived()
        {
            var arrived = _arrived;
            _arrived = null;
            arrived?.Invoke();
        }

        private IEnumerator AfterFade(float fade, Action done)
        {
            yield return new WaitForSeconds(fade);
            _posing = null;
            done();
        }

        private void StopPosing()
        {
            if (_posing == null) return;
            StopCoroutine(_posing);
            _posing = null;
        }

        private bool InState(string state)
        {
            var hash = Animator.StringToHash(state);
            return animator.GetCurrentAnimatorStateInfo(0).shortNameHash
                       == hash
                   || animator.GetNextAnimatorStateInfo(0).shortNameHash
                       == hash;
        }

        private void Require(UnityEngine.Object part, string field)
        {
            if (part == null)
                throw new InvalidOperationException(
                    $"Actor '{name}' has no {field}; set it in the "
                    + "Inspector.");
        }

        // A short state name works inside a sub-state machine too.
        private int PoseHash(string pose)
        {
            var hash = Animator.StringToHash(pose);
            if (!animator.HasState(0, hash))
                throw new ArgumentException(
                    $"Actor '{name}' has no pose '{pose}'. Is it a state "
                    + $"in the '{PosesMachine}' sub-state machine?");
            return hash;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var controller = animator == null
                ? null
                : animator.runtimeAnimatorController
                    as UnityEditor.Animations.AnimatorController;
            if (controller == null) return;

            var found = new List<string>();
            foreach (var child in controller.layers[0].stateMachine
                         .stateMachines)
                if (child.stateMachine.name == PosesMachine)
                    foreach (var state in child.stateMachine.states)
                        found.Add(state.state.name);

            if (found.Count == poses.Length && !found.Exists(
                    p => Array.IndexOf(poses, p) < 0)) return;
            poses = found.ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
