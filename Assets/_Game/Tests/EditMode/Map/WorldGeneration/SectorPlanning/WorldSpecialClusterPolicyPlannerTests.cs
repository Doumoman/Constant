using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.SectorPlanning
{
    [TestFixture]
    [Category("MAP15_03")]
    public sealed class WorldSpecialClusterPolicyPlannerTests
    {
        private ReferenceReservationFixture fixture;

        [SetUp]
        public void SetUp()
        {
            fixture = ReferenceReservationFixture.Create();
        }

        [Test]
        public void MultiSectorReservationPlanPublishesTransactionsClaimsAndDigests()
        {
            var result = fixture.Plan();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(result.Plan.ObservedWorldSectorCount, Is.EqualTo(169));
            Assert.That(result.Plan.ObservedInternalEdgeCount, Is.EqualTo(312));
            Assert.That(result.Plan.RequiredTransactionCount, Is.EqualTo(6));
            Assert.That(result.Plan.AcceptedTransactionCount, Is.EqualTo(6));
            Assert.That(result.Plan.MissingTransactionCount, Is.Zero);
            Assert.That(result.Plan.TwoSectorTransactionCount, Is.EqualTo(1));
            Assert.That(result.Plan.DeferredTransactionCount, Is.EqualTo(2));
            Assert.That(result.Plan.Claims.Count, Is.EqualTo(10));
            Assert.That(result.Plan.EdgeLocks.Count, Is.EqualTo(4));
            Assert.That(result.Plan.ClusterPolicies.Count, Is.EqualTo(4));
            Assert.That(result.Plan.InputDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(result.Plan.OutputDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(
                () => ((IList<WorldReservationClaim>)result.Plan.Claims).Add(result.Plan.Claims[0]),
                Throws.TypeOf<NotSupportedException>());

            TestContext.Progress.WriteLine("MAP15_03_INPUT_DIGEST=" + result.Plan.InputDigest);
            TestContext.Progress.WriteLine("MAP15_03_OUTPUT_DIGEST=" + result.Plan.OutputDigest);
            TestContext.Progress.WriteLine("MAP15_03_COUNTS=" + string.Join(",", new[]
            {
                result.Plan.Transactions.Count, result.Plan.Claims.Count, result.Plan.EdgeLocks.Count,
                result.Plan.ClusterPolicies.Count, result.Plan.Conflicts.Count,
            }));
        }

        [Test]
        public void TwoSectorVillageTransactionUsesAdjacentEdgeAndEntryReturnEvidence()
        {
            var result = fixture.Plan();
            var village = result.Plan.Transactions.Single(value =>
                value.AuthorityKind == SpecialRegionKind.Village);
            var edge = result.Plan.Request.IntersectorPlan.Edges.Single(value =>
                value.Id == village.EdgeIds.Single());

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(village.State, Is.EqualTo(WorldReservationTransactionState.Fixed));
            Assert.That(village.SpanKind, Is.EqualTo(WorldReservationSpanKind.TwoSector));
            Assert.That(village.SectorIds.Count, Is.EqualTo(2));
            Assert.That(village.SectorIds.Distinct().Count(), Is.EqualTo(2));
            Assert.That(edge.Id.MinSector, Is.EqualTo(village.SectorIds[0]));
            Assert.That(edge.Id.MaxSector, Is.EqualTo(village.SectorIds[1]));
            Assert.That(edge.Endpoints.Count, Is.EqualTo(2));
            Assert.That(edge.RouteSignature.Compatible, Is.True);
            Assert.That(village.RequiresEntryReturnEvidence, Is.True);
            Assert.That(village.EntryEvidenceId, Is.Not.Empty);
            Assert.That(village.ReturnEvidenceId, Is.Not.Empty);
            Assert.That(result.Plan.EdgeLocks.Count(value =>
                value.OwnerId == village.TransactionId && value.EdgeId == edge.Id), Is.EqualTo(1));
        }

        [Test]
        public void FixedSpecialReservationsBeatClusterAndQuietClaims()
        {
            var result = fixture.Plan();
            var rejectedCluster = result.Plan.ClusterPolicies.Single(value =>
                value.PolicyId == ReferenceReservationFixture.SpecialConflictClusterPolicyId);

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(rejectedCluster.Accepted, Is.False);
            Assert.That(result.Plan.Claims.Any(value =>
                value.SectorId == new WorldSectorId(14) &&
                value.OwnerKind == WorldReservationOwnerKind.FixedSpecial), Is.True);
            Assert.That(result.Plan.Claims.Any(value =>
                value.SectorId == new WorldSectorId(14) &&
                value.OwnerKind == WorldReservationOwnerKind.SectorContainedCluster), Is.False);
            Assert.That(result.Plan.Claims.Any(value =>
                value.SectorId == new WorldSectorId(2) &&
                value.OwnerKind == WorldReservationOwnerKind.QuietFiller), Is.False);
            Assert.That(result.Plan.Conflicts.Count, Is.EqualTo(2));
            Assert.That(result.Plan.Conflicts.All(value =>
                value.WinnerKind == WorldReservationOwnerKind.FixedSpecial), Is.True);
            Assert.That(result.Plan.Conflicts.Count(value =>
                value.ConflictType == WorldReservationConflictType.FixedSpecialOverlap), Is.Zero);
        }

        [Test]
        public void TerrainClustersAreSectorContainedByDefault()
        {
            var result = fixture.Plan();
            var contained = result.Plan.ClusterPolicies.Where(value =>
                value.SpanKind == WorldClusterSpanKind.SectorContained).ToArray();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(WorldSpecialClusterPolicyPlanner.DefaultClusterSpanKind,
                Is.EqualTo(WorldClusterSpanKind.SectorContained));
            Assert.That(contained.Length, Is.EqualTo(3));
            Assert.That(contained.All(value => value.SectorIds.Count == 1), Is.True);
            Assert.That(contained.All(value => !value.EdgeId.HasValue), Is.True);
            Assert.That(contained.Count(value => value.Accepted), Is.EqualTo(2));
            Assert.That(result.Plan.AcceptedClusterPolicyCount, Is.EqualTo(3));
            Assert.That(result.Plan.RejectedClusterPolicyCount, Is.EqualTo(1));
        }

        [Test]
        public void CrossSectorClusterRequiresExactAllowlistAndCompatibleEdge()
        {
            var result = fixture.Plan();
            var cross = result.Plan.ClusterPolicies.Single(value => value.IsCrossSector);
            var allowance = result.Plan.CrossSectorAllowances.Single();
            var edge = result.Plan.Request.IntersectorPlan.Edges.Single(value => value.Id == cross.EdgeId.Value);

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(cross.Accepted, Is.True);
            Assert.That(result.Plan.AcceptedCrossSectorClusterCount, Is.EqualTo(1));
            Assert.That(cross.ClusterId, Is.EqualTo(allowance.ClusterId));
            Assert.That(cross.VariantId, Is.EqualTo(allowance.VariantId));
            Assert.That(cross.EdgeId.Value, Is.EqualTo(allowance.EdgeId));
            Assert.That(cross.SpanReason, Is.EqualTo(allowance.SpanReason));
            Assert.That(edge.RouteSignature.Compatible, Is.True);
            Assert.That(edge.RouteSignature.MandatoryRoute, Is.False);
            Assert.That(edge.IsBoundary, Is.False);
            Assert.That(result.Plan.EdgeLocks.Count(value =>
                value.LockKind == WorldReservationLockKind.CrossSectorCluster &&
                value.EdgeId == edge.Id), Is.EqualTo(1));
        }

        [Test]
        public void ReservationConflictsReportWinnerLoserAndReasonDeterministically()
        {
            var first = fixture.Plan();
            var repeat = fixture.Plan();

            Assert.That(first.Success, Is.True, Join(first));
            Assert.That(first.Plan.Conflicts.Count, Is.EqualTo(2));
            Assert.That(first.Plan.Conflicts.All(value => value.WinnerId != string.Empty), Is.True);
            Assert.That(first.Plan.Conflicts.All(value => value.LoserId != string.Empty), Is.True);
            Assert.That(first.Plan.Conflicts.All(value => value.Reason != string.Empty), Is.True);
            Assert.That(first.Plan.Conflicts.Select(ConflictIdentity),
                Is.EqualTo(first.Plan.Conflicts.OrderBy(value => value).Select(ConflictIdentity)));
            Assert.That(first.Plan.Conflicts.Select(ConflictIdentity),
                Is.EqualTo(repeat.Plan.Conflicts.Select(ConflictIdentity)));
        }

        [Test]
        public void ReservationPolicyIsDeterministicAcrossRepeatReverseAndCulture()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var first = fixture.Plan();
                var repeat = fixture.Plan();
                var reversed = WorldSpecialClusterPolicyPlanner.Plan(fixture.Request(
                    fixture.Transactions.Reverse(),
                    fixture.ClusterPolicies.Reverse(),
                    fixture.Allowances.Reverse(),
                    fixture.QuietClaims.Reverse()));
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var culture = fixture.Plan();

                var results = new[] { first, repeat, reversed, culture };
                Assert.That(results.All(value => value.Success), Is.True,
                    string.Join(";", results.Select(Join)));
                Assert.That(results.Select(value => value.InputDigest).Distinct().Count(), Is.EqualTo(1));
                Assert.That(results.Select(value => value.OutputDigest).Distinct().Count(), Is.EqualTo(1));
                Assert.That(results.Select(value => string.Join(",", value.Plan.Claims.Select(claim => claim.ClaimId)))
                    .Distinct().Count(), Is.EqualTo(1));
                Assert.That(results.Select(value => string.Join(",", value.Plan.Conflicts.Select(ConflictIdentity)))
                    .Distinct().Count(), Is.EqualTo(1));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Test]
        public void InvalidReservationInputsFailAtomicallyWithoutPartialPlan()
        {
            var village = fixture.Transactions.Single(value =>
                value.AuthorityKind == SpecialRegionKind.Village);
            var duplicateSectorVillage = CopyTransaction(
                village,
                sectors: new[] { new WorldSectorId(2), new WorldSectorId(2) });
            var nonAdjacentVillage = CopyTransaction(
                village,
                sectors: new[] { new WorldSectorId(2), new WorldSectorId(4) });
            var missingReturnVillage = CopyTransaction(village, returnEvidenceId: string.Empty);
            var overlapping = new WorldSpecialReservationTransaction(
                "TX_SPECIAL_OVERLAP",
                "CORE_RESOURCE_OVERLAP",
                SpecialRegionKind.CoreResource,
                WorldReservationTransactionState.Fixed,
                WorldReservationSpanKind.SingleSector,
                new[] { new WorldSectorId(14) },
                Array.Empty<WorldIntersectorEdgeId>(),
                false,
                string.Empty,
                string.Empty,
                false,
                string.Empty,
                "MAP13_SPECIAL_REGION");
            var implicitCross = new WorldClusterContainmentPolicy(
                "POLICY_IMPLICIT_CROSS",
                new TerrainClusterId("TC_IMPLICIT_CROSS"),
                new SpineVariantId("SPINE_IMPLICIT"),
                WorldClusterSpanKind.SectorContained,
                new[] { new WorldSectorId(60), new WorldSectorId(61) },
                null,
                string.Empty,
                "MAP11_TERRAIN_CLUSTER");
            var protectedEdge = fixture.Edge(0, 1);
            var protectedPolicy = new WorldClusterContainmentPolicy(
                "POLICY_PROTECTED_CROSS",
                new TerrainClusterId("TC_PROTECTED"),
                new SpineVariantId("SPINE_PROTECTED"),
                WorldClusterSpanKind.CrossSectorAllowlisted,
                new[] { new WorldSectorId(0), new WorldSectorId(1) },
                protectedEdge,
                "PROTECTED_EDGE_TEST",
                "MAP11_TERRAIN_CLUSTER");
            var protectedAllowance = new WorldClusterCrossSectorAllowance(
                "ALLOW_PROTECTED_CROSS",
                protectedPolicy.ClusterId,
                protectedPolicy.VariantId,
                protectedEdge,
                WorldReservationOwnerKind.CrossSectorCluster,
                WorldClusterSpanKind.CrossSectorAllowlisted,
                protectedPolicy.SpanReason,
                "MAP11_TERRAIN_CLUSTER");

            var results = new[]
            {
                WorldSpecialClusterPolicyPlanner.Plan(null),
                PlanWithTransaction(fixture, village, duplicateSectorVillage),
                PlanWithTransaction(fixture, village, nonAdjacentVillage),
                PlanWithTransaction(fixture, village, missingReturnVillage),
                WorldSpecialClusterPolicyPlanner.Plan(fixture.Request(
                    fixture.Transactions.Concat(new[] { overlapping }), fixture.ClusterPolicies,
                    fixture.Allowances, fixture.QuietClaims)),
                WorldSpecialClusterPolicyPlanner.Plan(fixture.Request(
                    fixture.Transactions, fixture.ClusterPolicies.Concat(new[] { implicitCross }),
                    fixture.Allowances, fixture.QuietClaims)),
                WorldSpecialClusterPolicyPlanner.Plan(fixture.Request(
                    fixture.Transactions, fixture.ClusterPolicies, Array.Empty<WorldClusterCrossSectorAllowance>(),
                    fixture.QuietClaims)),
                WorldSpecialClusterPolicyPlanner.Plan(fixture.Request(
                    fixture.Transactions, new[] { protectedPolicy }, new[] { protectedAllowance },
                    fixture.QuietClaims)),
                WorldSpecialClusterPolicyPlanner.Plan(fixture.Request(
                    fixture.Transactions, fixture.ClusterPolicies, fixture.Allowances, fixture.QuietClaims,
                    map13AuthorityDigest: "INVALID")),
                WorldSpecialClusterPolicyPlanner.Plan(fixture.Request(
                    fixture.Transactions, fixture.ClusterPolicies, fixture.Allowances, fixture.QuietClaims,
                    tilemapMutationCount: 1)),
            };

            Assert.That(results.All(value => !value.Success), Is.True);
            Assert.That(results.All(value => value.Plan == null), Is.True);
            Assert.That(results.All(value => value.InputDigest == string.Empty), Is.True);
            Assert.That(results.All(value => value.OutputDigest == string.Empty), Is.True);
            Assert.That(results.All(value => value.Failures.Count != 0), Is.True);
            Assert.That(results.SelectMany(value => value.Failures).Select(value => value.Code),
                Does.Contain(WorldReservationPolicyFailureCode.DuplicateTransactionSector));
            Assert.That(results.SelectMany(value => value.Failures).Select(value => value.Code),
                Does.Contain(WorldReservationPolicyFailureCode.MissingCrossSectorAllowance));
            Assert.That(results.SelectMany(value => value.Failures).Select(value => value.Code),
                Does.Contain(WorldReservationPolicyFailureCode.ProtectedEdgeConflict));
            Assert.That(results.SelectMany(value => value.Failures).Select(value => value.Code),
                Does.Contain(WorldReservationPolicyFailureCode.MutationClaim));
        }

        [Test]
        public void WorldReservationPolicyDoesNotMutateWorldPlanEdgePlanOrAuthoringAssets()
        {
            var worldDigest = fixture.WorldPlan.CanonicalDigest;
            var solveDigest = fixture.SolveOrder.OutputDigest;
            var edgeDigest = fixture.IntersectorPlan.OutputDigest;
            var edgeFacts = fixture.IntersectorPlan.Edges.Select(value =>
                value.Id + "|" + value.CanonicalDigest).ToArray();
            var landmarkDigest = SpecialLandmarkRegionStarterCatalog.CanonicalDigest;
            var resourceDigest = CoreResourceRegionStarterCatalog.CanonicalDigest;
            var result = fixture.Plan();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(fixture.WorldPlan.CanonicalDigest, Is.EqualTo(worldDigest));
            Assert.That(fixture.SolveOrder.OutputDigest, Is.EqualTo(solveDigest));
            Assert.That(fixture.IntersectorPlan.OutputDigest, Is.EqualTo(edgeDigest));
            Assert.That(fixture.IntersectorPlan.Edges.Select(value => value.Id + "|" + value.CanonicalDigest),
                Is.EqualTo(edgeFacts));
            Assert.That(SpecialLandmarkRegionStarterCatalog.CanonicalDigest, Is.EqualTo(landmarkDigest));
            Assert.That(CoreResourceRegionStarterCatalog.CanonicalDigest, Is.EqualTo(resourceDigest));
            Assert.That(result.Plan.NewRngDrawCount, Is.Zero);
            Assert.That(result.Plan.FallbackCarveCount, Is.Zero);
            Assert.That(result.Plan.SectorRerenderCount, Is.Zero);
            Assert.That(result.Plan.GeneratedFileWriteCount, Is.Zero);
            Assert.That(result.Plan.TilemapMutationCount, Is.Zero);
            Assert.That(result.Plan.SceneMutationCount, Is.Zero);
            Assert.That(result.Plan.PrefabMutationCount, Is.Zero);
            Assert.That(result.Plan.GameObjectMutationCount, Is.Zero);
            Assert.That(result.Plan.GameplaySpawnCount, Is.Zero);
            Assert.That(result.Plan.SpecialRegionMutationCount, Is.Zero);
            Assert.That(result.Plan.SectorPlannerMutationCount, Is.Zero);
            Assert.That(result.Plan.WorldPlanMutationCount, Is.Zero);
            Assert.That(result.Plan.IntersectorPlanMutationCount, Is.Zero);
        }

        [Test]
        public void Map15HandoffKeepsMap15_04Locked()
        {
            var result = fixture.Plan();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(WorldMultiSectorReservationPlan.DownstreamOwner,
                Is.EqualTo("MAP15_04_IMPLEMENT_WORLD_PACING_DENSITY_AND_REPETITION"));
            Assert.That(WorldMultiSectorReservationPlan.OpensDownstreamTask, Is.False);
            Assert.That(result.Plan.Request.PublicationLabel,
                Is.EqualTo(WorldSpecialClusterPolicyPlanner.ReferencePublicationLabel));
            Assert.That(result.Plan.ObservedWorldSectorCount, Is.EqualTo(169));
            Assert.That(result.Plan.ObservedInternalEdgeCount, Is.EqualTo(312));
        }

        private static WorldReservationPolicyResult PlanWithTransaction(
            ReferenceReservationFixture reference,
            WorldSpecialReservationTransaction original,
            WorldSpecialReservationTransaction replacement) =>
            WorldSpecialClusterPolicyPlanner.Plan(reference.Request(
                reference.Transactions.Where(value => !ReferenceEquals(value, original)).Concat(new[] { replacement }),
                reference.ClusterPolicies,
                reference.Allowances,
                reference.QuietClaims));

        private static WorldSpecialReservationTransaction CopyTransaction(
            WorldSpecialReservationTransaction source,
            IEnumerable<WorldSectorId> sectors = null,
            string returnEvidenceId = null) =>
            new WorldSpecialReservationTransaction(
                source.TransactionId,
                source.SpecialKindId,
                source.AuthorityKind,
                source.State,
                source.SpanKind,
                sectors ?? source.SectorIds,
                source.EdgeIds,
                source.RequiresEntryReturnEvidence,
                source.EntryEvidenceId,
                returnEvidenceId ?? source.ReturnEvidenceId,
                source.ExplicitlyOwnsProtectedEdge,
                source.MergeReason,
                source.SourceOwner);

        private static string ConflictIdentity(WorldReservationConflict value) => string.Join("|", new[]
        {
            value.ConflictType.ToString(), value.Subject, value.WinnerId, value.WinnerKind.ToString(),
            value.LoserId, value.LoserKind.ToString(), value.Reason,
        });

        private static string Join(WorldReservationPolicyResult result) =>
            result == null ? "null" : string.Join(";", result.Failures.Select(value => value.ToString()));

        private sealed class ReferenceReservationFixture
        {
            internal const string Map14PhaseExitDigest =
                "5b8ed6a3c8b0a20fe2f2d05eea0b7731522aff30e691ca606c638adcdbd62d82";
            internal const string SpecialConflictClusterPolicyId = "POLICY_CLUSTER_SPECIAL_CONFLICT";

            private ReferenceReservationFixture(
                WorldPlanInput worldPlan,
                WorldSolveOrderResult solveOrder,
                WorldIntersectorEdgePlan intersectorPlan,
                WorldSpecialReservationTransaction[] transactions,
                WorldClusterContainmentPolicy[] clusterPolicies,
                WorldClusterCrossSectorAllowance[] allowances,
                WorldReservationClaim[] quietClaims)
            {
                WorldPlan = worldPlan;
                SolveOrder = solveOrder;
                IntersectorPlan = intersectorPlan;
                Transactions = transactions;
                ClusterPolicies = clusterPolicies;
                Allowances = allowances;
                QuietClaims = quietClaims;
            }

            internal WorldPlanInput WorldPlan { get; }
            internal WorldSolveOrderResult SolveOrder { get; }
            internal WorldIntersectorEdgePlan IntersectorPlan { get; }
            internal WorldSpecialReservationTransaction[] Transactions { get; }
            internal WorldClusterContainmentPolicy[] ClusterPolicies { get; }
            internal WorldClusterCrossSectorAllowance[] Allowances { get; }
            internal WorldReservationClaim[] QuietClaims { get; }

            internal static ReferenceReservationFixture Create()
            {
                var specialIds = new HashSet<int> { 2, 3, 14, 28, 42 };
                var nodes = Enumerable.Range(0, WorldPlanInput.SectorCount)
                    .Select(id => new WorldSectorNode(
                        new WorldSectorId(id),
                        new WorldSectorCoordinate(id % WorldPlanInput.SectorColumns,
                            id / WorldPlanInput.SectorColumns),
                        "BIO_REFERENCE_" + ((id / WorldPlanInput.SectorColumns) % 4),
                        1,
                        id <= 1 ? AccessClass.MandatoryNoTool : AccessClass.OptionalNoTool,
                        id <= 1 ? PacingRole.Traversal : PacingRole.Quiet,
                        specialIds.Contains(id),
                        false,
                        false,
                        id == 0,
                        specialIds.Contains(id) ? SpecialReservationId(id) : string.Empty))
                    .ToArray();
                var dependencies = new List<WorldDependencyEdge>
                {
                    new WorldDependencyEdge(new WorldSectorId(0), new WorldSectorId(1),
                        WorldDependencyKind.MandatoryRoute, "REFERENCE_MANDATORY_ROUTE", "MAP05_MANDATORY_ROUTE"),
                };
                dependencies.AddRange(specialIds.Select(id => new WorldDependencyEdge(
                    new WorldSectorId(0), new WorldSectorId(id), WorldDependencyKind.SpecialReservation,
                    "REFERENCE_SPECIAL_RESERVATION", "MAP13_SPECIAL_REGION")));
                var worldPlan = new WorldPlanInput(
                    nodes,
                    dependencies,
                    new WorldRetryEnvelope(6, 1, WorldSolveAbortReason.SectorLocalAttemptsExhausted),
                    Map14PhaseExitDigest,
                    WorldSolveOrderPlanner.ReferencePublicationLabel);
                var solveOrder = WorldSolveOrderPlanner.Plan(worldPlan);
                if (!solveOrder.Success) throw new InvalidOperationException(string.Join(";", solveOrder.Failures));

                var boundaryEdge = EdgeId(4, 5);
                var projections = BuildProjections(boundaryEdge);
                var boundary = new WorldBoundaryBinding(
                    boundaryEdge,
                    MoonpalaceCraterRootBoundaryAuthoringContract.PairRuleId,
                    MoonpalaceCraterRootBoundaryAuthoringContract.ProfileIds[0],
                    MoonpalaceCraterRootBoundaryAuthoringContract.CandidateIds[0],
                    new[]
                    {
                        MoonpalaceBoundaryWarningMarkerCategory.Tile.Token,
                        MoonpalaceBoundaryWarningMarkerCategory.Background.Token,
                    },
                    "MAP08_BOUNDARY_AUTHORITY");
                var edgeRequest = new WorldIntersectorBuildRequest(
                    worldPlan,
                    solveOrder,
                    projections,
                    new[] { boundary },
                    Map14PhaseExitDigest,
                    WorldIntersectorDigest.HashCanonicalText(MoonpalaceBiomePairCatalog.Canonical.Signature),
                    WorldBoundarySocketIntegrator.ReferencePublicationLabel);
                var edgeResult = WorldBoundarySocketIntegrator.Integrate(edgeRequest);
                if (!edgeResult.Success) throw new InvalidOperationException(string.Join(";", edgeResult.Failures));

                var villageEdge = EdgeId(2, 3);
                var transactions = new[]
                {
                    new WorldSpecialReservationTransaction(
                        "TX_VILLAGE_2S", SpecialRegionKind.Village.ToString(), SpecialRegionKind.Village,
                        WorldReservationTransactionState.Fixed, WorldReservationSpanKind.TwoSector,
                        new[] { new WorldSectorId(2), new WorldSectorId(3) }, new[] { villageEdge },
                        true, "VILLAGE_ENTRY_PORT", "VILLAGE_RETURN_PORT", false, string.Empty,
                        "MAP13_SPECIAL_REGION"),
                    Single("TX_CORE_RESOURCE", SpecialRegionKind.CoreResource, 14),
                    Single("TX_FORGE", SpecialRegionKind.Forge, 28),
                    Single("TX_BOSS", SpecialRegionKind.Boss, 42),
                    Deferred("TX_MERCHANT", SpecialLandmarkKind.WanderingMerchantCave.ToString()),
                    Deferred("TX_MARU", SpecialLandmarkKind.MaruTimeShrine.ToString()),
                };

                var crossEdge = EdgeId(30, 31);
                var policies = new[]
                {
                    Contained("POLICY_CLUSTER_20", "TC_MOON_BRIDGE", "SPINE_BASE", 20),
                    Contained(SpecialConflictClusterPolicyId, "TC_ROOT_CAVITY", "SPINE_LOW", 14),
                    new WorldClusterContainmentPolicy(
                        "POLICY_CLUSTER_CROSS_30_31", new TerrainClusterId("TC_RUIN_SPAN"),
                        new SpineVariantId("SPINE_RUIN_CROSS"), WorldClusterSpanKind.CrossSectorAllowlisted,
                        new[] { new WorldSectorId(30), new WorldSectorId(31) }, crossEdge,
                        "MAP11_EXPLICIT_TWO_SECTOR_SPAN", "MAP11_TERRAIN_CLUSTER"),
                    Contained("POLICY_CLUSTER_40", "TC_DOUGH_RECOVERY", "SPINE_RECOVERY", 40),
                };
                var allowance = new WorldClusterCrossSectorAllowance(
                    "ALLOW_TC_RUIN_SPAN_30_31", policies[2].ClusterId, policies[2].VariantId, crossEdge,
                    WorldReservationOwnerKind.CrossSectorCluster, WorldClusterSpanKind.CrossSectorAllowlisted,
                    policies[2].SpanReason, "MAP11_TERRAIN_CLUSTER");
                var quietClaims = new[]
                {
                    Quiet("CLAIM_QUIET_VILLAGE_CONFLICT", 2),
                    Quiet("CLAIM_QUIET_50", 50),
                };
                return new ReferenceReservationFixture(
                    worldPlan, solveOrder, edgeResult.Plan, transactions, policies,
                    new[] { allowance }, quietClaims);
            }

            internal WorldReservationPolicyRequest Request(
                IEnumerable<WorldSpecialReservationTransaction> transactions,
                IEnumerable<WorldClusterContainmentPolicy> policies,
                IEnumerable<WorldClusterCrossSectorAllowance> allowances,
                IEnumerable<WorldReservationClaim> quietClaims,
                string map13AuthorityDigest = null,
                int tilemapMutationCount = 0)
            {
                return new WorldReservationPolicyRequest(
                    WorldPlan,
                    SolveOrder,
                    IntersectorPlan,
                    transactions,
                    policies,
                    allowances,
                    quietClaims,
                    map13AuthorityDigest ?? AuthorityDigest(),
                    Map14PhaseExitDigest,
                    WorldSpecialClusterPolicyPlanner.ReferencePublicationLabel,
                    tilemapMutationCount: tilemapMutationCount);
            }

            internal WorldReservationPolicyResult Plan() => WorldSpecialClusterPolicyPlanner.Plan(
                Request(Transactions, ClusterPolicies, Allowances, QuietClaims));

            internal WorldIntersectorEdgeId Edge(int first, int second) => EdgeId(first, second);

            private static WorldSocketProjection[] BuildProjections(WorldIntersectorEdgeId boundaryEdge)
            {
                var result = new List<WorldSocketProjection>(WorldIntersectorEdgePlan.EndpointCount);
                for (var y = 0; y < WorldPlanInput.SectorRows; y++)
                for (var x = 0; x < WorldPlanInput.SectorColumns - 1; x++)
                {
                    var first = new WorldSectorId((y * WorldPlanInput.SectorColumns) + x);
                    var second = new WorldSectorId(first.Value + 1);
                    var edge = new WorldIntersectorEdgeId(first, second, WorldEdgeOrientation.Horizontal);
                    var mandatory = edge == EdgeId(0, 1);
                    var boundary = edge == boundaryEdge;
                    result.Add(Projection(first, WorldSectorSide.East, new WorldSocketAnchor(47, 16, 3),
                        mandatory, boundary));
                    result.Add(Projection(second, WorldSectorSide.West, new WorldSocketAnchor(0, 16, 3),
                        mandatory, boundary));
                }
                for (var y = 0; y < WorldPlanInput.SectorRows - 1; y++)
                for (var x = 0; x < WorldPlanInput.SectorColumns; x++)
                {
                    var first = new WorldSectorId((y * WorldPlanInput.SectorColumns) + x);
                    var second = new WorldSectorId(first.Value + WorldPlanInput.SectorColumns);
                    result.Add(Projection(first, WorldSectorSide.North, new WorldSocketAnchor(24, 31, 3),
                        false, false));
                    result.Add(Projection(second, WorldSectorSide.South, new WorldSocketAnchor(24, 0, 3),
                        false, false));
                }
                return result.ToArray();
            }

            private static WorldSocketProjection Projection(
                WorldSectorId sector,
                WorldSectorSide side,
                WorldSocketAnchor anchor,
                bool mandatory,
                bool boundary) =>
                new WorldSocketProjection(
                    sector, side, anchor, false, mandatory, boundary,
                    boundary ? "MAP08_BOUNDARY" : "MAP15_01_WORLD_PLAN");

            private static WorldSpecialReservationTransaction Single(
                string id,
                SpecialRegionKind kind,
                int sector) =>
                new WorldSpecialReservationTransaction(
                    id, kind.ToString(), kind, WorldReservationTransactionState.Fixed,
                    WorldReservationSpanKind.SingleSector, new[] { new WorldSectorId(sector) },
                    Array.Empty<WorldIntersectorEdgeId>(), true, id + "_ENTRY", id + "_RETURN",
                    false, string.Empty, "MAP13_SPECIAL_REGION");

            private static WorldSpecialReservationTransaction Deferred(string id, string kindId) =>
                new WorldSpecialReservationTransaction(
                    id, kindId, SpecialRegionKind.OptionalLandmark, WorldReservationTransactionState.Deferred,
                    WorldReservationSpanKind.Deferred, Array.Empty<WorldSectorId>(),
                    Array.Empty<WorldIntersectorEdgeId>(), false, string.Empty, string.Empty,
                    false, string.Empty, "MAP13_DEFERRED_OPTIONAL_LOCAL");

            private static WorldClusterContainmentPolicy Contained(
                string policyId,
                string clusterId,
                string variantId,
                int sector) =>
                new WorldClusterContainmentPolicy(
                    policyId, new TerrainClusterId(clusterId), new SpineVariantId(variantId),
                    WorldClusterSpanKind.SectorContained, new[] { new WorldSectorId(sector) }, null,
                    "SECTOR_CONTAINED_DEFAULT", "MAP11_TERRAIN_CLUSTER");

            private static WorldReservationClaim Quiet(string claimId, int sector) =>
                new WorldReservationClaim(
                    claimId, WorldReservationOwnerKind.QuietFiller, new WorldSectorId(sector), null,
                    claimId, "REFERENCE QUIET/FILLER RESERVATION", "MAP14_QUIET_FILL");

            private static WorldIntersectorEdgeId EdgeId(int first, int second) =>
                new WorldIntersectorEdgeId(
                    new WorldSectorId(first), new WorldSectorId(second), WorldEdgeOrientation.Horizontal);

            private static string SpecialReservationId(int id)
            {
                if (id == 2 || id == 3) return "SR_VILLAGE_REFERENCE";
                if (id == 14) return "SR_CORE_RESOURCE_REFERENCE";
                if (id == 28) return "SR_FORGE_REFERENCE";
                return "SR_BOSS_REFERENCE";
            }

            private static string AuthorityDigest() => WorldReservationPolicyDigest.HashCanonicalText(
                CoreResourceRegionStarterCatalog.CanonicalDigest + "\n" +
                SpecialLandmarkRegionStarterCatalog.CanonicalDigest);
        }
    }
}
