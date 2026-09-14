using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class Sv5JumpContractExport
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        public static IReadOnlyDictionary<string, string> BuildArtifacts(Sv5JumpContractFixture fixture)
        {
            RequirePassing(fixture);
            var values = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "jump_contract.json", JumpContractJson(fixture) },
                { "supports.csv", SupportsCsv(fixture) },
                { "grab_edges.csv", GrabEdgesCsv(fixture) },
                { "links.csv", LinksCsv(fixture) },
                { "measurement_proofs.csv", MeasurementProofsCsv(fixture) },
                { "validation_states.csv", ValidationStatesCsv(fixture) },
                { "contract_validation.json", ValidationJson(fixture) },
                { "preview/jump_contract.svg", PreviewSvg(fixture) }
            };
            return new ReadOnlyDictionary<string, string>(values);
        }

        public static void WriteCanonical(string directory)
        {
            Write(directory, Sv5JumpContract.CreateCanonicalLocalFixture());
        }

        public static void Write(string directory, Sv5JumpContractFixture fixture)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("An export directory is required.", nameof(directory));
            }

            foreach (KeyValuePair<string, string> artifact in BuildArtifacts(fixture))
            {
                string path = Path.Combine(directory, artifact.Key.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, Normalize(artifact.Value), Utf8);
            }
        }

        public static string JumpContractJson(Sv5JumpContractFixture fixture)
        {
            RequirePassing(fixture);
            var text = new StringBuilder();
            text.Append("{\n");
            text.Append("  \"schema\": \"SV5_13_JUMP_CONTRACT/v1\",\n");
            text.Append("  \"contract_digest\": ").Append(Json(fixture.Digest)).Append(",\n");
            text.Append("  \"canonical_link_ids\": [\"J1\", \"J2\", \"J3\"],\n");
            text.Append("  \"supports\": [\n");
            AppendArray(text, fixture.Supports.Select(SupportJson));
            text.Append("  ],\n");
            text.Append("  \"grab_edges\": [\n");
            AppendArray(text, fixture.GrabEdges.Select(GrabJson));
            text.Append("  ],\n");
            text.Append("  \"links\": [\n");
            AppendArray(text, fixture.Links.Select(LinkJson));
            text.Append("  ],\n");
            text.Append("  \"measurement_proofs\": [\n");
            AppendArray(text, fixture.MeasurementProofs.Select(ProofJson));
            text.Append("  ],\n");
            text.Append("  \"validation_states\": [\n");
            AppendArray(text, fixture.ValidationStates.Select(StateJson));
            text.Append("  ],\n");
            text.Append("  \"one_way_route_semantics\": {\"reverse_completion_required\": false},\n");
            text.Append("  \"readiness\": {\n");
            text.Append("    \"jump_contract_ready\": true,\n");
            text.Append("    \"jump_solid_geometry_ready\": false,\n");
            text.Append("    \"jump_grab_geometry_ready\": false,\n");
            text.Append("    \"composed_geometry_ready\": false,\n");
            text.Append("    \"player_verified\": false\n");
            text.Append("  },\n");
            text.Append("  \"performance\": {\n");
            text.Append("    \"fixture_scope\": \"LOCAL_INTEGER_CELLS\",\n");
            text.Append("    \"whole_world_builds_in_new_targeted_tests\": 0,\n");
            text.Append("    \"whole_world_searches_in_new_targeted_tests\": 0\n");
            text.Append("  }\n");
            text.Append("}\n");
            return text.ToString();
        }

        public static string SupportsCsv(Sv5JumpContractFixture fixture)
        {
            RequirePassing(fixture);
            return Csv(
                "support_id,kind,x,y,width,height,top_y,top_x_min,top_x_max,active_route,decorative_only,validation_state",
                fixture.Supports.Select(value => Row(
                    value.SupportId,
                    value.ExportKind,
                    value.X,
                    value.Y,
                    value.Width,
                    value.Height,
                    value.TopY,
                    value.TopXMin,
                    value.TopXMax,
                    value.ActiveRoute,
                    value.DecorativeOnly,
                    Sv5JumpContract.StateName(value.ValidationState))));
        }

        public static string GrabEdgesCsv(Sv5JumpContractFixture fixture)
        {
            RequirePassing(fixture);
            return Csv(
                "grab_edge_id,support_id,contact_x,contact_y,face,approach_direction,hang_body_x,hang_body_y,pull_up_x,pull_up_y,safe",
                fixture.GrabEdges.Select(value => Row(
                    value.GrabEdgeId,
                    value.SupportId,
                    value.Contact.X,
                    value.Contact.Y,
                    Sv5JumpContract.FaceName(value.Face),
                    Sv5JumpContract.DirectionName(value.ApproachDirection),
                    value.HangBody.X,
                    value.HangBody.Y,
                    value.PullUp.X,
                    value.PullUp.Y,
                    value.Safe)));
        }

        public static string LinksCsv(Sv5JumpContractFixture fixture)
        {
            RequirePassing(fixture);
            return Csv(
                "link_id,source_support_id,target_support_id,mode,direction,takeoff_x,takeoff_y,landing_x,landing_y,gap_air,rise,grab_edge_id,validation_state",
                fixture.Links.Select(value => Row(
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
                    Sv5JumpContract.StateName(value.ValidationState))));
        }

        public static string MeasurementProofsCsv(Sv5JumpContractFixture fixture)
        {
            RequirePassing(fixture);
            return Csv(
                "link_id,source_near_face_x,target_near_face_x,computed_gap_air,source_top_y,target_top_y,computed_rise,formula_pass",
                fixture.MeasurementProofs.Select(value => Row(
                    value.LinkId,
                    value.SourceNearFaceX,
                    value.TargetNearFaceX,
                    value.ComputedGapAir,
                    value.SourceTopY,
                    value.TargetTopY,
                    value.ComputedRise,
                    value.FormulaPass)));
        }

        public static string ValidationStatesCsv(Sv5JumpContractFixture fixture)
        {
            RequirePassing(fixture);
            return Csv(
                "object_id,object_kind,state,player_run_id,player_result",
                fixture.ValidationStates.Select(value => Row(
                    value.ObjectId,
                    value.ObjectKind,
                    Sv5JumpContract.StateName(value.State),
                    value.PlayerRunId,
                    value.PlayerResult)));
        }

        public static string ValidationJson(Sv5JumpContractFixture fixture)
        {
            RequirePassing(fixture);
            int planned = fixture.ValidationStates.Count(value => value.State == Sv5JumpValidationState.Planned);
            int staticScreen = fixture.ValidationStates.Count(value => value.State == Sv5JumpValidationState.StaticScreen);
            int playerVerified = fixture.ValidationStates.Count(value => value.State == Sv5JumpValidationState.PlayerVerified);
            return "{\n" +
                "  \"schema\": \"SV5_13_JUMP_CONTRACT_VALIDATION/v1\",\n" +
                "  \"status\": \"PASS\",\n" +
                "  \"errors\": [],\n" +
                "  \"contract_digest\": " + Json(fixture.Digest) + ",\n" +
                "  \"support_count\": " + Number(fixture.Supports.Count) + ",\n" +
                "  \"solid_support_count\": " + Number(fixture.Supports.Count(value => value.Kind == Sv5JumpSupportKind.Solid)) + ",\n" +
                "  \"one_way_support_count\": " + Number(fixture.Supports.Count(value => value.Kind == Sv5JumpSupportKind.OneWay)) + ",\n" +
                "  \"grab_edge_count\": " + Number(fixture.GrabEdges.Count) + ",\n" +
                "  \"link_count\": " + Number(fixture.Links.Count) + ",\n" +
                "  \"planned_count\": " + Number(planned) + ",\n" +
                "  \"static_screen_count\": " + Number(staticScreen) + ",\n" +
                "  \"player_verified_count\": " + Number(playerVerified) + ",\n" +
                "  \"whole_world_builds_in_new_targeted_tests\": 0,\n" +
                "  \"reverse_completion_required\": false,\n" +
                "  \"jump_contract_ready\": true,\n" +
                "  \"jump_solid_geometry_ready\": false,\n" +
                "  \"jump_grab_geometry_ready\": false,\n" +
                "  \"composed_geometry_ready\": false,\n" +
                "  \"player_verified\": false\n" +
                "}\n";
        }

        public static string PreviewSvg(Sv5JumpContractFixture fixture)
        {
            RequirePassing(fixture);
            const int scale = 20;
            const int baseline = 128;
            var text = new StringBuilder();
            text.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 620 230\">");
            text.Append("<rect width=\"100%\" height=\"100%\" fill=\"#101820\"/>");
            text.Append("<text x=\"12\" y=\"18\" fill=\"white\" font-size=\"13\">SV5_13 local jump contract | SOLID / ONE_WAY</text>");

            foreach (Sv5JumpSupport support in fixture.Supports)
            {
                int x = 24 + support.X * scale;
                int y = baseline - support.TopY * scale;
                if (support.Kind == Sv5JumpSupportKind.Solid)
                {
                    text.Append("<rect x=\"").Append(x).Append("\" y=\"").Append(y)
                        .Append("\" width=\"").Append(support.Width * scale).Append("\" height=\"")
                        .Append(support.Height * scale).Append("\" fill=\"#d85b5b\" stroke=\"#ffd0d0\"/>");
                }
                else
                {
                    text.Append("<line x1=\"").Append(x).Append("\" y1=\"").Append(y)
                        .Append("\" x2=\"").Append(x + support.Width * scale).Append("\" y2=\"")
                        .Append(y).Append("\" stroke=\"#55b7ff\" stroke-width=\"5\"/>");
                }

                text.Append("<text x=\"").Append(x).Append("\" y=\"").Append(y - 5)
                    .Append("\" fill=\"").Append(support.Kind == Sv5JumpSupportKind.Solid ? "#ff9e9e" : "#8ed2ff")
                    .Append("\" font-size=\"7\">").Append(Xml(support.ExportKind)).Append("</text>");
            }

            foreach (Sv5JumpLink link in fixture.Links)
            {
                int x1 = 24 + link.Takeoff.X * scale + scale / 2;
                int y1 = baseline - link.Takeoff.Y * scale;
                int x2 = 24 + link.Landing.X * scale + scale / 2;
                int y2 = baseline - link.Landing.Y * scale;
                string color = link.Mode == Sv5JumpMode.JumpGrab ? "#ffd166" : "#69e5ae";
                text.Append("<line x1=\"").Append(x1).Append("\" y1=\"").Append(y1)
                    .Append("\" x2=\"").Append(x2).Append("\" y2=\"").Append(y2)
                    .Append("\" stroke=\"").Append(color).Append("\" stroke-width=\"2\" stroke-dasharray=\"4 2\"/>");
                text.Append("<circle cx=\"").Append(x1).Append("\" cy=\"").Append(y1)
                    .Append("\" r=\"4\" fill=\"#ffffff\"/><circle cx=\"").Append(x2).Append("\" cy=\"")
                    .Append(y2).Append("\" r=\"4\" fill=\"#00ff9d\"/>");
                text.Append("<text x=\"").Append(Math.Min(x1, x2)).Append("\" y=\"").Append(155 + (link.LinkId[1] - '1') * 18)
                    .Append("\" fill=\"").Append(color).Append("\" font-size=\"10\">")
                    .Append(link.LinkId).Append(" ").Append(Sv5JumpContract.ModeName(link.Mode))
                    .Append(" | gap_air=").Append(link.GapAir).Append(" rise=").Append(link.Rise)
                    .Append(" | ").Append(Sv5JumpContract.StateName(link.ValidationState)).Append("</text>");
            }

            Sv5JumpGrabEdge grab = fixture.GrabEdges.Single();
            int contactX = 24 + grab.Contact.X * scale;
            int contactY = baseline - (grab.Contact.Y + 1) * scale;
            text.Append("<circle cx=\"").Append(contactX).Append("\" cy=\"").Append(contactY)
                .Append("\" r=\"6\" fill=\"none\" stroke=\"#ffcc00\" stroke-width=\"3\"/>");
            text.Append("<text x=\"").Append(contactX - 94).Append("\" y=\"").Append(contactY - 8)
                .Append("\" fill=\"#ffcc00\" font-size=\"8\">exposed SOLID Grab contact / hang-body / pull-up</text>");
            text.Append("<text x=\"14\" y=\"214\" fill=\"#ffffff\" font-size=\"9\">takeoff = white | landing = green | gap_air counts empty columns between facing boundaries | rise uses top faces</text>");
            text.Append("</svg>\n");
            return text.ToString();
        }

        private static string SupportJson(Sv5JumpSupport value)
        {
            return "    {\"support_id\":" + Json(value.SupportId) + ",\"kind\":" + Json(value.ExportKind) +
                ",\"x\":" + Number(value.X) + ",\"y\":" + Number(value.Y) +
                ",\"width\":" + Number(value.Width) + ",\"height\":" + Number(value.Height) +
                ",\"top_y\":" + Number(value.TopY) + ",\"top_x_min\":" + Number(value.TopXMin) +
                ",\"top_x_max\":" + Number(value.TopXMax) + ",\"active_route\":" + Boolean(value.ActiveRoute) +
                ",\"decorative_only\":" + Boolean(value.DecorativeOnly) + ",\"validation_state\":" +
                Json(Sv5JumpContract.StateName(value.ValidationState)) + "}";
        }

        private static string GrabJson(Sv5JumpGrabEdge value)
        {
            return "    {\"grab_edge_id\":" + Json(value.GrabEdgeId) + ",\"support_id\":" + Json(value.SupportId) +
                ",\"contact\":" + PointJson(value.Contact) + ",\"face\":" + Json(Sv5JumpContract.FaceName(value.Face)) +
                ",\"approach_direction\":" + Json(Sv5JumpContract.DirectionName(value.ApproachDirection)) +
                ",\"hang_body\":" + PointJson(value.HangBody) + ",\"pull_up\":" + PointJson(value.PullUp) +
                ",\"exposed\":" + Boolean(value.Exposed) + ",\"hang_body_clear\":" + Boolean(value.HangBodyClear) +
                ",\"pull_up_clear\":" + Boolean(value.PullUpClear) + ",\"safe\":" + Boolean(value.Safe) + "}";
        }

        private static string LinkJson(Sv5JumpLink value)
        {
            return "    {\"link_id\":" + Json(value.LinkId) + ",\"source_support_id\":" + Json(value.Source.SupportId) +
                ",\"target_support_id\":" + Json(value.Target.SupportId) + ",\"direction\":" +
                Json(Sv5JumpContract.DirectionName(value.Direction)) + ",\"takeoff\":" + PointJson(value.Takeoff) +
                ",\"landing\":" + PointJson(value.Landing) + ",\"gap_air\":" + Number(value.GapAir) +
                ",\"rise\":" + Number(value.Rise) + ",\"mode\":" + Json(Sv5JumpContract.ModeName(value.Mode)) +
                ",\"grab_edge_id\":" + Json(value.GrabEdge == null ? string.Empty : value.GrabEdge.GrabEdgeId) +
                ",\"validation_state\":" + Json(Sv5JumpContract.StateName(value.ValidationState)) + "}";
        }

        private static string ProofJson(Sv5JumpMeasurementProof value)
        {
            return "    {\"link_id\":" + Json(value.LinkId) + ",\"source_near_face_x\":" + Number(value.SourceNearFaceX) +
                ",\"target_near_face_x\":" + Number(value.TargetNearFaceX) + ",\"computed_gap_air\":" +
                Number(value.ComputedGapAir) + ",\"source_top_y\":" + Number(value.SourceTopY) +
                ",\"target_top_y\":" + Number(value.TargetTopY) + ",\"computed_rise\":" + Number(value.ComputedRise) +
                ",\"formula_pass\":" + Boolean(value.FormulaPass) + "}";
        }

        private static string StateJson(Sv5JumpValidationEvidence value)
        {
            return "    {\"object_id\":" + Json(value.ObjectId) + ",\"object_kind\":" + Json(value.ObjectKind) +
                ",\"state\":" + Json(Sv5JumpContract.StateName(value.State)) + ",\"player_run_id\":" +
                Json(value.PlayerRunId) + ",\"player_result\":" + Json(value.PlayerResult) + "}";
        }

        private static void AppendArray(StringBuilder text, IEnumerable<string> rows)
        {
            string[] values = rows.ToArray();
            for (int index = 0; index < values.Length; index++)
            {
                text.Append(values[index]);
                text.Append(index + 1 == values.Length ? "\n" : ",\n");
            }
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

        private static string Xml(string value)
        {
            return (value ?? string.Empty).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        private static string Number(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string Boolean(bool value)
        {
            return value ? "true" : "false";
        }

        private static string Normalize(string value)
        {
            return value.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        private static void RequirePassing(Sv5JumpContractFixture fixture)
        {
            if (fixture == null || !fixture.JumpContractReady)
            {
                throw new ArgumentException("A passing canonical jump-contract fixture is required.", nameof(fixture));
            }
        }
    }
}
