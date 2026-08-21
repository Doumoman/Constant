using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvPrimaryKeyOccurrence
    {
        private readonly ReadOnlyCollection<CsvValidatedField> primaryKeyFields;

        internal CsvPrimaryKeyOccurrence(
            string sourceName,
            string schemaFileName,
            CsvPrimaryKey key,
            CsvValidatedRecord sourceValidatedRecord,
            IEnumerable<CsvValidatedField> primaryKeyFields)
        {
            SourceName = sourceName ?? throw new ArgumentNullException(nameof(sourceName));
            SchemaFileName = schemaFileName ??
                             throw new ArgumentNullException(nameof(schemaFileName));
            Key = key ?? throw new ArgumentNullException(nameof(key));
            SourceValidatedRecord = sourceValidatedRecord ??
                                    throw new ArgumentNullException(nameof(sourceValidatedRecord));
            SourceRecord = sourceValidatedRecord.SourceRecord;

            var copiedFields = new List<CsvValidatedField>(
                primaryKeyFields ?? throw new ArgumentNullException(nameof(primaryKeyFields)));
            if (copiedFields.Count == 0)
            {
                throw new ArgumentException(
                    "A primary-key occurrence must contain at least one primary-key field.",
                    nameof(primaryKeyFields));
            }

            for (var index = 0; index < copiedFields.Count; index++)
            {
                if (copiedFields[index] == null)
                {
                    throw new ArgumentException(
                        "A primary-key occurrence field cannot be null.",
                        nameof(primaryKeyFields));
                }
            }

            this.primaryKeyFields = new ReadOnlyCollection<CsvValidatedField>(copiedFields);
            Location = copiedFields[0].SourceField.StartLocation;
        }

        public string SourceName { get; }

        public string SchemaFileName { get; }

        public CsvPrimaryKey Key { get; }

        public int RecordNumber => Location.RecordNumber;

        public int PhysicalLine => Location.PhysicalLine;

        public int PhysicalColumn => Location.PhysicalColumn;

        public int CharOffset => Location.CharOffset;

        public CsvSourceLocation Location { get; }

        public CsvRecord SourceRecord { get; }

        public CsvValidatedRecord SourceValidatedRecord { get; }

        public IReadOnlyList<CsvValidatedField> PrimaryKeyFields => primaryKeyFields;

        internal static int CompareSourceOrder(
            CsvPrimaryKeyOccurrence left,
            CsvPrimaryKeyOccurrence right)
        {
            var comparison = left.RecordNumber.CompareTo(right.RecordNumber);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = left.PhysicalLine.CompareTo(right.PhysicalLine);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = left.PhysicalColumn.CompareTo(right.PhysicalColumn);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = left.CharOffset.CompareTo(right.CharOffset);
            return comparison != 0
                ? comparison
                : StringComparer.Ordinal.Compare(left.SourceName, right.SourceName);
        }
    }
}
