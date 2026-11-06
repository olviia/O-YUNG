using System;
using Oyung.Actors;

namespace Oyung.Sequences.Unity
{
    // CLAUDE: class comment (your words). Draft: one thing a sequence
    // CLAUDE: asks its actor to do. Knows its own data. Execute plays it
    // CLAUDE: out and calls done. It must work from wherever the actor
    // CLAUDE: stands: a step cut off by a save or a chunk unloading simply
    // CLAUDE: starts again.
    // CLAUDE: Each step asks for the capability it needs (Require).
    [Serializable]
    public abstract class SequenceStep
    {
        /// <summary>Plays the step out, then calls done once.</summary>
        public abstract void Execute(IActor actor, Action done);

        /// <summary>The actor as the capability this step needs; throws
        /// naming both if it lacks it.</summary>
        protected T Require<T>(IActor actor) where T : class, IActor =>
            actor as T ?? throw new InvalidOperationException(
                $"Actor '{actor.Id.name}' can't do {GetType().Name}: " +
                $"it is not an {typeof(T).Name}.");
    }
}
