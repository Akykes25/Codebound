using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codebound.Language
{
    /// <summary>A validated program, ready to be run by <see cref="ProgramInterpreter"/>.</summary>
    public sealed class ParsedProgram
    {
        internal ParsedProgram(CompilationUnitSyntax root)
        {
            Root = root;
        }

        internal CompilationUnitSyntax Root { get; }
    }

    public sealed class CompilationResult
    {
        internal CompilationResult(ParsedProgram program, IReadOnlyList<CodeDiagnostic> diagnostics)
        {
            Program = program;
            Diagnostics = diagnostics;
        }

        /// <summary>The program to run, or null when the code has errors.</summary>
        public ParsedProgram Program { get; }
        public IReadOnlyList<CodeDiagnostic> Diagnostics { get; }
        public bool Succeeded => Program != null;
    }

    /// <summary>
    /// Parses the player's method body with Roslyn and accepts only the allowed C# subset.
    /// Roslyn is never asked to emit or run code: execution stays in <see cref="ProgramInterpreter"/>.
    /// </summary>
    public static class ProgramCompiler
    {
        private static readonly CSharpParseOptions ParseOptions =
            new CSharpParseOptions(LanguageVersion.CSharp9, DocumentationMode.None, SourceCodeKind.Regular);

        public static CompilationResult Compile(string source, IEnumerable<MethodSignature> catalog)
        {
            var tree = CSharpSyntaxTree.ParseText(source ?? string.Empty, ParseOptions);
            var root = tree.GetCompilationUnitRoot();

            var syntaxError = root.GetDiagnostics()
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .OrderBy(diagnostic => diagnostic.Location.SourceSpan.Start)
                .FirstOrDefault();
            if (syntaxError != null)
            {
                return new CompilationResult(null, new[] { SyntaxErrorTranslator.Translate(syntaxError, root) });
            }

            var issues = new ProgramValidator(catalog).Validate(root);
            return issues.Count > 0
                ? new CompilationResult(null, issues)
                : new CompilationResult(new ParsedProgram(root), issues);
        }
    }
}
