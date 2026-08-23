using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.Map.Tests.WorldGeneration.Microchunks
{
    [Category("MAP07_04")]
    public sealed class MicrochunkSocketEdgeValidatorTests
    {
        public static IEnumerable<TestCaseData> ValidBandRangeCases
        {
            get
            {
                foreach (var axis in new[]
                         {
                             MicrochunkEdgeAxis.HorizontalEdge,
                             MicrochunkEdgeAxis.VerticalEdge
                         })
                {
                    var maximum = axis == MicrochunkEdgeAxis.HorizontalEdge
                        ? MicrochunkConstants.HeightTiles - 1
                        : MicrochunkConstants.WidthTiles - 1;
                    var side = axis == MicrochunkEdgeAxis.HorizontalEdge
                        ? MicrochunkSide.Left
                        : MicrochunkSide.Up;
                    for (var minimum = 0; minimum <= maximum; minimum++)
                    for (var end = minimum; end <= maximum; end++)
                    {
                        yield return new TestCaseData(axis, minimum, end, side);
                    }
                }
            }
        }

        public static IEnumerable<TestCaseData> ClearanceCoordinateCases
        {
            get
            {
                foreach (var depth in new[] { 1, 2 })
                {
                    for (var y = 0; y < MicrochunkConstants.HeightTiles; y++)
                    for (var offset = 0; offset < depth; offset++)
                    {
                        yield return new TestCaseData(MicrochunkSide.Left, depth, offset, y);
                        yield return new TestCaseData(
                            MicrochunkSide.Right,
                            depth,
                            MicrochunkConstants.WidthTiles - 1 - offset,
                            y);
                    }

                    for (var x = 0; x < MicrochunkConstants.WidthTiles; x++)
                    for (var offset = 0; offset < depth; offset++)
                    {
                        yield return new TestCaseData(MicrochunkSide.Down, depth, x, offset);
                        yield return new TestCaseData(
                            MicrochunkSide.Up,
                            depth,
                            x,
                            MicrochunkConstants.HeightTiles - 1 - offset);
                    }
                }
            }
        }

        public static IEnumerable<TestCaseData> OuterEdgeCoordinateCases
        {
            get
            {
                for (var y = 0; y < MicrochunkConstants.HeightTiles; y++)
                {
                    yield return new TestCaseData(MicrochunkSide.Left, 0, y);
                    yield return new TestCaseData(
                        MicrochunkSide.Right,
                        MicrochunkConstants.WidthTiles - 1,
                        y);
                }

                for (var x = 0; x < MicrochunkConstants.WidthTiles; x++)
                {
                    yield return new TestCaseData(MicrochunkSide.Down, x, 0);
                    yield return new TestCaseData(
                        MicrochunkSide.Up,
                        x,
                        MicrochunkConstants.HeightTiles - 1);
                }
            }
        }

        [TestCaseSource(nameof(ValidBandRangeCases))]
        public void EveryOrderedInBoundsBandRangeIsAccepted(
            MicrochunkEdgeAxis axis,
            int minimum,
            int maximum,
            MicrochunkSide side)
        {
            var band = Band("BAND", axis, minimum, maximum, 0);
            var socket = Socket("SOCKET", side, "BAND", "SIGNATURE", 0);
            var signature = Signature("SIGNATURE", axis, "BAND");

            var result = Validate(
                Definition(Array.Empty<MicrochunkTileCell>(), new[] { socket }, false),
                new[] { band },
                new[] { signature });

            Assert.That(result.Success, Is.True);
        }

        [TestCaseSource(nameof(ClearanceCoordinateCases))]
        public void EveryExactClearanceCoordinateDetectsBlockingGround(
            MicrochunkSide side,
            int depth,
            int x,
            int y)
        {
            var target = new MicrochunkLocalCoord(x, y);
            var cells = AllCells(coordinate =>
                coordinate == target ? Cell(coordinate, MicrochunkTileLayer.GroundSolid) : Cell(coordinate));
            var result = ValidateSingleSocket(side, depth, cells, true);

            Assert.That(result.Violations, Has.Count.EqualTo(1));
            Assert.That(result.Violations[0].Reason, Is.EqualTo(MicrochunkSocketEdgeValidator.BlockingTileCellReason));
            Assert.That(result.Violations[0].Coordinate, Is.EqualTo(target));
        }

        [TestCaseSource(nameof(OuterEdgeCoordinateCases))]
        public void PartialTileDataReportsEachMissingOuterClearanceCell(
            MicrochunkSide side,
            int x,
            int y)
        {
            var target = new MicrochunkLocalCoord(x, y);
            var cells = AllCells(Cell).Where(cell => cell.Coordinate != target);
            var result = ValidateSingleSocket(side, 1, cells, false);

            Assert.That(result.Violations, Has.Count.EqualTo(1));
            Assert.That(result.Violations[0].Reason, Is.EqualTo(MicrochunkSocketEdgeValidator.MissingTileCellReason));
            Assert.That(result.Violations[0].Coordinate, Is.EqualTo(target));
        }

        [TestCase("HORIZONTAL_EDGE", MicrochunkEdgeAxis.HorizontalEdge)]
        [TestCase("VERTICAL_EDGE", MicrochunkEdgeAxis.VerticalEdge)]
        [TestCase("SOLID", MicrochunkEdgeAxis.Solid)]
        public void AxisTokensAreExact(string token, MicrochunkEdgeAxis expected)
        {
            Assert.That(MicrochunkSocketBandDefinition.TryParseAxisToken(token, out var parsed), Is.True);
            Assert.That(parsed, Is.EqualTo(expected));
            Assert.That(MicrochunkSocketBandDefinition.ToAxisToken(parsed), Is.EqualTo(token));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("horizontal_edge")]
        [TestCase("VERTICAL")]
        [TestCase("EDGE_SOLID")]
        public void NonContractAxisTokensAreRejected(string token)
        {
            Assert.That(MicrochunkSocketBandDefinition.TryParseAxisToken(token, out _), Is.False);
            Assert.Throws<ArgumentException>(() => Band("BAND", token, 0, 0, 0));
        }

        [Test]
        public void SignatureMetadataIsImmutableAndTagsAreCanonical()
        {
            var input = new List<string> { "TAG_Z", "TAG_A" };
            var signature = new MicrochunkEdgeSignatureDefinition(
                "SIG", "HORIZONTAL_EDGE", "BAND", MicrochunkTraversalKind.OptionalBreak,
                3, 4, 5, MicrochunkToolRequirement.Pickaxe, false, input, "notes");
            input.Clear();

            Assert.That(signature.EdgeSignatureId, Is.EqualTo("SIG"));
            Assert.That(signature.AxisToken, Is.EqualTo("HORIZONTAL_EDGE"));
            Assert.That(signature.BandId, Is.EqualTo("BAND"));
            Assert.That(signature.TraversalKind, Is.EqualTo(MicrochunkTraversalKind.OptionalBreak));
            Assert.That(signature.GroundEntryHeight, Is.EqualTo(3));
            Assert.That(signature.ClearanceWidth, Is.EqualTo(4));
            Assert.That(signature.ClearanceHeight, Is.EqualTo(5));
            Assert.That(signature.ToolRequirement, Is.EqualTo(MicrochunkToolRequirement.Pickaxe));
            Assert.That(signature.MandatoryAllowed, Is.False);
            Assert.That(signature.Tags, Is.EqualTo(new[] { "TAG_A", "TAG_Z" }));
            Assert.That(signature.Notes, Is.EqualTo("notes"));
        }

        [Test]
        public void BandMetadataPreservesSuppliedContractValues()
        {
            var band = new MicrochunkSocketBandDefinition(
                "BAND", "VERTICAL_EDGE", 2, 9, 5, 3, "description");

            Assert.That(band.BandId, Is.EqualTo("BAND"));
            Assert.That(band.Axis, Is.EqualTo(MicrochunkEdgeAxis.VerticalEdge));
            Assert.That(band.MinimumLocalCoordinate, Is.EqualTo(2));
            Assert.That(band.MaximumLocalCoordinate, Is.EqualTo(9));
            Assert.That(band.RecommendedCenter, Is.EqualTo(5));
            Assert.That(band.MinimumClearanceTiles, Is.EqualTo(3));
            Assert.That(band.Description, Is.EqualTo("description"));
        }

        [Test]
        public void MissingBandAndSignatureAreBothReported()
        {
            var definition = Definition(
                Array.Empty<MicrochunkTileCell>(),
                new[] { Socket("SOCKET", MicrochunkSide.Left, "NO_BAND", "NO_SIGNATURE", 0) },
                false);

            var result = Validate(
                definition,
                Array.Empty<MicrochunkSocketBandDefinition>(),
                Array.Empty<MicrochunkEdgeSignatureDefinition>());

            Assert.That(result.Violations.Select(value => value.Reason), Is.EqualTo(new[]
            {
                MicrochunkSocketEdgeValidator.MissingBandReason,
                MicrochunkSocketEdgeValidator.MissingEdgeSignatureReason
            }));
        }

        [Test]
        public void SolidBandReferenceIsForbidden()
        {
            var result = Validate(
                Definition(Array.Empty<MicrochunkTileCell>(), new[]
                {
                    Socket("SOCKET", MicrochunkSide.Left, "SOLID_BAND", "SIGNATURE", 0)
                }, false),
                new[] { Band("SOLID_BAND", MicrochunkEdgeAxis.Solid, 0, 0, 0) },
                new[] { Signature("SIGNATURE", MicrochunkEdgeAxis.HorizontalEdge, "SOLID_BAND") });

            Assert.That(result.Violations.Any(value =>
                value.Reason == MicrochunkSocketEdgeValidator.SolidEdgeReferenceReason), Is.True);
        }

        [Test]
        public void EdgeSolidSignatureReferenceIsForbidden()
        {
            var socket = Socket("SOCKET", MicrochunkSide.Left, "BAND", "EDGE_SOLID", 0);
            var result = Validate(
                Definition(Array.Empty<MicrochunkTileCell>(), new[] { socket }, false),
                new[] { Band("BAND", MicrochunkEdgeAxis.HorizontalEdge, 0, 7, 0) },
                new[] { Signature("EDGE_SOLID", MicrochunkEdgeAxis.Solid, string.Empty) });

            Assert.That(result.Violations.Any(value =>
                value.Reason == MicrochunkSocketEdgeValidator.SolidEdgeReferenceReason), Is.True);
        }

        [TestCase(MicrochunkSide.Left, MicrochunkEdgeAxis.VerticalEdge)]
        [TestCase(MicrochunkSide.Right, MicrochunkEdgeAxis.VerticalEdge)]
        [TestCase(MicrochunkSide.Up, MicrochunkEdgeAxis.HorizontalEdge)]
        [TestCase(MicrochunkSide.Down, MicrochunkEdgeAxis.HorizontalEdge)]
        public void SocketSideRejectsOppositeBandAxis(MicrochunkSide side, MicrochunkEdgeAxis bandAxis)
        {
            var signatureAxis = ExpectedAxis(side);
            var result = Validate(
                Definition(Array.Empty<MicrochunkTileCell>(), new[]
                {
                    Socket("SOCKET", side, "BAND", "SIGNATURE", 0)
                }, false),
                new[] { Band("BAND", bandAxis, 0, 0, 0) },
                new[] { Signature("SIGNATURE", signatureAxis, "BAND") });

            Assert.That(result.Violations.Any(value =>
                value.Reason == MicrochunkSocketEdgeValidator.BandAxisMismatchReason), Is.True);
        }

        [TestCase(MicrochunkSide.Left, MicrochunkEdgeAxis.VerticalEdge)]
        [TestCase(MicrochunkSide.Right, MicrochunkEdgeAxis.VerticalEdge)]
        [TestCase(MicrochunkSide.Up, MicrochunkEdgeAxis.HorizontalEdge)]
        [TestCase(MicrochunkSide.Down, MicrochunkEdgeAxis.HorizontalEdge)]
        public void SocketSideRejectsOppositeSignatureAxis(MicrochunkSide side, MicrochunkEdgeAxis signatureAxis)
        {
            var axis = ExpectedAxis(side);
            var result = Validate(
                Definition(Array.Empty<MicrochunkTileCell>(), new[]
                {
                    Socket("SOCKET", side, "BAND", "SIGNATURE", 0)
                }, false),
                new[] { Band("BAND", axis, 0, 0, 0) },
                new[] { Signature("SIGNATURE", signatureAxis, "BAND") });

            Assert.That(result.Violations.Any(value =>
                value.Reason == MicrochunkSocketEdgeValidator.SignatureAxisMismatchReason), Is.True);
        }

        [Test]
        public void NonEmptySignatureBandMustMatchSocketBand()
        {
            AssertSingleMetadataViolation(
                Socket("SOCKET", MicrochunkSide.Left, "BAND", "SIGNATURE", 0),
                Signature("SIGNATURE", MicrochunkEdgeAxis.HorizontalEdge, "OTHER_BAND"),
                MicrochunkSocketEdgeValidator.SignatureBandMismatchReason);
        }

        [Test]
        public void EmptySignatureBandActsAsUnboundWildcard()
        {
            var result = Validate(
                Definition(Array.Empty<MicrochunkTileCell>(), new[]
                {
                    Socket("SOCKET", MicrochunkSide.Left, "BAND", "SIGNATURE", 0)
                }, false),
                new[] { Band("BAND", MicrochunkEdgeAxis.HorizontalEdge, 0, 7, 0) },
                new[] { Signature("SIGNATURE", MicrochunkEdgeAxis.HorizontalEdge, string.Empty) });

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void TraversalKindMustMatch()
        {
            var socket = Socket("SOCKET", MicrochunkSide.Left, "BAND", "SIGNATURE", 0);
            var signature = Signature(
                "SIGNATURE", MicrochunkEdgeAxis.HorizontalEdge, "BAND",
                MicrochunkTraversalKind.Climb);
            AssertSingleMetadataViolation(
                socket,
                signature,
                MicrochunkSocketEdgeValidator.TraversalMismatchReason);
        }

        [Test]
        public void ToolRequirementMustMatch()
        {
            var socket = Socket("SOCKET", MicrochunkSide.Left, "BAND", "SIGNATURE", 0);
            var signature = Signature(
                "SIGNATURE", MicrochunkEdgeAxis.HorizontalEdge, "BAND",
                MicrochunkTraversalKind.Walk,
                MicrochunkToolRequirement.Rope);
            AssertSingleMetadataViolation(
                socket,
                signature,
                MicrochunkSocketEdgeValidator.ToolRequirementMismatchReason);
        }

        [Test]
        public void MandatorySocketRequiresSignaturePermission()
        {
            var socket = Socket(
                "SOCKET", MicrochunkSide.Left, "BAND", "SIGNATURE", 0,
                true, MicrochunkTraversalKind.Walk, MicrochunkToolRequirement.None);
            var signature = Signature(
                "SIGNATURE", MicrochunkEdgeAxis.HorizontalEdge, "BAND",
                MicrochunkTraversalKind.Walk, MicrochunkToolRequirement.None, false);
            AssertSingleMetadataViolation(
                socket,
                signature,
                MicrochunkSocketEdgeValidator.MandatorySocketNotAllowedReason);
        }

        [Test]
        public void OptionalSocketMayUseSignatureThatDisallowsMandatory()
        {
            var socket = Socket(
                "SOCKET", MicrochunkSide.Left, "BAND", "SIGNATURE", 0,
                false, MicrochunkTraversalKind.Walk, MicrochunkToolRequirement.None);
            var result = Validate(
                Definition(Array.Empty<MicrochunkTileCell>(), new[] { socket }, false),
                new[] { Band("BAND", MicrochunkEdgeAxis.HorizontalEdge, 0, 7, 0) },
                new[] { Signature(
                    "SIGNATURE", MicrochunkEdgeAxis.HorizontalEdge, "BAND",
                    MicrochunkTraversalKind.Walk, MicrochunkToolRequirement.None, false) });

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void SocketMinimumSafeTilesMustMeetBandMinimum()
        {
            var socket = Socket("SOCKET", MicrochunkSide.Left, "BAND", "SIGNATURE", 1);
            var result = Validate(
                Definition(AllCells(Cell), new[] { socket }, true),
                new[] { Band("BAND", MicrochunkEdgeAxis.HorizontalEdge, 0, 7, 2) },
                new[] { Signature("SIGNATURE", MicrochunkEdgeAxis.HorizontalEdge, "BAND") });

            Assert.That(result.Violations.Any(value =>
                value.Reason == MicrochunkSocketEdgeValidator.MinimumSafeTilesBelowBandMinimumReason), Is.True);
        }

        [TestCase(MicrochunkEdgeAxis.HorizontalEdge, 4, 3)]
        [TestCase(MicrochunkEdgeAxis.VerticalEdge, 9, 8)]
        public void ReversedBandRangeIsReported(
            MicrochunkEdgeAxis axis,
            int minimum,
            int maximum)
        {
            AssertBandViolation(axis, minimum, maximum, 0, MicrochunkSocketEdgeValidator.BandRangeReversedReason);
        }

        [TestCase(MicrochunkEdgeAxis.HorizontalEdge, -1, 2)]
        [TestCase(MicrochunkEdgeAxis.HorizontalEdge, 0, 8)]
        [TestCase(MicrochunkEdgeAxis.VerticalEdge, -1, 2)]
        [TestCase(MicrochunkEdgeAxis.VerticalEdge, 0, 12)]
        public void OutOfBoundsBandRangeIsReported(
            MicrochunkEdgeAxis axis,
            int minimum,
            int maximum)
        {
            AssertBandViolation(axis, minimum, maximum, 0, MicrochunkSocketEdgeValidator.BandRangeOutOfBoundsReason);
        }

        [Test]
        public void NegativeBandMinimumClearanceIsReported()
        {
            AssertBandViolation(
                MicrochunkEdgeAxis.HorizontalEdge,
                0,
                7,
                -1,
                MicrochunkSocketEdgeValidator.BandMinimumClearanceNegativeReason);
        }

        [TestCase(MicrochunkSide.Left, 13)]
        [TestCase(MicrochunkSide.Right, 13)]
        [TestCase(MicrochunkSide.Up, 9)]
        [TestCase(MicrochunkSide.Down, 9)]
        public void ClearanceDepthCannotExceedDimension(MicrochunkSide side, int depth)
        {
            var axis = ExpectedAxis(side);
            var maximum = axis == MicrochunkEdgeAxis.HorizontalEdge ? 7 : 11;
            var result = Validate(
                Definition(Array.Empty<MicrochunkTileCell>(), new[]
                {
                    Socket("SOCKET", side, "BAND", "SIGNATURE", depth)
                }, false),
                new[] { Band("BAND", axis, 0, maximum, 0) },
                new[] { Signature("SIGNATURE", axis, "BAND") });

            Assert.That(result.Violations.Any(value =>
                value.Reason == MicrochunkSocketEdgeValidator.ClearanceDepthOutOfRangeReason), Is.True);
            Assert.That(result.Violations.Any(value => value.HasCoordinate), Is.False);
        }

        [TestCase(MicrochunkTileLayer.GroundSolid)]
        [TestCase(MicrochunkTileLayer.Breakable)]
        [TestCase(MicrochunkTileLayer.Hazard)]
        [TestCase(MicrochunkTileLayer.Liquid)]
        public void ContractBlockingLayersBlockClearance(MicrochunkTileLayer layer)
        {
            var target = new MicrochunkLocalCoord(0, 0);
            var result = ValidateSingleSocket(
                MicrochunkSide.Left,
                1,
                AllCells(coordinate => coordinate == target ? Cell(coordinate, layer) : Cell(coordinate)),
                true);

            Assert.That(result.Violations.Any(value =>
                value.Reason == MicrochunkSocketEdgeValidator.BlockingTileCellReason &&
                value.Coordinate == target), Is.True);
        }

        [TestCase(MicrochunkTileLayer.OneWay)]
        [TestCase(MicrochunkTileLayer.DecorationBack)]
        [TestCase(MicrochunkTileLayer.DecorationFront)]
        [TestCase(MicrochunkTileLayer.Marker)]
        public void ContractNonBlockingLayersDoNotBlockClearance(MicrochunkTileLayer layer)
        {
            var target = new MicrochunkLocalCoord(0, 0);
            var result = ValidateSingleSocket(
                MicrochunkSide.Left,
                1,
                AllCells(coordinate => coordinate == target ? Cell(coordinate, layer) : Cell(coordinate)),
                true);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void NoneCodesDoNotBlockClearance()
        {
            Assert.That(ValidateSingleSocket(MicrochunkSide.Left, 1, AllCells(Cell), true).Success, Is.True);
        }

        [Test]
        public void ViolationsUseSocketReasonAndRowMajorCanonicalOrder()
        {
            var id = new MicrochunkId("MC_ORDER");
            var values = new[]
            {
                Violation(id, "SOCKET_B", "Z_REASON", new MicrochunkLocalCoord(0, 0)),
                Violation(id, "SOCKET_A", "B_REASON", new MicrochunkLocalCoord(0, 1)),
                Violation(id, "SOCKET_A", "A_REASON", new MicrochunkLocalCoord(11, 7)),
                Violation(id, "SOCKET_A", "B_REASON", new MicrochunkLocalCoord(1, 0)),
                Violation(id, "SOCKET_A", "B_REASON", null)
            };

            var result = new MicrochunkSocketEdgeValidationResult(2, values);

            Assert.That(result.Violations.Select(value =>
                value.SocketId + "|" + value.Reason + "|" +
                (value.HasCoordinate ? value.Coordinate.Value.RowMajorIndex.ToString() : "NONE")),
                Is.EqualTo(new[]
                {
                    "SOCKET_A|A_REASON|95",
                    "SOCKET_A|B_REASON|NONE",
                    "SOCKET_A|B_REASON|1",
                    "SOCKET_A|B_REASON|12",
                    "SOCKET_B|Z_REASON|0"
                }));
        }

        [Test]
        public void ResultSnapshotDoesNotAliasInputList()
        {
            var values = new List<MicrochunkSocketEdgeValidationViolation>
            {
                Violation(new MicrochunkId("MC_IMMUTABLE"), "SOCKET", "REASON", null)
            };
            var result = new MicrochunkSocketEdgeValidationResult(1, values);
            values.Clear();

            Assert.That(result.IssueCount, Is.EqualTo(1));
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void TwentyFiveStarterSocketRowsValidateTogether()
        {
            var sockets = Enumerable.Range(0, 25)
                .Select(index =>
                {
                    var side = (MicrochunkSide)(index % 4);
                    var axis = ExpectedAxis(side);
                    var bandId = axis == MicrochunkEdgeAxis.HorizontalEdge ? "H_FULL" : "V_FULL";
                    var signatureId = axis == MicrochunkEdgeAxis.HorizontalEdge ? "SIG_H" : "SIG_V";
                    return Socket("SOCKET_" + index.ToString("D2"), side, bandId, signatureId, 1);
                })
                .ToArray();

            var result = Validate(
                Definition(AllCells(Cell), sockets, true),
                new[]
                {
                    Band("H_FULL", MicrochunkEdgeAxis.HorizontalEdge, 0, 7, 1),
                    Band("V_FULL", MicrochunkEdgeAxis.VerticalEdge, 0, 11, 1)
                },
                new[]
                {
                    Signature("SIG_H", MicrochunkEdgeAxis.HorizontalEdge, "H_FULL"),
                    Signature("SIG_V", MicrochunkEdgeAxis.VerticalEdge, "V_FULL")
                });

            Assert.That(result.Success, Is.True);
            Assert.That(result.EvaluatedSocketCount, Is.EqualTo(25));
        }

        [TestCase(MicrochunkTransform.R0)]
        [TestCase(MicrochunkTransform.MirrorX)]
        [TestCase(MicrochunkTransform.MirrorY)]
        [TestCase(MicrochunkTransform.R180)]
        public void GeometryConsistentTransformsRemainValid(MicrochunkTransform transform)
        {
            var sockets = new[]
            {
                Socket("L", MicrochunkSide.Left, "H_FULL", "SIG_H", 1),
                Socket("R", MicrochunkSide.Right, "H_FULL", "SIG_H", 1),
                Socket("U", MicrochunkSide.Up, "V_FULL", "SIG_V", 1),
                Socket("D", MicrochunkSide.Down, "V_FULL", "SIG_V", 1)
            };
            var transformed = MicrochunkTransformer.Transform(
                Definition(AllCells(Cell), sockets, true),
                transform).Definition;

            var result = Validate(
                transformed,
                new[]
                {
                    Band("H_FULL", MicrochunkEdgeAxis.HorizontalEdge, 0, 7, 1),
                    Band("V_FULL", MicrochunkEdgeAxis.VerticalEdge, 0, 11, 1)
                },
                new[]
                {
                    Signature("SIG_H", MicrochunkEdgeAxis.HorizontalEdge, "H_FULL"),
                    Signature("SIG_V", MicrochunkEdgeAxis.VerticalEdge, "V_FULL")
                });

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void ZeroSafeTilesRequiresNoTileCells()
        {
            var result = ValidateSingleSocket(
                MicrochunkSide.Left,
                0,
                Array.Empty<MicrochunkTileCell>(),
                false);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void ValidatorDoesNotRequireStandaloneNinetySixCellCompleteness()
        {
            var cells = Enumerable.Range(0, MicrochunkConstants.HeightTiles)
                .Select(y => Cell(new MicrochunkLocalCoord(0, y)));
            var result = ValidateSingleSocket(MicrochunkSide.Left, 1, cells, false);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void NullInputsAreRejected()
        {
            var definition = Definition(Array.Empty<MicrochunkTileCell>(), Array.Empty<MicrochunkSocketDefinition>(), false);
            var bands = new Dictionary<string, MicrochunkSocketBandDefinition>();
            var signatures = new Dictionary<string, MicrochunkEdgeSignatureDefinition>();

            Assert.Throws<ArgumentNullException>(() =>
                MicrochunkSocketEdgeValidator.ValidateDefinition(null, bands, signatures));
            Assert.Throws<ArgumentNullException>(() =>
                MicrochunkSocketEdgeValidator.ValidateDefinition(definition, null, signatures));
            Assert.Throws<ArgumentNullException>(() =>
                MicrochunkSocketEdgeValidator.ValidateDefinition(definition, bands, null));
        }

        private static void AssertSingleMetadataViolation(
            MicrochunkSocketDefinition socket,
            MicrochunkEdgeSignatureDefinition signature,
            string expectedReason)
        {
            var result = Validate(
                Definition(Array.Empty<MicrochunkTileCell>(), new[] { socket }, false),
                new[] { Band(socket.BandId, ExpectedAxis(socket.Side), 0,
                    ExpectedAxis(socket.Side) == MicrochunkEdgeAxis.HorizontalEdge ? 7 : 11, 0) },
                new[] { signature });

            Assert.That(result.Violations.Select(value => value.Reason), Does.Contain(expectedReason));
        }

        private static void AssertBandViolation(
            MicrochunkEdgeAxis axis,
            int minimum,
            int maximum,
            int minimumClearance,
            string expectedReason)
        {
            var side = axis == MicrochunkEdgeAxis.HorizontalEdge
                ? MicrochunkSide.Left
                : MicrochunkSide.Up;
            var result = Validate(
                Definition(Array.Empty<MicrochunkTileCell>(), new[]
                {
                    Socket("SOCKET", side, "BAND", "SIGNATURE", 0)
                }, false),
                new[] { Band("BAND", axis, minimum, maximum, minimumClearance) },
                new[] { Signature("SIGNATURE", axis, "BAND") });

            Assert.That(result.Violations.Any(value => value.Reason == expectedReason), Is.True);
        }

        private static MicrochunkSocketEdgeValidationResult ValidateSingleSocket(
            MicrochunkSide side,
            int depth,
            IEnumerable<MicrochunkTileCell> cells,
            bool complete)
        {
            var axis = ExpectedAxis(side);
            var bandId = axis == MicrochunkEdgeAxis.HorizontalEdge ? "H_FULL" : "V_FULL";
            var signatureId = axis == MicrochunkEdgeAxis.HorizontalEdge ? "SIG_H" : "SIG_V";
            var maximum = axis == MicrochunkEdgeAxis.HorizontalEdge ? 7 : 11;
            return Validate(
                Definition(cells, new[] { Socket("SOCKET", side, bandId, signatureId, depth) }, complete),
                new[] { Band(bandId, axis, 0, maximum, 0) },
                new[] { Signature(signatureId, axis, bandId) });
        }

        private static MicrochunkSocketEdgeValidationResult Validate(
            MicrochunkDefinition definition,
            IEnumerable<MicrochunkSocketBandDefinition> bands,
            IEnumerable<MicrochunkEdgeSignatureDefinition> signatures)
        {
            return MicrochunkSocketEdgeValidator.ValidateDefinition(
                definition,
                bands.ToDictionary(value => value.BandId, StringComparer.Ordinal),
                signatures.ToDictionary(value => value.EdgeSignatureId, StringComparer.Ordinal));
        }

        private static MicrochunkDefinition Definition(
            IEnumerable<MicrochunkTileCell> cells,
            IEnumerable<MicrochunkSocketDefinition> sockets,
            bool complete)
        {
            return new MicrochunkDefinition(
                new MicrochunkId("MC_SOCKET_EDGE_TEST"),
                "socket edge test",
                MicrochunkConstants.WidthTiles,
                MicrochunkConstants.HeightTiles,
                MicrochunkUsageClass.Traversal,
                new[] { "BIOME_TEST" },
                new[] { "ROUTE_TEST" },
                new[] { MicrochunkTransform.R0 },
                1,
                0,
                0,
                0,
                complete,
                "PREFAB_TEST",
                true,
                string.Empty,
                cells,
                sockets,
                Array.Empty<MicrochunkObjectSlotDefinition>());
        }

        private static MicrochunkSocketDefinition Socket(
            string id,
            MicrochunkSide side,
            string bandId,
            string signatureId,
            int minimumSafeTiles,
            bool mandatoryAllowed = true,
            MicrochunkTraversalKind traversalKind = MicrochunkTraversalKind.Walk,
            MicrochunkToolRequirement toolRequirement = MicrochunkToolRequirement.None)
        {
            return new MicrochunkSocketDefinition(
                id,
                side,
                bandId,
                traversalKind,
                "BIDIRECTIONAL",
                mandatoryAllowed,
                toolRequirement,
                signatureId,
                MicrochunkRouteLayer.Both,
                minimumSafeTiles,
                string.Empty);
        }

        private static MicrochunkSocketBandDefinition Band(
            string id,
            MicrochunkEdgeAxis axis,
            int minimum,
            int maximum,
            int minimumClearance)
        {
            return new MicrochunkSocketBandDefinition(
                id,
                axis,
                minimum,
                maximum,
                (minimum + maximum) / 2,
                minimumClearance,
                string.Empty);
        }

        private static MicrochunkSocketBandDefinition Band(
            string id,
            string axisToken,
            int minimum,
            int maximum,
            int minimumClearance)
        {
            return new MicrochunkSocketBandDefinition(
                id,
                axisToken,
                minimum,
                maximum,
                (minimum + maximum) / 2,
                minimumClearance,
                string.Empty);
        }

        private static MicrochunkEdgeSignatureDefinition Signature(
            string id,
            MicrochunkEdgeAxis axis,
            string bandId,
            MicrochunkTraversalKind traversalKind = MicrochunkTraversalKind.Walk,
            MicrochunkToolRequirement toolRequirement = MicrochunkToolRequirement.None,
            bool mandatoryAllowed = true)
        {
            return new MicrochunkEdgeSignatureDefinition(
                id,
                axis,
                bandId,
                traversalKind,
                0,
                1,
                1,
                toolRequirement,
                mandatoryAllowed,
                Array.Empty<string>(),
                string.Empty);
        }

        private static IEnumerable<MicrochunkTileCell> AllCells(
            Func<MicrochunkLocalCoord, MicrochunkTileCell> factory)
        {
            for (var y = 0; y < MicrochunkConstants.HeightTiles; y++)
            for (var x = 0; x < MicrochunkConstants.WidthTiles; x++)
            {
                yield return factory(new MicrochunkLocalCoord(x, y));
            }
        }

        private static MicrochunkTileCell Cell(MicrochunkLocalCoord coordinate)
        {
            return Cell(coordinate, null);
        }

        private static MicrochunkTileCell Cell(
            MicrochunkLocalCoord coordinate,
            MicrochunkTileLayer? occupiedLayer)
        {
            string Code(MicrochunkTileLayer layer)
            {
                return occupiedLayer == layer ? "OCCUPIED" : "NONE";
            }

            return new MicrochunkTileCell(
                coordinate,
                Code(MicrochunkTileLayer.GroundSolid),
                Code(MicrochunkTileLayer.OneWay),
                Code(MicrochunkTileLayer.Breakable),
                Code(MicrochunkTileLayer.Hazard),
                Code(MicrochunkTileLayer.Liquid),
                Code(MicrochunkTileLayer.DecorationBack),
                Code(MicrochunkTileLayer.DecorationFront),
                Code(MicrochunkTileLayer.Marker));
        }

        private static MicrochunkEdgeAxis ExpectedAxis(MicrochunkSide side)
        {
            return side == MicrochunkSide.Left || side == MicrochunkSide.Right
                ? MicrochunkEdgeAxis.HorizontalEdge
                : MicrochunkEdgeAxis.VerticalEdge;
        }

        private static MicrochunkSocketEdgeValidationViolation Violation(
            MicrochunkId id,
            string socketId,
            string reason,
            MicrochunkLocalCoord? coordinate)
        {
            return new MicrochunkSocketEdgeValidationViolation(
                id,
                socketId,
                MicrochunkSide.Left,
                "BAND",
                "SIGNATURE",
                coordinate,
                reason);
        }
    }
}
