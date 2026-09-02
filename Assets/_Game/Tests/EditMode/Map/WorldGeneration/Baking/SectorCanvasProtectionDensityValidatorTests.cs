using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Baking
{
    [TestFixture]
    [Category("MAP16_02")]
    public sealed class SectorCanvasProtectionDensityValidatorTests
    {
        private ReferenceProtectionDensityFixture fixture;

        [SetUp]
        public void SetUp()
        {
            fixture = new ReferenceProtectionDensityFixture();
        }

        [Test]
        public void ProtectionDensityReportPublishesBudgetsCleanupAndDigests()
        {
            var result = SectorCanvasProtectionDensityValidator.Validate(fixture.AcceptedPlan());

            AssertPass(result);
            Assert.That(result.Report.ObservedCellCount, Is.EqualTo(1536));
            Assert.That(result.Report.UniqueCoordinateCount, Is.EqualTo(1536));
            Assert.That(result.Report.OutOfBoundsCellCount, Is.Zero);
            Assert.That(result.Report.RequiredLayerKindCount, Is.EqualTo(7));
            Assert.That(result.Report.CoveredLayerKindCount, Is.EqualTo(7));
            Assert.That(result.Report.MissingLayerKindCount, Is.Zero);
            Assert.That(result.Report.ProtectedCellCount, Is.EqualTo(4));
            Assert.That(result.Report.ProtectedOpenCellCount, Is.EqualTo(1));
            Assert.That(result.Report.FixedCellCount, Is.EqualTo(1));
            Assert.That(result.Report.BoundaryApertureCellCount, Is.EqualTo(1));
            Assert.That(result.Report.SpecialEntranceCellCount, Is.EqualTo(1));
            Assert.That(result.Report.RequiredCleanupCandidateKindCount, Is.EqualTo(6));
            Assert.That(result.Report.CoveredCleanupCandidateKindCount, Is.EqualTo(6));
            Assert.That(result.Report.MissingCleanupCandidateKindCount, Is.Zero);
            Assert.That(result.Report.Budgets.Count, Is.EqualTo(5));
            Assert.That(result.InputDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(result.OutputDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(ProtectionDensityDigest.ComputeOutput(result.Report),
                Is.EqualTo(result.OutputDigest));
            Assert.That(() => ((IList<SectorCanvasCleanupCandidate>)result.Report.CleanupCandidates)
                .Add(result.Report.CleanupCandidates[0]), Throws.TypeOf<NotSupportedException>());

            TestContext.Out.WriteLine("MAP16_02_INPUT_DIGEST=" + result.InputDigest);
            TestContext.Out.WriteLine("MAP16_02_OUTPUT_DIGEST=" + result.OutputDigest);
            TestContext.Out.WriteLine(string.Join(";", new[]
            {
                "MAP16_02_COUNTS=CELLS" + result.Report.ObservedCellCount,
                "LAYERS" + result.Report.CoveredLayerKindCount,
                "PROTECTED" + result.Report.ProtectedCellCount,
                "FIXED" + result.Report.FixedCellCount,
                "BOUNDARY" + result.Report.BoundaryApertureCellCount,
                "SPECIAL" + result.Report.SpecialEntranceCellCount,
                "INTRUSIONS" + result.Report.ProtectionIntrusionCount,
                "CLEANUP" + result.Report.CleanupCandidateCount,
                "SINGLE_SOLID" + result.Report.CountCandidates(CleanupCandidateKind.SingleCellSolidNoise),
                "SINGLE_AIR" + result.Report.CountCandidates(CleanupCandidateKind.SingleCellAirNoise),
                "HEAD_SNAG" + result.Report.CountCandidates(CleanupCandidateKind.HeadSnag),
                "SHALLOW_PIT" + result.Report.CountCandidates(CleanupCandidateKind.ShallowPit),
                "ONE_CELL_LIP" + result.Report.CountCandidates(CleanupCandidateKind.OneCellLip),
                "UNOWNED_POCKET" + result.Report.CountCandidates(CleanupCandidateKind.UnownedAirPocket),
                "PROJECTION" + result.Report.CleanupProjection.ChangedCellCount,
                "SOLID_PERMILLE" + result.Report.SolidPermille,
                "REACHABLE_PERMILLE" + result.Report.ReachablePermille,
                "UNOWNED" + result.Report.LargestUnownedAirWidth + "x" +
                    result.Report.LargestUnownedAirHeight + "x" + result.Report.LargestUnownedAirArea,
            }));
        }

        [Test]
        public void ProtectedOpenBoundaryFixedAndSpecialCellsHaveZeroIntrusions()
        {
            var result = SectorCanvasProtectionDensityValidator.Validate(fixture.AcceptedPlan());

            AssertPass(result);
            Assert.That(result.Report.ProtectionIntrusionCount, Is.Zero);
            foreach (var kind in Enum.GetValues(typeof(ProtectionIntrusionKind))
                         .Cast<ProtectionIntrusionKind>())
                Assert.That(result.Report.CountIntrusions(kind), Is.Zero, kind.ToString());
            Assert.That(result.Report.Budgets.Single(value =>
                value.Kind == DensityBudgetKind.ProtectionIntrusion).Verdict,
                Is.EqualTo(DensityBudgetVerdict.Pass));
        }

        [Test]
        public void CleanupClassifierDetectsSingleCellNoiseHeadSnagAndPitCandidates()
        {
            var result = SectorCanvasProtectionDensityValidator.Validate(fixture.AcceptedPlan());

            AssertPass(result);
            foreach (var kind in Enum.GetValues(typeof(CleanupCandidateKind))
                         .Cast<CleanupCandidateKind>())
                Assert.That(result.Report.CountCandidates(kind), Is.GreaterThan(0), kind.ToString());
            Assert.That(result.Report.CleanupCandidateCount, Is.GreaterThanOrEqualTo(6));
        }

        [Test]
        public void CleanupProjectionNeverChangesProtectedFixedBoundaryOrSpecialCells()
        {
            var plan = fixture.AcceptedPlan();
            var sourceTokens = plan.Cells.Select(value => value.StableToken).ToArray();
            var result = SectorCanvasProtectionDensityValidator.Validate(plan);

            AssertPass(result);
            Assert.That(result.Report.CleanupProjection.ChangedCellCount, Is.GreaterThan(0));
            Assert.That(result.Report.CleanupProjection.ProtectedOpenChangedCount, Is.Zero);
            Assert.That(result.Report.CleanupProjection.FixedChangedCount, Is.Zero);
            Assert.That(result.Report.CleanupProjection.BoundaryChangedCount, Is.Zero);
            Assert.That(result.Report.CleanupProjection.SpecialEntranceChangedCount, Is.Zero);
            Assert.That(result.Report.CleanupProjection.ProtectedAuthorityChangedCount, Is.Zero);
            Assert.That(result.Report.CleanupProjection.IsSafe, Is.True);
            Assert.That(plan.Cells.Select(value => value.StableToken), Is.EqualTo(sourceTokens));
        }

        [Test]
        public void SolidAndReachableDensityBudgetsStayWithinApprovedEnvelope()
        {
            var result = SectorCanvasProtectionDensityValidator.Validate(fixture.AcceptedPlan());

            AssertPass(result);
            Assert.That(result.Report.SolidCellCount, Is.EqualTo(768));
            Assert.That(result.Report.SolidPermille, Is.EqualTo(500));
            Assert.That(result.Report.ReachableCellCount, Is.EqualTo(715));
            Assert.That(result.Report.ReachablePermille, Is.EqualTo(465));
            Assert.That(result.Report.SolidPermille,
                Is.InRange(SectorCanvasProtectionDensityReport.SolidMinimumPermille,
                    SectorCanvasProtectionDensityReport.SolidMaximumPermille));
            Assert.That(result.Report.ReachablePermille,
                Is.InRange(SectorCanvasProtectionDensityReport.ReachableMinimumPermille,
                    SectorCanvasProtectionDensityReport.ReachableMaximumPermille));
            Assert.That(result.Report.DensityBudgetViolationCount, Is.Zero);
            Assert.That(result.Report.Budgets.All(value =>
                value.Verdict == DensityBudgetVerdict.Pass), Is.True);
        }

        [Test]
        public void UnownedAirRegionDoesNotExceedEightBySixLimit()
        {
            var result = SectorCanvasProtectionDensityValidator.Validate(fixture.AcceptedPlan());

            AssertPass(result);
            Assert.That(result.Report.LargestUnownedAirWidth, Is.EqualTo(8));
            Assert.That(result.Report.LargestUnownedAirHeight, Is.EqualTo(6));
            Assert.That(result.Report.LargestUnownedAirArea, Is.EqualTo(48));
            Assert.That(result.Report.UnownedAirViolationCount, Is.Zero);
            Assert.That(result.Report.UnownedAirRegions.All(value =>
                value.Width <= 8 && value.Height <= 6 && value.Area <= 48), Is.True);
        }

        [Test]
        public void ProtectionDensityDigestIsDeterministicAcrossRepeatReverseAndCulture()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var first = SectorCanvasProtectionDensityValidator.Validate(fixture.AcceptedPlan());
                var repeat = SectorCanvasProtectionDensityValidator.Validate(fixture.AcceptedPlan());
                var reverse = SectorCanvasProtectionDensityValidator.Validate(
                    fixture.AcceptedPlan(reverseClaims: true));
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var culture = SectorCanvasProtectionDensityValidator.Validate(fixture.AcceptedPlan());
                var results = new[] { first, repeat, reverse, culture };

                Assert.That(results.All(value => value.Success), Is.True,
                    string.Join(";", results.Select(Join)));
                Assert.That(results.Select(value => value.SourcePlan.InputDigest).Distinct().Count(),
                    Is.EqualTo(1));
                Assert.That(results.Select(value => value.SourcePlan.OutputDigest).Distinct().Count(),
                    Is.EqualTo(1));
                Assert.That(results.Select(value => value.InputDigest).Distinct().Count(), Is.EqualTo(1));
                Assert.That(results.Select(value => value.OutputDigest).Distinct().Count(), Is.EqualTo(1));
                Assert.That(results.Select(value => string.Join("\n",
                    value.Report.CleanupCandidates.Select(candidate => candidate.StableToken)))
                    .Distinct().Count(), Is.EqualTo(1));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Test]
        public void InvalidProtectionDensityInputsFailAtomicallyWithoutPartialReport()
        {
            var results = new[]
            {
                SectorCanvasProtectionDensityValidator.Validate(null),
                SectorCanvasProtectionDensityValidator.Validate(
                    fixture.Plan(ReferenceCanvasVariant.ProtectedSolidIntrusion)),
                SectorCanvasProtectionDensityValidator.Validate(
                    fixture.Plan(ReferenceCanvasVariant.ProtectedHazardIntrusion)),
                SectorCanvasProtectionDensityValidator.Validate(
                    fixture.Plan(ReferenceCanvasVariant.BoundaryBlocked)),
                SectorCanvasProtectionDensityValidator.Validate(
                    fixture.Plan(ReferenceCanvasVariant.FixedOverwritten)),
                SectorCanvasProtectionDensityValidator.Validate(
                    fixture.Plan(ReferenceCanvasVariant.SpecialBlocked)),
                SectorCanvasProtectionDensityValidator.Validate(
                    fixture.Plan(ReferenceCanvasVariant.ProtectionMissing)),
                SectorCanvasProtectionDensityValidator.Validate(
                    fixture.Plan(ReferenceCanvasVariant.AllSolid)),
                SectorCanvasProtectionDensityValidator.Validate(
                    fixture.Plan(ReferenceCanvasVariant.OversizedUnownedAir)),
            };

            Assert.That(results.All(value => !value.Success), Is.True);
            Assert.That(results.All(value => value.Report == null), Is.True);
            Assert.That(results.All(value => value.InputDigest == string.Empty), Is.True);
            Assert.That(results.All(value => value.OutputDigest == string.Empty), Is.True);
            Assert.That(results.All(value => value.Failures.Count > 0), Is.True);
            var kinds = results.SelectMany(value => value.Intrusions)
                .Select(value => value.Kind).Distinct().ToArray();
            foreach (var kind in Enum.GetValues(typeof(ProtectionIntrusionKind))
                         .Cast<ProtectionIntrusionKind>())
                Assert.That(kinds, Does.Contain(kind), kind.ToString());
            Assert.That(results.SelectMany(value => value.Failures).Select(value => value.Code),
                Does.Contain(ProtectionDensityFailureCode.DensityOutOfRange));
            Assert.That(results.SelectMany(value => value.Failures).Select(value => value.Code),
                Does.Contain(ProtectionDensityFailureCode.UnownedAirTooLarge));
        }

        [Test]
        public void ValidatorDoesNotMutateCanvasWorldAssemblyFilesTilesScenesOrGameplayObjects()
        {
            var plan = fixture.AcceptedPlan();
            var sourceTokens = plan.Cells.Select(value => value.StableToken).ToArray();
            var sourceIdentities = new[]
            {
                plan.InputDigest, plan.OutputDigest, plan.Request.Map15ExitDigest,
                plan.Request.WorldAssemblyDigest, plan.Request.SectorOwnershipDigest,
                plan.Request.BoundaryAuthorityDigest, plan.Request.FixedCanvasAuthorityDigest,
            };
            var result = SectorCanvasProtectionDensityValidator.Validate(plan);

            AssertPass(result);
            Assert.That(result.Report.SourcePlan, Is.SameAs(plan));
            Assert.That(plan.Cells.Select(value => value.StableToken), Is.EqualTo(sourceTokens));
            Assert.That(new[]
            {
                plan.InputDigest, plan.OutputDigest, plan.Request.Map15ExitDigest,
                plan.Request.WorldAssemblyDigest, plan.Request.SectorOwnershipDigest,
                plan.Request.BoundaryAuthorityDigest, plan.Request.FixedCanvasAuthorityDigest,
            }, Is.EqualTo(sourceIdentities));
            Assert.That(new[]
            {
                result.Report.NewRngDrawCount, result.Report.SliceCreationCount,
                result.Report.GeneratedFileWriteCount, result.Report.TilemapMutationCount,
                result.Report.SceneMutationCount, result.Report.PrefabMutationCount,
                result.Report.GameObjectMutationCount, result.Report.GameplaySpawnCount,
                result.Report.ProductionSeedApprovalCount, result.Report.SectorRerollCount,
                result.Report.FallbackCarveCount, result.Report.FullRegressionCount,
            }.All(value => value == 0), Is.True);
            Assert.That(result.Report.CleanupCandidates.Select(value => value.StableToken)
                .All(value => !value.Contains("/") && !value.Contains("\\") &&
                              !value.Contains("\n")), Is.True);
        }

        [Test]
        public void Map16HandoffKeepsMap16_03Locked()
        {
            var result = SectorCanvasProtectionDensityValidator.Validate(fixture.AcceptedPlan());

            AssertPass(result);
            Assert.That(SectorCanvasProtectionDensityReport.DownstreamOwner,
                Is.EqualTo("MAP16_03_VALIDATE_FINAL_ROUTE_AND_RECOVERY"));
            Assert.That(SectorCanvasProtectionDensityReport.OpensDownstreamTask, Is.False);
            Assert.That(result.SourcePlan.Request.SectorId,
                Is.EqualTo(SectorCanvasProtectionDensityValidator.ReferencePublicationLabel));
            Assert.That(result.Report.ProductionSeedApprovalCount, Is.Zero);
            Assert.That(result.Report.FullRegressionCount, Is.Zero);
        }

        private static void AssertPass(ProtectionDensityResult result)
        {
            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(result.Report, Is.Not.Null);
            Assert.That(result.Failures, Is.Empty);
        }

        private static string Join(ProtectionDensityResult result) => result == null
            ? "NULL"
            : string.Join(";", result.Failures.Select(value => value.ToString()));
    }

    internal enum ReferenceCanvasVariant
    {
        Accepted = 1,
        ProtectedSolidIntrusion = 2,
        ProtectedHazardIntrusion = 3,
        BoundaryBlocked = 4,
        FixedOverwritten = 5,
        SpecialBlocked = 6,
        ProtectionMissing = 7,
        AllSolid = 8,
        OversizedUnownedAir = 9,
    }

    internal sealed class ReferenceProtectionDensityFixture
    {
        private static readonly FinalCanvasCellCoordinate ProtectedOpen =
            new FinalCanvasCellCoordinate(0, 16);
        private static readonly FinalCanvasCellCoordinate Boundary =
            new FinalCanvasCellCoordinate(47, 16);
        private static readonly FinalCanvasCellCoordinate Fixed =
            new FinalCanvasCellCoordinate(5, 5);
        private static readonly FinalCanvasCellCoordinate SpecialEntrance =
            new FinalCanvasCellCoordinate(10, 16);

        public SectorFinalCanvasLayerPlan AcceptedPlan(bool reverseClaims = false) =>
            BuildPlan(ReferenceCanvasVariant.Accepted, reverseClaims);

        public SectorFinalCanvasLayerPlan Plan(ReferenceCanvasVariant variant) =>
            BuildPlan(variant, false);

        private static SectorFinalCanvasLayerPlan BuildPlan(
            ReferenceCanvasVariant variant,
            bool reverseClaims)
        {
            var claims = BuildClaims(variant);
            var source = reverseClaims ? claims.Reverse() : claims;
            var request = new FinalCanvasLayerRequest(
                SectorCanvasProtectionDensityValidator.ReferencePublicationLabel,
                SectorFinalCanvasLayerPlan.SectorWidth,
                SectorFinalCanvasLayerPlan.SectorHeight,
                source,
                true,
                Hash("MAP15_07_WORLD_ASSEMBLY_EXIT_APPROVED"),
                Hash("MAP15_06_WORLD_ASSEMBLY_OVERLAY_IDENTITY"),
                Hash("MAP14_SECTOR_OWNERSHIP_PROTECTED_ROUTE_IDENTITY"),
                Hash("MAP08_BOUNDARY_APERTURE_AUTHORITY"),
                Hash("MAP07_FIXED_CANVAS_AUTHORITY"),
                SectorCanvasLayerFinalizer.ReferencePublicationLabel);
            var result = SectorCanvasLayerFinalizer.Finalize(request);
            if (!result.Success)
                throw new InvalidOperationException(string.Join(";",
                    result.Failures.Select(value => value.ToString())));
            return result.Plan;
        }

        private static FinalCanvasLayerClaim[] BuildClaims(ReferenceCanvasVariant variant)
        {
            var claims = new List<FinalCanvasLayerClaim>(
                SectorFinalCanvasLayerPlan.CellCount * SectorFinalCanvasLayerPlan.RequiredLayerCount);
            for (var y = 0; y < SectorFinalCanvasLayerPlan.SectorHeight; y++)
            {
                for (var x = 0; x < SectorFinalCanvasLayerPlan.SectorWidth; x++)
                {
                    foreach (var layer in Enum.GetValues(typeof(FinalCanvasLayerKind))
                                 .Cast<FinalCanvasLayerKind>())
                        claims.Add(BuildClaim(variant, x, y, layer));
                }
            }
            return claims.ToArray();
        }

        private static FinalCanvasLayerClaim BuildClaim(
            ReferenceCanvasVariant variant,
            int x,
            int y,
            FinalCanvasLayerKind layer)
        {
            var coordinate = new FinalCanvasCellCoordinate(x, y);
            var terrainSolid = IsSolid(variant, x, y);
            var unowned = variant == ReferenceCanvasVariant.Accepted &&
                          x >= 2 && x <= 9 && y >= 20 && y <= 25;
            var traversable = !terrainSolid && y >= 16 &&
                              variant != ReferenceCanvasVariant.OversizedUnownedAir && !unowned;
            var cellKind = DefaultCellKind(layer, terrainSolid, traversable);
            var owner = FinalCanvasSourceOwner.QuietFiller;
            var priority = FinalCanvasClaimPriority.QuietFiller;
            var protection = FinalCanvasProtectionKind.None;
            var isProtected = false;

            if (variant == ReferenceCanvasVariant.Accepted)
            {
                if (coordinate.Equals(ProtectedOpen))
                    ApplyAuthority(layer, FinalCanvasSourceOwner.MandatoryRoute,
                        FinalCanvasClaimPriority.MandatoryRouteProtectedOpen,
                        FinalCanvasProtectionKind.MandatoryRouteProtectedOpen,
                        ref cellKind, ref owner, ref priority, ref protection, ref isProtected);
                else if (coordinate.Equals(Boundary))
                    ApplyAuthority(layer, FinalCanvasSourceOwner.Boundary,
                        FinalCanvasClaimPriority.BoundaryAperture,
                        FinalCanvasProtectionKind.BoundaryAperture,
                        ref cellKind, ref owner, ref priority, ref protection, ref isProtected);
                else if (coordinate.Equals(Fixed))
                    ApplyAuthority(layer, FinalCanvasSourceOwner.FixedSlice,
                        FinalCanvasClaimPriority.FixedSlice,
                        FinalCanvasProtectionKind.FixedSlice,
                        ref cellKind, ref owner, ref priority, ref protection, ref isProtected);
                else if (coordinate.Equals(SpecialEntrance))
                    ApplyAuthority(layer, FinalCanvasSourceOwner.SpecialRegion,
                        FinalCanvasClaimPriority.SpecialEntranceBuffer,
                        FinalCanvasProtectionKind.SpecialEntranceBuffer,
                        ref cellKind, ref owner, ref priority, ref protection, ref isProtected);
            }
            else
            {
                ApplyInvalidVariant(
                    variant, x, y, layer,
                    ref cellKind, ref owner, ref priority, ref protection, ref isProtected);
            }

            var id = string.Join("_", new[]
            {
                "MAP16_02", variant.ToString().ToUpperInvariant(),
                y.ToString("D2", CultureInfo.InvariantCulture),
                x.ToString("D2", CultureInfo.InvariantCulture),
                layer.ToString().ToUpperInvariant(),
            });
            return new FinalCanvasLayerClaim(
                id, coordinate, layer, cellKind, owner, priority, protection,
                isProtected, "MAP16_02_REFERENCE_PROVENANCE_" + id,
                "REFERENCE_PROTECTION_CLEANUP_DENSITY_REPORT");
        }

        private static void ApplyAuthority(
            FinalCanvasLayerKind layer,
            FinalCanvasSourceOwner authorityOwner,
            FinalCanvasClaimPriority authorityPriority,
            FinalCanvasProtectionKind authorityProtection,
            ref FinalCanvasCellKind cellKind,
            ref FinalCanvasSourceOwner owner,
            ref FinalCanvasClaimPriority priority,
            ref FinalCanvasProtectionKind protection,
            ref bool isProtected)
        {
            owner = authorityOwner;
            priority = authorityPriority;
            protection = authorityProtection;
            isProtected = true;
            if (layer == FinalCanvasLayerKind.Protection)
                cellKind = FinalCanvasCellKind.ProtectedOpen;
            else if (layer == FinalCanvasLayerKind.SourceOwner)
                cellKind = FinalCanvasCellKind.Owner;
            else if (layer == FinalCanvasLayerKind.Affordance &&
                     authorityProtection != FinalCanvasProtectionKind.FixedSlice)
                cellKind = FinalCanvasCellKind.Traversable;
        }

        private static void ApplyInvalidVariant(
            ReferenceCanvasVariant variant,
            int x,
            int y,
            FinalCanvasLayerKind layer,
            ref FinalCanvasCellKind cellKind,
            ref FinalCanvasSourceOwner owner,
            ref FinalCanvasClaimPriority priority,
            ref FinalCanvasProtectionKind protection,
            ref bool isProtected)
        {
            if (variant == ReferenceCanvasVariant.ProtectedSolidIntrusion &&
                x == 1 && y == 16)
            {
                if (layer == FinalCanvasLayerKind.Terrain) cellKind = FinalCanvasCellKind.Solid;
                ApplySpoofedAuthority(layer, FinalCanvasSourceOwner.MandatoryRoute,
                    FinalCanvasClaimPriority.MandatoryRouteProtectedOpen,
                    ref cellKind, ref owner, ref priority);
            }
            else if (variant == ReferenceCanvasVariant.ProtectedHazardIntrusion &&
                     x == 2 && y == 16)
            {
                if (layer == FinalCanvasLayerKind.Hazard) cellKind = FinalCanvasCellKind.Hazard;
                ApplySpoofedAuthority(layer, FinalCanvasSourceOwner.MandatoryRoute,
                    FinalCanvasClaimPriority.MandatoryRouteProtectedOpen,
                    ref cellKind, ref owner, ref priority);
            }
            else if (variant == ReferenceCanvasVariant.BoundaryBlocked &&
                     x == 3 && y == 16)
            {
                if (layer == FinalCanvasLayerKind.Terrain) cellKind = FinalCanvasCellKind.Solid;
                if (layer == FinalCanvasLayerKind.SourceOwner)
                {
                    owner = FinalCanvasSourceOwner.Boundary;
                    priority = FinalCanvasClaimPriority.BoundaryAperture;
                }
            }
            else if (variant == ReferenceCanvasVariant.FixedOverwritten &&
                     x == 4 && y == 5 && layer == FinalCanvasLayerKind.SourceOwner)
            {
                owner = FinalCanvasSourceOwner.FixedSlice;
                priority = FinalCanvasClaimPriority.FixedSlice;
            }
            else if (variant == ReferenceCanvasVariant.SpecialBlocked &&
                     x == 5 && y == 16)
            {
                if (layer == FinalCanvasLayerKind.Terrain) cellKind = FinalCanvasCellKind.Solid;
                if (layer == FinalCanvasLayerKind.SourceOwner)
                {
                    owner = FinalCanvasSourceOwner.SpecialRegion;
                    priority = FinalCanvasClaimPriority.SpecialEntranceBuffer;
                }
            }
            else if (variant == ReferenceCanvasVariant.ProtectionMissing &&
                     x == 6 && y == 16 && layer == FinalCanvasLayerKind.Terrain)
            {
                owner = FinalCanvasSourceOwner.MandatoryRoute;
                priority = FinalCanvasClaimPriority.MandatoryRouteProtectedOpen;
            }
        }

        private static void ApplySpoofedAuthority(
            FinalCanvasLayerKind layer,
            FinalCanvasSourceOwner authorityOwner,
            FinalCanvasClaimPriority authorityPriority,
            ref FinalCanvasCellKind cellKind,
            ref FinalCanvasSourceOwner owner,
            ref FinalCanvasClaimPriority priority)
        {
            if (layer != FinalCanvasLayerKind.Protection) return;
            cellKind = FinalCanvasCellKind.ProtectedOpen;
            owner = authorityOwner;
            priority = authorityPriority;
        }

        private static FinalCanvasCellKind DefaultCellKind(
            FinalCanvasLayerKind layer,
            bool terrainSolid,
            bool traversable)
        {
            switch (layer)
            {
                case FinalCanvasLayerKind.Terrain:
                    return terrainSolid ? FinalCanvasCellKind.Solid : FinalCanvasCellKind.Air;
                case FinalCanvasLayerKind.Affordance:
                    return traversable ? FinalCanvasCellKind.Traversable : FinalCanvasCellKind.None;
                case FinalCanvasLayerKind.Material:
                    return terrainSolid ? FinalCanvasCellKind.Material : FinalCanvasCellKind.None;
                case FinalCanvasLayerKind.SourceOwner:
                    return FinalCanvasCellKind.Owner;
                default:
                    return FinalCanvasCellKind.None;
            }
        }

        private static bool IsSolid(ReferenceCanvasVariant variant, int x, int y)
        {
            if (variant == ReferenceCanvasVariant.AllSolid) return true;
            var solid = y < 16;
            if (variant != ReferenceCanvasVariant.Accepted) return solid;

            if ((x == 20 && y == 20) ||
                (x == 25 && (y == 20 || y == 21)) ||
                ((x == 30 || x == 31) && y == 16))
                return true;
            if ((x == 20 && y == 8) || (x == 35 && y == 15) ||
                ((x == 10 || x == 12 || x == 14) && y == 5))
                return false;
            return solid;
        }

        private static string Hash(string token) =>
            FinalCanvasLayerDigest.HashCanonicalText(token);
    }
}
