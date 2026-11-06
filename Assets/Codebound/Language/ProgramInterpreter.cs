using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codebound.Language
{
    /// <summary>What a program can do in the game world.</summary>
    public interface IProgramHost
    {
        /// <summary>Runs a catalog method. Returns a bool for bool methods and null for void ones.</summary>
        object Invoke(string method, IReadOnlyList<object> arguments);
    }

    public enum ExecutionStatus
    {
        Completed,
        StepLimitExceeded,
    }

    /// <summary>One game method call made by the program, in execution order.</summary>
    public sealed class TraceEntry
    {
        internal TraceEntry(int line, int column, string method, IReadOnlyList<object> arguments, object result)
        {
            Line = line;
            Column = column;
            Method = method;
            Arguments = arguments;
            Result = result;
        }

        public int Line { get; }
        public int Column { get; }
        public string Method { get; }
        public IReadOnlyList<object> Arguments { get; }
        public object Result { get; }

        public override string ToString()
        {
            var call = $"{Line}:{Method}({string.Join(",", Arguments)})";
            return Result == null ? call : $"{call}={Result}";
        }
    }

    public sealed class ExecutionResult
    {
        internal ExecutionResult(ExecutionStatus status, IReadOnlyList<TraceEntry> trace)
        {
            Status = status;
            Trace = trace;
        }

        public ExecutionStatus Status { get; }
        public IReadOnlyList<TraceEntry> Trace { get; }
    }

    /// <summary>Walks a validated program and sends its calls to the host. Nothing else can run.</summary>
    public sealed class ProgramInterpreter
    {
        /// <summary>Limit on game method calls plus loop iterations, so no program can hang the game.</summary>
        public const int DefaultMaxSteps = 500;

        private readonly IProgramHost host;
        private readonly int maxSteps;
        private readonly List<TraceEntry> trace = new List<TraceEntry>();
        private readonly List<Dictionary<string, int>> scopes = new List<Dictionary<string, int>>();
        private int steps;

        private ProgramInterpreter(IProgramHost host, int maxSteps)
        {
            this.host = host;
            this.maxSteps = maxSteps;
        }

        public static ExecutionResult Run(ParsedProgram program, IProgramHost host, int maxSteps = DefaultMaxSteps)
        {
            return new ProgramInterpreter(host, maxSteps).Execute(program);
        }

        private sealed class StepLimitReached : Exception
        {
        }

        private ExecutionResult Execute(ParsedProgram program)
        {
            var status = ExecutionStatus.Completed;
            try
            {
                scopes.Add(new Dictionary<string, int>());
                foreach (var member in program.Root.Members.OfType<GlobalStatementSyntax>())
                {
                    RunStatement(member.Statement);
                }
            }
            catch (StepLimitReached)
            {
                status = ExecutionStatus.StepLimitExceeded;
            }

            return new ExecutionResult(status, trace);
        }

        private void RunStatement(StatementSyntax statement)
        {
            switch (statement)
            {
                case BlockSyntax block:
                    scopes.Add(new Dictionary<string, int>());
                    foreach (var inner in block.Statements)
                    {
                        RunStatement(inner);
                    }

                    scopes.RemoveAt(scopes.Count - 1);
                    break;
                case LocalDeclarationStatementSyntax local:
                    var declared = local.Declaration.Variables[0];
                    var value = (int)Evaluate(declared.Initializer.Value);
                    scopes[scopes.Count - 1][declared.Identifier.ValueText] = value;
                    break;
                case ExpressionStatementSyntax expression:
                    Evaluate(expression.Expression);
                    break;
                case ForStatementSyntax forStatement:
                    RunFor(forStatement);
                    break;
                case IfStatementSyntax ifStatement:
                    RunIf(ifStatement);
                    break;
                default:
                    throw new InvalidOperationException($"Unvalidated statement: {statement.Kind()}");
            }
        }

        private void RunFor(ForStatementSyntax statement)
        {
            var variable = statement.Declaration.Variables[0];
            var name = variable.Identifier.ValueText;
            var scope = new Dictionary<string, int> { [name] = (int)Evaluate(variable.Initializer.Value) };
            scopes.Add(scope);
            while ((bool)Evaluate(statement.Condition))
            {
                CountStep();
                RunStatement(statement.Statement);
                scope[name]++;
            }

            scopes.RemoveAt(scopes.Count - 1);
        }

        private void RunIf(IfStatementSyntax statement)
        {
            if ((bool)Evaluate(statement.Condition))
            {
                RunStatement(statement.Statement);
            }
            else if (statement.Else != null)
            {
                RunStatement(statement.Else.Statement);
            }
        }

        private object Evaluate(ExpressionSyntax expression)
        {
            switch (expression)
            {
                case LiteralExpressionSyntax literal:
                    return (int)literal.Token.Value;
                case IdentifierNameSyntax identifier:
                    return ReadVariable(identifier.Identifier.ValueText);
                case BinaryExpressionSyntax binary:
                    return (int)Evaluate(binary.Left) < (int)Evaluate(binary.Right);
                case InvocationExpressionSyntax call:
                    return RunCall(call);
                default:
                    throw new InvalidOperationException($"Unvalidated expression: {expression.Kind()}");
            }
        }

        private object RunCall(InvocationExpressionSyntax call)
        {
            CountStep();
            var method = ((IdentifierNameSyntax)call.Expression).Identifier.ValueText;
            var arguments = call.ArgumentList.Arguments.Select(argument => Evaluate(argument.Expression)).ToArray();
            var result = host.Invoke(method, arguments);
            var position = call.GetLocation().GetLineSpan().StartLinePosition;
            trace.Add(new TraceEntry(position.Line + 1, position.Character + 1, method, arguments, result));
            return result;
        }

        private int ReadVariable(string name)
        {
            for (var i = scopes.Count - 1; i >= 0; i--)
            {
                if (scopes[i].TryGetValue(name, out var value))
                {
                    return value;
                }
            }

            throw new InvalidOperationException($"Unvalidated variable: {name}");
        }

        private void CountStep()
        {
            if (steps >= maxSteps)
            {
                throw new StepLimitReached();
            }

            steps++;
        }
    }
}
