using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum WorldReservationOwnerKind
    {
        FixedSpecial = 1,
        MandatoryRouteBoundary = 2,
        CrossSectorCluster = 3,
        SectorContainedCluster = 4,
        QuietFiller = 5,
    }

    public enum WorldReservationSpanKind
    {
        SingleSector = 1,
        TwoSector = 2,
        MultiSectorExplicit = 3,
        Deferred = 4,
    }

    public enum WorldReservationTransactionState
    {
        Fixed = 1,
        Deferred = 2,
    }

    public enum WorldClusterSpanKind
    {
        SectorContained = 1,
        CrossSectorAllowlisted = 2,
    }

    public enum WorldReservationLockKind
    {
        FixedSpecial = 1,
        MandatoryRoute = 2,
        Boundary = 3,
        CrossSectorCluster = 4,
    }

    public enum WorldReservationConflictType
    {
        PriorityOverride = 1,
        FixedSpecialOverlap = 2,
        ProtectedEdgeLock = 3,
    }

    public enum WorldReservationPolicyFailureCode
    {
        MissingRequest,
        InvalidWorldPlan,
        InvalidIntersectorPlan,
        InvalidDigest,
        DuplicateTransaction,
        InvalidTransactionState,
        DuplicateTransactionSector,
        MissingSector,
        InvalidTransactionSpan,
        MissingTransactionEdge,
        NonAdjacentTwoSectorTransaction,
        MissingEntryReturnEvidence,
        ProtectedEdgeConflict,
        FixedSpecialOverlap,
        InvalidClusterIdentity,
        InvalidClusterSpan,
        MissingCrossSectorAllowance,
        DuplicateCrossSectorAllowance,
        InvalidCrossSectorAllowance,
        InvalidQuietClaim,
        FallbackCarveRequired,
        SectorRerenderRequired,
        MutationClaim,
        EmptyOutputDigest,
    }

    public sealed class WorldReservationClaim : IComparable<WorldReservationClaim>
    {
        public WorldReservationClaim(
            string claimId,
            WorldReservationOwnerKind ownerKind,
            WorldSectorId sectorId,
            WorldIntersectorEdgeId? edgeId,
            string ownerId,
            string reason,
            string sourceOwner)
        {
            ClaimId = claimId ?? string.Empty;
            OwnerKind = ownerKind;
            SectorId = sectorId;
            EdgeId = edgeId;
            OwnerId = ownerId ?? string.Empty;
            Reason = reason ?? string.Empty;
            SourceOwner = sourceOwner ?? string.Empty;
        }

        public string ClaimId { get; }
        public WorldReservationOwnerKind OwnerKind { get; }
        public int Priority => (int)OwnerKind;
        public WorldSectorId SectorId { get; }
        public WorldIntersectorEdgeId? EdgeId { get; }
        public string OwnerId { get; }
        public string Reason { get; }
        public string SourceOwner { get; }

        public int CompareTo(WorldReservationClaim other)
        {
            if (other == null) return -1;
            var comparison = Priority.CompareTo(other.Priority);
            if (comparison != 0) return comparison;
            comparison = SectorId.CompareTo(other.SectorId);
            if (comparison != 0) return comparison;
            comparison = CompareEdge(EdgeId, other.EdgeId);
            if (comparison != 0) return comparison;
            return string.Compare(ClaimId, other.ClaimId, StringComparison.Ordinal);
        }

        private static int CompareEdge(WorldIntersectorEdgeId? left, WorldIntersectorEdgeId? right)
        {
            if (!left.HasValue) return right.HasValue ? -1 : 0;
            return !right.HasValue ? 1 : left.Value.CompareTo(right.Value);
        }
    }

    public sealed class WorldReservationEdgeLock : IComparable<WorldReservationEdgeLock>
    {
        public WorldReservationEdgeLock(
            string lockId,
            WorldIntersectorEdgeId edgeId,
            WorldReservationLockKind lockKind,
            WorldReservationOwnerKind ownerKind,
            string ownerId,
            bool protectsMandatoryRoute,
            bool protectsBoundary,
            string reason)
        {
            LockId = lockId ?? string.Empty;
            EdgeId = edgeId;
            LockKind = lockKind;
            OwnerKind = ownerKind;
            OwnerId = ownerId ?? string.Empty;
            ProtectsMandatoryRoute = protectsMandatoryRoute;
            ProtectsBoundary = protectsBoundary;
            Reason = reason ?? string.Empty;
        }

        public string LockId { get; }
        public WorldIntersectorEdgeId EdgeId { get; }
        public WorldReservationLockKind LockKind { get; }
        public WorldReservationOwnerKind OwnerKind { get; }
        public string OwnerId { get; }
        public bool ProtectsMandatoryRoute { get; }
        public bool ProtectsBoundary { get; }
        public string Reason { get; }

        public int CompareTo(WorldReservationEdgeLock other)
        {
            if (other == null) return -1;
            var comparison = ((int)OwnerKind).CompareTo((int)other.OwnerKind);
            if (comparison != 0) return comparison;
            comparison = EdgeId.CompareTo(other.EdgeId);
            return comparison != 0
                ? comparison
                : string.Compare(LockId, other.LockId, StringComparison.Ordinal);
        }
    }

    public sealed class WorldSpecialReservationTransaction : IComparable<WorldSpecialReservationTransaction>
    {
        private readonly ReadOnlyCollection<WorldSectorId> sectorIds;
        private readonly ReadOnlyCollection<WorldIntersectorEdgeId> edgeIds;

        public WorldSpecialReservationTransaction(
            string transactionId,
            string specialKindId,
            SpecialRegionKind authorityKind,
            WorldReservationTransactionState state,
            WorldReservationSpanKind spanKind,
            IEnumerable<WorldSectorId> sourceSectorIds,
            IEnumerable<WorldIntersectorEdgeId> sourceEdgeIds,
            bool requiresEntryReturnEvidence,
            string entryEvidenceId,
            string returnEvidenceId,
            bool explicitlyOwnsProtectedEdge,
            string mergeReason,
            string sourceOwner)
        {
            TransactionId = transactionId ?? string.Empty;
            SpecialKindId = specialKindId ?? string.Empty;
            AuthorityKind = authorityKind;
            State = state;
            SpanKind = spanKind;
            sectorIds = new ReadOnlyCollection<WorldSectorId>((sourceSectorIds ?? Array.Empty<WorldSectorId>())
                .OrderBy(value => value).ToArray());
            edgeIds = new ReadOnlyCollection<WorldIntersectorEdgeId>(
                (sourceEdgeIds ?? Array.Empty<WorldIntersectorEdgeId>()).OrderBy(value => value).ToArray());
            RequiresEntryReturnEvidence = requiresEntryReturnEvidence;
            EntryEvidenceId = entryEvidenceId ?? string.Empty;
            ReturnEvidenceId = returnEvidenceId ?? string.Empty;
            ExplicitlyOwnsProtectedEdge = explicitlyOwnsProtectedEdge;
            MergeReason = mergeReason ?? string.Empty;
            SourceOwner = sourceOwner ?? string.Empty;
        }

        public string TransactionId { get; }
        public string SpecialKindId { get; }
        public SpecialRegionKind AuthorityKind { get; }
        public WorldReservationTransactionState State { get; }
        public WorldReservationSpanKind SpanKind { get; }
        public IReadOnlyList<WorldSectorId> SectorIds => sectorIds;
        public IReadOnlyList<WorldIntersectorEdgeId> EdgeIds => edgeIds;
        public bool RequiresEntryReturnEvidence { get; }
        public string EntryEvidenceId { get; }
        public string ReturnEvidenceId { get; }
        public bool ExplicitlyOwnsProtectedEdge { get; }
        public string MergeReason { get; }
        public string SourceOwner { get; }
        public bool IsDeferred => State == WorldReservationTransactionState.Deferred;

        public int CompareTo(WorldSpecialReservationTransaction other)
        {
            if (other == null) return -1;
            var comparison = (IsDeferred ? 1 : 0).CompareTo(other.IsDeferred ? 1 : 0);
            return comparison != 0
                ? comparison
                : string.Compare(TransactionId, other.TransactionId, StringComparison.Ordinal);
        }
    }

    public sealed class WorldClusterContainmentPolicy : IComparable<WorldClusterContainmentPolicy>
    {
        private readonly ReadOnlyCollection<WorldSectorId> sectorIds;

        public WorldClusterContainmentPolicy(
            string policyId,
            TerrainClusterId clusterId,
            SpineVariantId variantId,
            WorldClusterSpanKind spanKind,
            IEnumerable<WorldSectorId> sourceSectorIds,
            WorldIntersectorEdgeId? edgeId,
            string spanReason,
            string sourceOwner)
            : this(policyId, clusterId, variantId, spanKind, sourceSectorIds, edgeId, spanReason, sourceOwner, false)
        {
        }

        private WorldClusterContainmentPolicy(
            string policyId,
            TerrainClusterId clusterId,
            SpineVariantId variantId,
            WorldClusterSpanKind spanKind,
            IEnumerable<WorldSectorId> sourceSectorIds,
            WorldIntersectorEdgeId? edgeId,
            string spanReason,
            string sourceOwner,
            bool accepted)
        {
            PolicyId = policyId ?? string.Empty;
            ClusterId = clusterId;
            VariantId = variantId;
            SpanKind = spanKind;
            sectorIds = new ReadOnlyCollection<WorldSectorId>((sourceSectorIds ?? Array.Empty<WorldSectorId>())
                .OrderBy(value => value).ToArray());
            EdgeId = edgeId;
            SpanReason = spanReason ?? string.Empty;
            SourceOwner = sourceOwner ?? string.Empty;
            Accepted = accepted;
        }

        public string PolicyId { get; }
        public TerrainClusterId ClusterId { get; }
        public SpineVariantId VariantId { get; }
        public WorldClusterSpanKind SpanKind { get; }
        public IReadOnlyList<WorldSectorId> SectorIds => sectorIds;
        public WorldIntersectorEdgeId? EdgeId { get; }
        public string SpanReason { get; }
        public string SourceOwner { get; }
        public bool Accepted { get; }
        public bool IsCrossSector => SpanKind == WorldClusterSpanKind.CrossSectorAllowlisted;

        internal WorldClusterContainmentPolicy Resolve(bool accepted) =>
            new WorldClusterContainmentPolicy(
                PolicyId, ClusterId, VariantId, SpanKind, SectorIds, EdgeId,
                SpanReason, SourceOwner, accepted);

        public int CompareTo(WorldClusterContainmentPolicy other)
        {
            if (other == null) return -1;
            var comparison = ClusterId.CompareTo(other.ClusterId);
            if (comparison != 0) return comparison;
            comparison = VariantId.CompareTo(other.VariantId);
            if (comparison != 0) return comparison;
            return string.Compare(PolicyId, other.PolicyId, StringComparison.Ordinal);
        }
    }

    public sealed class WorldClusterCrossSectorAllowance : IComparable<WorldClusterCrossSectorAllowance>
    {
        public WorldClusterCrossSectorAllowance(
            string allowanceId,
            TerrainClusterId clusterId,
            SpineVariantId variantId,
            WorldIntersectorEdgeId edgeId,
            WorldReservationOwnerKind allowedOwnerKind,
            WorldClusterSpanKind allowedSpanKind,
            string spanReason,
            string sourceOwner)
        {
            AllowanceId = allowanceId ?? string.Empty;
            ClusterId = clusterId;
            VariantId = variantId;
            EdgeId = edgeId;
            AllowedOwnerKind = allowedOwnerKind;
            AllowedSpanKind = allowedSpanKind;
            SpanReason = spanReason ?? string.Empty;
            SourceOwner = sourceOwner ?? string.Empty;
        }

        public string AllowanceId { get; }
        public TerrainClusterId ClusterId { get; }
        public SpineVariantId VariantId { get; }
        public WorldIntersectorEdgeId EdgeId { get; }
        public WorldReservationOwnerKind AllowedOwnerKind { get; }
        public WorldClusterSpanKind AllowedSpanKind { get; }
        public string SpanReason { get; }
        public string SourceOwner { get; }

        public int CompareTo(WorldClusterCrossSectorAllowance other)
        {
            if (other == null) return -1;
            var comparison = ClusterId.CompareTo(other.ClusterId);
            if (comparison != 0) return comparison;
            comparison = VariantId.CompareTo(other.VariantId);
            if (comparison != 0) return comparison;
            comparison = EdgeId.CompareTo(other.EdgeId);
            return comparison != 0
                ? comparison
                : string.Compare(AllowanceId, other.AllowanceId, StringComparison.Ordinal);
        }
    }

    public sealed class WorldReservationConflict : IComparable<WorldReservationConflict>
    {
        public WorldReservationConflict(
            WorldReservationConflictType conflictType,
            string subject,
            string winnerId,
            WorldReservationOwnerKind winnerKind,
            string loserId,
            WorldReservationOwnerKind loserKind,
            string reason)
        {
            ConflictType = conflictType;
            Subject = subject ?? string.Empty;
            WinnerId = winnerId ?? string.Empty;
            WinnerKind = winnerKind;
            LoserId = loserId ?? string.Empty;
            LoserKind = loserKind;
            Reason = reason ?? string.Empty;
        }

        public WorldReservationConflictType ConflictType { get; }
        public string Subject { get; }
        public string WinnerId { get; }
        public WorldReservationOwnerKind WinnerKind { get; }
        public string LoserId { get; }
        public WorldReservationOwnerKind LoserKind { get; }
        public string Reason { get; }

        public int CompareTo(WorldReservationConflict other)
        {
            if (other == null) return -1;
            var comparison = ConflictType.CompareTo(other.ConflictType);
            if (comparison != 0) return comparison;
            comparison = string.Compare(WinnerId, other.WinnerId, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = string.Compare(LoserId, other.LoserId, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Subject, other.Subject, StringComparison.Ordinal);
        }
    }

    public sealed class WorldReservationPolicyRequest
    {
        private readonly ReadOnlyCollection<WorldSpecialReservationTransaction> transactions;
        private readonly ReadOnlyCollection<WorldClusterContainmentPolicy> clusterPolicies;
        private readonly ReadOnlyCollection<WorldClusterCrossSectorAllowance> crossSectorAllowances;
        private readonly ReadOnlyCollection<WorldReservationClaim> quietClaims;

        public WorldReservationPolicyRequest(
            WorldPlanInput worldPlan,
            WorldSolveOrderResult solveOrder,
            WorldIntersectorEdgePlan intersectorPlan,
            IEnumerable<WorldSpecialReservationTransaction> sourceTransactions,
            IEnumerable<WorldClusterContainmentPolicy> sourceClusterPolicies,
            IEnumerable<WorldClusterCrossSectorAllowance> sourceCrossSectorAllowances,
            IEnumerable<WorldReservationClaim> sourceQuietClaims,
            string map13AuthorityDigest,
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
            int specialRegionMutationCount = 0,
            int sectorPlannerMutationCount = 0,
            int worldPlanMutationCount = 0,
            int intersectorPlanMutationCount = 0)
        {
            WorldPlan = worldPlan;
            SolveOrder = solveOrder;
            IntersectorPlan = intersectorPlan;
            transactions = Freeze(sourceTransactions);
            clusterPolicies = Freeze(sourceClusterPolicies);
            crossSectorAllowances = Freeze(sourceCrossSectorAllowances);
            quietClaims = Freeze(sourceQuietClaims);
            Map13AuthorityDigest = map13AuthorityDigest ?? string.Empty;
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
            SpecialRegionMutationCount = specialRegionMutationCount;
            SectorPlannerMutationCount = sectorPlannerMutationCount;
            WorldPlanMutationCount = worldPlanMutationCount;
            IntersectorPlanMutationCount = intersectorPlanMutationCount;
            CanonicalDigest = WorldReservationPolicyDigest.ComputeInput(this);
        }

        public WorldPlanInput WorldPlan { get; }
        public WorldSolveOrderResult SolveOrder { get; }
        public WorldIntersectorEdgePlan IntersectorPlan { get; }
        public IReadOnlyList<WorldSpecialReservationTransaction> Transactions => transactions;
        public IReadOnlyList<WorldClusterContainmentPolicy> ClusterPolicies => clusterPolicies;
        public IReadOnlyList<WorldClusterCrossSectorAllowance> CrossSectorAllowances => crossSectorAllowances;
        public IReadOnlyList<WorldReservationClaim> QuietClaims => quietClaims;
        public string Map13AuthorityDigest { get; }
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
        public int SpecialRegionMutationCount { get; }
        public int SectorPlannerMutationCount { get; }
        public int WorldPlanMutationCount { get; }
        public int IntersectorPlanMutationCount { get; }
        public string CanonicalDigest { get; }

        private static ReadOnlyCollection<T> Freeze<T>(IEnumerable<T> source) where T : IComparable<T> =>
            new ReadOnlyCollection<T>((source ?? Array.Empty<T>())
                .Where(value => value != null).OrderBy(value => value).ToArray());
    }

    public sealed class WorldMultiSectorReservationPlan
    {
        private readonly ReadOnlyCollection<WorldSpecialReservationTransaction> transactions;
        private readonly ReadOnlyCollection<WorldReservationClaim> claims;
        private readonly ReadOnlyCollection<WorldReservationEdgeLock> edgeLocks;
        private readonly ReadOnlyCollection<WorldClusterContainmentPolicy> clusterPolicies;
        private readonly ReadOnlyCollection<WorldClusterCrossSectorAllowance> crossSectorAllowances;
        private readonly ReadOnlyCollection<WorldReservationConflict> conflicts;

        internal WorldMultiSectorReservationPlan(
            WorldReservationPolicyRequest request,
            IEnumerable<WorldSpecialReservationTransaction> sourceTransactions,
            IEnumerable<WorldReservationClaim> sourceClaims,
            IEnumerable<WorldReservationEdgeLock> sourceEdgeLocks,
            IEnumerable<WorldClusterContainmentPolicy> sourceClusterPolicies,
            IEnumerable<WorldClusterCrossSectorAllowance> sourceCrossSectorAllowances,
            IEnumerable<WorldReservationConflict> sourceConflicts,
            string outputDigest)
        {
            Request = request;
            transactions = Freeze(sourceTransactions);
            claims = Freeze(sourceClaims);
            edgeLocks = Freeze(sourceEdgeLocks);
            clusterPolicies = Freeze(sourceClusterPolicies);
            crossSectorAllowances = Freeze(sourceCrossSectorAllowances);
            conflicts = Freeze(sourceConflicts);
            OutputDigest = outputDigest ?? string.Empty;
        }

        public const int WorldSectorCount = WorldPlanInput.SectorCount;
        public const int InternalEdgeCount = WorldIntersectorEdgePlan.InternalEdgeCount;
        public const string DownstreamOwner = "MAP15_04_IMPLEMENT_WORLD_PACING_DENSITY_AND_REPETITION";
        public const bool OpensDownstreamTask = false;

        public WorldReservationPolicyRequest Request { get; }
        public IReadOnlyList<WorldSpecialReservationTransaction> Transactions => transactions;
        public IReadOnlyList<WorldReservationClaim> Claims => claims;
        public IReadOnlyList<WorldReservationEdgeLock> EdgeLocks => edgeLocks;
        public IReadOnlyList<WorldClusterContainmentPolicy> ClusterPolicies => clusterPolicies;
        public IReadOnlyList<WorldClusterCrossSectorAllowance> CrossSectorAllowances => crossSectorAllowances;
        public IReadOnlyList<WorldReservationConflict> Conflicts => conflicts;
        public string InputDigest => Request.CanonicalDigest;
        public string OutputDigest { get; }
        public int ObservedWorldSectorCount => Request.WorldPlan.Nodes.Count;
        public int ObservedInternalEdgeCount => Request.IntersectorPlan.Edges.Count;
        public int RequiredTransactionCount => Request.Transactions.Count;
        public int AcceptedTransactionCount => transactions.Count;
        public int MissingTransactionCount => RequiredTransactionCount - AcceptedTransactionCount;
        public int TwoSectorTransactionCount => transactions.Count(value => value.SpanKind == WorldReservationSpanKind.TwoSector);
        public int DeferredTransactionCount => transactions.Count(value => value.IsDeferred);
        public int AcceptedClusterPolicyCount => clusterPolicies.Count(value => value.Accepted);
        public int RejectedClusterPolicyCount => clusterPolicies.Count(value => !value.Accepted);
        public int AcceptedCrossSectorClusterCount => clusterPolicies.Count(value => value.Accepted && value.IsCrossSector);
        public int NewRngDrawCount => Request.NewRngDrawCount;
        public int FallbackCarveCount => Request.FallbackCarveCount;
        public int SectorRerenderCount => Request.SectorRerenderCount;
        public int GeneratedFileWriteCount => Request.GeneratedFileWriteCount;
        public int TilemapMutationCount => Request.TilemapMutationCount;
        public int SceneMutationCount => Request.SceneMutationCount;
        public int PrefabMutationCount => Request.PrefabMutationCount;
        public int GameObjectMutationCount => Request.GameObjectMutationCount;
        public int GameplaySpawnCount => Request.GameplaySpawnCount;
        public int SpecialRegionMutationCount => Request.SpecialRegionMutationCount;
        public int SectorPlannerMutationCount => Request.SectorPlannerMutationCount;
        public int WorldPlanMutationCount => Request.WorldPlanMutationCount;
        public int IntersectorPlanMutationCount => Request.IntersectorPlanMutationCount;

        private static ReadOnlyCollection<T> Freeze<T>(IEnumerable<T> source) where T : IComparable<T> =>
            new ReadOnlyCollection<T>((source ?? Array.Empty<T>())
                .Where(value => value != null).OrderBy(value => value).ToArray());
    }

    public sealed class WorldReservationPolicyFailure :
        IComparable<WorldReservationPolicyFailure>, IEquatable<WorldReservationPolicyFailure>
    {
        public WorldReservationPolicyFailure(WorldReservationPolicyFailureCode code, string subject, string reason)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public WorldReservationPolicyFailureCode Code { get; }
        public string Subject { get; }
        public string Reason { get; }

        public int CompareTo(WorldReservationPolicyFailure other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0 ? comparison : string.Compare(Reason, other.Reason, StringComparison.Ordinal);
        }

        public bool Equals(WorldReservationPolicyFailure other) => other != null && CompareTo(other) == 0;
        public override bool Equals(object obj) => Equals(obj as WorldReservationPolicyFailure);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(ToString());
        public override string ToString() => Code + "|" + Subject + "|" + Reason;
    }

    public sealed class WorldReservationPolicyResult
    {
        private readonly ReadOnlyCollection<WorldReservationPolicyFailure> failures;

        private WorldReservationPolicyResult(
            WorldMultiSectorReservationPlan plan,
            IEnumerable<WorldReservationPolicyFailure> sourceFailures)
        {
            Plan = plan;
            failures = new ReadOnlyCollection<WorldReservationPolicyFailure>(
                (sourceFailures ?? Array.Empty<WorldReservationPolicyFailure>())
                .Where(value => value != null).Distinct().OrderBy(value => value).ToArray());
        }

        public bool Success => Plan != null && failures.Count == 0;
        public WorldMultiSectorReservationPlan Plan { get; }
        public IReadOnlyList<WorldReservationPolicyFailure> Failures => failures;
        public string InputDigest => Plan == null ? string.Empty : Plan.InputDigest;
        public string OutputDigest => Plan == null ? string.Empty : Plan.OutputDigest;

        internal static WorldReservationPolicyResult Pass(WorldMultiSectorReservationPlan plan) =>
            new WorldReservationPolicyResult(plan, Array.Empty<WorldReservationPolicyFailure>());

        internal static WorldReservationPolicyResult Fail(IEnumerable<WorldReservationPolicyFailure> failures) =>
            new WorldReservationPolicyResult(null, failures);
    }

    public static class WorldReservationPolicyDigest
    {
        public static string ComputeInput(WorldReservationPolicyRequest request)
        {
            if (request == null) return string.Empty;
            var lines = new List<string>
            {
                "WORLD_INPUT|" + Digest(request.WorldPlan == null ? string.Empty : request.WorldPlan.CanonicalDigest),
                "WORLD_OUTPUT|" + Digest(request.SolveOrder == null ? string.Empty : request.SolveOrder.OutputDigest),
                "EDGE_INPUT|" + Digest(request.IntersectorPlan == null ? string.Empty : request.IntersectorPlan.InputDigest),
                "EDGE_OUTPUT|" + Digest(request.IntersectorPlan == null ? string.Empty : request.IntersectorPlan.OutputDigest),
                "MAP13|" + Digest(request.Map13AuthorityDigest),
                "MAP14|" + Digest(request.Map14HandoffDigest),
                "PUBLICATION|" + Token(request.PublicationLabel),
                "COUNTERS|" + string.Join("|", new[]
                {
                    Number(request.NewRngDrawCount), Number(request.FallbackCarveCount),
                    Number(request.SectorRerenderCount), Number(request.GeneratedFileWriteCount),
                    Number(request.TilemapMutationCount), Number(request.SceneMutationCount),
                    Number(request.PrefabMutationCount), Number(request.GameObjectMutationCount),
                    Number(request.GameplaySpawnCount), Number(request.SpecialRegionMutationCount),
                    Number(request.SectorPlannerMutationCount), Number(request.WorldPlanMutationCount),
                    Number(request.IntersectorPlanMutationCount),
                }),
            };
            lines.AddRange(request.Transactions.Select(Transaction));
            lines.AddRange(request.ClusterPolicies.Select(ClusterPolicy));
            lines.AddRange(request.CrossSectorAllowances.Select(Allowance));
            lines.AddRange(request.QuietClaims.Select(Claim));
            return HashCanonicalText(string.Join("\n", lines));
        }

        public static string ComputeOutput(
            WorldReservationPolicyRequest request,
            IEnumerable<WorldSpecialReservationTransaction> transactions,
            IEnumerable<WorldReservationClaim> claims,
            IEnumerable<WorldReservationEdgeLock> locks,
            IEnumerable<WorldClusterContainmentPolicy> policies,
            IEnumerable<WorldClusterCrossSectorAllowance> allowances,
            IEnumerable<WorldReservationConflict> conflicts)
        {
            var lines = new List<string> { "INPUT|" + Digest(request == null ? string.Empty : request.CanonicalDigest) };
            lines.AddRange((transactions ?? Array.Empty<WorldSpecialReservationTransaction>()).OrderBy(value => value)
                .Select(Transaction));
            lines.AddRange((claims ?? Array.Empty<WorldReservationClaim>()).OrderBy(value => value).Select(Claim));
            lines.AddRange((locks ?? Array.Empty<WorldReservationEdgeLock>()).OrderBy(value => value).Select(EdgeLock));
            lines.AddRange((policies ?? Array.Empty<WorldClusterContainmentPolicy>()).OrderBy(value => value)
                .Select(ClusterPolicy));
            lines.AddRange((allowances ?? Array.Empty<WorldClusterCrossSectorAllowance>()).OrderBy(value => value)
                .Select(Allowance));
            lines.AddRange((conflicts ?? Array.Empty<WorldReservationConflict>()).OrderBy(value => value)
                .Select(Conflict));
            return HashCanonicalText(string.Join("\n", lines));
        }

        public static string HashCanonicalText(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(new UTF8Encoding(false).GetBytes(value ?? string.Empty));
                return string.Concat(bytes.Select(item => item.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        private static string Transaction(WorldSpecialReservationTransaction value) => string.Join("|", new[]
        {
            "TX", Token(value.TransactionId), Token(value.SpecialKindId), value.AuthorityKind.ToString(),
            value.State.ToString(), value.SpanKind.ToString(),
            string.Join(",", value.SectorIds.Select(item => Number(item.Value))),
            string.Join(",", value.EdgeIds.Select(item => item.ToString())),
            Bool(value.RequiresEntryReturnEvidence), Token(value.EntryEvidenceId), Token(value.ReturnEvidenceId),
            Bool(value.ExplicitlyOwnsProtectedEdge), Token(value.MergeReason), Token(value.SourceOwner),
        });

        private static string Claim(WorldReservationClaim value) => string.Join("|", new[]
        {
            "CLAIM", Token(value.ClaimId), value.OwnerKind.ToString(), Number(value.Priority),
            Number(value.SectorId.Value), Edge(value.EdgeId), Token(value.OwnerId),
            Token(value.Reason), Token(value.SourceOwner),
        });

        private static string EdgeLock(WorldReservationEdgeLock value) => string.Join("|", new[]
        {
            "LOCK", Token(value.LockId), value.EdgeId.ToString(), value.LockKind.ToString(),
            value.OwnerKind.ToString(), Token(value.OwnerId), Bool(value.ProtectsMandatoryRoute),
            Bool(value.ProtectsBoundary), Token(value.Reason),
        });

        private static string ClusterPolicy(WorldClusterContainmentPolicy value) => string.Join("|", new[]
        {
            "CLUSTER", Token(value.PolicyId), Token(value.ClusterId.Value), Token(value.VariantId.Value),
            value.SpanKind.ToString(), string.Join(",", value.SectorIds.Select(item => Number(item.Value))),
            Edge(value.EdgeId), Token(value.SpanReason), Token(value.SourceOwner), Bool(value.Accepted),
        });

        private static string Allowance(WorldClusterCrossSectorAllowance value) => string.Join("|", new[]
        {
            "ALLOW", Token(value.AllowanceId), Token(value.ClusterId.Value), Token(value.VariantId.Value),
            value.EdgeId.ToString(), value.AllowedOwnerKind.ToString(), value.AllowedSpanKind.ToString(),
            Token(value.SpanReason), Token(value.SourceOwner),
        });

        private static string Conflict(WorldReservationConflict value) => string.Join("|", new[]
        {
            "CONFLICT", value.ConflictType.ToString(), Token(value.Subject), Token(value.WinnerId),
            value.WinnerKind.ToString(), Token(value.LoserId), value.LoserKind.ToString(), Token(value.Reason),
        });

        private static string Edge(WorldIntersectorEdgeId? value) => value.HasValue ? value.Value.ToString() : "NONE";
        private static string Digest(string value) => value ?? string.Empty;
        private static string Token(string value)
        {
            var normalized = value ?? string.Empty;
            return normalized.Length.ToString(CultureInfo.InvariantCulture) + ":" + normalized;
        }
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Bool(bool value) => value ? "1" : "0";
    }
}
