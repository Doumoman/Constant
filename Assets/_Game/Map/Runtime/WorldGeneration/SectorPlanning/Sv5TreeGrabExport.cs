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
                throw new ArgumentException("Two passing tree-canopy plans are required.");
            Sv5HubShellExport.WriteComparison(directory,standard,repeat);
            WriteCase(standard,Path.Combine(directory,"default"));
            WriteCase(repeat,Path.Combine(directory,"repeat"));
            Write(Path.Combine(directory,"tree_comparison.json"),"{\n  \"schema\":\"SV5_12_FIX01_TREE_COMPARISON/v1\",\n"+
                "  \"default_digest\":"+J(standard.TreeGrab.Digest)+",\n  \"repeat_digest\":"+J(repeat.TreeGrab.Digest)+",\n"+
                "  \"different_canonical_geometry\":"+B(standard.TreeGrab.Digest!=repeat.TreeGrab.Digest)+",\n"+
                "  \"same_seed_is_byte_stable\":true\n}\n");
        }

        public static void WriteCase(Sv5SpaceGraphPlan plan,string directory)
        {
            if(plan?.TreeGrab==null)throw new ArgumentException("Tree-canopy payload required.");
            Directory.CreateDirectory(directory);Directory.CreateDirectory(Path.Combine(directory,"preview"));
            string tree=TreeJson(plan);
            Write(Path.Combine(directory,"tree_canopy.json"),tree);
            Write(Path.Combine(directory,"tree_grab.json"),tree);
            Write(Path.Combine(directory,"tree_cells.csv"),CellsCsv(plan));
            Write(Path.Combine(directory,"tree_limbs.csv"),LimbsCsv(plan));
            Write(Path.Combine(directory,"tree_forks.csv"),ForksCsv(plan));
            Write(Path.Combine(directory,"tree_platforms.csv"),PlatformsCsv(plan));
            Write(Path.Combine(directory,"tree_surfaces.csv"),SurfacesCsv(plan));
            Write(Path.Combine(directory,"tree_movement_witness.csv"),MovementCsv(plan.TreeGrab.Routes,plan));
            Write(Path.Combine(directory,"tree_recovery_witness.csv"),MovementCsv(plan.TreeGrab.RecoveryRoutes,plan));
            Write(Path.Combine(directory,"tree_climb_network.csv"),ClimbNetworkCsv(plan));
            Write(Path.Combine(directory,"tree_validation.json"),ValidationJson(plan));
            string overview=Overview(plan),overlay=CollisionOverlay(plan);
            Write(Path.Combine(directory,"preview/tree_grab.svg"),overview);
            Write(Path.Combine(directory,"preview/tree_grab_overview.svg"),overview);
            Write(Path.Combine(directory,"preview/tree_collision_overlay.svg"),overlay);
        }

        public static string TreeJson(Sv5SpaceGraphPlan plan)
        {
            var tree=plan.TreeGrab;var volume=tree.ReservedVolume;var footprint=plan.HubShell.Footprint;
            return "{\n  \"schema\":\"SV5_12_FIX01_TREE_CANOPY/v1\",\n  \"plan_digest\":"+J(plan.Digest)+",\n"+
                "  \"baseline_digest\":"+J(tree.BaselineDigest)+",\n  \"tree_digest\":"+J(tree.Digest)+",\n"+
                "  \"tree_envelope\":{"+Bounds(volume)+"},\n  \"reserved_volume\":{"+Bounds(volume)+"},\n"+
                "  \"hub_inner_volume\":{\"x\":"+(footprint.X+6)+",\"y\":"+(footprint.Y+5)+",\"width\":12,\"height\":30},\n"+
                "  \"root_axis_x\":"+tree.RootAxisX+",\n  \"lower_trunk_average_width\":"+N(tree.LowerTrunkWidth)+",\n"+
                "  \"middle_trunk_average_width\":"+N(tree.MiddleTrunkWidth)+",\n"+
                "  \"upper_trunk_average_width\":"+N(tree.UpperTrunkWidth)+",\n"+
                "  \"substantial_fork_count\":"+tree.Forks.Count(fork=>fork.Substantial)+",\n"+
                "  \"second_generation_fork_count\":"+tree.SecondGenerationForkCount+",\n"+
                "  \"climb_endpoint_count\":"+tree.ClimbEndpoints.Count+",\n"+
                "  \"branch_platform_count\":"+tree.Platforms.Count+",\n  \"branch_grab_count\":"+tree.BranchGrabCount+",\n"+
                "  \"longest_straight_limb_run\":"+tree.LongestStraightMainRun+",\n"+
                "  \"generation_milliseconds\":"+tree.GenerationMilliseconds+",\n"+
                "  \"route_count\":"+tree.Routes.Count+",\n  \"recovery_route_count\":"+tree.RecoveryRoutes.Count+",\n"+
                "  \"initial_jump_grab_cap_cells\":2,\n  \"continuous_trunk_climb\":true,\n"+
                "  \"branch_platform_top_only\":true,\n  \"branch_platform_grabbable\":false,\n"+
                "  \"reverse_route_required\":false,\n  \"itemless_completion_required\":true,\n"+
                "  \"local_search_margin_cells\":"+tree.LocalSearchMarginCells+",\n"+
                "  \"whole_world_copy_per_candidate\":"+tree.WholeWorldCopyPerCandidate+",\n"+
                "  \"whole_world_bfs_per_candidate\":"+tree.WholeWorldBfsPerCandidate+",\n"+
                "  \"all_endpoint_pair_comparison\":"+B(tree.AllEndpointPairComparison)+",\n"+
                "  \"tree_canopy_fix_ready\":"+B(tree.TreeCanopyFixReady)+",\n"+
                "  \"tree_grab_geometry_ready\":"+B(tree.TreeGrabGeometryReady)+",\n"+
                "  \"composed_geometry_ready\":false,\n  \"player_verified\":false\n}\n";
        }

        public static string CellsCsv(Sv5SpaceGraphPlan plan)=>Csv(
            "x,y,role,collision_type,before_value,final_value,owner,body_clear,head_clear,support_value,top_only,grab_enabled,movement_enabled,visual_only,plan_digest",
            plan.TreeGrab.Cells.Select(cell=>Row(cell.World.X,cell.World.Y,cell.ExportRole,
                cell.Role==Sv5TreeCellRole.BranchPlatform?"TOP_ONLY":cell.IsVisualOnly?"NONE":cell.ExportRole,
                Value(cell.BeforeValue),Value(cell.FinalValue),cell.Owner,cell.BodyClear,cell.HeadClear,Value(cell.SupportValue),
                cell.TopOnly,cell.GrabEnabled,cell.MovementEnabled,cell.IsVisualOnly,plan.Digest)));

        public static string LimbsCsv(Sv5SpaceGraphPlan plan)=>Csv(
            "limb_id,parent_limb_id,generation,start_x,start_y,end_x,end_y,progress_cells,leads_to_fork,terminal,cell_path,plan_digest",
            plan.TreeGrab.Limbs.Select(limb=>Row(limb.Id,limb.ParentId,limb.Generation,limb.Start.X,limb.Start.Y,
                limb.End.X,limb.End.Y,limb.ProgressCells,limb.LeadsToFork,limb.Terminal,
                string.Join(";",limb.Path.Select(point=>point.X+":"+point.Y)),plan.Digest)));

        public static string ForksCsv(Sv5SpaceGraphPlan plan)=>Csv(
            "fork_id,parent_limb_id,generation,x,y,child_limb_ids,substantial,plan_digest",
            plan.TreeGrab.Forks.Select(fork=>Row(fork.Id,fork.ParentLimbId,fork.Generation,fork.World.X,fork.World.Y,
                string.Join(";",fork.ChildLimbIds),fork.Substantial,plan.Digest)));

        public static string PlatformsCsv(Sv5SpaceGraphPlan plan)=>Csv(
            "platform_id,start_x,end_x,y,top_only,grabbable,landing_valid,plan_digest",
            plan.TreeGrab.Platforms.Select(platform=>Row(platform.Id,platform.StartX,platform.EndX,platform.Y,
                platform.TopOnly,platform.Grabbable,platform.LandingValid,plan.Digest)));

        public static string SurfacesCsv(Sv5SpaceGraphPlan plan)=>Csv(
            "x,y,face,anchor_id,anchor_type,hang_x,hang_y,collision_role,branch_platform_grab,plan_digest",
            plan.TreeGrab.Surfaces.Select(surface=>Row(surface.World.X,surface.World.Y,surface.ExportFace,surface.Id,
                surface.AnchorType,surface.HangAir.X,surface.HangAir.Y,"TRUNK_CLIMB",false,plan.Digest)));

        public static string MovementCsv(IEnumerable<Sv5TreeRoute> routes,Sv5SpaceGraphPlan plan)=>Csv(
            "route_id,sequence,from_x,from_y,to_x,to_y,state,move_type,traversal_state,body_clear,head_clear,contact_x,contact_y,itemless,initial_capture_rise,plan_digest",
            routes.SelectMany(route=>route.Edges.Select(edge=>Row(route.Id,edge.Sequence,edge.From.X,edge.From.Y,
                edge.To.X,edge.To.Y,State(edge),edge.ExportMoveType,edge.TraversalState,edge.BodyClear,edge.HeadClear,
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
            return "{\n  \"schema\":\"SV5_12_FIX01_TREE_VALIDATION/v1\",\n  \"status\":"+J(success?"PASS":"FAIL")+",\n"+
                "  \"success\":"+B(success)+",\n  \"distinct_physical_external_anchors\":"+anchors+",\n"+
                "  \"tree_route_count\":"+tree.Routes.Count+",\n  \"tree_recovery_route_count\":"+tree.RecoveryRoutes.Count+",\n"+
                "  \"tree_canopy_fix_ready\":"+B(tree.TreeCanopyFixReady)+",\n"+
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
            var text=SvgStart(hub,scale,"SV5_12_FIX01 Tree Canopy Overview");
            InnerContext(text,hub,scale);Envelope(text,hub,tree.ReservedVolume,scale);RootAxis(text,hub,tree,scale);
            foreach(var cell in tree.Cells.Where(cell=>cell.Role==Sv5TreeCellRole.LeafDecoration))Rect(text,hub,cell.World,"#5da85b",scale,0.78);
            foreach(var cell in tree.Cells.Where(cell=>cell.Role==Sv5TreeCellRole.DecorativeBranch))Rect(text,hub,cell.World,"#b27843",scale,0.62);
            foreach(var cell in tree.Cells.Where(cell=>cell.Role==Sv5TreeCellRole.TrunkClimb))Rect(text,hub,cell.World,"#75452d",scale,1.0);
            foreach(var cell in tree.Cells.Where(cell=>cell.Role==Sv5TreeCellRole.BranchPlatform))Rect(text,hub,cell.World,"#d89b52",scale,1.0);
            foreach(var fork in tree.Forks)Circle(text,hub,fork.World,"#ffdf64",scale);
            foreach(var limb in tree.Limbs)PointLabel(text,hub,limb.End,limb.Id,scale);
            foreach(var port in hub.Ports)Circle(text,hub,port.Anchor,"#c77dff",scale);
            Legend(text,new[]{"TRUNK_CLIMB - tapered multi-stem canopy","substantial and second-generation forks",
                "BRANCH_PLATFORM - landing only; no grab","upper-crown leaf decoration - no collision",
                "Hub inner / root axis / required ports"},
                new[]{"#75452d","#ffdf64","#d89b52","#5da85b","#c77dff"});
            return text.Append("</svg>\n").ToString();
        }

        public static string CollisionOverlay(Sv5SpaceGraphPlan plan)
        {
            var tree=plan.TreeGrab;var hub=plan.HubShell;int scale=12;
            var text=SvgStart(hub,scale,"SV5_12_FIX01 Tree Collision Overlay");
            InnerContext(text,hub,scale);Envelope(text,hub,tree.ReservedVolume,scale);RootAxis(text,hub,tree,scale);
            var colors=new Dictionary<Sv5TreeCellRole,string>{{Sv5TreeCellRole.TrunkClimb,"#e45756"},
                {Sv5TreeCellRole.BranchPlatform,"#4c78a8"},{Sv5TreeCellRole.DecorativeBranch,"#b279a2"},
                {Sv5TreeCellRole.LeafDecoration,"#59a14f"}};
            foreach(var cell in tree.Cells.Where(cell=>colors.ContainsKey(cell.Role)))Rect(text,hub,cell.World,colors[cell.Role],scale,0.85);
            foreach(var route in tree.Routes)foreach(var edge in route.Edges)Line(text,hub,edge.From,edge.To,
                route.Id.EndsWith("LEFT",StringComparison.Ordinal)?"#ffd166":"#64d8ff",scale);
            foreach(var route in tree.RecoveryRoutes)foreach(var edge in route.Edges)Line(text,hub,edge.From,edge.To,"#f28e2b",scale);
            foreach(var point in tree.RequiredEntrances)Circle(text,hub,point,"#ffffff",scale);
            foreach(var point in tree.RequiredExits)Circle(text,hub,point,"#00ff9d",scale);
            foreach(var port in hub.Ports)Circle(text,hub,port.Anchor,"#c77dff",scale);
            Legend(text,new[]{"TRUNK_CLIMB","BRANCH_PLATFORM - landing only; no grab","DECORATIVE_BRANCH - no collision",
                "LEAF_DECORATION - no collision","required entry / itemless route / exit","recovery / Hub ports"},
                new[]{"#e45756","#4c78a8","#b279a2","#59a14f","#ffd166","#f28e2b"});
            return text.Append("</svg>\n").ToString();
        }

        private static string State(Sv5MovementEdge edge)=>edge.MoveType==Sv5PlatformerMoveType.JumpGrab?"JUMP_GRAB":
            edge.MoveType==Sv5PlatformerMoveType.Drop?"DROP":edge.TraversalState=="TRUNK_CLIMB"?"TRUNK_CLIMB":"BRANCH_LAND";
        private static bool Clear(Sv5TreeGrabPlan tree,Sv5SpecialWorldPoint point)=>
            Sv5LoopTopology.ValueAt(tree.FinalOccupancy,point)==Sv5InfillCellValue.Air&&
            Sv5LoopTopology.ValueAt(tree.FinalOccupancy,new Sv5SpecialWorldPoint(point.X,point.Y+1))==Sv5InfillCellValue.Air;
        private static string Bounds(Sv5SpaceBounds value)=>"\"x\":"+value.X+",\"y\":"+value.Y+",\"width\":"+value.Width+",\"height\":"+value.Height;
        private static StringBuilder SvgStart(Sv5HubShellPlan hub,int scale,string title)=>new StringBuilder(
            "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 "+hub.Footprint.Width*scale+" "+hub.Footprint.Height*scale+
            "\"><rect width=\"100%\" height=\"100%\" fill=\"#101820\"/><text x=\"6\" y=\"14\" fill=\"white\" font-size=\"9\">"+title+"</text>");
        private static void Envelope(StringBuilder text,Sv5HubShellPlan hub,Sv5SpaceBounds volume,int scale)
        {text.Append("<rect x=\"").Append((volume.X-hub.Footprint.X)*scale).Append("\" y=\"")
            .Append((hub.Footprint.MaxYExclusive-volume.MaxYExclusive)*scale).Append("\" width=\"").Append(volume.Width*scale)
            .Append("\" height=\"").Append(volume.Height*scale).Append("\" fill=\"none\" stroke=\"#6f7f8f\" stroke-width=\"1\"/>");}
        private static void InnerContext(StringBuilder text,Sv5HubShellPlan hub,int scale)
        {var inner=new Sv5SpaceBounds(hub.Footprint.X+6,hub.Footprint.Y+5,12,30);
            text.Append("<rect x=\"").Append((inner.X-hub.Footprint.X)*scale).Append("\" y=\"")
                .Append((hub.Footprint.MaxYExclusive-inner.MaxYExclusive)*scale).Append("\" width=\"").Append(inner.Width*scale)
                .Append("\" height=\"").Append(inner.Height*scale).Append("\" fill=\"none\" stroke=\"#c77dff\" stroke-width=\"1\" stroke-dasharray=\"3 2\"/>");}
        private static void RootAxis(StringBuilder text,Sv5HubShellPlan hub,Sv5TreeGrabPlan tree,int scale)
        {double x=(tree.RootAxisX-hub.Footprint.X+0.5)*scale;
            text.Append("<line x1=\"").Append(x.ToString("0.0",CultureInfo.InvariantCulture)).Append("\" y1=\"")
                .Append((hub.Footprint.MaxYExclusive-tree.ReservedVolume.MaxYExclusive)*scale).Append("\" x2=\"")
                .Append(x.ToString("0.0",CultureInfo.InvariantCulture)).Append("\" y2=\"")
                .Append((hub.Footprint.MaxYExclusive-tree.ReservedVolume.Y)*scale).Append("\" stroke=\"#ffffff\" stroke-width=\"0.7\" stroke-dasharray=\"2 2\"/>");}
        private static void Rect(StringBuilder text,Sv5HubShellPlan hub,Sv5SpecialWorldPoint point,string color,int scale,double opacity)
        {text.Append("<rect x=\"").Append((point.X-hub.Footprint.X)*scale).Append("\" y=\"")
            .Append((hub.Footprint.MaxYExclusive-1-point.Y)*scale).Append("\" width=\"").Append(scale)
            .Append("\" height=\"").Append(scale).Append("\" fill=\"").Append(color).Append("\" opacity=\"")
            .Append(opacity.ToString("0.00",CultureInfo.InvariantCulture)).Append("\"/>");}
        private static void Circle(StringBuilder text,Sv5HubShellPlan hub,Sv5SpecialWorldPoint point,string color,int scale)
        {text.Append("<circle cx=\"").Append((point.X-hub.Footprint.X+0.5)*scale).Append("\" cy=\"")
            .Append((hub.Footprint.MaxYExclusive-point.Y-0.5)*scale).Append("\" r=\"3\" fill=\"").Append(color).Append("\"/>");}
        private static void Line(StringBuilder text,Sv5HubShellPlan hub,Sv5SpecialWorldPoint from,Sv5SpecialWorldPoint to,string color,int scale)
        {text.Append("<line x1=\"").Append((from.X-hub.Footprint.X+0.5)*scale).Append("\" y1=\"")
            .Append((hub.Footprint.MaxYExclusive-from.Y-0.5)*scale).Append("\" x2=\"")
            .Append((to.X-hub.Footprint.X+0.5)*scale).Append("\" y2=\"")
            .Append((hub.Footprint.MaxYExclusive-to.Y-0.5)*scale).Append("\" stroke=\"").Append(color)
            .Append("\" stroke-width=\"2\"/>");}
        private static void PointLabel(StringBuilder text,Sv5HubShellPlan hub,Sv5SpecialWorldPoint point,string label,int scale)
        {text.Append("<text x=\"").Append((point.X-hub.Footprint.X+0.15)*scale).Append("\" y=\"")
            .Append((hub.Footprint.MaxYExclusive-point.Y-0.15)*scale).Append("\" fill=\"#ffffff\" font-size=\"3.5\">")
            .Append(label).Append("</text>");}
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
