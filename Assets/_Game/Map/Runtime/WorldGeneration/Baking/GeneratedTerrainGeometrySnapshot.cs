using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.MicroPatterns;

namespace StarNight.Map.WorldGeneration.Baking
{
    public sealed class GeneratedTerrainGeometrySnapshot
    {
        public const int CanonicalSectorWidth = WorldGenConstants.SectorWidthTiles;
        public const int CanonicalSectorHeight = WorldGenConstants.SectorHeightTiles;
        public const int CanonicalSectorCellCount = CanonicalSectorWidth * CanonicalSectorHeight;
        public const int CanonicalMicroChunkWidth = WorldGenConstants.MicroChunkWidthTiles;
        public const int CanonicalMicroChunkHeight = WorldGenConstants.MicroChunkHeightTiles;
        public const int CanonicalMicroChunkCellCount = CanonicalMicroChunkWidth * CanonicalMicroChunkHeight;
        public const int CanonicalChunkGridWidth = CanonicalSectorWidth / CanonicalMicroChunkWidth;
        public const int CanonicalChunkGridHeight = CanonicalSectorHeight / CanonicalMicroChunkHeight;
        public const int CanonicalChunkCount = CanonicalChunkGridWidth * CanonicalChunkGridHeight;
        public const int CanonicalMicroPatternWidth = MicroPatternDefinition.RequiredWidth;
        public const int CanonicalMicroPatternHeight = MicroPatternDefinition.RequiredHeight;
        public const int CanonicalPatternsPerChunkX = CanonicalMicroChunkWidth / CanonicalMicroPatternWidth;
        public const int CanonicalPatternsPerChunkY = CanonicalMicroChunkHeight / CanonicalMicroPatternHeight;
        public const int CanonicalSectorPatternGridWidth = CanonicalSectorWidth / CanonicalMicroPatternWidth;
        public const int CanonicalSectorPatternGridHeight = CanonicalSectorHeight / CanonicalMicroPatternHeight;
        public const int CanonicalSectorPatternCellCount =
            CanonicalSectorPatternGridWidth * CanonicalSectorPatternGridHeight;
        public const int CanonicalWorldSectorColumns = WorldGenConstants.SectorColumns;
        public const int CanonicalWorldSectorRows = WorldGenConstants.SectorRows;
        public const int CanonicalWorldSectorCount = WorldGenConstants.SectorCount;
        public const int CanonicalWorldWidth = WorldGenConstants.WorldWidthTiles;
        public const int CanonicalWorldHeight = WorldGenConstants.WorldHeightTiles;
        public const int CanonicalWorldCellCount = WorldGenConstants.WorldTileCount;
        public const int CanonicalWorldProjectedSliceCount = CanonicalWorldSectorCount * CanonicalChunkCount;
        public const int CanonicalLayersPerFinalCanvasCell = SectorFinalCanvasLayerPlan.RequiredLayerCount;
        public const int CanonicalSectorLayerRecordCount =
            CanonicalSectorCellCount * CanonicalLayersPerFinalCanvasCell;
        public const bool CanonicalChunkRotationAllowed = SectorPatternChunkPartition.ChunkRotationAllowed;

        private readonly ReadOnlyCollection<string> canonicalLines;

        private GeneratedTerrainGeometrySnapshot()
        {
            SectorWidth = CanonicalSectorWidth;
            SectorHeight = CanonicalSectorHeight;
            SectorCellCount = CanonicalSectorCellCount;
            MicroChunkWidth = CanonicalMicroChunkWidth;
            MicroChunkHeight = CanonicalMicroChunkHeight;
            MicroChunkCellCount = CanonicalMicroChunkCellCount;
            ChunkGridWidth = CanonicalChunkGridWidth;
            ChunkGridHeight = CanonicalChunkGridHeight;
            ChunkCount = CanonicalChunkCount;
            MicroPatternWidth = CanonicalMicroPatternWidth;
            MicroPatternHeight = CanonicalMicroPatternHeight;
            PatternsPerChunkX = CanonicalPatternsPerChunkX;
            PatternsPerChunkY = CanonicalPatternsPerChunkY;
            WorldSectorColumns = CanonicalWorldSectorColumns;
            WorldSectorRows = CanonicalWorldSectorRows;
            WorldSectorCount = CanonicalWorldSectorCount;
            WorldWidth = CanonicalWorldWidth;
            WorldHeight = CanonicalWorldHeight;
            WorldCellCount = CanonicalWorldCellCount;
            WorldProjectedSliceCount = CanonicalWorldProjectedSliceCount;
            LayersPerFinalCanvasCell = CanonicalLayersPerFinalCanvasCell;
            SectorLayerRecordCount = CanonicalSectorLayerRecordCount;
            ChunkRotationAllowed = CanonicalChunkRotationAllowed;
            canonicalLines = new ReadOnlyCollection<string>(new[]
            {
                "SECTOR|" + Number(SectorWidth) + "|" + Number(SectorHeight) + "|" + Number(SectorCellCount),
                "MICRO_CHUNK|" + Number(MicroChunkWidth) + "|" + Number(MicroChunkHeight) + "|" +
                    Number(MicroChunkCellCount),
                "CHUNK_GRID|" + Number(ChunkGridWidth) + "|" + Number(ChunkGridHeight) + "|" + Number(ChunkCount),
                "MICRO_PATTERN|" + Number(MicroPatternWidth) + "|" + Number(MicroPatternHeight) + "|" +
                    Number(PatternsPerChunkX) + "|" + Number(PatternsPerChunkY),
                "WORLD|" + Number(WorldSectorColumns) + "|" + Number(WorldSectorRows) + "|" +
                    Number(WorldSectorCount) + "|" + Number(WorldWidth) + "|" + Number(WorldHeight) + "|" +
                    Number(WorldCellCount) + "|" + Number(WorldProjectedSliceCount),
                "LAYERS|" + Number(LayersPerFinalCanvasCell) + "|" + Number(SectorLayerRecordCount),
                "CHUNK_ROTATION_ALLOWED|" + (ChunkRotationAllowed ? "1" : "0"),
            });
        }

