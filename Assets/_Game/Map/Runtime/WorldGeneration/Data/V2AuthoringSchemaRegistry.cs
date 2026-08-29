using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class V2AuthoringSchemaRegistry
    {
        private static readonly ReadOnlyCollection<V2AuthoringTableDescriptor> DefaultTables =
            new ReadOnlyCollection<V2AuthoringTableDescriptor>(CreateDefaultTables()
                .OrderBy(value => value.RelativeAuthoringPath, StringComparer.Ordinal)
                .ToList());

        private readonly ReadOnlyCollection<V2AuthoringTableDescriptor> tables;
        private readonly IReadOnlyDictionary<string, V2AuthoringTableDescriptor> tablesByPath;

        internal V2AuthoringSchemaRegistry(
            IEnumerable<V2AuthoringTableDescriptor> sourceTables,
            V2AuthoringForeignKeyIndex foreignKeyIndex,
            string canonicalDigest)
        {
            var ordered = sourceTables
                .OrderBy(value => value.RelativeAuthoringPath, StringComparer.Ordinal)
                .ToList();
            tables = new ReadOnlyCollection<V2AuthoringTableDescriptor>(ordered);
            tablesByPath = new ReadOnlyDictionary<string, V2AuthoringTableDescriptor>(
                ordered.ToDictionary(value => value.RelativeAuthoringPath, StringComparer.Ordinal));
            ForeignKeyIndex = foreignKeyIndex ?? throw new ArgumentNullException(nameof(foreignKeyIndex));
            CanonicalDigest = canonicalDigest ?? throw new ArgumentNullException(nameof(canonicalDigest));
        }

        public IReadOnlyList<V2AuthoringTableDescriptor> Tables => tables;
        public V2AuthoringForeignKeyIndex ForeignKeyIndex { get; }
        public string CanonicalDigest { get; }

        public static IReadOnlyList<V2AuthoringTableDescriptor> DescribeDefaultTables()
        {
            return DefaultTables;
        }

        public static V2AuthoringSchemaValidationResult CreateDefault(CsvSchemaCatalog legacyCatalog)
        {
            return V2AuthoringSchemaValidator.Validate(DefaultTables, legacyCatalog);
        }

        public bool TryGetTable(string relativeAuthoringPath, out V2AuthoringTableDescriptor table)
        {
            return tablesByPath.TryGetValue(relativeAuthoringPath, out table);
        }

        private static V2AuthoringTableDescriptor[] CreateDefaultTables()
        {
            return new[]
            {
                Table("micro_pattern_catalog_v2", V2AuthoringOwner.MicroPattern,
                    "MicroPattern/micro_pattern_catalog_v2.csv",
                    Column(1, "pattern_id", CsvSchemaDataType.Id, true, 1),
                    Column(2, "selection_weight", CsvSchemaDataType.Int, true),
                    Column(3, "biome_ids", CsvSchemaDataType.IdList, true),
                    Column(4, "allowed_transforms", CsvSchemaDataType.EnumList, true, allowed: Values("R0", "MIRROR_X", "MIRROR_Y", "R180")),
                    Column(5, "protected_policy", CsvSchemaDataType.Enum, true, allowed: Values("FORCE_NO_CHANGE", "REJECT_CANDIDATE"))),

                Table("micro_pattern_cells_v2", V2AuthoringOwner.MicroPattern,
                    "MicroPattern/micro_pattern_cells_v2.csv",
                    Column(1, "pattern_id", CsvSchemaDataType.Id, true, 1, foreignKey: V2("micro_pattern_catalog_v2.csv", "pattern_id")),
                    Column(2, "local_x", CsvSchemaDataType.Int, true, 2),
                    Column(3, "local_y", CsvSchemaDataType.Int, true, 3),
                    Column(4, "operation", CsvSchemaDataType.Enum, true, allowed: Values("NO_CHANGE", "ADD_SOLID", "CARVE_AIR", "SET_SURFACE", "SET_AFFORDANCE", "SET_MATERIAL", "SET_HAZARD", "SET_MARKER")),
                    Column(5, "layer", CsvSchemaDataType.Enum, true, allowed: Values("GEOMETRY", "SURFACE", "AFFORDANCE", "MATERIAL", "HAZARD", "MARKER")),
                    Column(6, "payload_id", CsvSchemaDataType.Id, false)),

                Table("terrain_cluster_catalog_v2", V2AuthoringOwner.TerrainCluster,
                    "TerrainCluster/terrain_cluster_catalog_v2.csv",
                    Column(1, "cluster_id", CsvSchemaDataType.Id, true, 1),
                    Column(2, "pacing_role", CsvSchemaDataType.Enum, true, allowed: Values("QUIET", "TRAVERSAL", "DISCOVERY", "RISK", "RECOVERY", "SAFE", "MACHINERY", "FLOW", "ACTIVITY", "NARRATIVE", "REWARD", "LANDMARK", "RESOURCE", "BOSS", "INTEGRATED")),
                    Column(3, "biome_id", CsvSchemaDataType.Id, true),
                    Column(4, "footprint_variant_id", CsvSchemaDataType.Id, true),
                    Column(5, "spine_variant_id", CsvSchemaDataType.Id, true)),

                Table("terrain_cluster_cells_v2", V2AuthoringOwner.TerrainCluster,
                    "TerrainCluster/terrain_cluster_cells_v2.csv",
                    Column(1, "cluster_id", CsvSchemaDataType.Id, true, 1, foreignKey: V2("terrain_cluster_catalog_v2.csv", "cluster_id")),
                    Column(2, "chunk_x", CsvSchemaDataType.Int, true, 2),
                    Column(3, "chunk_y", CsvSchemaDataType.Int, true, 3),
                    Column(4, "cell_role", CsvSchemaDataType.Enum, false, allowed: Values("ENTRY", "BUILD_UP", "CORE", "RECOVERY", "REWARD", "EXIT")),
                    Column(5, "port_id", CsvSchemaDataType.Id, false),
                    Column(6, "access_class", CsvSchemaDataType.Enum, false, allowed: Values("MANDATORY_NO_TOOL", "OPTIONAL_NO_TOOL", "OPTIONAL_TOOL", "OPTIONAL_ENVIRONMENT", "OPTIONAL_EXPLOSIVE", "OPTIONAL_HIDDEN", "PROGRESSION_GATE")),
                    Column(7, "source_microchunk_id", CsvSchemaDataType.Id, false, foreignKey: Legacy("microchunk_catalog.csv", "microchunk_id")),
                    Column(8, "source_boundary_chunk_id", CsvSchemaDataType.Id, false, foreignKey: Legacy("boundary_chunk_catalog.csv", "boundary_chunk_id"))),

                Table("terrain_cluster_variants_v2", V2AuthoringOwner.TerrainCluster,
                    "TerrainCluster/terrain_cluster_variants_v2.csv",
                    Column(1, "cluster_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_catalog_v2.csv", "cluster_id")),
                    Column(2, "spine_variant_id", CsvSchemaDataType.Id, true, 1),
                    Column(3, "graph_kind", CsvSchemaDataType.Enum, true, allowed: Values("TRAVERSAL"))),

                Table("terrain_cluster_role_anchors_v2", V2AuthoringOwner.TerrainCluster,
                    "TerrainCluster/terrain_cluster_role_anchors_v2.csv",
                    Column(1, "cluster_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_catalog_v2.csv", "cluster_id")),
                    Column(2, "role_anchor_id", CsvSchemaDataType.Id, true, 1),
                    Column(3, "role_kind", CsvSchemaDataType.Enum, true, allowed: Values("ENTRY", "BUILD_UP", "CORE", "RECOVERY", "REWARD", "EXIT")),
                    Column(4, "local_x", CsvSchemaDataType.Int, true),
                    Column(5, "local_y", CsvSchemaDataType.Int, true)),

                Table("terrain_cluster_nodes_v2", V2AuthoringOwner.TerrainCluster,
                    "TerrainCluster/terrain_cluster_nodes_v2.csv",
                    Column(1, "cluster_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_catalog_v2.csv", "cluster_id")),
                    Column(2, "spine_variant_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_variants_v2.csv", "spine_variant_id")),
                    Column(3, "node_id", CsvSchemaDataType.Id, true, 1),
                    Column(4, "local_x", CsvSchemaDataType.Int, true),
                    Column(5, "local_y", CsvSchemaDataType.Int, true),
                    Column(6, "mandatory", CsvSchemaDataType.Bool, true)),

                Table("terrain_cluster_role_variant_links_v2", V2AuthoringOwner.TerrainCluster,
                    "TerrainCluster/terrain_cluster_role_variant_links_v2.csv",
                    Column(1, "cluster_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_catalog_v2.csv", "cluster_id")),
                    Column(2, "spine_variant_id", CsvSchemaDataType.Id, true, 1, foreignKey: V2("terrain_cluster_variants_v2.csv", "spine_variant_id")),
                    Column(3, "role_anchor_id", CsvSchemaDataType.Id, true, 2, foreignKey: V2("terrain_cluster_role_anchors_v2.csv", "role_anchor_id")),
                    Column(4, "node_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_nodes_v2.csv", "node_id"))),

                Table("terrain_cluster_ports_v2", V2AuthoringOwner.TerrainCluster,
                    "TerrainCluster/terrain_cluster_ports_v2.csv",
                    Column(1, "cluster_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_catalog_v2.csv", "cluster_id")),
                    Column(2, "port_id", CsvSchemaDataType.Id, true, 1),
                    Column(3, "port_kind", CsvSchemaDataType.Enum, true, allowed: Values("ENTRY", "EXIT")),
                    Column(4, "is_primary", CsvSchemaDataType.Bool, true),
                    Column(5, "role_anchor_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_role_anchors_v2.csv", "role_anchor_id")),
                    Column(6, "local_x", CsvSchemaDataType.Int, true),
                    Column(7, "local_y", CsvSchemaDataType.Int, true),
                    Column(8, "outward_side", CsvSchemaDataType.Enum, true, allowed: Values("L", "R", "U", "D")),
                    Column(9, "compatible_route_types", CsvSchemaDataType.IntList, true),
                    Column(10, "access_class", CsvSchemaDataType.Enum, true, allowed: Values("MANDATORY_NO_TOOL", "OPTIONAL_NO_TOOL", "OPTIONAL_TOOL", "OPTIONAL_ENVIRONMENT", "OPTIONAL_EXPLOSIVE", "OPTIONAL_HIDDEN", "PROGRESSION_GATE"))),

                Table("terrain_cluster_spine_edges_v2", V2AuthoringOwner.TerrainCluster,
                    "TerrainCluster/terrain_cluster_spine_edges_v2.csv",
                    Column(1, "cluster_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_catalog_v2.csv", "cluster_id")),
                    Column(2, "spine_variant_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_variants_v2.csv", "spine_variant_id")),
                    Column(3, "edge_id", CsvSchemaDataType.Id, true, 1),
                    Column(4, "from_node_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_nodes_v2.csv", "node_id")),
                    Column(5, "to_node_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_nodes_v2.csv", "node_id")),
                    Column(6, "movement", CsvSchemaDataType.Enum, true, allowed: Values("WALK", "JUMP", "DROP", "CLIMB", "SLIDE", "BOUNCE")),
                    Column(7, "start_x", CsvSchemaDataType.Int, true),
                    Column(8, "start_y", CsvSchemaDataType.Int, true),
                    Column(9, "end_x", CsvSchemaDataType.Int, true),
                    Column(10, "end_y", CsvSchemaDataType.Int, true),
                    Column(11, "mandatory", CsvSchemaDataType.Bool, true),
                    Column(12, "graph_kind", CsvSchemaDataType.Enum, true, allowed: Values("TRAVERSAL")),
                    Column(13, "clearance_width", CsvSchemaDataType.Int, true),
                    Column(14, "clearance_height", CsvSchemaDataType.Int, true),
                    Column(15, "landing_width", CsvSchemaDataType.Int, true),
                    Column(16, "landing_x", CsvSchemaDataType.Int, true),
                    Column(17, "landing_y", CsvSchemaDataType.Int, true),
                    Column(18, "recovery_width", CsvSchemaDataType.Int, true),
                    Column(19, "recovery_x", CsvSchemaDataType.Int, true),
                    Column(20, "recovery_y", CsvSchemaDataType.Int, true),
                    Column(21, "estimated_duration_ms", CsvSchemaDataType.Int, true),
                    Column(22, "timing_ruleset_id", CsvSchemaDataType.Id, true)),

                Table("terrain_cluster_envelope_cells_v2", V2AuthoringOwner.TerrainCluster,
                    "TerrainCluster/terrain_cluster_envelope_cells_v2.csv",
                    Column(1, "cluster_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_catalog_v2.csv", "cluster_id")),
                    Column(2, "spine_variant_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_variants_v2.csv", "spine_variant_id")),
                    Column(3, "edge_id", CsvSchemaDataType.Id, true, 1, foreignKey: V2("terrain_cluster_spine_edges_v2.csv", "edge_id")),
                    Column(4, "envelope_kind", CsvSchemaDataType.Enum, true, 2, allowed: Values("CENTERLINE", "FLOOR", "CLEARANCE", "JUMP_ARC", "DROP_COLUMN", "LANDING", "RECOVERY", "PROTECTED")),
                    Column(5, "local_x", CsvSchemaDataType.Int, true, 3),
                    Column(6, "local_y", CsvSchemaDataType.Int, true, 4)),

                Table("terrain_cluster_high_routes_v2", V2AuthoringOwner.TerrainCluster,
                    "TerrainCluster/terrain_cluster_high_routes_v2.csv",
                    Column(1, "cluster_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_catalog_v2.csv", "cluster_id")),
                    Column(2, "spine_variant_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_variants_v2.csv", "spine_variant_id")),
                    Column(3, "high_route_id", CsvSchemaDataType.Id, true, 1),
                    Column(4, "divergence_node_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_nodes_v2.csv", "node_id")),
                    Column(5, "rejoin_node_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_nodes_v2.csv", "node_id")),
                    Column(6, "high_point_node_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_nodes_v2.csv", "node_id"))),

                Table("terrain_cluster_high_route_edges_v2", V2AuthoringOwner.TerrainCluster,
                    "TerrainCluster/terrain_cluster_high_route_edges_v2.csv",
                    Column(1, "cluster_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_catalog_v2.csv", "cluster_id")),
                    Column(2, "spine_variant_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_variants_v2.csv", "spine_variant_id")),
                    Column(3, "high_route_id", CsvSchemaDataType.Id, true, 1, foreignKey: V2("terrain_cluster_high_routes_v2.csv", "high_route_id")),
                    Column(4, "edge_order", CsvSchemaDataType.Int, true, 2),
                    Column(5, "edge_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_spine_edges_v2.csv", "edge_id"))),

                Table("terrain_cluster_high_route_benefits_v2", V2AuthoringOwner.TerrainCluster,
                    "TerrainCluster/terrain_cluster_high_route_benefits_v2.csv",
                    Column(1, "cluster_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_catalog_v2.csv", "cluster_id")),
                    Column(2, "spine_variant_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_variants_v2.csv", "spine_variant_id")),
                    Column(3, "high_route_id", CsvSchemaDataType.Id, true, 1, foreignKey: V2("terrain_cluster_high_routes_v2.csv", "high_route_id")),
                    Column(4, "benefit_id", CsvSchemaDataType.Id, true, 2)),

                Table("terrain_cluster_high_route_failures_v2", V2AuthoringOwner.TerrainCluster,
                    "TerrainCluster/terrain_cluster_high_route_failures_v2.csv",
                    Column(1, "cluster_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_catalog_v2.csv", "cluster_id")),
                    Column(2, "spine_variant_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_variants_v2.csv", "spine_variant_id")),
                    Column(3, "high_route_id", CsvSchemaDataType.Id, true, 1, foreignKey: V2("terrain_cluster_high_routes_v2.csv", "high_route_id")),
                    Column(4, "failure_node_id", CsvSchemaDataType.Id, true, 2, foreignKey: V2("terrain_cluster_nodes_v2.csv", "node_id")),
                    Column(5, "preferred_recovery_target_node_id", CsvSchemaDataType.Id, false, foreignKey: V2("terrain_cluster_nodes_v2.csv", "node_id"))),

                Table("activity_catalog_v2", V2AuthoringOwner.Activity,
                    "Activity/activity_catalog_v2.csv",
                    Column(1, "activity_id", CsvSchemaDataType.Id, true, 1),
                    Column(2, "static_shell_id", CsvSchemaDataType.Id, true),
                    Column(3, "reward_policy", CsvSchemaDataType.Enum, true, allowed: Values("NONE", "OPTIONAL", "GUARANTEED")),
                    Column(4, "recovery_policy", CsvSchemaDataType.Enum, true, allowed: Values("NONE", "LOCAL", "FULL")),
                    Column(5, "removal_safe", CsvSchemaDataType.Bool, true),
                    Column(6, "terrain_cluster_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_catalog_v2.csv", "cluster_id")),
                    Column(7, "spine_variant_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_variants_v2.csv", "spine_variant_id")),
                    Column(8, "entry_traversal_node_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_nodes_v2.csv", "node_id")),
                    Column(9, "exit_traversal_node_id", CsvSchemaDataType.Id, true, foreignKey: V2("terrain_cluster_nodes_v2.csv", "node_id")),
                    Column(10, "preserve_static_traversal", CsvSchemaDataType.Bool, true),
                    Column(11, "preserve_access_class", CsvSchemaDataType.Bool, true),
                    Column(12, "permanent_solid_mutation_allowed", CsvSchemaDataType.Bool, true),
                    Column(13, "mandatory_exit_destruction_allowed", CsvSchemaDataType.Bool, true),
                    Column(14, "min_active_chunks", CsvSchemaDataType.Int, true),
                    Column(15, "max_active_chunks", CsvSchemaDataType.Int, true),
                    Column(16, "clearance_width", CsvSchemaDataType.Int, true),
                    Column(17, "clearance_height", CsvSchemaDataType.Int, true),
                    Column(18, "placement_weight", CsvSchemaDataType.Int, true),
                    Column(19, "strength_class", CsvSchemaDataType.Enum, true, allowed: Values("ORDINARY", "STRONG"))),

                Table("activity_compatibility_v2", V2AuthoringOwner.Activity,
                    "Activity/activity_compatibility_v2.csv",
                    Column(1, "activity_id", CsvSchemaDataType.Id, true, 1, foreignKey: V2("activity_catalog_v2.csv", "activity_id")),
                    Column(2, "compatibility_kind", CsvSchemaDataType.Enum, true, 2, allowed: Values("BIOME", "PACING", "ACCESS")),
                    Column(3, "value_token", CsvSchemaDataType.Id, true, 3)),

                Table("activity_cues_v2", V2AuthoringOwner.Activity,
                    "Activity/activity_cues_v2.csv",
                    Column(1, "activity_id", CsvSchemaDataType.Id, true, 1, foreignKey: V2("activity_catalog_v2.csv", "activity_id")),
                    Column(2, "cue_id", CsvSchemaDataType.Id, true, 2),
                    Column(3, "cue_kind", CsvSchemaDataType.Enum, true, allowed: Values("VISUAL", "AUDIO", "ENVIRONMENT", "MOTION")),
                    Column(4, "marker_id", CsvSchemaDataType.Id, true),
                    Column(5, "slot_id", CsvSchemaDataType.Id, true, foreignKey: V2("activity_slots_v2.csv", "slot_id")),
                    Column(6, "detectable_before_activation", CsvSchemaDataType.Bool, true)),

                Table("activity_graph_edges_v2", V2AuthoringOwner.Activity,
                    "Activity/activity_graph_edges_v2.csv",
                    Column(1, "activity_id", CsvSchemaDataType.Id, true, 1, foreignKey: V2("activity_catalog_v2.csv", "activity_id")),
                    Column(2, "edge_id", CsvSchemaDataType.Id, true, 2),
                    Column(3, "graph_kind", CsvSchemaDataType.Enum, true, allowed: Values("MECHANISM", "PROGRESSION")),
                    Column(4, "edge_kind", CsvSchemaDataType.Enum, true, allowed: Values("ACTIVATES", "DRIVES", "EMITS", "ENABLES", "DISABLES", "RESETS", "ADVANCE", "FAILURE", "RESET", "EXIT")),
                    Column(5, "from_node_id", CsvSchemaDataType.Id, true, foreignKey: V2("activity_graph_nodes_v2.csv", "node_id")),
                    Column(6, "to_node_id", CsvSchemaDataType.Id, true, foreignKey: V2("activity_graph_nodes_v2.csv", "node_id")),
                    Column(7, "edge_order", CsvSchemaDataType.Int, true)),

                Table("activity_graph_nodes_v2", V2AuthoringOwner.Activity,
                    "Activity/activity_graph_nodes_v2.csv",
                    Column(1, "activity_id", CsvSchemaDataType.Id, true, foreignKey: V2("activity_catalog_v2.csv", "activity_id")),
                    Column(2, "graph_kind", CsvSchemaDataType.Enum, true, allowed: Values("MECHANISM", "PROGRESSION")),
                    Column(3, "node_id", CsvSchemaDataType.Id, true, 1),
                    Column(4, "node_kind", CsvSchemaDataType.Enum, true, allowed: Values("CueEmitter", "Trigger", "Device", "Hazard", "ProjectileEmitter", "RewardEmitter", "RecoveryController", "ResetController", "Cue", "Activation", "Core", "Reward", "Recovery", "Reset", "Exit")),
                    Column(5, "slot_id", CsvSchemaDataType.Id, false, foreignKey: V2("activity_slots_v2.csv", "slot_id")),
                    Column(6, "is_start", CsvSchemaDataType.Bool, true),
                    Column(7, "is_terminal", CsvSchemaDataType.Bool, true)),

                Table("activity_safety_cells_v2", V2AuthoringOwner.Activity,
                    "Activity/activity_safety_cells_v2.csv",
                    Column(1, "activity_id", CsvSchemaDataType.Id, true, 1, foreignKey: V2("activity_catalog_v2.csv", "activity_id")),
                    Column(2, "safety_cell_kind", CsvSchemaDataType.Enum, true, 2, allowed: Values("SAFE_POCKET", "RECOVERY")),
                    Column(3, "local_x", CsvSchemaDataType.Int, true, 3),
                    Column(4, "local_y", CsvSchemaDataType.Int, true, 4)),

                Table("activity_slots_v2", V2AuthoringOwner.Activity,
                    "Activity/activity_slots_v2.csv",
                    Column(1, "activity_id", CsvSchemaDataType.Id, true, foreignKey: V2("activity_catalog_v2.csv", "activity_id")),
                    Column(2, "slot_id", CsvSchemaDataType.Id, true, 1),
                    Column(3, "slot_kind", CsvSchemaDataType.Enum, true, allowed: Values("Cue", "Trigger", "Device", "Hazard", "Projectile", "Reward", "Recovery", "Reset", "Npc")),
                    Column(4, "local_x", CsvSchemaDataType.Int, true),
                    Column(5, "local_y", CsvSchemaDataType.Int, true)),

                Table("event_overlay_catalog_v2", V2AuthoringOwner.EventOverlay,
                    "EventOverlay/event_overlay_catalog_v2.csv",
                    Column(1, "overlay_id", CsvSchemaDataType.Id, true, 1),
                    Column(2, "selection_weight", CsvSchemaDataType.Int, true),
                    Column(3, "variant_kind", CsvSchemaDataType.Enum, true, allowed: Values("NPC", "REWARD", "STATE", "COSMETIC", "EMPTY")),
                    Column(4, "is_empty", CsvSchemaDataType.Bool, true),
                    Column(5, "terrain_cluster_id", CsvSchemaDataType.Id, false, foreignKey: V2("terrain_cluster_catalog_v2.csv", "cluster_id")),
                    Column(6, "activity_id", CsvSchemaDataType.Id, false, foreignKey: V2("activity_catalog_v2.csv", "activity_id")),
                    Column(7, "minimum_progression_gap", CsvSchemaDataType.Int, true)),

                Table("event_overlay_compatibility_v2", V2AuthoringOwner.EventOverlay,
                    "EventOverlay/event_overlay_compatibility_v2.csv",
                    Column(1, "overlay_id", CsvSchemaDataType.Id, true, 1, foreignKey: V2("event_overlay_catalog_v2.csv", "overlay_id")),
                    Column(2, "compatibility_kind", CsvSchemaDataType.Enum, true, 2, allowed: Values("BIOME", "PACING", "ACCESS", "ACTIVITY", "SPECIAL_SLOT")),
                    Column(3, "value_token", CsvSchemaDataType.Id, true, 3)),

                Table("event_overlay_markers_v2", V2AuthoringOwner.EventOverlay,
                    "EventOverlay/event_overlay_markers_v2.csv",
                    Column(1, "overlay_id", CsvSchemaDataType.Id, true, 1, foreignKey: V2("event_overlay_catalog_v2.csv", "overlay_id")),
                    Column(2, "marker_id", CsvSchemaDataType.Id, true, 2),
                    Column(3, "marker_kind", CsvSchemaDataType.Enum, true, allowed: Values("ENABLE_MARKER", "DISABLE_MARKER", "SPAWN_NPC", "SPAWN_REWARD", "SET_STATE")),
                    Column(4, "local_x", CsvSchemaDataType.Int, true),
                    Column(5, "local_y", CsvSchemaDataType.Int, true),
                    Column(6, "operation", CsvSchemaDataType.Enum, true, allowed: Values("EnableMarker", "DisableMarker", "SpawnNpc", "SpawnReward", "SetState")),
                    Column(7, "payload_id", CsvSchemaDataType.Id, true),
                    Column(8, "target_source_kind", CsvSchemaDataType.Enum, true, allowed: Values("TERRAIN_CLUSTER", "ACTIVITY", "SPECIAL_REGION")),
                    Column(9, "target_owner_id", CsvSchemaDataType.Id, true),
                    Column(10, "target_slot_kind", CsvSchemaDataType.Id, true)),

                Table("special_region_catalog_v2", V2AuthoringOwner.SpecialRegion,
                    "SpecialRegion/special_region_catalog_v2.csv",
                    Column(1, "region_id", CsvSchemaDataType.Id, true, 1),
                    Column(2, "region_kind", CsvSchemaDataType.Enum, true, allowed: Values("VILLAGE", "CORE_RESOURCE", "FORGE", "BOSS", "OPTIONAL_LANDMARK")),
                    Column(3, "reservation_id", CsvSchemaDataType.Id, true),
                    Column(4, "footprint_width", CsvSchemaDataType.Int, true),
                    Column(5, "footprint_height", CsvSchemaDataType.Int, true)),

                Table("special_region_cells_v2", V2AuthoringOwner.SpecialRegion,
                    "SpecialRegion/special_region_cells_v2.csv",
                    Column(1, "region_id", CsvSchemaDataType.Id, true, 1, foreignKey: V2("special_region_catalog_v2.csv", "region_id")),
                    Column(2, "local_x", CsvSchemaDataType.Int, true, 2),
                    Column(3, "local_y", CsvSchemaDataType.Int, true, 3),
                    Column(4, "cell_kind", CsvSchemaDataType.Enum, true, allowed: Values("FIXED_SHELL", "REPLACEABLE_SLOT")),
                    Column(5, "slot_id", CsvSchemaDataType.Id, false)),

                Table("special_region_ports_v2", V2AuthoringOwner.SpecialRegion,
                    "SpecialRegion/special_region_ports_v2.csv",
                    Column(1, "region_id", CsvSchemaDataType.Id, true, 1, foreignKey: V2("special_region_catalog_v2.csv", "region_id")),
                    Column(2, "port_id", CsvSchemaDataType.Id, true, 2),
                    Column(3, "port_kind", CsvSchemaDataType.Enum, true, allowed: Values("ENTRY", "RETURN")),
                    Column(4, "side", CsvSchemaDataType.Enum, true, allowed: Values("L", "R", "U", "D")),
                    Column(5, "access_class", CsvSchemaDataType.Enum, true, allowed: Values("MANDATORY_NO_TOOL", "OPTIONAL_NO_TOOL", "OPTIONAL_TOOL", "OPTIONAL_ENVIRONMENT", "OPTIONAL_EXPLOSIVE", "OPTIONAL_HIDDEN", "PROGRESSION_GATE"))),

                Table("special_region_persistence_v2", V2AuthoringOwner.SpecialRegion,
                    "SpecialRegion/special_region_persistence_v2.csv",
                    Column(1, "region_id", CsvSchemaDataType.Id, true, 1, foreignKey: V2("special_region_catalog_v2.csv", "region_id")),
                    Column(2, "persistence_key", CsvSchemaDataType.Id, true, 2),
                    Column(3, "scope", CsvSchemaDataType.Enum, true, allowed: Values("REGION", "SLOT", "REWARD", "ENCOUNTER"))),
            };
        }

        private static V2AuthoringTableDescriptor Table(
            string tableId,
            V2AuthoringOwner owner,
            string relativePath,
            params V2AuthoringColumnDescriptor[] columns)
        {
            return new V2AuthoringTableDescriptor(tableId, owner, relativePath, columns);
        }

        private static V2AuthoringColumnDescriptor Column(
            int order,
            string name,
            CsvSchemaDataType type,
            bool required,
            int? primaryKeyOrder = null,
            string defaultValue = "",
            IEnumerable<string> allowed = null,
            V2AuthoringForeignKey foreignKey = null)
        {
            return new V2AuthoringColumnDescriptor(
                order, name, type, required, primaryKeyOrder, defaultValue, allowed, foreignKey);
        }

        private static V2AuthoringForeignKey V2(string fileName, string columnName)
        {
            return new V2AuthoringForeignKey(
                V2AuthoringSchemaDomain.AuthoringV2, fileName, columnName);
        }

        private static V2AuthoringForeignKey Legacy(string fileName, string columnName)
        {
            return new V2AuthoringForeignKey(
                V2AuthoringSchemaDomain.LegacyAuthoring, fileName, columnName);
        }

        private static string[] Values(params string[] values)
        {
            return values;
        }
    }
}
