namespace Oyung.Globals
{
    /// <summary>
    /// What the installer needs from any global fact, whatever its value
    /// type: one list can hold GlobalFact&lt;bool&gt; and
    /// GlobalFact&lt;int&gt; through it.
    /// </summary>
    internal interface IGlobalFact
    {
        string Name { get; }

        /// <summary>Starts reading values from and listening to the
        /// store.</summary>
        void Bind(GlobalStore store);

        /// <summary>Stops listening; Value throws until bound again.
        /// </summary>
        void Unbind();
    }
}
