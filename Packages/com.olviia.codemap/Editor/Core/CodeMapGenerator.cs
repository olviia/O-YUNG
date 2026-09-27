using System;
using System.Collections.Generic;
using System.Linq;
using Olviia.CodeMap.Core.Model;
using Olviia.CodeMap.Core.Modules;
using Olviia.CodeMap.Core.Parsing;
using Olviia.CodeMap.Core.Rendering;
using Olviia.CodeMap.Core.Resolution;

namespace Olviia.CodeMap.Core
{
    /// <summary>
    /// Entry point of Core. Keeps the parsed state of every source file, updates it file by file and renders the index.
    /// Does no file or editor I/O: the host passes text in and writes the results out.
    /// </summary>
    public sealed class CodeMapGenerator
    {
        private readonly SourceParser _parser;
        private readonly MarkdownRenderer _renderer = new MarkdownRenderer();
        private readonly Dictionary<string, FileEntry> _files = new Dictionary<string, FileEntry>(StringComparer.OrdinalIgnoreCase);

        /// <param name="parser">Parser used for changed files.</param>
        /// <param name="cachedFiles">Previously parsed files, e.g. loaded from a cache; empty for a full rebuild.</param>
        public CodeMapGenerator(SourceParser parser, IEnumerable<FileEntry> cachedFiles)
        {
            _parser = parser;
            foreach (FileEntry file in cachedFiles)
                _files[Normalize(file.Path)] = file;
        }

        /// <summary>Current parsed state of all files; what the host saves as cache.</summary>
        public IReadOnlyCollection<FileEntry> Files => _files.Values;

        /// <summary>Parses a new or changed file and replaces its previous entry.</summary>
        /// <param name="path">Project-relative path.</param>
        /// <param name="sourceText">Current file contents.</param>
        public void Update(string path, string sourceText)
        {
            string key = Normalize(path);
            _files[key] = _parser.Parse(key, sourceText);
        }

        /// <summary>Forgets a deleted file, or the old path of a moved one.</summary>
        /// <param name="path">Project-relative path.</param>
        public void Remove(string path)
        {
            _files.Remove(Normalize(path));
        }

        /// <summary>Renders the index from the current state.</summary>
        /// <param name="modules">Modules read from assembly definitions.</param>
        /// <param name="runtimeFallback">Module for files outside any assembly definition.</param>
        /// <param name="editorFallback">Module for such files inside an "Editor" folder.</param>
        /// <returns>All output documents; the fallback modules are included only when they own files.</returns>
        public IReadOnlyList<RenderedFile> Render(IReadOnlyList<ModuleInfo> modules, ModuleInfo runtimeFallback, ModuleInfo editorFallback)
        {
            var resolver = new ModuleResolver(modules, runtimeFallback, editorFallback);
            ILookup<ModuleInfo, FileEntry> filesByModule = _files.Values.ToLookup(f => resolver.Resolve(f.Path));

            // Named modules alphabetically, then the fallbacks; files by path. Stable order keeps git diffs clean.
            IEnumerable<ModuleInfo> ordered = modules.OrderBy(m => m.Name, StringComparer.Ordinal)
                .Concat(new[] { runtimeFallback, editorFallback }.Where(filesByModule.Contains));

            List<ModuleFiles> moduleFiles = ordered
                .Select(m => new ModuleFiles(m, filesByModule[m].OrderBy(f => f.Path, StringComparer.Ordinal).ToList()))
                .ToList();

            return _renderer.Render(moduleFiles, new OverrideResolver(_files.Values));
        }

        private static string Normalize(string path)
        {
            return path.Replace('\\', '/');
        }
    }
}
