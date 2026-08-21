using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;

namespace StarNight.Map.Tests.WorldGeneration.Data
{
    public sealed class CsvHeaderAndFieldValidatorTests
    {
        [Test]
        public void ExactHeaderAndDataRecordSucceed()
        {
            var schema = Schema(Column("id", true), Column("value", true));
            var result = Validate("id,value\nitem,value", schema);

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Records.Count, Is.EqualTo(1));
            Assert.That(result.Records[0].RecordNumber, Is.EqualTo(2));
            Assert.That(result.Records[0].Fields.Select(field => field.EffectiveValue),
                Is.EqualTo(new[] { "item", "value" }));
        }

        [Test]
        public void HeaderComparisonIsOrdinalAndCaseSensitive()
        {
            var result = Validate(
                "ID,value",
                Schema(Column("id", true), Column("value", true)));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Errors.Select(error => error.ErrorCode),
                Is.EqualTo(new[]
                {
                    CsvHeaderFieldErrorCode.UnexpectedHeader,
                    CsvHeaderFieldErrorCode.MissingHeader,
                }));
            Assert.That(result.Errors[0].ActualValue, Is.EqualTo("ID"));
            Assert.That(result.Errors[1].ExpectedValue, Is.EqualTo("id"));
        }

        [Test]
        public void MissingHeaderRecordReportsEveryExpectedColumnAtFileStart()
        {
            var result = Validate(
                string.Empty,
                Schema(Column("id", true), Column("value", true)));

            Assert.That(result.Errors.Count, Is.EqualTo(2));
            Assert.That(result.Errors.Select(error => error.ErrorCode),
                Is.All.EqualTo(CsvHeaderFieldErrorCode.MissingHeader));
            Assert.That(result.Errors.Select(error => error.ExpectedValue),
                Is.EqualTo(new[] { "id", "value" }));
            foreach (var error in result.Errors)
            {
                AssertLocation(error.Location, 0, 1, 1, 1, 1);
            }
        }

        [Test]
        public void MissingOneHeaderUsesHeaderEndExclusiveLocation()
        {
            var result = Validate(
                "id",
                Schema(Column("id", true), Column("value", true)));

            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0].ErrorCode,
                Is.EqualTo(CsvHeaderFieldErrorCode.MissingHeader));
            Assert.That(result.Errors[0].ExpectedValue, Is.EqualTo("value"));
            AssertLocation(result.Errors[0].Location, 2, 1, 3, 1, 1);
        }

        [Test]
        public void UnexpectedHeaderUsesActualFieldStartLocation()
        {
            var result = Validate(
                "id,extra",
                Schema(Column("id", true)));

            var unexpected = result.Errors.Single(error =>
                error.ErrorCode == CsvHeaderFieldErrorCode.UnexpectedHeader);
            Assert.That(unexpected.ActualValue, Is.EqualTo("extra"));
            AssertLocation(unexpected.Location, 3, 1, 4, 1, 2);
        }

        [Test]
        public void DuplicateHeaderUsesSecondOccurrenceLocation()
        {
            var result = Validate(
                "id,id",
                Schema(Column("id", true), Column("value", true)));

            Assert.That(result.Errors.Select(error => error.ErrorCode),
                Is.EqualTo(new[]
                {
                    CsvHeaderFieldErrorCode.DuplicateHeader,
                    CsvHeaderFieldErrorCode.MissingHeader,
                }));
            AssertLocation(result.Errors[0].Location, 3, 1, 4, 1, 2);
        }

        [Test]
        public void ReorderedHeaderReportsEveryMismatchingActualPosition()
        {
            var result = Validate(
                "value,id",
                Schema(Column("id", true), Column("value", true)));

            Assert.That(result.Errors.Count, Is.EqualTo(2));
            Assert.That(result.Errors.Select(error => error.ErrorCode),
                Is.All.EqualTo(CsvHeaderFieldErrorCode.HeaderOrderMismatch));
            Assert.That(result.Errors.Select(error => error.ExpectedValue),
                Is.EqualTo(new[] { "id", "value" }));
            Assert.That(result.Errors.Select(error => error.ActualValue),
                Is.EqualTo(new[] { "value", "id" }));
        }

        [Test]
        public void MissingAndUnexpectedHeaderInventoryIsDeterministic()
        {
            var schema = Schema(Column("id", true), Column("value", true));
            var first = Validate("id,extra", schema);
            var second = Validate("id,extra", schema);

            Assert.That(first.Errors.Select(error => error.ErrorCode),
                Is.EqualTo(new[]
                {
                    CsvHeaderFieldErrorCode.UnexpectedHeader,
                    CsvHeaderFieldErrorCode.MissingHeader,
                }));
            Assert.That(first.Errors.Select(error => error.ToString()),
                Is.EqualTo(second.Errors.Select(error => error.ToString())));
        }

        [Test]
        public void HeaderErrorPublishesNoRecordsAndSkipsDataValidation()
        {
            var result = Validate(
                "id,wrong\nitem,",
                Schema(Column("id", true), Column("value", true)));

            Assert.That(result.Records, Is.Empty);
            Assert.That(result.Errors.Any(error =>
                error.ErrorCode == CsvHeaderFieldErrorCode.RequiredFieldEmpty), Is.False);
            Assert.That(result.Errors.Any(error =>
                error.ErrorCode == CsvHeaderFieldErrorCode.UnexpectedHeader), Is.True);
        }

        [Test]
        public void HeaderOnlyFileSucceedsWithNoDataRecords()
        {
            var result = Validate(
                "id,value",
                Schema(Column("id", true), Column("value", true)));

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.Records, Is.Empty);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void ExactFieldCountProducesSchemaOrderedFields()
        {
            var result = Validate(
                "id,value\nitem,text",
                Schema(Column("id", true), Column("value", false)));

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.Records[0].Fields.Select(field => field.Schema.ColumnName),
                Is.EqualTo(new[] { "id", "value" }));
            Assert.That(result.Records[0].Fields.Select(field => field.RawValue),
                Is.EqualTo(new[] { "item", "text" }));
        }

        [Test]
        public void TooFewFieldsReportsRecordEndExclusiveLocation()
        {
            var result = Validate(
                "id,value\n1",
                Schema(Column("id", true), Column("value", true)));

            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0].ErrorCode,
                Is.EqualTo(CsvHeaderFieldErrorCode.FieldCountMismatch));
            Assert.That(result.Errors[0].ExpectedValue, Is.EqualTo("2"));
            Assert.That(result.Errors[0].ActualValue, Is.EqualTo("1"));
            AssertLocation(result.Errors[0].Location, 10, 2, 2, 2, 1);
        }

        [Test]
        public void TooManyFieldsReportsFirstExtraFieldLocation()
        {
            var result = Validate(
                "id,value\n1,ok,extra",
                Schema(Column("id", true), Column("value", true)));

            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0].ErrorCode,
                Is.EqualTo(CsvHeaderFieldErrorCode.FieldCountMismatch));
            Assert.That(result.Errors[0].ExpectedValue, Is.EqualTo("2"));
            Assert.That(result.Errors[0].ActualValue, Is.EqualTo("3"));
            AssertLocation(result.Errors[0].Location, 14, 2, 6, 2, 3);
        }

        [Test]
        public void CountMismatchRowIsSkippedWhileLaterRowsAreInspected()
        {
            var result = Validate(
                "id,value\n1\n2,",
                Schema(Column("id", true), Column("value", true)));

            Assert.That(result.Errors.Select(error => error.ErrorCode),
                Is.EqualTo(new[]
                {
                    CsvHeaderFieldErrorCode.FieldCountMismatch,
                    CsvHeaderFieldErrorCode.RequiredFieldEmpty,
                }));
            Assert.That(result.Errors.Select(error => error.RecordNumber),
                Is.EqualTo(new[] { 2, 3 }));
        }

        [Test]
        public void RequiredNonEmptyRawValueSucceeds()
        {
            var result = Validate(
                "id,value\n1,present",
                Schema(Column("id", true), Column("value", true)));

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.Records[0].Fields[1].EffectiveValue, Is.EqualTo("present"));
            Assert.That(result.Records[0].Fields[1].UsedDefault, Is.False);
        }

        [Test]
        public void RequiredEmptyWithoutDefaultReportsFieldStart()
        {
            var result = Validate(
                "id,value\n1,",
                Schema(Column("id", true), Column("value", true)));

            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0].ErrorCode,
                Is.EqualTo(CsvHeaderFieldErrorCode.RequiredFieldEmpty));
            AssertLocation(result.Errors[0].Location, 11, 2, 3, 2, 2);
        }

        [Test]
        public void RequiredEmptyWithDefaultUsesRawSchemaDefault()
        {
            var result = Validate(
                "id,value\n1,",
                Schema(Column("id", true), Column("value", true, " fallback ")));

            Assert.That(result.Success, Is.True, FormatErrors(result));
            var field = result.Records[0].Fields[1];
            Assert.That(field.RawValue, Is.Empty);
            Assert.That(field.EffectiveValue, Is.EqualTo(" fallback "));
            Assert.That(field.UsedDefault, Is.True);
        }

        [Test]
        public void OptionalEmptyWithoutDefaultRemainsEmpty()
        {
            var result = Validate(
                "id,value\n1,",
                Schema(Column("id", true), Column("value", false)));

            Assert.That(result.Success, Is.True, FormatErrors(result));
            var field = result.Records[0].Fields[1];
            Assert.That(field.RawValue, Is.Empty);
            Assert.That(field.EffectiveValue, Is.Empty);
            Assert.That(field.UsedDefault, Is.False);
        }

        [Test]
        public void OptionalEmptyWithDefaultUsesDefault()
        {
            var result = Validate(
                "id,value\n1,",
                Schema(Column("id", true), Column("value", false, "fallback")));

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.Records[0].Fields[1].EffectiveValue, Is.EqualTo("fallback"));
            Assert.That(result.Records[0].Fields[1].UsedDefault, Is.True);
        }

        [Test]
        public void NonEmptyRawValueOverridesDefault()
        {
            var result = Validate(
                "id,value\n1,raw",
                Schema(Column("id", true), Column("value", false, "fallback")));

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.Records[0].Fields[1].RawValue, Is.EqualTo("raw"));
            Assert.That(result.Records[0].Fields[1].EffectiveValue, Is.EqualTo("raw"));
            Assert.That(result.Records[0].Fields[1].UsedDefault, Is.False);
        }

        [Test]
        public void WhitespaceIsNotTrimmedOrTreatedAsEmpty()
        {
            var result = Validate(
                "id,value\n1,   ",
                Schema(Column("id", true), Column("value", true, "fallback")));

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.Records[0].Fields[1].RawValue, Is.EqualTo("   "));
            Assert.That(result.Records[0].Fields[1].EffectiveValue, Is.EqualTo("   "));
            Assert.That(result.Records[0].Fields[1].UsedDefault, Is.False);
        }

        [Test]
        public void QuotedCommaAndMultilineValueRemainOneField()
        {
            var result = Validate(
                "id,value\n1,\"hello,\nmoon\"",
                Schema(Column("id", true), Column("value", true)));

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.Records[0].Fields.Count, Is.EqualTo(2));
            Assert.That(result.Records[0].Fields[1].RawValue, Is.EqualTo("hello,\nmoon"));
            Assert.That(result.Records[0].Fields[1].SourceField.WasQuoted, Is.True);
        }

        [Test]
        public void MultipleRowErrorsUseStableRecordAndFieldOrder()
        {
            var schema = Schema(
                Column("id", true),
                Column("a", true),
                Column("b", true));
            var first = Validate("id,a,b\n1,,\n2,,x", schema);
            var second = Validate("id,a,b\n1,,\n2,,x", schema);

            Assert.That(first.Errors.Select(error =>
                    error.RecordNumber + ":" + error.FieldNumber),
                Is.EqualTo(new[] { "2:2", "2:3", "3:2" }));
            Assert.That(first.Errors.Select(error => error.ToString()),
                Is.EqualTo(second.Errors.Select(error => error.ToString())));
        }

        [Test]
        public void SyntaxReaderFailureBecomesSingleSyntaxReadFailedError()
        {
            var readResult = Read("id,value\n\"unterminated", "reader-source.csv");
            Assert.That(readResult.Success, Is.False);

            var result = new CsvHeaderAndFieldValidator().Validate(
                readResult,
                Schema(Column("id", true), Column("value", true)),
                "fallback.csv");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Records, Is.Empty);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0].ErrorCode,
                Is.EqualTo(CsvHeaderFieldErrorCode.SyntaxReadFailed));
            Assert.That(result.Errors[0].SourceName, Is.EqualTo("reader-source.csv"));
            Assert.That(result.Errors[0].Location,
                Is.EqualTo(readResult.Errors[0].Location));
            Assert.That(result.Errors[0].ActualValue,
                Is.EqualTo(readResult.Errors[0].Code.ToString()));
        }

        [Test]
        public void AnyErrorPreventsAllValidatedRecordPublication()
        {
            var result = Validate(
                "id,value\n1,valid\n2,",
                Schema(Column("id", true), Column("value", true)));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Records, Is.Empty);
        }

        [Test]
        public void SuccessfulAndFailedCollectionsAreReadOnly()
        {
            var schema = Schema(Column("id", true), Column("value", true));
            var success = Validate("id,value\n1,ok", schema);
            var failure = Validate("id,value\n1,", schema);

            Assert.Throws<NotSupportedException>(() =>
                ((IList<CsvValidatedRecord>)success.Records).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<CsvValidatedField>)success.Records[0].Fields).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<CsvHeaderFieldError>)failure.Errors).Clear());
        }

        [Test]
        public void InputBytesReaderModelsAndSchemaModelsRemainUnchanged()
        {
            var bytes = new UTF8Encoding(false, true).GetBytes("id,value\n1,raw");
            var before = bytes.ToArray();
            var readResult = new Rfc4180CsvReader().Read(bytes, "source.csv");
            var schema = Schema(Column("id", true), Column("value", false, "fallback"));
            var sourceRecord = readResult.Records[1];
            var sourceField = sourceRecord.Fields[1];
            var schemaColumn = schema.Columns[1];

            var result = new CsvHeaderAndFieldValidator().Validate(
                readResult,
                schema,
                "source.csv");

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(bytes, Is.EqualTo(before));
            Assert.That(readResult.Records[1], Is.SameAs(sourceRecord));
            Assert.That(result.Records[0].SourceRecord, Is.SameAs(sourceRecord));
            Assert.That(result.Records[0].Fields[1].SourceField, Is.SameAs(sourceField));
            Assert.That(result.Records[0].Fields[1].Schema, Is.SameAs(schemaColumn));
            Assert.That(schema.Columns[1].DefaultValue, Is.EqualTo("fallback"));
        }

        [Test]
        public void ErrorsExposeCompleteSourceSchemaAndPositionContext()
        {
            var result = new CsvHeaderAndFieldValidator().Validate(
                Read("id,value\n1,", "folder/input.csv"),
                Schema(Column("id", true), Column("value", true)),
                "folder/input.csv");
            var error = result.Errors.Single();

            Assert.That(error.SourceName, Is.EqualTo("folder/input.csv"));
            Assert.That(error.SchemaFileName, Is.EqualTo("schema.csv"));
            Assert.That(error.ErrorCode,
                Is.EqualTo(CsvHeaderFieldErrorCode.RequiredFieldEmpty));
            Assert.That(error.Message, Is.Not.Empty);
            Assert.That(error.RecordNumber, Is.EqualTo(2));
            Assert.That(error.FieldNumber, Is.EqualTo(2));
            Assert.That(error.PhysicalLine, Is.EqualTo(2));
            Assert.That(error.PhysicalColumn, Is.EqualTo(3));
            Assert.That(error.CharOffset, Is.EqualTo(11));
            Assert.That(error.ExpectedValue, Is.EqualTo("non-empty"));
            Assert.That(error.ActualValue, Is.Empty);
        }

        [Test]
        public void NullArgumentsThrowExplicitArgumentExceptions()
        {
            var validator = new CsvHeaderAndFieldValidator();
            var schema = Schema(Column("id", true));
            var readResult = Read("id", "source.csv");

            Assert.Throws<ArgumentNullException>(() => validator.Validate(null, schema));
            Assert.Throws<ArgumentNullException>(() => validator.Validate(readResult, null));
            Assert.Throws<ArgumentNullException>(() =>
                validator.Validate(readResult, schema, null));
        }

        private static CsvHeaderFieldValidationResult Validate(
            string text,
            CsvFileSchema schema)
        {
            return new CsvHeaderAndFieldValidator().Validate(
                Read(text, "source.csv"),
                schema,
                "source.csv");
        }

        private static CsvReadResult Read(string text, string sourceName)
        {
            return new Rfc4180CsvReader().Read(
                new UTF8Encoding(false, true).GetBytes(text),
                sourceName);
        }

        private static CsvFileSchema Schema(params ColumnDefinition[] columns)
        {
            var rows = columns.Select((column, index) => new CsvSchemaDictionaryRow(
                "schema.csv",
                (index + 1).ToString(CultureInfo.InvariantCulture),
                column.Name,
                "STRING",
                column.IsRequired ? "1" : "0",
                index == 0 ? "1" : string.Empty,
                column.DefaultValue,
                string.Empty,
                string.Empty,
                string.Empty,
                index + 2));
            var built = new CsvSchemaCatalogBuilder().Build(rows);
            Assert.That(built.Success, Is.True,
                string.Join("\n", built.Errors.Select(error => error.ToString())));
            return built.Catalog.GetFile("schema.csv");
        }

        private static ColumnDefinition Column(
            string name,
            bool isRequired,
            string defaultValue = "")
        {
            return new ColumnDefinition(name, isRequired, defaultValue);
        }

        private static void AssertLocation(
            CsvSourceLocation location,
            int charOffset,
            int physicalLine,
            int physicalColumn,
            int recordNumber,
            int fieldNumber)
        {
            Assert.That(location.CharOffset, Is.EqualTo(charOffset));
            Assert.That(location.PhysicalLine, Is.EqualTo(physicalLine));
            Assert.That(location.PhysicalColumn, Is.EqualTo(physicalColumn));
            Assert.That(location.RecordNumber, Is.EqualTo(recordNumber));
            Assert.That(location.FieldNumber, Is.EqualTo(fieldNumber));
        }

        private static string FormatErrors(CsvHeaderFieldValidationResult result)
        {
            return string.Join("\n", result.Errors.Select(error => error.ToString()));
        }

        private sealed class ColumnDefinition
        {
            public ColumnDefinition(string name, bool isRequired, string defaultValue)
            {
                Name = name;
                IsRequired = isRequired;
                DefaultValue = defaultValue;
            }

            public string Name { get; }

            public bool IsRequired { get; }

            public string DefaultValue { get; }
        }
    }
}
