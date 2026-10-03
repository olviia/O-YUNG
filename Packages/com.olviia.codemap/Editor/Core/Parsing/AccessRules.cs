using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Olviia.CodeMap.Core.Model;

namespace Olviia.CodeMap.Core.Parsing
{
    /// <summary>C# accessibility rules: reading modifiers and combining a member's access with its container's.</summary>
    internal static class AccessRules
    {
        // How far an access level reaches, per audience: 2 = everyone, 1 = derived types only, 0 = nobody.
        private const int Everyone = 2;
        private const int Derived = 1;
        private const int Nobody = 0;

        /// <summary>Reads the declared access level from modifiers.</summary>
        /// <param name="modifiers">Modifiers of a type or member.</param>
        /// <param name="whenMissing">C# default when no access modifier is written.</param>
        /// <returns>The declared access level.</returns>
        public static Access FromModifiers(SyntaxTokenList modifiers, Access whenMissing)
        {
            bool isPublic = modifiers.Any(SyntaxKind.PublicKeyword);
            bool isProtected = modifiers.Any(SyntaxKind.ProtectedKeyword);
            bool isInternal = modifiers.Any(SyntaxKind.InternalKeyword);
            bool isPrivate = modifiers.Any(SyntaxKind.PrivateKeyword);

            if (isPublic) return Access.Public;
            if (isProtected && isInternal) return Access.ProtectedInternal;
            if (isProtected && isPrivate) return Access.PrivateProtected;
            if (isProtected) return Access.Protected;
            if (isInternal) return Access.Internal;
            if (isPrivate) return Access.Private;
            return whenMissing;
        }

        /// <summary>A member is never more visible than its container: combines both into the effective access level.</summary>
        /// <param name="container">Effective access of the containing type.</param>
        /// <param name="declared">Declared access of the member.</param>
        /// <returns>The narrower of the two, audience by audience.</returns>
        public static Access Narrow(Access container, Access declared)
        {
            (int inside, int outside) a = Reach(container);
            (int inside, int outside) b = Reach(declared);
            return FromReach(Math.Min(a.inside, b.inside), Math.Min(a.outside, b.outside));
        }

        // inside = code in the same assembly, outside = code in other assemblies.
        private static (int inside, int outside) Reach(Access access)
        {
            switch (access)
            {
                case Access.Public: return (Everyone, Everyone);
                case Access.ProtectedInternal: return (Everyone, Derived);
                case Access.Internal: return (Everyone, Nobody);
                case Access.Protected: return (Derived, Derived);
                case Access.PrivateProtected: return (Derived, Nobody);
                case Access.Private: return (Nobody, Nobody);
                default: throw new ArgumentOutOfRangeException(nameof(access), access, null);
            }
        }

        private static Access FromReach(int inside, int outside)
        {
            if (inside == Everyone)
                return outside == Everyone ? Access.Public : outside == Derived ? Access.ProtectedInternal : Access.Internal;
            if (inside == Nobody)
                return Access.Private;
            return outside == Derived ? Access.Protected : Access.PrivateProtected;
        }
    }
}
