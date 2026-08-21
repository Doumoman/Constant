using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvValidatedRecord
    {
        private readonly ReadOnlyCollection<CsvValidatedField> fields;

        internal CsvValidatedRecord(
            int recordNumber,
            IEnumerable<CsvValidatedField> fields,
            CsvRecord sourceRecord)
        {
            if (recordNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(recordNumber));
            }

            RecordNumber = recordNumber;
            this.fields = new ReadOnlyCollection<CsvValidatedField>(
                new List<CsvValidatedField>(
                    fields ?? throw new ArgumentNullException(nameof(fields))));
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
        }

        public int RecordNumber { get; }

        public IReadOnlyList<CsvValidatedField> Fields => fields;

        public CsvRecord SourceRecord { get; }
    }
}
