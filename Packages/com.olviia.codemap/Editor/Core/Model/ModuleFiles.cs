using System.Collections.Generic;

namespace Olviia.CodeMap.Core.Model
{
    /// <summary>A module together with the parsed files it owns. Input for rendering.</summary>
    public sealed class ModuleFiles
    {
        /// <summary>The module.</summary>
        public ModuleInfo Module { get; }

        /// <summary>Its files, ordered by path.</summary>
        public IReadOnlyList<FileEntry> Files { get; }

        /// <param name="module">The module.</param>
        /// <param name="files">Its files, ordered by path.</param>
        public ModuleFiles(ModuleInfo module, IReadOnlyList<FileEntry> files)
        {
            Module = module;
            Files = files;
        }
    }
}
