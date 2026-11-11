using Codebound.Simulation;

namespace Codebound.Levels
{
    /// <summary>What a play session needs from a level. <see cref="LevelDefinition"/> is the real one.</summary>
    public interface IPlayableLevel
    {
        int Number { get; }
        string Title { get; }
        bool IsTimed { get; }

        LevelRun Run(string source);
        LevelVerdict Evaluate(LevelRun run);
        string Advise(string source, LevelRun run, LevelVerdict verdict);
        LevelDebrief Debrief(string source, LevelVerdict verdict);
        LevelHelp CreateHelp();
    }
}
