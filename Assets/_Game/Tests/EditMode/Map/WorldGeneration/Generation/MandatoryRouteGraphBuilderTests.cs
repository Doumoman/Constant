using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    [Category("MAP05_08")]
    public sealed class MandatoryRouteGraphBuilderTests
    {
        private MandatoryRouteTerminalSet terminals;
        private MandatoryRouteMaskLookup lookup;
        private MandatoryConnectorTree tree;
        private HorizontalBackbonePlan horizontal;
        private VerticalGatewayPlan vertical;
        private UpDownConflictResolutionPlan conflicts;
        private MandatoryRouteLoopPlan loops;
        private SiteReservationSnapshot site;
        private BiomePatchValidationPublication biome;
        private MandatoryRouteGraphBuilder reused;
        private MandatoryRouteGraphBuildResult baseline;
        private string expectedSignature;
        private string sourceSignature;

        public static IEnumerable DeterminismCases
        {
            get
            {
                for (var index = 0; index < 220; index++)
                    yield return new TestCaseData(index).SetName("Build_DeterministicMandatoryGraph_" + index.ToString("D3", CultureInfo.InvariantCulture));
            }
        }

        public static IEnumerable<string> InvalidNodeIds => new[]
        {
            null, string.Empty, "NODE_00_A", "NODE_000_", "node_000_A", "NODE_A00_A",
            "NODE_000_a", "NODE_000_A-B", "NODE000_A", "NODE_000_A B", "NODE_000_A/B", "NODE_000_한글"
        };

        public static IEnumerable<string> InvalidEdgeIds => new[]
        {
            null, string.Empty, "EDGE_00_L_A", "EDGE_000_X_A", "EDGE_000_L_", "edge_000_L_A",
            "EDGE_A00_L_A", "EDGE_000_L_a", "EDGE_000_L_A-B", "EDGE000_L_A", "EDGE_000_L_A B", "EDGE_000_L_한글"
        };

        public static IEnumerable Type4Cases
        {
            get
            {
                yield return new TestCaseData(false, false, MandatoryRouteMaskFamily.Type4UdId);
                yield return new TestCaseData(true, false, MandatoryRouteMaskFamily.Type4LudId);
                yield return new TestCaseData(false, true, MandatoryRouteMaskFamily.Type4RudId);
                yield return new TestCaseData(true, true, MandatoryRouteMaskFamily.Type4LrudId);
            }
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var fixture = new MandatoryRouteLoopPlannerTests();
            fixture.OneTimeSetUp();
            var type = typeof(MandatoryRouteLoopPlannerTests);
            terminals = GetField<MandatoryRouteTerminalSet>(fixture, type, "terminalSet");
            lookup = GetField<MandatoryRouteMaskLookup>(fixture, type, "lookup");
            tree = GetField<MandatoryConnectorTree>(fixture, type, "tree");
            horizontal = GetField<HorizontalBackbonePlan>(fixture, type, "horizontal");
            vertical = GetField<VerticalGatewayPlan>(fixture, type, "vertical");
            conflicts = GetField<UpDownConflictResolutionPlan>(fixture, type, "conflicts");
            site = GetField<SiteReservationSnapshot>(fixture, type, "site");
            biome = GetField<BiomePatchValidationPublication>(fixture, type, "biome");
            loops = new MandatoryRouteLoopPlanner().Build(terminals, tree, horizontal, vertical, conflicts).Plan;
            Assert.That(loops, Is.Not.Null);
            reused = new MandatoryRouteGraphBuilder();
            baseline = Complete(reused);
            expectedSignature = Signature(baseline);
            sourceSignature = SourceSignature();
        }

        [TestCaseSource(nameof(DeterminismCases))]
        public void BuildIsCultureFreshReuseAndThreadOrderIndependent(int caseId)
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = (caseId & 1) == 0 ? CultureInfo.GetCultureInfo("en-US") : CultureInfo.GetCultureInfo("tr-TR");
                var result = Complete((caseId & 2) == 0 ? new MandatoryRouteGraphBuilder() : reused);
                Assert.That(Signature(result), Is.EqualTo(expectedSignature));
                Assert.That(SourceSignature(), Is.EqualTo(sourceSignature));
            }
            finally { CultureInfo.CurrentCulture = previous; }
        }

        [TestCaseSource(nameof(InvalidNodeIds))]
        public void NodeIdRejectsNonCanonicalValues(string value)
        {
            Assert.That(MandatoryRouteGraphNodeId.TryCreate(value, out var parsed), Is.False);
            Assert.That(parsed.IsValid, Is.False);
            if (value == null) Assert.Throws<ArgumentNullException>(() => new MandatoryRouteGraphNodeId(value));
            else Assert.Throws<ArgumentException>(() => new MandatoryRouteGraphNodeId(value));
        }

        [TestCaseSource(nameof(InvalidEdgeIds))]
        public void EdgeIdRejectsNonCanonicalValues(string value)
        {
            Assert.That(MandatoryRouteGraphEdgeId.TryCreate(value, out var parsed), Is.False);
            Assert.That(parsed.IsValid, Is.False);
            if (value == null) Assert.Throws<ArgumentNullException>(() => new MandatoryRouteGraphEdgeId(value));
            else Assert.Throws<ArgumentException>(() => new MandatoryRouteGraphEdgeId(value));
        }

        [TestCase("NODE_000_MANDATORY")]
        [TestCase("NODE_007_ROUTE_A")]
        [TestCase("NODE_168_Z9")]
        public void NodeIdHasOrdinalValueEqualityAndHash(string value)
        {
            var first = new MandatoryRouteGraphNodeId(value);
            var copy = new MandatoryRouteGraphNodeId(new string(value.ToCharArray()));
            Assert.That(first, Is.EqualTo(copy));
            Assert.That(first.CompareTo(copy), Is.Zero);
            Assert.That(first.GetHashCode(), Is.EqualTo(copy.GetHashCode()));
            Assert.That(first.Value, Is.EqualTo(value));
        }

        [TestCase("EDGE_000_L_MANDATORY")]
        [TestCase("EDGE_042_U_ROUTE_A")]
        [TestCase("EDGE_999_D_Z9")]
        public void EdgeIdHasOrdinalValueEqualityAndHash(string value)
        {
            var first = new MandatoryRouteGraphEdgeId(value);
            var copy = new MandatoryRouteGraphEdgeId(new string(value.ToCharArray()));
            Assert.That(first, Is.EqualTo(copy));
            Assert.That(first.CompareTo(copy), Is.Zero);
            Assert.That(first.GetHashCode(), Is.EqualTo(copy.GetHashCode()));
            Assert.That(first.Value, Is.EqualTo(value));
        }

        [TestCaseSource(nameof(Type4Cases))]
        public void Type4FamilyResolvesAllFourIndependentHorizontalCombinations(bool left, bool right, string expectedId)
        {
            var family = baseline.Graph.MaskFamily;
            Assert.That(family.TryResolve(left, right, true, true, out var entry), Is.True);
            Assert.That(entry.MaskId, Is.EqualTo(expectedId));
            Assert.That(entry.RouteType, Is.EqualTo(4));
            Assert.That(entry.OpenLeft, Is.EqualTo(left));
            Assert.That(entry.OpenRight, Is.EqualTo(right));
            Assert.That(entry.OpenUp && entry.OpenDown, Is.True);
        }

        [Test]
        public void StarterPublishesExactGraphCardinalityAndDiagnostics()
        {
            var diagnostics = baseline.Diagnostics;
            Assert.That(baseline.Status, Is.EqualTo(MandatoryRouteGraphBuildStatus.Completed));
            Assert.That(baseline.Succeeded && !baseline.RetryRequired, Is.True);
            Assert.That(baseline.Errors, Is.Empty);
            Assert.That(new[] { diagnostics.TerminalCount, diagnostics.TreeEdgeCount, diagnostics.BackboneSegmentCount,
                diagnostics.GatewayPairCount, diagnostics.ConflictResolutionCount, diagnostics.AcceptedLoopCount },
                Is.EqualTo(new[] { 7, 6, 6, 4, 0, 2 }));
            Assert.That(new[] { diagnostics.NodeCount, diagnostics.DirectedEdgeCount, diagnostics.UndirectedEdgeCount, diagnostics.CellCount },
                Is.EqualTo(new[] { 47, 96, 48, 47 }));
        }

        [Test]
        public void ExactStarterMaskCountsPreserveType4WithoutCanonicalization()
        {
            var d = baseline.Diagnostics;
            Assert.That(new[] { d.Type1Count, d.Type2Count, d.Type3Count, d.Type4UdCount, d.Type4LudCount, d.Type4RudCount, d.Type4LrudCount },
                Is.EqualTo(new[] { 20, 4, 4, 17, 0, 0, 2 }));
            Assert.That(d.Type4Count, Is.EqualTo(19));
            Assert.That(baseline.Graph.Cells.Where(value => value.Mask.RouteType == 4).All(value => value.OpenUp && value.OpenDown), Is.True);
            Assert.That(baseline.Graph.Cells.Where(value => value.Mask.RouteType == 4)
                .All(value => value.RouteMaskId == ResolveType4Id(value.OpenLeft, value.OpenRight)), Is.True);
        }

        [Test]
        public void MaskFamilyRegistersExactSevenIdsAndAgreesWithLookup()
        {
            var family = baseline.Graph.MaskFamily;
            Assert.That(family.Count, Is.EqualTo(7));
            Assert.That(family.Entries.Select(value => value.MaskId), Is.EqualTo(new[]
            {
                MandatoryRouteMaskFamily.Type1Id, MandatoryRouteMaskFamily.Type2Id, MandatoryRouteMaskFamily.Type3Id,
                MandatoryRouteMaskFamily.Type4UdId, MandatoryRouteMaskFamily.Type4LudId,
                MandatoryRouteMaskFamily.Type4RudId, MandatoryRouteMaskFamily.Type4LrudId
            }));
            Assert.That(family.Entries.Take(3).Select(value => value.MaskId),
                Is.EqualTo(lookup.Records.Select(value => value.MaskId.Value)));
            Assert.That(family.TryResolve(true, false, false, false, out _), Is.False);
        }

        [Test]
        public void GraphPreservesExactSevenSourceArtifactIdentities()
        {
            var graph = baseline.Graph;
            Assert.That(graph.SourceTerminalSet, Is.SameAs(terminals));
            Assert.That(graph.SourceRouteMaskLookup, Is.SameAs(lookup));
            Assert.That(graph.SourceConnectorTree, Is.SameAs(tree));
            Assert.That(graph.SourceHorizontalBackbonePlan, Is.SameAs(horizontal));
            Assert.That(graph.SourceVerticalGatewayPlan, Is.SameAs(vertical));
            Assert.That(graph.SourceConflictResolutionPlan, Is.SameAs(conflicts));
            Assert.That(graph.SourceLoopPlan, Is.SameAs(loops));
        }

        [Test]
        public void EveryDirectedEdgeHasExactReverseWithMatchingContract()
        {
            var edges = baseline.Graph.Edges;
            Assert.That(MandatoryRouteGraphBuilder.HasExactReciprocity(edges), Is.True);
            Assert.That(MandatoryRouteGraphBuilder.HasExactReciprocity(edges.Take(edges.Count - 1)), Is.False);
            foreach (var edge in edges)
            {
                var reverse = edges.Single(value => value.FromSectorIndex == edge.ToSectorIndex && value.ToSectorIndex == edge.FromSectorIndex);
                Assert.That(reverse.Side, Is.EqualTo(edge.ReverseSide));
                Assert.That(reverse.ReverseSide, Is.EqualTo(edge.Side));
                Assert.That(reverse.CostTiles, Is.EqualTo(edge.CostTiles));
                Assert.That(reverse.SourceArtifactId, Is.EqualTo(edge.SourceArtifactId));
            }
        }

        [Test]
        public void DirectedEdgesHaveExactLayerTraversalSignatureAndCost()
        {
            foreach (var edge in baseline.Graph.Edges)
            {
                Assert.That(edge.Layer, Is.EqualTo("MANDATORY"));
                Assert.That(edge.Open, Is.True);
                Assert.That(edge.SourceArtifactId, Is.Not.Empty);
                var horizontalEdge = edge.Side == "L" || edge.Side == "R";
                Assert.That(edge.TraversalKind, Is.EqualTo(horizontalEdge ? "WALK" : "DROP_CLIMB_PAIR"));
                Assert.That(edge.EdgeSignatureId, Is.EqualTo(horizontalEdge ? "EDGE_H_MID_WALK" : "EDGE_V_CENTER_CLIMB"));
                Assert.That(edge.CostTiles, Is.EqualTo(horizontalEdge ? WorldGenConstants.SectorWidthTiles : WorldGenConstants.SectorHeightTiles));
            }
        }

        [Test]
        public void EdgeAndNodeOrderingAreStableAndCanonical()
        {
            Assert.That(baseline.Graph.Nodes.Select(value => value.SectorIndex), Is.Ordered);
            Assert.That(baseline.Graph.Edges.Select(value => value.EdgeId.Value), Is.Ordered);
            for (var index = 0; index < baseline.Graph.Edges.Count; index++)
                Assert.That(baseline.Graph.Edges[index].EdgeId.Value, Does.StartWith("EDGE_" + index.ToString("D3", CultureInfo.InvariantCulture) + "_"));
            Assert.That(baseline.Graph.Nodes.All(value => value.NodeId.IsValid && value.MandatoryGraphNode), Is.True);
        }

        [Test]
        public void GeneratedEdgeCsvMatchesExactByteAndTokenContract()
        {
            var bytes = baseline.Graph.GeneratedWorldEdgesCsv;
            Assert.That(bytes.Take(3), Is.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }));
            Assert.That(bytes.Length, Is.EqualTo(7094));
            var text = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
            Assert.That(text, Does.StartWith(GeneratedWorldEdgesCsvSerializer.Header + "\r\n"));
            Assert.That(text, Does.EndWith("\r\n"));
            Assert.That(HasBareNewline(text), Is.False);
            var rows = text.Split(new[] { "\r\n" }, StringSplitOptions.None);
            Assert.That(rows[0].Split(',').Length, Is.EqualTo(11));
            Assert.That(rows.Length, Is.EqualTo(98));
            foreach (var row in rows.Skip(1).Take(96))
            {
                var fields = row.Split(',');
                Assert.That(fields.Length, Is.EqualTo(11));
                Assert.That(fields[6], Is.EqualTo("MANDATORY"));
                Assert.That(fields[7], Is.EqualTo("WALK").Or.EqualTo("DROP_CLIMB_PAIR"));
                Assert.That(fields[8], Is.EqualTo("1"));
                Assert.That(int.Parse(fields[10], CultureInfo.InvariantCulture), Is.GreaterThanOrEqualTo(0));
            }
        }

        [Test]
        public void GeneratedEdgeSerializerSortsCallerOrderAndReturnsIdenticalBytes()
        {
            var rows = ToGeneratedRows(baseline.Graph);
            var forward = GeneratedWorldEdgesCsvSerializer.Serialize(rows);
            rows.Reverse();
            var reverse = GeneratedWorldEdgesCsvSerializer.Serialize(rows);
            Assert.That(reverse, Is.EqualTo(forward));
            Assert.That(forward, Is.EqualTo(baseline.Graph.GeneratedWorldEdgesCsv));
            Assert.That(GeneratedWorldEdgesCsvSerializer.FileName, Is.EqualTo("generated_world_edges.csv"));
        }

        [Test]
        public void RouteStampedWorldPreservesExact169CellSectorCsvV1Contract()
        {
            var graph = baseline.Graph;
            Assert.That(graph.RouteStampedWorld, Is.Not.SameAs(biome.WorldWithBiomeAssignments));
            Assert.That(graph.RouteStampedWorld.Cells.Count, Is.EqualTo(169));
            var bytes = GeneratedWorldDataCsvSerializer.Serialize(graph.RouteStampedWorld);
            Assert.That(bytes.Length, Is.EqualTo(16838));
            var text = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
            Assert.That(text.Split(new[] { "\r\n" }, StringSplitOptions.None)[0].Split(',').Length, Is.EqualTo(13));
            Assert.That(GeneratedWorldDataCsvSerializer.Header.Split(',').Length, Is.EqualTo(13));
            Assert.That(GeneratedWorldDataCsvSerializer.Serialize(graph.RouteStampedWorld), Is.EqualTo(bytes));
        }

        [Test]
        public void RouteStampChangesOnlyAllowedFieldsAndPreservesReservedRole()
        {
            foreach (var cell in baseline.Graph.Cells)
            {
                var source = cell.SourceCell;
                var stamped = cell.StampedCell;
                Assert.That(stamped.Index, Is.EqualTo(source.Index));
                Assert.That(stamped.Coordinate, Is.EqualTo(source.Coordinate));
                Assert.That(stamped.PrimaryBiomeId, Is.EqualTo(source.PrimaryBiomeId));
                Assert.That(stamped.SecondaryBiomeId, Is.EqualTo(source.SecondaryBiomeId));
                Assert.That(stamped.PatchId, Is.EqualTo(source.PatchId));
                Assert.That(stamped.SpecialSiteInstanceId, Is.EqualTo(source.SpecialSiteInstanceId));
                Assert.That(stamped.BoundaryProfileId, Is.EqualTo(source.BoundaryProfileId));
                Assert.That(stamped.SectorRecipeId, Is.EqualTo(source.SectorRecipeId));
                Assert.That(stamped.ReservationId, Is.EqualTo(source.ReservationId));
                Assert.That(stamped.RouteMaskId, Is.EqualTo(cell.Mask.MaskId));
                Assert.That(stamped.MandatoryGraphNode, Is.True);
                Assert.That(stamped.ShortestDistanceFromStart, Is.GreaterThanOrEqualTo(0));
                Assert.That(stamped.Role, Is.EqualTo(source.Role == GeneratedSectorRole.ReservedSite ? GeneratedSectorRole.ReservedSite : GeneratedSectorRole.Mandatory));
            }
        }

        [Test]
        public void NonRouteCellsArePreservedByExactReference()
        {
            var route = new HashSet<int>(baseline.Graph.Cells.Select(value => value.SectorIndex));
            var source = biome.WorldWithBiomeAssignments;
            foreach (var cell in baseline.Graph.RouteStampedWorld.Cells)
                if (!route.Contains(cell.Index)) Assert.That(cell, Is.SameAs(source.GetCell(cell.Index)));
        }

        [Test]
        public void StartBfsReachesEveryTerminalAndLoopAnchor()
        {
            var graph = baseline.Graph;
            Assert.That(graph.TryGetNode(terminals.StartTerminal.ApproachSector, out var start), Is.True);
            Assert.That(start.ShortestDistanceFromStart, Is.Zero);
            foreach (var terminal in terminals.Terminals)
            {
                Assert.That(graph.TryGetNode(terminal.ApproachSector, out var node), Is.True, terminal.TerminalId.Value);
                Assert.That(node.ShortestDistanceFromStart, Is.GreaterThanOrEqualTo(0));
            }
            foreach (var loop in loops.Loops)
            {
                Assert.That(graph.TryGetNode(loop.InclusiveOrderedCells[0], out var first), Is.True);
                Assert.That(graph.TryGetNode(loop.InclusiveOrderedCells[loop.InclusiveOrderedCells.Count - 1], out var last), Is.True);
                Assert.That(first.ShortestDistanceFromStart, Is.GreaterThanOrEqualTo(0));
                Assert.That(last.ShortestDistanceFromStart, Is.GreaterThanOrEqualTo(0));
            }
            Assert.That(baseline.Diagnostics.ReachableTerminalCount, Is.EqualTo(7));
        }

        [Test]
        public void GraphLookupsPreservePublishedInstances()
        {
            foreach (var node in baseline.Graph.Nodes)
            {
                Assert.That(baseline.Graph.TryGetNode(node.SectorIndex, out var byIndex), Is.True);
                Assert.That(baseline.Graph.TryGetNode(node.Coordinate, out var byCoord), Is.True);
                Assert.That(byIndex, Is.SameAs(node));
                Assert.That(byCoord, Is.SameAs(node));
            }
            foreach (var edge in baseline.Graph.Edges)
            {
                Assert.That(baseline.Graph.TryGetEdge(edge.EdgeId, out var found), Is.True);
                Assert.That(found, Is.SameAs(edge));
            }
        }

        [Test]
        public void PublishedCollectionsAndBytesAreImmutableSnapshots()
        {
            var graph = baseline.Graph;
            Assert.Throws<NotSupportedException>(() => ((IList<MandatoryRouteGraphNode>)graph.Nodes).Add(graph.Nodes[0]));
            Assert.Throws<NotSupportedException>(() => ((IList<MandatoryRouteGraphEdge>)graph.Edges).Add(graph.Edges[0]));
            Assert.Throws<NotSupportedException>(() => ((IList<MandatoryRouteGraphCell>)graph.Cells).Add(graph.Cells[0]));
            var first = graph.GeneratedWorldEdgesCsv;
            var original = first[0];
            first[0] = 0;
            Assert.That(graph.GeneratedWorldEdgesCsv[0], Is.EqualTo(original));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void NullSourceReturnsTypedInvalidInputWithoutRetry(int sourceIndex)
        {
            var result = reused.Build(sourceIndex == 0 ? null : terminals, sourceIndex == 1 ? null : lookup,
                sourceIndex == 2 ? null : tree, sourceIndex == 3 ? null : horizontal, sourceIndex == 4 ? null : vertical,
                sourceIndex == 5 ? null : conflicts, sourceIndex == 6 ? null : loops);
            Assert.That(result.Status, Is.EqualTo(MandatoryRouteGraphBuildStatus.InvalidInput));
            Assert.That(result.Graph, Is.Null);
            Assert.That(result.Diagnostics, Is.Null);
            Assert.That(result.Errors.Single().Code, Is.EqualTo(MandatoryRouteGraphBuildErrorCode.NullInput));
            Assert.That(result.RetryRequired, Is.False);
        }

        [Test]
        public void MismatchedSourceIdentityReturnsTypedError()
        {
            var otherTree = new MandatoryConnectorTreeBuilder().Build(terminals, lookup).Tree;
            var result = reused.Build(terminals, lookup, otherTree, horizontal, vertical, conflicts, loops);
            Assert.That(result.Status, Is.EqualTo(MandatoryRouteGraphBuildStatus.InvalidInput));
            Assert.That(result.Errors.Single().Code, Is.EqualTo(MandatoryRouteGraphBuildErrorCode.SourceIdentityMismatch));
        }

        [Test]
        public void FreshReusedAndParallelBuildsHaveOneSignature()
        {
            var signatures = new string[12];
            Parallel.For(0, signatures.Length, index => signatures[index] = Signature(Complete((index & 1) == 0 ? new MandatoryRouteGraphBuilder() : reused)));
            Assert.That(signatures.Distinct().Single(), Is.EqualTo(expectedSignature));
            Assert.That(SourceSignature(), Is.EqualTo(sourceSignature));
        }

        [Test]
        public void DiagnosticsFreezeZeroRngFilesystemClockAndMutation()
        {
            var d = baseline.Diagnostics;
            Assert.That(new[] { d.RngDrawCount, d.FileWriteCount, d.ClockReadCount, d.SourceMutationCount }, Is.EqualTo(new[] { 0, 0, 0, 0 }));
            Assert.That(d.GeneratedEdgeRowCount, Is.EqualTo(96));
            Assert.That(d.GeneratedSectorCsvByteCount, Is.EqualTo(16838));
            Assert.That(d.GeneratedEdgeCsvByteCount, Is.EqualTo(7094));
        }

        [Test]
        public void RuntimeSurfaceHasNoMutableStaticUnityEditorOrMap05_11PlusSymbols()
        {
            var types = new[]
            {
                typeof(MandatoryRouteMaskFamily), typeof(MandatoryRouteGraphNodeId), typeof(MandatoryRouteGraphEdgeId),
                typeof(MandatoryRouteGraphNode), typeof(MandatoryRouteGraphEdge), typeof(MandatoryRouteGraphCell),
                typeof(MandatoryRouteGraph), typeof(MandatoryRouteGraphBuildError), typeof(MandatoryRouteGraphDiagnostics),
                typeof(MandatoryRouteGraphBuildResult), typeof(MandatoryRouteGraphBuilder), typeof(GeneratedWorldEdge), typeof(GeneratedWorldEdgesCsvSerializer)
            };
            foreach (var type in types)
                Assert.That(type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(field => !field.IsLiteral && !field.IsInitOnly), Is.Empty, type.FullName);
            var assembly = typeof(MandatoryRouteGraphBuilder).Assembly;
            Assert.That(assembly.GetReferencedAssemblies().Any(value => value.Name == "UnityEditor"), Is.False);
            var names = string.Join("|", assembly.GetTypes().Select(value => value.Name));
            foreach (var forbidden in new[] { "MandatoryRoutePass", "SectorRouteMaskAssigner" })
                Assert.That(names, Does.Not.Contain(forbidden));
        }

        [Test]
        public void SourceSnapshotsRemainByteAndValueIdenticalAfterRepeatedBuilds()
        {
            var before = SourceSignature();
            var worldBytes = GeneratedWorldDataCsvSerializer.Serialize(biome.WorldWithBiomeAssignments);
            for (var index = 0; index < 4; index++) Complete(reused);
            Assert.That(SourceSignature(), Is.EqualTo(before));
            Assert.That(GeneratedWorldDataCsvSerializer.Serialize(biome.WorldWithBiomeAssignments), Is.EqualTo(worldBytes));
        }

        private MandatoryRouteGraphBuildResult Complete(MandatoryRouteGraphBuilder builder)
        {
            var result = builder.Build(terminals, lookup, tree, horizontal, vertical, conflicts, loops);
            Assert.That(result.Status, Is.EqualTo(MandatoryRouteGraphBuildStatus.Completed), FormatErrors(result));
            return result;
        }

        private string SourceSignature() =>
            terminals.TerminalCount + "|" + tree.TreeEdgeCount + "|" + horizontal.SegmentCount + ":" + horizontal.TotalCost + "|" +
            vertical.GatewayPairCount + ":" + vertical.Type4JunctionCellCount + "|" + conflicts.CandidateCount + ":" + conflicts.ResolvedCount + "|" +
            loops.CandidateCount + ":" + loops.LoopCount + ":" + loops.TotalCost + "|" + site.Sectors.Count + "|" +
            ByteHash(GeneratedWorldDataCsvSerializer.Serialize(biome.WorldWithBiomeAssignments));

        private static string Signature(MandatoryRouteGraphBuildResult result) =>
            result.Status + "|" + string.Join(",", result.Errors.Select(value => value.Code + ":" + value.SourceId + ":" + value.SectorIndex)) + "|" +
            (result.Graph == null ? "null" : string.Join("/", result.Graph.Nodes.Select(value => value.NodeId.Value + ":" + value.RouteMaskId + ":" + value.ShortestDistanceFromStart)) + "|" +
                string.Join("/", result.Graph.Edges.Select(value => value.EdgeId.Value + ":" + value.FromSectorIndex + ":" + value.ToSectorIndex + ":" + value.SourceArtifactId)) + "|" +
                ByteHash(result.Graph.GeneratedWorldEdgesCsv)) + "|" +
            (result.Diagnostics == null ? "null" : result.Diagnostics.NodeCount + ":" + result.Diagnostics.DirectedEdgeCount + ":" + result.Diagnostics.Type4Count + ":" +
                result.Diagnostics.GeneratedSectorCsvByteCount + ":" + result.Diagnostics.GeneratedEdgeCsvByteCount);

        private static List<GeneratedWorldEdge> ToGeneratedRows(MandatoryRouteGraph graph)
        {
            var values = new List<GeneratedWorldEdge>();
            foreach (var edge in graph.Edges)
                values.Add(new GeneratedWorldEdge(graph.RouteStampedWorld.Seed, WorldGridIndex.ToCoordinate(edge.FromSectorIndex), edge.Side,
                    WorldGridIndex.ToCoordinate(edge.ToSectorIndex), edge.Layer, edge.TraversalKind, edge.Open, edge.EdgeSignatureId, edge.CostTiles));
            return values;
        }

        private static string ResolveType4Id(bool left, bool right) => left ? (right ? MandatoryRouteMaskFamily.Type4LrudId : MandatoryRouteMaskFamily.Type4LudId) :
            (right ? MandatoryRouteMaskFamily.Type4RudId : MandatoryRouteMaskFamily.Type4UdId);

        private static ulong ByteHash(IEnumerable<byte> bytes)
        {
            var value = 1469598103934665603UL;
            foreach (var item in bytes) { value ^= item; value *= 1099511628211UL; }
            return value;
        }

        private static bool HasBareNewline(string text)
        {
            for (var index = 0; index < text.Length; index++)
            {
                if (text[index] == '\n' && (index == 0 || text[index - 1] != '\r')) return true;
                if (text[index] == '\r' && (index + 1 >= text.Length || text[index + 1] != '\n')) return true;
            }
            return false;
        }

        private static string FormatErrors(MandatoryRouteGraphBuildResult result) => string.Join("\n", result.Errors.Select(value => value.Code + " " + value.Message));
        private static T GetField<T>(object target, Type type, string name) => (T)type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    }
}
