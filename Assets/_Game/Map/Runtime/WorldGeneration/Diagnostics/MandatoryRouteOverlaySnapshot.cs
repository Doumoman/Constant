using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.WorldGeneration.Diagnostics
{
    public sealed class MandatoryRouteOverlaySnapshot
    {
        private readonly IReadOnlyList<MandatoryRouteOverlayCell> cells;
        private readonly IReadOnlyList<MandatoryRouteGraphEdge> edges;
        private readonly IReadOnlyDictionary<int, MandatoryRouteOverlayCell> byIndex;
        private MandatoryRouteOverlaySnapshot(MandatoryRouteValidationReport report, List<MandatoryRouteOverlayCell> sourceCells)
        {
            SourceReport = report; cells = new ReadOnlyCollection<MandatoryRouteOverlayCell>(sourceCells);
            edges = new ReadOnlyCollection<MandatoryRouteGraphEdge>(new List<MandatoryRouteGraphEdge>(report.SourceGraph.Edges));
            var map = new Dictionary<int, MandatoryRouteOverlayCell>(); foreach (var cell in sourceCells) map.Add(cell.Index, cell);
            byIndex = new ReadOnlyDictionary<int, MandatoryRouteOverlayCell>(map);
            var s = report.Summary;
            NodeCount = report.SourceGraph.NodeCount; DirectedEdgeCount = s.DirectedEdgeCount; UndirectedEdgeCount = s.UndirectedEdgeCount; RouteCellCount = report.SourceGraph.CellCount;
            Type1Count=s.Type1Count; Type2Count=s.Type2Count; Type3Count=s.Type3Count; Type4UdCount=s.Type4UdCount; Type4LudCount=s.Type4LudCount; Type4RudCount=s.Type4RudCount; Type4LrudCount=s.Type4LrudCount;
            ReachableTerminalCount=s.ReachableTerminalCount; TerminalCount=report.SourceTerminalSet.TerminalCount; RepresentedLoopCount=s.RepresentedLoopCount;
            RuleCount=s.RuleCount; PassedRuleCount=s.PassedRuleCount; ViolationCount=s.ViolationCount; ErrorCount=s.ErrorCount; WarningCount=s.WarningCount;
            GeneratedSectorCsvByteCount=s.GeneratedSectorCsvByteCount; GeneratedEdgeCsvByteCount=s.GeneratedEdgeCsvByteCount; GeneratedEdgeRowCount=s.GeneratedEdgeRowCount;
            ValidationBanner="PASS_ROUTE 12/12 | V/E/W 0/0/0";
        }
        public MandatoryRouteValidationReport SourceReport { get; } public IReadOnlyList<MandatoryRouteOverlayCell> Cells=>cells; public IReadOnlyList<MandatoryRouteGraphEdge> Edges=>edges;
        public int NodeCount{get;} public int DirectedEdgeCount{get;} public int UndirectedEdgeCount{get;} public int RouteCellCount{get;}
        public int Type1Count{get;} public int Type2Count{get;} public int Type3Count{get;} public int Type4UdCount{get;} public int Type4LudCount{get;} public int Type4RudCount{get;} public int Type4LrudCount{get;}
        public int ReachableTerminalCount{get;} public int TerminalCount{get;} public int RepresentedLoopCount{get;} public int RuleCount{get;} public int PassedRuleCount{get;} public int ViolationCount{get;} public int ErrorCount{get;} public int WarningCount{get;}
        public int GeneratedSectorCsvByteCount{get;} public int GeneratedEdgeCsvByteCount{get;} public int GeneratedEdgeRowCount{get;} public string ValidationBanner{get;}
        public bool TryGetCell(int index,out MandatoryRouteOverlayCell cell)=>byIndex.TryGetValue(index,out cell);
        public MandatoryRouteOverlayCell GetCell(SectorCoord coordinate){ if(!TryGetCell(WorldGridIndex.ToIndex(coordinate),out var cell)) throw new ArgumentOutOfRangeException(nameof(coordinate)); return cell; }
        public static MandatoryRouteOverlaySnapshot Create(MandatoryRouteValidationReport report)
        {
            if(report==null) throw new ArgumentNullException(nameof(report));
            var s=report.Summary; var graph=report.SourceGraph;
            if(!report.IsValid || report.PassId!="PASS_ROUTE" || s.RuleCount!=12 || s.PassedRuleCount!=12 || s.ViolationCount!=0 || s.ErrorCount!=0 || s.WarningCount!=0) throw new ArgumentException("A complete PASS_ROUTE report is required.",nameof(report));
            if(graph.NodeCount!=47 || graph.DirectedEdgeCount!=96 || graph.UndirectedEdgeCount!=48 || graph.CellCount!=47 || s.ReachableTerminalCount!=7 || s.RepresentedLoopCount!=2 || s.GeneratedSectorCsvByteCount!=16838 || s.GeneratedEdgeCsvByteCount!=7094 || s.GeneratedEdgeRowCount!=96) throw new ArgumentException("Report does not match the approved starter vector.",nameof(report));
            var nodes=new Dictionary<int,MandatoryRouteGraphNode>(); foreach(var node in graph.Nodes) nodes.Add(node.SectorIndex,node);
            var values=new List<MandatoryRouteOverlayCell>(); foreach(var cell in graph.Cells){ if(!nodes.TryGetValue(cell.SectorIndex,out var node)) throw new ArgumentException("Every route cell requires a node.",nameof(report)); values.Add(new MandatoryRouteOverlayCell(cell,node)); }
            values.Sort((a,b)=>a.Index.CompareTo(b.Index)); return new MandatoryRouteOverlaySnapshot(report,values);
        }
    }
}
