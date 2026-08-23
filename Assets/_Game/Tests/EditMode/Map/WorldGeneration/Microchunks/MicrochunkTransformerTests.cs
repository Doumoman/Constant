using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.Map.Tests.WorldGeneration.Microchunks
{
    [Category("MAP07_03")]
    public sealed class MicrochunkTransformerTests
    {
        public static IEnumerable<TestCaseData> AllCoordinateTransformCases
        {
            get
            {
                foreach (MicrochunkTransform transform in Enum.GetValues(typeof(MicrochunkTransform)))
                {
                    for (var y = 0; y < MicrochunkConstants.HeightTiles; y++)
                    for (var x = 0; x < MicrochunkConstants.WidthTiles; x++)
                    {
                        var expectedX = transform == MicrochunkTransform.MirrorX ||
                                        transform == MicrochunkTransform.R180
                            ? MicrochunkConstants.WidthTiles - 1 - x
                            : x;
                        var expectedY = transform == MicrochunkTransform.MirrorY ||
                                        transform == MicrochunkTransform.R180
                            ? MicrochunkConstants.HeightTiles - 1 - y
                            : y;
                        yield return new TestCaseData(transform, x, y, expectedX, expectedY);
                    }
                }
            }
        }

        public static IEnumerable<TestCaseData> SideTransformCases
        {
            get
            {
                foreach (MicrochunkTransform transform in Enum.GetValues(typeof(MicrochunkTransform)))
                foreach (MicrochunkSide side in Enum.GetValues(typeof(MicrochunkSide)))
                    yield return new TestCaseData(transform, side, ExpectedSide(side, transform));
            }
        }

        public static IEnumerable<TestCaseData> OrientationTransformCases
        {
            get
            {
                foreach (MicrochunkTransform transform in Enum.GetValues(typeof(MicrochunkTransform)))
                foreach (MicrochunkObjectOrientation orientation in Enum.GetValues(typeof(MicrochunkObjectOrientation)))
                    yield return new TestCaseData(
                        transform,
                        orientation,
                        ExpectedOrientation(orientation, transform));
            }
        }

        public static IEnumerable<TestCaseData> TransformTokenCases => new[]
        {
            new TestCaseData("R0", MicrochunkTransform.R0),
            new TestCaseData("MIRROR_X", MicrochunkTransform.MirrorX),
            new TestCaseData("MIRROR_Y", MicrochunkTransform.MirrorY),
            new TestCaseData("R180", MicrochunkTransform.R180)
        };

        public static IEnumerable<TestCaseData> OrientationTokenCases => new[]
        {
            new TestCaseData("NONE", MicrochunkObjectOrientation.None),
            new TestCaseData("L", MicrochunkObjectOrientation.Left),
            new TestCaseData("R", MicrochunkObjectOrientation.Right),
            new TestCaseData("U", MicrochunkObjectOrientation.Up),
            new TestCaseData("D", MicrochunkObjectOrientation.Down)
        };

        public static IEnumerable<string> ForbiddenTransformTokens => new[]
        {
            null, string.Empty, " ", "R90", "R270", "ROTATE_90", "mirror_x", "UNKNOWN"
        };

        public static IEnumerable<string> Map0704PlusProductionSymbols => new[]
        {
            "GeneratedSectorMicrochunksWriter",
            "MicrochunkObjectSlotValidator",
            "Microchunk96CellValidator",
            "MicrochunkReachabilityProbe",
            "MicrochunkAuthoringWindow",
            "MicrochunkCsvImporter",
            "MicrochunkCsvExporter",
            "MicrochunkPreviewReport",
            "BoundaryChunkResolver",
            "SectorRecipeResolver",
            "SectorAssembly"
        };

        [TestCaseSource(nameof(AllCoordinateTransformCases))]
        public void CoordinateProjectionUsesExactTwelveByEightFormula(
            MicrochunkTransform transform,
            int x,
            int y,
            int expectedX,
            int expectedY)
        {
            var transformed = MicrochunkTransformUtility.TransformCoordinate(
                new MicrochunkLocalCoord(x, y),
                transform);

            Assert.That(transformed, Is.EqualTo(new MicrochunkLocalCoord(expectedX, expectedY)));
        }

        [TestCaseSource(nameof(SideTransformCases))]
        public void SocketSideProjectionIsExact(
            MicrochunkTransform transform,
            MicrochunkSide side,
            MicrochunkSide expected)
        {
            Assert.That(MicrochunkTransformUtility.TransformSide(side, transform), Is.EqualTo(expected));
        }

        [TestCaseSource(nameof(OrientationTransformCases))]
        public void ObjectOrientationProjectionIsExact(
            MicrochunkTransform transform,
            MicrochunkObjectOrientation orientation,
            MicrochunkObjectOrientation expected)
        {
            Assert.That(
                MicrochunkTransformUtility.TransformOrientation(orientation, transform),
                Is.EqualTo(expected));
        }

        [TestCaseSource(nameof(TransformTokenCases))]
        public void TransformTokensParseAndRoundTripExactly(
            string token,
            MicrochunkTransform expected)
        {
            Assert.That(MicrochunkTransformUtility.TryParseTransformToken(token, out var parsed), Is.True);
            Assert.That(parsed, Is.EqualTo(expected));
            Assert.That(MicrochunkTransformUtility.ToTransformToken(parsed), Is.EqualTo(token));
        }

        [TestCaseSource(nameof(OrientationTokenCases))]
        public void OrientationTokensParseAndRoundTripExactly(
            string token,
            MicrochunkObjectOrientation expected)
        {
            Assert.That(MicrochunkTransformUtility.TryParseOrientationToken(token, out var parsed), Is.True);
            Assert.That(parsed, Is.EqualTo(expected));
            Assert.That(MicrochunkTransformUtility.ToOrientationToken(parsed), Is.EqualTo(token));
        }

        [TestCaseSource(nameof(ForbiddenTransformTokens))]
        public void UnsupportedTransformTokensAreRejected(string token)
        {
            Assert.That(MicrochunkTransformUtility.TryParseTransformToken(token, out _), Is.False);
        }

        [TestCase(MicrochunkTransform.R0)]
        [TestCase(MicrochunkTransform.MirrorX)]
        [TestCase(MicrochunkTransform.MirrorY)]
        [TestCase(MicrochunkTransform.R180)]
        public void CompleteDefinitionsPreserveNinetySixUniqueCanonicalCells(
            MicrochunkTransform transform)
        {
            var source = CompleteDefinition();
            var result = MicrochunkTransformer.Transform(source, transform);
            var transformed = result.Definition;

            Assert.That(transformed.TileDataComplete, Is.True);
            Assert.That(transformed.TileCells, Has.Count.EqualTo(96));
            Assert.That(
                transformed.TileCells.Select(value => value.Coordinate).Distinct().Count(),
                Is.EqualTo(96));
            Assert.That(
                transformed.TileCells.Select(value => value.Coordinate.RowMajorIndex),
                Is.EqualTo(Enumerable.Range(0, 96)));
            Assert.That(result.TileCellCount, Is.EqualTo(96));
            Assert.That(result.SocketCount, Is.EqualTo(4));
            Assert.That(result.ObjectSlotCount, Is.EqualTo(5));
        }

        [TestCase(MicrochunkTransform.R0)]
        [TestCase(MicrochunkTransform.MirrorX)]
        [TestCase(MicrochunkTransform.MirrorY)]
        [TestCase(MicrochunkTransform.R180)]
        public void EveryCellMovesWithItsEightCodes(MicrochunkTransform transform)
        {
            var source = CompleteDefinition();
            var transformed = MicrochunkTransformer.Transform(source, transform).Definition;

            foreach (var sourceCell in source.TileCells)
            {
                var expectedCoordinate = MicrochunkTransformUtility.TransformCoordinate(
                    sourceCell.Coordinate,
                    transform);
                var actual = transformed.TileCells.Single(value => value.Coordinate == expectedCoordinate);
                AssertCellCodesEqual(sourceCell, actual);
            }
        }

        [TestCase(MicrochunkTransform.R0)]
        [TestCase(MicrochunkTransform.MirrorX)]
        [TestCase(MicrochunkTransform.MirrorY)]
        [TestCase(MicrochunkTransform.R180)]
        public void SocketSidesTransformAndBandsPreserveByDefault(MicrochunkTransform transform)
        {
            var source = CompleteDefinition();
            var transformed = MicrochunkTransformer.Transform(source, transform).Definition;

            foreach (var sourceSocket in source.Sockets)
            {
                var actual = transformed.Sockets.Single(value => value.SocketId == sourceSocket.SocketId);
                Assert.That(actual.Side, Is.EqualTo(ExpectedSide(sourceSocket.Side, transform)));
                Assert.That(actual.BandId, Is.EqualTo(sourceSocket.BandId));
                AssertSocketMetadataEqual(sourceSocket, actual);
            }
        }

        [TestCase(MicrochunkTransform.R0)]
        [TestCase(MicrochunkTransform.MirrorX)]
        [TestCase(MicrochunkTransform.MirrorY)]
        [TestCase(MicrochunkTransform.R180)]
        public void SlotAnchorsAndOrientationsTransformTogether(MicrochunkTransform transform)
        {
            var source = CompleteDefinition();
            var transformed = MicrochunkTransformer.Transform(source, transform).Definition;

            foreach (var sourceSlot in source.ObjectSlots)
            {
                var actual = transformed.ObjectSlots.Single(value => value.SlotId == sourceSlot.SlotId);
                Assert.That(
                    actual.Anchor,
                    Is.EqualTo(MicrochunkTransformUtility.TransformCoordinate(sourceSlot.Anchor, transform)));
                Assert.That(actual.Orientation, Is.EqualTo(ExpectedOrientation(sourceSlot.Orientation, transform)));
                AssertSlotMetadataEqual(sourceSlot, actual);
            }
        }

        [Test]
        public void R0ReconstructsAnEquivalentButDistinctDefinition()
        {
            var source = CompleteDefinition();
            var result = MicrochunkTransformer.Transform(source, MicrochunkTransform.R0);

            Assert.That(result.SourceDefinition, Is.SameAs(source));
            Assert.That(result.Definition, Is.Not.SameAs(source));
            Assert.That(result.TransformedDefinition, Is.SameAs(result.Definition));
            AssertEquivalent(source, result.Definition);
        }

        [TestCase(MicrochunkTransform.R0)]
        [TestCase(MicrochunkTransform.MirrorX)]
        [TestCase(MicrochunkTransform.MirrorY)]
        [TestCase(MicrochunkTransform.R180)]
        public void PartialDefinitionsRemainPartialAndDuplicateFree(MicrochunkTransform transform)
        {
            var source = Definition(Cells().Where(value => value.Coordinate.X < 3), false);
            var transformed = MicrochunkTransformer.Transform(source, transform).Definition;

            Assert.That(transformed.TileDataComplete, Is.False);
            Assert.That(transformed.TileCells, Has.Count.EqualTo(24));
            Assert.That(
                transformed.TileCells.Select(value => value.Coordinate).Distinct().Count(),
                Is.EqualTo(24));
            Assert.That(
                transformed.TileCells.Select(value => value.Coordinate.RowMajorIndex),
                Is.Ordered.Ascending);
        }

        [TestCase(MicrochunkTransform.MirrorX)]
        [TestCase(MicrochunkTransform.MirrorY)]
        [TestCase(MicrochunkTransform.R180)]
        public void MirrorTransformsAreInvolutions(MicrochunkTransform transform)
        {
            var source = CompleteDefinition();
            var once = MicrochunkTransformer.Transform(source, transform).Definition;
            var twice = MicrochunkTransformer.Transform(once, transform).Definition;

            AssertEquivalent(source, twice);
        }

        [Test]
        public void MirrorXThenMirrorYEqualsR180()
        {
            var source = CompleteDefinition();
            var mirrorX = MicrochunkTransformer.Transform(source, MicrochunkTransform.MirrorX).Definition;
            var composed = MicrochunkTransformer.Transform(mirrorX, MicrochunkTransform.MirrorY).Definition;
            var rotated = MicrochunkTransformer.Transform(source, MicrochunkTransform.R180).Definition;

            AssertEquivalent(rotated, composed);
        }

        [Test]
        public void TileCodeRemapperRunsInCanonicalCellAndLayerOrderIncludingMarker()
        {
            var calls = new List<string>();
            var options = new MicrochunkTransformOptions(
                tileCodeRemapper: (code, layer, transform) =>
                {
                    calls.Add(layer + ":" + code + ":" + transform);
                    return code + "_X";
                });

            var transformed = MicrochunkTransformer.Transform(
                CompleteDefinition(),
                MicrochunkTransform.MirrorX,
                options).Definition;

            Assert.That(calls, Has.Count.EqualTo(96 * MicrochunkConstants.LayerCount));
            Assert.That(calls[0], Is.EqualTo("GroundSolid:G_0_0:MirrorX"));
            Assert.That(calls[7], Is.EqualTo("Marker:M_0_0:MirrorX"));
            Assert.That(calls[8], Is.EqualTo("GroundSolid:G_1_0:MirrorX"));
            var movedFirst = transformed.TileCells.Single(value => value.Coordinate == new MicrochunkLocalCoord(11, 0));
            Assert.That(movedFirst.GroundCode, Is.EqualTo("G_0_0_X"));
            Assert.That(movedFirst.MarkerCode, Is.EqualTo("M_0_0_X"));
        }

        [Test]
        public void SocketBandRemapperReceivesOriginalAndTransformedSidesDeterministically()
        {
            var calls = new List<string>();
            var options = new MicrochunkTransformOptions(
                socketBandRemapper: (original, transformed, band, transform) =>
                {
                    calls.Add(original + ">" + transformed + ":" + band + ":" + transform);
                    return band + "_REMAPPED";
                });

            var transformedDefinition = MicrochunkTransformer.Transform(
                CompleteDefinition(),
                MicrochunkTransform.R180,
                options).Definition;

            Assert.That(calls, Is.EqualTo(new[]
            {
                "Down>Up:BAND_D:R180",
                "Left>Right:BAND_L:R180",
                "Right>Left:BAND_R:R180",
                "Up>Down:BAND_U:R180"
            }));
            Assert.That(transformedDefinition.Sockets.All(value => value.BandId.EndsWith("_REMAPPED", StringComparison.Ordinal)), Is.True);
        }

        [Test]
        public void IdProjectionIsExplicitAndDefaultPreservesIdentity()
        {
            var source = CompleteDefinition();
            var preserved = MicrochunkTransformer.Transform(source, MicrochunkTransform.MirrorY).Definition;
            var options = new MicrochunkTransformOptions(
                idProjector: (id, transform) => new MicrochunkId(id.Value + "_" + transform));
            var projected = MicrochunkTransformer.Transform(
                source,
                MicrochunkTransform.MirrorY,
                options).Definition;

            Assert.That(preserved.Id, Is.EqualTo(source.Id));
            Assert.That(projected.Id.Value, Is.EqualTo("MC_TRANSFORM_TEST_MirrorY"));
        }

        [Test]
        public void DefaultOptionsPreserveExactNoneCodesAndBands()
        {
            var transformed = MicrochunkTransformer.Transform(
                CompleteDefinition(),
                MicrochunkTransform.R180).Definition;

            Assert.That(transformed.TileCells.Select(value => value.LiquidCode), Is.All.EqualTo("NONE"));
            Assert.That(transformed.Sockets.Select(value => value.BandId),
                Is.EqualTo(new[] { "BAND_D", "BAND_L", "BAND_R", "BAND_U" }));
        }

        [TestCase(MicrochunkTransform.R0)]
        [TestCase(MicrochunkTransform.MirrorX)]
        [TestCase(MicrochunkTransform.MirrorY)]
        [TestCase(MicrochunkTransform.R180)]
        public void TileLayerRuleOutcomeIsInvariantUnderDefaultTransform(MicrochunkTransform transform)
        {
            var valid = Definition(new[]
            {
                Cell(1, 1, "GROUND", "NONE", "NONE", "NONE", "NONE", "NONE", "NONE", "MARKER")
            }, false);
            var invalid = Definition(new[]
            {
                Cell(2, 2, "GROUND", "NONE", "NONE", "NONE", "LIQUID", "NONE", "NONE", "NONE")
            }, false);

            var validBefore = MicrochunkTileLayerRules.ValidateDefinition(valid);
            var invalidBefore = MicrochunkTileLayerRules.ValidateDefinition(invalid);
            var validAfter = MicrochunkTileLayerRules.ValidateDefinition(
                MicrochunkTransformer.Transform(valid, transform).Definition);
            var invalidAfter = MicrochunkTileLayerRules.ValidateDefinition(
                MicrochunkTransformer.Transform(invalid, transform).Definition);

            Assert.That(validAfter.Success, Is.EqualTo(validBefore.Success));
            Assert.That(validAfter.ViolationCount, Is.EqualTo(validBefore.ViolationCount));
            Assert.That(invalidAfter.Success, Is.EqualTo(invalidBefore.Success));
            Assert.That(invalidAfter.ViolationCount, Is.EqualTo(invalidBefore.ViolationCount));
        }

        [Test]
        public void NullAndUndefinedInputsAreRejected()
        {
            Assert.Throws<ArgumentNullException>(() =>
                MicrochunkTransformer.Transform(null, MicrochunkTransform.R0));
            Assert.Throws<ArgumentNullException>(() =>
                MicrochunkTransformer.Transform(CompleteDefinition(), MicrochunkTransform.R0, null));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                MicrochunkTransformer.Transform(CompleteDefinition(), (MicrochunkTransform)999));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                MicrochunkTransformUtility.TransformSide((MicrochunkSide)999, MicrochunkTransform.R0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                MicrochunkTransformUtility.TransformOrientation((MicrochunkObjectOrientation)999, MicrochunkTransform.R0));
        }

        [Test]
        public void InvalidRemapperOutputsAreRejectedByImmutableConstructionBoundary()
        {
            var invalidCode = new MicrochunkTransformOptions(
                tileCodeRemapper: (code, layer, transform) => null);
            var invalidBand = new MicrochunkTransformOptions(
                socketBandRemapper: (original, transformed, band, transform) => " ");
            var invalidId = new MicrochunkTransformOptions(
                idProjector: (id, transform) => default);

            Assert.Throws<InvalidOperationException>(() =>
                MicrochunkTransformer.Transform(CompleteDefinition(), MicrochunkTransform.R0, invalidCode));
            Assert.Throws<InvalidOperationException>(() =>
                MicrochunkTransformer.Transform(CompleteDefinition(), MicrochunkTransform.R0, invalidBand));
            Assert.Throws<InvalidOperationException>(() =>
                MicrochunkTransformer.Transform(CompleteDefinition(), MicrochunkTransform.R0, invalidId));
        }

        [TestCaseSource(nameof(Map0704PlusProductionSymbols))]
        public void Map0704PlusProductionSymbolsAreAbsent(string typeName)
        {
            var assembly = typeof(MicrochunkTransformer).Assembly;
            Assert.That(assembly.GetTypes().Any(value => value.Name == typeName), Is.False, typeName);
        }

        private static MicrochunkDefinition CompleteDefinition()
        {
            return Definition(Cells(), true);
        }

        private static MicrochunkDefinition Definition(
            IEnumerable<MicrochunkTileCell> cells,
            bool complete)
        {
            return new MicrochunkDefinition(
                new MicrochunkId("MC_TRANSFORM_TEST"),
                "Transform Test",
                MicrochunkConstants.WidthTiles,
                MicrochunkConstants.HeightTiles,
                MicrochunkUsageClass.Traversal,
                new[] { "BIO_B", "BIO_A" },
                new[] { "VERTICAL", "MANDATORY" },
                new[]
                {
                    MicrochunkTransform.R180,
                    MicrochunkTransform.MirrorY,
                    MicrochunkTransform.R0,
                    MicrochunkTransform.MirrorX
                },
                100,
                2,
                3,
                4,
                complete,
                "PREFAB_MC_TRANSFORM_TEST",
                true,
                "transform notes",
                cells,
                Sockets(),
                Slots());
        }

        private static IEnumerable<MicrochunkTileCell> Cells()
        {
            for (var y = 0; y < MicrochunkConstants.HeightTiles; y++)
            for (var x = 0; x < MicrochunkConstants.WidthTiles; x++)
            {
                yield return Cell(
                    x,
                    y,
                    "G_" + x + "_" + y,
                    "O_" + x + "_" + y,
                    "B_" + x + "_" + y,
                    "H_" + x + "_" + y,
                    "NONE",
                    "DB_" + x + "_" + y,
                    "DF_" + x + "_" + y,
                    "M_" + x + "_" + y);
            }
        }

        private static MicrochunkTileCell Cell(
            int x,
            int y,
            string ground,
            string oneWay,
            string breakable,
            string hazard,
            string liquid,
            string decorationBack,
            string decorationFront,
            string marker)
        {
            return new MicrochunkTileCell(
                new MicrochunkLocalCoord(x, y),
                ground,
                oneWay,
                breakable,
                hazard,
                liquid,
                decorationBack,
                decorationFront,
                marker);
        }

        private static IEnumerable<MicrochunkSocketDefinition> Sockets()
        {
            return new[]
            {
                Socket("SOCK_U", MicrochunkSide.Up, "BAND_U"),
                Socket("SOCK_R", MicrochunkSide.Right, "BAND_R"),
                Socket("SOCK_L", MicrochunkSide.Left, "BAND_L"),
                Socket("SOCK_D", MicrochunkSide.Down, "BAND_D")
            };
        }

        private static MicrochunkSocketDefinition Socket(
            string id,
            MicrochunkSide side,
            string band)
        {
            return new MicrochunkSocketDefinition(
                id,
                side,
                band,
                MicrochunkTraversalKind.Walk,
                "BIDIRECTIONAL",
                true,
                MicrochunkToolRequirement.None,
                "EDGE_TEST",
                MicrochunkRouteLayer.Both,
                2,
                "socket notes");
        }

        private static IEnumerable<MicrochunkObjectSlotDefinition> Slots()
        {
            return new[]
            {
                Slot("SLOT_U", 1, 6, MicrochunkObjectOrientation.Up),
                Slot("SLOT_R", 10, 4, MicrochunkObjectOrientation.Right),
                Slot("SLOT_N", 6, 3, MicrochunkObjectOrientation.None),
                Slot("SLOT_L", 2, 2, MicrochunkObjectOrientation.Left),
                Slot("SLOT_D", 8, 1, MicrochunkObjectOrientation.Down)
            };
        }

        private static MicrochunkObjectSlotDefinition Slot(
            string id,
            int x,
            int y,
            MicrochunkObjectOrientation orientation)
        {
            return new MicrochunkObjectSlotDefinition(
                id,
                new MicrochunkLocalCoord(x, y),
                MicrochunkSlotCategory.Resource,
                "POOL_TEST",
                true,
                orientation,
                true,
                2,
                "M_SLOT",
                "slot notes");
        }

        private static MicrochunkSide ExpectedSide(
            MicrochunkSide side,
            MicrochunkTransform transform)
        {
            if (transform == MicrochunkTransform.MirrorX || transform == MicrochunkTransform.R180)
            {
                if (side == MicrochunkSide.Left) side = MicrochunkSide.Right;
                else if (side == MicrochunkSide.Right) side = MicrochunkSide.Left;
            }

            if (transform == MicrochunkTransform.MirrorY || transform == MicrochunkTransform.R180)
            {
                if (side == MicrochunkSide.Up) side = MicrochunkSide.Down;
                else if (side == MicrochunkSide.Down) side = MicrochunkSide.Up;
            }

            return side;
        }

        private static MicrochunkObjectOrientation ExpectedOrientation(
            MicrochunkObjectOrientation orientation,
            MicrochunkTransform transform)
        {
            if (transform == MicrochunkTransform.MirrorX || transform == MicrochunkTransform.R180)
            {
                if (orientation == MicrochunkObjectOrientation.Left) orientation = MicrochunkObjectOrientation.Right;
                else if (orientation == MicrochunkObjectOrientation.Right) orientation = MicrochunkObjectOrientation.Left;
            }

            if (transform == MicrochunkTransform.MirrorY || transform == MicrochunkTransform.R180)
            {
                if (orientation == MicrochunkObjectOrientation.Up) orientation = MicrochunkObjectOrientation.Down;
                else if (orientation == MicrochunkObjectOrientation.Down) orientation = MicrochunkObjectOrientation.Up;
            }

            return orientation;
        }

        private static void AssertEquivalent(
            MicrochunkDefinition expected,
            MicrochunkDefinition actual)
        {
            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            Assert.That(actual.DisplayName, Is.EqualTo(expected.DisplayName));
            Assert.That(actual.WidthTiles, Is.EqualTo(expected.WidthTiles));
            Assert.That(actual.HeightTiles, Is.EqualTo(expected.HeightTiles));
            Assert.That(actual.UsageClass, Is.EqualTo(expected.UsageClass));
            Assert.That(actual.BiomeIds, Is.EqualTo(expected.BiomeIds));
            Assert.That(actual.RouteRoles, Is.EqualTo(expected.RouteRoles));
            Assert.That(actual.AllowedTransforms, Is.EqualTo(expected.AllowedTransforms));
            Assert.That(actual.SelectionWeight, Is.EqualTo(expected.SelectionWeight));
            Assert.That(actual.Threat, Is.EqualTo(expected.Threat));
            Assert.That(actual.Cognitive, Is.EqualTo(expected.Cognitive));
            Assert.That(actual.Chain, Is.EqualTo(expected.Chain));
            Assert.That(actual.TileDataComplete, Is.EqualTo(expected.TileDataComplete));
            Assert.That(actual.PrefabId, Is.EqualTo(expected.PrefabId));
            Assert.That(actual.Active, Is.EqualTo(expected.Active));
            Assert.That(actual.Notes, Is.EqualTo(expected.Notes));
            Assert.That(actual.TileCells.Select(CellSignature), Is.EqualTo(expected.TileCells.Select(CellSignature)));
            Assert.That(actual.Sockets.Select(SocketSignature), Is.EqualTo(expected.Sockets.Select(SocketSignature)));
            Assert.That(actual.ObjectSlots.Select(SlotSignature), Is.EqualTo(expected.ObjectSlots.Select(SlotSignature)));
        }

        private static string CellSignature(MicrochunkTileCell value)
        {
            return value.Coordinate.RowMajorIndex + "|" +
                   value.GroundCode + "|" + value.OneWayCode + "|" + value.BreakableCode + "|" +
                   value.HazardCode + "|" + value.LiquidCode + "|" + value.DecorationBackCode + "|" +
                   value.DecorationFrontCode + "|" + value.MarkerCode;
        }

        private static string SocketSignature(MicrochunkSocketDefinition value)
        {
            return value.SocketId + "|" + value.Side + "|" + value.BandId + "|" +
                   value.TraversalKind + "|" + value.Direction + "|" + value.MandatoryAllowed + "|" +
                   value.ToolRequirement + "|" + value.EdgeSignatureId + "|" + value.RouteLayer + "|" +
                   value.MinimumSafeTiles + "|" + value.Notes;
        }

        private static string SlotSignature(MicrochunkObjectSlotDefinition value)
        {
            return value.SlotId + "|" + value.Anchor.RowMajorIndex + "|" + value.Category + "|" +
                   value.AllowedPoolId + "|" + value.Required + "|" + value.Orientation + "|" +
                   value.VisibleFromRoute + "|" + value.ForbiddenRadiusTiles + "|" +
                   value.RequiredMarkerCode + "|" + value.Notes;
        }

        private static void AssertCellCodesEqual(
            MicrochunkTileCell expected,
            MicrochunkTileCell actual)
        {
            Assert.That(actual.GroundCode, Is.EqualTo(expected.GroundCode));
            Assert.That(actual.OneWayCode, Is.EqualTo(expected.OneWayCode));
            Assert.That(actual.BreakableCode, Is.EqualTo(expected.BreakableCode));
            Assert.That(actual.HazardCode, Is.EqualTo(expected.HazardCode));
            Assert.That(actual.LiquidCode, Is.EqualTo(expected.LiquidCode));
            Assert.That(actual.DecorationBackCode, Is.EqualTo(expected.DecorationBackCode));
            Assert.That(actual.DecorationFrontCode, Is.EqualTo(expected.DecorationFrontCode));
            Assert.That(actual.MarkerCode, Is.EqualTo(expected.MarkerCode));
        }

        private static void AssertSocketMetadataEqual(
            MicrochunkSocketDefinition expected,
            MicrochunkSocketDefinition actual)
        {
            Assert.That(actual.TraversalKind, Is.EqualTo(expected.TraversalKind));
            Assert.That(actual.Direction, Is.EqualTo(expected.Direction));
            Assert.That(actual.MandatoryAllowed, Is.EqualTo(expected.MandatoryAllowed));
            Assert.That(actual.ToolRequirement, Is.EqualTo(expected.ToolRequirement));
            Assert.That(actual.EdgeSignatureId, Is.EqualTo(expected.EdgeSignatureId));
            Assert.That(actual.RouteLayer, Is.EqualTo(expected.RouteLayer));
            Assert.That(actual.MinimumSafeTiles, Is.EqualTo(expected.MinimumSafeTiles));
            Assert.That(actual.Notes, Is.EqualTo(expected.Notes));
        }

        private static void AssertSlotMetadataEqual(
            MicrochunkObjectSlotDefinition expected,
            MicrochunkObjectSlotDefinition actual)
        {
            Assert.That(actual.Category, Is.EqualTo(expected.Category));
            Assert.That(actual.AllowedPoolId, Is.EqualTo(expected.AllowedPoolId));
            Assert.That(actual.Required, Is.EqualTo(expected.Required));
            Assert.That(actual.VisibleFromRoute, Is.EqualTo(expected.VisibleFromRoute));
            Assert.That(actual.ForbiddenRadiusTiles, Is.EqualTo(expected.ForbiddenRadiusTiles));
            Assert.That(actual.RequiredMarkerCode, Is.EqualTo(expected.RequiredMarkerCode));
            Assert.That(actual.Notes, Is.EqualTo(expected.Notes));
        }
    }
}
