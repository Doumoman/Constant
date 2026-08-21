using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using StarNight.Map.WorldGeneration.Data;
using StarNight.MapAuthoring.WorldGeneration.Import;
using UnityEngine;

namespace StarNight.Map.Editor.WorldGeneration.Data
{
    public sealed class CsvImportPipeline
    {
        public const string AuthoringRootProjectRelativePath =
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/";
        public const string DictionaryFileName = "CSV_DATA_DICTIONARY.csv";

        private static readonly string[] WorldRouteFiles =
        {
            "world_profiles.csv", "generation_profiles.csv", "generation_passes.csv",
            "rng_streams.csv", "sector_route_masks.csv", "socket_band_definitions.csv",
            "edge_signatures.csv", "edge_signature_compatibility.csv",
            "sector_recipe_catalog.csv", "sector_recipe_cells.csv",
            "sector_recipe_paths.csv", "sector_external_sockets.csv",
            "sector_recipe_pool_entries.csv"
        };

        private static readonly string[] BiomeBoundaryFiles =
        {
            "biome_types.csv", "biome_patch_rules.csv", "biome_boundary_profiles.csv",
            "biome_boundary_pair_rules.csv", "boundary_chunk_catalog.csv"
        };

        private static readonly string[] SpecialVillageFiles =
        {
            "event_activation_routes.csv", "special_map_catalog.csv",
            "special_map_entry_sockets.csv", "special_map_footprint_cells.csv",
            "special_map_rewards.csv", "shop_archetypes.csv", "shop_inventory_rules.csv",
            "shopkeeper_species.csv", "village_facilities.csv", "village_layout_catalog.csv",
            "village_layout_cells.csv", "village_profiles.csv"
        };

        private static readonly string[] MicrochunkPopulationFiles =
            MicrochunkPopulationItemDefinitionSource.ExpectedFileNames.ToArray();

        private static readonly ReadOnlyCollection<string> ExpectedFiles =
            new ReadOnlyCollection<string>(new[] { DictionaryFileName }
                .Concat(ForeignKeySourceSet.ExpectedFileNames)
                .ToArray());
        private static readonly HashSet<string> ExpectedFileSet =
            new HashSet<string>(ExpectedFiles, StringComparer.Ordinal);
        private static readonly StaticDataRegistryStore SharedStore =
            new StaticDataRegistryStore();

        private readonly StaticDataRegistryStore store;

        public CsvImportPipeline()
            : this(SharedStore)
        {
        }

        public CsvImportPipeline(StaticDataRegistryStore registryStore)
        {
            store = registryStore ?? throw new ArgumentNullException(nameof(registryStore));
        }

        public static IReadOnlyList<string> ExpectedFileNames => ExpectedFiles;

        public CsvImportSessionResult CreateNotRunResult()
        {
            var inventory = ResolveInventory();
            var files = BuildFileStatuses(inventory, null, Array.Empty<CsvImportIssue>(), true);
            return new CsvImportSessionResult(
                files,
                inventory.Issues,
                null,
                null,
                "NOT_RUN",
                0f,
                CsvImportReportFileWriter.ReportProjectRelativePath,
                false,
                string.Empty);
        }

