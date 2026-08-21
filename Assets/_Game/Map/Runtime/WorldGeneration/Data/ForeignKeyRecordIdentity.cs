using System;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class ForeignKeyRecordIdentity
    {
        public ForeignKeyRecordIdentity(
            string fileName,
            CsvParsedRecord sourceRecord)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
        }

        public string FileName { get; }

        public CsvParsedRecord SourceRecord { get; }

        public int RecordNumber => SourceRecord.RecordNumber;
    }
}
