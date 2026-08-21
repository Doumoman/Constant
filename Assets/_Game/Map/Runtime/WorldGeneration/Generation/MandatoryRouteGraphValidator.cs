using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryRouteGraphValidator
    {
        public const int RequiredRuleCount = 12;
        public const string MaskFamilyRule = "VAL_ROUTE_MASK_FAMILY";
        public const string Type4UdRequiredRule = "VAL_ROUTE_TYPE4_UD_REQUIRED";
        public const string Type4LrPreservedRule = "VAL_ROUTE_TYPE4_LR_PRESERVED";
        public const string EdgeReciprocityRule = "VAL_ROUTE_EDGE_RECIPROCITY";
        public const string EdgeSideMatchRule = "VAL_ROUTE_EDGE_SIDE_MATCH";
        public const string TerminalBfsRule = "VAL_ROUTE_TERMINAL_BFS";
        public const string LoopRepresentedRule = "VAL_ROUTE_LOOP_REPRESENTED";
        public const string SectorStampRule = "VAL_ROUTE_SECTOR_STAMP";
        public const string GeneratedSectorCsvRule = "VAL_ROUTE_GENERATED_SECTOR_CSV";
        public const string GeneratedEdgeCsvRule = "VAL_ROUTE_GENERATED_EDGE_CSV";
        public const string NoType0IntrusionRule = "VAL_ROUTE_NO_TYPE0_INTRUSION";
        public const string SourceImmutabilityRule = "VAL_ROUTE_SOURCE_IMMUTABILITY";

        public MandatoryRouteValidationResult Validate(MandatoryRouteGraph graph)
        {
            if (graph == null) return Invalid();
            var rows = ToGeneratedRows(graph);
            return Validate(graph, graph.RouteStampedWorld, rows,
                GeneratedWorldDataCsvSerializer.Serialize(graph.RouteStampedWorld), graph.GeneratedWorldEdgesCsv,
                graph.SourceTerminalSet, graph.SourceLoopPlan);
        }

        public MandatoryRouteValidationResult Validate(MandatoryRouteGraph graph, GeneratedWorldData routeStampedWorld,
            IEnumerable<GeneratedWorldEdge> generatedEdges, byte[] generatedSectorCsv, byte[] generatedEdgeCsv,
            MandatoryRouteTerminalSet terminalSet, MandatoryRouteLoopPlan loopPlan)
        {
            if (graph == null || routeStampedWorld == null || generatedEdges == null || generatedSectorCsv == null ||
                generatedEdgeCsv == null || terminalSet == null || loopPlan == null) return Invalid();

            var rows = new List<GeneratedWorldEdge>();
            foreach (var row in generatedEdges)
            {
                if (row == null) return Invalid();
                rows.Add(row);
            }

            var beforeGraphEdgeCsv = graph.GeneratedWorldEdgesCsv;
            var beforeGraphSectorCsv = GeneratedWorldDataCsvSerializer.Serialize(graph.RouteStampedWorld);
            var sourceBiomeWorld = graph.SourceTerminalSet.SourceBiomePublication.WorldWithBiomeAssignments;
            var beforeSourceWorldCsv = GeneratedWorldDataCsvSerializer.Serialize(sourceBiomeWorld);
            var violations = new List<MandatoryRouteValidationViolation>();
            var familyCounts = new Dictionary<string, int>(StringComparer.Ordinal);

            ValidateCells(graph, routeStampedWorld, violations, familyCounts);
            ValidateEdges(graph, violations);
            ValidateGeneratedRowsAndCsv(graph, routeStampedWorld, rows, generatedSectorCsv, generatedEdgeCsv, violations);
            var reachableTerminals = ValidateTerminalReachability(graph, terminalSet, violations);
            var representedLoops = ValidateLoops(graph, loopPlan, violations);

            var sourceMutationCount = 0;
            if (!ReferenceEquals(routeStampedWorld, graph.RouteStampedWorld) ||
                !ReferenceEquals(terminalSet, graph.SourceTerminalSet) || !ReferenceEquals(loopPlan, graph.SourceLoopPlan))
                Add(violations, SourceImmutabilityRule, default, default, -1, default, string.Empty, "SOURCE_IDENTITY_MISMATCH");
            if (!ByteEquals(beforeGraphEdgeCsv, graph.GeneratedWorldEdgesCsv) ||
                !ByteEquals(beforeGraphSectorCsv, GeneratedWorldDataCsvSerializer.Serialize(graph.RouteStampedWorld)) ||
                !ByteEquals(beforeSourceWorldCsv, GeneratedWorldDataCsvSerializer.Serialize(sourceBiomeWorld)))
            {
                sourceMutationCount = 1;
                Add(violations, SourceImmutabilityRule, default, default, -1, default, string.Empty, "SOURCE_MUTATION_DETECTED");
            }

            var unique = Unique(violations);
            var failedRules = new HashSet<MandatoryRouteValidationRuleId>();
            var errorCount = 0;
            var warningCount = 0;
            foreach (var violation in unique)
            {
                failedRules.Add(violation.RuleId);
                if (violation.Severity == MandatoryRouteValidationSeverity.Error) errorCount++;
                else warningCount++;
            }

            var summary = new MandatoryRouteValidationSummary(RequiredRuleCount, RequiredRuleCount - failedRules.Count, failedRules.Count,
                unique.Count, errorCount, warningCount, reachableTerminals, representedLoops,
                Count(familyCounts, MandatoryRouteMaskFamily.Type1Id), Count(familyCounts, MandatoryRouteMaskFamily.Type2Id),
                Count(familyCounts, MandatoryRouteMaskFamily.Type3Id), Count(familyCounts, MandatoryRouteMaskFamily.Type4UdId),
                Count(familyCounts, MandatoryRouteMaskFamily.Type4LudId), Count(familyCounts, MandatoryRouteMaskFamily.Type4RudId),
                Count(familyCounts, MandatoryRouteMaskFamily.Type4LrudId), graph.DirectedEdgeCount, graph.UndirectedEdgeCount,
                generatedSectorCsv.Length, generatedEdgeCsv.Length, rows.Count);
            var report = new MandatoryRouteValidationReport(graph, routeStampedWorld, terminalSet, loopPlan, summary, unique);
            var diagnostics = new MandatoryRouteValidationDiagnostics(RequiredRuleCount, graph.NodeCount, graph.DirectedEdgeCount,
                graph.CellCount, rows.Count, sourceMutationCount);
            return new MandatoryRouteValidationResult(MandatoryRouteValidationStatus.Completed, report, diagnostics);
        }

        private static void ValidateCells(MandatoryRouteGraph graph, GeneratedWorldData routeStampedWorld,
            ICollection<MandatoryRouteValidationViolation> violations, IDictionary<string, int> familyCounts)
        {
            var routeIndices = new HashSet<int>();
            foreach (var cell in graph.Cells)
            {
                routeIndices.Add(cell.SectorIndex);
                var nodeId = default(MandatoryRouteGraphNodeId);
                graph.TryGetNode(cell.SectorIndex, out var node);
                if (node != null) nodeId = node.NodeId;

                if (!WorldCoordinateUtility.IsValid(cell.Coordinate) || cell.SectorIndex != WorldGridIndex.ToIndex(cell.Coordinate))
                    Add(violations, NoType0IntrusionRule, nodeId, default, cell.SectorIndex, cell.Coordinate, string.Empty, "ROUTE_WORLD_OUTSIDE");

                if (!graph.MaskFamily.TryGetById(cell.RouteMaskId, out var mask))
                {
                    Add(violations, MaskFamilyRule, nodeId, default, cell.SectorIndex, cell.Coordinate, cell.RouteMaskId, "UNSUPPORTED_ROUTE_MASK");
                }
                else
                {
                    familyCounts[mask.MaskId] = Count(familyCounts, mask.MaskId) + 1;
                    if (!HasExactMaskSemantics(mask) || mask.OpenLeft != cell.OpenLeft || mask.OpenRight != cell.OpenRight ||
                        mask.OpenUp != cell.OpenUp || mask.OpenDown != cell.OpenDown)
                        Add(violations, MaskFamilyRule, nodeId, default, cell.SectorIndex, cell.Coordinate, mask.MaskId, "MASK_OPEN_BITS_MISMATCH");
                    if (mask.RouteType == 4)
                    {
                        if (!cell.OpenUp || !cell.OpenDown)
                            Add(violations, Type4UdRequiredRule, nodeId, default, cell.SectorIndex, cell.Coordinate, mask.MaskId, "TYPE4_UD_REQUIRED");
                        if (!graph.MaskFamily.TryResolve(cell.OpenLeft, cell.OpenRight, cell.OpenUp, cell.OpenDown, out var resolved) ||
                            resolved.MaskId != cell.RouteMaskId)
                            Add(violations, Type4LrPreservedRule, nodeId, default, cell.SectorIndex, cell.Coordinate, mask.MaskId, "TYPE4_LR_NOT_PRESERVED");
                    }
                }

                if (node == null || node.SectorIndex != cell.SectorIndex || node.Coordinate != cell.Coordinate ||
                    node.RouteMaskId != cell.RouteMaskId || node.OpenLeft != cell.OpenLeft || node.OpenRight != cell.OpenRight ||
                    node.OpenUp != cell.OpenUp || node.OpenDown != cell.OpenDown ||
                    node.ShortestDistanceFromStart != cell.ShortestDistanceFromStart || !node.MandatoryGraphNode)
                    Add(violations, SectorStampRule, nodeId, default, cell.SectorIndex, cell.Coordinate, cell.RouteMaskId, "GRAPH_NODE_STAMP_MISMATCH");

                if (!routeStampedWorld.TryGetCell(cell.SectorIndex, out var stamped) || !ReferenceEquals(stamped, cell.StampedCell) ||
                    stamped.RouteMaskId != cell.RouteMaskId || !stamped.MandatoryGraphNode ||
                    stamped.ShortestDistanceFromStart != cell.ShortestDistanceFromStart)
                    Add(violations, SectorStampRule, nodeId, default, cell.SectorIndex, cell.Coordinate, cell.RouteMaskId, "WORLD_SECTOR_STAMP_MISMATCH");

                if (cell.StampedCell.Role == GeneratedSectorRole.Type0 || cell.StampedCell.Role == GeneratedSectorRole.InactiveBuffer ||
                    cell.SourceCell.Role == GeneratedSectorRole.Type0 || cell.SourceCell.Role == GeneratedSectorRole.InactiveBuffer)
                    Add(violations, NoType0IntrusionRule, nodeId, default, cell.SectorIndex, cell.Coordinate, cell.RouteMaskId, "FORBIDDEN_ROUTE_ROLE");
                if (cell.StampedCell.Role == GeneratedSectorRole.ReservedSite && !cell.IsApprovedReservedAdapter)
                    Add(violations, NoType0IntrusionRule, nodeId, default, cell.SectorIndex, cell.Coordinate, cell.StampedCell.ReservationId, "RESERVED_INTERIOR_ROUTE");
            }

            foreach (var worldCell in routeStampedWorld.Cells)
                if (worldCell.MandatoryGraphNode && !routeIndices.Contains(worldCell.Index))
                    Add(violations, SectorStampRule, default, default, worldCell.Index, worldCell.Coordinate, worldCell.RouteMaskId, "STAMP_WITHOUT_GRAPH_CELL");
        }

        private static void ValidateEdges(MandatoryRouteGraph graph, ICollection<MandatoryRouteValidationViolation> violations)
        {
            foreach (var edge in graph.Edges)
            {
                graph.TryGetNode(edge.FromSectorIndex, out var fromNode);
                graph.TryGetNode(edge.ToSectorIndex, out var toNode);
                var inBounds = edge.FromSectorIndex >= 0 && edge.FromSectorIndex < WorldGenConstants.SectorCount &&
                    edge.ToSectorIndex >= 0 && edge.ToSectorIndex < WorldGenConstants.SectorCount;
                if (!inBounds || fromNode == null || toNode == null)
                {
                    Add(violations, EdgeSideMatchRule, edge.FromNodeId, edge.EdgeId,
                        inBounds ? edge.FromSectorIndex : -1, default, edge.SourceArtifactId, "EDGE_ENDPOINT_MISSING");
                    continue;
                }

                var expectedSide = Side(fromNode.Coordinate, toNode.Coordinate);
                if (expectedSide == null || edge.Side != expectedSide || edge.ReverseSide != Reverse(edge.Side) ||
                    !IsOpen(fromNode, edge.Side) || !IsOpen(toNode, edge.ReverseSide) || edge.Layer != "MANDATORY" || !edge.Open)
                    Add(violations, EdgeSideMatchRule, fromNode.NodeId, edge.EdgeId, edge.FromSectorIndex,
                        fromNode.Coordinate, edge.SourceArtifactId, "EDGE_SIDE_OR_OPEN_MISMATCH");

                var reverseCount = 0;
                foreach (var candidate in graph.Edges)
                    if (candidate.FromSectorIndex == edge.ToSectorIndex && candidate.ToSectorIndex == edge.FromSectorIndex &&
                        candidate.Side == edge.ReverseSide && candidate.ReverseSide == edge.Side &&
                        candidate.Open == edge.Open && candidate.CostTiles == edge.CostTiles && candidate.Layer == edge.Layer &&
                        candidate.TraversalKind == edge.TraversalKind && candidate.EdgeSignatureId == edge.EdgeSignatureId &&
                        candidate.SourceArtifactId == edge.SourceArtifactId) reverseCount++;
                if (reverseCount != 1)
                    Add(violations, EdgeReciprocityRule, fromNode.NodeId, edge.EdgeId, edge.FromSectorIndex,
                        fromNode.Coordinate, edge.SourceArtifactId, "EDGE_REVERSE_COUNT_" + reverseCount.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static void ValidateGeneratedRowsAndCsv(MandatoryRouteGraph graph, GeneratedWorldData routeStampedWorld,
            IReadOnlyList<GeneratedWorldEdge> rows, byte[] generatedSectorCsv, byte[] generatedEdgeCsv,
            ICollection<MandatoryRouteValidationViolation> violations)
        {
            var expectedSectorCsv = GeneratedWorldDataCsvSerializer.Serialize(routeStampedWorld);
            if (!ByteEquals(expectedSectorCsv, generatedSectorCsv) ||
                !HasExactCsvEnvelope(generatedSectorCsv, GeneratedWorldDataCsvSerializer.Header, WorldGenConstants.SectorCount, 13))
                Add(violations, GeneratedSectorCsvRule, default, default, -1, default, string.Empty, "GENERATED_SECTOR_CSV_MISMATCH");

            var expectedEdgeCsv = GeneratedWorldEdgesCsvSerializer.Serialize(rows);
            if (!ByteEquals(expectedEdgeCsv, generatedEdgeCsv) || !ByteEquals(generatedEdgeCsv, graph.GeneratedWorldEdgesCsv) ||
                !HasExactCsvEnvelope(generatedEdgeCsv, GeneratedWorldEdgesCsvSerializer.Header, rows.Count, 11))
                Add(violations, GeneratedEdgeCsvRule, default, default, -1, default, string.Empty, "GENERATED_EDGE_CSV_MISMATCH");

            var expected = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var edge in graph.Edges)
            {
                var key = EdgeKey(routeStampedWorld.Seed, WorldGridIndex.ToCoordinate(edge.FromSectorIndex), edge.Side,
                    WorldGridIndex.ToCoordinate(edge.ToSectorIndex), edge.Layer, edge.TraversalKind, edge.Open,
                    edge.EdgeSignatureId, edge.CostTiles);
                expected[key] = Count(expected, key) + 1;
            }
            var actual = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                var key = EdgeKey(row.Seed, row.From, row.Side, row.To, row.EdgeLayer, row.TraversalKind, row.Open,
                    row.EdgeSignatureId, row.CostTiles);
                actual[key] = Count(actual, key) + 1;
            }
            foreach (var pair in expected)
                if (Count(actual, pair.Key) != pair.Value)
                    Add(violations, GeneratedEdgeCsvRule, default, default, -1, default, pair.Key, "GENERATED_EDGE_ROW_MISSING");
            foreach (var pair in actual)
                if (Count(expected, pair.Key) != pair.Value)
                    Add(violations, GeneratedEdgeCsvRule, default, default, -1, default, pair.Key, "GENERATED_EDGE_ROW_EXTRA");
        }

        private static int ValidateTerminalReachability(MandatoryRouteGraph graph, MandatoryRouteTerminalSet terminalSet,
            ICollection<MandatoryRouteValidationViolation> violations)
        {
            var adjacency = new Dictionary<int, List<int>>();
            foreach (var node in graph.Nodes) adjacency[node.SectorIndex] = new List<int>();
            foreach (var edge in graph.Edges)
                if (adjacency.TryGetValue(edge.FromSectorIndex, out var values) && adjacency.ContainsKey(edge.ToSectorIndex)) values.Add(edge.ToSectorIndex);
            foreach (var values in adjacency.Values) values.Sort();
            var start = WorldGridIndex.ToIndex(terminalSet.StartTerminal.ApproachSector);
            var reached = new HashSet<int>();
            if (adjacency.ContainsKey(start))
            {
                var queue = new Queue<int>();
                queue.Enqueue(start); reached.Add(start);
                while (queue.Count != 0)
                {
                    var current = queue.Dequeue();
                    foreach (var next in adjacency[current]) if (reached.Add(next)) queue.Enqueue(next);
                }
            }
            var count = 0;
            foreach (var terminal in terminalSet.Terminals)
            {
                var index = WorldGridIndex.ToIndex(terminal.ApproachSector);
                if (reached.Contains(index)) count++;
                else Add(violations, TerminalBfsRule, default, default, index, terminal.ApproachSector,
                    terminal.TerminalId.Value, "MANDATORY_TERMINAL_UNREACHABLE");
            }
            return count;
        }

        private static int ValidateLoops(MandatoryRouteGraph graph, MandatoryRouteLoopPlan loopPlan,
            ICollection<MandatoryRouteValidationViolation> violations)
        {
            var represented = 0;
            foreach (var loop in loopPlan.Loops)
            {
                var nodesPresent = true;
                foreach (var coordinate in loop.InclusiveOrderedCells)
                    if (!graph.TryGetNode(coordinate, out var node) || !node.LoopSourceIds.Contains(loop.LoopId.Value)) nodesPresent = false;
                var edgePresent = graph.Edges.Any(edge => edge.SourceArtifactId == loop.LoopId.Value);
                if (loop.IsIndependent && nodesPresent && edgePresent) represented++;
                else Add(violations, LoopRepresentedRule, default, default,
                    WorldGridIndex.ToIndex(loop.InclusiveOrderedCells[0]), loop.InclusiveOrderedCells[0], loop.LoopId.Value, "ACCEPTED_LOOP_NOT_REPRESENTED");
            }
            return represented;
        }

        private static bool HasExactMaskSemantics(MandatoryRouteMaskFamily.Entry mask)
        {
            if (mask.RouteType == 1) return mask.OpenLeft && mask.OpenRight && !mask.OpenUp && !mask.OpenDown;
            if (mask.RouteType == 2) return mask.OpenLeft && mask.OpenRight && !mask.OpenUp && mask.OpenDown;
            if (mask.RouteType == 3) return mask.OpenLeft && mask.OpenRight && mask.OpenUp && !mask.OpenDown;
            return mask.RouteType == 4 && mask.OpenUp && mask.OpenDown;
        }

        private static bool IsOpen(MandatoryRouteGraphNode node, string side) =>
            side == "L" ? node.OpenLeft : side == "R" ? node.OpenRight : side == "U" ? node.OpenUp : side == "D" && node.OpenDown;

        private static string Side(SectorCoord from, SectorCoord to)
        {
            var distance = Math.Abs(from.X - to.X) + Math.Abs(from.Y - to.Y);
            if (distance != 1) return null;
            return to.X < from.X ? "L" : to.X > from.X ? "R" : to.Y > from.Y ? "U" : "D";
        }

        private static string Reverse(string side) => side == "L" ? "R" : side == "R" ? "L" : side == "U" ? "D" : side == "D" ? "U" : string.Empty;

        private static List<GeneratedWorldEdge> ToGeneratedRows(MandatoryRouteGraph graph)
        {
            var rows = new List<GeneratedWorldEdge>(graph.Edges.Count);
            foreach (var edge in graph.Edges)
                rows.Add(new GeneratedWorldEdge(graph.RouteStampedWorld.Seed, WorldGridIndex.ToCoordinate(edge.FromSectorIndex), edge.Side,
                    WorldGridIndex.ToCoordinate(edge.ToSectorIndex), edge.Layer, edge.TraversalKind, edge.Open, edge.EdgeSignatureId, edge.CostTiles));
            return rows;
        }

        private static string EdgeKey(ulong seed, SectorCoord from, string side, SectorCoord to, string layer,
            string traversal, bool open, string signature, int cost) =>
            seed.ToString(CultureInfo.InvariantCulture) + "|" + from.X.ToString(CultureInfo.InvariantCulture) + "|" +
            from.Y.ToString(CultureInfo.InvariantCulture) + "|" + side + "|" + to.X.ToString(CultureInfo.InvariantCulture) + "|" +
            to.Y.ToString(CultureInfo.InvariantCulture) + "|" + layer + "|" + traversal + "|" + (open ? "1" : "0") + "|" +
            signature + "|" + cost.ToString(CultureInfo.InvariantCulture);

        private static bool HasExactCsvEnvelope(byte[] bytes, string header, int dataRows, int columnCount)
        {
            if (bytes == null || bytes.Length < 5 || bytes[0] != 0xEF || bytes[1] != 0xBB || bytes[2] != 0xBF) return false;
            string text;
            try { text = new UTF8Encoding(false, true).GetString(bytes, 3, bytes.Length - 3); }
            catch (DecoderFallbackException) { return false; }
            if (!text.StartsWith(header + "\r\n", StringComparison.Ordinal) || !text.EndsWith("\r\n", StringComparison.Ordinal)) return false;
            for (var index = 0; index < text.Length; index++)
            {
                if (text[index] == '\n' && (index == 0 || text[index - 1] != '\r')) return false;
                if (text[index] == '\r' && (index + 1 >= text.Length || text[index + 1] != '\n')) return false;
            }
            var lines = text.Split(new[] { "\r\n" }, StringSplitOptions.None);
            if (lines.Length != dataRows + 2 || lines[0] != header || lines[lines.Length - 1] != string.Empty) return false;
            for (var index = 0; index < lines.Length - 1; index++) if (lines[index].Split(',').Length != columnCount) return false;
            return true;
        }

        private static bool ByteEquals(byte[] left, byte[] right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Length != right.Length) return false;
            for (var index = 0; index < left.Length; index++) if (left[index] != right[index]) return false;
            return true;
        }

        private static List<MandatoryRouteValidationViolation> Unique(IEnumerable<MandatoryRouteValidationViolation> source)
        {
            var values = new List<MandatoryRouteValidationViolation>(source);
            values.Sort();
            var result = new List<MandatoryRouteValidationViolation>();
            string previous = null;
            foreach (var value in values)
            {
                if (string.Equals(previous, value.SortKey, StringComparison.Ordinal)) continue;
                result.Add(value); previous = value.SortKey;
            }
            return result;
        }

        private static void Add(ICollection<MandatoryRouteValidationViolation> values, string rule,
            MandatoryRouteGraphNodeId nodeId, MandatoryRouteGraphEdgeId edgeId, int sectorIndex, SectorCoord coordinate,
            string sourceId, string token) => values.Add(new MandatoryRouteValidationViolation(new MandatoryRouteValidationRuleId(rule),
                MandatoryRouteValidationSeverity.Error, nodeId, edgeId, coordinate, sectorIndex, sourceId, token));

        private static int Count<TKey>(IDictionary<TKey, int> values, TKey key) => values.TryGetValue(key, out var value) ? value : 0;
        private static MandatoryRouteValidationResult Invalid() =>
            new MandatoryRouteValidationResult(MandatoryRouteValidationStatus.InvalidInput, null, null);
    }
}
