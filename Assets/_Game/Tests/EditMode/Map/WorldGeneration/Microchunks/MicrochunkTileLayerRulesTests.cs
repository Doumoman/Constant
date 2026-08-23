using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.Map.Tests.WorldGeneration.Microchunks
{
    [Category("MAP07_02")]
    public sealed class MicrochunkTileLayerRulesTests
    {
        public static IEnumerable<TestCaseData> AllCoordinateCases
        {
            get
            {
                for (var y = 0; y < MicrochunkConstants.HeightTiles; y++)
                for (var x = 0; x < MicrochunkConstants.WidthTiles; x++)
                    yield return new TestCaseData(x, y);
            }
        }

        public static IEnumerable<MicrochunkTileLayer> SingleLayerCases =>
            Enum.GetValues(typeof(MicrochunkTileLayer)).Cast<MicrochunkTileLayer>();

        public static IEnumerable<TestCaseData> DecorationCompatibilityCases
        {
            get
            {
                foreach (var layer in SingleLayerCases)
                {
                    yield return new TestCaseData(MicrochunkTileLayer.DecorationBack, layer);
                    yield return new TestCaseData(MicrochunkTileLayer.DecorationFront, layer);
                }
            }
        }

        public static IEnumerable<TestCaseData> AllowedMarkerPairCases => new[]
        {
            new TestCaseData(MicrochunkTileLayer.GroundSolid, MicrochunkTileLayer.Marker),
            new TestCaseData(MicrochunkTileLayer.OneWay, MicrochunkTileLayer.Marker),
            new TestCaseData(MicrochunkTileLayer.Breakable, MicrochunkTileLayer.Marker),
            new TestCaseData(MicrochunkTileLayer.Hazard, MicrochunkTileLayer.Marker)
        };

        public static IEnumerable<TestCaseData> ForbiddenPairCases => new[]
        {
            new TestCaseData(MicrochunkTileLayer.GroundSolid, MicrochunkTileLayer.Breakable),
            new TestCaseData(MicrochunkTileLayer.GroundSolid, MicrochunkTileLayer.OneWay),
            new TestCaseData(MicrochunkTileLayer.Breakable, MicrochunkTileLayer.OneWay),
            new TestCaseData(MicrochunkTileLayer.GroundSolid, MicrochunkTileLayer.Liquid),
            new TestCaseData(MicrochunkTileLayer.Breakable, MicrochunkTileLayer.Liquid),
            new TestCaseData(MicrochunkTileLayer.Hazard, MicrochunkTileLayer.GroundSolid),
            new TestCaseData(MicrochunkTileLayer.Hazard, MicrochunkTileLayer.OneWay),
            new TestCaseData(MicrochunkTileLayer.Hazard, MicrochunkTileLayer.Breakable),
            new TestCaseData(MicrochunkTileLayer.Hazard, MicrochunkTileLayer.Liquid),
            new TestCaseData(MicrochunkTileLayer.Liquid, MicrochunkTileLayer.Marker),
            new TestCaseData(MicrochunkTileLayer.Liquid, MicrochunkTileLayer.OneWay)
        };

        public static IEnumerable<string> Map0703PlusProductionSymbols => new[]
        {
            "MicrochunkTransformer",
            "MicrochunkSocketEdgeValidator",
            "MicrochunkObjectSlotValidator",
            "Microchunk96CellValidator",
            "MicrochunkReachabilityProbe",
            "MicrochunkAuthoringWindow",
            "MicrochunkCsvImporter",
            "MicrochunkCsvExporter",
            "MicrochunkPreviewReport",
            "BoundaryChunkResolver"
        };

        [TestCaseSource(nameof(AllCoordinateCases))]
        public void EmptyCellsPassAtEveryCoordinate(int x, int y)
        {
            var cell = Cell(x, y);
            var occupancy = MicrochunkTileLayerOccupancy.FromCell(cell);
            var result = MicrochunkTileLayerRules.ValidateCell(cell);

            Assert.That(occupancy.Count, Is.Zero);
            Assert.That(occupancy.OccupiedLayers, Is.Empty);
            Assert.That(result.TotalEvaluatedCells, Is.EqualTo(1));
            Assert.That(result.Success, Is.True);
            Assert.That(result.ViolationCount, Is.Zero);
        }

        [TestCaseSource(nameof(SingleLayerCases))]
        public void EachSingleLayerPasses(MicrochunkTileLayer layer)
        {
            var cell = Cell(2, 3, layer);
            var occupancy = MicrochunkTileLayerOccupancy.FromCell(cell);
            var result = MicrochunkTileLayerRules.ValidateCell(cell);

            Assert.That(occupancy.Count, Is.EqualTo(1));
            Assert.That(occupancy.IsOccupied(layer), Is.True);
            Assert.That(occupancy.GetCode(layer), Is.EqualTo(CodeFor(layer)));
            Assert.That(result.Success, Is.True);
        }

        [TestCaseSource(nameof(DecorationCompatibilityCases))]
        public void DecorationsCoexistWithEveryLogicalLayer(
            MicrochunkTileLayer decoration,
            MicrochunkTileLayer other)
        {
            var result = MicrochunkTileLayerRules.ValidateCell(Cell(3, 4, decoration, other));
            Assert.That(result.Success, Is.True);
        }

        [TestCaseSource(nameof(AllowedMarkerPairCases))]
        public void ExplicitMarkerPairsPass(MicrochunkTileLayer first, MicrochunkTileLayer second)
        {
            var result = MicrochunkTileLayerRules.ValidateCell(Cell(4, 5, first, second));
            Assert.That(result.Success, Is.True);
        }

        [TestCaseSource(nameof(ForbiddenPairCases))]
        public void ForbiddenPairsProduceOneCanonicalViolation(
            MicrochunkTileLayer first,
            MicrochunkTileLayer second)
        {
            var result = MicrochunkTileLayerRules.ValidateCell(Cell(5, 6, first, second));
            var violation = result.Violations.Single();
            var expectedFirst = (int)first < (int)second ? first : second;
            var expectedSecond = (int)first < (int)second ? second : first;

            Assert.That(result.Success, Is.False);
            Assert.That(result.ViolationCount, Is.EqualTo(1));
            Assert.That(violation.Coordinate, Is.EqualTo(new MicrochunkLocalCoord(5, 6)));
            Assert.That(violation.FirstLayer, Is.EqualTo(expectedFirst));
            Assert.That(violation.SecondLayer, Is.EqualTo(expectedSecond));
            Assert.That(violation.FirstCode, Is.EqualTo(CodeFor(expectedFirst)));
            Assert.That(violation.SecondCode, Is.EqualTo(CodeFor(expectedSecond)));
            Assert.That(violation.Reason, Is.EqualTo(MicrochunkTileLayerRules.ForbiddenPairReason));
        }

        [Test]
        public void MultipleViolationsUseStableLayerPriorityOrder()
        {
            var result = MicrochunkTileLayerRules.ValidateCell(Cell(
                1,
                1,
                MicrochunkTileLayer.Marker,
                MicrochunkTileLayer.Liquid,
                MicrochunkTileLayer.OneWay,
                MicrochunkTileLayer.GroundSolid));

            Assert.That(result.Violations.Select(value => new[] { value.FirstLayer, value.SecondLayer }), Is.EqualTo(new[]
            {
                new[] { MicrochunkTileLayer.GroundSolid, MicrochunkTileLayer.OneWay },
                new[] { MicrochunkTileLayer.GroundSolid, MicrochunkTileLayer.Liquid },
                new[] { MicrochunkTileLayer.OneWay, MicrochunkTileLayer.Liquid },
                new[] { MicrochunkTileLayer.Liquid, MicrochunkTileLayer.Marker }
            }));
        }

        [Test]
        public void DefinitionValidationAggregatesViolationsInRowMajorOrder()
        {
            var definition = Definition(new[]
            {
                Cell(11, 7, MicrochunkTileLayer.GroundSolid, MicrochunkTileLayer.Breakable),
                Cell(0, 0, MicrochunkTileLayer.Liquid, MicrochunkTileLayer.Marker)
            });

            var result = MicrochunkTileLayerRules.ValidateDefinition(definition);

            Assert.That(result.TotalEvaluatedCells, Is.EqualTo(2));
            Assert.That(result.ViolationCount, Is.EqualTo(2));
            Assert.That(result.Violations.Select(value => value.Coordinate.RowMajorIndex), Is.EqualTo(new[] { 0, 95 }));
        }

        [Test]
        public void OccupancyAndResultCollectionsAreReadOnlySnapshots()
        {
            var cell = Cell(0, 0, MicrochunkTileLayer.GroundSolid, MicrochunkTileLayer.OneWay);
            var occupancy = MicrochunkTileLayerOccupancy.FromCell(cell);
            var result = MicrochunkTileLayerRules.ValidateCell(cell);

            Assert.Throws<NotSupportedException>(() =>
                ((IList<MicrochunkTileLayer>)occupancy.OccupiedLayers).Add(MicrochunkTileLayer.Marker));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<MicrochunkTileLayerRuleViolation>)result.Violations).Clear());
        }

        [Test]
        public void NullCellIsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => MicrochunkTileLayerOccupancy.FromCell(null));
            Assert.Throws<ArgumentNullException>(() => MicrochunkTileLayerRules.ValidateCell(null));
        }

        [Test]
        public void NullDefinitionIsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => MicrochunkTileLayerRules.ValidateDefinition(null));
        }

        [TestCaseSource(nameof(Map0703PlusProductionSymbols))]
        public void Map0703PlusProductionSymbolsAreAbsent(string typeName)
        {
            var assembly = typeof(MicrochunkTileLayerRules).Assembly;
            Assert.That(assembly.GetTypes().Any(value => value.Name == typeName), Is.False, typeName);
        }

        private static MicrochunkTileCell Cell(
            int x,
            int y,
            params MicrochunkTileLayer[] occupiedLayers)
        {
            var occupied = new HashSet<MicrochunkTileLayer>(occupiedLayers);
            return new MicrochunkTileCell(
                new MicrochunkLocalCoord(x, y),
                CodeOrNone(occupied, MicrochunkTileLayer.GroundSolid),
                CodeOrNone(occupied, MicrochunkTileLayer.OneWay),
                CodeOrNone(occupied, MicrochunkTileLayer.Breakable),
                CodeOrNone(occupied, MicrochunkTileLayer.Hazard),
                CodeOrNone(occupied, MicrochunkTileLayer.Liquid),
                CodeOrNone(occupied, MicrochunkTileLayer.DecorationBack),
                CodeOrNone(occupied, MicrochunkTileLayer.DecorationFront),
                CodeOrNone(occupied, MicrochunkTileLayer.Marker));
        }

        private static string CodeOrNone(
            ISet<MicrochunkTileLayer> occupied,
            MicrochunkTileLayer layer)
        {
            return occupied.Contains(layer) ? CodeFor(layer) : "NONE";
        }

        private static string CodeFor(MicrochunkTileLayer layer)
        {
            return "CODE_" + layer.ToString().ToUpperInvariant();
        }

        private static MicrochunkDefinition Definition(IEnumerable<MicrochunkTileCell> cells)
        {
            return new MicrochunkDefinition(
                new MicrochunkId("MC_RULE_TEST"),
                "Rule Test Microchunk",
                MicrochunkConstants.WidthTiles,
                MicrochunkConstants.HeightTiles,
                MicrochunkUsageClass.Traversal,
                new[] { "BIO_TEST" },
                new[] { "MANDATORY" },
                new[] { MicrochunkTransform.R0 },
                100,
                0,
                0,
                0,
                false,
                "PREFAB_MC_RULE_TEST",
                true,
                string.Empty,
                cells,
                new MicrochunkSocketDefinition[0],
                new MicrochunkObjectSlotDefinition[0]);
        }
    }
}
