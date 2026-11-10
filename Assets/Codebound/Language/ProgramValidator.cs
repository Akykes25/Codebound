using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codebound.Language
{
    /// <summary>
    /// Accepts only the allowed C# subset. Anything not handled here is rejected,
    /// so new C# syntax can never reach the interpreter by accident.
    /// </summary>
    internal sealed class ProgramValidator
    {
        private const string CountedForMessage =
            "Este juego usa el for contado: for (int i = 0; i < 4; i++).";

        private readonly Dictionary<string, MethodSignature> methods;
        private readonly List<CodeDiagnostic> issues = new List<CodeDiagnostic>();
        private readonly List<HashSet<string>> scopes = new List<HashSet<string>>();

        private readonly HashSet<string> available;

        /// <param name="availableMethods">The catalog methods this level allows, or null for all of them.</param>
        public ProgramValidator(IEnumerable<MethodSignature> catalog, IEnumerable<string> availableMethods = null)
        {
            methods = catalog.ToDictionary(method => method.Name);
            available = availableMethods == null ? null : new HashSet<string>(availableMethods);
        }

        public IReadOnlyList<CodeDiagnostic> Validate(CompilationUnitSyntax root)
        {
            foreach (var usingDirective in root.Usings)
            {
                Report(usingDirective, "using no está disponible en este juego.");
            }

            foreach (var attributeList in root.AttributeLists)
            {
                Report(attributeList, "Los atributos no están disponibles en este juego.");
            }

            PushScope();
            foreach (var member in root.Members)
            {
                if (member is GlobalStatementSyntax global)
                {
                    CheckStatement(global.Statement);
                }
                else
                {
                    Report(member, $"{member.GetFirstToken().Text} no está disponible: acá solo se escriben instrucciones.");
                }
            }

            PopScope();
            return issues;
        }

        private void CheckStatement(StatementSyntax statement)
        {
            switch (statement)
            {
                case BlockSyntax block:
                    PushScope();
                    foreach (var inner in block.Statements)
                    {
                        CheckStatement(inner);
                    }

                    PopScope();
                    break;
                case LocalDeclarationStatementSyntax declaration:
                    CheckDeclaration(declaration);
                    break;
                case ExpressionStatementSyntax expressionStatement:
                    CheckExpressionStatement(expressionStatement);
                    break;
                case ForStatementSyntax forStatement:
                    CheckFor(forStatement);
                    break;
                case IfStatementSyntax ifStatement:
                    CheckIf(ifStatement);
                    break;
                case EmptyStatementSyntax empty:
                    Report(empty, "Sobra un ; en esta línea.");
                    break;
                case LocalFunctionStatementSyntax localFunction:
                    Report(localFunction, "Crear métodos propios todavía no está disponible.");
                    break;
                default:
                    Report(statement, $"{statement.GetFirstToken().Text} no está disponible en este juego.");
                    break;
            }
        }

        private void CheckDeclaration(LocalDeclarationStatementSyntax statement)
        {
            var declaration = statement.Declaration;
            if (statement.Modifiers.Any() || !IsIntType(declaration.Type))
            {
                Report(statement, $"Por ahora solo se pueden declarar variables int, no {statement.GetFirstToken().Text}.");
                return;
            }

            if (declaration.Variables.Count != 1 || declaration.Variables[0].Initializer == null)
            {
                Report(statement, "Declará una variable por vez y dale un valor: int pulsos = 3;");
                return;
            }

            var variable = declaration.Variables[0];
            var name = variable.Identifier.ValueText;
            var kind = CheckExpression(variable.Initializer.Value);
            if (kind != null && kind != ValueKind.Int)
            {
                Report(variable, $"La variable {name} es int y no puede guardar ese valor.");
            }

            Declare(variable, name);
        }

        private void CheckExpressionStatement(ExpressionStatementSyntax statement)
        {
            switch (statement.Expression)
            {
                case InvocationExpressionSyntax call:
                    CheckCall(call);
                    break;
                case AssignmentExpressionSyntax assignment:
                    Report(assignment, "Cambiar una variable con = todavía no está disponible.");
                    break;
                case PostfixUnaryExpressionSyntax postfix:
                    Report(postfix, $"{postfix.OperatorToken.Text} solo se puede usar en el encabezado de un for.");
                    break;
                case PrefixUnaryExpressionSyntax prefix:
                    Report(prefix, $"{prefix.OperatorToken.Text} no está disponible en este juego.");
                    break;
                default:
                    Report(statement, $"{statement.GetFirstToken().Text} no es una instrucción que este juego pueda ejecutar.");
                    break;
            }
        }

        private void CheckFor(ForStatementSyntax statement)
        {
            var declaration = statement.Declaration;
            if (declaration == null || !IsIntType(declaration.Type) || declaration.Variables.Count != 1
                || declaration.Variables[0].Initializer == null || statement.Initializers.Any())
            {
                Report(statement, CountedForMessage);
                return;
            }

            var variable = declaration.Variables[0];
            var name = variable.Identifier.ValueText;
            if (!IsLessThanOnVariable(statement.Condition, name) || !IsPostIncrementOnVariable(statement.Incrementors, name))
            {
                Report(statement, CountedForMessage);
                return;
            }

            var startKind = CheckExpression(variable.Initializer.Value);
            if (startKind != null && startKind != ValueKind.Int)
            {
                Report(variable, $"La variable {name} es int y no puede guardar ese valor.");
            }

            PushScope();
            Declare(variable, name);
            var limit = ((BinaryExpressionSyntax)statement.Condition).Right;
            var limitKind = CheckExpression(limit);
            if (limitKind != null && limitKind != ValueKind.Int)
            {
                Report(limit, "El límite del for tiene que ser un int.");
            }

            CheckBody(statement.Statement);
            PopScope();
        }

        /// <summary>C# forbids a declaration as the lone body of a for, if or else.</summary>
        private void CheckBody(StatementSyntax body)
        {
            if (body is LocalDeclarationStatementSyntax)
            {
                Report(body, "Para declarar una variable dentro de un for o un if, usá llaves { }.");
                return;
            }

            CheckStatement(body);
        }

        private void CheckIf(IfStatementSyntax statement)
        {
            var kind = CheckExpression(statement.Condition);
            if (kind != null && kind != ValueKind.Bool)
            {
                Report(statement.Condition, "La condición del if tiene que ser verdadera o falsa, por ejemplo HasKey().");
            }

            CheckBody(statement.Statement);
            if (statement.Else != null)
            {
                CheckBody(statement.Else.Statement);
            }
        }

        /// <summary>Returns the expression's type, or null when it already reported a problem.</summary>
        private ValueKind? CheckExpression(ExpressionSyntax expression)
        {
            switch (expression)
            {
                case LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.NumericLiteralExpression):
                    if (literal.Token.Value is int)
                    {
                        return ValueKind.Int;
                    }

                    Report(literal, $"El número {literal.Token.Text} no es un int válido.");
                    return null;
                case IdentifierNameSyntax identifier:
                    if (IsDeclared(identifier.Identifier.ValueText))
                    {
                        return ValueKind.Int;
                    }

                    Report(identifier, $"La variable {identifier.Identifier.ValueText} no existe en este punto del código.");
                    return null;
                case InvocationExpressionSyntax call:
                    return CheckValueCall(call);
                case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.LessThanExpression):
                    return CheckLessThan(binary);
                default:
                    Report(expression, $"{expression} no está disponible en este juego.");
                    return null;
            }
        }

        private ValueKind? CheckValueCall(InvocationExpressionSyntax call)
        {
            var returns = CheckCall(call);
            if (returns == ValueKind.Void)
            {
                Report(call, $"{MethodName(call)} no devuelve un valor, así que no se puede usar acá.");
                return null;
            }

            return returns;
        }

        private ValueKind? CheckLessThan(BinaryExpressionSyntax binary)
        {
            var left = CheckExpression(binary.Left);
            var right = CheckExpression(binary.Right);
            if (left == null || right == null)
            {
                return null;
            }

            if (left != ValueKind.Int || right != ValueKind.Int)
            {
                Report(binary, "El operador < compara dos valores int.");
                return null;
            }

            return ValueKind.Bool;
        }

        /// <summary>Returns the method's return type, or null when the call is invalid.</summary>
        private ValueKind? CheckCall(InvocationExpressionSyntax call)
        {
            if (!(call.Expression is IdentifierNameSyntax identifier))
            {
                Report(call, $"Los métodos se llaman directamente por su nombre, sin {call.Expression}.");
                return null;
            }

            var name = identifier.Identifier.ValueText;
            if (!methods.TryGetValue(name, out var method))
            {
                Report(call, $"El método {name} no existe en este juego.");
                return null;
            }

            if (available != null && !available.Contains(name))
            {
                Report(call, $"El método {name} todavía no está disponible en este nivel.");
                return method.Returns;
            }

            var arguments = call.ArgumentList.Arguments;
            if (arguments.Count != method.Parameters.Count)
            {
                Report(call, $"{name} recibe {method.Parameters.Count} valor(es) y le pasaste {arguments.Count}.");
                return method.Returns;
            }

            for (var i = 0; i < arguments.Count; i++)
            {
                var argument = arguments[i];
                if (argument.NameColon != null || !argument.RefKindKeyword.IsKind(SyntaxKind.None))
                {
                    Report(argument, $"Pasale a {name} solo el valor, sin nombre ni modificadores.");
                    continue;
                }

                var kind = CheckExpression(argument.Expression);
                if (kind != null && kind != method.Parameters[i])
                {
                    Report(argument, $"{name} necesita un valor de otro tipo en esa posición.");
                }
            }

            return method.Returns;
        }

        private static string MethodName(InvocationExpressionSyntax call)
        {
            return ((IdentifierNameSyntax)call.Expression).Identifier.ValueText;
        }

        private static bool IsIntType(TypeSyntax type)
        {
            return type is PredefinedTypeSyntax predefined && predefined.Keyword.IsKind(SyntaxKind.IntKeyword);
        }

        private static bool IsLessThanOnVariable(ExpressionSyntax condition, string variable)
        {
            return condition is BinaryExpressionSyntax binary
                && binary.IsKind(SyntaxKind.LessThanExpression)
                && binary.Left is IdentifierNameSyntax left
                && left.Identifier.ValueText == variable;
        }

        private static bool IsPostIncrementOnVariable(SeparatedSyntaxList<ExpressionSyntax> incrementors, string variable)
        {
            return incrementors.Count == 1
                && incrementors[0] is PostfixUnaryExpressionSyntax postfix
                && postfix.IsKind(SyntaxKind.PostIncrementExpression)
                && postfix.Operand is IdentifierNameSyntax operand
                && operand.Identifier.ValueText == variable;
        }

        private void PushScope()
        {
            scopes.Add(new HashSet<string>());
        }

        private void PopScope()
        {
            scopes.RemoveAt(scopes.Count - 1);
        }

        private bool IsDeclared(string name)
        {
            return scopes.Any(scope => scope.Contains(name));
        }

        private void Declare(SyntaxNode node, string name)
        {
            if (IsDeclared(name))
            {
                Report(node, $"La variable {name} ya existe; elegí otro nombre.");
                return;
            }

            scopes[scopes.Count - 1].Add(name);
        }

        private void Report(SyntaxNode node, string message)
        {
            var position = node.GetLocation().GetLineSpan().StartLinePosition;
            issues.Add(new CodeDiagnostic(position.Line + 1, position.Character + 1, message));
        }
    }
}
