using System;
using System.Collections.Generic;
using System.Globalization;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvHeaderAndFieldValidator
    {
        public CsvHeaderFieldValidationResult Validate(
            CsvReadResult readResult,
            CsvFileSchema schema)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            return Validate(readResult, schema, schema.FileName);
        }

        public CsvHeaderFieldValidationResult Validate(
            CsvReadResult readResult,
            CsvFileSchema schema,
            string sourceName)
        {
            if (readResult == null)
            {
                throw new ArgumentNullException(nameof(readResult));
            }

            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (sourceName == null)
            {
                throw new ArgumentNullException(nameof(sourceName));
            }

            if (!readResult.Success)
            {
                return SyntaxFailure(readResult, schema, sourceName);
            }

            var errors = new List<CsvHeaderFieldError>();
            if (readResult.Records.Count == 0)
            {
                var fileStart = new CsvSourceLocation(0, 1, 1, 1, 1);
                foreach (var column in schema.Columns)
                {
                    errors.Add(Error(
                        sourceName,
                        schema,
                        CsvHeaderFieldErrorCode.MissingHeader,
                        "Expected header is missing: " + column.ColumnName + ".",
                        fileStart,
                        column.ColumnName,
                        string.Empty));
                }

                return Failure(errors);
            }

            var header = readResult.Records[0];
            ValidateHeader(sourceName, schema, header, errors);
            if (errors.Count > 0)
            {
                return Failure(errors);
            }

            var records = new List<CsvValidatedRecord>();
            for (var recordIndex = 1; recordIndex < readResult.Records.Count; recordIndex++)
            {
                var sourceRecord = readResult.Records[recordIndex];
                if (sourceRecord.Fields.Count != schema.Columns.Count)
                {
                    AddFieldCountError(sourceName, schema, sourceRecord, errors);
                    continue;
                }

                var fields = new List<CsvValidatedField>(schema.Columns.Count);
                for (var fieldIndex = 0; fieldIndex < schema.Columns.Count; fieldIndex++)
                {
                    var column = schema.Columns[fieldIndex];
                    var sourceField = sourceRecord.Fields[fieldIndex];
                    var rawValue = sourceField.Value;
                    var usedDefault = rawValue.Length == 0 && column.DefaultValue.Length > 0;
                    var effectiveValue = usedDefault ? column.DefaultValue : rawValue;

                    fields.Add(new CsvValidatedField(
                        column,
                        sourceField,
                        rawValue,
                        effectiveValue,
                        usedDefault));

                    if (column.IsRequired && effectiveValue.Length == 0)
                    {
                        errors.Add(Error(
                            sourceName,
                            schema,
                            CsvHeaderFieldErrorCode.RequiredFieldEmpty,
                            "Required field is empty after default application: " +
                            column.ColumnName + ".",
                            sourceField.StartLocation,
                            "non-empty",
                            rawValue));
                    }
                }

                records.Add(new CsvValidatedRecord(
                    sourceRecord.RecordNumber,
                    fields,
                    sourceRecord));
            }

            return errors.Count == 0
                ? new CsvHeaderFieldValidationResult(records, errors)
                : Failure(errors);
        }

        private static CsvHeaderFieldValidationResult SyntaxFailure(
            CsvReadResult readResult,
            CsvFileSchema schema,
            string fallbackSourceName)
        {
            var readError = readResult.Errors.Count > 0 ? readResult.Errors[0] : null;
            var sourceName = readError != null ? readError.SourceName : fallbackSourceName;
            var location = readError != null
                ? readError.Location
                : new CsvSourceLocation(0, 1, 1, 1, 1);
            var actualValue = readError != null ? readError.Code.ToString() : string.Empty;
            var message = readError != null
                ? "CSV syntax read failed: " + readError.Code + ": " + readError.Message
                : "CSV syntax read failed without an error payload.";

            return Failure(new[]
            {
                Error(
                    sourceName,
                    schema,
                    CsvHeaderFieldErrorCode.SyntaxReadFailed,
                    message,
                    location,
                    string.Empty,
                    actualValue),
            });
        }

        private static void ValidateHeader(
            string sourceName,
            CsvFileSchema schema,
            CsvRecord header,
            ICollection<CsvHeaderFieldError> errors)
        {
            var expectedNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var column in schema.Columns)
            {
                expectedNames.Add(column.ColumnName);
            }

            var actualNames = new HashSet<string>(StringComparer.Ordinal);
            for (var fieldIndex = 0; fieldIndex < header.Fields.Count; fieldIndex++)
            {
                var field = header.Fields[fieldIndex];
                var actualName = field.Value;
                var isFirstOccurrence = actualNames.Add(actualName);

                if (!expectedNames.Contains(actualName))
                {
                    errors.Add(Error(
                        sourceName,
                        schema,
                        CsvHeaderFieldErrorCode.UnexpectedHeader,
                        "Unexpected header: " + actualName + ".",
                        field.StartLocation,
                        string.Empty,
                        actualName));
                }

                if (!isFirstOccurrence)
                {
                    errors.Add(Error(
                        sourceName,
                        schema,
                        CsvHeaderFieldErrorCode.DuplicateHeader,
                        "Duplicate header after first occurrence: " + actualName + ".",
                        field.StartLocation,
                        actualName,
                        actualName));
                }
            }

            foreach (var column in schema.Columns)
            {
                if (!actualNames.Contains(column.ColumnName))
                {
                    errors.Add(Error(
                        sourceName,
                        schema,
                        CsvHeaderFieldErrorCode.MissingHeader,
                        "Expected header is missing: " + column.ColumnName + ".",
                        header.EndLocationExclusive,
                        column.ColumnName,
                        string.Empty));
                }
            }

            if (errors.Count > 0)
            {
                return;
            }

            for (var fieldIndex = 0; fieldIndex < header.Fields.Count; fieldIndex++)
            {
                var expectedName = schema.Columns[fieldIndex].ColumnName;
                var field = header.Fields[fieldIndex];
                if (!string.Equals(expectedName, field.Value, StringComparison.Ordinal))
                {
                    errors.Add(Error(
                        sourceName,
                        schema,
                        CsvHeaderFieldErrorCode.HeaderOrderMismatch,
                        "Header order mismatch at position " +
                        (fieldIndex + 1).ToString(CultureInfo.InvariantCulture) + ".",
                        field.StartLocation,
                        expectedName,
                        field.Value));
                }
            }
        }

        private static void AddFieldCountError(
            string sourceName,
            CsvFileSchema schema,
            CsvRecord sourceRecord,
            ICollection<CsvHeaderFieldError> errors)
        {
            var expectedCount = schema.Columns.Count;
            var actualCount = sourceRecord.Fields.Count;
            var location = actualCount > expectedCount
                ? sourceRecord.Fields[expectedCount].StartLocation
                : sourceRecord.EndLocationExclusive;

            errors.Add(Error(
                sourceName,
                schema,
                CsvHeaderFieldErrorCode.FieldCountMismatch,
                "Record field count mismatch: expected " +
                expectedCount.ToString(CultureInfo.InvariantCulture) + " but was " +
                actualCount.ToString(CultureInfo.InvariantCulture) + ".",
                location,
                expectedCount.ToString(CultureInfo.InvariantCulture),
                actualCount.ToString(CultureInfo.InvariantCulture)));
        }

        private static CsvHeaderFieldError Error(
            string sourceName,
            CsvFileSchema schema,
            CsvHeaderFieldErrorCode code,
            string message,
            CsvSourceLocation location,
            string expectedValue,
            string actualValue)
        {
            return new CsvHeaderFieldError(
                sourceName,
                schema.FileName,
                code,
                message,
                location,
                expectedValue,
                actualValue);
        }

        private static CsvHeaderFieldValidationResult Failure(
            IEnumerable<CsvHeaderFieldError> errors)
        {
            return new CsvHeaderFieldValidationResult(
                Array.Empty<CsvValidatedRecord>(),
                errors);
        }
    }
}
