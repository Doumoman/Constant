using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class IntrusionPlacementRecord
    {
        internal IntrusionPlacementRecord(
            int sequence,
            string intrusionRuleId,
            string intruderBiomeId,
            int intrusionOrdinal,
            BiomePatchId intrusionPatchId,
            int sectorIndex,
            SectorCoord coordinate,
            string hostBiomeId,
            BiomePatchId donorPatchId,
            BiomePatchRole donorRole,
            int donorSizeBefore,
            int donorSizeAfter,
            string boundaryPairRuleId,
            string boundaryProfileId,
            int sharedIntruderEdgeCount,
            int anchorSectorIndex,
            int candidateCountBeforeDraw,
            int candidateRoll,
            int sameRuleNearestIntrusionDistance)
        {
            if (sequence < 0) throw new ArgumentOutOfRangeException(nameof(sequence));
            ReservationValidation.RequireCanonicalId(intrusionRuleId, nameof(intrusionRuleId), false);
            ReservationValidation.RequireCanonicalId(intruderBiomeId, nameof(intruderBiomeId), false);
            if (intrusionOrdinal < 0 || intrusionOrdinal > 99)
                throw new ArgumentOutOfRangeException(nameof(intrusionOrdinal));
            if (!intrusionPatchId.IsValid || !donorPatchId.IsValid)
                throw new ArgumentException("Patch IDs must be valid.");
            BiomePatchModelValidation.ValidateGridIdentity(sectorIndex, coordinate);
            ReservationValidation.RequireCanonicalId(hostBiomeId, nameof(hostBiomeId), false);
            if (donorRole != BiomePatchRole.Core && donorRole != BiomePatchRole.Satellite)
                throw new ArgumentOutOfRangeException(nameof(donorRole));
            if (donorSizeBefore < 2 || donorSizeAfter != donorSizeBefore - 1)
                throw new ArgumentOutOfRangeException(nameof(donorSizeAfter));
            ReservationValidation.RequireCanonicalId(boundaryPairRuleId, nameof(boundaryPairRuleId), false);
            ReservationValidation.RequireCanonicalId(boundaryProfileId, nameof(boundaryProfileId), false);
            if (sharedIntruderEdgeCount < 1 || anchorSectorIndex < 0 ||
                candidateCountBeforeDraw < 1 || candidateRoll < 0 ||
                candidateRoll >= candidateCountBeforeDraw ||
                sameRuleNearestIntrusionDistance < -1)
                throw new ArgumentOutOfRangeException(nameof(candidateRoll));

            Sequence = sequence;
            IntrusionRuleId = intrusionRuleId;
            IntruderBiomeId = intruderBiomeId;
            IntrusionOrdinal = intrusionOrdinal;
            IntrusionPatchId = intrusionPatchId;
            SectorIndex = sectorIndex;
            Coordinate = coordinate;
            HostBiomeId = hostBiomeId;
            DonorPatchId = donorPatchId;
            DonorRole = donorRole;
            DonorSizeBefore = donorSizeBefore;
            DonorSizeAfter = donorSizeAfter;
            BoundaryPairRuleId = boundaryPairRuleId;
            BoundaryProfileId = boundaryProfileId;
            SharedIntruderEdgeCount = sharedIntruderEdgeCount;
            AnchorSectorIndex = anchorSectorIndex;
            CandidateCountBeforeDraw = candidateCountBeforeDraw;
            CandidateRoll = candidateRoll;
            SameRuleNearestIntrusionDistance = sameRuleNearestIntrusionDistance;
        }

        public int Sequence { get; }
        public string IntrusionRuleId { get; }
        public string IntruderBiomeId { get; }
        public int IntrusionOrdinal { get; }
        public BiomePatchId IntrusionPatchId { get; }
        public int SectorIndex { get; }
        public SectorCoord Coordinate { get; }
        public string HostBiomeId { get; }
        public BiomePatchId DonorPatchId { get; }
        public BiomePatchRole DonorRole { get; }
        public int DonorSizeBefore { get; }
        public int DonorSizeAfter { get; }
        public string BoundaryPairRuleId { get; }
        public string BoundaryProfileId { get; }
        public int SharedIntruderEdgeCount { get; }
        public int AnchorSectorIndex { get; }
        public int CandidateCountBeforeDraw { get; }
        public int CandidateRoll { get; }
        public int SameRuleNearestIntrusionDistance { get; }
    }
}
