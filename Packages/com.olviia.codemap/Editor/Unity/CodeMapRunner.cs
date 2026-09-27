using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Olviia.CodeMap.Core;
using Olviia.CodeMap.Core.Model;
using Olviia.CodeMap.Core.Parsing;
using Olviia.CodeMap.Core.Rendering;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Olviia.CodeMap.Editor
{
    /// <summary>
    /// Connects Core to the editor: reads sources and assembly definitions, runs the generator, writes the index and the cache.
    /// Builds the index once after load when it or the cache is missing.
    /// </summary>
    [InitializeOnLoad]
    internal static class CodeMapRunner
    {
        private const string SourceRoot = "Assets";
        private const string OutputFolder = "docs";
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        static CodeMapRunner()
        {
            if (!File.Exists(Path.Combine(OutputFolder, MarkdownRenderer.MainFileName)) || CacheStore.Load() == null)
                EditorApplication.delayCall += RebuildAll;
        }

        /// <summary>Whether a path is a source file CodeMap indexes: a .cs file under Assets, outside folders Unity ignores.</summary>
        /// <param name="path">Project-relative asset path.</param>
        /// <returns>True for indexed source files.</returns>
        public static bool IsIndexedSource(string path)
        {
            string normalized = path.Replace('\\', '/');
            if (!normalized.StartsWith(SourceRoot + "/", StringComparison.OrdinalIgnoreCase) || !normalized.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                return false;
            // Unity skips folders starting with "." or ending with "~" (e.g. Samples~).
            return !normalized.Split('/').Any(segment => segment.StartsWith(".") || segment.EndsWith("~"));
        }

        /// <summary>Applies changed and removed files to the cached state and re-renders. Falls back to a full rebuild without a cache.</summary>
        /// <param name="changedPaths">Created, modified or moved-to source files.</param>
        /// <param name="removedPaths">Deleted or moved-from source files.</param>
        public static void Update(IReadOnlyCollection<string> changedPaths, IReadOnlyCollection<string> removedPaths)
        {
            IReadOnlyList<FileEntry> cached = CacheStore.Load();
            if (cached == null)
            {
                RebuildAll();
                return;
            }

            Run(() =>
            {
                var generator = new CodeMapGenerator(CreateParser(), cached);
                foreach (string path in removedPaths)
                    generator.Remove(path);
                foreach (string path in changedPaths.Where(File.Exists))
                    generator.Update(path, File.ReadAllText(path));
                Finish(generator);
            });
        }

        /// <summary>Parses every source file from scratch and re-renders.</summary>
        [MenuItem("Tools/CodeMap/Rebuild Index")]
        public static void RebuildAll()
        {
            Run(() =>
            {
                Stopwatch timer = Stopwatch.StartNew();
                var generator = new CodeMapGenerator(CreateParser(), new FileEntry[0]);
                string[] sources = Directory.EnumerateFiles(SourceRoot, "*.cs", SearchOption.AllDirectories)
                    .Select(p => p.Replace('\\', '/'))
                    .Where(IsIndexedSource)
                    .ToArray();
                foreach (string path in sources)
                    generator.Update(path, File.ReadAllText(path));
                Finish(generator);
                Debug.Log("CodeMap: indexed " + sources.Length + " files in " + timer.ElapsedMilliseconds + " ms.");
            });
        }

        private static void Finish(CodeMapGenerator generator)
        {
            IReadOnlyList<RenderedFile> rendered = generator.Render(AsmdefReader.ReadModules(SourceRoot), AsmdefReader.RuntimeFallback, AsmdefReader.EditorFallback);
            Write(rendered);
            CacheStore.Save(generator.Files);
        }

        // Writes only files whose content changed and removes module files of modules that no longer exist.
        private static void Write(IReadOnlyList<RenderedFile> rendered)
        {
            foreach (RenderedFile file in rendered)
            {
                string path = Path.Combine(OutputFolder, file.RelativePath);
                if (File.Exists(path) && File.ReadAllText(path) == file.Content)
                    continue;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, file.Content, Utf8NoBom);
            }

            string moduleFolder = Path.Combine(OutputFolder, MarkdownRenderer.ModuleFolder);
            if (!Directory.Exists(moduleFolder))
                return;
            var current = new HashSet<string>(rendered.Select(f => Path.GetFullPath(Path.Combine(OutputFolder, f.RelativePath))), StringComparer.OrdinalIgnoreCase);
            foreach (string stale in Directory.GetFiles(moduleFolder, "*.md").Where(p => !current.Contains(Path.GetFullPath(p))))
                File.Delete(stale);
        }

        // Parses #if blocks the way the editor compiles them right now.
        private static SourceParser CreateParser()
        {
            return new SourceParser(EditorUserBuildSettings.activeScriptCompilationDefines);
        }

        // The index is a helper; a failure must never break the import it runs in.
        private static void Run(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
