using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.Activities;
using StarNight.Map.WorldGeneration.Activities.Authoring;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.TerrainClusters;
using StarNight.Map.WorldGeneration.TerrainClusters.Authoring;

namespace StarNight.Map.WorldGeneration.EventOverlays.Authoring
{
    public sealed class EventOverlayAuthoringRow
    {
        private readonly ReadOnlyDictionary<string, string> fields;

        public EventOverlayAuthoringRow(
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

    public sealed class EventOverlayAuthoringError : IEquatable<EventOverlayAuthoringError>, IComparable<EventOverlayAuthoringError>
    {
        public EventOverlayAuthoringError(string tablePath, int recordNumber, string columnName, string detail)
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
        public int CompareTo(EventOverlayAuthoringError other)
        {
            if (other == null) return -1;
            var comparison = string.Compare(TablePath, other.TablePath, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = RecordNumber.CompareTo(other.RecordNumber);
            if (comparison != 0) return comparison;
            comparison = string.Compare(ColumnName, other.ColumnName, StringComparison.Ordinal);
            return comparison != 0 ? comparison : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }
        public bool Equals(EventOverlayAuthoringError other) => other != null && CompareTo(other) == 0;
        public override bool Equals(object obj) => Equals(obj as EventOverlayAuthoringError);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(ToString());
        public override string ToString() => TablePath + "|record=" + RecordNumber + "|" + ColumnName + "|" + Detail;
    }

    public sealed class EventMarkerAuthoringTarget
    {
        internal EventMarkerAuthoringTarget(
            EventMarkerId markerId,
            EventMarkerOperation operation,
            string payloadId,
            string sourceKind,
            string sourceOwnerId,
            string sourceSlotKind,
            LocalTileCoord coordinate)
        {
            MarkerId = markerId;
            Operation = operation;
            PayloadId = payloadId ?? string.Empty;
            SourceKind = sourceKind ?? string.Empty;
            SourceOwnerId = sourceOwnerId ?? string.Empty;
            SourceSlotKind = sourceSlotKind ?? string.Empty;
            Coordinate = coordinate;
        }

        public EventMarkerId MarkerId { get; }
        public EventMarkerOperation Operation { get; }
        public string PayloadId { get; }
        public string SourceKind { get; }
        public string SourceOwnerId { get; }
        public string SourceSlotKind { get; }
        public LocalTileCoord Coordinate { get; }
    }

    public sealed class EventOverlayAuthoringEntry
    {
        private readonly ReadOnlyCollection<EventMarkerAuthoringTarget> markerTargets;
        private readonly ReadOnlyCollection<string> compatibleActivities;
        private readonly ReadOnlyCollection<string> compatibleSpecialSlots;
        private readonly ReadOnlyCollection<EventOverlayAuthoringRow> sourceRows;

        internal EventOverlayAuthoringEntry(
            EventOverlayContract contract,
            EventOverlayAssignmentProfile profile,
            EventOverlayRemovalEvidence removalEvidence,
            IEnumerable<EventMarkerAuthoringTarget> targets,
            IEnumerable<string> activities,
            IEnumerable<string> specialSlots,
            IEnumerable<EventOverlayAuthoringRow> rows)
        {
            Contract = contract ?? throw new ArgumentNullException(nameof(contract));
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            RemovalEvidence = removalEvidence ?? throw new ArgumentNullException(nameof(removalEvidence));
            markerTargets = new ReadOnlyCollection<EventMarkerAuthoringTarget>((targets ?? Array.Empty<EventMarkerAuthoringTarget>())
                .OrderBy(value => value.MarkerId).ToArray());
            compatibleActivities = new ReadOnlyCollection<string>((activities ?? Array.Empty<string>())
                .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
            compatibleSpecialSlots = new ReadOnlyCollection<string>((specialSlots ?? Array.Empty<string>())
                .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
            sourceRows = new ReadOnlyCollection<EventOverlayAuthoringRow>((rows ?? Array.Empty<EventOverlayAuthoringRow>())
                .OrderBy(value => value.TablePath, StringComparer.Ordinal).ThenBy(value => value.RecordNumber).ToArray());
        }

        public EventOverlayId Id => Contract.Id;
        public EventOverlayContract Contract { get; }
        public EventOverlayAssignmentProfile Profile { get; }
        public EventOverlayRemovalEvidence RemovalEvidence { get; }
        public IReadOnlyList<EventMarkerAuthoringTarget> MarkerTargets => markerTargets;
        public IReadOnlyList<string> CompatibleActivityIds => compatibleActivities;
        public IReadOnlyList<string> CompatibleSpecialSlotKinds => compatibleSpecialSlots;
        public IReadOnlyList<EventOverlayAuthoringRow> SourceRows => sourceRows;
    }

    public sealed class EventOverlayAuthoringCatalog
    {
        private readonly ReadOnlyCollection<EventOverlayAuthoringEntry> entries;
        private readonly ReadOnlyDictionary<EventOverlayId, EventOverlayAuthoringEntry> byId;

        internal EventOverlayAuthoringCatalog(IEnumerable<EventOverlayAuthoringEntry> sourceEntries, string canonicalContent)
        {
            var copy = (sourceEntries ?? throw new ArgumentNullException(nameof(sourceEntries)))
                .OrderBy(value => value.Id).ToArray();
            entries = new ReadOnlyCollection<EventOverlayAuthoringEntry>(copy);
            byId = new ReadOnlyDictionary<EventOverlayId, EventOverlayAuthoringEntry>(copy.ToDictionary(value => value.Id));
            StableDigest = Canonical.Sha256(canonicalContent ?? string.Empty);
        }

        public IReadOnlyList<EventOverlayAuthoringEntry> Entries => entries;
        public IReadOnlyDictionary<EventOverlayId, EventOverlayAuthoringEntry> ById => byId;
        public string StableDigest { get; }
        public bool TryGet(EventOverlayId id, out EventOverlayAuthoringEntry entry) => byId.TryGetValue(id, out entry);
    }

    public sealed class EventOverlayAuthoringBuildResult
    {
        private readonly ReadOnlyCollection<EventOverlayAuthoringError> errors;
        internal EventOverlayAuthoringBuildResult(EventOverlayAuthoringCatalog catalog, IEnumerable<EventOverlayAuthoringError> sourceErrors)
        {
            var ordered = (sourceErrors ?? throw new ArgumentNullException(nameof(sourceErrors)))
                .Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            errors = new ReadOnlyCollection<EventOverlayAuthoringError>(ordered);
            Catalog = ordered.Length == 0 ? catalog : null;
        }
        public bool Success => Catalog != null && errors.Count == 0;
        public EventOverlayAuthoringCatalog Catalog { get; }
        public IReadOnlyList<EventOverlayAuthoringError> Errors => errors;
    }

    public static class EventOverlayAuthoringCatalogBuilder
    {
        private const string CatalogPath = "EventOverlay/event_overlay_catalog_v2.csv";
        private const string CompatibilityPath = "EventOverlay/event_overlay_compatibility_v2.csv";
        private const string MarkersPath = "EventOverlay/event_overlay_markers_v2.csv";

        public static EventOverlayAuthoringBuildResult Build(
            IEnumerable<EventOverlayAuthoringRow> sourceRows,
            TerrainClusterAuthoringCatalog terrainCatalog,
            ActivityAuthoringCatalog activityCatalog)
        {
            var errors = new List<EventOverlayAuthoringError>();
            if (sourceRows == null || terrainCatalog == null || activityCatalog == null)
            {
                errors.Add(new EventOverlayAuthoringError(string.Empty, 0, "input", "Rows and referenced catalogs are required."));
                return new EventOverlayAuthoringBuildResult(null, errors);
            }
            var rows = sourceRows.Where(value => value != null).ToArray();
            var paths = new[] { CatalogPath, CompatibilityPath, MarkersPath };
            foreach (var row in rows.Where(value => !paths.Contains(value.TablePath, StringComparer.Ordinal)))
                Add(errors, row, "table", "Unexpected EventOverlay table path.");
            var catalogRows = rows.Where(value => value.TablePath == CatalogPath).ToArray();
            DetectDuplicate(catalogRows, row => row.Get("overlay_id"), "overlay_id", errors);
            DetectDuplicate(rows.Where(value => value.TablePath == MarkersPath), row => row.Get("overlay_id") + "|" + row.Get("marker_id"), "primary-key", errors);
            DetectDuplicate(rows.Where(value => value.TablePath == CompatibilityPath), row => row.Get("overlay_id") + "|" + row.Get("compatibility_kind") + "|" + row.Get("value_token"), "primary-key", errors);
            var ids = new HashSet<string>(catalogRows.Select(value => value.Get("overlay_id")), StringComparer.Ordinal);
            foreach (var row in rows.Where(value => value.TablePath != CatalogPath && !ids.Contains(value.Get("overlay_id"))))
                Add(errors, row, "overlay_id", "Event child row has no catalog owner.");

            var entries = new List<EventOverlayAuthoringEntry>();
            foreach (var row in catalogRows.OrderBy(value => value.Get("overlay_id"), StringComparer.Ordinal))
            {
                var entry = BuildEntry(row, rows, terrainCatalog, activityCatalog, errors);
                if (entry != null) entries.Add(entry);
            }
            if (entries.Count(value => value.Contract.Kind == EventOverlayKind.Empty) != 1)
                errors.Add(new EventOverlayAuthoringError(CatalogPath, 0, "is_empty", "Exactly one explicit Empty overlay is required."));
            if (errors.Count != 0) return new EventOverlayAuthoringBuildResult(null, errors);

            var material = new StringBuilder();
            Canonical.Append(material, "SCHEMA", V2AuthoringSchemaCanonicalDigest.Compute(V2AuthoringSchemaRegistry.DescribeDefaultTables()));
            Canonical.Append(material, "TERRAIN", terrainCatalog.StableDigest);
            Canonical.Append(material, "ACTIVITY", activityCatalog.StableDigest);
            foreach (var row in rows.OrderBy(value => value.TablePath, StringComparer.Ordinal)
                         .ThenBy(value => CanonicalRowKey(value), StringComparer.Ordinal))
                Canonical.Append(material, "ROW", row.TablePath,
                    string.Join("\u001f", row.Fields.OrderBy(value => value.Key, StringComparer.Ordinal)
                        .Select(value => value.Key + "=" + value.Value)));
            foreach (var entry in entries.OrderBy(value => value.Id))
                Canonical.Append(material, "ENTRY", entry.Id.Value, entry.Profile.ContractDigest,
                    entry.Profile.Weight.ToString(CultureInfo.InvariantCulture),
                    entry.Profile.MinimumProgressionGap.ToString(CultureInfo.InvariantCulture));
            return new EventOverlayAuthoringBuildResult(new EventOverlayAuthoringCatalog(entries, material.ToString()), errors);
        }

        private static EventOverlayAuthoringEntry BuildEntry(
            EventOverlayAuthoringRow catalogRow,
            IReadOnlyList<EventOverlayAuthoringRow> allRows,
            TerrainClusterAuthoringCatalog terrainCatalog,
            ActivityAuthoringCatalog activityCatalog,
            ICollection<EventOverlayAuthoringError> errors)
        {
            var initialErrorCount = errors.Count;
            var overlayId = catalogRow.Get("overlay_id");
            var owned = allRows.Where(value => value.Get("overlay_id") == overlayId).ToArray();
            if (!ParseKind(catalogRow.Get("variant_kind"), out var kind)) Add(errors, catalogRow, "variant_kind", "Unknown overlay kind.");
            Bool(catalogRow, "is_empty", errors, out var isEmpty);
            if (isEmpty != (kind == EventOverlayKind.Empty)) Add(errors, catalogRow, "is_empty", "Empty flag must match variant kind.");
            Int(catalogRow, "selection_weight", errors, out var weight);
            Int(catalogRow, "minimum_progression_gap", errors, out var gap);
            if ((isEmpty && (weight != 0 || gap != 0)) || (!isEmpty && (weight < 1 || weight > 10000 || gap < 0)))
                Add(errors, catalogRow, "selection", "Empty uses 0/0; non-empty uses weight 1..10000 and gap >= 0.");

            var terrainId = new TerrainClusterId(catalogRow.Get("terrain_cluster_id"));
            if (!terrainCatalog.TryGet(terrainId, out var terrain))
            {
                Add(errors, catalogRow, "terrain_cluster_id", "Explicit shell must resolve, including Empty.");
                return null;
            }
            ActivityAuthoringEntry activity = null;
            ActivityStructureId? activityId = null;
            if (!string.IsNullOrEmpty(catalogRow.Get("activity_id")))
            {
                var parsed = new ActivityStructureId(catalogRow.Get("activity_id"));
                if (!activityCatalog.TryGet(parsed, out activity)) Add(errors, catalogRow, "activity_id", "Unknown Activity reference.");
                else if (activity.Contract.TerrainClusterId != terrainId) Add(errors, catalogRow, "activity_id", "Activity and Event must share the same shell.");
                activityId = parsed;
            }

            var biomes = new List<MoonpalaceBiomeId>();
            var pacing = new List<PacingRole>();
            var access = new List<AccessClass>();
            var compatibleActivities = new List<string>();
            var compatibleSpecialSlots = new List<string>();
            foreach (var row in owned.Where(value => value.TablePath == CompatibilityPath))
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
                    case "ACTIVITY":
                        if (!activityCatalog.ById.Keys.Any(value => value.Value == row.Get("value_token"))) Add(errors, row, "value_token", "Unknown compatible Activity.");
                        compatibleActivities.Add(row.Get("value_token"));
                        break;
                    case "SPECIAL_SLOT":
                        if (row.Get("value_token") != "NPC" && row.Get("value_token") != "REWARD" && row.Get("value_token") != "EVENT")
                            Add(errors, row, "value_token", "Unknown replaceable SpecialRegion slot kind.");
                        compatibleSpecialSlots.Add(row.Get("value_token"));
                        break;
                    default: Add(errors, row, "compatibility_kind", "Unknown Event compatibility kind."); break;
                }
            }
            if (biomes.Count == 0 || pacing.Count == 0 || access.Count == 0)
                Add(errors, catalogRow, "compatibility", "Explicit BIOME, PACING, and ACCESS rows are required.");
            if (biomes.Any(value => value != terrain.Biome)) Add(errors, catalogRow, "compatibility.biome", "Biome must match the shell authority.");
            if (activityId.HasValue && !compatibleActivities.Contains(activityId.Value.Value))
                Add(errors, catalogRow, "compatibility.activity", "Referenced Activity requires an exact ACTIVITY compatibility row.");
            if (!activityId.HasValue && compatibleActivities.Count != 0)
                Add(errors, catalogRow, "compatibility.activity", "ACTIVITY compatibility requires a catalog Activity reference.");

            var assignments = new List<EventMarkerAssignment>();
            var targets = new List<EventMarkerAuthoringTarget>();
            foreach (var row in owned.Where(value => value.TablePath == MarkersPath))
            {
                if (!ParseOperation(row.Get("operation"), out var operation)) Add(errors, row, "operation", "Unknown marker operation.");
                if (!MarkerKindMatches(row.Get("marker_kind"), operation)) Add(errors, row, "marker_kind", "Marker kind must match operation.");
                Int(row, "local_x", errors, out var x);
                Int(row, "local_y", errors, out var y);
                var coordinate = new LocalTileCoord(x, y);
                var markerId = new EventMarkerId(row.Get("marker_id"));
                if (row.Get("target_source_kind") == "ACTIVITY")
                {
                    if (activity == null || row.Get("target_owner_id") != activity.Id.Value)
                        Add(errors, row, "target_owner_id", "Activity target owner must equal the Event Activity reference.");
                    else
                    {
                        var matches = activity.Contract.Slots.Where(value => value.Id.Value == row.Get("target_slot_kind") ||
                            value.Kind.ToString() == row.Get("target_slot_kind")).ToArray();
                        if (matches.Length != 1 || matches[0].Tile != coordinate || matches[0].MarkerId != markerId.Value)
                            Add(errors, row, "target", "Activity marker, slot identity/kind, and coordinate provenance must match exactly.");
                    }
                }
                else if (row.Get("target_source_kind") == "TERRAIN_CLUSTER")
                {
                    if (row.Get("target_owner_id") != terrain.Id.Value || !TryRole(row.Get("target_slot_kind"), out var role))
                        Add(errors, row, "target_owner_id", "Terrain target owner/role is invalid.");
                    else
                    {
                        var anchor = terrain.Contract.RoleAnchors.SingleOrDefault(value => value.Role == role);
                        if (anchor == null || anchor.Tile != coordinate)
                            Add(errors, row, "target", "Terrain role anchor coordinate provenance must match exactly.");
                    }
                }
                else if (row.Get("target_source_kind") == "SPECIAL_REGION")
                    Add(errors, row, "target_source_kind", "No physical SpecialRegion starter catalog is available for this exact target.");
                else Add(errors, row, "target_source_kind", "Unknown marker target source.");
                assignments.Add(new EventMarkerAssignment(markerId, operation, row.Get("payload_id")));
                targets.Add(new EventMarkerAuthoringTarget(markerId, operation, row.Get("payload_id"),
                    row.Get("target_source_kind"), row.Get("target_owner_id"), row.Get("target_slot_kind"), coordinate));
            }
            if ((isEmpty && assignments.Count != 0) || (!isEmpty && assignments.Count != 1))
                Add(errors, catalogRow, "markers", "Empty owns zero markers; every non-empty starter owns exactly one.");

            var contract = new EventOverlayContract(new EventOverlayId(overlayId), kind, terrainId, activityId, assignments);
            var shellValidation = TerrainClusterContractValidator.Validate(terrain.Contract);
            var shellDigest = shellValidation.IsValid ? shellValidation.CanonicalDigest : string.Empty;
            var activityDigest = activity == null ? string.Empty : ActivityContractValidator.Validate(activity.Contract, terrain.Contract).CanonicalDigest;
            var evidence = new EventOverlayRemovalEvidence(shellDigest, shellDigest, shellDigest, shellDigest,
                access.FirstOrDefault(), access.FirstOrDefault(), activityDigest, activityDigest);
            var validation = EventOverlayValidator.Validate(contract, terrain.Contract,
                activity == null ? null : activity.Contract, targets.Select(value => value.MarkerId), evidence);
            foreach (var error in validation.Errors)
                errors.Add(new EventOverlayAuthoringError(CatalogPath, catalogRow.RecordNumber, "contract", error.ToString()));
            var profile = new EventOverlayAssignmentProfile(contract, validation.CanonicalDigest, weight, gap,
                biomes, pacing, access, activityId);
            if (errors.Count != initialErrorCount) return null;
            return new EventOverlayAuthoringEntry(contract, profile, evidence, targets,
                compatibleActivities, compatibleSpecialSlots, owned);
        }

        private static bool MarkerKindMatches(string markerKind, EventMarkerOperation operation)
        {
            switch (operation)
            {
                case EventMarkerOperation.EnableMarker: return markerKind == "ENABLE_MARKER";
                case EventMarkerOperation.DisableMarker: return markerKind == "DISABLE_MARKER";
                case EventMarkerOperation.SpawnNpc: return markerKind == "SPAWN_NPC";
                case EventMarkerOperation.SpawnReward: return markerKind == "SPAWN_REWARD";
                case EventMarkerOperation.SetState: return markerKind == "SET_STATE";
                default: return false;
            }
        }

        private static bool ParseKind(string token, out EventOverlayKind kind)
        {
            switch (token)
            {
                case "NPC": kind = EventOverlayKind.Npc; return true;
                case "REWARD": kind = EventOverlayKind.Reward; return true;
                case "STATE": kind = EventOverlayKind.State; return true;
                case "COSMETIC": kind = EventOverlayKind.Cosmetic; return true;
                case "EMPTY": kind = EventOverlayKind.Empty; return true;
                default: kind = default; return false;
            }
        }

        private static bool ParseOperation(string token, out EventMarkerOperation operation)
        {
            return Enum.TryParse(token, false, out operation) && Enum.IsDefined(typeof(EventMarkerOperation), operation) &&
                   !int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
        }

        private static bool TryRole(string token, out ClusterRoleKind role)
        {
            switch (token)
            {
                case "ENTRY": role = ClusterRoleKind.Entry; return true;
                case "BUILD_UP": role = ClusterRoleKind.BuildUp; return true;
                case "CORE": role = ClusterRoleKind.Core; return true;
                case "REWARD": role = ClusterRoleKind.Reward; return true;
                case "RECOVERY": role = ClusterRoleKind.Recovery; return true;
                case "EXIT": role = ClusterRoleKind.Exit; return true;
                default: role = default; return false;
            }
        }

        private static bool Bool(EventOverlayAuthoringRow row, string column, ICollection<EventOverlayAuthoringError> errors, out bool value)
        {
            if (row.Get(column) == "true") { value = true; return true; }
            if (row.Get(column) == "false") { value = false; return true; }
            value = false; Add(errors, row, column, "Expected exact lowercase Boolean."); return false;
        }

        private static bool Int(EventOverlayAuthoringRow row, string column, ICollection<EventOverlayAuthoringError> errors, out int value)
        {
            if (int.TryParse(row.Get(column), NumberStyles.Integer, CultureInfo.InvariantCulture, out value)) return true;
            Add(errors, row, column, "Expected invariant integer."); return false;
        }

        private static void DetectDuplicate(IEnumerable<EventOverlayAuthoringRow> source, Func<EventOverlayAuthoringRow, string> key,
            string column, ICollection<EventOverlayAuthoringError> errors)
        {
            foreach (var group in source.GroupBy(key, StringComparer.Ordinal).Where(value => value.Count() > 1))
                foreach (var row in group) Add(errors, row, column, "Duplicate primary key: " + group.Key);
        }
        private static void Add(ICollection<EventOverlayAuthoringError> errors, EventOverlayAuthoringRow row, string column, string detail) =>
            errors.Add(new EventOverlayAuthoringError(row.TablePath, row.RecordNumber, column, detail));
        private static string CanonicalRowKey(EventOverlayAuthoringRow row) => string.Join("\u001f", row.Fields.OrderBy(value => value.Key, StringComparer.Ordinal).Select(value => value.Value));
    }
}
