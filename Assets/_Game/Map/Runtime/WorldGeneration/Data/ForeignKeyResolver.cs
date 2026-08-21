using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class ForeignKeyResolver
    {
        private static readonly HashSet<string> ExpectedFiles =
            new HashSet<string>(ForeignKeySourceSet.ExpectedFileNames, StringComparer.Ordinal);

        public ForeignKeyResolutionResult Resolve(ForeignKeySourceSet sourceSet)
        {
            var errors = new List<ForeignKeyResolutionError>();
            if (sourceSet == null)
            {
                foreach (var fileName in ForeignKeySourceSet.ExpectedFileNames)
                {
                    errors.Add(Error(
                        ForeignKeyResolutionErrorCode.MissingSource,
                        "Required foreign-key source is missing.",
                        fileName));
                }

                errors.Sort(ForeignKeyResolutionError.Compare);
                return Failure(errors);
            }

            var sourcesByFile = CollectSources(sourceSet, errors);
            ValidateSources(sourceSet.SchemaCatalog, sourcesByFile, errors);
            ValidateDeclarations(sourceSet.SchemaCatalog, errors);
            errors.Sort(ForeignKeyResolutionError.Compare);
            if (errors.Count > 0)
            {
                return Failure(errors);
            }

            var recordIndex = BuildIndex(
                sourceSet.SchemaCatalog,
                sourcesByFile,
                errors,
                out var identitiesByRecord);
            errors.Sort(ForeignKeyResolutionError.Compare);
            if (errors.Count > 0 || recordIndex == null)
            {
                return Failure(errors);
            }

            var references = ResolveReferences(
                sourceSet.SchemaCatalog,
                sourcesByFile,
                recordIndex,
                identitiesByRecord,
                errors);
            references.Sort(CompareReferences);
            errors.Sort(ForeignKeyResolutionError.Compare);
            return new ForeignKeyResolutionResult(recordIndex, references, errors);
        }

        private static Dictionary<string, List<ForeignKeySourceSet.Source>> CollectSources(
            ForeignKeySourceSet sourceSet,
            ICollection<ForeignKeyResolutionError> errors)
        {
            var sourcesByFile = new Dictionary<
                string,
                List<ForeignKeySourceSet.Source>>(StringComparer.Ordinal);
            foreach (var source in sourceSet.Sources)
            {
                if (source == null)
                {
                    errors.Add(Error(
                        ForeignKeyResolutionErrorCode.MissingSource,
                        "Foreign-key source entry cannot be null.",
                        string.Empty));
                    continue;
                }

                if (source.Schema == null)
                {
                    errors.Add(Error(
                        ForeignKeyResolutionErrorCode.SchemaMismatch,
                        "Foreign-key source schema cannot be null.",
                        string.Empty));
                    continue;
                }

                var fileName = source.Schema.FileName;
                if (!ExpectedFiles.Contains(fileName))
                {
                    errors.Add(Error(
                        ForeignKeyResolutionErrorCode.UnexpectedSource,
                        "Source filename is not part of the exact 49-source contract.",
                        fileName));
                    continue;
                }

                if (!sourcesByFile.TryGetValue(fileName, out var matches))
                {
                    matches = new List<ForeignKeySourceSet.Source>();
                    sourcesByFile.Add(fileName, matches);
                }

                matches.Add(source);
                if (matches.Count > 1)
                {
                    errors.Add(Error(
                        ForeignKeyResolutionErrorCode.DuplicateSource,
                        "Source filename occurs more than once.",
                        fileName));
                }
            }

            foreach (var fileName in ForeignKeySourceSet.ExpectedFileNames)
            {
                if (!sourcesByFile.ContainsKey(fileName))
                {
                    errors.Add(Error(
                        ForeignKeyResolutionErrorCode.MissingSource,
                        "Required foreign-key source is missing.",
                        fileName));
                }
            }

            return sourcesByFile;
        }

        private static void ValidateSources(
            CsvSchemaCatalog catalog,
            IReadOnlyDictionary<string, List<ForeignKeySourceSet.Source>> sourcesByFile,
            ICollection<ForeignKeyResolutionError> errors)
        {
            if (catalog == null)
            {
                errors.Add(Error(
                    ForeignKeyResolutionErrorCode.SchemaMismatch,
                    "Foreign-key source catalog cannot be null.",
                    string.Empty));
                return;
            }

            foreach (var fileName in ForeignKeySourceSet.ExpectedFileNames)
            {
                if (!sourcesByFile.TryGetValue(fileName, out var matches) ||
                    matches.Count != 1)
                {
                    continue;
                }

                var source = matches[0];
                if (!catalog.TryGetFile(fileName, out var catalogSchema) ||
                    !ReferenceEquals(source.Schema, catalogSchema))
                {
                    errors.Add(Error(
                        ForeignKeyResolutionErrorCode.SchemaMismatch,
                        "Source schema is not the matching catalog schema instance.",
                        fileName));
                    continue;
                }

                ValidateSchemaShape(catalogSchema, errors);
                if (source.ParseResult == null ||
                    !source.ParseResult.Success ||
                    source.ParseResult.Errors.Count != 0)
                {
                    errors.Add(Error(
                        ForeignKeyResolutionErrorCode.UnsuccessfulParse,
                        "Source parse result must be successful and contain zero errors.",
                        fileName));
                    continue;
                }

                ValidateRecords(fileName, catalogSchema, source.ParseResult, errors);
            }
        }

        private static void ValidateSchemaShape(
            CsvFileSchema schema,
            ICollection<ForeignKeyResolutionError> errors)
        {
            if (schema.PrimaryKeyColumns.Count == 0)
            {
                errors.Add(Error(
                    ForeignKeyResolutionErrorCode.SchemaMismatch,
                    "Every static source schema must declare a primary key.",
                    schema.FileName));
            }

            for (var index = 0; index < schema.Columns.Count; index++)
            {
                var column = schema.Columns[index];
                if (column == null ||
                    column.ColumnOrder != index + 1 ||
                    !string.Equals(column.FileName, schema.FileName, StringComparison.Ordinal))
                {
                    errors.Add(Error(
                        ForeignKeyResolutionErrorCode.SchemaMismatch,
                        "Schema filename, column inventory, or column order is inconsistent.",
                        schema.FileName,
                        sourceColumnOrder: index + 1));
                }
            }
        }

        private static void ValidateRecords(
            string fileName,
            CsvFileSchema schema,
            CsvScalarAndListParseResult parseResult,
            ICollection<ForeignKeyResolutionError> errors)
        {
            var seenRecords = new HashSet<CsvParsedRecord>(ReferenceComparer<CsvParsedRecord>.Instance);
            var seenRecordNumbers = new HashSet<int>();
            foreach (var record in parseResult.Records)
            {
                if (record == null)
                {
                    errors.Add(Error(
                        ForeignKeyResolutionErrorCode.SchemaMismatch,
                        "Parsed record cannot be null.",
                        fileName));
                    continue;
                }

                var validated = record.ValidatedRecord;
                var sourceRecord = record.SourceRecord;
                var identityValid = validated != null &&
                                    sourceRecord != null &&
                                    ReferenceEquals(sourceRecord, validated.SourceRecord) &&
                                    record.RecordNumber == validated.RecordNumber &&
                                    record.RecordNumber == sourceRecord.RecordNumber &&
                                    record.Fields.Count == schema.Columns.Count &&
                                    validated.Fields.Count == schema.Columns.Count &&
                                    sourceRecord.Fields.Count == schema.Columns.Count &&
                                    seenRecords.Add(record) &&
                                    seenRecordNumbers.Add(record.RecordNumber);
                if (!identityValid)
                {
                    errors.Add(Error(
                        ForeignKeyResolutionErrorCode.SchemaMismatch,
                        "Parsed, validated, and source record identities are inconsistent.",
                        fileName,
                        record.RecordNumber,
                        sourceLocation: sourceRecord?.StartLocation));
                    continue;
                }

                for (var index = 0; index < schema.Columns.Count; index++)
                {
                    var parsedField = record.Fields[index];
                    var validatedField = validated.Fields[index];
                    var sourceField = sourceRecord.Fields[index];
                    if (parsedField == null ||
                        parsedField.Value == null ||
                        !ReferenceEquals(parsedField.Schema, schema.Columns[index]) ||
                        !ReferenceEquals(parsedField.ValidatedField, validatedField) ||
                        !ReferenceEquals(validatedField.Schema, schema.Columns[index]) ||
                        !ReferenceEquals(validatedField.SourceField, sourceField) ||
                        parsedField.Value.DataType != schema.Columns[index].DataType)
                    {
                        errors.Add(Error(
                            ForeignKeyResolutionErrorCode.SchemaMismatch,
                            "Parsed field schema, source identity, or value type is inconsistent.",
                            fileName,
                            record.RecordNumber,
                            schema.Columns[index].ColumnOrder,
                            schema.Columns[index].ColumnName,
                            sourceField.StartLocation));
                    }
                }
            }
        }

        private static void ValidateDeclarations(
            CsvSchemaCatalog catalog,
            ICollection<ForeignKeyResolutionError> errors)
        {
            if (catalog == null) return;
            foreach (var fileName in ForeignKeySourceSet.ExpectedFileNames)
            {
                if (!catalog.TryGetFile(fileName, out var schema)) continue;
                foreach (var column in schema.Columns)
                {
                    var foreignKey = column.ForeignKey;
                    if (foreignKey == null) continue;
                    if (column.DataType != CsvSchemaDataType.Id &&
                        column.DataType != CsvSchemaDataType.IdList)
                    {
                        errors.Add(DeclarationError(
                            schema,
                            column,
                            "Only ID and ID_LIST columns may declare a foreign key."));
                        continue;
                    }

                    if (!ExpectedFiles.Contains(foreignKey.TargetFileName) ||
                        !catalog.TryGetFile(foreignKey.TargetFileName, out var targetSchema) ||
                        !targetSchema.TryGetColumn(
                            foreignKey.TargetColumnName,
                            out var targetColumn) ||
                        !targetColumn.PrimaryKeyOrder.HasValue)
                    {
                        errors.Add(DeclarationError(
                            schema,
                            column,
                            "Foreign-key target must be an exact declared primary-key column."));
                    }
                }
            }
        }

        private static ForeignKeyRecordIndex BuildIndex(
            CsvSchemaCatalog catalog,
            IReadOnlyDictionary<string, List<ForeignKeySourceSet.Source>> sourcesByFile,
            ICollection<ForeignKeyResolutionError> errors,
            out Dictionary<CsvParsedRecord, ForeignKeyRecordIdentity> identitiesByRecord)
        {
            identitiesByRecord = new Dictionary<
                CsvParsedRecord,
                ForeignKeyRecordIdentity>(ReferenceComparer<CsvParsedRecord>.Instance);
            var identities = new List<ForeignKeyRecordIdentity>();
            var lookupEntries = new List<ForeignKeyRecordIndex.LookupEntry>();
            var targetColumns = CollectTargetColumns(catalog);

            foreach (var fileName in ForeignKeySourceSet.ExpectedFileNames)
            {
                var schema = catalog.GetFile(fileName);
                var records = sourcesByFile[fileName][0].ParseResult.Records
                    .OrderBy(record => record.RecordNumber)
                    .ToArray();
                var primaryKeys = new Dictionary<CsvPrimaryKey, ForeignKeyRecordIdentity>();
                var targetValues = new Dictionary<TargetValueKey, ForeignKeyRecordIdentity>();

                foreach (var record in records)
                {
                    var identity = new ForeignKeyRecordIdentity(fileName, record);
                    identities.Add(identity);
                    identitiesByRecord.Add(record, identity);

                    var components = new List<string>(schema.PrimaryKeyColumns.Count);
                    foreach (var primaryKeyColumn in schema.PrimaryKeyColumns)
                    {
                        var field = FieldForColumn(schema, record, primaryKeyColumn);
                        components.Add(field.EffectiveValue);
                        var targetColumnKey = new TargetColumnKey(
                            fileName,
                            primaryKeyColumn.ColumnName);
                        if (!targetColumns.Contains(targetColumnKey)) continue;

                        var targetValueKey = new TargetValueKey(
                            primaryKeyColumn.ColumnName,
                            field.EffectiveValue);
                        if (!targetValues.TryAdd(targetValueKey, identity))
                        {
                            errors.Add(Error(
                                ForeignKeyResolutionErrorCode.InvalidForeignKeyDeclaration,
                                "Referenced target primary-key column is not unique by itself.",
                                fileName,
                                record.RecordNumber,
                                primaryKeyColumn.ColumnOrder,
                                primaryKeyColumn.ColumnName,
                                field.ValidatedField.SourceField.StartLocation,
                                targetFileName: fileName,
                                targetColumnName: primaryKeyColumn.ColumnName,
                                targetValue: field.EffectiveValue,
                                sourceIdentity: identity,
                                sourceField: field));
                        }
                        else
                        {
                            lookupEntries.Add(new ForeignKeyRecordIndex.LookupEntry(
                                fileName,
                                primaryKeyColumn.ColumnName,
                                field.EffectiveValue,
                                identity));
                        }
                    }

                    if (components.Any(component => component.Length == 0))
                    {
                        errors.Add(Error(
                            ForeignKeyResolutionErrorCode.SchemaMismatch,
                            "Primary-key components cannot be empty.",
                            fileName,
                            record.RecordNumber,
                            sourceLocation: record.SourceRecord.StartLocation,
                            sourceIdentity: identity));
                        continue;
                    }

                    var primaryKey = new CsvPrimaryKey(components);
                    if (!primaryKeys.TryAdd(primaryKey, identity))
                    {
                        errors.Add(Error(
                            ForeignKeyResolutionErrorCode.SchemaMismatch,
                            "Duplicate primary key reached the foreign-key input gate.",
                            fileName,
                            record.RecordNumber,
                            sourceLocation: record.SourceRecord.StartLocation,
                            sourceIdentity: identity));
                    }
                }
            }

            return errors.Count == 0
                ? new ForeignKeyRecordIndex(identities, lookupEntries)
                : null;
        }

        private static HashSet<TargetColumnKey> CollectTargetColumns(CsvSchemaCatalog catalog)
        {
            var result = new HashSet<TargetColumnKey>();
            foreach (var fileName in ForeignKeySourceSet.ExpectedFileNames)
            {
                foreach (var column in catalog.GetFile(fileName).Columns)
                {
                    if (column.ForeignKey != null)
                    {
                        result.Add(new TargetColumnKey(
                            column.ForeignKey.TargetFileName,
                            column.ForeignKey.TargetColumnName));
                    }
                }
            }

            return result;
        }

        private static List<ResolvedForeignKeyReference> ResolveReferences(
            CsvSchemaCatalog catalog,
            IReadOnlyDictionary<string, List<ForeignKeySourceSet.Source>> sourcesByFile,
            ForeignKeyRecordIndex recordIndex,
            IReadOnlyDictionary<CsvParsedRecord, ForeignKeyRecordIdentity> identitiesByRecord,
            ICollection<ForeignKeyResolutionError> errors)
        {
            var references = new List<ResolvedForeignKeyReference>();
            foreach (var fileName in ForeignKeySourceSet.ExpectedFileNames)
            {
                var schema = catalog.GetFile(fileName);
                foreach (var record in sourcesByFile[fileName][0].ParseResult.Records
                             .OrderBy(item => item.RecordNumber))
                {
                    var sourceIdentity = identitiesByRecord[record];
                    for (var columnIndex = 0; columnIndex < schema.Columns.Count; columnIndex++)
                    {
                        var column = schema.Columns[columnIndex];
                        var foreignKey = column.ForeignKey;
                        if (foreignKey == null) continue;
                        var field = record.Fields[columnIndex];
                        if (field.Value.IsEmpty) continue;

                        if (column.DataType == CsvSchemaDataType.Id)
                        {
                            ResolveValue(
                                sourceIdentity,
                                field,
                                null,
                                field.Value.IdValue,
                                foreignKey,
                                recordIndex,
                                references,
                                errors);
                        }
                        else
                        {
                            var values = field.Value.IdListValue;
                            for (var listIndex = 0; listIndex < values.Count; listIndex++)
                            {
                                ResolveValue(
                                    sourceIdentity,
                                    field,
                                    listIndex,
                                    values[listIndex],
                                    foreignKey,
                                    recordIndex,
                                    references,
                                    errors);
                            }
                        }
                    }
                }
            }

            return references;
        }

        private static void ResolveValue(
            ForeignKeyRecordIdentity sourceIdentity,
            CsvParsedField sourceField,
            int? listIndex,
            string value,
            CsvForeignKeyReference foreignKey,
            ForeignKeyRecordIndex recordIndex,
            ICollection<ResolvedForeignKeyReference> references,
            ICollection<ForeignKeyResolutionError> errors)
        {
            if (recordIndex.TryGet(
                    foreignKey.TargetFileName,
                    foreignKey.TargetColumnName,
                    value,
                    out var targetIdentity))
            {
                references.Add(new ResolvedForeignKeyReference(
                    sourceIdentity,
                    sourceField,
                    listIndex,
                    value,
                    foreignKey.TargetFileName,
                    foreignKey.TargetColumnName,
                    value,
                    targetIdentity));
                return;
            }

            errors.Add(Error(
                ForeignKeyResolutionErrorCode.MissingTargetRecord,
                "Foreign-key target record was not found.",
                sourceIdentity.FileName,
                sourceIdentity.RecordNumber,
                sourceField.Schema.ColumnOrder,
                sourceField.Schema.ColumnName,
                sourceField.ValidatedField.SourceField.StartLocation,
                listIndex,
                value,
                foreignKey.TargetFileName,
                foreignKey.TargetColumnName,
                value,
                sourceIdentity,
                sourceField));
        }

        private static CsvParsedField FieldForColumn(
            CsvFileSchema schema,
            CsvParsedRecord record,
            CsvColumnSchema column)
        {
            for (var index = 0; index < schema.Columns.Count; index++)
            {
                if (ReferenceEquals(schema.Columns[index], column))
                {
                    return record.Fields[index];
                }
            }

            throw new InvalidOperationException(
                "A primary-key column is not part of its source schema.");
        }

        private static int CompareReferences(
            ResolvedForeignKeyReference left,
            ResolvedForeignKeyReference right)
        {
            var comparison = StringComparer.Ordinal.Compare(
                left.SourceFileName,
                right.SourceFileName);
            if (comparison != 0) return comparison;
            comparison = left.SourceRecordNumber.CompareTo(right.SourceRecordNumber);
            if (comparison != 0) return comparison;
            comparison = left.SourceColumnOrder.CompareTo(right.SourceColumnOrder);
            if (comparison != 0) return comparison;
            comparison = Nullable.Compare(left.ListIndex, right.ListIndex);
            return comparison != 0
                ? comparison
                : StringComparer.Ordinal.Compare(left.TargetValue, right.TargetValue);
        }

        private static ForeignKeyResolutionResult Failure(
            IEnumerable<ForeignKeyResolutionError> errors)
        {
            return new ForeignKeyResolutionResult(
                null,
                Array.Empty<ResolvedForeignKeyReference>(),
                errors);
        }

        private static ForeignKeyResolutionError DeclarationError(
            CsvFileSchema schema,
            CsvColumnSchema column,
            string message)
        {
            return Error(
                ForeignKeyResolutionErrorCode.InvalidForeignKeyDeclaration,
                message,
                schema.FileName,
                sourceColumnOrder: column.ColumnOrder,
                sourceColumnName: column.ColumnName,
                targetFileName: column.ForeignKey?.TargetFileName,
                targetColumnName: column.ForeignKey?.TargetColumnName);
        }

        private static ForeignKeyResolutionError Error(
            ForeignKeyResolutionErrorCode code,
            string message,
            string sourceFileName,
            int? sourceRecordNumber = null,
            int? sourceColumnOrder = null,
            string sourceColumnName = null,
            CsvSourceLocation? sourceLocation = null,
            int? listIndex = null,
            string rawValue = null,
            string targetFileName = null,
            string targetColumnName = null,
            string targetValue = null,
            ForeignKeyRecordIdentity sourceIdentity = null,
            CsvParsedField sourceField = null)
        {
            return new ForeignKeyResolutionError(
                code,
                message,
                sourceFileName,
                sourceRecordNumber,
                sourceColumnOrder,
                sourceColumnName,
                sourceLocation,
                listIndex,
                rawValue,
                targetFileName,
                targetColumnName,
                targetValue,
                sourceIdentity,
                sourceField);
        }

        private readonly struct TargetColumnKey : IEquatable<TargetColumnKey>
        {
            private readonly string fileName;
            private readonly string columnName;

            public TargetColumnKey(string fileName, string columnName)
            {
                this.fileName = fileName;
                this.columnName = columnName;
            }

            public bool Equals(TargetColumnKey other)
            {
                return string.Equals(fileName, other.fileName, StringComparison.Ordinal) &&
                       string.Equals(columnName, other.columnName, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is TargetColumnKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (StringComparer.Ordinal.GetHashCode(fileName) * 397) ^
                           StringComparer.Ordinal.GetHashCode(columnName);
                }
            }
        }

        private readonly struct TargetValueKey : IEquatable<TargetValueKey>
        {
            private readonly string columnName;
            private readonly string value;

            public TargetValueKey(string columnName, string value)
            {
                this.columnName = columnName;
                this.value = value;
            }

            public bool Equals(TargetValueKey other)
            {
                return string.Equals(columnName, other.columnName, StringComparison.Ordinal) &&
                       string.Equals(value, other.value, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is TargetValueKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (StringComparer.Ordinal.GetHashCode(columnName) * 397) ^
                           StringComparer.Ordinal.GetHashCode(value);
                }
            }
        }

        private sealed class ReferenceComparer<T> : IEqualityComparer<T>
            where T : class
        {
            public static readonly ReferenceComparer<T> Instance = new ReferenceComparer<T>();

            public bool Equals(T left, T right)
            {
                return ReferenceEquals(left, right);
            }

            public int GetHashCode(T value)
            {
                return RuntimeHelpers.GetHashCode(value);
            }
        }
    }
}
