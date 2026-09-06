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

namespace StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace
{
    public static class MoonPalaceBoundaryPoolPublisher
    {
        public const string AuthoringDirectoryRelativePath = "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_06";
        public const string GeneratedDirectoryRelativePath = "MapDesign/MCP/GENERATED/MAP21_06";
        public const string CandidatesFileName = "moonpalace_boundary_candidates.csv";
        public const string TilesFileName = "moonpalace_boundary_tiles.csv";
        public const string SocketsFileName = "moonpalace_boundary_sockets.csv";
        public const string RoutesFileName = "moonpalace_boundary_route_profiles.csv";
        public const string WarningsFileName = "moonpalace_boundary_warning_evidence.csv";
        public const string PoolManifestFileName = "moonpalace_boundary_pool_manifest.json";
        public const string ProjectionManifestFileName = "moonpalace_boundary_projection_manifest.json";
        public const string WarningManifestFileName = "moonpalace_boundary_warning_manifest.json";
        public const string DigestManifestFileName = "moonpalace_boundary_digest_manifest.json";

        public const string SourcePairRulesRelativePath = "Assets/_Game/Map/Data/WorldGeneration/Authoring/Boundary/biome_boundary_pair_rules.csv";
        public const string SourceProfilesRelativePath = "Assets/_Game/Map/Data/WorldGeneration/Authoring/Boundary/biome_boundary_profiles.csv";
        public const string SourceCatalogRelativePath = "Assets/_Game/Map/Data/WorldGeneration/Authoring/Boundary/boundary_chunk_catalog.csv";
        public const string SourceBiomeProfilesRelativePath = "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_01/moonpalace_biome_profiles.csv";
        public const string SourceTileShellRelativePath = "Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_01/moonpalace_tile_shell.csv";
        public const string SourceMap2104ManifestRelativePath = "MapDesign/MCP/GENERATED/MAP21_04/moonpalace_all_biome_cluster_pool_manifest.json";
        public const string SourceMap2105ManifestRelativePath = "MapDesign/MCP/GENERATED/MAP21_05/moonpalace_activity_event_digest_manifest.json";
        public const string SourceMap0814ResultRelativePath = "MapDesign/MCP/REPORTS/MAP08_14_MAP08_EXIT_TESTS_RESULT.md";
        public const string SourceMap0814TaskRelativePath = "MapDesign/MCP/TASKS/MAP08_14_MAP08_EXIT_TESTS.md";

