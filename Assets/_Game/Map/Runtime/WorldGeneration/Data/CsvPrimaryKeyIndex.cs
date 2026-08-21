using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvPrimaryKeyIndex
    {
        private readonly ReadOnlyCollection<CsvPrimaryKeyOccurrence> entries;
        private readonly Dictionary<CsvPrimaryKey, CsvPrimaryKeyOccurrence> entriesByKey;

        internal CsvPrimaryKeyIndex(
            string schemaFileName,
            IEnumerable<CsvPrimaryKeyOccurrence> sourceEntries)
        {
            SchemaFileName = schemaFileName ??
                             throw new ArgumentNullException(nameof(schemaFileName));

            var copiedEntries = new List<CsvPrimaryKeyOccurrence>(
                sourceEntries ?? throw new ArgumentNullException(nameof(sourceEntries)));
            copiedEntries.Sort((left, right) => left.Key.CompareTo(right.Key));
            entriesByKey = new Dictionary<CsvPrimaryKey, CsvPrimaryKeyOccurrence>();
            foreach (var entry in copiedEntries)
            {
                if (entry == null)
                {
                    throw new ArgumentException(
                        "A primary-key index entry cannot be null.",
                        nameof(sourceEntries));
                }

                if (!entriesByKey.TryAdd(entry.Key, entry))
                {
                    throw new ArgumentException(
                        "A primary-key index cannot contain duplicate keys.",
                        nameof(sourceEntries));
                }
            }

            entries = new ReadOnlyCollection<CsvPrimaryKeyOccurrence>(copiedEntries);
        }

        public string SchemaFileName { get; }

        public int Count => entries.Count;

        public IReadOnlyList<CsvPrimaryKeyOccurrence> Entries => entries;

        public bool TryGet(CsvPrimaryKey key, out CsvPrimaryKeyOccurrence occurrence)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return entriesByKey.TryGetValue(key, out occurrence);
        }
    }
}
