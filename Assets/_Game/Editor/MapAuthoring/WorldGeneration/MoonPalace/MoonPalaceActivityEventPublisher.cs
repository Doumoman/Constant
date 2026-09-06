using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.MoonPalace;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor.WorldGeneration.MoonPalace
{
    public static class MoonPalaceActivityEventPublisher
    {
        public const string AuthoringDirectoryRelativePath =
            "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_05";
        public const string GeneratedDirectoryRelativePath =
            "MapDesign/MCP/GENERATED/MAP21_05";
        public const string ActivityProfilesFileName =
            "moonpalace_activity_profiles.csv";
        public const string ActivityBindingsFileName =
            "moonpalace_activity_cluster_bindings.csv";
        public const string ActivitySlotsFileName = "moonpalace_activity_slots.csv";
        public const string EventProfilesFileName =
            "moonpalace_event_overlay_profiles.csv";
        public const string EventMarkersFileName =
            "moonpalace_event_overlay_markers.csv";
        public const string CompatibilityFileName =
            "moonpalace_activity_event_compatibility.csv";
        public const string ActivityEventManifestFileName =
            "moonpalace_activity_event_manifest.json";
        public const string RemovalSafetyManifestFileName =
            "moonpalace_activity_removal_safety_manifest.json";
        public const string EventOverlayManifestFileName =
            "moonpalace_event_overlay_manifest.json";
        public const string DigestManifestFileName =
            "moonpalace_activity_event_digest_manifest.json";

        public const string SourceMap12ActivityCatalogRelativePath =
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/Activity/activity_catalog_v2.csv";
        public const string SourceMap12ActivitySlotsRelativePath =
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/Activity/activity_slots_v2.csv";
        public const string SourceMap12ActivitySafetyRelativePath =
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/Activity/activity_safety_cells_v2.csv";
        public const string SourceMap12ActivityCompatibilityRelativePath =
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/Activity/activity_compatibility_v2.csv";
        public const string SourceMap12EventCatalogRelativePath =
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/EventOverlay/event_overlay_catalog_v2.csv";
        public const string SourceMap12EventMarkersRelativePath =
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/EventOverlay/event_overlay_markers_v2.csv";
        public const string SourceMap2104AllBiomeManifestRelativePath =
            "MapDesign/MCP/GENERATED/MAP21_04/moonpalace_all_biome_cluster_pool_manifest.json";
        public const string SourceMap2104DigestManifestRelativePath =
            "MapDesign/MCP/GENERATED/MAP21_04/moonpalace_mill_dough_cluster_digest_manifest.json";
        private const string SourceMap1207ResultRelativePath =
            "MapDesign/MCP/REPORTS/MAP12_07_MAP12_ACTIVITY_EXIT_TESTS_RESULT.md";
        private const string SourceMap2104ResultRelativePath =
            "MapDesign/MCP/REPORTS/MAP21_04_EXPAND_MILL_AND_DOUGH_CLUSTER_POOLS_RESULT.md";
        private const string SourceMap2104TaskRelativePath =
            "MapDesign/MCP/TASKS/MAP21_04_EXPAND_MILL_AND_DOUGH_CLUSTER_POOLS.md";

        public static MoonPalaceActivityEventPublishedSample CreateReadOnlySample(
            string projectRoot, string createdUtc, bool reverseInputOrder = false)
        {
            projectRoot = RequireProjectRoot(projectRoot);
            ValidateUpstreamPreconditions(projectRoot);
            var sourceActivityCatalog = ReadRows(projectRoot,
                SourceMap12ActivityCatalogRelativePath);
            var sourceActivitySlots = ReadRows(projectRoot,
                SourceMap12ActivitySlotsRelativePath);
            var sourceActivitySafety = ReadRows(projectRoot,
                SourceMap12ActivitySafetyRelativePath);
            var sourceActivityCompatibility = ReadRows(projectRoot,
                SourceMap12ActivityCompatibilityRelativePath);
            var sourceEventCatalog = ReadRows(projectRoot,
                SourceMap12EventCatalogRelativePath);
            var sourceEventMarkers = ReadRows(projectRoot,
                SourceMap12EventMarkersRelativePath);
            var clusterManifest = JsonUtility.FromJson<SourceClusterManifestDocument>(
                File.ReadAllText(Resolve(projectRoot,
                    SourceMap2104AllBiomeManifestRelativePath)));
            if (clusterManifest == null || clusterManifest.cluster_catalog == null ||
                clusterManifest.cluster_catalog.Length != 48 ||
                clusterManifest.canonical_digest !=
                    MoonPalaceActivityEventPreconditions.SourceMap2104AllBiomeClusterDigest)
                throw new InvalidDataException("MAP21_04 all-biome manifest mismatch.");
            if (reverseInputOrder)
            {
                sourceActivityCatalog.Reverse();
                sourceActivitySlots.Reverse();
                sourceActivitySafety.Reverse();
                sourceActivityCompatibility.Reverse();
                sourceEventCatalog.Reverse();
                sourceEventMarkers.Reverse();
                Array.Reverse(clusterManifest.cluster_catalog);
            }

            var clusters = clusterManifest.cluster_catalog.Select(value =>
                new MoonPalaceActivityClusterDescriptor(value.cluster_id, value.biome_id,
                    value.pool_kind)).ToArray();
            var specs = ActivitySpecs().ToDictionary(value => value.ActivityId,
                StringComparer.Ordinal);
            ValidateMap12Activities(sourceActivityCatalog, sourceActivitySlots,
                sourceActivitySafety, sourceActivityCompatibility, specs);
            var bindings = new List<MoonPalaceActivityClusterBinding>();
            var slots = new List<MoonPalaceActivitySlotRecord>();
            var removals = new List<MoonPalaceActivityRemovalSafetyRecord>();
            var activities = new List<MoonPalaceActivityProfile>();
            foreach (var row in sourceActivityCatalog.OrderBy(value =>
                Value(value, "activity_id"), StringComparer.Ordinal))
            {
                var activityId = Value(row, "activity_id");
                var spec = specs[activityId];
                var binding = new MoonPalaceActivityClusterBinding(activityId,
                    spec.BiomeId, spec.PrimaryClusterId, spec.FallbackClusterId);
                bindings.Add(binding);
                var activitySlots = BuildActivitySlots(activityId, sourceActivitySlots,
                    sourceActivitySafety).ToArray();
                slots.AddRange(activitySlots);
                var staticShellDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
                {
                    "MAP21_05_STATIC_ACTIVITY_SHELL_V1", activityId,
                    spec.PrimaryClusterId, Value(row, "static_shell_id"),
                    "PreserveStaticTerrainIdentity",
                });
                var criticalTargetDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
                {
                    "MAP21_05_ACTIVITY_CRITICAL_TARGET_V1", activityId,
                    Value(row, "entry_traversal_node_id"),
                    Value(row, "exit_traversal_node_id"),
                    string.Join("|", activitySlots.Where(value =>
                        value.SlotSemantic == "Reward" ||
                        value.SlotSemantic == "ExitPreservation")
                        .Select(value => value.SlotId).OrderBy(value => value,
                            StringComparer.Ordinal)),
                });
                var removal = new MoonPalaceActivityRemovalSafetyRecord(activityId,
                    staticShellDigest, criticalTargetDigest,
                    activitySlots.Count(value => value.SlotSemantic == "SafePocket"),
                    activitySlots.Count(value => value.SlotSemantic == "Recovery"));
                removals.Add(removal);
                var slotDigest = SetDigest("MAP21_05_ACTIVITY_SLOT_SET_V1",
                    activitySlots.Select(value => value.CanonicalLine));
                activities.Add(new MoonPalaceActivityProfile(activityId,
                    spec.ActivityKind, spec.BiomeId,
                    Title(Value(row, "strength_class")),
                    Integer(row, "placement_weight"), binding,
                    activitySlots.Select(value => value.SlotSemantic).Distinct(
                        StringComparer.Ordinal), staticShellDigest, removal.CanonicalDigest,
                    slotDigest));
            }

            ValidateMap12Events(sourceEventCatalog, sourceEventMarkers);
            var compatibility = BuildCompatibility().ToArray();
            var markers = BuildEventMarkers(sourceEventMarkers).ToArray();
            var events = BuildEventProfiles(sourceEventCatalog, markers, compatibility)
                .ToArray();
            var production = new MoonPalaceActivityEventProduction(activities, bindings,
                slots, removals, events, markers, compatibility, clusters, createdUtc);
            var activityEventJson = production.SerializeActivityEventManifest();
            var removalJson = production.SerializeRemovalSafetyManifest();
            var eventJson = production.SerializeEventOverlayManifest();
            var csvDigests = new[]
            {
                Named(ActivityProfilesFileName, production.SerializeActivityProfilesCsv()),
                Named(ActivityBindingsFileName, production.SerializeBindingsCsv()),
                Named(ActivitySlotsFileName, production.SerializeSlotsCsv()),
                Named(EventProfilesFileName, production.SerializeEventProfilesCsv()),
                Named(EventMarkersFileName, production.SerializeEventMarkersCsv()),
                Named(CompatibilityFileName, production.SerializeCompatibilityCsv()),
            };
            var jsonDigests = new[]
            {
                Named(ActivityEventManifestFileName, activityEventJson),
                Named(RemovalSafetyManifestFileName, removalJson),
                Named(EventOverlayManifestFileName, eventJson),
            };
            var digestManifest = new MoonPalaceActivityEventDigestManifest(production,
                csvDigests, jsonDigests, createdUtc);
            return new MoonPalaceActivityEventPublishedSample(production, digestManifest,
                MoonPalaceActivityEventForbiddenOperationCounters.Zero,
                Array.Empty<string>());
        }

        public static MoonPalaceActivityEventPublishedSample PublishAuthoringAndSamples(
            string projectRoot, bool focusedMap2105Pass)
        {
            if (!focusedMap2105Pass)
                throw new InvalidOperationException(
                    "MAP21_06 handoff requires a focused MAP21_05 PASS.");
            projectRoot = RequireProjectRoot(projectRoot);
            var sample = CreateReadOnlySample(projectRoot,
                DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(Resolve(projectRoot, AuthoringDirectoryRelativePath));
            Directory.CreateDirectory(Resolve(projectRoot, GeneratedDirectoryRelativePath));
            var paths = new[]
            {
                Combine(AuthoringDirectoryRelativePath, ActivityProfilesFileName),
                Combine(AuthoringDirectoryRelativePath, ActivityBindingsFileName),
                Combine(AuthoringDirectoryRelativePath, ActivitySlotsFileName),
                Combine(AuthoringDirectoryRelativePath, EventProfilesFileName),
                Combine(AuthoringDirectoryRelativePath, EventMarkersFileName),
                Combine(AuthoringDirectoryRelativePath, CompatibilityFileName),
                Combine(GeneratedDirectoryRelativePath, ActivityEventManifestFileName),
                Combine(GeneratedDirectoryRelativePath, RemovalSafetyManifestFileName),
                Combine(GeneratedDirectoryRelativePath, EventOverlayManifestFileName),
                Combine(GeneratedDirectoryRelativePath, DigestManifestFileName),
            };
            Write(projectRoot, paths[0], sample.Production.SerializeActivityProfilesCsv());
            Write(projectRoot, paths[1], sample.Production.SerializeBindingsCsv());
            Write(projectRoot, paths[2], sample.Production.SerializeSlotsCsv());
            Write(projectRoot, paths[3], sample.Production.SerializeEventProfilesCsv());
            Write(projectRoot, paths[4], sample.Production.SerializeEventMarkersCsv());
            Write(projectRoot, paths[5], sample.Production.SerializeCompatibilityCsv());
            Write(projectRoot, paths[6], sample.Production.SerializeActivityEventManifest());
            Write(projectRoot, paths[7], sample.Production.SerializeRemovalSafetyManifest());
            Write(projectRoot, paths[8], sample.Production.SerializeEventOverlayManifest());
            Write(projectRoot, paths[9], sample.DigestManifest.Serialize());
            return new MoonPalaceActivityEventPublishedSample(sample.Production,
                sample.DigestManifest, sample.Counters, paths);
        }

        private static IEnumerable<MoonPalaceActivitySlotRecord> BuildActivitySlots(
            string activityId,
            IEnumerable<IReadOnlyDictionary<string, string>> sourceSlots,
            IEnumerable<IReadOnlyDictionary<string, string>> sourceSafety)
        {
            var rows = sourceSlots.Where(row => Value(row, "activity_id") == activityId)
                .OrderBy(row => Value(row, "slot_id"), StringComparer.Ordinal).ToArray();
            foreach (var row in rows)
            {
                var sourceId = Value(row, "slot_id");
                yield return new MoonPalaceActivitySlotRecord(activityId, sourceId,
                    Value(row, "slot_kind"), Integer(row, "local_x"),
                    Integer(row, "local_y"), sourceId, "STATIC_MARKER:" + sourceId);
            }
            var safePocket = sourceSafety.Single(row =>
                Value(row, "activity_id") == activityId &&
                Value(row, "safety_cell_kind") == "SAFE_POCKET");
            yield return new MoonPalaceActivitySlotRecord(activityId,
                "SLOT_" + activityId.Substring(4) + "_SAFE_POCKET", "SafePocket",
                Integer(safePocket, "local_x"), Integer(safePocket, "local_y"),
                "MAP12_SAFETY_CELL:SAFE_POCKET",
                "STATIC_MARKER:SAFE_POCKET:" + activityId);
            var reset = rows.Single(row => Value(row, "slot_kind") == "Reset");
            yield return new MoonPalaceActivitySlotRecord(activityId,
                "SLOT_" + activityId.Substring(4) + "_EXIT_PRESERVATION",
                "ExitPreservation", Integer(reset, "local_x"),
                Integer(reset, "local_y"), Value(reset, "slot_id"),
                "STATIC_PROOF:EXIT_PRESERVATION:" + activityId);
        }

        private static IEnumerable<MoonPalaceEventMarkerRecord> BuildEventMarkers(
            IEnumerable<IReadOnlyDictionary<string, string>> sourceMarkers)
        {
            foreach (var row in sourceMarkers.OrderBy(value =>
                Value(value, "overlay_id"), StringComparer.Ordinal))
            {
                var eventId = Value(row, "overlay_id");
                if (eventId == "EVT_METEOR_FALL")
                    yield return new MoonPalaceEventMarkerRecord(eventId,
                        "MARKER_MOONPALACE_TERRAIN_CORE_METEOR", "SET_STATE",
                        Integer(row, "local_x"), Integer(row, "local_y"),
                        Value(row, "operation"), Value(row, "payload_id"),
                        "TERRAIN_CLUSTER", "ALL_TERRAIN_CORE_CANDIDATES", "CORE");
                else
                    yield return new MoonPalaceEventMarkerRecord(eventId,
                        Value(row, "marker_id"), Value(row, "marker_kind"),
                        Integer(row, "local_x"), Integer(row, "local_y"),
                        Value(row, "operation"), Value(row, "payload_id"),
                        Value(row, "target_source_kind"),
                        Value(row, "target_owner_id"),
                        Value(row, "target_slot_kind"));
            }
        }

        private static IEnumerable<MoonPalaceActivityEventCompatibilityRecord>
            BuildCompatibility()
        {
            yield return new MoonPalaceActivityEventCompatibilityRecord(
                "EVT_METEOR_FALL", string.Empty, "Terrain:CORE", "CORE",
                "TerrainCoreMarkerCandidateOnly");
            yield return new MoonPalaceActivityEventCompatibilityRecord(
                "EVT_WANDERING_MERCHANT", "ACT_MILL_ESCORT_CART", "Activity:Npc",
                "Npc", "ExactActivitySlotOnly");
            yield return new MoonPalaceActivityEventCompatibilityRecord(
                "EVT_RARE_CREATURE", "ACT_MILL_ESCORT_CART", "Activity:Npc", "Npc",
                "ExactActivitySlotOnly");
            yield return new MoonPalaceActivityEventCompatibilityRecord(
                "EVT_MARU_INTERVENTION", "ACT_MARU_REWIND_ANOMALY",
                "Activity:Device", "Device", "ExactActivitySlotOnly");
            foreach (var id in MoonPalaceActivityEventProduction.RequiredActivityIds)
                yield return new MoonPalaceActivityEventCompatibilityRecord("EVT_EMPTY", id,
                    "ActivityFallback", "Any", "ExplicitEmptyFallback");
            yield return new MoonPalaceActivityEventCompatibilityRecord("EVT_EMPTY",
                string.Empty, "UnassignedClusters", "Any",
                "ExplicitUnassignedClusterFallback");
        }

        private static IEnumerable<MoonPalaceEventOverlayProfile> BuildEventProfiles(
            IEnumerable<IReadOnlyDictionary<string, string>> sourceCatalog,
            IEnumerable<MoonPalaceEventMarkerRecord> markers,
            IEnumerable<MoonPalaceActivityEventCompatibilityRecord> compatibility)
        {
            foreach (var row in sourceCatalog.OrderBy(value =>
                Value(value, "overlay_id"), StringComparer.Ordinal))
            {
                var eventId = Value(row, "overlay_id");
                var eventMarkers = markers.Where(value => value.EventId == eventId).ToArray();
                var eventCompatibility = compatibility.Where(value =>
                    value.EventId == eventId).ToArray();
                var empty = Boolean(row, "is_empty");
                yield return new MoonPalaceEventOverlayProfile(eventId,
                    Title(Value(row, "variant_kind")), empty ? "none" :
                        eventMarkers.Single().Operation,
                    Integer(row, "selection_weight"),
                    Integer(row, "minimum_progression_gap"), eventMarkers.Length,
                    eventCompatibility.Where(value => value.ActivityId.Length != 0)
                        .Select(value => value.ActivityId),
                    eventId == "EVT_METEOR_FALL" ? "Terrain:CORE" :
                        eventId == "EVT_EMPTY" ?
                            "AllActivities|UnassignedClusters" :
                            eventCompatibility.Single().ClusterScope,
                    empty ? "none" : eventMarkers.Single().PayloadIdentity, empty,
                    SetDigest("MAP21_05_EVENT_MARKER_SET_V1",
                        eventMarkers.Select(value => value.CanonicalLine)),
                    SetDigest("MAP21_05_EVENT_COMPATIBILITY_SET_V1",
                        eventCompatibility.Select(value => value.CanonicalLine)));
            }
        }

        private static ActivitySpec[] ActivitySpecs() => new[]
        {
            new ActivitySpec("ACT_CRATER_BOULDER_CHAIN", "BoulderChain", "MoonCrater",
                "TC_CRATER_PROD_ROCK_SHELF_CHAIN", "TC_CRATER_PROD_FALL_RECOVERY"),
            new ActivitySpec("ACT_CRATER_RICOCHET_MINE", "RicochetMine", "MoonCrater",
                "TC_CRATER_PROD_BROKEN_SLOPE", "TC_CRATER_PROD_BOWL_CROSS"),
            new ActivitySpec("ACT_DOUGH_TIME_TRIAL", "TimeTrial", "MoonDough",
                "TC_DOUGH_PROD_BOUNCE_CUP", "TC_DOUGH_PROD_SQUISH_CROSS"),
            new ActivitySpec("ACT_MARU_REWIND_ANOMALY", "MaruRewindAnomaly", "MoonDough",
                "TC_DOUGH_PROD_RECOVERY_PAD_CHAIN", "TC_DOUGH_PROD_STICKY_SHELF"),
            new ActivitySpec("ACT_MILL_ESCORT_CART", "EscortCart", "AbandonedMill",
                "TC_MILL_PROD_BEAM_OVERHANG", "TC_MILL_PROD_RUST_LEDGE"),
            new ActivitySpec("ACT_MILL_GEAR_GRID", "GearGrid", "AbandonedMill",
                "TC_MILL_PROD_GEAR_GALLERY", "TC_MILL_PROD_BROKEN_PILLAR"),
            new ActivitySpec("ACT_MILL_PESTLE_WORKSHOP", "PestleWorkshop",
                "AbandonedMill", "TC_MILL_PROD_ORTHOGONAL_SHAFT",
                "TC_MILL_PROD_FALL_RECOVERY"),
        };

        private static void ValidateMap12Activities(
            IReadOnlyCollection<IReadOnlyDictionary<string, string>> catalog,
            IReadOnlyCollection<IReadOnlyDictionary<string, string>> slots,
            IReadOnlyCollection<IReadOnlyDictionary<string, string>> safety,
            IReadOnlyCollection<IReadOnlyDictionary<string, string>> compatibility,
            IReadOnlyDictionary<string, ActivitySpec> specs)
        {
            var ids = catalog.Select(row => Value(row, "activity_id"))
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (!ids.SequenceEqual(MoonPalaceActivityEventProduction.RequiredActivityIds
                    .OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal))
                throw new InvalidDataException("MAP12 exact Activity inventory mismatch.");
            if (slots.Count != 52 || safety.Count != 14)
                throw new InvalidDataException("MAP12 Activity slot/safety inventory mismatch.");
            foreach (var id in ids)
            {
                var biomeRows = compatibility.Where(row =>
                    Value(row, "activity_id") == id &&
                    Value(row, "compatibility_kind") == "BIOME").ToArray();
                if (biomeRows.Length != 1 || Value(biomeRows[0], "value_token") !=
                    specs[id].BiomeId ||
                    slots.Count(row => Value(row, "activity_id") == id &&
                        Value(row, "slot_kind") == "Cue") != 1 ||
                    slots.Count(row => Value(row, "activity_id") == id &&
                        Value(row, "slot_kind") == "Reward") != 1 ||
                    slots.Count(row => Value(row, "activity_id") == id &&
                        Value(row, "slot_kind") == "Recovery") != 1 ||
                    slots.Count(row => Value(row, "activity_id") == id &&
                        Value(row, "slot_kind") == "Reset") != 1 ||
                    safety.Count(row => Value(row, "activity_id") == id &&
                        Value(row, "safety_cell_kind") == "SAFE_POCKET") != 1)
                    throw new InvalidDataException("MAP12 Activity semantic mismatch: " + id);
            }
        }

        private static void ValidateMap12Events(
            IReadOnlyCollection<IReadOnlyDictionary<string, string>> catalog,
            IReadOnlyCollection<IReadOnlyDictionary<string, string>> markers)
        {
            var ids = catalog.Select(row => Value(row, "overlay_id"))
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (!ids.SequenceEqual(MoonPalaceActivityEventProduction.RequiredEventIds
                    .OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal) ||
                markers.Count != 4 ||
                markers.Select(row => Value(row, "overlay_id")).Distinct(
                    StringComparer.Ordinal).Count() != 4)
                throw new InvalidDataException("MAP12 exact Event inventory mismatch.");
            var empty = catalog.Single(row => Value(row, "overlay_id") == "EVT_EMPTY");
            if (!Boolean(empty, "is_empty") ||
                Integer(empty, "selection_weight") != 0 ||
                Integer(empty, "minimum_progression_gap") != 0)
                throw new InvalidDataException("MAP12 Empty event mismatch.");
        }

        private static void ValidateUpstreamPreconditions(string projectRoot)
        {
            if (FileSha256(Resolve(projectRoot, SourceMap1207ResultRelativePath)) !=
                    MoonPalaceActivityEventPreconditions.SourceMap1207ResultDigest)
                throw new InvalidDataException("MAP12_07 Result SHA-256 mismatch.");
            var map12Report = File.ReadAllText(Resolve(projectRoot,
                SourceMap1207ResultRelativePath));
            foreach (var digest in new[]
            {
                MoonPalaceActivityEventPreconditions.SourceMap12AggregateDigest,
                MoonPalaceActivityEventPreconditions.SourceMap12ActivityCatalogDigest,
                MoonPalaceActivityEventPreconditions.SourceMap12EventCatalogDigest,
            })
                if (!map12Report.Contains(digest))
                    throw new InvalidDataException("MAP12 approved digest is absent.");
            if (FileSha256(Resolve(projectRoot, SourceMap2104ResultRelativePath)) !=
                    MoonPalaceActivityEventPreconditions.SourceMap2104ResultDigest ||
                FileSha256(Resolve(projectRoot, SourceMap2104TaskRelativePath)) !=
                    MoonPalaceActivityEventPreconditions.SourceMap2104TaskDigest)
                throw new InvalidDataException("MAP21_04 Result/Task SHA-256 mismatch.");
            var digestManifest = JsonUtility.FromJson<SourceDigestManifestDocument>(
                File.ReadAllText(Resolve(projectRoot,
                    SourceMap2104DigestManifestRelativePath)));
            if (digestManifest == null || digestManifest.canonical_digest !=
                    MoonPalaceActivityEventPreconditions.SourceMap2104DigestManifestDigest ||
                digestManifest.all_biome_cluster_pool_digest !=
                    MoonPalaceActivityEventPreconditions.SourceMap2104AllBiomeClusterDigest ||
                digestManifest.MAP21_05_handoff_digest !=
                    MoonPalaceActivityEventPreconditions.SourceMap2105HandoffDigest)
                throw new InvalidDataException("MAP21_04 digest/handoff chain mismatch.");
        }

        private static List<IReadOnlyDictionary<string, string>> ReadRows(
            string projectRoot, string relativePath)
        {
            var read = new Rfc4180CsvReader().Read(File.ReadAllBytes(
                Resolve(projectRoot, relativePath)), relativePath);
            if (!read.Success || read.Records.Count < 2)
                throw new InvalidDataException("RFC4180 CSV read failed: " + relativePath);
            var headers = read.Records[0].Fields.Select(value => value.Value.Trim())
                .ToArray();
            if (headers.Any(value => value.Length == 0) ||
                headers.Distinct(StringComparer.Ordinal).Count() != headers.Length)
                throw new InvalidDataException("Invalid CSV headers: " + relativePath);
            var result = new List<IReadOnlyDictionary<string, string>>();
            foreach (var record in read.Records.Skip(1))
            {
                if (record.Fields.Count != headers.Length)
                    throw new InvalidDataException("CSV row width mismatch: " + relativePath);
                var row = new Dictionary<string, string>(StringComparer.Ordinal);
                for (var index = 0; index < headers.Length; index++)
                    row.Add(headers[index], record.Fields[index].Value.Trim());
                result.Add(row);
            }
            return result;
        }

        private static MoonPalaceNamedDigest Named(string name, string content) =>
            new MoonPalaceNamedDigest(name,
                BakingCanonicalDigest.HashCanonicalText(content));

        private static string SetDigest(string prefix, IEnumerable<string> lines) =>
            BakingCanonicalDigest.HashCanonicalLines(new[] { prefix }.Concat(
                lines.OrderBy(value => value, StringComparer.Ordinal)));

        private static string Value(IReadOnlyDictionary<string, string> row, string field)
        {
            if (!row.TryGetValue(field, out var value) || string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException("Missing/empty CSV field: " + field);
            return value;
        }

        private static int Integer(IReadOnlyDictionary<string, string> row, string field)
        {
            if (!int.TryParse(Value(row, field), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var value))
                throw new InvalidDataException("Invalid integer: " + field);
            return value;
        }

        private static bool Boolean(IReadOnlyDictionary<string, string> row, string field)
        {
            if (!bool.TryParse(Value(row, field), out var value))
                throw new InvalidDataException("Invalid bool: " + field);
            return value;
        }

        private static string Title(string value)
        {
            var lower = value.ToLowerInvariant();
            return char.ToUpperInvariant(lower[0]) + lower.Substring(1);
        }

        private static void Write(string root, string relativePath, string content) =>
            File.WriteAllText(Resolve(root, relativePath), content,
                BakingCanonicalDigest.Utf8NoBomEncoding);
        private static string Combine(string left, string right) =>
            left.TrimEnd('/') + "/" + right;
        private static string RequireProjectRoot(string root)
        {
            if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException(nameof(root));
            return Path.GetFullPath(root);
        }
        private static string Resolve(string root, string relativePath) =>
            Path.GetFullPath(Path.Combine(RequireProjectRoot(root),
                relativePath.Replace('/', Path.DirectorySeparatorChar)));

        private static string FileSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(stream);
                var output = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes)
                    output.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return output.ToString();
            }
        }

        private sealed class ActivitySpec
        {
            public ActivitySpec(string activityId, string activityKind, string biomeId,
                string primaryClusterId, string fallbackClusterId)
            {
                ActivityId = activityId; ActivityKind = activityKind; BiomeId = biomeId;
                PrimaryClusterId = primaryClusterId; FallbackClusterId = fallbackClusterId;
            }
            public string ActivityId { get; }
            public string ActivityKind { get; }
            public string BiomeId { get; }
            public string PrimaryClusterId { get; }
            public string FallbackClusterId { get; }
        }

        [Serializable]
        private sealed class SourceClusterManifestDocument
        {
            public SourceClusterRecordDocument[] cluster_catalog;
            public string canonical_digest;
        }

        [Serializable]
        private sealed class SourceClusterRecordDocument
        {
            public string cluster_id;
            public string biome_id;
            public string pool_kind;
        }

        [Serializable]
        private sealed class SourceDigestManifestDocument
        {
            public string all_biome_cluster_pool_digest;
            public string MAP21_05_handoff_digest;
            public string canonical_digest;
        }
    }

    public sealed class MoonPalaceActivityEventPublishedSample
    {
        private readonly ReadOnlyCollection<string> writtenRelativePaths;

        public MoonPalaceActivityEventPublishedSample(
            MoonPalaceActivityEventProduction production,
            MoonPalaceActivityEventDigestManifest digestManifest,
            MoonPalaceActivityEventForbiddenOperationCounters counters,
            IEnumerable<string> sourceWrittenRelativePaths)
        {
            Production = production ?? throw new ArgumentNullException(nameof(production));
            DigestManifest = digestManifest ?? throw new ArgumentNullException(
                nameof(digestManifest));
            Counters = counters ?? throw new ArgumentNullException(nameof(counters));
            writtenRelativePaths = new ReadOnlyCollection<string>((
                sourceWrittenRelativePaths ?? Array.Empty<string>()).OrderBy(value => value,
                    StringComparer.Ordinal).ToArray());
        }

        public MoonPalaceActivityEventProduction Production { get; }
        public MoonPalaceActivityEventDigestManifest DigestManifest { get; }
        public MoonPalaceActivityEventForbiddenOperationCounters Counters { get; }
        public IReadOnlyList<string> WrittenRelativePaths => writtenRelativePaths;
    }
}
