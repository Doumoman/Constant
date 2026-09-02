using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Baking
{
    [TestFixture]
    [Category("MAP16_03")]
    public sealed class SectorFinalRouteRecoveryValidatorTests
    {
        private ReferenceFinalRouteRecoveryFixture fixture;

        [SetUp]
        public void SetUp()
        {
            fixture = new ReferenceFinalRouteRecoveryFixture();
        }

        [Test]
        public void FinalRouteRecoveryReportPublishesAnchorsWitnessesAndDigests()
        {
            var result = SectorFinalRouteRecoveryValidator.Validate(fixture.AcceptedRequest());

            AssertPass(result);
            var report = result.Report;
            Assert.That(report.Width, Is.EqualTo(48));
            Assert.That(report.Height, Is.EqualTo(32));
            Assert.That(report.ObservedCellCount, Is.EqualTo(1536));
            Assert.That(report.UniqueCoordinateCount, Is.EqualTo(1536));
            Assert.That(report.Anchors.Count, Is.EqualTo(8));
            Assert.That(report.RouteNodeCount, Is.GreaterThan(0));
            Assert.That(report.RouteEdgeCount, Is.GreaterThan(0));
            Assert.That(report.Witnesses.Count, Is.EqualTo(7));
            Assert.That(report.RecoveryWitnesses.Count, Is.EqualTo(1));
            Assert.That(report.InputDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(report.OutputDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(FinalRouteRecoveryDigest.ComputeOutput(report),
                Is.EqualTo(report.OutputDigest));
            Assert.That(() => ((IList<FinalRouteNode>)report.Nodes).Add(report.Nodes[0]),
                Throws.TypeOf<NotSupportedException>());
            Assert.That(() => ((IList<FinalRouteAnchor>)report.Anchors).Add(report.Anchors[0]),
                Throws.TypeOf<NotSupportedException>());

            TestContext.Out.WriteLine("MAP16_03_INPUT_DIGEST=" + report.InputDigest);
            TestContext.Out.WriteLine("MAP16_03_OUTPUT_DIGEST=" + report.OutputDigest);
            TestContext.Out.WriteLine(string.Join(";", new[]
            {
                "MAP16_03_COUNTS=CELLS" + report.ObservedCellCount,
                "UNIQUE" + report.UniqueCoordinateCount,
                "NODES" + report.RouteNodeCount,
                "EDGES" + report.RouteEdgeCount,
                "BASE" + report.BaseRouteWitnessCoveredCount,
                "SOCKET" + report.ExternalSocketWitnessCoveredCount,
                "BOUNDARY" + report.BoundaryApertureWitnessCoveredCount,
                "SPECIAL" + report.SpecialEntranceWitnessCoveredCount,
                "HIGH_FAILURE" + report.HighFailureSampleCoveredCount,
                "RECOVERY" + report.RecoveryWitnessCoveredCount,
                "BLOCKED" + report.BlockedCellCrossingCount,
                "SOFTLOCK" + report.StaticSoftlockCandidateCount,
            }));
        }

        [Test]
        public void BaseEntryToExitWitnessExistsAndAvoidsSolidHazardBlockedCells()
        {
            var result = SectorFinalRouteRecoveryValidator.Validate(fixture.AcceptedRequest());

            AssertPass(result);
            var report = result.Report;
            var witness = report.Witnesses.Single(value =>
                value.Kind == FinalRouteWitnessKind.BaseEntryToExit);
            Assert.That(report.BaseRouteWitnessExists, Is.True);
            Assert.That(report.BaseRouteStartEndMatch, Is.True);
            Assert.That(witness.Path.First(), Is.EqualTo(report.BaseEntryAnchor.Coordinate));
            Assert.That(witness.Path.Last(), Is.EqualTo(report.BaseExitAnchor.Coordinate));
            Assert.That(witness.Path.All(value => !IsBlocked(report.SourceCanvasPlan, value)),
                Is.True);
            Assert.That(report.BaseRouteWitnessRequiredCount, Is.EqualTo(1));
            Assert.That(report.BaseRouteWitnessCoveredCount, Is.EqualTo(1));
            Assert.That(report.BaseRouteWitnessMissingCount, Is.Zero);
            Assert.That(report.SolidCrossingCount, Is.Zero);
            Assert.That(report.HazardCrossingCount, Is.Zero);
            Assert.That(report.BlockedProtectionCrossingCount, Is.Zero);
        }

        [Test]
        public void ExternalSocketsAndBoundaryAperturesConnectToBaseRoute()
        {
            var result = SectorFinalRouteRecoveryValidator.Validate(fixture.AcceptedRequest());

            AssertPass(result);
            var report = result.Report;
            Assert.That(report.ExternalSocketWitnessRequiredCount, Is.EqualTo(2));
            Assert.That(report.ExternalSocketWitnessCoveredCount, Is.EqualTo(2));
            Assert.That(report.ExternalSocketWitnessMissingCount, Is.Zero);
            Assert.That(report.BoundaryApertureWitnessRequiredCount, Is.EqualTo(1));
            Assert.That(report.BoundaryApertureWitnessCoveredCount, Is.EqualTo(1));
            Assert.That(report.BoundaryApertureWitnessMissingCount, Is.Zero);
            Assert.That(report.SpecialEntranceWitnessRequiredCount, Is.EqualTo(1));
            Assert.That(report.SpecialEntranceWitnessCoveredCount, Is.EqualTo(1));
            Assert.That(report.SpecialEntranceWitnessMissingCount, Is.Zero);
            Assert.That(report.Witnesses.Where(value => value.Kind ==
                    FinalRouteWitnessKind.ExternalSocketToBaseRoute || value.Kind ==
                    FinalRouteWitnessKind.BoundaryApertureToBaseRoute || value.Kind ==
                    FinalRouteWitnessKind.SpecialEntranceToBaseRoute)
                .All(value => value.Verdict == FinalRouteWitnessVerdict.Covered), Is.True);
        }

        [Test]
        public void HighRouteFailureSamplesRecoverToBaseRouteWithoutFallbackCarve()
        {
            var result = SectorFinalRouteRecoveryValidator.Validate(fixture.AcceptedRequest());

            AssertPass(result);
            var report = result.Report;
            Assert.That(report.HighRouteBranchAnchors.Count, Is.EqualTo(1));
            Assert.That(report.HighFailureSampleRequiredCount, Is.EqualTo(1));
            Assert.That(report.HighFailureSampleCoveredCount, Is.EqualTo(1));
            Assert.That(report.HighFailureSampleMissingCount, Is.Zero);
            Assert.That(report.RecoveryWitnessRequiredCount, Is.EqualTo(1));
            Assert.That(report.RecoveryWitnessCoveredCount, Is.EqualTo(1));
            Assert.That(report.RecoveryWitnessMissingCount, Is.Zero);
            Assert.That(report.RecoveryWitnesses.All(value =>
                !value.UsesFallbackCarve && !value.UsesSilentWidening &&
                !value.RequiresSectorRerender && !value.RequiresWholeWorldRerandom), Is.True);
            Assert.That(report.FallbackCarveCount, Is.Zero);
            Assert.That(report.SilentWideningCount, Is.Zero);
            Assert.That(report.SectorRerenderCount, Is.Zero);
            Assert.That(report.WholeWorldRerandomCount, Is.Zero);
        }

        [Test]
        public void StaticSoftlockCandidatesAreZeroForAcceptedCanvas()
        {
            var result = SectorFinalRouteRecoveryValidator.Validate(fixture.AcceptedRequest());

            AssertPass(result);
            Assert.That(result.Report.SoftlockCandidates, Is.Empty);
            Assert.That(result.Report.StaticSoftlockCandidateCount, Is.Zero);
            Assert.That(result.Report.BlockedCellCrossingCount, Is.Zero);
        }

        [Test]
        public void RouteRecoveryFailuresAreTypedAndAtomicForMissingBlockedOrIsolatedRoutes()
        {
            var results = new[]
            {
                SectorFinalRouteRecoveryValidator.Validate(null),
                SectorFinalRouteRecoveryValidator.Validate(
                    fixture.Request(ReferenceFinalRouteVariant.MissingCanvas)),
                SectorFinalRouteRecoveryValidator.Validate(
                    fixture.Request(ReferenceFinalRouteVariant.MissingReport)),
                SectorFinalRouteRecoveryValidator.Validate(
                    fixture.Request(ReferenceFinalRouteVariant.MissingEntry)),
                SectorFinalRouteRecoveryValidator.Validate(
                    fixture.Request(ReferenceFinalRouteVariant.BlockedExit)),
                SectorFinalRouteRecoveryValidator.Validate(
                    fixture.Request(ReferenceFinalRouteVariant.IsolatedFailure)),
                SectorFinalRouteRecoveryValidator.Validate(
                    fixture.Request(ReferenceFinalRouteVariant.SourceMismatch)),
            };

            Assert.That(results.All(value => !value.Success), Is.True);
            Assert.That(results.All(value => value.Report == null), Is.True);
            Assert.That(results.All(value => value.InputDigest == string.Empty), Is.True);
            Assert.That(results.All(value => value.OutputDigest == string.Empty), Is.True);
            Assert.That(results.All(value => value.Failures.Count > 0), Is.True);
            var codes = results.SelectMany(value => value.Failures)
                .Select(value => value.Code).Distinct().ToArray();
            Assert.That(codes, Does.Contain(FinalRouteFailureKind.MissingRequest));
            Assert.That(codes, Does.Contain(FinalRouteFailureKind.MissingCanvasPlan));
            Assert.That(codes, Does.Contain(FinalRouteFailureKind.MissingProtectionDensityReport));
            Assert.That(codes, Does.Contain(FinalRouteFailureKind.MissingBaseEntry));
            Assert.That(codes, Does.Contain(FinalRouteFailureKind.BlockedAnchor));
            Assert.That(codes, Does.Contain(FinalRouteFailureKind.MissingHighFailureRecovery));
            Assert.That(codes, Does.Contain(FinalRouteFailureKind.SourceReportMismatch));
            Assert.That(results.SelectMany(value => value.Failures)
                .All(value => value.Subject.Length > 0 && value.Reason.Length > 0), Is.True);
        }

        [Test]
        public void RouteRecoveryDigestIsDeterministicAcrossRepeatReverseAndCulture()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var first = SectorFinalRouteRecoveryValidator.Validate(fixture.AcceptedRequest());
                var repeat = SectorFinalRouteRecoveryValidator.Validate(fixture.AcceptedRequest());
                var reverseClaims = SectorFinalRouteRecoveryValidator.Validate(
                    fixture.AcceptedRequest(reverseClaims: true));
                var reverseEvidence = SectorFinalRouteRecoveryValidator.Validate(
                    fixture.AcceptedRequest(reverseEvidence: true));
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var culture = SectorFinalRouteRecoveryValidator.Validate(fixture.AcceptedRequest());
                var results = new[] { first, repeat, reverseClaims, reverseEvidence, culture };

                Assert.That(results.All(value => value.Success), Is.True,
                    string.Join(";", results.Select(Join)));
                Assert.That(results.Select(value => value.InputDigest).Distinct().Count(), Is.EqualTo(1));
                Assert.That(results.Select(value => value.OutputDigest).Distinct().Count(), Is.EqualTo(1));
                Assert.That(results.Select(value => string.Join("\n",
                    value.Report.Nodes.Select(node => node.StableToken))).Distinct().Count(),
                    Is.EqualTo(1));
                Assert.That(results.Select(value => string.Join("\n",
                    value.Report.Edges.Select(edge => edge.StableToken))).Distinct().Count(),
                    Is.EqualTo(1));
                Assert.That(results.Select(value => string.Join("\n",
                    value.Report.Witnesses.Select(witness => witness.StableToken))).Distinct().Count(),
                    Is.EqualTo(1));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Test]
        public void ValidatorDoesNotMutateCanvasProtectionDensityWorldFilesTilesScenesOrGameplayObjects()
        {
            var request = fixture.AcceptedRequest();
            var plan = request.CanvasPlan;
            var density = request.ProtectionDensityReport;
            var cellTokens = plan.Cells.Select(value => value.StableToken).ToArray();
            var anchorTokens = request.Anchors.Select(value => value.StableToken).ToArray();
            var identities = new[]
            {
                plan.InputDigest, plan.OutputDigest, density.InputDigest, density.OutputDigest,
                plan.Request.Map15ExitDigest, plan.Request.WorldAssemblyDigest,
                plan.Request.SectorOwnershipDigest, plan.Request.BoundaryAuthorityDigest,
                plan.Request.FixedCanvasAuthorityDigest,
            };

            var result = SectorFinalRouteRecoveryValidator.Validate(request);

            AssertPass(result);
            Assert.That(result.Report.SourceCanvasPlan, Is.SameAs(plan));
            Assert.That(result.Report.SourceProtectionDensityReport, Is.SameAs(density));
            Assert.That(plan.Cells.Select(value => value.StableToken), Is.EqualTo(cellTokens));
            Assert.That(request.Anchors.Select(value => value.StableToken), Is.EqualTo(anchorTokens));
            Assert.That(new[]
            {
                plan.InputDigest, plan.OutputDigest, density.InputDigest, density.OutputDigest,
                plan.Request.Map15ExitDigest, plan.Request.WorldAssemblyDigest,
                plan.Request.SectorOwnershipDigest, plan.Request.BoundaryAuthorityDigest,
                plan.Request.FixedCanvasAuthorityDigest,
            }, Is.EqualTo(identities));
            Assert.That(new[]
            {
                result.Report.GeneratedFileWriteCount, result.Report.TilemapMutationCount,
                result.Report.SceneMutationCount, result.Report.PrefabMutationCount,
                result.Report.GameObjectMutationCount, result.Report.GameplaySpawnCount,
                result.Report.SliceCreationCount, result.Report.ProductionSeedApprovalCount,
            }.All(value => value == 0), Is.True);
        }

        [Test]
        public void RouteRecoveryDoesNotUsePlayerPhysicsPlayModeOrTilemapBake()
        {
            var result = SectorFinalRouteRecoveryValidator.Validate(fixture.AcceptedRequest());

            AssertPass(result);
            Assert.That(result.Report.PlayerPhysicsSimulationCount, Is.Zero);
            Assert.That(result.Report.PlayModeRunCount, Is.Zero);
            Assert.That(result.Report.TilemapBakeCount, Is.Zero);
            Assert.That(result.Report.SliceCreationCount, Is.Zero);
            Assert.That(result.Report.GeneratedFileWriteCount, Is.Zero);
            Assert.That(result.Report.FullRegressionCount, Is.Zero);
            Assert.That(result.Report.Witnesses.Select(value => value.StableToken)
                .All(value => !value.Contains("/") && !value.Contains("\\") &&
                              !value.Contains("\n")), Is.True);
        }

        [Test]
        public void Map16HandoffKeepsMap16_04Locked()
        {
            var result = SectorFinalRouteRecoveryValidator.Validate(fixture.AcceptedRequest());

            AssertPass(result);
            Assert.That(SectorFinalRouteRecoveryReport.DownstreamOwner,
                Is.EqualTo("MAP16_04_IMPLEMENT_PATTERN_CHUNK_COORDINATES_AND_PARTITION"));
            Assert.That(SectorFinalRouteRecoveryReport.OpensDownstreamTask, Is.False);
            Assert.That(result.Request.PublicationLabel,
                Is.EqualTo(SectorFinalRouteRecoveryValidator.ReferencePublicationLabel));
            Assert.That(result.Report.ProductionSeedApprovalCount, Is.Zero);
            Assert.That(result.Report.FullRegressionCount, Is.Zero);
        }

        private static bool IsBlocked(
            SectorFinalCanvasLayerPlan plan,
            FinalCanvasCellCoordinate coordinate)
        {
            var cell = plan.Cells.Single(value => value.Coordinate.Equals(coordinate));
            var terrain = cell.Winner(FinalCanvasLayerKind.Terrain);
            var hazard = cell.Winner(FinalCanvasLayerKind.Hazard);
            var protection = cell.Winner(FinalCanvasLayerKind.Protection);
            return terrain.CellKind == FinalCanvasCellKind.Solid ||
                   hazard.CellKind == FinalCanvasCellKind.Hazard ||
                   protection.CellKind != FinalCanvasCellKind.None &&
                   protection.CellKind != FinalCanvasCellKind.ProtectedOpen;
        }

        private static void AssertPass(FinalRouteRecoveryResult result)
        {
            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(result.Report, Is.Not.Null);
            Assert.That(result.Failures, Is.Empty);
        }

        private static string Join(FinalRouteRecoveryResult result) => result == null
            ? "NULL"
            : string.Join(";", result.Failures.Select(value => value.ToString()));
    }

    internal enum ReferenceFinalRouteVariant
    {
        Accepted = 1,
        MissingCanvas = 2,
        MissingReport = 3,
        MissingEntry = 4,
        BlockedExit = 5,
        IsolatedFailure = 6,
        SourceMismatch = 7,
    }

    internal sealed class ReferenceFinalRouteRecoveryFixture
    {
        public FinalRouteRecoveryRequest AcceptedRequest(
            bool reverseClaims = false,
            bool reverseEvidence = false) => BuildRequest(
                ReferenceFinalRouteVariant.Accepted, reverseClaims, reverseEvidence);

        public FinalRouteRecoveryRequest Request(ReferenceFinalRouteVariant variant) =>
            BuildRequest(variant, false, false);

        private static FinalRouteRecoveryRequest BuildRequest(
            ReferenceFinalRouteVariant variant,
            bool reverseClaims,
            bool reverseEvidence)
        {
            if (variant == ReferenceFinalRouteVariant.MissingCanvas)
                return new FinalRouteRecoveryRequest(
                    null, null, BuildAnchors(variant), Array.Empty<FinalRouteEdge>(),
                    SectorFinalRouteRecoveryValidator.ReferencePublicationLabel);

            var planFixture = new ReferenceProtectionDensityFixture();
            var plan = planFixture.AcceptedPlan(reverseClaims);
            var densityResult = SectorCanvasProtectionDensityValidator.Validate(plan);
            if (!densityResult.Success)
                throw new InvalidOperationException(string.Join(";",
                    densityResult.Failures.Select(value => value.ToString())));
            var density = variant == ReferenceFinalRouteVariant.MissingReport
                ? null : densityResult.Report;

            if (variant == ReferenceFinalRouteVariant.SourceMismatch)
            {
                var otherPlan = planFixture.AcceptedPlan();
                var otherDensity = SectorCanvasProtectionDensityValidator.Validate(otherPlan);
                density = otherDensity.Report;
            }

            var anchors = BuildAnchors(variant);
            var sourceAnchors = reverseEvidence ? anchors.Reverse() : anchors;
            return new FinalRouteRecoveryRequest(
                plan, density, sourceAnchors, Array.Empty<FinalRouteEdge>(),
                SectorFinalRouteRecoveryValidator.ReferencePublicationLabel);
        }

        private static FinalRouteAnchor[] BuildAnchors(ReferenceFinalRouteVariant variant)
        {
            var anchors = new List<FinalRouteAnchor>
            {
                Anchor("MAP14_BASE_ENTRY", FinalRouteNodeKind.BaseEntry, 0, 16,
                    FinalCanvasSourceOwner.MandatoryRoute,
                    FinalCanvasProtectionKind.MandatoryRouteProtectedOpen),
                Anchor("MAP14_BASE_EXIT", FinalRouteNodeKind.BaseExit,
                    variant == ReferenceFinalRouteVariant.BlockedExit ? 30 : 47, 16,
                    variant == ReferenceFinalRouteVariant.BlockedExit
                        ? FinalCanvasSourceOwner.QuietFiller
                        : FinalCanvasSourceOwner.Boundary,
                    variant == ReferenceFinalRouteVariant.BlockedExit
                        ? FinalCanvasProtectionKind.None
                        : FinalCanvasProtectionKind.BoundaryAperture),
                Anchor("MAP15_02_EXTERNAL_SOCKET_LEFT", FinalRouteNodeKind.ExternalSocket,
                    0, 16, FinalCanvasSourceOwner.MandatoryRoute,
                    FinalCanvasProtectionKind.MandatoryRouteProtectedOpen),
                Anchor("MAP15_02_EXTERNAL_SOCKET_RIGHT", FinalRouteNodeKind.ExternalSocket,
                    47, 16, FinalCanvasSourceOwner.Boundary,
                    FinalCanvasProtectionKind.BoundaryAperture),
                Anchor("MAP08_BOUNDARY_APERTURE_RIGHT", FinalRouteNodeKind.BoundaryAperture,
                    47, 16, FinalCanvasSourceOwner.Boundary,
                    FinalCanvasProtectionKind.BoundaryAperture),
                Anchor("MAP13_SPECIAL_ENTRANCE", FinalRouteNodeKind.SpecialEntrance,
                    10, 16, FinalCanvasSourceOwner.SpecialRegion,
                    FinalCanvasProtectionKind.SpecialEntranceBuffer),
                Anchor("MAP14_REFERENCE_HIGH_BRANCH", FinalRouteNodeKind.HighRouteBranch,
                    40, 24, FinalCanvasSourceOwner.QuietFiller,
                    FinalCanvasProtectionKind.None),
                Anchor("MAP14_REFERENCE_HIGH_FAILURE", FinalRouteNodeKind.FailureSample,
                    variant == ReferenceFinalRouteVariant.IsolatedFailure ? 10 : 40,
                    variant == ReferenceFinalRouteVariant.IsolatedFailure ? 5 : 23,
                    FinalCanvasSourceOwner.QuietFiller,
                    FinalCanvasProtectionKind.None),
            };
            if (variant == ReferenceFinalRouteVariant.MissingEntry)
                anchors.RemoveAll(value => value.Kind == FinalRouteNodeKind.BaseEntry);
            return anchors.ToArray();
        }

        private static FinalRouteAnchor Anchor(
            string stableId,
            FinalRouteNodeKind kind,
            int x,
            int y,
            FinalCanvasSourceOwner sourceOwner,
            FinalCanvasProtectionKind protection) => new FinalRouteAnchor(
                stableId, kind, new FinalCanvasCellCoordinate(x, y),
                sourceOwner, protection, true);
    }
}
