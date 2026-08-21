using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalRegionSnapshot
    {
        public const int RequiredMandatoryNodeCount = 47;
        public const int RequiredMandatoryDirectedEdgeCount = 96;
        public const int RequiredMandatoryRouteCellCount = 47;

        private readonly IReadOnlyList<OptionalRegion> regions;
        private readonly IReadOnlyList<OptionalRegionCell> cells;
        private readonly IReadOnlyList<int> mandatoryRouteSectorIndices;

        public OptionalRegionSnapshot(
            IEnumerable<OptionalRegion> regions,
            IEnumerable<OptionalRegionCell> cells,
            IEnumerable<int> mandatoryRouteSectorIndices,
            int sourceMandatoryNodeCount,
            int sourceMandatoryDirectedEdgeCount,
            int sourceMandatoryRouteCellCount,
            string sourceMandatoryGraphDigest)
        {
            if (regions == null) throw new ArgumentNullException(nameof(regions));
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            if (mandatoryRouteSectorIndices == null) throw new ArgumentNullException(nameof(mandatoryRouteSectorIndices));
            if (sourceMandatoryNodeCount != RequiredMandatoryNodeCount)
                throw new ArgumentException("Mandatory node count must match the MAP05 known vector.", nameof(sourceMandatoryNodeCount));
            if (sourceMandatoryDirectedEdgeCount != RequiredMandatoryDirectedEdgeCount)
                throw new ArgumentException("Mandatory directed edge count must match the MAP05 known vector.", nameof(sourceMandatoryDirectedEdgeCount));
            if (sourceMandatoryRouteCellCount != RequiredMandatoryRouteCellCount)
                throw new ArgumentException("Mandatory route cell count must match the MAP05 known vector.", nameof(sourceMandatoryRouteCellCount));
            if (string.IsNullOrWhiteSpace(sourceMandatoryGraphDigest) ||
                !string.Equals(sourceMandatoryGraphDigest, sourceMandatoryGraphDigest.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Source mandatory graph digest must be a canonical non-empty identity.", nameof(sourceMandatoryGraphDigest));

            var mandatoryValues = new List<int>(mandatoryRouteSectorIndices);
            var mandatorySet = new HashSet<int>();
            foreach (var index in mandatoryValues)
            {
                if (index < 0 || index >= WorldGenConstants.SectorCount)
                    throw new ArgumentOutOfRangeException(nameof(mandatoryRouteSectorIndices));
                if (!mandatorySet.Add(index))
                    throw new ArgumentException("Mandatory route sector indices must be unique.", nameof(mandatoryRouteSectorIndices));
            }
            if (mandatoryValues.Count != RequiredMandatoryRouteCellCount)
                throw new ArgumentException("Mandatory route sector indices must contain exactly 47 entries.", nameof(mandatoryRouteSectorIndices));
            mandatoryValues.Sort();

            var regionValues = new List<OptionalRegion>(regions);
            var regionIds = new HashSet<OptionalRegionId>();
            foreach (var region in regionValues)
            {
                if (region == null) throw new ArgumentException("Optional regions cannot contain null.", nameof(regions));
                if (!regionIds.Add(region.RegionId))
                    throw new ArgumentException("Optional region IDs must be unique.", nameof(regions));
                if (!mandatorySet.Contains(region.Attachment.MandatoryRouteSectorIndex))
                    throw new ArgumentException("Every attachment must originate from the mandatory route.", nameof(regions));
            }
            regionValues.Sort((left, right) => left.RegionId.CompareTo(right.RegionId));

            var cellValues = new List<OptionalRegionCell>(cells);
            var optionalSectorIndices = new HashSet<int>();
            var cellsByRegion = new Dictionary<OptionalRegionId, List<OptionalRegionCell>>();
            foreach (var cell in cellValues)
            {
                if (cell == null) throw new ArgumentException("Optional cells cannot contain null.", nameof(cells));
                if (!regionIds.Contains(cell.RegionId))
                    throw new ArgumentException("Every optional cell must belong to a published region.", nameof(cells));
                if (!optionalSectorIndices.Add(cell.SectorIndex))
                    throw new ArgumentException("Optional sector indices must be globally unique.", nameof(cells));
                if (mandatorySet.Contains(cell.SectorIndex))
                    throw new ArgumentException("Optional cells cannot overlap mandatory route sectors.", nameof(cells));
                if (!cellsByRegion.TryGetValue(cell.RegionId, out var owned))
                {
                    owned = new List<OptionalRegionCell>();
                    cellsByRegion.Add(cell.RegionId, owned);
                }
                owned.Add(cell);
            }

            if ((regionValues.Count == 0) != (cellValues.Count == 0))
                throw new ArgumentException("Regions and cells must both be empty or both be populated.");

            foreach (var region in regionValues)
            {
                if (!cellsByRegion.TryGetValue(region.RegionId, out var published) ||
                    !SameCells(region.Cells, published))
                    throw new ArgumentException("Published cells must exactly match each region aggregate.", nameof(cells));
            }

            cellValues.Sort(ComparePublishedCells);
            this.regions = new ReadOnlyCollection<OptionalRegion>(regionValues);
            this.cells = new ReadOnlyCollection<OptionalRegionCell>(cellValues);
            this.mandatoryRouteSectorIndices = new ReadOnlyCollection<int>(mandatoryValues);
            SourceMandatoryNodeCount = sourceMandatoryNodeCount;
            SourceMandatoryDirectedEdgeCount = sourceMandatoryDirectedEdgeCount;
            SourceMandatoryRouteCellCount = sourceMandatoryRouteCellCount;
            SourceMandatoryGraphDigest = sourceMandatoryGraphDigest;
        }

        public IReadOnlyList<OptionalRegion> Regions => regions;
        public IReadOnlyList<OptionalRegionCell> Cells => cells;
        public IReadOnlyList<int> MandatoryRouteSectorIndices => mandatoryRouteSectorIndices;
        public int SourceMandatoryNodeCount { get; }
        public int SourceMandatoryDirectedEdgeCount { get; }
        public int SourceMandatoryRouteCellCount { get; }
        public string SourceMandatoryGraphDigest { get; }
        public bool IsEmpty => regions.Count == 0;

        private static bool SameCells(IReadOnlyList<OptionalRegionCell> expected, List<OptionalRegionCell> actual)
        {
            if (expected.Count != actual.Count) return false;
            actual.Sort(OptionalRegion.CompareCells);
            for (var index = 0; index < expected.Count; index++)
            {
                if (expected[index].RegionId != actual[index].RegionId ||
                    expected[index].SectorIndex != actual[index].SectorIndex ||
                    expected[index].Sector != actual[index].Sector ||
                    expected[index].Depth != actual[index].Depth ||
                    expected[index].IsAttachmentCell != actual[index].IsAttachmentCell ||
                    expected[index].RequiresReturnConnection != actual[index].RequiresReturnConnection)
                    return false;
            }
            return true;
        }

        private static int ComparePublishedCells(OptionalRegionCell left, OptionalRegionCell right)
        {
            var sector = OptionalRegion.CompareCells(left, right);
            return sector != 0 ? sector : left.RegionId.CompareTo(right.RegionId);
        }
    }
}
