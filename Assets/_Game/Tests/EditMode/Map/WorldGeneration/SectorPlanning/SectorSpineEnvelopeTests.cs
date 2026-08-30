using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.SectorPlanning
{
    [TestFixture]
    [Category("MAP14_04")]
    public sealed class SectorSpineEnvelopeTests
    {
        [Test]
        public void BuildPublishesCanonicalSpineEnvelopePlanFromClusterPlacements()
        {
            var fixture = Fixture.Create();
            var result = fixture.Build();

            Assert.That(result.Success, Is.True, Join(result.Errors));
            Assert.That(result.Plan, Is.Not.Null);
            Assert.That(result.Plan.SectorCount, Is.EqualTo(9));
            Assert.That(result.Plan.Graph.Nodes, Has.Count.EqualTo(34));
            Assert.That(result.Plan.Graph.Edges, Has.Count.EqualTo(26));
            Assert.That(result.Plan.SpineGraphDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(result.Plan.EnvelopeDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(result.Plan.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(result.Plan.Map14_05HandoffReady, Is.True);
            Assert.Throws<NotSupportedException>(() => ((IList<SectorSpineNode>)result.Plan.Graph.Nodes).Add(result.Plan.Graph.Nodes[0]));
            Assert.Throws<NotSupportedException>(() => ((IList<SectorTraversalEnvelopeCell>)result.Plan.EnvelopeCells).Add(result.Plan.EnvelopeCells[0]));

            TestContext.Out.WriteLine("MAP14_04_METRICS|sectors={0}|nodes={1}|edges={2}|envelope={3}|protected={4}|mandatory={5}|optionalRecovery={6}|compatibleOverlap={7}|blockingOverlap={8}",
                result.Plan.SectorCount, result.Plan.NodeCount, result.Plan.EdgeCount, result.Plan.EnvelopeCellCount,
                result.Plan.ProtectedOpenCellCount, result.Plan.MandatoryRouteCount, result.Plan.OptionalHighRecoveryRouteCount,
                result.Plan.AnchorCompatibleOverlapCount, result.Plan.BlockingAnchorOverlapCount);
            TestContext.Out.WriteLine("MAP14_04_NODE_COUNTS|" + string.Join("|", result.Plan.Graph.NodeCountByKind.Select(value => value.Key + "=" + value.Value)));
            TestContext.Out.WriteLine("MAP14_04_EDGE_COUNTS|" + string.Join("|", result.Plan.Graph.EdgeCountByKind.Select(value => value.Key + "=" + value.Value)));
            TestContext.Out.WriteLine("MAP14_04_ENVELOPE_COUNTS|" + string.Join("|", result.Plan.EnvelopeCellCountByKind.Select(value => value.Key + "=" + value.Value)));
            TestContext.Out.WriteLine("MAP14_04_DIGESTS|graph={0}|envelope={1}|plan={2}", result.Plan.SpineGraphDigest, result.Plan.EnvelopeDigest, result.Plan.CanonicalDigest);
            TestContext.Out.WriteLine("MAP14_04_CLUSTERS|" + string.Join("|", fixture.PlacementPlan.Placements.OrderBy(value => value.SectorIndex).Select(value => value.ClusterId.Value + "/" + value.VariantId.Value)));
        }

        [Test]
        public void SpineNodesRepresentExternalBoundaryClusterAndSpecialEndpoints()
        {
            var plan = Fixture.Create().BuildPlan();
            Assert.That(plan.Graph.Count(SectorSpineNodeKind.ExternalSocket), Is.EqualTo(4));
            Assert.That(plan.Graph.Count(SectorSpineNodeKind.BoundaryBridge), Is.EqualTo(1));
            Assert.That(plan.Graph.Count(SectorSpineNodeKind.ClusterEntry), Is.EqualTo(9));
            Assert.That(plan.Graph.Count(SectorSpineNodeKind.ClusterExit), Is.EqualTo(9));
            Assert.That(plan.Graph.Count(SectorSpineNodeKind.SpecialEntry), Is.EqualTo(3));
            Assert.That(plan.Graph.Count(SectorSpineNodeKind.SpecialReturn), Is.EqualTo(3));
            Assert.That(plan.Graph.Count(SectorSpineNodeKind.RecoveryJoin), Is.EqualTo(3));
            Assert.That(plan.Graph.Count(SectorSpineNodeKind.OptionalBranch), Is.EqualTo(2));
            Assert.That(plan.Graph.Nodes.All(value => value.SourceIdentity.Length > 64), Is.True);
            Assert.That(plan.Graph.Nodes.Where(value => value.Kind == SectorSpineNodeKind.SpecialEntry || value.Kind == SectorSpineNodeKind.SpecialReturn)
                .Select(value => value.SourceId), Is.All.Contain("ENTRY"));
        }

        [Test]
        public void MandatoryLowRoutesConnectRequiredEndpointsInStableOrder()
        {
            var plan = Fixture.Create().BuildPlan();
            Assert.That(plan.Graph.Count(SectorSpineEdgeKind.MandatoryLow), Is.EqualTo(4));
            Assert.That(plan.Graph.Count(SectorSpineEdgeKind.ClusterConnector), Is.EqualTo(9));
            Assert.That(plan.Graph.Count(SectorSpineEdgeKind.MandatorySpecialConnector), Is.EqualTo(6));
            Assert.That(plan.Graph.Count(SectorSpineEdgeKind.BoundaryConnector), Is.EqualTo(1));
            Assert.That(plan.MandatoryRouteCount, Is.EqualTo(23));
            Assert.That(plan.Graph.Edges.SequenceEqual(plan.Graph.Edges.OrderBy(value => value)), Is.True);
            Assert.That(plan.Graph.Edges.All(value => value.RouteClass == AccessClass.MandatoryNoTool.ToString()), Is.True);
            foreach (var sector in plan.Graph.Nodes.Select(value => value.SectorIndex).Distinct()) AssertRequiredConnected(plan.Graph, sector);
        }

        [Test]
        public void OptionalHighAndRecoveryRoutesRejoinMandatoryRoute()
        {
            var plan = Fixture.Create().BuildPlan();
            Assert.That(plan.Graph.Count(SectorSpineEdgeKind.OptionalHigh), Is.EqualTo(2));
            Assert.That(plan.Graph.Count(SectorSpineEdgeKind.Recovery), Is.EqualTo(1));
            Assert.That(plan.Graph.Count(SectorSpineEdgeKind.Return), Is.EqualTo(3));
            Assert.That(plan.OptionalHighRecoveryRouteCount, Is.EqualTo(3));
            var recoveryIds = plan.Graph.Nodes.Where(value => value.Kind == SectorSpineNodeKind.RecoveryJoin).Select(value => value.NodeId).ToHashSet(StringComparer.Ordinal);
            Assert.That(plan.Graph.Edges.Where(value => value.Kind == SectorSpineEdgeKind.OptionalHigh).All(value => recoveryIds.Contains(value.ToNodeId)), Is.True);
            Assert.That(plan.Graph.Edges.Where(value => value.Kind == SectorSpineEdgeKind.Recovery).All(value => recoveryIds.Contains(value.ToNodeId)), Is.True);
            Assert.That(plan.Graph.Edges.Where(value => value.Kind == SectorSpineEdgeKind.Return).All(value => recoveryIds.Contains(value.FromNodeId)), Is.True);
        }

        [Test]
        public void EnvelopePublishesProtectedOpenClearanceLandingAndRecoveryCells()
        {
            var plan = Fixture.Create().BuildPlan();
            Assert.That(plan.Count(SectorTraversalEnvelopeCellKind.Centerline), Is.GreaterThan(0));
            Assert.That(plan.Count(SectorTraversalEnvelopeCellKind.Floor), Is.GreaterThan(0));
            Assert.That(plan.Count(SectorTraversalEnvelopeCellKind.Clearance), Is.GreaterThan(0));
            Assert.That(plan.Count(SectorTraversalEnvelopeCellKind.Landing), Is.GreaterThan(0));
            Assert.That(plan.Count(SectorTraversalEnvelopeCellKind.Recovery), Is.GreaterThan(0));
            Assert.That(plan.Count(SectorTraversalEnvelopeCellKind.ProtectedOpen), Is.EqualTo(plan.ProtectedOpenCellCount));
            Assert.That(plan.EnvelopeCells.All(value => value.Coordinate.X >= 0 && value.Coordinate.X < 48 && value.Coordinate.Y >= 0 && value.Coordinate.Y < 32), Is.True);
            var required = plan.EnvelopeCells.Where(value => value.Kind == SectorTraversalEnvelopeCellKind.Centerline
                                                             || value.Kind == SectorTraversalEnvelopeCellKind.Clearance
                                                             || value.Kind == SectorTraversalEnvelopeCellKind.Landing
                                                             || value.Kind == SectorTraversalEnvelopeCellKind.Recovery
                                                             || value.Kind == SectorTraversalEnvelopeCellKind.ProtectedAnchorBridge)
                .Select(Key).ToHashSet(StringComparer.Ordinal);
            Assert.That(plan.ProtectedOpenCells.Select(Key).ToHashSet(StringComparer.Ordinal).SetEquals(required), Is.True);
        }

        [Test]
        public void EnvelopeAvoidsBlockingAnchorsAndPreservesCompatibleBridgeOverlaps()
        {
            var fixture = Fixture.Create();
            var plan = fixture.BuildPlan();
            Assert.That(plan.AnchorCompatibleOverlapCount, Is.GreaterThan(0));
            Assert.That(plan.BlockingAnchorOverlapCount, Is.Zero);
            Assert.That(plan.Count(SectorTraversalEnvelopeCellKind.ProtectedAnchorBridge), Is.GreaterThan(0));

            var failure = SectorSpineGraphBuilder.Build(fixture.Request(new[] { SectorSpineEnvelopeErrorCode.EdgeCrossesBlockingAnchor }));
            Assert.That(failure.Success, Is.False);
            Assert.That(failure.Graph, Is.Null);
            Assert.That(failure.CanonicalDigest, Is.Empty);
            Assert.That(failure.Errors.Select(value => value.Code), Does.Contain(SectorSpineEnvelopeErrorCode.EdgeCrossesBlockingAnchor));
        }

        [Test]
        public void SpineEnvelopePreservesInputAnchorAndClusterIdentities()
        {
            var fixture = Fixture.Create();
            var plan = fixture.BuildPlan();
            Assert.That(plan.PlannerInputDigestBefore, Is.EqualTo(fixture.Input.CanonicalDigest));
            Assert.That(plan.PlannerInputDigestAfter, Is.EqualTo(plan.PlannerInputDigestBefore));
            Assert.That(plan.AnchorPlanDigestBefore, Is.EqualTo(fixture.AnchorPlan.CanonicalDigest));
            Assert.That(plan.AnchorPlanDigestAfter, Is.EqualTo(plan.AnchorPlanDigestBefore));
            Assert.That(plan.ClusterPlacementPlanDigestBefore, Is.EqualTo(fixture.PlacementPlan.CanonicalDigest));
            Assert.That(plan.ClusterPlacementPlanDigestAfter, Is.EqualTo(plan.ClusterPlacementPlanDigestBefore));
            Assert.That(plan.PacingAssignmentDigestAfter, Is.EqualTo(plan.PacingAssignmentDigestBefore));
            Assert.That(plan.RouteAccessIdentityAfter, Is.EqualTo(plan.RouteAccessIdentityBefore));
            Assert.That(plan.ExternalSocketIdentityAfter, Is.EqualTo(plan.ExternalSocketIdentityBefore));
            Assert.That(plan.BoundaryIdentityAfter, Is.EqualTo(plan.BoundaryIdentityBefore));
            Assert.That(plan.SpecialIdentityAfter, Is.EqualTo(plan.SpecialIdentityBefore));
            Assert.That(plan.ClusterIdentityAfter, Is.EqualTo(plan.ClusterIdentityBefore));
        }

        [Test]
        public void InvalidMissingEndpointBlockingOverlapAndMutationClaimsFailAtomically()
        {
            var fixture = Fixture.Create();
            foreach (var code in new[]
            {
                SectorSpineEnvelopeErrorCode.MissingEndpoint,
                SectorSpineEnvelopeErrorCode.EdgeOutOfBounds,
                SectorSpineEnvelopeErrorCode.EnvelopeOverlapsBlockingAnchor,
            })
            {
                var failure = SectorSpineGraphBuilder.Build(fixture.Request(new[] { code }));
                Assert.That(failure.Success, Is.False);
                Assert.That(failure.Graph, Is.Null);
                Assert.That(failure.CanonicalDigest, Is.Empty);
                Assert.That(failure.Errors.Select(value => value.Code), Does.Contain(code));
                Assert.That(failure.Errors.SequenceEqual(failure.Errors.OrderBy(value => value)), Is.True);
            }

            var mutation = SectorSpineGraphBuilder.Build(fixture.MutationRequest());
            Assert.That(mutation.Success, Is.False);
            Assert.That(mutation.Graph, Is.Null);
            Assert.That(mutation.CanonicalDigest, Is.Empty);
            Assert.That(mutation.Errors.Select(value => value.Code), Does.Contain(SectorSpineEnvelopeErrorCode.RouteAccessMutationClaim));
            Assert.That(mutation.Errors.Select(value => value.Code), Does.Contain(SectorSpineEnvelopeErrorCode.AnchorMutationClaim));
            Assert.That(mutation.Errors.Select(value => value.Code), Does.Contain(SectorSpineEnvelopeErrorCode.ClusterMutationClaim));
            Assert.That(mutation.Errors.Select(value => value.Code), Does.Contain(SectorSpineEnvelopeErrorCode.PatternMutationClaim));
            Assert.That(mutation.Errors.Select(value => value.Code), Does.Contain(SectorSpineEnvelopeErrorCode.ActivityMutationClaim));
            Assert.That(mutation.Errors.Select(value => value.Code), Does.Contain(SectorSpineEnvelopeErrorCode.SolverMutationClaim));
            Assert.That(mutation.Errors.Select(value => value.Code), Does.Contain(SectorSpineEnvelopeErrorCode.RngMutationClaim));
            Assert.That(mutation.Errors.Select(value => value.Code), Does.Contain(SectorSpineEnvelopeErrorCode.TileMutationClaim));
        }

        [Test]
        public void PublicationIsDeterministicAcrossRepeatReverseAndTurkishCulture()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var first = Fixture.Create().BuildPlan();
                var repeat = Fixture.Create().BuildPlan();
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var reversed = Fixture.Create(reverse: true).BuildPlan();
                Assert.That(repeat.SpineGraphDigest, Is.EqualTo(first.SpineGraphDigest));
                Assert.That(reversed.SpineGraphDigest, Is.EqualTo(first.SpineGraphDigest));
                Assert.That(reversed.EnvelopeDigest, Is.EqualTo(first.EnvelopeDigest));
                Assert.That(reversed.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Test]
        public void BuildDoesNotInvokePatternActivityRetryTileOrPhysicsSystems()
        {
            var result = Fixture.Create().Build();
            Assert.That(result.Success, Is.True, Join(result.Errors));
            Assert.That(result.MutationCount, Is.Zero);
            Assert.That(result.SolverInvocationCount, Is.Zero);
            Assert.That(result.RandomDrawCount, Is.Zero);
            Assert.That(result.TileWriteCount, Is.Zero);
            Assert.That(result.Plan.MicroPatternRenderCount, Is.Zero);
            Assert.That(result.Plan.ActivityEventPlacementCount, Is.Zero);
            Assert.That(result.Plan.RetryCount, Is.Zero);
            Assert.That(result.Plan.CanvasOwnershipWriteCount, Is.Zero);
            Assert.That(result.Plan.SceneMutationCount, Is.Zero);
            Assert.That(result.Plan.PhysicsInvocationCount, Is.Zero);
        }

        private static void AssertRequiredConnected(SectorSpineGraph graph, int sectorIndex)
        {
            var required = graph.Nodes.Where(value => value.SectorIndex == sectorIndex
                && (value.Kind == SectorSpineNodeKind.ExternalSocket || value.Kind == SectorSpineNodeKind.ClusterEntry
                    || value.Kind == SectorSpineNodeKind.ClusterExit || value.Kind == SectorSpineNodeKind.SpecialEntry
                    || value.Kind == SectorSpineNodeKind.SpecialReturn)).ToArray();
            var adjacency = required.ToDictionary(value => value.NodeId, value => new List<string>(), StringComparer.Ordinal);
            foreach (var edge in graph.Edges.Where(value => value.SectorIndex == sectorIndex
                && value.Kind != SectorSpineEdgeKind.OptionalHigh && value.Kind != SectorSpineEdgeKind.Recovery))
            {
                if (!adjacency.ContainsKey(edge.FromNodeId) || !adjacency.ContainsKey(edge.ToNodeId)) continue;
                adjacency[edge.FromNodeId].Add(edge.ToNodeId);
                adjacency[edge.ToNodeId].Add(edge.FromNodeId);
            }
            var visited = new HashSet<string>(StringComparer.Ordinal) { required[0].NodeId };
            var queue = new Queue<string>(); queue.Enqueue(required[0].NodeId);
            while (queue.Count > 0) foreach (var next in adjacency[queue.Dequeue()]) if (visited.Add(next)) queue.Enqueue(next);
            Assert.That(visited.Count, Is.EqualTo(required.Length), "Disconnected mandatory sector " + sectorIndex);
        }

        private static string Key(SectorTraversalEnvelopeCell value) => value.SectorIndex + ":" + value.Coordinate.X + "," + value.Coordinate.Y;
        private static string Join(IEnumerable<SectorSpineEnvelopeError> errors) => string.Join("\n", errors.Select(value => value.ToString()));

        private sealed class Fixture
        {
            private static readonly SectorCoord Plain = new SectorCoord(1, 1);
            private static readonly SectorCoord Quiet = new SectorCoord(2, 1);
            private static readonly SectorCoord Village = new SectorCoord(3, 1);
            private static readonly SectorCoord Core = new SectorCoord(4, 1);
            private static readonly SectorCoord Forge = new SectorCoord(5, 1);
            private static readonly SectorCoord Boss = new SectorCoord(6, 1);
            private static readonly SectorCoord Activity = new SectorCoord(7, 1);
            private static readonly SectorCoord Deferred = new SectorCoord(8, 1);
            private static readonly SectorCoord Neighbor = new SectorCoord(9, 1);

            private Fixture(SectorPlannerInput input, IReadOnlyList<SectorPacingAssignment> assignments,
                SectorFixedAnchorPlan anchorPlan, SectorClusterPlacementPlan placementPlan)
            {
                Input = input;
                Assignments = assignments;
                AnchorPlan = anchorPlan;
                PlacementPlan = placementPlan;
            }

            internal SectorPlannerInput Input { get; }
            internal IReadOnlyList<SectorPacingAssignment> Assignments { get; }
            internal SectorFixedAnchorPlan AnchorPlan { get; }
            internal SectorClusterPlacementPlan PlacementPlan { get; }

            internal static Fixture Create(bool reverse = false)
            {
                var sectors = CreateSectors();
                if (reverse) sectors.Reverse();
                var authority = SectorPlannerAuthorityDigestSnapshot.CaptureCurrentPublicAuthorities(
                    Digest('a'), Digest('b'), 24, Digest('c'), 16, Digest('d'), 7, Digest('e'), 5, Digest('f'));
                var inputResult = SectorPlannerInputBuilder.Build(new SectorPlannerInputRequest(
                    sectors, authority, SectorPlannerInputBuilder.ReferencePublicationLabel));
                if (!inputResult.Success) throw new InvalidOperationException(string.Join("\n", inputResult.Errors));

                var assignments = SectorPacingRolePlanner.Assign(inputResult.Input).ToList();
                var projections = CreateAnchors();
                if (reverse) { assignments.Reverse(); projections.Reverse(); }
                var anchorResult = SectorFixedAnchorPlanner.Build(new SectorFixedAnchorBuildRequest(
                    inputResult.Input, assignments, projections, SectorFixedAnchorPlanner.ReferencePublicationLabel));
                if (!anchorResult.Success) throw new InvalidOperationException(string.Join("\n", anchorResult.Errors));

                var catalog = CreateCatalog();
                if (reverse) catalog.Reverse();
                var candidates = SectorClusterCandidateBuilder.Build(new SectorClusterCandidateBuildRequest(
                    inputResult.Input, assignments, anchorResult.Plan, catalog, SectorClusterCandidateBuilder.ReferenceCandidatePublicationLabel));
                if (!candidates.Success) throw new InvalidOperationException(string.Join("\n", candidates.Errors));
                var placements = SectorClusterPlacementPlanner.Place(new SectorClusterPlacementRequest(
                    candidates.CandidateSet, anchorResult.Plan, SectorClusterPlacementPlanner.ReferencePlacementPublicationLabel));
                if (!placements.Success) throw new InvalidOperationException(string.Join("\n", placements.Errors));
                return new Fixture(inputResult.Input, assignments, anchorResult.Plan, placements.Plan);
            }

            internal SectorSpineEnvelopeBuildRequest Request(IEnumerable<SectorSpineEnvelopeErrorCode> faults = null)
                => new SectorSpineEnvelopeBuildRequest(Input, Assignments, AnchorPlan, PlacementPlan,
                    SectorSpineGraphBuilder.ReferenceGraphPublicationLabel,
                    SectorTraversalEnvelopeBuilder.ReferenceEnvelopePublicationLabel,
                    sourceReferenceFaults: faults);

            internal SectorSpineEnvelopeBuildRequest MutationRequest()
                => new SectorSpineEnvelopeBuildRequest(Input, Assignments, AnchorPlan, PlacementPlan,
                    SectorSpineGraphBuilder.ReferenceGraphPublicationLabel,
                    SectorTraversalEnvelopeBuilder.ReferenceEnvelopePublicationLabel,
                    routeAccessMutationClaim: true, anchorMutationClaim: true, clusterMutationClaim: true,
                    microPatternRenderCount: 1, activityEventPlacementCount: 1, retryCount: 1,
                    solverInvocationCount: 1, randomDrawCount: 1, tileWriteCount: 1,
                    canvasOwnershipWriteCount: 1, sceneMutationCount: 1, physicsInvocationCount: 1);

            internal SectorSpineEnvelopeBuildResult Build()
            {
                var request = Request();
                var graph = SectorSpineGraphBuilder.Build(request);
                if (!graph.Success) throw new InvalidOperationException(Join(graph.Errors));
                return SectorTraversalEnvelopeBuilder.Build(request, graph.Graph);
            }

            internal SectorSpineEnvelopePlan BuildPlan()
            {
                var result = Build();
                if (!result.Success) throw new InvalidOperationException(Join(result.Errors));
                return result.Plan;
            }

            private static List<SectorPlannerSectorSnapshot> CreateSectors()
            {
                return new List<SectorPlannerSectorSnapshot>
                {
                    Sector(Plain, MoonpalaceBiomeId.MoonCrater, new[] { PacingRole.Traversal, PacingRole.Recovery },
                        route: new SectorPlannerRouteSnapshot(1, AccessClass.MandatoryNoTool, new[] { "SOCKET_L", "SOCKET_R", "SOCKET_U", "SOCKET_D" }, true, true),
                        boundaries: new[] { new SectorPlannerBoundarySnapshot(SectorPlannerSide.Right, "PAIR_CRATER_ROOT", "BOUNDARY_CRATER_ROOT", 1) }),
                    Sector(Quiet, MoonpalaceBiomeId.MoonCrater, new[] { PacingRole.Quiet }, quiet: true),
                    Sector(Village, MoonpalaceBiomeId.CassiaRoot, new[] { PacingRole.Safe, PacingRole.Landmark },
                        special: new SectorPlannerSpecialRegionSnapshot("REGION_VILLAGE", SectorPlannerSpecialRegionKind.Village,
                            SectorPlannerSpecialRegionBinding.ReferenceOnly, "FP_VILLAGE_REFERENCE", false, false, false), ordinal: 2, optionalDistance: 0),
                    Sector(Core, MoonpalaceBiomeId.CassiaRoot, new[] { PacingRole.Resource },
                        sites: new[] { new SectorPlannerSiteSnapshot("SITE_CORE", "CORE_RESOURCE", "RES_CORE", true) },
                        special: Mandatory("REGION_CORE", SectorPlannerSpecialRegionKind.CoreResource, "FP_CORE"), mandatoryDistance: 0),
                    Sector(Forge, MoonpalaceBiomeId.AbandonedMill, new[] { PacingRole.Landmark, PacingRole.Machinery },
                        sites: new[] { new SectorPlannerSiteSnapshot("SITE_FORGE", "FORGE", "RES_FORGE", true) },
                        special: Mandatory("REGION_FORGE", SectorPlannerSpecialRegionKind.Forge, "FP_FORGE"), mandatoryDistance: 0),
                    Sector(Boss, MoonpalaceBiomeId.MoonDough, new[] { PacingRole.Boss },
                        sites: new[] { new SectorPlannerSiteSnapshot("SITE_BOSS", "BOSS_GATE", "RES_BOSS", true) },
                        special: Mandatory("REGION_BOSS", SectorPlannerSpecialRegionKind.Boss, "FP_BOSS"), mandatoryDistance: 0),
                    Sector(Activity, MoonpalaceBiomeId.MoonCrater, new[] { PacingRole.Activity }, activity: true, eventAvailable: true, ordinal: 5),
                    Sector(Deferred, MoonpalaceBiomeId.CassiaRoot, new[] { PacingRole.Discovery },
                        special: new SectorPlannerSpecialRegionSnapshot("REGION_MERCHANT", SectorPlannerSpecialRegionKind.Merchant,
                            SectorPlannerSpecialRegionBinding.DeferredOptionalLocal, string.Empty, false, false, false),
                        optional: new[] { new SectorPlannerOptionalRegionSnapshot("REGION_MERCHANT", SectorPlannerSpecialRegionKind.Merchant, true, true, false) }, optionalDistance: 1),
                    Sector(Neighbor, MoonpalaceBiomeId.AbandonedMill, new[] { PacingRole.Traversal },
                        neighbors: new[]
                        {
                            new SectorPlannerNeighborSnapshot(SectorPlannerSide.Left, new SectorCoord(8, 1), 1, AccessClass.MandatoryNoTool, Array.Empty<string>(), PacingRole.Traversal),
                            new SectorPlannerNeighborSnapshot(SectorPlannerSide.Right, new SectorCoord(10, 1), 1, AccessClass.MandatoryNoTool, Array.Empty<string>(), PacingRole.Traversal),
                            new SectorPlannerNeighborSnapshot(SectorPlannerSide.Up, new SectorCoord(9, 0), 1, AccessClass.MandatoryNoTool, Array.Empty<string>(), PacingRole.Traversal),
                            new SectorPlannerNeighborSnapshot(SectorPlannerSide.Down, new SectorCoord(9, 2), 1, AccessClass.MandatoryNoTool, Array.Empty<string>(), PacingRole.Traversal),
                        }),
                };
            }

            private static SectorPlannerSectorSnapshot Sector(
                SectorCoord coordinate, MoonpalaceBiomeId biome, IEnumerable<PacingRole> roles,
                SectorPlannerRouteSnapshot route = null, IEnumerable<SectorPlannerBoundarySnapshot> boundaries = null,
                IEnumerable<SectorPlannerSiteSnapshot> sites = null, SectorPlannerSpecialRegionSnapshot special = null,
                IEnumerable<SectorPlannerOptionalRegionSnapshot> optional = null, IEnumerable<SectorPlannerNeighborSnapshot> neighbors = null,
                bool quiet = false, bool activity = false, bool eventAvailable = false, int ordinal = 4,
                int mandatoryDistance = 2, int optionalDistance = 3)
            {
                return new SectorPlannerSectorSnapshot(
                    coordinate, (coordinate.Y * 13) + coordinate.X, 48, 32,
                    new SectorPlannerBiomeSnapshot("PATCH_" + coordinate.X, biome.ToString()),
                    route ?? new SectorPlannerRouteSnapshot(1, AccessClass.MandatoryNoTool, Array.Empty<string>(), false, false),
                    boundaries, sites, special ?? SectorPlannerSpecialRegionSnapshot.None, optional, neighbors,
                    new SectorPlannerWorldProgressSnapshot(ordinal, "CHAPTER_REFERENCE", "BRANCH_REFERENCE", mandatoryDistance, optionalDistance),
                    roles, quiet, activity, eventAvailable);
            }

            private static SectorPlannerSpecialRegionSnapshot Mandatory(string id, SectorPlannerSpecialRegionKind kind, string footprint)
                => new SectorPlannerSpecialRegionSnapshot(id, kind, SectorPlannerSpecialRegionBinding.ReservedMandatory, footprint, true, true, true);

            private static List<SectorFixedAnchorProjection> CreateAnchors()
            {
                var result = new List<SectorFixedAnchorProjection>
                {
                    RouteAnchor("ANCHOR_SOCKET_L", "SOCKET_L", SectorPlannerSide.Left, new SectorFixedAnchorRect(0, 14, 1, 4)),
                    RouteAnchor("ANCHOR_SOCKET_R", "SOCKET_R", SectorPlannerSide.Right, new SectorFixedAnchorRect(47, 14, 1, 4)),
                    RouteAnchor("ANCHOR_SOCKET_U", "SOCKET_U", SectorPlannerSide.Up, new SectorFixedAnchorRect(22, 0, 4, 1)),
                    RouteAnchor("ANCHOR_SOCKET_D", "SOCKET_D", SectorPlannerSide.Down, new SectorFixedAnchorRect(22, 31, 4, 1)),
                    new SectorFixedAnchorProjection("ANCHOR_BOUNDARY_FIXED", Plain, SectorFixedAnchorKind.BoundaryFixedSlice,
                        SectorFixedAnchorSource.BoundarySnapshot, SectorFixedAnchorPriority.BoundaryFixedSlice,
                        new SectorFixedAnchorRect(47, 4, 1, 4), "BOUNDARY_CRATER_ROOT", SectorPlannerSide.Right, true, "BOUNDARY_CRATER_ROOT"),
                    new SectorFixedAnchorProjection("ANCHOR_BOUNDARY_WARNING", Plain, SectorFixedAnchorKind.BoundaryWarning,
                        SectorFixedAnchorSource.BoundarySnapshot, SectorFixedAnchorPriority.BoundaryWarning,
                        new SectorFixedAnchorRect(47, 4, 1, 4), "BOUNDARY_CRATER_ROOT", SectorPlannerSide.Right, true, "BOUNDARY_CRATER_ROOT"),
                    new SectorFixedAnchorProjection("ANCHOR_VILLAGE_REFERENCE", Village, SectorFixedAnchorKind.ReferenceOnlyMarker,
                        SectorFixedAnchorSource.SpecialRegionSnapshot, SectorFixedAnchorPriority.ReferenceOnly,
                        new SectorFixedAnchorRect(23, 15, 1, 1), "REGION_VILLAGE"),
                };
                AddSpecial(result, Core, "CORE", "REGION_CORE", "SITE_CORE");
                AddSpecial(result, Forge, "FORGE", "REGION_FORGE", "SITE_FORGE");
                AddSpecial(result, Boss, "BOSS", "REGION_BOSS", "SITE_BOSS");
                return result;
            }

            private static SectorFixedAnchorProjection RouteAnchor(string anchorId, string sourceId, SectorPlannerSide side, SectorFixedAnchorRect rect)
                => new SectorFixedAnchorProjection(anchorId, Plain, SectorFixedAnchorKind.ExternalRouteSocket,
                    SectorFixedAnchorSource.RouteSnapshot, SectorFixedAnchorPriority.ExternalRouteSocket, rect, sourceId, side);

            private static void AddSpecial(ICollection<SectorFixedAnchorProjection> result, SectorCoord coordinate, string token, string regionId, string siteId)
            {
                result.Add(new SectorFixedAnchorProjection("ANCHOR_" + token + "_FOOTPRINT", coordinate,
                    SectorFixedAnchorKind.SpecialFootprint, SectorFixedAnchorSource.SpecialRegionSnapshot,
                    SectorFixedAnchorPriority.SpecialReservation, new SectorFixedAnchorRect(18, 12, 12, 8), regionId,
                    placedOwnershipClaim: true, progressionBlockerClaim: true));
                result.Add(new SectorFixedAnchorProjection("ANCHOR_" + token + "_ENTRY", coordinate,
                    SectorFixedAnchorKind.SpecialEntryReturn, SectorFixedAnchorSource.SpecialRegionSnapshot,
                    SectorFixedAnchorPriority.SpecialTransition, new SectorFixedAnchorRect(16, 14, 2, 4), regionId,
                    placedOwnershipClaim: true, progressionBlockerClaim: true));
                result.Add(new SectorFixedAnchorProjection("ANCHOR_" + token + "_BUFFER", coordinate,
                    SectorFixedAnchorKind.SpecialApronBuffer, SectorFixedAnchorSource.SpecialRegionSnapshot,
                    SectorFixedAnchorPriority.SpecialTransition, new SectorFixedAnchorRect(30, 12, 2, 8), regionId,
                    placedOwnershipClaim: true, progressionBlockerClaim: true));
                result.Add(new SectorFixedAnchorProjection("ANCHOR_" + token + "_SITE", coordinate,
                    SectorFixedAnchorKind.SiteReservation, SectorFixedAnchorSource.SiteSnapshot,
                    SectorFixedAnchorPriority.SpecialReservation, new SectorFixedAnchorRect(20, 22, 4, 2), siteId, placedOwnershipClaim: true));
            }

            private static List<SectorClusterSourceProjection> CreateCatalog()
            {
                var mandatory = new[] { AccessClass.MandatoryNoTool };
                var route1 = new[] { 1 };
                var sockets = new[] { "SOCKET_L", "SOCKET_R", "SOCKET_U", "SOCKET_D" };
                return new List<SectorClusterSourceProjection>
                {
                    Source("TC_REF_TRAVERSAL_BRIDGE", "SPINE_TRAVERSAL_R0", MoonpalaceBiomeId.MoonCrater, PacingRole.Traversal, route1, mandatory, sockets, H2(), Origins(2, 1), false, false, 10),
                    Source("TC_REF_TRAVERSAL_RECOVERY", "SPINE_RECOVERY_R0", MoonpalaceBiomeId.MoonCrater, PacingRole.Recovery, route1, mandatory, sockets, V2(), Origins(1, 2), false, false, 11),
                    Source("TC_REF_QUIET_BUFFER", "SPINE_QUIET_R0", MoonpalaceBiomeId.MoonCrater, PacingRole.Quiet, route1, mandatory, null, H2(), Origins(2, 1), true, false, 20),
                    Source("TC_REF_QUIET_ALCOVE", "SPINE_QUIET_MX", MoonpalaceBiomeId.MoonCrater, PacingRole.Quiet, route1, mandatory, null, V2(), Origins(1, 2), true, false, 21, ClusterFootprintTransform.MirrorX),
                    Source("TC_REF_VILLAGE_APPROACH", "SPINE_SAFE_R0", MoonpalaceBiomeId.CassiaRoot, PacingRole.Safe, route1, mandatory, null, H2(), Origins(2, 1), false, true, 30),
                    Source("TC_REF_VILLAGE_LANDMARK", "SPINE_LANDMARK_MX", MoonpalaceBiomeId.CassiaRoot, PacingRole.Landmark, route1, mandatory, null, V2(), Origins(1, 2), false, true, 31, ClusterFootprintTransform.MirrorX),
                    Source("TC_REF_CORE_RESOURCE_RING", "SPINE_RESOURCE_R0", MoonpalaceBiomeId.CassiaRoot, PacingRole.Resource, route1, mandatory, null, H4(), Origins(4, 1), false, true, 40),
                    Source("TC_REF_CORE_RESOURCE_SHAFT", "SPINE_RESOURCE_MY", MoonpalaceBiomeId.CassiaRoot, PacingRole.Resource, route1, mandatory, null, V3(), Origins(1, 3), false, true, 41, ClusterFootprintTransform.MirrorY),
                    Source("TC_REF_FORGE_MACHINERY", "SPINE_LANDMARK_R0", MoonpalaceBiomeId.AbandonedMill, PacingRole.Landmark, route1, mandatory, null, H4(), Origins(4, 1), false, true, 50),
                    Source("TC_REF_FORGE_SERVICE", "SPINE_MACHINERY_R0", MoonpalaceBiomeId.AbandonedMill, PacingRole.Machinery, route1, mandatory, null, V3(), Origins(1, 3), false, true, 51),
                    Source("TC_REF_BOSS_GATE", "SPINE_BOSS_R0", MoonpalaceBiomeId.MoonDough, PacingRole.Boss, route1, mandatory, null, Boss5(), Origins(4, 2), false, true, 60),
                    Source("TC_REF_BOSS_APPROACH", "SPINE_BOSS_MX", MoonpalaceBiomeId.MoonDough, PacingRole.Boss, route1, mandatory, null, H4(), Origins(4, 1), false, true, 61, ClusterFootprintTransform.MirrorX),
                    Source("TC_REF_ACTIVITY_SHELL", "SPINE_ACTIVITY_R0", MoonpalaceBiomeId.MoonCrater, PacingRole.Activity, route1, mandatory, null, H2(), Origins(2, 1), false, false, 70),
                    Source("TC_REF_ACTIVITY_ALT", "SPINE_ACTIVITY_MY", MoonpalaceBiomeId.MoonCrater, PacingRole.Activity, route1, mandatory, null, V2(), Origins(1, 2), false, false, 71, ClusterFootprintTransform.MirrorY),
                    Source("TC_REF_DISCOVERY_PASSAGE", "SPINE_DISCOVERY_R0", MoonpalaceBiomeId.CassiaRoot, PacingRole.Discovery, route1, mandatory, null, H2(), Origins(2, 1), false, true, 80),
                    Source("TC_REF_DISCOVERY_ALT", "SPINE_DISCOVERY_MX", MoonpalaceBiomeId.CassiaRoot, PacingRole.Discovery, route1, mandatory, null, V2(), Origins(1, 2), false, true, 81, ClusterFootprintTransform.MirrorX),
                    Source("TC_REF_NEIGHBOR_FLOW", "SPINE_NEIGHBOR_R0", MoonpalaceBiomeId.AbandonedMill, PacingRole.Traversal, route1, mandatory, null, L3(), Origins(2, 2), false, false, 90),
                    Source("TC_REF_NEIGHBOR_ALT", "SPINE_NEIGHBOR_R180", MoonpalaceBiomeId.AbandonedMill, PacingRole.Traversal, route1, mandatory, null, H2(), Origins(2, 1), false, false, 91, ClusterFootprintTransform.R180),
                    Source("TC_REJECT_SOCKET", "SPINE_REJECT_SOCKET", MoonpalaceBiomeId.MoonCrater, PacingRole.Traversal, new[] { 2 }, mandatory, sockets, H2(), Origins(2, 1), false, false, 100),
                    Source("TC_REJECT_ACCESS", "SPINE_REJECT_ACCESS", MoonpalaceBiomeId.MoonCrater, PacingRole.Quiet, route1, new[] { AccessClass.OptionalTool }, null, H2(), Origins(2, 1), true, false, 101),
                    Source("TC_REJECT_DENSITY", "SPINE_REJECT_DENSITY", MoonpalaceBiomeId.CassiaRoot, PacingRole.Resource, route1, mandatory, null, H2(), Origins(2, 1), false, true, 102, minimumDensity: 3),
                    Source("TC_REJECT_ANCHOR", "SPINE_REJECT_ANCHOR", MoonpalaceBiomeId.MoonDough, PacingRole.Boss, route1, mandatory, null, Boss5(), new[] { Cell(0, 1) }, false, true, 103),
                };
            }

            private static SectorClusterSourceProjection Source(
                string clusterId, string variantId, MoonpalaceBiomeId biome, PacingRole pacing,
                IEnumerable<int> routes, IEnumerable<AccessClass> access, IEnumerable<string> sockets,
                IEnumerable<SectorClusterFootprintCell> cells, IEnumerable<SectorClusterFootprintCell> origins,
                bool quiet, bool special, int catalogOrder, ClusterFootprintTransform transform = ClusterFootprintTransform.R0,
                int minimumDensity = 2)
                => new SectorClusterSourceProjection(new TerrainClusterId(clusterId), new SpineVariantId(variantId), transform, biome,
                    new[] { pacing }, routes, access, sockets, cells, origins, minimumDensity, 5, quiet, special, catalogOrder, 0);

            private static SectorClusterFootprintCell[] H2() => new[] { Cell(0, 0), Cell(1, 0) };
            private static SectorClusterFootprintCell[] V2() => new[] { Cell(0, 0), Cell(0, 1) };
            private static SectorClusterFootprintCell[] V3() => new[] { Cell(0, 0), Cell(0, 1), Cell(0, 2) };
            private static SectorClusterFootprintCell[] H4() => new[] { Cell(0, 0), Cell(1, 0), Cell(2, 0), Cell(3, 0) };
            private static SectorClusterFootprintCell[] L3() => new[] { Cell(0, 0), Cell(1, 0), Cell(0, 1) };
            private static SectorClusterFootprintCell[] Boss5() => new[] { Cell(0, 0), Cell(1, 0), Cell(2, 0), Cell(3, 0), Cell(0, 1) };
            private static SectorClusterFootprintCell Cell(int x, int y) => new SectorClusterFootprintCell(x, y);
            private static SectorClusterFootprintCell[] Origins(int width, int height)
            {
                var result = new List<SectorClusterFootprintCell>();
                for (var y = 0; y <= 4 - height; y++) for (var x = 0; x <= 4 - width; x++) result.Add(Cell(x, y));
                return result.ToArray();
            }
            private static string Digest(char value) => new string(value, 64);
        }
    }
}
