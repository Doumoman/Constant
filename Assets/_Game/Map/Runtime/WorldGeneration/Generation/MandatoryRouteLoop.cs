using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryRouteLoop
    {
        private readonly IReadOnlyList<SectorCoord> cells;
        private readonly IReadOnlyList<VerticalGatewayJunctionCell> junctionReferences;

        internal MandatoryRouteLoop(MandatoryRouteLoopCandidate candidate, string independenceWitness)
        {
            Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
            if (!candidate.IsEligible) throw new ArgumentException("Only eligible independent candidates can become loops.", nameof(candidate));
            if (string.IsNullOrEmpty(independenceWitness)) throw new ArgumentException("Independence witness is required.", nameof(independenceWitness));
            IndependenceWitness = independenceWitness;
            var cellValues = new List<SectorCoord>(candidate.OrderedCells);
            var cellSet = new HashSet<SectorCoord>(cellValues);
            var junctionValues = new List<VerticalGatewayJunctionCell>();
            foreach (var junction in candidate.SourceType4Junctions)
                if (cellSet.Contains(junction.Coord)) junctionValues.Add(junction);
            cells = new ReadOnlyCollection<SectorCoord>(cellValues);
            junctionReferences = new ReadOnlyCollection<VerticalGatewayJunctionCell>(junctionValues);
        }

        public MandatoryRouteLoopCandidate Candidate { get; }
        public MandatoryRouteLoopId LoopId => Candidate.LoopId;
        public MandatoryRouteTerminalId StartTerminalId => Candidate.StartTerminalId;
        public MandatoryRouteTerminalId EndTerminalId => Candidate.EndTerminalId;
        public MandatoryConnectorEdgeId SourceConnectorEdgeId => Candidate.SourceConnectorEdgeId;
        public IReadOnlyList<SectorCoord> InclusiveOrderedCells => cells;
        public IReadOnlyList<VerticalGatewayJunctionCell> VerticalJunctionReferences => junctionReferences;
        public int SharedCellCount => Candidate.SharedCellCount;
        public int UniqueCellCount => Candidate.UniqueCellCount;
        public int TotalCost => Candidate.CheckedTotalCost;
        public bool IsIndependent => true;
        public string IndependenceWitness { get; }
    }
}
