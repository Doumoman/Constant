using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.MicroPatterns;

namespace StarNight.Map.WorldGeneration.TerrainClusters.Tests
{
    [TestFixture]
    [Category("MAP11_05")]
    public sealed class TerrainClusterPatternRendererTests
    {
        [Test]
        public void Render_ExplicitEmptyZonesAndPlacementsPublishesPatternFreeStaticShellCanvas()
        {
            var fixture = BuildFixture();
            var catalog = BuildCatalog(PatternSpec.Single(
                "MP_UNUSED", new LocalTileCoord(0, 0), "MARKER", "MARKER", "MARKER_UNUSED"));

            var result = Render(fixture, catalog,
                Array.Empty<TerrainClusterPatternZoneCell>(),
                Array.Empty<TerrainClusterPatternPlacementIntent>());

            AssertSuccess(result);
            var report = result.Report;
            var activeCount = fixture.Canvas.TileCells.Count(value =>
                value.State == ClusterChunkMaskState.Active);
            var protectedCount = fixture.Traversal.ProtectedTiles
                .Select(value => value.CompiledCoordinate).Distinct().Count();
            Assert.That(report.IsPatternFree, Is.True);
            Assert.That(report.Placements, Is.Empty);
            Assert.That(report.ApplicationPlans, Is.Empty);
            Assert.That(report.ApplicationPlanDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(report.Map10TargetCoordinateCount, Is.Zero);
            Assert.That(report.GeometryCarveSubstrateCoordinateCount, Is.Zero);
            Assert.That(report.RendererInvocationCount, Is.Zero);
            Assert.That(report.RendererDeltaCoordinateCount, Is.Zero);
            Assert.That(report.ChangedCoordinateCount, Is.Zero);
            Assert.That(report.ProtectedWriteCount, Is.Zero);
            Assert.That(report.ProtectedValueChangeCount, Is.Zero);
            Assert.That(report.RenderDelta, Is.Null);
            Assert.That(report.FullWorkingCanvasCoordinateCount, Is.EqualTo(activeCount));
            Assert.That(report.UntouchedFullCanvasCoordinateCount, Is.EqualTo(activeCount));
            Assert.That(report.InitialWorkingCanvas, Is.SameAs(report.FinalWorkingCanvas));
            Assert.That(report.InitialWorkingCanvas.CanonicalDigest,
                Is.EqualTo(report.FinalWorkingCanvas.CanonicalDigest));
            Assert.That(report.ZoneMap.AbsoluteProtectedCoordinateCount, Is.EqualTo(protectedCount));
            Assert.That(report.ZoneMap.Cells, Is.Not.Empty);
            Assert.That(report.ZoneMap.Cells.All(value =>
                value.HasKind(TerrainClusterPatternZoneKind.AbsoluteProtected)), Is.True);

            var shellByCoordinate = fixture.Witness.StaticShell.Cells.ToDictionary(
                value => value.CompiledCoordinate);
            foreach (var cell in report.InitialWorkingCanvas.Cells)
            {
                Assert.That(shellByCoordinate.TryGetValue(cell.Coordinate, out var shell), Is.True);
                Assert.That(cell.StaticShellCell, Is.SameAs(shell));
                Assert.That(cell.Solid,
                    Is.EqualTo(shell.Occupancy == TerrainClusterShellOccupancy.Solid));
                Assert.That(cell.IsGeometryCarveSubstrate, Is.False);
                Assert.That(report.FinalWorkingCanvas.TryGetCell(cell.Coordinate, out var final), Is.True);
                Assert.That(final.State.ValuesEqual(cell.State), Is.True);
                Assert.That(final.State.Provenance, Is.EqualTo(cell.State.Provenance));
            }
        }

        [Test]
        public void Render_PatternFreeReversedEmptyInputsAndCultureKeepCanonicalDigest()
        {
            var fixture = BuildFixture();
            var catalog = BuildCatalog(PatternSpec.Single(
                "MP_UNUSED_ORDER", new LocalTileCoord(0, 0), "MARKER", "MARKER", "MARKER_UNUSED"));
            var originalCulture = CultureInfo.CurrentCulture;
            try
            {
                var first = Render(fixture, catalog,
                    Array.Empty<TerrainClusterPatternZoneCell>(),
                    Array.Empty<TerrainClusterPatternPlacementIntent>());
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                var second = Render(fixture, catalog,
                    Enumerable.Empty<TerrainClusterPatternZoneCell>().Reverse(),
                    Enumerable.Empty<TerrainClusterPatternPlacementIntent>().Reverse());

                AssertSuccess(first);
                AssertSuccess(second);
                Assert.That(second.Report.IsPatternFree, Is.True);
                Assert.That(second.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));
                Assert.That(second.ZoneMap.CanonicalDigest, Is.EqualTo(first.ZoneMap.CanonicalDigest));
                Assert.That(second.InitialWorkingCanvas.CanonicalDigest,
                    Is.EqualTo(first.InitialWorkingCanvas.CanonicalDigest));
                Assert.That(second.FinalWorkingCanvas.CanonicalDigest,
                    Is.EqualTo(first.FinalWorkingCanvas.CanonicalDigest));
            }
            finally { CultureInfo.CurrentCulture = originalCulture; }
        }

        [Test]
        public void Render_PatternFreeDigestMismatchStillRejectsAtomically()
        {
            var fixture = BuildFixture();
            var catalog = BuildCatalog(PatternSpec.Single(
                "MP_UNUSED_MISMATCH", new LocalTileCoord(0, 0), "MARKER", "MARKER", "MARKER_UNUSED"));
            var request = new TerrainClusterPatternRenderRequest(
                fixture.Canvas, "wrong-local-canvas-digest",
                fixture.Traversal, fixture.Traversal.CanonicalDigest,
                fixture.Witness, fixture.Witness.CanonicalDigest,
                catalog, catalog.StableDigest,
                Array.Empty<TerrainClusterPatternZoneCell>(),
                Array.Empty<TerrainClusterPatternPlacementIntent>());

            AssertFailure(TerrainClusterPatternRenderer.Render(request),
                TerrainClusterPatternRenderErrorCode.ArtifactDigestMismatch);
        }

