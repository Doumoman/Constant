using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.TerrainClusters.Authoring
{
    public sealed class TerrainClusterAuthoringRow
    {
        private readonly ReadOnlyDictionary<string, string> fields;

        public TerrainClusterAuthoringRow(
            string tablePath,
            int recordNumber,
            IEnumerable<KeyValuePair<string, string>> sourceFields)
        {
            TablePath = (tablePath ?? string.Empty).Replace('\\', '/');
            RecordNumber = recordNumber;
            var copy = (sourceFields ?? throw new ArgumentNullException(nameof(sourceFields)))
                .ToDictionary(value => value.Key, value => value.Value ?? string.Empty, StringComparer.Ordinal);
            fields = new ReadOnlyDictionary<string, string>(copy);
        }

        public string TablePath { get; }
        public int RecordNumber { get; }
        public IReadOnlyDictionary<string, string> Fields => fields;

        public string Get(string columnName)
        {
            string value;
            return fields.TryGetValue(columnName ?? string.Empty, out value) ? value : string.Empty;
        }
    }

    public enum TerrainClusterAuthoringErrorCode
    {
        MissingTable = 1,
        UnexpectedTable = 2,
        InvalidField = 3,
        InvalidToken = 4,
        DuplicatePrimaryKey = 5,
        MissingForeignKey = 6,
        CrossOwnerReference = 7,
        InvalidFootprint = 8,
        InvalidVariant = 9,
        InvalidRoleLink = 10,
        InvalidPort = 11,
        InvalidNodeOrEdge = 12,
        InvalidEnvelope = 13,
        InvalidHighRoute = 14,
        InvalidContract = 15,
        InvalidCatalog = 16,
        AtomicPublishRejected = 17,
    }

    public sealed class TerrainClusterAuthoringError :
        IEquatable<TerrainClusterAuthoringError>,
        IComparable<TerrainClusterAuthoringError>
    {
        public TerrainClusterAuthoringError(
            TerrainClusterAuthoringErrorCode code,
            string tablePath,
            int recordNumber,
            string columnName,
            string detail)
        {
            Code = code;
            TablePath = (tablePath ?? string.Empty).Replace('\\', '/');
            RecordNumber = recordNumber;
            ColumnName = columnName ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public TerrainClusterAuthoringErrorCode Code { get; }
        public string TablePath { get; }
        public int RecordNumber { get; }
        public string ColumnName { get; }
        public string Detail { get; }

        public int CompareTo(TerrainClusterAuthoringError other)
        {
            if (other == null) return -1;
            var comparison = string.Compare(TablePath, other.TablePath, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = RecordNumber.CompareTo(other.RecordNumber);
            if (comparison != 0) return comparison;
            comparison = string.Compare(ColumnName, other.ColumnName, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = Code.CompareTo(other.Code);
            return comparison != 0
                ? comparison
                : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(TerrainClusterAuthoringError other)
        {
            return other != null && CompareTo(other) == 0;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TerrainClusterAuthoringError);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(ToString());
        }

        public override string ToString()
        {
            return Code + "|" + TablePath + "|record=" + RecordNumber +
                   "|column=" + ColumnName + "|" + Detail;
        }
    }

    public sealed class TerrainClusterAuthoringBuildResult
    {
        private readonly ReadOnlyCollection<TerrainClusterAuthoringError> errors;

        internal TerrainClusterAuthoringBuildResult(
            TerrainClusterAuthoringCatalog catalog,
            IEnumerable<TerrainClusterAuthoringError> sourceErrors)
        {
            var ordered = (sourceErrors ?? throw new ArgumentNullException(nameof(sourceErrors)))
                .Where(value => value != null)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            errors = new ReadOnlyCollection<TerrainClusterAuthoringError>(ordered);
            if (ordered.Length > 0 && catalog != null)
                throw new ArgumentException("A failed build cannot publish a TerrainCluster catalog.");
            Catalog = ordered.Length == 0 ? catalog : null;
        }

        public bool Success => errors.Count == 0 && Catalog != null;
        public bool Published => Catalog != null;
        public TerrainClusterAuthoringCatalog Catalog { get; }
        public IReadOnlyList<TerrainClusterAuthoringError> Errors => errors;
        public string StableDigest => Catalog == null ? string.Empty : Catalog.StableDigest;
    }
}
