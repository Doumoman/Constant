using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.Tooling
{
    public static class GeneratedCsvNavigationPreconditions
    {
        public const string TaskId =
            "MAP20_04_IMPLEMENT_CSV_NAVIGATION_AND_VALIDATION_JUMP";
        public const string Map2004HandoffDigest =
            "863018695423be601328975e69356f2ece76ec4db57b024db9406d7131a43285";
    }

    public enum GeneratedCsvSourceKind
    {
        AuthoringCsv = 0,
        GeneratedCsv = 1,
        GeneratedJson = 2,
        InMemoryOnly = 3,
        MissingData = 4,
    }

    /// <summary>Exact immutable source coordinates. Unavailable data uses zero coordinates.</summary>
    public sealed class GeneratedCsvSourceLocation
    {
        public const string MissingDigest = "NONE";

        public GeneratedCsvSourceLocation(GeneratedCsvSourceKind sourceKind, string csvFilePath,
            string csvFileDigest, int rowNumber1Based, int columnNumber1Based,
            string columnName, string fieldName, string recordId, string recordKind,
            string lineDigest, bool sourceAvailable, string missingReason)
        {
            SourceKind = sourceKind;
            CsvFilePath = NormalizePath(csvFilePath);
            CsvFileDigest = Clean(csvFileDigest);
            RowNumber1Based = rowNumber1Based;
            ColumnNumber1Based = columnNumber1Based;
            ColumnName = Clean(columnName);
            FieldName = Clean(fieldName);
            RecordId = Clean(recordId);
            RecordKind = Require(recordKind, nameof(recordKind));
            LineDigest = Clean(lineDigest);
            SourceAvailable = sourceAvailable;
            MissingReason = Clean(missingReason);
            Validate();
        }

        public GeneratedCsvSourceKind SourceKind { get; }
        public string CsvFilePath { get; }
        public string CsvFileDigest { get; }
        public int RowNumber1Based { get; }
        public int ColumnNumber1Based { get; }
        public string ColumnName { get; }
        public string FieldName { get; }
        public string RecordId { get; }
        public string RecordKind { get; }
        public string LineDigest { get; }
        public bool SourceAvailable { get; }
        public string MissingReason { get; }

        public string CopyAddress => SourceAvailable
            ? CsvFilePath + ":" + RowNumber1Based.ToString(CultureInfo.InvariantCulture) + ":" +
              ColumnNumber1Based.ToString(CultureInfo.InvariantCulture)
            : "MissingData: " + MissingReason;

        internal string CanonicalLine => GeneratedInspectionCanonical.Join(
            SourceKind.ToString(), CsvFilePath, CsvFileDigest,
            RowNumber1Based.ToString(CultureInfo.InvariantCulture),
            ColumnNumber1Based.ToString(CultureInfo.InvariantCulture), ColumnName, FieldName,
            RecordId, RecordKind, LineDigest, SourceAvailable ? "1" : "0", MissingReason);

        public static GeneratedCsvSourceLocation Missing(string recordId, string recordKind,
            string reason) => new GeneratedCsvSourceLocation(GeneratedCsvSourceKind.MissingData,
            string.Empty, MissingDigest, 0, 0, string.Empty, string.Empty, recordId, recordKind,
            MissingDigest, false, reason);

        private void Validate()
        {
            if (SourceAvailable)
            {
                if (SourceKind == GeneratedCsvSourceKind.MissingData)
                    throw new ArgumentException("An available source cannot use MissingData kind.");
                Require(CsvFilePath, nameof(CsvFilePath));
                if (!BakingCanonicalDigest.IsLowerHexSha256(CsvFileDigest))
                    throw new ArgumentException("CSV file digest must be lower-hex SHA-256.",
                        nameof(CsvFileDigest));
                if (!BakingCanonicalDigest.IsLowerHexSha256(LineDigest))
                    throw new ArgumentException("Line digest must be lower-hex SHA-256.",
                        nameof(LineDigest));
                if (RowNumber1Based < 1) throw new ArgumentOutOfRangeException(
                    nameof(RowNumber1Based));
                if (ColumnNumber1Based < 1) throw new ArgumentOutOfRangeException(
                    nameof(ColumnNumber1Based));
                Require(ColumnName, nameof(ColumnName));
                Require(FieldName, nameof(FieldName));
                Require(RecordId, nameof(RecordId));
                if (MissingReason.Length != 0)
                    throw new ArgumentException("An available source cannot have a missing reason.",
                        nameof(MissingReason));
                return;
            }

            if (RowNumber1Based != 0 || ColumnNumber1Based != 0)
                throw new ArgumentException("Unavailable source coordinates must remain zero.");
            if (!string.Equals(CsvFileDigest, MissingDigest, StringComparison.Ordinal) ||
                !string.Equals(LineDigest, MissingDigest, StringComparison.Ordinal))
                throw new ArgumentException("Unavailable source digests must be NONE.");
            if (MissingReason.Length == 0)
                throw new ArgumentException("Unavailable source requires a missing reason.",
                    nameof(MissingReason));
        }

        private static string NormalizePath(string value) => Clean(value).Replace('\\', '/');

        private static string Require(string value, string parameterName)
        {
            var result = Clean(value);
            if (result.Length == 0) throw new ArgumentException("Value is required.", parameterName);
            return result;
        }

        private static string Clean(string value) => value == null ? string.Empty : value.Trim();
    }

    /// <summary>
    /// Read-only lookup from validation error id to an exact source and inspector selection target.
    /// </summary>
    public sealed class GeneratedCsvNavigationIndex
    {
        public const string SchemaVersion = "map20_04.csv_navigation_index.v1";

        private readonly ReadOnlyCollection<string> sourceKindTokens;
        private readonly ReadOnlyCollection<GeneratedCsvSourceLocation> sourceRecords;
        private readonly ReadOnlyCollection<GeneratedValidationJumpTarget> jumpTargetRecords;
        private readonly ReadOnlyCollection<GeneratedCsvSourceLocation> missingDataRecords;
        private readonly Dictionary<string, GeneratedValidationJumpTarget> byValidationErrorId;

        public GeneratedCsvNavigationIndex(string sourceMap2003DetailSnapshotDigest,
            string sourceMap2003CombinedDetailDigest, string map2004HandoffDigest,
            IEnumerable<GeneratedCsvSourceLocation> sources,
            IEnumerable<GeneratedValidationJumpTarget> targets, string createdUtc)
        {
            SourceMap2003DetailSnapshotDigest = RequireDigest(
                sourceMap2003DetailSnapshotDigest, nameof(sourceMap2003DetailSnapshotDigest));
            SourceMap2003CombinedDetailDigest = RequireDigest(
                sourceMap2003CombinedDetailDigest, nameof(sourceMap2003CombinedDetailDigest));
            Map2004HandoffDigest = RequireDigest(map2004HandoffDigest,
                nameof(map2004HandoffDigest));
            if (!string.Equals(Map2004HandoffDigest,
                GeneratedCsvNavigationPreconditions.Map2004HandoffDigest,
                StringComparison.Ordinal))
                throw new ArgumentException("MAP20_04 handoff digest mismatch.",
                    nameof(map2004HandoffDigest));

            sourceKindTokens = new ReadOnlyCollection<string>(Enum
                .GetValues(typeof(GeneratedCsvSourceKind)).Cast<GeneratedCsvSourceKind>()
                .OrderBy(value => (int)value).Select(value => value.ToString()).ToArray());
            var orderedSources = (sources ?? throw new ArgumentNullException(nameof(sources)))
                .Where(value => value != null).OrderBy(value => value.CanonicalLine,
                    StringComparer.Ordinal).ToArray();
            var orderedTargets = (targets ?? throw new ArgumentNullException(nameof(targets)))
                .Where(value => value != null)
                .OrderBy(value => value.ValidationErrorId, StringComparer.Ordinal)
                .ThenBy(value => value.CanonicalDigest, StringComparer.Ordinal).ToArray();
            if (orderedSources.Length == 0)
                throw new ArgumentException("At least one source record is required.", nameof(sources));
            if (orderedTargets.Length == 0)
                throw new ArgumentException("At least one jump target is required.", nameof(targets));
            if (orderedTargets.GroupBy(value => value.ValidationErrorId, StringComparer.Ordinal)
                .Any(group => group.Count() != 1))
                throw new ArgumentException("Validation error ids must be unique.", nameof(targets));
            if (orderedTargets.Any(target => !orderedSources.Any(source =>
                ReferenceEquals(source, target.SourceLocation) ||
                string.Equals(source.CanonicalLine, target.SourceLocation.CanonicalLine,
                    StringComparison.Ordinal))))
                throw new ArgumentException("Every jump target source must be indexed.", nameof(targets));

            sourceRecords = new ReadOnlyCollection<GeneratedCsvSourceLocation>(orderedSources);
            jumpTargetRecords = new ReadOnlyCollection<GeneratedValidationJumpTarget>(orderedTargets);
            missingDataRecords = new ReadOnlyCollection<GeneratedCsvSourceLocation>(
                orderedSources.Where(value => !value.SourceAvailable).ToArray());
            byValidationErrorId = orderedTargets.ToDictionary(
                value => value.ValidationErrorId, StringComparer.Ordinal);
            CreatedUtc = createdUtc ?? string.Empty;
            CanonicalDigest = ComputeCanonicalDigest();
        }

        public string SourceMap2003DetailSnapshotDigest { get; }
        public string SourceMap2003CombinedDetailDigest { get; }
        public string Map2004HandoffDigest { get; }
        public IReadOnlyList<string> SourceKindTokens => sourceKindTokens;
        public IReadOnlyList<GeneratedCsvSourceLocation> SourceRecords => sourceRecords;
        public IReadOnlyList<GeneratedValidationJumpTarget> JumpTargetRecords => jumpTargetRecords;
        public IReadOnlyList<GeneratedCsvSourceLocation> MissingDataRecords => missingDataRecords;
        public string CreatedUtc { get; }
        public bool CreatedUtcExcludedFromCanonicalDigest => true;
        public string CanonicalDigest { get; }

        public GeneratedValidationJumpTarget FindByValidationErrorId(string validationErrorId)
        {
            if (validationErrorId == null) throw new ArgumentNullException(nameof(validationErrorId));
            return byValidationErrorId.TryGetValue(validationErrorId, out var target)
                ? target
                : null;
        }

        public string Serialize() => GeneratedInspectionCanonical.ToJson(
            new GeneratedCsvNavigationIndexDocument
            {
                schema_version = SchemaVersion,
                task_id = GeneratedCsvNavigationPreconditions.TaskId,
                source_MAP20_03_detail_snapshot_digest = SourceMap2003DetailSnapshotDigest,
                source_MAP20_03_combined_detail_digest = SourceMap2003CombinedDetailDigest,
                MAP20_04_handoff_digest = Map2004HandoffDigest,
                source_records = sourceRecords.Select(
                    GeneratedCsvSourceLocationDocument.From).ToArray(),
                jump_target_records = jumpTargetRecords.Select(value => value.ToDocument()).ToArray(),
                missing_data_records = missingDataRecords.Select(
                    GeneratedCsvSourceLocationDocument.From).ToArray(),
                canonical_digest = CanonicalDigest,
                created_utc = CreatedUtc,
                created_utc_excluded_from_canonical_digest = true,
            });

        private string ComputeCanonicalDigest()
        {
            var lines = new List<string>
            {
                SchemaVersion, GeneratedCsvNavigationPreconditions.TaskId,
                SourceMap2003DetailSnapshotDigest, SourceMap2003CombinedDetailDigest,
                Map2004HandoffDigest, string.Join("|", sourceKindTokens),
                "created_utc_excluded=true",
            };
            lines.AddRange(sourceRecords.Select(value => "source=" + value.CanonicalLine));
            lines.AddRange(jumpTargetRecords.Select(value => "target=" + value.CanonicalLine));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        private static string RequireDigest(string value, string parameterName)
        {
            if (!BakingCanonicalDigest.IsLowerHexSha256(value))
                throw new ArgumentException("Value must be lower-hex SHA-256.", parameterName);
            return value;
        }
    }

    [Serializable]
    internal sealed class GeneratedCsvSourceLocationDocument
    {
        public string source_kind;
        public string csv_file_path;
        public string csv_file_digest;
        public int row_number_1_based;
        public int column_number_1_based;
        public string column_name;
        public string field_name;
        public string record_id;
        public string record_kind;
        public string line_digest;
        public bool source_available;
        public string missing_reason;

        public static GeneratedCsvSourceLocationDocument From(GeneratedCsvSourceLocation value) =>
            new GeneratedCsvSourceLocationDocument
            {
                source_kind = value.SourceKind.ToString(),
                csv_file_path = value.CsvFilePath,
                csv_file_digest = value.CsvFileDigest,
                row_number_1_based = value.RowNumber1Based,
                column_number_1_based = value.ColumnNumber1Based,
                column_name = value.ColumnName,
                field_name = value.FieldName,
                record_id = value.RecordId,
                record_kind = value.RecordKind,
                line_digest = value.LineDigest,
                source_available = value.SourceAvailable,
                missing_reason = value.MissingReason,
            };
    }

    [Serializable]
    internal sealed class GeneratedCsvNavigationIndexDocument
    {
        public string schema_version;
        public string task_id;
        public string source_MAP20_03_detail_snapshot_digest;
        public string source_MAP20_03_combined_detail_digest;
        public string MAP20_04_handoff_digest;
        public GeneratedCsvSourceLocationDocument[] source_records;
        public GeneratedValidationJumpTargetDocument[] jump_target_records;
        public GeneratedCsvSourceLocationDocument[] missing_data_records;
        public string canonical_digest;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
    }
}
