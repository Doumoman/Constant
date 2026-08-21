using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using StarNight.Map.WorldGeneration.Data;
using UnityEngine;

namespace StarNight.MapAuthoring.WorldGeneration.Import
{
    public sealed class CsvSchemaDictionaryImportResult
    {
        private readonly ReadOnlyCollection<CsvSchemaDictionaryRow> rows;
        private readonly ReadOnlyCollection<string> errors;

        internal CsvSchemaDictionaryImportResult(
            IEnumerable<CsvSchemaDictionaryRow> rows,
            IEnumerable<string> errors)
        {
            this.rows = new ReadOnlyCollection<CsvSchemaDictionaryRow>(
                new List<CsvSchemaDictionaryRow>(rows ?? Array.Empty<CsvSchemaDictionaryRow>()));
            this.errors = new ReadOnlyCollection<string>(
                new List<string>(errors ?? Array.Empty<string>()));
        }

        public bool Success => errors.Count == 0;

        public IReadOnlyList<CsvSchemaDictionaryRow> Rows => rows;

        public IReadOnlyList<string> Errors => errors;
    }

    public sealed class CsvSchemaDictionaryImporter
    {
        public const string DictionaryProjectRelativePath =
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/CSV_DATA_DICTIONARY.csv";

        public const string ExpectedHeader =
            "file_name,column_order,column_name,data_type,required,primary_key_order," +
            "default_value,allowed_values,foreign_key,description";

        private static readonly string[] ExpectedHeaderFields = ExpectedHeader.Split(',');

        public CsvSchemaDictionaryImportResult Import()
        {
            try
            {
                return ParseBytes(File.ReadAllBytes(GetDictionaryFullPath()));
            }
            catch (Exception exception)
            {
                return Failure("Dictionary read failed: " + exception.Message);
            }
        }

        public CsvSchemaDictionaryImportResult ParseBytes(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            var readResult = new Rfc4180CsvReader().Read(bytes, DictionaryProjectRelativePath);
            if (!readResult.Success)
            {
                return new CsvSchemaDictionaryImportResult(
                    Array.Empty<CsvSchemaDictionaryRow>(),
                    readResult.Errors.Select(error => error.ToString()));
            }

            if (!readResult.HadUtf8Bom)
            {
                return Failure("CSV schema dictionary must contain a UTF-8 BOM.");
            }

            if (readResult.Records.Count == 0 || !HasExpectedHeader(readResult.Records[0]))
            {
                return Failure("CSV schema dictionary header must match the exact 10-column contract.");
            }

            var errors = new List<string>();
            var rows = new List<CsvSchemaDictionaryRow>();
            for (var recordIndex = 1; recordIndex < readResult.Records.Count; recordIndex++)
            {
                var record = readResult.Records[recordIndex];
                if (record.Fields.Count != ExpectedHeaderFields.Length)
                {
                    errors.Add(
                        "Record " + record.RecordNumber + " at physical line " +
                        record.StartLocation.PhysicalLine +
                        ": expected exactly 10 fields but found " + record.Fields.Count + ".");
                    continue;
                }

                rows.Add(new CsvSchemaDictionaryRow(
                    record.Fields[0].Value,
                    record.Fields[1].Value,
                    record.Fields[2].Value,
                    record.Fields[3].Value,
                    record.Fields[4].Value,
                    record.Fields[5].Value,
                    record.Fields[6].Value,
                    record.Fields[7].Value,
                    record.Fields[8].Value,
                    record.Fields[9].Value,
                    record.StartLocation.PhysicalLine));
            }

            if (errors.Count > 0)
            {
                return new CsvSchemaDictionaryImportResult(
                    Array.Empty<CsvSchemaDictionaryRow>(),
                    errors.OrderBy(error => error, StringComparer.Ordinal));
            }

            return new CsvSchemaDictionaryImportResult(rows, Array.Empty<string>());
        }

        private static bool HasExpectedHeader(CsvRecord record)
        {
            if (record.Fields.Count != ExpectedHeaderFields.Length)
            {
                return false;
            }

            for (var index = 0; index < ExpectedHeaderFields.Length; index++)
            {
                if (record.Fields[index].WasQuoted ||
                    !record.Fields[index].Value.Equals(
                        ExpectedHeaderFields[index],
                        StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static CsvSchemaDictionaryImportResult Failure(string error)
        {
            return new CsvSchemaDictionaryImportResult(
                Array.Empty<CsvSchemaDictionaryRow>(),
                new[] { error });
        }

        private static string GetDictionaryFullPath()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var normalizedRoot = projectRoot.TrimEnd(
                                     Path.DirectorySeparatorChar,
                                     Path.AltDirectorySeparatorChar) +
                                 Path.DirectorySeparatorChar;
            var fullPath = Path.GetFullPath(Path.Combine(
                projectRoot,
                DictionaryProjectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "CSV schema dictionary path escaped the Unity project root.");
            }

            return fullPath;
        }
    }
}
