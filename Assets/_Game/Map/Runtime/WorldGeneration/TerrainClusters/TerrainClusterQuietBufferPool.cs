using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Pipeline;

namespace StarNight.Map.WorldGeneration.TerrainClusters
{
    public sealed class TerrainClusterQuietBufferPoolCompileRequest
    {
        private readonly ReadOnlyCollection<TerrainClusterQuietBufferProfile> profiles;

        public TerrainClusterQuietBufferPoolCompileRequest(
            IEnumerable<TerrainClusterQuietBufferProfile> profiles)
        {
            this.profiles = new ReadOnlyCollection<TerrainClusterQuietBufferProfile>(
                (profiles ?? Array.Empty<TerrainClusterQuietBufferProfile>()).ToArray());
        }

        public IReadOnlyList<TerrainClusterQuietBufferProfile> Profiles => profiles;
    }

    public sealed class TerrainClusterQuietBufferPool
    {
        private readonly ReadOnlyCollection<TerrainClusterQuietBufferCandidate> candidates;

        internal TerrainClusterQuietBufferPool(
            IEnumerable<TerrainClusterQuietBufferCandidate> candidates,
            string canonicalDigest)
        {
            var copy = (candidates ?? Array.Empty<TerrainClusterQuietBufferCandidate>())
                .OrderBy(value => value.QuietBufferId, StringComparer.Ordinal).ToArray();
            this.candidates = new ReadOnlyCollection<TerrainClusterQuietBufferCandidate>(copy);
            ByBiome = BuildSingleIndex(copy, value => value.Biome);
            ByUse = BuildMultiIndex(copy, value => value.SupportedUses);
            ByEntrySide = BuildSingleIndex(copy, value => value.EntrySide);
            ByExitSide = BuildSingleIndex(copy, value => value.ExitSide);
            ByRouteType = BuildMultiIndex(copy, value => value.CompatibleRouteTypes);
            ByPacingRole = BuildMultiIndex(copy, value => value.CompatiblePacingRoles);
            ByAccessClass = BuildMultiIndex(copy, value => value.CompatibleAccessClasses);
            CanonicalDigest = canonicalDigest ?? string.Empty;
        }

        public IReadOnlyList<TerrainClusterQuietBufferCandidate> Candidates => candidates;
        public IReadOnlyDictionary<MoonpalaceBiomeId, IReadOnlyList<TerrainClusterQuietBufferCandidate>> ByBiome { get; }
        public IReadOnlyDictionary<TerrainClusterQuietBufferUse, IReadOnlyList<TerrainClusterQuietBufferCandidate>> ByUse { get; }
        public IReadOnlyDictionary<ClusterPortSide, IReadOnlyList<TerrainClusterQuietBufferCandidate>> ByEntrySide { get; }
        public IReadOnlyDictionary<ClusterPortSide, IReadOnlyList<TerrainClusterQuietBufferCandidate>> ByExitSide { get; }
        public IReadOnlyDictionary<int, IReadOnlyList<TerrainClusterQuietBufferCandidate>> ByRouteType { get; }
        public IReadOnlyDictionary<PacingRole, IReadOnlyList<TerrainClusterQuietBufferCandidate>> ByPacingRole { get; }
        public IReadOnlyDictionary<AccessClass, IReadOnlyList<TerrainClusterQuietBufferCandidate>> ByAccessClass { get; }
        public string CanonicalDigest { get; }

        public TerrainClusterQuietBufferResult Query(TerrainClusterQuietBufferQuery query)
        {
            return TerrainClusterQuietBufferPoolCompiler.Query(this, query);
        }

        private static IReadOnlyDictionary<TKey, IReadOnlyList<TerrainClusterQuietBufferCandidate>> BuildMultiIndex<TKey>(
            IEnumerable<TerrainClusterQuietBufferCandidate> source,
            Func<TerrainClusterQuietBufferCandidate, IEnumerable<TKey>> keys)
        {
            var mutable = new Dictionary<TKey, List<TerrainClusterQuietBufferCandidate>>();
            foreach (var candidate in source)
            {
                foreach (var key in keys(candidate).Distinct())
                {
                    if (!mutable.TryGetValue(key, out var bucket))
                    {
                        bucket = new List<TerrainClusterQuietBufferCandidate>();
                        mutable.Add(key, bucket);
                    }
                    bucket.Add(candidate);
                }
            }

            return new ReadOnlyDictionary<TKey, IReadOnlyList<TerrainClusterQuietBufferCandidate>>(
                mutable.ToDictionary(
                    pair => pair.Key,
                    pair => (IReadOnlyList<TerrainClusterQuietBufferCandidate>)
                        new ReadOnlyCollection<TerrainClusterQuietBufferCandidate>(
                            pair.Value.OrderBy(value => value.QuietBufferId, StringComparer.Ordinal).ToArray())));
        }

        private static IReadOnlyDictionary<TKey, IReadOnlyList<TerrainClusterQuietBufferCandidate>> BuildSingleIndex<TKey>(
            IEnumerable<TerrainClusterQuietBufferCandidate> source,
            Func<TerrainClusterQuietBufferCandidate, TKey> key)
        {
            return BuildMultiIndex(source, value => new[] { key(value) });
        }
    }

