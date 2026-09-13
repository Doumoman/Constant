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
        public int MaximumConnectionCenterlineCells => 120;
        public int MaximumLocalRouteAttempts => 4096;
        public int MaximumRouteAttemptsPerCandidate => 24;
        public int MaximumLocalSearchWidth => 160;
        public int MaximumLocalSearchHeight => 128;
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
        private readonly ReadOnlyCollection<RmapSpecialWorldPoint> movementWitness;
        private readonly ReadOnlyCollection<Sv5HubConnectionCell> cells;
        internal Sv5HubConnection(string id, string portId, string socketId, string externalRoomId,
            string externalSpaceGroupId, RmapSpecialWorldPoint portAnchor, RmapSpecialWorldPoint externalAnchor,
            Sv5HubConnectionRoute route)
        {
            Id=id; PortId=portId; SocketId=socketId; ExternalRoomId=externalRoomId;
            ExternalSpaceGroupId=externalSpaceGroupId; PortAnchor=portAnchor; ExternalAnchor=externalAnchor;
            if(route==null)throw new ArgumentNullException(nameof(route));
            centerline=Array.AsReadOnly(route.Centerline.ToArray());
            movementWitness=Array.AsReadOnly(route.MovementWitness.ToArray());
            cells=Array.AsReadOnly(route.Cells.Select(cell=>cell.Bind(id)).OrderBy(cell=>cell).ToArray());
            Direction=route.Direction;RouteVerified=route.RouteVerified;ProtectedOverlap=route.ProtectedOverlap;
            Type0Overlap=route.Type0Overlap;ProgressionBypass=route.ProgressionBypass;
            if(centerline.Count<2 || !centerline.First().Equals(portAnchor) || !centerline.Last().Equals(externalAnchor))
                throw new ArgumentException("A hub connection must reach its external anchor.");
        }
        public string Id { get; }
        public string PortId { get; }
        public string SocketId { get; }
        public string ExternalRoomId { get; }
        public string ExternalSpaceGroupId { get; }
        public RmapSpecialWorldPoint PortAnchor { get; }
        public RmapSpecialWorldPoint ExternalAnchor { get; }
        public IReadOnlyList<RmapSpecialWorldPoint> Centerline => centerline;
        public IReadOnlyList<RmapSpecialWorldPoint> MovementWitness => movementWitness;
        public IReadOnlyList<Sv5HubConnectionCell> Cells => cells;
        public string Direction { get; }
        public bool RouteVerified { get; }
        public bool ProtectedOverlap { get; }
        public bool Type0Overlap { get; }
        public bool ProgressionBypass { get; }
        public int ChangedCellCount => Cells.Count(cell=>cell.Changed);
        public string DigestToken => Id+"|"+PortId+"|"+ExternalRoomId+"|"+ExternalSpaceGroupId+"|"+
            PortAnchor+"|"+ExternalAnchor+"|"+Direction+"|"+RouteVerified+"|"+ProtectedOverlap+"|"+
            Type0Overlap+"|"+ProgressionBypass+"|"+string.Join(";",Centerline)+"|"+
            string.Join(";",Cells.Select(cell=>cell.ExportRole+":"+cell.Sequence+":"+cell.World+":"+
                cell.BeforeValue+">"+cell.FinalValue+":"+cell.Ownership));
        public int CompareTo(Sv5HubConnection other) => other == null ? 1 : string.Compare(Id,other.Id,StringComparison.Ordinal);
    }

    public sealed class Sv5HubConstraintSet : IComparable<Sv5HubConstraintSet>
    {
        private readonly ReadOnlyCollection<RmapSpecialWorldPoint> cells;
        internal Sv5HubConstraintSet(string category,IEnumerable<RmapSpecialWorldPoint> source)
        {Category=category;cells=Array.AsReadOnly(source.Distinct().OrderBy(value=>value).ToArray());}
        public string Category { get; }
        public IReadOnlyList<RmapSpecialWorldPoint> Cells => cells;
        public int CompareTo(Sv5HubConstraintSet other)=>other==null?1:string.Compare(Category,other.Category,StringComparison.Ordinal);
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
            int endpoints, int localCells, int routeAttempts, IEnumerable<KeyValuePair<string,int>> rejections)
        {
            TimingsMilliseconds=new ReadOnlyDictionary<string,double>((timings ?? Array.Empty<KeyValuePair<string,double>>())
                .OrderBy(v=>v.Key,StringComparer.Ordinal).ToDictionary(v=>v.Key,v=>v.Value,StringComparer.Ordinal));
            CandidateCount=candidates; IndexedEndpointCount=endpoints; LocalAdjacencyCells=localCells;
            LocalRouteAttempts=routeAttempts;RejectionHistogram=new ReadOnlyDictionary<string,int>((rejections??
                Array.Empty<KeyValuePair<string,int>>()).OrderBy(value=>value.Key,StringComparer.Ordinal)
                .ToDictionary(value=>value.Key,value=>value.Value,StringComparer.Ordinal));
        }
        public IReadOnlyDictionary<string,double> TimingsMilliseconds { get; }
        public int CandidateCount { get; }
        public int IndexedEndpointCount { get; }
        public int LocalAdjacencyCells { get; }
        public int LocalRouteAttempts { get; }
        public IReadOnlyDictionary<string,int> RejectionHistogram { get; }
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
            IEnumerable<Sv5HubConstraintSet> constraintSets, IEnumerable<string> diagnostics, Sv5HubPerformance performance)
        {
            BaselineDigest=baselineDigest; Profile=profile; Candidates=Array.AsReadOnly(candidates.OrderBy(v=>v).ToArray());
            HubId=hubId ?? string.Empty; Footprint=footprint; Cells=Array.AsReadOnly(cells.OrderBy(v=>v).ToArray());
            Sockets=Array.AsReadOnly(sockets.OrderBy(v=>v).ToArray()); Ports=Array.AsReadOnly(ports.OrderBy(v=>v).ToArray());
            Connections=Array.AsReadOnly(connections.OrderBy(v=>v).ToArray()); BaselineOccupancy=baselineOccupancy;
            FinalOccupancy=finalOccupancy;ConstraintSets=Array.AsReadOnly((constraintSets??Array.Empty<Sv5HubConstraintSet>())
                .OrderBy(value=>value).ToArray()); Diagnostics=Array.AsReadOnly((diagnostics ?? Array.Empty<string>())
                .Distinct(StringComparer.Ordinal).OrderBy(v=>v,StringComparer.Ordinal).ToArray()); Performance=performance;
            CellDigest=RmapWorldDefinition.Hash(string.Join("\n",Cells.Select(v=>v.DigestToken)
                .OrderBy(v=>v,StringComparer.Ordinal)));
            Digest=RmapWorldDefinition.Hash("SV5_HUB_SHELL_ACTUAL_CELL_V1\n"+BaselineDigest+"\n"+Profile.Digest+"\n"+
                HubId+"\n"+Footprint+"\n"+CellDigest+"\n"+string.Join("\n",Sockets.Select(v=>v.Id+"|"+v.Side+"|"+
                v.Tier+"|"+v.Anchor+"|"+v.ApertureHeight+"|"+v.Active))+"\n"+
                string.Join("\n",Connections.Select(v=>v.DigestToken))+"\n"+string.Join("\n",Diagnostics));
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
        public IReadOnlyList<Sv5HubConstraintSet> ConstraintSets { get; }
        public IReadOnlyList<string> Diagnostics { get; }
        public Sv5HubPerformance Performance { get; }
        public string CellDigest { get; }
        public string Digest { get; }
        public bool TreeGrabGeometryReady => false;
        public bool ComposedGeometryReady => false;
        public bool PlayerVerified => false;
        public bool Success => Diagnostics.Count==0 && HubId.Length!=0 && Sockets.Count==6 && Ports.Count>=4 && Ports.Count<=6 &&
            Connections.Count==Ports.Count && Connections.Select(v=>v.ExternalRoomId).Distinct(StringComparer.Ordinal).Count()==Connections.Count &&
            Connections.Select(v=>v.ExternalSpaceGroupId).Distinct(StringComparer.Ordinal).Count()==Connections.Count &&
            Connections.All(v=>v.RouteVerified&&!v.ProtectedOverlap&&!v.Type0Overlap&&!v.ProgressionBypass&&v.ChangedCellCount>0);
    }

    public static class Sv5HubShell
    {
        private sealed class SocketSpec
        {
            public Sv5HubSide Side; public Sv5HubTier Tier; public RmapSpecialWorldPoint Anchor; public int Height; public int ApertureStart;
        }
        private sealed class ExternalTarget : IComparable<ExternalTarget>
        {
            public string Id; public string RoomId; public string SpaceGroupId; public string Host;
            public RmapSpecialWorldPoint World; public int ComponentId;
            public int CompareTo(ExternalTarget other)
            {
                if(other==null)return 1;int world=World.CompareTo(other.World);return world!=0?world:
                    string.Compare(Id,other.Id,StringComparison.Ordinal);
            }
        }
        private sealed class Assignment
        { public SocketSpec Socket; public ExternalTarget Endpoint; public Sv5HubConnectionRoute Route; }
        private sealed class Attempt
        { public Sv5HubCandidate Candidate; public IReadOnlyList<Assignment> Assignments; }
        private sealed class EvaluationBatch
        { public Attempt[] Attempts; public int RouteAttempts; public SortedDictionary<string,int> Rejections; }
        private sealed class ConstraintIndex
        {
            public HashSet<RmapSpecialWorldPoint> ProtectedBody;
            public HashSet<RmapSpecialWorldPoint> Type0;
            public HashSet<RmapSpecialWorldPoint> Progression;
            public HashSet<RmapSpecialWorldPoint> EndpointForbidden;
            public HashSet<RmapSpecialWorldPoint> FootprintBlocked;
            public Sv5HubConstraintSet[] Sources;
        }

        public static Sv5HubShellPlan Build(Sv5SpaceGraphPlan plan, Sv5HubProfile profile = null)
        {
            if(plan==null || plan.Sidepaths==null || !plan.Sidepaths.Success)
                throw new ArgumentException("A passing PlanWithSidepaths result is required.",nameof(plan));
            profile=profile ?? new Sv5HubProfile();
            var total=Stopwatch.StartNew(); var timings=new SortedDictionary<string,double>(StringComparer.Ordinal);
            T Time<T>(string key,Func<T> action){var watch=Stopwatch.StartNew();try{return action();}finally{watch.Stop();timings[key]=Ms(watch);}}
            var baseline=plan.Sidepaths.FinalOccupancy;
            var constraints=Time("source_index_ms",()=>BuildConstraints(plan));
            var closedComponents=Time("progression_component_index_ms",()=>ClosedComponents(plan));
            var endpoints=Time("external_target_index_ms",()=>Targets(plan,constraints,closedComponents));
            var evaluation=Time("candidate_evaluation_ms",()=>Enumerate(plan,profile,constraints,endpoints,closedComponents));
            var attempts=evaluation.Attempts;
            var eligible=attempts.Where(v=>v.Assignments.Count>=profile.MinimumConnections)
                .OrderByDescending(v=>v.Assignments.Count).ThenBy(v=>v.Candidate.PathCells)
                .ThenBy(v=>v.Candidate.Id,StringComparer.Ordinal).ToArray();
            var diagnostics=new List<string>();
            if(eligible.Length==0) diagnostics.Add("HUB_NO_CANDIDATE_WITH_FOUR_DISTINCT_CONNECTIONS|MAX="+
                attempts.Max(v=>v.Candidate.AvailableConnections).ToString(CultureInfo.InvariantCulture));
            if(eligible.Length==0)diagnostics.AddRange(attempts.OrderByDescending(value=>value.Assignments.Count)
                .ThenBy(value=>value.Candidate.Id,StringComparer.Ordinal).Take(5).Select(value=>"HUB_TOP|"+
                    value.Candidate.Bounds+"|"+string.Join(";",value.Assignments.Select(assignment=>
                        assignment.Socket.Side+":"+assignment.Socket.Tier+":"+assignment.Endpoint.SpaceGroupId+"@"+
                        assignment.Endpoint.World))));
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
                    Array.Empty<Sv5HubPort>(),Array.Empty<Sv5HubConnection>(),baseline,empty,constraints.Sources,
                    diagnostics,new Sv5HubPerformance(timings,attempts.Length,endpoints.Length,0,
                        evaluation.RouteAttempts,evaluation.Rejections));
            }
            string hubId="SV5_HUB_"+RmapWorldDefinition.Hash("SV5_HUB|"+plan.Seed+"|"+chosen.Candidate.Bounds+"|"+
                string.Join(";",chosen.Assignments.Select(v=>v.Endpoint.Id))).Substring(0,20);
            var assignments=chosen.Assignments.Take(profile.MaximumConnections).ToArray();
            var externalAnchors=new HashSet<RmapSpecialWorldPoint>(assignments.Select(value=>value.Endpoint.World));
            var sourceSets=constraints.Sources.Select(source=>source.Category=="RESERVATION"?
                new Sv5HubConstraintSet(source.Category,source.Cells.Where(cell=>!externalAnchors.Contains(cell))):source).ToArray();
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
                    socket.Anchor,assignment.Endpoint.World,assignment.Route));
            }
            var cells=Time("accepted_overlay_ms",()=>BuildCells(hubId,chosen.Candidate.Bounds,sockets,baseline));
            var final=new Dictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell>(baseline);
            foreach(var cell in cells) final[cell.World]=new Sv5LoopOccupancyCell(cell.World,cell.FinalValue,"HUB_SHELL_ACTUAL",cell.MicroPatternOwner);
            foreach(var cell in connections.SelectMany(connection=>connection.Cells).Where(cell=>cell.Changed))
                final[cell.World]=new Sv5LoopOccupancyCell(cell.World,cell.FinalValue,"HUB_CONNECTION_ACTUAL","HUB_CONNECTION_ACTUAL");
            if(!ConnectedPorts(cells,ports))diagnostics.Add("HUB_PORT_COMPONENT_SPLIT");
            if(cells.Any(v=>constraints.FootprintBlocked.Contains(v.World)))diagnostics.Add("HUB_PROTECTED_OVERLAP");
            if(connections.Any(v=>!v.RouteVerified||v.ProtectedOverlap||v.Type0Overlap||v.ProgressionBypass))
                diagnostics.Add("HUB_CONNECTION_GEOMETRY_INVALID");
            if(connections.SelectMany(v=>v.Cells).All(v=>!v.Changed))diagnostics.Add("HUB_CONNECTION_NO_ACTUAL_CHANGE");
            if(!plan.PhysicalProduct.Success || plan.ProjectionProofs.Count!=6)diagnostics.Add("HUB_BASELINE_PRODUCT_INVALID");
            total.Stop();timings["total_build_ms"]=Ms(total);
            return new Sv5HubShellPlan(plan.Digest,profile,attempts.Select(v=>v.Candidate),hubId,chosen.Candidate.Bounds,
                cells,sockets,ports,connections,baseline,new ReadOnlyDictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell>(final),
                sourceSets,diagnostics,new Sv5HubPerformance(timings,attempts.Length,endpoints.Length,
                    cells.Count+connections.SelectMany(v=>v.Cells).Select(v=>v.World).Distinct().Count(),
                    evaluation.RouteAttempts,evaluation.Rejections));
        }

        private static EvaluationBatch Enumerate(Sv5SpaceGraphPlan plan,Sv5HubProfile profile,
            ConstraintIndex constraints,ExternalTarget[] endpoints,
            IReadOnlyDictionary<RmapSpecialWorldPoint,int> closedComponents)
        {
            var output=new List<Attempt>();int routeAttempts=0;
            var rejections=new SortedDictionary<string,int>(StringComparer.Ordinal);
            void Reject(string reason){rejections[reason]=rejections.TryGetValue(reason,out int count)?count+1:1;}
            foreach(var bounds in CandidateBounds(profile,endpoints))
            {
                string id="HUB_CANDIDATE_"+RmapWorldDefinition.Hash(plan.Seed+"|"+bounds).Substring(0,16);
                if(FootprintBlocked(bounds,constraints.FootprintBlocked))
                {Reject("FOOTPRINT_CONFLICT");output.Add(new Attempt{Candidate=new Sv5HubCandidate(id,bounds,0,0,"REJECTED","FOOTPRINT_CONFLICT"),Assignments=Array.Empty<Assignment>()});continue;}
                var assignments=new List<Assignment>();string lastReason="CONNECTION_SHORTFALL";
                int candidateRouteAttempts=0;
                var socketOptions=Specs(bounds).Select(spec=>new
                    {Spec=spec,Endpoints=Options(spec,bounds,endpoints).ToArray()})
                    .OrderBy(v=>v.Endpoints.Length).ThenBy(v=>v.Spec.Side).ThenBy(v=>v.Spec.Tier).ToArray();
                int potentialGroups=socketOptions.SelectMany(value=>value.Endpoints).Select(value=>value.SpaceGroupId)
                    .Distinct(StringComparer.Ordinal).Count();
                if(potentialGroups<profile.MinimumConnections)
                {Reject("TARGET_SHORTFALL");output.Add(new Attempt{Candidate=new Sv5HubCandidate(id,bounds,potentialGroups,0,
                    "REJECTED","TARGET_SHORTFALL"),Assignments=Array.Empty<Assignment>()});continue;}
                void Search(int optionIndex,List<Assignment> current,HashSet<string> rooms,HashSet<string> groups,
                    HashSet<RmapSpecialWorldPoint> reserved,int? component)
                {
                    if(current.Count>assignments.Count)assignments=current.ToList();
                    if(assignments.Count>=profile.TargetConnections||optionIndex>=socketOptions.Length||
                        routeAttempts>=profile.MaximumLocalRouteAttempts||
                        candidateRouteAttempts>=profile.MaximumRouteAttemptsPerCandidate)return;
                    if(current.Count+socketOptions.Length-optionIndex<=assignments.Count)return;
                    var option=socketOptions[optionIndex];
                    var spec=option.Spec;
                    foreach(var endpoint in option.Endpoints)
                    {
                        if(rooms.Contains(endpoint.RoomId)||groups.Contains(endpoint.SpaceGroupId))continue;
                        if(!closedComponents.TryGetValue(endpoint.World,out int endpointComponent))
                        {lastReason="EXTERNAL_ANCHOR_OUTSIDE_MOVEMENT";Reject(lastReason);continue;}
                        if(component.HasValue&&component.Value!=endpointComponent)
                        {lastReason="PROGRESSION_COMPONENT_BRIDGE";Reject(lastReason);continue;}
                        if(routeAttempts>=profile.MaximumLocalRouteAttempts)
                        {lastReason="ROUTE_ATTEMPT_CAP";Reject(lastReason);break;}
                        if(candidateRouteAttempts>=profile.MaximumRouteAttemptsPerCandidate)
                        {lastReason="CANDIDATE_ROUTE_ATTEMPT_CAP";Reject(lastReason);break;}
                        routeAttempts++;candidateRouteAttempts++;
                        if(!Sv5HubConnectionRouter.TryRoute(spec.Anchor,endpoint.World,bounds,plan.Sidepaths.FinalOccupancy,
                            constraints.ProtectedBody,constraints.Type0,constraints.Progression,constraints.EndpointForbidden,
                            reserved,out Sv5HubConnectionRoute route,out string reason))
                        {lastReason=reason;Reject(reason);continue;}
                        var selected=new Assignment{Socket=spec,Endpoint=endpoint,Route=route};
                        var owned=selected.Route.Cells.Where(cell=>cell.Changed).Select(cell=>cell.World).ToArray();
                        current.Add(selected);rooms.Add(endpoint.RoomId);groups.Add(endpoint.SpaceGroupId);reserved.UnionWith(owned);
                        Search(optionIndex+1,current,rooms,groups,reserved,component??endpointComponent);
                        reserved.ExceptWith(owned);groups.Remove(endpoint.SpaceGroupId);rooms.Remove(endpoint.RoomId);
                        current.RemoveAt(current.Count-1);
                        if(assignments.Count>=profile.TargetConnections||routeAttempts>=profile.MaximumLocalRouteAttempts||
                            candidateRouteAttempts>=profile.MaximumRouteAttemptsPerCandidate)break;
                    }
                    Search(optionIndex+1,current,rooms,groups,reserved,component);
                }
                Search(0,new List<Assignment>(),new HashSet<string>(StringComparer.Ordinal),
                    new HashSet<string>(StringComparer.Ordinal),new HashSet<RmapSpecialWorldPoint>(),null);
                assignments=assignments.OrderBy(v=>v.Socket.Side).ThenBy(v=>v.Socket.Tier).Take(profile.TargetConnections).ToList();
                int pathCells=assignments.SelectMany(v=>v.Route.Centerline).Distinct().Count();
                string status=assignments.Count>=profile.MinimumConnections?"ELIGIBLE":"REJECTED";
                string reasonText=status=="ELIGIBLE"?string.Empty:lastReason;
                if(status=="REJECTED")Reject(reasonText);
                output.Add(new Attempt{Candidate=new Sv5HubCandidate(id,bounds,assignments.Count,pathCells,status,reasonText),Assignments=assignments});
            }
            return new EvaluationBatch{Attempts=output.ToArray(),RouteAttempts=routeAttempts,Rejections=rejections};
        }

        private static IEnumerable<Sv5SpaceBounds> CandidateBounds(Sv5HubProfile profile,ExternalTarget[] targets)
        {
            int centerX=Sv5SpaceGraphPlanner.WorldWidth/2;
            int centerY=Sv5SpaceGraphPlanner.WorldHeight/2;
            return Enumerable.Range(1,(416-profile.FootprintHeight-16)/8+1)
                .SelectMany(yIndex=>Enumerable.Range(3,(624-profile.FootprintWidth-48)/8+1)
                    .Select(xIndex=>new Sv5SpaceBounds(xIndex*8,yIndex*8,profile.FootprintWidth,profile.FootprintHeight)))
                .Select(bounds=>new{Bounds=bounds,TargetGroups=Specs(bounds).SelectMany(spec=>Options(spec,bounds,targets))
                    .Select(target=>target.SpaceGroupId).Distinct(StringComparer.Ordinal).Count()})
                .OrderByDescending(value=>value.TargetGroups)
                .ThenBy(value=>Math.Abs(value.Bounds.X+value.Bounds.Width/2-centerX)+
                    Math.Abs(value.Bounds.Y+value.Bounds.Height/2-centerY))
                .ThenBy(value=>value.Bounds.Y).ThenBy(value=>value.Bounds.X).Select(value=>value.Bounds);
        }

        private static IEnumerable<ExternalTarget> Options(SocketSpec spec,Sv5SpaceBounds bounds,
            IEnumerable<ExternalTarget> endpoints)
        {
            int Score(ExternalTarget endpoint)=>Math.Abs(endpoint.World.Y-spec.Anchor.Y)*4+
                Math.Abs(endpoint.World.X-spec.Anchor.X);
            return endpoints.Where(endpoint=>!bounds.Contains(endpoint.World))
                .Where(endpoint=>Math.Abs(endpoint.World.X-spec.Anchor.X)+Math.Abs(endpoint.World.Y-spec.Anchor.Y)+1<=
                    Sv5HubConnectionRouter.MaximumCenterlineCells)
                .GroupBy(endpoint=>endpoint.SpaceGroupId,StringComparer.Ordinal)
                .SelectMany(group=>group.OrderBy(Score).ThenBy(endpoint=>endpoint.RoomId,StringComparer.Ordinal)
                    .ThenBy(endpoint=>endpoint.Id,StringComparer.Ordinal).Take(4))
                .OrderBy(Score).ThenBy(endpoint=>endpoint.RoomId,StringComparer.Ordinal)
                .ThenBy(endpoint=>endpoint.Id,StringComparer.Ordinal);
        }

        private static ExternalTarget[] Targets(Sv5SpaceGraphPlan plan,ConstraintIndex constraints,
            IReadOnlyDictionary<RmapSpecialWorldPoint,int> closedComponents)
        {
            var occupancy=plan.Sidepaths.FinalOccupancy;
            bool Supported(RmapSpecialWorldPoint point)=>point.Y>0&&point.Y+1<Sv5SpaceGraphPlanner.WorldHeight&&
                Sv5LoopTopology.ValueAt(occupancy,point)==Sv5InfillCellValue.Air&&
                Sv5LoopTopology.ValueAt(occupancy,new RmapSpecialWorldPoint(point.X,point.Y+1))==Sv5InfillCellValue.Air&&
                Sv5LoopTopology.ValueAt(occupancy,new RmapSpecialWorldPoint(point.X,point.Y-1))==Sv5InfillCellValue.Solid;
            bool Ingress(RmapSpecialWorldPoint end,RmapSpecialWorldPoint point)
            {
                if(point.X<0||point.X>=Sv5SpaceGraphPlanner.WorldWidth||point.Y<0||
                    point.Y>=Sv5SpaceGraphPlanner.WorldHeight-1)return false;
                if(constraints.ProtectedBody.Contains(point)||constraints.Type0.Contains(point)||
                    constraints.Progression.Contains(point))return false;
                var head=new RmapSpecialWorldPoint(point.X,point.Y+1);if(head.Equals(end))return true;
                return !constraints.ProtectedBody.Contains(head)&&!constraints.Type0.Contains(head)&&
                    !constraints.Progression.Contains(head);
            }
            bool Valid(ExternalTarget target)=>Supported(target.World)&&
                !constraints.EndpointForbidden.Contains(target.World)&&Cardinal(target.World).Any(point=>Ingress(target.World,point));
            var indexed=plan.Sidepaths.EndpointIndex.Endpoints.Where(endpoint=>closedComponents.ContainsKey(endpoint.World))
                .Select(endpoint=>new ExternalTarget{
                Id=endpoint.EndpointId,RoomId=endpoint.RoomId,SpaceGroupId=endpoint.SpaceGroupId,
                Host=endpoint.ConnectionId,World=endpoint.World,ComponentId=closedComponents[endpoint.World]});
            var deadEnds=plan.Sidepaths.Links.Where(link=>link.Kind==Sv5SidepathKind.DeadEnd&&
                closedComponents.ContainsKey(link.Centerline.Last())).Select(link=>new ExternalTarget{
                Id=link.Id+"_TIP",RoomId=link.FromRoomId,SpaceGroupId=link.SpaceGroupId,Host=link.Host,
                World=link.Centerline.Last(),ComponentId=closedComponents[link.Centerline.Last()]});
            var groupsByComponent=indexed.GroupBy(target=>target.ComponentId).ToDictionary(group=>group.Key,group=>
                group.GroupBy(target=>target.SpaceGroupId,StringComparer.Ordinal).Select(spaceGroup=>spaceGroup
                    .OrderBy(target=>target.RoomId,StringComparer.Ordinal).ThenBy(target=>target.Id,StringComparer.Ordinal).First()).ToArray());
            var physical=Sv5SpaceLoops.ApplyPhysicalCells(plan.Connections,plan.Loops);
            physical=Sv5SpaceSidepaths.ApplyPhysicalCells(physical,plan.Sidepaths);
            var passages=physical.SelectMany(connection=>connection.Centerline.Concat(connection.ApertureCells)).Distinct()
                .Where(world=>closedComponents.ContainsKey(world)&&groupsByComponent.ContainsKey(closedComponents[world]))
                .SelectMany(world=>groupsByComponent[closedComponents[world]].Select(group=>new ExternalTarget{
                    Id="COMPONENT_"+group.ComponentId.ToString(CultureInfo.InvariantCulture)+"_"+group.SpaceGroupId+"_"+
                        world.X.ToString(CultureInfo.InvariantCulture)+"_"+world.Y.ToString(CultureInfo.InvariantCulture),
                    RoomId=group.RoomId,SpaceGroupId=group.SpaceGroupId,Host=group.Host,World=world,
                    ComponentId=group.ComponentId}));
            return indexed.Concat(deadEnds).Concat(passages).Where(Valid).GroupBy(value=>value.Id,StringComparer.Ordinal)
                .Select(group=>group.First()).GroupBy(value=>value.SpaceGroupId,StringComparer.Ordinal)
                .SelectMany(group=>group.OrderBy(value=>value.World).ThenBy(value=>value.Id,StringComparer.Ordinal).Take(64))
                .OrderBy(value=>value).ToArray();
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
        private static ConstraintIndex BuildConstraints(Sv5SpaceGraphPlan plan)
        {
            var protectedCells=new HashSet<RmapSpecialWorldPoint>(Sv5SpaceSidepaths.ProtectedCells(plan));
            var type0=new HashSet<RmapSpecialWorldPoint>(plan.Core.RouteSource.Secrets.SelectMany(secret=>secret.Chunks)
                .SelectMany(chunk=>Enumerable.Range(0,8).SelectMany(y=>Enumerable.Range(0,12)
                    .Select(x=>new RmapSpecialWorldPoint(chunk.X*12+x,chunk.Y*8+y)))));
            var progression=new HashSet<RmapSpecialWorldPoint>(plan.Gates.SelectMany(gate=>gate.BlockingCells)
                .Concat(plan.Gates.SelectMany(gate=>gate.BlockingFaces).SelectMany(face=>new[]{face.First,face.Second})));
            var core=new HashSet<RmapSpecialWorldPoint>(plan.Core.CoreCells.Select(cell=>cell.World)
                .Concat(plan.Core.RouteCells.Select(cell=>cell.World)));
            var infill=new HashSet<RmapSpecialWorldPoint>(plan.Infill.Cells.Select(cell=>cell.World));
            var loops=new HashSet<RmapSpecialWorldPoint>(plan.Loops.Cells.Select(cell=>cell.World));
            var sidepaths=new HashSet<RmapSpecialWorldPoint>(plan.Sidepaths.Cells.Select(cell=>cell.World));
            var reservations=new HashSet<RmapSpecialWorldPoint>(plan.Reservations
                .Where(cell=>cell.Kind==Sv5SpaceReservationKind.CoreProtected||
                    cell.Kind==Sv5SpaceReservationKind.CoreRoute||
                    cell.Kind==Sv5SpaceReservationKind.ConditionalGate).Select(cell=>cell.World));
            reservations.ExceptWith(infill);reservations.ExceptWith(loops);reservations.ExceptWith(sidepaths);
            var protectedBody=new HashSet<RmapSpecialWorldPoint>(protectedCells);protectedBody.UnionWith(core);
            protectedBody.UnionWith(reservations);protectedBody.UnionWith(infill);protectedBody.UnionWith(loops);protectedBody.UnionWith(sidepaths);
            var endpointForbidden=new HashSet<RmapSpecialWorldPoint>(protectedCells);endpointForbidden.UnionWith(core);
            endpointForbidden.UnionWith(type0);endpointForbidden.UnionWith(progression);
            var footprint=new HashSet<RmapSpecialWorldPoint>(protectedBody);footprint.UnionWith(type0);footprint.UnionWith(progression);
            return new ConstraintIndex{ProtectedBody=protectedBody,Type0=type0,Progression=progression,
                EndpointForbidden=endpointForbidden,FootprintBlocked=footprint,Sources=new[]{
                    new Sv5HubConstraintSet("PROTECTED",protectedCells),new Sv5HubConstraintSet("TYPE0",type0),
                    new Sv5HubConstraintSet("PROGRESSION_GATE",progression),new Sv5HubConstraintSet("CORE",core),
                    new Sv5HubConstraintSet("RESERVATION",reservations),new Sv5HubConstraintSet("INFILL",infill),
                    new Sv5HubConstraintSet("LOOP",loops),new Sv5HubConstraintSet("SIDEPATH",sidepaths)}};
        }

        private static IReadOnlyDictionary<RmapSpecialWorldPoint,int> ClosedComponents(Sv5SpaceGraphPlan plan)
        {
            var physical=Sv5SpaceLoops.ApplyPhysicalCells(plan.Connections,plan.Loops);
            physical=Sv5SpaceSidepaths.ApplyPhysicalCells(physical,plan.Sidepaths);
            var passage=new HashSet<RmapSpecialWorldPoint>(physical.SelectMany(connection=>connection.Centerline.Concat(connection.ApertureCells)));
            var blockedCells=new HashSet<RmapSpecialWorldPoint>(plan.Gates.SelectMany(gate=>gate.BlockingCells));
            var blockedFaces=new HashSet<string>(plan.Gates.SelectMany(gate=>gate.BlockingFaces).Select(face=>Face(face.First,face.Second)),StringComparer.Ordinal);
            passage.ExceptWith(blockedCells);var result=new Dictionary<RmapSpecialWorldPoint,int>();int component=0;
            foreach(var start in passage.OrderBy(value=>value))
            {
                if(result.ContainsKey(start))continue;var queue=new Queue<RmapSpecialWorldPoint>();queue.Enqueue(start);result[start]=component;
                while(queue.Count!=0){var at=queue.Dequeue();foreach(var next in Cardinal(at).Where(passage.Contains).OrderBy(value=>value))
                    if(!blockedFaces.Contains(Face(at,next))&&!result.ContainsKey(next)){result[next]=component;queue.Enqueue(next);}}
                component++;
            }
            return new ReadOnlyDictionary<RmapSpecialWorldPoint,int>(result);
        }

        public static IReadOnlyList<Sv5SpaceConnection> ApplyPhysicalCells(IEnumerable<Sv5SpaceConnection> source,Sv5HubShellPlan hub)
        {
            var connections=(source??Array.Empty<Sv5SpaceConnection>()).ToArray();if(hub==null)return Array.AsReadOnly(connections);
            var hubAir=hub.Cells.Where(cell=>cell.FinalValue==Sv5InfillCellValue.Air).Select(cell=>cell.World)
                .Concat(hub.Connections.SelectMany(connection=>connection.Cells.Where(cell=>cell.FinalValue==Sv5InfillCellValue.Air)
                    .Select(cell=>cell.World))).Distinct().ToArray();
            return Array.AsReadOnly(connections.Select(connection=>
            {
                var attached=hub.Connections.Where(value=>value.ExternalSpaceGroupId==connection.Id).ToArray();
                if(attached.Length==0)return connection;
                var air=hubAir.Concat(attached.SelectMany(value=>value.Centerline)).Distinct().ToArray();
                return new Sv5SpaceConnection(connection.Id,connection.Kind,connection.FromPortId,connection.ToPortId,
                    connection.FromPlaceId,connection.ToPlaceId,connection.Direction,connection.Flow,connection.Condition,
                    connection.SourceGraphEdgeId,connection.SelectionState,connection.Centerline,connection.Envelope.Concat(air),
                    connection.ApertureCells.Concat(air));
            }).ToArray());
        }

        private static string Face(RmapSpecialWorldPoint first,RmapSpecialWorldPoint second)=>first.CompareTo(second)<=0?first+">"+second:second+">"+first;
        private static IEnumerable<RmapSpecialWorldPoint> Cardinal(RmapSpecialWorldPoint point)
        {yield return new RmapSpecialWorldPoint(point.X-1,point.Y);yield return new RmapSpecialWorldPoint(point.X+1,point.Y);
            yield return new RmapSpecialWorldPoint(point.X,point.Y-1);yield return new RmapSpecialWorldPoint(point.X,point.Y+1);}
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
