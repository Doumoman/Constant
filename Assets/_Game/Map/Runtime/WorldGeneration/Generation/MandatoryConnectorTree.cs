using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryConnectorTree
    {
        private readonly IReadOnlyList<MandatoryConnectorCandidateEdge> candidateEdges;
        private readonly IReadOnlyList<MandatoryConnectorCandidateEdge> treeEdges;
        private readonly IReadOnlyDictionary<MandatoryConnectorEdgeId, MandatoryConnectorCandidateEdge> byId;
        private readonly IReadOnlyDictionary<MandatoryRouteTerminalId, IReadOnlyList<MandatoryConnectorCandidateEdge>> adjacency;

        internal MandatoryConnectorTree(MandatoryRouteTerminalSet terminalSet, MandatoryRouteMaskLookup routeMaskLookup, IEnumerable<MandatoryConnectorCandidateEdge> candidates, IEnumerable<MandatoryConnectorCandidateEdge> selected)
        {
            SourceTerminalSet = terminalSet ?? throw new ArgumentNullException(nameof(terminalSet));
            SourceRouteMaskLookup = routeMaskLookup ?? throw new ArgumentNullException(nameof(routeMaskLookup));
            var candidateValues = new List<MandatoryConnectorCandidateEdge>(candidates ?? throw new ArgumentNullException(nameof(candidates)));
            var treeValues = new List<MandatoryConnectorCandidateEdge>(selected ?? throw new ArgumentNullException(nameof(selected)));
            if (terminalSet.TerminalCount != 7 || candidateValues.Count != 21 || treeValues.Count != 6) throw new ArgumentException("Connector tree cardinality is invalid.");
            var ids = new Dictionary<MandatoryConnectorEdgeId, MandatoryConnectorCandidateEdge>();
            var pairs = new HashSet<string>(StringComparer.Ordinal);
            foreach (var edge in candidateValues)
            {
                if (edge == null || edge.IsTreeEdge || !ids.TryAdd(edge.EdgeId, edge) || !pairs.Add(PairKey(edge))) throw new ArgumentException("Candidate edge identity is invalid.");
            }
            var treeIds = new Dictionary<MandatoryConnectorEdgeId, MandatoryConnectorCandidateEdge>();
            var mutableAdjacency = new Dictionary<MandatoryRouteTerminalId, List<MandatoryConnectorCandidateEdge>>();
            foreach (var terminal in terminalSet.Terminals) mutableAdjacency.Add(terminal.TerminalId, new List<MandatoryConnectorCandidateEdge>());
            var total = 0;
            foreach (var edge in treeValues)
            {
                if (edge == null || !edge.IsTreeEdge || !ids.ContainsKey(edge.EdgeId) || !treeIds.TryAdd(edge.EdgeId, edge)) throw new ArgumentException("Tree edge identity is invalid.");
                if (!mutableAdjacency.TryGetValue(edge.FromTerminalId, out var from) || !mutableAdjacency.TryGetValue(edge.ToTerminalId, out var to)) throw new ArgumentException("Tree endpoint is missing.");
                from.Add(edge);
                to.Add(edge);
                total = checked(total + edge.Cost.TotalCost);
            }
            var visited = new HashSet<MandatoryRouteTerminalId>();
            Visit(terminalSet.Terminals[0].TerminalId, mutableAdjacency, visited);
            if (visited.Count != 7) throw new ArgumentException("Tree must be connected.");
            var frozenAdjacency = new Dictionary<MandatoryRouteTerminalId, IReadOnlyList<MandatoryConnectorCandidateEdge>>();
            foreach (var pair in mutableAdjacency)
            {
                pair.Value.Sort((left, right) => left.EdgeId.CompareTo(right.EdgeId));
                frozenAdjacency.Add(pair.Key, new ReadOnlyCollection<MandatoryConnectorCandidateEdge>(pair.Value));
            }
            candidateEdges = new ReadOnlyCollection<MandatoryConnectorCandidateEdge>(candidateValues);
            treeEdges = new ReadOnlyCollection<MandatoryConnectorCandidateEdge>(treeValues);
            byId = new ReadOnlyDictionary<MandatoryConnectorEdgeId, MandatoryConnectorCandidateEdge>(treeIds);
            adjacency = new ReadOnlyDictionary<MandatoryRouteTerminalId, IReadOnlyList<MandatoryConnectorCandidateEdge>>(frozenAdjacency);
            TotalTreeCost = total;
        }

        public MandatoryRouteTerminalSet SourceTerminalSet { get; }
        public MandatoryRouteMaskLookup SourceRouteMaskLookup { get; }
        public IReadOnlyList<MandatoryConnectorCandidateEdge> CandidateEdges => candidateEdges;
        public IReadOnlyList<MandatoryConnectorCandidateEdge> TreeEdges => treeEdges;
        public int NodeCount => SourceTerminalSet.TerminalCount;
        public int CandidateEdgeCount => candidateEdges.Count;
        public int TreeEdgeCount => treeEdges.Count;
        public int TotalTreeCost { get; }
        public bool IsConnected => true;
        public bool IsAcyclic => true;
        public bool CoversAllTerminals => true;
        public bool TryGetTreeEdge(MandatoryConnectorEdgeId id, out MandatoryConnectorCandidateEdge edge) => byId.TryGetValue(id, out edge);
        public IReadOnlyList<MandatoryConnectorCandidateEdge> GetTreeEdgesForTerminal(MandatoryRouteTerminalId terminalId)
        {
            return adjacency.TryGetValue(terminalId, out var edges) ? edges : Array.Empty<MandatoryConnectorCandidateEdge>();
        }

        private static string PairKey(MandatoryConnectorCandidateEdge edge) => edge.FromTerminalId.Value + "\n" + edge.ToTerminalId.Value;
        private static void Visit(MandatoryRouteTerminalId id, IReadOnlyDictionary<MandatoryRouteTerminalId, List<MandatoryConnectorCandidateEdge>> values, ISet<MandatoryRouteTerminalId> visited)
        {
            if (!visited.Add(id)) return;
            foreach (var edge in values[id]) Visit(edge.FromTerminalId == id ? edge.ToTerminalId : edge.FromTerminalId, values, visited);
        }
    }
}
