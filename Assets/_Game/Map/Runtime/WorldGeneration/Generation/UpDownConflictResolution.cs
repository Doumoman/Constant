using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class UpDownConflictResolution
    {
        private readonly IReadOnlyList<SectorCoord> spanCells;

        public UpDownConflictResolution(
            UpDownConflictId conflictId,
            VerticalGatewayId sourceGatewayId,
            VerticalGatewayAnchor upper,
            VerticalGatewayAnchor lower,
            IEnumerable<SectorCoord> inclusiveSpan,
            int checkedCost,
            string reason)
        {
            if (!conflictId.IsValid) throw new ArgumentException("Conflict identity must be valid.", nameof(conflictId));
            if (!sourceGatewayId.IsValid) throw new ArgumentException("Source gateway identity must be valid.", nameof(sourceGatewayId));
            Upper = upper ?? throw new ArgumentNullException(nameof(upper));
            Lower = lower ?? throw new ArgumentNullException(nameof(lower));
            if (!upper.IsUpperAnchor || lower.IsUpperAnchor || !upper.IsEndpointAdapter || !lower.IsEndpointAdapter)
                throw new ArgumentException("Resolution anchors must be an upper/lower adapter pair.");
            if (upper.Coord.X != lower.Coord.X || upper.Coord.Y <= lower.Coord.Y)
                throw new ArgumentException("Resolution anchors must form one descending column.");
            var cells = new List<SectorCoord>(inclusiveSpan ?? throw new ArgumentNullException(nameof(inclusiveSpan)));
            if (cells.Count != upper.Coord.Y - lower.Coord.Y + 1) throw new ArgumentException("Inclusive span count is invalid.", nameof(inclusiveSpan));
            for (var index = 0; index < cells.Count; index++)
                if (cells[index].X != upper.Coord.X || cells[index].Y != upper.Coord.Y - index)
                    throw new ArgumentException("Inclusive span must be ordered upper-to-lower.", nameof(inclusiveSpan));
            if (checkedCost < 0) throw new ArgumentOutOfRangeException(nameof(checkedCost));
            if (string.IsNullOrEmpty(reason)) throw new ArgumentException("Resolution reason is required.", nameof(reason));
            ConflictId = conflictId;
            SourceGatewayId = sourceGatewayId;
            CheckedCost = checkedCost;
            Reason = reason;
            spanCells = new ReadOnlyCollection<SectorCoord>(cells);
        }

        public UpDownConflictId ConflictId { get; }
        public VerticalGatewayId SourceGatewayId { get; }
        public VerticalGatewayAnchor Upper { get; }
        public VerticalGatewayAnchor Lower { get; }
        public IReadOnlyList<SectorCoord> InclusiveSpan => spanCells;
        public int VerticalDistance => Upper.Coord.Y - Lower.Coord.Y;
        public int CheckedCost { get; }
        public string Reason { get; }
    }
}
