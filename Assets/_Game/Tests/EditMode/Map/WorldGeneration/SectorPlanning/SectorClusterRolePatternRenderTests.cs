using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.SectorPlanning
{
    [TestFixture]
    [Category("MAP14_05")]
    public sealed class SectorClusterRolePatternRenderTests
    {
        [Test]
        public void BuildPublishesClusterRoleCellsAndPatternZonesFromSpineEnvelope()
        {
            var fixture = Fixture.Create();
            var roleResult = fixture.BuildRoles();
            Assert.That(roleResult.Success, Is.True, Join(roleResult.Errors));
            var renderResult = fixture.Render(roleResult.Plan);
            Assert.That(renderResult.Success, Is.True, Join(renderResult.Errors));
            var rolePlan = roleResult.Plan;
            var renderPlan = renderResult.Plan;

            Assert.That(rolePlan.SectorCount, Is.EqualTo(9));
            Assert.That(rolePlan.ClusterPlacementCount, Is.EqualTo(9));
            Assert.That(rolePlan.RoleCellCount, Is.EqualTo(fixture.PlacementPlan.PlacedFootprintCellCount));
            Assert.That(rolePlan.PatternZoneCount, Is.EqualTo(rolePlan.RoleCellCount * 6));
            Assert.That(rolePlan.Count(SectorClusterRoleCellKind.BoundaryApproach), Is.GreaterThan(0));
            Assert.That(rolePlan.Count(SectorClusterRoleCellKind.SpecialApproach), Is.GreaterThan(0));
            Assert.That(rolePlan.Count(SectorClusterRoleCellKind.RecoverySupport), Is.GreaterThan(0));
            Assert.That(rolePlan.Count(SectorClusterRoleCellKind.QuietBuffer), Is.GreaterThan(0));
            Assert.That(rolePlan.Count(SectorClusterRoleCellKind.PatternFill), Is.GreaterThan(0));
            Assert.That(rolePlan.Count(SectorPatternZoneKind.ClusterBody), Is.GreaterThan(0));
            Assert.That(rolePlan.Count(SectorPatternZoneKind.ClusterEdge), Is.GreaterThan(0));
            Assert.That(rolePlan.Count(SectorPatternZoneKind.BoundaryBlend), Is.GreaterThan(0));
            Assert.That(rolePlan.Count(SectorPatternZoneKind.SpecialApproach), Is.GreaterThan(0));
            Assert.That(rolePlan.Count(SectorPatternZoneKind.Recovery), Is.GreaterThan(0));
            Assert.That(rolePlan.Count(SectorPatternZoneKind.QuietBuffer), Is.GreaterThan(0));
            Assert.That(rolePlan.Count(SectorPatternZoneKind.Detail), Is.GreaterThan(0));
            Assert.That(renderPlan.SelectedPatternCount, Is.EqualTo(rolePlan.PatternZoneCount));
            Assert.That(renderPlan.ApplicationPlanCount, Is.EqualTo(rolePlan.PatternZoneCount));
            Assert.That(renderPlan.Map14_06HandoffReady, Is.True);
            AssertLowerSha(rolePlan.CanonicalDigest);
            AssertLowerSha(renderPlan.CanonicalDigest);

            TestContext.Out.WriteLine("MAP14_05_METRIC sectors=" + renderPlan.SectorCount);
            TestContext.Out.WriteLine("MAP14_05_METRIC clusters=" + renderPlan.ClusterPlacementCount);
            TestContext.Out.WriteLine("MAP14_05_METRIC roleCells=" + renderPlan.RoleCellCount);
            foreach (SectorClusterRoleCellKind kind in Enum.GetValues(typeof(SectorClusterRoleCellKind)))
                TestContext.Out.WriteLine("MAP14_05_ROLE " + kind + "=" + rolePlan.Count(kind));
            TestContext.Out.WriteLine("MAP14_05_METRIC patternZones=" + renderPlan.PatternZoneCount);
            foreach (SectorPatternZoneKind kind in Enum.GetValues(typeof(SectorPatternZoneKind)))
                TestContext.Out.WriteLine("MAP14_05_ZONE " + kind + "=" + rolePlan.Count(kind));
            TestContext.Out.WriteLine("MAP14_05_METRIC selectedPatterns=" + renderPlan.SelectedPatternCount);
            TestContext.Out.WriteLine("MAP14_05_METRIC applicationPlans=" + renderPlan.ApplicationPlanCount);
            TestContext.Out.WriteLine("MAP14_05_METRIC rendererInvocations=" + renderPlan.RendererInvocationCount);
            TestContext.Out.WriteLine("MAP14_05_METRIC renderTargetCells=" + renderPlan.RenderTargetCellCount);
            TestContext.Out.WriteLine("MAP14_05_METRIC changedCells=" + renderPlan.RenderedChangedCellCount);
            TestContext.Out.WriteLine("MAP14_05_METRIC noChangeCells=" + renderPlan.IdempotentNoChangeCellCount);
            TestContext.Out.WriteLine("MAP14_05_METRIC appliedWrites=" + renderPlan.AppliedWriteCount);
            TestContext.Out.WriteLine("MAP14_05_METRIC idempotentWrites=" + renderPlan.IdempotentWriteCount);
            TestContext.Out.WriteLine("MAP14_05_METRIC protectedMaskHits=" + renderPlan.ProtectedMaskHitCount);
            TestContext.Out.WriteLine("MAP14_05_METRIC protectedPreventedWrites=" + renderPlan.ProtectedPreventedWriteCount);
            TestContext.Out.WriteLine("MAP14_05_METRIC protectedWrites=" + renderPlan.ProtectedWriteCount);
            TestContext.Out.WriteLine("MAP14_05_METRIC rendererConflicts=" + renderPlan.RendererConflictCount);
            TestContext.Out.WriteLine("MAP14_05_METRIC zoneOverlaps=" + renderPlan.PatternZoneOverlapCount);
            TestContext.Out.WriteLine("MAP14_05_METRIC outOfClusterZones=" + renderPlan.OutOfClusterZoneCount);
            TestContext.Out.WriteLine("MAP14_05_DIGEST roleZone=" + rolePlan.CanonicalDigest);
            TestContext.Out.WriteLine("MAP14_05_DIGEST patternCatalog=" + renderPlan.SourcePatternCatalogDigestBefore);
            TestContext.Out.WriteLine("MAP14_05_DIGEST render=" + renderPlan.CanonicalDigest);
            TestContext.Out.WriteLine("MAP14_05_MAP10 applicationPlanner=" + renderPlan.Map10ApplicationPlannerType);
            TestContext.Out.WriteLine("MAP14_05_MAP10 orderedRenderer=" + renderPlan.Map10OrderedRendererType);
            TestContext.Out.WriteLine("MAP14_05_MAP10 ruleset=" + renderPlan.Map10RenderRulesetVersion);
        }

        [Test]
        public void RoleCellsCoverPlacedClusterFootprintsExactlyOnce()
        {
            var fixture = Fixture.Create();
            var result = fixture.BuildRoles();
            Assert.That(result.Success, Is.True, Join(result.Errors));

            var expected = fixture.PlacementPlan.Placements.SelectMany(placement =>
                placement.Cells.Select(cell => Key(placement.SectorIndex, cell))).OrderBy(value => value).ToArray();
            var actual = result.Plan.RoleCells.Select(value =>
                Key(value.SectorIndex, value.FootprintCell)).OrderBy(value => value).ToArray();
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual.Distinct().Count(), Is.EqualTo(actual.Length));
            Assert.That(result.Plan.RoleCells.All(value => value.TileRect.IsInside(48, 32)), Is.True);
        }

        [Test]
        public void PatternZonesPartitionRoleCellsIntoAlignedFourByFourSlots()
        {
            var result = Fixture.Create().BuildRoles();
            Assert.That(result.Success, Is.True, Join(result.Errors));
            var plan = result.Plan;

            foreach (var role in plan.RoleCells)
            {
                var zones = plan.PatternZones.Where(value => value.SectorIndex == role.SectorIndex &&
                    value.OwnerCell.Equals(role.FootprintCell)).ToArray();
                Assert.That(zones.Length, Is.EqualTo(6));
                Assert.That(zones.All(value => value.TileRect.Width == 4 && value.TileRect.Height == 4), Is.True);
                Assert.That(zones.All(value => value.TileRect.X % 4 == 0 && value.TileRect.Y % 4 == 0), Is.True);
                Assert.That(zones.Sum(value => value.TileRect.Width * value.TileRect.Height), Is.EqualTo(96));
            }

            Assert.That(plan.PatternZoneOverlapCount, Is.Zero);
            Assert.That(plan.OutOfClusterZoneCount, Is.Zero);
        }

        [Test]
        public void ProtectedOpenBoundaryAndSpecialEntryCellsReceiveNoPatternWrites()
        {
            var fixture = Fixture.Create();
            var roleResult = fixture.BuildRoles();
            var renderResult = fixture.Render(roleResult.Plan);
            Assert.That(renderResult.Success, Is.True, Join(renderResult.Errors));
            var plan = renderResult.Plan;

            Assert.That(plan.ProtectedMaskHitCount, Is.GreaterThan(0));
            Assert.That(plan.ProtectedPreventedWriteCount, Is.GreaterThan(0));
            Assert.That(plan.ProtectedWriteCount, Is.Zero);
            Assert.That(roleResult.Plan.ProtectionEvidence.Any(value =>
                value.SourceKind == MicroPatternProtectedSourceKind.RouteSpine), Is.True);
            Assert.That(roleResult.Plan.ProtectionEvidence.Any(value =>
                value.SourceKind == MicroPatternProtectedSourceKind.BoundaryProtectedOpen), Is.True);
            Assert.That(roleResult.Plan.ProtectionEvidence.Any(value =>
                value.SourceKind == MicroPatternProtectedSourceKind.SpecialFixedEntry), Is.True);

            var protectedKeys = new HashSet<string>(roleResult.Plan.ProtectionEvidence.Select(value =>
                value.SectorIndex + "|" + value.Coordinate.X + "|" + value.Coordinate.Y));
            var renderedProtected = plan.RenderCells.Where(value => protectedKeys.Contains(
                value.SectorIndex + "|" + value.Coordinate.X + "|" + value.Coordinate.Y)).ToArray();
            Assert.That(renderedProtected.Length, Is.GreaterThan(0));
            Assert.That(renderedProtected.All(value => !value.Changed && value.AppliedWriteCount == 0), Is.True);
        }

        [Test]
        public void RenderUsesMap10ApplicationPlannerAndOrderedRenderer()
        {
            var fixture = Fixture.Create();
            var roleResult = fixture.BuildRoles();
            var result = fixture.Render(roleResult.Plan);
            Assert.That(result.Success, Is.True, Join(result.Errors));
            var plan = result.Plan;

            Assert.That(plan.ApplicationPlanCount, Is.EqualTo(plan.SelectedPatternCount));
            Assert.That(plan.SelectedPatternCount, Is.EqualTo(plan.PatternZoneCount));
            Assert.That(plan.RendererInvocationCount, Is.EqualTo(plan.SectorCount));
            Assert.That(plan.RendererDeltaDigests.Count, Is.EqualTo(plan.RendererInvocationCount));
            Assert.That(plan.Map10ApplicationPlannerType, Is.EqualTo(typeof(MicroPatternApplicationPlanner).FullName));
            Assert.That(plan.Map10OrderedRendererType, Is.EqualTo(typeof(MicroPatternOrderedRenderer).FullName));
            Assert.That(plan.Map10RenderRulesetVersion, Is.EqualTo(MicroPatternRenderDelta.RulesetVersion));
            Assert.That(plan.RendererDeltaDigests.All(IsLowerSha), Is.True);
        }

        [Test]
        public void RenderedPatternCanvasIsInMemoryAndDoesNotFinalizeOwnership()
        {
            var fixture = Fixture.Create();
            var roleResult = fixture.BuildRoles();
            var result = fixture.Render(roleResult.Plan);
            Assert.That(result.Success, Is.True, Join(result.Errors));
            var plan = result.Plan;

            Assert.That(plan.RenderTargetCellCount, Is.EqualTo(plan.RoleCellCount * 96));
            Assert.That(plan.RenderedChangedCellCount, Is.GreaterThan(0));
            Assert.That(plan.IdempotentNoChangeCellCount, Is.GreaterThan(0));
            Assert.That(plan.TileWriteCount, Is.Zero);
            Assert.That(plan.CanvasOwnershipWriteCount, Is.Zero);
            Assert.That(plan.ActivityEventPlacementCount, Is.Zero);
            Assert.That(plan.SceneMutationCount, Is.Zero);
            Assert.That(plan.PrefabMutationCount, Is.Zero);
            Assert.That(plan.GameObjectMutationCount, Is.Zero);
            Assert.That(plan.AssetWriteCount, Is.Zero);
            Assert.That(plan.RendererConflictCount, Is.Zero);
        }

        [Test]
        public void PatternSelectionIsDeterministicWithoutRngOrRetry()
        {
            var fixture = Fixture.Create();
            var roles = fixture.BuildRoles().Plan;
            var first = fixture.Render(roles).Plan;
            var second = fixture.Render(roles, reversePatterns: true).Plan;

            Assert.That(second.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));
            Assert.That(second.Selections.Select(value => value.PatternId.Value),
                Is.EqualTo(first.Selections.Select(value => value.PatternId.Value)));
            Assert.That(first.Selections.Select(value => value.RepetitionSignature).Distinct().Count(),
                Is.GreaterThan(1));
            Assert.That(first.RandomDrawCount, Is.Zero);
            Assert.That(first.RetryCount, Is.Zero);
            Assert.That(first.SolverInvocationCount, Is.Zero);
        }

        [Test]
        public void SpecialVillageOptionalAndActivityBoundariesRemainNonOwning()
        {
            var fixture = Fixture.Create();
            var roles = fixture.BuildRoles().Plan;
            var render = fixture.Render(roles).Plan;

            Assert.That(roles.Count(SectorClusterRoleCellKind.SpecialApproach), Is.GreaterThan(0));
            Assert.That(roles.Count(SectorPatternZoneKind.SpecialApproach), Is.GreaterThan(0));
            Assert.That(roles.RoleCells.Any(value => value.SectorCoordinate.Equals(new SectorCoord(3, 1))), Is.True);
            Assert.That(roles.RoleCells.Any(value => value.SectorCoordinate.Equals(new SectorCoord(8, 1))), Is.True);
            Assert.That(roles.RoleCells.Any(value => value.SectorCoordinate.Equals(new SectorCoord(7, 1))), Is.True);
            Assert.That(render.ActivityEventPlacementCount, Is.Zero);
            Assert.That(render.CanvasOwnershipWriteCount, Is.Zero);
            Assert.That(render.SpecialIdentityBefore, Is.EqualTo(render.SpecialIdentityAfter));
            Assert.That(render.ProtectedOpenIdentityBefore, Is.EqualTo(render.ProtectedOpenIdentityAfter));
        }

        [Test]
        public void InvalidMissingPatternProtectedConflictAndMutationClaimsFailAtomically()
        {
            var fixture = Fixture.Create();
            var missingSpine = SectorClusterRoleZoneBuilder.Build(new SectorClusterRoleZoneBuildRequest(
                fixture.Input, fixture.Assignments, fixture.AnchorPlan, fixture.PlacementPlan, null,
                SectorClusterRoleZoneBuilder.ReferencePublicationLabel));
            AssertAtomic(missingSpine, SectorPatternRenderErrorCode.MissingSpineEnvelopePlan);

            var roles = fixture.BuildRoles().Plan;
            var missingPattern = SectorPatternRenderPlanner.Render(new SectorPatternRenderRequest(
                roles, Array.Empty<SectorPatternSourceProjection>(),
                SectorPatternRenderPlanner.ReferencePublicationLabel));
            AssertAtomic(missingPattern, SectorPatternRenderErrorCode.MissingPatternCandidate);

            var protectedFault = fixture.RenderFault(roles, SectorPatternRenderErrorCode.ProtectedWriteAttempt);
            AssertAtomic(protectedFault, SectorPatternRenderErrorCode.ProtectedWriteAttempt);
            var conflictFault = fixture.RenderFault(roles, SectorPatternRenderErrorCode.RendererConflict);
            AssertAtomic(conflictFault, SectorPatternRenderErrorCode.RendererConflict);
            var mutation = fixture.RenderMutation(roles);
            Assert.That(mutation.Success, Is.False);
            Assert.That(mutation.Plan, Is.Null);
            Assert.That(mutation.CanonicalDigest, Is.Empty);
            Assert.That(mutation.Errors.Select(value => value.Code), Does.Contain(
                SectorPatternRenderErrorCode.RouteAccessMutationClaim));
            Assert.That(mutation.Errors.Select(value => value.Code), Does.Contain(
                SectorPatternRenderErrorCode.RngMutationClaim));
            Assert.That(mutation.Errors, Is.Ordered);
        }

        [Test]
        public void PublicationIsDeterministicAcrossRepeatReverseAndTurkishCulture()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var firstFixture = Fixture.Create();
                var firstRoles = firstFixture.BuildRoles().Plan;
                var firstRender = firstFixture.Render(firstRoles).Plan;

                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var reversedFixture = Fixture.Create(reverse: true);
                var reversedRoles = reversedFixture.BuildRoles().Plan;
                var reversedRender = reversedFixture.Render(reversedRoles, reversePatterns: true).Plan;

                Assert.That(reversedRoles.CanonicalDigest, Is.EqualTo(firstRoles.CanonicalDigest));
                Assert.That(reversedRender.CanonicalDigest, Is.EqualTo(firstRender.CanonicalDigest));
                Assert.That(reversedRender.SourcePatternCatalogDigestBefore,
                    Is.EqualTo(firstRender.SourcePatternCatalogDigestBefore));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        private static void AssertAtomic(
            SectorClusterRoleZoneBuildResult result,
            SectorPatternRenderErrorCode code)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(code));
            Assert.That(result.Errors, Is.Ordered);
        }

        private static void AssertAtomic(
            SectorPatternRenderBuildResult result,
            SectorPatternRenderErrorCode code)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(code));
            Assert.That(result.Errors, Is.Ordered);
        }

        private static void AssertLowerSha(string value)
        {
            Assert.That(value, Has.Length.EqualTo(64));
            Assert.That(value.All(character => character >= '0' && character <= '9' ||
                                               character >= 'a' && character <= 'f'), Is.True);
        }

        private static bool IsLowerSha(string value) => value != null && value.Length == 64 &&
            value.All(character => character >= '0' && character <= '9' ||
                                   character >= 'a' && character <= 'f');

        private static string Key(int sectorIndex, SectorClusterFootprintCell cell) =>
            sectorIndex + "|" + cell.X + "|" + cell.Y;
        private static string Join<T>(IEnumerable<T> values) =>
            string.Join("\n", values.Select(value => value == null ? "<null>" : value.ToString()));

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

            private Fixture(
                SectorPlannerInput input,
                IReadOnlyList<SectorPacingAssignment> assignments,
                SectorFixedAnchorPlan anchorPlan,
                SectorClusterPlacementPlan placementPlan,
                SectorSpineEnvelopePlan spineEnvelopePlan)
            {
                Input = input;
                Assignments = assignments;
                AnchorPlan = anchorPlan;
                PlacementPlan = placementPlan;
                SpineEnvelopePlan = spineEnvelopePlan;
            }

            internal SectorPlannerInput Input { get; }
            internal IReadOnlyList<SectorPacingAssignment> Assignments { get; }
            internal SectorFixedAnchorPlan AnchorPlan { get; }
            internal SectorClusterPlacementPlan PlacementPlan { get; }
            internal SectorSpineEnvelopePlan SpineEnvelopePlan { get; }

            internal static Fixture Create(bool reverse = false)
            {
                var sectors = CreateSectors();
                if (reverse) sectors.Reverse();
                var authority = SectorPlannerAuthorityDigestSnapshot.CaptureCurrentPublicAuthorities(
                    Digest('a'), Digest('b'), 24, Digest('c'), 16, Digest('d'), 7,
                    Digest('e'), 5, Digest('f'));
                var inputResult = SectorPlannerInputBuilder.Build(new SectorPlannerInputRequest(
                    sectors, authority, SectorPlannerInputBuilder.ReferencePublicationLabel));
                Require(inputResult.Success, inputResult.Errors);

                var assignments = SectorPacingRolePlanner.Assign(inputResult.Input).ToList();
                var projections = CreateAnchors();
                if (reverse)
                {
                    assignments.Reverse();
                    projections.Reverse();
                }
                var anchorResult = SectorFixedAnchorPlanner.Build(new SectorFixedAnchorBuildRequest(
                    inputResult.Input, assignments, projections,
                    SectorFixedAnchorPlanner.ReferencePublicationLabel));
                Require(anchorResult.Success, anchorResult.Errors);

                var catalog = CreateClusterCatalog();
                if (reverse) catalog.Reverse();
                var candidates = SectorClusterCandidateBuilder.Build(new SectorClusterCandidateBuildRequest(
                    inputResult.Input, assignments, anchorResult.Plan, catalog,
                    SectorClusterCandidateBuilder.ReferenceCandidatePublicationLabel));
                Require(candidates.Success, candidates.Errors);
                var placements = SectorClusterPlacementPlanner.Place(new SectorClusterPlacementRequest(
                    candidates.CandidateSet, anchorResult.Plan,
                    SectorClusterPlacementPlanner.ReferencePlacementPublicationLabel));
                Require(placements.Success, placements.Errors);

                var spineRequest = new SectorSpineEnvelopeBuildRequest(
                    inputResult.Input, assignments, anchorResult.Plan, placements.Plan,
                    SectorSpineGraphBuilder.ReferenceGraphPublicationLabel,
                    SectorTraversalEnvelopeBuilder.ReferenceEnvelopePublicationLabel);
                var graph = SectorSpineGraphBuilder.Build(spineRequest);
                Require(graph.Success, graph.Errors);
                var envelope = SectorTraversalEnvelopeBuilder.Build(spineRequest, graph.Graph);
                Require(envelope.Success, envelope.Errors);
                return new Fixture(inputResult.Input, assignments, anchorResult.Plan,
                    placements.Plan, envelope.Plan);
            }

            internal SectorClusterRoleZoneBuildResult BuildRoles() =>
                SectorClusterRoleZoneBuilder.Build(new SectorClusterRoleZoneBuildRequest(
                    Input, Assignments, AnchorPlan, PlacementPlan, SpineEnvelopePlan,
                    SectorClusterRoleZoneBuilder.ReferencePublicationLabel));

            internal SectorPatternRenderBuildResult Render(
                SectorClusterRolePatternPlan roles,
                bool reversePatterns = false) =>
                SectorPatternRenderPlanner.Render(new SectorPatternRenderRequest(
                    roles, CreatePatternCatalog(reversePatterns),
                    SectorPatternRenderPlanner.ReferencePublicationLabel));

            internal SectorPatternRenderBuildResult RenderFault(
                SectorClusterRolePatternPlan roles,
                SectorPatternRenderErrorCode fault) =>
                SectorPatternRenderPlanner.Render(new SectorPatternRenderRequest(
                    roles, CreatePatternCatalog(false),
                    SectorPatternRenderPlanner.ReferencePublicationLabel,
                    sourceReferenceFaults: new[] { fault }));

            internal SectorPatternRenderBuildResult RenderMutation(
                SectorClusterRolePatternPlan roles) =>
                SectorPatternRenderPlanner.Render(new SectorPatternRenderRequest(
                    roles, CreatePatternCatalog(false),
                    SectorPatternRenderPlanner.ReferencePublicationLabel,
                    routeAccessMutationClaim: true,
                    anchorMutationClaim: true,
                    clusterMutationClaim: true,
                    spineEnvelopeMutationClaim: true,
                    activityMutationClaim: true,
                    ownershipMutationClaim: true,
                    solverInvocationCount: 1,
                    randomDrawCount: 1,
                    retryCount: 1,
                    tileWriteCount: 1,
                    sceneMutationCount: 1));

            private static List<SectorPlannerSectorSnapshot> CreateSectors()
            {
                return new List<SectorPlannerSectorSnapshot>
                {
                    Sector(Plain, MoonpalaceBiomeId.MoonCrater,
                        new[] { PacingRole.Traversal, PacingRole.Recovery },
                        route: new SectorPlannerRouteSnapshot(1, AccessClass.MandatoryNoTool,
                            new[] { "SOCKET_L", "SOCKET_R", "SOCKET_U", "SOCKET_D" }, true, true),
                        boundaries: new[] { new SectorPlannerBoundarySnapshot(
                            SectorPlannerSide.Right, "PAIR_CRATER_ROOT", "BOUNDARY_CRATER_ROOT", 1) }),
                    Sector(Quiet, MoonpalaceBiomeId.MoonCrater,
                        new[] { PacingRole.Quiet }, quiet: true),
                    Sector(Village, MoonpalaceBiomeId.CassiaRoot,
                        new[] { PacingRole.Safe, PacingRole.Landmark },
                        special: new SectorPlannerSpecialRegionSnapshot(
                            "REGION_VILLAGE", SectorPlannerSpecialRegionKind.Village,
                            SectorPlannerSpecialRegionBinding.ReferenceOnly,
                            "FP_VILLAGE_REFERENCE", false, false, false),
                        ordinal: 2, optionalDistance: 0),
                    Sector(Core, MoonpalaceBiomeId.CassiaRoot,
                        new[] { PacingRole.Resource },
                        sites: new[] { new SectorPlannerSiteSnapshot(
                            "SITE_CORE", "CORE_RESOURCE", "RES_CORE", true) },
                        special: Mandatory("REGION_CORE", SectorPlannerSpecialRegionKind.CoreResource, "FP_CORE"),
                        mandatoryDistance: 0),
                    Sector(Forge, MoonpalaceBiomeId.AbandonedMill,
                        new[] { PacingRole.Landmark, PacingRole.Machinery },
                        sites: new[] { new SectorPlannerSiteSnapshot(
                            "SITE_FORGE", "FORGE", "RES_FORGE", true) },
                        special: Mandatory("REGION_FORGE", SectorPlannerSpecialRegionKind.Forge, "FP_FORGE"),
                        mandatoryDistance: 0),
                    Sector(Boss, MoonpalaceBiomeId.MoonDough,
                        new[] { PacingRole.Boss },
                        sites: new[] { new SectorPlannerSiteSnapshot(
                            "SITE_BOSS", "BOSS_GATE", "RES_BOSS", true) },
                        special: Mandatory("REGION_BOSS", SectorPlannerSpecialRegionKind.Boss, "FP_BOSS"),
                        mandatoryDistance: 0),
                    Sector(Activity, MoonpalaceBiomeId.MoonCrater,
                        new[] { PacingRole.Activity }, activity: true, eventAvailable: true, ordinal: 5),
                    Sector(Deferred, MoonpalaceBiomeId.CassiaRoot,
                        new[] { PacingRole.Discovery },
                        special: new SectorPlannerSpecialRegionSnapshot(
                            "REGION_MERCHANT", SectorPlannerSpecialRegionKind.Merchant,
                            SectorPlannerSpecialRegionBinding.DeferredOptionalLocal,
                            string.Empty, false, false, false),
                        optional: new[] { new SectorPlannerOptionalRegionSnapshot(
                            "REGION_MERCHANT", SectorPlannerSpecialRegionKind.Merchant,
                            true, true, false) }, optionalDistance: 1),
                    Sector(Neighbor, MoonpalaceBiomeId.AbandonedMill,
                        new[] { PacingRole.Traversal },
                        neighbors: new[]
                        {
                            new SectorPlannerNeighborSnapshot(SectorPlannerSide.Left,
                                new SectorCoord(8, 1), 1, AccessClass.MandatoryNoTool,
                                Array.Empty<string>(), PacingRole.Traversal),
                            new SectorPlannerNeighborSnapshot(SectorPlannerSide.Right,
                                new SectorCoord(10, 1), 1, AccessClass.MandatoryNoTool,
                                Array.Empty<string>(), PacingRole.Traversal),
                            new SectorPlannerNeighborSnapshot(SectorPlannerSide.Up,
                                new SectorCoord(9, 0), 1, AccessClass.MandatoryNoTool,
                                Array.Empty<string>(), PacingRole.Traversal),
                            new SectorPlannerNeighborSnapshot(SectorPlannerSide.Down,
                                new SectorCoord(9, 2), 1, AccessClass.MandatoryNoTool,
                                Array.Empty<string>(), PacingRole.Traversal),
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
                    coordinate,
                    (coordinate.Y * 13) + coordinate.X,
                    48,
                    32,
                    new SectorPlannerBiomeSnapshot("PATCH_" + coordinate.X, biome.ToString()),
                    route ?? new SectorPlannerRouteSnapshot(
                        1, AccessClass.MandatoryNoTool, Array.Empty<string>(), false, false),
                    boundaries,
                    sites,
                    special ?? SectorPlannerSpecialRegionSnapshot.None,
                    optional,
                    neighbors,
                    new SectorPlannerWorldProgressSnapshot(
                        ordinal, "CHAPTER_REFERENCE", "BRANCH_REFERENCE",
                        mandatoryDistance, optionalDistance),
                    roles,
                    quiet,
                    activity,
                    eventAvailable);
            }

            private static SectorPlannerSpecialRegionSnapshot Mandatory(
                string id,
                SectorPlannerSpecialRegionKind kind,
                string footprint) =>
                new SectorPlannerSpecialRegionSnapshot(
                    id, kind, SectorPlannerSpecialRegionBinding.ReservedMandatory,
                    footprint, true, true, true);

            private static List<SectorFixedAnchorProjection> CreateAnchors()
            {
                var result = new List<SectorFixedAnchorProjection>
                {
                    RouteAnchor("ANCHOR_SOCKET_L", "SOCKET_L", SectorPlannerSide.Left,
                        new SectorFixedAnchorRect(0, 14, 1, 4)),
                    RouteAnchor("ANCHOR_SOCKET_R", "SOCKET_R", SectorPlannerSide.Right,
                        new SectorFixedAnchorRect(47, 14, 1, 4)),
                    RouteAnchor("ANCHOR_SOCKET_U", "SOCKET_U", SectorPlannerSide.Up,
                        new SectorFixedAnchorRect(22, 0, 4, 1)),
                    RouteAnchor("ANCHOR_SOCKET_D", "SOCKET_D", SectorPlannerSide.Down,
                        new SectorFixedAnchorRect(22, 31, 4, 1)),
                    new SectorFixedAnchorProjection(
                        "ANCHOR_BOUNDARY_FIXED", Plain, SectorFixedAnchorKind.BoundaryFixedSlice,
                        SectorFixedAnchorSource.BoundarySnapshot,
                        SectorFixedAnchorPriority.BoundaryFixedSlice,
                        new SectorFixedAnchorRect(47, 4, 1, 4),
                        "BOUNDARY_CRATER_ROOT", SectorPlannerSide.Right, true,
                        "BOUNDARY_CRATER_ROOT"),
                    new SectorFixedAnchorProjection(
                        "ANCHOR_BOUNDARY_WARNING", Plain, SectorFixedAnchorKind.BoundaryWarning,
                        SectorFixedAnchorSource.BoundarySnapshot,
                        SectorFixedAnchorPriority.BoundaryWarning,
                        new SectorFixedAnchorRect(47, 4, 1, 4),
                        "BOUNDARY_CRATER_ROOT", SectorPlannerSide.Right, true,
                        "BOUNDARY_CRATER_ROOT"),
                    new SectorFixedAnchorProjection(
                        "ANCHOR_VILLAGE_REFERENCE", Village,
                        SectorFixedAnchorKind.ReferenceOnlyMarker,
                        SectorFixedAnchorSource.SpecialRegionSnapshot,
                        SectorFixedAnchorPriority.ReferenceOnly,
                        new SectorFixedAnchorRect(23, 15, 1, 1), "REGION_VILLAGE"),
                };
                AddSpecial(result, Core, "CORE", "REGION_CORE", "SITE_CORE");
                AddSpecial(result, Forge, "FORGE", "REGION_FORGE", "SITE_FORGE");
                AddSpecial(result, Boss, "BOSS", "REGION_BOSS", "SITE_BOSS");
                return result;
            }

            private static SectorFixedAnchorProjection RouteAnchor(
                string anchorId,
                string sourceId,
                SectorPlannerSide side,
                SectorFixedAnchorRect rect) =>
                new SectorFixedAnchorProjection(
                    anchorId, Plain, SectorFixedAnchorKind.ExternalRouteSocket,
                    SectorFixedAnchorSource.RouteSnapshot,
                    SectorFixedAnchorPriority.ExternalRouteSocket,
                    rect, sourceId, side);

            private static void AddSpecial(
                ICollection<SectorFixedAnchorProjection> result,
                SectorCoord coordinate,
                string token,
                string regionId,
                string siteId)
            {
                result.Add(new SectorFixedAnchorProjection(
                    "ANCHOR_" + token + "_FOOTPRINT", coordinate,
                    SectorFixedAnchorKind.SpecialFootprint,
                    SectorFixedAnchorSource.SpecialRegionSnapshot,
                    SectorFixedAnchorPriority.SpecialReservation,
                    new SectorFixedAnchorRect(18, 12, 12, 8), regionId,
                    placedOwnershipClaim: true, progressionBlockerClaim: true));
                result.Add(new SectorFixedAnchorProjection(
                    "ANCHOR_" + token + "_ENTRY", coordinate,
                    SectorFixedAnchorKind.SpecialEntryReturn,
                    SectorFixedAnchorSource.SpecialRegionSnapshot,
                    SectorFixedAnchorPriority.SpecialTransition,
                    new SectorFixedAnchorRect(16, 14, 2, 4), regionId,
                    placedOwnershipClaim: true, progressionBlockerClaim: true));
                result.Add(new SectorFixedAnchorProjection(
                    "ANCHOR_" + token + "_BUFFER", coordinate,
                    SectorFixedAnchorKind.SpecialApronBuffer,
                    SectorFixedAnchorSource.SpecialRegionSnapshot,
                    SectorFixedAnchorPriority.SpecialTransition,
                    new SectorFixedAnchorRect(30, 12, 2, 8), regionId,
                    placedOwnershipClaim: true, progressionBlockerClaim: true));
                result.Add(new SectorFixedAnchorProjection(
                    "ANCHOR_" + token + "_SITE", coordinate,
                    SectorFixedAnchorKind.SiteReservation,
                    SectorFixedAnchorSource.SiteSnapshot,
                    SectorFixedAnchorPriority.SpecialReservation,
                    new SectorFixedAnchorRect(20, 22, 4, 2), siteId,
                    placedOwnershipClaim: true));
            }

            private static List<SectorClusterSourceProjection> CreateClusterCatalog()
            {
                var mandatory = new[] { AccessClass.MandatoryNoTool };
                var route1 = new[] { 1 };
                var sockets = new[] { "SOCKET_L", "SOCKET_R", "SOCKET_U", "SOCKET_D" };
                return new List<SectorClusterSourceProjection>
                {
                    Source("TC_REF_TRAVERSAL_BRIDGE", "SPINE_TRAVERSAL_R0", MoonpalaceBiomeId.MoonCrater,
                        PacingRole.Traversal, route1, mandatory, sockets, H2(), Origins(2, 1), false, false, 10),
                    Source("TC_REF_TRAVERSAL_RECOVERY", "SPINE_RECOVERY_R0", MoonpalaceBiomeId.MoonCrater,
                        PacingRole.Recovery, route1, mandatory, sockets, V2(), Origins(1, 2), false, false, 11),
                    Source("TC_REF_QUIET_BUFFER", "SPINE_QUIET_R0", MoonpalaceBiomeId.MoonCrater,
                        PacingRole.Quiet, route1, mandatory, null, H2(), Origins(2, 1), true, false, 20),
                    Source("TC_REF_QUIET_ALCOVE", "SPINE_QUIET_MX", MoonpalaceBiomeId.MoonCrater,
                        PacingRole.Quiet, route1, mandatory, null, V2(), Origins(1, 2), true, false, 21,
                        ClusterFootprintTransform.MirrorX),
                    Source("TC_REF_VILLAGE_APPROACH", "SPINE_SAFE_R0", MoonpalaceBiomeId.CassiaRoot,
                        PacingRole.Safe, route1, mandatory, null, H2(), Origins(2, 1), false, true, 30),
                    Source("TC_REF_VILLAGE_LANDMARK", "SPINE_LANDMARK_MX", MoonpalaceBiomeId.CassiaRoot,
                        PacingRole.Landmark, route1, mandatory, null, V2(), Origins(1, 2), false, true, 31,
                        ClusterFootprintTransform.MirrorX),
                    Source("TC_REF_CORE_RESOURCE_RING", "SPINE_RESOURCE_R0", MoonpalaceBiomeId.CassiaRoot,
                        PacingRole.Resource, route1, mandatory, null, H4(), Origins(4, 1), false, true, 40),
                    Source("TC_REF_CORE_RESOURCE_SHAFT", "SPINE_RESOURCE_MY", MoonpalaceBiomeId.CassiaRoot,
                        PacingRole.Resource, route1, mandatory, null, V3(), Origins(1, 3), false, true, 41,
                        ClusterFootprintTransform.MirrorY),
                    Source("TC_REF_FORGE_MACHINERY", "SPINE_LANDMARK_R0", MoonpalaceBiomeId.AbandonedMill,
                        PacingRole.Landmark, route1, mandatory, null, H4(), Origins(4, 1), false, true, 50),
                    Source("TC_REF_FORGE_SERVICE", "SPINE_MACHINERY_R0", MoonpalaceBiomeId.AbandonedMill,
                        PacingRole.Machinery, route1, mandatory, null, V3(), Origins(1, 3), false, true, 51),
                    Source("TC_REF_BOSS_GATE", "SPINE_BOSS_R0", MoonpalaceBiomeId.MoonDough,
                        PacingRole.Boss, route1, mandatory, null, Boss5(), Origins(4, 2), false, true, 60),
                    Source("TC_REF_BOSS_APPROACH", "SPINE_BOSS_MX", MoonpalaceBiomeId.MoonDough,
                        PacingRole.Boss, route1, mandatory, null, H4(), Origins(4, 1), false, true, 61,
                        ClusterFootprintTransform.MirrorX),
                    Source("TC_REF_ACTIVITY_SHELL", "SPINE_ACTIVITY_R0", MoonpalaceBiomeId.MoonCrater,
                        PacingRole.Activity, route1, mandatory, null, H2(), Origins(2, 1), false, false, 70),
                    Source("TC_REF_ACTIVITY_ALT", "SPINE_ACTIVITY_MY", MoonpalaceBiomeId.MoonCrater,
                        PacingRole.Activity, route1, mandatory, null, V2(), Origins(1, 2), false, false, 71,
                        ClusterFootprintTransform.MirrorY),
                    Source("TC_REF_DISCOVERY_PASSAGE", "SPINE_DISCOVERY_R0", MoonpalaceBiomeId.CassiaRoot,
                        PacingRole.Discovery, route1, mandatory, null, H2(), Origins(2, 1), false, true, 80),
                    Source("TC_REF_DISCOVERY_ALT", "SPINE_DISCOVERY_MX", MoonpalaceBiomeId.CassiaRoot,
                        PacingRole.Discovery, route1, mandatory, null, V2(), Origins(1, 2), false, true, 81,
                        ClusterFootprintTransform.MirrorX),
                    Source("TC_REF_NEIGHBOR_FLOW", "SPINE_NEIGHBOR_R0", MoonpalaceBiomeId.AbandonedMill,
                        PacingRole.Traversal, route1, mandatory, null, L3(), Origins(2, 2), false, false, 90),
                    Source("TC_REF_NEIGHBOR_ALT", "SPINE_NEIGHBOR_R180", MoonpalaceBiomeId.AbandonedMill,
                        PacingRole.Traversal, route1, mandatory, null, H2(), Origins(2, 1), false, false, 91,
                        ClusterFootprintTransform.R180),
                    Source("TC_REJECT_SOCKET", "SPINE_REJECT_SOCKET", MoonpalaceBiomeId.MoonCrater,
                        PacingRole.Traversal, new[] { 2 }, mandatory, sockets, H2(), Origins(2, 1), false, false, 100),
                    Source("TC_REJECT_ACCESS", "SPINE_REJECT_ACCESS", MoonpalaceBiomeId.MoonCrater,
                        PacingRole.Quiet, route1, new[] { AccessClass.OptionalTool }, null, H2(), Origins(2, 1), true, false, 101),
                    Source("TC_REJECT_DENSITY", "SPINE_REJECT_DENSITY", MoonpalaceBiomeId.CassiaRoot,
                        PacingRole.Resource, route1, mandatory, null, H2(), Origins(2, 1), false, true, 102,
                        minimumDensity: 3),
                    Source("TC_REJECT_ANCHOR", "SPINE_REJECT_ANCHOR", MoonpalaceBiomeId.MoonDough,
                        PacingRole.Boss, route1, mandatory, null, Boss5(), new[] { Cell(0, 1) }, false, true, 103),
                };
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
                int minimumDensity = 2) =>
                new SectorClusterSourceProjection(
                    new TerrainClusterId(clusterId), new SpineVariantId(variantId), transform,
                    biome, new[] { pacing }, routes, access, sockets, cells, origins,
                    minimumDensity, 5, quiet, special, catalogOrder, 0);

            private static List<SectorPatternSourceProjection> CreatePatternCatalog(bool reverse)
            {
                var allRoles = Enum.GetValues(typeof(SectorClusterRoleCellKind))
                    .Cast<SectorClusterRoleCellKind>().ToArray();
                var allPacing = Enum.GetValues(typeof(PacingRole)).Cast<PacingRole>()
                    .Where(value => value != PacingRole.None).ToArray();
                var result = new List<SectorPatternSourceProjection>
                {
                    Pattern("MP_REF_BODY_A", "BODY_A", 1,
                        new[] { SectorPatternZoneKind.ClusterBody }, allRoles, allPacing, "SIG_BODY_A", 10),
                    Pattern("MP_REF_BODY_B", "BODY_B", 2,
                        new[] { SectorPatternZoneKind.ClusterBody }, allRoles, allPacing, "SIG_BODY_B", 11),
                    Pattern("MP_REF_EDGE", "EDGE", 3,
                        new[] { SectorPatternZoneKind.ClusterEdge }, allRoles, allPacing, "SIG_EDGE", 20),
                    Pattern("MP_REF_ROUTE", "ROUTE", 4,
                        new[] { SectorPatternZoneKind.RouteShoulder }, allRoles, allPacing, "SIG_ROUTE", 30),
                    Pattern("MP_REF_BOUNDARY", "BOUNDARY", 5,
                        new[] { SectorPatternZoneKind.BoundaryBlend }, allRoles, allPacing, "SIG_BOUNDARY", 40),
                    Pattern("MP_REF_SPECIAL", "SPECIAL", 6,
                        new[] { SectorPatternZoneKind.SpecialApproach }, allRoles, allPacing, "SIG_SPECIAL", 50),
                    Pattern("MP_REF_RECOVERY", "RECOVERY", 7,
                        new[] { SectorPatternZoneKind.Recovery }, allRoles, allPacing, "SIG_RECOVERY", 60),
                    Pattern("MP_REF_QUIET", "QUIET", 8,
                        new[] { SectorPatternZoneKind.QuietBuffer }, allRoles, allPacing, "SIG_QUIET", 70),
                    Pattern("MP_REF_DETAIL", "DETAIL", 9,
                        new[] { SectorPatternZoneKind.Detail }, allRoles, allPacing, "SIG_DETAIL", 80),
                    Pattern("MP_REF_PROTECTED", "PROTECTED", 10,
                        new[] { SectorPatternZoneKind.ProtectedNoWrite }, allRoles, allPacing, "SIG_PROTECTED", 90),
                };
                if (reverse) result.Reverse();
                return result;
            }

            private static SectorPatternSourceProjection Pattern(
                string id,
                string token,
                int salt,
                IEnumerable<SectorPatternZoneKind> zones,
                IEnumerable<SectorClusterRoleCellKind> roles,
                IEnumerable<PacingRole> pacing,
                string signature,
                int order) =>
                new SectorPatternSourceProjection(
                    PatternDefinition(id, token, salt),
                    MicroPatternTransform.R0,
                    zones,
                    roles,
                    pacing,
                    signature,
                    order);

            private static MicroPatternDefinition PatternDefinition(
                string id,
                string token,
                int salt)
            {
                var cells = new List<MicroPatternCell>();
                for (var y = 0; y < 4; y++)
                for (var x = 0; x < 4; x++)
                {
                    var geometry = (x + y + salt) % 3 == 0
                        ? MicroPatternOperation.AddSolid
                        : MicroPatternOperation.CarveAir;
                    cells.Add(new MicroPatternCell(
                        new LocalTileCoord(x, y),
                        new[]
                        {
                            new MicroPatternInstruction(MicroPatternLayer.Geometry, geometry),
                            new MicroPatternInstruction(MicroPatternLayer.Surface,
                                MicroPatternOperation.SetSurface, "SURFACE_" + token),
                            new MicroPatternInstruction(MicroPatternLayer.Material,
                                MicroPatternOperation.SetMaterial, "MATERIAL_" + token),
                        }));
                }

                return new MicroPatternDefinition(
                    new MicroPatternId(id),
                    4,
                    4,
                    cells,
                    10 + salt,
                    new[]
                    {
                        MoonpalaceBiomeId.MoonCrater,
                        MoonpalaceBiomeId.CassiaRoot,
                        MoonpalaceBiomeId.AbandonedMill,
                        MoonpalaceBiomeId.MoonDough,
                    },
                    new[] { MicroPatternTransform.R0 },
                    MicroPatternProtectedPolicy.ForceNoChange,
                    id);
            }

            private static SectorClusterFootprintCell[] H2() => new[] { Cell(0, 0), Cell(1, 0) };
            private static SectorClusterFootprintCell[] V2() => new[] { Cell(0, 0), Cell(0, 1) };
            private static SectorClusterFootprintCell[] V3() => new[] { Cell(0, 0), Cell(0, 1), Cell(0, 2) };
            private static SectorClusterFootprintCell[] H4() => new[] { Cell(0, 0), Cell(1, 0), Cell(2, 0), Cell(3, 0) };
            private static SectorClusterFootprintCell[] L3() => new[] { Cell(0, 0), Cell(1, 0), Cell(0, 1) };
            private static SectorClusterFootprintCell[] Boss5() => new[]
            {
                Cell(0, 0), Cell(1, 0), Cell(2, 0), Cell(3, 0), Cell(0, 1),
            };

            private static SectorClusterFootprintCell Cell(int x, int y) =>
                new SectorClusterFootprintCell(x, y);

            private static SectorClusterFootprintCell[] Origins(int width, int height)
            {
                var result = new List<SectorClusterFootprintCell>();
                for (var y = 0; y <= 4 - height; y++)
                for (var x = 0; x <= 4 - width; x++)
                    result.Add(Cell(x, y));
                return result.ToArray();
            }

            private static void Require<T>(bool condition, IEnumerable<T> errors)
            {
                if (!condition) throw new InvalidOperationException(Join(errors));
            }

            private static string Digest(char value) => new string(value, 64);
        }
    }
}
