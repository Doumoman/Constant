using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public sealed class MicrochunkReachabilityProbe
    {
        public const string MandatorySocketPairUnreachableReason =
            MicrochunkReachabilityViolation.MandatorySocketPairUnreachableReason;
        public const string MandatorySocketEntryUnreachableReason =
            MicrochunkReachabilityViolation.MandatorySocketEntryUnreachableReason;
        public const string CellCoverageInvalidReason =
            MicrochunkReachabilityViolation.CellCoverageInvalidReason;

        private static readonly int[] CardinalDeltaX = { -1, 1, 0, 0 };
        private static readonly int[] CardinalDeltaY = { 0, 0, -1, 1 };

        public MicrochunkReachabilityResult ValidateDefinition(
            MicrochunkDefinition definition,
            IEnumerable<MicrochunkSocketBandDefinition> bandDefinitions)
        {
            return ValidateDefinition(definition, bandDefinitions, MicrochunkReachabilityPolicy.Default, null);
        }

        public MicrochunkReachabilityResult ValidateDefinition(
            MicrochunkDefinition definition,
            IEnumerable<MicrochunkSocketBandDefinition> bandDefinitions,
            MicrochunkReachabilityPolicy policy)
        {
            return ValidateDefinition(definition, bandDefinitions, policy, null);
        }

        public MicrochunkReachabilityResult ValidateDefinition(
            MicrochunkDefinition definition,
            IEnumerable<MicrochunkSocketBandDefinition> bandDefinitions,
            Microchunk96CellValidationResult coverageResult)
        {
            return ValidateDefinition(
                definition,
                bandDefinitions,
                MicrochunkReachabilityPolicy.Default,
                coverageResult);
        }

        public MicrochunkReachabilityResult ValidateDefinition(
            MicrochunkDefinition definition,
            IEnumerable<MicrochunkSocketBandDefinition> bandDefinitions,
            Microchunk96CellValidationResult coverageResult,
            MicrochunkReachabilityPolicy policy)
        {
            return ValidateDefinition(definition, bandDefinitions, policy, coverageResult);
        }

        public MicrochunkReachabilityResult ValidateDefinition(
            MicrochunkDefinition definition,
            IEnumerable<MicrochunkSocketBandDefinition> bandDefinitions,
            MicrochunkReachabilityPolicy policy,
            Microchunk96CellValidationResult coverageResult)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (bandDefinitions == null) throw new ArgumentNullException(nameof(bandDefinitions));
            if (policy == null) throw new ArgumentNullException(nameof(policy));

            var bands = SnapshotBands(bandDefinitions);
            var mandatorySockets = definition.Sockets
                .Where(socket => socket.MandatoryAllowed &&
                                 socket.ToolRequirement == MicrochunkToolRequirement.None)
                .OrderBy(socket => socket.SocketId, StringComparer.Ordinal)
                .ToList();

            var coverage = coverageResult ?? new Microchunk96CellValidator().ValidateDefinition(
                definition,
                Microchunk96CellValidationPolicy.Complete);
            if (!IsSuccessfulCompleteCoverage(coverage))
            {
                return new MicrochunkReachabilityResult(
                    mandatorySockets.Count,
                    0,
                    0,
                    new[]
                    {
                        new MicrochunkReachabilityViolation(
                            definition.Id,
                            string.Empty,
                            string.Empty,
                            null,
                            CellCoverageInvalidReason)
                    },
                    Array.Empty<MicrochunkReachabilityPathWitness>(),
                    Array.Empty<MicrochunkTraversalNode>(),
                    Array.Empty<MicrochunkTraversalEdge>(),
                    new Dictionary<string, IReadOnlyList<MicrochunkLocalCoord>>(StringComparer.Ordinal));
            }

            var cellsByCoordinate = definition.TileCells.ToDictionary(cell => cell.Coordinate);
            var nodes = definition.TileCells
                .Where(cell => !IsBlocked(cell))
                .OrderBy(cell => cell.Coordinate.RowMajorIndex)
                .Select(cell => new MicrochunkTraversalNode(definition.Id, cell.Coordinate))
                .ToList();
            var nodeCoordinates = new HashSet<MicrochunkLocalCoord>(nodes.Select(node => node.Coordinate));
            var edges = BuildTraversalEdges(nodes, cellsByCoordinate, nodeCoordinates, policy);
            var entries = new Dictionary<string, IReadOnlyList<MicrochunkLocalCoord>>(StringComparer.Ordinal);
            var violations = new List<MicrochunkReachabilityViolation>();

            foreach (var socket in mandatorySockets)
            {
                var candidates = ResolveEntryCandidates(socket, bands);
                var validEntries = candidates
                    .Where(nodeCoordinates.Contains)
                    .OrderBy(coordinate => coordinate.RowMajorIndex)
                    .ToList();
                entries.Add(socket.SocketId, new ReadOnlyCollection<MicrochunkLocalCoord>(validEntries));

                if (validEntries.Count == 0)
                {
                    violations.Add(new MicrochunkReachabilityViolation(
                        definition.Id,
                        socket.SocketId,
                        string.Empty,
                        candidates.Count == 0 ? (MicrochunkLocalCoord?)null : candidates[0],
                        MandatorySocketEntryUnreachableReason));
                }

                foreach (var entry in validEntries)
                {
                    AddEdge(
                        edges,
                        new MicrochunkTraversalEdge(
                            entry,
                            entry,
                            MicrochunkTraversalEdge.SocketEntryMovement,
                            0));
                }
            }

            SortEdges(edges, policy);
            var adjacency = BuildAdjacency(nodes, edges);
            var pairCount = (mandatorySockets.Count * (mandatorySockets.Count - 1)) / 2;
            var reachablePairCount = 0;
            var witnesses = new List<MicrochunkReachabilityPathWitness>();

            for (var sourceIndex = 0; sourceIndex < mandatorySockets.Count; sourceIndex++)
            {
                for (var targetIndex = sourceIndex + 1; targetIndex < mandatorySockets.Count; targetIndex++)
                {
                    var source = mandatorySockets[sourceIndex];
                    var target = mandatorySockets[targetIndex];
                    var sourceEntries = entries[source.SocketId];
                    var targetEntries = entries[target.SocketId];

                    MicrochunkReachabilityPathWitness witness;
                    if (sourceEntries.Count > 0 &&
                        targetEntries.Count > 0 &&
                        TryFindPath(
                            definition.Id,
                            source.SocketId,
                            target.SocketId,
                            sourceEntries,
                            targetEntries,
                            adjacency,
                            out witness))
                    {
                        reachablePairCount++;
                        witnesses.Add(witness);
                    }
                    else
                    {
                        violations.Add(new MicrochunkReachabilityViolation(
                            definition.Id,
                            source.SocketId,
                            target.SocketId,
                            null,
                            MandatorySocketPairUnreachableReason));
                    }
                }
            }

            return new MicrochunkReachabilityResult(
                mandatorySockets.Count,
                pairCount,
                reachablePairCount,
                violations,
                witnesses,
                nodes,
                edges,
                entries);
        }

        private static bool IsSuccessfulCompleteCoverage(Microchunk96CellValidationResult coverage)
        {
            return coverage != null &&
                   coverage.Success &&
                   coverage.EvaluatedMicrochunkCount == 1 &&
                   coverage.EvaluatedRecordCount == MicrochunkConstants.CellCount &&
                   coverage.InRangeUniqueCoordinateCount == MicrochunkConstants.CellCount &&
                   coverage.MissingCoordinateCount == 0 &&
                   coverage.DuplicateCoordinateCount == 0 &&
                   coverage.OutOfRangeRecordCount == 0;
        }

        private static List<MicrochunkSocketBandDefinition> SnapshotBands(
            IEnumerable<MicrochunkSocketBandDefinition> source)
        {
            var values = new List<MicrochunkSocketBandDefinition>();
            foreach (var value in source)
            {
                if (value == null)
                {
                    throw new ArgumentException("Socket band definitions cannot contain null.", nameof(source));
                }
                values.Add(value);
            }
            return values;
        }

        private static bool IsBlocked(MicrochunkTileCell cell)
        {
            var occupancy = MicrochunkTileLayerOccupancy.FromCell(cell);
            return occupancy.IsOccupied(MicrochunkTileLayer.GroundSolid) ||
                   occupancy.IsOccupied(MicrochunkTileLayer.Breakable) ||
                   occupancy.IsOccupied(MicrochunkTileLayer.Hazard) ||
                   occupancy.IsOccupied(MicrochunkTileLayer.Liquid);
        }

        private static List<MicrochunkTraversalEdge> BuildTraversalEdges(
            IReadOnlyList<MicrochunkTraversalNode> nodes,
            IReadOnlyDictionary<MicrochunkLocalCoord, MicrochunkTileCell> cells,
            ISet<MicrochunkLocalCoord> nodeCoordinates,
            MicrochunkReachabilityPolicy policy)
        {
            var edges = new List<MicrochunkTraversalEdge>();
            foreach (var node in nodes)
            {
                var source = node.Coordinate;

                for (var index = 0; index < CardinalDeltaX.Length; index++)
                {
                    MicrochunkLocalCoord target;
                    if (!MicrochunkLocalCoord.TryCreate(
                            source.X + CardinalDeltaX[index],
                            source.Y + CardinalDeltaY[index],
                            out target) ||
                        !nodeCoordinates.Contains(target))
                    {
                        continue;
                    }

                    edges.Add(new MicrochunkTraversalEdge(
                        source,
                        target,
                        MicrochunkTraversalEdge.FloodMovement,
                        1));

                    if (target.Y == source.Y)
                    {
                        edges.Add(new MicrochunkTraversalEdge(
                            source,
                            target,
                            MicrochunkTraversalEdge.WalkMovement,
                            1));
                    }
                }

                for (var rise = 1; rise <= policy.MaximumJumpRise; rise++)
                {
                    for (var horizontal = -policy.MaximumJumpHorizontalSpan;
                         horizontal <= policy.MaximumJumpHorizontalSpan;
                         horizontal++)
                    {
                        MicrochunkLocalCoord target;
                        if (!MicrochunkLocalCoord.TryCreate(source.X + horizontal, source.Y + rise, out target) ||
                            !nodeCoordinates.Contains(target))
                        {
                            continue;
                        }

                        edges.Add(new MicrochunkTraversalEdge(
                            source,
                            target,
                            MicrochunkTraversalEdge.JumpMovement,
                            1));
                    }
                }

                for (var drop = 1; drop <= policy.MaximumDropDistance; drop++)
                {
                    MicrochunkLocalCoord target;
                    if (!MicrochunkLocalCoord.TryCreate(source.X, source.Y - drop, out target) ||
                        !nodeCoordinates.Contains(target))
                    {
                        continue;
                    }

                    edges.Add(new MicrochunkTraversalEdge(
                        source,
                        target,
                        MicrochunkTraversalEdge.DropMovement,
                        1));
                }

                for (var vertical = -1; vertical <= 1; vertical += 2)
                {
                    MicrochunkLocalCoord target;
                    if (!MicrochunkLocalCoord.TryCreate(source.X, source.Y + vertical, out target) ||
                        !nodeCoordinates.Contains(target))
                    {
                        continue;
                    }

                    if (policy.IsClimbMarker(cells[source].MarkerCode) ||
                        policy.IsClimbMarker(cells[target].MarkerCode))
                    {
                        edges.Add(new MicrochunkTraversalEdge(
                            source,
                            target,
                            MicrochunkTraversalEdge.ClimbMovement,
                            1));
                    }
                }
            }

            SortEdges(edges, policy);
            return edges;
        }

        private static List<MicrochunkLocalCoord> ResolveEntryCandidates(
            MicrochunkSocketDefinition socket,
            IReadOnlyList<MicrochunkSocketBandDefinition> bands)
        {
            var matches = bands
                .Where(value => string.Equals(value.BandId, socket.BandId, StringComparison.Ordinal))
                .ToList();
            if (matches.Count != 1) return new List<MicrochunkLocalCoord>();

            var band = matches[0];
            var horizontalSide = socket.Side == MicrochunkSide.Left || socket.Side == MicrochunkSide.Right;
            var expectedAxis = horizontalSide
                ? MicrochunkEdgeAxis.HorizontalEdge
                : MicrochunkEdgeAxis.VerticalEdge;
            var maximum = horizontalSide
                ? MicrochunkConstants.HeightTiles - 1
                : MicrochunkConstants.WidthTiles - 1;
            if (band.Axis != expectedAxis ||
                band.MinimumLocalCoordinate < 0 ||
                band.MaximumLocalCoordinate > maximum ||
                band.MinimumLocalCoordinate > band.MaximumLocalCoordinate)
            {
                return new List<MicrochunkLocalCoord>();
            }

            var coordinates = new List<MicrochunkLocalCoord>();
            for (var value = band.MinimumLocalCoordinate; value <= band.MaximumLocalCoordinate; value++)
            {
                switch (socket.Side)
                {
                    case MicrochunkSide.Left:
                        coordinates.Add(new MicrochunkLocalCoord(0, value));
                        break;
                    case MicrochunkSide.Right:
                        coordinates.Add(new MicrochunkLocalCoord(MicrochunkConstants.WidthTiles - 1, value));
                        break;
                    case MicrochunkSide.Down:
                        coordinates.Add(new MicrochunkLocalCoord(value, 0));
                        break;
                    case MicrochunkSide.Up:
                        coordinates.Add(new MicrochunkLocalCoord(value, MicrochunkConstants.HeightTiles - 1));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(socket.Side));
                }
            }

            coordinates.Sort((left, right) => left.RowMajorIndex.CompareTo(right.RowMajorIndex));
            return coordinates;
        }

        private static void AddEdge(
            ICollection<MicrochunkTraversalEdge> edges,
            MicrochunkTraversalEdge candidate)
        {
            if (edges.Any(value =>
                    value.SourceCoordinate == candidate.SourceCoordinate &&
                    value.TargetCoordinate == candidate.TargetCoordinate &&
                    value.MovementKind == candidate.MovementKind))
            {
                return;
            }

            edges.Add(candidate);
        }

        private static void SortEdges(
            List<MicrochunkTraversalEdge> edges,
            MicrochunkReachabilityPolicy policy)
        {
            edges.Sort((left, right) =>
            {
                var comparison = left.SourceCoordinate.RowMajorIndex.CompareTo(right.SourceCoordinate.RowMajorIndex);
                if (comparison != 0) return comparison;
                comparison = policy.GetNeighborOrder(left.MovementKind)
                    .CompareTo(policy.GetNeighborOrder(right.MovementKind));
                if (comparison != 0) return comparison;
                comparison = left.TargetCoordinate.RowMajorIndex.CompareTo(right.TargetCoordinate.RowMajorIndex);
                if (comparison != 0) return comparison;
                comparison = string.Compare(left.MovementKind, right.MovementKind, StringComparison.Ordinal);
                if (comparison != 0) return comparison;
                return left.Cost.CompareTo(right.Cost);
            });
        }

        private static Dictionary<MicrochunkLocalCoord, IReadOnlyList<MicrochunkTraversalEdge>> BuildAdjacency(
            IEnumerable<MicrochunkTraversalNode> nodes,
            IEnumerable<MicrochunkTraversalEdge> edges)
        {
            var mutable = nodes.ToDictionary(
                node => node.Coordinate,
                node => new List<MicrochunkTraversalEdge>());
            foreach (var edge in edges)
            {
                mutable[edge.SourceCoordinate].Add(edge);
            }

            return mutable.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<MicrochunkTraversalEdge>)new ReadOnlyCollection<MicrochunkTraversalEdge>(pair.Value));
        }

        private static bool TryFindPath(
            MicrochunkId microchunkId,
            string sourceSocketId,
            string targetSocketId,
            IEnumerable<MicrochunkLocalCoord> sourceEntries,
            IEnumerable<MicrochunkLocalCoord> targetEntries,
            IReadOnlyDictionary<MicrochunkLocalCoord, IReadOnlyList<MicrochunkTraversalEdge>> adjacency,
            out MicrochunkReachabilityPathWitness witness)
        {
            var targets = new HashSet<MicrochunkLocalCoord>(targetEntries);
            var visited = new HashSet<MicrochunkLocalCoord>();
            var predecessors = new Dictionary<MicrochunkLocalCoord, MicrochunkTraversalEdge>();
            var queue = new Queue<MicrochunkLocalCoord>();
            foreach (var source in sourceEntries.OrderBy(value => value.RowMajorIndex))
            {
                if (visited.Add(source)) queue.Enqueue(source);
            }

            MicrochunkLocalCoord? reached = null;
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (targets.Contains(current))
                {
                    reached = current;
                    break;
                }

                IReadOnlyList<MicrochunkTraversalEdge> neighbors;
                if (!adjacency.TryGetValue(current, out neighbors)) continue;
                foreach (var edge in neighbors)
                {
                    if (!visited.Add(edge.TargetCoordinate)) continue;
                    predecessors.Add(edge.TargetCoordinate, edge);
                    queue.Enqueue(edge.TargetCoordinate);
                }
            }

            if (!reached.HasValue)
            {
                witness = null;
                return false;
            }

            var reverseCoordinates = new List<MicrochunkLocalCoord> { reached.Value };
            var reverseEdges = new List<MicrochunkTraversalEdge>();
            var cursor = reached.Value;
            MicrochunkTraversalEdge predecessor;
            while (predecessors.TryGetValue(cursor, out predecessor))
            {
                reverseEdges.Add(predecessor);
                cursor = predecessor.SourceCoordinate;
                reverseCoordinates.Add(cursor);
            }

            reverseCoordinates.Reverse();
            reverseEdges.Reverse();
            witness = new MicrochunkReachabilityPathWitness(
                microchunkId,
                sourceSocketId,
                targetSocketId,
                reverseCoordinates,
                reverseEdges);
            return true;
        }
    }
}
