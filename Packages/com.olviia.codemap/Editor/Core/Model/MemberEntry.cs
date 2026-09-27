using System.Collections.Generic;

namespace Olviia.CodeMap.Core.Model
{
    /// <summary>One member of a type as it appears in the index.</summary>
    public sealed class MemberEntry
    {
        /// <summary>Constructor, method, property and so on.</summary>
        public MemberKind Kind { get; }

        /// <summary>Effective access level.</summary>
        public Access Access { get; }

        /// <summary>Bare name used to match overrides, e.g. <c>Save</c>.</summary>
        public string Name { get; }

        /// <summary>Parameter types as written, used to tell overloads apart; empty for non-callable members.</summary>
        public IReadOnlyList<string> ParameterTypes { get; }

        /// <summary>Full signature without access modifier, attributes or body, e.g. <c>bool TryApply(IDamageable target, int amount)</c>.</summary>
        public string Signature { get; }

        /// <summary>True when declared with the <c>override</c> keyword.</summary>
        public bool IsOverride { get; }

        /// <summary>Interface name for explicit implementations such as <c>void ISaveable.Save()</c>; empty otherwise.</summary>
        public string ExplicitInterface { get; }

        /// <summary>1-based line of the declaration.</summary>
        public int Line { get; }

        /// <summary>Documentation of the member.</summary>
        public DocComment Doc { get; }

        /// <param name="kind">Member kind.</param>
        /// <param name="access">Effective access level.</param>
        /// <param name="name">Bare name.</param>
        /// <param name="parameterTypes">Parameter types as written.</param>
        /// <param name="signature">Display signature.</param>
        /// <param name="isOverride">Whether it has the override keyword.</param>
        /// <param name="explicitInterface">Explicitly implemented interface, or empty.</param>
        /// <param name="line">1-based declaration line.</param>
        /// <param name="doc">Documentation.</param>
        public MemberEntry(MemberKind kind, Access access, string name, IReadOnlyList<string> parameterTypes,
            string signature, bool isOverride, string explicitInterface, int line, DocComment doc)
        {
            Kind = kind;
            Access = access;
            Name = name;
            ParameterTypes = parameterTypes;
            Signature = signature;
            IsOverride = isOverride;
            ExplicitInterface = explicitInterface;
            Line = line;
            Doc = doc;
        }
    }
}
