using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.Map.Tests.WorldGeneration.Microchunks
{
    [Category("MAP07_01")]
    public sealed class MicrochunkDefinitionTests
    {
        public static IEnumerable<TestCaseData> AllCoordinateCases
        {
            get
            {
                for (var y = 0; y < MicrochunkConstants.HeightTiles; y++)
                for (var x = 0; x < MicrochunkConstants.WidthTiles; x++)
                    yield return new TestCaseData(x, y, (y * MicrochunkConstants.WidthTiles) + x);
            }
        }

        public static IEnumerable<TestCaseData> InvalidCoordinateCases => new[]
        {
            new TestCaseData(-1, 0), new TestCaseData(12, 0),
            new TestCaseData(int.MinValue, 0), new TestCaseData(int.MaxValue, 0),
            new TestCaseData(0, -1), new TestCaseData(0, 8),
            new TestCaseData(0, int.MinValue), new TestCaseData(0, int.MaxValue)
        };

        public static IEnumerable<TestCaseData> RowMajorOrderingCases => new[]
        {
            new TestCaseData(0, 0, 0), new TestCaseData(11, 0, 11),
            new TestCaseData(0, 1, 12), new TestCaseData(11, 1, 23),
            new TestCaseData(0, 6, 72), new TestCaseData(11, 6, 83),
            new TestCaseData(0, 7, 84), new TestCaseData(11, 7, 95)
        };

        [Test]
        public void ConstantsAreExactTwelveEightNinetySixAndEight()
        {
            Assert.That(MicrochunkConstants.WidthTiles, Is.EqualTo(12));
            Assert.That(MicrochunkConstants.HeightTiles, Is.EqualTo(8));
            Assert.That(MicrochunkConstants.CellCount, Is.EqualTo(96));
            Assert.That(MicrochunkConstants.LayerCount, Is.EqualTo(8));
        }

        [TestCaseSource(nameof(AllCoordinateCases))]
        public void AllNinetySixLocalCoordinatesAreValid(int x, int y, int expectedIndex)
        {
            Assert.That(MicrochunkLocalCoord.TryCreate(x, y, out var parsed), Is.True);
            var direct = new MicrochunkLocalCoord(x, y);
            Assert.That(parsed, Is.EqualTo(direct));
            Assert.That(parsed.RowMajorIndex, Is.EqualTo(expectedIndex));
        }

        [TestCaseSource(nameof(InvalidCoordinateCases))]
        public void CoordinatesOutsideBoundsAreRejected(int x, int y)
        {
            Assert.That(MicrochunkLocalCoord.TryCreate(x, y, out _), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => new MicrochunkLocalCoord(x, y));
        }

        [TestCaseSource(nameof(RowMajorOrderingCases))]
        public void RowMajorIndexIsStable(int x, int y, int expected)
        {
            Assert.That(new MicrochunkLocalCoord(x, y).RowMajorIndex, Is.EqualTo(expected));
        }

        [TestCase("MC_A")]
        [TestCase("MC_GRAY_H_STRAIGHT_01")]
        [TestCase("CaseSensitive")]
        [TestCase(" MC_EXACT ")]
        public void MicrochunkIdPreservesExactSpelling(string value)
        {
            Assert.That(MicrochunkId.TryCreate(value, out var parsed), Is.True);
            Assert.That(parsed.Value, Is.EqualTo(value));
            Assert.That(parsed.ToString(), Is.EqualTo(value));
            Assert.That(new MicrochunkId(value), Is.EqualTo(parsed));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("\t\r\n")]
        public void MicrochunkIdRejectsNullEmptyAndWhitespace(string value)
        {
            Assert.That(MicrochunkId.TryCreate(value, out var parsed), Is.False);
            Assert.That(parsed.IsValid, Is.False);
            Assert.Throws<ArgumentException>(() => new MicrochunkId(value));
        }

        [Test]
        public void CompleteDefinitionAcceptsExactlyNinetySixCellsAndOrdersThem()
        {
            var definition = Definition(Cells().Reverse(), true);
            Assert.That(definition.TileCells, Has.Count.EqualTo(96));
            Assert.That(definition.TileCells.Select(value => value.Coordinate.RowMajorIndex),
                Is.EqualTo(Enumerable.Range(0, 96)));
        }

        [Test]
        public void DefinitionRejectsDuplicateTileCoordinates()
        {
            var values = Cells().ToList();
            values.Add(Cell(0, 0));
            Assert.Throws<ArgumentException>(() => Definition(values, false));
        }

        [Test]
        public void CompleteDefinitionRejectsMissingCell()
        {
            Assert.Throws<ArgumentException>(() => Definition(Cells().Take(95), true));
        }

        [Test]
        public void PartialDefinitionIsAllowedOnlyWhenCompletenessFlagIsFalse()
        {
            var partial = Definition(Cells().Take(12), false);
            Assert.That(partial.TileDataComplete, Is.False);
            Assert.That(partial.TileCells, Has.Count.EqualTo(12));
            Assert.Throws<ArgumentException>(() => Definition(Cells().Take(12), true));
        }

        [Test]
        public void TileCellPreservesAllEightLayerCodesIncludingNone()
        {
            var value = new MicrochunkTileCell(
                new MicrochunkLocalCoord(4, 3),
                "G", "NONE", "B", "H", "L", "DB", "DF", "M");
            Assert.That(new[]
            {
                value.GroundCode, value.OneWayCode, value.BreakableCode, value.HazardCode,
                value.LiquidCode, value.DecorationBackCode, value.DecorationFrontCode, value.MarkerCode
            }, Is.EqualTo(new[] { "G", "NONE", "B", "H", "L", "DB", "DF", "M" }));
            Assert.That(value.Coordinate, Is.EqualTo(new MicrochunkLocalCoord(4, 3)));
        }

        [Test]
        public void SocketPreservesStrongTypesTokensAndMinimumSafeTiles()
        {
            var value = Socket("SOCK_R", 3);
            Assert.That(value.Side, Is.EqualTo(MicrochunkSide.Right));
            Assert.That(value.TraversalKind, Is.EqualTo(MicrochunkTraversalKind.Walk));
            Assert.That(value.RouteLayer, Is.EqualTo(MicrochunkRouteLayer.Both));
            Assert.That(value.ToolRequirement, Is.EqualTo(MicrochunkToolRequirement.None));
            Assert.That(value.BandId, Is.EqualTo("BAND_H_MID"));
            Assert.That(value.Direction, Is.EqualTo("BIDIRECTIONAL"));
            Assert.That(value.EdgeSignatureId, Is.EqualTo("EDGE_H_MID_WALK"));
            Assert.That(value.MandatoryAllowed, Is.True);
            Assert.That(value.MinimumSafeTiles, Is.EqualTo(3));
        }

        [Test]
        public void ObjectSlotPreservesAnchorCategoryPoolRequiredAndRadius()
        {
            var value = Slot("RES_A", 2);
            Assert.That(value.Anchor, Is.EqualTo(new MicrochunkLocalCoord(6, 1)));
            Assert.That(value.Category, Is.EqualTo(MicrochunkSlotCategory.Resource));
            Assert.That(value.AllowedPoolId, Is.EqualTo("POOL_SLOT_COMMON_RESOURCE"));
            Assert.That(value.Required, Is.True);
            Assert.That(value.VisibleFromRoute, Is.True);
            Assert.That(value.ForbiddenRadiusTiles, Is.EqualTo(2));
            Assert.That(value.RequiredMarkerCode, Is.EqualTo("M_SLOT_RESOURCE"));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void DefinitionRejectsNegativeOwnedNumericFields(int field)
        {
            var values = new[] { 10, 2, 3, 4 };
            values[field] = -1;
            Assert.Throws<ArgumentOutOfRangeException>(() => Definition(
                Cells(), true, values[0], values[1], values[2], values[3]));
        }

        [Test]
        public void SocketRejectsNegativeMinimumSafeTiles()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Socket("SOCK_L", -1));
        }

        [Test]
        public void ObjectSlotRejectsNegativeForbiddenRadius()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Slot("RES_A", -1));
        }

        [Test]
        public void AllowedTransformsPreserveExactSetSemanticsAndCanonicalOrder()
        {
            var transforms = new[]
            {
                MicrochunkTransform.R180, MicrochunkTransform.MirrorY,
                MicrochunkTransform.R0, MicrochunkTransform.MirrorX
            };
            var definition = Definition(Cells(), true, transforms: transforms);
            Assert.That(definition.AllowedTransforms, Is.EqualTo(new[]
            {
                MicrochunkTransform.R0, MicrochunkTransform.MirrorX,
                MicrochunkTransform.MirrorY, MicrochunkTransform.R180
            }));
        }

        [Test]
        public void MetadataSocketsAndSlotsAreCanonicalAndReadOnlySnapshots()
        {
            var biomes = new List<string> { "BIO_Z", "BIO_A" };
            var roles = new List<string> { "VERTICAL", "MANDATORY" };
            var sockets = new List<MicrochunkSocketDefinition> { Socket("SOCK_R", 2), Socket("SOCK_L", 2) };
            var slots = new List<MicrochunkObjectSlotDefinition> { Slot("SLOT_Z", 1), Slot("SLOT_A", 1) };
            var definition = Definition(Cells(), true, biomes: biomes, roles: roles, sockets: sockets, slots: slots);
            biomes.Clear();
            roles.Clear();
            sockets.Clear();
            slots.Clear();
            Assert.That(definition.BiomeIds, Is.EqualTo(new[] { "BIO_A", "BIO_Z" }));
            Assert.That(definition.RouteRoles, Is.EqualTo(new[] { "MANDATORY", "VERTICAL" }));
            Assert.That(definition.Sockets.Select(value => value.SocketId), Is.EqualTo(new[] { "SOCK_L", "SOCK_R" }));
            Assert.That(definition.ObjectSlots.Select(value => value.SlotId), Is.EqualTo(new[] { "SLOT_A", "SLOT_Z" }));
        }

        [TestCase("MicrochunkPreviewReport")]
        [TestCase("TileLayerRuleMatrix")]
        [TestCase("MicrochunkAuthoringWindow")]
        [TestCase("SectorRecipeResolver")]
        [TestCase("MicrochunkSocketAndSlotEditor")]
        [TestCase("GeneratedSectorMicrochunkWriter")]
        [TestCase("MicrochunkReachabilityProbe")]
        [TestCase("MicrochunkCsvImporter")]
        [TestCase("MicrochunkCsvExporter")]
        [TestCase("BoundaryChunkResolver")]
        public void Map0702PlusProductionSymbolsAreAbsent(string typeName)
        {
            var assembly = typeof(MicrochunkDefinition).Assembly;
            Assert.That(assembly.GetTypes().Any(value => value.Name == typeName), Is.False, typeName);
        }

        private static MicrochunkDefinition Definition(
            IEnumerable<MicrochunkTileCell> cells,
            bool complete,
            int selectionWeight = 100,
            int threat = 2,
            int cognitive = 3,
            int chain = 4,
            IEnumerable<MicrochunkTransform> transforms = null,
            IEnumerable<string> biomes = null,
            IEnumerable<string> roles = null,
            IEnumerable<MicrochunkSocketDefinition> sockets = null,
            IEnumerable<MicrochunkObjectSlotDefinition> slots = null)
        {
            return new MicrochunkDefinition(
                new MicrochunkId("MC_TEST"),
                "Test Microchunk",
                12,
                8,
                MicrochunkUsageClass.Traversal,
                biomes ?? new[] { "BIO_TEST" },
                roles ?? new[] { "MANDATORY", "HORIZONTAL" },
                transforms ?? new[] { MicrochunkTransform.R0, MicrochunkTransform.MirrorX },
                selectionWeight,
                threat,
                cognitive,
                chain,
                complete,
                "PREFAB_MC_TEST",
                true,
                "notes",
                cells,
                sockets ?? new[] { Socket("SOCK_L", 2) },
                slots ?? new[] { Slot("RES_A", 2) });
        }

        private static IEnumerable<MicrochunkTileCell> Cells()
        {
            for (var y = 0; y < 8; y++)
            for (var x = 0; x < 12; x++)
                yield return Cell(x, y);
        }

        private static MicrochunkTileCell Cell(int x, int y)
        {
            return new MicrochunkTileCell(
                new MicrochunkLocalCoord(x, y),
                "G_TEST", "NONE", "NONE", "NONE", "NONE", "NONE", "NONE", "NONE");
        }

        private static MicrochunkSocketDefinition Socket(string id, int minimumSafeTiles)
        {
            return new MicrochunkSocketDefinition(
                id,
                id.EndsWith("R", StringComparison.Ordinal) ? MicrochunkSide.Right : MicrochunkSide.Left,
                "BAND_H_MID",
                MicrochunkTraversalKind.Walk,
                "BIDIRECTIONAL",
                true,
                MicrochunkToolRequirement.None,
                "EDGE_H_MID_WALK",
                MicrochunkRouteLayer.Both,
                minimumSafeTiles,
                string.Empty);
        }

        private static MicrochunkObjectSlotDefinition Slot(string id, int forbiddenRadius)
        {
            return new MicrochunkObjectSlotDefinition(
                id,
                new MicrochunkLocalCoord(6, 1),
                MicrochunkSlotCategory.Resource,
                "POOL_SLOT_COMMON_RESOURCE",
                true,
                MicrochunkObjectOrientation.None,
                true,
                forbiddenRadius,
                "M_SLOT_RESOURCE",
                string.Empty);
        }
    }
}
