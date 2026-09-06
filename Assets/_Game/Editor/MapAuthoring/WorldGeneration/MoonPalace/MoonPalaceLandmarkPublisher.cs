using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.MoonPalace;
using StarNight.Map.WorldGeneration.SpecialRegions;
using UnityEngine;

namespace StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace
{
    public static class MoonPalaceLandmarkPublisher
    {
        public const string AuthoringDirectoryRelativePath = "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_09";
        public const string GeneratedDirectoryRelativePath = "MapDesign/MCP/GENERATED/MAP21_09";
        public const string ProfilesFileName = "moonpalace_landmark_profiles.csv";
        public const string ChunksFileName = "moonpalace_landmark_chunks.csv";
        public const string NodesFileName = "moonpalace_landmark_nodes.csv";
        public const string EdgesFileName = "moonpalace_landmark_edges.csv";
        public const string RoutesFileName = "moonpalace_landmark_routes.csv";
        public const string ForgeLedgerFileName = "moonpalace_forge_resource_ledger.csv";
        public const string BossGateFileName = "moonpalace_boss_gate_encounter.csv";
        public const string OptionalFileName = "moonpalace_optional_landmarks.csv";
        public const string MarkersFileName = "moonpalace_landmark_markers.csv";
        public const string StatePersistenceFileName = "moonpalace_landmark_state_persistence.csv";
        public const string LandmarkManifestFileName = "moonpalace_landmark_manifest.json";
        public const string ForgeManifestFileName = "moonpalace_forge_manifest.json";
        public const string BossManifestFileName = "moonpalace_boss_manifest.json";
        public const string OptionalManifestFileName = "moonpalace_optional_landmarks_manifest.json";
        public const string DigestManifestFileName = "moonpalace_landmark_digest_manifest.json";

        public const string Map1307ResultPath = "MapDesign/MCP/REPORTS/MAP13_07_AUTHOR_FORGE_BOSS_AND_OPTIONAL_REGIONS_RESULT.md";
        public const string Map1308ResultPath = "MapDesign/MCP/REPORTS/MAP13_08_CREATE_SPECIAL_VALIDATOR_AND_PREVIEW_RESULT.md";
        public const string Map1309ResultPath = "MapDesign/MCP/REPORTS/MAP13_09_MAP13_SPECIAL_REGION_EXIT_TESTS_RESULT.md";
        public const string Map1806ResultPath = "MapDesign/MCP/REPORTS/MAP18_06_IMPLEMENT_SPECIAL_STATE_EXPORT_AND_DEBUG_RESULT.md";
        public const string Map2107ResultPath = "MapDesign/MCP/REPORTS/MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS_RESULT.md";
        public const string Map2108ResultPath = "MapDesign/MCP/REPORTS/MAP21_08_COMPLETE_MOONPALACE_VILLAGE_RESULT.md";
        public const string Map2108TaskPath = "MapDesign/MCP/TASKS/MAP21_08_COMPLETE_MOONPALACE_VILLAGE.md";
        public const string LandmarkDefinitionsPath = "Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialLandmarkRegionDefinitions.cs";
        public const string LandmarkCatalogPath = "Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialLandmarkRegionStarterCatalog.cs";
        public const string LandmarkCompilerPath = "Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialLandmarkRegionCompiler.cs";
        public const string Map18ExportPath = "Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedSpecialStateExport.cs";
        public const string Map18ExporterPath = "Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedSpecialStateExporter.cs";
        public const string Map2107ProfilesPath = "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_07/moonpalace_core_resource_profiles.csv";
        public const string Map2107DigestPath = "MapDesign/MCP/GENERATED/MAP21_07/moonpalace_core_resource_digest_manifest.json";
        public const string Map2108DigestPath = "MapDesign/MCP/GENERATED/MAP21_08/moonpalace_village_digest_manifest.json";

