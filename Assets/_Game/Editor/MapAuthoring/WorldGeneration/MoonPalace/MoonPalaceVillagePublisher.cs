using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.MoonPalace;
using UnityEngine;

namespace StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace
{
    public static class MoonPalaceVillagePublisher
    {
        public const string AuthoringDirectoryRelativePath = "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_08";
        public const string GeneratedDirectoryRelativePath = "MapDesign/MCP/GENERATED/MAP21_08";
        public const string ProfilesFileName = "moonpalace_village_profiles.csv";
        public const string FacilitiesFileName = "moonpalace_village_facilities.csv";
        public const string RoadsFileName = "moonpalace_village_roads.csv";
        public const string DoorsFileName = "moonpalace_village_doors.csv";
        public const string MarkersFileName = "moonpalace_village_markers.csv";
        public const string StateVariantsFileName = "moonpalace_village_state_variants.csv";
        public const string VillageManifestFileName = "moonpalace_village_manifest.json";
        public const string AccessManifestFileName = "moonpalace_village_access_manifest.json";
        public const string StateManifestFileName = "moonpalace_village_state_manifest.json";
        public const string DigestManifestFileName = "moonpalace_village_digest_manifest.json";

        public const string Map1304ResultPath = "MapDesign/MCP/REPORTS/MAP13_04_IMPLEMENT_VILLAGE_SHELL_FACILITIES_AND_ACCESS_RESULT.md";
        public const string Map1305ResultPath = "MapDesign/MCP/REPORTS/MAP13_05_IMPLEMENT_VILLAGE_STATE_VARIANTS_RESULT.md";
        public const string Map1309ResultPath = "MapDesign/MCP/REPORTS/MAP13_09_MAP13_SPECIAL_REGION_EXIT_TESTS_RESULT.md";
        public const string Map1806ResultPath = "MapDesign/MCP/REPORTS/MAP18_06_IMPLEMENT_SPECIAL_STATE_EXPORT_AND_DEBUG_RESULT.md";
        public const string Map2107ResultPath = "MapDesign/MCP/REPORTS/MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS_RESULT.md";
        public const string Map2107TaskPath = "MapDesign/MCP/TASKS/MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS.md";
        public const string Map1304DefinitionsPath = "Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/VillageShellFacilities.cs";
        public const string Map1304CompilerPath = "Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/VillageShellFacilityCompiler.cs";
        public const string Map1305DefinitionsPath = "Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/VillageStateVariants.cs";
        public const string Map1305CompilerPath = "Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/VillageStateVariantCompiler.cs";
        public const string Map18ExportPath = "Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedSpecialStateExport.cs";
        public const string Map18ExporterPath = "Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedSpecialStateExporter.cs";
        public const string Map2104ManifestPath = "MapDesign/MCP/GENERATED/MAP21_04/moonpalace_all_biome_cluster_pool_manifest.json";
        public const string Map2105ManifestPath = "MapDesign/MCP/GENERATED/MAP21_05/moonpalace_activity_event_digest_manifest.json";
        public const string Map2106ManifestPath = "MapDesign/MCP/GENERATED/MAP21_06/moonpalace_boundary_digest_manifest.json";
        public const string Map2107ManifestPath = "MapDesign/MCP/GENERATED/MAP21_07/moonpalace_core_resource_digest_manifest.json";

