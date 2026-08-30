using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Activities;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.EventOverlays;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.SectorPlanning
{
    [TestFixture]
    [Category("MAP14_07")]
    public sealed class SectorCanvasOwnershipResolverTests
    {
        [Test]
        public void BuildClaimsPublishesAllSourceOwnersForReferenceSectorCanvas()
        {
            var canvas = Canvas();
            Assert.That(canvas.Build.Success, Is.True, Errors(canvas.Build.Errors));
            Assert.That(canvas.Build.ClaimsReady, Is.True);
            Assert.That(canvas.Build.Plan, Is.Null);
            AssertLowerSha(canvas.Build.CanonicalDigest);
            Assert.That(canvas.Upstream.QuietFillPlan.SectorCount, Is.EqualTo(9));
            Assert.That(canvas.Build.Claims.Count, Is.GreaterThan(9 * 48 * 32));
            foreach (SectorCanvasOwnerKind kind in Enum.GetValues(typeof(SectorCanvasOwnerKind)))
                Assert.That(canvas.Build.Claims.Any(value => value.OwnerKind == kind), Is.True, kind.ToString());
            Assert.That(((ICollection<SectorCanvasOwnershipClaim>)canvas.Build.Claims).IsReadOnly, Is.True);

            var plan = canvas.Resolved.Plan;
            TestContext.WriteLine("MAP14_07_TOTAL sectors={0} claims={1} winners={2} evidence={3} suppressed={4} owned={5} coverage={6} emptyEvidence={7} coexistence={8} conflicts={9}",
                plan.SectorCount, plan.ClaimCount, plan.WinnerClaimCount, plan.EvidenceClaimCount,
                plan.SuppressedClaimCount, plan.OwnedCellCount, plan.CoverageCount,
                plan.ExplicitNoTerrainEvidenceCoordinateCount,
                plan.AllowedCrossPlaneCoexistenceCount, plan.ConflictCount);
            foreach (SectorCanvasOwnerKind kind in Enum.GetValues(typeof(SectorCanvasOwnerKind)))
                TestContext.WriteLine("MAP14_07_OWNER kind={0} claims={1} winners={2} suppressed={3}",
                    kind, plan.CountClaims(kind), plan.CountWinners(kind), plan.CountSuppressed(kind));
            foreach (SectorCanvasOwnershipPlane plane in Enum.GetValues(typeof(SectorCanvasOwnershipPlane)))
                TestContext.WriteLine("MAP14_07_PLANE plane={0} owned={1}", plane, plan.CountOwned(plane));
            TestContext.WriteLine("MAP14_07_DIGEST claims={0} plan={1}",
                plan.ClaimDigest, plan.CanonicalDigest);
        }

        [Test]
        public void ResolverAppliesSpecialBoundarySpineClusterPatternQuietMarkerPriority()
        {
            var fixture = Fixture.Create();
            var upstream = Complete(fixture);
            var coordinate = new LocalTileCoord(0, 14);
            var sector = new SectorCoord(1, 1);
            var additions = new[]
            {
                Synthetic("PRIORITY_SPECIAL", sector, 14, coordinate, SectorCanvasOwnershipPlane.Terrain,
                    SectorCanvasOwnerKind.SpecialRegion, true, false),
                Synthetic("PRIORITY_BOUNDARY", sector, 14, coordinate, SectorCanvasOwnershipPlane.Terrain,
                    SectorCanvasOwnerKind.Boundary, true, false),
                Synthetic("PRIORITY_SPINE", sector, 14, coordinate, SectorCanvasOwnershipPlane.Terrain,
                    SectorCanvasOwnerKind.Spine, true, false),
                Synthetic("PRIORITY_CLUSTER", sector, 14, coordinate, SectorCanvasOwnershipPlane.Terrain,
                    SectorCanvasOwnerKind.TerrainCluster, true, false),
                Synthetic("PRIORITY_PATTERN", sector, 14, coordinate, SectorCanvasOwnershipPlane.Terrain,
                    SectorCanvasOwnerKind.MicroPattern, true, false),
                Synthetic("PRIORITY_QUIET", sector, 14, coordinate, SectorCanvasOwnershipPlane.Terrain,
                    SectorCanvasOwnerKind.Quiet, true, false),
                Synthetic("PRIORITY_ACTIVITY", sector, 14, coordinate, SectorCanvasOwnershipPlane.Marker,
                    SectorCanvasOwnerKind.ActivityMarker, true, true),
                Synthetic("PRIORITY_EVENT", sector, 14, coordinate, SectorCanvasOwnershipPlane.Marker,
                    SectorCanvasOwnerKind.EventMarker, true, true),
            };
            var build = SectorCanvasOwnershipClaimBuilder.BuildClaims(Request(fixture, upstream, additions));
            var resolved = SectorCanvasOwnershipResolver.Resolve(build);
            Assert.That(resolved.Success, Is.True, Errors(resolved.Errors));
            var terrain = resolved.Plan.WinnerClaims.Single(value =>
                value.SectorIndex == 14 && value.Coordinate.Equals(coordinate) &&
                value.Plane == SectorCanvasOwnershipPlane.Terrain);
            var marker = resolved.Plan.WinnerClaims.Single(value =>
                value.SectorIndex == 14 && value.Coordinate.Equals(coordinate) &&
                value.Plane == SectorCanvasOwnershipPlane.Marker);
            Assert.That(terrain.OwnerKind, Is.EqualTo(SectorCanvasOwnerKind.SpecialRegion));
            Assert.That(marker.OwnerKind, Is.EqualTo(SectorCanvasOwnerKind.ActivityMarker));
            var terrainSuppressed = resolved.Plan.SuppressedClaims.Where(value =>
                value.SectorIndex == 14 && value.Coordinate.Equals(coordinate) &&
                value.Plane == SectorCanvasOwnershipPlane.Terrain).ToArray();
            CollectionAssert.IsSubsetOf(new[]
            {
                SectorCanvasOwnerKind.Boundary, SectorCanvasOwnerKind.Spine,
                SectorCanvasOwnerKind.TerrainCluster, SectorCanvasOwnerKind.MicroPattern,
                SectorCanvasOwnerKind.Quiet,
            }, terrainSuppressed.Select(value => value.SuppressedOwnerKind).ToArray());
            Assert.That(terrainSuppressed.All(value =>
                (int)value.WinnerPriority < (int)value.SuppressedPriority &&
                value.Reason.Contains(">")), Is.True);
            Assert.That(resolved.Plan.SuppressedClaims.Any(value =>
                value.WinnerOwnerKind == SectorCanvasOwnerKind.ActivityMarker &&
                value.SuppressedOwnerKind == SectorCanvasOwnerKind.EventMarker), Is.True);
        }

        [Test]
        public void ResolvedCanvasHasNoSamePlaneDoubleOwners()
        {
            var canvas = Canvas();
            var plan = canvas.Resolved.Plan;
            Assert.That(plan, Is.Not.Null, Errors(canvas.Resolved.Errors));
            Assert.That(plan.WinnerClaims.GroupBy(value =>
                value.SectorIndex + "|" + value.Coordinate.X + "," + value.Coordinate.Y + "|" + value.Plane)
                .All(value => value.Count() == 1), Is.True);
            Assert.That(plan.SamePlaneDoubleOwnerCount, Is.Zero);
            Assert.That(plan.ForbiddenOverlapCount, Is.Zero);
            Assert.That(plan.UnresolvedConflictCount, Is.Zero);
        }

        [Test]
        public void ProtectedOpenAnchorsSpecialShellsAndPatternNoWriteRulesHold()
        {
            var canvas = Canvas();
            var plan = canvas.Resolved.Plan;
            var protectedCells = canvas.Upstream.QuietFillPlan.Cells
                .Where(value => value.ProtectedNoWrite).ToArray();
            Assert.That(protectedCells.Length, Is.GreaterThan(0));
            foreach (var cell in protectedCells)
            {
                Assert.That(plan.WinnerClaims.Any(value =>
                    value.SectorIndex == cell.SectorIndex && value.Coordinate.Equals(cell.Coordinate) &&
                    value.Plane == SectorCanvasOwnershipPlane.Protection), Is.True);
                Assert.That(plan.WinnerClaims.Any(value =>
                    value.SectorIndex == cell.SectorIndex && value.Coordinate.Equals(cell.Coordinate) &&
                    value.Plane == SectorCanvasOwnershipPlane.Terrain &&
                    (value.OwnerKind == SectorCanvasOwnerKind.MicroPattern ||
                     value.OwnerKind == SectorCanvasOwnerKind.Quiet)), Is.False);
            }
            Assert.That(canvas.Fixture.AnchorPlan.CanonicalDigest,
                Is.EqualTo(plan.FixedAnchorPlanDigestAfter));
            Assert.That(canvas.Fixture.SpineEnvelopePlan.CanonicalDigest,
                Is.EqualTo(plan.SpineEnvelopePlanDigestAfter));
            Assert.That(canvas.Fixture.RenderPlan.CanonicalDigest,
                Is.EqualTo(plan.PatternRenderPlanDigestAfter));
            Assert.That(plan.Claims.Any(value =>
                value.OwnerKind == SectorCanvasOwnerKind.SpecialRegion &&
                (value.SourceObjectId.Contains("REGION_CORE") ||
                 value.SourceObjectId.Contains("REGION_FORGE") ||
                 value.SourceObjectId.Contains("REGION_BOSS"))), Is.True,
                "Core/Forge/Boss Special ownership/evidence claims must remain traceable.");
            Assert.That(plan.Claims.Any(value =>
                value.OwnerKind == SectorCanvasOwnerKind.Boundary &&
                (value.SourceObjectId.Contains("ANCHOR_BOUNDARY_FIXED") ||
                 value.SemanticValue.Contains("BoundaryFixedSlice"))), Is.True,
                "Boundary fixed-slice ownership/evidence claim must remain traceable.");
            Assert.That(plan.Claims.Any(value => value.OwnerKind == SectorCanvasOwnerKind.MicroPattern &&
                                                 value.Plane == SectorCanvasOwnershipPlane.Terrain), Is.True,
                "At least one unprotected MAP14_05 render cell must remain a MicroPattern terrain claim.");
        }

        [Test]
        public void ActivityAndEventMarkersRemainMarkerOnlyOrEvidenceOnly()
        {
            var canvas = Canvas();
            var plan = canvas.Resolved.Plan;
            var markerClaims = plan.Claims.Where(value =>
                value.OwnerKind == SectorCanvasOwnerKind.ActivityMarker ||
                value.OwnerKind == SectorCanvasOwnerKind.EventMarker).ToArray();
            Assert.That(markerClaims.Length, Is.GreaterThan(0));
            Assert.That(markerClaims.All(value => value.MarkerOnly &&
                (value.Plane == SectorCanvasOwnershipPlane.Marker ||
                 value.Plane == SectorCanvasOwnershipPlane.Evidence)), Is.True);
            Assert.That(markerClaims.Any(value => value.Plane == SectorCanvasOwnershipPlane.Terrain), Is.False);
            Assert.That(plan.Claims.Count(value => value.OwnerKind == SectorCanvasOwnerKind.Empty &&
                                                   value.Plane == SectorCanvasOwnershipPlane.Evidence),
                Is.GreaterThanOrEqualTo(canvas.Upstream.EventAssignedEmptyCount));
            Assert.That(canvas.Upstream.ActivitySelectedCount, Is.EqualTo(1));
            Assert.That(canvas.Upstream.EventAssignedNonEmptyCount, Is.EqualTo(1));
            Assert.That(canvas.Upstream.EventAssignedEmptyCount, Is.EqualTo(8));
            Assert.That(plan.ActivityRuntimeSpawnCount, Is.Zero);
            Assert.That(plan.EventRuntimeSpawnCount, Is.Zero);
        }

        [Test]
        public void CoveragePublishesTerrainWinnerOrExplicitNoTerrainEvidenceForEveryTile()
        {
            var canvas = Canvas();
            var plan = canvas.Resolved.Plan;
            Assert.That(plan.SectorCount, Is.EqualTo(9));
            Assert.That(plan.ExpectedCoverageCount, Is.EqualTo(9 * 48 * 32));
            Assert.That(plan.CoverageCount, Is.EqualTo(9 * 48 * 32));
            Assert.That(plan.CoverageCount, Is.EqualTo(plan.ExpectedCoverageCount));
            Assert.That(plan.ExplicitNoTerrainEvidenceCoordinateCount, Is.GreaterThan(0));
            Assert.That(plan.Map14_08HandoffReady, Is.True);
        }

        [Test]
        public void ConflictRulesRejectEqualPriorityForbiddenOverlapAndMissingWinner()
        {
            var fixture = Fixture.Create();
            var upstream = Complete(fixture);
            var plain = new SectorCoord(1, 1);
            var externalSocket = new LocalTileCoord(0, 14);
            var equal = new[]
            {
                Synthetic("EQUAL_A", plain, 14, externalSocket, SectorCanvasOwnershipPlane.Terrain,
                    SectorCanvasOwnerKind.TerrainCluster, true, false),
                Synthetic("EQUAL_B", plain, 14, externalSocket, SectorCanvasOwnershipPlane.Terrain,
                    SectorCanvasOwnerKind.TerrainCluster, true, false),
            };
            AssertAtomic(SectorCanvasOwnershipResolver.Resolve(
                    SectorCanvasOwnershipClaimBuilder.BuildClaims(Request(fixture, upstream, equal))),
                SectorCanvasOwnershipErrorCode.DoubleOwnerConflict,
                SectorCanvasOwnershipErrorCode.TerrainPlaneConflict);

            var quietSector = new SectorCoord(2, 1);
            var quietCoordinate = new LocalTileCoord(10, 10);
            var forbidden = new[]
            {
                Synthetic("FORBIDDEN_SPECIAL", quietSector, 15, quietCoordinate,
                    SectorCanvasOwnershipPlane.Reservation, SectorCanvasOwnerKind.SpecialRegion, false, false),
                Synthetic("FORBIDDEN_BOUNDARY", quietSector, 15, quietCoordinate,
                    SectorCanvasOwnershipPlane.Reservation, SectorCanvasOwnerKind.Boundary, false, false),
            };
            AssertAtomic(SectorCanvasOwnershipResolver.Resolve(
                    SectorCanvasOwnershipClaimBuilder.BuildClaims(Request(fixture, upstream, forbidden))),
                SectorCanvasOwnershipErrorCode.ForbiddenOverlap,
                SectorCanvasOwnershipErrorCode.ReservationPlaneConflict);

            var missing = SectorCanvasOwnershipClaimBuilder.BuildClaims(Request(
                fixture, upstream, faults: new[] { SectorCanvasOwnershipErrorCode.MissingRequiredClaim }));
            AssertAtomic(SectorCanvasOwnershipResolver.Resolve(missing),
                SectorCanvasOwnershipErrorCode.MissingRequiredClaim);

            var mutation = SectorCanvasOwnershipClaimBuilder.BuildClaims(Request(
                fixture, upstream, activityMarkerMutationClaim: true,
                eventMarkerMutationClaim: true, retryCount: 1, map14RngDrawCount: 1,
                tilemapWriteCount: 1, sceneMutationCount: 1));
            AssertAtomic(mutation,
                SectorCanvasOwnershipErrorCode.ActivityMarkerMutationClaim,
                SectorCanvasOwnershipErrorCode.EventMarkerMutationClaim,
                SectorCanvasOwnershipErrorCode.SolverMutationClaim,
                SectorCanvasOwnershipErrorCode.RngMutationClaim,
                SectorCanvasOwnershipErrorCode.TileMutationClaim,
                SectorCanvasOwnershipErrorCode.SceneMutationClaim);

            var outOfBounds = new[]
            {
                Synthetic("OUT_OF_BOUNDS", plain, 14, new LocalTileCoord(48, 0),
                    SectorCanvasOwnershipPlane.Terrain, SectorCanvasOwnerKind.Quiet, true, false),
            };
            AssertAtomic(SectorCanvasOwnershipClaimBuilder.BuildClaims(
                    Request(fixture, upstream, outOfBounds)),
                SectorCanvasOwnershipErrorCode.ClaimOutOfBounds);
        }

        [Test]
        public void UpstreamIdentityAndRenderQuietMarkerPlansAreNotMutated()
        {
            var fixture = Fixture.Create();
            var upstream = Complete(fixture);
            var before = new[]
            {
                fixture.Input.CanonicalDigest,
                string.Join(";", fixture.Assignments.Select(value => value.CanonicalDigest)),
                fixture.AnchorPlan.CanonicalDigest,
                fixture.PlacementPlan.CanonicalDigest,
                fixture.SpineEnvelopePlan.CanonicalDigest,
                fixture.RolePlan.CanonicalDigest,
                fixture.RenderPlan.CanonicalDigest,
                upstream.QuietFillPlan.CanonicalDigest,
                upstream.CanonicalDigest,
                upstream.ActivityFrequencyPlanDigestBefore,
                upstream.EventAssignmentPlanDigestBefore,
            };
            var result = SectorCanvasOwnershipResolver.Resolve(
                SectorCanvasOwnershipClaimBuilder.BuildClaims(Request(fixture, upstream)));
            Assert.That(result.Success, Is.True, Errors(result.Errors));
            var after = new[]
            {
                fixture.Input.CanonicalDigest,
                string.Join(";", fixture.Assignments.Select(value => value.CanonicalDigest)),
                fixture.AnchorPlan.CanonicalDigest,
                fixture.PlacementPlan.CanonicalDigest,
                fixture.SpineEnvelopePlan.CanonicalDigest,
                fixture.RolePlan.CanonicalDigest,
                fixture.RenderPlan.CanonicalDigest,
                upstream.QuietFillPlan.CanonicalDigest,
                upstream.CanonicalDigest,
                upstream.ActivityFrequencyPlanDigestAfter,
                upstream.EventAssignmentPlanDigestAfter,
            };
            CollectionAssert.AreEqual(before, after);
            var plan = result.Plan;
            Assert.That(plan.RouteAccessIdentityBefore, Is.EqualTo(plan.RouteAccessIdentityAfter));
            Assert.That(plan.ExternalSocketIdentityBefore, Is.EqualTo(plan.ExternalSocketIdentityAfter));
            Assert.That(plan.BoundaryIdentityBefore, Is.EqualTo(plan.BoundaryIdentityAfter));
            Assert.That(plan.SpecialIdentityBefore, Is.EqualTo(plan.SpecialIdentityAfter));
            Assert.That(plan.ClusterIdentityBefore, Is.EqualTo(plan.ClusterIdentityAfter));
            Assert.That(plan.ProtectedOpenIdentityBefore, Is.EqualTo(plan.ProtectedOpenIdentityAfter));
        }

        [Test]
        public void NoRetryRngTilePhysicsSceneOrGameplayMutation()
        {
            var plan = Canvas().Resolved.Plan;
            Assert.That(plan.FinalReferenceOwnershipWriteCount, Is.EqualTo(plan.OwnedCellCount));
            Assert.That(plan.RetryCount, Is.Zero);
            Assert.That(plan.Map14RngDrawCount, Is.Zero);
            Assert.That(plan.SolverInvocationCount, Is.Zero);
            Assert.That(plan.PatternClusterReselectionCount, Is.Zero);
            Assert.That(plan.TilemapWriteCount, Is.Zero);
            Assert.That(plan.SceneMutationCount, Is.Zero);
            Assert.That(plan.PrefabMutationCount, Is.Zero);
            Assert.That(plan.GameObjectMutationCount, Is.Zero);
            Assert.That(plan.ActivityRuntimeSpawnCount, Is.Zero);
            Assert.That(plan.EventRuntimeSpawnCount, Is.Zero);
            Assert.That(plan.SpecialPersistenceMutationCount, Is.Zero);
            Assert.That(plan.RewardExecutionCount, Is.Zero);
            Assert.That(plan.CombatExecutionCount, Is.Zero);
            Assert.That(plan.CraftingExecutionCount, Is.Zero);
            Assert.That(plan.InventoryExecutionCount, Is.Zero);
            Assert.That(plan.NpcExecutionCount, Is.Zero);
        }

        [Test]
        public void PublicationIsDeterministicAcrossRepeatReverseAndTurkishCulture()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUi = CultureInfo.CurrentUICulture;
            try
            {
                var first = Canvas();
                var repeated = SectorCanvasOwnershipResolver.Resolve(first.Build);
                var reverse = Canvas(true);
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var turkish = Canvas();
                Assert.That(new[]
                {
                    first.Resolved.Success, repeated.Success,
                    reverse.Resolved.Success, turkish.Resolved.Success,
                }, Is.All.True);
                Assert.That(repeated.CanonicalDigest, Is.EqualTo(first.Resolved.CanonicalDigest));
                Assert.That(reverse.Build.CanonicalDigest, Is.EqualTo(first.Build.CanonicalDigest));
                Assert.That(reverse.Resolved.CanonicalDigest, Is.EqualTo(first.Resolved.CanonicalDigest));
                Assert.That(turkish.Build.CanonicalDigest, Is.EqualTo(first.Build.CanonicalDigest));
                Assert.That(turkish.Resolved.CanonicalDigest, Is.EqualTo(first.Resolved.CanonicalDigest));
                Assert.That(Join(reverse.Resolved.Plan.WinnerClaims.Select(value => value.ClaimId)),
                    Is.EqualTo(Join(first.Resolved.Plan.WinnerClaims.Select(value => value.ClaimId))));
                Assert.That(Join(reverse.Resolved.Plan.SuppressedClaims.Select(value =>
                        value.WinnerClaimId + ">" + value.SuppressedClaimId)),
                    Is.EqualTo(Join(first.Resolved.Plan.SuppressedClaims.Select(value =>
                        value.WinnerClaimId + ">" + value.SuppressedClaimId))));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUi;
            }
        }

        private static CanvasPackage Canvas(bool reverse = false)
        {
            var fixture = Fixture.Create(reverse);
            var fill = fixture.Fill();
            Require(fill.Success, fill.Errors);
            var upstream = fixture.Place(fill.Plan, fixture.CreateAuthorities(fill.Plan, reverse));
            Require(upstream.Success, upstream.Errors);
            var build = SectorCanvasOwnershipClaimBuilder.BuildClaims(Request(fixture, upstream.Plan));
            Require(build.Success, build.Errors);
            var resolved = SectorCanvasOwnershipResolver.Resolve(build);
            Require(resolved.Success, resolved.Errors);
            return new CanvasPackage(fixture, upstream.Plan, build, resolved);
        }

        private static SectorQuietActivityEventPlan Complete(Fixture fixture)
        {
            var fill = fixture.Fill();
            Require(fill.Success, fill.Errors);
            var result = fixture.Place(fill.Plan);
            Require(result.Success, result.Errors);
            return result.Plan;
        }

        private static SectorCanvasOwnershipBuildRequest Request(
            Fixture fixture,
            SectorQuietActivityEventPlan upstream,
            IEnumerable<SectorCanvasOwnershipClaim> additions = null,
            IEnumerable<SectorCanvasOwnershipErrorCode> faults = null,
            bool activityMarkerMutationClaim = false,
            bool eventMarkerMutationClaim = false,
            int retryCount = 0,
            int map14RngDrawCount = 0,
            int tilemapWriteCount = 0,
            int sceneMutationCount = 0) =>
            new SectorCanvasOwnershipBuildRequest(
                fixture.Input, fixture.Assignments, fixture.AnchorPlan,
                fixture.PlacementPlan, fixture.SpineEnvelopePlan, fixture.RolePlan,
                fixture.RenderPlan, upstream,
                SectorCanvasOwnershipClaimBuilder.ReferencePublicationLabel,
                sourceAdditionalClaims: additions,
                sourceReferenceFaults: faults,
                activityMarkerMutationClaim: activityMarkerMutationClaim,
                eventMarkerMutationClaim: eventMarkerMutationClaim,
                retryCount: retryCount,
                map14RngDrawCount: map14RngDrawCount,
                tilemapWriteCount: tilemapWriteCount,
                sceneMutationCount: sceneMutationCount);

        private static SectorCanvasOwnershipClaim Synthetic(
            string id,
            SectorCoord sector,
            int sectorIndex,
            LocalTileCoord coordinate,
            SectorCanvasOwnershipPlane plane,
            SectorCanvasOwnerKind owner,
            bool allowSuppression,
            bool markerOnly) =>
            new SectorCanvasOwnershipClaim(
                id, sector, sectorIndex, coordinate, plane, owner, Priority(owner),
                "MAP14_07_REFERENCE", id, Hash("SOURCE|" + id), id,
                true, allowSuppression, plane != SectorCanvasOwnershipPlane.Terrain,
                markerOnly);

        private static SectorCanvasOwnershipPriority Priority(SectorCanvasOwnerKind owner)
        {
            switch (owner)
            {
                case SectorCanvasOwnerKind.SpecialRegion: return SectorCanvasOwnershipPriority.SpecialRegion;
                case SectorCanvasOwnerKind.Boundary: return SectorCanvasOwnershipPriority.Boundary;
                case SectorCanvasOwnerKind.Spine: return SectorCanvasOwnershipPriority.Spine;
                case SectorCanvasOwnerKind.TerrainCluster: return SectorCanvasOwnershipPriority.TerrainCluster;
                case SectorCanvasOwnerKind.MicroPattern: return SectorCanvasOwnershipPriority.MicroPattern;
                case SectorCanvasOwnerKind.Quiet: return SectorCanvasOwnershipPriority.Quiet;
                case SectorCanvasOwnerKind.ActivityMarker: return SectorCanvasOwnershipPriority.ActivityMarker;
                case SectorCanvasOwnerKind.EventMarker: return SectorCanvasOwnershipPriority.EventMarker;
                default: return SectorCanvasOwnershipPriority.Evidence;
            }
        }

        private static void AssertAtomic(
            SectorCanvasOwnershipBuildResult result,
            params SectorCanvasOwnershipErrorCode[] expected)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.Claims, Is.Empty);
            Assert.That(result.CanonicalDigest, Is.Empty);
            foreach (var code in expected)
                Assert.That(result.Errors.Select(value => value.Code), Does.Contain(code));
            Assert.That(result.Errors, Is.Ordered);
        }

        private static void AssertLowerSha(string value) =>
            Assert.That(value, Does.Match("^[0-9a-f]{64}$"));

        private static string Join<T>(IEnumerable<T> values) => string.Join(";", values);

        private static string Errors(IEnumerable<SectorCanvasOwnershipError> errors) =>
            string.Join(";", (errors ?? Array.Empty<SectorCanvasOwnershipError>())
                .Select(value => value.ToString()));

        private static void Require<T>(bool success, IEnumerable<T> errors)
        {
            if (!success) throw new InvalidOperationException(string.Join(";", errors));
        }

        private static string Hash(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                var result = new StringBuilder(bytes.Length * 2);
                foreach (var item in bytes)
                    result.Append(item.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }

        private sealed class CanvasPackage
        {
            internal CanvasPackage(
                Fixture fixture,
                SectorQuietActivityEventPlan upstream,
                SectorCanvasOwnershipBuildResult build,
                SectorCanvasOwnershipBuildResult resolved)
            {
                Fixture = fixture;
                Upstream = upstream;
                Build = build;
                Resolved = resolved;
            }

            internal Fixture Fixture { get; }
            internal SectorQuietActivityEventPlan Upstream { get; }
            internal SectorCanvasOwnershipBuildResult Build { get; }
            internal SectorCanvasOwnershipBuildResult Resolved { get; }
        }

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
            private static readonly string CatalogDigest = Hash("MAP11_CATALOG");
            private static readonly string SignatureDigest = Hash("MAP11_SIGNATURES");
            private static readonly string ManifestDigest = Hash("MAP12_MANIFEST");

            private Fixture(
                SectorPlannerInput input,
                IReadOnlyList<SectorPacingAssignment> assignments,
                SectorFixedAnchorPlan anchorPlan,
                SectorClusterPlacementPlan placementPlan,
                SectorSpineEnvelopePlan spineEnvelopePlan,
                SectorClusterRolePatternPlan rolePlan,
                SectorPatternRenderPlan renderPlan)
            {
                Input = input;
                Assignments = assignments;
                AnchorPlan = anchorPlan;
                PlacementPlan = placementPlan;
                SpineEnvelopePlan = spineEnvelopePlan;
                RolePlan = rolePlan;
                RenderPlan = renderPlan;
            }

            internal SectorPlannerInput Input { get; }
            internal IReadOnlyList<SectorPacingAssignment> Assignments { get; }
            internal SectorFixedAnchorPlan AnchorPlan { get; }
            internal SectorClusterPlacementPlan PlacementPlan { get; }
            internal SectorSpineEnvelopePlan SpineEnvelopePlan { get; }
            internal SectorClusterRolePatternPlan RolePlan { get; }
            internal SectorPatternRenderPlan RenderPlan { get; }

            internal static Fixture Create(bool reverse = false)
            {
                var sectors = CreateSectors();
                if (reverse) sectors.Reverse();
                var authority = SectorPlannerAuthorityDigestSnapshot.CaptureCurrentPublicAuthorities(
                    Hash("FOUNDATION"), Hash("LAYER"), 24, Hash("PATTERN"), 16,
                    Hash("CLUSTER"), 7, Hash("ACTIVITY"), 5, Hash("EVENT"));
                var input = SectorPlannerInputBuilder.Build(new SectorPlannerInputRequest(
                    sectors, authority, SectorPlannerInputBuilder.ReferencePublicationLabel));
                Require(input.Success, input.Errors);
                var assignments = SectorPacingRolePlanner.Assign(input.Input).ToList();
                var anchors = CreateAnchors();
                if (reverse) { assignments.Reverse(); anchors.Reverse(); }
                var anchor = SectorFixedAnchorPlanner.Build(new SectorFixedAnchorBuildRequest(
                    input.Input, assignments, anchors, SectorFixedAnchorPlanner.ReferencePublicationLabel));
                Require(anchor.Success, anchor.Errors);
                var catalog = CreateClusterCatalog();
                if (reverse) catalog.Reverse();
                var candidates = SectorClusterCandidateBuilder.Build(new SectorClusterCandidateBuildRequest(
                    input.Input, assignments, anchor.Plan, catalog,
                    SectorClusterCandidateBuilder.ReferenceCandidatePublicationLabel));
                Require(candidates.Success, candidates.Errors);
                var placement = SectorClusterPlacementPlanner.Place(new SectorClusterPlacementRequest(
                    candidates.CandidateSet, anchor.Plan,
                    SectorClusterPlacementPlanner.ReferencePlacementPublicationLabel));
                Require(placement.Success, placement.Errors);
                var spineRequest = new SectorSpineEnvelopeBuildRequest(
                    input.Input, assignments, anchor.Plan, placement.Plan,
                    SectorSpineGraphBuilder.ReferenceGraphPublicationLabel,
                    SectorTraversalEnvelopeBuilder.ReferenceEnvelopePublicationLabel);
                var graph = SectorSpineGraphBuilder.Build(spineRequest);
                Require(graph.Success, graph.Errors);
                var spine = SectorTraversalEnvelopeBuilder.Build(spineRequest, graph.Graph);
                Require(spine.Success, spine.Errors);
                var roles = SectorClusterRoleZoneBuilder.Build(new SectorClusterRoleZoneBuildRequest(
                    input.Input, assignments, anchor.Plan, placement.Plan, spine.Plan,
                    SectorClusterRoleZoneBuilder.ReferencePublicationLabel));
                Require(roles.Success, roles.Errors);
                var patterns = CreatePatternCatalog();
                if (reverse) patterns.Reverse();
                var render = SectorPatternRenderPlanner.Render(new SectorPatternRenderRequest(
                    roles.Plan, patterns, SectorPatternRenderPlanner.ReferencePublicationLabel));
                Require(render.Success, render.Errors);
                return new Fixture(input.Input, assignments, anchor.Plan, placement.Plan,
                    spine.Plan, roles.Plan, render.Plan);
            }

            internal SectorQuietFillBuildResult Fill() => SectorQuietFillPlanner.Fill(
                new SectorQuietActivityEventBuildRequest(
                    Input, Assignments, AnchorPlan, PlacementPlan, SpineEnvelopePlan,
                    RolePlan, RenderPlan, SectorQuietFillPlanner.ReferencePublicationLabel));

            internal SectorQuietActivityEventBuildResult Place(SectorQuietFillPlan fill) =>
                Place(fill, CreateAuthorities(fill));

            internal SectorQuietActivityEventBuildResult Place(
                SectorQuietFillPlan fill,
                AuthorityPackage package) =>
                SectorActivityEventPlacementPlanner.Place(package.Request(fill));

            internal AuthorityPackage CreateAuthorities(SectorQuietFillPlan fill, bool reverse = false)
            {
                var ownership = Ownership();
                var profiles = new List<ActivityPlacementProfile>();
                var projections = new List<SectorActivityOpportunityProjection>();
                foreach (var placement in PlacementPlan.Placements.OrderBy(value => value.SectorIndex))
                {
                    var sector = Input.Sectors.Single(value => value.SectorIndex == placement.SectorIndex);
                    var assignment = Assignments.Single(value => value.Coordinate == sector.Coordinate);
                    ownership.TryGetSector(sector.Coordinate, out var owned);
                    var rectangle = FindRectangle(fill, sector.Coordinate);
                    var activityId = new ActivityStructureId("ACTIVITY_MAP14_06_" + placement.SectorIndex.ToString("D3", CultureInfo.InvariantCulture));
                    var shell = Hash("SHELL|" + placement.ClusterId.Value);
                    var safety = Hash("SAFETY|" + placement.ClusterId.Value);
                    profiles.Add(new ActivityPlacementProfile(
                        activityId, placement.ClusterId, placement.VariantId,
                        Hash("ACTIVITY|" + activityId.Value), shell, safety,
                        new[] { Biome(sector) }, new[] { assignment.PrimaryRole },
                        new[] { sector.Route.AccessClass }, placement.Cells.Count, placement.Cells.Count,
                        2, 2, 100, ActivityStrengthClass.Strong));
                    var opportunity = new ActivityPlacementOpportunity(
                        "ACTIVITY_OPPORTUNITY_" + placement.SectorIndex.ToString("D3", CultureInfo.InvariantCulture),
                        sector.Coordinate, owned.PatchId.Value, Biome(sector),
                        placement.ClusterId, placement.VariantId, assignment.PrimaryRole,
                        sector.Route.AccessClass, placement.Cells.Count,
                        new ActivityPlacementClearanceEvidence(rectangle[0], 2, 2,
                            rectangle, rectangle, Array.Empty<LocalTileCoord>(), Array.Empty<LocalTileCoord>()),
                        CatalogDigest, SignatureDigest, ManifestDigest, shell, safety);
                    projections.Add(new SectorActivityOpportunityProjection(
                        opportunity, rectangle[0], MarkerForActivity(assignment.PrimaryRole), safety));
                }
                if (reverse) { profiles.Reverse(); projections.Reverse(); }
                var activityIndex = ActivityCandidateIndexCompiler.Compile(new ActivityCandidateIndexCompileRequest(
                    profiles, projections.Select(value => value.Authority), ownership,
                    CatalogDigest, SignatureDigest, ManifestDigest));
                Require(activityIndex.Success, activityIndex.Errors);
                var activityPlan = ActivityFrequencyPlanner.Plan(new ActivityFrequencyPlanRequest(
                    activityIndex.Index, new ActivityFrequencyPolicy(120, 1, 1, 1),
                    0x14060001UL, 0, RngFactory()));
                Require(activityPlan.Success, activityPlan.Errors);

                var eventProfiles = EventProfiles();
                var eventProjections = new List<SectorEventMarkerOpportunityProjection>();
                foreach (var activityProjection in projections.OrderBy(value => value.OpportunityId, StringComparer.Ordinal))
                {
                    var sector = Input.Sectors.Single(value => value.Coordinate == activityProjection.SectorCoordinate);
                    ownership.TryGetSector(sector.Coordinate, out var owned);
                    var markerKind = MarkerForEvent(sector);
                    var owner = sector.SpecialRegion.Kind == SectorPlannerSpecialRegionKind.None
                        ? PlacementPlan.Placements.Single(value => value.SectorCoordinate == sector.Coordinate).ClusterId.Value
                        : sector.SpecialRegion.RegionId;
                    var sourceKind = markerKind == SectorActivityEventMarkerKind.EventSpecial
                        ? EventMarkerTargetSourceKind.SpecialRegion
                        : markerKind == SectorActivityEventMarkerKind.EventActivity
                            ? EventMarkerTargetSourceKind.Activity
                            : EventMarkerTargetSourceKind.TerrainCluster;
                    var marker = new EventMarkerTargetEvidence(
                        new EventMarkerId("MARKER_MAP14_06"), sourceKind, owner,
                        activityProjection.MarkerCoordinate, activityProjection.MarkerCoordinate,
                        markerKind.ToString(), "QUIET", "QUIET", Hash("STATIC"), Hash("STATIC"),
                        Hash("PROTECTION"), Hash("PROTECTION"), default(SpecialPersistenceKey),
                        string.Empty, string.Empty);
                    var opportunity = new EventOverlayOpportunity(
                        "EVENT_OPP_" + sector.SectorIndex.ToString("D3", CultureInfo.InvariantCulture),
                        sector.Coordinate, owned.PatchId.Value, sector.SectorIndex,
                        Biome(sector), Assignments.Single(value => value.Coordinate == sector.Coordinate).PrimaryRole,
                        sector.Route.AccessClass, new TerrainClusterId("TC_MAP14_06_EVENT"), null,
                        activityPlan.Plan.CanonicalDigest, new[] { marker });
                    eventProjections.Add(new SectorEventMarkerOpportunityProjection(
                        opportunity, activityProjection.MarkerCoordinate, markerKind, owner));
                }
                if (reverse) { eventProfiles.Reverse(); eventProjections.Reverse(); }
                var eventIndex = EventOverlayCandidateIndexCompiler.Compile(new EventOverlayCandidateIndexRequest(
                    eventProfiles, eventProjections.Select(value => value.Authority), activityPlan.Plan.CanonicalDigest));
                Require(eventIndex.Success, eventIndex.Errors);
                var eventPlan = EventOverlayAssignmentPlanner.Plan(new EventOverlayAssignmentPlanRequest(
                    eventIndex.Index, new EventOverlayAssignmentPolicy(80),
                    0x14060002UL, 0, RngFactory()));
                Require(eventPlan.Success, eventPlan.Errors);
                return new AuthorityPackage(projections, activityIndex.Index, activityPlan.Plan,
                    eventProjections, eventIndex.Index, eventPlan.Plan);
            }

            private static List<SectorPlannerSectorSnapshot> CreateSectors() =>
                new List<SectorPlannerSectorSnapshot>
                {
                    Sector(Plain, MoonpalaceBiomeId.MoonCrater,
                        new[] { PacingRole.Traversal, PacingRole.Recovery },
                        route: new SectorPlannerRouteSnapshot(1, AccessClass.MandatoryNoTool,
                            new[] { "SOCKET_L", "SOCKET_R", "SOCKET_U", "SOCKET_D" }, true, true),
                        boundaries: new[] { new SectorPlannerBoundarySnapshot(
                            SectorPlannerSide.Right, "PAIR_CRATER_ROOT", "BOUNDARY_CRATER_ROOT", 1) }),
                    Sector(Quiet, MoonpalaceBiomeId.MoonCrater, new[] { PacingRole.Quiet }, quiet: true),
                    Sector(Village, MoonpalaceBiomeId.CassiaRoot, new[] { PacingRole.Safe, PacingRole.Landmark },
                        special: new SectorPlannerSpecialRegionSnapshot("REGION_VILLAGE",
                            SectorPlannerSpecialRegionKind.Village, SectorPlannerSpecialRegionBinding.ReferenceOnly,
                            "FP_VILLAGE_REFERENCE", false, false, false), ordinal: 2, optionalDistance: 0),
                    Sector(Core, MoonpalaceBiomeId.CassiaRoot, new[] { PacingRole.Resource },
                        sites: new[] { new SectorPlannerSiteSnapshot("SITE_CORE", "CORE_RESOURCE", "RES_CORE", true) },
                        special: Mandatory("REGION_CORE", SectorPlannerSpecialRegionKind.CoreResource, "FP_CORE"), mandatoryDistance: 0),
                    Sector(Forge, MoonpalaceBiomeId.AbandonedMill, new[] { PacingRole.Landmark, PacingRole.Machinery },
                        sites: new[] { new SectorPlannerSiteSnapshot("SITE_FORGE", "FORGE", "RES_FORGE", true) },
                        special: Mandatory("REGION_FORGE", SectorPlannerSpecialRegionKind.Forge, "FP_FORGE"), mandatoryDistance: 0),
                    Sector(Boss, MoonpalaceBiomeId.MoonDough, new[] { PacingRole.Boss },
                        sites: new[] { new SectorPlannerSiteSnapshot("SITE_BOSS", "BOSS_GATE", "RES_BOSS", true) },
                        special: Mandatory("REGION_BOSS", SectorPlannerSpecialRegionKind.Boss, "FP_BOSS"), mandatoryDistance: 0),
                    Sector(Activity, MoonpalaceBiomeId.MoonCrater, new[] { PacingRole.Activity },
                        activity: true, eventAvailable: true, ordinal: 5),
                    Sector(Deferred, MoonpalaceBiomeId.CassiaRoot, new[] { PacingRole.Discovery },
                        special: new SectorPlannerSpecialRegionSnapshot("REGION_MERCHANT",
                            SectorPlannerSpecialRegionKind.Merchant, SectorPlannerSpecialRegionBinding.DeferredOptionalLocal,
                            string.Empty, false, false, false),
                        optional: new[] { new SectorPlannerOptionalRegionSnapshot("REGION_MERCHANT",
                            SectorPlannerSpecialRegionKind.Merchant, true, true, false) }, optionalDistance: 1),
                    Sector(Neighbor, MoonpalaceBiomeId.AbandonedMill, new[] { PacingRole.Traversal },
                        neighbors: new[]
                        {
                            new SectorPlannerNeighborSnapshot(SectorPlannerSide.Left, Deferred, 1,
                                AccessClass.MandatoryNoTool, Array.Empty<string>(), PacingRole.Traversal),
                            new SectorPlannerNeighborSnapshot(SectorPlannerSide.Right, new SectorCoord(10, 1), 1,
                                AccessClass.MandatoryNoTool, Array.Empty<string>(), PacingRole.Traversal),
                        }),
                };

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
                int optionalDistance = 3) =>
                new SectorPlannerSectorSnapshot(
                    coordinate, (coordinate.Y * 13) + coordinate.X, 48, 32,
                    new SectorPlannerBiomeSnapshot("PATCH_" + coordinate.X, biome.ToString()),
                    route ?? new SectorPlannerRouteSnapshot(1, AccessClass.MandatoryNoTool,
                        Array.Empty<string>(), false, false), boundaries, sites,
                    special ?? SectorPlannerSpecialRegionSnapshot.None, optional, neighbors,
                    new SectorPlannerWorldProgressSnapshot(ordinal, "CHAPTER_REFERENCE", "BRANCH_REFERENCE",
                        mandatoryDistance, optionalDistance), roles, quiet, activity, eventAvailable);

            private static SectorPlannerSpecialRegionSnapshot Mandatory(
                string id, SectorPlannerSpecialRegionKind kind, string footprint) =>
                new SectorPlannerSpecialRegionSnapshot(id, kind,
                    SectorPlannerSpecialRegionBinding.ReservedMandatory, footprint, true, true, true);

            private static List<SectorFixedAnchorProjection> CreateAnchors()
            {
                var result = new List<SectorFixedAnchorProjection>
                {
                    RouteAnchor("ANCHOR_SOCKET_L", "SOCKET_L", SectorPlannerSide.Left, new SectorFixedAnchorRect(0, 14, 1, 4)),
                    RouteAnchor("ANCHOR_SOCKET_R", "SOCKET_R", SectorPlannerSide.Right, new SectorFixedAnchorRect(47, 14, 1, 4)),
                    RouteAnchor("ANCHOR_SOCKET_U", "SOCKET_U", SectorPlannerSide.Up, new SectorFixedAnchorRect(22, 0, 4, 1)),
                    RouteAnchor("ANCHOR_SOCKET_D", "SOCKET_D", SectorPlannerSide.Down, new SectorFixedAnchorRect(22, 31, 4, 1)),
                    new SectorFixedAnchorProjection("ANCHOR_BOUNDARY_FIXED", Plain,
                        SectorFixedAnchorKind.BoundaryFixedSlice, SectorFixedAnchorSource.BoundarySnapshot,
                        SectorFixedAnchorPriority.BoundaryFixedSlice, new SectorFixedAnchorRect(47, 4, 1, 4),
                        "BOUNDARY_CRATER_ROOT", SectorPlannerSide.Right, true, "BOUNDARY_CRATER_ROOT"),
                    new SectorFixedAnchorProjection("ANCHOR_BOUNDARY_WARNING", Plain,
                        SectorFixedAnchorKind.BoundaryWarning, SectorFixedAnchorSource.BoundarySnapshot,
                        SectorFixedAnchorPriority.BoundaryWarning, new SectorFixedAnchorRect(47, 4, 1, 4),
                        "BOUNDARY_CRATER_ROOT", SectorPlannerSide.Right, true, "BOUNDARY_CRATER_ROOT"),
                    new SectorFixedAnchorProjection("ANCHOR_VILLAGE_REFERENCE", Village,
                        SectorFixedAnchorKind.ReferenceOnlyMarker, SectorFixedAnchorSource.SpecialRegionSnapshot,
                        SectorFixedAnchorPriority.ReferenceOnly, new SectorFixedAnchorRect(23, 15, 1, 1), "REGION_VILLAGE"),
                };
                AddSpecial(result, Core, "CORE", "REGION_CORE", "SITE_CORE");
                AddSpecial(result, Forge, "FORGE", "REGION_FORGE", "SITE_FORGE");
                AddSpecial(result, Boss, "BOSS", "REGION_BOSS", "SITE_BOSS");
                return result;
            }

            private static SectorFixedAnchorProjection RouteAnchor(
                string id, string source, SectorPlannerSide side, SectorFixedAnchorRect rect) =>
                new SectorFixedAnchorProjection(id, Plain, SectorFixedAnchorKind.ExternalRouteSocket,
                    SectorFixedAnchorSource.RouteSnapshot, SectorFixedAnchorPriority.ExternalRouteSocket,
                    rect, source, side);

            private static void AddSpecial(
                ICollection<SectorFixedAnchorProjection> result,
                SectorCoord coordinate,
                string token,
                string region,
                string site)
            {
                result.Add(new SectorFixedAnchorProjection("ANCHOR_" + token + "_FOOTPRINT", coordinate,
                    SectorFixedAnchorKind.SpecialFootprint, SectorFixedAnchorSource.SpecialRegionSnapshot,
                    SectorFixedAnchorPriority.SpecialReservation, new SectorFixedAnchorRect(18, 12, 12, 8),
                    region, placedOwnershipClaim: true, progressionBlockerClaim: true));
                result.Add(new SectorFixedAnchorProjection("ANCHOR_" + token + "_ENTRY", coordinate,
                    SectorFixedAnchorKind.SpecialEntryReturn, SectorFixedAnchorSource.SpecialRegionSnapshot,
                    SectorFixedAnchorPriority.SpecialTransition, new SectorFixedAnchorRect(16, 14, 2, 4),
                    region, placedOwnershipClaim: true, progressionBlockerClaim: true));
                result.Add(new SectorFixedAnchorProjection("ANCHOR_" + token + "_BUFFER", coordinate,
                    SectorFixedAnchorKind.SpecialApronBuffer, SectorFixedAnchorSource.SpecialRegionSnapshot,
                    SectorFixedAnchorPriority.SpecialTransition, new SectorFixedAnchorRect(30, 12, 2, 8),
                    region, placedOwnershipClaim: true, progressionBlockerClaim: true));
                result.Add(new SectorFixedAnchorProjection("ANCHOR_" + token + "_SITE", coordinate,
                    SectorFixedAnchorKind.SiteReservation, SectorFixedAnchorSource.SiteSnapshot,
                    SectorFixedAnchorPriority.SpecialReservation, new SectorFixedAnchorRect(20, 22, 4, 2),
                    site, placedOwnershipClaim: true));
            }

            private static List<SectorClusterSourceProjection> CreateClusterCatalog()
            {
                var access = new[] { AccessClass.MandatoryNoTool };
                var route = new[] { 1 };
                var sockets = new[] { "SOCKET_L", "SOCKET_R", "SOCKET_U", "SOCKET_D" };
                return new List<SectorClusterSourceProjection>
                {
                    Source("TC_REF_TRAVERSAL_BRIDGE", "SPINE_TRAVERSAL_R0", MoonpalaceBiomeId.MoonCrater, PacingRole.Traversal, route, access, sockets, H2(), Origins(2,1), false, false, 10),
                    Source("TC_REF_QUIET_BUFFER", "SPINE_QUIET_R0", MoonpalaceBiomeId.MoonCrater, PacingRole.Quiet, route, access, null, H2(), Origins(2,1), true, false, 20),
                    Source("TC_REF_VILLAGE_APPROACH", "SPINE_SAFE_R0", MoonpalaceBiomeId.CassiaRoot, PacingRole.Safe, route, access, null, H2(), Origins(2,1), false, true, 30),
                    Source("TC_REF_CORE_RESOURCE_RING", "SPINE_RESOURCE_R0", MoonpalaceBiomeId.CassiaRoot, PacingRole.Resource, route, access, null, H4(), Origins(4,1), false, true, 40),
                    Source("TC_REF_FORGE_MACHINERY", "SPINE_LANDMARK_R0", MoonpalaceBiomeId.AbandonedMill, PacingRole.Landmark, route, access, null, H4(), Origins(4,1), false, true, 50),
                    Source("TC_REF_BOSS_GATE", "SPINE_BOSS_R0", MoonpalaceBiomeId.MoonDough, PacingRole.Boss, route, access, null, Boss5(), Origins(4,2), false, true, 60),
                    Source("TC_REF_ACTIVITY_SHELL", "SPINE_ACTIVITY_R0", MoonpalaceBiomeId.MoonCrater, PacingRole.Activity, route, access, null, H2(), Origins(2,1), false, false, 70),
                    Source("TC_REF_DISCOVERY_PASSAGE", "SPINE_DISCOVERY_R0", MoonpalaceBiomeId.CassiaRoot, PacingRole.Discovery, route, access, null, H2(), Origins(2,1), false, true, 80),
                    Source("TC_REF_NEIGHBOR_FLOW", "SPINE_NEIGHBOR_R0", MoonpalaceBiomeId.AbandonedMill, PacingRole.Traversal, route, access, null, L3(), Origins(2,2), false, false, 90),
                };
            }

            private static SectorClusterSourceProjection Source(
                string cluster, string variant, MoonpalaceBiomeId biome, PacingRole pacing,
                IEnumerable<int> routes, IEnumerable<AccessClass> access, IEnumerable<string> sockets,
                IEnumerable<SectorClusterFootprintCell> cells, IEnumerable<SectorClusterFootprintCell> origins,
                bool quiet, bool special, int order) =>
                new SectorClusterSourceProjection(new TerrainClusterId(cluster), new SpineVariantId(variant),
                    ClusterFootprintTransform.R0, biome, new[] { pacing }, routes, access, sockets,
                    cells, origins, 2, 5, quiet, special, order, 0);

            private static List<SectorPatternSourceProjection> CreatePatternCatalog()
            {
                var roles = Enum.GetValues(typeof(SectorClusterRoleCellKind)).Cast<SectorClusterRoleCellKind>().ToArray();
                var pacing = Enum.GetValues(typeof(PacingRole)).Cast<PacingRole>().Where(value => value != PacingRole.None).ToArray();
                return new List<SectorPatternSourceProjection>
                {
                    Pattern("MP_REF_BODY", 1, new[] { SectorPatternZoneKind.ClusterBody }, roles, pacing, 10),
                    Pattern("MP_REF_EDGE", 2, new[] { SectorPatternZoneKind.ClusterEdge }, roles, pacing, 20),
                    Pattern("MP_REF_ROUTE", 3, new[] { SectorPatternZoneKind.RouteShoulder }, roles, pacing, 30),
                    Pattern("MP_REF_BOUNDARY", 4, new[] { SectorPatternZoneKind.BoundaryBlend }, roles, pacing, 40),
                    Pattern("MP_REF_SPECIAL", 5, new[] { SectorPatternZoneKind.SpecialApproach }, roles, pacing, 50),
                    Pattern("MP_REF_RECOVERY", 6, new[] { SectorPatternZoneKind.Recovery }, roles, pacing, 60),
                    Pattern("MP_REF_QUIET", 7, new[] { SectorPatternZoneKind.QuietBuffer }, roles, pacing, 70),
                    Pattern("MP_REF_DETAIL", 8, new[] { SectorPatternZoneKind.Detail }, roles, pacing, 80),
                    Pattern("MP_REF_PROTECTED", 9, new[] { SectorPatternZoneKind.ProtectedNoWrite }, roles, pacing, 90),
                };
            }

            private static SectorPatternSourceProjection Pattern(
                string id, int salt, IEnumerable<SectorPatternZoneKind> zones,
                IEnumerable<SectorClusterRoleCellKind> roles, IEnumerable<PacingRole> pacing, int order) =>
                new SectorPatternSourceProjection(PatternDefinition(id, salt), MicroPatternTransform.R0,
                    zones, roles, pacing, "SIG_" + id, order);

            private static MicroPatternDefinition PatternDefinition(string id, int salt)
            {
                var cells = new List<MicroPatternCell>();
                for (var y = 0; y < 4; y++)
                for (var x = 0; x < 4; x++)
                    cells.Add(new MicroPatternCell(new LocalTileCoord(x, y), new[]
                    {
                        new MicroPatternInstruction(MicroPatternLayer.Geometry,
                            (x + y + salt) % 3 == 0 ? MicroPatternOperation.AddSolid : MicroPatternOperation.CarveAir),
                        new MicroPatternInstruction(MicroPatternLayer.Surface, MicroPatternOperation.SetSurface, "SURFACE_" + id),
                        new MicroPatternInstruction(MicroPatternLayer.Material, MicroPatternOperation.SetMaterial, "MATERIAL_" + id),
                    }));
                return new MicroPatternDefinition(new MicroPatternId(id), 4, 4, cells, 10 + salt,
                    new[] { MoonpalaceBiomeId.MoonCrater, MoonpalaceBiomeId.CassiaRoot,
                        MoonpalaceBiomeId.AbandonedMill, MoonpalaceBiomeId.MoonDough },
                    new[] { MicroPatternTransform.R0 }, MicroPatternProtectedPolicy.ForceNoChange, id);
            }

            private static BiomePatchSnapshot Ownership()
            {
                var grouped = Enumerable.Range(0, WorldGenConstants.SectorCount)
                    .GroupBy(BiomeForIndex).OrderBy(value => value.Key.CanonicalId, StringComparer.Ordinal).ToArray();
                var patches = new List<BiomePatch>();
                var patchByBiome = new Dictionary<MoonpalaceBiomeId, BiomePatchId>();
                foreach (var group in grouped)
                {
                    var indices = group.ToArray();
                    var id = new BiomePatchId("PATCH_MAP14_06_" + BiomeToken(group.Key));
                    patchByBiome.Add(group.Key, id);
                    patches.Add(new BiomePatch(id, BiomeToken(group.Key), "RULE_MAP14_06",
                        BiomePatchRole.Satellite,
                        new[] { new BiomePatchSeed(indices[0], WorldGridIndex.ToCoordinate(indices[0]), BiomePatchRole.Satellite, null) },
                        indices));
                }
                var ownership = Enumerable.Range(0, WorldGenConstants.SectorCount).Select(index =>
                {
                    var biome = BiomeForIndex(index);
                    return new BiomeSectorOwnership(index, WorldGridIndex.ToCoordinate(index),
                        BiomeToken(biome), string.Empty, patchByBiome[biome]);
                }).ToArray();
                return new BiomePatchSnapshot(1406UL, patches, ownership, Array.Empty<BiomePatchSiteBinding>());
            }

            private static MoonpalaceBiomeId BiomeForIndex(int index)
            {
                if (index == 16 || index == 17 || index == 21) return MoonpalaceBiomeId.CassiaRoot;
                if (index == 18 || index == 22) return MoonpalaceBiomeId.AbandonedMill;
                if (index == 19) return MoonpalaceBiomeId.MoonDough;
                return MoonpalaceBiomeId.MoonCrater;
            }

            private static string BiomeToken(MoonpalaceBiomeId biome)
            {
                if (biome == MoonpalaceBiomeId.MoonCrater) return "BIO_MOON_CRATER";
                if (biome == MoonpalaceBiomeId.CassiaRoot) return "BIO_CASSIA_ROOT";
                if (biome == MoonpalaceBiomeId.AbandonedMill) return "BIO_ABANDONED_MILL";
                return "BIO_MOON_DOUGH";
            }

            private static MoonpalaceBiomeId Biome(SectorPlannerSectorSnapshot sector) =>
                sector.Coordinate == Village || sector.Coordinate == Core || sector.Coordinate == Deferred
                    ? MoonpalaceBiomeId.CassiaRoot
                    : sector.Coordinate == Forge || sector.Coordinate == Neighbor
                        ? MoonpalaceBiomeId.AbandonedMill
                        : sector.Coordinate == Boss ? MoonpalaceBiomeId.MoonDough : MoonpalaceBiomeId.MoonCrater;

            private static LocalTileCoord[] FindRectangle(SectorQuietFillPlan fill, SectorCoord sector)
            {
                var eligible = new HashSet<LocalTileCoord>(fill.Cells.Where(value => value.SectorCoordinate == sector && value.ActivityEligible)
                    .Select(value => value.Coordinate));
                for (var y = 0; y < 31; y++)
                for (var x = 0; x < 47; x++)
                {
                    var result = new[] { new LocalTileCoord(x, y), new LocalTileCoord(x + 1, y),
                        new LocalTileCoord(x, y + 1), new LocalTileCoord(x + 1, y + 1) };
                    if (result.All(eligible.Contains)) return result;
                }
                throw new InvalidOperationException("No eligible 2x2 Quiet rectangle in " + sector);
            }

            private static List<EventOverlayAssignmentProfile> EventProfiles()
            {
                var biomes = new[] { MoonpalaceBiomeId.MoonCrater, MoonpalaceBiomeId.CassiaRoot,
                    MoonpalaceBiomeId.AbandonedMill, MoonpalaceBiomeId.MoonDough };
                var pacing = Enum.GetValues(typeof(PacingRole)).Cast<PacingRole>().Where(value => value != PacingRole.None).ToArray();
                var access = new[] { AccessClass.MandatoryNoTool };
                var cluster = new TerrainClusterId("TC_MAP14_06_EVENT");
                var empty = new EventOverlayContract(new EventOverlayId("EVT_MAP14_06_EMPTY"),
                    EventOverlayKind.Empty, cluster, null, Array.Empty<EventMarkerAssignment>());
                var terrain = new EventOverlayContract(new EventOverlayId("EVT_MAP14_06_TERRAIN"),
                    EventOverlayKind.Cosmetic, cluster, null,
                    new[] { new EventMarkerAssignment(new EventMarkerId("MARKER_MAP14_06"),
                        EventMarkerOperation.EnableMarker, "MARKER_ONLY") });
                return new List<EventOverlayAssignmentProfile>
                {
                    new EventOverlayAssignmentProfile(empty, 0, 0, biomes, pacing, access),
                    new EventOverlayAssignmentProfile(terrain, 100, 2, biomes, pacing, access),
                };
            }

            private static SectorActivityEventMarkerKind MarkerForActivity(PacingRole role) =>
                role == PacingRole.Recovery ? SectorActivityEventMarkerKind.ActivityRecovery :
                role == PacingRole.Resource ? SectorActivityEventMarkerKind.ActivityReward :
                role == PacingRole.Activity ? SectorActivityEventMarkerKind.ActivityCore :
                SectorActivityEventMarkerKind.ActivityCue;

            private static SectorActivityEventMarkerKind MarkerForEvent(SectorPlannerSectorSnapshot sector) =>
                sector.SpecialRegion.Kind != SectorPlannerSpecialRegionKind.None
                    ? SectorActivityEventMarkerKind.EventSpecial
                    : sector.ActivityCatalogAvailable
                        ? SectorActivityEventMarkerKind.EventActivity
                        : SectorActivityEventMarkerKind.EventTerrain;

            private static DeterministicRngStreamFactory RngFactory()
            {
                var definitions = new SortedDictionary<string, RngStreamDefinition>(StringComparer.Ordinal)
                {
                    { WorldGenerationRngStreams.SectorRecipeStreamId,
                        Definition(WorldGenerationRngStreams.SectorRecipeStreamId, "E9931A70C2D520F4", "SECTOR") },
                    { WorldGenerationRngStreams.PopulationStreamId,
                        Definition(WorldGenerationRngStreams.PopulationStreamId, "A63D4078F9E21C55", "SPAWN") },
                };
                var set = (WorldRouteDefinitionSet)FormatterServices.GetUninitializedObject(typeof(WorldRouteDefinitionSet));
                SetAutoProperty(set, "RngStreams", new ReadOnlyDictionary<string, RngStreamDefinition>(definitions));
                return new DeterministicRngStreamFactory(set);
            }

            private static RngStreamDefinition Definition(string id, string salt, string scope)
            {
                var definition = (RngStreamDefinition)FormatterServices.GetUninitializedObject(typeof(RngStreamDefinition));
                SetAutoProperty(definition, "RngStreamId", id);
                SetAutoProperty(definition, "SaltHex", Hex(salt));
                SetAutoProperty(definition, "ResetScope", scope);
                SetAutoProperty(definition, "DescriptionKo", "MAP14_06 focused fixture");
                SetAutoProperty(definition, "Active", true);
                return definition;
            }

            private static CsvHexValue Hex(string value)
            {
                var bytes = Enumerable.Range(0, value.Length / 2)
                    .Select(index => byte.Parse(value.Substring(index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray();
                var constructor = typeof(CsvHexValue).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
                    null, new[] { typeof(string), typeof(IEnumerable<byte>) }, null);
                Assert.That(constructor, Is.Not.Null);
                return (CsvHexValue)constructor.Invoke(new object[] { value, bytes });
            }

            private static void SetAutoProperty(object target, string property, object value)
            {
                var field = target.GetType().GetField("<" + property + ">k__BackingField",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null, property);
                field.SetValue(target, value);
            }

            private static SectorClusterFootprintCell[] H2() => new[] { Cell(0, 0), Cell(1, 0) };
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

            private static void Require<T>(bool success, IEnumerable<T> errors)
            {
                if (!success) throw new InvalidOperationException(string.Join(";", errors));
            }

            private static string Hash(string value)
            {
                using (var sha = SHA256.Create())
                {
                    var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                    var result = new StringBuilder(bytes.Length * 2);
                    foreach (var item in bytes) result.Append(item.ToString("x2", CultureInfo.InvariantCulture));
                    return result.ToString();
                }
            }
        }

        private sealed class AuthorityPackage
        {
            internal AuthorityPackage(
                IEnumerable<SectorActivityOpportunityProjection> activities,
                ActivityCandidateIndex activityIndex,
                ActivityFrequencyPlan activityPlan,
                IEnumerable<SectorEventMarkerOpportunityProjection> events,
                EventOverlayCandidateIndex eventIndex,
                EventOverlayAssignmentPlan eventPlan)
            {
                Activities = activities.ToArray();
                ActivityIndex = activityIndex;
                ActivityPlan = activityPlan;
                Events = events.ToArray();
                EventIndex = eventIndex;
                EventPlan = eventPlan;
            }

            internal SectorActivityOpportunityProjection[] Activities { get; }
            internal ActivityCandidateIndex ActivityIndex { get; }
            internal ActivityFrequencyPlan ActivityPlan { get; }
            internal SectorEventMarkerOpportunityProjection[] Events { get; }
            internal EventOverlayCandidateIndex EventIndex { get; }
            internal EventOverlayAssignmentPlan EventPlan { get; }

            internal SectorActivityEventPlacementRequest Request(
                SectorQuietFillPlan fill,
                IEnumerable<SectorQuietActivityEventErrorCode> referenceFaults = null,
                bool activityMarkerMutationClaim = false,
                bool eventMarkerMutationClaim = false,
                bool specialPersistenceMutationClaim = false,
                bool ownershipMutationClaim = false,
                int solverInvocationCount = 0,
                int map14RngDrawCount = 0,
                int retryCount = 0,
                int tileWriteCount = 0) =>
                new SectorActivityEventPlacementRequest(
                    fill, Activities, ActivityIndex, ActivityPlan, Events, EventIndex, EventPlan,
                    SectorActivityEventPlacementPlanner.ReferencePublicationLabel,
                    referenceFaults: referenceFaults,
                    activityMarkerMutationClaim: activityMarkerMutationClaim,
                    eventMarkerMutationClaim: eventMarkerMutationClaim,
                    specialPersistenceMutationClaim: specialPersistenceMutationClaim,
                    ownershipMutationClaim: ownershipMutationClaim,
                    solverInvocationCount: solverInvocationCount,
                    map14RngDrawCount: map14RngDrawCount,
                    retryCount: retryCount,
                    tileWriteCount: tileWriteCount);
        }
    }
}
