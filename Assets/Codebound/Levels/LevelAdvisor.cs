using Codebound.Language;
using Codebound.Simulation;

namespace Codebound.Levels
{
    /// <summary>
    /// Decides when to show a level's advice: the code ran without errors but did not reach the goal,
    /// and Roslyn says it is not the expected program. Every wrong program of a level gets the same advice.
    /// </summary>
    public static class LevelAdvisor
    {
        /// <returns>The level's advice, or null when it does not apply.</returns>
        public static string Advise(string source, LevelRun run, LevelVerdict verdict, string solutionCode, string advice)
        {
            // Code with errors already has its own message, and a solved world only misses a concept,
            // which LevelVerdict.MissingConcepts explains.
            if (string.IsNullOrWhiteSpace(advice) || !run.Compiled || verdict.PendingObjectives.Count == 0)
            {
                return null;
            }

            return ProgramComparer.AreEquivalent(source, solutionCode) ? null : advice;
        }
    }
}
