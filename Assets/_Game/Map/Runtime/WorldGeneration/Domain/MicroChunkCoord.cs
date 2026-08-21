using System;
using System.Globalization;

namespace StarNight.Map.WorldGeneration.Domain
{
    public readonly struct MicroChunkCoord : IEquatable<MicroChunkCoord>
    {
        public int X { get; }
        public int Y { get; }

        public MicroChunkCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(MicroChunkCoord other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is MicroChunkCoord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "MicroChunkCoord({0}, {1})",
                X,
                Y);
        }

        public static bool operator ==(MicroChunkCoord left, MicroChunkCoord right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MicroChunkCoord left, MicroChunkCoord right)
        {
            return !left.Equals(right);
        }
    }
}