        [Test]
        public void Render_NonprotectedZoneAndEmptyPlacementsRetainsMissingInputFailure()
        {
            var fixture = BuildFixture();
            var catalog = BuildCatalog(PatternSpec.Single(
                "MP_UNUSED_ZONE", new LocalTileCoord(0, 0), "MARKER", "MARKER", "MARKER_UNUSED"));
            var coordinate = FindUnprotectedAir(fixture,
                fixture.Canvas.TileCells.Where(value => value.State == ClusterChunkMaskState.Active)
                    .Select(value => value.Coordinate), 1)[0];

            var result = Render(fixture, catalog,
                new[] { Zone(coordinate, TerrainClusterPatternZoneKind.GeometryAdd) },
                Array.Empty<TerrainClusterPatternPlacementIntent>());

            AssertFailure(result, TerrainClusterPatternRenderErrorCode.MissingInput);
            Assert.That(result.Errors.Any(value =>
                value.Path == "placements" &&
                value.Detail == "At least one caller-selected placement is required."), Is.True);
        }

        [Test]
        public void Render_NullCollectionsDoNotQualifyAsPatternFree()
        {
            var fixture = BuildFixture();
            var catalog = BuildCatalog(PatternSpec.Single(
                "MP_UNUSED_NULL", new LocalTileCoord(0, 0), "MARKER", "MARKER", "MARKER_UNUSED"));
            var request = CreateRequest(fixture, catalog, null, null);

            AssertFailure(TerrainClusterPatternRenderer.Render(request),
                TerrainClusterPatternRenderErrorCode.MissingInput);
        }

        [Test]
        public void Render_NormalPlacementStillInvokesMap10OrderedRendererOnce()
        {
            var setup = BuildSuccessSetup(BuildFixture());

            var result = TerrainClusterPatternRenderer.Render(setup.Request);

            AssertSuccess(result);
            Assert.That(result.Report.IsPatternFree, Is.False);
            Assert.That(result.Report.RendererInvocationCount, Is.EqualTo(1));
            Assert.That(result.ApplicationPlans, Is.Not.Empty);
            Assert.That(result.RenderDelta, Is.Not.Null);
            Assert.That(result.Report.Map10TargetCoordinateCount, Is.GreaterThan(0));
        }

        [Test]
        public void Render_CarveSubstrateSeedsSolidAndCarvePublishesAir()
        {
            var fixture = BuildFixture();
            var setup = BuildSuccessSetup(fixture);
            var shellBefore = fixture.Witness.StaticShell.Cells.ToDictionary(
                value => value.CompiledCoordinate, value => value.Occupancy);

            var result = TerrainClusterPatternRenderer.Render(setup.Request);

            AssertSuccess(result);
            Assert.That(shellBefore[setup.CarveCoordinate], Is.EqualTo(TerrainClusterShellOccupancy.Air));
            Assert.That(result.InitialWorkingCanvas.TryGetCell(setup.CarveCoordinate, out var initial), Is.True);
            Assert.That(initial.Solid, Is.True);
            Assert.That(initial.IsGeometryCarveSubstrate, Is.True);
            Assert.That(initial.GeometryProvenance, Does.Contain(TerrainClusterPatternGeometryProvenanceKind.StaticShellAir));
            Assert.That(initial.GeometryProvenance, Does.Contain(TerrainClusterPatternGeometryProvenanceKind.GeometryCarveSubstrate));
            Assert.That(result.FinalWorkingCanvas.TryGetCell(setup.CarveCoordinate, out var final), Is.True);
            Assert.That(final.Solid, Is.False);
            Assert.That(result.RenderDelta.Cells.Single(value => value.TargetCoordinate == setup.CarveCoordinate)
                .Writes.Single().Operation, Is.EqualTo(MicroPatternOperation.CarveAir));
        }

        [Test]
        public void Render_AddAffordanceMarker_UsesAllowedZones()
        {
            var setup = BuildSuccessSetup(BuildFixture());
            var result = TerrainClusterPatternRenderer.Render(setup.Request);

            AssertSuccess(result);
            Assert.That(result.FinalWorkingCanvas.TryGetCell(setup.AddCoordinate, out var add), Is.True);
            Assert.That(add.Solid, Is.True);
            Assert.That(result.FinalWorkingCanvas.TryGetCell(setup.AffordanceCoordinate, out var affordance), Is.True);
            Assert.That(affordance.AffordanceId, Is.EqualTo("AFFORDANCE_TEST"));
            Assert.That(result.FinalWorkingCanvas.TryGetCell(setup.MarkerCoordinate, out var marker), Is.True);
            Assert.That(marker.MarkerId, Is.EqualTo("MARKER_TEST"));
        }

        [Test]
        public void Render_SubstrateAndRendererNeverMutateMap1104StaticShell()
        {
            var fixture = BuildFixture();
            var setup = BuildSuccessSetup(fixture);
            var shell = fixture.Witness.StaticShell;
            var snapshot = shell.Cells.Select(value => Tuple.Create(
                value, value.Occupancy, value.IsProtectedOpen, value.Provenance.ToArray())).ToArray();

            AssertSuccess(TerrainClusterPatternRenderer.Render(setup.Request));

            Assert.That(fixture.Witness.StaticShell, Is.SameAs(shell));
            foreach (var item in snapshot)
            {
                Assert.That(item.Item1.Occupancy, Is.EqualTo(item.Item2));
                Assert.That(item.Item1.IsProtectedOpen, Is.EqualTo(item.Item3));
                Assert.That(item.Item1.Provenance, Is.EqualTo(item.Item4));
            }
        }

        [Test]
        public void Render_SubstrateProtectionOverlapRejectsAtomically()
        {
            var fixture = BuildFixture();
            var coordinate = fixture.Traversal.ProtectedTiles[0].CompiledCoordinate;
            var catalog = BuildCatalog(PatternSpec.NoChange("MP_NO_CHANGE"));
            var origin = FindActiveFootprint(fixture, false);
            var zones = new[] { Zone(coordinate, TerrainClusterPatternZoneKind.GeometryCarve) };
            var intent = Intent(catalog, "MP_NO_CHANGE", "TCP_PROTECTED_OVERLAP", origin);

            AssertFailure(Render(fixture, catalog, zones, new[] { intent }),
                TerrainClusterPatternRenderErrorCode.ProtectedZoneOverlap);
        }

