using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvPrimaryKeyIndexBuildResult
    {
        private readonly ReadOnlyCollection<CsvDuplicatePrimaryKey> duplicates;

        internal CsvPrimaryKeyIndexBuildResult(
            CsvPrimaryKeyIndex index,
            IEnumerable<CsvDuplicatePrimaryKey> duplicates)
        {
            Index = index;
            this.duplicates = new ReadOnlyCollection<CsvDuplicatePrimaryKey>(
                new List<CsvDuplicatePrimaryKey>(
                    duplicates ?? throw new ArgumentNullException(nameof(duplicates))));

            if (Index != null && this.duplicates.Count > 0)
            {
                throw new ArgumentException(
                    "A primary-key build result cannot publish an index and duplicates together.");
            }
        }

        public bool Success => Index != null && duplicates.Count == 0;

        public CsvPrimaryKeyIndex Index { get; }

        public IReadOnlyList<CsvDuplicatePrimaryKey> Duplicates => duplicates;
    }
}
