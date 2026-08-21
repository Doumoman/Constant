using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalAttachmentEnumerationDiagnostics
    {
        private readonly IReadOnlyList<string> rejectionCodes;

        public OptionalAttachmentEnumerationDiagnostics(
            int rawNeighborProbes,
            int outOfBoundsRejected,
            int mandatoryRejected,
            int terminalRejected,
            int siteReservationRejected,
            int biomeReservedRejected,
            int duplicateEntryRejected,
            int acceptedCount,
            IEnumerable<string> rejectionCodes)
        {
            if (rawNeighborProbes < 0) throw new ArgumentOutOfRangeException(nameof(rawNeighborProbes));
            if (outOfBoundsRejected < 0) throw new ArgumentOutOfRangeException(nameof(outOfBoundsRejected));
            if (mandatoryRejected < 0) throw new ArgumentOutOfRangeException(nameof(mandatoryRejected));
            if (terminalRejected < 0) throw new ArgumentOutOfRangeException(nameof(terminalRejected));
            if (siteReservationRejected < 0) throw new ArgumentOutOfRangeException(nameof(siteReservationRejected));
            if (biomeReservedRejected < 0) throw new ArgumentOutOfRangeException(nameof(biomeReservedRejected));
            if (duplicateEntryRejected < 0) throw new ArgumentOutOfRangeException(nameof(duplicateEntryRejected));
            if (acceptedCount < 0) throw new ArgumentOutOfRangeException(nameof(acceptedCount));
            if (rejectionCodes == null) throw new ArgumentNullException(nameof(rejectionCodes));

            var rejectedCount = outOfBoundsRejected + mandatoryRejected + terminalRejected +
                siteReservationRejected + biomeReservedRejected + duplicateEntryRejected;
            if (rawNeighborProbes != rejectedCount + acceptedCount)
            {
                throw new ArgumentException("Probe accounting must equal accepted plus rejected outcomes.", nameof(rawNeighborProbes));
            }

            var codes = new List<string>(rejectionCodes);
            if (codes.Count != rejectedCount)
            {
                throw new ArgumentException("Rejection code count must equal rejected outcomes.", nameof(rejectionCodes));
            }

            foreach (var code in codes)
            {
                if (string.IsNullOrEmpty(code) || !string.Equals(code, code.Trim(), StringComparison.Ordinal))
                {
                    throw new ArgumentException("Rejection codes must be canonical non-empty tokens.", nameof(rejectionCodes));
                }
            }

            RawNeighborProbes = rawNeighborProbes;
            OutOfBoundsRejected = outOfBoundsRejected;
            MandatoryRejected = mandatoryRejected;
            TerminalRejected = terminalRejected;
            SiteReservationRejected = siteReservationRejected;
            BiomeReservedRejected = biomeReservedRejected;
            DuplicateEntryRejected = duplicateEntryRejected;
            AcceptedCount = acceptedCount;
            this.rejectionCodes = new ReadOnlyCollection<string>(codes);
        }

        public int RawNeighborProbes { get; }
        public int OutOfBoundsRejected { get; }
        public int MandatoryRejected { get; }
        public int TerminalRejected { get; }
        public int SiteReservationRejected { get; }
        public int BiomeReservedRejected { get; }
        public int DuplicateEntryRejected { get; }
        public int AcceptedCount { get; }
        public IReadOnlyList<string> RejectionCodes => rejectionCodes;
    }
}
