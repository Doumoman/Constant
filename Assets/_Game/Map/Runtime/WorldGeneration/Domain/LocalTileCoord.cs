using System;
using System.Globalization;

namespace StarNight.Map.WorldGeneration.Domain
{
    public readonly struct LocalTileCoord : IEquatable<LocalTileCoord>
    {
        public int X { get; }
        public int Y { get; }

        public LocalTileCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(LocalTileCoord other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is LocalTileCoord other && Equals(other);
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
                "LocalTileCoord({0}, {1})",
                X,
                Y);
        }

        public static bool operator ==(LocalTileCoord left, LocalTileCoord right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LocalTileCoord left, LocalTileCoord right)
        {
            return !left.Equals(right);
        }
    }
}