        public CsvImportSessionResult Execute(Action<string, float> progress = null)
        {
            var issues = new List<CsvImportIssue>();
            var dataByFile = new Dictionary<string, FileData>(StringComparer.Ordinal);
            CsvSchemaCatalog catalog = null;
            ForeignKeySourceSet sourceSet = null;
            ForeignKeyResolutionResult foreignKeys = null;
            StaticDataRegistry registry = null;
            ContentVersionHash contentHash = null;

            Report(progress, "INVENTORY", 0.03f);
            var inventory = ResolveInventory();
            issues.AddRange(inventory.Issues);

            Report(progress, "READ", 0.10f);
            foreach (var fileName in ExpectedFiles)
            {
                if (!inventory.PathsByFile.TryGetValue(fileName, out var projectPath)) continue;
                try
                {
                    var fullPath = ProjectPathToFullPath(projectPath);
                    var bytes = File.ReadAllBytes(fullPath);
                    var read = new Rfc4180CsvReader().Read(bytes, fileName);
                    var data = new FileData(projectPath, bytes, read, RawSha256(bytes));
                    dataByFile.Add(fileName, data);
                    if (!read.HadUtf8Bom)
                    {
                        issues.Add(Error(
                            "READ", "MISSING_UTF8_BOM",
                            "Every fixed authoring CSV must contain a UTF-8 BOM.", fileName));
                    }

                    foreach (var error in read.Errors)
                    {
                        issues.Add(Error(
                            "READ", error.Code.ToString(), error.Message, fileName,
                            error.Location.RecordNumber, null, error.Location.PhysicalLine,
                            error.Location.PhysicalColumn, error.Location.CharOffset));
                    }
                }
                catch (Exception exception)
                {
                    issues.Add(Error(
                        "READ", "FILE_READ_FAILED", exception.Message, fileName));
                }
            }

            Report(progress, "SCHEMA", 0.20f);
            if (dataByFile.TryGetValue(DictionaryFileName, out var dictionaryData) &&
                dictionaryData.ReadResult.Success && dictionaryData.ReadResult.HadUtf8Bom)
            {
                var import = new CsvSchemaDictionaryImporter().ParseBytes(dictionaryData.Bytes);
                foreach (var error in import.Errors)
                {
                    issues.Add(Error("SCHEMA_IMPORT", "DICTIONARY_IMPORT_FAILED", error,
                        DictionaryFileName));
                }

                if (import.Success)
                {
                    var catalogResult = new CsvSchemaCatalogBuilder().Build(import.Rows);
                    foreach (var error in catalogResult.Errors)
                    {
                        issues.Add(Error(
                            "SCHEMA_CATALOG", error.Code, error.Message,
                            string.IsNullOrEmpty(error.FileName) ? DictionaryFileName : error.FileName,
                            error.SourceRowNumber > 0 ? error.SourceRowNumber : (int?)null,
                            error.ColumnName,
                            error.SourceRowNumber > 0 ? error.SourceRowNumber : (int?)null));
                    }

                    if (catalogResult.Success)
                    {
                        catalog = catalogResult.Catalog;
                        ValidateCatalogInventory(catalog, issues);
                        if (HasErrorAtStage(issues, "SCHEMA_CATALOG")) catalog = null;
                    }
                }
            }
            else
            {
                issues.Add(Skipped(
                    "SCHEMA_IMPORT",
                    "Dictionary read/BOM prerequisites did not pass."));
            }

            Report(progress, "VALIDATE_PARSE", 0.38f);
            var parsedByFile = new Dictionary<string, CsvScalarAndListParseResult>(
                StringComparer.Ordinal);
            if (catalog == null)
            {
                issues.Add(Skipped(
                    "VALIDATE_PARSE",
                    "Schema catalog is unavailable; all static validation/parsing was skipped."));
            }
            else
            {
                foreach (var fileName in ForeignKeySourceSet.ExpectedFileNames)
                {
                    ParseOneFile(fileName, catalog, dataByFile, parsedByFile, issues);
                }
            }

            Report(progress, "DEFINITIONS", 0.55f);
            WorldRouteDefinitionSet worldDefinitions = null;
            BiomeBoundaryDefinitionSet biomeDefinitions = null;
            SpecialVillageDefinitionSet specialDefinitions = null;
            MicrochunkPopulationItemDefinitionSet microDefinitions = null;
            BuildWorldDefinitions(
                catalog, parsedByFile, issues, out worldDefinitions);
            BuildBiomeDefinitions(
                catalog, parsedByFile, issues, out biomeDefinitions);
            BuildSpecialDefinitions(
                catalog, parsedByFile, issues, out specialDefinitions);
            BuildMicroDefinitions(
                catalog, parsedByFile, issues, out microDefinitions);

            Report(progress, "FOREIGN_KEYS", 0.67f);
            if (catalog != null &&
                ForeignKeySourceSet.ExpectedFileNames.All(parsedByFile.ContainsKey))
            {
                sourceSet = new ForeignKeySourceSet(
                    catalog,
                    ForeignKeySourceSet.ExpectedFileNames.Select(fileName =>
                        new ForeignKeySourceSet.Source(
                            catalog.GetFile(fileName), parsedByFile[fileName])));
                foreignKeys = new ForeignKeyResolver().Resolve(sourceSet);
                foreach (var error in foreignKeys.Errors)
                {
                    var location = error.SourceLocation;
                    issues.Add(Error(
                        "FOREIGN_KEYS", error.ErrorCode.ToString(), error.Message,
                        error.SourceFileName, error.SourceRecordNumber,
                        error.SourceColumnName,
                        location?.PhysicalLine, location?.PhysicalColumn,
                        location?.CharOffset,
                        error.TargetFileName, error.TargetColumnName, error.TargetValue));
                }
            }
            else
            {
                issues.Add(Skipped(
                    "FOREIGN_KEYS",
                    "One or more of the exact 49 parsed sources is unavailable."));
            }

            Report(progress, "REGISTRY", 0.77f);
            if (worldDefinitions != null && biomeDefinitions != null &&
                specialDefinitions != null && microDefinitions != null &&
                foreignKeys != null && foreignKeys.Success)
            {
                var registryResult = new StaticDataRegistryBuilder().Build(
                    new StaticDataRegistryInput(
                        worldDefinitions,
                        biomeDefinitions,
                        specialDefinitions,
                        microDefinitions,
                        foreignKeys));
                foreach (var error in registryResult.Errors)
                {
                    issues.Add(Error(
                        "REGISTRY", error.ErrorCode.ToString(), error.Message,
                        error.FileName, error.RecordNumber, error.DefinitionType,
                        error.SourceLocation?.PhysicalLine,
                        error.SourceLocation?.PhysicalColumn,
                        error.SourceLocation?.CharOffset));
                }

                if (registryResult.Success) registry = registryResult.Registry;
            }
            else
            {
                issues.Add(Skipped(
                    "REGISTRY",
                    "Definition or foreign-key prerequisites did not pass."));
            }

            Report(progress, "CONTENT_HASH", 0.84f);
            if (registry != null && sourceSet != null)
            {
                var hashResult = new ContentVersionHashCalculator().Calculate(
                    registry, sourceSet, catalog);
                foreach (var error in hashResult.Errors)
                {
                    issues.Add(Error(
                        "CONTENT_HASH", error.ErrorCode.ToString(), error.Message,
                        error.FileName, error.RecordNumber, error.FieldName,
                        error.SourceLocation?.PhysicalLine,
                        error.SourceLocation?.PhysicalColumn,
                        error.SourceLocation?.CharOffset));
                }

                if (hashResult.Success) contentHash = hashResult.Hash;
            }
            else
            {
                issues.Add(Skipped(
                    "CONTENT_HASH",
                    "Registry/source-set prerequisites did not pass."));
            }

            Report(progress, "PUBLISH", 0.90f);
            var report = new StaticDataAtomicPublisher(store).Publish(
                new StaticDataPublishRequest(
                    registry,
                    contentHash,
                    OrderIssues(issues),
                    Guid.NewGuid().ToString("N")));

            Report(progress, "REPORT_WRITE", 0.96f);
            CsvImportReportWriteResult write;
            try
            {
                write = new CsvImportReportFileWriter().Write(
                    CsvImportReportJson.SerializeUtf8(report));
            }
            catch (Exception exception)
            {
                write = new CsvImportReportWriteResult(false, string.Empty, exception.Message);
            }

            var sessionIssues = new List<CsvImportIssue>(report.Issues);
            if (!write.Success)
            {
                sessionIssues.Add(Error(
                    "REPORT_WRITE",
                    "REPORT_PERSISTENCE_FAILED",
                    string.IsNullOrEmpty(write.Error)
                        ? "The import report could not be persisted."
                        : write.Error));
            }

            sessionIssues = OrderIssues(sessionIssues).ToList();
            var statuses = BuildFileStatuses(inventory, dataByFile, sessionIssues, false);
            Report(progress, "COMPLETE", 1f);
            return new CsvImportSessionResult(
                statuses,
                sessionIssues,
                report,
                foreignKeys?.RecordIndex,
                "COMPLETE",
                1f,
                CsvImportReportFileWriter.ReportProjectRelativePath,
                write.Success,
                write.Error);
        }

