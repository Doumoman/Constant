using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.MicroPatterns;
using UnityEngine;

namespace StarNight.MapAuthoring.WorldGeneration.Import
{
    public sealed class MicroPatternCsvImportError :
        IEquatable<MicroPatternCsvImportError>,
        IComparable<MicroPatternCsvImportError>
    {
        public MicroPatternCsvImportError(
            MicroPatternCellSchemaErrorCode code,
            string filePath,
            int recordNumber,
            int fieldNumber,
            string patternId,
            int? x,
            int? y,
            string layer,
            string field,
            string detail)
        {
            Code = code;
            FilePath = filePath ?? string.Empty;
            RecordNumber = recordNumber;
            FieldNumber = fieldNumber;
            PatternId = patternId ?? string.Empty;
            X = x;
            Y = y;
            Layer = layer ?? string.Empty;
            Field = field ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public MicroPatternCellSchemaErrorCode Code { get; }
        public string FilePath { get; }
        public int RecordNumber { get; }
        public int FieldNumber { get; }
        public string PatternId { get; }
        public int? X { get; }
        public int? Y { get; }
        public string Layer { get; }
        public string Field { get; }
        public string Detail { get; }

        public int CompareTo(MicroPatternCsvImportError other)
        {
            if (other == null) return -1;
            var comparison = string.Compare(FilePath, other.FilePath, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = RecordNumber.CompareTo(other.RecordNumber);
            if (comparison != 0) return comparison;
            comparison = FieldNumber.CompareTo(other.FieldNumber);
            if (comparison != 0) return comparison;
            comparison = string.Compare(PatternId, other.PatternId, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = CompareNullable(X, other.X);
            if (comparison != 0) return comparison;
            comparison = CompareNullable(Y, other.Y);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Layer, other.Layer, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Field, other.Field, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(MicroPatternCsvImportError other)
        {
            return other != null && CompareTo(other) == 0;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MicroPatternCsvImportError);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Code;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(FilePath);
                hash = (hash * 397) ^ RecordNumber;
                hash = (hash * 397) ^ FieldNumber;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(PatternId);
                hash = (hash * 397) ^ X.GetHashCode();
                hash = (hash * 397) ^ Y.GetHashCode();
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Layer);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Field);
                return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Detail);
            }
        }

        public override string ToString()
        {
            return Code + "|" + FilePath + "|record=" + RecordNumber +
                   "|field=" + FieldNumber + "|pattern=" + PatternId +
                   "|x=" + Number(X) + "|y=" + Number(Y) + "|layer=" + Layer +
                   "|column=" + Field + "|" + Detail;
        }

        private static int CompareNullable(int? left, int? right)
        {
            if (!left.HasValue) return right.HasValue ? -1 : 0;
            return right.HasValue ? left.Value.CompareTo(right.Value) : 1;
        }

