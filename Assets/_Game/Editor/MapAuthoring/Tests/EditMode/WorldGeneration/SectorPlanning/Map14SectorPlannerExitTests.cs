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

namespace StarNight.Map.Editor.Tests.WorldGeneration.SectorPlanning
{
    [TestFixture]
    [Category("MAP14_10")]
    public sealed class Map14SectorPlannerExitTests
    {
        private const int Map15StartClaimCount = 0;
        private static ReferenceFixture fixture;
        private static SectorCanvasOwnershipPlan canvas;
        private static SectorPlannerRetryBuildResult retry;
        private static SectorPlannerDebugExportRequest request;
        private static SectorPlannerDebugExportResult export;
        private static SectorPlannerDebugExportResult failure;
        private static SectorPlannerDebugExportResult catalog;
        private static ReachabilityAudit reachability;

        [OneTimeSetUp]
        public void BuildReferencePacket()
        {
            fixture = ReferenceFixture.Create(false);
            canvas = fixture.BuildCanvas(false);
            retry = SectorPlannerRetryExecutor.Execute(RetryRequest(canvas, MetricsInputs()));
            Require(retry.Success, retry.Errors);
            request = new SectorPlannerDebugExportRequest(retry.Plan);
            export = SectorPlannerDebugExporter.Export(request);
            Require(export.Success, export.Errors);
            failure = SectorPlannerFailureRingExporter.ExportFailureRing(request, retry.Plan.NodeTraces.First(), fixture.Input.Sectors);
            Require(failure.Success, failure.Errors);
            catalog = SectorPlannerGrayboxFixtureCatalogBuilder.Build(request, export.Export, failure.FailureRing, fixture.Input.Sectors);
            Require(catalog.Success, catalog.Errors);
            reachability = ReachabilityAudit.Build(fixture, canvas, catalog.Fixtures);
        }

        [Test]
        public void CurrentChainPublishesAllMap14ArtifactsForExit()
        {
            var digests = ArtifactDigests(request.RetryPlan, export, catalog);
            Assert.That(export.Success, Is.True);
            Assert.That(export.Export.Kind, Is.EqualTo(SectorPlannerDebugExportKind.SuccessPlan));
            Assert.That(export.Export.SectionCount, Is.EqualTo(9));
            Assert.That(export.Export.Sections.Select(value => value.Kind), Is.EquivalentTo(new[]
            {
                SectorPlannerDebugSectionKind.SourceIdentity,
                SectorPlannerDebugSectionKind.RouteAccess,
                SectorPlannerDebugSectionKind.AnchorBoundarySpecial,
                SectorPlannerDebugSectionKind.SpineEnvelope,
                SectorPlannerDebugSectionKind.ClusterPattern,
                SectorPlannerDebugSectionKind.QuietActivityEvent,
                SectorPlannerDebugSectionKind.OwnershipPlanes,
                SectorPlannerDebugSectionKind.RetryRng,
                SectorPlannerDebugSectionKind.MutationProof,
            }));
            Assert.That(export.Export.TokenCount, Is.GreaterThan(0));
            Assert.That(export.Export.TextGridPayloadCount, Is.EqualTo(9));
            Assert.That(export.Export.GridPayloads.All(value => value.Rows.Count > 0), Is.True);
            Assert.That(export.Export.Legend.Count, Is.EqualTo(Enum.GetValues(typeof(SectorPlannerDebugTokenKind)).Length));
            AssertLowerSha(export.Export.CanonicalDigest);
            Assert.That(digests.Count, Is.EqualTo(12));
            Assert.That(digests.All(value => value.Length == 64), Is.True);
            Assert.That(fixture.Input.Sectors.Count, Is.EqualTo(9));
            Assert.That(fixture.Assignments.Count, Is.EqualTo(9));
            Assert.That(fixture.AnchorPlan.Anchors, Is.Not.Empty);
            Assert.That(fixture.PlacementPlan.Placements, Is.Not.Empty);
            Assert.That(fixture.SpineEnvelopePlan.Map14_05HandoffReady, Is.True);
            Assert.That(canvas.Map14_08HandoffReady, Is.True);
            Assert.That(request.RetryPlan.Map14_09HandoffReady, Is.True);
            Assert.That(((IList<SectorPlannerDebugSection>)export.Export.Sections).IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => ((IList<SectorPlannerDebugSection>)export.Export.Sections).Clear());
            TestContext.WriteLine("MAP14_ARTIFACT_DIGESTS=12;SECTORS=9;PACING_ASSIGNMENTS=9;DEBUG_EXPORTS=1;SUCCESS_SECTIONS=" + export.Export.SectionCount +
                ";TOKENS=" + export.Export.TokenCount + ";GRID_PAYLOADS=" + export.Export.TextGridPayloadCount +
                ";DIGEST=" + export.Export.CanonicalDigest);
        }

        [Test]
        public void FailureRingExplainsAbortOrRetryWithoutMutatingSources()
        {
            var ring = failure.FailureRing;
            Assert.That(ring.ExportedSectorCount, Is.EqualTo(9));
            Assert.That(ring.RingSectorCount, Is.EqualTo(8));
            Assert.That(ring.MissingNeighborCount, Is.Zero);
            Assert.That(ring.CenterSector.SectorCoordinate, Is.EqualTo(new SectorCoord(2, 2)));
            Assert.That(ring.Failure.Owner, Is.EqualTo(SectorPlannerRetryFailureOwner.PatternSelection));
            Assert.That(ring.Failure.Code, Is.EqualTo("MISSING_PATTERN"));
            Assert.That(ring.NextStage, Is.EqualTo(SectorPlannerRetryStage.PatternCandidate));
            Assert.That(ring.RetryExecutionCount + ring.NewRngDrawCount + ring.RepairCount, Is.Zero);
            Assert.That(ring.FailureSection.Kind, Is.EqualTo(SectorPlannerDebugSectionKind.FailureRing));
            Assert.That(ring.FailureSection.SourceDigest, Is.EqualTo(request.RetryPlan.CanonicalDigest));
            AssertLowerSha(ring.CanonicalDigest);
            TestContext.WriteLine("FAILURE_RING_EXPORTS=1;CENTER=1;RING=8;MISSING=0;REPAIR=0;DIGEST=" + ring.CanonicalDigest);
        }

        [Test]
        public void GrayboxCoverageApprovesRouteBiomeBoundaryAndSpecialRequirements()
        {
            AssertCoverage(SectorPlannerGrayboxCoverageKind.RouteType, 7);
            AssertCoverage(SectorPlannerGrayboxCoverageKind.Biome, 4);
            AssertCoverage(SectorPlannerGrayboxCoverageKind.BoundaryPair, 6);
            AssertCoverage(SectorPlannerGrayboxCoverageKind.SpecialRegion, 6);
            AssertCoverage(SectorPlannerGrayboxCoverageKind.PacingRole, 6);
            AssertCoverage(SectorPlannerGrayboxCoverageKind.AccessClass, 2);
            AssertCoverage(SectorPlannerGrayboxCoverageKind.OwnershipPlane, 5);
            AssertCoverage(SectorPlannerGrayboxCoverageKind.RetryStage, 8);
            Assert.That(catalog.CoverageAudit.OneSectorFixtureCount, Is.EqualTo(9));
            Assert.That(catalog.CoverageAudit.ThreeSectorFixtureCount, Is.EqualTo(9));
            Assert.That(catalog.CoverageAudit.FailureRingFixtureCount, Is.EqualTo(1));
            Assert.That(catalog.CoverageAudit.TotalMissingCount, Is.Zero);
            Assert.That(catalog.CoverageAudit.RequiredFor(SectorPlannerGrayboxCoverageKind.RouteType),
                Is.EqualTo(new[] { "BOUNDARY", "SPECIAL", "TYPE_0", "TYPE_1", "TYPE_2", "TYPE_3", "TYPE_4" }));
            Assert.That(catalog.CoverageAudit.RequiredFor(SectorPlannerGrayboxCoverageKind.SpecialRegion),
                Is.EqualTo(new[] { "Boss", "CoreResource", "Forge", "Maru", "Merchant", "Village" }));
            TestContext.WriteLine("FIXTURES_ONE=9;FIXTURES_THREE=9;FIXTURES_FAILURE_RING=1;ROUTE=" +
                CoverageCounts(SectorPlannerGrayboxCoverageKind.RouteType) + ";ROUTE_VALUES=" + CoverageValues(SectorPlannerGrayboxCoverageKind.RouteType) +
                ";BIOME=" + CoverageCounts(SectorPlannerGrayboxCoverageKind.Biome) + ";BIOME_VALUES=" + CoverageValues(SectorPlannerGrayboxCoverageKind.Biome) +
                ";BOUNDARY=" + CoverageCounts(SectorPlannerGrayboxCoverageKind.BoundaryPair) + ";BOUNDARY_VALUES=" + CoverageValues(SectorPlannerGrayboxCoverageKind.BoundaryPair) +
                ";SPECIAL=" + CoverageCounts(SectorPlannerGrayboxCoverageKind.SpecialRegion) + ";SPECIAL_VALUES=" + CoverageValues(SectorPlannerGrayboxCoverageKind.SpecialRegion) +
                ";PACING_ROLE=" + CoverageCounts(SectorPlannerGrayboxCoverageKind.PacingRole) + ";PACING_VALUES=" + CoverageValues(SectorPlannerGrayboxCoverageKind.PacingRole) +
                ";ACCESS_CLASS=" + CoverageCounts(SectorPlannerGrayboxCoverageKind.AccessClass) + ";ACCESS_VALUES=" + CoverageValues(SectorPlannerGrayboxCoverageKind.AccessClass) +
                ";OWNERSHIP_PLANE=" + CoverageCounts(SectorPlannerGrayboxCoverageKind.OwnershipPlane) +
                ";RETRY_STAGE_TERMINAL=" + CoverageCounts(SectorPlannerGrayboxCoverageKind.RetryStage));
        }

