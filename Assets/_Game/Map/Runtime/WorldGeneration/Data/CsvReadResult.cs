using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvReadResult
    {
        private readonly ReadOnlyCollection<CsvRecord> records;
        private readonly ReadOnlyCollection<CsvReadError> errors;

        internal CsvReadResult(
            bool hadUtf8Bom,
            IEnumerable<CsvRecord> records,
            IEnumerable<CsvReadError> errors)
        {
            HadUtf8Bom = hadUtf8Bom;
            this.records = new ReadOnlyCollection<CsvRecord>(
                new List<CsvRecord>(records ?? throw new ArgumentNullException(nameof(records))));
            this.errors = new ReadOnlyCollection<CsvReadError>(
                new List<CsvReadError>(errors ?? throw new ArgumentNullException(nameof(errors))));
        }

        public bool Success => errors.Count == 0;

        public bool HadUtf8Bom { get; }

        public IReadOnlyList<CsvRecord> Records => records;

        public IReadOnlyList<CsvReadError> Errors => errors;
    }
}
