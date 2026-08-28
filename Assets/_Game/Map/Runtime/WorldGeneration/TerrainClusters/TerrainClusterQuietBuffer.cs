using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Pipeline;

namespace StarNight.Map.WorldGeneration.TerrainClusters
{
    public enum TerrainClusterQuietBufferUse
    {
        BeforeLandmark = 1,
        AfterLandmark = 2,
        UnplacedSpace = 3,
    }

    public sealed class TerrainClusterQuietBufferProfile
    {
        private readonly ReadOnlyCollection<TerrainClusterQuietBufferUse> supportedUses;
        private readonly ReadOnlyCollection<PacingRole> compatiblePacingRoles;
        private readonly ReadOnlyCollection<AccessClass> compatibleAccessClasses;

        public TerrainClusterQuietBufferProfile(
            string quietBufferId,
            MoonpalaceBiomeId biome,
            IEnumerable<TerrainClusterQuietBufferUse> supportedUses,
            IEnumerable<PacingRole> compatiblePacingRoles,
            IEnumerable<AccessClass> compatibleAccessClasses,
            TerrainClusterLocalCanvas localCanvas,
            string expectedLocalCanvasDigest,
            TerrainClusterRoleSocketContract roleSocketContract,
            string expectedRoleSocketContractDigest,
            TerrainClusterTraversalCompilation traversalCompilation,
            string expectedTraversalCompilationDigest,
            TerrainClusterRouteWitnessReport routeWitnessReport,
            string expectedRouteWitnessDigest,
            TerrainClusterPatternRenderReport patternRenderReport,
            string expectedPatternRenderDigest)
        {
            QuietBufferId = quietBufferId ?? string.Empty;
            Biome = biome;
            this.supportedUses = Copy(supportedUses);
            this.compatiblePacingRoles = Copy(compatiblePacingRoles);
            this.compatibleAccessClasses = Copy(compatibleAccessClasses);
            LocalCanvas = localCanvas;
            ExpectedLocalCanvasDigest = expectedLocalCanvasDigest ?? string.Empty;
            RoleSocketContract = roleSocketContract;
            ExpectedRoleSocketContractDigest = expectedRoleSocketContractDigest ?? string.Empty;
            TraversalCompilation = traversalCompilation;
            ExpectedTraversalCompilationDigest = expectedTraversalCompilationDigest ?? string.Empty;
            RouteWitnessReport = routeWitnessReport;
            ExpectedRouteWitnessDigest = expectedRouteWitnessDigest ?? string.Empty;
            PatternRenderReport = patternRenderReport;
            ExpectedPatternRenderDigest = expectedPatternRenderDigest ?? string.Empty;
        }

        public string QuietBufferId { get; }
        public MoonpalaceBiomeId Biome { get; }
        public IReadOnlyList<TerrainClusterQuietBufferUse> SupportedUses => supportedUses;
        public IReadOnlyList<PacingRole> CompatiblePacingRoles => compatiblePacingRoles;
        public IReadOnlyList<AccessClass> CompatibleAccessClasses => compatibleAccessClasses;
        public TerrainClusterLocalCanvas LocalCanvas { get; }
        public string ExpectedLocalCanvasDigest { get; }
        public TerrainClusterRoleSocketContract RoleSocketContract { get; }
        public string ExpectedRoleSocketContractDigest { get; }
        public TerrainClusterTraversalCompilation TraversalCompilation { get; }
        public string ExpectedTraversalCompilationDigest { get; }
        public TerrainClusterRouteWitnessReport RouteWitnessReport { get; }
        public string ExpectedRouteWitnessDigest { get; }
        public TerrainClusterPatternRenderReport PatternRenderReport { get; }
        public string ExpectedPatternRenderDigest { get; }

        private static ReadOnlyCollection<T> Copy<T>(IEnumerable<T> source)
            where T : struct
        {
            var copy = (source ?? Array.Empty<T>()).ToArray();
            Array.Sort(copy);
            return new ReadOnlyCollection<T>(copy);
        }
    }

    public sealed class TerrainClusterQuietBufferChunkEvidence
    {
        internal TerrainClusterQuietBufferChunkEvidence(
            ClusterChunkCoord chunk,
            int solidCount,
            int airCount,
            int baselineCoordinateCount)
        {
            Chunk = chunk;
            SolidCount = solidCount;
            AirCount = airCount;
            BaselineCoordinateCount = baselineCoordinateCount;
        }

        public ClusterChunkCoord Chunk { get; }
        public int SolidCount { get; }
        public int AirCount { get; }
        public int BaselineCoordinateCount { get; }
    }