        [Test]
        public void OneSectorGrayboxesHaveDeterministicTileReachability()
        {
            Assert.That(reachability.OneSectorChecksRequired, Is.Positive);
            Assert.That(reachability.OneSectorChecksPassed, Is.EqualTo(reachability.OneSectorChecksRequired));
            Assert.That(reachability.OneSectorChecksFailed, Is.Zero);
            Assert.That(reachability.RequiredWitnessCount, Is.Positive);
            Assert.That(reachability.MissingWitnessCount, Is.Zero);
            Assert.That(reachability.TilePathDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(ReachabilityAudit.Build(fixture, canvas, catalog.Fixtures).TilePathDigest,
                Is.EqualTo(reachability.TilePathDigest));
            TestContext.WriteLine("ONE_SECTOR_ROUTE_CHECKS=" + reachability.OneSectorChecksRequired + "/" +
                reachability.OneSectorChecksPassed + "/" + reachability.OneSectorChecksFailed +
                ";ENTRY_EXIT_WITNESSES=" + reachability.RequiredWitnessCount + ";MISSING_WITNESSES=0;TILE_PATH_DIGEST=" + reachability.TilePathDigest);
        }

        [Test]
        public void ThreeSectorGrayboxesPreserveExternalSocketsAndBoundaryContinuity()
        {
            Assert.That(reachability.ThreeSectorChecksRequired, Is.EqualTo(9));
            Assert.That(reachability.ThreeSectorChecksPassed, Is.EqualTo(reachability.ThreeSectorChecksRequired));
            Assert.That(reachability.ThreeSectorChecksFailed, Is.Zero);
            Assert.That(reachability.SocketContinuityChecksRequired, Is.Positive);
            Assert.That(reachability.SocketContinuityChecksPassed, Is.EqualTo(reachability.SocketContinuityChecksRequired));
            Assert.That(reachability.SocketContinuityChecksFailed, Is.Zero);
            Assert.That(reachability.BoundaryBridgeChecksRequired, Is.EqualTo(6));
            Assert.That(reachability.BoundaryBridgeChecksPassed, Is.EqualTo(reachability.BoundaryBridgeChecksRequired));
            TestContext.WriteLine("THREE_SECTOR_ROUTE_CHECKS=" + reachability.ThreeSectorChecksRequired + "/" +
                reachability.ThreeSectorChecksPassed + "/" + reachability.ThreeSectorChecksFailed +
                ";SOCKET_CONTINUITY=" + reachability.SocketContinuityChecksRequired + "/" +
                reachability.SocketContinuityChecksPassed + "/" + reachability.SocketContinuityChecksFailed +
                ";BOUNDARY_BRIDGES=" + reachability.BoundaryBridgeChecksRequired + "/" + reachability.BoundaryBridgeChecksPassed + "/0");
        }

        [Test]
        public void OwnershipCanvasHasFullCoverageNoDoubleOwnersAndNoForbiddenConflict()
        {
            Assert.That(canvas.CoverageCount, Is.EqualTo(13824));
            Assert.That(canvas.ExpectedCoverageCount, Is.EqualTo(13824));
            Assert.That(canvas.SamePlaneDoubleOwnerCount, Is.Zero);
            Assert.That(canvas.ForbiddenOverlapCount, Is.Zero);
            Assert.That(canvas.UnresolvedConflictCount, Is.Zero);
            Assert.That(canvas.CountOwned(SectorCanvasOwnershipPlane.Terrain), Is.EqualTo(13088));
            Assert.That(canvas.CountOwned(SectorCanvasOwnershipPlane.Protection), Is.EqualTo(1464));
            Assert.That(canvas.CountOwned(SectorCanvasOwnershipPlane.Reservation), Is.EqualTo(425));
            Assert.That(canvas.CountOwned(SectorCanvasOwnershipPlane.Marker), Is.EqualTo(1));
            Assert.That(canvas.CountOwned(SectorCanvasOwnershipPlane.Evidence), Is.Zero);
            Assert.That(canvas.WinnerClaims.Count(value => value.Plane == SectorCanvasOwnershipPlane.Terrain &&
                (value.OwnerKind == SectorCanvasOwnerKind.ActivityMarker || value.OwnerKind == SectorCanvasOwnerKind.EventMarker)), Is.Zero);
            Assert.That(canvas.EmptyEvidenceOnlyCoordinateCount, Is.EqualTo(736));
            TestContext.WriteLine("OWNERSHIP_COVERAGE=13824/13824;TERRAIN=13088;PROTECTION=1464;RESERVATION=425;MARKER=1;EVIDENCE_PLANE=0;" +
                "DOUBLE_OWNER=0;FORBIDDEN_OVERLAP=0;UNRESOLVED_CONFLICT=0;ACTIVITY_EVENT_TERRAIN_OWNER=0;EXPLICIT_NO_TERRAIN_EVIDENCE=736");
        }

        [Test]
        public void RetryPolicyCapsAbortDeterministicallyAndDoNotRepairByCarving()
        {
            var firstPass = SectorPlannerRetryExecutor.Execute(RetryRequest(canvas, Array.Empty<SectorPlannerAttemptTraceInput>()));
            Assert.That(firstPass.Success, Is.True, RetryErrors(firstPass.Errors));
            Assert.That(firstPass.TerminalDecision, Is.EqualTo(SectorPlannerRetryDecisionKind.AcceptFirstPass));
            Assert.That(firstPass.Plan.RetryNodeCount, Is.Zero);
            Assert.That(firstPass.Plan.Map14RetryRngDrawCount, Is.Zero);

            var capCases = CapCases();
            foreach (var item in capCases)
            {
                var first = SectorPlannerRetryExecutor.Execute(RetryRequest(canvas, item.Inputs, item.Policy));
                var repeat = SectorPlannerRetryExecutor.Execute(RetryRequest(canvas, item.Inputs.Reverse(), item.Policy));
                Assert.That(first.Success, Is.False);
                Assert.That(first.TerminalDecision, Is.EqualTo(SectorPlannerRetryDecisionKind.AbortCapReached));
                Assert.That(first.CapAbortCount, Is.EqualTo(1));
                Assert.That(RetryErrors(repeat.Errors), Is.EqualTo(RetryErrors(first.Errors)));
            }

            var forbidden = ForbiddenCases();
            foreach (var input in forbidden)
            {
                var result = SectorPlannerRetryExecutor.Execute(RetryRequest(canvas, new[] { input }));
                Assert.That(result.Success, Is.False);
                Assert.That(result.TerminalDecision, Is.EqualTo(SectorPlannerRetryDecisionKind.AbortForbiddenFallback));
                Assert.That(result.ForbiddenAbortCount, Is.EqualTo(1));
                Assert.That(result.Map14RetryRngDrawCount, Is.Zero);
                Assert.That(result.Errors.Select(value => value.Code), Does.Contain(input.Failure.ForbiddenErrorCode));
            }

            Assert.That(request.RetryPlan.FallbackCorridorCarveCount + request.RetryPlan.ValidationRelaxationCount +
                request.RetryPlan.WholeSectorRerandomCount + request.RetryPlan.WholeWorldRerandomCount, Is.Zero);
            TestContext.WriteLine("FIRST_PASS_ACCEPT=1;FIRST_PASS_RETRY_NODES=0;FIRST_PASS_MAP14_DRAWS=0;CAP_CASES=6;CAP_ABORTS=6;" +
                "FORBIDDEN_FALLBACK_CASES=8;FORBIDDEN_REJECTED=8;FALLBACK_CARVE=0;VALIDATION_RELAXATION=0;NEW_MAP14_10_RNG_DRAWS=0");
        }

        private static string CoverageCounts(SectorPlannerGrayboxCoverageKind kind)
        {
            return catalog.CoverageAudit.RequiredFor(kind).Count + "/" +
                catalog.CoverageAudit.CoveredFor(kind).Count + "/" +
                catalog.CoverageAudit.MissingFor(kind).Count;
        }

        private static string CoverageValues(SectorPlannerGrayboxCoverageKind kind) =>
            string.Join(",", catalog.CoverageAudit.CoveredFor(kind));

        [Test]
        public void StaticSoftlockCandidateCountsAreZeroForRequiredRoutesAndSpecialEntrances()
        {
            Assert.That(reachability.StaticSoftlockCandidateCount, Is.Zero,
                string.Join(";", reachability.SoftlockReasons));
            Assert.That(reachability.RequiredRouteSoftlocks, Is.Zero);
            Assert.That(reachability.SpecialEntranceSoftlocks, Is.Zero);
            Assert.That(reachability.BoundaryBridgeSoftlocks, Is.Zero);
            Assert.That(reachability.SpecialEntranceChecksRequired, Is.EqualTo(3));
            Assert.That(reachability.SpecialEntranceChecksPassed, Is.EqualTo(3));
            Assert.That(reachability.ActivityEventMarkerRequiredForRouteCount, Is.Zero);
            Assert.That(reachability.MissingOwnershipWitnessCount, Is.Zero);
            TestContext.WriteLine("STATIC_SOFTLOCK_REQUIRED_ROUTE=0;STATIC_SOFTLOCK_SPECIAL_ENTRY_RETURN=0;STATIC_SOFTLOCK_BOUNDARY_BRIDGE=0;" +
                "STATIC_SOFTLOCK_MISSING_OWNERSHIP=0;STATIC_SOFTLOCK_TOTAL=0;SPECIAL_ENTRY_CHECKS=3/3/0;ACTIVITY_EVENT_REQUIRED_FOR_ROUTE=0");
        }

        [Test]
        public void InvalidExitInputsFailAtomicallyWithoutOpeningMap15()
        {
            var token = new SectorPlannerDebugToken(new SectorCoord(2, 2), new LocalTileCoord(0, 0),
                SectorPlannerDebugTokenKind.Empty, "DUPLICATE", "MAP14_09", "DUPLICATE");
            var invalid = new[]
            {
                SectorPlannerDebugExporter.Export(null),
                SectorPlannerDebugExporter.Export(new SectorPlannerDebugExportRequest(null)),
                SectorPlannerDebugExporter.Export(new SectorPlannerDebugExportRequest(request.RetryPlan, new[] { token, token })),
                SectorPlannerDebugExporter.Export(new SectorPlannerDebugExportRequest(request.RetryPlan, unsupportedFileWriteClaim: true)),
                SectorPlannerGrayboxFixtureCatalogBuilder.Build(request, export.Export, failure.FailureRing, fixture.Input.Sectors.Take(1)),
            };
            foreach (var result in invalid)
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.Export, Is.Null);
                Assert.That(result.FailureRing, Is.Null);
                Assert.That(result.CoverageAudit, Is.Null);
                Assert.That(result.Fixtures, Is.Empty);
                Assert.That(result.CanonicalDigest, Is.Empty);
                Assert.That(result.Errors, Is.Not.Empty.And.Ordered);
                Assert.That(result.MutationCount + result.NewRngDrawCount + result.RetryExecutionCount, Is.Zero);
            }
            Assert.That(export.Export.MutationProof.ExitApprovalClaimCount, Is.Zero);
            Assert.That(Map15StartClaimCount, Is.Zero);
        }

