using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum WorldPacingWindowKind
    {
        Quiet,
        Cluster,
        Activity,
        Event,
        Landmark,
    }

    public enum WorldDensityBudgetKind
    {
        AbstractSolidReachable,
    }

    public enum WorldDensityBudgetVerdict
    {
        WithinRange,
        Warning,
        Violation,
    }

    public enum WorldContentSignatureKind
    {
        Pattern,
        Cluster,
        Activity,
    }

    public enum WorldPacingDensityViolationType
    {
        WindowUnderfilled,
        WindowOverfilled,
        DensityBelowMinimum,
        DensityAboveMaximum,
        ReachableBudgetBelowMinimum,
        ReachableBudgetAboveMaximum,
        RecentPatternRepeat,
        RecentClusterRepeat,
        RecentActivityRepeat,
        ActivityCapExceeded,
        EventCapExceeded,
    }

    public enum WorldPacingDensityFailureCode
    {
        MissingInput,
        UpstreamFailure,
        WorldSectorCountMismatch,
        InternalEdgeCountMismatch,
        UpstreamDigestMismatch,
        InvalidDigest,
        MissingWindowKind,
        DuplicateWindowId,
        InvalidWindow,
        MissingSector,
        DuplicateBudgetSector,
        MissingBudgetSector,
        InvalidBudget,
        InvalidSignature,
        DuplicateRecentUseRule,
        MissingRecentUseRule,
        InvalidRecentUseRule,
        ActivityEventAuthorityContradiction,
        MutationClaim,
    }

    public sealed class WorldPacingWindow : IComparable<WorldPacingWindow>
    {
        private readonly ReadOnlyCollection<WorldSectorId> sectorIds;

        public WorldPacingWindow(
            string windowId,
            WorldPacingWindowKind kind,
            IEnumerable<WorldSectorId> sectors,
            int firstSolveStep,
            int lastSolveStep,
            int minimumCount,
            int maximumCount,
            int observedCount,
            string reason,
            string sourceOwner)
        {
            WindowId = windowId ?? string.Empty;
            Kind = kind;
            sectorIds = new ReadOnlyCollection<WorldSectorId>((sectors ?? Array.Empty<WorldSectorId>())
                .Distinct().OrderBy(value => value).ToArray());
            FirstSolveStep = firstSolveStep;
            LastSolveStep = lastSolveStep;
            MinimumCount = minimumCount;
            MaximumCount = maximumCount;
            ObservedCount = observedCount;
            Reason = reason ?? string.Empty;
            SourceOwner = sourceOwner ?? string.Empty;
        }

        public string WindowId { get; }
        public WorldPacingWindowKind Kind { get; }
        public IReadOnlyList<WorldSectorId> SectorIds => sectorIds;
        public int FirstSolveStep { get; }
        public int LastSolveStep { get; }
        public int MinimumCount { get; }
        public int MaximumCount { get; }
        public int ObservedCount { get; }
        public string Reason { get; }
        public string SourceOwner { get; }
        public bool CountWithinRange => ObservedCount >= MinimumCount && ObservedCount <= MaximumCount;

        public int CompareTo(WorldPacingWindow other)
        {
            if (other == null) return -1;
            var comparison = Kind.CompareTo(other.Kind);
            if (comparison != 0) return comparison;
            comparison = FirstSolveStep.CompareTo(other.FirstSolveStep);
            return comparison != 0
                ? comparison
                : string.Compare(WindowId, other.WindowId, StringComparison.Ordinal);
        }
    }

    public sealed class WorldSectorDensityBudget : IComparable<WorldSectorDensityBudget>
    {
        public WorldSectorDensityBudget(
            WorldSectorId sectorId,
            WorldDensityBudgetKind kind,
            int minimumSolidBudget,
            int maximumSolidBudget,
            int observedSolidBudget,
            int minimumReachableBudget,
            int maximumReachableBudget,
            int observedReachableBudget,
            string reason,
            string sourceOwner)
        {
            SectorId = sectorId;
            Kind = kind;
            MinimumSolidBudget = minimumSolidBudget;
            MaximumSolidBudget = maximumSolidBudget;
            ObservedSolidBudget = observedSolidBudget;
            MinimumReachableBudget = minimumReachableBudget;
            MaximumReachableBudget = maximumReachableBudget;
            ObservedReachableBudget = observedReachableBudget;
            Reason = reason ?? string.Empty;
            SourceOwner = sourceOwner ?? string.Empty;
        }

        public WorldSectorId SectorId { get; }
        public WorldDensityBudgetKind Kind { get; }
        public int MinimumSolidBudget { get; }
        public int MaximumSolidBudget { get; }
        public int ObservedSolidBudget { get; }
        public int MinimumReachableBudget { get; }
        public int MaximumReachableBudget { get; }
        public int ObservedReachableBudget { get; }
        public string Reason { get; }
        public string SourceOwner { get; }
        public bool SolidWithinRange => ObservedSolidBudget >= MinimumSolidBudget &&
                                        ObservedSolidBudget <= MaximumSolidBudget;
        public bool ReachableWithinRange => ObservedReachableBudget >= MinimumReachableBudget &&
                                            ObservedReachableBudget <= MaximumReachableBudget;
        public WorldDensityBudgetVerdict Verdict
        {
            get
            {
                if (!SolidWithinRange || !ReachableWithinRange) return WorldDensityBudgetVerdict.Violation;
                return ObservedSolidBudget == MinimumSolidBudget || ObservedSolidBudget == MaximumSolidBudget ||
                       ObservedReachableBudget == MinimumReachableBudget ||
                       ObservedReachableBudget == MaximumReachableBudget
                    ? WorldDensityBudgetVerdict.Warning
                    : WorldDensityBudgetVerdict.WithinRange;
            }
        }

        public int CompareTo(WorldSectorDensityBudget other)
        {
            if (other == null) return -1;
            var comparison = SectorId.CompareTo(other.SectorId);
            return comparison != 0 ? comparison : Kind.CompareTo(other.Kind);
        }
    }

    public sealed class WorldContentSignature : IComparable<WorldContentSignature>
    {
        public WorldContentSignature(
            WorldContentSignatureKind kind,
            string signatureId,
            WorldSectorId sectorId,
            int solveStep,
            string sourceOwner)
        {
            Kind = kind;
            SignatureId = signatureId ?? string.Empty;
            SectorId = sectorId;
            SolveStep = solveStep;
            SourceOwner = sourceOwner ?? string.Empty;
        }

        public WorldContentSignatureKind Kind { get; }
        public string SignatureId { get; }
        public WorldSectorId SectorId { get; }
        public int SolveStep { get; }
        public string SourceOwner { get; }

        public int CompareTo(WorldContentSignature other)
        {
            if (other == null) return -1;
            var comparison = Kind.CompareTo(other.Kind);
            if (comparison != 0) return comparison;
            comparison = SectorId.CompareTo(other.SectorId);
            if (comparison != 0) return comparison;
            comparison = string.Compare(SignatureId, other.SignatureId, StringComparison.Ordinal);
            return comparison != 0 ? comparison : SolveStep.CompareTo(other.SolveStep);
        }
    }

    public sealed class WorldRecentUseRule : IComparable<WorldRecentUseRule>
    {
        public WorldRecentUseRule(
            WorldContentSignatureKind kind,
            int minimumSectorDistance,
            int minimumSolveStepDistance,
            bool requireGraphDistance,
            string reason,
            string sourceOwner)
        {
            Kind = kind;
            MinimumSectorDistance = minimumSectorDistance;
            MinimumSolveStepDistance = minimumSolveStepDistance;
            RequireGraphDistance = requireGraphDistance;
            Reason = reason ?? string.Empty;
            SourceOwner = sourceOwner ?? string.Empty;
        }

        public WorldContentSignatureKind Kind { get; }
        public int MinimumSectorDistance { get; }
        public int MinimumSolveStepDistance { get; }
        public bool RequireGraphDistance { get; }
        public string Reason { get; }
        public string SourceOwner { get; }

        public int CompareTo(WorldRecentUseRule other)
        {
            if (other == null) return -1;
            var comparison = Kind.CompareTo(other.Kind);
            return comparison != 0
                ? comparison
                : string.Compare(Reason, other.Reason, StringComparison.Ordinal);
        }
    }

    public sealed class WorldRecentUseObservation : IComparable<WorldRecentUseObservation>
    {
        public WorldRecentUseObservation(
            string observationId,
            WorldContentSignatureKind kind,
            string earlierSignatureId,
            string laterSignatureId,
            WorldSectorId earlierSectorId,
            WorldSectorId laterSectorId,
            int earlierSolveStep,
            int laterSolveStep,
            int graphDistance,
            bool graphDistanceAvailable,
            int solveStepDistance,
            bool accepted,
            string violationReason)
        {
            ObservationId = observationId ?? string.Empty;
            Kind = kind;
            EarlierSignatureId = earlierSignatureId ?? string.Empty;
            LaterSignatureId = laterSignatureId ?? string.Empty;
            EarlierSectorId = earlierSectorId;
            LaterSectorId = laterSectorId;
            EarlierSolveStep = earlierSolveStep;
            LaterSolveStep = laterSolveStep;
            GraphDistance = graphDistance;
            GraphDistanceAvailable = graphDistanceAvailable;
            SolveStepDistance = solveStepDistance;
            Accepted = accepted;
            ViolationReason = violationReason ?? string.Empty;
        }

        public string ObservationId { get; }
        public WorldContentSignatureKind Kind { get; }
        public string EarlierSignatureId { get; }
        public string LaterSignatureId { get; }
        public WorldSectorId EarlierSectorId { get; }
        public WorldSectorId LaterSectorId { get; }
        public int EarlierSolveStep { get; }
        public int LaterSolveStep { get; }
        public int GraphDistance { get; }
        public bool GraphDistanceAvailable { get; }
        public int SolveStepDistance { get; }
        public bool Accepted { get; }
        public string ViolationReason { get; }

        public int CompareTo(WorldRecentUseObservation other)
        {
            if (other == null) return -1;
            var comparison = Kind.CompareTo(other.Kind);
            if (comparison != 0) return comparison;
            comparison = EarlierSectorId.CompareTo(other.EarlierSectorId);
            if (comparison != 0) return comparison;
            comparison = LaterSectorId.CompareTo(other.LaterSectorId);
            return comparison != 0
                ? comparison
                : string.Compare(ObservationId, other.ObservationId, StringComparison.Ordinal);
        }
    }

    public sealed class WorldActivityEventConstraint : IComparable<WorldActivityEventConstraint>
    {
        public WorldActivityEventConstraint(
            string constraintId,
            WorldPacingWindowKind kind,
            int targetPermille,
            int maximumCount,
            string authorityDigest,
            string sourceOwner)
        {
            ConstraintId = constraintId ?? string.Empty;
            Kind = kind;
            TargetPermille = targetPermille;
            MaximumCount = maximumCount;
            AuthorityDigest = authorityDigest ?? string.Empty;
            SourceOwner = sourceOwner ?? string.Empty;
        }

        public string ConstraintId { get; }
        public WorldPacingWindowKind Kind { get; }
        public int TargetPermille { get; }
        public int MaximumCount { get; }
        public string AuthorityDigest { get; }
        public string SourceOwner { get; }

        public int CompareTo(WorldActivityEventConstraint other)
        {
            if (other == null) return -1;
            var comparison = Kind.CompareTo(other.Kind);
            return comparison != 0
                ? comparison
                : string.Compare(ConstraintId, other.ConstraintId, StringComparison.Ordinal);
        }
    }

    public sealed class WorldPacingDensityViolation : IComparable<WorldPacingDensityViolation>
    {
        public WorldPacingDensityViolation(
            WorldPacingDensityViolationType violationType,
            string subject,
            WorldSectorId? sectorId,
            string signatureId,
            string reason)
        {
            ViolationType = violationType;
            Subject = subject ?? string.Empty;
            SectorId = sectorId;
            SignatureId = signatureId ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public WorldPacingDensityViolationType ViolationType { get; }
        public string Subject { get; }
        public WorldSectorId? SectorId { get; }
        public string SignatureId { get; }
        public string Reason { get; }

        public int CompareTo(WorldPacingDensityViolation other)
        {
            if (other == null) return -1;
            var comparison = ViolationType.CompareTo(other.ViolationType);
            if (comparison != 0) return comparison;
            comparison = CompareSector(SectorId, other.SectorId);
            if (comparison != 0) return comparison;
            comparison = string.Compare(SignatureId, other.SignatureId, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Subject, other.Subject, StringComparison.Ordinal);
        }

        private static int CompareSector(WorldSectorId? left, WorldSectorId? right)
        {
            if (!left.HasValue) return right.HasValue ? -1 : 0;
            return right.HasValue ? left.Value.CompareTo(right.Value) : 1;
        }
    }

    public sealed class WorldPacingDensityRequest
    {
        private readonly ReadOnlyCollection<WorldPacingWindow> windows;
        private readonly ReadOnlyCollection<WorldSectorDensityBudget> budgets;
        private readonly ReadOnlyCollection<WorldContentSignature> signatures;
        private readonly ReadOnlyCollection<WorldRecentUseRule> recentUseRules;
        private readonly ReadOnlyCollection<WorldActivityEventConstraint> activityEventConstraints;

        public WorldPacingDensityRequest(
            WorldPlanInput worldPlan,
            WorldSolveOrderResult solveOrder,
            WorldIntersectorEdgePlan intersectorPlan,
            WorldMultiSectorReservationPlan reservationPlan,
            IEnumerable<WorldPacingWindow> sourceWindows,
            IEnumerable<WorldSectorDensityBudget> sourceBudgets,
            IEnumerable<WorldContentSignature> sourceSignatures,
            IEnumerable<WorldRecentUseRule> sourceRecentUseRules,
            IEnumerable<WorldActivityEventConstraint> sourceActivityEventConstraints,
            string map10IdentityDigest,
            string map11IdentityDigest,
            string map12IdentityDigest,
            string map13IdentityDigest,
            string map14HandoffDigest,
            string publicationLabel,
            int newRngDrawCount = 0,
            int fallbackCarveCount = 0,
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
            int reservationPlanMutationCount = 0)
        {
            WorldPlan = worldPlan;
            SolveOrder = solveOrder;
            IntersectorPlan = intersectorPlan;
            ReservationPlan = reservationPlan;
            windows = Freeze(sourceWindows);
            budgets = Freeze(sourceBudgets);
            signatures = Freeze(sourceSignatures);
            recentUseRules = Freeze(sourceRecentUseRules);
            activityEventConstraints = Freeze(sourceActivityEventConstraints);
            Map10IdentityDigest = map10IdentityDigest ?? string.Empty;
            Map11IdentityDigest = map11IdentityDigest ?? string.Empty;
            Map12IdentityDigest = map12IdentityDigest ?? string.Empty;
            Map13IdentityDigest = map13IdentityDigest ?? string.Empty;
            Map14HandoffDigest = map14HandoffDigest ?? string.Empty;
            PublicationLabel = publicationLabel ?? string.Empty;
            NewRngDrawCount = newRngDrawCount;
            FallbackCarveCount = fallbackCarveCount;
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
            CanonicalDigest = WorldPacingDensityDigest.ComputeInput(this);
        }

        public WorldPlanInput WorldPlan { get; }
        public WorldSolveOrderResult SolveOrder { get; }
        public WorldIntersectorEdgePlan IntersectorPlan { get; }
        public WorldMultiSectorReservationPlan ReservationPlan { get; }
        public IReadOnlyList<WorldPacingWindow> Windows => windows;
        public IReadOnlyList<WorldSectorDensityBudget> Budgets => budgets;
        public IReadOnlyList<WorldContentSignature> Signatures => signatures;
        public IReadOnlyList<WorldRecentUseRule> RecentUseRules => recentUseRules;
        public IReadOnlyList<WorldActivityEventConstraint> ActivityEventConstraints => activityEventConstraints;
        public string Map10IdentityDigest { get; }
        public string Map11IdentityDigest { get; }
        public string Map12IdentityDigest { get; }
        public string Map13IdentityDigest { get; }
        public string Map14HandoffDigest { get; }
        public string PublicationLabel { get; }
        public int NewRngDrawCount { get; }
        public int FallbackCarveCount { get; }
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
        public string CanonicalDigest { get; }

        private static ReadOnlyCollection<T> Freeze<T>(IEnumerable<T> source) where T : IComparable<T> =>
            new ReadOnlyCollection<T>((source ?? Array.Empty<T>())
                .Where(value => value != null).OrderBy(value => value).ToArray());
    }

    public sealed class WorldPacingDensityPlan
    {
        private readonly ReadOnlyCollection<WorldPacingWindow> windows;
        private readonly ReadOnlyCollection<WorldSectorDensityBudget> budgets;
        private readonly ReadOnlyCollection<WorldContentSignature> signatures;
        private readonly ReadOnlyCollection<WorldRecentUseRule> recentUseRules;
        private readonly ReadOnlyCollection<WorldRecentUseObservation> observations;
        private readonly ReadOnlyCollection<WorldPacingDensityViolation> violations;

        internal WorldPacingDensityPlan(
            WorldPacingDensityRequest request,
            IEnumerable<WorldRecentUseObservation> sourceObservations,
            IEnumerable<WorldPacingDensityViolation> sourceViolations,
            string outputDigest)
        {
            Request = request;
            windows = new ReadOnlyCollection<WorldPacingWindow>(request.Windows.OrderBy(value => value).ToArray());
            budgets = new ReadOnlyCollection<WorldSectorDensityBudget>(request.Budgets.OrderBy(value => value).ToArray());
            signatures = new ReadOnlyCollection<WorldContentSignature>(request.Signatures.OrderBy(value => value).ToArray());
            recentUseRules = new ReadOnlyCollection<WorldRecentUseRule>(request.RecentUseRules.OrderBy(value => value).ToArray());
            observations = new ReadOnlyCollection<WorldRecentUseObservation>((sourceObservations ??
                Array.Empty<WorldRecentUseObservation>()).OrderBy(value => value).ToArray());
            violations = new ReadOnlyCollection<WorldPacingDensityViolation>((sourceViolations ??
                Array.Empty<WorldPacingDensityViolation>()).OrderBy(value => value).ToArray());
            OutputDigest = outputDigest ?? string.Empty;
        }

        public const int WorldSectorCount = WorldPlanInput.SectorCount;
        public const int InternalEdgeCount = WorldIntersectorEdgePlan.InternalEdgeCount;
        public const int RequiredWindowKindCount = 5;
        public const int RequiredSignatureKindCount = 3;
        public const int RequiredRecentUseRuleCount = 3;
        public const string DownstreamOwner = "MAP15_05_IMPLEMENT_NEIGHBOR_ROLLBACK_AND_FAILURE_REPORT";
        public const bool OpensDownstreamTask = false;

        public WorldPacingDensityRequest Request { get; }
        public IReadOnlyList<WorldPacingWindow> Windows => windows;
        public IReadOnlyList<WorldSectorDensityBudget> Budgets => budgets;
        public IReadOnlyList<WorldContentSignature> Signatures => signatures;
        public IReadOnlyList<WorldRecentUseRule> RecentUseRules => recentUseRules;
        public IReadOnlyList<WorldRecentUseObservation> Observations => observations;
        public IReadOnlyList<WorldPacingDensityViolation> Violations => violations;
        public int ObservedWorldSectorCount => Request.WorldPlan.Nodes.Count;
        public int ObservedInternalEdgeCount => Request.IntersectorPlan.Edges.Count;
        public bool ReservationPlanObserved => Request.ReservationPlan != null;
        public string ReservationPlanDigest => Request.ReservationPlan == null
            ? string.Empty
            : Request.ReservationPlan.OutputDigest;
        public int CoveredWindowKindCount => windows.Select(value => value.Kind).Distinct().Count();
        public int MissingWindowKindCount => RequiredWindowKindCount - CoveredWindowKindCount;
        public int DensityBudgetSectorCount => budgets.Select(value => value.SectorId).Distinct().Count();
        public int BudgetViolationCount => violations.Count(value =>
            value.ViolationType == WorldPacingDensityViolationType.DensityBelowMinimum ||
            value.ViolationType == WorldPacingDensityViolationType.DensityAboveMaximum ||
            value.ViolationType == WorldPacingDensityViolationType.ReachableBudgetBelowMinimum ||
            value.ViolationType == WorldPacingDensityViolationType.ReachableBudgetAboveMaximum);
        public int CoveredSignatureKindCount => signatures.Select(value => value.Kind).Distinct().Count();
        public int MissingSignatureKindCount => RequiredSignatureKindCount - CoveredSignatureKindCount;
        public int CoveredRecentUseRuleCount => recentUseRules.Select(value => value.Kind).Distinct().Count();
        public int MissingRecentUseRuleCount => RequiredRecentUseRuleCount - CoveredRecentUseRuleCount;
        public int AcceptedRecentUseObservationCount => observations.Count(value => value.Accepted);
        public int RecentUseViolationCount => observations.Count(value => !value.Accepted);
        public int ActivityEventCapViolationCount => violations.Count(value =>
            value.ViolationType == WorldPacingDensityViolationType.ActivityCapExceeded ||
            value.ViolationType == WorldPacingDensityViolationType.EventCapExceeded);
        public string InputDigest => Request.CanonicalDigest;
        public string OutputDigest { get; }
        public int NewRngDrawCount => Request.NewRngDrawCount;
        public int FallbackCarveCount => Request.FallbackCarveCount;
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
    }

    public sealed class WorldPacingDensityFailure :
        IComparable<WorldPacingDensityFailure>, IEquatable<WorldPacingDensityFailure>
    {
        public WorldPacingDensityFailure(WorldPacingDensityFailureCode code, string subject, string reason)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public WorldPacingDensityFailureCode Code { get; }
        public string Subject { get; }
        public string Reason { get; }

        public int CompareTo(WorldPacingDensityFailure other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0 ? comparison : string.Compare(Reason, other.Reason, StringComparison.Ordinal);
        }

        public bool Equals(WorldPacingDensityFailure other) => other != null && CompareTo(other) == 0;
        public override bool Equals(object obj) => Equals(obj as WorldPacingDensityFailure);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(ToString());
        public override string ToString() => Code + "|" + Subject + "|" + Reason;
    }

    public sealed class WorldPacingDensityResult
    {
        private readonly ReadOnlyCollection<WorldPacingDensityFailure> failures;

        private WorldPacingDensityResult(
            WorldPacingDensityPlan plan,
            IEnumerable<WorldPacingDensityFailure> sourceFailures)
        {
            Plan = plan;
            failures = new ReadOnlyCollection<WorldPacingDensityFailure>((sourceFailures ??
                Array.Empty<WorldPacingDensityFailure>()).Distinct().OrderBy(value => value).ToArray());
        }

        public bool Success => Plan != null && failures.Count == 0;
        public WorldPacingDensityPlan Plan { get; }
        public IReadOnlyList<WorldPacingDensityFailure> Failures => failures;
        public string InputDigest => Plan == null ? string.Empty : Plan.InputDigest;
        public string OutputDigest => Plan == null ? string.Empty : Plan.OutputDigest;

        internal static WorldPacingDensityResult Pass(WorldPacingDensityPlan plan) =>
            new WorldPacingDensityResult(plan, Array.Empty<WorldPacingDensityFailure>());

        internal static WorldPacingDensityResult Fail(IEnumerable<WorldPacingDensityFailure> sourceFailures) =>
            new WorldPacingDensityResult(null, sourceFailures);
    }

    public static class WorldPacingDensityDigest
    {
        public static string ComputeInput(WorldPacingDensityRequest request)
        {
            if (request == null) return string.Empty;
            var lines = new List<string>
            {
                "WORLD|" + Digest(request.WorldPlan == null ? string.Empty : request.WorldPlan.CanonicalDigest),
                "SOLVE|" + Digest(request.SolveOrder == null ? string.Empty : request.SolveOrder.OutputDigest),
                "INTERSECTOR|" + Digest(request.IntersectorPlan == null ? string.Empty : request.IntersectorPlan.OutputDigest),
                "RESERVATION|" + Digest(request.ReservationPlan == null ? string.Empty : request.ReservationPlan.OutputDigest),
                "MAP10|" + Digest(request.Map10IdentityDigest),
                "MAP11|" + Digest(request.Map11IdentityDigest),
                "MAP12|" + Digest(request.Map12IdentityDigest),
                "MAP13|" + Digest(request.Map13IdentityDigest),
                "MAP14|" + Digest(request.Map14HandoffDigest),
                "PUBLICATION|" + Token(request.PublicationLabel),
                string.Join("|", new[]
                {
                    "MUTATION", Number(request.NewRngDrawCount), Number(request.FallbackCarveCount),
                    Number(request.SectorRerenderCount), Number(request.GeneratedFileWriteCount),
                    Number(request.TilemapMutationCount), Number(request.SceneMutationCount),
                    Number(request.PrefabMutationCount), Number(request.GameObjectMutationCount),
                    Number(request.GameplaySpawnCount), Number(request.AuthoringMutationCount),
                    Number(request.WorldPlanMutationCount), Number(request.IntersectorPlanMutationCount),
                    Number(request.ReservationPlanMutationCount),
                }),
            };
            lines.AddRange(request.Windows.OrderBy(value => value).Select(Window));
            lines.AddRange(request.Budgets.OrderBy(value => value).Select(Budget));
            lines.AddRange(request.Signatures.OrderBy(value => value).Select(Signature));
            lines.AddRange(request.RecentUseRules.OrderBy(value => value).Select(Rule));
            lines.AddRange(request.ActivityEventConstraints.OrderBy(value => value).Select(Constraint));
            return HashCanonicalText(string.Join("\n", lines));
        }

        public static string ComputeOutput(
            WorldPacingDensityRequest request,
            IEnumerable<WorldRecentUseObservation> observations,
            IEnumerable<WorldPacingDensityViolation> violations)
        {
            var lines = new List<string> { "INPUT|" + (request == null ? string.Empty : request.CanonicalDigest) };
            if (request != null)
            {
                lines.AddRange(request.Windows.OrderBy(value => value).Select(Window));
                lines.AddRange(request.Budgets.OrderBy(value => value).Select(Budget));
                lines.AddRange(request.Signatures.OrderBy(value => value).Select(Signature));
                lines.AddRange(request.RecentUseRules.OrderBy(value => value).Select(Rule));
                lines.AddRange(request.ActivityEventConstraints.OrderBy(value => value).Select(Constraint));
            }
            lines.AddRange((observations ?? Array.Empty<WorldRecentUseObservation>()).OrderBy(value => value)
                .Select(Observation));
            lines.AddRange((violations ?? Array.Empty<WorldPacingDensityViolation>()).OrderBy(value => value)
                .Select(Violation));
            return HashCanonicalText(string.Join("\n", lines));
        }

        public static string HashCanonicalText(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                var result = new StringBuilder(bytes.Length * 2);
                foreach (var item in bytes) result.Append(item.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }

        private static string Window(WorldPacingWindow value) => string.Join("|", new[]
        {
            "WINDOW", value.Kind.ToString(), Token(value.WindowId),
            string.Join(",", value.SectorIds.Select(item => Number(item.Value))),
            Number(value.FirstSolveStep), Number(value.LastSolveStep), Number(value.MinimumCount),
            Number(value.MaximumCount), Number(value.ObservedCount), Token(value.Reason), Token(value.SourceOwner),
        });

        private static string Budget(WorldSectorDensityBudget value) => string.Join("|", new[]
        {
            "BUDGET", Number(value.SectorId.Value), value.Kind.ToString(), Number(value.MinimumSolidBudget),
            Number(value.MaximumSolidBudget), Number(value.ObservedSolidBudget),
            Number(value.MinimumReachableBudget), Number(value.MaximumReachableBudget),
            Number(value.ObservedReachableBudget), value.Verdict.ToString(), Token(value.Reason),
            Token(value.SourceOwner),
        });

        private static string Signature(WorldContentSignature value) => string.Join("|", new[]
        {
            "SIGNATURE", value.Kind.ToString(), Token(value.SignatureId), Number(value.SectorId.Value),
            Number(value.SolveStep), Token(value.SourceOwner),
        });

        private static string Rule(WorldRecentUseRule value) => string.Join("|", new[]
        {
            "RULE", value.Kind.ToString(), Number(value.MinimumSectorDistance),
            Number(value.MinimumSolveStepDistance), Bool(value.RequireGraphDistance), Token(value.Reason),
            Token(value.SourceOwner),
        });

        private static string Constraint(WorldActivityEventConstraint value) => string.Join("|", new[]
        {
            "CAP", value.Kind.ToString(), Token(value.ConstraintId), Number(value.TargetPermille),
            Number(value.MaximumCount), Digest(value.AuthorityDigest), Token(value.SourceOwner),
        });

        private static string Observation(WorldRecentUseObservation value) => string.Join("|", new[]
        {
            "OBSERVATION", value.Kind.ToString(), Token(value.ObservationId), Token(value.EarlierSignatureId),
            Token(value.LaterSignatureId), Number(value.EarlierSectorId.Value), Number(value.LaterSectorId.Value),
            Number(value.EarlierSolveStep), Number(value.LaterSolveStep), Number(value.GraphDistance),
            Bool(value.GraphDistanceAvailable), Number(value.SolveStepDistance), Bool(value.Accepted),
            Token(value.ViolationReason),
        });

        private static string Violation(WorldPacingDensityViolation value) => string.Join("|", new[]
        {
            "VIOLATION", value.ViolationType.ToString(),
            value.SectorId.HasValue ? Number(value.SectorId.Value.Value) : "NONE", Token(value.SignatureId),
            Token(value.Subject), Token(value.Reason),
        });

        private static string Digest(string value) => value ?? string.Empty;
        private static string Token(string value) => (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Replace("|", "/");
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Bool(bool value) => value ? "1" : "0";
    }
}
