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
    public enum Sv5TreeCellRole
    {
        TrunkClimb = 1,
        BranchPlatform = 2,
        DecorativeBranch = 3,
        LeafDecoration = 4,
        MovementClearance = 5
    }

    public enum Sv5TreeGrabFace { Left = 1, Right = 2 }

    public sealed class Sv5TreeCell : IComparable<Sv5TreeCell>
    {
        internal Sv5TreeCell(RmapSpecialWorldPoint world,Sv5TreeCellRole role,
            Sv5InfillCellValue beforeValue,Sv5InfillCellValue finalValue,Sv5InfillCellValue supportValue)
        {
            World=world;Role=role;BeforeValue=beforeValue;FinalValue=finalValue;SupportValue=supportValue;
            Owner=beforeValue==finalValue?string.Empty:(IsVisualOnly?"TREE_VISUAL_ACTUAL":"TREE_GRAB_ACTUAL");
        }

        public RmapSpecialWorldPoint World { get; }
        public Sv5TreeCellRole Role { get; }
        public Sv5InfillCellValue BeforeValue { get; }
        public Sv5InfillCellValue FinalValue { get; }
        public Sv5InfillCellValue SupportValue { get; }
        public string Owner { get; }
        public bool BodyClear=>FinalValue==Sv5InfillCellValue.Air;
        public bool HeadClear { get; internal set; }
        public bool TopOnly=>Role==Sv5TreeCellRole.BranchPlatform;
        public bool GrabEnabled=>Role==Sv5TreeCellRole.TrunkClimb;
        public bool MovementEnabled=>Role==Sv5TreeCellRole.TrunkClimb||Role==Sv5TreeCellRole.BranchPlatform;
        public bool IsVisualOnly=>Role==Sv5TreeCellRole.DecorativeBranch||Role==Sv5TreeCellRole.LeafDecoration;
        public string ExportRole=>Role==Sv5TreeCellRole.TrunkClimb?"TRUNK_CLIMB":
            Role==Sv5TreeCellRole.BranchPlatform?"BRANCH_PLATFORM":
            Role==Sv5TreeCellRole.DecorativeBranch?"DECORATIVE_BRANCH":
            Role==Sv5TreeCellRole.LeafDecoration?"LEAF_DECORATION":"MOVEMENT_CLEARANCE";
        public string DigestToken=>World+"|"+ExportRole+"|"+BeforeValue+">"+FinalValue+"|"+Owner+"|"+
            TopOnly+"|"+GrabEnabled;
        public int CompareTo(Sv5TreeCell other)=>other==null?1:World.CompareTo(other.World);
    }

    public sealed class Sv5TreeSurface : IComparable<Sv5TreeSurface>
    {
        internal Sv5TreeSurface(string id,RmapSpecialWorldPoint world,Sv5TreeGrabFace face)
        {
            Id=id??string.Empty;World=world;Face=face;
            HangAir=new RmapSpecialWorldPoint(world.X+(face==Sv5TreeGrabFace.Left?-1:1),world.Y);
        }

        public string Id { get; }
        public RmapSpecialWorldPoint World { get; }
        public Sv5TreeGrabFace Face { get; }
        public RmapSpecialWorldPoint HangAir { get; }
        public string AnchorType=>"TRUNK_CLIMB_FACE";
        public string ExportFace=>Face.ToString().ToUpperInvariant();
        public string DigestToken=>Id+"|"+World+"|"+ExportFace+"|"+HangAir;
        public int CompareTo(Sv5TreeSurface other)=>other==null?1:string.Compare(Id,other.Id,StringComparison.Ordinal);
    }

    public sealed class Sv5TreeLimb : IComparable<Sv5TreeLimb>
    {
        internal Sv5TreeLimb(string id,string parentId,int generation,IEnumerable<RmapSpecialWorldPoint> path,
            bool leadsToFork,bool terminal)
        {
            Id=id??string.Empty;ParentId=parentId??string.Empty;Generation=generation;
            Path=Array.AsReadOnly((path??Array.Empty<RmapSpecialWorldPoint>()).ToArray());
            LeadsToFork=leadsToFork;Terminal=terminal;
        }
        public string Id { get; }
        public string ParentId { get; }
        public int Generation { get; }
        public IReadOnlyList<RmapSpecialWorldPoint> Path { get; }
        public bool LeadsToFork { get; }
        public bool Terminal { get; }
        public int ProgressCells=>Math.Max(0,Path.Count-1);
        public RmapSpecialWorldPoint Start=>Path.First();
        public RmapSpecialWorldPoint End=>Path.Last();
        public string DigestToken=>Id+"|"+ParentId+"|"+Generation+"|"+LeadsToFork+"|"+Terminal+"|"+
            string.Join(";",Path);
        public int CompareTo(Sv5TreeLimb other)=>other==null?1:string.Compare(Id,other.Id,StringComparison.Ordinal);
    }

    public sealed class Sv5TreeFork : IComparable<Sv5TreeFork>
    {
        internal Sv5TreeFork(string id,string parentLimbId,int generation,RmapSpecialWorldPoint world,
            IEnumerable<string> childLimbIds,bool substantial)
        {
            Id=id??string.Empty;ParentLimbId=parentLimbId??string.Empty;Generation=generation;World=world;
            ChildLimbIds=Array.AsReadOnly((childLimbIds??Array.Empty<string>()).ToArray());Substantial=substantial;
        }
        public string Id { get; }
        public string ParentLimbId { get; }
        public int Generation { get; }
        public RmapSpecialWorldPoint World { get; }
        public IReadOnlyList<string> ChildLimbIds { get; }
        public bool Substantial { get; }
        public string DigestToken=>Id+"|"+ParentLimbId+"|"+Generation+"|"+World+"|"+
            string.Join(";",ChildLimbIds)+"|"+Substantial;
        public int CompareTo(Sv5TreeFork other)=>other==null?1:string.Compare(Id,other.Id,StringComparison.Ordinal);
    }

    public sealed class Sv5TreePlatform : IComparable<Sv5TreePlatform>
    {
        internal Sv5TreePlatform(string id,int startX,int endX,int y)
        {Id=id??string.Empty;StartX=startX;EndX=endX;Y=y;}
        public string Id { get; }
        public int StartX { get; }
        public int EndX { get; }
        public int Y { get; }
        public bool TopOnly=>true;
        public bool Grabbable=>false;
        public bool LandingValid=>true;
        public string DigestToken=>Id+"|"+StartX+":"+EndX+":"+Y;
        public int CompareTo(Sv5TreePlatform other)=>other==null?1:string.Compare(Id,other.Id,StringComparison.Ordinal);
    }

    public sealed class Sv5TreeRoute : IComparable<Sv5TreeRoute>
    {
        internal Sv5TreeRoute(string id,IEnumerable<Sv5MovementEdge> edges,bool recovery)
        {Id=id;Edges=Array.AsReadOnly(edges.OrderBy(edge=>edge.Sequence).ToArray());Recovery=recovery;}
        public string Id { get; }
        public IReadOnlyList<Sv5MovementEdge> Edges { get; }
        public bool Recovery { get; }
        public string DigestToken=>Id+"|"+Recovery+"|"+string.Join(";",Edges.Select(edge=>edge.DigestToken));
        public int CompareTo(Sv5TreeRoute other)=>other==null?1:string.Compare(Id,other.Id,StringComparison.Ordinal);
    }

    public sealed class Sv5TreeGrabPlan
    {
        internal Sv5TreeGrabPlan(string baselineDigest,Sv5SpaceBounds reservedVolume,int rootAxisX,
            IEnumerable<Sv5TreeCell> cells,IEnumerable<Sv5TreeSurface> surfaces,IEnumerable<Sv5TreeLimb> limbs,
            IEnumerable<Sv5TreeFork> forks,IEnumerable<Sv5TreePlatform> platforms,
            IEnumerable<Sv5TreeRoute> routes,IEnumerable<Sv5TreeRoute> recoveryRoutes,
            IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell> finalOccupancy,
            IEnumerable<int> requiredLevels,double lowerWidth,double middleWidth,double upperWidth,
            int longestStraightRun,long generationMilliseconds,IEnumerable<string> diagnostics)
        {
            BaselineDigest=baselineDigest??string.Empty;ReservedVolume=reservedVolume;RootAxisX=rootAxisX;
            Cells=Array.AsReadOnly(cells.OrderBy(cell=>cell).ToArray());
            Surfaces=Array.AsReadOnly(surfaces.OrderBy(surface=>surface).ToArray());
            Limbs=Array.AsReadOnly(limbs.OrderBy(limb=>limb).ToArray());
            Forks=Array.AsReadOnly(forks.OrderBy(fork=>fork).ToArray());
            Platforms=Array.AsReadOnly(platforms.OrderBy(platform=>platform).ToArray());
            Routes=Array.AsReadOnly(routes.OrderBy(route=>route).ToArray());
            RecoveryRoutes=Array.AsReadOnly(recoveryRoutes.OrderBy(route=>route).ToArray());
            FinalOccupancy=finalOccupancy;
            RequiredLevels=Array.AsReadOnly(requiredLevels.Distinct().OrderBy(value=>value).ToArray());
            LowerTrunkWidth=lowerWidth;MiddleTrunkWidth=middleWidth;UpperTrunkWidth=upperWidth;
            LongestStraightMainRun=longestStraightRun;GenerationMilliseconds=generationMilliseconds;
            Diagnostics=Array.AsReadOnly((diagnostics??Array.Empty<string>()).Distinct(StringComparer.Ordinal)
                .OrderBy(value=>value,StringComparer.Ordinal).ToArray());
            Digest=RmapWorldDefinition.Hash("SV5_12_FIX01_TREE_CANOPY_V1\n"+BaselineDigest+"\n"+ReservedVolume+"\n"+
                RootAxisX+"\n"+string.Join("\n",Cells.Select(cell=>cell.DigestToken))+"\n"+
                string.Join("\n",Surfaces.Select(surface=>surface.DigestToken))+"\n"+
                string.Join("\n",Limbs.Select(limb=>limb.DigestToken))+"\n"+
                string.Join("\n",Forks.Select(fork=>fork.DigestToken))+"\n"+
                string.Join("\n",Platforms.Select(platform=>platform.DigestToken))+"\n"+
                string.Join("\n",Routes.Concat(RecoveryRoutes).Select(route=>route.DigestToken))+"\n"+
                LowerTrunkWidth.ToString("0.000",CultureInfo.InvariantCulture)+"|"+
                MiddleTrunkWidth.ToString("0.000",CultureInfo.InvariantCulture)+"|"+
                UpperTrunkWidth.ToString("0.000",CultureInfo.InvariantCulture)+"|"+
                string.Join("\n",Diagnostics));
        }

        public string BaselineDigest { get; }
        public Sv5SpaceBounds ReservedVolume { get; }
        public int RootAxisX { get; }
        public IReadOnlyList<Sv5TreeCell> Cells { get; }
        public IReadOnlyList<Sv5TreeSurface> Surfaces { get; }
        public IReadOnlyList<Sv5TreeLimb> Limbs { get; }
        public IReadOnlyList<Sv5TreeFork> Forks { get; }
        public IReadOnlyList<Sv5TreePlatform> Platforms { get; }
        public IReadOnlyList<Sv5TreeRoute> Routes { get; }
        public IReadOnlyList<Sv5TreeRoute> RecoveryRoutes { get; }
        public IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell> FinalOccupancy { get; }
        public IReadOnlyList<int> RequiredLevels { get; }
        public IReadOnlyList<RmapSpecialWorldPoint> MajorBranchPoints=>Forks.Select(fork=>fork.World).ToArray();
        public IReadOnlyList<RmapSpecialWorldPoint> ClimbEndpoints=>Limbs.Where(limb=>limb.Terminal).Select(limb=>limb.End).Distinct().ToArray();
        public IReadOnlyList<RmapSpecialWorldPoint> RequiredEntrances=>Routes.Select(route=>route.Edges.First().From).ToArray();
        public IReadOnlyList<RmapSpecialWorldPoint> RequiredExits=>Routes.Select(route=>route.Edges.Last().To).ToArray();
        public double LowerTrunkWidth { get; }
        public double MiddleTrunkWidth { get; }
        public double UpperTrunkWidth { get; }
        public int LongestStraightMainRun { get; }
        public long GenerationMilliseconds { get; }
        public int BranchPlatformCount=>Platforms.Count;
        public int BranchPlatformCellCount=>Cells.Count(cell=>cell.Role==Sv5TreeCellRole.BranchPlatform);
        public int BranchGrabCount=>Surfaces.Count(surface=>Cells.Any(cell=>cell.World.Equals(surface.World)&&
            cell.Role==Sv5TreeCellRole.BranchPlatform));
        public int SecondGenerationForkCount=>Forks.Count(fork=>Limbs.Any(limb=>limb.Id==fork.ParentLimbId&&limb.Generation>=1));
        public IReadOnlyList<string> Diagnostics { get; }
        public string Digest { get; }
        public bool TreeCanopyFixReady=>Success;
        public bool TreeGrabGeometryReady=>Success;
        public bool ComposedGeometryReady=>false;
        public bool PlayerVerified=>false;
        public int LocalSearchMarginCells=>2;
        public int WholeWorldCopyPerCandidate=>0;
        public int WholeWorldBfsPerCandidate=>0;
        public int GlobalProductRuns=>1;
        public bool AllEndpointPairComparison=>false;
        public bool Success=>Diagnostics.Count==0&&ReservedVolume.Width>=8&&ReservedVolume.Width<=10&&
            ReservedVolume.Height>=22&&ReservedVolume.Height<=26&&Forks.Count(fork=>fork.Substantial)>=3&&
            SecondGenerationForkCount>=1&&ClimbEndpoints.Count>=4&&ClimbEndpoints.Count<=6&&
            Platforms.Count>=7&&Platforms.Count<=10&&Surfaces.Count>0&&Routes.Count>=2&&
            RecoveryRoutes.Count>=RequiredLevels.Count&&RequiredLevels.Count>=3&&BranchGrabCount==0;
    }

    public static class Sv5TreeGrab
    {
        private sealed class Desired
        {public Sv5TreeCellRole Role;public Sv5InfillCellValue Value;}

        private sealed class ContactSpec
        {
            public ContactSpec(int x,int y,Sv5TreeGrabFace face){X=x;Y=y;Face=face;}
            public int X;public int Y;public Sv5TreeGrabFace Face;
        }

        public static Sv5TreeGrabPlan Build(Sv5SpaceGraphPlan baseline)
        {
            if(baseline?.HubShell==null||!baseline.HubShell.Success)
                throw new ArgumentException("A passing Hub-shell plan is required.",nameof(baseline));
            var timer=Stopwatch.StartNew();var hub=baseline.HubShell;var bounds=hub.Footprint;
            var reserved=new Sv5SpaceBounds(bounds.X+7,bounds.Y+8,10,24);
            var inner=new Sv5SpaceBounds(bounds.X+6,bounds.Y+5,12,30);
            var owned=new Sv5SpaceBounds(reserved.X-2,reserved.Y-2,reserved.Width+4,reserved.Height+4);
            var desired=new Dictionary<RmapSpecialWorldPoint,Desired>();var diagnostics=new List<string>();
            int variant=(bounds.X/16)&1;
            RmapSpecialWorldPoint P(int x,int y)=>new RmapSpecialWorldPoint(
                reserved.X+(variant==0?x:reserved.Width-1-x),reserved.Y+y);
            Sv5TreeGrabFace Face(Sv5TreeGrabFace face)=>variant==0?face:
                (face==Sv5TreeGrabFace.Left?Sv5TreeGrabFace.Right:Sv5TreeGrabFace.Left);

            void Put(RmapSpecialWorldPoint point,Sv5TreeCellRole role,Sv5InfillCellValue value)
            {
                if(!owned.Contains(point)){diagnostics.Add("TREE_OWNED_AABB_ESCAPE|"+point);return;}
                if(desired.TryGetValue(point,out Desired existing))
                {
                    if(existing.Value!=value)diagnostics.Add("TREE_ROLE_OCCUPANCY_CONFLICT|"+point);
                    if(role==Sv5TreeCellRole.TrunkClimb||existing.Role==Sv5TreeCellRole.MovementClearance)
                        desired[point]=new Desired{Role=role,Value=value};
                    return;
                }
                desired.Add(point,new Desired{Role=role,Value=value});
            }
            void Solid(int x,int y,Sv5TreeCellRole role)=>Put(P(x,y),role,Sv5InfillCellValue.Solid);
            void Visual(int x,int y,Sv5TreeCellRole role)
            {
                var point=P(x,y);
                if(reserved.Contains(point)&&!desired.ContainsKey(point)&&
                    Sv5LoopTopology.ValueAt(hub.FinalOccupancy,point)==Sv5InfillCellValue.Air)
                    Put(point,role,Sv5InfillCellValue.Air);
            }
            IReadOnlyList<RmapSpecialWorldPoint> Path(params int[] coordinates)
            {
                var result=new List<RmapSpecialWorldPoint>();
                for(int index=0;index<coordinates.Length;index+=2)result.Add(P(coordinates[index],coordinates[index+1]));
                return result;
            }

            var limbs=new List<Sv5TreeLimb>
            {
                new Sv5TreeLimb("L00_ROOT","",0,Path(5,0,5,1,5,2,5,3,4,3,4,4,4,5,5,5,5,6,5,7),true,false),
                new Sv5TreeLimb("L10_LEFT","L00_ROOT",1,Path(5,7,4,7,4,8,3,8,3,9,3,10,2,10,2,11,2,12),true,false),
                new Sv5TreeLimb("L11_RIGHT","L00_ROOT",1,Path(5,7,6,7,6,8,7,8,7,9,7,10,8,10,8,11,8,12),true,false),
                new Sv5TreeLimb("L20_LEFT_CROWN","L10_LEFT",2,Path(2,12,1,12,1,13,0,13,0,14,0,15,1,15,1,16,1,17,0,17,0,18,0,19,1,19,1,20,1,21,0,21,0,22,0,23),false,true),
                new Sv5TreeLimb("L21_INNER_CROWN","L10_LEFT",2,Path(2,12,3,12,3,13,4,13,4,14,4,15,3,15,3,16,3,17,4,17,4,18,4,19,3,19,3,20,3,21,4,21,4,22,4,23),false,true),
                new Sv5TreeLimb("L22_RIGHT_CROWN","L11_RIGHT",2,Path(8,12,9,12,9,13,9,14,8,14,8,15,8,16,9,16,9,17,9,18,8,18,8,19,9,19,9,20,9,21,9,22,9,23,8,23),false,true),
                new Sv5TreeLimb("L23_SECOND_FORK","L11_RIGHT",2,Path(8,12,7,12,7,13,6,13,6,14,6,15,7,15,7,16,7,17,6,17,6,18),true,false),
                new Sv5TreeLimb("L30_HIGH_CENTER","L23_SECOND_FORK",3,Path(6,18,6,19,5,19,5,20,6,20,6,21,6,22,6,23),false,true),
                new Sv5TreeLimb("L31_HIGH_RIGHT","L23_SECOND_FORK",3,Path(6,18,7,18,7,19,7,20,7,21,7,22),false,true)
            };
            foreach(var point in limbs.SelectMany(limb=>limb.Path).Distinct())Put(point,Sv5TreeCellRole.TrunkClimb,Sv5InfillCellValue.Solid);

            foreach(int x in Enumerable.Range(3,5))Solid(x,0,Sv5TreeCellRole.TrunkClimb);
            foreach(int x in Enumerable.Range(3,5))Solid(x,1,Sv5TreeCellRole.TrunkClimb);
            foreach(int x in Enumerable.Range(3,5))Solid(x,2,Sv5TreeCellRole.TrunkClimb);
            foreach(int x in Enumerable.Range(3,4))Solid(x,3,Sv5TreeCellRole.TrunkClimb);
            foreach(int x in Enumerable.Range(3,4))Solid(x,4,Sv5TreeCellRole.TrunkClimb);
            foreach(int x in Enumerable.Range(3,4))Solid(x,5,Sv5TreeCellRole.TrunkClimb);
            foreach(int x in Enumerable.Range(3,4))Solid(x,6,Sv5TreeCellRole.TrunkClimb);
            foreach(int x in Enumerable.Range(3,4))Solid(x,7,Sv5TreeCellRole.TrunkClimb);
            foreach(var point in new[]{new[]{2,8},new[]{1,9},new[]{2,9},new[]{9,10},new[]{1,11},
                new[]{3,11},new[]{5,14},new[]{7,3},new[]{7,4},new[]{7,5},new[]{1,8},new[]{0,9}})
                Solid(point[0],point[1],Sv5TreeCellRole.TrunkClimb);

            var forks=new List<Sv5TreeFork>
            {
                new Sv5TreeFork("F00","L00_ROOT",0,P(5,7),new[]{"L10_LEFT","L11_RIGHT"},true),
                new Sv5TreeFork("F10","L10_LEFT",1,P(2,12),new[]{"L20_LEFT_CROWN","L21_INNER_CROWN"},true),
                new Sv5TreeFork("F11","L11_RIGHT",1,P(8,12),new[]{"L22_RIGHT_CROWN","L23_SECOND_FORK"},true),
                new Sv5TreeFork("F20","L23_SECOND_FORK",2,P(6,18),new[]{"L30_HIGH_CENTER","L31_HIGH_RIGHT"},true)
            };

            var platforms=new List<Sv5TreePlatform>();
            void Platform(string id,int startX,int endX,int y)
            {
                int worldStart=P(startX,y).X,worldEnd=P(endX,y).X;
                int first=Math.Min(worldStart,worldEnd),last=Math.Max(worldStart,worldEnd);
                platforms.Add(new Sv5TreePlatform(id,first,last,P(startX,y).Y));
                for(int x=startX;x<=endX;x++)Solid(x,y,Sv5TreeCellRole.BranchPlatform);
            }
            Platform("P00_LEFT_ROOT",2,2,0);Platform("P01_RIGHT_ROOT",8,9,0);
            Platform("P02_LEFT_LOW",1,1,7);Platform("P03_RIGHT_LOW",9,9,9);
            Platform("P04_LEFT_MID",2,2,15);Platform("P05_CENTER_MID",5,5,15);
            Platform("P06_LEFT_RECOVERY",2,2,19);Platform("P07_CENTER_RECOVERY",5,5,21);
            Platform("P08_LEFT_EXIT",2,2,22);

            foreach(var point in new[]{new[]{2,6},new[]{1,6},new[]{0,7},new[]{7,6},new[]{8,6},new[]{9,7},
                new[]{1,10},new[]{0,10},new[]{9,11},new[]{2,14},new[]{1,14},new[]{7,14},new[]{2,17},
                new[]{5,17},new[]{2,20},new[]{5,20}})Visual(point[0],point[1],Sv5TreeCellRole.DecorativeBranch);
            for(int y=13;y<24;y++)for(int x=0;x<10;x++)
                if(((x*3+y*5+variant*2)%7)<=1&&(y>=18||x==0||x==9))Visual(x,y,Sv5TreeCellRole.LeafDecoration);

            if(!inner.Contains(P(0,0))||!inner.Contains(P(9,23)))diagnostics.Add("TREE_ENVELOPE_OUTSIDE_HUB_INNER");
            var collisionSolid=new HashSet<RmapSpecialWorldPoint>(desired.Where(pair=>pair.Value.Value==Sv5InfillCellValue.Solid)
                .Select(pair=>pair.Key));
            var trunk=new HashSet<RmapSpecialWorldPoint>(desired.Where(pair=>pair.Value.Role==Sv5TreeCellRole.TrunkClimb)
                .Select(pair=>pair.Key));
            var surfaces=BuildSurfaces(trunk,collisionSolid).ToArray();
            var surfaceIndex=surfaces.ToDictionary(surface=>surface.World+"|"+surface.ExportFace,StringComparer.Ordinal);
            Sv5TreeSurface FindSurface(RmapSpecialWorldPoint contact,Sv5TreeGrabFace face)
            {
                surfaceIndex.TryGetValue(contact+"|"+face.ToString().ToUpperInvariant(),out Sv5TreeSurface surface);
                return surface;
            }

            var routes=new List<Sv5TreeRoute>();
            void Route(string id,IReadOnlyList<ContactSpec> specs,RmapSpecialWorldPoint exit)
            {
                var selected=new List<Sv5TreeSurface>();
                foreach(var spec in specs)
                {
                    var surface=FindSurface(P(spec.X,spec.Y),Face(spec.Face));
                    if(surface==null)diagnostics.Add("TREE_ROUTE_SURFACE_MISSING|"+id+"|"+P(spec.X,spec.Y));
                    else selected.Add(surface);
                }
                if(selected.Count!=specs.Count)return;
                var edges=new List<Sv5MovementEdge>();
                var start=new RmapSpecialWorldPoint(selected[0].HangAir.X,selected[0].HangAir.Y-1);
                edges.Add(new Sv5MovementEdge(0,start,selected[0].HangAir,Sv5PlatformerMoveType.JumpGrab,true,true,
                    selected[0].World,"BRANCH_TO_TRUNK"));
                for(int index=1;index<selected.Count;index++)edges.Add(new Sv5MovementEdge(edges.Count,
                    selected[index-1].HangAir,selected[index].HangAir,Sv5PlatformerMoveType.Jump,true,true,
                    selected[index].World,"TRUNK_CLIMB"));
                edges.Add(new Sv5MovementEdge(edges.Count,selected.Last().HangAir,exit,Sv5PlatformerMoveType.Jump,
                    true,true,null,"TRUNK_TO_BRANCH"));
                routes.Add(new Sv5TreeRoute(id,edges,false));
            }
            Route("TREE_ROUTE_LEFT",new[]{new ContactSpec(3,2,Sv5TreeGrabFace.Left),
                new ContactSpec(3,4,Sv5TreeGrabFace.Left),new ContactSpec(3,6,Sv5TreeGrabFace.Left),
                new ContactSpec(4,8,Sv5TreeGrabFace.Right),new ContactSpec(3,9,Sv5TreeGrabFace.Right),
                new ContactSpec(3,10,Sv5TreeGrabFace.Right),new ContactSpec(1,13,Sv5TreeGrabFace.Right),
                new ContactSpec(0,15,Sv5TreeGrabFace.Left),new ContactSpec(1,16,Sv5TreeGrabFace.Right),
                new ContactSpec(0,18,Sv5TreeGrabFace.Left),new ContactSpec(1,20,Sv5TreeGrabFace.Right),
                new ContactSpec(0,22,Sv5TreeGrabFace.Left),new ContactSpec(0,23,Sv5TreeGrabFace.Left)},P(2,23));
            Route("TREE_ROUTE_RIGHT",new[]{new ContactSpec(7,2,Sv5TreeGrabFace.Right),
                new ContactSpec(7,3,Sv5TreeGrabFace.Right),new ContactSpec(7,5,Sv5TreeGrabFace.Right),
                new ContactSpec(6,8,Sv5TreeGrabFace.Left),new ContactSpec(7,9,Sv5TreeGrabFace.Left),
                new ContactSpec(7,10,Sv5TreeGrabFace.Left),new ContactSpec(9,13,Sv5TreeGrabFace.Right),
                new ContactSpec(9,14,Sv5TreeGrabFace.Right),new ContactSpec(9,16,Sv5TreeGrabFace.Right),
                new ContactSpec(9,18,Sv5TreeGrabFace.Right),new ContactSpec(9,21,Sv5TreeGrabFace.Right),
                new ContactSpec(9,22,Sv5TreeGrabFace.Right),new ContactSpec(9,23,Sv5TreeGrabFace.Right)},P(8,24));

            var recovery=new List<Sv5TreeRoute>();var requiredLevels=new List<int>();
            void Recovery(string id,RmapSpecialWorldPoint from,RmapSpecialWorldPoint to)
            {
                requiredLevels.Add(from.Y);recovery.Add(new Sv5TreeRoute(id,new[]{new Sv5MovementEdge(0,from,to,
                    Sv5PlatformerMoveType.Drop,true,true,null,"DROP_RECOVERY")},true));
            }
            Recovery("TREE_RECOVERY_01",P(2,20),P(2,16));
            Recovery("TREE_RECOVERY_02",P(2,23),P(2,20));
            Recovery("TREE_RECOVERY_03",P(5,24),P(5,22));

            var movementPoints=routes.Concat(recovery).SelectMany(route=>route.Edges).SelectMany(edge=>new[]{edge.From,edge.To,
                new RmapSpecialWorldPoint(edge.From.X,edge.From.Y+1),new RmapSpecialWorldPoint(edge.To.X,edge.To.Y+1)}).Distinct().ToArray();
            foreach(var point in movementPoints)
            {
                if(collisionSolid.Contains(point)){diagnostics.Add("TREE_MOVEMENT_CLEARANCE_BLOCKED|"+point);continue;}
                if(!desired.ContainsKey(point))Put(point,Sv5TreeCellRole.MovementClearance,Sv5InfillCellValue.Air);
            }

            var hubMovementOpen=new HashSet<RmapSpecialWorldPoint>(hub.Connections.SelectMany(connection=>connection.MovementWitness)
                .SelectMany(edge=>new[]{edge.From,edge.To,new RmapSpecialWorldPoint(edge.From.X,edge.From.Y+1),
                    new RmapSpecialWorldPoint(edge.To.X,edge.To.Y+1)}));
            foreach(var port in hub.Ports)
            {hubMovementOpen.Add(port.Anchor);hubMovementOpen.Add(new RmapSpecialWorldPoint(port.Anchor.X,port.Anchor.Y+1));}
            if(collisionSolid.Any(hubMovementOpen.Contains))diagnostics.Add("TREE_REQUIRED_ENTRY_EXIT_BLOCKED");

            var final=new Dictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell>(hub.FinalOccupancy);
            foreach(var pair in desired)final[pair.Key]=new Sv5LoopOccupancyCell(pair.Key,pair.Value.Value,
                pair.Value.Role==Sv5TreeCellRole.TrunkClimb?"TREE_TRUNK_CLIMB":
                pair.Value.Role==Sv5TreeCellRole.BranchPlatform?"TREE_BRANCH_PLATFORM":"TREE_VISUAL_OR_CLEARANCE",
                pair.Value.Role.ToString().ToUpperInvariant());
            var cells=desired.OrderBy(pair=>pair.Key).Select(pair=>new Sv5TreeCell(pair.Key,pair.Value.Role,
                Sv5LoopTopology.ValueAt(hub.FinalOccupancy,pair.Key),pair.Value.Value,
                Sv5LoopTopology.ValueAt(final,new RmapSpecialWorldPoint(pair.Key.X,pair.Key.Y-1)))).ToArray();
            foreach(var cell in cells)cell.HeadClear=Sv5LoopTopology.ValueAt(final,
                new RmapSpecialWorldPoint(cell.World.X,cell.World.Y+1))==Sv5InfillCellValue.Air;

            int rootWidth=trunk.Count(point=>point.Y==reserved.Y);
            double lowerAverage=BandAverage(trunk,reserved,0),middleAverage=BandAverage(trunk,reserved,1),
                upperAverage=BandAverage(trunk,reserved,2);
            int straightRun=LongestStraightRun(limbs);
            int minX=limbs.SelectMany(limb=>limb.Path).Min(point=>point.X),maxX=limbs.SelectMany(limb=>limb.Path).Max(point=>point.X);
            int minY=limbs.SelectMany(limb=>limb.Path).Min(point=>point.Y),maxY=limbs.SelectMany(limb=>limb.Path).Max(point=>point.Y);
            int rootAxis=P(5,0).X;
            if(rootWidth<4||rootWidth>5)diagnostics.Add("TREE_ROOT_WIDTH_INVALID|"+rootWidth);
            if(lowerAverage<upperAverage*1.5||lowerAverage<middleAverage||middleAverage<upperAverage)
                diagnostics.Add("TREE_TAPER_INVALID|"+lowerAverage+"|"+middleAverage+"|"+upperAverage);
            if(forks.Count(fork=>fork.Substantial)<3)diagnostics.Add("TREE_SUBSTANTIAL_FORK_SHORTFALL");
            if(forks.Count(fork=>limbs.Any(limb=>limb.Id==fork.ParentLimbId&&limb.Generation>=1))<1)
                diagnostics.Add("TREE_SECOND_GENERATION_FORK_MISSING");
            int endpointCount=limbs.Where(limb=>limb.Terminal).Select(limb=>limb.End).Distinct().Count();
            if(endpointCount<4||endpointCount>6)diagnostics.Add("TREE_CLIMB_ENDPOINT_INVALID|"+endpointCount);
            if(maxX-minX+1<8||maxY-minY+1<20||rootAxis-minX<3||maxX-rootAxis<3)
                diagnostics.Add("TREE_CANOPY_SPAN_INVALID|"+(maxX-minX+1)+"|"+(maxY-minY+1));
            if(straightRun>5)diagnostics.Add("TREE_STRAIGHT_RUN_TOO_LONG|"+straightRun);
            if(platforms.Count<7||platforms.Count>10)diagnostics.Add("TREE_PLATFORM_COUNT_INVALID|"+platforms.Count);
            if(!Connected(trunk))diagnostics.Add("TREE_TRUNK_CLIMB_NETWORK_SPLIT");
            if(HasSolidRectangle(collisionSolid,6,6))diagnostics.Add("TREE_FORBIDDEN_SOLID_RECTANGLE");
            if(Symmetric(trunk,rootAxis))diagnostics.Add("TREE_FORBIDDEN_PERFECT_SYMMETRY");
            if(limbs.Any(limb=>limb.Path.Any(point=>!trunk.Contains(point))))diagnostics.Add("TREE_LIMB_NOT_BOUND_TO_TRUNK");
            if(limbs.Any(limb=>limb.Path.Distinct().Count()!=limb.Path.Count||
                limb.Path.Zip(limb.Path.Skip(1),(first,second)=>Math.Abs(first.X-second.X)+Math.Abs(first.Y-second.Y)).Any(delta=>delta!=1)))
                diagnostics.Add("TREE_LIMB_PATH_INVALID");
            var surfacePoints=new HashSet<RmapSpecialWorldPoint>(surfaces.Select(surface=>surface.World));
            foreach(var route in routes)
            {
                if(route.Edges.Count(edge=>edge.MoveType==Sv5PlatformerMoveType.JumpGrab)!=1)
                    diagnostics.Add("TREE_INITIAL_GRAB_COUNT_INVALID|"+route.Id);
                if(route.Edges.Any(edge=>!ValidateMovementEdge(edge,collisionSolid,surfacePoints)))
                    diagnostics.Add("TREE_ROUTE_INVALID|"+route.Id);
                if(route.Edges.Skip(1).Take(route.Edges.Count-2).Any(edge=>edge.TraversalState!="TRUNK_CLIMB"))
                    diagnostics.Add("TREE_CONTINUOUS_CLIMB_STATE_MISSING|"+route.Id);
            }
            foreach(var route in recovery)
            {
                var edge=route.Edges.Single();
                if(!ValidateMovementEdge(edge,collisionSolid,surfacePoints)||Sv5LoopTopology.ValueAt(final,
                    new RmapSpecialWorldPoint(edge.To.X,edge.To.Y-1))!=Sv5InfillCellValue.Solid)
                    diagnostics.Add("TREE_RECOVERY_INVALID|"+route.Id);
            }
            if(!AnchorsDistinct(hub.Connections.Select(connection=>connection.ExternalAnchor)))
                diagnostics.Add("TREE_DUPLICATE_EXTERNAL_ANCHOR");
            if(!ConnectionBodiesDisjoint(hub.Connections,hub.Footprint))diagnostics.Add("TREE_CONNECTION_BODY_MERGE");
            timer.Stop();if(timer.ElapsedMilliseconds>100)diagnostics.Add("TREE_GENERATION_TIME_EXCEEDED|"+timer.ElapsedMilliseconds);
            return new Sv5TreeGrabPlan(baseline.Digest,reserved,rootAxis,cells,surfaces,limbs,forks,platforms,
                routes,recovery,new ReadOnlyDictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell>(final),requiredLevels,
                lowerAverage,middleAverage,upperAverage,straightRun,timer.ElapsedMilliseconds,diagnostics);
        }

        public static bool ValidateMovementEdge(Sv5MovementEdge edge,ISet<RmapSpecialWorldPoint> solid,
            ISet<RmapSpecialWorldPoint> surfacePoints)
        {
            if(edge==null||!edge.BodyClear||!edge.HeadClear)return false;
            int dx=edge.To.X-edge.From.X,dy=edge.To.Y-edge.From.Y;
            if(edge.TraversalState=="TRUNK_CLIMB")return edge.Contact.HasValue&&solid!=null&&surfacePoints!=null&&
                solid.Contains(edge.Contact.Value)&&surfacePoints.Contains(edge.Contact.Value)&&
                Math.Abs(dx)<=4&&dy>=1&&dy<=4;
            if(edge.MoveType==Sv5PlatformerMoveType.Walk)return Math.Abs(dx)==1&&dy==0;
            if(edge.MoveType==Sv5PlatformerMoveType.StepUp||edge.MoveType==Sv5PlatformerMoveType.StepDown)
                return Math.Abs(dx)==1&&Math.Abs(dy)==1;
            if(edge.MoveType==Sv5PlatformerMoveType.Jump)return Math.Abs(dx)>=1&&Math.Abs(dx)<=3&&dy>=-2&&dy<=2;
            if(edge.MoveType==Sv5PlatformerMoveType.Drop)return Math.Abs(dx)<=1&&dy>=-4&&dy<=-1;
            if(edge.MoveType!=Sv5PlatformerMoveType.JumpGrab||Math.Abs(dx)>2||dy<1||dy>2||!edge.Contact.HasValue)
                return false;
            return solid!=null&&surfacePoints!=null&&solid.Contains(edge.Contact.Value)&&surfacePoints.Contains(edge.Contact.Value);
        }

        public static bool HasSolidRectangle(ISet<RmapSpecialWorldPoint> solid,int width,int height)
        {
            if(solid==null||width<=0||height<=0)return false;
            return solid.Any(origin=>Enumerable.Range(0,width).All(dx=>Enumerable.Range(0,height)
                .All(dy=>solid.Contains(new RmapSpecialWorldPoint(origin.X+dx,origin.Y+dy)))));
        }

        public static bool ConnectionBodiesDisjoint(IEnumerable<Sv5HubConnection> source,Sv5SpaceBounds hubBounds)
        {
            var connections=(source??Array.Empty<Sv5HubConnection>()).ToArray();
            return PointSetsDisjoint(connections.Select(connection=>connection.Centerline.Where(point=>
                !hubBounds.Contains(point)&&!point.Equals(connection.ExternalAnchor))));
        }

        public static bool AnchorsDistinct(IEnumerable<RmapSpecialWorldPoint> anchors)
        {var values=(anchors??Array.Empty<RmapSpecialWorldPoint>()).ToArray();return values.Distinct().Count()==values.Length;}

        public static bool PointSetsDisjoint(IEnumerable<IEnumerable<RmapSpecialWorldPoint>> source)
        {
            var seen=new HashSet<RmapSpecialWorldPoint>();
            foreach(var points in source??Array.Empty<IEnumerable<RmapSpecialWorldPoint>>())
                foreach(var point in (points??Array.Empty<RmapSpecialWorldPoint>()).Distinct())if(!seen.Add(point))return false;
            return true;
        }

        public static bool RoutesReachUpper(IEnumerable<Sv5TreeRoute> routes,int upperY)=>(routes??
            Array.Empty<Sv5TreeRoute>()).Any(route=>route.Edges.Count>0&&route.Edges.Max(edge=>edge.To.Y)>=upperY);

        public static bool RecoveryCoversLevels(IEnumerable<Sv5TreeRoute> routes,IEnumerable<int> requiredLevels)
        {
            var sources=new HashSet<int>((routes??Array.Empty<Sv5TreeRoute>()).Where(route=>route.Edges.Count>0)
                .Select(route=>route.Edges.First().From.Y));
            return (requiredLevels??Array.Empty<int>()).All(sources.Contains);
        }

        public static bool Connected(ISet<RmapSpecialWorldPoint> cells)
        {
            if(cells==null||cells.Count==0)return false;
            var visited=new HashSet<RmapSpecialWorldPoint>();var queue=new Queue<RmapSpecialWorldPoint>();
            var first=cells.OrderBy(point=>point).First();visited.Add(first);queue.Enqueue(first);
            while(queue.Count>0)foreach(var next in Cardinal(queue.Dequeue()))if(cells.Contains(next)&&visited.Add(next))queue.Enqueue(next);
            return visited.Count==cells.Count;
        }

        public static bool Symmetric(IEnumerable<RmapSpecialWorldPoint> cells,int centerX)
        {
            var set=new HashSet<RmapSpecialWorldPoint>(cells??Array.Empty<RmapSpecialWorldPoint>());
            return set.Count>0&&set.All(point=>set.Contains(new RmapSpecialWorldPoint(centerX*2-point.X,point.Y)));
        }

        public static int LongestStraightRun(IEnumerable<Sv5TreeLimb> source)
        {
            int best=0;
            foreach(var limb in source??Array.Empty<Sv5TreeLimb>())
            {
                int current=0,previousX=int.MinValue,previousY=int.MinValue;
                for(int index=1;index<limb.Path.Count;index++)
                {
                    int dx=limb.Path[index].X-limb.Path[index-1].X,dy=limb.Path[index].Y-limb.Path[index-1].Y;
                    current=dx==previousX&&dy==previousY?current+1:1;best=Math.Max(best,current);
                    previousX=dx;previousY=dy;
                }
            }
            return best;
        }

        private static IEnumerable<Sv5TreeSurface> BuildSurfaces(ISet<RmapSpecialWorldPoint> trunk,
            ISet<RmapSpecialWorldPoint> collisionSolid)
        {
            int index=0;
            foreach(var point in trunk.OrderBy(value=>value))
            {
                foreach(var face in new[]{Sv5TreeGrabFace.Left,Sv5TreeGrabFace.Right})
                {
                    var hang=new RmapSpecialWorldPoint(point.X+(face==Sv5TreeGrabFace.Left?-1:1),point.Y);
                    var head=new RmapSpecialWorldPoint(hang.X,hang.Y+1);
                    if(collisionSolid.Contains(hang)||collisionSolid.Contains(head))continue;
                    yield return new Sv5TreeSurface("TREE_CLIMB_"+(++index).ToString("000",CultureInfo.InvariantCulture),point,face);
                }
            }
        }

        private static IEnumerable<RmapSpecialWorldPoint> Cardinal(RmapSpecialWorldPoint point)
        {
            yield return new RmapSpecialWorldPoint(point.X-1,point.Y);yield return new RmapSpecialWorldPoint(point.X+1,point.Y);
            yield return new RmapSpecialWorldPoint(point.X,point.Y-1);yield return new RmapSpecialWorldPoint(point.X,point.Y+1);
        }

        private static double BandAverage(ISet<RmapSpecialWorldPoint> trunk,Sv5SpaceBounds volume,int band)
        {
            int start=volume.Y+(volume.Height*band)/3,end=volume.Y+(volume.Height*(band+1))/3;
            var widths=new List<int>();
            for(int y=start;y<end;y++)
            {
                var xs=trunk.Where(point=>point.Y==y).Select(point=>point.X).OrderBy(x=>x).ToArray();
                if(xs.Length==0)continue;int best=1,current=1;
                for(int index=1;index<xs.Length;index++){current=xs[index]==xs[index-1]+1?current+1:1;best=Math.Max(best,current);}
                widths.Add(best);
            }
            return widths.Count==0?0:widths.Average();
        }
    }
}