        [Test]
        public void DeterminismHoldsAcrossRepeatReverseCultureSeedAndAttemptEvidence()
        {
            var baselineDigests = ArtifactDigests(request.RetryPlan, export, catalog);
            var repeatExport = SectorPlannerDebugExporter.Export(request);
            var repeatFailure = SectorPlannerFailureRingExporter.ExportFailureRing(request,
                request.RetryPlan.NodeTraces.First(), fixture.Input.Sectors.Reverse());
            var repeatCatalog = SectorPlannerGrayboxFixtureCatalogBuilder.Build(request,
                repeatExport.Export, repeatFailure.FailureRing, fixture.Input.Sectors.Reverse());
            Assert.That(repeatCatalog.Success, Is.True, Errors(repeatCatalog.Errors));
            Assert.That(repeatExport.CanonicalDigest, Is.EqualTo(export.CanonicalDigest));
            Assert.That(repeatFailure.CanonicalDigest, Is.EqualTo(failure.CanonicalDigest));
            Assert.That(repeatCatalog.CanonicalDigest, Is.EqualTo(catalog.CanonicalDigest));
            Assert.That(ArtifactDigests(request.RetryPlan, repeatExport, repeatCatalog), Is.EqualTo(baselineDigests));

            var reverseFixture = ReferenceFixture.Create(true);
            var reverseRetry = SectorPlannerRetryExecutor.Execute(RetryRequest(reverseFixture.BuildCanvas(true), MetricsInputs().Reverse()));
            Require(reverseRetry.Success, reverseRetry.Errors);
            var reverseRequest = new SectorPlannerDebugExportRequest(reverseRetry.Plan);
            var reverseExport = SectorPlannerDebugExporter.Export(reverseRequest);
            var reverseFailure = SectorPlannerFailureRingExporter.ExportFailureRing(reverseRequest,
                reverseRetry.Plan.NodeTraces.First(), reverseFixture.Input.Sectors.Reverse());
            var reverseCatalog = SectorPlannerGrayboxFixtureCatalogBuilder.Build(reverseRequest,
                reverseExport.Export, reverseFailure.FailureRing, reverseFixture.Input.Sectors.Reverse());
            Assert.That(reverseCatalog.Success, Is.True, Errors(reverseCatalog.Errors));
            Assert.That(reverseExport.CanonicalDigest, Is.EqualTo(export.CanonicalDigest));
            Assert.That(reverseFailure.CanonicalDigest, Is.EqualTo(failure.CanonicalDigest));
            Assert.That(reverseCatalog.CanonicalDigest, Is.EqualTo(catalog.CanonicalDigest));
            Assert.That(ArtifactDigests(reverseRetry.Plan, reverseExport, reverseCatalog), Is.EqualTo(baselineDigests));

            var originalCulture = CultureInfo.CurrentCulture;
            var originalUi = CultureInfo.CurrentUICulture;
            try
            {
                var turkish = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentCulture = turkish;
                CultureInfo.CurrentUICulture = turkish;
                var cultureExport = SectorPlannerDebugExporter.Export(request);
                var cultureFailure = SectorPlannerFailureRingExporter.ExportFailureRing(request,
                    request.RetryPlan.NodeTraces.First(), fixture.Input.Sectors.Reverse());
                var cultureCatalog = SectorPlannerGrayboxFixtureCatalogBuilder.Build(request,
                    cultureExport.Export, cultureFailure.FailureRing, fixture.Input.Sectors.Reverse());
                Assert.That(cultureCatalog.Success, Is.True, Errors(cultureCatalog.Errors));
                Assert.That(cultureExport.CanonicalDigest, Is.EqualTo(export.CanonicalDigest));
                Assert.That(cultureFailure.CanonicalDigest, Is.EqualTo(failure.CanonicalDigest));
                Assert.That(cultureCatalog.CanonicalDigest, Is.EqualTo(catalog.CanonicalDigest));
                Assert.That(ArtifactDigests(request.RetryPlan, cultureExport, cultureCatalog), Is.EqualTo(baselineDigests));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUi;
            }

            var changedSeed = SectorPlannerRetryExecutor.Execute(RetryRequest(canvas, MetricsInputs(), seed: 0x140A0002UL));
            Require(changedSeed.Success, changedSeed.Errors);
            Assert.That(changedSeed.Plan.CanonicalDigest, Is.Not.EqualTo(request.RetryPlan.CanonicalDigest));
            Assert.That(changedSeed.Plan.PlannerInputDigestBefore, Is.EqualTo(request.RetryPlan.PlannerInputDigestBefore));
            Assert.That(changedSeed.Plan.CanvasOwnershipPlanDigestBefore, Is.EqualTo(request.RetryPlan.CanvasOwnershipPlanDigestBefore));
            Assert.That(changedSeed.Plan.RngTraces.Select(value => value.CanonicalDigest),
                Is.Not.EqualTo(request.RetryPlan.RngTraces.Select(value => value.CanonicalDigest)));
            TestContext.WriteLine("CURRENT_DIGEST_SET=12;REPEAT_MATCHED=12;REVERSE_MATCHED=12;TR_TR_MATCHED=12;" +
                "SEED_SENSITIVE_RETRY_RNG_ONLY=1;NEW_MAP14_10_RNG_DRAWS=0");
        }

        [Test]
        public void NoProductionTilePhysicsScenePreviewGameplayOrFileExportMutation()
        {
            var proof = export.Export.MutationProof;
            var plan = request.RetryPlan;
            Assert.That(plan.AllUpstreamIdentitiesPreserved, Is.True);
            Assert.That(plan.PlannerInputDigestBefore, Is.EqualTo(plan.PlannerInputDigestAfter));
            Assert.That(plan.PacingAssignmentDigestBefore, Is.EqualTo(plan.PacingAssignmentDigestAfter));
            Assert.That(plan.FixedAnchorPlanDigestBefore, Is.EqualTo(plan.FixedAnchorPlanDigestAfter));
            Assert.That(plan.ClusterPlacementPlanDigestBefore, Is.EqualTo(plan.ClusterPlacementPlanDigestAfter));
            Assert.That(plan.SpineEnvelopePlanDigestBefore, Is.EqualTo(plan.SpineEnvelopePlanDigestAfter));
            Assert.That(plan.RolePatternPlanDigestBefore, Is.EqualTo(plan.RolePatternPlanDigestAfter));
            Assert.That(plan.PatternRenderPlanDigestBefore, Is.EqualTo(plan.PatternRenderPlanDigestAfter));
            Assert.That(plan.QuietActivityEventPlanDigestBefore, Is.EqualTo(plan.QuietActivityEventPlanDigestAfter));
            Assert.That(plan.CanvasOwnershipPlanDigestBefore, Is.EqualTo(plan.CanvasOwnershipPlanDigestAfter));
            Assert.That(plan.ActivityAuthorityDigestBefore, Is.EqualTo(plan.ActivityAuthorityDigestAfter));
            Assert.That(plan.EventAuthorityDigestBefore, Is.EqualTo(plan.EventAuthorityDigestAfter));
            Assert.That(plan.ExternalSocketIdentityBefore, Is.EqualTo(plan.ExternalSocketIdentityAfter));
            Assert.That(plan.BoundaryIdentityBefore, Is.EqualTo(plan.BoundaryIdentityAfter));
            Assert.That(plan.SpecialIdentityBefore, Is.EqualTo(plan.SpecialIdentityAfter));
            Assert.That(plan.ClusterIdentityBefore, Is.EqualTo(plan.ClusterIdentityAfter));
            Assert.That(plan.ProtectedOpenIdentityBefore, Is.EqualTo(plan.ProtectedOpenIdentityAfter));
            Assert.That(proof.TotalMutationCount, Is.Zero);
            Assert.That(proof.TilemapWriteCount + proof.SceneMutationCount + proof.PrefabMutationCount +
                        proof.GameObjectMutationCount + proof.EditorWindowMutationCount + proof.GeneratedDebugFileWriteCount, Is.Zero);
            Assert.That(proof.ActivityRuntimeSpawnCount + proof.EventRuntimeSpawnCount + proof.GameplayExecutionCount, Is.Zero);
            Assert.That(proof.ExitApprovalClaimCount, Is.Zero);
            Assert.That(catalog.Fixtures.Sum(value => value.SceneAssetCount + value.PrefabAssetCount + value.GameObjectCount + value.TilemapCount), Is.Zero);
            Assert.That(export.Export.FileWriteCount + export.Export.TilemapOwnershipClaimCount, Is.Zero);
            Assert.That(export.NewRngDrawCount + export.RetryExecutionCount, Is.Zero);
            TestContext.WriteLine("PRODUCTION_SOURCE_MUTATION=0;NEW_RUNTIME_CSHARP=0;NEW_RNG_DRAW=0;RETRY_EXECUTION_BEYOND_MAP14_08=0;" +
                "TILEMAP_WRITE=0;SCENE_PREFAB_TILEMAP_GAMEOBJECT_MUTATION=0;EDITOR_WINDOW_OVERLAY_INSPECTOR=0;DEBUG_FILE_WRITE=0;" +
                "ACTIVITY_EVENT_RUNTIME_SPAWN=0;GAMEPLAY_EXECUTION=0;MAP15_START_CLAIM=0");
        }

