using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public sealed class Sv5LoopOccupancyCell
    {
        public Sv5LoopOccupancyCell(Sv5SpecialWorldPoint world, Sv5InfillCellValue value,
            string provenance, string owner)
        { World=world; Value=value; Provenance=provenance ?? string.Empty; Owner=owner ?? string.Empty; }
        public Sv5SpecialWorldPoint World { get; }
        public Sv5InfillCellValue Value { get; }
        public string Provenance { get; }
        public string Owner { get; }
        public string Token => World+"|"+Value+"|"+Provenance+"|"+Owner;
    }

    public sealed class Sv5LoopTopologyEdge : IComparable<Sv5LoopTopologyEdge>
    {
        internal Sv5LoopTopologyEdge(string id,string from,string to,string kind,string loopId="")
        { Id=id; FromVertex=from; ToVertex=to; Kind=kind; LoopId=loopId ?? string.Empty; }
        public string Id { get; }
        public string FromVertex { get; }
        public string ToVertex { get; }
        public string Kind { get; }
        public string LoopId { get; }
        public string Token => Id+"|"+FromVertex+"|"+ToVertex+"|"+Kind+"|"+LoopId;
        public int CompareTo(Sv5LoopTopologyEdge other) => other==null ? 1 :
            string.Compare(Id,other.Id,StringComparison.Ordinal);
    }

    public sealed class Sv5LoopTopologyMetrics
    {
        internal Sv5LoopTopologyMetrics(int vertices,int edges,int components,int rank,IEnumerable<string> bridges)
        {
            Vertices=vertices; Edges=edges; Components=components; CycleRank=rank;
            Bridges=Array.AsReadOnly((bridges ?? Array.Empty<string>()).OrderBy(v=>v,StringComparer.Ordinal).ToArray());
        }
        public int Vertices { get; }
        public int Edges { get; }
        public int Components { get; }
        public int CycleRank { get; }
        public IReadOnlyList<string> Bridges { get; }
        public string Token => Vertices+"|"+Edges+"|"+Components+"|"+CycleRank+"|"+string.Join(";",Bridges);
    }

    public sealed class Sv5LoopTopologyEvaluation
    {
        internal Sv5LoopTopologyEvaluation(int baselineCost,int finalCost,int baselineContacts,int newlyCarvedAir,
            bool baselineAlternatePath,bool edgeRemovalAlternatePath,int cycleDelta,
            IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell> finalOccupancy,
            IReadOnlyCollection<Sv5SpecialWorldPoint> finalFootNodes,IEnumerable<string> errors)
        {
            BaselineCost=baselineCost; FinalCost=finalCost; BaselineContacts=baselineContacts;
            NewlyCarvedAir=newlyCarvedAir; BaselineAlternatePath=baselineAlternatePath;
            EdgeRemovalAlternatePath=edgeRemovalAlternatePath; CycleDelta=cycleDelta;
            FinalOccupancy=finalOccupancy; FinalFootNodes=finalFootNodes;
            Errors=Array.AsReadOnly((errors ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal)
                .OrderBy(v=>v,StringComparer.Ordinal).ToArray());
        }
        public int BaselineCost { get; }
        public int FinalCost { get; }
        public int BaselineContacts { get; }
        public int NewlyCarvedAir { get; }
        public bool BaselineAlternatePath { get; }
        public bool EdgeRemovalAlternatePath { get; }
        public int CycleDelta { get; }
        public IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell> FinalOccupancy { get; }
        public IReadOnlyCollection<Sv5SpecialWorldPoint> FinalFootNodes { get; }
        public IReadOnlyList<string> Errors { get; }
        public bool Success => Errors.Count==0;
    }

    public sealed class Sv5LoopTopologyProof
    {
        internal Sv5LoopTopologyProof(Sv5LoopLink link,Sv5LoopTopologyEvaluation evaluation,
            Sv5LoopTopologyMetrics before,Sv5LoopTopologyMetrics after)
        {
            LoopId=link.Id; FromRoom=link.FromRoomId; ToRoom=link.ToRoomId;
            From=link.FootPath.First(); To=link.FootPath.Last();
            BaselineCost=evaluation.BaselineCost; FinalCost=evaluation.FinalCost;
            BaselineContacts=evaluation.BaselineContacts; NewlyCarvedAir=evaluation.NewlyCarvedAir;
            BaselineAlternatePath=evaluation.BaselineAlternatePath;
            EdgeRemovalAlternatePath=evaluation.EdgeRemovalAlternatePath;
            CycleDelta=evaluation.CycleDelta; Before=before; After=after;
        }
        public string LoopId { get; }
        public string FromRoom { get; }
        public string ToRoom { get; }
        public Sv5SpecialWorldPoint From { get; }
        public Sv5SpecialWorldPoint To { get; }
        public int BaselineCost { get; }
        public int FinalCost { get; }
        public int BaselineContacts { get; }
        public int NewlyCarvedAir { get; }
        public bool BaselineAlternatePath { get; }
        public bool EdgeRemovalAlternatePath { get; }
        public int CycleDelta { get; }
        public Sv5LoopTopologyMetrics Before { get; }
        public Sv5LoopTopologyMetrics After { get; }
        public string Token => LoopId+"|"+FromRoom+"|"+ToRoom+"|"+From+"|"+To+"|"+BaselineCost+"|"+
            FinalCost+"|"+BaselineContacts+"|"+NewlyCarvedAir+"|"+BaselineAlternatePath+"|"+
            EdgeRemovalAlternatePath+"|"+CycleDelta+"|"+Before.Token+"|"+After.Token;
    }

    public sealed class Sv5LoopTopologyPlan
    {
        internal Sv5LoopTopologyPlan(IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell> baseline,
            IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell> final,
            IReadOnlyCollection<Sv5SpecialWorldPoint> baselineFeet,
            IReadOnlyCollection<Sv5SpecialWorldPoint> finalFeet,IEnumerable<Sv5LoopTopologyEdge> baselineEdges,
            IEnumerable<Sv5LoopTopologyEdge> finalEdges,Sv5LoopTopologyMetrics before,
            Sv5LoopTopologyMetrics after,IEnumerable<Sv5LoopTopologyProof> proofs,IEnumerable<string> diagnostics)
        {
            BaselineOccupancy=baseline; FinalOccupancy=final; BaselineFootNodes=baselineFeet; FinalFootNodes=finalFeet;
            BaselineEdges=Array.AsReadOnly(baselineEdges.OrderBy(v=>v).ToArray());
            FinalEdges=Array.AsReadOnly(finalEdges.OrderBy(v=>v).ToArray()); Before=before; After=after;
            Proofs=Array.AsReadOnly(proofs.OrderBy(v=>v.LoopId,StringComparer.Ordinal).ToArray());
            Diagnostics=Array.AsReadOnly((diagnostics ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal)
                .OrderBy(v=>v,StringComparer.Ordinal).ToArray());
            Digest=StarNight.Map.WorldGeneration.WorldData.Sv5WorldDefinition.Hash(
                "SV5_LOOP_TOPOLOGY_FIX01_V1\n"+string.Join("\n",BaselineOccupancy.Values.OrderBy(v=>v.World).Select(v=>v.Token))+"\n"+
                string.Join("\n",FinalOccupancy.Values.OrderBy(v=>v.World).Select(v=>v.Token))+"\n"+
                string.Join("\n",BaselineEdges.Select(v=>v.Token))+"\n"+string.Join("\n",FinalEdges.Select(v=>v.Token))+"\n"+
                string.Join("\n",Proofs.Select(v=>v.Token))+"\n"+string.Join("\n",Diagnostics));
        }
        public IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell> BaselineOccupancy { get; }
        public IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell> FinalOccupancy { get; }
        public IReadOnlyCollection<Sv5SpecialWorldPoint> BaselineFootNodes { get; }
        public IReadOnlyCollection<Sv5SpecialWorldPoint> FinalFootNodes { get; }
        public IReadOnlyList<Sv5LoopTopologyEdge> BaselineEdges { get; }
        public IReadOnlyList<Sv5LoopTopologyEdge> FinalEdges { get; }
        public Sv5LoopTopologyMetrics Before { get; }
        public Sv5LoopTopologyMetrics After { get; }
        public IReadOnlyList<Sv5LoopTopologyProof> Proofs { get; }
        public IReadOnlyList<string> Diagnostics { get; }
        public string Digest { get; }
        public bool Success => Diagnostics.Count==0;
    }

    /// <summary>Seed-scoped immutable baseline evidence and caches shared by every loop evaluation.</summary>
    public sealed class Sv5LoopTopologyContext
    {
        private readonly HashSet<Sv5SpecialWorldPoint> baselineFootSet;
        private readonly ReadOnlyDictionary<Sv5SpecialWorldPoint,IReadOnlyList<Sv5SpecialWorldPoint>> footAdjacency;
        private readonly ReadOnlyDictionary<Sv5SpecialWorldPoint,int> footComponents;
        private readonly Dictionary<string,int> baselineShortestCosts=new Dictionary<string,int>(StringComparer.Ordinal);
        private readonly ReadOnlyDictionary<string,int> roomComponents;
        private readonly HashSet<string> roomVertices;

        internal Sv5LoopTopologyContext(Sv5SpaceGraphPlan plan,
            IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell> baseline)
        {
            Plan=plan ?? throw new ArgumentNullException(nameof(plan));
            BaselineOccupancy=baseline ?? throw new ArgumentNullException(nameof(baseline));
            baselineFootSet=new HashSet<Sv5SpecialWorldPoint>(Sv5LoopTopology.SupportedFootNodes(baseline));
            BaselineFootNodes=Array.AsReadOnly(baselineFootSet.OrderBy(v=>v).ToArray());
            var adjacency=baselineFootSet.OrderBy(v=>v).ToDictionary(v=>v,v=>(IReadOnlyList<Sv5SpecialWorldPoint>)
                Array.AsReadOnly(Sv5LoopTopology.FootNeighbors(v).Where(baselineFootSet.Contains).OrderBy(n=>n).ToArray()));
            footAdjacency=new ReadOnlyDictionary<Sv5SpecialWorldPoint,IReadOnlyList<Sv5SpecialWorldPoint>>(adjacency);
            var components=new Dictionary<Sv5SpecialWorldPoint,int>(); int component=0;
            foreach(var start in baselineFootSet.OrderBy(v=>v))
            {
                if(components.ContainsKey(start)) continue;
                var queue=new Queue<Sv5SpecialWorldPoint>(); queue.Enqueue(start); components[start]=component;
                while(queue.Count!=0)
                {
                    var at=queue.Dequeue();
                    foreach(var next in adjacency[at]) if(!components.ContainsKey(next))
                    { components[next]=component; queue.Enqueue(next); }
                }
                component++;
            }
            footComponents=new ReadOnlyDictionary<Sv5SpecialWorldPoint,int>(components);
            BaselineEdges=Sv5LoopTopology.BaselineEdges(plan);
            Before=Sv5LoopTopology.Measure(BaselineEdges);
            roomVertices=new HashSet<string>(BaselineEdges.SelectMany(v=>new[]{v.FromVertex,v.ToVertex}),StringComparer.Ordinal);
            var roomMap=new Dictionary<string,int>(StringComparer.Ordinal); int roomComponent=0;
            foreach(string start in roomVertices.OrderBy(v=>v,StringComparer.Ordinal))
            {
                if(roomMap.ContainsKey(start)) continue;
                var queue=new Queue<string>(); queue.Enqueue(start); roomMap[start]=roomComponent;
                while(queue.Count!=0)
                {
                    string at=queue.Dequeue();
                    foreach(string next in BaselineEdges.Where(v=>v.FromVertex==at || v.ToVertex==at)
                        .Select(v=>v.FromVertex==at ? v.ToVertex : v.FromVertex).OrderBy(v=>v,StringComparer.Ordinal))
                        if(!roomMap.ContainsKey(next)){roomMap[next]=roomComponent;queue.Enqueue(next);}
                }
                roomComponent++;
            }
            roomComponents=new ReadOnlyDictionary<string,int>(roomMap);
        }

        public Sv5SpaceGraphPlan Plan { get; }
        public IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell> BaselineOccupancy { get; }
        public IReadOnlyCollection<Sv5SpecialWorldPoint> BaselineFootNodes { get; }
        public IReadOnlyDictionary<Sv5SpecialWorldPoint,IReadOnlyList<Sv5SpecialWorldPoint>> FootAdjacency => footAdjacency;
        public IReadOnlyDictionary<Sv5SpecialWorldPoint,int> FootComponents => footComponents;
        public IReadOnlyList<Sv5LoopTopologyEdge> BaselineEdges { get; }
        public Sv5LoopTopologyMetrics Before { get; }
        public int BaselineShortestCacheEntries => baselineShortestCosts.Count;

        public bool IsBaselineFoot(Sv5SpecialWorldPoint point) => baselineFootSet.Contains(point);
        public bool SameFootComponent(Sv5SpecialWorldPoint first,Sv5SpecialWorldPoint second)
            => footComponents.TryGetValue(first,out int left) && footComponents.TryGetValue(second,out int right) && left==right;
        public bool RoomsConnected(string first,string second)
            => first==second || (roomComponents.TryGetValue(first,out int left) &&
                roomComponents.TryGetValue(second,out int right) && left==right);

        public int BaselineShortestCost(Sv5SpecialWorldPoint start,Sv5SpecialWorldPoint goal)
        {
            string key=PointPair(start,goal);
            if(baselineShortestCosts.TryGetValue(key,out int cached)) return cached;
            int result=-1;
            if(SameFootComponent(start,goal))
            {
                var distance=new Dictionary<Sv5SpecialWorldPoint,int>{{start,1}};
                var queue=new Queue<Sv5SpecialWorldPoint>(); queue.Enqueue(start);
                while(queue.Count!=0)
                {
                    var at=queue.Dequeue();
                    if(at.Equals(goal)){result=distance[at];break;}
                    foreach(var next in footAdjacency[at]) if(!distance.ContainsKey(next))
                    { distance[next]=distance[at]+1; queue.Enqueue(next); }
                }
            }
            baselineShortestCosts.Add(key,result); return result;
        }

        public int CycleRankWithAddedEdge(string from,string to)
        {
            int vertices=Before.Vertices+(roomVertices.Contains(from) ? 0 : 1)+
                (!roomVertices.Contains(to) && to!=from ? 1 : 0);
            int components=Before.Components;
            if(!roomVertices.Contains(from)) components++;
            if(!roomVertices.Contains(to) && to!=from) components++;
            if(from!=to && !RoomsConnected(from,to)) components--;
            return Before.Edges+1-vertices+components;
        }

        private static string PointPair(Sv5SpecialWorldPoint first,Sv5SpecialWorldPoint second)
            => first.CompareTo(second)<=0 ? first+"|"+second : second+"|"+first;
    }

    /// <summary>Reconstructs topology evidence from authoritative cells and typed parent/host links.
    /// No stored loop boolean, claimed count, or shortcut label participates in these calculations.</summary>
    public static class Sv5LoopTopology
    {
        private sealed class OverlayOccupancy : IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell>
        {
            private readonly IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell> baseline;
            private readonly IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell> overlay;
            internal OverlayOccupancy(IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell> sourceBaseline,
                IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell> sourceOverlay)
            { baseline=sourceBaseline; overlay=sourceOverlay; }
            public int Count => baseline.Count+overlay.Keys.Count(v=>!baseline.ContainsKey(v));
            public IEnumerable<Sv5SpecialWorldPoint> Keys => baseline.Keys.Concat(overlay.Keys.Where(v=>!baseline.ContainsKey(v)));
            public IEnumerable<Sv5LoopOccupancyCell> Values => Keys.Select(v=>this[v]);
            public Sv5LoopOccupancyCell this[Sv5SpecialWorldPoint key] => overlay.TryGetValue(key,out Sv5LoopOccupancyCell value) ? value : baseline[key];
            public bool ContainsKey(Sv5SpecialWorldPoint key) => overlay.ContainsKey(key) || baseline.ContainsKey(key);
            public bool TryGetValue(Sv5SpecialWorldPoint key,out Sv5LoopOccupancyCell value)
            { if(overlay.TryGetValue(key,out value)) return true; return baseline.TryGetValue(key,out value); }
            public IEnumerator<KeyValuePair<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell>> GetEnumerator()
            {
                foreach(var pair in baseline) yield return overlay.TryGetValue(pair.Key,out Sv5LoopOccupancyCell value) ?
                    new KeyValuePair<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell>(pair.Key,value) : pair;
                foreach(var pair in overlay) if(!baseline.ContainsKey(pair.Key)) yield return pair;
            }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class OverlayFootNodes : IReadOnlyCollection<Sv5SpecialWorldPoint>
        {
            private readonly IReadOnlyCollection<Sv5SpecialWorldPoint> baseline;
            private readonly HashSet<Sv5SpecialWorldPoint> affected;
            private readonly Func<Sv5SpecialWorldPoint,bool> predicate;
            internal OverlayFootNodes(IReadOnlyCollection<Sv5SpecialWorldPoint> sourceBaseline,
                HashSet<Sv5SpecialWorldPoint> sourceAffected,Func<Sv5SpecialWorldPoint,bool> sourcePredicate)
            { baseline=sourceBaseline;affected=sourceAffected;predicate=sourcePredicate; }
            public int Count => this.Count(v=>true);
            public IEnumerator<Sv5SpecialWorldPoint> GetEnumerator()
            {
                foreach(var point in baseline) if(!affected.Contains(point) || predicate(point)) yield return point;
                foreach(var point in affected.OrderBy(v=>v)) if(predicate(point) && !baseline.Contains(point)) yield return point;
            }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public static IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell> CaptureBaseline(
            Sv5SpaceGraphPlan plan)
        {
            if(plan==null || plan.Infill==null) throw new ArgumentException("An infill plan is required.",nameof(plan));
            var cells=new Dictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell>();
            foreach(var pair in Sv5SpaceInfill.KnownBase(plan).OrderBy(v=>v.Key))
                cells[pair.Key]=new Sv5LoopOccupancyCell(pair.Key,pair.Value,"CORE_OR_ROUTE_BASE",string.Empty);
            foreach(var group in plan.Reservations.GroupBy(v=>v.World).OrderBy(v=>v.Key))
            {
                var rows=group.OrderBy(v=>v.Kind).ThenBy(v=>v.OwnerId,StringComparer.Ordinal).ToArray();
                var value=ValueAt(cells,group.Key);
                string provenance="RESERVATION_"+string.Join("+",rows.Select(v=>v.Kind).Distinct());
                string owner=string.Join("|",rows.Select(v=>v.OwnerId).Distinct(StringComparer.Ordinal).OrderBy(v=>v,StringComparer.Ordinal));
                cells[group.Key]=new Sv5LoopOccupancyCell(group.Key,value,provenance,owner);
            }
            foreach(var cell in plan.Infill.Cells.OrderBy(v=>v.World))
                cells[cell.World]=new Sv5LoopOccupancyCell(cell.World,cell.Value,"INFILL_ACTUAL",cell.Owner);
            foreach(var connection in plan.Connections.OrderBy(v=>v.Id,StringComparer.Ordinal))
            {
                foreach(var world in connection.Centerline.Concat(connection.ApertureCells).Distinct().OrderBy(v=>v))
                    if(!cells.TryGetValue(world,out Sv5LoopOccupancyCell old) || old.Value==Sv5InfillCellValue.Air ||
                        old.Provenance=="INFILL_ACTUAL" || old.Value==Sv5InfillCellValue.Unknown)
                        cells[world]=new Sv5LoopOccupancyCell(world,Sv5InfillCellValue.Air,
                            "HOST_CORRIDOR_AIR",connection.Id);
            }
            CarveHostComponentNetworks(plan,cells);
            return new ReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell>(cells);
        }

        public static IReadOnlyList<Sv5LoopTopologyEdge> BaselineEdges(Sv5SpaceGraphPlan plan)
        {
            var roomMap=plan.Infill.Rooms.ToDictionary(v=>v.Id,v=>v,StringComparer.Ordinal);
            var rooms=new HashSet<string>(roomMap.Keys,StringComparer.Ordinal);
            return Array.AsReadOnly(plan.Infill.Links.OrderBy(v=>v.Room,StringComparer.Ordinal).Select(link=>
            {
                string parent=BaseRoom(link.Parent);
                if(parent==link.Room || !rooms.Contains(parent)) parent=link.Host;
                return new Sv5LoopTopologyEdge("SV5_INFILL_EDGE_"+link.Room,link.Room,parent,
                    parent==link.Host ? "INFILL_HOST" : "INFILL_PARENT");
            }).ToArray());
        }

        public static Sv5LoopTopologyEvaluation Evaluate(Sv5LoopTopologyContext context,string loopId,
            string fromRoomId,string toRoomId,IReadOnlyList<Sv5SpecialWorldPoint> footPath,
            IEnumerable<Sv5LoopCell> cells)
        {
            if(context==null) throw new ArgumentNullException(nameof(context));
            var errors=new List<string>(); var path=(footPath ?? Array.Empty<Sv5SpecialWorldPoint>()).ToArray();
            var source=(cells ?? Array.Empty<Sv5LoopCell>()).ToArray();
            var overlay=new Dictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell>();
            foreach(var cell in source)
            {
                var actual=ValueAt(context.BaselineOccupancy,cell.World);
                if(actual!=cell.SourceValue) errors.Add("SOURCE_VALUE_MISMATCH|"+cell.World+"|"+actual+"/"+cell.SourceValue);
                overlay[cell.World]=new Sv5LoopOccupancyCell(cell.World,cell.FinalValue,"LOOP_ACTUAL",loopId);
            }
            var affected=new HashSet<Sv5SpecialWorldPoint>();
            foreach(var point in overlay.Keys) for(int dy=-1;dy<=1;dy++)
                affected.Add(new Sv5SpecialWorldPoint(point.X,point.Y+dy));
            bool IsFinalFoot(Sv5SpecialWorldPoint point)
            {
                if(!affected.Contains(point)) return context.IsBaselineFoot(point);
                return point.Y>0 && point.Y+1<Sv5SpaceGraphPlanner.WorldHeight &&
                    OverlayValue(point)==Sv5InfillCellValue.Air &&
                    OverlayValue(new Sv5SpecialWorldPoint(point.X,point.Y+1))==Sv5InfillCellValue.Air &&
                    OverlayValue(new Sv5SpecialWorldPoint(point.X,point.Y-1))==Sv5InfillCellValue.Solid;
            }
            Sv5InfillCellValue OverlayValue(Sv5SpecialWorldPoint point)
                => overlay.TryGetValue(point,out Sv5LoopOccupancyCell value) ? value.Value :
                    ValueAt(context.BaselineOccupancy,point);

            int contacts=path.Distinct().Count(context.IsBaselineFoot);
            if(path.Length<2 || !context.IsBaselineFoot(path.First()) || !context.IsBaselineFoot(path.Last()) || contacts!=2)
                errors.Add("BASELINE_CONTACTS|"+contacts);
            int newly=path.Distinct().Count(p=>ValueAt(context.BaselineOccupancy,p)!=Sv5InfillCellValue.Air &&
                OverlayValue(p)==Sv5InfillCellValue.Air);
            if(newly<2) errors.Add("NEWLY_CARVED_AIR|"+newly);
            if(path.Any(p=>!IsFinalFoot(p))) errors.Add("FINAL_PATH_NOT_SUPPORTED");
            int beforeCost=path.Length==0 ? -1 : context.BaselineShortestCost(path.First(),path.Last());
            int afterCost=path.Length==0 ? -1 : ShortestCostOverlay(path.First(),path.Last(),context,IsFinalFoot);
            if(beforeCost<=0) errors.Add("BASELINE_FOOT_BFS_MISSING");
            if(afterCost<=0) errors.Add("FINAL_FOOT_BFS_MISSING");
            bool baselineAlternate=context.RoomsConnected(fromRoomId,toRoomId);
            if(!baselineAlternate) errors.Add("BASELINE_ROOM_ALTERNATE_PATH_MISSING");
            bool removalAlternate=Connected(context.BaselineEdges,fromRoomId,toRoomId,string.Empty);
            if(!removalAlternate) errors.Add("LOOP_EDGE_REMOVAL_DISCONNECTS");
            int cycleDelta=context.CycleRankWithAddedEdge(fromRoomId,toRoomId)-context.Before.CycleRank;
            if(cycleDelta!=1) errors.Add("CYCLE_DELTA|"+cycleDelta);
            var view=new OverlayOccupancy(context.BaselineOccupancy,overlay);
            var feetView=new OverlayFootNodes(context.BaselineFootNodes,affected,IsFinalFoot);
            return new Sv5LoopTopologyEvaluation(beforeCost,afterCost,contacts,newly,baselineAlternate,
                removalAlternate,cycleDelta,view,feetView,errors);
        }

        public static Sv5LoopTopologyPlan Build(Sv5LoopTopologyContext context,IEnumerable<Sv5LoopLink> sourceLinks,
            IReadOnlyDictionary<string,Sv5LoopTopologyEvaluation> suppliedEvaluations=null,bool validateStored=true)
        {
            if(context==null) throw new ArgumentNullException(nameof(context));
            var links=(sourceLinks ?? Array.Empty<Sv5LoopLink>()).OrderBy(v=>v.Id,StringComparer.Ordinal).ToArray();
            var finalEdges=context.BaselineEdges.Concat(links.Select(link=>new Sv5LoopTopologyEdge(link.Id,
                link.FromRoomId,link.ToRoomId,"LOOP",link.Id))).ToArray();
            var after=Measure(finalEdges);
            var final=new Dictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell>(context.BaselineOccupancy);
            var affected=new HashSet<Sv5SpecialWorldPoint>(); var diagnostics=new List<string>();
            foreach(var link in links) foreach(var cell in link.Cells)
            {
                if(final.TryGetValue(cell.World,out Sv5LoopOccupancyCell existing) &&
                    existing.Provenance=="LOOP_ACTUAL" && existing.Owner!=link.Id && existing.Value!=cell.FinalValue)
                    diagnostics.Add(link.Id+"|FINAL_OVERLAY_CONFLICT|"+cell.World);
                final[cell.World]=new Sv5LoopOccupancyCell(cell.World,cell.FinalValue,"LOOP_ACTUAL",link.Id);
                for(int dy=-1;dy<=1;dy++) affected.Add(new Sv5SpecialWorldPoint(cell.World.X,cell.World.Y+dy));
            }
            var finalOccupancy=new ReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell>(final);
            var finalFeet=new HashSet<Sv5SpecialWorldPoint>(context.BaselineFootNodes);
            foreach(var point in affected)
            {
                if(IsSupportedFoot(finalOccupancy,point)) finalFeet.Add(point); else finalFeet.Remove(point);
            }
            var proofs=new List<Sv5LoopTopologyProof>();
            foreach(var link in links)
            {
                Sv5LoopTopologyEvaluation evaluation=null;
                if(suppliedEvaluations!=null) suppliedEvaluations.TryGetValue(link.Id,out evaluation);
                evaluation=evaluation ?? Evaluate(context,link.Id,link.FromRoomId,link.ToRoomId,link.FootPath,link.Cells);
                diagnostics.AddRange(evaluation.Errors.Select(v=>link.Id+"|"+v));
                if(validateStored && (link.BaselineCost!=evaluation.BaselineCost || link.NewCost!=evaluation.FinalCost))
                    diagnostics.Add(link.Id+"|STORED_COST_MISMATCH");
                if(validateStored && (link.AlternatePathExists!=evaluation.BaselineAlternatePath || link.CycleDelta!=evaluation.CycleDelta))
                    diagnostics.Add(link.Id+"|STORED_TOPOLOGY_MISMATCH");
                proofs.Add(new Sv5LoopTopologyProof(link,evaluation,context.Before,after));
            }
            if(finalEdges.Where(v=>v.Kind=="LOOP").Any(v=>string.IsNullOrEmpty(v.LoopId))) diagnostics.Add("UNTYPED_LOOP_EDGE");
            if(proofs.Any(v=>v.CycleDelta!=1)) diagnostics.Add("FINAL_CYCLE_PROOF_INVALID");
            return new Sv5LoopTopologyPlan(context.BaselineOccupancy,finalOccupancy,context.BaselineFootNodes,
                Array.AsReadOnly(finalFeet.OrderBy(v=>v).ToArray()),context.BaselineEdges,finalEdges,
                context.Before,after,proofs,diagnostics);
        }

        public static IReadOnlyCollection<Sv5SpecialWorldPoint> SupportedFootNodes(
            IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell> occupancy)
        {
            var result=new HashSet<Sv5SpecialWorldPoint>();
            foreach(var pair in occupancy.Where(v=>v.Value.Value==Sv5InfillCellValue.Air))
            {
                var p=pair.Key;
                if(p.Y>0 && p.Y+1<Sv5SpaceGraphPlanner.WorldHeight &&
                    ValueAt(occupancy,new Sv5SpecialWorldPoint(p.X,p.Y+1))==Sv5InfillCellValue.Air &&
                    ValueAt(occupancy,new Sv5SpecialWorldPoint(p.X,p.Y-1))==Sv5InfillCellValue.Solid) result.Add(p);
            }
            return Array.AsReadOnly(result.OrderBy(v=>v).ToArray());
        }

        private static bool IsSupportedFoot(
            IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell> occupancy,Sv5SpecialWorldPoint point)
            => point.Y>0 && point.Y+1<Sv5SpaceGraphPlanner.WorldHeight &&
               ValueAt(occupancy,point)==Sv5InfillCellValue.Air &&
               ValueAt(occupancy,new Sv5SpecialWorldPoint(point.X,point.Y+1))==Sv5InfillCellValue.Air &&
               ValueAt(occupancy,new Sv5SpecialWorldPoint(point.X,point.Y-1))==Sv5InfillCellValue.Solid;

        private static int ShortestCostOverlay(Sv5SpecialWorldPoint start,Sv5SpecialWorldPoint goal,
            Sv5LoopTopologyContext context,Func<Sv5SpecialWorldPoint,bool> isFoot)
        {
            if(!isFoot(start) || !isFoot(goal)) return -1;
            var queue=new Queue<Sv5SpecialWorldPoint>();
            var distance=new Dictionary<Sv5SpecialWorldPoint,int>{{start,1}}; queue.Enqueue(start);
            while(queue.Count!=0)
            {
                var at=queue.Dequeue(); if(at.Equals(goal)) return distance[at];
                foreach(var next in FootNeighbors(at))
                {
                    if(!isFoot(next) || distance.ContainsKey(next)) continue;
                    distance[next]=distance[at]+1; queue.Enqueue(next);
                }
            }
            return -1;
        }

        public static int ShortestCost(Sv5SpecialWorldPoint start,Sv5SpecialWorldPoint goal,
            IEnumerable<Sv5SpecialWorldPoint> sourceNodes)
        {
            var nodes=sourceNodes as ISet<Sv5SpecialWorldPoint> ?? new HashSet<Sv5SpecialWorldPoint>(sourceNodes);
            if(!nodes.Contains(start) || !nodes.Contains(goal)) return -1;
            var queue=new Queue<Sv5SpecialWorldPoint>(); var distance=new Dictionary<Sv5SpecialWorldPoint,int>{{start,1}};
            queue.Enqueue(start);
            while(queue.Count!=0)
            {
                var p=queue.Dequeue(); if(p.Equals(goal)) return distance[p];
                foreach(int dx in new[]{-1,1}) foreach(int dy in new[]{-1,0,1})
                {
                    var next=new Sv5SpecialWorldPoint(p.X+dx,p.Y+dy);
                    if(nodes.Contains(next) && !distance.ContainsKey(next))
                    { distance[next]=distance[p]+1; queue.Enqueue(next); }
                }
            }
            return -1;
        }

        public static IReadOnlyCollection<Sv5SpecialWorldPoint> ReachableFootNodes(
            IEnumerable<Sv5SpecialWorldPoint> sourceNodes,Sv5SpecialWorldPoint start)
        {
            var nodes=new HashSet<Sv5SpecialWorldPoint>(sourceNodes ?? Array.Empty<Sv5SpecialWorldPoint>());
            return Array.AsReadOnly(Reachable(nodes,new[]{start}).OrderBy(v=>v).ToArray());
        }

        public static Sv5LoopTopologyMetrics Measure(IEnumerable<Sv5LoopTopologyEdge> source)
        {
            var edges=(source ?? Array.Empty<Sv5LoopTopologyEdge>()).ToArray();
            var vertices=new HashSet<string>(edges.SelectMany(v=>new[]{v.FromVertex,v.ToVertex}),StringComparer.Ordinal);
            var adjacency=vertices.ToDictionary(v=>v,v=>new List<KeyValuePair<string,int>>(),StringComparer.Ordinal);
            for(int index=0;index<edges.Length;index++)
            {
                adjacency[edges[index].FromVertex].Add(new KeyValuePair<string,int>(edges[index].ToVertex,index));
                adjacency[edges[index].ToVertex].Add(new KeyValuePair<string,int>(edges[index].FromVertex,index));
            }
            int timer=0,components=0; var discovery=new Dictionary<string,int>(StringComparer.Ordinal);
            var low=new Dictionary<string,int>(StringComparer.Ordinal); var bridgeIds=new HashSet<string>(StringComparer.Ordinal);
            void Visit(string at,int parentEdge)
            {
                discovery[at]=low[at]=++timer;
                foreach(var item in adjacency[at].OrderBy(v=>v.Key,StringComparer.Ordinal).ThenBy(v=>v.Value))
                {
                    string next=item.Key; int edgeIndex=item.Value;
                    if(edgeIndex==parentEdge) continue;
                    if(!discovery.ContainsKey(next))
                    {
                        Visit(next,edgeIndex); low[at]=Math.Min(low[at],low[next]);
                        if(low[next]>discovery[at]) bridgeIds.Add(edges[edgeIndex].Id);
                    }
                    else low[at]=Math.Min(low[at],discovery[next]);
                }
            }
            foreach(string vertex in vertices.OrderBy(v=>v,StringComparer.Ordinal)) if(!discovery.ContainsKey(vertex))
            { components++; Visit(vertex,-1); }
            var bridges=bridgeIds.OrderBy(v=>v,StringComparer.Ordinal).ToArray();
            return new Sv5LoopTopologyMetrics(vertices.Count,edges.Length,components,edges.Length-vertices.Count+components,bridges);
        }

        private static void CarveHostComponentNetworks(Sv5SpaceGraphPlan plan,
            IDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell> cells)
        {
            var roomHosts=plan.Infill.Rooms.ToDictionary(v=>v.Id,v=>v.Host,StringComparer.Ordinal);
            var infillOwner=plan.Infill.Cells.GroupBy(v=>v.World).ToDictionary(v=>v.Key,
                v=>v.OrderBy(c=>c.Owner,StringComparer.Ordinal).First().Owner);
            var initialView=new ReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell>(
                (Dictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell>)cells);
            var initialFeet=new HashSet<Sv5SpecialWorldPoint>(SupportedFootNodes(initialView));
            var componentByFoot=new Dictionary<Sv5SpecialWorldPoint,int>();
            var componentCells=new Dictionary<int,HashSet<Sv5SpecialWorldPoint>>(); int componentId=0;
            foreach(var start in initialFeet.OrderBy(v=>v))
            {
                if(componentByFoot.ContainsKey(start)) continue;
                var component=Reachable(initialFeet,new[]{start});
                componentCells[componentId]=component;
                foreach(var p in component) componentByFoot[p]=componentId;
                componentId++;
            }
            foreach(string host in roomHosts.Values.Distinct(StringComparer.Ordinal).OrderBy(v=>v,StringComparer.Ordinal))
            {
                var ids=initialFeet.Where(p=>infillOwner.TryGetValue(p,out string owner) &&
                        roomHosts.TryGetValue(BaseRoom(owner),out string ownerHost) && ownerHost==host)
                    .Select(p=>componentByFoot[p]).Distinct().OrderBy(id=>componentCells[id].Min()).ToArray();
                if(ids.Length<2) continue;
                var network=new HashSet<Sv5SpecialWorldPoint>(componentCells[ids[0]]);
                foreach(int id in ids.Skip(1))
                {
                    var view=new ReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell>(
                        (Dictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell>)cells);
                    var existing=new HashSet<Sv5SpecialWorldPoint>(SupportedFootNodes(view));
                    network=Reachable(existing,network);
                    var terminal=Reachable(existing,componentCells[id].Where(existing.Contains));
                    if(terminal.Count==0 || terminal.Any(network.Contains)) continue;
                    var nearest=terminal.SelectMany(a=>network.Select(b=>new{A=a,B=b,
                        D=Math.Abs(a.X-b.X)+Math.Abs(a.Y-b.Y)})).OrderBy(v=>v.D).ThenBy(v=>v.A).ThenBy(v=>v.B).First();
                    IReadOnlyList<Sv5SpecialWorldPoint> path=Array.AsReadOnly(Array.Empty<Sv5SpecialWorldPoint>());
                    foreach(int margin in new[]{4,8,16,32,64})
                    {
                        int minX=Math.Max(0,Math.Min(nearest.A.X,nearest.B.X)-margin);
                        int maxX=Math.Min(Sv5SpaceGraphPlanner.WorldWidth-1,Math.Max(nearest.A.X,nearest.B.X)+margin);
                        int minY=Math.Max(1,Math.Min(nearest.A.Y,nearest.B.Y)-margin);
                        int maxY=Math.Min(Sv5SpaceGraphPlanner.WorldHeight-2,Math.Max(nearest.A.Y,nearest.B.Y)+margin);
                        var possible=new HashSet<Sv5SpecialWorldPoint>(existing);
                        for(int y=minY;y<=maxY;y++) for(int x=minX;x<=maxX;x++)
                        {
                            var p=new Sv5SpecialWorldPoint(x,y);
                            if(CanPlaceFootAnywhere(p,cells)) possible.Add(p);
                        }
                        path=FindPath(possible,terminal,network);
                        if(path.Count!=0) break;
                    }
                    foreach(var p in path)
                    {
                        SetBackbone(new Sv5SpecialWorldPoint(p.X,p.Y-1),Sv5InfillCellValue.Solid,
                            "HOST_COMPONENT_SUPPORT",host,cells);
                        SetBackbone(p,Sv5InfillCellValue.Air,"HOST_COMPONENT_AIR",host,cells);
                        SetBackbone(new Sv5SpecialWorldPoint(p.X,p.Y+1),Sv5InfillCellValue.Air,
                            "HOST_COMPONENT_AIR",host,cells);
                    }
                    network.UnionWith(path); network.UnionWith(terminal);
                }
            }
        }

        private static bool CanPlaceFootAnywhere(Sv5SpecialWorldPoint p,
            IDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell> cells)
            => CanAssignAnywhere(p,Sv5InfillCellValue.Air,cells) &&
               CanAssignAnywhere(new Sv5SpecialWorldPoint(p.X,p.Y+1),Sv5InfillCellValue.Air,cells) &&
               CanAssignAnywhere(new Sv5SpecialWorldPoint(p.X,p.Y-1),Sv5InfillCellValue.Solid,cells);

        private static bool CanAssignAnywhere(Sv5SpecialWorldPoint p,Sv5InfillCellValue value,
            IDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell> cells)
        {
            if(p.X<0 || p.X>=Sv5SpaceGraphPlanner.WorldWidth || p.Y<0 || p.Y>=Sv5SpaceGraphPlanner.WorldHeight)
                return false;
            if(cells.TryGetValue(p,out Sv5LoopOccupancyCell old) && old.Provenance.IndexOf("ConditionalGate",StringComparison.Ordinal)>=0)
                return false;
            return !cells.TryGetValue(p,out old) || old.Value==Sv5InfillCellValue.Unknown || old.Value==value ||
                MutableHostCell(old);
        }

        private static void SetBackbone(Sv5SpecialWorldPoint p,Sv5InfillCellValue value,string provenance,
            string owner,IDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell> cells)
        {
            if(cells.TryGetValue(p,out Sv5LoopOccupancyCell old) &&
                old.Value!=Sv5InfillCellValue.Unknown && !MutableHostCell(old)) return;
            string combined=old==null || string.IsNullOrEmpty(old.Provenance) ? provenance :
                old.Provenance+"+"+provenance;
            cells[p]=new Sv5LoopOccupancyCell(p,value,combined,owner);
        }

        private static bool MutableHostCell(Sv5LoopOccupancyCell cell)
            => cell!=null && (cell.Provenance=="INFILL_ACTUAL" ||
                cell.Provenance.IndexOf("HOST_CORRIDOR_AIR",StringComparison.Ordinal)>=0 ||
                cell.Provenance.IndexOf("CorridorClearance",StringComparison.Ordinal)>=0 ||
                cell.Provenance.IndexOf("CorridorCenterline",StringComparison.Ordinal)>=0 ||
                cell.Provenance.IndexOf("PortAperture",StringComparison.Ordinal)>=0);

        private static HashSet<Sv5SpecialWorldPoint> Reachable(ISet<Sv5SpecialWorldPoint> nodes,
            IEnumerable<Sv5SpecialWorldPoint> starts)
        {
            var reached=new HashSet<Sv5SpecialWorldPoint>(starts.Where(nodes.Contains));
            var queue=new Queue<Sv5SpecialWorldPoint>(reached);
            while(queue.Count!=0)
            {
                var p=queue.Dequeue();
                foreach(var n in FootNeighbors(p)) if(nodes.Contains(n) && reached.Add(n)) queue.Enqueue(n);
            }
            return reached;
        }

        private static IReadOnlyList<Sv5SpecialWorldPoint> FindPath(ISet<Sv5SpecialWorldPoint> nodes,
            IEnumerable<Sv5SpecialWorldPoint> starts,ISet<Sv5SpecialWorldPoint> targets)
        {
            var queue=new Queue<Sv5SpecialWorldPoint>();
            var parent=new Dictionary<Sv5SpecialWorldPoint,Sv5SpecialWorldPoint>();
            foreach(var start in starts.Where(nodes.Contains).OrderBy(v=>v))
                if(!parent.ContainsKey(start)){parent[start]=start;queue.Enqueue(start);}
            Sv5SpecialWorldPoint found=default(Sv5SpecialWorldPoint); bool success=false;
            while(queue.Count!=0 && !success)
            {
                var p=queue.Dequeue();
                if(targets.Contains(p)){found=p;success=true;break;}
                foreach(var n in FootNeighbors(p).Where(nodes.Contains).OrderBy(v=>v))
                    if(!parent.ContainsKey(n)){parent[n]=p;queue.Enqueue(n);}
            }
            if(!success) return Array.AsReadOnly(Array.Empty<Sv5SpecialWorldPoint>());
            var result=new List<Sv5SpecialWorldPoint>{found};
            while(!parent[result[result.Count-1]].Equals(result[result.Count-1]))
                result.Add(parent[result[result.Count-1]]);
            result.Reverse(); return Array.AsReadOnly(result.ToArray());
        }

        internal static IEnumerable<Sv5SpecialWorldPoint> FootNeighbors(Sv5SpecialWorldPoint p)
        {
            foreach(int dx in new[]{-1,1}) foreach(int dy in new[]{-1,0,1})
                yield return new Sv5SpecialWorldPoint(p.X+dx,p.Y+dy);
        }

        public static bool Connected(IEnumerable<Sv5LoopTopologyEdge> source,string from,string to,string omittedEdgeId)
        {
            if(from==to) return true;
            var edges=(source ?? Array.Empty<Sv5LoopTopologyEdge>()).Where(v=>v.Id!=omittedEdgeId).ToArray();
            var vertices=new HashSet<string>(edges.SelectMany(v=>new[]{v.FromVertex,v.ToVertex}),StringComparer.Ordinal);
            if(!vertices.Contains(from) || !vertices.Contains(to)) return false;
            var visited=new HashSet<string>(StringComparer.Ordinal){from}; var queue=new Queue<string>(); queue.Enqueue(from);
            while(queue.Count!=0)
            {
                string at=queue.Dequeue();
                foreach(var next in edges.Where(v=>v.FromVertex==at || v.ToVertex==at)
                    .Select(v=>v.FromVertex==at ? v.ToVertex : v.FromVertex))
                {
                    if(next==to) return true;
                    if(visited.Add(next)) queue.Enqueue(next);
                }
            }
            return false;
        }

        public static Sv5InfillCellValue ValueAt(
            IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell> occupancy,Sv5SpecialWorldPoint point)
            => occupancy.TryGetValue(point,out Sv5LoopOccupancyCell cell) ? cell.Value : Sv5InfillCellValue.Unknown;

        internal static string BaseRoom(string owner) => owner!=null && owner.EndsWith("_LINK",StringComparison.Ordinal) ?
            owner.Substring(0,owner.Length-5) : owner ?? string.Empty;
    }
}
