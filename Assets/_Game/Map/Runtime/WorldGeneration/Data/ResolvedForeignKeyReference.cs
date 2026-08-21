using System;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class ResolvedForeignKeyReference
    {
        internal ResolvedForeignKeyReference(
            ForeignKeyRecordIdentity sourceIdentity,
            CsvParsedField sourceField,
            int? listIndex,
            string rawValue,
            string targetFileName,
            string targetColumnName,
            string targetValue,
            ForeignKeyRecordIdentity targetIdentity)
        {
            SourceIdentity = sourceIdentity ??
                             throw new ArgumentNullException(nameof(sourceIdentity));
            SourceField = sourceField ?? throw new ArgumentNullException(nameof(sourceField));
            ListIndex = listIndex;
            RawValue = rawValue ?? throw new ArgumentNullException(nameof(rawValue));
            TargetFileName = targetFileName ??
                             throw new ArgumentNullException(nameof(targetFileName));
            TargetColumnName = targetColumnName ??
                               throw new ArgumentNullException(nameof(targetColumnName));
            TargetValue = targetValue ?? throw new ArgumentNullException(nameof(targetValue));
            TargetIdentity = targetIdentity ??
                             throw new ArgumentNullException(nameof(targetIdentity));
        }

        public ForeignKeyRecordIdentity SourceIdentity { get; }

        public string SourceFileName => SourceIdentity.FileName;

        public int SourceRecordNumber => SourceIdentity.RecordNumber;

        public CsvParsedField SourceField { get; }

        public string SourceColumnName => SourceField.Schema.ColumnName;

        public int SourceColumnOrder => SourceField.Schema.ColumnOrder;

        public CsvSourceLocation SourceLocation =>
            SourceField.ValidatedField.SourceField.StartLocation;

        public int? ListIndex { get; }

        public string RawValue { get; }

        public string TargetFileName { get; }

        public string TargetColumnName { get; }

        public string TargetValue { get; }

        public ForeignKeyRecordIdentity TargetIdentity { get; }
    }
}