        private static void AssertCoverage(SectorPlannerGrayboxCoverageKind kind, int required)
        {
            Assert.That(catalog.CoverageAudit.RequiredFor(kind).Count, Is.EqualTo(required));
            Assert.That(catalog.CoverageAudit.CoveredFor(kind).Count, Is.EqualTo(required));
            Assert.That(catalog.CoverageAudit.MissingFor(kind), Is.Empty);
        }

        private static IReadOnlyList<string> ArtifactDigests(
            SectorPlannerRetryPlan plan,
            SectorPlannerDebugExportResult debugExport,
            SectorPlannerDebugExportResult grayboxCatalog) => new[]
        {
            plan.PlannerInputDigestBefore,
            plan.PacingAssignmentDigestBefore,
            plan.FixedAnchorPlanDigestBefore,
            plan.ClusterPlacementPlanDigestBefore,
            plan.SpineEnvelopePlanDigestBefore,
            plan.RolePatternPlanDigestBefore,
            plan.PatternRenderPlanDigestBefore,
            plan.QuietActivityEventPlanDigestBefore,
            plan.CanvasOwnershipPlanDigestBefore,
            plan.CanonicalDigest,
            debugExport.Export.CanonicalDigest,
            grayboxCatalog.CoverageAudit.CanonicalDigest,
        };

        private static string RetryErrors(IEnumerable<SectorPlannerRetryError> errors) =>
            string.Join(";", (errors ?? Array.Empty<SectorPlannerRetryError>()).Select(value => value.ToString()));

        private static IReadOnlyList<CapFixture> CapCases() => new[]
        {
            CapCase(new SectorPlannerRetryLimit(1, 5, 5, 5, 10, 10),
                Attempt(0, 0, SectorPlannerRetryFailureOwner.PatternSelection, "MISSING", 0, false, "A", "B"),
                Attempt(1, 1, SectorPlannerRetryFailureOwner.PatternSelection, "MISSING", 0, true, "A", "B")),
            CapCase(new SectorPlannerRetryLimit(5, 1, 5, 5, 10, 10),
                Attempt(0, 0, SectorPlannerRetryFailureOwner.PatternApplication, "TRANSFORM_REJECT", 0, false, "A", "B"),
                Attempt(1, 1, SectorPlannerRetryFailureOwner.PatternApplication, "TRANSFORM_REJECT", 0, true, "A", "B")),
            CapCase(new SectorPlannerRetryLimit(5, 5, 1, 5, 10, 10),
                Attempt(0, 0, SectorPlannerRetryFailureOwner.ClusterPlacement, "RANKING", 0, false, "A", "B"),
                Attempt(1, 1, SectorPlannerRetryFailureOwner.ClusterPlacement, "RANKING", 0, true, "A", "B")),
            CapCase(new SectorPlannerRetryLimit(5, 5, 5, 1, 10, 10),
                Attempt(0, 0, SectorPlannerRetryFailureOwner.ClusterPlacement, "FOOTPRINT_OVERLAP", 0, false, "A", "B"),
                Attempt(1, 1, SectorPlannerRetryFailureOwner.ClusterPlacement, "FOOTPRINT_OVERLAP", 0, true, "A", "B")),
            CapCase(new SectorPlannerRetryLimit(5, 5, 5, 5, 1, 10),
                Attempt(0, 0, SectorPlannerRetryFailureOwner.PatternSelection, "MISSING", 0, false, "A", "B"),
                Attempt(1, 1, SectorPlannerRetryFailureOwner.ClusterPlacement, "RANKING", 0, true, "A", "B")),
            CapCase(new SectorPlannerRetryLimit(5, 5, 5, 5, 10, 1),
                Attempt(0, 0, SectorPlannerRetryFailureOwner.PatternSelection, "MISSING", 0, false, "A", "B"),
                Attempt(1, 1, SectorPlannerRetryFailureOwner.ClusterPlacement, "RANKING", 0, true, "A", "B")),
        };

        private static CapFixture CapCase(
            SectorPlannerRetryLimit limits,
            params SectorPlannerAttemptTraceInput[] inputs) => new CapFixture(Policy(limits), inputs);

        private static SectorPlannerRetryPolicy Policy(SectorPlannerRetryLimit limits) =>
            new SectorPlannerRetryPolicy(limits, new[]
            {
                SectorPlannerRetryStage.PatternCandidate,
                SectorPlannerRetryStage.PatternTransform,
                SectorPlannerRetryStage.ClusterVariant,
                SectorPlannerRetryStage.ClusterFootprint,
                SectorPlannerRetryStage.SectorAttempt,
                SectorPlannerRetryStage.Abort,
            });

        private static IReadOnlyList<SectorPlannerAttemptTraceInput> ForbiddenCases() => new[]
        {
            Forbidden("CORRIDOR", SectorPlannerRetryErrorCode.SyntheticCorridorAttempt),
            Forbidden("VALIDATION", SectorPlannerRetryErrorCode.ValidationRelaxationAttempt),
            Forbidden("SECTOR_REROLL", SectorPlannerRetryErrorCode.WholeSectorRerandomAttempt),
            Forbidden("WORLD_REROLL", SectorPlannerRetryErrorCode.WholeWorldRerandomAttempt),
            Forbidden("SOCKET", SectorPlannerRetryErrorCode.SocketMutationAttempt),
            Forbidden("BOUNDARY", SectorPlannerRetryErrorCode.BoundaryMutationAttempt),
            Forbidden("SPECIAL", SectorPlannerRetryErrorCode.SpecialReservationMutationAttempt),
            Forbidden("PROTECTED", SectorPlannerRetryErrorCode.ProtectedMaskRelaxationAttempt),
        };

        private static SectorPlannerAttemptTraceInput Forbidden(
            string code,
            SectorPlannerRetryErrorCode errorCode) => new SectorPlannerAttemptTraceInput(
            0,
            0,
            new SectorPlannerRetryFailure(SectorPlannerRetryFailureOwner.ForbiddenFallback,
                code, code + "_SUBJECT", code + "_DETAIL", 0, errorCode),
            Array.Empty<string>(),
            false);

        private sealed class CapFixture
        {
            internal CapFixture(SectorPlannerRetryPolicy policy, IReadOnlyList<SectorPlannerAttemptTraceInput> inputs)
            {
                Policy = policy;
                Inputs = inputs;
            }

            internal SectorPlannerRetryPolicy Policy { get; }
            internal IReadOnlyList<SectorPlannerAttemptTraceInput> Inputs { get; }
        }

        private sealed class ReachabilityAudit
        {
            private ReachabilityAudit()
            {
            }

            internal int OneSectorChecksRequired { get; private set; }
            internal int OneSectorChecksPassed { get; private set; }
            internal int OneSectorChecksFailed => OneSectorChecksRequired - OneSectorChecksPassed;
            internal int ThreeSectorChecksRequired { get; private set; }
            internal int ThreeSectorChecksPassed { get; private set; }
            internal int ThreeSectorChecksFailed => ThreeSectorChecksRequired - ThreeSectorChecksPassed;
            internal int RequiredWitnessCount { get; private set; }
            internal int MissingWitnessCount { get; private set; }
            internal int MissingOwnershipWitnessCount { get; private set; }
            internal int SocketContinuityChecksRequired { get; private set; }
            internal int SocketContinuityChecksPassed { get; private set; }
            internal int SocketContinuityChecksFailed => SocketContinuityChecksRequired - SocketContinuityChecksPassed;
            internal int BoundaryBridgeChecksRequired { get; private set; }
            internal int BoundaryBridgeChecksPassed { get; private set; }
            internal int SpecialEntranceChecksRequired { get; private set; }
            internal int SpecialEntranceChecksPassed { get; private set; }
            internal int RequiredRouteSoftlocks { get; private set; }
            internal int SpecialEntranceSoftlocks { get; private set; }
            internal int BoundaryBridgeSoftlocks { get; private set; }
            internal int ActivityEventMarkerRequiredForRouteCount { get; private set; }
            internal string TilePathDigest { get; private set; }
            internal IReadOnlyList<string> SoftlockReasons { get; private set; }
            internal int StaticSoftlockCandidateCount => SoftlockReasons.Count;

