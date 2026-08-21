using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Data;
using UnityEngine;

namespace StarNight.Map.Tests.WorldGeneration.Data
{
    public enum CsvFailureMutationKind
    {
        DuplicatePrimaryKey,
        InvalidEnumToken,
        InvalidInt,
        InvalidFloat,
        MissingSingleForeignKey,
        MissingListForeignKey,
        MissingUtf8Bom,
        RowOrderReversed,
        HeaderOrderChanged,
        CompoundIndependentFailures
    }

    public sealed class CsvFailureMutation
    {
        internal CsvFailureMutation(
            string mutationName,
            string fileName,
            string columnName,
            int recordNumber,
            int sourceLine,
            string before,
            string after,
            string beforeSha256,
            string afterSha256)
        {
            MutationName = mutationName;
            FileName = fileName;
            ColumnName = columnName;
            RecordNumber = recordNumber;
            SourceLine = sourceLine;
            Before = before;
            After = after;
            BeforeSha256 = beforeSha256;
            AfterSha256 = afterSha256;
        }

        public string MutationName { get; }
        public string FileName { get; }
        public string ColumnName { get; }
        public int RecordNumber { get; }
        public int SourceLine { get; }
        public string Before { get; }
        public string After { get; }
        public string BeforeSha256 { get; }
        public string AfterSha256 { get; }
    }

    public sealed class CsvFixtureImportResult
    {
        internal CsvFixtureImportResult(
            CsvImportReport report,
            StaticDataRegistry candidateRegistry,
            ContentVersionHash candidateHash,
            CsvSchemaCatalog catalog,
            IReadOnlyDictionary<string, string> rawHashes)
        {
            Report = report;
            CandidateRegistry = candidateRegistry;
            CandidateHash = candidateHash;
            Catalog = catalog;
            RawHashes = rawHashes;
        }

        public CsvImportReport Report { get; }
        public StaticDataRegistry CandidateRegistry { get; }
        public ContentVersionHash CandidateHash { get; }
        public CsvSchemaCatalog Catalog { get; }
        public IReadOnlyDictionary<string, string> RawHashes { get; }
        public IReadOnlyList<CsvImportIssue> Issues => Report.Issues;
    }

    public sealed class CsvFailureFixtureFactory : IDisposable
    {
        public const string DictionaryFileName = "CSV_DATA_DICTIONARY.csv";
        public const string DuplicatePrimaryKeyName = "DUPLICATE_PRIMARY_KEY";
        public const string InvalidEnumTokenName = "INVALID_ENUM_TOKEN";
        public const string InvalidIntName = "INVALID_INT";
        public const string InvalidFloatName = "INVALID_FLOAT";
        public const string MissingSingleForeignKeyName = "MISSING_SINGLE_FOREIGN_KEY";
        public const string MissingListForeignKeyName = "MISSING_LIST_FOREIGN_KEY";
        public const string MissingUtf8BomName = "MISSING_UTF8_BOM";
        public const string RowOrderReversedName = "ROW_ORDER_REVERSED";
        public const string HeaderOrderChangedName = "HEADER_ORDER_CHANGED";
        public const string CompoundIndependentFailuresName = "COMPOUND_INDEPENDENT_FAILURES";

        private static readonly byte[] Utf8Bom = { 0xef, 0xbb, 0xbf };
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private static readonly ReadOnlyCollection<string> expectedFileNames =
            new ReadOnlyCollection<string>(new[] { DictionaryFileName }
                .Concat(ForeignKeySourceSet.ExpectedFileNames)
                .ToArray());

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

        private readonly List<CsvFailureMutation> mutations = new List<CsvFailureMutation>();
        private readonly string rootPrefix;
        private bool disposed;

        private CsvFailureFixtureFactory(string sourceRoot, string root)
        {
            SourceRoot = Path.GetFullPath(sourceRoot);
            Root = Path.GetFullPath(root);
            rootPrefix = Root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                         Path.DirectorySeparatorChar;
            Directory.CreateDirectory(Root);
            CopyExactInventory();
        }

        public static IReadOnlyList<string> ExpectedFileNames => expectedFileNames;
        public string SourceRoot { get; }
        public string Root { get; }
        public IReadOnlyList<CsvFailureMutation> Mutations =>
            new ReadOnlyCollection<CsvFailureMutation>(mutations.ToList());