    public sealed class TerrainClusterQuietBufferQuery
    {
        public TerrainClusterQuietBufferQuery(
            MoonpalaceBiomeId biome,
            TerrainClusterQuietBufferUse use,
            ClusterPortSide requiredEntrySide,
            ClusterPortSide requiredExitSide,
            int requiredRouteType,
            PacingRole requiredPacingRole,
            AccessClass requiredAccessClass,
            int? maximumActiveChunkCount = null,
            string expectedPoolDigest = null)
        {
            Biome = biome;
            Use = use;
            RequiredEntrySide = requiredEntrySide;
            RequiredExitSide = requiredExitSide;
            RequiredRouteType = requiredRouteType;
            RequiredPacingRole = requiredPacingRole;
            RequiredAccessClass = requiredAccessClass;
            MaximumActiveChunkCount = maximumActiveChunkCount;
            ExpectedPoolDigest = expectedPoolDigest ?? string.Empty;
        }

        public MoonpalaceBiomeId Biome { get; }
        public TerrainClusterQuietBufferUse Use { get; }
        public ClusterPortSide RequiredEntrySide { get; }
        public ClusterPortSide RequiredExitSide { get; }
        public int RequiredRouteType { get; }
        public PacingRole RequiredPacingRole { get; }
        public AccessClass RequiredAccessClass { get; }
        public int? MaximumActiveChunkCount { get; }
        public string ExpectedPoolDigest { get; }
    }

    public sealed class TerrainClusterQuietBufferQueryResult
    {
        private readonly ReadOnlyCollection<TerrainClusterQuietBufferCandidate> matches;
        private readonly ReadOnlyCollection<string> matchedCandidateIds;
        private readonly ReadOnlyCollection<string> matchedCandidateDigests;

        internal TerrainClusterQuietBufferQueryResult(
            TerrainClusterQuietBufferQuery request,
            string poolDigest,
            IEnumerable<TerrainClusterQuietBufferCandidate> matches,
            string canonicalDigest)
        {
            Request = request;
            PoolDigest = poolDigest ?? string.Empty;
            var copy = (matches ?? Array.Empty<TerrainClusterQuietBufferCandidate>())
                .OrderBy(value => value.QuietBufferId, StringComparer.Ordinal).ToArray();
            this.matches = new ReadOnlyCollection<TerrainClusterQuietBufferCandidate>(copy);
            matchedCandidateIds = new ReadOnlyCollection<string>(copy.Select(value => value.QuietBufferId).ToArray());
            matchedCandidateDigests = new ReadOnlyCollection<string>(copy.Select(value => value.CanonicalDigest).ToArray());
            CanonicalDigest = canonicalDigest ?? string.Empty;
        }

        public TerrainClusterQuietBufferQuery Request { get; }
        public string PoolDigest { get; }
        public IReadOnlyList<TerrainClusterQuietBufferCandidate> Matches => matches;
        public IReadOnlyList<string> MatchedCandidateIds => matchedCandidateIds;
        public IReadOnlyList<string> MatchedCandidateDigests => matchedCandidateDigests;
        public int MatchCount => matches.Count;
        public int RngDrawCount => 0;
        public int SelectionCount => 0;
        public string CanonicalDigest { get; }
    }

    public sealed class TerrainClusterQuietBufferResult
    {
        private static readonly IReadOnlyList<TerrainClusterQuietBufferCandidate> EmptyCandidates =
            Array.Empty<TerrainClusterQuietBufferCandidate>();
        private readonly ReadOnlyCollection<TerrainClusterQuietBufferError> errors;

        internal TerrainClusterQuietBufferResult(
            TerrainClusterQuietBufferPool pool,
            TerrainClusterQuietBufferQueryResult queryResult,
            IEnumerable<TerrainClusterQuietBufferError> errors)
        {
            var copy = (errors ?? Array.Empty<TerrainClusterQuietBufferError>())
                .Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            this.errors = new ReadOnlyCollection<TerrainClusterQuietBufferError>(copy);
            Pool = copy.Length == 0 ? pool : null;
            QueryResult = copy.Length == 0 ? queryResult : null;
        }

        public bool IsSuccess => errors.Count == 0 && (Pool != null || QueryResult != null);
        public TerrainClusterQuietBufferPool Pool { get; }
        public TerrainClusterQuietBufferQueryResult QueryResult { get; }
        public IReadOnlyList<TerrainClusterQuietBufferCandidate> Candidates =>
            Pool == null ? EmptyCandidates : Pool.Candidates;
        public IReadOnlyList<TerrainClusterQuietBufferError> Errors => errors;
        public string CanonicalDigest => Pool != null
            ? Pool.CanonicalDigest
            : QueryResult == null ? string.Empty : QueryResult.CanonicalDigest;
    }

    public static class TerrainClusterQuietBufferPoolCompiler
    {
        public const string RulesetVersion = "MAP11_06_QUIET_BUFFER_CLUSTER_POOL_V1";

        private static readonly Regex QuietBufferIdPattern = new Regex(
            "^QBUF_[A-Z0-9_]+$", RegexOptions.CultureInvariant);

