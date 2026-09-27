using System.Collections.Generic;

namespace Olviia.CodeMap.Core.Model
{
    /// <summary>One type declared in a source file.</summary>
    public sealed class TypeEntry
    {
        /// <summary>Class, struct, interface and so on.</summary>
        public TypeKind Kind { get; }

        /// <summary>Effective access level; a nested type inside an internal type counts as internal.</summary>
        public Access Access { get; }

        /// <summary>Name with generic parameters; nested types are dotted, e.g. <c>Inventory.Slot&lt;T&gt;</c>.</summary>
        public string Name { get; }

        /// <summary>Base class and interfaces as written, e.g. <c>MonoBehaviour</c>, <c>IDamageable</c>.</summary>
        public IReadOnlyList<string> BaseTypes { get; }

        /// <summary>1-based line of the declaration.</summary>
        public int Line { get; }

        /// <summary>Documentation of the type.</summary>
        public DocComment Doc { get; }

        /// <summary>Non-private members in source order.</summary>
        public IReadOnlyList<MemberEntry> Members { get; }

        /// <param name="kind">Type kind.</param>
        /// <param name="access">Effective access level.</param>
        /// <param name="name">Display name.</param>
        /// <param name="baseTypes">Base class and interfaces.</param>
        /// <param name="line">1-based declaration line.</param>
        /// <param name="doc">Documentation.</param>
        /// <param name="members">Members in source order.</param>
        public TypeEntry(TypeKind kind, Access access, string name, IReadOnlyList<string> baseTypes,
            int line, DocComment doc, IReadOnlyList<MemberEntry> members)
        {
            Kind = kind;
            Access = access;
            Name = name;
            BaseTypes = baseTypes;
            Line = line;
            Doc = doc;
            Members = members;
        }
    }
}
