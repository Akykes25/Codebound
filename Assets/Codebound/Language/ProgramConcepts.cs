using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codebound.Language
{
    /// <summary>The programming concepts a level can ask the player to use.</summary>
    [Flags]
    public enum ProgramConcept
    {
        None = 0,

        /// <summary>A counted <c>for</c> loop.</summary>
        ForLoop = 1 << 0,

        /// <summary>An <c>if</c> with its <c>else</c>.</summary>
        IfElse = 1 << 1,

        /// <summary>An <c>int</c> variable passed as the argument of a call, as in <c>ChargeCore(pulses)</c>.</summary>
        IntVariableArgument = 1 << 2,
    }

    /// <summary>
    /// Reads which concepts appear in the code. It looks at the syntax only:
    /// a loop counts even if it never runs a lap.
    /// </summary>
    internal static class ProgramConceptDetector
    {
        public static ProgramConcept Detect(CompilationUnitSyntax root)
        {
            var nodes = root.DescendantNodes().ToList();
            var concepts = ProgramConcept.None;

            if (nodes.OfType<ForStatementSyntax>().Any())
            {
                concepts |= ProgramConcept.ForLoop;
            }

            if (nodes.OfType<IfStatementSyntax>().Any(statement => statement.Else != null))
            {
                concepts |= ProgramConcept.IfElse;
            }

            // The validator only lets declared int variables appear as identifiers,
            // so any identifier passed to a call is an int variable.
            if (nodes.OfType<ArgumentSyntax>().Any(argument => argument.Expression is IdentifierNameSyntax))
            {
                concepts |= ProgramConcept.IntVariableArgument;
            }

            return concepts;
        }
    }
}
