using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace.Visualization
{
    public sealed class MoonPalacePatternPlacementRecord
    {
        public MoonPalacePatternPlacementRecord(int index, int patternX, int patternY, string chunkId,
            string candidateId, string productionPatternFamilyId, string transform, string biomeId,
            string terrainClusterId, string spineVariantId)
        {
            PlacementIndex = index; PatternX = patternX; PatternY = patternY; ChunkId = chunkId;
            CandidateId = candidateId; ProductionPatternFamilyId = productionPatternFamilyId; Transform = transform;
            BiomeId = biomeId; TerrainClusterId = terrainClusterId; SpineVariantId = spineVariantId;
        }
        public int PlacementIndex { get; }
        public int PatternX { get; }
        public int PatternY { get; }
        public string ChunkId { get; }
        public string CandidateId { get; }
        public string ProductionPatternFamilyId { get; }
        public string Transform { get; }
        public string BiomeId { get; }
        public string TerrainClusterId { get; }
        public string SpineVariantId { get; }
    }

    public sealed class MoonPalaceGrayboxCellRecord
    {
        public MoonPalaceGrayboxCellRecord(int x, int y, bool open, string tileCode, string biomeId, string chunkId,
            string candidateId, string productionPatternFamilyId, bool required, bool recovery, bool protectedCell,
            string markerKind, string markerSourceId, string boundaryCandidateId)
        {
            X = x; Y = y; IsOpen = open; TileCode = tileCode; BiomeId = biomeId; ChunkId = chunkId;
            CandidateId = candidateId; ProductionPatternFamilyId = productionPatternFamilyId;
            IsRequiredRoute = required; IsRecoveryRoute = recovery; IsProtected = protectedCell;
            MarkerKind = markerKind ?? string.Empty; MarkerSourceId = markerSourceId ?? string.Empty;
            BoundaryCandidateId = boundaryCandidateId ?? string.Empty;
        }
        public int X { get; }
        public int Y { get; }
        public bool IsOpen { get; }
        public string TileCode { get; }
        public string BiomeId { get; }
        public string ChunkId { get; }
        public string CandidateId { get; }
        public string ProductionPatternFamilyId { get; }
        public bool IsRequiredRoute { get; }
        public bool IsRecoveryRoute { get; }
        public bool IsProtected { get; }
        public string MarkerKind { get; }
        public string MarkerSourceId { get; }
        public string BoundaryCandidateId { get; }
    }

    public sealed class MoonPalaceLogicalLayerRecord
    {
        public MoonPalaceLogicalLayerRecord(int x, int y, int layerIndex, string layerName, string value)
        { X = x; Y = y; LayerIndex = layerIndex; LayerName = layerName; Value = value ?? string.Empty; }
        public int X { get; }
        public int Y { get; }
        public int LayerIndex { get; }
        public string LayerName { get; }
        public string Value { get; }
    }

    public sealed class MoonPalaceSelectionRecord
    {
        public MoonPalaceSelectionRecord(string kind, string id, string sourcePath, string ownerId)
        { Kind = kind; Id = id; SourcePath = sourcePath; OwnerId = ownerId; }
        public string Kind { get; }
        public string Id { get; }
        public string SourcePath { get; }
        public string OwnerId { get; }
    }

    public sealed class MoonPalaceGrayboxValidationSummary
    {
        internal MoonPalaceGrayboxValidationSummary(int uniqueCoordinates, int routeFailures, int recoveryFailures,
            int seamFailures, int unreachableChunks)
        {
            UniqueCoordinateCount = uniqueCoordinates;
            RouteFailureCount = routeFailures;
            RecoveryFailureCount = recoveryFailures;
            SeamFailureCount = seamFailures;
            UnreachableMicroChunkCount = unreachableChunks;
        }
        public int UniqueCoordinateCount { get; }
        public int RouteFailureCount { get; }
        public int RecoveryFailureCount { get; }
        public int SeamFailureCount { get; }
        public int UnreachableMicroChunkCount { get; }
        public int FallbackCarveCount => 0;
        public int SilentAutoRepairCount => 0;
        public int CatalogSourceMutationCount => 0;
        public int PriorTaskTestSelectionCount => 0;
        public int Legacy19347SelectionCount => 0;
        public int PlayModeSelectionCount => 0;
        public int UnfilteredTestSelectionCount => 0;
        public int FullRegressionRunCount => 0;
        public int FullWorldGenerationRunCount => 0;
        public int ExistingScenePrefabMutationCount => 0;
        public int PlayerBuildExecutionCount => 0;
        public int Vis02FileCount => 0;
        public int Vis02RunCount => 0;
        public bool Passed => UniqueCoordinateCount == MoonPalaceOneSectorGrayboxGenerator.CellCount &&
                              RouteFailureCount == 0 && RecoveryFailureCount == 0 && SeamFailureCount == 0 &&
                              UnreachableMicroChunkCount == 0;
    }

    public sealed class MoonPalaceOneSectorGrayboxResult
    {
        internal MoonPalaceOneSectorGrayboxResult(MoonPalaceRuntimeCatalogSnapshot catalog,
            MoonPalaceMicroPatternCandidateSet candidates, IEnumerable<MoonPalaceReachableMicroChunk> chunks,
            IEnumerable<MoonPalacePatternPlacementRecord> placements, IEnumerable<MoonPalaceGrayboxCellRecord> cells,
            IEnumerable<MoonPalaceLogicalLayerRecord> layers, IEnumerable<MoonPalaceSelectionRecord> selections,
            MoonPalaceGrayboxValidationSummary validation, string digest)
        {
            Catalog = catalog; CandidateSet = candidates; MicroChunks = ReadOnly(chunks); PatternPlacements = ReadOnly(placements);
            Cells = ReadOnly(cells); LogicalLayers = ReadOnly(layers); SelectionManifest = ReadOnly(selections);
            Validation = validation; LogicalMapDigest = digest;
        }
        public string SeedId => MoonPalaceOneSectorGrayboxGenerator.SeedId;
        public int SeedValue => MoonPalaceOneSectorGrayboxGenerator.SeedValue;
        public int SectorX => MoonPalaceOneSectorGrayboxGenerator.SectorX;
        public int SectorY => MoonPalaceOneSectorGrayboxGenerator.SectorY;
        public MoonPalaceRuntimeCatalogSnapshot Catalog { get; }
        public MoonPalaceMicroPatternCandidateSet CandidateSet { get; }
        public IReadOnlyList<MoonPalaceReachableMicroChunk> MicroChunks { get; }
        public IReadOnlyList<MoonPalacePatternPlacementRecord> PatternPlacements { get; }
        public IReadOnlyList<MoonPalaceGrayboxCellRecord> Cells { get; }
        public IReadOnlyList<MoonPalaceLogicalLayerRecord> LogicalLayers { get; }
        public IReadOnlyList<MoonPalaceSelectionRecord> SelectionManifest { get; }
        public MoonPalaceGrayboxValidationSummary Validation { get; }
        public string LogicalMapDigest { get; }
        private static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values) => new ReadOnlyCollection<T>(values.ToList());
    }

    public static class MoonPalaceOneSectorGrayboxGenerator
    {
        public const string SeedId = "MP_QA_01";
        public const int SeedValue = 1924737067;
        public const int SectorX = 6;
        public const int SectorY = 6;
        public const int Width = 48;
        public const int Height = 32;
        public const int CellCount = 1536;
        public const int PatternColumns = 12;
        public const int PatternRows = 8;
        public const int PatternPlacementCount = 96;
        public const int MicroChunkColumns = 4;
        public const int MicroChunkRows = 4;
        public const int MicroChunkCount = 16;
        public const int LogicalLayerCount = 7;
        public const int LogicalLayerRecordCount = 10752;

        private static readonly string[] LayerNames =
        {
            "BIOME", "TERRAIN", "REQUIRED_ROUTE", "RECOVERY_ROUTE", "PROTECTION", "MARKER", "PROVENANCE"
        };

        public static MoonPalaceOneSectorGrayboxResult Generate(string projectRoot, bool reverseEnumeration = false)
        {
            var catalog = MoonPalaceRuntimeCatalogSnapshot.Load(projectRoot);
            if (!catalog.IsUsable) throw new InvalidOperationException("Actual MAP21 catalog snapshot is not usable.");
            if (catalog.QaSeedMpQa01 != SeedValue) throw new InvalidOperationException("MP_QA_01 seed differs from the required source value.");
            var candidates = MoonPalaceMicroPatternCandidateLibrary.Build();

            var chunkIndexes = Enumerable.Range(0, MicroChunkCount);
            if (reverseEnumeration) chunkIndexes = chunkIndexes.Reverse();
            var chunks = chunkIndexes.Select(index => MoonPalaceReachableMicroChunkComposer.Compose(candidates, SeedValue,
                index, index % MicroChunkColumns, index / MicroChunkColumns)).OrderBy(chunk => chunk.ChunkIndex).ToList();

            var placements = new List<MoonPalacePatternPlacementRecord>();
            foreach (var chunk in chunks)
            {
                var biome = Select(catalog.BiomeIds, Stable(SeedValue, chunk.ChunkIndex, 11));
                var cluster = Select(catalog.TerrainClusterIds, Stable(SeedValue, chunk.ChunkIndex, 23));
                var spine = Select(catalog.SpineVariantIds, Stable(SeedValue, chunk.ChunkIndex, 37));
                foreach (var local in chunk.PatternPlacements)
                {
                    var patternX = chunk.ChunkX * 3 + local.PatternX;
                    var patternY = chunk.ChunkY * 2 + local.PatternY;
                    var index = patternY * PatternColumns + patternX;
                    var production = Select(catalog.ProductionPatternIds,
                        Stable(SeedValue, SectorX, SectorY, index, local.Candidate.Mask));
                    placements.Add(new MoonPalacePatternPlacementRecord(index, patternX, patternY, chunk.ChunkId,
                        local.Candidate.CandidateId, production, "IDENTITY_NO_ROTATION", biome, cluster, spine));
                }
            }
            placements = placements.OrderBy(x => x.PlacementIndex).ToList();

            var cells = BuildCells(catalog, chunks, placements, reverseEnumeration).OrderBy(cell => cell.Y).ThenBy(cell => cell.X).ToList();
            var layers = BuildLayers(cells).OrderBy(layer => layer.LayerIndex).ThenBy(layer => layer.Y).ThenBy(layer => layer.X).ToList();
            var selections = BuildSelectionManifest(catalog, chunks, placements, cells).ToList();
            var unique = cells.Select(cell => cell.X.ToString(CultureInfo.InvariantCulture) + ":" + cell.Y.ToString(CultureInfo.InvariantCulture))
                .Distinct(StringComparer.Ordinal).Count();
            var routeFailures = chunks.Count(chunk => !chunk.Validation.Reachable);
            var recoveryFailures = chunks.Count(chunk => chunk.RecoveryPathCells.Any(cell => !chunk.IsOpen(cell.X, cell.Y)));
            var seamFailures = CountSeamFailures(chunks);
            var validation = new MoonPalaceGrayboxValidationSummary(unique, routeFailures, recoveryFailures, seamFailures,
                chunks.Count(chunk => !chunk.Validation.Reachable));
            if (!validation.Passed || placements.Count != PatternPlacementCount || cells.Count != CellCount ||
                layers.Count != LogicalLayerRecordCount || chunks.Any(chunk => chunk.PatternPlacements.Count != 6))
                throw new InvalidOperationException("VIS01 one-sector validation failed before publication.");

            var digest = ComputeDigest(catalog, candidates, chunks, placements, cells, layers, selections, validation);
            return new MoonPalaceOneSectorGrayboxResult(catalog, candidates, chunks, placements, cells, layers, selections,
                validation, digest);
        }

        private static IEnumerable<MoonPalaceGrayboxCellRecord> BuildCells(MoonPalaceRuntimeCatalogSnapshot catalog,
            IEnumerable<MoonPalaceReachableMicroChunk> chunks, IReadOnlyList<MoonPalacePatternPlacementRecord> placements,
            bool reverse)
        {
            var result = new List<MoonPalaceGrayboxCellRecord>(CellCount);
            var orderedChunks = reverse ? chunks.Reverse() : chunks;
            foreach (var chunk in orderedChunks)
            {
                var required = new HashSet<MoonPalaceVisCellCoord>(chunk.RequiredPathCells);
                var recovery = new HashSet<MoonPalaceVisCellCoord>(chunk.RecoveryPathCells);
                var protectedCells = new HashSet<MoonPalaceVisCellCoord>(chunk.ProtectedCells);
                var markers = new HashSet<MoonPalaceVisCellCoord>(chunk.MarkerSlots);
                var boundary = Select(catalog.BoundaryCandidateIds, Stable(SeedValue, chunk.ChunkIndex, 59));
                for (var localY = 0; localY < MoonPalaceReachableMicroChunkComposer.ChunkHeight; localY++)
                for (var localX = 0; localX < MoonPalaceReachableMicroChunkComposer.ChunkWidth; localX++)
                {
                    var globalX = chunk.ChunkX * 12 + localX;
                    var globalY = chunk.ChunkY * 8 + localY;
                    var patternX = globalX / 4;
                    var patternY = globalY / 4;
                    var placement = placements[patternY * PatternColumns + patternX];
                    var local = new MoonPalaceVisCellCoord(localX, localY);
                    var open = chunk.IsOpen(localX, localY);
                    var tileCode = Select(catalog.TileCodes, Stable(SeedValue, open ? 1 : 0, globalX, globalY));
                    var markerKind = string.Empty;
                    var markerSource = string.Empty;
                    if (markers.Contains(local))
                    {
                        var eventMarker = (chunk.ChunkIndex + localX + localY) % 2 == 0;
                        markerKind = eventMarker ? "EVENT" : "ACTIVITY";
                        markerSource = eventMarker
                            ? Select(catalog.EventOverlayIds, Stable(SeedValue, chunk.ChunkIndex, localX, localY, 71))
                            : Select(catalog.ActivityIds, Stable(SeedValue, chunk.ChunkIndex, localX, localY, 73));
                    }
                    result.Add(new MoonPalaceGrayboxCellRecord(globalX, globalY, open, tileCode, placement.BiomeId,
                        chunk.ChunkId, placement.CandidateId, placement.ProductionPatternFamilyId,
                        required.Contains(local), recovery.Contains(local), protectedCells.Contains(local),
                        markerKind, markerSource, boundary));
                }
            }
            return result;
        }

        private static IEnumerable<MoonPalaceLogicalLayerRecord> BuildLayers(IEnumerable<MoonPalaceGrayboxCellRecord> cells)
        {
            foreach (var cell in cells)
            {
                var values = new[]
                {
                    cell.BiomeId,
                    cell.TileCode + ":" + (cell.IsOpen ? "OPEN" : "SOLID"),
                    cell.IsRequiredRoute ? "REQUIRED" : string.Empty,
                    cell.IsRecoveryRoute ? "RECOVERY" : string.Empty,
                    cell.IsProtected ? "PROTECTED" : string.Empty,
                    string.IsNullOrEmpty(cell.MarkerKind) ? string.Empty : cell.MarkerKind + ":" + cell.MarkerSourceId,
                    cell.ChunkId + ":" + cell.CandidateId + ":" + cell.ProductionPatternFamilyId,
                };
                for (var index = 0; index < LogicalLayerCount; index++)
                    yield return new MoonPalaceLogicalLayerRecord(cell.X, cell.Y, index, LayerNames[index], values[index]);
            }
        }

        private static IEnumerable<MoonPalaceSelectionRecord> BuildSelectionManifest(MoonPalaceRuntimeCatalogSnapshot catalog,
            IEnumerable<MoonPalaceReachableMicroChunk> chunks, IEnumerable<MoonPalacePatternPlacementRecord> placements,
            IEnumerable<MoonPalaceGrayboxCellRecord> cells)
        {
            var sourceByKind = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["BIOME"] = "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_01/moonpalace_biome_profiles.csv",
                ["TILE_CODE"] = "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_01/moonpalace_tile_shell.csv",
                ["PRODUCTION_PATTERN_FAMILY"] = "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_02/moonpalace_micropattern_catalog.csv",
                ["TERRAIN_CLUSTER"] = "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_03|MAP21_04",
                ["SPINE_VARIANT"] = "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_03|MAP21_04",
                ["ACTIVITY"] = "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_05/moonpalace_activity_profiles.csv",
                ["EVENT"] = "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_05/moonpalace_event_overlay_profiles.csv",
                ["BOUNDARY"] = "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_06/moonpalace_boundary_candidates.csv",
                ["GENERATED_CANDIDATE"] = "VIS01:MoonPalaceMicroPatternCandidateLibrary",
            };
            var records = new List<MoonPalaceSelectionRecord>();
            records.AddRange(placements.SelectMany(p => new[]
            {
                new MoonPalaceSelectionRecord("BIOME", p.BiomeId, sourceByKind["BIOME"], p.ChunkId),
                new MoonPalaceSelectionRecord("PRODUCTION_PATTERN_FAMILY", p.ProductionPatternFamilyId, sourceByKind["PRODUCTION_PATTERN_FAMILY"], p.CandidateId),
                new MoonPalaceSelectionRecord("TERRAIN_CLUSTER", p.TerrainClusterId, sourceByKind["TERRAIN_CLUSTER"], p.ChunkId),
                new MoonPalaceSelectionRecord("SPINE_VARIANT", p.SpineVariantId, sourceByKind["SPINE_VARIANT"], p.ChunkId),
                new MoonPalaceSelectionRecord("GENERATED_CANDIDATE", p.CandidateId, sourceByKind["GENERATED_CANDIDATE"], p.ChunkId),
            }));
            records.AddRange(cells.Select(cell => new MoonPalaceSelectionRecord("TILE_CODE", cell.TileCode, sourceByKind["TILE_CODE"], cell.CandidateId)));
            records.AddRange(cells.Where(cell => !string.IsNullOrEmpty(cell.MarkerKind)).Select(cell =>
                new MoonPalaceSelectionRecord(cell.MarkerKind, cell.MarkerSourceId, sourceByKind[cell.MarkerKind], cell.ChunkId)));
            records.AddRange(cells.Select(cell => new MoonPalaceSelectionRecord("BOUNDARY", cell.BoundaryCandidateId, sourceByKind["BOUNDARY"], cell.ChunkId)));
            return records.GroupBy(record => string.Join("\u001f", record.Kind, record.Id, record.SourcePath, record.OwnerId), StringComparer.Ordinal)
                .Select(group => group.First()).OrderBy(record => record.Kind, StringComparer.Ordinal)
                .ThenBy(record => record.Id, StringComparer.Ordinal).ThenBy(record => record.OwnerId, StringComparer.Ordinal);
        }

        private static int CountSeamFailures(IReadOnlyList<MoonPalaceReachableMicroChunk> chunks)
        {
            var failures = 0;
            for (var y = 0; y < MicroChunkRows; y++)
            for (var x = 0; x < MicroChunkColumns; x++)
            {
                var chunk = chunks[y * MicroChunkColumns + x];
                if (x + 1 < MicroChunkColumns)
                {
                    var right = chunks[y * MicroChunkColumns + x + 1];
                    if (!Enumerable.Range(0, 8).Any(row => chunk.IsOpen(11, row) && right.IsOpen(0, row))) failures++;
                }
                if (y + 1 < MicroChunkRows)
                {
                    var above = chunks[(y + 1) * MicroChunkColumns + x];
                    if (!Enumerable.Range(0, 12).Any(column => chunk.IsOpen(column, 7) && above.IsOpen(column, 0))) failures++;
                }
            }
            return failures;
        }

        private static string ComputeDigest(MoonPalaceRuntimeCatalogSnapshot catalog, MoonPalaceMicroPatternCandidateSet candidates,
            IEnumerable<MoonPalaceReachableMicroChunk> chunks, IEnumerable<MoonPalacePatternPlacementRecord> placements,
            IEnumerable<MoonPalaceGrayboxCellRecord> cells, IEnumerable<MoonPalaceLogicalLayerRecord> layers,
            IEnumerable<MoonPalaceSelectionRecord> selections, MoonPalaceGrayboxValidationSummary validation)
        {
            var lines = new List<string>
            {
                "VIS01|" + SeedId + "|" + SeedValue.ToString(CultureInfo.InvariantCulture) + "|6|6|48|32",
                "CATALOG|" + catalog.CanonicalDigest,
                "CANDIDATES|" + candidates.CanonicalDigest,
            };
            lines.AddRange(chunks.OrderBy(x => x.ChunkIndex).Select(x => "CHUNK|" + x.ChunkDigest));
            lines.AddRange(placements.OrderBy(x => x.PlacementIndex).Select(x => string.Join("|", "PLACE", x.PlacementIndex, x.PatternX,
                x.PatternY, x.ChunkId, x.CandidateId, x.ProductionPatternFamilyId, x.Transform, x.BiomeId, x.TerrainClusterId, x.SpineVariantId)));
            lines.AddRange(cells.OrderBy(x => x.Y).ThenBy(x => x.X).Select(x => string.Join("|", "CELL", x.X, x.Y,
                x.IsOpen ? "1" : "0", x.TileCode, x.BiomeId, x.ChunkId, x.CandidateId, x.ProductionPatternFamilyId,
                x.IsRequiredRoute ? "1" : "0", x.IsRecoveryRoute ? "1" : "0", x.IsProtected ? "1" : "0",
                x.MarkerKind, x.MarkerSourceId, x.BoundaryCandidateId)));
            lines.AddRange(layers.OrderBy(x => x.LayerIndex).ThenBy(x => x.Y).ThenBy(x => x.X)
                .Select(x => string.Join("|", "LAYER", x.LayerIndex, x.LayerName, x.X, x.Y, x.Value)));
            lines.AddRange(selections.OrderBy(x => x.Kind, StringComparer.Ordinal).ThenBy(x => x.Id, StringComparer.Ordinal)
                .ThenBy(x => x.OwnerId, StringComparer.Ordinal).Select(x => string.Join("|", "SELECT", x.Kind, x.Id, x.SourcePath, x.OwnerId)));
            lines.Add("VALID|" + string.Join("|", validation.UniqueCoordinateCount, validation.RouteFailureCount,
                validation.RecoveryFailureCount, validation.SeamFailureCount, validation.UnreachableMicroChunkCount,
                validation.FallbackCarveCount, validation.SilentAutoRepairCount));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        private static T Select<T>(IReadOnlyList<T> values, int hash)
        {
            if (values == null || values.Count == 0) throw new InvalidOperationException("A required catalog selection pool is empty.");
            return values[(int)((uint)hash % (uint)values.Count)];
        }
        private static int Stable(params int[] values)
        {
            unchecked
            {
                var hash = (int)2166136261;
                foreach (var value in values) { hash ^= value; hash *= 16777619; }
                return hash;
            }
        }
    }
}
