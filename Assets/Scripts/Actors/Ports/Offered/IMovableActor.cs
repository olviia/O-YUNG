using System;
using Oyung.Locations;

namespace Oyung.Actors
{
    /// <summary>
    /// Offered port: an actor that can go places. How it gets there
    /// (walking, rolling) is its own business.
    /// </summary>
    public interface IMovableActor : IActor
    {
        /// <summary>Goes to the location, ends facing its way, then
        /// calls arrived once. A new order replaces the old one, whose
        /// arrived is never called. Throws if the location isn't loaded.
        /// </summary>
        void WalkTo(LocationId to, Action arrived);
    }
}
