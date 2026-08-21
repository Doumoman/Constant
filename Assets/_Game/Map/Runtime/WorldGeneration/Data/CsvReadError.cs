using System;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvReadError
    {
        internal CsvReadError(
            string sourceName,
            CsvReadErrorCode code,
            string message,
            CsvSourceLocation location)
        {
            SourceName = sourceName ?? throw new ArgumentNullException(nameof(sourceName));
            Code = code;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Location = location;
        }

        public string SourceName { get; }

        public CsvReadErrorCode Code { get; }

        public string Message { get; }

        public CsvSourceLocation Location { get; }

        public override string ToString()
        {
            return SourceName + " (" + Location + ") " + Code + ": " + Message;
        }
    }
}
