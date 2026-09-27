using System;
using System.Linq;
using UnityEditor;

namespace Olviia.CodeMap.Editor
{
    /// <summary>Unity's import hook: after every import, passes changed source files to <see cref="CodeMapRunner"/>.</summary>
    internal sealed class CodeMapPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] movedTo, string[] movedFrom)
        {
            string[] changed = imported.Concat(movedTo).Where(CodeMapRunner.IsIndexedSource).ToArray();
            string[] removed = deleted.Concat(movedFrom).Where(CodeMapRunner.IsIndexedSource).ToArray();
            // Assembly definition changes move files between modules without touching any source file.
            bool modulesChanged = imported.Concat(deleted).Concat(movedTo).Concat(movedFrom).Any(IsAssemblyDefinition);

            if (changed.Length > 0 || removed.Length > 0 || modulesChanged)
                CodeMapRunner.Update(changed, removed);
        }

        private static bool IsAssemblyDefinition(string path)
        {
            return path.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".asmref", StringComparison.OrdinalIgnoreCase);
        }
    }
}
