using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.TerrainClusters
{
    public readonly struct TerrainClusterId : IEquatable<TerrainClusterId>, IComparable<TerrainClusterId>
    {
        private readonly string value;

        public TerrainClusterId(string value)
        {
            this.value = value;
        }

        public string Value => value ?? string.Empty;

        public int CompareTo(TerrainClusterId other)
        {
            return string.Compare(Value, other.Value, StringComparison.Ordinal);
        }

        public bool Equals(TerrainClusterId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is TerrainClusterId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(TerrainClusterId left, TerrainClusterId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TerrainClusterId left, TerrainClusterId right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct SpineVariantId : IEquatable<SpineVariantId>, IComparable<SpineVariantId>
    {
        private readonly string value;

        public SpineVariantId(string value)
        {
            this.value = value;
        }

        public string Value => value ?? string.Empty;

        public int CompareTo(SpineVariantId other)
        {
            return string.Compare(Value, other.Value, StringComparison.Ordinal);
        }

        public bool Equals(SpineVariantId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SpineVariantId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(SpineVariantId left, SpineVariantId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SpineVariantId left, SpineVariantId right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct ClusterChunkCoord : IEquatable<ClusterChunkCoord>, IComparable<ClusterChunkCoord>
    {
        public ClusterChunkCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        public int CompareTo(ClusterChunkCoord other)
        {
            var comparison = Y.CompareTo(other.Y);
            return comparison != 0 ? comparison : X.CompareTo(other.X);
        }

        public bool Equals(ClusterChunkCoord other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is ClusterChunkCoord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public static bool operator ==(ClusterChunkCoord left, ClusterChunkCoord right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ClusterChunkCoord left, ClusterChunkCoord right)
        {
            return !left.Equals(right);
        }
    }

    public sealed class ClusterFootprint
    {
        private readonly ReadOnlyCollection<ClusterChunkCoord> activeChunks;

        public ClusterFootprint(IEnumerable<ClusterChunkCoord> activeChunks)
        {
            var copy = activeChunks == null
                ? Array.Empty<ClusterChunkCoord>()
                : activeChunks.ToArray();
            Array.Sort(copy);
            this.activeChunks = new ReadOnlyCollection<ClusterChunkCoord>(copy);
        }

        public IReadOnlyList<ClusterChunkCoord> ActiveChunks => activeChunks;
    }

    public enum ClusterRoleKind
    {
        Entry = 1,
        BuildUp = 2,
        Core = 3,
        Recovery = 4,
        Reward = 5,
        Exit = 6,
    }

    public sealed class ClusterRoleAnchor
    {
        public ClusterRoleAnchor(
            string anchorId,
            ClusterRoleKind role,
            LocalTileCoord tile,
            string traversalNodeId)
        {
            AnchorId = anchorId ?? string.Empty;
            Role = role;
            Tile = tile;
            TraversalNodeId = traversalNodeId ?? string.Empty;
        }

        public string AnchorId { get; }
        public ClusterRoleKind Role { get; }
        public LocalTileCoord Tile { get; }
        public string TraversalNodeId { get; }
    }

    public enum ClusterPortKind
    {
        Entry = 1,
        Exit = 2,
    }

    public enum ClusterPortSide
    {
        L = 1,
        R = 2,
        U = 3,
        D = 4,
    }

    public sealed class ClusterPort
    {
        private readonly ReadOnlyCollection<int> compatibleRouteTypes;

        public ClusterPort(
            string portId,
            ClusterPortKind kind,
            bool isPrimary,
            string roleAnchorId,
            LocalTileCoord tile,
            ClusterPortSide outwardSide,
            IEnumerable<int> compatibleRouteTypes)
        {
            PortId = portId ?? string.Empty;
            Kind = kind;
            IsPrimary = isPrimary;
            RoleAnchorId = roleAnchorId ?? string.Empty;
            Tile = tile;
            OutwardSide = outwardSide;
            var copy = compatibleRouteTypes == null ? Array.Empty<int>() : compatibleRouteTypes.ToArray();
            Array.Sort(copy);
            this.compatibleRouteTypes = new ReadOnlyCollection<int>(copy);
        }

        public string PortId { get; }
        public ClusterPortKind Kind { get; }
        public bool IsPrimary { get; }
        public string RoleAnchorId { get; }
        public LocalTileCoord Tile { get; }
        public ClusterPortSide OutwardSide { get; }
        public IReadOnlyList<int> CompatibleRouteTypes => compatibleRouteTypes;
    }

    public enum TraversalGraphKind
    {
        Traversal = 1,
        Mechanism = 2,
        Progression = 3,
    }

    public enum TraversalMovementKind
    {
        Walk = 1,
        Jump = 2,
        Drop = 3,
        Climb = 4,
        Slide = 5,
        Bounce = 6,
    }

    public sealed class TraversalNode
    {
        public TraversalNode(
            string nodeId,
            LocalTileCoord tile,
            bool isMandatory,
            string roleAnchorId,
            TraversalGraphKind graphKind = TraversalGraphKind.Traversal)
        {
            NodeId = nodeId ?? string.Empty;
            Tile = tile;
            IsMandatory = isMandatory;
            RoleAnchorId = roleAnchorId ?? string.Empty;
            GraphKind = graphKind;
        }

        public string NodeId { get; }
        public LocalTileCoord Tile { get; }
        public bool IsMandatory { get; }
        public string RoleAnchorId { get; }
        public TraversalGraphKind GraphKind { get; }
    }

    public sealed class TraversalEnvelope
    {
        private readonly ReadOnlyCollection<LocalTileCoord> centerline;
        private readonly ReadOnlyCollection<LocalTileCoord> floor;
        private readonly ReadOnlyCollection<LocalTileCoord> clearance;
        private readonly ReadOnlyCollection<LocalTileCoord> jumpArc;
        private readonly ReadOnlyCollection<LocalTileCoord> dropColumn;
        private readonly ReadOnlyCollection<LocalTileCoord> landing;
        private readonly ReadOnlyCollection<LocalTileCoord> recovery;
        private readonly ReadOnlyCollection<LocalTileCoord> protectedTiles;

        public TraversalEnvelope(
            IEnumerable<LocalTileCoord> centerline,
            IEnumerable<LocalTileCoord> floor,
            IEnumerable<LocalTileCoord> clearance,
            IEnumerable<LocalTileCoord> jumpArc,
            IEnumerable<LocalTileCoord> dropColumn,
            IEnumerable<LocalTileCoord> landing,
            IEnumerable<LocalTileCoord> recovery)
        {
            this.centerline = CopyCoordinates(centerline);
            this.floor = CopyCoordinates(floor);
            this.clearance = CopyCoordinates(clearance);
            this.jumpArc = CopyCoordinates(jumpArc);
            this.dropColumn = CopyCoordinates(dropColumn);
            this.landing = CopyCoordinates(landing);
            this.recovery = CopyCoordinates(recovery);
            protectedTiles = CopyCoordinates(
                this.centerline
                    .Concat(this.floor)
                    .Concat(this.clearance)
                    .Concat(this.jumpArc)
                    .Concat(this.dropColumn)
                    .Concat(this.landing)
                    .Concat(this.recovery)
                    .Distinct());
        }

        public IReadOnlyList<LocalTileCoord> Centerline => centerline;
        public IReadOnlyList<LocalTileCoord> Floor => floor;
        public IReadOnlyList<LocalTileCoord> Clearance => clearance;
        public IReadOnlyList<LocalTileCoord> JumpArc => jumpArc;
        public IReadOnlyList<LocalTileCoord> DropColumn => dropColumn;
        public IReadOnlyList<LocalTileCoord> Landing => landing;
        public IReadOnlyList<LocalTileCoord> Recovery => recovery;
        public IReadOnlyList<LocalTileCoord> ProtectedTiles => protectedTiles;

        private static ReadOnlyCollection<LocalTileCoord> CopyCoordinates(
            IEnumerable<LocalTileCoord> source)
        {
            var copy = source == null ? Array.Empty<LocalTileCoord>() : source.ToArray();
            Array.Sort(copy, CompareCoordinates);
            return new ReadOnlyCollection<LocalTileCoord>(copy);
        }

        private static int CompareCoordinates(LocalTileCoord left, LocalTileCoord right)
        {
            var comparison = left.Y.CompareTo(right.Y);
            return comparison != 0 ? comparison : left.X.CompareTo(right.X);
        }
    }

    public sealed class TraversalEdge
    {
        public TraversalEdge(
            string edgeId,
            string fromNodeId,
            string toNodeId,
            TraversalMovementKind movementKind,
            LocalTileCoord startTile,
            LocalTileCoord endTile,
            int minimumClearanceWidth,
            int minimumClearanceHeight,
            LocalTileCoord? landingTile,
            LocalTileCoord? recoveryTile,
            bool isMandatory,
            TraversalEnvelope envelope,
            TraversalGraphKind graphKind = TraversalGraphKind.Traversal)
        {
            EdgeId = edgeId ?? string.Empty;
            FromNodeId = fromNodeId ?? string.Empty;
            ToNodeId = toNodeId ?? string.Empty;
            MovementKind = movementKind;
            StartTile = startTile;
            EndTile = endTile;
            MinimumClearanceWidth = minimumClearanceWidth;
            MinimumClearanceHeight = minimumClearanceHeight;
            LandingTile = landingTile;
            RecoveryTile = recoveryTile;
            IsMandatory = isMandatory;
            Envelope = envelope;
            GraphKind = graphKind;
        }

        public string EdgeId { get; }
        public string FromNodeId { get; }
        public string ToNodeId { get; }
        public TraversalMovementKind MovementKind { get; }
        public LocalTileCoord StartTile { get; }
        public LocalTileCoord EndTile { get; }
        public int MinimumClearanceWidth { get; }
        public int MinimumClearanceHeight { get; }
        public LocalTileCoord? LandingTile { get; }
        public LocalTileCoord? RecoveryTile { get; }
        public bool IsMandatory { get; }
        public TraversalEnvelope Envelope { get; }
        public TraversalGraphKind GraphKind { get; }
    }

    public sealed class SpineVariant
    {
        private readonly ReadOnlyCollection<TraversalNode> nodes;
        private readonly ReadOnlyCollection<TraversalEdge> edges;

        public SpineVariant(
            SpineVariantId id,
            bool isBaseline,
            TraversalGraphKind graphKind,
            IEnumerable<TraversalNode> nodes,
            IEnumerable<TraversalEdge> edges)
        {
            Id = id;
            IsBaseline = isBaseline;
            GraphKind = graphKind;
            var nodeCopy = nodes == null ? Array.Empty<TraversalNode>() : nodes.ToArray();
            Array.Sort(nodeCopy, CompareNodes);
            this.nodes = new ReadOnlyCollection<TraversalNode>(nodeCopy);
            var edgeCopy = edges == null ? Array.Empty<TraversalEdge>() : edges.ToArray();
            Array.Sort(edgeCopy, CompareEdges);
            this.edges = new ReadOnlyCollection<TraversalEdge>(edgeCopy);
        }

        public SpineVariantId Id { get; }
        public bool IsBaseline { get; }
        public TraversalGraphKind GraphKind { get; }
        public IReadOnlyList<TraversalNode> Nodes => nodes;
        public IReadOnlyList<TraversalEdge> Edges => edges;

        private static int CompareNodes(TraversalNode left, TraversalNode right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return -1;
            if (right == null) return 1;
            return string.Compare(left.NodeId, right.NodeId, StringComparison.Ordinal);
        }

        private static int CompareEdges(TraversalEdge left, TraversalEdge right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return -1;
            if (right == null) return 1;
            return string.Compare(left.EdgeId, right.EdgeId, StringComparison.Ordinal);
        }
    }

    public sealed class TerrainClusterTraversalContract
    {
        private readonly ReadOnlyCollection<SpineVariant> variants;

        public TerrainClusterTraversalContract(IEnumerable<SpineVariant> variants)
        {
            var copy = variants == null ? Array.Empty<SpineVariant>() : variants.ToArray();
            Array.Sort(copy, CompareVariants);
            this.variants = new ReadOnlyCollection<SpineVariant>(copy);
        }

        public IReadOnlyList<SpineVariant> Variants => variants;

        private static int CompareVariants(SpineVariant left, SpineVariant right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return -1;
            if (right == null) return 1;
            return left.Id.CompareTo(right.Id);
        }
    }

    public sealed class TerrainClusterContract
    {
        private readonly ReadOnlyCollection<ClusterRoleAnchor> roleAnchors;
        private readonly ReadOnlyCollection<ClusterPort> ports;

        public TerrainClusterContract(
            TerrainClusterId id,
            ClusterFootprint footprint,
            IEnumerable<ClusterRoleAnchor> roleAnchors,
            IEnumerable<ClusterPort> ports,
            TerrainClusterTraversalContract traversal,
            string displayText = null)
        {
            Id = id;
            Footprint = footprint;
            var roleCopy = roleAnchors == null ? Array.Empty<ClusterRoleAnchor>() : roleAnchors.ToArray();
            Array.Sort(roleCopy, CompareRoles);
            this.roleAnchors = new ReadOnlyCollection<ClusterRoleAnchor>(roleCopy);
            var portCopy = ports == null ? Array.Empty<ClusterPort>() : ports.ToArray();
            Array.Sort(portCopy, ComparePorts);
            this.ports = new ReadOnlyCollection<ClusterPort>(portCopy);
            Traversal = traversal;
            DisplayText = displayText ?? string.Empty;
        }

        public TerrainClusterId Id { get; }
        public ClusterFootprint Footprint { get; }
        public IReadOnlyList<ClusterRoleAnchor> RoleAnchors => roleAnchors;
        public IReadOnlyList<ClusterPort> Ports => ports;
        public TerrainClusterTraversalContract Traversal { get; }
        public string DisplayText { get; }

        public string GetCanonicalDigest(IEnumerable<TerrainClusterId> sixChunkAllowlist = null)
        {
            var result = TerrainClusterContractValidator.Validate(this, sixChunkAllowlist);
            if (!result.IsValid)
            {
                throw new InvalidOperationException(
                    "Cannot compute a published digest for an invalid TerrainCluster contract.");
            }

            return result.CanonicalDigest;
        }

        private static int CompareRoles(ClusterRoleAnchor left, ClusterRoleAnchor right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return -1;
            if (right == null) return 1;
            return string.Compare(left.AnchorId, right.AnchorId, StringComparison.Ordinal);
        }

        private static int ComparePorts(ClusterPort left, ClusterPort right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return -1;
            if (right == null) return 1;
            return string.Compare(left.PortId, right.PortId, StringComparison.Ordinal);
        }
    }
}
