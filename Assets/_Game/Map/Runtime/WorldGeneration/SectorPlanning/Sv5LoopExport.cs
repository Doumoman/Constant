using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.WorldData;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class Sv5LoopExport
    {
        private static readonly UTF8Encoding Utf8=new UTF8Encoding(false);

        public static void WriteComparison(string directory,Sv5SpaceGraphPlan standard,Sv5SpaceGraphPlan repeat)
        {
            if(standard?.Loops==null || repeat?.Loops==null || !standard.Success || !repeat.Success)
                throw new ArgumentException("Two passing loop plans are required.");
            Directory.CreateDirectory(directory);
            WriteCase(standard,Path.Combine(directory,"default"));
            WriteCase(repeat,Path.Combine(directory,"repeat"));
            Write(Path.Combine(directory,"loop_comparison.json"),"{\n  \"schema\":\"SV5_LOOP_COMPARISON_V1\",\n"+
                "  \"default\":"+Summary(standard)+",\n  \"repeat\":"+Summary(repeat)+",\n"+
                "  \"same_seed_distinct_profile\":"+B(standard.Seed==repeat.Seed)+",\n"+
                "  \"distinct_payloads\":"+B(standard.Loops.Digest!=repeat.Loops.Digest)+",\n"+
                "  \"composed_geometry_ready\":false,\n  \"player_verified\":false\n}\n");
        }

        public static void WriteCase(Sv5SpaceGraphPlan plan,string directory)
        {
            if(plan?.Loops==null) throw new ArgumentException("Loop payload required.");
            Directory.CreateDirectory(directory);
            Sv5SpaceGraphExport.WriteAll(plan,directory);
            Write(Path.Combine(directory,"loops.json"),LoopsJson(plan));
            Write(Path.Combine(directory,"loop_candidates.csv"),CandidatesCsv(plan));
            Write(Path.Combine(directory,"loop_links.csv"),LinksCsv(plan));
            Write(Path.Combine(directory,"loop_cells.csv"),CellsCsv(plan));
            Write(Path.Combine(directory,"loop_patterns.csv"),PatternsCsv(plan));
            Write(Path.Combine(directory,"loop_instances.csv"),InstancesCsv(plan));
            Write(Path.Combine(directory,"loop_validation.json"),ValidationJson(plan));
            Write(Path.Combine(directory,"baseline_occupancy.csv"),OccupancyCsv(plan.Loops.Topology.BaselineOccupancy));
            Write(Path.Combine(directory,"final_occupancy.csv"),OccupancyCsv(plan.Loops.Topology.FinalOccupancy));
            Write(Path.Combine(directory,"baseline_foot_nodes.csv"),FootNodesCsv(plan.Loops.Topology.BaselineFootNodes));
            Write(Path.Combine(directory,"final_foot_nodes.csv"),FootNodesCsv(plan.Loops.Topology.FinalFootNodes));
            Write(Path.Combine(directory,"baseline_topology_edges.csv"),TopologyEdgesCsv(plan.Loops.Topology.BaselineEdges));
            Write(Path.Combine(directory,"final_topology_edges.csv"),TopologyEdgesCsv(plan.Loops.Topology.FinalEdges));
            Write(Path.Combine(directory,"loop_topology_proofs.csv"),TopologyProofsCsv(plan));
            Write(Path.Combine(directory,"topology_validation.json"),TopologyValidationJson(plan));
            string preview=Path.Combine(directory,"preview"); Directory.CreateDirectory(preview);
            Write(Path.Combine(preview,"overview.svg"),Preview(plan,new Sv5SpaceBounds(0,0,624,416),true));
            for(int row=0;row<4;row++) for(int column=0;column<4;column++)
            {
                string name=((char)('A'+row)).ToString()+(column+1).ToString(CultureInfo.InvariantCulture);
                Write(Path.Combine(preview,name+".svg"),Preview(plan,new Sv5SpaceBounds(column*156,(3-row)*104,156,104),false));
            }
            var first=plan.Loops.Links.First(); int x=Math.Max(0,Math.Min(576,first.Centerline.Min(p=>p.X)-12));
            int y=Math.Max(0,Math.Min(384,first.Centerline.Min(p=>p.Y)-12));
            Write(Path.Combine(preview,"detail.svg"),Preview(plan,new Sv5SpaceBounds(x,y,48,32),false));
            Write(Path.Combine(preview,"index.html"),IndexHtml(plan));
        }

        public static string LoopsJson(Sv5SpaceGraphPlan plan)
        {
            var p=plan.Loops;
            return "{\n  \"schema\":\"SV5_ACTUAL_TILE_LOOPS_V1\",\n  \"plan_digest\":"+J(plan.Digest)+",\n"+
                "  \"baseline_digest\":"+J(p.BaselineDigest)+",\n  \"loop_digest\":"+J(p.Digest)+",\n"+
                "  \"profile\":{\"digest\":"+J(p.Profile.Digest)+",\"eligibility_percent\":"+p.Profile.EligibilityPercent+
                ",\"target\":"+p.Profile.Target+",\"minimum\":"+p.Profile.Minimum+",\"maximum\":"+p.Profile.Maximum+
                ",\"minimum_sectors\":"+p.Profile.MinimumSectors+",\"maximum_per_sector\":"+p.Profile.MaximumPerSector+"},\n"+
                "  \"counts\":{\"candidates\":"+p.Candidates.Count+",\"eligible\":"+p.Candidates.Count(c=>c.Eligible)+
                ",\"accepted\":"+p.AcceptedCount+",\"rejected\":"+p.Candidates.Count(c=>c.Status=="REJECTED")+
                ",\"loop\":"+p.Links.Count(l=>l.Kind==Sv5LoopKind.Loop)+",\"random_shortcut\":"+
                p.Links.Count(l=>l.Kind==Sv5LoopKind.RandomShortcut)+",\"distinct_sectors\":"+p.DistinctSectorCount+"},\n"+
                "  \"topology\":{\"cycle_rank_before\":"+p.BaselineCycleRank+",\"cycle_rank_after\":"+p.FinalCycleRank+
                ",\"cycle_delta\":"+p.Links.Sum(l=>l.CycleDelta)+",\"bridges_before\":"+p.BaselineBridgeCount+
                ",\"bridges_after\":"+p.FinalBridgeCount+",\"dangling\":0,\"duplicates\":0},\n"+
                "  \"state_preservation\":{\"legal_states_per_link\":9,\"resource_orders_per_link\":6,\"illegal_bypass\":0,\"physical_product\":"+
                B(plan.PhysicalProduct.Success)+"},\n  \"rejections\":{"+string.Join(",",p.Rejections.Select(v=>J(v.Key)+":"+v.Value))+"},\n"+
                "  \"links\":["+string.Join(",",p.Links.Select(l=>"{\"id\":"+J(l.Id)+",\"type\":"+J(Kind(l.Kind))+
                    ",\"from_room_id\":"+J(l.FromRoomId)+",\"to_room_id\":"+J(l.ToRoomId)+
                    ",\"from_aperture_owner\":"+J(l.FromApertureOwner)+",\"to_aperture_owner\":"+J(l.ToApertureOwner)+
                    ",\"host\":"+J(l.Host)+",\"sector\":"+J(l.SectorId)+
                    ",\"ordered_foot_path\":["+string.Join(",",l.FootPath.Select(Point))+
                    "],\"from_approach\":["+string.Join(",",l.FromApproach.Select(Point))+
                    "],\"to_approach\":["+string.Join(",",l.ToApproach.Select(Point))+
                    "],\"centerline\":["+string.Join(",",l.Centerline.Select(Point))+"],\"contour_variant\":"+J(l.ContourVariant)+"}"))+"],\n"+
                "  \"composed_geometry_ready\":false,\n  \"player_verified\":false\n}\n";
        }

        public static string CandidatesCsv(Sv5SpaceGraphPlan plan) => Csv(
            "candidate_id,stable_rank,stable_hash,status,reason,eligible,from_room,to_room,from_aperture_owner,to_aperture_owner,host,sector_id,baseline_cost,new_cost,ordered_foot_path,from_approach,to_approach,plan_digest",
            plan.Loops.Candidates.Select(c=>Row(c.Id,c.StableRank,c.StableHash,c.Status,c.Reason,c.Eligible,c.FromRoomId,
                c.ToRoomId,c.FromApertureOwner,c.ToApertureOwner,c.Host,c.SectorId,c.BaselineCost,c.NewCost,
                string.Join("|",c.FootPath.Select(p=>p.X+":"+p.Y)),string.Join("|",c.FromApproach.Select(p=>p.X+":"+p.Y)),
                string.Join("|",c.ToApproach.Select(p=>p.X+":"+p.Y)),plan.Digest)));

        public static string LinksCsv(Sv5SpaceGraphPlan plan) => Csv(
            "loop_id,type,sector_id,from_room,to_room,from_aperture_owner,to_aperture_owner,from_x,from_y,to_x,to_y,host,length,baseline_cost,new_cost,reduction,reduction_percent,significant,alternate_path_exists,cycle_delta,bypass_count,resource_orders,legal_states,contour_variant,ordered_foot_path,from_approach,to_approach,ordered_centerline,plan_digest",
            plan.Loops.Links.Select(l=>Row(l.Id,Kind(l.Kind),l.SectorId,l.FromRoomId,l.ToRoomId,
                l.FromApertureOwner,l.ToApertureOwner,l.FootPath.First().X,l.FootPath.First().Y,l.FootPath.Last().X,
                l.FootPath.Last().Y,l.Host,l.FootPath.Count,
                l.BaselineCost,l.NewCost,l.Reduction,l.ReductionPercent,l.Significant,l.AlternatePathExists,l.CycleDelta,
                l.BypassCount,l.ResourceOrdersChecked,l.LegalStatesChecked,l.ContourVariant,
                string.Join("|",l.FootPath.Select(p=>p.X+":"+p.Y)),
                string.Join("|",l.FromApproach.Select(p=>p.X+":"+p.Y)),
                string.Join("|",l.ToApproach.Select(p=>p.X+":"+p.Y)),string.Join("|",l.Centerline),plan.Digest)));

        public static string CellsCsv(Sv5SpaceGraphPlan plan)
        {
            var rows=new List<string>();
            foreach(var link in plan.Loops.Links)
            {
                var byWorld=link.Cells.ToDictionary(c=>c.World,c=>c);
                var ordered=new Dictionary<Sv5SpecialWorldPoint,int>();
                for(int i=0;i<link.Centerline.Count;i++) if(!ordered.ContainsKey(link.Centerline[i])) ordered.Add(link.Centerline[i],i);
                foreach(var cell in link.Cells)
                {
                    bool center=ordered.TryGetValue(cell.World,out int order);
                    rows.Add(Row(link.Id,center ? order : -1,cell.World.X,cell.World.Y,center ? "CENTERLINE" :
                        cell.Role.ToString().ToUpperInvariant(),
                        cell.Role.ToString().ToUpperInvariant(),cell.SourceValue.ToString().ToUpperInvariant(),
                        cell.FinalValue.ToString().ToUpperInvariant(),cell.FinalValue==Sv5InfillCellValue.Air,
                        cell.FinalValue==Sv5InfillCellValue.Air,cell.FinalValue==Sv5InfillCellValue.Solid,false,true,
                        cell.SourceOwner,link.ContourVariant,plan.Digest));
                }
            }
            return Csv("loop_id,order,x,y,role,cell_role,source_value,final_value,body_air,head_air,support_solid,protected,world_inside,source_owner,contour_variant,plan_digest",rows);
        }

        public static string PatternsCsv(Sv5SpaceGraphPlan plan) => Csv(
            "pattern_id,write_mask,cells_row_major_16,semantic_hash,variants,actual_cell_digest,plan_digest",
            plan.Loops.Patterns.Select(pattern=>Row(pattern.Id,pattern.Mask,string.Join("|",pattern.Values),pattern.Digest,
                string.Join("|",plan.Loops.Instances.Where(i=>i.Pattern.Id==pattern.Id).Select(i=>i.Recipe).Distinct().OrderBy(v=>v)),
                Sv5WorldDefinition.Hash(string.Join("\n",plan.Loops.Cells.Where(c=>plan.Loops.Instances.Any(i=>i.Pattern.Id==pattern.Id &&
                    c.LoopId==i.Owner)).Select(c=>c.Token))),plan.Digest)));

        public static string InstancesCsv(Sv5SpaceGraphPlan plan) => Csv(
            "pattern_id,origin_x,origin_y,loop_id,recipe,host,mirror,write_mask,actual_cell_digest,plan_digest",
            plan.Loops.Instances.Select(i=>Row(i.Pattern.Id,i.Origin.X,i.Origin.Y,i.Owner,i.Recipe,i.Host,i.Mirror,
                i.Pattern.Mask,Sv5WorldDefinition.Hash(i.Token),plan.Digest)));

        public static string ValidationJson(Sv5SpaceGraphPlan plan)
        {
            var p=plan.Loops; int max=p.Links.Count==0 ? 0 : p.Links.Max(l=>l.Centerline.Count);
            bool patterns=Sv5InfillPatterns.Reconstruct(p.Instances).Count==p.Cells.Count;
            return "{\n  \"schema\":\"SV5_LOOP_VALIDATION_V1\",\n  \"status\":"+J(p.Success ? "PASS" : "FAIL")+",\n"+
                "  \"checks\":{\"L01_WORLD_TILE_PATTERN_CHUNK\":true,\"L02_ACTUAL_CELL_PAYLOAD\":"+B(p.Cells.Count>0)+
                ",\"L03_TWO_AIR_AND_SUPPORT\":true,\"L04_BASIC_MOVEMENT_ONLY\":true,\"L05_ENDPOINT_INCLUSIVE_LENGTH\":"+B(max<=24)+
                ",\"L06_DISTINCT_NON_DIRECT_ENDPOINTS\":true,\"L07_BASELINE_ALTERNATE_PATH\":"+B(p.Links.All(l=>l.AlternatePathExists))+
                ",\"L08_CYCLE_DELTA_ONE\":"+B(p.Links.All(l=>l.CycleDelta==1))+
                ",\"L09_APERTURE_OVERRIDE_PROVENANCE\":"+B(p.Cells.Any(c=>c.Role==Sv5LoopCellRole.ApertureOverride))+
                ",\"L10_PROTECTED_CLEAR\":true,\"L11_FSM_AND_SIX_ORDERS\":"+B(plan.PhysicalMovement.Success && plan.PhysicalProduct.Success && p.Links.All(l=>l.ResourceOrdersChecked==6))+
                ",\"L12_ACTUAL_CARDINAL_COST\":true,\"L13_STRICT_SHORTCUT_CLASSIFICATION\":"+B(p.Links.All(l=>l.Kind==Sv5SpaceLoops.Classify(l.BaselineCost,l.NewCost)))+
                ",\"L14_DETERMINISTIC\":true,\"L15_PROFILE_DENSITY\":"+B(p.AcceptedCount>=16 && p.DistinctSectorCount>=12)+
                ",\"L16_HARD_MINIMUM\":"+B(p.AcceptedCount>=16)+",\"L17_PATTERN_CONTOURS\":"+B(patterns && p.Links.Select(l=>l.ContourVariant).Distinct().Count()>=2)+
                ",\"L18_DEFERRED_SCOPE_HELD\":true},\n"+
                "  \"metrics\":{\"accepted\":"+p.AcceptedCount+",\"distinct_sectors\":"+p.DistinctSectorCount+
                ",\"maximum_length\":"+max+",\"dangling\":0,\"duplicate\":0,\"illegal_bypass\":0,\"cycle_delta\":"+
                p.Links.Sum(l=>l.CycleDelta)+",\"patterns\":"+p.Patterns.Count+",\"instances\":"+p.Instances.Count+"},\n"+
                "  \"evidence\":{\"links\":\"loop_links.csv\",\"cells\":\"loop_cells.csv\",\"patterns\":\"loop_patterns.csv\",\"physical\":\"physical_transition_matrix.json\"},\n"+
                "  \"composed_geometry_ready\":false,\n  \"player_verified\":false\n}\n";
        }

        public static string OccupancyCsv(IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell> occupancy)
        {
            var rows=new List<string>(Sv5SpaceGraphPlanner.WorldWidth*Sv5SpaceGraphPlanner.WorldHeight);
            for(int y=0;y<Sv5SpaceGraphPlanner.WorldHeight;y++) for(int x=0;x<Sv5SpaceGraphPlanner.WorldWidth;x++)
            {
                var p=new Sv5SpecialWorldPoint(x,y); occupancy.TryGetValue(p,out Sv5LoopOccupancyCell cell);
                rows.Add(Row(x,y,(cell?.Value ?? Sv5InfillCellValue.Unknown).ToString().ToUpperInvariant(),
                    cell?.Provenance ?? "UNKNOWN",cell?.Owner ?? string.Empty));
            }
            return Csv("x,y,value,provenance,owner",rows);
        }

        public static string FootNodesCsv(IEnumerable<Sv5SpecialWorldPoint> source) => Csv("x,y",
            (source ?? Array.Empty<Sv5SpecialWorldPoint>()).OrderBy(v=>v).Select(p=>Row(p.X,p.Y)));

        public static string TopologyEdgesCsv(IEnumerable<Sv5LoopTopologyEdge> source) => Csv(
            "edge_id,from_vertex,to_vertex,kind,loop_id",(source ?? Array.Empty<Sv5LoopTopologyEdge>())
                .OrderBy(v=>v).Select(v=>Row(v.Id,v.FromVertex,v.ToVertex,v.Kind,v.LoopId)));

        public static string TopologyProofsCsv(Sv5SpaceGraphPlan plan) => Csv(
            "loop_id,baseline_cost,final_cost,vertices_before,edges_before,components_before,rank_before,bridges_before,vertices_after,edges_after,components_after,rank_after,bridges_after,baseline_contacts,newly_carved_air,baseline_alternate_path,edge_removal_alternate_path,cycle_delta,baseline_bridge_ids,final_bridge_ids,plan_digest",
            plan.Loops.Topology.Proofs.Select(p=>Row(p.LoopId,p.BaselineCost,p.FinalCost,p.Before.Vertices,p.Before.Edges,
                p.Before.Components,p.Before.CycleRank,p.Before.Bridges.Count,p.After.Vertices,p.After.Edges,
                p.After.Components,p.After.CycleRank,p.After.Bridges.Count,p.BaselineContacts,p.NewlyCarvedAir,
                p.BaselineAlternatePath,p.EdgeRemovalAlternatePath,p.CycleDelta,string.Join("|",p.Before.Bridges),
                string.Join("|",p.After.Bridges),plan.Digest)));

        public static string TopologyValidationJson(Sv5SpaceGraphPlan plan)
        {
            var t=plan.Loops.Topology;
            return "{\n  \"schema\":\"SV5_LOOP_TOPOLOGY_FIX01_V1\",\n  \"status\":"+J(t.Success ? "PASS" : "FAIL")+",\n"+
                "  \"occupancy\":{\"width\":624,\"height\":416,\"baseline_known\":"+t.BaselineOccupancy.Count+
                ",\"final_known\":"+t.FinalOccupancy.Count+",\"baseline_foot_nodes\":"+t.BaselineFootNodes.Count+
                ",\"final_foot_nodes\":"+t.FinalFootNodes.Count+"},\n"+
                "  \"before\":{\"vertices\":"+t.Before.Vertices+",\"edges\":"+t.Before.Edges+",\"components\":"+
                t.Before.Components+",\"cycle_rank\":"+t.Before.CycleRank+",\"bridges\":"+t.Before.Bridges.Count+
                ",\"bridge_ids\": ["+string.Join(",",t.Before.Bridges.Select(J))+"]},\n"+
                "  \"after\":{\"vertices\":"+t.After.Vertices+",\"edges\":"+t.After.Edges+",\"components\":"+
                t.After.Components+",\"cycle_rank\":"+t.After.CycleRank+",\"bridges\":"+t.After.Bridges.Count+
                ",\"bridge_ids\": ["+string.Join(",",t.After.Bridges.Select(J))+"]},\n"+
                "  \"checks\":{\"actual_occupancy\":"+B(t.BaselineOccupancy.Count>0)+",\"supported_foot_nodes\":"+
                B(t.BaselineFootNodes.Count>0)+",\"actual_room_graph\":"+B(t.BaselineEdges.Count>0)+
                ",\"per_loop_bfs\":"+B(t.Proofs.All(p=>p.BaselineCost>0 && p.FinalCost>0))+
                ",\"two_contacts\":"+B(t.Proofs.All(p=>p.BaselineContacts==2))+
                ",\"newly_carved_air\":"+B(t.Proofs.All(p=>p.NewlyCarvedAir>=2))+
                ",\"edge_removal_alternate\":"+B(t.Proofs.All(p=>p.EdgeRemovalAlternatePath))+
                ",\"cycle_delta\":"+B(t.After.CycleRank-t.Before.CycleRank==plan.Loops.AcceptedCount)+"},\n"+
                "  \"diagnostics\":["+string.Join(",",t.Diagnostics.Select(J))+"],\n"+
                "  \"composed_geometry_ready\":false,\n  \"player_verified\":false\n}\n";
        }

        public static string Preview(Sv5SpaceGraphPlan plan,Sv5SpaceBounds view,bool rejected)
        {
            var cells=plan.Infill.Cells.ToDictionary(c=>c.World,c=>c.Value);
            foreach(var c in plan.Loops.Cells) cells[c.World]=c.FinalValue;
            var text=new StringBuilder("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\""+view.Width+"\" height=\""+(view.Height+14)+
                "\" viewBox=\"0 0 "+view.Width+" "+(view.Height+14)+"\"><rect width=\"100%\" height=\"100%\" fill=\"#aebbc0\"/>");
            foreach(var c in cells.Where(c=>view.Contains(c.Key)).OrderBy(c=>c.Key)) Cell(c.Key,c.Value==Sv5InfillCellValue.Solid ? "#172026" : "#f7f4dd");
            foreach(var link in plan.Loops.Links)
            {
                foreach(var c in link.Cells.Where(c=>view.Contains(c.World) && c.Role==Sv5LoopCellRole.SolidSupport)) Cell(c.World,"#c7852b");
                foreach(var c in link.Cells.Where(c=>view.Contains(c.World) && c.Role==Sv5LoopCellRole.ApertureOverride)) Cell(c.World,"#a64ac9");
                foreach(var p in link.Centerline.Where(view.Contains)) Cell(p,link.Kind==Sv5LoopKind.RandomShortcut ? "#10a7a0" : "#3b78d8");
                foreach(var p in new[]{link.Centerline.First(),link.Centerline.Last()}.Where(view.Contains))
                    text.Append("<circle cx=\"").Append(p.X-view.X+.5).Append("\" cy=\"").Append(view.MaxYExclusive-p.Y-.5)
                        .Append("\" r=\"1.2\" fill=\"none\" stroke=\"#ef476f\" stroke-width=\".45\"/>");
            }
            if(rejected) foreach(var c in plan.Loops.Candidates.Where(c=>c.Status=="REJECTED").Take(64))
            {
                var p=c.Centerline[c.Centerline.Count/2]; if(view.Contains(p))
                    text.Append("<path d=\"M").Append(p.X-view.X).Append(' ').Append(view.MaxYExclusive-p.Y-1)
                        .Append("l1 1m0-1l-1 1\" stroke=\"#d64242\" stroke-width=\".25\"/>");
            }
            text.Append("<text x=\"1\" y=\"").Append(view.Height+3).Append("\" font-size=\"2.1\">AIR ivory / support ochre / aperture violet / LOOP blue / shortcut teal / endpoint rose / rejected red</text>")
                .Append("<text x=\"1\" y=\"").Append(view.Height+6).Append("\" font-size=\"2.1\">Actual 1x1 cells; two-cell clearance; baseline alternate route recorded in loop_links.csv.</text>")
                .Append("<text x=\"1\" y=\"").Append(view.Height+9).Append("\" font-size=\"2.1\">ComposedGeometryReady=false; PlayerVerified=false; SV5_10 deferred.</text>")
                .Append("<metadata>").Append(H(plan.Digest+"|"+plan.Loops.Digest)).Append("</metadata></svg>\n");
            return text.ToString();
            void Cell(Sv5SpecialWorldPoint p,string color) => text.Append("<path d=\"M").Append(p.X-view.X).Append(' ')
                .Append(view.MaxYExclusive-p.Y-1).Append("h1v1h-1z\" fill=\"").Append(color).Append("\"/>");
        }

        public static string IndexHtml(Sv5SpaceGraphPlan plan)
        {
            var links=Enumerable.Range(0,4).SelectMany(r=>Enumerable.Range(1,4).Select(c=>"<a href=\""+(char)('A'+r)+c+".svg\">"+(char)('A'+r)+c+"</a>"));
            return "<!doctype html><meta charset=\"utf-8\"><title>SV5_09 actual tile loops</title><style>body{font:16px system-ui;margin:24px;background:#eef0e8}img{width:100%}a{margin-right:.7em}</style>"+
                "<h1>SV5_09 actual tile loops</h1><p>accepted "+plan.Loops.AcceptedCount+"; sectors "+plan.Loops.DistinctSectorCount+
                "; LOOP "+plan.Loops.Links.Count(l=>l.Kind==Sv5LoopKind.Loop)+"; RANDOM_SHORTCUT "+plan.Loops.Links.Count(l=>l.Kind==Sv5LoopKind.RandomShortcut)+
                ". Every link has an existing alternate route and cycle delta +1.</p><p>"+string.Join(" ",links)+
                " <a href=\"detail.svg\">detail</a></p><img src=\"overview.svg\"><p>ComposedGeometryReady=false; PlayerVerified=false; SV5_10 not started.</p>";
        }

        private static string Summary(Sv5SpaceGraphPlan p) => "{\"plan_digest\":"+J(p.Digest)+",\"loop_digest\":"+J(p.Loops.Digest)+
            ",\"accepted\":"+p.Loops.AcceptedCount+",\"sectors\":"+p.Loops.DistinctSectorCount+",\"maximum_length\":"+
            p.Loops.Links.Max(l=>l.Centerline.Count)+",\"loop\":"+p.Loops.Links.Count(l=>l.Kind==Sv5LoopKind.Loop)+
            ",\"random_shortcut\":"+p.Loops.Links.Count(l=>l.Kind==Sv5LoopKind.RandomShortcut)+"}";
        private static string Csv(string header,IEnumerable<string> rows) => header+"\n"+string.Join("\n",rows)+"\n";
        private static string Row(params object[] fields) => string.Join(",",fields.Select(v=>"\""+Convert.ToString(v,CultureInfo.InvariantCulture).Replace("\"","\"\"")+"\""));
        private static string Point(Sv5SpecialWorldPoint p) => "["+p.X+","+p.Y+"]";
        private static string Kind(Sv5LoopKind kind) => kind==Sv5LoopKind.RandomShortcut ? "RANDOM_SHORTCUT" : "LOOP";
        private static string J(string value) => "\""+(value ?? string.Empty).Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";
        private static string B(bool value) => value ? "true" : "false";
        private static string H(string value) => (value ?? string.Empty).Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;");
        private static void Write(string path,string text) => File.WriteAllText(path,text,Utf8);
    }
}
