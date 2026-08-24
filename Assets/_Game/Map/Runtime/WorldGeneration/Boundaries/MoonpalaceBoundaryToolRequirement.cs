using System;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public readonly struct MoonpalaceBoundaryToolRequirement :
        IEquatable<MoonpalaceBoundaryToolRequirement>,
        IComparable<MoonpalaceBoundaryToolRequirement>
    {
        private readonly string token;

        private MoonpalaceBoundaryToolRequirement(string token)
        {
            this.token = token;
        }

        public static MoonpalaceBoundaryToolRequirement None { get; } =
            new MoonpalaceBoundaryToolRequirement("NONE");

        public static MoonpalaceBoundaryToolRequirement Pickaxe { get; } =
            new MoonpalaceBoundaryToolRequirement("Pickaxe");

        public static MoonpalaceBoundaryToolRequirement Rope { get; } =
            new MoonpalaceBoundaryToolRequirement("Rope");

        public static MoonpalaceBoundaryToolRequirement Bomb { get; } =
            new MoonpalaceBoundaryToolRequirement("Bomb");

        public static MoonpalaceBoundaryToolRequirement KeyItem { get; } =
            new MoonpalaceBoundaryToolRequirement("KeyItem");

        public bool IsDefined => token != null;

        public string Token
        {
            get
            {
                if (!IsDefined) throw new InvalidOperationException("Tool requirement is undefined.");
                return token;
            }
        }

        public static MoonpalaceBoundaryToolRequirement Parse(string value)
        {
            if (!TryParse(value, out var requirement))
            {
                throw new ArgumentException("Unknown boundary tool requirement token.", nameof(value));
            }

            return requirement;
        }

        public static bool TryParse(string value, out MoonpalaceBoundaryToolRequirement requirement)
        {
            switch (value)
            {
                case "NONE":
                    requirement = None;
                    return true;
                case "Pickaxe":
                    requirement = Pickaxe;
                    return true;
                case "Rope":
                    requirement = Rope;
                    return true;
                case "Bomb":
                    requirement = Bomb;
                    return true;
                case "KeyItem":
                    requirement = KeyItem;
                    return true;
                default:
                    requirement = default;
                    return false;
            }
        }

        public int CompareTo(MoonpalaceBoundaryToolRequirement other)
        {
            return string.Compare(Token, other.Token, StringComparison.Ordinal);
        }

        public bool Equals(MoonpalaceBoundaryToolRequirement other)
        {
            return string.Equals(token, other.token, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is MoonpalaceBoundaryToolRequirement other && Equals(other);
        }

        public override int GetHashCode()
        {
            if (token == null) return 0;
            unchecked
            {
                var hash = 17;
                for (var index = 0; index < token.Length; index++) hash = (hash * 31) + token[index];
                return hash;
            }
        }

        public override string ToString()
        {
            return Token;
        }

        public static bool operator ==(
            MoonpalaceBoundaryToolRequirement left,
            MoonpalaceBoundaryToolRequirement right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            MoonpalaceBoundaryToolRequirement left,
            MoonpalaceBoundaryToolRequirement right)
        {
            return !left.Equals(right);
        }
    }
}
