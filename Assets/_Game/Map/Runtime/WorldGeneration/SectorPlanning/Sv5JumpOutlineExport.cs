using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class Sv5JumpOutlineExport
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        public static IReadOnlyDictionary<string, string> BuildArtifacts(Sv5JumpOutlinePlan plan)
        {
            RequirePassing(plan);
            var values = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "deformation_depths.csv", DeformationDepthsCsv(plan) },
                { "final_occupancy.csv", FinalOccupancyCsv(plan) },
                { "grab_action_sequence.csv", GrabActionSequenceCsv(plan) },
                { "grab_clearance.csv", GrabClearanceCsv(plan) },
                { "grab_edges.csv", GrabEdgesCsv(plan) },
                { "jump_outline.json", JumpOutlineJson(plan) },
                { "jump_outline_validation.json", ValidationJson(plan) },
                { "outline_cells.csv", OutlineCellsCsv(plan) },
                { "preview/jump_outline.svg", PreviewSvg(plan) },
                { "route_clearance.csv", RouteClearanceCsv(plan) },
                { "route_links.csv", RouteLinksCsv(plan) }
            };
            return new ReadOnlyDictionary<string, string>(values);
        }

        public static void WriteCanonical(string directory)
        {
            Write(directory, Sv5JumpOutlineGeometry.CreateCanonicalLocalFixture());
        }

        public static void Write(string directory, Sv5JumpOutlinePlan plan)
        {
            if (string.IsNullOrWhiteSpace(directory))
                throw new ArgumentException("An export directory is required.", nameof(directory));
            foreach (KeyValuePair<string, string> artifact in BuildArtifacts(plan))
            {
                string path = Path.Combine(directory, artifact.Key.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, Normalize(artifact.Value), Utf8);
            }
        }

        public static string JumpOutlineJson(Sv5JumpOutlinePlan plan)
        {
            RequirePassing(plan);
            return "{\n" +
                "  \"schema\": \"SV5_16_JUMP_OUTLINE/v1\",\n" +
                "  \"fixture_digest\": " + Json(plan.Digest) + ",\n" +
                "  \"base_fixture_digest\": " + Json(plan.BaseFixtureDigest) + ",\n" +
                "  \"coordinate_scope\": \"24x32_LOCAL_INTEGER_CELLS\",\n" +
                "  \"canvas\": {\"width\": 24, \"height\": 32, \"scope\": \"LOCAL_JUMP_ROOM_NOT_SECTOR\"},\n" +
                "  \"base_support_cells\": " + Number(plan.SupportCells.Count) + ",\n" +
                "  \"outline_cells\": " + Number(plan.OutlineCells.Count) + ",\n" +
                "  \"final_occupancy_cells\": " + Number(plan.FinalOccupancy.Count) + ",\n" +
                "  \"backed_solid_supports\": 5,\n" +
                "  \"one_way_backing_cells\": 0,\n" +
                "  \"route_link_count\": " + Number(plan.RouteLinks.Count) + ",\n" +
                "  \"grab_edge_count\": " + Number(plan.GrabEdges.Count) + ",\n" +
                "  \"filled_solid_6x6_windows\": " + Number(plan.FilledSolidSixBySixWindows) + ",\n" +
                "  \"rectangular_room_shell_created\": false,\n" +
                "  \"outline_faces_automatically_grabbable\": false,\n" +
                "  \"reverse_completion_required\": false,\n" +
                "  \"validation_states\": {\"supports\": \"STATIC_SCREEN\", \"links\": \"PLANNED\", \"player_verified_count\": 0},\n" +
                "  \"readiness\": {\n" +
                "    \"jump_contract_ready\": true,\n" +
                "    \"jump_solid_geometry_ready\": true,\n" +
                "    \"jump_grab_geometry_ready\": true,\n" +
                "    \"jump_outline_ready\": true,\n" +
                "    \"composed_geometry_ready\": false,\n" +
                "    \"player_verified\": false\n" +
                "  },\n" +
                "  \"performance\": {\n" +
                "    \"whole_world_builds_in_new_targeted_tests\": 0,\n" +
                "    \"whole_world_searches_in_new_targeted_tests\": 0\n" +
                "  }\n" +
                "}\n";
        }

        public static string DeformationDepthsCsv(Sv5JumpOutlinePlan plan)
        {
            RequirePassing(plan);
            return Csv("owner_support_id,x,support_bottom_y,depth,emitted_cell_count",
                plan.DeformationDepths.Select(value => Row(value.OwnerSupportId, value.X,
                    value.SupportBottomY, value.Depth, value.EmittedCellCount)));
        }

        public static string OutlineCellsCsv(Sv5JumpOutlinePlan plan)
        {
            RequirePassing(plan);
            return Csv("outline_cell_id,owner_support_id,x,y,depth,collision",
                plan.OutlineCells.Select(value => Row(value.OutlineCellId, value.OwnerSupportId,
                    value.Point.X, value.Point.Y, value.Depth, value.Collision)));
        }

        public static string FinalOccupancyCsv(Sv5JumpOutlinePlan plan)
        {
            RequirePassing(plan);
            return Csv("x,y,collision,owner_id,source,support_kind",
                plan.FinalOccupancy.Select(value => Row(value.Point.X, value.Point.Y, value.Collision,
                    value.OwnerId, value.Source, value.SupportKind)));
        }

        public static string RouteLinksCsv(Sv5JumpOutlinePlan plan)
        {
            RequirePassing(plan);
            return Csv("order,link_id,source_support_id,target_support_id,mode,direction,takeoff_x,takeoff_y,landing_x,landing_y,gap_air,rise,grab_edge_id,required_route",
                plan.RouteLinks.Select(value => Row(value.Order, value.LinkId, value.Source.SupportId,
                    value.Target.SupportId, Sv5JumpContract.ModeName(value.Mode),
                    Sv5JumpContract.DirectionName(value.Direction), value.Takeoff.X, value.Takeoff.Y,
                    value.Landing.X, value.Landing.Y, value.GapAir, value.Rise,
                    value.GrabEdge == null ? string.Empty : value.GrabEdge.GrabEdgeId, value.RequiredRoute)));
        }

        public static string RouteClearanceCsv(Sv5JumpOutlinePlan plan)
        {
            RequirePassing(plan);
            return Csv("support_id,foot_x,foot_y,body_x,body_y,head_x,head_y,clear",
                plan.RouteClearances.Select(value => Row(value.SupportId, value.Foot.X, value.Foot.Y,
                    value.Body.X, value.Body.Y, value.Head.X, value.Head.Y, value.Clear)));
        }

        public static string GrabEdgesCsv(Sv5JumpOutlinePlan plan)
        {
            RequirePassing(plan);
            return Csv("grab_edge_id,link_id,target_support_id,contact_x,contact_y,face,approach_direction,hang_body_x,hang_body_y,pull_up_x,pull_up_y,exposed,hang_body_clear,pull_up_clear,safe",
                plan.GrabEdges.Select(value => Row(value.GrabEdgeId, Sv5JumpGrabGeometry.GrabLinkId,
                    value.SupportId, value.Contact.X, value.Contact.Y, Sv5JumpContract.FaceName(value.Face),
                    Sv5JumpContract.DirectionName(value.ApproachDirection), value.HangBody.X, value.HangBody.Y,
                    value.PullUp.X, value.PullUp.Y, value.Exposed, value.HangBodyClear, value.PullUpClear, value.Safe)));
        }

        public static string GrabClearanceCsv(Sv5JumpOutlinePlan plan)
        {
            RequirePassing(plan);
            return Csv("grab_edge_id,hang_body_x,hang_body_y,hang_head_x,hang_head_y,pull_up_foot_x,pull_up_foot_y,pull_up_head_x,pull_up_head_y,all_air",
                plan.GrabClearances.Select(value => Row(value.GrabEdgeId, value.HangBody.X, value.HangBody.Y,
                    value.HangHead.X, value.HangHead.Y, value.PullUpFoot.X, value.PullUpFoot.Y,
                    value.PullUpHead.X, value.PullUpHead.Y, value.AllAir)));
        }

        public static string GrabActionSequenceCsv(Sv5JumpOutlinePlan plan)
        {
            RequirePassing(plan);
            return Csv("order,action,player_x,player_y,contact_x,contact_y,support_id",
                plan.Actions.Select(value => Row(value.Order, value.Action, value.Player.X, value.Player.Y,
                    value.Contact.X, value.Contact.Y, value.SupportId)));
        }

        public static string ValidationJson(Sv5JumpOutlinePlan plan)
        {
            RequirePassing(plan);
            return "{\n" +
                "  \"schema\": \"SV5_16_JUMP_OUTLINE_VALIDATION/v1\",\n" +
                "  \"status\": \"PASS\",\n" +
                "  \"errors\": [],\n" +
                "  \"fixture_digest\": " + Json(plan.Digest) + ",\n" +
                "  \"base_fixture_digest\": " + Json(plan.BaseFixtureDigest) + ",\n" +
                "  \"base_support_cells\": " + Number(plan.SupportCells.Count) + ",\n" +
                "  \"outline_cells\": " + Number(plan.OutlineCells.Count) + ",\n" +
                "  \"final_occupancy_cells\": " + Number(plan.FinalOccupancy.Count) + ",\n" +
                "  \"route_clearance_pass\": " + Number(plan.RouteClearances.Count(value => value.Clear)) + ",\n" +
                "  \"grab_edge_count\": " + Number(plan.GrabEdges.Count) + ",\n" +
                "  \"filled_solid_6x6_windows\": " + Number(plan.FilledSolidSixBySixWindows) + ",\n" +
                "  \"jump_contract_ready\": true,\n" +
                "  \"jump_solid_geometry_ready\": true,\n" +
                "  \"jump_grab_geometry_ready\": true,\n" +
                "  \"jump_outline_ready\": true,\n" +
                "  \"composed_geometry_ready\": false,\n" +
                "  \"player_verified\": false\n" +
                "}\n";
        }

        public static string PreviewSvg(Sv5JumpOutlinePlan plan)
        {
            RequirePassing(plan);
            const int scale = 20;
            const int left = 40;
            const int top = 48;
            const int viewWidth = 980;
            const int viewHeight = 760;
            int canvasWidth = plan.Width * scale;
            int canvasHeight = plan.Height * scale;
            var text = new StringBuilder();
            text.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 980 760\">");
            text.Append("<rect width=\"100%\" height=\"100%\" fill=\"#0b1720\"/>");
            text.Append("<text x=\"40\" y=\"27\" fill=\"#ffffff\" font-size=\"16\">SV5_16 24x32 local jump room | irregular SOLID outline</text>");
            text.Append("<rect x=\"").Append(left).Append("\" y=\"").Append(top).Append("\" width=\"")
                .Append(canvasWidth).Append("\" height=\"").Append(canvasHeight)
                .Append("\" fill=\"#132832\" stroke=\"#e4f4fa\" stroke-width=\"2\"/>");
            for (int x = 1; x < plan.Width; x++)
            {
                int px = left + x * scale;
                text.Append("<line x1=\"").Append(px).Append("\" y1=\"").Append(top).Append("\" x2=\"")
                    .Append(px).Append("\" y2=\"").Append(top + canvasHeight)
                    .Append("\" stroke=\"#29434e\" stroke-width=\"0.45\"/>");
            }
            for (int y = 1; y < plan.Height; y++)
            {
                int py = top + y * scale;
                text.Append("<line x1=\"").Append(left).Append("\" y1=\"").Append(py).Append("\" x2=\"")
                    .Append(left + canvasWidth).Append("\" y2=\"").Append(py)
                    .Append("\" stroke=\"#29434e\" stroke-width=\"0.45\"/>");
            }
            foreach (Sv5JumpFinalOccupancyCell cell in plan.FinalOccupancy)
            {
                int x = left + cell.Point.X * scale;
                int y = top + (plan.Height - 1 - cell.Point.Y) * scale;
                string fill = cell.Source == "OUTLINE_BACKING" ? "#9b4f32" :
                    cell.SupportKind == "SOLID" ? "#d95f59" : "#398fc6";
                text.Append("<rect x=\"").Append(x).Append("\" y=\"").Append(y)
                    .Append("\" width=\"").Append(scale).Append("\" height=\"").Append(scale)
                    .Append("\" fill=\"").Append(fill).Append("\" stroke=\"#edf8fb\" stroke-width=\"1\"/>");
                if (cell.SupportKind == "ONE_WAY")
                    text.Append("<line x1=\"").Append(x).Append("\" y1=\"").Append(y).Append("\" x2=\"")
                        .Append(x + scale).Append("\" y2=\"").Append(y)
                        .Append("\" stroke=\"#9ee0ff\" stroke-width=\"4\"/>");
            }
            foreach (Sv5JumpGrabRouteLink link in plan.RouteLinks)
            {
                int x1 = left + link.Takeoff.X * scale + scale / 2;
                int y1 = top + (plan.Height - link.Takeoff.Y) * scale;
                int x2 = left + link.Landing.X * scale + scale / 2;
                int y2 = top + (plan.Height - link.Landing.Y) * scale;
                string color = link.Mode == Sv5JumpMode.JumpGrab ? "#ffca4b" : "#62e0a5";
                text.Append("<line x1=\"").Append(x1).Append("\" y1=\"").Append(y1).Append("\" x2=\"")
                    .Append(x2).Append("\" y2=\"").Append(y2).Append("\" stroke=\"").Append(color)
                    .Append("\" stroke-width=\"3\" stroke-dasharray=\"7 4\"/>");
            }
            Sv5JumpGrabEdge edge = plan.GrabEdges.Single();
            int gx = left + (edge.Contact.X + 1) * scale;
            int gy = top + (plan.Height - 1 - edge.Contact.Y) * scale;
            text.Append("<line x1=\"").Append(gx).Append("\" y1=\"").Append(gy).Append("\" x2=\"")
                .Append(gx).Append("\" y2=\"").Append(gy + scale)
                .Append("\" stroke=\"#fff176\" stroke-width=\"5\"/>");
            int legendX = left + canvasWidth + 28;
            text.Append("<text x=\"").Append(legendX).Append("\" y=\"70\" fill=\"white\" font-size=\"15\">LEGEND</text>");
            Legend(text, legendX, 88, "#d95f59", "base SOLID");
            Legend(text, legendX, 118, "#398fc6", "ONE_WAY");
            Legend(text, legendX, 148, "#9b4f32", "outline SOLID");
            text.Append("<line x1=\"").Append(legendX).Append("\" y1=\"190\" x2=\"").Append(legendX + 20)
                .Append("\" y2=\"190\" stroke=\"#ffca4b\" stroke-width=\"4\"/>");
            text.Append("<text x=\"").Append(legendX + 28).Append("\" y=\"194\" fill=\"#ffca4b\" font-size=\"12\">JUMP_GRAB</text>");
            text.Append("<text x=\"").Append(legendX).Append("\" y=\"238\" fill=\"#ffffff\" font-size=\"11\">0-2 cell downward deformation</text>");
            text.Append("<text x=\"").Append(legendX).Append("\" y=\"262\" fill=\"#9ee0ff\" font-size=\"11\">ONE_WAY unchanged</text>");
            text.Append("<text x=\"").Append(legendX).Append("\" y=\"286\" fill=\"#d8edf7\" font-size=\"11\">no rectangular room shell</text>");
            text.Append("<text x=\"").Append(legendX).Append("\" y=\"310\" fill=\"#d8edf7\" font-size=\"11\">PLAYER verification deferred</text>");
            text.Append("<text x=\"").Append(legendX).Append("\" y=\"350\" fill=\"#ffffff\" font-size=\"11\">27 base + 17 outline = 44 cells</text>");
            text.Append("<text x=\"40\" y=\"728\" fill=\"#d8edf7\" font-size=\"11\">SV5_15 supports, nine links, and JS04_RIGHT_GRAB remain fixed.</text>");
            text.Append("</svg>\n");
            return text.ToString();
        }

        private static void Legend(StringBuilder text, int x, int y, string color, string label)
        {
            text.Append("<rect x=\"").Append(x).Append("\" y=\"").Append(y)
                .Append("\" width=\"18\" height=\"18\" fill=\"").Append(color).Append("\"/>");
            text.Append("<text x=\"").Append(x + 28).Append("\" y=\"").Append(y + 14)
                .Append("\" fill=\"white\" font-size=\"12\">").Append(label).Append("</text>");
        }

        private static string Csv(string header, IEnumerable<string> rows)
        {
            return header + "\n" + string.Join("\n", rows) + "\n";
        }

        private static string Row(params object[] values)
        {
            return string.Join(",", values.Select(value => Quote(value == null ? string.Empty :
                value is bool && (bool)value ? "true" : value is bool ? "false" :
                Convert.ToString(value, CultureInfo.InvariantCulture))));
        }

        private static string Quote(string value)
        {
            return value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0 ? value :
                "\"" + value.Replace("\"", "\"\"") + "\"";
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

        private static void RequirePassing(Sv5JumpOutlinePlan plan)
        {
            if (plan == null || !plan.JumpOutlineReady)
                throw new ArgumentException("A passing local jump-outline plan is required.", nameof(plan));
        }
    }
}
