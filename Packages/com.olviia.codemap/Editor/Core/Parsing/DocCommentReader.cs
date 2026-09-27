using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Olviia.CodeMap.Core.Model;

namespace Olviia.CodeMap.Core.Parsing
{
    /// <summary>Extracts <c>///</c> documentation (summary, params, returns, inheritdoc) from a declaration.</summary>
    internal static class DocCommentReader
    {
        /// <summary>Reads the doc comment written directly above a declaration.</summary>
        /// <param name="declaration">Type or member declaration.</param>
        /// <returns>The parsed comment, or <see cref="DocComment.None"/> if there is none.</returns>
        public static DocComment Read(SyntaxNode declaration)
        {
            DocumentationCommentTriviaSyntax doc = null;
            foreach (SyntaxTrivia trivia in declaration.GetLeadingTrivia())
            {
                // The comment closest to the declaration wins.
                if (trivia.GetStructure() is DocumentationCommentTriviaSyntax structure)
                    doc = structure;
            }
            if (doc == null)
                return DocComment.None;

            string summary = string.Empty;
            string returns = string.Empty;
            bool inherited = false;
            var parameters = new List<KeyValuePair<string, string>>();

            foreach (XmlNodeSyntax node in doc.Content)
            {
                if (node is XmlElementSyntax element)
                {
                    switch (element.StartTag.Name.LocalName.Text)
                    {
                        case "summary": summary = ContentText(element.Content); break;
                        case "returns": returns = ContentText(element.Content); break;
                        case "param": parameters.Add(new KeyValuePair<string, string>(AttributeValue(element.StartTag.Attributes), ContentText(element.Content))); break;
                        case "inheritdoc": inherited = true; break;
                    }
                }
                else if (node is XmlEmptyElementSyntax empty && empty.Name.LocalName.Text == "inheritdoc")
                {
                    inherited = true;
                }
            }
            return new DocComment(summary, parameters, returns, inherited);
        }

        // Flattens XML content to plain text; <see cref="X"/> and <paramref name="x"/> become X and x.
        private static string ContentText(SyntaxList<XmlNodeSyntax> content)
        {
            var text = new StringBuilder();
            foreach (XmlNodeSyntax node in content)
            {
                switch (node)
                {
                    case XmlTextSyntax plain:
                        foreach (SyntaxToken token in plain.TextTokens)
                            text.Append(token.IsKind(SyntaxKind.XmlTextLiteralNewLineToken) ? " " : token.Text);
                        break;
                    case XmlElementSyntax inner:
                        text.Append(ContentText(inner.Content));
                        break;
                    case XmlEmptyElementSyntax reference:
                        text.Append(AttributeValue(reference.Attributes));
                        break;
                }
            }
            return SyntaxText.Collapse(text.ToString());
        }

        private static string AttributeValue(SyntaxList<XmlAttributeSyntax> attributes)
        {
            foreach (XmlAttributeSyntax attribute in attributes)
            {
                switch (attribute)
                {
                    case XmlCrefAttributeSyntax cref: return cref.Cref.ToString();
                    case XmlNameAttributeSyntax name: return name.Identifier.ToString();
                    case XmlTextAttributeSyntax text: return string.Concat(text.TextTokens);
                }
            }
            return string.Empty;
        }
    }
}
