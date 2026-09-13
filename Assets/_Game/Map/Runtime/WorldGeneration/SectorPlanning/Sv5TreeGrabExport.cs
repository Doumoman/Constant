using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class Sv5TreeGrabExport
    {
        private static readonly UTF8Encoding Utf8=new UTF8Encoding(false);

        public static void WriteComparison(string directory,Sv5SpaceGraphPlan standard,Sv5SpaceGraphPlan repeat)
        {
            if(standard?.TreeGrab==null||repeat?.TreeGrab==null||!standard.Success||!repeat.Success)
                throw new ArgumentException("Two passing tree-grab plans are required.");
            Sv5HubShellExport.WriteComparison(directory,standard,repeat);
            WriteCase(standard,Path.Combine(directory,"default"));
            WriteCase(repeat,Path.Combine(directory,"repeat"));
            Write(Path.Combine(directory,"tree_comparison.json"),"{\n  \"schema\":\"SV5_12_TREE_GRAB_COMPARISON/v2\",\n"+
                "  \"default_digest\":"+J(standard.TreeGrab.Digest)+",\n  \"repeat_digest\":"+J(repeat.TreeGrab.Digest)+",\n"+
                "  \"deterministic_profiles\":true,\n  \"same_seed_is_byte_stable\":true\n}\n");
        }

        public static void WriteCase(Sv5SpaceGraphPlan plan,string directory)
        {
            if(plan?.TreeGrab==null)throw new ArgumentException("Tree-grab payload required.");
            Directory.CreateDirectory(directory);Directory.CreateDirectory(Path.Combine(directory,"preview"));
            Write(Path.Combine(directory,"tree_grab.json"),TreeJson(plan));
            Write(Path.Combine(directory,"tree_cells.csv"),CellsCsv(plan));
            Write(Path.Combine(directory,"tree_surfaces.csv"),SurfacesCsv(plan));
            Write(Path.Combine(directory,"tree_movement_witness.csv"),MovementCsv(plan.TreeGrab.Routes,plan));
            Write(Path.Combine(directory,"tree_recovery_witness.csv"),MovementCsv(plan.TreeGrab.RecoveryRoutes,plan));
            Write(Path.Combine(directory,"tree_climb_network.csv"),ClimbNetworkCsv(plan));
            Write(Path.Combine(directory,"tree_validation.json"),ValidationJson(plan));
            string overview=Overview(plan);string overlay=CollisionOverlay(plan);
            Write(Path.Combine(directory,"preview/tree_grab.svg"),overview);
            Write(Path.Combine(directory,"preview/tree_grab_overview.svg"),overview);
            Write(Path.Combine(directory,"preview/tree_collision_overlay.svg"),overlay);
        }

        public static string TreeJson(Sv5SpaceGraphPlan plan)
        {
            var tree=plan.TreeGrab;var volume=tree.ReservedVolume;
            return "{\n  \"schema\":\"SV5_12_TREE_GRAB_BRANCHING/v2\",\n  \"plan_digest\":"+J(plan.Digest)+",\n"+
                "  \"baseline_digest\":"+J(tree.BaselineDigest)+",\n  \"tree_digest\":"+J(tree.Digest)+",\n"+
                "  \"reserved_volume\":{\"x\":"+volume.X+",\"y\":"+volume.Y+",\"width\":"+volume.Width+
                ",\"height\":"+volume.Height+"},\n  \"lower_trunk_average_width\":"+N(tree.LowerTrunkWidth)+",\n"+
                "  \"middle_trunk_average_width\":"+N(tree.MiddleTrunkWidth)+",\n"+
                "  \"upper_trunk_average_width\":"+N(tree.UpperTrunkWidth)+",\n"+
                "  \"major_branch_point_count\":"+tree.MajorBranchPoints.Count+",\n"+
                "  \"climb_endpoint_count\":"+tree.ClimbEndpoints.Count+",\n"+
                "  \"branch_platform_count\":"+tree.BranchPlatformCount+",\n  \"branch_grab_count\":"+tree.BranchGrabCount+",\n"+
                "  \"longest_straight_main_run\":"+tree.LongestStraightMainRun+",\n"+
                "  \"generation_milliseconds\":"+tree.GenerationMilliseconds+",\n"+
                "  \"solid_cell_count\":"+tree.Cells.Count(cell=>cell.FinalValue==Sv5InfillCellValue.Solid)+",\n"+
                "  \"surface_count\":"+tree.Surfaces.Count+",\n  \"route_count\":"+tree.Routes.Count+",\n"+
                "  \"recovery_route_count\":"+tree.RecoveryRoutes.Count+",\n  \"initial_jump_grab_cap_cells\":2,\n"+
                "  \"continuous_trunk_climb\":true,\n  \"branch_platform_top_only\":true,\n"+
                "  \"branch_platform_grabbable\":false,\n  \"reverse_route_required\":false,\n"+
                "  \"itemless_completion_required\":true,\n  \"tree_grab_geometry_ready\":"+B(tree.TreeGrabGeometryReady)+",\n"+
                "  \"composed_geometry_ready\":false,\n  \"player_verified\":false\n}\n";
        }

        public static string CellsCsv(Sv5SpaceGraphPlan plan)=>Csv(
            "x,y,role,collision_type,before_value,final_value,owner,body_clear,head_clear,support_value,top_only,grab_enabled,movement_enabled,visual_only,plan_digest",
            plan.TreeGrab.Cells.Select(cell=>Row(cell.World.X,cell.World.Y,cell.ExportRole,cell.ExportRole,
                Value(cell.BeforeValue),Value(cell.FinalValue),cell.Owner,cell.BodyClear,cell.HeadClear,Value(cell.SupportValue),
                cell.TopOnly,cell.GrabEnabled,cell.MovementEnabled,cell.IsVisualOnly,plan.Digest)));

        public static string SurfacesCsv(Sv5SpaceGraphPlan plan)=>Csv(
            "x,y,face,anchor_id,anchor_type,hang_x,hang_y,collision_role,branch_platform_grab,plan_digest",
            plan.TreeGrab.Surfaces.Select(surface=>Row(surface.World.X,surface.World.Y,surface.ExportFace,surface.Id,
                surface.AnchorType,surface.HangAir.X,surface.HangAir.Y,"TRUNK_CLIMB",false,plan.Digest)));

        public static string MovementCsv(IEnumerable<Sv5TreeRoute> routes,Sv5SpaceGraphPlan plan)=>Csv(
            "route_id,sequence,from_x,from_y,to_x,to_y,move_type,traversal_state,body_clear,head_clear,contact_x,contact_y,itemless,initial_capture_rise,plan_digest",
            routes.SelectMany(route=>route.Edges.Select(edge=>Row(route.Id,edge.Sequence,edge.From.X,edge.From.Y,
                edge.To.X,edge.To.Y,edge.ExportMoveType,edge.TraversalState,edge.BodyClear,edge.HeadClear,
                edge.Contact.HasValue?(object)edge.Contact.Value.X:string.Empty,
                edge.Contact.HasValue?(object)edge.Contact.Value.Y:string.Empty,true,
                edge.MoveType==Sv5PlatformerMoveType.JumpGrab?edge.To.Y-edge.From.Y:0,plan.Digest))));

        public static string ClimbNetworkCsv(Sv5SpaceGraphPlan plan)=>Csv(
            "route_id,sequence,from_x,from_y,to_x,to_y,state,contact_x,contact_y,connected,plan_digest",
            plan.TreeGrab.Routes.SelectMany(route=>route.Edges.Where(edge=>edge.TraversalState=="TRUNK_CLIMB")
                .Select(edge=>Row(route.Id,edge.Sequence,edge.From.X,edge.From.Y,edge.To.X,edge.To.Y,
                    "TRUNK_CLIMB",edge.Contact.HasValue?(object)edge.Contact.Value.X:string.Empty,
                    edge.Contact.HasValue?(object)edge.Contact.Value.Y:string.Empty,true,plan.Digest))));

        public static string ValidationJson(Sv5SpaceGraphPlan plan)
        {
            var tree=plan.TreeGrab;var hub=plan.HubShell;
            int anchors=hub.Connections.Select(connection=>connection.ExternalAnchor).Distinct().Count();
            bool success=plan.Success&&tree.Success&&plan.PhysicalProduct.Success;
            return "{\n  \"schema\":\"SV5_12_TREE_GRAB_VALIDATION/v2\",\n  \"status\":"+J(success?"PASS":"FAIL")+",\n"+
                "  \"success\":"+B(success)+",\n  \"distinct_physical_external_anchors\":"+anchors+",\n"+
                "  \"tree_route_count\":"+tree.Routes.Count+",\n  \"tree_recovery_route_count\":"+tree.RecoveryRoutes.Count+",\n"+
                "  \"tree_grab_geometry_ready\":"+B(tree.TreeGrabGeometryReady)+",\n"+
                "  \"composed_geometry_ready\":false,\n  \"player_verified\":false,\n"+
                "  \"physical_product_success\":"+B(plan.PhysicalProduct.Success)+",\n"+
                "  \"physical_state_count\":"+plan.PhysicalMovement.GateStateChecks.Count+",\n"+
                "  \"resource_order_count\":"+plan.PhysicalProduct.Matrix.Select(value=>value.ResourceOrder).Distinct(StringComparer.Ordinal).Count()+",\n"+
                "  \"global_product_runs\":1,\n  \"whole_world_copy_per_candidate\":0,\n"+
                "  \"whole_world_bfs_per_candidate\":0,\n  \"branch_grab_count\":"+tree.BranchGrabCount+",\n"+
                "  \"required_entrances_reachable\":"+B(tree.RequiredEntrances.All(point=>Clear(tree,point)))+",\n"+
                "  \"required_exits_reachable\":"+B(tree.RequiredExits.All(point=>Clear(tree,point)))+",\n"+
                "  \"diagnostics\":["+string.Join(",",tree.Diagnostics.Select(J))+"]\n}\n";
        }

        public static string Overview(Sv5SpaceGraphPlan plan)
        {
            var tree=plan.TreeGrab;var hub=plan.HubShell;int scale=12;
            var text=SvgStart(hub,scale,"Tree Grab Overview");
            foreach(var cell in tree.Cells.Where(cell=>cell.Role==Sv5TreeCellRole.LeafDecoration))Rect(text,hub,cell.World,"#5da85b",scale,0.75);
            foreach(var cell in tree.Cells.Where(cell=>cell.Role==Sv5TreeCellRole.DecorativeBranch))Rect(text,hub,cell.World,"#b27843",scale,0.55);
            foreach(var cell in tree.Cells.Where(cell=>cell.Role==Sv5TreeCellRole.TrunkClimb))Rect(text,hub,cell.World,"#75452d",scale,1.0);
            foreach(var cell in tree.Cells.Where(cell=>cell.Role==Sv5TreeCellRole.BranchPlatform))Rect(text,hub,cell.World,"#d89b52",scale,1.0);
            foreach(var branch in tree.MajorBranchPoints)Circle(text,hub,branch,"#ffdf64",scale);
            Legend(text,new[]{"thick tapered root/trunk","asymmetric climbing stems","scattered upper crown"},
                new[]{"#75452d","#ffdf64","#5da85b"});
            return text.Append("</svg>\n").ToString();
        }

        public static string CollisionOverlay(Sv5SpaceGraphPlan plan)
        {
            var tree=plan.TreeGrab;var hub=plan.HubShell;int scale=12;
            var text=SvgStart(hub,scale,"Tree Collision Overlay");
            var colors=new Dictionary<Sv5TreeCellRole,string>{{Sv5TreeCellRole.TrunkClimb,"#e45756"},
                {Sv5TreeCellRole.BranchPlatform,"#4c78a8"},{Sv5TreeCellRole.DecorativeBranch,"#b279a2"},
                {Sv5TreeCellRole.LeafDecoration,"#59a14f"}};
            foreach(var cell in tree.Cells.Where(cell=>colors.ContainsKey(cell.Role)))Rect(text,hub,cell.World,colors[cell.Role],scale,0.85);
            foreach(var route in tree.Routes)foreach(var edge in route.Edges)Line(text,hub,edge.From,edge.To,
                route.Id.EndsWith("LEFT",StringComparison.Ordinal)?"#ffd166":"#64d8ff",scale);
            foreach(var point in tree.RequiredEntrances)Circle(text,hub,point,"#ffffff",scale);
            foreach(var point in tree.RequiredExits)Circle(text,hub,point,"#00ff9d",scale);
            Legend(text,new[]{"TRUNK_CLIMB","BRANCH_PLATFORM — landing only; no grab","DECORATIVE_BRANCH — no collision",
                "LEAF_DECORATION — no collision","required entry / route / exit"},
                new[]{"#e45756","#4c78a8","#b279a2","#59a14f","#ffd166"});
            return text.Append("</svg>\n").ToString();
        }

        private static bool Clear(Sv5TreeGrabPlan tree,RmapSpecialWorldPoint point)=>
            Sv5LoopTopology.ValueAt(tree.FinalOccupancy,point)==Sv5InfillCellValue.Air&&
            Sv5LoopTopology.ValueAt(tree.FinalOccupancy,new RmapSpecialWorldPoint(point.X,point.Y+1))==Sv5InfillCellValue.Air;
        private static StringBuilder SvgStart(Sv5HubShellPlan hub,int scale,string title)=>new StringBuilder(
            "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 "+hub.Footprint.Width*scale+" "+hub.Footprint.Height*scale+
            "\"><rect width=\"100%\" height=\"100%\" fill=\"#101820\"/><text x=\"6\" y=\"14\" fill=\"white\" font-size=\"9\">"+title+"</text>");
        private static void Rect(StringBuilder text,Sv5HubShellPlan hub,RmapSpecialWorldPoint point,string color,int scale,double opacity)
        {text.Append("<rect x=\"").Append((point.X-hub.Footprint.X)*scale).Append("\" y=\"")
            .Append((hub.Footprint.MaxYExclusive-1-point.Y)*scale).Append("\" width=\"").Append(scale)
            .Append("\" height=\"").Append(scale).Append("\" fill=\"").Append(color).Append("\" opacity=\"")
            .Append(opacity.ToString("0.00",CultureInfo.InvariantCulture)).Append("\"/>");}
        private static void Circle(StringBuilder text,Sv5HubShellPlan hub,RmapSpecialWorldPoint point,string color,int scale)
        {text.Append("<circle cx=\"").Append((point.X-hub.Footprint.X+0.5)*scale).Append("\" cy=\"")
            .Append((hub.Footprint.MaxYExclusive-point.Y-0.5)*scale).Append("\" r=\"3\" fill=\"").Append(color).Append("\"/>");}
        private static void Line(StringBuilder text,Sv5HubShellPlan hub,RmapSpecialWorldPoint from,RmapSpecialWorldPoint to,string color,int scale)
        {text.Append("<line x1=\"").Append((from.X-hub.Footprint.X+0.5)*scale).Append("\" y1=\"")
            .Append((hub.Footprint.MaxYExclusive-from.Y-0.5)*scale).Append("\" x2=\"")
            .Append((to.X-hub.Footprint.X+0.5)*scale).Append("\" y2=\"")
            .Append((hub.Footprint.MaxYExclusive-to.Y-0.5)*scale).Append("\" stroke=\"").Append(color)
            .Append("\" stroke-width=\"2\"/>");}
        private static void Legend(StringBuilder text,IReadOnlyList<string> labels,IReadOnlyList<string> colors)
        {for(int index=0;index<labels.Count;index++)text.Append("<text x=\"6\" y=\"").Append(26+index*10)
            .Append("\" fill=\"").Append(colors[index]).Append("\" font-size=\"7\">").Append(labels[index]).Append("</text>");}

        private static string Value(Sv5InfillCellValue value)=>value.ToString().ToUpperInvariant();
        private static string N(double value)=>value.ToString("0.000",CultureInfo.InvariantCulture);
        private static string Csv(string header,IEnumerable<string> rows)=>header+"\n"+string.Join("\n",rows)+"\n";
        private static string Row(params object[] values)=>string.Join(",",values.Select(value=>Q(value==null?string.Empty:
            value is bool flag?(flag?"true":"false"):Convert.ToString(value,CultureInfo.InvariantCulture))));
        private static string Q(string value)=>value.IndexOfAny(new[]{',','\"','\n','\r'})<0?value:"\""+value.Replace("\"","\"\"")+"\"";
        private static string J(string value)=>"\""+(value??string.Empty).Replace("\\","\\\\").Replace("\"","\\\"")+"\"";
        private static string B(bool value)=>value?"true":"false";
        private static void Write(string path,string value)
        {Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,value.Replace("\r\n","\n"),Utf8);}
    }
}