        public static TerrainClusterQuietBufferResult Compile(
            TerrainClusterQuietBufferPoolCompileRequest request)
        {
            var errors = new List<TerrainClusterQuietBufferError>();
            if (request == null)
            {
                Add(errors, TerrainClusterQuietBufferErrorCode.MissingInput,
                    "request", "Pool compile request is required.");
                return Failure(errors);
            }

            if (request.Profiles.Count == 0)
            {
                Add(errors, TerrainClusterQuietBufferErrorCode.EmptyPool,
                    "request.profiles", "At least one Quiet Buffer profile is required.");
                return Failure(errors);
            }

            FindReferenceDuplicates(request.Profiles, errors);
            FindIdDuplicates(request.Profiles, errors);

            var candidates = new List<TerrainClusterQuietBufferCandidate>();
            for (var index = 0; index < request.Profiles.Count; index++)
            {
                var profile = request.Profiles[index];
                if (profile == null)
                {
                    Add(errors, TerrainClusterQuietBufferErrorCode.MissingInput,
                        "request.profiles[" + Number(index) + "]", "Profile is required.");
                    continue;
                }

                var candidate = ValidateAndPublish(profile, index, errors);
                if (candidate != null) candidates.Add(candidate);
            }

            FindCandidateIdentityDuplicates(candidates, errors);
            if (errors.Count != 0) return Failure(errors);

            candidates.Sort((left, right) => string.Compare(
                left.QuietBufferId, right.QuietBufferId, StringComparison.Ordinal));
            var digest = ComputePoolDigest(candidates);
            var pool = new TerrainClusterQuietBufferPool(candidates, digest);
            if (!IsCanonicalPool(pool))
            {
                Add(errors, TerrainClusterQuietBufferErrorCode.NonCanonicalPublication,
                    "pool", "Candidate or index publication is not canonical.");
                return Failure(errors);
            }
            return new TerrainClusterQuietBufferResult(pool, null, errors);
        }

        public static TerrainClusterQuietBufferResult Query(
            TerrainClusterQuietBufferPool pool,
            TerrainClusterQuietBufferQuery query)
        {
            return Query(pool, query == null ? string.Empty : query.ExpectedPoolDigest, query);
        }

        public static TerrainClusterQuietBufferResult Query(
            TerrainClusterQuietBufferPool pool,
            string expectedPoolDigest,
            TerrainClusterQuietBufferQuery query)
        {
            var errors = new List<TerrainClusterQuietBufferError>();
            if (pool == null)
                Add(errors, TerrainClusterQuietBufferErrorCode.MissingInput, "pool", "Compiled pool is required.");
            if (query == null)
                Add(errors, TerrainClusterQuietBufferErrorCode.MissingInput, "query", "Query is required.");
            if (pool == null || query == null) return Failure(errors);

            if (!string.Equals(expectedPoolDigest ?? string.Empty, pool.CanonicalDigest, StringComparison.Ordinal))
                Add(errors, TerrainClusterQuietBufferErrorCode.PoolDigestMismatch,
                    "query.expectedPoolDigest", "Expected digest differs from the compiled pool digest.");
            ValidateQuery(query, errors);
            if (errors.Count != 0) return Failure(errors);

            var matches = pool.Candidates.Where(value => value.Supports(query))
                .OrderBy(value => value.QuietBufferId, StringComparer.Ordinal).ToArray();
            var digest = ComputeQueryDigest(pool.CanonicalDigest, query, matches);
            return new TerrainClusterQuietBufferResult(
                null,
                new TerrainClusterQuietBufferQueryResult(query, pool.CanonicalDigest, matches, digest),
                errors);
        }

        private static TerrainClusterQuietBufferCandidate ValidateAndPublish(
            TerrainClusterQuietBufferProfile profile,
            int index,
            ICollection<TerrainClusterQuietBufferError> errors)
        {
            var prefix = "request.profiles[" + Number(index) + "]";
            var errorCount = errors.Count;
            ValidateProfileHeader(profile, prefix, errors);
            if (profile.LocalCanvas == null || profile.RoleSocketContract == null ||
                profile.TraversalCompilation == null || profile.RouteWitnessReport == null ||
                profile.PatternRenderReport == null)
                return null;

            ValidateArtifactChain(profile, prefix, errors);
            var activeChunks = profile.LocalCanvas.ChunkCells
                .Where(value => value.State == ClusterChunkMaskState.Active)
                .Select(value => value.Coordinate).Distinct().OrderBy(value => value).ToArray();
            if (activeChunks.Length != 2)
                Add(errors, TerrainClusterQuietBufferErrorCode.InvalidFootprintSize,
                    prefix + ".localCanvas.activeChunks", "Quiet Buffer footprint must contain exactly two active chunks.");

            ProjectedClusterPort entryPort;
            ProjectedClusterPort exitPort;
            if (!profile.RoleSocketContract.TryGetPrimaryPort(ClusterPortKind.Entry, out entryPort) || entryPort == null ||
                !profile.RoleSocketContract.TryGetPrimaryPort(ClusterPortKind.Exit, out exitPort) || exitPort == null)
            {
                Add(errors, TerrainClusterQuietBufferErrorCode.ArtifactIdentityMismatch,
                    prefix + ".roleSocketContract.primaryPorts", "Entry and Exit primary ports are required.");
                return null;
            }

            CompiledClusterLocalTileCell entryCell;
            CompiledClusterLocalTileCell exitCell;
            if (!profile.LocalCanvas.TryGetTileCell(entryPort.CompiledCoordinate, out entryCell) || entryCell.State != ClusterChunkMaskState.Active ||
                !profile.LocalCanvas.TryGetTileCell(exitPort.CompiledCoordinate, out exitCell) || exitCell.State != ClusterChunkMaskState.Active)
            {
                Add(errors, TerrainClusterQuietBufferErrorCode.ArtifactIdentityMismatch,
                    prefix + ".primaryPorts", "Primary ports must resolve to active Local Canvas cells.");
                return null;
            }
            if (entryCell.OwningChunk == exitCell.OwningChunk)
                Add(errors, TerrainClusterQuietBufferErrorCode.EntryExitChunkMismatch,
                    prefix + ".primaryPorts", "Entry and Exit primary ports must own different active chunks.");

            var compatibleRouteTypes = entryPort.CompatibleRouteTypes
                .Intersect(exitPort.CompatibleRouteTypes).Distinct().OrderBy(value => value).ToArray();
            if (compatibleRouteTypes.Length == 0 || compatibleRouteTypes.Any(value => value < 0 || value > 4))
                Add(errors, TerrainClusterQuietBufferErrorCode.NonCanonicalPublication,
                    prefix + ".compatibleRouteTypes", "Primary port RouteType intersection must contain only values 0..4.");

            var baselineChunks = ValidateBaseline(profile, activeChunks, prefix, errors);
            var chunkEvidence = ValidateWorkingCanvas(profile, activeChunks, baselineChunks, prefix, errors);
            var rewardCount = profile.RoleSocketContract.Roles.Count(value => value.Role == ClusterRoleKind.Reward);
            if (rewardCount != 0)
                Add(errors, TerrainClusterQuietBufferErrorCode.RewardRoleNotQuiet,
                    prefix + ".roleSocketContract.roles", "Quiet Buffer candidates cannot publish Reward roles.");
            var markerCount = profile.PatternRenderReport.FinalWorkingCanvas.Cells.Count(
                value => !string.IsNullOrEmpty(value.MarkerId));
            if (markerCount != 0)
                Add(errors, TerrainClusterQuietBufferErrorCode.MarkerNotQuiet,
                    prefix + ".patternRenderReport.final.marker", "Quiet Buffer candidates cannot publish markers.");
            var hazardCount = profile.PatternRenderReport.FinalWorkingCanvas.Cells.Count(
                value => !string.IsNullOrEmpty(value.HazardId));
            if (hazardCount != 0)
                Add(errors, TerrainClusterQuietBufferErrorCode.HazardNotQuiet,
                    prefix + ".patternRenderReport.final.hazard", "Quiet Buffer candidates cannot publish hazards.");
            if (profile.PatternRenderReport.ProtectedWriteCount != 0 ||
                profile.PatternRenderReport.ProtectedValueChangeCount != 0)
                Add(errors, TerrainClusterQuietBufferErrorCode.ProtectedMutationDetected,
                    prefix + ".patternRenderReport.protected", "Protected write and value-change counts must be exactly zero.");

            if (errors.Count != errorCount) return null;
            var digest = ComputeCandidateDigest(
                profile, entryPort, exitPort, compatibleRouteTypes, activeChunks,
                baselineChunks, chunkEvidence, rewardCount, markerCount, hazardCount);
            return new TerrainClusterQuietBufferCandidate(
                profile, entryPort, exitPort, compatibleRouteTypes, activeChunks,
                profile.RouteWitnessReport.BaselineRoute.OrderedNodeIds,
                profile.RouteWitnessReport.BaselineRoute.OrderedEdges.Select(value => value.EdgeId),
                baselineChunks, chunkEvidence, rewardCount, markerCount, hazardCount, digest);
        }

