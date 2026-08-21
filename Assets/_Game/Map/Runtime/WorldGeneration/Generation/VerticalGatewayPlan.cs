using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class VerticalGatewayPlan
    {
        private readonly IReadOnlyList<VerticalGatewayPair> pairs;
        private readonly IReadOnlyDictionary<VerticalGatewayId, VerticalGatewayPair> byId;
        private readonly IReadOnlyDictionary<HorizontalBackboneSegmentId, IReadOnlyList<VerticalGatewayPair>> bySegment;

        internal VerticalGatewayPlan(
            HorizontalBackbonePlan sourceHorizontalPlan,
            MandatoryRouteMaskLookup sourceRouteMaskLookup,
            SiteReservationSnapshot sourceSiteSnapshot,
            BiomePatchValidationPublication sourceBiomePublication,
            IEnumerable<VerticalGatewayPair> gatewayPairs)
        {
            SourceHorizontalPlan = sourceHorizontalPlan ?? throw new ArgumentNullException(nameof(sourceHorizontalPlan));
            SourceRouteMaskLookup = sourceRouteMaskLookup ?? throw new ArgumentNullException(nameof(sourceRouteMaskLookup));
            SourceSiteSnapshot = sourceSiteSnapshot ?? throw new ArgumentNullException(nameof(sourceSiteSnapshot));
            SourceBiomePublication = sourceBiomePublication ?? throw new ArgumentNullException(nameof(sourceBiomePublication));
            var values = new List<VerticalGatewayPair>(gatewayPairs ?? throw new ArgumentNullException(nameof(gatewayPairs)));
            values.Sort((left, right) => left.GatewayId.CompareTo(right.GatewayId));
            if (values.Count != 4) throw new ArgumentException("Exactly four vertical gateway pairs are required.", nameof(gatewayPairs));
            var ids = new Dictionary<VerticalGatewayId, VerticalGatewayPair>();
            var mutableBySegment = new Dictionary<HorizontalBackboneSegmentId, List<VerticalGatewayPair>>();
            var totalJunctions = 0;
            var conflicts = 0;
            var totalSpans = 0;
            var totalCost = 0;
            foreach (var pair in values)
            {
                if (pair == null || !ids.TryAdd(pair.GatewayId, pair)) throw new ArgumentException("Gateway identities must be unique.", nameof(gatewayPairs));
                if (!mutableBySegment.TryGetValue(pair.SourceSegmentId, out var segmentPairs))
                {
                    segmentPairs = new List<VerticalGatewayPair>();
                    mutableBySegment.Add(pair.SourceSegmentId, segmentPairs);
                }
                segmentPairs.Add(pair);
                totalJunctions = checked(totalJunctions + pair.Type4JunctionCells.Count);
                if (pair.RequiresUpDownConflictResolution) conflicts++;
                totalSpans = checked(totalSpans + pair.SpanCells.Count);
                totalCost = checked(totalCost + pair.TotalCost);
            }
            var frozenBySegment = new Dictionary<HorizontalBackboneSegmentId, IReadOnlyList<VerticalGatewayPair>>();
            foreach (var item in mutableBySegment)
            {
                item.Value.Sort((left, right) => left.GatewayId.CompareTo(right.GatewayId));
                frozenBySegment.Add(item.Key, new ReadOnlyCollection<VerticalGatewayPair>(item.Value));
            }
            pairs = new ReadOnlyCollection<VerticalGatewayPair>(values);
            byId = new ReadOnlyDictionary<VerticalGatewayId, VerticalGatewayPair>(ids);
            bySegment = new ReadOnlyDictionary<HorizontalBackboneSegmentId, IReadOnlyList<VerticalGatewayPair>>(frozenBySegment);
            PendingSegmentCount = sourceHorizontalPlan.GatewayPendingSegmentCount;
            Type4JunctionCellCount = totalJunctions;
            ConflictPendingCount = conflicts;
            TotalVerticalSpanCellCount = totalSpans;
            TotalCost = totalCost;
        }

        public HorizontalBackbonePlan SourceHorizontalPlan { get; }
        public MandatoryRouteMaskLookup SourceRouteMaskLookup { get; }
        public SiteReservationSnapshot SourceSiteSnapshot { get; }
        public BiomePatchValidationPublication SourceBiomePublication { get; }
        public IReadOnlyList<VerticalGatewayPair> GatewayPairs => pairs;
        public int GatewayPairCount => pairs.Count;
        public int PendingSegmentCount { get; }
        public int UpperAnchorCount => pairs.Count;
        public int LowerAnchorCount => pairs.Count;
        public int Type4JunctionCellCount { get; }
        public int ConflictPendingCount { get; }
        public int TotalVerticalSpanCellCount { get; }
        public int TotalCost { get; }
        public bool TryGetPair(VerticalGatewayId id, out VerticalGatewayPair pair) => byId.TryGetValue(id, out pair);
        public IReadOnlyList<VerticalGatewayPair> GetPairsForSegment(HorizontalBackboneSegmentId segmentId) =>
            bySegment.TryGetValue(segmentId, out var values) ? values : Array.Empty<VerticalGatewayPair>();
    }
}
