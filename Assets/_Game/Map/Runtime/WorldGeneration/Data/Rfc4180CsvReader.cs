using System;
using System.Collections.Generic;
using System.Text;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class Rfc4180CsvReader
    {
        private const int Utf8BomLength = 3;

        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public CsvReadResult Read(byte[] utf8Bytes, string sourceName)
        {
            if (utf8Bytes == null)
            {
                throw new ArgumentNullException(nameof(utf8Bytes));
            }

            if (sourceName == null)
            {
                throw new ArgumentNullException(nameof(sourceName));
            }

            if (HasUnsupportedBom(utf8Bytes))
            {
                return Failure(
                    false,
                    sourceName,
                    CsvReadErrorCode.UnsupportedBom,
                    "UTF-16 and UTF-32 byte order marks are not supported.",
                    new CsvSourceLocation(0, 1, 1, 1, 1));
            }

            var hadUtf8Bom = HasPrefix(utf8Bytes, 0xEF, 0xBB, 0xBF);
            var contentOffset = hadUtf8Bom ? Utf8BomLength : 0;
            string text;
            try
            {
                text = StrictUtf8.GetString(
                    utf8Bytes,
                    contentOffset,
                    utf8Bytes.Length - contentOffset);
            }
            catch (DecoderFallbackException)
            {
                var invalidRelativeIndex = FindFirstInvalidUtf8Byte(
                    utf8Bytes,
                    contentOffset,
                    utf8Bytes.Length - contentOffset);
                string validPrefix;
                try
                {
                    validPrefix = StrictUtf8.GetString(
                        utf8Bytes,
                        contentOffset,
                        invalidRelativeIndex);
                }
                catch (DecoderFallbackException)
                {
                    validPrefix = string.Empty;
                }

                return Failure(
                    hadUtf8Bom,
                    sourceName,
                    CsvReadErrorCode.InvalidUtf8,
                    "Input contains an invalid UTF-8 byte sequence.",
                    GetLocationAfterValidPrefix(validPrefix));
            }

            return Parse(text, sourceName, hadUtf8Bom);
        }

        private static CsvReadResult Parse(string text, string sourceName, bool hadUtf8Bom)
        {
            if (text.Length == 0)
            {
                return Success(hadUtf8Bom, Array.Empty<CsvRecord>());
            }

            var records = new List<CsvRecord>();
            var fields = new List<CsvField>();
            var fieldValue = new StringBuilder();
            var cursor = new Cursor();
            var recordNumber = 1;
            var fieldNumber = 1;
            var state = ParserState.StartField;
            var fieldWasQuoted = false;
            var recordStart = cursor.Location(recordNumber, fieldNumber);
            var fieldStart = recordStart;
            var terminalRecordSeparator = false;

            while (cursor.CharOffset < text.Length)
            {
                var character = text[cursor.CharOffset];
                switch (state)
                {
                    case ParserState.StartField:
                        if (character == ',')
                        {
                            AddField(
                                fields,
                                fieldValue,
                                false,
                                fieldStart,
                                cursor.Location(recordNumber, fieldNumber));
                            cursor.AdvanceCodeUnit();
                            fieldNumber++;
                            fieldStart = cursor.Location(recordNumber, fieldNumber);
                        }
                        else if (character == '"')
                        {
                            fieldWasQuoted = true;
                            state = ParserState.InQuotedField;
                            cursor.AdvanceCodeUnit();
                        }
                        else if (character == '\r')
                        {
                            if (!IsCrLf(text, cursor.CharOffset))
                            {
                                return SyntaxFailure(
                                    hadUtf8Bom,
                                    sourceName,
                                    CsvReadErrorCode.BareCarriageReturn,
                                    "A carriage return must be followed by a line feed.",
                                    cursor,
                                    recordNumber,
                                    fieldNumber);
                            }

                            CompleteRecord(
                                records,
                                fields,
                                fieldValue,
                                false,
                                recordStart,
                                fieldStart,
                                cursor,
                                recordNumber,
                                fieldNumber);
                            cursor.AdvanceLineBoundary(2);
                            terminalRecordSeparator = cursor.CharOffset == text.Length;
                            if (!terminalRecordSeparator)
                            {
                                StartNextRecord(
                                    ref recordNumber,
                                    ref fieldNumber,
                                    ref state,
                                    ref fieldWasQuoted,
                                    ref recordStart,
                                    ref fieldStart,
                                    cursor);
                            }
                        }
                        else if (character == '\n')
                        {
                            CompleteRecord(
                                records,
                                fields,
                                fieldValue,
                                false,
                                recordStart,
                                fieldStart,
                                cursor,
                                recordNumber,
                                fieldNumber);
                            cursor.AdvanceLineBoundary(1);
                            terminalRecordSeparator = cursor.CharOffset == text.Length;
                            if (!terminalRecordSeparator)
                            {
                                StartNextRecord(
                                    ref recordNumber,
                                    ref fieldNumber,
                                    ref state,
                                    ref fieldWasQuoted,
                                    ref recordStart,
                                    ref fieldStart,
                                    cursor);
                            }
                        }
                        else
                        {
                            fieldValue.Append(character);
                            state = ParserState.InUnquotedField;
                            cursor.AdvanceCodeUnit();
                        }

                        break;

                    case ParserState.InUnquotedField:
                        if (character == ',')
                        {
                            AddField(
                                fields,
                                fieldValue,
                                false,
                                fieldStart,
                                cursor.Location(recordNumber, fieldNumber));
                            cursor.AdvanceCodeUnit();
                            fieldNumber++;
                            fieldStart = cursor.Location(recordNumber, fieldNumber);
                            state = ParserState.StartField;
                        }
                        else if (character == '"')
                        {
                            return SyntaxFailure(
                                hadUtf8Bom,
                                sourceName,
                                CsvReadErrorCode.UnexpectedQuoteInUnquotedField,
                                "A quote may only open at the start of a field.",
                                cursor,
                                recordNumber,
                                fieldNumber);
                        }
                        else if (character == '\r')
                        {
                            if (!IsCrLf(text, cursor.CharOffset))
                            {
                                return SyntaxFailure(
                                    hadUtf8Bom,
                                    sourceName,
                                    CsvReadErrorCode.BareCarriageReturn,
                                    "A carriage return must be followed by a line feed.",
                                    cursor,
                                    recordNumber,
                                    fieldNumber);
                            }

                            CompleteRecord(
                                records,
                                fields,
                                fieldValue,
                                false,
                                recordStart,
                                fieldStart,
                                cursor,
                                recordNumber,
                                fieldNumber);
                            cursor.AdvanceLineBoundary(2);
                            terminalRecordSeparator = cursor.CharOffset == text.Length;
                            if (!terminalRecordSeparator)
                            {
                                StartNextRecord(
                                    ref recordNumber,
                                    ref fieldNumber,
                                    ref state,
                                    ref fieldWasQuoted,
                                    ref recordStart,
                                    ref fieldStart,
                                    cursor);
                            }
                        }
                        else if (character == '\n')
                        {
                            CompleteRecord(
                                records,
                                fields,
                                fieldValue,
                                false,
                                recordStart,
                                fieldStart,
                                cursor,
                                recordNumber,
                                fieldNumber);
                            cursor.AdvanceLineBoundary(1);
                            terminalRecordSeparator = cursor.CharOffset == text.Length;
                            if (!terminalRecordSeparator)
                            {
                                StartNextRecord(
                                    ref recordNumber,
                                    ref fieldNumber,
                                    ref state,
                                    ref fieldWasQuoted,
                                    ref recordStart,
                                    ref fieldStart,
                                    cursor);
                            }
                        }
                        else
                        {
                            fieldValue.Append(character);
                            cursor.AdvanceCodeUnit();
                        }

                        break;

                    case ParserState.InQuotedField:
                        if (character == '"')
                        {
                            if (cursor.CharOffset + 1 < text.Length &&
                                text[cursor.CharOffset + 1] == '"')
                            {
                                fieldValue.Append('"');
                                cursor.AdvanceCodeUnit();
                                cursor.AdvanceCodeUnit();
                            }
                            else
                            {
                                cursor.AdvanceCodeUnit();
                                state = ParserState.AfterClosingQuote;
                            }
                        }
                        else if (character == '\r')
                        {
                            if (!IsCrLf(text, cursor.CharOffset))
                            {
                                return SyntaxFailure(
                                    hadUtf8Bom,
                                    sourceName,
                                    CsvReadErrorCode.BareCarriageReturn,
                                    "A carriage return must be followed by a line feed.",
                                    cursor,
                                    recordNumber,
                                    fieldNumber);
                            }

                            fieldValue.Append("\r\n");
                            cursor.AdvanceLineBoundary(2);
                        }
                        else if (character == '\n')
                        {
                            fieldValue.Append('\n');
                            cursor.AdvanceLineBoundary(1);
                        }
                        else
                        {
                            fieldValue.Append(character);
                            cursor.AdvanceCodeUnit();
                        }

                        break;

                    case ParserState.AfterClosingQuote:
                        if (character == ',')
                        {
                            AddField(
                                fields,
                                fieldValue,
                                true,
                                fieldStart,
                                cursor.Location(recordNumber, fieldNumber));
                            cursor.AdvanceCodeUnit();
                            fieldNumber++;
                            fieldStart = cursor.Location(recordNumber, fieldNumber);
                            fieldWasQuoted = false;
                            state = ParserState.StartField;
                        }
                        else if (character == '\r')
                        {
                            if (!IsCrLf(text, cursor.CharOffset))
                            {
                                return SyntaxFailure(
                                    hadUtf8Bom,
                                    sourceName,
                                    CsvReadErrorCode.BareCarriageReturn,
                                    "A carriage return must be followed by a line feed.",
                                    cursor,
                                    recordNumber,
                                    fieldNumber);
                            }

                            CompleteRecord(
                                records,
                                fields,
                                fieldValue,
                                true,
                                recordStart,
                                fieldStart,
                                cursor,
                                recordNumber,
                                fieldNumber);
                            cursor.AdvanceLineBoundary(2);
                            terminalRecordSeparator = cursor.CharOffset == text.Length;
                            if (!terminalRecordSeparator)
                            {
                                StartNextRecord(
                                    ref recordNumber,
                                    ref fieldNumber,
                                    ref state,
                                    ref fieldWasQuoted,
                                    ref recordStart,
                                    ref fieldStart,
                                    cursor);
                            }
                        }
                        else if (character == '\n')
                        {
                            CompleteRecord(
                                records,
                                fields,
                                fieldValue,
                                true,
                                recordStart,
                                fieldStart,
                                cursor,
                                recordNumber,
                                fieldNumber);
                            cursor.AdvanceLineBoundary(1);
                            terminalRecordSeparator = cursor.CharOffset == text.Length;
                            if (!terminalRecordSeparator)
                            {
                                StartNextRecord(
                                    ref recordNumber,
                                    ref fieldNumber,
                                    ref state,
                                    ref fieldWasQuoted,
                                    ref recordStart,
                                    ref fieldStart,
                                    cursor);
                            }
                        }
                        else
                        {
                            return SyntaxFailure(
                                hadUtf8Bom,
                                sourceName,
                                CsvReadErrorCode.UnexpectedCharacterAfterClosingQuote,
                                "Only a separator, a record boundary, or EOF may follow a closing quote.",
                                cursor,
                                recordNumber,
                                fieldNumber);
                        }

                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (terminalRecordSeparator)
                {
                    break;
                }
            }

            if (terminalRecordSeparator)
            {
                return Success(hadUtf8Bom, records);
            }

            if (state == ParserState.InQuotedField)
            {
                return SyntaxFailure(
                    hadUtf8Bom,
                    sourceName,
                    CsvReadErrorCode.UnterminatedQuotedField,
                    "The quoted field reached EOF before a closing quote.",
                    cursor,
                    recordNumber,
                    fieldNumber);
            }

            CompleteRecord(
                records,
                fields,
                fieldValue,
                fieldWasQuoted,
                recordStart,
                fieldStart,
                cursor,
                recordNumber,
                fieldNumber);
            return Success(hadUtf8Bom, records);
        }

        private static void AddField(
            ICollection<CsvField> fields,
            StringBuilder fieldValue,
            bool wasQuoted,
            CsvSourceLocation startLocation,
            CsvSourceLocation endLocationExclusive)
        {
            fields.Add(new CsvField(
                fieldValue.ToString(),
                wasQuoted,
                startLocation,
                endLocationExclusive));
            fieldValue.Length = 0;
        }

        private static void CompleteRecord(
            ICollection<CsvRecord> records,
            ICollection<CsvField> fields,
            StringBuilder fieldValue,
            bool wasQuoted,
            CsvSourceLocation recordStart,
            CsvSourceLocation fieldStart,
            Cursor cursor,
            int recordNumber,
            int fieldNumber)
        {
            var endLocation = cursor.Location(recordNumber, fieldNumber);
            AddField(fields, fieldValue, wasQuoted, fieldStart, endLocation);
            records.Add(new CsvRecord(recordNumber, fields, recordStart, endLocation));
            fields.Clear();
        }

        private static void StartNextRecord(
            ref int recordNumber,
            ref int fieldNumber,
            ref ParserState state,
            ref bool fieldWasQuoted,
            ref CsvSourceLocation recordStart,
            ref CsvSourceLocation fieldStart,
            Cursor cursor)
        {
            recordNumber++;
            fieldNumber = 1;
            state = ParserState.StartField;
            fieldWasQuoted = false;
            recordStart = cursor.Location(recordNumber, fieldNumber);
            fieldStart = recordStart;
        }

        private static CsvReadResult SyntaxFailure(
            bool hadUtf8Bom,
            string sourceName,
            CsvReadErrorCode code,
            string message,
            Cursor cursor,
            int recordNumber,
            int fieldNumber)
        {
            return Failure(
                hadUtf8Bom,
                sourceName,
                code,
                message,
                cursor.Location(recordNumber, fieldNumber));
        }

        private static CsvReadResult Success(
            bool hadUtf8Bom,
            IEnumerable<CsvRecord> records)
        {
            return new CsvReadResult(
                hadUtf8Bom,
                records,
                Array.Empty<CsvReadError>());
        }

        private static CsvReadResult Failure(
            bool hadUtf8Bom,
            string sourceName,
            CsvReadErrorCode code,
            string message,
            CsvSourceLocation location)
        {
            return new CsvReadResult(
                hadUtf8Bom,
                Array.Empty<CsvRecord>(),
                new[] { new CsvReadError(sourceName, code, message, location) });
        }

        private static bool IsCrLf(string text, int index)
        {
            return index + 1 < text.Length && text[index + 1] == '\n';
        }

        private static bool HasUnsupportedBom(byte[] bytes)
        {
            return HasPrefix(bytes, 0xFF, 0xFE, 0x00, 0x00) ||
                   HasPrefix(bytes, 0x00, 0x00, 0xFE, 0xFF) ||
                   HasPrefix(bytes, 0xFF, 0xFE) ||
                   HasPrefix(bytes, 0xFE, 0xFF);
        }

        private static bool HasPrefix(byte[] bytes, params byte[] prefix)
        {
            if (bytes.Length < prefix.Length)
            {
                return false;
            }

            for (var index = 0; index < prefix.Length; index++)
            {
                if (bytes[index] != prefix[index])
                {
                    return false;
                }
            }

            return true;
        }

        private static int FindFirstInvalidUtf8Byte(byte[] bytes, int offset, int count)
        {
            var relativeIndex = 0;
            while (relativeIndex < count)
            {
                var first = bytes[offset + relativeIndex];
                if (first <= 0x7F)
                {
                    relativeIndex++;
                    continue;
                }

                if (first >= 0xC2 && first <= 0xDF)
                {
                    if (!HasContinuation(bytes, offset, count, relativeIndex + 1))
                    {
                        return relativeIndex;
                    }

                    relativeIndex += 2;
                    continue;
                }

                if (first == 0xE0)
                {
                    if (!HasByteInRange(bytes, offset, count, relativeIndex + 1, 0xA0, 0xBF) ||
                        !HasContinuation(bytes, offset, count, relativeIndex + 2))
                    {
                        return relativeIndex;
                    }

                    relativeIndex += 3;
                    continue;
                }

                if ((first >= 0xE1 && first <= 0xEC) ||
                    (first >= 0xEE && first <= 0xEF))
                {
                    if (!HasContinuation(bytes, offset, count, relativeIndex + 1) ||
                        !HasContinuation(bytes, offset, count, relativeIndex + 2))
                    {
                        return relativeIndex;
                    }

                    relativeIndex += 3;
                    continue;
                }

                if (first == 0xED)
                {
                    if (!HasByteInRange(bytes, offset, count, relativeIndex + 1, 0x80, 0x9F) ||
                        !HasContinuation(bytes, offset, count, relativeIndex + 2))
                    {
                        return relativeIndex;
                    }

                    relativeIndex += 3;
                    continue;
                }

                if (first == 0xF0)
                {
                    if (!HasByteInRange(bytes, offset, count, relativeIndex + 1, 0x90, 0xBF) ||
                        !HasContinuation(bytes, offset, count, relativeIndex + 2) ||
                        !HasContinuation(bytes, offset, count, relativeIndex + 3))
                    {
                        return relativeIndex;
                    }

                    relativeIndex += 4;
                    continue;
                }

                if (first >= 0xF1 && first <= 0xF3)
                {
                    if (!HasContinuation(bytes, offset, count, relativeIndex + 1) ||
                        !HasContinuation(bytes, offset, count, relativeIndex + 2) ||
                        !HasContinuation(bytes, offset, count, relativeIndex + 3))
                    {
                        return relativeIndex;
                    }

                    relativeIndex += 4;
                    continue;
                }

                if (first == 0xF4)
                {
                    if (!HasByteInRange(bytes, offset, count, relativeIndex + 1, 0x80, 0x8F) ||
                        !HasContinuation(bytes, offset, count, relativeIndex + 2) ||
                        !HasContinuation(bytes, offset, count, relativeIndex + 3))
                    {
                        return relativeIndex;
                    }

                    relativeIndex += 4;
                    continue;
                }

                return relativeIndex;
            }

            return count;
        }

        private static bool HasContinuation(
            byte[] bytes,
            int offset,
            int count,
            int relativeIndex)
        {
            return HasByteInRange(bytes, offset, count, relativeIndex, 0x80, 0xBF);
        }

        private static bool HasByteInRange(
            byte[] bytes,
            int offset,
            int count,
            int relativeIndex,
            byte minimum,
            byte maximum)
        {
            return relativeIndex < count &&
                   bytes[offset + relativeIndex] >= minimum &&
                   bytes[offset + relativeIndex] <= maximum;
        }

        private static CsvSourceLocation GetLocationAfterValidPrefix(string prefix)
        {
            var cursor = new Cursor();
            var recordNumber = 1;
            var fieldNumber = 1;
            var state = ParserState.StartField;
            while (cursor.CharOffset < prefix.Length)
            {
                var character = prefix[cursor.CharOffset];
                if (character == '\r' && IsCrLf(prefix, cursor.CharOffset))
                {
                    var wasQuoted = state == ParserState.InQuotedField;
                    cursor.AdvanceLineBoundary(2);
                    if (!wasQuoted)
                    {
                        recordNumber++;
                        fieldNumber = 1;
                        state = ParserState.StartField;
                    }

                    continue;
                }

                if (character == '\n')
                {
                    var wasQuoted = state == ParserState.InQuotedField;
                    cursor.AdvanceLineBoundary(1);
                    if (!wasQuoted)
                    {
                        recordNumber++;
                        fieldNumber = 1;
                        state = ParserState.StartField;
                    }

                    continue;
                }

                if (character == ',' && state != ParserState.InQuotedField)
                {
                    fieldNumber++;
                    state = ParserState.StartField;
                    cursor.AdvanceCodeUnit();
                    continue;
                }

                if (character == '"')
                {
                    if (state == ParserState.StartField)
                    {
                        state = ParserState.InQuotedField;
                    }
                    else if (state == ParserState.InQuotedField)
                    {
                        if (cursor.CharOffset + 1 < prefix.Length &&
                            prefix[cursor.CharOffset + 1] == '"')
                        {
                            cursor.AdvanceCodeUnit();
                        }
                        else
                        {
                            state = ParserState.AfterClosingQuote;
                        }
                    }
                }
                else if (state == ParserState.StartField)
                {
                    state = ParserState.InUnquotedField;
                }

                cursor.AdvanceCodeUnit();
            }

            return cursor.Location(recordNumber, fieldNumber);
        }

        private enum ParserState
        {
            StartField,
            InUnquotedField,
            InQuotedField,
            AfterClosingQuote,
        }

        private sealed class Cursor
        {
            public Cursor()
            {
                CharOffset = 0;
                PhysicalLine = 1;
                PhysicalColumn = 1;
            }

            public int CharOffset { get; private set; }

            private int PhysicalLine { get; set; }

            private int PhysicalColumn { get; set; }

            public CsvSourceLocation Location(int recordNumber, int fieldNumber)
            {
                return new CsvSourceLocation(
                    CharOffset,
                    PhysicalLine,
                    PhysicalColumn,
                    recordNumber,
                    fieldNumber);
            }

            public void AdvanceCodeUnit()
            {
                CharOffset++;
                PhysicalColumn++;
            }

            public void AdvanceLineBoundary(int codeUnitCount)
            {
                CharOffset += codeUnitCount;
                PhysicalLine++;
                PhysicalColumn = 1;
            }
        }
    }
}
