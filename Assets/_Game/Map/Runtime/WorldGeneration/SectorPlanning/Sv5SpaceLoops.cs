using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.WorldData;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum Sv5LoopKind { Loop = 1, RandomShortcut = 2 }
    public enum Sv5LoopCellRole { Air = 1, SolidSupport = 2, ApertureOverride = 3 }

    public sealed class Sv5LoopProfile
    {
        public Sv5LoopProfile(bool enabled = true, int eligibilityPercent = 30, int target = 32,
            int minimum = 16, int maximum = 48, int minimumSectors = 12, int maximumPerSector = 3)
        {
            if (eligibilityPercent < 0 || eligibilityPercent > 100 || minimum < 0 || target < minimum ||
                maximum < target || minimumSectors < 0 || maximumPerSector < 1)
                throw new ArgumentException("Invalid loop profile.");
            Enabled = enabled; EligibilityPercent = eligibilityPercent; Target = target; Minimum = minimum;
            Maximum = maximum; MinimumSectors = minimumSectors; MaximumPerSector = maximumPerSector;
        }
        public bool Enabled { get; }
        public int EligibilityPercent { get; }
        public int Target { get; }
        public int Minimum { get; }
        public int Maximum { get; }
        public int MinimumSectors { get; }
        public int MaximumPerSector { get; }
        public int MinimumLength => 4;
        public int MaximumLength => 24;
        public string Digest => RmapWorldDefinition.Hash("SV5_LOOP_PROFILE_V1|1.0.0|624|416|1|4|4|12|8|48|32|" +
            Enabled + "|" + EligibilityPercent + "|" + Target + "|" + Minimum + "|" + Maximum + "|" +
            MinimumSectors + "|" + MaximumPerSector + "|4|24|2|1|1|NO_PLUS_TWO|NO_SIDEPATH");
    }

    public sealed class Sv5LoopCell : IComparable<Sv5LoopCell>
    {
        internal Sv5LoopCell(string loopId, RmapSpecialWorldPoint world, Sv5LoopCellRole role,
            Sv5InfillCellValue sourceValue, Sv5InfillCellValue finalValue, string sourceOwner)
        {
            LoopId = loopId ?? string.Empty; World = world; Role = role; SourceValue = sourceValue;
            FinalValue = finalValue; SourceOwner = sourceOwner ?? string.Empty;
        }
        public string LoopId { get; }
        public RmapSpecialWorldPoint World { get; }
        public Sv5LoopCellRole Role { get; }
        public Sv5InfillCellValue SourceValue { get; }
        public Sv5InfillCellValue FinalValue { get; }
        public string SourceOwner { get; }
        public string Token => LoopId + "|" + World + "|" + Role + "|" + SourceValue + ">" + FinalValue + "|" + SourceOwner;
        public int CompareTo(Sv5LoopCell other)
        {
            if (other == null) return 1;
            int id = string.Compare(LoopId, other.LoopId, StringComparison.Ordinal);
            if (id != 0) return id;
            int point = World.CompareTo(other.World);
            return point != 0 ? point : Role.CompareTo(other.Role);
        }
    }

    public sealed class Sv5LoopCandidate : IComparable<Sv5LoopCandidate>
    {
        private readonly ReadOnlyCollection<RmapSpecialWorldPoint> footPath;
        private readonly ReadOnlyCollection<RmapSpecialWorldPoint> centerline;
        private readonly ReadOnlyCollection<Sv5LoopCell> cells;

        internal Sv5LoopCandidate(string id, string hash, ulong rank, string fromRoomId, string toRoomId,
            string fromApertureOwner, string toApertureOwner, string host,
            string sector, IEnumerable<RmapSpecialWorldPoint> sourceFeet,
            IEnumerable<RmapSpecialWorldPoint> sourceCenterline, IEnumerable<Sv5LoopCell> sourceCells,
            IEnumerable<RmapSpecialWorldPoint> sourceFromApproach,
            IEnumerable<RmapSpecialWorldPoint> sourceToApproach,
            int baselineCost, int finalCost, string status, string reason)
        {
            Id=id; StableHash=hash; StableRank=rank; FromRoomId=fromRoomId; ToRoomId=toRoomId;
            FromApertureOwner=fromApertureOwner; ToApertureOwner=toApertureOwner; Host=host; SectorId=sector;
            footPath=Array.AsReadOnly(sourceFeet.ToArray()); centerline=Array.AsReadOnly(sourceCenterline.ToArray());
            FromApproach=Array.AsReadOnly((sourceFromApproach ?? Array.Empty<RmapSpecialWorldPoint>()).ToArray());
            ToApproach=Array.AsReadOnly((sourceToApproach ?? Array.Empty<RmapSpecialWorldPoint>()).ToArray());
            cells=Array.AsReadOnly(sourceCells.OrderBy(c=>c).ToArray()); BaselineCost=baselineCost; FinalCost=finalCost;
            Status=status; Reason=reason;
        }
        public string Id { get; }
        public string StableHash { get; }
        public ulong StableRank { get; }
        public string FromRoomId { get; }
        public string ToRoomId { get; }
        public string FromApertureOwner { get; }
        public string ToApertureOwner { get; }
        public string FromOwner => FromRoomId;
        public string ToOwner => ToRoomId;
        public string Host { get; }
        public string SectorId { get; }
        public IReadOnlyList<RmapSpecialWorldPoint> FootPath => footPath;
        public IReadOnlyList<RmapSpecialWorldPoint> Centerline => centerline;
        public IReadOnlyList<RmapSpecialWorldPoint> FromApproach { get; }
        public IReadOnlyList<RmapSpecialWorldPoint> ToApproach { get; }
        public IReadOnlyList<Sv5LoopCell> Cells => cells;
        public int BaselineCost { get; internal set; }
        public int FinalCost { get; internal set; }
        public int NewCost => FinalCost;
        public bool Eligible { get; internal set; }
        public string Status { get; internal set; }
        public string Reason { get; internal set; }
        public string RoomPairKey => string.Compare(FromRoomId,ToRoomId,StringComparison.Ordinal) <= 0 ?
            FromRoomId+"|"+ToRoomId : ToRoomId+"|"+FromRoomId;
        public string AperturePairKey => string.Compare(FromApertureOwner,ToApertureOwner,StringComparison.Ordinal) <= 0 ?
            FromApertureOwner+"|"+ToApertureOwner : ToApertureOwner+"|"+FromApertureOwner;
        public string PairKey => EndpointPair(footPath.First(),footPath.Last());
        public string PathToken => string.Join(";",centerline);
        public string Token => Id+"|"+StableHash+"|"+StableRank+"|"+FromRoomId+"|"+ToRoomId+"|"+
            FromApertureOwner+"|"+ToApertureOwner+"|"+Host+"|"+SectorId+"|"+BaselineCost+"|"+NewCost+"|"+
            Status+"|"+Reason+"|"+string.Join(";",footPath)+"|"+PathToken+"|"+
            string.Join(";",FromApproach)+"|"+string.Join(";",ToApproach);
        public int CompareTo(Sv5LoopCandidate other)
        {
            if(other==null) return 1;
            int rank=StableRank.CompareTo(other.StableRank);
            return rank!=0 ? rank : string.Compare(Id,other.Id,StringComparison.Ordinal);
        }
        private static string EndpointPair(RmapSpecialWorldPoint first,RmapSpecialWorldPoint second)
            => first.CompareTo(second)<=0 ? first+"|"+second : second+"|"+first;
    }

    public sealed class Sv5LoopLink : IComparable<Sv5LoopLink>
    {
        private readonly ReadOnlyCollection<RmapSpecialWorldPoint> feet;
        private readonly ReadOnlyCollection<RmapSpecialWorldPoint> centerline;
        private readonly ReadOnlyCollection<Sv5LoopCell> cells;
        internal Sv5LoopLink(string id, Sv5LoopKind kind, string fromRoomId, string toRoomId,
            string fromApertureOwner, string toApertureOwner, string host, string sector,
            IEnumerable<RmapSpecialWorldPoint> sourceFeet, IEnumerable<RmapSpecialWorldPoint> sourceCenterline,
            IEnumerable<RmapSpecialWorldPoint> sourceFromApproach,
            IEnumerable<RmapSpecialWorldPoint> sourceToApproach,
            IEnumerable<Sv5LoopCell> sourceCells, int baselineCost, int newCost, string contourVariant,
            bool alternatePathExists, int cycleDelta, int bypassCount, int resourceOrdersChecked,
            int legalStatesChecked)
        {
            Id=id; Kind=kind; FromRoomId=fromRoomId; ToRoomId=toRoomId;
            FromApertureOwner=fromApertureOwner; ToApertureOwner=toApertureOwner; Host=host; SectorId=sector;
            feet=Array.AsReadOnly(sourceFeet.ToArray()); centerline=Array.AsReadOnly(sourceCenterline.ToArray());
            FromApproach=Array.AsReadOnly((sourceFromApproach ?? Array.Empty<RmapSpecialWorldPoint>()).ToArray());
            ToApproach=Array.AsReadOnly((sourceToApproach ?? Array.Empty<RmapSpecialWorldPoint>()).ToArray());
            cells=Array.AsReadOnly(sourceCells.OrderBy(c=>c).ToArray()); BaselineCost=baselineCost; NewCost=newCost;
            ContourVariant=contourVariant; AlternatePathExists=alternatePathExists; CycleDelta=cycleDelta;
            BypassCount=bypassCount; ResourceOrdersChecked=resourceOrdersChecked; LegalStatesChecked=legalStatesChecked;
        }
        public string Id { get; }
        public Sv5LoopKind Kind { get; }
        public string FromRoomId { get; }
        public string ToRoomId { get; }
        public string FromApertureOwner { get; }
        public string ToApertureOwner { get; }
        public string FromOwner => FromRoomId;
        public string ToOwner => ToRoomId;
        public string Host { get; }
        public string SectorId { get; }
        public IReadOnlyList<RmapSpecialWorldPoint> FootPath => feet;
        public IReadOnlyList<RmapSpecialWorldPoint> Centerline => centerline;
        public IReadOnlyList<RmapSpecialWorldPoint> FromApproach { get; }
        public IReadOnlyList<RmapSpecialWorldPoint> ToApproach { get; }
        public IReadOnlyList<Sv5LoopCell> Cells => cells;
        public int BaselineCost { get; }
        public int NewCost { get; }
        public int Reduction => BaselineCost-NewCost;
        public int ReductionPercent => BaselineCost <= 0 ? 0 : (BaselineCost-NewCost)*100/BaselineCost;
        public bool Significant => ReductionPercent >= 20;
        public string ContourVariant { get; }
        public bool AlternatePathExists { get; }
        public int CycleDelta { get; }
        public int BypassCount { get; }
        public int ResourceOrdersChecked { get; }
        public int LegalStatesChecked { get; }
        public string Token => Id+"|"+Kind+"|"+FromRoomId+"|"+ToRoomId+"|"+FromApertureOwner+"|"+
            ToApertureOwner+"|"+Host+"|"+SectorId+"|"+
            BaselineCost+"|"+NewCost+"|"+ContourVariant+"|"+AlternatePathExists+"|"+CycleDelta+"|"+
            BypassCount+"|"+ResourceOrdersChecked+"|"+LegalStatesChecked+"|"+string.Join(";",feet)+"|"+
            string.Join(";",centerline)+"|"+string.Join(";",FromApproach)+"|"+string.Join(";",ToApproach)+"|"+
            string.Join(";",cells.Select(c=>c.Token));
        public int CompareTo(Sv5LoopLink other) => other==null ? 1 : string.Compare(Id,other.Id,StringComparison.Ordinal);
    }

    public sealed class Sv5LoopPlan
    {
        internal Sv5LoopPlan(string baseline, Sv5LoopProfile profile, IEnumerable<Sv5LoopCandidate> candidates,
            IEnumerable<Sv5LoopLink> links, IEnumerable<string> diagnostics, IDictionary<string,int> rejections,
            Sv5LoopTopologyPlan topology)
        {
            BaselineDigest=baseline; Profile=profile;
            Candidates=Array.AsReadOnly(candidates.OrderBy(c=>c).ToArray());
            Links=Array.AsReadOnly(links.OrderBy(l=>l).ToArray());
            Cells=Array.AsReadOnly(Links.SelectMany(l=>l.Cells).OrderBy(c=>c).ToArray());
            var patternCells=Cells.Select(c=>new Sv5InfillCell(c.World,c.FinalValue,c.LoopId,
                "LOOP_"+Links.Single(l=>l.Id==c.LoopId).ContourVariant,host:Links.Single(l=>l.Id==c.LoopId).Host)).ToArray();
            Instances=Array.AsReadOnly(Sv5InfillPatterns.Split(patternCells).ToArray());
            Patterns=Array.AsReadOnly(Instances.Select(i=>i.Pattern).GroupBy(p=>p.Id,StringComparer.Ordinal)
                .Select(g=>g.First()).OrderBy(p=>p.Id,StringComparer.Ordinal).ToArray());
            Diagnostics=Array.AsReadOnly((diagnostics ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal)
                .OrderBy(v=>v,StringComparer.Ordinal).ToArray());
            Rejections=new ReadOnlyDictionary<string,int>(new SortedDictionary<string,int>(rejections ??
                new Dictionary<string,int>(),StringComparer.Ordinal));
            Topology=topology ?? throw new ArgumentNullException(nameof(topology));
            BaselineCycleRank=Topology.Before.CycleRank; FinalCycleRank=Topology.After.CycleRank;
            BaselineBridgeCount=Topology.Before.Bridges.Count; FinalBridgeCount=Topology.After.Bridges.Count;
            Digest=RmapWorldDefinition.Hash("SV5_ACTUAL_TILE_LOOPS_V1\n"+baseline+"\n"+profile.Digest+"\n"+
                string.Join("\n",Candidates.Select(c=>c.Token))+"\n"+string.Join("\n",Links.Select(l=>l.Token))+"\n"+
                string.Join("\n",Instances.Select(i=>i.Token))+"\n"+Topology.Digest+"\n"+string.Join("\n",Diagnostics));
        }
        public string BaselineDigest { get; }
        public Sv5LoopProfile Profile { get; }
        public IReadOnlyList<Sv5LoopCandidate> Candidates { get; }
        public IReadOnlyList<Sv5LoopLink> Links { get; }
        public IReadOnlyList<Sv5LoopCell> Cells { get; }
        public IReadOnlyList<Sv5InfillPattern> Patterns { get; }
        public IReadOnlyList<Sv5InfillInstance> Instances { get; }
        public IReadOnlyList<string> Diagnostics { get; }
        public IReadOnlyDictionary<string,int> Rejections { get; }
        public Sv5LoopTopologyPlan Topology { get; }
        public int BaselineCycleRank { get; }
        public int FinalCycleRank { get; }
        public int BaselineBridgeCount { get; }
        public int FinalBridgeCount { get; }
        public int AcceptedCount => Links.Count;
        public int DistinctSectorCount => Links.Select(l=>l.SectorId).Distinct(StringComparer.Ordinal).Count();
        public string Digest { get; }
        public bool Success => Diagnostics.Count==0 && Topology.Success && AcceptedCount>=Profile.Minimum && AcceptedCount<=Profile.Maximum &&
            DistinctSectorCount>=Profile.MinimumSectors && Links.GroupBy(l=>l.SectorId).All(g=>g.Count()<=Profile.MaximumPerSector) &&
            Links.All(l=>l.AlternatePathExists && l.CycleDelta==1 && l.BypassCount==0 && l.NewCost>=4 && l.NewCost<=24);
    }

    public static class Sv5SpaceLoops
    {
        private sealed class Desired
        {
            public Sv5InfillCellValue Value;
            public Sv5LoopCellRole Role;
        }

        private sealed class JunctionSegment
        {
            public string FromRoomId;
            public string ToRoomId;
            public string FromApertureOwner;
            public string ToApertureOwner;
            public string Host;
            public IReadOnlyList<RmapSpecialWorldPoint> Feet;
            public IReadOnlyList<RmapSpecialWorldPoint> FromApproach;
            public IReadOnlyList<RmapSpecialWorldPoint> ToApproach;
        }

        private sealed class ActualEndpoint
        {
            public string RoomId;
            public string ApertureOwner;
            public string Host;
            public RmapSpecialWorldPoint World;
            public IReadOnlyList<RmapSpecialWorldPoint> ExitNeighbors;
            public bool Preferred;
        }

        private sealed class PerformanceRecorder
        {
            private static readonly string[] RequiredRejections={"ENDPOINT_NOT_ACTUAL_ROOM","PATH_NOT_TRIMMED",
                "BASELINE_INTERIOR_OVERLAP","NEWLY_CARVED_LT_2","UNSUPPORTED_FOOT","PROTECTED","DUPLICATE_PAIR",
                "SECTOR_CAP","TOPOLOGY_NO_ALTERNATE","COST_UNREACHABLE"};
            private readonly SortedDictionary<string,double> timings=new SortedDictionary<string,double>(StringComparer.Ordinal);
            private readonly SortedDictionary<string,int> rejections=new SortedDictionary<string,int>(StringComparer.Ordinal);
            private readonly SortedDictionary<string,int> survivors=new SortedDictionary<string,int>(StringComparer.Ordinal);
            private readonly SortedDictionary<string,string> details=new SortedDictionary<string,string>(StringComparer.Ordinal);
            private readonly Stopwatch total=Stopwatch.StartNew();
            private readonly ulong seed; private readonly Sv5LoopProfile profile;

            internal PerformanceRecorder(ulong sourceSeed,Sv5LoopProfile sourceProfile)
            { seed=sourceSeed;profile=sourceProfile;foreach(string key in RequiredRejections) rejections[key]=0; }
            internal T Time<T>(string key,Func<T> action)
            {
                var watch=Stopwatch.StartNew(); try{return action();}
                finally{watch.Stop();timings[key]=Milliseconds(watch);}
            }
            internal void Time(string key,Action action)
            {
                var watch=Stopwatch.StartNew(); try{action();}
                finally{watch.Stop();timings[key]=Milliseconds(watch);}
            }
            internal void Reject(string key)
            { if(string.IsNullOrEmpty(key)) key="UNSPECIFIED";rejections[key]=rejections.TryGetValue(key,out int count) ? count+1 : 1; }
            internal void Survive(string key) => survivors[key]=survivors.TryGetValue(key,out int count) ? count+1 : 1;
            internal void Set(string key,int value) => survivors[key]=value;
            internal void Detail(string key,string value) => details[key]=value ?? string.Empty;
            internal IReadOnlyDictionary<string,int> Rejections => rejections;
            internal void Write(int candidates,int accepted,int sectors,int shortestCacheEntries)
            {
                total.Stop(); timings["total_build_ms"]=Milliseconds(total);
                string Object<T>(IEnumerable<KeyValuePair<string,T>> rows,Func<T,string> value) => "{"+
                    string.Join(",",rows.Select(v=>Quote(v.Key)+":"+value(v.Value)))+"}";
                string json="{\n  \"schema\":\"SV5_09_FIX01_PERFORMANCE_DIAGNOSTIC_V1\",\n"+
                    "  \"seed\":"+seed.ToString(CultureInfo.InvariantCulture)+",\n"+
                    "  \"profile\":{\"minimum\":"+profile.Minimum+",\"minimum_sectors\":"+profile.MinimumSectors+",\"target\":"+profile.Target+"},\n"+
                    "  \"timings_ms\":"+Object(timings,v=>v.ToString("0.###",CultureInfo.InvariantCulture))+",\n"+
                    "  \"rejection_histogram\":"+Object(rejections,v=>v.ToString(CultureInfo.InvariantCulture))+",\n"+
                    "  \"survivors\":"+Object(survivors,v=>v.ToString(CultureInfo.InvariantCulture))+",\n"+
                    "  \"details\":"+Object(details,v=>Quote(v))+",\n"+
                    "  \"counts\":{\"candidates\":"+candidates+",\"accepted\":"+accepted+",\"sectors\":"+sectors+
                    ",\"baseline_shortest_cache_entries\":"+shortestCacheEntries+"}\n}\n";
                string path=Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(),
                    "MapDesign","MCP","GENERATED","SV5_09_FIX01","_work","performance_diagnostic.json"));
                Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path,json,new UTF8Encoding(false));
                Console.WriteLine("[SV5_09_FIX01_PERFORMANCE] "+json.Replace("\r",string.Empty).Replace("\n"," "));
            }
            private static double Milliseconds(Stopwatch value) => value.ElapsedTicks*1000.0/Stopwatch.Frequency;
            private static string Quote(string value) => "\""+(value ?? string.Empty).Replace("\\","\\\\").Replace("\"","\\\"")+"\"";
        }

        public static Sv5LoopPlan Build(Sv5SpaceGraphPlan plan, Sv5LoopProfile profile = null)
        {
            if(plan==null) throw new ArgumentNullException(nameof(plan));
            if(plan.Infill==null || !plan.Infill.Success) throw new ArgumentException("A passing PlanWithInfill result is required.",nameof(plan));
            profile=profile ?? new Sv5LoopProfile();
            var performance=new PerformanceRecorder(plan.Seed,profile);
            var actual=plan.Infill.Cells.ToDictionary(c=>c.World,c=>c);
            var rooms=plan.Infill.Rooms.ToDictionary(r=>r.Id,r=>r,StringComparer.Ordinal);
            var forbidden=Forbidden(plan);
            var reservations=plan.Reservations.GroupBy(r=>r.World).ToDictionary(g=>g.Key,g=>g.ToArray());
            var baselineOccupancy=performance.Time("baseline_occupancy_ms",()=>Sv5LoopTopology.CaptureBaseline(plan));
            var context=performance.Time("baseline_foot_adjacency_room_graph_bridge_ms",
                ()=>new Sv5LoopTopologyContext(plan,baselineOccupancy));
            var generated=new List<Sv5LoopCandidate>();
            var emptyRoomPairDiagnostics=new List<string>();
            var pathTokens=new HashSet<string>(StringComparer.Ordinal);
            performance.Time("endpoint_and_candidate_generation_ms",()=>
            {
                var endpoints=new List<ActualEndpoint>();
                foreach(var cell in actual.Values.Where(c=>c.Value==Sv5InfillCellValue.Air).OrderBy(c=>c.World))
                {
                    string roomId=BaseRoom(cell.Owner);
                    if(!rooms.TryGetValue(roomId,out Sv5InfillRoom room) || !context.IsBaselineFoot(cell.World))
                    { performance.Reject("ENDPOINT_NOT_ACTUAL_ROOM"); continue; }
                    var exits=Sv5LoopTopology.FootNeighbors(cell.World).Where(p=>!context.IsBaselineFoot(p))
                        .OrderBy(p=>p).ToList();
                    if(exits.Count==0){performance.Reject("UNSUPPORTED_FOOT");continue;}
                    var preferredExits=new List<RmapSpecialWorldPoint>();
                    foreach(var next in exits)
                    {
                        var probeFeet=new[]{cell.World,next};
                        IReadOnlyList<RmapSpecialWorldPoint> probeCenter=Sv5SpaceInfill.CardinalCenterline(probeFeet);
                        if(TryCells(string.Empty,cell.Owner,cell.Owner,room.Host,probeFeet,probeCenter,actual,
                            baselineOccupancy,forbidden,reservations,out Sv5LoopCell[] ignored,out string ignoredError))
                            preferredExits.Add(next);
                    }
                    endpoints.Add(new ActualEndpoint{RoomId=room.Id,ApertureOwner=cell.Owner,Host=room.Host,World=cell.World,
                        ExitNeighbors=Array.AsReadOnly((preferredExits.Count==0 ? exits : preferredExits).ToArray()),
                        Preferred=preferredExits.Count!=0});
                }
                performance.Set("boundary_exit_endpoints",endpoints.Count);
                var byRoom=endpoints.GroupBy(v=>v.RoomId,StringComparer.Ordinal).ToDictionary(g=>g.Key,
                    g=>g.OrderBy(v=>v.World).ThenBy(v=>v.ApertureOwner,StringComparer.Ordinal).ToArray(),StringComparer.Ordinal);
                string[] roomIds=byRoom.Keys.OrderBy(v=>v,StringComparer.Ordinal).ToArray();
                var routePossibleCache=new Dictionary<string,HashSet<RmapSpecialWorldPoint>>(StringComparer.Ordinal);
                for(int ai=0;ai<roomIds.Length;ai++) for(int bi=ai+1;bi<roomIds.Length;bi++)
                {
                    string fromRoomId=roomIds[ai],toRoomId=roomIds[bi];
                    var left=rooms[fromRoomId]; var right=rooms[toRoomId];
                    if(left.Host!=right.Host || PairExclusionReason(left,right,left.Id,right.Id).Length!=0) continue;
                    var allEndpointPairs=byRoom[fromRoomId].SelectMany(from=>byRoom[toRoomId].Select(to=>new{From=from,To=to,
                            Cost=Distance(from.World,to.World)+1,
                            ExitPenalty=(from.Preferred ? 0 : 2)+(to.Preferred ? 0 : 2)+
                                (from.ExitNeighbors.Any(p=>Distance(p,to.World)<Distance(from.World,to.World)) ? 0 : 1)+
                                (to.ExitNeighbors.Any(p=>Distance(p,from.World)<Distance(to.World,from.World)) ? 0 : 1)}))
                        .Where(v=>v.Cost<=profile.MaximumLength && context.SameFootComponent(v.From.World,v.To.World))
                        .OrderBy(v=>v.ExitPenalty).ThenBy(v=>v.Cost).ThenBy(v=>v.From.World).ThenBy(v=>v.To.World)
                        .ThenBy(v=>v.From.ApertureOwner,StringComparer.Ordinal).ThenBy(v=>v.To.ApertureOwner,StringComparer.Ordinal)
                        .ToArray();
                    var roomWideEndpointPairs=allEndpointPairs.Take(8)
                        .Concat(allEndpointPairs.GroupBy(v=>v.From.World).Select(g=>g.First()).Take(8))
                        .Concat(allEndpointPairs.GroupBy(v=>v.To.World).Select(g=>g.First()).Take(8));
                    var apertureWideEndpointPairs=allEndpointPairs
                        .GroupBy(v=>Pair(v.From.ApertureOwner,v.To.ApertureOwner),StringComparer.Ordinal)
                        .OrderBy(g=>g.Key,StringComparer.Ordinal).SelectMany(group=>
                        {
                            var rows=group.ToArray();
                            return rows.Take(24)
                                .Concat(rows.GroupBy(v=>v.From.World).Select(g=>g.First()).Take(20))
                                .Concat(rows.GroupBy(v=>v.To.World).Select(g=>g.First()).Take(20))
                                .GroupBy(v=>v.From.World+"|"+v.To.World,StringComparer.Ordinal).Select(g=>g.First())
                                .OrderBy(v=>v.ExitPenalty).ThenBy(v=>v.Cost).ThenBy(v=>v.From.World).ThenBy(v=>v.To.World)
                                .Take(64);
                        });
                    var detourFriendlyEndpointPairs=allEndpointPairs
                        .Where(v=>Math.Abs(v.From.World.X-v.To.World.X)>=Math.Abs(v.From.World.Y-v.To.World.Y)+2)
                        .OrderByDescending(v=>Math.Abs(v.From.World.X-v.To.World.X)-Math.Abs(v.From.World.Y-v.To.World.Y))
                        .ThenBy(v=>v.Cost).ThenBy(v=>v.From.World).ThenBy(v=>v.To.World).Take(48);
                    var endpointPairs=roomWideEndpointPairs.Concat(apertureWideEndpointPairs).Concat(detourFriendlyEndpointPairs)
                        .GroupBy(v=>v.From.World+"|"+v.To.World,StringComparer.Ordinal).Select(g=>g.First())
                        .OrderBy(v=>v.ExitPenalty).ThenBy(v=>v.Cost).ThenBy(v=>v.From.World).ThenBy(v=>v.To.World)
                        .ToArray();
                    int generatedBeforeRoomPair=generated.Count;
                    var rejectionBeforeRoomPair=performance.Rejections.ToDictionary(v=>v.Key,v=>v.Value,StringComparer.Ordinal);
                    var rawRoomPairRejections=new SortedDictionary<string,int>(StringComparer.Ordinal);
                    void RawReject(string reason)
                    {
                        string raw=reason ?? "UNSPECIFIED"; int separator=raw.IndexOf('|');
                        if(separator>=0) raw=raw.Substring(0,separator);
                        rawRoomPairRejections[raw]=rawRoomPairRejections.TryGetValue(raw,out int count) ? count+1 : 1;
                    }
                    foreach(var endpointPair in endpointPairs)
                    {
                        string aperturePair=Pair(endpointPair.From.ApertureOwner,endpointPair.To.ApertureOwner);
                        performance.Survive("endpoint_coordinate_pairs_evaluated");
                        const int singleBendVariantCount=48;
                        const int doubleBendVariantCount=162;
                        const int contourVariantCount=singleBendVariantCount+doubleBendVariantCount;
                        const int endpointExitVariantCount=32;
                        const int routedVariantCount=16;
                        const int exitVariantStart=contourVariantCount;
                        const int routedVariantStart=exitVariantStart+endpointExitVariantCount;
                        const int geometryVariantCount=routedVariantStart+routedVariantCount;
                        for(int variant=0;variant<geometryVariantCount;variant++)
                        {
                            var routed=variant<singleBendVariantCount ?
                                SupportedContourPath(endpointPair.From.World,endpointPair.To.World,variant) :
                                variant<contourVariantCount ? SupportedDoubleContourPath(endpointPair.From.World,
                                    endpointPair.To.World,variant-singleBendVariantCount) :
                                variant<routedVariantStart ? EndpointExitPath(endpointPair.From,endpointPair.To,
                                    variant-exitVariantStart,actual) :
                                RoutedPath(endpointPair.From.World,endpointPair.To.World,variant-routedVariantStart,
                                    endpointPair.From.ApertureOwner,endpointPair.To.ApertureOwner,left.Host,actual,
                                    baselineOccupancy,context,forbidden,reservations,profile.MaximumLength,routePossibleCache);
                            if(routed.Count==0){performance.Reject("UNSUPPORTED_FOOT");RawReject("NO_PATH");continue;}
                            performance.Survive("raw_paths");
                            if(!TryTrimPath(routed,fromRoomId,toRoomId,actual,rooms,context,
                                profile.MaximumLength,out JunctionSegment segment,out string trimError))
                            { performance.Reject(trimError); RawReject(trimError); continue; }
                            performance.Survive("actual_room_endpoints"); performance.Survive("trimmed_paths");
                            var feet=segment.Feet; var center=Sv5SpaceInfill.CardinalCenterline(feet);
                            if(feet.Skip(1).Take(feet.Count-2).Any(context.IsBaselineFoot))
                            {performance.Reject("BASELINE_INTERIOR_OVERLAP");RawReject("BASELINE_INTERIOR_OVERLAP");continue;}
                            performance.Survive("baseline_interior_clear");
                            var geometryErrors=ValidateSupportedPath(feet,profile.MaximumLength);
                            if(geometryErrors.Count!=0 || center.Count<profile.MinimumLength || center.Count>profile.MaximumLength ||
                                center.Zip(center.Skip(1),(a,b)=>Distance(a,b)).Any(d=>d!=1))
                            {performance.Reject("UNSUPPORTED_FOOT");RawReject(geometryErrors.FirstOrDefault() ?? "CENTERLINE_INVALID");continue;}
                            performance.Survive("supported_geometry");
                            string pathToken=string.Join(";",center);
                            if(!pathTokens.Add(aperturePair+"|"+pathToken))
                            {performance.Reject("DUPLICATE_PAIR");RawReject("DUPLICATE_PAIR");continue;}
                            if(!TryCells(string.Empty,segment.FromApertureOwner,segment.ToApertureOwner,segment.Host,
                                feet,center,actual,baselineOccupancy,forbidden,reservations,
                                out Sv5LoopCell[] cells,out string cellError))
                            {
                                performance.Reject((cellError ?? string.Empty).StartsWith("PROTECTED_OR_RESERVED",StringComparison.Ordinal) ?
                                    "PROTECTED" : "UNSUPPORTED_FOOT"); RawReject(cellError); continue;
                            }
                            performance.Survive("protected_clear");
                            var byWorld=cells.ToDictionary(c=>c.World,c=>c);
                            int newlyCarved=feet.Count(p=>byWorld.TryGetValue(p,out Sv5LoopCell cell) &&
                                cell.SourceValue!=Sv5InfillCellValue.Air && cell.FinalValue==Sv5InfillCellValue.Air);
                            if(newlyCarved<2){performance.Reject("NEWLY_CARVED_LT_2");RawReject("NEWLY_CARVED_LT_2");continue;}
                            performance.Survive("newly_carved_at_least_two");
                            string hash=RmapWorldDefinition.Hash("SV5_LOOP_CANDIDATE_V2|"+plan.Seed+"|"+
                                fromRoomId+"|"+toRoomId+"|"+segment.FromApertureOwner+"|"+segment.ToApertureOwner+"|"+
                                pathToken+"|ELIGIBILITY_SALT_9");
                            ulong rank=ulong.Parse(hash.Substring(0,16),NumberStyles.HexNumber,CultureInfo.InvariantCulture);
                            string id="SV5_LOOP_CAND_"+hash.Substring(0,20); string sector=Sector(center[center.Count/2]);
                            var candidate=new Sv5LoopCandidate(id,hash,rank,fromRoomId,toRoomId,
                                segment.FromApertureOwner,segment.ToApertureOwner,segment.Host,sector,feet,center,cells,
                                segment.FromApproach,segment.ToApproach,0,0,"REJECTED","INELIGIBLE_HASH");
                            generated.Add(candidate); performance.Survive("generated_candidates");
                        }
                    }
                    if(endpointPairs.Length!=0 && generated.Count==generatedBeforeRoomPair)
                    {
                        string rejectionDelta=string.Join(",",performance.Rejections
                            .Where(v=>v.Value-(rejectionBeforeRoomPair.TryGetValue(v.Key,out int before) ? before : 0)>0)
                            .Select(v=>v.Key+"="+(v.Value-(rejectionBeforeRoomPair.TryGetValue(v.Key,out int before) ? before : 0))));
                        emptyRoomPairDiagnostics.Add(Pair(fromRoomId,toRoomId)+"|min_cost="+
                            (allEndpointPairs.Length==0 ? -1 : allEndpointPairs[0].Cost)+"|endpoint_pairs="+
                            endpointPairs.Length+"|"+rejectionDelta+"|raw="+
                            string.Join(",",rawRoomPairRejections.Select(v=>v.Key+"="+v.Value)));
                    }
                }
            });
            performance.Detail("empty_room_pair_rejections",string.Join(";",emptyRoomPairDiagnostics));
            performance.Detail("generated_aperture_pairs",string.Join(";",generated.Select(c=>c.AperturePairKey)
                .Distinct(StringComparer.Ordinal).OrderBy(v=>v,StringComparer.Ordinal)));

            var eligible=new List<Sv5LoopCandidate>();
            performance.Time("eligibility_and_packing_prepare_ms",()=>
            {
                int eligibilityBudget=(generated.Count*profile.EligibilityPercent+99)/100;
                var eligibilityGroups=generated.GroupBy(c=>c.PairKey,StringComparer.Ordinal)
                    .OrderBy(g=>g.Key,StringComparer.Ordinal).Select(g=>g.OrderBy(c=>c.StableRank)
                        .ThenBy(c=>c.Id,StringComparer.Ordinal).ToArray()).ToArray();
                var selectedEligibility=new HashSet<string>(StringComparer.Ordinal); int eligibilityRound=0;
                while(selectedEligibility.Count<eligibilityBudget)
                {
                    int before=selectedEligibility.Count;
                    foreach(var group in eligibilityGroups)
                    {
                        if(eligibilityRound<group.Length) selectedEligibility.Add(group[eligibilityRound].Id);
                        if(selectedEligibility.Count>=eligibilityBudget) break;
                    }
                    if(selectedEligibility.Count==before) break;
                    eligibilityRound++;
                }
                foreach(var candidate in generated)
                {
                    candidate.Eligible=selectedEligibility.Contains(candidate.Id);
                    if(candidate.Eligible){candidate.Status="ELIGIBLE";candidate.Reason="PENDING_EVALUATION";}
                }
                performance.Set("eligibility_budget",eligibilityBudget);
                performance.Set("eligible_endpoint_pairs",generated.Where(c=>c.Eligible)
                    .Select(c=>c.PairKey).Distinct(StringComparer.Ordinal).Count());
                performance.Set("eligible_aperture_pairs",generated.Where(c=>c.Eligible)
                    .Select(c=>c.AperturePairKey).Distinct(StringComparer.Ordinal).Count());
                performance.Set("eligible_sectors",generated.Where(c=>c.Eligible)
                    .Select(c=>c.SectorId).Distinct(StringComparer.Ordinal).Count());
                foreach(var group in generated.Where(c=>c.Eligible).GroupBy(c=>c.PairKey,StringComparer.Ordinal)
                    .OrderBy(g=>g.Key,StringComparer.Ordinal))
                {
                    var ordered=group.OrderBy(c=>c.Centerline.Count).ThenBy(c=>c.Cells.Count)
                        .ThenBy(c=>c.StableRank).ThenBy(c=>c.Id,StringComparer.Ordinal).ToArray();
                    foreach(var candidate in ordered.Take(32)) eligible.Add(candidate);
                    foreach(var candidate in ordered.Skip(32))
                    {candidate.Status="REJECTED";candidate.Reason="NOT_SHORTLISTED_GEOMETRY";}
                }
                performance.Set("eligible_shortlist",eligible.Count);
            });

            var evaluations=new Dictionary<string,Sv5LoopTopologyEvaluation>(StringComparer.Ordinal);
            var viable=new List<Sv5LoopCandidate>();
            performance.Time("accepted_candidate_evaluation_ms",()=>
            {
                foreach(var candidate in eligible.OrderBy(c=>c.Centerline.Count).ThenBy(c=>c.StableRank))
                {
                    var evaluation=Sv5LoopTopology.Evaluate(context,candidate.Id,candidate.FromRoomId,
                        candidate.ToRoomId,candidate.FootPath,candidate.Cells);
                    if(!evaluation.BaselineAlternatePath || !evaluation.EdgeRemovalAlternatePath)
                    {candidate.Status="REJECTED";candidate.Reason="TOPOLOGY_NO_ALTERNATE";performance.Reject(candidate.Reason);continue;}
                    performance.Survive("topology_alternate");
                    if(evaluation.BaselineCost<=0 || evaluation.FinalCost<=0)
                    {candidate.Status="REJECTED";candidate.Reason="COST_UNREACHABLE";performance.Reject(candidate.Reason);continue;}
                    performance.Survive("cost_reachable");
                    if(evaluation.Errors.Count!=0)
                    {candidate.Status="REJECTED";candidate.Reason="UNSUPPORTED_FOOT";performance.Reject(candidate.Reason);continue;}
                    candidate.BaselineCost=evaluation.BaselineCost; candidate.FinalCost=evaluation.FinalCost;
                    candidate.Reason="PENDING_PACKING"; evaluations[candidate.Id]=evaluation; viable.Add(candidate);
                }
                performance.Set("viable_before_packing",viable.Count);
                performance.Set("viable_endpoint_pairs",viable.Select(v=>v.PairKey).Distinct(StringComparer.Ordinal).Count());
                performance.Set("viable_aperture_pairs",viable.Select(v=>v.AperturePairKey).Distinct(StringComparer.Ordinal).Count());
                performance.Set("viable_room_pairs",viable.Select(v=>v.RoomPairKey).Distinct(StringComparer.Ordinal).Count());
                performance.Set("viable_sectors",viable.Select(v=>v.SectorId).Distinct(StringComparer.Ordinal).Count());
            });

            var packed=new List<Sv5LoopCandidate>(); var packedCells=new HashSet<RmapSpecialWorldPoint>();
            var packedPairs=new HashSet<string>(StringComparer.Ordinal);
            var packedSectors=new Dictionary<string,int>(StringComparer.Ordinal);
            var cellFrequency=viable.SelectMany(c=>c.Cells.Select(cell=>cell.World)).GroupBy(p=>p)
                .ToDictionary(g=>g.Key,g=>g.Count());
            long ConflictScore(Sv5LoopCandidate candidate)=>candidate.Cells.Sum(c=>(long)cellFrequency[c.World]-1L);
            var orderedCandidates=viable.OrderBy(ConflictScore).ThenBy(c=>c.Cells.Count)
                .ThenBy(c=>c.Centerline.Count).ThenBy(c=>c.StableRank).ThenBy(c=>c.Id,StringComparer.Ordinal).ToArray();
            var sectorGroups=viable.GroupBy(c=>c.SectorId,StringComparer.Ordinal)
                .OrderBy(g=>g.Select(c=>c.PairKey).Distinct(StringComparer.Ordinal).Count())
                .ThenBy(g=>g.Key,StringComparer.Ordinal).Select(g=>g.OrderBy(ConflictScore).ThenBy(c=>c.Cells.Count)
                    .ThenBy(c=>c.Centerline.Count).ThenBy(c=>c.StableRank).Take(96).ToArray()).ToArray();
            var bestPacked=new List<Sv5LoopCandidate>(); int bestSectorCount=0; long packingNodes=0;
            const long packingNodeLimit=2000;
            void RememberBest()
            {
                if(packed.Count>bestPacked.Count || (packed.Count==bestPacked.Count && packedSectors.Count>bestSectorCount))
                {bestPacked=new List<Sv5LoopCandidate>(packed);bestSectorCount=packedSectors.Count;}
            }
            bool TryAddPacked(Sv5LoopCandidate candidate)
            {
                int sectorCount=packedSectors.TryGetValue(candidate.SectorId,out int count) ? count : 0;
                if(sectorCount>=profile.MaximumPerSector || packedPairs.Contains(candidate.PairKey) ||
                    candidate.Cells.Any(c=>packedCells.Contains(c.World))) return false;
                packed.Add(candidate); packedPairs.Add(candidate.PairKey); packedSectors[candidate.SectorId]=sectorCount+1;
                foreach(var cell in candidate.Cells) packedCells.Add(cell.World);
                RememberBest(); return true;
            }
            void RemovePacked(Sv5LoopCandidate candidate)
            {
                int sectorCount=packedSectors[candidate.SectorId]; packed.Remove(candidate);
                packedPairs.Remove(candidate.PairKey);
                if(sectorCount==1) packedSectors.Remove(candidate.SectorId); else packedSectors[candidate.SectorId]=sectorCount-1;
                foreach(var cell in candidate.Cells) packedCells.Remove(cell.World);
            }
            void LoadPacked(IEnumerable<Sv5LoopCandidate> source)
            {
                packed.Clear(); packedCells.Clear(); packedPairs.Clear(); packedSectors.Clear();
                foreach(var candidate in source)
                {
                    packed.Add(candidate); packedPairs.Add(candidate.PairKey);
                    packedSectors[candidate.SectorId]=packedSectors.TryGetValue(candidate.SectorId,out int count) ? count+1 : 1;
                    foreach(var cell in candidate.Cells) packedCells.Add(cell.World);
                }
            }
            bool FillMinimumGreedy()
            {
                var additions=new List<Sv5LoopCandidate>();
                foreach(var candidate in orderedCandidates)
                {
                    if(packed.Count>=profile.Minimum) break;
                    if(TryAddPacked(candidate)) additions.Add(candidate);
                }
                bool result=packed.Count>=profile.Minimum;
                if(!result) for(int i=additions.Count-1;i>=0;i--) RemovePacked(additions[i]);
                return result;
            }
            bool SearchPacking(int sectorIndex)
            {
                packingNodes++; RememberBest();
                if(packingNodes>=packingNodeLimit) return false;
                if(packedSectors.Count>=profile.MinimumSectors) return FillMinimumGreedy();
                if(sectorIndex>=sectorGroups.Length ||
                    packedSectors.Count+sectorGroups.Length-sectorIndex<profile.MinimumSectors) return false;
                foreach(var candidate in sectorGroups[sectorIndex])
                {
                    if(!TryAddPacked(candidate)) continue;
                    if(SearchPacking(sectorIndex+1)) return true;
                    RemovePacked(candidate);
                }
                return SearchPacking(sectorIndex+1);
            }
            bool AugmentSameRoomPair()
            {
                int before=packed.Count;
                foreach(var victim in packed.ToArray())
                {
                    RemovePacked(victim);
                    var options=orderedCandidates.Where(c=>c.RoomPairKey==victim.RoomPairKey).Take(256).ToArray();
                    foreach(var first in options)
                    {
                        packingNodes++; if(!TryAddPacked(first)) continue;
                        foreach(var second in options)
                        {
                            packingNodes++; if(!TryAddPacked(second)) continue;
                            if(packed.Count>before) return true;
                            RemovePacked(second);
                        }
                        RemovePacked(first);
                    }
                    TryAddPacked(victim);
                }
                return false;
            }
            performance.Time("eligibility_and_packing_ms",()=>
            {
                bool solved=SearchPacking(0);
                if(!solved)
                {
                    LoadPacked(bestPacked);
                    while(packed.Count<profile.Minimum && packedSectors.Count>=profile.MinimumSectors && AugmentSameRoomPair()){}
                    solved=packed.Count>=profile.Minimum && packedSectors.Count>=profile.MinimumSectors;
                }
                if(profile.Minimum==0 && profile.MinimumSectors==0) packed.Clear();
                performance.Set("packing_search_nodes",packingNodes>int.MaxValue ? int.MaxValue : (int)packingNodes);
                performance.Set("packing_selected",packed.Count);
                performance.Detail("packing_selected_endpoint_pairs",string.Join(";",packed.Select(c=>c.PairKey)
                    .OrderBy(v=>v,StringComparer.Ordinal)));
            });

            var selectedPairs=new HashSet<string>(packed.Select(v=>v.PairKey),StringComparer.Ordinal);
            var selectedSectors=packed.GroupBy(v=>v.SectorId,StringComparer.Ordinal)
                .ToDictionary(g=>g.Key,g=>g.Count(),StringComparer.Ordinal);
            var selectedIds=new HashSet<string>(packed.Select(v=>v.Id),StringComparer.Ordinal);
            foreach(var candidate in viable.Where(v=>!selectedIds.Contains(v.Id)))
            {
                if(selectedPairs.Contains(candidate.PairKey)){candidate.Status="REJECTED";candidate.Reason="DUPLICATE_PAIR";}
                else if(selectedSectors.TryGetValue(candidate.SectorId,out int count) && count>=profile.MaximumPerSector)
                {candidate.Status="REJECTED";candidate.Reason="SECTOR_CAP";}
                else {candidate.Status="REJECTED";candidate.Reason="PACKING_CONFLICT";}
                performance.Reject(candidate.Reason);
            }

            var accepted=new List<Sv5LoopLink>();
            var acceptedEvaluations=new Dictionary<string,Sv5LoopTopologyEvaluation>(StringComparer.Ordinal);
            foreach(var candidate in packed.OrderBy(c=>c.SectorId,StringComparer.Ordinal).ThenBy(c=>c.StableRank))
            {
                string id="SV5_LOOP_"+candidate.StableHash.Substring(0,20);
                string contour=accepted.Count%2==0 ? "SHELF" : "SCALLOP";
                var recelled=candidate.Cells.Select(c=>new Sv5LoopCell(id,c.World,c.Role,c.SourceValue,c.FinalValue,c.SourceOwner)).ToArray();
                var evaluation=evaluations[candidate.Id];
                accepted.Add(new Sv5LoopLink(id,Classify(evaluation.BaselineCost,evaluation.FinalCost),
                    candidate.FromRoomId,candidate.ToRoomId,candidate.FromApertureOwner,candidate.ToApertureOwner,
                    candidate.Host,candidate.SectorId,candidate.FootPath,candidate.Centerline,candidate.FromApproach,
                    candidate.ToApproach,recelled,evaluation.BaselineCost,evaluation.FinalCost,contour,
                    evaluation.BaselineAlternatePath,evaluation.CycleDelta,0,0,0));
                acceptedEvaluations[id]=evaluation; candidate.Status="ACCEPTED";candidate.Reason="PASS";
            }

            var diagnostics=new List<string>();
            if(accepted.Count<profile.Minimum) diagnostics.Add("MINIMUM_LOOPS|"+accepted.Count+"/"+profile.Minimum);
            int distinct=accepted.Select(l=>l.SectorId).Distinct(StringComparer.Ordinal).Count();
            if(distinct<profile.MinimumSectors) diagnostics.Add("MINIMUM_LOOP_SECTORS|"+distinct+"/"+profile.MinimumSectors);
            if(accepted.GroupBy(l=>l.SectorId).Any(g=>g.Count()>profile.MaximumPerSector)) diagnostics.Add("LOOP_SECTOR_CAP");
            var finalTopology=performance.Time("final_topology_and_tarjan_bridge_ms",
                ()=>Sv5LoopTopology.Build(context,accepted,acceptedEvaluations,true));
            diagnostics.AddRange(finalTopology.Diagnostics);
            Sv5SpacePhysicalMovementPlan finalPhysical=null;
            performance.Time("final_9_states_x_6_resource_orders_ms",()=>
            {
                var finalMovement=ApplyPhysicalCells(plan.Connections,accepted);
                finalPhysical=accepted.Count==0 && profile.Minimum==0 ? plan.PhysicalMovement :
                    Sv5SpacePhysicalMovement.Analyze(plan.Core,finalMovement,plan.ContactDecisions,plan.Gates);
            });
            int finalStates=finalPhysical.GateStateChecks.Count;
            int finalOrders=finalPhysical.Product.Matrix.Select(v=>v.ResourceOrder).Distinct(StringComparer.Ordinal).Count();
            if(!finalPhysical.Success) diagnostics.Add("FSM_OR_RESOURCE_BYPASS|"+
                (finalPhysical.Diagnostics.FirstOrDefault() ?? "PHYSICAL_PRODUCT_FAILED"));
            var checkedLinks=accepted.Select(link=>new Sv5LoopLink(link.Id,link.Kind,link.FromRoomId,link.ToRoomId,
                link.FromApertureOwner,link.ToApertureOwner,link.Host,link.SectorId,link.FootPath,link.Centerline,
                link.FromApproach,link.ToApproach,link.Cells,link.BaselineCost,link.NewCost,
                link.ContourVariant,link.AlternatePathExists,link.CycleDelta,finalPhysical.Diagnostics.Count,
                finalOrders,finalStates)).ToArray();
            var rejection=new Dictionary<string,int>(performance.Rejections,StringComparer.Ordinal);
            foreach(var candidate in generated.Where(c=>c.Status=="REJECTED")) Add(rejection,candidate.Reason);
            var result=new Sv5LoopPlan(plan.Digest,profile,generated,checkedLinks,diagnostics,rejection,finalTopology);
            performance.Write(generated.Count,result.AcceptedCount,result.DistinctSectorCount,context.BaselineShortestCacheEntries);
            return result;
        }

        public static IReadOnlyList<string> ValidatePayload(Sv5SpaceGraphPlan baseline,Sv5LoopPlan payload)
        {
            var errors=new List<string>();
            if(baseline==null || payload==null) return Array.AsReadOnly(new[]{"LOOP_PAYLOAD_NULL"});
            if(baseline.Digest!=payload.BaselineDigest) errors.Add("LOOP_BASELINE_DIGEST_MISMATCH");
            errors.AddRange(payload.Diagnostics);
            errors.AddRange(payload.Topology.Diagnostics.Select(e=>"LOOP_TOPOLOGY|"+e));
            if(payload.Links.Select(l=>l.Id).Distinct(StringComparer.Ordinal).Count()!=payload.Links.Count) errors.Add("DUPLICATE_LOOP_ID");
            if(payload.Links.Select(l=>EndpointPair(l.FootPath.First(),l.FootPath.Last()))
                .Distinct(StringComparer.Ordinal).Count()!=payload.Links.Count) errors.Add("DUPLICATE_LOOP_PAIR");
            if(payload.Cells.GroupBy(c=>c.World).Any(g=>g.Select(c=>c.LoopId).Distinct(StringComparer.Ordinal).Count()>1)) errors.Add("LOOP_CELL_OVERLAP");
            foreach(var link in payload.Links)
            {
                if(ValidateSupportedPath(link.FootPath,payload.Profile.MaximumLength).Count!=0) errors.Add("LOOP_PATH_INVALID|"+link.Id);
                if(link.Centerline.Count<4 || link.Centerline.Count>24 ||
                    link.Centerline.Zip(link.Centerline.Skip(1),(a,b)=>Distance(a,b)).Any(d=>d!=1)) errors.Add("LOOP_CENTERLINE_INVALID|"+link.Id);
                if(!link.AlternatePathExists || link.CycleDelta!=1) errors.Add("LOOP_CYCLE_INVALID|"+link.Id);
                if(link.BypassCount!=0 || link.ResourceOrdersChecked!=6) errors.Add("LOOP_STATE_INVALID|"+link.Id);
                if(link.Kind!=Classify(link.BaselineCost,link.NewCost)) errors.Add("LOOP_CLASSIFICATION_INVALID|"+link.Id);
            }
            var reconstructed=Sv5InfillPatterns.Reconstruct(payload.Instances);
            if(reconstructed.Count!=payload.Cells.Select(c=>c.World).Distinct().Count() ||
                payload.Cells.Any(c=>!reconstructed.TryGetValue(c.World,out var value) || value!=c.FinalValue)) errors.Add("LOOP_PATTERN_RECONSTRUCTION");
            var state=Sv5SpacePhysicalMovement.FindStateErrors(baseline.Core,ApplyPhysicalCells(baseline.Connections,payload),baseline.Gates);
            if(state.Count!=0) errors.AddRange(state.Select(e=>"LOOP_"+e));
            return Array.AsReadOnly(errors.Distinct(StringComparer.Ordinal).OrderBy(e=>e,StringComparer.Ordinal).ToArray());
        }

        public static IReadOnlyList<Sv5SpaceConnection> ApplyPhysicalCells(IEnumerable<Sv5SpaceConnection> source,
            Sv5LoopPlan loops) => ApplyPhysicalCells(source,loops==null ? Array.Empty<Sv5LoopLink>() : loops.Links);

        internal static IReadOnlyList<Sv5SpaceConnection> ApplyPhysicalCells(IEnumerable<Sv5SpaceConnection> source,
            IEnumerable<Sv5LoopLink> loops)
        {
            var links=(loops ?? Array.Empty<Sv5LoopLink>()).ToArray();
            var additions=links.GroupBy(l=>l.Host,StringComparer.Ordinal).ToDictionary(g=>g.Key,g=>g.SelectMany(l=>
                l.Cells.Where(c=>c.FinalValue==Sv5InfillCellValue.Air).Select(c=>c.World)).Distinct().ToArray(),StringComparer.Ordinal);
            var supports=links.GroupBy(l=>l.Host,StringComparer.Ordinal).ToDictionary(g=>g.Key,g=>new HashSet<RmapSpecialWorldPoint>(g.SelectMany(l=>
                l.Cells.Where(c=>c.FinalValue==Sv5InfillCellValue.Solid).Select(c=>c.World))),StringComparer.Ordinal);
            return Array.AsReadOnly((source ?? Array.Empty<Sv5SpaceConnection>()).Select(c=>
            {
                if(!additions.TryGetValue(c.Id,out RmapSpecialWorldPoint[] cells) || cells.Length==0) return c;
                supports.TryGetValue(c.Id,out HashSet<RmapSpecialWorldPoint> solid);
                solid=solid ?? new HashSet<RmapSpecialWorldPoint>();
                return new Sv5SpaceConnection(c.Id,c.Kind,c.FromPortId,c.ToPortId,c.FromPlaceId,c.ToPlaceId,c.Direction,
                    c.Flow,c.Condition,c.SourceGraphEdgeId,c.SelectionState,c.Centerline,c.Envelope.Concat(cells),
                    c.ApertureCells.Where(p=>!solid.Contains(p)).Concat(cells));
            }).ToArray());
        }

        public static string PairExclusionReason(Sv5InfillRoom left,Sv5InfillRoom right,string leftOwner,string rightOwner)
        {
            if(left==null || right==null) return "MISSING_ENDPOINT_ROOM";
            if(left.Id==right.Id) return "SAME_ROOM_OR_APERTURE";
            if(left.Parent==right.Id || right.Parent==left.Id) return "DIRECT_PARENT_CHILD";
            if(leftOwner==rightOwner) return "DUPLICATE_APERTURE";
            return string.Empty;
        }

        public static Sv5LoopKind Classify(int baselineCost,int newCost) => newCost<baselineCost ?
            Sv5LoopKind.RandomShortcut : Sv5LoopKind.Loop;

        public static IReadOnlyList<string> ValidateSupportedPath(IEnumerable<RmapSpecialWorldPoint> source,int maximum=24)
        {
            var feet=(source ?? Array.Empty<RmapSpecialWorldPoint>()).ToArray(); var errors=new List<string>();
            if(feet.Length==0) errors.Add("EMPTY_SUPPORTED_PATH");
            if(feet.Distinct().Count()!=feet.Length) errors.Add("SELF_INTERSECTION");
            foreach(var pair in feet.Zip(feet.Skip(1),(a,b)=>new{a,b}))
            {
                if(Math.Abs(pair.a.X-pair.b.X)!=1) errors.Add("NON_HORIZONTAL_STEP");
                if(Math.Abs(pair.a.Y-pair.b.Y)>1) errors.Add("UNSUPPORTED_PLUS_TWO");
            }
            if(feet.Length!=0 && !errors.Contains("NON_HORIZONTAL_STEP") && !errors.Contains("UNSUPPORTED_PLUS_TWO"))
            {
                int length=Sv5SpaceInfill.CardinalCenterline(feet).Count;
                if(length<4) errors.Add("UNDER_MINIMUM|"+length);
                if(length>maximum) errors.Add("OVER_MAXIMUM|"+length+"/"+maximum);
            }
            return Array.AsReadOnly(errors.Distinct(StringComparer.Ordinal).ToArray());
        }

        private static IReadOnlyList<RmapSpecialWorldPoint> SupportedPath(RmapSpecialWorldPoint start,
            RmapSpecialWorldPoint goal,bool early)
        {
            int dx=goal.X-start.X,dy=goal.Y-start.Y,columns=Math.Abs(dx),rise=Math.Abs(dy);
            if(columns==0 || columns<rise) return Array.AsReadOnly(Array.Empty<RmapSpecialWorldPoint>());
            int sx=Math.Sign(dx),sy=Math.Sign(dy); var result=new List<RmapSpecialWorldPoint>();
            for(int i=0;i<=columns;i++)
            {
                int climbed=rise==0 ? 0 : early ? (i*rise+columns-1)/columns : (i*rise)/columns;
                result.Add(new RmapSpecialWorldPoint(start.X+sx*i,start.Y+sy*climbed));
            }
            result[result.Count-1]=goal;
            return Array.AsReadOnly(result.ToArray());
        }

        private static IReadOnlyList<RmapSpecialWorldPoint> SupportedContourPath(
            RmapSpecialWorldPoint start,RmapSpecialWorldPoint goal,int variant)
        {
            if(variant<2) return SupportedPath(start,goal,variant==1);
            int columns=Math.Abs(goal.X-start.X);
            if(columns<4) return Array.AsReadOnly(Array.Empty<RmapSpecialWorldPoint>());
            int code=(variant-2)/2;
            int bendOffset=code%5-2;
            int left=Math.Max(1,Math.Min(columns-1,columns/2+bendOffset));
            int right=columns-left,sx=Math.Sign(goal.X-start.X);
            int low=Math.Max(start.Y-left,goal.Y-right),high=Math.Min(start.Y+left,goal.Y+right);
            int baseY=start.Y+(goal.Y-start.Y)*left/columns;
            int amplitude=code/5+1;
            int desired=baseY+(variant%2==0 ? amplitude : -amplitude);
            int middleY=Math.Max(low,Math.Min(high,desired));
            var middle=new RmapSpecialWorldPoint(start.X+sx*left,middleY);
            var first=SupportedPath(start,middle,variant%2==0);
            var second=SupportedPath(middle,goal,variant%2!=0);
            if(first.Count==0 || second.Count==0) return Array.AsReadOnly(Array.Empty<RmapSpecialWorldPoint>());
            return Array.AsReadOnly(first.Concat(second.Skip(1)).ToArray());
        }

        private static IReadOnlyList<RmapSpecialWorldPoint> SupportedDoubleContourPath(
            RmapSpecialWorldPoint start,RmapSpecialWorldPoint goal,int variant)
        {
            int columns=Math.Abs(goal.X-start.X);
            if(columns<6) return Array.AsReadOnly(Array.Empty<RmapSpecialWorldPoint>());
            int sx=Math.Sign(goal.X-start.X),firstColumn=Math.Max(1,columns/3);
            int secondColumn=Math.Min(columns-1,Math.Max(firstColumn+1,columns*2/3));
            int code=variant/2,firstOffset=code%9-4,secondOffset=(code/9)%9-4;
            int firstBase=start.Y+(goal.Y-start.Y)*firstColumn/columns;
            int secondBase=start.Y+(goal.Y-start.Y)*secondColumn/columns;
            var firstPoint=new RmapSpecialWorldPoint(start.X+sx*firstColumn,firstBase+firstOffset);
            var secondPoint=new RmapSpecialWorldPoint(start.X+sx*secondColumn,secondBase+secondOffset);
            bool early=variant%2!=0;
            var first=SupportedPath(start,firstPoint,early);
            var second=SupportedPath(firstPoint,secondPoint,!early);
            var third=SupportedPath(secondPoint,goal,early);
            if(first.Count==0 || second.Count==0 || third.Count==0)
                return Array.AsReadOnly(Array.Empty<RmapSpecialWorldPoint>());
            return Array.AsReadOnly(first.Concat(second.Skip(1)).Concat(third.Skip(1)).ToArray());
        }

        private static IReadOnlyList<RmapSpecialWorldPoint> EndpointExitPath(ActualEndpoint from,ActualEndpoint to,
            int variant,IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5InfillCell> actual)
        {
            var exitPairs=from.ExitNeighbors.Where(p=>!IsActualAir(p,actual))
                .SelectMany(left=>to.ExitNeighbors.Where(p=>!IsActualAir(p,actual)).Select(right=>new{Left=left,Right=right}))
                .Where(v=>Math.Abs(v.Left.X-v.Right.X)>=Math.Abs(v.Left.Y-v.Right.Y) && !v.Left.Equals(v.Right))
                .OrderBy(v=>Distance(v.Left,v.Right)).ThenBy(v=>v.Left).ThenBy(v=>v.Right).Take(16).ToArray();
            int pairIndex=variant/2;
            if(pairIndex>=exitPairs.Length) return Array.AsReadOnly(Array.Empty<RmapSpecialWorldPoint>());
            var pair=exitPairs[pairIndex];
            var middle=SupportedPath(pair.Left,pair.Right,variant%2!=0);
            if(middle.Count==0) return Array.AsReadOnly(Array.Empty<RmapSpecialWorldPoint>());
            return Array.AsReadOnly(new[]{from.World}.Concat(middle).Concat(new[]{to.World}).ToArray());
        }

        private static bool IsActualAir(RmapSpecialWorldPoint point,
            IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5InfillCell> actual)
            => actual.TryGetValue(point,out Sv5InfillCell cell) && cell.Value==Sv5InfillCellValue.Air;

        private static bool TryCells(string loopId,string from,string to,string host,IReadOnlyList<RmapSpecialWorldPoint> feet,
            IReadOnlyList<RmapSpecialWorldPoint> centerline,IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5InfillCell> actual,
            IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell> occupancy,
            ISet<RmapSpecialWorldPoint> forbidden,
            IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5SpaceReservationCell[]> reservations,
            out Sv5LoopCell[] output,out string error)
        {
            var desired=new Dictionary<RmapSpecialWorldPoint,Desired>(); error=string.Empty; string localError=string.Empty;
            bool AddDesired(RmapSpecialWorldPoint p,Sv5InfillCellValue value,Sv5LoopCellRole role)
            {
                if(desired.TryGetValue(p,out Desired existing))
                {
                    if(existing.Value!=value){localError="CONFLICTING_FINAL_VALUE|"+p;return false;}
                    if(role==Sv5LoopCellRole.ApertureOverride) existing.Role=role;
                    return true;
                }
                desired.Add(p,new Desired{Value=value,Role=role}); return true;
            }
            foreach(var p in centerline)
            {
                if(!AddDesired(p,Sv5InfillCellValue.Air,Sv5LoopCellRole.Air) ||
                    !AddDesired(new RmapSpecialWorldPoint(p.X,p.Y+1),Sv5InfillCellValue.Air,Sv5LoopCellRole.Air))
                { error=localError; output=Array.Empty<Sv5LoopCell>(); return false; }
            }
            foreach(var p in feet)
                if(!AddDesired(new RmapSpecialWorldPoint(p.X,p.Y-1),Sv5InfillCellValue.Solid,Sv5LoopCellRole.SolidSupport))
                { error=localError; output=Array.Empty<Sv5LoopCell>(); return false; }
            var cells=new List<Sv5LoopCell>();
            foreach(var pair in desired.OrderBy(p=>p.Key))
            {
                var p=pair.Key; var d=pair.Value;
                if(p.X<0 || p.X>=Sv5SpaceGraphPlanner.WorldWidth || p.Y<0 || p.Y>=Sv5SpaceGraphPlanner.WorldHeight)
                { error="WORLD_OUTSIDE|"+p; output=Array.Empty<Sv5LoopCell>(); return false; }
                actual.TryGetValue(p,out Sv5InfillCell source);
                var sourceValue=Sv5LoopTopology.ValueAt(occupancy,p);
                bool endpointOwned=source!=null && (source.Owner==from || source.Owner==to);
                if(forbidden.Contains(p) && !endpointOwned)
                { error="PROTECTED_OR_RESERVED|"+p; output=Array.Empty<Sv5LoopCell>(); return false; }
                if(!endpointOwned && reservations.TryGetValue(p,out Sv5SpaceReservationCell[] reserved) &&
                    !(source!=null && d.Value==Sv5InfillCellValue.Solid && source.Value==Sv5InfillCellValue.Solid) &&
                    !((d.Value==Sv5InfillCellValue.Air && reserved.All(r=>r.OwnerId==host &&
                        (r.Kind==Sv5SpaceReservationKind.CorridorClearance ||
                         r.Kind==Sv5SpaceReservationKind.CorridorCenterline ||
                         r.Kind==Sv5SpaceReservationKind.PortAperture))) ||
                       (d.Value==Sv5InfillCellValue.Solid && reserved.All(r=>r.OwnerId==host &&
                         (r.Kind==Sv5SpaceReservationKind.CorridorClearance ||
                          r.Kind==Sv5SpaceReservationKind.PortAperture)))))
                { error="PROTECTED_OR_RESERVED|"+p; output=Array.Empty<Sv5LoopCell>(); return false; }
                bool existingHostAir=source==null && d.Value==Sv5InfillCellValue.Air &&
                    reservations.TryGetValue(p,out Sv5SpaceReservationCell[] hostRows) && hostRows.Any(r=>r.OwnerId==host &&
                        (r.Kind==Sv5SpaceReservationKind.CorridorCenterline || r.Kind==Sv5SpaceReservationKind.PortAperture));
                var role=d.Role;
                if(d.Value==Sv5InfillCellValue.Air)
                {
                    if(source!=null && !endpointOwned){error=(source.Value==Sv5InfillCellValue.Air ? "EXTERNAL_AIR_DEPENDENCY|" : "FOREIGN_SOLID_OPENING|")+p;output=Array.Empty<Sv5LoopCell>();return false;}
                    if(source!=null && source.Value==Sv5InfillCellValue.Solid)
                        role=Sv5LoopCellRole.ApertureOverride;
                }
                if(d.Value==Sv5InfillCellValue.Solid && source!=null &&
                    source.Value!=Sv5InfillCellValue.Solid && !endpointOwned &&
                    sourceValue!=Sv5InfillCellValue.Solid)
                { error="SUPPORT_NOT_SOLID|"+p; output=Array.Empty<Sv5LoopCell>(); return false; }
                occupancy.TryGetValue(p,out Sv5LoopOccupancyCell occupancyCell);
                cells.Add(new Sv5LoopCell(loopId,p,role,sourceValue,d.Value,source?.Owner ??
                    occupancyCell?.Owner ?? (existingHostAir ? host : string.Empty)));
            }
            output=cells.ToArray(); return true;
        }

        private static HashSet<RmapSpecialWorldPoint> Forbidden(Sv5SpaceGraphPlan plan)
        {
            var result=new HashSet<RmapSpecialWorldPoint>(plan.Core.CoreCells.Select(c=>c.World));
            result.UnionWith(plan.Core.RouteCells.Select(c=>c.World));
            result.UnionWith(plan.Core.RouteSource.Secrets.SelectMany(s=>s.Chunks).SelectMany(chunk=>
                Enumerable.Range(0,8).SelectMany(y=>Enumerable.Range(0,12).Select(x=>new RmapSpecialWorldPoint(chunk.X*12+x,chunk.Y*8+y)))));
            result.UnionWith(plan.Gates.SelectMany(g=>g.BlockingCells));
            result.UnionWith(plan.Gates.SelectMany(g=>g.BlockingFaces).SelectMany(f=>new[]{f.First,f.Second}));
            result.UnionWith(plan.Ports.SelectMany(p=>p.BoundaryCells.Concat(new[]{p.Anchor})));
            return result;
        }

        private static bool TryTrimPath(IReadOnlyList<RmapSpecialWorldPoint> source,string fromRoomId,
            string toRoomId,IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5InfillCell> actual,
            IReadOnlyDictionary<string,Sv5InfillRoom> rooms,Sv5LoopTopologyContext context,
            int maximum,out JunctionSegment segment,out string error)
        {
            segment=null; error=string.Empty; var path=(source ?? Array.Empty<RmapSpecialWorldPoint>()).ToArray();
            if(!rooms.TryGetValue(fromRoomId,out Sv5InfillRoom left) ||
                !rooms.TryGetValue(toRoomId,out Sv5InfillRoom right) || left.Host!=right.Host)
            {error="ENDPOINT_NOT_ACTUAL_ROOM";return false;}
            var actualContacts=Enumerable.Range(0,path.Length).Where(index=>context.IsBaselineFoot(path[index]) &&
                actual.TryGetValue(path[index],out Sv5InfillCell cell) && cell.Value==Sv5InfillCellValue.Air &&
                rooms.ContainsKey(BaseRoom(cell.Owner))).ToArray();
            var targets=actualContacts.Where(index=>BaseRoom(actual[path[index]].Owner)==toRoomId).ToArray();
            if(targets.Length==0){error="ENDPOINT_NOT_ACTUAL_ROOM";return false;}
            int last=targets.First();
            var sources=actualContacts.Where(index=>index<last && BaseRoom(actual[path[index]].Owner)==fromRoomId).ToArray();
            if(sources.Length==0){error="ENDPOINT_NOT_ACTUAL_ROOM";return false;}
            int first=sources.Last();
            if(first<0 || last<=first || last>=path.Length){error="PATH_NOT_TRIMMED";return false;}
            var trimmed=path.Skip(first).Take(last-first+1).ToArray();
            if(trimmed.Length<2 || Sv5SpaceInfill.CardinalCenterline(trimmed).Count>maximum ||
                !context.IsBaselineFoot(trimmed.First()) || !context.IsBaselineFoot(trimmed.Last()))
            {error="PATH_NOT_TRIMMED";return false;}
            if(trimmed.Skip(1).Take(trimmed.Length-2).Any(context.IsBaselineFoot))
            {error="BASELINE_INTERIOR_OVERLAP";return false;}
            string fromApertureOwner=actual[trimmed.First()].Owner;
            string toApertureOwner=actual[trimmed.Last()].Owner;
            if(BaseRoom(fromApertureOwner)!=fromRoomId || BaseRoom(toApertureOwner)!=toRoomId)
            {error="ENDPOINT_NOT_ACTUAL_ROOM";return false;}
            if(PairExclusionReason(left,right,fromApertureOwner,toApertureOwner).Length!=0)
            {error="PATH_NOT_TRIMMED";return false;}
            segment=new JunctionSegment{FromRoomId=left.Id,ToRoomId=right.Id,
                FromApertureOwner=fromApertureOwner,ToApertureOwner=toApertureOwner,Host=left.Host,
                Feet=Array.AsReadOnly(trimmed),FromApproach=Array.AsReadOnly(path.Take(first).ToArray()),
                ToApproach=Array.AsReadOnly(path.Skip(last+1).ToArray())};
            return true;
        }
        private static IReadOnlyList<RmapSpecialWorldPoint> RoutedPath(RmapSpecialWorldPoint start,
            RmapSpecialWorldPoint goal,int routeVariant,string from,string to,string host,
            IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5InfillCell> actual,
            IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell> occupancy,
            Sv5LoopTopologyContext context,ISet<RmapSpecialWorldPoint> forbidden,
            IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5SpaceReservationCell[]> reservations,int maximum,
            IDictionary<string,HashSet<RmapSpecialWorldPoint>> possibleCache)
        {
            int directCost=Distance(start,goal)+1;
            if(directCost>maximum) return Array.AsReadOnly(Array.Empty<RmapSpecialWorldPoint>());
            int detourColumns=(maximum-directCost)/2;
            int minX=Math.Max(0,Math.Min(start.X,goal.X)-detourColumns);
            int maxX=Math.Min(Sv5SpaceGraphPlanner.WorldWidth-1,Math.Max(start.X,goal.X)+detourColumns);
            if(maxX-minX+1>maximum)
            {
                int excess=maxX-minX+1-maximum; minX+=excess/2; maxX-=excess-excess/2;
            }
            int ySpan=Math.Abs(start.Y-goal.Y)+1;
            int ySlack=Math.Max(0,maximum-ySpan);
            int minY=Math.Max(1,Math.Min(start.Y,goal.Y)-ySlack/2);
            int maxY=Math.Min(Sv5SpaceGraphPlanner.WorldHeight-2,minY+maximum-1);
            minY=Math.Max(1,maxY-maximum+1);
            string possibleKey=start+"|"+goal+"|"+from+"|"+to+"|"+host+"|"+minX+"|"+maxX+"|"+minY+"|"+maxY;
            if(!possibleCache.TryGetValue(possibleKey,out HashSet<RmapSpecialWorldPoint> possible))
            {
                possible=new HashSet<RmapSpecialWorldPoint>{start,goal};
                for(int y=minY;y<=maxY;y++) for(int x=minX;x<=maxX;x++)
                {
                    var p=new RmapSpecialWorldPoint(x,y);
                    if(p.Equals(start) || p.Equals(goal) || context.IsBaselineFoot(p)) continue;
                    if(actual.TryGetValue(p,out Sv5InfillCell infill) && infill.Value==Sv5InfillCellValue.Air) continue;
                    if(TryCells(string.Empty,from,to,host,new[]{p},new[]{p},actual,occupancy,forbidden,reservations,
                        out Sv5LoopCell[] ignored,out string ignoredError)) possible.Add(p);
                }
                possibleCache[possibleKey]=possible;
            }
            var parent=new Dictionary<RmapSpecialWorldPoint,RmapSpecialWorldPoint>{{start,start}};
            var distance=new Dictionary<RmapSpecialWorldPoint,int>{{start,1}};
            var queue=new Queue<RmapSpecialWorldPoint>(); queue.Enqueue(start);
            int direction=Math.Sign(goal.X-start.X);
            int[] dxs=direction==0 ? (routeVariant%2==0 ? new[]{1,-1} : new[]{-1,1}) :
                (routeVariant%2==0 ? new[]{direction,-direction} : new[]{-direction,direction});
            int[][] yOrders={new[]{-1,0,1},new[]{-1,1,0},new[]{0,-1,1},new[]{0,1,-1},new[]{1,-1,0},new[]{1,0,-1}};
            int[] dys=yOrders[(routeVariant/2)%yOrders.Length];
            while(queue.Count!=0 && !parent.ContainsKey(goal))
            {
                var p=queue.Dequeue();
                var nextRows=dxs.SelectMany(dx=>dys.Select(dy=>new RmapSpecialWorldPoint(p.X+dx,p.Y+dy)))
                    .OrderBy(next=>RouteTieBreak(next,goal,routeVariant)).ThenBy(next=>Distance(next,goal)).ToArray();
                foreach(var next in nextRows)
                {
                    int nextCost=distance[p]+1+Math.Abs(next.Y-p.Y);
                    if(!possible.Contains(next) || parent.ContainsKey(next) || nextCost>maximum) continue;
                    parent[next]=p; distance[next]=nextCost; queue.Enqueue(next);
                }
            }
            if(!parent.ContainsKey(goal)) return Array.AsReadOnly(Array.Empty<RmapSpecialWorldPoint>());
            var path=new List<RmapSpecialWorldPoint>{goal};
            while(!path[path.Count-1].Equals(start)) path.Add(parent[path[path.Count-1]]);
            path.Reverse(); return Array.AsReadOnly(path.ToArray());
        }
        private static int RouteTieBreak(RmapSpecialWorldPoint point,RmapSpecialWorldPoint goal,int variant)
        {
            unchecked
            {
                int value=point.X*73856093^point.Y*19349663^goal.X*83492791^goal.Y*297121507^
                    (variant+1)*1640531513;
                return value&int.MaxValue;
            }
        }
        private static IReadOnlyDictionary<RmapSpecialWorldPoint,int> FootComponents(
            IEnumerable<RmapSpecialWorldPoint> source)
        {
            var nodes=new HashSet<RmapSpecialWorldPoint>(source ?? Array.Empty<RmapSpecialWorldPoint>());
            var result=new Dictionary<RmapSpecialWorldPoint,int>(); int component=0;
            foreach(var start in nodes.OrderBy(v=>v))
            {
                if(result.ContainsKey(start)) continue;
                var queue=new Queue<RmapSpecialWorldPoint>(); queue.Enqueue(start); result[start]=component;
                while(queue.Count!=0)
                {
                    var p=queue.Dequeue();
                    foreach(int dx in new[]{-1,1}) foreach(int dy in new[]{-1,0,1})
                    {
                        var next=new RmapSpecialWorldPoint(p.X+dx,p.Y+dy);
                        if(nodes.Contains(next) && !result.ContainsKey(next)){result[next]=component;queue.Enqueue(next);}
                    }
                }
                component++;
            }
            return new ReadOnlyDictionary<RmapSpecialWorldPoint,int>(result);
        }
        private static bool Is(IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5InfillCell> cells,int x,int y,Sv5InfillCellValue value)
            => cells.TryGetValue(new RmapSpecialWorldPoint(x,y),out Sv5InfillCell cell) && cell.Value==value;
        private static string BaseRoom(string owner) => owner.EndsWith("_LINK",StringComparison.Ordinal) ? owner.Substring(0,owner.Length-5) : owner;
        private static int Distance(RmapSpecialWorldPoint a,RmapSpecialWorldPoint b) => Math.Abs(a.X-b.X)+Math.Abs(a.Y-b.Y);
        private static string Pair(string a,string b) => string.Compare(a,b,StringComparison.Ordinal)<=0 ? a+"|"+b : b+"|"+a;
        private static string EndpointPair(RmapSpecialWorldPoint a,RmapSpecialWorldPoint b)
            => a.CompareTo(b)<=0 ? a+"|"+b : b+"|"+a;
        private static string Sector(RmapSpecialWorldPoint p) => "S"+(p.Y/32).ToString("00",CultureInfo.InvariantCulture)+"_"+
            (p.X/48).ToString("00",CultureInfo.InvariantCulture);
        private static void Add(IDictionary<string,int> counts,string key)
        { if(string.IsNullOrEmpty(key)) key="UNSPECIFIED"; counts[key]=counts.TryGetValue(key,out int count) ? count+1 : 1; }
    }
}
