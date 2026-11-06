using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codebound.Language
{
    /// <summary>Turns Roslyn's technical syntax errors into messages written for beginners.</summary>
    internal static class SyntaxErrorTranslator
    {
        private const string IncompleteInstruction =
            "Esta instrucción está incompleta: revisá que no le falte un nombre o un símbolo.";

        private static readonly Regex QuotedText = new Regex("'([^']+)'", RegexOptions.Compiled);

        public static CodeDiagnostic Translate(Diagnostic diagnostic, CompilationUnitSyntax root)
        {
            var position = diagnostic.Location.GetLineSpan().StartLinePosition;
            return new CodeDiagnostic(position.Line + 1, position.Character + 1, Describe(diagnostic, root));
        }

        private static string Describe(Diagnostic diagnostic, CompilationUnitSyntax root)
        {
            switch (diagnostic.Id)
            {
                case "CS1002":
                    return "Falta ; al final de la instrucción.";
                case "CS1513":
                    return "Falta } para cerrar el bloque.";
                case "CS1514":
                    return "Falta { para abrir el bloque.";
                case "CS1026":
                    return "Falta ) para cerrar el paréntesis.";
                case "CS1001":
                    return IncompleteInstruction;
                case "CS1003":
                    var expected = QuotedArgument(diagnostic);
                    return expected == "," ? IncompleteInstruction : $"Falta {expected} en esta instrucción.";
                case "CS1525":
                    return $"Aparece {QuotedArgument(diagnostic)} donde se esperaba un valor.";
                case "CS1646":
                    return "El símbolo @ no está disponible en este juego.";
                case "CS1010":
                    return "Los textos entre comillas no están disponibles en este juego.";
                case "CS1035":
                    return "Falta cerrar el comentario con */.";
                case "CS1056":
                    return $"El símbolo {QuotedArgument(diagnostic)} no está disponible en este juego.";
                default:
                    return DescribeUnexpectedText(diagnostic, root);
            }
        }

        private static string DescribeUnexpectedText(Diagnostic diagnostic, CompilationUnitSyntax root)
        {
            var source = root.SyntaxTree.GetText();
            var span = diagnostic.Location.SourceSpan;
            var text = source.ToString(span).Trim();
            switch (text)
            {
                case "" when span.Start >= source.Length:
                    return "Hay un error de escritura al final del código: algo quedó sin terminar.";
                case "":
                    return "Hay un error de escritura en este punto: algo quedó sin terminar.";
                case "}":
                    return "Sobra } porque no hay ningún bloque para cerrar.";
                case "else":
                    return "else tiene que ir justo después del bloque de un if.";
                default:
                    return $"Hay un error de escritura cerca de {text}.";
            }
        }

        private static string QuotedArgument(Diagnostic diagnostic)
        {
            var match = QuotedText.Match(diagnostic.GetMessage(CultureInfo.InvariantCulture));
            return match.Success ? match.Groups[1].Value : "un símbolo";
        }
    }
}
