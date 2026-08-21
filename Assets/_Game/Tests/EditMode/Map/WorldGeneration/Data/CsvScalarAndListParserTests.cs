using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;

namespace StarNight.Map.Tests.WorldGeneration.Data
{
    public sealed class CsvScalarAndListParserTests
    {
        [TestCase("text")]
        [TestCase("")]
        [TestCase("  text  ")]
        public void StringPreservesEffectiveValueExactly(string input)
        {
            var field = ParseValue("STRING", input);

            Assert.That(field.Value.StringValue, Is.EqualTo(input));
            Assert.That(field.Value.IsEmpty, Is.EqualTo(input.Length == 0));
        }

        [TestCase("A")]
        [TestCase("ABC_123")]
        [TestCase("0")]
        [TestCase("")]
        public void IdAcceptsExactAsciiGrammarAndOptionalEmpty(string input)
        {
            var field = ParseValue("ID", input);

            Assert.That(field.Value.StringValue, Is.EqualTo(input));
            Assert.That(field.Value.IsEmpty, Is.EqualTo(input.Length == 0));
        }

        [TestCase("lower")]
        [TestCase("HAS-HYPHEN")]
        [TestCase(" SPACE")]
        public void IdRejectsNonGrammarText(string input)
        {
            AssertError("ID", input, CsvValueParseErrorCode.InvalidId);
        }

        [TestCase("0", 0)]
        [TestCase("42", 42)]
        [TestCase("-42", -42)]
        [TestCase("+42", 42)]
        [TestCase("2147483647", int.MaxValue)]
        [TestCase("-2147483648", int.MinValue)]
        public void IntAcceptsInvariantSignedDecimal(string input, int expected)
        {
            Assert.That(ParseValue("INT", input).Value.IntValue, Is.EqualTo(expected));
        }

        [Test]
        public void EmptyIntIsSuccessfulTypedEmpty()
        {
            var value = ParseValue("INT", string.Empty).Value;

            Assert.That(value.IsEmpty, Is.True);
            Assert.That(value.IntValue, Is.Zero);
        }

        [TestCase("2147483648")]
        [TestCase("-2147483649")]
        [TestCase(" 1")]
        [TestCase("1.0")]
        [TestCase("1,000")]
        public void IntRejectsOverflowWhitespaceDecimalAndThousands(string input)
        {
            AssertError("INT", input, CsvValueParseErrorCode.InvalidInteger);
        }

        [Test]
        public void ULongAcceptsZeroAndMaximum()
        {
            Assert.That(ParseValue("ULONG", "0").Value.ULongValue, Is.Zero);
            Assert.That(
                ParseValue("ULONG", ulong.MaxValue.ToString(CultureInfo.InvariantCulture))
                    .Value.ULongValue,
                Is.EqualTo(ulong.MaxValue));
        }

        [TestCase("-1")]
        [TestCase("+1")]
        [TestCase(" 1")]
        [TestCase("18446744073709551616")]
        public void ULongRejectsSignsWhitespaceAndOverflow(string input)
        {
            AssertError("ULONG", input, CsvValueParseErrorCode.InvalidUnsignedInteger);
        }

        [TestCase("1", 1f)]
        [TestCase("1.5", 1.5f)]
        [TestCase("-2.25", -2.25f)]
        [TestCase("1e3", 1000f)]
        [TestCase("+5E-1", 0.5f)]
        public void FloatAcceptsFiniteInvariantForms(string input, float expected)
        {
            Assert.That(ParseValue("FLOAT", input).Value.FloatValue, Is.EqualTo(expected));
        }

        [TestCase("1,5")]
        [TestCase(" 1.5")]
        [TestCase("NaN")]
        [TestCase("Infinity")]
        [TestCase("-Infinity")]
        [TestCase("1e1000")]
        public void FloatRejectsLocaleWhitespaceNonFiniteAndOverflow(string input)
        {
            AssertError("FLOAT", input, CsvValueParseErrorCode.InvalidFloat);
        }

