using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryRouteGraphBuilder
    {
        public MandatoryRouteGraphBuildResult Build(MandatoryRouteTerminalSet terminalSet, MandatoryConnectorTree connectorTree,
            HorizontalBackbonePlan horizontalPlan, VerticalGatewayPlan verticalPlan, UpDownConflictResolutionPlan conflictPlan,
            MandatoryRouteLoopPlan loopPlan)
        {
            return Build(terminalSet, connectorTree == null ? null : connectorTree.SourceRouteMaskLookup, connectorTree,
                horizontalPlan, verticalPlan, conflictPlan, loopPlan);
        }

        public MandatoryRouteGraphBuildResult Build(MandatoryRouteTerminalSet terminalSet, MandatoryRouteMaskLookup maskLookup,
            MandatoryConnectorTree connectorTree, HorizontalBackbonePlan horizontalPlan, VerticalGatewayPlan verticalPlan,
            UpDownConflictResolutionPlan conflictPlan, MandatoryRouteLoopPlan loopPlan)
        {
            if (terminalSet == null || maskLookup == null || connectorTree == null || horizontalPlan == null || verticalPlan == null || conflictPlan == null || loopPlan == null)
                return Invalid(MandatoryRouteGraphBuildErrorCode.NullInput, string.Empty, -1, "All seven source artifacts are required.");
            if (!SourcesMatch(terminalSet, maskLookup, connectorTree, horizontalPlan, verticalPlan, conflictPlan, loopPlan))
                return Invalid(MandatoryRouteGraphBuildErrorCode.SourceIdentityMismatch, string.Empty, -1, "Source artifact identities do not form the exact P03 chain.");
            if (terminalSet.TerminalCount != 7 || connectorTree.TreeEdgeCount != 6 || horizontalPlan.SegmentCount != 6 || verticalPlan.GatewayPairCount != 4)
                return Invalid(MandatoryRouteGraphBuildErrorCode.InvalidCardinality, string.Empty, -1, "Starter source cardinalities are invalid.");
            if (conflictPlan.UnresolvedCount != 0)
                return Invalid(MandatoryRouteGraphBuildErrorCode.UnresolvedConflict, string.Empty, -1, "Unresolved up/down conflicts cannot be published.");
            if (!loopPlan.MeetsMinimum || loopPlan.IndependentLoopCount < MandatoryRouteLoopPlan.MinimumLoopCount)
                return Invalid(MandatoryRouteGraphBuildErrorCode.MinimumLoopCountNotMet, string.Empty, -1, "At least two independent loops are required.");

            try
            {
                return BuildChecked(terminalSet, maskLookup, connectorTree, horizontalPlan, verticalPlan, conflictPlan, loopPlan);
            }
            catch (OverflowException)
            {
                return Invalid(MandatoryRouteGraphBuildErrorCode.ArithmeticOverflow, string.Empty, -1, "Checked graph arithmetic overflowed.");
            }
        }

        private static MandatoryRouteGraphBuildResult BuildChecked(MandatoryRouteTerminalSet terminalSet, MandatoryRouteMaskLookup maskLookup,
            MandatoryConnectorTree connectorTree, HorizontalBackbonePlan horizontalPlan, VerticalGatewayPlan verticalPlan,
            UpDownConflictResolutionPlan conflictPlan, MandatoryRouteLoopPlan loopPlan)
        {
            var states = new Dictionary<int, CellState>();
            var connections = new Dictionary<string, ConnectionSeed>(StringComparer.Ordinal);

            foreach (var segment in horizontalPlan.Segments)
            {
                foreach (var cell in segment.Cells)
                {
                    var state = GetState(states, cell.Coord);
                    state.OpenLeft |= cell.OpensLeft;
                    state.OpenRight |= cell.OpensRight;
                }
                for (var index = 1; index < segment.Cells.Count; index++)
                {
                    var from = segment.Cells[index - 1].Coord;
                    var to = segment.Cells[index].Coord;
                    if (Manhattan(from, to) == 1) AddConnection(states, connections, from, to, 1, segment.SegmentId.Value);
                }
            }

            foreach (var pair in verticalPlan.GatewayPairs)
            {
                for (var index = 0; index < pair.SpanCells.Count; index++)
                {
                    var state = GetState(states, pair.SpanCells[index]);
                    state.GatewayIds.Add(pair.GatewayId.Value);
                    if (index == 0 || index == pair.SpanCells.Count - 1)
                    {
                        state.OpenLeft = true;
                        state.OpenRight = true;
                    }
                }
                for (var index = 1; index < pair.SpanCells.Count; index++)
                    AddConnection(states, connections, pair.SpanCells[index - 1], pair.SpanCells[index], 2, pair.GatewayId.Value);
                foreach (var junction in pair.Type4JunctionCells)
                {
                    var state = GetState(states, junction.Coord);
                    state.OpenLeft |= junction.OpensLeft;
                    state.OpenRight |= junction.OpensRight;
                    state.OpenUp |= junction.OpensUp;
                    state.OpenDown |= junction.OpensDown;
                }
                if (pair.Upper.IsEndpointAdapter) GetState(states, pair.Upper.Coord).ApprovedReservedAdapter = true;
                if (pair.Lower.IsEndpointAdapter) GetState(states, pair.Lower.Coord).ApprovedReservedAdapter = true;
            }

            foreach (var resolution in conflictPlan.Resolutions)
            {
                for (var index = 0; index < resolution.InclusiveSpan.Count; index++)
                {
                    var state = GetState(states, resolution.InclusiveSpan[index]);
                    state.GatewayIds.Add(resolution.SourceGatewayId.Value);
                    if (index == 0 || index == resolution.InclusiveSpan.Count - 1)
                    {
                        state.OpenLeft = true;
                        state.OpenRight = true;
                    }
                }
                for (var index = 1; index < resolution.InclusiveSpan.Count; index++)
                    AddConnection(states, connections, resolution.InclusiveSpan[index - 1], resolution.InclusiveSpan[index], 3, resolution.ConflictId.Value);
            }

            foreach (var loop in loopPlan.Loops)
            {
                foreach (var coord in loop.InclusiveOrderedCells) GetState(states, coord).LoopIds.Add(loop.LoopId.Value);
                for (var index = 1; index < loop.InclusiveOrderedCells.Count; index++)
                    AddConnection(states, connections, loop.InclusiveOrderedCells[index - 1], loop.InclusiveOrderedCells[index], 4, loop.LoopId.Value);
            }

            var siteSnapshot = terminalSet.SourceSiteSnapshot;
            var sourceWorld = terminalSet.SourceBiomePublication.WorldWithBiomeAssignments;
            foreach (var terminal in terminalSet.Terminals)
            {
                var index = WorldGridIndex.ToIndex(terminal.ApproachSector);
                if (!states.TryGetValue(index, out var state))
                    return Invalid(MandatoryRouteGraphBuildErrorCode.MissingTerminal, terminal.TerminalId.Value, index, "A mandatory terminal is absent from route cells.");
                state.TerminalIds.Add(terminal.TerminalId.Value);
                state.SiteIds.Add(terminal.ReservationId.Value);
                if (terminal.Kind == MandatoryRouteTerminalKind.Start) state.ApprovedReservedAdapter = true;
                var anchorIndex = WorldGridIndex.ToIndex(terminal.AnchorSector);
                if (states.TryGetValue(anchorIndex, out var anchorState))
                {
                    anchorState.TerminalIds.Add(terminal.TerminalId.Value);
                    anchorState.SiteIds.Add(terminal.ReservationId.Value);
                    anchorState.ApprovedReservedAdapter = true;
                }
            }

            foreach (var state in states.Values)
            {
                var sourceCell = sourceWorld.GetCell(state.Index);
                if (sourceCell.Role == GeneratedSectorRole.InactiveBuffer)
                    return Invalid(MandatoryRouteGraphBuildErrorCode.InactiveRouteCell, string.Empty, state.Index, "Mandatory route enters an inactive cell.");
                var reservation = siteSnapshot.GetSector(state.Index);
                if (reservation.IsReserved)
                {
                    if (reservation.ReservationId.HasValue) state.SiteIds.Add(reservation.ReservationId.Value.Value);
                    if (!state.ApprovedReservedAdapter)
                        return Invalid(MandatoryRouteGraphBuildErrorCode.ReservedInteriorRouteCell, reservation.ReservationId.HasValue ? reservation.ReservationId.Value.Value : string.Empty,
                            state.Index, "Mandatory route enters a reserved interior cell that is not an approved adapter.");
                }
            }

            var family = new MandatoryRouteMaskFamily(maskLookup);
            foreach (var state in states.Values)
            {
                if (state.OpenUp != state.OpenDown)
                {
                    state.OpenLeft = true;
                    state.OpenRight = true;
                }
                if (!family.TryResolve(state.OpenLeft, state.OpenRight, state.OpenUp, state.OpenDown, out var mask))
                    return Invalid(MandatoryRouteGraphBuildErrorCode.UnsupportedOpenMask, string.Empty, state.Index, "Mandatory route open combination is unsupported.");
                state.Mask = mask;
            }

            var adjacency = BuildAdjacency(states, connections.Values);
            var startIndex = WorldGridIndex.ToIndex(terminalSet.StartTerminal.ApproachSector);
            var distances = BreadthFirstDistances(startIndex, adjacency);
            foreach (var terminal in terminalSet.Terminals)
            {
                var index = WorldGridIndex.ToIndex(terminal.ApproachSector);
                if (!distances.ContainsKey(index))
                    return Invalid(MandatoryRouteGraphBuildErrorCode.UnreachableTerminal, terminal.TerminalId.Value, index, "Start BFS cannot reach a mandatory terminal.");
            }
            foreach (var state in states.Values)
                if (!distances.ContainsKey(state.Index))
                    return Invalid(MandatoryRouteGraphBuildErrorCode.UnreachableTerminal, string.Empty, state.Index, "Start BFS cannot reach a mandatory route cell.");

            var nodes = new List<MandatoryRouteGraphNode>();
            var nodesByIndex = new Dictionary<int, MandatoryRouteGraphNode>();
            foreach (var state in states.Values.OrderBy(value => value.Index))
            {
                var id = new MandatoryRouteGraphNodeId("NODE_" + state.Index.ToString("D3", CultureInfo.InvariantCulture) + "_MANDATORY");
                var node = new MandatoryRouteGraphNode(id, state.Coord, state.Mask.MaskId, state.OpenLeft, state.OpenRight, state.OpenUp, state.OpenDown,
                    distances[state.Index], state.TerminalIds, state.SiteIds, state.LoopIds, state.GatewayIds);
                nodes.Add(node);
                nodesByIndex.Add(state.Index, node);
            }

            var directed = BuildDirectedEdges(terminalSet.WorldSeed, connections.Values, nodesByIndex);
            if (!HasExactReciprocity(directed))
                return Invalid(MandatoryRouteGraphBuildErrorCode.BrokenReciprocity, string.Empty, -1, "Every directed edge must have one exact reverse edge.");
            var generatedRows = new List<GeneratedWorldEdge>();
            foreach (var edge in directed)
            {
                generatedRows.Add(new GeneratedWorldEdge(terminalSet.WorldSeed, WorldGridIndex.ToCoordinate(edge.FromSectorIndex), edge.Side,
                    WorldGridIndex.ToCoordinate(edge.ToSectorIndex), edge.Layer, edge.TraversalKind, edge.Open, edge.EdgeSignatureId, edge.CostTiles));
            }
            var edgeCsv = GeneratedWorldEdgesCsvSerializer.Serialize(generatedRows);

            var stampedCells = new List<SectorCell>(WorldGenConstants.SectorCount);
            foreach (var sourceCell in sourceWorld.Cells)
            {
                if (!states.TryGetValue(sourceCell.Index, out var state)) { stampedCells.Add(sourceCell); continue; }
                var role = sourceCell.Role == GeneratedSectorRole.ReservedSite ? GeneratedSectorRole.ReservedSite : GeneratedSectorRole.Mandatory;
                stampedCells.Add(new SectorCell(sourceCell.Index, sourceCell.Coordinate, role, sourceCell.PrimaryBiomeId, sourceCell.SecondaryBiomeId,
                    sourceCell.PatchId, state.Mask.MaskId, sourceCell.SpecialSiteInstanceId, sourceCell.BoundaryProfileId, sourceCell.SectorRecipeId,
                    sourceCell.ReservationId, distances[sourceCell.Index], true));
            }
            var stampedWorld = new GeneratedWorldData(sourceWorld.Seed, stampedCells);
            var graphCells = new List<MandatoryRouteGraphCell>();
            foreach (var state in states.Values.OrderBy(value => value.Index))
                graphCells.Add(new MandatoryRouteGraphCell(sourceWorld.GetCell(state.Index), stampedWorld.GetCell(state.Index), state.Mask,
                    state.OpenLeft, state.OpenRight, state.OpenUp, state.OpenDown, state.ApprovedReservedAdapter));

            var sectorCsv = GeneratedWorldDataCsvSerializer.Serialize(stampedWorld);
            var counts = states.Values.GroupBy(value => value.Mask.MaskId).ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            var diagnostics = new MandatoryRouteGraphDiagnostics(terminalSet.TerminalCount, connectorTree.TreeEdgeCount, horizontalPlan.SegmentCount,
                verticalPlan.GatewayPairCount, conflictPlan.ResolvedCount, loopPlan.LoopCount, nodes.Count, directed.Count, graphCells.Count,
                Count(counts, MandatoryRouteMaskFamily.Type1Id), Count(counts, MandatoryRouteMaskFamily.Type2Id), Count(counts, MandatoryRouteMaskFamily.Type3Id),
                Count(counts, MandatoryRouteMaskFamily.Type4UdId), Count(counts, MandatoryRouteMaskFamily.Type4LudId),
                Count(counts, MandatoryRouteMaskFamily.Type4RudId), Count(counts, MandatoryRouteMaskFamily.Type4LrudId),
                terminalSet.TerminalCount, generatedRows.Count, sectorCsv.Length, edgeCsv.Length);
            var graph = new MandatoryRouteGraph(terminalSet, maskLookup, connectorTree, horizontalPlan, verticalPlan, conflictPlan, loopPlan, family,
                nodes, directed, graphCells, stampedWorld, edgeCsv);
            return new MandatoryRouteGraphBuildResult(MandatoryRouteGraphBuildStatus.Completed, graph, diagnostics, Array.Empty<MandatoryRouteGraphBuildError>());
        }

        private static bool SourcesMatch(MandatoryRouteTerminalSet terminalSet, MandatoryRouteMaskLookup maskLookup, MandatoryConnectorTree connectorTree,
            HorizontalBackbonePlan horizontalPlan, VerticalGatewayPlan verticalPlan, UpDownConflictResolutionPlan conflictPlan, MandatoryRouteLoopPlan loopPlan)
        {
            return ReferenceEquals(connectorTree.SourceTerminalSet, terminalSet) && ReferenceEquals(connectorTree.SourceRouteMaskLookup, maskLookup) &&
                ReferenceEquals(horizontalPlan.SourceConnectorTree, connectorTree) && ReferenceEquals(horizontalPlan.SourceRouteMaskLookup, maskLookup) &&
                ReferenceEquals(verticalPlan.SourceHorizontalPlan, horizontalPlan) && ReferenceEquals(verticalPlan.SourceRouteMaskLookup, maskLookup) &&
                ReferenceEquals(conflictPlan.SourceVerticalGatewayPlan, verticalPlan) && ReferenceEquals(conflictPlan.SourceRouteMaskLookup, maskLookup) &&
                ReferenceEquals(loopPlan.SourceTerminalSet, terminalSet) && ReferenceEquals(loopPlan.SourceConnectorTree, connectorTree) &&
                ReferenceEquals(loopPlan.SourceHorizontalBackbonePlan, horizontalPlan) && ReferenceEquals(loopPlan.SourceVerticalGatewayPlan, verticalPlan) &&
                ReferenceEquals(loopPlan.SourceConflictResolutionPlan, conflictPlan) &&
                ReferenceEquals(terminalSet.SourceSiteSnapshot, horizontalPlan.SourceSiteSnapshot) &&
                ReferenceEquals(terminalSet.SourceBiomePublication, horizontalPlan.SourceBiomePublication) &&
                ReferenceEquals(terminalSet.SourceSiteSnapshot, verticalPlan.SourceSiteSnapshot) &&
                ReferenceEquals(terminalSet.SourceBiomePublication, verticalPlan.SourceBiomePublication) &&
                ReferenceEquals(terminalSet.SourceSiteSnapshot, conflictPlan.SourceSiteSnapshot) &&
                ReferenceEquals(terminalSet.SourceBiomePublication, conflictPlan.SourceBiomePublication);
        }

        private static CellState GetState(IDictionary<int, CellState> states, SectorCoord coord)
        {
            if (!WorldCoordinateUtility.IsValid(coord)) throw new ArgumentOutOfRangeException(nameof(coord));
            var index = WorldGridIndex.ToIndex(coord);
            if (!states.TryGetValue(index, out var state)) { state = new CellState(index, coord); states.Add(index, state); }
            return state;
        }

        private static void AddConnection(IDictionary<int, CellState> states, IDictionary<string, ConnectionSeed> connections,
            SectorCoord from, SectorCoord to, int phase, string sourceId)
        {
            if (!WorldCoordinateUtility.IsValid(from) || !WorldCoordinateUtility.IsValid(to) || Manhattan(from, to) != 1)
                throw new ArgumentOutOfRangeException(nameof(to));
            var left = GetState(states, from);
            var right = GetState(states, to);
            SetOpen(left, right);
            SetOpen(right, left);
            var minimum = Math.Min(left.Index, right.Index);
            var maximum = Math.Max(left.Index, right.Index);
            var key = minimum.ToString(CultureInfo.InvariantCulture) + ":" + maximum.ToString(CultureInfo.InvariantCulture);
            var candidate = new ConnectionSeed(minimum, maximum, phase, sourceId ?? string.Empty);
            if (!connections.TryGetValue(key, out var existing) || candidate.CompareTo(existing) < 0) connections[key] = candidate;
        }

        private static void SetOpen(CellState from, CellState to)
        {
            if (to.Coord.X < from.Coord.X) from.OpenLeft = true;
            else if (to.Coord.X > from.Coord.X) from.OpenRight = true;
            else if (to.Coord.Y > from.Coord.Y) from.OpenUp = true;
            else from.OpenDown = true;
        }

        private static Dictionary<int, List<int>> BuildAdjacency(IReadOnlyDictionary<int, CellState> states, IEnumerable<ConnectionSeed> connections)
        {
            var result = new Dictionary<int, List<int>>();
            foreach (var index in states.Keys) result.Add(index, new List<int>());
            foreach (var edge in connections)
            {
                result[edge.FirstIndex].Add(edge.SecondIndex);
                result[edge.SecondIndex].Add(edge.FirstIndex);
            }
            foreach (var values in result.Values) values.Sort();
            return result;
        }

        private static Dictionary<int, int> BreadthFirstDistances(int start, IReadOnlyDictionary<int, List<int>> adjacency)
        {
            var distances = new Dictionary<int, int> { { start, 0 } };
            var queue = new Queue<int>(); queue.Enqueue(start);
            while (queue.Count != 0)
            {
                var current = queue.Dequeue();
                foreach (var next in adjacency[current]) if (!distances.ContainsKey(next))
                {
                    distances.Add(next, checked(distances[current] + 1));
                    queue.Enqueue(next);
                }
            }
            return distances;
        }

        private static List<MandatoryRouteGraphEdge> BuildDirectedEdges(ulong seed, IEnumerable<ConnectionSeed> connections,
            IReadOnlyDictionary<int, MandatoryRouteGraphNode> nodes)
        {
            var values = new List<DirectedSeed>();
            foreach (var connection in connections)
            {
                var first = WorldGridIndex.ToCoordinate(connection.FirstIndex);
                var second = WorldGridIndex.ToCoordinate(connection.SecondIndex);
                values.Add(new DirectedSeed(connection.FirstIndex, connection.SecondIndex, Side(first, second), connection.SourceId));
                values.Add(new DirectedSeed(connection.SecondIndex, connection.FirstIndex, Side(second, first), connection.SourceId));
            }
            values.Sort(DirectedSeed.Compare);
            var result = new List<MandatoryRouteGraphEdge>(values.Count);
            for (var index = 0; index < values.Count; index++)
            {
                var value = values[index];
                var horizontal = value.Side == "L" || value.Side == "R";
                var id = new MandatoryRouteGraphEdgeId("EDGE_" + index.ToString("D3", CultureInfo.InvariantCulture) + "_" + value.Side + "_MANDATORY");
                result.Add(new MandatoryRouteGraphEdge(id, nodes[value.FromIndex].NodeId, nodes[value.ToIndex].NodeId,
                    value.FromIndex, value.ToIndex, value.Side, Reverse(value.Side), horizontal ? "WALK" : "DROP_CLIMB_PAIR",
                    horizontal ? "EDGE_H_MID_WALK" : "EDGE_V_CENTER_CLIMB", horizontal ? WorldGenConstants.SectorWidthTiles : WorldGenConstants.SectorHeightTiles,
                    value.SourceId));
            }
            return result;
        }

        public static bool HasExactReciprocity(IEnumerable<MandatoryRouteGraphEdge> sourceEdges)
        {
            if (sourceEdges == null) return false;
            var edges = new List<MandatoryRouteGraphEdge>(sourceEdges);
            foreach (var edge in edges)
            {
                if (edge == null) return false;
                var matches = 0;
                foreach (var candidate in edges)
                    if (candidate.FromSectorIndex == edge.ToSectorIndex && candidate.ToSectorIndex == edge.FromSectorIndex &&
                        candidate.Side == edge.ReverseSide && candidate.ReverseSide == edge.Side && candidate.CostTiles == edge.CostTiles &&
                        candidate.SourceArtifactId == edge.SourceArtifactId) matches++;
                if (matches != 1) return false;
            }
            return true;
        }

        private static string Side(SectorCoord from, SectorCoord to) => to.X < from.X ? "L" : to.X > from.X ? "R" : to.Y > from.Y ? "U" : "D";
        private static string Reverse(string side) => side == "L" ? "R" : side == "R" ? "L" : side == "U" ? "D" : "U";
        private static int Manhattan(SectorCoord left, SectorCoord right) => Math.Abs(left.X - right.X) + Math.Abs(left.Y - right.Y);
        private static int Count(IReadOnlyDictionary<string, int> values, string id) => values.TryGetValue(id, out var count) ? count : 0;
        private static MandatoryRouteGraphBuildResult Invalid(MandatoryRouteGraphBuildErrorCode code, string sourceId, int sectorIndex, string message) =>
            new MandatoryRouteGraphBuildResult(MandatoryRouteGraphBuildStatus.InvalidInput, null, null,
                new[] { new MandatoryRouteGraphBuildError(code, sourceId, sectorIndex, message) });

        private sealed class CellState
        {
            public CellState(int index, SectorCoord coord)
            {
                Index = index; Coord = coord;
                TerminalIds = new SortedSet<string>(StringComparer.Ordinal);
                SiteIds = new SortedSet<string>(StringComparer.Ordinal);
                LoopIds = new SortedSet<string>(StringComparer.Ordinal);
                GatewayIds = new SortedSet<string>(StringComparer.Ordinal);
            }
            public int Index { get; }
            public SectorCoord Coord { get; }
            public bool OpenLeft { get; set; }
            public bool OpenRight { get; set; }
            public bool OpenUp { get; set; }
            public bool OpenDown { get; set; }
            public bool ApprovedReservedAdapter { get; set; }
            public MandatoryRouteMaskFamily.Entry Mask { get; set; }
            public SortedSet<string> TerminalIds { get; }
            public SortedSet<string> SiteIds { get; }
            public SortedSet<string> LoopIds { get; }
            public SortedSet<string> GatewayIds { get; }
        }

        private sealed class ConnectionSeed : IComparable<ConnectionSeed>
        {
            public ConnectionSeed(int firstIndex, int secondIndex, int phase, string sourceId)
            { FirstIndex = firstIndex; SecondIndex = secondIndex; Phase = phase; SourceId = sourceId; }
            public int FirstIndex { get; }
            public int SecondIndex { get; }
            public int Phase { get; }
            public string SourceId { get; }
            public int CompareTo(ConnectionSeed other)
            {
                var order = Phase.CompareTo(other.Phase);
                return order != 0 ? order : string.Compare(SourceId, other.SourceId, StringComparison.Ordinal);
            }
        }

        private sealed class DirectedSeed
        {
            public DirectedSeed(int fromIndex, int toIndex, string side, string sourceId)
            { FromIndex = fromIndex; ToIndex = toIndex; Side = side; SourceId = sourceId; }
            public int FromIndex { get; }
            public int ToIndex { get; }
            public string Side { get; }
            public string SourceId { get; }
            public static int Compare(DirectedSeed left, DirectedSeed right)
            {
                var order = left.FromIndex.CompareTo(right.FromIndex);
                if (order != 0) return order;
                order = SideOrder(left.Side).CompareTo(SideOrder(right.Side));
                return order != 0 ? order : left.ToIndex.CompareTo(right.ToIndex);
            }
            private static int SideOrder(string side) => side == "L" ? 0 : side == "R" ? 1 : side == "U" ? 2 : 3;
        }
    }
}
