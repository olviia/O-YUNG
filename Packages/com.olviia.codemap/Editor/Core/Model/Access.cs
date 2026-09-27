namespace Olviia.CodeMap.Core.Model
{
    /// <summary>C# access level of an indexed type or member. Private code is never indexed, so it has no value here.</summary>
    public enum Access
    {
        Public,
        Protected,
        ProtectedInternal,
        Internal,
        PrivateProtected
    }

    /// <summary>Rules derived from <see cref="Access"/>.</summary>
    public static class AccessExtensions
    {
        /// <summary>Whether code in another assembly can see it. Decides main index vs. module file.</summary>
        /// <param name="access">Effective access level.</param>
        /// <returns>True for public, protected and protected internal.</returns>
        public static bool IsVisibleOutsideAssembly(this Access access)
        {
            return access == Access.Public
                || access == Access.Protected
                || access == Access.ProtectedInternal;
        }
    }
}
