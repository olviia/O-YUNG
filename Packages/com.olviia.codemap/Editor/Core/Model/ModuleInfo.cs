using System.Collections.Generic;

namespace Olviia.CodeMap.Core.Model
{
    /// <summary>One assembly of the project, shown as a module in the index.</summary>
    public sealed class ModuleInfo
    {
        /// <summary>Assembly name, e.g. <c>Game.Core.Combat</c> or <c>Assembly-CSharp</c>.</summary>
        public string Name { get; }

        /// <summary>Folders owned by the module: the .asmdef folder plus any .asmref folders. Project-relative.</summary>
        public IReadOnlyList<string> RootPaths { get; }

        /// <summary>Names of referenced modules, already resolved from GUIDs.</summary>
        public IReadOnlyList<string> References { get; }

        /// <param name="name">Assembly name.</param>
        /// <param name="rootPaths">Owned folders.</param>
        /// <param name="references">Referenced module names.</param>
        public ModuleInfo(string name, IReadOnlyList<string> rootPaths, IReadOnlyList<string> references)
        {
            Name = name;
            RootPaths = rootPaths;
            References = references;
        }
    }
}
