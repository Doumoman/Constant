using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.TerrainClusters.Authoring;
using UnityEngine;

namespace StarNight.MapAuthoring.WorldGeneration.Import
{
    public enum TerrainClusterCsvImportErrorCode
    {
        MissingInputFile = 1,
        InvalidBom = 2,
        InvalidLineEnding = 3,
        CsvSyntax = 4,
        HeaderMismatch = 5,
        FieldValidation = 6,
        NonCanonicalRowOrder = 7,
        AuthoringValidation = 8,
        AtomicPublishRejected = 9,
    }

    public sealed class TerrainClusterCsvImportError :
        IEquatable<TerrainClusterCsvImportError>,
        IComparable<TerrainClusterCsvImportError>
    {
        public TerrainClusterCsvImportError(
            TerrainClusterCsvImportErrorCode code,
            string filePath,
            int recordNumber,
            int fieldNumber,
            string columnName,
            string detail)
        {
            Code = code;
            FilePath = (filePath ?? string.Empty).Replace('\\', '/');
            RecordNumber = recordNumber;
            FieldNumber = fieldNumber;
            ColumnName = columnName ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public TerrainClusterCsvImportErrorCode Code { get; }
        public string FilePath { get; }
        public int RecordNumber { get; }
        public int FieldNumber { get; }
        public string ColumnName { get; }
        public string Detail { get; }

        public int CompareTo(TerrainClusterCsvImportError other)
        {
            if (other == null) return -1;
            var comparison = string.Compare(FilePath, other.FilePath, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = RecordNumber.CompareTo(other.RecordNumber);
            if (comparison != 0) return comparison;
            comparison = FieldNumber.CompareTo(other.FieldNumber);
            if (comparison != 0) return comparison;
            comparison = string.Compare(ColumnName, other.ColumnName, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = Code.CompareTo(other.Code);
            return comparison != 0
                ? comparison
                : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(TerrainClusterCsvImportError other)
        {
            return other != null && CompareTo(other) == 0;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TerrainClusterCsvImportError);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(ToString());
        }

        public override string ToString()
        {
            return Code + "|" + FilePath + "|record=" + RecordNumber +
                   "|field=" + FieldNumber + "|column=" + ColumnName + "|" + Detail;
        }
    }

    public sealed class TerrainClusterCsvImportResult
    {
        private readonly ReadOnlyCollection<TerrainClusterCsvImportError> errors;

        internal TerrainClusterCsvImportResult(
            TerrainClusterAuthoringCatalog catalog,
            IEnumerable<TerrainClusterCsvImportError> sourceErrors)
        {
            var ordered = (sourceErrors ?? throw new ArgumentNullException(nameof(sourceErrors)))
                .Where(value => value != null)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            errors = new ReadOnlyCollection<TerrainClusterCsvImportError>(ordered);
            if (ordered.Length > 0 && catalog != null)
                throw new ArgumentException("A failed import cannot publish a TerrainCluster catalog.");
            Catalog = ordered.Length == 0 ? catalog : null;
        }

        public bool Success => errors.Count == 0 && Catalog != null;
        public bool Published => Catalog != null;
        public TerrainClusterAuthoringCatalog Catalog { get; }
        public IReadOnlyList<TerrainClusterCsvImportError> Errors => errors;
        public string StableDigest => Catalog == null ? string.Empty : Catalog.StableDigest;
    }

    public sealed class TerrainClusterCsvImporterV2
    {
        public const string AuthoringRootProjectRelativePath =
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/";
        public const string TerrainClusterRootProjectRelativePath =
            AuthoringRootProjectRelativePath + "TerrainCluster/";

        public static readonly IReadOnlyList<string> ProjectRelativePaths =
            new ReadOnlyCollection<string>(new[]
            {
                TerrainClusterRootProjectRelativePath + "terrain_cluster_catalog_v2.csv",
                TerrainClusterRootProjectRelativePath + "terrain_cluster_cells_v2.csv",
                TerrainClusterRootProjectRelativePath + "terrain_cluster_envelope_cells_v2.csv",
                TerrainClusterRootProjectRelativePath + "terrain_cluster_high_route_benefits_v2.csv",
                TerrainClusterRootProjectRelativePath + "terrain_cluster_high_route_edges_v2.csv",
                TerrainClusterRootProjectRelativePath + "terrain_cluster_high_route_failures_v2.csv",
                TerrainClusterRootProjectRelativePath + "terrain_cluster_high_routes_v2.csv",
                TerrainClusterRootProjectRelativePath + "terrain_cluster_nodes_v2.csv",
                TerrainClusterRootProjectRelativePath + "terrain_cluster_ports_v2.csv",
                TerrainClusterRootProjectRelativePath + "terrain_cluster_role_anchors_v2.csv",
                TerrainClusterRootProjectRelativePath + "terrain_cluster_role_variant_links_v2.csv",
                TerrainClusterRootProjectRelativePath + "terrain_cluster_spine_edges_v2.csv",
                TerrainClusterRootProjectRelativePath + "terrain_cluster_variants_v2.csv",
            });

        private static readonly V2AuthoringTableDescriptor[] Descriptors =
            V2AuthoringSchemaRegistry.DescribeDefaultTables()
                .Where(value => value.Owner == V2AuthoringOwner.TerrainCluster)
                .OrderBy(value => value.RelativeAuthoringPath, StringComparer.Ordinal)
                .ToArray();

        private static readonly CsvSchemaCatalog Schemas = BuildSchemas();

        public TerrainClusterCsvImportResult Import()
        {
            var errors = new List<TerrainClusterCsvImportError>();
            var bytes = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (var path in ProjectRelativePaths)
            {
                try
                {
                    var fullPath = ProjectPath(path);
                    if (!File.Exists(fullPath))
                    {
                        errors.Add(new TerrainClusterCsvImportError(
                            TerrainClusterCsvImportErrorCode.MissingInputFile,
                            path, 0, 0, "file", "Required exact-path input is missing."));
                        continue;
                    }
                    bytes.Add(path, File.ReadAllBytes(fullPath));
                }
                catch (Exception exception)
                {
                    errors.Add(new TerrainClusterCsvImportError(
                        TerrainClusterCsvImportErrorCode.MissingInputFile,
                        path, 0, 0, "file", exception.Message));
                }
            }
            if (errors.Count > 0) return Failed(errors);
            return ParseBytes(bytes);
        }

        public TerrainClusterCsvImportResult ParseBytes(
            IReadOnlyDictionary<string, byte[]> sourceBytes)
        {
            if (sourceBytes == null) throw new ArgumentNullException(nameof(sourceBytes));
            var errors = new List<TerrainClusterCsvImportError>();
            var rows = new List<TerrainClusterAuthoringRow>();
            foreach (var descriptor in Descriptors)
            {
                var projectPath = AuthoringRootProjectRelativePath + descriptor.RelativeAuthoringPath;
                byte[] bytes;
                if (!sourceBytes.TryGetValue(projectPath, out bytes) || bytes == null)
                {
                    errors.Add(new TerrainClusterCsvImportError(
                        TerrainClusterCsvImportErrorCode.MissingInputFile,
                        projectPath, 0, 0, "file", "Required exact-path input is missing."));
                    continue;
                }
                rows.AddRange(ReadAndValidate(bytes, projectPath, descriptor, errors));
            }
            if (errors.Count > 0) return Failed(errors);

            var build = TerrainClusterAuthoringValidation.Build(rows);
            if (!build.Success)
            {
                errors.AddRange(build.Errors.Select(error => new TerrainClusterCsvImportError(
                    TerrainClusterCsvImportErrorCode.AuthoringValidation,
                    AuthoringRootProjectRelativePath + error.TablePath,
                    error.RecordNumber, 0, error.ColumnName, error.ToString())));
                return Failed(errors);
            }
            return new TerrainClusterCsvImportResult(
                build.Catalog, Array.Empty<TerrainClusterCsvImportError>());
        }

        private static IReadOnlyList<TerrainClusterAuthoringRow> ReadAndValidate(
            byte[] bytes,
            string projectPath,
            V2AuthoringTableDescriptor descriptor,
            ICollection<TerrainClusterCsvImportError> errors)
        {
            if (bytes.Length < 4 || bytes[0] != 0xef || bytes[1] != 0xbb || bytes[2] != 0xbf)
            {
                errors.Add(new TerrainClusterCsvImportError(
                    TerrainClusterCsvImportErrorCode.InvalidBom,
                    projectPath, 1, 1, "BOM", "UTF-8 BOM is required."));
            }
            if (bytes.Contains((byte)'\r') || bytes.Length == 0 || bytes[bytes.Length - 1] != (byte)'\n' ||
                (bytes.Length > 1 && bytes[bytes.Length - 2] == (byte)'\n'))
            {
                errors.Add(new TerrainClusterCsvImportError(
                    TerrainClusterCsvImportErrorCode.InvalidLineEnding,
                    projectPath, 0, 0, "line-ending", "LF-only with exactly one final LF is required."));
            }

            var read = new Rfc4180CsvReader().Read(bytes, projectPath);
            if (!read.Success)
            {
                foreach (var error in read.Errors)
                {
                    errors.Add(new TerrainClusterCsvImportError(
                        TerrainClusterCsvImportErrorCode.CsvSyntax,
                        projectPath, error.Location.RecordNumber, error.Location.FieldNumber,
                        "csv", error.ToString()));
                }
                return Array.Empty<TerrainClusterAuthoringRow>();
            }

            var expectedHeader = descriptor.Columns.OrderBy(value => value.ColumnOrder)
                .Select(value => value.ColumnName).ToArray();
            if (read.Records.Count == 0 || !HeaderMatches(read.Records[0], expectedHeader))
            {
                errors.Add(new TerrainClusterCsvImportError(
                    TerrainClusterCsvImportErrorCode.HeaderMismatch,
                    projectPath, 1, 1, "header",
                    "Expected exact header: " + string.Join(",", expectedHeader)));
            }

            var validation = new CsvHeaderAndFieldValidator().Validate(
                read, Schemas.GetFile(descriptor.FileName), projectPath);
            foreach (var error in validation.Errors)
            {
                errors.Add(new TerrainClusterCsvImportError(
                    TerrainClusterCsvImportErrorCode.FieldValidation,
                    projectPath, error.RecordNumber, error.FieldNumber,
                    error.ExpectedValue, error.Message));
            }

            if (errors.Any(value => string.Equals(value.FilePath, projectPath, StringComparison.Ordinal)))
                return Array.Empty<TerrainClusterAuthoringRow>();

            var result = read.Records.Skip(1).Select(record =>
                new TerrainClusterAuthoringRow(
                    descriptor.RelativeAuthoringPath,
                    record.RecordNumber,
                    descriptor.Columns.OrderBy(value => value.ColumnOrder)
                        .Select((column, index) =>
                            new KeyValuePair<string, string>(
                                column.ColumnName, record.Fields[index].Value)))).ToArray();
            var primary = descriptor.Columns.Where(value => value.PrimaryKeyOrder.HasValue)
                .OrderBy(value => value.PrimaryKeyOrder.Value).ToArray();
            var keys = result.Select(row => CanonicalKey(row, primary)).ToArray();
            if (!keys.SequenceEqual(keys.OrderBy(value => value, StringComparer.Ordinal)))
            {
                errors.Add(new TerrainClusterCsvImportError(
                    TerrainClusterCsvImportErrorCode.NonCanonicalRowOrder,
                    projectPath, 0, 0, "primary-key",
                    "Rows must be canonical by registered primary-key order."));
                return Array.Empty<TerrainClusterAuthoringRow>();
            }
            return result;
        }

        private static bool HeaderMatches(CsvRecord record, IReadOnlyList<string> expected)
        {
            if (record.Fields.Count != expected.Count) return false;
            for (var index = 0; index < expected.Count; index++)
            {
                if (record.Fields[index].WasQuoted ||
                    !string.Equals(record.Fields[index].Value, expected[index], StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        private static string CanonicalKey(
            TerrainClusterAuthoringRow row,
            IEnumerable<V2AuthoringColumnDescriptor> primary)
        {
            return string.Join("\u001f", primary.Select(column =>
            {
                var value = row.Get(column.ColumnName);
                int parsed;
                return column.DataType == CsvSchemaDataType.Int &&
                       int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
                    ? (parsed < 0 ? "0" : "1") +
                      Math.Abs((long)parsed).ToString("D12", CultureInfo.InvariantCulture)
                    : value;
            }));
        }

        private static CsvSchemaCatalog BuildSchemas()
        {
            var sourceRow = 1;
            var rows = Descriptors.SelectMany(table => table.Columns
                .OrderBy(column => column.ColumnOrder)
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
                    sourceRow++))).ToArray();
            var build = new CsvSchemaCatalogBuilder().Build(rows);
            if (!build.Success || build.Catalog.FileCount != 13)
                throw new InvalidOperationException("Approved TerrainCluster V2 schemas could not be materialized.");
            return build.Catalog;
        }

        private static TerrainClusterCsvImportResult Failed(
            ICollection<TerrainClusterCsvImportError> errors)
        {
            if (!errors.Any(value => value.Code == TerrainClusterCsvImportErrorCode.AtomicPublishRejected))
            {
                errors.Add(new TerrainClusterCsvImportError(
                    TerrainClusterCsvImportErrorCode.AtomicPublishRejected,
                    string.Empty, 0, 0, "catalog",
                    "Import errors rejected atomic TerrainCluster publication."));
            }
            return new TerrainClusterCsvImportResult(null, errors);
        }

        private static string ProjectPath(string projectRelativePath)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var normalizedRoot = projectRoot.TrimEnd(
                                     Path.DirectorySeparatorChar,
                                     Path.AltDirectorySeparatorChar) +
                                 Path.DirectorySeparatorChar;
            var fullPath = Path.GetFullPath(Path.Combine(
                projectRoot, projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("TerrainCluster CSV path escaped the project root.");
            return fullPath;
        }
    }
}
