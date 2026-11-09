using Codebound.Language;

namespace Codebound.Simulation
{
    /// <summary>What the world must look like after a run, and which concepts the code must use.</summary>
    public sealed class LevelGoal
    {
        public bool TerminalActive { get; set; }
        public bool AllModulesActive { get; set; }

        /// <summary>The exact charge the core must end with, or null when the core does not matter.</summary>
        public int? CoreCharge { get; set; }

        public bool PowerDoorOpen { get; set; }
        public bool AllGatesOpen { get; set; }

        /// <summary>Where the robot must end, or null when its position does not matter.</summary>
        public GridPosition? FinalPosition { get; set; }

        public ProgramConcept RequiredConcepts { get; set; }
    }
}
