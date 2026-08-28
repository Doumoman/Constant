using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.TerrainClusters
{
    public sealed class CompiledClusterChunkCell
    {
        public CompiledClusterChunkCell(
            ClusterChunkCoord coordinate,
            ClusterChunkMaskState state,
            ClusterChunkCoord sourceCoordinate,
            int canonicalIndex)
        {
            Coordinate = coordinate;
            State = state;
            SourceCoordinate = sourceCoordinate;
            CanonicalIndex = canonicalIndex;
        }

        public ClusterChunkCoord Coordinate { get; }
        public ClusterChunkMaskState State { get; }
        public ClusterChunkCoord SourceCoordinate { get; }
        public int CanonicalIndex { get; }
    }

    public sealed class CompiledClusterLocalTileCell
    {
        public CompiledClusterLocalTileCell(
            LocalTileCoord coordinate,
            ClusterChunkCoord owningChunk,
            LocalTileCoord withinChunkCoordinate,
            ClusterChunkMaskState state,
            ClusterChunkCoord sourceChunkCoordinate,
            LocalTileCoord sourceCoordinate,
            int canonicalIndex)
        {
            Coordinate = coordinate;
            OwningChunk = owningChunk;
            WithinChunkCoordinate = withinChunkCoordinate;
            State = state;
            SourceChunkCoordinate = sourceChunkCoordinate;
            SourceCoordinate = sourceCoordinate;
            CanonicalIndex = canonicalIndex;
        }

        public LocalTileCoord Coordinate { get; }
        public ClusterChunkCoord OwningChunk { get; }
        public LocalTileCoord WithinChunkCoordinate { get; }
        public ClusterChunkMaskState State { get; }
        public ClusterChunkCoord SourceChunkCoordinate { get; }
        public LocalTileCoord SourceCoordinate { get; }
        public int CanonicalIndex { get; }
    }

    public sealed class TerrainClusterLocalCanvas
    {
        private readonly ReadOnlyCollection<CompiledClusterChunkCell> chunkCells;
        private readonly ReadOnlyCollection<CompiledClusterLocalTileCell> tileCells;
        private readonly ReadOnlyDictionary<ClusterChunkCoord, CompiledClusterChunkCell> chunksByCoordinate;
        private readonly ReadOnlyDictionary<LocalTileCoord, CompiledClusterLocalTileCell> tilesByCoordinate;
        private readonly ReadOnlyDictionary<ClusterChunkCoord, ClusterChunkCoord> sourceToCompiledChunks;
        private readonly ReadOnlyDictionary<ClusterChunkCoord, ClusterChunkCoord> compiledToSourceChunks;
        private readonly ReadOnlyDictionary<LocalTileCoord, LocalTileCoord> sourceToCompiledTiles;
        private readonly ReadOnlyDictionary<LocalTileCoord, LocalTileCoord> compiledToSourceTiles;

        internal TerrainClusterLocalCanvas(
            TerrainClusterId clusterId,
            string sourceFootprintDigest,
            ClusterFootprintTransform transform,
            int chunkWidth,
            int chunkHeight,
            int tileWidth,
            int tileHeight,
            IEnumerable<CompiledClusterChunkCell> chunkCells,
            IEnumerable<CompiledClusterLocalTileCell> tileCells,
            string canonicalDigest)
        {
            ClusterId = clusterId;
            SourceFootprintDigest = sourceFootprintDigest ?? string.Empty;
            Transform = transform;
            ChunkWidth = chunkWidth;
            ChunkHeight = chunkHeight;
            TileWidth = tileWidth;
            TileHeight = tileHeight;
            CanonicalDigest = canonicalDigest ?? string.Empty;

            var chunkCopy = chunkCells.ToArray();
            Array.Sort(chunkCopy, (left, right) => left.CanonicalIndex.CompareTo(right.CanonicalIndex));
            this.chunkCells = new ReadOnlyCollection<CompiledClusterChunkCell>(chunkCopy);

            var tileCopy = tileCells.ToArray();
            Array.Sort(tileCopy, (left, right) => left.CanonicalIndex.CompareTo(right.CanonicalIndex));
            this.tileCells = new ReadOnlyCollection<CompiledClusterLocalTileCell>(tileCopy);

            chunksByCoordinate = new ReadOnlyDictionary<ClusterChunkCoord, CompiledClusterChunkCell>(
                chunkCopy.ToDictionary(value => value.Coordinate));
            tilesByCoordinate = new ReadOnlyDictionary<LocalTileCoord, CompiledClusterLocalTileCell>(
                tileCopy.ToDictionary(value => value.Coordinate));
            sourceToCompiledChunks = new ReadOnlyDictionary<ClusterChunkCoord, ClusterChunkCoord>(
                chunkCopy.ToDictionary(value => value.SourceCoordinate, value => value.Coordinate));
            compiledToSourceChunks = new ReadOnlyDictionary<ClusterChunkCoord, ClusterChunkCoord>(
                chunkCopy.ToDictionary(value => value.Coordinate, value => value.SourceCoordinate));
            sourceToCompiledTiles = new ReadOnlyDictionary<LocalTileCoord, LocalTileCoord>(
                tileCopy.ToDictionary(value => value.SourceCoordinate, value => value.Coordinate));
            compiledToSourceTiles = new ReadOnlyDictionary<LocalTileCoord, LocalTileCoord>(
                tileCopy.ToDictionary(value => value.Coordinate, value => value.SourceCoordinate));
        }

        public TerrainClusterId ClusterId { get; }
        public string SourceFootprintDigest { get; }
        public ClusterFootprintTransform Transform { get; }
        public int ChunkWidth { get; }
        public int ChunkHeight { get; }
        public int TileWidth { get; }
        public int TileHeight { get; }
        public IReadOnlyList<CompiledClusterChunkCell> ChunkCells => chunkCells;
        public IReadOnlyList<CompiledClusterLocalTileCell> TileCells => tileCells;
        public string CanonicalDigest { get; }

        public bool TryGetChunkCell(
            ClusterChunkCoord coordinate,
            out CompiledClusterChunkCell cell)
        {
            return chunksByCoordinate.TryGetValue(coordinate, out cell);
        }

        public bool TryGetTileCell(
            LocalTileCoord coordinate,
            out CompiledClusterLocalTileCell cell)
        {
            return tilesByCoordinate.TryGetValue(coordinate, out cell);
        }

        public bool TryGetCompiledChunk(
            ClusterChunkCoord sourceCoordinate,
            out ClusterChunkCoord compiledCoordinate)
        {
            return sourceToCompiledChunks.TryGetValue(sourceCoordinate, out compiledCoordinate);
        }

        public bool TryGetSourceChunk(
            ClusterChunkCoord compiledCoordinate,
            out ClusterChunkCoord sourceCoordinate)
        {
            return compiledToSourceChunks.TryGetValue(compiledCoordinate, out sourceCoordinate);
        }

        public bool TryGetCompiledTile(
            LocalTileCoord sourceCoordinate,
            out LocalTileCoord compiledCoordinate)
        {
            return sourceToCompiledTiles.TryGetValue(sourceCoordinate, out compiledCoordinate);
        }

        public bool TryGetSourceTile(
            LocalTileCoord compiledCoordinate,
            out LocalTileCoord sourceCoordinate)
        {
            return compiledToSourceTiles.TryGetValue(compiledCoordinate, out sourceCoordinate);
        }
    }

    public sealed class TerrainClusterFootprintCompileRequest
    {
        private readonly ReadOnlyCollection<TerrainClusterId> sixChunkAllowlist;

        public TerrainClusterFootprintCompileRequest(
            TerrainClusterContract sourceContract,
            ClusterFootprintTransform transform,
            IEnumerable<TerrainClusterId> sixChunkAllowlist = null)
        {
            SourceContract = sourceContract;
            Transform = transform;
            var copy = (sixChunkAllowlist ?? Array.Empty<TerrainClusterId>())
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            this.sixChunkAllowlist = new ReadOnlyCollection<TerrainClusterId>(copy);
        }

        public TerrainClusterContract SourceContract { get; }
        public ClusterFootprintTransform Transform { get; }
        public IReadOnlyList<TerrainClusterId> SixChunkAllowlist => sixChunkAllowlist;
    }

    public enum TerrainClusterFootprintCompileErrorCode
    {
        MissingInput = 1,
        InvalidSourceFootprint = 2,
        SixChunkNotAllowlisted = 3,
        InvalidTransform = 4,
        InvalidChunkBounds = 5,
        TransformMappingMismatch = 6,
        DisconnectedCompiledFootprint = 7,
        MissingOrDuplicateChunkCell = 8,
        ChunkMaskCountMismatch = 9,
        MissingOrDuplicateTileCell = 10,
        TileChunkMappingMismatch = 11,
        TileMaskCountMismatch = 12,
        NonCanonicalPublication = 13,
    }

    public sealed class TerrainClusterFootprintCompileError :
        IEquatable<TerrainClusterFootprintCompileError>,
        IComparable<TerrainClusterFootprintCompileError>
    {
        public TerrainClusterFootprintCompileError(
            TerrainClusterFootprintCompileErrorCode code,
            string path,
            string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public TerrainClusterFootprintCompileErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(TerrainClusterFootprintCompileError other)
        {
            if (other == null) return -1;
            var comparison = ((int)Code).CompareTo((int)other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(TerrainClusterFootprintCompileError other)
        {
            return other != null &&
                   Code == other.Code &&
                   string.Equals(Path, other.Path, StringComparison.Ordinal) &&
                   string.Equals(Detail, other.Detail, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TerrainClusterFootprintCompileError);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Code;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Path);
                return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Detail);
            }
        }

        public override string ToString()
        {
            return Code + "|" + Path + "|" + Detail;
        }
    }

    public sealed class TerrainClusterFootprintCompileResult
    {
        private static readonly IReadOnlyList<CompiledClusterChunkCell> EmptyChunkCells =
            Array.Empty<CompiledClusterChunkCell>();
        private static readonly IReadOnlyList<CompiledClusterLocalTileCell> EmptyTileCells =
            Array.Empty<CompiledClusterLocalTileCell>();
        private readonly ReadOnlyCollection<TerrainClusterFootprintCompileError> errors;

        internal TerrainClusterFootprintCompileResult(
            TerrainClusterLocalCanvas localCanvas,
            IEnumerable<TerrainClusterFootprintCompileError> errors)
        {
            var copy = (errors ?? Array.Empty<TerrainClusterFootprintCompileError>())
                .Where(value => value != null)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            this.errors = new ReadOnlyCollection<TerrainClusterFootprintCompileError>(copy);
            LocalCanvas = copy.Length == 0 ? localCanvas : null;
        }

        public bool IsSuccess => LocalCanvas != null && errors.Count == 0;
        public TerrainClusterLocalCanvas LocalCanvas { get; }
        public IReadOnlyList<TerrainClusterFootprintCompileError> Errors => errors;
        public IReadOnlyList<CompiledClusterChunkCell> ChunkCells =>
            LocalCanvas == null ? EmptyChunkCells : LocalCanvas.ChunkCells;
        public IReadOnlyList<CompiledClusterLocalTileCell> TileCells =>
            LocalCanvas == null ? EmptyTileCells : LocalCanvas.TileCells;
        public int ChunkMappingCount => LocalCanvas == null ? 0 : LocalCanvas.ChunkCells.Count;
        public int TileMappingCount => LocalCanvas == null ? 0 : LocalCanvas.TileCells.Count;
        public string CanonicalDigest => LocalCanvas == null ? string.Empty : LocalCanvas.CanonicalDigest;
    }
}