        public static MoonPalaceLandmarkPublishedSample CreateReadOnlySample(string projectRoot,
            string createdUtc, bool reverseInputOrder = false)
        {
            projectRoot = RequireRoot(projectRoot);
            var observed = ValidateUpstream(projectRoot);
            var source = SpecialLandmarkRegionStarterCatalog.Entries.ToArray();
            ValidateSource(source);
            if (reverseInputOrder) Array.Reverse(source);

            var profiles = new List<MoonPalaceLandmarkProfile>();
            var chunks = new List<MoonPalaceLandmarkChunk>();
            var nodes = new List<MoonPalaceLandmarkNode>();
            var edges = new List<MoonPalaceLandmarkEdge>();
            var routes = new List<MoonPalaceLandmarkRoute>();
            var states = new List<MoonPalaceLandmarkState>();
            var transitions = new List<MoonPalaceLandmarkTransition>();
            var resets = new List<MoonPalaceLandmarkReset>();
            var markers = new List<MoonPalaceLandmarkMarker>();
            var ledgers = new List<MoonPalaceForgeResourceLedger>();
            var optional = new List<MoonPalaceOptionalLandmarkVariant>();

            foreach (var definition in source)
            {
                var landmark = definition.Landmark.ToString();
                profiles.Add(new MoonPalaceLandmarkProfile(landmark, definition.RegionId.Value,
                    definition.Binding.ToString(), definition.DesignWidth, definition.DesignHeight,
                    definition.ActiveDesignChunks.Count, definition.Nodes.Count, definition.Edges.Count,
                    definition.Routes.Count, definition.States.Count, definition.Transitions.Count,
                    definition.Resets.Count, definition.Markers.Count));
                chunks.AddRange(definition.ActiveDesignChunks.Select(value => new MoonPalaceLandmarkChunk(
                    landmark, landmark + "_CHUNK_" + value.X.ToString("D2", CultureInfo.InvariantCulture) +
                    "_" + value.Y.ToString("D2", CultureInfo.InvariantCulture), value.X, value.Y)));
                nodes.AddRange(definition.Nodes.Select(value => new MoonPalaceLandmarkNode(landmark,
                    value.NodeId, value.Role.ToString(), value.Coordinate.X - definition.DesignOrigin.X,
                    value.Coordinate.Y - definition.DesignOrigin.Y, value.Required)));
                edges.AddRange(definition.Edges.Select(value => new MoonPalaceLandmarkEdge(landmark,
                    value.EdgeId, value.FromNodeId, value.ToNodeId, value.RouteKind.ToString(),
                    value.Order, value.AccessClass.ToString(), value.Required, value.Dependency.ToString())));
                routes.AddRange(definition.Routes.Select(value => new MoonPalaceLandmarkRoute(landmark,
                    value.RouteId, value.Kind.ToString(), value.StartNodeId, value.EndNodeId, value.EdgeIds)));
                states.AddRange(definition.States.Select(value => new MoonPalaceLandmarkState(landmark,
                    value.StateId, value.Role.ToString(), StateOrder(landmark, value.Role.ToString()), value.Persistent)));
                transitions.AddRange(definition.Transitions.Select(value => new MoonPalaceLandmarkTransition(landmark,
                    value.TransitionId, value.FromStateId, value.ToStateId, value.Trigger.ToString(), value.Order)));
                resets.AddRange(definition.Resets.Select(value => new MoonPalaceLandmarkReset(landmark,
                    value.ResetId, value.Policy.ToString(), value.FailureNodeId, value.RecoveryNodeId,
                    value.FromStateId, value.ToStateId, value.ReturnsAllForgeInputs,
                    value.PreservesSealAcceptance, value.PreventsReroll)));
                markers.AddRange(definition.Markers.Select(value => new MoonPalaceLandmarkMarker(landmark,
                    value.MarkerId, value.Kind.ToString(), value.NodeId, value.StateId, value.Order,
                    value.Required, value.Dependency.ToString(), value.PersistenceKey.Value)));

                if (definition.Landmark == SpecialLandmarkKind.MoonSealForge)
                {
                    foreach (var value in definition.ForgeLedgers)
                    {
                        var resource = value.Resource.ToString();
                        ledgers.Add(new MoonPalaceForgeResourceLedger(resource, ResourceKey(resource),
                            value.AvailableStateId, value.ReservedStateId, value.ConsumedStateId,
                            value.ReturnedStateId, FailureBranch(resource), true));
                    }
                }
                if (definition.Landmark == SpecialLandmarkKind.WanderingMerchantCave)
                {
                    optional.AddRange(definition.MerchantVariants.Select(value =>
                        new MoonPalaceOptionalLandmarkVariant(landmark, "MerchantVariant",
                            value.ToString(), (int)value, false, false)));
                }
            }
            optional.Add(new MoonPalaceOptionalLandmarkVariant("MaruTimeShrine", "MaruChoice", "Ignored", 1, true, true));
            optional.Add(new MoonPalaceOptionalLandmarkVariant("MaruTimeShrine", "MaruChoice", "ShortHint", 2, true, true));
            optional.Add(new MoonPalaceOptionalLandmarkVariant("MaruTimeShrine", "MaruChoice", "StrongHint", 3, true, true));

            var production = new MoonPalaceLandmarkProduction(profiles, chunks, nodes, edges,
                routes, states, transitions, resets, markers, ledgers, optional, createdUtc);
            var landmarkJson = production.SerializeLandmarkManifest();
            var forgeJson = production.SerializeForgeManifest();
            var bossJson = production.SerializeBossManifest();
            var optionalJson = production.SerializeOptionalManifest();
            var csvDigests = new[]
            {
                Named(ProfilesFileName, production.SerializeProfilesCsv()),
                Named(ChunksFileName, production.SerializeChunksCsv()),
                Named(NodesFileName, production.SerializeNodesCsv()),
                Named(EdgesFileName, production.SerializeEdgesCsv()),
                Named(RoutesFileName, production.SerializeRoutesCsv()),
                Named(ForgeLedgerFileName, production.SerializeForgeLedgerCsv()),
                Named(BossGateFileName, production.SerializeBossGateEncounterCsv()),
                Named(OptionalFileName, production.SerializeOptionalLandmarksCsv()),
                Named(MarkersFileName, production.SerializeMarkersCsv()),
                Named(StatePersistenceFileName, production.SerializeStatePersistenceCsv()),
            };
            var jsonDigests = new[]
            {
                Named(LandmarkManifestFileName, landmarkJson), Named(ForgeManifestFileName, forgeJson),
                Named(BossManifestFileName, bossJson), Named(OptionalManifestFileName, optionalJson),
            };
            var digest = new MoonPalaceLandmarkDigestManifest(production, observed,
                csvDigests, jsonDigests, createdUtc);
            return new MoonPalaceLandmarkPublishedSample(production, digest,
                MoonPalaceLandmarkForbiddenOperationCounters.Zero, Array.Empty<string>(), SourcePaths());
        }

