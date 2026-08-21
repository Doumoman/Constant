using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class BiomeSectorOwnership
    {
        private BiomeSectorOwnership(
            int sectorIndex,
            SectorCoord sector,
            bool isAssigned,
            string primaryBiomeId,
            string secondaryBiomeId,
            BiomePatchId? patchId)
        {
            BiomePatchModelValidation.ValidateGridIdentity(sectorIndex, sector);
            SectorIndex = sectorIndex;
            Sector = sector;
            IsAssigned = isAssigned;
            PrimaryBiomeId = primaryBiomeId;
            SecondaryBiomeId = secondaryBiomeId;
            PatchId = patchId;
        }

        public BiomeSectorOwnership(
            int sectorIndex,
            SectorCoord sector,
            string primaryBiomeId,
            string secondaryBiomeId,
            BiomePatchId patchId)
        {
            BiomePatchModelValidation.ValidateGridIdentity(sectorIndex, sector);
            ReservationValidation.RequireCanonicalId(primaryBiomeId, nameof(primaryBiomeId), false);
            ReservationValidation.RequireCanonicalId(secondaryBiomeId, nameof(secondaryBiomeId), true);
            if (!patchId.IsValid)
                throw new ArgumentException("Patch ID must be valid.", nameof(patchId));
            if (secondaryBiomeId.Length > 0 && string.Equals(primaryBiomeId, secondaryBiomeId, StringComparison.Ordinal))
                throw new ArgumentException("Secondary biome must differ from primary biome.", nameof(secondaryBiomeId));

            SectorIndex = sectorIndex;
            Sector = sector;
            IsAssigned = true;
            PrimaryBiomeId = primaryBiomeId;
            SecondaryBiomeId = secondaryBiomeId;
            PatchId = patchId;
        }

        public int SectorIndex { get; }
        public SectorCoord Sector { get; }
        public bool IsAssigned { get; }
        public string PrimaryBiomeId { get; }
        public string SecondaryBiomeId { get; }
        public BiomePatchId? PatchId { get; }

        public static BiomeSectorOwnership CreateUnassigned(int sectorIndex, SectorCoord sector)
        {
            return new BiomeSectorOwnership(
                sectorIndex,
                sector,
                false,
                string.Empty,
                string.Empty,
                null);
        }
    }
}
