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
        internal Sv5TreeGrabPlan(string baselineDigest,Sv5SpaceBounds reservedVolume,
            IEnumerable<Sv5TreeCell> cells,IEnumerable<Sv5TreeSurface> surfaces,
            IEnumerable<Sv5TreeRoute> routes,IEnumerable<Sv5TreeRoute> recoveryRoutes,
            IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell> finalOccupancy,
            IEnumerable<int> requiredLevels,IEnumerable<RmapSpecialWorldPoint> majorBranchPoints,
            IEnumerable<RmapSpecialWorldPoint> climbEndpoints,double lowerWidth,double middleWidth,double upperWidth,
            int longestStraightRun,long generationMilliseconds,IEnumerable<string> diagnostics)
        {
            BaselineDigest=baselineDigest??string.Empty;ReservedVolume=reservedVolume;
            Cells=Array.AsReadOnly(cells.OrderBy(cell=>cell).ToArray());
            Surfaces=Array.AsReadOnly(surfaces.OrderBy(surface=>surface).ToArray());
            Routes=Array.AsReadOnly(routes.OrderBy(route=>route).ToArray());
            RecoveryRoutes=Array.AsReadOnly(recoveryRoutes.OrderBy(route=>route).ToArray());
            FinalOccupancy=finalOccupancy;RequiredLevels=Array.AsReadOnly(requiredLevels.Distinct().OrderBy(value=>value).ToArray());
            MajorBranchPoints=Array.AsReadOnly(majorBranchPoints.Distinct().OrderBy(value=>value).ToArray());
            ClimbEndpoints=Array.AsReadOnly(climbEndpoints.Distinct().OrderBy(value=>value).ToArray());
            LowerTrunkWidth=lowerWidth;MiddleTrunkWidth=middleWidth;UpperTrunkWidth=upperWidth;
            LongestStraightMainRun=longestStraightRun;GenerationMilliseconds=generationMilliseconds;
            Diagnostics=Array.AsReadOnly((diagnostics??Array.Empty<string>()).Distinct(StringComparer.Ordinal)
                .OrderBy(value=>value,StringComparer.Ordinal).ToArray());
            Digest=RmapWorldDefinition.Hash("SV5_TREE_GRAB_BRANCHING_V2\n"+BaselineDigest+"\n"+ReservedVolume+"\n"+
                string.Join("\n",Cells.Select(cell=>cell.DigestToken))+"\n"+
                string.Join("\n",Surfaces.Select(surface=>surface.DigestToken))+"\n"+
                string.Join("\n",Routes.Concat(RecoveryRoutes).Select(route=>route.DigestToken))+"\n"+
                LowerTrunkWidth.ToString("0.000",CultureInfo.InvariantCulture)+"|"+
                MiddleTrunkWidth.ToString("0.000",CultureInfo.InvariantCulture)+"|"+
                UpperTrunkWidth.ToString("0.000",CultureInfo.InvariantCulture)+"|"+
                string.Join("\n",Diagnostics));
        }

        public string BaselineDigest { get; }
        public Sv5SpaceBounds ReservedVolume { get; }
        public IReadOnlyList<Sv5TreeCell> Cells { get; }
        public IReadOnlyList<Sv5TreeSurface> Surfaces { get; }
        public IReadOnlyList<Sv5TreeRoute> Routes { get; }
        public IReadOnlyList<Sv5TreeRoute> RecoveryRoutes { get; }
        public IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell> FinalOccupancy { get; }
        public IReadOnlyList<int> RequiredLevels { get; }
        public IReadOnlyList<RmapSpecialWorldPoint> MajorBranchPoints { get; }
        public IReadOnlyList<RmapSpecialWorldPoint> ClimbEndpoints { get; }
        public IReadOnlyList<RmapSpecialWorldPoint> RequiredEntrances=>Routes.Select(route=>route.Edges.First().From).ToArray();
        public IReadOnlyList<RmapSpecialWorldPoint> RequiredExits=>Routes.Select(route=>route.Edges.Last().To).ToArray();
        public double LowerTrunkWidth { get; }
        public double MiddleTrunkWidth { get; }
        public double UpperTrunkWidth { get; }
        public int LongestStraightMainRun { get; }
        public long GenerationMilliseconds { get; }
        public int BranchPlatformCount=>Cells.Count(cell=>cell.Role==Sv5TreeCellRole.BranchPlatform);
        public int BranchGrabCount=>Surfaces.Count(surface=>Cells.Any(cell=>cell.World.Equals(surface.World)&&
            cell.Role==Sv5TreeCellRole.BranchPlatform));
        public IReadOnlyList<string> Diagnostics { get; }
        public string Digest { get; }
        public bool TreeGrabGeometryReady=>Success;
        public bool ComposedGeometryReady=>false;
        public bool PlayerVerified=>false;
        public int WholeWorldCopyPerCandidate=>0;
        public int WholeWorldBfsPerCandidate=>0;
        public int GlobalProductRuns=>1;
        public bool Success=>Diagnostics.Count==0&&Cells.Any(cell=>cell.Role==Sv5TreeCellRole.TrunkClimb)&&
            MajorBranchPoints.Count>=2&&ClimbEndpoints.Count>=3&&Surfaces.Count>0&&Routes.Count>=2&&
            RecoveryRoutes.Count>=RequiredLevels.Count&&RequiredLevels.Count>=3&&BranchGrabCount==0;
    }

    public static class Sv5TreeGrab
    {
        private sealed class Desired
        {public Sv5TreeCellRole Role;public Sv5InfillCellValue Value;}

        public static Sv5TreeGrabPlan Build(Sv5SpaceGraphPlan baseline)
        {
            if(baseline?.HubShell==null||!baseline.HubShell.Success)
                throw new ArgumentException("A passing Hub-shell plan is required.",nameof(baseline));
            var timer=Stopwatch.StartNew();var hub=baseline.HubShell;var bounds=hub.Footprint;
            var reserved=new Sv5SpaceBounds(bounds.X+10,bounds.Y+8,4,24);
            var owned=new Sv5SpaceBounds(reserved.X-2,reserved.Y-2,reserved.Width+4,reserved.Height+4);
            var desired=new Dictionary<RmapSpecialWorldPoint,Desired>();var diagnostics=new List<string>();
            var mainWidths=new Dictionary<int,int>();var mainCenters=new Dictionary<int,int>();
            int variant=(int)(baseline.Seed%3UL);int center=bounds.X+11;

            void Put(int x,int y,Sv5TreeCellRole role,Sv5InfillCellValue value)
            {
                var point=new RmapSpecialWorldPoint(x,y);
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
            void Solid(int x,int y,Sv5TreeCellRole role)=>Put(x,y,role,Sv5InfillCellValue.Solid);
            void Visual(int x,int y,Sv5TreeCellRole role)
            {
                var point=new RmapSpecialWorldPoint(x,y);
                if(!desired.ContainsKey(point)&&Sv5LoopTopology.ValueAt(hub.FinalOccupancy,point)==Sv5InfillCellValue.Air)
                    Put(x,y,role,Sv5InfillCellValue.Air);
            }
            void MainRow(int relativeY,int rowCenter,int width)
            {
                int first=rowCenter-(width-1)/2;
                for(int x=first;x<first+width;x++)Solid(x,bounds.Y+relativeY,Sv5TreeCellRole.TrunkClimb);
                mainWidths[relativeY]=width;mainCenters[relativeY]=rowCenter;
            }

            int[] lowerWidths={4,4,4,3,3,3};
            int[] lowerOffsets={0,0,variant==1?1:0,0,-1,0};
            for(int index=0;index<lowerWidths.Length;index++)MainRow(8+index,center+lowerOffsets[index],lowerWidths[index]);
            int[] middleWidths={3,2,3,2,2,3,2,2};
            int[] middleOffsets={0,0,1,1,0,-1,-1,0};
            for(int index=0;index<middleWidths.Length;index++)MainRow(14+index,center+middleOffsets[index],middleWidths[index]);
            int[] upperWidths={2,1,2,1,1,2,1,1,2,1};
            int[] upperOffsets={1,1,0,-1,-1,0,1,1,0,1};
            for(int index=0;index<upperWidths.Length;index++)MainRow(22+index,center+upperOffsets[index],upperWidths[index]);

            var leftContacts=new List<RmapSpecialWorldPoint>();var rightContacts=new List<RmapSpecialWorldPoint>();
            for(int relativeY=10,index=0;relativeY<=30;relativeY+=2,index++)
            {
                leftContacts.Add(new RmapSpecialWorldPoint(center-1-(index%2),bounds.Y+relativeY));
                rightContacts.Add(new RmapSpecialWorldPoint(center+2-(index%2),bounds.Y+relativeY));
            }
            void Rail(IReadOnlyList<RmapSpecialWorldPoint> contacts,bool left)
            {
                foreach(var contact in contacts)Solid(contact.X,contact.Y,Sv5TreeCellRole.TrunkClimb);
                for(int index=0;index+1<contacts.Count;index++)
                {
                    var from=contacts[index];var to=contacts[index+1];
                    Solid(from.X,from.Y+1,Sv5TreeCellRole.TrunkClimb);
                    if(left)
                    {
                        if(to.X>from.X)Solid(to.X,from.Y+1,Sv5TreeCellRole.TrunkClimb);
                        else Solid(from.X,to.Y,Sv5TreeCellRole.TrunkClimb);
                    }
                    else
                    {
                        if(to.X<from.X)Solid(to.X,from.Y+1,Sv5TreeCellRole.TrunkClimb);
                        else Solid(from.X,to.Y,Sv5TreeCellRole.TrunkClimb);
                    }
                }
            }
            Rail(leftContacts,true);Rail(rightContacts,false);

            int rootY=bounds.Y+8;
            Solid(center-2,rootY,Sv5TreeCellRole.BranchPlatform);
            Solid(center+3,rootY,Sv5TreeCellRole.BranchPlatform);
            foreach(var point in new[]{new RmapSpecialWorldPoint(center-3,bounds.Y+29),
                new RmapSpecialWorldPoint(center+4,bounds.Y+29)})Solid(point.X,point.Y,Sv5TreeCellRole.BranchPlatform);
            foreach(int relativeY in new[]{13,17,21,25})Solid(center+4,bounds.Y+relativeY,Sv5TreeCellRole.BranchPlatform);

            foreach(var pair in new[]{new[]{center-2,center-3},new[]{center+2,center+4},new[]{center-1,center-3}})
            {
                int branchY=pair[0]==center-2?bounds.Y+15:pair[0]==center+2?bounds.Y+20:bounds.Y+25;
                int direction=Math.Sign(pair[1]-pair[0]);
                for(int x=pair[0];x!=pair[1]+direction;x+=direction)Visual(x,branchY,Sv5TreeCellRole.DecorativeBranch);
            }
            for(int relativeY=23;relativeY<=33;relativeY++)
            {
                int spread=relativeY<27?2:3;
                for(int x=center-spread;x<=center+spread+(relativeY%3==0?1:0);x++)
                    if(((x+relativeY+variant)&1)==0)Visual(x,bounds.Y+relativeY,Sv5TreeCellRole.LeafDecoration);
            }

            var plannedRouteClearance=leftContacts.Select(contact=>new RmapSpecialWorldPoint(contact.X-1,contact.Y))
                .Concat(rightContacts.Select(contact=>new RmapSpecialWorldPoint(contact.X+1,contact.Y))).ToArray();
            var requiredOpen=new HashSet<RmapSpecialWorldPoint>(plannedRouteClearance.SelectMany(point=>new[]{point,
                new RmapSpecialWorldPoint(point.X,point.Y+1)}));
            foreach(var point in new[]{new RmapSpecialWorldPoint(center-2,bounds.Y+9),
                new RmapSpecialWorldPoint(center+3,bounds.Y+9),new RmapSpecialWorldPoint(center-3,bounds.Y+30),
                new RmapSpecialWorldPoint(center+4,bounds.Y+30)})
            {requiredOpen.Add(point);requiredOpen.Add(new RmapSpecialWorldPoint(point.X,point.Y+1));}
            foreach(int level in new[]{14,18,22,26})
            {
                requiredOpen.Add(new RmapSpecialWorldPoint(center+4,bounds.Y+level));
                requiredOpen.Add(new RmapSpecialWorldPoint(center+4,bounds.Y+level+1));
            }
            var hubMovementOpen=new HashSet<RmapSpecialWorldPoint>(hub.Connections.SelectMany(connection=>connection.MovementWitness)
                .SelectMany(edge=>new[]{edge.From,edge.To,new RmapSpecialWorldPoint(edge.From.X,edge.From.Y+1),
                    new RmapSpecialWorldPoint(edge.To.X,edge.To.Y+1)}));
            foreach(var port in hub.Ports)
            {hubMovementOpen.Add(port.Anchor);hubMovementOpen.Add(new RmapSpecialWorldPoint(port.Anchor.X,port.Anchor.Y+1));}
            requiredOpen.UnionWith(hubMovementOpen);
            foreach(var point in requiredOpen)
                if(desired.TryGetValue(point,out Desired blocked)&&blocked.Value==Sv5InfillCellValue.Solid)
                    desired.Remove(point);

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
            void Route(string id,IReadOnlyList<RmapSpecialWorldPoint> contacts,Sv5TreeGrabFace face,int exitX)
            {
                var nodes=contacts.Select(contact=>new RmapSpecialWorldPoint(contact.X+
                    (face==Sv5TreeGrabFace.Left?-1:1),contact.Y)).ToArray();
                var edges=new List<Sv5MovementEdge>();var firstSurface=FindSurface(contacts[0],face);
                var start=new RmapSpecialWorldPoint(nodes[0].X,nodes[0].Y-1);
                edges.Add(new Sv5MovementEdge(0,start,nodes[0],Sv5PlatformerMoveType.JumpGrab,true,true,
                    contacts[0],"BRANCH_TO_TRUNK"));
                for(int index=1;index<nodes.Length;index++)
                {
                    int dx=Math.Abs(nodes[index].X-nodes[index-1].X),dy=nodes[index].Y-nodes[index-1].Y;
                    var move=dx==1&&dy==1?Sv5PlatformerMoveType.StepUp:Sv5PlatformerMoveType.Jump;
                    edges.Add(new Sv5MovementEdge(edges.Count,nodes[index-1],nodes[index],move,true,true,
                        contacts[index],"TRUNK_CLIMB"));
                }
                var exit=new RmapSpecialWorldPoint(exitX,nodes.Last().Y);
                edges.Add(new Sv5MovementEdge(edges.Count,nodes.Last(),exit,Sv5PlatformerMoveType.Jump,true,true,
                    null,"TRUNK_TO_BRANCH"));
                if(firstSurface==null)diagnostics.Add("TREE_INITIAL_GRAB_SURFACE_MISSING|"+id);
                routes.Add(new Sv5TreeRoute(id,edges,false));
            }
            Route("TREE_ROUTE_LEFT",leftContacts,Sv5TreeGrabFace.Left,center-3);
            Route("TREE_ROUTE_RIGHT",rightContacts,Sv5TreeGrabFace.Right,center+4);

            var requiredLevels=new[]{18,22,26}.Select(relativeY=>bounds.Y+relativeY).ToArray();
            var recovery=new List<Sv5TreeRoute>();
            foreach(int level in requiredLevels)
            {
                var from=new RmapSpecialWorldPoint(center+4,level);var to=new RmapSpecialWorldPoint(center+4,level-4);
                recovery.Add(new Sv5TreeRoute("TREE_RECOVERY_"+(level-bounds.Y).ToString("00",CultureInfo.InvariantCulture),
                    new[]{new Sv5MovementEdge(0,from,to,Sv5PlatformerMoveType.Drop,true,true,null,"DROP_RECOVERY")},true));
            }

            var movementPoints=routes.Concat(recovery).SelectMany(route=>route.Edges).SelectMany(edge=>new[]{edge.From,edge.To,
                new RmapSpecialWorldPoint(edge.From.X,edge.From.Y+1),new RmapSpecialWorldPoint(edge.To.X,edge.To.Y+1)}).Distinct().ToArray();
            foreach(var point in movementPoints)
            {
                if(collisionSolid.Contains(point)){diagnostics.Add("TREE_MOVEMENT_CLEARANCE_BLOCKED|"+point);continue;}
                if(!desired.ContainsKey(point))Put(point.X,point.Y,Sv5TreeCellRole.MovementClearance,Sv5InfillCellValue.Air);
            }

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

            var degree=trunk.ToDictionary(point=>point,point=>Cardinal(point).Count(trunk.Contains));
            var branchPoints=degree.Where(pair=>pair.Value>=3).Select(pair=>pair.Key)
                .GroupBy(point=>(point.Y-bounds.Y)/4).Select(group=>group.OrderBy(point=>point).First()).Take(4).ToArray();
            var endpoints=new[]{leftContacts.Last(),rightContacts.Last(),new RmapSpecialWorldPoint(
                center+upperOffsets.Last(),bounds.Y+31)}.Where(trunk.Contains).Distinct().ToArray();
            if(branchPoints.Length<2)diagnostics.Add("TREE_MAJOR_BRANCH_SHORTFALL|"+branchPoints.Length);
            if(endpoints.Length<3)diagnostics.Add("TREE_CLIMB_ENDPOINT_SHORTFALL|"+endpoints.Length);
            if(!Connected(trunk))diagnostics.Add("TREE_TRUNK_CLIMB_NETWORK_SPLIT");
            if(HasSolidRectangle(collisionSolid,6,6))diagnostics.Add("TREE_FORBIDDEN_SOLID_RECTANGLE");
            if(cells.Any(cell=>cell.Role==Sv5TreeCellRole.BranchPlatform&&cell.GrabEnabled))
                diagnostics.Add("TREE_BRANCH_PLATFORM_GRAB_ENABLED");
            if(surfaces.Any(surface=>desired[surface.World].Role!=Sv5TreeCellRole.TrunkClimb))
                diagnostics.Add("TREE_BRANCH_GRAB_SURFACE_EXPORTED");
            var lowerAverage=Average(mainWidths,8,13);var middleAverage=Average(mainWidths,14,21);
            var upperAverage=Average(mainWidths,22,31);int straightRun=LongestRun(mainCenters);
            if(lowerAverage<upperAverage*1.5)diagnostics.Add("TREE_TAPER_RATIO_INVALID");
            if(!(lowerAverage>=middleAverage&&middleAverage>=upperAverage))diagnostics.Add("TREE_TAPER_ORDER_INVALID");
            if(straightRun>9)diagnostics.Add("TREE_STRAIGHT_RUN_TOO_LONG|"+straightRun);
            if(Symmetric(trunk,center))diagnostics.Add("TREE_FORBIDDEN_PERFECT_SYMMETRY");
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
            var occupiedByConnections=new HashSet<RmapSpecialWorldPoint>(hub.Connections.SelectMany(connection=>connection.Cells)
                .Where(cell=>cell.Role!=Sv5HubConnectionCellRole.Support).Select(cell=>cell.World));
            if(movementPoints.Any(occupiedByConnections.Contains)||hub.Ports.Any(port=>movementPoints.Contains(port.Anchor)))
                diagnostics.Add("TREE_REQUIRED_ENTRY_EXIT_BLOCKED");
            if(!AnchorsDistinct(hub.Connections.Select(connection=>connection.ExternalAnchor)))
                diagnostics.Add("TREE_DUPLICATE_EXTERNAL_ANCHOR");
            if(!ConnectionBodiesDisjoint(hub.Connections,hub.Footprint))diagnostics.Add("TREE_CONNECTION_BODY_MERGE");
            timer.Stop();
            return new Sv5TreeGrabPlan(baseline.Digest,reserved,cells,surfaces,routes,recovery,
                new ReadOnlyDictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell>(final),requiredLevels,branchPoints,endpoints,
                lowerAverage,middleAverage,upperAverage,straightRun,timer.ElapsedMilliseconds,diagnostics);
        }

        public static bool ValidateMovementEdge(Sv5MovementEdge edge,ISet<RmapSpecialWorldPoint> solid,
            ISet<RmapSpecialWorldPoint> surfacePoints)
        {
            if(edge==null||!edge.BodyClear||!edge.HeadClear)return false;
            int dx=edge.To.X-edge.From.X,dy=edge.To.Y-edge.From.Y;
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

        private static double Average(IReadOnlyDictionary<int,int> values,int minimum,int maximum)=>
            values.Where(pair=>pair.Key>=minimum&&pair.Key<=maximum).Average(pair=>pair.Value);

        private static int LongestRun(IReadOnlyDictionary<int,int> centers)
        {
            int longest=0,current=0,previous=int.MinValue;
            foreach(var pair in centers.OrderBy(pair=>pair.Key))
            {current=pair.Value==previous?current+1:1;longest=Math.Max(longest,current);previous=pair.Value;}
            return longest;
        }
    }
}
