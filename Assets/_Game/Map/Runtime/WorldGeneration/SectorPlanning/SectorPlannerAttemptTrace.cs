using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public sealed class SectorPlannerRetryFailure : IComparable<SectorPlannerRetryFailure>
    {
        public SectorPlannerRetryFailure(
            SectorPlannerRetryFailureOwner owner,
            string code,
            string subject,
            string detail,
            int recoverySequenceOrdinal = 0,
            SectorPlannerRetryErrorCode forbiddenErrorCode = SectorPlannerRetryErrorCode.ForbiddenFallbackAttempt)
        {
            Owner = owner;
            Code = code ?? string.Empty;
            Subject = subject ?? string.Empty;
            Detail = detail ?? string.Empty;
            RecoverySequenceOrdinal = recoverySequenceOrdinal;
            ForbiddenErrorCode = forbiddenErrorCode;
        }

        public SectorPlannerRetryFailureOwner Owner { get; }
        public string Code { get; }
        public string Subject { get; }
        public string Detail { get; }
        public int RecoverySequenceOrdinal { get; }
        public SectorPlannerRetryErrorCode ForbiddenErrorCode { get; }

        public int CompareTo(SectorPlannerRetryFailure other)
        {
            if (ReferenceEquals(other, null)) return 1;
            var comparison = Owner.CompareTo(other.Owner);
            if (comparison != 0) return comparison;
            comparison = RecoverySequenceOrdinal.CompareTo(other.RecoverySequenceOrdinal);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Code, other.Code, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }
    }

    public sealed class SectorPlannerAttemptTraceInput : IComparable<SectorPlannerAttemptTraceInput>
    {
        private readonly ReadOnlyCollection<string> candidateIds;

        public SectorPlannerAttemptTraceInput(
            int attemptOrdinal,
            int nodeOrdinal,
            SectorPlannerRetryFailure failure,
            IEnumerable<string> sourceCandidateIds,
            bool recoverySucceeded = true)
        {
            AttemptOrdinal = attemptOrdinal;
            NodeOrdinal = nodeOrdinal;
            Failure = failure;
            candidateIds = new ReadOnlyCollection<string>(
                (sourceCandidateIds ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrEmpty(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray());
            RecoverySucceeded = recoverySucceeded;
        }

        public int AttemptOrdinal { get; }
        public int NodeOrdinal { get; }
        public SectorPlannerRetryFailure Failure { get; }
        public IReadOnlyList<string> CandidateIds => candidateIds;
        public bool RecoverySucceeded { get; }

        public int CompareTo(SectorPlannerAttemptTraceInput other)
        {
            if (ReferenceEquals(other, null)) return 1;
            var comparison = AttemptOrdinal.CompareTo(other.AttemptOrdinal);
            if (comparison != 0) return comparison;
            comparison = NodeOrdinal.CompareTo(other.NodeOrdinal);
            if (comparison != 0) return comparison;
            comparison = Failure == null ? (other.Failure == null ? 0 : -1) : Failure.CompareTo(other.Failure);
            return comparison != 0
                ? comparison
                : string.Compare(string.Join(";", candidateIds), string.Join(";", other.candidateIds), StringComparison.Ordinal);
        }
    }

    public sealed class SectorPlannerRngTrace : IComparable<SectorPlannerRngTrace>
    {
        public SectorPlannerRngTrace(
            string streamId,
            SectorPlannerRngPassScope passScope,
            string scopeLabel,
            ulong worldSeed,
            SectorCoord sectorCoordinate,
            int attemptOrdinal,
            int nodeOrdinal,
            ulong drawOrdinalBefore,
            ulong drawOrdinalAfter,
            ulong drawCount,
            int ticket,
            int candidateCount,
            string chosenCandidateId,
            string initialStateDigest,
            string finalStateDigest)
        {
            StreamId = streamId ?? string.Empty;
            PassScope = passScope;
            ScopeLabel = scopeLabel ?? string.Empty;
            WorldSeed = worldSeed;
            SectorCoordinate = sectorCoordinate;
            AttemptOrdinal = attemptOrdinal;
            NodeOrdinal = nodeOrdinal;
            DrawOrdinalBefore = drawOrdinalBefore;
            DrawOrdinalAfter = drawOrdinalAfter;
            DrawCount = drawCount;
            Ticket = ticket;
            CandidateCount = candidateCount;
            ChosenCandidateId = chosenCandidateId ?? string.Empty;
            InitialStateDigest = initialStateDigest ?? string.Empty;
            FinalStateDigest = finalStateDigest ?? string.Empty;
            CanonicalDigest = SectorPlannerRetryCanonicalDigest.ComputeRngTrace(this);
        }

        public string StreamId { get; }
        public SectorPlannerRngPassScope PassScope { get; }
        public string ScopeLabel { get; }
        public ulong WorldSeed { get; }
        public SectorCoord SectorCoordinate { get; }
        public int AttemptOrdinal { get; }
        public int NodeOrdinal { get; }
        public ulong DrawOrdinalBefore { get; }
        public ulong DrawOrdinalAfter { get; }
        public ulong DrawCount { get; }
        public int Ticket { get; }
        public int CandidateCount { get; }
        public string ChosenCandidateId { get; }
        public string InitialStateDigest { get; }
        public string FinalStateDigest { get; }
        public string CanonicalDigest { get; }

        public int CompareTo(SectorPlannerRngTrace other)
        {
            if (ReferenceEquals(other, null)) return 1;
            var comparison = AttemptOrdinal.CompareTo(other.AttemptOrdinal);
            if (comparison != 0) return comparison;
            comparison = NodeOrdinal.CompareTo(other.NodeOrdinal);
            if (comparison != 0) return comparison;
            comparison = PassScope.CompareTo(other.PassScope);
            if (comparison != 0) return comparison;
            return string.Compare(CanonicalDigest, other.CanonicalDigest, StringComparison.Ordinal);
        }
    }

    public sealed class SectorPlannerAttemptTrace : IComparable<SectorPlannerAttemptTrace>
    {
        public SectorPlannerAttemptTrace(
            int attemptOrdinal,
            int nodeOrdinal,
            SectorPlannerRetryFailure failure,
            SectorPlannerRetryStage nextStage,
            SectorPlannerRetryDecisionKind decision,
            string reason)
        {
            AttemptOrdinal = attemptOrdinal;
            NodeOrdinal = nodeOrdinal;
            Failure = failure;
            NextStage = nextStage;
            Decision = decision;
            Reason = reason ?? string.Empty;
        }

        public int AttemptOrdinal { get; }
        public int NodeOrdinal { get; }
        public SectorPlannerRetryFailure Failure { get; }
        public SectorPlannerRetryStage NextStage { get; }
        public SectorPlannerRetryDecisionKind Decision { get; }
        public string Reason { get; }

        public int CompareTo(SectorPlannerAttemptTrace other)
        {
            if (ReferenceEquals(other, null)) return 1;
            var comparison = AttemptOrdinal.CompareTo(other.AttemptOrdinal);
            if (comparison != 0) return comparison;
            comparison = NodeOrdinal.CompareTo(other.NodeOrdinal);
            if (comparison != 0) return comparison;
            comparison = NextStage.CompareTo(other.NextStage);
            if (comparison != 0) return comparison;
            return Failure == null ? (other.Failure == null ? 0 : -1) : Failure.CompareTo(other.Failure);
        }
    }

    public sealed class SectorPlannerRetryNodeTrace : IComparable<SectorPlannerRetryNodeTrace>
    {
        public SectorPlannerRetryNodeTrace(
            SectorPlannerAttemptTrace attemptTrace,
            SectorPlannerRngTrace rngTrace,
            string selectedCandidateId,
            bool recoverySucceeded,
            SectorPlannerRetryDecisionKind resultingDecision)
        {
            AttemptTrace = attemptTrace;
            RngTrace = rngTrace;
            SelectedCandidateId = selectedCandidateId ?? string.Empty;
            RecoverySucceeded = recoverySucceeded;
            ResultingDecision = resultingDecision;
        }

        public SectorPlannerAttemptTrace AttemptTrace { get; }
        public SectorPlannerRngTrace RngTrace { get; }
        public string SelectedCandidateId { get; }
        public bool RecoverySucceeded { get; }
        public SectorPlannerRetryDecisionKind ResultingDecision { get; }
        public SectorPlannerRetryStage Stage => AttemptTrace == null ? SectorPlannerRetryStage.None : AttemptTrace.NextStage;
        public int AttemptOrdinal => AttemptTrace == null ? -1 : AttemptTrace.AttemptOrdinal;
        public int NodeOrdinal => AttemptTrace == null ? -1 : AttemptTrace.NodeOrdinal;

        public int CompareTo(SectorPlannerRetryNodeTrace other)
        {
            if (ReferenceEquals(other, null)) return 1;
            var comparison = AttemptOrdinal.CompareTo(other.AttemptOrdinal);
            if (comparison != 0) return comparison;
            comparison = NodeOrdinal.CompareTo(other.NodeOrdinal);
            return comparison != 0
                ? comparison
                : string.Compare(SelectedCandidateId, other.SelectedCandidateId, StringComparison.Ordinal);
        }
    }

    public sealed class SectorPlannerRetryBuildRequest
    {
        private readonly ReadOnlyCollection<SectorPlannerAttemptTraceInput> attemptInputs;
        private readonly ReadOnlyCollection<SectorPlannerAttemptTrace> sourceAttemptTraces;
        private readonly ReadOnlyCollection<SectorPlannerRngTrace> sourceRngTraces;

        public SectorPlannerRetryBuildRequest(
            SectorCanvasOwnershipPlan ownershipPlan,
            SectorPlannerRetryPolicy retryPolicy,
            DeterministicRngStreamFactory rngAuthority,
            ulong worldSeed,
            SectorCoord sectorCoordinate,
            int initialAttemptOrdinal = 0,
            IEnumerable<SectorPlannerAttemptTraceInput> sourceAttemptInputs = null,
            IEnumerable<SectorPlannerAttemptTrace> sourcePublishedAttemptTraces = null,
            IEnumerable<SectorPlannerRngTrace> sourcePublishedRngTraces = null,
            string publicationLabel = "MAP14_08_REFERENCE_RETRY_PLAN",
            bool upstreamMutationClaim = false,
            bool patternMutationClaim = false,
            bool clusterMutationClaim = false,
            bool footprintMutationClaim = false,
            bool ownershipMutationClaim = false,
            int fallbackCorridorCarveCount = 0,
            int validationRelaxationCount = 0,
            int wholeSectorRerandomCount = 0,
            int wholeWorldRerandomCount = 0,
            int fixedAnchorMutationCount = 0,
            int boundarySocketMutationCount = 0,
            int specialReservationMutationCount = 0,
            int protectedMaskRelaxationCount = 0,
            int tilemapWriteCount = 0,
            int sceneMutationCount = 0,
            int prefabMutationCount = 0,
            int gameObjectMutationCount = 0,
            int activityRuntimeSpawnCount = 0,
            int eventRuntimeSpawnCount = 0,
            int gameplayExecutionCount = 0,
            int debugExportWriteCount = 0)
        {
            OwnershipPlan = ownershipPlan;
            RetryPolicy = retryPolicy;
            RngAuthority = rngAuthority;
            WorldSeed = worldSeed;
            SectorCoordinate = sectorCoordinate;
            InitialAttemptOrdinal = initialAttemptOrdinal;
            attemptInputs = new ReadOnlyCollection<SectorPlannerAttemptTraceInput>(
                (sourceAttemptInputs ?? Array.Empty<SectorPlannerAttemptTraceInput>())
                .Where(value => value != null).OrderBy(value => value).ToArray());
            sourceAttemptTraces = new ReadOnlyCollection<SectorPlannerAttemptTrace>(
                (sourcePublishedAttemptTraces ?? Array.Empty<SectorPlannerAttemptTrace>())
                .Where(value => value != null).OrderBy(value => value).ToArray());
            sourceRngTraces = new ReadOnlyCollection<SectorPlannerRngTrace>(
                (sourcePublishedRngTraces ?? Array.Empty<SectorPlannerRngTrace>())
                .Where(value => value != null).OrderBy(value => value).ToArray());
            PublicationLabel = publicationLabel ?? string.Empty;
            UpstreamMutationClaim = upstreamMutationClaim;
            PatternMutationClaim = patternMutationClaim;
            ClusterMutationClaim = clusterMutationClaim;
            FootprintMutationClaim = footprintMutationClaim;
            OwnershipMutationClaim = ownershipMutationClaim;
            FallbackCorridorCarveCount = fallbackCorridorCarveCount;
            ValidationRelaxationCount = validationRelaxationCount;
            WholeSectorRerandomCount = wholeSectorRerandomCount;
            WholeWorldRerandomCount = wholeWorldRerandomCount;
            FixedAnchorMutationCount = fixedAnchorMutationCount;
            BoundarySocketMutationCount = boundarySocketMutationCount;
            SpecialReservationMutationCount = specialReservationMutationCount;
            ProtectedMaskRelaxationCount = protectedMaskRelaxationCount;
            TilemapWriteCount = tilemapWriteCount;
            SceneMutationCount = sceneMutationCount;
            PrefabMutationCount = prefabMutationCount;
            GameObjectMutationCount = gameObjectMutationCount;
            ActivityRuntimeSpawnCount = activityRuntimeSpawnCount;
            EventRuntimeSpawnCount = eventRuntimeSpawnCount;
            GameplayExecutionCount = gameplayExecutionCount;
            DebugExportWriteCount = debugExportWriteCount;
        }

        public SectorCanvasOwnershipPlan OwnershipPlan { get; }
        public SectorPlannerRetryPolicy RetryPolicy { get; }
        public DeterministicRngStreamFactory RngAuthority { get; }
        public ulong WorldSeed { get; }
        public SectorCoord SectorCoordinate { get; }
        public int InitialAttemptOrdinal { get; }
        public IReadOnlyList<SectorPlannerAttemptTraceInput> AttemptInputs => attemptInputs;
        public IReadOnlyList<SectorPlannerAttemptTrace> SourceAttemptTraces => sourceAttemptTraces;
        public IReadOnlyList<SectorPlannerRngTrace> SourceRngTraces => sourceRngTraces;
        public string PublicationLabel { get; }
        public bool UpstreamMutationClaim { get; }
        public bool PatternMutationClaim { get; }
        public bool ClusterMutationClaim { get; }
        public bool FootprintMutationClaim { get; }
        public bool OwnershipMutationClaim { get; }
        public int FallbackCorridorCarveCount { get; }
        public int ValidationRelaxationCount { get; }
        public int WholeSectorRerandomCount { get; }
        public int WholeWorldRerandomCount { get; }
        public int FixedAnchorMutationCount { get; }
        public int BoundarySocketMutationCount { get; }
        public int SpecialReservationMutationCount { get; }
        public int ProtectedMaskRelaxationCount { get; }
        public int TilemapWriteCount { get; }
        public int SceneMutationCount { get; }
        public int PrefabMutationCount { get; }
        public int GameObjectMutationCount { get; }
        public int ActivityRuntimeSpawnCount { get; }
        public int EventRuntimeSpawnCount { get; }
        public int GameplayExecutionCount { get; }
        public int DebugExportWriteCount { get; }
    }

    public sealed class SectorPlannerRetryPlan
    {
        private readonly ReadOnlyCollection<SectorPlannerAttemptTrace> attemptTraces;
        private readonly ReadOnlyCollection<SectorPlannerRetryNodeTrace> nodeTraces;
        private readonly ReadOnlyCollection<SectorPlannerRngTrace> rngTraces;
        private readonly ReadOnlyDictionary<SectorPlannerRetryStage, int> retryCountByStage;

        internal SectorPlannerRetryPlan(
            SectorPlannerRetryBuildRequest request,
            IEnumerable<SectorPlannerAttemptTrace> sourceAttemptTraces,
            IEnumerable<SectorPlannerRetryNodeTrace> sourceNodeTraces,
            SectorPlannerRetryDecisionKind terminalDecision,
            string canonicalDigest)
        {
            Request = request;
            attemptTraces = new ReadOnlyCollection<SectorPlannerAttemptTrace>(
                (sourceAttemptTraces ?? Array.Empty<SectorPlannerAttemptTrace>()).OrderBy(value => value).ToArray());
            nodeTraces = new ReadOnlyCollection<SectorPlannerRetryNodeTrace>(
                (sourceNodeTraces ?? Array.Empty<SectorPlannerRetryNodeTrace>()).OrderBy(value => value).ToArray());
            rngTraces = new ReadOnlyCollection<SectorPlannerRngTrace>(
                nodeTraces.Where(value => value.RngTrace != null).Select(value => value.RngTrace).OrderBy(value => value).ToArray());
            TerminalDecision = terminalDecision;
            CanonicalDigest = canonicalDigest ?? string.Empty;
            var counts = new SortedDictionary<SectorPlannerRetryStage, int>();
            foreach (SectorPlannerRetryStage stage in Enum.GetValues(typeof(SectorPlannerRetryStage))) counts[stage] = 0;
            foreach (var node in nodeTraces) counts[node.Stage]++;
            retryCountByStage = new ReadOnlyDictionary<SectorPlannerRetryStage, int>(counts);
        }

        public SectorPlannerRetryBuildRequest Request { get; }
        public IReadOnlyList<SectorPlannerAttemptTrace> AttemptTraces => attemptTraces;
        public IReadOnlyList<SectorPlannerRetryNodeTrace> NodeTraces => nodeTraces;
        public IReadOnlyList<SectorPlannerRngTrace> RngTraces => rngTraces;
        public IReadOnlyDictionary<SectorPlannerRetryStage, int> RetryCountByStage => retryCountByStage;
        public SectorPlannerRetryDecisionKind TerminalDecision { get; }
        public string CanonicalDigest { get; }
        public int FirstPassAcceptCount => TerminalDecision == SectorPlannerRetryDecisionKind.AcceptFirstPass ? 1 : 0;
        public int SyntheticRetryCaseCount => attemptTraces.Count;
        public int RetryNodeCount => nodeTraces.Count;
        public int TerminalDecisionCount => 1;
        public int Map14RetryRngDrawCount => rngTraces.Sum(value => checked((int)value.DrawCount));
        public ulong Map12ActivityRngDrawCount => Request.OwnershipPlan.Request.QuietActivityEventPlan.ActivityMap12RngDrawCount;
        public ulong Map12EventRngDrawCount => Request.OwnershipPlan.Request.QuietActivityEventPlan.EventMap12RngDrawCount;
        public int Count(SectorPlannerRetryStage stage) => retryCountByStage[stage];

        public string PlannerInputDigestBefore => Request.OwnershipPlan.PlannerInputDigestBefore;
        public string PlannerInputDigestAfter => Request.OwnershipPlan.PlannerInputDigestAfter;
        public string PacingAssignmentDigestBefore => Request.OwnershipPlan.PacingAssignmentDigestBefore;
        public string PacingAssignmentDigestAfter => Request.OwnershipPlan.PacingAssignmentDigestAfter;
        public string FixedAnchorPlanDigestBefore => Request.OwnershipPlan.FixedAnchorPlanDigestBefore;
        public string FixedAnchorPlanDigestAfter => Request.OwnershipPlan.FixedAnchorPlanDigestAfter;
        public string ClusterPlacementPlanDigestBefore => Request.OwnershipPlan.ClusterPlacementPlanDigestBefore;
        public string ClusterPlacementPlanDigestAfter => Request.OwnershipPlan.ClusterPlacementPlanDigestAfter;
        public string SpineEnvelopePlanDigestBefore => Request.OwnershipPlan.SpineEnvelopePlanDigestBefore;
        public string SpineEnvelopePlanDigestAfter => Request.OwnershipPlan.SpineEnvelopePlanDigestAfter;
        public string RolePatternPlanDigestBefore => Request.OwnershipPlan.RolePatternPlanDigestBefore;
        public string RolePatternPlanDigestAfter => Request.OwnershipPlan.RolePatternPlanDigestAfter;
        public string PatternRenderPlanDigestBefore => Request.OwnershipPlan.PatternRenderPlanDigestBefore;
        public string PatternRenderPlanDigestAfter => Request.OwnershipPlan.PatternRenderPlanDigestAfter;
        public string QuietActivityEventPlanDigestBefore => Request.OwnershipPlan.QuietActivityEventPlanDigestBefore;
        public string QuietActivityEventPlanDigestAfter => Request.OwnershipPlan.QuietActivityEventPlanDigestAfter;
        public string CanvasOwnershipPlanDigestBefore => Request.OwnershipPlan.CanonicalDigest;
        public string CanvasOwnershipPlanDigestAfter => Request.OwnershipPlan.CanonicalDigest;
        public string ActivityAuthorityDigestBefore => Request.OwnershipPlan.ActivityAuthorityDigestBefore;
        public string ActivityAuthorityDigestAfter => Request.OwnershipPlan.ActivityAuthorityDigestAfter;
        public string EventAuthorityDigestBefore => Request.OwnershipPlan.EventAuthorityDigestBefore;
        public string EventAuthorityDigestAfter => Request.OwnershipPlan.EventAuthorityDigestAfter;
        public string RouteAccessIdentityBefore => Request.OwnershipPlan.RouteAccessIdentityBefore;
        public string RouteAccessIdentityAfter => Request.OwnershipPlan.RouteAccessIdentityAfter;
        public string ExternalSocketIdentityBefore => Request.OwnershipPlan.ExternalSocketIdentityBefore;
        public string ExternalSocketIdentityAfter => Request.OwnershipPlan.ExternalSocketIdentityAfter;
        public string BoundaryIdentityBefore => Request.OwnershipPlan.BoundaryIdentityBefore;
        public string BoundaryIdentityAfter => Request.OwnershipPlan.BoundaryIdentityAfter;
        public string SpecialIdentityBefore => Request.OwnershipPlan.SpecialIdentityBefore;
        public string SpecialIdentityAfter => Request.OwnershipPlan.SpecialIdentityAfter;
        public string ClusterIdentityBefore => Request.OwnershipPlan.ClusterIdentityBefore;
        public string ClusterIdentityAfter => Request.OwnershipPlan.ClusterIdentityAfter;
        public string ProtectedOpenIdentityBefore => Request.OwnershipPlan.ProtectedOpenIdentityBefore;
        public string ProtectedOpenIdentityAfter => Request.OwnershipPlan.ProtectedOpenIdentityAfter;

        public bool AllUpstreamIdentitiesPreserved =>
            Same(PlannerInputDigestBefore, PlannerInputDigestAfter) &&
            Same(PacingAssignmentDigestBefore, PacingAssignmentDigestAfter) &&
            Same(FixedAnchorPlanDigestBefore, FixedAnchorPlanDigestAfter) &&
            Same(ClusterPlacementPlanDigestBefore, ClusterPlacementPlanDigestAfter) &&
            Same(SpineEnvelopePlanDigestBefore, SpineEnvelopePlanDigestAfter) &&
            Same(RolePatternPlanDigestBefore, RolePatternPlanDigestAfter) &&
            Same(PatternRenderPlanDigestBefore, PatternRenderPlanDigestAfter) &&
            Same(QuietActivityEventPlanDigestBefore, QuietActivityEventPlanDigestAfter) &&
            Same(CanvasOwnershipPlanDigestBefore, CanvasOwnershipPlanDigestAfter) &&
            Same(ActivityAuthorityDigestBefore, ActivityAuthorityDigestAfter) &&
            Same(EventAuthorityDigestBefore, EventAuthorityDigestAfter) &&
            Same(RouteAccessIdentityBefore, RouteAccessIdentityAfter) &&
            Same(ExternalSocketIdentityBefore, ExternalSocketIdentityAfter) &&
            Same(BoundaryIdentityBefore, BoundaryIdentityAfter) &&
            Same(SpecialIdentityBefore, SpecialIdentityAfter) &&
            Same(ClusterIdentityBefore, ClusterIdentityAfter) &&
            Same(ProtectedOpenIdentityBefore, ProtectedOpenIdentityAfter);

        public bool Map14_09HandoffReady => Request.OwnershipPlan.Map14_08HandoffReady &&
                                             AllUpstreamIdentitiesPreserved &&
                                             (TerminalDecision == SectorPlannerRetryDecisionKind.AcceptFirstPass ||
                                              TerminalDecision == SectorPlannerRetryDecisionKind.AcceptRecovered);

        public int FallbackCorridorCarveCount => 0;
        public int ValidationRelaxationCount => 0;
        public int WholeSectorRerandomCount => 0;
        public int WholeWorldRerandomCount => 0;
        public int FixedAnchorMutationCount => 0;
        public int BoundarySocketMutationCount => 0;
        public int SpecialReservationMutationCount => 0;
        public int ProtectedMaskRelaxationCount => 0;
        public int TilemapWriteCount => 0;
        public int SceneMutationCount => 0;
        public int PrefabMutationCount => 0;
        public int GameObjectMutationCount => 0;
        public int ActivityRuntimeSpawnCount => 0;
        public int EventRuntimeSpawnCount => 0;
        public int GameplayExecutionCount => 0;
        public int DebugExportWriteCount => 0;

        private static bool Same(string left, string right) => string.Equals(left, right, StringComparison.Ordinal);
    }

    public sealed class SectorPlannerRetryBuildResult
    {
        private readonly ReadOnlyCollection<SectorPlannerAttemptTrace> attemptTraces;
        private readonly ReadOnlyCollection<SectorPlannerRetryNodeTrace> nodeTraces;
        private readonly ReadOnlyCollection<SectorPlannerRetryError> errors;

        internal SectorPlannerRetryBuildResult(
            SectorPlannerRetryBuildRequest request,
            SectorPlannerRetryPlan plan,
            IEnumerable<SectorPlannerAttemptTrace> sourceAttemptTraces,
            IEnumerable<SectorPlannerRetryNodeTrace> sourceNodeTraces,
            SectorPlannerRetryDecisionKind terminalDecision,
            IEnumerable<SectorPlannerRetryError> sourceErrors)
        {
            Request = request;
            errors = new ReadOnlyCollection<SectorPlannerRetryError>(
                (sourceErrors ?? Array.Empty<SectorPlannerRetryError>()).Where(value => value != null)
                .Distinct().OrderBy(value => value).ToArray());
            Plan = errors.Count == 0 ? plan : null;
            attemptTraces = new ReadOnlyCollection<SectorPlannerAttemptTrace>(
                (sourceAttemptTraces ?? Array.Empty<SectorPlannerAttemptTrace>()).OrderBy(value => value).ToArray());
            nodeTraces = new ReadOnlyCollection<SectorPlannerRetryNodeTrace>(
                (sourceNodeTraces ?? Array.Empty<SectorPlannerRetryNodeTrace>()).OrderBy(value => value).ToArray());
            TerminalDecision = terminalDecision;
        }

        public bool Success => Plan != null && errors.Count == 0;
        public SectorPlannerRetryBuildRequest Request { get; }
        public SectorPlannerRetryPlan Plan { get; }
        public IReadOnlyList<SectorPlannerAttemptTrace> AttemptTraces => attemptTraces;
        public IReadOnlyList<SectorPlannerRetryNodeTrace> NodeTraces => nodeTraces;
        public IReadOnlyList<SectorPlannerRetryError> Errors => errors;
        public SectorPlannerRetryDecisionKind TerminalDecision { get; }
        public string CanonicalDigest => Plan == null ? string.Empty : Plan.CanonicalDigest;
        public int Map14RetryRngDrawCount => nodeTraces.Where(value => value.RngTrace != null)
            .Sum(value => checked((int)value.RngTrace.DrawCount));
        public int AbortCount => IsAbort(TerminalDecision) ? 1 : 0;
        public int CapAbortCount => TerminalDecision == SectorPlannerRetryDecisionKind.AbortCapReached ? 1 : 0;
        public int ForbiddenAbortCount => TerminalDecision == SectorPlannerRetryDecisionKind.AbortForbiddenFallback ? 1 : 0;

        private static bool IsAbort(SectorPlannerRetryDecisionKind decision)
        {
            return decision == SectorPlannerRetryDecisionKind.AbortCapReached ||
                   decision == SectorPlannerRetryDecisionKind.AbortUnownedFailure ||
                   decision == SectorPlannerRetryDecisionKind.AbortForbiddenFallback ||
                   decision == SectorPlannerRetryDecisionKind.AbortNonDeterministicTrace;
        }
    }

    public static class SectorPlannerRetryCanonicalDigest
    {
        public static string ComputePolicy(SectorPlannerRetryPolicy policy)
        {
            if (policy == null || policy.Limits == null) return string.Empty;
            var material = new StringBuilder();
            Append(material, "POLICY", policy.RulesetVersion);
            foreach (var stage in policy.RecoveryOrder) Append(material, "ORDER", stage);
            Append(material, "LIMITS",
                policy.Limits.MaxPatternCandidateAttemptsPerZone,
                policy.Limits.MaxPatternTransformAttemptsPerPattern,
                policy.Limits.MaxClusterVariantAttemptsPerSector,
                policy.Limits.MaxClusterFootprintAttemptsPerSector,
                policy.Limits.MaxRetryNodesPerSector,
                policy.Limits.MaxTotalLocalAttemptsPerSector);
            return Hash(material.ToString());
        }

        public static string ComputeRngTrace(SectorPlannerRngTrace trace)
        {
            if (trace == null) return string.Empty;
            var material = new StringBuilder();
            Append(material, "RNG", trace.StreamId, trace.PassScope, trace.ScopeLabel,
                trace.WorldSeed, trace.SectorCoordinate.X, trace.SectorCoordinate.Y,
                trace.AttemptOrdinal, trace.NodeOrdinal, trace.DrawOrdinalBefore,
                trace.DrawOrdinalAfter, trace.DrawCount, trace.Ticket,
                trace.CandidateCount, trace.ChosenCandidateId,
                trace.InitialStateDigest, trace.FinalStateDigest);
            return Hash(material.ToString());
        }

        public static string ComputePlan(
            SectorPlannerRetryBuildRequest request,
            IEnumerable<SectorPlannerAttemptTrace> attempts,
            IEnumerable<SectorPlannerRetryNodeTrace> nodes,
            SectorPlannerRetryDecisionKind terminalDecision)
        {
            if (request == null || request.OwnershipPlan == null || request.RetryPolicy == null) return string.Empty;
            var material = new StringBuilder();
            Append(material, "RULESET", "MAP14_08_RETRY_PLAN_V1", request.PublicationLabel,
                request.OwnershipPlan.CanonicalDigest, request.RetryPolicy.CanonicalDigest,
                request.WorldSeed, request.SectorCoordinate.X, request.SectorCoordinate.Y,
                request.InitialAttemptOrdinal, terminalDecision);
            foreach (var attempt in (attempts ?? Array.Empty<SectorPlannerAttemptTrace>()).OrderBy(value => value))
            {
                Append(material, "ATTEMPT", attempt.AttemptOrdinal, attempt.NodeOrdinal,
                    attempt.NextStage, attempt.Decision, attempt.Reason,
                    attempt.Failure == null ? SectorPlannerRetryFailureOwner.Unknown : attempt.Failure.Owner,
                    attempt.Failure == null ? string.Empty : attempt.Failure.Code,
                    attempt.Failure == null ? string.Empty : attempt.Failure.Subject,
                    attempt.Failure == null ? string.Empty : attempt.Failure.Detail);
            }
            foreach (var node in (nodes ?? Array.Empty<SectorPlannerRetryNodeTrace>()).OrderBy(value => value))
            {
                Append(material, "NODE", node.AttemptOrdinal, node.NodeOrdinal, node.Stage,
                    node.SelectedCandidateId, node.RecoverySucceeded, node.ResultingDecision,
                    node.RngTrace == null ? string.Empty : node.RngTrace.CanonicalDigest);
            }
            return Hash(material.ToString());
        }

        public static string Hash(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                var result = new StringBuilder(bytes.Length * 2);
                foreach (var item in bytes) result.Append(item.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }

        private static void Append(StringBuilder material, params object[] values)
        {
            foreach (var value in values)
            {
                var text = value == null ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture);
                material.Append(text.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(text).Append('|');
            }
            material.Append('\n');
        }
    }
}