        private static void ValidateProfileHeader(
            TerrainClusterQuietBufferProfile profile,
            string prefix,
            ICollection<TerrainClusterQuietBufferError> errors)
        {
            if (!QuietBufferIdPattern.IsMatch(profile.QuietBufferId))
                Add(errors, TerrainClusterQuietBufferErrorCode.InvalidQuietBufferId,
                    prefix + ".quietBufferId", "ID must match ^QBUF_[A-Z0-9_]+$.");
            if (!profile.Biome.IsDefined)
                Add(errors, TerrainClusterQuietBufferErrorCode.InvalidBiome,
                    prefix + ".biome", "A defined MoonpalaceBiomeId is required.");

            var uses = profile.SupportedUses;
            if (uses.Count == 0 || uses.Distinct().Count() != uses.Count ||
                uses.Any(value => value < TerrainClusterQuietBufferUse.BeforeLandmark ||
                                  value > TerrainClusterQuietBufferUse.UnplacedSpace))
                Add(errors, TerrainClusterQuietBufferErrorCode.InvalidUseKind,
                    prefix + ".supportedUses", "At least one unique, defined Quiet Buffer use is required.");

            var pacing = profile.CompatiblePacingRoles;
            var allowedPacing = new[] { PacingRole.Quiet, PacingRole.Traversal, PacingRole.Recovery, PacingRole.Safe, PacingRole.Flow };
            if (pacing.Count == 0 || pacing.Distinct().Count() != pacing.Count ||
                !pacing.Contains(PacingRole.Quiet) || pacing.Any(value => !allowedPacing.Contains(value)))
                Add(errors, TerrainClusterQuietBufferErrorCode.InvalidPacingCompatibility,
                    prefix + ".compatiblePacingRoles", "Compatibility must contain Quiet and only Quiet/Traversal/Recovery/Safe/Flow.");

            var access = profile.CompatibleAccessClasses;
            var allowedAccess = new[] { AccessClass.MandatoryNoTool, AccessClass.OptionalNoTool };
            if (access.Count == 0 || access.Distinct().Count() != access.Count ||
                !access.Contains(AccessClass.MandatoryNoTool) || access.Any(value => !allowedAccess.Contains(value)))
                Add(errors, TerrainClusterQuietBufferErrorCode.InvalidAccessCompatibility,
                    prefix + ".compatibleAccessClasses", "Compatibility must contain MandatoryNoTool and only no-tool access.");

            if (profile.LocalCanvas == null)
                Add(errors, TerrainClusterQuietBufferErrorCode.MissingInput, prefix + ".localCanvas", "MAP11_01 Local Canvas is required.");
            if (profile.RoleSocketContract == null)
                Add(errors, TerrainClusterQuietBufferErrorCode.MissingInput, prefix + ".roleSocketContract", "MAP11_02 role/socket contract is required.");
            if (profile.TraversalCompilation == null)
                Add(errors, TerrainClusterQuietBufferErrorCode.MissingInput, prefix + ".traversalCompilation", "MAP11_03 traversal compilation is required.");
            if (profile.RouteWitnessReport == null)
                Add(errors, TerrainClusterQuietBufferErrorCode.MissingInput, prefix + ".routeWitnessReport", "MAP11_04 route witness report is required.");
            if (profile.PatternRenderReport == null)
                Add(errors, TerrainClusterQuietBufferErrorCode.MissingInput, prefix + ".patternRenderReport", "MAP11_05 pattern render report is required.");
        }