        public static IReadOnlyList<CsvImportIssue> ValidateInventory(
            IEnumerable<string> projectRelativePaths)
        {
            if (projectRelativePaths == null)
            {
                return new[]
                {
                    Error("INVENTORY", "MISSING_INVENTORY",
                        "CSV inventory is missing.")
                };
            }

            var issues = new List<CsvImportIssue>();
            var names = new List<string>();
            foreach (var rawPath in projectRelativePaths)
            {
                var path = (rawPath ?? string.Empty).Replace('\\', '/');
                if (!path.StartsWith(
                        AuthoringRootProjectRelativePath,
                        StringComparison.Ordinal) ||
                    path.Length <= AuthoringRootProjectRelativePath.Length)
                {
                    issues.Add(Error(
                        "INVENTORY", "PATH_OUTSIDE_FIXED_ROOT",
                        "CSV path is outside the fixed authoring root.",
                        Path.GetFileName(path)));
                    continue;
                }

                var name = Path.GetFileName(path);
                names.Add(name);
                if (!ExpectedFileSet.Contains(name))
                {
                    issues.Add(Error(
                        "INVENTORY", "UNEXPECTED_FILE",
                        "Unexpected CSV file under the fixed authoring root.", name));
                }
            }

            foreach (var group in names.GroupBy(name => name, StringComparer.Ordinal))
            {
                if (group.Count() > 1)
                {
                    issues.Add(Error(
                        "INVENTORY", "DUPLICATE_FILE",
                        "CSV filename occurs more than once under the fixed root.", group.Key));
                }
            }

            foreach (var expected in ExpectedFiles)
            {
                if (!names.Contains(expected, StringComparer.Ordinal))
                {
                    issues.Add(Error(
                        "INVENTORY", "MISSING_FILE",
                        "Required fixed CSV file is missing.", expected));
                }
            }

            return new ReadOnlyCollection<CsvImportIssue>(OrderIssues(issues).ToList());
        }

