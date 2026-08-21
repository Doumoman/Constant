using System;

namespace StarNight.Map.WorldGeneration.Data
{
    public readonly struct CsvSourceLocation : IEquatable<CsvSourceLocation>
    {
        public CsvSourceLocation(
            int charOffset,
            int physicalLine,
            int physicalColumn,
            int recordNumber,
            int fieldNumber)
        {
            if (charOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(charOffset));
            }

            if (physicalLine < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(physicalLine));
            }

            if (physicalColumn < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(physicalColumn));
            }

            if (recordNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(recordNumber));
            }

            if (fieldNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(fieldNumber));
            }

            CharOffset = charOffset;
            PhysicalLine = physicalLine;
            PhysicalColumn = physicalColumn;
            RecordNumber = recordNumber;
            FieldNumber = fieldNumber;
        }

        public int CharOffset { get; }

        public int PhysicalLine { get; }

        public int PhysicalColumn { get; }

        public int RecordNumber { get; }

        public int FieldNumber { get; }

        public bool Equals(CsvSourceLocation other)
        {
            return CharOffset == other.CharOffset &&
                   PhysicalLine == other.PhysicalLine &&
                   PhysicalColumn == other.PhysicalColumn &&
                   RecordNumber == other.RecordNumber &&
                   FieldNumber == other.FieldNumber;
        }

        public override bool Equals(object obj)
        {
            return obj is CsvSourceLocation other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = CharOffset;
                hashCode = (hashCode * 397) ^ PhysicalLine;
                hashCode = (hashCode * 397) ^ PhysicalColumn;
                hashCode = (hashCode * 397) ^ RecordNumber;
                hashCode = (hashCode * 397) ^ FieldNumber;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return "offset=" + CharOffset +
                   " line=" + PhysicalLine +
                   " column=" + PhysicalColumn +
                   " record=" + RecordNumber +
                   " field=" + FieldNumber;
        }
    }
}
