using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Activities;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.EventOverlays;
using StarNight.Map.WorldGeneration.Generation;
using static StarNight.Map.WorldGeneration.SectorPlanning.SectorQuietActivityEventImports;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum SectorQuietFillCellKind
    {
        QuietBuffer = 1,
        QuietAir = 2,
        QuietSolid = 3,
        RouteMargin = 4,
        BoundaryMargin = 5,
        SpecialMargin = 6,
        ActivityCandidate = 7,
        EventCandidate = 8,
        ProtectedNoWrite = 9,
        ReservedNoWrite = 10,
        AlreadyPatternRendered = 11,
    }

    public enum SectorQuietFillSourceKind
    {
        ReferencePatternCanvas = 1,
        ProtectedOpen = 2,
        RouteEnvelope = 3,
        BoundaryAnchor = 4,
        SpecialAnchor = 5,
        SpecialFixedShell = 6,
        VillageReference = 7,
        ClusterFootprint = 8,
        ClusterPatternZone = 9,
        ActivityCompatibility = 10,
        EventMarkerOpportunity = 11,
        ManualReferenceFixture = 12,
    }

    public enum SectorActivityEventMarkerKind
    {
        ActivityCue = 1,
        ActivityCore = 2,
        ActivityReward = 3,
        ActivityRecovery = 4,
        EventTerrain = 5,
        EventActivity = 6,
        EventSpecial = 7,
        EventEmpty = 8,
    }

    public enum SectorActivityEventPlacementState
    {
        Eligible = 1,
        Selected = 2,
        Rejected = 3,
        Assigned = 4,
        ExplicitEmpty = 5,
    }

    public enum SectorQuietActivityEventErrorCode
    {
        MissingInput,
        MissingPatternRenderPlan,
        MissingSpineEnvelopePlan,
        MissingActivityAuthority,
        MissingEventAuthority,
        SectorMismatch,
        QuietCellOutOfBounds,
        DuplicateQuietCell,
        QuietCellTouchesProtectedOpen,
        QuietCellTouchesFinalOwner,
        PatternCanvasMutationClaim,
        ActivityOpportunityOutOfBounds,
        ActivityOpportunityOverlapsProtected,
        ActivityFrequencyRejected,
        ActivityStrongCapViolation,
        ActivityRemovalSafetyMissing,
        ActivityMarkerMutationClaim,
        EventOpportunityOutOfBounds,
        EventOpportunityOverlapsProtected,
        EventAssignmentRejected,
        EventCooldownViolation,
        MissingEmptyEvent,
        EventMarkerMutationClaim,
        SpecialPersistenceMutationClaim,
        AnchorMutationClaim,
        ClusterMutationClaim,
        SpineEnvelopeMutationClaim,
        OwnershipMutationClaim,
        SolverMutationClaim,
        RngMutationClaim,
        TileMutationClaim,
        NonCanonicalPublication,
    }

    public sealed class SectorQuietActivityEventError :
        IComparable<SectorQuietActivityEventError>, IEquatable<SectorQuietActivityEventError>
    {
        public SectorQuietActivityEventError(
            SectorQuietActivityEventErrorCode code,
            string subject,
            string detail)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public SectorQuietActivityEventErrorCode Code { get; }
        public string Subject { get; }
        public string Detail { get; }

        public int CompareTo(SectorQuietActivityEventError other)
        {
            if (ReferenceEquals(other, null)) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0 ? comparison : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(SectorQuietActivityEventError other) => other != null && CompareTo(other) == 0;
        public override bool Equals(object obj) => Equals(obj as SectorQuietActivityEventError);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(ToString());
        public override string ToString() => Code + "|" + Subject + "|" + Detail;
    }

    public sealed class SectorQuietFillCell : IComparable<SectorQuietFillCell>
    {
        public SectorQuietFillCell(
            SectorCoord sectorCoordinate,
            int sectorIndex,
            LocalTileCoord coordinate,
            SectorQuietFillCellKind kind,
            SectorQuietFillSourceKind sourceKind,
            string sourceIdentity,
            bool protectedNoWrite,
            bool reservedNoWrite,
            bool patternRendered,
            bool activityEligible,
            bool eventEligible)
        {
            SectorCoordinate = sectorCoordinate;
            SectorIndex = sectorIndex;
            Coordinate = coordinate;
            Kind = kind;
            SourceKind = sourceKind;
            SourceIdentity = sourceIdentity ?? string.Empty;
            ProtectedNoWrite = protectedNoWrite;
            ReservedNoWrite = reservedNoWrite;
            PatternRendered = patternRendered;
            ActivityEligible = activityEligible;
            EventEligible = eventEligible;
        }

        public SectorCoord SectorCoordinate { get; }
        public int SectorIndex { get; }
        public LocalTileCoord Coordinate { get; }
        public SectorQuietFillCellKind Kind { get; }
        public SectorQuietFillSourceKind SourceKind { get; }
        public string SourceIdentity { get; }
        public bool ProtectedNoWrite { get; }
        public bool ReservedNoWrite { get; }
        public bool PatternRendered { get; }
        public bool ActivityEligible { get; }
        public bool EventEligible { get; }
        public bool IsQuietFill => Kind == SectorQuietFillCellKind.QuietBuffer ||
                                   Kind == SectorQuietFillCellKind.QuietAir ||
                                   Kind == SectorQuietFillCellKind.QuietSolid ||
                                   Kind == SectorQuietFillCellKind.ActivityCandidate ||
                                   Kind == SectorQuietFillCellKind.EventCandidate;
        public bool IsBuffer => Kind == SectorQuietFillCellKind.QuietBuffer ||
                                Kind == SectorQuietFillCellKind.RouteMargin ||
                                Kind == SectorQuietFillCellKind.BoundaryMargin ||
                                Kind == SectorQuietFillCellKind.SpecialMargin;

        public int CompareTo(SectorQuietFillCell other)
        {
            if (ReferenceEquals(other, null)) return -1;
            var comparison = SectorIndex.CompareTo(other.SectorIndex);
            if (comparison != 0) return comparison;
            comparison = Coordinate.Y.CompareTo(other.Coordinate.Y);
            return comparison != 0 ? comparison : Coordinate.X.CompareTo(other.Coordinate.X);
        }
    }

    public sealed class SectorActivityOpportunityProjection : IComparable<SectorActivityOpportunityProjection>
    {
        public SectorActivityOpportunityProjection(
            ActivityPlacementOpportunity authority,
            LocalTileCoord markerCoordinate,
            SectorActivityEventMarkerKind markerKind,
            string removalSafetyIdentity)
        {
            Authority = authority;
            MarkerCoordinate = markerCoordinate;
            MarkerKind = markerKind;
            RemovalSafetyIdentity = removalSafetyIdentity ?? string.Empty;
        }

        public ActivityPlacementOpportunity Authority { get; }
        public LocalTileCoord MarkerCoordinate { get; }
        public SectorActivityEventMarkerKind MarkerKind { get; }
        public string RemovalSafetyIdentity { get; }
        public string OpportunityId => Authority == null ? string.Empty : Authority.OpportunityId;
        public SectorCoord SectorCoordinate => Authority == null ? default(SectorCoord) : Authority.Sector;

        public int CompareTo(SectorActivityOpportunityProjection other)
        {
            if (ReferenceEquals(other, null)) return -1;
            return string.Compare(OpportunityId, other.OpportunityId, StringComparison.Ordinal);
        }
    }

    public sealed class SectorEventMarkerOpportunityProjection : IComparable<SectorEventMarkerOpportunityProjection>
    {
        public SectorEventMarkerOpportunityProjection(
            EventOverlayOpportunity authority,
            LocalTileCoord markerCoordinate,
            SectorActivityEventMarkerKind markerKind,
            string ownerIdentity)
        {
            Authority = authority;
            MarkerCoordinate = markerCoordinate;
            MarkerKind = markerKind;
            OwnerIdentity = ownerIdentity ?? string.Empty;
        }

        public EventOverlayOpportunity Authority { get; }
        public LocalTileCoord MarkerCoordinate { get; }
        public SectorActivityEventMarkerKind MarkerKind { get; }
        public string OwnerIdentity { get; }
        public string OpportunityId => Authority == null ? string.Empty : Authority.OpportunityId;
        public SectorCoord SectorCoordinate => Authority == null ? default(SectorCoord) : Authority.Sector;

        public int CompareTo(SectorEventMarkerOpportunityProjection other)
        {
            if (ReferenceEquals(other, null)) return -1;
            return string.Compare(OpportunityId, other.OpportunityId, StringComparison.Ordinal);
        }
    }

    public sealed class SectorActivityPlacementDecision : IComparable<SectorActivityPlacementDecision>
    {
        public SectorActivityPlacementDecision(
            SectorActivityOpportunityProjection opportunity,
            SectorActivityEventPlacementState state,
            ActivityStructureId activityId,
            ActivityStrengthClass strength,
            string reason,
            string candidateKey,
            int worldStrongBefore,
            int worldStrongAfter,
            int patchStrongBefore,
            int patchStrongAfter,
            int sectorStrongBefore,
            int sectorStrongAfter)
        {
            Opportunity = opportunity;
            State = state;
            ActivityId = activityId;
            Strength = strength;
            Reason = reason ?? string.Empty;
            CandidateKey = candidateKey ?? string.Empty;
            WorldStrongBefore = worldStrongBefore;
            WorldStrongAfter = worldStrongAfter;
            PatchStrongBefore = patchStrongBefore;
            PatchStrongAfter = patchStrongAfter;
            SectorStrongBefore = sectorStrongBefore;
            SectorStrongAfter = sectorStrongAfter;
        }

        public SectorActivityOpportunityProjection Opportunity { get; }
        public string OpportunityId => Opportunity.OpportunityId;
        public SectorActivityEventPlacementState State { get; }
        public ActivityStructureId ActivityId { get; }
        public ActivityStrengthClass Strength { get; }
        public string Reason { get; }
        public string CandidateKey { get; }
        public int WorldStrongBefore { get; }
        public int WorldStrongAfter { get; }
        public int PatchStrongBefore { get; }
        public int PatchStrongAfter { get; }
        public int SectorStrongBefore { get; }
        public int SectorStrongAfter { get; }

        public int CompareTo(SectorActivityPlacementDecision other)
        {
            if (ReferenceEquals(other, null)) return -1;
            return string.Compare(OpportunityId, other.OpportunityId, StringComparison.Ordinal);
        }
    }

    public sealed class SectorEventMarkerPlacementDecision : IComparable<SectorEventMarkerPlacementDecision>
    {
        private readonly ReadOnlyCollection<string> cooldownExclusionEvidence;

        public SectorEventMarkerPlacementDecision(
            SectorEventMarkerOpportunityProjection opportunity,
            SectorActivityEventPlacementState state,
            EventOverlayId eventId,
            EventOverlayKind eventKind,
            string candidateKey,
            int previousProgressionOrdinal,
            int currentProgressionOrdinal,
            int requiredProgressionGap,
            int actualProgressionGap,
            IEnumerable<string> cooldownExclusionEvidence)
        {
            Opportunity = opportunity;
            State = state;
            EventId = eventId;
            EventKind = eventKind;
            CandidateKey = candidateKey ?? string.Empty;
            PreviousProgressionOrdinal = previousProgressionOrdinal;
            CurrentProgressionOrdinal = currentProgressionOrdinal;
            RequiredProgressionGap = requiredProgressionGap;
            ActualProgressionGap = actualProgressionGap;
            this.cooldownExclusionEvidence = Copy(cooldownExclusionEvidence, StringComparer.Ordinal);
        }

        public SectorEventMarkerOpportunityProjection Opportunity { get; }
        public string OpportunityId => Opportunity.OpportunityId;
        public SectorActivityEventPlacementState State { get; }
        public EventOverlayId EventId { get; }
        public EventOverlayKind EventKind { get; }
        public string CandidateKey { get; }
        public int PreviousProgressionOrdinal { get; }
        public int CurrentProgressionOrdinal { get; }
        public int RequiredProgressionGap { get; }
        public int ActualProgressionGap { get; }
        public IReadOnlyList<string> CooldownExclusionEvidence => cooldownExclusionEvidence;

        public int CompareTo(SectorEventMarkerPlacementDecision other)
        {
            if (ReferenceEquals(other, null)) return -1;
            return string.Compare(OpportunityId, other.OpportunityId, StringComparison.Ordinal);
        }
    }

    public sealed class SectorQuietActivityEventBuildRequest
    {
        private readonly ReadOnlyCollection<SectorPacingAssignment> assignments;
        private readonly ReadOnlyCollection<SectorQuietActivityEventErrorCode> referenceFaults;

        public SectorQuietActivityEventBuildRequest(
            SectorPlannerInput input,
            IEnumerable<SectorPacingAssignment> assignments,
            SectorFixedAnchorPlan anchorPlan,
            SectorClusterPlacementPlan clusterPlacementPlan,
            SectorSpineEnvelopePlan spineEnvelopePlan,
            SectorClusterRolePatternPlan roleZonePlan,
            SectorPatternRenderPlan patternRenderPlan,
            string publicationLabel,
            string expectedCanonicalDigest = "",
            IEnumerable<SectorQuietActivityEventErrorCode> referenceFaults = null,
            bool patternCanvasMutationClaim = false,
            bool anchorMutationClaim = false,
            bool clusterMutationClaim = false,
            bool spineEnvelopeMutationClaim = false,
            bool ownershipMutationClaim = false,
            int solverInvocationCount = 0,
            int map14RngDrawCount = 0,
            int retryCount = 0,
            int tileWriteCount = 0)
        {
            Input = input;
            this.assignments = Copy(assignments, Comparer<SectorPacingAssignment>.Create(CompareAssignments));
            AnchorPlan = anchorPlan;
            ClusterPlacementPlan = clusterPlacementPlan;
            SpineEnvelopePlan = spineEnvelopePlan;
            RoleZonePlan = roleZonePlan;
            PatternRenderPlan = patternRenderPlan;
            PublicationLabel = publicationLabel ?? string.Empty;
            ExpectedCanonicalDigest = expectedCanonicalDigest ?? string.Empty;
            this.referenceFaults = Copy(referenceFaults, Comparer<SectorQuietActivityEventErrorCode>.Default);
            PatternCanvasMutationClaim = patternCanvasMutationClaim;
            AnchorMutationClaim = anchorMutationClaim;
            ClusterMutationClaim = clusterMutationClaim;
            SpineEnvelopeMutationClaim = spineEnvelopeMutationClaim;
            OwnershipMutationClaim = ownershipMutationClaim;
            SolverInvocationCount = solverInvocationCount;
            Map14RngDrawCount = map14RngDrawCount;
            RetryCount = retryCount;
            TileWriteCount = tileWriteCount;
        }

        public SectorPlannerInput Input { get; }
        public IReadOnlyList<SectorPacingAssignment> Assignments => assignments;
        public SectorFixedAnchorPlan AnchorPlan { get; }
        public SectorClusterPlacementPlan ClusterPlacementPlan { get; }
        public SectorSpineEnvelopePlan SpineEnvelopePlan { get; }
        public SectorClusterRolePatternPlan RoleZonePlan { get; }
        public SectorPatternRenderPlan PatternRenderPlan { get; }
        public string PublicationLabel { get; }
        public string ExpectedCanonicalDigest { get; }
        public IReadOnlyList<SectorQuietActivityEventErrorCode> ReferenceFaults => referenceFaults;
        public bool PatternCanvasMutationClaim { get; }
        public bool AnchorMutationClaim { get; }
        public bool ClusterMutationClaim { get; }
        public bool SpineEnvelopeMutationClaim { get; }
        public bool OwnershipMutationClaim { get; }
        public int SolverInvocationCount { get; }
        public int Map14RngDrawCount { get; }
        public int RetryCount { get; }
        public int TileWriteCount { get; }
    }

    public sealed class SectorActivityEventPlacementRequest
    {
        private readonly ReadOnlyCollection<SectorActivityOpportunityProjection> activityOpportunities;
        private readonly ReadOnlyCollection<SectorEventMarkerOpportunityProjection> eventOpportunities;
        private readonly ReadOnlyCollection<SectorQuietActivityEventErrorCode> referenceFaults;

        public SectorActivityEventPlacementRequest(
            SectorQuietFillPlan quietFillPlan,
            IEnumerable<SectorActivityOpportunityProjection> activityOpportunities,
            ActivityCandidateIndex activityCandidateIndex,
            ActivityFrequencyPlan activityFrequencyPlan,
            IEnumerable<SectorEventMarkerOpportunityProjection> eventOpportunities,
            EventOverlayCandidateIndex eventCandidateIndex,
            EventOverlayAssignmentPlan eventAssignmentPlan,
            string publicationLabel,
            string expectedCanonicalDigest = "",
            IEnumerable<SectorQuietActivityEventErrorCode> referenceFaults = null,
            bool activityMarkerMutationClaim = false,
            bool eventMarkerMutationClaim = false,
            bool specialPersistenceMutationClaim = false,
            bool ownershipMutationClaim = false,
            int solverInvocationCount = 0,
            int map14RngDrawCount = 0,
            int retryCount = 0,
            int tileWriteCount = 0)
        {
            QuietFillPlan = quietFillPlan;
            this.activityOpportunities = Copy(activityOpportunities, Comparer<SectorActivityOpportunityProjection>.Default);
            ActivityCandidateIndex = activityCandidateIndex;
            ActivityFrequencyPlan = activityFrequencyPlan;
            this.eventOpportunities = Copy(eventOpportunities, Comparer<SectorEventMarkerOpportunityProjection>.Default);
            EventCandidateIndex = eventCandidateIndex;
            EventAssignmentPlan = eventAssignmentPlan;
            PublicationLabel = publicationLabel ?? string.Empty;
            ExpectedCanonicalDigest = expectedCanonicalDigest ?? string.Empty;
            this.referenceFaults = Copy(referenceFaults, Comparer<SectorQuietActivityEventErrorCode>.Default);
            ActivityMarkerMutationClaim = activityMarkerMutationClaim;
            EventMarkerMutationClaim = eventMarkerMutationClaim;
            SpecialPersistenceMutationClaim = specialPersistenceMutationClaim;
            OwnershipMutationClaim = ownershipMutationClaim;
            SolverInvocationCount = solverInvocationCount;
            Map14RngDrawCount = map14RngDrawCount;
            RetryCount = retryCount;
            TileWriteCount = tileWriteCount;
        }

        public SectorQuietFillPlan QuietFillPlan { get; }
        public IReadOnlyList<SectorActivityOpportunityProjection> ActivityOpportunities => activityOpportunities;
        public ActivityCandidateIndex ActivityCandidateIndex { get; }
        public ActivityFrequencyPlan ActivityFrequencyPlan { get; }
        public IReadOnlyList<SectorEventMarkerOpportunityProjection> EventOpportunities => eventOpportunities;
        public EventOverlayCandidateIndex EventCandidateIndex { get; }
        public EventOverlayAssignmentPlan EventAssignmentPlan { get; }
        public string PublicationLabel { get; }
        public string ExpectedCanonicalDigest { get; }
        public IReadOnlyList<SectorQuietActivityEventErrorCode> ReferenceFaults => referenceFaults;
        public bool ActivityMarkerMutationClaim { get; }
        public bool EventMarkerMutationClaim { get; }
        public bool SpecialPersistenceMutationClaim { get; }
        public bool OwnershipMutationClaim { get; }
        public int SolverInvocationCount { get; }
        public int Map14RngDrawCount { get; }
        public int RetryCount { get; }
        public int TileWriteCount { get; }
    }

    public sealed class SectorQuietFillPlan
    {
        private readonly ReadOnlyCollection<SectorQuietFillCell> cells;
        private readonly ReadOnlyDictionary<SectorQuietFillCellKind, int> countByKind;

        internal SectorQuietFillPlan(
            SectorQuietActivityEventBuildRequest request,
            IEnumerable<SectorQuietFillCell> cells,
            int protectedCoordinateCount,
            int reservedCoordinateCount,
            int patternRenderedCoordinateCount,
            string canonicalDigest)
        {
            Request = request;
            var copy = (cells ?? Array.Empty<SectorQuietFillCell>()).ToArray();
            Array.Sort(copy);
            this.cells = new ReadOnlyCollection<SectorQuietFillCell>(copy);
            countByKind = Counts(copy);
            ProtectedCoordinateCount = protectedCoordinateCount;
            ReservedCoordinateCount = reservedCoordinateCount;
            PatternRenderedCoordinateCount = patternRenderedCoordinateCount;
            CanonicalDigest = canonicalDigest ?? string.Empty;
        }

        public SectorQuietActivityEventBuildRequest Request { get; }
        public IReadOnlyList<SectorQuietFillCell> Cells => cells;
        public IReadOnlyDictionary<SectorQuietFillCellKind, int> CountByKind => countByKind;
        public int SectorCount => Request.Input.Sectors.Count;
        public int ClassifiedCellCount => cells.Count;
        public int QuietFillCellCount => cells.Count(value => value.IsQuietFill);
        public int BufferCellCount => cells.Count(value => value.IsBuffer);
        public int ProtectedCoordinateCount { get; }
        public int ReservedCoordinateCount { get; }
        public int PatternRenderedCoordinateCount { get; }
        public int UnclassifiedRemainderCount => (SectorCount * 48 * 32) - ClassifiedCellCount;
        public int ProtectedIntrusionCount => cells.Count(value => value.IsQuietFill && value.ProtectedNoWrite);
        public int ReservedIntrusionCount => cells.Count(value => value.IsQuietFill && value.ReservedNoWrite);
        public int PatternOverwriteCount => cells.Count(value => value.IsQuietFill && value.PatternRendered);
        public string PlannerInputDigestBefore => Request.Input.CanonicalDigest;
        public string PlannerInputDigestAfter => Request.Input.CanonicalDigest;
        public string PacingAssignmentDigestBefore => Request.RoleZonePlan.PacingAssignmentDigestBefore;
        public string PacingAssignmentDigestAfter => Request.RoleZonePlan.PacingAssignmentDigestAfter;
        public string AnchorPlanDigestBefore => Request.AnchorPlan.CanonicalDigest;
        public string AnchorPlanDigestAfter => Request.AnchorPlan.CanonicalDigest;
        public string ClusterPlacementPlanDigestBefore => Request.ClusterPlacementPlan.CanonicalDigest;
        public string ClusterPlacementPlanDigestAfter => Request.ClusterPlacementPlan.CanonicalDigest;
        public string SpineEnvelopePlanDigestBefore => Request.SpineEnvelopePlan.CanonicalDigest;
        public string SpineEnvelopePlanDigestAfter => Request.SpineEnvelopePlan.CanonicalDigest;
        public string RoleZonePlanDigestBefore => Request.RoleZonePlan.CanonicalDigest;
        public string RoleZonePlanDigestAfter => Request.RoleZonePlan.CanonicalDigest;
        public string PatternRenderPlanDigestBefore => Request.PatternRenderPlan.CanonicalDigest;
        public string PatternRenderPlanDigestAfter => Request.PatternRenderPlan.CanonicalDigest;
        public string RouteAccessIdentityBefore => Request.RoleZonePlan.RouteAccessIdentityBefore;
        public string RouteAccessIdentityAfter => Request.RoleZonePlan.RouteAccessIdentityAfter;
        public string ExternalSocketIdentityBefore => Request.RoleZonePlan.ExternalSocketIdentityBefore;
        public string ExternalSocketIdentityAfter => Request.RoleZonePlan.ExternalSocketIdentityAfter;
        public string BoundaryIdentityBefore => Request.RoleZonePlan.BoundaryIdentityBefore;
        public string BoundaryIdentityAfter => Request.RoleZonePlan.BoundaryIdentityAfter;
        public string SpecialIdentityBefore => Request.RoleZonePlan.SpecialIdentityBefore;
        public string SpecialIdentityAfter => Request.RoleZonePlan.SpecialIdentityAfter;
        public string ClusterIdentityBefore => Request.RoleZonePlan.ClusterIdentityBefore;
        public string ClusterIdentityAfter => Request.RoleZonePlan.ClusterIdentityAfter;
        public string ProtectedOpenIdentityBefore => Request.RoleZonePlan.ProtectedOpenIdentityBefore;
        public string ProtectedOpenIdentityAfter => Request.RoleZonePlan.ProtectedOpenIdentityAfter;
        public string CanonicalDigest { get; }
        public int Count(SectorQuietFillCellKind kind) => countByKind[kind];

        public bool TryGetCell(SectorCoord sector, LocalTileCoord coordinate, out SectorQuietFillCell cell)
        {
            cell = cells.FirstOrDefault(value => value.SectorCoordinate == sector && value.Coordinate == coordinate);
            return cell != null;
        }

        private static ReadOnlyDictionary<SectorQuietFillCellKind, int> Counts(IEnumerable<SectorQuietFillCell> values)
        {
            var result = Enum.GetValues(typeof(SectorQuietFillCellKind)).Cast<SectorQuietFillCellKind>()
                .ToDictionary(key => key, key => 0);
            foreach (var value in values) result[value.Kind]++;
            return new ReadOnlyDictionary<SectorQuietFillCellKind, int>(result);
        }
    }

    public sealed class SectorQuietActivityEventPlan
    {
        private readonly ReadOnlyCollection<SectorActivityPlacementDecision> activityDecisions;
        private readonly ReadOnlyCollection<SectorEventMarkerPlacementDecision> eventDecisions;

        internal SectorQuietActivityEventPlan(
            SectorActivityEventPlacementRequest request,
            IEnumerable<SectorActivityPlacementDecision> activityDecisions,
            IEnumerable<SectorEventMarkerPlacementDecision> eventDecisions,
            int compatibleActivityCandidateCount,
            int activityRejectedCount,
            int eventNonEmptyCompatibleCandidateCount,
            int eventEmptyCompatibleCandidateCount,
            int cooldownViolationCount,
            string canonicalDigest)
        {
            Request = request;
            this.activityDecisions = Copy(activityDecisions, Comparer<SectorActivityPlacementDecision>.Default);
            this.eventDecisions = Copy(eventDecisions, Comparer<SectorEventMarkerPlacementDecision>.Default);
            CompatibleActivityCandidateCount = compatibleActivityCandidateCount;
            ActivityRejectedCount = activityRejectedCount;
            EventNonEmptyCompatibleCandidateCount = eventNonEmptyCompatibleCandidateCount;
            EventEmptyCompatibleCandidateCount = eventEmptyCompatibleCandidateCount;
            CooldownViolationCount = cooldownViolationCount;
            CanonicalDigest = canonicalDigest ?? string.Empty;
        }

        public SectorActivityEventPlacementRequest Request { get; }
        public SectorQuietFillPlan QuietFillPlan => Request.QuietFillPlan;
        public IReadOnlyList<SectorActivityPlacementDecision> ActivityDecisions => activityDecisions;
        public IReadOnlyList<SectorEventMarkerPlacementDecision> EventDecisions => eventDecisions;
        public int ActivityOpportunityCount => Request.ActivityOpportunities.Count;
        public int CompatibleActivityCandidateCount { get; }
        public int ActivitySelectedCount => activityDecisions.Count(value => value.State == SectorActivityEventPlacementState.Selected);
        public int ActivityRejectedCount { get; }
        public int StrongSelectedCount => activityDecisions.Count(value => value.State == SectorActivityEventPlacementState.Selected && value.Strength == ActivityStrengthClass.Strong);
        public int EventOpportunityCount => Request.EventOpportunities.Count;
        public int EventNonEmptyCompatibleCandidateCount { get; }
        public int EventEmptyCompatibleCandidateCount { get; }
        public int EventAssignedNonEmptyCount => eventDecisions.Count(value => value.State == SectorActivityEventPlacementState.Assigned);
        public int EventAssignedEmptyCount => eventDecisions.Count(value => value.State == SectorActivityEventPlacementState.ExplicitEmpty);
        public int CooldownExclusionCount => eventDecisions.Sum(value => value.CooldownExclusionEvidence.Count);
        public int CooldownViolationCount { get; }
        public string Map12ActivityCandidateCompilerType => typeof(ActivityCandidateIndexCompiler).FullName;
        public string Map12ActivityFrequencyPlannerType => typeof(ActivityFrequencyPlanner).FullName;
        public string Map12EventCandidateCompilerType => typeof(EventOverlayCandidateIndexCompiler).FullName;
        public string Map12EventAssignmentPlannerType => typeof(EventOverlayAssignmentPlanner).FullName;
        public string ActivityCandidateIndexDigestBefore => Request.ActivityCandidateIndex.CanonicalDigest;
        public string ActivityCandidateIndexDigestAfter => Request.ActivityCandidateIndex.CanonicalDigest;
        public string ActivityFrequencyPlanDigestBefore => Request.ActivityFrequencyPlan.CanonicalDigest;
        public string ActivityFrequencyPlanDigestAfter => Request.ActivityFrequencyPlan.CanonicalDigest;
        public string EventCandidateIndexDigestBefore => Request.EventCandidateIndex.CanonicalDigest;
        public string EventCandidateIndexDigestAfter => Request.EventCandidateIndex.CanonicalDigest;
        public string EventAssignmentPlanDigestBefore => Request.EventAssignmentPlan.CanonicalDigest;
        public string EventAssignmentPlanDigestAfter => Request.EventAssignmentPlan.CanonicalDigest;
        public string ActivityRngStreamId => Request.ActivityFrequencyPlan.RngStreamId;
        public int ActivityMap12RngStreamCount => Request.ActivityFrequencyPlan.RngStreamCreationCount;
        public ulong ActivityMap12RngDrawCount => Request.ActivityFrequencyPlan.RngDrawCount;
        public string EventRngStreamId => Request.EventAssignmentPlan.RngStreamId;
        public int EventMap12RngStreamCount => Request.EventAssignmentPlan.RngStreamCreationCount;
        public ulong EventMap12RngDrawCount => Request.EventAssignmentPlan.RngDrawCount;
        public bool RemovalSafetyIdentityPreserved => activityDecisions.Where(value => value.State == SectorActivityEventPlacementState.Selected)
            .All(value => !string.IsNullOrEmpty(value.Opportunity.RemovalSafetyIdentity) &&
                          value.Opportunity.Authority.RemovalSafetyDigest == value.Opportunity.RemovalSafetyIdentity);
        public int ProtectedIntrusionCount => QuietFillPlan.ProtectedIntrusionCount;
        public int AnchorMutationCount => 0;
        public int ClusterMutationCount => 0;
        public int SpineEnvelopeMutationCount => 0;
        public int PatternCanvasMutationCount => 0;
        public int ActivityMarkerMutationCount => 0;
        public int EventMarkerMutationCount => 0;
        public int SpecialPersistenceMutationCount => 0;
        public int FinalCanvasOwnershipWriteCount => 0;
        public int LayerConflictResolutionCount => 0;
        public int SolverInvocationCount => 0;
        public int Map14RngDrawCount => 0;
        public int RetryCount => 0;
        public int TilemapWriteCount => 0;
        public int SceneMutationCount => 0;
        public int PrefabMutationCount => 0;
        public int GameObjectMutationCount => 0;
        public int ActivityRuntimeSpawnCount => 0;
        public int EventRuntimeSpawnCount => 0;
        public int RewardExecutionCount => 0;
        public int CombatExecutionCount => 0;
        public int CraftingExecutionCount => 0;
        public int InventoryExecutionCount => 0;
        public int NpcExecutionCount => 0;
        public string CanonicalDigest { get; }
        public bool Map14_07HandoffReady => QuietFillPlan.UnclassifiedRemainderCount == 0 &&
                                             ProtectedIntrusionCount == 0 && CooldownViolationCount == 0;
    }

    public sealed class SectorQuietFillBuildResult
    {
        private readonly ReadOnlyCollection<SectorQuietActivityEventError> errors;

        internal SectorQuietFillBuildResult(
            SectorQuietFillPlan plan,
            string canonicalDigest,
            IEnumerable<SectorQuietActivityEventError> errors)
        {
            Plan = plan;
            CanonicalDigest = canonicalDigest ?? string.Empty;
            this.errors = Errors(errors);
        }

        public bool Success => Plan != null && errors.Count == 0;
        public SectorQuietFillPlan Plan { get; }
        public string CanonicalDigest { get; }
        public IReadOnlyList<SectorQuietActivityEventError> Errors => errors;
    }

    public sealed class SectorQuietActivityEventBuildResult
    {
        private readonly ReadOnlyCollection<SectorQuietActivityEventError> errors;

        internal SectorQuietActivityEventBuildResult(
            SectorQuietActivityEventPlan plan,
            string canonicalDigest,
            IEnumerable<SectorQuietActivityEventError> errors)
        {
            Plan = plan;
            CanonicalDigest = canonicalDigest ?? string.Empty;
            this.errors = Errors(errors);
        }

        public bool Success => Plan != null && errors.Count == 0;
        public SectorQuietActivityEventPlan Plan { get; }
        public string CanonicalDigest { get; }
        public IReadOnlyList<SectorQuietActivityEventError> Errors => errors;
    }

    public static class SectorQuietActivityEventCanonicalDigest
    {
        public static string ComputeQuiet(SectorQuietFillPlan plan)
        {
            if (plan == null) return string.Empty;
            var material = new StringBuilder();
            Append(material, "RULESET", "MAP14_06_QUIET_FILL_V1", plan.Request.PublicationLabel,
                plan.PlannerInputDigestBefore, plan.PacingAssignmentDigestBefore, plan.AnchorPlanDigestBefore,
                plan.ClusterPlacementPlanDigestBefore, plan.SpineEnvelopePlanDigestBefore,
                plan.RoleZonePlanDigestBefore, plan.PatternRenderPlanDigestBefore);
            foreach (var cell in plan.Cells)
                Append(material, "CELL", Number(cell.SectorIndex), Number(cell.Coordinate.X), Number(cell.Coordinate.Y),
                    Number((int)cell.Kind), Number((int)cell.SourceKind), cell.SourceIdentity,
                    Bool(cell.ProtectedNoWrite), Bool(cell.ReservedNoWrite), Bool(cell.PatternRendered),
                    Bool(cell.ActivityEligible), Bool(cell.EventEligible));
            Append(material, "COUNTS", Number(plan.ClassifiedCellCount), Number(plan.QuietFillCellCount),
                Number(plan.BufferCellCount), Number(plan.ProtectedCoordinateCount),
                Number(plan.ReservedCoordinateCount), Number(plan.PatternRenderedCoordinateCount),
                Number(plan.UnclassifiedRemainderCount));
            return Hash(material.ToString());
        }

        public static string Compute(SectorQuietActivityEventPlan plan)
        {
            if (plan == null) return string.Empty;
            var material = new StringBuilder();
            Append(material, "RULESET", "MAP14_06_ACTIVITY_EVENT_V1", plan.Request.PublicationLabel,
                plan.QuietFillPlan.CanonicalDigest, plan.ActivityCandidateIndexDigestBefore,
                plan.ActivityFrequencyPlanDigestBefore, plan.EventCandidateIndexDigestBefore,
                plan.EventAssignmentPlanDigestBefore);
            foreach (var decision in plan.ActivityDecisions)
                Append(material, "ACTIVITY", decision.OpportunityId, Number((int)decision.State),
                    decision.ActivityId.Value, Number((int)decision.Strength), decision.Reason,
                    decision.CandidateKey, Number(decision.WorldStrongBefore), Number(decision.WorldStrongAfter),
                    Number(decision.PatchStrongBefore), Number(decision.PatchStrongAfter),
                    Number(decision.SectorStrongBefore), Number(decision.SectorStrongAfter));
            foreach (var decision in plan.EventDecisions)
                Append(material, "EVENT", decision.OpportunityId, Number((int)decision.State),
                    decision.EventId.Value, Number((int)decision.EventKind), decision.CandidateKey,
                    Number(decision.PreviousProgressionOrdinal), Number(decision.CurrentProgressionOrdinal),
                    Number(decision.RequiredProgressionGap), Number(decision.ActualProgressionGap),
                    string.Join(",", decision.CooldownExclusionEvidence));
            Append(material, "COUNTS", Number(plan.ActivityOpportunityCount),
                Number(plan.CompatibleActivityCandidateCount), Number(plan.ActivitySelectedCount),
                Number(plan.ActivityRejectedCount), Number(plan.StrongSelectedCount),
                Number(plan.EventOpportunityCount), Number(plan.EventNonEmptyCompatibleCandidateCount),
                Number(plan.EventEmptyCompatibleCandidateCount), Number(plan.EventAssignedNonEmptyCount),
                Number(plan.EventAssignedEmptyCount), Number(plan.CooldownExclusionCount),
                Number(plan.CooldownViolationCount));
            return Hash(material.ToString());
        }

        internal static string Hash(string material)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(material ?? string.Empty));
                var result = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes) result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }

        internal static void Append(StringBuilder target, params string[] fields)
        {
            foreach (var field in fields)
            {
                var value = field ?? string.Empty;
                target.Append(value.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(value).Append('|');
            }
            target.Append('\n');
        }

        internal static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Bool(bool value) => value ? "1" : "0";
    }

    internal static class SectorQuietActivityEventCollections
    {
        internal static ReadOnlyCollection<T> Copy<T>(IEnumerable<T> source, IComparer<T> comparer)
        {
            var copy = source == null ? Array.Empty<T>() : source.ToArray();
            Array.Sort(copy, comparer);
            return new ReadOnlyCollection<T>(copy);
        }

        internal static ReadOnlyCollection<SectorQuietActivityEventError> Errors(
            IEnumerable<SectorQuietActivityEventError> source)
        {
            var copy = (source ?? Array.Empty<SectorQuietActivityEventError>()).Where(value => value != null)
                .Distinct().OrderBy(value => value).ToArray();
            return new ReadOnlyCollection<SectorQuietActivityEventError>(copy);
        }

        internal static int CompareAssignments(SectorPacingAssignment left, SectorPacingAssignment right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (ReferenceEquals(left, null)) return 1;
            if (ReferenceEquals(right, null)) return -1;
            var comparison = left.Coordinate.Y.CompareTo(right.Coordinate.Y);
            return comparison != 0 ? comparison : left.Coordinate.X.CompareTo(right.Coordinate.X);
        }
    }

    internal static class SectorQuietActivityEventImports
    {
        internal static ReadOnlyCollection<T> Copy<T>(IEnumerable<T> source, IComparer<T> comparer) =>
            SectorQuietActivityEventCollections.Copy(source, comparer);
        internal static ReadOnlyCollection<SectorQuietActivityEventError> Errors(IEnumerable<SectorQuietActivityEventError> source) =>
            SectorQuietActivityEventCollections.Errors(source);
        internal static int CompareAssignments(SectorPacingAssignment left, SectorPacingAssignment right) =>
            SectorQuietActivityEventCollections.CompareAssignments(left, right);
    }
}
