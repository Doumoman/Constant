using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum PatchCleanupMoveKind
    {
        CheckerboardCollapse,
        NeckCollapse,
        NeckWiden
    }

    public readonly struct PatchCleanupScore : IEquatable<PatchCleanupScore>, IComparable<PatchCleanupScore>
    {
        public PatchCleanupScore(int checkerboardCount, int neckCount, int crossPatchUndirectedEdgeCount)
        {
            if (checkerboardCount < 0 || neckCount < 0 || crossPatchUndirectedEdgeCount < 0)
                throw new ArgumentOutOfRangeException(nameof(checkerboardCount));
            CheckerboardCount = checkerboardCount;
            NeckCount = neckCount;
            CrossPatchUndirectedEdgeCount = crossPatchUndirectedEdgeCount;
        }

        public int CheckerboardCount { get; }
        public int NeckCount { get; }
        public int CrossPatchUndirectedEdgeCount { get; }

        public int CompareTo(PatchCleanupScore other)
        {
            var value = CheckerboardCount.CompareTo(other.CheckerboardCount);
            if (value != 0) return value;
            value = NeckCount.CompareTo(other.NeckCount);
            return value != 0 ? value : CrossPatchUndirectedEdgeCount.CompareTo(other.CrossPatchUndirectedEdgeCount);
        }

        public bool Equals(PatchCleanupScore other)
        {
            return CheckerboardCount == other.CheckerboardCount &&
                   NeckCount == other.NeckCount &&
                   CrossPatchUndirectedEdgeCount == other.CrossPatchUndirectedEdgeCount;
        }

        public override bool Equals(object obj) => obj is PatchCleanupScore other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((CheckerboardCount * 397) ^ NeckCount) * 397 ^ CrossPatchUndirectedEdgeCount;
            }
        }

        public static bool operator ==(PatchCleanupScore left, PatchCleanupScore right) => left.Equals(right);
        public static bool operator !=(PatchCleanupScore left, PatchCleanupScore right) => !left.Equals(right);
        public static bool operator <(PatchCleanupScore left, PatchCleanupScore right) => left.CompareTo(right) < 0;
        public static bool operator >(PatchCleanupScore left, PatchCleanupScore right) => left.CompareTo(right) > 0;
    }

    public sealed class PatchCleanupMoveRecord
    {
        internal PatchCleanupMoveRecord(
            int sequence,
            PatchCleanupMoveKind kind,
            int centerSectorIndex,
            int movedSectorIndex,
            BiomePatchId donorPatchId,
            BiomePatchId targetPatchId,
            string donorBiomeId,
            string targetBiomeId,
            int donorSizeBefore,
            int donorSizeAfter,
            int targetSizeBefore,
            int targetSizeAfter,
            PatchCleanupScore scoreBefore,
            PatchCleanupScore scoreAfter)
        {
            if (sequence < 0) throw new ArgumentOutOfRangeException(nameof(sequence));
            if (!Enum.IsDefined(typeof(PatchCleanupMoveKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (centerSectorIndex < 0 || centerSectorIndex >= Domain.WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(centerSectorIndex));
            if (movedSectorIndex < 0 || movedSectorIndex >= Domain.WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(movedSectorIndex));
            if (!donorPatchId.IsValid || !targetPatchId.IsValid || donorPatchId == targetPatchId)
                throw new ArgumentException("Move patch IDs must be valid and different.");
            ReservationValidation.RequireCanonicalId(donorBiomeId, nameof(donorBiomeId), false);
            ReservationValidation.RequireCanonicalId(targetBiomeId, nameof(targetBiomeId), false);
            if (donorSizeBefore < 2 || donorSizeAfter != donorSizeBefore - 1 ||
                targetSizeBefore < 1 || targetSizeAfter != targetSizeBefore + 1)
                throw new ArgumentOutOfRangeException(nameof(donorSizeAfter));
            if (scoreAfter.CompareTo(scoreBefore) >= 0)
                throw new ArgumentException("Every cleanup move must strictly decrease the global score.");

            Sequence = sequence;
            Kind = kind;
            CenterSectorIndex = centerSectorIndex;
            MovedSectorIndex = movedSectorIndex;
            DonorPatchId = donorPatchId;
            TargetPatchId = targetPatchId;
            DonorBiomeId = donorBiomeId;
            TargetBiomeId = targetBiomeId;
            DonorSizeBefore = donorSizeBefore;
            DonorSizeAfter = donorSizeAfter;
            TargetSizeBefore = targetSizeBefore;
            TargetSizeAfter = targetSizeAfter;
            ScoreBefore = scoreBefore;
            ScoreAfter = scoreAfter;
        }

        public int Sequence { get; }
        public PatchCleanupMoveKind Kind { get; }
        public int CenterSectorIndex { get; }
        public int MovedSectorIndex { get; }
        public BiomePatchId DonorPatchId { get; }
        public BiomePatchId TargetPatchId { get; }
        public string DonorBiomeId { get; }
        public string TargetBiomeId { get; }
        public int DonorSizeBefore { get; }
        public int DonorSizeAfter { get; }
        public int TargetSizeBefore { get; }
        public int TargetSizeAfter { get; }
        public PatchCleanupScore ScoreBefore { get; }
        public PatchCleanupScore ScoreAfter { get; }
    }
}
