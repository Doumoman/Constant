using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class HorizontalBackbonePlan
    {
        private readonly IReadOnlyList<HorizontalBackboneSegment> segments;
        private readonly IReadOnlyDictionary<HorizontalBackboneSegmentId, HorizontalBackboneSegment> byId;
        private readonly IReadOnlyDictionary<MandatoryRouteTerminalId, IReadOnlyList<HorizontalBackboneSegment>> adjacency;

        internal HorizontalBackbonePlan(
            MandatoryConnectorTree sourceConnectorTree,
            MandatoryRouteMaskLookup sourceRouteMaskLookup,
            SiteReservationSnapshot sourceSiteSnapshot,
            BiomePatchValidationPublication sourceBiomePublication,
            IEnumerable<HorizontalBackboneSegment> segments)
        {
            SourceConnectorTree = sourceConnectorTree ?? throw new ArgumentNullException(nameof(sourceConnectorTree));
            SourceRouteMaskLookup = sourceRouteMaskLookup ?? throw new ArgumentNullException(nameof(sourceRouteMaskLookup));
            SourceSiteSnapshot = sourceSiteSnapshot ?? throw new ArgumentNullException(nameof(sourceSiteSnapshot));
            SourceBiomePublication = sourceBiomePublication ?? throw new ArgumentNullException(nameof(sourceBiomePublication));
            var values = new List<HorizontalBackboneSegment>(segments ?? throw new ArgumentNullException(nameof(segments)));
            values.Sort((left, right) => left.SegmentId.CompareTo(right.SegmentId));
            if (values.Count != 6) throw new ArgumentException("Exactly six horizontal backbone segments are required.", nameof(segments));
            var ids = new Dictionary<HorizontalBackboneSegmentId, HorizontalBackboneSegment>();
            var mutableAdjacency = new Dictionary<MandatoryRouteTerminalId, List<HorizontalBackboneSegment>>();
            foreach (var terminal in sourceConnectorTree.SourceTerminalSet.Terminals)
                mutableAdjacency.Add(terminal.TerminalId, new List<HorizontalBackboneSegment>());
            var totalCells = 0;
            var sameRows = 0;
            var pending = 0;
            var totalCost = 0;
            foreach (var segment in values)
            {
                if (segment == null || !ids.TryAdd(segment.SegmentId, segment)) throw new ArgumentException("Segment identities must be unique.", nameof(segments));
                if (!mutableAdjacency.TryGetValue(segment.FromTerminalId, out var from) || !mutableAdjacency.TryGetValue(segment.ToTerminalId, out var to))
                    throw new ArgumentException("Segment terminal is absent from source tree.", nameof(segments));
                from.Add(segment);
                to.Add(segment);
                totalCells = checked(totalCells + segment.Cells.Count);
                totalCost = checked(totalCost + segment.TotalCost);
                if (segment.IsSameRow) sameRows++;
                if (segment.RequiresVerticalGateway) pending++;
            }
            var frozenAdjacency = new Dictionary<MandatoryRouteTerminalId, IReadOnlyList<HorizontalBackboneSegment>>();
            foreach (var pair in mutableAdjacency)
            {
                pair.Value.Sort((left, right) => left.SegmentId.CompareTo(right.SegmentId));
                frozenAdjacency.Add(pair.Key, new ReadOnlyCollection<HorizontalBackboneSegment>(pair.Value));
            }
            this.segments = new ReadOnlyCollection<HorizontalBackboneSegment>(values);
            byId = new ReadOnlyDictionary<HorizontalBackboneSegmentId, HorizontalBackboneSegment>(ids);
            adjacency = new ReadOnlyDictionary<MandatoryRouteTerminalId, IReadOnlyList<HorizontalBackboneSegment>>(frozenAdjacency);
            TotalHorizontalCellCount = totalCells;
            SameRowSegmentCount = sameRows;
            GatewayPendingSegmentCount = pending;
            TotalCost = totalCost;
        }

        public MandatoryConnectorTree SourceConnectorTree { get; }
        public MandatoryRouteMaskLookup SourceRouteMaskLookup { get; }
        public SiteReservationSnapshot SourceSiteSnapshot { get; }
        public BiomePatchValidationPublication SourceBiomePublication { get; }
        public IReadOnlyList<HorizontalBackboneSegment> Segments => segments;
        public int SegmentCount => segments.Count;
        public int TotalHorizontalCellCount { get; }
        public int SameRowSegmentCount { get; }
        public int GatewayPendingSegmentCount { get; }
        public int TotalCost { get; }
        public bool TryGetSegment(HorizontalBackboneSegmentId id, out HorizontalBackboneSegment segment) => byId.TryGetValue(id, out segment);
        public IReadOnlyList<HorizontalBackboneSegment> GetSegmentsForTerminal(MandatoryRouteTerminalId terminalId) =>
            adjacency.TryGetValue(terminalId, out var values) ? values : Array.Empty<HorizontalBackboneSegment>();
    }
}
