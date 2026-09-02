using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Baking
{
    [TestFixture]
    [Category("MAP16_05")]
    public sealed class GeneratedMicroChunkSliceBuilderTests
    {
        [Test]
        public void GeneratedSliceSetPublishesSixteenSlicesCellsLayersSocketsAndDigests()
        {
            var authorities = AcceptedAuthorities();
            var first = GeneratedMicroChunkSliceBuilder.Build(
                authorities.Canvas, authorities.Density, authorities.Route, authorities.Partition);
            var repeat = GeneratedMicroChunkSliceBuilder.Build(
                authorities.Canvas, authorities.Density, authorities.Route, authorities.Partition);
            var baseline = GeneratedMicroChunkSliceBuildRequest.FromAuthorities(
                authorities.Canvas, authorities.Density, authorities.Route, authorities.Partition);
            var reverse = GeneratedMicroChunkSliceBuilder.Build(Request(
                authorities, baseline.CellSources.Reverse().ToArray()));

            Assert.That(first.Success, Is.True, Failures(first));
            Assert.That(repeat.Success, Is.True, Failures(repeat));
            Assert.That(reverse.Success, Is.True, Failures(reverse));
            Assert.That(GeneratedMicroChunkSliceSet.SectorWidth, Is.EqualTo(48));
            Assert.That(GeneratedMicroChunkSliceSet.SectorHeight, Is.EqualTo(32));
            Assert.That(GeneratedMicroChunkSliceSet.SectorCellCount, Is.EqualTo(1536));
            Assert.That(GeneratedMicroChunkSliceSet.MicroChunkWidth, Is.EqualTo(12));
            Assert.That(GeneratedMicroChunkSliceSet.MicroChunkHeight, Is.EqualTo(8));
            Assert.That(GeneratedMicroChunkSliceSet.MicroChunkCellCount, Is.EqualTo(96));
            Assert.That(GeneratedMicroChunkSliceSet.ChunkGridWidth, Is.EqualTo(4));
            Assert.That(GeneratedMicroChunkSliceSet.ChunkGridHeight, Is.EqualTo(4));
            Assert.That(GeneratedMicroChunkSliceSet.ChunkCount, Is.EqualTo(16));
            Assert.That(GeneratedMicroChunkSliceSet.MicroPatternWidth, Is.EqualTo(4));
            Assert.That(GeneratedMicroChunkSliceSet.MicroPatternHeight, Is.EqualTo(4));
            Assert.That(GeneratedMicroChunkSliceSet.ChunkPatternGridWidth, Is.EqualTo(3));
            Assert.That(GeneratedMicroChunkSliceSet.ChunkPatternGridHeight, Is.EqualTo(2));
            Assert.That(GeneratedMicroChunkSliceSet.LayerKindsPerCell, Is.EqualTo(7));
            Assert.That(GeneratedMicroChunkSliceSet.ChunkRotationAllowed, Is.False);
            Assert.That(first.SliceSet.Slices, Has.Count.EqualTo(16));
            Assert.That(first.SliceSet.TotalCellCount, Is.EqualTo(1536));
            Assert.That(first.SliceSet.TotalLayerRecordCount, Is.EqualTo(10752));
            Assert.That(first.SliceSet.SocketBandCount, Is.GreaterThan(0));
            Assert.That(first.SliceSet.SocketSideSignatureCount, Is.EqualTo(64));
            Assert.That(GeneratedMicroChunkSliceDigest.IsLowerHexSha256(first.InputDigest), Is.True);
            Assert.That(GeneratedMicroChunkSliceDigest.IsLowerHexSha256(first.OutputDigest), Is.True);
            Assert.That(repeat.InputDigest, Is.EqualTo(first.InputDigest));
            Assert.That(repeat.OutputDigest, Is.EqualTo(first.OutputDigest));
            Assert.That(reverse.InputDigest, Is.EqualTo(first.InputDigest));
            Assert.That(reverse.OutputDigest, Is.EqualTo(first.OutputDigest));

            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var culture = GeneratedMicroChunkSliceBuilder.Build(Request(
                    authorities, baseline.CellSources.Reverse().ToArray()));
                Assert.That(culture.Success, Is.True, Failures(culture));
                Assert.That(culture.InputDigest, Is.EqualTo(first.InputDigest));
                Assert.That(culture.OutputDigest, Is.EqualTo(first.OutputDigest));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }

            TestContext.WriteLine("MAP16_05_EVIDENCE input=" + first.InputDigest +
                " output=" + first.OutputDigest +
                " bands=" + first.SliceSet.SocketBandCount +
                " memberships=" + first.SliceSet.WitnessMembershipCount +
                " member_cells=" + first.SliceSet.WitnessMemberCellCount +
                " passable=" + first.SliceSet.Slices.Sum(value =>
                    value.TraversalSummary.PassableCellCount) +
                " blocked=" + first.SliceSet.Slices.Sum(value =>
                    value.TraversalSummary.BlockedCellCount) +
                " components=" + first.SliceSet.Slices.Sum(value =>
                    value.TraversalSummary.ConnectedPassableComponentCount));
        }

        [Test]
        public void EachSliceContainsExactlyNinetySixUniqueCellsAndSevenLayerRecordsPerCell()
        {
            var set = AcceptedSliceSet();

            Assert.That(set.Slices, Has.Count.EqualTo(16));
            foreach (var slice in set.Slices)
            {
                Assert.That(slice.CellCount, Is.EqualTo(96));
                Assert.That(slice.Cells.Select(value => value.LocalCoordinate).Distinct().Count(),
                    Is.EqualTo(96));
                Assert.That(slice.Cells.Select(value => value.SectorCoordinate).Distinct().Count(),
                    Is.EqualTo(96));
                Assert.That(slice.Cells.All(value => value.LayerCount == 7), Is.True);
                Assert.That(slice.LayerRecordCount, Is.EqualTo(672));
                Assert.That(slice.Width, Is.EqualTo(12));
                Assert.That(slice.Height, Is.EqualTo(8));
                Assert.That(slice.ChunkIndex, Is.EqualTo((slice.ChunkY * 4) + slice.ChunkX));
            }
        }

        [Test]
        public void AllSectorCellsAreCoveredExactlyOnceWithoutGapsOverlapOrOutOfBounds()
        {
            var set = AcceptedSliceSet();

            Assert.That(set.TotalCellCount, Is.EqualTo(1536));
            Assert.That(set.UniqueSectorCellCount, Is.EqualTo(1536));
            Assert.That(set.DuplicateSectorCellCount, Is.Zero);
            Assert.That(set.MissingSectorCellCount, Is.Zero);
            Assert.That(set.OutOfBoundsSectorCellCount, Is.Zero);
            Assert.That(set.Slices.SelectMany(value => value.Cells)
                .Select(value => value.SectorCoordinate).Distinct().Count(), Is.EqualTo(1536));
        }

        [Test]
        public void LayerSourceProtectionAndProvenanceAreCopiedFromFinalCanvasCells()
        {
            var authorities = AcceptedAuthorities();
            var result = GeneratedMicroChunkSliceBuilder.Build(
                authorities.Canvas, authorities.Density, authorities.Route, authorities.Partition);
            Assert.That(result.Success, Is.True, Failures(result));
            var canvas = authorities.Canvas.Cells.ToDictionary(value => value.Coordinate);

            foreach (var generated in result.SliceSet.Slices.SelectMany(value => value.Cells))
            {
                var source = canvas[new FinalCanvasCellCoordinate(
                    generated.SectorCoordinate.X, generated.SectorCoordinate.Y)];
                Assert.That(generated.Layers, Has.Count.EqualTo(7));
                foreach (var record in generated.Layers)
                {
                    var winner = source.Winner(record.Layer);
                    Assert.That(record.CellKind, Is.EqualTo(winner.CellKind));
                    Assert.That(record.SourceOwner, Is.EqualTo(winner.SourceOwner));
                    Assert.That(record.ProvenanceId, Is.EqualTo(winner.ProvenanceId));
                    Assert.That(record.Protection, Is.EqualTo(winner.Protection));
                    Assert.That(record.IsProtected, Is.EqualTo(winner.IsProtected));
                    Assert.That(record.ClaimId, Is.EqualTo(winner.ClaimId));
                    Assert.That(record.SourceCellToken, Is.EqualTo(source.StableToken));
                }
            }
            Assert.That(result.SliceSet.TotalLayerRecordCount, Is.EqualTo(10752));
            Assert.That(result.SliceSet.LayerRecordsWithSourceOwnerCount, Is.EqualTo(10752));
            Assert.That(result.SliceSet.LayerRecordsWithProvenanceCount, Is.EqualTo(10752));
        }

        [Test]
        public void SocketBandsAreDerivedOnlyFromOpenEdgeCellsOnAllFourSides()
        {
            var set = AcceptedSliceSet();
            var sides = Enum.GetValues(typeof(GeneratedMicroChunkSocketSide))
                .Cast<GeneratedMicroChunkSocketSide>().ToArray();

            Assert.That(sides, Is.EqualTo(new[]
            {
                GeneratedMicroChunkSocketSide.Left,
                GeneratedMicroChunkSocketSide.Right,
                GeneratedMicroChunkSocketSide.Down,
                GeneratedMicroChunkSocketSide.Up,
            }));
            Assert.That(set.SocketBandCount, Is.GreaterThan(0));
            Assert.That(set.SocketBandsOnBlockedCellsCount, Is.Zero);
            foreach (var slice in set.Slices)
            {
                Assert.That(slice.SideSignatures.Select(value => value.Side.Value),
                    Is.EquivalentTo(sides));
                foreach (var band in slice.SocketBands)
                {
                    Assert.That(band.Length, Is.GreaterThan(0));
                    Assert.That(band.Cells.All(value => value.IsPassable), Is.True);
                    Assert.That(band.SourceEvidence, Has.Count.EqualTo(band.Length));
                    Assert.That(band.SourceEvidence.All(value => !string.IsNullOrEmpty(value)), Is.True);
                    Assert.That(band.TouchesPassableComponent, Is.True);
                    AssertBandEdgeAndContiguity(band);
                }
            }
        }

        [Test]
        public void SocketSignaturesAndTraversalSummariesAreStableAndNonEmpty()
        {
            var set = AcceptedSliceSet();

            Assert.That(set.InvalidSideSignatureCount, Is.Zero);
            Assert.That(set.InvalidSliceSignatureCount, Is.Zero);
            Assert.That(set.MissingTraversalSummaryCount, Is.Zero);
            Assert.That(set.MissingPassableComponentSummaryCount, Is.Zero);
            foreach (var slice in set.Slices)
            {
                Assert.That(GeneratedMicroChunkSliceDigest.IsLowerHexSha256(
                    slice.Signature.Digest), Is.True);
                Assert.That(slice.SideSignatures, Has.Count.EqualTo(4));
                Assert.That(slice.SideSignatures.All(value =>
                    GeneratedMicroChunkSliceDigest.IsLowerHexSha256(value.Digest)), Is.True);
                Assert.That(slice.TraversalSummary.PassableCellCount +
                    slice.TraversalSummary.BlockedCellCount, Is.EqualTo(96));
                Assert.That(slice.TraversalSummary.ConnectedPassableComponentCount,
                    Is.GreaterThanOrEqualTo(0));
                Assert.That(slice.TraversalSummary.EverySocketBandTouchesPassableComponent, Is.True);
            }
        }

        [Test]
        public void RouteRecoveryWitnessMembershipProjectsIntoGeneratedSlices()
        {
            var authorities = AcceptedAuthorities();
            var result = GeneratedMicroChunkSliceBuilder.Build(
                authorities.Canvas, authorities.Density, authorities.Route, authorities.Partition);
            Assert.That(result.Success, Is.True, Failures(result));
            var observed = result.SliceSet.Slices.SelectMany(value => value.Cells)
                .SelectMany(value => value.WitnessMemberships)
                .Select(value => value.StableToken).OrderBy(value => value).ToArray();
            var expected = authorities.Partition.WitnessProjections.Select(value =>
                    new GeneratedMicroChunkWitnessMembership(
                        value.WitnessKind, value.SourceStableId, value.PathIndex).StableToken)
                .OrderBy(value => value).ToArray();

            Assert.That(observed, Is.EqualTo(expected));
            Assert.That(result.SliceSet.WitnessMembershipCount,
                Is.EqualTo(authorities.Partition.WitnessProjections.Count));
            Assert.That(result.SliceSet.WitnessMembershipCount, Is.GreaterThan(0));
            Assert.That(result.SliceSet.WitnessMemberCellCount, Is.GreaterThan(0));
        }

        [Test]
        public void InvalidSliceInputsFailAtomicallyForMissingCoverageProvenanceBlockedSocketsAndRotation()
        {
            var authorities = AcceptedAuthorities();
            var baseline = GeneratedMicroChunkSliceBuildRequest.FromAuthorities(
                authorities.Canvas, authorities.Density, authorities.Route, authorities.Partition);
            var missing = baseline.CellSources.Take(1535).ToArray();
            var duplicate = baseline.CellSources.Take(1535)
                .Concat(new[] { baseline.CellSources[0] }).ToArray();
            var badProvenance = baseline.CellSources.ToArray();
            var first = badProvenance[0];
            var layers = first.Layers.Select(value => value.Layer == FinalCanvasLayerKind.Terrain
                ? new GeneratedMicroChunkLayerRecord(
                    value.Layer, value.CellKind, value.SourceOwner, string.Empty,
                    value.Protection, value.IsProtected, value.ClaimId, value.SourceCellToken)
                : value).ToArray();
            badProvenance[0] = new GeneratedMicroChunkCellSource(
                first.Address, layers, first.WitnessMemberships);

            AssertAtomicFailure(GeneratedMicroChunkSliceBuilder.Build(null),
                GeneratedMicroChunkSliceFailureCode.MissingRequest);
            AssertAtomicFailure(GeneratedMicroChunkSliceBuilder.Build(
                Request(authorities, missing)),
                GeneratedMicroChunkSliceFailureCode.InvalidCellCount,
                GeneratedMicroChunkSliceFailureCode.MissingCoordinate);
            AssertAtomicFailure(GeneratedMicroChunkSliceBuilder.Build(
                Request(authorities, duplicate)),
                GeneratedMicroChunkSliceFailureCode.DuplicateCoordinate,
                GeneratedMicroChunkSliceFailureCode.MissingCoordinate);
            AssertAtomicFailure(GeneratedMicroChunkSliceBuilder.Build(
                Request(authorities, badProvenance)),
                GeneratedMicroChunkSliceFailureCode.MissingProvenance,
                GeneratedMicroChunkSliceFailureCode.LayerCopyMismatch);
            AssertAtomicFailure(GeneratedMicroChunkSliceBuilder.Build(
                Request(authorities, baseline.CellSources,
                    forcedSockets: new[] { new SectorTileCoordinate(0, 0) })),
                GeneratedMicroChunkSliceFailureCode.BlockedSocketCell);
            AssertAtomicFailure(GeneratedMicroChunkSliceBuilder.Build(
                Request(authorities, baseline.CellSources, rotateNinetyDegrees: true)),
                GeneratedMicroChunkSliceFailureCode.RotationForbidden);
        }

        [Test]
        public void SliceBuilderDoesNotWriteFilesTilemapsScenesPrefabsGameplayOrMarkerSlots()
        {
            var authorities = AcceptedAuthorities();
            var canvasDigest = authorities.Canvas.OutputDigest;
            var densityDigest = authorities.Density.OutputDigest;
            var routeDigest = authorities.Route.OutputDigest;
            var partitionDigest = authorities.Partition.OutputDigest;
            var set = AcceptedSliceSet(authorities);

            Assert.That(set.SourceCanvasPlan, Is.SameAs(authorities.Canvas));
            Assert.That(set.SourceProtectionDensityReport, Is.SameAs(authorities.Density));
            Assert.That(set.SourceRouteRecoveryReport, Is.SameAs(authorities.Route));
            Assert.That(set.SourcePartition, Is.SameAs(authorities.Partition));
            Assert.That(set.MarkerSlotRecordCount, Is.Zero);
            Assert.That(set.StableSpawnIdCount, Is.Zero);
            Assert.That(set.TilemapBakeCount, Is.Zero);
            Assert.That(set.GeneratedFileWriteCount, Is.Zero);
            Assert.That(set.GeneratedAssetWriteCount, Is.Zero);
            Assert.That(set.TilemapMutationCount, Is.Zero);
            Assert.That(set.SceneMutationCount, Is.Zero);
            Assert.That(set.PrefabMutationCount, Is.Zero);
            Assert.That(set.GameObjectMutationCount, Is.Zero);
            Assert.That(set.GameplaySpawnCount, Is.Zero);
            Assert.That(set.PlayerPhysicsSimulationCount, Is.Zero);
            Assert.That(set.SectorRerenderCount, Is.Zero);
            Assert.That(set.SectorRerollCount, Is.Zero);
            Assert.That(set.FallbackCarveCount, Is.Zero);
            Assert.That(set.SilentWideningCount, Is.Zero);
            Assert.That(set.FullRegressionCount, Is.Zero);
            Assert.That(set.ProductionSeedApprovalCount, Is.Zero);
            Assert.That(set.RotationRequestCount, Is.Zero);
            Assert.That(authorities.Canvas.OutputDigest, Is.EqualTo(canvasDigest));
            Assert.That(authorities.Density.OutputDigest, Is.EqualTo(densityDigest));
            Assert.That(authorities.Route.OutputDigest, Is.EqualTo(routeDigest));
            Assert.That(authorities.Partition.OutputDigest, Is.EqualTo(partitionDigest));
        }

        [Test]
        public void Map16HandoffKeepsMap16_06Locked()
        {
            var set = AcceptedSliceSet();

            Assert.That(GeneratedMicroChunkSliceSet.DownstreamOwner,
                Is.EqualTo("MAP16_06_PROJECT_MARKERS_SLOTS_AND_PROVENANCE"));
            Assert.That(GeneratedMicroChunkSliceSet.OpensDownstreamTask, Is.False);
            Assert.That(set.MarkerSlotRecordCount, Is.Zero);
            Assert.That(set.StableSpawnIdCount, Is.Zero);
        }

        private static AuthoritySet AcceptedAuthorities()
        {
            var fixture = new ReferenceFinalRouteRecoveryFixture();
            var routeRequest = fixture.AcceptedRequest();
            var routeResult = SectorFinalRouteRecoveryValidator.Validate(routeRequest);
            Assert.That(routeResult.Success, Is.True,
                string.Join(";", routeResult.Failures.Select(value => value.ToString())));
            var partitionResult = SectorPatternChunkPartitioner.Partition(
                routeRequest.CanvasPlan, routeRequest.ProtectionDensityReport, routeResult.Report);
            Assert.That(partitionResult.Success, Is.True,
                string.Join(";", partitionResult.Failures.Select(value => value.ToString())));
            return new AuthoritySet(
                routeRequest.CanvasPlan, routeRequest.ProtectionDensityReport,
                routeResult.Report, partitionResult.Partition);
        }

        private static GeneratedMicroChunkSliceSet AcceptedSliceSet()
        {
            var authorities = AcceptedAuthorities();
            return AcceptedSliceSet(authorities);
        }

        private static GeneratedMicroChunkSliceSet AcceptedSliceSet(AuthoritySet authorities)
        {
            var result = GeneratedMicroChunkSliceBuilder.Build(
                authorities.Canvas, authorities.Density, authorities.Route, authorities.Partition);
            Assert.That(result.Success, Is.True, Failures(result));
            return result.SliceSet;
        }

        private static GeneratedMicroChunkSliceBuildRequest Request(
            AuthoritySet authorities,
            IEnumerable<GeneratedMicroChunkCellSource> sources,
            IEnumerable<SectorTileCoordinate> forcedSockets = null,
            bool rotateNinetyDegrees = false) => new GeneratedMicroChunkSliceBuildRequest(
                authorities.Canvas, authorities.Density, authorities.Route, authorities.Partition,
                sources, forcedSockets, rotateNinetyDegrees);

        private static void AssertAtomicFailure(
            GeneratedMicroChunkSliceResult result,
            params GeneratedMicroChunkSliceFailureCode[] expectedCodes)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.SliceSet, Is.Null);
            Assert.That(result.OutputDigest, Is.Empty);
            foreach (var code in expectedCodes)
                Assert.That(result.Failures.Select(value => value.Code), Does.Contain(code));
        }

        private static void AssertBandEdgeAndContiguity(GeneratedMicroChunkSocketBand band)
        {
            var positions = band.Cells.Select(value =>
                band.Side == GeneratedMicroChunkSocketSide.Left ||
                band.Side == GeneratedMicroChunkSocketSide.Right
                    ? value.LocalCoordinate.Y : value.LocalCoordinate.X).ToArray();
            Assert.That(positions, Is.Ordered);
            for (var index = 1; index < positions.Length; index++)
                Assert.That(positions[index], Is.EqualTo(positions[index - 1] + 1));
            foreach (var cell in band.Cells)
            {
                switch (band.Side)
                {
                    case GeneratedMicroChunkSocketSide.Left:
                        Assert.That(cell.LocalCoordinate.X, Is.Zero);
                        break;
                    case GeneratedMicroChunkSocketSide.Right:
                        Assert.That(cell.LocalCoordinate.X, Is.EqualTo(11));
                        break;
                    case GeneratedMicroChunkSocketSide.Down:
                        Assert.That(cell.LocalCoordinate.Y, Is.Zero);
                        break;
                    case GeneratedMicroChunkSocketSide.Up:
                        Assert.That(cell.LocalCoordinate.Y, Is.EqualTo(7));
                        break;
                }
            }
        }

        private static string Failures(GeneratedMicroChunkSliceResult result) =>
            result == null ? "NULL RESULT" : string.Join(";",
                result.Failures.Select(value => value.ToString()));

        private sealed class AuthoritySet
        {
            public AuthoritySet(
                SectorFinalCanvasLayerPlan canvas,
                SectorCanvasProtectionDensityReport density,
                SectorFinalRouteRecoveryReport route,
                SectorPatternChunkPartition partition)
            {
                Canvas = canvas;
                Density = density;
                Route = route;
                Partition = partition;
            }

            public SectorFinalCanvasLayerPlan Canvas { get; }
            public SectorCanvasProtectionDensityReport Density { get; }
            public SectorFinalRouteRecoveryReport Route { get; }
            public SectorPatternChunkPartition Partition { get; }
        }
    }
}
