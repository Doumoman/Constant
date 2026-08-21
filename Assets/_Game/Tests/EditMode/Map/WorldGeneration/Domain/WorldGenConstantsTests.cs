using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.Tests.WorldGeneration.Domain
{
    public sealed class WorldGenConstantsTests
    {
        [Test]
        public void LockedWorldDimensions_AreExact()
        {
            Assert.That(WorldGenConstants.WorldWidthTiles, Is.EqualTo(624));
            Assert.That(WorldGenConstants.WorldHeightTiles, Is.EqualTo(416));
        }

        [Test]
        public void LockedSectorDimensions_AreExact()
        {
            Assert.That(WorldGenConstants.SectorWidthTiles, Is.EqualTo(48));
            Assert.That(WorldGenConstants.SectorHeightTiles, Is.EqualTo(32));
        }

        [Test]
        public void LockedMicroChunkDimensions_AreExact()
        {
            Assert.That(WorldGenConstants.MicroChunkWidthTiles, Is.EqualTo(12));
            Assert.That(WorldGenConstants.MicroChunkHeightTiles, Is.EqualTo(8));
        }

        [Test]
        public void DerivedSectorGrid_IsExact()
        {
            Assert.That(WorldGenConstants.SectorColumns, Is.EqualTo(13));
            Assert.That(WorldGenConstants.SectorRows, Is.EqualTo(13));
            Assert.That(WorldGenConstants.SectorCount, Is.EqualTo(169));
        }

        [Test]
        public void DerivedMicroChunkGridAndTileCounts_AreExact()
        {
            Assert.That(WorldGenConstants.MicroChunkColumnsPerSector, Is.EqualTo(4));
            Assert.That(WorldGenConstants.MicroChunkRowsPerSector, Is.EqualTo(4));
            Assert.That(WorldGenConstants.MicroChunksPerSector, Is.EqualTo(16));
            Assert.That(WorldGenConstants.TilesPerMicroChunk, Is.EqualTo(96));
            Assert.That(WorldGenConstants.TilesPerSector, Is.EqualTo(1536));
            Assert.That(WorldGenConstants.WorldTileCount, Is.EqualTo(259584));
        }

        [Test]
        public void Dimensions_ReconstructParentSpacesExactly()
        {
            Assert.That(
                WorldGenConstants.SectorWidthTiles * WorldGenConstants.SectorColumns,
                Is.EqualTo(WorldGenConstants.WorldWidthTiles));
            Assert.That(
                WorldGenConstants.SectorHeightTiles * WorldGenConstants.SectorRows,
                Is.EqualTo(WorldGenConstants.WorldHeightTiles));
            Assert.That(
                WorldGenConstants.MicroChunkWidthTiles * WorldGenConstants.MicroChunkColumnsPerSector,
                Is.EqualTo(WorldGenConstants.SectorWidthTiles));
            Assert.That(
                WorldGenConstants.MicroChunkHeightTiles * WorldGenConstants.MicroChunkRowsPerSector,
                Is.EqualTo(WorldGenConstants.SectorHeightTiles));

            var publicStaticFields = typeof(WorldGenConstants)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

            Assert.That(publicStaticFields, Has.Length.EqualTo(15));
            Assert.That(publicStaticFields.All(field => field.FieldType == typeof(int)), Is.True);
            Assert.That(publicStaticFields.All(field => field.IsLiteral), Is.True);
            Assert.That(
                typeof(WorldGenConstants).GetProperties(
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly),
                Is.Empty);
        }
    }
}