    public sealed class TerrainClusterQuietBufferCandidate
    {
        private readonly ReadOnlyCollection<TerrainClusterQuietBufferUse> supportedUses;
        private readonly ReadOnlyCollection<PacingRole> compatiblePacingRoles;
        private readonly ReadOnlyCollection<AccessClass> compatibleAccessClasses;
        private readonly ReadOnlyCollection<int> compatibleRouteTypes;
        private readonly ReadOnlyCollection<ClusterChunkCoord> activeChunks;
        private readonly ReadOnlyCollection<string> baselineNodeIds;
        private readonly ReadOnlyCollection<string> baselineEdgeIds;
        private readonly ReadOnlyCollection<ClusterChunkCoord> baselineCoveredChunks;
        private readonly ReadOnlyCollection<TerrainClusterQuietBufferChunkEvidence> chunkEvidence;

        internal TerrainClusterQuietBufferCandidate(
            TerrainClusterQuietBufferProfile profile,
            ProjectedClusterPort entryPort,
            ProjectedClusterPort exitPort,
            IEnumerable<int> compatibleRouteTypes,
            IEnumerable<ClusterChunkCoord> activeChunks,
            IEnumerable<string> baselineNodeIds,
            IEnumerable<string> baselineEdgeIds,
            IEnumerable<ClusterChunkCoord> baselineCoveredChunks,
            IEnumerable<TerrainClusterQuietBufferChunkEvidence> chunkEvidence,
            int rewardRoleCount,
            int markerCount,
            int hazardCount,
            string canonicalDigest)
        {
            QuietBufferId = profile.QuietBufferId;
            Biome = profile.Biome;
            supportedUses = Copy(profile.SupportedUses);
            compatiblePacingRoles = Copy(profile.CompatiblePacingRoles);
            compatibleAccessClasses = Copy(profile.CompatibleAccessClasses);
            LocalCanvas = profile.LocalCanvas;
            RoleSocketContract = profile.RoleSocketContract;
            TraversalCompilation = profile.TraversalCompilation;
            RouteWitnessReport = profile.RouteWitnessReport;
            PatternRenderReport = profile.PatternRenderReport;
            ClusterId = profile.LocalCanvas.ClusterId;
            Transform = profile.LocalCanvas.Transform;
            EntryPortId = entryPort.PortId;
            ExitPortId = exitPort.PortId;
            EntrySide = entryPort.CompiledOutwardSide;
            ExitSide = exitPort.CompiledOutwardSide;
            this.compatibleRouteTypes = Copy(compatibleRouteTypes);
            this.activeChunks = Copy(activeChunks);
            this.baselineNodeIds = Copy(baselineNodeIds, StringComparer.Ordinal);
            this.baselineEdgeIds = Copy(baselineEdgeIds, StringComparer.Ordinal);
            this.baselineCoveredChunks = Copy(baselineCoveredChunks);
            this.chunkEvidence = new ReadOnlyCollection<TerrainClusterQuietBufferChunkEvidence>(
                (chunkEvidence ?? Array.Empty<TerrainClusterQuietBufferChunkEvidence>())
                    .OrderBy(value => value.Chunk).ToArray());
            RewardRoleCount = rewardRoleCount;
            MarkerCount = markerCount;
            HazardCount = hazardCount;
            LocalCanvasDigest = profile.LocalCanvas.CanonicalDigest;
            RoleSocketContractDigest = profile.RoleSocketContract.CanonicalDigest;
            TraversalCompilationDigest = profile.TraversalCompilation.CanonicalDigest;
            RouteWitnessDigest = profile.RouteWitnessReport.CanonicalDigest;
            PatternRenderDigest = profile.PatternRenderReport.CanonicalDigest;
            InitialWorkingCanvasDigest = profile.PatternRenderReport.InitialWorkingCanvas.CanonicalDigest;
            FinalWorkingCanvasDigest = profile.PatternRenderReport.FinalWorkingCanvas.CanonicalDigest;
            CanonicalDigest = canonicalDigest ?? string.Empty;
        }

