using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class PatchCleanupDiagnostics
    {
        private readonly IReadOnlyList<PatchCleanupMoveRecord> moves;

        internal PatchCleanupDiagnostics(
            ulong worldSeed,
            ulong sourceRngDrawCount,
            int initialPatchCount,
            int finalPatchCount,
            int initialAssignedSectorCount,
            int finalAssignedSectorCount,
            int initialUnassignedSectorCount,
            int finalUnassignedSectorCount,
            PatchCleanupScore initialScore,
            PatchCleanupScore finalScore,
            int protectedAnomalyCount,
            int stepLimit,
            IEnumerable<PatchCleanupMoveRecord> moves,
            int overlapViolationCount,
            int orphanOwnershipCount,
            int disconnectedPatchCount,
            int siteMisownershipCount,
            int protectedOwnershipChangeCount,
            int sourceMutationCount,
            bool rollback)
        {
            if (moves == null) throw new ArgumentNullException(nameof(moves));
            if (initialPatchCount < 0 || finalPatchCount < 0 ||
                initialAssignedSectorCount < 0 || finalAssignedSectorCount < 0 ||
                initialUnassignedSectorCount < 0 || finalUnassignedSectorCount < 0 ||
                protectedAnomalyCount < 0 || stepLimit < 1 ||
                overlapViolationCount < 0 || orphanOwnershipCount < 0 ||
                disconnectedPatchCount < 0 || siteMisownershipCount < 0 ||
                protectedOwnershipChangeCount < 0 || sourceMutationCount < 0)
                throw new ArgumentOutOfRangeException(nameof(initialPatchCount));

            var records = new List<PatchCleanupMoveRecord>(moves);
            records.Sort((left, right) => left.Sequence.CompareTo(right.Sequence));
            for (var index = 0; index < records.Count; index++)
                if (records[index] == null || records[index].Sequence != index)
                    throw new ArgumentException("Cleanup moves require exact sequence order.", nameof(moves));
            if (records.Count > stepLimit) throw new ArgumentException("Move count exceeds the step limit.", nameof(moves));
            if (rollback && records.Count != 0) throw new ArgumentException("Rollback diagnostics cannot publish moves.", nameof(moves));
            if (!rollback && records.Count != 0 && finalScore.CompareTo(initialScore) >= 0)
                throw new ArgumentException("Successful moves must reduce the global score.");
            if (initialAssignedSectorCount + initialUnassignedSectorCount != Domain.WorldGenConstants.SectorCount ||
                finalAssignedSectorCount + finalUnassignedSectorCount != Domain.WorldGenConstants.SectorCount)
                throw new ArgumentException("Sector counts must cover the world.");

            WorldSeed = worldSeed;
            SourceRngDrawCount = sourceRngDrawCount;
            FinalRngDrawCount = sourceRngDrawCount;
            RngMethodCallCount = 0;
            RngRawDrawCount = 0;
            InitialPatchCount = initialPatchCount;
            FinalPatchCount = finalPatchCount;
            InitialAssignedSectorCount = initialAssignedSectorCount;
            FinalAssignedSectorCount = finalAssignedSectorCount;
            InitialUnassignedSectorCount = initialUnassignedSectorCount;
            FinalUnassignedSectorCount = finalUnassignedSectorCount;
            InitialScore = initialScore;
            FinalScore = finalScore;
            InitialActionableCheckerboardCount = initialScore.CheckerboardCount;
            FinalActionableCheckerboardCount = finalScore.CheckerboardCount;
            InitialActionableNeckCount = initialScore.NeckCount;
            FinalActionableNeckCount = finalScore.NeckCount;
            ProtectedAnomalyCount = protectedAnomalyCount;
            MoveCount = records.Count;
            StepLimit = stepLimit;
            this.moves = new ReadOnlyCollection<PatchCleanupMoveRecord>(records);
            OverlapViolationCount = overlapViolationCount;
            OrphanOwnershipCount = orphanOwnershipCount;
            DisconnectedPatchCount = disconnectedPatchCount;
            SiteMisownershipCount = siteMisownershipCount;
            ProtectedOwnershipChangeCount = protectedOwnershipChangeCount;
            SourceMutationCount = sourceMutationCount;
            Rollback = rollback;
        }

        public ulong WorldSeed { get; }
        public ulong SourceRngDrawCount { get; }
        public ulong FinalRngDrawCount { get; }
        public int RngMethodCallCount { get; }
        public int RngRawDrawCount { get; }
        public int InitialPatchCount { get; }
        public int FinalPatchCount { get; }
        public int InitialAssignedSectorCount { get; }
        public int FinalAssignedSectorCount { get; }
        public int InitialUnassignedSectorCount { get; }
        public int FinalUnassignedSectorCount { get; }
        public PatchCleanupScore InitialScore { get; }
        public PatchCleanupScore FinalScore { get; }
        public int InitialActionableCheckerboardCount { get; }
        public int FinalActionableCheckerboardCount { get; }
        public int InitialActionableNeckCount { get; }
        public int FinalActionableNeckCount { get; }
        public int ProtectedAnomalyCount { get; }
        public int MoveCount { get; }
        public int StepLimit { get; }
        public IReadOnlyList<PatchCleanupMoveRecord> Moves => moves;
        public int OverlapViolationCount { get; }
        public int OrphanOwnershipCount { get; }
        public int DisconnectedPatchCount { get; }
        public int SiteMisownershipCount { get; }
        public int ProtectedOwnershipChangeCount { get; }
        public int SourceMutationCount { get; }
        public bool Rollback { get; }
    }
}
