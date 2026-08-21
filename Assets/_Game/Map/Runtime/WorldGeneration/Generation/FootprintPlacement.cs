using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class FootprintPlacement
    {
        private readonly IReadOnlyList<SectorCoord> occupiedSectors;
        private readonly IReadOnlyList<FootprintPlacementEntry> entries;
        private readonly IReadOnlyDictionary<SectorCoord, SiteFootprintCell> cellsBySector;

        public FootprintPlacement(
            SiteOriginCandidate candidate,
            SiteFootprint footprint,
            IEnumerable<SectorCoord> occupiedSectors,
            IEnumerable<FootprintPlacementEntry> entries)
        {
            if (candidate == null) throw new ArgumentNullException(nameof(candidate));
            if (footprint == null) throw new ArgumentNullException(nameof(footprint));
            if (occupiedSectors == null) throw new ArgumentNullException(nameof(occupiedSectors));
            if (entries == null) throw new ArgumentNullException(nameof(entries));

            var occupied = new List<SectorCoord>(occupiedSectors);
            if (occupied.Count != footprint.Cells.Count)
                throw new ArgumentException("Occupied sectors must match the footprint cells.", nameof(occupiedSectors));
            var occupiedSet = new HashSet<SectorCoord>();
            foreach (var sector in occupied)
            {
                if (!IsWorldSector(sector)) throw new ArgumentOutOfRangeException(nameof(occupiedSectors));
                if (!occupiedSet.Add(sector))
                    throw new ArgumentException("Occupied sectors must be unique.", nameof(occupiedSectors));
            }

            var bySector = new Dictionary<SectorCoord, SiteFootprintCell>();
            foreach (var cell in footprint.Cells)
            {
                var expected = new SectorCoord(candidate.Origin.X + cell.LocalX, candidate.Origin.Y + cell.LocalY);
                if (!occupiedSet.Contains(expected))
                    throw new ArgumentException("Occupied sectors must equal candidate origin plus footprint cells.", nameof(occupiedSectors));
                bySector.Add(expected, cell);
            }
            occupied.Sort((left, right) => WorldGridIndex.ToIndex(left).CompareTo(WorldGridIndex.ToIndex(right)));

            var entryList = new List<FootprintPlacementEntry>(entries);
            var socketIds = new HashSet<string>(StringComparer.Ordinal);
            var faces = new HashSet<EntryFace>();
            foreach (var entry in entryList)
            {
                if (entry == null)
                    throw new ArgumentException("Placement entries cannot contain null.", nameof(entries));
                if (!socketIds.Add(entry.EntrySocketId))
                    throw new ArgumentException("Placement entry socket IDs must be unique.", nameof(entries));
                if (!footprint.TryGetCell(entry.LocalX, entry.LocalY, out _))
                    throw new ArgumentException("Placement entry must reference a footprint cell.", nameof(entries));
                var expected = new SectorCoord(candidate.Origin.X + entry.LocalX, candidate.Origin.Y + entry.LocalY);
                if (entry.FootprintSector != expected || !occupiedSet.Contains(expected))
                    throw new ArgumentException("Placement entry sector must match its local footprint cell.", nameof(entries));
                if (!faces.Add(new EntryFace(entry.FootprintSector, entry.Side)))
                    throw new ArgumentException("Placement entry faces must be unique.", nameof(entries));
            }
            entryList.Sort((left, right) =>
                string.Compare(left.EntrySocketId, right.EntrySocketId, StringComparison.Ordinal));

            Candidate = candidate;
            Footprint = footprint;
            this.occupiedSectors = new ReadOnlyCollection<SectorCoord>(occupied);
            this.entries = new ReadOnlyCollection<FootprintPlacementEntry>(entryList);
            cellsBySector = new ReadOnlyDictionary<SectorCoord, SiteFootprintCell>(bySector);
        }

        public SiteOriginCandidate Candidate { get; }
        public SiteFootprint Footprint { get; }
        public IReadOnlyList<SectorCoord> OccupiedSectors => occupiedSectors;
        public IReadOnlyList<FootprintPlacementEntry> Entries => entries;

        public bool TryGetFootprintCell(SectorCoord sector, out SiteFootprintCell cell)
        {
            return cellsBySector.TryGetValue(sector, out cell);
        }

        private static bool IsWorldSector(SectorCoord coordinate)
        {
            return coordinate.X >= 0 && coordinate.X < WorldGenConstants.SectorColumns &&
                   coordinate.Y >= 0 && coordinate.Y < WorldGenConstants.SectorRows;
        }

        private readonly struct EntryFace : IEquatable<EntryFace>
        {
            private readonly SectorCoord sector;
            private readonly SiteEntrySide side;

            public EntryFace(SectorCoord sector, SiteEntrySide side)
            {
                this.sector = sector;
                this.side = side;
            }

            public bool Equals(EntryFace other) => sector == other.sector && side == other.side;
            public override bool Equals(object obj) => obj is EntryFace other && Equals(other);
            public override int GetHashCode() => (sector.GetHashCode() * 397) ^ (int)side;
        }
    }
}
