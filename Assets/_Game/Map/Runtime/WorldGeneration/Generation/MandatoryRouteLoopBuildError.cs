using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum MandatoryRouteLoopBuildErrorCode
    {
        MissingTerminalSet = 0,
        MissingConnectorTree = 1,
        MissingHorizontalBackbonePlan = 2,
        MissingVerticalGatewayPlan = 3,
        MissingConflictResolutionPlan = 4,
        SourceIdentityMismatch = 5,
        DuplicateLoopId = 6
    }

    public sealed class MandatoryRouteLoopBuildError
    {
        public MandatoryRouteLoopBuildError(MandatoryRouteLoopBuildErrorCode code, string sourceId, string message)
        {
            if (sourceId == null) throw new ArgumentNullException(nameof(sourceId));
            if (string.IsNullOrEmpty(message)) throw new ArgumentException("Error message is required.", nameof(message));
            Code = code;
            SourceId = sourceId;
            Message = message;
        }

        public MandatoryRouteLoopBuildErrorCode Code { get; }
        public string SourceId { get; }
        public string Message { get; }
    }
}
