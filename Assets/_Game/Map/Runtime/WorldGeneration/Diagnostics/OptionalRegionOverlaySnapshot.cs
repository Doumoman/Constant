using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Diagnostics
{
    public sealed class OptionalRegionOverlaySnapshot
    {
        private readonly IReadOnlyList<OptionalRegionOverlayCell> cells;
        private readonly IReadOnlyList<OptionalRegionOverlayConnection> connections;
        private readonly IReadOnlyList<OptionalRegionOverlayLegendEntry> legend;

        internal OptionalRegionOverlaySnapshot(
            OptionalRegionOverlayStatus status,
            IEnumerable<OptionalRegionOverlayCell> sourceCells,
            IEnumerable<OptionalRegionOverlayConnection> sourceConnections,
            IEnumerable<OptionalRegionOverlayLegendEntry> sourceLegend,
            string sourceValidationDigest,
            string sourceInactiveDigest,
            string canonicalDigest,
            int rngDrawCount)
        {
            if (!Enum.IsDefined(typeof(OptionalRegionOverlayStatus), status)) throw new ArgumentOutOfRangeException(nameof(status));
            if (sourceCells == null) throw new ArgumentNullException(nameof(sourceCells));
            if (sourceConnections == null) throw new ArgumentNullException(nameof(sourceConnections));
            if (sourceLegend == null) throw new ArgumentNullException(nameof(sourceLegend));
            if (rngDrawCount != 0) throw new ArgumentOutOfRangeException(nameof(rngDrawCount));

            var cellValues = new List<OptionalRegionOverlayCell>(sourceCells);
            var connectionValues = new List<OptionalRegionOverlayConnection>(sourceConnections);
            var legendValues = new List<OptionalRegionOverlayLegendEntry>(sourceLegend);
            if (status == OptionalRegionOverlayStatus.Completed)
            {
                if (cellValues.Count != 169 || string.IsNullOrEmpty(sourceValidationDigest) ||
                    string.IsNullOrEmpty(sourceInactiveDigest) || string.IsNullOrEmpty(canonicalDigest))
                    throw new ArgumentException("Completed overlay snapshots require complete publication.");
            }
            else if (cellValues.Count != 0 || connectionValues.Count != 0 || legendValues.Count != 0 ||
                     !string.IsNullOrEmpty(canonicalDigest))
            {
                throw new ArgumentException("Failed overlay snapshots must be atomic and digest-free.");
            }

            cells = new ReadOnlyCollection<OptionalRegionOverlayCell>(cellValues);
            connections = new ReadOnlyCollection<OptionalRegionOverlayConnection>(connectionValues);
            legend = new ReadOnlyCollection<OptionalRegionOverlayLegendEntry>(legendValues);
            Status = status;
            SourceValidationDigest = sourceValidationDigest ?? string.Empty;
            SourceInactiveDigest = sourceInactiveDigest ?? string.Empty;
            CanonicalDigest = canonicalDigest ?? string.Empty;
            RngDrawCount = rngDrawCount;
        }

        public OptionalRegionOverlayStatus Status { get; }
        public IReadOnlyList<OptionalRegionOverlayCell> Cells => cells;
        public IReadOnlyList<OptionalRegionOverlayConnection> Connections => connections;
        public IReadOnlyList<OptionalRegionOverlayLegendEntry> Legend => legend;
        public string SourceValidationDigest { get; }
        public string SourceInactiveDigest { get; }
        public string CanonicalDigest { get; }
        public int RngDrawCount { get; }
        public bool IsSuccess => Status == OptionalRegionOverlayStatus.Completed;

        internal static OptionalRegionOverlaySnapshot Failure(OptionalRegionOverlayStatus status)
        {
            return new OptionalRegionOverlaySnapshot(
                status,
                Array.Empty<OptionalRegionOverlayCell>(),
                Array.Empty<OptionalRegionOverlayConnection>(),
                Array.Empty<OptionalRegionOverlayLegendEntry>(),
                string.Empty,
                string.Empty,
                string.Empty,
                0);
        }
    }
}
