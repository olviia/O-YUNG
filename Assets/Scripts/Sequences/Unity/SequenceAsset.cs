using System;
using Oyung.Actors;
using System.Collections.Generic;
using Oyung.SharedKernel;
using Oyung.SharedKernel.Unity;
using UnityEngine;

namespace Oyung.Sequences.Unity
{
    // CLAUDE: class comment (your words). Draft: one scripted sequence:
    // CLAUDE: which actor, when it starts, its steps in order. Value =
    // CLAUDE: finished (proxy to the store), read by others through
    // CLAUDE: BoolCondition. Starts once its condition is met, then runs
    // CLAUDE: whenever its actor is loaded: steps already done jump to
    // CLAUDE: their end, the current one plays. The player can't stop it.
    [CreateAssetMenu(menuName = "Oyung/Sequences/Sequence",
        fileName = "NewSequence")]
    public sealed class SequenceAsset : ModuleFact<bool>
    {
        // For the inspector tools, which find the actor by this field.
        internal const string ActorField = nameof(actor);

        [SerializeField] private ActorId actor;

        [SerializeReference] private Condition startWhen;

        [SerializeReference] private List<SequenceStep> steps =
            new List<SequenceStep>();

        private SequenceStore _store;
        private bool _started;
        // Which run of a step may report done. A step whose actor left
        // and came back is run again; the old run's done is ignored.
        private int _run;

        public override bool Value => Store.StepOf(Name) >= steps.Count;

        // CLAUDE: create phase: Value works, nothing runs yet.
        internal void Bind(SequenceStore store) => _store = store;

        // CLAUDE: start phase (after load). A sequence a save left
        // CLAUDE: halfway carries on without asking its condition again.
        internal void StartListening()
        {
            StartWhen.Changed += OnStartWhenChanged;
            OnStartWhenChanged();
        }

        internal void Unbind()
        {
            if (startWhen != null) startWhen.Changed -= OnStartWhenChanged;
            if (actor != null) actor.Changed -= OnActorChanged;
            _started = false;
            _run++;
            _store = null;
        }

        private void OnStartWhenChanged()
        {
            if (_started) return;
            if (Store.StepOf(Name) == 0 && !StartWhen.IsMet) return;
            _started = true;
            StartWhen.Changed -= OnStartWhenChanged;
            Actor.Changed += OnActorChanged;
            OnActorChanged();
        }

        // CLAUDE: runs when the actor streams in or out. Out: the step
        // CLAUDE: in progress is dropped; in: catch up and play on.
        // CLAUDE: Known gap: a finished sequence puts its actor back at
        // CLAUDE: its end each time the actor streams in, even if a later
        // CLAUDE: sequence moved it on. Saved actor state will own this.
        private void OnActorChanged()
        {
            _run++;
            var current = Actor.Current;
            if (current == null) return;
            var at = Store.StepOf(Name);
            for (var i = 0; i < at && i < steps.Count; i++)
                Step(i).JumpToEnd(current);
            Run(current, at);
        }

        private void Run(IActor current, int index)
        {
            if (index >= steps.Count) return;
            var run = ++_run;
            Step(index).Execute(current,
                () => OnStepDone(current, index, run));
        }

        private void OnStepDone(IActor current, int index, int run)
        {
            if (run != _run) return;
            Store.SetStep(Name, index + 1);
            if (index + 1 >= steps.Count) RaiseChanged();
            else Run(current, index + 1);
        }

        private SequenceStep Step(int index) => steps[index] ??
            throw new InvalidOperationException(
                $"Sequence '{Name}' has an empty step {index}.");

        private Condition StartWhen => startWhen ??
            throw new InvalidOperationException(
                $"Sequence '{Name}' has no Start-when condition.");

        private ActorId Actor => actor ??
            throw new InvalidOperationException(
                $"Sequence '{Name}' has no actor.");

        private SequenceStore Store => _store ??
            throw new InvalidOperationException(
                $"Sequence '{Name}' is not bound. Is it in the catalog?");
    }
}
