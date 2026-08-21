#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Map
{
    [Serializable]
    public readonly struct GridCell : IEquatable<GridCell>
    {
        public readonly int X;
        public readonly int Y;

        public GridCell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public GridCell(Vector2Int value)
            : this(value.x, value.y)
        {
        }

        public Vector2Int ToVector2Int()
        {
            return new Vector2Int(X, Y);
        }

        public bool Equals(GridCell other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridCell other && Equals(other);
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
            return $"({X}, {Y})";
        }

        public static GridCell operator +(GridCell left, GridCell right)
        {
            return new GridCell(left.X + right.X, left.Y + right.Y);
        }

        public static GridCell operator -(GridCell left, GridCell right)
        {
            return new GridCell(left.X - right.X, left.Y - right.Y);
        }

        public static bool operator ==(GridCell left, GridCell right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridCell left, GridCell right)
        {
            return !left.Equals(right);
        }
    }
}

#endif