        public static MoonPalaceLandmarkPublishedSample PublishAuthoringAndSamples(
            string projectRoot, bool focusedMap2109Pass)
        {
            if (!focusedMap2109Pass) throw new InvalidOperationException(
                "MAP21_10 handoff requires focused MAP21_09 PASS.");
            projectRoot = RequireRoot(projectRoot);
            var sample = CreateReadOnlySample(projectRoot,
                DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(Resolve(projectRoot, AuthoringDirectoryRelativePath));
            Directory.CreateDirectory(Resolve(projectRoot, GeneratedDirectoryRelativePath));
            var paths = new[]
            {
                Combine(AuthoringDirectoryRelativePath, ProfilesFileName),
                Combine(AuthoringDirectoryRelativePath, ChunksFileName),
                Combine(AuthoringDirectoryRelativePath, NodesFileName),
                Combine(AuthoringDirectoryRelativePath, EdgesFileName),
                Combine(AuthoringDirectoryRelativePath, RoutesFileName),
                Combine(AuthoringDirectoryRelativePath, ForgeLedgerFileName),
                Combine(AuthoringDirectoryRelativePath, BossGateFileName),
                Combine(AuthoringDirectoryRelativePath, OptionalFileName),
                Combine(AuthoringDirectoryRelativePath, MarkersFileName),
                Combine(AuthoringDirectoryRelativePath, StatePersistenceFileName),
                Combine(GeneratedDirectoryRelativePath, LandmarkManifestFileName),
                Combine(GeneratedDirectoryRelativePath, ForgeManifestFileName),
                Combine(GeneratedDirectoryRelativePath, BossManifestFileName),
                Combine(GeneratedDirectoryRelativePath, OptionalManifestFileName),
                Combine(GeneratedDirectoryRelativePath, DigestManifestFileName),
            };
            var contents = new[]
            {
                sample.Production.SerializeProfilesCsv(), sample.Production.SerializeChunksCsv(),
                sample.Production.SerializeNodesCsv(), sample.Production.SerializeEdgesCsv(),
                sample.Production.SerializeRoutesCsv(), sample.Production.SerializeForgeLedgerCsv(),
                sample.Production.SerializeBossGateEncounterCsv(), sample.Production.SerializeOptionalLandmarksCsv(),
                sample.Production.SerializeMarkersCsv(), sample.Production.SerializeStatePersistenceCsv(),
                sample.Production.SerializeLandmarkManifest(), sample.Production.SerializeForgeManifest(),
                sample.Production.SerializeBossManifest(), sample.Production.SerializeOptionalManifest(),
                sample.DigestManifest.Serialize(),
            };
            for (var index = 0; index < paths.Length; index++) Write(projectRoot, paths[index], contents[index]);
            return new MoonPalaceLandmarkPublishedSample(sample.Production, sample.DigestManifest,
                sample.Counters, paths, sample.SourceReadRelativePaths);
        }

        private static void ValidateSource(IReadOnlyCollection<SpecialLandmarkRegionDefinition> source)
        {
            if (source.Count != 4 || source.Sum(x => x.ActiveDesignChunks.Count) != 29 ||
                source.Sum(x => x.Nodes.Count) != 39 || source.Sum(x => x.Edges.Count) != 58 ||
                source.Sum(x => x.Routes.Count) != 18 || source.Sum(x => x.States.Count) != 25 ||
                source.Sum(x => x.Transitions.Count) != 18 || source.Sum(x => x.Resets.Count) != 9 ||
                source.Sum(x => x.Markers.Count) != 31)
                throw new InvalidDataException("MAP13 landmark aggregate mismatch.");
            var expected = new Dictionary<string, int[]>(StringComparer.Ordinal)
            {
                { "MoonSealForge", new[] { 9, 13, 22, 6, 14, 9, 3, 12 } },
                { "BossSealArena", new[] { 12, 12, 16, 5, 4, 4, 3, 7 } },
                { "WanderingMerchantCave", new[] { 3, 7, 8, 3, 3, 2, 1, 7 } },
                { "MaruTimeShrine", new[] { 5, 7, 12, 4, 4, 3, 2, 5 } },
            };
            foreach (var value in source)
            {
                if (!expected.TryGetValue(value.Landmark.ToString(), out var counts) ||
                    !counts.SequenceEqual(new[] { value.ActiveDesignChunks.Count, value.Nodes.Count,
                        value.Edges.Count, value.Routes.Count, value.States.Count,
                        value.Transitions.Count, value.Resets.Count, value.Markers.Count }))
                    throw new InvalidDataException("MAP13 landmark identity mismatch.");
            }
            var forge = source.Single(x => x.Landmark == SpecialLandmarkKind.MoonSealForge);
            if (forge.RequiredReward == null || forge.RequiredReward.SlotId.Value != MoonPalaceLandmarkPreconditions.MoonSealSlot ||
                forge.RequiredReward.PersistenceKey.Value != MoonPalaceLandmarkPreconditions.MoonSealRewardKey ||
                forge.RequiredReward.Amount != 1 || !forge.RequiredReward.Required || forge.ForgeLedgers.Count != 3)
                throw new InvalidDataException("MAP13 Forge reward contract mismatch.");
            var boss = source.Single(x => x.Landmark == SpecialLandmarkKind.BossSealArena);
            if (boss.IntroducesNewMovementRule || boss.Markers.Count(x => x.Kind == SpecialLandmarkMarkerKind.MoonSealRequirement) != 1)
                throw new InvalidDataException("MAP13 Boss contract mismatch.");
            foreach (var value in source.Where(x => x.Binding == SpecialLandmarkBindingKind.DeferredOptionalLocal))
                if (value.ReservedWidth != 0 || value.ReservedHeight != 0)
                    throw new InvalidDataException("Optional landmark claims a reserved footprint.");
        }

        private static IReadOnlyList<MoonPalaceNamedDigest> ValidateUpstream(string root)
        {
            var observed = new[]
            {
                Historical(root, "MAP13_07_RESULT", Map1307ResultPath, "MAP13_07_AUTHOR_FORGE_BOSS_AND_OPTIONAL_REGIONS"),
                Historical(root, "MAP13_08_RESULT", Map1308ResultPath, "MAP13_08_CREATE_SPECIAL_VALIDATOR_AND_PREVIEW", MoonPalaceLandmarkPreconditions.SourceMap13AuditDigest),
                Historical(root, "MAP13_09_RESULT", Map1309ResultPath, "MAP13_09_MAP13_SPECIAL_REGION_EXIT_TESTS", MoonPalaceLandmarkPreconditions.SourceMap13AuditDigest),
                Historical(root, "MAP18_06_RESULT", Map1806ResultPath, "MAP18_06_IMPLEMENT_SPECIAL_STATE_EXPORT_AND_DEBUG", MoonPalaceLandmarkPreconditions.SourceMap18ExportDigest, MoonPalaceLandmarkPreconditions.SourceMap18DebugDigest),
                Historical(root, "MAP21_07_RESULT", Map2107ResultPath, "MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS", MoonPalaceLandmarkPreconditions.SourceMap2107Digest),
                Historical(root, "MAP21_08_RESULT", Map2108ResultPath, "MAP21_08_COMPLETE_MOONPALACE_VILLAGE", MoonPalaceLandmarkPreconditions.SourceMap2108Digest),
            };
            if (observed.Single(x => x.Name == "MAP21_08_RESULT").Digest != MoonPalaceLandmarkPreconditions.StrictMap2108ResultDigest)
                throw new InvalidDataException("MAP21_08 Result strict SHA mismatch.");
            if (FileSha(Resolve(root, Map2108TaskPath)) != MoonPalaceLandmarkPreconditions.StrictMap2108TaskDigest)
                throw new InvalidDataException("MAP21_08 installed Task strict SHA mismatch.");
            foreach (var path in new[] { LandmarkDefinitionsPath, LandmarkCatalogPath, LandmarkCompilerPath,
                Map18ExportPath, Map18ExporterPath }) FileSha(Resolve(root, path));
            var rewardText = Read(root, Map2107ProfilesPath);
            foreach (var resource in new[] { "MoonCore", "CassiaSap", "StarNuruk" })
                if (!rewardText.Contains(ResourceKey(resource))) throw new InvalidDataException("MAP21_07 reward key missing.");
            var map2107 = JsonUtility.FromJson<Map2107Document>(Read(root, Map2107DigestPath));
            var map2108 = JsonUtility.FromJson<Map2108Document>(Read(root, Map2108DigestPath));
            if (map2107 == null || map2107.canonical_digest != MoonPalaceLandmarkPreconditions.SourceMap2107Digest ||
                map2108 == null || map2108.canonical_digest != MoonPalaceLandmarkPreconditions.SourceMap2108Digest ||
                map2108.MAP21_09_handoff_digest != MoonPalaceLandmarkPreconditions.StrictMap2109HandoffDigest)
                throw new InvalidDataException("MAP21_07/08 digest chain mismatch.");
            return new ReadOnlyCollection<MoonPalaceNamedDigest>(observed.OrderBy(x => x.Name, StringComparer.Ordinal).ToArray());
        }

        private static MoonPalaceNamedDigest Historical(string root, string name, string path,
            string taskId, params string[] evidence)
        {
            var resolved = Resolve(root, path);
            if (!File.Exists(resolved)) throw new FileNotFoundException(path, resolved);
            var lines = File.ReadAllLines(resolved);
            if (lines.Count(x => x == "TASK: " + taskId) != 1 || lines.Count(x => x == "STATUS: PASS") != 1)
                throw new InvalidDataException("Source Result identity mismatch: " + path);
            var text = File.ReadAllText(resolved);
            if (evidence.Any(x => !text.Contains(x))) throw new InvalidDataException("Source Result evidence mismatch: " + path);
            return new MoonPalaceNamedDigest(name, FileSha(resolved));
        }

        private static int StateOrder(string landmark, string role)
        {
            if (landmark == "BossSealArena") return Array.IndexOf(new[] { "GateLocked", "GateAccepted", "EncounterActive", "Defeated" }, role);
            if (landmark == "MaruTimeShrine") return Array.IndexOf(new[] { "Offered", "Ignored", "ShortHint", "StrongHint" }, role);
            if (landmark == "WanderingMerchantCave") return Array.IndexOf(new[] { "MerchantAvailable", "Visited", "Departed" }, role);
            return string.CompareOrdinal(role, "") + role.Sum(x => (int)x);
        }
        private static string ResourceKey(string resource) => resource == "MoonCore" ?
            "SR_STATE_MOON_CORE_SITE_5_REWARD_MOON_CORE_REWARD" : resource == "CassiaSap" ?
            "SR_STATE_CASSIA_SAP_SITE_5_REWARD_CASSIA_SAP_REWARD" : resource == "StarNuruk" ?
            "SR_STATE_STAR_NURUK_SITE_5_REWARD_STAR_NURUK_REWARD" : throw new ArgumentException("Unknown resource.");
        private static string FailureBranch(string resource) => resource == "MoonCore" ? "Grind" :
            resource == "CassiaSap" ? "Mix" : resource == "StarNuruk" ? "Press" : throw new ArgumentException("Unknown resource.");
        private static string[] SourcePaths() => new[] { Map1307ResultPath, Map1308ResultPath, Map1309ResultPath,
            Map1806ResultPath, Map2107ResultPath, Map2108ResultPath, Map2108TaskPath, LandmarkDefinitionsPath,
            LandmarkCatalogPath, LandmarkCompilerPath, Map18ExportPath, Map18ExporterPath, Map2107ProfilesPath,
            Map2107DigestPath, Map2108DigestPath };
        private static MoonPalaceNamedDigest Named(string name, string content) =>
            new MoonPalaceNamedDigest(name, BakingCanonicalDigest.HashCanonicalText(content));
        private static void Write(string root, string relative, string content) => File.WriteAllText(
            Resolve(root, relative), content, BakingCanonicalDigest.Utf8NoBomEncoding);
        private static string Read(string root, string relative) => File.ReadAllText(Resolve(root, relative));
        private static string Combine(string left, string right) => left.TrimEnd('/') + "/" + right;
        private static string RequireRoot(string root) { if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException(nameof(root)); return Path.GetFullPath(root); }
        private static string Resolve(string root, string relative) => Path.GetFullPath(Path.Combine(RequireRoot(root), relative.Replace('/', Path.DirectorySeparatorChar)));
        private static string FileSha(string path) { using (var stream = File.OpenRead(path)) using (var sha = SHA256.Create()) return string.Concat(sha.ComputeHash(stream).Select(x => x.ToString("x2", CultureInfo.InvariantCulture))); }
        [Serializable] private sealed class Map2107Document { public string canonical_digest; }
        [Serializable] private sealed class Map2108Document { public string MAP21_09_handoff_digest; public string canonical_digest; }
    }

