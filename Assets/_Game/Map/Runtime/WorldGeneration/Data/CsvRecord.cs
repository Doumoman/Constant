using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvRecord
    {
        private readonly ReadOnlyCollection<CsvField> fields;

        internal CsvRecord(
            int recordNumber,
            IEnumerable<CsvField> fields,
            CsvSourceLocation startLocation,
            CsvSourceLocation endLocationExclusive)
        {
            RecordNumber = recordNumber;
            this.fields = new ReadOnlyCollection<CsvField>(
                new List<CsvField>(fields ?? throw new ArgumentNullException(nameof(fields))));
            StartLocation = startLocation;
            EndLocationExclusive = endLocationExclusive;
        }

        public int RecordNumber { get; }

        public IReadOnlyList<CsvField> Fields => fields;

        public CsvSourceLocation StartLocation { get; }

        public CsvSourceLocation EndLocationExclusive { get; }
    }
}