        public static MoonPalaceVillagePublishedSample CreateReadOnlySample(string projectRoot,
            string createdUtc, bool reverseInputOrder = false)
        {
            projectRoot = RequireRoot(projectRoot);
            var observedResults = ValidateUpstream(projectRoot);
            var specs = LayoutSpecs().ToArray();
            if (reverseInputOrder) Array.Reverse(specs);
            var facilities = new List<MoonPalaceVillageFacility>();
            var roads = new List<MoonPalaceVillageRoadCell>();
            var doors = new List<MoonPalaceVillageDoor>();
            var markers = new List<MoonPalaceVillageMarker>();
            var states = new List<MoonPalaceVillageStateVariant>();
            var profiles = new List<MoonPalaceVillageLayoutProfile>();

            foreach (var spec in specs)
            {
                var layoutRoads = BuildRoads(spec).ToArray();
                var layoutFacilities = BuildFacilities(spec).ToArray();
                var layoutDoors = BuildDoors(spec, layoutFacilities).ToArray();
                var layoutMarkers = BuildMarkers(spec, layoutFacilities).ToArray();
                var facilityDigest = MoonPalaceVillageProduction.SetDigest(
                    "MAP21_08_LAYOUT_FACILITIES_V1", layoutFacilities.Select(value => value.CanonicalLine));
                var roadDigest = MoonPalaceVillageProduction.SetDigest(
                    "MAP21_08_LAYOUT_ROADS_V1", layoutRoads.Select(value => value.CanonicalLine));
                var doorDigest = MoonPalaceVillageProduction.SetDigest(
                    "MAP21_08_LAYOUT_DOORS_V1", layoutDoors.Select(value => value.CanonicalLine));
                var layoutStates = BuildStates(spec, layoutMarkers, roadDigest,
                    facilityDigest, doorDigest).ToArray();
                var stateDigest = MoonPalaceVillageProduction.SetDigest(
                    "MAP21_08_LAYOUT_STATES_V1", layoutMarkers.Select(value => value.CanonicalLine)
                        .Concat(layoutStates.Select(value => value.CanonicalLine)));
                facilities.AddRange(layoutFacilities);
                roads.AddRange(layoutRoads);
                doors.AddRange(layoutDoors);
                markers.AddRange(layoutMarkers);
                states.AddRange(layoutStates);
                profiles.Add(new MoonPalaceVillageLayoutProfile(spec.LayoutId, spec.Shape,
                    spec.Width, spec.Height, spec.ActiveSectors, layoutRoads.Length,
                    layoutFacilities.Length, 2, spec.OptionalFacilityCount,
                    layoutDoors.Length, layoutMarkers.Count(value => value.MarkerKind == "Npc"),
                    layoutMarkers.Count(value => value.MarkerKind == "Inventory"),
                    layoutMarkers.Count(value => value.IsShopkeeper), layoutStates.Length,
                    facilityDigest, roadDigest, doorDigest, stateDigest));
            }

            var production = new MoonPalaceVillageProduction(profiles, facilities, roads,
                doors, markers, states, createdUtc);
            var villageJson = production.SerializeVillageManifest();
            var accessJson = production.SerializeAccessManifest();
            var stateJson = production.SerializeStateManifest();
            var csvDigests = new[]
            {
                Named(ProfilesFileName, production.SerializeProfilesCsv()),
                Named(FacilitiesFileName, production.SerializeFacilitiesCsv()),
                Named(RoadsFileName, production.SerializeRoadsCsv()),
                Named(DoorsFileName, production.SerializeDoorsCsv()),
                Named(MarkersFileName, production.SerializeMarkersCsv()),
                Named(StateVariantsFileName, production.SerializeStateVariantsCsv()),
            };
            var jsonDigests = new[]
            {
                Named(VillageManifestFileName, villageJson),
                Named(AccessManifestFileName, accessJson),
                Named(StateManifestFileName, stateJson),
            };
            var digestManifest = new MoonPalaceVillageDigestManifest(production,
                observedResults, csvDigests, jsonDigests, createdUtc);
            return new MoonPalaceVillagePublishedSample(production, digestManifest,
                MoonPalaceVillageForbiddenOperationCounters.Zero, Array.Empty<string>(),
                SourcePaths());
        }

