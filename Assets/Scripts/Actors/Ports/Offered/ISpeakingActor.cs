using System;

namespace Oyung.Actors
{
    /// <summary>
    /// Offered port: an actor that can speak. How it sounds is its own
    /// business.
    /// </summary>
    public interface ISpeakingActor : IActor
    {
        /// <summary>Speaks the line, then calls done (may be null) once.
        /// A new line replaces the old one, whose done is never called.
        /// A line with no sayable words is done at once.</summary>
        void Say(string text, Action done);
    }
}
