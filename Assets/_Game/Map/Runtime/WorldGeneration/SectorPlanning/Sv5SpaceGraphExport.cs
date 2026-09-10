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
            Write(Path.Combine(directory, "reservation_cells.csv"), ReservationCellsCsv(plan));
            Write(Path.Combine(directory, "state_proofs.json"), StateProofsJson(plan));
            Write(Path.Combine(directory, "contact_checks.csv"), ContactChecksCsv(plan));
            Write(Path.Combine(directory, "obligations.csv"), ObligationsCsv(plan));
            Write(Path.Combine(directory, "validation.json"), ValidationJson(plan));
            Write(Path.Combine(preview, "overview.svg"), OverviewSvg(plan));
            for (var row = 0; row < 4; row++)
            for (var column = 0; column < 4; column++)
                Write(Path.Combine(preview, ZoomName(row, column) + ".svg"), ZoomSvg(plan, row, column));
            Write(Path.Combine(preview, "index.html"), IndexHtml(plan));
        }

        public static string SpaceGraphJson(Sv5SpaceGraphPlan plan)
        {
            Require(plan);
            var text = new StringBuilder();
            text.Append("{\n  \"schema\": \"SV5_SPACE_GRAPH_V1\",\n")
                .Append("  \"world\": {\"width\":624,\"height\":416,\"origin\":\"BOTTOM_LEFT\",\"bounds\":\"HALF_OPEN\",\"micro_chunk\":[12,8],\"pattern\":[4,4]},\n")
                .Append("  \"seed\": ").Append(plan.Seed.ToString(CultureInfo.InvariantCulture)).Append(",\n")
                .Append("  \"profile\": {\"id\":").Append(J(plan.Profile.Id)).Append(",\"version\":")
                .Append(J(plan.Profile.Version)).Append(",\"digest\":").Append(J(plan.Profile.Digest)).Append("},\n")
                .Append("  \"source\": {\"world_digest\":").Append(J(plan.Core.RouteSource.Definition.Digest))
                .Append(",\"core_digest\":").Append(J(plan.Core.Digest)).Append(",\"graph_digest\":")
                .Append(J(plan.Core.RouteSource.Graph.Digest)).Append(",\"core_sites\":8,\"core_cells\":2432},\n")
                .Append("  \"plan_digest\": ").Append(J(plan.Digest)).Append(",\n")
                .Append("  \"readiness\": {\"planned_layout\":true,\"logical_state\":true,\"contact_state\":true,\"composed_geometry\":false,\"player\":false},\n")
                .Append("  \"infill\": {\"owner\":\"SV5_08_INFILL\",\"state\":\"INFILL_PENDING\",\"tile_count\":")
                .Append(plan.InfillPendingTileCount.ToString(CultureInfo.InvariantCulture)).Append("},\n")
                .Append("  \"places\": [\n");
            AppendObjects(text, plan.Places.Select(value => "    {\"id\":" + J(value.Id) + ",\"family\":" +
                J(value.Family) + ",\"kind\":" + J(value.Kind.ToString()) + ",\"bounds\":{" +
                "\"x\":" + N(value.Bounds.X) + ",\"y\":" + N(value.Bounds.Y) + ",\"width\":" +
                N(value.Bounds.Width) + ",\"height\":" + N(value.Bounds.Height) + "},\"core_site_id\":" +
                J(value.CoreSiteId) + ",\"future_owner\":" + J(value.FutureOwner) + ",\"readiness\":" +
                J(value.Readiness) + "}"));
            text.Append("  ],\n  \"ports\": [\n");
            AppendObjects(text, plan.Ports.Select(value => "    {\"id\":" + J(value.Id) + ",\"place_id\":" +
                J(value.PlaceId) + ",\"anchor\":" + Point(value.Anchor) + ",\"direction\":" +
                J(value.Direction.ToString()) + ",\"flow\":" + J(value.Flow) + ",\"condition\":" +
                J(value.Condition) + ",\"source_access_id\":" + J(value.SourceAccessId) + ",\"status\":" +
                J(value.Status) + "}"));
            text.Append("  ],\n  \"connections\": [\n");
            AppendObjects(text, plan.Connections.Select(value => "    {\"id\":" + J(value.Id) + ",\"kind\":" +
                J(value.Kind.ToString()) + ",\"from_port\":" + J(value.FromPortId) + ",\"to_port\":" +
                J(value.ToPortId) + ",\"direction\":" + J(value.Direction.ToString()) + ",\"flow\":" +
                J(value.Flow) + ",\"condition\":" + J(value.Condition) + ",\"source_graph_edge_id\":" +
                J(value.SourceGraphEdgeId) + ",\"selection_state\":" + J(value.SelectionState) +
                ",\"centerline\":[" + string.Join(",", value.Centerline.Select(Point)) + "]}"));
            text.Append("  ],\n  \"gates\": [\n");
            AppendObjects(text, plan.Gates.Select(value => "    {\"id\":" + J(value.Id) + ",\"contact_id\":" +
                J(value.ContactId) + ",\"world\":" + Point(value.World) + ",\"predicate\":" +
                J(value.Predicate) + ",\"crossing\":" + J(value.Crossing.ToString()) +
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
            "connection_id,kind,from_port_id,to_port_id,from_place_id,to_place_id,direction,flow,condition,source_graph_edge_id,selection_state,centerline_count,centerline,envelope_count,envelope,plan_digest",
            Require(plan).Connections.Select(value => Row(value.Id, value.Kind, value.FromPortId, value.ToPortId,
                value.FromPlaceId, value.ToPlaceId, value.Direction, value.Flow, value.Condition,
                value.SourceGraphEdgeId, value.SelectionState, value.Centerline.Count, Cells(value.Centerline),
                value.Envelope.Count, Cells(value.Envelope), plan.Digest)));

        public static string ReservationCellsCsv(Sv5SpaceGraphPlan plan) => Csv(
            "world_x,world_y,reservation_kind,owner_id,semantics,micro_chunk_x,micro_chunk_y,pattern_x,pattern_y,is_final_tile,plan_digest",
            Require(plan).Reservations.Select(value => Row(value.World.X, value.World.Y, value.Kind, value.OwnerId,
                value.Semantics, value.MicroChunkX, value.MicroChunkY, value.PatternX, value.PatternY, false,
                plan.Digest)));

        public static string ContactChecksCsv(Sv5SpaceGraphPlan plan) => Csv(
            "contact_id,kind,first_x,first_y,second_x,second_y,direction,route_a,route_b,first_kinds,second_kinds,split_node_id,crossing,predicate,logical_state_transition_checked,geometry_state,player_state,detail,plan_digest",
            Require(plan).ContactDecisions.Select(value => Row(value.Source.Id, value.Source.Kind,
                value.Source.FirstWorld.X, value.Source.FirstWorld.Y, value.Source.SecondWorld.X,
                value.Source.SecondWorld.Y, value.Source.Direction, value.Source.RouteA, value.Source.RouteB,
                value.Source.FirstKinds, value.Source.SecondKinds, value.SplitNodeId, value.Crossing, value.Predicate,
                value.LogicalStateTransitionChecked, value.GeometryState, value.PlayerState, value.Detail, plan.Digest)));

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
            return Csv("obligation_id,owner_task,requirement,readiness,verification_layer", rows.Select(Row));
        }

        public static string StateProofsJson(Sv5SpaceGraphPlan plan)
        {
            Require(plan);
            return "{\n" +
                "  \"schema\": \"SV5_SPACE_STATE_PROOFS_V1\",\n" +
                "  \"plan_digest\": " + J(plan.Digest) + ",\n" +
                "  \"baseline\": {\"source\":\"RMAP13\",\"edge_count\":" + N(plan.Core.RouteSource.Graph.Edges.Count) +
                    ",\"proof_count\":" + N(plan.Core.RouteSource.Graph.Proofs.Count) + ",\"pass\":" +
                    B(plan.Core.RouteSource.Graph.Success) + "},\n" +
                "  \"candidate_set\": {\"source\":\"SV5_05_FIX01_API\",\"status\":\"PRESERVED_SEPARATE\"},\n" +
                "  \"actual_projection\": [\n" + string.Join(",\n", plan.ProjectionProofs.Select(value =>
                    "    {\"proof_id\":" + J(value.GoalProof.ProofId) + ",\"resource_order\":" +
                    J(string.Join(">", value.GoalProof.RequestedOrder)) + ",\"success\":" + B(value.Success) +
                    ",\"reachable_states\":" + N(value.ReachableStates) + ",\"transitions\":" +
                    N(value.Transitions) + ",\"reverse_reachable_states\":" + N(value.ReverseReachableStates) +
                    ",\"dead_ends\":[" + string.Join(",", value.DeadEnds.Select(J)) + "],\"actions\":[" +
                    string.Join(",", value.GoalProof.Actions.Select(J)) + "]}")) + "\n  ],\n" +
                "  \"readiness\": {\"logical_state\":true,\"contact_state\":true,\"reservation_geometry\":\"PLANNED\",\"composed_geometry\":false,\"player\":false}\n" +
                "}\n";
        }

        public static string ValidationJson(Sv5SpaceGraphPlan plan)
        {
            Require(plan);
            return "{\n" +
                "  \"schema\": \"SV5_SPACE_VALIDATION_V1\",\n" +
                "  \"status\": " + J(plan.Success ? "PASS" : "FAIL") + ",\n" +
                "  \"plan_digest\": " + J(plan.Digest) + ",\n" +
                "  \"world\": [624,416],\n" +
                "  \"core_sites\": " + N(plan.Core.Sites.Count) + ",\n" +
                "  \"core_cells\": " + N(plan.Core.CoreCells.Count) + ",\n" +
                "  \"large_places\": " + N(plan.Places.Count(value => value.Kind == Sv5SpacePlaceKind.Large)) + ",\n" +
                "  \"ordinary_places\": " + N(plan.Places.Count(value => value.Kind == Sv5SpacePlaceKind.Ordinary)) + ",\n" +
                "  \"connections\": " + N(plan.Connections.Count) + ",\n" +
                "  \"complete_contact_pairs\": " + N(plan.ContactDecisions.Count) + ",\n" +
                "  \"conditional_gates\": " + N(plan.Gates.Count) + ",\n" +
                "  \"projection_orders\": " + N(plan.ProjectionProofs.Count) + ",\n" +
                "  \"projection_pass\": " + B(plan.ProjectionProofs.Count == 6 && plan.ProjectionProofs.All(value => value.Success)) + ",\n" +
                "  \"infill_pending_tiles\": " + N(plan.InfillPendingTileCount) + ",\n" +
                "  \"geometry_state_ready\": false,\n" +
                "  \"player_verified\": false,\n" +
                "  \"focused_test_evidence\": \"EXTERNAL_FOCUSED_RESULTS_XML\",\n" +
                "  \"diagnostics\": [" + string.Join(",", plan.Diagnostics.Select(J)) + "]\n" +
                "}\n";
        }

        public static string OverviewSvg(Sv5SpaceGraphPlan plan) => Svg(plan, 0, 0, 624, 416, true, "SV5_06 planned 624x416 space graph");
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
            return "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><title>SV5_06 review</title>" +
                "<style>body{font:14px system-ui;background:#101820;color:#eef4f1;margin:24px}img{width:100%;background:#18252c;border:1px solid #78909c}main{display:grid;grid-template-columns:repeat(4,1fr);gap:12px}figure{margin:0}figcaption{padding:4px} .legend{line-height:1.6}</style></head><body>" +
                "<h1>SV5_06 planned space graph</h1><p>Digest <code>" + H(plan.Digest) +
                "</code>. This is planned layout evidence; composed geometry and Player verification remain false.</p>" +
                "<p class=\"legend\">Blue: preserved core · Gold: large place · Green: ordinary room · Cyan: actual core connector · Purple: optional return circuit · Red: conditional split gate · Grey: INFILL_PENDING.</p>" +
                "<p><a href=\"overview.svg\"><img src=\"overview.svg\" alt=\"overview\"></a></p><main>" + cards +
                "</main></body></html>\n";
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
                svg.Append("<circle cx=\"").Append(value.World.X).Append("\" cy=\"").Append(value.World.Y)
                    .Append("\" r=\"").Append(overview ? "1.7" : "2.3")
                    .Append("\" fill=\"none\" stroke=\"#ef5350\" stroke-width=\"1\"/>");
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