        [TestCase("0", false)]
        [TestCase("1", true)]
        public void BoolAcceptsOnlyExactNumericTokens(string input, bool expected)
        {
            Assert.That(ParseValue("BOOL", input).Value.BoolValue, Is.EqualTo(expected));
        }

        [TestCase("true")]
        [TestCase("False")]
        [TestCase("01")]
        public void BoolRejectsWordsAndVariants(string input)
        {
            AssertError("BOOL", input, CsvValueParseErrorCode.InvalidBoolean);
        }

        [TestCase("A")]
        [TestCase("B")]
        [TestCase("")]
        public void EnumAcceptsOrdinalAllowedValueAndOptionalEmpty(string input)
        {
            var value = ParseValue("ENUM", input, "A|B").Value;

            Assert.That(value.StringValue, Is.EqualTo(input));
            Assert.That(value.IsEmpty, Is.EqualTo(input.Length == 0));
        }

        [TestCase("a")]
        [TestCase("C")]
        public void EnumRejectsCaseMismatchAndUnknownValue(string input)
        {
            var result = ParseResult("ENUM", input, "A|B");

            Assert.That(result.Errors.Single().ErrorCode,
                Is.EqualTo(CsvValueParseErrorCode.InvalidEnum));
            Assert.That(result.Errors.Single().AllowedValues, Is.EqualTo(new[] { "A", "B" }));
        }

        [Test]
        public void EmptyVocabularyEnumAcceptsExactNonEmptyToken()
        {
            var value = ParseValue("ENUM", "Mixed-token_7", string.Empty).Value;

            Assert.That(value.StringValue, Is.EqualTo("Mixed-token_7"));
            Assert.That(value.IsEmpty, Is.False);
        }

        [Test]
        public void EmptyVocabularyEnumAcceptsOptionalEmptyAsTypedEmpty()
        {
            var value = ParseValue("ENUM", string.Empty, string.Empty).Value;

            Assert.That(value.DataType, Is.EqualTo(CsvSchemaDataType.Enum));
            Assert.That(value.IsEmpty, Is.True);
        }

        [Test]
        public void EmptyVocabularyRequiredEnumAcceptsNonEmptyToken()
        {
            var schema = Schema(new[]
            {
                Definition("id", "STRING", primaryKeyOrder: "1", required: "1"),
                Definition("value", "ENUM", allowedValues: string.Empty, required: "1"),
            });

            var value = Parse(Context(schema, "id,value\nROW_1,ExactToken")).Records
                .Single().Fields[1].Value;

            Assert.That(value.StringValue, Is.EqualTo("ExactToken"));
        }

        [Test]
        public void EmptyVocabularyRequiredEnumStillRejectsMissingValueDuringValidation()
        {
            var schema = Schema(new[]
            {
                Definition("id", "STRING", primaryKeyOrder: "1", required: "1"),
                Definition("value", "ENUM", allowedValues: string.Empty, required: "1"),
            });

            var validation = Validate(schema, "id,value\nROW_1,");

            Assert.That(validation.Success, Is.False);
            Assert.That(validation.Errors, Is.Not.Empty);
        }

