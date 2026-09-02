using System;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Baking
{
    [TestFixture]
    [Category("MAP16_04")]
    public sealed class SectorPatternChunkPartitionerTests
    {
        [Test]
        public void PatternChunkPartitionPublishesConstantsSlotsCoverageAndDigests()
        {
            var authorities = AcceptedAuthorities();
            var first = SectorPatternChunkPartitioner.Partition(
                authorities.Canvas, authorities.Density, authorities.Route);
            var repeat = SectorPatternChunkPartitioner.Partition(
                authorities.Canvas, authorities.Density, authorities.Route);
            var baselineRequest = PatternChunkPartitionRequest.FromAuthorities(
                authorities.Canvas, authorities.Density, authorities.Route);
            var reverseRequest = Request(authorities,
                baselineRequest.TileCoordinates.Reverse().ToArray(),
                baselineRequest.PatternCoordinates.Reverse().ToArray());

            Assert.That(first.Success, Is.True, Failures(first));
            Assert.That(repeat.Success, Is.True, Failures(repeat));
            Assert.That(SectorPatternChunkPartition.SectorWidth, Is.EqualTo(48));
            Assert.That(SectorPatternChunkPartition.SectorHeight, Is.EqualTo(32));
            Assert.That(SectorPatternChunkPartition.SectorCellCount, Is.EqualTo(1536));
            Assert.That(SectorPatternChunkPartition.MicroPatternWidth, Is.EqualTo(4));
            Assert.That(SectorPatternChunkPartition.MicroPatternHeight, Is.EqualTo(4));
            Assert.That(SectorPatternChunkPartition.SectorPatternGridWidth, Is.EqualTo(12));
            Assert.That(SectorPatternChunkPartition.SectorPatternGridHeight, Is.EqualTo(8));
            Assert.That(SectorPatternChunkPartition.SectorPatternCellCount, Is.EqualTo(96));
            Assert.That(SectorPatternChunkPartition.MicroChunkWidth, Is.EqualTo(12));
            Assert.That(SectorPatternChunkPartition.MicroChunkHeight, Is.EqualTo(8));
            Assert.That(SectorPatternChunkPartition.ChunkGridWidth, Is.EqualTo(4));
            Assert.That(SectorPatternChunkPartition.ChunkGridHeight, Is.EqualTo(4));
            Assert.That(SectorPatternChunkPartition.ChunkCount, Is.EqualTo(16));
            Assert.That(SectorPatternChunkPartition.ChunkCellCount, Is.EqualTo(96));
            Assert.That(SectorPatternChunkPartition.ChunkPatternGridWidth, Is.EqualTo(3));
            Assert.That(SectorPatternChunkPartition.ChunkPatternGridHeight, Is.EqualTo(2));
            Assert.That(SectorPatternChunkPartition.ChunkPatternCellCount, Is.EqualTo(6));
            Assert.That(SectorPatternChunkPartition.ChunkRotationAllowed, Is.False);
            Assert.That(first.Partition.ChunkSlots, Has.Count.EqualTo(16));
            Assert.That(first.Partition.CoverageCount, Is.EqualTo(1536));
            Assert.That(first.Partition.PatternCoverageCount, Is.EqualTo(96));
            Assert.That(PatternChunkPartitionDigest.IsLowerHexSha256(first.InputDigest), Is.True);
            Assert.That(PatternChunkPartitionDigest.IsLowerHexSha256(first.OutputDigest), Is.True);
            Assert.That(repeat.InputDigest, Is.EqualTo(first.InputDigest));
            Assert.That(repeat.OutputDigest, Is.EqualTo(first.OutputDigest));
            TestContext.WriteLine("MAP16_04_EVIDENCE input=" + first.InputDigest +
                " output=" + first.OutputDigest +
                " witnesses=" + first.Partition.WitnessProjections.Count);

            var reverse = SectorPatternChunkPartitioner.Partition(reverseRequest);
            Assert.That(reverse.Success, Is.True, Failures(reverse));
            Assert.That(reverse.InputDigest, Is.EqualTo(first.InputDigest));
            Assert.That(reverse.OutputDigest, Is.EqualTo(first.OutputDigest));

            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var culture = SectorPatternChunkPartitioner.Partition(reverseRequest);
                Assert.That(culture.Success, Is.True, Failures(culture));
                Assert.That(culture.InputDigest, Is.EqualTo(first.InputDigest));
                Assert.That(culture.OutputDigest, Is.EqualTo(first.OutputDigest));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Test]
        public void SectorTilesPartitionIntoSixteenTwelveByEightChunksWithoutGapsOrOverlap()
        {
            var partition = AcceptedPartition();

            Assert.That(partition.TileAssignmentCount, Is.EqualTo(1536));
            Assert.That(partition.CoverageCount, Is.EqualTo(1536));
            Assert.That(partition.DuplicateTileAssignmentCount, Is.Zero);
            Assert.That(partition.MissingTileAssignmentCount, Is.Zero);
            Assert.That(partition.OutOfBoundsTileAssignmentCount, Is.Zero);
            Assert.That(partition.TileAddresses.Select(value => value.SectorCoordinate)
                .Distinct().Count(), Is.EqualTo(1536));
            Assert.That(partition.ChunkSlots.SelectMany(value => value.TileAddresses).Count(),
                Is.EqualTo(1536));
        }

        [Test]
        public void ChunkIndexUsesChunkYTimesFourPlusChunkXForEverySlot()
        {
            var partition = AcceptedPartition();

            Assert.That(partition.ChunkIndexMismatchCount, Is.Zero);
            foreach (var slot in partition.ChunkSlots)
            {
                Assert.That(slot.Index, Is.EqualTo((slot.ChunkY * 4) + slot.ChunkX));
                Assert.That(slot.Origin.X, Is.EqualTo(slot.ChunkX * 12));
                Assert.That(slot.Origin.Y, Is.EqualTo(slot.ChunkY * 8));
                Assert.That(slot.MinX, Is.EqualTo(slot.Origin.X));
                Assert.That(slot.MinY, Is.EqualTo(slot.Origin.Y));
                Assert.That(slot.MaxXExclusive, Is.EqualTo(slot.MinX + 12));
                Assert.That(slot.MaxYExclusive, Is.EqualTo(slot.MinY + 8));
            }
        }

        [Test]
        public void TileCoordinatesRoundTripThroughChunkAndLocalTileAddresses()
        {
            var partition = AcceptedPartition();

            Assert.That(partition.TileRoundTripMismatchCount, Is.Zero);
            foreach (var address in partition.TileAddresses)
            {
                Assert.That(address.ChunkCoordinate.X,
                    Is.EqualTo(address.SectorCoordinate.X / 12));
                Assert.That(address.ChunkCoordinate.Y,
                    Is.EqualTo(address.SectorCoordinate.Y / 8));
                Assert.That(address.LocalTileCoordinate.X,
                    Is.EqualTo(address.SectorCoordinate.X % 12));
                Assert.That(address.LocalTileCoordinate.Y,
                    Is.EqualTo(address.SectorCoordinate.Y % 8));
                Assert.That(address.SectorRoundTripCoordinate,
                    Is.EqualTo(address.SectorCoordinate));
            }
        }

        [Test]
        public void MicroPatternCoordinatesRoundTripThroughChunkLocalPatternAndLocalCellAddresses()
        {
            var partition = AcceptedPartition();

            Assert.That(partition.PatternAssignmentCount, Is.EqualTo(96));
            Assert.That(partition.PatternCoverageCount, Is.EqualTo(96));
            Assert.That(partition.DuplicatePatternAssignmentCount, Is.Zero);
            Assert.That(partition.MissingPatternAssignmentCount, Is.Zero);
            Assert.That(partition.OutOfBoundsPatternAssignmentCount, Is.Zero);
            Assert.That(partition.PatternRoundTripMismatchCount, Is.Zero);
            Assert.That(partition.LocalPatternCellRoundTripMismatchCount, Is.Zero);
            foreach (var address in partition.PatternAddresses)
            {
                Assert.That(address.ChunkCoordinate.X,
                    Is.EqualTo(address.SectorPatternCoordinate.X / 3));
                Assert.That(address.ChunkCoordinate.Y,
                    Is.EqualTo(address.SectorPatternCoordinate.Y / 2));
                Assert.That(address.LocalPatternCoordinate.X,
                    Is.EqualTo(address.SectorPatternCoordinate.X % 3));
                Assert.That(address.LocalPatternCoordinate.Y,
                    Is.EqualTo(address.SectorPatternCoordinate.Y % 2));
                Assert.That(address.SectorPatternRoundTripCoordinate,
                    Is.EqualTo(address.SectorPatternCoordinate));
            }
            foreach (var address in partition.TileAddresses)
            {
                Assert.That(address.PatternCoordinate.X,
                    Is.EqualTo(address.SectorCoordinate.X / 4));
                Assert.That(address.PatternCoordinate.Y,
                    Is.EqualTo(address.SectorCoordinate.Y / 4));
                Assert.That(address.LocalPatternCellCoordinate.X,
                    Is.EqualTo(address.SectorCoordinate.X % 4));
                Assert.That(address.LocalPatternCellCoordinate.Y,
                    Is.EqualTo(address.SectorCoordinate.Y % 4));
                Assert.That(address.PatternCellRoundTripCoordinate,
                    Is.EqualTo(address.SectorCoordinate));
            }
        }

        [Test]
        public void EachChunkContainsExactlyNinetySixTilesAndSixMicroPatterns()
        {
            var partition = AcceptedPartition();

            Assert.That(partition.ChunkSlots, Has.Count.EqualTo(16));
            Assert.That(partition.ChunkSlots.All(value => value.TileCount == 96), Is.True);
            Assert.That(partition.ChunkSlots.All(value => value.PatternCount == 6), Is.True);
            Assert.That(partition.ChunkSlots.All(value => value.Width == 12 && value.Height == 8),
                Is.True);
            Assert.That(partition.ChunkSlots.All(value => !value.RotationAllowed), Is.True);
        }

        [Test]
        public void RouteRecoveryWitnessCoordinatesProjectIntoChunkSlotsWithoutMutation()
        {
            var authorities = AcceptedAuthorities();
            var canvasInput = authorities.Canvas.InputDigest;
            var canvasOutput = authorities.Canvas.OutputDigest;
            var densityInput = authorities.Density.InputDigest;
            var densityOutput = authorities.Density.OutputDigest;
            var routeInput = authorities.Route.InputDigest;
            var routeOutput = authorities.Route.OutputDigest;
            var expected = authorities.Route.Witnesses.Sum(value => value.Path.Count) +
                           authorities.Route.RecoveryWitnesses.Sum(value => value.Path.Count);

            var result = SectorPatternChunkPartitioner.Partition(
                authorities.Canvas, authorities.Density, authorities.Route);

            Assert.That(result.Success, Is.True, Failures(result));
            Assert.That(result.Partition.WitnessProjections, Has.Count.EqualTo(expected));
            Assert.That(result.Partition.MissingWitnessProjectionCount, Is.Zero);
            Assert.That(result.Partition.WitnessProjections.All(value =>
                value.Address.SectorCoordinate.IsInBounds &&
                value.Address.LocalTileCoordinate.IsInBounds &&
                value.Address.ChunkIndex >= 0 && value.Address.ChunkIndex < 16), Is.True);
            Assert.That(result.Partition.SourceCanvasPlan, Is.SameAs(authorities.Canvas));
            Assert.That(result.Partition.SourceProtectionDensityReport,
                Is.SameAs(authorities.Density));
            Assert.That(result.Partition.SourceRouteRecoveryReport, Is.SameAs(authorities.Route));
            Assert.That(authorities.Canvas.InputDigest, Is.EqualTo(canvasInput));
            Assert.That(authorities.Canvas.OutputDigest, Is.EqualTo(canvasOutput));
            Assert.That(authorities.Density.InputDigest, Is.EqualTo(densityInput));
            Assert.That(authorities.Density.OutputDigest, Is.EqualTo(densityOutput));
            Assert.That(authorities.Route.InputDigest, Is.EqualTo(routeInput));
            Assert.That(authorities.Route.OutputDigest, Is.EqualTo(routeOutput));
        }

        [Test]
        public void InvalidPartitionInputsFailAtomicallyForBadCountsDuplicatesMissingAndRotation()
        {
            var authorities = AcceptedAuthorities();
            var baseline = PatternChunkPartitionRequest.FromAuthorities(
                authorities.Canvas, authorities.Density, authorities.Route);
            var missingTile = baseline.TileCoordinates.Take(1535).ToArray();
            var duplicateTile = baseline.TileCoordinates.Take(1535)
                .Concat(new[] { baseline.TileCoordinates[0] }).ToArray();
            var outOfBoundsTile = baseline.TileCoordinates.Take(1535)
                .Concat(new[] { new SectorTileCoordinate(48, 0) }).ToArray();
            var duplicatePattern = baseline.PatternCoordinates.Take(95)
                .Concat(new[] { baseline.PatternCoordinates[0] }).ToArray();

            AssertAtomicFailure(SectorPatternChunkPartitioner.Partition(null),
                PatternChunkPartitionFailureCode.MissingRequest);
            AssertAtomicFailure(SectorPatternChunkPartitioner.Partition(
                Request(authorities, missingTile, baseline.PatternCoordinates)),
                PatternChunkPartitionFailureCode.InvalidCellCount,
                PatternChunkPartitionFailureCode.MissingTileCoordinate);
            AssertAtomicFailure(SectorPatternChunkPartitioner.Partition(
                Request(authorities, duplicateTile, baseline.PatternCoordinates)),
                PatternChunkPartitionFailureCode.DuplicateTileCoordinate,
                PatternChunkPartitionFailureCode.MissingTileCoordinate);
            AssertAtomicFailure(SectorPatternChunkPartitioner.Partition(
                Request(authorities, outOfBoundsTile, baseline.PatternCoordinates)),
                PatternChunkPartitionFailureCode.OutOfBoundsTileCoordinate,
                PatternChunkPartitionFailureCode.MissingTileCoordinate);
            AssertAtomicFailure(SectorPatternChunkPartitioner.Partition(
                Request(authorities, baseline.TileCoordinates, duplicatePattern)),
                PatternChunkPartitionFailureCode.DuplicatePatternCoordinate,
                PatternChunkPartitionFailureCode.MissingPatternCoordinate);
            AssertAtomicFailure(SectorPatternChunkPartitioner.Partition(
                Request(authorities, baseline.TileCoordinates, baseline.PatternCoordinates,
                    microChunkWidth: 10)),
                PatternChunkPartitionFailureCode.InvalidDimensions,
                PatternChunkPartitionFailureCode.NonDivisibleConstants);
            AssertAtomicFailure(SectorPatternChunkPartitioner.Partition(
                Request(authorities, baseline.TileCoordinates, baseline.PatternCoordinates,
                    rotateNinetyDegrees: true)),
                PatternChunkPartitionFailureCode.RotationForbidden);
        }

        [Test]
        public void PartitionerDoesNotBuildSlicesSocketsTilemapsFilesScenesOrGameplayObjects()
        {
            var partition = AcceptedPartition();

            Assert.That(partition.LayerCopyCount, Is.Zero);
            Assert.That(partition.SliceRecordCreationCount, Is.Zero);
            Assert.That(partition.SocketDerivationCount, Is.Zero);
            Assert.That(partition.TilemapBakeCount, Is.Zero);
            Assert.That(partition.GeneratedFileWriteCount, Is.Zero);
            Assert.That(partition.TilemapMutationCount, Is.Zero);
            Assert.That(partition.SceneMutationCount, Is.Zero);
            Assert.That(partition.PrefabMutationCount, Is.Zero);
            Assert.That(partition.GameObjectMutationCount, Is.Zero);
            Assert.That(partition.GameplaySpawnCount, Is.Zero);
            Assert.That(partition.PlayerPhysicsSimulationCount, Is.Zero);
            Assert.That(partition.SectorRerenderCount, Is.Zero);
            Assert.That(partition.SectorRerollCount, Is.Zero);
            Assert.That(partition.FallbackCarveCount, Is.Zero);
            Assert.That(partition.SilentWideningCount, Is.Zero);
            Assert.That(partition.FullRegressionCount, Is.Zero);
            Assert.That(partition.ProductionSeedApprovalCount, Is.Zero);
            Assert.That(partition.RotationRequestCount, Is.Zero);
        }

        [Test]
        public void Map16HandoffKeepsMap16_05Locked()
        {
            var partition = AcceptedPartition();

            Assert.That(SectorPatternChunkPartition.DownstreamOwner,
                Is.EqualTo("MAP16_05_BUILD_96_CELL_SLICES_AND_DERIVE_SOCKETS"));
            Assert.That(SectorPatternChunkPartition.OpensDownstreamTask, Is.False);
            Assert.That(partition.SliceRecordCreationCount, Is.Zero);
            Assert.That(partition.SocketDerivationCount, Is.Zero);
        }

        private static SectorPatternChunkPartition AcceptedPartition()
        {
            var authorities = AcceptedAuthorities();
            var result = SectorPatternChunkPartitioner.Partition(
                authorities.Canvas, authorities.Density, authorities.Route);
            Assert.That(result.Success, Is.True, Failures(result));
            return result.Partition;
        }

        private static AcceptedAuthoritySet AcceptedAuthorities()
        {
            var fixture = new ReferenceFinalRouteRecoveryFixture();
            var routeRequest = fixture.AcceptedRequest();
            var routeResult = SectorFinalRouteRecoveryValidator.Validate(routeRequest);
            Assert.That(routeResult.Success, Is.True,
                string.Join(";", routeResult.Failures.Select(value => value.ToString())));
            return new AcceptedAuthoritySet(
                routeRequest.CanvasPlan,
                routeRequest.ProtectionDensityReport,
                routeResult.Report);
        }

        private static PatternChunkPartitionRequest Request(
            AcceptedAuthoritySet authorities,
            System.Collections.Generic.IEnumerable<SectorTileCoordinate> tiles,
            System.Collections.Generic.IEnumerable<MicroPatternCoordinate> patterns,
            int sectorWidth = 48,
            int sectorHeight = 32,
            int microPatternWidth = 4,
            int microPatternHeight = 4,
            int microChunkWidth = 12,
            int microChunkHeight = 8,
            bool rotateNinetyDegrees = false) => new PatternChunkPartitionRequest(
                authorities.Canvas, authorities.Density, authorities.Route,
                tiles, patterns, sectorWidth, sectorHeight,
                microPatternWidth, microPatternHeight, microChunkWidth, microChunkHeight,
                rotateNinetyDegrees);

        private static void AssertAtomicFailure(
            PatternChunkPartitionResult result,
            params PatternChunkPartitionFailureCode[] expectedCodes)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Partition, Is.Null);
            Assert.That(result.OutputDigest, Is.Empty);
            foreach (var code in expectedCodes)
                Assert.That(result.Failures.Select(value => value.Code), Does.Contain(code));
        }

        private static string Failures(PatternChunkPartitionResult result) =>
            result == null ? "NULL RESULT" : string.Join(";",
                result.Failures.Select(value => value.ToString()));

        private sealed class AcceptedAuthoritySet
        {
            public AcceptedAuthoritySet(
                SectorFinalCanvasLayerPlan canvas,
                SectorCanvasProtectionDensityReport density,
                SectorFinalRouteRecoveryReport route)
            {
                Canvas = canvas;
                Density = density;
                Route = route;
            }

            public SectorFinalCanvasLayerPlan Canvas { get; }
            public SectorCanvasProtectionDensityReport Density { get; }
            public SectorFinalRouteRecoveryReport Route { get; }
        }
    }
}
