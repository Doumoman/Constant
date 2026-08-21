using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum VerticalGatewayBuildStatus
    {
        Completed,
        InvalidInput
    }

    public sealed class VerticalGatewayBuildResult
    {
        private readonly IReadOnlyList<VerticalGatewayBuildError> errors;

        private VerticalGatewayBuildResult(
            VerticalGatewayBuildStatus status,
            VerticalGatewayPlan plan,
            VerticalGatewayDiagnostics diagnostics,
            IEnumerable<VerticalGatewayBuildError> sourceErrors)
        {
            var values = new List<VerticalGatewayBuildError>(sourceErrors ?? throw new ArgumentNullException(nameof(sourceErrors)));
            values.Sort();
            var deduplicated = new List<VerticalGatewayBuildError>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var error in values)
                if (error != null && keys.Add(error.Key)) deduplicated.Add(error);
            if (status == VerticalGatewayBuildStatus.Completed && (plan == null || diagnostics == null || deduplicated.Count != 0))
                throw new ArgumentException("Completed results require a plan, diagnostics, and no errors.");
            if (status == VerticalGatewayBuildStatus.InvalidInput && (plan != null || diagnostics != null || deduplicated.Count == 0))
                throw new ArgumentException("Invalid results require errors and publish no output.");
            Status = status;
            Plan = plan;
            Diagnostics = diagnostics;
            errors = new ReadOnlyCollection<VerticalGatewayBuildError>(deduplicated);
        }

        public VerticalGatewayBuildStatus Status { get; }
        public VerticalGatewayPlan Plan { get; }
        public VerticalGatewayDiagnostics Diagnostics { get; }
        public IReadOnlyList<VerticalGatewayBuildError> Errors => errors;
        public bool Succeeded => Status == VerticalGatewayBuildStatus.Completed;
        public bool RetryRequired => false;

        internal static VerticalGatewayBuildResult Completed(VerticalGatewayPlan plan, VerticalGatewayDiagnostics diagnostics) =>
            new VerticalGatewayBuildResult(VerticalGatewayBuildStatus.Completed, plan, diagnostics, Array.Empty<VerticalGatewayBuildError>());

        internal static VerticalGatewayBuildResult Invalid(IEnumerable<VerticalGatewayBuildError> errors) =>
            new VerticalGatewayBuildResult(VerticalGatewayBuildStatus.InvalidInput, null, null, errors);
    }
}
