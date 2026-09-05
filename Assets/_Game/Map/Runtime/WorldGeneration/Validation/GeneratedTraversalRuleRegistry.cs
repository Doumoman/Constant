using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.WorldGeneration.Validation
{
    public enum GeneratedTraversalRuleSeverity
    {
        Critical = 1,
        Error = 2,
        Warning = 3,
        Info = 4,
    }

    public enum GeneratedTraversalRuleOwner
    {
        ProfileCompleteness = 1,
        MovementKindSupport = 2,
        EnvelopeSupport = 3,
        ColliderClearance = 4,
        JumpArcEnvelope = 5,
        DropRecovery = 6,
        ClimbReach = 7,
        BounceArc = 8,
        FallLimit = 9,
        ChaseWidth = 10,
        RouteCriticality = 11,
        DigestCompatibility = 12,
    }

    public enum GeneratedTraversalRuleFailurePolicy
    {
        None = 0,
        RejectProfileLock = 1,
        BlockRouteCriticalValidation = 2,
        ReportWithoutRelaxation = 3,
    }

    public sealed class GeneratedTraversalRuleThreshold
    {
        public GeneratedTraversalRuleThreshold(
            string key,
            decimal minimumValue,
            decimal maximumValue,
            string expectedValue)
        {
            Key = Normalize(key);
            MinimumValue = minimumValue;
            MaximumValue = maximumValue;
            ExpectedValue = Normalize(expectedValue);
        }

        public string Key { get; }
        public decimal MinimumValue { get; }
        public decimal MaximumValue { get; }
        public string ExpectedValue { get; }
        public bool IsValid => !string.IsNullOrEmpty(Key) &&
            !string.IsNullOrEmpty(ExpectedValue) && MinimumValue <= MaximumValue;
        public string StableToken => string.Join("|", new[]
        {
            "THRESHOLD", Key, Number(MinimumValue), Number(MaximumValue), ExpectedValue,
        });

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
        private static string Number(decimal value) =>
            value.ToString("0.############################", CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedTraversalValidationRule :
        IComparable<GeneratedTraversalValidationRule>
    {
        private readonly ReadOnlyCollection<TraversalMovementKind> targetMovementKinds;
        private readonly ReadOnlyCollection<CompiledTraversalEnvelopeSetKind> targetEnvelopeKinds;

        public GeneratedTraversalValidationRule(
            string ruleId,
            GeneratedTraversalRuleOwner owner,
            GeneratedTraversalRuleSeverity severity,
            GeneratedTraversalRuleThreshold threshold,
            IEnumerable<TraversalMovementKind> sourceMovementKinds,
            IEnumerable<CompiledTraversalEnvelopeSetKind> sourceEnvelopeKinds,
            GeneratedTraversalRuleFailurePolicy failurePolicy,
            string consumerTask)
        {
            RuleId = Normalize(ruleId);
            Owner = owner;
            Severity = severity;
            Threshold = threshold;
            targetMovementKinds = new ReadOnlyCollection<TraversalMovementKind>(
                (sourceMovementKinds ?? Array.Empty<TraversalMovementKind>())
                    .OrderBy(value => (int)value).ToArray());
            targetEnvelopeKinds = new ReadOnlyCollection<CompiledTraversalEnvelopeSetKind>(
                (sourceEnvelopeKinds ?? Array.Empty<CompiledTraversalEnvelopeSetKind>())
                    .OrderBy(value => (int)value).ToArray());
            FailurePolicy = failurePolicy;
            ConsumerTask = Normalize(consumerTask);
        }

        public string RuleId { get; }
        public GeneratedTraversalRuleOwner Owner { get; }
        public GeneratedTraversalRuleSeverity Severity { get; }
        public GeneratedTraversalRuleThreshold Threshold { get; }
        public IReadOnlyList<TraversalMovementKind> TargetMovementKinds => targetMovementKinds;
        public IReadOnlyList<CompiledTraversalEnvelopeSetKind> TargetEnvelopeKinds => targetEnvelopeKinds;
        public GeneratedTraversalRuleFailurePolicy FailurePolicy { get; }
        public string ConsumerTask { get; }
        public string StableToken => string.Join("|", new[]
        {
            "RULE", RuleId, Number((int)Owner), Number((int)Severity),
            Threshold == null ? "MISSING" : Threshold.StableToken,
            string.Join(",", targetMovementKinds.Select(value => Number((int)value))),
            string.Join(",", targetEnvelopeKinds.Select(value => Number((int)value))),
            Number((int)FailurePolicy), ConsumerTask,
        });

        public int CompareTo(GeneratedTraversalValidationRule other) => other == null
            ? -1
            : string.Compare(RuleId, other.RuleId, StringComparison.Ordinal);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedTraversalMovementEnvelopeRequirement :
        IComparable<GeneratedTraversalMovementEnvelopeRequirement>
    {
        private readonly ReadOnlyCollection<CompiledTraversalEnvelopeSetKind> requiredEnvelopeKinds;

        public GeneratedTraversalMovementEnvelopeRequirement(
            TraversalMovementKind movementKind,
            IEnumerable<CompiledTraversalEnvelopeSetKind> sourceEnvelopeKinds)
        {
            MovementKind = movementKind;
            requiredEnvelopeKinds = new ReadOnlyCollection<CompiledTraversalEnvelopeSetKind>(
                (sourceEnvelopeKinds ?? Array.Empty<CompiledTraversalEnvelopeSetKind>())
                    .OrderBy(value => (int)value).ToArray());
        }

        public TraversalMovementKind MovementKind { get; }
        public IReadOnlyList<CompiledTraversalEnvelopeSetKind> RequiredEnvelopeKinds =>
            requiredEnvelopeKinds;
        public string StableToken => "MATRIX|" + Number((int)MovementKind) + "|" +
            string.Join(",", requiredEnvelopeKinds.Select(value => Number((int)value)));

        public int CompareTo(GeneratedTraversalMovementEnvelopeRequirement other) => other == null
            ? -1
            : ((int)MovementKind).CompareTo((int)other.MovementKind);

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedTraversalRuleRegistry
    {
        private readonly ReadOnlyCollection<GeneratedTraversalValidationRule> rules;
        private readonly ReadOnlyCollection<GeneratedTraversalMovementEnvelopeRequirement> matrix;
        private readonly ReadOnlyCollection<ClusterTraversalProtectionSourceKind> protectionSourceKinds;

        public GeneratedTraversalRuleRegistry(
            string version,
            string profileCompatibilityDigest,
            IEnumerable<GeneratedTraversalValidationRule> sourceRules,
            IEnumerable<GeneratedTraversalMovementEnvelopeRequirement> sourceMatrix,
            IEnumerable<ClusterTraversalProtectionSourceKind> sourceProtectionKinds,
            string declaredRegistryDigest = null,
            string declaredMatrixDigest = null)
        {
            Version = Normalize(version);
            ProfileCompatibilityDigest = Normalize(profileCompatibilityDigest);
            rules = new ReadOnlyCollection<GeneratedTraversalValidationRule>(
                (sourceRules ?? Array.Empty<GeneratedTraversalValidationRule>())
                    .Where(value => value != null).OrderBy(value => value).ToArray());
            matrix = new ReadOnlyCollection<GeneratedTraversalMovementEnvelopeRequirement>(
                (sourceMatrix ?? Array.Empty<GeneratedTraversalMovementEnvelopeRequirement>())
                    .Where(value => value != null).OrderBy(value => value).ToArray());
            protectionSourceKinds = new ReadOnlyCollection<ClusterTraversalProtectionSourceKind>(
                (sourceProtectionKinds ?? Array.Empty<ClusterTraversalProtectionSourceKind>())
                    .OrderBy(value => (int)value).ToArray());
            MovementEnvelopeMatrixDigest = declaredMatrixDigest ?? ComputeMatrixDigest();
            RegistryDigest = declaredRegistryDigest ?? ComputeRegistryDigest();
        }

        public string Version { get; }
        public string ProfileCompatibilityDigest { get; }
        public IReadOnlyList<GeneratedTraversalValidationRule> Rules => rules;
        public IReadOnlyList<GeneratedTraversalMovementEnvelopeRequirement> MovementEnvelopeMatrix => matrix;
        public IReadOnlyList<ClusterTraversalProtectionSourceKind> ProtectionSourceKinds =>
            protectionSourceKinds;
        public string RegistryDigest { get; }
        public string MovementEnvelopeMatrixDigest { get; }

        public string ComputeMatrixDigest() => BakingCanonicalDigest.HashCanonicalLines(
            new[] { "MAP19_MOVEMENT_ENVELOPE_MATRIX_V1" }
                .Concat(matrix.Select(value => value.StableToken)));

        public string ComputeRegistryDigest()
        {
            var lines = new List<string>
            {
                "MAP19_TRAVERSAL_RULE_REGISTRY_V1", Version,
                ProfileCompatibilityDigest, MovementEnvelopeMatrixDigest,
            };
            lines.AddRange(protectionSourceKinds.Select(value =>
                "PROTECTION_SOURCE|" + ((int)value).ToString(CultureInfo.InvariantCulture) + "|" + value));
            lines.AddRange(rules.Select(value => value.StableToken));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
    }

    public static class GeneratedTraversalRuleCatalog
    {
        public const string Version = "MAP19_TRAVERSAL_RULE_REGISTRY_V1";
        public const string ConsumerTask = "MAP19_02_BUILD_TILE_MOVEMENT_GRAPH";

        public static GeneratedTraversalRuleRegistry Create(GeneratedTraversalProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            var allMovements = profile.MovementKinds.ToArray();
            var allEnvelopes = profile.EnvelopeKinds.ToArray();
            var rules = new[]
            {
                Rule("TRV-PROFILE-001", GeneratedTraversalRuleOwner.ProfileCompleteness,
                    GeneratedTraversalRuleSeverity.Critical, Count("required.capability_groups", 7),
                    allMovements, allEnvelopes, GeneratedTraversalRuleFailurePolicy.RejectProfileLock),
                Rule("TRV-MOVEMENT-002", GeneratedTraversalRuleOwner.MovementKindSupport,
                    GeneratedTraversalRuleSeverity.Critical, Count("required.movement_kinds", 6),
                    allMovements, Array.Empty<CompiledTraversalEnvelopeSetKind>(),
                    GeneratedTraversalRuleFailurePolicy.RejectProfileLock),
                Rule("TRV-ENVELOPE-003", GeneratedTraversalRuleOwner.EnvelopeSupport,
                    GeneratedTraversalRuleSeverity.Critical, Count("required.envelope_kinds", 7),
                    Array.Empty<TraversalMovementKind>(), allEnvelopes,
                    GeneratedTraversalRuleFailurePolicy.RejectProfileLock),
                Rule("TRV-COLLIDER-004", GeneratedTraversalRuleOwner.ColliderClearance,
                    GeneratedTraversalRuleSeverity.Critical,
                    Capability(profile, "collider.clearance_height_tiles"),
                    allMovements, new[] { CompiledTraversalEnvelopeSetKind.Clearance },
                    GeneratedTraversalRuleFailurePolicy.BlockRouteCriticalValidation),
                Rule("TRV-JUMP-005", GeneratedTraversalRuleOwner.JumpArcEnvelope,
                    GeneratedTraversalRuleSeverity.Error,
                    Capability(profile, "jump.vertical_rise_tiles"),
                    new[] { TraversalMovementKind.Jump },
                    new[] { CompiledTraversalEnvelopeSetKind.JumpArc,
                        CompiledTraversalEnvelopeSetKind.Landing },
                    GeneratedTraversalRuleFailurePolicy.BlockRouteCriticalValidation),
                Rule("TRV-DROP-006", GeneratedTraversalRuleOwner.DropRecovery,
                    GeneratedTraversalRuleSeverity.Critical, Count("drop.recovery_required", 1),
                    new[] { TraversalMovementKind.Drop },
                    new[] { CompiledTraversalEnvelopeSetKind.DropColumn,
                        CompiledTraversalEnvelopeSetKind.Landing,
                        CompiledTraversalEnvelopeSetKind.Recovery },
                    GeneratedTraversalRuleFailurePolicy.BlockRouteCriticalValidation),
                Rule("TRV-CLIMB-007", GeneratedTraversalRuleOwner.ClimbReach,
                    GeneratedTraversalRuleSeverity.Error,
                    Capability(profile, "climb.vertical_reach_tiles"),
                    new[] { TraversalMovementKind.Climb },
                    new[] { CompiledTraversalEnvelopeSetKind.Clearance,
                        CompiledTraversalEnvelopeSetKind.Landing,
                        CompiledTraversalEnvelopeSetKind.Recovery },
                    GeneratedTraversalRuleFailurePolicy.BlockRouteCriticalValidation),
                Rule("TRV-BOUNCE-008", GeneratedTraversalRuleOwner.BounceArc,
                    GeneratedTraversalRuleSeverity.Warning,
                    Capability(profile, "bounce.vertical_rise_tiles"),
                    new[] { TraversalMovementKind.Bounce },
                    new[] { CompiledTraversalEnvelopeSetKind.JumpArc,
                        CompiledTraversalEnvelopeSetKind.Landing,
                        CompiledTraversalEnvelopeSetKind.Recovery },
                    GeneratedTraversalRuleFailurePolicy.ReportWithoutRelaxation),
                Rule("TRV-FALL-009", GeneratedTraversalRuleOwner.FallLimit,
                    GeneratedTraversalRuleSeverity.Critical,
                    Capability(profile, "fall.safe_drop_tiles"),
                    new[] { TraversalMovementKind.Drop },
                    new[] { CompiledTraversalEnvelopeSetKind.DropColumn,
                        CompiledTraversalEnvelopeSetKind.Landing },
                    GeneratedTraversalRuleFailurePolicy.BlockRouteCriticalValidation),
                Rule("TRV-CHASE-010", GeneratedTraversalRuleOwner.ChaseWidth,
                    GeneratedTraversalRuleSeverity.Info,
                    Capability(profile, "chase_width.preferred_tiles"),
                    new[] { TraversalMovementKind.Walk,
                        TraversalMovementKind.Slide },
                    new[] { CompiledTraversalEnvelopeSetKind.Floor,
                        CompiledTraversalEnvelopeSetKind.Clearance },
                    GeneratedTraversalRuleFailurePolicy.ReportWithoutRelaxation),
                Rule("TRV-ROUTE-011", GeneratedTraversalRuleOwner.RouteCriticality,
                    GeneratedTraversalRuleSeverity.Critical, Count("required.matrix_entries", 6),
                    allMovements, allEnvelopes,
                    GeneratedTraversalRuleFailurePolicy.BlockRouteCriticalValidation),
                Rule("TRV-DIGEST-012", GeneratedTraversalRuleOwner.DigestCompatibility,
                    GeneratedTraversalRuleSeverity.Critical, Count("required.digest_match", 1),
                    allMovements, allEnvelopes,
                    GeneratedTraversalRuleFailurePolicy.RejectProfileLock),
            };

            return new GeneratedTraversalRuleRegistry(Version, profile.Digest, rules,
                CreateRequiredMatrix(), new[]
                {
                    ClusterTraversalProtectionSourceKind.RouteSpine,
                    ClusterTraversalProtectionSourceKind.TraversalEnvelope,
                });
        }

        public static IReadOnlyList<GeneratedTraversalMovementEnvelopeRequirement>
            CreateRequiredMatrix() => new ReadOnlyCollection<GeneratedTraversalMovementEnvelopeRequirement>(
                new[]
                {
                    Matrix(TraversalMovementKind.Walk,
                        CompiledTraversalEnvelopeSetKind.Floor,
                        CompiledTraversalEnvelopeSetKind.Clearance,
                        CompiledTraversalEnvelopeSetKind.Landing,
                        CompiledTraversalEnvelopeSetKind.Recovery),
                    Matrix(TraversalMovementKind.Jump,
                        CompiledTraversalEnvelopeSetKind.Clearance,
                        CompiledTraversalEnvelopeSetKind.JumpArc,
                        CompiledTraversalEnvelopeSetKind.Landing,
                        CompiledTraversalEnvelopeSetKind.Recovery),
                    Matrix(TraversalMovementKind.Drop,
                        CompiledTraversalEnvelopeSetKind.Clearance,
                        CompiledTraversalEnvelopeSetKind.DropColumn,
                        CompiledTraversalEnvelopeSetKind.Landing,
                        CompiledTraversalEnvelopeSetKind.Recovery),
                    Matrix(TraversalMovementKind.Climb,
                        CompiledTraversalEnvelopeSetKind.Clearance,
                        CompiledTraversalEnvelopeSetKind.Landing,
                        CompiledTraversalEnvelopeSetKind.Recovery),
                    Matrix(TraversalMovementKind.Slide,
                        CompiledTraversalEnvelopeSetKind.Floor,
                        CompiledTraversalEnvelopeSetKind.Clearance,
                        CompiledTraversalEnvelopeSetKind.Landing,
                        CompiledTraversalEnvelopeSetKind.Recovery),
                    Matrix(TraversalMovementKind.Bounce,
                        CompiledTraversalEnvelopeSetKind.Clearance,
                        CompiledTraversalEnvelopeSetKind.JumpArc,
                        CompiledTraversalEnvelopeSetKind.Landing,
                        CompiledTraversalEnvelopeSetKind.Recovery),
                });

        private static GeneratedTraversalValidationRule Rule(
            string id,
            GeneratedTraversalRuleOwner owner,
            GeneratedTraversalRuleSeverity severity,
            GeneratedTraversalRuleThreshold threshold,
            IEnumerable<TraversalMovementKind> movements,
            IEnumerable<CompiledTraversalEnvelopeSetKind> envelopes,
            GeneratedTraversalRuleFailurePolicy failurePolicy) =>
            new GeneratedTraversalValidationRule(id, owner, severity, threshold,
                movements, envelopes, failurePolicy, ConsumerTask);

        private static GeneratedTraversalRuleThreshold Capability(
            GeneratedTraversalProfile profile, string key)
        {
            var value = profile.Capabilities.Single(candidate =>
                string.Equals(candidate.Key, key, StringComparison.Ordinal));
            return new GeneratedTraversalRuleThreshold(
                value.Key, value.MinimumValue, value.MaximumValue, value.ExactValue);
        }

        private static GeneratedTraversalRuleThreshold Count(string key, int value) =>
            new GeneratedTraversalRuleThreshold(key, value, value,
                value.ToString(CultureInfo.InvariantCulture));

        private static GeneratedTraversalMovementEnvelopeRequirement Matrix(
            TraversalMovementKind movement,
            params CompiledTraversalEnvelopeSetKind[] envelopes) =>
            new GeneratedTraversalMovementEnvelopeRequirement(movement, envelopes);
    }
}
