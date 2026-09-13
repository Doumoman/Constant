using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum Sv5HubConnectionCellRole { Centerline = 1, HeadClearance = 2, Support = 3 }
    public enum Sv5PlatformerMoveType { Walk = 1, StepUp = 2, StepDown = 3, Jump = 4, Drop = 5, JumpGrab = 6 }

    public sealed class Sv5MovementEdge : IComparable<Sv5MovementEdge>
    {
        public Sv5MovementEdge(int sequence, RmapSpecialWorldPoint from, RmapSpecialWorldPoint to,
            Sv5PlatformerMoveType moveType, bool bodyClear, bool headClear,
            RmapSpecialWorldPoint? contact = null, string traversalState = null)
        {
            Sequence = sequence; From = from; To = to; MoveType = moveType;
            BodyClear = bodyClear; HeadClear = headClear; Contact = contact;
            TraversalState = traversalState ?? string.Empty;
        }
        public int Sequence { get; }
        public RmapSpecialWorldPoint From { get; }
        public RmapSpecialWorldPoint To { get; }
        public Sv5PlatformerMoveType MoveType { get; }
        public bool BodyClear { get; }
        public bool HeadClear { get; }
        public RmapSpecialWorldPoint? Contact { get; }
        public string TraversalState { get; }
        public string ExportMoveType => MoveType == Sv5PlatformerMoveType.StepUp ? "STEP_UP" :
            MoveType == Sv5PlatformerMoveType.StepDown ? "STEP_DOWN" :
            MoveType == Sv5PlatformerMoveType.JumpGrab ? "JUMP_GRAB" : MoveType.ToString().ToUpperInvariant();
        public string DigestToken => Sequence+"|"+From+">"+To+"|"+ExportMoveType+"|"+
            BodyClear+"|"+HeadClear+"|"+(Contact.HasValue?Contact.Value.ToString():string.Empty)+"|"+
            TraversalState;
        public int CompareTo(Sv5MovementEdge other) => other == null ? 1 : Sequence.CompareTo(other.Sequence);
    }

    public sealed class Sv5HubConnectionCell : IComparable<Sv5HubConnectionCell>
    {
        internal Sv5HubConnectionCell(string connectionId, Sv5HubConnectionCellRole role, int sequence,
            RmapSpecialWorldPoint world, Sv5InfillCellValue beforeValue, Sv5InfillCellValue finalValue,
            Sv5InfillCellValue headValue, Sv5InfillCellValue supportValue)
        {
            ConnectionId = connectionId ?? string.Empty; Role = role; Sequence = sequence; World = world;
            BeforeValue = beforeValue; FinalValue = finalValue; HeadValue = headValue; SupportValue = supportValue;
            Changed = beforeValue != finalValue; Ownership = Changed ? "HUB_CONNECTION_ACTUAL" : string.Empty;
        }

        public string ConnectionId { get; }
        public Sv5HubConnectionCellRole Role { get; }
        public int Sequence { get; }
        public RmapSpecialWorldPoint World { get; }
        public Sv5InfillCellValue BeforeValue { get; }
        public Sv5InfillCellValue FinalValue { get; }
        public Sv5InfillCellValue HeadValue { get; }
        public Sv5InfillCellValue SupportValue { get; }
        public bool Changed { get; }
        public string Ownership { get; }
        public string ExportRole => Role == Sv5HubConnectionCellRole.Centerline ? "CENTERLINE" :
            Role == Sv5HubConnectionCellRole.HeadClearance ? "HEAD_CLEARANCE" : "SUPPORT";
        internal Sv5HubConnectionCell Bind(string connectionId) => new Sv5HubConnectionCell(connectionId, Role,
            Sequence, World, BeforeValue, FinalValue, HeadValue, SupportValue);
        public int CompareTo(Sv5HubConnectionCell other)
        {
            if (other == null) return 1;
            int connection = string.Compare(ConnectionId, other.ConnectionId, StringComparison.Ordinal);
            if (connection != 0) return connection;
            int role = Role.CompareTo(other.Role); if (role != 0) return role;
            int sequence = Sequence.CompareTo(other.Sequence); return sequence != 0 ? sequence : World.CompareTo(other.World);
        }
    }

    public sealed class Sv5HubConnectionRoute
    {
        internal Sv5HubConnectionRoute(IEnumerable<RmapSpecialWorldPoint> centerline,
            IEnumerable<Sv5HubConnectionCell> cells, IEnumerable<Sv5MovementEdge> witness,
            string direction, bool verified, bool protectedOverlap, bool type0Overlap, bool progressionBypass)
        {
            Centerline = new ReadOnlyCollection<RmapSpecialWorldPoint>(centerline.ToArray());
            Cells = new ReadOnlyCollection<Sv5HubConnectionCell>(cells.ToArray());
            MovementWitness = new ReadOnlyCollection<Sv5MovementEdge>(witness.ToArray());
            Direction = direction; RouteVerified = verified; ProtectedOverlap = protectedOverlap;
            Type0Overlap = type0Overlap; ProgressionBypass = progressionBypass;
        }

        public IReadOnlyList<RmapSpecialWorldPoint> Centerline { get; }
        public IReadOnlyList<Sv5HubConnectionCell> Cells { get; }
        public IReadOnlyList<Sv5MovementEdge> MovementWitness { get; }
        public string Direction { get; }
        public bool RouteVerified { get; }
        public bool ProtectedOverlap { get; }
        public bool Type0Overlap { get; }
        public bool ProgressionBypass { get; }
        public int ChangedCells => Cells.Count(value => value.Changed);
    }

    /// <summary>Bounded deterministic routing over immutable occupancy plus sparse desired-cell overlays.</summary>
    public static class Sv5HubConnectionRouter
    {
        public const int MaximumCenterlineCells = 120;
        public const int MaximumSearchWidth = 160;
        public const int MaximumSearchHeight = 128;

        private sealed class Desired
        {
            public Sv5InfillCellValue Value;
            public Sv5HubConnectionCellRole Role;
            public int Sequence;
        }
        private sealed class MonotoneNode
        {
            public RmapSpecialWorldPoint Point;
            public int PreviousRise;
            public int Distance;
            public MonotoneNode Parent;
        }

        public static IReadOnlyList<RmapSpecialWorldPoint> LegacyVerticalThenHorizontal(
            RmapSpecialWorldPoint start, RmapSpecialWorldPoint end)
        {
            var result = new List<RmapSpecialWorldPoint> { start }; int x = start.X, y = start.Y;
            while (y != end.Y) { y += Math.Sign(end.Y - y); result.Add(new RmapSpecialWorldPoint(x, y)); }
            while (x != end.X) { x += Math.Sign(end.X - x); result.Add(new RmapSpecialWorldPoint(x, y)); }
            return Array.AsReadOnly(result.ToArray());
        }

        public static bool BodyIntersects(IEnumerable<RmapSpecialWorldPoint> centerline,
            ISet<RmapSpecialWorldPoint> cells)
        {
            var path = (centerline ?? Array.Empty<RmapSpecialWorldPoint>()).ToArray();
            return path.Skip(1).Take(Math.Max(0, path.Length - 2)).Any(cells.Contains);
        }

        public static bool TryRoute(RmapSpecialWorldPoint start, RmapSpecialWorldPoint end,
            Sv5SpaceBounds shellBounds, IReadOnlyDictionary<RmapSpecialWorldPoint, Sv5LoopOccupancyCell> baseline,
            ISet<RmapSpecialWorldPoint> protectedBody, ISet<RmapSpecialWorldPoint> type0,
            ISet<RmapSpecialWorldPoint> progression, ISet<RmapSpecialWorldPoint> endpointForbidden,
            ISet<RmapSpecialWorldPoint> reserved, out Sv5HubConnectionRoute route, out string reason)
        {
            route = null; reason = string.Empty;
            if (baseline == null || protectedBody == null || type0 == null || progression == null ||
                endpointForbidden == null || reserved == null) throw new ArgumentNullException(nameof(baseline));
            int direct = Math.Abs(start.X - end.X) + Math.Abs(start.Y - end.Y) + 1;
            if (direct > MaximumCenterlineCells) { reason = "LENGTH"; return false; }
            if (endpointForbidden.Contains(end) || Value(baseline, end) != Sv5InfillCellValue.Air ||
                Value(baseline, new RmapSpecialWorldPoint(end.X, end.Y + 1)) != Sv5InfillCellValue.Air)
            { reason = "EXTERNAL_ANCHOR_NOT_READ_ONLY_AIR"; return false; }

            int directMinX = Math.Min(start.X, end.X), directMaxX = Math.Max(start.X, end.X);
            int directMinY = Math.Min(start.Y, end.Y), directMaxY = Math.Max(start.Y, end.Y);
            int marginX = Math.Max(0, Math.Min(24, (MaximumSearchWidth - (directMaxX - directMinX + 1)) / 2));
            int marginY = Math.Max(0, Math.Min(24, (MaximumSearchHeight - (directMaxY - directMinY + 1)) / 2));
            int minX = Math.Max(0, directMinX - marginX);
            int maxX = Math.Min(Sv5SpaceGraphPlanner.WorldWidth - 1, directMaxX + marginX);
            int minY = Math.Max(0, directMinY - marginY);
            int maxY = Math.Min(Sv5SpaceGraphPlanner.WorldHeight - 2, directMaxY + marginY);
            if (maxX - minX + 1 > MaximumSearchWidth || maxY - minY + 1 > MaximumSearchHeight)
            { reason = "LOCAL_SEARCH_BOUNDS"; return false; }

            bool Forbidden(RmapSpecialWorldPoint point)=>protectedBody.Contains(point)||type0.Contains(point)||progression.Contains(point);
            var startSupport=new RmapSpecialWorldPoint(start.X,start.Y-1);
            var endSupport=new RmapSpecialWorldPoint(end.X,end.Y-1);
            bool CanUse(RmapSpecialWorldPoint point)
            {
                if(point.X<minX||point.X>maxX||point.Y<=0||point.Y>maxY||
                    point.Y>=Sv5SpaceGraphPlanner.WorldHeight-1)return false;
                if(point.Equals(start))return true;if(reserved.Contains(point))return false;if(point.Equals(end))return true;
                if(point.Equals(startSupport)||point.Equals(endSupport))return false;
                if(shellBounds.Contains(point)||Forbidden(point))return false;
                var head=new RmapSpecialWorldPoint(point.X,point.Y+1);
                return !reserved.Contains(head)&&!Forbidden(head)&&!shellBounds.Contains(head);
            }
            if(CardinalNeighbors(end).All(point=>!CanUse(point))){reason="EXTERNAL_ANCHOR_INGRESS_BLOCKED";return false;}
            var parent=new Dictionary<RmapSpecialWorldPoint,RmapSpecialWorldPoint>{{start,start}};
            var distance=new Dictionary<RmapSpecialWorldPoint,int>{{start,1}};
            var queue=new Queue<RmapSpecialWorldPoint>();queue.Enqueue(start);
            while(queue.Count>0&&!parent.ContainsKey(end))
            {
                var at=queue.Dequeue();if(distance[at]>=MaximumCenterlineCells)continue;
                foreach(var next in CardinalNeighbors(at).OrderBy(point=>Manhattan(point,end)).ThenBy(point=>point))
                {if(parent.ContainsKey(next)||!CanUse(next))continue;parent.Add(next,at);distance.Add(next,distance[at]+1);queue.Enqueue(next);}
            }
            if(!parent.ContainsKey(end)){reason="NO_ACTUAL_CELL_ROUTE";return false;}
            var path=new List<RmapSpecialWorldPoint>{end};
            while(!path[path.Count-1].Equals(start))path.Add(parent[path[path.Count-1]]);path.Reverse();
            if(path.Count>MaximumCenterlineCells||path.Distinct().Count()!=path.Count||
                path.Zip(path.Skip(1),(a,b)=>Manhattan(a,b)).Any(value=>value!=1))
            {reason="CENTERLINE_INVALID";return false;}
            var centerAir=new HashSet<RmapSpecialWorldPoint>(path.Concat(path.Take(path.Count-1).Select(point=>
                new RmapSpecialWorldPoint(point.X,point.Y+1))));
            bool SupportAllowed(RmapSpecialWorldPoint point)=>point.Y>=0&&
                (Value(baseline,point)==Sv5InfillCellValue.Solid||
                 (!shellBounds.Contains(point)&&!reserved.Contains(point)&&!Forbidden(point)));
            bool CanStand(RmapSpecialWorldPoint point)
            {
                if(point.X<minX||point.X>maxX||point.Y<=0||point.Y>maxY||point.Y>=Sv5SpaceGraphPlanner.WorldHeight-1)
                    return false;if(point.Equals(start))return true;if(reserved.Contains(point))return false;
                var head=new RmapSpecialWorldPoint(point.X,point.Y+1);
                var support=new RmapSpecialWorldPoint(point.X,point.Y-1);
                if(point.Equals(end))return Value(baseline,support)==Sv5InfillCellValue.Solid;
                return !shellBounds.Contains(point)&&!shellBounds.Contains(head)&&!Forbidden(point)&&!Forbidden(head)&&
                    !reserved.Contains(head)&&!reserved.Contains(support)&&!centerAir.Contains(support)&&SupportAllowed(support);
            }
            var movementParent=new Dictionary<RmapSpecialWorldPoint,RmapSpecialWorldPoint>{{start,start}};
            var movementDistance=new Dictionary<RmapSpecialWorldPoint,int>{{start,0}};
            var movementQueue=new Queue<RmapSpecialWorldPoint>();movementQueue.Enqueue(start);
            while(movementQueue.Count>0&&!movementParent.ContainsKey(end))
            {
                var at=movementQueue.Dequeue();if(movementDistance[at]>=MaximumCenterlineCells-1)continue;
                foreach(var next in WideMovementNeighbors(at).OrderBy(point=>Manhattan(point,end)).ThenBy(point=>point))
                {
                    if(movementParent.ContainsKey(next)||!CanStand(next))continue;
                    movementParent.Add(next,at);movementDistance.Add(next,movementDistance[at]+1);movementQueue.Enqueue(next);
                }
            }
            if(!movementParent.ContainsKey(end)){reason="NO_PLATFORMER_WITNESS";return false;}
            var states=new List<RmapSpecialWorldPoint>{end};
            while(!states[states.Count-1].Equals(start))states.Add(movementParent[states[states.Count-1]]);states.Reverse();
            var desired=new Dictionary<RmapSpecialWorldPoint,Desired>();
            bool AddDesired(RmapSpecialWorldPoint point,Sv5InfillCellValue value,Sv5HubConnectionCellRole role,int sequence)
            {
                if(desired.TryGetValue(point,out Desired present))return present.Value==value;
                desired.Add(point,new Desired{Value=value,Role=role,Sequence=sequence});return true;
            }
            bool valid=true;
            for(int index=0;index<path.Count;index++)valid&=AddDesired(path[index],Sv5InfillCellValue.Air,
                Sv5HubConnectionCellRole.Centerline,index);
            foreach(var point in path.Take(path.Count-1))valid&=AddDesired(new RmapSpecialWorldPoint(point.X,point.Y+1),
                    Sv5InfillCellValue.Air,Sv5HubConnectionCellRole.HeadClearance,-1);
            foreach(var state in states)
            {
                valid&=AddDesired(state,Sv5InfillCellValue.Air,Sv5HubConnectionCellRole.HeadClearance,-1);
                valid&=AddDesired(new RmapSpecialWorldPoint(state.X,state.Y+1),Sv5InfillCellValue.Air,
                        Sv5HubConnectionCellRole.HeadClearance,-1);
            }
            foreach(var state in states)
            {
                var support=new RmapSpecialWorldPoint(state.X,state.Y-1);
                if(!state.Equals(start)&&!state.Equals(end)&&!SupportAllowed(support))valid=false;
                valid&=AddDesired(support,Sv5InfillCellValue.Solid,Sv5HubConnectionCellRole.Support,-1);
            }
            if(!valid){reason="MOVEMENT_SUPPORT_CONFLICT";return false;}
            Sv5InfillCellValue Final(RmapSpecialWorldPoint point)=>desired.TryGetValue(point,out Desired value)?
                    value.Value:Value(baseline,point);
            var witness=new List<Sv5MovementEdge>();
            for(int index=1;index<states.Count;index++)
            {
                var from=states[index-1];var to=states[index];int dx=Math.Abs(to.X-from.X),dy=to.Y-from.Y;
                var kind=dx==1&&dy==0?Sv5PlatformerMoveType.Walk:dx==1&&dy==1?Sv5PlatformerMoveType.StepUp:
                    dx==1&&dy==-1?Sv5PlatformerMoveType.StepDown:Sv5PlatformerMoveType.Jump;
                witness.Add(new Sv5MovementEdge(index-1,from,to,kind,Final(to)==Sv5InfillCellValue.Air,
                        Final(new RmapSpecialWorldPoint(to.X,to.Y+1))==Sv5InfillCellValue.Air));
            }
            bool verified=states.All(point=>Final(point)==Sv5InfillCellValue.Air&&
                    Final(new RmapSpecialWorldPoint(point.X,point.Y+1))==Sv5InfillCellValue.Air&&
                    Final(new RmapSpecialWorldPoint(point.X,point.Y-1))==Sv5InfillCellValue.Solid)&&witness.Count>0;
            var actualAir=desired.Where(pair=>pair.Value.Value==Sv5InfillCellValue.Air).Select(pair=>pair.Key)
                    .Where(point=>!point.Equals(start)&&!point.Equals(end)).ToArray();
            bool protectedOverlap=actualAir.Any(protectedBody.Contains),type0Overlap=actualAir.Any(type0.Contains),
                    progressionBypass=actualAir.Any(progression.Contains);
            if(!verified||protectedOverlap||type0Overlap||progressionBypass)
            {reason="ROUTE_GEOMETRY_INVALID";return false;}
            var cells=desired.OrderBy(pair=>pair.Value.Role==Sv5HubConnectionCellRole.Centerline?0:1)
                    .ThenBy(pair=>pair.Value.Sequence).ThenBy(pair=>pair.Key).Select(pair=>new Sv5HubConnectionCell(
                        string.Empty,pair.Value.Role,pair.Value.Sequence,pair.Key,Value(baseline,pair.Key),pair.Value.Value,
                        Final(new RmapSpecialWorldPoint(pair.Key.X,pair.Key.Y+1)),
                        Final(new RmapSpecialWorldPoint(pair.Key.X,pair.Key.Y-1)))).ToArray();
            if(cells.Any(cell=>cell.Changed&&(protectedBody.Contains(cell.World)||type0.Contains(cell.World)||
                    progression.Contains(cell.World)))||cells.Single(cell=>cell.Role==Sv5HubConnectionCellRole.Centerline&&
                    cell.World.Equals(end)).Changed){reason="OWNERSHIP_OR_EXTERNAL_ANCHOR";return false;}
            route=new Sv5HubConnectionRoute(path,cells,witness,"HUB_TO_EXTERNAL",true,protectedOverlap,
                    type0Overlap,progressionBypass);return true;
        }

        private static int Manhattan(RmapSpecialWorldPoint first, RmapSpecialWorldPoint second) =>
            Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y);
        private static Sv5InfillCellValue Value(
            IReadOnlyDictionary<RmapSpecialWorldPoint, Sv5LoopOccupancyCell> occupancy,
            RmapSpecialWorldPoint point) => Sv5LoopTopology.ValueAt(occupancy, point);
        private static IEnumerable<RmapSpecialWorldPoint> MovementNeighbors(RmapSpecialWorldPoint point)
        {
            yield return new RmapSpecialWorldPoint(point.X - 1, point.Y);
            yield return new RmapSpecialWorldPoint(point.X + 1, point.Y);
            yield return new RmapSpecialWorldPoint(point.X - 1, point.Y - 1);
            yield return new RmapSpecialWorldPoint(point.X + 1, point.Y - 1);
            yield return new RmapSpecialWorldPoint(point.X - 1, point.Y + 1);
            yield return new RmapSpecialWorldPoint(point.X + 1, point.Y + 1);
        }
        private static IEnumerable<RmapSpecialWorldPoint> WideMovementNeighbors(RmapSpecialWorldPoint point)
        {
            foreach(int dx in new[]{-1,1,-2,2,-3,3})
            foreach(int dy in new[]{0,-1,1,-2,2})
            {
                if(Math.Abs(dx)==1&&Math.Abs(dy)<=1||Math.Abs(dx)>=2)
                    yield return new RmapSpecialWorldPoint(point.X+dx,point.Y+dy);
            }
        }
        private static IEnumerable<RmapSpecialWorldPoint> CardinalNeighbors(RmapSpecialWorldPoint point)
        {
            yield return new RmapSpecialWorldPoint(point.X-1,point.Y);
            yield return new RmapSpecialWorldPoint(point.X+1,point.Y);
            yield return new RmapSpecialWorldPoint(point.X,point.Y-1);
            yield return new RmapSpecialWorldPoint(point.X,point.Y+1);
        }
    }
}
