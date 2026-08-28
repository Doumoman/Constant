using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.TerrainClusters
{
    public static class TerrainClusterFootprintCompiler
    {
        public const string RulesetVersion = "MAP11_01_CLUSTER_FOOTPRINT_LOCAL_CANVAS_V1";
        private const string SourceFootprintRulesetVersion = "MAP11_01_SOURCE_FOOTPRINT_V1";

        public static TerrainClusterFootprintCompileResult Compile(
            TerrainClusterFootprintCompileRequest request)
        {
            var errors = new List<TerrainClusterFootprintCompileError>();
            if (request == null)
            {
                Add(errors, TerrainClusterFootprintCompileErrorCode.MissingInput,
                    "request", "Compile request is required.");
                return Failure(errors);
            }

            if (request.SourceContract == null)
            {
                Add(errors, TerrainClusterFootprintCompileErrorCode.MissingInput,
                    "request.sourceContract", "Source TerrainCluster contract is required.");
            }

            if (!ClusterFootprintTransformUtility.IsSupported(request.Transform))
            {
                Add(errors, TerrainClusterFootprintCompileErrorCode.InvalidTransform,
                    "request.transform", Number((int)request.Transform));
            }

            TerrainClusterValidationResult sourceValidation = null;
            if (request.SourceContract != null)
            {
                sourceValidation = TerrainClusterContractValidator.Validate(
                    request.SourceContract,
                    request.SixChunkAllowlist);
                foreach (var sourceError in sourceValidation.Errors)
                {
                    var code = sourceError.Code == TerrainClusterValidationErrorCode.SixChunkNotAllowlisted
                        ? TerrainClusterFootprintCompileErrorCode.SixChunkNotAllowlisted
                        : TerrainClusterFootprintCompileErrorCode.InvalidSourceFootprint;
                    Add(
                        errors,
                        code,
                        "source." + sourceError.Path,
                        sourceError.Code + ":" + sourceError.Detail);
                }
            }

            if (errors.Count != 0)
            {
                return Failure(errors);
            }

            var sourceChunks = sourceValidation.Contract.Footprint.ActiveChunks
                .OrderBy(value => value)
                .ToArray();
            int chunkWidth;
            int chunkHeight;
            int tileWidth;
            int tileHeight;
            if (!TryComputeBounds(
                    sourceChunks,
                    out chunkWidth,
                    out chunkHeight,
                    out tileWidth,
                    out tileHeight))
            {
                Add(errors, TerrainClusterFootprintCompileErrorCode.InvalidChunkBounds,
                    "source.footprint.activeChunks", "Normalized bounds overflow or are empty.");
                return Failure(errors);
            }

            var compiledActive = new HashSet<ClusterChunkCoord>();
            foreach (var sourceChunk in sourceChunks)
            {
                var compiledChunk = ClusterFootprintTransformUtility.Apply(
                    sourceChunk,
                    chunkWidth,
                    chunkHeight,
                    request.Transform);
                var roundTrip = ClusterFootprintTransformUtility.Apply(
                    compiledChunk,
                    chunkWidth,
                    chunkHeight,
                    request.Transform);
                if (!compiledActive.Add(compiledChunk) || roundTrip != sourceChunk)
                {
                    Add(errors, TerrainClusterFootprintCompileErrorCode.TransformMappingMismatch,
                        ChunkPath(sourceChunk), "Chunk transform is not a unique involution.");
                }
            }

            if (compiledActive.Count != 0 &&
                ReachableChunks(compiledActive).Count != compiledActive.Count)
            {
                Add(errors, TerrainClusterFootprintCompileErrorCode.DisconnectedCompiledFootprint,
                    "compiled.activeChunks", "Transformed active chunks are not 4-neighbor connected.");
            }

            var chunkCells = BuildChunkCells(
                compiledActive,
                chunkWidth,
                chunkHeight,
                request.Transform);
            ValidateChunkPublication(
                chunkCells,
                sourceChunks.Length,
                chunkWidth,
                chunkHeight,
                errors);

            var tileCells = BuildTileCells(
                compiledActive,
                chunkWidth,
                chunkHeight,
                tileWidth,
                tileHeight,
                request.Transform,
                errors);
            ValidateTilePublication(
                tileCells,
                sourceChunks.Length,
                tileWidth,
                tileHeight,
                errors);

            if (errors.Count != 0)
            {
                return Failure(errors);
            }

            var sourceFootprintDigest = ComputeSourceFootprintDigest(
                sourceValidation.Contract.Id,
                sourceChunks);
            var canonicalDigest = ComputeCompiledDigest(
                sourceValidation.Contract.Id,
                sourceFootprintDigest,
                sourceChunks,
                request.Transform,
                chunkWidth,
                chunkHeight,
                tileWidth,
                tileHeight,
                chunkCells,
                tileCells);
            var canvas = new TerrainClusterLocalCanvas(
                sourceValidation.Contract.Id,
                sourceFootprintDigest,
                request.Transform,
                chunkWidth,
                chunkHeight,
                tileWidth,
                tileHeight,
                chunkCells,
                tileCells,
                canonicalDigest);
            return new TerrainClusterFootprintCompileResult(canvas, errors);
        }

        private static bool TryComputeBounds(
            IReadOnlyList<ClusterChunkCoord> sourceChunks,
            out int chunkWidth,
            out int chunkHeight,
            out int tileWidth,
            out int tileHeight)
        {
            chunkWidth = 0;
            chunkHeight = 0;
            tileWidth = 0;
            tileHeight = 0;
            if (sourceChunks == null || sourceChunks.Count == 0) return false;

            var width = (long)sourceChunks.Max(value => value.X) + 1L;
            var height = (long)sourceChunks.Max(value => value.Y) + 1L;
            var tilesWide = width * WorldGenConstants.MicroChunkWidthTiles;
            var tilesHigh = height * WorldGenConstants.MicroChunkHeightTiles;
            if (width <= 0L || height <= 0L ||
                width > int.MaxValue || height > int.MaxValue ||
                tilesWide > int.MaxValue || tilesHigh > int.MaxValue)
            {
                return false;
            }

            chunkWidth = (int)width;
            chunkHeight = (int)height;
            tileWidth = (int)tilesWide;
            tileHeight = (int)tilesHigh;
            return true;
        }

        private static List<CompiledClusterChunkCell> BuildChunkCells(
            ISet<ClusterChunkCoord> compiledActive,
            int chunkWidth,
            int chunkHeight,
            ClusterFootprintTransform transform)
        {
            var cells = new List<CompiledClusterChunkCell>(chunkWidth * chunkHeight);
            for (var y = 0; y < chunkHeight; y++)
            {
                for (var x = 0; x < chunkWidth; x++)
                {
                    var coordinate = new ClusterChunkCoord(x, y);
                    var sourceCoordinate = ClusterFootprintTransformUtility.Apply(
                        coordinate,
                        chunkWidth,
                        chunkHeight,
                        transform);
                    cells.Add(new CompiledClusterChunkCell(
                        coordinate,
                        compiledActive.Contains(coordinate)
                            ? ClusterChunkMaskState.Active
                            : ClusterChunkMaskState.Inactive,
                        sourceCoordinate,
                        y * chunkWidth + x));
                }
            }

            return cells;
        }

        private static List<CompiledClusterLocalTileCell> BuildTileCells(
            ISet<ClusterChunkCoord> compiledActive,
            int chunkWidth,
            int chunkHeight,
            int tileWidth,
            int tileHeight,
            ClusterFootprintTransform transform,
            ICollection<TerrainClusterFootprintCompileError> errors)
        {
            var cells = new List<CompiledClusterLocalTileCell>(tileWidth * tileHeight);
            for (var y = 0; y < tileHeight; y++)
            {
                for (var x = 0; x < tileWidth; x++)
                {
                    var coordinate = new LocalTileCoord(x, y);
                    var owningChunk = new ClusterChunkCoord(
                        x / WorldGenConstants.MicroChunkWidthTiles,
                        y / WorldGenConstants.MicroChunkHeightTiles);
                    var withinChunk = new LocalTileCoord(
                        x % WorldGenConstants.MicroChunkWidthTiles,
                        y % WorldGenConstants.MicroChunkHeightTiles);
                    var sourceCoordinate = ClusterFootprintTransformUtility.Apply(
                        coordinate,
                        tileWidth,
                        tileHeight,
                        transform);
                    var sourceChunk = new ClusterChunkCoord(
                        sourceCoordinate.X / WorldGenConstants.MicroChunkWidthTiles,
                        sourceCoordinate.Y / WorldGenConstants.MicroChunkHeightTiles);
                    var expectedSourceChunk = ClusterFootprintTransformUtility.Apply(
                        owningChunk,
                        chunkWidth,
                        chunkHeight,
                        transform);
                    if (sourceChunk != expectedSourceChunk)
                    {
                        Add(errors, TerrainClusterFootprintCompileErrorCode.TileChunkMappingMismatch,
                            TilePath(coordinate), "Tile and owning-chunk transforms disagree.");
                    }

                    var roundTrip = ClusterFootprintTransformUtility.Apply(
                        sourceCoordinate,
                        tileWidth,
                        tileHeight,
                        transform);
                    if (roundTrip != coordinate)
                    {
                        Add(errors, TerrainClusterFootprintCompileErrorCode.TransformMappingMismatch,
                            TilePath(coordinate), "Tile transform is not an involution.");
                    }

                    cells.Add(new CompiledClusterLocalTileCell(
                        coordinate,
                        owningChunk,
                        withinChunk,
                        compiledActive.Contains(owningChunk)
                            ? ClusterChunkMaskState.Active
                            : ClusterChunkMaskState.Inactive,
                        sourceChunk,
                        sourceCoordinate,
                        y * tileWidth + x));
                }
            }

            return cells;
        }

        private static void ValidateChunkPublication(
            IReadOnlyList<CompiledClusterChunkCell> cells,
            int expectedActiveCount,
            int chunkWidth,
            int chunkHeight,
            ICollection<TerrainClusterFootprintCompileError> errors)
        {
            var expectedCount = chunkWidth * chunkHeight;
            var coordinates = new HashSet<ClusterChunkCoord>();
            var sources = new HashSet<ClusterChunkCoord>();
            for (var index = 0; index < cells.Count; index++)
            {
                var cell = cells[index];
                if (!coordinates.Add(cell.Coordinate) || !sources.Add(cell.SourceCoordinate))
                {
                    Add(errors, TerrainClusterFootprintCompileErrorCode.MissingOrDuplicateChunkCell,
                        ChunkPath(cell.Coordinate), "Chunk coordinate or mapping occurs more than once.");
                }

                if (cell.CanonicalIndex != index ||
                    cell.Coordinate.X != index % chunkWidth ||
                    cell.Coordinate.Y != index / chunkWidth)
                {
                    Add(errors, TerrainClusterFootprintCompileErrorCode.NonCanonicalPublication,
                        ChunkPath(cell.Coordinate), "Chunk cells must publish in canonical (y,x) order.");
                }
            }

            if (cells.Count != expectedCount || coordinates.Count != expectedCount || sources.Count != expectedCount)
            {
                Add(errors, TerrainClusterFootprintCompileErrorCode.MissingOrDuplicateChunkCell,
                    "compiled.chunkCells", CountDetail(cells.Count, expectedCount));
            }

            var activeCount = cells.Count(value => value.State == ClusterChunkMaskState.Active);
            var inactiveCount = cells.Count(value => value.State == ClusterChunkMaskState.Inactive);
            if (activeCount != expectedActiveCount || inactiveCount != expectedCount - expectedActiveCount)
            {
                Add(errors, TerrainClusterFootprintCompileErrorCode.ChunkMaskCountMismatch,
                    "compiled.chunkCells", "active=" + Number(activeCount) +
                    ",inactive=" + Number(inactiveCount) + ",total=" + Number(expectedCount));
            }
        }

        private static void ValidateTilePublication(
            IReadOnlyList<CompiledClusterLocalTileCell> cells,
            int activeChunkCount,
            int tileWidth,
            int tileHeight,
            ICollection<TerrainClusterFootprintCompileError> errors)
        {
            var expectedCount = tileWidth * tileHeight;
            var coordinates = new HashSet<LocalTileCoord>();
            var sources = new HashSet<LocalTileCoord>();
            for (var index = 0; index < cells.Count; index++)
            {
                var cell = cells[index];
                if (!coordinates.Add(cell.Coordinate) || !sources.Add(cell.SourceCoordinate))
                {
                    Add(errors, TerrainClusterFootprintCompileErrorCode.MissingOrDuplicateTileCell,
                        TilePath(cell.Coordinate), "Tile coordinate or mapping occurs more than once.");
                }

                if (cell.CanonicalIndex != index ||
                    cell.Coordinate.X != index % tileWidth ||
                    cell.Coordinate.Y != index / tileWidth)
                {
                    Add(errors, TerrainClusterFootprintCompileErrorCode.NonCanonicalPublication,
                        TilePath(cell.Coordinate), "Tile cells must publish in canonical index order.");
                }
            }

            if (cells.Count != expectedCount || coordinates.Count != expectedCount || sources.Count != expectedCount)
            {
                Add(errors, TerrainClusterFootprintCompileErrorCode.MissingOrDuplicateTileCell,
                    "compiled.tileCells", CountDetail(cells.Count, expectedCount));
            }

            var expectedActive = activeChunkCount * WorldGenConstants.TilesPerMicroChunk;
            var activeCount = cells.Count(value => value.State == ClusterChunkMaskState.Active);
            var inactiveCount = cells.Count(value => value.State == ClusterChunkMaskState.Inactive);
            if (activeCount != expectedActive || inactiveCount != expectedCount - expectedActive)
            {
                Add(errors, TerrainClusterFootprintCompileErrorCode.TileMaskCountMismatch,
                    "compiled.tileCells", "active=" + Number(activeCount) +
                    ",inactive=" + Number(inactiveCount) + ",total=" + Number(expectedCount));
            }
        }

        private static HashSet<ClusterChunkCoord> ReachableChunks(ISet<ClusterChunkCoord> active)
        {
            var reachable = new HashSet<ClusterChunkCoord>();
            if (active.Count == 0) return reachable;
            var queue = new Queue<ClusterChunkCoord>();
            var first = active.OrderBy(value => value).First();
            reachable.Add(first);
            queue.Enqueue(first);
            while (queue.Count != 0)
            {
                var current = queue.Dequeue();
                var neighbors = new[]
                {
                    new ClusterChunkCoord(current.X - 1, current.Y),
                    new ClusterChunkCoord(current.X + 1, current.Y),
                    new ClusterChunkCoord(current.X, current.Y - 1),
                    new ClusterChunkCoord(current.X, current.Y + 1),
                };
                foreach (var neighbor in neighbors)
                {
                    if (active.Contains(neighbor) && reachable.Add(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return reachable;
        }

        private static string ComputeSourceFootprintDigest(
            TerrainClusterId clusterId,
            IEnumerable<ClusterChunkCoord> sourceChunks)
        {
            var material = new StringBuilder();
            Append(material, "RULESET", SourceFootprintRulesetVersion);
            Append(material, "CLUSTER_ID", clusterId.Value);
            Append(material, "MICRO_CHUNK_SIZE",
                Number(WorldGenConstants.MicroChunkWidthTiles),
                Number(WorldGenConstants.MicroChunkHeightTiles));
            foreach (var chunk in sourceChunks.OrderBy(value => value))
            {
                Append(material, "SOURCE_CHUNK", Number(chunk.X), Number(chunk.Y));
            }

            return Hash(material);
        }

        private static string ComputeCompiledDigest(
            TerrainClusterId clusterId,
            string sourceFootprintDigest,
            IEnumerable<ClusterChunkCoord> sourceChunks,
            ClusterFootprintTransform transform,
            int chunkWidth,
            int chunkHeight,
            int tileWidth,
            int tileHeight,
            IEnumerable<CompiledClusterChunkCell> chunkCells,
            IEnumerable<CompiledClusterLocalTileCell> tileCells)
        {
            var material = new StringBuilder();
            Append(material, "RULESET", RulesetVersion);
            Append(material, "CLUSTER_ID", clusterId.Value);
            Append(material, "SOURCE_FOOTPRINT_DIGEST", sourceFootprintDigest);
            foreach (var chunk in sourceChunks.OrderBy(value => value))
            {
                Append(material, "SOURCE_CHUNK", Number(chunk.X), Number(chunk.Y));
            }

            Append(material, "TRANSFORM", Number((int)transform));
            Append(material, "CHUNK_BOUNDS", Number(chunkWidth), Number(chunkHeight));
            Append(material, "TILE_BOUNDS", Number(tileWidth), Number(tileHeight));
            foreach (var cell in chunkCells.OrderBy(value => value.CanonicalIndex))
            {
                Append(material, "CHUNK_CELL",
                    Number(cell.CanonicalIndex), Coordinate(cell.Coordinate),
                    Number((int)cell.State), Coordinate(cell.SourceCoordinate));
                Append(material, "SOURCE_TO_COMPILED_CHUNK",
                    Coordinate(cell.SourceCoordinate), Coordinate(cell.Coordinate));
                Append(material, "COMPILED_TO_SOURCE_CHUNK",
                    Coordinate(cell.Coordinate), Coordinate(cell.SourceCoordinate));
            }

            foreach (var cell in tileCells.OrderBy(value => value.CanonicalIndex))
            {
                Append(material, "TILE_CELL",
                    Number(cell.CanonicalIndex), Coordinate(cell.Coordinate),
                    Coordinate(cell.OwningChunk), Coordinate(cell.WithinChunkCoordinate),
                    Number((int)cell.State), Coordinate(cell.SourceChunkCoordinate),
                    Coordinate(cell.SourceCoordinate));
                Append(material, "SOURCE_TO_COMPILED_TILE",
                    Coordinate(cell.SourceCoordinate), Coordinate(cell.Coordinate));
                Append(material, "COMPILED_TO_SOURCE_TILE",
                    Coordinate(cell.Coordinate), Coordinate(cell.SourceCoordinate));
            }

            return Hash(material);
        }

        private static TerrainClusterFootprintCompileResult Failure(
            IEnumerable<TerrainClusterFootprintCompileError> errors)
        {
            return new TerrainClusterFootprintCompileResult(null, errors);
        }

        private static void Add(
            ICollection<TerrainClusterFootprintCompileError> errors,
            TerrainClusterFootprintCompileErrorCode code,
            string path,
            string detail)
        {
            errors.Add(new TerrainClusterFootprintCompileError(code, path, detail));
        }

        private static string CountDetail(int actual, int expected)
        {
            return "actual=" + Number(actual) + ",expected=" + Number(expected);
        }

        private static string ChunkPath(ClusterChunkCoord coordinate)
        {
            return "chunk[" + Coordinate(coordinate) + "]";
        }

        private static string TilePath(LocalTileCoord coordinate)
        {
            return "tile[" + Coordinate(coordinate) + "]";
        }

        private static string Coordinate(ClusterChunkCoord coordinate)
        {
            return Number(coordinate.X) + "," + Number(coordinate.Y);
        }

        private static string Coordinate(LocalTileCoord coordinate)
        {
            return Number(coordinate.X) + "," + Number(coordinate.Y);
        }

        private static string Number(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static void Append(StringBuilder material, params string[] fields)
        {
            if (material.Length != 0) material.Append('\n');
            material.Append(string.Join("|", fields));
        }

        private static string Hash(StringBuilder material)
        {
            var bytes = Encoding.UTF8.GetBytes(material.ToString());
            using (var sha256 = SHA256.Create())
            {
                return string.Concat(sha256.ComputeHash(bytes).Select(value => value.ToString("x2")));
            }
        }
    }
}
