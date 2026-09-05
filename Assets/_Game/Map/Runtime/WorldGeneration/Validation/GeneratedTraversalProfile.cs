using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.WorldGeneration.Validation
{
    public sealed class GeneratedTraversalProfileVersion
    {
        public GeneratedTraversalProfileVersion(
            string profileNamespace,
            string version,
            string dataVersion,
            string generatorVersion)
        {
            ProfileNamespace = Normalize(profileNamespace);
            Version = Normalize(version);
            DataVersion = Normalize(dataVersion);
            GeneratorVersion = Normalize(generatorVersion);
        }

        public string ProfileNamespace { get; }
        public string Version { get; }
        public string DataVersion { get; }
        public string GeneratorVersion { get; }
        public bool IsValid => !string.IsNullOrEmpty(ProfileNamespace) &&
            !string.IsNullOrEmpty(Version) && !string.IsNullOrEmpty(DataVersion) &&
            !string.IsNullOrEmpty(GeneratorVersion);
        public string StableToken => string.Join("|", new[]
        {
            "TRAVERSAL_PROFILE_VERSION", ProfileNamespace, Version,
            DataVersion, GeneratorVersion,
        });

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
    }

    public enum GeneratedTraversalCapabilityGroup
    {
        Collider = 1,
        Jump = 2,
        AirControl = 3,
        Climb = 4,
        Bounce = 5,
        Fall = 6,
        ChaseWidth = 7,
    }

    public enum GeneratedTraversalCapabilityValueKind
    {
        Integer = 1,
        Decimal = 2,
        Boolean = 3,
        Range = 4,
        Enum = 5,
    }

    public enum GeneratedTraversalThresholdPolicy
    {
        Exact = 1,
        InclusiveRange = 2,
    }

    public sealed class GeneratedTraversalCapabilityValue :
        IComparable<GeneratedTraversalCapabilityValue>
    {
        public GeneratedTraversalCapabilityValue(
            string key,
            GeneratedTraversalCapabilityGroup group,
            GeneratedTraversalCapabilityValueKind valueKind,
            string unit,
            string sourceLabel,
            GeneratedTraversalThresholdPolicy thresholdPolicy,
            decimal minimumValue,
            decimal maximumValue,
            string exactValue,
            string consumerPhase)
        {
            Key = Normalize(key);
            Group = group;
            ValueKind = valueKind;
            Unit = Normalize(unit);
            SourceLabel = Normalize(sourceLabel);
            ThresholdPolicy = thresholdPolicy;
            MinimumValue = minimumValue;
            MaximumValue = maximumValue;
            ExactValue = Normalize(exactValue);
            ConsumerPhase = Normalize(consumerPhase);
        }

        public string Key { get; }
        public GeneratedTraversalCapabilityGroup Group { get; }
        public GeneratedTraversalCapabilityValueKind ValueKind { get; }
        public string Unit { get; }
        public string SourceLabel { get; }
        public GeneratedTraversalThresholdPolicy ThresholdPolicy { get; }
        public decimal MinimumValue { get; }
        public decimal MaximumValue { get; }
        public string ExactValue { get; }
        public string ConsumerPhase { get; }

        public bool HasValidThreshold
        {
            get
            {
                if (!Enum.IsDefined(typeof(GeneratedTraversalCapabilityGroup), Group) ||
                    !Enum.IsDefined(typeof(GeneratedTraversalCapabilityValueKind), ValueKind) ||
                    !Enum.IsDefined(typeof(GeneratedTraversalThresholdPolicy), ThresholdPolicy) ||
                    string.IsNullOrEmpty(Key) || string.IsNullOrEmpty(Unit) ||
                    string.IsNullOrEmpty(SourceLabel) || string.IsNullOrEmpty(ExactValue) ||
                    string.IsNullOrEmpty(ConsumerPhase) || MinimumValue > MaximumValue)
                    return false;

                if (ThresholdPolicy == GeneratedTraversalThresholdPolicy.Exact &&
                    MinimumValue != MaximumValue)
                    return false;

                if (ValueKind == GeneratedTraversalCapabilityValueKind.Integer &&
                    (decimal.Truncate(MinimumValue) != MinimumValue ||
                     decimal.Truncate(MaximumValue) != MaximumValue))
                    return false;

                if (ValueKind == GeneratedTraversalCapabilityValueKind.Boolean)
                {
                    return ThresholdPolicy == GeneratedTraversalThresholdPolicy.Exact &&
                        (string.Equals(ExactValue, "true", StringComparison.Ordinal) ||
                         string.Equals(ExactValue, "false", StringComparison.Ordinal));
                }

                return true;
            }
        }

        public string StableToken => string.Join("|", new[]
        {
            "CAPABILITY", Key, Number((int)Group), Number((int)ValueKind), Unit,
            SourceLabel, Number((int)ThresholdPolicy), Number(MinimumValue),
            Number(MaximumValue), ExactValue, ConsumerPhase,
        });

        public int CompareTo(GeneratedTraversalCapabilityValue other) => other == null
            ? -1
            : string.Compare(Key, other.Key, StringComparison.Ordinal);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Number(decimal value) =>
            value.ToString("0.############################", CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedTraversalProfile
    {
        private readonly ReadOnlyCollection<GeneratedTraversalCapabilityValue> capabilities;
        private readonly ReadOnlyCollection<TraversalMovementKind> movementKinds;
        private readonly ReadOnlyCollection<CompiledTraversalEnvelopeSetKind> envelopeKinds;

        public GeneratedTraversalProfile(
            GeneratedTraversalProfileVersion version,
            IEnumerable<GeneratedTraversalCapabilityValue> sourceCapabilities,
            IEnumerable<TraversalMovementKind> sourceMovementKinds,
            IEnumerable<CompiledTraversalEnvelopeSetKind> sourceEnvelopeKinds,
            string declaredDigest = null)
        {
            Version = version;
            capabilities = new ReadOnlyCollection<GeneratedTraversalCapabilityValue>(
                (sourceCapabilities ?? Array.Empty<GeneratedTraversalCapabilityValue>())
                    .Where(value => value != null)
                    .OrderBy(value => value)
                    .ToArray());
            movementKinds = new ReadOnlyCollection<TraversalMovementKind>(
                (sourceMovementKinds ?? Array.Empty<TraversalMovementKind>())
                    .OrderBy(value => (int)value)
                    .ToArray());
            envelopeKinds = new ReadOnlyCollection<CompiledTraversalEnvelopeSetKind>(
                (sourceEnvelopeKinds ?? Array.Empty<CompiledTraversalEnvelopeSetKind>())
                    .OrderBy(value => (int)value)
                    .ToArray());
            Digest = declaredDigest ?? ComputeDigest();
        }

        public GeneratedTraversalProfileVersion Version { get; }
        public IReadOnlyList<GeneratedTraversalCapabilityValue> Capabilities => capabilities;
        public IReadOnlyList<TraversalMovementKind> MovementKinds => movementKinds;
        public IReadOnlyList<CompiledTraversalEnvelopeSetKind> EnvelopeKinds => envelopeKinds;
        public string Digest { get; }
        public int CentralizedProductionValueOwnerCount => capabilities
            .Select(value => value.SourceLabel)
            .Distinct(StringComparer.Ordinal)
            .Count();

        public IReadOnlyList<GeneratedTraversalCapabilityValue> ForGroup(
            GeneratedTraversalCapabilityGroup group) => new ReadOnlyCollection<GeneratedTraversalCapabilityValue>(
                capabilities.Where(value => value.Group == group).ToArray());

        public string ComputeDigest()
        {
            var lines = new List<string>
            {
                "GENERATED_TRAVERSAL_PROFILE_V1",
                Version == null ? "MISSING" : Version.StableToken,
            };
            lines.AddRange(movementKinds.Select(value =>
                "MOVEMENT|" + ((int)value).ToString(CultureInfo.InvariantCulture) + "|" + value));
            lines.AddRange(envelopeKinds.Select(value =>
                "ENVELOPE|" + ((int)value).ToString(CultureInfo.InvariantCulture) + "|" + value));
            lines.AddRange(capabilities.Select(value => value.StableToken));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }
    }

    public static class GeneratedTraversalProfileCatalog
    {
        public const string ValueSourceLabel =
            "MAP19_01_VALIDATION_BASELINE_NOT_PLAYER_TUNING";
        public const string ConsumerPhase = "MAP19_TILE_MOVEMENT_VALIDATION";

        public static GeneratedTraversalProfile Create()
        {
            var version = new GeneratedTraversalProfileVersion(
                "STARNIGHT_MAP19_TRAVERSAL", "1.0.0", "MAP19_DATA_V1",
                "MAP19_PROFILE_RULE_LOCK_V1");
            var values = new[]
            {
                ExactDecimal("collider.footprint_width_tiles", GeneratedTraversalCapabilityGroup.Collider, 0.75m, "tiles"),
                ExactDecimal("collider.footprint_height_tiles", GeneratedTraversalCapabilityGroup.Collider, 1.80m, "tiles"),
                ExactInteger("collider.clearance_width_tiles", GeneratedTraversalCapabilityGroup.Collider, 1, "tiles"),
                ExactInteger("collider.clearance_height_tiles", GeneratedTraversalCapabilityGroup.Collider, 2, "tiles"),
                Range("jump.horizontal_range_tiles", GeneratedTraversalCapabilityGroup.Jump, 1m, 6m, "tiles"),
                Range("jump.vertical_rise_tiles", GeneratedTraversalCapabilityGroup.Jump, 1m, 4m, "tiles"),
                ExactInteger("jump.minimum_landing_width_tiles", GeneratedTraversalCapabilityGroup.Jump, 1, "tiles"),
                Boolean("air_control.enabled", GeneratedTraversalCapabilityGroup.AirControl, true),
                Range("air_control.horizontal_correction_tiles", GeneratedTraversalCapabilityGroup.AirControl, 0m, 2m, "tiles"),
                Boolean("climb.enabled", GeneratedTraversalCapabilityGroup.Climb, true),
                Range("climb.vertical_reach_tiles", GeneratedTraversalCapabilityGroup.Climb, 1m, 6m, "tiles"),
                Boolean("bounce.enabled", GeneratedTraversalCapabilityGroup.Bounce, true),
                Range("bounce.horizontal_range_tiles", GeneratedTraversalCapabilityGroup.Bounce, 1m, 8m, "tiles"),
                Range("bounce.vertical_rise_tiles", GeneratedTraversalCapabilityGroup.Bounce, 1m, 6m, "tiles"),
                Range("fall.safe_drop_tiles", GeneratedTraversalCapabilityGroup.Fall, 0m, 8m, "tiles"),
                ExactInteger("fall.validation_limit_tiles", GeneratedTraversalCapabilityGroup.Fall, 12, "tiles"),
                ExactInteger("chase_width.minimum_tiles", GeneratedTraversalCapabilityGroup.ChaseWidth, 3, "tiles"),
                Range("chase_width.preferred_tiles", GeneratedTraversalCapabilityGroup.ChaseWidth, 3m, 6m, "tiles"),
            };
            return new GeneratedTraversalProfile(version, values,
                Enum.GetValues(typeof(TraversalMovementKind)).Cast<TraversalMovementKind>(),
                Enum.GetValues(typeof(CompiledTraversalEnvelopeSetKind))
                    .Cast<CompiledTraversalEnvelopeSetKind>());
        }

        private static GeneratedTraversalCapabilityValue ExactInteger(
            string key, GeneratedTraversalCapabilityGroup group, int value, string unit) =>
            Value(key, group, GeneratedTraversalCapabilityValueKind.Integer, unit,
                GeneratedTraversalThresholdPolicy.Exact, value, value,
                value.ToString(CultureInfo.InvariantCulture));

        private static GeneratedTraversalCapabilityValue ExactDecimal(
            string key, GeneratedTraversalCapabilityGroup group, decimal value, string unit) =>
            Value(key, group, GeneratedTraversalCapabilityValueKind.Decimal, unit,
                GeneratedTraversalThresholdPolicy.Exact, value, value,
                value.ToString("0.############################", CultureInfo.InvariantCulture));

        private static GeneratedTraversalCapabilityValue Range(
            string key, GeneratedTraversalCapabilityGroup group,
            decimal minimum, decimal maximum, string unit) =>
            Value(key, group, GeneratedTraversalCapabilityValueKind.Range, unit,
                GeneratedTraversalThresholdPolicy.InclusiveRange, minimum, maximum,
                minimum.ToString(CultureInfo.InvariantCulture) + ".." +
                maximum.ToString(CultureInfo.InvariantCulture));

        private static GeneratedTraversalCapabilityValue Boolean(
            string key, GeneratedTraversalCapabilityGroup group, bool value) =>
            Value(key, group, GeneratedTraversalCapabilityValueKind.Boolean, "bool",
                GeneratedTraversalThresholdPolicy.Exact, value ? 1m : 0m,
                value ? 1m : 0m, value ? "true" : "false");

        private static GeneratedTraversalCapabilityValue Value(
            string key,
            GeneratedTraversalCapabilityGroup group,
            GeneratedTraversalCapabilityValueKind kind,
            string unit,
            GeneratedTraversalThresholdPolicy policy,
            decimal minimum,
            decimal maximum,
            string exact) => new GeneratedTraversalCapabilityValue(
                key, group, kind, unit, ValueSourceLabel, policy,
                minimum, maximum, exact, ConsumerPhase);
    }
}
