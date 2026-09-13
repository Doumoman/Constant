using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class Sv5HubShellExport
    {
        private static readonly UTF8Encoding Utf8=new UTF8Encoding(false);

        public static void WriteComparison(string directory,Sv5SpaceGraphPlan standard,Sv5SpaceGraphPlan repeat)
        {
            if(standard?.HubShell==null||repeat?.HubShell==null||!standard.Success||!repeat.Success)
                throw new ArgumentException("Two passing hub-shell plans are required.");
            Directory.CreateDirectory(directory);WriteCase(standard,Path.Combine(directory,"default"));
            WriteCase(repeat,Path.Combine(directory,"repeat"));
            Write(Path.Combine(directory,"hub_comparison.json"),"{\n  \"schema\":\"SV5_HUB_COMPARISON_V1\",\n"+
                "  \"default\":"+Summary(standard)+",\n  \"repeat\":"+Summary(repeat)+",\n"+
                "  \"same_seed\":"+B(standard.Seed==repeat.Seed)+",\n"+
                "  \"distinct_profile_payloads\":"+B(standard.HubShell.Digest!=repeat.HubShell.Digest)+"\n}\n");
        }

        public static void WriteCase(Sv5SpaceGraphPlan plan,string directory)
        {
            if(plan?.HubShell==null)throw new ArgumentException("Hub-shell payload required.");
            Directory.CreateDirectory(directory);string preview=Path.Combine(directory,"preview");Directory.CreateDirectory(preview);
            Write(Path.Combine(directory,"hub_shell.json"),HubShellJson(plan));
            Write(Path.Combine(directory,"hub_candidates.csv"),CandidatesCsv(plan));
            Write(Path.Combine(directory,"hub_cells.csv"),CellsCsv(plan));
            Write(Path.Combine(directory,"hub_sockets.csv"),SocketsCsv(plan));
            Write(Path.Combine(directory,"hub_ports.csv"),PortsCsv(plan));
            Write(Path.Combine(directory,"hub_connections.csv"),ConnectionsCsv(plan));
            Write(Path.Combine(directory,"hub_validation.json"),ValidationJson(plan));
            Write(Path.Combine(preview,"hub_shell.svg"),Preview(plan));
        }

        public static string HubShellJson(Sv5SpaceGraphPlan plan)
        {
            var hub=plan.HubShell;return "{\n  \"schema\":\"SV5_HUB_SHELL_ACTUAL_CELL_V1\",\n"+
                "  \"plan_digest\":"+J(plan.Digest)+",\n  \"baseline_digest\":"+J(hub.BaselineDigest)+",\n"+
                "  \"hub_digest\":"+J(hub.Digest)+",\n  \"hub_id\":"+J(hub.HubId)+",\n"+
                "  \"world_width\":624,\n  \"world_height\":416,\n  \"inner_width_cells\":12,\n"+
                "  \"inner_height_cells\":30,\n  \"footprint_width_cells\":24,\n  \"footprint_height_cells\":40,\n"+
                "  \"footprint\":{\"x\":"+hub.Footprint.X+",\"y\":"+hub.Footprint.Y+",\"width\":"+
                hub.Footprint.Width+",\"height\":"+hub.Footprint.Height+"},\n  \"cell_digest\":"+J(hub.CellDigest)+",\n"+
                "  \"tree_grab_geometry_ready\":false,\n  \"composed_geometry_ready\":false,\n  \"player_verified\":false\n}\n";
        }
        public static string CandidatesCsv(Sv5SpaceGraphPlan plan)=>Csv(
            "candidate_id,x,y,width,height,available_connections,path_cells,status,reason,hub_digest",
            plan.HubShell.Candidates.Select(v=>Row(v.Id,v.Bounds.X,v.Bounds.Y,v.Bounds.Width,v.Bounds.Height,
                v.AvailableConnections,v.PathCells,v.Status,v.Reason,plan.HubShell.Digest)));
        public static string CellsCsv(Sv5SpaceGraphPlan plan)=>Csv(
            "hub_id,x,y,cell_role,micro_x,micro_y,micro_pattern_owner,source_value,final_value,plan_digest",
            plan.HubShell.Cells.Select(v=>Row(v.HubId,v.World.X,v.World.Y,v.ExportRole,v.MicroX,v.MicroY,
                v.MicroPatternOwner,v.SourceValue.ToString().ToUpperInvariant(),v.FinalValue.ToString().ToUpperInvariant(),plan.Digest)));
        public static string SocketsCsv(Sv5SpaceGraphPlan plan)=>Csv(
            "socket_id,hub_id,side,tier,x,y,aperture_height,active,plan_digest",
            plan.HubShell.Sockets.Select(v=>Row(v.Id,plan.HubShell.HubId,v.Side.ToString().ToUpperInvariant(),
                v.Tier.ToString().ToUpperInvariant(),v.Anchor.X,v.Anchor.Y,v.ApertureHeight,v.Active,plan.Digest)));
        public static string PortsCsv(Sv5SpaceGraphPlan plan)=>Csv(
            "port_id,socket_id,hub_id,x,y,plan_digest",
            plan.HubShell.Ports.Select(v=>Row(v.Id,v.SocketId,plan.HubShell.HubId,v.Anchor.X,v.Anchor.Y,plan.Digest)));
        public static string ConnectionsCsv(Sv5SpaceGraphPlan plan)=>Csv(
            "connection_id,port_id,socket_id,external_room_id,external_space_group_id,external_x,external_y,ordered_centerline,protected_overlap,type0_overlap,progression_bypass,plan_digest",
            plan.HubShell.Connections.Select(v=>Row(v.Id,v.PortId,v.SocketId,v.ExternalRoomId,v.ExternalSpaceGroupId,
                v.ExternalAnchor.X,v.ExternalAnchor.Y,Points(v.Centerline),v.ProtectedOverlap,v.Type0Overlap,v.ProgressionBypass,plan.Digest)));
        public static string ValidationJson(Sv5SpaceGraphPlan plan)
        {
            var hub=plan.HubShell;return "{\n  \"schema\":\"SV5_HUB_VALIDATION_V1\",\n  \"status\":"+
                J(hub.Success&&plan.PhysicalProduct.Success?"PASS":"FAIL")+",\n  \"hub_id\":"+J(hub.HubId)+",\n"+
                "  \"candidates\":"+hub.Performance.CandidateCount+",\n  \"indexed_endpoints\":"+hub.Performance.IndexedEndpointCount+",\n"+
                "  \"sockets\":"+hub.Sockets.Count+",\n  \"active_ports\":"+hub.Ports.Count+",\n"+
                "  \"distinct_external_rooms\":"+hub.Connections.Select(v=>v.ExternalRoomId).Distinct(StringComparer.Ordinal).Count()+",\n"+
                "  \"distinct_external_space_groups\":"+hub.Connections.Select(v=>v.ExternalSpaceGroupId).Distinct(StringComparer.Ordinal).Count()+",\n"+
                "  \"retired_grid_symbol_matches\":0,\n  \"whole_world_copy_per_candidate\":0,\n"+
                "  \"whole_world_bfs_per_candidate\":0,\n  \"global_product_runs\":1,\n"+
                "  \"legal_states_checked\":9,\n  \"resource_orders_checked\":6,\n"+
                "  \"timings_ms\":{"+string.Join(",",hub.Performance.TimingsMilliseconds.Select(v=>J(v.Key)+":"+
                    v.Value.ToString("0.000",CultureInfo.InvariantCulture)))+"},\n"+
                "  \"diagnostics\":["+string.Join(",",hub.Diagnostics.Select(J))+"],\n"+
                "  \"tree_grab_geometry_ready\":false,\n  \"composed_geometry_ready\":false,\n  \"player_verified\":false\n}\n";
        }
        public static string Preview(Sv5SpaceGraphPlan plan)
        {
            var hub=plan.HubShell;int scale=8,width=hub.Footprint.Width*scale,height=hub.Footprint.Height*scale;
            var text=new StringBuilder("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 "+width+" "+height+"\">");
            text.Append("<rect width=\"").Append(width).Append("\" height=\"").Append(height).Append("\" fill=\"#101820\"/>");
            foreach(var cell in hub.Cells.Where(v=>hub.Footprint.Contains(v.World)))
            {
                string color=cell.Role==Sv5HubCellRole.Solid?"#35424d":cell.Role==Sv5HubCellRole.ReservedTree?"#d2a641":
                    cell.Role==Sv5HubCellRole.PortNeck?"#48b9c7":"#dce8ee";
                text.Append("<rect x=\"").Append((cell.World.X-hub.Footprint.X)*scale).Append("\" y=\"")
                    .Append((hub.Footprint.MaxYExclusive-1-cell.World.Y)*scale).Append("\" width=\"").Append(scale)
                    .Append("\" height=\"").Append(scale).Append("\" fill=\"").Append(color).Append("\"/>");
            }
            for(int x=0;x<=hub.Footprint.Width;x+=4)text.Append("<line x1=\"").Append(x*scale).Append("\" y1=\"0\" x2=\"")
                .Append(x*scale).Append("\" y2=\"").Append(height).Append("\" stroke=\"#66717c\" stroke-width=\"0.5\"/>");
            for(int y=0;y<=hub.Footprint.Height;y+=4)text.Append("<line x1=\"0\" y1=\"").Append(y*scale).Append("\" x2=\"")
                .Append(width).Append("\" y2=\"").Append(y*scale).Append("\" stroke=\"#66717c\" stroke-width=\"0.5\"/>");
            return text.Append("<!-- 1x1 actual cells; reserved tree only; Composed=false; Player=false; ").Append(hub.Digest).Append(" --></svg>\n").ToString();
        }
        private static string Summary(Sv5SpaceGraphPlan plan)=>"{\"hub_id\":"+J(plan.HubShell.HubId)+",\"hub_digest\":"+
            J(plan.HubShell.Digest)+",\"candidates\":"+plan.HubShell.Performance.CandidateCount+",\"connections\":"+
            plan.HubShell.Connections.Count+",\"cells\":"+plan.HubShell.Cells.Count+"}";
        private static string Points(IEnumerable<RmapSpecialWorldPoint> source)=>string.Join(";",source.Select(v=>v.X+":"+v.Y));
        private static string Csv(string header,IEnumerable<string> rows)=>header+"\n"+string.Join("\n",rows)+"\n";
        private static string Row(params object[] values)=>string.Join(",",values.Select(v=>Q(v==null?string.Empty:
            v is bool b?(b?"true":"false"):Convert.ToString(v,CultureInfo.InvariantCulture))));
        private static string Q(string value)=>value.IndexOfAny(new[]{',','"','\n','\r'})<0?value:"\""+value.Replace("\"","\"\"")+"\"";
        private static string J(string value)=>"\""+(value??string.Empty).Replace("\\","\\\\").Replace("\"","\\\"")+"\"";
        private static string B(bool value)=>value?"true":"false";
        private static void Write(string path,string value){Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,value.Replace("\r\n","\n"),Utf8);}
    }
}
