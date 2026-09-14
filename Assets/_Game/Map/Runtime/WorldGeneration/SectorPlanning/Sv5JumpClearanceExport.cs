using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class Sv5JumpClearanceExport
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        public static IReadOnlyDictionary<string, string> BuildArtifacts(Sv5JumpClearancePlan plan)
        {
            RequirePassing(plan);
            return new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "clearance_grab_contacts.csv", GrabContactsCsv(plan) },
                { "clearance_links.csv", LinksCsv(plan) },
                { "clearance_trace.csv", TraceCsv(plan) },
                { "clearance_validation.json", ValidationJson(plan) },
                { "jump_clearance.json", JumpClearanceJson(plan) },
                { "preview/jump_clearance.svg", PreviewSvg(plan) },
            };
        }

        public static void WriteCanonical(string directory)
        {
            Write(directory, Sv5JumpClearance.CreateCanonicalLocalProof());
        }

        public static void Write(string directory, Sv5JumpClearancePlan plan)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Export directory is required.", nameof(directory));
            foreach (KeyValuePair<string, string> artifact in BuildArtifacts(plan))
            {
                string path = Path.Combine(directory, artifact.Key.Replace('/', Path.DirectorySeparatorChar));
                string parent = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
                File.WriteAllText(path, Normalize(artifact.Value), Utf8);
            }
        }

        public static string JumpClearanceJson(Sv5JumpClearancePlan plan)
        {
            RequirePassing(plan);
            var text = new StringBuilder();
            text.AppendLine("{");
            text.AppendLine("  \"schema\": \"SV5_18_JUMP_CLEARANCE/v1\",");
            text.AppendLine("  \"task\": \"SV5_18_JUMP_CLEARANCE\",");
            text.AppendLine("  \"status\": \"PASS\",");
            text.AppendLine("  \"coordinate_scope\": \"24x32_LOCAL_INTEGER_CELLS\",");
            text.AppendLine("  \"input_catalog_digest\": \"" + Json(plan.InputCatalogDigest) + "\",");
            text.AppendLine("  \"clearance_digest\": \"" + Json(plan.Digest) + "\",");
            text.AppendLine("  \"endpoint_correction_id\": \"" + Sv5JumpClearance.EndpointCorrectionId + "\",");
            text.AppendLine("  \"endpoint_correction_count\": " + plan.EndpointCorrectionCount + ",");
            text.AppendLine("  \"effective_link_endpoint_digest\": \"" + Sv5JumpClearance.EffectiveLinkEndpointDigest + "\",");
            text.AppendLine("  \"recipe_count\": 2,");
            text.AppendLine("  \"link_count\": " + plan.Links.Count + ",");
            text.AppendLine("  \"passed_link_count\": " + plan.Links.Count(value => value.ClearancePass) + ",");
            text.AppendLine("  \"trace_state_count\": " + plan.Links.Sum(value => value.Trace.Samples.Count) + ",");
            text.AppendLine("  \"grab_contact_count\": " + plan.GrabContacts.Count + ",");
            text.AppendLine("  \"grab_pull_up_supercover_exception\": {");
            text.AppendLine("    \"source_link_id\": \"JS_LINK_03\",");
            text.AppendLine("    \"transition\": \"HANG_TO_PULL_UP\",");
            text.AppendLine("    \"allowed_occupied_corner\": \"EXACT_EXPORTED_SOLID_GRAB_CONTACT\",");
            text.AppendLine("    \"other_corner_and_body_head_clear\": true");
            text.AppendLine("  },");
            text.AppendLine("  \"readiness\": {");
            text.AppendLine("    \"JumpRecipeReady\": true,");
            text.AppendLine("    \"ComposedGeometryReady\": true,");
            text.AppendLine("    \"SweptClearanceReady\": true,");
            text.AppendLine("    \"RecoveryReady\": false,");
            text.AppendLine("    \"PlayerVerified\": false");
            text.AppendLine("  },");
            text.AppendLine("  \"whole_world_builds\": 0,");
            text.AppendLine("  \"whole_world_searches\": 0,");
            text.AppendLine("  \"global_endpoint_comparisons\": 0");
            text.AppendLine("}");
            return text.ToString();
        }

        public static string LinksCsv(Sv5JumpClearancePlan plan)
        {
            RequirePassing(plan);
            return Csv(
                "recipe_id,order,source_link_id,mode,direction,source_takeoff_x,source_takeoff_y,takeoff_x,takeoff_y,landing_x,landing_y,endpoint_adjusted,correction_id,trace_states,clearance_pass,diagnostic",
                plan.Links.Select(value => Row(
                    value.Trace.RecipeId, value.Trace.Order, value.Trace.SourceLinkId,
                    Sv5JumpClearance.ModeName(value.Trace.Mode), Sv5JumpClearance.DirectionName(value.Trace.Direction),
                    value.Trace.SourceTakeoff.X, value.Trace.SourceTakeoff.Y,
                    value.Trace.EffectiveTakeoff.X, value.Trace.EffectiveTakeoff.Y,
                    value.Trace.Landing.X, value.Trace.Landing.Y, value.Trace.EndpointAdjusted,
                    value.Trace.CorrectionId, value.Trace.Samples.Count, value.ClearancePass,
                    string.Join(";", value.Diagnostics))));
        }

        public static string TraceCsv(Sv5JumpClearancePlan plan)
        {
            RequirePassing(plan);
            return Csv(
                "recipe_id,link_order,source_link_id,sample_order,phase,foot_x,foot_y,body_x,body_y,head_x,head_y,body_clear,head_clear,contract_accepted",
                plan.Links.SelectMany(result => result.Trace.Samples.Select(sample => Row(
                    result.Trace.RecipeId, result.Trace.Order, result.Trace.SourceLinkId, sample.SampleOrder,
                    Sv5JumpClearance.PhaseName(sample.Phase), sample.Foot.X, sample.Foot.Y,
                    sample.Body.X, sample.Body.Y, sample.Head.X, sample.Head.Y, true, true, result.ClearancePass))));
        }

        public static string GrabContactsCsv(Sv5JumpClearancePlan plan)
        {
            RequirePassing(plan);
            return Csv(
                "recipe_id,source_link_id,source_grab_edge_id,contact_x,contact_y,face,hang_body_x,hang_body_y,hang_head_x,hang_head_y,pull_up_x,pull_up_y,pull_up_head_x,pull_up_head_y,contact_solid,exposed,safe,trace_bound",
                plan.GrabContacts.Select(value => Row(value.RecipeId, value.SourceLinkId, value.SourceGrabEdgeId,
                    value.Contact.X, value.Contact.Y, Sv5JumpClearance.FaceName(value.Face),
                    value.HangBody.X, value.HangBody.Y, value.HangHead.X, value.HangHead.Y,
                    value.PullUp.X, value.PullUp.Y, value.PullUpHead.X, value.PullUpHead.Y,
                    true, true, true, true)));
        }

        public static string ValidationJson(Sv5JumpClearancePlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var text = new StringBuilder();
            text.AppendLine("{");
            text.AppendLine("  \"schema\": \"SV5_18_JUMP_CLEARANCE_VALIDATION/v1\",");
            text.AppendLine("  \"status\": \"" + (plan.Diagnostics.Count == 0 ? "PASS" : "FAIL") + "\",");
            text.AppendLine("  \"input_catalog_digest\": \"" + Json(plan.InputCatalogDigest) + "\",");
            text.AppendLine("  \"clearance_digest\": \"" + Json(plan.Digest) + "\",");
            text.AppendLine("  \"endpoint_correction_id\": \"" + Sv5JumpClearance.EndpointCorrectionId + "\",");
            text.AppendLine("  \"endpoint_correction_count\": " + plan.EndpointCorrectionCount + ",");
            text.AppendLine("  \"effective_link_endpoint_digest\": \"" + Sv5JumpClearance.EffectiveLinkEndpointDigest + "\",");
            text.AppendLine("  \"grab_pull_up_contact_corner_exception_count\": 2,");
            text.AppendLine("  \"link_count\": " + plan.Links.Count + ",");
            text.AppendLine("  \"passed_link_count\": " + plan.Links.Count(value => value.ClearancePass) + ",");
            text.AppendLine("  \"errors\": [");
            for (int index = 0; index < plan.Diagnostics.Count; index++)
            {
                text.Append("    \"").Append(Json(plan.Diagnostics[index])).Append('"');
                text.AppendLine(index + 1 == plan.Diagnostics.Count ? string.Empty : ",");
            }
            text.AppendLine("  ]");
            text.AppendLine("}");
            return text.ToString();
        }

        public static string PreviewSvg(Sv5JumpClearancePlan plan)
        {
            RequirePassing(plan);
            Sv5JumpRecipeCatalogModel catalog = Sv5JumpRecipeCatalog.CreateCanonicalCatalog();
            const int cell = 13;
            const int boardY = 78;
            var text = new StringBuilder();
            text.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"880\" height=\"620\" viewBox=\"0 0 880 620\">");
            text.AppendLine("<rect width=\"880\" height=\"620\" fill=\"#0e1520\"/>");
            text.AppendLine("<style>text{font-family:Consolas,monospace;fill:#eef6ff}.title{font-size:19px;font-weight:bold}.label{font-size:11px}.muted{fill:#9db0c5}.grid{stroke:#2a394b;stroke-width:.45}.trace{fill:#69e6b6;stroke:#10222a;stroke-width:.7}.head{fill:none;stroke:#8ecbff;stroke-width:1}.grab{fill:#ff5d8f;stroke:#fff;stroke-width:1.2}.adjusted{fill:#ffd166;stroke:#fff;stroke-width:1}</style>");
            text.AppendLine("<text x=\"24\" y=\"29\" class=\"title\">SV5_18 local body/head swept-clearance proof</text>");
            text.AppendLine("<text x=\"24\" y=\"52\" class=\"label muted\">two 24x32 recipes | 18 links | Endpoint Fix01 x4 | RecoveryReady=false | PlayerVerified=false</text>");
            DrawRecipe(text, catalog, plan, Sv5JumpRecipeCatalog.R0RecipeId, 24, boardY, cell);
            DrawRecipe(text, catalog, plan, Sv5JumpRecipeCatalog.MirrorRecipeId, 448, boardY, cell);
            text.AppendLine("<rect x=\"24\" y=\"522\" width=\"11\" height=\"11\" fill=\"#53677d\"/><text x=\"41\" y=\"532\" class=\"label\">SOLID</text>");
            text.AppendLine("<rect x=\"108\" y=\"522\" width=\"11\" height=\"11\" fill=\"#2f8f83\"/><text x=\"125\" y=\"532\" class=\"label\">ONE_WAY</text>");
            text.AppendLine("<circle cx=\"227\" cy=\"527\" r=\"5\" class=\"trace\"/><text x=\"239\" y=\"532\" class=\"label\">body/foot trace</text>");
            text.AppendLine("<rect x=\"366\" y=\"521\" width=\"11\" height=\"11\" class=\"head\"/><text x=\"383\" y=\"532\" class=\"label\">head cell</text>");
            text.AppendLine("<circle cx=\"490\" cy=\"527\" r=\"5\" class=\"adjusted\"/><text x=\"502\" y=\"532\" class=\"label\">effective takeoff</text>");
            text.AppendLine("<circle cx=\"655\" cy=\"527\" r=\"5\" class=\"grab\"/><text x=\"667\" y=\"532\" class=\"label\">SOLID Grab contact</text>");
            text.AppendLine("<text x=\"24\" y=\"566\" class=\"label\">JS_LINK_03: TAKEOFF → AIR → HANG → PULL_UP; only the exact contact corner is exempt.</text>");
            text.AppendLine("<text x=\"24\" y=\"590\" class=\"label muted\">Discrete occupancy evidence only; no live motor, recovery, or Player verification claim.</text>");
            text.AppendLine("</svg>");
            return text.ToString();
        }

        private static void DrawRecipe(
            StringBuilder text,
            Sv5JumpRecipeCatalogModel catalog,
            Sv5JumpClearancePlan plan,
            string recipeId,
            int originX,
            int originY,
            int cell)
        {
            Sv5JumpRecipeVariant recipe = catalog.Recipe(recipeId);
            text.AppendLine("<text x=\"" + originX + "\" y=\"" + (originY - 12) + "\" class=\"title\">" + recipeId + "</text>");
            text.AppendLine("<rect x=\"" + originX + "\" y=\"" + originY + "\" width=\"312\" height=\"416\" fill=\"#17202c\" stroke=\"#71839a\"/>");
            foreach (Sv5JumpRecipeOccupancy row in recipe.Occupancy)
            {
                int x = originX + row.Point.X * cell;
                int y = originY + (31 - row.Point.Y) * cell;
                string fill = row.SupportKind == "ONE_WAY" ? "#2f8f83" : row.Source == "OUTLINE_BACKING" ? "#334155" : "#53677d";
                text.AppendLine("<rect x=\"" + x + "\" y=\"" + y + "\" width=\"" + cell + "\" height=\"" + cell + "\" fill=\"" + fill + "\" class=\"grid\"/>");
            }
            foreach (Sv5JumpClearanceLinkResult result in plan.Links.Where(value => value.Trace.RecipeId == recipeId))
            {
                string points = string.Join(" ", result.Trace.Samples.Select(sample =>
                    PixelX(originX, cell, sample.Body.X) + "," + PixelY(originY, cell, sample.Body.Y)));
                text.AppendLine("<polyline points=\"" + points + "\" fill=\"none\" stroke=\"#69e6b6\" stroke-width=\"1.5\" opacity=\".8\"/>");
                foreach (Sv5JumpClearanceSample sample in result.Trace.Samples)
                {
                    int bx = PixelX(originX, cell, sample.Body.X);
                    int by = PixelY(originY, cell, sample.Body.Y);
                    int hx = originX + sample.Head.X * cell + 2;
                    int hy = originY + (31 - sample.Head.Y) * cell + 2;
                    text.AppendLine("<rect x=\"" + hx + "\" y=\"" + hy + "\" width=\"9\" height=\"9\" class=\"head\"/>");
                    text.AppendLine("<circle cx=\"" + bx + "\" cy=\"" + by + "\" r=\"3.1\" class=\"trace\"/>");
                }
                if (result.Trace.EndpointAdjusted)
                    text.AppendLine("<circle cx=\"" + PixelX(originX, cell, result.Trace.EffectiveTakeoff.X) + "\" cy=\"" + PixelY(originY, cell, result.Trace.EffectiveTakeoff.Y) + "\" r=\"5\" class=\"adjusted\"/>");
            }
            Sv5JumpClearanceGrabContact grab = plan.GrabContacts.Single(value => value.RecipeId == recipeId);
            text.AppendLine("<circle cx=\"" + PixelX(originX, cell, grab.Contact.X) + "\" cy=\"" + PixelY(originY, cell, grab.Contact.Y) + "\" r=\"5\" class=\"grab\"/>");
            for (int x = 0; x <= 24; x++)
                text.AppendLine("<line x1=\"" + (originX + x * cell) + "\" y1=\"" + originY + "\" x2=\"" + (originX + x * cell) + "\" y2=\"" + (originY + 32 * cell) + "\" class=\"grid\"/>");
            for (int y = 0; y <= 32; y++)
                text.AppendLine("<line x1=\"" + originX + "\" y1=\"" + (originY + y * cell) + "\" x2=\"" + (originX + 24 * cell) + "\" y2=\"" + (originY + y * cell) + "\" class=\"grid\"/>");
        }

        private static int PixelX(int origin, int cell, int x)
        {
            return origin + x * cell + cell / 2;
        }

        private static int PixelY(int origin, int cell, int y)
        {
            return origin + (31 - y) * cell + cell / 2;
        }

        private static string Csv(string header, IEnumerable<string> rows)
        {
            return header + "\n" + string.Join("\n", rows) + "\n";
        }

        private static string Row(params object[] values)
        {
            return string.Join(",", values.Select(value => Quote(value is bool
                ? ((bool)value ? "true" : "false")
                : Convert.ToString(value, CultureInfo.InvariantCulture))));
        }

        private static string Quote(string value)
        {
            value = value ?? string.Empty;
            return value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0 ? value : "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string Json(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n");
        }

        private static void RequirePassing(Sv5JumpClearancePlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (!plan.SweptClearanceReady || plan.Diagnostics.Count != 0)
                throw new InvalidOperationException("SV5_18 clearance proof is invalid: " + string.Join("; ", plan.Diagnostics));
        }
    }
}
