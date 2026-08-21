using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class BiomePatchSiteBinding
    {
        private readonly IReadOnlyList<int> occupiedSectorIndices;

        public BiomePatchSiteBinding(
            SiteReservationId siteReservationId,
            BiomePatchId patchId,
            string biomeId,
            IEnumerable<int> occupiedSectorIndices)
        {
            if (!siteReservationId.IsValid)
                throw new ArgumentException("Site reservation ID must be valid.", nameof(siteReservationId));
            if (!patchId.IsValid)
                throw new ArgumentException("Patch ID must be valid.", nameof(patchId));
            ReservationValidation.RequireCanonicalId(biomeId, nameof(biomeId), false);
            if (occupiedSectorIndices == null)
                throw new ArgumentNullException(nameof(occupiedSectorIndices));

            var indices = new List<int>(occupiedSectorIndices);
            if (indices.Count == 0)
                throw new ArgumentException("At least one occupied sector is required.", nameof(occupiedSectorIndices));
            var unique = new HashSet<int>();
            foreach (var index in indices)
            {
                if (index < 0 || index >= Domain.WorldGenConstants.SectorCount)
                    throw new ArgumentOutOfRangeException(nameof(occupiedSectorIndices));
                if (!unique.Add(index))
                    throw new ArgumentException("Occupied sector indices must be unique.", nameof(occupiedSectorIndices));
            }
            indices.Sort();

            SiteReservationId = siteReservationId;
            PatchId = patchId;
            BiomeId = biomeId;
            this.occupiedSectorIndices = new ReadOnlyCollection<int>(indices);
        }

        public SiteReservationId SiteReservationId { get; }
        public BiomePatchId PatchId { get; }
        public string BiomeId { get; }
        public IReadOnlyList<int> OccupiedSectorIndices => occupiedSectorIndices;
    }
}
