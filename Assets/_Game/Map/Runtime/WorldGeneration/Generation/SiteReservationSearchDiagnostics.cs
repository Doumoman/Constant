using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SiteReservationGroupDiagnostics
    {
        private readonly int[] rejectionReasonCounts;

        internal SiteReservationGroupDiagnostics(
            SitePlacementKey key,
            int sourceOptionCount,
            int stateVisitCount,
            int candidateEvaluationCount,
            int selectionPushCount,
            int backtrackPopCount,
            int exhaustionCount,
            int rejectedOptionEvaluationCount,
            IReadOnlyList<int> rejectionReasonCounts)
        {
            if (!key.IsValid) throw new ArgumentException("A valid group key is required.", nameof(key));
            RequireNonNegative(sourceOptionCount, nameof(sourceOptionCount));
            RequireNonNegative(stateVisitCount, nameof(stateVisitCount));
            RequireNonNegative(candidateEvaluationCount, nameof(candidateEvaluationCount));
            RequireNonNegative(selectionPushCount, nameof(selectionPushCount));
            RequireNonNegative(backtrackPopCount, nameof(backtrackPopCount));
            RequireNonNegative(exhaustionCount, nameof(exhaustionCount));
            RequireNonNegative(rejectedOptionEvaluationCount, nameof(rejectedOptionEvaluationCount));
            if (rejectionReasonCounts == null || rejectionReasonCounts.Count != 5)
                throw new ArgumentException("Exactly five rejection counts are required.", nameof(rejectionReasonCounts));

            this.rejectionReasonCounts = new int[5];
            for (var index = 0; index < this.rejectionReasonCounts.Length; index++)
            {
                RequireNonNegative(rejectionReasonCounts[index], nameof(rejectionReasonCounts));
                this.rejectionReasonCounts[index] = rejectionReasonCounts[index];
            }

            Key = key;
            SourceOptionCount = sourceOptionCount;
            StateVisitCount = stateVisitCount;
            CandidateEvaluationCount = candidateEvaluationCount;
            SelectionPushCount = selectionPushCount;
            BacktrackPopCount = backtrackPopCount;
            ExhaustionCount = exhaustionCount;
            RejectedOptionEvaluationCount = rejectedOptionEvaluationCount;
        }

        public SitePlacementKey Key { get; }
        public int SourceOptionCount { get; }
        public int StateVisitCount { get; }
        public int CandidateEvaluationCount { get; }
        public int SelectionPushCount { get; }
        public int BacktrackPopCount { get; }
        public int ExhaustionCount { get; }
        public int RejectedOptionEvaluationCount { get; }

        public int GetReasonCount(SiteReservationRejectionReason reason)
        {
            if (!Enum.IsDefined(typeof(SiteReservationRejectionReason), reason))
                throw new ArgumentOutOfRangeException(nameof(reason));
            return rejectionReasonCounts[(int)reason];
        }

        private static void RequireNonNegative(int value, string parameter)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(parameter);
        }
    }

    public sealed class SiteReservationSearchDiagnostics
    {
        private readonly IReadOnlyList<SiteReservationGroupDiagnostics> groups;

        internal SiteReservationSearchDiagnostics(
            IEnumerable<SiteReservationGroupDiagnostics> groups,
            int failedCombinationCount,
            int deepestSelectedDepth,
            ulong rngInitialState,
            ulong rngDrawCountBefore,
            ulong tieBreakDrawCount,
            ulong rngDrawCountAfter)
        {
            if (groups == null) throw new ArgumentNullException(nameof(groups));
            var snapshot = new List<SiteReservationGroupDiagnostics>(groups);
            if (snapshot.Exists(item => item == null))
                throw new ArgumentException("Group diagnostics cannot contain null.", nameof(groups));
            if (failedCombinationCount < 0 || failedCombinationCount >
                SiteReservationSearchLimits.ProductionMaximum)
                throw new ArgumentOutOfRangeException(nameof(failedCombinationCount));
            if (deepestSelectedDepth < 0 || deepestSelectedDepth > 6)
                throw new ArgumentOutOfRangeException(nameof(deepestSelectedDepth));
            if (rngDrawCountAfter < rngDrawCountBefore ||
                rngDrawCountAfter - rngDrawCountBefore != tieBreakDrawCount)
                throw new ArgumentException("RNG diagnostic draw counts must be exact.");

            var sourceOptions = 0;
            var evaluations = 0;
            var pushes = 0;
            var pops = 0;
            checked
            {
                foreach (var group in snapshot)
                {
                    sourceOptions += group.SourceOptionCount;
                    evaluations += group.CandidateEvaluationCount;
                    pushes += group.SelectionPushCount;
                    pops += group.BacktrackPopCount;
                }
            }
            if (pops != failedCombinationCount)
                throw new ArgumentException("Failed-combination and backtrack counts must match.");

            this.groups = new ReadOnlyCollection<SiteReservationGroupDiagnostics>(snapshot);
            TotalSourceOptionCount = sourceOptions;
            CandidateEvaluationCount = evaluations;
            SelectionPushCount = pushes;
            FailedCombinationCount = failedCombinationCount;
            BacktrackCount = pops;
            DeepestSelectedDepth = deepestSelectedDepth;
            RngInitialState = rngInitialState;
            RngDrawCountBefore = rngDrawCountBefore;
            TieBreakDrawCount = tieBreakDrawCount;
            RngDrawCountAfter = rngDrawCountAfter;
        }

        public IReadOnlyList<SiteReservationGroupDiagnostics> Groups => groups;
        public int TotalSourceOptionCount { get; }
        public int CandidateEvaluationCount { get; }
        public int SelectionPushCount { get; }
        public int FailedCombinationCount { get; }
        public int BacktrackCount { get; }
        public int DeepestSelectedDepth { get; }
        public ulong RngInitialState { get; }
        public ulong RngDrawCountBefore { get; }
        public ulong TieBreakDrawCount { get; }
        public ulong RngDrawCountAfter { get; }
    }
}
