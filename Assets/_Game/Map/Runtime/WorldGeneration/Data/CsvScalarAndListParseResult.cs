using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvScalarAndListParseResult
    {
        private readonly ReadOnlyCollection<CsvParsedRecord> records;
        private readonly ReadOnlyCollection<CsvValueParseError> errors;

        internal CsvScalarAndListParseResult(
            IEnumerable<CsvParsedRecord> sourceRecords,
            IEnumerable<CsvValueParseError> sourceErrors)
        {
            records = new ReadOnlyCollection<CsvParsedRecord>(
                new List<CsvParsedRecord>(
                    sourceRecords ?? throw new ArgumentNullException(nameof(sourceRecords))));
            errors = new ReadOnlyCollection<CsvValueParseError>(
                new List<CsvValueParseError>(
                    sourceErrors ?? throw new ArgumentNullException(nameof(sourceErrors))));

            if (records.Count > 0 && errors.Count > 0)
            {
                throw new ArgumentException(
                    "A failed scalar/list parse result cannot publish parsed records.");
            }
        }

        public bool Success => errors.Count == 0;

        public IReadOnlyList<CsvParsedRecord> Records => records;

        public IReadOnlyList<CsvValueParseError> Errors => errors;
    }
}
