using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public sealed class MicrochunkReachabilityPathWitness
    {
        private readonly IReadOnlyList<MicrochunkLocalCoord> coordinates;
        private readonly IReadOnlyList<MicrochunkTraversalEdge> edges;

        public MicrochunkId MicrochunkId { get; }
        public string SourceSocketId { get; }
        public string TargetSocketId { get; }
        public string PairedSocketId => TargetSocketId;
        public IReadOnlyList<MicrochunkLocalCoord> Coordinates => coordinates;
        public IReadOnlyList<MicrochunkLocalCoord> OrderedCoordinates => coordinates;
        public IReadOnlyList<MicrochunkTraversalEdge> Edges => edges;
        public int Cost { get; }
        public int StepCount => edges.Count;

        public MicrochunkReachabilityPathWitness(
            MicrochunkId microchunkId,
            string sourceSocketId,
            string targetSocketId,
            IEnumerable<MicrochunkLocalCoord> coordinates,
            IEnumerable<MicrochunkTraversalEdge> edges)
        {
            if (!microchunkId.IsValid) throw new ArgumentException("A valid microchunk ID is required.", nameof(microchunkId));
            if (string.IsNullOrWhiteSpace(sourceSocketId)) throw new ArgumentException("Source socket ID is required.", nameof(sourceSocketId));
            if (string.IsNullOrWhiteSpace(targetSocketId)) throw new ArgumentException("Target socket ID is required.", nameof(targetSocketId));
            if (coordinates == null) throw new ArgumentNullException(nameof(coordinates));
            if (edges == null) throw new ArgumentNullException(nameof(edges));

            var coordinateValues = new List<MicrochunkLocalCoord>(coordinates);
            var edgeValues = new List<MicrochunkTraversalEdge>();
            foreach (var edge in edges)
            {
                if (edge == null) throw new ArgumentException("Path edges cannot contain null.", nameof(edges));
                edgeValues.Add(edge);
            }

            if (coordinateValues.Count == 0)
            {
                throw new ArgumentException("A reachable path requires at least one coordinate.", nameof(coordinates));
            }

            if (edgeValues.Count != coordinateValues.Count - 1)
            {
                throw new ArgumentException("Path edges must connect each consecutive coordinate.", nameof(edges));
            }

            for (var index = 0; index < edgeValues.Count; index++)
            {
                if (edgeValues[index].SourceCoordinate != coordinateValues[index] ||
                    edgeValues[index].TargetCoordinate != coordinateValues[index + 1])
                {
                    throw new ArgumentException("Path edges must match the ordered coordinate witness.", nameof(edges));
                }
            }

            MicrochunkId = microchunkId;
            SourceSocketId = sourceSocketId;
            TargetSocketId = targetSocketId;
            this.coordinates = new ReadOnlyCollection<MicrochunkLocalCoord>(coordinateValues);
            this.edges = new ReadOnlyCollection<MicrochunkTraversalEdge>(edgeValues);
            Cost = edgeValues.Sum(value => value.Cost);
        }
    }

    public sealed class MicrochunkReachabilityResult
    {
        private readonly IReadOnlyList<MicrochunkTraversalNode> nodes;
        private readonly IReadOnlyList<MicrochunkTraversalEdge> edges;
        private readonly IReadOnlyList<MicrochunkReachabilityViolation> violations;
        private readonly IReadOnlyList<MicrochunkReachabilityPathWitness> pathWitnesses;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<MicrochunkLocalCoord>> socketEntries;

        public int EvaluatedSocketCount { get; }
        public int EvaluatedPairCount { get; }
        public int ReachablePairCount { get; }
        public int IssueCount => violations.Count;
        public bool Success => IssueCount == 0 && ReachablePairCount == EvaluatedPairCount;
        public bool IsValid => Success;
        public IReadOnlyList<MicrochunkTraversalNode> Nodes => nodes;
        public IReadOnlyList<MicrochunkTraversalEdge> Edges => edges;
        public IReadOnlyList<MicrochunkReachabilityViolation> Violations => violations;
        public IReadOnlyList<MicrochunkReachabilityPathWitness> PathWitnesses => pathWitnesses;
        public IReadOnlyList<MicrochunkReachabilityPathWitness> Witnesses => pathWitnesses;
        public IReadOnlyDictionary<string, IReadOnlyList<MicrochunkLocalCoord>> SocketEntries => socketEntries;

        public MicrochunkReachabilityResult(
            int evaluatedSocketCount,
            int evaluatedPairCount,
            int reachablePairCount,
            IEnumerable<MicrochunkReachabilityViolation> violations,
            IEnumerable<MicrochunkReachabilityPathWitness> pathWitnesses)
            : this(
                evaluatedSocketCount,
                evaluatedPairCount,
                reachablePairCount,
                violations,
                pathWitnesses,
                Array.Empty<MicrochunkTraversalNode>(),
                Array.Empty<MicrochunkTraversalEdge>(),
                new Dictionary<string, IReadOnlyList<MicrochunkLocalCoord>>(StringComparer.Ordinal))
        {
        }

        public MicrochunkReachabilityResult(
            int evaluatedSocketCount,
            int evaluatedPairCount,
            int reachablePairCount,
            IEnumerable<MicrochunkReachabilityViolation> violations,
            IEnumerable<MicrochunkReachabilityPathWitness> pathWitnesses,
            IEnumerable<MicrochunkTraversalNode> nodes,
            IEnumerable<MicrochunkTraversalEdge> edges,
            IReadOnlyDictionary<string, IReadOnlyList<MicrochunkLocalCoord>> socketEntries)
        {
            if (evaluatedSocketCount < 0) throw new ArgumentOutOfRangeException(nameof(evaluatedSocketCount));
            if (evaluatedPairCount < 0) throw new ArgumentOutOfRangeException(nameof(evaluatedPairCount));
            if (reachablePairCount < 0 || reachablePairCount > evaluatedPairCount) throw new ArgumentOutOfRangeException(nameof(reachablePairCount));
            if (violations == null) throw new ArgumentNullException(nameof(violations));
            if (pathWitnesses == null) throw new ArgumentNullException(nameof(pathWitnesses));
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));
            if (edges == null) throw new ArgumentNullException(nameof(edges));
            if (socketEntries == null) throw new ArgumentNullException(nameof(socketEntries));

            EvaluatedSocketCount = evaluatedSocketCount;
            EvaluatedPairCount = evaluatedPairCount;
            ReachablePairCount = reachablePairCount;

            this.nodes = FreezeNodes(nodes);
            this.edges = FreezeEdges(edges);
            this.violations = FreezeViolations(violations);
            this.pathWitnesses = FreezeWitnesses(pathWitnesses);
            this.socketEntries = FreezeSocketEntries(socketEntries);
        }

        private static IReadOnlyList<MicrochunkTraversalNode> FreezeNodes(IEnumerable<MicrochunkTraversalNode> source)
        {
            var values = new List<MicrochunkTraversalNode>();
            foreach (var value in source)
            {
                if (value == null) throw new ArgumentException("Nodes cannot contain null.", nameof(source));
                values.Add(value);
            }
            values.Sort((left, right) =>
            {
                var comparison = left.MicrochunkId.CompareTo(right.MicrochunkId);
                return comparison != 0 ? comparison : left.Coordinate.CompareTo(right.Coordinate);
            });
            return new ReadOnlyCollection<MicrochunkTraversalNode>(values);
        }

        private static IReadOnlyList<MicrochunkTraversalEdge> FreezeEdges(IEnumerable<MicrochunkTraversalEdge> source)
        {
            var values = new List<MicrochunkTraversalEdge>();
            foreach (var value in source)
            {
                if (value == null) throw new ArgumentException("Edges cannot contain null.", nameof(source));
                values.Add(value);
            }
            return new ReadOnlyCollection<MicrochunkTraversalEdge>(values);
        }

        private static IReadOnlyList<MicrochunkReachabilityViolation> FreezeViolations(
            IEnumerable<MicrochunkReachabilityViolation> source)
        {
            var values = new List<MicrochunkReachabilityViolation>();
            foreach (var value in source)
            {
                if (value == null) throw new ArgumentException("Violations cannot contain null.", nameof(source));
                values.Add(value);
            }
            values.Sort(CompareViolations);
            return new ReadOnlyCollection<MicrochunkReachabilityViolation>(values);
        }

        private static IReadOnlyList<MicrochunkReachabilityPathWitness> FreezeWitnesses(
            IEnumerable<MicrochunkReachabilityPathWitness> source)
        {
            var values = new List<MicrochunkReachabilityPathWitness>();
            foreach (var value in source)
            {
                if (value == null) throw new ArgumentException("Path witnesses cannot contain null.", nameof(source));
                values.Add(value);
            }
            values.Sort((left, right) =>
            {
                var comparison = left.MicrochunkId.CompareTo(right.MicrochunkId);
                if (comparison != 0) return comparison;
                comparison = string.Compare(left.SourceSocketId, right.SourceSocketId, StringComparison.Ordinal);
                if (comparison != 0) return comparison;
                return string.Compare(left.TargetSocketId, right.TargetSocketId, StringComparison.Ordinal);
            });
            return new ReadOnlyCollection<MicrochunkReachabilityPathWitness>(values);
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<MicrochunkLocalCoord>> FreezeSocketEntries(
            IReadOnlyDictionary<string, IReadOnlyList<MicrochunkLocalCoord>> source)
        {
            var values = new SortedDictionary<string, IReadOnlyList<MicrochunkLocalCoord>>(StringComparer.Ordinal);
            foreach (var pair in source)
            {
                if (string.IsNullOrWhiteSpace(pair.Key)) throw new ArgumentException("Socket entry IDs must be non-empty.", nameof(source));
                if (pair.Value == null) throw new ArgumentException("Socket entry lists cannot be null.", nameof(source));
                var coordinates = pair.Value.OrderBy(value => value.RowMajorIndex).ToList();
                values.Add(pair.Key, new ReadOnlyCollection<MicrochunkLocalCoord>(coordinates));
            }
            return new ReadOnlyDictionary<string, IReadOnlyList<MicrochunkLocalCoord>>(values);
        }

        private static int CompareViolations(
            MicrochunkReachabilityViolation left,
            MicrochunkReachabilityViolation right)
        {
            var comparison = left.MicrochunkId.CompareTo(right.MicrochunkId);
            if (comparison != 0) return comparison;
            comparison = string.Compare(left.SocketId, right.SocketId, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = string.Compare(left.PairedSocketId, right.PairedSocketId, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = string.Compare(left.Reason, right.Reason, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            return Nullable.Compare(
                left.LocalCoordinate.HasValue ? left.LocalCoordinate.Value.RowMajorIndex : (int?)null,
                right.LocalCoordinate.HasValue ? right.LocalCoordinate.Value.RowMajorIndex : (int?)null);
        }
    }
}
