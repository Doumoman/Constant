using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Data;

namespace StarNight.Map.WorldGeneration.MoonPalace.Visualization
{
    public sealed class MoonPalaceCatalogSourceSnapshot
    {
        public MoonPalaceCatalogSourceSnapshot(
            string relativePath,
            int recordCount,
            string headerHash,
            string contentSha256,
            IEnumerable<string> typedLoadErrors,
            IEnumerable<string> foreignKeyErrors,
            IEnumerable<string> duplicateIdErrors)
        {
            RelativePath = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
            RecordCount = recordCount;
            HeaderHash = headerHash ?? string.Empty;
            ContentSha256 = contentSha256 ?? string.Empty;
            TypedLoadErrors = ReadOnly(typedLoadErrors);
            ForeignKeyErrors = ReadOnly(foreignKeyErrors);
            DuplicateIdErrors = ReadOnly(duplicateIdErrors);
        }

        public string RelativePath { get; }
        public int RecordCount { get; }
        public string HeaderHash { get; }
        public string ContentSha256 { get; }
        public IReadOnlyList<string> TypedLoadErrors { get; }
        public IReadOnlyList<string> ForeignKeyErrors { get; }
        public IReadOnlyList<string> DuplicateIdErrors { get; }
        public bool IsValid => TypedLoadErrors.Count == 0 && ForeignKeyErrors.Count == 0 && DuplicateIdErrors.Count == 0;

        private static IReadOnlyList<string> ReadOnly(IEnumerable<string> values) =>
            new ReadOnlyCollection<string>((values ?? Array.Empty<string>()).OrderBy(x => x, StringComparer.Ordinal).ToList());
    }

    public sealed class MoonPalaceRuntimeCatalogSnapshot
    {
        private const string Root = "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace";

        private static readonly SourceSpec[] Specs =
        {
            S("MAP21_01/moonpalace_biome_profiles.csv", new[] { "biome_id" }, "biome_id"),
            S("MAP21_01/moonpalace_tile_shell.csv", new[] { "tile_code" }, "tile_code"),
            S("MAP21_02/moonpalace_micropattern_catalog.csv", new[] { "pattern_id" }, "pattern_id", "biome_id", "silhouette_family"),
            S("MAP21_02/moonpalace_micropattern_cells.csv", new[] { "pattern_id", "x", "y" }, "pattern_id", "x", "y", "tile_code"),
            S("MAP21_03/moonpalace_crater_root_cluster_catalog.csv", new[] { "cluster_id" }, "cluster_id", "biome_id"),
            S("MAP21_03/moonpalace_crater_root_cluster_spine_variants.csv", new[] { "cluster_id", "spine_variant_id" }, "cluster_id", "spine_variant_id"),
            S("MAP21_04/moonpalace_mill_dough_cluster_catalog.csv", new[] { "cluster_id" }, "cluster_id", "biome_id"),
            S("MAP21_04/moonpalace_mill_dough_cluster_spine_variants.csv", new[] { "cluster_id", "spine_variant_id" }, "cluster_id", "spine_variant_id"),
            S("MAP21_05/moonpalace_activity_profiles.csv", new[] { "activity_id" }, "activity_id", "biome_id", "primary_cluster_id"),
            S("MAP21_05/moonpalace_event_overlay_profiles.csv", new[] { "event_id" }, "event_id"),
            S("MAP21_06/moonpalace_boundary_candidates.csv", new[] { "candidate_id" }, "candidate_id", "pair_id", "biome_a", "biome_b"),
            S("MAP21_07/moonpalace_core_resource_profiles.csv", new[] { "region_id" }, "region_id", "biome_id"),
            S("MAP21_10/moonpalace_density_windows.csv", new[] { "window_id" }, "window_id", "minimum", "target", "maximum"),
            S("MAP21_11/moonpalace_qa_seed_manifest.csv", new[] { "seed_id" }, "seed_id", "seed_value", "locked"),
        };

