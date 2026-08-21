using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public readonly struct Type0RouteOpenMask : IEquatable<Type0RouteOpenMask>, IComparable<Type0RouteOpenMask>
    {
        private const int LeftBit = 1;
        private const int RightBit = 2;
        private const int UpBit = 4;
        private const int DownBit = 8;

        private readonly int bits;

        public Type0RouteOpenMask(bool openLeft, bool openRight, bool openUp, bool openDown)
        {
            if (openLeft && openRight)
            {
                throw new ArgumentException("Type0 route masks cannot open left and right simultaneously.");
            }

            bits = (openLeft ? LeftBit : 0) |
                   (openRight ? RightBit : 0) |
                   (openUp ? UpBit : 0) |
                   (openDown ? DownBit : 0);
        }

        public bool OpenLeft => (bits & LeftBit) != 0;
        public bool OpenRight => (bits & RightBit) != 0;
        public bool OpenUp => (bits & UpBit) != 0;
        public bool OpenDown => (bits & DownBit) != 0;
        public int OpenCount =>
            (OpenLeft ? 1 : 0) + (OpenRight ? 1 : 0) +
            (OpenUp ? 1 : 0) + (OpenDown ? 1 : 0);
        public bool HasHorizontalThrough => OpenLeft && OpenRight;

        public int CompareTo(Type0RouteOpenMask other)
        {
            return bits.CompareTo(other.bits);
        }

        public bool Equals(Type0RouteOpenMask other)
        {
            return bits == other.bits;
        }

        public override bool Equals(object obj)
        {
            return obj is Type0RouteOpenMask other && Equals(other);
        }

        public override int GetHashCode()
        {
            return bits;
        }

        public override string ToString()
        {
            return string.Concat(
                OpenLeft ? "L" : string.Empty,
                OpenRight ? "R" : string.Empty,
                OpenUp ? "U" : string.Empty,
                OpenDown ? "D" : string.Empty);
        }

        public static bool operator ==(Type0RouteOpenMask left, Type0RouteOpenMask right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Type0RouteOpenMask left, Type0RouteOpenMask right)
        {
            return !left.Equals(right);
        }
    }
}
