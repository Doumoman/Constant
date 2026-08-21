using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public readonly struct MandatoryRouteOpenMask : IEquatable<MandatoryRouteOpenMask>, IComparable<MandatoryRouteOpenMask>
    {
        public MandatoryRouteOpenMask(bool openLeft, bool openRight, bool openUp, bool openDown)
        {
            OpenLeft = openLeft;
            OpenRight = openRight;
            OpenUp = openUp;
            OpenDown = openDown;
        }

        public bool OpenLeft { get; }
        public bool OpenRight { get; }
        public bool OpenUp { get; }
        public bool OpenDown { get; }
        public int OpenCount => (OpenLeft ? 1 : 0) + (OpenRight ? 1 : 0) + (OpenUp ? 1 : 0) + (OpenDown ? 1 : 0);
        public bool HasHorizontalRun => OpenLeft && OpenRight;
        public bool HasVerticalPairConflict => OpenUp && OpenDown;

        public static MandatoryRouteOpenMask Type1Horizontal => new MandatoryRouteOpenMask(true, true, false, false);
        public static MandatoryRouteOpenMask Type2Down => new MandatoryRouteOpenMask(true, true, false, true);
        public static MandatoryRouteOpenMask Type3Up => new MandatoryRouteOpenMask(true, true, true, false);

        public bool Equals(MandatoryRouteOpenMask other) => Bits == other.Bits;
        public override bool Equals(object obj) => obj is MandatoryRouteOpenMask other && Equals(other);
        public int CompareTo(MandatoryRouteOpenMask other) => Bits.CompareTo(other.Bits);
        public override int GetHashCode() => Bits;
        public override string ToString() => string.Concat(OpenLeft ? "L" : "-", OpenRight ? "R" : "-", OpenUp ? "U" : "-", OpenDown ? "D" : "-");
        public static bool operator ==(MandatoryRouteOpenMask left, MandatoryRouteOpenMask right) => left.Equals(right);
        public static bool operator !=(MandatoryRouteOpenMask left, MandatoryRouteOpenMask right) => !left.Equals(right);

        private int Bits => (OpenLeft ? 1 : 0) | (OpenRight ? 2 : 0) | (OpenUp ? 4 : 0) | (OpenDown ? 8 : 0);
    }
}
