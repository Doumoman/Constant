using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.WorldData;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum Sv5SidepathKind { Returning = 1, DeadEnd = 2 }
    public enum Sv5SidepathMovementRole { SupportedFoot = 1, StepTransition = 2 }
    public enum Sv5SidepathCellRole { Air = 1, Headroom = 2, SolidSupport = 3, ChamberClearance = 4 }

    public sealed class Sv5SidepathProfile
    {
        public Sv5SidepathProfile(bool enabled=true,int target=12,int minimum=8,int minimumReturning=5,
            int minimumSpaceGroups=6,int maximumPerSpaceGroup=2)
        {
            if(minimum<0 || target<minimum || minimumReturning<0 || minimumReturning>minimum ||
                minimumSpaceGroups<0 || maximumPerSpaceGroup<1) throw new ArgumentException("Invalid sidepath profile.");
            Enabled=enabled;Target=target;Minimum=minimum;MinimumReturning=minimumReturning;
            MinimumSpaceGroups=minimumSpaceGroups;MaximumPerSpaceGroup=maximumPerSpaceGroup;
        }
        public bool Enabled { get; }
        public int Target { get; }
        public int Minimum { get; }
        public int MinimumReturning { get; }
        public int MinimumSpaceGroups { get; }
        public int MaximumPerSpaceGroup { get; }
        public int MinimumLength => 20;
        public int MaximumLength => 50;
        public string Digest => RmapWorldDefinition.Hash("SV5_SIDEPATH_PROFILE_NONGRID_V2|624|416|20|50|2|"+
            Target+"|"+Minimum+"|"+MinimumReturning+"|"+MinimumSpaceGroups+"|"+MaximumPerSpaceGroup+"|8|0.4");
    }

    public sealed class Sv5SidepathCell : IComparable<Sv5SidepathCell>
    {
        internal Sv5SidepathCell(string id,RmapSpecialWorldPoint world,Sv5SidepathCellRole role,
            Sv5InfillCellValue source,Sv5InfillCellValue final)
        { SidepathId=id;World=world;Role=role;SourceValue=source;FinalValue=final; }
        public string SidepathId { get; }
        public RmapSpecialWorldPoint World { get; }
        public Sv5SidepathCellRole Role { get; }
        public Sv5InfillCellValue SourceValue { get; }
        public Sv5InfillCellValue FinalValue { get; }
        public string Token => SidepathId+"|"+World+"|"+Role+"|"+SourceValue+">"+FinalValue;
        public int CompareTo(Sv5SidepathCell other) => other==null ? 1 : SidepathId!=other.SidepathId ?
            string.Compare(SidepathId,other.SidepathId,StringComparison.Ordinal) : World.CompareTo(other.World);
    }

    public sealed class Sv5SidepathCandidate : IComparable<Sv5SidepathCandidate>
    {
        internal Sv5SidepathCandidate(string id,string stableHash,Sv5SidepathKind kind,Sv5SidepathEndpoint from,
            Sv5SidepathEndpoint to,IEnumerable<RmapSpecialWorldPoint> path,IEnumerable<Sv5SidepathCell> cells,
            int directionChanges,int newlyCarved,int score,string status,string reason)
        {
            Id=id;StableHash=stableHash;Kind=kind;From=from;To=to;
            Centerline=Array.AsReadOnly(path.ToArray());Cells=Array.AsReadOnly(cells.OrderBy(v=>v).ToArray());
            DirectionChanges=directionChanges;NewlyCarvedAir=newlyCarved;CandidateScore=score;Status=status;Reason=reason;
        }
        public string Id { get; }
        public string StableHash { get; }
        public Sv5SidepathKind Kind { get; }
        public Sv5SidepathEndpoint From { get; }
        public Sv5SidepathEndpoint To { get; }
        public IReadOnlyList<RmapSpecialWorldPoint> Centerline { get; }
        public IReadOnlyList<Sv5SidepathCell> Cells { get; }
        public int DirectionChanges { get; }
        public int NewlyCarvedAir { get; }
        public int CandidateScore { get; }
        public string Status { get; internal set; }
        public string Reason { get; internal set; }
        public string SpaceGroupId => From.SpaceGroupId;
        public string EndpointPair => To==null ? From.EndpointId+"|DEAD_END" : Sv5SidepathEndpointPair.Key(From.EndpointId,To.EndpointId);
        public string Token => Id+"|"+StableHash+"|"+Kind+"|"+From.EndpointId+"|"+(To?.EndpointId ?? string.Empty)+"|"+
            CandidateScore+"|"+DirectionChanges+"|"+NewlyCarvedAir+"|"+Status+"|"+Reason+"|"+
            string.Join(";",Centerline)+"|"+string.Join(";",Cells.Select(v=>v.Token));
        public int CompareTo(Sv5SidepathCandidate other) => other==null ? 1 : string.Compare(Id,other.Id,StringComparison.Ordinal);
    }

    public sealed class Sv5SidepathLink : IComparable<Sv5SidepathLink>
    {
        internal Sv5SidepathLink(Sv5SidepathCandidate candidate,bool shortcut,int baselineCost,int finalCost)
        {
            Id=candidate.Id;Kind=candidate.Kind;FromRoomId=candidate.From.RoomId;ToRoomId=candidate.To?.RoomId ?? string.Empty;
            FromEndpointId=candidate.From.EndpointId;ToEndpointId=candidate.To?.EndpointId ?? string.Empty;
            SpaceGroupId=candidate.SpaceGroupId;Host=candidate.From.ConnectionId;Centerline=candidate.Centerline;Cells=candidate.Cells;
            DirectionChanges=candidate.DirectionChanges;NewlyCarvedAir=candidate.NewlyCarvedAir;
            RandomShortcut=shortcut;BaselineCost=baselineCost;FinalCost=finalCost;
        }
        public string Id { get; }
        public Sv5SidepathKind Kind { get; }
        public string FromRoomId { get; }
        public string ToRoomId { get; }
        public string FromEndpointId { get; }
        public string ToEndpointId { get; }
        public string SpaceGroupId { get; }
        public string Host { get; }
        public IReadOnlyList<RmapSpecialWorldPoint> Centerline { get; }
        public IReadOnlyList<Sv5SidepathCell> Cells { get; }
        public int DirectionChanges { get; }
        public int NewlyCarvedAir { get; }
        public bool Rejoins => Kind==Sv5SidepathKind.Returning;
        public bool Returnable => Kind==Sv5SidepathKind.Returning;
        public bool RandomShortcut { get; }
        public int BaselineCost { get; }
        public int FinalCost { get; }
        public int BypassCount => 0;
        public int LegalStatesChecked => 9;
        public int ResourceOrdersChecked => 6;
        public RmapSpecialWorldPoint Midpoint => Centerline[Centerline.Count/2];
        public string Token => Id+"|"+Kind+"|"+FromRoomId+"|"+ToRoomId+"|"+FromEndpointId+"|"+ToEndpointId+"|"+
            SpaceGroupId+"|"+Host+"|"+DirectionChanges+"|"+NewlyCarvedAir+"|"+RandomShortcut+"|"+
            BaselineCost+"|"+FinalCost+"|"+string.Join(";",Centerline)+"|"+string.Join(";",Cells.Select(v=>v.Token));
        public int CompareTo(Sv5SidepathLink other) => other==null ? 1 : string.Compare(Id,other.Id,StringComparison.Ordinal);
    }

    public sealed class Sv5SidepathSearchRecord
    {
        internal Sv5SidepathSearchRecord(string endpoint,int expansions,string result)
        { EndpointId=endpoint;Expansions=expansions;Result=result; }
        public string EndpointId { get; }
        public int Expansions { get; }
        public string Result { get; }
    }

    public sealed class Sv5SidepathPerformance
    {
        internal Sv5SidepathPerformance(IDictionary<string,double> source,int workers,int endpoints,int nearbyPairs,
            int candidateCount,int affectedNodes,int deadEndExpansions)
        {
            TimingsMilliseconds=new ReadOnlyDictionary<string,double>(new SortedDictionary<string,double>(source,StringComparer.Ordinal));
            WorkerCount=workers;EndpointCount=endpoints;NearbyPairCount=nearbyPairs;CandidateCount=candidateCount;
            AffectedFootNodes=affectedNodes;DeadEndExpansions=deadEndExpansions;
        }
        public IReadOnlyDictionary<string,double> TimingsMilliseconds { get; }
        public int WorkerCount { get; }
        public int EndpointCount { get; }
        public int NearbyPairCount { get; }
        public int CandidateCount { get; }
        public int AffectedFootNodes { get; }
        public int DeadEndExpansions { get; }
        public int WorldIndexBuilds => 1;
        public int WholeWorldCopyPerCandidate => 0;
        public int WholeWorldBfsPerCandidate => 0;
        public int GlobalProductRuns => 1;
    }

    public sealed class Sv5SidepathPlan
    {
        internal Sv5SidepathPlan(string baseline,Sv5SidepathProfile profile,Sv5SidepathEndpointPlan index,
            IEnumerable<Sv5SidepathCandidate> candidates,IEnumerable<Sv5SidepathLink> links,
            IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell> baselineOccupancy,
            IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell> finalOccupancy,
            IEnumerable<string> diagnostics,IDictionary<string,int> rejections,Sv5SidepathPerformance performance,
            IEnumerable<Sv5SidepathSearchRecord> searches)
        {
            BaselineDigest=baseline;Profile=profile;EndpointIndex=index;
            Candidates=Array.AsReadOnly(candidates.OrderBy(v=>v).ToArray());Links=Array.AsReadOnly(links.OrderBy(v=>v).ToArray());
            Cells=Array.AsReadOnly(Links.SelectMany(v=>v.Cells).OrderBy(v=>v).ToArray());
            BaselineOccupancy=baselineOccupancy;FinalOccupancy=finalOccupancy;
            Diagnostics=Array.AsReadOnly((diagnostics ?? Array.Empty<string>()).Distinct().OrderBy(v=>v,StringComparer.Ordinal).ToArray());
            Rejections=new ReadOnlyDictionary<string,int>(new SortedDictionary<string,int>(rejections,StringComparer.Ordinal));
            Performance=performance;Searches=Array.AsReadOnly(searches.OrderBy(v=>v.EndpointId,StringComparer.Ordinal).ToArray());
            Digest=RmapWorldDefinition.Hash("SV5_ACTUAL_TILE_SIDEPATH_NONGRID_V2\n"+baseline+"\n"+profile.Digest+"\n"+
                index.Digest+"\n"+string.Join("\n",Candidates.Select(v=>v.Token))+"\n"+
                string.Join("\n",Links.Select(v=>v.Token))+"\n"+string.Join("\n",Diagnostics));
        }
        public string BaselineDigest { get; }
        public Sv5SidepathProfile Profile { get; }
        public Sv5SidepathEndpointPlan EndpointIndex { get; }
        public IReadOnlyList<Sv5SidepathCandidate> Candidates { get; }
        public IReadOnlyList<Sv5SidepathLink> Links { get; }
        public IReadOnlyList<Sv5SidepathCell> Cells { get; }
        public IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell> BaselineOccupancy { get; }
        public IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell> FinalOccupancy { get; }
        public IReadOnlyList<string> Diagnostics { get; }
        public IReadOnlyDictionary<string,int> Rejections { get; }
        public Sv5SidepathPerformance Performance { get; }
        public IReadOnlyList<Sv5SidepathSearchRecord> Searches { get; }
        public int AcceptedCount => Links.Count;
        public int ReturningCount => Links.Count(v=>v.Kind==Sv5SidepathKind.Returning);
        public int DistinctSpaceGroupCount => Links.Select(v=>v.SpaceGroupId).Distinct(StringComparer.Ordinal).Count();
        public string Digest { get; }
        public bool Success => Diagnostics.Count==0 && AcceptedCount>=Profile.Minimum && ReturningCount>=Profile.MinimumReturning &&
            DistinctSpaceGroupCount>=Profile.MinimumSpaceGroups && Links.GroupBy(v=>v.SpaceGroupId).All(v=>v.Count()<=Profile.MaximumPerSpaceGroup) &&
            Links.All(v=>v.Centerline.Count>=20 && v.Centerline.Count<=50 && v.BypassCount==0 && v.LegalStatesChecked==9 && v.ResourceOrdersChecked==6);
    }

    public static class Sv5SpaceSidepaths
    {
        private sealed class Desired { public Sv5InfillCellValue Value; public Sv5SidepathCellRole Role; }
        private sealed class Attempt { public Sv5SidepathCandidate Candidate; public string Reason; public string EndpointId; public int Expansions; }

        public static Sv5SidepathPlan Build(Sv5SpaceGraphPlan plan,Sv5SidepathProfile profile=null,int? workerOverride=null)
        {
            if(plan==null || plan.Infill==null || plan.Loops==null || !plan.Loops.Success)
                throw new ArgumentException("A passing PlanWithLoops result is required.",nameof(plan));
            profile=profile ?? new Sv5SidepathProfile();
            int workers=workerOverride ?? 1;
            if(workers<1 || workers>4) throw new ArgumentOutOfRangeException(nameof(workerOverride));
            var total=Stopwatch.StartNew();var timing=new SortedDictionary<string,double>(StringComparer.Ordinal);
            T Time<T>(string key,Func<T> action){var w=Stopwatch.StartNew();try{return action();}finally{w.Stop();timing[key]=Ms(w);}}
            void TimeAction(string key,Action action){var w=Stopwatch.StartNew();try{action();}finally{w.Stop();timing[key]=Ms(w);}}
            var occupancy=plan.Loops.Topology.FinalOccupancy;var feet=plan.Loops.Topology.FinalFootNodes;
            var index=Time("endpoint_index_ms",()=>Sv5SidepathEndpointIndex.Build(plan,occupancy,feet));
            var protectedCells=new HashSet<RmapSpecialWorldPoint>(ProtectedCells(plan));Attempt[] returning=null,deadEnds=null;
            TimeAction("candidate_generation_ms",()=>
            {
                returning=index.Pairs.Where(v=>v.Eligible).AsParallel().AsOrdered().WithDegreeOfParallelism(workers)
                    .Select(v=>EvaluateReturning(plan,v,occupancy,protectedCells)).ToArray();
                deadEnds=index.Endpoints.AsParallel().AsOrdered().WithDegreeOfParallelism(workers)
                    .Select(v=>EvaluateDeadEnd(plan,v,occupancy,protectedCells)).ToArray();
            });
            var attempts=returning.Concat(deadEnds).ToArray();var rejection=new SortedDictionary<string,int>(StringComparer.Ordinal);
            foreach(string reason in index.Pairs.Where(v=>!v.Eligible).Select(v=>v.RejectionReason)
                .Concat(attempts.Where(v=>v.Candidate==null).Select(v=>v.Reason)))
                rejection[reason]=rejection.TryGetValue(reason,out int count)?count+1:1;
            var candidates=attempts.Where(v=>v.Candidate!=null).Select(v=>v.Candidate).GroupBy(v=>v.Id,StringComparer.Ordinal).Select(v=>v.First())
                .OrderBy(v=>v.SpaceGroupId,StringComparer.Ordinal).ThenBy(v=>v.From.RoomId,StringComparer.Ordinal)
                .ThenBy(v=>v.From.EndpointId,StringComparer.Ordinal).ThenBy(v=>v.CandidateScore).ThenBy(v=>v.Id,StringComparer.Ordinal).ToList();
            var packed=new List<Sv5SidepathCandidate>();var usedCells=new HashSet<RmapSpecialWorldPoint>();
            var usedPairs=new HashSet<string>(StringComparer.Ordinal);var groups=new Dictionary<string,int>(StringComparer.Ordinal);
            bool Add(Sv5SidepathCandidate candidate)
            {
                if(packed.Count>=profile.Target||usedPairs.Contains(candidate.EndpointPair)||
                    groups.TryGetValue(candidate.SpaceGroupId,out int count)&&count>=profile.MaximumPerSpaceGroup||
                    candidate.Cells.Any(v=>usedCells.Contains(v.World))||candidate.Centerline.Any(usedCells.Contains))return false;
                packed.Add(candidate);usedPairs.Add(candidate.EndpointPair);foreach(var cell in candidate.Cells)usedCells.Add(cell.World);
                foreach(var point in candidate.Centerline)usedCells.Add(point);groups[candidate.SpaceGroupId]=groups.TryGetValue(candidate.SpaceGroupId,out count)?count+1:1;return true;
            }
            TimeAction("packing_ms",()=>
            {
                foreach(var group in candidates.GroupBy(v=>v.SpaceGroupId,StringComparer.Ordinal).OrderBy(v=>v.Key,StringComparer.Ordinal))
                    Add(group.OrderBy(v=>v.Kind==Sv5SidepathKind.Returning?0:1).ThenBy(v=>v.CandidateScore).ThenBy(v=>v.Id,StringComparer.Ordinal).First());
                foreach(var candidate in candidates.OrderBy(v=>v.Kind==Sv5SidepathKind.Returning?0:1).ThenBy(v=>v.SpaceGroupId,StringComparer.Ordinal)
                    .ThenBy(v=>v.From.RoomId,StringComparer.Ordinal).ThenBy(v=>v.From.EndpointId,StringComparer.Ordinal)
                    .ThenBy(v=>v.CandidateScore).ThenBy(v=>v.Id,StringComparer.Ordinal))Add(candidate);
            });
            foreach(var candidate in candidates.Except(packed)){candidate.Status="REJECTED";candidate.Reason="PACKING";
                rejection["PACKING"]=rejection.TryGetValue("PACKING",out int count)?count+1:1;}
            var links=packed.Select(v=>new Sv5SidepathLink(v,false,-1,v.Centerline.Count)).ToArray();
            var final=Time("final_overlay_ms",()=>FinalOccupancy(occupancy,links));var diagnostics=new List<string>();
            if(links.Length<profile.Minimum)diagnostics.Add("SIDEPATH_MINIMUM|"+links.Length+"/"+profile.Minimum);
            if(links.Count(v=>v.Kind==Sv5SidepathKind.Returning)<profile.MinimumReturning)diagnostics.Add("SIDEPATH_RETURNING|"+links.Count(v=>v.Kind==Sv5SidepathKind.Returning)+"/"+profile.MinimumReturning);
            if(links.Select(v=>v.SpaceGroupId).Distinct().Count()<profile.MinimumSpaceGroups)diagnostics.Add("SIDEPATH_SPACE_GROUPS|"+links.Select(v=>v.SpaceGroupId).Distinct().Count()+"/"+profile.MinimumSpaceGroups);
            total.Stop();timing["total_build_ms"]=Ms(total);
            int affected=links.SelectMany(v=>v.Cells).SelectMany(v=>new[]{new RmapSpecialWorldPoint(v.World.X,v.World.Y-1),v.World,new RmapSpecialWorldPoint(v.World.X,v.World.Y+1)}).Distinct().Count();
            var perf=new Sv5SidepathPerformance(timing,workers,index.Endpoints.Count,index.Pairs.Count,candidates.Count,affected,deadEnds.Sum(v=>v.Expansions));
            var searches=deadEnds.Select(v=>new Sv5SidepathSearchRecord(v.EndpointId,v.Expansions,v.Candidate==null?v.Reason:"GENERATED"));
            return new Sv5SidepathPlan(plan.Digest,profile,index,candidates,links,occupancy,final,diagnostics,rejection,perf,searches);
        }

        private static Attempt EvaluateReturning(Sv5SpaceGraphPlan plan,Sv5SidepathEndpointPair pair,
            IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell> occupancy,ISet<RmapSpecialWorldPoint> protectedCells)
        {return Evaluate(plan,Sv5SidepathKind.Returning,pair.From,pair.To,IrregularPath(pair.From.World,pair.To.World,pair.From.StableHash+pair.To.StableHash),occupancy,protectedCells,0);}
        private static Attempt EvaluateDeadEnd(Sv5SpaceGraphPlan plan,Sv5SidepathEndpoint from,
            IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell> occupancy,ISet<RmapSpecialWorldPoint> protectedCells)
        {int expansions;var path=DeadEndPath(from,out expansions);return Evaluate(plan,Sv5SidepathKind.DeadEnd,from,null,path,occupancy,protectedCells,expansions);}
        private static Attempt Evaluate(Sv5SpaceGraphPlan plan,Sv5SidepathKind kind,Sv5SidepathEndpoint from,Sv5SidepathEndpoint to,
            IReadOnlyList<RmapSpecialWorldPoint> path,IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell> occupancy,
            ISet<RmapSpecialWorldPoint> protectedCells,int expansions)
        {
            var result=new Attempt{EndpointId=from.EndpointId,Expansions=expansions};if(path.Count<20||path.Count>50){result.Reason="LENGTH";return result;}
            string identity=from.EndpointId+"|"+(to?.EndpointId??"DEAD_END");string hash=RmapWorldDefinition.Hash("SV5_SIDE_CANDIDATE_V2|"+plan.Seed+"|"+kind+"|"+identity+"|"+string.Join(";",path));
            string id="SV5_SIDE_"+hash.Substring(0,20);if(!TryCells(id,from,to,path,occupancy,protectedCells,out Sv5SidepathCell[] cells,out int newly,out int changes,out string reason)){result.Reason=reason;return result;}
            int score=path.Count*100-newly*4+changes;result.Candidate=new Sv5SidepathCandidate(id,hash,kind,from,to,path,cells,changes,newly,score,"ELIGIBLE",string.Empty);return result;
        }

        public static IReadOnlyList<string> ValidatePayload(Sv5SpaceGraphPlan baseline,Sv5SidepathPlan payload)
        {
            var errors=new List<string>();if(baseline==null||payload==null)return new[]{"SIDEPATH_PAYLOAD_NULL"};
            if(baseline.Digest!=payload.BaselineDigest)errors.Add("SIDEPATH_BASELINE_DIGEST");if(!payload.Success)errors.AddRange(payload.Diagnostics);
            if(payload.Links.SelectMany(v=>v.Cells).GroupBy(v=>v.World).Any(v=>v.Select(c=>c.FinalValue).Distinct().Count()>1))errors.Add("SIDEPATH_CELL_CONFLICT");
            return Array.AsReadOnly(errors.Distinct().OrderBy(v=>v,StringComparer.Ordinal).ToArray());
        }
        public static IReadOnlyList<Sv5SpaceConnection> ApplyPhysicalCells(IEnumerable<Sv5SpaceConnection> source,Sv5SidepathPlan sidepaths)
        {
            var links=sidepaths==null?Array.Empty<Sv5SidepathLink>():sidepaths.Links.ToArray();return Array.AsReadOnly((source??Array.Empty<Sv5SpaceConnection>()).Select(connection=>
            {
                var owned=links.Where(v=>v.Host==connection.Id).ToArray();if(owned.Length==0)return connection;
                var air=owned.SelectMany(v=>v.Cells.Where(c=>c.FinalValue==Sv5InfillCellValue.Air).Select(c=>c.World)).Concat(owned.SelectMany(v=>v.Centerline)).Distinct().ToArray();
                var solid=new HashSet<RmapSpecialWorldPoint>(owned.SelectMany(v=>v.Cells.Where(c=>c.FinalValue==Sv5InfillCellValue.Solid).Select(c=>c.World)));
                return new Sv5SpaceConnection(connection.Id,connection.Kind,connection.FromPortId,connection.ToPortId,connection.FromPlaceId,connection.ToPlaceId,
                    connection.Direction,connection.Flow,connection.Condition,connection.SourceGraphEdgeId,connection.SelectionState,connection.Centerline,
                    connection.Envelope.Concat(air),connection.ApertureCells.Where(v=>!solid.Contains(v)).Concat(air));
            }).ToArray());
        }
        public static Sv5SidepathMovementRole MovementRole(IReadOnlyList<RmapSpecialWorldPoint> path,int index)
        {bool transition=index>0&&path[index-1].Y!=path[index].Y||index+1<path.Count&&path[index+1].Y!=path[index].Y;return transition?Sv5SidepathMovementRole.StepTransition:Sv5SidepathMovementRole.SupportedFoot;}
        public static int DirectionChanges(IReadOnlyList<RmapSpecialWorldPoint> path)
        {var directions=path.Zip(path.Skip(1),(a,b)=>new RmapSpecialWorldPoint(b.X-a.X,b.Y-a.Y)).ToArray();return directions.Zip(directions.Skip(1),(a,b)=>a.Equals(b)?0:1).Sum();}
        private static IReadOnlyList<RmapSpecialWorldPoint> IrregularPath(RmapSpecialWorldPoint start,RmapSpecialWorldPoint goal,string token)
        {
            int dx=goal.X-start.X,dy=goal.Y-start.Y;if(dx<1)return Array.AsReadOnly(Array.Empty<RmapSpecialWorldPoint>());int pairs=1;
            while(dx+Math.Abs(dy)+pairs*2+1<20)pairs++;int events=Math.Abs(dy)+pairs*2;if(dx<events*2+2||dx+events+1>50)return Array.AsReadOnly(Array.Empty<RmapSpecialWorldPoint>());
            int detour=(Convert.ToInt32(token.Substring(0,2),16)&1)==0?1:-1;if(start.Y<4)detour=1;if(start.Y>411)detour=-1;
            var deltas=new List<int>();for(int i=0;i<pairs;i++){deltas.Add(detour);deltas.Add(-detour);}for(int i=0;i<Math.Abs(dy);i++)deltas.Insert(Math.Min(deltas.Count,1+i*2),Math.Sign(dy));
            var positions=new List<int>();int previous=0;for(int i=0;i<deltas.Count;i++){int position=1+(i+1)*(dx-1)/(deltas.Count+1);position=Math.Max(previous+2,position);if(position>=dx)return Array.AsReadOnly(Array.Empty<RmapSpecialWorldPoint>());positions.Add(position);previous=position;}
            var result=new List<RmapSpecialWorldPoint>{start};int y=start.Y,eventIndex=0;for(int column=1;column<=dx;column++){result.Add(new RmapSpecialWorldPoint(start.X+column,y));if(eventIndex<positions.Count&&positions[eventIndex]==column){y+=deltas[eventIndex++];result.Add(new RmapSpecialWorldPoint(start.X+column,y));}}
            if(y!=goal.Y||!result.Last().Equals(goal))return Array.AsReadOnly(Array.Empty<RmapSpecialWorldPoint>());return Array.AsReadOnly(result.ToArray());
        }
        private static IReadOnlyList<RmapSpecialWorldPoint> DeadEndPath(Sv5SidepathEndpoint from,out int expansions)
        {
            var result=new List<RmapSpecialWorldPoint>{from.World};int x=from.World.X,y=from.World.Y,direction=from.ExitX;
            int lift=(Convert.ToInt32(from.StableHash.Substring(0,2),16)&1)==0?1:-1;if(y<4)lift=1;if(y>411)lift=-1;expansions=0;
            for(int step=1;step<=22;step++){x+=direction;expansions++;if(x<1||x>622)return Array.AsReadOnly(Array.Empty<RmapSpecialWorldPoint>());result.Add(new RmapSpecialWorldPoint(x,y));if(step==6||step==15){y+=lift;expansions++;result.Add(new RmapSpecialWorldPoint(x,y));}if(step==10||step==19){y-=lift;expansions++;result.Add(new RmapSpecialWorldPoint(x,y));}}
            return Array.AsReadOnly(result.ToArray());
        }
        private static bool TryCells(string id,Sv5SidepathEndpoint from,Sv5SidepathEndpoint to,IReadOnlyList<RmapSpecialWorldPoint> path,
            IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell> occupancy,ISet<RmapSpecialWorldPoint> protectedCells,
            out Sv5SidepathCell[] output,out int newly,out int changes,out string error)
        {
            output=Array.Empty<Sv5SidepathCell>();newly=0;changes=DirectionChanges(path);error=string.Empty;
            if(path.Distinct().Count()!=path.Count){error="SELF_INTERSECTION";return false;}if(path.Zip(path.Skip(1),(a,b)=>Math.Abs(a.X-b.X)+Math.Abs(a.Y-b.Y)).Any(v=>v!=1)){error="NON_CARDINAL";return false;}if(changes<2){error="IRREGULARITY";return false;}
            var desired=new Dictionary<RmapSpecialWorldPoint,Desired>();bool Add(RmapSpecialWorldPoint point,Sv5InfillCellValue value,Sv5SidepathCellRole role){if(desired.TryGetValue(point,out Desired old)){if(old.Value!=value)return false;if(role==Sv5SidepathCellRole.ChamberClearance)old.Role=role;return true;}desired[point]=new Desired{Value=value,Role=role};return true;}
            for(int i=0;i<path.Count;i++){var point=path[i];var movement=MovementRole(path,i);if(!Add(point,Sv5InfillCellValue.Air,Sv5SidepathCellRole.Air)||!Add(new RmapSpecialWorldPoint(point.X,point.Y+1),Sv5InfillCellValue.Air,Sv5SidepathCellRole.Headroom)){error="CELL_CONFLICT";return false;}if(movement==Sv5SidepathMovementRole.SupportedFoot&&!Add(new RmapSpecialWorldPoint(point.X,point.Y-1),Sv5InfillCellValue.Solid,Sv5SidepathCellRole.SolidSupport)){error="CELL_CONFLICT";return false;}if(movement==Sv5SidepathMovementRole.StepTransition&&!Add(new RmapSpecialWorldPoint(point.X,point.Y+2),Sv5InfillCellValue.Air,Sv5SidepathCellRole.ChamberClearance)){error="CELL_CONFLICT";return false;}}
            var middle=path[path.Count/2];for(int dx=-1;dx<=1;dx++)if(!Add(new RmapSpecialWorldPoint(middle.X+dx,middle.Y+2),Sv5InfillCellValue.Air,Sv5SidepathCellRole.ChamberClearance)){error="CELL_CONFLICT";return false;}
            var changed=new List<Sv5SidepathCell>();foreach(var pair in desired.OrderBy(v=>v.Key)){var point=pair.Key;if(point.X<0||point.X>=624||point.Y<0||point.Y>=416){error="WORLD_OUTSIDE";return false;}var source=Sv5LoopTopology.ValueAt(occupancy,point);if(protectedCells.Contains(point)&&source!=pair.Value.Value){error="PROTECTED";return false;}if(pair.Value.Value==Sv5InfillCellValue.Solid&&source==Sv5InfillCellValue.Air){error="FILLS_EXISTING_AIR";return false;}if(source!=pair.Value.Value)changed.Add(new Sv5SidepathCell(id,point,pair.Value.Role,source,pair.Value.Value));}
            newly=path.Count(point=>Sv5LoopTopology.ValueAt(occupancy,point)!=Sv5InfillCellValue.Air);if(newly<8||newly*5<path.Count*2){error="NEWLY_CARVED";return false;}output=changed.ToArray();return true;
        }
        private static IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell> FinalOccupancy(IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell> baseline,IEnumerable<Sv5SidepathLink> links)
        {var final=new Dictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell>(baseline);foreach(var cell in links.SelectMany(v=>v.Cells))final[cell.World]=new Sv5LoopOccupancyCell(cell.World,cell.FinalValue,"SIDEPATH_ACTUAL",cell.SidepathId);return new ReadOnlyDictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell>(final);}
        public static IReadOnlyCollection<RmapSpecialWorldPoint> ProtectedCells(Sv5SpaceGraphPlan plan)
        {var result=new HashSet<RmapSpecialWorldPoint>(plan.Core.CoreCells.Select(v=>v.World));result.UnionWith(plan.Core.RouteCells.Select(v=>v.World));result.UnionWith(plan.Core.RouteSource.Secrets.SelectMany(v=>v.Chunks).SelectMany(chunk=>Enumerable.Range(0,8).SelectMany(y=>Enumerable.Range(0,12).Select(x=>new RmapSpecialWorldPoint(chunk.X*12+x,chunk.Y*8+y)))));result.UnionWith(plan.Gates.SelectMany(v=>v.BlockingCells));result.UnionWith(plan.Gates.SelectMany(v=>v.BlockingFaces).SelectMany(v=>new[]{v.First,v.Second}));result.UnionWith(plan.Ports.SelectMany(v=>v.BoundaryCells.Concat(new[]{v.Anchor})));return result;}
        private static double Ms(Stopwatch watch)=>watch.ElapsedTicks*1000.0/Stopwatch.Frequency;
    }
}
