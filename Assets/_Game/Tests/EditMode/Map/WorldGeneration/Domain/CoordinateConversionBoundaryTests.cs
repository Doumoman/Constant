using System;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.Tests.WorldGeneration.Domain
{
    public sealed class CoordinateConversionBoundaryTests
    {
        [Test]
        public void WorldCorners_RoundTripExactly()
        {
            AssertWorldCorner(
                new WorldTileCoord(0, 0),
                new SectorCoord(0, 0),
                new MicroChunkCoord(0, 0),
                new LocalTileCoord(0, 0));
            AssertWorldCorner(
                new WorldTileCoord(623, 0),
                new SectorCoord(12, 0),
                new MicroChunkCoord(3, 0),
                new LocalTileCoord(11, 0));
            AssertWorldCorner(
                new WorldTileCoord(0, 415),
                new SectorCoord(0, 12),
                new MicroChunkCoord(0, 3),
                new LocalTileCoord(0, 7));
            AssertWorldCorner(
                new WorldTileCoord(623, 415),
                new SectorCoord(12, 12),
                new MicroChunkCoord(3, 3),
                new LocalTileCoord(11, 7));
        }

        [Test]
        public void EverySectorAndMicroChunkCorner_RoundTripsExactly()
        {
            var visited = 0;

            for (var sectorY = 0; sectorY < WorldGenConstants.SectorRows; sectorY++)
            {
                for (var sectorX = 0; sectorX < WorldGenConstants.SectorColumns; sectorX++)
                {
                    var sector = new SectorCoord(sectorX, sectorY);

                    for (var microY = 0; microY < WorldGenConstants.MicroChunkRowsPerSector; microY++)
                    {
                        for (var microX = 0; microX < WorldGenConstants.MicroChunkColumnsPerSector; microX++)
                        {
                            var microChunk = new MicroChunkCoord(microX, microY);
                            AssertComponentCorner(sector, microChunk, new LocalTileCoord(0, 0), ref visited);
                            AssertComponentCorner(
                                sector,
                                microChunk,
                                new LocalTileCoord(WorldGenConstants.MicroChunkWidthTiles - 1, 0),
                                ref visited);
                            AssertComponentCorner(
                                sector,
                                microChunk,
                                new LocalTileCoord(0, WorldGenConstants.MicroChunkHeightTiles - 1),
                                ref visited);
                            AssertComponentCorner(
                                sector,
                                microChunk,
                                new LocalTileCoord(
                                    WorldGenConstants.MicroChunkWidthTiles - 1,
                                    WorldGenConstants.MicroChunkHeightTiles - 1),
                                ref visited);
                        }
                    }
                }
            }

            Assert.That(visited, Is.EqualTo(10816));
        }

        [Test]
        public void EveryWorldTile_RoundTripsExactly()
        {
            var visited = 0;

            for (var y = 0; y < WorldGenConstants.WorldHeightTiles; y++)
            {
                for (var x = 0; x < WorldGenConstants.WorldWidthTiles; x++)
                {
                    AssertWorldRoundTrip(new WorldTileCoord(x, y));
                    visited++;
                }
            }

            Assert.That(visited, Is.EqualTo(WorldGenConstants.WorldTileCount));
            Assert.That(visited, Is.EqualTo(259584));
        }

        [Test]
        public void TryCreate_RejectsImmediateAndIntegerExtremeOutOfRangeAxes()
        {
            AssertWorldTileTryCreateBounds();
            AssertSectorTryCreateBounds();
            AssertMicroChunkTryCreateBounds();
            AssertLocalTileTryCreateBounds();
        }

        [Test]
        public void TryToWorld_RejectsEveryInvalidComponentEdge()
        {
            var invalidSectorX = new[] { -1, WorldGenConstants.SectorColumns, int.MinValue, int.MaxValue };
            var invalidSectorY = new[] { -1, WorldGenConstants.SectorRows, int.MinValue, int.MaxValue };
            var invalidMicroX = new[] { -1, WorldGenConstants.MicroChunkColumnsPerSector, int.MinValue, int.MaxValue };
            var invalidMicroY = new[] { -1, WorldGenConstants.MicroChunkRowsPerSector, int.MinValue, int.MaxValue };
            var invalidLocalX = new[] { -1, WorldGenConstants.MicroChunkWidthTiles, int.MinValue, int.MaxValue };
            var invalidLocalY = new[] { -1, WorldGenConstants.MicroChunkHeightTiles, int.MinValue, int.MaxValue };

            foreach (var value in invalidSectorX)
            {
                AssertTryToWorldRejects(new SectorCoord(value, 0), new MicroChunkCoord(0, 0), new LocalTileCoord(0, 0));
            }

            foreach (var value in invalidSectorY)
            {
                AssertTryToWorldRejects(new SectorCoord(0, value), new MicroChunkCoord(0, 0), new LocalTileCoord(0, 0));
            }

            foreach (var value in invalidMicroX)
            {
                AssertTryToWorldRejects(new SectorCoord(0, 0), new MicroChunkCoord(value, 0), new LocalTileCoord(0, 0));
            }

            foreach (var value in invalidMicroY)
            {
                AssertTryToWorldRejects(new SectorCoord(0, 0), new MicroChunkCoord(0, value), new LocalTileCoord(0, 0));
            }

            foreach (var value in invalidLocalX)
            {
                AssertTryToWorldRejects(new SectorCoord(0, 0), new MicroChunkCoord(0, 0), new LocalTileCoord(value, 0));
            }

            foreach (var value in invalidLocalY)
            {
                AssertTryToWorldRejects(new SectorCoord(0, 0), new MicroChunkCoord(0, 0), new LocalTileCoord(0, value));
            }
        }

        [Test]
        public void TryFromWorld_RejectsEveryOutsideWorldEdgeWithoutPartialOutputs()
        {
            var invalidX = new[] { -1, WorldGenConstants.WorldWidthTiles, int.MinValue, int.MaxValue };
            var invalidY = new[] { -1, WorldGenConstants.WorldHeightTiles, int.MinValue, int.MaxValue };

            foreach (var value in invalidX)
            {
                AssertTryFromWorldRejects(new WorldTileCoord(value, 0));
            }

            foreach (var value in invalidY)
            {
                AssertTryFromWorldRejects(new WorldTileCoord(0, value));
            }
        }

        [Test]
        public void ToWorld_RejectsEachInvalidComponentWithExactParamName()
        {
            AssertToWorldRejectsSector(new SectorCoord(-1, 0));
            AssertToWorldRejectsSector(new SectorCoord(WorldGenConstants.SectorColumns, 0));
            AssertToWorldRejectsSector(new SectorCoord(0, -1));
            AssertToWorldRejectsSector(new SectorCoord(0, WorldGenConstants.SectorRows));

            AssertToWorldRejectsMicroChunk(new MicroChunkCoord(-1, 0));
            AssertToWorldRejectsMicroChunk(new MicroChunkCoord(WorldGenConstants.MicroChunkColumnsPerSector, 0));
            AssertToWorldRejectsMicroChunk(new MicroChunkCoord(0, -1));
            AssertToWorldRejectsMicroChunk(new MicroChunkCoord(0, WorldGenConstants.MicroChunkRowsPerSector));

            AssertToWorldRejectsLocalTile(new LocalTileCoord(-1, 0));
            AssertToWorldRejectsLocalTile(new LocalTileCoord(WorldGenConstants.MicroChunkWidthTiles, 0));
            AssertToWorldRejectsLocalTile(new LocalTileCoord(0, -1));
            AssertToWorldRejectsLocalTile(new LocalTileCoord(0, WorldGenConstants.MicroChunkHeightTiles));
        }

        [Test]
        public void DirectProjections_RejectEveryOutsideWorldEdgeWithExactParamName()
        {
            var outsideWorld = new[]
            {
                new WorldTileCoord(-1, 0),
                new WorldTileCoord(WorldGenConstants.WorldWidthTiles, 0),
                new WorldTileCoord(0, -1),
                new WorldTileCoord(0, WorldGenConstants.WorldHeightTiles)
            };

            foreach (var worldTile in outsideWorld)
            {
                AssertToSectorRejects(worldTile);
                AssertToMicroChunkRejects(worldTile);
                AssertToLocalTileRejects(worldTile);
            }
        }

        private static void AssertWorldCorner(
            WorldTileCoord worldTile,
            SectorCoord expectedSector,
            MicroChunkCoord expectedMicroChunk,
            LocalTileCoord expectedLocalTile)
        {
            Assert.That(WorldCoordinateUtility.IsValid(worldTile), Is.True);
            Assert.That(WorldCoordinateUtility.IsValid(expectedSector), Is.True);
            Assert.That(WorldCoordinateUtility.IsValid(expectedMicroChunk), Is.True);
            Assert.That(WorldCoordinateUtility.IsValid(expectedLocalTile), Is.True);

            Assert.That(
                WorldCoordinateUtility.TryFromWorld(worldTile, out var sector, out var microChunk, out var localTile),
                Is.True);
            Assert.That(sector, Is.EqualTo(expectedSector));
            Assert.That(microChunk, Is.EqualTo(expectedMicroChunk));
            Assert.That(localTile, Is.EqualTo(expectedLocalTile));

            Assert.That(
                WorldCoordinateUtility.TryToWorld(sector, microChunk, localTile, out var recomposed),
                Is.True);
            Assert.That(recomposed, Is.EqualTo(worldTile));
            Assert.That(WorldCoordinateUtility.ToWorld(sector, microChunk, localTile), Is.EqualTo(worldTile));
            Assert.That(WorldCoordinateUtility.ToSector(worldTile), Is.EqualTo(expectedSector));
            Assert.That(WorldCoordinateUtility.ToMicroChunk(worldTile), Is.EqualTo(expectedMicroChunk));
            Assert.That(WorldCoordinateUtility.ToLocalTile(worldTile), Is.EqualTo(expectedLocalTile));
        }

        private static void AssertComponentCorner(
            SectorCoord sector,
            MicroChunkCoord microChunk,
            LocalTileCoord localTile,
            ref int visited)
        {
            var expectedWorld = new WorldTileCoord(
                (sector.X * WorldGenConstants.SectorWidthTiles) +
                (microChunk.X * WorldGenConstants.MicroChunkWidthTiles) +
                localTile.X,
                (sector.Y * WorldGenConstants.SectorHeightTiles) +
                (microChunk.Y * WorldGenConstants.MicroChunkHeightTiles) +
                localTile.Y);

            if (!WorldCoordinateUtility.IsValid(sector) ||
                !WorldCoordinateUtility.IsValid(microChunk) ||
                !WorldCoordinateUtility.IsValid(localTile) ||
                !WorldCoordinateUtility.IsValid(expectedWorld))
            {
                Assert.Fail($"Expected valid component corner at {expectedWorld}.");
            }

            if (!WorldCoordinateUtility.TryToWorld(sector, microChunk, localTile, out var worldTile) ||
                worldTile != expectedWorld ||
                WorldCoordinateUtility.ToWorld(sector, microChunk, localTile) != expectedWorld)
            {
                Assert.Fail($"Composition mismatch at {expectedWorld}.");
            }

            if (!WorldCoordinateUtility.TryFromWorld(worldTile, out var projectedSector, out var projectedMicroChunk, out var projectedLocalTile) ||
                projectedSector != sector ||
                projectedMicroChunk != microChunk ||
                projectedLocalTile != localTile ||
                WorldCoordinateUtility.ToSector(worldTile) != sector ||
                WorldCoordinateUtility.ToMicroChunk(worldTile) != microChunk ||
                WorldCoordinateUtility.ToLocalTile(worldTile) != localTile)
            {
                Assert.Fail($"Projection mismatch at {expectedWorld}.");
            }

            visited++;
        }

        private static void AssertWorldRoundTrip(WorldTileCoord worldTile)
        {
            if (!WorldCoordinateUtility.IsValid(worldTile))
            {
                Assert.Fail($"Expected valid world tile {worldTile}.");
            }

            if (!WorldCoordinateUtility.TryFromWorld(worldTile, out var sector, out var microChunk, out var localTile))
            {
                Assert.Fail($"TryFromWorld rejected {worldTile}.");
            }

            if (!WorldCoordinateUtility.IsValid(sector) ||
                !WorldCoordinateUtility.IsValid(microChunk) ||
                !WorldCoordinateUtility.IsValid(localTile))
            {
                Assert.Fail($"TryFromWorld produced invalid components for {worldTile}.");
            }

            if (!WorldCoordinateUtility.TryToWorld(sector, microChunk, localTile, out var recomposed) ||
                recomposed != worldTile ||
                WorldCoordinateUtility.ToWorld(sector, microChunk, localTile) != worldTile)
            {
                Assert.Fail($"Composition mismatch for {worldTile}.");
            }

            if (WorldCoordinateUtility.ToSector(worldTile) != sector ||
                WorldCoordinateUtility.ToMicroChunk(worldTile) != microChunk ||
                WorldCoordinateUtility.ToLocalTile(worldTile) != localTile)
            {
                Assert.Fail($"Direct projection mismatch for {worldTile}.");
            }
        }

        private static void AssertWorldTileTryCreateBounds()
        {
            AssertTryCreateWorldTileRejects(-1, 0);
            AssertTryCreateWorldTileRejects(int.MinValue, 0);
            AssertTryCreateWorldTileRejects(WorldGenConstants.WorldWidthTiles, 0);
            AssertTryCreateWorldTileRejects(int.MaxValue, 0);
            AssertTryCreateWorldTileRejects(0, -1);
            AssertTryCreateWorldTileRejects(0, int.MinValue);
            AssertTryCreateWorldTileRejects(0, WorldGenConstants.WorldHeightTiles);
            AssertTryCreateWorldTileRejects(0, int.MaxValue);
            Assert.That(
                WorldCoordinateUtility.TryCreateWorldTile(
                    WorldGenConstants.WorldWidthTiles - 1,
                    WorldGenConstants.WorldHeightTiles - 1,
                    out var maximum),
                Is.True);
            Assert.That(
                maximum,
                Is.EqualTo(new WorldTileCoord(
                    WorldGenConstants.WorldWidthTiles - 1,
                    WorldGenConstants.WorldHeightTiles - 1)));
        }

        private static void AssertSectorTryCreateBounds()
        {
            AssertTryCreateSectorRejects(-1, 0);
            AssertTryCreateSectorRejects(int.MinValue, 0);
            AssertTryCreateSectorRejects(WorldGenConstants.SectorColumns, 0);
            AssertTryCreateSectorRejects(int.MaxValue, 0);
            AssertTryCreateSectorRejects(0, -1);
            AssertTryCreateSectorRejects(0, int.MinValue);
            AssertTryCreateSectorRejects(0, WorldGenConstants.SectorRows);
            AssertTryCreateSectorRejects(0, int.MaxValue);
            Assert.That(
                WorldCoordinateUtility.TryCreateSector(
                    WorldGenConstants.SectorColumns - 1,
                    WorldGenConstants.SectorRows - 1,
                    out var maximum),
                Is.True);
            Assert.That(
                maximum,
                Is.EqualTo(new SectorCoord(
                    WorldGenConstants.SectorColumns - 1,
                    WorldGenConstants.SectorRows - 1)));
        }

        private static void AssertMicroChunkTryCreateBounds()
        {
            AssertTryCreateMicroChunkRejects(-1, 0);
            AssertTryCreateMicroChunkRejects(int.MinValue, 0);
            AssertTryCreateMicroChunkRejects(WorldGenConstants.MicroChunkColumnsPerSector, 0);
            AssertTryCreateMicroChunkRejects(int.MaxValue, 0);
            AssertTryCreateMicroChunkRejects(0, -1);
            AssertTryCreateMicroChunkRejects(0, int.MinValue);
            AssertTryCreateMicroChunkRejects(0, WorldGenConstants.MicroChunkRowsPerSector);
            AssertTryCreateMicroChunkRejects(0, int.MaxValue);
            Assert.That(
                WorldCoordinateUtility.TryCreateMicroChunk(
                    WorldGenConstants.MicroChunkColumnsPerSector - 1,
                    WorldGenConstants.MicroChunkRowsPerSector - 1,
                    out var maximum),
                Is.True);
            Assert.That(
                maximum,
                Is.EqualTo(new MicroChunkCoord(
                    WorldGenConstants.MicroChunkColumnsPerSector - 1,
                    WorldGenConstants.MicroChunkRowsPerSector - 1)));
        }

        private static void AssertLocalTileTryCreateBounds()
        {
            AssertTryCreateLocalTileRejects(-1, 0);
            AssertTryCreateLocalTileRejects(int.MinValue, 0);
            AssertTryCreateLocalTileRejects(WorldGenConstants.MicroChunkWidthTiles, 0);
            AssertTryCreateLocalTileRejects(int.MaxValue, 0);
            AssertTryCreateLocalTileRejects(0, -1);
            AssertTryCreateLocalTileRejects(0, int.MinValue);
            AssertTryCreateLocalTileRejects(0, WorldGenConstants.MicroChunkHeightTiles);
            AssertTryCreateLocalTileRejects(0, int.MaxValue);
            Assert.That(
                WorldCoordinateUtility.TryCreateLocalTile(
                    WorldGenConstants.MicroChunkWidthTiles - 1,
                    WorldGenConstants.MicroChunkHeightTiles - 1,
                    out var maximum),
                Is.True);
            Assert.That(
                maximum,
                Is.EqualTo(new LocalTileCoord(
                    WorldGenConstants.MicroChunkWidthTiles - 1,
                    WorldGenConstants.MicroChunkHeightTiles - 1)));
        }

        private static void AssertTryCreateWorldTileRejects(int x, int y)
        {
            Assert.That(WorldCoordinateUtility.IsValid(new WorldTileCoord(x, y)), Is.False);
            Assert.That(WorldCoordinateUtility.TryCreateWorldTile(x, y, out var coordinate), Is.False);
            Assert.That(coordinate, Is.EqualTo(default(WorldTileCoord)));
        }

        private static void AssertTryCreateSectorRejects(int x, int y)
        {
            Assert.That(WorldCoordinateUtility.IsValid(new SectorCoord(x, y)), Is.False);
            Assert.That(WorldCoordinateUtility.TryCreateSector(x, y, out var coordinate), Is.False);
            Assert.That(coordinate, Is.EqualTo(default(SectorCoord)));
        }

        private static void AssertTryCreateMicroChunkRejects(int x, int y)
        {
            Assert.That(WorldCoordinateUtility.IsValid(new MicroChunkCoord(x, y)), Is.False);
            Assert.That(WorldCoordinateUtility.TryCreateMicroChunk(x, y, out var coordinate), Is.False);
            Assert.That(coordinate, Is.EqualTo(default(MicroChunkCoord)));
        }

        private static void AssertTryCreateLocalTileRejects(int x, int y)
        {
            Assert.That(WorldCoordinateUtility.IsValid(new LocalTileCoord(x, y)), Is.False);
            Assert.That(WorldCoordinateUtility.TryCreateLocalTile(x, y, out var coordinate), Is.False);
            Assert.That(coordinate, Is.EqualTo(default(LocalTileCoord)));
        }

        private static void AssertTryToWorldRejects(
            SectorCoord sector,
            MicroChunkCoord microChunk,
            LocalTileCoord localTile)
        {
            Assert.That(
                WorldCoordinateUtility.TryToWorld(sector, microChunk, localTile, out var worldTile),
                Is.False);
            Assert.That(worldTile, Is.EqualTo(default(WorldTileCoord)));
        }

        private static void AssertTryFromWorldRejects(WorldTileCoord worldTile)
        {
            Assert.That(
                WorldCoordinateUtility.TryFromWorld(worldTile, out var sector, out var microChunk, out var localTile),
                Is.False);
            Assert.That(sector, Is.EqualTo(default(SectorCoord)));
            Assert.That(microChunk, Is.EqualTo(default(MicroChunkCoord)));
            Assert.That(localTile, Is.EqualTo(default(LocalTileCoord)));
        }

        private static void AssertToWorldRejectsSector(SectorCoord invalidSector)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                WorldCoordinateUtility.ToWorld(
                    invalidSector,
                    new MicroChunkCoord(0, 0),
                    new LocalTileCoord(0, 0)));
            Assert.That(exception.ParamName, Is.EqualTo("sector"));
        }

        private static void AssertToWorldRejectsMicroChunk(MicroChunkCoord invalidMicroChunk)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                WorldCoordinateUtility.ToWorld(
                    new SectorCoord(0, 0),
                    invalidMicroChunk,
                    new LocalTileCoord(0, 0)));
            Assert.That(exception.ParamName, Is.EqualTo("microChunk"));
        }

        private static void AssertToWorldRejectsLocalTile(LocalTileCoord invalidLocalTile)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                WorldCoordinateUtility.ToWorld(
                    new SectorCoord(0, 0),
                    new MicroChunkCoord(0, 0),
                    invalidLocalTile));
            Assert.That(exception.ParamName, Is.EqualTo("localTile"));
        }

        private static void AssertToSectorRejects(WorldTileCoord invalidWorldTile)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                WorldCoordinateUtility.ToSector(invalidWorldTile));
            Assert.That(exception.ParamName, Is.EqualTo("worldTile"));
        }

        private static void AssertToMicroChunkRejects(WorldTileCoord invalidWorldTile)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                WorldCoordinateUtility.ToMicroChunk(invalidWorldTile));
            Assert.That(exception.ParamName, Is.EqualTo("worldTile"));
        }

        private static void AssertToLocalTileRejects(WorldTileCoord invalidWorldTile)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                WorldCoordinateUtility.ToLocalTile(invalidWorldTile));
            Assert.That(exception.ParamName, Is.EqualTo("worldTile"));
        }
    }
}
