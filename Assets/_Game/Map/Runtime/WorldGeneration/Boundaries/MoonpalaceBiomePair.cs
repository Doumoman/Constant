using System;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public readonly struct MoonpalaceBiomePair :
        IEquatable<MoonpalaceBiomePair>,
        IComparable<MoonpalaceBiomePair>
    {
        private const string Separator = "<->";

        public MoonpalaceBiomePair(MoonpalaceBiomeId biomeA, MoonpalaceBiomeId biomeB)
        {
            if (!biomeA.IsDefined) throw new ArgumentException("Biome A is undefined.", nameof(biomeA));
            if (!biomeB.IsDefined) throw new ArgumentException("Biome B is undefined.", nameof(biomeB));
            if (biomeA == biomeB) throw new ArgumentException("A Moonpalace biome pair cannot be a self-pair.");

            if (biomeA < biomeB)
            {
                First = biomeA;
                Second = biomeB;
            }
            else
            {
                First = biomeB;
                Second = biomeA;
            }
        }

        public MoonpalaceBiomeId First { get; }
        public MoonpalaceBiomeId Second { get; }
        public bool IsDefined => First.IsDefined && Second.IsDefined && First != Second;
        public string PairId => First.CanonicalId + Separator + Second.CanonicalId;

        public static MoonpalaceBiomePair Parse(string pairId)
        {
            if (!TryParse(pairId, out var pair))
            {
                throw new FormatException("Unknown Moonpalace biome pair ID: " + (pairId ?? "<null>"));
            }

            return pair;
        }

        public static bool TryParse(string pairId, out MoonpalaceBiomePair pair)
        {
            pair = default;
            if (string.IsNullOrEmpty(pairId)) return false;

            var separatorIndex = pairId.IndexOf(Separator, StringComparison.Ordinal);
            if (separatorIndex <= 0 ||
                separatorIndex != pairId.LastIndexOf(Separator, StringComparison.Ordinal))
            {
                return false;
            }

            var firstText = pairId.Substring(0, separatorIndex);
            var secondText = pairId.Substring(separatorIndex + Separator.Length);
            if (!MoonpalaceBiomeId.TryParse(firstText, out var first) ||
                !MoonpalaceBiomeId.TryParse(secondText, out var second) ||
                first == second)
            {
                return false;
            }

            pair = new MoonpalaceBiomePair(first, second);
            return true;
        }

        public int CompareTo(MoonpalaceBiomePair other)
        {
            EnsureDefined();
            other.EnsureDefined();
            var firstComparison = First.CompareTo(other.First);
            return firstComparison != 0 ? firstComparison : Second.CompareTo(other.Second);
        }

        public bool Equals(MoonpalaceBiomePair other)
        {
            return First == other.First && Second == other.Second;
        }

        public override bool Equals(object obj)
        {
            return obj is MoonpalaceBiomePair other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (First.GetHashCode() * 397) ^ Second.GetHashCode();
            }
        }

        public override string ToString()
        {
            EnsureDefined();
            return PairId;
        }

        public static bool operator ==(MoonpalaceBiomePair left, MoonpalaceBiomePair right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MoonpalaceBiomePair left, MoonpalaceBiomePair right)
        {
            return !left.Equals(right);
        }

        private void EnsureDefined()
        {
            if (!IsDefined)
            {
                throw new InvalidOperationException("The Moonpalace biome pair is undefined.");
            }
        }
    }
}
