using System;
using System.Collections.Generic;
using System.Globalization;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvScalarAndListParser
    {
        private static readonly NumberStyles IntegerStyles = NumberStyles.AllowLeadingSign;
        private static readonly NumberStyles FloatStyles =
            NumberStyles.AllowLeadingSign |
            NumberStyles.AllowDecimalPoint |
            NumberStyles.AllowExponent;

        public CsvScalarAndListParseResult Parse(
            CsvFileSchema schema,
            CsvHeaderFieldValidationResult validationResult,
            CsvPrimaryKeyIndexBuildResult primaryKeyResult)
        {
            ValidateInputs(schema, validationResult, primaryKeyResult);
            var sourceName = primaryKeyResult.Index.Entries.Count == 0
                ? schema.FileName
                : primaryKeyResult.Index.Entries[0].SourceName;
            return ParseValidated(schema, validationResult, sourceName);
        }

        public CsvScalarAndListParseResult Parse(
            CsvFileSchema schema,
            CsvHeaderFieldValidationResult validationResult,
            CsvPrimaryKeyIndexBuildResult primaryKeyResult,
            string sourceName)
        {
            if (sourceName == null)
            {
                throw new ArgumentNullException(nameof(sourceName));
            }

            ValidateInputs(schema, validationResult, primaryKeyResult);
            return ParseValidated(schema, validationResult, sourceName);
        }

        private static CsvScalarAndListParseResult ParseValidated(
            CsvFileSchema schema,
            CsvHeaderFieldValidationResult validationResult,
            string sourceName)
        {
            var parsedRecords = new List<CsvParsedRecord>(validationResult.Records.Count);
            var errors = new List<CsvValueParseError>();
            foreach (var validatedRecord in validationResult.Records)
            {
                var parsedFields = new List<CsvParsedField>(validatedRecord.Fields.Count);
                foreach (var validatedField in validatedRecord.Fields)
                {
                    if (TryParseField(
                            sourceName,
                            schema.FileName,
                            validatedField,
                            errors,
                            out var parsedValue))
                    {
                        parsedFields.Add(new CsvParsedField(validatedField, parsedValue));
                    }
                }

                if (parsedFields.Count == validatedRecord.Fields.Count)
                {
                    parsedRecords.Add(new CsvParsedRecord(validatedRecord, parsedFields));
                }
            }

            errors.Sort(CsvValueParseError.Compare);
            return errors.Count == 0
                ? new CsvScalarAndListParseResult(parsedRecords, errors)
                : new CsvScalarAndListParseResult(
                    Array.Empty<CsvParsedRecord>(),
                    errors);
        }

        private static void ValidateInputs(
            CsvFileSchema schema,
            CsvHeaderFieldValidationResult validationResult,
            CsvPrimaryKeyIndexBuildResult primaryKeyResult)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (validationResult == null)
            {
                throw new ArgumentNullException(nameof(validationResult));
            }

            if (primaryKeyResult == null)
            {
                throw new ArgumentNullException(nameof(primaryKeyResult));
            }

            if (!validationResult.Success || validationResult.Errors.Count != 0)
            {
                throw new InvalidOperationException(
                    "Scalar/list parsing requires a successful header/field validation result.");
            }

            if (!primaryKeyResult.Success ||
                primaryKeyResult.Duplicates.Count != 0 ||
                primaryKeyResult.Index == null)
            {
                throw new InvalidOperationException(
                    "Scalar/list parsing requires a successful primary-key index result.");
            }

            var index = primaryKeyResult.Index;
            if (!string.Equals(index.SchemaFileName, schema.FileName, StringComparison.Ordinal) ||
                index.Count != validationResult.Records.Count)
            {
                throw new InvalidOperationException(
                    "Schema, validated record, and primary-key index identities do not match.");
            }

            var validatedRecords = new HashSet<CsvValidatedRecord>();
            foreach (var record in validationResult.Records)
            {
                ValidateRecordMapping(schema, record);
                if (!validatedRecords.Add(record))
                {
                    throw new InvalidOperationException(
                        "A validated CSV record occurs more than once.");
                }
            }

            var indexedRecords = new HashSet<CsvValidatedRecord>();
            string indexedSourceName = null;
            foreach (var entry in index.Entries)
            {
                if (entry == null ||
                    !string.Equals(entry.SchemaFileName, schema.FileName, StringComparison.Ordinal) ||
                    entry.SourceValidatedRecord == null ||
                    !validatedRecords.Contains(entry.SourceValidatedRecord) ||
                    !ReferenceEquals(entry.SourceRecord, entry.SourceValidatedRecord.SourceRecord) ||
                    !indexedRecords.Add(entry.SourceValidatedRecord))
                {
                    throw new InvalidOperationException(
                        "A primary-key index entry does not match the validated records.");
                }

                if (indexedSourceName == null)
                {
                    indexedSourceName = entry.SourceName;
                }
                else if (!string.Equals(
                             indexedSourceName,
                             entry.SourceName,
                             StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Primary-key index entries do not share one source name.");
                }

                ValidatePrimaryKeyEntry(schema, entry);
            }

            if (indexedRecords.Count != validatedRecords.Count)
            {
                throw new InvalidOperationException(
                    "Primary-key index entries do not cover every validated record exactly once.");
            }
        }

        private static void ValidateRecordMapping(
            CsvFileSchema schema,
            CsvValidatedRecord record)
        {
            if (record == null ||
                record.SourceRecord == null ||
                record.RecordNumber != record.SourceRecord.RecordNumber ||
                record.Fields.Count != schema.Columns.Count ||
                record.SourceRecord.Fields.Count != schema.Columns.Count)
            {
                throw new InvalidOperationException(
                    "Validated CSV record count or source mapping does not match the schema.");
            }

            for (var fieldIndex = 0; fieldIndex < schema.Columns.Count; fieldIndex++)
            {
                var field = record.Fields[fieldIndex];
                if (field == null ||
                    !ReferenceEquals(field.Schema, schema.Columns[fieldIndex]) ||
                    !ReferenceEquals(field.SourceField, record.SourceRecord.Fields[fieldIndex]))
                {
                    throw new InvalidOperationException(
                        "Validated CSV field mapping does not match the supplied schema.");
                }
            }
        }

        private static void ValidatePrimaryKeyEntry(
            CsvFileSchema schema,
            CsvPrimaryKeyOccurrence entry)
        {
            if (entry.Key == null ||
                entry.PrimaryKeyFields.Count != schema.PrimaryKeyColumns.Count ||
                entry.Key.Components.Count != schema.PrimaryKeyColumns.Count)
            {
                throw new InvalidOperationException(
                    "Primary-key entry component count does not match the schema.");
            }

            for (var keyIndex = 0; keyIndex < schema.PrimaryKeyColumns.Count; keyIndex++)
            {
                var expectedSchema = schema.PrimaryKeyColumns[keyIndex];
                var entryField = entry.PrimaryKeyFields[keyIndex];
                if (entryField == null ||
                    !ReferenceEquals(entryField.Schema, expectedSchema) ||
                    !string.Equals(
                        entry.Key.Components[keyIndex],
                        entryField.EffectiveValue,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Primary-key entry fields do not match the supplied schema.");
                }
            }
        }

        private static bool TryParseField(
            string sourceName,
            string schemaFileName,
            CsvValidatedField field,
            ICollection<CsvValueParseError> errors,
            out CsvParsedValue value)
        {
            var text = field.EffectiveValue;
            if (text.Length == 0)
            {
                value = CsvParsedValue.Empty(field.Schema.DataType);
                return true;
            }

            switch (field.Schema.DataType)
            {
                case CsvSchemaDataType.String:
                    value = CsvParsedValue.FromString(CsvSchemaDataType.String, text);
                    return true;
                case CsvSchemaDataType.Id:
                    if (IsId(text))
                    {
                        value = CsvParsedValue.FromString(CsvSchemaDataType.Id, text);
                        return true;
                    }

                    AddScalarError(
                        errors,
                        sourceName,
                        schemaFileName,
                        field,
                        CsvValueParseErrorCode.InvalidId,
                        "ID must contain only ASCII A-Z, 0-9, or underscore.");
                    break;
                case CsvSchemaDataType.Int:
                    if (TryParseInteger(text, out var integer))
                    {
                        value = CsvParsedValue.FromInteger(integer);
                        return true;
                    }

                    AddScalarError(
                        errors,
                        sourceName,
                        schemaFileName,
                        field,
                        CsvValueParseErrorCode.InvalidInteger,
                        "INT must be an invariant signed decimal integer in Int32 range.");
                    break;
                case CsvSchemaDataType.ULong:
                    if (TryParseUnsignedInteger(text, out var unsignedInteger))
                    {
                        value = CsvParsedValue.FromUnsignedInteger(unsignedInteger);
                        return true;
                    }

                    AddScalarError(
                        errors,
                        sourceName,
                        schemaFileName,
                        field,
                        CsvValueParseErrorCode.InvalidUnsignedInteger,
                        "ULONG must contain only decimal digits and fit UInt64.");
                    break;
                case CsvSchemaDataType.Float:
                    if (TryParseFloat(text, out var floatingPoint))
                    {
                        value = CsvParsedValue.FromFloat(floatingPoint);
                        return true;
                    }

                    AddScalarError(
                        errors,
                        sourceName,
                        schemaFileName,
                        field,
                        CsvValueParseErrorCode.InvalidFloat,
                        "FLOAT must use finite invariant decimal or exponent syntax.");
                    break;
                case CsvSchemaDataType.Bool:
                    if (text == "0" || text == "1")
                    {
                        value = CsvParsedValue.FromBoolean(text == "1");
                        return true;
                    }

                    AddScalarError(
                        errors,
                        sourceName,
                        schemaFileName,
                        field,
                        CsvValueParseErrorCode.InvalidBoolean,
                        "BOOL must be exactly 0 or 1.");
                    break;
                case CsvSchemaDataType.Enum:
                    if (field.Schema.AllowedValues.Count == 0 ||
                        ContainsOrdinal(field.Schema.AllowedValues, text))
                    {
                        value = CsvParsedValue.FromString(CsvSchemaDataType.Enum, text);
                        return true;
                    }

                    AddScalarError(
                        errors,
                        sourceName,
                        schemaFileName,
                        field,
                        CsvValueParseErrorCode.InvalidEnum,
                        "ENUM must exactly match one allowed value.",
                        field.Schema.AllowedValues);
                    break;
                case CsvSchemaDataType.Hex:
                    if (TryParseHex(text, out var bytes))
                    {
                        value = CsvParsedValue.FromHex(new CsvHexValue(text, bytes));
                        return true;
                    }

                    AddScalarError(
                        errors,
                        sourceName,
                        schemaFileName,
                        field,
                        CsvValueParseErrorCode.InvalidHex,
                        "HEX must use an optional 0x prefix and at least one ASCII hex digit.");
                    break;
                case CsvSchemaDataType.DateTime:
                    if (TryParseUtcDateTime(text, out var dateTime))
                    {
                        value = CsvParsedValue.FromDateTime(dateTime);
                        return true;
                    }

                    AddScalarError(
                        errors,
                        sourceName,
                        schemaFileName,
                        field,
                        CsvValueParseErrorCode.InvalidDateTime,
                        "DATETIME must use exact UTC ISO-8601 with Z and up to seven fractional digits.");
                    break;
                case CsvSchemaDataType.IdList:
                case CsvSchemaDataType.EnumList:
                    return TryParseStringList(
                        sourceName,
                        schemaFileName,
                        field,
                        errors,
                        out value);
                case CsvSchemaDataType.IntList:
                    return TryParseIntegerList(
                        sourceName,
                        schemaFileName,
                        field,
                        errors,
                        out value);
                default:
                    throw new ArgumentOutOfRangeException();
            }

            value = null;
            return false;
        }

        private static bool TryParseStringList(
            string sourceName,
            string schemaFileName,
            CsvValidatedField field,
            ICollection<CsvValueParseError> errors,
            out CsvParsedValue value)
        {
            var components = field.EffectiveValue.Split('|');
            var parsed = new List<string>(components.Length);
            var success = true;
            for (var itemIndex = 0; itemIndex < components.Length; itemIndex++)
            {
                var item = components[itemIndex].Trim();
                if (item.Length == 0)
                {
                    AddListError(
                        errors,
                        sourceName,
                        schemaFileName,
                        field,
                        CsvValueParseErrorCode.EmptyListItem,
                        "List items cannot be empty after trimming.",
                        itemIndex,
                        item,
                        Array.Empty<string>());
                    success = false;
                    continue;
                }

                var isValid = field.Schema.DataType == CsvSchemaDataType.IdList
                    ? IsId(item)
                    : field.Schema.AllowedValues.Count == 0 ||
                      ContainsOrdinal(field.Schema.AllowedValues, item);
                if (!isValid)
                {
                    AddListError(
                        errors,
                        sourceName,
                        schemaFileName,
                        field,
                        CsvValueParseErrorCode.InvalidListItem,
                        field.Schema.DataType == CsvSchemaDataType.IdList
                            ? "ID_LIST item must contain only ASCII A-Z, 0-9, or underscore."
                            : "ENUM_LIST item must exactly match one allowed value.",
                        itemIndex,
                        item,
                        field.Schema.DataType == CsvSchemaDataType.EnumList
                            ? field.Schema.AllowedValues
                            : Array.Empty<string>());
                    success = false;
                    continue;
                }

                parsed.Add(item);
            }

            value = success
                ? CsvParsedValue.FromStringList(field.Schema.DataType, parsed)
                : null;
            return success;
        }

        private static bool TryParseIntegerList(
            string sourceName,
            string schemaFileName,
            CsvValidatedField field,
            ICollection<CsvValueParseError> errors,
            out CsvParsedValue value)
        {
            var components = field.EffectiveValue.Split('|');
            var parsed = new List<int>(components.Length);
            var success = true;
            for (var itemIndex = 0; itemIndex < components.Length; itemIndex++)
            {
                var item = components[itemIndex].Trim();
                if (item.Length == 0)
                {
                    AddListError(
                        errors,
                        sourceName,
                        schemaFileName,
                        field,
                        CsvValueParseErrorCode.EmptyListItem,
                        "List items cannot be empty after trimming.",
                        itemIndex,
                        item,
                        Array.Empty<string>());
                    success = false;
                    continue;
                }

                if (!TryParseInteger(item, out var integer))
                {
                    AddListError(
                        errors,
                        sourceName,
                        schemaFileName,
                        field,
                        CsvValueParseErrorCode.InvalidListItem,
                        "INT_LIST item must be an invariant signed decimal integer in Int32 range.",
                        itemIndex,
                        item,
                        Array.Empty<string>());
                    success = false;
                    continue;
                }

                parsed.Add(integer);
            }

            value = success ? CsvParsedValue.FromIntegerList(parsed) : null;
            return success;
        }

        private static void AddScalarError(
            ICollection<CsvValueParseError> errors,
            string sourceName,
            string schemaFileName,
            CsvValidatedField field,
            CsvValueParseErrorCode code,
            string message,
            IEnumerable<string> allowedValues = null)
        {
            errors.Add(new CsvValueParseError(
                sourceName,
                schemaFileName,
                field.Schema,
                code,
                message,
                field.SourceField.StartLocation,
                field.EffectiveValue,
                null,
                string.Empty,
                allowedValues));
        }

        private static void AddListError(
            ICollection<CsvValueParseError> errors,
            string sourceName,
            string schemaFileName,
            CsvValidatedField field,
            CsvValueParseErrorCode code,
            string message,
            int itemIndex,
            string itemValue,
            IEnumerable<string> allowedValues)
        {
            errors.Add(new CsvValueParseError(
                sourceName,
                schemaFileName,
                field.Schema,
                code,
                message,
                field.SourceField.StartLocation,
                field.EffectiveValue,
                itemIndex,
                itemValue,
                allowedValues));
        }

        private static bool IsId(string value)
        {
            if (value.Length == 0)
            {
                return false;
            }

            foreach (var character in value)
            {
                if ((character < 'A' || character > 'Z') &&
                    (character < '0' || character > '9') &&
                    character != '_')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryParseInteger(string value, out int result)
        {
            if (!HasSignedDecimalSyntax(value))
            {
                result = default;
                return false;
            }

            return int.TryParse(value, IntegerStyles, CultureInfo.InvariantCulture, out result);
        }

        private static bool TryParseUnsignedInteger(string value, out ulong result)
        {
            if (!HasDigitsOnly(value))
            {
                result = default;
                return false;
            }

            return ulong.TryParse(
                value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out result);
        }

        private static bool TryParseFloat(string value, out float result)
        {
            if (!HasFloatSyntax(value) ||
                !float.TryParse(value, FloatStyles, CultureInfo.InvariantCulture, out result) ||
                float.IsNaN(result) ||
                float.IsInfinity(result))
            {
                result = default;
                return false;
            }

            return true;
        }

        private static bool HasSignedDecimalSyntax(string value)
        {
            var index = value.Length > 0 && (value[0] == '+' || value[0] == '-') ? 1 : 0;
            if (index == value.Length)
            {
                return false;
            }

            for (; index < value.Length; index++)
            {
                if (value[index] < '0' || value[index] > '9')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasDigitsOnly(string value)
        {
            if (value.Length == 0)
            {
                return false;
            }

            foreach (var character in value)
            {
                if (character < '0' || character > '9')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasFloatSyntax(string value)
        {
            if (value.Length == 0)
            {
                return false;
            }

            var index = value[0] == '+' || value[0] == '-' ? 1 : 0;
            var digitsBeforePoint = 0;
            while (index < value.Length && IsAsciiDigit(value[index]))
            {
                digitsBeforePoint++;
                index++;
            }

            var digitsAfterPoint = 0;
            if (index < value.Length && value[index] == '.')
            {
                index++;
                while (index < value.Length && IsAsciiDigit(value[index]))
                {
                    digitsAfterPoint++;
                    index++;
                }
            }

            if (digitsBeforePoint + digitsAfterPoint == 0)
            {
                return false;
            }

            if (index < value.Length && (value[index] == 'e' || value[index] == 'E'))
            {
                index++;
                if (index < value.Length && (value[index] == '+' || value[index] == '-'))
                {
                    index++;
                }

                var exponentStart = index;
                while (index < value.Length && IsAsciiDigit(value[index]))
                {
                    index++;
                }

                if (index == exponentStart)
                {
                    return false;
                }
            }

            return index == value.Length;
        }

        private static bool TryParseHex(string value, out byte[] bytes)
        {
            var digitStart = value.StartsWith("0x", StringComparison.Ordinal) ||
                             value.StartsWith("0X", StringComparison.Ordinal)
                ? 2
                : 0;
            var digitCount = value.Length - digitStart;
            if (digitCount == 0)
            {
                bytes = null;
                return false;
            }

            for (var index = digitStart; index < value.Length; index++)
            {
                if (HexNibble(value[index]) < 0)
                {
                    bytes = null;
                    return false;
                }
            }

            bytes = new byte[(digitCount + 1) / 2];
            var sourceIndex = digitStart;
            var targetIndex = 0;
            if ((digitCount & 1) != 0)
            {
                bytes[targetIndex++] = (byte)HexNibble(value[sourceIndex++]);
            }

            while (sourceIndex < value.Length)
            {
                bytes[targetIndex++] = (byte)(
                    (HexNibble(value[sourceIndex]) << 4) |
                    HexNibble(value[sourceIndex + 1]));
                sourceIndex += 2;
            }

            return true;
        }

        private static int HexNibble(char value)
        {
            if (value >= '0' && value <= '9')
            {
                return value - '0';
            }

            if (value >= 'A' && value <= 'F')
            {
                return value - 'A' + 10;
            }

            return value >= 'a' && value <= 'f' ? value - 'a' + 10 : -1;
        }

        private static bool TryParseUtcDateTime(string value, out DateTimeOffset result)
        {
            if (!HasUtcDateTimeShape(value))
            {
                result = default;
                return false;
            }

            var format = value.Length == 20
                ? "yyyy-MM-dd'T'HH:mm:ss'Z'"
                : "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'";
            return DateTimeOffset.TryParseExact(
                value,
                format,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out result);
        }

        private static bool HasUtcDateTimeShape(string value)
        {
            if (value.Length != 20 && (value.Length < 22 || value.Length > 28))
            {
                return false;
            }

            if (value[4] != '-' || value[7] != '-' || value[10] != 'T' ||
                value[13] != ':' || value[16] != ':' || value[value.Length - 1] != 'Z')
            {
                return false;
            }

            for (var index = 0; index < 19; index++)
            {
                if (index == 4 || index == 7 || index == 10 || index == 13 || index == 16)
                {
                    continue;
                }

                if (!IsAsciiDigit(value[index]))
                {
                    return false;
                }
            }

            if (value.Length == 20)
            {
                return true;
            }

            if (value[19] != '.')
            {
                return false;
            }

            for (var index = 20; index < value.Length - 1; index++)
            {
                if (!IsAsciiDigit(value[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsAsciiDigit(char value)
        {
            return value >= '0' && value <= '9';
        }

        private static bool ContainsOrdinal(IReadOnlyList<string> values, string candidate)
        {
            foreach (var value in values)
            {
                if (string.Equals(value, candidate, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
