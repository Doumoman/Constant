using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum UpDownConflictBuildStatus
    {
        Completed = 0,
        InvalidInput = 1
    }

    public sealed class UpDownConflictBuildResult
    {
        internal UpDownConflictBuildResult(
            UpDownConflictBuildStatus status,
            UpDownConflictResolutionPlan plan,
            UpDownConflictDiagnostics diagnostics,
            IEnumerable<UpDownConflictBuildError> errors)
        {
            Status = status;
            Plan = plan;
            Diagnostics = diagnostics;
            var values = new List<UpDownConflictBuildError>(errors ?? throw new ArgumentNullException(nameof(errors)));
            Errors = new ReadOnlyCollection<UpDownConflictBuildError>(values);
        }

        public UpDownConflictBuildStatus Status { get; }
        public UpDownConflictResolutionPlan Plan { get; }
        public UpDownConflictDiagnostics Diagnostics { get; }
        public IReadOnlyList<UpDownConflictBuildError> Errors { get; }
        public bool Succeeded => Status == UpDownConflictBuildStatus.Completed;
        public bool RetryRequired => false;
    }
}
