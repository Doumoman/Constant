using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class Sv5JumpSolidExport
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        public static IReadOnlyDictionary<string, string> BuildArtifacts(Sv5JumpSolidPlan plan)
        {
            RequirePassing(plan);
            var values = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "jump_solid.json", JumpSolidJson(plan) },
                { "jump_solid_validation.json", ValidationJson(plan) },
                { "preview/jump_solid.svg", PreviewSvg(plan) },
                { "route_links.csv", RouteLinksCsv(plan) },
                { "route_support_sequence.csv", RouteSupportSequenceCsv(plan) },
                { "solid_use.csv", SolidUseCsv(plan) },
                { "support_cells.csv", SupportCellsCsv(plan) },
                { "support_clearance.csv", SupportClearanceCsv(plan) },
                { "supports.csv", SupportsCsv(plan) }
            };
            return new ReadOnlyDictionary<string, string>(values);
        }

        public static void WriteCanonical(string directory)
        {
            Write(directory, Sv5JumpSolidGeometry.CreateCanonicalLocalFixture());
        }

        public static void Write(string directory, Sv5JumpSolidPlan plan)
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

        public static string JumpSolidJson(Sv5JumpSolidPlan plan)
        {
            RequirePassing(plan);
            int solidCount = plan.Supports.Count(value => value.Kind == Sv5JumpSupportKind.Solid);
            int oneWayCount = plan.Supports.Count(value => value.Kind == Sv5JumpSupportKind.OneWay);
            int gapMin = plan.RouteLinks.Min(value => value.GapAir);
            int gapMax = plan.RouteLinks.Max(value => value.GapAir);
            int riseMin = plan.RouteLinks.Min(value => value.Rise);
            int riseMax = plan.RouteLinks.Max(value => value.Rise);
            var text = new StringBuilder();
            text.Append("{\n");
            text.Append("  \"schema\": \"SV5_14_JUMP_SOLID/v1\",\n");
            text.Append("  \"fixture_digest\": ").Append(Json(plan.Digest)).Append(",\n");
            text.Append("  \"coordinate_scope\": \"LOCAL_JUMP_ROOM_NOT_WORLD_PLACEMENT\",\n");
            text.Append("  \"canvas\": {\"width\": ").Append(Number(plan.Width)).Append(", \"height\": ")
                .Append(Number(plan.Height)).Append("},\n");
            text.Append("  \"illustrative_world_origin\": {\"x\": 360, \"y\": 224, \"authoritative\": false},\n");
            text.Append("  \"entry_support_id\": ").Append(Json(plan.RequiredSupportSequence.First())).Append(",\n");
            text.Append("  \"exit_support_id\": ").Append(Json(plan.RequiredSupportSequence.Last())).Append(",\n");
            text.Append("  \"entry_socket\": ").Append(SocketJson(plan.EntrySocket)).Append(",\n");
            text.Append("  \"exit_socket\": ").Append(SocketJson(plan.ExitSocket)).Append(",\n");
            text.Append("  \"support_count\": ").Append(Number(plan.Supports.Count)).Append(",\n");
            text.Append("  \"support_cell_count\": ").Append(Number(plan.SupportCells.Count)).Append(",\n");
            text.Append("  \"solid_support_count\": ").Append(Number(solidCount)).Append(",\n");
            text.Append("  \"one_way_support_count\": ").Append(Number(oneWayCount)).Append(",\n");
            text.Append("  \"required_route_support_count\": ").Append(Number(plan.RequiredSupportSequence.Count)).Append(",\n");
            text.Append("  \"required_route_link_count\": ").Append(Number(plan.RouteLinks.Count)).Append(",\n");
            text.Append("  \"route_vertical_span\": ").Append(Number(plan.RouteVerticalSpan)).Append(",\n");
            text.Append("  \"gap_air_range\": [").Append(Number(gapMin)).Append(", ").Append(Number(gapMax)).Append("],\n");
            text.Append("  \"rise_range\": [").Append(Number(riseMin)).Append(", ").Append(Number(riseMax)).Append("],\n");
            text.Append("  \"grab_edges_created\": 0,\n");
            text.Append("  \"reverse_completion_required\": false,\n");
            text.Append("  \"validation_states\": {\"supports\": \"STATIC_SCREEN\", \"links\": \"PLANNED\", \"player_verified_count\": 0},\n");
            text.Append("  \"readiness\": {\n");
            text.Append("    \"jump_contract_ready\": true,\n");
            text.Append("    \"jump_solid_geometry_ready\": true,\n");
            text.Append("    \"jump_grab_geometry_ready\": false,\n");
            text.Append("    \"composed_geometry_ready\": false,\n");
            text.Append("    \"player_verified\": false\n");
            text.Append("  },\n");
            text.Append("  \"performance\": {\n");
            text.Append("    \"fixture_scope\": \"24x32_LOCAL_INTEGER_CELLS\",\n");
            text.Append("    \"whole_world_builds_in_new_targeted_tests\": 0,\n");
            text.Append("    \"whole_world_searches_in_new_targeted_tests\": 0\n");
            text.Append("  }\n");
            text.Append("}\n");
            return text.ToString();
        }

        public static string SupportsCsv(Sv5JumpSolidPlan plan)
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
                    plan.RequiredRouteContains(value.SupportId))));
        }

        public static string SupportCellsCsv(Sv5JumpSolidPlan plan)
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

        public static string RouteSupportSequenceCsv(Sv5JumpSolidPlan plan)
        {
            RequirePassing(plan);
            return Csv(
                "order,support_id,entry,exit",
                plan.RequiredSupportSequence.Select((value, index) => Row(
                    index,
                    value,
                    index == 0,
                    index == plan.RequiredSupportSequence.Count - 1)));
        }

        public static string RouteLinksCsv(Sv5JumpSolidPlan plan)
        {
            RequirePassing(plan);
            return Csv(
                "order,link_id,source_support_id,target_support_id,mode,direction,takeoff_x,takeoff_y,landing_x,landing_y,gap_air,rise,required_route",
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
                    value.RequiredRoute)));
        }

        public static string SupportClearanceCsv(Sv5JumpSolidPlan plan)
        {
            RequirePassing(plan);
            return Csv(
                "support_id,foot_x,foot_y,body_x,body_y,head_x,head_y,clear",
                plan.Clearances.Select(value => Row(
                    value.SupportId,
                    value.Foot.X,
                    value.Foot.Y,
                    value.Body.X,
                    value.Body.Y,
                    value.Head.X,
                    value.Head.Y,
                    value.Clear)));
        }

        public static string SolidUseCsv(Sv5JumpSolidPlan plan)
        {
            RequirePassing(plan);
            return Csv(
                "support_id,as_takeoff_count,as_landing_count,counted_active_solid",
                plan.SolidUses.Select(value => Row(
                    value.SupportId,
                    value.AsTakeoffCount,
                    value.AsLandingCount,
                    value.CountedActiveSolid)));
        }

        public static string ValidationJson(Sv5JumpSolidPlan plan)
        {
            RequirePassing(plan);
            return "{\n" +
                "  \"schema\": \"SV5_14_JUMP_SOLID_VALIDATION/v1\",\n" +
                "  \"status\": \"PASS\",\n" +
                "  \"errors\": [],\n" +
                "  \"fixture_digest\": " + Json(plan.Digest) + ",\n" +
                "  \"support_count\": " + Number(plan.Supports.Count) + ",\n" +
                "  \"support_cell_count\": " + Number(plan.SupportCells.Count) + ",\n" +
                "  \"required_route_support_count\": " + Number(plan.RequiredSupportSequence.Count) + ",\n" +
                "  \"required_route_link_count\": " + Number(plan.RouteLinks.Count) + ",\n" +
                "  \"route_vertical_span\": " + Number(plan.RouteVerticalSpan) + ",\n" +
                "  \"clearance_count\": " + Number(plan.Clearances.Count) + ",\n" +
                "  \"grab_edge_count\": 0,\n" +
                "  \"player_verified_count\": 0,\n" +
                "  \"jump_contract_ready\": true,\n" +
                "  \"jump_solid_geometry_ready\": true,\n" +
                "  \"jump_grab_geometry_ready\": false,\n" +
                "  \"composed_geometry_ready\": false,\n" +
                "  \"player_verified\": false\n" +
                "}\n";
        }

        public static string PreviewSvg(Sv5JumpSolidPlan plan)
        {
            RequirePassing(plan);
            const int scale = 22;
            const int left = 40;
            const int top = 42;
            const int viewWidth = 820;
            const int viewHeight = 790;
            int canvasWidth = plan.Width * scale;
            int canvasHeight = plan.Height * scale;
            var text = new StringBuilder();
            text.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 ")
                .Append(viewWidth).Append(" ").Append(viewHeight).Append("\">");
            text.Append("<rect width=\"100%\" height=\"100%\" fill=\"#101820\"/>");
            text.Append("<text x=\"40\" y=\"24\" fill=\"white\" font-size=\"15\">SV5_14 24x32 local jump room | mixed SOLID / ONE_WAY</text>");
            text.Append("<rect x=\"").Append(left).Append("\" y=\"").Append(top)
                .Append("\" width=\"").Append(canvasWidth).Append("\" height=\"").Append(canvasHeight)
                .Append("\" fill=\"#162731\" stroke=\"#d8edf7\" stroke-width=\"2\"/>");

            for (int x = 1; x < plan.Width; x++)
            {
                int px = left + x * scale;
                text.Append("<line x1=\"").Append(px).Append("\" y1=\"").Append(top)
                    .Append("\" x2=\"").Append(px).Append("\" y2=\"").Append(top + canvasHeight)
                    .Append("\" stroke=\"#29434e\" stroke-width=\"0.5\"/>");
            }

            for (int y = 1; y < plan.Height; y++)
            {
                int py = top + y * scale;
                text.Append("<line x1=\"").Append(left).Append("\" y1=\"").Append(py)
                    .Append("\" x2=\"").Append(left + canvasWidth).Append("\" y2=\"").Append(py)
                    .Append("\" stroke=\"#29434e\" stroke-width=\"0.5\"/>");
            }

            foreach (Sv5JumpSolidCell cell in plan.SupportCells)
            {
                int x = left + cell.Point.X * scale;
                int y = top + (plan.Height - 1 - cell.Point.Y) * scale;
                string fill = cell.Kind == Sv5JumpSupportKind.Solid ? "#d85b5b" : "#4098d7";
                text.Append("<rect x=\"").Append(x).Append("\" y=\"").Append(y)
                    .Append("\" width=\"").Append(scale).Append("\" height=\"").Append(scale)
                    .Append("\" fill=\"").Append(fill).Append("\" stroke=\"#f3f7fa\" stroke-width=\"1\"/>");
                if (cell.Kind == Sv5JumpSupportKind.OneWay)
                {
                    text.Append("<line x1=\"").Append(x).Append("\" y1=\"").Append(y)
                        .Append("\" x2=\"").Append(x + scale).Append("\" y2=\"").Append(y)
                        .Append("\" stroke=\"#9bd7ff\" stroke-width=\"4\"/>");
                }
            }

            foreach (Sv5JumpSolidRouteLink link in plan.RouteLinks)
            {
                int x1 = left + link.Takeoff.X * scale + scale / 2;
                int y1 = top + (plan.Height - link.Takeoff.Y) * scale;
                int x2 = left + link.Landing.X * scale + scale / 2;
                int y2 = top + (plan.Height - link.Landing.Y) * scale;
                text.Append("<line x1=\"").Append(x1).Append("\" y1=\"").Append(y1)
                    .Append("\" x2=\"").Append(x2).Append("\" y2=\"").Append(y2)
                    .Append("\" stroke=\"#69e5ae\" stroke-width=\"3\" stroke-dasharray=\"7 4\"/>");
                text.Append("<circle cx=\"").Append(x1).Append("\" cy=\"").Append(y1)
                    .Append("\" r=\"4\" fill=\"#ffffff\"/><circle cx=\"").Append(x2).Append("\" cy=\"")
                    .Append(y2).Append("\" r=\"4\" fill=\"#00ff9d\"/>");
                text.Append("<text x=\"").Append((x1 + x2) / 2).Append("\" y=\"").Append((y1 + y2) / 2 - 5)
                    .Append("\" fill=\"#b7ffe1\" font-size=\"9\">").Append(link.Order + 1).Append("</text>");
            }

            AppendSocket(text, plan, plan.EntrySocket, "ENTRY", "#ffd166", left, top, scale);
            AppendSocket(text, plan, plan.ExitSocket, "EXIT", "#ff8fab", left, top, scale);

            int legendX = left + canvasWidth + 24;
            text.Append("<text x=\"").Append(legendX).Append("\" y=\"72\" fill=\"white\" font-size=\"14\">LEGEND</text>");
            text.Append("<rect x=\"").Append(legendX).Append("\" y=\"88\" width=\"18\" height=\"18\" fill=\"#d85b5b\"/>");
            text.Append("<text x=\"").Append(legendX + 28).Append("\" y=\"102\" fill=\"white\" font-size=\"12\">SOLID cell</text>");
            text.Append("<rect x=\"").Append(legendX).Append("\" y=\"118\" width=\"18\" height=\"18\" fill=\"#4098d7\"/>");
            text.Append("<text x=\"").Append(legendX + 28).Append("\" y=\"132\" fill=\"white\" font-size=\"12\">ONE_WAY cell</text>");
            text.Append("<text x=\"").Append(legendX).Append("\" y=\"164\" fill=\"#ffffff\" font-size=\"11\">takeoff = white</text>");
            text.Append("<text x=\"").Append(legendX).Append("\" y=\"184\" fill=\"#00ff9d\" font-size=\"11\">landing = green</text>");
            text.Append("<text x=\"").Append(legendX).Append("\" y=\"220\" fill=\"#ffd166\" font-size=\"11\">Grab deferred to SV5_15</text>");
            text.Append("<text x=\"").Append(legendX).Append("\" y=\"248\" fill=\"#b7ffe1\" font-size=\"11\">ordered ascent: 10 supports</text>");
            text.Append("<text x=\"").Append(legendX).Append("\" y=\"268\" fill=\"#b7ffe1\" font-size=\"11\">9 JUMP links | span 9</text>");
            text.Append("<text x=\"40\" y=\"770\" fill=\"#d8edf7\" font-size=\"11\">Local coordinates only; no world placement or PLAYER_VERIFIED claim.</text>");
            text.Append("</svg>\n");
            return text.ToString();
        }

        private static void AppendSocket(
            StringBuilder text,
            Sv5JumpSolidPlan plan,
            Sv5JumpSolidSocket socket,
            string label,
            string color,
            int left,
            int top,
            int scale)
        {
            int x = left + socket.Foot.X * scale + scale / 2;
            int y = top + (plan.Height - socket.Foot.Y) * scale;
            text.Append("<circle cx=\"").Append(x).Append("\" cy=\"").Append(y)
                .Append("\" r=\"8\" fill=\"none\" stroke=\"").Append(color).Append("\" stroke-width=\"3\"/>");
            text.Append("<text x=\"").Append(x + 10).Append("\" y=\"").Append(y - 8)
                .Append("\" fill=\"").Append(color).Append("\" font-size=\"11\">").Append(label).Append("</text>");
        }

        private static string SocketJson(Sv5JumpSolidSocket socket)
        {
            return "{\"socket_id\":" + Json(socket.SocketId) + ",\"support_id\":" + Json(socket.SupportId) +
                ",\"foot\":" + PointJson(socket.Foot) + "}";
        }

        private static string PointJson(Sv5JumpPoint value)
        {
            return "{\"x\":" + Number(value.X) + ",\"y\":" + Number(value.Y) + "}";
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

        private static void RequirePassing(Sv5JumpSolidPlan plan)
        {
            if (plan == null || !plan.JumpSolidGeometryReady)
            {
                throw new ArgumentException("A passing local jump-solid plan is required.", nameof(plan));
            }
        }
    }
}
