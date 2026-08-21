using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class ContentVersionHashCalculator
    {
        private const string Magic = "STARNIGHT_STATIC_DATA_CONTENT_V1";
        private static readonly HashSet<string> ExpectedFiles =
            new HashSet<string>(ForeignKeySourceSet.ExpectedFileNames, StringComparer.Ordinal);
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public ContentVersionHashResult Calculate(
            StaticDataRegistry registry,
            ForeignKeySourceSet sourceSet,
            CsvSchemaCatalog catalog)
        {
            var errors = new List<ContentVersionHashError>();
            if (registry == null)
            {
                errors.Add(Error(ContentVersionHashErrorCode.MissingRegistry,
                    "A successful static data registry is required."));
            }

            if (sourceSet == null)
            {
                errors.Add(Error(ContentVersionHashErrorCode.MissingSourceSet,
                    "The exact 49-source foreign-key source set is required."));
            }

            if (catalog == null || sourceSet == null ||
                !ReferenceEquals(sourceSet.SchemaCatalog, catalog))
            {
                errors.Add(Error(ContentVersionHashErrorCode.CatalogMismatch,
                    "The supplied catalog must be the exact source-set catalog instance."));
            }

            var sourcesByFile = sourceSet == null
                ? new Dictionary<string, List<ForeignKeySourceSet.Source>>(StringComparer.Ordinal)
                : CollectSources(sourceSet, errors);
            if (sourceSet != null)
            {
                ValidateInventory(sourcesByFile, catalog, errors);
            }

            if (registry != null && sourceSet != null)
            {
                ValidateRecordIdentities(registry, sourcesByFile, errors);
            }

            errors.Sort(ContentVersionHashError.Compare);
            if (errors.Count > 0)
            {
                return new ContentVersionHashResult(null, errors);
            }

            var files = BuildCanonicalFiles(sourcesByFile, errors);
            errors.Sort(ContentVersionHashError.Compare);
            if (errors.Count > 0)
            {
                return new ContentVersionHashResult(null, errors);
            }

            try
            {
                byte[] payload;
                using (var writer = new ContentHashCanonicalWriter())
                {
                    writer.WriteString(Magic);
                    writer.WriteCount(files.Count);
                    foreach (var file in files)
                    {
                        WriteFile(writer, file);
                    }

                    payload = writer.ToArray();
                }

                byte[] digest;
                using (var sha256 = SHA256.Create())
                {
                    digest = sha256.ComputeHash(payload);
                }

                return new ContentVersionHashResult(
                    new ContentVersionHash(digest),
                    Array.Empty<ContentVersionHashError>());
            }
            catch (EncoderFallbackException exception)
            {
                errors.Add(Error(ContentVersionHashErrorCode.UnsupportedValue,
                    "Canonical UTF-8 encoding failed: " + exception.Message));
                return new ContentVersionHashResult(null, errors);
            }
        }

        private static Dictionary<string, List<ForeignKeySourceSet.Source>> CollectSources(
            ForeignKeySourceSet sourceSet,
            ICollection<ContentVersionHashError> errors)
        {
            var result = new Dictionary<string, List<ForeignKeySourceSet.Source>>(StringComparer.Ordinal);
            foreach (var source in sourceSet.Sources)
            {
                var fileName = source?.Schema?.FileName ?? string.Empty;
                if (source == null || source.Schema == null || !ExpectedFiles.Contains(fileName))
                {
                    errors.Add(Error(ContentVersionHashErrorCode.SourceInventoryMismatch,
                        "Source is null or its filename is outside the exact 49-source inventory.",
                        fileName));
                    continue;
                }

                if (!result.TryGetValue(fileName, out var matches))
                {
                    matches = new List<ForeignKeySourceSet.Source>();
                    result.Add(fileName, matches);
                }

                matches.Add(source);
                if (matches.Count > 1)
                {
                    errors.Add(Error(ContentVersionHashErrorCode.SourceInventoryMismatch,
                        "Source filename occurs more than once.", fileName));
                }
            }

            foreach (var fileName in ForeignKeySourceSet.ExpectedFileNames)
            {
                if (!result.ContainsKey(fileName))
                {
                    errors.Add(Error(ContentVersionHashErrorCode.SourceInventoryMismatch,
                        "Required static source is missing.", fileName));
                }
            }

            return result;
        }

        private static void ValidateInventory(
            IReadOnlyDictionary<string, List<ForeignKeySourceSet.Source>> sourcesByFile,
            CsvSchemaCatalog catalog,
            ICollection<ContentVersionHashError> errors)
        {
            foreach (var fileName in ForeignKeySourceSet.ExpectedFileNames)
            {
                if (!sourcesByFile.TryGetValue(fileName, out var matches) || matches.Count != 1)
                {
                    continue;
                }

                var source = matches[0];
                if (catalog == null || !catalog.TryGetFile(fileName, out var schema) ||
                    !ReferenceEquals(schema, source.Schema))
                {
                    errors.Add(Error(ContentVersionHashErrorCode.CatalogMismatch,
                        "Source schema is not the exact matching catalog schema instance.", fileName));
                    continue;
                }

                ValidateSchema(schema, source.ParseResult, errors);
            }
        }

        private static void ValidateSchema(
            CsvFileSchema schema,
            CsvScalarAndListParseResult parseResult,
            ICollection<ContentVersionHashError> errors)
        {
            if (schema.PrimaryKeyColumns.Count == 0 || parseResult == null ||
                !parseResult.Success || parseResult.Errors.Count != 0)
            {
                errors.Add(Error(ContentVersionHashErrorCode.SchemaMismatch,
                    "Source requires a primary key and a successful zero-error parse result.",
                    schema.FileName));
                return;
            }

            for (var index = 0; index < schema.Columns.Count; index++)
            {
                var column = schema.Columns[index];
                if (column == null || column.ColumnOrder != index + 1 ||
                    !string.Equals(column.FileName, schema.FileName, StringComparison.Ordinal))
                {
                    errors.Add(Error(ContentVersionHashErrorCode.SchemaMismatch,
                        "Schema column inventory, filename, or order is inconsistent.",
                        schema.FileName, fieldName: column?.ColumnName));
                }
            }

            var recordNumbers = new HashSet<int>();
            var records = new HashSet<CsvParsedRecord>(ReferenceComparer<CsvParsedRecord>.Instance);
            foreach (var record in parseResult.Records)
            {
                if (record == null || record.SourceRecord == null || record.ValidatedRecord == null ||
                    !ReferenceEquals(record.SourceRecord, record.ValidatedRecord.SourceRecord) ||
                    record.RecordNumber != record.SourceRecord.RecordNumber ||
                    record.Fields.Count != schema.Columns.Count ||
                    !records.Add(record) || !recordNumbers.Add(record.RecordNumber))
                {
                    errors.Add(Error(ContentVersionHashErrorCode.SchemaMismatch,
                        "Parsed record identity or field inventory is inconsistent.",
                        schema.FileName, record?.RecordNumber,
                        sourceLocation: record?.SourceRecord?.StartLocation));
                    continue;
                }

                for (var index = 0; index < schema.Columns.Count; index++)
                {
                    var field = record.Fields[index];
                    if (field == null || field.Value == null || field.ValidatedField == null ||
                        !ReferenceEquals(field.Schema, schema.Columns[index]) ||
                        field.Value.DataType != schema.Columns[index].DataType)
                    {
                        errors.Add(Error(ContentVersionHashErrorCode.SchemaMismatch,
                            "Parsed field does not match the exact schema column and data type.",
                            schema.FileName, record.RecordNumber,
                            schema.Columns[index].ColumnName,
                            record.SourceRecord.StartLocation));
                    }
                }
            }
        }

        private static void ValidateRecordIdentities(
            StaticDataRegistry registry,
            IReadOnlyDictionary<string, List<ForeignKeySourceSet.Source>> sourcesByFile,
            ICollection<ContentVersionHashError> errors)
        {
            if (registry.ForeignKeyResolution == null || !registry.ForeignKeyResolution.Success ||
                registry.RecordIndex == null)
            {
                errors.Add(Error(ContentVersionHashErrorCode.RecordIdentityMismatch,
                    "Registry must retain a successful foreign-key result and record index."));
                return;
            }

            var registryByRecord = new Dictionary<CsvParsedRecord, ForeignKeyRecordIdentity>(
                ReferenceComparer<CsvParsedRecord>.Instance);
            var registryKeys = new HashSet<RecordKey>();
            foreach (var identity in registry.Records)
            {
                if (identity == null || identity.SourceRecord == null ||
                    !registryByRecord.TryAdd(identity.SourceRecord, identity) ||
                    !registryKeys.Add(new RecordKey(identity.FileName, identity.RecordNumber)))
                {
                    errors.Add(Error(ContentVersionHashErrorCode.RecordIdentityMismatch,
                        "Registry contains a null or duplicate file-record identity.",
                        identity?.FileName, identity?.RecordNumber));
                }
            }

            var sourceRecordCount = 0;
            foreach (var pair in sourcesByFile)
            {
                if (pair.Value.Count != 1 || pair.Value[0].ParseResult == null) continue;
                foreach (var record in pair.Value[0].ParseResult.Records)
                {
                    sourceRecordCount++;
                    if (!registryByRecord.TryGetValue(record, out var identity) ||
                        !string.Equals(identity.FileName, pair.Key, StringComparison.Ordinal) ||
                        identity.RecordNumber != record.RecordNumber)
                    {
                        errors.Add(Error(ContentVersionHashErrorCode.RecordIdentityMismatch,
                            "Source parsed record is not the exact registry record identity.",
                            pair.Key, record?.RecordNumber,
                            sourceLocation: record?.SourceRecord?.StartLocation));
                    }
                }
            }

            if (sourceRecordCount != registry.Records.Count)
            {
                errors.Add(Error(ContentVersionHashErrorCode.RecordIdentityMismatch,
                    "Source and registry record inventories have different counts."));
            }
        }

        private static List<CanonicalFile> BuildCanonicalFiles(
            IReadOnlyDictionary<string, List<ForeignKeySourceSet.Source>> sourcesByFile,
            ICollection<ContentVersionHashError> errors)
        {
            var files = new List<CanonicalFile>();
            foreach (var fileName in ForeignKeySourceSet.ExpectedFileNames
                         .OrderBy(item => item, StringComparer.Ordinal))
            {
                var source = sourcesByFile[fileName][0];
                var schema = source.Schema;
                var records = new List<CanonicalRecord>();
                var keys = new HashSet<CanonicalKey>();
                foreach (var record in source.ParseResult.Records)
                {
                    var keyComponents = new List<string>();
                    foreach (var primaryKeyColumn in schema.PrimaryKeyColumns)
                    {
                        var field = record.Fields[primaryKeyColumn.ColumnOrder - 1];
                        var keyValue = string.Empty;
                        try
                        {
                            if (!TryScalar(field.Value, out keyValue))
                            {
                                errors.Add(Error(ContentVersionHashErrorCode.UnsupportedValue,
                                    "Primary-key values must use a supported scalar type.",
                                    fileName, record.RecordNumber, primaryKeyColumn.ColumnName,
                                    field.ValidatedField.SourceField.StartLocation));
                                keyValue = string.Empty;
                            }

                            ValidateUtf8(keyValue);
                        }
                        catch (Exception exception) when (
                            exception is EncoderFallbackException ||
                            exception is InvalidOperationException)
                        {
                            errors.Add(Error(ContentVersionHashErrorCode.UnsupportedValue,
                                "Primary-key value cannot be represented canonically: " + exception.Message,
                                fileName, record.RecordNumber, primaryKeyColumn.ColumnName,
                                field.ValidatedField.SourceField.StartLocation));
                            keyValue = string.Empty;
                        }

                        keyComponents.Add(keyValue);
                    }

                    var key = new CanonicalKey(keyComponents);
                    if (!keys.Add(key))
                    {
                        errors.Add(Error(ContentVersionHashErrorCode.DuplicateCanonicalPrimaryKey,
                            "Canonical primary-key tuple occurs more than once.",
                            fileName, record.RecordNumber,
                            sourceLocation: record.SourceRecord.StartLocation));
                    }

                    var fields = new List<CanonicalField>();
                    for (var index = 0; index < schema.Columns.Count; index++)
                    {
                        fields.Add(CanonicalizeField(fileName, record, schema.Columns[index],
                            record.Fields[index], errors));
                    }

                    records.Add(new CanonicalRecord(key, fields));
                }

                records.Sort((left, right) => left.Key.CompareTo(right.Key));
                files.Add(new CanonicalFile(schema, records));
            }

            return files;
        }

        private static CanonicalField CanonicalizeField(
            string fileName,
            CsvParsedRecord record,
            CsvColumnSchema column,
            CsvParsedField field,
            ICollection<ContentVersionHashError> errors)
        {
            try
            {
                if (field.Value.IsEmpty)
                {
                    return IsList(column.DataType)
                        ? CanonicalField.List(Array.Empty<string>())
                        : CanonicalField.Scalar(string.Empty);
                }

                switch (column.DataType)
                {
                    case CsvSchemaDataType.IdList:
                    case CsvSchemaDataType.EnumList:
                        return CanonicalField.List(field.Value.StringListValue);
                    case CsvSchemaDataType.IntList:
                        return CanonicalField.List(field.Value.IntListValue.Select(item =>
                            item.ToString(CultureInfo.InvariantCulture)));
                    default:
                        if (TryScalar(field.Value, out var scalar))
                        {
                            ValidateUtf8(scalar);
                            return CanonicalField.Scalar(scalar);
                        }

                        break;
                }
            }
            catch (Exception exception) when (
                exception is EncoderFallbackException || exception is InvalidOperationException)
            {
                errors.Add(Error(ContentVersionHashErrorCode.UnsupportedValue,
                    "Value cannot be represented canonically: " + exception.Message,
                    fileName, record.RecordNumber, column.ColumnName,
                    field.ValidatedField.SourceField.StartLocation));
                return CanonicalField.Scalar(string.Empty);
            }

            errors.Add(Error(ContentVersionHashErrorCode.UnsupportedValue,
                "Value uses an unsupported canonical data type.",
                fileName, record.RecordNumber, column.ColumnName,
                field.ValidatedField.SourceField.StartLocation));
            return CanonicalField.Scalar(string.Empty);
        }

        private static bool TryScalar(CsvParsedValue value, out string canonical)
        {
            if (value.IsEmpty)
            {
                canonical = string.Empty;
                return !IsList(value.DataType);
            }

            switch (value.DataType)
            {
                case CsvSchemaDataType.String:
                case CsvSchemaDataType.Id:
                case CsvSchemaDataType.Enum:
                    canonical = value.StringValue;
                    return true;
                case CsvSchemaDataType.Int:
                    canonical = value.IntValue.ToString(CultureInfo.InvariantCulture);
                    return true;
                case CsvSchemaDataType.ULong:
                    canonical = value.ULongValue.ToString(CultureInfo.InvariantCulture);
                    return true;
                case CsvSchemaDataType.Float:
                    var number = value.FloatValue;
                    if (float.IsNaN(number) || float.IsInfinity(number))
                    {
                        throw new InvalidOperationException("NaN and Infinity are not canonical values.");
                    }

                    canonical = number == 0f
                        ? "0"
                        : number.ToString("R", CultureInfo.InvariantCulture);
                    return true;
                case CsvSchemaDataType.Bool:
                    canonical = value.BoolValue ? "1" : "0";
                    return true;
                case CsvSchemaDataType.Hex:
                    canonical = value.HexValue.OriginalValue;
                    return true;
                case CsvSchemaDataType.DateTime:
                    canonical = value.DateTimeValue.ToUniversalTime().ToString(
                        "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture);
                    return true;
                default:
                    canonical = null;
                    return false;
            }
        }

        private static bool IsList(CsvSchemaDataType dataType)
        {
            return dataType == CsvSchemaDataType.IdList ||
                   dataType == CsvSchemaDataType.EnumList ||
                   dataType == CsvSchemaDataType.IntList;
        }

        private static void ValidateUtf8(string value)
        {
            StrictUtf8.GetByteCount(value);
        }

        private static void WriteFile(ContentHashCanonicalWriter writer, CanonicalFile file)
        {
            writer.WriteString(file.Schema.FileName);
            writer.WriteCount(file.Schema.Columns.Count);
            foreach (var column in file.Schema.Columns)
            {
                writer.WriteString(column.ColumnName);
                writer.WriteString(CsvSchemaDataTypes.ToToken(column.DataType));
            }

            writer.WriteCount(file.Records.Count);
            foreach (var record in file.Records)
            {
                writer.WriteCount(record.Key.Components.Count);
                foreach (var component in record.Key.Components)
                {
                    writer.WriteString(component);
                }

                writer.WriteCount(record.Fields.Count);
                for (var index = 0; index < record.Fields.Count; index++)
                {
                    var column = file.Schema.Columns[index];
                    var field = record.Fields[index];
                    writer.WriteString(column.ColumnName);
                    writer.WriteString(CsvSchemaDataTypes.ToToken(column.DataType));
                    if (field.IsList)
                    {
                        writer.WriteCount(field.Values.Count);
                        foreach (var item in field.Values) writer.WriteString(item);
                    }
                    else
                    {
                        writer.WriteString(field.Values[0]);
                    }
                }
            }
        }

        private static ContentVersionHashError Error(
            ContentVersionHashErrorCode code,
            string message,
            string fileName = null,
            int? recordNumber = null,
            string fieldName = null,
            CsvSourceLocation? sourceLocation = null)
        {
            return new ContentVersionHashError(
                code, message, fileName, recordNumber, fieldName, sourceLocation);
        }

        private sealed class CanonicalFile
        {
            public CanonicalFile(CsvFileSchema schema, List<CanonicalRecord> records)
            {
                Schema = schema;
                Records = records;
            }

            public CsvFileSchema Schema { get; }
            public List<CanonicalRecord> Records { get; }
        }

        private sealed class CanonicalRecord
        {
            public CanonicalRecord(CanonicalKey key, List<CanonicalField> fields)
            {
                Key = key;
                Fields = fields;
            }

            public CanonicalKey Key { get; }
            public List<CanonicalField> Fields { get; }
        }

        private sealed class CanonicalField
        {
            private CanonicalField(bool isList, IEnumerable<string> values)
            {
                IsList = isList;
                Values = new List<string>(values);
                foreach (var value in Values) ValidateUtf8(value);
            }

            public bool IsList { get; }
            public List<string> Values { get; }
            public static CanonicalField Scalar(string value) =>
                new CanonicalField(false, new[] { value });
            public static CanonicalField List(IEnumerable<string> values) =>
                new CanonicalField(true, values);
        }

        private sealed class CanonicalKey : IEquatable<CanonicalKey>, IComparable<CanonicalKey>
        {
            public CanonicalKey(IEnumerable<string> components)
            {
                Components = new List<string>(components);
            }

            public List<string> Components { get; }

            public int CompareTo(CanonicalKey other)
            {
                var count = Math.Min(Components.Count, other.Components.Count);
                for (var index = 0; index < count; index++)
                {
                    var comparison = StringComparer.Ordinal.Compare(
                        Components[index], other.Components[index]);
                    if (comparison != 0) return comparison;
                }

                return Components.Count.CompareTo(other.Components.Count);
            }

            public bool Equals(CanonicalKey other) =>
                other != null && CompareTo(other) == 0;
            public override bool Equals(object obj) => Equals(obj as CanonicalKey);
            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = 17;
                    foreach (var component in Components)
                    {
                        hash = (hash * 31) ^ StringComparer.Ordinal.GetHashCode(component);
                    }

                    return hash;
                }
            }
        }

        private readonly struct RecordKey : IEquatable<RecordKey>
        {
            private readonly string fileName;
            private readonly int recordNumber;

            public RecordKey(string fileName, int recordNumber)
            {
                this.fileName = fileName;
                this.recordNumber = recordNumber;
            }

            public bool Equals(RecordKey other) =>
                recordNumber == other.recordNumber &&
                string.Equals(fileName, other.fileName, StringComparison.Ordinal);
            public override bool Equals(object obj) => obj is RecordKey other && Equals(other);
            public override int GetHashCode()
            {
                unchecked
                {
                    return (StringComparer.Ordinal.GetHashCode(fileName) * 397) ^ recordNumber;
                }
            }
        }

        private sealed class ReferenceComparer<T> : IEqualityComparer<T>
            where T : class
        {
            public static readonly ReferenceComparer<T> Instance = new ReferenceComparer<T>();
            public bool Equals(T left, T right) => ReferenceEquals(left, right);
            public int GetHashCode(T value) => RuntimeHelpers.GetHashCode(value);
        }
    }
}
