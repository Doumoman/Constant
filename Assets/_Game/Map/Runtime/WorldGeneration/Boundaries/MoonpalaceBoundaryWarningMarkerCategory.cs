using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public readonly struct MoonpalaceBoundaryWarningMarkerCategory :
        IEquatable<MoonpalaceBoundaryWarningMarkerCategory>,
        IComparable<MoonpalaceBoundaryWarningMarkerCategory>
    {
        private readonly byte encodedOrder;

        private MoonpalaceBoundaryWarningMarkerCategory(int order)
        {
            encodedOrder = checked((byte)(order + 1));
        }

        public static MoonpalaceBoundaryWarningMarkerCategory Tile { get; } =
            new MoonpalaceBoundaryWarningMarkerCategory(0);

        public static MoonpalaceBoundaryWarningMarkerCategory Background { get; } =
            new MoonpalaceBoundaryWarningMarkerCategory(1);

        public static MoonpalaceBoundaryWarningMarkerCategory Resource { get; } =
            new MoonpalaceBoundaryWarningMarkerCategory(2);

        public static MoonpalaceBoundaryWarningMarkerCategory Audio { get; } =
            new MoonpalaceBoundaryWarningMarkerCategory(3);

        public static IReadOnlyList<MoonpalaceBoundaryWarningMarkerCategory> CanonicalValues { get; } =
            new ReadOnlyCollection<MoonpalaceBoundaryWarningMarkerCategory>(new[]
            {
                Tile,
                Background,
                Resource,
                Audio,
            });

        public bool IsDefined => encodedOrder >= 1 && encodedOrder <= 4;

        public int Order
        {
            get
            {
                EnsureDefined();
                return encodedOrder - 1;
            }
        }

        public string Token
        {
            get
            {
                switch (Order)
                {
                    case 0: return "Tile";
                    case 1: return "Background";
                    case 2: return "Resource";
                    case 3: return "Audio";
                    default: throw new InvalidOperationException("Unknown warning marker category.");
                }
            }
        }

        public MoonpalaceBoundaryWarningMarker Marker
        {
            get
            {
                switch (Order)
                {
                    case 0: return MoonpalaceBoundaryWarningMarker.Tile;
                    case 1: return MoonpalaceBoundaryWarningMarker.Background;
                    case 2: return MoonpalaceBoundaryWarningMarker.Resource;
                    case 3: return MoonpalaceBoundaryWarningMarker.Audio;
                    default: throw new InvalidOperationException("Unknown warning marker category.");
                }
            }
        }

        public static MoonpalaceBoundaryWarningMarkerCategory Parse(string value)
        {
            if (!TryParse(value, out var category))
            {
                throw new ArgumentException("Unknown boundary warning marker category token.", nameof(value));
            }

            return category;
        }

        public static bool TryParse(
            string value,
            out MoonpalaceBoundaryWarningMarkerCategory category)
        {
            switch (value)
            {
                case "Tile":
                    category = Tile;
                    return true;
                case "Background":
                    category = Background;
                    return true;
                case "Resource":
                    category = Resource;
                    return true;
                case "Audio":
                    category = Audio;
                    return true;
                default:
                    category = default;
                    return false;
            }
        }

        public int CompareTo(MoonpalaceBoundaryWarningMarkerCategory other)
        {
            EnsureDefined();
            other.EnsureDefined();
            return encodedOrder.CompareTo(other.encodedOrder);
        }

        public bool Equals(MoonpalaceBoundaryWarningMarkerCategory other)
        {
            return encodedOrder == other.encodedOrder;
        }

        public override bool Equals(object obj)
        {
            return obj is MoonpalaceBoundaryWarningMarkerCategory other && Equals(other);
        }

        public override int GetHashCode()
        {
            return encodedOrder;
        }

        public override string ToString()
        {
            return Token;
        }

        public static bool operator ==(
            MoonpalaceBoundaryWarningMarkerCategory left,
            MoonpalaceBoundaryWarningMarkerCategory right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            MoonpalaceBoundaryWarningMarkerCategory left,
            MoonpalaceBoundaryWarningMarkerCategory right)
        {
            return !left.Equals(right);
        }

        private void EnsureDefined()
        {
            if (!IsDefined)
            {
                throw new InvalidOperationException("The warning marker category is undefined.");
            }
        }
    }
}
