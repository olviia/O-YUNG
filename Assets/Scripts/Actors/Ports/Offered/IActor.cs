namespace Oyung.Actors
{
    /// <summary>
    /// Offered port: something in the world that can be directed. Says
    /// only who it is; what it can do comes from the capability ports
    /// it also implements (IMovableActor, IAnimatedActor).
    /// </summary>
    public interface IActor
    {
        /// <summary>The id it is found by.</summary>
        ActorId Id { get; }
    }
}