            internal static ReachabilityAudit Build(
                ReferenceFixture source,
                SectorCanvasOwnershipPlan ownership,
                IReadOnlyList<SectorPlannerGrayboxFixture> fixtures)
            {
                var result = new ReachabilityAudit();
                var graph = source.SpineEnvelopePlan.Graph;
                var nodes = graph.Nodes.ToDictionary(value => value.NodeId, StringComparer.Ordinal);
                var sectors = source.Input.Sectors.ToDictionary(value => value.Coordinate);
                var edgePass = new Dictionary<string, bool>(StringComparer.Ordinal);
                var reasons = new List<string>();
                var digestMaterial = new List<string>();

                foreach (var edge in graph.Edges.OrderBy(value => value.EdgeId, StringComparer.Ordinal))
                {
                    result.RequiredWitnessCount += 2;
                    if (!nodes.TryGetValue(edge.FromNodeId, out var from) || !nodes.TryGetValue(edge.ToNodeId, out var to))
                    {
                        result.MissingWitnessCount++;
                        edgePass[edge.EdgeId] = false;
                        reasons.Add("MISSING_ENDPOINT:" + edge.EdgeId);
                        continue;
                    }

                    var path = PathExists(edge, from.Coordinate, to.Coordinate);
                    edgePass[edge.EdgeId] = path;
                    if (!path) reasons.Add("DISCONNECTED_TILE_PATH:" + edge.EdgeId);
                    foreach (var coordinate in edge.CenterlineCells.Distinct())
                    {
                        if (!HasOwnershipWitness(ownership, from.SectorCoordinate, coordinate))
                        {
                            result.MissingOwnershipWitnessCount++;
                            reasons.Add("MISSING_OWNERSHIP:" + edge.EdgeId + ":" + coordinate.X + "," + coordinate.Y);
                        }
                        if (ownership.WinnerClaims.Any(value => value.SectorCoordinate == from.SectorCoordinate &&
                            value.Coordinate == coordinate && value.Plane == SectorCanvasOwnershipPlane.Terrain &&
                            (value.OwnerKind == SectorCanvasOwnerKind.ActivityMarker || value.OwnerKind == SectorCanvasOwnerKind.EventMarker)))
                        {
                            result.ActivityEventMarkerRequiredForRouteCount++;
                            reasons.Add("MARKER_REQUIRED_FOR_ROUTE:" + edge.EdgeId);
                        }
                    }
                    digestMaterial.Add(edge.EdgeId + "=" + (path ? "PASS" : "FAIL") + ":" +
                        string.Join(",", edge.CenterlineCells.Select(value => value.X + "." + value.Y)));
                }

                var sectorPass = new Dictionary<SectorCoord, bool>();
                foreach (var sector in source.Input.Sectors)
                {
                    var edges = graph.Edges.Where(value => value.SectorIndex == sector.SectorIndex).ToArray();
                    sectorPass[sector.Coordinate] = edges.Length > 0 && edges.All(value => edgePass[value.EdgeId]);
                }

                foreach (var item in fixtures.Where(value => value.Kind == SectorPlannerGrayboxFixtureKind.OneSector))
                {
                    result.OneSectorChecksRequired++;
                    if (sectorPass[item.CenterSector]) result.OneSectorChecksPassed++;
                    else
                    {
                        result.RequiredRouteSoftlocks++;
                        reasons.Add("ONE_SECTOR_ROUTE:" + item.FixtureId);
                    }
                }

                foreach (var item in fixtures.Where(value => value.Kind == SectorPlannerGrayboxFixtureKind.ThreeSector))
                {
                    result.ThreeSectorChecksRequired++;
                    var allLocalRoutes = sectorPass[item.CenterSector] && item.NeighborSectors.All(value => sectorPass[value]);
                    if (allLocalRoutes) result.ThreeSectorChecksPassed++;
                    else
                    {
                        result.RequiredRouteSoftlocks++;
                        reasons.Add("THREE_SECTOR_ROUTE:" + item.FixtureId);
                    }

                    foreach (var neighborCoordinate in item.NeighborSectors)
                    {
                        if (!TryDirection(item.CenterSector, neighborCoordinate, out var side))
                        {
                            reasons.Add("NON_CARDINAL_NEIGHBOR:" + item.FixtureId + ":" + neighborCoordinate);
                            continue;
                        }
                        var center = sectors[item.CenterSector];
                        var neighbor = sectors[neighborCoordinate];
                        if (!Opens(center, side) || !Opens(neighbor, Opposite(side))) continue;
                        result.SocketContinuityChecksRequired++;
                        if (sectorPass[item.CenterSector] && sectorPass[neighborCoordinate])
                            result.SocketContinuityChecksPassed++;
                        else reasons.Add("SOCKET_CONTINUITY:" + item.FixtureId + ":" + neighborCoordinate);
                    }
                }

                foreach (var sector in source.Input.Sectors.Where(value => value.Boundaries.Count != 0))
                {
                    result.BoundaryBridgeChecksRequired++;
                    var bridgeNodes = graph.Nodes.Where(value => value.SectorIndex == sector.SectorIndex &&
                        value.Kind == SectorSpineNodeKind.BoundaryBridge).ToArray();
                    if (bridgeNodes.Length > 0 && bridgeNodes.All(value => NodeConnected(value, graph.Edges, edgePass)))
                        result.BoundaryBridgeChecksPassed++;
                    else
                    {
                        result.BoundaryBridgeSoftlocks++;
                        reasons.Add("BOUNDARY_BRIDGE:" + sector.SectorIndex);
                    }
                }

                foreach (var group in graph.Nodes.Where(value => value.Kind == SectorSpineNodeKind.SpecialEntry ||
                    value.Kind == SectorSpineNodeKind.SpecialReturn).GroupBy(value => value.SectorIndex))
                {
                    result.SpecialEntranceChecksRequired++;
                    var entry = group.Any(value => value.Kind == SectorSpineNodeKind.SpecialEntry && NodeConnected(value, graph.Edges, edgePass));
                    var returnPath = group.Any(value => value.Kind == SectorSpineNodeKind.SpecialReturn && NodeConnected(value, graph.Edges, edgePass));
                    if (entry && returnPath) result.SpecialEntranceChecksPassed++;
                    else
                    {
                        result.SpecialEntranceSoftlocks++;
                        reasons.Add("SPECIAL_ENTRY_RETURN:" + group.Key);
                    }
                }

                result.TilePathDigest = Hash(string.Join("\n", digestMaterial.OrderBy(value => value, StringComparer.Ordinal)));
                result.SoftlockReasons = new ReadOnlyCollection<string>(reasons.Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray());
                return result;
            }

            private static bool PathExists(SectorSpineEdge edge, LocalTileCoord start, LocalTileCoord end)
            {
                var walkable = new HashSet<LocalTileCoord>(edge.CenterlineCells) { start, end };
                var visited = new HashSet<LocalTileCoord> { start };
                var pending = new Queue<LocalTileCoord>();
                pending.Enqueue(start);
                while (pending.Count != 0)
                {
                    var current = pending.Dequeue();
                    if (current == end) return true;
                    foreach (var next in Neighbors(current))
                    {
                        if (walkable.Contains(next) && visited.Add(next)) pending.Enqueue(next);
                    }
                }
                return false;
            }

            private static IEnumerable<LocalTileCoord> Neighbors(LocalTileCoord value)
            {
                if (value.X > 0) yield return new LocalTileCoord(value.X - 1, value.Y);
                if (value.X < 47) yield return new LocalTileCoord(value.X + 1, value.Y);
                if (value.Y > 0) yield return new LocalTileCoord(value.X, value.Y - 1);
                if (value.Y < 31) yield return new LocalTileCoord(value.X, value.Y + 1);
            }

            private static bool HasOwnershipWitness(
                SectorCanvasOwnershipPlan ownership,
                SectorCoord sector,
                LocalTileCoord coordinate) => ownership.OwnedCells.Any(value =>
                    value.SectorCoordinate == sector && value.Coordinate == coordinate &&
                    value.Plane == SectorCanvasOwnershipPlane.Terrain) || ownership.EvidenceClaims.Any(value =>
                    value.SectorCoordinate == sector && value.Coordinate == coordinate);

            private static bool NodeConnected(
                SectorSpineNode node,
                IEnumerable<SectorSpineEdge> edges,
                IReadOnlyDictionary<string, bool> edgePass) => edges.Any(value =>
                    (string.Equals(value.FromNodeId, node.NodeId, StringComparison.Ordinal) ||
                     string.Equals(value.ToNodeId, node.NodeId, StringComparison.Ordinal)) && edgePass[value.EdgeId]);

            private static bool TryDirection(SectorCoord from, SectorCoord to, out SectorPlannerSide side)
            {
                if (to.X == from.X - 1 && to.Y == from.Y) { side = SectorPlannerSide.Left; return true; }
                if (to.X == from.X + 1 && to.Y == from.Y) { side = SectorPlannerSide.Right; return true; }
                if (to.X == from.X && to.Y == from.Y - 1) { side = SectorPlannerSide.Up; return true; }
                if (to.X == from.X && to.Y == from.Y + 1) { side = SectorPlannerSide.Down; return true; }
                side = default(SectorPlannerSide);
                return false;
            }

            private static SectorPlannerSide Opposite(SectorPlannerSide side)
            {
                switch (side)
                {
                    case SectorPlannerSide.Left: return SectorPlannerSide.Right;
                    case SectorPlannerSide.Right: return SectorPlannerSide.Left;
                    case SectorPlannerSide.Up: return SectorPlannerSide.Down;
                    default: return SectorPlannerSide.Up;
                }
            }

            private static bool Opens(SectorPlannerSectorSnapshot sector, SectorPlannerSide side)
            {
                switch (sector.Route.RouteType)
                {
                    case 1:
                        return side == SectorPlannerSide.Left || side == SectorPlannerSide.Right;
                    case 2:
                        return side == SectorPlannerSide.Left || side == SectorPlannerSide.Right || side == SectorPlannerSide.Down;
                    case 3:
                        return side == SectorPlannerSide.Left || side == SectorPlannerSide.Right || side == SectorPlannerSide.Up;
                    case 4:
                        if (side == SectorPlannerSide.Up || side == SectorPlannerSide.Down) return true;
                        return sector.Route.ExternalSockets.Contains(SocketToken(side));
                    default:
                        return sector.Route.ExternalSockets.Contains(SocketToken(side));
                }
            }