        private static InventoryResult ResolveInventory()
        {
            var projectRoot = GetProjectRoot();
            var root = ProjectPathToFullPath(AuthoringRootProjectRelativePath);
            var paths = Directory.Exists(root)
                ? Directory.GetFiles(root, "*.csv", SearchOption.AllDirectories)
                    .Select(full => FullPathToProjectPath(projectRoot, full))
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .ToArray()
                : Array.Empty<string>();
            var issues = ValidateInventory(paths).ToList();
            var byFile = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var group in paths.GroupBy(Path.GetFileName, StringComparer.Ordinal))
            {
                if (group.Count() == 1 && ExpectedFileSet.Contains(group.Key))
                {
                    byFile.Add(group.Key, group.Single());
                }
            }

            return new InventoryResult(byFile, issues);
        }

        private static void ValidateCatalogInventory(
            CsvSchemaCatalog catalog,
            ICollection<CsvImportIssue> issues)
        {
            var actual = new HashSet<string>(
                catalog.Files.Select(file => file.FileName), StringComparer.Ordinal);
            foreach (var fileName in ForeignKeySourceSet.ExpectedFileNames)
            {
                if (!actual.Contains(fileName))
                {
                    issues.Add(Error(
                        "SCHEMA_CATALOG", "MISSING_SCHEMA",
                        "Dictionary catalog is missing an exact static schema.", fileName));
                }
            }

            // Generated/output schemas may live in the dictionary, but they are deliberately
            // excluded from this fixed 49-source import pipeline.
        }

        private static void ParseOneFile(
            string fileName,
            CsvSchemaCatalog catalog,
            IReadOnlyDictionary<string, FileData> dataByFile,
            IDictionary<string, CsvScalarAndListParseResult> parsedByFile,
            ICollection<CsvImportIssue> issues)
        {
            if (!dataByFile.TryGetValue(fileName, out var data) ||
                !data.ReadResult.Success || !data.ReadResult.HadUtf8Bom)
            {
                return;
            }

            if (!catalog.TryGetFile(fileName, out var schema)) return;
            try
            {
                var validation = new CsvHeaderAndFieldValidator().Validate(
                    data.ReadResult, schema, fileName);
                foreach (var error in validation.Errors)
                {
                    issues.Add(Error(
                        "HEADER_FIELDS", error.ErrorCode.ToString(), error.Message,
                        fileName, error.RecordNumber, null, error.PhysicalLine,
                        error.PhysicalColumn, error.CharOffset));
                }

                if (!validation.Success) return;
                var primaryKeys = new CsvPrimaryKeyIndexBuilder().Build(
                    schema, validation, fileName);
                foreach (var duplicate in primaryKeys.Duplicates)
                {
                    foreach (var occurrence in duplicate.Occurrences)
                    {
                        issues.Add(Error(
                            "PRIMARY_KEYS", "DUPLICATE_PRIMARY_KEY",
                            "Duplicate primary key: " + duplicate.Key,
                            fileName, occurrence.RecordNumber, null,
                            occurrence.PhysicalLine, occurrence.PhysicalColumn,
                            occurrence.CharOffset));
                    }
                }

                if (!primaryKeys.Success) return;
                var parsed = new CsvScalarAndListParser().Parse(
                    schema, validation, primaryKeys, fileName);
                foreach (var error in parsed.Errors)
                {
                    issues.Add(Error(
                        "VALUE_PARSE", error.ErrorCode.ToString(), error.Message,
                        fileName, error.RecordNumber, error.ColumnName,
                        error.PhysicalLine, error.PhysicalColumn, error.CharOffset));
                }

                if (parsed.Success) parsedByFile.Add(fileName, parsed);
            }
            catch (Exception exception)
            {
                issues.Add(Error(
                    "VALIDATE_PARSE", "UNEXPECTED_FILE_EXCEPTION",
                    exception.Message, fileName));
            }
        }

