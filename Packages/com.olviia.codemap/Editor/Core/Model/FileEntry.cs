using System.Collections.Generic;

namespace Olviia.CodeMap.Core.Model
{
    /// <summary>Everything parsed from one .cs file. The unit of caching: when a file changes, only its entry is replaced.</summary>
    public sealed class FileEntry
    {
        /// <summary>Project-relative path with forward slashes, e.g. <c>Assets/Scripts/Combat/DamageSystem.cs</c>.</summary>
        public string Path { get; }

        /// <summary>Non-private types in source order.</summary>
        public IReadOnlyList<TypeEntry> Types { get; }

        /// <param name="path">Project-relative path.</param>
        /// <param name="types">Types in source order.</param>
        public FileEntry(string path, IReadOnlyList<TypeEntry> types)
        {
            Path = path;
            Types = types;
        }
    }
}
