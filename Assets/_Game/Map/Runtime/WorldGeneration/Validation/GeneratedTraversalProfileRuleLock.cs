using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.WorldGeneration.Validation
{
    public sealed class GeneratedTraversalProfileRuleFailure :
        IComparable<GeneratedTraversalProfileRuleFailure>
    {
        public GeneratedTraversalProfileRuleFailure(
            string owner,
            string reason,
            string offendingKey,
            string expectedValue,
            string actualValue)
        {
            Owner = Normalize(owner);
            Reason = Normalize(reason);
            OffendingKey = Normalize(offendingKey);
            ExpectedValue = Normalize(expectedValue);
            ActualValue = Normalize(actualValue);
        }

        public string Owner { get; }
        public string Reason { get; }
        public string OffendingKey { get; }
        public string ExpectedValue { get; }
        public string ActualValue { get; }
        public string StableToken => string.Join("|", new[]
        {
            "FAILURE", Owner, Reason, OffendingKey, ExpectedValue, ActualValue,
        });

        public int CompareTo(GeneratedTraversalProfileRuleFailure other) => other == null
            ? -1
            : string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? "MISSING"
            : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
        public override string ToString() => StableToken;
    }

    public sealed class GeneratedTraversalForbiddenWork
    {
        public GeneratedTraversalForbiddenWork(
            int playerControllerChanges = 0,
            int playerPhysicsChanges = 0,
            int tileGraphNodeCount = 0,
            int tileGraphEdgeCount = 0,
            int bfsRunCount = 0,
            int completionSearchCount = 0,
            int seedBatchRunCount = 0,
            int runtimeObjectSpawnCount = 0,
            bool map19_02Started = false)
        {
            PlayerControllerChanges = playerControllerChanges;
            PlayerPhysicsChanges = playerPhysicsChanges;
            TileGraphNodeCount = tileGraphNodeCount;
            TileGraphEdgeCount = tileGraphEdgeCount;
            BfsRunCount = bfsRunCount;
            CompletionSearchCount = completionSearchCount;
            SeedBatchRunCount = seedBatchRunCount;
            RuntimeObjectSpawnCount = runtimeObjectSpawnCount;
            Map19_02Started = map19_02Started;
        }

        public int PlayerControllerChanges { get; }
        public int PlayerPhysicsChanges { get; }
        public int TileGraphNodeCount { get; }
        public int TileGraphEdgeCount { get; }
        public int BfsRunCount { get; }
        public int CompletionSearchCount { get; }
        public int SeedBatchRunCount { get; }
        public int RuntimeObjectSpawnCount { get; }
        public bool Map19_02Started { get; }
    }

    public sealed class GeneratedTraversalProfileRuleLockRequest
    {
        public GeneratedTraversalProfileRuleLockRequest(
            string map18ApprovedAuditDigest,
            string map19_01IncomingHandoffDigest,
            GeneratedTraversalProfile profile,
            GeneratedTraversalRuleRegistry registry,
            GeneratedTraversalForbiddenWork forbiddenWork)
        {
            Map18ApprovedAuditDigest = map18ApprovedAuditDigest ?? string.Empty;
            Map19_01IncomingHandoffDigest = map19_01IncomingHandoffDigest ?? string.Empty;
            Profile = profile;
            Registry = registry;
            ForbiddenWork = forbiddenWork;
        }

        public string Map18ApprovedAuditDigest { get; }
        public string Map19_01IncomingHandoffDigest { get; }
        public GeneratedTraversalProfile Profile { get; }
        public GeneratedTraversalRuleRegistry Registry { get; }
        public GeneratedTraversalForbiddenWork ForbiddenWork { get; }
    }

    public sealed class GeneratedTraversalProfileRuleLockSurface
    {
        internal GeneratedTraversalProfileRuleLockSurface(
            string map18ApprovedAuditDigest,
            string map19_01IncomingHandoffDigest,
            GeneratedTraversalProfile profile,
            GeneratedTraversalRuleRegistry registry)
        {
            Map18ApprovedAuditDigest = map18ApprovedAuditDigest;
            Map19_01IncomingHandoffDigest = map19_01IncomingHandoffDigest;
            Profile = profile;
            RuleRegistry = registry;
            TraversalProfileDigest = profile.Digest;
            RuleRegistryDigest = registry.RegistryDigest;
            MovementEnvelopeMatrixDigest = registry.MovementEnvelopeMatrixDigest;
            Map19_02HandoffDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_02_TRAVERSAL_PROFILE_RULE_HANDOFF_V1",
                Map18ApprovedAuditDigest,
                Map19_01IncomingHandoffDigest,
                TraversalProfileDigest,
                RuleRegistryDigest,
                MovementEnvelopeMatrixDigest,
                string.Join(",", registry.ProtectionSourceKinds.Select(value =>
                    ((int)value).ToString(CultureInfo.InvariantCulture))),
                "MAP19_02_LOCKED=1|STARTED=0",
            });
        }

        public string Map18ApprovedAuditDigest { get; }
        public string Map19_01IncomingHandoffDigest { get; }
        public GeneratedTraversalProfile Profile { get; }
        public GeneratedTraversalRuleRegistry RuleRegistry { get; }
        public string TraversalProfileDigest { get; }
        public string RuleRegistryDigest { get; }
        public string MovementEnvelopeMatrixDigest { get; }
        public string Map19_02HandoffDigest { get; }
        public bool Map19_02Locked => true;
        public bool Map19_02Started => false;

        public int PlayerControllerBehaviorChangeCount => 0;
        public int RigidbodyBehaviorChangeCount => 0;
        public int ColliderBehaviorChangeCount => 0;
        public int Physics2DBehaviorChangeCount => 0;
        public int TileGraphNodeCount => 0;
        public int TileGraphEdgeCount => 0;
        public int BfsRunCount => 0;
        public int CompletionSearchCount => 0;
        public int SeedBatchRunCount => 0;
        public int RuntimeObjectSpawnCount => 0;
    }

    public sealed class GeneratedTraversalProfileRuleLockResult
    {
        private readonly ReadOnlyCollection<GeneratedTraversalProfileRuleFailure> failures;

        internal GeneratedTraversalProfileRuleLockResult(
            GeneratedTraversalProfileRuleLockSurface surface,
            IEnumerable<GeneratedTraversalProfileRuleFailure> sourceFailures)
        {
            Surface = surface;
            failures = new ReadOnlyCollection<GeneratedTraversalProfileRuleFailure>(
                (sourceFailures ?? Array.Empty<GeneratedTraversalProfileRuleFailure>())
                    .OrderBy(value => value).ToArray());
        }

        public bool Success => Surface != null && failures.Count == 0;
        public GeneratedTraversalProfileRuleLockSurface Surface { get; }
        public IReadOnlyList<GeneratedTraversalProfileRuleFailure> Failures => failures;
        public string Map19_02HandoffDigest => Success
            ? Surface.Map19_02HandoffDigest
            : string.Empty;
    }

    public static class GeneratedTraversalProfileRuleLock
    {
        public const string Map18ApprovedAuditDigest =
            "d17fc7aa674e42bbe17b576032d4f03298ecdf53e890dd1ae2352c68078ae6a7";
        public const string Map19_01IncomingHandoffDigest =
            "4fdec72ed7065e10f2a368285af7c86a54bd48afebae6d9b335af18e3788a6d2";

        private static readonly GeneratedTraversalCapabilityGroup[] RequiredGroups =
            Enum.GetValues(typeof(GeneratedTraversalCapabilityGroup))
                .Cast<GeneratedTraversalCapabilityGroup>().ToArray();
        private static readonly TraversalMovementKind[] RequiredMovements =
            Enum.GetValues(typeof(TraversalMovementKind))
                .Cast<TraversalMovementKind>().ToArray();
        private static readonly CompiledTraversalEnvelopeSetKind[] RequiredEnvelopes =
            Enum.GetValues(typeof(CompiledTraversalEnvelopeSetKind))
                .Cast<CompiledTraversalEnvelopeSetKind>().ToArray();
        private static readonly GeneratedTraversalRuleOwner[] RequiredOwners =
            Enum.GetValues(typeof(GeneratedTraversalRuleOwner))
                .Cast<GeneratedTraversalRuleOwner>().ToArray();
        private static readonly ClusterTraversalProtectionSourceKind[] RequiredProtectionSources =
        {
            ClusterTraversalProtectionSourceKind.RouteSpine,
            ClusterTraversalProtectionSourceKind.TraversalEnvelope,
        };
        private static readonly string[] RequiredCapabilityKeys =
        {
            "collider.footprint_width_tiles",
            "collider.footprint_height_tiles",
            "collider.clearance_width_tiles",
            "collider.clearance_height_tiles",
            "jump.horizontal_range_tiles",
            "jump.vertical_rise_tiles",
            "jump.minimum_landing_width_tiles",
            "air_control.enabled",
            "air_control.horizontal_correction_tiles",
            "climb.enabled",
            "climb.vertical_reach_tiles",
            "bounce.enabled",
            "bounce.horizontal_range_tiles",
            "bounce.vertical_rise_tiles",
            "fall.safe_drop_tiles",
            "fall.validation_limit_tiles",
            "chase_width.minimum_tiles",
            "chase_width.preferred_tiles",
        };

        public static GeneratedTraversalProfileRuleLockRequest CreateAcceptedRequest()
        {
            var profile = GeneratedTraversalProfileCatalog.Create();
            return new GeneratedTraversalProfileRuleLockRequest(
                Map18ApprovedAuditDigest, Map19_01IncomingHandoffDigest,
                profile, GeneratedTraversalRuleCatalog.Create(profile),
                new GeneratedTraversalForbiddenWork());
        }

        public static GeneratedTraversalProfileRuleLockResult Lock(
            GeneratedTraversalProfileRuleLockRequest request)
        {
            var failures = new List<GeneratedTraversalProfileRuleFailure>();
            if (request == null)
            {
                Add(failures, "ProfileCompleteness", "MISSING_REQUEST", "request",
                    "NON_NULL", "NULL");
                return Result(null, failures);
            }

            ValidateDigestValue(failures, "MAP18", "MAP18_APPROVED_AUDIT_DIGEST",
                request.Map18ApprovedAuditDigest, Map18ApprovedAuditDigest);
            ValidateDigestValue(failures, "MAP18", "MAP19_01_INCOMING_HANDOFF_DIGEST",
                request.Map19_01IncomingHandoffDigest, Map19_01IncomingHandoffDigest);
            ValidateProfile(request.Profile, failures);
            ValidateRegistry(request.Profile, request.Registry, failures);
            ValidateForbiddenWork(request.ForbiddenWork, failures);

            if (failures.Count != 0)
                return Result(null, failures);

            var surface = new GeneratedTraversalProfileRuleLockSurface(
                request.Map18ApprovedAuditDigest,
                request.Map19_01IncomingHandoffDigest,
                request.Profile,
                request.Registry);
            return Result(surface, failures);
        }

        private static void ValidateProfile(
            GeneratedTraversalProfile profile,
            ICollection<GeneratedTraversalProfileRuleFailure> failures)
        {
            if (profile == null)
            {
                Add(failures, "ProfileCompleteness", "MISSING_PROFILE", "profile",
                    "NON_NULL", "NULL");
                return;
            }

            if (profile.Version == null || !profile.Version.IsValid)
            {
                Add(failures, "ProfileCompleteness", "INVALID_PROFILE_VERSION", "version",
                    "NAMESPACE|VERSION|DATA|GENERATOR", profile.Version == null
                        ? "NULL" : profile.Version.StableToken);
            }

            foreach (var group in RequiredGroups)
            {
                if (!profile.Capabilities.Any(value => value.Group == group))
                    Add(failures, "ProfileCompleteness", "MISSING_CAPABILITY_GROUP", group.ToString(),
                        "PRESENT", "MISSING");
            }

            foreach (var key in RequiredCapabilityKeys)
            {
                if (!profile.Capabilities.Any(value => string.Equals(value.Key, key,
                    StringComparison.Ordinal)))
                    Add(failures, "ProfileCompleteness", "MISSING_CAPABILITY_VALUE", key,
                        "PRESENT", "MISSING");
            }

            foreach (var duplicate in profile.Capabilities.GroupBy(value => value.Key,
                         StringComparer.Ordinal).Where(group => group.Count() > 1))
            {
                Add(failures, "ProfileCompleteness", "DUPLICATE_CAPABILITY_KEY", duplicate.Key,
                    "1", duplicate.Count().ToString(CultureInfo.InvariantCulture));
            }

            foreach (var capability in profile.Capabilities.Where(value => !value.HasValidThreshold))
            {
                Add(failures, capability.Group.ToString(), "INVALID_CAPABILITY_THRESHOLD",
                    capability.Key, "VALID_RANGE_OR_EXACT", capability.StableToken);
            }

            ValidateEnumSet(profile.MovementKinds, RequiredMovements, "MovementKindSupport",
                "MOVEMENT_KIND", failures);
            ValidateEnumSet(profile.EnvelopeKinds, RequiredEnvelopes, "EnvelopeSupport",
                "ENVELOPE_KIND", failures);

            var computed = profile.ComputeDigest();
            if (!BakingCanonicalDigest.IsLowerHexSha256(profile.Digest) ||
                !string.Equals(profile.Digest, computed, StringComparison.Ordinal))
            {
                Add(failures, "DigestCompatibility", "PROFILE_DIGEST_MISMATCH", "profile.digest",
                    computed, profile.Digest);
            }
        }

        private static void ValidateRegistry(
            GeneratedTraversalProfile profile,
            GeneratedTraversalRuleRegistry registry,
            ICollection<GeneratedTraversalProfileRuleFailure> failures)
        {
            if (registry == null)
            {
                Add(failures, "DigestCompatibility", "MISSING_RULE_REGISTRY", "registry",
                    "NON_NULL", "NULL");
                return;
            }

            if (!string.Equals(registry.Version, GeneratedTraversalRuleCatalog.Version,
                StringComparison.Ordinal))
                Add(failures, "DigestCompatibility", "INVALID_REGISTRY_VERSION", "registry.version",
                    GeneratedTraversalRuleCatalog.Version, registry.Version);

            if (profile != null && !string.Equals(registry.ProfileCompatibilityDigest,
                profile.Digest, StringComparison.Ordinal))
                Add(failures, "DigestCompatibility", "PROFILE_COMPATIBILITY_DIGEST_MISMATCH",
                    "registry.profile_digest", profile.Digest,
                    registry.ProfileCompatibilityDigest);

            if (registry.Rules.Count < RequiredOwners.Length)
                Add(failures, "ProfileCompleteness", "INSUFFICIENT_RULE_COUNT", "registry.rules",
                    RequiredOwners.Length.ToString(CultureInfo.InvariantCulture),
                    registry.Rules.Count.ToString(CultureInfo.InvariantCulture));

            foreach (var duplicate in registry.Rules.GroupBy(value => value.RuleId,
                         StringComparer.Ordinal).Where(group => group.Count() > 1))
                Add(failures, "ProfileCompleteness", "DUPLICATE_RULE_ID", duplicate.Key,
                    "1", duplicate.Count().ToString(CultureInfo.InvariantCulture));

            foreach (var owner in RequiredOwners)
            {
                if (!registry.Rules.Any(value => value.Owner == owner))
                    Add(failures, owner.ToString(), "MISSING_RULE_OWNER", owner.ToString(),
                        "PRESENT", "MISSING");
            }

            foreach (var rule in registry.Rules)
            {
                if (!Enum.IsDefined(typeof(GeneratedTraversalRuleOwner), rule.Owner))
                    Add(failures, "RuleRegistry", "INVALID_RULE_OWNER", rule.RuleId,
                        "DEFINED_OWNER", ((int)rule.Owner).ToString(CultureInfo.InvariantCulture));
                if (!Enum.IsDefined(typeof(GeneratedTraversalRuleSeverity), rule.Severity))
                    Add(failures, "RuleRegistry", "INVALID_SEVERITY", rule.RuleId,
                        "Critical|Error|Warning|Info",
                        ((int)rule.Severity).ToString(CultureInfo.InvariantCulture));
                if (rule.Threshold == null || !rule.Threshold.IsValid)
                    Add(failures, rule.Owner.ToString(), "INVALID_THRESHOLD_RANGE", rule.RuleId,
                        "MIN<=MAX_AND_EXPECTED", rule.Threshold == null
                            ? "NULL" : rule.Threshold.StableToken);
                if (rule.Severity == GeneratedTraversalRuleSeverity.Critical &&
                    rule.FailurePolicy == GeneratedTraversalRuleFailurePolicy.None)
                    Add(failures, rule.Owner.ToString(), "CRITICAL_WITHOUT_FAILURE_POLICY", rule.RuleId,
                        "NON_NONE", "None");
                if (!Enum.IsDefined(typeof(GeneratedTraversalRuleFailurePolicy), rule.FailurePolicy))
                    Add(failures, rule.Owner.ToString(), "INVALID_FAILURE_POLICY", rule.RuleId,
                        "DEFINED_POLICY",
                        ((int)rule.FailurePolicy).ToString(CultureInfo.InvariantCulture));
                foreach (var movement in rule.TargetMovementKinds.Where(value =>
                             !RequiredMovements.Contains(value)))
                    Add(failures, rule.Owner.ToString(), "UNSUPPORTED_MOVEMENT_KIND", rule.RuleId,
                        "Walk|Jump|Drop|Climb|Slide|Bounce", movement.ToString());
                foreach (var envelope in rule.TargetEnvelopeKinds.Where(value =>
                             !RequiredEnvelopes.Contains(value)))
                    Add(failures, rule.Owner.ToString(), "UNSUPPORTED_ENVELOPE_KIND", rule.RuleId,
                        "Centerline|Floor|Clearance|JumpArc|DropColumn|Landing|Recovery",
                        envelope.ToString());
            }

            ValidateMatrix(registry.MovementEnvelopeMatrix, failures);
            ValidateProtectionSources(registry.ProtectionSourceKinds, failures);

            var computedMatrix = registry.ComputeMatrixDigest();
            if (!BakingCanonicalDigest.IsLowerHexSha256(registry.MovementEnvelopeMatrixDigest) ||
                !string.Equals(registry.MovementEnvelopeMatrixDigest, computedMatrix,
                    StringComparison.Ordinal))
                Add(failures, "DigestCompatibility", "MATRIX_DIGEST_MISMATCH", "matrix.digest",
                    computedMatrix, registry.MovementEnvelopeMatrixDigest);

            var computedRegistry = registry.ComputeRegistryDigest();
            if (!BakingCanonicalDigest.IsLowerHexSha256(registry.RegistryDigest) ||
                !string.Equals(registry.RegistryDigest, computedRegistry, StringComparison.Ordinal))
                Add(failures, "DigestCompatibility", "REGISTRY_DIGEST_MISMATCH", "registry.digest",
                    computedRegistry, registry.RegistryDigest);
        }

        private static void ValidateMatrix(
            IReadOnlyList<GeneratedTraversalMovementEnvelopeRequirement> matrix,
            ICollection<GeneratedTraversalProfileRuleFailure> failures)
        {
            var required = GeneratedTraversalRuleCatalog.CreateRequiredMatrix();
            foreach (var entry in matrix.Where(value => !RequiredMovements.Contains(value.MovementKind)))
                Add(failures, "MovementKindSupport", "UNSUPPORTED_MOVEMENT_KIND",
                    "matrix." + entry.MovementKind, "DEFINED_MOVEMENT", entry.StableToken);

            foreach (var expected in required)
            {
                var actual = matrix.Where(value => value.MovementKind == expected.MovementKind).ToArray();
                if (actual.Length != 1)
                {
                    Add(failures, "RouteCriticality", "MOVEMENT_ENVELOPE_MATRIX_MISMATCH",
                        expected.MovementKind.ToString(), expected.StableToken,
                        actual.Length.ToString(CultureInfo.InvariantCulture) + " ENTRIES");
                    continue;
                }

                if (!actual[0].RequiredEnvelopeKinds.SequenceEqual(expected.RequiredEnvelopeKinds))
                    Add(failures, "RouteCriticality", "MOVEMENT_ENVELOPE_MATRIX_MISMATCH",
                        expected.MovementKind.ToString(), expected.StableToken,
                        actual[0].StableToken);
                foreach (var envelope in actual[0].RequiredEnvelopeKinds.Where(value =>
                             !RequiredEnvelopes.Contains(value)))
                    Add(failures, "EnvelopeSupport", "UNSUPPORTED_ENVELOPE_KIND",
                        expected.MovementKind.ToString(), "DEFINED_ENVELOPE", envelope.ToString());
            }
        }

        private static void ValidateProtectionSources(
            IReadOnlyList<ClusterTraversalProtectionSourceKind> actual,
            ICollection<GeneratedTraversalProfileRuleFailure> failures)
        {
            foreach (var expected in RequiredProtectionSources)
            {
                if (actual.Count(value => value == expected) != 1)
                    Add(failures, "EnvelopeSupport", "PROTECTION_SOURCE_MISMATCH",
                        expected.ToString(), "EXACTLY_ONCE",
                        actual.Count(value => value == expected).ToString(CultureInfo.InvariantCulture));
            }

            foreach (var unsupported in actual.Where(value => !RequiredProtectionSources.Contains(value)))
                Add(failures, "EnvelopeSupport", "UNSUPPORTED_PROTECTION_SOURCE",
                    unsupported.ToString(), "RouteSpine|TraversalEnvelope", unsupported.ToString());
        }

        private static void ValidateForbiddenWork(
            GeneratedTraversalForbiddenWork work,
            ICollection<GeneratedTraversalProfileRuleFailure> failures)
        {
            if (work == null)
            {
                Add(failures, "ProfileCompleteness", "MISSING_BOUNDARY_AUDIT", "forbidden_work",
                    "EXPLICIT_ZERO_COUNTERS", "NULL");
                return;
            }

            ValidateZero(failures, "PhysicsBoundary", "PLAYER_CONTROLLER_CHANGE_ATTEMPTED",
                "player_controller", work.PlayerControllerChanges);
            ValidateZero(failures, "PhysicsBoundary", "PLAYER_PHYSICS_CHANGE_ATTEMPTED",
                "player_physics", work.PlayerPhysicsChanges);
            ValidateZero(failures, "GraphBoundary", "TILE_GRAPH_NODE_WORK_ATTEMPTED",
                "tile_graph_nodes", work.TileGraphNodeCount);
            ValidateZero(failures, "GraphBoundary", "TILE_GRAPH_EDGE_WORK_ATTEMPTED",
                "tile_graph_edges", work.TileGraphEdgeCount);
            ValidateZero(failures, "GraphBoundary", "BFS_WORK_ATTEMPTED",
                "bfs_runs", work.BfsRunCount);
            ValidateZero(failures, "GraphBoundary", "COMPLETION_SEARCH_ATTEMPTED",
                "completion_searches", work.CompletionSearchCount);
            ValidateZero(failures, "SeedBoundary", "SEED_RUNNER_WORK_ATTEMPTED",
                "seed_batch_runs", work.SeedBatchRunCount);
            ValidateZero(failures, "RuntimeBoundary", "RUNTIME_OBJECT_WORK_ATTEMPTED",
                "runtime_objects", work.RuntimeObjectSpawnCount);
            if (work.Map19_02Started)
                Add(failures, "TaskBoundary", "MAP19_02_STARTED", "MAP19_02",
                    "LOCKED", "STARTED");
        }

        private static void ValidateDigestValue(
            ICollection<GeneratedTraversalProfileRuleFailure> failures,
            string owner,
            string reason,
            string actual,
            string expected)
        {
            if (!BakingCanonicalDigest.IsLowerHexSha256(actual) ||
                !string.Equals(actual, expected, StringComparison.Ordinal))
                Add(failures, owner, reason, reason.ToLowerInvariant(), expected,
                    string.IsNullOrEmpty(actual) ? "MISSING" : actual);
        }

        private static void ValidateEnumSet<T>(
            IReadOnlyList<T> actual,
            IReadOnlyList<T> required,
            string owner,
            string label,
            ICollection<GeneratedTraversalProfileRuleFailure> failures)
            where T : struct
        {
            foreach (var expected in required)
            {
                var count = actual.Count(value => EqualityComparer<T>.Default.Equals(value, expected));
                if (count == 0)
                    Add(failures, owner, "MISSING_" + label, expected.ToString(),
                        "PRESENT", "MISSING");
                else if (count > 1)
                    Add(failures, owner, "DUPLICATE_" + label, expected.ToString(),
                        "1", count.ToString(CultureInfo.InvariantCulture));
            }

            foreach (var unsupported in actual.Where(value => !required.Contains(value)))
                Add(failures, owner, "UNSUPPORTED_" + label, unsupported.ToString(),
                    "DEFINED_VALUE", Convert.ToInt32(unsupported, CultureInfo.InvariantCulture)
                        .ToString(CultureInfo.InvariantCulture));
        }

        private static void ValidateZero(
            ICollection<GeneratedTraversalProfileRuleFailure> failures,
            string owner,
            string reason,
            string key,
            int actual)
        {
            if (actual != 0)
                Add(failures, owner, reason, key, "0",
                    actual.ToString(CultureInfo.InvariantCulture));
        }

        private static GeneratedTraversalProfileRuleLockResult Result(
            GeneratedTraversalProfileRuleLockSurface surface,
            IEnumerable<GeneratedTraversalProfileRuleFailure> failures) =>
            new GeneratedTraversalProfileRuleLockResult(surface, failures);

        private static void Add(
            ICollection<GeneratedTraversalProfileRuleFailure> failures,
            string owner,
            string reason,
            string key,
            string expected,
            string actual) => failures.Add(new GeneratedTraversalProfileRuleFailure(
                owner, reason, key, expected, actual));
    }
}
