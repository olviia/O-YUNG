using System.Collections.Generic;

namespace Olviia.CodeMap.Core.Model
{
    /// <summary>Parsed <c>///</c> documentation of one type or member.</summary>
    public sealed class DocComment
    {
        /// <summary>Shared instance for code without documentation.</summary>
        public static readonly DocComment None =
            new DocComment(string.Empty, new KeyValuePair<string, string>[0], string.Empty, false);

        /// <summary>Text of &lt;summary&gt; with whitespace collapsed; empty if missing.</summary>
        public string Summary { get; }

        /// <summary>Parameter name and description pairs, in declaration order.</summary>
        public IReadOnlyList<KeyValuePair<string, string>> Params { get; }

        /// <summary>Text of &lt;returns&gt;; empty if missing.</summary>
        public string Returns { get; }

        /// <summary>True when the comment is &lt;inheritdoc/&gt;: documented, but the text lives on the base member.</summary>
        public bool IsInherited { get; }

        /// <summary>True when there is neither a summary nor inherited documentation.</summary>
        public bool IsEmpty => Summary.Length == 0 && !IsInherited;

        /// <param name="summary">Summary text.</param>
        /// <param name="parameters">Parameter name and description pairs.</param>
        /// <param name="returns">Returns text.</param>
        /// <param name="isInherited">Whether the comment is &lt;inheritdoc/&gt;.</param>
        public DocComment(string summary, IReadOnlyList<KeyValuePair<string, string>> parameters, string returns, bool isInherited)
        {
            Summary = summary;
            Params = parameters;
            Returns = returns;
            IsInherited = isInherited;
        }
    }
}
