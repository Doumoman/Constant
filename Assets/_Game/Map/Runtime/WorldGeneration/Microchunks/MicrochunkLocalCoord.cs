using System;
using System.Globalization;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public readonly struct MicrochunkLocalCoord : IEquatable<MicrochunkLocalCoord>, IComparable<MicrochunkLocalCoord>
    {
        public int X { get; }
        public int Y { get; }
        public int RowMajorIndex => (Y * MicrochunkConstants.WidthTiles) + X;

        public MicrochunkLocalCoord(int x, int y)
        {
            if (x < 0 || x >= MicrochunkConstants.WidthTiles)
            {
                throw new ArgumentOutOfRangeException(nameof(x));
            }

            if (y < 0 || y >= MicrochunkConstants.HeightTiles)
            {
                throw new ArgumentOutOfRangeException(nameof(y));
            }

            X = x;
            Y = y;
        }

        public static bool TryCreate(int x, int y, out MicrochunkLocalCoord coordinate)
        {
            if (x < 0 || x >= MicrochunkConstants.WidthTiles ||
                y < 0 || y >= MicrochunkConstants.HeightTiles)
            {
                coordinate = default;
                return false;
            }

            coordinate = new MicrochunkLocalCoord(x, y);
            return true;
        }

        public int CompareTo(MicrochunkLocalCoord other)
        {
            return RowMajorIndex.CompareTo(other.RowMajorIndex);
        }

        public bool Equals(MicrochunkLocalCoord other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is MicrochunkLocalCoord other && Equals(other);
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
                "MicrochunkLocalCoord({0}, {1})",
                X,
                Y);
        }

        public static bool operator ==(MicrochunkLocalCoord left, MicrochunkLocalCoord right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MicrochunkLocalCoord left, MicrochunkLocalCoord right)
        {
            return !left.Equals(right);
        }
    }
}
