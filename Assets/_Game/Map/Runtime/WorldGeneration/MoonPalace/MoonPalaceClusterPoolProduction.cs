using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.WorldGeneration.MoonPalace
{
    public static class MoonPalaceClusterPoolPreconditions
    {
        public const string TaskId = "MAP21_03_EXPAND_CRATER_AND_ROOT_CLUSTER_POOLS";
        public const string SourceMap2102ResultDigest =
            "8963713cd62f4f25311e876df74551e6e1304c50885652f51a32b46019ca2017";
        public const string SourceMap2102TaskDigest =
            "0e29cf56fbc0172aa8fb6fcc7969700207409fc5b15103bec23b85fa70540073";
        public const string SourceMap2103HandoffDigest =
            "c918b519cdcbc944746187f7dc6d1d3b0b00280f8ca47702e3e4f518ae55f4db";
        public const string SourcePatternCatalogDigest =
            "64765ac5aeccf149ebf635c54519a58ecc2629f98e6cacd285ee1bfacb77c560";
        public const string SourcePatternCellDigest =
            "d5d9fc1a8ff1b4e019c1fa779731ecbc80ec4a02da695c409ec9ccd4046b01ec";
        public const string SourcePatternTagDigest =
            "35a9ce555c91ebc786fc90f142250bd931de04de40fbeaef665ddb8398cd1c62";
    }

    public enum MoonPalaceClusterPoolKind
    {
        Terrain = 1,
        Quiet = 2,
        Buffer = 3,
    }

    public enum MoonPalaceClusterPatternSlotKind
    {
        BaselineRequired = 1,
        OptionalSilhouette = 2,
        RecoveryCue = 3,
    }

    public sealed class MoonPalaceClusterFootprintRecord
    {
        public MoonPalaceClusterFootprintRecord(string clusterId, int chunkX, int chunkY,
            ClusterRoleKind chunkRole, ClusterPortSide entrySide, ClusterPortSide exitSide,
            bool isEntryChunk, bool isExitChunk)
        {
            ClusterId = RequireClusterId(clusterId);
            if (chunkX < 0 || chunkX > 3) throw new ArgumentOutOfRangeException(nameof(chunkX));
            if (chunkY < 0 || chunkY > 3) throw new ArgumentOutOfRangeException(nameof(chunkY));
            if (!Enum.IsDefined(typeof(ClusterRoleKind), chunkRole))
                throw new ArgumentOutOfRangeException(nameof(chunkRole));
            if (!Enum.IsDefined(typeof(ClusterPortSide), entrySide))
                throw new ArgumentOutOfRangeException(nameof(entrySide));
            if (!Enum.IsDefined(typeof(ClusterPortSide), exitSide))
                throw new ArgumentOutOfRangeException(nameof(exitSide));
            ChunkX = chunkX;
            ChunkY = chunkY;
            ChunkRole = chunkRole;
            EntrySide = entrySide;
            ExitSide = exitSide;
            IsEntryChunk = isEntryChunk;
            IsExitChunk = isExitChunk;
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MAP21_03_CLUSTER_FOOTPRINT_RECORD_V1", CanonicalLine });
        }

        public string ClusterId { get; }
        public int ChunkX { get; }
        public int ChunkY { get; }
        public ClusterRoleKind ChunkRole { get; }
        public ClusterPortSide EntrySide { get; }
        public ClusterPortSide ExitSide { get; }
        public bool IsEntryChunk { get; }
        public bool IsExitChunk { get; }
        public string CanonicalDigest { get; }
        public string CoordinateToken => ChunkX.ToString(CultureInfo.InvariantCulture) + "," +
            ChunkY.ToString(CultureInfo.InvariantCulture);
        public string CanonicalLine => MoonPalaceCanonical.Join(ClusterId,
            ChunkX.ToString(CultureInfo.InvariantCulture),
            ChunkY.ToString(CultureInfo.InvariantCulture), ChunkRole.ToString(),
            EntrySide.ToString(), ExitSide.ToString(), IsEntryChunk ? "true" : "false",
            IsExitChunk ? "true" : "false");

        internal static string RequireClusterId(string value)
        {
            value = MoonPalaceProfileValidation.Require(value, nameof(value));
            if (!value.StartsWith("TC_", StringComparison.Ordinal) || value.Any(character =>
                    !(character == '_' || character >= 'A' && character <= 'Z' ||
                      character >= '0' && character <= '9')))
                throw new ArgumentException("Cluster id must match ^TC_[A-Z0-9_]+$.",
                    nameof(value));
            return value;
        }
    }

    public sealed class MoonPalaceClusterSpineVariantRecord
    {
        private readonly ReadOnlyCollection<TraversalMovementKind> movementTokens;

        public MoonPalaceClusterSpineVariantRecord(string clusterId, string spineVariantId,
            bool isBaseline, ClusterPortSide entrySide, ClusterPortSide exitSide,
            string routeIntent, IEnumerable<TraversalMovementKind> sourceMovementTokens,
            string highRouteState, string recoveryState, string protectedMaskPolicy)
        {
            ClusterId = MoonPalaceClusterFootprintRecord.RequireClusterId(clusterId);
            SpineVariantId = MoonPalaceProfileValidation.Require(spineVariantId,
                nameof(spineVariantId));
            var requiredSuffix = isBaseline ? "_BASE" : "_ALT";
            if (!SpineVariantId.Equals(ClusterId + requiredSuffix, StringComparison.Ordinal))
                throw new ArgumentException("Spine variant id must use the cluster BASE/ALT id.",
                    nameof(spineVariantId));
            if (!Enum.IsDefined(typeof(ClusterPortSide), entrySide))
                throw new ArgumentOutOfRangeException(nameof(entrySide));
            if (!Enum.IsDefined(typeof(ClusterPortSide), exitSide))
                throw new ArgumentOutOfRangeException(nameof(exitSide));
            var movements = (sourceMovementTokens ?? throw new ArgumentNullException(
                    nameof(sourceMovementTokens))).ToArray();
            if (movements.Length == 0 || movements.Any(value =>
                    !Enum.IsDefined(typeof(TraversalMovementKind), value)))
                throw new ArgumentException("Published MAP11 movement tokens are required.",
                    nameof(sourceMovementTokens));
            IsBaseline = isBaseline;
            EntrySide = entrySide;
            ExitSide = exitSide;
            RouteIntent = MoonPalaceProfileValidation.Require(routeIntent, nameof(routeIntent));
            movementTokens = new ReadOnlyCollection<TraversalMovementKind>(movements);
            HighRouteState = MoonPalaceProfileValidation.Require(highRouteState,
                nameof(highRouteState));
            RecoveryState = MoonPalaceProfileValidation.Require(recoveryState,
                nameof(recoveryState));
            ProtectedMaskPolicy = MoonPalaceProfileValidation.Require(protectedMaskPolicy,
                nameof(protectedMaskPolicy));
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MAP21_03_CLUSTER_SPINE_VARIANT_V1", CanonicalLine });
        }

        public string ClusterId { get; }
        public string SpineVariantId { get; }
        public bool IsBaseline { get; }
        public ClusterPortSide EntrySide { get; }
        public ClusterPortSide ExitSide { get; }
        public string RouteIntent { get; }
        public IReadOnlyList<TraversalMovementKind> MovementTokens => movementTokens;
        public string HighRouteState { get; }
        public string RecoveryState { get; }
        public string ProtectedMaskPolicy { get; }
        public string CanonicalDigest { get; }
        public string CanonicalLine => MoonPalaceCanonical.Join(ClusterId, SpineVariantId,
            IsBaseline ? "true" : "false", EntrySide.ToString(), ExitSide.ToString(),
            RouteIntent, string.Join("|", MovementTokens.Select(value => value.ToString())),
            HighRouteState, RecoveryState, ProtectedMaskPolicy);
    }

    public sealed class MoonPalaceClusterPatternSlotRecord
    {
        private readonly ReadOnlyCollection<string> allowedPatternIds;

        public MoonPalaceClusterPatternSlotRecord(string clusterId, string slotId,
            int chunkX, int chunkY, MoonPalaceClusterPatternSlotKind slotKind,
            IEnumerable<string> sourceAllowedPatternIds, string biomeId,
            string protectedMaskPolicy, int riskBudget,
            IReadOnlyDictionary<string, string> patternBiomeById)
        {
            ClusterId = MoonPalaceClusterFootprintRecord.RequireClusterId(clusterId);
            SlotId = MoonPalaceProfileValidation.Require(slotId, nameof(slotId));
            if (chunkX < 0 || chunkX > 3) throw new ArgumentOutOfRangeException(nameof(chunkX));
            if (chunkY < 0 || chunkY > 3) throw new ArgumentOutOfRangeException(nameof(chunkY));
            if (!Enum.IsDefined(typeof(MoonPalaceClusterPatternSlotKind), slotKind))
                throw new ArgumentOutOfRangeException(nameof(slotKind));
            BiomeId = MoonpalaceBiomeId.Parse(biomeId).CanonicalId;
            var patterns = (sourceAllowedPatternIds ?? throw new ArgumentNullException(
                    nameof(sourceAllowedPatternIds)))
                .Select(value => MoonPalaceProfileValidation.Require(value,
                    nameof(sourceAllowedPatternIds)))
                .Distinct(StringComparer.Ordinal).OrderBy(value => value,
                    StringComparer.Ordinal).ToArray();
            if (patterns.Length == 0)
                throw new ArgumentException("Pattern slot allowlist cannot be empty.",
                    nameof(sourceAllowedPatternIds));
            if (patternBiomeById == null || patterns.Any(patternId =>
                    !patternBiomeById.TryGetValue(patternId, out var patternBiome) ||
                    !string.Equals(patternBiome, BiomeId, StringComparison.Ordinal)))
                throw new ArgumentException("Pattern slots require known same-biome MAP21_02 ids.",
                    nameof(sourceAllowedPatternIds));
            if (riskBudget < 0 || riskBudget > 100)
                throw new ArgumentOutOfRangeException(nameof(riskBudget));
            ChunkX = chunkX;
            ChunkY = chunkY;
            SlotKind = slotKind;
            allowedPatternIds = new ReadOnlyCollection<string>(patterns);
            ProtectedMaskPolicy = MoonPalaceProfileValidation.Require(protectedMaskPolicy,
                nameof(protectedMaskPolicy));
            RiskBudget = riskBudget;
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MAP21_03_CLUSTER_PATTERN_SLOT_V1", CanonicalLine });
        }

        public string ClusterId { get; }
        public string SlotId { get; }
        public int ChunkX { get; }
        public int ChunkY { get; }
        public MoonPalaceClusterPatternSlotKind SlotKind { get; }
        public IReadOnlyList<string> AllowedPatternIds => allowedPatternIds;
        public string BiomeId { get; }
        public string ProtectedMaskPolicy { get; }
        public int RiskBudget { get; }
        public string CanonicalDigest { get; }
        public string CoordinateToken => ChunkX.ToString(CultureInfo.InvariantCulture) + "," +
            ChunkY.ToString(CultureInfo.InvariantCulture);
        public string CanonicalLine => MoonPalaceCanonical.Join(ClusterId, SlotId,
            ChunkX.ToString(CultureInfo.InvariantCulture),
            ChunkY.ToString(CultureInfo.InvariantCulture), SlotKind.ToString(),
            string.Join("|", AllowedPatternIds), BiomeId, ProtectedMaskPolicy,
            RiskBudget.ToString(CultureInfo.InvariantCulture));
    }

    public sealed class MoonPalaceClusterCatalogRecord
    {
        public const string SchemaVersion = "map21_03.moonpalace_cluster_pool.v1";

        public MoonPalaceClusterCatalogRecord(string clusterId, string biomeId,
            MoonPalaceClusterPoolKind poolKind, PacingRole pacingRole,
            AccessClass accessClass, int activeChunkCount, string sourceStarterClusterId,
            string sourcePatternPoolDigest, string footprintDigest,
            string spineVariantDigest, string patternSlotDigest, string structuralSignature,
            string silhouetteSignature, string repeatGroup)
        {
            ClusterId = MoonPalaceClusterFootprintRecord.RequireClusterId(clusterId);
            BiomeId = MoonpalaceBiomeId.Parse(biomeId).CanonicalId;
            if (BiomeId != MoonpalaceBiomeId.MoonCrater.CanonicalId &&
                BiomeId != MoonpalaceBiomeId.CassiaRoot.CanonicalId &&
                BiomeId != MoonpalaceBiomeId.AbandonedMill.CanonicalId &&
                BiomeId != MoonpalaceBiomeId.MoonDough.CanonicalId)
                throw new ArgumentException("MoonPalace cluster pools own only the four locked biomes.",
                    nameof(biomeId));
            if (!Enum.IsDefined(typeof(MoonPalaceClusterPoolKind), poolKind))
                throw new ArgumentOutOfRangeException(nameof(poolKind));
            if (!PacingRoleTokenCodec.IsPublished(pacingRole))
                throw new ArgumentOutOfRangeException(nameof(pacingRole));
            if (!AccessClassTokenCodec.IsPublished(accessClass))
                throw new ArgumentOutOfRangeException(nameof(accessClass));
            if (activeChunkCount < 2 || activeChunkCount > 5)
                throw new ArgumentOutOfRangeException(nameof(activeChunkCount));
            PoolKind = poolKind;
            PacingRole = pacingRole;
            AccessClass = accessClass;
            ActiveChunkCount = activeChunkCount;
            SourceStarterClusterId = MoonPalaceProfileValidation.Require(sourceStarterClusterId,
                nameof(sourceStarterClusterId));
            SourcePatternPoolDigest = MoonPalaceProfileValidation.Digest(
                sourcePatternPoolDigest, nameof(sourcePatternPoolDigest));
            if (SourcePatternPoolDigest !=
                MoonPalaceClusterPoolPreconditions.SourcePatternCatalogDigest)
                throw new ArgumentException("MAP21_02 pattern pool digest mismatch.",
                    nameof(sourcePatternPoolDigest));
            FootprintDigest = MoonPalaceProfileValidation.Digest(footprintDigest,
                nameof(footprintDigest));
            SpineVariantDigest = MoonPalaceProfileValidation.Digest(spineVariantDigest,
                nameof(spineVariantDigest));
            PatternSlotDigest = MoonPalaceProfileValidation.Digest(patternSlotDigest,
                nameof(patternSlotDigest));
            StructuralSignature = MoonPalaceProfileValidation.Digest(structuralSignature,
                nameof(structuralSignature));
            SilhouetteSignature = MoonPalaceProfileValidation.Digest(silhouetteSignature,
                nameof(silhouetteSignature));
            RepeatGroup = MoonPalaceProfileValidation.Require(repeatGroup, nameof(repeatGroup));
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MAP21_03_CLUSTER_CATALOG_RECORD_V1", CanonicalLine });
        }

        public string ClusterId { get; }
        public string BiomeId { get; }
        public MoonPalaceClusterPoolKind PoolKind { get; }
        public PacingRole PacingRole { get; }
        public AccessClass AccessClass { get; }
        public int ActiveChunkCount { get; }
        public string SourceStarterClusterId { get; }
        public string SourcePatternPoolDigest { get; }
        public string FootprintDigest { get; }
        public string SpineVariantDigest { get; }
        public string PatternSlotDigest { get; }
        public string StructuralSignature { get; }
        public string SilhouetteSignature { get; }
        public string RepeatGroup { get; }
        public bool CreatedUtcExcludedFromCanonicalDigest => true;
        public string CanonicalDigest { get; }
        public string CanonicalLine => MoonPalaceCanonical.Join(ClusterId, SchemaVersion,
            BiomeId, PoolKind.ToString(), PacingRoleTokenCodec.ToToken(PacingRole),
            AccessClassTokenCodec.ToToken(AccessClass),
            ActiveChunkCount.ToString(CultureInfo.InvariantCulture), SourceStarterClusterId,
            SourcePatternPoolDigest, FootprintDigest, SpineVariantDigest, PatternSlotDigest,
            StructuralSignature, SilhouetteSignature, RepeatGroup,
            "created_utc_excluded=true");
    }

    public sealed class MoonPalaceClusterPoolProduction
    {
        public const string SchemaVersion = "map21_03.moonpalace_cluster_pool_production.v1";
        private static readonly ReadOnlyCollection<string> requiredClusterIds =
            new ReadOnlyCollection<string>(new[]
            {
                "TC_CRATER_PROD_RIM_ASCENT", "TC_CRATER_PROD_BROKEN_SLOPE",
                "TC_CRATER_PROD_BOWL_CROSS", "TC_CRATER_PROD_ROCK_SHELF_CHAIN",
                "TC_CRATER_PROD_HIGH_LEDGE", "TC_CRATER_PROD_FALL_RECOVERY",
                "TC_CRATER_QUIET_DUST_RIM", "TC_CRATER_QUIET_SHALLOW_BOWL",
                "TC_CRATER_QUIET_SHADOW_SHELF", "TC_CRATER_BUFFER_ENTRY_RAMP",
                "TC_CRATER_BUFFER_EXIT_LEDGE", "TC_CRATER_BUFFER_RECOVERY_DIP",
                "TC_ROOT_PROD_ARCH_FLOW", "TC_ROOT_PROD_VERTICAL_TUNNEL",
                "TC_ROOT_PROD_HOLLOW_POCKET", "TC_ROOT_PROD_CANOPY_SWITCHBACK",
                "TC_ROOT_PROD_SAP_LEDGE", "TC_ROOT_PROD_FORK_RECOVERY",
                "TC_ROOT_QUIET_ARCH_SHADE", "TC_ROOT_QUIET_VINE_PASSAGE",
                "TC_ROOT_QUIET_ROOT_BRIDGE", "TC_ROOT_BUFFER_ENTRY_ARCH",
                "TC_ROOT_BUFFER_EXIT_TUNNEL", "TC_ROOT_BUFFER_RECOVERY_CANOPY",
            }.OrderBy(value => value, StringComparer.Ordinal).ToArray());
        private static readonly HashSet<string> allowedSourceClusterIds = new HashSet<string>(
            new[]
            {
                "TC_CRATER_BOWL_ASCENT", "TC_CRATER_BROKEN_SLOPE",
                "TC_CRATER_QUIET_RIM", "TC_CRATER_ROCK_SHELF_RECOVERY",
                "TC_ROOT_FORKED_CANOPY_RECOVERY", "TC_ROOT_HOLLOW_POCKET",
                "TC_ROOT_QUIET_ARCH", "TC_ROOT_VERTICAL_TUNNEL",
            }, StringComparer.Ordinal);
        private static readonly HashSet<string> allowedRepeatGroups = new HashSet<string>(new[]
        {
            "CraterTerrain", "CraterQuiet", "CraterBuffer",
            "RootTerrain", "RootQuiet", "RootBuffer",
        }, StringComparer.Ordinal);

        private readonly ReadOnlyCollection<MoonPalaceClusterCatalogRecord> catalog;
        private readonly ReadOnlyCollection<MoonPalaceClusterFootprintRecord> footprints;
        private readonly ReadOnlyCollection<MoonPalaceClusterSpineVariantRecord> spineVariants;
        private readonly ReadOnlyCollection<MoonPalaceClusterPatternSlotRecord> patternSlots;

        public MoonPalaceClusterPoolProduction(
            IEnumerable<MoonPalaceClusterCatalogRecord> sourceCatalog,
            IEnumerable<MoonPalaceClusterFootprintRecord> sourceFootprints,
            IEnumerable<MoonPalaceClusterSpineVariantRecord> sourceSpineVariants,
            IEnumerable<MoonPalaceClusterPatternSlotRecord> sourcePatternSlots,
            IEnumerable<string> hazardPatternIds,
            IEnumerable<string> bufferForbiddenMarkerPatternIds, string createdUtc)
            : this(sourceCatalog, sourceFootprints, sourceSpineVariants, sourcePatternSlots,
                hazardPatternIds, bufferForbiddenMarkerPatternIds, createdUtc,
                requiredClusterIds, new[] { "MoonCrater", "CassiaRoot" },
                allowedRepeatGroups, allowedSourceClusterIds, 8, "MAP21_03")
        {
        }

        internal MoonPalaceClusterPoolProduction(
            IEnumerable<MoonPalaceClusterCatalogRecord> sourceCatalog,
            IEnumerable<MoonPalaceClusterFootprintRecord> sourceFootprints,
            IEnumerable<MoonPalaceClusterSpineVariantRecord> sourceSpineVariants,
            IEnumerable<MoonPalaceClusterPatternSlotRecord> sourcePatternSlots,
            IEnumerable<string> hazardPatternIds,
            IEnumerable<string> bufferForbiddenMarkerPatternIds, string createdUtc,
            IEnumerable<string> sourceRequiredClusterIds,
            IEnumerable<string> sourceRequiredBiomes,
            IEnumerable<string> sourceAllowedRepeatGroups,
            IEnumerable<string> sourceAllowedClusterIds, int requiredSourceMappingCount,
            string inventoryOwner)
        {
            var requiredIds = new ReadOnlyCollection<string>((sourceRequiredClusterIds ??
                    throw new ArgumentNullException(nameof(sourceRequiredClusterIds)))
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
            var requiredBiomes = (sourceRequiredBiomes ?? throw new ArgumentNullException(
                    nameof(sourceRequiredBiomes))).OrderBy(value => value,
                    StringComparer.Ordinal).ToArray();
            var repeatGroups = new HashSet<string>(sourceAllowedRepeatGroups ??
                throw new ArgumentNullException(nameof(sourceAllowedRepeatGroups)),
                StringComparer.Ordinal);
            var sourceClusterIds = new HashSet<string>(sourceAllowedClusterIds ??
                throw new ArgumentNullException(nameof(sourceAllowedClusterIds)),
                StringComparer.Ordinal);
            var orderedCatalog = Required(sourceCatalog, nameof(sourceCatalog))
                .OrderBy(value => value.ClusterId, StringComparer.Ordinal).ToArray();
            if (!orderedCatalog.Select(value => value.ClusterId).SequenceEqual(
                    requiredIds, StringComparer.Ordinal))
                throw new ArgumentException("Exact " + inventoryOwner +
                    " cluster inventory is required.",
                    nameof(sourceCatalog));
            if (orderedCatalog.GroupBy(value => value.ClusterId, StringComparer.Ordinal)
                .Any(group => group.Count() != 1))
                throw new ArgumentException("Cluster ids must be unique.", nameof(sourceCatalog));
            foreach (var biome in requiredBiomes)
            {
                var biomeRecords = orderedCatalog.Where(value => value.BiomeId == biome).ToArray();
                if (biomeRecords.Length != 12 ||
                    biomeRecords.Count(value => value.PoolKind ==
                        MoonPalaceClusterPoolKind.Terrain) != 6 ||
                    biomeRecords.Count(value => value.PoolKind ==
                        MoonPalaceClusterPoolKind.Quiet) != 3 ||
                    biomeRecords.Count(value => value.PoolKind ==
                        MoonPalaceClusterPoolKind.Buffer) != 3)
                    throw new ArgumentException("Each biome requires Terrain 6 / Quiet 3 / Buffer 3.",
                        nameof(sourceCatalog));
            }
            if (orderedCatalog.Any(value => !repeatGroups.Contains(value.RepeatGroup)))
                throw new ArgumentException("Unknown repetition group.", nameof(sourceCatalog));
            if (orderedCatalog.Any(value => value.SourceStarterClusterId != "MissingData" &&
                    !sourceClusterIds.Contains(value.SourceStarterClusterId)) ||
                orderedCatalog.Count(value => value.SourceStarterClusterId != "MissingData") <
                    requiredSourceMappingCount)
                throw new ArgumentException(requiredSourceMappingCount.ToString(
                    CultureInfo.InvariantCulture) +
                    " compatible MAP11 source mappings are required.",
                    nameof(sourceCatalog));

            var orderedFootprints = Required(sourceFootprints, nameof(sourceFootprints))
                .OrderBy(value => value.ClusterId, StringComparer.Ordinal)
                .ThenBy(value => value.ChunkY).ThenBy(value => value.ChunkX).ToArray();
            var orderedSpines = Required(sourceSpineVariants, nameof(sourceSpineVariants))
                .OrderBy(value => value.ClusterId, StringComparer.Ordinal)
                .ThenBy(value => value.SpineVariantId, StringComparer.Ordinal).ToArray();
            var orderedSlots = Required(sourcePatternSlots, nameof(sourcePatternSlots))
                .OrderBy(value => value.ClusterId, StringComparer.Ordinal)
                .ThenBy(value => value.SlotId, StringComparer.Ordinal).ToArray();
            var knownClusters = new HashSet<string>(requiredIds, StringComparer.Ordinal);
            if (orderedFootprints.Any(value => !knownClusters.Contains(value.ClusterId)) ||
                orderedSpines.Any(value => !knownClusters.Contains(value.ClusterId)) ||
                orderedSlots.Any(value => !knownClusters.Contains(value.ClusterId)))
                throw new ArgumentException("Cluster child record references an unknown cluster.");

            foreach (var record in orderedCatalog)
            {
                var footprint = orderedFootprints.Where(value =>
                    value.ClusterId == record.ClusterId).ToArray();
                ValidateFootprint(record, footprint);
                if (record.FootprintDigest != ComputeFootprintDigest(footprint))
                    throw new ArgumentException("Footprint digest mismatch: " + record.ClusterId);
                var spines = orderedSpines.Where(value =>
                    value.ClusterId == record.ClusterId).ToArray();
                ValidateSpines(record, footprint, spines);
                if (record.SpineVariantDigest != ComputeSpineVariantDigest(spines))
                    throw new ArgumentException("Spine digest mismatch: " + record.ClusterId);
                var slots = orderedSlots.Where(value => value.ClusterId == record.ClusterId).ToArray();
                ValidateSlots(record, footprint, slots);
                if (record.PatternSlotDigest != ComputePatternSlotDigest(slots))
                    throw new ArgumentException("Pattern slot digest mismatch: " + record.ClusterId);
            }

            var hazards = new HashSet<string>(hazardPatternIds ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            var forbiddenMarkers = new HashSet<string>(bufferForbiddenMarkerPatternIds ??
                Array.Empty<string>(), StringComparer.Ordinal);
            QuietHazardBaselineSlotCount = orderedCatalog.Where(value => value.PoolKind ==
                    MoonPalaceClusterPoolKind.Quiet)
                .SelectMany(cluster => orderedSlots.Where(slot =>
                    slot.ClusterId == cluster.ClusterId && slot.SlotKind ==
                    MoonPalaceClusterPatternSlotKind.BaselineRequired))
                .Count(slot => slot.AllowedPatternIds.Any(hazards.Contains));
            BufferForbiddenMarkerReferenceCount = orderedCatalog.Where(value => value.PoolKind ==
                    MoonPalaceClusterPoolKind.Buffer)
                .SelectMany(cluster => orderedSlots.Where(slot =>
                    slot.ClusterId == cluster.ClusterId))
                .Count(slot => slot.AllowedPatternIds.Any(forbiddenMarkers.Contains));
            if (QuietHazardBaselineSlotCount != 0 || BufferForbiddenMarkerReferenceCount != 0)
                throw new ArgumentException("Quiet/Buffer static slot policy was violated.");
            if (orderedCatalog.Select(value => value.StructuralSignature)
                    .Distinct(StringComparer.Ordinal).Count() != orderedCatalog.Length)
                throw new ArgumentException("Structural signatures must be globally unique.",
                    nameof(sourceCatalog));
            if (orderedCatalog.GroupBy(value => value.BiomeId + "/" + value.PoolKind,
                    StringComparer.Ordinal).Any(group => group.Select(value =>
                    value.SilhouetteSignature).Distinct(StringComparer.Ordinal).Count() !=
                    group.Count()))
                throw new ArgumentException("Silhouette signatures must be unique per biome/pool.",
                    nameof(sourceCatalog));

            catalog = new ReadOnlyCollection<MoonPalaceClusterCatalogRecord>(orderedCatalog);
            footprints = new ReadOnlyCollection<MoonPalaceClusterFootprintRecord>(orderedFootprints);
            spineVariants = new ReadOnlyCollection<MoonPalaceClusterSpineVariantRecord>(orderedSpines);
            patternSlots = new ReadOnlyCollection<MoonPalaceClusterPatternSlotRecord>(orderedSlots);
            CreatedUtc = createdUtc ?? string.Empty;
            ClusterCatalogDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MAP21_03_CLUSTER_CATALOG_V1" }.Concat(catalog.Select(value => value.CanonicalLine)));
            ClusterFootprintDigest = ComputeFootprintDigest(footprints);
            ClusterSpineVariantDigest = ComputeSpineVariantDigest(spineVariants);
            ClusterPatternSlotDigest = ComputePatternSlotDigest(patternSlots);
            ClusterSignatureDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MAP21_03_CLUSTER_SIGNATURES_V1" }.Concat(catalog.Select(value =>
                    MoonPalaceCanonical.Join(value.ClusterId, value.RepeatGroup,
                        value.StructuralSignature, value.SilhouetteSignature))));
            PoolManifestDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP21_03_CLUSTER_POOL_MANIFEST_V1", SchemaVersion,
                MoonPalaceClusterPoolPreconditions.SourceMap2103HandoffDigest,
                ClusterCatalogDigest, ClusterFootprintDigest, ClusterSpineVariantDigest,
                ClusterPatternSlotDigest, "created_utc_excluded=true",
            });
            SignatureManifestDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP21_03_CLUSTER_SIGNATURE_MANIFEST_V1", SchemaVersion,
                ClusterSignatureDigest, "created_utc_excluded=true",
            });
        }

        public static IReadOnlyList<string> RequiredClusterIds => requiredClusterIds;
        public IReadOnlyList<MoonPalaceClusterCatalogRecord> Catalog => catalog;
        public IReadOnlyList<MoonPalaceClusterFootprintRecord> Footprints => footprints;
        public IReadOnlyList<MoonPalaceClusterSpineVariantRecord> SpineVariants => spineVariants;
        public IReadOnlyList<MoonPalaceClusterPatternSlotRecord> PatternSlots => patternSlots;
        public string CreatedUtc { get; }
        public bool CreatedUtcExcludedFromCanonicalDigest => true;
        public int ActivitySlotCount => 0;
        public int EventOverlayCount => 0;
        public int SpecialRegionBindingCount => 0;
        public int PopulationSlotCount => 0;
        public int RewardAnchorCount => 0;
        public int RuntimeBindingCount => 0;
        public int TilemapWriteCount => 0;
        public int BufferLandmarkSearchExecutions => 0;
        public int QuietHazardBaselineSlotCount { get; }
        public int BufferForbiddenMarkerReferenceCount { get; }
        public string ClusterCatalogDigest { get; }
        public string ClusterFootprintDigest { get; }
        public string ClusterSpineVariantDigest { get; }
        public string ClusterPatternSlotDigest { get; }
        public string ClusterSignatureDigest { get; }
        public string PoolManifestDigest { get; }
        public string SignatureManifestDigest { get; }

        public string SerializePoolManifest() => MoonPalaceCanonical.ToJson(
            MoonPalaceClusterPoolManifestDocument.From(this));
        public string SerializeSignatureManifest() => MoonPalaceCanonical.ToJson(
            MoonPalaceClusterSignatureManifestDocument.From(this));

        public string SerializeCatalogCsv()
        {
            var lines = new List<string>
            {
                "cluster_id,schema_version,biome_id,pool_kind,pacing_role,access_class,active_chunk_count,source_starter_cluster_id,source_pattern_pool_digest,footprint_digest,spine_variant_digest,pattern_slot_digest,structural_signature,silhouette_signature,repeat_group,created_utc_excluded_from_canonical_digest,canonical_digest",
            };
            lines.AddRange(Catalog.Select(value => MoonPalaceProductionCsv.Join(value.ClusterId,
                MoonPalaceClusterCatalogRecord.SchemaVersion, value.BiomeId,
                value.PoolKind.ToString(), PacingRoleTokenCodec.ToToken(value.PacingRole),
                AccessClassTokenCodec.ToToken(value.AccessClass),
                value.ActiveChunkCount.ToString(CultureInfo.InvariantCulture),
                value.SourceStarterClusterId, value.SourcePatternPoolDigest,
                value.FootprintDigest, value.SpineVariantDigest, value.PatternSlotDigest,
                value.StructuralSignature, value.SilhouetteSignature, value.RepeatGroup,
                "true", value.CanonicalDigest)));
            return string.Join("\n", lines) + "\n";
        }

        public string SerializeFootprintCsv()
        {
            var lines = new List<string>
            {
                "cluster_id,chunk_x,chunk_y,chunk_role,entry_side,exit_side,is_entry_chunk,is_exit_chunk,canonical_digest",
            };
            lines.AddRange(Footprints.Select(value => MoonPalaceProductionCsv.Join(
                value.ClusterId, value.ChunkX.ToString(CultureInfo.InvariantCulture),
                value.ChunkY.ToString(CultureInfo.InvariantCulture), value.ChunkRole.ToString(),
                value.EntrySide.ToString(), value.ExitSide.ToString(),
                value.IsEntryChunk ? "true" : "false", value.IsExitChunk ? "true" : "false",
                value.CanonicalDigest)));
            return string.Join("\n", lines) + "\n";
        }

        public string SerializeSpineVariantCsv()
        {
            var lines = new List<string>
            {
                "cluster_id,spine_variant_id,is_baseline,entry_side,exit_side,route_intent,movement_tokens,high_route_state,recovery_state,protected_mask_policy,canonical_digest",
            };
            lines.AddRange(SpineVariants.Select(value => MoonPalaceProductionCsv.Join(
                value.ClusterId, value.SpineVariantId, value.IsBaseline ? "true" : "false",
                value.EntrySide.ToString(), value.ExitSide.ToString(), value.RouteIntent,
                string.Join("|", value.MovementTokens.Select(token => token.ToString())),
                value.HighRouteState, value.RecoveryState, value.ProtectedMaskPolicy,
                value.CanonicalDigest)));
            return string.Join("\n", lines) + "\n";
        }

        public string SerializePatternSlotCsv()
        {
            var lines = new List<string>
            {
                "cluster_id,slot_id,chunk_x,chunk_y,slot_kind,allowed_pattern_ids,biome_id,protected_mask_policy,risk_budget,canonical_digest",
            };
            lines.AddRange(PatternSlots.Select(value => MoonPalaceProductionCsv.Join(
                value.ClusterId, value.SlotId,
                value.ChunkX.ToString(CultureInfo.InvariantCulture),
                value.ChunkY.ToString(CultureInfo.InvariantCulture), value.SlotKind.ToString(),
                string.Join("|", value.AllowedPatternIds), value.BiomeId,
                value.ProtectedMaskPolicy, value.RiskBudget.ToString(CultureInfo.InvariantCulture),
                value.CanonicalDigest)));
            return string.Join("\n", lines) + "\n";
        }

        public static string ComputeFootprintDigest(
            IEnumerable<MoonPalaceClusterFootprintRecord> values) =>
            BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MAP21_03_CLUSTER_FOOTPRINTS_V1" }.Concat((values ??
                    Array.Empty<MoonPalaceClusterFootprintRecord>())
                    .OrderBy(value => value.ClusterId, StringComparer.Ordinal)
                    .ThenBy(value => value.ChunkY).ThenBy(value => value.ChunkX)
                    .Select(value => value.CanonicalLine)));

        public static string ComputeSpineVariantDigest(
            IEnumerable<MoonPalaceClusterSpineVariantRecord> values) =>
            BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MAP21_03_CLUSTER_SPINES_V1" }.Concat((values ??
                    Array.Empty<MoonPalaceClusterSpineVariantRecord>())
                    .OrderBy(value => value.ClusterId, StringComparer.Ordinal)
                    .ThenBy(value => value.SpineVariantId, StringComparer.Ordinal)
                    .Select(value => value.CanonicalLine)));

        public static string ComputePatternSlotDigest(
            IEnumerable<MoonPalaceClusterPatternSlotRecord> values) =>
            BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MAP21_03_CLUSTER_PATTERN_SLOTS_V1" }.Concat((values ??
                    Array.Empty<MoonPalaceClusterPatternSlotRecord>())
                    .OrderBy(value => value.ClusterId, StringComparer.Ordinal)
                    .ThenBy(value => value.SlotId, StringComparer.Ordinal)
                    .Select(value => value.CanonicalLine)));

        private static void ValidateFootprint(MoonPalaceClusterCatalogRecord record,
            IReadOnlyCollection<MoonPalaceClusterFootprintRecord> footprint)
        {
            if (footprint.Count != record.ActiveChunkCount ||
                footprint.Select(value => value.CoordinateToken).Distinct(
                    StringComparer.Ordinal).Count() != footprint.Count)
                throw new ArgumentException("Footprint count/coordinates mismatch: " +
                    record.ClusterId);
            if (footprint.Min(value => value.ChunkX) != 0 ||
                footprint.Min(value => value.ChunkY) != 0)
                throw new ArgumentException("Footprint must be normalized: " + record.ClusterId);
            if (footprint.Count(value => value.IsEntryChunk) != 1 ||
                footprint.Count(value => value.IsExitChunk) != 1)
                throw new ArgumentException("Footprint requires one entry and exit chunk: " +
                    record.ClusterId);
            var expectedRange = record.PoolKind == MoonPalaceClusterPoolKind.Quiet
                ? record.ActiveChunkCount == 2
                : record.PoolKind == MoonPalaceClusterPoolKind.Buffer
                    ? record.ActiveChunkCount >= 2 && record.ActiveChunkCount <= 3
                    : record.ActiveChunkCount >= 3 && record.ActiveChunkCount <= 5;
            if (!expectedRange)
                throw new ArgumentException("Pool active chunk range mismatch: " + record.ClusterId);
            var all = new HashSet<string>(footprint.Select(value => value.CoordinateToken),
                StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var queue = new Queue<MoonPalaceClusterFootprintRecord>();
            queue.Enqueue(footprint.First());
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!visited.Add(current.CoordinateToken)) continue;
                foreach (var candidate in footprint)
                    if (!visited.Contains(candidate.CoordinateToken) &&
                        Math.Abs(candidate.ChunkX - current.ChunkX) +
                        Math.Abs(candidate.ChunkY - current.ChunkY) == 1)
                        queue.Enqueue(candidate);
            }
            if (visited.Count != all.Count)
                throw new ArgumentException("Footprint must be 4-neighbor connected: " +
                    record.ClusterId);
        }

        private static void ValidateSpines(MoonPalaceClusterCatalogRecord record,
            IEnumerable<MoonPalaceClusterFootprintRecord> footprint,
            IReadOnlyCollection<MoonPalaceClusterSpineVariantRecord> spines)
        {
            if (spines.Count != 2 || spines.Count(value => value.IsBaseline) != 1 ||
                spines.Count(value => value.SpineVariantId.EndsWith("_BASE",
                    StringComparison.Ordinal)) != 1 ||
                spines.Count(value => value.SpineVariantId.EndsWith("_ALT",
                    StringComparison.Ordinal)) != 1)
                throw new ArgumentException("Exactly one BASE and one ALT are required: " +
                    record.ClusterId);
            var entry = footprint.Single(value => value.IsEntryChunk);
            var exit = footprint.Single(value => value.IsExitChunk);
            if (spines.Any(value => value.EntrySide != entry.EntrySide ||
                value.ExitSide != exit.ExitSide))
                throw new ArgumentException("Spine ports must match footprint ports: " +
                    record.ClusterId);
        }

        private static void ValidateSlots(MoonPalaceClusterCatalogRecord record,
            IEnumerable<MoonPalaceClusterFootprintRecord> footprint,
            IReadOnlyCollection<MoonPalaceClusterPatternSlotRecord> slots)
        {
            var footprintCoordinates = new HashSet<string>(footprint.Select(value =>
                value.CoordinateToken), StringComparer.Ordinal);
            if (slots.Count == 0 || slots.Any(value => value.BiomeId != record.BiomeId ||
                    !footprintCoordinates.Contains(value.CoordinateToken)) ||
                footprintCoordinates.Any(coordinate => !slots.Any(value =>
                    value.CoordinateToken == coordinate)))
                throw new ArgumentException("Every active chunk requires a same-biome slot: " +
                    record.ClusterId);
        }

        private static T[] Required<T>(IEnumerable<T> values, string parameterName)
            where T : class
        {
            var result = (values ?? throw new ArgumentNullException(parameterName)).ToArray();
            if (result.Any(value => value == null))
                throw new ArgumentException("Null records are not allowed.", parameterName);
            return result;
        }
    }

    public sealed class MoonPalaceClusterPoolDigestManifest
    {
        public const string SchemaVersion = "map21_03.cluster_digest_manifest.v1";

        public MoonPalaceClusterPoolDigestManifest(MoonPalaceClusterPoolProduction production,
            string map2104HandoffDigest, string createdUtc)
        {
            Production = production ?? throw new ArgumentNullException(nameof(production));
            Map2104HandoffDigest = MoonPalaceProfileValidation.Digest(map2104HandoffDigest,
                nameof(map2104HandoffDigest));
            CreatedUtc = createdUtc ?? string.Empty;
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                SchemaVersion, MoonPalaceClusterPoolPreconditions.TaskId,
                MoonPalaceClusterPoolPreconditions.SourceMap2102ResultDigest,
                MoonPalaceClusterPoolPreconditions.SourceMap2102TaskDigest,
                MoonPalaceClusterPoolPreconditions.SourceMap2103HandoffDigest,
                MoonPalaceClusterPoolPreconditions.SourcePatternCatalogDigest,
                MoonPalaceClusterPoolPreconditions.SourcePatternCellDigest,
                MoonPalaceClusterPoolPreconditions.SourcePatternTagDigest,
                Production.ClusterCatalogDigest, Production.ClusterFootprintDigest,
                Production.ClusterSpineVariantDigest, Production.ClusterPatternSlotDigest,
                Production.ClusterSignatureDigest, Map2104HandoffDigest,
                "created_utc_excluded=true",
            });
        }

        public MoonPalaceClusterPoolProduction Production { get; }
        public string Map2104HandoffDigest { get; }
        public string CreatedUtc { get; }
        public bool CreatedUtcExcludedFromCanonicalDigest => true;
        public string CanonicalDigest { get; }
        public string Serialize() => MoonPalaceCanonical.ToJson(
            MoonPalaceClusterDigestManifestDocument.From(this));
    }

    [Serializable]
    internal sealed class MoonPalaceClusterCatalogRecordDocument
    {
        public string cluster_id;
        public string schema_version;
        public string biome_id;
        public string pool_kind;
        public string pacing_role;
        public string access_class;
        public int active_chunk_count;
        public string source_starter_cluster_id;
        public string source_pattern_pool_digest;
        public string footprint_digest;
        public string spine_variant_digest;
        public string pattern_slot_digest;
        public string structural_signature;
        public string silhouette_signature;
        public string repeat_group;
        public bool created_utc_excluded_from_canonical_digest;
        public string canonical_digest;

        public static MoonPalaceClusterCatalogRecordDocument From(
            MoonPalaceClusterCatalogRecord value) => new MoonPalaceClusterCatalogRecordDocument
        {
            cluster_id = value.ClusterId,
            schema_version = MoonPalaceClusterCatalogRecord.SchemaVersion,
            biome_id = value.BiomeId,
            pool_kind = value.PoolKind.ToString(),
            pacing_role = PacingRoleTokenCodec.ToToken(value.PacingRole),
            access_class = AccessClassTokenCodec.ToToken(value.AccessClass),
            active_chunk_count = value.ActiveChunkCount,
            source_starter_cluster_id = value.SourceStarterClusterId,
            source_pattern_pool_digest = value.SourcePatternPoolDigest,
            footprint_digest = value.FootprintDigest,
            spine_variant_digest = value.SpineVariantDigest,
            pattern_slot_digest = value.PatternSlotDigest,
            structural_signature = value.StructuralSignature,
            silhouette_signature = value.SilhouetteSignature,
            repeat_group = value.RepeatGroup,
            created_utc_excluded_from_canonical_digest = true,
            canonical_digest = value.CanonicalDigest,
        };
    }

    [Serializable]
    internal sealed class MoonPalaceClusterFootprintRecordDocument
    {
        public string cluster_id;
        public int chunk_x;
        public int chunk_y;
        public string chunk_role;
        public string entry_side;
        public string exit_side;
        public bool is_entry_chunk;
        public bool is_exit_chunk;
        public string canonical_digest;

        public static MoonPalaceClusterFootprintRecordDocument From(
            MoonPalaceClusterFootprintRecord value) =>
            new MoonPalaceClusterFootprintRecordDocument
            {
                cluster_id = value.ClusterId,
                chunk_x = value.ChunkX,
                chunk_y = value.ChunkY,
                chunk_role = value.ChunkRole.ToString(),
                entry_side = value.EntrySide.ToString(),
                exit_side = value.ExitSide.ToString(),
                is_entry_chunk = value.IsEntryChunk,
                is_exit_chunk = value.IsExitChunk,
                canonical_digest = value.CanonicalDigest,
            };
    }

    [Serializable]
    internal sealed class MoonPalaceClusterSpineVariantRecordDocument
    {
        public string cluster_id;
        public string spine_variant_id;
        public bool is_baseline;
        public string entry_side;
        public string exit_side;
        public string route_intent;
        public string[] movement_tokens;
        public string high_route_state;
        public string recovery_state;
        public string protected_mask_policy;
        public string canonical_digest;

        public static MoonPalaceClusterSpineVariantRecordDocument From(
            MoonPalaceClusterSpineVariantRecord value) =>
            new MoonPalaceClusterSpineVariantRecordDocument
            {
                cluster_id = value.ClusterId,
                spine_variant_id = value.SpineVariantId,
                is_baseline = value.IsBaseline,
                entry_side = value.EntrySide.ToString(),
                exit_side = value.ExitSide.ToString(),
                route_intent = value.RouteIntent,
                movement_tokens = value.MovementTokens.Select(token => token.ToString()).ToArray(),
                high_route_state = value.HighRouteState,
                recovery_state = value.RecoveryState,
                protected_mask_policy = value.ProtectedMaskPolicy,
                canonical_digest = value.CanonicalDigest,
            };
    }

    [Serializable]
    internal sealed class MoonPalaceClusterPatternSlotRecordDocument
    {
        public string cluster_id;
        public string slot_id;
        public int chunk_x;
        public int chunk_y;
        public string slot_kind;
        public string[] allowed_pattern_ids;
        public string biome_id;
        public string protected_mask_policy;
        public int risk_budget;
        public string canonical_digest;

        public static MoonPalaceClusterPatternSlotRecordDocument From(
            MoonPalaceClusterPatternSlotRecord value) =>
            new MoonPalaceClusterPatternSlotRecordDocument
            {
                cluster_id = value.ClusterId,
                slot_id = value.SlotId,
                chunk_x = value.ChunkX,
                chunk_y = value.ChunkY,
                slot_kind = value.SlotKind.ToString(),
                allowed_pattern_ids = value.AllowedPatternIds.ToArray(),
                biome_id = value.BiomeId,
                protected_mask_policy = value.ProtectedMaskPolicy,
                risk_budget = value.RiskBudget,
                canonical_digest = value.CanonicalDigest,
            };
    }

    [Serializable]
    internal sealed class MoonPalaceClusterPoolManifestDocument
    {
        public string schema_version;
        public string task_id;
        public string source_MAP21_03_handoff_digest;
        public MoonPalaceClusterCatalogRecordDocument[] cluster_catalog;
        public MoonPalaceClusterFootprintRecordDocument[] footprint_records;
        public MoonPalaceClusterSpineVariantRecordDocument[] spine_variant_records;
        public MoonPalaceClusterPatternSlotRecordDocument[] pattern_slot_records;
        public string cluster_catalog_digest;
        public string cluster_footprint_digest;
        public string cluster_spine_variant_digest;
        public string cluster_pattern_slot_digest;
        public int activity_slot_count;
        public int event_overlay_count;
        public int special_region_binding_count;
        public int population_slot_count;
        public int reward_anchor_count;
        public int runtime_binding_count;
        public int tilemap_write_count;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
        public string canonical_digest;

        public static MoonPalaceClusterPoolManifestDocument From(
            MoonPalaceClusterPoolProduction value) => new MoonPalaceClusterPoolManifestDocument
        {
            schema_version = MoonPalaceClusterPoolProduction.SchemaVersion,
            task_id = MoonPalaceClusterPoolPreconditions.TaskId,
            source_MAP21_03_handoff_digest =
                MoonPalaceClusterPoolPreconditions.SourceMap2103HandoffDigest,
            cluster_catalog = value.Catalog.Select(
                MoonPalaceClusterCatalogRecordDocument.From).ToArray(),
            footprint_records = value.Footprints.Select(
                MoonPalaceClusterFootprintRecordDocument.From).ToArray(),
            spine_variant_records = value.SpineVariants.Select(
                MoonPalaceClusterSpineVariantRecordDocument.From).ToArray(),
            pattern_slot_records = value.PatternSlots.Select(
                MoonPalaceClusterPatternSlotRecordDocument.From).ToArray(),
            cluster_catalog_digest = value.ClusterCatalogDigest,
            cluster_footprint_digest = value.ClusterFootprintDigest,
            cluster_spine_variant_digest = value.ClusterSpineVariantDigest,
            cluster_pattern_slot_digest = value.ClusterPatternSlotDigest,
            activity_slot_count = value.ActivitySlotCount,
            event_overlay_count = value.EventOverlayCount,
            special_region_binding_count = value.SpecialRegionBindingCount,
            population_slot_count = value.PopulationSlotCount,
            reward_anchor_count = value.RewardAnchorCount,
            runtime_binding_count = value.RuntimeBindingCount,
            tilemap_write_count = value.TilemapWriteCount,
            created_utc = value.CreatedUtc,
            created_utc_excluded_from_canonical_digest = true,
            canonical_digest = value.PoolManifestDigest,
        };
    }

    [Serializable]
    internal sealed class MoonPalaceClusterSignatureDocument
    {
        public string cluster_id;
        public string biome_id;
        public string pool_kind;
        public string repeat_group;
        public string structural_signature;
        public string silhouette_signature;
        public string source_starter_cluster_id;
        public string canonical_digest;

        public static MoonPalaceClusterSignatureDocument From(
            MoonPalaceClusterCatalogRecord value) => new MoonPalaceClusterSignatureDocument
        {
            cluster_id = value.ClusterId,
            biome_id = value.BiomeId,
            pool_kind = value.PoolKind.ToString(),
            repeat_group = value.RepeatGroup,
            structural_signature = value.StructuralSignature,
            silhouette_signature = value.SilhouetteSignature,
            source_starter_cluster_id = value.SourceStarterClusterId,
            canonical_digest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP21_03_CLUSTER_SIGNATURE_RECORD_V1", value.ClusterId,
                value.StructuralSignature, value.SilhouetteSignature, value.RepeatGroup,
            }),
        };
    }

    [Serializable]
    internal sealed class MoonPalaceClusterSignatureManifestDocument
    {
        public string schema_version;
        public string task_id;
        public MoonPalaceClusterSignatureDocument[] signatures;
        public string cluster_signature_digest;
        public int structural_signature_duplicates;
        public int silhouette_signature_duplicates_within_biome_pool;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
        public string canonical_digest;

        public static MoonPalaceClusterSignatureManifestDocument From(
            MoonPalaceClusterPoolProduction value) =>
            new MoonPalaceClusterSignatureManifestDocument
            {
                schema_version = MoonPalaceClusterPoolProduction.SchemaVersion,
                task_id = MoonPalaceClusterPoolPreconditions.TaskId,
                signatures = value.Catalog.Select(MoonPalaceClusterSignatureDocument.From)
                    .ToArray(),
                cluster_signature_digest = value.ClusterSignatureDigest,
                structural_signature_duplicates = 0,
                silhouette_signature_duplicates_within_biome_pool = 0,
                created_utc = value.CreatedUtc,
                created_utc_excluded_from_canonical_digest = true,
                canonical_digest = value.SignatureManifestDigest,
            };
    }

    [Serializable]
    internal sealed class MoonPalaceClusterDigestManifestDocument
    {
        public string schema_version;
        public string task_id;
        public string source_MAP21_02_result_digest;
        public string source_MAP21_02_task_digest;
        public string source_MAP21_03_handoff_digest;
        public string source_pattern_catalog_digest;
        public string source_pattern_cell_digest;
        public string source_pattern_tag_digest;
        public string cluster_catalog_digest;
        public string cluster_footprint_digest;
        public string cluster_spine_variant_digest;
        public string cluster_pattern_slot_digest;
        public string cluster_signature_digest;
        public string MAP21_04_handoff_digest;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
        public string canonical_digest;

        public static MoonPalaceClusterDigestManifestDocument From(
            MoonPalaceClusterPoolDigestManifest value) =>
            new MoonPalaceClusterDigestManifestDocument
            {
                schema_version = MoonPalaceClusterPoolDigestManifest.SchemaVersion,
                task_id = MoonPalaceClusterPoolPreconditions.TaskId,
                source_MAP21_02_result_digest =
                    MoonPalaceClusterPoolPreconditions.SourceMap2102ResultDigest,
                source_MAP21_02_task_digest =
                    MoonPalaceClusterPoolPreconditions.SourceMap2102TaskDigest,
                source_MAP21_03_handoff_digest =
                    MoonPalaceClusterPoolPreconditions.SourceMap2103HandoffDigest,
                source_pattern_catalog_digest =
                    MoonPalaceClusterPoolPreconditions.SourcePatternCatalogDigest,
                source_pattern_cell_digest =
                    MoonPalaceClusterPoolPreconditions.SourcePatternCellDigest,
                source_pattern_tag_digest =
                    MoonPalaceClusterPoolPreconditions.SourcePatternTagDigest,
                cluster_catalog_digest = value.Production.ClusterCatalogDigest,
                cluster_footprint_digest = value.Production.ClusterFootprintDigest,
                cluster_spine_variant_digest = value.Production.ClusterSpineVariantDigest,
                cluster_pattern_slot_digest = value.Production.ClusterPatternSlotDigest,
                cluster_signature_digest = value.Production.ClusterSignatureDigest,
                MAP21_04_handoff_digest = value.Map2104HandoffDigest,
                created_utc = value.CreatedUtc,
                created_utc_excluded_from_canonical_digest = true,
                canonical_digest = value.CanonicalDigest,
            };
    }
}
