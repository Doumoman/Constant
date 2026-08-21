using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum MandatoryRouteLoopBuildStatus
    {
        Completed = 0,
        InvalidInput = 1
    }

    public sealed class MandatoryRouteLoopBuildResult
    {
        internal MandatoryRouteLoopBuildResult(
            MandatoryRouteLoopBuildStatus status,
            MandatoryRouteLoopPlan plan,
            MandatoryRouteLoopDiagnostics diagnostics,
            IEnumerable<MandatoryRouteLoopBuildError> errors)
        {
            Status = status;
            Plan = plan;
            Diagnostics = diagnostics;
            Errors = new ReadOnlyCollection<MandatoryRouteLoopBuildError>(new List<MandatoryRouteLoopBuildError>(errors ?? throw new ArgumentNullException(nameof(errors))));
        }

        public MandatoryRouteLoopBuildStatus Status { get; }
        public MandatoryRouteLoopPlan Plan { get; }
        public MandatoryRouteLoopDiagnostics Diagnostics { get; }
        public IReadOnlyList<MandatoryRouteLoopBuildError> Errors { get; }
        public bool Succeeded => Status == MandatoryRouteLoopBuildStatus.Completed;
        public bool RetryRequired => false;
    }
}
