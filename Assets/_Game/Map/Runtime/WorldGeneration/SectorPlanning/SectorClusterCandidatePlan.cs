using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum SectorClusterCandidateReason
    {
        BiomeCompatible,
        PacingPrimaryMatch,
        PacingCandidateMatch,
        RouteSocketCompatible,
        AccessCompatible,
        FootprintFitsFreeGrid,
        AvoidsFixedAnchor,
        DensityWithinPolicy,
        QuietPoolCompatible,
        SpecialAdjacencyCompatible,
        ConstraintLargeFirst,
    }

    public enum SectorClusterCandidateErrorCode
    {
        MissingInput,
        MissingAnchorPlan,
        MissingAssignment,
        SectorMismatch,
        MissingClusterCatalog,
        NoCandidateForSector,
        DuplicateCandidate,
        InvalidFootprint,
        FootprintOutOfBounds,
        AnchorOverlap,
        SocketMismatch,
        AccessMismatch,
        PacingMismatch,
        BiomeMismatch,
        DensityOutOfPolicy,
        PlacementOverlap,
        PlacementOrderViolation,
        SolverMutationClaim,
        RngMutationClaim,
        TileMutationClaim,
        NonCanonicalPublication,
        LowerRankedCandidate,
    }

    public readonly struct SectorClusterFootprintCell :
        IEquatable<SectorClusterFootprintCell>, IComparable<SectorClusterFootprintCell>
    {
        public SectorClusterFootprintCell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        public SectorFixedAnchorRect ToTileRect()
        {
            return new SectorFixedAnchorRect(
                X * WorldGenConstants.MicroChunkWidthTiles,
                Y * WorldGenConstants.MicroChunkHeightTiles,
                WorldGenConstants.MicroChunkWidthTiles,
                WorldGenConstants.MicroChunkHeightTiles);
        }

        public int CompareTo(SectorClusterFootprintCell other)
        {
            var comparison = Y.CompareTo(other.Y);
            return comparison != 0 ? comparison : X.CompareTo(other.X);
        }

        public bool Equals(SectorClusterFootprintCell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is SectorClusterFootprintCell other && Equals(other);
        public override int GetHashCode() => (X * 397) ^ Y;
        public override string ToString() => X.ToString(CultureInfo.InvariantCulture) + "," + Y.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class SectorClusterFootprintPlacement
    {
        private readonly ReadOnlyCollection<SectorClusterFootprintCell> cells;
        private readonly ReadOnlyCollection<SectorFixedAnchorRect> tileRects;

        internal SectorClusterFootprintPlacement(
            int originX,
            int originY,
            IEnumerable<SectorClusterFootprintCell> sourceCells,
            int anchorProximityPenalty)
        {
            OriginX = originX;
            OriginY = originY;
            cells = new ReadOnlyCollection<SectorClusterFootprintCell>((sourceCells ?? Array.Empty<SectorClusterFootprintCell>())
                .OrderBy(value => value).ToArray());
            tileRects = new ReadOnlyCollection<SectorFixedAnchorRect>(cells.Select(value => value.ToTileRect()).ToArray());
            AnchorProximityPenalty = anchorProximityPenalty;
        }

        public int OriginX { get; }
        public int OriginY { get; }
        public IReadOnlyList<SectorClusterFootprintCell> Cells => cells;
        public IReadOnlyList<SectorFixedAnchorRect> TileRects => tileRects;
        public int AnchorProximityPenalty { get; }
    }

    public sealed class SectorClusterSourceProjection
    {
        private readonly ReadOnlyCollection<PacingRole> compatiblePacingRoles;
        private readonly ReadOnlyCollection<int> compatibleRouteTypes;
        private readonly ReadOnlyCollection<AccessClass> compatibleAccessClasses;
        private readonly ReadOnlyCollection<string> compatibleSocketIds;
        private readonly ReadOnlyCollection<SectorClusterFootprintCell> footprintCells;
        private readonly ReadOnlyCollection<SectorClusterFootprintCell> approvedOrigins;

        public SectorClusterSourceProjection(
            TerrainClusterId clusterId,
            SpineVariantId variantId,
            ClusterFootprintTransform transform,
            MoonpalaceBiomeId biome,
            IEnumerable<PacingRole> sourceCompatiblePacingRoles,
            IEnumerable<int> sourceCompatibleRouteTypes,
            IEnumerable<AccessClass> sourceCompatibleAccessClasses,
            IEnumerable<string> sourceCompatibleSocketIds,
            IEnumerable<SectorClusterFootprintCell> sourceFootprintCells,
            IEnumerable<SectorClusterFootprintCell> sourceApprovedOrigins,
            int minimumDensityCells,
            int maximumDensityCells,
            bool quietPoolCompatible,
            bool specialAdjacencyCompatible,
            int catalogOrder,
            int variantOrder)
        {
            ClusterId = clusterId;
            VariantId = variantId;
            Transform = transform;
            Biome = biome;
            compatiblePacingRoles = Copy(sourceCompatiblePacingRoles);
            compatibleRouteTypes = Copy(sourceCompatibleRouteTypes);
            compatibleAccessClasses = Copy(sourceCompatibleAccessClasses);
            compatibleSocketIds = new ReadOnlyCollection<string>((sourceCompatibleSocketIds ?? Array.Empty<string>())
                .Select(value => value ?? string.Empty).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
            footprintCells = new ReadOnlyCollection<SectorClusterFootprintCell>((sourceFootprintCells ?? Array.Empty<SectorClusterFootprintCell>())
                .OrderBy(value => value).ToArray());
            approvedOrigins = new ReadOnlyCollection<SectorClusterFootprintCell>((sourceApprovedOrigins ?? Array.Empty<SectorClusterFootprintCell>())
                .Distinct().OrderBy(value => value).ToArray());
            MinimumDensityCells = minimumDensityCells;
            MaximumDensityCells = maximumDensityCells;
            QuietPoolCompatible = quietPoolCompatible;
            SpecialAdjacencyCompatible = specialAdjacencyCompatible;
            CatalogOrder = catalogOrder;
            VariantOrder = variantOrder;
        }

        public TerrainClusterId ClusterId { get; }
        public SpineVariantId VariantId { get; }
        public ClusterFootprintTransform Transform { get; }
        public MoonpalaceBiomeId Biome { get; }
        public IReadOnlyList<PacingRole> CompatiblePacingRoles => compatiblePacingRoles;
        public IReadOnlyList<int> CompatibleRouteTypes => compatibleRouteTypes;
        public IReadOnlyList<AccessClass> CompatibleAccessClasses => compatibleAccessClasses;
        public IReadOnlyList<string> CompatibleSocketIds => compatibleSocketIds;
        public IReadOnlyList<SectorClusterFootprintCell> FootprintCells => footprintCells;
        public IReadOnlyList<SectorClusterFootprintCell> ApprovedOrigins => approvedOrigins;
        public int MinimumDensityCells { get; }
        public int MaximumDensityCells { get; }
        public bool QuietPoolCompatible { get; }
        public bool SpecialAdjacencyCompatible { get; }
        public int CatalogOrder { get; }
        public int VariantOrder { get; }

        private static ReadOnlyCollection<T> Copy<T>(IEnumerable<T> source)
        {
            return new ReadOnlyCollection<T>((source ?? Array.Empty<T>()).Distinct().OrderBy(value => value).ToArray());
        }
    }

    public sealed class SectorClusterCandidate
    {
        private readonly ReadOnlyCollection<string> externalSocketIds;
        private readonly ReadOnlyCollection<SectorClusterFootprintCell> footprintCells;
        private readonly ReadOnlyCollection<SectorClusterFootprintPlacement> approvedPlacements;
        private readonly ReadOnlyCollection<SectorClusterCandidateReason> reasons;

        internal SectorClusterCandidate(
            SectorPlannerSectorSnapshot sector,
            SectorPacingAssignment assignment,
            SectorClusterSourceProjection source,
            IEnumerable<SectorClusterFootprintPlacement> sourcePlacements,
            IEnumerable<SectorClusterCandidateReason> sourceReasons,
            int deterministicScore)
        {
            SectorCoordinate = sector.Coordinate;
            SectorIndex = sector.SectorIndex;
            ClusterId = source.ClusterId;
            VariantId = source.VariantId;
            Transform = source.Transform;
            BiomeId = sector.Biome.BiomeId;
            MatchedPacingRole = source.CompatiblePacingRoles.Contains(assignment.PrimaryRole)
                ? assignment.PrimaryRole
                : assignment.Candidates.Select(value => value.Role).First(source.CompatiblePacingRoles.Contains);
            PrimaryPacingMatch = MatchedPacingRole == assignment.PrimaryRole;
            RouteType = sector.Route.RouteType;
            AccessClass = sector.Route.AccessClass;
            externalSocketIds = new ReadOnlyCollection<string>(sector.Route.ExternalSockets.ToArray());
            footprintCells = new ReadOnlyCollection<SectorClusterFootprintCell>(source.FootprintCells.OrderBy(value => value).ToArray());
            approvedPlacements = new ReadOnlyCollection<SectorClusterFootprintPlacement>((sourcePlacements ?? Array.Empty<SectorClusterFootprintPlacement>())
                .OrderBy(value => value.AnchorProximityPenalty).ThenBy(value => value.OriginY).ThenBy(value => value.OriginX).ToArray());
            reasons = new ReadOnlyCollection<SectorClusterCandidateReason>((sourceReasons ?? Array.Empty<SectorClusterCandidateReason>())
                .Distinct().OrderBy(value => value).ToArray());
            DeterministicScore = deterministicScore;
            CatalogOrder = source.CatalogOrder;
            VariantOrder = source.VariantOrder;
        }

        public SectorCoord SectorCoordinate { get; }
        public int SectorIndex { get; }
        public TerrainClusterId ClusterId { get; }
        public SpineVariantId VariantId { get; }
        public ClusterFootprintTransform Transform { get; }
        public string BiomeId { get; }
        public PacingRole MatchedPacingRole { get; }
        public bool PrimaryPacingMatch { get; }
        public int RouteType { get; }
        public AccessClass AccessClass { get; }
        public IReadOnlyList<string> ExternalSocketIds => externalSocketIds;
        public IReadOnlyList<SectorClusterFootprintCell> FootprintCells => footprintCells;
        public IReadOnlyList<SectorClusterFootprintPlacement> ApprovedPlacements => approvedPlacements;
        public IReadOnlyList<SectorClusterCandidateReason> Reasons => reasons;
        public int DeterministicScore { get; }
        public int CatalogOrder { get; }
        public int VariantOrder { get; }

        internal static int Compare(SectorClusterCandidate left, SectorClusterCandidate right)
        {
            var comparison = left.SectorIndex.CompareTo(right.SectorIndex);
            if (comparison != 0) return comparison;
            comparison = right.PrimaryPacingMatch.CompareTo(left.PrimaryPacingMatch);
            if (comparison != 0) return comparison;
            comparison = right.FootprintCells.Count.CompareTo(left.FootprintCells.Count);
            if (comparison != 0) return comparison;
            comparison = left.ApprovedPlacements[0].AnchorProximityPenalty.CompareTo(right.ApprovedPlacements[0].AnchorProximityPenalty);
            if (comparison != 0) return comparison;
            comparison = left.CatalogOrder.CompareTo(right.CatalogOrder);
            if (comparison != 0) return comparison;
            comparison = left.VariantOrder.CompareTo(right.VariantOrder);
            if (comparison != 0) return comparison;
            comparison = left.ClusterId.CompareTo(right.ClusterId);
            return comparison != 0 ? comparison : left.VariantId.CompareTo(right.VariantId);
        }
    }

    public sealed class SectorClusterCandidateBuildRequest
    {
        private readonly ReadOnlyCollection<SectorPacingAssignment> assignments;
        private readonly ReadOnlyCollection<SectorClusterSourceProjection> clusterCatalog;

        public SectorClusterCandidateBuildRequest(
            SectorPlannerInput input,
            IEnumerable<SectorPacingAssignment> sourceAssignments,
            SectorFixedAnchorPlan anchorPlan,
            IEnumerable<SectorClusterSourceProjection> sourceClusterCatalog,
            string publicationLabel,
            string expectedCanonicalDigest = "",
            int solverMutationCount = 0,
            int randomDrawCount = 0,
            int tileWriteCount = 0)
        {
            Input = input;
            assignments = new ReadOnlyCollection<SectorPacingAssignment>((sourceAssignments ?? Array.Empty<SectorPacingAssignment>()).Where(value => value != null).ToArray());
            AnchorPlan = anchorPlan;
            clusterCatalog = new ReadOnlyCollection<SectorClusterSourceProjection>((sourceClusterCatalog ?? Array.Empty<SectorClusterSourceProjection>()).Where(value => value != null).ToArray());
            PublicationLabel = publicationLabel ?? string.Empty;
            ExpectedCanonicalDigest = expectedCanonicalDigest ?? string.Empty;
            SolverMutationCount = solverMutationCount;
            RandomDrawCount = randomDrawCount;
            TileWriteCount = tileWriteCount;
        }

        public SectorPlannerInput Input { get; }
        public IReadOnlyList<SectorPacingAssignment> Assignments => assignments;
        public SectorFixedAnchorPlan AnchorPlan { get; }
        public IReadOnlyList<SectorClusterSourceProjection> ClusterCatalog => clusterCatalog;
        public string PublicationLabel { get; }
        public string ExpectedCanonicalDigest { get; }
        public int SolverMutationCount { get; }
        public int RandomDrawCount { get; }
        public int TileWriteCount { get; }
    }

    public sealed class SectorClusterCandidateSet
    {
        private readonly ReadOnlyCollection<SectorClusterCandidate> candidates;
        private readonly ReadOnlyDictionary<int, int> candidateCountBySectorIndex;
        private readonly ReadOnlyDictionary<SectorClusterCandidateErrorCode, int> rejectedCountByReason;

        internal SectorClusterCandidateSet(
            string publicationLabel,
            string plannerInputDigest,
            string anchorPlanDigest,
            int sectorCount,
            IEnumerable<SectorClusterCandidate> sourceCandidates,
            IDictionary<SectorClusterCandidateErrorCode, int> sourceRejectedCounts,
            string canonicalDigest)
        {
            PublicationLabel = publicationLabel ?? string.Empty;
            PlannerInputDigest = plannerInputDigest ?? string.Empty;
            AnchorPlanDigest = anchorPlanDigest ?? string.Empty;
            SectorCount = sectorCount;
            var ordered = (sourceCandidates ?? Array.Empty<SectorClusterCandidate>()).OrderBy(value => value, Comparer<SectorClusterCandidate>.Create(SectorClusterCandidate.Compare)).ToArray();
            candidates = new ReadOnlyCollection<SectorClusterCandidate>(ordered);
            candidateCountBySectorIndex = new ReadOnlyDictionary<int, int>(ordered.GroupBy(value => value.SectorIndex)
                .OrderBy(value => value.Key).ToDictionary(value => value.Key, value => value.Count()));
            rejectedCountByReason = new ReadOnlyDictionary<SectorClusterCandidateErrorCode, int>((sourceRejectedCounts ?? new Dictionary<SectorClusterCandidateErrorCode, int>())
                .Where(value => value.Value > 0).OrderBy(value => value.Key).ToDictionary(value => value.Key, value => value.Value));
            CanonicalDigest = canonicalDigest ?? string.Empty;
        }

        public string PublicationLabel { get; }
        public string PlannerInputDigest { get; }
        public string AnchorPlanDigest { get; }
        public int SectorCount { get; }
        public IReadOnlyList<SectorClusterCandidate> Candidates => candidates;
        public int CandidateCount => candidates.Count;
        public IReadOnlyDictionary<int, int> CandidateCountBySectorIndex => candidateCountBySectorIndex;
        public IReadOnlyDictionary<SectorClusterCandidateErrorCode, int> RejectedCountByReason => rejectedCountByReason;
        public int RejectedCandidateCount => rejectedCountByReason.Values.Sum();
        public string CanonicalDigest { get; }
        public int SolverInvocationCount => 0;
        public int RandomDrawCount => 0;
        public int TileWriteCount => 0;

        public IReadOnlyList<SectorClusterCandidate> CandidatesForSector(SectorCoord coordinate)
        {
            return new ReadOnlyCollection<SectorClusterCandidate>(candidates.Where(value => value.SectorCoordinate.Equals(coordinate)).ToArray());
        }
    }

    public sealed class SectorClusterCandidateError : IEquatable<SectorClusterCandidateError>, IComparable<SectorClusterCandidateError>
    {
        public SectorClusterCandidateError(SectorClusterCandidateErrorCode code, string subject, string detail)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public SectorClusterCandidateErrorCode Code { get; }
        public string Subject { get; }
        public string Detail { get; }

        public int CompareTo(SectorClusterCandidateError other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0 ? comparison : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(SectorClusterCandidateError other) => other != null && CompareTo(other) == 0;
        public override bool Equals(object obj) => Equals(obj as SectorClusterCandidateError);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(ToString());
        public override string ToString() => Code + "|" + Subject + "|" + Detail;
    }

    public sealed class SectorClusterCandidateBuildResult
    {
        private readonly ReadOnlyCollection<SectorClusterCandidateError> errors;

        internal SectorClusterCandidateBuildResult(SectorClusterCandidateSet candidateSet, IEnumerable<SectorClusterCandidateError> sourceErrors)
        {
            var ordered = (sourceErrors ?? Array.Empty<SectorClusterCandidateError>()).Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            errors = new ReadOnlyCollection<SectorClusterCandidateError>(ordered);
            CandidateSet = ordered.Length == 0 ? candidateSet : null;
            CanonicalDigest = CandidateSet == null ? string.Empty : CandidateSet.CanonicalDigest;
        }

        public bool Success => CandidateSet != null && errors.Count == 0;
        public SectorClusterCandidateSet CandidateSet { get; }
        public string CanonicalDigest { get; }
        public IReadOnlyList<SectorClusterCandidateError> Errors => errors;
        public int MutationCount => 0;
        public int SolverInvocationCount => 0;
        public int RandomDrawCount => 0;
        public int TileWriteCount => 0;
    }

    public sealed class SectorClusterPlacement
    {
        private readonly ReadOnlyCollection<SectorClusterFootprintCell> cells;
        private readonly ReadOnlyCollection<SectorFixedAnchorRect> tileRects;

        internal SectorClusterPlacement(SectorClusterCandidate candidate, SectorClusterFootprintPlacement footprint, int constraintClass)
        {
            SectorCoordinate = candidate.SectorCoordinate;
            SectorIndex = candidate.SectorIndex;
            ClusterId = candidate.ClusterId;
            VariantId = candidate.VariantId;
            Transform = candidate.Transform;
            MatchedPacingRole = candidate.MatchedPacingRole;
            PrimaryPacingMatch = candidate.PrimaryPacingMatch;
            ConstraintClass = constraintClass;
            OriginX = footprint.OriginX;
            OriginY = footprint.OriginY;
            cells = new ReadOnlyCollection<SectorClusterFootprintCell>(footprint.Cells.ToArray());
            tileRects = new ReadOnlyCollection<SectorFixedAnchorRect>(footprint.TileRects.ToArray());
        }

        public SectorCoord SectorCoordinate { get; }
        public int SectorIndex { get; }
        public TerrainClusterId ClusterId { get; }
        public SpineVariantId VariantId { get; }
        public ClusterFootprintTransform Transform { get; }
        public PacingRole MatchedPacingRole { get; }
        public bool PrimaryPacingMatch { get; }
        public int ConstraintClass { get; }
        public int OriginX { get; }
        public int OriginY { get; }
        public IReadOnlyList<SectorClusterFootprintCell> Cells => cells;
        public IReadOnlyList<SectorFixedAnchorRect> TileRects => tileRects;
    }

    public sealed class SectorClusterPlacementRequest
    {
        public SectorClusterPlacementRequest(
            SectorClusterCandidateSet candidateSet,
            SectorFixedAnchorPlan anchorPlan,
            string publicationLabel,
            string expectedCanonicalDigest = "",
            int solverMutationCount = 0,
            int randomDrawCount = 0,
            int tileWriteCount = 0)
        {
            CandidateSet = candidateSet;
            AnchorPlan = anchorPlan;
            PublicationLabel = publicationLabel ?? string.Empty;
            ExpectedCanonicalDigest = expectedCanonicalDigest ?? string.Empty;
            SolverMutationCount = solverMutationCount;
            RandomDrawCount = randomDrawCount;
            TileWriteCount = tileWriteCount;
        }

        public SectorClusterCandidateSet CandidateSet { get; }
        public SectorFixedAnchorPlan AnchorPlan { get; }
        public string PublicationLabel { get; }
        public string ExpectedCanonicalDigest { get; }
        public int SolverMutationCount { get; }
        public int RandomDrawCount { get; }
        public int TileWriteCount { get; }
    }

    public sealed class SectorClusterPlacementPlan
    {
        private readonly ReadOnlyCollection<SectorClusterPlacement> placements;
        private readonly ReadOnlyDictionary<SectorClusterCandidateErrorCode, int> rejectedCountByReason;

        internal SectorClusterPlacementPlan(
            string publicationLabel,
            string candidateSetDigest,
            string anchorPlanDigest,
            IEnumerable<SectorClusterPlacement> sourcePlacements,
            IDictionary<SectorClusterCandidateErrorCode, int> sourceRejectedCounts,
            int hardAnchorFootprintCellCount,
            int freeFootprintCellCount,
            string canonicalDigest)
        {
            PublicationLabel = publicationLabel ?? string.Empty;
            CandidateSetDigest = candidateSetDigest ?? string.Empty;
            AnchorPlanDigest = anchorPlanDigest ?? string.Empty;
            placements = new ReadOnlyCollection<SectorClusterPlacement>((sourcePlacements ?? Array.Empty<SectorClusterPlacement>()).ToArray());
            rejectedCountByReason = new ReadOnlyDictionary<SectorClusterCandidateErrorCode, int>((sourceRejectedCounts ?? new Dictionary<SectorClusterCandidateErrorCode, int>())
                .Where(value => value.Value > 0).OrderBy(value => value.Key).ToDictionary(value => value.Key, value => value.Value));
            HardAnchorFootprintCellCount = hardAnchorFootprintCellCount;
            FreeFootprintCellCount = freeFootprintCellCount;
            CanonicalDigest = canonicalDigest ?? string.Empty;
        }

        public string PublicationLabel { get; }
        public string CandidateSetDigest { get; }
        public string AnchorPlanDigest { get; }
        public IReadOnlyList<SectorClusterPlacement> Placements => placements;
        public int SectorCount => placements.Count;
        public int AcceptedPlacementCount => placements.Count;
        public IReadOnlyDictionary<SectorClusterCandidateErrorCode, int> RejectedCountByReason => rejectedCountByReason;
        public int RejectedCandidateCount => rejectedCountByReason.Values.Sum();
        public int PlacedFootprintCellCount => placements.Sum(value => value.Cells.Count);
        public int HardAnchorFootprintCellCount { get; }
        public int FreeFootprintCellCount { get; }
        public int AnchorOverlapCount => 0;
        public int PlacementOverlapCount => 0;
        public string CanonicalDigest { get; }
        public bool Map14_04HandoffReady => placements.Count > 0 && AnchorOverlapCount == 0 && PlacementOverlapCount == 0;
        public int RouteSpineInvocationCount => 0;
        public int TraversalEnvelopeInvocationCount => 0;
        public int MicroPatternRenderCount => 0;
        public int ActivityPlacementCount => 0;
        public int EventPlacementCount => 0;
        public int RetryCount => 0;
        public int SolverInvocationCount => 0;
        public int RandomDrawCount => 0;
        public int TileWriteCount => 0;
        public int CanvasOwnershipClaimCount => 0;
        public int GameplaySpawnCount => 0;
    }

    public sealed class SectorClusterPlacementBuildResult
    {
        private readonly ReadOnlyCollection<SectorClusterCandidateError> errors;

        internal SectorClusterPlacementBuildResult(SectorClusterPlacementPlan plan, IEnumerable<SectorClusterCandidateError> sourceErrors)
        {
            var ordered = (sourceErrors ?? Array.Empty<SectorClusterCandidateError>()).Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            errors = new ReadOnlyCollection<SectorClusterCandidateError>(ordered);
            Plan = ordered.Length == 0 ? plan : null;
            CanonicalDigest = Plan == null ? string.Empty : Plan.CanonicalDigest;
        }

        public bool Success => Plan != null && errors.Count == 0;
        public SectorClusterPlacementPlan Plan { get; }
        public string CanonicalDigest { get; }
        public IReadOnlyList<SectorClusterCandidateError> Errors => errors;
        public int MutationCount => 0;
        public int SolverInvocationCount => 0;
        public int RandomDrawCount => 0;
        public int TileWriteCount => 0;
    }

    public static class SectorClusterCandidateCanonicalDigest
    {
        public static string Compute(SectorClusterCandidateSet candidateSet)
        {
            if (candidateSet == null) throw new ArgumentNullException(nameof(candidateSet));
            return SectorClusterCanonicalMaterial.ComputeCandidateSet(
                candidateSet.PublicationLabel,
                candidateSet.PlannerInputDigest,
                candidateSet.AnchorPlanDigest,
                candidateSet.SectorCount,
                candidateSet.Candidates,
                candidateSet.RejectedCountByReason);
        }
    }

    public static class SectorClusterPlacementCanonicalDigest
    {
        public static string Compute(SectorClusterPlacementPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            return SectorClusterCanonicalMaterial.ComputePlacementPlan(
                plan.PublicationLabel,
                plan.CandidateSetDigest,
                plan.AnchorPlanDigest,
                plan.Placements,
                plan.RejectedCountByReason,
                plan.HardAnchorFootprintCellCount,
                plan.FreeFootprintCellCount);
        }
    }

    internal static class SectorClusterCanonicalMaterial
    {
        internal static string ComputeCandidateSet(
            string publicationLabel,
            string plannerInputDigest,
            string anchorPlanDigest,
            int sectorCount,
            IEnumerable<SectorClusterCandidate> candidates,
            IReadOnlyDictionary<SectorClusterCandidateErrorCode, int> rejected)
        {
            var builder = new StringBuilder();
            Append(builder, publicationLabel, plannerInputDigest, anchorPlanDigest, sectorCount);
            foreach (var candidate in candidates.OrderBy(value => value, Comparer<SectorClusterCandidate>.Create(SectorClusterCandidate.Compare)))
            {
                Append(builder, candidate.SectorIndex, candidate.SectorCoordinate.X, candidate.SectorCoordinate.Y,
                    candidate.ClusterId.Value, candidate.VariantId.Value, candidate.Transform, candidate.BiomeId,
                    candidate.MatchedPacingRole, candidate.PrimaryPacingMatch, candidate.RouteType, candidate.AccessClass,
                    candidate.DeterministicScore, candidate.CatalogOrder, candidate.VariantOrder);
                foreach (var socket in candidate.ExternalSocketIds) Append(builder, socket);
                foreach (var cell in candidate.FootprintCells) Append(builder, cell.X, cell.Y);
                foreach (var placement in candidate.ApprovedPlacements)
                {
                    Append(builder, placement.OriginX, placement.OriginY, placement.AnchorProximityPenalty);
                    foreach (var cell in placement.Cells) Append(builder, cell.X, cell.Y);
                }
                foreach (var reason in candidate.Reasons) Append(builder, reason);
            }
            foreach (var pair in rejected.OrderBy(value => value.Key)) Append(builder, pair.Key, pair.Value);
            return Hash(builder.ToString());
        }

        internal static string ComputePlacementPlan(
            string publicationLabel,
            string candidateSetDigest,
            string anchorPlanDigest,
            IEnumerable<SectorClusterPlacement> placements,
            IReadOnlyDictionary<SectorClusterCandidateErrorCode, int> rejected,
            int hardAnchorFootprintCellCount,
            int freeFootprintCellCount)
        {
            var builder = new StringBuilder();
            Append(builder, publicationLabel, candidateSetDigest, anchorPlanDigest, hardAnchorFootprintCellCount, freeFootprintCellCount);
            foreach (var placement in placements)
            {
                Append(builder, placement.SectorIndex, placement.SectorCoordinate.X, placement.SectorCoordinate.Y,
                    placement.ClusterId.Value, placement.VariantId.Value, placement.Transform,
                    placement.MatchedPacingRole, placement.PrimaryPacingMatch, placement.ConstraintClass,
                    placement.OriginX, placement.OriginY);
                foreach (var cell in placement.Cells) Append(builder, cell.X, cell.Y);
            }
            foreach (var pair in rejected.OrderBy(value => value.Key)) Append(builder, pair.Key, pair.Value);
            return Hash(builder.ToString());
        }

        internal static void Append(StringBuilder builder, params object[] values)
        {
            foreach (var value in values)
            {
                if (builder.Length > 0) builder.Append('\n');
                switch (value)
                {
                    case null: builder.Append("<null>"); break;
                    case bool flag: builder.Append(flag ? "1" : "0"); break;
                    case IFormattable formattable: builder.Append(formattable.ToString(null, CultureInfo.InvariantCulture)); break;
                    default: builder.Append(value.ToString()); break;
                }
            }
        }

        internal static string Hash(string value)
        {
            using (var sha256 = SHA256.Create())
            {
                return string.Concat(sha256.ComputeHash(new UTF8Encoding(false).GetBytes(value ?? string.Empty))
                    .Select(item => item.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }
    }
}
