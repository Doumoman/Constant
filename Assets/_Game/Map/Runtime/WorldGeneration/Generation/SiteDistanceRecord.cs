using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SiteDistanceRecord
    {
        public SiteDistanceRecord(
            SitePlacementKey first,
            SitePlacementKey second,
            int distance,
            SectorCoord firstClosestSector,
            SectorCoord secondClosestSector,
            int firstClosestSectorIndex,
            int secondClosestSectorIndex)
        {
            if (!first.IsValid) throw new ArgumentException("A valid first key is required.", nameof(first));
            if (!second.IsValid) throw new ArgumentException("A valid second key is required.", nameof(second));
            if (first.CompareTo(second) >= 0)
                throw new ArgumentException("Distance record keys must be in canonical order.", nameof(second));
            if (distance < 1 || distance > 24) throw new ArgumentOutOfRangeException(nameof(distance));
            if (WorldGridIndex.ToIndex(firstClosestSector) != firstClosestSectorIndex)
                throw new ArgumentException("The first sector and index must match.", nameof(firstClosestSectorIndex));
            if (WorldGridIndex.ToIndex(secondClosestSector) != secondClosestSectorIndex)
                throw new ArgumentException("The second sector and index must match.", nameof(secondClosestSectorIndex));
            if (Math.Abs(firstClosestSector.X - secondClosestSector.X) +
                Math.Abs(firstClosestSector.Y - secondClosestSector.Y) != distance)
                throw new ArgumentException("Closest sectors must match the distance.", nameof(distance));

            First = first;
            Second = second;
            Distance = distance;
            FirstClosestSector = firstClosestSector;
            SecondClosestSector = secondClosestSector;
            FirstClosestSectorIndex = firstClosestSectorIndex;
            SecondClosestSectorIndex = secondClosestSectorIndex;
        }

        public SitePlacementKey First { get; }
        public SitePlacementKey Second { get; }
        public int Distance { get; }
        public SectorCoord FirstClosestSector { get; }
        public SectorCoord SecondClosestSector { get; }
        public int FirstClosestSectorIndex { get; }
        public int SecondClosestSectorIndex { get; }
    }

    internal readonly struct SitePlacementPairKey : IEquatable<SitePlacementPairKey>
    {
        public SitePlacementPairKey(SitePlacementKey first, SitePlacementKey second)
        {
            if (first.CompareTo(second) <= 0)
            {
                First = first;
                Second = second;
            }
            else
            {
                First = second;
                Second = first;
            }
        }

        public SitePlacementKey First { get; }
        public SitePlacementKey Second { get; }

        public bool Equals(SitePlacementPairKey other) =>
            First.Equals(other.First) && Second.Equals(other.Second);
        public override bool Equals(object obj) => obj is SitePlacementPairKey other && Equals(other);
        public override int GetHashCode()
        {
            unchecked { return (First.GetHashCode() * 397) ^ Second.GetHashCode(); }
        }
    }
}