        private static string Number(int? value)
        {
            return value.HasValue
                ? value.Value.ToString(CultureInfo.InvariantCulture)
                : string.Empty;
        }
    }

    public sealed class MicroPatternCsvImportResult
    {
        private readonly ReadOnlyCollection<MicroPatternCsvImportError> errors;

        internal MicroPatternCsvImportResult(
            MicroPatternAuthoringCatalog catalog,
            bool isHeaderOnly,
            IEnumerable<MicroPatternCsvImportError> sourceErrors)
        {
            var ordered = (sourceErrors ?? throw new ArgumentNullException(nameof(sourceErrors)))
                .Where(value => value != null)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            errors = new ReadOnlyCollection<MicroPatternCsvImportError>(ordered);
            if (ordered.Length > 0 && catalog != null)
                throw new ArgumentException("A failed import cannot publish a catalog.");
            Catalog = ordered.Length == 0 ? catalog : null;
            IsHeaderOnly = ordered.Length == 0 && isHeaderOnly;
        }

        public bool Success => errors.Count == 0;
        public bool Published => Catalog != null;
        public bool IsHeaderOnly { get; }
        public MicroPatternAuthoringCatalog Catalog { get; }
        public IReadOnlyList<MicroPatternCsvImportError> Errors => errors;
        public string StableDigest => Catalog == null ? string.Empty : Catalog.StableDigest;
    }

    public sealed class MicroPatternCsvImporterV2
    {
        public const string CatalogProjectRelativePath =
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroPattern/micro_pattern_catalog_v2.csv";
        public const string CellsProjectRelativePath =
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroPattern/micro_pattern_cells_v2.csv";
        public const string CatalogExpectedHeader =
            "pattern_id,selection_weight,biome_ids,allowed_transforms,protected_policy";
        public const string CellsExpectedHeader =
            "pattern_id,local_x,local_y,operation,layer,payload_id";

        private static readonly CsvSchemaCatalog Schemas = BuildSchemas();

        public MicroPatternCsvImportResult Import()
        {
            var errors = new List<MicroPatternCsvImportError>();
            var catalogBytes = ReadExact(CatalogProjectRelativePath, errors);
            var cellBytes = ReadExact(CellsProjectRelativePath, errors);
            if (errors.Count > 0)
            {
                errors.Add(AtomicError());
                return new MicroPatternCsvImportResult(null, false, errors);
            }

            return ParseBytes(catalogBytes, cellBytes);
        }

        public MicroPatternCsvImportResult ParseBytes(byte[] catalogBytes, byte[] cellBytes)
        {
            if (catalogBytes == null) throw new ArgumentNullException(nameof(catalogBytes));
            if (cellBytes == null) throw new ArgumentNullException(nameof(cellBytes));

            var errors = new List<MicroPatternCsvImportError>();
            var catalogRecords = ReadAndValidate(
                catalogBytes, CatalogProjectRelativePath, CatalogExpectedHeader,
                Schemas.GetFile("micro_pattern_catalog_v2.csv"), errors);
            var cellRecords = ReadAndValidate(
                cellBytes, CellsProjectRelativePath, CellsExpectedHeader,
                Schemas.GetFile("micro_pattern_cells_v2.csv"), errors);
            if (errors.Count > 0)
            {
                errors.Add(AtomicError());
                return new MicroPatternCsvImportResult(null, false, errors);
            }

            var catalogRows = catalogRecords.Select(record => new MicroPatternCatalogRowV2(
                record.Fields[0].Value,
                record.Fields[1].Value,
                record.Fields[2].Value,
                record.Fields[3].Value,
                record.Fields[4].Value,
                CatalogProjectRelativePath,
                record.RecordNumber));
            var cellRows = cellRecords.Select(record => new MicroPatternCellRowV2(
                record.Fields[0].Value,
                record.Fields[1].Value,
                record.Fields[2].Value,
                record.Fields[3].Value,
                record.Fields[4].Value,
                record.Fields[5].Value,
                CellsProjectRelativePath,
                record.RecordNumber));

            var build = new MicroPatternCellSchemaBuilder().Build(catalogRows, cellRows);
            if (!build.Success)
            {
                errors.AddRange(build.Errors.Select(FromSchemaError));
                return new MicroPatternCsvImportResult(null, false, errors);
            }

            return new MicroPatternCsvImportResult(
                build.Catalog,
                build.IsHeaderOnly,
                Array.Empty<MicroPatternCsvImportError>());
        }

        private static IReadOnlyList<CsvRecord> ReadAndValidate(
            byte[] bytes,
            string sourcePath,
            string expectedHeader,
            CsvFileSchema schema,
            ICollection<MicroPatternCsvImportError> errors)
        {
            var read = new Rfc4180CsvReader().Read(bytes, sourcePath);
            if (!read.Success)
            {
                foreach (var error in read.Errors)
                {
                    errors.Add(new MicroPatternCsvImportError(
                        MicroPatternCellSchemaErrorCode.CsvSyntaxError,
                        sourcePath,
                        error.Location.RecordNumber,
                        error.Location.FieldNumber,
                        string.Empty, null, null, string.Empty, string.Empty,
                        error.ToString()));
                }
                return Array.Empty<CsvRecord>();
            }

            if (!read.HadUtf8Bom)
            {
                errors.Add(new MicroPatternCsvImportError(
                    MicroPatternCellSchemaErrorCode.InvalidBom,
                    sourcePath, 1, 1, string.Empty, null, null, string.Empty,
                    "BOM", "UTF-8 BOM is required."));
            }

            var expected = expectedHeader.Split(',');
            if (read.Records.Count == 0 || !HeaderMatches(read.Records[0], expected))
            {
                errors.Add(new MicroPatternCsvImportError(
                    MicroPatternCellSchemaErrorCode.HeaderMismatch,
                    sourcePath, 1, 1, string.Empty, null, null, string.Empty,
                    "header", "Expected exact header: " + expectedHeader));
            }

            var validation = new CsvHeaderAndFieldValidator().Validate(read, schema, sourcePath);
            foreach (var error in validation.Errors)
            {
                if (error.ErrorCode == CsvHeaderFieldErrorCode.RequiredFieldEmpty) continue;
                var code = error.ErrorCode == CsvHeaderFieldErrorCode.FieldCountMismatch
                    ? MicroPatternCellSchemaErrorCode.RowFieldCountMismatch
                    : MicroPatternCellSchemaErrorCode.HeaderMismatch;
                errors.Add(new MicroPatternCsvImportError(
                    code, sourcePath, error.RecordNumber, error.FieldNumber,
                    string.Empty, null, null, string.Empty,
                    error.ErrorCode == CsvHeaderFieldErrorCode.FieldCountMismatch
                        ? "row"
                        : "header",
                    error.Message));
            }

            if (errors.Any(value => string.Equals(value.FilePath, sourcePath, StringComparison.Ordinal)))
                return Array.Empty<CsvRecord>();

            return read.Records.Skip(1).ToArray();
        }

        private static bool HeaderMatches(CsvRecord record, IReadOnlyList<string> expected)
        {
            if (record.Fields.Count != expected.Count) return false;
            for (var index = 0; index < expected.Count; index++)
            {
                if (record.Fields[index].WasQuoted ||
                    !string.Equals(record.Fields[index].Value, expected[index], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static byte[] ReadExact(
            string projectRelativePath,
            ICollection<MicroPatternCsvImportError> errors)
        {
            try
            {
                var fullPath = ProjectPath(projectRelativePath);
                if (!File.Exists(fullPath))
                {
                    errors.Add(new MicroPatternCsvImportError(
                        MicroPatternCellSchemaErrorCode.MissingInputFile,
                        projectRelativePath, 0, 0, string.Empty, null, null,
                        string.Empty, "file", "Required exact-path input is missing."));
                    return Array.Empty<byte>();
                }

                return File.ReadAllBytes(fullPath);
            }
            catch (Exception exception)
            {
                errors.Add(new MicroPatternCsvImportError(
                    MicroPatternCellSchemaErrorCode.MissingInputFile,
                    projectRelativePath, 0, 0, string.Empty, null, null,
                    string.Empty, "file", exception.Message));
                return Array.Empty<byte>();
            }
        }

        private static string ProjectPath(string projectRelativePath)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var normalizedRoot = projectRoot.TrimEnd(
                                     Path.DirectorySeparatorChar,
                                     Path.AltDirectorySeparatorChar) +
                                 Path.DirectorySeparatorChar;
            var fullPath = Path.GetFullPath(Path.Combine(
                projectRoot,
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("MicroPattern CSV path escaped the project root.");
            return fullPath;
        }

        private static CsvSchemaCatalog BuildSchemas()
        {
            var tablePaths = new HashSet<string>(new[]
            {
                "MicroPattern/micro_pattern_catalog_v2.csv",
                "MicroPattern/micro_pattern_cells_v2.csv",
            }, StringComparer.Ordinal);
            var sourceRow = 1;
            var rows = V2AuthoringSchemaRegistry.DescribeDefaultTables()
                .Where(table => tablePaths.Contains(table.RelativeAuthoringPath))
                .OrderBy(table => table.RelativeAuthoringPath, StringComparer.Ordinal)
                .SelectMany(table => table.Columns.OrderBy(column => column.ColumnOrder)
                    .Select(column => new CsvSchemaDictionaryRow(
                        table.FileName,
                        column.ColumnOrder.ToString(CultureInfo.InvariantCulture),
                        column.ColumnName,
                        CsvSchemaDataTypes.ToToken(column.DataType),
                        column.IsRequired ? "1" : "0",
                        column.PrimaryKeyOrder.HasValue
                            ? column.PrimaryKeyOrder.Value.ToString(CultureInfo.InvariantCulture)
                            : string.Empty,
                        column.DefaultValue,
                        string.Join("|", column.AllowedValues),
                        string.Empty,
                        column.Description,
                        sourceRow++)))
                .ToArray();
            var build = new CsvSchemaCatalogBuilder().Build(rows);
            if (!build.Success || build.Catalog.FileCount != 2)
                throw new InvalidOperationException("Approved MicroPattern V2 schemas could not be materialized.");
            return build.Catalog;
        }

        private static MicroPatternCsvImportError FromSchemaError(
            MicroPatternCellSchemaError error)
        {
            return new MicroPatternCsvImportError(
                error.Code, error.SourceFile, error.RecordNumber, 0,
                error.PatternId, error.X, error.Y, error.Layer, error.Field, error.Detail);
        }

        private static MicroPatternCsvImportError AtomicError()
        {
            return new MicroPatternCsvImportError(
                MicroPatternCellSchemaErrorCode.AtomicPublishRejected,
                string.Empty, 0, 0, string.Empty, null, null,
                string.Empty, "catalog", "Import errors rejected atomic publication.");
        }
    }
}
