using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum MandatoryTerminalBuildStatus
    {
        Completed,
        InvalidInput
    }

    public sealed class MandatoryTerminalBuildResult
    {
        private readonly IReadOnlyList<MandatoryTerminalBuildError> errors;

        private MandatoryTerminalBuildResult(
            MandatoryTerminalBuildStatus status,
            MandatoryRouteTerminalSet terminalSet,
            MandatoryTerminalBuildDiagnostics diagnostics,
            IEnumerable<MandatoryTerminalBuildError> errors)
        {
            if (errors == null) throw new ArgumentNullException(nameof(errors));
            var ordered = MandatoryTerminalBuilder.SortAndDedupeErrors(errors);
            if (status == MandatoryTerminalBuildStatus.Completed)
            {
                if (terminalSet == null || diagnostics == null || ordered.Count != 0)
                    throw new ArgumentException("Completed build requires output, diagnostics, and zero errors.");
            }
            else if (status == MandatoryTerminalBuildStatus.InvalidInput)
            {
                if (terminalSet != null || diagnostics != null || ordered.Count == 0)
                    throw new ArgumentException("Invalid input requires errors and no output.");
            }
            else throw new ArgumentOutOfRangeException(nameof(status));

            Status = status;
            TerminalSet = terminalSet;
            Diagnostics = diagnostics;
            this.errors = new ReadOnlyCollection<MandatoryTerminalBuildError>(ordered);
        }

        public MandatoryTerminalBuildStatus Status { get; }
        public bool Succeeded => Status == MandatoryTerminalBuildStatus.Completed;
        public bool RetryRequired => false;
        public MandatoryRouteTerminalSet TerminalSet { get; }
        public MandatoryTerminalBuildDiagnostics Diagnostics { get; }
        public IReadOnlyList<MandatoryTerminalBuildError> Errors => errors;

        internal static MandatoryTerminalBuildResult Completed(
            MandatoryRouteTerminalSet terminalSet,
            MandatoryTerminalBuildDiagnostics diagnostics) =>
            new MandatoryTerminalBuildResult(
                MandatoryTerminalBuildStatus.Completed, terminalSet, diagnostics,
                Array.Empty<MandatoryTerminalBuildError>());

        internal static MandatoryTerminalBuildResult Invalid(
            IEnumerable<MandatoryTerminalBuildError> errors) =>
            new MandatoryTerminalBuildResult(
                MandatoryTerminalBuildStatus.InvalidInput, null, null, errors);
    }
}
