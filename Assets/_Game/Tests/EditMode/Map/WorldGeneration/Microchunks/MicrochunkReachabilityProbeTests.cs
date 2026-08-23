using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.Map.Tests.WorldGeneration.Microchunks
{
    [TestFixture]
    [Category("MAP07_07")]
    public sealed class MicrochunkReachabilityProbeTests
    {
        private static readonly MicrochunkId DefaultId = new MicrochunkId("MC_REACHABILITY_TEST");

        public static IEnumerable<TestCaseData> EveryCoordinate
        {
            get
            {
                for (var y = 0; y < MicrochunkConstants.HeightTiles; y++)
                for (var x = 0; x < MicrochunkConstants.WidthTiles; x++)
                {
                    var index = (y * MicrochunkConstants.WidthTiles) + x;
                    yield return new TestCaseData(x, y, index)
                        .SetName($"EveryUnblockedCoordinate_{index:D2}_{x}_{y}");
                }
            }
        }

        public static IEnumerable<TestCaseData> EveryBlockingLayerAndCoordinate
        {
            get
            {
                var layers = new[]
                {
                    MicrochunkTileLayer.GroundSolid,
                    MicrochunkTileLayer.Breakable,
                    MicrochunkTileLayer.Hazard,
                    MicrochunkTileLayer.Liquid
                };
                foreach (var layer in layers)
                for (var y = 0; y < MicrochunkConstants.HeightTiles; y++)
                for (var x = 0; x < MicrochunkConstants.WidthTiles; x++)
                {
                    var index = (y * MicrochunkConstants.WidthTiles) + x;
                    yield return new TestCaseData(layer, x, y, index)
                        .SetName($"Blocking_{layer}_{index:D2}_{x}_{y}");
                }
            }
        }

        public static IEnumerable<TestCaseData> ApprovedTransforms => new[]
        {
            new TestCaseData(MicrochunkTransform.R0),
            new TestCaseData(MicrochunkTransform.MirrorX),
            new TestCaseData(MicrochunkTransform.MirrorY),
            new TestCaseData(MicrochunkTransform.R180)
        };

        [TestCaseSource(nameof(EveryCoordinate))]
        public void EveryUnblockedCoordinateBecomesAnExactTraversalNode(int x, int y, int index)
        {
            var result = Validate(Definition(CompleteCells(), Array.Empty<MicrochunkSocketDefinition>()));

            Assert.That(result.Success, Is.True);
            Assert.That(result.Nodes, Has.Count.EqualTo(96));
            Assert.That(result.Nodes[index].MicrochunkId, Is.EqualTo(DefaultId));
            Assert.That(result.Nodes[index].Coordinate, Is.EqualTo(new MicrochunkLocalCoord(x, y)));
        }

        [TestCaseSource(nameof(EveryBlockingLayerAndCoordinate))]
        public void EveryBlockingLayerRemovesTheExactCoordinate(
            MicrochunkTileLayer layer,
            int x,
            int y,
            int index)
        {
            var cells = CompleteCells();
            cells[index] = Cell(x, y, layer, "BLOCKED");

            var result = Validate(Definition(cells, Array.Empty<MicrochunkSocketDefinition>()));

            Assert.That(result.Success, Is.True);
            Assert.That(result.Nodes, Has.Count.EqualTo(95));
            Assert.That(result.Nodes.Any(node => node.Coordinate == new MicrochunkLocalCoord(x, y)), Is.False);
        }

        [TestCase(MicrochunkTileLayer.OneWay)]
        [TestCase(MicrochunkTileLayer.DecorationBack)]
        [TestCase(MicrochunkTileLayer.DecorationFront)]
        [TestCase(MicrochunkTileLayer.Marker)]
        public void EveryExplicitNonBlockingLayerPreservesTheNode(MicrochunkTileLayer layer)
        {
            var cells = CompleteCells();
            cells[37] = Cell(1, 3, layer, "NON_BLOCKING");

            var result = Validate(Definition(cells, Array.Empty<MicrochunkSocketDefinition>()));

            Assert.That(result.Nodes, Has.Count.EqualTo(96));
            Assert.That(result.Nodes.Any(node => node.Coordinate == new MicrochunkLocalCoord(1, 3)), Is.True);
        }

        [Test]
        public void ExplicitNoneCodesRemainUnblocked()
        {
            var result = Validate(Definition(CompleteCells(), Array.Empty<MicrochunkSocketDefinition>()));

            Assert.That(result.Nodes, Has.Count.EqualTo(MicrochunkConstants.CellCount));
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void NodeEdgePolicyViolationResultAndWitnessAreImmutableSnapshots()
        {
            var node = new MicrochunkTraversalNode(DefaultId, new MicrochunkLocalCoord(1, 2));
            var edge = new MicrochunkTraversalEdge(
                new MicrochunkLocalCoord(1, 2),
                new MicrochunkLocalCoord(2, 2),
                MicrochunkTraversalEdge.WalkMovement,
                1);
            var markerSource = new List<string> { "M_LADDER" };
            var orderSource = new List<string> { MicrochunkTraversalEdge.JumpMovement };
            var policy = new MicrochunkReachabilityPolicy(2, 3, 4, markerSource, orderSource);
            var violation = new MicrochunkReachabilityViolation(
                DefaultId,
                "SOCK_A",
                "SOCK_B",
                new MicrochunkLocalCoord(3, 2),
                MicrochunkReachabilityProbe.MandatorySocketPairUnreachableReason);
            var coordinateSource = new List<MicrochunkLocalCoord>
            {
                edge.SourceCoordinate,
                edge.TargetCoordinate
            };
            var edgeSource = new List<MicrochunkTraversalEdge> { edge };
            var witness = new MicrochunkReachabilityPathWitness(
                DefaultId,
                "SOCK_A",
                "SOCK_B",
                coordinateSource,
                edgeSource);
            var violationSource = new List<MicrochunkReachabilityViolation> { violation };
            var witnessSource = new List<MicrochunkReachabilityPathWitness> { witness };
            var nodeSource = new List<MicrochunkTraversalNode> { node };
            var resultEdgeSource = new List<MicrochunkTraversalEdge> { edge };
            var entries = new Dictionary<string, IReadOnlyList<MicrochunkLocalCoord>>
            {
                { "SOCK_A", new List<MicrochunkLocalCoord> { edge.SourceCoordinate } }
            };
            var result = new MicrochunkReachabilityResult(
                2, 1, 0, violationSource, witnessSource, nodeSource, resultEdgeSource, entries);

            markerSource.Clear();
            orderSource.Clear();
            coordinateSource.Clear();
            edgeSource.Clear();
            violationSource.Clear();
            witnessSource.Clear();
            nodeSource.Clear();
            resultEdgeSource.Clear();
            ((List<MicrochunkLocalCoord>)entries["SOCK_A"]).Clear();

            Assert.That(node.MicrochunkId, Is.EqualTo(DefaultId));
            Assert.That(node.Coordinate, Is.EqualTo(new MicrochunkLocalCoord(1, 2)));
            Assert.That(edge.MovementKind, Is.EqualTo("WALK"));
            Assert.That(edge.MovementKindValue, Is.EqualTo(MicrochunkTraversalMovementKind.Walk));
            Assert.That(policy.MaximumJumpRise, Is.EqualTo(2));
            Assert.That(policy.MaximumJumpHorizontalSpan, Is.EqualTo(3));
            Assert.That(policy.MaximumDropDistance, Is.EqualTo(4));
            Assert.That(policy.ClimbMarkerCodes, Is.EqualTo(new[] { "M_LADDER" }));
            Assert.That(policy.NeighborOrdering[0], Is.EqualTo("JUMP"));
            Assert.That(violation.HasPairedSocketId, Is.True);
            Assert.That(violation.HasLocalCoordinate, Is.True);
            Assert.That(witness.Coordinates, Has.Count.EqualTo(2));
            Assert.That(witness.Edges, Has.Count.EqualTo(1));
            Assert.That(witness.Cost, Is.EqualTo(1));
            Assert.That(result.Nodes, Has.Count.EqualTo(1));
            Assert.That(result.Edges, Has.Count.EqualTo(1));
            Assert.That(result.Violations, Has.Count.EqualTo(1));
            Assert.That(result.PathWitnesses, Has.Count.EqualTo(1));
            Assert.That(result.SocketEntries["SOCK_A"], Has.Count.EqualTo(1));
        }

        [Test]
        public void MovementKindTokensAreExactAndRoundTrip()
        {
            var tokens = new[] { "FLOOD", "WALK", "JUMP", "DROP", "CLIMB", "SOCKET_ENTRY" };

            Assert.That(Enum.GetValues(typeof(MicrochunkTraversalMovementKind)).Length, Is.EqualTo(6));
            Assert.That(tokens.All(MicrochunkTraversalEdge.IsSupportedMovementKind), Is.True);
            Assert.That(tokens.Select(MicrochunkTraversalEdge.ParseMovementKind)
                .Select(MicrochunkTraversalEdge.ToMovementToken), Is.EqualTo(tokens));
            Assert.Throws<ArgumentException>(() => new MicrochunkTraversalEdge(
                new MicrochunkLocalCoord(0, 0), new MicrochunkLocalCoord(1, 0), "walk", 1));
        }

        [Test]
        public void MandatoryNoToolSocketsAreTheOnlyEvaluatedSockets()
        {
            var sockets = new[]
            {
                Socket("A_MANDATORY", MicrochunkSide.Left, "H", true, MicrochunkToolRequirement.None),
                Socket("B_TOOL", MicrochunkSide.Right, "H", true, MicrochunkToolRequirement.Pickaxe),
                Socket("C_OPTIONAL", MicrochunkSide.Right, "H", false, MicrochunkToolRequirement.None)
            };

            var result = Validate(Definition(CompleteCells(), sockets), HorizontalBand("H", 0, 0));

            Assert.That(result.EvaluatedSocketCount, Is.EqualTo(1));
            Assert.That(result.EvaluatedPairCount, Is.Zero);
            Assert.That(result.SocketEntries.Keys, Is.EqualTo(new[] { "A_MANDATORY" }));
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AllFourSidesResolveExactBandEdgeCoordinates()
        {
            var sockets = new[]
            {
                Socket("D", MicrochunkSide.Down, "V", true, MicrochunkToolRequirement.None),
                Socket("L", MicrochunkSide.Left, "H", true, MicrochunkToolRequirement.None),
                Socket("R", MicrochunkSide.Right, "H", true, MicrochunkToolRequirement.None),
                Socket("U", MicrochunkSide.Up, "V", true, MicrochunkToolRequirement.None)
            };
            var bands = new[] { HorizontalBand("H", 2, 3), VerticalBand("V", 4, 5) };

            var result = Validate(Definition(CompleteCells(), sockets), bands);

            Assert.That(result.SocketEntries["L"], Is.EqualTo(new[]
            {
                new MicrochunkLocalCoord(0, 2), new MicrochunkLocalCoord(0, 3)
            }));
            Assert.That(result.SocketEntries["R"], Is.EqualTo(new[]
            {
                new MicrochunkLocalCoord(11, 2), new MicrochunkLocalCoord(11, 3)
            }));
            Assert.That(result.SocketEntries["D"], Is.EqualTo(new[]
            {
                new MicrochunkLocalCoord(4, 0), new MicrochunkLocalCoord(5, 0)
            }));
            Assert.That(result.SocketEntries["U"], Is.EqualTo(new[]
            {
                new MicrochunkLocalCoord(4, 7), new MicrochunkLocalCoord(5, 7)
            }));
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void MissingBandReportsStableEntryDiagnostic()
        {
            var definition = Definition(
                CompleteCells(),
                new[] { Socket("SOCK_MISSING", MicrochunkSide.Left, "NO_BAND", true, MicrochunkToolRequirement.None) });

            var result = Validate(definition);

            Assert.That(result.Success, Is.False);
            Assert.That(result.EvaluatedPairCount, Is.Zero);
            Assert.That(result.Violations, Has.Count.EqualTo(1));
            Assert.That(result.Violations[0].SocketId, Is.EqualTo("SOCK_MISSING"));
            Assert.That(result.Violations[0].Reason, Is.EqualTo("MANDATORY_SOCKET_ENTRY_UNREACHABLE"));
            Assert.That(result.Violations[0].HasLocalCoordinate, Is.False);
        }

        [Test]
        public void FullyBlockedBandReportsFirstCandidateCoordinate()
        {
            var cells = CompleteCells();
            cells[24] = Cell(0, 2, MicrochunkTileLayer.GroundSolid, "BLOCK");
            cells[36] = Cell(0, 3, MicrochunkTileLayer.GroundSolid, "BLOCK");
            var definition = Definition(
                cells,
                new[] { Socket("SOCK_L", MicrochunkSide.Left, "H", true, MicrochunkToolRequirement.None) });

            var result = Validate(definition, HorizontalBand("H", 2, 3));

            Assert.That(result.Success, Is.False);
            Assert.That(result.SocketEntries["SOCK_L"], Is.Empty);
            Assert.That(result.Violations.Single().Coordinate, Is.EqualTo(new MicrochunkLocalCoord(0, 2)));
        }

        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(2, 1)]
        [TestCase(3, 3)]
        public void SocketPairEnumerationUsesUnorderedCombinations(int socketCount, int expectedPairs)
        {
            var sockets = new List<MicrochunkSocketDefinition>();
            var sides = new[] { MicrochunkSide.Left, MicrochunkSide.Right, MicrochunkSide.Down };
            for (var index = 0; index < socketCount; index++)
            {
                sockets.Add(Socket(
                    "SOCK_" + index,
                    sides[index],
                    sides[index] == MicrochunkSide.Down ? "V" : "H",
                    true,
                    MicrochunkToolRequirement.None));
            }

            var result = Validate(
                Definition(CompleteCells(), sockets),
                HorizontalBand("H", 0, 0),
                VerticalBand("V", 0, 0));

            Assert.That(result.EvaluatedSocketCount, Is.EqualTo(socketCount));
            Assert.That(result.EvaluatedPairCount, Is.EqualTo(expectedPairs));
            Assert.That(result.ReachablePairCount, Is.EqualTo(expectedPairs));
            Assert.That(result.PathWitnesses, Has.Count.EqualTo(expectedPairs));
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void BfsChoosesStableRowMajorShortestWitness()
        {
            var sockets = new[]
            {
                Socket("A_LEFT", MicrochunkSide.Left, "H", true, MicrochunkToolRequirement.None),
                Socket("B_RIGHT", MicrochunkSide.Right, "H", true, MicrochunkToolRequirement.None)
            };
            var policy = new MicrochunkReachabilityPolicy(0, 0, 0, Array.Empty<string>());

            var first = Validate(Definition(CompleteCells(), sockets), policy, HorizontalBand("H", 0, 1));
            var second = Validate(Definition(CompleteCells(), sockets.Reverse()), policy, HorizontalBand("H", 0, 1));

            var expected = Enumerable.Range(0, 12).Select(x => new MicrochunkLocalCoord(x, 0)).ToArray();
            Assert.That(first.PathWitnesses.Single().Coordinates, Is.EqualTo(expected));
            Assert.That(second.PathWitnesses.Single().Coordinates, Is.EqualTo(expected));
            Assert.That(first.PathWitnesses.Single().Edges.Select(edge => edge.MovementKind),
                Is.All.EqualTo(MicrochunkTraversalEdge.FloodMovement));
        }

        [Test]
        public void SuppliedNeighborOrderBreaksEqualLengthMovementTies()
        {
            var sockets = new[]
            {
                Socket("A", MicrochunkSide.Left, "H", true, MicrochunkToolRequirement.None),
                Socket("B", MicrochunkSide.Right, "H", true, MicrochunkToolRequirement.None)
            };
            var policy = new MicrochunkReachabilityPolicy(
                0,
                0,
                0,
                Array.Empty<string>(),
                new[]
                {
                    MicrochunkTraversalEdge.WalkMovement,
                    MicrochunkTraversalEdge.FloodMovement
                });

            var result = Validate(Definition(CompleteCells(), sockets), policy, HorizontalBand("H", 0, 0));

            Assert.That(result.PathWitnesses.Single().Edges.Select(edge => edge.MovementKind),
                Is.All.EqualTo(MicrochunkTraversalEdge.WalkMovement));
        }

        [Test]
        public void GraphPublishesFloodWalkJumpDropClimbAndSocketEntryEdges()
        {
            var cells = CompleteCells();
            cells[13] = Cell(1, 1, MicrochunkTileLayer.Marker, "M_LADDER");
            var sockets = new[]
            {
                Socket("SOCK_L", MicrochunkSide.Left, "H", true, MicrochunkToolRequirement.None)
            };
            var policy = new MicrochunkReachabilityPolicy(2, 2, 3, new[] { "M_LADDER" });

            var result = Validate(
                Definition(cells, sockets),
                policy,
                HorizontalBand("H", 0, 0));

            Assert.That(result.Edges.Select(edge => edge.MovementKind).Distinct(), Is.SupersetOf(new[]
            {
                "FLOOD", "WALK", "JUMP", "DROP", "CLIMB", "SOCKET_ENTRY"
            }));
            Assert.That(result.Edges.Where(edge => edge.MovementKind != "SOCKET_ENTRY")
                .Select(edge => edge.Cost), Is.All.EqualTo(1));
            Assert.That(result.Edges.Where(edge => edge.MovementKind == "SOCKET_ENTRY")
                .Select(edge => edge.Cost), Is.All.EqualTo(0));
        }

        [Test]
        public void CompleteBlockingWallReportsUnreachablePair()
        {
            var cells = CompleteCells();
            for (var y = 0; y < MicrochunkConstants.HeightTiles; y++)
            {
                cells[(y * MicrochunkConstants.WidthTiles) + 5] =
                    Cell(5, y, MicrochunkTileLayer.GroundSolid, "WALL");
            }
            var sockets = new[]
            {
                Socket("A_LEFT", MicrochunkSide.Left, "H", true, MicrochunkToolRequirement.None),
                Socket("B_RIGHT", MicrochunkSide.Right, "H", true, MicrochunkToolRequirement.None)
            };
            var policy = new MicrochunkReachabilityPolicy(0, 0, 0, Array.Empty<string>());

            var result = Validate(Definition(cells, sockets), policy, HorizontalBand("H", 0, 0));

            Assert.That(result.EvaluatedPairCount, Is.EqualTo(1));
            Assert.That(result.ReachablePairCount, Is.Zero);
            Assert.That(result.PathWitnesses, Is.Empty);
            Assert.That(result.Violations.Single().Reason, Is.EqualTo("MANDATORY_SOCKET_PAIR_UNREACHABLE"));
        }

        [Test]
        public void EntryFailureAlsoPreventsFalsePairSuccess()
        {
            var sockets = new[]
            {
                Socket("A_MISSING", MicrochunkSide.Left, "MISSING", true, MicrochunkToolRequirement.None),
                Socket("B_RIGHT", MicrochunkSide.Right, "H", true, MicrochunkToolRequirement.None)
            };

            var result = Validate(Definition(CompleteCells(), sockets), HorizontalBand("H", 0, 0));

            Assert.That(result.EvaluatedPairCount, Is.EqualTo(1));
            Assert.That(result.ReachablePairCount, Is.Zero);
            Assert.That(result.Violations.Select(value => value.Reason), Is.EquivalentTo(new[]
            {
                "MANDATORY_SOCKET_ENTRY_UNREACHABLE",
                "MANDATORY_SOCKET_PAIR_UNREACHABLE"
            }));
        }

        [Test]
        public void CompleteCoverageGateFailurePreventsGraphAndPathSuccess()
        {
            var draft = Definition(
                new[] { EmptyCell(0, 0) },
                new[]
                {
                    Socket("A", MicrochunkSide.Left, "H", true, MicrochunkToolRequirement.None),
                    Socket("B", MicrochunkSide.Right, "H", true, MicrochunkToolRequirement.None)
                },
                false);

            var result = Validate(draft, HorizontalBand("H", 0, 0));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Nodes, Is.Empty);
            Assert.That(result.Edges, Is.Empty);
            Assert.That(result.EvaluatedSocketCount, Is.EqualTo(2));
            Assert.That(result.EvaluatedPairCount, Is.Zero);
            Assert.That(result.Violations.Single().Reason, Is.EqualTo("CELL_COVERAGE_INVALID"));
        }

        [Test]
        public void PartialPolicySuccessCannotMasqueradeAsCompleteCoverage()
        {
            var draft = Definition(new[] { EmptyCell(0, 0) }, Array.Empty<MicrochunkSocketDefinition>(), false);
            var partialResult = new Microchunk96CellValidator().ValidateDefinition(
                draft,
                Microchunk96CellValidationPolicy.Partial);

            var result = new MicrochunkReachabilityProbe().ValidateDefinition(
                draft,
                Array.Empty<MicrochunkSocketBandDefinition>(),
                partialResult);

            Assert.That(partialResult.Success, Is.True);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Violations.Single().Reason, Is.EqualTo("CELL_COVERAGE_INVALID"));
        }

        [Test]
        public void SuccessfulPriorCoverageResultCanBeSuppliedWithoutMutation()
        {
            var definition = Definition(CompleteCells(), Array.Empty<MicrochunkSocketDefinition>());
            var prior = new Microchunk96CellValidator().ValidateDefinition(
                definition,
                Microchunk96CellValidationPolicy.Complete);
            var originalViolations = prior.Violations.ToArray();

            var result = new MicrochunkReachabilityProbe().ValidateDefinition(
                definition,
                Array.Empty<MicrochunkSocketBandDefinition>(),
                prior,
                MicrochunkReachabilityPolicy.Default);

            Assert.That(result.Success, Is.True);
            Assert.That(prior.Success, Is.True);
            Assert.That(prior.Violations, Is.EqualTo(originalViolations));
            Assert.That(prior.EvaluatedRecordCount, Is.EqualTo(96));
        }

        [TestCaseSource(nameof(ApprovedTransforms))]
        public void AllApprovedTransformsPreserveReachableFixture(MicrochunkTransform transform)
        {
            var source = Definition(
                CompleteCells(),
                new[]
                {
                    Socket("A", MicrochunkSide.Left, "H", true, MicrochunkToolRequirement.None),
                    Socket("B", MicrochunkSide.Right, "H", true, MicrochunkToolRequirement.None)
                });
            var transformed = MicrochunkTransformer.Transform(source, transform).Definition;

            var result = Validate(transformed, HorizontalBand("H", 0, 7));

            Assert.That(result.Success, Is.True);
            Assert.That(result.ReachablePairCount, Is.EqualTo(1));
            Assert.That(result.PathWitnesses, Has.Count.EqualTo(1));
        }

        [Test]
        public void ValidationDoesNotMutateDefinitionCellsSocketsBandsOrPolicy()
        {
            var cells = CompleteCells();
            var sockets = new[]
            {
                Socket("B", MicrochunkSide.Right, "H", true, MicrochunkToolRequirement.None),
                Socket("A", MicrochunkSide.Left, "H", true, MicrochunkToolRequirement.None)
            };
            var definition = Definition(cells, sockets);
            var band = HorizontalBand("H", 0, 1);
            var bands = new List<MicrochunkSocketBandDefinition> { band };
            var markers = new List<string> { "M_CLIMB" };
            var policy = new MicrochunkReachabilityPolicy(1, 1, 1, markers);
            var cellSnapshot = definition.TileCells.Select(CellSignature).ToArray();
            var socketSnapshot = definition.Sockets.Select(SocketSignature).ToArray();
            var markerSnapshot = policy.ClimbMarkerCodes.ToArray();

            var result = Validate(definition, policy, bands.ToArray());

            Assert.That(result.Success, Is.True);
            Assert.That(definition.TileCells.Select(CellSignature), Is.EqualTo(cellSnapshot));
            Assert.That(definition.Sockets.Select(SocketSignature), Is.EqualTo(socketSnapshot));
            Assert.That(bands, Has.Count.EqualTo(1));
            Assert.That(bands[0], Is.SameAs(band));
            Assert.That(policy.ClimbMarkerCodes, Is.EqualTo(markerSnapshot));
        }

        [Test]
        public void ViolationsUseRequiredStableOrdering()
        {
            var values = new[]
            {
                new MicrochunkReachabilityViolation(DefaultId, "B", "", new MicrochunkLocalCoord(2, 0), "Z"),
                new MicrochunkReachabilityViolation(DefaultId, "A", "C", null, "B"),
                new MicrochunkReachabilityViolation(DefaultId, "A", "B", null, "C"),
                new MicrochunkReachabilityViolation(DefaultId, "A", "B", new MicrochunkLocalCoord(3, 0), "A"),
                new MicrochunkReachabilityViolation(DefaultId, "A", "B", new MicrochunkLocalCoord(1, 0), "A")
            };

            var result = new MicrochunkReachabilityResult(
                3, 3, 0, values.Reverse(), Array.Empty<MicrochunkReachabilityPathWitness>());

            Assert.That(result.Violations.Select(value =>
                    value.SocketId + "|" + value.PairedSocketId + "|" + value.Reason + "|" +
                    (value.Coordinate.HasValue ? value.Coordinate.Value.RowMajorIndex : -1)),
                Is.EqualTo(new[] { "A|B|A|1", "A|B|A|3", "A|B|C|-1", "A|C|B|-1", "B||Z|2" }));
        }

        [Test]
        public void InvalidInputsAndPolicyValuesAreRejected()
        {
            var probe = new MicrochunkReachabilityProbe();
            var definition = Definition(CompleteCells(), Array.Empty<MicrochunkSocketDefinition>());

            Assert.Throws<ArgumentNullException>(() => probe.ValidateDefinition(null, Array.Empty<MicrochunkSocketBandDefinition>()));
            Assert.Throws<ArgumentNullException>(() => probe.ValidateDefinition(definition, null));
            Assert.Throws<ArgumentNullException>(() => probe.ValidateDefinition(definition, Array.Empty<MicrochunkSocketBandDefinition>(), (MicrochunkReachabilityPolicy)null));
            Assert.Throws<ArgumentException>(() => probe.ValidateDefinition(
                definition,
                new MicrochunkSocketBandDefinition[] { null }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MicrochunkReachabilityPolicy(-1, 0, 0, Array.Empty<string>()));
            Assert.Throws<ArgumentException>(() => new MicrochunkReachabilityPolicy(0, 0, 0, new[] { "NONE" }));
            Assert.Throws<ArgumentException>(() => new MicrochunkReachabilityPolicy(
                0, 0, 0, Array.Empty<string>(), new[] { "walk" }));
        }

        [TestCase("MicrochunkAuthoringWindow")]
        [TestCase("MicrochunkAuthoringGrid")]
        [TestCase("MicrochunkSocketAndSlotEditor")]
        [TestCase("MicrochunkCsvImporter")]
        [TestCase("MicrochunkCsvExporter")]
        [TestCase("MicrochunkPreviewReport")]
        [TestCase("BoundaryChunkResolver")]
        [TestCase("SectorRecipeResolver")]
        [TestCase("GeneratedSectorMicrochunkWriter")]
        [TestCase("PopulationSlotIndex")]
        [TestCase("StableSpawnId")]
        [TestCase("WorldTraversalValidator")]
        public void Map0708PlusProductionSymbolsRemainAbsent(string typeName)
        {
            var assembly = typeof(MicrochunkReachabilityProbe).Assembly;
            Assert.That(assembly.GetTypes().Any(value => value.Name == typeName), Is.False, typeName);
        }

        private static MicrochunkReachabilityResult Validate(
            MicrochunkDefinition definition,
            params MicrochunkSocketBandDefinition[] bands)
        {
            return new MicrochunkReachabilityProbe().ValidateDefinition(definition, bands);
        }

        private static MicrochunkReachabilityResult Validate(
            MicrochunkDefinition definition,
            MicrochunkReachabilityPolicy policy,
            params MicrochunkSocketBandDefinition[] bands)
        {
            return new MicrochunkReachabilityProbe().ValidateDefinition(definition, bands, policy);
        }

        private static MicrochunkDefinition Definition(
            IEnumerable<MicrochunkTileCell> cells,
            IEnumerable<MicrochunkSocketDefinition> sockets,
            bool complete = true)
        {
            return new MicrochunkDefinition(
                DefaultId,
                "Reachability Test",
                MicrochunkConstants.WidthTiles,
                MicrochunkConstants.HeightTiles,
                MicrochunkUsageClass.Traversal,
                new[] { "BIO_TEST" },
                new[] { "MANDATORY" },
                new[]
                {
                    MicrochunkTransform.R0,
                    MicrochunkTransform.MirrorX,
                    MicrochunkTransform.MirrorY,
                    MicrochunkTransform.R180
                },
                100,
                0,
                0,
                0,
                complete,
                "PREFAB_REACHABILITY_TEST",
                true,
                string.Empty,
                cells,
                sockets,
                Array.Empty<MicrochunkObjectSlotDefinition>());
        }

        private static List<MicrochunkTileCell> CompleteCells()
        {
            var values = new List<MicrochunkTileCell>(MicrochunkConstants.CellCount);
            for (var y = 0; y < MicrochunkConstants.HeightTiles; y++)
            for (var x = 0; x < MicrochunkConstants.WidthTiles; x++)
            {
                values.Add(EmptyCell(x, y));
            }
            return values;
        }

        private static MicrochunkTileCell EmptyCell(int x, int y)
        {
            return new MicrochunkTileCell(
                new MicrochunkLocalCoord(x, y),
                "NONE", "NONE", "NONE", "NONE", "NONE", "NONE", "NONE", "NONE");
        }

        private static MicrochunkTileCell Cell(
            int x,
            int y,
            MicrochunkTileLayer layer,
            string code)
        {
            var values = Enumerable.Repeat("NONE", MicrochunkConstants.LayerCount).ToArray();
            values[(int)layer] = code;
            return new MicrochunkTileCell(
                new MicrochunkLocalCoord(x, y),
                values[0], values[1], values[2], values[3],
                values[4], values[5], values[6], values[7]);
        }

        private static MicrochunkSocketDefinition Socket(
            string id,
            MicrochunkSide side,
            string bandId,
            bool mandatoryAllowed,
            MicrochunkToolRequirement toolRequirement)
        {
            return new MicrochunkSocketDefinition(
                id,
                side,
                bandId,
                MicrochunkTraversalKind.Walk,
                "BIDIRECTIONAL",
                mandatoryAllowed,
                toolRequirement,
                "EDGE_TEST",
                MicrochunkRouteLayer.Both,
                0,
                string.Empty);
        }

        private static MicrochunkSocketBandDefinition HorizontalBand(string id, int minimum, int maximum)
        {
            return new MicrochunkSocketBandDefinition(
                id,
                MicrochunkEdgeAxis.HorizontalEdge,
                minimum,
                maximum,
                minimum,
                0,
                string.Empty);
        }

        private static MicrochunkSocketBandDefinition VerticalBand(string id, int minimum, int maximum)
        {
            return new MicrochunkSocketBandDefinition(
                id,
                MicrochunkEdgeAxis.VerticalEdge,
                minimum,
                maximum,
                minimum,
                0,
                string.Empty);
        }

        private static string CellSignature(MicrochunkTileCell value)
        {
            return value.Coordinate.RowMajorIndex + "|" + value.GroundCode + "|" + value.OneWayCode + "|" +
                   value.BreakableCode + "|" + value.HazardCode + "|" + value.LiquidCode + "|" +
                   value.DecorationBackCode + "|" + value.DecorationFrontCode + "|" + value.MarkerCode;
        }

        private static string SocketSignature(MicrochunkSocketDefinition value)
        {
            return value.SocketId + "|" + value.Side + "|" + value.BandId + "|" + value.MandatoryAllowed + "|" +
                   value.ToolRequirement + "|" + value.TraversalKind + "|" + value.Direction + "|" +
                   value.EdgeSignatureId + "|" + value.RouteLayer + "|" + value.MinimumSafeTiles;
        }
    }
}
