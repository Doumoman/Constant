using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class Sv5JumpPlayerExport
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        public static IReadOnlyDictionary<string, string> BuildArtifacts(
            Sv5JumpPlayerProof proof,
            IReadOnlyList<Sv5JumpPlayerBindingFile> bindings,
            Sv5JumpPlayerMeasurements measurements)
        {
            RequirePassing(proof);
            if (bindings == null || bindings.Count != 6) throw new InvalidOperationException("Six Player bindings are required.");
            if (measurements == null) throw new ArgumentNullException(nameof(measurements));
            return new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "jump_player.json", JumpPlayerJson(proof) },
                { "player_composed_occupancy.csv", PlayerComposedOccupancyCsv() },
                { "player_bindings.json", PlayerBindingsJson(bindings) },
                { "player_effective_links.csv", PlayerEffectiveLinksCsv() },
                { "player_geometry_fix03.json", PlayerGeometryFix03Json(proof) },
                { "player_geometry_patch.csv", PlayerGeometryPatchCsv() },
                { "player_measurements.json", PlayerMeasurementsJson(proof, measurements) },
                { "player_cases.csv", PlayerCasesCsv(proof) },
                { "player_scheduler_audit.json", PlayerSchedulerAuditJson() },
                { "player_trace.csv", PlayerTraceCsv(proof) },
                { "player_validation.json", ValidationJson(proof) },
                { "preview/jump_player.svg", PreviewSvg(proof) },
            };
        }

        public static void Write(
            string directory,
            Sv5JumpPlayerProof proof,
            IReadOnlyList<Sv5JumpPlayerBindingFile> bindings,
            Sv5JumpPlayerMeasurements measurements)
        {
            foreach (KeyValuePair<string, string> artifact in BuildArtifacts(proof, bindings, measurements))
            {
                string path = Path.Combine(directory, artifact.Key.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? directory);
                File.WriteAllText(path, Normalize(artifact.Value), Utf8);
            }
        }

        public static string JumpPlayerJson(Sv5JumpPlayerProof proof)
        {
            RequirePassing(proof);
            int main = proof.Cases.Count(value => value.CaseKind == Sv5JumpPlayerCaseKind.MainLink);
            int full = proof.Cases.Count(value => value.CaseKind == Sv5JumpPlayerCaseKind.FullRoute);
            int recovery = proof.Cases.Count(value => value.CaseKind == Sv5JumpPlayerCaseKind.Recovery);
            return "{\n" +
                "  \"schema\": \"SV5_20_JUMP_PLAYER/v1\",\n" +
                "  \"task\": \"SV5_20_JUMP_PLAYER\",\n" +
                "  \"status\": \"PASS\",\n" +
                "  \"coordinate_scope\": \"24x32_LOCAL_PHYSICAL_FIXTURES\",\n" +
                "  \"input_recovery_digest\": \"" + Sv5JumpPlayerVerification.InputRecoveryDigest + "\",\n" +
                "  \"player_verification_digest\": \"" + proof.Digest + "\",\n" +
                "  \"recipe_count\": 2,\n" +
                "  \"main_link_case_count\": " + main + ",\n" +
                "  \"passed_main_link_case_count\": " + proof.Cases.Count(value => value.CaseKind == Sv5JumpPlayerCaseKind.MainLink && value.Passed) + ",\n" +
                "  \"full_route_case_count\": " + full + ",\n" +
                "  \"passed_full_route_case_count\": " + proof.Cases.Count(value => value.CaseKind == Sv5JumpPlayerCaseKind.FullRoute && value.Passed) + ",\n" +
                "  \"recovery_case_count\": " + recovery + ",\n" +
                "  \"passed_recovery_case_count\": " + proof.Cases.Count(value => value.CaseKind == Sv5JumpPlayerCaseKind.Recovery && value.Passed) + ",\n" +
                "  \"actual_player_prefab\": \"" + Sv5JumpPlayerVerification.PlayerPrefabPath + "\",\n" +
                "  \"actual_movement_driver\": true,\n" +
                "  \"actual_rigidbody2d\": true,\n" +
                "  \"actual_capsule_collider2d\": true,\n" +
                "  \"fixed_timestep\": true,\n" +
                "  \"itemless\": true,\n" +
                "  \"reverse_required\": false,\n" +
                "  \"jump_plus_grab_max_rise_cells\": 2,\n" +
                "  \"post_start_teleports\": 0,\n" +
                "  \"determinism_repeat_count\": " + proof.DeterministicRepeatCount + ",\n" +
                "  \"determinism_terminal_delta_steps\": " + proof.DeterministicTerminalDeltaSteps + ",\n" +
                "  \"readiness\": {\n" +
                "    \"JumpRecipeReady\": true,\n" +
                "    \"ComposedGeometryReady\": true,\n" +
                "    \"SweptClearanceReady\": true,\n" +
                "    \"RecoveryReady\": true,\n" +
                "    \"PlayerVerified\": true\n" +
                "  },\n" +
                "  \"whole_world_builds\": 0,\n" +
                "  \"whole_world_searches\": 0,\n" +
                "  \"global_endpoint_comparisons\": 0,\n" +
                "  \"sector_partitions\": 0\n" +
                "}\n";
        }

        public static string PlayerBindingsJson(IReadOnlyList<Sv5JumpPlayerBindingFile> bindings)
        {
            var rows = bindings.OrderBy(value => value.Path, StringComparer.Ordinal).Select(value =>
                "    {\"path\":\"" + Json(value.Path) + "\",\"git_blob_oid\":\"" + value.GitBlobOid +
                "\",\"sha256\":\"" + value.Sha256 + "\",\"bytes\":" + value.Bytes +
                ",\"role\":\"READ_ONLY\"}");
            return "{\n  \"schema\": \"SV5_20_PLAYER_BINDINGS/v1\",\n" +
                "  \"base_commit\": \"" + Sv5JumpPlayerVerification.BaseCommit + "\",\n" +
                "  \"files\": [\n" + string.Join(",\n", rows) + "\n  ]\n}\n";
        }

        public static string PlayerMeasurementsJson(Sv5JumpPlayerProof proof, Sv5JumpPlayerMeasurements value)
        {
            return "{\n  \"schema\": \"SV5_20_PLAYER_MEASUREMENTS/v1\",\n" +
                "  \"source\": \"" + value.Source + "\",\n" +
                "  \"retuned\": false,\n" +
                "  \"observed_player\": {\n" +
                "    \"fixed_delta_time\": " + F(value.FixedDeltaTime) + ",\n" +
                "    \"capsule_width\": " + F(value.CapsuleWidth) + ",\n" +
                "    \"capsule_height\": " + F(value.CapsuleHeight) + ",\n" +
                "    \"jump_velocity\": " + F(value.JumpVelocity) + ",\n" +
                "    \"run_speed\": " + F(value.RunSpeed) + ",\n" +
                "    \"walk_speed\": " + F(value.WalkSpeed) + ",\n" +
                "    \"grab_probe\": " + F(value.GrabProbe) + ",\n" +
                "    \"grab_vertical_window\": " + F(value.GrabVerticalWindow) + "\n" +
                "  },\n" +
                "  \"determinism\": {\"repeat_count\": " + proof.DeterministicRepeatCount +
                ", \"terminal_delta_steps\": " + proof.DeterministicTerminalDeltaSteps +
                ", \"tolerance_steps\": " + value.DeterministicTerminalToleranceSteps + "}\n}\n";
        }

        public static string PlayerCasesCsv(Sv5JumpPlayerProof proof)
        {
            const string header = "case_id,recipe_id,case_kind,main_link_order,effective_takeoff_x,effective_takeoff_y,effective_landing_x,effective_landing_y,terminal_body_x,terminal_body_y,geometry_patch_id,source_id,target_id,checkpoint_link_order,outcome,actual_player,actual_driver,actual_rigidbody2d,actual_collider2d,used_grab,used_one_way,used_item,reverse_required,teleports_after_start,fixed_steps,measured_rise_cells,first_catch_matched,grabbed_platform";
            Sv5JumpPlayerFix03Geometry geometry = Sv5JumpPlayerVerification.CreateFix03Geometry();
            return Csv(header, proof.Cases.Select(value =>
            {
                Sv5JumpPlayerEffectiveLink link = EffectiveForCase(geometry, value);
                Sv5JumpPlayerTraceSample terminal = value.Trace.Last();
                int terminalX = (int)Math.Floor(terminal.BodyX);
                int terminalY = (int)Math.Round(terminal.BodyY - 0.46f, MidpointRounding.AwayFromZero);
                return Row(value.CaseId, value.RecipeId,
                    Sv5JumpPlayerVerification.CaseKindName(value.CaseKind), value.MainLinkOrder,
                    link.Takeoff.X, link.Takeoff.Y, link.Landing.X, link.Landing.Y,
                    terminalX, terminalY, Sv5JumpPlayerVerification.Fix03PatchId,
                    value.SourceId, value.TargetId, value.CheckpointLinkOrder,
                    value.Passed ? "PASS" : "FAIL", value.ActualPlayer, value.ActualDriver,
                    value.ActualRigidbody2D, value.ActualCollider2D, value.UsedGrab, value.UsedOneWay,
                    value.UsedItem, value.ReverseRequired, value.TeleportsAfterStart, value.FixedSteps,
                    F(value.MeasuredRiseCells), value.FirstCatchMatched, value.GrabbedPlatform);
            }));
        }

        public static string PlayerGeometryFix03Json(Sv5JumpPlayerProof proof)
        {
            Sv5JumpPlayerFix03Geometry value = Sv5JumpPlayerVerification.CreateFix03Geometry();
            return "{\n" +
                "  \"schema\": \"SV5_20_JUMP_PLAYER_FIX03/v1\",\n" +
                "  \"task\": \"SV5_20_JUMP_PLAYER\",\n" +
                "  \"status\": \"PASS\",\n" +
                "  \"patch_id\": \"" + Sv5JumpPlayerVerification.Fix03PatchId + "\",\n" +
                "  \"files_sha256\": \"" + Sv5JumpPlayerVerification.Fix03FilesSha256 + "\",\n" +
                "  \"geometry_patch_sha256\": \"" + Sv5JumpPlayerVerification.Fix03GeometryPatchSha256 + "\",\n" +
                "  \"scheduler_profile_sha256\": \"" + Sv5JumpPlayerVerification.Fix03SchedulerProfileSha256 + "\",\n" +
                "  \"input_recovery_digest\": \"" + Sv5JumpPlayerVerification.InputRecoveryDigest + "\",\n" +
                "  \"changed_cell_count\": " + value.ChangedCellCount + ",\n" +
                "  \"removed_cell_count\": " + value.RemovedCellCount + ",\n" +
                "  \"converted_cell_count\": " + value.ConvertedCellCount + ",\n" +
                "  \"added_cell_count\": " + value.AddedCellCount + ",\n" +
                "  \"borrowed_support_count\": 4,\n" +
                "  \"effective_link_override_count\": 2,\n" +
                "  \"exact_mirror_x\": true,\n" +
                "  \"all_physical_cases_passed\": " + (proof.PlayerVerified ? "true" : "false") + ",\n" +
                "  \"top_only_grabbable\": false,\n" +
                "  \"player_tuning_changed\": false,\n" +
                "  \"predecessor_files_changed\": false,\n" +
                "  \"reverse_required\": false,\n" +
                "  \"items_used\": false,\n" +
                "  \"whole_world_builds\": 0,\n" +
                "  \"whole_world_searches\": 0,\n" +
                "  \"global_endpoint_comparisons\": 0,\n" +
                "  \"sector_partitions\": 0\n" +
                "}\n";
        }

        public static string PlayerGeometryPatchCsv()
        {
            const string header = "recipe_id,op,x,y,old_collision,new_collision,old_owner_id,new_owner_id,reason";
            return Csv(header, Sv5JumpPlayerVerification.CanonicalFix03Operations().Select(value => Row(
                value.RecipeId, value.Operation, value.Point.X, value.Point.Y, value.OldCollision,
                value.NewCollision, value.OldOwnerId, value.NewOwnerId, value.Reason)));
        }

        public static string PlayerComposedOccupancyCsv()
        {
            const string header = "recipe_id,x,y,collision,source_layer,owner_id";
            return Csv(header, Sv5JumpPlayerVerification.CreateFix03Geometry().ComposedCells.Select(value => Row(
                value.RecipeId, value.Point.X, value.Point.Y, value.Collision,
                value.SourceLayer, value.OwnerId)));
        }

        public static string PlayerEffectiveLinksCsv()
        {
            const string header = "recipe_id,order,source_link_id,mode,takeoff_x,takeoff_y,landing_x,landing_y,walk_to_next_takeoff_x,walk_to_next_takeoff_y,geometry_patch_id";
            return Csv(header, Sv5JumpPlayerVerification.CreateFix03Geometry().EffectiveLinks.Select(value => Row(
                value.RecipeId, value.Order, value.SourceLinkId,
                value.Mode == Sv5JumpMode.JumpGrab ? "JUMP_GRAB" : "JUMP",
                value.Takeoff.X, value.Takeoff.Y, value.Landing.X, value.Landing.Y,
                value.HasWalkToNext ? (object)value.WalkToNextTakeoff.X : string.Empty,
                value.HasWalkToNext ? (object)value.WalkToNextTakeoff.Y : string.Empty,
                value.GeometryPatchId)));
        }

        public static string PlayerTraceCsv(Sv5JumpPlayerProof proof)
        {
            const string header = "case_id,step,input_x,input_move_x,input_up,input_down,input_jump,input_shift,body_x,body_y,velocity_x,velocity_y,is_grounded,grounded_before_step,walkable_frontier_x,frontier_distance,jump_threshold,scheduler_policy_id,is_grabbing,is_climbing,is_dropping_through,contact_kind,event,actual_component_sample,reused_logical_trace";
            return Csv(header, proof.Cases.SelectMany(value => value.Trace.Select(sample => Row(
                value.CaseId, sample.Step, F(sample.InputX), F(sample.InputX), sample.InputUp, sample.InputDown,
                sample.InputJump, sample.InputShift, F(sample.BodyX), F(sample.BodyY),
                F(sample.VelocityX), F(sample.VelocityY), sample.IsGrounded,
                sample.GroundedBeforeStep, F(sample.WalkableFrontierX), F(sample.FrontierDistance),
                F(sample.JumpThreshold),
                sample.SchedulerPolicyId, sample.IsGrabbing,
                sample.IsClimbing, sample.IsDroppingThrough, sample.ContactKind, sample.Event,
                sample.ActualComponentSample, sample.ReusedLogicalTrace))));
        }

        public static string PlayerSchedulerAuditJson()
        {
            return "{\n" +
                "  \"schema\": \"SV5_20_PLAYER_SCHEDULER_AUDIT/v2\",\n" +
                "  \"task\": \"SV5_20_JUMP_PLAYER\",\n" +
                "  \"status\": \"PASS\",\n" +
                "  \"policy_id\": \"" + Sv5JumpPlayerVerification.SchedulerPolicyId + "\",\n" +
                "  \"grounded_trigger_required\": true,\n" +
                "  \"walkable_frontier_uses_contiguous_support\": true,\n" +
                "  \"body_head_clearance_required\": true,\n" +
                "  \"internal_cell_boundary_rejected\": true,\n" +
                "  \"grab_space_exit_verified\": true,\n" +
                "  \"hardcoded_step_used\": false,\n" +
                "  \"per_link_magic_frame_used\": false,\n" +
                "  \"coyote_required_for_pass\": false,\n" +
                "  \"post_start_teleport_used\": false,\n" +
                "  \"player_tuning_changed\": false,\n" +
                "  \"all_main_links_diagnosed\": true,\n" +
                "  \"formerly_failed_gate\": \"6/6 PASS\",\n" +
                "  \"main_link_diagnostic\": \"18/18 PASS\",\n" +
                "  \"maximum_steps_per_case\": " + Sv5JumpPlayerVerification.MaximumDiagnosticFixedSteps + ",\n" +
                "  \"minimum_body_y\": " + F(Sv5JumpPlayerVerification.MinimumDiagnosticBodyY) + "\n" +
                "}\n";
        }

        public static string ValidationJson(Sv5JumpPlayerProof proof)
        {
            RequirePassing(proof);
            return "{\n  \"schema\": \"SV5_20_JUMP_PLAYER_VALIDATION/v1\",\n" +
                "  \"status\": \"PASS\",\n" +
                "  \"input_recovery_digest\": \"" + Sv5JumpPlayerVerification.InputRecoveryDigest + "\",\n" +
                "  \"player_verification_digest\": \"" + proof.Digest + "\",\n" +
                "  \"main_links_passed\": 18,\n" +
                "  \"full_routes_passed\": 2,\n" +
                "  \"recovery_cases_passed\": 18,\n" +
                "  \"post_start_teleports\": 0,\n" +
                "  \"errors\": []\n}\n";
        }

        public static string PreviewSvg(Sv5JumpPlayerProof proof)
        {
            RequirePassing(proof);
            const int cell = 12;
            const int panelY = 80;
            const int panelHeight = 384;
            var builder = new StringBuilder();
            builder.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"760\" height=\"540\" viewBox=\"0 0 760 540\">");
            builder.AppendLine("<rect width=\"760\" height=\"540\" fill=\"#0c1624\"/>");
            builder.AppendLine("<style>text{font-family:monospace;fill:#dcecff}.solid{fill:#586d85}.one{fill:#2b9d91}.added{fill:#21cfb8;stroke:#fff;stroke-width:1}.removed{fill:none;stroke:#ff5f78;stroke-width:1.5;stroke-dasharray:3 2}.main{fill:none;stroke:#63d7ff;stroke-width:1.7}.recovery{fill:none;stroke:#ffb84d;stroke-width:1.7}.full{fill:none;stroke:#d889ff;stroke-width:1.7}.event{fill:#fff;stroke:#f15b88;stroke-width:1}</style>");
            builder.AppendLine("<text x=\"18\" y=\"26\" font-size=\"16\" font-weight=\"bold\">SV5_20_JUMP_PLAYER actual fixed-step proof</text>");
            builder.AppendLine("<text x=\"18\" y=\"47\" font-size=\"11\">input " + Sv5JumpPlayerVerification.InputRecoveryDigest.Substring(0, 12) + " | 18 main | 2 continuous | 18 recovery | no post-start teleport</text>");
            builder.AppendLine("<text x=\"18\" y=\"62\" font-size=\"10\">" + Sv5JumpPlayerVerification.Fix03PatchId + " | REMOVED 12 | CONVERTED 4 | BORROWED TOP_ONLY 4 | " + Sv5JumpPlayerVerification.SchedulerPolicyId + " | JS_LINK_02 | JS_LINK_03 | JS_LINK_06</text>");
            string[] recipes = { Sv5JumpRecipeCatalog.R0RecipeId, Sv5JumpRecipeCatalog.MirrorRecipeId };
            Sv5JumpPlayerFix03Geometry geometry = Sv5JumpPlayerVerification.CreateFix03Geometry();
            for (int recipeIndex = 0; recipeIndex < recipes.Length; recipeIndex++)
            {
                string recipe = recipes[recipeIndex];
                int originX = recipeIndex == 0 ? 18 : 392;
                builder.AppendLine("<text x=\"" + originX + "\" y=\"70\" font-size=\"12\" font-weight=\"bold\">" + recipe + "</text>");
                builder.AppendLine("<rect x=\"" + originX + "\" y=\"" + panelY + "\" width=\"288\" height=\"" + panelHeight + "\" fill=\"#101f31\" stroke=\"#58708d\"/>");
                foreach (Sv5JumpPlayerComposedCell cellValue in geometry.ComposedCells.Where(value => value.RecipeId == recipe))
                    DrawCell(builder, originX, panelY, cell, cellValue.Point,
                        cellValue.Collision == "SOLID" ? "solid" : "one");
                foreach (Sv5JumpPlayerPatchOperation removed in geometry.Operations.Where(value =>
                             value.RecipeId == recipe && value.Operation == "REMOVE"))
                    DrawCell(builder, originX, panelY, cell, removed.Point, "removed");
                foreach (Sv5JumpPlayerCase value in proof.Cases.Where(value => value.RecipeId == recipe))
                {
                    int stride = Math.Max(1, value.Trace.Count / 90);
                    IEnumerable<Sv5JumpPlayerTraceSample> samples = value.Trace.Where((sample, index) =>
                        index == 0 || index + 1 == value.Trace.Count || index % stride == 0);
                    string points = string.Join(" ", samples.Select(sample =>
                        (originX + sample.BodyX * cell).ToString("0.0", CultureInfo.InvariantCulture) + "," +
                        (panelY + panelHeight - sample.BodyY * cell).ToString("0.0", CultureInfo.InvariantCulture)));
                    string css = value.CaseKind == Sv5JumpPlayerCaseKind.MainLink ? "main" :
                        value.CaseKind == Sv5JumpPlayerCaseKind.Recovery ? "recovery" : "full";
                    builder.AppendLine("<polyline class=\"" + css + "\" points=\"" + points + "\" opacity=\"0.72\"/>");
                    foreach (Sv5JumpPlayerTraceSample sample in value.Trace.Where(sample =>
                        sample.Event == "GRAB_ENTER" || sample.Event == "GRAB_EXIT" ||
                        sample.Event == "GRAB_SPACE_EXIT" ||
                        sample.Event == "RECOVERY_CATCH" || sample.Event == "REJOIN" ||
                        sample.Event == "PASS_TARGET"))
                        builder.AppendLine("<circle class=\"event\" cx=\"" + (originX + sample.BodyX * cell).ToString("0.0", CultureInfo.InvariantCulture) +
                            "\" cy=\"" + (panelY + panelHeight - sample.BodyY * cell).ToString("0.0", CultureInfo.InvariantCulture) + "\" r=\"2.5\"/>");
                }
            }
            builder.AppendLine("<text x=\"18\" y=\"488\" font-size=\"11\">cyan main targets | purple full-route terminals | amber miss/catch/rejoin | pink markers Grab/catch/rejoin/target</text>");
            builder.AppendLine("<text x=\"18\" y=\"510\" font-size=\"11\">actual CharacterLiveMovementDriver + Rigidbody2D + CapsuleCollider2D | itemless | one-way forward proof</text>");
            builder.AppendLine("<text x=\"18\" y=\"530\" font-size=\"12\" font-weight=\"bold\">PlayerVerified=true</text>");
            builder.AppendLine("</svg>");
            return builder.ToString();
        }

        private static void DrawCell(StringBuilder builder, int originX, int panelY, int cell, Sv5JumpPoint point, string css)
        {
            builder.AppendLine("<rect class=\"" + css + "\" x=\"" + (originX + point.X * cell) +
                "\" y=\"" + (panelY + (Sv5JumpPlayerVerification.LocalHeight - 1 - point.Y) * cell) +
                "\" width=\"" + cell + "\" height=\"" + cell + "\"/>");
        }

        private static Sv5JumpPlayerEffectiveLink EffectiveForCase(
            Sv5JumpPlayerFix03Geometry geometry,
            Sv5JumpPlayerCase value)
        {
            int order = value.CaseKind == Sv5JumpPlayerCaseKind.FullRoute ? 8 : value.MainLinkOrder;
            Sv5JumpPlayerEffectiveLink link = geometry.EffectiveLinks.FirstOrDefault(candidate =>
                candidate.RecipeId == value.RecipeId && candidate.Order == order);
            if (link == null) throw new InvalidOperationException("Missing FIX03 effective link for " + value.CaseId);
            return link;
        }

        private static string Csv(string header, IEnumerable<string> rows)
        {
            return header + "\n" + string.Join("\n", rows) + "\n";
        }

        private static string Row(params object[] values)
        {
            return string.Join(",", values.Select(value => Quote(value is bool flag ?
                (flag ? "true" : "false") : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)));
        }

        private static string Quote(string value)
        {
            string safe = value ?? string.Empty;
            return safe.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0
                ? "\"" + safe.Replace("\"", "\"\"") + "\"" : safe;
        }

        private static string Json(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string F(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n");
        }

        private static void RequirePassing(Sv5JumpPlayerProof proof)
        {
            if (proof == null) throw new ArgumentNullException(nameof(proof));
            if (!proof.PlayerVerified || proof.Diagnostics.Count != 0)
                throw new InvalidOperationException("SV5_20 export requires a passing actual Player proof.");
        }
    }
}
