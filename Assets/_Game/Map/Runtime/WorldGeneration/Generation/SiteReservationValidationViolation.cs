using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum SiteReservationValidationViolationCode
    {
        MissingRequiredReservation,
        UnexpectedReservation,
        RequiredCountMismatch,
        FootprintOutsideWorld,
        FootprintIdentityMismatch,
        FootprintOverlap,
        BlocksEntryApproach,
        EntryApproachOccupied,
        DistanceBelowMinimum,
        VillageDistanceBucketMismatch,
        CoreClusterViolation,
        MissingRequiredEntry,
        EntryIdentityMismatch,
        EntryOutsideWorld,
        EntryFacesOwnFootprint,
        EntryExteriorOccupied,
        EntryRouteTypeMismatch,
        MissingCapacityWitness,
        CapacityWitnessIdentityMismatch,
        CapacityWitnessDisconnected,
        CapacityWitnessOverlap,
        CapacityWitnessBlockedByVillage
    }

    public sealed class SiteReservationValidationViolation
    {
        public SiteReservationValidationViolation(
            SiteReservationValidationViolationCode code,
            SiteReservationValidationRule rule,
            string firstId,
            string secondId,
            int sectorIndex,
            int measuredValue,
            int expectedValue,
            string message)
        {
            if (!Enum.IsDefined(typeof(SiteReservationValidationViolationCode), code))
                throw new ArgumentOutOfRangeException(nameof(code));
            if (!Enum.IsDefined(typeof(SiteReservationValidationRule), rule))
                throw new ArgumentOutOfRangeException(nameof(rule));
            if (!CanonicalOrEmpty(firstId)) throw new ArgumentException("First ID must be canonical or empty.", nameof(firstId));
            if (!CanonicalOrEmpty(secondId)) throw new ArgumentException("Second ID must be canonical or empty.", nameof(secondId));
            if (sectorIndex < -1 || sectorIndex >= WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(sectorIndex));
            if (measuredValue < -1) throw new ArgumentOutOfRangeException(nameof(measuredValue));
            if (expectedValue < -1) throw new ArgumentOutOfRangeException(nameof(expectedValue));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("A stable non-empty message is required.", nameof(message));

            Code = code;
            Rule = rule;
            FirstId = firstId;
            SecondId = secondId;
            SectorIndex = sectorIndex;
            MeasuredValue = measuredValue;
            ExpectedValue = expectedValue;
            Message = message;
        }

        public SiteReservationValidationViolationCode Code { get; }
        public SiteReservationValidationRule Rule { get; }
        public string FirstId { get; }
        public string SecondId { get; }
        public int SectorIndex { get; }
        public int MeasuredValue { get; }
        public int ExpectedValue { get; }
        public string Message { get; }

        private static bool CanonicalOrEmpty(string value) =>
            value != null && (value.Length == 0 || SitePlacementKey.IsCanonicalId(value));
    }
}
