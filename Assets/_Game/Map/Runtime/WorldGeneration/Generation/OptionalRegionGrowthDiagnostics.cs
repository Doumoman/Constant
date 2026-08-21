using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalRegionGrowthDiagnostics
    {
        private readonly IReadOnlyList<string> rejectionCodes;

        public OptionalRegionGrowthDiagnostics(
            int sourceCandidateCount,
            int attemptedCandidates,
            int acceptedRegionCount,
            int rejectedCandidateCount,
            int regionLimitSkipped,
            int acceptedCellCount,
            int rawCellProbes,
            int outOfBoundsCellRejected,
            int mandatoryCellRejected,
            int additionalMandatoryBridgeRejected,
            int siteReservationCellRejected,
            int biomeReservedCellRejected,
            int claimedCellRejected,
            int duplicateFrontierRejected,
            int horizontalThroughCellRejected,
            int noTargetDepthPathRejected,
            int depth1RegionCount,
            int depth2RegionCount,
            int depth3RegionCount,
            int depth4RegionCount,
            IEnumerable<string> rejectionCodes)
        {
            var values = new[]
            {
                sourceCandidateCount, attemptedCandidates, acceptedRegionCount, rejectedCandidateCount,
                regionLimitSkipped, acceptedCellCount, rawCellProbes, outOfBoundsCellRejected,
                mandatoryCellRejected, additionalMandatoryBridgeRejected, siteReservationCellRejected,
                biomeReservedCellRejected, claimedCellRejected, duplicateFrontierRejected,
                horizontalThroughCellRejected, noTargetDepthPathRejected, depth1RegionCount,
                depth2RegionCount, depth3RegionCount, depth4RegionCount
            };
            foreach (var value in values)
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(sourceCandidateCount));
            if (sourceCandidateCount != attemptedCandidates + regionLimitSkipped)
                throw new ArgumentException("Source candidate accounting is inconsistent.", nameof(sourceCandidateCount));
            if (attemptedCandidates != acceptedRegionCount + rejectedCandidateCount)
                throw new ArgumentException("Attempted candidate accounting is inconsistent.", nameof(attemptedCandidates));
            if (acceptedRegionCount != depth1RegionCount + depth2RegionCount + depth3RegionCount + depth4RegionCount)
                throw new ArgumentException("Depth bucket accounting is inconsistent.", nameof(acceptedRegionCount));
            if (rejectionCodes == null) throw new ArgumentNullException(nameof(rejectionCodes));
            var codes = new List<string>(rejectionCodes);
            if (codes.Count != rejectedCandidateCount)
                throw new ArgumentException("One rejection code is required per rejected candidate.", nameof(rejectionCodes));
            foreach (var code in codes)
                if (string.IsNullOrEmpty(code) || !string.Equals(code, code.Trim(), StringComparison.Ordinal))
                    throw new ArgumentException("Rejection codes must be canonical non-empty tokens.", nameof(rejectionCodes));

            SourceCandidateCount = sourceCandidateCount;
            AttemptedCandidates = attemptedCandidates;
            AcceptedRegionCount = acceptedRegionCount;
            RejectedCandidateCount = rejectedCandidateCount;
            RegionLimitSkipped = regionLimitSkipped;
            AcceptedCellCount = acceptedCellCount;
            RawCellProbes = rawCellProbes;
            OutOfBoundsCellRejected = outOfBoundsCellRejected;
            MandatoryCellRejected = mandatoryCellRejected;
            AdditionalMandatoryBridgeRejected = additionalMandatoryBridgeRejected;
            SiteReservationCellRejected = siteReservationCellRejected;
            BiomeReservedCellRejected = biomeReservedCellRejected;
            ClaimedCellRejected = claimedCellRejected;
            DuplicateFrontierRejected = duplicateFrontierRejected;
            HorizontalThroughCellRejected = horizontalThroughCellRejected;
            NoTargetDepthPathRejected = noTargetDepthPathRejected;
            Depth1RegionCount = depth1RegionCount;
            Depth2RegionCount = depth2RegionCount;
            Depth3RegionCount = depth3RegionCount;
            Depth4RegionCount = depth4RegionCount;
            this.rejectionCodes = new ReadOnlyCollection<string>(codes);
        }

        public int SourceCandidateCount { get; }
        public int AttemptedCandidates { get; }
        public int AcceptedRegionCount { get; }
        public int RejectedCandidateCount { get; }
        public int RegionLimitSkipped { get; }
        public int AcceptedCellCount { get; }
        public int RawCellProbes { get; }
        public int OutOfBoundsCellRejected { get; }
        public int MandatoryCellRejected { get; }
        public int AdditionalMandatoryBridgeRejected { get; }
        public int SiteReservationCellRejected { get; }
        public int BiomeReservedCellRejected { get; }
        public int ClaimedCellRejected { get; }
        public int DuplicateFrontierRejected { get; }
        public int HorizontalThroughCellRejected { get; }
        public int NoTargetDepthPathRejected { get; }
        public int Depth1RegionCount { get; }
        public int Depth2RegionCount { get; }
        public int Depth3RegionCount { get; }
        public int Depth4RegionCount { get; }
        public IReadOnlyList<string> RejectionCodes => rejectionCodes;
    }
}
