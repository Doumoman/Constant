using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    /// <summary>All views consume the same owned cells; unknown reservations are never painted as finished AIR.</summary>
    public static class Sv5InfillExport
    {
        private static string J(string v) => "\""+(v ?? "").Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";
        private static string B(bool v) => v ? "true" : "false";
        private static string P(RmapSpecialWorldPoint p) => "["+p.X+","+p.Y+"]";
        private static string Points(IEnumerable<RmapSpecialWorldPoint> points) => "["+string.Join(",",points.Select(P))+"]";
        private static string Row(params object[] fields) => string.Join(",",fields.Select(v=>"\""+Convert.ToString(v,CultureInfo.InvariantCulture).Replace("\"","\"\"")+"\""));
        private static string Csv(string header,IEnumerable<string> rows) => header+"\n"+string.Join("\n",rows)+"\n";
        private static void Write(string path,string value) => File.WriteAllText(path,value,new UTF8Encoding(false));
        private static string H(string value) => value.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;").Replace("\"","&quot;");

        public static string ValidationJson(Sv5SpaceGraphPlan plan)
        {
            var infill=plan.Infill ?? throw new ArgumentException("Infill payload required.");
            var reconstructed=Sv5InfillPatterns.Reconstruct(infill.Instances);
            bool patterns=reconstructed.Count==infill.Cells.Count && infill.Cells.All(c=>reconstructed.TryGetValue(c.World,out var v) && v==c.Value);
            var passage=new HashSet<RmapSpecialWorldPoint>(plan.Connections.SelectMany(c=>c.Centerline.Concat(c.ApertureCells)));
            bool air=infill.Cells.Where(c=>c.Value==Sv5InfillCellValue.Air).All(c=>passage.Contains(c.World));
            bool solid=infill.Cells.Where(c=>c.Value==Sv5InfillCellValue.Solid).All(c=>!passage.Contains(c.World));
            var owned=infill.Cells.ToDictionary(c=>c.World,c=>c.Value);
            bool local=infill.Rooms.Count>0 && infill.Rooms.Count==infill.Links.Count;
            foreach(var link in infill.Links)
            {
                var room=infill.Rooms.Single(r=>r.Id==link.Room);
                var parent=infill.Rooms.SingleOrDefault(r=>r.Id==room.Parent);
                local &= link.Path.Count>0 && Sv5InfillPatterns.Screen(owned,
                    parent==null ? link.Path[0] : parent.Entry,room.Deep).Success;
            }
            var known=Sv5SpaceInfill.KnownBase(plan).ToDictionary(c=>c.Key,c=>c.Value);
            foreach(var cell in infill.Cells) known[cell.World]=cell.Value;
            var failures=Sv5InfillPatterns.SolidWindows(known);
            int fullyKnown=FullyKnownWindows(known);
            return "{\"schema\":\"SV5_INFILL_VALIDATION_V1\",\"plan_digest\":"+J(plan.Digest)+",\"infill_digest\":"+J(infill.Digest)+
                ",\"baseline_digest\":"+J(infill.BaselineDigest)+",\"CELLS\":"+B(plan.Success && air && solid && failures.Count==0)+
                ",\"PATTERNS\":"+B(patterns)+",\"STATIC_SCREEN\":"+B(local)+
                ",\"CONTACT\":"+B(Sv5SpaceGraphValidator.FindGateErrors(plan.ContactDecisions,plan.Gates).Count==0 && plan.ContactDecisions.All(c=>c.CoverageChecked && c.LogicalStateTransitionChecked))+
                ",\"PHYSICAL_PRODUCT\":"+B(plan.PhysicalProduct.Success)+",\"COMPOSED\":false,\"PLAYER\":false,"+
                "\"all_new_air_in_coordinate_graph\":"+B(air)+",\"solid_disjoint_from_passage\":"+B(solid)+
                ",\"six_by_six_total_windows\":254409,\"fully_known_windows\":"+fullyKnown+
                ",\"mixed_pending_windows\":"+(254409-fullyKnown)+",\"solid_window_failures\":"+Points(failures)+
                ",\"errors\":["+string.Join(",",infill.Diagnostics.Concat(plan.Diagnostics).Distinct().Select(J))+"],"+
                "\"qualification\":\"Known-solid union and local supported movement only; not composed-world or Player verification\"}";
        }

        private static int FullyKnownWindows(IReadOnlyDictionary<RmapSpecialWorldPoint,Sv5InfillCellValue> known)
        {
            var sums=new int[625,417]; int count=0;
            for(int y=0;y<416;y++) for(int x=0;x<624;x++)
                sums[x+1,y+1]=sums[x,y+1]+sums[x+1,y]-sums[x,y]+(known.TryGetValue(new RmapSpecialWorldPoint(x,y),out var value) && value!=Sv5InfillCellValue.Unknown ? 1 : 0);
            for(int y=0;y<=410;y++) for(int x=0;x<=618;x++)
                if(sums[x+6,y+6]-sums[x,y+6]-sums[x+6,y]+sums[x,y]==36) count++;
            return count;
        }

        public static string InfillJson(Sv5SpaceGraphPlan plan)
        {
            var p=plan.Infill ?? throw new ArgumentException("Infill payload required.");
            return "{\"schema\":\"SV5_INFILL_V1\",\"plan_digest\":"+J(plan.Digest)+",\"baseline_digest\":"+J(p.BaselineDigest)+
                ",\"infill_digest\":"+J(p.Digest)+",\"profile_digest\":"+J(p.Profile.Digest)+",\"target\":"+p.Profile.Target+
                ",\"minimum\":"+p.Profile.Minimum+",\"maximum\":"+p.Profile.Maximum+",\"new_rooms\":"+p.NewRoomCount+
                ",\"new_owned_tiles\":"+p.NewOwnedTiles+",\"sector_counts\":["+string.Join(",",p.SectorCounts)+"],\"termination\":"+J(p.Termination)+
                ",\"target_shortfall\":"+Math.Max(0,p.Profile.Target-p.NewRoomCount)+",\"rejections\":{"+
                string.Join(",",p.Rejections.Select(k=>J(k.Key)+":"+k.Value))+"},\"rooms\":["+
                string.Join(",",p.Rooms.Select(r=>"{\"id\":"+J(r.Id)+",\"recipe\":"+J(r.Recipe)+",\"bounds\":["+r.Bounds+"],\"parent\":"+
                    J(r.Parent)+",\"host\":"+J(r.Host)+",\"mirror\":"+B(r.Mirror)+",\"depth\":"+r.Depth+",\"legacy\":"+B(r.Legacy)+
                    ",\"entry\":"+P(r.Entry)+",\"deep\":"+P(r.Deep)+"}"))+"],\"cells\":["+
                string.Join(",",p.Cells.Select(c=>"{\"world\":"+P(c.World)+",\"base\":"+J(c.Value.ToString().ToUpperInvariant())+
                    ",\"owner\":"+J(c.Owner)+",\"recipe\":"+J(c.Recipe)+",\"parent\":"+J(c.Parent)+",\"host\":"+J(c.Host)+",\"shared\":"+B(c.Shared)+"}"))+
                "],\"validation\":"+ValidationJson(plan)+",\"composed_geometry_ready\":false,\"player_verified\":false}";
        }

        public static void WriteCase(Sv5SpaceGraphPlan plan,string directory)
        {
            var p=plan.Infill ?? throw new ArgumentException("Infill payload required."); Directory.CreateDirectory(directory);
            Write(Path.Combine(directory,"infill.json"),InfillJson(plan));
            Write(Path.Combine(directory,"infill_validation.json"),ValidationJson(plan));
            Write(Path.Combine(directory,"infill_cells.csv"),Csv("world_x,world_y,base,owner,recipe,parent,host,shared,pattern_x,pattern_y,pattern_index,plan_digest",
                p.Cells.Select(c=>Row(c.World.X,c.World.Y,c.Value,c.Owner,c.Recipe,c.Parent,c.Host,c.Shared,c.World.X/4*4,c.World.Y/4*4,
                    c.World.Y%4*4+c.World.X%4,plan.Digest))));
            Write(Path.Combine(directory,"infill_patterns.csv"),Csv("pattern_id,write_mask,cells_row_major_16,semantic_hash,plan_digest",
                p.Instances.Select(i=>i.Pattern).GroupBy(i=>i.Id).OrderBy(g=>g.Key,StringComparer.Ordinal).Select(g=>g.First())
                    .Select(i=>Row(i.Id,i.Mask,string.Join("|",i.Values),i.Digest,plan.Digest))));
            Write(Path.Combine(directory,"infill_instances.csv"),Csv("pattern_id,origin_x,origin_y,room_id,formation_id,recipe,parent,host,mirror,plan_digest",
                p.Instances.Select(i=>Row(i.Pattern.Id,i.Origin.X,i.Origin.Y,i.Owner,i.Owner,i.Recipe,i.Parent,i.Host,i.Mirror,plan.Digest))));
            Write(Path.Combine(directory,"infill_rooms.csv"),Csv("room_id,recipe,bounds,parent,host,depth,entry,deep,air,solid,legacy,reward,static_success,plan_digest",
                p.Rooms.Select(r=>Row(r.Id,r.Recipe,r.Bounds,r.Parent,r.Host,r.Depth,r.Entry,r.Deep,r.Cells.Count(c=>c.Value==Sv5InfillCellValue.Air),
                    r.Cells.Count(c=>c.Value==Sv5InfillCellValue.Solid),r.Legacy,"NONE",p.Links.Single(l=>l.Room==r.Id).Proof.Success,plan.Digest))));
            Write(Path.Combine(directory,"infill_links.csv"),Csv("room_id,parent,host,ordered_cardinal_air_path,host_access,external_cardinal_air,external_cell_count,opening_faces,support_cells,approach,return,success,qualification,plan_digest",
                p.Links.Select(l=>Row(l.Room,l.Parent,l.Host,Points(l.Path),Points(l.HostAccess),Points(l.ExternalCenterline),l.ExternalCenterline.Count,string.Join("|",l.Faces.Select(f=>f.StableToken)),
                    Points(l.Proof.Approach.Select(q=>new RmapSpecialWorldPoint(q.X,q.Y-1))),Points(l.Proof.Approach),Points(l.Proof.Return),l.Proof.Success,l.Proof.Reason,plan.Digest))));
            var known=Sv5SpaceInfill.KnownBase(plan); var added=p.Cells.Select(c=>c.World).ToHashSet();
            var reserved=p.PreviouslyReservedCells.ToHashSet();
            var legacy=p.Rooms.Where(r=>r.Legacy).Select(r=>r.Id).ToHashSet();
            var newOwned=p.Cells.Where(c=>!c.Shared && !legacy.Contains(c.Owner) && !reserved.Contains(c.World)).Select(c=>c.World).ToHashSet();
            var windows=new List<string>();
            for(int row=0;row<13;row++) for(int col=0;col<13;col++)
            {
                var bounds=new Sv5SpaceBounds(col*48,row*32,48,32);
                int before=known.Keys.Count(bounds.Contains), after=known.Keys.Concat(added).Distinct().Count(bounds.Contains);
                int reservations=reserved.Count(bounds.Contains), owned=newOwned.Count(bounds.Contains);
                var rooms=p.Rooms.Where(r=>!r.Legacy && bounds.Contains(new RmapSpecialWorldPoint(r.Bounds.X+r.Bounds.Width/2,r.Bounds.Y+r.Bounds.Height/2))).ToArray();
                windows.Add(Row(col,row,before,after,reservations,owned,1536-after,rooms.Length,
                    rooms.Count(r=>!p.Rooms.Any(child=>child.Parent==r.Id)),rooms.Count(r=>!p.Links.Single(l=>l.Room==r.Id).Proof.Success),plan.Digest));
            }
            Write(Path.Combine(directory,"infill_windows.csv"),Csv("window_x,window_y,before_known,after_known,reserved,new_owned,pending_cells,new_rooms,leaf_rooms,static_failures,plan_digest",windows));
        }

        public static string Preview(Sv5SpaceGraphPlan plan,Sv5SpaceBounds view,bool showInfill=true)
        {
            var cells=Sv5SpaceInfill.KnownBase(plan).ToDictionary(c=>c.Key,c=>c.Value);
            if(showInfill && plan.Infill!=null) foreach(var c in plan.Infill.Cells) cells[c.World]=c.Value;
            var text=new StringBuilder("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\""+view.Width+"\" height=\""+(view.Height+12)+"\" viewBox=\"0 0 "+view.Width+" "+(view.Height+12)+"\">");
            text.Append("<rect width=\"100%\" height=\"100%\" fill=\"#b8c3c6\"/><defs><pattern id=\"grid\" width=\"4\" height=\"4\" patternUnits=\"userSpaceOnUse\"><path d=\"M0 0H4V4 M1 0V4 M2 0V4 M3 0V4 M0 1H4 M0 2H4 M0 3H4\" fill=\"none\" stroke=\"#60727b\" stroke-width=\".03\"/><path d=\"M0 4V0H4\" fill=\"none\" stroke=\"#174b65\" stroke-width=\".09\"/></pattern></defs>");
            foreach(var place in plan.Places.Where(p=>p.Kind!=Sv5SpacePlaceKind.Core))
            {
                int x=Math.Max(view.X,place.Bounds.X),y=Math.Max(view.Y,place.Bounds.Y),right=Math.Min(view.MaxXExclusive,place.Bounds.MaxXExclusive),top=Math.Min(view.MaxYExclusive,place.Bounds.MaxYExclusive);
                if(x>=right || y>=top) continue;
                text.Append("<rect x=\"").Append(x-view.X).Append("\" y=\"").Append(view.MaxYExclusive-top).Append("\" width=\"").Append(right-x)
                    .Append("\" height=\"").Append(top-y).Append("\" fill=\"#d7c9aa\" stroke=\"#97845f\" stroke-width=\".2\"/>");
            }
            // Baseline passage remains a plan reservation, distinctly blue, not completed terrain.
            foreach(var point in plan.Connections.SelectMany(c=>c.Centerline).Distinct().Where(view.Contains))
                if(!cells.ContainsKey(point)) Cell(point,"#789eaf");
            foreach(var cell in cells.Where(c=>view.Contains(c.Key)).OrderBy(c=>c.Key)) Cell(cell.Key,cell.Value==Sv5InfillCellValue.Solid ? "#111a20" : "#f5f5e9");
            text.Append("<rect width=\"").Append(view.Width).Append("\" height=\"").Append(view.Height).Append("\" fill=\"url(#grid)\"/>");
            for(int x=(view.X/12+1)*12;x<view.MaxXExclusive;x+=12) text.Append("<path d=\"M").Append(x-view.X).Append(" 0V").Append(view.Height).Append("\" stroke=\"#385e6a\" stroke-width=\".12\" opacity=\".5\"/>");
            for(int y=(view.Y/8+1)*8;y<view.MaxYExclusive;y+=8) text.Append("<path d=\"M0 ").Append(view.MaxYExclusive-y).Append("H").Append(view.Width).Append("\" stroke=\"#385e6a\" stroke-width=\".12\" opacity=\".5\"/>");
            if(showInfill && plan.Infill!=null) foreach(var room in plan.Infill.Rooms.Where(r=>view.Contains(r.Entry)))
                text.Append("<circle cx=\"").Append((room.Entry.X-view.X+0.5).ToString(CultureInfo.InvariantCulture)).Append("\" cy=\"").Append((view.MaxYExclusive-room.Entry.Y-0.5).ToString(CultureInfo.InvariantCulture))
                    .Append("\" r=\".45\" fill=\"#d36620\"><title>").Append(H(room.Id+" "+room.Recipe)).Append("</title></circle>");
            text.Append("<text x=\"1\" y=\"").Append(view.Height+3).Append("\" font-size=\"2.2\">SOLID black / AIR ivory / planned passage blue / unassembled tan / UNKNOWN gray</text>")
                .Append("<text x=\"1\" y=\"").Append(view.Height+6).Append("\" font-size=\"2.2\">1-cell grid; 4x4 pattern; 12x8 chunk. Composed=false; Player=false.</text>")
                .Append("<metadata>").Append(H(!showInfill && plan.Infill!=null ? plan.Infill.BaselineDigest : plan.Digest)).Append("</metadata></svg>\n");
            return text.ToString();
            void Cell(RmapSpecialWorldPoint p,string color) => text.Append("<path d=\"M").Append(p.X-view.X).Append(" ").Append(view.MaxYExclusive-p.Y-1)
                .Append("h1v1h-1z\" fill=\"").Append(color).Append("\"/>");
        }

        public static void WriteComparison(string directory,Sv5SpaceGraphPlan standard,Sv5SpaceGraphPlan repeat)
        {
            if(standard.Infill==null || repeat.Infill==null || !standard.Success || !repeat.Success) throw new ArgumentException("Two passing infill plans required.");
            Directory.CreateDirectory(Path.Combine(directory,"preview"));
            Sv5SpaceGraphExport.WriteAll(standard,Path.Combine(directory,"default"));
            Sv5SpaceGraphExport.WriteAll(repeat,Path.Combine(directory,"repeat"));
            Write(Path.Combine(directory,"comparison.json"),"{\"default\":"+Summary(standard)+",\"repeat\":"+Summary(repeat)+"}");
            Write(Path.Combine(directory,"preview/before_after.svg"),Comparison(standard));
            Write(Path.Combine(directory,"preview/repeat_before_after.svg"),Comparison(repeat));
            var room=standard.Infill.Rooms.First(r=>!r.Legacy && r.Recipe=="LANDING");
            int vx=Math.Min(576,Math.Max(0,room.Bounds.X-16)),vy=Math.Min(384,Math.Max(0,room.Bounds.Y-8));
            Write(Path.Combine(directory,"preview/detail.svg"),Preview(standard,new Sv5SpaceBounds(vx,vy,48,32)));
            var links=new List<string>();
            for(int row=0;row<4;row++) for(int col=0;col<4;col++)
            {
                string name=((char)('A'+row)).ToString()+(col+1);
                Write(Path.Combine(directory,"preview/"+name+".svg"),Preview(standard,new Sv5SpaceBounds(col*156,(3-row)*104,156,104)));
                links.Add("<a href=\""+name+".svg\">"+name+"</a>");
            }
            Write(Path.Combine(directory,"preview/index.html"),"<!doctype html><meta charset=\"utf-8\"><title>SV5_08 actual local cells</title>"+
                "<style>body{font:16px system-ui;margin:24px;background:#eef0e8}img{width:100%}a{margin-right:1em}</style>"+
                "<h1>SV5_08 · actual local cells, not composed-world or Player evidence</h1><p>Default new rooms "+standard.Infill.NewRoomCount+
                "; repeat "+repeat.Infill.NewRoomCount+". Black SOLID, ivory AIR, tan unassembled large places, gray unknown.</p>"+
                "<p>"+string.Join(" ",links)+"</p><img src=\"before_after.svg\"><img src=\"repeat_before_after.svg\"><img src=\"detail.svg\">"+
                "<p>ComposedGeometryReady=false; PlayerVerified=false. SV5_09 remains LOCKED.</p>");
        }
        private static string Summary(Sv5SpaceGraphPlan p) => "{\"plan_digest\":"+J(p.Digest)+",\"baseline_digest\":"+J(p.Infill.BaselineDigest)+
            ",\"new_rooms\":"+p.Infill.NewRoomCount+",\"new_owned_tiles\":"+p.Infill.NewOwnedTiles+",\"sector_counts\":["+string.Join(",",p.Infill.SectorCounts)+
            "],\"termination\":"+J(p.Infill.Termination)+",\"target_shortfall\":"+Math.Max(0,p.Infill.Profile.Target-p.Infill.NewRoomCount)+",\"validation\":"+ValidationJson(p)+"}";
        private static string Comparison(Sv5SpaceGraphPlan p) => "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1260 444\">"+
            "<text x=\"0\" y=\"10\" font-size=\"8\">BEFORE · SV5_07 diversity ON / INFILL NONE</text><text x=\"636\" y=\"10\" font-size=\"8\">AFTER · actual SV5_08 cells</text>"+
            "<g transform=\"translate(0 16)\">"+Preview(p,new Sv5SpaceBounds(0,0,624,416),false)+"</g><g transform=\"translate(636 16)\">"+
            Preview(p,new Sv5SpaceBounds(0,0,624,416))+"</g></svg>\n";
    }
}