        [Test]
        public void EmptyVocabularyEnumListAcceptsTokensWithExistingTrimSemantics()
        {
            var value = ParseValue(
                "ENUM_LIST",
                " Alpha |beta-case|123 ",
                string.Empty).Value;

            Assert.That(value.EnumListValue, Is.EqualTo(new[] { "Alpha", "beta-case", "123" }));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<string>)value.EnumListValue).Add("extra"));
        }

        [Test]
        public void EmptyVocabularyEnumListAcceptsOptionalEmptyAsTypedEmpty()
        {
            var value = ParseValue("ENUM_LIST", string.Empty, string.Empty).Value;

            Assert.That(value.DataType, Is.EqualTo(CsvSchemaDataType.EnumList));
            Assert.That(value.IsEmpty, Is.True);
        }

        [Test]
        public void EmptyVocabularyEnumListStillRejectsEmptyElement()
        {
            var result = ParseResult("ENUM_LIST", "A||B", string.Empty);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Errors.Single().ErrorCode,
                Is.EqualTo(CsvValueParseErrorCode.EmptyListItem));
            Assert.That(result.Errors.Single().ListItemIndex, Is.EqualTo(1));
        }

        [TestCase("ENUM", "UNKNOWN", CsvValueParseErrorCode.InvalidEnum)]
        [TestCase("ENUM_LIST", "A|UNKNOWN", CsvValueParseErrorCode.InvalidListItem)]
        public void NonEmptyVocabularyStillRejectsUnknownToken(
            string dataType,
            string input,
            CsvValueParseErrorCode expectedCode)
        {
            var result = ParseResult(dataType, input, "A|B");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Errors.Single().ErrorCode, Is.EqualTo(expectedCode));
        }

        [TestCase("AF", new byte[] { 0xAF })]
        [TestCase("af", new byte[] { 0xAF })]
        [TestCase("0x10Af", new byte[] { 0x10, 0xAF })]
        [TestCase("F", new byte[] { 0x0F })]
        public void HexAcceptsCasePrefixAndOddDigitCount(string input, byte[] expected)
        {
            var hex = ParseValue("HEX", input).Value.HexValue;

            Assert.That(hex.OriginalValue, Is.EqualTo(input));
            Assert.That(hex.Bytes, Is.EqualTo(expected));
        }

        [TestCase("0x")]
        [TestCase("GG")]
        [TestCase("+1")]
        [TestCase("A_B")]
        [TestCase(" A")]
        public void HexRejectsMissingDigitsInvalidDigitsSignsSeparatorsAndWhitespace(string input)
        {
            AssertError("HEX", input, CsvValueParseErrorCode.InvalidHex);
        }

        [Test]
        public void HexBytesAreReadOnlyAndCopied()
        {
            var bytes = ParseValue("HEX", "A1").Value.HexValue.Bytes;

            Assert.Throws<NotSupportedException>(() => ((IList<byte>)bytes).Add(0xFF));
            Assert.That(bytes, Is.EqualTo(new byte[] { 0xA1 }));
        }

        [TestCase("2026-08-11T12:34:56Z")]
        [TestCase("2026-08-11T12:34:56.1234567Z")]
        public void DateTimeAcceptsExactUtcWholeAndFractionalSeconds(string input)
        {
            var value = ParseValue("DATETIME", input).Value.DateTimeValue;

            Assert.That(value.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(value.Year, Is.EqualTo(2026));
        }

        [TestCase("2026-02-30T00:00:00Z")]
        [TestCase("2026-08-11T12:34:56+00:00")]
        [TestCase("2026-08-11T12:34:56")]
        [TestCase(" 2026-08-11T12:34:56Z")]
        [TestCase("2026-08-11 12:34:56Z")]
        public void DateTimeRejectsInvalidDateOffsetLocalWhitespaceAndSeparator(string input)
        {
            AssertError("DATETIME", input, CsvValueParseErrorCode.InvalidDateTime);
        }

        [Test]
        public void EmptyIdListPublishesReadOnlyEmptyList()
        {
            var value = ParseValue("ID_LIST", string.Empty).Value;

            Assert.That(value.IsEmpty, Is.True);
            Assert.That(value.IdListValue, Is.Empty);
            Assert.Throws<NotSupportedException>(() =>
                ((IList<string>)value.IdListValue).Add("A"));
        }

        [TestCase("A", new[] { "A" })]
        [TestCase("A|B|A", new[] { "A", "B", "A" })]
        [TestCase(" A | B ", new[] { "A", "B" })]
        public void IdListTrimsComponentsAndPreservesOrderAndDuplicates(
            string input,
            string[] expected)
        {
            Assert.That(ParseValue("ID_LIST", input).Value.IdListValue, Is.EqualTo(expected));
        }

        [Test]
        public void InvalidIdListItemReportsTrimmedIndexAndValue()
        {
            var error = ParseResult("ID_LIST", "GOOD| bad ").Errors.Single();

            Assert.That(error.ErrorCode, Is.EqualTo(CsvValueParseErrorCode.InvalidListItem));
            Assert.That(error.ListItemIndex, Is.EqualTo(1));
            Assert.That(error.ListItemValue, Is.EqualTo("bad"));
        }

        [Test]
        public void EnumListUsesOrdinalAllowedValuesAndPreservesDuplicates()
        {
            var value = ParseValue("ENUM_LIST", " A |B|A ", "A|B").Value;

            Assert.That(value.EnumListValue, Is.EqualTo(new[] { "A", "B", "A" }));
        }

        [Test]
        public void InvalidEnumListItemPublishesAllowedValues()
        {
            var error = ParseResult("ENUM_LIST", "A|b", "A|B").Errors.Single();

            Assert.That(error.ErrorCode, Is.EqualTo(CsvValueParseErrorCode.InvalidListItem));
            Assert.That(error.ListItemIndex, Is.EqualTo(1));
            Assert.That(error.ListItemValue, Is.EqualTo("b"));
            Assert.That(error.AllowedValues, Is.EqualTo(new[] { "A", "B" }));
        }

        [TestCase("", new int[0])]
        [TestCase("-1|+2|3", new[] { -1, 2, 3 })]
        [TestCase(" 1 | -2 ", new[] { 1, -2 })]
        public void IntListParsesEmptySignedAndTrimmedComponents(string input, int[] expected)
        {
            var value = ParseValue("INT_LIST", input).Value;

            Assert.That(value.IntListValue, Is.EqualTo(expected));
            Assert.That(value.IsEmpty, Is.EqualTo(input.Length == 0));
        }

        [Test]
        public void IntListOverflowReportsInvalidItem()
        {
            var error = ParseResult("INT_LIST", "1|2147483648").Errors.Single();

            Assert.That(error.ErrorCode, Is.EqualTo(CsvValueParseErrorCode.InvalidListItem));
            Assert.That(error.ListItemIndex, Is.EqualTo(1));
        }

        [TestCase("|A", 0)]
        [TestCase("A|", 1)]
        [TestCase("A||B", 1)]
        [TestCase("A|   |B", 1)]
        public void EmptyListComponentsAreNeverDropped(string input, int expectedIndex)
        {
            var error = ParseResult("ID_LIST", input).Errors.Single();

            Assert.That(error.ErrorCode, Is.EqualTo(CsvValueParseErrorCode.EmptyListItem));
            Assert.That(error.ListItemIndex, Is.EqualTo(expectedIndex));
            Assert.That(error.ListItemValue, Is.Empty);
        }

        [Test]
        public void DefaultEffectiveValueIsParsedAndFieldProvenanceIsPreserved()
        {
            var field = ParseValue("INT", string.Empty, string.Empty, "+7");

            Assert.That(field.RawValue, Is.Empty);
            Assert.That(field.EffectiveValue, Is.EqualTo("+7"));
            Assert.That(field.UsedDefault, Is.True);
            Assert.That(field.Value.IntValue, Is.EqualTo(7));
            Assert.That(field.ValidatedField, Is.Not.Null);
        }

        [Test]
        public void NonEmptyRawValueWinsOverDefault()
        {
            var field = ParseValue("INT", "8", string.Empty, "7");

            Assert.That(field.RawValue, Is.EqualTo("8"));
            Assert.That(field.EffectiveValue, Is.EqualTo("8"));
            Assert.That(field.UsedDefault, Is.False);
            Assert.That(field.Value.IntValue, Is.EqualTo(8));
        }

        [Test]
        public void ParseErrorsAccumulateAcrossFieldsAndRecordsInDeterministicOrder()
        {
            var schema = Schema(new[]
            {
                Definition("id", "STRING", primaryKeyOrder: "1", required: "1"),
                Definition("number", "INT"),
                Definition("code", "ID"),
            });
            var context = Context(schema, "id,number,code\nR1,x,bad\nR2,2147483648,also-bad");

            var result = Parse(context);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Records, Is.Empty);
            Assert.That(result.Errors.Select(error => error.RecordNumber),
                Is.EqualTo(new[] { 2, 2, 3, 3 }));
            Assert.That(result.Errors.Select(error => error.ColumnName),
                Is.EqualTo(new[] { "number", "code", "number", "code" }));
        }

        [Test]
        public void MultipleListErrorsUseAscendingItemIndex()
        {
            var result = ParseResult("ID_LIST", "bad||also-bad");

            Assert.That(result.Errors.Select(error => error.ListItemIndex),
                Is.EqualTo(new int?[] { 0, 1, 2 }));
            Assert.That(result.Errors.Select(error => error.ErrorCode), Is.EqualTo(new[]
            {
                CsvValueParseErrorCode.InvalidListItem,
                CsvValueParseErrorCode.EmptyListItem,
                CsvValueParseErrorCode.InvalidListItem,
            }));
        }

        [Test]
        public void ParseErrorCarriesExactFieldStartAndContext()
        {
            var context = Context("INT", "bad", string.Empty);
            var validatedField = context.Validation.Records.Single().Fields[1];

            var error = Parse(context).Errors.Single();

            Assert.That(error.SourceName, Is.EqualTo("source.csv"));
            Assert.That(error.SchemaFileName, Is.EqualTo("schema.csv"));
            Assert.That(error.ColumnName, Is.EqualTo("value"));
            Assert.That(error.DataType, Is.EqualTo(CsvSchemaDataType.Int));
            Assert.That(error.Location, Is.EqualTo(validatedField.SourceField.StartLocation));
            Assert.That(error.EffectiveValue, Is.EqualTo("bad"));
            Assert.That(error.ListItemIndex, Is.Null);
        }

        [Test]
        public void SuccessfulResultPublishesSchemaOrderAndExactSourceReferences()
        {
            var schema = Schema(new[]
            {
                Definition("id", "STRING", primaryKeyOrder: "1", required: "1"),
                Definition("number", "INT"),
                Definition("flag", "BOOL"),
            });
            var context = Context(schema, "id,number,flag\nROW,7,1");

            var result = Parse(context);
            var record = result.Records.Single();

            Assert.That(result.Success, Is.True);
            Assert.That(record.Fields.Select(field => field.Schema.ColumnName),
                Is.EqualTo(new[] { "id", "number", "flag" }));
            Assert.That(record.ValidatedRecord, Is.SameAs(context.Validation.Records.Single()));
            Assert.That(record.SourceRecord, Is.SameAs(record.ValidatedRecord.SourceRecord));
            Assert.That(record.Fields[1].ValidatedField,
                Is.SameAs(record.ValidatedRecord.Fields[1]));
        }

        [Test]
        public void ResultRecordFieldAndListCollectionsAreReadOnly()
        {
            var result = ParseResult("ID_LIST", "A|B");

            Assert.Throws<NotSupportedException>(() =>
                ((IList<CsvParsedRecord>)result.Records).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<CsvParsedField>)result.Records[0].Fields).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<string>)result.Records[0].Fields[1].Value.IdListValue).Clear());
        }

        [Test]
        public void WrongTypedAccessorThrowsInvalidOperationException()
        {
            var value = ParseValue("INT", "1").Value;

            Assert.Throws<InvalidOperationException>(() => _ = value.StringValue);
            Assert.Throws<InvalidOperationException>(() => _ = value.HexValue);
        }

        [Test]
        public void HeaderOnlySuccessfulInputsPublishEmptySuccess()
        {
            var schema = Schema(new[]
            {
                Definition("id", "STRING", primaryKeyOrder: "1", required: "1"),
            });
            var context = Context(schema, "id");

            var result = Parse(context);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Records, Is.Empty);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void NullInputsAreRejectedExplicitly()
        {
            var context = Context("STRING", "value", string.Empty);
            var parser = new CsvScalarAndListParser();

            Assert.Throws<ArgumentNullException>(() =>
                parser.Parse(null, context.Validation, context.PrimaryKeys));
            Assert.Throws<ArgumentNullException>(() =>
                parser.Parse(context.Schema, null, context.PrimaryKeys));
            Assert.Throws<ArgumentNullException>(() =>
                parser.Parse(context.Schema, context.Validation, null));
            Assert.Throws<ArgumentNullException>(() =>
                parser.Parse(context.Schema, context.Validation, context.PrimaryKeys, null));
        }

        [Test]
        public void UnsuccessfulValidationGateIsRejectedWithoutOutput()
        {
            var context = Context("STRING", "value", string.Empty);
            var failedValidation = new CsvHeaderAndFieldValidator().Validate(
                Read("wrong,value\nROW,text"),
                context.Schema,
                "source.csv");

            Assert.That(failedValidation.Success, Is.False);
            Assert.Throws<InvalidOperationException>(() =>
                new CsvScalarAndListParser().Parse(
                    context.Schema,
                    failedValidation,
                    context.PrimaryKeys));
        }

        [Test]
        public void DuplicatePrimaryKeyGateIsRejectedWithoutOutput()
        {
            var schema = Schema(new[]
            {
                Definition("id", "STRING", primaryKeyOrder: "1", required: "1"),
                Definition("value", "STRING"),
            });
            var validation = Validate(schema, "id,value\nSAME,a\nSAME,b");
            var duplicateKeys = new CsvPrimaryKeyIndexBuilder().Build(
                schema,
                validation,
                "source.csv");

            Assert.That(duplicateKeys.Success, Is.False);
            Assert.Throws<InvalidOperationException>(() =>
                new CsvScalarAndListParser().Parse(schema, validation, duplicateKeys));
        }

        [Test]
        public void SeparatelyBuiltSchemaIsRejectedEvenWhenTextMatches()
        {
            var context = Context("STRING", "value", string.Empty);
            var otherSchema = Schema(new[]
            {
                Definition("id", "STRING", primaryKeyOrder: "1", required: "1"),
                Definition("value", "STRING"),
            });

            Assert.Throws<InvalidOperationException>(() =>
                new CsvScalarAndListParser().Parse(
                    otherSchema,
                    context.Validation,
                    context.PrimaryKeys));
        }

        [Test]
        public void PrimaryKeyResultFromOtherValidationIsRejected()
        {
            var first = Context("STRING", "one", string.Empty);
            var second = Context("STRING", "two", string.Empty);

            Assert.Throws<InvalidOperationException>(() =>
                new CsvScalarAndListParser().Parse(
                    first.Schema,
                    first.Validation,
                    second.PrimaryKeys));
        }

        [Test]
        public void ParseDoesNotModifySchemaValidationOrPrimaryKeyModels()
        {
            var context = Context("INT_LIST", "1|2|1", string.Empty);
            var columnsBefore = context.Schema.Columns.ToArray();
            var recordsBefore = context.Validation.Records.ToArray();
            var fieldsBefore = context.Validation.Records[0].Fields.ToArray();
            var entriesBefore = context.PrimaryKeys.Index.Entries.ToArray();

            var result = Parse(context);

            Assert.That(result.Success, Is.True);
            Assert.That(context.Schema.Columns, Is.EqualTo(columnsBefore));
            Assert.That(context.Validation.Records, Is.EqualTo(recordsBefore));
            Assert.That(context.Validation.Records[0].Fields, Is.EqualTo(fieldsBefore));
            Assert.That(context.PrimaryKeys.Index.Entries, Is.EqualTo(entriesBefore));
        }

        private static CsvParsedField ParseValue(
            string dataType,
            string value,
            string allowedValues = "",
            string defaultValue = "")
        {
            var result = ParseResult(dataType, value, allowedValues, defaultValue);
            Assert.That(result.Success, Is.True, FormatErrors(result));
            return result.Records.Single().Fields[1];
        }

        private static void AssertError(
            string dataType,
            string value,
            CsvValueParseErrorCode expectedCode)
        {
            var result = ParseResult(dataType, value);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Records, Is.Empty);
            Assert.That(result.Errors.Single().ErrorCode, Is.EqualTo(expectedCode));
        }

        private static CsvScalarAndListParseResult ParseResult(
            string dataType,
            string value,
            string allowedValues = "",
            string defaultValue = "")
        {
            return Parse(Context(dataType, value, allowedValues, defaultValue));
        }

        private static ParseContext Context(
            string dataType,
            string value,
            string allowedValues,
            string defaultValue = "")
        {
            var schema = Schema(new[]
            {
                Definition("id", "STRING", primaryKeyOrder: "1", required: "1"),
                Definition("value", dataType, defaultValue, allowedValues),
            });
            return Context(schema, "id,value\nROW_1," + CsvCell(value));
        }

        private static ParseContext Context(CsvFileSchema schema, string csvText)
        {
            var validation = Validate(schema, csvText);
            Assert.That(validation.Success, Is.True, string.Join(
                "\n",
                validation.Errors.Select(error => error.ToString())));
            var primaryKeys = new CsvPrimaryKeyIndexBuilder().Build(
                schema,
                validation,
                "source.csv");
            Assert.That(primaryKeys.Success, Is.True);
            return new ParseContext(schema, validation, primaryKeys);
        }

        private static CsvScalarAndListParseResult Parse(ParseContext context)
        {
            return new CsvScalarAndListParser().Parse(
                context.Schema,
                context.Validation,
                context.PrimaryKeys);
        }

        private static CsvHeaderFieldValidationResult Validate(
            CsvFileSchema schema,
            string csvText)
        {
            return new CsvHeaderAndFieldValidator().Validate(
                Read(csvText),
                schema,
                "source.csv");
        }

        private static CsvReadResult Read(string csvText)
        {
            return new Rfc4180CsvReader().Read(
                new UTF8Encoding(false, true).GetBytes(csvText),
                "source.csv");
        }

        private static CsvFileSchema Schema(IEnumerable<ColumnDefinition> definitions)
        {
            var rows = definitions.Select((definition, index) => new CsvSchemaDictionaryRow(
                "schema.csv",
                (index + 1).ToString(CultureInfo.InvariantCulture),
                definition.Name,
                definition.DataType,
                definition.Required,
                definition.PrimaryKeyOrder,
                definition.DefaultValue,
                definition.AllowedValues,
                string.Empty,
                string.Empty,
                index + 2));
            var built = new CsvSchemaCatalogBuilder().Build(rows);
            Assert.That(built.Success, Is.True, string.Join(
                "\n",
                built.Errors.Select(error => error.ToString())));
            return built.Catalog.GetFile("schema.csv");
        }

        private static ColumnDefinition Definition(
            string name,
            string dataType,
            string defaultValue = "",
            string allowedValues = "",
            string primaryKeyOrder = "",
            string required = "0")
        {
            return new ColumnDefinition(
                name,
                dataType,
                defaultValue,
                allowedValues,
                primaryKeyOrder,
                required);
        }

        private static string CsvCell(string value)
        {
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
            {
                return value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string FormatErrors(CsvScalarAndListParseResult result)
        {
            return string.Join("\n", result.Errors.Select(error =>
                error.ErrorCode + ": " + error.Message));
        }

        private sealed class ParseContext
        {
            public ParseContext(
                CsvFileSchema schema,
                CsvHeaderFieldValidationResult validation,
                CsvPrimaryKeyIndexBuildResult primaryKeys)
            {
                Schema = schema;
                Validation = validation;
                PrimaryKeys = primaryKeys;
            }

            public CsvFileSchema Schema { get; }

            public CsvHeaderFieldValidationResult Validation { get; }

            public CsvPrimaryKeyIndexBuildResult PrimaryKeys { get; }
        }

        private sealed class ColumnDefinition
        {
            public ColumnDefinition(
                string name,
                string dataType,
                string defaultValue,
                string allowedValues,
                string primaryKeyOrder,
                string required)
            {
                Name = name;
                DataType = dataType;
                DefaultValue = defaultValue;
                AllowedValues = allowedValues;
                PrimaryKeyOrder = primaryKeyOrder;
                Required = required;
            }

            public string Name { get; }

            public string DataType { get; }

            public string DefaultValue { get; }

            public string AllowedValues { get; }

            public string PrimaryKeyOrder { get; }

            public string Required { get; }
        }
    }
}
