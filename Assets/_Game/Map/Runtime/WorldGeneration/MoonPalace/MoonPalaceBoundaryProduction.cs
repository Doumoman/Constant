using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace
{
    public static class MoonPalaceBoundaryPreconditions
    {
        public const string TaskId = "MAP21_06_EXPAND_ALL_SIX_BOUNDARY_POOLS";
        public const string SourceMap0814ResultDigest = "5d0b2f0d478ef8479b93e1b9163445f6e736022b533dee77f81690b8670cf2d1";
        public const string SourceMap0814TaskDigest = "6fffc0ed3f8ca333cf7d74d44c437ab6e4193871ce8b2a7a254405e4bcaa5e8e";
        public const string SourceMap08AggregateDigest = "f7ff1c49f5bc33a4ad57799269bc3915806fe0cb60f347ed76eb16ea26f7fc68";
        public const string SourceMap08AuthoringManifestDigest = "f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb";
        public const string SourceMap2104ClusterManifestDigest = "d30baf04fcd6eceeabd7b6f467b24ce11ce7c5f44833887bf7b0457e6fbdda72";
        public const string SourceMap2105ActivityEventManifestDigest = "645f95b8c58757179223b8e5b219b60b5a732cb898a69f99594178c76f166a1e";
        public const string SourceMap2106HandoffDigest = "854cf1a08510898a1570676ce4ea30f1c0b3b3883c7694be49092f1fae88733b";
    }

    public sealed class MoonPalaceBoundaryCandidate
    {
        public const string SchemaVersion = "map21_06.boundary_candidate.v1";

        public MoonPalaceBoundaryCandidate(string candidateId, string pairId,
            string biomeA, string biomeB, string orientation, int variantIndex,
            string routeProfileId, string tileProfileId, string backgroundProfileId,
            string resourceProfileId, string audioProfileId, string sourcePairDigest,
            string sourceWarningDigest, string tileDigest, string socketDigest,
            string warningDigest)
        {
            CandidateId = Required(candidateId); PairId = Required(pairId);
            BiomeA = Required(biomeA); BiomeB = Required(biomeB);
            Orientation = Required(orientation);
            if (variantIndex < 1 || variantIndex > 4) throw new ArgumentOutOfRangeException(nameof(variantIndex));
            VariantIndex = variantIndex; RouteProfileId = Required(routeProfileId);
            TileProfileId = Required(tileProfileId); BackgroundProfileId = Required(backgroundProfileId);
            ResourceProfileId = Required(resourceProfileId); AudioProfileId = Required(audioProfileId);
            SourceMap08PairDigest = Digest(sourcePairDigest); SourceMap08WarningPolicyDigest = Digest(sourceWarningDigest);
            TileDigest = Digest(tileDigest); SocketDigest = Digest(socketDigest); WarningDigest = Digest(warningDigest);
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "MAP21_06_BOUNDARY_CANDIDATE_V1", CanonicalLine });
        }

        public string CandidateId { get; }
        public string PairId { get; }
        public string BiomeA { get; }
        public string BiomeB { get; }
        public string Orientation { get; }
        public int VariantIndex { get; }
        public string RouteProfileId { get; }
        public string TileProfileId { get; }
        public string BackgroundProfileId { get; }
        public string ResourceProfileId { get; }
        public string AudioProfileId { get; }
        public string SourceMap08PairDigest { get; }
        public string SourceMap08WarningPolicyDigest { get; }
        public string TileDigest { get; }
        public string SocketDigest { get; }
        public string WarningDigest { get; }
        public string CanonicalDigest { get; }
        public string CanonicalLine => MoonPalaceCanonical.Join(CandidateId, SchemaVersion, PairId,
            BiomeA, BiomeB, Orientation, VariantIndex.ToString(CultureInfo.InvariantCulture),
            RouteProfileId, TileProfileId, BackgroundProfileId, ResourceProfileId, AudioProfileId,
            SourceMap08PairDigest, SourceMap08WarningPolicyDigest, TileDigest, SocketDigest, WarningDigest);

        private static string Required(string value) { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Required value."); return value.Trim(); }
        private static string Digest(string value) { if (!BakingCanonicalDigest.IsLowerHexSha256(value)) throw new ArgumentException("SHA-256 required."); return value; }
    }

    public sealed class MoonPalaceBoundaryTileRecord
    {
        public MoonPalaceBoundaryTileRecord(string candidateId, int localX, int localY,
            string tileCode, string tileRole, string sourceBiome, string materialToken)
        {
            CandidateId = Required(candidateId);
            if (localX < 0 || localX >= 12 || localY < 0 || localY >= 8) throw new ArgumentOutOfRangeException("local coordinate");
            LocalX = localX; LocalY = localY; TileCode = Required(tileCode); TileRole = Required(tileRole);
            SourceBiome = Required(sourceBiome); MaterialToken = Required(materialToken);
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "MAP21_06_BOUNDARY_TILE_V1", CanonicalLine });
        }
        public string CandidateId { get; }
        public int LocalX { get; }
        public int LocalY { get; }
        public string TileCode { get; }
        public string TileRole { get; }
        public string SourceBiome { get; }
        public string MaterialToken { get; }
        public string CanonicalDigest { get; }
        public string CanonicalLine => MoonPalaceCanonical.Join(CandidateId,
            LocalX.ToString(CultureInfo.InvariantCulture), LocalY.ToString(CultureInfo.InvariantCulture),
            TileCode, TileRole, SourceBiome, MaterialToken);
        private static string Required(string value) { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Required value."); return value.Trim(); }
    }

    public sealed class MoonPalaceBoundaryRouteProfile
    {
        public MoonPalaceBoundaryRouteProfile(string candidateId, string projectionId,
            string pairId, string direction, string orientation, string entrySide,
            string exitSide, string socketSignature, string routeIntent,
            string movementTokens, string accessClass, bool mandatoryRoute,
            string toolRequirement)
        {
            CandidateId = Required(candidateId); ProjectionId = Required(projectionId); PairId = Required(pairId);
            Direction = Required(direction); Orientation = Required(orientation); EntrySide = Required(entrySide);
            ExitSide = Required(exitSide); SocketSignature = Required(socketSignature); RouteIntent = Required(routeIntent);
            MovementTokens = Required(movementTokens); AccessClass = Required(accessClass);
            MandatoryRoute = mandatoryRoute; ToolRequirement = Required(toolRequirement);
            ProfileDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "MAP21_06_ROUTE_PROFILE_V1", CanonicalLine });
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "MAP21_06_ROUTE_PROJECTION_V1", ProfileDigest, CanonicalLine });
        }
        public string CandidateId { get; }
        public string ProjectionId { get; }
        public string PairId { get; }
        public string Direction { get; }
        public string Orientation { get; }
        public string EntrySide { get; }
        public string ExitSide { get; }
        public string SocketSignature { get; }
        public string RouteIntent { get; }
        public string MovementTokens { get; }
        public string AccessClass { get; }
        public bool MandatoryRoute { get; }
        public string ToolRequirement { get; }
        public string ProfileDigest { get; }
        public string CanonicalDigest { get; }
        public string CanonicalLine => MoonPalaceCanonical.Join(CandidateId, ProjectionId, PairId,
            Direction, Orientation, EntrySide, ExitSide, SocketSignature, RouteIntent,
            MovementTokens, AccessClass, MandatoryRoute ? "true" : "false", ToolRequirement);
        private static string Required(string value) { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Required value."); return value.Trim(); }
    }

    public sealed class MoonPalaceBoundarySocketRecord
    {
        public MoonPalaceBoundarySocketRecord(string socketKey, string candidateId,
            string projectionId, string direction, string entrySide, string exitSide,
            string socketSignature, string routeProfileId)
        {
            SocketKey = Required(socketKey); CandidateId = Required(candidateId);
            ProjectionId = Required(projectionId); Direction = Required(direction);
            EntrySide = Required(entrySide); ExitSide = Required(exitSide);
            SocketSignature = Required(socketSignature); RouteProfileId = Required(routeProfileId);
            ToolRequirement = "NONE";
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "MAP21_06_BOUNDARY_SOCKET_V1", CanonicalLine });
        }
        public string SocketKey { get; }
        public string CandidateId { get; }
        public string ProjectionId { get; }
        public string Direction { get; }
        public string EntrySide { get; }
        public string ExitSide { get; }
        public string SocketSignature { get; }
        public string RouteProfileId { get; }
        public string ToolRequirement { get; }
        public string CanonicalDigest { get; }
        public string CanonicalLine => MoonPalaceCanonical.Join(SocketKey, CandidateId, ProjectionId,
            Direction, EntrySide, ExitSide, SocketSignature, RouteProfileId, ToolRequirement);
        private static string Required(string value) { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Required value."); return value.Trim(); }
    }

    public sealed class MoonPalaceBoundaryWarningEvidence
    {
        public MoonPalaceBoundaryWarningEvidence(string projectionId, string candidateId,
            string pairId, string direction, string category, string evidenceKey,
            string enteringBiome, string sourcePolicy, string severity)
        {
            ProjectionId = Required(projectionId); CandidateId = Required(candidateId); PairId = Required(pairId);
            Direction = Required(direction); Category = Required(category); EvidenceKey = Required(evidenceKey);
            EnteringBiome = Required(enteringBiome); SourcePolicy = Required(sourcePolicy); Severity = Required(severity);
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "MAP21_06_WARNING_EVIDENCE_V1", CanonicalLine });
        }
        public string ProjectionId { get; }
        public string CandidateId { get; }
        public string PairId { get; }
        public string Direction { get; }
        public string Category { get; }
        public string EvidenceKey { get; }
        public string EnteringBiome { get; }
        public string SourcePolicy { get; }
        public string Severity { get; }
        public string CanonicalDigest { get; }
        public string CanonicalLine => MoonPalaceCanonical.Join(ProjectionId, CandidateId, PairId,
            Direction, Category, EvidenceKey, EnteringBiome, SourcePolicy, Severity);
        private static string Required(string value) { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Required value."); return value.Trim(); }
    }

    public sealed class MoonPalaceBoundaryProduction
    {
        public const string SchemaVersion = "map21_06.boundary_production.v1";
        private static readonly string[] PairIds = { "PAIR_CRATER_ROOT", "PAIR_CRATER_MILL", "PAIR_CRATER_DOUGH", "PAIR_ROOT_MILL", "PAIR_ROOT_DOUGH", "PAIR_MILL_DOUGH" };
        private static readonly HashSet<string> Biomes = new HashSet<string>(new[] { "MoonCrater", "CassiaRoot", "AbandonedMill", "MoonDough" }, StringComparer.Ordinal);
        private static readonly HashSet<string> Categories = new HashSet<string>(new[] { "Tile", "Background", "Resource", "Audio" }, StringComparer.Ordinal);
        private readonly ReadOnlyCollection<MoonPalaceBoundaryCandidate> candidates;
        private readonly ReadOnlyCollection<MoonPalaceBoundaryTileRecord> tiles;
        private readonly ReadOnlyCollection<MoonPalaceBoundarySocketRecord> sockets;
        private readonly ReadOnlyCollection<MoonPalaceBoundaryRouteProfile> routes;
        private readonly ReadOnlyCollection<MoonPalaceBoundaryWarningEvidence> warnings;

        public MoonPalaceBoundaryProduction(IEnumerable<MoonPalaceBoundaryCandidate> sourceCandidates,
            IEnumerable<MoonPalaceBoundaryTileRecord> sourceTiles, IEnumerable<MoonPalaceBoundarySocketRecord> sourceSockets,
            IEnumerable<MoonPalaceBoundaryRouteProfile> sourceRoutes, IEnumerable<MoonPalaceBoundaryWarningEvidence> sourceWarnings,
            string createdUtc)
        {
            candidates = Ordered(sourceCandidates, x => x.CandidateId, nameof(sourceCandidates));
            tiles = Ordered(sourceTiles, x => x.CandidateId + "/" + x.LocalY.ToString("D2", CultureInfo.InvariantCulture) + "/" + x.LocalX.ToString("D2", CultureInfo.InvariantCulture), nameof(sourceTiles));
            sockets = Ordered(sourceSockets, x => x.SocketKey, nameof(sourceSockets));
            routes = Ordered(sourceRoutes, x => x.ProjectionId, nameof(sourceRoutes));
            warnings = Ordered(sourceWarnings, x => x.ProjectionId + "/" + x.Category, nameof(sourceWarnings));
            CreatedUtc = createdUtc ?? string.Empty;
            Validate();
            CandidateSetDigest = SetDigest("MAP21_06_CANDIDATE_SET_V1", candidates.Select(x => x.CanonicalLine));
            TileSetDigest = SetDigest("MAP21_06_TILE_SET_V1", tiles.Select(x => x.CanonicalLine));
            SocketSetDigest = SetDigest("MAP21_06_SOCKET_SET_V1", sockets.Select(x => x.CanonicalLine));
            ProjectionSetDigest = SetDigest("MAP21_06_PROJECTION_SET_V1", routes.Select(x => x.CanonicalLine));
            WarningSetDigest = SetDigest("MAP21_06_WARNING_SET_V1", warnings.Select(x => x.CanonicalLine));
            PoolManifestDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "MAP21_06_POOL_MANIFEST_V1", CandidateSetDigest, TileSetDigest, "created_utc_excluded=true" });
            ProjectionManifestDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "MAP21_06_PROJECTION_MANIFEST_V1", ProjectionSetDigest, SocketSetDigest, "created_utc_excluded=true" });
            WarningManifestDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "MAP21_06_WARNING_MANIFEST_V1", WarningSetDigest, "created_utc_excluded=true" });
        }

        public static IReadOnlyList<string> RequiredPairIds => PairIds;
        public IReadOnlyList<MoonPalaceBoundaryCandidate> Candidates => candidates;
        public IReadOnlyList<MoonPalaceBoundaryTileRecord> Tiles => tiles;
        public IReadOnlyList<MoonPalaceBoundarySocketRecord> Sockets => sockets;
        public IReadOnlyList<MoonPalaceBoundaryRouteProfile> Routes => routes;
        public IReadOnlyList<MoonPalaceBoundaryWarningEvidence> Warnings => warnings;
        public string CreatedUtc { get; }
        public bool CreatedUtcExcludedFromCanonicalDigest => true;
        public string CandidateSetDigest { get; }
        public string TileSetDigest { get; }
        public string SocketSetDigest { get; }
        public string ProjectionSetDigest { get; }
        public string WarningSetDigest { get; }
        public string PoolManifestDigest { get; }
        public string ProjectionManifestDigest { get; }
        public string WarningManifestDigest { get; }

        public string SerializeCandidatesCsv() => Csv("candidate_id,schema_version,pair_id,biome_a,biome_b,orientation,variant_index,route_profile_id,tile_profile_id,background_profile_id,resource_profile_id,audio_profile_id,source_map08_pair_digest,source_map08_warning_policy_digest,tile_digest,socket_digest,warning_digest,canonical_digest",
            candidates.Select(x => Row(x.CandidateId, MoonPalaceBoundaryCandidate.SchemaVersion, x.PairId, x.BiomeA, x.BiomeB, x.Orientation, Number(x.VariantIndex), x.RouteProfileId, x.TileProfileId, x.BackgroundProfileId, x.ResourceProfileId, x.AudioProfileId, x.SourceMap08PairDigest, x.SourceMap08WarningPolicyDigest, x.TileDigest, x.SocketDigest, x.WarningDigest, x.CanonicalDigest)));
        public string SerializeTilesCsv() => Csv("candidate_id,local_x,local_y,tile_code,tile_role,source_biome,material_token,canonical_digest",
            tiles.Select(x => Row(x.CandidateId, Number(x.LocalX), Number(x.LocalY), x.TileCode, x.TileRole, x.SourceBiome, x.MaterialToken, x.CanonicalDigest)));
        public string SerializeSocketsCsv() => Csv("socket_key,candidate_id,projection_id,direction,entry_side,exit_side,socket_signature,route_profile_id,tool_requirement,canonical_digest",
            sockets.Select(x => Row(x.SocketKey, x.CandidateId, x.ProjectionId, x.Direction, x.EntrySide, x.ExitSide, x.SocketSignature, x.RouteProfileId, x.ToolRequirement, x.CanonicalDigest)));
        public string SerializeRoutesCsv() => Csv("candidate_id,projection_id,pair_id,direction,orientation,entry_side,exit_side,socket_signature,route_intent,movement_tokens,access_class,mandatory_route,tool_requirement,profile_digest,canonical_digest",
            routes.Select(x => Row(x.CandidateId, x.ProjectionId, x.PairId, x.Direction, x.Orientation, x.EntrySide, x.ExitSide, x.SocketSignature, x.RouteIntent, x.MovementTokens, x.AccessClass, x.MandatoryRoute ? "true" : "false", x.ToolRequirement, x.ProfileDigest, x.CanonicalDigest)));
        public string SerializeWarningsCsv() => Csv("projection_id,candidate_id,pair_id,direction,category,evidence_key,entering_biome,source_policy,severity,canonical_digest",
            warnings.Select(x => Row(x.ProjectionId, x.CandidateId, x.PairId, x.Direction, x.Category, x.EvidenceKey, x.EnteringBiome, x.SourcePolicy, x.Severity, x.CanonicalDigest)));
        public string SerializePoolManifest() => MoonPalaceCanonical.ToJson(MoonPalaceBoundaryPoolDocument.From(this));
        public string SerializeProjectionManifest() => MoonPalaceCanonical.ToJson(MoonPalaceBoundaryProjectionDocument.From(this));
        public string SerializeWarningManifest() => MoonPalaceCanonical.ToJson(MoonPalaceBoundaryWarningDocument.From(this));

        private void Validate()
        {
            if (candidates.Count != 48 || tiles.Count != 4608 || routes.Count != 96 || sockets.Count != 96 || warnings.Count < 192) throw new ArgumentException("Exact MAP21_06 inventory required.");
            RequireUnique(candidates.Select(x => x.CandidateId), "candidate");
            RequireUnique(routes.Select(x => x.ProjectionId), "projection");
            RequireUnique(sockets.Select(x => x.SocketKey), "socket");
            var foundPairs = candidates.Select(x => x.PairId).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal);
            if (!foundPairs.SequenceEqual(PairIds.OrderBy(x => x, StringComparer.Ordinal), StringComparer.Ordinal)) throw new ArgumentException("Exact six pair IDs required.");
            foreach (var pair in PairIds)
            {
                var values = candidates.Where(x => x.PairId == pair).ToArray();
                if (values.Length != 8 || values.Count(x => x.Orientation == "Horizontal") != 4 || values.Count(x => x.Orientation == "Vertical") != 4) throw new ArgumentException("Four H and four V candidates per pair required.");
            }
            foreach (var candidate in candidates)
            {
                if (!Biomes.Contains(candidate.BiomeA) || !Biomes.Contains(candidate.BiomeB) || candidate.BiomeA == candidate.BiomeB) throw new ArgumentException("Unknown biome.");
                var candidateTiles = tiles.Where(x => x.CandidateId == candidate.CandidateId).ToArray();
                if (candidateTiles.Length != 96 || candidateTiles.Select(x => x.LocalX + "/" + x.LocalY).Distinct(StringComparer.Ordinal).Count() != 96) throw new ArgumentException("Exact 12x8 tile grid required.");
                var candidateRoutes = routes.Where(x => x.CandidateId == candidate.CandidateId).ToArray();
                var candidateSockets = sockets.Where(x => x.CandidateId == candidate.CandidateId).ToArray();
                if (candidateRoutes.Length != 2 || candidateSockets.Length != 2 ||
                    !candidateRoutes.Select(x => x.Direction).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(new[] { "A_TO_B", "B_TO_A" }, StringComparer.Ordinal)) throw new ArgumentException("Two directional projections and sockets required.");
                var expectedSignature = candidate.Orientation == "Horizontal" ? "EDGE_H_MID_WALK" : candidate.Orientation == "Vertical" ? "EDGE_V_CENTER_CLIMB" : string.Empty;
                if (expectedSignature.Length == 0 || candidateRoutes.Any(x => x.SocketSignature != expectedSignature || x.ToolRequirement != "NONE" || !x.MandatoryRoute) || candidateSockets.Any(x => x.SocketSignature != expectedSignature || x.ToolRequirement != "NONE")) throw new ArgumentException("Route/socket compatibility mismatch.");
                foreach (var route in candidateRoutes)
                {
                    var matching = warnings.Where(x => x.ProjectionId == route.ProjectionId).ToArray();
                    var entering = route.Direction == "A_TO_B" ? candidate.BiomeB : candidate.BiomeA;
                    if (matching.Select(x => x.Category).Distinct(StringComparer.Ordinal).Count() < 2 || matching.Any(x => x.EnteringBiome != entering || !Categories.Contains(x.Category))) throw new ArgumentException("Insufficient or reversed warning evidence.");
                }
                if (candidate.TileDigest != SetDigest("MAP21_06_CANDIDATE_TILES_V1", candidateTiles.Select(x => x.CanonicalLine)) ||
                    candidate.SocketDigest != SetDigest("MAP21_06_CANDIDATE_SOCKETS_V1", candidateSockets.Select(x => x.CanonicalLine)) ||
                    candidate.WarningDigest != SetDigest("MAP21_06_CANDIDATE_WARNINGS_V1", warnings.Where(x => x.CandidateId == candidate.CandidateId).Select(x => x.CanonicalLine))) throw new ArgumentException("Candidate child digest mismatch.");
            }
            if (tiles.Any(x => !candidates.Any(c => c.CandidateId == x.CandidateId)) || sockets.Any(x => !routes.Any(r => r.ProjectionId == x.ProjectionId)) || warnings.Any(x => !routes.Any(r => r.ProjectionId == x.ProjectionId))) throw new ArgumentException("Orphan record.");
        }

        public static string SetDigest(string prefix, IEnumerable<string> lines) => BakingCanonicalDigest.HashCanonicalLines(new[] { prefix }.Concat((lines ?? Array.Empty<string>()).OrderBy(x => x, StringComparer.Ordinal)));
        private static ReadOnlyCollection<T> Ordered<T>(IEnumerable<T> source, Func<T, string> key, string name)
        {
            var values = (source ?? throw new ArgumentNullException(name)).ToArray();
            if (values.Any(x => x == null)) throw new ArgumentException("Null record.", name);
            return new ReadOnlyCollection<T>(values.OrderBy(key, StringComparer.Ordinal).ToArray());
        }
        private static void RequireUnique(IEnumerable<string> values, string label) { var data = values.ToArray(); if (data.Distinct(StringComparer.Ordinal).Count() != data.Length) throw new ArgumentException("Duplicate " + label + "."); }
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Csv(string header, IEnumerable<string> rows) => string.Join("\n", new[] { header }.Concat(rows)) + "\n";
        private static string Row(params string[] values) => string.Join(",", values.Select(Escape));
        private static string Escape(string value) { var text = value ?? string.Empty; return text.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0 ? text : "\"" + text.Replace("\"", "\"\"") + "\""; }
    }

    public sealed class MoonPalaceBoundaryDigestManifest
    {
        private readonly ReadOnlyCollection<MoonPalaceNamedDigest> csvDigests;
        private readonly ReadOnlyCollection<MoonPalaceNamedDigest> jsonDigests;
        public MoonPalaceBoundaryDigestManifest(MoonPalaceBoundaryProduction production,
            IEnumerable<MoonPalaceNamedDigest> sourceCsvDigests, IEnumerable<MoonPalaceNamedDigest> sourceJsonDigests, string createdUtc)
        {
            Production = production ?? throw new ArgumentNullException(nameof(production));
            csvDigests = Copy(sourceCsvDigests, 5); jsonDigests = Copy(sourceJsonDigests, 3); CreatedUtc = createdUtc ?? string.Empty;
            Map2107HandoffDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "MAP21_07_BOUNDARY_HANDOFF_V1", MoonPalaceBoundaryPreconditions.SourceMap2106HandoffDigest,
                MoonPalaceBoundaryPreconditions.SourceMap08AggregateDigest, MoonPalaceBoundaryPreconditions.SourceMap08AuthoringManifestDigest,
                MoonPalaceBoundaryPreconditions.SourceMap2105ActivityEventManifestDigest, production.PoolManifestDigest, production.ProjectionManifestDigest, production.WarningManifestDigest }
                .Concat(csvDigests.Select(x => x.CanonicalLine)).Concat(jsonDigests.Select(x => x.CanonicalLine)));
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "map21_06.boundary_digest_manifest.v1", MoonPalaceBoundaryPreconditions.TaskId,
                MoonPalaceBoundaryPreconditions.SourceMap08AggregateDigest, MoonPalaceBoundaryPreconditions.SourceMap08AuthoringManifestDigest,
                MoonPalaceBoundaryPreconditions.SourceMap2104ClusterManifestDigest, MoonPalaceBoundaryPreconditions.SourceMap2105ActivityEventManifestDigest,
                Map2107HandoffDigest, "created_utc_excluded=true" }.Concat(csvDigests.Select(x => x.CanonicalLine)).Concat(jsonDigests.Select(x => x.CanonicalLine)));
        }
        public MoonPalaceBoundaryProduction Production { get; }
        public IReadOnlyList<MoonPalaceNamedDigest> CsvDigests => csvDigests;
        public IReadOnlyList<MoonPalaceNamedDigest> JsonDigests => jsonDigests;
        public string CreatedUtc { get; }
        public string Map2107HandoffDigest { get; }
        public string CanonicalDigest { get; }
        public string Serialize() => MoonPalaceCanonical.ToJson(MoonPalaceBoundaryDigestDocument.From(this));
        private static ReadOnlyCollection<MoonPalaceNamedDigest> Copy(IEnumerable<MoonPalaceNamedDigest> source, int count)
        { var values = (source ?? throw new ArgumentNullException(nameof(source))).OrderBy(x => x.Name, StringComparer.Ordinal).ToArray(); if (values.Length != count || values.Any(x => x == null) || values.Select(x => x.Name).Distinct(StringComparer.Ordinal).Count() != count) throw new ArgumentException("Digest inventory mismatch."); return new ReadOnlyCollection<MoonPalaceNamedDigest>(values); }
    }

    public sealed class MoonPalaceBoundaryForbiddenOperationCounters
    {
        public static MoonPalaceBoundaryForbiddenOperationCounters Zero => new MoonPalaceBoundaryForbiddenOperationCounters();
        public int GenerationRunnerExecutions => 0; public int RendererExecutions => 0; public int ValidationRunnerExecutions => 0;
        public int ReplayExecutions => 0; public int RollbackExecutions => 0; public int TilemapWrites => 0; public int RuntimeObjectSpawns => 0;
        public int ScenePrefabChanges => 0; public int PriorCategorySelections => 0; public int PlayModeSelections => 0; public int LegacyRegressionSelections => 0;
        public int UnfilteredOrFullRegressionSelections => 0; public int Map08CategoryReruns => 0; public int UpstreamRegenerationRuns => 0;
        public bool AllZero => GenerationRunnerExecutions + RendererExecutions + ValidationRunnerExecutions + ReplayExecutions + RollbackExecutions + TilemapWrites + RuntimeObjectSpawns + ScenePrefabChanges + PriorCategorySelections + PlayModeSelections + LegacyRegressionSelections + UnfilteredOrFullRegressionSelections + Map08CategoryReruns + UpstreamRegenerationRuns == 0;
    }

    [Serializable] internal sealed class MoonPalaceBoundaryPoolDocument
    {
        public string schema_version; public string publication_kind; public int pair_count; public int candidate_count; public int tile_row_count; public string candidate_set_digest; public string tile_set_digest; public string canonical_digest;
        public static MoonPalaceBoundaryPoolDocument From(MoonPalaceBoundaryProduction x) => new MoonPalaceBoundaryPoolDocument { schema_version = MoonPalaceBoundaryProduction.SchemaVersion, publication_kind = "StaticBoundaryPoolNotGeneratedWorld", pair_count = 6, candidate_count = x.Candidates.Count, tile_row_count = x.Tiles.Count, candidate_set_digest = x.CandidateSetDigest, tile_set_digest = x.TileSetDigest, canonical_digest = x.PoolManifestDigest };
    }
    [Serializable] internal sealed class MoonPalaceBoundaryProjectionDocument
    {
        public string schema_version; public int projection_count; public int socket_count; public int tool_requirement_none_count; public string horizontal_signature; public string vertical_signature; public string projection_set_digest; public string socket_set_digest; public string canonical_digest;
        public static MoonPalaceBoundaryProjectionDocument From(MoonPalaceBoundaryProduction x) => new MoonPalaceBoundaryProjectionDocument { schema_version = MoonPalaceBoundaryProduction.SchemaVersion, projection_count = x.Routes.Count, socket_count = x.Sockets.Count, tool_requirement_none_count = x.Routes.Count(r => r.ToolRequirement == "NONE"), horizontal_signature = "EDGE_H_MID_WALK", vertical_signature = "EDGE_V_CENTER_CLIMB", projection_set_digest = x.ProjectionSetDigest, socket_set_digest = x.SocketSetDigest, canonical_digest = x.ProjectionManifestDigest };
    }
    [Serializable] internal sealed class MoonPalaceBoundaryWarningDocument
    {
        public string schema_version; public int evidence_count; public int projection_count; public int minimum_categories_per_projection; public string[] allowed_categories; public string warning_set_digest; public string canonical_digest;
        public static MoonPalaceBoundaryWarningDocument From(MoonPalaceBoundaryProduction x) => new MoonPalaceBoundaryWarningDocument { schema_version = MoonPalaceBoundaryProduction.SchemaVersion, evidence_count = x.Warnings.Count, projection_count = x.Warnings.Select(w => w.ProjectionId).Distinct(StringComparer.Ordinal).Count(), minimum_categories_per_projection = x.Warnings.GroupBy(w => w.ProjectionId).Min(g => g.Select(w => w.Category).Distinct(StringComparer.Ordinal).Count()), allowed_categories = new[] { "Audio", "Background", "Resource", "Tile" }, warning_set_digest = x.WarningSetDigest, canonical_digest = x.WarningManifestDigest };
    }
    [Serializable] internal sealed class MoonPalaceBoundaryDigestDocument
    {
        public string schema_version; public string task_id; public string source_map08_aggregate_digest; public string source_map08_authoring_manifest_digest; public string source_map21_04_cluster_manifest_digest; public string source_map21_05_activity_event_manifest_digest; public MoonPalaceBoundaryNamedDigestDocument[] csv_digests; public MoonPalaceBoundaryNamedDigestDocument[] json_digests; public string MAP21_07_handoff_digest; public bool created_utc_excluded_from_canonical_digest; public string created_utc; public string canonical_digest;
        public static MoonPalaceBoundaryDigestDocument From(MoonPalaceBoundaryDigestManifest x) => new MoonPalaceBoundaryDigestDocument { schema_version = "map21_06.boundary_digest_manifest.v1", task_id = MoonPalaceBoundaryPreconditions.TaskId, source_map08_aggregate_digest = MoonPalaceBoundaryPreconditions.SourceMap08AggregateDigest, source_map08_authoring_manifest_digest = MoonPalaceBoundaryPreconditions.SourceMap08AuthoringManifestDigest, source_map21_04_cluster_manifest_digest = MoonPalaceBoundaryPreconditions.SourceMap2104ClusterManifestDigest, source_map21_05_activity_event_manifest_digest = MoonPalaceBoundaryPreconditions.SourceMap2105ActivityEventManifestDigest, csv_digests = x.CsvDigests.Select(MoonPalaceBoundaryNamedDigestDocument.From).ToArray(), json_digests = x.JsonDigests.Select(MoonPalaceBoundaryNamedDigestDocument.From).ToArray(), MAP21_07_handoff_digest = x.Map2107HandoffDigest, created_utc_excluded_from_canonical_digest = true, created_utc = x.CreatedUtc, canonical_digest = x.CanonicalDigest };
    }
    [Serializable] internal sealed class MoonPalaceBoundaryNamedDigestDocument
    {
        public string name; public string sha256;
        public static MoonPalaceBoundaryNamedDigestDocument From(MoonPalaceNamedDigest x) => new MoonPalaceBoundaryNamedDigestDocument { name = x.Name, sha256 = x.Digest };
    }
}
