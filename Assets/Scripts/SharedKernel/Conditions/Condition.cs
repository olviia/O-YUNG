using System;

namespace Oyung.SharedKernel
{
    /// <summary>
    /// Shared Kernel contract: a question about some module's state
    /// ("is the intro seen?", "has 3 flowers?"). Knows nothing itself;
    /// each state owner subclasses it in its own .Unity side to ask
    /// about its own data. Used by deciders (Story, NPC AI), which
    /// hold it inline and never know which module answers.
    /// </summary>
    [Serializable]
    public abstract class Condition
    {
        /// <summary>True when the condition holds right now. Cheap, no
        /// side effects. Throws if the provider isn't bound.</summary>
        public abstract bool IsMet { get; }

        // CLAUDE: draft comment, rewrite in your words.
        /// <summary>Raised when a value the condition reads has changed;
        /// ask <see cref="IsMet"/> again. The answer itself may stay the
        /// same. Subscribe with a method, not a lambda, so you can
        /// unsubscribe (-=) when you stop caring.</summary>
        public abstract event Action Changed;
    }
}
