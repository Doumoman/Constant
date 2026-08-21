using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class VillageReservationCandidate
    {
        private readonly IReadOnlyList<int> occupiedSectorIndices;

        internal VillageReservationCandidate(
            string villageProfileId,
            string specialMapId,
            string layoutId,
            int layoutWeight,
            SectorCoord origin,
            int originIndex,
            int candidateOrdinal,
            int footprintWidthSectors,
            int footprintHeightSectors,
            IEnumerable<int> occupiedSectorIndices,
            SiteEntrySide entrySide,
            int entryFootprintSectorIndex,
            int entryExteriorSectorIndex,
            int startDistance,
            int bucketOrdinal)
        {
            if (!SitePlacementKey.IsCanonicalId(villageProfileId))
                throw new ArgumentException("A canonical Village profile ID is required.", nameof(villageProfileId));
            if (!SitePlacementKey.IsCanonicalId(specialMapId))
                throw new ArgumentException("A canonical special-map ID is required.", nameof(specialMapId));
            if (!SitePlacementKey.IsCanonicalId(layoutId))
                throw new ArgumentException("A canonical layout ID is required.", nameof(layoutId));
            if (layoutWeight <= 0) throw new ArgumentOutOfRangeException(nameof(layoutWeight));
            if (originIndex < 0 || originIndex >= WorldGenConstants.SectorCount ||
                WorldGridIndex.ToIndex(origin) != originIndex)
                throw new ArgumentOutOfRangeException(nameof(originIndex));
            if (candidateOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(candidateOrdinal));
            if (!SupportedDimensions(footprintWidthSectors, footprintHeightSectors))
                throw new ArgumentOutOfRangeException(nameof(footprintWidthSectors));
            if (!IsDefined(entrySide)) throw new ArgumentOutOfRangeException(nameof(entrySide));
            if (startDistance < 0) throw new ArgumentOutOfRangeException(nameof(startDistance));
            if (bucketOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(bucketOrdinal));
            if (occupiedSectorIndices == null) throw new ArgumentNullException(nameof(occupiedSectorIndices));

            var occupied = new List<int>(occupiedSectorIndices);
            occupied.Sort();
            if (occupied.Count != footprintWidthSectors * footprintHeightSectors)
                throw new ArgumentException("Occupied sectors must cover the full rectangle.", nameof(occupiedSectorIndices));
            var expected = new List<int>(occupied.Count);
            for (var localY = 0; localY < footprintHeightSectors; localY++)
                for (var localX = 0; localX < footprintWidthSectors; localX++)
                    expected.Add(WorldGridIndex.ToIndex(new SectorCoord(origin.X + localX, origin.Y + localY)));
            expected.Sort();
            for (var index = 0; index < expected.Count; index++)
                if (occupied[index] != expected[index])
                    throw new ArgumentException("Occupied sectors must equal the canonical rectangle.", nameof(occupiedSectorIndices));

            if (entryFootprintSectorIndex < 0 ||
                occupied.BinarySearch(entryFootprintSectorIndex) < 0)
                throw new ArgumentOutOfRangeException(nameof(entryFootprintSectorIndex));
            if (entryExteriorSectorIndex < 0 || entryExteriorSectorIndex >= WorldGenConstants.SectorCount ||
                occupied.BinarySearch(entryExteriorSectorIndex) >= 0)
                throw new ArgumentOutOfRangeException(nameof(entryExteriorSectorIndex));
            var footprintCoordinate = WorldGridIndex.ToCoordinate(entryFootprintSectorIndex);
            var exteriorCoordinate = WorldGridIndex.ToCoordinate(entryExteriorSectorIndex);
            SiteReservationTokenCodec.GetDelta(entrySide, out var deltaX, out var deltaY);
            if (exteriorCoordinate != new SectorCoord(
                    footprintCoordinate.X + deltaX, footprintCoordinate.Y + deltaY))
                throw new ArgumentException("Entry exterior must be one exact side step from its footprint sector.");

            VillageProfileId = villageProfileId;
            SpecialMapId = specialMapId;
            LayoutId = layoutId;
            LayoutWeight = layoutWeight;
            Origin = origin;
            OriginIndex = originIndex;
            CandidateOrdinal = candidateOrdinal;
            FootprintWidthSectors = footprintWidthSectors;
            FootprintHeightSectors = footprintHeightSectors;
            this.occupiedSectorIndices = new ReadOnlyCollection<int>(occupied);
            EntrySide = entrySide;
            EntryFootprintSectorIndex = entryFootprintSectorIndex;
            EntryExteriorSectorIndex = entryExteriorSectorIndex;
            StartDistance = startDistance;
            BucketOrdinal = bucketOrdinal;
        }

        public string VillageProfileId { get; }
        public string SpecialMapId { get; }
        public string LayoutId { get; }
        public int LayoutWeight { get; }
        public SectorCoord Origin { get; }
        public int OriginIndex { get; }
        public int CandidateOrdinal { get; }
        public int FootprintWidthSectors { get; }
        public int FootprintHeightSectors { get; }
        public IReadOnlyList<int> OccupiedSectorIndices => occupiedSectorIndices;
        public SiteEntrySide EntrySide { get; }
        public int EntryFootprintSectorIndex { get; }
        public int EntryExteriorSectorIndex { get; }
        public int StartDistance { get; }
        public int BucketOrdinal { get; }

        private static bool SupportedDimensions(int width, int height) =>
            (width == 1 && height == 1) || (width == 2 && height == 1) ||
            (width == 1 && height == 2);

        private static bool IsDefined(SiteEntrySide side) =>
            side == SiteEntrySide.L || side == SiteEntrySide.R ||
            side == SiteEntrySide.U || side == SiteEntrySide.D;
    }
}
