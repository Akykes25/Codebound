using System.Collections.Generic;

namespace Codebound.Simulation
{
    /// <summary>How the world is at a given moment of a run.</summary>
    public sealed class WorldState
    {
        private readonly List<GridPosition> activeModules = new List<GridPosition>();
        private readonly List<GridPosition> openGates = new List<GridPosition>();

        internal WorldState(GridPosition start)
        {
            Position = start;
        }

        public GridPosition Position { get; internal set; }
        public bool HasKey { get; internal set; }
        public bool TerminalActive { get; internal set; }
        public int CoreCharge { get; internal set; }
        public bool PowerDoorOpen { get; internal set; }
        public IReadOnlyList<GridPosition> ActiveModules => activeModules;
        public IReadOnlyList<GridPosition> OpenGates => openGates;

        internal void ActivateModule(GridPosition module)
        {
            if (!activeModules.Contains(module))
            {
                activeModules.Add(module);
            }
        }

        internal void OpenGate(GridPosition gate)
        {
            openGates.Add(gate);
        }
    }
}
