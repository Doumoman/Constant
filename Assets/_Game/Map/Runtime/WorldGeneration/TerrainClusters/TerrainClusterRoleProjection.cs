using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.TerrainClusters
{
    public enum ProjectedRoleConnectionKind
    {
        EntryPort = 1,
        ExitPort = 2,
        InternalRole = 3,
    }

    public sealed class ProjectedClusterRoleAnchor
    {
        public ProjectedClusterRoleAnchor(
            string anchorId,
            ClusterRoleKind role,
            LocalTileCoord sourceCoordinate,
            LocalTileCoord compiledCoordinate,
            ClusterChunkCoord owningCompiledChunk,
            string traversalNodeId)
        {
            AnchorId = anchorId ?? string.Empty;
            Role = role;
            SourceCoordinate = sourceCoordinate;
            CompiledCoordinate = compiledCoordinate;
            OwningCompiledChunk = owningCompiledChunk;
            TraversalNodeId = traversalNodeId ?? string.Empty;
        }

        public string AnchorId { get; }
        public ClusterRoleKind Role { get; }
        public LocalTileCoord SourceCoordinate { get; }
        public LocalTileCoord CompiledCoordinate { get; }
        public ClusterChunkCoord OwningCompiledChunk { get; }
        public string TraversalNodeId { get; }
    }

    public sealed class ProjectedClusterPort
    {
        private readonly ReadOnlyCollection<int> compatibleRouteTypes;

        public ProjectedClusterPort(
            string portId,
            ClusterPortKind kind,
            LocalTileCoord sourceCoordinate,
            LocalTileCoord compiledCoordinate,
            ClusterPortSide sourceOutwardSide,
            ClusterPortSide compiledOutwardSide,
            string roleAnchorId,
            ClusterRoleKind roleKind,
            IEnumerable<int> compatibleRouteTypes)
        {
            PortId = portId ?? string.Empty;
            Kind = kind;
            SourceCoordinate = sourceCoordinate;
            CompiledCoordinate = compiledCoordinate;
            SourceOutwardSide = sourceOutwardSide;
            CompiledOutwardSide = compiledOutwardSide;
            RoleAnchorId = roleAnchorId ?? string.Empty;
            RoleKind = roleKind;
            var copy = (compatibleRouteTypes ?? Array.Empty<int>()).ToArray();
            Array.Sort(copy);
            this.compatibleRouteTypes = new ReadOnlyCollection<int>(copy);
        }

        public string PortId { get; }
        public ClusterPortKind Kind { get; }
        public LocalTileCoord SourceCoordinate { get; }
        public LocalTileCoord CompiledCoordinate { get; }
        public ClusterPortSide SourceOutwardSide { get; }
        public ClusterPortSide CompiledOutwardSide { get; }
        public string RoleAnchorId { get; }
        public ClusterRoleKind RoleKind { get; }
        public IReadOnlyList<int> CompatibleRouteTypes => compatibleRouteTypes;
    }

    public sealed class ProjectedRoleSpineLink
    {
        public ProjectedRoleSpineLink(
            SpineVariantId variantId,
            bool isBaseline,
            string roleAnchorId,
            ClusterRoleKind roleKind,
            string traversalNodeId,
            LocalTileCoord sourceCoordinate,
            LocalTileCoord compiledCoordinate,
            ProjectedRoleConnectionKind connectionKind)
        {
            VariantId = variantId;
            IsBaseline = isBaseline;
            RoleAnchorId = roleAnchorId ?? string.Empty;
            RoleKind = roleKind;
            TraversalNodeId = traversalNodeId ?? string.Empty;
            SourceCoordinate = sourceCoordinate;
            CompiledCoordinate = compiledCoordinate;
            ConnectionKind = connectionKind;
        }

        public SpineVariantId VariantId { get; }
        public bool IsBaseline { get; }
        public string RoleAnchorId { get; }
        public ClusterRoleKind RoleKind { get; }
        public string TraversalNodeId { get; }
        public LocalTileCoord SourceCoordinate { get; }
        public LocalTileCoord CompiledCoordinate { get; }
        public ProjectedRoleConnectionKind ConnectionKind { get; }
    }

    internal static class ClusterRoleSocketTransformUtility
    {
        public static bool TryTransformSide(
            ClusterPortSide source,
            ClusterFootprintTransform transform,
            out ClusterPortSide compiled)
        {
            if (source < ClusterPortSide.L || source > ClusterPortSide.D)
            {
                compiled = default(ClusterPortSide);
                return false;
            }

            switch (transform)
            {
                case ClusterFootprintTransform.R0:
                    compiled = source;
                    return true;
                case ClusterFootprintTransform.MirrorX:
                    compiled = source == ClusterPortSide.L ? ClusterPortSide.R :
                        source == ClusterPortSide.R ? ClusterPortSide.L : source;
                    return true;
                case ClusterFootprintTransform.MirrorY:
                    compiled = source == ClusterPortSide.U ? ClusterPortSide.D :
                        source == ClusterPortSide.D ? ClusterPortSide.U : source;
                    return true;
                case ClusterFootprintTransform.R180:
                    compiled = source == ClusterPortSide.L ? ClusterPortSide.R :
                        source == ClusterPortSide.R ? ClusterPortSide.L :
                        source == ClusterPortSide.U ? ClusterPortSide.D : ClusterPortSide.U;
                    return true;
                default:
                    compiled = default(ClusterPortSide);
                    return false;
            }
        }

        public static bool TryNeighbor(
            LocalTileCoord coordinate,
            ClusterPortSide side,
            out LocalTileCoord neighbor)
        {
            switch (side)
            {
                case ClusterPortSide.L:
                    neighbor = new LocalTileCoord(coordinate.X - 1, coordinate.Y);
                    return true;
                case ClusterPortSide.R:
                    neighbor = new LocalTileCoord(coordinate.X + 1, coordinate.Y);
                    return true;
                case ClusterPortSide.U:
                    neighbor = new LocalTileCoord(coordinate.X, coordinate.Y + 1);
                    return true;
                case ClusterPortSide.D:
                    neighbor = new LocalTileCoord(coordinate.X, coordinate.Y - 1);
                    return true;
                default:
                    neighbor = default(LocalTileCoord);
                    return false;
            }
        }
    }
}
