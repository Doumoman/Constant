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
    public static class MoonPalaceClusterPoolPublisher
    {
        public const string AuthoringDirectoryRelativePath =
            "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_03";
        public const string GeneratedDirectoryRelativePath =
            "MapDesign/MCP/GENERATED/MAP21_03";
        public const string CatalogCsvFileName =
            "moonpalace_crater_root_cluster_catalog.csv";
        public const string FootprintCsvFileName =
            "moonpalace_crater_root_cluster_footprints.csv";
        public const string SpineVariantCsvFileName =
            "moonpalace_crater_root_cluster_spine_variants.csv";
        public const string PatternSlotCsvFileName =
            "moonpalace_crater_root_cluster_pattern_slots.csv";
        public const string PoolManifestFileName =
            "moonpalace_crater_root_cluster_pool_manifest.json";
        public const string SignatureManifestFileName =
            "moonpalace_crater_root_cluster_signature_manifest.json";
        public const string DigestManifestFileName =
            "moonpalace_crater_root_cluster_digest_manifest.json";

        public const string SourceMap2102CatalogRelativePath =
            "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_02/moonpalace_micropattern_catalog.csv";
        public const string SourceMap2102CellsRelativePath =
            "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_02/moonpalace_micropattern_cells.csv";
        public const string SourceMap2102TagsRelativePath =
            "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_02/moonpalace_micropattern_tags.csv";
        public const string SourceMap11CatalogRelativePath =
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/TerrainCluster/terrain_cluster_catalog_v2.csv";
        private const string SourceMap2102DigestManifestRelativePath =
            "MapDesign/MCP/GENERATED/MAP21_02/moonpalace_micropattern_digest_manifest.json";
        private const string SourceMap2102ResultRelativePath =
            "MapDesign/MCP/REPORTS/MAP21_02_PRODUCTIONIZE_24_MICROPATTERNS_RESULT.md";
        private const string SourceMap2102TaskRelativePath =
            "MapDesign/MCP/TASKS/MAP21_02_PRODUCTIONIZE_24_MICROPATTERNS.md";

        public static MoonPalaceClusterPoolPublishedSample CreateReadOnlySample(
            string projectRoot, string createdUtc, bool reverseInputOrder = false)
        {
            projectRoot = RequireProjectRoot(projectRoot);
            ValidateUpstreamPreconditions(projectRoot);
            var patternCatalogRows = ReadRows(projectRoot, SourceMap2102CatalogRelativePath);
            var patternCellRows = ReadRows(projectRoot, SourceMap2102CellsRelativePath);
            var patternTagRows = ReadRows(projectRoot, SourceMap2102TagsRelativePath);
            var starterClusterRows = ReadRows(projectRoot, SourceMap11CatalogRelativePath);
            var specs = CreateSpecs().ToList();
            if (reverseInputOrder)
            {
                patternCatalogRows.Reverse();
                patternCellRows.Reverse();
                patternTagRows.Reverse();
                starterClusterRows.Reverse();
                specs.Reverse();
            }

            var patternBiomeById = patternCatalogRows.ToDictionary(
                row => Value(row, "pattern_id"), row => Value(row, "biome_id"),
                StringComparer.Ordinal);
            var requiredCraterRootPatternIds = MoonPalaceMicroPatternProduction.RequiredPatternIds
                .Where(id => patternBiomeById.TryGetValue(id, out var biome) &&
                    (biome == "MoonCrater" || biome == "CassiaRoot"))
                .OrderBy(id => id, StringComparer.Ordinal).ToArray();
            if (requiredCraterRootPatternIds.Length != 12)
                throw new InvalidDataException("MAP21_02 Crater/Root pattern inventory mismatch.");
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
            var patternSignatureTokens = requiredCraterRootPatternIds.ToDictionary(id => id,
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
                var structuralSignature = StructuralSignature(clusterFootprints, baseline);
                var silhouetteSignature = SilhouetteSignature(clusterSlots,
                    patternSignatureTokens);
                footprints.AddRange(clusterFootprints);
                spines.AddRange(clusterSpines);
                slots.AddRange(clusterSlots);
                catalog.Add(new MoonPalaceClusterCatalogRecord(spec.ClusterId, spec.BiomeId,
                    spec.PoolKind, spec.PacingRole, spec.AccessClass,
                    clusterFootprints.Length, spec.SourceStarterClusterId,
                    MoonPalaceClusterPoolPreconditions.SourcePatternCatalogDigest,
                    MoonPalaceClusterPoolProduction.ComputeFootprintDigest(clusterFootprints),
                    MoonPalaceClusterPoolProduction.ComputeSpineVariantDigest(clusterSpines),
                    MoonPalaceClusterPoolProduction.ComputePatternSlotDigest(clusterSlots),
                    structuralSignature, silhouetteSignature, spec.RepeatGroup));
            }

            var production = new MoonPalaceClusterPoolProduction(catalog, footprints, spines,
                slots, hazardPatternIds, forbiddenBufferMarkerPatterns, createdUtc);
            var map2104Handoff = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP21_04_MOONPALACE_CLUSTER_POOL_HANDOFF_V1",
                MoonPalaceClusterPoolPreconditions.SourceMap2103HandoffDigest,
                production.ClusterCatalogDigest, production.ClusterFootprintDigest,
                production.ClusterSpineVariantDigest, production.ClusterPatternSlotDigest,
                production.ClusterSignatureDigest,
            });
            var digestManifest = new MoonPalaceClusterPoolDigestManifest(production,
                map2104Handoff, createdUtc);
            return new MoonPalaceClusterPoolPublishedSample(production, digestManifest,
                MoonPalaceClusterPoolForbiddenOperationCounters.Zero, Array.Empty<string>());
        }

        public static MoonPalaceClusterPoolPublishedSample PublishAuthoringAndSamples(
            string projectRoot, bool focusedMap2103Pass)
        {
            if (!focusedMap2103Pass)
                throw new InvalidOperationException(
                    "MAP21_04 handoff publication requires focused MAP21_03 PASS.");
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
                CombineRelative(GeneratedDirectoryRelativePath, DigestManifestFileName),
            };
            Write(projectRoot, relativePaths[0], sample.Production.SerializeCatalogCsv());
            Write(projectRoot, relativePaths[1], sample.Production.SerializeFootprintCsv());
            Write(projectRoot, relativePaths[2], sample.Production.SerializeSpineVariantCsv());
            Write(projectRoot, relativePaths[3], sample.Production.SerializePatternSlotCsv());
            Write(projectRoot, relativePaths[4], sample.Production.SerializePoolManifest());
            Write(projectRoot, relativePaths[5], sample.Production.SerializeSignatureManifest());
            Write(projectRoot, relativePaths[6], sample.DigestManifest.Serialize());
            return new MoonPalaceClusterPoolPublishedSample(sample.Production,
                sample.DigestManifest, sample.Counters, relativePaths);
        }

        private static IEnumerable<MoonPalaceClusterFootprintRecord> CreateFootprints(
            ClusterSpec spec)
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
            ClusterSpec spec)
        {
            var baselineMovements = spec.BiomeId == "CassiaRoot"
                ? new[] { TraversalMovementKind.Walk, TraversalMovementKind.Climb }
                : new[] { TraversalMovementKind.Walk, TraversalMovementKind.Jump };
            var alternateMovements = spec.PoolKind == MoonPalaceClusterPoolKind.Buffer
                ? new[] { TraversalMovementKind.Walk, TraversalMovementKind.Drop }
                : spec.BiomeId == "CassiaRoot"
                    ? new[] { TraversalMovementKind.Climb, TraversalMovementKind.Jump }
                    : new[] { TraversalMovementKind.Jump, TraversalMovementKind.Drop };
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
            ClusterSpec spec, IReadOnlyDictionary<string, string> patternBiomeById,
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

        private static void ValidateMap11Sources(IEnumerable<ClusterSpec> specs,
            IEnumerable<IReadOnlyDictionary<string, string>> starterRows)
        {
            var starters = starterRows.Where(row => Value(row, "biome_id") == "MoonCrater" ||
                    Value(row, "biome_id") == "CassiaRoot")
                .ToDictionary(row => Value(row, "cluster_id"), row => Value(row, "biome_id"),
                    StringComparer.Ordinal);
            var mapped = specs.Where(spec => spec.SourceStarterClusterId != "MissingData")
                .ToArray();
            if (mapped.Length != 8 || mapped.Any(spec =>
                    !starters.TryGetValue(spec.SourceStarterClusterId, out var biome) ||
                    biome != spec.BiomeId))
                throw new InvalidDataException("MAP11 Crater/Root source mapping mismatch.");
        }

        private static bool IsForbiddenBufferMarker(string token)
        {
            var upper = (token ?? string.Empty).ToUpperInvariant();
            return new[] { "REWARD", "BOSS", "VILLAGE", "FORGE", "SHOP",
                "REQUIRED_RESOURCE" }.Any(upper.Contains);
        }

        private static ClusterSpec[] CreateSpecs() => new[]
        {
            Spec(0, "TC_CRATER_PROD_RIM_ASCENT", "MoonCrater", MoonPalaceClusterPoolKind.Terrain,
                PacingRole.Traversal, "TC_CRATER_BOWL_ASCENT", "0,0;1,0;2,0", ClusterPortSide.L,
                ClusterPortSide.R, "CraterRimAscent", "CraterTerrain"),
            Spec(1, "TC_CRATER_PROD_BROKEN_SLOPE", "MoonCrater", MoonPalaceClusterPoolKind.Terrain,
                PacingRole.Discovery, "TC_CRATER_BROKEN_SLOPE", "0,0;1,0;2,0;3,0",
                ClusterPortSide.U, ClusterPortSide.D, "CraterBrokenSlopeDrop", "CraterTerrain"),
            Spec(2, "TC_CRATER_PROD_BOWL_CROSS", "MoonCrater", MoonPalaceClusterPoolKind.Terrain,
                PacingRole.Traversal, "MissingData", "0,0;1,0;1,1;2,1", ClusterPortSide.L,
                ClusterPortSide.R, "CraterBowlCross", "CraterTerrain"),
            Spec(3, "TC_CRATER_PROD_ROCK_SHELF_CHAIN", "MoonCrater", MoonPalaceClusterPoolKind.Terrain,
                PacingRole.Recovery, "TC_CRATER_ROCK_SHELF_RECOVERY", "0,0;1,0;1,1;2,1;3,1",
                ClusterPortSide.L, ClusterPortSide.D, "CraterRockShelfChain", "CraterTerrain"),
            Spec(4, "TC_CRATER_PROD_HIGH_LEDGE", "MoonCrater", MoonPalaceClusterPoolKind.Terrain,
                PacingRole.Risk, "MissingData", "0,0;0,1;1,1;1,2", ClusterPortSide.D,
                ClusterPortSide.U, "CraterHighLedge", "CraterTerrain"),
            Spec(5, "TC_CRATER_PROD_FALL_RECOVERY", "MoonCrater", MoonPalaceClusterPoolKind.Terrain,
                PacingRole.Recovery, "MissingData", "0,1;1,1;2,1;1,0;1,2", ClusterPortSide.U,
                ClusterPortSide.L, "CraterFallRecovery", "CraterTerrain"),
            Spec(6, "TC_CRATER_QUIET_DUST_RIM", "MoonCrater", MoonPalaceClusterPoolKind.Quiet,
                PacingRole.Quiet, "TC_CRATER_QUIET_RIM", "0,0;1,0", ClusterPortSide.L,
                ClusterPortSide.R, "CraterQuietDustRim", "CraterQuiet"),
            Spec(7, "TC_CRATER_QUIET_SHALLOW_BOWL", "MoonCrater", MoonPalaceClusterPoolKind.Quiet,
                PacingRole.Quiet, "MissingData", "0,0;0,1", ClusterPortSide.U,
                ClusterPortSide.D, "CraterQuietShallowBowl", "CraterQuiet"),
            Spec(8, "TC_CRATER_QUIET_SHADOW_SHELF", "MoonCrater", MoonPalaceClusterPoolKind.Quiet,
                PacingRole.Quiet, "MissingData", "0,0;1,0", ClusterPortSide.R,
                ClusterPortSide.L, "CraterQuietShadowShelf", "CraterQuiet"),
            Spec(9, "TC_CRATER_BUFFER_ENTRY_RAMP", "MoonCrater", MoonPalaceClusterPoolKind.Buffer,
                PacingRole.Safe, "MissingData", "0,0;1,0", ClusterPortSide.L,
                ClusterPortSide.R, "CraterBufferEntryRamp", "CraterBuffer"),
            Spec(10, "TC_CRATER_BUFFER_EXIT_LEDGE", "MoonCrater", MoonPalaceClusterPoolKind.Buffer,
                PacingRole.Safe, "MissingData", "0,0;1,0;1,1", ClusterPortSide.L,
                ClusterPortSide.U, "CraterBufferExitLedge", "CraterBuffer"),
            Spec(11, "TC_CRATER_BUFFER_RECOVERY_DIP", "MoonCrater", MoonPalaceClusterPoolKind.Buffer,
                PacingRole.Recovery, "MissingData", "0,0;0,1;0,2", ClusterPortSide.D,
                ClusterPortSide.U, "CraterBufferRecoveryDip", "CraterBuffer"),
            Spec(12, "TC_ROOT_PROD_ARCH_FLOW", "CassiaRoot", MoonPalaceClusterPoolKind.Terrain,
                PacingRole.Traversal, "MissingData", "0,0;1,0;2,0", ClusterPortSide.L,
                ClusterPortSide.R, "RootArchFlow", "RootTerrain"),
            Spec(13, "TC_ROOT_PROD_VERTICAL_TUNNEL", "CassiaRoot", MoonPalaceClusterPoolKind.Terrain,
                PacingRole.Discovery, "TC_ROOT_VERTICAL_TUNNEL", "0,0;0,1;0,2;0,3",
                ClusterPortSide.D, ClusterPortSide.U, "RootVerticalTunnel", "RootTerrain"),
            Spec(14, "TC_ROOT_PROD_HOLLOW_POCKET", "CassiaRoot", MoonPalaceClusterPoolKind.Terrain,
                PacingRole.Traversal, "TC_ROOT_HOLLOW_POCKET", "0,0;1,0;1,1;2,1",
                ClusterPortSide.L, ClusterPortSide.R, "RootHollowPocket", "RootTerrain"),
            Spec(15, "TC_ROOT_PROD_CANOPY_SWITCHBACK", "CassiaRoot", MoonPalaceClusterPoolKind.Terrain,
                PacingRole.Risk, "MissingData", "0,0;0,1;1,1;1,2;2,2", ClusterPortSide.D,
                ClusterPortSide.R, "RootCanopySwitchback", "RootTerrain"),
            Spec(16, "TC_ROOT_PROD_SAP_LEDGE", "CassiaRoot", MoonPalaceClusterPoolKind.Terrain,
                PacingRole.Discovery, "MissingData", "0,1;1,1;1,0;2,0", ClusterPortSide.L,
                ClusterPortSide.R, "RootSapLedge", "RootTerrain"),
            Spec(17, "TC_ROOT_PROD_FORK_RECOVERY", "CassiaRoot", MoonPalaceClusterPoolKind.Terrain,
                PacingRole.Recovery, "TC_ROOT_FORKED_CANOPY_RECOVERY", "0,1;1,1;2,1;1,0;1,2",
                ClusterPortSide.L, ClusterPortSide.D, "RootForkRecovery", "RootTerrain"),
            Spec(18, "TC_ROOT_QUIET_ARCH_SHADE", "CassiaRoot", MoonPalaceClusterPoolKind.Quiet,
                PacingRole.Quiet, "TC_ROOT_QUIET_ARCH", "0,0;1,0", ClusterPortSide.L,
                ClusterPortSide.R, "RootQuietArchShade", "RootQuiet"),
            Spec(19, "TC_ROOT_QUIET_VINE_PASSAGE", "CassiaRoot", MoonPalaceClusterPoolKind.Quiet,
                PacingRole.Quiet, "MissingData", "0,0;0,1", ClusterPortSide.D,
                ClusterPortSide.U, "RootQuietVinePassage", "RootQuiet"),
            Spec(20, "TC_ROOT_QUIET_ROOT_BRIDGE", "CassiaRoot", MoonPalaceClusterPoolKind.Quiet,
                PacingRole.Quiet, "MissingData", "0,0;1,0", ClusterPortSide.R,
                ClusterPortSide.L, "RootQuietBridge", "RootQuiet"),
            Spec(21, "TC_ROOT_BUFFER_ENTRY_ARCH", "CassiaRoot", MoonPalaceClusterPoolKind.Buffer,
                PacingRole.Safe, "MissingData", "0,0;1,0", ClusterPortSide.L,
                ClusterPortSide.R, "RootBufferEntryArch", "RootBuffer"),
            Spec(22, "TC_ROOT_BUFFER_EXIT_TUNNEL", "CassiaRoot", MoonPalaceClusterPoolKind.Buffer,
                PacingRole.Safe, "MissingData", "0,0;0,1;0,2", ClusterPortSide.D,
                ClusterPortSide.U, "RootBufferExitTunnel", "RootBuffer"),
            Spec(23, "TC_ROOT_BUFFER_RECOVERY_CANOPY", "CassiaRoot", MoonPalaceClusterPoolKind.Buffer,
                PacingRole.Recovery, "MissingData", "0,0;1,0;1,1", ClusterPortSide.L,
                ClusterPortSide.U, "RootBufferRecoveryCanopy", "RootBuffer"),
        };

        private static ClusterSpec Spec(int ordinal, string clusterId, string biomeId,
            MoonPalaceClusterPoolKind poolKind, PacingRole pacingRole,
            string sourceStarterClusterId, string coordinateToken, ClusterPortSide entrySide,
            ClusterPortSide exitSide, string routeIntent, string repeatGroup) =>
            new ClusterSpec(ordinal, clusterId, biomeId, poolKind, pacingRole,
                AccessClass.MandatoryNoTool, sourceStarterClusterId,
                coordinateToken.Split(';').Select(token =>
                {
                    var parts = token.Split(',');
                    return new SpecCoordinate(int.Parse(parts[0], CultureInfo.InvariantCulture),
                        int.Parse(parts[1], CultureInfo.InvariantCulture));
                }).ToArray(), entrySide, exitSide, routeIntent, repeatGroup);

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
            if (FileSha256(Resolve(projectRoot, SourceMap2102ResultRelativePath)) !=
                MoonPalaceClusterPoolPreconditions.SourceMap2102ResultDigest)
                throw new InvalidDataException("MAP21_02 Result SHA-256 mismatch.");
            if (FileSha256(Resolve(projectRoot, SourceMap2102TaskRelativePath)) !=
                MoonPalaceClusterPoolPreconditions.SourceMap2102TaskDigest)
                throw new InvalidDataException("MAP21_02 Task SHA-256 mismatch.");
            var value = JsonUtility.FromJson<SourceMap2102DigestDocument>(File.ReadAllText(
                Resolve(projectRoot, SourceMap2102DigestManifestRelativePath)));
            if (value == null || value.MAP21_03_handoff_digest !=
                    MoonPalaceClusterPoolPreconditions.SourceMap2103HandoffDigest ||
                value.pattern_catalog_digest !=
                    MoonPalaceClusterPoolPreconditions.SourcePatternCatalogDigest ||
                value.pattern_cell_digest !=
                    MoonPalaceClusterPoolPreconditions.SourcePatternCellDigest ||
                value.pattern_tag_digest !=
                    MoonPalaceClusterPoolPreconditions.SourcePatternTagDigest)
                throw new InvalidDataException("MAP21_02 digest chain mismatch.");
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

    public sealed class MoonPalaceClusterPoolPublishedSample
    {
        private readonly ReadOnlyCollection<string> writtenRelativePaths;

        public MoonPalaceClusterPoolPublishedSample(MoonPalaceClusterPoolProduction production,
            MoonPalaceClusterPoolDigestManifest digestManifest,
            MoonPalaceClusterPoolForbiddenOperationCounters counters,
            IEnumerable<string> sourceWrittenRelativePaths)
        {
            Production = production ?? throw new ArgumentNullException(nameof(production));
            DigestManifest = digestManifest ?? throw new ArgumentNullException(
                nameof(digestManifest));
            Counters = counters ?? throw new ArgumentNullException(nameof(counters));
            writtenRelativePaths = new ReadOnlyCollection<string>((sourceWrittenRelativePaths ??
                Array.Empty<string>()).OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        public MoonPalaceClusterPoolProduction Production { get; }
        public MoonPalaceClusterPoolDigestManifest DigestManifest { get; }
        public MoonPalaceClusterPoolForbiddenOperationCounters Counters { get; }
        public IReadOnlyList<string> WrittenRelativePaths => writtenRelativePaths;
    }

    public sealed class MoonPalaceClusterPoolForbiddenOperationCounters
    {
        public static MoonPalaceClusterPoolForbiddenOperationCounters Zero =>
            new MoonPalaceClusterPoolForbiddenOperationCounters();
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
        public int MillDoughClustersAuthored => 0;
        public int BufferLandmarkSearchExecutions => 0;
        public int CsvWritesOutsideMap2103 => 0;
        public bool AllZero => GenerationExecutions == 0 && RendererExecutions == 0 &&
            ValidationRunnerExecutions == 0 && ReplayExecutions == 0 &&
            RollbackExecutions == 0 && PlayModeSelections == 0 &&
            LegacyRegressionSelections == 0 && PriorCategorySelections == 0 &&
            UnfilteredOrFullRegressionRuns == 0 && RuntimeObjectSpawns == 0 &&
            MillDoughClustersAuthored == 0 && BufferLandmarkSearchExecutions == 0 &&
            CsvWritesOutsideMap2103 == 0;
    }

    internal sealed class ClusterSpec
    {
        public ClusterSpec(int ordinal, string clusterId, string biomeId,
            MoonPalaceClusterPoolKind poolKind, PacingRole pacingRole,
            AccessClass accessClass, string sourceStarterClusterId,
            SpecCoordinate[] coordinates, ClusterPortSide entrySide,
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
        public SpecCoordinate[] Coordinates { get; }
        public ClusterPortSide EntrySide { get; }
        public ClusterPortSide ExitSide { get; }
        public string RouteIntent { get; }
        public string RepeatGroup { get; }
    }

    internal readonly struct SpecCoordinate
    {
        public SpecCoordinate(int x, int y) { X = x; Y = y; }
        public int X { get; }
        public int Y { get; }
    }

    [Serializable]
    internal sealed class SourceMap2102DigestDocument
    {
        public string pattern_catalog_digest;
        public string pattern_cell_digest;
        public string pattern_tag_digest;
        public string MAP21_03_handoff_digest;
    }
}