        private static void BuildWorldDefinitions(
            CsvSchemaCatalog catalog,
            IReadOnlyDictionary<string, CsvScalarAndListParseResult> parsed,
            ICollection<CsvImportIssue> issues,
            out WorldRouteDefinitionSet definitions)
        {
            definitions = null;
            if (!CanBuild("WORLD_ROUTE_DEFINITIONS", WorldRouteFiles, catalog, parsed, issues))
                return;
            var result = new WorldRouteDefinitionBuilder().Build(WorldRouteFiles.Select(file =>
                new WorldRouteDefinitionSource(catalog.GetFile(file), parsed[file])));
            foreach (var error in result.Errors)
            {
                issues.Add(DefinitionError(
                    "WORLD_ROUTE_DEFINITIONS", error.FileName,
                    error.ErrorCode.ToString(), error.Message, error.RecordNumber,
                    error.ColumnName, error.Location));
            }

            if (result.Success) definitions = result.DefinitionSet;
        }

        private static void BuildBiomeDefinitions(
            CsvSchemaCatalog catalog,
            IReadOnlyDictionary<string, CsvScalarAndListParseResult> parsed,
            ICollection<CsvImportIssue> issues,
            out BiomeBoundaryDefinitionSet definitions)
        {
            definitions = null;
            if (!CanBuild("BIOME_BOUNDARY_DEFINITIONS", BiomeBoundaryFiles, catalog, parsed, issues))
                return;
            var result = new BiomeBoundaryDefinitionBuilder().Build(BiomeBoundaryFiles.Select(file =>
                new BiomeBoundaryDefinitionSource(catalog.GetFile(file), parsed[file])));
            foreach (var error in result.Errors)
            {
                issues.Add(DefinitionError(
                    "BIOME_BOUNDARY_DEFINITIONS", error.FileName,
                    error.ErrorCode.ToString(), error.Message, error.RecordNumber,
                    error.ColumnName, error.Location));
            }

            if (result.Success) definitions = result.DefinitionSet;
        }

        private static void BuildSpecialDefinitions(
            CsvSchemaCatalog catalog,
            IReadOnlyDictionary<string, CsvScalarAndListParseResult> parsed,
            ICollection<CsvImportIssue> issues,
            out SpecialVillageDefinitionSet definitions)
        {
            definitions = null;
            if (!CanBuild("SPECIAL_VILLAGE_DEFINITIONS", SpecialVillageFiles, catalog, parsed, issues))
                return;
            var result = new SpecialVillageDefinitionBuilder().Build(SpecialVillageFiles.Select(file =>
                new SpecialVillageDefinitionSource(catalog.GetFile(file), parsed[file])));
            foreach (var error in result.Errors)
            {
                issues.Add(DefinitionError(
                    "SPECIAL_VILLAGE_DEFINITIONS", error.FileName,
                    error.ErrorCode.ToString(), error.Message, error.RecordNumber,
                    error.ColumnName, error.Location));
            }

            if (result.Success) definitions = result.DefinitionSet;
        }

