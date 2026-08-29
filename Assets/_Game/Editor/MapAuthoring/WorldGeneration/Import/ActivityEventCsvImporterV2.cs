using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Activities.Authoring;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.EventOverlays.Authoring;
using StarNight.Map.WorldGeneration.TerrainClusters.Authoring;
using UnityEngine;

namespace StarNight.MapAuthoring.WorldGeneration.Import
{
    public enum ActivityEventCsvImportErrorCode
    {
        MissingInputFile = 1,
        InvalidBom = 2,
        InvalidLineEnding = 3,
        CsvSyntax = 4,
        HeaderMismatch = 5,
        FieldValidation = 6,
        NonCanonicalRowOrder = 7,
        ActivityValidation = 8,
        EventValidation = 9,
        AtomicPublishRejected = 10,
    }

    public sealed class ActivityEventCsvImportError : IEquatable<ActivityEventCsvImportError>, IComparable<ActivityEventCsvImportError>
    {
        public ActivityEventCsvImportError(ActivityEventCsvImportErrorCode code, string filePath,
            int recordNumber, int fieldNumber, string columnName, string detail)
        {
            Code = code;
            FilePath = (filePath ?? string.Empty).Replace('\\', '/');
            RecordNumber = recordNumber;
            FieldNumber = fieldNumber;
            ColumnName = columnName ?? string.Empty;
            Detail = detail ?? string.Empty;
        }
        public ActivityEventCsvImportErrorCode Code { get; }
        public string FilePath { get; }
        public int RecordNumber { get; }
        public int FieldNumber { get; }
        public string ColumnName { get; }
        public string Detail { get; }
        public int CompareTo(ActivityEventCsvImportError other)
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
            return comparison != 0 ? comparison : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }
        public bool Equals(ActivityEventCsvImportError other) => other != null && CompareTo(other) == 0;
        public override bool Equals(object obj) => Equals(obj as ActivityEventCsvImportError);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(ToString());
        public override string ToString() => Code + "|" + FilePath + "|record=" + RecordNumber +
            "|field=" + FieldNumber + "|column=" + ColumnName + "|" + Detail;
    }

    public sealed class ActivityEventCsvImportResult
    {
        private readonly ReadOnlyCollection<ActivityEventCsvImportError> errors;
        internal ActivityEventCsvImportResult(ActivityAuthoringCatalog activityCatalog,
            EventOverlayAuthoringCatalog eventCatalog, string aggregateDigest,
            IEnumerable<ActivityEventCsvImportError> sourceErrors)
        {
            var ordered = (sourceErrors ?? throw new ArgumentNullException(nameof(sourceErrors)))
                .Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            errors = new ReadOnlyCollection<ActivityEventCsvImportError>(ordered);
            if (ordered.Length != 0)
            {
                ActivityCatalog = null;
                EventCatalog = null;
                AggregateStableDigest = string.Empty;
            }
            else
            {
                ActivityCatalog = activityCatalog;
                EventCatalog = eventCatalog;
                AggregateStableDigest = aggregateDigest ?? string.Empty;
            }
        }
        public bool Success => ActivityCatalog != null && EventCatalog != null && errors.Count == 0;
        public bool Published => ActivityCatalog != null && EventCatalog != null;
        public ActivityAuthoringCatalog ActivityCatalog { get; }
        public EventOverlayAuthoringCatalog EventCatalog { get; }
        public string AggregateStableDigest { get; }
        public IReadOnlyList<ActivityEventCsvImportError> Errors => errors;
    }

    public sealed class ActivityEventCsvImporterV2
    {
        public const string AuthoringRootProjectRelativePath = "Assets/_Game/Map/Data/WorldGeneration/Authoring/";
        public static readonly IReadOnlyList<string> ProjectRelativePaths = new ReadOnlyCollection<string>(new[]
        {
            AuthoringRootProjectRelativePath + "Activity/activity_catalog_v2.csv",
            AuthoringRootProjectRelativePath + "Activity/activity_compatibility_v2.csv",
            AuthoringRootProjectRelativePath + "Activity/activity_cues_v2.csv",
            AuthoringRootProjectRelativePath + "Activity/activity_graph_edges_v2.csv",
            AuthoringRootProjectRelativePath + "Activity/activity_graph_nodes_v2.csv",
            AuthoringRootProjectRelativePath + "Activity/activity_safety_cells_v2.csv",
            AuthoringRootProjectRelativePath + "Activity/activity_slots_v2.csv",
            AuthoringRootProjectRelativePath + "EventOverlay/event_overlay_catalog_v2.csv",
            AuthoringRootProjectRelativePath + "EventOverlay/event_overlay_compatibility_v2.csv",
            AuthoringRootProjectRelativePath + "EventOverlay/event_overlay_markers_v2.csv",
        });

        private static readonly V2AuthoringTableDescriptor[] Descriptors =
            V2AuthoringSchemaRegistry.DescribeDefaultTables()
                .Where(value => value.Owner == V2AuthoringOwner.Activity || value.Owner == V2AuthoringOwner.EventOverlay)
                .OrderBy(value => value.RelativeAuthoringPath, StringComparer.Ordinal).ToArray();
        private static readonly CsvSchemaCatalog Schemas = BuildSchemas();

        public ActivityEventCsvImportResult Import(TerrainClusterAuthoringCatalog terrainCatalog)
        {
            if (terrainCatalog == null) throw new ArgumentNullException(nameof(terrainCatalog));
            var errors = new List<ActivityEventCsvImportError>();
            var bytes = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (var path in ProjectRelativePaths)
            {
                try
                {
                    var fullPath = ProjectPath(path);
                    if (!File.Exists(fullPath))
                    {
                        errors.Add(Error(ActivityEventCsvImportErrorCode.MissingInputFile, path, 0, 0, "file", "Required exact-path input is missing."));
                        continue;
                    }
                    bytes.Add(path, File.ReadAllBytes(fullPath));
                }
                catch (Exception exception)
                {
                    errors.Add(Error(ActivityEventCsvImportErrorCode.MissingInputFile, path, 0, 0, "file", exception.Message));
                }
            }
            return errors.Count == 0 ? ParseBytes(bytes, terrainCatalog) : Failed(errors);
        }

        public ActivityEventCsvImportResult ParseBytes(
            IReadOnlyDictionary<string, byte[]> sourceBytes,
            TerrainClusterAuthoringCatalog terrainCatalog)
        {
            if (sourceBytes == null) throw new ArgumentNullException(nameof(sourceBytes));
            if (terrainCatalog == null) throw new ArgumentNullException(nameof(terrainCatalog));
            var errors = new List<ActivityEventCsvImportError>();
            var activityRows = new List<ActivityAuthoringRow>();
            var eventRows = new List<EventOverlayAuthoringRow>();
            foreach (var descriptor in Descriptors)
            {
                var projectPath = AuthoringRootProjectRelativePath + descriptor.RelativeAuthoringPath;
                if (!sourceBytes.TryGetValue(projectPath, out var bytes) || bytes == null)
                {
                    errors.Add(Error(ActivityEventCsvImportErrorCode.MissingInputFile, projectPath, 0, 0, "file", "Required exact-path input is missing."));
                    continue;
                }
                var records = ReadAndValidate(bytes, projectPath, descriptor, errors);
                foreach (var record in records)
                {
                    var fields = descriptor.Columns.OrderBy(value => value.ColumnOrder)
                        .Select((column, index) => new KeyValuePair<string, string>(column.ColumnName, record.Fields[index].Value));
                    if (descriptor.Owner == V2AuthoringOwner.Activity)
                        activityRows.Add(new ActivityAuthoringRow(descriptor.RelativeAuthoringPath, record.RecordNumber, fields));
                    else eventRows.Add(new EventOverlayAuthoringRow(descriptor.RelativeAuthoringPath, record.RecordNumber, fields));
                }
            }
            if (errors.Count != 0) return Failed(errors);

            var activityBuild = ActivityAuthoringCatalogBuilder.Build(activityRows, terrainCatalog);
            if (!activityBuild.Success)
            {
                errors.AddRange(activityBuild.Errors.Select(error => Error(ActivityEventCsvImportErrorCode.ActivityValidation,
                    AuthoringRootProjectRelativePath + error.TablePath, error.RecordNumber, 0, error.ColumnName, error.Detail)));
                return Failed(errors);
            }
            var eventBuild = EventOverlayAuthoringCatalogBuilder.Build(eventRows, terrainCatalog, activityBuild.Catalog);
            if (!eventBuild.Success)
            {
                errors.AddRange(eventBuild.Errors.Select(error => Error(ActivityEventCsvImportErrorCode.EventValidation,
                    AuthoringRootProjectRelativePath + error.TablePath, error.RecordNumber, 0, error.ColumnName, error.Detail)));
                return Failed(errors);
            }
            var aggregate = Sha256(V2AuthoringSchemaCanonicalDigest.Compute(V2AuthoringSchemaRegistry.DescribeDefaultTables()) + "\n" +
                                   terrainCatalog.StableDigest + "\n" + activityBuild.Catalog.StableDigest + "\n" + eventBuild.Catalog.StableDigest);
            return new ActivityEventCsvImportResult(activityBuild.Catalog, eventBuild.Catalog, aggregate,
                Array.Empty<ActivityEventCsvImportError>());
        }

        private static IReadOnlyList<CsvRecord> ReadAndValidate(byte[] bytes, string projectPath,
            V2AuthoringTableDescriptor descriptor, ICollection<ActivityEventCsvImportError> errors)
        {
            if (bytes.Length < 4 || bytes[0] != 0xef || bytes[1] != 0xbb || bytes[2] != 0xbf)
                errors.Add(Error(ActivityEventCsvImportErrorCode.InvalidBom, projectPath, 1, 1, "BOM", "UTF-8 BOM is required."));
            if (bytes.Contains((byte)'\r') || bytes.Length == 0 || bytes[bytes.Length - 1] != (byte)'\n' ||
                (bytes.Length > 1 && bytes[bytes.Length - 2] == (byte)'\n'))
                errors.Add(Error(ActivityEventCsvImportErrorCode.InvalidLineEnding, projectPath, 0, 0, "line-ending", "LF-only with exactly one final LF is required."));
            var read = new Rfc4180CsvReader().Read(bytes, projectPath);
            if (!read.Success)
            {
                foreach (var error in read.Errors)
                    errors.Add(Error(ActivityEventCsvImportErrorCode.CsvSyntax, projectPath,
                        error.Location.RecordNumber, error.Location.FieldNumber, "csv", error.ToString()));
                return Array.Empty<CsvRecord>();
            }
            var expected = descriptor.Columns.OrderBy(value => value.ColumnOrder).Select(value => value.ColumnName).ToArray();
            if (read.Records.Count == 0 || !HeaderMatches(read.Records[0], expected))
                errors.Add(Error(ActivityEventCsvImportErrorCode.HeaderMismatch, projectPath, 1, 1, "header", "Expected exact header: " + string.Join(",", expected)));
            var validation = new CsvHeaderAndFieldValidator().Validate(read, Schemas.GetFile(descriptor.FileName), projectPath);
            foreach (var error in validation.Errors)
                errors.Add(Error(ActivityEventCsvImportErrorCode.FieldValidation, projectPath,
                    error.RecordNumber, error.FieldNumber, error.ExpectedValue, error.Message));
            if (errors.Any(value => value.FilePath == projectPath)) return Array.Empty<CsvRecord>();
            var records = read.Records.Skip(1).ToArray();
            var primary = descriptor.Columns.Where(value => value.PrimaryKeyOrder.HasValue)
                .OrderBy(value => value.PrimaryKeyOrder.Value).ToArray();
            var keys = records.Select(record => CanonicalKey(record, descriptor, primary)).ToArray();
            if (!keys.SequenceEqual(keys.OrderBy(value => value, StringComparer.Ordinal)))
            {
                errors.Add(Error(ActivityEventCsvImportErrorCode.NonCanonicalRowOrder, projectPath, 0, 0,
                    "primary-key", "Rows must be canonical by registered primary-key order."));
                return Array.Empty<CsvRecord>();
            }
            return records;
        }

        private static bool HeaderMatches(CsvRecord record, IReadOnlyList<string> expected)
        {
            if (record.Fields.Count != expected.Count) return false;
            for (var index = 0; index < expected.Count; index++)
                if (record.Fields[index].WasQuoted || record.Fields[index].Value != expected[index]) return false;
            return true;
        }

        private static string CanonicalKey(CsvRecord record, V2AuthoringTableDescriptor descriptor,
            IEnumerable<V2AuthoringColumnDescriptor> primary)
        {
            var columns = descriptor.Columns.OrderBy(value => value.ColumnOrder).ToArray();
            return string.Join("\u001f", primary.Select(column =>
            {
                var index = Array.FindIndex(columns, value => value.ColumnName == column.ColumnName);
                var value = record.Fields[index].Value;
                return column.DataType == CsvSchemaDataType.Int && int.TryParse(value, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var parsed)
                    ? (parsed < 0 ? "0" : "1") + Math.Abs((long)parsed).ToString("D12", CultureInfo.InvariantCulture)
                    : value;
            }));
        }

        private static CsvSchemaCatalog BuildSchemas()
        {
            var sourceRow = 1;
            var rows = Descriptors.SelectMany(table => table.Columns.OrderBy(column => column.ColumnOrder)
                .Select(column => new CsvSchemaDictionaryRow(table.FileName,
                    column.ColumnOrder.ToString(CultureInfo.InvariantCulture), column.ColumnName,
                    CsvSchemaDataTypes.ToToken(column.DataType), column.IsRequired ? "1" : "0",
                    column.PrimaryKeyOrder.HasValue ? column.PrimaryKeyOrder.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                    column.DefaultValue, string.Join("|", column.AllowedValues), string.Empty, column.Description, sourceRow++))).ToArray();
            var build = new CsvSchemaCatalogBuilder().Build(rows);
            if (!build.Success || build.Catalog.FileCount != 10)
                throw new InvalidOperationException("Approved Activity/Event V2 schemas could not be materialized.");
            return build.Catalog;
        }

        private static ActivityEventCsvImportResult Failed(ICollection<ActivityEventCsvImportError> errors)
        {
            if (!errors.Any(value => value.Code == ActivityEventCsvImportErrorCode.AtomicPublishRejected))
                errors.Add(Error(ActivityEventCsvImportErrorCode.AtomicPublishRejected, string.Empty, 0, 0,
                    "catalog", "Import errors rejected both Activity and Event catalog publication."));
            return new ActivityEventCsvImportResult(null, null, string.Empty, errors);
        }

        private static ActivityEventCsvImportError Error(ActivityEventCsvImportErrorCode code,
            string filePath, int record, int field, string column, string detail) =>
            new ActivityEventCsvImportError(code, filePath, record, field, column, detail);

        private static string ProjectPath(string projectRelativePath)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var normalizedRoot = projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var fullPath = Path.GetFullPath(Path.Combine(projectRoot, projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Activity/Event CSV path escaped the project root.");
            return fullPath;
        }

        private static string Sha256(string value)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(new UTF8Encoding(false).GetBytes(value ?? string.Empty))
                    .Select(item => item.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }
}
