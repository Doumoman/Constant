using System;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.Population
{
    public sealed class GeneratedStableSpawnId :
        IEquatable<GeneratedStableSpawnId>, IComparable<GeneratedStableSpawnId>
    {
        internal GeneratedStableSpawnId(string canonicalAddressLine)
        {
            Namespace = GeneratedStableSpawnIdFactory.Namespace;
            CanonicalIdentityLine = Namespace + "|" + (canonicalAddressLine ?? string.Empty);
            Value = BakingCanonicalDigest.HashCanonicalLines(new[] { CanonicalIdentityLine });
        }

        public string Namespace { get; }
        public string CanonicalIdentityLine { get; }
        public string Value { get; }
        public bool IsValid => Namespace == GeneratedStableSpawnIdFactory.Namespace &&
            BakingCanonicalDigest.IsLowerHexSha256(Value);

        public int CompareTo(GeneratedStableSpawnId other) => other == null
            ? -1 : string.Compare(Value, other.Value, StringComparison.Ordinal);
        public bool Equals(GeneratedStableSpawnId other) => other != null &&
            string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => Equals(obj as GeneratedStableSpawnId);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
    }

    public static class GeneratedStableSpawnIdFactory
    {
        public const string Namespace = "POPULATION_STABLE_SPAWN_V1";

        public static GeneratedStableSpawnId Create(GeneratedContentSlotAddress address)
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            return new GeneratedStableSpawnId(address.CanonicalLine);
        }
    }
}
