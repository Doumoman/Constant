using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryRouteValidationReport
    {
        private readonly IReadOnlyList<MandatoryRouteValidationViolation> violations;
        private readonly IReadOnlyList<MandatoryRouteValidationViolation> errors;
        private readonly IReadOnlyList<MandatoryRouteValidationViolation> warnings;

        internal MandatoryRouteValidationReport(MandatoryRouteGraph sourceGraph, GeneratedWorldData sourceWorld,
            MandatoryRouteTerminalSet sourceTerminalSet, MandatoryRouteLoopPlan sourceLoopPlan,
            MandatoryRouteValidationSummary summary, IEnumerable<MandatoryRouteValidationViolation> sourceViolations)
        {
            SourceGraph = sourceGraph ?? throw new ArgumentNullException(nameof(sourceGraph));
            SourceWorld = sourceWorld ?? throw new ArgumentNullException(nameof(sourceWorld));
            SourceTerminalSet = sourceTerminalSet ?? throw new ArgumentNullException(nameof(sourceTerminalSet));
            SourceLoopPlan = sourceLoopPlan ?? throw new ArgumentNullException(nameof(sourceLoopPlan));
            Summary = summary ?? throw new ArgumentNullException(nameof(summary));
            var values = new List<MandatoryRouteValidationViolation>(sourceViolations ?? throw new ArgumentNullException(nameof(sourceViolations)));
            values.Sort();
            var unique = new List<MandatoryRouteValidationViolation>(values.Count);
            string previous = null;
            foreach (var value in values)
            {
                if (value == null) throw new ArgumentException("Violations cannot contain null.", nameof(sourceViolations));
                if (string.Equals(previous, value.SortKey, StringComparison.Ordinal)) continue;
                unique.Add(value);
                previous = value.SortKey;
            }
            var errorValues = unique.FindAll(value => value.Severity == MandatoryRouteValidationSeverity.Error);
            var warningValues = unique.FindAll(value => value.Severity == MandatoryRouteValidationSeverity.Warning);
            violations = new ReadOnlyCollection<MandatoryRouteValidationViolation>(unique);
            errors = new ReadOnlyCollection<MandatoryRouteValidationViolation>(errorValues);
            warnings = new ReadOnlyCollection<MandatoryRouteValidationViolation>(warningValues);
        }

        public MandatoryRouteGraph SourceGraph { get; }
        public GeneratedWorldData SourceWorld { get; }
        public MandatoryRouteTerminalSet SourceTerminalSet { get; }
        public MandatoryRouteLoopPlan SourceLoopPlan { get; }
        public MandatoryRouteValidationSummary Summary { get; }
        public IReadOnlyList<MandatoryRouteValidationViolation> Violations => violations;
        public IReadOnlyList<MandatoryRouteValidationViolation> Errors => errors;
        public IReadOnlyList<MandatoryRouteValidationViolation> Warnings => warnings;
        public bool IsValid => errors.Count == 0;
        public string PassId => IsValid ? "PASS_ROUTE" : string.Empty;
    }
}
