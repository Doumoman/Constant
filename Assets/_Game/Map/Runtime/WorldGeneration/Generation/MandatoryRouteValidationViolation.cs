using System;
using System.Globalization;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryRouteValidationViolation : IComparable<MandatoryRouteValidationViolation>
    {
        public MandatoryRouteValidationViolation(MandatoryRouteValidationRuleId ruleId, MandatoryRouteValidationSeverity severity,
            MandatoryRouteGraphNodeId graphNodeId, MandatoryRouteGraphEdgeId graphEdgeId, SectorCoord sectorCoordinate,
            int sectorIndex, string sourceArtifactId, string messageToken)
        {
            if (!ruleId.IsValid) throw new ArgumentException("A valid rule ID is required.", nameof(ruleId));
            if (!Enum.IsDefined(typeof(MandatoryRouteValidationSeverity), severity)) throw new ArgumentOutOfRangeException(nameof(severity));
            if (sectorIndex < -1 || sectorIndex >= WorldGenConstants.SectorCount) throw new ArgumentOutOfRangeException(nameof(sectorIndex));
            RuleId = ruleId;
            Severity = severity;
            GraphNodeId = graphNodeId;
            GraphEdgeId = graphEdgeId;
            SectorCoordinate = sectorCoordinate;
            SectorIndex = sectorIndex;
            SourceArtifactId = sourceArtifactId ?? throw new ArgumentNullException(nameof(sourceArtifactId));
            MessageToken = messageToken ?? throw new ArgumentNullException(nameof(messageToken));
            SortKey = ((int)severity).ToString("D2", CultureInfo.InvariantCulture) + "|" + ruleId.Value + "|" +
                sectorIndex.ToString("D4", CultureInfo.InvariantCulture) + "|" + (graphEdgeId.Value ?? string.Empty) + "|" +
                messageToken + "|" + (graphNodeId.Value ?? string.Empty) + "|" + SourceArtifactId;
        }

        public MandatoryRouteValidationRuleId RuleId { get; }
        public MandatoryRouteValidationSeverity Severity { get; }
        public MandatoryRouteGraphNodeId GraphNodeId { get; }
        public MandatoryRouteGraphEdgeId GraphEdgeId { get; }
        public SectorCoord SectorCoordinate { get; }
        public int SectorIndex { get; }
        public string SourceArtifactId { get; }
        public string MessageToken { get; }
        public string SortKey { get; }
        public int CompareTo(MandatoryRouteValidationViolation other) => other == null ? 1 : string.Compare(SortKey, other.SortKey, StringComparison.Ordinal);
    }
}
