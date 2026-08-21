using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvHeaderFieldValidationResult
    {
        private readonly ReadOnlyCollection<CsvValidatedRecord> records;
        private readonly ReadOnlyCollection<CsvHeaderFieldError> errors;

        internal CsvHeaderFieldValidationResult(
            IEnumerable<CsvValidatedRecord> records,
            IEnumerable<CsvHeaderFieldError> errors)
        {
            this.records = new ReadOnlyCollection<CsvValidatedRecord>(
                new List<CsvValidatedRecord>(
                    records ?? throw new ArgumentNullException(nameof(records))));
            this.errors = new ReadOnlyCollection<CsvHeaderFieldError>(
                new List<CsvHeaderFieldError>(
                    errors ?? throw new ArgumentNullException(nameof(errors))));

            if (this.errors.Count > 0 && this.records.Count > 0)
            {
                throw new ArgumentException(
                    "A failed CSV validation result cannot publish validated records.");
            }
        }

        public bool Success => errors.Count == 0;

        public IReadOnlyList<CsvValidatedRecord> Records => records;

        public IReadOnlyList<CsvHeaderFieldError> Errors => errors;
    }
}
