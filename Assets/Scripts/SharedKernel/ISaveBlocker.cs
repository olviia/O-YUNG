namespace Oyung.SharedKernel
{
    /// <summary>
    /// Shared Kernel contract: lets any module say "saving is not possible
    /// right now" without knowing the save module. Named locks: saving is
    /// allowed only while no reason is held, and one module's Unblock never
    /// cancels another's Block.
    /// </summary>
    public interface ISaveBlocker
    {
        /// <summary>Holds a block under this reason, e.g. "cutscene". The
        /// same reason may be held more than once.</summary>
        void Block(string reason);

        /// <summary>Releases one block held under this reason. Throws if
        /// none is held: an unbalanced pair is a bug.</summary>
        void Unblock(string reason);
    }
}