        public static MoonPalaceBoundaryPublishedSample CreateReadOnlySample(string projectRoot,
            string createdUtc, bool reverseInputOrder = false)
        {
            projectRoot = RequireRoot(projectRoot);
            ValidateUpstream(projectRoot);
            var pairRows = ReadRows(projectRoot, SourcePairRulesRelativePath);
            var profileRows = ReadRows(projectRoot, SourceProfilesRelativePath);
            var catalogRows = ReadRows(projectRoot, SourceCatalogRelativePath);
            var biomeRows = ReadRows(projectRoot, SourceBiomeProfilesRelativePath);
            var tileRows = ReadRows(projectRoot, SourceTileShellRelativePath);
            if (reverseInputOrder)
            {
                pairRows.Reverse(); profileRows.Reverse(); catalogRows.Reverse();
                biomeRows.Reverse(); tileRows.Reverse();
            }
            ValidateMap08(pairRows, profileRows, catalogRows);
            var biomeProfiles = BiomeProfiles(biomeRows);
            if (!tileRows.Any(row => Value(row, "tile_code") == "MP_BOUNDARY_BLEND"))
                throw new InvalidDataException("MAP21_01 boundary tile shell is missing.");

            var candidates = new List<MoonPalaceBoundaryCandidate>();
            var tiles = new List<MoonPalaceBoundaryTileRecord>();
            var sockets = new List<MoonPalaceBoundarySocketRecord>();
            var routes = new List<MoonPalaceBoundaryRouteProfile>();
            var warnings = new List<MoonPalaceBoundaryWarningEvidence>();
            foreach (var row in pairRows.OrderBy(value => Value(value, "boundary_pair_rule_id"), StringComparer.Ordinal))
            {
                var pairId = Value(row, "boundary_pair_rule_id");
                var biomeA = ProductionBiome(Value(row, "biome_a_id"));
                var biomeB = ProductionBiome(Value(row, "biome_b_id"));
                var pairToken = pairId.Substring("PAIR_".Length);
                var allowedProfiles = Value(row, "allowed_boundary_profile_ids").Split('|');
                var pairDigest = RowDigest("MAP08_PAIR_RULE_V1", row);
                var warningDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
                {
                    "MAP08_WARNING_POLICY_V1", pairId, "minimum_categories=2",
                    "categories=Tile|Background|Resource|Audio",
                }.Concat(profileRows.Where(profile => allowedProfiles.Contains(
                        Value(profile, "boundary_profile_id"), StringComparer.Ordinal))
                    .Select(profile => RowDigest("MAP08_BOUNDARY_PROFILE_V1", profile))
                    .OrderBy(value => value, StringComparer.Ordinal)));

                foreach (var orientation in new[] { "Horizontal", "Vertical" })
                for (var variant = 1; variant <= 4; variant++)
                {
                    var shortOrientation = orientation == "Horizontal" ? "H" : "V";
                    var candidateId = "BND_" + pairToken + "_" + shortOrientation + "_" + variant.ToString("D2", CultureInfo.InvariantCulture);
                    var profileId = SelectCompatibleProfile(allowedProfiles, profileRows, orientation, variant);
                    var routeProfileId = "ROUTE_" + candidateId;
                    var candidateTiles = BuildTiles(candidateId, biomeA, biomeB, orientation, biomeProfiles).ToArray();
                    var candidateRoutes = BuildRoutes(candidateId, pairId, orientation).ToArray();
                    var candidateSockets = candidateRoutes.Select(route => new MoonPalaceBoundarySocketRecord(
                        "SOCKET_" + route.ProjectionId, candidateId, route.ProjectionId, route.Direction,
                        route.EntrySide, route.ExitSide, route.SocketSignature, routeProfileId)).ToArray();
                    var candidateWarnings = BuildWarnings(candidateId, pairId, biomeA, biomeB,
                        Value(row, "transition_resource_pool_id"), candidateRoutes, biomeProfiles).ToArray();
                    tiles.AddRange(candidateTiles); routes.AddRange(candidateRoutes);
                    sockets.AddRange(candidateSockets); warnings.AddRange(candidateWarnings);
                    candidates.Add(new MoonPalaceBoundaryCandidate(candidateId, pairId, biomeA, biomeB,
                        orientation, variant, routeProfileId, "TILE_" + profileId,
                        "BACKGROUND_" + profileId, Value(row, "transition_resource_pool_id"),
                        "AUDIO_" + pairToken, pairDigest, warningDigest,
                        MoonPalaceBoundaryProduction.SetDigest("MAP21_06_CANDIDATE_TILES_V1", candidateTiles.Select(x => x.CanonicalLine)),
                        MoonPalaceBoundaryProduction.SetDigest("MAP21_06_CANDIDATE_SOCKETS_V1", candidateSockets.Select(x => x.CanonicalLine)),
                        MoonPalaceBoundaryProduction.SetDigest("MAP21_06_CANDIDATE_WARNINGS_V1", candidateWarnings.Select(x => x.CanonicalLine))));
                }
            }

