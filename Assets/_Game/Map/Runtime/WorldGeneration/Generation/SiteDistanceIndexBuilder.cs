using System;
using System.Collections.Generic;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SiteDistanceIndexBuilder
    {
        public SiteDistanceIndexResult Build(IEnumerable<FootprintPlacement> placements)
        {
            if (placements == null)
            {
                return SiteDistanceIndexResult.Failure(new[]
                {
                    Error(SiteDistanceErrorCode.MissingPlacements, string.Empty, string.Empty, -1,
                        "A placement collection is required.")
                });
            }

            var errors = new List<SiteDistanceError>();
            var snapshots = new List<PlacementSnapshot>();
            foreach (var placement in placements)
            {
                if (placement == null)
                {
                    errors.Add(Error(SiteDistanceErrorCode.NullPlacement, string.Empty, string.Empty, -1,
                        "Placements cannot contain null."));
                    continue;
                }

                var source = placement.Candidate == null
                    ? string.Empty
                    : CanonicalOrEmpty(placement.Candidate.SourceDefinitionId);
                if (!TryValidatePlacement(placement, source, errors, out var snapshot)) continue;
                snapshots.Add(snapshot);
            }
            snapshots.Sort((left, right) => left.Key.CompareTo(right.Key));

            for (var index = 1; index < snapshots.Count; index++)
            {
                if (snapshots[index - 1].Key == snapshots[index].Key)
                {
                    errors.Add(Error(SiteDistanceErrorCode.DuplicatePlacementKey,
                        snapshots[index].Key.SourceDefinitionId,
                        snapshots[index].Key.SourceDefinitionId,
                        -1, "Placement keys must be unique."));
                }
            }

            var ownerBySector = new Dictionary<int, PlacementSnapshot>();
            foreach (var snapshot in snapshots)
            {
                foreach (var index in snapshot.SectorIndices)
                {
                    if (ownerBySector.TryGetValue(index, out var owner) && owner.Key != snapshot.Key)
                    {
                        errors.Add(Error(SiteDistanceErrorCode.OverlappingPlacements,
                            owner.Key.SourceDefinitionId,
                            snapshot.Key.SourceDefinitionId,
                            index, "Different placements cannot occupy the same sector."));
                    }
                    else if (!ownerBySector.ContainsKey(index))
                    {
                        ownerBySector.Add(index, snapshot);
                    }
                }
            }
            if (errors.Count != 0) return SiteDistanceIndexResult.Failure(errors);

            var keys = new List<SitePlacementKey>(snapshots.Count);
            foreach (var snapshot in snapshots) keys.Add(snapshot.Key);
            var records = new List<SiteDistanceRecord>();
            for (var first = 0; first < snapshots.Count; first++)
            {
                for (var second = first + 1; second < snapshots.Count; second++)
                {
                    records.Add(CreateRecord(snapshots[first], snapshots[second]));
                }
            }
            return SiteDistanceIndexResult.Success(new SiteDistanceIndex(keys, records));
        }

        private static bool TryValidatePlacement(
            FootprintPlacement placement,
            string source,
            ICollection<SiteDistanceError> errors,
            out PlacementSnapshot snapshot)
        {
            snapshot = null;
            if (placement.Candidate == null || placement.Footprint == null ||
                placement.OccupiedSectors == null || placement.OccupiedSectors.Count == 0)
            {
                errors.Add(Error(SiteDistanceErrorCode.InvalidPlacement, source, string.Empty, -1,
                    "A placement requires a candidate, footprint, and occupied sectors."));
                return false;
            }

            SitePlacementKey key;
            try { key = SitePlacementKey.FromPlacement(placement); }
            catch (ArgumentException)
            {
                errors.Add(Error(SiteDistanceErrorCode.InvalidPlacement, source, string.Empty, -1,
                    "A placement has invalid identity."));
                return false;
            }
            if (!key.IsValid || placement.OccupiedSectors.Count != placement.Footprint.Cells.Count)
            {
                errors.Add(Error(SiteDistanceErrorCode.InvalidPlacement, source, string.Empty, -1,
                    "A placement has invalid identity or footprint cardinality."));
                return false;
            }

            var indices = new List<int>(placement.OccupiedSectors.Count);
            var seen = new HashSet<int>();
            foreach (var sector in placement.OccupiedSectors)
            {
                if (!IsWorldSector(sector))
                {
                    errors.Add(Error(SiteDistanceErrorCode.InvalidOccupiedSector,
                        key.SourceDefinitionId, string.Empty, -1,
                        "An occupied sector is outside the world."));
                    continue;
                }
                var index = WorldGridIndex.ToIndex(sector);
                if (!seen.Add(index))
                {
                    errors.Add(Error(SiteDistanceErrorCode.InvalidOccupiedSector,
                        key.SourceDefinitionId, string.Empty, index,
                        "Occupied sectors must be unique."));
                    continue;
                }
                indices.Add(index);
            }
            if (indices.Count != placement.OccupiedSectors.Count) return false;
            indices.Sort();
            snapshot = new PlacementSnapshot(key, indices);
            return true;
        }

        private static SiteDistanceRecord CreateRecord(
            PlacementSnapshot first,
            PlacementSnapshot second)
        {
            var bestDistance = int.MaxValue;
            var bestFirst = -1;
            var bestSecond = -1;
            foreach (var firstIndex in first.SectorIndices)
            {
                var firstSector = WorldGridIndex.ToCoordinate(firstIndex);
                foreach (var secondIndex in second.SectorIndices)
                {
                    var secondSector = WorldGridIndex.ToCoordinate(secondIndex);
                    var distance = Math.Abs(firstSector.X - secondSector.X) +
                                   Math.Abs(firstSector.Y - secondSector.Y);
                    if (distance < bestDistance ||
                        (distance == bestDistance &&
                         (firstIndex < bestFirst ||
                          (firstIndex == bestFirst && secondIndex < bestSecond))))
                    {
                        bestDistance = distance;
                        bestFirst = firstIndex;
                        bestSecond = secondIndex;
                    }
                }
            }
            return new SiteDistanceRecord(
                first.Key,
                second.Key,
                bestDistance,
                WorldGridIndex.ToCoordinate(bestFirst),
                WorldGridIndex.ToCoordinate(bestSecond),
                bestFirst,
                bestSecond);
        }

        private static bool IsWorldSector(SectorCoord sector) =>
            sector.X >= 0 && sector.X < WorldGenConstants.SectorColumns &&
            sector.Y >= 0 && sector.Y < WorldGenConstants.SectorRows;
        private static string CanonicalOrEmpty(string value) =>
            SitePlacementKey.IsCanonicalId(value) ? value : string.Empty;
        private static SiteDistanceError Error(
            SiteDistanceErrorCode code,
            string first,
            string second,
            int sector,
            string message) => new SiteDistanceError(code, first, second, sector, message);

        private sealed class PlacementSnapshot
        {
            public PlacementSnapshot(SitePlacementKey key, IReadOnlyList<int> sectorIndices)
            {
                Key = key;
                SectorIndices = new List<int>(sectorIndices).AsReadOnly();
            }
            public SitePlacementKey Key { get; }
            public IReadOnlyList<int> SectorIndices { get; }
        }
    }
}
