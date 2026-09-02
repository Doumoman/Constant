using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Baking
{
    [TestFixture]
    [Category("MAP16_01")]
    public sealed class SectorCanvasLayerFinalizerTests
    {
        private ReferenceFinalCanvasFixture fixture;

        [SetUp]
        public void SetUp()
        {
            fixture = ReferenceFinalCanvasFixture.Create();
        }

        [Test]
        public void FinalCanvasPlanPublishesSevenLayersCellsSourceOwnersAndDigests()
        {
            var result = fixture.Finalize();

            AssertPass(result);
            Assert.That(result.Plan.RequiredLayerKindCount, Is.EqualTo(7));
            Assert.That(result.Plan.CoveredLayerKindCount, Is.EqualTo(7));
            Assert.That(result.Plan.MissingLayerKindCount, Is.Zero);
            Assert.That(result.Plan.LayerSummaries.All(value => value.WinnerCount == 1536), Is.True);
            Assert.That(result.Plan.WinningClaimCount, Is.EqualTo(1536 * 7));
            Assert.That(result.Plan.ProtectedCellCount, Is.EqualTo(4));
            Assert.That(result.Plan.FixedCellCount, Is.EqualTo(1));
            Assert.That(result.Plan.BoundaryApertureCellCount, Is.EqualTo(1));
            Assert.That(result.Plan.MarkerCount, Is.EqualTo(2));
            Assert.That(result.Plan.FixedPrecedenceWinCount, Is.EqualTo(2));
            Assert.That(result.Plan.BoundaryPrecedenceWinCount, Is.EqualTo(2));
            Assert.That(result.InputDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(result.OutputDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(() => ((IList<FinalCanvasCell>)result.Plan.Cells).Add(result.Plan.Cells[0]),
                Throws.TypeOf<NotSupportedException>());

            TestContext.Out.WriteLine("MAP16_01_INPUT_DIGEST=" + result.InputDigest);
            TestContext.Out.WriteLine("MAP16_01_OUTPUT_DIGEST=" + result.OutputDigest);
            TestContext.Out.WriteLine(
                "MAP16_01_COUNTS=CELLS1536;LAYERS7;WINNERS10752;PROTECTED4;FIXED1;BOUNDARY1;MARKERS2;FIXED_WINS2;BOUNDARY_WINS2;CONFLICTS0");
        }

        [Test]
        public void FinalCanvasContainsExactly1536UniqueInBoundsCellsForOneSector()
        {
            var result = fixture.Finalize();

            AssertPass(result);
            Assert.That(SectorFinalCanvasLayerPlan.SectorWidth, Is.EqualTo(48));
            Assert.That(SectorFinalCanvasLayerPlan.SectorHeight, Is.EqualTo(32));
            Assert.That(SectorFinalCanvasLayerPlan.CellCount, Is.EqualTo(1536));
            Assert.That(result.Plan.ObservedCellCount, Is.EqualTo(1536));
            Assert.That(result.Plan.UniqueCoordinateCount, Is.EqualTo(1536));
            Assert.That(result.Plan.OutOfBoundsCellCount, Is.Zero);
            Assert.That(result.Plan.Cells.Select(value => value.RowMajorIndex),
                Is.EqualTo(Enumerable.Range(0, 1536)));
        }

        [Test]
        public void FixedSliceBoundaryAndProtectedOpenPrecedenceBeatWeakerClaims()
        {
            var result = fixture.Finalize();
            var fixedCell = result.Plan.Cells.Single(value => value.Coordinate.Equals(
                ReferenceFinalCanvasFixture.FixedCoordinate));
            var boundaryCell = result.Plan.Cells.Single(value => value.Coordinate.Equals(
                ReferenceFinalCanvasFixture.BoundaryCoordinate));
            var routeCell = result.Plan.Cells.Single(value => value.Coordinate.Equals(
                ReferenceFinalCanvasFixture.RouteCoordinate));

            AssertPass(result);
            Assert.That(fixedCell.Winner(FinalCanvasLayerKind.Terrain).SourceOwner,
                Is.EqualTo(FinalCanvasSourceOwner.FixedSlice));
            Assert.That(boundaryCell.Winner(FinalCanvasLayerKind.Terrain).SourceOwner,
                Is.EqualTo(FinalCanvasSourceOwner.Boundary));
            Assert.That(routeCell.Winner(FinalCanvasLayerKind.Protection).Protection,
                Is.EqualTo(FinalCanvasProtectionKind.MandatoryRouteProtectedOpen));
            Assert.That(result.Plan.FixedPrecedenceWinCount, Is.GreaterThan(0));
            Assert.That(result.Plan.BoundaryPrecedenceWinCount, Is.GreaterThan(0));
            Assert.That(result.Plan.ConflictCount, Is.Zero);
            Assert.That(result.Plan.SilentOverwriteCount, Is.Zero);
        }

        [Test]
        public void SpecialEntranceAndMandatoryRouteCellsCannotBeBlockedBySolidOrHazard()
        {
            var routeClaims = fixture.Claims.Concat(new[]
            {
                fixture.Claim("INVALID_ROUTE_HAZARD", ReferenceFinalCanvasFixture.RouteCoordinate,
                    FinalCanvasLayerKind.Hazard, FinalCanvasCellKind.Hazard,
                    FinalCanvasSourceOwner.MicroPattern, FinalCanvasClaimPriority.TerrainClusterPattern),
            });
            var specialClaims = fixture.Claims.Concat(new[]
            {
                fixture.Claim("INVALID_SPECIAL_SOLID", ReferenceFinalCanvasFixture.SpecialEntranceCoordinate,
                    FinalCanvasLayerKind.Terrain, FinalCanvasCellKind.Solid,
                    FinalCanvasSourceOwner.TerrainCluster, FinalCanvasClaimPriority.TerrainClusterSpine),
            });
            var route = SectorCanvasLayerFinalizer.Finalize(fixture.Request(routeClaims));
            var special = SectorCanvasLayerFinalizer.Finalize(fixture.Request(specialClaims));

            AssertAtomicFailure(route, FinalCanvasConflictKind.MandatoryRouteProtectedOpenBlocked);
            AssertAtomicFailure(special, FinalCanvasConflictKind.SpecialEntranceBlocked);
            Assert.That(fixture.Finalize().Plan.ProtectedCellCount, Is.GreaterThanOrEqualTo(4));
        }

        [Test]
        public void LayerConflictsAreTypedDeterministicAndNeverSilentOverwrite()
        {
            var conflict = fixture.Claim(
                "SAME_PRIORITY_DIFFERENT_TERRAIN", new FinalCanvasCellCoordinate(6, 6),
                FinalCanvasLayerKind.Terrain, FinalCanvasCellKind.Solid,
                FinalCanvasSourceOwner.QuietFiller, FinalCanvasClaimPriority.QuietFiller);
            var claims = fixture.Claims.Concat(new[] { conflict }).ToArray();
            var forward = SectorCanvasLayerFinalizer.Finalize(fixture.Request(claims));
            var reverse = SectorCanvasLayerFinalizer.Finalize(fixture.Request(claims.Reverse()));

            AssertAtomicFailure(forward, FinalCanvasConflictKind.SamePriorityDifferentValue);
            AssertAtomicFailure(reverse, FinalCanvasConflictKind.SamePriorityDifferentValue);
            Assert.That(forward.Conflicts.Select(value => value.StableToken),
                Is.EqualTo(reverse.Conflicts.Select(value => value.StableToken)));
            Assert.That(forward.Conflicts.Count, Is.EqualTo(1));
        }

        [Test]
        public void SourceOwnerAndProvenanceArePublishedForEveryWinningLayerClaim()
        {
            var result = fixture.Finalize();

            AssertPass(result);
            Assert.That(result.Plan.WinningClaimsWithSourceOwnerCount,
                Is.EqualTo(result.Plan.WinningClaimCount));
            Assert.That(result.Plan.WinningClaimsWithProvenanceCount,
                Is.EqualTo(result.Plan.WinningClaimCount));
            Assert.That(result.Plan.Cells.SelectMany(value => value.Winners)
                .All(value => value.SourceOwner != FinalCanvasSourceOwner.Unknown &&
                              !string.IsNullOrEmpty(value.ProvenanceId)), Is.True);
            Assert.That(result.Plan.MarkerCount, Is.EqualTo(2));
            Assert.That(result.Plan.CountWinners(FinalCanvasSourceOwner.Activity), Is.EqualTo(1));
            Assert.That(result.Plan.CountWinners(FinalCanvasSourceOwner.EventOverlay), Is.EqualTo(1));
        }

        [Test]
        public void FinalCanvasDigestIsDeterministicAcrossRepeatReverseAndCulture()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var first = fixture.Finalize();
                var repeat = fixture.Finalize();
                var reverse = SectorCanvasLayerFinalizer.Finalize(fixture.Request(fixture.Claims.Reverse()));
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var culture = fixture.Finalize();
                var results = new[] { first, repeat, reverse, culture };

                Assert.That(results.All(value => value.Success), Is.True,
                    string.Join(";", results.Select(Join)));
                Assert.That(results.Select(value => value.InputDigest).Distinct().Count(), Is.EqualTo(1));
                Assert.That(results.Select(value => value.OutputDigest).Distinct().Count(), Is.EqualTo(1));
                Assert.That(results.Select(value => string.Join("\n",
                    value.Plan.Cells.Select(cell => cell.StableToken))).Distinct().Count(), Is.EqualTo(1));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Test]
        public void InvalidCanvasInputsFailAtomicallyWithoutPartialPlan()
        {
            var missingLayer = fixture.Claims.Where(value => !(value.Coordinate.X == 47 &&
                value.Coordinate.Y == 31 && value.Layer == FinalCanvasLayerKind.Marker)).ToArray();
            var outOfBounds = fixture.Claims.Concat(new[]
            {
                fixture.Claim("OUT_OF_BOUNDS", new FinalCanvasCellCoordinate(48, 31),
                    FinalCanvasLayerKind.Terrain, FinalCanvasCellKind.Ground,
                    FinalCanvasSourceOwner.QuietFiller, FinalCanvasClaimPriority.QuietFiller),
            });
            var fixedOverwrite = fixture.Claims.Concat(new[]
            {
                fixture.Claim("INVALID_FIXED_OVERWRITE", ReferenceFinalCanvasFixture.FixedCoordinate,
                    FinalCanvasLayerKind.Terrain, FinalCanvasCellKind.Solid,
                    FinalCanvasSourceOwner.MicroPattern, FinalCanvasClaimPriority.TerrainClusterPattern),
            });
            var boundaryOverwrite = fixture.Claims.Concat(new[]
            {
                fixture.Claim("INVALID_BOUNDARY_OVERWRITE", ReferenceFinalCanvasFixture.BoundaryCoordinate,
                    FinalCanvasLayerKind.Terrain, FinalCanvasCellKind.Solid,
                    FinalCanvasSourceOwner.TerrainCluster, FinalCanvasClaimPriority.TerrainClusterSpine),
            });
            var results = new[]
            {
                SectorCanvasLayerFinalizer.Finalize(null),
                SectorCanvasLayerFinalizer.Finalize(fixture.Request(width: 47)),
                SectorCanvasLayerFinalizer.Finalize(fixture.Request(missingLayer)),
                SectorCanvasLayerFinalizer.Finalize(fixture.Request(outOfBounds)),
                SectorCanvasLayerFinalizer.Finalize(fixture.Request(fixedOverwrite)),
                SectorCanvasLayerFinalizer.Finalize(fixture.Request(boundaryOverwrite)),
                SectorCanvasLayerFinalizer.Finalize(fixture.Request(map15ExitApproved: false)),
                SectorCanvasLayerFinalizer.Finalize(fixture.Request(productionSeedApprovalCount: 1)),
            };

            Assert.That(results.All(value => !value.Success), Is.True);
            Assert.That(results.All(value => value.Plan == null), Is.True);
            Assert.That(results.All(value => value.InputDigest == string.Empty), Is.True);
            Assert.That(results.All(value => value.OutputDigest == string.Empty), Is.True);
            Assert.That(results.All(value => value.Failures.Count > 0), Is.True);
            Assert.That(results.SelectMany(value => value.Failures).Select(value => value.Code),
                Does.Contain(FinalCanvasLayerFailureCode.ForbiddenOperation));
            Assert.That(results.SelectMany(value => value.Conflicts).Select(value => value.Kind),
                Does.Contain(FinalCanvasConflictKind.FixedSliceOverwrite));
            Assert.That(results.SelectMany(value => value.Conflicts).Select(value => value.Kind),
                Does.Contain(FinalCanvasConflictKind.BoundaryApertureOverwrite));
        }

        [Test]
        public void FinalizerDoesNotMutateWorldAssemblyAuthoringFilesTilesScenesOrGameplayObjects()
        {
            var request = fixture.Request();
            var inputClaims = request.Claims.Select(value => value.StableToken).ToArray();
            var upstream = new[]
            {
                request.Map15ExitDigest, request.WorldAssemblyDigest, request.SectorOwnershipDigest,
                request.BoundaryAuthorityDigest, request.FixedCanvasAuthorityDigest,
            };
            var result = SectorCanvasLayerFinalizer.Finalize(request);

            AssertPass(result);
            Assert.That(request.Claims.Select(value => value.StableToken), Is.EqualTo(inputClaims));
            Assert.That(new[]
            {
                request.Map15ExitDigest, request.WorldAssemblyDigest, request.SectorOwnershipDigest,
                request.BoundaryAuthorityDigest, request.FixedCanvasAuthorityDigest,
            }, Is.EqualTo(upstream));
            Assert.That(new[]
            {
                result.Plan.NewRngDrawCount, result.Plan.SliceCreationCount,
                result.Plan.GeneratedFileWriteCount, result.Plan.TilemapMutationCount,
                result.Plan.SceneMutationCount, result.Plan.PrefabMutationCount,
                result.Plan.GameObjectMutationCount, result.Plan.GameplaySpawnCount,
                result.Plan.ProductionSeedApprovalCount, result.Plan.SectorRerollCount,
                result.Plan.FallbackCarveCount, result.Plan.FullRegressionCount,
            }.All(value => value == 0), Is.True);
            Assert.That(result.Plan.Cells.SelectMany(value => value.Winners)
                .Select(value => value.StableToken)
                .All(value => !value.Contains("/") && !value.Contains("\\") && !value.Contains("\n")), Is.True);
        }

        [Test]
        public void Map16HandoffKeepsMap16_02Locked()
        {
            var result = fixture.Finalize();

            AssertPass(result);
            Assert.That(SectorFinalCanvasLayerPlan.DownstreamOwner,
                Is.EqualTo("MAP16_02_VALIDATE_PROTECTION_CLEANUP_AND_DENSITY"));
            Assert.That(SectorFinalCanvasLayerPlan.OpensDownstreamTask, Is.False);
            Assert.That(result.Plan.Request.PublicationLabel,
                Is.EqualTo(SectorCanvasLayerFinalizer.ReferencePublicationLabel));
            Assert.That(result.Plan.ProductionSeedApprovalCount, Is.Zero);
            Assert.That(result.Plan.FullRegressionCount, Is.Zero);
        }

        private static void AssertPass(FinalCanvasLayerResult result)
        {
            Assert.That(result.Success, Is.True, Join(result));
        }

        private static void AssertAtomicFailure(
            FinalCanvasLayerResult result,
            FinalCanvasConflictKind expectedConflict)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.InputDigest, Is.Empty);
            Assert.That(result.OutputDigest, Is.Empty);
            Assert.That(result.Conflicts.Select(value => value.Kind), Does.Contain(expectedConflict));
            Assert.That(result.Failures, Is.Not.Empty);
        }

        private static string Join(FinalCanvasLayerResult result) => result == null
            ? "null"
            : string.Join(";", result.Failures.Select(value => value.ToString()));

        private sealed class ReferenceFinalCanvasFixture
        {
            internal static readonly FinalCanvasCellCoordinate FixedCoordinate =
                new FinalCanvasCellCoordinate(1, 1);
            internal static readonly FinalCanvasCellCoordinate BoundaryCoordinate =
                new FinalCanvasCellCoordinate(2, 1);
            internal static readonly FinalCanvasCellCoordinate RouteCoordinate =
                new FinalCanvasCellCoordinate(3, 1);
            internal static readonly FinalCanvasCellCoordinate SpecialEntranceCoordinate =
                new FinalCanvasCellCoordinate(4, 1);

            private const string Map15ExitDigest =
                "1bf40f24898f41f6f004a9b363262d287445200e5f6c223edf2ea35386300dc8";
            private const string WorldAssemblyDigest =
                "71a38dd6452b1805244166cc832745c8ba84a939b96dd7b0378712b5b1a52cfb";
            private const string BoundaryAuthorityDigest =
                "f7ff1c49f5bc33a4ad57799269bc3915806fe0cb60f347ed76eb16ea26f7fc68";
            private const string FixedCanvasAuthorityDigest =
                "4545e7962dc4da03ec04fe57d3b90d28bb60c50474a8c6d93b63eb392168191b";

            private readonly FinalCanvasLayerClaim[] claims;

            private ReferenceFinalCanvasFixture(IEnumerable<FinalCanvasLayerClaim> sourceClaims)
            {
                claims = sourceClaims.OrderBy(value => value).ToArray();
            }

            internal IReadOnlyList<FinalCanvasLayerClaim> Claims => claims;

            internal static ReferenceFinalCanvasFixture Create()
            {
                var claims = new List<FinalCanvasLayerClaim>(1536 * 7 + 16);
                for (var y = 0; y < SectorFinalCanvasLayerPlan.SectorHeight; y++)
                for (var x = 0; x < SectorFinalCanvasLayerPlan.SectorWidth; x++)
                {
                    var coordinate = new FinalCanvasCellCoordinate(x, y);
                    foreach (FinalCanvasLayerKind layer in Enum.GetValues(typeof(FinalCanvasLayerKind)))
                    {
                        claims.Add(new FinalCanvasLayerClaim(
                            "BASE_" + coordinate.RowMajorIndex.ToString("D4", CultureInfo.InvariantCulture) +
                            "_" + layer.ToString().ToUpperInvariant(), coordinate, layer, BaseKind(layer),
                            FinalCanvasSourceOwner.QuietFiller, FinalCanvasClaimPriority.QuietFiller,
                            FinalCanvasProtectionKind.None, false,
                            "MAP16_REFERENCE_QUIET_FILLER", "REFERENCE FINAL CANVAS LAYER PLAN"));
                    }
                }

                AddProtectedPair(claims, "FIXED", FixedCoordinate,
                    FinalCanvasSourceOwner.FixedSlice, FinalCanvasClaimPriority.FixedSlice,
                    FinalCanvasProtectionKind.FixedSlice, FinalCanvasCellKind.Ground,
                    "MAP07_FIXED_CANVAS_AUTHORITY");
                AddProtectedPair(claims, "BOUNDARY", BoundaryCoordinate,
                    FinalCanvasSourceOwner.Boundary, FinalCanvasClaimPriority.BoundaryAperture,
                    FinalCanvasProtectionKind.BoundaryAperture, FinalCanvasCellKind.Ground,
                    "MAP08_BOUNDARY_APERTURE_AUTHORITY");
                AddProtectedPair(claims, "ROUTE", RouteCoordinate,
                    FinalCanvasSourceOwner.MandatoryRoute,
                    FinalCanvasClaimPriority.MandatoryRouteProtectedOpen,
                    FinalCanvasProtectionKind.MandatoryRouteProtectedOpen,
                    FinalCanvasCellKind.Traversable, "MAP14_PROTECTED_ROUTE_AUTHORITY");
                AddProtectedPair(claims, "SPECIAL_ENTRANCE", SpecialEntranceCoordinate,
                    FinalCanvasSourceOwner.SpecialRegion,
                    FinalCanvasClaimPriority.SpecialEntranceBuffer,
                    FinalCanvasProtectionKind.SpecialEntranceBuffer,
                    FinalCanvasCellKind.Traversable, "MAP13_SPECIAL_ENTRANCE_AUTHORITY");
                claims.Add(Marker("ACTIVITY_MARKER", new FinalCanvasCellCoordinate(10, 10),
                    FinalCanvasSourceOwner.Activity, FinalCanvasClaimPriority.ActivityMarker,
                    "MAP12_ACTIVITY_MARKER"));
                claims.Add(Marker("EVENT_MARKER", new FinalCanvasCellCoordinate(11, 10),
                    FinalCanvasSourceOwner.EventOverlay, FinalCanvasClaimPriority.EventMarker,
                    "MAP12_EVENT_MARKER"));

                return new ReferenceFinalCanvasFixture(claims);
            }

            internal FinalCanvasLayerClaim Claim(
                string claimId,
                FinalCanvasCellCoordinate coordinate,
                FinalCanvasLayerKind layer,
                FinalCanvasCellKind kind,
                FinalCanvasSourceOwner owner,
                FinalCanvasClaimPriority priority) => new FinalCanvasLayerClaim(
                    claimId, coordinate, layer, kind, owner, priority,
                    FinalCanvasProtectionKind.None, false,
                    "MAP16_SYNTHETIC_INVALID_EVIDENCE", "REFERENCE FINAL CANVAS LAYER PLAN");

            internal FinalCanvasLayerRequest Request(
                IEnumerable<FinalCanvasLayerClaim> sourceClaims = null,
                int width = SectorFinalCanvasLayerPlan.SectorWidth,
                bool map15ExitApproved = true,
                int productionSeedApprovalCount = 0) => new FinalCanvasLayerRequest(
                    "SECTOR_084", width, SectorFinalCanvasLayerPlan.SectorHeight,
                    sourceClaims ?? claims, map15ExitApproved,
                    Map15ExitDigest, WorldAssemblyDigest,
                    FinalCanvasLayerDigest.HashCanonicalText("MAP14_SECTOR_OWNERSHIP_PUBLIC"),
                    BoundaryAuthorityDigest, FixedCanvasAuthorityDigest,
                    SectorCanvasLayerFinalizer.ReferencePublicationLabel,
                    productionSeedApprovalCount: productionSeedApprovalCount);

            internal FinalCanvasLayerResult Finalize() =>
                SectorCanvasLayerFinalizer.Finalize(Request());

            private static void AddProtectedPair(
                ICollection<FinalCanvasLayerClaim> claims,
                string prefix,
                FinalCanvasCellCoordinate coordinate,
                FinalCanvasSourceOwner owner,
                FinalCanvasClaimPriority priority,
                FinalCanvasProtectionKind protection,
                FinalCanvasCellKind terrainKind,
                string provenance)
            {
                claims.Add(new FinalCanvasLayerClaim(
                    prefix + "_TERRAIN", coordinate, FinalCanvasLayerKind.Terrain, terrainKind,
                    owner, priority, protection, true, provenance,
                    "REFERENCE FINAL CANVAS LAYER PLAN"));
                claims.Add(new FinalCanvasLayerClaim(
                    prefix + "_PROTECTION", coordinate, FinalCanvasLayerKind.Protection,
                    FinalCanvasCellKind.ProtectedOpen, owner, priority, protection, true,
                    provenance, "REFERENCE FINAL CANVAS LAYER PLAN"));
            }

            private static FinalCanvasLayerClaim Marker(
                string claimId,
                FinalCanvasCellCoordinate coordinate,
                FinalCanvasSourceOwner owner,
                FinalCanvasClaimPriority priority,
                string provenance) => new FinalCanvasLayerClaim(
                    claimId, coordinate, FinalCanvasLayerKind.Marker, FinalCanvasCellKind.Marker,
                    owner, priority, FinalCanvasProtectionKind.None, false,
                    provenance, "REFERENCE FINAL CANVAS LAYER PLAN");

            private static FinalCanvasCellKind BaseKind(FinalCanvasLayerKind layer)
            {
                switch (layer)
                {
                    case FinalCanvasLayerKind.Terrain: return FinalCanvasCellKind.Ground;
                    case FinalCanvasLayerKind.Affordance: return FinalCanvasCellKind.Traversable;
                    case FinalCanvasLayerKind.Material: return FinalCanvasCellKind.Material;
                    case FinalCanvasLayerKind.SourceOwner: return FinalCanvasCellKind.Owner;
                    default: return FinalCanvasCellKind.None;
                }
            }
        }
    }
}
