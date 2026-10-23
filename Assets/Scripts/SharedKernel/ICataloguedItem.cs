namespace Oyung.SharedKernel
{
    // CLAUDE: draft comment, rewrite in your words.
    /// <summary>
    /// Shared Kernel contract: an asset a catalog holds and finds by its
    /// stable name; saves and dialogue use the same name. Unique within
    /// its catalog.
    /// </summary>
    public interface ICataloguedItem
    {
        string Name { get; }
    }
}
