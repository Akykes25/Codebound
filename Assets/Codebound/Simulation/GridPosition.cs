using System;

namespace Codebound.Simulation
{
    /// <summary>A grid cell. X grows to the right and Y grows downwards.</summary>
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public GridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        public bool IsNextTo(GridPosition other)
        {
            return Math.Abs(X - other.X) + Math.Abs(Y - other.Y) == 1;
        }

        public bool Equals(GridPosition other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (X * 397) ^ Y;
        }

        public override string ToString()
        {
            return $"({X},{Y})";
        }
    }
}
