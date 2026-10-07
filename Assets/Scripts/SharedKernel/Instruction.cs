using System;

namespace Oyung.SharedKernel
{
    /// <summary>
    /// Shared Kernel contract: an order to change some module's state
    /// ("set intro_seen", "remove 5 flowers"). Knows nothing itself;
    /// each state owner subclasses it in its own .Unity side. Used by
    /// deciders (Story, NPC AI), which hold it inline and never know
    /// which module carries it out.
    /// </summary>
    [Serializable]
    public abstract class Instruction
    {
        /// <summary>Carries out the order. Strict: if it can't be done
        /// (precondition broken), throws naming the asset. To check
        /// first, the designer pairs it with a Condition.</summary>
        public abstract void Execute();
    }
}
