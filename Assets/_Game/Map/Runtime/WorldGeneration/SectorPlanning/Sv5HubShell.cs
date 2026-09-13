using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.WorldData;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum Sv5HubCellRole { Air = 1, Solid = 2, ReservedTree = 3, PortNeck = 4 }
    public enum Sv5HubSide { Left = 1, Right = 2 }
    public enum Sv5HubTier { Low = 1, Mid = 2, High = 3 }

    public sealed class Sv5HubProfile
    {
        public Sv5HubProfile(bool enabled = true, int targetConnections = 6, int minimumConnections = 4,
            int maximumConnections = 6)
        {
            if (minimumConnections < 4 || maximumConnections > 6 || targetConnections < minimumConnections ||
                targetConnections > maximumConnections) throw new ArgumentException("Invalid hub-shell profile.");
            Enabled = enabled; TargetConnections = targetConnections; MinimumConnections = minimumConnections;
            MaximumConnections = maximumConnections;
        }
        public bool Enabled { get; }
        public int TargetConnections { get; }
        public int MinimumConnections { get; }
        public int MaximumConnections { get; }
        public int InnerWidth => 12;
        public int InnerHeight => 30;
        public int FootprintWidth => 24;
        public int FootprintHeight => 40;
        public int PatternSize => 4;
        public string Digest => RmapWorldDefinition.Hash("SV5_HUB_SHELL_PROFILE_V1|624|416|12|30|24|40|4|3|3|6|7|"+
            TargetConnections+"|"+MinimumConnections+"|"+MaximumConnections);
    }

    public sealed class Sv5HubCell : IComparable<Sv5HubCell>
    {
        internal Sv5HubCell(string hubId, RmapSpecialWorldPoint world, Sv5HubCellRole role,
            Sv5InfillCellValue sourceValue, Sv5InfillCellValue finalValue)
        {
            HubId = hubId; World = world; Role = role; SourceValue = sourceValue; FinalValue = finalValue;
            MicroX = world.X / 4; MicroY = world.Y / 4;
            MicroPatternOwner = hubId+"@"+MicroX.ToString(CultureInfo.InvariantCulture)+":"+
                MicroY.ToString(CultureInfo.InvariantCulture);
        }
        public string HubId { get; }
        public RmapSpecialWorldPoint World { get; }
        public Sv5HubCellRole Role { get; }
        public Sv5InfillCellValue SourceValue { get; }
        public Sv5InfillCellValue FinalValue { get; }
        public int MicroX { get; }
        public int MicroY { get; }
        public string MicroPatternOwner { get; }
        public string ExportRole => Role == Sv5HubCellRole.ReservedTree ? "RESERVED_TREE" :
            Role == Sv5HubCellRole.PortNeck ? "PORT_NECK" : Role.ToString().ToUpperInvariant();
        public string DigestToken => HubId+"|"+World.X+"|"+World.Y+"|"+ExportRole+"|"+MicroX+"|"+MicroY;
        public int CompareTo(Sv5HubCell other) => other == null ? 1 : World.CompareTo(other.World);
    }

    public sealed class Sv5HubSocket : IComparable<Sv5HubSocket>
    {
        internal Sv5HubSocket(string id, Sv5HubSide side, Sv5HubTier tier, RmapSpecialWorldPoint anchor,
            int apertureHeight, bool active)
        { Id=id; Side=side; Tier=tier; Anchor=anchor; ApertureHeight=apertureHeight; Active=active; }
        public string Id { get; }
        public Sv5HubSide Side { get; }
        public Sv5HubTier Tier { get; }
        public RmapSpecialWorldPoint Anchor { get; }
        public int ApertureHeight { get; }
        public bool Active { get; }
        public int CompareTo(Sv5HubSocket other) => other == null ? 1 : string.Compare(Id,other.Id,StringComparison.Ordinal);
    }

    public sealed class Sv5HubPort : IComparable<Sv5HubPort>
    {
        internal Sv5HubPort(string id, string socketId, RmapSpecialWorldPoint anchor)
        { Id=id; SocketId=socketId; Anchor=anchor; }
        public string Id { get; }
        public string SocketId { get; }
        public RmapSpecialWorldPoint Anchor { get; }
        public int CompareTo(Sv5HubPort other) => other == null ? 1 : string.Compare(Id,other.Id,StringComparison.Ordinal);
    }

    public sealed class Sv5HubConnection : IComparable<Sv5HubConnection>
    {
        private readonly ReadOnlyCollection<RmapSpecialWorldPoint> centerline;
        internal Sv5HubConnection(string id, string portId, string socketId, string externalRoomId,
            string externalSpaceGroupId, RmapSpecialWorldPoint externalAnchor,
            IEnumerable<RmapSpecialWorldPoint> sourceCenterline)
        {
            Id=id; PortId=portId; SocketId=socketId; ExternalRoomId=externalRoomId;
            ExternalSpaceGroupId=externalSpaceGroupId; ExternalAnchor=externalAnchor;
            centerline=Array.AsReadOnly((sourceCenterline ?? Array.Empty<RmapSpecialWorldPoint>()).ToArray());
            if(centerline.Count<2 || !centerline.Last().Equals(externalAnchor))
                throw new ArgumentException("A hub connection must reach its external anchor.");
        }
        public string Id { get; }
        public string PortId { get; }
        public string SocketId { get; }
        public string ExternalRoomId { get; }
        public string ExternalSpaceGroupId { get; }
        public RmapSpecialWorldPoint ExternalAnchor { get; }
        public IReadOnlyList<RmapSpecialWorldPoint> Centerline => centerline;
        public bool ProtectedOverlap => false;
        public bool Type0Overlap => false;
        public bool ProgressionBypass => false;
        public int CompareTo(Sv5HubConnection other) => other == null ? 1 : string.Compare(Id,other.Id,StringComparison.Ordinal);
    }

    public sealed class Sv5HubCandidate : IComparable<Sv5HubCandidate>
    {
        internal Sv5HubCandidate(string id, Sv5SpaceBounds bounds, int availableConnections, int pathCells,
            string status, string reason)
        { Id=id; Bounds=bounds; AvailableConnections=availableConnections; PathCells=pathCells; Status=status; Reason=reason; }
        public string Id { get; }
        public Sv5SpaceBounds Bounds { get; }
        public int AvailableConnections { get; }
        public int PathCells { get; }
        public string Status { get; internal set; }
        public string Reason { get; internal set; }
        public int CompareTo(Sv5HubCandidate other) => other == null ? 1 : string.Compare(Id,other.Id,StringComparison.Ordinal);
    }

    public sealed class Sv5HubPerformance
    {
        internal Sv5HubPerformance(IEnumerable<KeyValuePair<string,double>> timings, int candidates,
            int endpoints, int localCells)
        {
            TimingsMilliseconds=new ReadOnlyDictionary<string,double>((timings ?? Array.Empty<KeyValuePair<string,double>>())
                .OrderBy(v=>v.Key,StringComparer.Ordinal).ToDictionary(v=>v.Key,v=>v.Value,StringComparer.Ordinal));
            CandidateCount=candidates; IndexedEndpointCount=endpoints; LocalAdjacencyCells=localCells;
        }
        public IReadOnlyDictionary<string,double> TimingsMilliseconds { get; }
        public int CandidateCount { get; }
        public int IndexedEndpointCount { get; }
        public int LocalAdjacencyCells { get; }
        public int WholeWorldCopyPerCandidate => 0;
        public int WholeWorldBfsPerCandidate => 0;
        public int GlobalProductRuns => 1;
    }

    public sealed class Sv5HubShellPlan
    {
        internal Sv5HubShellPlan(string baselineDigest, Sv5HubProfile profile, IEnumerable<Sv5HubCandidate> candidates,
            string hubId, Sv5SpaceBounds footprint, IEnumerable<Sv5HubCell> cells, IEnumerable<Sv5HubSocket> sockets,
            IEnumerable<Sv5HubPort> ports, IEnumerable<Sv5HubConnection> connections,
            IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell> baselineOccupancy,
            IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell> finalOccupancy,
            IEnumerable<string> diagnostics, Sv5HubPerformance performance)
        {
            BaselineDigest=baselineDigest; Profile=profile; Candidates=Array.AsReadOnly(candidates.OrderBy(v=>v).ToArray());
            HubId=hubId ?? string.Empty; Footprint=footprint; Cells=Array.AsReadOnly(cells.OrderBy(v=>v).ToArray());
            Sockets=Array.AsReadOnly(sockets.OrderBy(v=>v).ToArray()); Ports=Array.AsReadOnly(ports.OrderBy(v=>v).ToArray());
            Connections=Array.AsReadOnly(connections.OrderBy(v=>v).ToArray()); BaselineOccupancy=baselineOccupancy;
            FinalOccupancy=finalOccupancy; Diagnostics=Array.AsReadOnly((diagnostics ?? Array.Empty<string>())
                .Distinct(StringComparer.Ordinal).OrderBy(v=>v,StringComparer.Ordinal).ToArray()); Performance=performance;
            CellDigest=RmapWorldDefinition.Hash(string.Join("\n",Cells.Select(v=>v.DigestToken)
                .OrderBy(v=>v,StringComparer.Ordinal)));
            Digest=RmapWorldDefinition.Hash("SV5_HUB_SHELL_ACTUAL_CELL_V1\n"+BaselineDigest+"\n"+Profile.Digest+"\n"+
                HubId+"\n"+Footprint+"\n"+CellDigest+"\n"+string.Join("\n",Sockets.Select(v=>v.Id+"|"+v.Side+"|"+
                v.Tier+"|"+v.Anchor+"|"+v.ApertureHeight+"|"+v.Active))+"\n"+
                string.Join("\n",Connections.Select(v=>v.Id+"|"+v.PortId+"|"+v.ExternalRoomId+"|"+
                v.ExternalSpaceGroupId+"|"+string.Join(";",v.Centerline)))+"\n"+string.Join("\n",Diagnostics));
        }
        public string BaselineDigest { get; }
        public Sv5HubProfile Profile { get; }
        public IReadOnlyList<Sv5HubCandidate> Candidates { get; }
        public string HubId { get; }
        public Sv5SpaceBounds Footprint { get; }
        public IReadOnlyList<Sv5HubCell> Cells { get; }
        public IReadOnlyList<Sv5HubSocket> Sockets { get; }
        public IReadOnlyList<Sv5HubPort> Ports { get; }
        public IReadOnlyList<Sv5HubConnection> Connections { get; }
        public IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell> BaselineOccupancy { get; }
        public IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell> FinalOccupancy { get; }
        public IReadOnlyList<string> Diagnostics { get; }
        public Sv5HubPerformance Performance { get; }
        public string CellDigest { get; }
        public string Digest { get; }
        public bool TreeGrabGeometryReady => false;
        public bool ComposedGeometryReady => false;
        public bool PlayerVerified => false;
        public bool Success => Diagnostics.Count==0 && HubId.Length!=0 && Sockets.Count==6 && Ports.Count>=4 && Ports.Count<=6 &&
            Connections.Count==Ports.Count && Connections.Select(v=>v.ExternalRoomId).Distinct(StringComparer.Ordinal).Count()==Connections.Count &&
            Connections.Select(v=>v.ExternalSpaceGroupId).Distinct(StringComparer.Ordinal).Count()==Connections.Count;
    }

    public static class Sv5HubShell
    {
        private sealed class SocketSpec
        {
            public Sv5HubSide Side; public Sv5HubTier Tier; public RmapSpecialWorldPoint Anchor; public int Height; public int ApertureStart;
        }
        private sealed class Assignment
        { public SocketSpec Socket; public Sv5SidepathEndpoint Endpoint; public IReadOnlyList<RmapSpecialWorldPoint> Path; }
        private sealed class Attempt
        { public Sv5HubCandidate Candidate; public IReadOnlyList<Assignment> Assignments; }

        public static Sv5HubShellPlan Build(Sv5SpaceGraphPlan plan, Sv5HubProfile profile = null)
        {
            if(plan==null || plan.Sidepaths==null || !plan.Sidepaths.Success)
                throw new ArgumentException("A passing PlanWithSidepaths result is required.",nameof(plan));
            profile=profile ?? new Sv5HubProfile();
            var total=Stopwatch.StartNew(); var timings=new SortedDictionary<string,double>(StringComparer.Ordinal);
            T Time<T>(string key,Func<T> action){var watch=Stopwatch.StartNew();try{return action();}finally{watch.Stop();timings[key]=Ms(watch);}}
            var baseline=plan.Sidepaths.FinalOccupancy;
            var footprintBlocked=Time("source_index_ms",()=>FootprintBlockedCells(plan));
            var endpoints=plan.Sidepaths.EndpointIndex.Endpoints.OrderBy(v=>v).ToArray();
            var attempts=Time("candidate_evaluation_ms",()=>Enumerate(plan,profile,footprintBlocked,endpoints).ToArray());
            var eligible=attempts.Where(v=>v.Assignments.Count>=profile.MinimumConnections)
                .OrderByDescending(v=>v.Assignments.Count).ThenBy(v=>v.Candidate.PathCells)
                .ThenBy(v=>v.Candidate.Id,StringComparer.Ordinal).ToArray();
            var diagnostics=new List<string>();
            if(eligible.Length==0) diagnostics.Add("HUB_NO_CANDIDATE_WITH_FOUR_DISTINCT_CONNECTIONS|MAX="+
                attempts.Max(v=>v.Candidate.AvailableConnections).ToString(CultureInfo.InvariantCulture));
            var chosen=eligible.FirstOrDefault();
            foreach(var attempt in attempts)
            {
                if(attempt==chosen){attempt.Candidate.Status="ACCEPTED";attempt.Candidate.Reason=string.Empty;}
                else if(attempt.Candidate.Status=="ELIGIBLE"){attempt.Candidate.Status="REJECTED";attempt.Candidate.Reason="LOWER_RANK";}
            }
            if(chosen==null)
            {
                total.Stop(); timings["total_build_ms"]=Ms(total);
                var empty=new ReadOnlyDictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell>(new Dictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell>(baseline));
                return new Sv5HubShellPlan(plan.Digest,profile,attempts.Select(v=>v.Candidate),string.Empty,
                    new Sv5SpaceBounds(0,0,24,40),Array.Empty<Sv5HubCell>(),Array.Empty<Sv5HubSocket>(),
                    Array.Empty<Sv5HubPort>(),Array.Empty<Sv5HubConnection>(),baseline,empty,diagnostics,
                    new Sv5HubPerformance(timings,attempts.Length,endpoints.Length,0));
            }
            string hubId="SV5_HUB_"+RmapWorldDefinition.Hash("SV5_HUB|"+plan.Seed+"|"+chosen.Candidate.Bounds+"|"+
                string.Join(";",chosen.Assignments.Select(v=>v.Endpoint.EndpointId))).Substring(0,20);
            var assignments=chosen.Assignments.Take(profile.MaximumConnections).ToArray();
            var sockets=Specs(chosen.Candidate.Bounds).Select((spec,index)=>new Sv5HubSocket(
                hubId+"_SOCKET_"+(index+1).ToString("00",CultureInfo.InvariantCulture),spec.Side,spec.Tier,spec.Anchor,
                spec.Height,assignments.Any(v=>Same(v.Socket,spec)))).ToArray();
            var ports=new List<Sv5HubPort>(); var connections=new List<Sv5HubConnection>();
            foreach(var assignment in assignments)
            {
                int index=Array.FindIndex(sockets,v=>v.Side==assignment.Socket.Side && v.Tier==assignment.Socket.Tier);
                var socket=sockets[index]; string portId=hubId+"_PORT_"+(index+1).ToString("00",CultureInfo.InvariantCulture);
                ports.Add(new Sv5HubPort(portId,socket.Id,socket.Anchor));
                connections.Add(new Sv5HubConnection(hubId+"_CONNECTION_"+(index+1).ToString("00",CultureInfo.InvariantCulture),
                    portId,socket.Id,assignment.Endpoint.RoomId,assignment.Endpoint.SpaceGroupId,
                    assignment.Endpoint.World,assignment.Path));
            }
            var cells=Time("accepted_overlay_ms",()=>BuildCells(hubId,chosen.Candidate.Bounds,sockets,baseline));
            var final=new Dictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell>(baseline);
            foreach(var cell in cells) final[cell.World]=new Sv5LoopOccupancyCell(cell.World,cell.FinalValue,"HUB_SHELL_ACTUAL",cell.MicroPatternOwner);
            if(!ConnectedPorts(cells,ports))diagnostics.Add("HUB_PORT_COMPONENT_SPLIT");
            if(cells.Any(v=>footprintBlocked.Contains(v.World)))diagnostics.Add("HUB_PROTECTED_OVERLAP");
            if(!plan.PhysicalProduct.Success || plan.ProjectionProofs.Count!=6)diagnostics.Add("HUB_BASELINE_PRODUCT_INVALID");
            total.Stop();timings["total_build_ms"]=Ms(total);
            return new Sv5HubShellPlan(plan.Digest,profile,attempts.Select(v=>v.Candidate),hubId,chosen.Candidate.Bounds,
                cells,sockets,ports,connections,baseline,new ReadOnlyDictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell>(final),
                diagnostics,new Sv5HubPerformance(timings,attempts.Length,endpoints.Length,cells.Count));
        }

        private static IEnumerable<Attempt> Enumerate(Sv5SpaceGraphPlan plan,Sv5HubProfile profile,
            HashSet<RmapSpecialWorldPoint> footprintBlocked,Sv5SidepathEndpoint[] endpoints)
        {
            for(int y=8;y<=416-profile.FootprintHeight-8;y+=8)
            for(int x=24;x<=624-profile.FootprintWidth-24;x+=8)
            {
                var bounds=new Sv5SpaceBounds(x,y,profile.FootprintWidth,profile.FootprintHeight);
                string id="HUB_CANDIDATE_"+RmapWorldDefinition.Hash(plan.Seed+"|"+bounds).Substring(0,16);
                if(FootprintBlocked(bounds,footprintBlocked))
                { yield return new Attempt{Candidate=new Sv5HubCandidate(id,bounds,0,0,"REJECTED","FOOTPRINT_CONFLICT"),Assignments=Array.Empty<Assignment>()}; continue; }
                var assignments=new List<Assignment>();var rooms=new HashSet<string>(StringComparer.Ordinal);
                var groups=new HashSet<string>(StringComparer.Ordinal);
                var socketOptions=Specs(bounds).Select(spec=>new
                    {Spec=spec,Endpoints=Options(spec,bounds,endpoints).ToArray()})
                    .OrderBy(v=>v.Endpoints.Length).ThenBy(v=>v.Spec.Side).ThenBy(v=>v.Spec.Tier).ToArray();
                foreach(var option in socketOptions)
                {
                    var spec=option.Spec;
                    Assignment selected=null;
                    foreach(var endpoint in option.Endpoints)
                    {
                        if(rooms.Contains(endpoint.RoomId)||groups.Contains(endpoint.SpaceGroupId))continue;
                        var path=Path(spec.Anchor,endpoint.World);
                        if(path.Count>120)continue;
                        selected=new Assignment{Socket=spec,Endpoint=endpoint,Path=path};break;
                    }
                    if(selected==null)continue;
                    assignments.Add(selected);rooms.Add(selected.Endpoint.RoomId);groups.Add(selected.Endpoint.SpaceGroupId);
                }
                assignments=assignments.OrderBy(v=>v.Socket.Side).ThenBy(v=>v.Socket.Tier).Take(profile.TargetConnections).ToList();
                int pathCells=assignments.SelectMany(v=>v.Path).Distinct().Count();
                string status=assignments.Count>=profile.MinimumConnections?"ELIGIBLE":"REJECTED";
                string reason=status=="ELIGIBLE"?string.Empty:"CONNECTION_SHORTFALL";
                yield return new Attempt{Candidate=new Sv5HubCandidate(id,bounds,assignments.Count,pathCells,status,reason),Assignments=assignments};
            }
        }

        private static IEnumerable<Sv5SidepathEndpoint> Options(SocketSpec spec,Sv5SpaceBounds bounds,
            IEnumerable<Sv5SidepathEndpoint> endpoints) => endpoints.Where(endpoint=>spec.Side==Sv5HubSide.Left ?
                endpoint.ExitX==1 && endpoint.World.X<bounds.X : endpoint.ExitX==-1 && endpoint.World.X>=bounds.MaxXExclusive)
            .OrderBy(endpoint=>Math.Abs(endpoint.World.Y-spec.Anchor.Y)*4+Math.Abs(endpoint.World.X-spec.Anchor.X))
            .ThenBy(endpoint=>endpoint.RoomId,StringComparer.Ordinal).ThenBy(endpoint=>endpoint.EndpointId,StringComparer.Ordinal);
        private static IReadOnlyList<RmapSpecialWorldPoint> Path(RmapSpecialWorldPoint start,RmapSpecialWorldPoint end)
        {
            var result=new List<RmapSpecialWorldPoint>{start};int x=start.X,y=start.Y;
            while(y!=end.Y){y+=Math.Sign(end.Y-y);result.Add(new RmapSpecialWorldPoint(x,y));}
            while(x!=end.X){x+=Math.Sign(end.X-x);result.Add(new RmapSpecialWorldPoint(x,y));}
            return Array.AsReadOnly(result.ToArray());
        }
        private static SocketSpec[] Specs(Sv5SpaceBounds bounds)
        {
            int[] starts={6,17,28};int[] heights={6,7,6};var tiers=new[]{Sv5HubTier.Low,Sv5HubTier.Mid,Sv5HubTier.High};
            var result=new List<SocketSpec>();foreach(var side in new[]{Sv5HubSide.Left,Sv5HubSide.Right})for(int i=0;i<3;i++)
                result.Add(new SocketSpec{Side=side,Tier=tiers[i],Height=heights[i],ApertureStart=bounds.Y+starts[i],
                    Anchor=new RmapSpecialWorldPoint(side==Sv5HubSide.Left?bounds.X:bounds.MaxXExclusive-1,bounds.Y+starts[i]+heights[i]/2)});
            return result.ToArray();
        }
        private static bool Same(SocketSpec first,SocketSpec second)=>first.Side==second.Side&&first.Tier==second.Tier;
        private static bool FootprintBlocked(Sv5SpaceBounds bounds,ISet<RmapSpecialWorldPoint> blocked)
        {
            for(int y=bounds.Y;y<bounds.MaxYExclusive;y++)for(int x=bounds.X;x<bounds.MaxXExclusive;x++)
                if(blocked.Contains(new RmapSpecialWorldPoint(x,y)))return true;return false;
        }
        private static HashSet<RmapSpecialWorldPoint> FootprintBlockedCells(Sv5SpaceGraphPlan plan)
        {
            var result=new HashSet<RmapSpecialWorldPoint>(Sv5SpaceSidepaths.ProtectedCells(plan));
            result.UnionWith(plan.Reservations.Select(v=>v.World));result.UnionWith(plan.Infill.Cells.Select(v=>v.World));
            result.UnionWith(plan.Loops.Cells.Select(v=>v.World));result.UnionWith(plan.Sidepaths.Cells.Select(v=>v.World));return result;
        }
        private static IReadOnlyList<Sv5HubCell> BuildCells(string hubId,Sv5SpaceBounds bounds,
            IEnumerable<Sv5HubSocket> sockets,IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell> baseline)
        {
            var roles=new Dictionary<RmapSpecialWorldPoint,Sv5HubCellRole>();
            for(int y=bounds.Y;y<bounds.MaxYExclusive;y++)for(int x=bounds.X;x<bounds.MaxXExclusive;x++)
                roles[new RmapSpecialWorldPoint(x,y)]=Sv5HubCellRole.Solid;
            for(int y=bounds.Y+5;y<bounds.Y+35;y++)for(int x=bounds.X+6;x<bounds.X+18;x++)
                roles[new RmapSpecialWorldPoint(x,y)]=Sv5HubCellRole.Air;
            for(int y=bounds.Y+8;y<bounds.Y+32;y++)for(int x=bounds.X+10;x<bounds.X+14;x++)
                roles[new RmapSpecialWorldPoint(x,y)]=Sv5HubCellRole.ReservedTree;
            foreach(var socket in sockets.Where(v=>v.Active))
            {
                int start=socket.Anchor.Y-socket.ApertureHeight/2;
                int minX=socket.Side==Sv5HubSide.Left?bounds.X:bounds.X+17;
                int maxX=socket.Side==Sv5HubSide.Left?bounds.X+6:bounds.MaxXExclusive-1;
                for(int y=start;y<start+socket.ApertureHeight;y++)for(int x=minX;x<=maxX;x++)
                    roles[new RmapSpecialWorldPoint(x,y)]=Sv5HubCellRole.PortNeck;
            }
            return Array.AsReadOnly(roles.OrderBy(v=>v.Key).Select(pair=>new Sv5HubCell(hubId,pair.Key,pair.Value,
                Sv5LoopTopology.ValueAt(baseline,pair.Key),pair.Value==Sv5HubCellRole.Solid?Sv5InfillCellValue.Solid:Sv5InfillCellValue.Air)).ToArray());
        }
        private static bool ConnectedPorts(IEnumerable<Sv5HubCell> cells,IEnumerable<Sv5HubPort> ports)
        {
            var passable=new HashSet<RmapSpecialWorldPoint>(cells.Where(v=>v.Role!=Sv5HubCellRole.Solid).Select(v=>v.World));
            var anchors=ports.Select(v=>v.Anchor).ToArray();if(anchors.Length==0||anchors.Any(v=>!passable.Contains(v)))return false;
            var visited=new HashSet<RmapSpecialWorldPoint>{anchors[0]};var queue=new Queue<RmapSpecialWorldPoint>();queue.Enqueue(anchors[0]);
            while(queue.Count!=0){var at=queue.Dequeue();foreach(var next in new[]{new RmapSpecialWorldPoint(at.X-1,at.Y),new RmapSpecialWorldPoint(at.X+1,at.Y),new RmapSpecialWorldPoint(at.X,at.Y-1),new RmapSpecialWorldPoint(at.X,at.Y+1)})if(passable.Contains(next)&&visited.Add(next))queue.Enqueue(next);}
            return anchors.All(visited.Contains);
        }
        private static double Ms(Stopwatch watch)=>watch.ElapsedTicks*1000.0/Stopwatch.Frequency;
    }
}
