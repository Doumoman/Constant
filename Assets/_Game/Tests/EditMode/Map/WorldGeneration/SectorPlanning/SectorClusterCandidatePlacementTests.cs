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
    [Category("MAP14_03")]
    public sealed class SectorClusterCandidatePlacementTests
    {
        [Test]
        public void BuildPublishesStableClusterCandidatesFromPlannerInputAndAnchors()
        {
            var fixture = Fixture.Create();
            var mutableCatalog = fixture.Catalog.ToList();
            var request = fixture.CandidateRequest(mutableCatalog);
            mutableCatalog.Clear();

            var result = SectorClusterCandidateBuilder.Build(request);

            Assert.That(result.Success, Is.True, Join(result.Errors));
            Assert.That(result.CandidateSet.SectorCount, Is.EqualTo(9));
            Assert.That(result.CandidateSet.CandidateCount, Is.EqualTo(18));
            Assert.That(result.CandidateSet.CandidateCountBySectorIndex.Values, Is.All.EqualTo(2));
            Assert.That(result.CandidateSet.RejectedCandidateCount, Is.EqualTo(180));
            Assert.That(result.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(SectorClusterCandidateCanonicalDigest.Compute(result.CandidateSet), Is.EqualTo(result.CanonicalDigest));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<SectorClusterCandidate>)result.CandidateSet.Candidates).Add(null));
        }

        [Test]
        public void CandidatesRespectBiomePacingRouteSocketAccessAndFootprintCompatibility()
        {
            var fixture = Fixture.Create();
            var set = fixture.BuildCandidates();

            foreach (var candidate in set.Candidates)
            {
                Assert.That(candidate.Reasons, Does.Contain(SectorClusterCandidateReason.BiomeCompatible));
                Assert.That(candidate.Reasons.Any(value => value == SectorClusterCandidateReason.PacingPrimaryMatch
                                                           || value == SectorClusterCandidateReason.PacingCandidateMatch), Is.True);
                Assert.That(candidate.Reasons, Does.Contain(SectorClusterCandidateReason.RouteSocketCompatible));
                Assert.That(candidate.Reasons, Does.Contain(SectorClusterCandidateReason.AccessCompatible));
                Assert.That(candidate.Reasons, Does.Contain(SectorClusterCandidateReason.FootprintFitsFreeGrid));
                Assert.That(candidate.Reasons, Does.Contain(SectorClusterCandidateReason.AvoidsFixedAnchor));
                Assert.That(candidate.Reasons, Does.Contain(SectorClusterCandidateReason.DensityWithinPolicy));
                Assert.That(candidate.ApprovedPlacements, Is.Not.Empty);
                Assert.That(candidate.FootprintCells.Count, Is.InRange(2, 5));
            }

            Assert.That(set.Candidates.SelectMany(value => value.Reasons), Does.Contain(SectorClusterCandidateReason.QuietPoolCompatible));
            Assert.That(set.Candidates.SelectMany(value => value.Reasons), Does.Contain(SectorClusterCandidateReason.SpecialAdjacencyCompatible));
            Assert.That(set.Candidates.SelectMany(value => value.Reasons), Does.Contain(SectorClusterCandidateReason.ConstraintLargeFirst));
            Assert.That(set.RejectedCountByReason[SectorClusterCandidateErrorCode.BiomeMismatch], Is.EqualTo(142));
            Assert.That(set.RejectedCountByReason[SectorClusterCandidateErrorCode.PacingMismatch], Is.EqualTo(34));
            Assert.That(set.RejectedCountByReason[SectorClusterCandidateErrorCode.SocketMismatch], Is.EqualTo(1));
            Assert.That(set.RejectedCountByReason[SectorClusterCandidateErrorCode.AccessMismatch], Is.EqualTo(1));
            Assert.That(set.RejectedCountByReason[SectorClusterCandidateErrorCode.DensityOutOfPolicy], Is.EqualTo(1));
            Assert.That(set.RejectedCountByReason[SectorClusterCandidateErrorCode.AnchorOverlap], Is.EqualTo(1));
        }

        [Test]
        public void CandidatesAvoidFixedAnchorsWithoutMutatingAnchors()
        {
            var fixture = Fixture.Create();
            var anchorDigest = fixture.AnchorPlan.CanonicalDigest;
            var sourceIdentities = fixture.AnchorPlan.Anchors.Select(value => value.SourceIdentity).ToArray();

            var set = fixture.BuildCandidates();

            foreach (var candidate in set.Candidates)
            foreach (var placement in candidate.ApprovedPlacements)
            {
                var blocking = BlockingCells(fixture.AnchorPlan, candidate.SectorIndex);
                Assert.That(placement.Cells.Any(blocking.Contains), Is.False,
                    candidate.ClusterId.Value + "/" + candidate.VariantId.Value);
            }
            Assert.That(fixture.AnchorPlan.CanonicalDigest, Is.EqualTo(anchorDigest));
            CollectionAssert.AreEqual(sourceIdentities, fixture.AnchorPlan.Anchors.Select(value => value.SourceIdentity));
            Assert.That(fixture.AnchorPlan.RouteIdentityBeforeDigest, Is.EqualTo(fixture.AnchorPlan.RouteIdentityAfterDigest));
            Assert.That(fixture.AnchorPlan.BoundaryIdentityBeforeDigest, Is.EqualTo(fixture.AnchorPlan.BoundaryIdentityAfterDigest));
            Assert.That(fixture.AnchorPlan.SiteIdentityBeforeDigest, Is.EqualTo(fixture.AnchorPlan.SiteIdentityAfterDigest));
            Assert.That(fixture.AnchorPlan.SpecialIdentityBeforeDigest, Is.EqualTo(fixture.AnchorPlan.SpecialIdentityAfterDigest));
        }

        [Test]
        public void PlacePublishesConstraintLargeFirstClusterPlacementPlan()
        {
            var fixture = Fixture.Create();
            var plan = fixture.BuildPlacements();

            Assert.That(plan.AcceptedPlacementCount, Is.EqualTo(9));
            Assert.That(plan.Map14_04HandoffReady, Is.True);
            CollectionAssert.AreEqual(new[]
            {
                "TC_REF_BOSS_GATE",
                "TC_REF_CORE_RESOURCE_RING",
                "TC_REF_FORGE_MACHINERY",
                "TC_REF_TRAVERSAL_BRIDGE",
                "TC_REF_NEIGHBOR_FLOW",
                "TC_REF_ACTIVITY_SHELL",
                "TC_REF_DISCOVERY_PASSAGE",
                "TC_REF_QUIET_BUFFER",
                "TC_REF_VILLAGE_APPROACH",
            }, plan.Placements.Select(value => value.ClusterId.Value));
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 1, 2, 2, 2, 2, 2 },
                plan.Placements.Select(value => value.ConstraintClass));
            Assert.That(plan.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(SectorClusterPlacementCanonicalDigest.Compute(plan), Is.EqualTo(plan.CanonicalDigest));
        }

        [Test]
        public void PlacedFootprintsStayInsideFourByFourGridAndDoNotOverlap()
        {
            var fixture = Fixture.Create();
            var plan = fixture.BuildPlacements();
            var occupied = new HashSet<string>(StringComparer.Ordinal);

            foreach (var placement in plan.Placements)
            {
                Assert.That(placement.Cells, Is.All.Matches<SectorClusterFootprintCell>(value =>
                    value.X >= 0 && value.X < 4 && value.Y >= 0 && value.Y < 4));
                Assert.That(placement.TileRects, Is.All.Matches<SectorFixedAnchorRect>(value => value.IsInside(48, 32)));
                Assert.That(placement.TileRects, Is.All.Matches<SectorFixedAnchorRect>(value => value.Width == 12 && value.Height == 8));
                foreach (var cell in placement.Cells)
                    Assert.That(occupied.Add(placement.SectorIndex + ":" + cell), Is.True);
            }

            Assert.That(plan.PlacedFootprintCellCount, Is.EqualTo(26));
            Assert.That(plan.HardAnchorFootprintCellCount, Is.EqualTo(21));
            Assert.That(plan.FreeFootprintCellCount, Is.EqualTo(97));
            Assert.That(plan.AnchorOverlapCount, Is.Zero);
            Assert.That(plan.PlacementOverlapCount, Is.Zero);
        }

        [Test]
        public void SpecialVillageOptionalAndActivityBoundariesRemainNonOwningWhereRequired()
        {
            var fixture = Fixture.Create();
            var plan = fixture.BuildPlacements();

            Assert.That(fixture.AnchorPlan.Count(SectorFixedAnchorKind.SpecialFootprint), Is.EqualTo(3));
            Assert.That(fixture.AnchorPlan.Count(SectorFixedAnchorKind.SpecialEntryReturn), Is.EqualTo(3));
            Assert.That(fixture.AnchorPlan.Count(SectorFixedAnchorKind.SpecialApronBuffer), Is.EqualTo(3));
            Assert.That(fixture.AnchorPlan.Count(SectorFixedAnchorKind.ReferenceOnlyMarker), Is.EqualTo(1));
            Assert.That(fixture.AnchorPlan.CountForSector(Fixture.Deferred), Is.Zero);
            Assert.That(fixture.AnchorPlan.CountForSector(Fixture.Activity), Is.Zero);
            Assert.That(plan.ActivityPlacementCount, Is.Zero);
            Assert.That(plan.EventPlacementCount, Is.Zero);
            Assert.That(plan.CanvasOwnershipClaimCount, Is.Zero);
            Assert.That(plan.GameplaySpawnCount, Is.Zero);
            Assert.That(plan.Placements.Count(value => value.SectorCoordinate.Equals(Fixture.Village)), Is.EqualTo(1));
            Assert.That(plan.Placements.Count(value => value.SectorCoordinate.Equals(Fixture.Deferred)), Is.EqualTo(1));
            Assert.That(plan.Placements.Count(value => value.SectorCoordinate.Equals(Fixture.Activity)), Is.EqualTo(1));
        }

        [Test]
        public void NoCandidateCollisionAndMutationClaimsFailAtomically()
        {
            var fixture = Fixture.Create();
            var duplicate = SectorClusterCandidateBuilder.Build(fixture.CandidateRequest(
                fixture.Catalog.Concat(new[] { fixture.Catalog[0] })));
            var allCollide = SectorClusterCandidateBuilder.Build(fixture.CandidateRequest(
                fixture.Catalog.Where(value => value.ClusterId.Value == "TC_REJECT_ANCHOR")));
            var mutation = SectorClusterCandidateBuilder.Build(new SectorClusterCandidateBuildRequest(
                fixture.Input, fixture.Assignments, fixture.AnchorPlan, fixture.Catalog,
                SectorClusterCandidateBuilder.ReferenceCandidatePublicationLabel,
                solverMutationCount: 1, randomDrawCount: 1, tileWriteCount: 1));
            var validSet = fixture.BuildCandidates();
            var placementMutation = SectorClusterPlacementPlanner.Place(new SectorClusterPlacementRequest(
                validSet, fixture.AnchorPlan, SectorClusterPlacementPlanner.ReferencePlacementPublicationLabel,
                solverMutationCount: 1, randomDrawCount: 1, tileWriteCount: 1));

            AssertAtomicFailure(duplicate, SectorClusterCandidateErrorCode.DuplicateCandidate);
            AssertAtomicFailure(allCollide, SectorClusterCandidateErrorCode.NoCandidateForSector, SectorClusterCandidateErrorCode.AnchorOverlap);
            AssertAtomicFailure(mutation, SectorClusterCandidateErrorCode.SolverMutationClaim,
                SectorClusterCandidateErrorCode.RngMutationClaim, SectorClusterCandidateErrorCode.TileMutationClaim);
            Assert.That(placementMutation.Success, Is.False);
            Assert.That(placementMutation.Plan, Is.Null);
            Assert.That(placementMutation.CanonicalDigest, Is.Empty);
            Assert.That(placementMutation.Errors.Select(value => value.Code), Does.Contain(SectorClusterCandidateErrorCode.SolverMutationClaim));
            Assert.That(placementMutation.Errors.Select(value => value.Code), Does.Contain(SectorClusterCandidateErrorCode.RngMutationClaim));
            Assert.That(placementMutation.Errors.Select(value => value.Code), Does.Contain(SectorClusterCandidateErrorCode.TileMutationClaim));
            CollectionAssert.AreEqual(placementMutation.Errors.OrderBy(value => value).ToArray(), placementMutation.Errors);
        }

        [Test]
        public void CandidateAndPlacementPublicationIsDeterministicAcrossRepeatReverseAndTurkishCulture()
        {
            var first = Fixture.Create();
            var reverse = Fixture.Create(true);
            var candidateDigest = first.BuildCandidates().CanonicalDigest;
            var placementDigest = first.BuildPlacements().CanonicalDigest;
            Assert.That(first.BuildCandidates().CanonicalDigest, Is.EqualTo(candidateDigest));
            Assert.That(first.BuildPlacements().CanonicalDigest, Is.EqualTo(placementDigest));
            Assert.That(reverse.BuildCandidates().CanonicalDigest, Is.EqualTo(candidateDigest));
            Assert.That(reverse.BuildPlacements().CanonicalDigest, Is.EqualTo(placementDigest));

            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var turkish = Fixture.Create(true);
                Assert.That(turkish.BuildCandidates().CanonicalDigest, Is.EqualTo(candidateDigest));
                Assert.That(turkish.BuildPlacements().CanonicalDigest, Is.EqualTo(placementDigest));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Test]
        public void BuildAndPlaceDoNotInvokeSpinePatternActivityRetryOrTileSystems()
        {
            var fixture = Fixture.Create();
            var candidateResult = SectorClusterCandidateBuilder.Build(fixture.CandidateRequest(fixture.Catalog));
            var placementResult = SectorClusterPlacementPlanner.Place(new SectorClusterPlacementRequest(
                candidateResult.CandidateSet, fixture.AnchorPlan, SectorClusterPlacementPlanner.ReferencePlacementPublicationLabel));
            var plan = placementResult.Plan;

            Assert.That(candidateResult.MutationCount + candidateResult.SolverInvocationCount
                        + candidateResult.RandomDrawCount + candidateResult.TileWriteCount, Is.Zero);
            Assert.That(placementResult.MutationCount + placementResult.SolverInvocationCount
                        + placementResult.RandomDrawCount + placementResult.TileWriteCount, Is.Zero);
            Assert.That(plan.RouteSpineInvocationCount, Is.Zero);
            Assert.That(plan.TraversalEnvelopeInvocationCount, Is.Zero);
            Assert.That(plan.MicroPatternRenderCount, Is.Zero);
            Assert.That(plan.ActivityPlacementCount, Is.Zero);
            Assert.That(plan.EventPlacementCount, Is.Zero);
            Assert.That(plan.RetryCount, Is.Zero);
            Assert.That(plan.SolverInvocationCount, Is.Zero);
            Assert.That(plan.RandomDrawCount, Is.Zero);
            Assert.That(plan.TileWriteCount, Is.Zero);
        }

        [Test]
        public void CandidateAndPlacementAccountingPublishesExactMap14_04HandoffEvidence()
        {
            var fixture = Fixture.Create();
            var set = fixture.BuildCandidates();
            var plan = fixture.BuildPlacements();

            Assert.That(fixture.Input.Sectors.Count, Is.EqualTo(9));
            Assert.That(fixture.AnchorPlan.Anchors.Count, Is.EqualTo(19));
            Assert.That(set.CandidateCount, Is.EqualTo(18));
            Assert.That(plan.AcceptedPlacementCount, Is.EqualTo(9));
            Assert.That(plan.RejectedCandidateCount, Is.EqualTo(189));
            Assert.That(plan.RejectedCountByReason[SectorClusterCandidateErrorCode.LowerRankedCandidate], Is.EqualTo(9));
            Assert.That(plan.Map14_04HandoffReady, Is.True);
            Assert.That(fixture.Assignments.Sum(value => value.RouteMutationCount + value.AccessMutationCount + value.SocketMutationCount), Is.Zero);

            TestContext.Out.WriteLine("MAP14_03_SECTORS=" + set.SectorCount);
            TestContext.Out.WriteLine("MAP14_03_CANDIDATES=" + set.CandidateCount);
            TestContext.Out.WriteLine("MAP14_03_ACCEPTED=" + plan.AcceptedPlacementCount);
            TestContext.Out.WriteLine("MAP14_03_REJECTED=" + plan.RejectedCandidateCount);
            TestContext.Out.WriteLine("MAP14_03_FOOTPRINT=" + plan.PlacedFootprintCellCount);
            TestContext.Out.WriteLine("MAP14_03_HARD_ANCHOR_CELLS=" + plan.HardAnchorFootprintCellCount);
            TestContext.Out.WriteLine("MAP14_03_FREE=" + plan.FreeFootprintCellCount);
            TestContext.Out.WriteLine("MAP14_03_CANDIDATE_DIGEST=" + set.CanonicalDigest);
            TestContext.Out.WriteLine("MAP14_03_PLACEMENT_DIGEST=" + plan.CanonicalDigest);
            foreach (var placement in plan.Placements)
                TestContext.Out.WriteLine("MAP14_03_SELECTED=" + placement.ClusterId.Value + "/" + placement.VariantId.Value);
        }

        private static void AssertAtomicFailure(
            SectorClusterCandidateBuildResult result,
            params SectorClusterCandidateErrorCode[] expected)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.CandidateSet, Is.Null);
            Assert.That(result.CanonicalDigest, Is.Empty);
            foreach (var code in expected) Assert.That(result.Errors.Select(value => value.Code), Does.Contain(code));
            CollectionAssert.AreEqual(result.Errors.OrderBy(value => value).ToArray(), result.Errors);
            Assert.That(result.MutationCount + result.SolverInvocationCount + result.RandomDrawCount + result.TileWriteCount, Is.Zero);
        }

        private static HashSet<SectorClusterFootprintCell> BlockingCells(SectorFixedAnchorPlan plan, int sectorIndex)
        {
            var result = new HashSet<SectorClusterFootprintCell>();
            foreach (var anchor in plan.Anchors.Where(value => value.SectorIndex == sectorIndex
                                                               && value.Kind != SectorFixedAnchorKind.ReferenceOnlyMarker
                                                               && value.Kind != SectorFixedAnchorKind.BoundaryWarning))
            for (var y = 0; y < 4; y++)
            for (var x = 0; x < 4; x++)
            {
                var cell = new SectorClusterFootprintCell(x, y);
                if (cell.ToTileRect().Overlaps(anchor.Rect)) result.Add(cell);
            }
            return result;
        }

        private static string Join(IEnumerable<SectorClusterCandidateError> errors)
            => string.Join(Environment.NewLine, errors.Select(value => value.ToString()));

        private sealed class Fixture
        {
            internal static readonly SectorCoord Plain = new SectorCoord(1, 1);
            internal static readonly SectorCoord Quiet = new SectorCoord(2, 1);
            internal static readonly SectorCoord Village = new SectorCoord(3, 1);
            internal static readonly SectorCoord Core = new SectorCoord(4, 1);
            internal static readonly SectorCoord Forge = new SectorCoord(5, 1);
            internal static readonly SectorCoord Boss = new SectorCoord(6, 1);
            internal static readonly SectorCoord Activity = new SectorCoord(7, 1);
            internal static readonly SectorCoord Deferred = new SectorCoord(8, 1);
            internal static readonly SectorCoord Neighbor = new SectorCoord(9, 1);

            private Fixture(
                SectorPlannerInput input,
                IReadOnlyList<SectorPacingAssignment> assignments,
                SectorFixedAnchorPlan anchorPlan,
                IReadOnlyList<SectorClusterSourceProjection> catalog)
            {
                Input = input;
                Assignments = assignments;
                AnchorPlan = anchorPlan;
                Catalog = catalog;
            }

            internal SectorPlannerInput Input { get; }
            internal IReadOnlyList<SectorPacingAssignment> Assignments { get; }
            internal SectorFixedAnchorPlan AnchorPlan { get; }
            internal IReadOnlyList<SectorClusterSourceProjection> Catalog { get; }

            internal static Fixture Create(bool reverse = false)
            {
                var sectors = CreateSectors();
                if (reverse) sectors.Reverse();
                var authority = SectorPlannerAuthorityDigestSnapshot.CaptureCurrentPublicAuthorities(
                    Digest('a'), Digest('b'), 24, Digest('c'), 16,
                    Digest('d'), 7, Digest('e'), 5, Digest('f'));
                var inputResult = SectorPlannerInputBuilder.Build(new SectorPlannerInputRequest(
                    sectors, authority, SectorPlannerInputBuilder.ReferencePublicationLabel));
                if (!inputResult.Success) throw new InvalidOperationException(string.Join("\n", inputResult.Errors));

                var assignments = SectorPacingRolePlanner.Assign(inputResult.Input).ToList();
                var projections = CreateAnchors();
                if (reverse)
                {
                    assignments.Reverse();
                    projections.Reverse();
                }
                var anchorResult = SectorFixedAnchorPlanner.Build(new SectorFixedAnchorBuildRequest(
                    inputResult.Input, assignments, projections, SectorFixedAnchorPlanner.ReferencePublicationLabel));
                if (!anchorResult.Success) throw new InvalidOperationException(string.Join("\n", anchorResult.Errors));

                var catalog = CreateCatalog();
                if (reverse) catalog.Reverse();
                return new Fixture(inputResult.Input, assignments, anchorResult.Plan, catalog);
            }

            internal SectorClusterCandidateBuildRequest CandidateRequest(IEnumerable<SectorClusterSourceProjection> catalog)
            {
                return new SectorClusterCandidateBuildRequest(
                    Input, Assignments, AnchorPlan, catalog,
                    SectorClusterCandidateBuilder.ReferenceCandidatePublicationLabel);
            }

            internal SectorClusterCandidateSet BuildCandidates()
            {
                var result = SectorClusterCandidateBuilder.Build(CandidateRequest(Catalog));
                if (!result.Success) throw new InvalidOperationException(Join(result.Errors));
                return result.CandidateSet;
            }

            internal SectorClusterPlacementPlan BuildPlacements()
            {
                var result = SectorClusterPlacementPlanner.Place(new SectorClusterPlacementRequest(
                    BuildCandidates(), AnchorPlan, SectorClusterPlacementPlanner.ReferencePlacementPublicationLabel));
                if (!result.Success) throw new InvalidOperationException(string.Join("\n", result.Errors));
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
                            SectorPlannerSpecialRegionBinding.ReferenceOnly, "FP_VILLAGE_REFERENCE", false, false, false),
                        ordinal: 2, optionalDistance: 0),
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
                        optional: new[] { new SectorPlannerOptionalRegionSnapshot("REGION_MERCHANT", SectorPlannerSpecialRegionKind.Merchant, true, true, false) },
                        optionalDistance: 1),
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
                SectorCoord coordinate,
                MoonpalaceBiomeId biome,
                IEnumerable<PacingRole> roles,
                SectorPlannerRouteSnapshot route = null,
                IEnumerable<SectorPlannerBoundarySnapshot> boundaries = null,
                IEnumerable<SectorPlannerSiteSnapshot> sites = null,
                SectorPlannerSpecialRegionSnapshot special = null,
                IEnumerable<SectorPlannerOptionalRegionSnapshot> optional = null,
                IEnumerable<SectorPlannerNeighborSnapshot> neighbors = null,
                bool quiet = false,
                bool activity = false,
                bool eventAvailable = false,
                int ordinal = 4,
                int mandatoryDistance = 2,
                int optionalDistance = 3)
            {
                return new SectorPlannerSectorSnapshot(
                    coordinate, (coordinate.Y * 13) + coordinate.X, 48, 32,
                    new SectorPlannerBiomeSnapshot("PATCH_" + coordinate.X, biome.ToString()),
                    route ?? new SectorPlannerRouteSnapshot(1, AccessClass.MandatoryNoTool, Array.Empty<string>(), false, false),
                    boundaries, sites, special ?? SectorPlannerSpecialRegionSnapshot.None,
                    optional, neighbors,
                    new SectorPlannerWorldProgressSnapshot(ordinal, "CHAPTER_REFERENCE", "BRANCH_REFERENCE", mandatoryDistance, optionalDistance),
                    roles, quiet, activity, eventAvailable);
            }

            private static SectorPlannerSpecialRegionSnapshot Mandatory(
                string id, SectorPlannerSpecialRegionKind kind, string footprint)
                => new SectorPlannerSpecialRegionSnapshot(id, kind, SectorPlannerSpecialRegionBinding.ReservedMandatory,
                    footprint, true, true, true);

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
                        new SectorFixedAnchorRect(47, 4, 1, 4), "BOUNDARY_CRATER_ROOT", SectorPlannerSide.Right,
                        true, "BOUNDARY_CRATER_ROOT"),
                    new SectorFixedAnchorProjection("ANCHOR_BOUNDARY_WARNING", Plain, SectorFixedAnchorKind.BoundaryWarning,
                        SectorFixedAnchorSource.BoundarySnapshot, SectorFixedAnchorPriority.BoundaryWarning,
                        new SectorFixedAnchorRect(47, 4, 1, 4), "BOUNDARY_CRATER_ROOT", SectorPlannerSide.Right,
                        true, "BOUNDARY_CRATER_ROOT"),
                    new SectorFixedAnchorProjection("ANCHOR_VILLAGE_REFERENCE", Village, SectorFixedAnchorKind.ReferenceOnlyMarker,
                        SectorFixedAnchorSource.SpecialRegionSnapshot, SectorFixedAnchorPriority.ReferenceOnly,
                        new SectorFixedAnchorRect(23, 15, 1, 1), "REGION_VILLAGE"),
                };
                AddSpecial(result, Core, "CORE", "REGION_CORE", "SITE_CORE");
                AddSpecial(result, Forge, "FORGE", "REGION_FORGE", "SITE_FORGE");
                AddSpecial(result, Boss, "BOSS", "REGION_BOSS", "SITE_BOSS");
                return result;
            }

            private static SectorFixedAnchorProjection RouteAnchor(
                string anchorId, string sourceId, SectorPlannerSide side, SectorFixedAnchorRect rect)
                => new SectorFixedAnchorProjection(anchorId, Plain, SectorFixedAnchorKind.ExternalRouteSocket,
                    SectorFixedAnchorSource.RouteSnapshot, SectorFixedAnchorPriority.ExternalRouteSocket,
                    rect, sourceId, side);

            private static void AddSpecial(
                ICollection<SectorFixedAnchorProjection> result,
                SectorCoord coordinate,
                string token,
                string regionId,
                string siteId)
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
                    SectorFixedAnchorPriority.SpecialReservation, new SectorFixedAnchorRect(20, 22, 4, 2), siteId,
                    placedOwnershipClaim: true));
            }

            private static List<SectorClusterSourceProjection> CreateCatalog()
            {
                var mandatory = new[] { AccessClass.MandatoryNoTool };
                var route1 = new[] { 1 };
                var plainSockets = new[] { "SOCKET_L", "SOCKET_R", "SOCKET_U", "SOCKET_D" };
                var result = new List<SectorClusterSourceProjection>
                {
                    Source("TC_REF_TRAVERSAL_BRIDGE", "SPINE_TRAVERSAL_R0", MoonpalaceBiomeId.MoonCrater, PacingRole.Traversal, route1, mandatory, plainSockets, H2(), Origins(2, 1), false, false, 10),
                    Source("TC_REF_TRAVERSAL_RECOVERY", "SPINE_RECOVERY_R0", MoonpalaceBiomeId.MoonCrater, PacingRole.Recovery, route1, mandatory, plainSockets, V2(), Origins(1, 2), false, false, 11),
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
                    Source("TC_REJECT_SOCKET", "SPINE_REJECT_SOCKET", MoonpalaceBiomeId.MoonCrater, PacingRole.Traversal, new[] { 2 }, mandatory, plainSockets, H2(), Origins(2, 1), false, false, 100),
                    Source("TC_REJECT_ACCESS", "SPINE_REJECT_ACCESS", MoonpalaceBiomeId.MoonCrater, PacingRole.Quiet, route1, new[] { AccessClass.OptionalTool }, null, H2(), Origins(2, 1), true, false, 101),
                    Source("TC_REJECT_DENSITY", "SPINE_REJECT_DENSITY", MoonpalaceBiomeId.CassiaRoot, PacingRole.Resource, route1, mandatory, null, H2(), Origins(2, 1), false, true, 102, minimumDensity: 3),
                    Source("TC_REJECT_ANCHOR", "SPINE_REJECT_ANCHOR", MoonpalaceBiomeId.MoonDough, PacingRole.Boss, route1, mandatory, null, Boss5(), new[] { new SectorClusterFootprintCell(0, 1) }, false, true, 103),
                };
                return result;
            }

            private static SectorClusterSourceProjection Source(
                string clusterId,
                string variantId,
                MoonpalaceBiomeId biome,
                PacingRole pacing,
                IEnumerable<int> routes,
                IEnumerable<AccessClass> access,
                IEnumerable<string> sockets,
                IEnumerable<SectorClusterFootprintCell> cells,
                IEnumerable<SectorClusterFootprintCell> origins,
                bool quiet,
                bool special,
                int catalogOrder,
                ClusterFootprintTransform transform = ClusterFootprintTransform.R0,
                int minimumDensity = 2)
            {
                return new SectorClusterSourceProjection(
                    new TerrainClusterId(clusterId), new SpineVariantId(variantId), transform, biome,
                    new[] { pacing }, routes, access, sockets, cells, origins,
                    minimumDensity, 5, quiet, special, catalogOrder, 0);
            }

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
                for (var y = 0; y <= 4 - height; y++)
                for (var x = 0; x <= 4 - width; x++) result.Add(Cell(x, y));
                return result.ToArray();
            }

            private static string Digest(char value) => new string(value, 64);
        }
    }
}
