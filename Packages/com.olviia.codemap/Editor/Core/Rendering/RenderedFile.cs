namespace Olviia.CodeMap.Core.Rendering
{
    /// <summary>One output document, ready to be written to disk.</summary>
    public sealed class RenderedFile
    {
        /// <summary>Path relative to the output folder, e.g. <c>INDEX.md</c> or <c>index/Game.Combat.md</c>.</summary>
        public string RelativePath { get; }

        /// <summary>Markdown content with <c>\n</c> line endings.</summary>
        public string Content { get; }

        /// <param name="relativePath">Path relative to the output folder.</param>
        /// <param name="content">Markdown content.</param>
        public RenderedFile(string relativePath, string content)
        {
            RelativePath = relativePath;
            Content = content;
        }
    }
}
