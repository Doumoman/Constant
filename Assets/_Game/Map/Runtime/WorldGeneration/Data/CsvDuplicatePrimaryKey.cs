using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvDuplicatePrimaryKey
    {
        private readonly ReadOnlyCollection<CsvPrimaryKeyOccurrence> occurrences;

        internal CsvDuplicatePrimaryKey(
            string schemaFileName,
            CsvPrimaryKey key,
            IEnumerable<CsvPrimaryKeyOccurrence> occurrences)
        {
            SchemaFileName = schemaFileName ??
                             throw new ArgumentNullException(nameof(schemaFileName));
            Key = key ?? throw new ArgumentNullException(nameof(key));

            var copiedOccurrences = new List<CsvPrimaryKeyOccurrence>(
                occurrences ?? throw new ArgumentNullException(nameof(occurrences)));
            if (copiedOccurrences.Count < 2)
            {
                throw new ArgumentException(
                    "A duplicate primary-key group must contain at least two occurrences.",
                    nameof(occurrences));
            }

            for (var index = 0; index < copiedOccurrences.Count; index++)
            {
                if (copiedOccurrences[index] == null)
                {
                    throw new ArgumentException(
                        "A duplicate primary-key occurrence cannot be null.",
                        nameof(occurrences));
                }
            }

            this.occurrences = new ReadOnlyCollection<CsvPrimaryKeyOccurrence>(
                copiedOccurrences);
        }

        public string SchemaFileName { get; }

        public CsvPrimaryKey Key { get; }

        public IReadOnlyList<CsvPrimaryKeyOccurrence> Occurrences => occurrences;
    }
}
