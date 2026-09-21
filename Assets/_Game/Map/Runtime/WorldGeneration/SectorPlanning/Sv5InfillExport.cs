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
        private static string P(Sv5SpecialWorldPoint p) => "["+p.X+","+p.Y+"]";
        private static string Points(IEnumerable<Sv5SpecialWorldPoint> points) => "["+string.Join(",",points.Select(P))+"]";
        private static string Row(params object[] fields) => string.Join(",",fields.Select(v=>"\""+Convert.ToString(v,CultureInfo.InvariantCulture).Replace("\"","\"\"")+"\""));
        private static string Csv(string header,IEnumerable<string> rows) => header+"\n"+string.Join("\n",rows)+"\n";
        private static void Write(string path,string value) => File.WriteAllText(path,value,new UTF8Encoding(false));
        private static string H(string value) => value.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;").Replace("\"","&quot;");

        public static string ValidationJson(Sv5SpaceGraphPlan plan)
        {
            var infill=plan.Infill ?? throw new ArgumentException("Infill payload required.");
            var reconstructed=Sv5InfillPatterns.Reconstruct(infill.Instances);
            bool patterns=reconstructed.Count==infill.Cells.Count && infill.Cells.All(c=>reconstructed.TryGetValue(c.World,out var v) && v==c.Value);
            var passage=new HashSet<Sv5SpecialWorldPoint>(plan.Connections.SelectMany(c=>c.Centerline.Concat(c.ApertureCells)));
            bool air=infill.Cells.Where(c=>c.Value==Sv5InfillCellValue.Air).All(c=>passage.Contains(c.World));
            bool solid=infill.Cells.Where(c=>c.Value==Sv5InfillCellValue.Solid).All(c=>!passage.Contains(c.World));
            var owned=infill.Cells.ToDictionary(c=>c.World,c=>c.Value);
            var measuredLinks=infill.Links.ToArray();
            int overLength=measuredLinks.Count(l=>l.ConnectionCellCount>infill.Profile.MaximumLink);
            int endpointErrors=measuredLinks.Count(l=>
            {
                var room=infill.Rooms.Single(r=>r.Id==l.Room);
                var parent=infill.Rooms.SingleOrDefault(r=>r.Id==room.Parent);
                if(l.Path.Count==0 || !l.Path.Last().Equals(room.Entry)) return true;
                if(parent==null) return l.HostAccess.Count<2 || !l.HostAccess.Last().Equals(l.Path[0]);
                var first=l.Path[0]; var bounds=parent.Bounds;
                return l.HostAccess.Count!=0 || !bounds.Contains(first) ||
                    (first.X!=bounds.X && first.X!=bounds.MaxXExclusive-1 && first.Y!=bounds.Y && first.Y!=bounds.MaxYExclusive-1);
            });
            int nonCardinal=measuredLinks.Count(l=>
            {
                var room=infill.Rooms.Single(r=>r.Id==l.Room);
                bool root=!infill.Rooms.Any(r=>r.Id==room.Parent);
                return Sv5SpaceInfill.FindConnectionLengthErrors(l.Path,l.HostAccess,infill.Profile.MaximumLink,root)
                    .Any(error=>error=="NON_CARDINAL_STEP");
            });
            int maximumLength=measuredLinks.Select(l=>l.ConnectionCellCount).DefaultIfEmpty(0).Max();
            bool length=measuredLinks.Length>0 && overLength==0 && endpointErrors==0 && nonCardinal==0;
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
            return "{\"schema\":\"SV5_INFILL_VALIDATION_V2\",\"plan_digest\":"+J(plan.Digest)+",\"infill_digest\":"+J(infill.Digest)+
                ",\"baseline_digest\":"+J(infill.BaselineDigest)+",\"CELLS\":"+B(plan.Success && air && solid && failures.Count==0)+
                ",\"PATTERNS\":"+B(patterns)+",\"STATIC_SCREEN\":"+B(local)+",\"CONNECTION_LENGTH\":"+B(length)+
                ",\"CONTACT\":"+B(Sv5SpaceGraphValidator.FindGateErrors(plan.ContactDecisions,plan.Gates).Count==0 && plan.ContactDecisions.All(c=>c.CoverageChecked && c.LogicalStateTransitionChecked))+
                ",\"PHYSICAL_PRODUCT\":"+B(plan.PhysicalProduct.Success)+",\"COMPOSED\":false,\"PLAYER\":false,"+
                "\"length_policy\":"+J(Sv5InfillProfile.ConnectionLengthPolicy)+",\"length_target_count\":"+measuredLinks.Length+
                ",\"maximum_connection_cell_count\":"+maximumLength+",\"over_length_count\":"+overLength+
                ",\"endpoint_error_count\":"+endpointErrors+",\"non_cardinal_count\":"+nonCardinal+","+
                "\"all_new_air_in_coordinate_graph\":"+B(air)+",\"solid_disjoint_from_passage\":"+B(solid)+
                ",\"six_by_six_total_windows\":254409,\"fully_known_windows\":"+fullyKnown+
                ",\"mixed_pending_windows\":"+(254409-fullyKnown)+",\"solid_window_failures\":"+Points(failures)+
                ",\"errors\":["+string.Join(",",infill.Diagnostics.Concat(plan.Diagnostics).Distinct().Select(J))+"],"+
                "\"qualification\":\"Known-solid union and local supported movement only; not composed-world or Player verification\"}";
        }

        private static int FullyKnownWindows(IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5InfillCellValue> known)
        {
            var sums=new int[625,417]; int count=0;
            for(int y=0;y<416;y++) for(int x=0;x<624;x++)
                sums[x+1,y+1]=sums[x,y+1]+sums[x+1,y]-sums[x,y]+(known.TryGetValue(new Sv5SpecialWorldPoint(x,y),out var value) && value!=Sv5InfillCellValue.Unknown ? 1 : 0);
            for(int y=0;y<=410;y++) for(int x=0;x<=618;x++)
                if(sums[x+6,y+6]-sums[x,y+6]-sums[x+6,y]+sums[x,y]==36) count++;
            return count;
        }

        public static string InfillJson(Sv5SpaceGraphPlan plan)
        {
            var p=plan.Infill ?? throw new ArgumentException("Infill payload required.");
            var measured=p.Links.ToArray();
            return "{\"schema\":\"SV5_INFILL_V2\",\"plan_digest\":"+J(plan.Digest)+",\"baseline_digest\":"+J(p.BaselineDigest)+
                ",\"infill_digest\":"+J(p.Digest)+",\"profile_digest\":"+J(p.Profile.Digest)+",\"length_policy\":"+
                J(Sv5InfillProfile.ConnectionLengthPolicy)+",\"length_target_count\":"+measured.Length+
                ",\"maximum_connection_cell_count\":"+measured.Select(l=>l.ConnectionCellCount).DefaultIfEmpty(0).Max()+
                ",\"over_length_count\":"+measured.Count(l=>l.ConnectionCellCount>p.Profile.MaximumLink)+",\"target\":"+p.Profile.Target+
                ",\"minimum\":"+p.Profile.Minimum+",\"maximum\":"+p.Profile.Maximum+",\"new_rooms\":"+p.NewRoomCount+
                ",\"new_owned_tiles\":"+p.NewOwnedTiles+",\"sector_counts\":["+string.Join(",",p.SectorCounts)+"],\"termination\":"+J(p.Termination)+
                ",\"target_shortfall\":"+Math.Max(0,p.Profile.Target-p.NewRoomCount)+",\"rejections\":{"+
                string.Join(",",p.Rejections.Select(k=>J(k.Key)+":"+k.Value))+"},\"rooms\":["+
                string.Join(",",p.Rooms.Select(r=>"{\"id\":"+J(r.Id)+",\"recipe\":"+J(r.Recipe)+",\"bounds\":["+r.Bounds+"],\"parent\":"+
                    J(r.Parent)+",\"host\":"+J(r.Host)+",\"mirror\":"+B(r.Mirror)+",\"depth\":"+r.Depth+
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
            Write(Path.Combine(directory,"infill_rooms.csv"),Csv("room_id,recipe,bounds,parent,host,depth,entry,deep,air,solid,reward,static_success,plan_digest",
                p.Rooms.Select(r=>Row(r.Id,r.Recipe,r.Bounds,r.Parent,r.Host,r.Depth,r.Entry,r.Deep,r.Cells.Count(c=>c.Value==Sv5InfillCellValue.Air),
                    r.Cells.Count(c=>c.Value==Sv5InfillCellValue.Solid),"NONE",p.Links.Single(l=>l.Room==r.Id).Proof.Success,plan.Digest))));
            Write(Path.Combine(directory,"infill_links.csv"),Csv("room_id,parent,host,ordered_cardinal_air_path,host_access,external_cardinal_air,external_cell_count,connection_centerline,connection_cell_count,length_policy,length_status,opening_faces,support_cells,approach,return,success,qualification,plan_digest",
                p.Links.Select(l=>Row(l.Room,l.Parent,l.Host,Points(l.Path),Points(l.HostAccess),Points(l.ExternalCenterline),l.ExternalCenterline.Count,
                    Points(l.ConnectionCenterline),l.ConnectionCellCount,Sv5InfillProfile.ConnectionLengthPolicy,
                    l.ConnectionCellCount<=p.Profile.MaximumLink ? "PASS" : "FAIL_OVER_LIMIT",
                    string.Join("|",l.Faces.Select(f=>f.StableToken)),Points(l.Proof.Approach.Select(q=>new Sv5SpecialWorldPoint(q.X,q.Y-1))),
                    Points(l.Proof.Approach),Points(l.Proof.Return),l.Proof.Success,l.Proof.Reason,plan.Digest))));
            var known=Sv5SpaceInfill.KnownBase(plan); var added=p.Cells.Select(c=>c.World).ToHashSet();
            var reserved=p.PreviouslyReservedCells.ToHashSet();
            var newOwned=p.Cells.Where(c=>!c.Shared && !reserved.Contains(c.World)).Select(c=>c.World).ToHashSet();
            var windows=new List<string>();
            for(int row=0;row<13;row++) for(int col=0;col<13;col++)
            {
                var bounds=new Sv5SpaceBounds(col*48,row*32,48,32);
                int before=known.Keys.Count(bounds.Contains), after=known.Keys.Concat(added).Distinct().Count(bounds.Contains);
                int reservations=reserved.Count(bounds.Contains), owned=newOwned.Count(bounds.Contains);
                var rooms=p.Rooms.Where(r=>bounds.Contains(new Sv5SpecialWorldPoint(r.Bounds.X+r.Bounds.Width/2,r.Bounds.Y+r.Bounds.Height/2))).ToArray();
                windows.Add(Row(col,row,before,after,reservations,owned,1536-after,rooms.Length,
                    rooms.Count(r=>!p.Rooms.Any(child=>child.Parent==r.Id)),rooms.Count(r=>!p.Links.Single(l=>l.Room==r.Id).Proof.Success),plan.Digest));
            }
            Write(Path.Combine(directory,"infill_windows.csv"),Csv("window_x,window_y,before_known,after_known,reserved,new_owned,pending_cells,new_rooms,leaf_rooms,static_failures,plan_digest",windows));
        }

        public static string Preview(Sv5SpaceGraphPlan plan,Sv5SpaceBounds view,bool showInfill=true)
        {
            var cells=Sv5SpaceInfill.KnownBase(plan).ToDictionary(c=>c.Key,c=>c.Value);
            if(showInfill && plan.Infill!=null) foreach(var c in plan.Infill.Cells) cells[c.World]=c.Value;
            return PreviewCells(plan,view,cells,showInfill,showInfill && plan.Infill!=null ? plan.Digest : plan.Infill?.BaselineDigest ?? plan.Digest);
        }

        private static string PreviewCells(Sv5SpaceGraphPlan plan,Sv5SpaceBounds view,
            IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5InfillCellValue> cells,bool showRoomMarkers,string metadata)
        {
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
            if(showRoomMarkers && plan.Infill!=null) foreach(var room in plan.Infill.Rooms.Where(r=>view.Contains(r.Entry)))
                text.Append("<circle cx=\"").Append((room.Entry.X-view.X+0.5).ToString(CultureInfo.InvariantCulture)).Append("\" cy=\"").Append((view.MaxYExclusive-room.Entry.Y-0.5).ToString(CultureInfo.InvariantCulture))
                    .Append("\" r=\".45\" fill=\"#d36620\"><title>").Append(H(room.Id+" "+room.Recipe)).Append("</title></circle>");
            text.Append("<text x=\"1\" y=\"").Append(view.Height+3).Append("\" font-size=\"2.2\">SOLID black / AIR ivory / planned passage blue / unassembled tan / UNKNOWN gray</text>")
                .Append("<text x=\"1\" y=\"").Append(view.Height+6).Append("\" font-size=\"2.2\">1-cell grid; 4x4 pattern; 12x8 chunk. Composed=false; Player=false.</text>")
                .Append("<metadata>").Append(H(metadata)).Append("</metadata></svg>\n");
            return text.ToString();
            void Cell(Sv5SpecialWorldPoint p,string color) => text.Append("<path d=\"M").Append(p.X-view.X).Append(" ").Append(view.MaxYExclusive-p.Y-1)
                .Append("h1v1h-1z\" fill=\"").Append(color).Append("\"/>");
        }

        public static void WriteComparison(string directory,Sv5SpaceGraphPlan standard,Sv5SpaceGraphPlan repeat)
        {
            if(standard.Infill==null || repeat.Infill==null || !standard.Success || !repeat.Success) throw new ArgumentException("Two passing infill plans required.");
            Directory.CreateDirectory(Path.Combine(directory,"preview"));
            Sv5SpaceGraphExport.WriteAll(standard,Path.Combine(directory,"default"));
            Sv5SpaceGraphExport.WriteAll(repeat,Path.Combine(directory,"repeat"));
            string historicalRoot=Path.GetFullPath(Path.Combine(directory,"..","SV5_08"));
            string historicalComparisonPath=Path.Combine(historicalRoot,"comparison.json");
            if(!File.Exists(historicalComparisonPath))
            {
                var ancestor=new DirectoryInfo(Path.GetFullPath(directory));
                while(ancestor!=null && !string.Equals(ancestor.Name,"GENERATED",StringComparison.OrdinalIgnoreCase)) ancestor=ancestor.Parent;
                if(ancestor!=null)
                {
                    string canonical=Path.Combine(ancestor.FullName,"SV5_08");
                    if(File.Exists(Path.Combine(canonical,"comparison.json")))
                    { historicalRoot=canonical; historicalComparisonPath=Path.Combine(historicalRoot,"comparison.json"); }
                }
            }
            if(!File.Exists(historicalComparisonPath)) throw new FileNotFoundException("Historical SV5_08 comparison is required read-only evidence.",historicalComparisonPath);
            string historicalComparison=File.ReadAllText(historicalComparisonPath,Encoding.UTF8).Trim();
            var historicalDefault=ReadCells(Path.Combine(historicalRoot,"default","infill_cells.csv"));
            var historicalRepeat=ReadCells(Path.Combine(historicalRoot,"repeat","infill_cells.csv"));
            Write(Path.Combine(directory,"comparison.json"),"{\"schema\":\"SV5_INFILL_FIX01_COMPARISON_V1\",\"historical_sv5_08\":"+
                historicalComparison+",\"fix01\":{\"default\":"+Summary(standard)+",\"repeat\":"+Summary(repeat)+"}}");
            var world=new Sv5SpaceBounds(0,0,624,416);
            Write(Path.Combine(directory,"preview/before_after.svg"),HistoricalComparison(standard,historicalDefault,world,"DEFAULT",false));
            Write(Path.Combine(directory,"preview/repeat_before_after.svg"),HistoricalComparison(repeat,historicalRepeat,world,"REPEAT",false));
            Write(Path.Combine(directory,"preview/detail.svg"),HistoricalComparison(standard,historicalDefault,
                new Sv5SpaceBounds(552,380,48,36),"RECORDED 26-CELL WINDOW",true));
            var links=new List<string>();
            for(int row=0;row<4;row++) for(int col=0;col<4;col++)
            {
                string name=((char)('A'+row)).ToString()+(col+1);
                Write(Path.Combine(directory,"preview/"+name+".svg"),Preview(standard,new Sv5SpaceBounds(col*156,(3-row)*104,156,104)));
                links.Add("<a href=\""+name+".svg\">"+name+"</a>");
            }
            Write(Path.Combine(directory,"preview/index.html"),"<!doctype html><meta charset=\"utf-8\"><title>SV5_08 actual local cells</title>"+
                "<style>body{font:16px system-ui;margin:24px;background:#eef0e8}img{width:100%}a{margin-right:1em}</style>"+
                "<h1>SV5_08 → SV5_08_FIX01 · actual local-cell comparison</h1><p>Default FIX01 new rooms "+standard.Infill.NewRoomCount+
                "; repeat "+repeat.Infill.NewRoomCount+". Left panels are immutable SV5_08 CSV cells; right panels are FIX01 cells. Black SOLID, ivory AIR, tan unassembled large places, gray unknown.</p>"+
                "<p>"+string.Join(" ",links)+"</p><img src=\"before_after.svg\"><img src=\"repeat_before_after.svg\"><img src=\"detail.svg\">"+
                "<p>ComposedGeometryReady=false; PlayerVerified=false. SV5_09 remains LOCKED.</p>");
        }
        private static string Summary(Sv5SpaceGraphPlan p) => "{\"plan_digest\":"+J(p.Digest)+",\"baseline_digest\":"+J(p.Infill.BaselineDigest)+
            ",\"new_rooms\":"+p.Infill.NewRoomCount+",\"new_owned_tiles\":"+p.Infill.NewOwnedTiles+",\"sector_counts\":["+string.Join(",",p.Infill.SectorCounts)+
            "],\"termination\":"+J(p.Infill.Termination)+",\"target_shortfall\":"+Math.Max(0,p.Infill.Profile.Target-p.Infill.NewRoomCount)+",\"validation\":"+ValidationJson(p)+"}";
        private static IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5InfillCellValue> ReadCells(string path)
        {
            if(!File.Exists(path)) throw new FileNotFoundException("Historical SV5_08 cell CSV is required read-only evidence.",path);
            var cells=new Dictionary<Sv5SpecialWorldPoint,Sv5InfillCellValue>();
            foreach(string line in File.ReadLines(path).Skip(1).Where(value=>value.Length>0))
            {
                string[] fields=line.Trim('"').Split(new[]{"\",\""},StringSplitOptions.None);
                if(fields.Length<3) throw new InvalidDataException("Malformed historical infill cell row.");
                cells[new Sv5SpecialWorldPoint(int.Parse(fields[0],CultureInfo.InvariantCulture),int.Parse(fields[1],CultureInfo.InvariantCulture))]=
                    (Sv5InfillCellValue)Enum.Parse(typeof(Sv5InfillCellValue),fields[2],true);
            }
            return new System.Collections.ObjectModel.ReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5InfillCellValue>(cells);
        }

        private static string HistoricalComparison(Sv5SpaceGraphPlan plan,
            IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5InfillCellValue> historical,Sv5SpaceBounds view,string label,bool showLengthWitness)
        {
            var before=Sv5SpaceInfill.KnownBase(plan).ToDictionary(c=>c.Key,c=>c.Value);
            foreach(var cell in historical) before[cell.Key]=cell.Value;
            var after=Sv5SpaceInfill.KnownBase(plan).ToDictionary(c=>c.Key,c=>c.Value);
            foreach(var cell in plan.Infill.Cells) after[cell.World]=cell.Value;
            int gap=12,width=view.Width*2+gap,height=view.Height+30;
            var text=new StringBuilder("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 "+width+" "+height+"\">")
                .Append("<text x=\"0\" y=\"10\" font-size=\"6\">").Append(H(label+" BEFORE · immutable SV5_08 actual cells")).Append("</text>")
                .Append("<text x=\"").Append(view.Width+gap).Append("\" y=\"10\" font-size=\"6\">").Append(H(label+" AFTER · SV5_08_FIX01 actual cells")).Append("</text>")
                .Append("<g transform=\"translate(0 16)\">").Append(PreviewCells(plan,view,before,false,"SV5_08_HISTORICAL_READ_ONLY")).Append("</g>")
                .Append("<g transform=\"translate(").Append(view.Width+gap).Append(" 16)\">").Append(PreviewCells(plan,view,after,true,plan.Digest)).Append("</g>");
            if(showLengthWitness)
            {
                var old=new[]{new[]{571,399},new[]{572,399},new[]{572,398},new[]{573,398},new[]{573,397},new[]{574,397},new[]{574,396},
                    new[]{575,396},new[]{575,395},new[]{576,395},new[]{576,394},new[]{577,394},new[]{577,393},new[]{578,393},new[]{578,392},
                    new[]{579,392},new[]{579,391},new[]{580,391},new[]{581,391},new[]{582,391},new[]{583,391},new[]{584,391},new[]{585,391},
                    new[]{586,391},new[]{587,391},new[]{588,391}};
                string points=string.Join(" ",old.Select(p=>(p[0]-view.X+0.5).ToString(CultureInfo.InvariantCulture)+","+
                    (16+view.MaxYExclusive-p[1]-0.5).ToString(CultureInfo.InvariantCulture)));
                text.Append("<polyline points=\"").Append(points).Append("\" fill=\"none\" stroke=\"#d12929\" stroke-width=\".5\"><title>historical 26-cell connector rejected by V2</title></polyline>");
                foreach(var link in plan.Infill.Links.Where(l=>l.ConnectionCenterline.Any(view.Contains)))
                {
                    string current=string.Join(" ",link.ConnectionCenterline.Where(view.Contains).Select(p=>(view.Width+gap+p.X-view.X+0.5).ToString(CultureInfo.InvariantCulture)+","+
                        (16+view.MaxYExclusive-p.Y-0.5).ToString(CultureInfo.InvariantCulture)));
                    if(current.Length>0) text.Append("<polyline points=\"").Append(current).Append("\" fill=\"none\" stroke=\"#087e8b\" stroke-width=\".45\"><title>").Append(H(link.Room+" inclusive="+link.ConnectionCellCount)).Append("</title></polyline>");
                }
            }
            return text.Append("<text x=\"0\" y=\"").Append(height-2).Append("\" font-size=\"3\">red=historical rejected 26-cell witness; teal=current connector; UNKNOWN/unplaced remains gray; Composed=false; Player=false</text></svg>\n").ToString();
        }
    }
}