        public static MoonPalaceVillagePublishedSample PublishAuthoringAndSamples(
            string projectRoot, bool focusedMap2108Pass)
        {
            if (!focusedMap2108Pass)
                throw new InvalidOperationException("MAP21_09 handoff requires focused MAP21_08 PASS.");
            projectRoot = RequireRoot(projectRoot);
            var sample = CreateReadOnlySample(projectRoot,
                DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(Resolve(projectRoot, AuthoringDirectoryRelativePath));
            Directory.CreateDirectory(Resolve(projectRoot, GeneratedDirectoryRelativePath));
            var paths = new[]
            {
                Combine(AuthoringDirectoryRelativePath, ProfilesFileName),
                Combine(AuthoringDirectoryRelativePath, FacilitiesFileName),
                Combine(AuthoringDirectoryRelativePath, RoadsFileName),
                Combine(AuthoringDirectoryRelativePath, DoorsFileName),
                Combine(AuthoringDirectoryRelativePath, MarkersFileName),
                Combine(AuthoringDirectoryRelativePath, StateVariantsFileName),
                Combine(GeneratedDirectoryRelativePath, VillageManifestFileName),
                Combine(GeneratedDirectoryRelativePath, AccessManifestFileName),
                Combine(GeneratedDirectoryRelativePath, StateManifestFileName),
                Combine(GeneratedDirectoryRelativePath, DigestManifestFileName),
            };
            Write(projectRoot, paths[0], sample.Production.SerializeProfilesCsv());
            Write(projectRoot, paths[1], sample.Production.SerializeFacilitiesCsv());
            Write(projectRoot, paths[2], sample.Production.SerializeRoadsCsv());
            Write(projectRoot, paths[3], sample.Production.SerializeDoorsCsv());
            Write(projectRoot, paths[4], sample.Production.SerializeMarkersCsv());
            Write(projectRoot, paths[5], sample.Production.SerializeStateVariantsCsv());
            Write(projectRoot, paths[6], sample.Production.SerializeVillageManifest());
            Write(projectRoot, paths[7], sample.Production.SerializeAccessManifest());
            Write(projectRoot, paths[8], sample.Production.SerializeStateManifest());
            Write(projectRoot, paths[9], sample.DigestManifest.Serialize());
            return new MoonPalaceVillagePublishedSample(sample.Production,
                sample.DigestManifest, sample.Counters, paths, sample.SourceReadRelativePaths);
        }

        private static IEnumerable<LayoutSpec> LayoutSpecs()
        {
            yield return new LayoutSpec("VLG_MOONPALACE_1X1_OVERVIEW", "1x1", 48, 32, 1, 3, false);
            yield return new LayoutSpec("VLG_MOONPALACE_2X1_MARKET", "2x1", 96, 32, 2, 4, false);
            yield return new LayoutSpec("VLG_MOONPALACE_1X2_ASCENT", "1x2", 48, 64, 2, 3, true);
        }

        private static IEnumerable<MoonPalaceVillageRoadCell> BuildRoads(LayoutSpec spec)
        {
            var count = spec.Vertical ? spec.Height : spec.Width;
            for (var index = 0; index < count; index++)
            {
                var x = spec.Vertical ? 24 : index;
                var y = spec.Vertical ? index : 16;
                yield return new MoonPalaceVillageRoadCell(spec.LayoutId,
                    spec.Token + "_ROAD_" + index.ToString("D3", CultureInfo.InvariantCulture),
                    index, x, y);
            }
        }

        private static IEnumerable<MoonPalaceVillageFacility> BuildFacilities(LayoutSpec spec)
        {
            var kinds = new List<string> { "Kitchen", "Repair", "OptionalRest", "OptionalStorage", "OptionalMarket" };
            if (spec.OptionalFacilityCount == 4) kinds.Add("OptionalLore");
            var anchors = Anchors(spec, kinds.Count).ToArray();
            for (var index = 0; index < kinds.Count; index++)
            {
                var kind = kinds[index];
                yield return new MoonPalaceVillageFacility(spec.LayoutId,
                    spec.Token + "_FAC_" + kind.ToUpperInvariant(), kind,
                    index < 2 ? "Fixed" : "Optional",
                    spec.Token + "_SLOT_" + (index + 1).ToString("D2", CultureInfo.InvariantCulture),
                    spec.Vertical ? 17 : anchors[index], spec.Vertical ? anchors[index] : 10);
            }
        }

        private static IEnumerable<MoonPalaceVillageDoor> BuildDoors(LayoutSpec spec,
            IEnumerable<MoonPalaceVillageFacility> facilities)
        {
            var values = facilities.OrderBy(value => value.FacilityId, StringComparer.Ordinal).ToArray();
            foreach (var facility in values)
            {
                var anchor = spec.Vertical ? facility.LocalY : facility.LocalX;
                var doorX = spec.Vertical ? 23 : anchor;
                var doorY = spec.Vertical ? anchor : 15;
                var returnX = spec.Vertical ? 24 : anchor;
                var returnY = spec.Vertical ? anchor : 16;
                yield return new MoonPalaceVillageDoor(spec.LayoutId,
                    spec.Token + "_DOOR_" + facility.FacilityKind.ToUpperInvariant(),
                    facility.FacilityId,
                    spec.Token + "_RETURN_" + facility.FacilityKind.ToUpperInvariant(),
                    doorX, doorY, returnX, returnY);
            }
        }

        private static IEnumerable<MoonPalaceVillageMarker> BuildMarkers(LayoutSpec spec,
            IReadOnlyCollection<MoonPalaceVillageFacility> facilities)
        {
            var kitchen = facilities.Single(value => value.FacilityKind == "Kitchen");
            var repair = facilities.Single(value => value.FacilityKind == "Repair");
            var market = facilities.Single(value => value.FacilityKind == "OptionalMarket");
            yield return Marker(spec, kitchen, "NPC_KITCHEN", "Npc", "KitchenResident", false);
            yield return Marker(spec, repair, "NPC_REPAIR", "Npc", "RepairResident", false);
            yield return Marker(spec, market, "NPC_SHOPKEEPER", "Npc", "Shopkeeper", true);
            yield return Marker(spec, kitchen, "INV_KITCHEN", "Inventory", "KitchenInventory", false);
            yield return Marker(spec, repair, "INV_REPAIR", "Inventory", "RepairInventory", false);
        }

        private static MoonPalaceVillageMarker Marker(LayoutSpec spec,
            MoonPalaceVillageFacility facility, string suffix, string kind, string role,
            bool shopkeeper) => new MoonPalaceVillageMarker(spec.LayoutId,
                spec.Token + "_" + suffix, kind, facility.FacilityId, role,
                facility.LocalX, facility.LocalY, shopkeeper);

        private static IEnumerable<MoonPalaceVillageStateVariant> BuildStates(LayoutSpec spec,
            IEnumerable<MoonPalaceVillageMarker> markers, string roadDigest,
            string facilityDigest, string doorDigest)
        {
            var npcIds = markers.Where(value => value.MarkerKind == "Npc")
                .Select(value => value.MarkerId).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var inventoryIds = markers.Where(value => value.MarkerKind == "Inventory")
                .Select(value => value.MarkerId).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var npc = string.Join("|", npcIds);
            var inventory = string.Join("|", inventoryIds);
            yield return State(spec, "Normal", npc, "Normal|Normal|Normal", inventory,
                "Standard|Standard", "Standard", "NONE", roadDigest, facilityDigest, doorDigest);
            yield return State(spec, "Friendly", npc, "Friendly|Friendly|Friendly", inventory,
                "FriendlyAccess|FriendlyAccess", "Welcome", "NONE", roadDigest, facilityDigest, doorDigest);
            yield return State(spec, "IndividualHostile", npc, "Hostile|Normal|Normal", inventory,
                "Standard|Standard", "Standard", npcIds[0], roadDigest, facilityDigest, doorDigest);
            yield return State(spec, "AllHostile", npc, "Hostile|Hostile|Hostile", inventory,
                "Unavailable|Unavailable", "Alert", "NONE", roadDigest, facilityDigest, doorDigest);
            yield return State(spec, "Evacuation", npc, "Evacuated|Evacuated|Evacuated", inventory,
                "Evacuated|Evacuated", "Evacuated", "NONE", roadDigest, facilityDigest, doorDigest);
        }

        private static MoonPalaceVillageStateVariant State(LayoutSpec spec, string kind,
            string npcIds, string npcStates, string inventoryIds, string inventoryStates,
            string doorState, string target, string roadDigest, string facilityDigest,
            string doorDigest) => new MoonPalaceVillageStateVariant(spec.LayoutId, kind,
                npcIds, npcStates, inventoryIds, inventoryStates, doorState, target,
                roadDigest, facilityDigest, doorDigest);

        private static IEnumerable<int> Anchors(LayoutSpec spec, int count)
        {
            int[] values;
            if (spec.Vertical) values = new[] { 8, 20, 32, 44, 56 };
            else if (spec.Width == 96) values = new[] { 8, 24, 40, 56, 72, 88 };
            else values = new[] { 6, 15, 24, 33, 42 };
            return values.Take(count);
        }

        private static IReadOnlyList<MoonPalaceNamedDigest> ValidateUpstream(string root)
        {
            var observed = new[]
            {
                Historical(root, "MAP13_04_RESULT", Map1304ResultPath,
                    "MAP13_04_IMPLEMENT_VILLAGE_SHELL_FACILITIES_AND_ACCESS"),
                Historical(root, "MAP13_05_RESULT", Map1305ResultPath,
                    "MAP13_05_IMPLEMENT_VILLAGE_STATE_VARIANTS"),
                Historical(root, "MAP13_09_RESULT", Map1309ResultPath,
                    "MAP13_09_MAP13_SPECIAL_REGION_EXIT_TESTS",
                    MoonPalaceVillagePreconditions.SourceMap13AuditDigest),
                Historical(root, "MAP18_06_RESULT", Map1806ResultPath,
                    "MAP18_06_IMPLEMENT_SPECIAL_STATE_EXPORT_AND_DEBUG",
                    MoonPalaceVillagePreconditions.SourceMap18ExportDigest,
                    MoonPalaceVillagePreconditions.SourceMap18DebugDigest),
            };
            StrictResult(root, Map2107ResultPath,
                "MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS",
                MoonPalaceVillagePreconditions.StrictMap2107ResultDigest);
            if (FileSha256(Resolve(root, Map2107TaskPath)) !=
                MoonPalaceVillagePreconditions.StrictMap2107TaskDigest)
                throw new InvalidDataException("MAP21_07 installed Task strict SHA mismatch.");
            foreach (var path in new[] { Map1304DefinitionsPath, Map1304CompilerPath,
                Map1305DefinitionsPath, Map1305CompilerPath, Map18ExportPath,
                Map18ExporterPath }) FileSha256(Resolve(root, path));
            var map2104 = JsonUtility.FromJson<CanonicalDocument>(Read(root, Map2104ManifestPath));
            var map2105 = JsonUtility.FromJson<CanonicalDocument>(Read(root, Map2105ManifestPath));
            var map2106 = JsonUtility.FromJson<CanonicalDocument>(Read(root, Map2106ManifestPath));
            var map2107 = JsonUtility.FromJson<Map2107Document>(Read(root, Map2107ManifestPath));
            if (map2104 == null || map2104.canonical_digest != MoonPalaceVillagePreconditions.SourceMap2104Digest ||
                map2105 == null || map2105.canonical_digest != MoonPalaceVillagePreconditions.SourceMap2105Digest ||
                map2106 == null || map2106.canonical_digest != MoonPalaceVillagePreconditions.SourceMap2106Digest ||
                map2107 == null || map2107.canonical_digest != MoonPalaceVillagePreconditions.SourceMap2107Digest ||
                map2107.MAP21_08_handoff_digest != MoonPalaceVillagePreconditions.StrictMap2108HandoffDigest)
                throw new InvalidDataException("MAP21 source digest chain mismatch.");
            return observed;
        }

        private static MoonPalaceNamedDigest Historical(string root, string name,
            string path, string taskId, params string[] evidence)
        {
            var resolved = Resolve(root, path);
            if (!File.Exists(resolved)) throw new FileNotFoundException(path, resolved);
            var lines = File.ReadAllLines(resolved);
            if (lines.Count(value => value == "TASK: " + taskId) != 1 ||
                lines.Count(value => value == "STATUS: PASS") != 1)
                throw new InvalidDataException("Historical Result TASK/STATUS mismatch: " + path);
            var text = File.ReadAllText(resolved);
            if (evidence.Any(value => !text.Contains(value)))
                throw new InvalidDataException("Historical Result evidence mismatch: " + path);
            return new MoonPalaceNamedDigest(name, FileSha256(resolved));
        }

        private static void StrictResult(string root, string path, string taskId,
            string expectedSha)
        {
            var resolved = Resolve(root, path);
            if (FileSha256(resolved) != expectedSha)
                throw new InvalidDataException("Immediate predecessor Result strict SHA mismatch.");
            var lines = File.ReadAllLines(resolved);
            if (lines.Count(value => value == "TASK: " + taskId) != 1 ||
                lines.Count(value => value == "STATUS: PASS") != 1)
                throw new InvalidDataException("Immediate predecessor Result identity mismatch.");
        }

        private static string[] SourcePaths() => new[]
        {
            Map1304ResultPath, Map1305ResultPath, Map1309ResultPath, Map1806ResultPath,
            Map2107ResultPath, Map2107TaskPath, Map1304DefinitionsPath, Map1304CompilerPath,
            Map1305DefinitionsPath, Map1305CompilerPath, Map18ExportPath, Map18ExporterPath,
            Map2104ManifestPath, Map2105ManifestPath, Map2106ManifestPath, Map2107ManifestPath,
        };
        private static MoonPalaceNamedDigest Named(string name, string content) =>
            new MoonPalaceNamedDigest(name, BakingCanonicalDigest.HashCanonicalText(content));
        private static void Write(string root, string relative, string content) =>
            File.WriteAllText(Resolve(root, relative), content,
                BakingCanonicalDigest.Utf8NoBomEncoding);
        private static string Read(string root, string relative) => File.ReadAllText(Resolve(root, relative));
        private static string Combine(string left, string right) => left.TrimEnd('/') + "/" + right;
        private static string RequireRoot(string root)
        {
            if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException(nameof(root));
            return Path.GetFullPath(root);
        }
        private static string Resolve(string root, string relative) => Path.GetFullPath(
            Path.Combine(RequireRoot(root), relative.Replace('/', Path.DirectorySeparatorChar)));
        private static string FileSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(stream).Select(value =>
                    value.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private sealed class LayoutSpec
        {
            public LayoutSpec(string layoutId, string shape, int width, int height,
                int activeSectors, int optionalFacilityCount, bool vertical)
            {
                LayoutId = layoutId;
                Shape = shape;
                Width = width;
                Height = height;
                ActiveSectors = activeSectors;
                OptionalFacilityCount = optionalFacilityCount;
                Vertical = vertical;
                Token = layoutId.Substring("VLG_MOONPALACE_".Length);
            }
            public string LayoutId { get; }
            public string Shape { get; }
            public int Width { get; }
            public int Height { get; }
            public int ActiveSectors { get; }
            public int OptionalFacilityCount { get; }
            public bool Vertical { get; }
            public string Token { get; }
        }
        [Serializable] private sealed class CanonicalDocument { public string canonical_digest; }
        [Serializable] private sealed class Map2107Document
        {
            public string MAP21_08_handoff_digest;
            public string canonical_digest;
        }
    }

    public sealed class MoonPalaceVillagePublishedSample
    {
        private readonly ReadOnlyCollection<string> written;
        private readonly ReadOnlyCollection<string> sourceReads;
        public MoonPalaceVillagePublishedSample(MoonPalaceVillageProduction production,
            MoonPalaceVillageDigestManifest digestManifest,
            MoonPalaceVillageForbiddenOperationCounters counters,
            IEnumerable<string> writtenPaths, IEnumerable<string> sourceReadPaths)
        {
            Production = production ?? throw new ArgumentNullException(nameof(production));
            DigestManifest = digestManifest ?? throw new ArgumentNullException(nameof(digestManifest));
            Counters = counters ?? throw new ArgumentNullException(nameof(counters));
            written = new ReadOnlyCollection<string>((writtenPaths ?? Array.Empty<string>())
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
            sourceReads = new ReadOnlyCollection<string>((sourceReadPaths ?? Array.Empty<string>())
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }
        public MoonPalaceVillageProduction Production { get; }
        public MoonPalaceVillageDigestManifest DigestManifest { get; }
        public MoonPalaceVillageForbiddenOperationCounters Counters { get; }
        public IReadOnlyList<string> WrittenRelativePaths => written;
        public IReadOnlyList<string> SourceReadRelativePaths => sourceReads;
    }
}
