using System;
using System.Collections.Generic;
using System.Linq;
using Olviia.CodeMap.Core.Model;

namespace Olviia.CodeMap.Core.Modules
{
    /// <summary>Finds the module that owns a source file, using Unity's rule: the closest folder with an assembly definition wins.</summary>
    public sealed class ModuleResolver
    {
        private const string EditorFolder = "Editor";

        // Every owned folder paired with its module, deepest folders first, so the first match is the closest one.
        private readonly IReadOnlyList<KeyValuePair<string, ModuleInfo>> _roots;
        private readonly ModuleInfo _runtimeFallback;
        private readonly ModuleInfo _editorFallback;

        /// <param name="modules">Modules from .asmdef files; their <see cref="ModuleInfo.RootPaths"/> include .asmref folders.</param>
        /// <param name="runtimeFallback">Module for files outside any assembly definition (Assembly-CSharp).</param>
        /// <param name="editorFallback">Module for such files inside an "Editor" folder (Assembly-CSharp-Editor).</param>
        public ModuleResolver(IReadOnlyList<ModuleInfo> modules, ModuleInfo runtimeFallback, ModuleInfo editorFallback)
        {
            _roots = modules
                .SelectMany(module => module.RootPaths.Select(root => new KeyValuePair<string, ModuleInfo>(Normalize(root).TrimEnd('/') + "/", module)))
                .OrderByDescending(pair => pair.Key.Length)
                .ToList();
            _runtimeFallback = runtimeFallback;
            _editorFallback = editorFallback;
        }

        /// <summary>Finds the owning module of a file.</summary>
        /// <param name="filePath">Project-relative path, e.g. <c>Assets/Scripts/Combat/DamageSystem.cs</c>.</param>
        /// <returns>The module with the closest root folder, or a fallback if no root contains the file.</returns>
        public ModuleInfo Resolve(string filePath)
        {
            string path = Normalize(filePath);
            foreach (KeyValuePair<string, ModuleInfo> root in _roots)
            {
                if (path.StartsWith(root.Key, StringComparison.OrdinalIgnoreCase))
                    return root.Value;
            }
            return IsInEditorFolder(path) ? _editorFallback : _runtimeFallback;
        }

        private static bool IsInEditorFolder(string path)
        {
            string[] segments = path.Split('/');
            // The last segment is the file name, not a folder.
            for (int i = 0; i < segments.Length - 1; i++)
            {
                if (string.Equals(segments[i], EditorFolder, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static string Normalize(string path)
        {
            return path.Replace('\\', '/');
        }
    }
}
