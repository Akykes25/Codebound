using System;
using System.Collections.Generic;

namespace Codebound.Simulation
{
    /// <summary>What a level contains when it starts. A run never changes it.</summary>
    public sealed class LevelLayout
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public GridPosition Start { get; set; }
        public IReadOnlyList<GridPosition> Walls { get; set; } = Array.Empty<GridPosition>();
        public IReadOnlyList<GridPosition> Modules { get; set; } = Array.Empty<GridPosition>();
        public IReadOnlyList<GridPosition> Gates { get; set; } = Array.Empty<GridPosition>();
        public GridPosition? Terminal { get; set; }
        public GridPosition? Core { get; set; }
        public GridPosition? PowerDoor { get; set; }
        public int RequiredCharge { get; set; }
        public GridPosition? Key { get; set; }
    }
}
