using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.TerrainClusters;
using StarNight.Map.WorldGeneration.Validation;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Validation
{
    [TestFixture]
    [Category("MAP19_01")]
    public sealed class GeneratedTraversalProfileRuleRegistryTests
    {
        [Test]
        public void TraversalProfilePublishesVersionedCapabilityGroups()
        {
            var profile = GeneratedTraversalProfileCatalog.Create();

            Assert.That(profile.Version.IsValid, Is.True);
            Assert.That(profile.Version.Version, Is.EqualTo("1.0.0"));
            Assert.That(profile.Capabilities, Has.Count.EqualTo(18));
            CollectionAssert.AreEqual(
                Enum.GetValues(typeof(GeneratedTraversalCapabilityGroup))
                    .Cast<GeneratedTraversalCapabilityGroup>(),
                profile.Capabilities.Select(value => value.Group).Distinct().OrderBy(value => value));
            CollectionAssert.AreEqual(
                new[] { TraversalMovementKind.Walk, TraversalMovementKind.Jump,
                    TraversalMovementKind.Drop, TraversalMovementKind.Climb,
                    TraversalMovementKind.Slide, TraversalMovementKind.Bounce },
                profile.MovementKinds);
            CollectionAssert.AreEqual(
                new[] { CompiledTraversalEnvelopeSetKind.Centerline,
                    CompiledTraversalEnvelopeSetKind.Floor,
                    CompiledTraversalEnvelopeSetKind.Clearance,
                    CompiledTraversalEnvelopeSetKind.JumpArc,
                    CompiledTraversalEnvelopeSetKind.DropColumn,
                    CompiledTraversalEnvelopeSetKind.Landing,
                    CompiledTraversalEnvelopeSetKind.Recovery },
                profile.EnvelopeKinds);
            AssertLowerHex(profile.Digest);
        }

        [Test]
        public void TraversalProfileUsesCentralizedValuesWithoutProductionDuplication()
        {
            var profile = GeneratedTraversalProfileCatalog.Create();

            Assert.That(profile.CentralizedProductionValueOwnerCount, Is.EqualTo(1));
            Assert.That(profile.Capabilities.Select(value => value.SourceLabel).Distinct(),
                Is.EqualTo(new[] { GeneratedTraversalProfileCatalog.ValueSourceLabel }));
            Assert.That(profile.Capabilities.Select(value => value.Key).Distinct().Count(),
                Is.EqualTo(profile.Capabilities.Count));
            Assert.That(profile.Capabilities.All(value => value.HasValidThreshold), Is.True);
            Assert.That(profile.Capabilities.All(value =>
                value.ConsumerPhase == GeneratedTraversalProfileCatalog.ConsumerPhase), Is.True);
            Assert.That(profile.ForGroup(GeneratedTraversalCapabilityGroup.Collider), Has.Count.EqualTo(4));
            Assert.That(profile.ForGroup(GeneratedTraversalCapabilityGroup.Jump), Has.Count.EqualTo(3));
            Assert.That(profile.ForGroup(GeneratedTraversalCapabilityGroup.AirControl), Has.Count.EqualTo(2));
            Assert.That(profile.ForGroup(GeneratedTraversalCapabilityGroup.Climb), Has.Count.EqualTo(2));
            Assert.That(profile.ForGroup(GeneratedTraversalCapabilityGroup.Bounce), Has.Count.EqualTo(3));
            Assert.That(profile.ForGroup(GeneratedTraversalCapabilityGroup.Fall), Has.Count.EqualTo(2));
            Assert.That(profile.ForGroup(GeneratedTraversalCapabilityGroup.ChaseWidth), Has.Count.EqualTo(2));
        }

        [Test]
        public void TraversalRuleRegistryPublishesRequiredOwnersSeveritiesAndThresholds()
        {
            var profile = GeneratedTraversalProfileCatalog.Create();
            var registry = GeneratedTraversalRuleCatalog.Create(profile);

            Assert.That(registry.Rules, Has.Count.EqualTo(12));
            CollectionAssert.AreEqual(
                Enum.GetValues(typeof(GeneratedTraversalRuleOwner))
                    .Cast<GeneratedTraversalRuleOwner>(),
                registry.Rules.Select(value => value.Owner).OrderBy(value => value));
            Assert.That(registry.Rules.Count(value =>
                value.Severity == GeneratedTraversalRuleSeverity.Critical), Is.EqualTo(8));
            Assert.That(registry.Rules.Count(value =>
                value.Severity == GeneratedTraversalRuleSeverity.Error), Is.EqualTo(2));
            Assert.That(registry.Rules.Count(value =>
                value.Severity == GeneratedTraversalRuleSeverity.Warning), Is.EqualTo(1));
            Assert.That(registry.Rules.Count(value =>
                value.Severity == GeneratedTraversalRuleSeverity.Info), Is.EqualTo(1));
            Assert.That(registry.Rules.All(value => value.Threshold != null &&
                value.Threshold.IsValid), Is.True);
            Assert.That(registry.Rules.Where(value =>
                    value.Severity == GeneratedTraversalRuleSeverity.Critical)
                .All(value => value.FailurePolicy != GeneratedTraversalRuleFailurePolicy.None), Is.True);
            AssertLowerHex(registry.RegistryDigest);
        }

        [Test]
        public void MovementEnvelopeMatrixMatchesApprovedWalkJumpDropClimbSlideBounceContract()
        {
            var registry = GeneratedTraversalRuleCatalog.Create(
                GeneratedTraversalProfileCatalog.Create());
            var expected = GeneratedTraversalRuleCatalog.CreateRequiredMatrix();

            Assert.That(registry.MovementEnvelopeMatrix, Has.Count.EqualTo(6));
            CollectionAssert.AreEqual(expected.Select(value => value.StableToken),
                registry.MovementEnvelopeMatrix.Select(value => value.StableToken));
            CollectionAssert.AreEqual(new[]
                {
                    ClusterTraversalProtectionSourceKind.RouteSpine,
                    ClusterTraversalProtectionSourceKind.TraversalEnvelope,
                }, registry.ProtectionSourceKinds);
            AssertLowerHex(registry.MovementEnvelopeMatrixDigest);
        }

        [Test]
        public void TraversalProfileRuleLockPreservesMap18ApprovedAuditAndIncomingHandoffDigests()
        {
            var result = GeneratedTraversalProfileRuleLock.Lock(
                GeneratedTraversalProfileRuleLock.CreateAcceptedRequest());

            Assert.That(result.Success, Is.True, Describe(result));
            Assert.That(result.Surface.Map18ApprovedAuditDigest,
                Is.EqualTo(GeneratedTraversalProfileRuleLock.Map18ApprovedAuditDigest));
            Assert.That(result.Surface.Map19_01IncomingHandoffDigest,
                Is.EqualTo(GeneratedTraversalProfileRuleLock.Map19_01IncomingHandoffDigest));
            AssertLowerHex(result.Surface.TraversalProfileDigest);
            AssertLowerHex(result.Surface.RuleRegistryDigest);
            AssertLowerHex(result.Surface.MovementEnvelopeMatrixDigest);
            AssertLowerHex(result.Map19_02HandoffDigest);
            TestContext.WriteLine(
                "MAP19_01_DIGEST_EVIDENCE profile={0} registry={1} matrix={2} handoff={3}",
                result.Surface.TraversalProfileDigest,
                result.Surface.RuleRegistryDigest,
                result.Surface.MovementEnvelopeMatrixDigest,
                result.Map19_02HandoffDigest);
        }

        [Test]
        public void TraversalProfileRuleDigestsAreStableAcrossRepeatReverseCultureAndRuleOrder()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var profile = GeneratedTraversalProfileCatalog.Create();
                var repeatedProfile = GeneratedTraversalProfileCatalog.Create();
                var reversedProfile = new GeneratedTraversalProfile(profile.Version,
                    profile.Capabilities.Reverse(), profile.MovementKinds.Reverse(),
                    profile.EnvelopeKinds.Reverse());
                var registry = GeneratedTraversalRuleCatalog.Create(profile);
                var reversedRegistry = new GeneratedTraversalRuleRegistry(
                    registry.Version, reversedProfile.Digest, registry.Rules.Reverse(),
                    registry.MovementEnvelopeMatrix.Reverse(),
                    registry.ProtectionSourceKinds.Reverse());

                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var cultureProfile = GeneratedTraversalProfileCatalog.Create();
                var cultureRegistry = GeneratedTraversalRuleCatalog.Create(cultureProfile);

                Assert.That(repeatedProfile.Digest, Is.EqualTo(profile.Digest));
                Assert.That(reversedProfile.Digest, Is.EqualTo(profile.Digest));
                Assert.That(cultureProfile.Digest, Is.EqualTo(profile.Digest));
                Assert.That(reversedRegistry.RegistryDigest, Is.EqualTo(registry.RegistryDigest));
                Assert.That(cultureRegistry.RegistryDigest, Is.EqualTo(registry.RegistryDigest));
                Assert.That(reversedRegistry.MovementEnvelopeMatrixDigest,
                    Is.EqualTo(registry.MovementEnvelopeMatrixDigest));

                var mutatedCapability = CopyCapability(profile.Capabilities[0],
                    profile.Capabilities[0].MinimumValue + 0.01m,
                    profile.Capabilities[0].MaximumValue + 0.01m,
                    "MUTATED");
                var mutatedCapabilities = profile.Capabilities.Skip(1)
                    .Concat(new[] { mutatedCapability });
                var mutatedProfile = new GeneratedTraversalProfile(profile.Version,
                    mutatedCapabilities, profile.MovementKinds, profile.EnvelopeKinds);

                var mutatedRule = CopyRule(registry.Rules[0],
                    new GeneratedTraversalRuleThreshold("mutation.probe", 1m, 2m, "1..2"));
                var mutatedRegistry = new GeneratedTraversalRuleRegistry(
                    registry.Version, registry.ProfileCompatibilityDigest,
                    registry.Rules.Skip(1).Concat(new[] { mutatedRule }),
                    registry.MovementEnvelopeMatrix, registry.ProtectionSourceKinds);
                var mutatedMatrix = new[]
                {
                    new GeneratedTraversalMovementEnvelopeRequirement(
                        TraversalMovementKind.Walk,
                        new[] { CompiledTraversalEnvelopeSetKind.Floor })
                }.Concat(registry.MovementEnvelopeMatrix.Where(value =>
                    value.MovementKind != TraversalMovementKind.Walk));
                var matrixRegistry = new GeneratedTraversalRuleRegistry(
                    registry.Version, registry.ProfileCompatibilityDigest, registry.Rules,
                    mutatedMatrix, registry.ProtectionSourceKinds);

                Assert.That(mutatedProfile.Digest, Is.Not.EqualTo(profile.Digest));
                Assert.That(mutatedRegistry.RegistryDigest, Is.Not.EqualTo(registry.RegistryDigest));
                Assert.That(matrixRegistry.MovementEnvelopeMatrixDigest,
                    Is.Not.EqualTo(registry.MovementEnvelopeMatrixDigest));
                TestContext.WriteLine(
                    "MAP19_01_MUTATION_EVIDENCE profile=1/1 registry=1/1 matrix=1/1");
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Test]
        public void TraversalProfileRuleFailuresAreAtomicAndReportOwnerReasonExpectedActual()
        {
            var accepted = GeneratedTraversalProfileRuleLock.CreateAcceptedRequest();
            var request = new GeneratedTraversalProfileRuleLockRequest(
                string.Empty, new string('0', 64), accepted.Profile, accepted.Registry,
                new GeneratedTraversalForbiddenWork(
                    playerControllerChanges: 1,
                    playerPhysicsChanges: 1,
                    tileGraphNodeCount: 1,
                    tileGraphEdgeCount: 1,
                    bfsRunCount: 1,
                    completionSearchCount: 1,
                    seedBatchRunCount: 1,
                    runtimeObjectSpawnCount: 1));

            var first = GeneratedTraversalProfileRuleLock.Lock(request);
            var second = GeneratedTraversalProfileRuleLock.Lock(request);

            Assert.That(first.Success, Is.False);
            Assert.That(first.Surface, Is.Null);
            Assert.That(first.Map19_02HandoffDigest, Is.Empty);
            Assert.That(first.Failures, Is.Not.Empty);
            Assert.That(first.Failures.All(value => !string.IsNullOrEmpty(value.Owner) &&
                !string.IsNullOrEmpty(value.Reason) && !string.IsNullOrEmpty(value.OffendingKey) &&
                !string.IsNullOrEmpty(value.ExpectedValue) && !string.IsNullOrEmpty(value.ActualValue)), Is.True);
            CollectionAssert.AreEqual(first.Failures.Select(value => value.StableToken),
                second.Failures.Select(value => value.StableToken));
            var reasons = first.Failures.Select(value => value.Reason).ToArray();
            Assert.That(reasons, Does.Contain("PLAYER_CONTROLLER_CHANGE_ATTEMPTED"));
            Assert.That(reasons, Does.Contain("PLAYER_PHYSICS_CHANGE_ATTEMPTED"));
            Assert.That(reasons, Does.Contain("TILE_GRAPH_NODE_WORK_ATTEMPTED"));
            Assert.That(reasons, Does.Contain("TILE_GRAPH_EDGE_WORK_ATTEMPTED"));
            Assert.That(reasons, Does.Contain("BFS_WORK_ATTEMPTED"));
            Assert.That(reasons, Does.Contain("COMPLETION_SEARCH_ATTEMPTED"));
            Assert.That(reasons, Does.Contain("SEED_RUNNER_WORK_ATTEMPTED"));
            Assert.That(reasons, Does.Contain("RUNTIME_OBJECT_WORK_ATTEMPTED"));
            TestContext.WriteLine("MAP19_01_FORBIDDEN_WORK_PROBES count={0}",
                reasons.Count(value => value.EndsWith("ATTEMPTED", StringComparison.Ordinal)));
        }

        [Test]
        public void TraversalProfileRuleLockRejectsDuplicateMissingInvalidMovementRuleAndThresholds()
        {
            var profile = GeneratedTraversalProfileCatalog.Create();
            var capabilities = profile.Capabilities
                .Where(value => value.Group != GeneratedTraversalCapabilityGroup.ChaseWidth)
                .Concat(new[] { profile.Capabilities[0] }).ToArray();
            var movements = profile.MovementKinds
                .Where(value => value != TraversalMovementKind.Walk)
                .Concat(new[] { (TraversalMovementKind)999 });
            var envelopes = profile.EnvelopeKinds
                .Where(value => value != CompiledTraversalEnvelopeSetKind.Recovery)
                .Concat(new[] { (CompiledTraversalEnvelopeSetKind)999 });
            var invalidProfile = new GeneratedTraversalProfile(profile.Version,
                capabilities, movements, envelopes);

            var registry = GeneratedTraversalRuleCatalog.Create(profile);
            var invalidRule = new GeneratedTraversalValidationRule(
                "TRV-INVALID-999", GeneratedTraversalRuleOwner.JumpArcEnvelope,
                (GeneratedTraversalRuleSeverity)999,
                new GeneratedTraversalRuleThreshold("invalid.range", 2m, 1m, "2..1"),
                new[] { (TraversalMovementKind)999 },
                new[] { CompiledTraversalEnvelopeSetKind.JumpArc },
                GeneratedTraversalRuleFailurePolicy.BlockRouteCriticalValidation,
                GeneratedTraversalRuleCatalog.ConsumerTask);
            var invalidMatrix = new[]
            {
                new GeneratedTraversalMovementEnvelopeRequirement(
                    TraversalMovementKind.Walk,
                    new[] { CompiledTraversalEnvelopeSetKind.Floor })
            }.Concat(registry.MovementEnvelopeMatrix.Where(value =>
                value.MovementKind != TraversalMovementKind.Walk));
            var invalidRegistry = new GeneratedTraversalRuleRegistry(
                registry.Version, invalidProfile.Digest,
                registry.Rules.Concat(new[] { registry.Rules[0], invalidRule }),
                invalidMatrix, registry.ProtectionSourceKinds);
            var request = new GeneratedTraversalProfileRuleLockRequest(
                GeneratedTraversalProfileRuleLock.Map18ApprovedAuditDigest,
                GeneratedTraversalProfileRuleLock.Map19_01IncomingHandoffDigest,
                invalidProfile, invalidRegistry, new GeneratedTraversalForbiddenWork());

            var result = GeneratedTraversalProfileRuleLock.Lock(request);
            var missingProfileResult = GeneratedTraversalProfileRuleLock.Lock(
                new GeneratedTraversalProfileRuleLockRequest(
                    GeneratedTraversalProfileRuleLock.Map18ApprovedAuditDigest,
                    GeneratedTraversalProfileRuleLock.Map19_01IncomingHandoffDigest,
                    null, registry, new GeneratedTraversalForbiddenWork()));
            var digestProfile = new GeneratedTraversalProfile(
                profile.Version, profile.Capabilities, profile.MovementKinds,
                profile.EnvelopeKinds, new string('0', 64));
            var digestRegistry = new GeneratedTraversalRuleRegistry(
                registry.Version, digestProfile.Digest, registry.Rules,
                registry.MovementEnvelopeMatrix, registry.ProtectionSourceKinds,
                new string('0', 64), new string('0', 64));
            var digestResult = GeneratedTraversalProfileRuleLock.Lock(
                new GeneratedTraversalProfileRuleLockRequest(
                    GeneratedTraversalProfileRuleLock.Map18ApprovedAuditDigest,
                    GeneratedTraversalProfileRuleLock.Map19_01IncomingHandoffDigest,
                    digestProfile, digestRegistry, new GeneratedTraversalForbiddenWork()));
            var reasons = result.Failures
                .Concat(missingProfileResult.Failures)
                .Concat(digestResult.Failures)
                .Select(value => value.Reason).ToArray();

            Assert.That(result.Success, Is.False);
            Assert.That(result.Surface, Is.Null);
            Assert.That(missingProfileResult.Success, Is.False);
            Assert.That(missingProfileResult.Surface, Is.Null);
            Assert.That(digestResult.Success, Is.False);
            Assert.That(digestResult.Surface, Is.Null);
            Assert.That(reasons, Does.Contain("MISSING_PROFILE"));
            Assert.That(reasons, Does.Contain("DUPLICATE_CAPABILITY_KEY"));
            Assert.That(reasons, Does.Contain("MISSING_CAPABILITY_GROUP"));
            Assert.That(reasons, Does.Contain("MISSING_MOVEMENT_KIND"));
            Assert.That(reasons, Does.Contain("UNSUPPORTED_MOVEMENT_KIND"));
            Assert.That(reasons, Does.Contain("MISSING_ENVELOPE_KIND"));
            Assert.That(reasons, Does.Contain("UNSUPPORTED_ENVELOPE_KIND"));
            Assert.That(reasons, Does.Contain("DUPLICATE_RULE_ID"));
            Assert.That(reasons, Does.Contain("INVALID_SEVERITY"));
            Assert.That(reasons, Does.Contain("INVALID_THRESHOLD_RANGE"));
            Assert.That(reasons, Does.Contain("MOVEMENT_ENVELOPE_MATRIX_MISMATCH"));
            Assert.That(reasons, Does.Contain("PROFILE_DIGEST_MISMATCH"));
            Assert.That(reasons, Does.Contain("MATRIX_DIGEST_MISMATCH"));
            Assert.That(reasons, Does.Contain("REGISTRY_DIGEST_MISMATCH"));
            Assert.That(result.Map19_02HandoffDigest, Is.Empty);
            Assert.That(missingProfileResult.Map19_02HandoffDigest, Is.Empty);
            Assert.That(digestResult.Map19_02HandoffDigest, Is.Empty);
            TestContext.WriteLine(
                "MAP19_01_FAILURE_PROBES missing_profile={0} missing_group={1} " +
                "missing_movement={2} missing_envelope={3} duplicate_capability={4} " +
                "duplicate_rule={5} invalid_severity={6} invalid_threshold={7} " +
                "matrix_mismatch={8} profile_digest_mismatch={9} " +
                "matrix_digest_mismatch={10} registry_digest_mismatch={11}",
                CountReason(reasons, "MISSING_PROFILE"),
                CountReason(reasons, "MISSING_CAPABILITY_GROUP"),
                CountReason(reasons, "MISSING_MOVEMENT_KIND"),
                CountReason(reasons, "MISSING_ENVELOPE_KIND"),
                CountReason(reasons, "DUPLICATE_CAPABILITY_KEY"),
                CountReason(reasons, "DUPLICATE_RULE_ID"),
                CountReason(reasons, "INVALID_SEVERITY"),
                CountReason(reasons, "INVALID_THRESHOLD_RANGE"),
                CountReason(reasons, "MOVEMENT_ENVELOPE_MATRIX_MISMATCH"),
                CountReason(reasons, "PROFILE_DIGEST_MISMATCH"),
                CountReason(reasons, "MATRIX_DIGEST_MISMATCH"),
                CountReason(reasons, "REGISTRY_DIGEST_MISMATCH"));
        }

        [Test]
        public void TraversalProfileRuleLockDoesNotBuildGraphsRunBfsMutatePhysicsOrRunRegressions()
        {
            var result = GeneratedTraversalProfileRuleLock.Lock(
                GeneratedTraversalProfileRuleLock.CreateAcceptedRequest());

            Assert.That(result.Success, Is.True, Describe(result));
            Assert.That(result.Surface.PlayerControllerBehaviorChangeCount, Is.Zero);
            Assert.That(result.Surface.RigidbodyBehaviorChangeCount, Is.Zero);
            Assert.That(result.Surface.ColliderBehaviorChangeCount, Is.Zero);
            Assert.That(result.Surface.Physics2DBehaviorChangeCount, Is.Zero);
            Assert.That(result.Surface.TileGraphNodeCount, Is.Zero);
            Assert.That(result.Surface.TileGraphEdgeCount, Is.Zero);
            Assert.That(result.Surface.BfsRunCount, Is.Zero);
            Assert.That(result.Surface.CompletionSearchCount, Is.Zero);
            Assert.That(result.Surface.SeedBatchRunCount, Is.Zero);
            Assert.That(result.Surface.RuntimeObjectSpawnCount, Is.Zero);
        }

        [Test]
        public void Map19HandoffKeepsMap19_02Locked()
        {
            var result = GeneratedTraversalProfileRuleLock.Lock(
                GeneratedTraversalProfileRuleLock.CreateAcceptedRequest());

            Assert.That(result.Success, Is.True, Describe(result));
            Assert.That(result.Surface.Map19_02Locked, Is.True);
            Assert.That(result.Surface.Map19_02Started, Is.False);
            AssertLowerHex(result.Surface.Map19_02HandoffDigest);
        }

        private static GeneratedTraversalCapabilityValue CopyCapability(
            GeneratedTraversalCapabilityValue source,
            decimal minimum,
            decimal maximum,
            string exactValue) => new GeneratedTraversalCapabilityValue(
                source.Key, source.Group, source.ValueKind, source.Unit,
                source.SourceLabel, source.ThresholdPolicy, minimum, maximum,
                exactValue, source.ConsumerPhase);

        private static GeneratedTraversalValidationRule CopyRule(
            GeneratedTraversalValidationRule source,
            GeneratedTraversalRuleThreshold threshold) => new GeneratedTraversalValidationRule(
                source.RuleId, source.Owner, source.Severity, threshold,
                source.TargetMovementKinds, source.TargetEnvelopeKinds,
                source.FailurePolicy, source.ConsumerTask);

        private static void AssertLowerHex(string value)
        {
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(value), Is.True, value);
        }

        private static int CountReason(IEnumerable<string> reasons, string reason) =>
            reasons.Count(value => string.Equals(value, reason, StringComparison.Ordinal));

        private static string Describe(GeneratedTraversalProfileRuleLockResult result) =>
            string.Join(Environment.NewLine, result.Failures.Select(value => value.StableToken));
    }
}
