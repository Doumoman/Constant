using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.EventOverlays;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.TerrainClusters;
using StarNight.Map.WorldGeneration.TerrainClusters.Authoring;

namespace StarNight.Map.WorldGeneration.Activities.Authoring
{
    public sealed class ActivityAuthoringRow
    {
        private readonly ReadOnlyDictionary<string, string> fields;

        public ActivityAuthoringRow(
            string tablePath,
            int recordNumber,
            IEnumerable<KeyValuePair<string, string>> sourceFields)
        {
            TablePath = (tablePath ?? string.Empty).Replace('\\', '/');
            RecordNumber = recordNumber;
            fields = new ReadOnlyDictionary<string, string>(
                (sourceFields ?? throw new ArgumentNullException(nameof(sourceFields)))
                .ToDictionary(value => value.Key, value => value.Value ?? string.Empty, StringComparer.Ordinal));
        }

        public string TablePath { get; }
        public int RecordNumber { get; }
        public IReadOnlyDictionary<string, string> Fields => fields;
        public string Get(string columnName) => fields.TryGetValue(columnName, out var value) ? value : string.Empty;
    }

    public sealed class ActivityAuthoringError : IEquatable<ActivityAuthoringError>, IComparable<ActivityAuthoringError>
    {
        public ActivityAuthoringError(string tablePath, int recordNumber, string columnName, string detail)
        {
            TablePath = tablePath ?? string.Empty;
            RecordNumber = recordNumber;
            ColumnName = columnName ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public string TablePath { get; }
        public int RecordNumber { get; }
        public string ColumnName { get; }
        public string Detail { get; }

        public int CompareTo(ActivityAuthoringError other)
        {
            if (other == null) return -1;
            var comparison = string.Compare(TablePath, other.TablePath, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = RecordNumber.CompareTo(other.RecordNumber);
            if (comparison != 0) return comparison;
            comparison = string.Compare(ColumnName, other.ColumnName, StringComparison.Ordinal);
            return comparison != 0 ? comparison : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(ActivityAuthoringError other) => other != null && CompareTo(other) == 0;
        public override bool Equals(object obj) => Equals(obj as ActivityAuthoringError);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(ToString());
        public override string ToString() => TablePath + "|record=" + RecordNumber + "|" + ColumnName + "|" + Detail;
    }

    public sealed class ActivityAuthoringEntry
    {
        private readonly ReadOnlyCollection<ActivityAuthoringRow> sourceRows;
        private readonly ReadOnlyDictionary<ActivitySlotId, EventMarkerId> markerBySlot;

        internal ActivityAuthoringEntry(
            ActivityStructureContract contract,
            ActivityPlacementProfile placementProfile,
            string staticShellId,
            string rewardPolicy,
            string recoveryPolicy,
            string shellDigest,
            string removalSafetyDigest,
            IEnumerable<ActivityAuthoringRow> rows)
        {
            Contract = contract ?? throw new ArgumentNullException(nameof(contract));
            PlacementProfile = placementProfile ?? throw new ArgumentNullException(nameof(placementProfile));
            StaticShellId = staticShellId ?? string.Empty;
            RewardPolicy = rewardPolicy ?? string.Empty;
            RecoveryPolicy = recoveryPolicy ?? string.Empty;
            ShellDigest = shellDigest ?? string.Empty;
            RemovalSafetyDigest = removalSafetyDigest ?? string.Empty;
            sourceRows = new ReadOnlyCollection<ActivityAuthoringRow>((rows ?? Array.Empty<ActivityAuthoringRow>())
                .OrderBy(value => value.TablePath, StringComparer.Ordinal)
                .ThenBy(value => value.RecordNumber).ToArray());
            markerBySlot = new ReadOnlyDictionary<ActivitySlotId, EventMarkerId>(
                Contract.Slots.ToDictionary(value => value.Id,
                    value => new EventMarkerId(value.MarkerId)));
        }

        public ActivityStructureId Id => Contract.Id;
        public ActivityStructureContract Contract { get; }
        public ActivityPlacementProfile PlacementProfile { get; }
        public string StaticShellId { get; }
        public string RewardPolicy { get; }
        public string RecoveryPolicy { get; }
        public string ShellDigest { get; }
        public string RemovalSafetyDigest { get; }
        public IReadOnlyList<ActivityAuthoringRow> SourceRows => sourceRows;
        public IReadOnlyDictionary<ActivitySlotId, EventMarkerId> MarkerBySlot => markerBySlot;
    }

    public sealed class ActivityAuthoringCatalog
    {
        private readonly ReadOnlyCollection<ActivityAuthoringEntry> entries;
        private readonly ReadOnlyDictionary<ActivityStructureId, ActivityAuthoringEntry> byId;

        internal ActivityAuthoringCatalog(IEnumerable<ActivityAuthoringEntry> sourceEntries, string canonicalContent)
        {
            var copy = (sourceEntries ?? throw new ArgumentNullException(nameof(sourceEntries)))
                .OrderBy(value => value.Id).ToArray();
            entries = new ReadOnlyCollection<ActivityAuthoringEntry>(copy);
            byId = new ReadOnlyDictionary<ActivityStructureId, ActivityAuthoringEntry>(copy.ToDictionary(value => value.Id));
            StableDigest = Canonical.Sha256(canonicalContent ?? string.Empty);
        }

        public IReadOnlyList<ActivityAuthoringEntry> Entries => entries;
        public IReadOnlyDictionary<ActivityStructureId, ActivityAuthoringEntry> ById => byId;
        public string StableDigest { get; }
        public bool TryGet(ActivityStructureId id, out ActivityAuthoringEntry entry) => byId.TryGetValue(id, out entry);
    }

    public sealed class ActivityAuthoringBuildResult
    {
        private readonly ReadOnlyCollection<ActivityAuthoringError> errors;

        internal ActivityAuthoringBuildResult(ActivityAuthoringCatalog catalog, IEnumerable<ActivityAuthoringError> sourceErrors)
        {
            var ordered = (sourceErrors ?? throw new ArgumentNullException(nameof(sourceErrors)))
                .Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            errors = new ReadOnlyCollection<ActivityAuthoringError>(ordered);
            Catalog = ordered.Length == 0 ? catalog : null;
        }

        public bool Success => Catalog != null && errors.Count == 0;
        public ActivityAuthoringCatalog Catalog { get; }
        public IReadOnlyList<ActivityAuthoringError> Errors => errors;
    }

    public static class ActivityAuthoringCatalogBuilder
    {
        private const string CatalogPath = "Activity/activity_catalog_v2.csv";
        private const string CompatibilityPath = "Activity/activity_compatibility_v2.csv";
        private const string CuesPath = "Activity/activity_cues_v2.csv";
        private const string EdgesPath = "Activity/activity_graph_edges_v2.csv";
        private const string NodesPath = "Activity/activity_graph_nodes_v2.csv";
        private const string SafetyPath = "Activity/activity_safety_cells_v2.csv";
        private const string SlotsPath = "Activity/activity_slots_v2.csv";

        private static readonly string[] Paths =
        {
            CatalogPath, CompatibilityPath, CuesPath, EdgesPath, NodesPath, SafetyPath, SlotsPath,
        };

        public static ActivityAuthoringBuildResult Build(
            IEnumerable<ActivityAuthoringRow> sourceRows,
            TerrainClusterAuthoringCatalog terrainCatalog)
        {
            var errors = new List<ActivityAuthoringError>();
            if (sourceRows == null || terrainCatalog == null)
            {
                errors.Add(new ActivityAuthoringError(string.Empty, 0, "input", "Rows and TerrainCluster catalog are required."));
                return new ActivityAuthoringBuildResult(null, errors);
            }

            var rows = sourceRows.Where(value => value != null).ToArray();
            foreach (var row in rows.Where(value => !Paths.Contains(value.TablePath, StringComparer.Ordinal)))
                Add(errors, row, "table", "Unexpected Activity table path.");
            var catalogRows = rows.Where(value => value.TablePath == CatalogPath).ToArray();
            DetectDuplicate(catalogRows, row => row.Get("activity_id"), "activity_id", errors);
            DetectDuplicate(rows.Where(value => value.TablePath == SlotsPath), row => row.Get("slot_id"), "slot_id", errors);
            DetectDuplicate(rows.Where(value => value.TablePath == NodesPath), row => row.Get("node_id"), "node_id", errors);
            DetectDuplicate(rows.Where(value => value.TablePath == CuesPath), row => row.Get("activity_id") + "|" + row.Get("cue_id"), "cue_id", errors);
            DetectDuplicate(rows.Where(value => value.TablePath == EdgesPath), row => row.Get("activity_id") + "|" + row.Get("edge_id"), "edge_id", errors);
            DetectDuplicate(rows.Where(value => value.TablePath == SafetyPath), row => row.Get("activity_id") + "|" + row.Get("safety_cell_kind") + "|" + row.Get("local_x") + "|" + row.Get("local_y"), "primary-key", errors);
            DetectDuplicate(rows.Where(value => value.TablePath == CompatibilityPath), row => row.Get("activity_id") + "|" + row.Get("compatibility_kind") + "|" + row.Get("value_token"), "primary-key", errors);

            var catalogIds = new HashSet<string>(catalogRows.Select(value => value.Get("activity_id")), StringComparer.Ordinal);
            foreach (var row in rows.Where(value => value.TablePath != CatalogPath && !catalogIds.Contains(value.Get("activity_id"))))
                Add(errors, row, "activity_id", "Activity child row has no catalog owner.");

            var entries = new List<ActivityAuthoringEntry>();
            foreach (var catalogRow in catalogRows.OrderBy(value => value.Get("activity_id"), StringComparer.Ordinal))
            {
                var entry = BuildEntry(catalogRow, rows, terrainCatalog, errors);
                if (entry != null) entries.Add(entry);
            }

            if (errors.Count != 0) return new ActivityAuthoringBuildResult(null, errors);
            var schemaDigest = V2AuthoringSchemaCanonicalDigest.Compute(V2AuthoringSchemaRegistry.DescribeDefaultTables());
            var material = new StringBuilder();
            Canonical.Append(material, "SCHEMA", schemaDigest);
            Canonical.Append(material, "TERRAIN", terrainCatalog.StableDigest);
            foreach (var row in rows.OrderBy(value => value.TablePath, StringComparer.Ordinal)
                         .ThenBy(value => CanonicalRowKey(value), StringComparer.Ordinal))
            {
                Canonical.Append(material, "ROW", row.TablePath,
                    string.Join("\u001f", row.Fields.OrderBy(value => value.Key, StringComparer.Ordinal)
                        .Select(value => value.Key + "=" + value.Value)));
            }
            foreach (var entry in entries.OrderBy(value => value.Id))
                Canonical.Append(material, "ENTRY", entry.Id.Value, entry.PlacementProfile.ActivityDigest,
                    entry.ShellDigest, entry.RemovalSafetyDigest);
            return new ActivityAuthoringBuildResult(new ActivityAuthoringCatalog(entries, material.ToString()), errors);
        }

        private static ActivityAuthoringEntry BuildEntry(
            ActivityAuthoringRow catalogRow,
            IReadOnlyList<ActivityAuthoringRow> allRows,
            TerrainClusterAuthoringCatalog terrainCatalog,
            ICollection<ActivityAuthoringError> errors)
        {
            var initialErrorCount = errors.Count;
            var activityIdText = catalogRow.Get("activity_id");
            var activityRows = allRows.Where(value => value.Get("activity_id") == activityIdText).ToArray();
            var terrainId = new TerrainClusterId(catalogRow.Get("terrain_cluster_id"));
            if (!terrainCatalog.TryGet(terrainId, out var terrain))
            {
                Add(errors, catalogRow, "terrain_cluster_id", "Unknown TerrainCluster reference.");
                return null;
            }

            var variantId = new SpineVariantId(catalogRow.Get("spine_variant_id"));
            if (variantId != terrain.BaselineVariantId)
                Add(errors, catalogRow, "spine_variant_id", "The explicit baseline variant does not match the TerrainCluster authority.");
            if (catalogRow.Get("static_shell_id") != terrain.Id.Value)
                Add(errors, catalogRow, "static_shell_id", "Static shell identity must be the explicit TerrainCluster identity.");

            var entryAnchor = terrain.Contract.RoleAnchors.SingleOrDefault(value => value.Role == ClusterRoleKind.Entry);
            var exitAnchor = terrain.Contract.RoleAnchors.SingleOrDefault(value => value.Role == ClusterRoleKind.Exit);
            if (entryAnchor == null || string.IsNullOrEmpty(catalogRow.Get("entry_traversal_node_id")))
                Add(errors, catalogRow, "entry_traversal_node_id", "An explicit baseline Entry node FK is required.");
            if (exitAnchor == null || string.IsNullOrEmpty(catalogRow.Get("exit_traversal_node_id")))
                Add(errors, catalogRow, "exit_traversal_node_id", "An explicit baseline Exit node FK is required.");

            if (!Bool(catalogRow, "removal_safe", errors, out var removalSafe) || !removalSafe)
                Add(errors, catalogRow, "removal_safe", "Starter Activity must be removal-safe.");
            Bool(catalogRow, "preserve_static_traversal", errors, out var preserveTraversal);
            Bool(catalogRow, "preserve_access_class", errors, out var preserveAccess);
            Bool(catalogRow, "permanent_solid_mutation_allowed", errors, out var solidMutation);
            Bool(catalogRow, "mandatory_exit_destruction_allowed", errors, out var exitDestruction);
            if (!preserveTraversal || !preserveAccess || solidMutation || exitDestruction)
                Add(errors, catalogRow, "removal-policy", "Required policy is true,true,false,false.");

            Int(catalogRow, "min_active_chunks", errors, out var minimumChunks);
            Int(catalogRow, "max_active_chunks", errors, out var maximumChunks);
            Int(catalogRow, "clearance_width", errors, out var clearanceWidth);
            Int(catalogRow, "clearance_height", errors, out var clearanceHeight);
            Int(catalogRow, "placement_weight", errors, out var weight);
            var activeCount = terrain.Contract.Footprint.ActiveChunks.Count;
            if (minimumChunks < 2 || maximumChunks < minimumChunks || maximumChunks < activeCount || minimumChunks > activeCount || maximumChunks > 5)
                Add(errors, catalogRow, "active_chunks", "Bounds must contain the exact 2..5-chunk starter shell.");
            if (clearanceWidth < 3 || clearanceHeight < 3)
                Add(errors, catalogRow, "clearance", "Starter clearance must be at least 3x3.");
            if (weight < 1 || weight > 10000)
                Add(errors, catalogRow, "placement_weight", "Weight must be in 1..10000.");
            var strength = catalogRow.Get("strength_class") == "STRONG"
                ? ActivityStrengthClass.Strong : ActivityStrengthClass.Ordinary;
            if (catalogRow.Get("strength_class") != "STRONG" && catalogRow.Get("strength_class") != "ORDINARY")
                Add(errors, catalogRow, "strength_class", "Unknown strength class.");

            var slotRows = activityRows.Where(value => value.TablePath == SlotsPath).ToArray();
            var slots = new List<ActivitySlot>();
            var slotRowsById = new Dictionary<string, ActivityAuthoringRow>(StringComparer.Ordinal);
            foreach (var row in slotRows)
            {
                if (!ParseEnum(row.Get("slot_kind"), out ActivitySlotKind kind))
                {
                    Add(errors, row, "slot_kind", "Unknown Activity slot kind.");
                    continue;
                }
                Int(row, "local_x", errors, out var x);
                Int(row, "local_y", errors, out var y);
                var slotId = row.Get("slot_id");
                slotRowsById[slotId] = row;
                slots.Add(new ActivitySlot(new ActivitySlotId(slotId), kind, new LocalTileCoord(x, y), MarkerForSlot(slotId)));
            }

            var cues = new List<ActivityCue>();
            foreach (var row in activityRows.Where(value => value.TablePath == CuesPath))
            {
                if (!ParseCueKind(row.Get("cue_kind"), out var kind)) Add(errors, row, "cue_kind", "Unknown cue kind.");
                Bool(row, "detectable_before_activation", errors, out var detectable);
                if (!slotRowsById.ContainsKey(row.Get("slot_id"))) Add(errors, row, "slot_id", "Cue slot FK does not resolve in the same Activity.");
                if (row.Get("marker_id") != MarkerForSlot(row.Get("slot_id"))) Add(errors, row, "marker_id", "Cue marker must equal its explicit slot companion marker.");
                cues.Add(new ActivityCue(kind, new ActivitySlotId(row.Get("slot_id")), detectable));
            }

            var nodeRows = activityRows.Where(value => value.TablePath == NodesPath).ToArray();
            var mechanismNodes = new List<MechanismNode>();
            var progressionNodes = new List<ProgressionNode>();
            foreach (var row in nodeRows)
            {
                var graphKind = row.Get("graph_kind");
                Bool(row, "is_start", errors, out var isStart);
                Bool(row, "is_terminal", errors, out var isTerminal);
                if (graphKind == "MECHANISM")
                {
                    if (!ParseEnum(row.Get("node_kind"), out MechanismNodeKind kind)) Add(errors, row, "node_kind", "Unknown mechanism node kind.");
                    if (!slotRowsById.ContainsKey(row.Get("slot_id"))) Add(errors, row, "slot_id", "Mechanism slot FK does not resolve in the same Activity.");
                    if (isStart || isTerminal) Add(errors, row, "flags", "Mechanism nodes cannot be progression endpoints.");
                    mechanismNodes.Add(new MechanismNode(row.Get("node_id"), kind, new ActivitySlotId(row.Get("slot_id"))));
                }
                else if (graphKind == "PROGRESSION")
                {
                    if (!ParseEnum(row.Get("node_kind"), out ProgressionPhaseKind phase)) Add(errors, row, "node_kind", "Unknown progression phase.");
                    if (!string.IsNullOrEmpty(row.Get("slot_id")) && !slotRowsById.ContainsKey(row.Get("slot_id"))) Add(errors, row, "slot_id", "Progression slot FK does not resolve in the same Activity.");
                    if (isStart != (phase == ProgressionPhaseKind.Cue) || isTerminal != (phase == ProgressionPhaseKind.Exit))
                        Add(errors, row, "flags", "Only progression Cue/Exit may be start/terminal.");
                    progressionNodes.Add(new ProgressionNode(row.Get("node_id"), phase));
                }
                else Add(errors, row, "graph_kind", "Unknown graph kind.");
            }

            var nodeOwner = nodeRows.ToDictionary(value => value.Get("node_id"), StringComparer.Ordinal);
            var mechanismEdges = new List<MechanismEdge>();
            var progressionEdges = new List<ProgressionEdge>();
            foreach (var row in activityRows.Where(value => value.TablePath == EdgesPath))
            {
                Int(row, "edge_order", errors, out var order);
                if (order < 0) Add(errors, row, "edge_order", "Edge order must be non-negative.");
                if (!nodeOwner.TryGetValue(row.Get("from_node_id"), out var from) || !nodeOwner.TryGetValue(row.Get("to_node_id"), out var to) ||
                    from.Get("activity_id") != activityIdText || to.Get("activity_id") != activityIdText ||
                    from.Get("graph_kind") != row.Get("graph_kind") || to.Get("graph_kind") != row.Get("graph_kind"))
                {
                    Add(errors, row, "node_fk", "Both graph endpoints must resolve in the same Activity and graph.");
                    continue;
                }
                if (row.Get("graph_kind") == "MECHANISM")
                {
                    if (!ParseMechanismRelation(row.Get("edge_kind"), out var relation)) Add(errors, row, "edge_kind", "Unknown mechanism relation.");
                    mechanismEdges.Add(new MechanismEdge(row.Get("edge_id"), row.Get("from_node_id"), row.Get("to_node_id"), relation));
                }
                else
                {
                    if (!ParseProgressionEdge(row.Get("edge_kind"), out var kind)) Add(errors, row, "edge_kind", "Unknown progression edge kind.");
                    progressionEdges.Add(new ProgressionEdge(row.Get("edge_id"), row.Get("from_node_id"), row.Get("to_node_id"), kind));
                }
            }

            var start = nodeRows.SingleOrDefault(value => value.Get("graph_kind") == "PROGRESSION" && value.Get("is_start") == "true");
            var terminal = nodeRows.SingleOrDefault(value => value.Get("graph_kind") == "PROGRESSION" && value.Get("is_terminal") == "true");
            if (start == null || terminal == null) Add(errors, catalogRow, "progression", "Exactly one start and terminal node are required.");

            var safe = new List<LocalTileCoord>();
            var recovery = new List<LocalTileCoord>();
            foreach (var row in activityRows.Where(value => value.TablePath == SafetyPath))
            {
                Int(row, "local_x", errors, out var x);
                Int(row, "local_y", errors, out var y);
                if (row.Get("safety_cell_kind") == "SAFE_POCKET") safe.Add(new LocalTileCoord(x, y));
                else if (row.Get("safety_cell_kind") == "RECOVERY") recovery.Add(new LocalTileCoord(x, y));
                else Add(errors, row, "safety_cell_kind", "Unknown safety cell kind.");
            }

            var biomes = new List<MoonpalaceBiomeId>();
            var pacing = new List<PacingRole>();
            var access = new List<AccessClass>();
            foreach (var row in activityRows.Where(value => value.TablePath == CompatibilityPath))
            {
                switch (row.Get("compatibility_kind"))
                {
                    case "BIOME":
                        if (MoonpalaceBiomeId.TryParse(row.Get("value_token"), out var biome)) biomes.Add(biome);
                        else Add(errors, row, "value_token", "Unknown biome.");
                        break;
                    case "PACING":
                        if (PacingRoleTokenCodec.TryParse(row.Get("value_token"), out var role)) pacing.Add(role);
                        else Add(errors, row, "value_token", "Unknown pacing role.");
                        break;
                    case "ACCESS":
                        if (AccessClassTokenCodec.TryParse(row.Get("value_token"), out var accessClass)) access.Add(accessClass);
                        else Add(errors, row, "value_token", "Unknown access class.");
                        break;
                    default: Add(errors, row, "compatibility_kind", "Unknown Activity compatibility kind."); break;
                }
            }
            if (biomes.Count == 0 || pacing.Count == 0 || access.Count == 0)
                Add(errors, catalogRow, "compatibility", "BIOME, PACING, and ACCESS rows are all required.");
            if (biomes.Count != 1 || biomes.Any(value => value != terrain.Biome))
                Add(errors, catalogRow, "compatibility.biome", "Starter biome must match the TerrainCluster authority.");
            if (!access.Contains(AccessClass.OptionalNoTool))
                Add(errors, catalogRow, "compatibility.access", "OPTIONAL_NO_TOOL is required.");

            var shellValidation = TerrainClusterContractValidator.Validate(terrain.Contract);
            var shellDigest = shellValidation.IsValid ? shellValidation.CanonicalDigest : string.Empty;
            var safety = new ActivityRemovalSafety(variantId,
                entryAnchor == null ? string.Empty : entryAnchor.TraversalNodeId,
                exitAnchor == null ? string.Empty : exitAnchor.TraversalNodeId,
                safe, recovery, preserveTraversal, preserveAccess, solidMutation, exitDestruction,
                0, 0, AccessClass.OptionalNoTool, AccessClass.OptionalNoTool, shellDigest, shellDigest);
            var contract = new ActivityStructureContract(new ActivityStructureId(activityIdText), terrainId, variantId,
                pacing, access, slots, cues, new MechanismGraph(mechanismNodes, mechanismEdges),
                new ProgressionGraph(start == null ? string.Empty : start.Get("node_id"),
                    terminal == null ? string.Empty : terminal.Get("node_id"), progressionNodes, progressionEdges), safety);
            var validation = ActivityContractValidator.Validate(contract, terrain.Contract);
            foreach (var error in validation.Errors)
                errors.Add(new ActivityAuthoringError(CatalogPath, catalogRow.RecordNumber, "contract", error.ToString()));

            var removalDigest = Canonical.Sha256(string.Join("|", new[]
            {
                activityIdText, variantId.Value, catalogRow.Get("entry_traversal_node_id"), catalogRow.Get("exit_traversal_node_id"),
                string.Join(",", safe.OrderBy(value => value.Y).ThenBy(value => value.X).Select(Coordinate)),
                string.Join(",", recovery.OrderBy(value => value.Y).ThenBy(value => value.X).Select(Coordinate)),
                preserveTraversal.ToString(), preserveAccess.ToString(), solidMutation.ToString(), exitDestruction.ToString(), shellDigest,
            }));
            var profile = new ActivityPlacementProfile(contract.Id, terrainId, variantId,
                validation.CanonicalDigest, shellDigest, removalDigest, biomes, pacing, access,
                minimumChunks, maximumChunks, clearanceWidth, clearanceHeight, weight, strength);
            if (errors.Count != initialErrorCount) return null;
            return new ActivityAuthoringEntry(contract, profile, catalogRow.Get("static_shell_id"),
                catalogRow.Get("reward_policy"), catalogRow.Get("recovery_policy"), shellDigest,
                removalDigest, activityRows);
        }

        private static string MarkerForSlot(string slotId) =>
            slotId != null && slotId.StartsWith("SLOT_", StringComparison.Ordinal)
                ? "MARKER_" + slotId.Substring(5) : string.Empty;

        private static bool ParseCueKind(string token, out ActivityCueKind kind)
        {
            switch (token)
            {
                case "VISUAL": kind = ActivityCueKind.Visual; return true;
                case "AUDIO": kind = ActivityCueKind.Audio; return true;
                case "ENVIRONMENT": kind = ActivityCueKind.Environment; return true;
                case "MOTION": kind = ActivityCueKind.Motion; return true;
                default: kind = default; return false;
            }
        }

        private static bool ParseMechanismRelation(string token, out MechanismRelationKind kind)
        {
            switch (token)
            {
                case "ACTIVATES": kind = MechanismRelationKind.Activates; return true;
                case "DRIVES": kind = MechanismRelationKind.Drives; return true;
                case "EMITS": kind = MechanismRelationKind.Emits; return true;
                case "ENABLES": kind = MechanismRelationKind.Enables; return true;
                case "DISABLES": kind = MechanismRelationKind.Disables; return true;
                case "RESETS": kind = MechanismRelationKind.Resets; return true;
                default: kind = default; return false;
            }
        }

        private static bool ParseProgressionEdge(string token, out ProgressionEdgeKind kind)
        {
            switch (token)
            {
                case "ADVANCE": kind = ProgressionEdgeKind.Advance; return true;
                case "FAILURE": kind = ProgressionEdgeKind.Failure; return true;
                case "RESET": kind = ProgressionEdgeKind.Reset; return true;
                case "EXIT": kind = ProgressionEdgeKind.Exit; return true;
                default: kind = default; return false;
            }
        }

        private static bool ParseEnum<T>(string token, out T value) where T : struct
        {
            return Enum.TryParse(token, false, out value) && Enum.IsDefined(typeof(T), value) &&
                   !int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
        }

        private static bool Bool(ActivityAuthoringRow row, string column, ICollection<ActivityAuthoringError> errors, out bool value)
        {
            if (row.Get(column) == "true") { value = true; return true; }
            if (row.Get(column) == "false") { value = false; return true; }
            value = false;
            Add(errors, row, column, "Expected exact lowercase Boolean.");
            return false;
        }

        private static bool Int(ActivityAuthoringRow row, string column, ICollection<ActivityAuthoringError> errors, out int value)
        {
            if (int.TryParse(row.Get(column), NumberStyles.Integer, CultureInfo.InvariantCulture, out value)) return true;
            Add(errors, row, column, "Expected invariant integer.");
            return false;
        }

        private static void DetectDuplicate(IEnumerable<ActivityAuthoringRow> source, Func<ActivityAuthoringRow, string> key,
            string column, ICollection<ActivityAuthoringError> errors)
        {
            foreach (var group in source.GroupBy(key, StringComparer.Ordinal).Where(value => value.Count() > 1))
                foreach (var row in group) Add(errors, row, column, "Duplicate primary key: " + group.Key);
        }

        private static void Add(ICollection<ActivityAuthoringError> errors, ActivityAuthoringRow row, string column, string detail) =>
            errors.Add(new ActivityAuthoringError(row.TablePath, row.RecordNumber, column, detail));
        private static string Coordinate(LocalTileCoord value) => value.X.ToString(CultureInfo.InvariantCulture) + "," + value.Y.ToString(CultureInfo.InvariantCulture);
        private static string CanonicalRowKey(ActivityAuthoringRow row) => string.Join("\u001f", row.Fields.OrderBy(value => value.Key, StringComparer.Ordinal).Select(value => value.Value));
    }

    internal static class Canonical
    {
        public static void Append(StringBuilder builder, params string[] values)
        {
            foreach (var value in values)
            {
                var normalized = value ?? string.Empty;
                builder.Append(normalized.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(normalized).Append('|');
            }
            builder.Append('\n');
        }

        public static string Sha256(string value)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(new UTF8Encoding(false).GetBytes(value ?? string.Empty))
                    .Select(item => item.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }
}
