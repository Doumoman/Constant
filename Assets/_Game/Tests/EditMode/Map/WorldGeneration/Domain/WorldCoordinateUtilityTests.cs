using System;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.Tests.WorldGeneration.Domain
{
    public sealed class WorldCoordinateUtilityTests
    {
        [Test]
        public void WorldTileBoundsAndTryCreate_AreConsistent()
        {
            Assert.That(WorldCoordinateUtility.IsValid(new WorldTileCoord(0, 0)), Is.True);
            Assert.That(WorldCoordinateUtility.IsValid(new WorldTileCoord(623, 415)), Is.True);
            Assert.That(WorldCoordinateUtility.TryCreateWorldTile(0, 0, out var minimum), Is.True);
            Assert.That(minimum, Is.EqualTo(new WorldTileCoord(0, 0)));
            Assert.That(WorldCoordinateUtility.TryCreateWorldTile(623, 415, out var maximum), Is.True);
            Assert.That(maximum, Is.EqualTo(new WorldTileCoord(623, 415)));

            Assert.That(WorldCoordinateUtility.TryCreateWorldTile(-1, 0, out var negativeX), Is.False);
            Assert.That(negativeX, Is.EqualTo(default(WorldTileCoord)));
            Assert.That(WorldCoordinateUtility.TryCreateWorldTile(0, -1, out var negativeY), Is.False);
            Assert.That(negativeY, Is.EqualTo(default(WorldTileCoord)));
            Assert.That(WorldCoordinateUtility.TryCreateWorldTile(624, 0, out var upperX), Is.False);
            Assert.That(upperX, Is.EqualTo(default(WorldTileCoord)));
            Assert.That(WorldCoordinateUtility.TryCreateWorldTile(0, 416, out var upperY), Is.False);
            Assert.That(upperY, Is.EqualTo(default(WorldTileCoord)));
        }

        [Test]
        public void SectorBoundsAndTryCreate_AreConsistent()
        {
            Assert.That(WorldCoordinateUtility.IsValid(new SectorCoord(0, 0)), Is.True);
            Assert.That(WorldCoordinateUtility.IsValid(new SectorCoord(12, 12)), Is.True);
            Assert.That(WorldCoordinateUtility.TryCreateSector(0, 0, out var minimum), Is.True);
            Assert.That(minimum, Is.EqualTo(new SectorCoord(0, 0)));
            Assert.That(WorldCoordinateUtility.TryCreateSector(12, 12, out var maximum), Is.True);
            Assert.That(maximum, Is.EqualTo(new SectorCoord(12, 12)));

            Assert.That(WorldCoordinateUtility.TryCreateSector(-1, 0, out var negativeX), Is.False);
            Assert.That(negativeX, Is.EqualTo(default(SectorCoord)));
            Assert.That(WorldCoordinateUtility.TryCreateSector(0, -1, out var negativeY), Is.False);
            Assert.That(negativeY, Is.EqualTo(default(SectorCoord)));
            Assert.That(WorldCoordinateUtility.TryCreateSector(13, 0, out var upperX), Is.False);
            Assert.That(upperX, Is.EqualTo(default(SectorCoord)));
            Assert.That(WorldCoordinateUtility.TryCreateSector(0, 13, out var upperY), Is.False);
            Assert.That(upperY, Is.EqualTo(default(SectorCoord)));
        }

        [Test]
        public void MicroChunkBoundsAndTryCreate_AreConsistent()
        {
            Assert.That(WorldCoordinateUtility.IsValid(new MicroChunkCoord(0, 0)), Is.True);
            Assert.That(WorldCoordinateUtility.IsValid(new MicroChunkCoord(3, 3)), Is.True);
            Assert.That(WorldCoordinateUtility.TryCreateMicroChunk(0, 0, out var minimum), Is.True);
            Assert.That(minimum, Is.EqualTo(new MicroChunkCoord(0, 0)));
            Assert.That(WorldCoordinateUtility.TryCreateMicroChunk(3, 3, out var maximum), Is.True);
            Assert.That(maximum, Is.EqualTo(new MicroChunkCoord(3, 3)));

            Assert.That(WorldCoordinateUtility.TryCreateMicroChunk(-1, 0, out var negativeX), Is.False);
            Assert.That(negativeX, Is.EqualTo(default(MicroChunkCoord)));
            Assert.That(WorldCoordinateUtility.TryCreateMicroChunk(0, -1, out var negativeY), Is.False);
            Assert.That(negativeY, Is.EqualTo(default(MicroChunkCoord)));
            Assert.That(WorldCoordinateUtility.TryCreateMicroChunk(4, 0, out var upperX), Is.False);
            Assert.That(upperX, Is.EqualTo(default(MicroChunkCoord)));
            Assert.That(WorldCoordinateUtility.TryCreateMicroChunk(0, 4, out var upperY), Is.False);
            Assert.That(upperY, Is.EqualTo(default(MicroChunkCoord)));
        }

        [Test]
        public void LocalTileBoundsAndTryCreate_AreConsistent()
        {
            Assert.That(WorldCoordinateUtility.IsValid(new LocalTileCoord(0, 0)), Is.True);
            Assert.That(WorldCoordinateUtility.IsValid(new LocalTileCoord(11, 7)), Is.True);
            Assert.That(WorldCoordinateUtility.TryCreateLocalTile(0, 0, out var minimum), Is.True);
            Assert.That(minimum, Is.EqualTo(new LocalTileCoord(0, 0)));
            Assert.That(WorldCoordinateUtility.TryCreateLocalTile(11, 7, out var maximum), Is.True);
            Assert.That(maximum, Is.EqualTo(new LocalTileCoord(11, 7)));

            Assert.That(WorldCoordinateUtility.TryCreateLocalTile(-1, 0, out var negativeX), Is.False);
            Assert.That(negativeX, Is.EqualTo(default(LocalTileCoord)));
            Assert.That(WorldCoordinateUtility.TryCreateLocalTile(0, -1, out var negativeY), Is.False);
            Assert.That(negativeY, Is.EqualTo(default(LocalTileCoord)));
            Assert.That(WorldCoordinateUtility.TryCreateLocalTile(12, 0, out var upperX), Is.False);
            Assert.That(upperX, Is.EqualTo(default(LocalTileCoord)));
            Assert.That(WorldCoordinateUtility.TryCreateLocalTile(0, 8, out var upperY), Is.False);
            Assert.That(upperY, Is.EqualTo(default(LocalTileCoord)));
        }

        [Test]
        public void TryToWorld_CombinesCoordinateSpaces()
        {
            var sector = new SectorCoord(1, 1);
            var microChunk = new MicroChunkCoord(1, 1);
            var localTile = new LocalTileCoord(1, 2);

            Assert.That(
                WorldCoordinateUtility.TryToWorld(sector, microChunk, localTile, out var worldTile),
                Is.True);
            Assert.That(worldTile, Is.EqualTo(new WorldTileCoord(61, 42)));
            Assert.That(WorldCoordinateUtility.IsValid(sector), Is.True);
            Assert.That(WorldCoordinateUtility.IsValid(microChunk), Is.True);
            Assert.That(WorldCoordinateUtility.IsValid(localTile), Is.True);
            Assert.That(WorldCoordinateUtility.IsValid(worldTile), Is.True);
        }

        [Test]
        public void TryToWorld_RejectsInvalidComponents()
        {
            Assert.That(
                WorldCoordinateUtility.TryToWorld(
                    new SectorCoord(13, 0),
                    new MicroChunkCoord(0, 0),
                    new LocalTileCoord(0, 0),
                    out var invalidSectorResult),
                Is.False);
            Assert.That(invalidSectorResult, Is.EqualTo(default(WorldTileCoord)));

            Assert.That(
                WorldCoordinateUtility.TryToWorld(
                    new SectorCoord(0, 0),
                    new MicroChunkCoord(4, 0),
                    new LocalTileCoord(0, 0),
                    out var invalidMicroChunkResult),
                Is.False);
            Assert.That(invalidMicroChunkResult, Is.EqualTo(default(WorldTileCoord)));

            Assert.That(
                WorldCoordinateUtility.TryToWorld(
                    new SectorCoord(0, 0),
                    new MicroChunkCoord(0, 0),
                    new LocalTileCoord(12, 0),
                    out var invalidLocalTileResult),
                Is.False);
            Assert.That(invalidLocalTileResult, Is.EqualTo(default(WorldTileCoord)));
        }

        [Test]
        public void ToWorld_RejectsInvalidComponentsWithoutClamping()
        {
            var sectorException = Assert.Throws<ArgumentOutOfRangeException>(() =>
                WorldCoordinateUtility.ToWorld(
                    new SectorCoord(13, 0),
                    new MicroChunkCoord(0, 0),
                    new LocalTileCoord(0, 0)));
            Assert.That(sectorException.ParamName, Is.EqualTo("sector"));

            var microChunkException = Assert.Throws<ArgumentOutOfRangeException>(() =>
                WorldCoordinateUtility.ToWorld(
                    new SectorCoord(0, 0),
                    new MicroChunkCoord(4, 0),
                    new LocalTileCoord(0, 0)));
            Assert.That(microChunkException.ParamName, Is.EqualTo("microChunk"));

            var localTileException = Assert.Throws<ArgumentOutOfRangeException>(() =>
                WorldCoordinateUtility.ToWorld(
                    new SectorCoord(0, 0),
                    new MicroChunkCoord(0, 0),
                    new LocalTileCoord(12, 0)));
            Assert.That(localTileException.ParamName, Is.EqualTo("localTile"));
        }

        [Test]
        public void TryFromWorld_DecomposesCoordinateSpaces()
        {
            Assert.That(
                WorldCoordinateUtility.TryFromWorld(
                    new WorldTileCoord(61, 42),
                    out var sector,
                    out var microChunk,
                    out var localTile),
                Is.True);
            Assert.That(sector, Is.EqualTo(new SectorCoord(1, 1)));
            Assert.That(microChunk, Is.EqualTo(new MicroChunkCoord(1, 1)));
            Assert.That(localTile, Is.EqualTo(new LocalTileCoord(1, 2)));
            Assert.That(WorldCoordinateUtility.IsValid(sector), Is.True);
            Assert.That(WorldCoordinateUtility.IsValid(microChunk), Is.True);
            Assert.That(WorldCoordinateUtility.IsValid(localTile), Is.True);
        }

        [Test]
        public void TryFromWorld_RejectsInvalidWorldWithoutPartialOutputs()
        {
            AssertFromWorldRejected(new WorldTileCoord(-1, 0));
            AssertFromWorldRejected(new WorldTileCoord(624, 0));
            AssertFromWorldRejected(new WorldTileCoord(0, -1));
            AssertFromWorldRejected(new WorldTileCoord(0, 416));
        }

        [Test]
        public void DirectProjectionMethods_MatchDecompositionAndRejectInvalidWorld()
        {
            var worldTile = new WorldTileCoord(61, 42);
            Assert.That(
                WorldCoordinateUtility.TryFromWorld(
                    worldTile,
                    out var sector,
                    out var microChunk,
                    out var localTile),
                Is.True);
            Assert.That(WorldCoordinateUtility.ToSector(worldTile), Is.EqualTo(sector));
            Assert.That(WorldCoordinateUtility.ToMicroChunk(worldTile), Is.EqualTo(microChunk));
            Assert.That(WorldCoordinateUtility.ToLocalTile(worldTile), Is.EqualTo(localTile));

            var invalidWorld = new WorldTileCoord(-1, 0);
            var sectorException = Assert.Throws<ArgumentOutOfRangeException>(() =>
                WorldCoordinateUtility.ToSector(invalidWorld));
            Assert.That(sectorException.ParamName, Is.EqualTo("worldTile"));
            var microChunkException = Assert.Throws<ArgumentOutOfRangeException>(() =>
                WorldCoordinateUtility.ToMicroChunk(invalidWorld));
            Assert.That(microChunkException.ParamName, Is.EqualTo("worldTile"));
            var localTileException = Assert.Throws<ArgumentOutOfRangeException>(() =>
                WorldCoordinateUtility.ToLocalTile(invalidWorld));
            Assert.That(localTileException.ParamName, Is.EqualTo("worldTile"));
        }

        private static void AssertFromWorldRejected(WorldTileCoord worldTile)
        {
            Assert.That(
                WorldCoordinateUtility.TryFromWorld(
                    worldTile,
                    out var sector,
                    out var microChunk,
                    out var localTile),
                Is.False);
            Assert.That(sector, Is.EqualTo(default(SectorCoord)));
            Assert.That(microChunk, Is.EqualTo(default(MicroChunkCoord)));
            Assert.That(localTile, Is.EqualTo(default(LocalTileCoord)));
        }
    }
}
