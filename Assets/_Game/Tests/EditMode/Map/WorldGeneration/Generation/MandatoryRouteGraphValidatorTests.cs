using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    [Category("MAP05_09")]
    public sealed class MandatoryRouteGraphValidatorTests
    {
        private MandatoryRouteGraph graph;
        private MandatoryRouteTerminalSet terminals;
        private MandatoryRouteLoopPlan loops;
        private MandatoryRouteGraphValidator reused;
        private List<GeneratedWorldEdge> rows;
        private byte[] sectorCsv;
        private byte[] edgeCsv;
        private string expectedSignature;
        private string sourceSignature;

        public static IEnumerable DeterminismCases
        {
            get
            {
                for (var index = 0; index < 240; index++)
                    yield return new TestCaseData(index).SetName("Validate_DeterministicMandatoryRouteReport_" + index.ToString("D3", CultureInfo.InvariantCulture));
            }
        }

        public static IEnumerable<string> InvalidRuleIds => new[]
        {
            null, string.Empty, "VAL_ROUTE_", "val_ROUTE_A", "VAL_route_A", "VAL_ROUTE_a", "VAL_ROUTE_A-B",
            "VALROUTE_A", "VAL_ROUTE_A B", "VAL_ROUTE_A/B", "VAL_ROUTE_한글", "VAL_OTHER_A"
        };

        public static IEnumerable<string> RequiredRuleIds => new[]
        {
            MandatoryRouteGraphValidator.MaskFamilyRule, MandatoryRouteGraphValidator.Type4UdRequiredRule,
            MandatoryRouteGraphValidator.Type4LrPreservedRule, MandatoryRouteGraphValidator.EdgeReciprocityRule,
            MandatoryRouteGraphValidator.EdgeSideMatchRule, MandatoryRouteGraphValidator.TerminalBfsRule,
            MandatoryRouteGraphValidator.LoopRepresentedRule, MandatoryRouteGraphValidator.SectorStampRule,
            MandatoryRouteGraphValidator.GeneratedSectorCsvRule, MandatoryRouteGraphValidator.GeneratedEdgeCsvRule,
            MandatoryRouteGraphValidator.NoType0IntrusionRule, MandatoryRouteGraphValidator.SourceImmutabilityRule
        };

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var fixture = new MandatoryRouteGraphBuilderTests();
            fixture.OneTimeSetUp();
            var type = typeof(MandatoryRouteGraphBuilderTests);
            var baseline = GetField<MandatoryRouteGraphBuildResult>(fixture, type, "baseline");
            graph = baseline.Graph;
            terminals = graph.SourceTerminalSet;
            loops = graph.SourceLoopPlan;
            reused = new MandatoryRouteGraphValidator();
            rows = ToRows(graph);
            sectorCsv = GeneratedWorldDataCsvSerializer.Serialize(graph.RouteStampedWorld);
            edgeCsv = graph.GeneratedWorldEdgesCsv;
            var result = Complete(reused, graph);
            expectedSignature = Signature(result);
            sourceSignature = SourceSignature();
        }

        [TestCaseSource(nameof(DeterminismCases))]
        public void ValidationIsCultureFreshReuseAndThreadOrderIndependent(int caseId)
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = (caseId & 1) == 0 ? CultureInfo.GetCultureInfo("en-US") : CultureInfo.GetCultureInfo("tr-TR");
                var result = Complete((caseId & 2) == 0 ? new MandatoryRouteGraphValidator() : reused, graph);
                Assert.That(Signature(result), Is.EqualTo(expectedSignature));
                Assert.That(SourceSignature(), Is.EqualTo(sourceSignature));
            }
            finally { CultureInfo.CurrentCulture = previous; }
        }

        [TestCaseSource(nameof(InvalidRuleIds))]
        public void RuleIdRejectsNonCanonicalValues(string value)
        {
            Assert.That(MandatoryRouteValidationRuleId.TryCreate(value, out var parsed), Is.False);
            Assert.That(parsed.IsValid, Is.False);
            if (value == null) Assert.Throws<ArgumentNullException>(() => new MandatoryRouteValidationRuleId(value));
            else Assert.Throws<ArgumentException>(() => new MandatoryRouteValidationRuleId(value));
        }

        [TestCaseSource(nameof(RequiredRuleIds))]
        public void RequiredRuleIdsHaveOrdinalValueSemantics(string value)
        {
            var first = new MandatoryRouteValidationRuleId(value);
            var copy = new MandatoryRouteValidationRuleId(new string(value.ToCharArray()));
            Assert.That(first.IsValid, Is.True);
            Assert.That(first, Is.EqualTo(copy));
            Assert.That(first.CompareTo(copy), Is.Zero);
            Assert.That(first.GetHashCode(), Is.EqualTo(copy.GetHashCode()));
        }

        [TestCase(false, false, MandatoryRouteMaskFamily.Type4UdId)]
        [TestCase(true, false, MandatoryRouteMaskFamily.Type4LudId)]
        [TestCase(false, true, MandatoryRouteMaskFamily.Type4RudId)]
        [TestCase(true, true, MandatoryRouteMaskFamily.Type4LrudId)]
        public void AllFourType4HorizontalCombinationsRemainLegal(bool left, bool right, string maskId)
        {
            Assert.That(graph.MaskFamily.TryResolve(left, right, true, true, out var mask), Is.True);
            Assert.That(mask.MaskId, Is.EqualTo(maskId));
            Assert.That(mask.OpenLeft, Is.EqualTo(left));
            Assert.That(mask.OpenRight, Is.EqualTo(right));
            Assert.That(mask.OpenUp && mask.OpenDown, Is.True);
            Assert.That(Complete(reused, graph).Report.IsValid, Is.True);
        }

        [Test]
        public void StarterPublishesExactPassRouteReport()
        {
            var result = Complete(reused, graph);
            var report = result.Report;
            var summary = report.Summary;
            Assert.That(result.Status, Is.EqualTo(MandatoryRouteValidationStatus.Completed));
            Assert.That(result.Succeeded && !result.RetryRequired, Is.True);
            Assert.That(report.PassId, Is.EqualTo("PASS_ROUTE"));
            Assert.That(report.Violations, Is.Empty);
            Assert.That(report.Errors, Is.Empty);
            Assert.That(report.Warnings, Is.Empty);
            Assert.That(new[] { summary.RuleCount, summary.PassedRuleCount, summary.FailedRuleCount }, Is.EqualTo(new[] { 12, 12, 0 }));
            Assert.That(new[] { graph.NodeCount, summary.DirectedEdgeCount, summary.UndirectedEdgeCount, graph.CellCount }, Is.EqualTo(new[] { 47, 96, 48, 47 }));
        }

        [Test]
        public void StarterSummaryFreezesMaskBfsLoopAndCsvCounts()
        {
            var summary = Complete(reused, graph).Report.Summary;
            Assert.That(new[] { summary.Type1Count, summary.Type2Count, summary.Type3Count, summary.Type4UdCount,
                summary.Type4LudCount, summary.Type4RudCount, summary.Type4LrudCount }, Is.EqualTo(new[] { 20, 4, 4, 17, 0, 0, 2 }));
            Assert.That(summary.Type4Count, Is.EqualTo(19));
            Assert.That(new[] { summary.ReachableTerminalCount, summary.RepresentedLoopCount }, Is.EqualTo(new[] { 7, 2 }));
            Assert.That(new[] { summary.GeneratedSectorCsvByteCount, summary.GeneratedEdgeCsvByteCount, summary.GeneratedEdgeRowCount },
                Is.EqualTo(new[] { 16838, 7094, 96 }));
        }

        [Test]
        public void ReportPreservesExactSourceArtifactIdentities()
        {
            var report = Complete(reused, graph).Report;
            Assert.That(report.SourceGraph, Is.SameAs(graph));
            Assert.That(report.SourceWorld, Is.SameAs(graph.RouteStampedWorld));
            Assert.That(report.SourceTerminalSet, Is.SameAs(terminals));
            Assert.That(report.SourceLoopPlan, Is.SameAs(loops));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void NullArtifactReturnsTypedInvalidInput(int sourceIndex)
        {
            var result = reused.Validate(sourceIndex == 0 ? null : graph, sourceIndex == 1 ? null : graph.RouteStampedWorld,
                sourceIndex == 2 ? null : rows, sourceIndex == 3 ? null : sectorCsv, sourceIndex == 4 ? null : edgeCsv,
                sourceIndex == 5 ? null : terminals, sourceIndex == 6 ? null : loops);
            Assert.That(result.Status, Is.EqualTo(MandatoryRouteValidationStatus.InvalidInput));
            Assert.That(result.Report, Is.Null);
            Assert.That(result.Diagnostics, Is.Null);
            Assert.That(result.Succeeded || result.RetryRequired, Is.False);
        }

        [Test]
        public void MissingGeneratedEdgeRowFailsClosed()
        {
            var changed = rows.Take(rows.Count - 1).ToList();
            var result = Validate(graph, graph.RouteStampedWorld, changed, sectorCsv, GeneratedWorldEdgesCsvSerializer.Serialize(changed));
            AssertRule(result, MandatoryRouteGraphValidator.GeneratedEdgeCsvRule);
        }

        [Test]
        public void ExtraGeneratedEdgeRowFailsClosed()
        {
            var changed = new List<GeneratedWorldEdge>(rows) { rows[0] };
            var result = Validate(graph, graph.RouteStampedWorld, changed, sectorCsv, GeneratedWorldEdgesCsvSerializer.Serialize(changed));
            AssertRule(result, MandatoryRouteGraphValidator.GeneratedEdgeCsvRule);
        }

        [Test]
        public void CorruptGeneratedSectorCsvFailsClosed()
        {
            var changed = (byte[])sectorCsv.Clone(); changed[changed.Length - 1] = (byte)'X';
            AssertRule(Validate(graph, graph.RouteStampedWorld, rows, changed, edgeCsv), MandatoryRouteGraphValidator.GeneratedSectorCsvRule);
        }

        [Test]
        public void CorruptGeneratedEdgeCsvFailsClosed()
        {
            var changed = (byte[])edgeCsv.Clone(); changed[0] = 0;
            AssertRule(Validate(graph, graph.RouteStampedWorld, rows, sectorCsv, changed), MandatoryRouteGraphValidator.GeneratedEdgeCsvRule);
        }

        [Test]
        public void MissingReverseEdgeFailsReciprocityAndCsvClosed()
        {
            var edges = graph.Edges.Take(graph.Edges.Count - 1).ToList();
            var changed = CloneGraph(edges: edges);
            var result = reused.Validate(changed);
            AssertRule(result, MandatoryRouteGraphValidator.EdgeReciprocityRule);
            AssertRule(result, MandatoryRouteGraphValidator.GeneratedEdgeCsvRule);
        }

        [Test]
        public void WrongEdgeSideFailsSideAndReciprocity()
        {
            var original = graph.Edges.First(value => value.Side == "L" || value.Side == "R");
            var edges = graph.Edges.ToList();
            edges[edges.IndexOf(original)] = CloneEdge(original, "U", "D");
            var result = reused.Validate(CloneGraph(edges: edges));
            AssertRule(result, MandatoryRouteGraphValidator.EdgeSideMatchRule);
            AssertRule(result, MandatoryRouteGraphValidator.EdgeReciprocityRule);
        }

        [Test]
        public void Type4MissingUpFailsUdRequired()
        {
            var target = graph.Cells.First(value => value.Mask.RouteType == 4);
            var cells = graph.Cells.ToList();
            cells[cells.IndexOf(target)] = CloneCell(target, target.StampedCell, target.OpenLeft, target.OpenRight, false, target.OpenDown, target.IsApprovedReservedAdapter);
            var result = reused.Validate(CloneGraph(cells: cells));
            AssertRule(result, MandatoryRouteGraphValidator.Type4UdRequiredRule);
        }

        [Test]
        public void Type4ForcedLeftRightFailsPreservation()
        {
            var target = graph.Cells.First(value => value.RouteMaskId == MandatoryRouteMaskFamily.Type4UdId);
            var cells = graph.Cells.ToList();
            cells[cells.IndexOf(target)] = CloneCell(target, target.StampedCell, true, true, true, true, target.IsApprovedReservedAdapter);
            var result = reused.Validate(CloneGraph(cells: cells));
            AssertRule(result, MandatoryRouteGraphValidator.Type4LrPreservedRule);
        }

        [Test]
        public void UnsupportedRouteMaskFailsFamilyAndStamp()
        {
            var target = graph.Cells[0];
            var stamped = CloneStamped(target.StampedCell, target.StampedCell.Role, "ROUTE_UNSUPPORTED");
            var world = ReplaceWorldCell(graph.RouteStampedWorld, stamped);
            var cells = graph.Cells.ToList();
            cells[0] = CloneCell(target, stamped, target.OpenLeft, target.OpenRight, target.OpenUp, target.OpenDown, target.IsApprovedReservedAdapter);
            var result = reused.Validate(CloneGraph(cells: cells, world: world));
            AssertRule(result, MandatoryRouteGraphValidator.MaskFamilyRule);
        }

        [TestCase(GeneratedSectorRole.Type0)]
        [TestCase(GeneratedSectorRole.InactiveBuffer)]
        public void ForbiddenRouteRoleFailsNoType0Intrusion(GeneratedSectorRole role)
        {
            var target = graph.Cells.First(value => value.StampedCell.Role != GeneratedSectorRole.ReservedSite);
            var stamped = CloneStamped(target.StampedCell, role, target.RouteMaskId);
            var world = ReplaceWorldCell(graph.RouteStampedWorld, stamped);
            var cells = graph.Cells.ToList();
            cells[cells.IndexOf(target)] = CloneCell(target, stamped, target.OpenLeft, target.OpenRight, target.OpenUp, target.OpenDown, false);
            AssertRule(reused.Validate(CloneGraph(cells: cells, world: world)), MandatoryRouteGraphValidator.NoType0IntrusionRule);
        }

        [Test]
        public void UnapprovedReservedInteriorFailsNoType0Intrusion()
        {
            var target = graph.Cells.First(value => value.StampedCell.Role != GeneratedSectorRole.ReservedSite);
            var stamped = CloneStamped(target.StampedCell, GeneratedSectorRole.ReservedSite, target.RouteMaskId);
            var world = ReplaceWorldCell(graph.RouteStampedWorld, stamped);
            var cells = graph.Cells.ToList();
            cells[cells.IndexOf(target)] = CloneCell(target, stamped, target.OpenLeft, target.OpenRight, target.OpenUp, target.OpenDown, false);
            AssertRule(reused.Validate(CloneGraph(cells: cells, world: world)), MandatoryRouteGraphValidator.NoType0IntrusionRule);
        }

        [Test]
        public void DifferentWorldIdentityFailsSourceImmutabilityAndStamp()
        {
            var other = terminals.SourceBiomePublication.WorldWithBiomeAssignments;
            var result = Validate(graph, other, rows, GeneratedWorldDataCsvSerializer.Serialize(other), edgeCsv);
            AssertRule(result, MandatoryRouteGraphValidator.SourceImmutabilityRule);
            AssertRule(result, MandatoryRouteGraphValidator.SectorStampRule);
        }

        [Test]
        public void IsolatedTerminalFailsBfs()
        {
            var terminalIndex = WorldGridIndex.ToIndex(terminals.Terminals[1].ApproachSector);
            var edges = graph.Edges.Where(value => value.FromSectorIndex != terminalIndex && value.ToSectorIndex != terminalIndex).ToList();
            AssertRule(reused.Validate(CloneGraph(edges: edges)), MandatoryRouteGraphValidator.TerminalBfsRule);
        }

        [Test]
        public void RemovedLoopMarkersFailLoopRepresentation()
        {
            var loop = loops.Loops[0];
            var loopIndices = new HashSet<int>(loop.InclusiveOrderedCells.Select(WorldGridIndex.ToIndex));
            var nodes = graph.Nodes.Select(value => loopIndices.Contains(value.SectorIndex) ? CloneNodeWithoutLoops(value) : value).ToList();
            AssertRule(reused.Validate(CloneGraph(nodes: nodes)), MandatoryRouteGraphValidator.LoopRepresentedRule);
        }

        [Test]
        public void ViolationsAreSortedDeduplicatedAndImmutable()
        {
            var changed = new List<GeneratedWorldEdge>(rows) { rows[0], rows[0] };
            var bytes = GeneratedWorldEdgesCsvSerializer.Serialize(changed);
            var report = Validate(graph, graph.RouteStampedWorld, changed, sectorCsv, bytes).Report;
            Assert.That(report.Violations.Select(value => value.SortKey), Is.Ordered);
            Assert.That(report.Violations.Select(value => value.SortKey).Distinct().Count(), Is.EqualTo(report.Violations.Count));
            Assert.Throws<NotSupportedException>(() => ((IList<MandatoryRouteValidationViolation>)report.Violations).Add(report.Violations[0]));
            Assert.That(report.Summary.ViolationCount, Is.EqualTo(report.Violations.Count));
        }

        [Test]
        public void DiagnosticsFreezeNoRngFilesystemClockOrMutation()
        {
            var diagnostics = Complete(reused, graph).Diagnostics;
            Assert.That(new[] { diagnostics.EvaluatedRuleCount, diagnostics.EvaluatedNodeCount, diagnostics.EvaluatedEdgeCount,
                diagnostics.EvaluatedCellCount, diagnostics.GeneratedEdgeRowCount }, Is.EqualTo(new[] { 12, 47, 96, 47, 96 }));
            Assert.That(new[] { diagnostics.RngDrawCount, diagnostics.FileReadCount, diagnostics.FileWriteCount,
                diagnostics.ClockReadCount, diagnostics.SourceMutationCount }, Is.EqualTo(new[] { 0, 0, 0, 0, 0 }));
        }

        [Test]
        public void RuntimeSurfaceHasNoMutableStaticUnityEditorOrMap06_03PlusSymbols()
        {
            var types = new[]
            {
                typeof(MandatoryRouteValidationRuleId), typeof(MandatoryRouteValidationSeverity),
                typeof(MandatoryRouteValidationViolation), typeof(MandatoryRouteValidationSummary),
                typeof(MandatoryRouteValidationReport), typeof(MandatoryRouteValidationDiagnostics),
                typeof(MandatoryRouteValidationResult), typeof(MandatoryRouteGraphValidator)
            };
            foreach (var type in types)
                Assert.That(type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(field => !field.IsLiteral && !field.IsInitOnly), Is.Empty, type.FullName);
            var assembly = typeof(MandatoryRouteGraphValidator).Assembly;
            Assert.That(assembly.GetReferencedAssemblies().Any(value => value.Name == "UnityEditor"), Is.False);
            var names = string.Join("|", assembly.GetTypes().Select(value => value.Name));
            foreach (var forbidden in new[]
            {
                "MandatoryRoutePass", "SectorRouteMaskAssigner",
                "OptionalReturnConnection", "OptionalClueAssigner", "OptionalRegionValidationOverlayWindow", "Type0Overlay"
            })
                Assert.That(names, Does.Not.Contain(forbidden));
        }

        [Test]
        public void FreshReusedAndParallelValidationHasOneSignature()
        {
            var signatures = new string[12];
            Parallel.For(0, signatures.Length, index => signatures[index] = Signature(Complete((index & 1) == 0 ? new MandatoryRouteGraphValidator() : reused, graph)));
            Assert.That(signatures.Distinct().Single(), Is.EqualTo(expectedSignature));
            Assert.That(SourceSignature(), Is.EqualTo(sourceSignature));
        }

        [Test]
        public void RepeatedValidationNeverMutatesGraphWorldOrCsv()
        {
            var before = SourceSignature();
            for (var index = 0; index < 4; index++) Complete(reused, graph);
            Assert.That(SourceSignature(), Is.EqualTo(before));
        }

        private MandatoryRouteValidationResult Complete(MandatoryRouteGraphValidator validator, MandatoryRouteGraph source)
        {
            var result = validator.Validate(source);
            Assert.That(result.Status, Is.EqualTo(MandatoryRouteValidationStatus.Completed));
            Assert.That(result.Succeeded, Is.True, FormatViolations(result));
            return result;
        }

        private MandatoryRouteValidationResult Validate(MandatoryRouteGraph source, GeneratedWorldData world,
            IEnumerable<GeneratedWorldEdge> edgeRows, byte[] sectors, byte[] edges) =>
            reused.Validate(source, world, edgeRows, sectors, edges, terminals, loops);

        private static void AssertRule(MandatoryRouteValidationResult result, string rule)
        {
            Assert.That(result.Status, Is.EqualTo(MandatoryRouteValidationStatus.Completed));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Report.Errors.Any(value => value.RuleId.Value == rule), Is.True, FormatViolations(result));
        }

        private MandatoryRouteGraph CloneGraph(IEnumerable<MandatoryRouteGraphNode> nodes = null,
            IEnumerable<MandatoryRouteGraphEdge> edges = null, IEnumerable<MandatoryRouteGraphCell> cells = null,
            GeneratedWorldData world = null, byte[] generatedEdgesCsv = null)
        {
            var constructor = typeof(MandatoryRouteGraph).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
            return (MandatoryRouteGraph)constructor.Invoke(new object[]
            {
                graph.SourceTerminalSet, graph.SourceRouteMaskLookup, graph.SourceConnectorTree, graph.SourceHorizontalBackbonePlan,
                graph.SourceVerticalGatewayPlan, graph.SourceConflictResolutionPlan, graph.SourceLoopPlan, graph.MaskFamily,
                nodes ?? graph.Nodes, edges ?? graph.Edges, cells ?? graph.Cells, world ?? graph.RouteStampedWorld,
                generatedEdgesCsv ?? graph.GeneratedWorldEdgesCsv
            });
        }

        private static MandatoryRouteGraphEdge CloneEdge(MandatoryRouteGraphEdge source, string side, string reverseSide)
        {
            var constructor = typeof(MandatoryRouteGraphEdge).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
            return (MandatoryRouteGraphEdge)constructor.Invoke(new object[] { source.EdgeId, source.FromNodeId, source.ToNodeId,
                source.FromSectorIndex, source.ToSectorIndex, side, reverseSide, source.TraversalKind, source.EdgeSignatureId,
                source.CostTiles, source.SourceArtifactId });
        }

        private static MandatoryRouteGraphCell CloneCell(MandatoryRouteGraphCell source, SectorCell stamped,
            bool left, bool right, bool up, bool down, bool approved)
        {
            var constructor = typeof(MandatoryRouteGraphCell).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
            return (MandatoryRouteGraphCell)constructor.Invoke(new object[] { source.SourceCell, stamped, source.Mask, left, right, up, down, approved });
        }

        private static MandatoryRouteGraphNode CloneNodeWithoutLoops(MandatoryRouteGraphNode source)
        {
            var constructor = typeof(MandatoryRouteGraphNode).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
            return (MandatoryRouteGraphNode)constructor.Invoke(new object[] { source.NodeId, source.Coordinate, source.RouteMaskId,
                source.OpenLeft, source.OpenRight, source.OpenUp, source.OpenDown, source.ShortestDistanceFromStart,
                source.TerminalSourceIds, source.SiteSourceIds, Array.Empty<string>(), source.GatewaySourceIds });
        }

        private static SectorCell CloneStamped(SectorCell source, GeneratedSectorRole role, string routeMaskId) =>
            new SectorCell(source.Index, source.Coordinate, role, source.PrimaryBiomeId, source.SecondaryBiomeId, source.PatchId,
                routeMaskId, source.SpecialSiteInstanceId, source.BoundaryProfileId, source.SectorRecipeId, source.ReservationId,
                source.ShortestDistanceFromStart, source.MandatoryGraphNode);

        private static GeneratedWorldData ReplaceWorldCell(GeneratedWorldData source, SectorCell replacement)
        {
            var cells = source.Cells.ToList();
            cells[replacement.Index] = replacement;
            return new GeneratedWorldData(source.Seed, cells);
        }

        private static List<GeneratedWorldEdge> ToRows(MandatoryRouteGraph source)
        {
            var values = new List<GeneratedWorldEdge>();
            foreach (var edge in source.Edges)
                values.Add(new GeneratedWorldEdge(source.RouteStampedWorld.Seed, WorldGridIndex.ToCoordinate(edge.FromSectorIndex), edge.Side,
                    WorldGridIndex.ToCoordinate(edge.ToSectorIndex), edge.Layer, edge.TraversalKind, edge.Open, edge.EdgeSignatureId, edge.CostTiles));
            return values;
        }

        private string SourceSignature() => graph.NodeCount + "|" + graph.DirectedEdgeCount + "|" + graph.CellCount + "|" +
            ByteHash(graph.GeneratedWorldEdgesCsv) + "|" + ByteHash(GeneratedWorldDataCsvSerializer.Serialize(graph.RouteStampedWorld)) + "|" +
            terminals.TerminalCount + "|" + loops.LoopCount;

        private static string Signature(MandatoryRouteValidationResult result) => result.Status + "|" + result.Succeeded + "|" +
            (result.Report == null ? "null" : result.Report.PassId + "|" + result.Report.Summary.PassedRuleCount + "|" +
                string.Join("/", result.Report.Violations.Select(value => value.SortKey))) + "|" +
            (result.Diagnostics == null ? "null" : result.Diagnostics.EvaluatedRuleCount + ":" + result.Diagnostics.SourceMutationCount);

        private static ulong ByteHash(IEnumerable<byte> bytes)
        {
            var value = 1469598103934665603UL;
            foreach (var item in bytes) { value ^= item; value *= 1099511628211UL; }
            return value;
        }

        private static string FormatViolations(MandatoryRouteValidationResult result) => result.Report == null ? "NO_REPORT" :
            string.Join("\n", result.Report.Violations.Select(value => value.RuleId.Value + " " + value.MessageToken + " " + value.SourceArtifactId));
        private static T GetField<T>(object target, Type type, string name) => (T)type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    }
}
