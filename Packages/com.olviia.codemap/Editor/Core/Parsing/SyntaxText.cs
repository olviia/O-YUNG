using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Olviia.CodeMap.Core.Parsing
{
    /// <summary>Turns syntax fragments into the compact text used in signatures.</summary>
    internal static class SyntaxText
    {
        private static readonly Regex Whitespace = new Regex(@"\s+");

        // Modifiers that change how a member is used; access modifiers are shown by the index section instead.
        private static readonly SyntaxKind[] ShownModifiers =
        {
            SyntaxKind.StaticKeyword, SyntaxKind.AbstractKeyword, SyntaxKind.VirtualKeyword,
            SyntaxKind.OverrideKeyword, SyntaxKind.SealedKeyword, SyntaxKind.ConstKeyword, SyntaxKind.ReadOnlyKeyword
        };

        /// <summary>Collapses all whitespace runs, including line breaks, to single spaces.</summary>
        /// <param name="text">Raw source text.</param>
        /// <returns>Single-line, trimmed text.</returns>
        public static string Collapse(string text)
        {
            return Whitespace.Replace(text, " ").Trim();
        }

        /// <summary>1-based line where a token starts.</summary>
        /// <param name="token">Usually the identifier of a declaration.</param>
        /// <returns>Line number as shown in editors.</returns>
        public static int Line(SyntaxToken token)
        {
            return token.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        }

        /// <summary>Signature prefix such as <c>static </c> or <c>override </c>.</summary>
        /// <param name="modifiers">All modifiers of the declaration.</param>
        /// <returns>Shown modifiers in source order with a trailing space; empty if none.</returns>
        public static string Modifiers(SyntaxTokenList modifiers)
        {
            string[] shown = modifiers.Where(m => ShownModifiers.Contains(m.Kind())).Select(m => m.Text).ToArray();
            return shown.Length == 0 ? string.Empty : string.Join(" ", shown) + " ";
        }

        /// <summary>Generic parameter list such as <c>&lt;T&gt;</c>.</summary>
        /// <param name="list">Type parameter list, may be null.</param>
        /// <returns>The list as text, or empty.</returns>
        public static string TypeParameters(TypeParameterListSyntax list)
        {
            return list == null ? string.Empty : Collapse(list.ToString());
        }

        /// <summary>Parameters in parentheses or brackets, without attributes.</summary>
        /// <param name="list">Parameter list, may be null.</param>
        /// <returns>For example <c>(int amount, DamageType type = DamageType.Physical)</c>.</returns>
        public static string Parameters(BaseParameterListSyntax list)
        {
            if (list == null)
                return string.Empty;
            bool brackets = list is BracketedParameterListSyntax;
            string inner = string.Join(", ", list.Parameters.Select(Parameter));
            return brackets ? "[" + inner + "]" : "(" + inner + ")";
        }

        /// <summary>Parameter types only, used to tell overloads apart.</summary>
        /// <param name="list">Parameter list, may be null.</param>
        /// <returns>For example <c>int</c>, <c>ref Vector3</c>.</returns>
        public static IReadOnlyList<string> ParameterTypes(BaseParameterListSyntax list)
        {
            if (list == null)
                return new string[0];
            return list.Parameters.Select(p => Collapse(ParameterModifiers(p) + p.Type)).ToList();
        }

        /// <summary>Property or indexer accessors visible from outside the type, e.g. <c>{ get; internal set; }</c>.</summary>
        /// <param name="accessors">Accessor list, may be null.</param>
        /// <param name="expressionBody">Expression body (<c>=&gt;</c>), which means get-only.</param>
        /// <returns>Accessors text; private accessors are left out.</returns>
        public static string Accessors(AccessorListSyntax accessors, ArrowExpressionClauseSyntax expressionBody)
        {
            if (accessors == null)
                return expressionBody != null ? "{ get; }" : string.Empty;

            var parts = new List<string>();
            foreach (AccessorDeclarationSyntax accessor in accessors.Accessors)
            {
                if (accessor.Modifiers.Count == 1 && accessor.Modifiers[0].IsKind(SyntaxKind.PrivateKeyword))
                    continue;
                string modifiers = accessor.Modifiers.Count == 0 ? string.Empty : Collapse(accessor.Modifiers.ToString()) + " ";
                parts.Add(modifiers + accessor.Keyword.Text + ";");
            }
            return "{ " + string.Join(" ", parts) + " }";
        }

        private static string Parameter(ParameterSyntax parameter)
        {
            string text = ParameterModifiers(parameter) + parameter.Type + " " + parameter.Identifier.Text;
            if (parameter.Default != null)
                text += " = " + parameter.Default.Value;
            return Collapse(text);
        }

        private static string ParameterModifiers(ParameterSyntax parameter)
        {
            return parameter.Modifiers.Count == 0 ? string.Empty : parameter.Modifiers.ToString() + " ";
        }
    }
}
