using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.WorldGeneration.Validation
{
    public enum GeneratedTileMovementNodeKind
    {
        Stand = 1,
        Climb = 2,
        Bounce = 3,
        IntersectorSocket = 4,
    }

    public enum GeneratedIntersectorSocketDirection
    {
        Left = 1,
        Right = 2,
        Down = 3,
        Up = 4,
    }

    public sealed class GeneratedTileMovementNode : IComparable<GeneratedTileMovementNode>
    {
        public GeneratedTileMovementNode(
            GeneratedTileMovementNodeKind kind,
            SectorCanvasId sector,
            GeneratedSliceCoord slice,
            LocalTileCoord cell,
            string surfaceId,
            string sourceOwner,
            string sourceDigest,
            string clearanceProtectionProof,
            string declaredNodeId = null)
        {
            Kind = kind;
            Sector = sector;
            Slice = slice;
            Cell = cell;
            SurfaceId = Normalize(surfaceId);
            SourceOwner = Normalize(sourceOwner);
            SourceDigest = Normalize(sourceDigest);
            ClearanceProtectionProof = Normalize(clearanceProtectionProof);
            NodeId = declaredNodeId ?? BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_02_TILE_MOVEMENT_NODE_ID_V1",
                Number((int)Kind), Sector.Value, Number(Slice.X), Number(Slice.Y),
                Number(Cell.X), Number(Cell.Y), SurfaceId, SourceOwner, SourceDigest,
                ClearanceProtectionProof,
            });
        }

        public string NodeId { get; }
        public GeneratedTileMovementNodeKind Kind { get; }
        public SectorCanvasId Sector { get; }
        public GeneratedSliceCoord Slice { get; }
        public LocalTileCoord Cell { get; }
        public string SurfaceId { get; }
        public string SourceOwner { get; }
        public string SourceDigest { get; }
        public string ClearanceProtectionProof { get; }
        public string StableToken => string.Join("|", new[]
        {
            "NODE", NodeId, Number((int)Kind), Sector.Value, Number(Slice.X), Number(Slice.Y),
            Number(Cell.X), Number(Cell.Y), SurfaceId, SourceOwner, SourceDigest,
            ClearanceProtectionProof,
        });

        public int CompareTo(GeneratedTileMovementNode other) => other == null
            ? -1
            : string.Compare(NodeId, other.NodeId, StringComparison.Ordinal);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? "MISSING"
            : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedTileMovementEdge : IComparable<GeneratedTileMovementEdge>
    {
        private readonly ReadOnlyCollection<CompiledTraversalEnvelopeSetKind> requiredEnvelopeKinds;
        private readonly ReadOnlyCollection<CompiledTraversalEnvelopeSetKind> provenEnvelopeKinds;
        private readonly ReadOnlyCollection<string> ruleIds;

        public GeneratedTileMovementEdge(
            string fromNodeId,
            string toNodeId,
            TraversalMovementKind movementKind,
            IEnumerable<CompiledTraversalEnvelopeSetKind> sourceRequiredEnvelopeKinds,
            IEnumerable<CompiledTraversalEnvelopeSetKind> sourceProvenEnvelopeKinds,
            IEnumerable<string> sourceRuleIds,
            decimal cost,
            GeneratedTraversalRuleFailurePolicy failurePolicy,
            string declaredEdgeId = null)
        {
            FromNodeId = Normalize(fromNodeId);
            ToNodeId = Normalize(toNodeId);
            MovementKind = movementKind;
            requiredEnvelopeKinds = ReadOnlyEnums(sourceRequiredEnvelopeKinds);
            provenEnvelopeKinds = ReadOnlyEnums(sourceProvenEnvelopeKinds);
            ruleIds = new ReadOnlyCollection<string>((sourceRuleIds ?? Array.Empty<string>())
                .Select(Normalize).OrderBy(value => value, StringComparer.Ordinal).ToArray());
            Cost = cost;
            FailurePolicy = failurePolicy;
            EdgeId = declaredEdgeId ?? BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_02_TILE_MOVEMENT_EDGE_ID_V1", FromNodeId, ToNodeId,
                Number((int)MovementKind), EnvelopeToken(requiredEnvelopeKinds),
                EnvelopeToken(provenEnvelopeKinds), string.Join(",", ruleIds), Number(Cost),
                Number((int)FailurePolicy),
            });
        }

        public string EdgeId { get; }
        public string FromNodeId { get; }
        public string ToNodeId { get; }
        public TraversalMovementKind MovementKind { get; }
        public IReadOnlyList<CompiledTraversalEnvelopeSetKind> RequiredEnvelopeKinds =>
            requiredEnvelopeKinds;
        public IReadOnlyList<CompiledTraversalEnvelopeSetKind> ProvenEnvelopeKinds =>
            provenEnvelopeKinds;
        public IReadOnlyList<string> RuleIds => ruleIds;
        public decimal Cost { get; }
        public GeneratedTraversalRuleFailurePolicy FailurePolicy { get; }
        public string StableToken => string.Join("|", new[]
        {
            "EDGE", EdgeId, FromNodeId, ToNodeId, Number((int)MovementKind),
            EnvelopeToken(requiredEnvelopeKinds), EnvelopeToken(provenEnvelopeKinds),
            string.Join(",", ruleIds), Number(Cost), Number((int)FailurePolicy),
        });

        public int CompareTo(GeneratedTileMovementEdge other) => other == null
            ? -1
            : string.Compare(EdgeId, other.EdgeId, StringComparison.Ordinal);

        private static ReadOnlyCollection<CompiledTraversalEnvelopeSetKind> ReadOnlyEnums(
            IEnumerable<CompiledTraversalEnvelopeSetKind> values) =>
            new ReadOnlyCollection<CompiledTraversalEnvelopeSetKind>((values ??
                    Array.Empty<CompiledTraversalEnvelopeSetKind>())
                .OrderBy(value => (int)value).ToArray());
        private static string EnvelopeToken(IEnumerable<CompiledTraversalEnvelopeSetKind> values) =>
            string.Join(",", values.Select(value => Number((int)value)));
        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? "MISSING"
            : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Number(decimal value) =>
            value.ToString("0.############################", CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedIntersectorSocketLink : IComparable<GeneratedIntersectorSocketLink>
    {
        public GeneratedIntersectorSocketLink(
            string fromSocketNodeId,
            string toSocketNodeId,
            GeneratedIntersectorSocketDirection fromDirection,
            GeneratedIntersectorSocketDirection toDirection,
            string provenanceDigest,
            string declaredLinkId = null)
        {
            FromSocketNodeId = Normalize(fromSocketNodeId);
            ToSocketNodeId = Normalize(toSocketNodeId);
            FromDirection = fromDirection;
            ToDirection = toDirection;
            ProvenanceDigest = Normalize(provenanceDigest);
            LinkId = declaredLinkId ?? BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_02_INTERSECTOR_SOCKET_LINK_ID_V1", FromSocketNodeId, ToSocketNodeId,
                Number((int)FromDirection), Number((int)ToDirection), ProvenanceDigest,
            });
        }

        public string LinkId { get; }
        public string FromSocketNodeId { get; }
        public string ToSocketNodeId { get; }
        public GeneratedIntersectorSocketDirection FromDirection { get; }
        public GeneratedIntersectorSocketDirection ToDirection { get; }
        public string ProvenanceDigest { get; }
        public string StableToken => string.Join("|", new[]
        {
            "SOCKET_LINK", LinkId, FromSocketNodeId, ToSocketNodeId,
            Number((int)FromDirection), Number((int)ToDirection), ProvenanceDigest,
        });

        public int CompareTo(GeneratedIntersectorSocketLink other) => other == null
            ? -1
            : string.Compare(LinkId, other.LinkId, StringComparison.Ordinal);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? "MISSING"
            : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedTileMovementGraphStats
    {
        public GeneratedTileMovementGraphStats(
            int sourceSectors,
            int sourceSlices,
            int sourceCells,
            int solidBlockedNodeRejections,
            int clearanceMissingEdgeRejections,
            int protectedEnvelopeEdgeRejections,
            int ruleRegistryThresholdLookups,
            int hardcodedDuplicateTraversalThresholds = 0,
            int matrixLocalCopyMismatches = 0,
            int unsupportedMovementKinds = 0,
            int unsupportedEnvelopeKinds = 0)
        {
            SourceSectors = sourceSectors;
            SourceSlices = sourceSlices;
            SourceCells = sourceCells;
            SolidBlockedNodeRejections = solidBlockedNodeRejections;
            ClearanceMissingEdgeRejections = clearanceMissingEdgeRejections;
            ProtectedEnvelopeEdgeRejections = protectedEnvelopeEdgeRejections;
            RuleRegistryThresholdLookups = ruleRegistryThresholdLookups;
            HardcodedDuplicateTraversalThresholds = hardcodedDuplicateTraversalThresholds;
            MatrixLocalCopyMismatches = matrixLocalCopyMismatches;
            UnsupportedMovementKinds = unsupportedMovementKinds;
            UnsupportedEnvelopeKinds = unsupportedEnvelopeKinds;
        }

        public int SourceSectors { get; }
        public int SourceSlices { get; }
        public int SourceCells { get; }
        public int SolidBlockedNodeRejections { get; }
        public int ClearanceMissingEdgeRejections { get; }
        public int ProtectedEnvelopeEdgeRejections { get; }
        public int RuleRegistryThresholdLookups { get; }
        public int HardcodedDuplicateTraversalThresholds { get; }
        public int MatrixLocalCopyMismatches { get; }
        public int UnsupportedMovementKinds { get; }
        public int UnsupportedEnvelopeKinds { get; }
    }

    public sealed class GeneratedTileMovementBoundaryAudit
    {
        public GeneratedTileMovementBoundaryAudit(
            int bfsRuns = 0,
            int completionSearches = 0,
            int nakedTraversalProofs = 0,
            int seedBatchRuns = 0,
            int physics2DQueries = 0,
            int playerTuningChanges = 0,
            int runtimeObjectSpawns = 0,
            int scenePrefabTilemapMutations = 0,
            bool map19_03Started = false)
        {
            BfsRuns = bfsRuns;
            CompletionSearches = completionSearches;
            NakedTraversalProofs = nakedTraversalProofs;
            SeedBatchRuns = seedBatchRuns;
            Physics2DQueries = physics2DQueries;
            PlayerTuningChanges = playerTuningChanges;
            RuntimeObjectSpawns = runtimeObjectSpawns;
            ScenePrefabTilemapMutations = scenePrefabTilemapMutations;
            Map19_03Started = map19_03Started;
        }

        public int BfsRuns { get; }
        public int CompletionSearches { get; }
        public int NakedTraversalProofs { get; }
        public int SeedBatchRuns { get; }
        public int Physics2DQueries { get; }
        public int PlayerTuningChanges { get; }
        public int RuntimeObjectSpawns { get; }
        public int ScenePrefabTilemapMutations { get; }
        public bool Map19_03Started { get; }
        public bool IsZero => BfsRuns == 0 && CompletionSearches == 0 && NakedTraversalProofs == 0 &&
            SeedBatchRuns == 0 && Physics2DQueries == 0 && PlayerTuningChanges == 0 &&
            RuntimeObjectSpawns == 0 && ScenePrefabTilemapMutations == 0 && !Map19_03Started;
    }

    public sealed class GeneratedTileMovementGraph
    {
        private readonly ReadOnlyCollection<GeneratedTileMovementNode> nodes;
        private readonly ReadOnlyCollection<GeneratedTileMovementEdge> edges;
        private readonly ReadOnlyCollection<GeneratedIntersectorSocketLink> socketLinks;

        public GeneratedTileMovementGraph(
            IEnumerable<GeneratedTileMovementNode> sourceNodes,
            IEnumerable<GeneratedTileMovementEdge> sourceEdges,
            IEnumerable<GeneratedIntersectorSocketLink> sourceSocketLinks,
            GeneratedTileMovementGraphStats stats,
            string traversalProfileDigest,
            string ruleRegistryDigest,
            string movementEnvelopeMatrixDigest,
            string map19_02IncomingHandoffDigest,
            string sourceTerrainDigest,
            string declaredNodeSetDigest = null,
            string declaredEdgeSetDigest = null,
            string declaredSocketLinkDigest = null,
            string declaredGraphDigest = null,
            string declaredMap19_03HandoffDigest = null)
        {
            nodes = ReadOnly(sourceNodes);
            edges = ReadOnly(sourceEdges);
            socketLinks = ReadOnly(sourceSocketLinks);
            Stats = stats;
            TraversalProfileDigest = Normalize(traversalProfileDigest);
            RuleRegistryDigest = Normalize(ruleRegistryDigest);
            MovementEnvelopeMatrixDigest = Normalize(movementEnvelopeMatrixDigest);
            Map19_02IncomingHandoffDigest = Normalize(map19_02IncomingHandoffDigest);
            SourceTerrainDigest = Normalize(sourceTerrainDigest);
            NodeSetDigest = declaredNodeSetDigest ?? ComputeNodeSetDigest();
            EdgeSetDigest = declaredEdgeSetDigest ?? ComputeEdgeSetDigest();
            SocketLinkDigest = declaredSocketLinkDigest ?? ComputeSocketLinkDigest();
            GraphDigest = declaredGraphDigest ?? ComputeGraphDigest();
            Map19_03HandoffDigest = declaredMap19_03HandoffDigest ?? ComputeMap19_03HandoffDigest();
        }

        public IReadOnlyList<GeneratedTileMovementNode> Nodes => nodes;
        public IReadOnlyList<GeneratedTileMovementEdge> Edges => edges;
        public IReadOnlyList<GeneratedIntersectorSocketLink> SocketLinks => socketLinks;
        public GeneratedTileMovementGraphStats Stats { get; }
        public string TraversalProfileDigest { get; }
        public string RuleRegistryDigest { get; }
        public string MovementEnvelopeMatrixDigest { get; }
        public string Map19_02IncomingHandoffDigest { get; }
        public string SourceTerrainDigest { get; }
        public string NodeSetDigest { get; }
        public string EdgeSetDigest { get; }
        public string SocketLinkDigest { get; }
        public string GraphDigest { get; }
        public string Map19_03HandoffDigest { get; }
        public bool Map19_03Started => false;

        public string ComputeNodeSetDigest() => BakingCanonicalDigest.HashCanonicalLines(
            new[] { "MAP19_02_TILE_MOVEMENT_NODE_SET_V1" }.Concat(nodes.Select(value => value.StableToken)));
        public string ComputeEdgeSetDigest() => BakingCanonicalDigest.HashCanonicalLines(
            new[] { "MAP19_02_TILE_MOVEMENT_EDGE_SET_V1" }.Concat(edges.Select(value => value.StableToken)));
        public string ComputeSocketLinkDigest() => BakingCanonicalDigest.HashCanonicalLines(
            new[] { "MAP19_02_INTERSECTOR_SOCKET_LINK_SET_V1" }
                .Concat(socketLinks.Select(value => value.StableToken)));
        public string ComputeGraphDigest() => BakingCanonicalDigest.HashCanonicalLines(new[]
        {
            "MAP19_02_TILE_MOVEMENT_GRAPH_V1", TraversalProfileDigest, RuleRegistryDigest,
            MovementEnvelopeMatrixDigest, Map19_02IncomingHandoffDigest, SourceTerrainDigest,
            NodeSetDigest, EdgeSetDigest, SocketLinkDigest,
        });
        public string ComputeMap19_03HandoffDigest() => BakingCanonicalDigest.HashCanonicalLines(new[]
        {
            "MAP19_03_TILE_MOVEMENT_GRAPH_HANDOFF_V1", GraphDigest, NodeSetDigest,
            EdgeSetDigest, SocketLinkDigest, "MAP19_03_LOCKED=1|STARTED=0",
        });

        private static ReadOnlyCollection<T> ReadOnly<T>(IEnumerable<T> source) where T : IComparable<T>
        {
            var copy = (source ?? Array.Empty<T>()).Where(value => value != null).ToArray();
            Array.Sort(copy);
            return new ReadOnlyCollection<T>(copy);
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? "MISSING"
            : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
    }

    public sealed class GeneratedTileMovementGraphRequest
    {
        public GeneratedTileMovementGraphRequest(
            SectorCanvasContract canvas,
            GeneratedSliceSet slices,
            GeneratedTraversalProfileRuleLockResult profileRuleLock,
            GeneratedTileMovementBoundaryAudit boundaryAudit = null)
        {
            Canvas = canvas;
            Slices = slices;
            ProfileRuleLock = profileRuleLock;
            BoundaryAudit = boundaryAudit ?? new GeneratedTileMovementBoundaryAudit();
        }

        public SectorCanvasContract Canvas { get; }
        public GeneratedSliceSet Slices { get; }
        public GeneratedTraversalProfileRuleLockResult ProfileRuleLock { get; }
        public GeneratedTileMovementBoundaryAudit BoundaryAudit { get; }
    }

    public sealed class GeneratedTileMovementGraphFailure : IComparable<GeneratedTileMovementGraphFailure>
    {
        public GeneratedTileMovementGraphFailure(
            string owner, string reason, string offendingKey, string expectedValue, string actualValue)
        {
            Owner = Normalize(owner);
            Reason = Normalize(reason);
            OffendingKey = Normalize(offendingKey);
            ExpectedValue = Normalize(expectedValue);
            ActualValue = Normalize(actualValue);
        }

        public string Owner { get; }
        public string Reason { get; }
        public string OffendingKey { get; }
        public string ExpectedValue { get; }
        public string ActualValue { get; }
        public string StableToken => string.Join("|", new[]
            { "FAILURE", Owner, Reason, OffendingKey, ExpectedValue, ActualValue });
        public int CompareTo(GeneratedTileMovementGraphFailure other) => other == null
            ? -1
            : string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
        public override string ToString() => StableToken;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? "MISSING"
            : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
    }

    public sealed class GeneratedTileMovementGraphResult
    {
        private readonly ReadOnlyCollection<GeneratedTileMovementGraphFailure> failures;

        internal GeneratedTileMovementGraphResult(
            GeneratedTileMovementGraph graph,
            IEnumerable<GeneratedTileMovementGraphFailure> sourceFailures)
        {
            Graph = graph;
            var copy = (sourceFailures ?? Array.Empty<GeneratedTileMovementGraphFailure>()).ToArray();
            Array.Sort(copy);
            failures = new ReadOnlyCollection<GeneratedTileMovementGraphFailure>(copy);
        }

        public bool Success => Graph != null && failures.Count == 0;
        public GeneratedTileMovementGraph Graph { get; }
        public IReadOnlyList<GeneratedTileMovementGraphFailure> Failures => failures;
        public string Map19_03HandoffDigest => Success ? Graph.Map19_03HandoffDigest : string.Empty;
    }
}
