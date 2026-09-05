using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.WorldGeneration.Validation
{
    public sealed class NakedMovementKindCount : IComparable<NakedMovementKindCount>
    {
        public NakedMovementKindCount(TraversalMovementKind movementKind, int count)
        {
            MovementKind = movementKind;
            Count = count;
        }

        public TraversalMovementKind MovementKind { get; }
        public int Count { get; }
        public string StableToken => "MOVEMENT|" + Number((int)MovementKind) + "|" + Number(Count);
        public int CompareTo(NakedMovementKindCount other) => other == null
            ? -1 : ((int)MovementKind).CompareTo((int)other.MovementKind);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class NakedFrontierEvidence : IComparable<NakedFrontierEvidence>
    {
        public NakedFrontierEvidence(string nodeId, int distance, int outgoingStepCount)
        {
            NodeId = Normalize(nodeId);
            Distance = distance;
            OutgoingStepCount = outgoingStepCount;
        }

        public string NodeId { get; }
        public int Distance { get; }
        public int OutgoingStepCount { get; }
        public string StableToken => string.Join("|", new[]
        {
            "NAKED_FRONTIER", NodeId, Number(Distance), Number(OutgoingStepCount),
        });
        public int CompareTo(NakedFrontierEvidence other) => other == null
            ? -1 : string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? "MISSING" : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class NakedReachabilityFailure : IComparable<NakedReachabilityFailure>
    {
        private readonly ReadOnlyCollection<NakedFrontierEvidence> frontier;

        public NakedReachabilityFailure(
            string owner,
            string reason,
            string offendingKey,
            string expected,
            string actual,
            string sourceDigest,
            IEnumerable<NakedFrontierEvidence> sourceFrontier = null)
        {
            Owner = Normalize(owner);
            Reason = Normalize(reason);
            OffendingKey = Normalize(offendingKey);
            Expected = Normalize(expected);
            Actual = Normalize(actual);
            SourceDigest = Normalize(sourceDigest);
            frontier = new ReadOnlyCollection<NakedFrontierEvidence>((sourceFrontier ??
                    Array.Empty<NakedFrontierEvidence>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
        }

        public string Owner { get; }
        public string Reason { get; }
        public string OffendingKey { get; }
        public string Expected { get; }
        public string Actual { get; }
        public string SourceDigest { get; }
        public IReadOnlyList<NakedFrontierEvidence> Frontier => frontier;
        public string StableToken => string.Join("|", new[]
        {
            "NAKED_FAILURE", Owner, Reason, OffendingKey, Expected, Actual, SourceDigest,
            string.Join(",", frontier.Select(value => value.StableToken)),
        });
        public int CompareTo(NakedReachabilityFailure other) => other == null
            ? -1 : string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
        public override string ToString() => StableToken;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? "MISSING" : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
    }

    public sealed class NakedReachabilityProof : IComparable<NakedReachabilityProof>
    {
        private readonly ReadOnlyCollection<NakedMovementKindCount> movementHistogram;

        internal NakedReachabilityProof(
            string startNodeId,
            string targetNodeId,
            int visitedNodeCount,
            int visitedEdgeCount,
            int shortestEdgeCount,
            IEnumerable<NakedMovementKindCount> sourceMovementHistogram,
            string graphDigest)
        {
            StartNodeId = startNodeId;
            TargetNodeId = targetNodeId;
            Found = true;
            VisitedNodeCount = visitedNodeCount;
            VisitedEdgeCount = visitedEdgeCount;
            ShortestEdgeCount = shortestEdgeCount;
            movementHistogram = new ReadOnlyCollection<NakedMovementKindCount>((
                    sourceMovementHistogram ?? Array.Empty<NakedMovementKindCount>())
                .Where(value => value != null).OrderBy(value => value).ToArray());
            GraphDigest = graphDigest;
            ProofDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_03_NAKED_REACHABILITY_PROOF_V1", StartNodeId, TargetNodeId,
                "FOUND=1", Number(VisitedNodeCount), Number(VisitedEdgeCount),
                Number(ShortestEdgeCount), GraphDigest,
            }.Concat(movementHistogram.Select(value => value.StableToken)));
        }

        public string StartNodeId { get; }
        public string TargetNodeId { get; }
        public bool Found { get; }
        public int VisitedNodeCount { get; }
        public int VisitedEdgeCount { get; }
        public int ShortestEdgeCount { get; }
        public IReadOnlyList<NakedMovementKindCount> MovementHistogram => movementHistogram;
        public string GraphDigest { get; }
        public string ProofDigest { get; }
        public string StableToken => "PROOF|" + TargetNodeId + "|" + ProofDigest;
        public int CompareTo(NakedReachabilityProof other) => other == null
            ? -1 : string.Compare(TargetNodeId, other.TargetNodeId, StringComparison.Ordinal);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class NakedTraversalSearchInput
    {
        private readonly ReadOnlyCollection<string> targetNodeIds;
        private readonly ReadOnlyCollection<TraversalMovementKind> allowedMovementKinds;

        public NakedTraversalSearchInput(
            GeneratedTileMovementGraph graph,
            string startNodeId,
            IEnumerable<string> sourceTargetNodeIds,
            IEnumerable<TraversalMovementKind> sourceAllowedMovementKinds,
            int toolMask = 0,
            int optionalMovementSourceCount = 0,
            int externalRuntimeStateReadCount = 0,
            int randomSourceCount = 0)
        {
            Graph = graph;
            StartNodeId = Normalize(startNodeId);
            targetNodeIds = new ReadOnlyCollection<string>((sourceTargetNodeIds ??
                    Array.Empty<string>()).Select(Normalize)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
            allowedMovementKinds = new ReadOnlyCollection<TraversalMovementKind>((
                    sourceAllowedMovementKinds ?? Array.Empty<TraversalMovementKind>())
                .OrderBy(value => (int)value).ToArray());
            ToolMask = toolMask;
            OptionalMovementSourceCount = optionalMovementSourceCount;
            ExternalRuntimeStateReadCount = externalRuntimeStateReadCount;
            RandomSourceCount = randomSourceCount;
        }

        public GeneratedTileMovementGraph Graph { get; }
        public string StartNodeId { get; }
        public IReadOnlyList<string> TargetNodeIds => targetNodeIds;
        public IReadOnlyList<TraversalMovementKind> AllowedMovementKinds => allowedMovementKinds;
        public int ToolMask { get; }
        public int OptionalMovementSourceCount { get; }
        public int ExternalRuntimeStateReadCount { get; }
        public int RandomSourceCount { get; }

        public string ComputeDigest() => BakingCanonicalDigest.HashCanonicalLines(new[]
        {
            "MAP19_03_NAKED_SEARCH_INPUT_V1",
            Graph == null ? "MISSING_GRAPH" : Graph.GraphDigest,
            Graph == null ? "MISSING_HANDOFF" : Graph.Map19_03HandoffDigest,
            StartNodeId,
            "TOOL_MASK=" + Number(ToolMask),
            "OPTIONAL_MOVEMENT_SOURCES=" + Number(OptionalMovementSourceCount),
            "EXTERNAL_RUNTIME_STATE_READS=" + Number(ExternalRuntimeStateReadCount),
            "RANDOM_SOURCES=" + Number(RandomSourceCount),
            "TARGETS=" + string.Join(",", targetNodeIds),
            "MOVEMENTS=" + string.Join(",", allowedMovementKinds.Select(value => Number((int)value))),
        });

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? "MISSING" : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class NakedTraversalSearchSurface
    {
        private readonly ReadOnlyCollection<NakedReachabilityProof> proofs;

        internal NakedTraversalSearchSurface(
            NakedTraversalSearchInput input,
            IEnumerable<NakedReachabilityProof> sourceProofs,
            int visitedNodeCount,
            int visitedEdgeCount)
        {
            Graph = input.Graph;
            StartNodeId = input.StartNodeId;
            proofs = new ReadOnlyCollection<NakedReachabilityProof>((sourceProofs ??
                    Array.Empty<NakedReachabilityProof>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
            ToolMask = input.ToolMask;
            OptionalMovementSourceCount = input.OptionalMovementSourceCount;
            VisitedNodeCount = visitedNodeCount;
            VisitedEdgeCount = visitedEdgeCount;
            InputDigest = input.ComputeDigest();
            ProofDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_03_NAKED_SEARCH_PROOF_SET_V1", InputDigest, Graph.GraphDigest,
                Number(VisitedNodeCount), Number(VisitedEdgeCount),
            }.Concat(proofs.Select(value => value.StableToken)));
        }

        public GeneratedTileMovementGraph Graph { get; }
        public string StartNodeId { get; }
        public IReadOnlyList<NakedReachabilityProof> Proofs => proofs;
        public int ToolMask { get; }
        public int OptionalMovementSourceCount { get; }
        public int VisitedNodeCount { get; }
        public int VisitedEdgeCount { get; }
        public int ReachedTargetCount => proofs.Count;
        public int ShortestProofCount => proofs.Count(value => value.Found);
        public string InputDigest { get; }
        public string ProofDigest { get; }
        public bool ConsumedRuntimePhysics => false;
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class NakedTraversalSearchResult
    {
        private readonly ReadOnlyCollection<NakedReachabilityFailure> failures;

        internal NakedTraversalSearchResult(
            NakedTraversalSearchSurface surface,
            IEnumerable<NakedReachabilityFailure> sourceFailures)
        {
            Surface = surface;
            failures = new ReadOnlyCollection<NakedReachabilityFailure>((sourceFailures ??
                    Array.Empty<NakedReachabilityFailure>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
        }

        public bool Success => Surface != null && failures.Count == 0;
        public NakedTraversalSearchSurface Surface { get; }
        public IReadOnlyList<NakedReachabilityFailure> Failures => failures;
        public string SuccessProofDigest => Success ? Surface.ProofDigest : string.Empty;
    }

    public static class GeneratedNakedTraversalSearch
    {
        public const string ExpectedGraphDigest =
            "bf6b682dd5797e6dd8cb469289ca26fc8db3f10175a99f77d6316c5afe65d063";
        public const string ExpectedIncomingHandoffDigest =
            "cf13a63268b016c737d4b1d7e324dc0595b27da18dfee862dc1dfcd7e448ce43";

        public static NakedTraversalSearchResult Search(NakedTraversalSearchInput input)
        {
            var failures = new List<NakedReachabilityFailure>();
            if (!Validate(input, failures))
                return Failure(failures);

            var allowed = new HashSet<TraversalMovementKind>(input.AllowedMovementKinds);
            var adjacency = BuildAdjacency(input.Graph, allowed);
            var queue = new Queue<string>();
            var distance = new Dictionary<string, int>(StringComparer.Ordinal);
            var predecessor = new Dictionary<string, Step>(StringComparer.Ordinal);
            queue.Enqueue(input.StartNodeId);
            distance[input.StartNodeId] = 0;
            var visitedEdges = 0;
            while (queue.Count != 0)
            {
                var nodeId = queue.Dequeue();
                if (!adjacency.TryGetValue(nodeId, out var steps))
                    continue;
                foreach (var step in steps)
                {
                    visitedEdges++;
                    if (distance.ContainsKey(step.ToNodeId))
                        continue;
                    distance[step.ToNodeId] = distance[nodeId] + 1;
                    predecessor[step.ToNodeId] = step;
                    queue.Enqueue(step.ToNodeId);
                }
            }

            var frontier = distance.OrderByDescending(value => value.Value)
                .ThenBy(value => value.Key, StringComparer.Ordinal).Take(8)
                .Select(value => new NakedFrontierEvidence(value.Key, value.Value,
                    adjacency.TryGetValue(value.Key, out var steps) ? steps.Count : 0)).ToArray();
            foreach (var target in input.TargetNodeIds.Where(target => !distance.ContainsKey(target)))
                failures.Add(new NakedReachabilityFailure("NakedReachability",
                    "UNREACHABLE_MANDATORY_TARGET", target, "REACHABLE_WITH_TOOL_MASK_0",
                    "UNREACHABLE", input.Graph.GraphDigest, frontier));
            if (failures.Count != 0)
                return Failure(failures);

            var proofs = input.TargetNodeIds.Select(target => Proof(input.StartNodeId, target,
                input.Graph.GraphDigest, distance, predecessor, visitedEdges)).ToArray();
            return new NakedTraversalSearchResult(new NakedTraversalSearchSurface(
                input, proofs, distance.Count, visitedEdges), failures);
        }

        private static bool Validate(
            NakedTraversalSearchInput input,
            ICollection<NakedReachabilityFailure> failures)
        {
            if (input == null)
            {
                Add(failures, "NakedSearch", "MISSING_SEARCH_INPUT", "input",
                    "NON_NULL", "NULL", "MISSING");
                return false;
            }
            if (input.Graph == null)
            {
                Add(failures, "MAP19_02", "MISSING_GRAPH_INPUT", "graph",
                    "NON_NULL", "NULL", "MISSING");
                return false;
            }
            var graph = input.Graph;
            Digest(failures, "MAP19_02", "GRAPH_DIGEST_MISMATCH", graph.GraphDigest,
                ExpectedGraphDigest, graph.GraphDigest);
            Digest(failures, "MAP19_02", "GRAPH_CANONICAL_DIGEST_MISMATCH", graph.GraphDigest,
                graph.ComputeGraphDigest(), graph.GraphDigest);
            Digest(failures, "MAP19_02", "INCOMING_HANDOFF_DIGEST_MISMATCH",
                graph.Map19_03HandoffDigest, ExpectedIncomingHandoffDigest, graph.GraphDigest);
            if (graph.Nodes.Count != 1614 || graph.Edges.Count != 5400 || graph.SocketLinks.Count != 24)
                Add(failures, "MAP19_02", "GRAPH_EVIDENCE_COUNT_MISMATCH", "graph.counts",
                    "1614/5400/24", graph.Nodes.Count + "/" + graph.Edges.Count + "/" +
                    graph.SocketLinks.Count, graph.GraphDigest);
            Duplicate(failures, graph.Nodes.Select(value => value.NodeId), "DUPLICATE_NODE_ID",
                graph.GraphDigest);
            Duplicate(failures, graph.Edges.Select(value => value.EdgeId), "DUPLICATE_EDGE_ID",
                graph.GraphDigest);
            var nodeIds = new HashSet<string>(graph.Nodes.Select(value => value.NodeId),
                StringComparer.Ordinal);
            if (!nodeIds.Contains(input.StartNodeId))
                Add(failures, "NakedSearch", "MISSING_START_NODE", input.StartNodeId,
                    "EXISTING_GRAPH_NODE", "MISSING", graph.GraphDigest);
            foreach (var target in input.TargetNodeIds.Where(target => !nodeIds.Contains(target)))
                Add(failures, "NakedSearch", "MISSING_TARGET_NODE", target,
                    "EXISTING_GRAPH_NODE", "MISSING", graph.GraphDigest);
            if (input.TargetNodeIds.Count == 0)
                Add(failures, "NakedSearch", "MISSING_TARGET_SET", "targets",
                    "AT_LEAST_ONE", "0", graph.GraphDigest);
            Duplicate(failures, input.TargetNodeIds, "DUPLICATE_TARGET_NODE", graph.GraphDigest);
            foreach (var movement in input.AllowedMovementKinds.Where(value =>
                         !Enum.IsDefined(typeof(TraversalMovementKind), value)))
                Add(failures, "MovementKindSupport", "UNSUPPORTED_MOVEMENT_KIND",
                    Number((int)movement), "DEFINED_MAP19_01_MOVEMENT", Number((int)movement),
                    graph.GraphDigest);
            Duplicate(failures, input.AllowedMovementKinds.Select(value => Number((int)value)),
                "DUPLICATE_ALLOWED_MOVEMENT", graph.GraphDigest);
            if (input.AllowedMovementKinds.Count == 0)
                Add(failures, "MovementKindSupport", "MISSING_ALLOWED_MOVEMENTS", "movements",
                    "MAP19_01_MOVEMENT_SET", "0", graph.GraphDigest);
            if (input.ToolMask != 0 || input.OptionalMovementSourceCount != 0 ||
                input.ExternalRuntimeStateReadCount != 0 || input.RandomSourceCount != 0)
                Add(failures, "NakedBoundary", "NON_NAKED_SEARCH_SOURCE_ATTEMPT", "boundary",
                    "0/0/0/0", input.ToolMask + "/" + input.OptionalMovementSourceCount + "/" +
                    input.ExternalRuntimeStateReadCount + "/" + input.RandomSourceCount,
                    graph.GraphDigest);
            foreach (var edge in graph.Edges.Where(value =>
                         !nodeIds.Contains(value.FromNodeId) || !nodeIds.Contains(value.ToNodeId)))
                Add(failures, "MAP19_02", "DANGLING_GRAPH_EDGE", edge.EdgeId,
                    "EXISTING_FROM_AND_TO", edge.FromNodeId + "->" + edge.ToNodeId,
                    graph.GraphDigest);
            foreach (var link in graph.SocketLinks.Where(value =>
                         !nodeIds.Contains(value.FromSocketNodeId) ||
                         !nodeIds.Contains(value.ToSocketNodeId)))
                Add(failures, "MAP19_02", "DANGLING_SOCKET_LINK", link.LinkId,
                    "EXISTING_FROM_AND_TO", link.FromSocketNodeId + "->" + link.ToSocketNodeId,
                    graph.GraphDigest);
            return failures.Count == 0;
        }

        private static Dictionary<string, List<Step>> BuildAdjacency(
            GeneratedTileMovementGraph graph,
            ISet<TraversalMovementKind> allowed)
        {
            var result = graph.Nodes.ToDictionary(value => value.NodeId,
                _ => new List<Step>(), StringComparer.Ordinal);
            foreach (var edge in graph.Edges.Where(value => allowed.Contains(value.MovementKind)))
                result[edge.FromNodeId].Add(new Step(edge.FromNodeId, edge.ToNodeId,
                    edge.EdgeId, edge.MovementKind));
            foreach (var link in graph.SocketLinks)
            {
                result[link.FromSocketNodeId].Add(new Step(link.FromSocketNodeId,
                    link.ToSocketNodeId, link.LinkId + ":F", null));
                result[link.ToSocketNodeId].Add(new Step(link.ToSocketNodeId,
                    link.FromSocketNodeId, link.LinkId + ":R", null));
            }
            foreach (var steps in result.Values)
                steps.Sort();
            return result;
        }

        private static NakedReachabilityProof Proof(
            string start,
            string target,
            string graphDigest,
            IReadOnlyDictionary<string, int> distance,
            IReadOnlyDictionary<string, Step> predecessor,
            int visitedEdges)
        {
            var histogram = new Dictionary<TraversalMovementKind, int>();
            var cursor = target;
            while (!string.Equals(cursor, start, StringComparison.Ordinal))
            {
                var step = predecessor[cursor];
                if (step.MovementKind.HasValue)
                {
                    histogram.TryGetValue(step.MovementKind.Value, out var count);
                    histogram[step.MovementKind.Value] = count + 1;
                }
                cursor = step.FromNodeId;
            }
            return new NakedReachabilityProof(start, target, distance.Count, visitedEdges,
                distance[target], histogram.Select(value => new NakedMovementKindCount(
                    value.Key, value.Value)), graphDigest);
        }

        private static NakedTraversalSearchResult Failure(
            IEnumerable<NakedReachabilityFailure> failures) =>
            new NakedTraversalSearchResult(null, failures);
        private static void Duplicate(
            ICollection<NakedReachabilityFailure> failures,
            IEnumerable<string> values,
            string reason,
            string sourceDigest)
        {
            foreach (var duplicate in values.GroupBy(value => value, StringComparer.Ordinal)
                         .Where(value => value.Count() > 1))
                Add(failures, "NakedSearch", reason, duplicate.Key, "1",
                    Number(duplicate.Count()), sourceDigest);
        }
        private static void Digest(
            ICollection<NakedReachabilityFailure> failures,
            string owner,
            string reason,
            string actual,
            string expected,
            string sourceDigest)
        {
            if (!BakingCanonicalDigest.IsLowerHexSha256(actual) ||
                !string.Equals(actual, expected, StringComparison.Ordinal))
                Add(failures, owner, reason, reason.ToLowerInvariant(), expected,
                    string.IsNullOrEmpty(actual) ? "MISSING" : actual, sourceDigest);
        }
        private static void Add(
            ICollection<NakedReachabilityFailure> failures,
            string owner,
            string reason,
            string key,
            string expected,
            string actual,
            string sourceDigest) => failures.Add(new NakedReachabilityFailure(
            owner, reason, key, expected, actual, sourceDigest));
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        internal sealed class Step : IComparable<Step>
        {
            public Step(string fromNodeId, string toNodeId, string sourceId,
                TraversalMovementKind? movementKind)
            {
                FromNodeId = fromNodeId;
                ToNodeId = toNodeId;
                SourceId = sourceId;
                MovementKind = movementKind;
            }
            public string FromNodeId { get; }
            public string ToNodeId { get; }
            public string SourceId { get; }
            public TraversalMovementKind? MovementKind { get; }
            public int CompareTo(Step other)
            {
                if (other == null) return -1;
                var to = string.Compare(ToNodeId, other.ToNodeId, StringComparison.Ordinal);
                return to != 0 ? to : string.Compare(SourceId, other.SourceId,
                    StringComparison.Ordinal);
            }
        }
    }
}
