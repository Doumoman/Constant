using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class ForeignKeyRecordIndex
    {
        private readonly ReadOnlyCollection<ForeignKeyRecordIdentity> records;
        private readonly Dictionary<LookupKey, ForeignKeyRecordIdentity> lookup;

        internal ForeignKeyRecordIndex(
            IEnumerable<ForeignKeyRecordIdentity> sourceRecords,
            IEnumerable<LookupEntry> sourceLookupEntries)
        {
            var copiedRecords = new List<ForeignKeyRecordIdentity>(
                sourceRecords ?? throw new ArgumentNullException(nameof(sourceRecords)));
            copiedRecords.Sort(CompareRecords);
            records = new ReadOnlyCollection<ForeignKeyRecordIdentity>(copiedRecords);

            lookup = new Dictionary<LookupKey, ForeignKeyRecordIdentity>();
            foreach (var entry in sourceLookupEntries ??
                     throw new ArgumentNullException(nameof(sourceLookupEntries)))
            {
                if (entry == null || entry.Identity == null)
                {
                    throw new ArgumentException("A foreign-key lookup entry cannot be null.");
                }

                var key = new LookupKey(entry.FileName, entry.ColumnName, entry.Value);
                if (!lookup.TryAdd(key, entry.Identity))
                {
                    throw new ArgumentException(
                        "A foreign-key record index cannot contain duplicate lookup keys.");
                }
            }
        }

        public int Count => records.Count;

        public int LookupCount => lookup.Count;

        public IReadOnlyList<ForeignKeyRecordIdentity> Records => records;

        public bool TryGet(
            string fileName,
            string columnName,
            string value,
            out ForeignKeyRecordIdentity identity)
        {
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));
            if (columnName == null) throw new ArgumentNullException(nameof(columnName));
            if (value == null) throw new ArgumentNullException(nameof(value));
            return lookup.TryGetValue(new LookupKey(fileName, columnName, value), out identity);
        }

        private static int CompareRecords(
            ForeignKeyRecordIdentity left,
            ForeignKeyRecordIdentity right)
        {
            var comparison = StringComparer.Ordinal.Compare(left.FileName, right.FileName);
            return comparison != 0
                ? comparison
                : left.RecordNumber.CompareTo(right.RecordNumber);
        }

        internal sealed class LookupEntry
        {
            public LookupEntry(
                string fileName,
                string columnName,
                string value,
                ForeignKeyRecordIdentity identity)
            {
                FileName = fileName;
                ColumnName = columnName;
                Value = value;
                Identity = identity;
            }

            public string FileName { get; }
            public string ColumnName { get; }
            public string Value { get; }
            public ForeignKeyRecordIdentity Identity { get; }
        }

        private readonly struct LookupKey : IEquatable<LookupKey>
        {
            private readonly string fileName;
            private readonly string columnName;
            private readonly string value;

            public LookupKey(string fileName, string columnName, string value)
            {
                this.fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
                this.columnName = columnName ??
                                  throw new ArgumentNullException(nameof(columnName));
                this.value = value ?? throw new ArgumentNullException(nameof(value));
            }

            public bool Equals(LookupKey other)
            {
                return string.Equals(fileName, other.fileName, StringComparison.Ordinal) &&
                       string.Equals(columnName, other.columnName, StringComparison.Ordinal) &&
                       string.Equals(value, other.value, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is LookupKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = StringComparer.Ordinal.GetHashCode(fileName);
                    hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(columnName);
                    return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(value);
                }
            }
        }
    }
}
