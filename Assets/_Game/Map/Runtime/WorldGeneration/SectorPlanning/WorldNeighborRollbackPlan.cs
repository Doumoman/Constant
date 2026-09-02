using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum WorldRollbackScopeKind
    {
        Corner = 0,
        Edge = 1,
        Interior = 2,
    }

    public enum WorldContradictionKind
    {
        SpecialConflict = 0,
        BoundaryConflict = 1,
        MandatoryRouteConflict = 2,
        IntersectorSocketConflict = 3,
        ReservationConflict = 4,
        PacingDensityConflict = 5,
        ClusterCandidateExhausted = 6,
        RetryExhausted = 7,
        Unknown = 8,
    }

    public enum WorldContradictionSource
    {
        Special = 0,
        Boundary = 1,
        MandatoryRoute = 2,
        IntersectorSocket = 3,
        Reservation = 4,
        PacingDensity = 5,
        ClusterCandidate = 6,
        Retry = 7,
        Unknown = 8,
    }

    public enum WorldRollbackDecisionKind
    {
        BoundedRetry = 0,
        Abort = 1,
        BlockedOwner = 2,
    }

    public enum WorldNeighborRollbackFailureCode
    {
        MissingRequest = 0,
        MissingWorldSolveOrder = 1,
        FailedWorldSolveOrder = 2,
        MissingIntersectorPlan = 3,
        MissingReservationPlan = 4,
        MissingPacingDensityPlan = 5,
        InvalidWorldSectorCount = 6,
        InvalidInternalEdgeCount = 7,
        InvalidAuthorityLink = 8,
        InvalidDigest = 9,
        FailedSectorOutOfBounds = 10,
        MissingFailedSector = 11,
        MissingScopeSector = 12,
        ScopeExceedsLimit = 13,
        InvalidContradiction = 14,
        ContradictionMissingSector = 15,
        UnknownEdgeEvidence = 16,
        UnknownReservationEvidence = 17,
        UnknownPacingEvidence = 18,
        UnknownCandidateEvidence = 19,
        UnknownRetryEvidence = 20,
        FirstContradictionSelectionFailed = 21,
        WholeWorldRerandomForbidden = 22,
        FallbackCarveForbidden = 23,
        SilentWideningForbidden = 24,
        SectorRerenderForbidden = 25,
        MutationClaim = 26,
    }

    public sealed class WorldRollbackSector : IComparable<WorldRollbackSector>
    {
        public WorldRollbackSector(
            WorldSectorId sectorId,
            WorldSectorCoordinate coordinate,
            int solveStepIndex,
            bool isFailedSector)
        {
            SectorId = sectorId;
            Coordinate = coordinate;
            SolveStepIndex = solveStepIndex;
            IsFailedSector = isFailedSector;
        }

        public WorldSectorId SectorId { get; }
        public WorldSectorCoordinate Coordinate { get; }
        public int SolveStepIndex { get; }
        public bool IsFailedSector { get; }

        public int CompareTo(WorldRollbackSector other)
        {
            if (other == null) return -1;
            var comparison = (IsFailedSector ? 0 : 1).CompareTo(other.IsFailedSector ? 0 : 1);
            if (comparison != 0) return comparison;
            comparison = SolveStepIndex.CompareTo(other.SolveStepIndex);
            return comparison != 0 ? comparison : SectorId.CompareTo(other.SectorId);
        }
    }

    public sealed class WorldRollbackScope
    {
        private readonly ReadOnlyCollection<WorldRollbackSector> sectors;

        internal WorldRollbackScope(
            WorldRollbackScopeKind kind,
            WorldSectorId failedSectorId,
            WorldSectorCoordinate failedCoordinate,
            IEnumerable<WorldRollbackSector> sourceSectors)
        {
            Kind = kind;
            FailedSectorId = failedSectorId;
            FailedCoordinate = failedCoordinate;
            sectors = new ReadOnlyCollection<WorldRollbackSector>((sourceSectors ??
                Array.Empty<WorldRollbackSector>()).Where(value => value != null).OrderBy(value => value).ToArray());
        }

        public const int Radius = 1;
        public const int MaximumSectorCount = 9;

        public WorldRollbackScopeKind Kind { get; }
        public WorldSectorId FailedSectorId { get; }
        public WorldSectorCoordinate FailedCoordinate { get; }
        public IReadOnlyList<WorldRollbackSector> Sectors => sectors;
        public int SectorCount => sectors.Count;
        public int ExpectedSectorCount => Kind == WorldRollbackScopeKind.Corner
            ? 4
            : Kind == WorldRollbackScopeKind.Edge ? 6 : 9;
        public bool ContainsFailedSector => sectors.Count(value => value.IsFailedSector &&
            value.SectorId == FailedSectorId) == 1;
    }

    public sealed class WorldContradictionEvidence : IComparable<WorldContradictionEvidence>
    {
        private readonly ReadOnlyCollection<WorldIntersectorEdgeId> relatedEdgeIds;
        private readonly ReadOnlyCollection<string> relatedReservationIds;
        private readonly ReadOnlyCollection<string> relatedPacingEvidenceIds;
        private readonly ReadOnlyCollection<string> relatedCandidateIds;
        private readonly ReadOnlyCollection<string> retryLabels;

        public WorldContradictionEvidence(
            string stableContradictionId,
            WorldContradictionKind kind,
            WorldContradictionSource source,
            WorldSectorId sectorId,
            int solveStepIndex,
            IEnumerable<WorldIntersectorEdgeId> sourceRelatedEdgeIds,
            IEnumerable<string> sourceRelatedReservationIds,
            IEnumerable<string> sourceRelatedPacingEvidenceIds,
            IEnumerable<string> sourceRelatedCandidateIds,
            IEnumerable<string> sourceRetryLabels,
            bool retryableWithinScope,
            bool requiresUpstreamOwnerRepair)
        {
            StableContradictionId = stableContradictionId ?? string.Empty;
            Kind = kind;
            Source = source;
            SectorId = sectorId;
            SolveStepIndex = solveStepIndex;
            relatedEdgeIds = new ReadOnlyCollection<WorldIntersectorEdgeId>((sourceRelatedEdgeIds ??
                Array.Empty<WorldIntersectorEdgeId>()).Distinct().OrderBy(value => value).ToArray());
            relatedReservationIds = Freeze(sourceRelatedReservationIds);
            relatedPacingEvidenceIds = Freeze(sourceRelatedPacingEvidenceIds);
            relatedCandidateIds = Freeze(sourceRelatedCandidateIds);
            retryLabels = Freeze(sourceRetryLabels);
            RetryableWithinScope = retryableWithinScope;
            RequiresUpstreamOwnerRepair = requiresUpstreamOwnerRepair;
        }

        public string StableContradictionId { get; }
        public WorldContradictionKind Kind { get; }
        public WorldContradictionSource Source { get; }
        public WorldSectorId SectorId { get; }
        public int SolveStepIndex { get; }
        public IReadOnlyList<WorldIntersectorEdgeId> RelatedEdgeIds => relatedEdgeIds;
        public IReadOnlyList<string> RelatedReservationIds => relatedReservationIds;
        public IReadOnlyList<string> RelatedPacingEvidenceIds => relatedPacingEvidenceIds;
        public IReadOnlyList<string> RelatedCandidateIds => relatedCandidateIds;
        public IReadOnlyList<string> RetryLabels => retryLabels;
        public bool RetryableWithinScope { get; }
        public bool RequiresUpstreamOwnerRepair { get; }

        public int CompareTo(WorldContradictionEvidence other)
        {
            if (other == null) return -1;
            var comparison = SolveStepIndex.CompareTo(other.SolveStepIndex);
            if (comparison != 0) return comparison;
            comparison = ((int)Source).CompareTo((int)other.Source);
            if (comparison != 0) return comparison;
            comparison = SectorId.CompareTo(other.SectorId);
            if (comparison != 0) return comparison;
            return string.Compare(StableContradictionId, other.StableContradictionId,
                StringComparison.Ordinal);
        }

        private static ReadOnlyCollection<string> Freeze(IEnumerable<string> source) =>
            new ReadOnlyCollection<string>((source ?? Array.Empty<string>())
                .Select(value => value ?? string.Empty)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray());
    }

    public sealed class WorldFailureReport
    {
        private readonly ReadOnlyCollection<WorldContradictionEvidence> observations;
        private readonly ReadOnlyCollection<WorldIntersectorEdgeId> relatedEdgeIds;
        private readonly ReadOnlyCollection<string> relatedReservationIds;
        private readonly ReadOnlyCollection<string> relatedPacingEvidenceIds;
        private readonly ReadOnlyCollection<string> relatedCandidateIds;
        private readonly ReadOnlyCollection<string> retryLabels;

        internal WorldFailureReport(
            IEnumerable<WorldContradictionEvidence> sourceObservations,
            WorldContradictionEvidence firstContradiction)
        {
            observations = new ReadOnlyCollection<WorldContradictionEvidence>((sourceObservations ??
                Array.Empty<WorldContradictionEvidence>()).Where(value => value != null).OrderBy(value => value).ToArray());
            FirstContradiction = firstContradiction;
            relatedEdgeIds = new ReadOnlyCollection<WorldIntersectorEdgeId>(observations
                .SelectMany(value => value.RelatedEdgeIds).Distinct().OrderBy(value => value).ToArray());
            relatedReservationIds = Freeze(observations.SelectMany(value => value.RelatedReservationIds));
            relatedPacingEvidenceIds = Freeze(observations.SelectMany(value => value.RelatedPacingEvidenceIds));
            relatedCandidateIds = Freeze(observations.SelectMany(value => value.RelatedCandidateIds));
            retryLabels = Freeze(observations.SelectMany(value => value.RetryLabels));
        }

        public IReadOnlyList<WorldContradictionEvidence> Observations => observations;
        public WorldContradictionEvidence FirstContradiction { get; }
        public bool HasFirstContradiction => FirstContradiction != null;
        public IReadOnlyList<WorldIntersectorEdgeId> RelatedEdgeIds => relatedEdgeIds;
        public IReadOnlyList<string> RelatedReservationIds => relatedReservationIds;
        public IReadOnlyList<string> RelatedPacingEvidenceIds => relatedPacingEvidenceIds;
        public IReadOnlyList<string> RelatedCandidateIds => relatedCandidateIds;
        public IReadOnlyList<string> RetryLabels => retryLabels;

        private static ReadOnlyCollection<string> Freeze(IEnumerable<string> source) =>
            new ReadOnlyCollection<string>((source ?? Array.Empty<string>())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray());
    }

    public sealed class WorldRollbackDecision
    {
        internal WorldRollbackDecision(
            WorldRollbackDecisionKind kind,
            string reason,
            int retryAttemptCount,
            int retryCap)
        {
            Kind = kind;
            Reason = reason ?? string.Empty;
            RetryAttemptCount = retryAttemptCount;
            RetryCap = retryCap;
        }

        public WorldRollbackDecisionKind Kind { get; }
        public string Reason { get; }
        public int RetryAttemptCount { get; }
        public int RetryCap { get; }
        public bool IsBoundedRetry => Kind == WorldRollbackDecisionKind.BoundedRetry;
    }

    public sealed class WorldRollbackPolicyRequest
    {
        private readonly ReadOnlyCollection<WorldContradictionEvidence> observations;
        private readonly ReadOnlyCollection<string> publicRetryLabels;

        public WorldRollbackPolicyRequest(
            WorldSolveOrderResult solveOrder,
            WorldIntersectorEdgePlan intersectorPlan,
            WorldMultiSectorReservationPlan reservationPlan,
            WorldPacingDensityPlan pacingDensityPlan,
            WorldSectorId failedSectorId,
            IEnumerable<WorldContradictionEvidence> sourceObservations,
            string map14DebugRetryIdentity,
            IEnumerable<string> sourcePublicRetryLabels,
            int retryAttemptCount,
            int retryCap,
            string publicationLabel,
            int wholeWorldRerandomCount = 0,
            int fallbackCarveCount = 0,
            int silentWideningCount = 0,
            int newRngDrawCount = 0,
            int sectorRerenderCount = 0,
            int generatedFileWriteCount = 0,
            int tilemapMutationCount = 0,
            int sceneMutationCount = 0,
            int prefabMutationCount = 0,
            int gameObjectMutationCount = 0,
            int gameplaySpawnCount = 0,
            int authoringMutationCount = 0,
            int worldPlanMutationCount = 0,
            int intersectorPlanMutationCount = 0,
            int reservationPlanMutationCount = 0,
            int pacingDensityPlanMutationCount = 0)
        {
            SolveOrder = solveOrder;
            IntersectorPlan = intersectorPlan;
            ReservationPlan = reservationPlan;
            PacingDensityPlan = pacingDensityPlan;
            FailedSectorId = failedSectorId;
            var rawObservations = (sourceObservations ?? Array.Empty<WorldContradictionEvidence>()).ToArray();
            NullObservationCount = rawObservations.Count(value => value == null);
            observations = new ReadOnlyCollection<WorldContradictionEvidence>(rawObservations
                .Where(value => value != null).OrderBy(value => value).ToArray());
            publicRetryLabels = new ReadOnlyCollection<string>((sourcePublicRetryLabels ?? Array.Empty<string>())
                .Select(value => value ?? string.Empty).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
            Map14DebugRetryIdentity = map14DebugRetryIdentity ?? string.Empty;
            RetryAttemptCount = retryAttemptCount;
            RetryCap = retryCap;
            PublicationLabel = publicationLabel ?? string.Empty;
            WholeWorldRerandomCount = wholeWorldRerandomCount;
            FallbackCarveCount = fallbackCarveCount;
            SilentWideningCount = silentWideningCount;
            NewRngDrawCount = newRngDrawCount;
            SectorRerenderCount = sectorRerenderCount;
            GeneratedFileWriteCount = generatedFileWriteCount;
            TilemapMutationCount = tilemapMutationCount;
            SceneMutationCount = sceneMutationCount;
            PrefabMutationCount = prefabMutationCount;
            GameObjectMutationCount = gameObjectMutationCount;
            GameplaySpawnCount = gameplaySpawnCount;
            AuthoringMutationCount = authoringMutationCount;
            WorldPlanMutationCount = worldPlanMutationCount;
            IntersectorPlanMutationCount = intersectorPlanMutationCount;
            ReservationPlanMutationCount = reservationPlanMutationCount;
            PacingDensityPlanMutationCount = pacingDensityPlanMutationCount;
            CanonicalDigest = WorldNeighborRollbackDigest.ComputeInput(this);
        }

        public WorldSolveOrderResult SolveOrder { get; }
        public WorldIntersectorEdgePlan IntersectorPlan { get; }
        public WorldMultiSectorReservationPlan ReservationPlan { get; }
        public WorldPacingDensityPlan PacingDensityPlan { get; }
        public WorldSectorId FailedSectorId { get; }
        public IReadOnlyList<WorldContradictionEvidence> Observations => observations;
        public int NullObservationCount { get; }
        public string Map14DebugRetryIdentity { get; }
        public IReadOnlyList<string> PublicRetryLabels => publicRetryLabels;
        public int RetryAttemptCount { get; }
        public int RetryCap { get; }
        public string PublicationLabel { get; }
        public int WholeWorldRerandomCount { get; }
        public int FallbackCarveCount { get; }
        public int SilentWideningCount { get; }
        public int NewRngDrawCount { get; }
        public int SectorRerenderCount { get; }
        public int GeneratedFileWriteCount { get; }
        public int TilemapMutationCount { get; }
        public int SceneMutationCount { get; }
        public int PrefabMutationCount { get; }
        public int GameObjectMutationCount { get; }
        public int GameplaySpawnCount { get; }
        public int AuthoringMutationCount { get; }
        public int WorldPlanMutationCount { get; }
        public int IntersectorPlanMutationCount { get; }
        public int ReservationPlanMutationCount { get; }
        public int PacingDensityPlanMutationCount { get; }
        public string CanonicalDigest { get; }
    }

    public sealed class WorldNeighborRollbackPlan
    {
        internal WorldNeighborRollbackPlan(
            WorldRollbackPolicyRequest request,
            WorldRollbackScope scope,
            WorldFailureReport failureReport,
            WorldRollbackDecision decision,
            string outputDigest)
        {
            Request = request;
            Scope = scope;
            FailureReport = failureReport;
            Decision = decision;
            OutputDigest = outputDigest ?? string.Empty;
        }

        public const int WorldSectorCount = WorldPlanInput.SectorCount;
        public const int InternalEdgeCount = WorldIntersectorEdgePlan.InternalEdgeCount;
        public const int ScopeRadius = WorldRollbackScope.Radius;
        public const int MaximumScopeSectorCount = WorldRollbackScope.MaximumSectorCount;
        public const string DownstreamOwner = "MAP15_06_EXPORT_OVERLAY_AND_BATCH_TEST_WORLD_PLANS";
        public const bool OpensDownstreamTask = false;

        public WorldRollbackPolicyRequest Request { get; }
        public WorldRollbackScope Scope { get; }
        public WorldFailureReport FailureReport { get; }
        public WorldRollbackDecision Decision { get; }
        public int ObservedWorldSectorCount => Request.SolveOrder.Input.Nodes.Count;
        public int ObservedInternalEdgeCount => Request.IntersectorPlan.Edges.Count;
        public string WorldSolveOrderDigest => Request.SolveOrder.OutputDigest;
        public string IntersectorPlanDigest => Request.IntersectorPlan.OutputDigest;
        public string ReservationPlanIdentity => Request.ReservationPlan.OutputDigest;
        public string PacingDensityPlanIdentity => Request.PacingDensityPlan.OutputDigest;
        public string Map14DebugRetryIdentity => Request.Map14DebugRetryIdentity;
        public string InputDigest => Request.CanonicalDigest;
        public string OutputDigest { get; }
        public int WholeWorldRerandomCount => Request.WholeWorldRerandomCount;
        public int FallbackCarveCount => Request.FallbackCarveCount;
        public int SilentWideningCount => Request.SilentWideningCount;
        public int NewRngDrawCount => Request.NewRngDrawCount;
        public int SectorRerenderCount => Request.SectorRerenderCount;
        public int GeneratedFileWriteCount => Request.GeneratedFileWriteCount;
        public int TilemapMutationCount => Request.TilemapMutationCount;
        public int SceneMutationCount => Request.SceneMutationCount;
        public int PrefabMutationCount => Request.PrefabMutationCount;
        public int GameObjectMutationCount => Request.GameObjectMutationCount;
        public int GameplaySpawnCount => Request.GameplaySpawnCount;
        public int AuthoringMutationCount => Request.AuthoringMutationCount;
        public int WorldPlanMutationCount => Request.WorldPlanMutationCount;
        public int IntersectorPlanMutationCount => Request.IntersectorPlanMutationCount;
        public int ReservationPlanMutationCount => Request.ReservationPlanMutationCount;
        public int PacingDensityPlanMutationCount => Request.PacingDensityPlanMutationCount;
    }

    public sealed class WorldNeighborRollbackFailure :
        IComparable<WorldNeighborRollbackFailure>, IEquatable<WorldNeighborRollbackFailure>
    {
        public WorldNeighborRollbackFailure(WorldNeighborRollbackFailureCode code, string subject, string reason)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public WorldNeighborRollbackFailureCode Code { get; }
        public string Subject { get; }
        public string Reason { get; }

        public int CompareTo(WorldNeighborRollbackFailure other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0 ? comparison : string.Compare(Reason, other.Reason, StringComparison.Ordinal);
        }

        public bool Equals(WorldNeighborRollbackFailure other) => other != null && CompareTo(other) == 0;
        public override bool Equals(object obj) => Equals(obj as WorldNeighborRollbackFailure);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(ToString());
        public override string ToString() => Code + "|" + Subject + "|" + Reason;
    }

    public sealed class WorldNeighborRollbackResult
    {
        private readonly ReadOnlyCollection<WorldNeighborRollbackFailure> failures;

        private WorldNeighborRollbackResult(
            WorldNeighborRollbackPlan plan,
            IEnumerable<WorldNeighborRollbackFailure> sourceFailures)
        {
            Plan = plan;
            failures = new ReadOnlyCollection<WorldNeighborRollbackFailure>((sourceFailures ??
                Array.Empty<WorldNeighborRollbackFailure>()).Where(value => value != null)
                .Distinct().OrderBy(value => value).ToArray());
        }

        public bool Success => Plan != null && failures.Count == 0;
        public WorldNeighborRollbackPlan Plan { get; }
        public IReadOnlyList<WorldNeighborRollbackFailure> Failures => failures;
        public string InputDigest => Plan == null ? string.Empty : Plan.InputDigest;
        public string OutputDigest => Plan == null ? string.Empty : Plan.OutputDigest;

        internal static WorldNeighborRollbackResult Pass(WorldNeighborRollbackPlan plan) =>
            new WorldNeighborRollbackResult(plan, Array.Empty<WorldNeighborRollbackFailure>());

        internal static WorldNeighborRollbackResult Fail(IEnumerable<WorldNeighborRollbackFailure> sourceFailures) =>
            new WorldNeighborRollbackResult(null, sourceFailures);
    }

    public static class WorldNeighborRollbackDigest
    {
        public static string ComputeInput(WorldRollbackPolicyRequest request)
        {
            if (request == null) return string.Empty;
            var lines = new List<string>
            {
                "WORLD_SOLVE_INPUT|" + Token(request.SolveOrder == null ? string.Empty : request.SolveOrder.InputDigest),
                "WORLD_SOLVE_OUTPUT|" + Token(request.SolveOrder == null ? string.Empty : request.SolveOrder.OutputDigest),
                "INTERSECTOR|" + Token(request.IntersectorPlan == null ? string.Empty : request.IntersectorPlan.OutputDigest),
                "RESERVATION|" + Token(request.ReservationPlan == null ? string.Empty : request.ReservationPlan.OutputDigest),
                "PACING|" + Token(request.PacingDensityPlan == null ? string.Empty : request.PacingDensityPlan.OutputDigest),
                "MAP14_RETRY|" + Token(request.Map14DebugRetryIdentity),
                "FAILED|" + request.FailedSectorId,
                "RETRY|" + Number(request.RetryAttemptCount) + "|" + Number(request.RetryCap),
                "PUBLICATION|" + Token(request.PublicationLabel),
                "FORBIDDEN|" + JoinNumbers(request.WholeWorldRerandomCount, request.FallbackCarveCount,
                    request.SilentWideningCount, request.NewRngDrawCount, request.SectorRerenderCount),
                "MUTATION|" + JoinNumbers(request.GeneratedFileWriteCount, request.TilemapMutationCount,
                    request.SceneMutationCount, request.PrefabMutationCount, request.GameObjectMutationCount,
                    request.GameplaySpawnCount, request.AuthoringMutationCount, request.WorldPlanMutationCount,
                    request.IntersectorPlanMutationCount, request.ReservationPlanMutationCount,
                    request.PacingDensityPlanMutationCount),
                "NULL_OBSERVATIONS|" + Number(request.NullObservationCount),
            };
            lines.AddRange(request.PublicRetryLabels.Select(value => "PUBLIC_RETRY|" + Token(value)));
            lines.AddRange(request.Observations.OrderBy(value => value).Select(CanonicalEvidence));
            return HashCanonicalText(string.Join("\n", lines));
        }

        public static string ComputeOutput(
            WorldRollbackPolicyRequest request,
            WorldRollbackScope scope,
            WorldFailureReport report,
            WorldRollbackDecision decision)
        {
            if (request == null || scope == null || report == null || decision == null) return string.Empty;
            var lines = new List<string>
            {
                "INPUT|" + request.CanonicalDigest,
                "SCOPE|" + scope.Kind + "|" + scope.FailedSectorId + "|" + scope.FailedCoordinate + "|" +
                    Number(WorldRollbackScope.Radius) + "|" + Number(scope.SectorCount),
            };
            lines.AddRange(scope.Sectors.Select(value => string.Join("|", new[]
            {
                "SCOPE_SECTOR", value.SectorId.ToString(), value.Coordinate.ToString(),
                Number(value.SolveStepIndex), value.IsFailedSector ? "FAILED" : "NEIGHBOR",
            })));
            lines.Add("FIRST|" + (report.FirstContradiction == null
                ? "NONE"
                : CanonicalEvidence(report.FirstContradiction)));
            lines.AddRange(report.Observations.Select(value => "REPORT_" + CanonicalEvidence(value)));
            lines.Add("DECISION|" + decision.Kind + "|" + Token(decision.Reason) + "|" +
                Number(decision.RetryAttemptCount) + "|" + Number(decision.RetryCap));
            lines.Add("COUNTERS|" + JoinNumbers(request.WholeWorldRerandomCount, request.FallbackCarveCount,
                request.SilentWideningCount, request.NewRngDrawCount, request.SectorRerenderCount,
                request.GeneratedFileWriteCount, request.TilemapMutationCount, request.SceneMutationCount,
                request.PrefabMutationCount, request.GameObjectMutationCount, request.GameplaySpawnCount,
                request.AuthoringMutationCount, request.WorldPlanMutationCount, request.IntersectorPlanMutationCount,
                request.ReservationPlanMutationCount, request.PacingDensityPlanMutationCount));
            return HashCanonicalText(string.Join("\n", lines));
        }

        public static string HashCanonicalText(string value)
        {
            var canonical = (value ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            using (var sha = SHA256.Create())
            {
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(canonical))
                    .Select(valueByte => valueByte.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        internal static string CanonicalEvidence(WorldContradictionEvidence value)
        {
            if (value == null) return "EVIDENCE|NULL";
            return string.Join("|", new[]
            {
                "EVIDENCE", Number(value.SolveStepIndex), value.Source.ToString(),
                value.SectorId.ToString(), Token(value.StableContradictionId), value.Kind.ToString(),
                value.RetryableWithinScope ? "RETRYABLE" : "NOT_RETRYABLE",
                value.RequiresUpstreamOwnerRepair ? "OWNER_REPAIR" : "LOCAL_OWNER",
                "EDGES=" + string.Join(",", value.RelatedEdgeIds.Select(item => item.ToString())),
                "RESERVATIONS=" + string.Join(",", value.RelatedReservationIds.Select(Token)),
                "PACING=" + string.Join(",", value.RelatedPacingEvidenceIds.Select(Token)),
                "CANDIDATES=" + string.Join(",", value.RelatedCandidateIds.Select(Token)),
                "RETRIES=" + string.Join(",", value.RetryLabels.Select(Token)),
            });
        }

        private static string Token(string value) => (value ?? string.Empty)
            .Replace("%", "%25").Replace("|", "%7C").Replace("\n", "%0A").Replace("\r", "%0D");

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static string JoinNumbers(params int[] values) =>
            string.Join("|", values.Select(Number));
    }
}
