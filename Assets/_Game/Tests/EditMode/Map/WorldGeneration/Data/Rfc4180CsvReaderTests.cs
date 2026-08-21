using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;

namespace StarNight.Map.Tests.WorldGeneration.Data
{
    public sealed class Rfc4180CsvReaderTests
    {
        [Test]
        public void ReadsBasicUnquotedFields()
        {
            var result = Read("a,b,c");

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.Records.Count, Is.EqualTo(1));
            Assert.That(result.Records[0].Fields.Select(field => field.Value),
                Is.EqualTo(new[] { "a", "b", "c" }));
            Assert.That(result.Records[0].Fields.All(field => !field.WasQuoted), Is.True);
        }

        [Test]
        public void ReadsLeadingMiddleAndTrailingEmptyFields()
        {
            var result = Read(",a,,");

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.Records[0].Fields.Select(field => field.Value),
                Is.EqualTo(new[] { string.Empty, "a", string.Empty, string.Empty }));
            AssertLocation(result.Records[0].Fields[0].StartLocation, 0, 1, 1, 1, 1);
            AssertLocation(result.Records[0].Fields[3].StartLocation, 4, 1, 5, 1, 4);
            Assert.That(
                result.Records[0].Fields[3].EndLocationExclusive,
                Is.EqualTo(result.Records[0].Fields[3].StartLocation));
        }

        [Test]
        public void ReadsQuotedComma()
        {
            var result = Read("\"a,b\",c");

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.Records[0].Fields[0].Value, Is.EqualTo("a,b"));
            Assert.That(result.Records[0].Fields[0].WasQuoted, Is.True);
            Assert.That(result.Records[0].Fields[1].Value, Is.EqualTo("c"));
        }

        [Test]
        public void DecodesEscapedQuote()
        {
            var result = Read("\"say \"\"moon\"\"\"");

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.Records[0].Fields.Single().Value, Is.EqualTo("say \"moon\""));
        }

        [Test]
        public void PreservesQuotedCrLfMultilineValue()
        {
            var result = Read("\"first\r\nsecond\",tail");

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.Records[0].Fields[0].Value, Is.EqualTo("first\r\nsecond"));
            Assert.That(result.Records[0].Fields[1].Value, Is.EqualTo("tail"));
        }

        [Test]
        public void PreservesQuotedLfMultilineValue()
        {
            var result = Read("\"first\nsecond\",tail");

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.Records[0].Fields[0].Value, Is.EqualTo("first\nsecond"));
            Assert.That(result.Records[0].Fields[1].Value, Is.EqualTo("tail"));
        }

        [Test]
        public void ReadsCrLfRecordBoundaries()
        {
            var result = Read("a,b\r\nc,d");

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(Project(result), Is.EqualTo(new[] { "a|b", "c|d" }));
            AssertLocation(result.Records[1].StartLocation, 5, 2, 1, 2, 1);
        }

        [Test]
        public void ReadsLfRecordBoundaries()
        {
            var result = Read("a,b\nc,d");

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(Project(result), Is.EqualTo(new[] { "a|b", "c|d" }));
            AssertLocation(result.Records[1].StartLocation, 4, 2, 1, 2, 1);
        }

        [Test]
        public void ReadsMixedCrLfAndLfRecordBoundaries()
        {
            var result = Read("a\r\nb\nc\r\nd");

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(Project(result), Is.EqualTo(new[] { "a", "b", "c", "d" }));
            Assert.That(result.Records.Select(record => record.StartLocation.PhysicalLine),
                Is.EqualTo(new[] { 1, 2, 3, 4 }));
        }

        [Test]
        public void ReportsUtf8BomPresent()
        {
            var result = new Rfc4180CsvReader().Read(WithBom("a,b"), "bom.csv");

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.HadUtf8Bom, Is.True);
            AssertLocation(result.Records[0].StartLocation, 0, 1, 1, 1, 1);
        }

        [Test]
        public void ReportsUtf8BomAbsent()
        {
            var result = Read("a,b");

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.HadUtf8Bom, Is.False);
        }

        [Test]
        public void StrictUtf8ReadsKoreanText()
        {
            var result = Read("달빛,마을");

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.Records[0].Fields.Select(field => field.Value),
                Is.EqualTo(new[] { "달빛", "마을" }));
        }

        [Test]
        public void EmptyInputProducesNoRecords()
        {
            var result = Read(string.Empty);

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.Records, Is.Empty);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void BlankLineProducesOneEmptyFieldRecord()
        {
            var result = Read("\n");

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.Records.Count, Is.EqualTo(1));
            Assert.That(result.Records[0].Fields.Count, Is.EqualTo(1));
            Assert.That(result.Records[0].Fields[0].Value, Is.Empty);
        }

        [Test]
        public void TerminalRecordSeparatorDoesNotProducePhantomRecord()
        {
            var crLf = Read("a\r\n");
            var lf = Read("a\n");

            Assert.That(crLf.Success, Is.True, FormatErrors(crLf));
            Assert.That(lf.Success, Is.True, FormatErrors(lf));
            Assert.That(crLf.Records.Count, Is.EqualTo(1));
            Assert.That(lf.Records.Count, Is.EqualTo(1));
        }

        [Test]
        public void PreservesRecordAndFieldOrdinalOrder()
        {
            var result = Read("a,b\nc,d");

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.Records.Select(record => record.RecordNumber),
                Is.EqualTo(new[] { 1, 2 }));
            Assert.That(result.Records[1].Fields.Select(field => field.StartLocation.FieldNumber),
                Is.EqualTo(new[] { 1, 2 }));
            Assert.That(result.Records[1].Fields.All(field =>
                field.StartLocation.RecordNumber == 2), Is.True);
        }

        [Test]
        public void TracksExactLocationsAcrossQuotedMultilineField()
        {
            var result = Read("first,\"two\r\nlines\",last\r\nx,y");

            Assert.That(result.Success, Is.True, FormatErrors(result));
            var multiline = result.Records[0].Fields[1];
            AssertLocation(multiline.StartLocation, 6, 1, 7, 1, 2);
            AssertLocation(multiline.EndLocationExclusive, 18, 2, 7, 1, 2);
            AssertLocation(result.Records[0].EndLocationExclusive, 23, 2, 12, 1, 3);
            AssertLocation(result.Records[1].StartLocation, 25, 3, 1, 2, 1);
        }

        [Test]
        public void CharOffsetAndColumnCountUtf16CodeUnits()
        {
            var result = Read("😀,x");

            Assert.That(result.Success, Is.True, FormatErrors(result));
            AssertLocation(result.Records[0].Fields[1].StartLocation, 3, 1, 4, 1, 2);
            AssertLocation(result.Records[0].EndLocationExclusive, 4, 1, 5, 1, 2);
        }

        [Test]
        public void RejectsBareCarriageReturnAtExactLocation()
        {
            var result = Read("a\rb");

            AssertError(result, CsvReadErrorCode.BareCarriageReturn, 1, 1, 2, 1, 1);
        }

        [Test]
        public void RejectsQuoteInsideUnquotedFieldAtExactLocation()
        {
            var result = Read("ab\"c");

            AssertError(
                result,
                CsvReadErrorCode.UnexpectedQuoteInUnquotedField,
                2,
                1,
                3,
                1,
                1);
        }

        [Test]
        public void RejectsCharacterAfterClosingQuoteAtExactLocation()
        {
            var result = Read("\"a\"x");

            AssertError(
                result,
                CsvReadErrorCode.UnexpectedCharacterAfterClosingQuote,
                3,
                1,
                4,
                1,
                1);
        }

        [Test]
        public void RejectsUnterminatedQuotedFieldAtEofLocation()
        {
            var result = Read("\"a");

            AssertError(result, CsvReadErrorCode.UnterminatedQuotedField, 2, 1, 3, 1, 1);
        }

        [Test]
        public void RejectsInvalidUtf8AtExactDecodedLocation()
        {
            var result = new Rfc4180CsvReader().Read(
                new byte[] { 0x61, 0x0A, 0xC3, 0x28 },
                "invalid.csv");

            AssertError(result, CsvReadErrorCode.InvalidUtf8, 2, 2, 1, 2, 1);
        }

        [Test]
        public void RejectsUtf16LittleEndianBom()
        {
            AssertUnsupportedBom(new byte[] { 0xFF, 0xFE, 0x61, 0x00 });
        }

        [Test]
        public void RejectsUtf16BigEndianBom()
        {
            AssertUnsupportedBom(new byte[] { 0xFE, 0xFF, 0x00, 0x61 });
        }

        [Test]
        public void RejectsUtf32LittleEndianBom()
        {
            AssertUnsupportedBom(new byte[] { 0xFF, 0xFE, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00 });
        }

        [Test]
        public void RejectsUtf32BigEndianBom()
        {
            AssertUnsupportedBom(new byte[] { 0x00, 0x00, 0xFE, 0xFF, 0x00, 0x00, 0x00, 0x61 });
        }

        [Test]
        public void SyntaxFailurePublishesNoPartialRecords()
        {
            var result = Read("valid,row\n\"unterminated");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Records, Is.Empty);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
        }

        [Test]
        public void ReadDoesNotModifySourceBytes()
        {
            var bytes = WithBom("\"a,b\",\"c\"\"d\"");
            var before = bytes.ToArray();

            var result = new Rfc4180CsvReader().Read(bytes, "unchanged.csv");

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(bytes, Is.EqualTo(before));
        }

        [Test]
        public void ErrorsPreserveSourceNameAndCollectionsAreReadOnly()
        {
            var failure = new Rfc4180CsvReader().Read(
                Encoding.UTF8.GetBytes("a\rb"),
                "folder/source.csv");
            var success = Read("a");

            Assert.That(failure.Errors.Single().SourceName, Is.EqualTo("folder/source.csv"));
            Assert.That(failure.Errors.Single().ToString(), Does.StartWith("folder/source.csv"));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<CsvRecord>)success.Records).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<CsvField>)success.Records[0].Fields).Clear());
        }

        [Test]
        public void NullArgumentsThrowExplicitArgumentExceptions()
        {
            var reader = new Rfc4180CsvReader();

            Assert.Throws<ArgumentNullException>(() => reader.Read(null, "source.csv"));
            Assert.Throws<ArgumentNullException>(() => reader.Read(Array.Empty<byte>(), null));
        }

        private static CsvReadResult Read(string text)
        {
            return new Rfc4180CsvReader().Read(
                new UTF8Encoding(false, true).GetBytes(text),
                "test.csv");
        }

        private static string[] Project(CsvReadResult result)
        {
            return result.Records
                .Select(record => string.Join("|", record.Fields.Select(field => field.Value)))
                .ToArray();
        }

        private static byte[] WithBom(string text)
        {
            var content = new UTF8Encoding(false, true).GetBytes(text);
            var bytes = new byte[content.Length + 3];
            bytes[0] = 0xEF;
            bytes[1] = 0xBB;
            bytes[2] = 0xBF;
            Buffer.BlockCopy(content, 0, bytes, 3, content.Length);
            return bytes;
        }

        private static void AssertUnsupportedBom(byte[] bytes)
        {
            var result = new Rfc4180CsvReader().Read(bytes, "unsupported.csv");

            AssertError(result, CsvReadErrorCode.UnsupportedBom, 0, 1, 1, 1, 1);
        }

        private static void AssertError(
            CsvReadResult result,
            CsvReadErrorCode code,
            int charOffset,
            int physicalLine,
            int physicalColumn,
            int recordNumber,
            int fieldNumber)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Records, Is.Empty);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0].Code, Is.EqualTo(code));
            AssertLocation(
                result.Errors[0].Location,
                charOffset,
                physicalLine,
                physicalColumn,
                recordNumber,
                fieldNumber);
        }

        private static void AssertLocation(
            CsvSourceLocation actual,
            int charOffset,
            int physicalLine,
            int physicalColumn,
            int recordNumber,
            int fieldNumber)
        {
            Assert.That(actual.CharOffset, Is.EqualTo(charOffset));
            Assert.That(actual.PhysicalLine, Is.EqualTo(physicalLine));
            Assert.That(actual.PhysicalColumn, Is.EqualTo(physicalColumn));
            Assert.That(actual.RecordNumber, Is.EqualTo(recordNumber));
            Assert.That(actual.FieldNumber, Is.EqualTo(fieldNumber));
        }

        private static string FormatErrors(CsvReadResult result)
        {
            return string.Join("\n", result.Errors.Select(error => error.ToString()));
        }
    }
}
