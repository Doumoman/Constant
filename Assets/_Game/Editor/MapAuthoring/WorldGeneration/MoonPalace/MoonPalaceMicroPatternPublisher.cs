using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.MoonPalace;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor.WorldGeneration.MoonPalace
{
    public static class MoonPalaceMicroPatternPublisher
    {
        public const string AuthoringDirectoryRelativePath =
            "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_02";
        public const string GeneratedDirectoryRelativePath =
            "MapDesign/MCP/GENERATED/MAP21_02";
        public const string CatalogCsvFileName = "moonpalace_micropattern_catalog.csv";
        public const string CellCsvFileName = "moonpalace_micropattern_cells.csv";
        public const string TagCsvFileName = "moonpalace_micropattern_tags.csv";
        public const string CatalogManifestFileName =
            "moonpalace_micropattern_catalog_manifest.json";
        public const string CellManifestFileName =
            "moonpalace_micropattern_cell_manifest.json";
        public const string DigestManifestFileName =
            "moonpalace_micropattern_digest_manifest.json";

        public const string SourceMap10CatalogRelativePath =
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroPattern/micro_pattern_catalog_v2.csv";
        public const string SourceMap10CellsRelativePath =
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroPattern/micro_pattern_cells_v2.csv";
        public const string SourceMap2101TileShellManifestRelativePath =
            "MapDesign/MCP/GENERATED/MAP21_01/moonpalace_tile_shell_manifest.json";
        public const string SourceMap2101DigestManifestRelativePath =
            "MapDesign/MCP/GENERATED/MAP21_01/moonpalace_profile_digest_manifest.json";

        private const string SourceMap2101ResultRelativePath =
            "MapDesign/MCP/REPORTS/MAP21_01_LOCK_MOONPALACE_PROFILES_AND_TILE_SHELL_RESULT.md";
        private const string SourceMap2101TaskRelativePath =
            "MapDesign/MCP/TASKS/MAP21_01_LOCK_MOONPALACE_PROFILES_AND_TILE_SHELL.md";

        public static MoonPalaceMicroPatternPublishedSample CreateReadOnlySample(
            string projectRoot, string createdUtc, bool reverseInputOrder = false)
        {
            projectRoot = RequireProjectRoot(projectRoot);
            ValidateUpstreamPreconditions(projectRoot);
            var sourceCatalog = ReadRows(projectRoot, SourceMap10CatalogRelativePath);
            var sourceRows = ReadRows(projectRoot, SourceMap10CellsRelativePath);
            if (reverseInputOrder)
            {
                sourceCatalog.Reverse();
                sourceRows.Reverse();
            }

            var shell = ReadTileShell(projectRoot);
            var shellCodes = shell.tile_records.Select(value => value.tile_code).ToArray();
            var shellByRole = shell.tile_records.GroupBy(value => value.tile_role,
                    StringComparer.Ordinal).ToDictionary(group => group.Key,
                    group => Single(group, group.Key), StringComparer.Ordinal);
            var sourceByPattern = sourceRows.GroupBy(row => Value(row, "pattern_id"),
                StringComparer.Ordinal).ToDictionary(group => group.Key,
                group => group.ToArray(), StringComparer.Ordinal);

            var cells = new List<MoonPalaceProductionPatternCell>();
            var tags = new List<MoonPalaceProductionPatternTag>();
            foreach (var row in sourceRows)
            {
                var layer = Value(row, "layer");
                if (string.Equals(layer, "GEOMETRY", StringComparison.Ordinal))
                {
                    var patternId = Value(row, "pattern_id");
                    var operation = Value(row, "operation");
                    var role = ResolveCellRole(patternId, operation);
                    if (!shellByRole.TryGetValue(role.ToString(), out var shellRecord))
                        throw new InvalidDataException("MAP21_01 shell role is missing: " + role);
                    var collision = EnumToken<MoonPalaceCollisionKind>(
                        shellRecord.collision_kind, "collision_kind");
                    var assetKind = EnumToken<MoonPalaceAssetReferenceKind>(
                        shellRecord.asset_reference_kind, "asset_reference_kind");
                    cells.Add(new MoonPalaceProductionPatternCell(patternId,
                        Integer(row, "local_x"), Integer(row, "local_y"), operation,
                        shellRecord.tile_code, role, collision, shellRecord.material_token,
                        shellRecord.fallback_tile_code, assetKind, shellRecord.missing_reason,
                        ProtectedPolicy(sourceCatalog, patternId), shellCodes));
                }
                else
                {
                    var kind = TagKind(layer);
                    tags.Add(new MoonPalaceProductionPatternTag(Value(row, "pattern_id"),
                        Integer(row, "local_x"), Integer(row, "local_y"), kind,
                        Value(row, "payload_id"), Binding(kind), Risk(kind), Priority(kind)));
                }
            }

            var catalog = new List<MoonPalaceProductionPatternCatalogRecord>();
            var preservedSilhouettes = new List<string>();
            foreach (var row in sourceCatalog)
            {
                var patternId = Value(row, "pattern_id");
                if (!sourceByPattern.TryGetValue(patternId, out var sourcePatternRows))
                    throw new InvalidDataException("Starter pattern has no source rows: " + patternId);
                var patternCells = cells.Where(value => value.PatternId == patternId).ToArray();
                if (patternCells.Length != 16)
                    throw new InvalidDataException("Starter pattern is not exact 4x4: " + patternId);
                preservedSilhouettes.Add(patternId);
                var patternTags = tags.Where(value => value.PatternId == patternId).ToArray();
                var role = PatternRole(patternTags);
                catalog.Add(new MoonPalaceProductionPatternCatalogRecord(patternId, patternId,
                    StarterDigest(row, sourcePatternRows), Value(row, "biome_ids"), role,
                    SilhouetteFamily(patternId, Value(row, "biome_ids")),
                    Value(row, "allowed_transforms").Split(new[] { '|' },
                        StringSplitOptions.RemoveEmptyEntries), Integer(row, "selection_weight"),
                    MoonPalaceMicroPatternPreconditions.SourceTileShellDigest,
                    MoonPalaceMicroPatternProduction.ComputeCellRecordDigest(patternCells),
                    MoonPalaceMicroPatternProduction.ComputeTagRecordDigest(patternTags),
                    "PreserveMissingDataAndFallback", CatalogBinding(role, patternTags)));
            }

            var production = new MoonPalaceMicroPatternProduction(catalog, cells, tags,
                preservedSilhouettes, shell.tile_records.Length, cells.Count,
                cells.Count(value => value.AssetReferenceKind ==
                    MoonPalaceAssetReferenceKind.MissingData &&
                    !string.IsNullOrEmpty(value.FallbackTileCode)), 0, createdUtc);
            var map2103Handoff = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP21_03_MOONPALACE_MICROPATTERN_HANDOFF_V1",
                MoonPalaceMicroPatternPreconditions.SourceMap2102HandoffDigest,
                production.PatternCatalogDigest, production.PatternCellDigest,
                production.PatternTagDigest,
            });
            var digestManifest = new MoonPalaceMicroPatternDigestManifest(production,
                map2103Handoff, createdUtc);
            return new MoonPalaceMicroPatternPublishedSample(production, digestManifest,
                MoonPalaceMicroPatternForbiddenOperationCounters.Zero,
                Array.Empty<string>());
        }

        public static MoonPalaceMicroPatternPublishedSample PublishAuthoringAndSamples(
            string projectRoot, bool focusedMap2102Pass)
        {
            if (!focusedMap2102Pass)
                throw new InvalidOperationException(
                    "MAP21_03 handoff publication requires focused MAP21_02 PASS.");
            projectRoot = RequireProjectRoot(projectRoot);
            var sample = CreateReadOnlySample(projectRoot,
                DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            var authoringDirectory = Resolve(projectRoot, AuthoringDirectoryRelativePath);
            var generatedDirectory = Resolve(projectRoot, GeneratedDirectoryRelativePath);
            Directory.CreateDirectory(authoringDirectory);
            Directory.CreateDirectory(generatedDirectory);
            var relativePaths = new[]
            {
                CombineRelative(AuthoringDirectoryRelativePath, CatalogCsvFileName),
                CombineRelative(AuthoringDirectoryRelativePath, CellCsvFileName),
                CombineRelative(AuthoringDirectoryRelativePath, TagCsvFileName),
                CombineRelative(GeneratedDirectoryRelativePath, CatalogManifestFileName),
                CombineRelative(GeneratedDirectoryRelativePath, CellManifestFileName),
                CombineRelative(GeneratedDirectoryRelativePath, DigestManifestFileName),
            };
            Write(projectRoot, relativePaths[0], sample.Production.SerializeCatalogCsv());
            Write(projectRoot, relativePaths[1], sample.Production.SerializeCellCsv());
            Write(projectRoot, relativePaths[2], sample.Production.SerializeTagCsv());
            Write(projectRoot, relativePaths[3], sample.Production.SerializeCatalogManifest());
            Write(projectRoot, relativePaths[4], sample.Production.SerializeCellManifest());
            Write(projectRoot, relativePaths[5], sample.DigestManifest.Serialize());
            return new MoonPalaceMicroPatternPublishedSample(sample.Production,
                sample.DigestManifest, sample.Counters, relativePaths);
        }

        private static string ProtectedPolicy(
            IEnumerable<IReadOnlyDictionary<string, string>> catalog, string patternId) =>
            Value(catalog.Single(row => string.Equals(Value(row, "pattern_id"), patternId,
                StringComparison.Ordinal)), "protected_policy");

        private static MoonPalaceTileRole ResolveCellRole(string patternId, string operation)
        {
            if (operation == "NO_CHANGE") return MoonPalaceTileRole.BoundaryBlend;
            if (operation == "CARVE_AIR") return MoonPalaceTileRole.Background;
            if (operation != "ADD_SOLID")
                throw new InvalidDataException("Unsupported geometry operation: " + operation);
            if (patternId.IndexOf("SLOPE", StringComparison.Ordinal) >= 0)
                return MoonPalaceTileRole.SlopeOrStep;
            if (patternId.IndexOf("SHELF", StringComparison.Ordinal) >= 0)
                return MoonPalaceTileRole.OneWayPlatform;
            if (patternId.IndexOf("ARCH", StringComparison.Ordinal) >= 0 ||
                patternId.IndexOf("PILLAR", StringComparison.Ordinal) >= 0)
                return MoonPalaceTileRole.Wall;
            if (patternId.IndexOf("OVERHANG", StringComparison.Ordinal) >= 0)
                return MoonPalaceTileRole.Ceiling;
            return MoonPalaceTileRole.Ground;
        }

        private static MoonPalaceProductionPatternRole PatternRole(
            IReadOnlyCollection<MoonPalaceProductionPatternTag> tags)
        {
            if (tags.Count == 0)
                return MoonPalaceProductionPatternRole.GeometrySilhouette;
            if (tags.Any(value => value.TagKind == MoonPalaceProductionTagKind.Affordance))
                return MoonPalaceProductionPatternRole.SurfaceAffordance;
            return MoonPalaceProductionPatternRole.MaterialHazardMarker;
        }

        private static MoonPalaceRuntimeBindingState CatalogBinding(
            MoonPalaceProductionPatternRole role,
            IEnumerable<MoonPalaceProductionPatternTag> tags)
        {
            if (role == MoonPalaceProductionPatternRole.GeometrySilhouette)
                return MoonPalaceRuntimeBindingState.MissingData;
            if (tags.Any(value => value.TagKind == MoonPalaceProductionTagKind.Hazard ||
                value.TagKind == MoonPalaceProductionTagKind.Affordance))
                return MoonPalaceRuntimeBindingState.NeedsRuntimeBinding;
            return MoonPalaceRuntimeBindingState.MissingData;
        }

        private static MoonPalaceProductionTagKind TagKind(string sourceLayer)
        {
            if (!Enum.TryParse(ToPascal(sourceLayer), false,
                    out MoonPalaceProductionTagKind value) ||
                !Enum.IsDefined(typeof(MoonPalaceProductionTagKind), value))
                throw new InvalidDataException("Unsupported MAP10 tag layer: " + sourceLayer);
            return value;
        }

        private static MoonPalaceRuntimeBindingState Binding(MoonPalaceProductionTagKind kind)
        {
            if (kind == MoonPalaceProductionTagKind.Affordance ||
                kind == MoonPalaceProductionTagKind.Hazard)
                return MoonPalaceRuntimeBindingState.NeedsRuntimeBinding;
            if (kind == MoonPalaceProductionTagKind.Material)
                return MoonPalaceRuntimeBindingState.MissingData;
            return MoonPalaceRuntimeBindingState.AuthoringOnly;
        }

        private static string Risk(MoonPalaceProductionTagKind kind)
        {
            if (kind == MoonPalaceProductionTagKind.Hazard) return "High";
            if (kind == MoonPalaceProductionTagKind.Affordance) return "Medium";
            return "Low";
        }

        private static int Priority(MoonPalaceProductionTagKind kind)
        {
            switch (kind)
            {
                case MoonPalaceProductionTagKind.Surface: return 20;
                case MoonPalaceProductionTagKind.Material: return 30;
                case MoonPalaceProductionTagKind.Affordance: return 40;
                case MoonPalaceProductionTagKind.Hazard: return 50;
                case MoonPalaceProductionTagKind.Marker: return 60;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private static string SilhouetteFamily(string patternId, string biomeId)
        {
            string prefix;
            switch (biomeId)
            {
                case "MoonCrater": prefix = "MP_CRATER_"; break;
                case "CassiaRoot": prefix = "MP_ROOT_"; break;
                case "AbandonedMill": prefix = "MP_MILL_"; break;
                case "MoonDough": prefix = "MP_DOUGH_"; break;
                default: throw new InvalidDataException("Unknown MoonPalace biome: " + biomeId);
            }
            if (!patternId.StartsWith(prefix, StringComparison.Ordinal))
                throw new InvalidDataException("Starter id and biome prefix disagree: " + patternId);
            return patternId.Substring(prefix.Length);
        }

        private static string StarterDigest(IReadOnlyDictionary<string, string> catalog,
            IEnumerable<IReadOnlyDictionary<string, string>> sourceRows)
        {
            var lines = new List<string>
            {
                "MAP10_STARTER_MICROPATTERN_SOURCE_V1",
                Pack(Value(catalog, "pattern_id"), Value(catalog, "selection_weight"),
                    Value(catalog, "biome_ids"), Value(catalog, "allowed_transforms"),
                    Value(catalog, "protected_policy")),
            };
            lines.AddRange(sourceRows.OrderBy(row => Value(row, "layer"), StringComparer.Ordinal)
                .ThenBy(row => Integer(row, "local_y"))
                .ThenBy(row => Integer(row, "local_x"))
                .ThenBy(row => Value(row, "operation"), StringComparer.Ordinal)
                .ThenBy(row => Value(row, "payload_id", false), StringComparer.Ordinal)
                .Select(row => Pack(Value(row, "pattern_id"), Value(row, "local_x"),
                    Value(row, "local_y"), Value(row, "operation"), Value(row, "layer"),
                    Value(row, "payload_id", false))));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        private static string Pack(params string[] values) => string.Join("/", values.Select(
            value => (value ?? string.Empty).Length.ToString(CultureInfo.InvariantCulture) +
                ":" + (value ?? string.Empty)));

        private static MoonPalaceSourceTileShellDocument ReadTileShell(string projectRoot)
        {
            var json = File.ReadAllText(Resolve(projectRoot,
                SourceMap2101TileShellManifestRelativePath));
            var value = JsonUtility.FromJson<MoonPalaceSourceTileShellDocument>(json);
            if (value == null || value.tile_records == null || value.tile_records.Length == 0 ||
                !string.Equals(value.canonical_digest,
                    MoonPalaceMicroPatternPreconditions.SourceTileShellDigest,
                    StringComparison.Ordinal))
                throw new InvalidDataException("MAP21_01 tile shell manifest is invalid.");
            if (value.tile_records.GroupBy(record => record.tile_code, StringComparer.Ordinal)
                .Any(group => group.Count() != 1))
                throw new InvalidDataException("MAP21_01 tile shell has duplicate codes.");
            return value;
        }

        private static void ValidateUpstreamPreconditions(string projectRoot)
        {
            if (FileSha256(Resolve(projectRoot, SourceMap2101ResultRelativePath)) !=
                MoonPalaceMicroPatternPreconditions.SourceMap2101ResultDigest)
                throw new InvalidDataException("MAP21_01 Result SHA-256 mismatch.");
            if (FileSha256(Resolve(projectRoot, SourceMap2101TaskRelativePath)) !=
                MoonPalaceMicroPatternPreconditions.SourceMap2101TaskDigest)
                throw new InvalidDataException("MAP21_01 Task SHA-256 mismatch.");
            var json = File.ReadAllText(Resolve(projectRoot,
                SourceMap2101DigestManifestRelativePath));
            var source = JsonUtility.FromJson<MoonPalaceSourceDigestManifestDocument>(json);
            if (source == null ||
                source.MAP21_02_handoff_digest !=
                    MoonPalaceMicroPatternPreconditions.SourceMap2102HandoffDigest ||
                source.biome_profile_digest !=
                    MoonPalaceMicroPatternPreconditions.SourceBiomeProfileDigest ||
                source.tile_shell_digest !=
                    MoonPalaceMicroPatternPreconditions.SourceTileShellDigest)
                throw new InvalidDataException("MAP21_01 handoff digest chain mismatch.");
        }

        private static List<IReadOnlyDictionary<string, string>> ReadRows(
            string projectRoot, string relativePath)
        {
            var read = new Rfc4180CsvReader().Read(File.ReadAllBytes(
                Resolve(projectRoot, relativePath)), relativePath);
            if (!read.Success || read.Records.Count < 2)
                throw new InvalidDataException("RFC4180 CSV CSV read failed: " + relativePath);
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

        private static string Value(IReadOnlyDictionary<string, string> row, string field,
            bool required = true)
        {
            if (!row.TryGetValue(field, out var value))
                throw new InvalidDataException("Missing CSV field: " + field);
            if (required && string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException("Empty CSV field: " + field);
            return value;
        }

        private static int Integer(IReadOnlyDictionary<string, string> row, string field)
        {
            if (!int.TryParse(Value(row, field), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var value))
                throw new InvalidDataException("Invalid integer field: " + field);
            return value;
        }

        private static T EnumToken<T>(string token, string field) where T : struct
        {
            if (!Enum.TryParse(token, false, out T value) ||
                !Enum.IsDefined(typeof(T), value))
                throw new InvalidDataException("Invalid enum token for " + field + ": " + token);
            return value;
        }

        private static T Single<T>(IEnumerable<T> values, string label)
        {
            var result = values.ToArray();
            if (result.Length != 1)
                throw new InvalidDataException("Expected one MAP21_01 shell role: " + label);
            return result[0];
        }

        private static string ToPascal(string token)
        {
            if (string.IsNullOrEmpty(token)) return string.Empty;
            return char.ToUpperInvariant(token[0]) + token.Substring(1).ToLowerInvariant();
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

    public sealed class MoonPalaceMicroPatternPublishedSample
    {
        private readonly string[] writtenRelativePaths;

        public MoonPalaceMicroPatternPublishedSample(
            MoonPalaceMicroPatternProduction production,
            MoonPalaceMicroPatternDigestManifest digestManifest,
            MoonPalaceMicroPatternForbiddenOperationCounters counters,
            IEnumerable<string> sourceWrittenRelativePaths)
        {
            Production = production ?? throw new ArgumentNullException(nameof(production));
            DigestManifest = digestManifest ?? throw new ArgumentNullException(
                nameof(digestManifest));
            Counters = counters ?? throw new ArgumentNullException(nameof(counters));
            writtenRelativePaths = (sourceWrittenRelativePaths ?? Array.Empty<string>())
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        public MoonPalaceMicroPatternProduction Production { get; }
        public MoonPalaceMicroPatternDigestManifest DigestManifest { get; }
        public MoonPalaceMicroPatternForbiddenOperationCounters Counters { get; }
        public IReadOnlyList<string> WrittenRelativePaths => writtenRelativePaths;
    }

    public sealed class MoonPalaceMicroPatternForbiddenOperationCounters
    {
        public static MoonPalaceMicroPatternForbiddenOperationCounters Zero =>
            new MoonPalaceMicroPatternForbiddenOperationCounters();
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
        public int RuntimeBindingsCreated => 0;
        public int AssetImports => 0;
        public bool AllZero => GenerationExecutions == 0 && RendererExecutions == 0 &&
            ValidationRunnerExecutions == 0 && ReplayExecutions == 0 &&
            RollbackExecutions == 0 && PlayModeSelections == 0 &&
            LegacyRegressionSelections == 0 && PriorCategorySelections == 0 &&
            UnfilteredOrFullRegressionRuns == 0 && RuntimeObjectSpawns == 0 &&
            RuntimeBindingsCreated == 0 && AssetImports == 0;
    }

    [Serializable]
    internal sealed class MoonPalaceSourceDigestManifestDocument
    {
        public string MAP21_02_handoff_digest;
        public string biome_profile_digest;
        public string tile_shell_digest;
    }

    [Serializable]
    internal sealed class MoonPalaceSourceTileShellDocument
    {
        public MoonPalaceSourceTileShellRecordDocument[] tile_records;
        public string canonical_digest;
    }

    [Serializable]
    internal sealed class MoonPalaceSourceTileShellRecordDocument
    {
        public string tile_code;
        public string tile_role;
        public string collision_kind;
        public string material_token;
        public string asset_reference_kind;
        public string fallback_tile_code;
        public string missing_reason;
    }
}
