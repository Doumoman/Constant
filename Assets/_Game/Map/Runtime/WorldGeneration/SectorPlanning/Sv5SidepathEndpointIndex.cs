using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.WorldData;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public sealed class Sv5SidepathEndpoint : IComparable<Sv5SidepathEndpoint>
    {
        internal Sv5SidepathEndpoint(string id,string room,string group,string connection,string port,
            string patternOwner,Sv5SpecialWorldPoint world,int exitX,string stableHash)
        {
            EndpointId=id;RoomId=room;SpaceGroupId=group;ConnectionId=connection;PortId=port;
            MicroPatternOwner=patternOwner;World=world;ExitX=exitX;StableHash=stableHash;
        }
        public string EndpointId { get; }
        public string RoomId { get; }
        public string SpaceGroupId { get; }
        public string ConnectionId { get; }
        public string PortId { get; }
        public string MicroPatternOwner { get; }
        public Sv5SpecialWorldPoint World { get; }
        public int ExitX { get; }
        public string StableHash { get; }
        public int CompareTo(Sv5SidepathEndpoint other)
        {
            if(other==null) return 1; int x=World.X.CompareTo(other.World.X); if(x!=0) return x;
            int y=World.Y.CompareTo(other.World.Y); if(y!=0) return y;
            int room=string.Compare(RoomId,other.RoomId,StringComparison.Ordinal); return room!=0 ? room :
                string.Compare(EndpointId,other.EndpointId,StringComparison.Ordinal);
        }
    }

    public sealed class Sv5SidepathEndpointPair : IComparable<Sv5SidepathEndpointPair>
    {
        internal Sv5SidepathEndpointPair(Sv5SidepathEndpoint from,Sv5SidepathEndpoint to,string reason)
        { From=from;To=to;PairKey=Key(from.EndpointId,to.EndpointId);RejectionReason=reason ?? string.Empty; }
        public Sv5SidepathEndpoint From { get; }
        public Sv5SidepathEndpoint To { get; }
        public string PairKey { get; }
        public int Dx => Math.Abs(To.World.X-From.World.X);
        public int Dy => Math.Abs(To.World.Y-From.World.Y);
        public int Manhattan => Dx+Dy;
        public string RejectionReason { get; }
        public bool Eligible => RejectionReason.Length==0;
        public int CompareTo(Sv5SidepathEndpointPair other) => other==null ? 1 : string.Compare(PairKey,other.PairKey,StringComparison.Ordinal);
        public static string Key(string first,string second) => string.Compare(first,second,StringComparison.Ordinal)<=0 ?
            first+"|"+second : second+"|"+first;
    }

    public sealed class Sv5SidepathEndpointPlan
    {
        internal Sv5SidepathEndpointPlan(IEnumerable<Sv5SidepathEndpoint> endpoints,IEnumerable<Sv5SidepathEndpointPair> pairs)
        {
            Endpoints=Array.AsReadOnly(endpoints.OrderBy(v=>v).ToArray());
            Pairs=Array.AsReadOnly(pairs.OrderBy(v=>v).ToArray());
            Digest=Sv5WorldDefinition.Hash("SV5_SIDEPATH_ENDPOINT_WINDOW_V1\n"+
                string.Join("\n",Endpoints.Select(v=>v.EndpointId+"|"+v.RoomId+"|"+v.SpaceGroupId+"|"+
                    v.ConnectionId+"|"+v.PortId+"|"+v.MicroPatternOwner+"|"+v.World+"|"+v.ExitX))+"\n"+
                string.Join("\n",Pairs.Select(v=>v.PairKey+"|"+v.Dx+"|"+v.Dy+"|"+v.Manhattan+"|"+v.RejectionReason)));
        }
        public IReadOnlyList<Sv5SidepathEndpoint> Endpoints { get; }
        public IReadOnlyList<Sv5SidepathEndpointPair> Pairs { get; }
        public string Digest { get; }
    }

    public static class Sv5SidepathEndpointIndex
    {
        public static Sv5SidepathEndpointPlan Build(Sv5SpaceGraphPlan plan,
            IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell> occupancy,
            IReadOnlyCollection<Sv5SpecialWorldPoint> supportedFeet)
        {
            if(plan==null || plan.Infill==null) throw new ArgumentException("An infill plan is required.",nameof(plan));
            if(occupancy==null || supportedFeet==null) throw new ArgumentNullException(nameof(occupancy));
            var rooms=plan.Infill.Rooms.ToDictionary(v=>v.Id,v=>v,StringComparer.Ordinal);
            var cells=plan.Infill.Cells.GroupBy(v=>v.World).ToDictionary(v=>v.Key,v=>v.Last());
            var feet=new HashSet<Sv5SpecialWorldPoint>(supportedFeet);
            var connections=plan.Connections.ToDictionary(v=>v.Id,v=>v,StringComparer.Ordinal);
            var ports=plan.Ports.ToDictionary(v=>v.Id,v=>v,StringComparer.Ordinal);
            var endpoints=new List<Sv5SidepathEndpoint>();
            foreach(var point in feet.OrderBy(v=>v))
            {
                if(!cells.TryGetValue(point,out Sv5InfillCell cell) || cell.Value!=Sv5InfillCellValue.Air) continue;
                string roomId=ActualRoom(cell.Owner,rooms); if(roomId.Length==0) continue;
                var room=rooms[roomId]; if(!connections.TryGetValue(room.Host,out Sv5SpaceConnection connection)) continue;
                foreach(int exitX in new[]{-1,1})
                {
                    var outside=new Sv5SpecialWorldPoint(point.X+exitX,point.Y);
                    if(outside.X<1 || outside.X>622 || room.Bounds.Contains(outside) || feet.Contains(outside)) continue;
                    string port=NearestPort(connection,point,ports);
                    string endpointId="EP_"+Sv5WorldDefinition.Hash(connection.Id+"|"+port+"|"+roomId+"|"+point+"|"+exitX).Substring(0,24);
                    string stable=Sv5WorldDefinition.Hash("SV5_SIDE_ENDPOINT|"+plan.Seed+"|"+endpointId);
                    endpoints.Add(new Sv5SidepathEndpoint(endpointId,roomId,connection.Id,connection.Id,port,
                        cell.Owner+":"+cell.Recipe,point,exitX,stable));
                }
            }
            endpoints=endpoints.GroupBy(v=>v.RoomId,StringComparer.Ordinal)
                .SelectMany(v=>v.OrderBy(e=>e.StableHash,StringComparer.Ordinal).ThenBy(e=>e).Take(8))
                .OrderBy(v=>v).ToList();
            var direct=DirectRoomPairs(plan);
            var seen=new HashSet<string>(StringComparer.Ordinal); var pairs=new List<Sv5SidepathEndpointPair>();
            for(int i=0;i<endpoints.Count;i++)
            for(int j=i+1;j<endpoints.Count;j++)
            {
                var from=endpoints[i]; var to=endpoints[j];
                if(to.World.X-from.World.X>49) break;
                string key=Sv5SidepathEndpointPair.Key(from.EndpointId,to.EndpointId); if(!seen.Add(key)) continue;
                string reason=from.RoomId==to.RoomId ? "SAME_ROOM" :
                    Math.Abs(to.World.X-from.World.X)+Math.Abs(to.World.Y-from.World.Y)>49 ? "MANHATTAN_GT_49" :
                    direct.Contains(RoomKey(from.RoomId,to.RoomId)) ? "DIRECT_ROOM_PAIR" :
                    from.ExitX!=1 || to.ExitX!=-1 ? "ENDPOINTS_NOT_FACING" : string.Empty;
                pairs.Add(new Sv5SidepathEndpointPair(from,to,reason));
            }
            return new Sv5SidepathEndpointPlan(endpoints,pairs);
        }

        private static HashSet<string> DirectRoomPairs(Sv5SpaceGraphPlan plan)
        {
            var roomIds=new HashSet<string>(plan.Infill.Rooms.Select(v=>v.Id),StringComparer.Ordinal);
            return new HashSet<string>(plan.Infill.Links.Where(v=>roomIds.Contains(v.Room)&&roomIds.Contains(v.Parent))
                .Select(v=>RoomKey(v.Room,v.Parent)),StringComparer.Ordinal);
        }
        private static string RoomKey(string a,string b) => string.Compare(a,b,StringComparison.Ordinal)<=0 ? a+"|"+b : b+"|"+a;
        private static string NearestPort(Sv5SpaceConnection connection,Sv5SpecialWorldPoint point,
            IReadOnlyDictionary<string,Sv5SpacePort> ports)
        {
            var ids=new[]{connection.FromPortId,connection.ToPortId}.Where(ports.ContainsKey).ToArray();
            return ids.OrderBy(id=>Math.Abs(ports[id].Anchor.X-point.X)+Math.Abs(ports[id].Anchor.Y-point.Y))
                .ThenBy(id=>id,StringComparer.Ordinal).FirstOrDefault() ?? string.Empty;
        }
        private static string ActualRoom(string owner,IReadOnlyDictionary<string,Sv5InfillRoom> rooms)
        {
            if(string.IsNullOrEmpty(owner)) return string.Empty; if(rooms.ContainsKey(owner)) return owner;
            if(owner.EndsWith("_LINK",StringComparison.Ordinal))
            { string room=owner.Substring(0,owner.Length-5); if(rooms.ContainsKey(room)) return room; }
            return string.Empty;
        }
    }
}
