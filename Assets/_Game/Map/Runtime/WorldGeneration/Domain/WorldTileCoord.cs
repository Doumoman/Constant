using System;
using System.Globalization;

namespace StarNight.Map.WorldGeneration.Domain
{
    public readonly struct WorldTileCoord : IEquatable<WorldTileCoord>
    {
        public int X { get; }
        public int Y { get; }

        public WorldTileCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(WorldTileCoord other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is WorldTileCoord other && Equals(other);
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
                "WorldTileCoord({0}, {1})",
                X,
                Y);
        }

        public static bool operator ==(WorldTileCoord left, WorldTileCoord right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(WorldTileCoord left, WorldTileCoord right)
        {
            return !left.Equals(right);
        }
    }
}
