using System;
using System.Collections.Generic;
using System.Linq;
using Olviia.CodeMap.Core.Model;

namespace Olviia.CodeMap.Core.Resolution
{
    /// <summary>
    /// Finds where an overriding or implementing member comes from, using only project code.
    /// Types are matched by short name, so two project types with the same name in different namespaces can be confused.
    /// </summary>
    public sealed class OverrideResolver
    {
        private readonly Dictionary<string, TypeShape> _types = new Dictionary<string, TypeShape>(StringComparer.Ordinal);

        /// <param name="files">Every parsed file of the project.</param>
        public OverrideResolver(IEnumerable<FileEntry> files)
        {
            foreach (FileEntry file in files)
            {
                foreach (TypeEntry type in file.Types)
                {
                    // Partial types are declared in several places; their parts are merged into one shape.
                    string key = SimpleName(type.Name);
                    if (!_types.TryGetValue(key, out TypeShape shape))
                        _types[key] = shape = new TypeShape(type.Name, type.Kind);
                    shape.BaseTypes.AddRange(type.BaseTypes);
                    shape.Members.AddRange(type.Members);
                }
            }
        }

        /// <summary>Describes the origin of a member for the index.</summary>
        /// <param name="owner">Type that declares the member.</param>
        /// <param name="member">The member.</param>
        /// <returns><c>override of X</c>, <c>override</c> (base outside the project), <c>implements IX</c>, or empty.</returns>
        public string OriginOf(TypeEntry owner, MemberEntry member)
        {
            // Explicit implementations already name their interface in the signature.
            if (!CanOverride(member.Kind) || member.ExplicitInterface.Length > 0 || owner.Kind == TypeKind.Interface)
                return string.Empty;

            if (member.IsOverride)
            {
                string overridden = FindOverridden(owner, member);
                return overridden == null ? "override" : "override of " + overridden;
            }

            List<string> interfaces = FindImplemented(owner, member);
            return interfaces.Count == 0 ? string.Empty : "implements " + string.Join(", ", interfaces);
        }

        // Walks up the base classes until one declares the member without "override" itself.
        private string FindOverridden(TypeEntry owner, MemberEntry member)
        {
            var visited = new HashSet<TypeShape>();
            TypeShape current = BaseClassOf(Shape(owner.Name));
            while (current != null && visited.Add(current))
            {
                MemberEntry match = current.Members.FirstOrDefault(m => Matches(m, member));
                if (match != null && !match.IsOverride)
                    return current.Name;
                current = BaseClassOf(current);
            }
            return null;
        }

        // Visits every base class and interface reachable from the owner and collects interfaces declaring the member.
        private List<string> FindImplemented(TypeEntry owner, MemberEntry member)
        {
            var result = new List<string>();
            var visited = new HashSet<TypeShape>();
            var pending = new Queue<TypeShape>(Parents(Shape(owner.Name)));
            while (pending.Count > 0)
            {
                TypeShape shape = pending.Dequeue();
                if (!visited.Add(shape))
                    continue;
                if (shape.Kind == TypeKind.Interface && shape.Members.Any(m => Matches(m, member)))
                    result.Add(shape.Name);
                foreach (TypeShape parent in Parents(shape))
                    pending.Enqueue(parent);
            }
            return result;
        }

        private IEnumerable<TypeShape> Parents(TypeShape shape)
        {
            if (shape == null)
                yield break;
            foreach (string baseType in shape.BaseTypes)
            {
                TypeShape parent = Shape(baseType);
                if (parent != null)
                    yield return parent;
            }
        }

        private TypeShape BaseClassOf(TypeShape shape)
        {
            return Parents(shape).FirstOrDefault(p => p.Kind == TypeKind.Class || p.Kind == TypeKind.Record);
        }

        private TypeShape Shape(string name)
        {
            _types.TryGetValue(SimpleName(name), out TypeShape shape);
            return shape;
        }

        private static bool Matches(MemberEntry candidate, MemberEntry member)
        {
            return candidate.Kind == member.Kind
                && candidate.Name == member.Name
                && candidate.ParameterTypes.SequenceEqual(member.ParameterTypes);
        }

        private static bool CanOverride(MemberKind kind)
        {
            return kind == MemberKind.Method || kind == MemberKind.Property || kind == MemberKind.Indexer || kind == MemberKind.Event;
        }

        // "Game.Combat.Repository<T>" and "Outer.Repository" both become "Repository".
        private static string SimpleName(string name)
        {
            int generic = name.IndexOf('<');
            if (generic >= 0)
                name = name.Substring(0, generic);
            int dot = name.LastIndexOf('.');
            return dot >= 0 ? name.Substring(dot + 1) : name;
        }

        // All parts of one type merged: what the resolver needs to walk the hierarchy.
        private sealed class TypeShape
        {
            public readonly string Name;
            public readonly TypeKind Kind;
            public readonly List<string> BaseTypes = new List<string>();
            public readonly List<MemberEntry> Members = new List<MemberEntry>();

            public TypeShape(string name, TypeKind kind)
            {
                Name = name;
                Kind = kind;
            }
        }
    }
}