        private MoonPalaceRuntimeCatalogSnapshot(
            IEnumerable<MoonPalaceCatalogSourceSnapshot> sources,
            IEnumerable<string> biomeIds,
            IEnumerable<string> tileCodes,
            IEnumerable<string> productionPatternIds,
            IEnumerable<string> terrainClusterIds,
            IEnumerable<string> spineVariantIds,
            IEnumerable<string> activityIds,
            IEnumerable<string> eventIds,
            IEnumerable<string> biomePairIds,
            IEnumerable<string> boundaryCandidateIds,
            IEnumerable<string> coreResourceRegionIds,
            IEnumerable<string> tuningDensityWindowIds,
            int productionPatternCellCount,
            int qaSeedValue,
            string canonicalDigest)
        {
            Sources = Objects(sources);
            BiomeIds = Strings(biomeIds);
            TileCodes = Strings(tileCodes);
            ProductionPatternIds = Strings(productionPatternIds);
            TerrainClusterIds = Strings(terrainClusterIds);
            SpineVariantIds = Strings(spineVariantIds);
            ActivityIds = Strings(activityIds);
            EventOverlayIds = Strings(eventIds);
            BiomePairIds = Strings(biomePairIds);
            BoundaryCandidateIds = Strings(boundaryCandidateIds);
            CoreResourceRegionIds = Strings(coreResourceRegionIds);
            TuningDensityWindowIds = Strings(tuningDensityWindowIds);
            ProductionPatternCellCount = productionPatternCellCount;
            QaSeedMpQa01 = qaSeedValue;
            CanonicalDigest = canonicalDigest ?? throw new ArgumentNullException(nameof(canonicalDigest));
        }

        public IReadOnlyList<MoonPalaceCatalogSourceSnapshot> Sources { get; }
        public IReadOnlyList<string> BiomeIds { get; }
        public IReadOnlyList<string> TileCodes { get; }
        public IReadOnlyList<string> ProductionPatternIds { get; }
        public IReadOnlyList<string> TerrainClusterIds { get; }
        public IReadOnlyList<string> SpineVariantIds { get; }
        public IReadOnlyList<string> ActivityIds { get; }
        public IReadOnlyList<string> EventOverlayIds { get; }
        public IReadOnlyList<string> BiomePairIds { get; }
        public IReadOnlyList<string> BoundaryCandidateIds { get; }
        public IReadOnlyList<string> CoreResourceRegionIds { get; }
        public IReadOnlyList<string> TuningDensityWindowIds { get; }
        public int ProductionPatternCellCount { get; }
        public int QaSeedMpQa01 { get; }
        public string CanonicalDigest { get; }

        public int BiomeCount => BiomeIds.Count;
        public int TileCodeCount => TileCodes.Count;
        public int ProductionMicroPatternCount => ProductionPatternIds.Count;
        public int TerrainClusterCount => TerrainClusterIds.Count;
        public int ClusterSpineVariantCount => SpineVariantIds.Count;
        public int ActivityProfileCount => ActivityIds.Count;
        public int EventOverlayProfileCount => EventOverlayIds.Count;
        public int BiomePairCount => BiomePairIds.Count;
        public int BoundaryCandidateCount => BoundaryCandidateIds.Count;
        public int CoreResourceRegionCount => CoreResourceRegionIds.Count;
        public int TuningDensityWindowCount => TuningDensityWindowIds.Count;
        public int TypedLoadErrorCount => Sources.Sum(x => x.TypedLoadErrors.Count);
        public int ForeignKeyErrorCount => Sources.Sum(x => x.ForeignKeyErrors.Count);
        public int DuplicateIdErrorCount => Sources.Sum(x => x.DuplicateIdErrors.Count);
        public bool IsUsable => Sources.Count == Specs.Length && Sources.All(x => x.IsValid) &&
                                BiomeCount > 0 && TileCodeCount > 0 && ProductionMicroPatternCount > 0 &&
                                TerrainClusterCount > 0 && ClusterSpineVariantCount > 0 && QaSeedMpQa01 != 0;

        public static IReadOnlyList<string> GetSourceRelativePaths() =>
            new ReadOnlyCollection<string>(Specs.Select(x => Combine(Root, x.RelativePath)).ToList());

