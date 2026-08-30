using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.SectorPlanning;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.SectorPlanning
{
    [TestFixture]
    [Category("MAP14_02")]
    public sealed class SectorFixedAnchorPlannerTests
    {
        private AnchorFixtureSet fixtures;

        [SetUp]
        public void SetUp()
        {
            fixtures = AnchorFixtureSet.Create();
        }

        [Test]
        public void BuildPublishesCanonicalFixedAnchorPlanFromPlannerInput()
        {
            var mutable = fixtures.ValidProjections.ToList();
            var request = fixtures.Request(mutable);
            mutable.Clear();

            var result = SectorFixedAnchorPlanner.Build(request);

            Assert.That(result.Success, Is.True, JoinErrors(result));
            Assert.That(result.Plan.PublicationLabel, Is.EqualTo("REFERENCE ANCHOR PLAN"));
            Assert.That(result.Plan.SectorCount, Is.EqualTo(9));
            Assert.That(result.Plan.Anchors.Count, Is.EqualTo(19));
            Assert.That(result.Plan.Anchors.All(value => value.Rect.IsInside(48, 32)), Is.True);
            Assert.That(result.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(result.CanonicalDigest, Is.EqualTo(SectorFixedAnchorCanonicalDigest.Compute(result.Plan)));
            Assert.That(result.Plan.CollisionCount, Is.Zero);
            Assert.That(result.Plan.CompatibleOverlapCount, Is.EqualTo(1));
            Assert.That(result.Plan.Map14_03HandoffReady, Is.True);
            Assert.That(() => ((IList<SectorFixedAnchor>)result.Plan.Anchors).Add(result.Plan.Anchors[0]),
                Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public void ExternalRouteSocketsAreSideAlignedAndDoNotMutateRouteAccess()
        {
            var input = fixtures.Input;
            var routeType = fixtures.PlainTraversalBoundarySector.Route.RouteType;
            var access = fixtures.PlainTraversalBoundarySector.Route.AccessClass;
            var sockets = fixtures.PlainTraversalBoundarySector.Route.ExternalSockets.ToArray();

            var plan = fixtures.BuildValid().Plan;
            var anchors = plan.Anchors.Where(value => value.Kind == SectorFixedAnchorKind.ExternalRouteSocket).ToArray();

            Assert.That(anchors.Length, Is.EqualTo(4));
            Assert.That(anchors.Select(value => value.Side.Value), Is.EquivalentTo(new[]
            {
                SectorPlannerSide.Left, SectorPlannerSide.Right, SectorPlannerSide.Up, SectorPlannerSide.Down,
            }));
            Assert.That(anchors.All(value => value.Rect.TouchesOnly(value.Side.Value, 48, 32)), Is.True);
            Assert.That(anchors.All(value => value.Source == SectorFixedAnchorSource.RouteSnapshot), Is.True);
            Assert.That(anchors.All(value => value.SourceIdentity.Contains("|" + routeType + "|" + access + "|")), Is.True);
            Assert.That(fixtures.PlainTraversalBoundarySector.Route.RouteType, Is.EqualTo(routeType));
            Assert.That(fixtures.PlainTraversalBoundarySector.Route.AccessClass, Is.EqualTo(access));
            Assert.That(fixtures.PlainTraversalBoundarySector.Route.ExternalSockets, Is.EqualTo(sockets));
            Assert.That(plan.RouteIdentityAfterDigest, Is.EqualTo(plan.RouteIdentityBeforeDigest));
            Assert.That(plan.RouteMutationCount + plan.AccessMutationCount + plan.SocketMutationCount, Is.Zero);
            Assert.That(input.CanonicalDigest, Is.EqualTo(plan.PlannerInputDigest));
        }

        [Test]
        public void BoundaryAnchorsPreservePairCandidateWarningEvidence()
        {
            var plan = fixtures.BuildValid().Plan;
            var anchors = plan.Anchors.Where(value => value.Source == SectorFixedAnchorSource.BoundarySnapshot).ToArray();

            Assert.That(anchors.Length, Is.EqualTo(2));
            Assert.That(anchors.Select(value => value.Kind), Is.EquivalentTo(new[]
            {
                SectorFixedAnchorKind.BoundaryFixedSlice,
                SectorFixedAnchorKind.BoundaryWarning,
            }));
            Assert.That(anchors.Select(value => value.SourceIdentity).Distinct().Count(), Is.EqualTo(1));
            Assert.That(anchors[0].SourceIdentity, Does.Contain("BP_PLAIN_R"));
            Assert.That(anchors[0].SourceIdentity, Does.Contain("BC_PLAIN_R_01"));
            Assert.That(anchors[0].SourceIdentity, Does.EndWith("|1"));
            Assert.That(anchors.All(value => value.Side == SectorPlannerSide.Right), Is.True);
            Assert.That(plan.CompatibleOverlapCount, Is.EqualTo(1));
            Assert.That(plan.CollisionCount, Is.Zero);
            Assert.That(plan.BoundaryIdentityAfterDigest, Is.EqualTo(plan.BoundaryIdentityBeforeDigest));
            Assert.That(plan.BoundaryMutationCount, Is.Zero);
        }

        [Test]
        public void SpecialAnchorsReserveFootprintEntryReturnBufferBeforeClusters()
        {
            var plan = fixtures.BuildValid().Plan;
            var mandatory = new[]
            {
                fixtures.CoreResourceSector,
                fixtures.ForgeLandmarkSector,
                fixtures.BossGateSector,
            };

            foreach (var sector in mandatory)
            {
                var anchors = plan.Anchors.Where(value => value.SectorIndex == sector.SectorIndex).ToArray();
                Assert.That(anchors.Length, Is.EqualTo(4));
                Assert.That(anchors.Select(value => value.Kind), Is.EquivalentTo(new[]
                {
                    SectorFixedAnchorKind.SpecialFootprint,
                    SectorFixedAnchorKind.SpecialEntryReturn,
                    SectorFixedAnchorKind.SpecialApronBuffer,
                    SectorFixedAnchorKind.SiteReservation,
                }));
                Assert.That(anchors.All(value => value.PlacedOwnershipClaim), Is.True);
            }

            Assert.That(plan.Count(SectorFixedAnchorKind.SpecialFootprint), Is.EqualTo(3));
            Assert.That(plan.Count(SectorFixedAnchorKind.SpecialEntryReturn), Is.EqualTo(3));
            Assert.That(plan.Count(SectorFixedAnchorKind.SpecialApronBuffer), Is.EqualTo(3));
            Assert.That(plan.Count(SectorFixedAnchorKind.SiteReservation), Is.EqualTo(3));
            Assert.That(plan.SpecialIdentityAfterDigest, Is.EqualTo(plan.SpecialIdentityBeforeDigest));
            Assert.That(plan.SiteIdentityAfterDigest, Is.EqualTo(plan.SiteIdentityBeforeDigest));
            Assert.That(plan.ClusterCandidateCount + plan.ClusterPlacementCount, Is.Zero);
        }

        [Test]
        public void VillageReferenceAndDeferredOptionalDoNotBecomePlacedProgressionBlockers()
        {
            var plan = fixtures.BuildValid().Plan;
            var village = plan.Anchors.Where(value =>
                value.SectorIndex == fixtures.VillageReferenceSector.SectorIndex).ToArray();
            var deferred = plan.Anchors.Where(value =>
                value.SectorIndex == fixtures.DeferredOptionalSector.SectorIndex).ToArray();

            Assert.That(village.Length, Is.EqualTo(1));
            Assert.That(village[0].Kind, Is.EqualTo(SectorFixedAnchorKind.ReferenceOnlyMarker));
            Assert.That(village[0].PlacedOwnershipClaim, Is.False);
            Assert.That(village[0].ProgressionBlockerClaim, Is.False);
            Assert.That(fixtures.VillageReferenceSector.SpecialRegion.MandatoryProgressionDependency, Is.False);
            Assert.That(deferred, Is.Empty);
            Assert.That(fixtures.DeferredOptionalSector.SpecialRegion.Binding,
                Is.EqualTo(SectorPlannerSpecialRegionBinding.DeferredOptionalLocal));
            Assert.That(fixtures.DeferredOptionalSector.SpecialRegion.PlacedOwnershipClaim, Is.False);
            Assert.That(fixtures.DeferredOptionalSector.OptionalRegions.Single().PlacedOwnershipClaim, Is.False);
        }

        [Test]
        public void ActivityEventAndNeighborFactsDoNotCreateAnchors()
        {
            var plan = fixtures.BuildValid().Plan;

            Assert.That(plan.CountForSector(fixtures.QuietBufferSector.Coordinate), Is.Zero);
            Assert.That(plan.CountForSector(fixtures.ActivityCompatibleSector.Coordinate), Is.Zero);
            Assert.That(plan.CountForSector(fixtures.DeferredOptionalSector.Coordinate), Is.Zero);
            Assert.That(plan.CountForSector(fixtures.NeighborInfluencedSector.Coordinate), Is.Zero);
            Assert.That(plan.Count(SectorFixedAnchorSource.PacingAssignment), Is.Zero);
            Assert.That(plan.Count(SectorFixedAnchorSource.OptionalRegionSnapshot), Is.Zero);
            Assert.That(plan.ActivityPlacementCount, Is.Zero);
            Assert.That(plan.EventMarkerCount, Is.Zero);
            Assert.That(plan.GameplaySpawnCount, Is.Zero);
            Assert.That(plan.PathEdgeCount, Is.Zero);
        }

        [Test]
        public void IncompatibleOverlapAndOutOfBoundsFailAtomically()
        {
            var projections = fixtures.ValidProjections.ToList();
            projections.Add(new SectorFixedAnchorProjection(
                "INVALID_OUT_OF_BOUNDS",
                fixtures.PlainTraversalBoundarySector.Coordinate,
                SectorFixedAnchorKind.ExternalRouteSocket,
                SectorFixedAnchorSource.RouteSnapshot,
                SectorFixedAnchorPriority.ExternalRouteSocket,
                new SectorFixedAnchorRect(-1, 14, 1, 4),
                "PLAIN_L",
                SectorPlannerSide.Left));
            projections.Add(new SectorFixedAnchorProjection(
                "INVALID_INCOMPATIBLE_OVERLAP",
                fixtures.PlainTraversalBoundarySector.Coordinate,
                SectorFixedAnchorKind.ExternalRouteSocket,
                SectorFixedAnchorSource.RouteSnapshot,
                SectorFixedAnchorPriority.ExternalRouteSocket,
                new SectorFixedAnchorRect(0, 14, 1, 4),
                "PLAIN_R",
                SectorPlannerSide.Left));

            var result = SectorFixedAnchorPlanner.Build(fixtures.Request(projections));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(SectorFixedAnchorErrorCode.AnchorOutOfBounds));
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(SectorFixedAnchorErrorCode.IncompatibleOverlap));
            Assert.That(result.Errors, Is.EqualTo(result.Errors.Distinct().OrderBy(value => value).ToArray()));
            Assert.That(result.MutationCount + result.SolverInvocationCount + result.RandomDrawCount + result.TileWriteCount, Is.Zero);
        }

        [Test]
        public void AssignmentMismatchAndMutationClaimsFailAtomically()
        {
            var missing = SectorFixedAnchorPlanner.Build(new SectorFixedAnchorBuildRequest(
                fixtures.Input,
                Array.Empty<SectorPacingAssignment>(),
                fixtures.ValidProjections,
                SectorFixedAnchorPlanner.ReferencePublicationLabel));
            var mismatchedAssignments = fixtures.Assignments.Take(8)
                .Concat(fixtures.Assignments.Take(1)).ToArray();
            var mismatch = SectorFixedAnchorPlanner.Build(new SectorFixedAnchorBuildRequest(
                fixtures.Input,
                mismatchedAssignments,
                fixtures.ValidProjections,
                SectorFixedAnchorPlanner.ReferencePublicationLabel));
            var mutation = SectorFixedAnchorPlanner.Build(new SectorFixedAnchorBuildRequest(
                fixtures.Input,
                fixtures.Assignments,
                fixtures.ValidProjections,
                SectorFixedAnchorPlanner.ReferencePublicationLabel,
                routeAccessMutationClaim: true,
                boundaryMutationClaim: true,
                siteMutationClaim: true,
                specialMutationClaim: true,
                solverMutationCount: 1,
                randomDrawCount: 1,
                tileWriteCount: 1,
                canvasMutationCount: 1,
                assetMutationCount: 1));

            Assert.That(new[] { missing, mismatch, mutation }.All(value => !value.Success), Is.True);
            Assert.That(new[] { missing, mismatch, mutation }.All(value => value.Plan == null && value.CanonicalDigest == string.Empty), Is.True);
            Assert.That(missing.Errors.Select(value => value.Code), Does.Contain(SectorFixedAnchorErrorCode.MissingPacingAssignment));
            Assert.That(mismatch.Errors.Select(value => value.Code), Does.Contain(SectorFixedAnchorErrorCode.SectorMismatch));
            Assert.That(mutation.Errors.Select(value => value.Code), Does.Contain(SectorFixedAnchorErrorCode.RouteAccessMutationClaim));
            Assert.That(mutation.Errors.Select(value => value.Code), Does.Contain(SectorFixedAnchorErrorCode.BoundaryMutationClaim));
            Assert.That(mutation.Errors.Select(value => value.Code), Does.Contain(SectorFixedAnchorErrorCode.SiteMutationClaim));
            Assert.That(mutation.Errors.Select(value => value.Code), Does.Contain(SectorFixedAnchorErrorCode.SpecialMutationClaim));
            Assert.That(mutation.Errors.Select(value => value.Code), Does.Contain(SectorFixedAnchorErrorCode.SolverMutationClaim));
        }

        [Test]
        public void DeferredPlacedAndReferenceLiveClaimsFailAtomically()
        {
            var projections = fixtures.ValidProjections.ToList();
            projections.Add(new SectorFixedAnchorProjection(
                "INVALID_DEFERRED_PLACED",
                fixtures.DeferredOptionalSector.Coordinate,
                SectorFixedAnchorKind.SpecialFootprint,
                SectorFixedAnchorSource.SpecialRegionSnapshot,
                SectorFixedAnchorPriority.SpecialReservation,
                new SectorFixedAnchorRect(18, 12, 12, 8),
                fixtures.DeferredOptionalSector.SpecialRegion.RegionId,
                placedOwnershipClaim: true));
            projections.Add(new SectorFixedAnchorProjection(
                "INVALID_VILLAGE_LIVE",
                fixtures.VillageReferenceSector.Coordinate,
                SectorFixedAnchorKind.ReferenceOnlyMarker,
                SectorFixedAnchorSource.SpecialRegionSnapshot,
                SectorFixedAnchorPriority.ReferenceOnly,
                new SectorFixedAnchorRect(24, 15, 1, 1),
                fixtures.VillageReferenceSector.SpecialRegion.RegionId,
                placedOwnershipClaim: true,
                progressionBlockerClaim: true));

            var result = SectorFixedAnchorPlanner.Build(fixtures.Request(projections));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(SectorFixedAnchorErrorCode.DeferredPlacedClaim));
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(SectorFixedAnchorErrorCode.ReferenceLiveClaim));
            Assert.That(result.MutationCount, Is.Zero);
        }

        [Test]
        public void AnchorPublicationIsDeterministicAcrossRepeatReverseAndTurkishCulture()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var first = fixtures.BuildValid().Plan;
                var repeat = fixtures.BuildValid().Plan;
                var reverse = SectorFixedAnchorPlanner.Build(new SectorFixedAnchorBuildRequest(
                    fixtures.Input,
                    fixtures.Assignments.Reverse(),
                    fixtures.ValidProjections.Reverse(),
                    SectorFixedAnchorPlanner.ReferencePublicationLabel)).Plan;
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var turkishFixtures = AnchorFixtureSet.Create();
                var turkish = SectorFixedAnchorPlanner.Build(new SectorFixedAnchorBuildRequest(
                    turkishFixtures.Input,
                    turkishFixtures.Assignments.Reverse(),
                    turkishFixtures.ValidProjections.Reverse(),
                    SectorFixedAnchorPlanner.ReferencePublicationLabel)).Plan;

                Assert.That(repeat.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));
                Assert.That(reverse.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));
                Assert.That(turkish.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));
                Assert.That(reverse.Anchors.Select(value => value.AnchorId),
                    Is.EqualTo(first.Anchors.Select(value => value.AnchorId)));
                Assert.That(turkish.Anchors.Select(value => value.AnchorId),
                    Is.EqualTo(first.Anchors.Select(value => value.AnchorId)));

                TestContext.WriteLine("fixtureMatrix=10 sectors=9 anchors=19 collision=0 compatibleOverlap=1");
                TestContext.WriteLine("planDigest=" + first.CanonicalDigest
                                      + " assignmentDigest=" + first.AssignmentDigest);
                foreach (SectorFixedAnchorKind kind in Enum.GetValues(typeof(SectorFixedAnchorKind)))
                    TestContext.WriteLine("kind." + kind + "=" + first.Count(kind));
                foreach (SectorFixedAnchorSource source in Enum.GetValues(typeof(SectorFixedAnchorSource)))
                    TestContext.WriteLine("source." + source + "=" + first.Count(source));
                foreach (SectorFixedAnchorPriority priority in Enum.GetValues(typeof(SectorFixedAnchorPriority)))
                    TestContext.WriteLine("priority." + priority + "=" + first.Count(priority));
                TestContext.WriteLine("PlainTraversalBoundarySector=" + first.CountForSector(fixtures.PlainTraversalBoundarySector.Coordinate));
                TestContext.WriteLine("QuietBufferSector=" + first.CountForSector(fixtures.QuietBufferSector.Coordinate));
                TestContext.WriteLine("VillageReferenceSector=" + first.CountForSector(fixtures.VillageReferenceSector.Coordinate));
                TestContext.WriteLine("CoreResourceSector=" + first.CountForSector(fixtures.CoreResourceSector.Coordinate));
                TestContext.WriteLine("ForgeLandmarkSector=" + first.CountForSector(fixtures.ForgeLandmarkSector.Coordinate));
                TestContext.WriteLine("BossGateSector=" + first.CountForSector(fixtures.BossGateSector.Coordinate));
                TestContext.WriteLine("ActivityCompatibleSector=" + first.CountForSector(fixtures.ActivityCompatibleSector.Coordinate));
                TestContext.WriteLine("DeferredOptionalSector=" + first.CountForSector(fixtures.DeferredOptionalSector.Coordinate));
                TestContext.WriteLine("NeighborInfluencedSector=" + first.CountForSector(fixtures.NeighborInfluencedSector.Coordinate));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        private static string JoinErrors(SectorFixedAnchorBuildResult result)
            => string.Join("\n", result.Errors.Select(value => value.ToString()));
    }

    internal sealed class AnchorFixtureSet
    {
        private const string MicroPatternDigest = "6a5aefd2eb368348d594158cc3f14e94d0ea509ea2cdd207a7715e8da80d19ac";
        private const string TerrainClusterDigest = "9d26786af477731d57503f16cc899210da6636f48dfb0542791e8fa591bd3bf7";
        private const string ActivityDigest = "3ef83fae74d935a2469ab587414d0498cb423609b171d1c7633423e297318c3a";
        private const string EventDigest = "2d2878f62605927a7b70a405a06079b3ebad7767e3bd7db9b6b2431177ea95a0";
        private const string SpecialRegionAuditDigest = "a7ab6fd571425c4c8e64d7eecad5dd246a3d9a8a08044801800948fc2fa03e4e";

        private AnchorFixtureSet()
        {
            var authority = SectorPlannerAuthorityDigestSnapshot.CaptureCurrentPublicAuthorities(
                Digest("MAP00_08_REFERENCE_SUMMARY"), MicroPatternDigest, 24,
                TerrainClusterDigest, 16, ActivityDigest, 7, EventDigest, 5,
                SpecialRegionAuditDigest);

            PlainTraversalBoundarySector = Sector(
                0, 0,
                new[] { PacingRole.Traversal, PacingRole.Recovery },
                sockets: new[] { "PLAIN_L", "PLAIN_R", "PLAIN_U", "PLAIN_D" },
                boundaries: new[]
                {
                    new SectorPlannerBoundarySnapshot(
                        SectorPlannerSide.Right, "BP_PLAIN_R", "BC_PLAIN_R_01", 1),
                },
                highRoute: true,
                recoveryNeeded: true);
            QuietBufferSector = Sector(1, 0, new[] { PacingRole.Quiet }, quiet: true);
            VillageReferenceSector = Sector(
                2, 0,
                new[] { PacingRole.Safe, PacingRole.Landmark },
                special: new SectorPlannerSpecialRegionSnapshot(
                    "SR_VILLAGE_REFERENCE_01", SectorPlannerSpecialRegionKind.Village,
                    SectorPlannerSpecialRegionBinding.ReferenceOnly, "VILLAGE_REFERENCE_SHELL",
                    false, false, false),
                progress: Progress(2, 4, 0));
            CoreResourceSector = Sector(
                3, 0,
                new[] { PacingRole.Resource },
                sites: new[] { new SectorPlannerSiteSnapshot("SITE_CORE_01", "CORE_RESOURCE", "RES_CORE_01", true) },
                special: Mandatory("SR_CORE_RESOURCE_01", SectorPlannerSpecialRegionKind.CoreResource, "FP_CORE_01"),
                progress: Progress(5, 0, 3));
            ForgeLandmarkSector = Sector(
                4, 0,
                new[] { PacingRole.Landmark, PacingRole.Machinery },
                sites: new[] { new SectorPlannerSiteSnapshot("SITE_FORGE_01", "FORGE_LANDMARK", "RES_FORGE_01", true) },
                special: Mandatory("SR_FORGE_01", SectorPlannerSpecialRegionKind.Forge, "FP_FORGE_01"),
                progress: Progress(7, 0, 3));
            BossGateSector = Sector(
                5, 0,
                new[] { PacingRole.Boss },
                sites: new[] { new SectorPlannerSiteSnapshot("SITE_BOSS_01", "BOSS_GATE", "RES_BOSS_01", true) },
                special: Mandatory("SR_BOSS_01", SectorPlannerSpecialRegionKind.Boss, "FP_BOSS_01"),
                progress: Progress(10, 0, 5));
            ActivityCompatibleSector = Sector(
                6, 0,
                new[] { PacingRole.Activity },
                activityAvailable: true,
                eventAvailable: true,
                progress: Progress(5, 2, 2));
            DeferredOptionalSector = Sector(
                7, 0,
                new[] { PacingRole.Discovery },
                special: new SectorPlannerSpecialRegionSnapshot(
                    "SR_MERCHANT_DEFERRED_01", SectorPlannerSpecialRegionKind.Merchant,
                    SectorPlannerSpecialRegionBinding.DeferredOptionalLocal, string.Empty,
                    false, false, false),
                optional: new[]
                {
                    new SectorPlannerOptionalRegionSnapshot(
                        "SR_MERCHANT_DEFERRED_01", SectorPlannerSpecialRegionKind.Merchant,
                        true, true, false),
                },
                progress: Progress(4, 2, 1));
            NeighborInfluencedSector = Sector(
                8, 5,
                new[] { PacingRole.Traversal },
                neighbors: new[]
                {
                    Neighbor(SectorPlannerSide.Left, 7, 5, "N_L"),
                    Neighbor(SectorPlannerSide.Right, 9, 5, "N_R"),
                    Neighbor(SectorPlannerSide.Up, 8, 4, "N_U"),
                    Neighbor(SectorPlannerSide.Down, 8, 6, "N_D"),
                },
                progress: Progress(6, 3, 2));

            var sectors = new[]
            {
                PlainTraversalBoundarySector, QuietBufferSector, VillageReferenceSector,
                CoreResourceSector, ForgeLandmarkSector, BossGateSector,
                ActivityCompatibleSector, DeferredOptionalSector, NeighborInfluencedSector,
            };
            var inputResult = SectorPlannerInputBuilder.Build(new SectorPlannerInputRequest(
                sectors, authority, SectorPlannerInputBuilder.ReferencePublicationLabel));
            if (!inputResult.Success)
                throw new InvalidOperationException(string.Join("\n", inputResult.Errors));
            Input = inputResult.Input;
            Assignments = SectorPacingRolePlanner.Assign(Input);

            var projections = new List<SectorFixedAnchorProjection>();
            projections.Add(Route("ANCHOR_ROUTE_L", PlainTraversalBoundarySector, "PLAIN_L", SectorPlannerSide.Left, new SectorFixedAnchorRect(0, 14, 1, 4)));
            projections.Add(Route("ANCHOR_ROUTE_R", PlainTraversalBoundarySector, "PLAIN_R", SectorPlannerSide.Right, new SectorFixedAnchorRect(47, 14, 1, 4)));
            projections.Add(Route("ANCHOR_ROUTE_U", PlainTraversalBoundarySector, "PLAIN_U", SectorPlannerSide.Up, new SectorFixedAnchorRect(22, 0, 4, 1)));
            projections.Add(Route("ANCHOR_ROUTE_D", PlainTraversalBoundarySector, "PLAIN_D", SectorPlannerSide.Down, new SectorFixedAnchorRect(22, 31, 4, 1)));
            var boundaryRect = new SectorFixedAnchorRect(47, 4, 1, 4);
            projections.Add(Boundary("ANCHOR_BOUNDARY_FIXED", SectorFixedAnchorKind.BoundaryFixedSlice, boundaryRect));
            projections.Add(Boundary("ANCHOR_BOUNDARY_WARNING", SectorFixedAnchorKind.BoundaryWarning, boundaryRect));
            projections.Add(new SectorFixedAnchorProjection(
                "ANCHOR_VILLAGE_REFERENCE", VillageReferenceSector.Coordinate,
                SectorFixedAnchorKind.ReferenceOnlyMarker, SectorFixedAnchorSource.SpecialRegionSnapshot,
                SectorFixedAnchorPriority.ReferenceOnly, new SectorFixedAnchorRect(23, 15, 1, 1),
                VillageReferenceSector.SpecialRegion.RegionId));
            AddMandatorySpecial(projections, "CORE", CoreResourceSector);
            AddMandatorySpecial(projections, "FORGE", ForgeLandmarkSector);
            AddMandatorySpecial(projections, "BOSS", BossGateSector);
            ValidProjections = projections.ToArray();
        }

        public SectorPlannerInput Input { get; }
        public IReadOnlyList<SectorPacingAssignment> Assignments { get; }
        public IReadOnlyList<SectorFixedAnchorProjection> ValidProjections { get; }
        public SectorPlannerSectorSnapshot PlainTraversalBoundarySector { get; }
        public SectorPlannerSectorSnapshot QuietBufferSector { get; }
        public SectorPlannerSectorSnapshot VillageReferenceSector { get; }
        public SectorPlannerSectorSnapshot CoreResourceSector { get; }
        public SectorPlannerSectorSnapshot ForgeLandmarkSector { get; }
        public SectorPlannerSectorSnapshot BossGateSector { get; }
        public SectorPlannerSectorSnapshot ActivityCompatibleSector { get; }
        public SectorPlannerSectorSnapshot DeferredOptionalSector { get; }
        public SectorPlannerSectorSnapshot NeighborInfluencedSector { get; }

        public static AnchorFixtureSet Create() => new AnchorFixtureSet();

        public SectorFixedAnchorBuildRequest Request(IEnumerable<SectorFixedAnchorProjection> projections)
            => new SectorFixedAnchorBuildRequest(
                Input, Assignments, projections, SectorFixedAnchorPlanner.ReferencePublicationLabel);

        public SectorFixedAnchorBuildResult BuildValid()
            => SectorFixedAnchorPlanner.Build(Request(ValidProjections));

        private SectorFixedAnchorProjection Boundary(
            string id,
            SectorFixedAnchorKind kind,
            SectorFixedAnchorRect rect)
            => new SectorFixedAnchorProjection(
                id, PlainTraversalBoundarySector.Coordinate, kind,
                SectorFixedAnchorSource.BoundarySnapshot,
                kind == SectorFixedAnchorKind.BoundaryFixedSlice
                    ? SectorFixedAnchorPriority.BoundaryFixedSlice
                    : SectorFixedAnchorPriority.BoundaryWarning,
                rect, "BC_PLAIN_R_01", SectorPlannerSide.Right,
                true, "BOUNDARY_BC_PLAIN_R_01");

        private static SectorFixedAnchorProjection Route(
            string id,
            SectorPlannerSectorSnapshot sector,
            string socket,
            SectorPlannerSide side,
            SectorFixedAnchorRect rect)
            => new SectorFixedAnchorProjection(
                id, sector.Coordinate, SectorFixedAnchorKind.ExternalRouteSocket,
                SectorFixedAnchorSource.RouteSnapshot, SectorFixedAnchorPriority.ExternalRouteSocket,
                rect, socket, side);

        private static void AddMandatorySpecial(
            ICollection<SectorFixedAnchorProjection> target,
            string token,
            SectorPlannerSectorSnapshot sector)
        {
            target.Add(new SectorFixedAnchorProjection(
                "ANCHOR_" + token + "_FOOTPRINT", sector.Coordinate,
                SectorFixedAnchorKind.SpecialFootprint, SectorFixedAnchorSource.SpecialRegionSnapshot,
                SectorFixedAnchorPriority.SpecialReservation, new SectorFixedAnchorRect(18, 12, 12, 8),
                sector.SpecialRegion.RegionId, placedOwnershipClaim: true, progressionBlockerClaim: true));
            target.Add(new SectorFixedAnchorProjection(
                "ANCHOR_" + token + "_ENTRY_RETURN", sector.Coordinate,
                SectorFixedAnchorKind.SpecialEntryReturn, SectorFixedAnchorSource.SpecialRegionSnapshot,
                SectorFixedAnchorPriority.SpecialTransition, new SectorFixedAnchorRect(16, 14, 2, 4),
                sector.SpecialRegion.RegionId, placedOwnershipClaim: true, progressionBlockerClaim: true));
            target.Add(new SectorFixedAnchorProjection(
                "ANCHOR_" + token + "_APRON_BUFFER", sector.Coordinate,
                SectorFixedAnchorKind.SpecialApronBuffer, SectorFixedAnchorSource.SpecialRegionSnapshot,
                SectorFixedAnchorPriority.SpecialTransition, new SectorFixedAnchorRect(30, 12, 2, 8),
                sector.SpecialRegion.RegionId, placedOwnershipClaim: true, progressionBlockerClaim: true));
            var site = sector.Sites.Single();
            target.Add(new SectorFixedAnchorProjection(
                "ANCHOR_" + token + "_SITE", sector.Coordinate,
                SectorFixedAnchorKind.SiteReservation, SectorFixedAnchorSource.SiteSnapshot,
                SectorFixedAnchorPriority.SpecialReservation, new SectorFixedAnchorRect(20, 22, 4, 2),
                site.SiteId, placedOwnershipClaim: true, progressionBlockerClaim: true));
        }

        private static SectorPlannerSectorSnapshot Sector(
            int x,
            int y,
            IEnumerable<PacingRole> roles,
            IEnumerable<string> sockets = null,
            IEnumerable<SectorPlannerBoundarySnapshot> boundaries = null,
            IEnumerable<SectorPlannerSiteSnapshot> sites = null,
            SectorPlannerSpecialRegionSnapshot special = null,
            IEnumerable<SectorPlannerOptionalRegionSnapshot> optional = null,
            IEnumerable<SectorPlannerNeighborSnapshot> neighbors = null,
            SectorPlannerWorldProgressSnapshot progress = null,
            bool highRoute = false,
            bool recoveryNeeded = false,
            bool quiet = false,
            bool activityAvailable = false,
            bool eventAvailable = false)
        {
            var coordinate = new SectorCoord(x, y);
            return new SectorPlannerSectorSnapshot(
                coordinate, (y * WorldGenConstants.SectorColumns) + x,
                WorldGenConstants.SectorWidthTiles, WorldGenConstants.SectorHeightTiles,
                new SectorPlannerBiomeSnapshot("PATCH_" + y.ToString("D2", CultureInfo.InvariantCulture)
                                               + "_" + x.ToString("D2", CultureInfo.InvariantCulture), "MOON_PALACE"),
                new SectorPlannerRouteSnapshot(1, AccessClass.MandatoryNoTool,
                    sockets, highRoute, recoveryNeeded),
                boundaries, sites, special ?? SectorPlannerSpecialRegionSnapshot.None,
                optional, neighbors, progress ?? Progress(3, 3, 3), roles,
                quiet, activityAvailable, eventAvailable);
        }

        private static SectorPlannerSpecialRegionSnapshot Mandatory(
            string id,
            SectorPlannerSpecialRegionKind kind,
            string footprint)
            => new SectorPlannerSpecialRegionSnapshot(
                id, kind, SectorPlannerSpecialRegionBinding.ReservedMandatory,
                footprint, true, true, true);

        private static SectorPlannerWorldProgressSnapshot Progress(int ordinal, int mandatory, int optional)
            => new SectorPlannerWorldProgressSnapshot(
                ordinal, "CHAPTER_" + ordinal.ToString(CultureInfo.InvariantCulture),
                "MAIN", mandatory, optional);

        private static SectorPlannerNeighborSnapshot Neighbor(
            SectorPlannerSide side,
            int x,
            int y,
            string socket)
            => new SectorPlannerNeighborSnapshot(
                side, new SectorCoord(x, y), 1, AccessClass.MandatoryNoTool,
                new[] { socket }, PacingRole.Traversal);

        private static string Digest(string value)
        {
            using (var algorithm = System.Security.Cryptography.SHA256.Create())
            {
                return string.Concat(algorithm.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value))
                    .Select(item => item.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }
    }
}
