using System;
using System.Collections.Generic;
using System.Linq;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class WorldSpecialClusterPolicyPlanner
    {
        public const string ReferencePublicationLabel = "REFERENCE MULTI-SECTOR RESERVATION PLAN";
        public const int NewRngDrawCount = 0;
        public static WorldClusterSpanKind DefaultClusterSpanKind => WorldClusterSpanKind.SectorContained;

        public static WorldReservationPolicyResult Plan(WorldReservationPolicyRequest request)
        {
            var failures = new List<WorldReservationPolicyFailure>();
            if (request == null)
            {
                failures.Add(Failure(
                    WorldReservationPolicyFailureCode.MissingRequest,
                    "request",
                    "World reservation policy request is required."));
                return WorldReservationPolicyResult.Fail(failures);
            }

            ValidateUpstream(request, failures);
            if (failures.Count != 0) return WorldReservationPolicyResult.Fail(failures);

            var sectors = new HashSet<WorldSectorId>(request.WorldPlan.Nodes.Select(value => value.Id));
            var edges = request.IntersectorPlan.Edges.ToDictionary(value => value.Id);
            ValidateTransactions(request.Transactions, sectors, edges, failures);
            ValidateFixedSpecialOverlaps(request.Transactions, failures);
            var allowances = IndexAllowances(request.CrossSectorAllowances, edges, failures);
            var specialEdges = new HashSet<WorldIntersectorEdgeId>(request.Transactions
                .Where(value => !value.IsDeferred)
                .SelectMany(value => value.EdgeIds));
            ValidateClusterPolicies(request.ClusterPolicies, allowances, specialEdges, sectors, edges, failures);
            ValidateAllowanceCoverage(request.ClusterPolicies, allowances.Values, failures);
            ValidateQuietClaims(request.QuietClaims, sectors, edges, failures);
            if (failures.Count != 0) return WorldReservationPolicyResult.Fail(failures);

            var claims = new List<WorldReservationClaim>();
            var locks = BuildInheritedAndSpecialLocks(request.Transactions, request.IntersectorPlan.Edges);
            var conflicts = new List<WorldReservationConflict>();
            var occupancy = new Dictionary<WorldSectorId, WorldReservationClaim>();

            AddSpecialClaims(request.Transactions, claims, occupancy, conflicts);
            var resolvedPolicies = ResolveClusterPolicies(
                request.ClusterPolicies, occupancy, claims, locks, conflicts);
            ResolveQuietClaims(request.QuietClaims, occupancy, claims, conflicts);

            var outputDigest = WorldReservationPolicyDigest.ComputeOutput(
                request,
                request.Transactions,
                claims,
                locks,
                resolvedPolicies,
                request.CrossSectorAllowances,
                conflicts);
            if (!WorldSolveDigest.IsLowerHexSha256(outputDigest))
            {
                failures.Add(Failure(
                    WorldReservationPolicyFailureCode.EmptyOutputDigest,
                    "output",
                    "Reservation policy output digest must be lower-hex SHA-256."));
                return WorldReservationPolicyResult.Fail(failures);
            }

            return WorldReservationPolicyResult.Pass(new WorldMultiSectorReservationPlan(
                request,
                request.Transactions,
                claims,
                locks,
                resolvedPolicies,
                request.CrossSectorAllowances,
                conflicts,
                outputDigest));
        }

        private static void ValidateUpstream(
            WorldReservationPolicyRequest request,
            ICollection<WorldReservationPolicyFailure> failures)
        {
            var world = request.WorldPlan;
            var solve = request.SolveOrder;
            if (world == null || solve == null || !solve.Success || solve.Input == null ||
                world.Nodes.Count != WorldPlanInput.SectorCount || solve.Steps.Count != WorldPlanInput.SectorCount ||
                !string.Equals(world.CanonicalDigest, solve.InputDigest, StringComparison.Ordinal))
            {
                failures.Add(Failure(
                    WorldReservationPolicyFailureCode.InvalidWorldPlan,
                    "world-plan",
                    "A successful 169-sector MAP15_01 plan and solve order are required."));
                return;
            }

            var intersector = request.IntersectorPlan;
            if (intersector == null || intersector.Request == null ||
                intersector.Edges.Count != WorldIntersectorEdgePlan.InternalEdgeCount ||
                intersector.EndpointActualCount != WorldIntersectorEdgePlan.EndpointCount ||
                intersector.Edges.Any(value => value.Endpoints.Count != 2 ||
                                               value.RouteSignature == null ||
                                               !value.RouteSignature.Compatible) ||
                !string.Equals(intersector.Request.WorldPlan.CanonicalDigest, world.CanonicalDigest,
                    StringComparison.Ordinal) ||
                !string.Equals(intersector.Request.SolveOrder.OutputDigest, solve.OutputDigest,
                    StringComparison.Ordinal))
            {
                failures.Add(Failure(
                    WorldReservationPolicyFailureCode.InvalidIntersectorPlan,
                    "intersector-plan",
                    "A compatible 312-edge/624-endpoint MAP15_02 plan for the same world is required."));
                return;
            }

            if (!WorldSolveDigest.IsLowerHexSha256(world.CanonicalDigest) ||
                !WorldSolveDigest.IsLowerHexSha256(solve.OutputDigest) ||
                !WorldSolveDigest.IsLowerHexSha256(intersector.InputDigest) ||
                !WorldSolveDigest.IsLowerHexSha256(intersector.OutputDigest) ||
                !WorldSolveDigest.IsLowerHexSha256(request.Map13AuthorityDigest) ||
                !WorldSolveDigest.IsLowerHexSha256(request.Map14HandoffDigest) ||
                !WorldSolveDigest.IsLowerHexSha256(request.CanonicalDigest) ||
                string.IsNullOrEmpty(request.PublicationLabel))
            {
                failures.Add(Failure(
                    WorldReservationPolicyFailureCode.InvalidDigest,
                    "digest",
                    "All upstream, authority, and canonical digests must be lower-hex SHA-256."));
            }

            if (request.FallbackCarveCount != 0 || solve.FallbackCarveCount != 0 ||
                intersector.FallbackCarveCount != 0)
            {
                failures.Add(Failure(
                    WorldReservationPolicyFailureCode.FallbackCarveRequired,
                    "fallback",
                    "Reservation policy cannot carve a fallback corridor."));
            }

            if (request.SectorRerenderCount != 0)
            {
                failures.Add(Failure(
                    WorldReservationPolicyFailureCode.SectorRerenderRequired,
                    "sector-rerender",
                    "Reservation policy cannot rerender sector terrain."));
            }

            if (request.NewRngDrawCount != NewRngDrawCount || solve.NewRngDrawCount != 0 ||
                intersector.NewRngDrawCount != 0 || request.GeneratedFileWriteCount != 0 ||
                request.TilemapMutationCount != 0 || request.SceneMutationCount != 0 ||
                request.PrefabMutationCount != 0 || request.GameObjectMutationCount != 0 ||
                request.GameplaySpawnCount != 0 || request.SpecialRegionMutationCount != 0 ||
                request.SectorPlannerMutationCount != 0 || request.WorldPlanMutationCount != 0 ||
                request.IntersectorPlanMutationCount != 0 || world.GeneratedFileWriteCount != 0 ||
                world.TilemapMutationCount != 0 || world.SceneMutationCount != 0 ||
                world.PrefabMutationCount != 0 || world.GameObjectMutationCount != 0 ||
                world.GameplaySpawnCount != 0 || world.SectorPlannerMutationCount != 0 ||
                intersector.GeneratedFileWriteCount != 0 || intersector.TilemapMutationCount != 0 ||
                intersector.SceneMutationCount != 0 || intersector.PrefabMutationCount != 0 ||
                intersector.GameObjectMutationCount != 0 || intersector.GameplaySpawnCount != 0 ||
                intersector.SectorPlannerMutationCount != 0 || intersector.WorldPlanMutationCount != 0)
            {
                failures.Add(Failure(
                    WorldReservationPolicyFailureCode.MutationClaim,
                    "mutation",
                    "MAP15_03 permits no RNG draw, file write, upstream mutation, or runtime mutation."));
            }
        }

        private static void ValidateTransactions(
            IReadOnlyList<WorldSpecialReservationTransaction> transactions,
            ISet<WorldSectorId> sectors,
            IReadOnlyDictionary<WorldIntersectorEdgeId, WorldIntersectorEdge> edges,
            ICollection<WorldReservationPolicyFailure> failures)
        {
            foreach (var duplicate in transactions.GroupBy(value => value.TransactionId, StringComparer.Ordinal)
                         .Where(group => string.IsNullOrEmpty(group.Key) || group.Count() != 1))
            {
                failures.Add(Failure(
                    WorldReservationPolicyFailureCode.DuplicateTransaction,
                    duplicate.Key,
                    "Transaction IDs must be non-empty and unique."));
            }

            foreach (var transaction in transactions)
            {
                if (string.IsNullOrEmpty(transaction.SpecialKindId) ||
                    string.IsNullOrEmpty(transaction.SourceOwner) ||
                    !Enum.IsDefined(typeof(StarNight.Map.WorldGeneration.SpecialRegions.SpecialRegionKind),
                        transaction.AuthorityKind))
                {
                    failures.Add(Failure(
                        WorldReservationPolicyFailureCode.InvalidTransactionState,
                        transaction.TransactionId,
                        "Special kind and public source owner are required."));
                }

                if (transaction.SectorIds.Distinct().Count() != transaction.SectorIds.Count)
                {
                    failures.Add(Failure(
                        WorldReservationPolicyFailureCode.DuplicateTransactionSector,
                        transaction.TransactionId,
                        "Special transaction cannot repeat a sector ID."));
                }

                foreach (var sector in transaction.SectorIds.Where(value => !sectors.Contains(value)))
                {
                    failures.Add(Failure(
                        WorldReservationPolicyFailureCode.MissingSector,
                        transaction.TransactionId + ":" + sector,
                        "Special transaction references a missing world sector."));
                }

                var validState = transaction.IsDeferred
                    ? transaction.SpanKind == WorldReservationSpanKind.Deferred &&
                      transaction.SectorIds.Count == 0 && transaction.EdgeIds.Count == 0
                    : transaction.State == WorldReservationTransactionState.Fixed &&
                      transaction.SpanKind != WorldReservationSpanKind.Deferred;
                if (!validState)
                {
                    failures.Add(Failure(
                        WorldReservationPolicyFailureCode.InvalidTransactionState,
                        transaction.TransactionId,
                        "Deferred state requires an empty Deferred span; fixed state requires a reserved span."));
                    continue;
                }

                if (transaction.IsDeferred) continue;
                ValidateFixedSpan(transaction, edges, failures);
            }
        }

        private static void ValidateFixedSpan(
            WorldSpecialReservationTransaction transaction,
            IReadOnlyDictionary<WorldIntersectorEdgeId, WorldIntersectorEdge> edges,
            ICollection<WorldReservationPolicyFailure> failures)
        {
            var validCount = transaction.SpanKind == WorldReservationSpanKind.SingleSector
                ? transaction.SectorIds.Count == 1 && transaction.EdgeIds.Count == 0
                : transaction.SpanKind == WorldReservationSpanKind.TwoSector
                    ? transaction.SectorIds.Count == 2 && transaction.EdgeIds.Count == 1
                    : transaction.SpanKind == WorldReservationSpanKind.MultiSectorExplicit &&
                      transaction.SectorIds.Count >= 2 && transaction.EdgeIds.Count >= 1;
            if (!validCount)
            {
                failures.Add(Failure(
                    WorldReservationPolicyFailureCode.InvalidTransactionSpan,
                    transaction.TransactionId,
                    "Transaction sector and edge counts do not match its declared span kind."));
                return;
            }

            var transactionSectors = new HashSet<WorldSectorId>(transaction.SectorIds);
            foreach (var edgeId in transaction.EdgeIds)
            {
                if (!edges.TryGetValue(edgeId, out var edge) || edge.Endpoints.Count != 2)
                {
                    failures.Add(Failure(
                        WorldReservationPolicyFailureCode.MissingTransactionEdge,
                        transaction.TransactionId + ":" + edgeId,
                        "Transaction edge must exist with two endpoints in MAP15_02."));
                    continue;
                }

                if (!edge.RouteSignature.Compatible ||
                    edge.Endpoints.Any(value => !transactionSectors.Contains(value.SectorId)))
                {
                    failures.Add(Failure(
                        WorldReservationPolicyFailureCode.NonAdjacentTwoSectorTransaction,
                        transaction.TransactionId + ":" + edgeId,
                        "Transaction edge must connect compatible endpoints inside its exact sector span."));
                }

                if ((edge.IsBoundary || edge.RouteSignature.MandatoryRoute) &&
                    !transaction.ExplicitlyOwnsProtectedEdge)
                {
                    failures.Add(Failure(
                        WorldReservationPolicyFailureCode.ProtectedEdgeConflict,
                        transaction.TransactionId + ":" + edgeId,
                        "Special transaction must explicitly own a mandatory or boundary edge lock."));
                }
            }

            if (transaction.SpanKind == WorldReservationSpanKind.TwoSector)
            {
                var edgeId = transaction.EdgeIds[0];
                if (edgeId.MinSector != transaction.SectorIds[0] || edgeId.MaxSector != transaction.SectorIds[1])
                {
                    failures.Add(Failure(
                        WorldReservationPolicyFailureCode.NonAdjacentTwoSectorTransaction,
                        transaction.TransactionId,
                        "Two-sector transaction must identify the exact adjacent intersector edge."));
                }
            }

            if (transaction.RequiresEntryReturnEvidence &&
                (string.IsNullOrEmpty(transaction.EntryEvidenceId) ||
                 string.IsNullOrEmpty(transaction.ReturnEvidenceId)))
            {
                failures.Add(Failure(
                    WorldReservationPolicyFailureCode.MissingEntryReturnEvidence,
                    transaction.TransactionId,
                    "Mandatory Special transaction requires both entry and return evidence."));
            }
        }

        private static void ValidateFixedSpecialOverlaps(
            IReadOnlyList<WorldSpecialReservationTransaction> transactions,
            ICollection<WorldReservationPolicyFailure> failures)
        {
            var fixedTransactions = transactions.Where(value => !value.IsDeferred).OrderBy(value => value).ToArray();
            for (var firstIndex = 0; firstIndex < fixedTransactions.Length; firstIndex++)
            {
                for (var secondIndex = firstIndex + 1; secondIndex < fixedTransactions.Length; secondIndex++)
                {
                    var first = fixedTransactions[firstIndex];
                    var second = fixedTransactions[secondIndex];
                    var overlaps = first.SectorIds.Intersect(second.SectorIds).Any() ||
                                   first.EdgeIds.Intersect(second.EdgeIds).Any();
                    var merged = !string.IsNullOrEmpty(first.MergeReason) &&
                                 string.Equals(first.MergeReason, second.MergeReason, StringComparison.Ordinal);
                    if (overlaps && !merged)
                    {
                        failures.Add(Failure(
                            WorldReservationPolicyFailureCode.FixedSpecialOverlap,
                            first.TransactionId + ":" + second.TransactionId,
                            "Fixed Special transactions overlap without one explicit shared merge reason."));
                    }
                }
            }
        }

        private static Dictionary<string, WorldClusterCrossSectorAllowance> IndexAllowances(
            IReadOnlyList<WorldClusterCrossSectorAllowance> allowances,
            IReadOnlyDictionary<WorldIntersectorEdgeId, WorldIntersectorEdge> edges,
            ICollection<WorldReservationPolicyFailure> failures)
        {
            var result = new Dictionary<string, WorldClusterCrossSectorAllowance>(StringComparer.Ordinal);
            foreach (var allowance in allowances)
            {
                if (string.IsNullOrEmpty(allowance.AllowanceId) || !result.TryAdd(allowance.AllowanceId, allowance))
                {
                    failures.Add(Failure(
                        WorldReservationPolicyFailureCode.DuplicateCrossSectorAllowance,
                        allowance.AllowanceId,
                        "Cross-sector allowance IDs must be non-empty and unique."));
                }

                if (string.IsNullOrEmpty(allowance.ClusterId.Value) ||
                    string.IsNullOrEmpty(allowance.VariantId.Value) ||
                    string.IsNullOrEmpty(allowance.SpanReason) ||
                    string.IsNullOrEmpty(allowance.SourceOwner) ||
                    allowance.AllowedOwnerKind != WorldReservationOwnerKind.CrossSectorCluster ||
                    allowance.AllowedSpanKind != WorldClusterSpanKind.CrossSectorAllowlisted ||
                    !edges.TryGetValue(allowance.EdgeId, out var edge) || edge.Endpoints.Count != 2 ||
                    !edge.RouteSignature.Compatible)
                {
                    failures.Add(Failure(
                        WorldReservationPolicyFailureCode.InvalidCrossSectorAllowance,
                        allowance.AllowanceId,
                        "Allowance requires exact cluster/variant/compatible edge/owner/span/reason/source identity."));
                }
            }
            return result;
        }

        private static void ValidateClusterPolicies(
            IReadOnlyList<WorldClusterContainmentPolicy> policies,
            IReadOnlyDictionary<string, WorldClusterCrossSectorAllowance> allowances,
            ISet<WorldIntersectorEdgeId> specialEdges,
            ISet<WorldSectorId> sectors,
            IReadOnlyDictionary<WorldIntersectorEdgeId, WorldIntersectorEdge> edges,
            ICollection<WorldReservationPolicyFailure> failures)
        {
            foreach (var duplicate in policies.GroupBy(value => value.PolicyId, StringComparer.Ordinal)
                         .Where(group => string.IsNullOrEmpty(group.Key) || group.Count() != 1))
            {
                failures.Add(Failure(
                    WorldReservationPolicyFailureCode.InvalidClusterIdentity,
                    duplicate.Key,
                    "Cluster policy IDs must be non-empty and unique."));
            }

            foreach (var policy in policies)
            {
                if (string.IsNullOrEmpty(policy.ClusterId.Value) || string.IsNullOrEmpty(policy.VariantId.Value) ||
                    string.IsNullOrEmpty(policy.SourceOwner) ||
                    policy.SectorIds.Distinct().Count() != policy.SectorIds.Count)
                {
                    failures.Add(Failure(
                        WorldReservationPolicyFailureCode.InvalidClusterIdentity,
                        policy.PolicyId,
                        "Cluster/variant/source identity and distinct sectors are required."));
                }

                foreach (var sector in policy.SectorIds.Where(value => !sectors.Contains(value)))
                {
                    failures.Add(Failure(
                        WorldReservationPolicyFailureCode.MissingSector,
                        policy.PolicyId + ":" + sector,
                        "Cluster policy references a missing world sector."));
                }

                if (!policy.IsCrossSector)
                {
                    if (policy.SectorIds.Count != 1 || policy.EdgeId.HasValue)
                    {
                        failures.Add(Failure(
                            WorldReservationPolicyFailureCode.InvalidClusterSpan,
                            policy.PolicyId,
                            "Default sector-contained cluster must claim exactly one sector and no world edge."));
                    }
                    continue;
                }

                if (policy.SectorIds.Count != 2 || !policy.EdgeId.HasValue || string.IsNullOrEmpty(policy.SpanReason) ||
                    !edges.TryGetValue(policy.EdgeId.Value, out var edge) || edge.Endpoints.Count != 2 ||
                    !edge.RouteSignature.Compatible ||
                    edge.Id.MinSector != policy.SectorIds[0] || edge.Id.MaxSector != policy.SectorIds[1])
                {
                    failures.Add(Failure(
                        WorldReservationPolicyFailureCode.InvalidClusterSpan,
                        policy.PolicyId,
                        "Cross-sector cluster requires two adjacent sectors and their exact compatible edge."));
                    continue;
                }

                var exact = allowances.Values.Where(value =>
                    value.ClusterId == policy.ClusterId && value.VariantId == policy.VariantId &&
                    value.EdgeId == policy.EdgeId.Value &&
                    string.Equals(value.SpanReason, policy.SpanReason, StringComparison.Ordinal)).ToArray();
                if (exact.Length != 1)
                {
                    failures.Add(Failure(
                        WorldReservationPolicyFailureCode.MissingCrossSectorAllowance,
                        policy.PolicyId,
                        "Cross-sector cluster requires one exact cluster/variant/edge/reason allowlist entry."));
                }

                if (edge.IsBoundary || edge.RouteSignature.MandatoryRoute || specialEdges.Contains(edge.Id))
                {
                    failures.Add(Failure(
                        WorldReservationPolicyFailureCode.ProtectedEdgeConflict,
                        policy.PolicyId + ":" + edge.Id,
                        "Allowlisted cluster cannot take a Special, mandatory-route, or boundary-warning edge lock."));
                }
            }
        }

        private static void ValidateAllowanceCoverage(
            IReadOnlyList<WorldClusterContainmentPolicy> policies,
            IEnumerable<WorldClusterCrossSectorAllowance> allowances,
            ICollection<WorldReservationPolicyFailure> failures)
        {
            foreach (var allowance in allowances)
            {
                if (!policies.Any(policy => policy.IsCrossSector &&
                    policy.ClusterId == allowance.ClusterId && policy.VariantId == allowance.VariantId &&
                    policy.EdgeId.HasValue && policy.EdgeId.Value == allowance.EdgeId &&
                    string.Equals(policy.SpanReason, allowance.SpanReason, StringComparison.Ordinal)))
                {
                    failures.Add(Failure(
                        WorldReservationPolicyFailureCode.InvalidCrossSectorAllowance,
                        allowance.AllowanceId,
                        "Allowance must match one declared cross-sector cluster policy exactly."));
                }
            }
        }

        private static void ValidateQuietClaims(
            IReadOnlyList<WorldReservationClaim> claims,
            ISet<WorldSectorId> sectors,
            IReadOnlyDictionary<WorldIntersectorEdgeId, WorldIntersectorEdge> edges,
            ICollection<WorldReservationPolicyFailure> failures)
        {
            foreach (var claim in claims)
            {
                if (string.IsNullOrEmpty(claim.ClaimId) ||
                    claim.OwnerKind != WorldReservationOwnerKind.QuietFiller ||
                    string.IsNullOrEmpty(claim.OwnerId) || string.IsNullOrEmpty(claim.Reason) ||
                    string.IsNullOrEmpty(claim.SourceOwner) || !sectors.Contains(claim.SectorId) ||
                    (claim.EdgeId.HasValue && !edges.ContainsKey(claim.EdgeId.Value)))
                {
                    failures.Add(Failure(
                        WorldReservationPolicyFailureCode.InvalidQuietClaim,
                        claim.ClaimId,
                        "Quiet claim requires an existing sector, optional existing edge, and complete owner/reason identity."));
                }
            }
        }

        private static List<WorldReservationEdgeLock> BuildInheritedAndSpecialLocks(
            IReadOnlyList<WorldSpecialReservationTransaction> transactions,
            IReadOnlyList<WorldIntersectorEdge> edges)
        {
            var result = new List<WorldReservationEdgeLock>();
            var ownedBySpecial = new Dictionary<WorldIntersectorEdgeId, WorldSpecialReservationTransaction>();
            foreach (var transaction in transactions.Where(value => !value.IsDeferred))
            foreach (var edgeId in transaction.EdgeIds)
                ownedBySpecial[edgeId] = transaction;

            foreach (var edge in edges.Where(value => value.RouteSignature.MandatoryRoute || value.IsBoundary))
            {
                if (ownedBySpecial.TryGetValue(edge.Id, out var special) && special.ExplicitlyOwnsProtectedEdge)
                {
                    result.Add(LockForSpecial(special, edge));
                }
                else
                {
                    result.Add(new WorldReservationEdgeLock(
                        "LOCK_INHERITED_" + edge.Id,
                        edge.Id,
                        edge.RouteSignature.MandatoryRoute
                            ? WorldReservationLockKind.MandatoryRoute
                            : WorldReservationLockKind.Boundary,
                        WorldReservationOwnerKind.MandatoryRouteBoundary,
                        edge.RouteSignature.MandatoryRoute ? "MAP15_02_MANDATORY_ROUTE" : "MAP15_02_BOUNDARY",
                        edge.RouteSignature.MandatoryRoute,
                        edge.IsBoundary,
                        "Inherited MAP15_02 route/boundary obligation"));
                }
            }

            foreach (var transaction in transactions.Where(value => !value.IsDeferred))
            foreach (var edgeId in transaction.EdgeIds.Where(edgeId =>
                         !result.Any(item => item.EdgeId == edgeId)))
            {
                var edge = edges.Single(value => value.Id == edgeId);
                result.Add(LockForSpecial(transaction, edge));
            }
            return result;
        }

        private static WorldReservationEdgeLock LockForSpecial(
            WorldSpecialReservationTransaction transaction,
            WorldIntersectorEdge edge) =>
            new WorldReservationEdgeLock(
                "LOCK_SPECIAL_" + transaction.TransactionId + "_" + edge.Id,
                edge.Id,
                WorldReservationLockKind.FixedSpecial,
                WorldReservationOwnerKind.FixedSpecial,
                transaction.TransactionId,
                edge.RouteSignature.MandatoryRoute,
                edge.IsBoundary,
                "Atomic Special transaction edge lock");

        private static void AddSpecialClaims(
            IEnumerable<WorldSpecialReservationTransaction> transactions,
            ICollection<WorldReservationClaim> claims,
            IDictionary<WorldSectorId, WorldReservationClaim> occupancy,
            ICollection<WorldReservationConflict> conflicts)
        {
            foreach (var transaction in transactions.Where(value => !value.IsDeferred).OrderBy(value => value))
            foreach (var sector in transaction.SectorIds)
            {
                var claimEdge = transaction.EdgeIds
                    .Where(edge => edge.MinSector == sector || edge.MaxSector == sector)
                    .Select(edge => (WorldIntersectorEdgeId?)edge)
                    .FirstOrDefault();
                var claim = new WorldReservationClaim(
                    "CLAIM_SPECIAL_" + transaction.TransactionId + "_" + sector.Value,
                    WorldReservationOwnerKind.FixedSpecial,
                    sector,
                    claimEdge,
                    transaction.TransactionId,
                    "Fixed Special transaction sector reservation",
                    transaction.SourceOwner);
                if (occupancy.TryGetValue(sector, out var winner))
                {
                    conflicts.Add(PriorityConflict(sector, winner, claim,
                        "Explicitly merged fixed Special transaction retained by stable transaction order."));
                    continue;
                }
                occupancy.Add(sector, claim);
                claims.Add(claim);
            }
        }

        private static WorldClusterContainmentPolicy[] ResolveClusterPolicies(
            IEnumerable<WorldClusterContainmentPolicy> sourcePolicies,
            IDictionary<WorldSectorId, WorldReservationClaim> occupancy,
            ICollection<WorldReservationClaim> claims,
            ICollection<WorldReservationEdgeLock> locks,
            ICollection<WorldReservationConflict> conflicts)
        {
            var result = new List<WorldClusterContainmentPolicy>();
            foreach (var policy in sourcePolicies.OrderBy(value => value))
            {
                var ownerKind = policy.IsCrossSector
                    ? WorldReservationOwnerKind.CrossSectorCluster
                    : WorldReservationOwnerKind.SectorContainedCluster;
                var proposed = policy.SectorIds.Select(sector => new WorldReservationClaim(
                    "CLAIM_CLUSTER_" + policy.PolicyId + "_" + sector.Value,
                    ownerKind,
                    sector,
                    policy.EdgeId,
                    policy.PolicyId,
                    policy.IsCrossSector
                        ? "Explicit allowlisted cross-sector TerrainCluster"
                        : "Default sector-contained TerrainCluster",
                    policy.SourceOwner)).ToArray();
                var blockers = proposed.Where(value => occupancy.ContainsKey(value.SectorId)).ToArray();
                if (blockers.Length != 0)
                {
                    foreach (var loser in blockers)
                    {
                        conflicts.Add(PriorityConflict(
                            loser.SectorId,
                            occupancy[loser.SectorId],
                            loser,
                            "Higher-priority reservation retained; TerrainCluster policy rejected atomically."));
                    }
                    result.Add(policy.Resolve(false));
                    continue;
                }

                foreach (var claim in proposed)
                {
                    occupancy.Add(claim.SectorId, claim);
                    claims.Add(claim);
                }
                if (policy.IsCrossSector)
                {
                    locks.Add(new WorldReservationEdgeLock(
                        "LOCK_CLUSTER_" + policy.PolicyId + "_" + policy.EdgeId.Value,
                        policy.EdgeId.Value,
                        WorldReservationLockKind.CrossSectorCluster,
                        WorldReservationOwnerKind.CrossSectorCluster,
                        policy.PolicyId,
                        false,
                        false,
                        "Exact allowlisted cross-sector TerrainCluster edge lock"));
                }
                result.Add(policy.Resolve(true));
            }
            return result.ToArray();
        }

        private static void ResolveQuietClaims(
            IEnumerable<WorldReservationClaim> sourceClaims,
            IDictionary<WorldSectorId, WorldReservationClaim> occupancy,
            ICollection<WorldReservationClaim> claims,
            ICollection<WorldReservationConflict> conflicts)
        {
            foreach (var claim in sourceClaims.OrderBy(value => value))
            {
                if (occupancy.TryGetValue(claim.SectorId, out var winner))
                {
                    conflicts.Add(PriorityConflict(
                        claim.SectorId,
                        winner,
                        claim,
                        "Higher-priority reservation retained; quiet/filler claim rejected."));
                    continue;
                }
                occupancy.Add(claim.SectorId, claim);
                claims.Add(claim);
            }
        }

        private static WorldReservationConflict PriorityConflict(
            WorldSectorId sector,
            WorldReservationClaim winner,
            WorldReservationClaim loser,
            string reason) =>
            new WorldReservationConflict(
                WorldReservationConflictType.PriorityOverride,
                sector.ToString(),
                winner.OwnerId,
                winner.OwnerKind,
                loser.OwnerId,
                loser.OwnerKind,
                reason);

        private static WorldReservationPolicyFailure Failure(
            WorldReservationPolicyFailureCode code,
            string subject,
            string reason) =>
            new WorldReservationPolicyFailure(code, subject, reason);
    }
}