        public string QuietBufferId { get; }
        public MoonpalaceBiomeId Biome { get; }
        public IReadOnlyList<TerrainClusterQuietBufferUse> SupportedUses => supportedUses;
        public IReadOnlyList<PacingRole> CompatiblePacingRoles => compatiblePacingRoles;
        public IReadOnlyList<AccessClass> CompatibleAccessClasses => compatibleAccessClasses;
        public TerrainClusterId ClusterId { get; }
        public ClusterFootprintTransform Transform { get; }
        public IReadOnlyList<ClusterChunkCoord> ActiveChunks => activeChunks;
        public int ActiveChunkCount => activeChunks.Count;
        public string EntryPortId { get; }
        public string ExitPortId { get; }
        public ClusterPortSide EntrySide { get; }
        public ClusterPortSide ExitSide { get; }
        public IReadOnlyList<int> CompatibleRouteTypes => compatibleRouteTypes;
        public IReadOnlyList<string> BaselineNodeIds => baselineNodeIds;
        public IReadOnlyList<string> BaselineEdgeIds => baselineEdgeIds;
        public IReadOnlyList<ClusterChunkCoord> BaselineCoveredChunks => baselineCoveredChunks;
        public IReadOnlyList<TerrainClusterQuietBufferChunkEvidence> ChunkEvidence => chunkEvidence;
        public int RewardRoleCount { get; }
        public int MarkerCount { get; }
        public int HazardCount { get; }
        public int ProtectedWriteCount => PatternRenderReport.ProtectedWriteCount;
        public int ProtectedValueChangeCount => PatternRenderReport.ProtectedValueChangeCount;
        public string SourceContractDigest => RoleSocketContract.SourceContractDigest;
        public string LocalCanvasDigest { get; }
        public string RoleSocketContractDigest { get; }
        public string TraversalCompilationDigest { get; }
        public string RouteWitnessDigest { get; }
        public string PatternRenderDigest { get; }
        public string InitialWorkingCanvasDigest { get; }
        public string FinalWorkingCanvasDigest { get; }
        public string CanonicalDigest { get; }
        public TerrainClusterLocalCanvas LocalCanvas { get; }
        public TerrainClusterRoleSocketContract RoleSocketContract { get; }
        public TerrainClusterTraversalCompilation TraversalCompilation { get; }
        public TerrainClusterRouteWitnessReport RouteWitnessReport { get; }
        public TerrainClusterPatternRenderReport PatternRenderReport { get; }

        public bool Supports(TerrainClusterQuietBufferQuery query)
        {
            return query != null && Biome == query.Biome &&
                   supportedUses.Contains(query.Use) && EntrySide == query.RequiredEntrySide &&
                   ExitSide == query.RequiredExitSide && compatibleRouteTypes.Contains(query.RequiredRouteType) &&
                   compatiblePacingRoles.Contains(query.RequiredPacingRole) &&
                   compatibleAccessClasses.Contains(query.RequiredAccessClass) &&
                   (!query.MaximumActiveChunkCount.HasValue || ActiveChunkCount <= query.MaximumActiveChunkCount.Value);
        }

        private static ReadOnlyCollection<T> Copy<T>(IEnumerable<T> source)
        {
            return new ReadOnlyCollection<T>((source ?? Array.Empty<T>()).Distinct().OrderBy(value => value).ToArray());
        }

        private static ReadOnlyCollection<string> Copy(
            IEnumerable<string> source,
            StringComparer comparer)
        {
            return new ReadOnlyCollection<string>((source ?? Array.Empty<string>())
                .Select(value => value ?? string.Empty).Distinct(comparer).OrderBy(value => value, comparer).ToArray());
        }
    }

    public enum TerrainClusterQuietBufferErrorCode
    {
        MissingInput = 1,
        ArtifactIdentityMismatch = 2,
        ArtifactDigestMismatch = 3,
        InvalidQuietBufferId = 4,
        DuplicateQuietBufferId = 5,
        InvalidBiome = 6,
        InvalidUseKind = 7,
        InvalidPacingCompatibility = 8,
        InvalidAccessCompatibility = 9,
        InvalidFootprintSize = 10,
        EntryExitChunkMismatch = 11,
        BaselineCoverageMismatch = 12,
        WorkingCanvasCoverageMismatch = 13,
        EmptyChunkTerrain = 14,
        RewardRoleNotQuiet = 15,
        MarkerNotQuiet = 16,
        HazardNotQuiet = 17,
        ProtectedMutationDetected = 18,
        DuplicateCandidateIdentity = 19,
        EmptyPool = 20,
        InvalidQuery = 21,
        PoolDigestMismatch = 22,
        NonCanonicalPublication = 23,
    }

    public sealed class TerrainClusterQuietBufferError :
        IEquatable<TerrainClusterQuietBufferError>,
        IComparable<TerrainClusterQuietBufferError>
    {
        public TerrainClusterQuietBufferError(
            TerrainClusterQuietBufferErrorCode code,
            string path,
            string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public TerrainClusterQuietBufferErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(TerrainClusterQuietBufferError other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return comparison != 0 ? comparison : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(TerrainClusterQuietBufferError other)
        {
            return other != null && Code == other.Code &&
                   string.Equals(Path, other.Path, StringComparison.Ordinal) &&
                   string.Equals(Detail, other.Detail, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TerrainClusterQuietBufferError);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Code;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Path);
                return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Detail);
            }
        }

        public override string ToString()
        {
            return Code + "|" + Path + "|" + Detail;
        }
    }
}