            var production = new MoonPalaceBoundaryProduction(candidates, tiles, sockets, routes, warnings, createdUtc);
            var poolJson = production.SerializePoolManifest();
            var projectionJson = production.SerializeProjectionManifest();
            var warningJson = production.SerializeWarningManifest();
            var csvDigests = new[]
            {
                Named(CandidatesFileName, production.SerializeCandidatesCsv()), Named(TilesFileName, production.SerializeTilesCsv()),
                Named(SocketsFileName, production.SerializeSocketsCsv()), Named(RoutesFileName, production.SerializeRoutesCsv()),
                Named(WarningsFileName, production.SerializeWarningsCsv()),
            };
            var jsonDigests = new[] { Named(PoolManifestFileName, poolJson), Named(ProjectionManifestFileName, projectionJson), Named(WarningManifestFileName, warningJson) };
            var digestManifest = new MoonPalaceBoundaryDigestManifest(production, csvDigests, jsonDigests, createdUtc);
            return new MoonPalaceBoundaryPublishedSample(production, digestManifest,
                MoonPalaceBoundaryForbiddenOperationCounters.Zero, Array.Empty<string>(),
                new[] { SourcePairRulesRelativePath, SourceProfilesRelativePath, SourceCatalogRelativePath,
                    SourceBiomeProfilesRelativePath, SourceTileShellRelativePath, SourceMap2104ManifestRelativePath,
                    SourceMap2105ManifestRelativePath, SourceMap0814ResultRelativePath, SourceMap0814TaskRelativePath });
        }

