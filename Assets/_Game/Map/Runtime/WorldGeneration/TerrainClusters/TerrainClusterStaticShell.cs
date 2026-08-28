using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.TerrainClusters
{
    public enum TerrainClusterShellOccupancy
    {
        Air = 1,
        Solid = 2,
    }

    public sealed class TerrainClusterStaticShellProvenance :
        IEquatable<TerrainClusterStaticShellProvenance>,
        IComparable<TerrainClusterStaticShellProvenance>
    {
        internal TerrainClusterStaticShellProvenance(
            SpineVariantId variantId,
            string edgeId,
            CompiledTraversalEnvelopeSetKind envelopeSetKind,
            LocalTileCoord sourceCoordinate,
            LocalTileCoord compiledCoordinate)
        {
            VariantId = variantId;
            EdgeId = edgeId ?? string.Empty;
            EnvelopeSetKind = envelopeSetKind;
            SourceCoordinate = sourceCoordinate;
            CompiledCoordinate = compiledCoordinate;
        }

        public SpineVariantId VariantId { get; }
        public string EdgeId { get; }
        public CompiledTraversalEnvelopeSetKind EnvelopeSetKind { get; }
        public LocalTileCoord SourceCoordinate { get; }
        public LocalTileCoord CompiledCoordinate { get; }

        public int CompareTo(TerrainClusterStaticShellProvenance other)
        {
            if (other == null) return -1;
            var comparison = VariantId.CompareTo(other.VariantId);
            if (comparison != 0) return comparison;
            comparison = string.Compare(EdgeId, other.EdgeId, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = EnvelopeSetKind.CompareTo(other.EnvelopeSetKind);
            if (comparison != 0) return comparison;
            comparison = SourceCoordinate.Y.CompareTo(other.SourceCoordinate.Y);
            return comparison != 0 ? comparison : SourceCoordinate.X.CompareTo(other.SourceCoordinate.X);
        }

        public bool Equals(TerrainClusterStaticShellProvenance other)
        {
            return other != null && VariantId == other.VariantId &&
                string.Equals(EdgeId, other.EdgeId, StringComparison.Ordinal) &&
                EnvelopeSetKind == other.EnvelopeSetKind &&
                SourceCoordinate == other.SourceCoordinate &&
                CompiledCoordinate == other.CompiledCoordinate;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TerrainClusterStaticShellProvenance);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = VariantId.GetHashCode();
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(EdgeId);
                hash = (hash * 397) ^ (int)EnvelopeSetKind;
                hash = (hash * 397) ^ SourceCoordinate.GetHashCode();
                return (hash * 397) ^ CompiledCoordinate.GetHashCode();
            }
        }
    }

    public sealed class TerrainClusterStaticShellCell
    {
        private readonly ReadOnlyCollection<TerrainClusterStaticShellProvenance> provenance;

        internal TerrainClusterStaticShellCell(
            LocalTileCoord compiledCoordinate,
            ClusterChunkCoord owningChunk,
            TerrainClusterShellOccupancy occupancy,
            bool isProtectedOpen,
            IEnumerable<TerrainClusterStaticShellProvenance> provenance)
        {
            CompiledCoordinate = compiledCoordinate;
            OwningChunk = owningChunk;
            Occupancy = occupancy;
            IsProtectedOpen = isProtectedOpen;
            this.provenance = new ReadOnlyCollection<TerrainClusterStaticShellProvenance>(
                (provenance ?? Array.Empty<TerrainClusterStaticShellProvenance>())
                    .Where(value => value != null).Distinct().OrderBy(value => value).ToArray());
        }

        public LocalTileCoord CompiledCoordinate { get; }
        public ClusterChunkCoord OwningChunk { get; }
        public TerrainClusterShellOccupancy Occupancy { get; }
        public bool IsProtectedOpen { get; }
        public IReadOnlyList<TerrainClusterStaticShellProvenance> Provenance => provenance;
    }

    public sealed class TerrainClusterStaticShell
    {
        private readonly ReadOnlyCollection<TerrainClusterStaticShellCell> cells;
        private readonly ReadOnlyDictionary<LocalTileCoord, TerrainClusterStaticShellCell> cellsByCoordinate;

        internal TerrainClusterStaticShell(
            TerrainClusterId clusterId,
            string localCanvasDigest,
            string traversalCompilationDigest,
            IEnumerable<TerrainClusterStaticShellCell> cells)
        {
            ClusterId = clusterId;
            LocalCanvasDigest = localCanvasDigest ?? string.Empty;
            TraversalCompilationDigest = traversalCompilationDigest ?? string.Empty;
            var copy = (cells ?? Array.Empty<TerrainClusterStaticShellCell>())
                .Where(value => value != null)
                .OrderBy(value => value.CompiledCoordinate.Y)
                .ThenBy(value => value.CompiledCoordinate.X)
                .ToArray();
            this.cells = new ReadOnlyCollection<TerrainClusterStaticShellCell>(copy);
            cellsByCoordinate = new ReadOnlyDictionary<LocalTileCoord, TerrainClusterStaticShellCell>(
                copy.ToDictionary(value => value.CompiledCoordinate));
        }

        public TerrainClusterId ClusterId { get; }
        public string LocalCanvasDigest { get; }
        public string TraversalCompilationDigest { get; }
        public IReadOnlyList<TerrainClusterStaticShellCell> Cells => cells;
        public int ActiveTileCount => cells.Count;
        public int PatternOperationCount => 0;

        public bool TryGetCell(LocalTileCoord coordinate, out TerrainClusterStaticShellCell cell)
        {
            return cellsByCoordinate.TryGetValue(coordinate, out cell);
        }
    }
}
