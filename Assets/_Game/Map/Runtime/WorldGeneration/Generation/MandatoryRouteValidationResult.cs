using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum MandatoryRouteValidationStatus
    {
        Completed = 0,
        InvalidInput = 1
    }

    public sealed class MandatoryRouteValidationResult
    {
        internal MandatoryRouteValidationResult(MandatoryRouteValidationStatus status, MandatoryRouteValidationReport report,
            MandatoryRouteValidationDiagnostics diagnostics)
        {
            if (!Enum.IsDefined(typeof(MandatoryRouteValidationStatus), status)) throw new ArgumentOutOfRangeException(nameof(status));
            Status = status;
            Report = report;
            Diagnostics = diagnostics;
        }

        public MandatoryRouteValidationStatus Status { get; }
        public MandatoryRouteValidationReport Report { get; }
        public MandatoryRouteValidationDiagnostics Diagnostics { get; }
        public bool Succeeded => Status == MandatoryRouteValidationStatus.Completed && Report != null && Report.IsValid;
        public bool RetryRequired => false;
    }
}
