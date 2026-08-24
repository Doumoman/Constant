using System;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public readonly struct MoonpalaceBiomeId :
        IEquatable<MoonpalaceBiomeId>,
        IComparable<MoonpalaceBiomeId>
    {
        private readonly byte encodedOrder;

        private MoonpalaceBiomeId(int order)
        {
            encodedOrder = checked((byte)(order + 1));
        }

        public static MoonpalaceBiomeId MoonCrater { get; } = new MoonpalaceBiomeId(0);
        public static MoonpalaceBiomeId CassiaRoot { get; } = new MoonpalaceBiomeId(1);
        public static MoonpalaceBiomeId AbandonedMill { get; } = new MoonpalaceBiomeId(2);
        public static MoonpalaceBiomeId MoonDough { get; } = new MoonpalaceBiomeId(3);

        public bool IsDefined => encodedOrder >= 1 && encodedOrder <= 4;

        public int Order
        {
            get
            {
                EnsureDefined();
                return encodedOrder - 1;
            }
        }

        public string CanonicalId
        {
            get
            {
                switch (Order)
                {
                    case 0: return "MoonCrater";
                    case 1: return "CassiaRoot";
                    case 2: return "AbandonedMill";
                    case 3: return "MoonDough";
                    default: throw new InvalidOperationException("Unknown Moonpalace biome order.");
                }
            }
        }

        public string DisplayName
        {
            get
            {
                switch (Order)
                {
                    case 0: return "Moon Crater";
                    case 1: return "Cassia Root";
                    case 2: return "Abandoned Mill";
                    case 3: return "Moon Dough";
                    default: throw new InvalidOperationException("Unknown Moonpalace biome order.");
                }
            }
        }

        public static MoonpalaceBiomeId Parse(string canonicalId)
        {
            if (!TryParse(canonicalId, out var biomeId))
            {
                throw new FormatException("Unknown Moonpalace biome ID: " +
                                          (canonicalId ?? "<null>"));
            }

            return biomeId;
        }

        public static bool TryParse(string canonicalId, out MoonpalaceBiomeId biomeId)
        {
            switch (canonicalId)
            {
                case "MoonCrater":
                    biomeId = MoonCrater;
                    return true;
                case "CassiaRoot":
                    biomeId = CassiaRoot;
                    return true;
                case "AbandonedMill":
                    biomeId = AbandonedMill;
                    return true;
                case "MoonDough":
                    biomeId = MoonDough;
                    return true;
                default:
                    biomeId = default;
                    return false;
            }
        }

        public int CompareTo(MoonpalaceBiomeId other)
        {
            EnsureDefined();
            other.EnsureDefined();
            return encodedOrder.CompareTo(other.encodedOrder);
        }

        public bool Equals(MoonpalaceBiomeId other)
        {
            return encodedOrder == other.encodedOrder;
        }

        public override bool Equals(object obj)
        {
            return obj is MoonpalaceBiomeId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return encodedOrder;
        }

        public override string ToString()
        {
            return CanonicalId;
        }

        public static bool operator ==(MoonpalaceBiomeId left, MoonpalaceBiomeId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MoonpalaceBiomeId left, MoonpalaceBiomeId right)
        {
            return !left.Equals(right);
        }

        public static bool operator <(MoonpalaceBiomeId left, MoonpalaceBiomeId right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(MoonpalaceBiomeId left, MoonpalaceBiomeId right)
        {
            return left.CompareTo(right) > 0;
        }

        private void EnsureDefined()
        {
            if (!IsDefined)
            {
                throw new InvalidOperationException("The Moonpalace biome ID is undefined.");
            }
        }
    }
}