        public static MoonPalaceRuntimeCatalogSnapshot Load(string projectRoot)
        {
            if (string.IsNullOrWhiteSpace(projectRoot)) throw new ArgumentException("Project root is required.", nameof(projectRoot));
            var root = Path.GetFullPath(projectRoot);
            var tables = Specs.Select(spec => LoadTable(root, spec)).ToList();

            var biomes = Ids(tables[0], "biome_id");
            var tiles = Ids(tables[1], "tile_code");
            var patterns = Ids(tables[2], "pattern_id");
            var clusters = Ids(tables[4], "cluster_id").Concat(Ids(tables[6], "cluster_id")).Distinct(StringComparer.Ordinal).ToArray();
            var spines = tables[5].Rows.Concat(tables[7].Rows)
                .Select(row => Value(row, "cluster_id") + "/" + Value(row, "spine_variant_id")).Distinct(StringComparer.Ordinal).ToArray();
            var activities = Ids(tables[8], "activity_id");
            var events = Ids(tables[9], "event_id");
            var pairs = Ids(tables[10], "pair_id");
            var boundaries = Ids(tables[10], "candidate_id");
            var resources = Ids(tables[11], "region_id");
            var windows = Ids(tables[12], "window_id");

            ValidateForeignKeys(tables[2], "biome_id", biomes, "biome");
            ValidateForeignKeys(tables[4], "biome_id", biomes, "biome");
            ValidateForeignKeys(tables[6], "biome_id", biomes, "biome");
            ValidateForeignKeys(tables[8], "biome_id", biomes, "biome");
            ValidateForeignKeys(tables[8], "primary_cluster_id", clusters, "terrain_cluster");
            ValidateForeignKeys(tables[10], "biome_a", biomes, "biome");
            ValidateForeignKeys(tables[10], "biome_b", biomes, "biome");
            ValidateForeignKeys(tables[11], "biome_id", biomes, "biome");
            ValidateForeignKeys(tables[3], "pattern_id", patterns, "production_micro_pattern");
            ValidateForeignKeys(tables[3], "tile_code", tiles, "tile_code");

            var qaValue = 0;
            var qa = tables[13].Rows.FirstOrDefault(row => Value(row, "seed_id") == "MP_QA_01");
            if (qa == null || !int.TryParse(Value(qa, "seed_value"), NumberStyles.Integer, CultureInfo.InvariantCulture, out qaValue))
                tables[13].TypedErrors.Add("MP_QA_01 seed_value is missing or is not an invariant Int32.");

            var sourceSnapshots = tables.Select(x => x.Freeze()).ToList();
            var digestLines = new List<string>();
            foreach (var source in sourceSnapshots.OrderBy(x => x.RelativePath, StringComparer.Ordinal))
            {
                digestLines.Add(string.Join("|", source.RelativePath, Invariant(source.RecordCount), source.HeaderHash, source.ContentSha256));
                digestLines.AddRange(source.TypedLoadErrors.Select(error => "TYPED|" + source.RelativePath + "|" + error));
                digestLines.AddRange(source.ForeignKeyErrors.Select(error => "FK|" + source.RelativePath + "|" + error));
                digestLines.AddRange(source.DuplicateIdErrors.Select(error => "DUP|" + source.RelativePath + "|" + error));
            }
            digestLines.Add("COUNTS|" + string.Join("|", biomes.Length, tiles.Length, patterns.Length, tables[3].Rows.Count,
                clusters.Length, spines.Length, activities.Length, events.Length, pairs.Length, boundaries.Length,
                resources.Length, windows.Length, qaValue).Replace(',', '.'));
            var digest = BakingCanonicalDigest.HashCanonicalLines(digestLines);

            return new MoonPalaceRuntimeCatalogSnapshot(sourceSnapshots, biomes, tiles, patterns, clusters, spines,
                activities, events, pairs, boundaries, resources, windows, tables[3].Rows.Count, qaValue, digest);
        }

        private static MutableTable LoadTable(string projectRoot, SourceSpec spec)
        {
            var relativePath = Combine(Root, spec.RelativePath);
            var table = new MutableTable(relativePath);
            var path = Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                table.TypedErrors.Add("SOURCE_MISSING");
                return table;
            }

            var bytes = File.ReadAllBytes(path);
            table.ContentSha = Sha256(bytes);
            var read = new Rfc4180CsvReader().Read(bytes, relativePath);
            if (!read.Success)
            {
                table.TypedErrors.AddRange(read.Errors.Select(x => x.ToString()));
                return table;
            }
            if (read.Records.Count == 0)
            {
                table.TypedErrors.Add("HEADER_MISSING");
                return table;
            }

