using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryRouteGraph
    {
        private readonly IReadOnlyList<MandatoryRouteGraphNode> nodes;
        private readonly IReadOnlyList<MandatoryRouteGraphEdge> edges;
        private readonly IReadOnlyList<MandatoryRouteGraphCell> cells;
        private readonly IReadOnlyDictionary<int, MandatoryRouteGraphNode> nodesBySector;
        private readonly IReadOnlyDictionary<MandatoryRouteGraphEdgeId, MandatoryRouteGraphEdge> edgesById;
        private readonly byte[] generatedWorldEdgesCsv;

        internal MandatoryRouteGraph(MandatoryRouteTerminalSet terminalSet, MandatoryRouteMaskLookup maskLookup,
            MandatoryConnectorTree connectorTree, HorizontalBackbonePlan horizontalPlan, VerticalGatewayPlan verticalPlan,
            UpDownConflictResolutionPlan conflictPlan, MandatoryRouteLoopPlan loopPlan, MandatoryRouteMaskFamily maskFamily,
            IEnumerable<MandatoryRouteGraphNode> nodes, IEnumerable<MandatoryRouteGraphEdge> edges,
            IEnumerable<MandatoryRouteGraphCell> cells, GeneratedWorldData routeStampedWorld, byte[] generatedWorldEdgesCsv)
        {
            SourceTerminalSet = terminalSet ?? throw new ArgumentNullException(nameof(terminalSet));
            SourceRouteMaskLookup = maskLookup ?? throw new ArgumentNullException(nameof(maskLookup));
            SourceConnectorTree = connectorTree ?? throw new ArgumentNullException(nameof(connectorTree));
            SourceHorizontalBackbonePlan = horizontalPlan ?? throw new ArgumentNullException(nameof(horizontalPlan));
            SourceVerticalGatewayPlan = verticalPlan ?? throw new ArgumentNullException(nameof(verticalPlan));
            SourceConflictResolutionPlan = conflictPlan ?? throw new ArgumentNullException(nameof(conflictPlan));
            SourceLoopPlan = loopPlan ?? throw new ArgumentNullException(nameof(loopPlan));
            MaskFamily = maskFamily ?? throw new ArgumentNullException(nameof(maskFamily));
            RouteStampedWorld = routeStampedWorld ?? throw new ArgumentNullException(nameof(routeStampedWorld));
            if (generatedWorldEdgesCsv == null) throw new ArgumentNullException(nameof(generatedWorldEdgesCsv));

            var nodeValues = new List<MandatoryRouteGraphNode>(nodes ?? throw new ArgumentNullException(nameof(nodes)));
            var edgeValues = new List<MandatoryRouteGraphEdge>(edges ?? throw new ArgumentNullException(nameof(edges)));
            var cellValues = new List<MandatoryRouteGraphCell>(cells ?? throw new ArgumentNullException(nameof(cells)));
            nodeValues.Sort((left, right) => left.SectorIndex.CompareTo(right.SectorIndex));
            edgeValues.Sort((left, right) => left.EdgeId.CompareTo(right.EdgeId));
            cellValues.Sort((left, right) => left.SectorIndex.CompareTo(right.SectorIndex));
            var nodeMap = new Dictionary<int, MandatoryRouteGraphNode>();
            var edgeMap = new Dictionary<MandatoryRouteGraphEdgeId, MandatoryRouteGraphEdge>();
            foreach (var node in nodeValues) if (node == null || !nodeMap.TryAdd(node.SectorIndex, node)) throw new ArgumentException("Graph node sectors must be unique.", nameof(nodes));
            foreach (var edge in edgeValues) if (edge == null || !edgeMap.TryAdd(edge.EdgeId, edge)) throw new ArgumentException("Graph edge IDs must be unique.", nameof(edges));
            this.nodes = new ReadOnlyCollection<MandatoryRouteGraphNode>(nodeValues);
            this.edges = new ReadOnlyCollection<MandatoryRouteGraphEdge>(edgeValues);
            this.cells = new ReadOnlyCollection<MandatoryRouteGraphCell>(cellValues);
            nodesBySector = new ReadOnlyDictionary<int, MandatoryRouteGraphNode>(nodeMap);
            edgesById = new ReadOnlyDictionary<MandatoryRouteGraphEdgeId, MandatoryRouteGraphEdge>(edgeMap);
            this.generatedWorldEdgesCsv = (byte[])generatedWorldEdgesCsv.Clone();
        }

        public MandatoryRouteTerminalSet SourceTerminalSet { get; }
        public MandatoryRouteMaskLookup SourceRouteMaskLookup { get; }
        public MandatoryConnectorTree SourceConnectorTree { get; }
        public HorizontalBackbonePlan SourceHorizontalBackbonePlan { get; }
        public VerticalGatewayPlan SourceVerticalGatewayPlan { get; }
        public UpDownConflictResolutionPlan SourceConflictResolutionPlan { get; }
        public MandatoryRouteLoopPlan SourceLoopPlan { get; }
        public MandatoryRouteMaskFamily MaskFamily { get; }
        public IReadOnlyList<MandatoryRouteGraphNode> Nodes => nodes;
        public IReadOnlyList<MandatoryRouteGraphEdge> Edges => edges;
        public IReadOnlyList<MandatoryRouteGraphCell> Cells => cells;
        public GeneratedWorldData RouteStampedWorld { get; }
        public byte[] GeneratedWorldEdgesCsv => (byte[])generatedWorldEdgesCsv.Clone();
        public int NodeCount => nodes.Count;
        public int DirectedEdgeCount => edges.Count;
        public int UndirectedEdgeCount => edges.Count / 2;
        public int CellCount => cells.Count;
        public bool TryGetNode(SectorCoord coordinate, out MandatoryRouteGraphNode node) => nodesBySector.TryGetValue(WorldGridIndex.ToIndex(coordinate), out node);
        public bool TryGetNode(int sectorIndex, out MandatoryRouteGraphNode node) => nodesBySector.TryGetValue(sectorIndex, out node);
        public bool TryGetEdge(MandatoryRouteGraphEdgeId edgeId, out MandatoryRouteGraphEdge edge) => edgesById.TryGetValue(edgeId, out edge);
    }
}
