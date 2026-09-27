using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Olviia.CodeMap.Core.Model;

namespace Olviia.CodeMap.Core.Parsing
{
    /// <summary>Builds <see cref="MemberEntry"/> objects from member declarations, one method per kind of member.</summary>
    internal static class MemberReader
    {
        /// <summary>Reads all non-private members of a class, struct, interface or record, in source order. Nested types are skipped here.</summary>
        /// <param name="type">Type declaration.</param>
        /// <param name="typeAccess">Effective access of the type; members cannot be more visible.</param>
        /// <returns>Member entries.</returns>
        public static IReadOnlyList<MemberEntry> ReadAll(TypeDeclarationSyntax type, Access typeAccess)
        {
            // Interface members are public unless stated otherwise; everything else defaults to private.
            Access? whenMissing = type is InterfaceDeclarationSyntax ? Access.Public : (Access?)null;
            var result = new List<MemberEntry>();

            if (type is RecordDeclarationSyntax record && record.ParameterList != null)
                result.Add(PrimaryConstructor(record, typeAccess));

            foreach (MemberDeclarationSyntax member in type.Members)
            {
                // Explicit interface implementations have no modifier but are reachable through the interface.
                Access? declared = HasExplicitInterface(member) ? Access.Public : AccessRules.FromModifiers(member.Modifiers, whenMissing);
                if (declared == null)
                    continue;
                Access access = AccessRules.Narrow(typeAccess, declared.Value);

                switch (member)
                {
                    case ConstructorDeclarationSyntax c: result.Add(Constructor(c, access)); break;
                    case MethodDeclarationSyntax m: result.Add(Method(m, access)); break;
                    case PropertyDeclarationSyntax p: result.Add(Property(p, access)); break;
                    case IndexerDeclarationSyntax i: result.Add(Indexer(i, access)); break;
                    case EventDeclarationSyntax e: result.Add(Event(e, access)); break;
                    case FieldDeclarationSyntax f: result.AddRange(Fields(f, access)); break;
                    case EventFieldDeclarationSyntax ef: result.AddRange(EventFields(ef, access)); break;
                    case OperatorDeclarationSyntax o: result.Add(Operator(o, access)); break;
                    case ConversionOperatorDeclarationSyntax co: result.Add(Conversion(co, access)); break;
                }
            }
            return result;
        }

        /// <summary>Reads the values of an enum.</summary>
        /// <param name="type">Enum declaration.</param>
        /// <param name="typeAccess">Effective access of the enum; values share it.</param>
        /// <returns>One entry per value, e.g. <c>Fire = 2</c>.</returns>
        public static IReadOnlyList<MemberEntry> EnumValues(EnumDeclarationSyntax type, Access typeAccess)
        {
            var result = new List<MemberEntry>();
            foreach (EnumMemberDeclarationSyntax value in type.Members)
            {
                string signature = value.Identifier.Text;
                if (value.EqualsValue != null)
                    signature += " = " + SyntaxText.Collapse(value.EqualsValue.Value.ToString());
                result.Add(Entry(MemberKind.EnumValue, typeAccess, value.Identifier.Text, null, signature, default, null, value.Identifier, value));
            }
            return result;
        }

        /// <summary>A delegate stored as its single member, so the model needs no special case for it.</summary>
        /// <param name="declaration">Delegate declaration.</param>
        /// <param name="access">Effective access.</param>
        /// <param name="doc">The delegate's documentation.</param>
        /// <returns>Entry with the full delegate signature.</returns>
        public static MemberEntry Delegate(DelegateDeclarationSyntax declaration, Access access, DocComment doc)
        {
            string signature = "delegate " + declaration.ReturnType + " " + declaration.Identifier.Text
                + SyntaxText.TypeParameters(declaration.TypeParameterList) + SyntaxText.Parameters(declaration.ParameterList);
            return new MemberEntry(MemberKind.Method, access, declaration.Identifier.Text, SyntaxText.ParameterTypes(declaration.ParameterList),
                SyntaxText.Collapse(signature), false, string.Empty, SyntaxText.Line(declaration.Identifier), doc);
        }

        private static MemberEntry PrimaryConstructor(RecordDeclarationSyntax record, Access access)
        {
            string signature = record.Identifier.Text + SyntaxText.Parameters(record.ParameterList);
            return Entry(MemberKind.Constructor, access, record.Identifier.Text, record.ParameterList, signature, default, null, record.Identifier, record);
        }

        private static MemberEntry Constructor(ConstructorDeclarationSyntax c, Access access)
        {
            string signature = c.Identifier.Text + SyntaxText.Parameters(c.ParameterList);
            return Entry(MemberKind.Constructor, access, c.Identifier.Text, c.ParameterList, signature, c.Modifiers, null, c.Identifier, c);
        }

        private static MemberEntry Method(MethodDeclarationSyntax m, Access access)
        {
            string signature = SyntaxText.Modifiers(m.Modifiers) + m.ReturnType + " " + Explicit(m.ExplicitInterfaceSpecifier)
                + m.Identifier.Text + SyntaxText.TypeParameters(m.TypeParameterList) + SyntaxText.Parameters(m.ParameterList);
            return Entry(MemberKind.Method, access, m.Identifier.Text, m.ParameterList, signature, m.Modifiers, m.ExplicitInterfaceSpecifier, m.Identifier, m);
        }