            var header = read.Records[0].Fields.Select(x => x.Value).ToArray();
            table.HeaderHash = BakingCanonicalDigest.HashCanonicalText(string.Join(",", header));
            foreach (var required in spec.RequiredColumns.Where(required => !header.Contains(required, StringComparer.Ordinal)))
                table.TypedErrors.Add("REQUIRED_COLUMN_MISSING:" + required);

            for (var index = 1; index < read.Records.Count; index++)
            {
                var record = read.Records[index];
                if (record.Fields.Count != header.Length)
                {
                    table.TypedErrors.Add("FIELD_COUNT:" + Invariant(record.RecordNumber));
                    continue;
                }
                var row = new Dictionary<string, string>(StringComparer.Ordinal);
                for (var field = 0; field < header.Length; field++) row[header[field]] = record.Fields[field].Value;
                table.Rows.Add(row);
            }

            var duplicateKeys = table.Rows.GroupBy(row => string.Join("\u001f", spec.PrimaryKeyColumns.Select(key => Value(row, key))), StringComparer.Ordinal)
                .Where(group => string.IsNullOrEmpty(group.Key) || group.Count() > 1).OrderBy(group => group.Key, StringComparer.Ordinal);
            foreach (var duplicate in duplicateKeys)
                table.DuplicateErrors.Add("PRIMARY_KEY:" + duplicate.Key + ":COUNT=" + Invariant(duplicate.Count()));
            return table;
        }

        private static void ValidateForeignKeys(MutableTable table, string column, IEnumerable<string> targets, string owner)
        {
            var targetSet = new HashSet<string>(targets, StringComparer.Ordinal);
            foreach (var value in table.Rows.Select(row => Value(row, column)).Where(value => !targetSet.Contains(value)).Distinct(StringComparer.Ordinal))
                table.ForeignKeyErrors.Add("FOREIGN_KEY:" + column + "=" + value + ":OWNER=" + owner);
        }

        private static string[] Ids(MutableTable table, string column) => table.Rows.Select(row => Value(row, column))
            .Where(value => !string.IsNullOrEmpty(value)).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        private static string Value(IReadOnlyDictionary<string, string> row, string column) => row.TryGetValue(column, out var value) ? value : string.Empty;
        private static string Combine(string left, string right) => left.TrimEnd('/') + "/" + right.TrimStart('/');
        private static string Invariant(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Sha256(byte[] bytes)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(bytes).Select(x => x.ToString("x2", CultureInfo.InvariantCulture)));
        }
        private static SourceSpec S(string path, string[] keys, params string[] columns) => new SourceSpec(path, keys, columns);
        private static IReadOnlyList<string> Strings(IEnumerable<string> values) =>
            new ReadOnlyCollection<string>((values ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList());
        private static IReadOnlyList<T> Objects<T>(IEnumerable<T> values) => new ReadOnlyCollection<T>((values ?? Array.Empty<T>()).ToList());

        private sealed class SourceSpec
        {
            public SourceSpec(string relativePath, string[] keys, string[] columns)
            {
                RelativePath = relativePath;
                PrimaryKeyColumns = keys;
                RequiredColumns = columns;
            }
            public string RelativePath { get; }
            public string[] PrimaryKeyColumns { get; }
            public string[] RequiredColumns { get; }
        }

        private sealed class MutableTable
        {
            public MutableTable(string path) { RelativePath = path; }
            public string RelativePath { get; }
            public string HeaderHash { get; set; } = string.Empty;
            public string ContentSha { get; set; } = string.Empty;
            public List<Dictionary<string, string>> Rows { get; } = new List<Dictionary<string, string>>();
            public List<string> TypedErrors { get; } = new List<string>();
            public List<string> ForeignKeyErrors { get; } = new List<string>();
            public List<string> DuplicateErrors { get; } = new List<string>();
            public MoonPalaceCatalogSourceSnapshot Freeze() => new MoonPalaceCatalogSourceSnapshot(
                RelativePath, Rows.Count, HeaderHash, ContentSha, TypedErrors, ForeignKeyErrors, DuplicateErrors);
        }
    }
}
