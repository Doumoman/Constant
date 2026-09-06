using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.MoonPalace;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.TerrainClusters;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor.WorldGeneration.MoonPalace
{
    public static class MoonPalaceMillDoughClusterPoolPublisher
    {
        public const string AuthoringDirectoryRelativePath =
            "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_04";
        public const string GeneratedDirectoryRelativePath =
            "MapDesign/MCP/GENERATED/MAP21_04";
        public const string CatalogCsvFileName =
            "moonpalace_mill_dough_cluster_catalog.csv";
        public const string FootprintCsvFileName =
            "moonpalace_mill_dough_cluster_footprints.csv";
        public const string SpineVariantCsvFileName =
            "moonpalace_mill_dough_cluster_spine_variants.csv";
        public const string PatternSlotCsvFileName =
            "moonpalace_mill_dough_cluster_pattern_slots.csv";
        public const string PoolManifestFileName =
            "moonpalace_mill_dough_cluster_pool_manifest.json";
        public const string SignatureManifestFileName =
            "moonpalace_mill_dough_cluster_signature_manifest.json";
        public const string AllBiomeManifestFileName =
            "moonpalace_all_biome_cluster_pool_manifest.json";
        public const string DigestManifestFileName =
            "moonpalace_mill_dough_cluster_digest_manifest.json";

        public const string SourceMap2102CatalogRelativePath =
            "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_02/moonpalace_micropattern_catalog.csv";
        public const string SourceMap2102CellsRelativePath =
            "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_02/moonpalace_micropattern_cells.csv";
        public const string SourceMap2102TagsRelativePath =
            "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_02/moonpalace_micropattern_tags.csv";
        public const string SourceMap11CatalogRelativePath =
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/TerrainCluster/terrain_cluster_catalog_v2.csv";
        public const string SourceMap2103CatalogRelativePath =
            "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_03/moonpalace_crater_root_cluster_catalog.csv";
        public const string SourceMap2103PoolManifestRelativePath =
            "MapDesign/MCP/GENERATED/MAP21_03/moonpalace_crater_root_cluster_pool_manifest.json";
        public const string SourceMap2103SignatureManifestRelativePath =
            "MapDesign/MCP/GENERATED/MAP21_03/moonpalace_crater_root_cluster_signature_manifest.json";
        public const string SourceMap2103DigestManifestRelativePath =
            "MapDesign/MCP/GENERATED/MAP21_03/moonpalace_crater_root_cluster_digest_manifest.json";
        private const string SourceMap2102DigestManifestRelativePath =
            "MapDesign/MCP/GENERATED/MAP21_02/moonpalace_micropattern_digest_manifest.json";
        private const string SourceMap2103ResultRelativePath =
            "MapDesign/MCP/REPORTS/MAP21_03_EXPAND_CRATER_AND_ROOT_CLUSTER_POOLS_RESULT.md";
        private const string SourceMap2103TaskRelativePath =
            "MapDesign/MCP/TASKS/MAP21_03_EXPAND_CRATER_AND_ROOT_CLUSTER_POOLS.md";

        public static MoonPalaceMillDoughClusterPoolPublishedSample CreateReadOnlySample(
            string projectRoot, string createdUtc, bool reverseInputOrder = false)
        {
            projectRoot = RequireProjectRoot(projectRoot);
            ValidateUpstreamPreconditions(projectRoot);
            var patternCatalogRows = ReadRows(projectRoot, SourceMap2102CatalogRelativePath);
            var patternCellRows = ReadRows(projectRoot, SourceMap2102CellsRelativePath);
            var patternTagRows = ReadRows(projectRoot, SourceMap2102TagsRelativePath);
            var starterClusterRows = ReadRows(projectRoot, SourceMap11CatalogRelativePath);
            var craterRootCatalogRows = ReadRows(projectRoot,
                SourceMap2103CatalogRelativePath);
            var specs = CreateSpecs().ToList();
            if (reverseInputOrder)
            {
                patternCatalogRows.Reverse();
                patternCellRows.Reverse();
                patternTagRows.Reverse();
                starterClusterRows.Reverse();
                craterRootCatalogRows.Reverse();
                specs.Reverse();
            }

            var patternBiomeById = patternCatalogRows.ToDictionary(
                row => Value(row, "pattern_id"), row => Value(row, "biome_id"),
                StringComparer.Ordinal);
            var requiredMillDoughPatternIds = MoonPalaceMicroPatternProduction.RequiredPatternIds
                .Where(id => patternBiomeById.TryGetValue(id, out var biome) &&
                    (biome == "AbandonedMill" || biome == "MoonDough"))
                .OrderBy(id => id, StringComparer.Ordinal).ToArray();
            if (requiredMillDoughPatternIds.Length != 12)
                throw new InvalidDataException("MAP21_02 Mill/Dough pattern inventory mismatch.");
            var patternFamilies = patternCatalogRows.ToDictionary(
                row => Value(row, "pattern_id"), row => Value(row, "silhouette_family"),
                StringComparer.Ordinal);
            var operationSummaries = patternCellRows.GroupBy(row => Value(row, "pattern_id"),
                    StringComparer.Ordinal).ToDictionary(group => group.Key,
                    group => string.Join("|", group.GroupBy(row => Value(row,
                            "source_operation"), StringComparer.Ordinal)
                        .OrderBy(item => item.Key, StringComparer.Ordinal)
                        .Select(item => item.Key + ":" + item.Count().ToString(
                            CultureInfo.InvariantCulture))), StringComparer.Ordinal);
            var patternSignatureTokens = requiredMillDoughPatternIds.ToDictionary(id => id,
                id => Pack(patternFamilies[id], operationSummaries[id]),
                StringComparer.Ordinal);
            var hazardPatternIds = new HashSet<string>(patternTagRows.Where(row =>
                    Value(row, "tag_kind") == "Hazard").Select(row => Value(row, "pattern_id")),
                StringComparer.Ordinal);
            var forbiddenBufferMarkerPatterns = new HashSet<string>(patternTagRows.Where(row =>
                    Value(row, "tag_kind") == "Marker" && IsForbiddenBufferMarker(
                        Value(row, "tag_token")))
                .Select(row => Value(row, "pattern_id")), StringComparer.Ordinal);
            ValidateMap11Sources(specs, starterClusterRows);
            var craterRootCatalog = ParseCraterRootCatalog(craterRootCatalogRows);

            var catalog = new List<MoonPalaceClusterCatalogRecord>();
            var footprints = new List<MoonPalaceClusterFootprintRecord>();
            var spines = new List<MoonPalaceClusterSpineVariantRecord>();
            var slots = new List<MoonPalaceClusterPatternSlotRecord>();
            foreach (var spec in specs)
            {
                var clusterFootprints = CreateFootprints(spec).ToArray();
                var clusterSpines = CreateSpines(spec).ToArray();
                var clusterSlots = CreateSlots(spec, patternBiomeById, hazardPatternIds,
                    forbiddenBufferMarkerPatterns).ToArray();
                var baseline = clusterSpines.Single(value => value.IsBaseline);
                footprints.AddRange(clusterFootprints);
                spines.AddRange(clusterSpines);
                slots.AddRange(clusterSlots);
                catalog.Add(new MoonPalaceClusterCatalogRecord(spec.ClusterId, spec.BiomeId,
                    spec.PoolKind, spec.PacingRole, spec.AccessClass,
                    clusterFootprints.Length, spec.SourceStarterClusterId,
                    MoonPalaceMillDoughClusterPoolPreconditions.SourcePatternCatalogDigest,
                    MoonPalaceClusterPoolProduction.ComputeFootprintDigest(clusterFootprints),
                    MoonPalaceClusterPoolProduction.ComputeSpineVariantDigest(clusterSpines),
                    MoonPalaceClusterPoolProduction.ComputePatternSlotDigest(clusterSlots),
                    StructuralSignature(clusterFootprints, baseline),
                    SilhouetteSignature(clusterSlots, patternSignatureTokens),
                    spec.RepeatGroup));
            }

            var production = new MoonPalaceMillDoughClusterPoolProduction(catalog, footprints,
                spines, slots, hazardPatternIds, forbiddenBufferMarkerPatterns,
                craterRootCatalog, createdUtc);
            var digestManifest = new MoonPalaceMillDoughClusterDigestManifest(production,
                createdUtc);
            return new MoonPalaceMillDoughClusterPoolPublishedSample(production,
                digestManifest, MoonPalaceMillDoughClusterPoolForbiddenOperationCounters.Zero,
                Array.Empty<string>());
        }

        public static MoonPalaceMillDoughClusterPoolPublishedSample PublishAuthoringAndSamples(
            string projectRoot, bool focusedMap2104Pass)
        {
            if (!focusedMap2104Pass)
                throw new InvalidOperationException(
                    "MAP21_05 handoff publication requires focused MAP21_04 PASS.");
            projectRoot = RequireProjectRoot(projectRoot);
            var sample = CreateReadOnlySample(projectRoot,
                DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(Resolve(projectRoot, AuthoringDirectoryRelativePath));
            Directory.CreateDirectory(Resolve(projectRoot, GeneratedDirectoryRelativePath));
            var relativePaths = new[]
            {
                CombineRelative(AuthoringDirectoryRelativePath, CatalogCsvFileName),
                CombineRelative(AuthoringDirectoryRelativePath, FootprintCsvFileName),
                CombineRelative(AuthoringDirectoryRelativePath, SpineVariantCsvFileName),
                CombineRelative(AuthoringDirectoryRelativePath, PatternSlotCsvFileName),
                CombineRelative(GeneratedDirectoryRelativePath, PoolManifestFileName),
                CombineRelative(GeneratedDirectoryRelativePath, SignatureManifestFileName),
                CombineRelative(GeneratedDirectoryRelativePath, AllBiomeManifestFileName),
                CombineRelative(GeneratedDirectoryRelativePath, DigestManifestFileName),
            };
            Write(projectRoot, relativePaths[0], sample.Production.SerializeCatalogCsv());
            Write(projectRoot, relativePaths[1], sample.Production.SerializeFootprintCsv());
            Write(projectRoot, relativePaths[2], sample.Production.SerializeSpineVariantCsv());
            Write(projectRoot, relativePaths[3], sample.Production.SerializePatternSlotCsv());
            Write(projectRoot, relativePaths[4], sample.Production.SerializePoolManifest());
            Write(projectRoot, relativePaths[5], sample.Production.SerializeSignatureManifest());
            Write(projectRoot, relativePaths[6], sample.Production.SerializeAllBiomeManifest());
            Write(projectRoot, relativePaths[7], sample.DigestManifest.Serialize());
            return new MoonPalaceMillDoughClusterPoolPublishedSample(sample.Production,
                sample.DigestManifest, sample.Counters, relativePaths);
        }

        private static IEnumerable<MoonPalaceClusterFootprintRecord> CreateFootprints(
            MillDoughClusterSpec spec)
        {
            for (var index = 0; index < spec.Coordinates.Length; index++)
            {
                var coordinate = spec.Coordinates[index];
                yield return new MoonPalaceClusterFootprintRecord(spec.ClusterId,
                    coordinate.X, coordinate.Y, Role(index, spec.Coordinates.Length),
                    spec.EntrySide, spec.ExitSide, index == 0,
                    index == spec.Coordinates.Length - 1);
            }
        }

        private static IEnumerable<MoonPalaceClusterSpineVariantRecord> CreateSpines(
            MillDoughClusterSpec spec)
        {
            var baselineMovements = spec.BiomeId == "AbandonedMill"
                ? new[] { TraversalMovementKind.Walk, TraversalMovementKind.Jump }
                : new[] { TraversalMovementKind.Walk, TraversalMovementKind.Jump };
            var alternateMovements = spec.PoolKind == MoonPalaceClusterPoolKind.Buffer
                ? new[] { TraversalMovementKind.Walk, TraversalMovementKind.Drop }
                : spec.BiomeId == "AbandonedMill"
                    ? new[] { TraversalMovementKind.Jump, TraversalMovementKind.Drop }
                    : new[] { TraversalMovementKind.Jump, TraversalMovementKind.Walk };
            yield return new MoonPalaceClusterSpineVariantRecord(spec.ClusterId,
                spec.ClusterId + "_BASE", true, spec.EntrySide, spec.ExitSide,
                spec.RouteIntent + ".Baseline", baselineMovements, "BaselineProtected",
                spec.PoolKind == MoonPalaceClusterPoolKind.Quiet ? "QuietStable" :
                    "StaticRecoveryAvailable", "PreserveSpineAndEntryExit");
            yield return new MoonPalaceClusterSpineVariantRecord(spec.ClusterId,
                spec.ClusterId + "_ALT", false, spec.EntrySide, spec.ExitSide,
                spec.RouteIntent + ".Alternate", alternateMovements, "AuthoredAlternative",
                "StaticRecoveryAvailable", "PreserveSpineAndEntryExit");
        }

        private static IEnumerable<MoonPalaceClusterPatternSlotRecord> CreateSlots(
            MillDoughClusterSpec spec, IReadOnlyDictionary<string, string> patternBiomeById,
            ISet<string> hazards, ISet<string> forbiddenBufferMarkers)
        {
            var all = patternBiomeById.Where(pair => pair.Value == spec.BiomeId)
                .Select(pair => pair.Key).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var candidates = spec.PoolKind == MoonPalaceClusterPoolKind.Terrain
                ? all
                : all.Where(pattern => !hazards.Contains(pattern) &&
                    !forbiddenBufferMarkers.Contains(pattern)).ToArray();
            var take = spec.PoolKind == MoonPalaceClusterPoolKind.Quiet ? 2 : 3;
            for (var index = 0; index < spec.Coordinates.Length; index++)
            {
                var coordinate = spec.Coordinates[index];
                var kind = spec.PoolKind == MoonPalaceClusterPoolKind.Quiet
                    ? MoonPalaceClusterPatternSlotKind.BaselineRequired
                    : spec.PoolKind == MoonPalaceClusterPoolKind.Buffer
                        ? MoonPalaceClusterPatternSlotKind.RecoveryCue
                        : index == 0
                            ? MoonPalaceClusterPatternSlotKind.BaselineRequired
                            : MoonPalaceClusterPatternSlotKind.OptionalSilhouette;
                yield return new MoonPalaceClusterPatternSlotRecord(spec.ClusterId,
                    spec.ClusterId + "_SLOT_" + index.ToString("D2",
                        CultureInfo.InvariantCulture), coordinate.X, coordinate.Y, kind,
                    RotateTake(candidates, spec.Ordinal + index, Math.Min(take,
                        candidates.Length)), spec.BiomeId, "PreserveSpineAndEntryExit",
                    spec.PoolKind == MoonPalaceClusterPoolKind.Quiet ? 0 :
                        spec.PoolKind == MoonPalaceClusterPoolKind.Buffer ? 10 : 35,
                    patternBiomeById);
            }
        }

        private static IReadOnlyList<string> RotateTake(string[] values, int offset, int count)
        {
            if (values.Length == 0) throw new InvalidDataException("Empty pattern candidates.");
            var result = new List<string>();
            for (var index = 0; index < count; index++)
                result.Add(values[(offset + index) % values.Length]);
            return result;
        }

        private static string StructuralSignature(
            IEnumerable<MoonPalaceClusterFootprintRecord> footprint,
            MoonPalaceClusterSpineVariantRecord baseline)
        {
            var ordered = footprint.OrderBy(value => value.ChunkY).ThenBy(value => value.ChunkX);
            return BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP21_03_STRUCTURAL_SIGNATURE_V1", baseline.EntrySide.ToString(),
                baseline.ExitSide.ToString(), baseline.RouteIntent,
                string.Join("|", baseline.MovementTokens.Select(value => value.ToString())),
            }.Concat(ordered.Select(value => Pack(
                value.ChunkX.ToString(CultureInfo.InvariantCulture),
                value.ChunkY.ToString(CultureInfo.InvariantCulture),
                value.ChunkRole.ToString()))));
        }

        private static string SilhouetteSignature(
            IEnumerable<MoonPalaceClusterPatternSlotRecord> slots,
            IReadOnlyDictionary<string, string> patternSignatureTokens) =>
            BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP21_03_SILHOUETTE_SIGNATURE_V1",
            }.Concat(slots.OrderBy(value => value.SlotId, StringComparer.Ordinal).Select(value =>
                Pack(value.CoordinateToken, value.SlotKind.ToString(),
                    string.Join("|", value.AllowedPatternIds.Select(patternId =>
                        patternSignatureTokens[patternId]))))));

        private static ClusterRoleKind Role(int index, int count)
        {
            if (index == 0) return ClusterRoleKind.Entry;
            if (index == count - 1) return ClusterRoleKind.Exit;
            if (count >= 4 && index == 1) return ClusterRoleKind.BuildUp;
            if (count == 5 && index == 3) return ClusterRoleKind.Recovery;
            return ClusterRoleKind.Core;
        }

        private static string Pack(params string[] values) => string.Join("/",
            (values ?? Array.Empty<string>()).Select(value =>
            {
                var text = value ?? string.Empty;
                return text.Length.ToString(CultureInfo.InvariantCulture) + ":" + text;
            }));

        private static void ValidateMap11Sources(IEnumerable<MillDoughClusterSpec> specs,
            IEnumerable<IReadOnlyDictionary<string, string>> starterRows)
        {
            var starters = starterRows.Where(row => Value(row, "biome_id") ==
                    "AbandonedMill" || Value(row, "biome_id") == "MoonDough")
                .ToDictionary(row => Value(row, "cluster_id"), row => Value(row, "biome_id"),
                    StringComparer.Ordinal);
            var mapped = specs.Where(spec => spec.SourceStarterClusterId != "MissingData")
                .ToArray();
            if (mapped.Length != 8 || mapped.Any(spec =>
                    !starters.TryGetValue(spec.SourceStarterClusterId, out var biome) ||
                    biome != spec.BiomeId))
                throw new InvalidDataException("MAP11 Mill/Dough source mapping mismatch.");
        }

        private static bool IsForbiddenBufferMarker(string token)
        {
            var upper = (token ?? string.Empty).ToUpperInvariant();
            return new[] { "REWARD", "BOSS", "VILLAGE", "FORGE", "SHOP",
                "REQUIRED_RESOURCE" }.Any(upper.Contains);
        }

        private static MillDoughClusterSpec[] CreateSpecs() => new[]
        {
            Spec(0, "TC_MILL_PROD_BEAM_OVERHANG", "AbandonedMill", MoonPalaceClusterPoolKind.Terrain,
                PacingRole.Traversal, "TC_MILL_BEAM_OVERHANG", "0,0;1,0;2,0",
                ClusterPortSide.L, ClusterPortSide.R, "MillBeamOverhang", "MillTerrain"),
            Spec(1, "TC_MILL_PROD_BROKEN_PILLAR", "AbandonedMill", MoonPalaceClusterPoolKind.Terrain,
                PacingRole.Discovery, "TC_MILL_BROKEN_PILLAR", "0,0;0,1;1,1;1,2",
                ClusterPortSide.D, ClusterPortSide.U, "MillBrokenPillar", "MillTerrain"),
            Spec(2, "TC_MILL_PROD_ORTHOGONAL_SHAFT", "AbandonedMill", MoonPalaceClusterPoolKind.Terrain,
                PacingRole.Traversal, "TC_MILL_ORTHOGONAL_SHAFT_RECOVERY", "0,0;1,0;1,1;2,1",
                ClusterPortSide.L, ClusterPortSide.R, "MillOrthogonalShaft", "MillTerrain"),
            Spec(3, "TC_MILL_PROD_GEAR_GALLERY", "AbandonedMill", MoonPalaceClusterPoolKind.Terrain,
                PacingRole.Risk, "MissingData", "0,1;1,1;1,0;2,0;3,0",
                ClusterPortSide.L, ClusterPortSide.R, "MillGearGallery", "MillTerrain"),
            Spec(4, "TC_MILL_PROD_RUST_LEDGE", "AbandonedMill", MoonPalaceClusterPoolKind.Terrain,
                PacingRole.Discovery, "MissingData", "0,0;1,0;2,0;2,1",
                ClusterPortSide.L, ClusterPortSide.U, "MillRustLedge", "MillTerrain"),
            Spec(5, "TC_MILL_PROD_FALL_RECOVERY", "AbandonedMill", MoonPalaceClusterPoolKind.Terrain,
                PacingRole.Recovery, "MissingData", "0,1;1,1;2,1;1,0;1,2",
                ClusterPortSide.U, ClusterPortSide.L, "MillFallRecovery", "MillTerrain"),
            Spec(6, "TC_MILL_QUIET_BEAM_WALK", "AbandonedMill", MoonPalaceClusterPoolKind.Quiet,
                PacingRole.Quiet, "TC_MILL_QUIET_BEAM", "0,0;1,0",
                ClusterPortSide.L, ClusterPortSide.R, "MillQuietBeamWalk", "MillQuiet"),
            Spec(7, "TC_MILL_QUIET_RUST_BALCONY", "AbandonedMill", MoonPalaceClusterPoolKind.Quiet,
                PacingRole.Quiet, "MissingData", "0,0;0,1",
                ClusterPortSide.D, ClusterPortSide.U, "MillQuietRustBalcony", "MillQuiet"),
            Spec(8, "TC_MILL_QUIET_GEAR_SHADOW", "AbandonedMill", MoonPalaceClusterPoolKind.Quiet,
                PacingRole.Quiet, "MissingData", "0,0;1,0",
                ClusterPortSide.R, ClusterPortSide.L, "MillQuietGearShadow", "MillQuiet"),
            Spec(9, "TC_MILL_BUFFER_ENTRY_BEAM", "AbandonedMill", MoonPalaceClusterPoolKind.Buffer,
                PacingRole.Safe, "MissingData", "0,0;1,0",
                ClusterPortSide.L, ClusterPortSide.R, "MillBufferEntryBeam", "MillBuffer"),
            Spec(10, "TC_MILL_BUFFER_EXIT_SHAFT", "AbandonedMill", MoonPalaceClusterPoolKind.Buffer,
                PacingRole.Safe, "MissingData", "0,0;0,1;0,2",
                ClusterPortSide.D, ClusterPortSide.U, "MillBufferExitShaft", "MillBuffer"),
            Spec(11, "TC_MILL_BUFFER_RECOVERY_PLATFORM", "AbandonedMill", MoonPalaceClusterPoolKind.Buffer,
                PacingRole.Recovery, "MissingData", "0,0;1,0;1,1",
                ClusterPortSide.L, ClusterPortSide.U, "MillBufferRecoveryPlatform", "MillBuffer"),
            Spec(12, "TC_DOUGH_PROD_BOUNCE_CUP", "MoonDough", MoonPalaceClusterPoolKind.Terrain,
                PacingRole.Traversal, "TC_DOUGH_BOUNCE_CUP", "0,0;1,0;2,0",
                ClusterPortSide.L, ClusterPortSide.R, "DoughBounceCup", "DoughTerrain"),
            Spec(13, "TC_DOUGH_PROD_SOFT_POCKET", "MoonDough", MoonPalaceClusterPoolKind.Terrain,
                PacingRole.Discovery, "TC_DOUGH_SOFT_POCKET", "0,0;1,0;1,1;2,1",
                ClusterPortSide.L, ClusterPortSide.R, "DoughSoftPocket", "DoughTerrain"),
            Spec(14, "TC_DOUGH_PROD_STICKY_SHELF", "MoonDough", MoonPalaceClusterPoolKind.Terrain,
                PacingRole.Traversal, "TC_DOUGH_STICKY_RISE_RECOVERY", "0,0;0,1;1,1;2,1",
                ClusterPortSide.D, ClusterPortSide.R, "DoughStickyShelf", "DoughTerrain"),
            Spec(15, "TC_DOUGH_PROD_FERMENT_RISE", "MoonDough", MoonPalaceClusterPoolKind.Terrain,
                PacingRole.Risk, "MissingData", "0,0;0,1;0,2;1,2;2,2",
                ClusterPortSide.D, ClusterPortSide.R, "DoughFermentRise", "DoughTerrain"),
            Spec(16, "TC_DOUGH_PROD_RECOVERY_PAD_CHAIN", "MoonDough", MoonPalaceClusterPoolKind.Terrain,
                PacingRole.Recovery, "MissingData", "0,0;1,0;2,0;2,1",
                ClusterPortSide.L, ClusterPortSide.U, "DoughRecoveryPadChain", "DoughTerrain"),
            Spec(17, "TC_DOUGH_PROD_SQUISH_CROSS", "MoonDough", MoonPalaceClusterPoolKind.Terrain,
                PacingRole.Discovery, "MissingData", "0,1;1,1;2,1;1,0;1,2",
                ClusterPortSide.U, ClusterPortSide.D, "DoughSquishCross", "DoughTerrain"),
            Spec(18, "TC_DOUGH_QUIET_SOFT_SHELF", "MoonDough", MoonPalaceClusterPoolKind.Quiet,
                PacingRole.Quiet, "TC_DOUGH_QUIET_SHELF", "0,0;1,0",
                ClusterPortSide.L, ClusterPortSide.R, "DoughQuietSoftShelf", "DoughQuiet"),
            Spec(19, "TC_DOUGH_QUIET_FERMENT_POCKET", "MoonDough", MoonPalaceClusterPoolKind.Quiet,
                PacingRole.Quiet, "MissingData", "0,0;0,1",
                ClusterPortSide.D, ClusterPortSide.U, "DoughQuietFermentPocket", "DoughQuiet"),
            Spec(20, "TC_DOUGH_QUIET_RECOVERY_PAD", "MoonDough", MoonPalaceClusterPoolKind.Quiet,
                PacingRole.Quiet, "MissingData", "0,0;1,0",
                ClusterPortSide.R, ClusterPortSide.L, "DoughQuietRecoveryPad", "DoughQuiet"),
            Spec(21, "TC_DOUGH_BUFFER_ENTRY_CUP", "MoonDough", MoonPalaceClusterPoolKind.Buffer,
                PacingRole.Safe, "MissingData", "0,0;1,0",
                ClusterPortSide.L, ClusterPortSide.R, "DoughBufferEntryCup", "DoughBuffer"),
            Spec(22, "TC_DOUGH_BUFFER_EXIT_SHELF", "MoonDough", MoonPalaceClusterPoolKind.Buffer,
                PacingRole.Safe, "MissingData", "0,0;1,0;2,0",
                ClusterPortSide.L, ClusterPortSide.R, "DoughBufferExitShelf", "DoughBuffer"),
            Spec(23, "TC_DOUGH_BUFFER_RECOVERY_BOWL", "MoonDough", MoonPalaceClusterPoolKind.Buffer,
                PacingRole.Recovery, "MissingData", "0,0;0,1;1,1",
                ClusterPortSide.D, ClusterPortSide.R, "DoughBufferRecoveryBowl", "DoughBuffer"),
        };

        private static MillDoughClusterSpec Spec(int ordinal, string clusterId, string biomeId,
            MoonPalaceClusterPoolKind poolKind, PacingRole pacingRole,
            string sourceStarterClusterId, string coordinateToken, ClusterPortSide entrySide,
            ClusterPortSide exitSide, string routeIntent, string repeatGroup) =>
            new MillDoughClusterSpec(ordinal, clusterId, biomeId, poolKind, pacingRole,
                AccessClass.MandatoryNoTool, sourceStarterClusterId,
                coordinateToken.Split(';').Select(token =>
                {
                    var parts = token.Split(',');
                    return new MillDoughSpecCoordinate(int.Parse(parts[0],
                        CultureInfo.InvariantCulture), int.Parse(parts[1],
                        CultureInfo.InvariantCulture));
                }).ToArray(), entrySide, exitSide, routeIntent, repeatGroup);

        private static IReadOnlyList<MoonPalaceClusterCatalogRecord> ParseCraterRootCatalog(
            IEnumerable<IReadOnlyDictionary<string, string>> rows)
        {
            var output = new List<MoonPalaceClusterCatalogRecord>();
            foreach (var row in rows)
            {
                if (!Enum.TryParse(Value(row, "pool_kind"), out MoonPalaceClusterPoolKind pool) ||
                    !PacingRoleTokenCodec.TryParse(Value(row, "pacing_role"), out var pacing) ||
                    !AccessClassTokenCodec.TryParse(Value(row, "access_class"), out var access))
                    throw new InvalidDataException("MAP21_03 catalog token mismatch.");
                var value = new MoonPalaceClusterCatalogRecord(Value(row, "cluster_id"),
                    Value(row, "biome_id"), pool, pacing, access,
                    int.Parse(Value(row, "active_chunk_count"), CultureInfo.InvariantCulture),
                    Value(row, "source_starter_cluster_id"),
                    Value(row, "source_pattern_pool_digest"), Value(row, "footprint_digest"),
                    Value(row, "spine_variant_digest"), Value(row, "pattern_slot_digest"),
                    Value(row, "structural_signature"), Value(row, "silhouette_signature"),
                    Value(row, "repeat_group"));
                if (value.CanonicalDigest != Value(row, "canonical_digest"))
                    throw new InvalidDataException("MAP21_03 catalog canonical digest mismatch.");
                output.Add(value);
            }
            return output;
        }

        private static List<IReadOnlyDictionary<string, string>> ReadRows(
            string projectRoot, string relativePath)
        {
            var read = new Rfc4180CsvReader().Read(File.ReadAllBytes(
                Resolve(projectRoot, relativePath)), relativePath);
            if (!read.Success || read.Records.Count < 2)
                throw new InvalidDataException("RFC4180 CSV read failed: " + relativePath);
            var headers = read.Records[0].Fields.Select(field => field.Value.Trim()).ToArray();
            if (headers.Any(value => value.Length == 0) ||
                headers.Distinct(StringComparer.Ordinal).Count() != headers.Length)
                throw new InvalidDataException("CSV headers must be unique and non-empty.");
            var rows = new List<IReadOnlyDictionary<string, string>>();
            foreach (var record in read.Records.Skip(1))
            {
                if (record.Fields.Count != headers.Length)
                    throw new InvalidDataException("CSV row width mismatch: " + relativePath);
                var row = new Dictionary<string, string>(StringComparer.Ordinal);
                for (var index = 0; index < headers.Length; index++)
                    row.Add(headers[index], record.Fields[index].Value.Trim());
                rows.Add(row);
            }
            return rows;
        }

        private static string Value(IReadOnlyDictionary<string, string> row, string field)
        {
            if (!row.TryGetValue(field, out var value) || string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException("Missing/empty CSV field: " + field);
            return value;
        }

        private static void ValidateUpstreamPreconditions(string projectRoot)
        {
            if (FileSha256(Resolve(projectRoot, SourceMap2103ResultRelativePath)) !=
                MoonPalaceMillDoughClusterPoolPreconditions.SourceMap2103ResultDigest)
                throw new InvalidDataException("MAP21_03 Result SHA-256 mismatch.");
            if (FileSha256(Resolve(projectRoot, SourceMap2103TaskRelativePath)) !=
                MoonPalaceMillDoughClusterPoolPreconditions.SourceMap2103TaskDigest)
                throw new InvalidDataException("MAP21_03 Task SHA-256 mismatch.");

            var map2102 = JsonUtility.FromJson<SourceMap2102DigestDocument>(File.ReadAllText(
                Resolve(projectRoot, SourceMap2102DigestManifestRelativePath)));
            if (map2102 == null || map2102.pattern_catalog_digest !=
                    MoonPalaceMillDoughClusterPoolPreconditions.SourcePatternCatalogDigest ||
                map2102.pattern_cell_digest !=
                    MoonPalaceMillDoughClusterPoolPreconditions.SourcePatternCellDigest ||
                map2102.pattern_tag_digest !=
                    MoonPalaceMillDoughClusterPoolPreconditions.SourcePatternTagDigest)
                throw new InvalidDataException("MAP21_02 pattern digest chain mismatch.");

            var map2103 = JsonUtility.FromJson<SourceMap2103DigestDocument>(File.ReadAllText(
                Resolve(projectRoot, SourceMap2103DigestManifestRelativePath)));
            if (map2103 == null || map2103.cluster_catalog_digest !=
                    MoonPalaceMillDoughClusterPoolPreconditions.SourceCraterRootCatalogDigest ||
                map2103.cluster_footprint_digest !=
                    MoonPalaceMillDoughClusterPoolPreconditions.SourceCraterRootFootprintDigest ||
                map2103.cluster_spine_variant_digest !=
                    MoonPalaceMillDoughClusterPoolPreconditions.SourceCraterRootSpineVariantDigest ||
                map2103.cluster_pattern_slot_digest !=
                    MoonPalaceMillDoughClusterPoolPreconditions.SourceCraterRootPatternSlotDigest ||
                map2103.cluster_signature_digest !=
                    MoonPalaceMillDoughClusterPoolPreconditions.SourceCraterRootSignatureDigest ||
                map2103.MAP21_04_handoff_digest !=
                    MoonPalaceMillDoughClusterPoolPreconditions.SourceMap2104HandoffDigest)
                throw new InvalidDataException("MAP21_03 cluster digest chain mismatch.");

            var pool = JsonUtility.FromJson<SourceMap2103PoolDocument>(File.ReadAllText(
                Resolve(projectRoot, SourceMap2103PoolManifestRelativePath)));
            var signatures = JsonUtility.FromJson<SourceMap2103SignatureDocument>(
                File.ReadAllText(Resolve(projectRoot,
                    SourceMap2103SignatureManifestRelativePath)));
            if (pool == null || signatures == null || pool.cluster_catalog_digest !=
                    MoonPalaceMillDoughClusterPoolPreconditions.SourceCraterRootCatalogDigest ||
                pool.cluster_footprint_digest !=
                    MoonPalaceMillDoughClusterPoolPreconditions.SourceCraterRootFootprintDigest ||
                pool.cluster_spine_variant_digest !=
                    MoonPalaceMillDoughClusterPoolPreconditions.SourceCraterRootSpineVariantDigest ||
                pool.cluster_pattern_slot_digest !=
                    MoonPalaceMillDoughClusterPoolPreconditions.SourceCraterRootPatternSlotDigest ||
                signatures.cluster_signature_digest !=
                    MoonPalaceMillDoughClusterPoolPreconditions.SourceCraterRootSignatureDigest)
                throw new InvalidDataException("MAP21_03 read-only artifact mismatch.");
        }

        private static string CombineRelative(string directory, string fileName) =>
            directory.TrimEnd('/') + "/" + fileName;

        private static void Write(string projectRoot, string relativePath, string content) =>
            File.WriteAllText(Resolve(projectRoot, relativePath), content,
                BakingCanonicalDigest.Utf8NoBomEncoding);

        private static string RequireProjectRoot(string projectRoot)
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new ArgumentException("Project root is required.", nameof(projectRoot));
            return Path.GetFullPath(projectRoot);
        }

        private static string Resolve(string projectRoot, string relativePath) =>
            Path.GetFullPath(Path.Combine(RequireProjectRoot(projectRoot),
                relativePath.Replace('/', Path.DirectorySeparatorChar)));

        private static string FileSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(stream);
                var output = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes)
                    output.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return output.ToString();
            }
        }
    }

    public sealed class MoonPalaceMillDoughClusterPoolPublishedSample
    {
        private readonly ReadOnlyCollection<string> writtenRelativePaths;

        public MoonPalaceMillDoughClusterPoolPublishedSample(
            MoonPalaceMillDoughClusterPoolProduction production,
            MoonPalaceMillDoughClusterDigestManifest digestManifest,
            MoonPalaceMillDoughClusterPoolForbiddenOperationCounters counters,
            IEnumerable<string> sourceWrittenRelativePaths)
        {
            Production = production ?? throw new ArgumentNullException(nameof(production));
            DigestManifest = digestManifest ?? throw new ArgumentNullException(
                nameof(digestManifest));
            Counters = counters ?? throw new ArgumentNullException(nameof(counters));
            writtenRelativePaths = new ReadOnlyCollection<string>((sourceWrittenRelativePaths ??
                Array.Empty<string>()).OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        public MoonPalaceMillDoughClusterPoolProduction Production { get; }
        public MoonPalaceMillDoughClusterDigestManifest DigestManifest { get; }
        public MoonPalaceMillDoughClusterPoolForbiddenOperationCounters Counters { get; }
        public IReadOnlyList<string> WrittenRelativePaths => writtenRelativePaths;
    }

    public sealed class MoonPalaceMillDoughClusterPoolForbiddenOperationCounters
    {
        public static MoonPalaceMillDoughClusterPoolForbiddenOperationCounters Zero =>
            new MoonPalaceMillDoughClusterPoolForbiddenOperationCounters();
        public int GenerationExecutions => 0;
        public int RendererExecutions => 0;
        public int ValidationRunnerExecutions => 0;
        public int ReplayExecutions => 0;
        public int RollbackExecutions => 0;
        public int PlayModeSelections => 0;
        public int LegacyRegressionSelections => 0;
        public int PriorCategorySelections => 0;
        public int UnfilteredOrFullRegressionRuns => 0;
        public int RuntimeObjectSpawns => 0;
        public int CraterRootClustersAuthored => 0;
        public int BufferLandmarkSearchExecutions => 0;
        public int CsvWritesOutsideMap2104 => 0;
        public bool AllZero => GenerationExecutions == 0 && RendererExecutions == 0 &&
            ValidationRunnerExecutions == 0 && ReplayExecutions == 0 &&
            RollbackExecutions == 0 && PlayModeSelections == 0 &&
            LegacyRegressionSelections == 0 && PriorCategorySelections == 0 &&
            UnfilteredOrFullRegressionRuns == 0 && RuntimeObjectSpawns == 0 &&
            CraterRootClustersAuthored == 0 && BufferLandmarkSearchExecutions == 0 &&
            CsvWritesOutsideMap2104 == 0;
    }

    internal sealed class MillDoughClusterSpec
    {
        public MillDoughClusterSpec(int ordinal, string clusterId, string biomeId,
            MoonPalaceClusterPoolKind poolKind, PacingRole pacingRole,
            AccessClass accessClass, string sourceStarterClusterId,
            MillDoughSpecCoordinate[] coordinates, ClusterPortSide entrySide,
            ClusterPortSide exitSide, string routeIntent, string repeatGroup)
        {
            Ordinal = ordinal;
            ClusterId = clusterId;
            BiomeId = biomeId;
            PoolKind = poolKind;
            PacingRole = pacingRole;
            AccessClass = accessClass;
            SourceStarterClusterId = sourceStarterClusterId;
            Coordinates = coordinates;
            EntrySide = entrySide;
            ExitSide = exitSide;
            RouteIntent = routeIntent;
            RepeatGroup = repeatGroup;
        }

        public int Ordinal { get; }
        public string ClusterId { get; }
        public string BiomeId { get; }
        public MoonPalaceClusterPoolKind PoolKind { get; }
        public PacingRole PacingRole { get; }
        public AccessClass AccessClass { get; }
        public string SourceStarterClusterId { get; }
        public MillDoughSpecCoordinate[] Coordinates { get; }
        public ClusterPortSide EntrySide { get; }
        public ClusterPortSide ExitSide { get; }
        public string RouteIntent { get; }
        public string RepeatGroup { get; }
    }

    internal readonly struct MillDoughSpecCoordinate
    {
        public MillDoughSpecCoordinate(int x, int y) { X = x; Y = y; }
        public int X { get; }
        public int Y { get; }
    }

    [Serializable]
    internal sealed class SourceMap2103DigestDocument
    {
        public string cluster_catalog_digest;
        public string cluster_footprint_digest;
        public string cluster_spine_variant_digest;
        public string cluster_pattern_slot_digest;
        public string cluster_signature_digest;
        public string MAP21_04_handoff_digest;
    }

    [Serializable]
    internal sealed class SourceMap2103PoolDocument
    {
        public string cluster_catalog_digest;
        public string cluster_footprint_digest;
        public string cluster_spine_variant_digest;
        public string cluster_pattern_slot_digest;
    }

    [Serializable]
    internal sealed class SourceMap2103SignatureDocument
    {
        public string cluster_signature_digest;
    }
}
