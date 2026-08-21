using System;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvField
    {
        internal CsvField(
            string value,
            bool wasQuoted,
            CsvSourceLocation startLocation,
            CsvSourceLocation endLocationExclusive)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            WasQuoted = wasQuoted;
            StartLocation = startLocation;
            EndLocationExclusive = endLocationExclusive;
        }

        public string Value { get; }

        public bool WasQuoted { get; }

        public CsvSourceLocation StartLocation { get; }

        public CsvSourceLocation EndLocationExclusive { get; }
    }
}
