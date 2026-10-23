namespace Oyung.SharedKernel
{
    // CLAUDE: draft comment, rewrite in your words.
    /// <summary>
    /// Shared Kernel contract: anything known by a stable name that saves,
    /// dialogue and catalogs look it up by. Unique within its kind.
    /// </summary>
    public interface INamed
    {
        string Name { get; }
    }
}
