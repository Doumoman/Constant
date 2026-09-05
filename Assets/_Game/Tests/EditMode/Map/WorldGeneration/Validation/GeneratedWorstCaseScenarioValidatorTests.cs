using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.TerrainClusters;
using StarNight.Map.WorldGeneration.Validation;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Validation
{
    [TestFixture]
    [Category("MAP19_06")]
    public sealed class GeneratedWorstCaseScenarioValidatorTests
    {
        private static UpstreamRun cachedUpstream;

        [Test]
        public void WorstCaseValidatorPassesZeroToolVillageSkippedHostileAndEvacuatedScenarios()
        {
            var run = Run();
            AssertSuccess(run.Result);
            Assert.That(run.Result.Surface.ScenarioKindsChecked, Is.EqualTo(7));
            Assert.That(run.Result.Surface.ScenarioInstancesChecked, Is.EqualTo(7));
            Assert.That(run.Result.Surface.ScenarioInstancesPassed, Is.EqualTo(7));
            Assert.That(run.Result.Surface.CompletionSearches, Is.EqualTo(7));
            Assert.That(run.Result.Surface.CompletionGoalsSatisfied, Is.EqualTo(7));
            Assert.That(run.Result.Surface.Passed(GeneratedWorstCaseScenarioKind.ZeroTool),
                Is.EqualTo(1));
            Assert.That(run.Result.Surface.Passed(
                GeneratedWorstCaseScenarioKind.VillageSkipped), Is.EqualTo(1));
            Assert.That(run.Result.Surface.Passed(
                GeneratedWorstCaseScenarioKind.VillageHostile), Is.EqualTo(1));
            Assert.That(run.Result.Surface.Passed(
                GeneratedWorstCaseScenarioKind.VillageEvacuated), Is.EqualTo(1));
        }

        [Test]
        public void WorstCaseValidatorModelsDestructibleTileLossAsLogicalOverlayOnly()
        {
            var cells = Cells();
            var before = cells.Select(value => value.Target.StableToken + "|" +
                value.Payload.StableToken).ToArray();
            var run = Run(cells: cells);
            AssertSuccess(run.Result);
            Assert.That(run.Result.Surface.DestructibleCandidatesChecked, Is.EqualTo(2));
            Assert.That(run.Result.Surface.LogicalOverlaysApplied, Is.EqualTo(2));
            Assert.That(run.Result.Surface.DestructibleOverlayCompletionsPassed,
                Is.EqualTo(2));
            Assert.That(cells.Select(value => value.Target.StableToken + "|" +
                value.Payload.StableToken), Is.EqualTo(before));
        }

        [Test]
        public void WorstCaseValidatorModelsMovingDeviceWorstPositionAsTransitionFilterOnly()
        {
            var transitions = Transitions();
            var before = transitions.Select(value => value.StableToken).ToArray();
            var run = Run(transitions: transitions);
            AssertSuccess(run.Result);
            Assert.That(run.Result.Surface.DeviceCandidatesChecked, Is.EqualTo(1));
            Assert.That(run.Result.Surface.WorstPositionTransforms, Is.EqualTo(2));
            Assert.That(run.Result.Surface.FallbackCompletionPasses, Is.EqualTo(2));
            Assert.That(run.Result.Surface.FallbackViolations, Is.Zero);
            Assert.That(transitions.Select(value => value.StableToken), Is.EqualTo(before));
        }

        [Test]
        public void WorstCaseValidatorPreservesProtectedCriticalRoutesLandingsAndSockets()
        {
            var result = Run().Result;
            AssertSuccess(result);
            Assert.That(result.Surface.ProtectedCriticalRejects, Is.EqualTo(1));
            Assert.That(result.Surface.Proofs.SelectMany(value => value.LogicalOverlayIds),
                Does.Not.Contain("PROTECTED_CRITICAL_CELL"));
            Assert.That(Cells().Single(value => value.IsProtectedCritical).ProtectedSources,
                Is.EqualTo(GeneratedWorstCaseProtectedCriticalSource.MandatoryRoute |
                    GeneratedWorstCaseProtectedCriticalSource.TraversalEnvelope |
                    GeneratedWorstCaseProtectedCriticalSource.RequiredLanding |
                    GeneratedWorstCaseProtectedCriticalSource.RecoveryFloor |
                    GeneratedWorstCaseProtectedCriticalSource.BoundarySocket));
        }

        [Test]
        public void WorstCaseValidatorConsumesMap19_02ToMap19_05SurfacesReadOnly()
        {
            var upstream = Upstream();
            var before = UpstreamDigests(upstream);
            var run = Run();
            AssertSuccess(run.Result);
            Assert.That(ReferenceEquals(run.Input.Graph, upstream.RemovalInput.Graph), Is.True);
            Assert.That(ReferenceEquals(run.Input.NakedSearch,
                upstream.RemovalInput.NakedSearch), Is.True);
            Assert.That(ReferenceEquals(run.Input.CompletionSearch,
                upstream.RemovalInput.CompletionSearch), Is.True);
            Assert.That(ReferenceEquals(run.Input.ClusterValidation,
                upstream.RemovalInput.ClusterValidation), Is.True);
            Assert.That(ReferenceEquals(run.Input.RepetitionValidation,
                upstream.Result), Is.True);
            Assert.That(UpstreamDigests(upstream), Is.EqualTo(before));
        }

        [Test]
        public void WorstCaseValidatorRejectsMissingHandoffDigestMismatchAndMissingBindings()
        {
            var missing = Run(omitRepetition: true).Result;
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.Failures.Select(value => value.Reason), Does.Contain(
                "MISSING_MAP19_05_HANDOFF"));

            var mismatch = Run(declaredMap19_05: new string('d', 64),
                declaredHandoff: new string('e', 64)).Result;
            Assert.That(mismatch.Failures.Select(value => value.Reason), Does.Contain(
                "DECLARED_MAP19_05_DIGEST_MISMATCH"));
            Assert.That(mismatch.Failures.Select(value => value.Reason), Does.Contain(
                "INCOMING_HANDOFF_DIGEST_MISMATCH"));

            var missingBinding = Run(catalog: new GeneratedWorstCaseScenarioCatalog(
                Catalog().Scenarios.Take(6))).Result;
            Assert.That(missingBinding.Failures.Select(value => value.Reason), Does.Contain(
                "MISSING_SCENARIO_BINDING"));
        }

        [Test]
        public void WorstCaseValidatorFailuresAreAtomicAndReportOwnerReasonExpectedActual()
        {
            var zeroTool = Run(transitions: RetagRequired(
                GeneratedWorstCaseTransitionRole.OptionalTool)).Result;
            AssertAtomic(zeroTool,
                "ZERO_TOOL_COMPLETION_FAILURE");
            Assert.That(zeroTool.Failures.Select(value => value.Reason), Does.Contain(
                "COMBINED_ADVERSE_COMPLETION_FAILURE"));
            AssertAtomic(Run(transitions: RetagRequired(
                GeneratedWorstCaseTransitionRole.OptionalShop)).Result,
                "VILLAGE_SKIPPED_COMPLETION_FAILURE");
            AssertAtomic(Run(transitions: RetagRequired(
                GeneratedWorstCaseTransitionRole.OptionalVillageHostile)).Result,
                "HOSTILE_VILLAGE_DEPENDENCY_FAILURE");
            AssertAtomic(Run(transitions: RetagRequired(
                GeneratedWorstCaseTransitionRole.OptionalVillageEvacuated)).Result,
                "EVACUATED_VILLAGE_DEPENDENCY_FAILURE");

            var protectedCatalog = Catalog("PROTECTED_CRITICAL_CELL");
            AssertAtomic(Run(catalog: protectedCatalog).Result,
                "PROTECTED_CRITICAL_DESTRUCTIBLE_OVERLAY");

            var device = Devices().Single();
            var missingFallback = new GeneratedWorstCaseDeviceCandidate(device.DeviceId,
                device.Target, device.WorstStatePayload, device.SourceOwner,
                device.SourceDigest, true, true, device.TransitionIds,
                Array.Empty<string>());
            AssertAtomic(Run(devices: new[] { missingFallback }).Result,
                "DEVICE_FALLBACK_MISSING");
            AssertAtomic(Run(actionAudit: new GeneratedWorstCaseScenarioActionAudit(
                runtimeQueryAttempts: 1)).Result, "FORBIDDEN_RUNTIME_MUTATION");
            AssertAtomic(Run(actionAudit: new GeneratedWorstCaseScenarioActionAudit(
                map19_07Started: true)).Result, "MAP19_07_START_ATTEMPT");
        }

        [Test]
        public void WorstCaseValidatorDigestIsStableAcrossRepeatReverseCultureAndScenarioOrder()
        {
            var first = Run();
            var repeat = Run();
            var reverse = Run(catalog: new GeneratedWorstCaseScenarioCatalog(
                    Catalog().Scenarios.Reverse()),
                transitions: Transitions().Reverse(), cells: Cells().Reverse(),
                devices: Devices().Reverse());
            AssertSuccess(first.Result);
            AssertSuccess(repeat.Result);
            AssertSuccess(reverse.Result);
            Assert.That(repeat.Result.CombinedSuccessDigest,
                Is.EqualTo(first.Result.CombinedSuccessDigest));
            Assert.That(reverse.Result.CombinedSuccessDigest,
                Is.EqualTo(first.Result.CombinedSuccessDigest));

            var priorCulture = CultureInfo.CurrentCulture;
            var priorUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                Assert.That(Run().Result.CombinedSuccessDigest,
                    Is.EqualTo(first.Result.CombinedSuccessDigest));
            }
            finally
            {
                CultureInfo.CurrentCulture = priorCulture;
                CultureInfo.CurrentUICulture = priorUiCulture;
            }

            var mutatedCells = Cells("MUTATED_DESTRUCTIBLE_SOURCE");
            var mutation = Run(cells: mutatedCells);
            AssertSuccess(mutation.Result);
            Assert.That(mutation.Result.WorstCaseInputDigest,
                Is.Not.EqualTo(first.Result.WorstCaseInputDigest));
            Assert.That(mutation.Result.ScenarioProofDigest,
                Is.Not.EqualTo(first.Result.ScenarioProofDigest));
            Assert.That(mutation.Result.Map19_07HandoffDigest,
                Is.Not.EqualTo(first.Result.Map19_07HandoffDigest));
        }

        [Test]
        public void WorstCaseValidatorPublishesMap19_07HandoffSurface()
        {
            var result = Run().Result;
            AssertSuccess(result);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                result.WorstCaseInputDigest), Is.True);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                result.ScenarioCatalogDigest), Is.True);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                result.ScenarioProofDigest), Is.True);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                result.DestructibleDeviceBoundaryDigest), Is.True);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                result.CombinedSuccessDigest), Is.True);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                result.Map19_07HandoffDigest), Is.True);
        }

        [Test]
        public void Map19HandoffKeepsMap19_07Locked()
        {
            var run = Run();
            AssertSuccess(run.Result);
            Assert.That(run.Result.Surface.Map19_07Started, Is.False);
            Assert.That(run.Input.ActionAudit.Map19_07Started, Is.False);
            WriteEvidence(run.Result);
        }

        private static ValidationRun Run(
            GeneratedWorstCaseScenarioCatalog catalog = null,
            IEnumerable<GeneratedWorstCaseTransitionBinding> transitions = null,
            IEnumerable<GeneratedWorstCaseLogicalCellCandidate> cells = null,
            IEnumerable<GeneratedWorstCaseDeviceCandidate> devices = null,
            GeneratedWorstCaseScenarioActionAudit actionAudit = null,
            string declaredMap19_05 = null,
            string declaredHandoff = null,
            bool omitRepetition = false)
        {
            var upstream = Upstream();
            var input = new GeneratedWorstCaseScenarioInput(upstream.RemovalInput.Graph,
                upstream.RemovalInput.NakedSearch, upstream.RemovalInput.CompletionSearch,
                upstream.RemovalInput.ClusterValidation,
                omitRepetition ? null : upstream.Result, Village(), catalog ?? Catalog(),
                transitions ?? Transitions(), upstream.RemovalInput.Goal,
                upstream.RemovalInput.AllowedMovementKinds, cells ?? Cells(),
                devices ?? Devices(), actionAudit, declaredMap19_05CombinedDigest:
                    declaredMap19_05, declaredIncomingHandoffDigest: declaredHandoff);
            return new ValidationRun(input,
                GeneratedWorstCaseScenarioValidator.Validate(input));
        }

        private static GeneratedWorstCaseScenarioCatalog Catalog(
            string destructiveOverlayId = "NONCRITICAL_DESTRUCTIBLE_CELL")
        {
            var activity = new[] { GeneratedWorstCaseTransitionRole.OptionalActivityEvent };
            var village = activity.Concat(new[]
            {
                GeneratedWorstCaseTransitionRole.OptionalShop,
                GeneratedWorstCaseTransitionRole.OptionalFacility,
                GeneratedWorstCaseTransitionRole.OptionalNpc,
            }).ToArray();
            return new GeneratedWorstCaseScenarioCatalog(new[]
            {
                Scenario("ZERO_TOOL", GeneratedWorstCaseScenarioKind.ZeroTool,
                    activity.Concat(new[] { GeneratedWorstCaseTransitionRole.OptionalTool })),
                Scenario("VILLAGE_SKIPPED", GeneratedWorstCaseScenarioKind.VillageSkipped,
                    village.Concat(new[] { GeneratedWorstCaseTransitionRole.OptionalVillageHostile,
                        GeneratedWorstCaseTransitionRole.OptionalVillageEvacuated })),
                Scenario("VILLAGE_HOSTILE", GeneratedWorstCaseScenarioKind.VillageHostile,
                    village.Concat(new[] { GeneratedWorstCaseTransitionRole.OptionalVillageHostile })),
                Scenario("VILLAGE_EVACUATED", GeneratedWorstCaseScenarioKind.VillageEvacuated,
                    village.Concat(new[] { GeneratedWorstCaseTransitionRole.OptionalVillageEvacuated })),
                Scenario("DESTRUCTIBLE_TILE_LOSS",
                    GeneratedWorstCaseScenarioKind.DestructibleTileLoss, activity,
                    overlays: new[] { destructiveOverlayId }),
                Scenario("MOVING_DEVICE_WORST_POSITION",
                    GeneratedWorstCaseScenarioKind.MovingDeviceWorstPosition, activity,
                    devices: new[] { "MOVING_DEVICE_A" }),
                Scenario("COMBINED_ADVERSE_STATIC_SHELL",
                    GeneratedWorstCaseScenarioKind.CombinedAdverseStaticShell,
                    Enum.GetValues(typeof(GeneratedWorstCaseTransitionRole))
                        .Cast<GeneratedWorstCaseTransitionRole>()
                        .Where(value => value != GeneratedWorstCaseTransitionRole.StaticRequired),
                    new[] { destructiveOverlayId }, new[] { "MOVING_DEVICE_A" }),
            });
        }

        private static GeneratedWorstCaseScenarioTransform Scenario(string id,
            GeneratedWorstCaseScenarioKind kind,
            IEnumerable<GeneratedWorstCaseTransitionRole> roles,
            IEnumerable<string> overlays = null, IEnumerable<string> devices = null) =>
            new GeneratedWorstCaseScenarioTransform(id, kind, "MAP19_06",
                Hash("SCENARIO_" + id), roles, overlays, devices, 0);

        private static GeneratedWorstCaseTransitionBinding[] Transitions()
        {
            var upstream = Upstream();
            var values = upstream.RemovalInput.Transitions.Select(value =>
                new GeneratedWorstCaseTransitionBinding(value.Transition,
                    value.IsRemoved ? GeneratedWorstCaseTransitionRole.OptionalActivityEvent :
                        GeneratedWorstCaseTransitionRole.StaticRequired,
                    value.SourceOwner, value.SourceDigest)).ToList();
            var node = upstream.RemovalInput.NakedSearch.Surface.StartNodeId;
            values.AddRange(new[]
            {
                Optional(node, "OPTIONAL_TOOL", GeneratedWorstCaseTransitionRole.OptionalTool),
                Optional(node, "OPTIONAL_SHOP", GeneratedWorstCaseTransitionRole.OptionalShop),
                Optional(node, "OPTIONAL_FACILITY", GeneratedWorstCaseTransitionRole.OptionalFacility),
                Optional(node, "OPTIONAL_NPC", GeneratedWorstCaseTransitionRole.OptionalNpc),
                Optional(node, "OPTIONAL_HOSTILE", GeneratedWorstCaseTransitionRole.OptionalVillageHostile),
                Optional(node, "OPTIONAL_EVACUATED", GeneratedWorstCaseTransitionRole.OptionalVillageEvacuated),
                Optional(node, "MOVING_DEVICE_TRANSITION", GeneratedWorstCaseTransitionRole.MovingDevice),
            });
            return values.OrderBy(value => value).ToArray();
        }

        private static GeneratedWorstCaseTransitionBinding Optional(string node, string id,
            GeneratedWorstCaseTransitionRole role)
        {
            var digest = Hash(id);
            return new GeneratedWorstCaseTransitionBinding(
                new GeneratedCompletionStateTransition(node,
                    GeneratedCompletionTransitionKind.CollectMandatoryResource,
                    GeneratedCompletionBindingSourceKind.SpecialRegionState, id, digest, 8),
                role, role == GeneratedWorstCaseTransitionRole.OptionalShop ? "MAP13_SHOP" :
                    role == GeneratedWorstCaseTransitionRole.MovingDevice ? "MAP17_DEVICE" :
                    "MAP18_VILLAGE_STATE", digest);
        }

        private static GeneratedWorstCaseTransitionBinding[] RetagRequired(
            GeneratedWorstCaseTransitionRole role)
        {
            var transitions = Transitions().ToList();
            var required = transitions.Last(value => value.Role ==
                GeneratedWorstCaseTransitionRole.StaticRequired);
            transitions[transitions.IndexOf(required)] = new GeneratedWorstCaseTransitionBinding(
                required.Transition, role, required.SourceOwner, required.SourceDigest);
            return transitions.ToArray();
        }

        private static GeneratedWorstCaseLogicalCellCandidate[] Cells(
            string sourceToken = "DESTRUCTIBLE_SOURCE")
        {
            var protection = GeneratedWorstCaseProtectedCriticalSource.MandatoryRoute |
                GeneratedWorstCaseProtectedCriticalSource.TraversalEnvelope |
                GeneratedWorstCaseProtectedCriticalSource.RequiredLanding |
                GeneratedWorstCaseProtectedCriticalSource.RecoveryFloor |
                GeneratedWorstCaseProtectedCriticalSource.BoundarySocket;
            return new[]
            {
                Cell("NONCRITICAL_DESTRUCTIBLE_CELL", 1, sourceToken,
                    GeneratedWorstCaseProtectedCriticalSource.None),
                Cell("PROTECTED_CRITICAL_CELL", 2, "PROTECTED_SOURCE", protection),
            };
        }

        private static GeneratedWorstCaseLogicalCellCandidate Cell(string id, int index,
            string source, GeneratedWorstCaseProtectedCriticalSource protection)
        {
            var target = Target(index, source);
            return new GeneratedWorstCaseLogicalCellCandidate(id, target,
                GeneratedSectorModificationKind.DestroyTile,
                GeneratedSectorModificationPayload.Destroy("SOLID", source),
                "MAP17_SECTOR_MODIFICATION", Hash(source), protection);
        }

        private static GeneratedWorstCaseDeviceCandidate[] Devices()
        {
            var transitions = Transitions();
            var fallback = transitions.First(value => value.Role ==
                GeneratedWorstCaseTransitionRole.StaticRequired).Transition.TransitionId;
            var deviceTransition = transitions.Single(value => value.Role ==
                GeneratedWorstCaseTransitionRole.MovingDevice).Transition.TransitionId;
            return new[]
            {
                new GeneratedWorstCaseDeviceCandidate("MOVING_DEVICE_A",
                    Target(3, "DEVICE_SOURCE"),
                    GeneratedSectorModificationPayload.DeviceState("POSITION", "DISABLED"),
                    "MAP17_DEVICE_STATE", Hash("DEVICE_SOURCE"), true, true,
                    new[] { deviceTransition }, new[] { fallback }),
            };
        }

        private static GeneratedSectorModificationTarget Target(int index, string source) =>
            new GeneratedSectorModificationTarget(new GeneratedSectorCoordinate(0, 0),
                new GeneratedSectorLocalCellIndex(index),
                (int)GeneratedTilemapLayerId.Terrain, source, "SLOT_" + index);

        private static VillageStateMarkerSetDefinition Village() =>
            new VillageStateMarkerSetDefinition(
                new[] { new VillageNpcMarkerDefinition("NPC_A", "FACILITY_A") },
                new[] { new VillageInventoryMarkerDefinition("SHOP_A", "FACILITY_A") },
                new[] { new VillageDoorMarkerDefinition("DOOR_A", "FACILITY_A",
                    new LocalTileCoord(0, 0)) }, "NPC_A");

        private static UpstreamRun Upstream()
        {
            if (cachedUpstream != null) return cachedUpstream;
            var method = typeof(GeneratedRepetitionEventRemovalValidatorTests).GetMethod(
                "Run", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            var raw = method.Invoke(null, new object[]
                { null, null, null, null, null, null, null, false });
            cachedUpstream = new UpstreamRun(
                Property<GeneratedRepetitionValidationInput>(raw, "RepetitionInput"),
                Property<GeneratedEventRemovalValidationInput>(raw, "RemovalInput"),
                Property<GeneratedRepetitionValidationResult>(raw, "Result"));
            Assert.That(cachedUpstream.Result.Success, Is.True,
                string.Join("\n", cachedUpstream.Result.Failures));
            return cachedUpstream;
        }

        private static T Property<T>(object source, string name) => (T)source.GetType()
            .GetProperty(name, BindingFlags.Instance | BindingFlags.Public).GetValue(source);

        private static string[] UpstreamDigests(UpstreamRun source) => new[]
        {
            source.RemovalInput.Graph.GraphDigest,
            source.RemovalInput.NakedSearch.SuccessProofDigest,
            source.RemovalInput.CompletionSearch.SuccessProofDigest,
            source.RemovalInput.ClusterValidation.CombinedSuccessDigest,
            source.Result.CombinedSuccessDigest, source.Result.Map19_06HandoffDigest,
        };

        private static void AssertSuccess(GeneratedWorstCaseScenarioResult result)
        {
            Assert.That(result.Success, Is.True, string.Join("\n", result.Failures));
            Assert.That(result.Surface, Is.Not.Null);
            Assert.That(result.Failures, Is.Empty);
        }

        private static void AssertAtomic(GeneratedWorstCaseScenarioResult result,
            string reason)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Surface, Is.Null);
            Assert.That(result.WorstCaseInputDigest, Is.Empty);
            Assert.That(result.ScenarioProofDigest, Is.Empty);
            Assert.That(result.CombinedSuccessDigest, Is.Empty);
            Assert.That(result.Map19_07HandoffDigest, Is.Empty);
            Assert.That(result.Failures.Select(value => value.Reason), Does.Contain(reason));
            Assert.That(result.Failures.All(value =>
                !string.IsNullOrEmpty(value.Owner) &&
                !string.IsNullOrEmpty(value.Reason) &&
                !string.IsNullOrEmpty(value.ScenarioId) &&
                !string.IsNullOrEmpty(value.CaseId) &&
                !string.IsNullOrEmpty(value.OffendingKey) &&
                !string.IsNullOrEmpty(value.Expected) &&
                !string.IsNullOrEmpty(value.Actual) &&
                !string.IsNullOrEmpty(value.SourceDigest) &&
                !string.IsNullOrEmpty(value.FrontierOrOverlayEvidence)), Is.True);
        }

        private static void WriteEvidence(GeneratedWorstCaseScenarioResult result)
        {
            var surface = result.Surface;
            TestContext.Out.WriteLine("SCENARIO_KINDS_CHECKED=" +
                surface.ScenarioKindsChecked);
            TestContext.Out.WriteLine("SCENARIO_INSTANCES_CHECKED=" +
                surface.ScenarioInstancesChecked);
            foreach (var kind in Enum.GetValues(typeof(GeneratedWorstCaseScenarioKind))
                .Cast<GeneratedWorstCaseScenarioKind>())
                TestContext.Out.WriteLine("SCENARIO_" + kind.ToString().ToUpperInvariant() +
                    "_PASSED=" + surface.Passed(kind));
            TestContext.Out.WriteLine("COMPLETION_SEARCHES=" + surface.CompletionSearches);
            TestContext.Out.WriteLine("COMPLETION_GOALS_SATISFIED=" +
                surface.CompletionGoalsSatisfied);
            TestContext.Out.WriteLine("SHORTEST_TRANSITION_MIN=" +
                surface.ShortestTransitionMinimum);
            TestContext.Out.WriteLine("SHORTEST_TRANSITION_MAX=" +
                surface.ShortestTransitionMaximum);
            TestContext.Out.WriteLine("DESTRUCTIBLE_CANDIDATES_CHECKED=" +
                surface.DestructibleCandidatesChecked);
            TestContext.Out.WriteLine("PROTECTED_CRITICAL_REJECTS=" +
                surface.ProtectedCriticalRejects);
            TestContext.Out.WriteLine("LOGICAL_OVERLAYS_APPLIED=" +
                surface.LogicalOverlaysApplied);
            TestContext.Out.WriteLine("DEVICE_CANDIDATES_CHECKED=" +
                surface.DeviceCandidatesChecked);
            TestContext.Out.WriteLine("WORST_POSITION_TRANSFORMS=" +
                surface.WorstPositionTransforms);
            TestContext.Out.WriteLine("FALLBACK_COMPLETION_PASSES=" +
                surface.FallbackCompletionPasses);
            TestContext.Out.WriteLine("FALLBACK_VIOLATIONS=" +
                surface.FallbackViolations);
            TestContext.Out.WriteLine("WORST_CASE_INPUT_DIGEST=" +
                surface.InputDigest);
            TestContext.Out.WriteLine("SCENARIO_CATALOG_DIGEST=" +
                surface.CatalogDigest);
            TestContext.Out.WriteLine("SCENARIO_PROOF_DIGEST=" +
                surface.ScenarioProofDigest);
            TestContext.Out.WriteLine("DESTRUCTIBLE_DEVICE_BOUNDARY_DIGEST=" +
                surface.DestructibleDeviceBoundaryDigest);
            TestContext.Out.WriteLine("COMBINED_DIGEST=" + surface.CombinedDigest);
            TestContext.Out.WriteLine("MAP19_07_HANDOFF_DIGEST=" +
                surface.Map19_07HandoffDigest);
        }

        private static string Hash(string value) =>
            BakingCanonicalDigest.HashCanonicalLines(new[] { value });

        private sealed class ValidationRun
        {
            public ValidationRun(GeneratedWorstCaseScenarioInput input,
                GeneratedWorstCaseScenarioResult result)
            {
                Input = input;
                Result = result;
            }
            public GeneratedWorstCaseScenarioInput Input { get; }
            public GeneratedWorstCaseScenarioResult Result { get; }
        }

        private sealed class UpstreamRun
        {
            public UpstreamRun(GeneratedRepetitionValidationInput repetitionInput,
                GeneratedEventRemovalValidationInput removalInput,
                GeneratedRepetitionValidationResult result)
            {
                RepetitionInput = repetitionInput;
                RemovalInput = removalInput;
                Result = result;
            }
            public GeneratedRepetitionValidationInput RepetitionInput { get; }
            public GeneratedEventRemovalValidationInput RemovalInput { get; }
            public GeneratedRepetitionValidationResult Result { get; }
        }
    }
}
