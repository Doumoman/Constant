using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SiteFootprintCell
    {
        private readonly IReadOnlyList<SiteEntrySide> requiredOpenSides;

        public SiteFootprintCell(
            int localX,
            int localY,
            string localRole,
            string requiredPrimaryBiomeId,
            string fixedSectorRecipeId,
            IEnumerable<SiteEntrySide> requiredOpenSides)
        {
            if (localX < 0) throw new ArgumentOutOfRangeException(nameof(localX));
            if (localY < 0) throw new ArgumentOutOfRangeException(nameof(localY));
            ReservationValidation.RequireCanonicalId(localRole, nameof(localRole), false);
            ReservationValidation.RequireCanonicalId(requiredPrimaryBiomeId, nameof(requiredPrimaryBiomeId), true);
            ReservationValidation.RequireCanonicalId(fixedSectorRecipeId, nameof(fixedSectorRecipeId), true);
            if (requiredOpenSides == null) throw new ArgumentNullException(nameof(requiredOpenSides));

            var seen = new HashSet<SiteEntrySide>();
            foreach (var side in requiredOpenSides)
            {
                if (!IsDefined(side)) throw new ArgumentOutOfRangeException(nameof(requiredOpenSides));
                if (!seen.Add(side)) throw new ArgumentException("Required sides must be unique.", nameof(requiredOpenSides));
            }

            var ordered = new List<SiteEntrySide>();
            foreach (var side in new[] { SiteEntrySide.L, SiteEntrySide.R, SiteEntrySide.U, SiteEntrySide.D })
            {
                if (seen.Contains(side)) ordered.Add(side);
            }

            LocalX = localX;
            LocalY = localY;
            LocalRole = localRole;
            RequiredPrimaryBiomeId = requiredPrimaryBiomeId;
            FixedSectorRecipeId = fixedSectorRecipeId;
            this.requiredOpenSides = new ReadOnlyCollection<SiteEntrySide>(ordered);
        }

        public int LocalX { get; }
        public int LocalY { get; }
        public string LocalRole { get; }
        public string RequiredPrimaryBiomeId { get; }
        public string FixedSectorRecipeId { get; }
        public IReadOnlyList<SiteEntrySide> RequiredOpenSides => requiredOpenSides;

        private static bool IsDefined(SiteEntrySide value)
        {
            return value == SiteEntrySide.L || value == SiteEntrySide.R ||
                   value == SiteEntrySide.U || value == SiteEntrySide.D;
        }
    }

    public sealed class SiteFootprint
    {
        private readonly IReadOnlyList<SiteFootprintCell> cells;
        private readonly IReadOnlyDictionary<int, SiteFootprintCell> cellsByLocalIndex;

        public SiteFootprint(
            int width,
            int height,
            SiteFootprintTransform transform,
            IEnumerable<SiteFootprintCell> cells)
        {
            if (width < 1 || width > WorldGenConstants.SectorColumns) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 1 || height > WorldGenConstants.SectorRows) throw new ArgumentOutOfRangeException(nameof(height));
            if (!IsDefined(transform)) throw new ArgumentOutOfRangeException(nameof(transform));
            if (cells == null) throw new ArgumentNullException(nameof(cells));

            var snapshot = new List<SiteFootprintCell>(cells);
            if (snapshot.Count == 0) throw new ArgumentException("A footprint requires at least one cell.", nameof(cells));
            var byIndex = new Dictionary<int, SiteFootprintCell>();
            foreach (var cell in snapshot)
            {
                if (cell == null) throw new ArgumentException("Footprint cells cannot contain null.", nameof(cells));
                if (cell.LocalX >= width || cell.LocalY >= height) throw new ArgumentException("Footprint cell is outside its dimensions.", nameof(cells));
                var localIndex = cell.LocalY * width + cell.LocalX;
                if (!byIndex.TryAdd(localIndex, cell)) throw new ArgumentException("Footprint cell coordinates must be unique.", nameof(cells));
            }

            snapshot.Sort((left, right) =>
            {
                var y = left.LocalY.CompareTo(right.LocalY);
                return y != 0 ? y : left.LocalX.CompareTo(right.LocalX);
            });

            Width = width;
            Height = height;
            Transform = transform;
            this.cells = new ReadOnlyCollection<SiteFootprintCell>(snapshot);
            cellsByLocalIndex = new ReadOnlyDictionary<int, SiteFootprintCell>(byIndex);
        }

        public int Width { get; }
        public int Height { get; }
        public SiteFootprintTransform Transform { get; }
        public IReadOnlyList<SiteFootprintCell> Cells => cells;

        public bool TryGetCell(int localX, int localY, out SiteFootprintCell cell)
        {
            if (localX < 0 || localX >= Width || localY < 0 || localY >= Height)
            {
                cell = null;
                return false;
            }

            return cellsByLocalIndex.TryGetValue(localY * Width + localX, out cell);
        }

        private static bool IsDefined(SiteFootprintTransform value)
        {
            return value == SiteFootprintTransform.R0 || value == SiteFootprintTransform.MirrorX ||
                   value == SiteFootprintTransform.MirrorY || value == SiteFootprintTransform.R180;
        }
    }
}
