namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryTerminalBuildDiagnostics
    {
        internal MandatoryTerminalBuildDiagnostics(
            ulong worldSeed,
            int reservationCount,
            int reservedSectorCount,
            int biomePatchCount,
            int biomeAssignedSectorCount,
            int biomeUnassignedSectorCount,
            int terminalCount,
            int startTerminalCount,
            int siteEntryTerminalCount,
            int requiredTerminalCount,
            int returnPathRequiredTerminalCount,
            int sharedApproachSectorCount,
            int rngDrawCount,
            int sourceMutationCount)
        {
            WorldSeed = worldSeed;
            ReservationCount = reservationCount;
            ReservedSectorCount = reservedSectorCount;
            BiomePatchCount = biomePatchCount;
            BiomeAssignedSectorCount = biomeAssignedSectorCount;
            BiomeUnassignedSectorCount = biomeUnassignedSectorCount;
            TerminalCount = terminalCount;
            StartTerminalCount = startTerminalCount;
            SiteEntryTerminalCount = siteEntryTerminalCount;
            RequiredTerminalCount = requiredTerminalCount;
            ReturnPathRequiredTerminalCount = returnPathRequiredTerminalCount;
            SharedApproachSectorCount = sharedApproachSectorCount;
            RngDrawCount = rngDrawCount;
            SourceMutationCount = sourceMutationCount;
        }

        public ulong WorldSeed { get; }
        public int ReservationCount { get; }
        public int ReservedSectorCount { get; }
        public int BiomePatchCount { get; }
        public int BiomeAssignedSectorCount { get; }
        public int BiomeUnassignedSectorCount { get; }
        public int TerminalCount { get; }
        public int StartTerminalCount { get; }
        public int SiteEntryTerminalCount { get; }
        public int RequiredTerminalCount { get; }
        public int ReturnPathRequiredTerminalCount { get; }
        public int SharedApproachSectorCount { get; }
        public int RngDrawCount { get; }
        public int SourceMutationCount { get; }
    }
}
