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
    public static class MoonPalaceCoreResourcePublisher
    {
        public const string AuthoringDirectoryRelativePath = "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_07";
        public const string GeneratedDirectoryRelativePath = "MapDesign/MCP/GENERATED/MAP21_07";
        public const string ProfilesFileName = "moonpalace_core_resource_profiles.csv";
        public const string ChunksFileName = "moonpalace_core_resource_chunks.csv";
        public const string NodesFileName = "moonpalace_core_resource_nodes.csv";
        public const string EdgesFileName = "moonpalace_core_resource_edges.csv";
        public const string RewardsFileName = "moonpalace_core_resource_rewards.csv";
        public const string PersistenceFileName = "moonpalace_core_resource_persistence.csv";
        public const string ResourceManifestFileName = "moonpalace_core_resource_manifest.json";
        public const string RouteManifestFileName = "moonpalace_core_resource_route_manifest.json";
        public const string PersistenceManifestFileName = "moonpalace_core_resource_persistence_manifest.json";
        public const string DigestManifestFileName = "moonpalace_core_resource_digest_manifest.json";

        public const string SourceMap13DefinitionsRelativePath = "Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/CoreResourceRegionDefinitions.cs";
        public const string SourceMap13CatalogRelativePath = "Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/CoreResourceRegionStarterCatalog.cs";
        public const string SourceMap13CompilerRelativePath = "Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/CoreResourceRegionCompiler.cs";
        public const string SourceMap1309ResultRelativePath = "MapDesign/MCP/REPORTS/MAP13_09_MAP13_SPECIAL_REGION_EXIT_TESTS_RESULT.md";
        public const string SourceMap18ExporterRelativePath = "Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedSpecialStateExporter.cs";
        public const string SourceMap1806ResultRelativePath = "MapDesign/MCP/REPORTS/MAP18_06_IMPLEMENT_SPECIAL_STATE_EXPORT_AND_DEBUG_RESULT.md";
        public const string SourceMap2104RelativePath = "MapDesign/MCP/GENERATED/MAP21_04/moonpalace_all_biome_cluster_pool_manifest.json";
        public const string SourceMap2105RelativePath = "MapDesign/MCP/GENERATED/MAP21_05/moonpalace_activity_event_digest_manifest.json";
        public const string SourceMap2106RelativePath = "MapDesign/MCP/GENERATED/MAP21_06/moonpalace_boundary_digest_manifest.json";

        public static MoonPalaceCoreResourcePublishedSample CreateReadOnlySample(string projectRoot,
            string createdUtc, bool reverseInputOrder = false)
        {
            projectRoot = RequireRoot(projectRoot);
            ValidateUpstream(projectRoot);
            var definitions = CoreResourceRegionStarterCatalog.Entries.ToArray();
            if (reverseInputOrder) Array.Reverse(definitions);
            ValidateStarter(definitions);
            var chunks = new List<MoonPalaceCoreResourceChunk>();
            var nodes = new List<MoonPalaceCoreResourceNode>();
            var edges = new List<MoonPalaceCoreResourceEdge>();
            var rewards = new List<MoonPalaceCoreResourceReward>();
            var checkpoints = new List<MoonPalaceCoreResourceCheckpoint>();
            var profiles = new List<MoonPalaceCoreResourceProfile>();

            foreach (var definition in definitions.OrderBy(value => value.RegionId.Value,
                StringComparer.Ordinal))
            {
                var regionId = definition.RegionId.Value;
                var token = RegionToken(definition.Resource.ToString());
                var lowRoute = definition.Routes.Single(value => value.Kind == CoreResourceRouteKind.Low);
                var highRoute = definition.Routes.Single(value => value.Kind == CoreResourceRouteKind.High);
                var recoveryRoute = definition.Routes.Single(value => value.Kind == CoreResourceRouteKind.Recovery);
                var recovery = definition.Recoveries.Single();
                var regionChunks = definition.ActiveDesignChunks.Select((value, index) =>
                    new MoonPalaceCoreResourceChunk(regionId, "CRC_" + token + "_" +
                        (index + 1).ToString("D2", CultureInfo.InvariantCulture), value.X,
                        value.Y)).ToArray();
                chunks.AddRange(regionChunks);

                var lowNodeIds = NodeIds(definition.Edges.Where(value =>
                    value.RouteKind == CoreResourceRouteKind.Low));
                var highNodeIds = NodeIds(definition.Edges.Where(value =>
                    value.RouteKind == CoreResourceRouteKind.High &&
                    value.EdgeId != recovery.FailureEdgeId));
                var recoveryNodeIds = NodeIds(definition.Edges.Where(value =>
                    value.RouteKind == CoreResourceRouteKind.Recovery));
                var regionNodes = definition.Nodes.Select(value =>
                {
                    var projected = Project(value.Coordinate.X, value.Coordinate.Y,
                        definition.DesignOrigin.X, definition.DesignOrigin.Y);
                    var routeKind = NodeRouteKind(value, lowNodeIds, highNodeIds,
                        recoveryNodeIds);
                    var routeId = routeKind == "Low" ? lowRoute.RouteId :
                        routeKind == "High" ? highRoute.RouteId : routeKind == "Recovery" ?
                        recoveryRoute.RouteId : "CR_ROUTE_" + token + "_FAILURE";
                    var required = value.Role != CoreResourceNodeRole.Failure &&
                        (lowNodeIds.Contains(value.NodeId) || recoveryNodeIds.Contains(value.NodeId));
                    var access = routeKind == "Low" || routeKind == "Recovery" ?
                        "MandatoryNoTool" : "OptionalEnvironment";
                    return new MoonPalaceCoreResourceNode(regionId, value.NodeId, routeId,
                        routeKind, value.Role.ToString(), projected[0], projected[1],
                        value.AuthoredOrder, required, access,
                        definition.Mechanism.ToString(), "None",
                        value.Role == CoreResourceNodeRole.RequiredReward ?
                            definition.RequiredReward.SlotId.Value : "NONE");
                }).ToArray();
                nodes.AddRange(regionNodes);

                var nodeById = definition.Nodes.ToDictionary(value => value.NodeId,
                    StringComparer.Ordinal);
                var regionEdges = definition.Edges.Select(value =>
                {
                    var isFailure = value.EdgeId == recovery.FailureEdgeId;
                    var routeKind = isFailure ? "Failure" : value.RouteKind.ToString();
                    var routeId = isFailure ? "CR_ROUTE_" + token + "_FAILURE" :
                        definition.Routes.Single(route => route.Kind == value.RouteKind &&
                            route.EdgeIds.Contains(value.EdgeId, StringComparer.Ordinal)).RouteId;
                    var target = nodeById[value.ToNodeId];
                    var projected = Project(target.Coordinate.X, target.Coordinate.Y,
                        definition.DesignOrigin.X, definition.DesignOrigin.Y);
                    var role = isFailure ? "FailureBranch" : value.RouteKind ==
                        CoreResourceRouteKind.Recovery ? "RecoveryStep" : target.Role ==
                        CoreResourceNodeRole.RequiredReward ? "RewardConvergence" : target.Role ==
                        CoreResourceNodeRole.Return ? "RewardReturn" : "Traversal";
                    return new MoonPalaceCoreResourceEdge(regionId, value.EdgeId, routeId,
                        routeKind, role, value.FromNodeId, value.ToNodeId, projected[0],
                        projected[1], value.Order, value.Required,
                        value.AccessClass.ToString(), value.Mechanism.ToString(),
                        value.Dependency.ToString(), target.Role ==
                            CoreResourceNodeRole.RequiredReward ?
                            definition.RequiredReward.SlotId.Value : "NONE");
                }).ToArray();
                edges.AddRange(regionEdges);

                var regionRewards = new List<MoonPalaceCoreResourceReward>
                {
                    new MoonPalaceCoreResourceReward(regionId,
                        definition.RequiredReward.RewardId, "Required",
                        definition.RequiredReward.NodeId, definition.Resource.ToString(),
                        definition.RequiredReward.SlotId.Value,
                        definition.RequiredReward.PersistenceKey.Value, "NONE",
                        definition.RequiredReward.Amount, true),
                };
                regionRewards.AddRange(definition.OptionalBenefits.Select(value =>
                    new MoonPalaceCoreResourceReward(regionId, value.BenefitId,
                        "OptionalBenefit", value.NodeId, definition.Resource.ToString(),
                        "NONE", "NONE", value.Kind.ToString(), 0, false)));
                rewards.AddRange(regionRewards);

                var regionCheckpoints = BuildCheckpoints(regionId,
                    definition.RequiredReward.PersistenceKey.Value, token).ToArray();
                checkpoints.AddRange(regionCheckpoints);
                profiles.Add(new MoonPalaceCoreResourceProfile(regionId,
                    definition.Resource.ToString(), definition.Biome.CanonicalId,
                    definition.Mechanism.ToString(), lowRoute.RouteId, highRoute.RouteId,
                    recovery.FailureEdgeId, recoveryRoute.RouteId,
                    definition.RequiredReward.SlotId.Value,
                    definition.RequiredReward.PersistenceKey.Value,
                    MoonPalaceCoreResourceProduction.SetDigest("MAP21_07_REGION_CHUNKS_V1",
                        regionChunks.Select(value => value.CanonicalLine)),
                    MoonPalaceCoreResourceProduction.SetDigest("MAP21_07_REGION_GRAPH_V1",
                        regionNodes.Select(value => value.CanonicalLine).Concat(
                            regionEdges.Select(value => value.CanonicalLine))),
                    MoonPalaceCoreResourceProduction.SetDigest("MAP21_07_REGION_REWARDS_V1",
                        regionRewards.Select(value => value.CanonicalLine)),
                    MoonPalaceCoreResourceProduction.SetDigest("MAP21_07_REGION_PERSISTENCE_V1",
                        regionCheckpoints.Select(value => value.CanonicalLine))));
            }

            var production = new MoonPalaceCoreResourceProduction(profiles, chunks, nodes,
                edges, rewards, checkpoints, createdUtc);
            var resourceJson = production.SerializeResourceManifest();
            var routeJson = production.SerializeRouteManifest();
            var persistenceJson = production.SerializePersistenceManifest();
            var csvDigests = new[]
            {
                Named(ProfilesFileName, production.SerializeProfilesCsv()),
                Named(ChunksFileName, production.SerializeChunksCsv()),
                Named(NodesFileName, production.SerializeNodesCsv()),
                Named(EdgesFileName, production.SerializeEdgesCsv()),
                Named(RewardsFileName, production.SerializeRewardsCsv()),
                Named(PersistenceFileName, production.SerializePersistenceCsv()),
            };
            var jsonDigests = new[]
            {
                Named(ResourceManifestFileName, resourceJson),
                Named(RouteManifestFileName, routeJson),
                Named(PersistenceManifestFileName, persistenceJson),
            };
            var manifest = new MoonPalaceCoreResourceDigestManifest(production,
                csvDigests, jsonDigests, createdUtc);
            return new MoonPalaceCoreResourcePublishedSample(production, manifest,
                MoonPalaceCoreResourceForbiddenOperationCounters.Zero,
                Array.Empty<string>(), SourcePaths());
        }

        public static MoonPalaceCoreResourcePublishedSample PublishAuthoringAndSamples(
            string projectRoot, bool focusedMap2107Pass)
        {
            if (!focusedMap2107Pass) throw new InvalidOperationException(
                "MAP21_08 handoff requires a focused MAP21_07 PASS.");
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
                Combine(AuthoringDirectoryRelativePath, RewardsFileName),
                Combine(AuthoringDirectoryRelativePath, PersistenceFileName),
                Combine(GeneratedDirectoryRelativePath, ResourceManifestFileName),
                Combine(GeneratedDirectoryRelativePath, RouteManifestFileName),
                Combine(GeneratedDirectoryRelativePath, PersistenceManifestFileName),
                Combine(GeneratedDirectoryRelativePath, DigestManifestFileName),
            };
            Write(projectRoot, paths[0], sample.Production.SerializeProfilesCsv());
            Write(projectRoot, paths[1], sample.Production.SerializeChunksCsv());
            Write(projectRoot, paths[2], sample.Production.SerializeNodesCsv());
            Write(projectRoot, paths[3], sample.Production.SerializeEdgesCsv());
            Write(projectRoot, paths[4], sample.Production.SerializeRewardsCsv());
            Write(projectRoot, paths[5], sample.Production.SerializePersistenceCsv());
            Write(projectRoot, paths[6], sample.Production.SerializeResourceManifest());
            Write(projectRoot, paths[7], sample.Production.SerializeRouteManifest());
            Write(projectRoot, paths[8], sample.Production.SerializePersistenceManifest());
            Write(projectRoot, paths[9], sample.DigestManifest.Serialize());
            return new MoonPalaceCoreResourcePublishedSample(sample.Production,
                sample.DigestManifest, sample.Counters, paths,
                sample.SourceReadRelativePaths);
        }

        private static IEnumerable<MoonPalaceCoreResourceCheckpoint> BuildCheckpoints(
            string regionId, string rewardKey, string token)
        {
            var index = 0;
            foreach (var state in MoonPalaceCoreResourceProduction.RequiredCheckpointStates)
            {
                index++;
                var claimed = state == "Claimed" || state == "RevisitedClaimed";
                yield return new MoonPalaceCoreResourceCheckpoint(regionId,
                    "CRP_" + token + "_" + index.ToString("D2",
                        CultureInfo.InvariantCulture), state, rewardKey,
                    claimed ? "Unavailable" : "Available",
                    claimed ? "Claimed" : "Unclaimed", false, false);
            }
        }

        private static HashSet<string> NodeIds(
            IEnumerable<CoreResourceSolutionEdge> source) => new HashSet<string>(
                source.SelectMany(value => new[] { value.FromNodeId, value.ToNodeId }),
                StringComparer.Ordinal);
        private static string NodeRouteKind(CoreResourceSolutionNode node,
            ISet<string> low, ISet<string> high, ISet<string> recovery)
        {
            if (node.Role == CoreResourceNodeRole.Failure) return "Failure";
            if (low.Contains(node.NodeId)) return "Low";
            if (recovery.Contains(node.NodeId)) return "Recovery";
            if (high.Contains(node.NodeId)) return "High";
            throw new InvalidDataException("Unrouted MAP13 node: " + node.NodeId);
        }
        private static int[] Project(int sourceX, int sourceY, int originX, int originY) =>
            new[] { Math.Max(0, Math.Min(35, sourceX - originX)),
                Math.Max(0, Math.Min(15, sourceY - originY)) };

        private static void ValidateStarter(IReadOnlyCollection<CoreResourceRegionDefinition> source)
        {
            if (source.Count != 3 || source.Sum(value => value.Nodes.Count) != 44 ||
                source.Sum(value => value.Edges.Count) != 51)
                throw new InvalidDataException("MAP13 starter totals mismatch.");
            var specs = new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                { "SR_MOON_CORE_SITE_5", new[] { "MoonCore", "MoonCrater", "ImpactChain", "14", "16", "5", "8", "SR_STATE_MOON_CORE_SITE_5_REWARD_MOON_CORE_REWARD" } },
                { "SR_CASSIA_SAP_SITE_5", new[] { "CassiaSap", "CassiaRoot", "WaterChannel", "15", "17", "7", "7", "SR_STATE_CASSIA_SAP_SITE_5_REWARD_CASSIA_SAP_REWARD" } },
                { "SR_STAR_NURUK_SITE_5", new[] { "StarNuruk", "MoonDough", "FermentationPressure", "15", "18", "8", "7", "SR_STATE_STAR_NURUK_SITE_5_REWARD_STAR_NURUK_REWARD" } },
            };
            foreach (var value in source)
            {
                if (!specs.TryGetValue(value.RegionId.Value, out var spec) ||
                    value.Resource.ToString() != spec[0] || value.Biome.CanonicalId != spec[1] ||
                    value.Mechanism.ToString() != spec[2] || value.Nodes.Count.ToString(
                        CultureInfo.InvariantCulture) != spec[3] || value.Edges.Count.ToString(
                        CultureInfo.InvariantCulture) != spec[4] || value.Edges.Count(edge =>
                        edge.RouteKind == CoreResourceRouteKind.Low).ToString(
                            CultureInfo.InvariantCulture) != spec[5] || value.Edges.Count(edge =>
                        edge.RouteKind == CoreResourceRouteKind.High && !edge.EdgeId.Contains(
                            "FAILURE_BRANCH")).ToString(CultureInfo.InvariantCulture) != spec[6] ||
                    value.DesignWidth != 36 || value.DesignHeight != 16 ||
                    value.ActiveDesignChunks.Count != 5 || value.Routes.Count != 3 ||
                    value.Recoveries.Count != 1 || value.OptionalBenefits.Count != 2 ||
                    value.RequiredReward.PersistenceKey.Value != spec[7])
                    throw new InvalidDataException("MAP13 CoreResource identity mismatch.");
            }
        }

        private static void ValidateUpstream(string root)
        {
            ValidateReport(root, SourceMap1309ResultRelativePath,
                MoonPalaceCoreResourcePreconditions.SourceMap1309ResultDigest,
                MoonPalaceCoreResourcePreconditions.SourceMap13AuditDigest);
            ValidateReport(root, SourceMap1806ResultRelativePath,
                MoonPalaceCoreResourcePreconditions.SourceMap1806ResultDigest,
                MoonPalaceCoreResourcePreconditions.SourceMap18ExportDigest,
                MoonPalaceCoreResourcePreconditions.SourceMap18DebugDigest);
            foreach (var path in new[] { SourceMap13DefinitionsRelativePath,
                SourceMap13CatalogRelativePath, SourceMap13CompilerRelativePath,
                SourceMap18ExporterRelativePath }) FileSha256(Resolve(root, path));
            var map2104 = JsonUtility.FromJson<CanonicalDocument>(File.ReadAllText(
                Resolve(root, SourceMap2104RelativePath)));
            var map2105 = JsonUtility.FromJson<CanonicalDocument>(File.ReadAllText(
                Resolve(root, SourceMap2105RelativePath)));
            var map2106 = JsonUtility.FromJson<Map2106Document>(File.ReadAllText(
                Resolve(root, SourceMap2106RelativePath)));
            if (map2104 == null || map2104.canonical_digest !=
                    MoonPalaceCoreResourcePreconditions.SourceMap2104Digest ||
                map2105 == null || map2105.canonical_digest !=
                    MoonPalaceCoreResourcePreconditions.SourceMap2105Digest ||
                map2106 == null || map2106.canonical_digest !=
                    MoonPalaceCoreResourcePreconditions.SourceMap2106Digest ||
                map2106.MAP21_07_handoff_digest !=
                    MoonPalaceCoreResourcePreconditions.SourceMap2107HandoffDigest)
                throw new InvalidDataException("MAP21_04/05/06 digest chain mismatch.");
        }

        private static void ValidateReport(string root, string path, string expectedSha,
            params string[] expectedDigests)
        {
            var resolved = Resolve(root, path);
            if (FileSha256(resolved) != expectedSha) throw new InvalidDataException(
                "Source Result SHA mismatch: " + path);
            var report = File.ReadAllText(resolved);
            if (!report.Contains("STATUS: PASS") || expectedDigests.Any(digest =>
                !report.Contains(digest))) throw new InvalidDataException(
                    "Source Result evidence mismatch: " + path);
        }

        private static string[] SourcePaths() => new[]
        {
            SourceMap13DefinitionsRelativePath, SourceMap13CatalogRelativePath,
            SourceMap13CompilerRelativePath, SourceMap1309ResultRelativePath,
            SourceMap18ExporterRelativePath, SourceMap1806ResultRelativePath,
            SourceMap2104RelativePath, SourceMap2105RelativePath, SourceMap2106RelativePath,
        };
        private static string RegionToken(string resource) => resource == "MoonCore" ?
            "MOON" : resource == "CassiaSap" ? "CASSIA" : "NURUK";
        private static MoonPalaceNamedDigest Named(string name, string content) =>
            new MoonPalaceNamedDigest(name, BakingCanonicalDigest.HashCanonicalText(content));
        private static void Write(string root, string relative, string content) =>
            File.WriteAllText(Resolve(root, relative), content,
                BakingCanonicalDigest.Utf8NoBomEncoding);
        private static string Combine(string left, string right) => left.TrimEnd('/') + "/" + right;
        private static string RequireRoot(string root) { if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException(nameof(root)); return Path.GetFullPath(root); }
        private static string Resolve(string root, string relative) => Path.GetFullPath(Path.Combine(RequireRoot(root), relative.Replace('/', Path.DirectorySeparatorChar)));
        private static string FileSha256(string path) { using (var stream = File.OpenRead(path)) using (var sha = SHA256.Create()) return string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("x2", CultureInfo.InvariantCulture))); }
        [Serializable] private sealed class CanonicalDocument { public string canonical_digest; }
        [Serializable] private sealed class Map2106Document { public string MAP21_07_handoff_digest; public string canonical_digest; }
    }

    public sealed class MoonPalaceCoreResourcePublishedSample
    {
        private readonly ReadOnlyCollection<string> written;
        private readonly ReadOnlyCollection<string> sourceReads;
        public MoonPalaceCoreResourcePublishedSample(MoonPalaceCoreResourceProduction production,
            MoonPalaceCoreResourceDigestManifest digestManifest,
            MoonPalaceCoreResourceForbiddenOperationCounters counters,
            IEnumerable<string> writtenPaths, IEnumerable<string> sourceReadPaths)
        {
            Production = production ?? throw new ArgumentNullException(nameof(production));
            DigestManifest = digestManifest ?? throw new ArgumentNullException(nameof(digestManifest));
            Counters = counters ?? throw new ArgumentNullException(nameof(counters));
            written = new ReadOnlyCollection<string>((writtenPaths ?? Array.Empty<string>()).OrderBy(value => value, StringComparer.Ordinal).ToArray());
            sourceReads = new ReadOnlyCollection<string>((sourceReadPaths ?? Array.Empty<string>()).OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }
        public MoonPalaceCoreResourceProduction Production { get; }
        public MoonPalaceCoreResourceDigestManifest DigestManifest { get; }
        public MoonPalaceCoreResourceForbiddenOperationCounters Counters { get; }
        public IReadOnlyList<string> WrittenRelativePaths => written;
        public IReadOnlyList<string> SourceReadRelativePaths => sourceReads;
    }
}