        public static MoonPalaceBoundaryPublishedSample PublishAuthoringAndSamples(string projectRoot,
            bool focusedMap2106Pass)
        {
            if (!focusedMap2106Pass) throw new InvalidOperationException("MAP21_07 handoff requires a focused MAP21_06 PASS.");
            projectRoot = RequireRoot(projectRoot);
            var sample = CreateReadOnlySample(projectRoot, DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(Resolve(projectRoot, AuthoringDirectoryRelativePath));
            Directory.CreateDirectory(Resolve(projectRoot, GeneratedDirectoryRelativePath));
            var paths = new[]
            {
                Combine(AuthoringDirectoryRelativePath, CandidatesFileName), Combine(AuthoringDirectoryRelativePath, TilesFileName),
                Combine(AuthoringDirectoryRelativePath, SocketsFileName), Combine(AuthoringDirectoryRelativePath, RoutesFileName),
                Combine(AuthoringDirectoryRelativePath, WarningsFileName), Combine(GeneratedDirectoryRelativePath, PoolManifestFileName),
                Combine(GeneratedDirectoryRelativePath, ProjectionManifestFileName), Combine(GeneratedDirectoryRelativePath, WarningManifestFileName),
                Combine(GeneratedDirectoryRelativePath, DigestManifestFileName),
            };
            Write(projectRoot, paths[0], sample.Production.SerializeCandidatesCsv());
            Write(projectRoot, paths[1], sample.Production.SerializeTilesCsv());
            Write(projectRoot, paths[2], sample.Production.SerializeSocketsCsv());
            Write(projectRoot, paths[3], sample.Production.SerializeRoutesCsv());
            Write(projectRoot, paths[4], sample.Production.SerializeWarningsCsv());
            Write(projectRoot, paths[5], sample.Production.SerializePoolManifest());
            Write(projectRoot, paths[6], sample.Production.SerializeProjectionManifest());
            Write(projectRoot, paths[7], sample.Production.SerializeWarningManifest());
            Write(projectRoot, paths[8], sample.DigestManifest.Serialize());
            return new MoonPalaceBoundaryPublishedSample(sample.Production, sample.DigestManifest,
                sample.Counters, paths, sample.SourceReadRelativePaths);
        }

        private static IEnumerable<MoonPalaceBoundaryTileRecord> BuildTiles(string candidateId,
            string biomeA, string biomeB, string orientation, IReadOnlyDictionary<string, BiomeProfile> profiles)
        {
            for (var y = 0; y < 8; y++) for (var x = 0; x < 12; x++)
            {
                var fromA = orientation == "Horizontal" ? x < 6 : y < 4;
                var seam = orientation == "Horizontal" ? x == 5 || x == 6 : y == 3 || y == 4;
                var biome = fromA ? biomeA : biomeB;
                var tileCode = seam ? "MP_BOUNDARY_BLEND" : y <= 1 ? "MP_GROUND_CORE" : "MP_BACKGROUND_CORE";
                var role = seam ? "BoundarySeam" : y <= 1 ? "TerrainShell" : "BackgroundShell";
                yield return new MoonPalaceBoundaryTileRecord(candidateId, x, y, tileCode, role, biome, profiles[biome].Material);
            }
        }

        private static IEnumerable<MoonPalaceBoundaryRouteProfile> BuildRoutes(string candidateId,
            string pairId, string orientation)
        {
            var horizontal = orientation == "Horizontal";
            var signature = horizontal ? "EDGE_H_MID_WALK" : "EDGE_V_CENTER_CLIMB";
            var movement = horizontal ? "Walk" : "Climb|Jump";
            yield return new MoonPalaceBoundaryRouteProfile(candidateId, candidateId + "__A_TO_B", pairId,
                "A_TO_B", orientation, horizontal ? "Left" : "Down", horizontal ? "Right" : "Up",
                signature, "MandatoryBoundaryTraversal", movement, "MANDATORY_NO_TOOL", true, "NONE");
            yield return new MoonPalaceBoundaryRouteProfile(candidateId, candidateId + "__B_TO_A", pairId,
                "B_TO_A", orientation, horizontal ? "Right" : "Up", horizontal ? "Left" : "Down",
                signature, "MandatoryBoundaryTraversal", movement, "MANDATORY_NO_TOOL", true, "NONE");
        }

        private static IEnumerable<MoonPalaceBoundaryWarningEvidence> BuildWarnings(string candidateId,
            string pairId, string biomeA, string biomeB, string resourcePool,
            IEnumerable<MoonPalaceBoundaryRouteProfile> routes, IReadOnlyDictionary<string, BiomeProfile> profiles)
        {
            foreach (var route in routes)
            {
                var entering = route.Direction == "A_TO_B" ? biomeB : biomeA;
                var profile = profiles[entering];
                foreach (var evidence in new[]
                {
                    new[] { "Tile", profile.Material }, new[] { "Background", profile.Background },
                    new[] { "Resource", resourcePool }, new[] { "Audio", profile.Audio },
                })
                    yield return new MoonPalaceBoundaryWarningEvidence(route.ProjectionId, candidateId,
                        pairId, route.Direction, evidence[0], evidence[1], entering,
                        "MAP08_WARNING_MIN_TWO_CATEGORIES", "Context");
            }
        }

        private static IReadOnlyDictionary<string, BiomeProfile> BiomeProfiles(IEnumerable<IReadOnlyDictionary<string, string>> rows)
        {
            var values = rows.Select(row => new BiomeProfile(Value(row, "biome_id"), Value(row, "primary_material_token"),
                Value(row, "ambient_audio_token"), Value(row, "background_token"))).ToArray();
            var expected = new[] { "MoonCrater", "CassiaRoot", "AbandonedMill", "MoonDough" };
            if (!values.Select(x => x.Id).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(expected.OrderBy(x => x, StringComparer.Ordinal), StringComparer.Ordinal)) throw new InvalidDataException("MAP21_01 biome vocabulary mismatch.");
            return values.ToDictionary(x => x.Id, StringComparer.Ordinal);
        }

        private static void ValidateMap08(IReadOnlyCollection<IReadOnlyDictionary<string, string>> pairs,
            IReadOnlyCollection<IReadOnlyDictionary<string, string>> profiles, IReadOnlyCollection<IReadOnlyDictionary<string, string>> catalog)
        {
            if (pairs.Count != 6 || profiles.Count != 6 || catalog.Count != 31) throw new InvalidDataException("MAP08 baseline must be 6 pairs and 31 candidates.");
            var exactPairs = MoonPalaceBoundaryProduction.RequiredPairIds.OrderBy(x => x, StringComparer.Ordinal);
            if (!pairs.Select(row => Value(row, "boundary_pair_rule_id")).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(exactPairs, StringComparer.Ordinal)) throw new InvalidDataException("MAP08 pair identity mismatch.");
            if (pairs.Any(row => Value(row, "active") != "1") || profiles.Any(row => Value(row, "tool_requirement") != "NONE")) throw new InvalidDataException("MAP08 active/tool policy mismatch.");
            foreach (var row in catalog)
            {
                var orientation = Value(row, "orientation");
                var expected = orientation == "HORIZONTAL" ? "EDGE_H_MID_WALK" : orientation == "VERTICAL" ? "EDGE_V_CENTER_CLIMB" : string.Empty;
                if (expected.Length == 0 || Value(row, "entry_edge_signature_id") != expected || Value(row, "exit_edge_signature_id") != expected) throw new InvalidDataException("MAP08 edge family mismatch.");
            }
        }

        private static string SelectCompatibleProfile(IReadOnlyList<string> allowed,
            IEnumerable<IReadOnlyDictionary<string, string>> profiles, string orientation, int variant)
        {
            var token = orientation.ToUpperInvariant();
            var compatible = allowed.Where(id => profiles.Any(row => Value(row, "boundary_profile_id") == id &&
                Value(row, "allowed_orientations").Split('|').Contains(token, StringComparer.Ordinal))).ToArray();
            if (compatible.Length == 0) throw new InvalidDataException("No compatible MAP08 boundary profile.");
            return compatible[(variant - 1) % compatible.Length];
        }

        private static void ValidateUpstream(string root)
        {
            if (FileSha256(Resolve(root, SourceMap0814ResultRelativePath)) != MoonPalaceBoundaryPreconditions.SourceMap0814ResultDigest ||
                FileSha256(Resolve(root, SourceMap0814TaskRelativePath)) != MoonPalaceBoundaryPreconditions.SourceMap0814TaskDigest) throw new InvalidDataException("MAP08_14 Result/Task SHA mismatch.");
            var report = File.ReadAllText(Resolve(root, SourceMap0814ResultRelativePath));
            if (!report.Contains("STATUS: PASS") || !report.Contains(MoonPalaceBoundaryPreconditions.SourceMap08AggregateDigest) ||
                !report.Contains(MoonPalaceBoundaryPreconditions.SourceMap08AuthoringManifestDigest)) throw new InvalidDataException("MAP08 accepted digest chain mismatch.");
            var cluster = JsonUtility.FromJson<ClusterManifestDocument>(File.ReadAllText(Resolve(root, SourceMap2104ManifestRelativePath)));
            if (cluster == null || cluster.canonical_digest != MoonPalaceBoundaryPreconditions.SourceMap2104ClusterManifestDigest || cluster.cluster_catalog == null || cluster.cluster_catalog.Length != 48) throw new InvalidDataException("MAP21_04 cluster manifest mismatch.");
            var activity = JsonUtility.FromJson<ActivityDigestDocument>(File.ReadAllText(Resolve(root, SourceMap2105ManifestRelativePath)));
            if (activity == null || activity.canonical_digest != MoonPalaceBoundaryPreconditions.SourceMap2105ActivityEventManifestDigest || activity.MAP21_06_handoff_digest != MoonPalaceBoundaryPreconditions.SourceMap2106HandoffDigest) throw new InvalidDataException("MAP21_05 digest/handoff mismatch.");
        }

        private static List<IReadOnlyDictionary<string, string>> ReadRows(string root, string relativePath)
        {
            var read = new Rfc4180CsvReader().Read(File.ReadAllBytes(Resolve(root, relativePath)), relativePath);
            if (!read.Success || read.Records.Count < 2) throw new InvalidDataException("RFC4180 CSV read failed: " + relativePath);
            var headers = read.Records[0].Fields.Select(x => x.Value.Trim()).ToArray();
            if (headers.Any(x => x.Length == 0) || headers.Distinct(StringComparer.Ordinal).Count() != headers.Length) throw new InvalidDataException("Invalid CSV headers.");
            return read.Records.Skip(1).Select(record =>
            {
                if (record.Fields.Count != headers.Length) throw new InvalidDataException("CSV width mismatch.");
                return (IReadOnlyDictionary<string, string>)headers.Select((header, index) => new { header, value = record.Fields[index].Value.Trim() }).ToDictionary(x => x.header, x => x.value, StringComparer.Ordinal);
            }).ToList();
        }

        private static string RowDigest(string prefix, IReadOnlyDictionary<string, string> row) => BakingCanonicalDigest.HashCanonicalLines(new[] { prefix }.Concat(row.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => Join(x.Key, x.Value))));
        private static string Join(params string[] values) => string.Join("/", (values ?? Array.Empty<string>()).Select(value => { var text = value ?? string.Empty; return text.Length.ToString(CultureInfo.InvariantCulture) + ":" + text; }));
        private static string Value(IReadOnlyDictionary<string, string> row, string field) { if (!row.TryGetValue(field, out var value) || string.IsNullOrWhiteSpace(value)) throw new InvalidDataException("Missing CSV field: " + field); return value; }
        private static string ProductionBiome(string source) { switch (source) { case "BIO_MOON_CRATER": return "MoonCrater"; case "BIO_CASSIA_ROOT": return "CassiaRoot"; case "BIO_ABANDONED_MILL": return "AbandonedMill"; case "BIO_MOON_DOUGH": return "MoonDough"; default: throw new InvalidDataException("Unknown MAP08 biome: " + source); } }
        private static MoonPalaceNamedDigest Named(string name, string content) => new MoonPalaceNamedDigest(name, BakingCanonicalDigest.HashCanonicalText(content));
        private static void Write(string root, string relativePath, string content) => File.WriteAllText(Resolve(root, relativePath), content, BakingCanonicalDigest.Utf8NoBomEncoding);
        private static string Combine(string left, string right) => left.TrimEnd('/') + "/" + right;
        private static string RequireRoot(string root) { if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException(nameof(root)); return Path.GetFullPath(root); }
        private static string Resolve(string root, string relative) => Path.GetFullPath(Path.Combine(RequireRoot(root), relative.Replace('/', Path.DirectorySeparatorChar)));
        private static string FileSha256(string path) { using (var stream = File.OpenRead(path)) using (var sha = SHA256.Create()) return string.Concat(sha.ComputeHash(stream).Select(x => x.ToString("x2", CultureInfo.InvariantCulture))); }

