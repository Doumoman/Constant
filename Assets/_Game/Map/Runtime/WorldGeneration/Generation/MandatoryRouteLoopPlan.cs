using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryRouteLoopPlan
    {
        private readonly IReadOnlyList<MandatoryRouteLoopCandidate> candidates;
        private readonly IReadOnlyList<MandatoryRouteLoop> loops;
        private readonly IReadOnlyDictionary<MandatoryRouteLoopId, MandatoryRouteLoopCandidate> candidatesById;
        private readonly IReadOnlyDictionary<MandatoryRouteLoopId, MandatoryRouteLoop> loopsById;

        internal MandatoryRouteLoopPlan(
            MandatoryRouteTerminalSet sourceTerminalSet,
            MandatoryConnectorTree sourceConnectorTree,
            HorizontalBackbonePlan sourceHorizontalBackbonePlan,
            VerticalGatewayPlan sourceVerticalGatewayPlan,
            UpDownConflictResolutionPlan sourceConflictResolutionPlan,
            IEnumerable<MandatoryRouteLoopCandidate> sourceCandidates,
            IEnumerable<MandatoryRouteLoop> sourceLoops)
        {
            SourceTerminalSet = sourceTerminalSet ?? throw new ArgumentNullException(nameof(sourceTerminalSet));
            SourceConnectorTree = sourceConnectorTree ?? throw new ArgumentNullException(nameof(sourceConnectorTree));
            SourceHorizontalBackbonePlan = sourceHorizontalBackbonePlan ?? throw new ArgumentNullException(nameof(sourceHorizontalBackbonePlan));
            SourceVerticalGatewayPlan = sourceVerticalGatewayPlan ?? throw new ArgumentNullException(nameof(sourceVerticalGatewayPlan));
            SourceConflictResolutionPlan = sourceConflictResolutionPlan ?? throw new ArgumentNullException(nameof(sourceConflictResolutionPlan));
            var candidateValues = new List<MandatoryRouteLoopCandidate>(sourceCandidates ?? throw new ArgumentNullException(nameof(sourceCandidates)));
            var loopValues = new List<MandatoryRouteLoop>(sourceLoops ?? throw new ArgumentNullException(nameof(sourceLoops)));
            candidateValues.Sort(MandatoryRouteLoopPlanner.CompareCandidates);
            loopValues.Sort((left, right) => left.LoopId.CompareTo(right.LoopId));
            var candidateMap = new Dictionary<MandatoryRouteLoopId, MandatoryRouteLoopCandidate>();
            foreach (var candidate in candidateValues)
                if (candidate == null || !candidateMap.TryAdd(candidate.LoopId, candidate)) throw new ArgumentException("Candidate IDs must be unique.", nameof(sourceCandidates));
            var loopMap = new Dictionary<MandatoryRouteLoopId, MandatoryRouteLoop>();
            var totalCost = 0;
            var shared = 0;
            foreach (var loop in loopValues)
            {
                if (loop == null || !candidateMap.TryGetValue(loop.LoopId, out var candidate) || !ReferenceEquals(loop.Candidate, candidate) || !loopMap.TryAdd(loop.LoopId, loop))
                    throw new ArgumentException("Loops must uniquely reference published candidates.", nameof(sourceLoops));
                totalCost = checked(totalCost + loop.TotalCost);
                shared = checked(shared + loop.SharedCellCount);
            }
            candidates = new ReadOnlyCollection<MandatoryRouteLoopCandidate>(candidateValues);
            loops = new ReadOnlyCollection<MandatoryRouteLoop>(loopValues);
            candidatesById = new ReadOnlyDictionary<MandatoryRouteLoopId, MandatoryRouteLoopCandidate>(candidateMap);
            loopsById = new ReadOnlyDictionary<MandatoryRouteLoopId, MandatoryRouteLoop>(loopMap);
            LoopCount = loopValues.Count;
            IndependentLoopCount = loopValues.FindAll(value => value.IsIndependent).Count;
            SharedCellCount = shared;
            TotalCost = totalCost;
        }

        public const int MinimumLoopCount = 2;
        public MandatoryRouteTerminalSet SourceTerminalSet { get; }
        public MandatoryConnectorTree SourceConnectorTree { get; }
        public HorizontalBackbonePlan SourceHorizontalBackbonePlan { get; }
        public VerticalGatewayPlan SourceVerticalGatewayPlan { get; }
        public UpDownConflictResolutionPlan SourceConflictResolutionPlan { get; }
        public IReadOnlyList<MandatoryRouteLoopCandidate> Candidates => candidates;
        public IReadOnlyList<MandatoryRouteLoop> Loops => loops;
        public int CandidateCount => candidates.Count;
        public int LoopCount { get; }
        public int IndependentLoopCount { get; }
        public int SharedCellCount { get; }
        public int TotalCost { get; }
        public bool MeetsMinimum => IndependentLoopCount >= MinimumLoopCount;
        public bool TryGetCandidate(MandatoryRouteLoopId id, out MandatoryRouteLoopCandidate candidate) => candidatesById.TryGetValue(id, out candidate);
        public bool TryGetLoop(MandatoryRouteLoopId id, out MandatoryRouteLoop loop) => loopsById.TryGetValue(id, out loop);
    }
}
