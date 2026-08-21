using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryConnectorCandidateEdge
    {
        public MandatoryConnectorCandidateEdge(
            MandatoryConnectorEdgeId edgeId,
            MandatoryRouteTerminalId fromTerminalId,
            MandatoryRouteTerminalId toTerminalId,
            int fromTerminalOrder,
            int toTerminalOrder,
            SectorCoord fromApproachSector,
            SectorCoord toApproachSector,
            MandatoryConnectorEdgeCost cost,
            bool isTreeEdge)
        {
            if (!edgeId.IsValid) throw new ArgumentException("Edge ID must be valid.", nameof(edgeId));
            if (!fromTerminalId.IsValid || !toTerminalId.IsValid) throw new ArgumentException("Endpoint IDs must be valid.");
            if (fromTerminalId == toTerminalId) throw new ArgumentException("Self-loops are not allowed.");
            if (fromTerminalOrder < 0 || toTerminalOrder < 0 ||
                fromTerminalOrder > toTerminalOrder ||
                (fromTerminalOrder == toTerminalOrder && fromTerminalId.CompareTo(toTerminalId) >= 0))
                throw new ArgumentException("Endpoint order must be canonical.");
            if (!IsWorldSector(fromApproachSector) || !IsWorldSector(toApproachSector))
                throw new ArgumentOutOfRangeException(nameof(fromApproachSector));
            var first = fromTerminalId.CompareTo(toTerminalId) < 0 ? fromTerminalId.Value : toTerminalId.Value;
            var second = fromTerminalId.CompareTo(toTerminalId) < 0 ? toTerminalId.Value : fromTerminalId.Value;
            if (!edgeId.Value.EndsWith("_" + first + "__TO__" + second, StringComparison.Ordinal))
                throw new ArgumentException("Edge ID endpoint identities do not match.", nameof(edgeId));
            if (cost.TotalCost < 0) throw new ArgumentException("Edge cost must be non-negative.", nameof(cost));
            EdgeId = edgeId;
            FromTerminalId = fromTerminalId;
            ToTerminalId = toTerminalId;
            FromTerminalOrder = fromTerminalOrder;
            ToTerminalOrder = toTerminalOrder;
            FromApproachSector = fromApproachSector;
            ToApproachSector = toApproachSector;
            Cost = cost;
            IsTreeEdge = isTreeEdge;
        }

        public MandatoryConnectorEdgeId EdgeId { get; }
        public MandatoryRouteTerminalId FromTerminalId { get; }
        public MandatoryRouteTerminalId ToTerminalId { get; }
        public int FromTerminalOrder { get; }
        public int ToTerminalOrder { get; }
        public SectorCoord FromApproachSector { get; }
        public SectorCoord ToApproachSector { get; }
        public MandatoryConnectorEdgeCost Cost { get; }
        public bool IsTreeEdge { get; }

        private static bool IsWorldSector(SectorCoord value) =>
            value.X >= 0 && value.X < WorldGenConstants.SectorColumns &&
            value.Y >= 0 && value.Y < WorldGenConstants.SectorRows;
    }
}
