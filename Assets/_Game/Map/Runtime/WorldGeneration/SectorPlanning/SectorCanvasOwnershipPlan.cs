using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum SectorCanvasOwnerKind
    {
        SpecialRegion = 1,
        Boundary = 2,
        Spine = 3,
        TerrainCluster = 4,
        MicroPattern = 5,
        Quiet = 6,
        ActivityMarker = 7,
        EventMarker = 8,
        ReservedNoWrite = 9,
        ProtectedNoWrite = 10,
        Empty = 11,
    }

    public enum SectorCanvasOwnershipPlane
    {
        Terrain = 1,
        Protection = 2,
        Reservation = 3,
        Marker = 4,
        Evidence = 5,
    }

    public enum SectorCanvasClaimState
    {
        Winner = 1,
        SuppressedByPriority = 2,
        AllowedCoPlaneEvidence = 3,
        RejectedConflict = 4,
        RejectedForbiddenOverlap = 5,
        RejectedOutOfBounds = 6,
        RejectedMutationClaim = 7,
    }

    public enum SectorCanvasOwnershipPriority
    {
        SpecialRegion = 100,
        Boundary = 200,
        Spine = 300,
        TerrainCluster = 400,
        MicroPattern = 500,
        Quiet = 600,
        ActivityMarker = 700,
        EventMarker = 800,
        Evidence = 900,
    }

    public enum SectorCanvasOwnershipErrorCode
    {
        MissingInput,
        MissingQuietActivityEventPlan,
        MissingPatternRenderPlan,
        MissingSpineEnvelopePlan,
        SectorMismatch,
        ClaimOutOfBounds,
        DuplicateClaimIdentity,
        MissingRequiredClaim,
        MissingPriorityRule,
        ForbiddenOverlap,
        DoubleOwnerConflict,
        MarkerPlaneConflict,
        ProtectionPlaneConflict,
        ReservationPlaneConflict,
        TerrainPlaneConflict,
        SuppressedClaimWithoutWinner,
        WinnerWithoutClaim,
        OwnedCellOutOfBounds,
        CanvasCoverageMismatch,
        NonCanonicalPublication,
        SpecialPersistenceMutationClaim,
        BoundaryMutationClaim,
        SpineEnvelopeMutationClaim,
        ClusterMutationClaim,
        PatternRenderMutationClaim,
        QuietMutationClaim,
        ActivityMarkerMutationClaim,
        EventMarkerMutationClaim,
        SolverMutationClaim,
        RngMutationClaim,
        TileMutationClaim,
        SceneMutationClaim,
    }

    public sealed class SectorCanvasOwnershipError :
        IComparable<SectorCanvasOwnershipError>, IEquatable<SectorCanvasOwnershipError>
    {
        public SectorCanvasOwnershipError(
            SectorCanvasOwnershipErrorCode code,
            string subject,
            string detail)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public SectorCanvasOwnershipErrorCode Code { get; }
        public string Subject { get; }
        public string Detail { get; }

        public int CompareTo(SectorCanvasOwnershipError other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(SectorCanvasOwnershipError other) =>
            other != null && CompareTo(other) == 0;

        public override bool Equals(object obj) => Equals(obj as SectorCanvasOwnershipError);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(ToString());
        public override string ToString() => Code + "|" + Subject + "|" + Detail;
    }

    public sealed class SectorCanvasOwnershipClaim : IComparable<SectorCanvasOwnershipClaim>
    {
        public SectorCanvasOwnershipClaim(
            string claimId,
            SectorCoord sectorCoordinate,
            int sectorIndex,
            LocalTileCoord coordinate,
            SectorCanvasOwnershipPlane plane,
            SectorCanvasOwnerKind ownerKind,
            SectorCanvasOwnershipPriority priority,
            string sourceTaskId,
            string sourceObjectId,
            string sourceDigest,
            string semanticValue,
            bool required,
            bool allowSuppression,
            bool noWrite,
            bool markerOnly,
            SectorCanvasClaimState state = SectorCanvasClaimState.Winner)
        {
            ClaimId = claimId ?? string.Empty;
            SectorCoordinate = sectorCoordinate;
            SectorIndex = sectorIndex;
            Coordinate = coordinate;
            Plane = plane;
            OwnerKind = ownerKind;
            Priority = priority;
            SourceTaskId = sourceTaskId ?? string.Empty;
            SourceObjectId = sourceObjectId ?? string.Empty;
            SourceDigest = sourceDigest ?? string.Empty;
            SemanticValue = semanticValue ?? string.Empty;
            Required = required;
            AllowSuppression = allowSuppression;
            NoWrite = noWrite;
            MarkerOnly = markerOnly;
            State = state;
        }

        public string ClaimId { get; }
        public SectorCoord SectorCoordinate { get; }
        public int SectorIndex { get; }
        public LocalTileCoord Coordinate { get; }
        public SectorCanvasOwnershipPlane Plane { get; }
        public SectorCanvasOwnerKind OwnerKind { get; }
        public SectorCanvasOwnershipPriority Priority { get; }
        public string SourceTaskId { get; }
        public string SourceObjectId { get; }
        public string SourceDigest { get; }
        public string SemanticValue { get; }
        public bool Required { get; }
        public bool AllowSuppression { get; }
        public bool NoWrite { get; }
        public bool MarkerOnly { get; }
        public SectorCanvasClaimState State { get; }

        internal SectorCanvasOwnershipClaim WithState(SectorCanvasClaimState state) =>
            new SectorCanvasOwnershipClaim(
                ClaimId, SectorCoordinate, SectorIndex, Coordinate, Plane, OwnerKind,
                Priority, SourceTaskId, SourceObjectId, SourceDigest, SemanticValue,
                Required, AllowSuppression, NoWrite, MarkerOnly, state);

        public int CompareTo(SectorCanvasOwnershipClaim other)
        {
            if (other == null) return -1;
            var comparison = SectorIndex.CompareTo(other.SectorIndex);
            if (comparison != 0) return comparison;
            comparison = Coordinate.Y.CompareTo(other.Coordinate.Y);
            if (comparison != 0) return comparison;
            comparison = Coordinate.X.CompareTo(other.Coordinate.X);
            if (comparison != 0) return comparison;
            comparison = Plane.CompareTo(other.Plane);
            if (comparison != 0) return comparison;
            comparison = Priority.CompareTo(other.Priority);
            if (comparison != 0) return comparison;
            comparison = OwnerKind.CompareTo(other.OwnerKind);
            return comparison != 0
                ? comparison
                : string.Compare(ClaimId, other.ClaimId, StringComparison.Ordinal);
        }
    }

    public sealed class SectorCanvasOwnedCell : IComparable<SectorCanvasOwnedCell>
    {
        internal SectorCanvasOwnedCell(SectorCanvasOwnershipClaim winner)
        {
            SectorCoordinate = winner.SectorCoordinate;
            SectorIndex = winner.SectorIndex;
            Coordinate = winner.Coordinate;
            Plane = winner.Plane;
            OwnerKind = winner.OwnerKind;
            WinnerClaimId = winner.ClaimId;
            SourceObjectId = winner.SourceObjectId;
            SemanticValue = winner.SemanticValue;
        }

        public SectorCoord SectorCoordinate { get; }
        public int SectorIndex { get; }
        public LocalTileCoord Coordinate { get; }
        public SectorCanvasOwnershipPlane Plane { get; }
        public SectorCanvasOwnerKind OwnerKind { get; }
        public string WinnerClaimId { get; }
        public string SourceObjectId { get; }
        public string SemanticValue { get; }

        public int CompareTo(SectorCanvasOwnedCell other)
        {
            if (other == null) return -1;
            var comparison = SectorIndex.CompareTo(other.SectorIndex);
            if (comparison != 0) return comparison;
            comparison = Coordinate.Y.CompareTo(other.Coordinate.Y);
            if (comparison != 0) return comparison;
            comparison = Coordinate.X.CompareTo(other.Coordinate.X);
            return comparison != 0 ? comparison : Plane.CompareTo(other.Plane);
        }
    }

    public sealed class SectorCanvasSuppressedClaim : IComparable<SectorCanvasSuppressedClaim>
    {
        internal SectorCanvasSuppressedClaim(
            SectorCanvasOwnershipClaim winner,
            SectorCanvasOwnershipClaim suppressed,
            string reason)
        {
            SectorCoordinate = suppressed.SectorCoordinate;
            SectorIndex = suppressed.SectorIndex;
            Coordinate = suppressed.Coordinate;
            Plane = suppressed.Plane;
            WinnerClaimId = winner.ClaimId;
            SuppressedClaimId = suppressed.ClaimId;
            WinnerOwnerKind = winner.OwnerKind;
            SuppressedOwnerKind = suppressed.OwnerKind;
            WinnerPriority = winner.Priority;
            SuppressedPriority = suppressed.Priority;
            Reason = reason ?? string.Empty;
            State = SectorCanvasClaimState.SuppressedByPriority;
        }

        public SectorCoord SectorCoordinate { get; }
        public int SectorIndex { get; }
        public LocalTileCoord Coordinate { get; }
        public SectorCanvasOwnershipPlane Plane { get; }
        public string WinnerClaimId { get; }
        public string SuppressedClaimId { get; }
        public SectorCanvasOwnerKind WinnerOwnerKind { get; }
        public SectorCanvasOwnerKind SuppressedOwnerKind { get; }
        public SectorCanvasOwnershipPriority WinnerPriority { get; }
        public SectorCanvasOwnershipPriority SuppressedPriority { get; }
        public string Reason { get; }
        public SectorCanvasClaimState State { get; }

        public int CompareTo(SectorCanvasSuppressedClaim other)
        {
            if (other == null) return -1;
            var comparison = SectorIndex.CompareTo(other.SectorIndex);
            if (comparison != 0) return comparison;
            comparison = Coordinate.Y.CompareTo(other.Coordinate.Y);
            if (comparison != 0) return comparison;
            comparison = Coordinate.X.CompareTo(other.Coordinate.X);
            if (comparison != 0) return comparison;
            comparison = Plane.CompareTo(other.Plane);
            return comparison != 0
                ? comparison
                : string.Compare(SuppressedClaimId, other.SuppressedClaimId, StringComparison.Ordinal);
        }
    }

    public sealed class SectorCanvasConflict : IComparable<SectorCanvasConflict>
    {
        internal SectorCanvasConflict(
            SectorCanvasOwnershipErrorCode code,
            SectorCoord sectorCoordinate,
            int sectorIndex,
            LocalTileCoord coordinate,
            SectorCanvasOwnershipPlane plane,
            IEnumerable<string> claimIds,
            string detail)
        {
            Code = code;
            SectorCoordinate = sectorCoordinate;
            SectorIndex = sectorIndex;
            Coordinate = coordinate;
            Plane = plane;
            ClaimIds = new ReadOnlyCollection<string>((claimIds ?? Array.Empty<string>())
                .Select(value => value ?? string.Empty).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
            Detail = detail ?? string.Empty;
        }

        public SectorCanvasOwnershipErrorCode Code { get; }
        public SectorCoord SectorCoordinate { get; }
        public int SectorIndex { get; }
        public LocalTileCoord Coordinate { get; }
        public SectorCanvasOwnershipPlane Plane { get; }
        public IReadOnlyList<string> ClaimIds { get; }
        public string Detail { get; }

        public int CompareTo(SectorCanvasConflict other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = SectorIndex.CompareTo(other.SectorIndex);
            if (comparison != 0) return comparison;
            comparison = Coordinate.Y.CompareTo(other.Coordinate.Y);
            if (comparison != 0) return comparison;
            comparison = Coordinate.X.CompareTo(other.Coordinate.X);
            if (comparison != 0) return comparison;
            comparison = Plane.CompareTo(other.Plane);
            return comparison != 0
                ? comparison
                : string.Compare(string.Join(";", ClaimIds), string.Join(";", other.ClaimIds), StringComparison.Ordinal);
        }
    }

    public sealed class SectorCanvasOwnershipBuildRequest
    {
        private readonly ReadOnlyCollection<SectorPacingAssignment> assignments;
        private readonly ReadOnlyCollection<SectorCanvasOwnershipClaim> additionalClaims;
        private readonly ReadOnlyCollection<SectorCanvasOwnershipErrorCode> referenceFaults;

        public SectorCanvasOwnershipBuildRequest(
            SectorPlannerInput input,
            IEnumerable<SectorPacingAssignment> sourceAssignments,
            SectorFixedAnchorPlan fixedAnchorPlan,
            SectorClusterPlacementPlan clusterPlacementPlan,
            SectorSpineEnvelopePlan spineEnvelopePlan,
            SectorClusterRolePatternPlan rolePatternPlan,
            SectorPatternRenderPlan patternRenderPlan,
            SectorQuietActivityEventPlan quietActivityEventPlan,
            string publicationLabel,
            string expectedClaimDigest = "",
            string expectedPlanDigest = "",
            IEnumerable<SectorCanvasOwnershipClaim> sourceAdditionalClaims = null,
            IEnumerable<SectorCanvasOwnershipErrorCode> sourceReferenceFaults = null,
            bool specialPersistenceMutationClaim = false,
            bool boundaryMutationClaim = false,
            bool spineEnvelopeMutationClaim = false,
            bool clusterMutationClaim = false,
            bool patternRenderMutationClaim = false,
            bool quietMutationClaim = false,
            bool activityMarkerMutationClaim = false,
            bool eventMarkerMutationClaim = false,
            int retryCount = 0,
            int map14RngDrawCount = 0,
            int solverInvocationCount = 0,
            int patternClusterReselectionCount = 0,
            int tilemapWriteCount = 0,
            int sceneMutationCount = 0,
            int prefabMutationCount = 0,
            int gameObjectMutationCount = 0,
            int activityRuntimeSpawnCount = 0,
            int eventRuntimeSpawnCount = 0,
            int gameplayExecutionCount = 0)
        {
            Input = input;
            assignments = new ReadOnlyCollection<SectorPacingAssignment>(
                (sourceAssignments ?? Array.Empty<SectorPacingAssignment>()).Where(value => value != null)
                .OrderBy(value => value.Coordinate.Y).ThenBy(value => value.Coordinate.X).ToArray());
            FixedAnchorPlan = fixedAnchorPlan;
            ClusterPlacementPlan = clusterPlacementPlan;
            SpineEnvelopePlan = spineEnvelopePlan;
            RolePatternPlan = rolePatternPlan;
            PatternRenderPlan = patternRenderPlan;
            QuietActivityEventPlan = quietActivityEventPlan;
            PublicationLabel = publicationLabel ?? string.Empty;
            ExpectedClaimDigest = expectedClaimDigest ?? string.Empty;
            ExpectedPlanDigest = expectedPlanDigest ?? string.Empty;
            additionalClaims = new ReadOnlyCollection<SectorCanvasOwnershipClaim>(
                (sourceAdditionalClaims ?? Array.Empty<SectorCanvasOwnershipClaim>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
            referenceFaults = new ReadOnlyCollection<SectorCanvasOwnershipErrorCode>(
                (sourceReferenceFaults ?? Array.Empty<SectorCanvasOwnershipErrorCode>()).Distinct().OrderBy(value => value).ToArray());
            SpecialPersistenceMutationClaim = specialPersistenceMutationClaim;
            BoundaryMutationClaim = boundaryMutationClaim;
            SpineEnvelopeMutationClaim = spineEnvelopeMutationClaim;
            ClusterMutationClaim = clusterMutationClaim;
            PatternRenderMutationClaim = patternRenderMutationClaim;
            QuietMutationClaim = quietMutationClaim;
            ActivityMarkerMutationClaim = activityMarkerMutationClaim;
            EventMarkerMutationClaim = eventMarkerMutationClaim;
            RetryCount = retryCount;
            Map14RngDrawCount = map14RngDrawCount;
            SolverInvocationCount = solverInvocationCount;
            PatternClusterReselectionCount = patternClusterReselectionCount;
            TilemapWriteCount = tilemapWriteCount;
            SceneMutationCount = sceneMutationCount;
            PrefabMutationCount = prefabMutationCount;
            GameObjectMutationCount = gameObjectMutationCount;
            ActivityRuntimeSpawnCount = activityRuntimeSpawnCount;
            EventRuntimeSpawnCount = eventRuntimeSpawnCount;
            GameplayExecutionCount = gameplayExecutionCount;
        }

        public SectorPlannerInput Input { get; }
        public IReadOnlyList<SectorPacingAssignment> Assignments => assignments;
        public SectorFixedAnchorPlan FixedAnchorPlan { get; }
        public SectorClusterPlacementPlan ClusterPlacementPlan { get; }
        public SectorSpineEnvelopePlan SpineEnvelopePlan { get; }
        public SectorClusterRolePatternPlan RolePatternPlan { get; }
        public SectorPatternRenderPlan PatternRenderPlan { get; }
        public SectorQuietActivityEventPlan QuietActivityEventPlan { get; }
        public string PublicationLabel { get; }
        public string ExpectedClaimDigest { get; }
        public string ExpectedPlanDigest { get; }
        public IReadOnlyList<SectorCanvasOwnershipClaim> AdditionalClaims => additionalClaims;
        public IReadOnlyList<SectorCanvasOwnershipErrorCode> ReferenceFaults => referenceFaults;
        public bool SpecialPersistenceMutationClaim { get; }
        public bool BoundaryMutationClaim { get; }
        public bool SpineEnvelopeMutationClaim { get; }
        public bool ClusterMutationClaim { get; }
        public bool PatternRenderMutationClaim { get; }
        public bool QuietMutationClaim { get; }
        public bool ActivityMarkerMutationClaim { get; }
        public bool EventMarkerMutationClaim { get; }
        public int RetryCount { get; }
        public int Map14RngDrawCount { get; }
        public int SolverInvocationCount { get; }
        public int PatternClusterReselectionCount { get; }
        public int TilemapWriteCount { get; }
        public int SceneMutationCount { get; }
        public int PrefabMutationCount { get; }
        public int GameObjectMutationCount { get; }
        public int ActivityRuntimeSpawnCount { get; }
        public int EventRuntimeSpawnCount { get; }
        public int GameplayExecutionCount { get; }
    }

    public sealed class SectorCanvasOwnershipPlan
    {
        private readonly ReadOnlyCollection<SectorCanvasOwnershipClaim> claims;
        private readonly ReadOnlyCollection<SectorCanvasOwnershipClaim> winnerClaims;
        private readonly ReadOnlyCollection<SectorCanvasOwnershipClaim> evidenceClaims;
        private readonly ReadOnlyCollection<SectorCanvasOwnedCell> ownedCells;
        private readonly ReadOnlyCollection<SectorCanvasSuppressedClaim> suppressedClaims;
        private readonly ReadOnlyCollection<SectorCanvasConflict> conflicts;
        private readonly ReadOnlyDictionary<SectorCanvasOwnerKind, int> claimCountByOwnerKind;
        private readonly ReadOnlyDictionary<SectorCanvasOwnerKind, int> winnerCountByOwnerKind;
        private readonly ReadOnlyDictionary<SectorCanvasOwnerKind, int> suppressedCountByOwnerKind;
        private readonly ReadOnlyDictionary<SectorCanvasOwnershipPlane, int> ownedCellCountByPlane;
        private readonly ReadOnlyDictionary<SectorCanvasOwnershipErrorCode, int> conflictCountByType;

        internal SectorCanvasOwnershipPlan(
            SectorCanvasOwnershipBuildRequest request,
            IEnumerable<SectorCanvasOwnershipClaim> sourceClaims,
            IEnumerable<SectorCanvasOwnershipClaim> sourceWinners,
            IEnumerable<SectorCanvasOwnershipClaim> sourceEvidence,
            IEnumerable<SectorCanvasOwnedCell> sourceOwnedCells,
            IEnumerable<SectorCanvasSuppressedClaim> sourceSuppressed,
            IEnumerable<SectorCanvasConflict> sourceConflicts,
            int allowedCrossPlaneCoexistenceCount,
            int explicitNoTerrainEvidenceCoordinateCount,
            int coverageCount,
            string claimDigest,
            string canonicalDigest)
        {
            Request = request;
            claims = Copy(sourceClaims);
            winnerClaims = Copy(sourceWinners);
            evidenceClaims = Copy(sourceEvidence);
            ownedCells = Copy(sourceOwnedCells);
            suppressedClaims = Copy(sourceSuppressed);
            conflicts = Copy(sourceConflicts);
            AllowedCrossPlaneCoexistenceCount = allowedCrossPlaneCoexistenceCount;
            ExplicitNoTerrainEvidenceCoordinateCount = explicitNoTerrainEvidenceCoordinateCount;
            CoverageCount = coverageCount;
            ClaimDigest = claimDigest ?? string.Empty;
            CanonicalDigest = canonicalDigest ?? string.Empty;
            claimCountByOwnerKind = CountAll(claims, value => value.OwnerKind,
                Enum.GetValues(typeof(SectorCanvasOwnerKind)).Cast<SectorCanvasOwnerKind>());
            winnerCountByOwnerKind = CountAll(winnerClaims, value => value.OwnerKind,
                Enum.GetValues(typeof(SectorCanvasOwnerKind)).Cast<SectorCanvasOwnerKind>());
            suppressedCountByOwnerKind = CountAll(suppressedClaims, value => value.SuppressedOwnerKind,
                Enum.GetValues(typeof(SectorCanvasOwnerKind)).Cast<SectorCanvasOwnerKind>());
            ownedCellCountByPlane = CountAll(ownedCells, value => value.Plane,
                Enum.GetValues(typeof(SectorCanvasOwnershipPlane)).Cast<SectorCanvasOwnershipPlane>());
            conflictCountByType = CountAll(conflicts, value => value.Code,
                Enum.GetValues(typeof(SectorCanvasOwnershipErrorCode)).Cast<SectorCanvasOwnershipErrorCode>());
        }

        public SectorCanvasOwnershipBuildRequest Request { get; }
        public IReadOnlyList<SectorCanvasOwnershipClaim> Claims => claims;
        public IReadOnlyList<SectorCanvasOwnershipClaim> WinnerClaims => winnerClaims;
        public IReadOnlyList<SectorCanvasOwnershipClaim> EvidenceClaims => evidenceClaims;
        public IReadOnlyList<SectorCanvasOwnedCell> OwnedCells => ownedCells;
        public IReadOnlyList<SectorCanvasSuppressedClaim> SuppressedClaims => suppressedClaims;
        public IReadOnlyList<SectorCanvasConflict> Conflicts => conflicts;
        public IReadOnlyDictionary<SectorCanvasOwnerKind, int> ClaimCountByOwnerKind => claimCountByOwnerKind;
        public IReadOnlyDictionary<SectorCanvasOwnerKind, int> WinnerCountByOwnerKind => winnerCountByOwnerKind;
        public IReadOnlyDictionary<SectorCanvasOwnerKind, int> SuppressedCountByOwnerKind => suppressedCountByOwnerKind;
        public IReadOnlyDictionary<SectorCanvasOwnershipPlane, int> OwnedCellCountByPlane => ownedCellCountByPlane;
        public IReadOnlyDictionary<SectorCanvasOwnershipErrorCode, int> ConflictCountByType => conflictCountByType;
        public int SectorCount => Request.Input.Sectors.Count;
        public int ClaimCount => claims.Count;
        public int WinnerClaimCount => winnerClaims.Count;
        public int EvidenceClaimCount => evidenceClaims.Count;
        public int OwnedCellCount => ownedCells.Count;
        public int SuppressedClaimCount => suppressedClaims.Count;
        public int AllowedCrossPlaneCoexistenceCount { get; }
        public int ExplicitNoTerrainEvidenceCoordinateCount { get; }
        public int EmptyEvidenceOnlyCoordinateCount => ExplicitNoTerrainEvidenceCoordinateCount;
        public int ConflictCount => conflicts.Count;
        public int SamePlaneDoubleOwnerCount => conflicts.Count(value => value.Code == SectorCanvasOwnershipErrorCode.DoubleOwnerConflict);
        public int ForbiddenOverlapCount => conflicts.Count(value => value.Code == SectorCanvasOwnershipErrorCode.ForbiddenOverlap);
        public int UnresolvedConflictCount => conflicts.Count;
        public int CoverageCount { get; }
        public int ExpectedCoverageCount => SectorCount * 48 * 32;
        public string ClaimDigest { get; }
        public string CanonicalDigest { get; }
        public bool Map14_08HandoffReady => CoverageCount == ExpectedCoverageCount &&
                                             SamePlaneDoubleOwnerCount == 0 &&
                                             ForbiddenOverlapCount == 0 &&
                                             UnresolvedConflictCount == 0;

        public string PlannerInputDigestBefore => Request.Input.CanonicalDigest;
        public string PlannerInputDigestAfter => Request.Input.CanonicalDigest;
        public string PacingAssignmentDigestBefore => Request.RolePatternPlan.PacingAssignmentDigestBefore;
        public string PacingAssignmentDigestAfter => Request.RolePatternPlan.PacingAssignmentDigestAfter;
        public string FixedAnchorPlanDigestBefore => Request.FixedAnchorPlan.CanonicalDigest;
        public string FixedAnchorPlanDigestAfter => Request.FixedAnchorPlan.CanonicalDigest;
        public string ClusterPlacementPlanDigestBefore => Request.ClusterPlacementPlan.CanonicalDigest;
        public string ClusterPlacementPlanDigestAfter => Request.ClusterPlacementPlan.CanonicalDigest;
        public string SpineEnvelopePlanDigestBefore => Request.SpineEnvelopePlan.CanonicalDigest;
        public string SpineEnvelopePlanDigestAfter => Request.SpineEnvelopePlan.CanonicalDigest;
        public string RolePatternPlanDigestBefore => Request.RolePatternPlan.CanonicalDigest;
        public string RolePatternPlanDigestAfter => Request.RolePatternPlan.CanonicalDigest;
        public string PatternRenderPlanDigestBefore => Request.PatternRenderPlan.CanonicalDigest;
        public string PatternRenderPlanDigestAfter => Request.PatternRenderPlan.CanonicalDigest;
        public string QuietActivityEventPlanDigestBefore => Request.QuietActivityEventPlan.CanonicalDigest;
        public string QuietActivityEventPlanDigestAfter => Request.QuietActivityEventPlan.CanonicalDigest;
        public string ActivityAuthorityDigestBefore => Request.QuietActivityEventPlan.ActivityFrequencyPlanDigestBefore;
        public string ActivityAuthorityDigestAfter => Request.QuietActivityEventPlan.ActivityFrequencyPlanDigestAfter;
        public string EventAuthorityDigestBefore => Request.QuietActivityEventPlan.EventAssignmentPlanDigestBefore;
        public string EventAuthorityDigestAfter => Request.QuietActivityEventPlan.EventAssignmentPlanDigestAfter;
        public string RouteAccessIdentityBefore => Request.RolePatternPlan.RouteAccessIdentityBefore;
        public string RouteAccessIdentityAfter => Request.RolePatternPlan.RouteAccessIdentityAfter;
        public string ExternalSocketIdentityBefore => Request.RolePatternPlan.ExternalSocketIdentityBefore;
        public string ExternalSocketIdentityAfter => Request.RolePatternPlan.ExternalSocketIdentityAfter;
        public string BoundaryIdentityBefore => Request.RolePatternPlan.BoundaryIdentityBefore;
        public string BoundaryIdentityAfter => Request.RolePatternPlan.BoundaryIdentityAfter;
        public string SpecialIdentityBefore => Request.RolePatternPlan.SpecialIdentityBefore;
        public string SpecialIdentityAfter => Request.RolePatternPlan.SpecialIdentityAfter;
        public string ClusterIdentityBefore => Request.RolePatternPlan.ClusterIdentityBefore;
        public string ClusterIdentityAfter => Request.RolePatternPlan.ClusterIdentityAfter;
        public string ProtectedOpenIdentityBefore => Request.RolePatternPlan.ProtectedOpenIdentityBefore;
        public string ProtectedOpenIdentityAfter => Request.RolePatternPlan.ProtectedOpenIdentityAfter;
        public int FinalReferenceOwnershipWriteCount => ownedCells.Count;
        public int RetryCount => 0;
        public int Map14RngDrawCount => 0;
        public int SolverInvocationCount => 0;
        public int PatternClusterReselectionCount => 0;
        public int TilemapWriteCount => 0;
        public int SceneMutationCount => 0;
        public int PrefabMutationCount => 0;
        public int GameObjectMutationCount => 0;
        public int ActivityRuntimeSpawnCount => 0;
        public int EventRuntimeSpawnCount => 0;
        public int SpecialPersistenceMutationCount => 0;
        public int RewardExecutionCount => 0;
        public int CombatExecutionCount => 0;
        public int CraftingExecutionCount => 0;
        public int InventoryExecutionCount => 0;
        public int NpcExecutionCount => 0;

        public int CountClaims(SectorCanvasOwnerKind kind) => claimCountByOwnerKind[kind];
        public int CountWinners(SectorCanvasOwnerKind kind) => winnerCountByOwnerKind[kind];
        public int CountSuppressed(SectorCanvasOwnerKind kind) => suppressedCountByOwnerKind[kind];
        public int CountOwned(SectorCanvasOwnershipPlane plane) => ownedCellCountByPlane[plane];

        private static ReadOnlyCollection<T> Copy<T>(IEnumerable<T> source) where T : IComparable<T> =>
            new ReadOnlyCollection<T>((source ?? Array.Empty<T>()).OrderBy(value => value).ToArray());

        private static ReadOnlyDictionary<TKey, int> CountAll<TValue, TKey>(
            IEnumerable<TValue> values,
            Func<TValue, TKey> selector,
            IEnumerable<TKey> keys)
        {
            var result = new SortedDictionary<TKey, int>();
            foreach (var key in keys) result[key] = 0;
            foreach (var value in values) result[selector(value)]++;
            return new ReadOnlyDictionary<TKey, int>(result);
        }
    }

    public sealed class SectorCanvasOwnershipBuildResult
    {
        private readonly ReadOnlyCollection<SectorCanvasOwnershipClaim> claims;
        private readonly ReadOnlyCollection<SectorCanvasOwnershipError> errors;

        internal SectorCanvasOwnershipBuildResult(
            SectorCanvasOwnershipBuildRequest request,
            IEnumerable<SectorCanvasOwnershipClaim> sourceClaims,
            SectorCanvasOwnershipPlan plan,
            string canonicalDigest,
            IEnumerable<SectorCanvasOwnershipError> sourceErrors)
        {
            Request = request;
            claims = new ReadOnlyCollection<SectorCanvasOwnershipClaim>(
                (sourceClaims ?? Array.Empty<SectorCanvasOwnershipClaim>()).OrderBy(value => value).ToArray());
            errors = new ReadOnlyCollection<SectorCanvasOwnershipError>(
                (sourceErrors ?? Array.Empty<SectorCanvasOwnershipError>()).Where(value => value != null)
                .Distinct().OrderBy(value => value).ToArray());
            Plan = errors.Count == 0 ? plan : null;
            CanonicalDigest = errors.Count == 0 ? canonicalDigest ?? string.Empty : string.Empty;
        }

        public bool Success => errors.Count == 0 && (ClaimsReady || Plan != null);
        public bool ClaimsReady => Plan == null && claims.Count > 0 && errors.Count == 0;
        public bool Resolved => Plan != null && errors.Count == 0;
        public SectorCanvasOwnershipBuildRequest Request { get; }
        public IReadOnlyList<SectorCanvasOwnershipClaim> Claims => claims;
        public SectorCanvasOwnershipPlan Plan { get; }
        public string CanonicalDigest { get; }
        public IReadOnlyList<SectorCanvasOwnershipError> Errors => errors;
    }

    public static class SectorCanvasOwnershipCanonicalDigest
    {
        public static string ComputeClaims(
            SectorCanvasOwnershipBuildRequest request,
            IEnumerable<SectorCanvasOwnershipClaim> claims)
        {
            if (request == null) return string.Empty;
            var material = new StringBuilder();
            Append(material, "RULESET", "MAP14_07_CLAIMS_V1", request.PublicationLabel,
                request.Input == null ? string.Empty : request.Input.CanonicalDigest,
                request.FixedAnchorPlan == null ? string.Empty : request.FixedAnchorPlan.CanonicalDigest,
                request.ClusterPlacementPlan == null ? string.Empty : request.ClusterPlacementPlan.CanonicalDigest,
                request.SpineEnvelopePlan == null ? string.Empty : request.SpineEnvelopePlan.CanonicalDigest,
                request.RolePatternPlan == null ? string.Empty : request.RolePatternPlan.CanonicalDigest,
                request.PatternRenderPlan == null ? string.Empty : request.PatternRenderPlan.CanonicalDigest,
                request.QuietActivityEventPlan == null ? string.Empty : request.QuietActivityEventPlan.CanonicalDigest);
            foreach (var claim in (claims ?? Array.Empty<SectorCanvasOwnershipClaim>()).OrderBy(value => value))
                Append(material, "CLAIM", claim.ClaimId, Number(claim.SectorIndex),
                    Number(claim.Coordinate.X), Number(claim.Coordinate.Y), Number((int)claim.Plane),
                    Number((int)claim.OwnerKind), Number((int)claim.Priority), claim.SourceTaskId,
                    claim.SourceObjectId, claim.SourceDigest, claim.SemanticValue,
                    Flag(claim.Required), Flag(claim.AllowSuppression), Flag(claim.NoWrite),
                    Flag(claim.MarkerOnly), Number((int)claim.State));
            return Hash(material.ToString());
        }

        public static string ComputePlan(SectorCanvasOwnershipPlan plan)
        {
            if (plan == null) return string.Empty;
            var material = new StringBuilder();
            Append(material, "RULESET", "MAP14_07_PLAN_V1", plan.ClaimDigest,
                Number(plan.SectorCount), Number(plan.CoverageCount),
                Number(plan.AllowedCrossPlaneCoexistenceCount),
                Number(plan.ExplicitNoTerrainEvidenceCoordinateCount));
            foreach (var winner in plan.WinnerClaims)
                Append(material, "WINNER", winner.ClaimId, Number((int)winner.OwnerKind),
                    Number((int)winner.Plane), Number((int)winner.Priority));
            foreach (var evidence in plan.EvidenceClaims)
                Append(material, "EVIDENCE", evidence.ClaimId, Number((int)evidence.OwnerKind), evidence.SemanticValue);
            foreach (var suppressed in plan.SuppressedClaims)
                Append(material, "SUPPRESSED", suppressed.WinnerClaimId, suppressed.SuppressedClaimId,
                    Number((int)suppressed.WinnerPriority), Number((int)suppressed.SuppressedPriority), suppressed.Reason);
            foreach (var owned in plan.OwnedCells)
                Append(material, "OWNED", Number(owned.SectorIndex), Number(owned.Coordinate.X),
                    Number(owned.Coordinate.Y), Number((int)owned.Plane), Number((int)owned.OwnerKind), owned.WinnerClaimId);
            return Hash(material.ToString());
        }

        private static void Append(StringBuilder builder, params string[] fields)
        {
            builder.Append(string.Join("|", fields.Select(value => value ?? string.Empty))).Append('\n');
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Flag(bool value) => value ? "1" : "0";

        private static string Hash(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                var result = new StringBuilder(bytes.Length * 2);
                foreach (var item in bytes)
                    result.Append(item.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }
    }
}
