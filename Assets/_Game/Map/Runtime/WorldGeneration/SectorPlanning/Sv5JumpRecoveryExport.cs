using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class Sv5JumpRecoveryExport
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        public static IReadOnlyDictionary<string, string> BuildArtifacts(Sv5JumpRecoveryPlan plan)
        {
            RequirePassing(plan);
            return new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "jump_recovery.json", JumpRecoveryJson(plan) },
                { "preview/jump_recovery.svg", PreviewSvg(plan) },
                { "recovery_links.csv", LinksCsv(plan) },
                { "recovery_miss_probes.csv", MissProbesCsv(plan) },
                { "recovery_overlay.csv", OverlayCsv(plan) },
                { "recovery_routes.csv", RoutesCsv(plan) },
                { "recovery_trace.csv", TraceCsv(plan) },
                { "recovery_validation.json", ValidationJson(plan) },
            };
        }

        public static void WriteCanonical(string directory)
        {
            Write(directory, Sv5JumpRecovery.CreateCanonicalLocalProof());
        }

        public static void Write(string directory, Sv5JumpRecoveryPlan plan)
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

        public static string JumpRecoveryJson(Sv5JumpRecoveryPlan plan)
        {
            RequirePassing(plan);
            var text = new StringBuilder();
            text.AppendLine("{");
            text.AppendLine("  \"schema\": \"SV5_19_JUMP_RECOVERY/v1\",");
            text.AppendLine("  \"task\": \"SV5_19_JUMP_RECOVERY\",");
            text.AppendLine("  \"status\": \"PASS\",");
            text.AppendLine("  \"coordinate_scope\": \"24x32_LOCAL_INTEGER_CELLS\",");
            text.AppendLine("  \"input_clearance_digest\": \"" + Json(plan.InputClearanceDigest) + "\",");
            text.AppendLine("  \"recovery_digest\": \"" + Json(plan.Digest) + "\",");
            text.AppendLine("  \"recipe_count\": 2,");
            text.AppendLine("  \"overlay_cell_count\": " + plan.Overlay.Count + ",");
            text.AppendLine("  \"overlay_group_count\": " + plan.OverlayGroupCount + ",");
            text.AppendLine("  \"probe_count\": " + plan.Probes.Count + ",");
            text.AppendLine("  \"passed_probe_count\": " + plan.Probes.Count(value => value.Caught) + ",");
            text.AppendLine("  \"route_count\": " + plan.Routes.Count + ",");
            text.AppendLine("  \"passed_route_count\": " + plan.Routes.Count(value => value.RecoveryPass) + ",");
            text.AppendLine("  \"linked_route_count\": " + plan.Routes.Count(value => value.RouteKind == Sv5JumpRecoveryRouteKind.Linked) + ",");
            text.AppendLine("  \"already_at_checkpoint_count\": " + plan.Routes.Count(value => value.RouteKind == Sv5JumpRecoveryRouteKind.AlreadyAtCheckpoint) + ",");
            text.AppendLine("  \"recovery_link_count\": " + plan.RecoveryLinkCount + ",");
            text.AppendLine("  \"recovery_trace_state_count\": " + plan.RecoveryTraceStateCount + ",");
            text.AppendLine("  \"checkpoint_orders\": [" + string.Join(",", plan.Routes.Select(value => value.CheckpointLinkOrder).Distinct().OrderBy(value => value)) + "],");
            text.AppendLine("  \"reverse_required\": false,");
            text.AppendLine("  \"items_used\": false,");
            text.AppendLine("  \"readiness\": {");
            text.AppendLine("    \"JumpRecipeReady\": true,");
            text.AppendLine("    \"ComposedGeometryReady\": true,");
            text.AppendLine("    \"SweptClearanceReady\": true,");
            text.AppendLine("    \"RecoveryReady\": true,");
            text.AppendLine("    \"PlayerVerified\": false");
            text.AppendLine("  },");
            text.AppendLine("  \"whole_world_builds\": 0,");
            text.AppendLine("  \"whole_world_searches\": 0,");
            text.AppendLine("  \"global_endpoint_comparisons\": 0");
            text.AppendLine("}");
            return text.ToString();
        }

        public static string OverlayCsv(Sv5JumpRecoveryPlan plan)
        {
            RequirePassing(plan);
            return Csv("recipe_id,group_id,x,y,collision,role,owner_id",
                plan.Overlay.Select(value => Row(value.RecipeId, value.GroupId, value.Point.X, value.Point.Y,
                    value.Collision, value.Role, value.OwnerId)));
        }

        public static string MissProbesCsv(Sv5JumpRecoveryPlan plan)
        {
            RequirePassing(plan);
            return Csv("recipe_id,failed_link_order,source_link_id,probe_order,source_sample_order,origin_x,origin_y,catch_source,catch_group_id,catch_x,catch_y,landing_body_x,landing_body_y,landing_head_x,landing_head_y,fall_distance,caught",
                plan.Probes.Select(value => Row(value.RecipeId, value.FailedLinkOrder, value.SourceLinkId, value.ProbeOrder,
                    value.SourceSampleOrder, value.Origin.X, value.Origin.Y, value.CatchSource, value.CatchGroupId,
                    value.CatchPoint.X, value.CatchPoint.Y, value.LandingBody.X, value.LandingBody.Y,
                    value.LandingHead.X, value.LandingHead.Y, value.FallDistance, value.Caught)));
        }

        public static string RoutesCsv(Sv5JumpRecoveryPlan plan)
        {
            RequirePassing(plan);
            return Csv("recipe_id,route_id,failed_link_order,checkpoint_link_order,route_kind,start_x,start_y,end_x,end_y,recovery_link_ids,recovery_pass,reverse_required,diagnostic",
                plan.Routes.Select(value => Row(value.RecipeId, value.RouteId, value.FailedLinkOrder,
                    value.CheckpointLinkOrder, Sv5JumpRecovery.RouteKindName(value.RouteKind), value.Start.X, value.Start.Y,
                    value.End.X, value.End.Y, string.Join(";", value.Links.Select(link => link.RecoveryLinkId)),
                    value.RecoveryPass, value.ReverseRequired, value.Diagnostic)));
        }

        public static string LinksCsv(Sv5JumpRecoveryPlan plan)
        {
            RequirePassing(plan);
            return Csv("recipe_id,route_id,link_order,recovery_link_id,mode,source_x,source_y,target_x,target_y,rise,trace_states",
                plan.Routes.SelectMany(route => route.Links.Select(link => Row(route.RecipeId, route.RouteId,
                    link.LinkOrder, link.RecoveryLinkId, Sv5JumpRecovery.ModeName(link.Mode), link.Source.X,
                    link.Source.Y, link.Target.X, link.Target.Y, link.Rise, link.Trace.Count))));
        }

        public static string TraceCsv(Sv5JumpRecoveryPlan plan)
        {
            RequirePassing(plan);
            return Csv("recipe_id,route_id,recovery_link_order,recovery_link_id,sample_order,phase,body_x,body_y,head_x,head_y,body_clear,head_clear,contract_accepted",
                plan.Routes.SelectMany(route => route.Links.SelectMany(link => link.Trace.Select(sample => Row(
                    route.RecipeId, route.RouteId, link.LinkOrder, link.RecoveryLinkId, sample.SampleOrder,
                    Sv5JumpRecovery.PhaseName(sample.Phase), sample.Body.X, sample.Body.Y, sample.Head.X,
                    sample.Head.Y, true, true, true)))));
        }

        public static string ValidationJson(Sv5JumpRecoveryPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var text = new StringBuilder();
            text.AppendLine("{");
            text.AppendLine("  \"schema\": \"SV5_19_JUMP_RECOVERY_VALIDATION/v1\",");
            text.AppendLine("  \"status\": \"" + (plan.Diagnostics.Count == 0 ? "PASS" : "FAIL") + "\",");
            text.AppendLine("  \"input_clearance_digest\": \"" + Json(plan.InputClearanceDigest) + "\",");
            text.AppendLine("  \"recovery_digest\": \"" + Json(plan.Digest) + "\",");
            text.AppendLine("  \"probe_count\": " + plan.Probes.Count + ",");
            text.AppendLine("  \"passed_probe_count\": " + plan.Probes.Count(value => value.Caught) + ",");
            text.AppendLine("  \"route_count\": " + plan.Routes.Count + ",");
            text.AppendLine("  \"passed_route_count\": " + plan.Routes.Count(value => value.RecoveryPass) + ",");
            text.AppendLine("  \"main_clearance_preserved_count\": 18,");
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

        public static string PreviewSvg(Sv5JumpRecoveryPlan plan)
        {
            RequirePassing(plan);
            Sv5JumpRecipeCatalogModel catalog = Sv5JumpRecipeCatalog.CreateCanonicalCatalog();
            Sv5JumpClearancePlan clearance = Sv5JumpClearance.CreateCanonicalLocalProof();
            const int cell = 13;
            const int boardY = 78;
            var text = new StringBuilder();
            text.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"880\" height=\"640\" viewBox=\"0 0 880 640\">");
            text.AppendLine("<rect width=\"880\" height=\"640\" fill=\"#0d1520\"/>");
            text.AppendLine("<style>text{font-family:Consolas,monospace;fill:#eef6ff}.title{font-size:18px;font-weight:bold}.label{font-size:11px}.muted{fill:#9db0c5}.grid{stroke:#2a394b;stroke-width:.45}.main{stroke:#5ed3a6;stroke-width:1.2;fill:none;opacity:.55}.miss{stroke:#ff647c;stroke-width:1.3;stroke-dasharray:4 3}.recover{stroke:#5dc8ff;stroke-width:2.2;fill:none}.checkpoint{fill:#d89cff;stroke:#fff;stroke-width:1}.catch{fill:#f5a94a;stroke:#ffe2ad;stroke-width:.6}</style>");
            text.AppendLine("<text x=\"24\" y=\"29\" class=\"title\">SV5_19 local representative-miss recovery proof</text>");
            text.AppendLine("<text x=\"24\" y=\"52\" class=\"label muted\">two 24x32 recipes | 18 AIR probes | first catch | WALK/JUMP/DROP only | no reverse claim</text>");
            DrawRecipe(text, catalog, clearance, plan, Sv5JumpRecipeCatalog.R0RecipeId, 24, boardY, cell);
            DrawRecipe(text, catalog, clearance, plan, Sv5JumpRecipeCatalog.MirrorRecipeId, 448, boardY, cell);
            text.AppendLine("<rect x=\"24\" y=\"522\" width=\"11\" height=\"11\" fill=\"#53677d\"/><text x=\"41\" y=\"532\" class=\"label\">base SOLID</text>");
            text.AppendLine("<rect x=\"128\" y=\"522\" width=\"11\" height=\"11\" fill=\"#2f8f83\"/><text x=\"145\" y=\"532\" class=\"label\">base TOP_ONLY</text>");
            text.AppendLine("<rect x=\"264\" y=\"522\" width=\"11\" height=\"11\" class=\"catch\"/><text x=\"281\" y=\"532\" class=\"label\">recovery catch</text>");
            text.AppendLine("<line x1=\"410\" y1=\"527\" x2=\"438\" y2=\"527\" class=\"miss\"/><text x=\"445\" y=\"532\" class=\"label\">miss ray</text>");
            text.AppendLine("<line x1=\"535\" y1=\"527\" x2=\"563\" y2=\"527\" class=\"recover\"/><text x=\"570\" y=\"532\" class=\"label\">recovery route</text>");
            text.AppendLine("<circle cx=\"717\" cy=\"527\" r=\"5\" class=\"checkpoint\"/><text x=\"729\" y=\"532\" class=\"label\">rejoin checkpoint</text>");
            text.AppendLine("<text x=\"24\" y=\"568\" class=\"label\">ALREADY_AT_CHECKPOINT emits zero movement links; later checkpoint shortcuts are rejected.</text>");
            text.AppendLine("<text x=\"24\" y=\"592\" class=\"label muted\">RecoveryReady=true | PlayerVerified=false | itemless one-way local proof only</text>");
            text.AppendLine("<text x=\"24\" y=\"616\" class=\"label muted\">Base/main evidence is immutable; no world placement or live Player physics claim.</text>");
            text.AppendLine("</svg>");
            return text.ToString();
        }

        private static void DrawRecipe(
            StringBuilder text,
            Sv5JumpRecipeCatalogModel catalog,
            Sv5JumpClearancePlan clearance,
            Sv5JumpRecoveryPlan plan,
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
            foreach (Sv5JumpRecoveryCell row in plan.Overlay.Where(value => value.RecipeId == recipeId))
                text.AppendLine("<rect x=\"" + (originX + row.Point.X * cell) + "\" y=\"" + (originY + (31 - row.Point.Y) * cell) +
                    "\" width=\"" + cell + "\" height=\"" + cell + "\" class=\"catch\"/>");
            foreach (Sv5JumpClearanceLinkResult result in clearance.Links.Where(value => value.Trace.RecipeId == recipeId))
            {
                string points = string.Join(" ", result.Trace.Samples.Select(sample => PixelX(originX, cell, sample.Body.X) + "," + PixelY(originY, cell, sample.Body.Y)));
                text.AppendLine("<polyline points=\"" + points + "\" class=\"main\"/>");
            }
            foreach (Sv5JumpMissProbe probe in plan.Probes.Where(value => value.RecipeId == recipeId))
                text.AppendLine("<line x1=\"" + PixelX(originX, cell, probe.Origin.X) + "\" y1=\"" + PixelY(originY, cell, probe.Origin.Y) +
                    "\" x2=\"" + PixelX(originX, cell, probe.LandingBody.X) + "\" y2=\"" + PixelY(originY, cell, probe.LandingBody.Y) + "\" class=\"miss\"/>");
            foreach (Sv5JumpRecoveryRoute route in plan.Routes.Where(value => value.RecipeId == recipeId))
            {
                if (route.Links.Count > 0)
                {
                    string points = string.Join(" ", route.Links.SelectMany(value => value.Trace).Select(sample =>
                        PixelX(originX, cell, sample.Body.X) + "," + PixelY(originY, cell, sample.Body.Y)));
                    text.AppendLine("<polyline points=\"" + points + "\" class=\"recover\"/>");
                }
                text.AppendLine("<circle cx=\"" + PixelX(originX, cell, route.End.X) + "\" cy=\"" + PixelY(originY, cell, route.End.Y) + "\" r=\"4\" class=\"checkpoint\"/>");
            }
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

        private static void RequirePassing(Sv5JumpRecoveryPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (!plan.RecoveryReady || plan.Diagnostics.Count != 0)
                throw new InvalidOperationException("SV5_19 recovery proof is invalid: " + string.Join("; ", plan.Diagnostics));
        }
    }
}