    public sealed class MoonPalaceLandmarkPublishedSample
    {
        private readonly ReadOnlyCollection<string> written;
        private readonly ReadOnlyCollection<string> sourceReads;
        public MoonPalaceLandmarkPublishedSample(MoonPalaceLandmarkProduction production,
            MoonPalaceLandmarkDigestManifest digestManifest, MoonPalaceLandmarkForbiddenOperationCounters counters,
            IEnumerable<string> writtenPaths, IEnumerable<string> sourceReadPaths)
        {
            Production = production ?? throw new ArgumentNullException(nameof(production));
            DigestManifest = digestManifest ?? throw new ArgumentNullException(nameof(digestManifest));
            Counters = counters ?? throw new ArgumentNullException(nameof(counters));
            written = new ReadOnlyCollection<string>((writtenPaths ?? Array.Empty<string>()).OrderBy(x => x, StringComparer.Ordinal).ToArray());
            sourceReads = new ReadOnlyCollection<string>((sourceReadPaths ?? Array.Empty<string>()).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        }
        public MoonPalaceLandmarkProduction Production { get; }
        public MoonPalaceLandmarkDigestManifest DigestManifest { get; }
        public MoonPalaceLandmarkForbiddenOperationCounters Counters { get; }
        public IReadOnlyList<string> WrittenRelativePaths => written;
        public IReadOnlyList<string> SourceReadRelativePaths => sourceReads;
    }
}
