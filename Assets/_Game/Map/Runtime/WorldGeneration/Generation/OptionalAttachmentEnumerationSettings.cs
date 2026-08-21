using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalAttachmentEnumerationSettings
    {
        public OptionalAttachmentEnumerationSettings()
            : this(9999, true, true, true, true)
        {
        }

        public OptionalAttachmentEnumerationSettings(
            int maxCandidates,
            bool excludeMandatoryTerminals,
            bool excludeSiteReservations,
            bool excludeBiomeReservedOrInactive,
            bool deduplicateEntrySector)
        {
            if (maxCandidates < 1 || maxCandidates > 9999)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCandidates));
            }

            MaxCandidates = maxCandidates;
            ExcludeMandatoryTerminals = excludeMandatoryTerminals;
            ExcludeSiteReservations = excludeSiteReservations;
            ExcludeBiomeReservedOrInactive = excludeBiomeReservedOrInactive;
            DeduplicateEntrySector = deduplicateEntrySector;
        }

        public int MaxCandidates { get; }
        public bool ExcludeMandatoryTerminals { get; }
        public bool ExcludeSiteReservations { get; }
        public bool ExcludeBiomeReservedOrInactive { get; }
        public bool DeduplicateEntrySector { get; }
    }
}
