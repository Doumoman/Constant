using System;
using System.Globalization;

namespace StarNight.Map.WorldGeneration.Domain
{
    public readonly struct SectorCoord : IEquatable<SectorCoord>
    {
        public int X { get; }
        public int Y { get; }

        public SectorCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(SectorCoord other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is SectorCoord other && Equals(other);
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
                "SectorCoord({0}, {1})",
                X,
                Y);
        }

        public static bool operator ==(SectorCoord left, SectorCoord right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SectorCoord left, SectorCoord right)
        {
            return !left.Equals(right);
        }
    }
}