        public static CsvFailureFixtureFactory Create()
        {
            var source = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "_Game/Map/Data/WorldGeneration/Authoring"));
            var root = Path.Combine(
                Path.GetTempPath(),
                "StarNightCsvFailureFixtures",
                Guid.NewGuid().ToString("N"));
            return new CsvFailureFixtureFactory(source, root);
        }

        public string ResolveFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName) ||
                !expectedFileNames.Contains(fileName, StringComparer.Ordinal) ||
                !string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal))
            {
                throw new ArgumentException("Filename is outside the exact fixture inventory.", nameof(fileName));
            }

            var fullPath = Path.GetFullPath(Path.Combine(Root, fileName));
            if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Fixture path escaped its owned root.");
            }

            return fullPath;
        }

        public IReadOnlyList<CsvFailureMutation> Apply(CsvFailureMutationKind kind)
        {
            ThrowIfDisposed();
            mutations.Clear();
            var excluded = new HashSet<string>(StringComparer.Ordinal);
            switch (kind)
            {
                case CsvFailureMutationKind.DuplicatePrimaryKey:
                    DuplicatePrimaryKey(DuplicatePrimaryKeyName, excluded);
                    break;
                case CsvFailureMutationKind.InvalidEnumToken:
                    MutateScalar(InvalidEnumTokenName, CsvSchemaDataType.Enum,
                        "UNKNOWN_ENUM_TOKEN", excluded, column => column.AllowedValues.Count > 0);
                    break;
                case CsvFailureMutationKind.InvalidInt:
                    MutateScalar(InvalidIntName, CsvSchemaDataType.Int,
                        "NOT_AN_INTEGER", excluded);
                    break;
                case CsvFailureMutationKind.InvalidFloat:
                    MutateScalar(InvalidFloatName, CsvSchemaDataType.Float,
                        "NOT_A_FLOAT", excluded);
                    break;
                case CsvFailureMutationKind.MissingSingleForeignKey:
                    MutateForeignKey(MissingSingleForeignKeyName, false, excluded);
                    break;
                case CsvFailureMutationKind.MissingListForeignKey:
                    MutateForeignKey(MissingListForeignKeyName, true, excluded);
                    break;
                case CsvFailureMutationKind.MissingUtf8Bom:
                    RemoveBom(MissingUtf8BomName, excluded);
                    break;
                case CsvFailureMutationKind.RowOrderReversed:
                    ReverseRows(RowOrderReversedName, excluded);
                    break;
                case CsvFailureMutationKind.HeaderOrderChanged:
                    SwapHeader(HeaderOrderChangedName, excluded);
                    break;
                case CsvFailureMutationKind.CompoundIndependentFailures:
                    DuplicatePrimaryKey(CompoundIndependentFailuresName + "/" +
                                        DuplicatePrimaryKeyName, excluded);
                    MutateScalar(CompoundIndependentFailuresName + "/" +
                                 InvalidEnumTokenName, CsvSchemaDataType.Enum,
                        "UNKNOWN_ENUM_TOKEN", excluded, column => column.AllowedValues.Count > 0);
                    MutateScalar(CompoundIndependentFailuresName + "/" + InvalidIntName,
                        CsvSchemaDataType.Int, "NOT_AN_INTEGER", excluded);
                    MutateForeignKey(CompoundIndependentFailuresName + "/" +
                                     MissingSingleForeignKeyName, false, excluded);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }

            return Mutations;
        }

        public CsvFixtureImportResult Run(
            StaticDataRegistryStore store = null,
            string attemptId = "csv-failure-fixture")
        {
            ThrowIfDisposed();
            var aggregateIndependentFailures = mutations.Any(item => item.MutationName.StartsWith(
                CompoundIndependentFailuresName + "/", StringComparison.Ordinal));
            return Import(Root, SourceRoot, store ?? new StaticDataRegistryStore(), attemptId,
                aggregateIndependentFailures);
        }

        public IReadOnlyDictionary<string, string> FileHashes()
        {
            return new ReadOnlyDictionary<string, string>(expectedFileNames.ToDictionary(
                fileName => fileName,
                fileName => Sha256(File.ReadAllBytes(ResolveFile(fileName))),
                StringComparer.Ordinal));
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
        }

        private void CopyExactInventory()
        {
            var sources = Directory.GetFiles(SourceRoot, "*.csv", SearchOption.AllDirectories)
                .GroupBy(Path.GetFileName, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
            if (sources.Count != expectedFileNames.Count ||
                sources.Keys.Except(expectedFileNames, StringComparer.Ordinal).Any())
            {
                throw new InvalidOperationException("Source inventory is not the exact expected 50 CSV files.");
            }

            foreach (var fileName in expectedFileNames)
            {
                if (!sources.TryGetValue(fileName, out var matches) || matches.Length != 1)
                {
                    throw new InvalidOperationException("Expected source is not unique: " + fileName);
                }

                File.Copy(matches[0], ResolveFile(fileName), false);
            }
        }

        private void DuplicatePrimaryKey(string mutationName, ISet<string> excluded)
        {
            var catalog = LoadCatalog(Root);
            foreach (var schema in catalog.Files.OrderBy(item => item.FileName, StringComparer.Ordinal))
            {
                if (schema.PrimaryKeyColumns.Count == 0 || excluded.Contains(schema.FileName)) continue;
                var path = ResolveFile(schema.FileName);
                var beforeBytes = File.ReadAllBytes(path);
                var document = ReadDocument(schema.FileName, beforeBytes);
                if (document.Read.Records.Count < 2) continue;
                var record = document.Read.Records[1];
                var raw = document.Content.Substring(
                    record.StartLocation.CharOffset,
                    record.EndLocationExclusive.CharOffset - record.StartLocation.CharOffset);
                var afterContent = document.Content.EndsWith(document.NewLine, StringComparison.Ordinal)
                    ? document.Content + raw + document.NewLine
                    : document.Content + document.NewLine + raw;
                var afterBytes = WithOriginalBom(document.HadBom, afterContent);
                File.WriteAllBytes(path, afterBytes);
                excluded.Add(schema.FileName);
                mutations.Add(Descriptor(
                    mutationName,
                    schema.FileName,
                    string.Join("|", schema.PrimaryKeyColumns.Select(item => item.ColumnName)),
                    record.RecordNumber,
                    record.StartLocation.PhysicalLine,
                    raw,
                    raw + document.NewLine + raw,
                    beforeBytes,
                    afterBytes));
                return;
            }

            throw new InvalidOperationException("No deterministic duplicate-primary-key target exists.");
        }

        private void MutateScalar(
            string mutationName,
            CsvSchemaDataType dataType,
            string replacement,
            ISet<string> excluded,
            Func<CsvColumnSchema, bool> extra = null)
        {
            var catalog = LoadCatalog(Root);
            var target = FindField(catalog, excluded, column =>
                column.DataType == dataType && (extra == null || extra(column)));
            RewriteField(mutationName, target, replacement);
            excluded.Add(target.Schema.FileName);
        }

        private void MutateForeignKey(
            string mutationName,
            bool list,
            ISet<string> excluded)
        {
            var catalog = LoadCatalog(Root);
            var target = FindField(catalog, excluded, column =>
                column.ForeignKey != null && column.IsRequired &&
                (list
                    ? column.DataType == CsvSchemaDataType.IdList
                    : column.DataType != CsvSchemaDataType.IdList));
            var replacement = list
                ? ReplaceFirstListItem(target.Field.Value, "MISSING_LIST_REFERENCE")
                : "MISSING_SINGLE_REFERENCE";
            RewriteField(mutationName, target, replacement);
            excluded.Add(target.Schema.FileName);
        }

        private void RemoveBom(string mutationName, ISet<string> excluded)
        {
            foreach (var fileName in ForeignKeySourceSet.ExpectedFileNames)
            {
                if (excluded.Contains(fileName)) continue;
                var path = ResolveFile(fileName);
                var before = File.ReadAllBytes(path);
                if (!HasBom(before)) continue;
                var after = new byte[before.Length - Utf8Bom.Length];
                Buffer.BlockCopy(before, Utf8Bom.Length, after, 0, after.Length);
                File.WriteAllBytes(path, after);
                excluded.Add(fileName);
                mutations.Add(Descriptor(
                    mutationName, fileName, "<BOM>", 1, 1,
                    "efbbbf", string.Empty, before, after));
                return;
            }

            throw new InvalidOperationException("No BOM-required static source exists.");
        }

        private void ReverseRows(string mutationName, ISet<string> excluded)
        {
            foreach (var fileName in ForeignKeySourceSet.ExpectedFileNames)
            {
                if (excluded.Contains(fileName)) continue;
                var path = ResolveFile(fileName);
                var before = File.ReadAllBytes(path);
                var document = ReadDocument(fileName, before);
                if (document.Read.Records.Count < 3 || document.Read.Records.Any(record =>
                        record.StartLocation.PhysicalLine != record.EndLocationExclusive.PhysicalLine))
                {
                    continue;
                }

                var rawRecords = document.Read.Records.Select(record => document.Content.Substring(
                    record.StartLocation.CharOffset,
                    record.EndLocationExclusive.CharOffset - record.StartLocation.CharOffset)).ToArray();
                var terminal = document.Content.EndsWith(document.NewLine, StringComparison.Ordinal)
                    ? document.NewLine
                    : string.Empty;
                var afterContent = rawRecords[0] + document.NewLine +
                                   string.Join(document.NewLine, rawRecords.Skip(1).Reverse()) + terminal;
                var after = WithOriginalBom(document.HadBom, afterContent);
                File.WriteAllBytes(path, after);
                excluded.Add(fileName);
                mutations.Add(Descriptor(
                    mutationName, fileName, "<ROW_ORDER>", 2,
                    document.Read.Records[1].StartLocation.PhysicalLine,
                    string.Join("|", rawRecords.Skip(1)),
                    string.Join("|", rawRecords.Skip(1).Reverse()),
                    before,
                    after));
                return;
            }

            throw new InvalidOperationException("No safe deterministic row-order target exists.");
        }

        private void SwapHeader(string mutationName, ISet<string> excluded)
        {
            var catalog = LoadCatalog(Root);
            foreach (var schema in catalog.Files.OrderBy(item => item.FileName, StringComparer.Ordinal))
            {
                if (schema.Columns.Count < 2 || excluded.Contains(schema.FileName)) continue;
                var path = ResolveFile(schema.FileName);
                var before = File.ReadAllBytes(path);
                var document = ReadDocument(schema.FileName, before);
                if (document.Read.Records.Count < 2) continue;
                var first = document.Read.Records[0].Fields[0];
                var second = document.Read.Records[0].Fields[1];
                var replacements = new[]
                {
                    new Replacement(first.StartLocation.CharOffset,
                        first.EndLocationExclusive.CharOffset, EncodeField(second.Value)),
                    new Replacement(second.StartLocation.CharOffset,
                        second.EndLocationExclusive.CharOffset, EncodeField(first.Value))
                };
                var afterContent = Replace(document.Content, replacements);
                var after = WithOriginalBom(document.HadBom, afterContent);
                File.WriteAllBytes(path, after);
                excluded.Add(schema.FileName);
                mutations.Add(Descriptor(
                    mutationName,
                    schema.FileName,
                    first.Value + "|" + second.Value,
                    1,
                    1,
                    first.Value + "|" + second.Value,
                    second.Value + "|" + first.Value,
                    before,
                    after));
                return;
            }

            throw new InvalidOperationException("No deterministic header target exists.");
        }

        private void RewriteField(string mutationName, FieldTarget target, string replacement)
        {
            var path = ResolveFile(target.Schema.FileName);
            var before = File.ReadAllBytes(path);
            var document = ReadDocument(target.Schema.FileName, before);
            var afterContent = Replace(document.Content, new[]
            {
                new Replacement(
                    target.Field.StartLocation.CharOffset,
                    target.Field.EndLocationExclusive.CharOffset,
                    EncodeField(replacement))
            });
            var after = WithOriginalBom(document.HadBom, afterContent);
            File.WriteAllBytes(path, after);
            mutations.Add(Descriptor(
                mutationName,
                target.Schema.FileName,
                target.Column.ColumnName,
                target.Record.RecordNumber,
                target.Field.StartLocation.PhysicalLine,
                target.Field.Value,
                replacement,
                before,
                after));
        }

        private FieldTarget FindField(
            CsvSchemaCatalog catalog,
            ISet<string> excluded,
            Func<CsvColumnSchema, bool> predicate)
        {
            foreach (var schema in catalog.Files.OrderBy(item => item.FileName, StringComparer.Ordinal))
            {
                if (excluded.Contains(schema.FileName) ||
                    !expectedFileNames.Contains(schema.FileName, StringComparer.Ordinal)) continue;
                var document = ReadDocument(schema.FileName, File.ReadAllBytes(ResolveFile(schema.FileName)));
                if (document.Read.Records.Count < 2) continue;
                foreach (var column in schema.Columns)
                {
                    if (!predicate(column)) continue;
                    foreach (var record in document.Read.Records.Skip(1))
                    {
                        var field = record.Fields[column.ColumnOrder - 1];
                        if (field.Value.Length > 0)
                        {
                            return new FieldTarget(schema, column, record, field);
                        }
                    }
                }
            }

            throw new InvalidOperationException("No deterministic field target exists.");
        }

        private static CsvSchemaCatalog LoadCatalog(string root)
        {
            var bytes = File.ReadAllBytes(Path.Combine(root, DictionaryFileName));
            var read = new Rfc4180CsvReader().Read(bytes, DictionaryFileName);
            if (!read.Success || read.Records.Count < 2)
                throw new InvalidOperationException("Dictionary fixture is unreadable.");
            var rows = read.Records.Skip(1).Select(record =>
            {
                if (record.Fields.Count != 10)
                    throw new InvalidOperationException("Dictionary row is not the exact 10-column contract.");
                return new CsvSchemaDictionaryRow(
                    record.Fields[0].Value,
                    record.Fields[1].Value,
                    record.Fields[2].Value,
                    record.Fields[3].Value,
                    record.Fields[4].Value,
                    record.Fields[5].Value,
                    record.Fields[6].Value,
                    record.Fields[7].Value,
                    record.Fields[8].Value,
                    record.Fields[9].Value,
                    record.RecordNumber);
            });
            var result = new CsvSchemaCatalogBuilder().Build(rows);
            if (!result.Success)
                throw new InvalidOperationException("Dictionary catalog build failed.");
            return result.Catalog;
        }

        private static CsvFixtureImportResult Import(
            string root,
            string sourceRoot,
            StaticDataRegistryStore store,
            string attemptId,
            bool aggregateIndependentFailures)
        {
            var issues = new List<CsvImportIssue>();
            var reads = new Dictionary<string, CsvReadResult>(StringComparer.Ordinal);
            var bytesByFile = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            var rawHashes = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var fileName in expectedFileNames)
            {
                var path = Path.Combine(root, fileName);
                var bytes = File.ReadAllBytes(path);
                var read = new Rfc4180CsvReader().Read(bytes, fileName);
                bytesByFile.Add(fileName, bytes);
                reads.Add(fileName, read);
                rawHashes.Add(fileName, Sha256(bytes));
                if (!read.HadUtf8Bom)
                {
                    issues.Add(Error("READ", "MISSING_UTF8_BOM",
                        "Every fixed authoring CSV must contain a UTF-8 BOM.", fileName,
                        1, null, 1, 1, 0));
                }

                foreach (var error in read.Errors)
                {
                    issues.Add(Error("READ", error.Code.ToString(), error.Message, fileName,
                        error.Location.RecordNumber, null, error.Location.PhysicalLine,
                        error.Location.PhysicalColumn, error.Location.CharOffset));
                }
            }

            CsvSchemaCatalog catalog = null;
            if (reads[DictionaryFileName].Success && reads[DictionaryFileName].HadUtf8Bom)
            {
                try
                {
                    catalog = LoadCatalog(root);
                }
                catch (Exception exception)
                {
                    issues.Add(Error("SCHEMA_CATALOG", "DICTIONARY_IMPORT_FAILED",
                        exception.Message, DictionaryFileName));
                }
            }
            else
            {
                issues.Add(Error("SCHEMA_IMPORT", "STAGE_SKIPPED",
                    "Dictionary read/BOM prerequisites did not pass."));
            }

            var parsed = new Dictionary<string, CsvScalarAndListParseResult>(StringComparer.Ordinal);
            if (catalog == null)
            {
                issues.Add(Error("VALIDATE_PARSE", "STAGE_SKIPPED",
                    "Schema catalog is unavailable; all static validation/parsing was skipped."));
            }
            else
            {
                foreach (var fileName in ForeignKeySourceSet.ExpectedFileNames)
                {
                    if (!reads[fileName].Success || !reads[fileName].HadUtf8Bom) continue;
                    var schema = catalog.GetFile(fileName);
                    var validation = new CsvHeaderAndFieldValidator().Validate(
                        reads[fileName], schema, fileName);
                    foreach (var error in validation.Errors)
                    {
                        issues.Add(Error("HEADER_FIELDS", error.ErrorCode.ToString(),
                            error.Message, fileName, error.RecordNumber, null,
                            error.PhysicalLine, error.PhysicalColumn, error.CharOffset));
                    }

                    if (!validation.Success) continue;
                    var primaryKeys = new CsvPrimaryKeyIndexBuilder().Build(schema, validation, fileName);
                    foreach (var duplicate in primaryKeys.Duplicates)
                    {
                        foreach (var occurrence in duplicate.Occurrences)
                        {
                            issues.Add(Error("PRIMARY_KEYS", "DUPLICATE_PRIMARY_KEY",
                                "Duplicate primary key: " + duplicate.Key,
                                fileName, occurrence.RecordNumber, null,
                                occurrence.PhysicalLine, occurrence.PhysicalColumn,
                                occurrence.CharOffset));
                        }
                    }

                    if (!primaryKeys.Success) continue;
                    var parse = new CsvScalarAndListParser().Parse(
                        schema, validation, primaryKeys, fileName);
                    foreach (var error in parse.Errors)
                    {
                        issues.Add(Error("VALUE_PARSE", error.ErrorCode.ToString(),
                            error.Message, fileName, error.RecordNumber, error.ColumnName,
                            error.PhysicalLine, error.PhysicalColumn, error.CharOffset));
                    }

                    if (parse.Success) parsed.Add(fileName, parse);
                }
            }

            IReadOnlyDictionary<string, CsvScalarAndListParseResult> effectiveParsed = parsed;
            if (aggregateIndependentFailures)
            {
                var independent = new Dictionary<string, CsvScalarAndListParseResult>(
                    parsed, StringComparer.Ordinal);
                foreach (var fileName in ForeignKeySourceSet.ExpectedFileNames)
                {
                    if (!independent.ContainsKey(fileName))
                        independent.Add(fileName, ParseAuthoringFallback(sourceRoot, catalog, fileName));
                }

                effectiveParsed = independent;
            }

            WorldRouteDefinitionSet world = null;
            BiomeBoundaryDefinitionSet biome = null;
            SpecialVillageDefinitionSet special = null;
            MicrochunkPopulationItemDefinitionSet micro = null;
            BuildDefinitions(catalog, effectiveParsed, issues, out world, out biome, out special, out micro);

            ForeignKeySourceSet sourceSet = null;
            ForeignKeyResolutionResult foreignKeys = null;
            if (catalog != null && ForeignKeySourceSet.ExpectedFileNames.All(effectiveParsed.ContainsKey))
            {
                sourceSet = new ForeignKeySourceSet(catalog,
                    ForeignKeySourceSet.ExpectedFileNames.Select(fileName =>
                        new ForeignKeySourceSet.Source(catalog.GetFile(fileName), effectiveParsed[fileName])));
                foreignKeys = new ForeignKeyResolver().Resolve(sourceSet);
                foreach (var error in foreignKeys.Errors)
                {
                    issues.Add(Error("FOREIGN_KEYS", error.ErrorCode.ToString(), error.Message,
                        error.SourceFileName, error.SourceRecordNumber, error.SourceColumnName,
                        error.SourceLocation?.PhysicalLine, error.SourceLocation?.PhysicalColumn,
                        error.SourceLocation?.CharOffset, error.TargetFileName,
                        error.TargetColumnName, error.TargetValue));
                }
            }
            else
            {
                issues.Add(Error("FOREIGN_KEYS", "STAGE_SKIPPED",
                    "One or more of the exact 49 parsed sources is unavailable."));
            }

            StaticDataRegistry registry = null;
            if (world != null && biome != null && special != null && micro != null &&
                foreignKeys != null && foreignKeys.Success)
            {
                var registryResult = new StaticDataRegistryBuilder().Build(
                    new StaticDataRegistryInput(world, biome, special, micro, foreignKeys));
                foreach (var error in registryResult.Errors)
                {
                    issues.Add(Error("REGISTRY", error.ErrorCode.ToString(), error.Message,
                        error.FileName, error.RecordNumber, error.DefinitionType,
                        error.SourceLocation?.PhysicalLine, error.SourceLocation?.PhysicalColumn,
                        error.SourceLocation?.CharOffset));
                }

                if (registryResult.Success) registry = registryResult.Registry;
            }
            else
            {
                issues.Add(Error("REGISTRY", "STAGE_SKIPPED",
                    "Definition or foreign-key prerequisites did not pass."));
            }

            ContentVersionHash hash = null;
            if (registry != null && sourceSet != null)
            {
                var hashResult = new ContentVersionHashCalculator().Calculate(
                    registry, sourceSet, catalog);
                foreach (var error in hashResult.Errors)
                {
                    issues.Add(Error("CONTENT_HASH", error.ErrorCode.ToString(), error.Message,
                        error.FileName, error.RecordNumber, error.FieldName,
                        error.SourceLocation?.PhysicalLine, error.SourceLocation?.PhysicalColumn,
                        error.SourceLocation?.CharOffset));
                }

                if (hashResult.Success) hash = hashResult.Hash;
            }
            else
            {
                issues.Add(Error("CONTENT_HASH", "STAGE_SKIPPED",
                    "Registry/source-set prerequisites did not pass."));
            }

            var report = new StaticDataAtomicPublisher(store).Publish(
                new StaticDataPublishRequest(registry, hash, OrderIssues(issues), attemptId));
            return new CsvFixtureImportResult(
                report,
                registry,
                hash,
                catalog,
                new ReadOnlyDictionary<string, string>(rawHashes));
        }

        private static CsvScalarAndListParseResult ParseAuthoringFallback(
            string sourceRoot,
            CsvSchemaCatalog catalog,
            string fileName)
        {
            var sourcePath = Directory.GetFiles(sourceRoot, fileName, SearchOption.AllDirectories)
                .Single();
            var read = new Rfc4180CsvReader().Read(File.ReadAllBytes(sourcePath), fileName);
            var schema = catalog.GetFile(fileName);
            var validation = new CsvHeaderAndFieldValidator().Validate(read, schema, fileName);
            if (!validation.Success)
                throw new InvalidOperationException("Authoring fallback header validation failed: " + fileName);
            var primaryKeys = new CsvPrimaryKeyIndexBuilder().Build(schema, validation, fileName);
            if (!primaryKeys.Success)
                throw new InvalidOperationException("Authoring fallback primary-key validation failed: " + fileName);
            var parsed = new CsvScalarAndListParser().Parse(schema, validation, primaryKeys, fileName);
            if (!parsed.Success)
                throw new InvalidOperationException("Authoring fallback parse failed: " + fileName);
            return parsed;
        }

        private static void BuildDefinitions(
            CsvSchemaCatalog catalog,
            IReadOnlyDictionary<string, CsvScalarAndListParseResult> parsed,
            ICollection<CsvImportIssue> issues,
            out WorldRouteDefinitionSet world,
            out BiomeBoundaryDefinitionSet biome,
            out SpecialVillageDefinitionSet special,
            out MicrochunkPopulationItemDefinitionSet micro)
        {
            world = null;
            biome = null;
            special = null;
            micro = null;
            if (CanBuild(WorldRouteFiles, catalog, parsed))
            {
                var result = new WorldRouteDefinitionBuilder().Build(WorldRouteFiles.Select(file =>
                    new WorldRouteDefinitionSource(catalog.GetFile(file), parsed[file])));
                foreach (var error in result.Errors)
                    issues.Add(DefinitionError("WORLD_ROUTE_DEFINITIONS", error.FileName,
                        error.ErrorCode.ToString(), error.Message, error.RecordNumber,
                        error.ColumnName, error.Location));
                if (result.Success) world = result.DefinitionSet;
            }
            else issues.Add(Error("WORLD_ROUTE_DEFINITIONS", "STAGE_SKIPPED",
                "Parsed prerequisites are unavailable."));

            if (CanBuild(BiomeBoundaryFiles, catalog, parsed))
            {
                var result = new BiomeBoundaryDefinitionBuilder().Build(BiomeBoundaryFiles.Select(file =>
                    new BiomeBoundaryDefinitionSource(catalog.GetFile(file), parsed[file])));
                foreach (var error in result.Errors)
                    issues.Add(DefinitionError("BIOME_BOUNDARY_DEFINITIONS", error.FileName,
                        error.ErrorCode.ToString(), error.Message, error.RecordNumber,
                        error.ColumnName, error.Location));
                if (result.Success) biome = result.DefinitionSet;
            }
            else issues.Add(Error("BIOME_BOUNDARY_DEFINITIONS", "STAGE_SKIPPED",
                "Parsed prerequisites are unavailable."));

            if (CanBuild(SpecialVillageFiles, catalog, parsed))
            {
                var result = new SpecialVillageDefinitionBuilder().Build(
                    SpecialVillageFiles.Select(file =>
                        new SpecialVillageDefinitionSource(catalog.GetFile(file), parsed[file])));
                foreach (var error in result.Errors)
                    issues.Add(DefinitionError("SPECIAL_VILLAGE_DEFINITIONS", error.FileName,
                        error.ErrorCode.ToString(), error.Message, error.RecordNumber,
                        error.ColumnName, error.Location));
                if (result.Success) special = result.DefinitionSet;
            }
            else issues.Add(Error("SPECIAL_VILLAGE_DEFINITIONS", "STAGE_SKIPPED",
                "Parsed prerequisites are unavailable."));

            if (CanBuildMicrochunk(catalog, parsed))
            {
                var result = new MicrochunkPopulationItemDefinitionBuilder().Build(
                    MicrochunkPopulationItemDefinitionSource.ExpectedFileNames.Select(file =>
                        new MicrochunkPopulationItemDefinitionSource(
                            catalog.GetFile(file), parsed[file])));
                foreach (var error in result.Errors)
                    issues.Add(DefinitionError("MICROCHUNK_POPULATION_DEFINITIONS", error.FileName,
                        error.ErrorCode.ToString(), error.Message, error.RecordNumber,
                        error.ColumnName, error.Location));
                if (result.Success) micro = result.DefinitionSet;
            }
            else issues.Add(Error("MICROCHUNK_POPULATION_DEFINITIONS", "STAGE_SKIPPED",
                "Parsed prerequisites are unavailable."));
        }

        private static bool CanBuild(
            IEnumerable<string> files,
            CsvSchemaCatalog catalog,
            IReadOnlyDictionary<string, CsvScalarAndListParseResult> parsed)
        {
            return catalog != null && files.All(parsed.ContainsKey);
        }

        private static bool CanBuildMicrochunk(
            CsvSchemaCatalog catalog,
            IReadOnlyDictionary<string, CsvScalarAndListParseResult> parsed)
        {
            var expected = MicrochunkPopulationItemDefinitionSource.ExpectedFileNames;
            return expected.Count == 17 &&
                   expected.Distinct(StringComparer.Ordinal).Count() == expected.Count &&
                   catalog != null &&
                   expected.All(parsed.ContainsKey);
        }

        private static CsvImportIssue DefinitionError(
            string stage,
            string file,
            string code,
            string message,
            int? record,
            string field,
            CsvSourceLocation? location)
        {
            return Error(stage, code, message, file, record, field,
                location?.PhysicalLine, location?.PhysicalColumn, location?.CharOffset);
        }

        private static IEnumerable<CsvImportIssue> OrderIssues(IEnumerable<CsvImportIssue> issues)
        {
            return issues.OrderBy(issue =>
                    string.Equals(issue.Severity, CsvImportIssue.ErrorSeverity,
                        StringComparison.Ordinal) ? 0 : 1)
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
                .ThenBy(issue => issue.Offset)
                .ToArray();
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
            return new CsvImportIssue(stage, CsvImportIssue.ErrorSeverity, code, message,
                sourceFile, record, field, line, column, offset,
                targetFile, targetColumn, targetValue);
        }

        private static Document ReadDocument(string fileName, byte[] bytes)
        {
            var read = new Rfc4180CsvReader().Read(bytes, fileName);
            if (!read.Success)
                throw new InvalidOperationException("Fixture source is not readable: " + fileName);
            var hadBom = HasBom(bytes);
            var content = StrictUtf8.GetString(bytes, hadBom ? Utf8Bom.Length : 0,
                bytes.Length - (hadBom ? Utf8Bom.Length : 0));
            var newline = content.Contains("\r\n") ? "\r\n" : "\n";
            return new Document(read, hadBom, content, newline);
        }

        private static byte[] WithOriginalBom(bool hadBom, string content)
        {
            var body = StrictUtf8.GetBytes(content);
            if (!hadBom) return body;
            var bytes = new byte[Utf8Bom.Length + body.Length];
            Buffer.BlockCopy(Utf8Bom, 0, bytes, 0, Utf8Bom.Length);
            Buffer.BlockCopy(body, 0, bytes, Utf8Bom.Length, body.Length);
            return bytes;
        }

        private static bool HasBom(byte[] bytes)
        {
            return bytes.Length >= Utf8Bom.Length && bytes[0] == Utf8Bom[0] &&
                   bytes[1] == Utf8Bom[1] && bytes[2] == Utf8Bom[2];
        }

        private static string EncodeField(string value)
        {
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string Replace(string source, IEnumerable<Replacement> replacements)
        {
            var result = source;
            foreach (var replacement in replacements.OrderByDescending(item => item.Start))
            {
                result = result.Remove(replacement.Start, replacement.End - replacement.Start)
                    .Insert(replacement.Start, replacement.Value);
            }

            return result;
        }

        private static string ReplaceFirstListItem(string value, string replacement)
        {
            var items = value.Split('|');
            items[0] = replacement;
            return string.Join("|", items);
        }

        private static CsvFailureMutation Descriptor(
            string mutationName,
            string fileName,
            string columnName,
            int recordNumber,
            int sourceLine,
            string before,
            string after,
            byte[] beforeBytes,
            byte[] afterBytes)
        {
            return new CsvFailureMutation(mutationName, fileName, columnName,
                recordNumber, sourceLine, before, after,
                Sha256(beforeBytes), Sha256(afterBytes));
        }

        private static string Sha256(byte[] bytes)
        {
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(bytes))
                    .Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(CsvFailureFixtureFactory));
        }

        private sealed class Document
        {
            public Document(CsvReadResult read, bool hadBom, string content, string newLine)
            {
                Read = read;
                HadBom = hadBom;
                Content = content;
                NewLine = newLine;
            }

            public CsvReadResult Read { get; }
            public bool HadBom { get; }
            public string Content { get; }
            public string NewLine { get; }
        }

        private sealed class FieldTarget
        {
            public FieldTarget(
                CsvFileSchema schema,
                CsvColumnSchema column,
                CsvRecord record,
                CsvField field)
            {
                Schema = schema;
                Column = column;
                Record = record;
                Field = field;
            }

            public CsvFileSchema Schema { get; }
            public CsvColumnSchema Column { get; }
            public CsvRecord Record { get; }
            public CsvField Field { get; }
        }

        private readonly struct Replacement
        {
            public Replacement(int start, int end, string value)
            {
                Start = start;
                End = end;
                Value = value;
            }

            public int Start { get; }
            public int End { get; }
            public string Value { get; }
        }
    }
}
