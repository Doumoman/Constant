using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum HorizontalBackboneBuildStatus
    {
        Completed,
        InvalidInput
    }

    public sealed class HorizontalBackboneBuildResult
    {
        private readonly IReadOnlyList<HorizontalBackboneBuildError> errors;

        public HorizontalBackboneBuildResult(HorizontalBackboneBuildStatus status, HorizontalBackbonePlan plan, HorizontalBackboneDiagnostics diagnostics, IEnumerable<HorizontalBackboneBuildError> errors)
        {
            if (status != HorizontalBackboneBuildStatus.Completed && status != HorizontalBackboneBuildStatus.InvalidInput) throw new ArgumentOutOfRangeException(nameof(status));
            if (errors == null) throw new ArgumentNullException(nameof(errors));
            var values = new List<HorizontalBackboneBuildError>(errors);
            if (status == HorizontalBackboneBuildStatus.Completed && (plan == null || diagnostics == null || values.Count != 0)) throw new ArgumentException("Completed result shape is invalid.");
            if (status == HorizontalBackboneBuildStatus.InvalidInput && (plan != null || diagnostics != null || values.Count == 0)) throw new ArgumentException("Invalid result shape is invalid.");
            Status = status;
            Plan = plan;
            Diagnostics = diagnostics;
            this.errors = new ReadOnlyCollection<HorizontalBackboneBuildError>(values);
        }

        public HorizontalBackboneBuildStatus Status { get; }
        public HorizontalBackbonePlan Plan { get; }
        public HorizontalBackboneDiagnostics Diagnostics { get; }
        public IReadOnlyList<HorizontalBackboneBuildError> Errors => errors;
        public bool Succeeded => Status == HorizontalBackboneBuildStatus.Completed;
        public bool RetryRequired => false;
    }
}
