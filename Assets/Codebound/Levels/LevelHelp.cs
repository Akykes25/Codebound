using System.Collections.Generic;

namespace Codebound.Levels
{
    /// <summary>The solution of a level: its code and the explanation of its logic.</summary>
    public readonly struct LevelSolution
    {
        public LevelSolution(string code, string explanation)
        {
            Code = code;
            Explanation = explanation;
        }

        public string Code { get; }
        public string Explanation { get; }
    }

    /// <summary>
    /// The help available during one attempt at a level: step-by-step hints and the solution.
    /// Opening the solution marks the attempt as assisted; revealing hints never does.
    /// Create one per attempt and keep it across resets, so a reset does not clear the mark.
    /// </summary>
    public sealed class LevelHelp
    {
        private readonly LevelSolution solution;

        public LevelHelp(IEnumerable<string> hints, string solutionCode, string solutionExplanation)
        {
            Hints = new HintSequence(hints);
            solution = new LevelSolution(solutionCode, solutionExplanation);
        }

        public HintSequence Hints { get; }

        /// <summary>
        /// True once the solution was opened. Pass it as the assisted flag when the level is completed,
        /// so the completion is recorded as "Asistido".
        /// </summary>
        public bool IsAssisted { get; private set; }

        /// <summary>Shows the solution and marks the attempt as assisted.</summary>
        public LevelSolution OpenSolution()
        {
            IsAssisted = true;
            return solution;
        }
    }
}