        [Test]
        public void Render_FullCanvasAndMap10PlanUnionHaveExactRepairedCoverage()
        {
            var fixture = BuildFixture();
            var setup = BuildSuccessSetup(fixture);
            var result = TerrainClusterPatternRenderer.Render(setup.Request);

            AssertSuccess(result);
            var activeCount = fixture.Canvas.TileCells.Count(value => value.State == ClusterChunkMaskState.Active);
            var planUnion = result.ApplicationPlans.SelectMany(value => value.Cells)
                .Select(value => value.TargetCoordinate).Distinct().ToArray();
            Assert.That(result.Report.FullWorkingCanvasCoordinateCount, Is.EqualTo(activeCount));
            Assert.That(result.Report.Map10TargetCoordinateCount, Is.EqualTo(planUnion.Length));
            Assert.That(result.RenderDelta.InputTarget.Cells.Select(value => value.TargetCoordinate),
                Is.EquivalentTo(planUnion));
            Assert.That(result.Report.Map10TargetCoordinateCount, Is.LessThan(activeCount));
            Assert.That(result.Report.UntouchedFullCanvasCoordinateCount,
                Is.EqualTo(activeCount - planUnion.Length));
        }

        [Test]
        public void Render_CoordinatesOutsidePlanUnionRemainUnchanged()
        {
            var setup = BuildSuccessSetup(BuildFixture());
            var result = TerrainClusterPatternRenderer.Render(setup.Request);
            AssertSuccess(result);
            var union = result.ApplicationPlans.SelectMany(value => value.Cells)
                .Select(value => value.TargetCoordinate).ToHashSet();
            var outside = result.InitialWorkingCanvas.Cells.Where(value => !union.Contains(value.Coordinate)).ToArray();
            Assert.That(outside, Is.Not.Empty);
            foreach (var before in outside)
            {
                Assert.That(result.FinalWorkingCanvas.TryGetCell(before.Coordinate, out var after), Is.True);
                Assert.That(after.State.ValuesEqual(before.State), Is.True, before.Coordinate.ToString());
                Assert.That(after.State.Provenance, Is.EqualTo(before.State.Provenance));
            }
        }

        [Test]
        public void Render_OverlappingPlanFootprintsCanonicalizeUnionAndCoalesceIdenticalWrites()
        {
            var fixture = BuildFixture();
            var setup = BuildSuccessSetup(fixture, twoPlacements: true);
            var result = TerrainClusterPatternRenderer.Render(setup.Request);

            AssertSuccess(result);
            Assert.That(result.ApplicationPlans, Has.Count.EqualTo(2));
            Assert.That(result.Report.Map10TargetCoordinateCount, Is.EqualTo(16));
            Assert.That(result.RenderDelta.Writes.Where(value => value.TargetCoordinate == setup.AddCoordinate)
                .Single().IsCoalesced, Is.True);
        }

        [Test]
        public void Render_ConflictingWritesRejectAtomically()
        {
            var fixture = BuildFixture();
            var origin = FindActiveFootprint(fixture, false);
            var coordinate = FindUnprotectedAir(fixture, Footprint(origin), 1)[0];
            var local = Subtract(coordinate, origin);
            var first = PatternSpec.Single("MP_CONFLICT_A", local, "AFFORDANCE", "AFFORDANCE", "AFF_A");
            var second = PatternSpec.Single("MP_CONFLICT_B", local, "AFFORDANCE", "AFFORDANCE", "AFF_B");
            var catalog = BuildCatalog(first, second);
            var zones = new[] { Zone(coordinate, TerrainClusterPatternZoneKind.Affordance) };
            var intents = new[]
            {
                Intent(catalog, first.Id, "TCP_CONFLICT_A", origin),
                Intent(catalog, second.Id, "TCP_CONFLICT_B", origin),
            };

            AssertFailure(Render(fixture, catalog, zones, intents),
                TerrainClusterPatternRenderErrorCode.RenderConflict);
        }

        [Test]
        public void Render_OutOfActiveCanvasPlanCoordinateRejectsAtomically()
        {
            var fixture = BuildFixture();
            var catalog = BuildCatalog(PatternSpec.NoChange("MP_OUTSIDE"));
            var origin = new LocalTileCoord(fixture.Canvas.TileWidth - 2, fixture.Canvas.TileHeight - 2);
            var intent = Intent(catalog, "MP_OUTSIDE", "TCP_OUTSIDE", origin);

            AssertFailure(Render(fixture, catalog, Array.Empty<TerrainClusterPatternZoneCell>(), new[] { intent }),
                TerrainClusterPatternRenderErrorCode.InvalidPlacement);
        }

        [Test]
        public void Render_ForceNoChangePreservesProtectedWriteAndChangeZero()
        {
            var fixture = BuildFixture();
            var origin = FindActiveFootprint(fixture, true);
            var protectedCoordinate = Footprint(origin)
                .First(value => fixture.Traversal.ProtectedTiles.Any(tile => tile.CompiledCoordinate == value));
            var local = Subtract(protectedCoordinate, origin);
            var catalog = BuildCatalog(PatternSpec.Single(
                "MP_FORCE_PROTECTED", local, "GEOMETRY", "ADD_SOLID", string.Empty,
                "FORCE_NO_CHANGE"));
            var intent = Intent(catalog, "MP_FORCE_PROTECTED", "TCP_FORCE_PROTECTED", origin);

            var result = Render(fixture, catalog, Array.Empty<TerrainClusterPatternZoneCell>(), new[] { intent });

            AssertSuccess(result);
            Assert.That(result.ApplicationPlans.Single().ProtectedHits, Is.Not.Empty);
            Assert.That(result.Report.ProtectedWriteCount, Is.Zero);
            Assert.That(result.Report.ProtectedValueChangeCount, Is.Zero);
            Assert.That(result.RenderDelta.Writes, Is.Empty);
        }

