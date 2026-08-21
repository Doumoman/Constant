using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class GeneratedBiomePatchRow
    {
        private readonly IReadOnlyList<SiteReservationId> specialMapInstanceIds;

        public GeneratedBiomePatchRow(
            ulong seed,
            BiomePatchId patchInstanceId,
            string biomeId,
            BiomePatchRole patchRole,
            int seedSectorX,
            int seedSectorY,
            int sectorCount,
            int minX,
            int minY,
            int maxX,
            int maxY,
            int perimeterEdges,
            IEnumerable<SiteReservationId> specialMapInstanceIds)
        {
            if (!patchInstanceId.IsValid)
                throw new ArgumentException("Patch instance ID must be valid.", nameof(patchInstanceId));
            ReservationValidation.RequireCanonicalId(biomeId, nameof(biomeId), false);
            if (!BiomePatchModelValidation.IsDefined(patchRole))
                throw new ArgumentOutOfRangeException(nameof(patchRole));
            ValidateCoordinate(seedSectorX, seedSectorY, nameof(seedSectorX));
            ValidateCoordinate(minX, minY, nameof(minX));
            ValidateCoordinate(maxX, maxY, nameof(maxX));
            if (sectorCount < 1) throw new ArgumentOutOfRangeException(nameof(sectorCount));
            if (minX > maxX || minY > maxY)
                throw new ArgumentException("Patch bounds must be ordered.");
            if (seedSectorX < minX || seedSectorX > maxX || seedSectorY < minY || seedSectorY > maxY)
                throw new ArgumentException("Representative seed must be inside patch bounds.");
            if (perimeterEdges < 1) throw new ArgumentOutOfRangeException(nameof(perimeterEdges));
            if (specialMapInstanceIds == null)
                throw new ArgumentNullException(nameof(specialMapInstanceIds));

            var ids = new List<SiteReservationId>(specialMapInstanceIds);
            ids.Sort();
            for (var index = 0; index < ids.Count; index++)
            {
                if (!ids[index].IsValid)
                    throw new ArgumentException("Special-map IDs must be valid.", nameof(specialMapInstanceIds));
                if (index > 0 && ids[index] == ids[index - 1])
                    throw new ArgumentException("Special-map IDs must be unique.", nameof(specialMapInstanceIds));
            }
            if (patchRole != BiomePatchRole.Core && ids.Count != 0)
                throw new ArgumentException("Only Core rows can contain special-map IDs.", nameof(specialMapInstanceIds));

            Seed = seed;
            PatchInstanceId = patchInstanceId;
            BiomeId = biomeId;
            PatchRole = patchRole;
            SeedSectorX = seedSectorX;
            SeedSectorY = seedSectorY;
            SectorCount = sectorCount;
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
            PerimeterEdges = perimeterEdges;
            this.specialMapInstanceIds = new ReadOnlyCollection<SiteReservationId>(ids);
        }

        public ulong Seed { get; }
        public BiomePatchId PatchInstanceId { get; }
        public string BiomeId { get; }
        public BiomePatchRole PatchRole { get; }
        public int SeedSectorX { get; }
        public int SeedSectorY { get; }
        public int SectorCount { get; }
        public int MinX { get; }
        public int MinY { get; }
        public int MaxX { get; }
        public int MaxY { get; }
        public int PerimeterEdges { get; }
        public IReadOnlyList<SiteReservationId> SpecialMapInstanceIds => specialMapInstanceIds;

        private static void ValidateCoordinate(int x, int y, string parameterName)
        {
            if (x < 0 || x >= Domain.WorldGenConstants.SectorColumns ||
                y < 0 || y >= Domain.WorldGenConstants.SectorRows)
                throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
