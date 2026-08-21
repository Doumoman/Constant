using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum MandatoryRouteGraphBuildStatus
    {
        Completed = 0,
        InvalidInput = 1
    }

    public sealed class MandatoryRouteGraphBuildResult
    {
        internal MandatoryRouteGraphBuildResult(MandatoryRouteGraphBuildStatus status, MandatoryRouteGraph graph,
            MandatoryRouteGraphDiagnostics diagnostics, IEnumerable<MandatoryRouteGraphBuildError> errors)
        {
            if (!Enum.IsDefined(typeof(MandatoryRouteGraphBuildStatus), status)) throw new ArgumentOutOfRangeException(nameof(status));
            Status = status;
            Graph = graph;
            Diagnostics = diagnostics;
            Errors = new ReadOnlyCollection<MandatoryRouteGraphBuildError>(new List<MandatoryRouteGraphBuildError>(errors ?? Array.Empty<MandatoryRouteGraphBuildError>()));
        }

        public MandatoryRouteGraphBuildStatus Status { get; }
        public MandatoryRouteGraph Graph { get; }
        public MandatoryRouteGraphDiagnostics Diagnostics { get; }
        public IReadOnlyList<MandatoryRouteGraphBuildError> Errors { get; }
        public bool Succeeded => Status == MandatoryRouteGraphBuildStatus.Completed && Graph != null && Errors.Count == 0;
        public bool RetryRequired => false;
    }
}