        [Test]
        public void Render_RejectCandidateProtectedHitRejectsAtomically()
        {
            var fixture = BuildFixture();
            var origin = FindActiveFootprint(fixture, true);
            var protectedCoordinate = Footprint(origin)
                .First(value => fixture.Traversal.ProtectedTiles.Any(tile => tile.CompiledCoordinate == value));
            var local = Subtract(protectedCoordinate, origin);
            var catalog = BuildCatalog(PatternSpec.Single(
                "MP_REJECT_PROTECTED", local, "GEOMETRY", "ADD_SOLID", string.Empty,
                "REJECT_CANDIDATE"));
            var intent = Intent(catalog, "MP_REJECT_PROTECTED", "TCP_REJECT_PROTECTED", origin);

            AssertFailure(Render(fixture, catalog, Array.Empty<TerrainClusterPatternZoneCell>(), new[] { intent }),
                TerrainClusterPatternRenderErrorCode.ApplicationPlanRejected);
        }

        [Test]
        public void Render_UnsupportedAndUnauthorizedOperationsAccumulate()
        {
            var fixture = BuildFixture();
            var origin = FindActiveFootprint(fixture, false);
            var available = FindUnprotectedAir(fixture, Footprint(origin), 2);
            var spec = new PatternSpec("MP_INVALID_OPS", "FORCE_NO_CHANGE", new[]
            {
                Instruction(Subtract(available[0], origin), "SURFACE", "SURFACE", "SURFACE_BAD"),
                Instruction(Subtract(available[1], origin), "GEOMETRY", "ADD_SOLID", string.Empty),
            });
            var catalog = BuildCatalog(spec);
            var intent = Intent(catalog, spec.Id, "TCP_INVALID_OPS", origin);

            var result = Render(fixture, catalog, Array.Empty<TerrainClusterPatternZoneCell>(), new[] { intent });

            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(TerrainClusterPatternRenderErrorCode.UnsupportedLayerOperation));
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(TerrainClusterPatternRenderErrorCode.UnauthorizedZoneOperation));
            AssertAtomicFailure(result);
        }

        [Test]
        public void Render_ReversedInputsAndCultureKeepCanonicalDigest()
        {
            var fixture = BuildFixture();
            var setup = BuildSuccessSetup(fixture, twoPlacements: true);
            var originalCulture = CultureInfo.CurrentCulture;
            try
            {
                var first = TerrainClusterPatternRenderer.Render(setup.Request);
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                var reversed = CreateRequest(fixture, setup.Catalog,
                    setup.Request.AuthoredZones.Reverse(), setup.Request.Placements.Reverse());
                var second = TerrainClusterPatternRenderer.Render(reversed);
                AssertSuccess(first);
                AssertSuccess(second);
                Assert.That(second.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));
                Assert.That(second.InitialWorkingCanvas.CanonicalDigest, Is.EqualTo(first.InitialWorkingCanvas.CanonicalDigest));
                Assert.That(second.RenderDelta.StableDigest, Is.EqualTo(first.RenderDelta.StableDigest));
            }
            finally { CultureInfo.CurrentCulture = originalCulture; }
        }

        [Test]
        public void Render_SemanticSubstrateZoneChangeChangesDigest()
        {
            var fixture = BuildFixture();
            var setup = BuildSuccessSetup(fixture);
            var first = TerrainClusterPatternRenderer.Render(setup.Request);
            var extra = FindUnprotectedAir(fixture,
                fixture.Canvas.TileCells.Where(value => value.State == ClusterChunkMaskState.Active)
                    .Select(value => value.Coordinate), 1,
                setup.Request.AuthoredZones.Select(value => value.Coordinate).ToHashSet())[0];
            var changedZones = setup.Request.AuthoredZones.Concat(new[]
            {
                Zone(extra, TerrainClusterPatternZoneKind.GeometryCarve),
            });
            var second = TerrainClusterPatternRenderer.Render(
                CreateRequest(fixture, setup.Catalog, changedZones, setup.Request.Placements));

            AssertSuccess(first);
            AssertSuccess(second);
            Assert.That(second.InitialWorkingCanvas.GeometryCarveSubstrateCoordinateCount,
                Is.EqualTo(first.InitialWorkingCanvas.GeometryCarveSubstrateCoordinateCount + 1));
            Assert.That(second.CanonicalDigest, Is.Not.EqualTo(first.CanonicalDigest));
        }

        [Test]
        public void Render_AccumulatedFailuresExposeNoPartialPublication()
        {
            var fixture = BuildFixture();
            var catalog = BuildCatalog(PatternSpec.NoChange("MP_KNOWN"));
            var origin = FindActiveFootprint(fixture, false);
            var intents = new[]
            {
                new TerrainClusterPatternPlacementIntent("bad_known", new MicroPatternId("MP_KNOWN"),
                    MicroPatternTransform.R0, origin, catalog.Definitions.Single().ComputeStableDigest()),
                new TerrainClusterPatternPlacementIntent("TCP_UNKNOWN", new MicroPatternId("MP_UNKNOWN"),
                    MicroPatternTransform.R0, origin, string.Empty),
                new TerrainClusterPatternPlacementIntent("TCP_DUPLICATE", new MicroPatternId("MP_KNOWN"),
                    MicroPatternTransform.R0, origin, catalog.Definitions.Single().ComputeStableDigest()),
                new TerrainClusterPatternPlacementIntent("TCP_DUPLICATE", new MicroPatternId("MP_KNOWN"),
                    MicroPatternTransform.R0, origin, catalog.Definitions.Single().ComputeStableDigest()),
            };
            var result = Render(fixture, catalog, Array.Empty<TerrainClusterPatternZoneCell>(), intents);

            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(TerrainClusterPatternRenderErrorCode.InvalidPlacementId));
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(TerrainClusterPatternRenderErrorCode.DuplicatePlacementId));
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(TerrainClusterPatternRenderErrorCode.UnknownPattern));
            AssertAtomicFailure(result);
        }

        [Test]
        public void RequestAndPublicationDefensivelyCopyMutableCollections()
        {
            var fixture = BuildFixture();
            var setup = BuildSuccessSetup(fixture);
            var zones = setup.Request.AuthoredZones.ToList();
            var placements = setup.Request.Placements.ToList();
            var request = CreateRequest(fixture, setup.Catalog, zones, placements);
            zones.Clear();
            placements.Clear();

            var result = TerrainClusterPatternRenderer.Render(request);

            AssertSuccess(result);
            Assert.That(request.AuthoredZones, Is.Not.Empty);
            Assert.That(request.Placements, Is.Not.Empty);
            Assert.That(result.ZoneMap.Cells, Is.Not.Empty);
            Assert.That(result.InitialWorkingCanvas.Cells, Is.Not.Empty);
            Assert.That(result.FinalWorkingCanvas.Cells, Is.Not.Empty);
        }

        [Test]
        public void ZoneKindsAreExactAndAddCarveConflictRejectsAtomically()
        {
            Assert.That(Enum.GetNames(typeof(TerrainClusterPatternZoneKind)), Is.EqualTo(new[]
            {
                "GeometryAdd", "GeometryCarve", "Affordance", "Marker", "AbsoluteProtected",
            }));
            var fixture = BuildFixture();
            var origin = FindActiveFootprint(fixture, false);
            var coordinate = FindUnprotectedAir(fixture, Footprint(origin), 1)[0];
            var catalog = BuildCatalog(PatternSpec.NoChange("MP_ZONE_CONFLICT"));
            var zones = new[]
            {
                Zone(coordinate, TerrainClusterPatternZoneKind.GeometryAdd),
                Zone(coordinate, TerrainClusterPatternZoneKind.GeometryCarve),
            };
            var intent = Intent(catalog, "MP_ZONE_CONFLICT", "TCP_ZONE_CONFLICT", origin);
            AssertFailure(Render(fixture, catalog, zones, new[] { intent }),
                TerrainClusterPatternRenderErrorCode.ConflictingGeometryZone);
        }

        [Test]
        public void ReportPublishesActualMap10PlanAndRenderDigests()
        {
            var setup = BuildSuccessSetup(BuildFixture());
            var result = TerrainClusterPatternRenderer.Render(setup.Request);

            AssertSuccess(result);
            Assert.That(result.Report.ApplicationPlanDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(result.ApplicationPlans.All(value => value.StableDigest.Length == 64), Is.True);
            Assert.That(result.RenderDelta.RenderRulesetVersion, Is.EqualTo(MicroPatternRenderDelta.RulesetVersion));
            Assert.That(result.RenderDelta.StableDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(result.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
        }

        [Test]
        public void RuntimeSourcesContainNoForbiddenRngCleanupQuietBufferSectorOrTilemapSymbols()
        {
            var root = Path.Combine("Assets", "_Game", "Map", "Runtime", "WorldGeneration", "TerrainClusters");
            var source = string.Join("\n", new[]
            {
                "TerrainClusterPatternZone.cs", "TerrainClusterPatternRenderer.cs",
            }.Select(value => File.ReadAllText(Path.Combine(root, value))));
            var forbidden = new[]
            {
                "UnityEditor", "StageMapGenerator", "GridWorld", "RoomTemplate", "RoomGridTransform",
                "TileMutationService", "SectorRecipeResolver", "System.Random", "UnityEngine.Random",
                "Time.deltaTime", "Tilemap", "MicroPatternPlanner", "MicroPatternSelection",
                "MicroPatternLocalCleanup", "QuietBuffer", "Starter16", "SectorCanvas",
            };
            foreach (var symbol in forbidden) Assert.That(source, Does.Not.Contain(symbol), symbol);
        }

        private static SuccessSetup BuildSuccessSetup(Fixture fixture, bool twoPlacements = false)
        {
            var origin = FindActiveFootprint(fixture, false);
            var available = FindUnprotectedAir(fixture, Footprint(origin), 4);
            var carve = available[0];
            var add = available[1];
            var affordance = available[2];
            var marker = available[3];
            var spec = new PatternSpec("MP_PRIMARY", "FORCE_NO_CHANGE", new[]
            {
                Instruction(Subtract(carve, origin), "GEOMETRY", "CARVE_AIR", string.Empty),
                Instruction(Subtract(add, origin), "GEOMETRY", "ADD_SOLID", string.Empty),
                Instruction(Subtract(affordance, origin), "AFFORDANCE", "AFFORDANCE", "AFFORDANCE_TEST"),
                Instruction(Subtract(marker, origin), "MARKER", "MARKER", "MARKER_TEST"),
            });
            var catalog = BuildCatalog(spec);
            var zones = new[]
            {
                Zone(carve, TerrainClusterPatternZoneKind.GeometryCarve),
                Zone(add, TerrainClusterPatternZoneKind.GeometryAdd),
                Zone(affordance, TerrainClusterPatternZoneKind.Affordance),
                Zone(marker, TerrainClusterPatternZoneKind.Marker),
            };
            var placements = new List<TerrainClusterPatternPlacementIntent>
            {
                Intent(catalog, spec.Id, "TCP_PRIMARY_A", origin),
            };
            if (twoPlacements) placements.Add(Intent(catalog, spec.Id, "TCP_PRIMARY_B", origin));
            return new SuccessSetup(
                CreateRequest(fixture, catalog, zones, placements), catalog,
                carve, add, affordance, marker);
        }

        private static TerrainClusterPatternRenderResult Render(
            Fixture fixture,
            MicroPatternAuthoringCatalog catalog,
            IEnumerable<TerrainClusterPatternZoneCell> zones,
            IEnumerable<TerrainClusterPatternPlacementIntent> placements)
        {
            return TerrainClusterPatternRenderer.Render(CreateRequest(fixture, catalog, zones, placements));
        }

        private static TerrainClusterPatternRenderRequest CreateRequest(
            Fixture fixture,
            MicroPatternAuthoringCatalog catalog,
            IEnumerable<TerrainClusterPatternZoneCell> zones,
            IEnumerable<TerrainClusterPatternPlacementIntent> placements)
        {
            return new TerrainClusterPatternRenderRequest(
                fixture.Canvas, fixture.Canvas.CanonicalDigest,
                fixture.Traversal, fixture.Traversal.CanonicalDigest,
                fixture.Witness, fixture.Witness.CanonicalDigest,
                catalog, catalog.StableDigest, zones, placements);
        }

        private static TerrainClusterPatternPlacementIntent Intent(
            MicroPatternAuthoringCatalog catalog,
            string patternId,
            string placementId,
            LocalTileCoord origin)
        {
            Assert.That(catalog.TryGetDefinition(new MicroPatternId(patternId), out var definition), Is.True);
            return new TerrainClusterPatternPlacementIntent(
                placementId, definition.Id, MicroPatternTransform.R0, origin, definition.ComputeStableDigest());
        }

        private static MicroPatternAuthoringCatalog BuildCatalog(params PatternSpec[] specs)
        {
            var catalogRows = specs.Select((spec, index) => new MicroPatternCatalogRowV2(
                spec.Id, "1", "MoonCrater", "R0", spec.ProtectedPolicy,
                "catalog.csv", index + 2)).ToArray();
            var cellRows = new List<MicroPatternCellRowV2>();
            var record = 2;
            foreach (var spec in specs)
            {
                var byCoordinate = spec.Instructions.ToDictionary(value => value.Coordinate);
                for (var y = 0; y < 4; y++)
                {
                    for (var x = 0; x < 4; x++)
                    {
                        var coordinate = new LocalTileCoord(x, y);
                        var instruction = byCoordinate.TryGetValue(coordinate, out var authored)
                            ? authored
                            : Instruction(coordinate, "GEOMETRY", "NO_CHANGE", string.Empty);
                        cellRows.Add(new MicroPatternCellRowV2(spec.Id,
                            x.ToString(CultureInfo.InvariantCulture), y.ToString(CultureInfo.InvariantCulture),
                            instruction.Operation, instruction.Layer, instruction.Payload,
                            "cells.csv", record++));
                    }
                }
            }
            var result = new MicroPatternCellSchemaBuilder().Build(catalogRows, cellRows);
            Assert.That(result.Success, Is.True, string.Join("\n", result.Errors.Select(value => value.ToString())));
            Assert.That(result.Published, Is.True);
            return result.Catalog;
        }

        private static PatternInstruction Instruction(
            LocalTileCoord coordinate, string layer, string operation, string payload)
        {
            return new PatternInstruction(coordinate, layer, operation, payload);
        }

        private static TerrainClusterPatternZoneCell Zone(
            LocalTileCoord coordinate, TerrainClusterPatternZoneKind kind)
        {
            return new TerrainClusterPatternZoneCell(coordinate, kind);
        }

        private static LocalTileCoord FindActiveFootprint(Fixture fixture, bool requireProtected)
        {
            var active = fixture.Canvas.TileCells.Where(value => value.State == ClusterChunkMaskState.Active)
                .Select(value => value.Coordinate).ToHashSet();
            var protectedCoordinates = fixture.Traversal.ProtectedTiles.Select(value => value.CompiledCoordinate).ToHashSet();
            for (var y = 0; y <= fixture.Canvas.TileHeight - 4; y++)
            {
                for (var x = 0; x <= fixture.Canvas.TileWidth - 4; x++)
                {
                    var origin = new LocalTileCoord(x, y);
                    var footprint = Footprint(origin);
                    if (footprint.All(active.Contains) &&
                        (!requireProtected || footprint.Any(protectedCoordinates.Contains)) &&
                        (requireProtected || FindUnprotectedAir(fixture, footprint, 4, null, false).Count >= 4))
                        return origin;
                }
            }
            Assert.Fail("No suitable active 4x4 footprint was found.");
            return default;
        }

        private static List<LocalTileCoord> FindUnprotectedAir(
            Fixture fixture,
            IEnumerable<LocalTileCoord> source,
            int count,
            ISet<LocalTileCoord> excluded = null,
            bool assertCount = true)
        {
            var protectedCoordinates = fixture.Traversal.ProtectedTiles.Select(value => value.CompiledCoordinate).ToHashSet();
            var result = source.Where(value => !protectedCoordinates.Contains(value) &&
                                               (excluded == null || !excluded.Contains(value)) &&
                                               fixture.Witness.StaticShell.TryGetCell(value, out var shell) &&
                                               shell.Occupancy == TerrainClusterShellOccupancy.Air)
                .OrderBy(value => value.Y).ThenBy(value => value.X).Take(count).ToList();
            if (assertCount) Assert.That(result, Has.Count.EqualTo(count));
            return result;
        }

        private static LocalTileCoord[] Footprint(LocalTileCoord origin)
        {
            return Enumerable.Range(0, 4).SelectMany(y => Enumerable.Range(0, 4)
                .Select(x => new LocalTileCoord(origin.X + x, origin.Y + y))).ToArray();
        }

        private static LocalTileCoord Subtract(LocalTileCoord value, LocalTileCoord origin)
        {
            return new LocalTileCoord(value.X - origin.X, value.Y - origin.Y);
        }

        private static Fixture BuildFixture(bool reverseInput = false)
        {
            var contract = CreateContract(reverseInput);
            var validation = TerrainClusterContractValidator.Validate(contract);
            Assert.That(validation.IsValid, Is.True, string.Join("\n", validation.Errors.Select(value => value.ToString())));
            var canvasResult = TerrainClusterFootprintCompiler.Compile(
                new TerrainClusterFootprintCompileRequest(contract, ClusterFootprintTransform.R0));
            Assert.That(canvasResult.IsSuccess, Is.True, string.Join("\n", canvasResult.Errors.Select(value => value.ToString())));
            var canvas = canvasResult.LocalCanvas;
            var roleResult = TerrainClusterRoleSocketCompiler.Compile(
                new TerrainClusterRoleSocketCompileRequest(contract, validation.CanonicalDigest, canvas,
                    canvas.CanonicalDigest, SocketEvidence()));
            Assert.That(roleResult.IsSuccess, Is.True, string.Join("\n", roleResult.Errors.Select(value => value.ToString())));
            var traversalResult = TerrainClusterTraversalCompiler.Compile(
                new TerrainClusterTraversalCompileRequest(contract, validation.CanonicalDigest, canvas,
                    canvas.CanonicalDigest, roleResult.Contract, roleResult.CanonicalDigest));
            Assert.That(traversalResult.IsSuccess, Is.True, string.Join("\n", traversalResult.Errors.Select(value => value.ToString())));
            var intent = CreateWitnessIntent(traversalResult.Compilation, reverseInput);
            var witnessResult = TerrainClusterRouteWitnessCompiler.Compile(
                new TerrainClusterRouteWitnessCompileRequest(canvas, canvas.CanonicalDigest,
                    roleResult.Contract, roleResult.CanonicalDigest, traversalResult.Compilation,
                    traversalResult.CanonicalDigest, intent));
            Assert.That(witnessResult.IsSuccess, Is.True, string.Join("\n", witnessResult.Errors.Select(value => value.ToString())));
            return new Fixture(canvas, traversalResult.Compilation, witnessResult.Report);
        }

        private static TerrainClusterContract CreateContract(bool reverseInput)
        {
            var roles = new[]
            {
                new ClusterRoleAnchor("ANCHOR_ENTRY", ClusterRoleKind.Entry, new LocalTileCoord(0, 1), "NODE_ENTRY"),
                new ClusterRoleAnchor("ANCHOR_BUILD_UP", ClusterRoleKind.BuildUp, new LocalTileCoord(5, 1), "NODE_BUILD_UP"),
                new ClusterRoleAnchor("ANCHOR_CORE", ClusterRoleKind.Core, new LocalTileCoord(12, 1), "NODE_CORE"),
                new ClusterRoleAnchor("ANCHOR_RECOVERY", ClusterRoleKind.Recovery, new LocalTileCoord(25, 1), "NODE_RECOVERY"),
                new ClusterRoleAnchor("ANCHOR_REWARD", ClusterRoleKind.Reward, new LocalTileCoord(30, 1), "NODE_REWARD"),
                new ClusterRoleAnchor("ANCHOR_EXIT", ClusterRoleKind.Exit, new LocalTileCoord(35, 1), "NODE_EXIT"),
            };
            var commonNodes = roles.Select(value => new TraversalNode(value.TraversalNodeId, value.Tile,
                value.Role != ClusterRoleKind.Reward, value.AnchorId)).Concat(new[]
            {
                new TraversalNode("NODE_STEP_A", new LocalTileCoord(8, 1), true, string.Empty),
                new TraversalNode("NODE_STEP_B", new LocalTileCoord(7, 1), false, string.Empty),
            }).ToArray();
            var alternateNodes = commonNodes.Concat(new[]
            {
                new TraversalNode("NODE_HIGH", new LocalTileCoord(8, 3), false, string.Empty),
                new TraversalNode("NODE_HIGH_END", new LocalTileCoord(10, 3), false, string.Empty),
            }).ToArray();
            var commonById = commonNodes.ToDictionary(value => value.NodeId, StringComparer.Ordinal);
            var alternateById = alternateNodes.ToDictionary(value => value.NodeId, StringComparer.Ordinal);
            var baseEdges = new[]
            {
                CreateEdge("EDGE_01_ENTRY", commonById["NODE_ENTRY"], commonById["NODE_BUILD_UP"], true),
                CreateEdge("EDGE_BASE_A1", commonById["NODE_BUILD_UP"], commonById["NODE_STEP_A"], true),
                CreateEdge("EDGE_BASE_A2", commonById["NODE_STEP_A"], commonById["NODE_CORE"], true),
                CreateEdge("EDGE_BASE_B1", commonById["NODE_BUILD_UP"], commonById["NODE_STEP_B"], false),
                CreateEdge("EDGE_BASE_B2", commonById["NODE_STEP_B"], commonById["NODE_CORE"], false),
                CreateEdge("EDGE_04_CORE", commonById["NODE_CORE"], commonById["NODE_RECOVERY"], true),
                CreateEdge("EDGE_05_RECOVERY", commonById["NODE_RECOVERY"], commonById["NODE_EXIT"], true),
            };
            var alternateEdges = baseEdges.Select(edge => CopyEdge(edge, alternateById)).Concat(new[]
            {
                CreateEdge("EDGE_HIGH_01", alternateById["NODE_BUILD_UP"], alternateById["NODE_HIGH"], false),
                CreateEdge("EDGE_HIGH_02", alternateById["NODE_HIGH"], alternateById["NODE_HIGH_END"], false),
                CreateEdge("EDGE_HIGH_03", alternateById["NODE_HIGH_END"], alternateById["NODE_CORE"], false),
                CreateEdge("EDGE_RECOVER", alternateById["NODE_HIGH"], alternateById["NODE_RECOVERY"], false),
            }).ToArray();
            var variants = new[]
            {
                new SpineVariant(new SpineVariantId("SPINE_BASELINE"), true, TraversalGraphKind.Traversal,
                    reverseInput ? commonNodes.Reverse() : commonNodes, reverseInput ? baseEdges.Reverse() : baseEdges),
                new SpineVariant(new SpineVariantId("SPINE_ALTERNATE"), false, TraversalGraphKind.Traversal,
                    reverseInput ? alternateNodes.Reverse() : alternateNodes, reverseInput ? alternateEdges.Reverse() : alternateEdges),
            };
            var ports = new[]
            {
                new ClusterPort("PORT_ENTRY", ClusterPortKind.Entry, true, "ANCHOR_ENTRY",
                    new LocalTileCoord(0, 1), ClusterPortSide.L, new[] { 0, 1, 2, 3, 4 }),
                new ClusterPort("PORT_EXIT", ClusterPortKind.Exit, true, "ANCHOR_EXIT",
                    new LocalTileCoord(35, 1), ClusterPortSide.R, new[] { 1, 2, 3, 4 }),
            };
            return new TerrainClusterContract(
                new TerrainClusterId("TC_PATTERN_RENDERER"),
                new ClusterFootprint(new[]
                {
                    new ClusterChunkCoord(0, 0), new ClusterChunkCoord(1, 0),
                    new ClusterChunkCoord(2, 0), new ClusterChunkCoord(0, 1),
                }),
                reverseInput ? roles.Reverse() : roles,
                reverseInput ? ports.Reverse() : ports,
                new TerrainClusterTraversalContract(reverseInput ? variants.Reverse() : variants),
                reverseInput ? "역순 표시" : "display");
        }

        private static TraversalEdge CreateEdge(string id, TraversalNode from, TraversalNode to, bool mandatory)
        {
            var envelope = new TraversalEnvelope(
                new[] { from.Tile, to.Tile },
                new[] { new LocalTileCoord(from.Tile.X, 0) },
                new[] { new LocalTileCoord(from.Tile.X, 5) },
                Array.Empty<LocalTileCoord>(), Array.Empty<LocalTileCoord>(),
                new[] { to.Tile }, new[] { to.Tile });
            return new TraversalEdge(id, from.NodeId, to.NodeId, TraversalMovementKind.Walk,
                from.Tile, to.Tile, 1, 2, to.Tile, to.Tile, mandatory, envelope);
        }

        private static TraversalEdge CopyEdge(TraversalEdge edge, IDictionary<string, TraversalNode> nodes)
        {
            return CreateEdge(edge.EdgeId, nodes[edge.FromNodeId], nodes[edge.ToNodeId], edge.IsMandatory);
        }

        private static TerrainClusterRouteWitnessIntent CreateWitnessIntent(
            TerrainClusterTraversalCompilation traversal, bool reverseInput)
        {
            var high = new TerrainClusterHighRouteDefinition(
                "HIGH_ROUTE_ONE", new SpineVariantId("SPINE_ALTERNATE"), "NODE_BUILD_UP",
                new[] { "EDGE_HIGH_01", "EDGE_HIGH_02", "EDGE_HIGH_03" },
                "NODE_CORE", "NODE_HIGH",
                reverseInput ? new[] { "BENEFIT_REWARD_ACCESS", "BENEFIT_HEIGHT_ADVANTAGE" } :
                    new[] { "BENEFIT_HEIGHT_ADVANTAGE", "BENEFIT_REWARD_ACCESS" },
                new[] { "NODE_HIGH" });
            var durations = traversal.Edges.Select(edge => new TraversalEdgeDurationEvidence(
                edge.VariantId, edge.EdgeId, edge.EdgeId == "EDGE_RECOVER" ? 2000 : 3000,
                "RULESET_ROUTE_V1"));
            if (reverseInput) durations = durations.Reverse();
            return new TerrainClusterRouteWitnessIntent(
                new SpineVariantId("SPINE_BASELINE"), new[] { high }, durations);
        }

        private static ClusterSectorSocketEvidence[] SocketEvidence()
        {
            return new[]
            {
                new ClusterSectorSocketEvidence("SR_EXIT", "SOCKET_EXIT", ClusterPortSide.R, 3, true, ClusterPortKind.Exit),
                new ClusterSectorSocketEvidence("SR_ENTRY", "SOCKET_ENTRY", ClusterPortSide.L, 2, true, ClusterPortKind.Entry),
            };
        }

        private static void AssertSuccess(TerrainClusterPatternRenderResult result)
        {
            Assert.That(result.Success, Is.True, ErrorText(result));
            Assert.That(result.Report, Is.Not.Null);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
        }

        private static void AssertFailure(
            TerrainClusterPatternRenderResult result,
            TerrainClusterPatternRenderErrorCode expected)
        {
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(expected), ErrorText(result));
            AssertAtomicFailure(result);
        }

        private static void AssertAtomicFailure(TerrainClusterPatternRenderResult result)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Report, Is.Null);
            Assert.That(result.ZoneMap, Is.Null);
            Assert.That(result.ApplicationPlans, Is.Empty);
            Assert.That(result.RenderDelta, Is.Null);
            Assert.That(result.InitialWorkingCanvas, Is.Null);
            Assert.That(result.FinalWorkingCanvas, Is.Null);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Errors, Is.Ordered);
            Assert.That(result.Errors.Distinct().Count(), Is.EqualTo(result.Errors.Count));
        }

        private static string ErrorText(TerrainClusterPatternRenderResult result)
        {
            return string.Join("\n", result.Errors.Select(value => value.ToString()));
        }

        private sealed class Fixture
        {
            public Fixture(TerrainClusterLocalCanvas canvas,
                TerrainClusterTraversalCompilation traversal,
                TerrainClusterRouteWitnessReport witness)
            {
                Canvas = canvas;
                Traversal = traversal;
                Witness = witness;
            }
            public TerrainClusterLocalCanvas Canvas { get; }
            public TerrainClusterTraversalCompilation Traversal { get; }
            public TerrainClusterRouteWitnessReport Witness { get; }
        }

        private sealed class SuccessSetup
        {
            public SuccessSetup(
                TerrainClusterPatternRenderRequest request,
                MicroPatternAuthoringCatalog catalog,
                LocalTileCoord carve,
                LocalTileCoord add,
                LocalTileCoord affordance,
                LocalTileCoord marker)
            {
                Request = request;
                Catalog = catalog;
                CarveCoordinate = carve;
                AddCoordinate = add;
                AffordanceCoordinate = affordance;
                MarkerCoordinate = marker;
            }
            public TerrainClusterPatternRenderRequest Request { get; }
            public MicroPatternAuthoringCatalog Catalog { get; }
            public LocalTileCoord CarveCoordinate { get; }
            public LocalTileCoord AddCoordinate { get; }
            public LocalTileCoord AffordanceCoordinate { get; }
            public LocalTileCoord MarkerCoordinate { get; }
        }

        private sealed class PatternSpec
        {
            public PatternSpec(string id, string protectedPolicy, IEnumerable<PatternInstruction> instructions)
            {
                Id = id;
                ProtectedPolicy = protectedPolicy;
                Instructions = instructions.ToArray();
            }
            public string Id { get; }
            public string ProtectedPolicy { get; }
            public IReadOnlyList<PatternInstruction> Instructions { get; }

            public static PatternSpec NoChange(string id) =>
                new PatternSpec(id, "FORCE_NO_CHANGE", Array.Empty<PatternInstruction>());

            public static PatternSpec Single(
                string id, LocalTileCoord coordinate, string layer, string operation,
                string payload, string policy = "FORCE_NO_CHANGE") =>
                new PatternSpec(id, policy, new[] { Instruction(coordinate, layer, operation, payload) });
        }

        private sealed class PatternInstruction
        {
            public PatternInstruction(LocalTileCoord coordinate, string layer, string operation, string payload)
            {
                Coordinate = coordinate;
                Layer = layer;
                Operation = operation;
                Payload = payload;
            }
            public LocalTileCoord Coordinate { get; }
            public string Layer { get; }
            public string Operation { get; }
            public string Payload { get; }
        }
    }
}
