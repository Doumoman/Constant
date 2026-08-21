using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class CorePatchGrowthRecord
    {
        private readonly IReadOnlyList<int> footprintSectorIndices;
        private readonly IReadOnlyList<int> mandatoryBufferSectorIndices;
        private readonly IReadOnlyList<int> addedSectorIndices;
        private readonly IReadOnlyList<int> finalSectorIndices;

        internal CorePatchGrowthRecord(
            BiomePatchId patchId,
            SiteReservationId sourceReservationId,
            string biomeId,
            string corePatchRuleId,
            int initialSectorCount,
            int outsideTheoreticalBufferCount,
            int minimumSectorCount,
            int maximumSectorCount,
            int targetSectorCount,
            int mandatoryAddedSectorCount,
            int supplementalAddedSectorCount,
            int growthRoundCount,
            IEnumerable<int> footprintSectorIndices,
            IEnumerable<int> mandatoryBufferSectorIndices,
            IEnumerable<int> addedSectorIndices,
            IEnumerable<int> finalSectorIndices)
        {
            if (!patchId.IsValid) throw new ArgumentException("Patch ID must be valid.", nameof(patchId));
            if (!sourceReservationId.IsValid)
                throw new ArgumentException("Source reservation ID must be valid.", nameof(sourceReservationId));
            ReservationValidation.RequireCanonicalId(biomeId, nameof(biomeId), false);
            ReservationValidation.RequireCanonicalId(corePatchRuleId, nameof(corePatchRuleId), false);
            if (outsideTheoreticalBufferCount < 0)
                throw new ArgumentOutOfRangeException(nameof(outsideTheoreticalBufferCount));
            if (minimumSectorCount < 1 || maximumSectorCount < minimumSectorCount ||
                maximumSectorCount > WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(minimumSectorCount));
            if (targetSectorCount < minimumSectorCount || targetSectorCount > maximumSectorCount)
                throw new ArgumentOutOfRangeException(nameof(targetSectorCount));
            if (mandatoryAddedSectorCount < 0 || supplementalAddedSectorCount < 0 || growthRoundCount < 0)
                throw new ArgumentOutOfRangeException(nameof(mandatoryAddedSectorCount));

            var footprint = CopyUniqueSorted(footprintSectorIndices, nameof(footprintSectorIndices), false);
            var mandatory = CopyUniqueSorted(mandatoryBufferSectorIndices, nameof(mandatoryBufferSectorIndices), false);
            var added = CopyUniqueSorted(addedSectorIndices, nameof(addedSectorIndices), true);
            var final = CopyUniqueSorted(finalSectorIndices, nameof(finalSectorIndices), false);

            if (initialSectorCount != footprint.Count)
                throw new ArgumentException("Initial sectors must equal the source footprint.", nameof(initialSectorCount));
            if (final.Count != targetSectorCount)
                throw new ArgumentException("Final sector count must equal the growth target.", nameof(finalSectorIndices));
            if (!IsSubset(footprint, mandatory) || !IsSubset(mandatory, final))
                throw new ArgumentException("Footprint, mandatory buffer, and final sectors must be nested.");

            var expectedAdded = Difference(final, footprint);
            if (!SequenceEqual(expectedAdded, added))
                throw new ArgumentException("Added sectors must equal final minus initial.", nameof(addedSectorIndices));
            var expectedMandatoryAdded = mandatory.Count - footprint.Count;
            if (mandatoryAddedSectorCount != expectedMandatoryAdded ||
                mandatoryAddedSectorCount + supplementalAddedSectorCount != added.Count)
                throw new ArgumentException("Growth addition counts are inconsistent.");
            if (!IsCardinallyConnected(final))
                throw new ArgumentException("Final patch sectors must be cardinally connected.", nameof(finalSectorIndices));

            PatchId = patchId;
            SourceReservationId = sourceReservationId;
            BiomeId = biomeId;
            CorePatchRuleId = corePatchRuleId;
            InitialSectorCount = initialSectorCount;
            MandatoryBufferSectorCount = mandatory.Count;
            OutsideTheoreticalBufferCount = outsideTheoreticalBufferCount;
            MinimumSectorCount = minimumSectorCount;
            MaximumSectorCount = maximumSectorCount;
            TargetSectorCount = targetSectorCount;
            MandatoryAddedSectorCount = mandatoryAddedSectorCount;
            SupplementalAddedSectorCount = supplementalAddedSectorCount;
            FinalSectorCount = final.Count;
            GrowthRoundCount = growthRoundCount;
            this.footprintSectorIndices = new ReadOnlyCollection<int>(footprint);
            this.mandatoryBufferSectorIndices = new ReadOnlyCollection<int>(mandatory);
            this.addedSectorIndices = new ReadOnlyCollection<int>(added);
            this.finalSectorIndices = new ReadOnlyCollection<int>(final);
        }

        public BiomePatchId PatchId { get; }
        public SiteReservationId SourceReservationId { get; }
        public string BiomeId { get; }
        public string CorePatchRuleId { get; }
        public int InitialSectorCount { get; }
        public int MandatoryBufferSectorCount { get; }
        public int OutsideTheoreticalBufferCount { get; }
        public int MinimumSectorCount { get; }
        public int MaximumSectorCount { get; }
        public int TargetSectorCount { get; }
        public int MandatoryAddedSectorCount { get; }
        public int SupplementalAddedSectorCount { get; }
        public int FinalSectorCount { get; }
        public int GrowthRoundCount { get; }
        public IReadOnlyList<int> FootprintSectorIndices => footprintSectorIndices;
        public IReadOnlyList<int> MandatoryBufferSectorIndices => mandatoryBufferSectorIndices;
        public IReadOnlyList<int> AddedSectorIndices => addedSectorIndices;
        public IReadOnlyList<int> FinalSectorIndices => finalSectorIndices;

        private static List<int> CopyUniqueSorted(IEnumerable<int> source, string name, bool allowEmpty)
        {
            if (source == null) throw new ArgumentNullException(name);
            var values = new List<int>(source);
            if (!allowEmpty && values.Count == 0)
                throw new ArgumentException("Sector list cannot be empty.", name);
            var unique = new HashSet<int>();
            foreach (var value in values)
            {
                if (value < 0 || value >= WorldGenConstants.SectorCount)
                    throw new ArgumentOutOfRangeException(name);
                if (!unique.Add(value)) throw new ArgumentException("Sector indices must be unique.", name);
            }
            values.Sort();
            return values;
        }

        private static bool IsSubset(IReadOnlyList<int> subset, IReadOnlyList<int> superset)
        {
            var set = new HashSet<int>(superset);
            foreach (var value in subset) if (!set.Contains(value)) return false;
            return true;
        }

        private static List<int> Difference(IReadOnlyList<int> values, IReadOnlyList<int> excluded)
        {
            var excludedSet = new HashSet<int>(excluded);
            var result = new List<int>();
            foreach (var value in values) if (!excludedSet.Contains(value)) result.Add(value);
            return result;
        }

        private static bool SequenceEqual(IReadOnlyList<int> left, IReadOnlyList<int> right)
        {
            if (left.Count != right.Count) return false;
            for (var index = 0; index < left.Count; index++)
                if (left[index] != right[index]) return false;
            return true;
        }

        private static bool IsCardinallyConnected(IReadOnlyList<int> values)
        {
            var set = new HashSet<int>(values);
            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            queue.Enqueue(values[0]);
            visited.Add(values[0]);
            while (queue.Count != 0)
            {
                foreach (var neighbor in GetNeighbors(queue.Dequeue()))
                    if (set.Contains(neighbor) && visited.Add(neighbor)) queue.Enqueue(neighbor);
            }
            return visited.Count == set.Count;
        }

        private static IEnumerable<int> GetNeighbors(int index)
        {
            var values = new[]
            {
                WorldGridIndex.GetLeftIndex(index), WorldGridIndex.GetRightIndex(index),
                WorldGridIndex.GetUpIndex(index), WorldGridIndex.GetDownIndex(index)
            };
            Array.Sort(values);
            foreach (var value in values)
                if (value != SectorNeighborIndices.NoNeighbor) yield return value;
        }
    }
}
