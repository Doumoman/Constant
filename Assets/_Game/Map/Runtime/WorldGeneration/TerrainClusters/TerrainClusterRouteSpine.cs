using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.TerrainClusters
{
    public sealed class CompiledTraversalNode
    {
        private readonly ReadOnlyCollection<string> linkedRoleAnchorIds;
        private readonly ReadOnlyCollection<ClusterRoleKind> linkedRoleKinds;

        internal CompiledTraversalNode(
            SpineVariantId variantId,
            string nodeId,
            LocalTileCoord sourceCoordinate,
            LocalTileCoord compiledCoordinate,
            ClusterChunkCoord owningCompiledChunk,
            bool isMandatory,
            TraversalGraphKind sourceGraphKind,
            string sourceRoleAnchorId,
            IEnumerable<ProjectedRoleSpineLink> roleLinks)
        {
            VariantId = variantId;
            NodeId = nodeId ?? string.Empty;
            SourceCoordinate = sourceCoordinate;
            CompiledCoordinate = compiledCoordinate;
            OwningCompiledChunk = owningCompiledChunk;
            IsMandatory = isMandatory;
            SourceGraphKind = sourceGraphKind;
            SourceRoleAnchorId = sourceRoleAnchorId ?? string.Empty;

            var links = (roleLinks ?? Array.Empty<ProjectedRoleSpineLink>())
                .OrderBy(value => value.RoleAnchorId, StringComparer.Ordinal)
                .ToArray();
            linkedRoleAnchorIds = new ReadOnlyCollection<string>(
                links.Select(value => value.RoleAnchorId).ToArray());
            linkedRoleKinds = new ReadOnlyCollection<ClusterRoleKind>(
                links.Select(value => value.RoleKind).ToArray());
        }

        public SpineVariantId VariantId { get; }
        public string NodeId { get; }
        public LocalTileCoord SourceCoordinate { get; }
        public LocalTileCoord CompiledCoordinate { get; }
        public ClusterChunkCoord OwningCompiledChunk { get; }
        public bool IsMandatory { get; }
        public TraversalGraphKind SourceGraphKind { get; }
        public string SourceRoleAnchorId { get; }
        public IReadOnlyList<string> LinkedRoleAnchorIds => linkedRoleAnchorIds;
        public IReadOnlyList<ClusterRoleKind> LinkedRoleKinds => linkedRoleKinds;
    }

    public sealed class CompiledTraversalEdge
    {
        internal CompiledTraversalEdge(
            SpineVariantId variantId,
            string edgeId,
            string fromNodeId,
            string toNodeId,
            TraversalMovementKind movementKind,
            LocalTileCoord sourceStartCoordinate,
            LocalTileCoord compiledStartCoordinate,
            LocalTileCoord sourceEndCoordinate,
            LocalTileCoord compiledEndCoordinate,
            int minimumClearanceWidth,
            int minimumClearanceHeight,
            LocalTileCoord sourceLandingCoordinate,
            LocalTileCoord compiledLandingCoordinate,
            LocalTileCoord sourceRecoveryCoordinate,
            LocalTileCoord compiledRecoveryCoordinate,
            bool isMandatory,
            TraversalGraphKind sourceGraphKind,
            CompiledTraversalEnvelope envelope)
        {
            VariantId = variantId;
            EdgeId = edgeId ?? string.Empty;
            FromNodeId = fromNodeId ?? string.Empty;
            ToNodeId = toNodeId ?? string.Empty;
            MovementKind = movementKind;
            SourceStartCoordinate = sourceStartCoordinate;
            CompiledStartCoordinate = compiledStartCoordinate;
            SourceEndCoordinate = sourceEndCoordinate;
            CompiledEndCoordinate = compiledEndCoordinate;
            MinimumClearanceWidth = minimumClearanceWidth;
            MinimumClearanceHeight = minimumClearanceHeight;
            SourceLandingCoordinate = sourceLandingCoordinate;
            CompiledLandingCoordinate = compiledLandingCoordinate;
            SourceRecoveryCoordinate = sourceRecoveryCoordinate;
            CompiledRecoveryCoordinate = compiledRecoveryCoordinate;
            IsMandatory = isMandatory;
            SourceGraphKind = sourceGraphKind;
            Envelope = envelope;
        }

        public SpineVariantId VariantId { get; }
        public string EdgeId { get; }
        public string FromNodeId { get; }
        public string ToNodeId { get; }
        public TraversalMovementKind MovementKind { get; }
        public LocalTileCoord SourceStartCoordinate { get; }
        public LocalTileCoord CompiledStartCoordinate { get; }
        public LocalTileCoord SourceEndCoordinate { get; }
        public LocalTileCoord CompiledEndCoordinate { get; }
        public int MinimumClearanceWidth { get; }
        public int MinimumClearanceHeight { get; }
        public LocalTileCoord SourceLandingCoordinate { get; }
        public LocalTileCoord CompiledLandingCoordinate { get; }
        public LocalTileCoord SourceRecoveryCoordinate { get; }
        public LocalTileCoord CompiledRecoveryCoordinate { get; }
        public bool IsMandatory { get; }
        public TraversalGraphKind SourceGraphKind { get; }
        public CompiledTraversalEnvelope Envelope { get; }
    }

    public sealed class CompiledClusterSpineVariant
    {
        private readonly ReadOnlyCollection<CompiledTraversalNode> nodes;
        private readonly ReadOnlyCollection<CompiledTraversalEdge> edges;
        private readonly ReadOnlyCollection<ClusterTraversalProtectedTile> protectedTiles;
        private readonly ReadOnlyDictionary<string, CompiledTraversalNode> nodesById;
        private readonly ReadOnlyDictionary<string, CompiledTraversalEdge> edgesById;

        internal CompiledClusterSpineVariant(
            SpineVariantId variantId,
            bool isBaseline,
            TraversalGraphKind sourceGraphKind,
            IEnumerable<CompiledTraversalNode> nodes,
            IEnumerable<CompiledTraversalEdge> edges,
            IEnumerable<ClusterTraversalProtectedTile> protectedTiles)
        {
            VariantId = variantId;
            IsBaseline = isBaseline;
            SourceGraphKind = sourceGraphKind;

            var nodeCopy = (nodes ?? Array.Empty<CompiledTraversalNode>())
                .OrderBy(value => value.NodeId, StringComparer.Ordinal)
                .ToArray();
            this.nodes = new ReadOnlyCollection<CompiledTraversalNode>(nodeCopy);
            var edgeCopy = (edges ?? Array.Empty<CompiledTraversalEdge>())
                .OrderBy(value => value.EdgeId, StringComparer.Ordinal)
                .ToArray();
            this.edges = new ReadOnlyCollection<CompiledTraversalEdge>(edgeCopy);
            var protectedCopy = (protectedTiles ?? Array.Empty<ClusterTraversalProtectedTile>())
                .OrderBy(value => value.CompiledCoordinate.Y)
                .ThenBy(value => value.CompiledCoordinate.X)
                .ToArray();
            this.protectedTiles = new ReadOnlyCollection<ClusterTraversalProtectedTile>(protectedCopy);
            nodesById = new ReadOnlyDictionary<string, CompiledTraversalNode>(
                nodeCopy.ToDictionary(value => value.NodeId, StringComparer.Ordinal));
            edgesById = new ReadOnlyDictionary<string, CompiledTraversalEdge>(
                edgeCopy.ToDictionary(value => value.EdgeId, StringComparer.Ordinal));
        }

        public SpineVariantId VariantId { get; }
        public bool IsBaseline { get; }
        public TraversalGraphKind SourceGraphKind { get; }
        public IReadOnlyList<CompiledTraversalNode> Nodes => nodes;
        public IReadOnlyList<CompiledTraversalEdge> Edges => edges;
        public IReadOnlyList<ClusterTraversalProtectedTile> ProtectedTiles => protectedTiles;

        public bool TryGetNode(string nodeId, out CompiledTraversalNode node)
        {
            return nodesById.TryGetValue(nodeId ?? string.Empty, out node);
        }

        public bool TryGetEdge(string edgeId, out CompiledTraversalEdge edge)
        {
            return edgesById.TryGetValue(edgeId ?? string.Empty, out edge);
        }
    }
}