        private static void BuildMicroDefinitions(
            CsvSchemaCatalog catalog,
            IReadOnlyDictionary<string, CsvScalarAndListParseResult> parsed,
            ICollection<CsvImportIssue> issues,
            out MicrochunkPopulationItemDefinitionSet definitions)
        {
            definitions = null;
            if (!CanBuild("MICROCHUNK_POPULATION_DEFINITIONS", MicrochunkPopulationFiles, catalog, parsed, issues))
                return;
            var result = new MicrochunkPopulationItemDefinitionBuilder().Build(
                MicrochunkPopulationFiles.Select(file =>
                    new MicrochunkPopulationItemDefinitionSource(
                        catalog.GetFile(file), parsed[file])));
            foreach (var error in result.Errors)
            {
                issues.Add(DefinitionError(
                    "MICROCHUNK_POPULATION_DEFINITIONS", error.FileName,
                    error.ErrorCode.ToString(), error.Message, error.RecordNumber,
                    error.ColumnName, error.Location));
            }

            if (result.Success) definitions = result.DefinitionSet;
        }

        private static bool CanBuild(
            string stage,
            IEnumerable<string> requiredFiles,
            CsvSchemaCatalog catalog,
            IReadOnlyDictionary<string, CsvScalarAndListParseResult> parsed,
            ICollection<CsvImportIssue> issues)
        {
            var missing = requiredFiles.Where(file => !parsed.ContainsKey(file)).ToArray();
            if (catalog != null && missing.Length == 0) return true;
            issues.Add(Skipped(
                stage,
                catalog == null
                    ? "Schema catalog is unavailable."
                    : "Parsed prerequisites are unavailable: " + string.Join(", ", missing)));
            return false;
        }

        private static CsvImportIssue DefinitionError(
            string stage,
            string fileName,
            string code,
            string message,
            int? record,
            string field,
            CsvSourceLocation? location)
        {
            return Error(
                stage, code, message, fileName, record, field,
                location?.PhysicalLine, location?.PhysicalColumn, location?.CharOffset);
        }

        private static IReadOnlyList<CsvImportFileStatus> BuildFileStatuses(
            InventoryResult inventory,
            IReadOnlyDictionary<string, FileData> dataByFile,
            IEnumerable<CsvImportIssue> sourceIssues,
            bool notRun)
        {
            var issues = sourceIssues.ToArray();
            var rows = new List<CsvImportFileStatus>(ExpectedFiles.Count);
            foreach (var fileName in ExpectedFiles)
            {
                inventory.PathsByFile.TryGetValue(fileName, out var projectPath);
                FileData data = null;
                dataByFile?.TryGetValue(fileName, out data);
                var errors = issues.Count(issue =>
                    string.Equals(issue.SourceFile, fileName, StringComparison.Ordinal) &&
                    string.Equals(issue.Severity, CsvImportIssue.ErrorSeverity, StringComparison.Ordinal));
                var warnings = issues.Count(issue =>
                    string.Equals(issue.SourceFile, fileName, StringComparison.Ordinal) &&
                    string.Equals(issue.Severity, CsvImportIssue.WarningSeverity, StringComparison.Ordinal));
                var state = notRun
                    ? CsvImportFileStatus.NotRun
                    : errors > 0
                        ? CsvImportFileStatus.Error
                        : warnings > 0
                            ? CsvImportFileStatus.Warning
                            : CsvImportFileStatus.Success;
                rows.Add(new CsvImportFileStatus(
                    fileName,
                    Category(projectPath, fileName),
                    projectPath ?? string.Empty,
                    state,
                    data?.Bytes.LongLength ?? 0,
                    data == null ? 0 : Math.Max(0, data.ReadResult.Records.Count - 1),
                    errors,
                    warnings,
                    data?.RawSha256 ?? string.Empty,
                    data != null && data.ReadResult.HadUtf8Bom));
            }

            return new ReadOnlyCollection<CsvImportFileStatus>(rows);
        }

        private static string Category(string projectPath, string fileName)
        {
            if (string.Equals(fileName, DictionaryFileName, StringComparison.Ordinal))
                return "Dictionary";
            if (string.IsNullOrEmpty(projectPath)) return "Unknown";
            var normalized = projectPath.Replace('\\', '/');
            var slash = normalized.LastIndexOf('/');
            if (slash <= 0) return "Unknown";
            var parent = normalized.LastIndexOf('/', slash - 1);
            return parent < 0 ? "Unknown" : normalized.Substring(parent + 1, slash - parent - 1);
        }

        private static bool HasErrorAtStage(
            IEnumerable<CsvImportIssue> issues,
            string stage)
        {
            return issues.Any(issue =>
                string.Equals(issue.Stage, stage, StringComparison.Ordinal) &&
                string.Equals(issue.Severity, CsvImportIssue.ErrorSeverity, StringComparison.Ordinal));
        }

