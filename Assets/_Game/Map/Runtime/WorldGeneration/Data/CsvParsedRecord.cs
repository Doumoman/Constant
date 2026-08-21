using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvParsedRecord
    {
        private readonly ReadOnlyCollection<CsvParsedField> fields;

        internal CsvParsedRecord(
            CsvValidatedRecord validatedRecord,
            IEnumerable<CsvParsedField> sourceFields)
        {
            ValidatedRecord = validatedRecord ??
                              throw new ArgumentNullException(nameof(validatedRecord));
            RecordNumber = validatedRecord.RecordNumber;
            SourceRecord = validatedRecord.SourceRecord;
            fields = new ReadOnlyCollection<CsvParsedField>(
                new List<CsvParsedField>(
                    sourceFields ?? throw new ArgumentNullException(nameof(sourceFields))));
        }

        public int RecordNumber { get; }

        public IReadOnlyList<CsvParsedField> Fields => fields;

        public CsvValidatedRecord ValidatedRecord { get; }

        public CsvRecord SourceRecord { get; }
    }
}
