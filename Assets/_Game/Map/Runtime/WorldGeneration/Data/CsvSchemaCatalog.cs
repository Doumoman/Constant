using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvSchemaCatalog
    {
        private readonly ReadOnlyCollection<CsvFileSchema> files;
        private readonly Dictionary<string, CsvFileSchema> filesByName;

        internal CsvSchemaCatalog(IEnumerable<CsvFileSchema> sourceFiles)
        {
            var orderedFiles = sourceFiles
                .OrderBy(file => file.FileName, StringComparer.Ordinal)
                .ToList();
            files = new ReadOnlyCollection<CsvFileSchema>(orderedFiles);
            filesByName = orderedFiles.ToDictionary(
                file => file.FileName,
                file => file,
                StringComparer.Ordinal);
            ColumnCount = orderedFiles.Sum(file => file.Columns.Count);
        }

        public IReadOnlyList<CsvFileSchema> Files => files;

        public int FileCount => files.Count;

        public int ColumnCount { get; }

        public bool TryGetFile(string fileName, out CsvFileSchema file)
        {
            return filesByName.TryGetValue(fileName, out file);
        }

        public CsvFileSchema GetFile(string fileName)
        {
            if (!TryGetFile(fileName, out var file))
            {
                throw new KeyNotFoundException("CSV file schema was not found: " + fileName);
            }

            return file;
        }
    }
}