        private static CsvImportIssue Skipped(string stage, string reason)
        {
            return Error(stage, "STAGE_SKIPPED", reason);
        }

        private static CsvImportIssue Error(
            string stage,
            string code,
            string message,
            string sourceFile = null,
            int? record = null,
            string field = null,
            int? line = null,
            int? column = null,
            int? offset = null,
            string targetFile = null,
            string targetColumn = null,
            string targetValue = null)
        {
            return new CsvImportIssue(
                stage,
                CsvImportIssue.ErrorSeverity,
                code,
                message,
                sourceFile,
                record,
                field,
                line,
                column,
                offset,
                targetFile,
                targetColumn,
                targetValue);
        }

        private static IEnumerable<CsvImportIssue> OrderIssues(
            IEnumerable<CsvImportIssue> issues)
        {
            return issues.OrderBy(issue => SeverityRank(issue.Severity))
                .ThenBy(issue => issue.Stage, StringComparer.Ordinal)
                .ThenBy(issue => issue.SourceFile, StringComparer.Ordinal)
                .ThenBy(issue => issue.RecordNumber)
                .ThenBy(issue => issue.SourceField, StringComparer.Ordinal)
                .ThenBy(issue => issue.TargetFile, StringComparer.Ordinal)
                .ThenBy(issue => issue.TargetColumn, StringComparer.Ordinal)
                .ThenBy(issue => issue.TargetValue, StringComparer.Ordinal)
                .ThenBy(issue => issue.Code, StringComparer.Ordinal)
                .ThenBy(issue => issue.Message, StringComparer.Ordinal)
                .ThenBy(issue => issue.Line)
                .ThenBy(issue => issue.Column)
                .ThenBy(issue => issue.Offset);
        }

        private static int SeverityRank(string severity)
        {
            return string.Equals(
                severity, CsvImportIssue.ErrorSeverity, StringComparison.Ordinal) ? 0 : 1;
        }

        private static void Report(Action<string, float> callback, string stage, float progress)
        {
            callback?.Invoke(stage, progress);
        }

        private static string RawSha256(byte[] bytes)
        {
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(bytes))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }

        private static string GetProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static string ProjectPathToFullPath(string projectPath)
        {
            var projectRoot = GetProjectRoot();
            var rootPrefix = projectRoot.TrimEnd(
                                 Path.DirectorySeparatorChar,
                                 Path.AltDirectorySeparatorChar) +
                             Path.DirectorySeparatorChar;
            var fullPath = Path.GetFullPath(Path.Combine(
                projectRoot,
                projectPath.Replace('/', Path.DirectorySeparatorChar)));
            if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Project-relative path escaped the project root.");
            }

            return fullPath;
        }

        private static string FullPathToProjectPath(string projectRoot, string fullPath)
        {
            var rootPrefix = projectRoot.TrimEnd(
                                 Path.DirectorySeparatorChar,
                                 Path.AltDirectorySeparatorChar) +
                             Path.DirectorySeparatorChar;
            var canonical = Path.GetFullPath(fullPath);
            if (!canonical.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("CSV path escaped the project root.");
            }

            return canonical.Substring(rootPrefix.Length).Replace('\\', '/');
        }

        private sealed class FileData
        {
            public FileData(
                string projectPath,
                byte[] bytes,
                CsvReadResult readResult,
                string rawSha256)
            {
                ProjectPath = projectPath;
                Bytes = bytes;
                ReadResult = readResult;
                RawSha256 = rawSha256;
            }

            public string ProjectPath { get; }
            public byte[] Bytes { get; }
            public CsvReadResult ReadResult { get; }
            public string RawSha256 { get; }
        }

        private sealed class InventoryResult
        {
            public InventoryResult(
                IDictionary<string, string> pathsByFile,
                IEnumerable<CsvImportIssue> issues)
            {
                PathsByFile = new ReadOnlyDictionary<string, string>(
                    new Dictionary<string, string>(pathsByFile, StringComparer.Ordinal));
                Issues = new ReadOnlyCollection<CsvImportIssue>(issues.ToList());
            }

            public IReadOnlyDictionary<string, string> PathsByFile { get; }
            public IReadOnlyList<CsvImportIssue> Issues { get; }
        }
    }
}
