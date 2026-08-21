namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryRouteMaskLookupDiagnostics
    {
        internal MandatoryRouteMaskLookupDiagnostics(int sourceRouteMaskCount, int activeRouteMaskCount,
            int mandatoryAllowedRouteMaskCount, int acceptedMandatoryMaskCount, int type1Count, int type2Count,
            int type3Count, int ignoredType0Count, int rejectedMandatoryCandidateCount, int rngDrawCount, int sourceMutationCount)
        {
            SourceRouteMaskCount = sourceRouteMaskCount;
            ActiveRouteMaskCount = activeRouteMaskCount;
            MandatoryAllowedRouteMaskCount = mandatoryAllowedRouteMaskCount;
            AcceptedMandatoryMaskCount = acceptedMandatoryMaskCount;
            Type1Count = type1Count;
            Type2Count = type2Count;
            Type3Count = type3Count;
            IgnoredType0Count = ignoredType0Count;
            RejectedMandatoryCandidateCount = rejectedMandatoryCandidateCount;
            RngDrawCount = rngDrawCount;
            SourceMutationCount = sourceMutationCount;
        }
        public int SourceRouteMaskCount { get; }
        public int ActiveRouteMaskCount { get; }
        public int MandatoryAllowedRouteMaskCount { get; }
        public int AcceptedMandatoryMaskCount { get; }
        public int Type1Count { get; }
        public int Type2Count { get; }
        public int Type3Count { get; }
        public int IgnoredType0Count { get; }
        public int RejectedMandatoryCandidateCount { get; }
        public int RngDrawCount { get; }
        public int SourceMutationCount { get; }
    }
}
