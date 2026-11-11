using Codebound.Language;
using Codebound.Simulation;

namespace Codebound.Levels
{
    /// <summary>
    /// What the player reads after completing a level: why the solution works.
    /// When the player solved it another way, it also shows the reference solution the explanation talks about.
    /// </summary>
    public sealed class LevelDebrief
    {
        private LevelDebrief(bool matchesSolution, string explanation, string referenceSolution)
        {
            MatchesSolution = matchesSolution;
            Explanation = explanation;
            ReferenceSolution = referenceSolution;
        }

        /// <summary>True when Roslyn says the player's code is the expected program.</summary>
        public bool MatchesSolution { get; }

        public string Title => MatchesSolution ? "Así funciona tu solución" : "Tu programa también resuelve el nivel";

        /// <summary>Why the level's solution works.</summary>
        public string Explanation { get; }

        /// <summary>The level's solution, to read next to the explanation, or null when the player wrote it.</summary>
        public string ReferenceSolution { get; }

        /// <returns>The debrief of a completed level, or null when the level was not completed.</returns>
        public static LevelDebrief For(string source, LevelVerdict verdict, string solutionCode, string solutionExplanation)
        {
            if (!verdict.Completed)
            {
                return null;
            }

            var matches = ProgramComparer.AreEquivalent(source, solutionCode);
            return new LevelDebrief(matches, solutionExplanation, matches ? null : solutionCode);
        }
    }
}
