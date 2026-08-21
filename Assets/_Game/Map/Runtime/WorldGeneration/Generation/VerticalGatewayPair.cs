using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public readonly struct VerticalGatewayJunctionCell : IEquatable<VerticalGatewayJunctionCell>
    {
        public VerticalGatewayJunctionCell(SectorCoord coord, bool opensLeft, bool opensRight)
        {
            if (coord.X < 0 || coord.X >= WorldGenConstants.SectorColumns || coord.Y < 0 || coord.Y >= WorldGenConstants.SectorRows)
                throw new ArgumentOutOfRangeException(nameof(coord));
            Coord = coord;
            OpensLeft = opensLeft;
            OpensRight = opensRight;
        }

        public SectorCoord Coord { get; }
        public bool OpensLeft { get; }
        public bool OpensRight { get; }
        public bool OpensUp => true;
        public bool OpensDown => true;
        public int RouteType => 4;

        public bool Equals(VerticalGatewayJunctionCell other) =>
            Coord == other.Coord && OpensLeft == other.OpensLeft && OpensRight == other.OpensRight;
        public override bool Equals(object obj) => obj is VerticalGatewayJunctionCell other && Equals(other);
        public override int GetHashCode() => ((Coord.GetHashCode() * 397) ^ (OpensLeft ? 1 : 0)) * 397 ^ (OpensRight ? 1 : 0);
        public static bool operator ==(VerticalGatewayJunctionCell left, VerticalGatewayJunctionCell right) => left.Equals(right);
        public static bool operator !=(VerticalGatewayJunctionCell left, VerticalGatewayJunctionCell right) => !left.Equals(right);
    }

    public sealed class VerticalGatewayPair
    {
        private readonly IReadOnlyList<SectorCoord> spanCells;
        private readonly IReadOnlyList<VerticalGatewayJunctionCell> junctionCells;

        public VerticalGatewayPair(
            VerticalGatewayId gatewayId,
            HorizontalBackboneSegmentId sourceSegmentId,
            VerticalGatewayAnchor upper,
            VerticalGatewayAnchor lower,
            int totalCost,
            bool requiresUpDownConflictResolution,
            IEnumerable<SectorCoord> spanCells,
            IEnumerable<VerticalGatewayJunctionCell> type4JunctionCells)
        {
            if (!gatewayId.IsValid) throw new ArgumentException("Gateway identity must be valid.", nameof(gatewayId));
            if (!sourceSegmentId.IsValid) throw new ArgumentException("Source segment identity must be valid.", nameof(sourceSegmentId));
            Upper = upper ?? throw new ArgumentNullException(nameof(upper));
            Lower = lower ?? throw new ArgumentNullException(nameof(lower));
            if (!upper.IsUpperAnchor || lower.IsUpperAnchor || !upper.OpensDown || upper.OpensUp || !lower.OpensUp || lower.OpensDown)
                throw new ArgumentException("Gateway anchor orientation is invalid.");
            if (upper.Coord.X != lower.Coord.X) throw new ArgumentException("Gateway anchors must share one column.");
            if (upper.Coord.Y <= lower.Coord.Y) throw new ArgumentException("Upper anchor must be above lower anchor.");
            if (totalCost < 0) throw new ArgumentOutOfRangeException(nameof(totalCost));

            var spans = new List<SectorCoord>(spanCells ?? throw new ArgumentNullException(nameof(spanCells)));
            var junctions = new List<VerticalGatewayJunctionCell>(type4JunctionCells ?? throw new ArgumentNullException(nameof(type4JunctionCells)));
            var expectedSpanCount = upper.Coord.Y - lower.Coord.Y + 1;
            if (spans.Count != expectedSpanCount || junctions.Count != expectedSpanCount - 2)
                throw new ArgumentException("Gateway span and Type4 interior counts are inconsistent.");
            for (var index = 0; index < spans.Count; index++)
            {
                if (spans[index].X != upper.Coord.X || spans[index].Y != upper.Coord.Y - index)
                    throw new ArgumentException("Gateway span must be inclusive and ordered upper-to-lower.", nameof(spanCells));
            }
            for (var index = 0; index < junctions.Count; index++)
            {
                if (junctions[index].Coord != spans[index + 1] || !junctions[index].OpensUp || !junctions[index].OpensDown || junctions[index].RouteType != 4)
                    throw new ArgumentException("Type4 junctions must exactly cover the interior span.", nameof(type4JunctionCells));
            }

            GatewayId = gatewayId;
            SourceSegmentId = sourceSegmentId;
            GatewayColumn = upper.Coord.X;
            VerticalDistance = upper.Coord.Y - lower.Coord.Y;
            TotalCost = totalCost;
            RequiresUpDownConflictResolution = requiresUpDownConflictResolution;
            this.spanCells = new ReadOnlyCollection<SectorCoord>(spans);
            junctionCells = new ReadOnlyCollection<VerticalGatewayJunctionCell>(junctions);
        }

        public VerticalGatewayId GatewayId { get; }
        public HorizontalBackboneSegmentId SourceSegmentId { get; }
        public VerticalGatewayAnchor Upper { get; }
        public VerticalGatewayAnchor Lower { get; }
        public int GatewayColumn { get; }
        public int VerticalDistance { get; }
        public int TotalCost { get; }
        public bool RequiresUpDownConflictResolution { get; }
        public IReadOnlyList<SectorCoord> SpanCells => spanCells;
        public IReadOnlyList<VerticalGatewayJunctionCell> Type4JunctionCells => junctionCells;
    }
}
