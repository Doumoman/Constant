using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum UpDownConflictBuildErrorCode
    {
        MissingVerticalGatewayPlan = 0,
        MissingRouteMaskLookup = 1,
        MissingSiteSnapshot = 2,
        MissingBiomePublication = 3,
        SourceIdentityMismatch = 4,
        DuplicateConflictId = 5,
        InvalidCandidate = 6
    }

    public sealed class UpDownConflictBuildError
    {
        public UpDownConflictBuildError(UpDownConflictBuildErrorCode code, string sourceId, string message)
        {
            if (sourceId == null) throw new ArgumentNullException(nameof(sourceId));
            if (string.IsNullOrEmpty(message)) throw new ArgumentException("Error message is required.", nameof(message));
            Code = code;
            SourceId = sourceId;
            Message = message;
        }

        public UpDownConflictBuildErrorCode Code { get; }
        public string SourceId { get; }
        public string Message { get; }
    }
}
