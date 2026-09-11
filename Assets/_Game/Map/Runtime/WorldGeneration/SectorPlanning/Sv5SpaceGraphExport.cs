using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class Sv5SpaceGraphExport
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        public static string DiversityProfileJson(Sv5DiversityProfile p) =>
            "{\"id\":"+J(p.Id)+",\"version\":"+J(p.Version)+",\"enabled\":"+B(p.Enabled)+
            ",\"radius\":"+N(p.Radius)+",\"weights\":["+string.Join(",",p.Weights.Select(N))+
            "],\"metric\":"+J(Sv5DiversityProfile.Metric)+",\"salt\":"+J(Sv5DiversityProfile.Salt)+
            ",\"aliases\":{"+string.Join(",",p.Aliases.Select(a=>J(a.Key)+":"+J(a.Value)))+
            "},\"exceptions\":[\"FIXED_CORE\",\"ORDINARY_ROOM\"],\"digest\":"+J(p.Digest)+"}";

        public static string DiversityComparisonJson(params Sv5SpaceGraphPlan[] plans)
        {
            CheckComparison(plans);
            return "{\n\"schema\":\"SV5_DIVERSITY_COMPARISON_V1\",\"composed_geometry_ready\":false,\"player_verified\":false,"+
                "\"cases\":["+string.Join(",\n",plans.Select((p,i)=>"{\"case\":"+J(CaseName(i))+
                ",\"policy\":"+DiversityProfileJson(p.Diversity.Profile)+",\"diversity_digest\":"+J(p.Diversity.Digest)+
                ",\"eligible_near_pairs\":"+N(p.Diversity.EligibleNearPairs)+",\"observation\":"+J(p.Diversity.Observation)+
                ",\"fallback_count\":"+N(p.Diversity.Decisions.Count(d=>d.Fallback))+
                ",\"family_counts\":{"+string.Join(",",p.Diversity.Parts.GroupBy(x=>p.Diversity.Profile.FamilyKey(x.Family))
                    .OrderBy(g=>g.Key,StringComparer.Ordinal).Select(g=>J(g.Key)+":"+N(g.Select(x=>x.FormationId).Distinct().Count())))+
                "},\"requests\":["+string.Join(",",p.Profile.Families.Select(f=>"{\"ordinal\":"+N(f.Ordinal)+
                    ",\"family\":"+J(f.Family)+",\"kind\":"+J(f.Kind.ToString())+",\"width\":"+N(f.Width)+
                    ",\"height\":"+N(f.Height)+",\"future_owner\":"+J(f.FutureOwner)+"}"))+
                "],\"validation\":"+ValidationJson(p)+",\"plan\":"+SpaceGraphJson(p)+"}"))+ "]\n}\n";
        }

        public static string DiversityDecisionsCsv(params Sv5SpaceGraphPlan[] plans) => Csv(
            "case,seed,request_id,formation_id,family_key,x,y,width,height,sector,sector_offset,rank,neighbor_ids,neighbor_count,weight,roll,decision,fallback,policy_digest,diversity_digest,plan_digest",
            plans.SelectMany((p,i)=>p.Diversity.Decisions.Select(d=>Row(CaseName(i),p.Seed,d.RequestId,d.FormationId,
                p.Diversity.Profile.FamilyKey(d.Family),d.Candidate.Bounds.X,d.Candidate.Bounds.Y,d.Candidate.Bounds.Width,d.Candidate.Bounds.Height,
                d.Candidate.Sector,d.Candidate.SectorOffset,d.Candidate.Rank,string.Join("|",d.Neighbors),d.Neighbors.Count,d.Weight,d.Roll,d.Outcome,d.Fallback,
                p.Diversity.Profile.Digest,p.Diversity.Digest,p.Digest))));

        public static string DiversityPairsCsv(params Sv5SpaceGraphPlan[] plans) => Csv(
            "case,seed,family_key,first_formation,second_formation,first_bounds,second_bounds,gap,near,eligible_near,exclusion,policy_digest,plan_digest",
            plans.SelectMany((p,i)=>p.Diversity.Pairs.Select(pair=>Row(CaseName(i),p.Seed,pair.Family,pair.First,pair.Second,
                pair.FirstBounds,pair.SecondBounds,pair.Gap,pair.Near,pair.EligibleNear,pair.Exclusion,p.Diversity.Profile.Digest,p.Digest))));

        public static void WriteDiversityComparison(string directory, params Sv5SpaceGraphPlan[] plans)
        {
            CheckComparison(plans);
            Directory.CreateDirectory(directory); Directory.CreateDirectory(Path.Combine(directory,"preview"));
            WriteAll(plans[1],Path.Combine(directory,"default")); WriteAll(plans[3],Path.Combine(directory,"repeat"));
            Write(Path.Combine(directory,"diversity.json"),DiversityComparisonJson(plans));
            Write(Path.Combine(directory,"decisions.csv"),DiversityDecisionsCsv(plans));
            Write(Path.Combine(directory,"pairs.csv"),DiversityPairsCsv(plans));
            Write(Path.Combine(directory,"preview/before_after.svg"),DiversityComparisonSvg(plans[2],plans[3],false));
            Write(Path.Combine(directory,"preview/detail.svg"),DiversityComparisonSvg(plans[2],plans[3],true));
            Write(Path.Combine(directory,"preview/index.html"),"<!doctype html><html lang=\"en\"><meta charset=\"utf-8\">"+
                "<title>SV5_07 actual diversity comparison</title><style>body{font:16px system-ui;margin:24px;color:#17212b}img{width:100%;border:1px solid #ccd}code{overflow-wrap:anywhere}</style>"+
                "<h1>Independent terrain diversity · seed 1304</h1><p>Repeat near pairs OFF "+plans[2].Diversity.EligibleNearPairs+
                " → ON "+plans[3].Diversity.EligibleNearPairs+". Default: "+H(plans[1].Diversity.Observation)+
                ". No place removed or shrunk. Same numbered request / family color before and after.</p>"+
                "<p>Outlines = planned footprints; blue lines = planned corridors; white = unassembled. Not finished terrain or Player evidence.</p>"+
                "<p><a href=\"../diversity.json\">Full OFF/ON geometry and metrics</a> · <a href=\"../decisions.csv\">Decisions</a> · <a href=\"../pairs.csv\">Pairs</a></p>"+
                "<a href=\"before_after.svg\"><img src=\"before_after.svg\" alt=\"Full 624 by 416 OFF and ON plans\"></a>"+
                "<a href=\"detail.svg\"><img src=\"detail.svg\" alt=\"Actual moved footprint and corridor detail with one-cell and four-cell grids\"></a>"+
                "<p>OFF <code>"+plans[2].Digest+"</code><br>ON <code>"+plans[3].Digest+"</code></p>"+
                "<p>ComposedGeometryReady=false; PlayerVerified=false. SV5_08 and later work remain pending.</p></html>\n");
        }

        private static string CaseName(int i) => new[] { "default/OFF","default/ON","repeat/OFF","repeat/ON" }[i];
        private static void CheckComparison(Sv5SpaceGraphPlan[] plans)
        {
            if(plans==null||plans.Length!=4||plans.Any(p=>p==null||!p.Success))
                throw new ArgumentException("Four validated default OFF/ON and repeat OFF/ON plans are required.");
            for(int i=0;i<4;i++) if(plans[i].Diversity.Profile.Enabled!=(i%2==1))
                throw new ArgumentException("Comparison policy order must be OFF, ON, OFF, ON.");
            for(int i=0;i<4;i+=2) if(plans[i].Seed!=plans[i+1].Seed||plans[i].Profile.Digest!=plans[i+1].Profile.Digest||
                plans[i].Core.Digest!=plans[i+1].Core.Digest) throw new ArgumentException("Comparison inputs must match.");
        }

        public static string DiversityComparisonSvg(Sv5SpaceGraphPlan off, Sv5SpaceGraphPlan on, bool detail)
        {
            var before=off.Diversity.Decisions.Where(d=>d.Selected).ToDictionary(d=>d.RequestId);
            var moved=on.Diversity.Decisions.Where(d=>d.Selected && !d.Candidate.Bounds.Equals(before[d.RequestId].Candidate.Bounds)).ToArray();
            var changed=moved.SelectMany(d=>new[]{d.Candidate.Bounds,before[d.RequestId].Candidate.Bounds}).ToArray();
            int x=0,y=0,w=624,h=416;
            if(detail&&changed.Length>0)
            {
                x=Math.Max(0,changed.Min(b=>b.X)-8)/4*4; y=Math.Max(0,changed.Min(b=>b.Y)-8)/4*4;
                w=Math.Min(624,changed.Max(b=>b.MaxXExclusive)+8)-x;
                h=Math.Min(416,changed.Max(b=>b.MaxYExclusive)+8)-y;
            }
            var s=new StringBuilder("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1320 620\" role=\"img\">");
            s.Append("<title>SV5_07 ").Append(detail?"actual changed corridor detail":"full 624x416 comparison")
                .Append("</title><desc>OFF ").Append(off.Digest).Append(" ON ").Append(on.Digest)
                .Append("; policy ").Append(on.Diversity.Profile.Digest).Append("; seed ").Append(on.Seed)
                .Append("; planned footprints and corridors, not composed terrain.</desc><rect width=\"1320\" height=\"620\" fill=\"white\"/>")
                .Append("<g font-family=\"sans-serif\" fill=\"#17212b\"><text x=\"20\" y=\"28\" font-size=\"20\">SV5_07 · ")
                .Append(detail?"Actual moved footprint / corridor detail":"Independent cave repetition · full world").Append("</text>")
                .Append("<text x=\"20\" y=\"50\" font-size=\"13\">White: unassembled | outline: planned place | blue: planned corridor | dark: confirmed fixed SOLID | red: gate</text>")
                .Append("<text x=\"20\" y=\"70\" font-size=\"13\">Thin grid 1 cell; thick grid 4x4 (detail). Dashed gray: other comparison footprint. Labels: stable request ordinal / family.</text>");
            Panel(off,on,20,"OFF"); Panel(on,off,680,"ON");
            s.Append("<text x=\"20\" y=\"544\" font-size=\"13\">Near eligible pairs: ").Append(off.Diversity.EligibleNearPairs).Append(" → ")
                .Append(on.Diversity.EligibleNearPairs).Append("; moved requests: ").Append(H(string.Join(", ",moved.Select(d=>d.RequestId))))
                .Append("</text><text x=\"20\" y=\"566\" font-size=\"13\">World window: ").Append(x).Append(",").Append(y).Append(" + ").Append(w).Append("x").Append(h)
                .Append(". Coordinate reachability is not floor / landing / headroom proof.</text>")
                .Append("<text x=\"20\" y=\"588\" font-size=\"13\">ComposedGeometryReady=false; PlayerVerified=false. No SV5_08 density or later terrain work performed.</text></g></svg>\n");
            return s.ToString();

            void Panel(Sv5SpaceGraphPlan p,Sv5SpaceGraphPlan other,int ox,string label)
            {
                s.Append("<text x=\"").Append(ox).Append("\" y=\"98\" font-size=\"16\">").Append(label).Append(" · ").Append(p.Places.Count)
                    .Append(" places / ").Append(p.Diversity.EligibleNearPairs).Append(" near pairs</text><svg x=\"").Append(ox)
                    .Append("\" y=\"110\" width=\"624\" height=\"416\" style=\"overflow:hidden\" viewBox=\"").Append(x).Append(' ').Append(416-y-h).Append(' ').Append(w).Append(' ').Append(h).Append("\">");
                s.Append("<g transform=\"translate(0 416) scale(1 -1)\">");
                if(detail)
                {
                    for(int gx=x;gx<=x+w;gx++) s.Append("<path d=\"M").Append(gx).Append(' ').Append(y).Append("v").Append(h)
                        .Append("\" stroke=\"").Append(gx%4==0?"#9aa7b2":"#dce2e6").Append("\" stroke-width=\"").Append(gx%4==0?"0.15":"0.05").Append("\"/>");
                    for(int gy=y;gy<=y+h;gy++) s.Append("<path d=\"M").Append(x).Append(' ').Append(gy).Append("h").Append(w)
                        .Append("\" stroke=\"").Append(gy%4==0?"#9aa7b2":"#dce2e6").Append("\" stroke-width=\"").Append(gy%4==0?"0.15":"0.05").Append("\"/>");
                }
                foreach(var c in p.Core.CoreCells.Where(c=>c.Protection==RmapSpecialProtectionKind.FixedSolid))
                    s.Append("<rect x=\"").Append(c.World.X).Append("\" y=\"").Append(c.World.Y).Append("\" width=\"1\" height=\"1\" fill=\"#37434d\"/>");
                foreach(var c in p.Connections) s.Append("<polyline points=\"").Append(string.Join(" ",c.Centerline.Select(v=>N(v.X)+","+N(v.Y))))
                    .Append("\" fill=\"none\" stroke=\"#377ec4\" stroke-width=\"0.45\"/>");
                if(detail) foreach(var d in moved) Rect(other.Diversity.Decisions.Single(k=>k.Selected&&k.RequestId==d.RequestId).Candidate.Bounds,"#777","2 1");
                foreach(var place in p.Places) Rect(place.Bounds,Color(p.Diversity.Profile.FamilyKey(place.Family)),"");
                foreach(var gate in p.Gates) foreach(var face in gate.BlockingFaces)
                {
                    double cx=(face.First.X+face.Second.X)/2.0+0.5,cy=(face.First.Y+face.Second.Y)/2.0+0.5;
                    bool vertical=face.First.X!=face.Second.X;
                    s.Append("<path d=\"M").Append(F(cx-(vertical?0:0.5))).Append(' ').Append(F(cy-(vertical?0.5:0)))
                        .Append(vertical?"v1":"h1").Append("\" stroke=\"#cf263d\" stroke-width=\"0.8\"/>");
                }
                s.Append("</g>");
                foreach(var place in p.Places)
                {
                    var decision=p.Diversity.Decisions.FirstOrDefault(d=>d.Selected&&d.FormationId==place.Id);
                    string number=decision==null?"CORE":decision.RequestId.Substring("SV5_REQUEST_".Length);
                    s.Append("<text x=\"").Append(place.Bounds.X+1).Append("\" y=\"").Append(416-place.Bounds.MaxYExclusive+5)
                        .Append("\" font-size=\"4\" fill=\"").Append(Color(p.Diversity.Profile.FamilyKey(place.Family))).Append("\">")
                        .Append(H(number+" "+p.Diversity.Profile.FamilyKey(place.Family))).Append("</text>");
                }
                s.Append("</svg>");
                void Rect(Sv5SpaceBounds b,string color,string dash) => s.Append("<rect x=\"").Append(b.X).Append("\" y=\"").Append(b.Y)
                    .Append("\" width=\"").Append(b.Width).Append("\" height=\"").Append(b.Height).Append("\" fill=\"none\" stroke=\"")
                    .Append(color).Append("\" stroke-width=\"0.7\" stroke-dasharray=\"").Append(dash).Append("\"/>");
            }
            string Color(string family)
            {
                string hash=StarNight.Map.WorldGeneration.WorldData.RmapWorldDefinition.Hash(family);
                return "#"+string.Join("",Enumerable.Range(0,3).Select(i=>(32+int.Parse(hash.Substring(i*2,2),NumberStyles.HexNumber)%128).ToString("x2")));
            }
            string F(double n) => n.ToString(CultureInfo.InvariantCulture);
        }

        public static void WriteAll(Sv5SpaceGraphPlan plan, string directory)
        {
            Require(plan);
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("An output directory is required.", nameof(directory));
            Directory.CreateDirectory(directory);
            string preview = Path.Combine(directory, "preview");
            Directory.CreateDirectory(preview);
            Write(Path.Combine(directory, "space_graph.json"), SpaceGraphJson(plan));
            Write(Path.Combine(directory, "places.csv"), PlacesCsv(plan));
            Write(Path.Combine(directory, "ports.csv"), PortsCsv(plan));
            Write(Path.Combine(directory, "connections.csv"), ConnectionsCsv(plan));
            Write(Path.Combine(directory, "segments.json"), SegmentsJson(plan));
            Write(Path.Combine(directory, "reservation_cells.csv"), ReservationCellsCsv(plan));
            Write(Path.Combine(directory, "state_proofs.json"), StateProofsJson(plan));
            Write(Path.Combine(directory, "contact_checks.csv"), ContactChecksCsv(plan));
            Write(Path.Combine(directory, "gate_geometry.json"), GateGeometryJson(plan));
            Write(Path.Combine(directory, "gate_state_checks.json"), GateStateChecksJson(plan));
            Write(Path.Combine(directory, "physical_contact_checks.json"), PhysicalContactChecksJson(plan));
            Write(Path.Combine(directory, "physical_gate_state_checks.json"), PhysicalGateStateChecksJson(plan));
            Write(Path.Combine(directory, "local_barrier_checks.json"), LocalBarrierChecksJson(plan));
            Write(Path.Combine(directory, "physical_transition_matrix.json"), PhysicalTransitionMatrixJson(plan));
            Write(Path.Combine(directory, "obligations.csv"), ObligationsCsv(plan));
            Write(Path.Combine(directory, "validation.json"), ValidationJson(plan));
            Write(Path.Combine(preview, "overview.svg"), OverviewSvg(plan));
            for (var row = 0; row < 4; row++)
            for (var column = 0; column < 4; column++)
                Write(Path.Combine(preview, ZoomName(row, column) + ".svg"), ZoomSvg(plan, row, column));
            Write(Path.Combine(preview, "W01_before_after.svg"), WitnessSvg(plan,
                "W01_SEAL_ENTRY_REACHABLE", "W01 - Forge-complete Seal entry"));
            Write(Path.Combine(preview, "W02_before_after.svg"), WitnessSvg(plan,
                "W02_BOSS_APPROACH_REACHABLE", "W02 - Seal-open Boss approach"));
            Write(Path.Combine(preview, "FIX02_bypass_before_after.svg"), BypassSvg(plan));
            Write(Path.Combine(preview, "index.html"), IndexHtml(plan));
        }

        public static string SegmentsJson(Sv5SpaceGraphPlan plan)
        {
            var text = new StringBuilder("{\n  \"plan_digest\": " + J(plan.Digest) + ",\n  \"segments\": [\n");
            AppendObjects(text, plan.Segments.Select(s => "    {\"segment_id\":" + J(s.Id) +
                ",\"connection_id\":" + J(s.ConnectionId) + ",\"kind\":" + J(s.Kind.ToString()) +
                ",\"scope\":" + J(s.RegionId) + ",\"gate_id\":" + J(s.GateId) + ",\"predicate\":" +
                J(s.Predicate == null ? "ACTIONLESS" : s.Predicate.StableToken) + ",\"source\":" + Point(s.Source) +
                ",\"target\":" + Point(s.Target) + ",\"centerline\":[" + string.Join(",",s.Centerline.Select(Point)) +
                "],\"aperture_cells\":[" + string.Join(",",s.ApertureCells.Select(Point)) + "]}"));
            return text.Append("  ]\n}\n").ToString();
        }

        public static string LocalBarrierChecksJson(Sv5SpaceGraphPlan plan)
        {
            var text = new StringBuilder("{\n  \"plan_digest\": " + J(plan.Digest) +
                ",\n  \"product_digest\": " + J(plan.PhysicalProduct.SemanticDigest) + ",\n  \"checks\": [\n");
            AppendObjects(text, plan.ContactDecisions.Where(c => c.Crossing == Sv5SpaceCrossingKind.ConditionalGate)
                .Select(c => {
                    var gate = plan.Gates.Single(g => g.BoundaryId == c.BoundaryId);
                    var errors = Sv5SpaceGraphValidator.ValidateBarrierFixture(c.Source, gate.BlockingCells, gate.BlockingFaces);
                    return "    {\"contact_id\":" + J(c.Source.Id) + ",\"kind\":" + J(c.Source.Kind) +
                        ",\"first\":" + Point(c.Source.FirstWorld) + ",\"second\":" + Point(c.Source.SecondWorld) +
                        ",\"gate_id\":" + J(gate.Id) + ",\"success\":" + B(errors.Count == 0) +
                        ",\"errors\":[" + string.Join(",", errors.Select(J)) + "]}";
                }));
            return text.Append("  ]\n}\n").ToString();
        }

        public static string PhysicalTransitionMatrixJson(Sv5SpaceGraphPlan plan)
        {
            var text = new StringBuilder("{\n  \"plan_digest\": " + J(plan.Digest) +
                ",\n  \"product_digest\": " + J(plan.PhysicalProduct.SemanticDigest) + ",\n  \"transitions\": [\n");
            AppendObjects(text, plan.PhysicalProduct.Matrix.Select(r => "    {\"resource_order\":" + J(r.ResourceOrder) +
                ",\"fsm_state\":" + J(r.State.StableToken) + ",\"connection_id\":" + J(r.Connection.Id) +
                ",\"direction\":" + J(r.Reverse ? "REVERSE" : "FORWARD") + ",\"expected_predicate\":" +
                J(new Sv5SpaceGatePredicate(r.Edge.RequiredResourceMask, r.Edge.RequiresForge, r.Edge.RequiresSeal,
                    r.Edge.RequiresBossComplete).StableToken) + ",\"expected_open\":" + B(r.ExpectedOpen) +
                ",\"closed_gate_ids\":[" + string.Join(",",r.ClosedGateIds.Select(J)) + "],\"open_gate_ids\":[" +
                string.Join(",",r.OpenGateIds.Select(J)) + "],\"physical_reachable\":" + B(r.Reachability.TargetPortReachable) +
                ",\"witness_id\":" + J(r.WitnessId) + ",\"blocked_reason\":" +
                J(r.Reachability.TargetPortReachable ? "" : "NO_GLOBAL_CARDINAL_PATH_WITH_CLOSED_GATES") +
                ",\"success\":" + B(r.Success) + "}"));
            text.Append("  ],\n  \"witnesses\": [\n");
            AppendObjects(text, plan.PhysicalProduct.Matrix.Select(r => r.Reachability).GroupBy(r => r.WitnessId)
                .Select(g => g.First()).OrderBy(r => r.WitnessId).Select(r => "    {\"id\":" + J(r.WitnessId) +
                    ",\"cells\":[" + string.Join(",",r.Witness.Select(Point)) + "]}"));
            return text.Append("  ]\n}\n").ToString();
        }

        public static string RepairDetailSvg(Sv5SpaceGraphPlan plan,
            IEnumerable<Sv5SpaceConnection> beforeConnections, IEnumerable<Sv5SpaceGate> beforeGates,
            bool initialDeepStar = false, int beforeLocalErrors = -1)
        {
            int minX = initialDeepStar ? 448 : 524, minY = initialDeepStar ? 328 : 306;
            const int width = 16, height = 22, scale = 24;
            var svg = new StringBuilder("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"920\" height=\"740\" viewBox=\"0 0 920 740\">");
            svg.Append("<rect width=\"920\" height=\"740\" fill=\"#fff\"/><g font-family=\"sans-serif\" fill=\"#17212b\">")
                .Append("<text x=\"24\" y=\"28\" font-size=\"18\">FIX04: ").Append(initialDeepStar ? "DeepStar INITIAL collateral cut" : "Forge / Seal corridor detail")
                .Append(" (not composed terrain)</text>")
                .Append("<text x=\"24\" y=\"52\" font-size=\"12\">Dark: confirmed SOLID | blue outline: planned passage | white: unplaced | red: closed gate face</text>")
                .Append("<text x=\"24\" y=\"70\" font-size=\"12\">Thin grid: 1 cell | thick grid: 4 x 4 pattern boundary | green: coordinate-search witness</text>");
            Panel(24,"BEFORE: exact FIX03",beforeConnections.ToArray(),beforeGates.ToArray());
            Panel(474,"AFTER: accepted FIX04",plan.Connections.ToArray(),plan.Gates.ToArray());
            svg.Append("<text x=\"24\" y=\"674\" font-size=\"12\">Viewport x=").Append(minX).Append("..").Append(minX+width-1)
                .Append(", y=").Append(minY).Append("..").Append(minY+height-1).Append("; S / T = visible planned-corridor entry / end.</text>")
                .Append("<text x=\"24\" y=\"695\" font-size=\"12\">").Append(initialDeepStar ?
                    "State INITIAL, all gates closed; local barrier errors before=" + beforeLocalErrors + ", after=" +
                    Sv5SpaceGraphValidator.FindGateErrors(plan.ContactDecisions,plan.Gates).Count :
                    "State: resources=7, forge=true, seal=false, boss=false. Offscreen gates still apply globally.").Append("</text>")
                .Append("<text x=\"24\" y=\"716\" font-size=\"12\">ComposedGeometryReady=false; PlayerVerified=false. No floor/landing/headroom completion is claimed.</text>")
                .Append("</g><!-- ").Append(plan.Digest).Append(" --></svg>\n");
            return svg.ToString();

            void Panel(int ox,string label,Sv5SpaceConnection[] connections,Sv5SpaceGate[] gates)
            {
                int X(int x) => ox+(x-minX)*scale;
                int Y(int y) => 120+(minY+height-1-y)*scale;
                bool Visible(RmapSpecialWorldPoint p) => p.X>=minX && p.X<minX+width && p.Y>=minY && p.Y<minY+height;
                var passage = new HashSet<RmapSpecialWorldPoint>(connections.SelectMany(c => c.Centerline.Concat(c.ApertureCells)));
                var solid = new HashSet<RmapSpecialWorldPoint>(plan.Core.CoreCells.Where(c => c.Protection == RmapSpecialProtectionKind.FixedSolid).Select(c => c.World)
                    .Concat(plan.Core.RouteCells.Where(c => c.RequiredBaseCell == StarNight.Map.WorldGeneration.MicroPatterns.RmapPatternBaseCell.Solid).Select(c => c.World)));
                var forge = initialDeepStar ? connections.Single(c => c.Id == "SV5_CORE_CONN_93f752cf7b8f813c") :
                    connections.Single(c => c.Condition == "FORGE_GATED_SEAL_APPROACH");
                var reach = Sv5SpacePhysicalMovement.Evaluate(plan.Core,connections,gates,forge.Id,
                    initialDeepStar ? 0UL : 7UL,!initialDeepStar,false,false);
                svg.Append("<text x=\"").Append(ox).Append("\" y=\"101\" font-size=\"15\">").Append(label).Append("; reachable=").Append(reach.TargetPortReachable).Append("</text>");
                for(int y=minY;y<minY+height;y++) for(int x=minX;x<minX+width;x++)
                {
                    var p = new RmapSpecialWorldPoint(x,y);
                    svg.Append("<rect x=\"").Append(X(x)).Append("\" y=\"").Append(Y(y)).Append("\" width=\"24\" height=\"24\" fill=\"")
                        .Append(solid.Contains(p)?"#38434e":"#fff").Append("\" stroke=\"#c7ccd1\" stroke-width=\"0.5\"/>");
                    if(passage.Contains(p)) svg.Append("<rect x=\"").Append(X(x)+3).Append("\" y=\"").Append(Y(y)+3).Append("\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"#2376bd\" stroke-dasharray=\"3 2\"/>");
                }
                for(int x=minX;x<=minX+width;x++) if(x%4==0)
                    Line(X(x),120,X(x),120+height*scale,"#66717c",1.5);
                for(int y=minY;y<=minY+height;y++) if(y%4==0)
                    Line(ox,Y(y)+scale,ox+width*scale,Y(y)+scale,"#66717c",1.5);
                foreach(var g in gates.Where(g => !g.TypedPredicate.IsOpen(initialDeepStar ? 0UL : 7UL,!initialDeepStar,false,false))) foreach(var f in g.BlockingFaces)
                    if(Visible(f.First)&&Visible(f.Second))
                    {
                        int cx=(X(f.First.X)+X(f.Second.X))/2+12,cy=(Y(f.First.Y)+Y(f.Second.Y))/2+12;
                        if(f.First.X!=f.Second.X) Line(cx,cy-12,cx,cy+12,"#d32337",4);
                        else Line(cx-12,cy,cx+12,cy,"#d32337",4);
                    }
                for(int i=1;i<reach.Witness.Count;i++) if(Visible(reach.Witness[i-1])&&Visible(reach.Witness[i]))
                    Line(X(reach.Witness[i-1].X)+12,Y(reach.Witness[i-1].Y)+12,X(reach.Witness[i].X)+12,Y(reach.Witness[i].Y)+12,"#19884a",3);
                var visible = forge.Centerline.Where(Visible).ToArray();
                if(visible.Length>0) foreach(var marker in new[] { new { P=visible.First(),Label="S" },new { P=visible.Last(),Label="T" } })
                    svg.Append("<text x=\"").Append(X(marker.P.X)+5).Append("\" y=\"").Append(Y(marker.P.Y)+18).Append("\" font-size=\"16\" fill=\"#9b3b03\">").Append(marker.Label).Append("</text>");
                void Line(int x1,int y1,int x2,int y2,string color,double stroke) => svg.Append("<line x1=\"").Append(x1).Append("\" y1=\"").Append(y1)
                    .Append("\" x2=\"").Append(x2).Append("\" y2=\"").Append(y2).Append("\" stroke=\"").Append(color).Append("\" stroke-width=\"")
                    .Append(stroke.ToString(CultureInfo.InvariantCulture)).Append("\"/>");
            }
        }

        public static string SpaceGraphJson(Sv5SpaceGraphPlan plan)
        {
            Require(plan);
            var text = new StringBuilder();
            text.Append("{\n  \"schema\": \"SV5_SPACE_GRAPH_FIX03_V1\",\n")
                .Append("  \"world\": {\"width\":624,\"height\":416,\"origin\":\"BOTTOM_LEFT\",\"bounds\":\"HALF_OPEN\",\"micro_chunk\":[12,8],\"pattern\":[4,4]},\n")
                .Append("  \"seed\": ").Append(plan.Seed.ToString(CultureInfo.InvariantCulture)).Append(",\n")
                .Append("  \"profile\": {\"id\":").Append(J(plan.Profile.Id)).Append(",\"version\":")
                .Append(J(plan.Profile.Version)).Append(",\"digest\":").Append(J(plan.Profile.Digest)).Append("},\n")
                .Append("  \"source\": {\"world_digest\":").Append(J(plan.Core.RouteSource.Definition.Digest))
                .Append(",\"core_digest\":").Append(J(plan.Core.Digest)).Append(",\"graph_digest\":")
                .Append(J(plan.Core.RouteSource.Graph.Digest)).Append(",\"core_sites\":8,\"core_cells\":2432},\n")
                .Append("  \"plan_digest\": ").Append(J(plan.Digest)).Append(",\n")
                .Append("  \"diversity_profile\": ").Append(DiversityProfileJson(plan.Diversity.Profile)).Append(",\n")
                .Append("  \"diversity_digest\": ").Append(J(plan.Diversity.Digest)).Append(",\n")
                .Append("  \"physical_movement_digest\": ").Append(J(plan.PhysicalMovement.SemanticDigest)).Append(",\n")
                .Append("  \"readiness\": {\"planned_layout\":true,\"logical_state\":true,\"contact_coverage\":true,\"global_coordinate_movement\":true,\"planned_gate_geometry\":true,\"planned_gate_state\":true,\"composed_geometry\":false,\"player\":false},\n")
                .Append("  \"infill\": {\"owner\":\"SV5_08_INFILL\",\"state\":\"INFILL_PENDING\",\"tile_count\":")
                .Append(plan.InfillPendingTileCount.ToString(CultureInfo.InvariantCulture)).Append("},\n")
                .Append("  \"places\": [\n");
            AppendObjects(text, plan.Places.Select(value => "    {\"id\":" + J(value.Id) + ",\"family\":" +
                J(value.Family) + ",\"formation_id\":"+J(value.FormationId)+",\"family_key\":"+J(plan.Diversity.Profile.FamilyKey(value.Family))+
                ",\"kind\":" + J(value.Kind.ToString()) + ",\"bounds\":{" +
                "\"x\":" + N(value.Bounds.X) + ",\"y\":" + N(value.Bounds.Y) + ",\"width\":" +
                N(value.Bounds.Width) + ",\"height\":" + N(value.Bounds.Height) + "},\"core_site_id\":" +
                J(value.CoreSiteId) + ",\"future_owner\":" + J(value.FutureOwner) + ",\"readiness\":" +
                J(value.Readiness) + "}"));
            text.Append("  ],\n  \"ports\": [\n");
            AppendObjects(text, plan.Ports.Select(value => "    {\"id\":" + J(value.Id) + ",\"place_id\":" +
                J(value.PlaceId) + ",\"anchor\":" + Point(value.Anchor) + ",\"direction\":" +
                J(value.Direction.ToString()) + ",\"flow\":" + J(value.Flow) + ",\"condition\":" +
                J(value.Condition) + ",\"source_access_id\":" + J(value.SourceAccessId) + ",\"status\":" +
                J(value.Status) + ",\"source_node_id\":"+J(value.SourceNodeId)+",\"boundary_cells\":["+string.Join(",",value.BoundaryCells.Select(Point))+"]}"));
            text.Append("  ],\n  \"connections\": [\n");
            AppendObjects(text, plan.Connections.Select(value => "    {\"id\":" + J(value.Id) + ",\"kind\":" +
                J(value.Kind.ToString()) + ",\"from_port\":" + J(value.FromPortId) + ",\"to_port\":" +
                J(value.ToPortId) + ",\"direction\":" + J(value.Direction.ToString()) + ",\"flow\":" +
                J(value.Flow) + ",\"condition\":" + J(value.Condition) + ",\"source_graph_edge_id\":" +
                J(value.SourceGraphEdgeId) + ",\"selection_state\":" + J(value.SelectionState) +
                ",\"centerline\":[" + string.Join(",", value.Centerline.Select(Point)) + "],\"envelope\":[" +
                string.Join(",", value.Envelope.Select(Point)) + "],\"aperture_cells\":[" +
                string.Join(",", value.ApertureCells.Select(Point)) + "]}"));
            text.Append("  ],\n  \"gates\": [\n");
            AppendObjects(text, plan.Gates.Select(value => "    {\"id\":" + J(value.Id) + ",\"boundary_id\":" +
                J(value.BoundaryId) + ",\"contact_ids\":[" + string.Join(",", value.ContactIds.Select(J)) +
                "],\"side_a_anchor\":" + Point(value.SideAAnchor) + ",\"side_b_anchor\":" +
                Point(value.SideBAnchor) + ",\"direction\":" + J(value.Direction.ToString()) + ",\"flow\":" +
                J(value.Flow) + ",\"predicate\":" + J(value.Predicate) + ",\"typed_predicate\":{" +
                "\"required_resource_mask\":" + value.TypedPredicate.RequiredResourceMask.ToString(CultureInfo.InvariantCulture) +
                ",\"requires_forge\":" + B(value.TypedPredicate.RequiresForge) + ",\"requires_seal\":" +
                B(value.TypedPredicate.RequiresSeal) + ",\"requires_boss_complete\":" +
                B(value.TypedPredicate.RequiresBossComplete) + "},\"source_connection_id\":" +
                J(value.SourceConnectionId) + ",\"source_route_id\":" + J(value.SourceRouteId) +
                ",\"source_port_id\":" + J(value.SourcePortId) + ",\"target_port_id\":" +
                J(value.TargetPortId) + ",\"crossing\":" +
                J(value.Crossing.ToString()) + ",\"blocking_cells\":[" +
                string.Join(",", value.BlockingCells.Select(Point)) + "],\"blocking_faces\":[" +
                string.Join(",", value.BlockingFaces.Select(item => J(item.StableToken))) +
                "],\"planned_barrier_verified\":" + B(value.PlannedBarrierVerified) +
                ",\"runtime_verified\":false}"));
            text.Append("  ],\n  \"counts\": {\"places\":").Append(N(plan.Places.Count)).Append(",\"ports\":")
                .Append(N(plan.Ports.Count)).Append(",\"connections\":").Append(N(plan.Connections.Count))
                .Append(",\"contacts\":").Append(N(plan.ContactDecisions.Count)).Append(",\"gates\":")
                .Append(N(plan.Gates.Count)).Append(",\"reservation_rows\":").Append(N(plan.Reservations.Count))
                .Append("}\n}\n");
            return text.ToString();
        }

        public static string PlacesCsv(Sv5SpaceGraphPlan plan) => Csv(
            "place_id,family,kind,x,y,width,height,max_x_exclusive,max_y_exclusive,core_site_id,future_owner,distribution_sector,readiness,plan_digest",
            Require(plan).Places.Select(value => Row(value.Id, value.Family, value.Kind, value.Bounds.X,
                value.Bounds.Y, value.Bounds.Width, value.Bounds.Height, value.Bounds.MaxXExclusive,
                value.Bounds.MaxYExclusive, value.CoreSiteId, value.FutureOwner, value.DistributionSector,
                value.Readiness, plan.Digest)));

        public static string PortsCsv(Sv5SpaceGraphPlan plan) => Csv(
            "port_id,place_id,boundary_cells,anchor_x,anchor_y,direction,flow,clearance,condition,source_access_id,source_node_id,status,plan_digest",
            Require(plan).Ports.Select(value => Row(value.Id, value.PlaceId, Cells(value.BoundaryCells), value.Anchor.X,
                value.Anchor.Y, value.Direction, value.Flow, "ONE_TILE_ENVELOPE", value.Condition,
                value.SourceAccessId, value.SourceNodeId, value.Status, plan.Digest)));

        public static string ConnectionsCsv(Sv5SpaceGraphPlan plan) => Csv(
            "connection_id,kind,from_port_id,to_port_id,from_place_id,to_place_id,direction,flow,condition,source_graph_edge_id,selection_state,centerline_count,centerline,envelope_count,envelope,aperture_count,aperture_cells,plan_digest",
            Require(plan).Connections.Select(value => Row(value.Id, value.Kind, value.FromPortId, value.ToPortId,
                value.FromPlaceId, value.ToPlaceId, value.Direction, value.Flow, value.Condition,
                value.SourceGraphEdgeId, value.SelectionState, value.Centerline.Count, Cells(value.Centerline),
                value.Envelope.Count, Cells(value.Envelope), value.ApertureCells.Count,
                Cells(value.ApertureCells), plan.Digest)));

        public static string ReservationCellsCsv(Sv5SpaceGraphPlan plan) => Csv(
            "world_x,world_y,reservation_kind,owner_id,semantics,micro_chunk_x,micro_chunk_y,pattern_x,pattern_y,is_final_tile,plan_digest",
            Require(plan).Reservations.Select(value => Row(value.World.X, value.World.Y, value.Kind, value.OwnerId,
                value.Semantics, value.MicroChunkX, value.MicroChunkY, value.PatternX, value.PatternY, false,
                plan.Digest)));

        public static string ContactChecksCsv(Sv5SpaceGraphPlan plan) => Csv(
            "contact_id,kind,first_x,first_y,second_x,second_y,direction,route_a,route_a_x,route_a_y,route_a_kinds,route_b,route_b_x,route_b_y,route_b_kinds,first_kinds,second_kinds,split_node_id,crossing,predicate,boundary_id,coverage_checked,logical_state_transition_checked,geometry_state,player_state,detail,plan_digest",
            Require(plan).ContactDecisions.Select(value => Row(value.Source.Id, value.Source.Kind,
                value.Source.FirstWorld.X, value.Source.FirstWorld.Y, value.Source.SecondWorld.X,
                value.Source.SecondWorld.Y, value.Source.Direction, value.Source.RouteA, value.Source.RouteAWorld.X,
                value.Source.RouteAWorld.Y, value.Source.RouteAKinds, value.Source.RouteB,
                value.Source.RouteBWorld.X, value.Source.RouteBWorld.Y, value.Source.RouteBKinds,
                value.Source.FirstKinds, value.Source.SecondKinds, value.SplitNodeId, value.Crossing, value.Predicate,
                value.BoundaryId, value.CoverageChecked, value.LogicalStateTransitionChecked, value.GeometryState,
                value.PlayerState, value.Detail, plan.Digest)));

        public static string GateGeometryJson(Sv5SpaceGraphPlan plan)
        {
            Require(plan);
            return "{\n" +
                "  \"schema\": \"SV5_PLANNED_GATE_GEOMETRY_FIX03_V1\",\n" +
                "  \"plan_digest\": " + J(plan.Digest) + ",\n" +
                "  \"composed_geometry_ready\": false,\n" +
                "  \"player_verified\": false,\n" +
                "  \"gates\": [\n" + string.Join(",\n", plan.Gates.Select(value =>
                    "    {\"gate_id\":" + J(value.Id) + ",\"boundary_id\":" + J(value.BoundaryId) +
                    ",\"contact_ids\":[" + string.Join(",", value.ContactIds.Select(J)) +
                    "],\"blocking_cells\":[" + string.Join(",", value.BlockingCells.Select(Point)) +
                    "],\"blocking_faces\":[" + string.Join(",", value.BlockingFaces.Select(item =>
                        "{\"first\":" + Point(item.First) + ",\"second\":" + Point(item.Second) + "}")) +
                    "],\"side_a_anchor\":" + Point(value.SideAAnchor) + ",\"side_b_anchor\":" +
                    Point(value.SideBAnchor) + ",\"direction\":" + J(value.Direction.ToString()) +
                    ",\"flow\":" + J(value.Flow) + ",\"predicate\":" + J(value.Predicate) +
                    ",\"typed_predicate\":{\"required_resource_mask\":" +
                    value.TypedPredicate.RequiredResourceMask.ToString(CultureInfo.InvariantCulture) +
                    ",\"requires_forge\":" + B(value.TypedPredicate.RequiresForge) +
                    ",\"requires_seal\":" + B(value.TypedPredicate.RequiresSeal) +
                    ",\"requires_boss_complete\":" + B(value.TypedPredicate.RequiresBossComplete) +
                    "},\"source_connection_id\":" + J(value.SourceConnectionId) +
                    ",\"source_route_id\":" + J(value.SourceRouteId) + ",\"source_port_id\":" +
                    J(value.SourcePortId) + ",\"target_port_id\":" + J(value.TargetPortId) +
                    ",\"sealed_state\":" + J(value.SealedState) + ",\"open_state\":" + J(value.OpenState) +
                    ",\"planned_barrier_verified\":" + B(value.PlannedBarrierVerified) + "}")) +
                "\n  ]\n}\n";
        }

        public static string GateStateChecksJson(Sv5SpaceGraphPlan plan)
        {
            Require(plan);
            return "{\n" +
                "  \"schema\": \"SV5_GATE_STATE_CHECKS_FIX03_V1\",\n" +
                "  \"plan_digest\": " + J(plan.Digest) + ",\n" +
                "  \"movement_space\": \"GLOBAL_WORLD_PASSAGE_AND_APERTURE;CLEARANCE_AND_INFILL_PENDING_EXCLUDED\",\n" +
                "  \"composed_geometry_ready\": false,\n" +
                "  \"player_verified\": false,\n" +
                "  \"checks\": [\n" + string.Join(",\n", plan.GateStateChecks.Select(value =>
                "    {\"id\":" + J(value.Id) + ",\"gate_id\":" + J(value.GateId) +
                    ",\"connection_id\":" + J(value.ConnectionId) + ",\"source_port_id\":" +
                    J(value.SourcePortId) + ",\"target_port_id\":" + J(value.TargetPortId) +
                    ",\"state\":{\"resource_mask\":" + value.ResourceMask.ToString(CultureInfo.InvariantCulture) +
                    ",\"forge_made\":" + B(value.ForgeMade) + ",\"seal_open\":" + B(value.SealOpen) +
                    ",\"boss_complete\":" + B(value.BossComplete) + "},\"expected_open\":" +
                    B(value.ExpectedOpen) + ",\"actual_open\":" + B(value.ActualOpen) +
                    ",\"source_anchor_reachable\":" + B(value.SourceAnchorReachable) +
                    ",\"target_port_reachable\":" + B(value.TargetPortReachable) +
                    ",\"sealed_cut_verified\":" + B(value.SealedCutVerified) +
                    ",\"open_path_verified\":" + B(value.OpenPathVerified) +
                    ",\"checked_cells\":" + N(value.CheckedCells) + ",\"checked_faces\":" +
                    N(value.CheckedFaces) + ",\"success\":" + B(value.Success) + ",\"evidence\":" +
                    J(value.Evidence) + "}")) + "\n  ]\n}\n";
        }

        public static string PhysicalContactChecksJson(Sv5SpaceGraphPlan plan)
        {
            Require(plan);
            return "{\n" +
                "  \"schema\": \"SV5_PHYSICAL_CONTACT_CHECKS_FIX03_V1\",\n" +
                "  \"plan_digest\": " + J(plan.Digest) + ",\n" +
                "  \"physical_movement_digest\": " + J(plan.PhysicalMovement.SemanticDigest) + ",\n" +
                "  \"movement_space\": \"GLOBAL_WORLD_PASSAGE_AND_APERTURE;CLEARANCE_AND_INFILL_PENDING_EXCLUDED\",\n" +
                "  \"checks\": [\n" + string.Join(",\n", plan.PhysicalMovement.ContactChecks.Select(value =>
                    "    {\"contact_id\":" + J(value.Source.Source.Id) + ",\"kind\":" +
                    J(value.Source.Source.Kind.ToString()) + ",\"first\":" + Point(value.Source.Source.FirstWorld) +
                    ",\"second\":" + Point(value.Source.Source.SecondWorld) +
                    ",\"first_kinds\":" + J(value.Source.Source.FirstKinds) +
                    ",\"second_kinds\":" + J(value.Source.Source.SecondKinds) +
                    ",\"route_a_kinds\":" + J(value.Source.Source.RouteAKinds) +
                    ",\"route_b_kinds\":" + J(value.Source.Source.RouteBKinds) +
                    ",\"crossing\":" +
                    J(value.Source.Crossing.ToString()) + ",\"boundary_id\":" + J(value.Source.BoundaryId) +
                    ",\"geometry_owner_id\":" + J(value.GeometryOwnerId) +
                    ",\"decision\":" + J(value.Source.Crossing == Sv5SpaceCrossingKind.Join ?
                        "GLOBAL_JOIN" : value.Source.Source.Kind == "SHARED" ?
                        "TYPED_GATE_GLOBAL_REGION_CUT" : "TYPED_GATE_GLOBAL_FACE_CUT") +
                    ",\"first_traversable\":" + B(value.FirstTraversable) +
                    ",\"second_traversable\":" + B(value.SecondTraversable) +
                    ",\"states\":[" + string.Join(",", value.States.Select(J)) +
                    "],\"success\":" + B(value.Success) + "}")) + "\n  ]\n}\n";
        }

        public static string PhysicalGateStateChecksJson(Sv5SpaceGraphPlan plan)
        {
            Require(plan);
            return "{\n" +
                "  \"schema\": \"SV5_PHYSICAL_GATE_STATE_CHECKS_FIX03_V1\",\n" +
                "  \"plan_digest\": " + J(plan.Digest) + ",\n" +
                "  \"physical_movement_digest\": " + J(plan.PhysicalMovement.SemanticDigest) + ",\n" +
                "  \"all_gates_applied_simultaneously\": true,\n" +
                "  \"checks\": [\n" + string.Join(",\n", plan.PhysicalMovement.GateStateChecks.Select(value =>
                    "    {\"id\":" + J(value.Id) + ",\"connection_id\":" + J(value.ConnectionId) +
                    ",\"source_port_id\":" + J(value.SourcePortId) + ",\"target_port_id\":" +
                    J(value.TargetPortId) + ",\"state\":{\"resource_mask\":" +
                    value.ResourceMask.ToString(CultureInfo.InvariantCulture) + ",\"forge_made\":" +
                    B(value.ForgeMade) + ",\"seal_open\":" + B(value.SealOpen) +
                    ",\"boss_complete\":" + B(value.BossComplete) + "},\"expected_reachable\":" +
                    B(value.ExpectedReachable) + ",\"reachable\":" + B(value.Reachable) +
                    ",\"source_anchor_reachable\":" + B(value.SourceAnchorReachable) +
                    ",\"closed_gate_ids\":[" + string.Join(",", value.ClosedGateIds.Select(J)) +
                    "],\"open_gate_ids\":[" + string.Join(",", value.OpenGateIds.Select(J)) +
                    "],\"checked_cells\":" + N(value.CheckedCells) + ",\"checked_faces\":" +
                    N(value.CheckedFaces) + ",\"witness\":[" + string.Join(",", value.Witness.Select(Point)) +
                    "],\"success\":" + B(value.Success) + "}")) + "\n  ]\n}\n";
        }

        public static string ObligationsCsv(Sv5SpaceGraphPlan plan)
        {
            Require(plan);
            var rows = new List<string[]>();
            foreach (IGrouping<string, Sv5SpacePlace> group in plan.Places.Where(value => value.Kind !=
                         Sv5SpacePlaceKind.Core).GroupBy(value => value.FutureOwner).OrderBy(value => value.Key,
                         StringComparer.Ordinal))
                rows.Add(new[] { "FAMILY_" + group.Key, group.Key, "Implement reserved shells for " +
                    string.Join("|", group.Select(value => value.Id)), "PENDING", "PLANNED_LAYOUT" });
            rows.Add(new[] { "SV5_07_DISTRIBUTION", "SV5_07_DIVERSITY", "Refine repeated-family distribution without changing the 06 ownership contract.", "PENDING", "PLANNED_LAYOUT" });
            rows.Add(new[] { "SV5_08_INFILL", "SV5_08_INFILL", "Fill INFILL_PENDING only; do not reinterpret it as AIR or SOLID before that task.", "PENDING", "PLANNED_LAYOUT" });
            rows.Add(new[] { "SV5_09_CONTACT_RECHECK", "SV5_09_LOOPS", "Re-run complete contact projection after loop candidates are added.", "PENDING", "CONTACT_STATE" });
            rows.Add(new[] { "SV5_10_SIDEPATH", "SV5_10_SIDEPATH", "Evaluate optional side-path dead ends against the same reverse-reachability gate.", "PENDING", "LOGICAL_STATE" });
            rows.Add(new[] { "SV5_41_COMPOSE", "SV5_41_COMPOSE", "Materialize planned envelopes and gates, then promote composed geometry only with collision evidence.", "PENDING", "COMPOSED_GEOMETRY" });
            rows.Add(new[] { "SV5_42_FINAL_SCAN", "SV5_42_FINAL_SCAN", "Run final 3-4 tile gap and 6x6 solid-window scans.", "PENDING", "COMPOSED_GEOMETRY" });
            rows.Add(new[] { "SV5_44_PLAYER", "SV5_44_WORLD_PLAYER", "Run actual whole-world Player traversal after composed geometry exists.", "PENDING", "PLAYER" });
            rows.Add(new[] { "SG06_F5_SPARSE_SINGLE_CIRCUIT", "SV5_07_08_09", "Resolve sparse 22-place single-circuit structure; FIX02 repairs typed state-gate correctness only.", "PENDING", "WHOLE_WORLD_STRUCTURE" });
            return Csv("obligation_id,owner_task,requirement,readiness,verification_layer", rows.Select(Row));
        }

        public static string StateProofsJson(Sv5SpaceGraphPlan plan)
        {
            Require(plan);
            return "{\n" +
                "  \"schema\": \"SV5_SPACE_STATE_PROOFS_FIX03_V1\",\n" +
                "  \"plan_digest\": " + J(plan.Digest) + ",\n" +
                "  \"baseline\": {\"source\":\"RMAP13\",\"edge_count\":" + N(plan.Core.RouteSource.Graph.Edges.Count) +
                    ",\"proof_count\":" + N(plan.Core.RouteSource.Graph.Proofs.Count) + ",\"pass\":" +
                    B(plan.Core.RouteSource.Graph.Success) + "},\n" +
                "  \"candidate_set\": {\"source\":\"SV5_05_FIX01_API\",\"status\":\"PRESERVED_SEPARATE\"},\n" +
                "  \"gate_bindings\": [" + string.Join(",", plan.Gates.Select(value =>
                    "{\"gate_id\":" + J(value.Id) + ",\"connection_id\":" + J(value.SourceConnectionId) +
                    ",\"typed_predicate\":" + J(value.TypedPredicate.StableToken) + "}")) + "],\n" +
                "  \"actual_projection\": [\n" + string.Join(",\n", plan.ProjectionProofs.Select(value =>
                    "    {\"proof_id\":" + J(value.GoalProof.ProofId) + ",\"resource_order\":" +
                    J(string.Join(">", value.GoalProof.RequestedOrder)) + ",\"success\":" + B(value.Success) +
                    ",\"reachable_states\":" + N(value.ReachableStates) + ",\"transitions\":" +
                    N(value.Transitions) + ",\"reverse_reachable_states\":" + N(value.ReverseReachableStates) +
                    ",\"dead_ends\":[" + string.Join(",", value.DeadEnds.Select(J)) + "],\"actions\":[" +
                    string.Join(",", value.GoalProof.Actions.Select(J)) + "]}")) + "\n  ],\n" +
                "  \"physical_movement_digest\": " + J(plan.PhysicalMovement.SemanticDigest) + ",\n" +
                "  \"readiness\": {\"logical_state\":true,\"contact_state\":true,\"global_coordinate_movement\":true,\"reservation_geometry\":\"PLANNED\",\"composed_geometry\":false,\"player\":false}\n" +
                "}\n";
        }

        public static string ValidationJson(Sv5SpaceGraphPlan plan)
        {
            Require(plan);
            return "{\n" +
                "  \"schema\": \"SV5_SPACE_VALIDATION_FIX03_V1\",\n" +
                "  \"status\": " + J(plan.Success ? "PASS" : "FAIL") + ",\n" +
                "  \"validation_layers\": {\"PLANNED_LAYOUT\":"+B(plan.Success)+",\"DIVERSITY\":"+
                B(plan.Diversity.Decisions.Where(d=>d.Selected).Select(d=>d.RequestId).Distinct().Count()==plan.Profile.Families.Count)+
                ",\"CONTACT\":"+B(plan.PhysicalMovement.Success)+",\"PHYSICAL_PRODUCT\":"+B(plan.PhysicalProduct.Success)+
                ",\"COMPOSED_GEOMETRY\":false,\"PLAYER\":false},\n"+
                "  \"diversity_digest\": "+J(plan.Diversity.Digest)+",\n"+
                "  \"plan_digest\": " + J(plan.Digest) + ",\n" +
                "  \"world\": [624,416],\n" +
                "  \"core_sites\": " + N(plan.Core.Sites.Count) + ",\n" +
                "  \"core_cells\": " + N(plan.Core.CoreCells.Count) + ",\n" +
                "  \"large_places\": " + N(plan.Places.Count(value => value.Kind == Sv5SpacePlaceKind.Large)) + ",\n" +
                "  \"ordinary_places\": " + N(plan.Places.Count(value => value.Kind == Sv5SpacePlaceKind.Ordinary)) + ",\n" +
                "  \"connections\": " + N(plan.Connections.Count) + ",\n" +
                "  \"complete_contact_pairs\": " + N(plan.ContactDecisions.Count) + ",\n" +
                "  \"contact_input_cells\": " + N(Sv5SpaceGraphValidator.AcceptedContactCells(plan.Core,
                    plan.Connections).Count) + ",\n" +
                "  \"contact_coverage_errors\": " + N(Sv5SpaceGraphValidator.FindContactCoverageErrors(
                    Sv5SpaceGraphValidator.AcceptedContactCells(plan.Core, plan.Connections),
                    plan.ContactDecisions.Select(value => value.Source)).Count) + ",\n" +
                "  \"reservation_conflicts\": " + N(Sv5SpaceGraphValidator.FindReservationConflicts(plan.Core,
                    plan.Reservations).Count) + ",\n" +
                "  \"gate_geometry_errors\": " + N(Sv5SpaceGraphValidator.FindGateErrors(plan.ContactDecisions,
                    plan.Gates).Count) + ",\n" +
                "  \"gate_state_errors\": " + N(Sv5SpaceGateGeometry.FindStateErrors(plan.Core,
                    plan.Connections, plan.Gates, plan.GateStateChecks).Count) + ",\n" +
                "  \"gate_state_checks\": " + N(plan.GateStateChecks.Count) + ",\n" +
                "  \"physical_contact_checks\": " + N(plan.PhysicalMovement.ContactChecks.Count) + ",\n" +
                "  \"physical_gate_state_checks\": " + N(plan.PhysicalMovement.GateStateChecks.Count) + ",\n" +
                "  \"physical_movement_digest\": " + J(plan.PhysicalMovement.SemanticDigest) + ",\n" +
                "  \"physical_movement_pass\": " + B(plan.PhysicalMovement.Success) + ",\n" +
                "  \"physical_product_digest\": " + J(plan.PhysicalProduct.SemanticDigest) + ",\n" +
                "  \"physical_product_pass\": " + B(plan.PhysicalProduct.Success) + ",\n" +
                "  \"physical_product_errors\": " + N(plan.PhysicalProduct.Diagnostics.Count) + ",\n" +
                "  \"conditional_gates\": " + N(plan.Gates.Count) + ",\n" +
                "  \"projection_orders\": " + N(plan.ProjectionProofs.Count) + ",\n" +
                "  \"projection_pass\": " + B(plan.ProjectionProofs.Count == 6 && plan.ProjectionProofs.All(value => value.Success)) + ",\n" +
                "  \"infill_pending_tiles\": " + N(plan.InfillPendingTileCount) + ",\n" +
                "  \"geometry_state_ready\": false,\n" +
                "  \"composed_geometry_ready\": false,\n" +
                "  \"player_verified\": false,\n" +
                "  \"focused_test_evidence\": \"EXTERNAL_FOCUSED_RESULTS_XML\",\n" +
                "  \"diagnostics\": [" + string.Join(",", plan.Diagnostics.Select(J)) + "]\n" +
                "}\n";
        }

        public static string OverviewSvg(Sv5SpaceGraphPlan plan) => Svg(plan, 0, 0, 624, 416, true, "SV5_06_FIX03 global-coordinate 624x416 space graph");
        public static string ZoomSvg(Sv5SpaceGraphPlan plan, int row, int column)
        {
            if (row < 0 || row > 3 || column < 0 || column > 3) throw new ArgumentOutOfRangeException(nameof(row));
            return Svg(Require(plan), column * 156, row * 104, 156, 104, false,
                ZoomName(row, column) + " planned layout");
        }

        public static string IndexHtml(Sv5SpaceGraphPlan plan)
        {
            Require(plan);
            var cards = new StringBuilder();
            for (var row = 0; row < 4; row++)
            for (var column = 0; column < 4; column++)
            {
                string name = ZoomName(row, column);
                cards.Append("<figure><a href=\"").Append(name).Append(".svg\"><img src=\"")
                    .Append(name).Append(".svg\" alt=\"").Append(name).Append("\"></a><figcaption>")
                    .Append(name).Append("</figcaption></figure>");
            }
            return "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><title>SV5_06_FIX03 review</title>" +
                "<style>body{font:14px system-ui;background:#101820;color:#eef4f1;margin:24px}img{width:100%;background:#18252c;border:1px solid #78909c}main{display:grid;grid-template-columns:repeat(4,1fr);gap:12px}figure{margin:0}figcaption{padding:4px} .legend{line-height:1.6}</style></head><body>" +
                "<h1>SV5_06_FIX03 global-coordinate gate plan</h1><p>Plan <code>" + H(plan.Digest) +
                "</code>; physical <code>" + H(plan.PhysicalMovement.SemanticDigest) +
                "</code>. This is planned layout evidence; composed geometry and Player verification remain false.</p>" +
                "<p class=\"legend\">Blue: preserved core · Gold: large place · Green: ordinary room · Cyan: actual core connector · Purple: optional return circuit · Red: conditional split gate · Grey: INFILL_PENDING.</p>" +
                "<p><a href=\"overview.svg\"><img src=\"overview.svg\" alt=\"overview\"></a></p>" +
                "<p><a href=\"W01_before_after.svg\">W01</a> · <a href=\"W02_before_after.svg\">W02</a> · <a href=\"FIX02_bypass_before_after.svg\">FIX02 bypass repair</a></p><main>" + cards +
                "</main></body></html>\n";
        }

        private static string BypassSvg(Sv5SpaceGraphPlan plan)
        {
            Require(plan);
            Sv5SpacePhysicalGateStateCheck closed = plan.PhysicalMovement.GateStateChecks
                .Single(value => value.Id == "EXIT_CLOSED_SEAL");
            Sv5SpacePhysicalGateStateCheck opened = plan.PhysicalMovement.GateStateChecks
                .Single(value => value.Id == "EXIT_OPEN");
            return "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 700 260\" role=\"img\">" +
                "<title>SV5_06_FIX03 FIX02 bypass repair</title><desc>Global-coordinate movement applies every closed gate simultaneously; composed geometry and Player verification remain pending.</desc>" +
                "<rect width=\"700\" height=\"260\" fill=\"#101820\"/>" +
                "<text x=\"24\" y=\"34\" fill=\"#fff\" font-size=\"20\" font-family=\"sans-serif\">FIX02 bypass: before / after</text>" +
                "<rect x=\"24\" y=\"60\" width=\"310\" height=\"150\" fill=\"#263238\" stroke=\"#ef5350\"/>" +
                "<text x=\"42\" y=\"90\" fill=\"#ef9a9a\" font-size=\"16\" font-family=\"sans-serif\">before: route-keyed check</text>" +
                "<text x=\"42\" y=\"122\" fill=\"#fff\" font-size=\"12\" font-family=\"monospace\">EXIT_CLOSED_SEAL reachable=true</text>" +
                "<text x=\"42\" y=\"148\" fill=\"#b0bec5\" font-size=\"12\" font-family=\"monospace\">225 separated contacts ignored</text>" +
                "<text x=\"42\" y=\"174\" fill=\"#b0bec5\" font-size=\"12\" font-family=\"monospace\">93 Passage-to-Passage contacts</text>" +
                "<rect x=\"366\" y=\"60\" width=\"310\" height=\"150\" fill=\"#263238\" stroke=\"#66bb6a\"/>" +
                "<text x=\"384\" y=\"90\" fill=\"#a5d6a7\" font-size=\"16\" font-family=\"sans-serif\">after: global world coordinates</text>" +
                "<text x=\"384\" y=\"122\" fill=\"#fff\" font-size=\"12\" font-family=\"monospace\">" +
                H(closed.Id + " reachable=" + closed.Reachable.ToString().ToLowerInvariant()) + "</text>" +
                "<text x=\"384\" y=\"148\" fill=\"#fff\" font-size=\"12\" font-family=\"monospace\">" +
                H(opened.Id + " reachable=" + opened.Reachable.ToString().ToLowerInvariant()) + "</text>" +
                "<text x=\"384\" y=\"174\" fill=\"#b0bec5\" font-size=\"12\" font-family=\"monospace\">all gates applied simultaneously</text>" +
                "<text x=\"24\" y=\"228\" fill=\"#90a4ae\" font-size=\"10\" font-family=\"monospace\">plan digest " +
                H(plan.Digest) + "</text><text x=\"24\" y=\"244\" fill=\"#90a4ae\" font-size=\"10\" font-family=\"monospace\">physical digest " +
                H(plan.PhysicalMovement.SemanticDigest) + "</text></svg>\n";
        }

        private static string WitnessSvg(Sv5SpaceGraphPlan plan, string checkId, string title)
        {
            Sv5SpaceGateStateCheck check = Require(plan).GateStateChecks.Single(value => value.Id == checkId);
            Sv5SpaceGate gate = plan.Gates.Single(value => value.Id == check.GateId);
            int minX = Math.Max(0, gate.BlockingFaces.SelectMany(value => new[] { value.First.X, value.Second.X })
                .Concat(new[] { gate.SideAAnchor.X, gate.SideBAnchor.X }).Min() - 5);
            int minY = Math.Max(0, gate.BlockingFaces.SelectMany(value => new[] { value.First.Y, value.Second.Y })
                .Concat(new[] { gate.SideAAnchor.Y, gate.SideBAnchor.Y }).Min() - 5);
            int width = 14, height = 14;
            var text = new StringBuilder();
            text.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 420 210\" role=\"img\">")
                .Append("<title>").Append(H(title)).Append("</title><desc>Plan ").Append(H(plan.Digest))
                .Append("; composed geometry and Player verification remain pending.</desc>")
                .Append("<rect width=\"420\" height=\"210\" fill=\"#101820\"/>")
                .Append("<text x=\"18\" y=\"28\" fill=\"#fff\" font-size=\"18\" font-family=\"sans-serif\">")
                .Append(H(title)).Append("</text><text x=\"18\" y=\"52\" fill=\"#b0bec5\" font-size=\"11\" font-family=\"monospace\">")
                .Append(H(check.Id + " | gate=" + check.GateId)).Append("</text>")
                .Append("<text x=\"18\" y=\"78\" fill=\"#80cbc4\" font-size=\"13\" font-family=\"sans-serif\">before: FIX01 blocked required port</text>")
                .Append("<text x=\"18\" y=\"104\" fill=\"#a5d6a7\" font-size=\"13\" font-family=\"sans-serif\">after: route-owned full-width face cut</text>")
                .Append("<text x=\"18\" y=\"130\" fill=\"#fff\" font-size=\"12\" font-family=\"monospace\">")
                .Append(H("state=" + check.ResourceMask + "/" + check.ForgeMade + "/" + check.SealOpen + "/" + check.BossComplete +
                    " expected=" + (check.ExpectedOpen ? "OPEN" : "SEALED"))).Append("</text>")
                .Append("<text x=\"18\" y=\"154\" fill=\"#fff\" font-size=\"12\" font-family=\"monospace\">")
                .Append(H("source=" + check.SourceAnchorReachable + " target=" + check.TargetPortReachable +
                    " cut=" + check.SealedCutVerified + " openPath=" + check.OpenPathVerified)).Append("</text>")
                .Append("<text x=\"18\" y=\"184\" fill=\"#90a4ae\" font-size=\"10\" font-family=\"monospace\">")
                .Append(H("window=" + minX + "," + minY + "," + width + "," + height +
                    " faces=" + gate.BlockingFaces.Count)).Append("</text></svg>\n");
            return text.ToString();
        }

        private static string Svg(Sv5SpaceGraphPlan plan, int viewX, int viewY, int viewWidth, int viewHeight,
            bool overview, string title)
        {
            var svg = new StringBuilder();
            svg.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"").Append(viewX).Append(' ')
                .Append(viewY).Append(' ').Append(viewWidth).Append(' ').Append(viewHeight)
                .Append("\" role=\"img\" aria-labelledby=\"title desc\"><title id=\"title\">").Append(H(title))
                .Append("</title><desc id=\"desc\">Planned layout generated from plan ").Append(H(plan.Digest))
                .Append("; background is INFILL_PENDING, not final air.</desc>")
                .Append("<rect x=\"0\" y=\"0\" width=\"624\" height=\"416\" fill=\"#202b31\"/>")
                .Append("<g transform=\"translate(0 416) scale(1 -1)\">");
            foreach (Sv5SpaceConnection value in plan.Connections)
                svg.Append("<polyline points=\"").Append(string.Join(" ", value.Centerline.Select(point =>
                    point.X.ToString(CultureInfo.InvariantCulture) + "," + point.Y.ToString(CultureInfo.InvariantCulture))))
                    .Append("\" fill=\"none\" stroke=\"").Append(value.Kind == Sv5SpaceConnectionKind.CoreProgression ?
                        "#4dd0e1" : value.Kind == Sv5SpaceConnectionKind.VillageInterior ? "#f48fb1" : "#b39ddb")
                    .Append("\" stroke-width=\"").Append(overview ? "1" : "1.3").Append("\" opacity=\"0.88\"/>");
            foreach (Sv5SpacePlace value in plan.Places)
                svg.Append("<rect x=\"").Append(value.Bounds.X).Append("\" y=\"").Append(value.Bounds.Y)
                    .Append("\" width=\"").Append(value.Bounds.Width).Append("\" height=\"").Append(value.Bounds.Height)
                    .Append("\" fill=\"").Append(value.Kind == Sv5SpacePlaceKind.Core ? "#1565c0" :
                        value.Kind == Sv5SpacePlaceKind.Large ? "#f9a825" : "#43a047")
                    .Append("\" fill-opacity=\"0.58\" stroke=\"#f5f5f5\" stroke-width=\"0.7\"/>");
            foreach (Sv5SpacePort value in plan.Ports)
                svg.Append("<circle cx=\"").Append(value.Anchor.X).Append("\" cy=\"").Append(value.Anchor.Y)
                    .Append("\" r=\"").Append(overview ? "1.2" : "1.8").Append("\" fill=\"#fff176\"/>");
            foreach (Sv5SpaceGate value in plan.Gates)
            {
                foreach (RmapSpecialWorldPoint cell in value.BlockingCells)
                    svg.Append("<rect x=\"").Append(cell.X).Append("\" y=\"").Append(cell.Y)
                        .Append("\" width=\"1\" height=\"1\" fill=\"#ef5350\"/>");
                foreach (Sv5SpaceBoundaryFace face in value.BlockingFaces)
                    svg.Append("<line x1=\"").Append(face.First.X + 0.5).Append("\" y1=\"")
                        .Append(face.First.Y + 0.5).Append("\" x2=\"").Append(face.Second.X + 0.5)
                        .Append("\" y2=\"").Append(face.Second.Y + 0.5)
                        .Append("\" stroke=\"#ef5350\" stroke-width=\"1.4\"/>");
            }
            svg.Append("</g>");
            if (overview)
            {
                for (var index = 1; index < 4; index++)
                {
                    svg.Append("<path d=\"M").Append(index * 156).Append(" 0V416 M0 ").Append(index * 104)
                        .Append("H624\" stroke=\"#90a4ae\" stroke-width=\"0.5\" opacity=\"0.65\"/>");
                }

                for (var row = 0; row < 4; row++)
                for (var column = 0; column < 4; column++)
                    svg.Append("<text x=\"").Append(column * 156 + 4).Append("\" y=\"").Append(row * 104 + 12)
                        .Append("\" fill=\"#eceff1\" font-size=\"9\" font-family=\"sans-serif\">")
                        .Append(ZoomName(row, column)).Append("</text>");
            }
            else
            {
                foreach (Sv5SpacePlace value in plan.Places.Where(value => IntersectsView(value.Bounds, viewX, viewY,
                             viewWidth, viewHeight)))
                    svg.Append("<text x=\"").Append(value.Bounds.X + 2).Append("\" y=\"")
                        .Append(416 - value.Bounds.Y - value.Bounds.Height + 9)
                        .Append("\" fill=\"#ffffff\" font-size=\"5\" font-family=\"sans-serif\">")
                        .Append(H(Short(value.Family))).Append("</text>");
            }
            svg.Append("</svg>\n");
            return svg.ToString();
        }

        private static void AppendObjects(StringBuilder output, IEnumerable<string> objects)
        { output.Append(string.Join(",\n", objects)).Append('\n'); }
        private static string Csv(string header, IEnumerable<string> rows) => header + "\n" +
            string.Join("\n", rows ?? Array.Empty<string>()) + "\n";
        private static string Row(params object[] values) => string.Join(",", (values ?? Array.Empty<object>()).Select(value =>
        {
            string text = value is bool boolean ? (boolean ? "true" : "false") : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            return text.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + text.Replace("\"", "\"\"") + "\"" : text;
        }));
        private static string Cells(IEnumerable<RmapSpecialWorldPoint> cells) => string.Join("|", (cells ??
            Array.Empty<RmapSpecialWorldPoint>()).Select(value => value.X.ToString(CultureInfo.InvariantCulture) + ":" +
            value.Y.ToString(CultureInfo.InvariantCulture)));
        private static string Point(RmapSpecialWorldPoint point) => "[" + N(point.X) + "," + N(point.Y) + "]";
        private static string J(string value) => "\"" + (value ?? string.Empty).Replace("\\", "\\\\")
            .Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\"";
        private static string N(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string B(bool value) => value ? "true" : "false";
        private static string H(string value) => (value ?? string.Empty).Replace("&", "&amp;").Replace("<", "&lt;")
            .Replace(">", "&gt;").Replace("\"", "&quot;");
        private static string Short(string value) => value != null && value.Length > 22 ? value.Substring(0, 22) : value ?? string.Empty;
        private static string ZoomName(int row, int column) => ((char)('A' + row)).ToString() + (column + 1).ToString(CultureInfo.InvariantCulture);
        private static bool IntersectsView(Sv5SpaceBounds bounds, int x, int y, int width, int height)
        {
            int worldMinY = 416 - y - height;
            int worldMaxY = 416 - y;
            return bounds.X < x + width && bounds.MaxXExclusive > x && bounds.Y < worldMaxY && bounds.MaxYExclusive > worldMinY;
        }
        private static void Write(string path, string text) => File.WriteAllText(path, text, Utf8);
        private static Sv5SpaceGraphPlan Require(Sv5SpaceGraphPlan plan) => plan ?? throw new ArgumentNullException(nameof(plan));
    }
}