        private static MemberEntry Property(PropertyDeclarationSyntax p, Access access)
        {
            string signature = SyntaxText.Modifiers(p.Modifiers) + p.Type + " " + Explicit(p.ExplicitInterfaceSpecifier)
                + p.Identifier.Text + " " + SyntaxText.Accessors(p.AccessorList, p.ExpressionBody);
            return Entry(MemberKind.Property, access, p.Identifier.Text, null, signature, p.Modifiers, p.ExplicitInterfaceSpecifier, p.Identifier, p);
        }

        private static MemberEntry Indexer(IndexerDeclarationSyntax i, Access access)
        {
            string signature = SyntaxText.Modifiers(i.Modifiers) + i.Type + " " + Explicit(i.ExplicitInterfaceSpecifier)
                + "this" + SyntaxText.Parameters(i.ParameterList) + " " + SyntaxText.Accessors(i.AccessorList, i.ExpressionBody);
            return Entry(MemberKind.Indexer, access, "this[]", i.ParameterList, signature, i.Modifiers, i.ExplicitInterfaceSpecifier, i.ThisKeyword, i);
        }

        private static MemberEntry Event(EventDeclarationSyntax e, Access access)
        {
            string signature = SyntaxText.Modifiers(e.Modifiers) + "event " + e.Type + " " + Explicit(e.ExplicitInterfaceSpecifier) + e.Identifier.Text;
            return Entry(MemberKind.Event, access, e.Identifier.Text, null, signature, e.Modifiers, e.ExplicitInterfaceSpecifier, e.Identifier, e);
        }

        // One declaration can hold several variables: "public int a, b;".
        private static IEnumerable<MemberEntry> Fields(FieldDeclarationSyntax f, Access access)
        {
            bool isConst = f.Modifiers.Any(SyntaxKind.ConstKeyword);
            foreach (VariableDeclaratorSyntax variable in f.Declaration.Variables)
            {
                string signature = SyntaxText.Modifiers(f.Modifiers) + f.Declaration.Type + " " + variable.Identifier.Text;
                if (isConst && variable.Initializer != null)
                    signature += " = " + variable.Initializer.Value;
                yield return Entry(MemberKind.Field, access, variable.Identifier.Text, null, signature, f.Modifiers, null, variable.Identifier, f);
            }
        }

        private static IEnumerable<MemberEntry> EventFields(EventFieldDeclarationSyntax e, Access access)
        {
            foreach (VariableDeclaratorSyntax variable in e.Declaration.Variables)
            {
                string signature = SyntaxText.Modifiers(e.Modifiers) + "event " + e.Declaration.Type + " " + variable.Identifier.Text;
                yield return Entry(MemberKind.Event, access, variable.Identifier.Text, null, signature, e.Modifiers, null, variable.Identifier, e);
            }
        }

        private static MemberEntry Operator(OperatorDeclarationSyntax o, Access access)
        {
            string name = "operator " + o.OperatorToken.Text;
            string signature = SyntaxText.Modifiers(o.Modifiers) + o.ReturnType + " " + name + SyntaxText.Parameters(o.ParameterList);
            return Entry(MemberKind.Operator, access, name, o.ParameterList, signature, o.Modifiers, null, o.OperatorToken, o);
        }

        private static MemberEntry Conversion(ConversionOperatorDeclarationSyntax c, Access access)
        {
            string name = c.ImplicitOrExplicitKeyword.Text + " operator " + c.Type;
            string signature = SyntaxText.Modifiers(c.Modifiers) + name + SyntaxText.Parameters(c.ParameterList);
            return Entry(MemberKind.Operator, access, name, c.ParameterList, signature, c.Modifiers, null, c.OperatorKeyword, c);
        }

        private static bool HasExplicitInterface(MemberDeclarationSyntax member)
        {
            switch (member)
            {
                case MethodDeclarationSyntax m: return m.ExplicitInterfaceSpecifier != null;
                case BasePropertyDeclarationSyntax p: return p.ExplicitInterfaceSpecifier != null;
                default: return false;
            }
        }

        // "ISaveable." for explicit implementations, empty otherwise.
        private static string Explicit(ExplicitInterfaceSpecifierSyntax specifier)
        {
            return specifier == null ? string.Empty : specifier.Name + ".";
        }

        private static MemberEntry Entry(MemberKind kind, Access access, string name, BaseParameterListSyntax parameters, string signature,
            SyntaxTokenList modifiers, ExplicitInterfaceSpecifierSyntax explicitInterface, SyntaxToken lineToken, SyntaxNode docOwner)
        {
            return new MemberEntry(
                kind,
                access,
                name,
                SyntaxText.ParameterTypes(parameters),
                SyntaxText.Collapse(signature),
                modifiers.Any(SyntaxKind.OverrideKeyword),
                explicitInterface == null ? string.Empty : SyntaxText.Collapse(explicitInterface.Name.ToString()),
                SyntaxText.Line(lineToken),
                DocCommentReader.Read(docOwner));
        }
    }
}
