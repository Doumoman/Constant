using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace
{
    public static class MoonPalaceMillDoughClusterPoolPreconditions
    {
        public const string TaskId = "MAP21_04_EXPAND_MILL_AND_DOUGH_CLUSTER_POOLS";
        public const string SourceMap2103ResultDigest =
            "279bf1c735a81a8c49da62a1e2d84567ef0691987f19a13634ef750579c64d08";
        public const string SourceMap2103TaskDigest =
            "1a414d0bbbb90fa048e7b5767ca027f07f74f8c9a5f4060dfe4ce3840c6ca2a8";
        public const string SourceMap2104HandoffDigest =
            "d0f96239ae6a8dc44ff11e36ff387618565c834f69bd5a18dc307994b71fef3c";
        public const string SourcePatternCatalogDigest =
            "64765ac5aeccf149ebf635c54519a58ecc2629f98e6cacd285ee1bfacb77c560";
        public const string SourcePatternCellDigest =
            "d5d9fc1a8ff1b4e019c1fa779731ecbc80ec4a02da695c409ec9ccd4046b01ec";
        public const string SourcePatternTagDigest =
            "35a9ce555c91ebc786fc90f142250bd931de04de40fbeaef665ddb8398cd1c62";
        public const string SourceCraterRootCatalogDigest =
            "6ce0618c6973993f9713e0ac24fc4fc23db08cd13836766fe477b9afef333cc0";
        public const string SourceCraterRootFootprintDigest =
            "638319ac268c2d754e1ab8f6d9f4dd1d42ac4d6dcea20fb0054dd1ba8e8118ad";
        public const string SourceCraterRootSpineVariantDigest =
            "fd1ab5dd284d203065439373ca1dab13b597baf215f4fce78dd16760aab70bdd";
        public const string SourceCraterRootPatternSlotDigest =
            "053dc0673c87d8e067786a61b0713c9ce3926025d737363b67647232dfd73842";
        public const string SourceCraterRootSignatureDigest =
            "df017920326a4c82ec22358c51f70f04b0faf34c589d5b983bb0427410eb4a8f";

        public static string SourceCraterRootClusterDigest =>
            BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP21_04_SOURCE_CRATER_ROOT_CLUSTER_DIGEST_V1",
                SourceCraterRootCatalogDigest, SourceCraterRootFootprintDigest,
                SourceCraterRootSpineVariantDigest, SourceCraterRootPatternSlotDigest,
                SourceCraterRootSignatureDigest,
            });
    }

    public sealed class MoonPalaceMillDoughClusterPoolProduction
    {
        public const string SchemaVersion = "map21_04.mill_dough_cluster_pool.v1";
        private static readonly ReadOnlyCollection<string> requiredClusterIds =
            new ReadOnlyCollection<string>(new[]
            {
                "TC_MILL_PROD_BEAM_OVERHANG", "TC_MILL_PROD_BROKEN_PILLAR",
                "TC_MILL_PROD_ORTHOGONAL_SHAFT", "TC_MILL_PROD_GEAR_GALLERY",
                "TC_MILL_PROD_RUST_LEDGE", "TC_MILL_PROD_FALL_RECOVERY",
                "TC_MILL_QUIET_BEAM_WALK", "TC_MILL_QUIET_RUST_BALCONY",
                "TC_MILL_QUIET_GEAR_SHADOW", "TC_MILL_BUFFER_ENTRY_BEAM",
                "TC_MILL_BUFFER_EXIT_SHAFT", "TC_MILL_BUFFER_RECOVERY_PLATFORM",
                "TC_DOUGH_PROD_BOUNCE_CUP", "TC_DOUGH_PROD_SOFT_POCKET",
                "TC_DOUGH_PROD_STICKY_SHELF", "TC_DOUGH_PROD_FERMENT_RISE",
                "TC_DOUGH_PROD_RECOVERY_PAD_CHAIN", "TC_DOUGH_PROD_SQUISH_CROSS",
                "TC_DOUGH_QUIET_SOFT_SHELF", "TC_DOUGH_QUIET_FERMENT_POCKET",
                "TC_DOUGH_QUIET_RECOVERY_PAD", "TC_DOUGH_BUFFER_ENTRY_CUP",
                "TC_DOUGH_BUFFER_EXIT_SHELF", "TC_DOUGH_BUFFER_RECOVERY_BOWL",
            }.OrderBy(value => value, StringComparer.Ordinal).ToArray());
        private static readonly ReadOnlyCollection<string> allowedSourceClusterIds =
            new ReadOnlyCollection<string>(new[]
            {
                "TC_MILL_BEAM_OVERHANG", "TC_MILL_BROKEN_PILLAR",
                "TC_MILL_ORTHOGONAL_SHAFT_RECOVERY", "TC_MILL_QUIET_BEAM",
                "TC_DOUGH_BOUNCE_CUP", "TC_DOUGH_QUIET_SHELF",
                "TC_DOUGH_SOFT_POCKET", "TC_DOUGH_STICKY_RISE_RECOVERY",
            });
        private static readonly ReadOnlyCollection<string> allowedRepeatGroups =
            new ReadOnlyCollection<string>(new[]
            {
                "MillTerrain", "MillQuiet", "MillBuffer",
                "DoughTerrain", "DoughQuiet", "DoughBuffer",
            });

        private readonly MoonPalaceClusterPoolProduction millDough;
        private readonly ReadOnlyCollection<MoonPalaceClusterCatalogRecord> craterRootCatalog;
        private readonly ReadOnlyCollection<MoonPalaceClusterCatalogRecord> allBiomeCatalog;

        public MoonPalaceMillDoughClusterPoolProduction(
            IEnumerable<MoonPalaceClusterCatalogRecord> sourceCatalog,
            IEnumerable<MoonPalaceClusterFootprintRecord> sourceFootprints,
            IEnumerable<MoonPalaceClusterSpineVariantRecord> sourceSpineVariants,
            IEnumerable<MoonPalaceClusterPatternSlotRecord> sourcePatternSlots,
            IEnumerable<string> hazardPatternIds,
            IEnumerable<string> bufferForbiddenMarkerPatternIds,
            IEnumerable<MoonPalaceClusterCatalogRecord> sourceCraterRootCatalog,
            string createdUtc)
        {
            millDough = new MoonPalaceClusterPoolProduction(sourceCatalog, sourceFootprints,
                sourceSpineVariants, sourcePatternSlots, hazardPatternIds,
                bufferForbiddenMarkerPatternIds, createdUtc, requiredClusterIds,
                new[] { "AbandonedMill", "MoonDough" }, allowedRepeatGroups,
                allowedSourceClusterIds, 8, "MAP21_04");

            var sourcePrior = (sourceCraterRootCatalog ?? throw new ArgumentNullException(
                    nameof(sourceCraterRootCatalog))).ToArray();
            if (sourcePrior.Any(value => value == null))
                throw new ArgumentException("Null Crater/Root records are not allowed.",
                    nameof(sourceCraterRootCatalog));
            var prior = sourcePrior.OrderBy(value => value.ClusterId,
                StringComparer.Ordinal).ToArray();
            if (!prior.Select(value => value.ClusterId).SequenceEqual(
                    MoonPalaceClusterPoolProduction.RequiredClusterIds, StringComparer.Ordinal) ||
                prior.Any(value => value.BiomeId != "MoonCrater" &&
                    value.BiomeId != "CassiaRoot"))
                throw new ArgumentException("Exact read-only MAP21_03 catalog is required.",
                    nameof(sourceCraterRootCatalog));
            var sourceCatalogDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MAP21_03_CLUSTER_CATALOG_V1" }.Concat(prior.Select(value =>
                    value.CanonicalLine)));
            if (sourceCatalogDigest !=
                MoonPalaceMillDoughClusterPoolPreconditions.SourceCraterRootCatalogDigest)
                throw new ArgumentException("MAP21_03 catalog digest mismatch.",
                    nameof(sourceCraterRootCatalog));

            var combined = prior.Concat(millDough.Catalog).OrderBy(value => value.ClusterId,
                StringComparer.Ordinal).ToArray();
            if (combined.Length != 48 || combined.Select(value => value.ClusterId)
                    .Distinct(StringComparer.Ordinal).Count() != 48)
                throw new ArgumentException("The all-biome catalog requires 48 unique records.");
            if (combined.Select(value => value.StructuralSignature)
                    .Distinct(StringComparer.Ordinal).Count() != 48)
                throw new ArgumentException(
                    "Structural signatures must be unique across all 48 clusters.");

            craterRootCatalog = new ReadOnlyCollection<MoonPalaceClusterCatalogRecord>(prior);
            allBiomeCatalog = new ReadOnlyCollection<MoonPalaceClusterCatalogRecord>(combined);
            CreatedUtc = createdUtc ?? string.Empty;
            MillDoughPoolManifestDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP21_04_MILL_DOUGH_CLUSTER_POOL_MANIFEST_V1", SchemaVersion,
                MoonPalaceMillDoughClusterPoolPreconditions.SourceMap2104HandoffDigest,
                ClusterCatalogDigest, ClusterFootprintDigest, ClusterSpineVariantDigest,
                ClusterPatternSlotDigest, "created_utc_excluded=true",
            });
            MillDoughSignatureManifestDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP21_04_MILL_DOUGH_CLUSTER_SIGNATURE_MANIFEST_V1", SchemaVersion,
                ClusterSignatureDigest, "created_utc_excluded=true",
            });
            AllBiomeClusterPoolDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP21_04_ALL_BIOME_CLUSTER_POOL_MANIFEST_V1", SchemaVersion,
                MoonPalaceMillDoughClusterPoolPreconditions.SourceCraterRootClusterDigest,
                MoonPalaceMillDoughClusterPoolPreconditions.SourceCraterRootCatalogDigest,
                ClusterCatalogDigest,
            }.Concat(allBiomeCatalog.Select(value => value.CanonicalLine)).Concat(new[]
            {
                "created_utc_excluded=true",
            }));
            Map2105HandoffDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP21_05_MOONPALACE_ALL_BIOME_CLUSTER_POOL_HANDOFF_V1",
                MoonPalaceMillDoughClusterPoolPreconditions.SourceMap2104HandoffDigest,
                MoonPalaceMillDoughClusterPoolPreconditions.SourceCraterRootClusterDigest,
                ClusterCatalogDigest, ClusterFootprintDigest, ClusterSpineVariantDigest,
                ClusterPatternSlotDigest, ClusterSignatureDigest, AllBiomeClusterPoolDigest,
            });
        }

        public static IReadOnlyList<string> RequiredClusterIds => requiredClusterIds;
        public IReadOnlyList<MoonPalaceClusterCatalogRecord> Catalog => millDough.Catalog;
        public IReadOnlyList<MoonPalaceClusterFootprintRecord> Footprints => millDough.Footprints;
        public IReadOnlyList<MoonPalaceClusterSpineVariantRecord> SpineVariants =>
            millDough.SpineVariants;
        public IReadOnlyList<MoonPalaceClusterPatternSlotRecord> PatternSlots =>
            millDough.PatternSlots;
        public IReadOnlyList<MoonPalaceClusterCatalogRecord> CraterRootCatalog =>
            craterRootCatalog;
        public IReadOnlyList<MoonPalaceClusterCatalogRecord> AllBiomeCatalog => allBiomeCatalog;
        public string CreatedUtc { get; }
        public bool CreatedUtcExcludedFromCanonicalDigest => true;
        public int ActivitySlotCount => millDough.ActivitySlotCount;
        public int EventOverlayCount => millDough.EventOverlayCount;
        public int SpecialRegionBindingCount => millDough.SpecialRegionBindingCount;
        public int PopulationSlotCount => millDough.PopulationSlotCount;
        public int RewardAnchorCount => millDough.RewardAnchorCount;
        public int RuntimeBindingCount => millDough.RuntimeBindingCount;
        public int TilemapWriteCount => millDough.TilemapWriteCount;
        public int BufferLandmarkSearchExecutions => millDough.BufferLandmarkSearchExecutions;
        public int QuietHazardBaselineSlotCount => millDough.QuietHazardBaselineSlotCount;
        public int BufferForbiddenMarkerReferenceCount =>
            millDough.BufferForbiddenMarkerReferenceCount;
        public string ClusterCatalogDigest => millDough.ClusterCatalogDigest;
        public string ClusterFootprintDigest => millDough.ClusterFootprintDigest;
        public string ClusterSpineVariantDigest => millDough.ClusterSpineVariantDigest;
        public string ClusterPatternSlotDigest => millDough.ClusterPatternSlotDigest;
        public string ClusterSignatureDigest => millDough.ClusterSignatureDigest;
        public string MillDoughPoolManifestDigest { get; }
        public string MillDoughSignatureManifestDigest { get; }
        public string AllBiomeClusterPoolDigest { get; }
        public string Map2105HandoffDigest { get; }

        public string SerializeCatalogCsv() => millDough.SerializeCatalogCsv();
        public string SerializeFootprintCsv() => millDough.SerializeFootprintCsv();
        public string SerializeSpineVariantCsv() => millDough.SerializeSpineVariantCsv();
        public string SerializePatternSlotCsv() => millDough.SerializePatternSlotCsv();
        public string SerializePoolManifest() => MoonPalaceCanonical.ToJson(
            MoonPalaceMillDoughClusterPoolManifestDocument.From(this));
        public string SerializeSignatureManifest() => MoonPalaceCanonical.ToJson(
            MoonPalaceMillDoughClusterSignatureManifestDocument.From(this));
        public string SerializeAllBiomeManifest() => MoonPalaceCanonical.ToJson(
            MoonPalaceAllBiomeClusterPoolManifestDocument.From(this));
    }

    public sealed class MoonPalaceMillDoughClusterDigestManifest
    {
        public const string SchemaVersion = "map21_04.cluster_digest_manifest.v1";

        public MoonPalaceMillDoughClusterDigestManifest(
            MoonPalaceMillDoughClusterPoolProduction production, string createdUtc)
        {
            Production = production ?? throw new ArgumentNullException(nameof(production));
            CreatedUtc = createdUtc ?? string.Empty;
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                SchemaVersion, MoonPalaceMillDoughClusterPoolPreconditions.TaskId,
                MoonPalaceMillDoughClusterPoolPreconditions.SourceMap2103ResultDigest,
                MoonPalaceMillDoughClusterPoolPreconditions.SourceMap2103TaskDigest,
                MoonPalaceMillDoughClusterPoolPreconditions.SourceMap2104HandoffDigest,
                MoonPalaceMillDoughClusterPoolPreconditions.SourcePatternCatalogDigest,
                MoonPalaceMillDoughClusterPoolPreconditions.SourcePatternCellDigest,
                MoonPalaceMillDoughClusterPoolPreconditions.SourcePatternTagDigest,
                MoonPalaceMillDoughClusterPoolPreconditions.SourceCraterRootClusterDigest,
                Production.ClusterCatalogDigest, Production.ClusterFootprintDigest,
                Production.ClusterSpineVariantDigest, Production.ClusterPatternSlotDigest,
                Production.ClusterSignatureDigest, Production.AllBiomeClusterPoolDigest,
                Production.Map2105HandoffDigest, "created_utc_excluded=true",
            });
        }

        public MoonPalaceMillDoughClusterPoolProduction Production { get; }
        public string CreatedUtc { get; }
        public bool CreatedUtcExcludedFromCanonicalDigest => true;
        public string CanonicalDigest { get; }
        public string Serialize() => MoonPalaceCanonical.ToJson(
            MoonPalaceMillDoughClusterDigestManifestDocument.From(this));
    }

    [Serializable]
    internal sealed class MoonPalaceMillDoughClusterPoolManifestDocument
    {
        public string schema_version;
        public string task_id;
        public string source_MAP21_04_handoff_digest;
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

        public static MoonPalaceMillDoughClusterPoolManifestDocument From(
            MoonPalaceMillDoughClusterPoolProduction value) =>
            new MoonPalaceMillDoughClusterPoolManifestDocument
            {
                schema_version = MoonPalaceMillDoughClusterPoolProduction.SchemaVersion,
                task_id = MoonPalaceMillDoughClusterPoolPreconditions.TaskId,
                source_MAP21_04_handoff_digest =
                    MoonPalaceMillDoughClusterPoolPreconditions.SourceMap2104HandoffDigest,
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
                canonical_digest = value.MillDoughPoolManifestDigest,
            };
    }

    [Serializable]
    internal sealed class MoonPalaceMillDoughClusterSignatureManifestDocument
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

        public static MoonPalaceMillDoughClusterSignatureManifestDocument From(
            MoonPalaceMillDoughClusterPoolProduction value) =>
            new MoonPalaceMillDoughClusterSignatureManifestDocument
            {
                schema_version = MoonPalaceMillDoughClusterPoolProduction.SchemaVersion,
                task_id = MoonPalaceMillDoughClusterPoolPreconditions.TaskId,
                signatures = value.Catalog.Select(MoonPalaceClusterSignatureDocument.From)
                    .ToArray(),
                cluster_signature_digest = value.ClusterSignatureDigest,
                structural_signature_duplicates = 0,
                silhouette_signature_duplicates_within_biome_pool = 0,
                created_utc = value.CreatedUtc,
                created_utc_excluded_from_canonical_digest = true,
                canonical_digest = value.MillDoughSignatureManifestDigest,
            };
    }

    [Serializable]
    internal sealed class MoonPalaceAllBiomeClusterPoolManifestDocument
    {
        public string schema_version;
        public string task_id;
        public string source_crater_root_cluster_digest;
        public string mill_dough_cluster_catalog_digest;
        public int cluster_record_count;
        public int MoonCrater_cluster_count;
        public int CassiaRoot_cluster_count;
        public int AbandonedMill_cluster_count;
        public int MoonDough_cluster_count;
        public MoonPalaceClusterCatalogRecordDocument[] cluster_catalog;
        public int combined_structural_signature_duplicates;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
        public string canonical_digest;

        public static MoonPalaceAllBiomeClusterPoolManifestDocument From(
            MoonPalaceMillDoughClusterPoolProduction value) =>
            new MoonPalaceAllBiomeClusterPoolManifestDocument
            {
                schema_version = "map21_04.all_biome_cluster_pool.v1",
                task_id = MoonPalaceMillDoughClusterPoolPreconditions.TaskId,
                source_crater_root_cluster_digest =
                    MoonPalaceMillDoughClusterPoolPreconditions.SourceCraterRootClusterDigest,
                mill_dough_cluster_catalog_digest = value.ClusterCatalogDigest,
                cluster_record_count = value.AllBiomeCatalog.Count,
                MoonCrater_cluster_count = value.AllBiomeCatalog.Count(record =>
                    record.BiomeId == "MoonCrater"),
                CassiaRoot_cluster_count = value.AllBiomeCatalog.Count(record =>
                    record.BiomeId == "CassiaRoot"),
                AbandonedMill_cluster_count = value.AllBiomeCatalog.Count(record =>
                    record.BiomeId == "AbandonedMill"),
                MoonDough_cluster_count = value.AllBiomeCatalog.Count(record =>
                    record.BiomeId == "MoonDough"),
                cluster_catalog = value.AllBiomeCatalog.Select(
                    MoonPalaceClusterCatalogRecordDocument.From).ToArray(),
                combined_structural_signature_duplicates = 0,
                created_utc = value.CreatedUtc,
                created_utc_excluded_from_canonical_digest = true,
                canonical_digest = value.AllBiomeClusterPoolDigest,
            };
    }

    [Serializable]
    internal sealed class MoonPalaceMillDoughClusterDigestManifestDocument
    {
        public string schema_version;
        public string task_id;
        public string source_MAP21_03_result_digest;
        public string source_MAP21_03_task_digest;
        public string source_MAP21_04_handoff_digest;
        public string source_pattern_catalog_digest;
        public string source_pattern_cell_digest;
        public string source_pattern_tag_digest;
        public string source_crater_root_cluster_digest;
        public string mill_dough_cluster_catalog_digest;
        public string mill_dough_cluster_footprint_digest;
        public string mill_dough_cluster_spine_variant_digest;
        public string mill_dough_cluster_pattern_slot_digest;
        public string mill_dough_cluster_signature_digest;
        public string all_biome_cluster_pool_digest;
        public string MAP21_05_handoff_digest;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
        public string canonical_digest;

        public static MoonPalaceMillDoughClusterDigestManifestDocument From(
            MoonPalaceMillDoughClusterDigestManifest value) =>
            new MoonPalaceMillDoughClusterDigestManifestDocument
            {
                schema_version = MoonPalaceMillDoughClusterDigestManifest.SchemaVersion,
                task_id = MoonPalaceMillDoughClusterPoolPreconditions.TaskId,
                source_MAP21_03_result_digest =
                    MoonPalaceMillDoughClusterPoolPreconditions.SourceMap2103ResultDigest,
                source_MAP21_03_task_digest =
                    MoonPalaceMillDoughClusterPoolPreconditions.SourceMap2103TaskDigest,
                source_MAP21_04_handoff_digest =
                    MoonPalaceMillDoughClusterPoolPreconditions.SourceMap2104HandoffDigest,
                source_pattern_catalog_digest =
                    MoonPalaceMillDoughClusterPoolPreconditions.SourcePatternCatalogDigest,
                source_pattern_cell_digest =
                    MoonPalaceMillDoughClusterPoolPreconditions.SourcePatternCellDigest,
                source_pattern_tag_digest =
                    MoonPalaceMillDoughClusterPoolPreconditions.SourcePatternTagDigest,
                source_crater_root_cluster_digest =
                    MoonPalaceMillDoughClusterPoolPreconditions.SourceCraterRootClusterDigest,
                mill_dough_cluster_catalog_digest = value.Production.ClusterCatalogDigest,
                mill_dough_cluster_footprint_digest = value.Production.ClusterFootprintDigest,
                mill_dough_cluster_spine_variant_digest =
                    value.Production.ClusterSpineVariantDigest,
                mill_dough_cluster_pattern_slot_digest =
                    value.Production.ClusterPatternSlotDigest,
                mill_dough_cluster_signature_digest = value.Production.ClusterSignatureDigest,
                all_biome_cluster_pool_digest = value.Production.AllBiomeClusterPoolDigest,
                MAP21_05_handoff_digest = value.Production.Map2105HandoffDigest,
                created_utc = value.CreatedUtc,
                created_utc_excluded_from_canonical_digest = true,
                canonical_digest = value.CanonicalDigest,
            };
    }
}
