using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Baking
{
    public readonly struct GeneratedSliceCoord : IEquatable<GeneratedSliceCoord>, IComparable<GeneratedSliceCoord>
    {
        public GeneratedSliceCoord(int x, int y) { X = x; Y = y; }
        public int X { get; }
        public int Y { get; }
        public int CanonicalIndex => Y * WorldGenConstants.MicroChunkColumnsPerSector + X;
        public int CompareTo(GeneratedSliceCoord other) => CanonicalIndex.CompareTo(other.CanonicalIndex);
        public bool Equals(GeneratedSliceCoord other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GeneratedSliceCoord other && Equals(other);
        public override int GetHashCode() { unchecked { return (X * 397) ^ Y; } }
        public override string ToString() => X + "," + Y;
        public static bool operator ==(GeneratedSliceCoord left, GeneratedSliceCoord right) => left.Equals(right);
        public static bool operator !=(GeneratedSliceCoord left, GeneratedSliceCoord right) => !left.Equals(right);
    }

    public sealed class GeneratedSliceCell
    {
        public GeneratedSliceCell(
            LocalTileCoord localCoordinate,
            SectorCanvasLayerSnapshot layers,
            SectorCanvasProvenance provenance)
        {
            LocalCoordinate = localCoordinate;
            Layers = layers;
            Provenance = provenance;
        }

        public LocalTileCoord LocalCoordinate { get; }
        public SectorCanvasLayerSnapshot Layers { get; }
        public SectorCanvasProvenance Provenance { get; }
        public int CanonicalIndex => LocalCoordinate.Y * WorldGenConstants.MicroChunkWidthTiles + LocalCoordinate.X;
    }

    public enum GeneratedSliceTransform
    {
        None = 1,
        Rotate90 = 2,
        MirrorX = 3,
        MirrorY = 4,
        Resample = 5,
        Pad = 6,
    }

    public sealed class GeneratedSliceProvenance
    {
        public GeneratedSliceProvenance(
            SectorCanvasId sourceCanvasId,
            string sourceCanvasDigest,
            string sourceValidationStampDigest,
            GeneratedSliceTransform transform)
        {
            SourceCanvasId = sourceCanvasId;
            SourceCanvasDigest = sourceCanvasDigest ?? string.Empty;
            SourceValidationStampDigest = sourceValidationStampDigest ?? string.Empty;
            Transform = transform;
        }

        public SectorCanvasId SourceCanvasId { get; }
        public string SourceCanvasDigest { get; }
        public string SourceValidationStampDigest { get; }
        public GeneratedSliceTransform Transform { get; }
    }

    public sealed class GeneratedMicroChunkSlice
    {
        private readonly ReadOnlyCollection<GeneratedSliceCell> cells;

        public GeneratedMicroChunkSlice(
            GeneratedSliceCoord coordinate,
            IEnumerable<GeneratedSliceCell> cells,
            GeneratedSliceProvenance provenance)
        {
            Coordinate = coordinate;
            var copy = cells == null ? Array.Empty<GeneratedSliceCell>() : cells.ToArray();
            Array.Sort(copy, CompareCells);
            this.cells = new ReadOnlyCollection<GeneratedSliceCell>(copy);
            Provenance = provenance;
        }

        public GeneratedSliceCoord Coordinate { get; }
        public IReadOnlyList<GeneratedSliceCell> Cells => cells;
        public GeneratedSliceProvenance Provenance { get; }

        private static int CompareCells(GeneratedSliceCell left, GeneratedSliceCell right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return -1;
            if (right == null) return 1;
            return left.CanonicalIndex.CompareTo(right.CanonicalIndex);
        }
    }

    public enum GeneratedSliceBoundaryRole
    {
        GeneratedOutput = 1,
        AuthoringSource = 2,
    }

    public sealed class GeneratedSliceSet
    {
        private readonly ReadOnlyCollection<GeneratedMicroChunkSlice> slices;

        public GeneratedSliceSet(
            SectorCanvasId sourceCanvasId,
            IEnumerable<GeneratedMicroChunkSlice> slices,
            GeneratedSliceBoundaryRole boundaryRole)
        {
            SourceCanvasId = sourceCanvasId;
            var copy = slices == null ? Array.Empty<GeneratedMicroChunkSlice>() : slices.ToArray();
            Array.Sort(copy, CompareSlices);
            this.slices = new ReadOnlyCollection<GeneratedMicroChunkSlice>(copy);
            BoundaryRole = boundaryRole;
        }

        public SectorCanvasId SourceCanvasId { get; }
        public IReadOnlyList<GeneratedMicroChunkSlice> Slices => slices;
        public GeneratedSliceBoundaryRole BoundaryRole { get; }

        private static int CompareSlices(GeneratedMicroChunkSlice left, GeneratedMicroChunkSlice right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return -1;
            if (right == null) return 1;
            return left.Coordinate.CompareTo(right.Coordinate);
        }
    }
}
