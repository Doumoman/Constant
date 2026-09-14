using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class Sv5JumpGrabExport
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        public static IReadOnlyDictionary<string, string> BuildArtifacts(Sv5JumpGrabPlan plan)
        {
            RequirePassing(plan);
            var values = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "grab_action_sequence.csv", GrabActionSequenceCsv(plan) },
                { "grab_clearance.csv", GrabClearanceCsv(plan) },
                { "grab_edges.csv", GrabEdgesCsv(plan) },
                { "jump_grab.json", JumpGrabJson(plan) },
                { "jump_grab_validation.json", ValidationJson(plan) },
                { "preview/jump_grab.svg", PreviewSvg(plan) },
                { "route_links.csv", RouteLinksCsv(plan) },
                { "support_cells.csv", SupportCellsCsv(plan) },
                { "supports.csv", SupportsCsv(plan) }
            };
            return new ReadOnlyDictionary<string, string>(values);
        }

        public static void WriteCanonical(string directory)
        {
            Write(directory, Sv5JumpGrabGeometry.CreateCanonicalLocalFixture());
        }

        public static void Write(string directory, Sv5JumpGrabPlan plan)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("An export directory is required.", nameof(directory));
            }

            foreach (KeyValuePair<string, string> artifact in BuildArtifacts(plan))
            {
                string path = Path.Combine(directory, artifact.Key.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, Normalize(artifact.Value), Utf8);
            }
        }

        public static string JumpGrabJson(Sv5JumpGrabPlan plan)
        {
            RequirePassing(plan);
            Sv5JumpGrabRouteLink grab = plan.RouteLinks.Single(value => value.Mode == Sv5JumpMode.JumpGrab);
            int gapMin = plan.RouteLinks.Min(value => value.GapAir);
            int gapMax = plan.RouteLinks.Max(value => value.GapAir);
            int riseMin = plan.RouteLinks.Min(value => value.Rise);
            int riseMax = plan.RouteLinks.Max(value => value.Rise);
            var text = new StringBuilder();
            text.Append("{\n");
            text.Append("  \"schema\": \"SV5_15_JUMP_GRAB/v1\",\n");
            text.Append("  \"fixture_digest\": ").Append(Json(plan.Digest)).Append(",\n");
            text.Append("  \"base_fixture_digest\": ").Append(Json(plan.BaseFixtureDigest)).Append(",\n");
            text.Append("  \"coordinate_scope\": \"24x32_LOCAL_INTEGER_CELLS\",\n");
            text.Append("  \"canvas\": {\"width\": ").Append(Number(plan.Width)).Append(", \"height\": ")
                .Append(Number(plan.Height)).Append(", \"scope\": \"LOCAL_JUMP_ROOM_NOT_SECTOR\"},\n");
            text.Append("  \"support_count\": ").Append(Number(plan.Supports.Count)).Append(",\n");
            text.Append("  \"support_cell_count\": ").Append(Number(plan.SupportCells.Count)).Append(",\n");
            text.Append("  \"route_link_count\": ").Append(Number(plan.RouteLinks.Count)).Append(",\n");
            text.Append("  \"jump_grab_link_count\": 1,\n");
            text.Append("  \"grab_edge_count\": 1,\n");
            text.Append("  \"grab_link\": {\"link_id\": ").Append(Json(grab.LinkId))
                .Append(", \"gap_air\": ").Append(Number(grab.GapAir))
                .Append(", \"rise\": ").Append(Number(grab.Rise)).Append("},\n");
            text.Append("  \"gap_air_range\": [").Append(Number(gapMin)).Append(", ").Append(Number(gapMax)).Append("],\n");
            text.Append("  \"rise_range\": [").Append(Number(riseMin)).Append(", ").Append(Number(riseMax)).Append("],\n");
            text.Append("  \"reverse_completion_required\": false,\n");
            text.Append("  \"ladder_logic_created\": false,\n");
            text.Append("  \"one_way_grab_allowed\": false,\n");
            text.Append("  \"validation_states\": {\"supports\": \"STATIC_SCREEN\", \"links\": \"PLANNED\", \"player_verified_count\": 0},\n");
            text.Append("  \"readiness\": {\n");
            text.Append("    \"jump_contract_ready\": true,\n");
            text.Append("    \"jump_solid_geometry_ready\": true,\n");
            text.Append("    \"jump_grab_geometry_ready\": true,\n");
            text.Append("    \"composed_geometry_ready\": false,\n");
            text.Append("    \"player_verified\": false\n");
            text.Append("  },\n");
            text.Append("  \"performance\": {\n");
            text.Append("    \"whole_world_builds_in_new_targeted_tests\": 0,\n");
            text.Append("    \"whole_world_searches_in_new_targeted_tests\": 0\n");
            text.Append("  }\n");
            text.Append("}\n");
            return text.ToString();
        }

        public static string SupportsCsv(Sv5JumpGrabPlan plan)
        {
            RequirePassing(plan);
            return Csv(
                "support_id,kind,x,y,width,height,top_y,top_x_min,top_x_max,active_route,decorative_only,required_route",
                plan.Supports.Select(value => Row(
                    value.SupportId,
                    Sv5JumpSolidGeometry.KindName(value.Kind),
                    value.X,
                    value.Y,
                    value.Width,
                    value.Height,
                    value.TopY,
                    value.TopXMin,
                    value.TopXMax,
                    value.ActiveRoute,
                    value.DecorativeOnly,
                    plan.RequiredSupportSequence.Contains(value.SupportId))));
        }

        public static string SupportCellsCsv(Sv5JumpGrabPlan plan)
        {
            RequirePassing(plan);
            return Csv(
                "support_id,x,y,kind,collision",
                plan.SupportCells.Select(value => Row(
                    value.SupportId,
                    value.Point.X,
                    value.Point.Y,
                    Sv5JumpSolidGeometry.KindName(value.Kind),
                    value.Collision)));
        }

        public static string RouteLinksCsv(Sv5JumpGrabPlan plan)
        {
            RequirePassing(plan);
            return Csv(
                "order,link_id,source_support_id,target_support_id,mode,direction,takeoff_x,takeoff_y,landing_x,landing_y,gap_air,rise,grab_edge_id,required_route",
                plan.RouteLinks.Select(value => Row(
                    value.Order,
                    value.LinkId,
                    value.Source.SupportId,
                    value.Target.SupportId,
                    Sv5JumpContract.ModeName(value.Mode),
                    Sv5JumpContract.DirectionName(value.Direction),
                    value.Takeoff.X,
                    value.Takeoff.Y,
                    value.Landing.X,
                    value.Landing.Y,
                    value.GapAir,
                    value.Rise,
                    value.GrabEdge == null ? string.Empty : value.GrabEdge.GrabEdgeId,
                    value.RequiredRoute)));
        }

        public static string GrabEdgesCsv(Sv5JumpGrabPlan plan)
        {
            RequirePassing(plan);
            return Csv(
                "grab_edge_id,link_id,target_support_id,contact_x,contact_y,face,approach_direction,hang_body_x,hang_body_y,pull_up_x,pull_up_y,exposed,hang_body_clear,pull_up_clear,safe",
                plan.GrabEdges.Select(value => Row(
                    value.GrabEdgeId,
                    Sv5JumpGrabGeometry.GrabLinkId,
                    value.SupportId,
                    value.Contact.X,
                    value.Contact.Y,
                    Sv5JumpContract.FaceName(value.Face),
                    Sv5JumpContract.DirectionName(value.ApproachDirection),
                    value.HangBody.X,
                    value.HangBody.Y,
                    value.PullUp.X,
                    value.PullUp.Y,
                    value.Exposed,
                    value.HangBodyClear,
                    value.PullUpClear,
                    value.Safe)));
        }

        public static string GrabClearanceCsv(Sv5JumpGrabPlan plan)
        {
            RequirePassing(plan);
            return Csv(
                "grab_edge_id,hang_body_x,hang_body_y,hang_head_x,hang_head_y,pull_up_foot_x,pull_up_foot_y,pull_up_head_x,pull_up_head_y,all_air",
                plan.Clearances.Select(value => Row(
                    value.GrabEdgeId,
                    value.HangBody.X,
                    value.HangBody.Y,
                    value.HangHead.X,
                    value.HangHead.Y,
                    value.PullUpFoot.X,
                    value.PullUpFoot.Y,
                    value.PullUpHead.X,
                    value.PullUpHead.Y,
                    value.AllAir)));
        }

        public static string GrabActionSequenceCsv(Sv5JumpGrabPlan plan)
        {
            RequirePassing(plan);
            return Csv(
                "order,action,player_x,player_y,contact_x,contact_y,support_id",
                plan.Actions.Select(value => Row(
                    value.Order,
                    value.Action,
                    value.Player.X,
                    value.Player.Y,
                    value.Contact.X,
                    value.Contact.Y,
                    value.SupportId)));
        }

        public static string ValidationJson(Sv5JumpGrabPlan plan)
        {
            RequirePassing(plan);
            return "{\n" +
                "  \"schema\": \"SV5_15_JUMP_GRAB_VALIDATION/v1\",\n" +
                "  \"status\": \"PASS\",\n" +
                "  \"errors\": [],\n" +
                "  \"fixture_digest\": " + Json(plan.Digest) + ",\n" +
                "  \"base_fixture_digest\": " + Json(plan.BaseFixtureDigest) + ",\n" +
                "  \"support_count\": " + Number(plan.Supports.Count) + ",\n" +
                "  \"support_cell_count\": " + Number(plan.SupportCells.Count) + ",\n" +
                "  \"route_link_count\": " + Number(plan.RouteLinks.Count) + ",\n" +
                "  \"jump_grab_link_count\": 1,\n" +
                "  \"grab_edge_count\": 1,\n" +
                "  \"grab_clearance_count\": 1,\n" +
                "  \"action_count\": 4,\n" +
                "  \"jump_contract_ready\": true,\n" +
                "  \"jump_solid_geometry_ready\": true,\n" +
                "  \"jump_grab_geometry_ready\": true,\n" +
                "  \"composed_geometry_ready\": false,\n" +
                "  \"player_verified\": false\n" +
                "}\n";
        }

        public static string PreviewSvg(Sv5JumpGrabPlan plan)
        {
            RequirePassing(plan);
            const int scale = 20;
            const int left = 40;
            const int top = 48;
            const int viewWidth = 930;
            const int viewHeight = 760;
            int canvasWidth = plan.Width * scale;
            int canvasHeight = plan.Height * scale;
            var text = new StringBuilder();
            text.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 ")
                .Append(viewWidth).Append(" ").Append(viewHeight).Append("\">");
            text.Append("<rect width=\"100%\" height=\"100%\" fill=\"#0d1820\"/>");
            text.Append("<text x=\"40\" y=\"27\" fill=\"#ffffff\" font-size=\"16\">SV5_15 24x32 local jump room | actual SOLID-face JUMP_GRAB</text>");
            text.Append("<rect x=\"").Append(left).Append("\" y=\"").Append(top)
                .Append("\" width=\"").Append(canvasWidth).Append("\" height=\"").Append(canvasHeight)
                .Append("\" fill=\"#142630\" stroke=\"#e2f3fa\" stroke-width=\"2\"/>");
            for (int x = 1; x < plan.Width; x++)
            {
                int px = left + x * scale;
                text.Append("<line x1=\"").Append(px).Append("\" y1=\"").Append(top)
                    .Append("\" x2=\"").Append(px).Append("\" y2=\"").Append(top + canvasHeight)
                    .Append("\" stroke=\"#29434e\" stroke-width=\"0.45\"/>");
            }
            for (int y = 1; y < plan.Height; y++)
            {
                int py = top + y * scale;
                text.Append("<line x1=\"").Append(left).Append("\" y1=\"").Append(py)
                    .Append("\" x2=\"").Append(left + canvasWidth).Append("\" y2=\"").Append(py)
                    .Append("\" stroke=\"#29434e\" stroke-width=\"0.45\"/>");
            }
            foreach (Sv5JumpSolidCell cell in plan.SupportCells)
            {
                int x = CellX(cell.Point.X, left, scale);
                int y = CellY(cell.Point.Y, plan.Height, top, scale);
                string fill = cell.Kind == Sv5JumpSupportKind.Solid ? "#d95f59" : "#398fc6";
                text.Append("<rect x=\"").Append(x).Append("\" y=\"").Append(y)
                    .Append("\" width=\"").Append(scale).Append("\" height=\"").Append(scale)
                    .Append("\" fill=\"").Append(fill).Append("\" stroke=\"#ecf7fb\" stroke-width=\"1\"/>");
                if (cell.Kind == Sv5JumpSupportKind.OneWay)
                {
                    text.Append("<line x1=\"").Append(x).Append("\" y1=\"").Append(y)
                        .Append("\" x2=\"").Append(x + scale).Append("\" y2=\"").Append(y)
                        .Append("\" stroke=\"#9ee0ff\" stroke-width=\"4\"/>");
                }
            }
            foreach (Sv5JumpGrabRouteLink link in plan.RouteLinks)
            {
                int x1 = CellX(link.Takeoff.X, left, scale) + scale / 2;
                int y1 = top + (plan.Height - link.Takeoff.Y) * scale;
                int x2 = CellX(link.Landing.X, left, scale) + scale / 2;
                int y2 = top + (plan.Height - link.Landing.Y) * scale;
                string color = link.Mode == Sv5JumpMode.JumpGrab ? "#ffbf3f" : "#5fe0a3";
                string dash = link.Mode == Sv5JumpMode.JumpGrab ? "3 3" : "7 4";
                text.Append("<line x1=\"").Append(x1).Append("\" y1=\"").Append(y1)
                    .Append("\" x2=\"").Append(x2).Append("\" y2=\"").Append(y2)
                    .Append("\" stroke=\"").Append(color).Append("\" stroke-width=\"3\" stroke-dasharray=\"")
                    .Append(dash).Append("\"/>");
                int labelX = link.Mode == Sv5JumpMode.JumpGrab ? x1 + 4 : (x1 + x2) / 2;
                int labelY = link.Mode == Sv5JumpMode.JumpGrab ? (y1 + y2) / 2 - 15 : (y1 + y2) / 2 - 5;
                text.Append("<text x=\"").Append(labelX).Append("\" y=\"").Append(labelY)
                    .Append("\" fill=\"").Append(color).Append("\" font-size=\"9\">")
                    .Append(link.Mode == Sv5JumpMode.JumpGrab ? "JUMP_GRAB" : (link.Order + 1).ToString(CultureInfo.InvariantCulture))
                    .Append("</text>");
            }
            Sv5JumpGrabEdge edge = plan.GrabEdges.Single();
            Sv5JumpGrabClearance clearance = plan.Clearances.Single();
            int contactRight = CellX(edge.Contact.X, left, scale) + scale;
            int contactTop = CellY(edge.Contact.Y, plan.Height, top, scale);
            text.Append("<line x1=\"").Append(contactRight).Append("\" y1=\"").Append(contactTop)
                .Append("\" x2=\"").Append(contactRight).Append("\" y2=\"").Append(contactTop + scale)
                .Append("\" stroke=\"#fff176\" stroke-width=\"5\"/>");
            AppendPoint(text, plan, edge.Contact, "CONTACT", "#fff176", left, top, scale, false, -62, 15);
            AppendPoint(text, plan, clearance.HangBody, "HANG body", "#ff8fa3", left, top, scale, true, 70, 17);
            AppendPoint(text, plan, clearance.HangHead, "HANG head", "#ffb3c1", left, top, scale, true, 10, 3);
            AppendPoint(text, plan, clearance.PullUpFoot, "PULL_UP foot / LAND", "#b8f36b", left, top, scale, true, -95, -5);
            AppendPoint(text, plan, clearance.PullUpHead, "PULL_UP head", "#d7ff9a", left, top, scale, true, -79, -8);
            AppendPoint(text, plan, plan.Link(Sv5JumpGrabGeometry.GrabLinkId).Takeoff, "TAKEOFF", "#ffffff", left, top, scale, true, 10, 16);
            AppendSocket(text, plan, plan.EntrySocket, "ENTRY", "#ffd166", left, top, scale);
            AppendSocket(text, plan, plan.ExitSocket, "EXIT", "#ff70b7", left, top, scale);

            int legendX = left + canvasWidth + 28;
            text.Append("<text x=\"").Append(legendX).Append("\" y=\"70\" fill=\"white\" font-size=\"15\">LEGEND</text>");
            text.Append("<rect x=\"").Append(legendX).Append("\" y=\"88\" width=\"18\" height=\"18\" fill=\"#d95f59\"/>");
            text.Append("<text x=\"").Append(legendX + 28).Append("\" y=\"102\" fill=\"white\" font-size=\"12\">SOLID cell</text>");
            text.Append("<rect x=\"").Append(legendX).Append("\" y=\"118\" width=\"18\" height=\"18\" fill=\"#398fc6\"/>");
            text.Append("<text x=\"").Append(legendX + 28).Append("\" y=\"132\" fill=\"white\" font-size=\"12\">ONE_WAY cell</text>");
            text.Append("<line x1=\"").Append(legendX).Append("\" y1=\"160\" x2=\"").Append(legendX + 20)
                .Append("\" y2=\"160\" stroke=\"#ffbf3f\" stroke-width=\"4\"/>");
            text.Append("<text x=\"").Append(legendX + 28).Append("\" y=\"164\" fill=\"#ffbf3f\" font-size=\"12\">JUMP_GRAB</text>");
            text.Append("<text x=\"").Append(legendX).Append("\" y=\"202\" fill=\"#fff176\" font-size=\"12\">SOLID face only</text>");
            text.Append("<text x=\"").Append(legendX).Append("\" y=\"226\" fill=\"#9ee0ff\" font-size=\"12\">ONE_WAY is not grabbable</text>");
            text.Append("<text x=\"").Append(legendX).Append("\" y=\"260\" fill=\"#ffffff\" font-size=\"11\">TAKEOFF -&gt; CONTACT / HANG</text>");
            text.Append("<text x=\"").Append(legendX).Append("\" y=\"280\" fill=\"#ffffff\" font-size=\"11\">-&gt; PULL_UP -&gt; LAND</text>");
            text.Append("<text x=\"").Append(legendX).Append("\" y=\"316\" fill=\"#d8edf7\" font-size=\"11\">PLAYER verification deferred</text>");
            text.Append("<text x=\"").Append(legendX).Append("\" y=\"340\" fill=\"#d8edf7\" font-size=\"11\">local room; not a Sector</text>");
            text.Append("<text x=\"40\" y=\"728\" fill=\"#d8edf7\" font-size=\"11\">Actual cells: contact=(15,6), hang=(16,6)/(16,7), pull-up=(15,7)/(15,8).</text>");
            text.Append("</svg>\n");
            return text.ToString();
        }

        private static void AppendPoint(
            StringBuilder text,
            Sv5JumpGrabPlan plan,
            Sv5JumpPoint point,
            string label,
            string color,
            int left,
            int top,
            int scale,
            bool playerPosition,
            int labelDx,
            int labelDy)
        {
            int x = CellX(point.X, left, scale) + scale / 2;
            int y = CellY(point.Y, plan.Height, top, scale) + scale / 2;
            text.Append("<circle cx=\"").Append(x).Append("\" cy=\"").Append(y)
                .Append("\" r=\"").Append(playerPosition ? 6 : 5).Append("\" fill=\"")
                .Append(playerPosition ? "none" : color).Append("\" stroke=\"").Append(color)
                .Append("\" stroke-width=\"2\"/>");
            text.Append("<text x=\"").Append(x + labelDx).Append("\" y=\"").Append(y + labelDy)
                .Append("\" fill=\"").Append(color).Append("\" font-size=\"9\">").Append(label).Append("</text>");
        }

        private static void AppendSocket(
            StringBuilder text,
            Sv5JumpGrabPlan plan,
            Sv5JumpSolidSocket socket,
            string label,
            string color,
            int left,
            int top,
            int scale)
        {
            int x = CellX(socket.Foot.X, left, scale) + scale / 2;
            int y = top + (plan.Height - socket.Foot.Y) * scale;
            text.Append("<circle cx=\"").Append(x).Append("\" cy=\"").Append(y)
                .Append("\" r=\"8\" fill=\"none\" stroke=\"").Append(color).Append("\" stroke-width=\"3\"/>");
            text.Append("<text x=\"").Append(x + 10).Append("\" y=\"").Append(y - 8)
                .Append("\" fill=\"").Append(color).Append("\" font-size=\"11\">").Append(label).Append("</text>");
        }

        private static int CellX(int x, int left, int scale)
        {
            return left + x * scale;
        }

        private static int CellY(int y, int height, int top, int scale)
        {
            return top + (height - 1 - y) * scale;
        }

        private static string Csv(string header, IEnumerable<string> rows)
        {
            return header + "\n" + string.Join("\n", rows) + "\n";
        }

        private static string Row(params object[] values)
        {
            return string.Join(",", values.Select(value => Quote(value == null
                ? string.Empty
                : value is bool && (bool)value
                    ? "true"
                    : value is bool
                        ? "false"
                        : Convert.ToString(value, CultureInfo.InvariantCulture))));
        }

        private static string Quote(string value)
        {
            return value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0
                ? value
                : "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string Json(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        private static string Number(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string Normalize(string value)
        {
            return value.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        private static void RequirePassing(Sv5JumpGrabPlan plan)
        {
            if (plan == null || !plan.JumpGrabGeometryReady)
            {
                throw new ArgumentException("A passing local jump-grab plan is required.", nameof(plan));
            }
        }
    }
}