            private static string SocketToken(SectorPlannerSide side)
            {
                switch (side)
                {
                    case SectorPlannerSide.Left: return "SOCKET_L";
                    case SectorPlannerSide.Right: return "SOCKET_R";
                    case SectorPlannerSide.Up: return "SOCKET_U";
                    default: return "SOCKET_D";
                }
            }
        }

        private static SectorPlannerAttemptTraceInput[] MetricsInputs() => new[]
        {
            Attempt(0, 0, SectorPlannerRetryFailureOwner.PatternSelection, "MISSING_PATTERN", 0, false, "MP_B", "MP_A"),
            Attempt(1, 1, SectorPlannerRetryFailureOwner.PatternApplication, "TRANSFORM_REJECT", 0, false, "R0", "FLIP_X"),
            Attempt(2, 2, SectorPlannerRetryFailureOwner.ClusterPlacement, "CANDIDATE_RANKING", 0, false, "VARIANT_B", "VARIANT_A"),
            Attempt(3, 3, SectorPlannerRetryFailureOwner.ClusterPlacement, "FOOTPRINT_OVERLAP", 0, false, "FOOTPRINT_B", "FOOTPRINT_A"),
            Attempt(4, 4, SectorPlannerRetryFailureOwner.PatternRender, "MAP10_RENDER_REJECT", 0, false, "MP_RENDER_B", "MP_RENDER_A"),
            Attempt(5, 5, SectorPlannerRetryFailureOwner.SpineEnvelope, "CANNOT_CONNECT", 0, true, "SPINE_B", "SPINE_A"),
        };

        private static SectorPlannerAttemptTraceInput Attempt(
            int attempt,
            int node,
            SectorPlannerRetryFailureOwner owner,
            string code,
            int recoverySequence,
            bool recovered,
            params string[] candidates) =>
            new SectorPlannerAttemptTraceInput(attempt, node,
                new SectorPlannerRetryFailure(owner, code, "SUBJECT_" + code, "DETAIL_" + code, recoverySequence),
                candidates, recovered);

        private static SectorPlannerRetryBuildRequest RetryRequest(
            SectorCanvasOwnershipPlan canvas,
            IEnumerable<SectorPlannerAttemptTraceInput> inputs,
            SectorPlannerRetryPolicy policy = null,
            ulong seed = 0x14090001UL) =>
            new SectorPlannerRetryBuildRequest(canvas, policy ?? SectorPlannerRetryPolicy.CreateDefault(), RetryRngFactory(),
                seed, new SectorCoord(2, 2), sourceAttemptInputs: inputs,
                publicationLabel: SectorPlannerRetryExecutor.ReferencePublicationLabel);

        private static DeterministicRngStreamFactory RetryRngFactory()
        {
            var definitions = new SortedDictionary<string, RngStreamDefinition>(StringComparer.Ordinal)
            {
                { WorldGenerationRngStreams.SectorRecipeStreamId, Definition(WorldGenerationRngStreams.SectorRecipeStreamId, "E9931A70C2D520F4", "SECTOR") },
                { WorldGenerationRngStreams.PopulationStreamId, Definition(WorldGenerationRngStreams.PopulationStreamId, "A63D4078F9E21C55", "SPAWN") },
            };
            var set = (WorldRouteDefinitionSet)FormatterServices.GetUninitializedObject(typeof(WorldRouteDefinitionSet));
            SetAutoProperty(set, "RngStreams", new ReadOnlyDictionary<string, RngStreamDefinition>(definitions));
            return new DeterministicRngStreamFactory(set);
        }

        private sealed class ReferenceFixture
        {
            private static readonly SectorCoord Quiet = new SectorCoord(1, 1);
            private static readonly SectorCoord Village = new SectorCoord(2, 1);
            private static readonly SectorCoord Core = new SectorCoord(3, 1);
            private static readonly SectorCoord Forge = new SectorCoord(1, 2);
            private static readonly SectorCoord Plain = new SectorCoord(2, 2);
            private static readonly SectorCoord Boss = new SectorCoord(3, 2);
            private static readonly SectorCoord Activity = new SectorCoord(1, 3);
            private static readonly SectorCoord Merchant = new SectorCoord(2, 3);
            private static readonly SectorCoord Maru = new SectorCoord(3, 3);
            private static readonly string CatalogDigest = Hash("MAP11_CATALOG");
            private static readonly string SignatureDigest = Hash("MAP11_SIGNATURES");
            private static readonly string ManifestDigest = Hash("MAP12_MANIFEST");

            private ReferenceFixture(
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

            internal static ReferenceFixture Create(bool reverse)
            {
                var sectors = CreateSectors();
                if (reverse) sectors.Reverse();
                var authority = SectorPlannerAuthorityDigestSnapshot.CaptureCurrentPublicAuthorities(
                    Hash("FOUNDATION"), Hash("MICRO_PATTERN"), 24, Hash("TERRAIN_CLUSTER"), 16,
                    Hash("ACTIVITY"), 7, Hash("EVENT"), 5, Hash("SPECIAL"));
                var input = SectorPlannerInputBuilder.Build(new SectorPlannerInputRequest(
                    sectors, authority, SectorPlannerInputBuilder.ReferencePublicationLabel));
                Require(input.Success, input.Errors);
                var assignments = SectorPacingRolePlanner.Assign(input.Input).ToList();
                var anchors = CreateAnchors();
                if (reverse) { assignments.Reverse(); anchors.Reverse(); }
                var anchor = SectorFixedAnchorPlanner.Build(new SectorFixedAnchorBuildRequest(
                    input.Input, assignments, anchors, SectorFixedAnchorPlanner.ReferencePublicationLabel));
                Require(anchor.Success, anchor.Errors);
                var clusterCatalog = CreateClusterCatalog();
                if (reverse) clusterCatalog.Reverse();
                var candidates = SectorClusterCandidateBuilder.Build(new SectorClusterCandidateBuildRequest(
                    input.Input, assignments, anchor.Plan, clusterCatalog,
                    SectorClusterCandidateBuilder.ReferenceCandidatePublicationLabel));
                Require(candidates.Success, candidates.Errors);
                var placement = SectorClusterPlacementPlanner.Place(new SectorClusterPlacementRequest(
                    candidates.CandidateSet, anchor.Plan,
                    SectorClusterPlacementPlanner.ReferencePlacementPublicationLabel));
                Require(placement.Success, placement.Errors);
                var spineRequest = new SectorSpineEnvelopeBuildRequest(input.Input, assignments, anchor.Plan,
                    placement.Plan, SectorSpineGraphBuilder.ReferenceGraphPublicationLabel,
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
                return new ReferenceFixture(input.Input, assignments, anchor.Plan, placement.Plan,
                    spine.Plan, roles.Plan, render.Plan);
            }

            internal SectorCanvasOwnershipPlan BuildCanvas(bool reverse)
            {
                var fill = SectorQuietFillPlanner.Fill(new SectorQuietActivityEventBuildRequest(
                    Input, Assignments, AnchorPlan, PlacementPlan, SpineEnvelopePlan, RolePlan, RenderPlan,
                    SectorQuietFillPlanner.ReferencePublicationLabel));
                Require(fill.Success, fill.Errors);
                var authority = CreateAuthorities(fill.Plan, reverse);
                var placed = SectorActivityEventPlacementPlanner.Place(authority.Request(fill.Plan));
                Require(placed.Success, placed.Errors);
                var ownershipRequest = new SectorCanvasOwnershipBuildRequest(
                    Input, Assignments, AnchorPlan, PlacementPlan, SpineEnvelopePlan, RolePlan, RenderPlan,
                    placed.Plan, SectorCanvasOwnershipClaimBuilder.ReferencePublicationLabel);
                var claims = SectorCanvasOwnershipClaimBuilder.BuildClaims(ownershipRequest);
                Require(claims.Success, claims.Errors);
                var resolved = SectorCanvasOwnershipResolver.Resolve(claims);
                Require(resolved.Success, resolved.Errors);
                return resolved.Plan;
            }

            private AuthorityPackage CreateAuthorities(SectorQuietFillPlan fill, bool reverse)
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
                    var activityId = new ActivityStructureId("ACTIVITY_MAP14_09_" + placement.SectorIndex.ToString("D3", CultureInfo.InvariantCulture));
                    var shell = Hash("SHELL|" + placement.ClusterId.Value);
                    var safety = Hash("SAFETY|" + placement.ClusterId.Value);
                    profiles.Add(new ActivityPlacementProfile(activityId, placement.ClusterId, placement.VariantId,
                        Hash("ACTIVITY|" + activityId.Value), shell, safety, new[] { Biome(sector) },
                        new[] { assignment.PrimaryRole }, new[] { sector.Route.AccessClass },
                        placement.Cells.Count, placement.Cells.Count, 2, 2, 100, ActivityStrengthClass.Strong));
                    var opportunity = new ActivityPlacementOpportunity(
                        "ACTIVITY_OPPORTUNITY_" + placement.SectorIndex.ToString("D3", CultureInfo.InvariantCulture),
                        sector.Coordinate, owned.PatchId.Value, Biome(sector), placement.ClusterId, placement.VariantId,
                        assignment.PrimaryRole, sector.Route.AccessClass, placement.Cells.Count,
                        new ActivityPlacementClearanceEvidence(rectangle[0], 2, 2, rectangle, rectangle,
                            Array.Empty<LocalTileCoord>(), Array.Empty<LocalTileCoord>()),
                        CatalogDigest, SignatureDigest, ManifestDigest, shell, safety);
                    projections.Add(new SectorActivityOpportunityProjection(opportunity, rectangle[0],
                        MarkerForActivity(assignment.PrimaryRole), safety));
                }
                if (reverse) { profiles.Reverse(); projections.Reverse(); }
                var activityIndex = ActivityCandidateIndexCompiler.Compile(new ActivityCandidateIndexCompileRequest(
                    profiles, projections.Select(value => value.Authority), ownership,
                    CatalogDigest, SignatureDigest, ManifestDigest));
                Require(activityIndex.Success, activityIndex.Errors);
                var activityPlan = ActivityFrequencyPlanner.Plan(new ActivityFrequencyPlanRequest(
                    activityIndex.Index, new ActivityFrequencyPolicy(120, 1, 1, 1),
                    0x14090002UL, 0, RetryRngFactory()));
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
                            ? EventMarkerTargetSourceKind.Activity : EventMarkerTargetSourceKind.TerrainCluster;
                    var marker = new EventMarkerTargetEvidence(new EventMarkerId("MARKER_MAP14_09"),
                        sourceKind, owner, activityProjection.MarkerCoordinate, activityProjection.MarkerCoordinate,
                        markerKind.ToString(), "QUIET", "QUIET", Hash("STATIC"), Hash("STATIC"),
                        Hash("PROTECTION"), Hash("PROTECTION"), default(SpecialPersistenceKey), string.Empty, string.Empty);
                    var opportunity = new EventOverlayOpportunity(
                        "EVENT_OPP_" + sector.SectorIndex.ToString("D3", CultureInfo.InvariantCulture),
                        sector.Coordinate, owned.PatchId.Value, sector.SectorIndex, Biome(sector),
                        Assignments.Single(value => value.Coordinate == sector.Coordinate).PrimaryRole,
                        sector.Route.AccessClass, new TerrainClusterId("TC_MAP14_09_EVENT"), null,
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
                    0x14090003UL, 0, RetryRngFactory()));
                Require(eventPlan.Success, eventPlan.Errors);
                return new AuthorityPackage(projections, activityIndex.Index, activityPlan.Plan,
                    eventProjections, eventIndex.Index, eventPlan.Plan);
            }

