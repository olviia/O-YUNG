using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Olviia.CodeMap.Core.Model;
using UnityEditor;
using UnityEngine;

namespace Olviia.CodeMap.Editor
{
    /// <summary>Reads the project's .asmdef and .asmref files into modules.</summary>
    internal static class AsmdefReader
    {
        private const string GuidPrefix = "GUID:";

        /// <summary>Unity's default assembly for scripts outside any assembly definition.</summary>
        public static readonly ModuleInfo RuntimeFallback = new ModuleInfo("Assembly-CSharp", new[] { "Assets" }, new string[0], false);

        /// <summary>Unity's default assembly for such scripts inside an "Editor" folder.</summary>
        public static readonly ModuleInfo EditorFallback = new ModuleInfo("Assembly-CSharp-Editor", new[] { "Assets" }, new string[0], false);

        /// <summary>Reads every assembly definition under a folder.</summary>
        /// <param name="searchRoot">Folder to search, e.g. <c>Assets</c>.</param>
        /// <returns>One module per .asmdef, with .asmref folders added to their target module and references resolved to names.</returns>
        public static IReadOnlyList<ModuleInfo> ReadModules(string searchRoot)
        {
            var roots = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var references = new Dictionary<string, string[]>(StringComparer.Ordinal);
            var engineFree = new HashSet<string>(StringComparer.Ordinal);

            foreach (string path in FindAssets("t:AssemblyDefinitionAsset", searchRoot))
            {
                AsmdefJson asmdef = JsonUtility.FromJson<AsmdefJson>(File.ReadAllText(path));
                roots[asmdef.name] = new List<string> { FolderOf(path) };
                references[asmdef.name] = asmdef.references ?? new string[0];
                if (asmdef.noEngineReferences)
                    engineFree.Add(asmdef.name);
            }

            foreach (string path in FindAssets("t:AssemblyDefinitionReferenceAsset", searchRoot))
            {
                string target = ResolveName(JsonUtility.FromJson<AsmrefJson>(File.ReadAllText(path)).reference);
                if (roots.TryGetValue(target, out List<string> folders))
                    folders.Add(FolderOf(path));
            }

            return roots
                .Select(pair => new ModuleInfo(pair.Key, pair.Value, references[pair.Key].Select(ResolveName).ToList(), engineFree.Contains(pair.Key)))
                .ToList();
        }

        // Unity writes references either as assembly names or as "GUID:<guid>" of the referenced .asmdef.
        private static string ResolveName(string reference)
        {
            if (string.IsNullOrEmpty(reference) || !reference.StartsWith(GuidPrefix, StringComparison.Ordinal))
                return reference;

            string path = AssetDatabase.GUIDToAssetPath(reference.Substring(GuidPrefix.Length));
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return reference;
            return JsonUtility.FromJson<AsmdefJson>(File.ReadAllText(path)).name;
        }

        private static IEnumerable<string> FindAssets(string filter, string searchRoot)
        {
            return AssetDatabase.FindAssets(filter, new[] { searchRoot }).Select(AssetDatabase.GUIDToAssetPath);
        }

        private static string FolderOf(string path)
        {
            return Path.GetDirectoryName(path).Replace('\\', '/');
        }

        // Field names must match the JSON keys Unity writes.
#pragma warning disable 0649
        [Serializable]
        private sealed class AsmdefJson
        {
            public string name;
            public string[] references;
            public bool noEngineReferences;
        }

        [Serializable]
        private sealed class AsmrefJson
        {
            public string reference;
        }
#pragma warning restore 0649
    }
}
