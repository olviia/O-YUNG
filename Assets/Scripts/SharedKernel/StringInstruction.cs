using System;

namespace Oyung.SharedKernel
{
    /// <summary>
    /// Shared Kernel contract: an order that carries a text only known
    /// while playing ("remember this name"). Like Instruction, each state
    /// owner subclasses it on its own .Unity side, and the decider holds it
    /// inline without knowing which module carries it out.
    /// </summary>
    [Serializable]
    public abstract class StringInstruction
    {
        /// <summary>Carries out the order. Strict: throws naming the asset
        /// if it can't be done.</summary>
        public abstract void Execute(string value);
    }
}
