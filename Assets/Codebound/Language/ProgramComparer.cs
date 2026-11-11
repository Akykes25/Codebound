using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codebound.Language
{
    /// <summary>
    /// Compares two programs with Roslyn by what they say, not by how they are written:
    /// spacing, line breaks, comments and the names chosen for variables do not matter.
    /// </summary>
    public static class ProgramComparer
    {
        private static readonly CSharpParseOptions ParseOptions =
            new CSharpParseOptions(LanguageVersion.CSharp9, DocumentationMode.None, SourceCodeKind.Regular);

        public static bool AreEquivalent(string source, string expected)
        {
            return Shape(source).SequenceEqual(Shape(expected));
        }

        /// <summary>The program's tokens, with each declared variable replaced by its declaration order.</summary>
        private static IEnumerable<string> Shape(string source)
        {
            var root = CSharpSyntaxTree.ParseText(source ?? string.Empty, ParseOptions).GetCompilationUnitRoot();
            var variables = root.DescendantNodes()
                .OfType<VariableDeclaratorSyntax>()
                .Select(declarator => declarator.Identifier.ValueText)
                .Distinct()
                .Select((name, index) => (name, index))
                .ToDictionary(variable => variable.name, variable => variable.index);

            return root.DescendantTokens()
                .Where(token => !token.IsKind(SyntaxKind.EndOfFileToken))
                .Select(token => token.IsKind(SyntaxKind.IdentifierToken) && variables.TryGetValue(token.ValueText, out var index)
                    ? "$" + index
                    : token.ValueText)
                .ToList();
        }
    }
}
