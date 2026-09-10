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
            Write(Path.Combine(directory, "gate_geometry.json"), GateGeometryJson(plan));
            Write(Path.Combine(directory, "gate_state_checks.json"), GateStateChecksJson(plan));
            Write(Path.Combine(directory, "physical_contact_checks.json"), PhysicalContactChecksJson(plan));
            Write(Path.Combine(directory, "physical_gate_state_checks.json"), PhysicalGateStateChecksJson(plan));
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
                .Append("  \"physical_movement_digest\": ").Append(J(plan.PhysicalMovement.SemanticDigest)).Append(",\n")
                .Append("  \"readiness\": {\"planned_layout\":true,\"logical_state\":true,\"contact_coverage\":true,\"global_coordinate_movement\":true,\"planned_gate_geometry\":true,\"planned_gate_state\":true,\"composed_geometry\":false,\"player\":false},\n")
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