        private sealed class BiomeProfile { public BiomeProfile(string id, string material, string audio, string background) { Id = id; Material = material; Audio = audio; Background = background; } public string Id { get; } public string Material { get; } public string Audio { get; } public string Background { get; } }
        [Serializable] private sealed class ClusterManifestDocument { public ClusterRecordDocument[] cluster_catalog; public string canonical_digest; }
        [Serializable] private sealed class ClusterRecordDocument { public string cluster_id; }
        [Serializable] private sealed class ActivityDigestDocument { public string MAP21_06_handoff_digest; public string canonical_digest; }
    }

    public sealed class MoonPalaceBoundaryPublishedSample
    {
        private readonly ReadOnlyCollection<string> written;
        private readonly ReadOnlyCollection<string> sourceReads;
        public MoonPalaceBoundaryPublishedSample(MoonPalaceBoundaryProduction production,
            MoonPalaceBoundaryDigestManifest digestManifest, MoonPalaceBoundaryForbiddenOperationCounters counters,
            IEnumerable<string> writtenPaths, IEnumerable<string> sourceReadPaths)
        {
            Production = production ?? throw new ArgumentNullException(nameof(production));
            DigestManifest = digestManifest ?? throw new ArgumentNullException(nameof(digestManifest));
            Counters = counters ?? throw new ArgumentNullException(nameof(counters));
            written = new ReadOnlyCollection<string>((writtenPaths ?? Array.Empty<string>()).OrderBy(x => x, StringComparer.Ordinal).ToArray());
            sourceReads = new ReadOnlyCollection<string>((sourceReadPaths ?? Array.Empty<string>()).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        }
        public MoonPalaceBoundaryProduction Production { get; }
        public MoonPalaceBoundaryDigestManifest DigestManifest { get; }
        public MoonPalaceBoundaryForbiddenOperationCounters Counters { get; }
        public IReadOnlyList<string> WrittenRelativePaths => written;
        public IReadOnlyList<string> SourceReadRelativePaths => sourceReads;
    }
}