        private static void ValidateArtifactChain(
            TerrainClusterQuietBufferProfile profile,
            string prefix,
            ICollection<TerrainClusterQuietBufferError> errors)
        {
            var clusterId = profile.LocalCanvas.ClusterId;
            if (profile.RoleSocketContract.ClusterId != clusterId ||
                profile.TraversalCompilation.ClusterId != clusterId ||
                profile.RouteWitnessReport.ClusterId != clusterId ||
                profile.RouteWitnessReport.StaticShell == null ||
                profile.RouteWitnessReport.StaticShell.ClusterId != clusterId ||
                profile.PatternRenderReport.ZoneMap == null ||
                profile.PatternRenderReport.ZoneMap.ClusterId != clusterId ||
                profile.PatternRenderReport.InitialWorkingCanvas == null ||
                profile.PatternRenderReport.InitialWorkingCanvas.ClusterId != clusterId ||
                profile.PatternRenderReport.FinalWorkingCanvas == null ||
                profile.PatternRenderReport.FinalWorkingCanvas.ClusterId != clusterId ||
                profile.RoleSocketContract.Transform != profile.LocalCanvas.Transform ||
                profile.TraversalCompilation.Transform != profile.LocalCanvas.Transform)
                Add(errors, TerrainClusterQuietBufferErrorCode.ArtifactIdentityMismatch,
                    prefix + ".artifacts", "MAP11_01..05 cluster identity or transform chain differs.");

            ValidateDigest(profile.ExpectedLocalCanvasDigest, profile.LocalCanvas.CanonicalDigest,
                prefix + ".expectedLocalCanvasDigest", errors);
            ValidateDigest(profile.ExpectedRoleSocketContractDigest, profile.RoleSocketContract.CanonicalDigest,
                prefix + ".expectedRoleSocketContractDigest", errors);
            ValidateDigest(profile.ExpectedTraversalCompilationDigest, profile.TraversalCompilation.CanonicalDigest,
                prefix + ".expectedTraversalCompilationDigest", errors);
            ValidateDigest(profile.ExpectedRouteWitnessDigest, profile.RouteWitnessReport.CanonicalDigest,
                prefix + ".expectedRouteWitnessDigest", errors);
            ValidateDigest(profile.ExpectedPatternRenderDigest, profile.PatternRenderReport.CanonicalDigest,
                prefix + ".expectedPatternRenderDigest", errors);

            if (!string.Equals(profile.RoleSocketContract.LocalCanvasDigest, profile.LocalCanvas.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(profile.TraversalCompilation.SourceContractDigest, profile.RoleSocketContract.SourceContractDigest, StringComparison.Ordinal) ||
                !string.Equals(profile.TraversalCompilation.LocalCanvasDigest, profile.LocalCanvas.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(profile.TraversalCompilation.RoleSocketContractDigest, profile.RoleSocketContract.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(profile.RouteWitnessReport.TraversalCompilationDigest, profile.TraversalCompilation.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(profile.RouteWitnessReport.StaticShell.LocalCanvasDigest, profile.LocalCanvas.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(profile.RouteWitnessReport.StaticShell.TraversalCompilationDigest, profile.TraversalCompilation.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(profile.PatternRenderReport.ZoneMap.LocalCanvasDigest, profile.LocalCanvas.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(profile.PatternRenderReport.ZoneMap.TraversalCompilationDigest, profile.TraversalCompilation.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(profile.PatternRenderReport.ZoneMap.RouteWitnessDigest, profile.RouteWitnessReport.CanonicalDigest, StringComparison.Ordinal))
                Add(errors, TerrainClusterQuietBufferErrorCode.ArtifactDigestMismatch,
                    prefix + ".artifacts.chain", "MAP11_01..05 embedded digest chain differs.");

            var shell = profile.RouteWitnessReport.StaticShell;
            var initial = profile.PatternRenderReport.InitialWorkingCanvas;
            if (shell == null || initial == null || shell.ActiveTileCount != initial.CoordinateCount ||
                shell.Cells.Any(value => !initial.TryGetCell(value.CompiledCoordinate, out var cell) ||
                                         cell.StaticShellCell.Occupancy != value.Occupancy ||
                                         cell.StaticShellCell.OwningChunk != value.OwningChunk))
                Add(errors, TerrainClusterQuietBufferErrorCode.ArtifactIdentityMismatch,
                    prefix + ".patternRenderReport.initial", "MAP11_04 Static Shell and MAP11_05 initial canvas differ.");
        }

        private static ClusterChunkCoord[] ValidateBaseline(
            TerrainClusterQuietBufferProfile profile,
            IReadOnlyCollection<ClusterChunkCoord> activeChunks,
            string prefix,
            ICollection<TerrainClusterQuietBufferError> errors)
        {
            var witness = profile.RouteWitnessReport.BaselineRoute;
            if (witness == null || !profile.TraversalCompilation.TryGetVariant(
                    witness == null ? default(SpineVariantId) : witness.VariantId, out var variant) ||
                variant == null || !variant.IsBaseline)
            {
                Add(errors, TerrainClusterQuietBufferErrorCode.BaselineCoverageMismatch,
                    prefix + ".routeWitnessReport.baseline", "Successful source-backed baseline witness is required.");
                return Array.Empty<ClusterChunkCoord>();
            }

            var valid = true;
            foreach (var nodeId in witness.OrderedNodeIds)
            {
                if (!variant.TryGetNode(nodeId, out var node) ||
                    !witness.CompiledCoordinates.Contains(node.CompiledCoordinate)) valid = false;
            }
            foreach (var edge in witness.OrderedEdges)
            {
                if (!variant.TryGetEdge(edge.EdgeId, out var source) ||
                    !string.Equals(source.FromNodeId, edge.FromNodeId, StringComparison.Ordinal) ||
                    !string.Equals(source.ToNodeId, edge.ToNodeId, StringComparison.Ordinal) ||
                    source.MovementKind != edge.MovementKind ||
                    source.CompiledStartCoordinate != edge.CompiledStartCoordinate ||
                    source.CompiledEndCoordinate != edge.CompiledEndCoordinate ||
                    edge.EstimatedDurationMilliseconds <= 0) valid = false;
            }

            var covered = new HashSet<ClusterChunkCoord>();
            foreach (var coordinate in witness.CompiledCoordinates)
            {
                if (!profile.LocalCanvas.TryGetTileCell(coordinate, out var cell) ||
                    cell.State != ClusterChunkMaskState.Active) valid = false;
                else covered.Add(cell.OwningChunk);
            }
            var coveredCopy = covered.OrderBy(value => value).ToArray();
            if (!valid || !covered.SetEquals(activeChunks))
                Add(errors, TerrainClusterQuietBufferErrorCode.BaselineCoverageMismatch,
                    prefix + ".routeWitnessReport.baseline", "Baseline source evidence must cover both active chunks without synthetic nodes or edges.");
            return coveredCopy;
        }

        private static TerrainClusterQuietBufferChunkEvidence[] ValidateWorkingCanvas(
            TerrainClusterQuietBufferProfile profile,
            IReadOnlyCollection<ClusterChunkCoord> activeChunks,
            IReadOnlyCollection<ClusterChunkCoord> baselineChunks,
            string prefix,
            ICollection<TerrainClusterQuietBufferError> errors)
        {
            var activeTileCount = profile.LocalCanvas.TileCells.Count(
                value => value.State == ClusterChunkMaskState.Active);
            var final = profile.PatternRenderReport.FinalWorkingCanvas;
            var valid = final != null && final.CoordinateCount == activeTileCount &&
                final.Cells.Select(value => value.Coordinate).Distinct().Count() == activeTileCount;
            if (valid)
            {
                foreach (var cell in final.Cells)
                {
                    if (!profile.LocalCanvas.TryGetTileCell(cell.Coordinate, out var localCell) ||
                        localCell.State != ClusterChunkMaskState.Active)
                    {
                        valid = false;
                        break;
                    }
                }
            }
            if (!valid)
                Add(errors, TerrainClusterQuietBufferErrorCode.WorkingCanvasCoverageMismatch,
                    prefix + ".patternRenderReport.final", "Final working canvas must exactly cover all active Local Canvas tiles.");

            var evidence = new List<TerrainClusterQuietBufferChunkEvidence>();
            foreach (var chunk in activeChunks.OrderBy(value => value))
            {
                var cells = final == null ? Array.Empty<TerrainClusterPatternWorkingCell>() :
                    final.Cells.Where(value => profile.LocalCanvas.TryGetTileCell(value.Coordinate, out var localCell) &&
                                               localCell.State == ClusterChunkMaskState.Active &&
                                               localCell.OwningChunk == chunk).ToArray();
                var solid = cells.Count(value => value.Solid);
                var air = cells.Length - solid;
                if (solid < 1 || air < 1)
                    Add(errors, TerrainClusterQuietBufferErrorCode.EmptyChunkTerrain,
                        prefix + ".patternRenderReport.final.chunk[" + chunk + "]",
                        "Every active chunk must contain at least one Solid and one Air cell.");
                var baselineCoordinateCount = profile.RouteWitnessReport.BaselineRoute == null ? 0 :
                    profile.RouteWitnessReport.BaselineRoute.CompiledCoordinates.Count(
                        value => profile.LocalCanvas.TryGetTileCell(value, out var localCell) &&
                                 localCell.OwningChunk == chunk);
                evidence.Add(new TerrainClusterQuietBufferChunkEvidence(
                    chunk, solid, air, baselineCoordinateCount));
            }
            return evidence.ToArray();
        }

        private static void ValidateQuery(
            TerrainClusterQuietBufferQuery query,
            ICollection<TerrainClusterQuietBufferError> errors)
        {
            if (!query.Biome.IsDefined ||
                query.Use < TerrainClusterQuietBufferUse.BeforeLandmark || query.Use > TerrainClusterQuietBufferUse.UnplacedSpace ||
                query.RequiredEntrySide < ClusterPortSide.L || query.RequiredEntrySide > ClusterPortSide.D ||
                query.RequiredExitSide < ClusterPortSide.L || query.RequiredExitSide > ClusterPortSide.D ||
                query.RequiredRouteType < 0 || query.RequiredRouteType > 4 ||
                !PacingRoleTokenCodec.IsPublished(query.RequiredPacingRole) ||
                !AccessClassTokenCodec.IsPublished(query.RequiredAccessClass) ||
                (query.MaximumActiveChunkCount.HasValue && query.MaximumActiveChunkCount.Value < 2))
                Add(errors, TerrainClusterQuietBufferErrorCode.InvalidQuery,
                    "query", "Query contains an undefined enum, invalid RouteType, or maximum active chunk count below two.");
        }

        private static void FindReferenceDuplicates(
            IReadOnlyList<TerrainClusterQuietBufferProfile> profiles,
            ICollection<TerrainClusterQuietBufferError> errors)
        {
            for (var left = 0; left < profiles.Count; left++)
            for (var right = left + 1; right < profiles.Count; right++)
            {
                if (profiles[left] != null && ReferenceEquals(profiles[left], profiles[right]))
                    Add(errors, TerrainClusterQuietBufferErrorCode.DuplicateCandidateIdentity,
                        "request.profiles[" + Number(right) + "]", "The same profile reference cannot be coalesced.");
            }
        }

        private static void FindIdDuplicates(
            IEnumerable<TerrainClusterQuietBufferProfile> profiles,
            ICollection<TerrainClusterQuietBufferError> errors)
        {
            foreach (var group in profiles.Where(value => value != null)
                         .GroupBy(value => value.QuietBufferId, StringComparer.Ordinal)
                         .Where(value => value.Count() > 1))
                Add(errors, TerrainClusterQuietBufferErrorCode.DuplicateQuietBufferId,
                    "request.profiles", "Duplicate Quiet Buffer ID: " + group.Key);
        }

        private static void FindCandidateIdentityDuplicates(
            IEnumerable<TerrainClusterQuietBufferCandidate> candidates,
            ICollection<TerrainClusterQuietBufferError> errors)
        {
            foreach (var group in candidates.GroupBy(
                         value => value.ClusterId.Value + "|" + Number((int)value.Transform),
                         StringComparer.Ordinal).Where(value => value.Count() > 1))
                Add(errors, TerrainClusterQuietBufferErrorCode.DuplicateCandidateIdentity,
                    "pool.candidates", "Duplicate TerrainCluster identity and transform: " + group.Key);
        }

        private static void ValidateDigest(
            string expected,
            string actual,
            string path,
            ICollection<TerrainClusterQuietBufferError> errors)
        {
            if (string.IsNullOrEmpty(actual) || !string.Equals(expected, actual, StringComparison.Ordinal))
                Add(errors, TerrainClusterQuietBufferErrorCode.ArtifactDigestMismatch,
                    path, "Expected digest differs from the supplied artifact digest.");
        }

        private static string ComputeCandidateDigest(
            TerrainClusterQuietBufferProfile profile,
            ProjectedClusterPort entry,
            ProjectedClusterPort exit,
            IEnumerable<int> routeTypes,
            IEnumerable<ClusterChunkCoord> activeChunks,
            IEnumerable<ClusterChunkCoord> baselineChunks,
            IEnumerable<TerrainClusterQuietBufferChunkEvidence> chunkEvidence,
            int rewardCount,
            int markerCount,
            int hazardCount)
        {
            var material = new StringBuilder();
            Append(material, "RULESET", RulesetVersion);
            Append(material, "ID", profile.QuietBufferId);
            Append(material, "BIOME", Number(profile.Biome.Order), profile.Biome.CanonicalId);
            foreach (var value in profile.SupportedUses.Distinct().OrderBy(value => value)) Append(material, "USE", Number((int)value));
            foreach (var value in profile.CompatiblePacingRoles.Distinct().OrderBy(value => value)) Append(material, "PACING", Number((int)value));
            foreach (var value in profile.CompatibleAccessClasses.Distinct().OrderBy(value => value)) Append(material, "ACCESS", Number((int)value));
            Append(material, "CLUSTER", profile.LocalCanvas.ClusterId.Value, Number((int)profile.LocalCanvas.Transform));
            foreach (var value in activeChunks.OrderBy(value => value)) Append(material, "CHUNK", Coordinate(value));
            Append(material, "ENTRY", entry.PortId, Number((int)entry.CompiledOutwardSide), Tile(entry.CompiledCoordinate));
            Append(material, "EXIT", exit.PortId, Number((int)exit.CompiledOutwardSide), Tile(exit.CompiledCoordinate));
            foreach (var value in routeTypes.OrderBy(value => value)) Append(material, "ROUTE_TYPE", Number(value));
            foreach (var value in profile.RouteWitnessReport.BaselineRoute.OrderedNodeIds) Append(material, "BASE_NODE", value);
            foreach (var value in profile.RouteWitnessReport.BaselineRoute.OrderedEdges) Append(material, "BASE_EDGE", value.EdgeId, Number((int)value.MovementKind), Number(value.EstimatedDurationMilliseconds));
            foreach (var value in baselineChunks.OrderBy(value => value)) Append(material, "BASE_CHUNK", Coordinate(value));
            foreach (var value in chunkEvidence.OrderBy(value => value.Chunk))
                Append(material, "TERRAIN", Coordinate(value.Chunk), Number(value.SolidCount), Number(value.AirCount), Number(value.BaselineCoordinateCount));
            Append(material, "QUIET_ZERO", Number(rewardCount), Number(markerCount), Number(hazardCount),
                Number(profile.PatternRenderReport.ProtectedWriteCount), Number(profile.PatternRenderReport.ProtectedValueChangeCount));
            Append(material, "MAP11_01", profile.LocalCanvas.SourceFootprintDigest, profile.LocalCanvas.CanonicalDigest);
            Append(material, "MAP11_02", profile.RoleSocketContract.SourceContractDigest, profile.RoleSocketContract.CanonicalDigest);
            Append(material, "MAP11_03", profile.TraversalCompilation.CanonicalDigest);
            Append(material, "MAP11_04", profile.RouteWitnessReport.RulesetId, profile.RouteWitnessReport.CanonicalDigest);
            Append(material, "MAP11_05", profile.PatternRenderReport.ZoneMap.CanonicalDigest,
                profile.PatternRenderReport.InitialWorkingCanvas.CanonicalDigest,
                profile.PatternRenderReport.FinalWorkingCanvas.CanonicalDigest,
                profile.PatternRenderReport.CanonicalDigest);
            return Hash(material);
        }

        private static string ComputePoolDigest(
            IEnumerable<TerrainClusterQuietBufferCandidate> candidates)
        {
            var copy = candidates.OrderBy(value => value.QuietBufferId, StringComparer.Ordinal).ToArray();
            var material = new StringBuilder();
            Append(material, "RULESET", RulesetVersion);
            foreach (var value in copy) Append(material, "CANDIDATE", value.QuietBufferId, value.CanonicalDigest);
            AppendIndex(material, "BIOME", copy.SelectMany(value => new[] { Index(value.Biome.Order, value) }));
            AppendIndex(material, "USE", copy.SelectMany(value => value.SupportedUses.Select(key => Index((int)key, value))));
            AppendIndex(material, "ENTRY", copy.Select(value => Index((int)value.EntrySide, value)));
            AppendIndex(material, "EXIT", copy.Select(value => Index((int)value.ExitSide, value)));
            AppendIndex(material, "ROUTE", copy.SelectMany(value => value.CompatibleRouteTypes.Select(key => Index(key, value))));
            AppendIndex(material, "PACING", copy.SelectMany(value => value.CompatiblePacingRoles.Select(key => Index((int)key, value))));
            AppendIndex(material, "ACCESS", copy.SelectMany(value => value.CompatibleAccessClasses.Select(key => Index((int)key, value))));
            return Hash(material);
        }

        private static string ComputeQueryDigest(
            string poolDigest,
            TerrainClusterQuietBufferQuery query,
            IEnumerable<TerrainClusterQuietBufferCandidate> matches)
        {
            var material = new StringBuilder();
            Append(material, "RULESET", RulesetVersion);
            Append(material, "POOL", poolDigest);
            Append(material, "QUERY", Number(query.Biome.Order), Number((int)query.Use),
                Number((int)query.RequiredEntrySide), Number((int)query.RequiredExitSide),
                Number(query.RequiredRouteType), Number((int)query.RequiredPacingRole),
                Number((int)query.RequiredAccessClass),
                query.MaximumActiveChunkCount.HasValue ? Number(query.MaximumActiveChunkCount.Value) : "NONE");
            foreach (var value in matches.OrderBy(value => value.QuietBufferId, StringComparer.Ordinal))
                Append(material, "MATCH", value.QuietBufferId, value.CanonicalDigest);
            return Hash(material);
        }

        private static KeyValuePair<int, TerrainClusterQuietBufferCandidate> Index(
            int key,
            TerrainClusterQuietBufferCandidate candidate)
        {
            return new KeyValuePair<int, TerrainClusterQuietBufferCandidate>(key, candidate);
        }

        private static void AppendIndex(
            StringBuilder material,
            string name,
            IEnumerable<KeyValuePair<int, TerrainClusterQuietBufferCandidate>> memberships)
        {
            foreach (var bucket in memberships.GroupBy(value => value.Key).OrderBy(value => value.Key))
                Append(material, "INDEX", name, Number(bucket.Key), string.Join(",", bucket
                    .Select(value => value.Value.QuietBufferId).Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal)));
        }

        private static bool IsCanonicalPool(TerrainClusterQuietBufferPool pool)
        {
            var ids = pool.Candidates.Select(value => value.QuietBufferId).ToArray();
            if (!ids.SequenceEqual(ids.OrderBy(value => value, StringComparer.Ordinal))) return false;
            return AllBucketsCanonical(pool.ByBiome.Values) && AllBucketsCanonical(pool.ByUse.Values) &&
                   AllBucketsCanonical(pool.ByEntrySide.Values) && AllBucketsCanonical(pool.ByExitSide.Values) &&
                   AllBucketsCanonical(pool.ByRouteType.Values) && AllBucketsCanonical(pool.ByPacingRole.Values) &&
                   AllBucketsCanonical(pool.ByAccessClass.Values);
        }

        private static bool AllBucketsCanonical(
            IEnumerable<IReadOnlyList<TerrainClusterQuietBufferCandidate>> buckets)
        {
            return buckets.All(bucket => bucket.Select(value => value.QuietBufferId).SequenceEqual(
                bucket.Select(value => value.QuietBufferId).OrderBy(value => value, StringComparer.Ordinal)));
        }

        private static TerrainClusterQuietBufferResult Failure(
            IEnumerable<TerrainClusterQuietBufferError> errors)
        {
            return new TerrainClusterQuietBufferResult(null, null, errors);
        }

        private static void Add(
            ICollection<TerrainClusterQuietBufferError> errors,
            TerrainClusterQuietBufferErrorCode code,
            string path,
            string detail)
        {
            errors.Add(new TerrainClusterQuietBufferError(code, path, detail));
        }

        private static string Hash(StringBuilder material)
        {
            using (var sha256 = SHA256.Create())
            {
                return string.Concat(sha256.ComputeHash(Encoding.UTF8.GetBytes(material.ToString()))
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        private static void Append(StringBuilder material, params string[] fields)
        {
            if (material.Length != 0) material.Append('\n');
            material.Append(string.Join("|", fields));
        }

        private static string Number(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string Coordinate(ClusterChunkCoord value)
        {
            return Number(value.X) + "," + Number(value.Y);
        }

        private static string Tile(StarNight.Map.WorldGeneration.Domain.LocalTileCoord value)
        {
            return Number(value.X) + "," + Number(value.Y);
        }
    }
}
