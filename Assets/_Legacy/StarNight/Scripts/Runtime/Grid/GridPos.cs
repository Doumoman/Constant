#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Grid
{
    [Serializable]
    public struct GridPos : IEquatable<GridPos>
    {
        [SerializeField] private int x;
        [SerializeField] private int y;

        public GridPos(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public int X => x;
        public int Y => y;

        public bool Equals(GridPos other)
        {
            return x == other.x && y == other.y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPos other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (x * 397) ^ y;
            }
        }

        public override string ToString()
        {
            return $"({x}, {y})";
        }

        public static bool operator ==(GridPos left, GridPos right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridPos left, GridPos right)
        {
            return !left.Equals(right);
        }

        public static GridPos operator +(GridPos left, GridPos right)
        {
            return new GridPos(left.x + right.x, left.y + right.y);
        }
    }
}

#endif