            private static List<SectorPlannerSectorSnapshot> CreateSectors() => new List<SectorPlannerSectorSnapshot>
            {
                Sector(Quiet, MoonpalaceBiomeId.MoonCrater, new[] { PacingRole.Quiet, PacingRole.Traversal }, 0, AccessClass.OptionalTool,
                    boundaries: Boundary("PAIR_CRATER_ROOT"), quiet: true),
                Sector(Village, MoonpalaceBiomeId.CassiaRoot, new[] { PacingRole.Safe, PacingRole.Landmark, PacingRole.Traversal }, 1,
                    AccessClass.MandatoryNoTool, boundaries: Boundary("PAIR_CRATER_MILL"),
                    special: new SectorPlannerSpecialRegionSnapshot("REGION_VILLAGE", SectorPlannerSpecialRegionKind.Village,
                        SectorPlannerSpecialRegionBinding.ReferenceOnly, "FP_VILLAGE_REFERENCE", false, false, false),
                    ordinal: 2, optionalDistance: 0),
                Sector(Core, MoonpalaceBiomeId.CassiaRoot, new[] { PacingRole.Resource, PacingRole.Traversal }, 2, AccessClass.MandatoryNoTool,
                    boundaries: Boundary("PAIR_CRATER_DOUGH"),
                    sites: new[] { new SectorPlannerSiteSnapshot("SITE_CORE", "CORE_RESOURCE", "RES_CORE", true) },
                    special: Mandatory("REGION_CORE", SectorPlannerSpecialRegionKind.CoreResource, "FP_CORE"), mandatoryDistance: 0),
                Sector(Forge, MoonpalaceBiomeId.AbandonedMill, new[] { PacingRole.Landmark, PacingRole.Machinery, PacingRole.Traversal }, 3,
                    AccessClass.MandatoryNoTool, boundaries: Boundary("PAIR_ROOT_MILL"),
                    sites: new[] { new SectorPlannerSiteSnapshot("SITE_FORGE", "FORGE", "RES_FORGE", true) },
                    special: Mandatory("REGION_FORGE", SectorPlannerSpecialRegionKind.Forge, "FP_FORGE"), mandatoryDistance: 0),
                Sector(Plain, MoonpalaceBiomeId.MoonCrater, new[] { PacingRole.Traversal, PacingRole.Recovery }, 4,
                    AccessClass.MandatoryNoTool, sockets: new[] { "SOCKET_L", "SOCKET_R", "SOCKET_U", "SOCKET_D" },
                    highRoute: true, recovery: true),
                Sector(Boss, MoonpalaceBiomeId.MoonDough, new[] { PacingRole.Boss, PacingRole.Traversal }, 4, AccessClass.MandatoryNoTool,
                    boundaries: Boundary("PAIR_ROOT_DOUGH"),
                    sites: new[] { new SectorPlannerSiteSnapshot("SITE_BOSS", "BOSS_GATE", "RES_BOSS", true) },
                    special: Mandatory("REGION_BOSS", SectorPlannerSpecialRegionKind.Boss, "FP_BOSS"), mandatoryDistance: 0),
                Sector(Activity, MoonpalaceBiomeId.MoonCrater, new[] { PacingRole.Activity, PacingRole.Traversal }, 0, AccessClass.OptionalTool,
                    boundaries: Boundary("PAIR_MILL_DOUGH"), activity: true, eventAvailable: true, ordinal: 5),
                Sector(Merchant, MoonpalaceBiomeId.CassiaRoot, new[] { PacingRole.Discovery }, 1, AccessClass.MandatoryNoTool,
                    special: new SectorPlannerSpecialRegionSnapshot("REGION_MERCHANT", SectorPlannerSpecialRegionKind.Merchant,
                        SectorPlannerSpecialRegionBinding.DeferredOptionalLocal, string.Empty, false, false, false),
                    optional: new[] { new SectorPlannerOptionalRegionSnapshot("REGION_MERCHANT", SectorPlannerSpecialRegionKind.Merchant, true, true, false) },
                    optionalDistance: 1),
                Sector(Maru, MoonpalaceBiomeId.AbandonedMill, new[] { PacingRole.Discovery, PacingRole.Traversal }, 1,
                    AccessClass.MandatoryNoTool,
                    optional: new[] { new SectorPlannerOptionalRegionSnapshot("REGION_MARU", SectorPlannerSpecialRegionKind.Maru, true, true, false) },
                    neighbors: new[]
                    {
                        new SectorPlannerNeighborSnapshot(SectorPlannerSide.Left, Merchant, 1, AccessClass.MandatoryNoTool, Array.Empty<string>(), PacingRole.Discovery),
                        new SectorPlannerNeighborSnapshot(SectorPlannerSide.Up, Boss, 4, AccessClass.MandatoryNoTool, Array.Empty<string>(), PacingRole.Boss),
                    }),
            };

            private static SectorPlannerSectorSnapshot Sector(
                SectorCoord coordinate,
                MoonpalaceBiomeId biome,
                IEnumerable<PacingRole> roles,
                int routeType,
                AccessClass access,
                IEnumerable<string> sockets = null,
                IEnumerable<SectorPlannerBoundarySnapshot> boundaries = null,
                IEnumerable<SectorPlannerSiteSnapshot> sites = null,
                SectorPlannerSpecialRegionSnapshot special = null,
                IEnumerable<SectorPlannerOptionalRegionSnapshot> optional = null,
                IEnumerable<SectorPlannerNeighborSnapshot> neighbors = null,
                bool highRoute = false,
                bool recovery = false,
                bool quiet = false,
                bool activity = false,
                bool eventAvailable = false,
                int ordinal = 4,
                int mandatoryDistance = 2,
                int optionalDistance = 3) =>
                new SectorPlannerSectorSnapshot(coordinate, (coordinate.Y * 13) + coordinate.X, 48, 32,
                    new SectorPlannerBiomeSnapshot("PATCH_" + coordinate.X + "_" + coordinate.Y, biome.ToString()),
                    new SectorPlannerRouteSnapshot(routeType, access, sockets, highRoute, recovery),
                    boundaries, sites, special ?? SectorPlannerSpecialRegionSnapshot.None, optional, neighbors,
                    new SectorPlannerWorldProgressSnapshot(ordinal, "CHAPTER_REFERENCE", "BRANCH_REFERENCE",
                        mandatoryDistance, optionalDistance), roles, quiet, activity, eventAvailable);

            private static SectorPlannerBoundarySnapshot[] Boundary(string pair) => new[]
            {
                new SectorPlannerBoundarySnapshot(SectorPlannerSide.Right, pair, "CANDIDATE_" + pair, 1),
            };

            private static SectorPlannerSpecialRegionSnapshot Mandatory(string id, SectorPlannerSpecialRegionKind kind, string footprint) =>
                new SectorPlannerSpecialRegionSnapshot(id, kind, SectorPlannerSpecialRegionBinding.ReservedMandatory,
                    footprint, true, true, true);

