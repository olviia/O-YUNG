using System;
using Oyung.Actors;

namespace Oyung.Sequences.Unity
{
    // CLAUDE: class comment (your words). Draft: one thing a sequence
    // CLAUDE: asks its actor to do. Knows its own data. Does it two ways:
    // CLAUDE: Execute plays it out and calls done; JumpToEnd leaves the
    // CLAUDE: actor at once as the step would (loading a save, an actor
    // CLAUDE: streaming back in). Execute must work from wherever the
    // CLAUDE: actor stands, so a step can resume halfway.
    // CLAUDE: Each step asks for the capability it needs (Require).
    [Serializable]
    public abstract class SequenceStep
    {
        /// <summary>Plays the step out, then calls done once.</summary>
        public abstract void Execute(IActor actor, Action done);

        /// <summary>Leaves the actor as the step would, at once.
        /// </summary>
        public abstract void JumpToEnd(IActor actor);

        /// <summary>The actor as the capability this step needs; throws
        /// naming both if it lacks it.</summary>
        protected T Require<T>(IActor actor) where T : class, IActor =>
            actor as T ?? throw new InvalidOperationException(
                $"Actor '{actor.Id.name}' can't do {GetType().Name}: " +
                $"it is not an {typeof(T).Name}.");
    }
}
