using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MultiSeedBiomeGrowthRecord
    {
        internal MultiSeedBiomeGrowthRecord(
            int sequence,
            BiomePatchId patchId,
            string biomeId,
            BiomePatchRole role,
            int sectorIndex,
            SectorCoord coordinate,
            int patchSizeBefore,
            int patchSizeAfter,
            bool wasMinimumPhase,
            BiomeGrowthCost cost)
        {
            if (sequence < 0) throw new ArgumentOutOfRangeException(nameof(sequence));
            if (!patchId.IsValid) throw new ArgumentException("Patch ID must be valid.", nameof(patchId));
            ReservationValidation.RequireCanonicalId(biomeId, nameof(biomeId), false);
            if (role != BiomePatchRole.Core && role != BiomePatchRole.Satellite)
                throw new ArgumentOutOfRangeException(nameof(role));
            BiomePatchModelValidation.ValidateGridIdentity(sectorIndex, coordinate);
            if (patchSizeBefore < 1 || patchSizeAfter != patchSizeBefore + 1)
                throw new ArgumentOutOfRangeException(nameof(patchSizeAfter));

            Sequence = sequence;
            PatchId = patchId;
            BiomeId = biomeId;
            Role = role;
            SectorIndex = sectorIndex;
            Coordinate = coordinate;
            PatchSizeBefore = patchSizeBefore;
            PatchSizeAfter = patchSizeAfter;
            WasMinimumPhase = wasMinimumPhase;
            Cost = cost ?? throw new ArgumentNullException(nameof(cost));
        }

        public int Sequence { get; }
        public BiomePatchId PatchId { get; }
        public string BiomeId { get; }
        public BiomePatchRole Role { get; }
        public int SectorIndex { get; }
        public SectorCoord Coordinate { get; }
        public int PatchSizeBefore { get; }
        public int PatchSizeAfter { get; }
        public bool WasMinimumPhase { get; }
        public BiomeGrowthCost Cost { get; }
    }
}