        public int SectorWidth { get; }
        public int SectorHeight { get; }
        public int SectorCellCount { get; }
        public int MicroChunkWidth { get; }
        public int MicroChunkHeight { get; }
        public int MicroChunkCellCount { get; }
        public int ChunkGridWidth { get; }
        public int ChunkGridHeight { get; }
        public int ChunkCount { get; }
        public int MicroPatternWidth { get; }
        public int MicroPatternHeight { get; }
        public int PatternsPerChunkX { get; }
        public int PatternsPerChunkY { get; }
        public int WorldSectorColumns { get; }
        public int WorldSectorRows { get; }
        public int WorldSectorCount { get; }
        public int WorldWidth { get; }
        public int WorldHeight { get; }
        public int WorldCellCount { get; }
        public int WorldProjectedSliceCount { get; }
        public int LayersPerFinalCanvasCell { get; }
        public int SectorLayerRecordCount { get; }
        public bool ChunkRotationAllowed { get; }
        public IReadOnlyList<string> CanonicalLines => canonicalLines;

        public static bool TryCreate(
            out GeneratedTerrainGeometrySnapshot snapshot,
            out IReadOnlyList<string> failures)
        {
            var errors = new List<string>();
            Require(errors, CanonicalSectorWidth > 0 && CanonicalSectorHeight > 0, "SECTOR_DIMENSIONS");
            Require(errors, CanonicalSectorCellCount == CanonicalSectorWidth * CanonicalSectorHeight,
                "SECTOR_CELL_COUNT");
            Require(errors, CanonicalMicroChunkWidth > 0 && CanonicalMicroChunkHeight > 0,
                "MICRO_CHUNK_DIMENSIONS");
            Require(errors, CanonicalSectorWidth % CanonicalMicroChunkWidth == 0 &&
                CanonicalSectorHeight % CanonicalMicroChunkHeight == 0, "CHUNK_GRID_DIVISIBILITY");
            Require(errors, CanonicalMicroChunkWidth % CanonicalMicroPatternWidth == 0 &&
                CanonicalMicroChunkHeight % CanonicalMicroPatternHeight == 0, "PATTERN_GRID_DIVISIBILITY");
            Require(errors, CanonicalWorldWidth == CanonicalWorldSectorColumns * CanonicalSectorWidth &&
                CanonicalWorldHeight == CanonicalWorldSectorRows * CanonicalSectorHeight, "WORLD_DIMENSIONS");
            Require(errors, CanonicalWorldSectorCount == CanonicalWorldSectorColumns * CanonicalWorldSectorRows,
                "WORLD_SECTOR_COUNT");
            Require(errors, CanonicalWorldCellCount == CanonicalWorldWidth * CanonicalWorldHeight,
                "WORLD_CELL_COUNT");
            Require(errors, CanonicalWorldProjectedSliceCount == CanonicalWorldSectorCount * CanonicalChunkCount,
                "WORLD_PROJECTED_SLICE_COUNT");
            Require(errors, CanonicalSectorLayerRecordCount ==
                CanonicalSectorCellCount * CanonicalLayersPerFinalCanvasCell, "SECTOR_LAYER_RECORD_COUNT");
            Require(errors, !CanonicalChunkRotationAllowed, "CHUNK_ROTATION_POLICY");

            var ordered = errors.Distinct(StringComparer.Ordinal).OrderBy(value => value,
                StringComparer.Ordinal).ToArray();
            failures = new ReadOnlyCollection<string>(ordered);
            snapshot = ordered.Length == 0 ? new GeneratedTerrainGeometrySnapshot() : null;
            return snapshot != null;
        }

        private static void Require(ICollection<string> failures, bool condition, string code)
        {
            if (!condition) failures.Add(code);
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
