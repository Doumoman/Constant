using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
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

        internal Sv5LoopCandidate(string id, string hash, ulong rank, string from, string to, string host,
            string sector, IEnumerable<RmapSpecialWorldPoint> sourceFeet,
            IEnumerable<RmapSpecialWorldPoint> sourceCenterline, IEnumerable<Sv5LoopCell> sourceCells,
            int baselineCost, string status, string reason)
        {
            Id=id; StableHash=hash; StableRank=rank; FromOwner=from; ToOwner=to; Host=host; SectorId=sector;
            footPath=Array.AsReadOnly(sourceFeet.ToArray()); centerline=Array.AsReadOnly(sourceCenterline.ToArray());
            cells=Array.AsReadOnly(sourceCells.OrderBy(c=>c).ToArray()); BaselineCost=baselineCost;
            Status=status; Reason=reason;
        }
        public string Id { get; }
        public string StableHash { get; }
        public ulong StableRank { get; }
        public string FromOwner { get; }
        public string ToOwner { get; }
        public string Host { get; }
        public string SectorId { get; }
        public IReadOnlyList<RmapSpecialWorldPoint> FootPath => footPath;
        public IReadOnlyList<RmapSpecialWorldPoint> Centerline => centerline;
        public IReadOnlyList<Sv5LoopCell> Cells => cells;
        public int BaselineCost { get; }
        public int NewCost => centerline.Count;
        public bool Eligible { get; internal set; }
        public string Status { get; internal set; }
        public string Reason { get; internal set; }
        public string PairKey => string.Compare(FromOwner,ToOwner,StringComparison.Ordinal) <= 0 ?
            FromOwner+"|"+ToOwner : ToOwner+"|"+FromOwner;
        public string PathToken => string.Join(";",centerline);
        public string Token => Id+"|"+StableHash+"|"+StableRank+"|"+FromOwner+"|"+ToOwner+"|"+Host+"|"+
            SectorId+"|"+BaselineCost+"|"+NewCost+"|"+Status+"|"+Reason+"|"+string.Join(";",footPath)+"|"+PathToken;
        public int CompareTo(Sv5LoopCandidate other)
        {
            if(other==null) return 1;
            int rank=StableRank.CompareTo(other.StableRank);
            return rank!=0 ? rank : string.Compare(Id,other.Id,StringComparison.Ordinal);
        }
    }

    public sealed class Sv5LoopLink : IComparable<Sv5LoopLink>
    {
        private readonly ReadOnlyCollection<RmapSpecialWorldPoint> feet;
        private readonly ReadOnlyCollection<RmapSpecialWorldPoint> centerline;
        private readonly ReadOnlyCollection<Sv5LoopCell> cells;
        internal Sv5LoopLink(string id, Sv5LoopKind kind, string from, string to, string host, string sector,
            IEnumerable<RmapSpecialWorldPoint> sourceFeet, IEnumerable<RmapSpecialWorldPoint> sourceCenterline,
            IEnumerable<Sv5LoopCell> sourceCells, int baselineCost, int newCost, string contourVariant,
            bool alternatePathExists, int cycleDelta, int bypassCount, int resourceOrdersChecked,
            int legalStatesChecked)
        {
            Id=id; Kind=kind; FromOwner=from; ToOwner=to; Host=host; SectorId=sector;
            feet=Array.AsReadOnly(sourceFeet.ToArray()); centerline=Array.AsReadOnly(sourceCenterline.ToArray());
            cells=Array.AsReadOnly(sourceCells.OrderBy(c=>c).ToArray()); BaselineCost=baselineCost; NewCost=newCost;
            ContourVariant=contourVariant; AlternatePathExists=alternatePathExists; CycleDelta=cycleDelta;
            BypassCount=bypassCount; ResourceOrdersChecked=resourceOrdersChecked; LegalStatesChecked=legalStatesChecked;
        }
        public string Id { get; }
        public Sv5LoopKind Kind { get; }
        public string FromOwner { get; }
        public string ToOwner { get; }
        public string Host { get; }
        public string SectorId { get; }
        public IReadOnlyList<RmapSpecialWorldPoint> FootPath => feet;
        public IReadOnlyList<RmapSpecialWorldPoint> Centerline => centerline;
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
        public string Token => Id+"|"+Kind+"|"+FromOwner+"|"+ToOwner+"|"+Host+"|"+SectorId+"|"+
            BaselineCost+"|"+NewCost+"|"+ContourVariant+"|"+AlternatePathExists+"|"+CycleDelta+"|"+
            BypassCount+"|"+ResourceOrdersChecked+"|"+LegalStatesChecked+"|"+string.Join(";",feet)+"|"+
            string.Join(";",centerline)+"|"+string.Join(";",cells.Select(c=>c.Token));
        public int CompareTo(Sv5LoopLink other) => other==null ? 1 : string.Compare(Id,other.Id,StringComparison.Ordinal);
    }

    public sealed class Sv5LoopPlan
    {
        internal Sv5LoopPlan(string baseline, Sv5LoopProfile profile, IEnumerable<Sv5LoopCandidate> candidates,
            IEnumerable<Sv5LoopLink> links, IEnumerable<string> diagnostics, IDictionary<string,int> rejections,
            int baselineCycleRank, int baselineBridgeCount)
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
            BaselineCycleRank=baselineCycleRank; FinalCycleRank=baselineCycleRank+Links.Count;
            BaselineBridgeCount=baselineBridgeCount; FinalBridgeCount=Math.Max(0,baselineBridgeCount-Links.Count);
            Digest=RmapWorldDefinition.Hash("SV5_ACTUAL_TILE_LOOPS_V1\n"+baseline+"\n"+profile.Digest+"\n"+
                string.Join("\n",Candidates.Select(c=>c.Token))+"\n"+string.Join("\n",Links.Select(l=>l.Token))+"\n"+
                string.Join("\n",Instances.Select(i=>i.Token))+"\n"+string.Join("\n",Diagnostics));
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
        public int BaselineCycleRank { get; }
        public int FinalCycleRank { get; }
        public int BaselineBridgeCount { get; }
        public int FinalBridgeCount { get; }
        public int AcceptedCount => Links.Count;
        public int DistinctSectorCount => Links.Select(l=>l.SectorId).Distinct(StringComparer.Ordinal).Count();
        public string Digest { get; }
        public bool Success => Diagnostics.Count==0 && AcceptedCount>=Profile.Minimum && AcceptedCount<=Profile.Maximum &&
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

        public static Sv5LoopPlan Build(Sv5SpaceGraphPlan plan, Sv5LoopProfile profile = null)
        {
            if(plan==null) throw new ArgumentNullException(nameof(plan));
            if(plan.Infill==null || !plan.Infill.Success) throw new ArgumentException("A passing PlanWithInfill result is required.",nameof(plan));
            profile=profile ?? new Sv5LoopProfile();
            var actual=plan.Infill.Cells.ToDictionary(c=>c.World,c=>c);
            var rooms=plan.Infill.Rooms.ToDictionary(r=>r.Id,r=>r,StringComparer.Ordinal);
            var forbidden=Forbidden(plan);
            var reservations=plan.Reservations.GroupBy(r=>r.World).ToDictionary(g=>g.Key,g=>g.ToArray());
            var baselineAir=new HashSet<RmapSpecialWorldPoint>(plan.Connections.SelectMany(c=>c.Centerline.Concat(c.ApertureCells)));
            var endpoints=actual.Values.Where(c=>c.Owner.StartsWith("SV5_INFILL_ROOM_",StringComparison.Ordinal) &&
                    c.Value==Sv5InfillCellValue.Air && Is(actual,c.World.X,c.World.Y+1,Sv5InfillCellValue.Air) &&
                    Is(actual,c.World.X,c.World.Y-1,Sv5InfillCellValue.Solid))
                .GroupBy(c=>c.Owner,StringComparer.Ordinal).ToDictionary(g=>g.Key,g=>g.Select(c=>c.World).OrderBy(p=>p).ToArray(),StringComparer.Ordinal);
            var generated=new List<Sv5LoopCandidate>();
            var searchRejections=new Dictionary<string,int>(StringComparer.Ordinal);
            var pathTokens=new HashSet<string>(StringComparer.Ordinal);
            string[] owners=endpoints.Keys.Where(o=>rooms.ContainsKey(BaseRoom(o))).OrderBy(o=>o,StringComparer.Ordinal).ToArray();
            for(int ai=0;ai<owners.Length;ai++)
            for(int bi=ai+1;bi<owners.Length;bi++)
            {
                string from=owners[ai],to=owners[bi],fromRoom=BaseRoom(from),toRoom=BaseRoom(to);
                string pairReason=PairExclusionReason(rooms[fromRoom],rooms[toRoom],from,to);
                if(pairReason.Length!=0 || rooms[fromRoom].Host!=rooms[toRoom].Host) continue;
                foreach(var start in endpoints[from]) foreach(var goal in endpoints[to])
                {
                    if(Distance(start,goal)+1>profile.MaximumLength) continue;
                    for(int variant=0;variant<2;variant++)
                    {
                        var feet=SupportedPath(start,goal,variant==1);
                        if(feet.Count==0) continue;
                        var center=Sv5SpaceInfill.CardinalCenterline(feet);
                        var geometryErrors=ValidateSupportedPath(feet,profile.MaximumLength);
                        if(geometryErrors.Count!=0 || center.Count<profile.MinimumLength) continue;
                        string pathToken=string.Join(";",center);
                        if(!pathTokens.Add(from+"|"+to+"|"+pathToken)) continue;
                        if(!TryCells(string.Empty,from,to,rooms[fromRoom].Host,feet,center,actual,forbidden,reservations,
                            out Sv5LoopCell[] cells,out string cellError))
                        { Add(searchRejections,(cellError ?? "GEOMETRY_REJECT").Split('|')[0]); continue; }
                        int baselineCost=ShortestCost(start,goal,baselineAir);
                        if(baselineCost<=0) continue;
                        string hash=RmapWorldDefinition.Hash("SV5_LOOP_CANDIDATE_V1|"+plan.Seed+"|"+
                            from+"|"+to+"|"+pathToken+"|ELIGIBILITY_SALT_9");
                        ulong rank=ulong.Parse(hash.Substring(0,16),NumberStyles.HexNumber,CultureInfo.InvariantCulture);
                        string id="SV5_LOOP_CAND_"+hash.Substring(0,20);
                        string sector=Sector(center[center.Count/2]);
                        var candidate=new Sv5LoopCandidate(id,hash,rank,from,to,rooms[fromRoom].Host,sector,feet,center,
                            cells,baselineCost,"REJECTED","INELIGIBLE_HASH");
                        candidate.Eligible=rank%100UL<(ulong)profile.EligibilityPercent;
                        if(candidate.Eligible){candidate.Status="ELIGIBLE";candidate.Reason="PENDING_ACCEPTANCE";}
                        generated.Add(candidate);
                    }
                }
            }

            var accepted=new List<Sv5LoopLink>();
            var usedPairs=new HashSet<string>(StringComparer.Ordinal);
            var claimed=new HashSet<RmapSpecialWorldPoint>();
            var sectorCounts=new Dictionary<string,int>(StringComparer.Ordinal);
            var rejection=new Dictionary<string,int>(StringComparer.Ordinal);
            foreach(var item in searchRejections) rejection[item.Key]=item.Value;
            var eligible=generated.Where(c=>c.Eligible).OrderBy(c=>c).ToArray();
            var sectorOrder=eligible.GroupBy(c=>c.SectorId,StringComparer.Ordinal)
                .OrderBy(g=>g.Min(c=>c.StableRank)).ThenBy(g=>g.Key,StringComparer.Ordinal).ToArray();
            foreach(var group in sectorOrder)
            {
                if(accepted.Count>=profile.Target || accepted.Count>=profile.Maximum) break;
                foreach(var candidate in group.OrderBy(c=>c))
                    if(TryAccept(candidate)) break;
            }
            foreach(var candidate in eligible)
            {
                if(accepted.Count>=profile.Target || accepted.Count>=profile.Maximum) break;
                if(candidate.Status=="ACCEPTED") continue;
                TryAccept(candidate);
            }
            foreach(var candidate in generated.Where(c=>c.Eligible && c.Status=="ELIGIBLE"))
            {
                candidate.Status="REJECTED"; candidate.Reason=accepted.Count>=profile.Target ? "TARGET_REACHED" : "NOT_SELECTED_DENSITY";
            }
            foreach(var candidate in generated.Where(c=>c.Status=="REJECTED")) Add(rejection,candidate.Reason);
            var diagnostics=new List<string>();
            if(accepted.Count<profile.Minimum) diagnostics.Add("MINIMUM_LOOPS|"+accepted.Count+"/"+profile.Minimum);
            int distinct=accepted.Select(l=>l.SectorId).Distinct(StringComparer.Ordinal).Count();
            if(distinct<profile.MinimumSectors) diagnostics.Add("MINIMUM_LOOP_SECTORS|"+distinct+"/"+profile.MinimumSectors);
            if(accepted.GroupBy(l=>l.SectorId).Any(g=>g.Count()>profile.MaximumPerSector)) diagnostics.Add("LOOP_SECTOR_CAP");
            int baseBridges=plan.Infill.Links.Count;
            return new Sv5LoopPlan(plan.Digest,profile,generated,accepted,diagnostics,rejection,0,baseBridges);

            bool TryAccept(Sv5LoopCandidate candidate)
            {
                if(candidate.Status=="ACCEPTED") return true;
                if(usedPairs.Contains(candidate.PairKey)) return Reject(candidate,"DUPLICATE_ENDPOINT_PAIR");
                int sectorCount=sectorCounts.TryGetValue(candidate.SectorId,out int count) ? count : 0;
                if(sectorCount>=profile.MaximumPerSector) return Reject(candidate,"SECTOR_CAP");
                var owned=candidate.Cells.Select(c=>c.World).ToArray();
                if(owned.Any(claimed.Contains)) return Reject(candidate,"OVERLAPS_ACCEPTED_LOOP");
                string id="SV5_LOOP_"+candidate.StableHash.Substring(0,20);
                string contour=accepted.Count%2==0 ? "SHELF" : "SCALLOP";
                var recelled=candidate.Cells.Select(c=>new Sv5LoopCell(id,c.World,c.Role,c.SourceValue,c.FinalValue,c.SourceOwner)).ToArray();
                var provisional=new Sv5LoopLink(id,Classify(candidate.BaselineCost,candidate.NewCost),candidate.FromOwner,
                    candidate.ToOwner,candidate.Host,candidate.SectorId,candidate.FootPath,candidate.Centerline,recelled,
                    candidate.BaselineCost,candidate.NewCost,contour,true,1,0,6,9);
                var trial=accepted.Concat(new[]{provisional}).ToArray();
                var movement=ApplyPhysicalCells(plan.Connections,trial);
                var stateErrors=Sv5SpacePhysicalMovement.FindStateErrors(plan.Core,movement,plan.Gates);
                if(stateErrors.Count!=0) return Reject(candidate,"FSM_OR_RESOURCE_BYPASS|"+stateErrors[0]);
                accepted.Add(provisional); usedPairs.Add(candidate.PairKey); sectorCounts[candidate.SectorId]=sectorCount+1;
                foreach(var p in owned) claimed.Add(p);
                candidate.Status="ACCEPTED";candidate.Reason="PASS";
                return true;
            }
            bool Reject(Sv5LoopCandidate candidate,string reason)
            { candidate.Status="REJECTED";candidate.Reason=reason; return false; }
        }

        public static IReadOnlyList<string> ValidatePayload(Sv5SpaceGraphPlan baseline,Sv5LoopPlan payload)
        {
            var errors=new List<string>();
            if(baseline==null || payload==null) return Array.AsReadOnly(new[]{"LOOP_PAYLOAD_NULL"});
            if(baseline.Digest!=payload.BaselineDigest) errors.Add("LOOP_BASELINE_DIGEST_MISMATCH");
            errors.AddRange(payload.Diagnostics);
            if(payload.Links.Select(l=>l.Id).Distinct(StringComparer.Ordinal).Count()!=payload.Links.Count) errors.Add("DUPLICATE_LOOP_ID");
            if(payload.Links.Select(l=>Pair(l.FromOwner,l.ToOwner)).Distinct(StringComparer.Ordinal).Count()!=payload.Links.Count) errors.Add("DUPLICATE_LOOP_PAIR");
            if(payload.Cells.GroupBy(c=>c.World).Any(g=>g.Select(c=>c.LoopId).Distinct(StringComparer.Ordinal).Count()>1)) errors.Add("LOOP_CELL_OVERLAP");
            foreach(var link in payload.Links)
            {
                if(ValidateSupportedPath(link.FootPath,payload.Profile.MaximumLength).Count!=0) errors.Add("LOOP_PATH_INVALID|"+link.Id);
                if(link.Centerline.Count<4 || link.Centerline.Count>24 || link.Centerline.Distinct().Count()!=link.Centerline.Count ||
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

        private static bool TryCells(string loopId,string from,string to,string host,IReadOnlyList<RmapSpecialWorldPoint> feet,
            IReadOnlyList<RmapSpecialWorldPoint> centerline,IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5InfillCell> actual,
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
                var sourceValue=source==null ? (existingHostAir ? Sv5InfillCellValue.Air : Sv5InfillCellValue.Unknown) : source.Value;
                var role=d.Role;
                if(d.Value==Sv5InfillCellValue.Air && source!=null)
                {
                    if(!endpointOwned){error=(source.Value==Sv5InfillCellValue.Air ? "EXTERNAL_AIR_DEPENDENCY|" : "FOREIGN_SOLID_OPENING|")+p;output=Array.Empty<Sv5LoopCell>();return false;}
                    if(source.Value==Sv5InfillCellValue.Solid) role=Sv5LoopCellRole.ApertureOverride;
                }
                if(d.Value==Sv5InfillCellValue.Solid && source!=null && source.Value!=Sv5InfillCellValue.Solid)
                { error="SUPPORT_NOT_SOLID|"+p; output=Array.Empty<Sv5LoopCell>(); return false; }
                cells.Add(new Sv5LoopCell(loopId,p,role,sourceValue,d.Value,source?.Owner ?? (existingHostAir ? host : string.Empty)));
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

        private static int ShortestCost(RmapSpecialWorldPoint start,RmapSpecialWorldPoint goal,ISet<RmapSpecialWorldPoint> air)
        {
            if(!air.Contains(start) || !air.Contains(goal)) return -1;
            var queue=new Queue<RmapSpecialWorldPoint>(); var distance=new Dictionary<RmapSpecialWorldPoint,int>{{start,1}}; queue.Enqueue(start);
            while(queue.Count!=0)
            {
                var p=queue.Dequeue(); if(p.Equals(goal)) return distance[p];
                foreach(var n in Neighbors(p)) if(air.Contains(n) && !distance.ContainsKey(n)){distance[n]=distance[p]+1;queue.Enqueue(n);}
            }
            return -1;
        }

        private static IEnumerable<RmapSpecialWorldPoint> Neighbors(RmapSpecialWorldPoint p)
        {
            yield return new RmapSpecialWorldPoint(p.X-1,p.Y); yield return new RmapSpecialWorldPoint(p.X+1,p.Y);
            yield return new RmapSpecialWorldPoint(p.X,p.Y-1); yield return new RmapSpecialWorldPoint(p.X,p.Y+1);
        }
        private static bool Is(IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5InfillCell> cells,int x,int y,Sv5InfillCellValue value)
            => cells.TryGetValue(new RmapSpecialWorldPoint(x,y),out Sv5InfillCell cell) && cell.Value==value;
        private static string BaseRoom(string owner) => owner.EndsWith("_LINK",StringComparison.Ordinal) ? owner.Substring(0,owner.Length-5) : owner;
        private static int Distance(RmapSpecialWorldPoint a,RmapSpecialWorldPoint b) => Math.Abs(a.X-b.X)+Math.Abs(a.Y-b.Y);
        private static string Pair(string a,string b) => string.Compare(a,b,StringComparison.Ordinal)<=0 ? a+"|"+b : b+"|"+a;
        private static string Sector(RmapSpecialWorldPoint p) => "S"+(p.Y/32).ToString("00",CultureInfo.InvariantCulture)+"_"+
            (p.X/48).ToString("00",CultureInfo.InvariantCulture);
        private static void Add(IDictionary<string,int> counts,string key)
        { if(string.IsNullOrEmpty(key)) key="UNSPECIFIED"; counts[key]=counts.TryGetValue(key,out int count) ? count+1 : 1; }
    }
}