            private static List<SectorFixedAnchorProjection> CreateAnchors()
            {
                var result = new List<SectorFixedAnchorProjection>
                {
                    RouteAnchor("ANCHOR_SOCKET_L", "SOCKET_L", SectorPlannerSide.Left, new SectorFixedAnchorRect(0, 14, 1, 4)),
                    RouteAnchor("ANCHOR_SOCKET_R", "SOCKET_R", SectorPlannerSide.Right, new SectorFixedAnchorRect(47, 14, 1, 4)),
                    RouteAnchor("ANCHOR_SOCKET_U", "SOCKET_U", SectorPlannerSide.Up, new SectorFixedAnchorRect(22, 0, 4, 1)),
                    RouteAnchor("ANCHOR_SOCKET_D", "SOCKET_D", SectorPlannerSide.Down, new SectorFixedAnchorRect(22, 31, 4, 1)),
                    new SectorFixedAnchorProjection("ANCHOR_VILLAGE_REFERENCE", Village,
                        SectorFixedAnchorKind.ReferenceOnlyMarker, SectorFixedAnchorSource.SpecialRegionSnapshot,
                        SectorFixedAnchorPriority.ReferenceOnly, new SectorFixedAnchorRect(23, 15, 1, 1), "REGION_VILLAGE"),
                };
                AddBoundary(result, Quiet, "PAIR_CRATER_ROOT");
                AddBoundary(result, Village, "PAIR_CRATER_MILL");
                AddBoundary(result, Core, "PAIR_CRATER_DOUGH");
                AddBoundary(result, Forge, "PAIR_ROOT_MILL");
                AddBoundary(result, Boss, "PAIR_ROOT_DOUGH");
                AddBoundary(result, Activity, "PAIR_MILL_DOUGH");
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

            private static void AddBoundary(ICollection<SectorFixedAnchorProjection> target, SectorCoord sector, string pair)
            {
                var candidate = "CANDIDATE_" + pair;
                var rect = new SectorFixedAnchorRect(47, 4, 1, 4);
                target.Add(new SectorFixedAnchorProjection("ANCHOR_" + pair + "_FIXED", sector,
                    SectorFixedAnchorKind.BoundaryFixedSlice, SectorFixedAnchorSource.BoundarySnapshot,
                    SectorFixedAnchorPriority.BoundaryFixedSlice, rect, candidate, SectorPlannerSide.Right, true, pair));
                target.Add(new SectorFixedAnchorProjection("ANCHOR_" + pair + "_WARNING", sector,
                    SectorFixedAnchorKind.BoundaryWarning, SectorFixedAnchorSource.BoundarySnapshot,
                    SectorFixedAnchorPriority.BoundaryWarning, rect, candidate, SectorPlannerSide.Right, true, pair));
            }

            private static void AddSpecial(ICollection<SectorFixedAnchorProjection> target, SectorCoord coordinate,
                string token, string region, string site)
            {
                target.Add(new SectorFixedAnchorProjection("ANCHOR_" + token + "_FOOTPRINT", coordinate,
                    SectorFixedAnchorKind.SpecialFootprint, SectorFixedAnchorSource.SpecialRegionSnapshot,
                    SectorFixedAnchorPriority.SpecialReservation, new SectorFixedAnchorRect(18, 12, 12, 8),
                    region, placedOwnershipClaim: true, progressionBlockerClaim: true));
                target.Add(new SectorFixedAnchorProjection("ANCHOR_" + token + "_ENTRY", coordinate,
                    SectorFixedAnchorKind.SpecialEntryReturn, SectorFixedAnchorSource.SpecialRegionSnapshot,
                    SectorFixedAnchorPriority.SpecialTransition, new SectorFixedAnchorRect(16, 14, 2, 4),
                    region, placedOwnershipClaim: true, progressionBlockerClaim: true));
                target.Add(new SectorFixedAnchorProjection("ANCHOR_" + token + "_BUFFER", coordinate,
                    SectorFixedAnchorKind.SpecialApronBuffer, SectorFixedAnchorSource.SpecialRegionSnapshot,
                    SectorFixedAnchorPriority.SpecialTransition, new SectorFixedAnchorRect(30, 12, 2, 8),
                    region, placedOwnershipClaim: true, progressionBlockerClaim: true));
                target.Add(new SectorFixedAnchorProjection("ANCHOR_" + token + "_SITE", coordinate,
                    SectorFixedAnchorKind.SiteReservation, SectorFixedAnchorSource.SiteSnapshot,
                    SectorFixedAnchorPriority.SpecialReservation, new SectorFixedAnchorRect(20, 22, 4, 2),
                    site, placedOwnershipClaim: true));
            }

            private static List<SectorClusterSourceProjection> CreateClusterCatalog()
            {
                var roles = Enum.GetValues(typeof(PacingRole)).Cast<PacingRole>().Where(value => value != PacingRole.None).ToArray();
                var routes = new[] { 0, 1, 2, 3, 4 };
                var access = new[] { AccessClass.MandatoryNoTool, AccessClass.OptionalTool };
                var sockets = new[] { "SOCKET_L", "SOCKET_R", "SOCKET_U", "SOCKET_D" };
                return new List<SectorClusterSourceProjection>
                {
                    Source("TC_REF_CRATER", "SPINE_CRATER_R0", MoonpalaceBiomeId.MoonCrater, roles, routes, access, sockets, H2(), Origins(2,1), 10),
                    Source("TC_REF_ROOT", "SPINE_ROOT_R0", MoonpalaceBiomeId.CassiaRoot, roles, routes, access, sockets, H2(), Origins(2,1), 20),
                    Source("TC_REF_MILL", "SPINE_MILL_R0", MoonpalaceBiomeId.AbandonedMill, roles, routes, access, sockets, L3(), Origins(2,2), 30),
                    Source("TC_REF_DOUGH", "SPINE_DOUGH_R0", MoonpalaceBiomeId.MoonDough, roles, routes, access, sockets, H2(), Origins(2,1), 40),
                };
            }

            private static SectorClusterSourceProjection Source(
                string cluster, string variant, MoonpalaceBiomeId biome, IEnumerable<PacingRole> pacing,
                IEnumerable<int> routes, IEnumerable<AccessClass> access, IEnumerable<string> sockets,
                IEnumerable<SectorClusterFootprintCell> cells, IEnumerable<SectorClusterFootprintCell> origins, int order) =>
                new SectorClusterSourceProjection(new TerrainClusterId(cluster), new SpineVariantId(variant),
                    ClusterFootprintTransform.R0, biome, pacing, routes, access, sockets, cells, origins,
                    2, 5, true, true, order, 0);

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

            private static SectorPatternSourceProjection Pattern(string id, int salt,
                IEnumerable<SectorPatternZoneKind> zones, IEnumerable<SectorClusterRoleCellKind> roles,
                IEnumerable<PacingRole> pacing, int order) =>
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
                    var id = new BiomePatchId("PATCH_MAP14_09_" + BiomeToken(group.Key));
                    patchByBiome.Add(group.Key, id);
                    patches.Add(new BiomePatch(id, BiomeToken(group.Key), "RULE_MAP14_09", BiomePatchRole.Satellite,
                        new[] { new BiomePatchSeed(indices[0], WorldGridIndex.ToCoordinate(indices[0]), BiomePatchRole.Satellite, null) }, indices));
                }
                var ownership = Enumerable.Range(0, WorldGenConstants.SectorCount).Select(index =>
                {
                    var biome = BiomeForIndex(index);
                    return new BiomeSectorOwnership(index, WorldGridIndex.ToCoordinate(index),
                        BiomeToken(biome), string.Empty, patchByBiome[biome]);
                }).ToArray();
                return new BiomePatchSnapshot(1409UL, patches, ownership, Array.Empty<BiomePatchSiteBinding>());
            }

            private static MoonpalaceBiomeId BiomeForIndex(int index)
            {
                if (index == 15 || index == 16 || index == 41) return MoonpalaceBiomeId.CassiaRoot;
                if (index == 27 || index == 42) return MoonpalaceBiomeId.AbandonedMill;
                if (index == 29) return MoonpalaceBiomeId.MoonDough;
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
                MoonpalaceBiomeId.Parse(sector.Biome.BiomeId);

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
                var access = new[] { AccessClass.MandatoryNoTool, AccessClass.OptionalTool };
                var cluster = new TerrainClusterId("TC_MAP14_09_EVENT");
                var empty = new EventOverlayContract(new EventOverlayId("EVT_MAP14_09_EMPTY"),
                    EventOverlayKind.Empty, cluster, null, Array.Empty<EventMarkerAssignment>());
                var terrain = new EventOverlayContract(new EventOverlayId("EVT_MAP14_09_TERRAIN"),
                    EventOverlayKind.Cosmetic, cluster, null,
                    new[] { new EventMarkerAssignment(new EventMarkerId("MARKER_MAP14_09"),
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
                    : sector.ActivityCatalogAvailable ? SectorActivityEventMarkerKind.EventActivity : SectorActivityEventMarkerKind.EventTerrain;

            private static SectorClusterFootprintCell[] H2() => new[] { Cell(0, 0), Cell(1, 0) };
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

            internal SectorActivityEventPlacementRequest Request(SectorQuietFillPlan fill) =>
                new SectorActivityEventPlacementRequest(fill, Activities, ActivityIndex, ActivityPlan,
                    Events, EventIndex, EventPlan, SectorActivityEventPlacementPlanner.ReferencePublicationLabel);
        }

        private static RngStreamDefinition Definition(string id, string salt, string scope)
        {
            var definition = (RngStreamDefinition)FormatterServices.GetUninitializedObject(typeof(RngStreamDefinition));
            SetAutoProperty(definition, "RngStreamId", id);
            SetAutoProperty(definition, "SaltHex", Hex(salt));
            SetAutoProperty(definition, "ResetScope", scope);
            SetAutoProperty(definition, "DescriptionKo", "MAP14_09 focused fixture");
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
            var field = target.GetType().GetField("<" + property + ">k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, property);
            field.SetValue(target, value);
        }

        private static void Require<T>(bool success, IEnumerable<T> errors)
        {
            if (!success) throw new InvalidOperationException(string.Join(";", errors ?? Array.Empty<T>()));
        }

        private static string Errors(IEnumerable<SectorPlannerDebugExportError> errors) =>
            string.Join(";", (errors ?? Array.Empty<SectorPlannerDebugExportError>()).Select(value => value.ToString()));

        private static void AssertLowerSha(string value) => Assert.That(value, Does.Match("^[0-9a-f]{64}$"));

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
}
