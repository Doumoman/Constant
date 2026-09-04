using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.MicroPatterns;

namespace StarNight.Map.WorldGeneration.Population
{
    public sealed class GeneratedHazardEnemyPlacementRequest
    {
        private readonly ReadOnlyCollection<GeneratedHazardEnemyPoolEntry> pools;
        private readonly ReadOnlyCollection<GeneratedHazardEnemyCandidateProtection> candidates;
        private readonly ReadOnlyCollection<GeneratedHazardEnemyBudgetLimit> budgetLimits;
        private readonly ReadOnlyCollection<string> existingReservationKeys;
        private readonly ReadOnlyCollection<string> existingStableSpawnIds;
        private readonly ReadOnlyCollection<string> existingBudgetSpendKeys;

        public GeneratedHazardEnemyPlacementRequest(
            GeneratedPopulationPlacementPlan populationPlan,
            IEnumerable<GeneratedHazardEnemyPoolEntry> sourcePools,
            IEnumerable<GeneratedHazardEnemyCandidateProtection> sourceCandidates,
            IEnumerable<GeneratedHazardEnemyBudgetLimit> sourceBudgetLimits,
            MicroPatternBiomeProfileCatalog biomeCatalog,
            string expectedPopulationPlanDigest,
            string expectedOccupiedSurfaceDigest,
            int expectedOccupiedSurfaceCount,
            int expectedRemainingCandidateCount,
            IEnumerable<string> sourceExistingReservationKeys = null,
            IEnumerable<string> sourceExistingStableSpawnIds = null,
            IEnumerable<string> sourceExistingBudgetSpendKeys = null,
            string expectedPlanDigest = null,
            string expectedFinalOccupiedSurfaceDigest = null,
            string expectedBudgetLedgerDigest = null,
            bool attemptedRuntimeSpawn = false,
            bool attemptedDamage = false,
            bool attemptedPhysics = false,
            bool attemptedEnemyAi = false,
            bool attemptedCombat = false)
        {
            PopulationPlan = populationPlan;
            pools = Freeze(sourcePools, out var nullPools);
            candidates = Freeze(sourceCandidates, out var nullCandidates);
            budgetLimits = Freeze(sourceBudgetLimits, out var nullBudgets);
            BiomeCatalog = biomeCatalog;
            ExpectedPopulationPlanDigest = Normalize(expectedPopulationPlanDigest);
            ExpectedOccupiedSurfaceDigest = Normalize(expectedOccupiedSurfaceDigest);
            ExpectedOccupiedSurfaceCount = expectedOccupiedSurfaceCount;
            ExpectedRemainingCandidateCount = expectedRemainingCandidateCount;
            existingReservationKeys = FreezeStrings(sourceExistingReservationKeys);
            existingStableSpawnIds = FreezeStrings(sourceExistingStableSpawnIds);
            existingBudgetSpendKeys = FreezeStrings(sourceExistingBudgetSpendKeys);
            ExpectedPlanDigest = Normalize(expectedPlanDigest);
            ExpectedFinalOccupiedSurfaceDigest = Normalize(expectedFinalOccupiedSurfaceDigest);
            ExpectedBudgetLedgerDigest = Normalize(expectedBudgetLedgerDigest);
            AttemptedRuntimeSpawn = attemptedRuntimeSpawn;
            AttemptedDamage = attemptedDamage;
            AttemptedPhysics = attemptedPhysics;
            AttemptedEnemyAi = attemptedEnemyAi;
            AttemptedCombat = attemptedCombat;
            NullPoolCount = nullPools;
            NullCandidateCount = nullCandidates;
            NullBudgetCount = nullBudgets;
        }

        public GeneratedPopulationPlacementPlan PopulationPlan { get; }
        public IReadOnlyList<GeneratedHazardEnemyPoolEntry> Pools => pools;
        public IReadOnlyList<GeneratedHazardEnemyCandidateProtection> Candidates => candidates;
        public IReadOnlyList<GeneratedHazardEnemyBudgetLimit> BudgetLimits => budgetLimits;
        public MicroPatternBiomeProfileCatalog BiomeCatalog { get; }
        public string ExpectedPopulationPlanDigest { get; }
        public string ExpectedOccupiedSurfaceDigest { get; }
        public int ExpectedOccupiedSurfaceCount { get; }
        public int ExpectedRemainingCandidateCount { get; }
        public IReadOnlyList<string> ExistingReservationKeys => existingReservationKeys;
        public IReadOnlyList<string> ExistingStableSpawnIds => existingStableSpawnIds;
        public IReadOnlyList<string> ExistingBudgetSpendKeys => existingBudgetSpendKeys;
        public string ExpectedPlanDigest { get; }
        public string ExpectedFinalOccupiedSurfaceDigest { get; }
        public string ExpectedBudgetLedgerDigest { get; }
        public bool AttemptedRuntimeSpawn { get; }
        public bool AttemptedDamage { get; }
        public bool AttemptedPhysics { get; }
        public bool AttemptedEnemyAi { get; }
        public bool AttemptedCombat { get; }
        public int NullPoolCount { get; }
        public int NullCandidateCount { get; }
        public int NullBudgetCount { get; }

        private static ReadOnlyCollection<T> Freeze<T>(IEnumerable<T> source, out int nullCount)
            where T : class, IComparable<T>
        {
            var raw = (source ?? Array.Empty<T>()).ToArray();
            nullCount = raw.Count(value => value == null);
            return new ReadOnlyCollection<T>(raw.Where(value => value != null)
                .OrderBy(value => value).ToArray());
        }

        private static ReadOnlyCollection<string> FreezeStrings(IEnumerable<string> source) =>
            new ReadOnlyCollection<string>((source ?? Array.Empty<string>())
                .Select(Normalize).Where(value => value.Length > 0)
                .Distinct(StringComparer.Ordinal).OrderBy(value => value,
                    StringComparer.Ordinal).ToArray());
        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? string.Empty : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
    }

    public enum GeneratedHazardEnemyPlacementFailureCode
    {
        MissingRequest = 1,
        MissingPopulationPlan = 2,
        PopulationPlanDigestMismatch = 3,
        OccupiedSurfaceDigestMismatch = 4,
        OccupiedSurfaceCountMismatch = 5,
        RemainingCandidateCountMismatch = 6,
        MissingBiomeCatalog = 7,
        MissingCandidateProtection = 8,
        DuplicateCandidateProtection = 9,
        InvalidCandidateProtection = 10,
        MissingHazardPool = 11,
        MissingEnemyPool = 12,
        DuplicatePool = 13,
        InvalidPoolKey = 14,
        InvalidPool = 15,
        MissingBudgetScope = 16,
        DuplicateBudgetScope = 17,
        InvalidBudgetScope = 18,
        MissingHazardCandidate = 19,
        MissingEnemyCandidate = 20,
        OccupiedSlotReuse = 21,
        RouteProtectionViolation = 22,
        RewardApproachProtectionViolation = 23,
        RecoveryFloorProtectionViolation = 24,
        SafeRadiusViolation = 25,
        NeighborRadiusViolation = 26,
        BudgetOverflow = 27,
        DuplicateBudgetSpend = 28,
        ReservationKeyCollision = 29,
        StableSpawnIdCollision = 30,
        AttemptedRuntimeSpawnDamagePhysicsAiOrCombat = 31,
        PlanDigestMismatch = 32,
        FinalOccupiedSurfaceDigestMismatch = 33,
        BudgetLedgerDigestMismatch = 34,
    }

    public sealed class GeneratedHazardEnemyPlacementFailure :
        IComparable<GeneratedHazardEnemyPlacementFailure>
    {
        public GeneratedHazardEnemyPlacementFailure(
            GeneratedHazardEnemyPlacementFailureCode code,
            string owner,
            string offendingKey,
            string expected,
            string actual,
            string reason)
        {
            Code = code;
            Owner = owner ?? string.Empty;
            OffendingKey = offendingKey ?? string.Empty;
            Expected = expected ?? string.Empty;
            Actual = actual ?? string.Empty;
            Reason = reason ?? string.Empty;
            StableToken = string.Join("|", new[]
            {
                Code.ToString().ToUpperInvariant(), Owner, OffendingKey,
                Expected, Actual, Reason,
            });
        }

        public GeneratedHazardEnemyPlacementFailureCode Code { get; }
        public string Owner { get; }
        public string OffendingKey { get; }
        public string Expected { get; }
        public string Actual { get; }
        public string Reason { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedHazardEnemyPlacementFailure other) => other == null
            ? -1 : string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
        public override string ToString() => StableToken;
    }

    public sealed class GeneratedHazardEnemyPlacementResult
    {
        private readonly ReadOnlyCollection<GeneratedHazardEnemyPlacementFailure> failures;

        internal GeneratedHazardEnemyPlacementResult(
            GeneratedHazardEnemyPlacementPlan plan,
            IEnumerable<GeneratedHazardEnemyPlacementFailure> sourceFailures)
        {
            Plan = plan;
            failures = new ReadOnlyCollection<GeneratedHazardEnemyPlacementFailure>(
                (sourceFailures ?? Array.Empty<GeneratedHazardEnemyPlacementFailure>())
                .OrderBy(value => value).ToArray());
        }

        public bool Success => Plan != null && failures.Count == 0;
        public GeneratedHazardEnemyPlacementPlan Plan { get; }
        public IReadOnlyList<GeneratedHazardEnemyPlacementFailure> Failures => failures;
        public int PartialEntryCount => Success ? Plan.EntryCount : 0;
        public int PartialBudgetSpendCount => Success ? Plan.BudgetLedger.SpendEntryCount : 0;
        public int PartialMutationCount => 0;
        public int RetryLoopCount => 0;
        public int BudgetRollbackCount => failures.Any(value => value.Code ==
            GeneratedHazardEnemyPlacementFailureCode.BudgetOverflow || value.Code ==
            GeneratedHazardEnemyPlacementFailureCode.DuplicateBudgetSpend) ? 1 : 0;
    }

    public static class GeneratedHazardEnemyProtectionRule
    {
        public static GeneratedHazardEnemyProtectionProof Evaluate(
            GeneratedHazardEnemyPoolEntry pool,
            GeneratedHazardEnemyCandidateProtection candidate,
            ISet<string> occupiedReservationKeys)
        {
            var slot = candidate == null ? null : candidate.Slot;
            var categoryAccepted = pool != null && slot != null &&
                pool.CompatibleCategories.Contains(slot.Address.Category);
            var biomeAccepted = pool != null && candidate != null &&
                pool.BiomeAllowlist.Contains(candidate.Biome);
            var reservationKey = slot == null ? string.Empty : slot.ReservationKey;
            var occupiedAccepted = slot != null && (occupiedReservationKeys == null ||
                !occupiedReservationKeys.Contains(reservationKey));
            var routeAccepted = pool != null && candidate != null &&
                candidate.RouteClearance >= pool.RequiredRouteClearance &&
                !candidate.IntersectsMandatoryRouteSpine &&
                !candidate.IntersectsTraversalEnvelope &&
                !candidate.IntersectsRequiredLanding &&
                !candidate.IntersectsSpecialVillageEntryBuffer &&
                !candidate.IntersectsCriticalSocketBoundary;
            var rewardAccepted = candidate != null &&
                !candidate.IntersectsRewardApproachFloor;
            var recoveryAccepted = candidate != null &&
                !candidate.IntersectsDropRecoveryFloor && !candidate.IntersectsSafePocket;
            var safeAccepted = pool != null && candidate != null &&
                candidate.SafeRadius >= pool.MinimumSafeRadius;
            var neighborAccepted = pool != null && candidate != null &&
                candidate.NeighborRadius >= pool.MinimumNeighborRadius;
            var ticket = pool == null || candidate == null ? string.Empty :
                BakingCanonicalDigest.HashCanonicalLines(new[]
                {
                    "MAP18_04_DETERMINISTIC_TICKET_V1",
                    pool.StableToken,
                    candidate.StableToken,
                });
            return new GeneratedHazardEnemyProtectionProof(pool, candidate,
                categoryAccepted, biomeAccepted, occupiedAccepted, routeAccepted,
                rewardAccepted, recoveryAccepted, safeAccepted, neighborAccepted, ticket);
        }
    }

    public static class GeneratedHazardEnemyBudgetPlanner
    {
        public const string ExpectedPopulationPlanDigest =
            "4fc87b1c2699802761b9956aaf58fdc9ebbfaf6f32f33bdc9b7a776752cd109e";
        public const string ExpectedPopulationOccupiedSurfaceDigest =
            "f5556c9e609de1b71195c45473582009f99b5799cb03052da75682ed9c43e422";
        public const int ExpectedPopulationOccupiedSurfaceCount = 7;
        public const int ExpectedPopulationRemainingCandidateCount = 5;
        public const int RequiredLogicalGroupCount = 2;
        public const int RequiredBudgetScopeCount = 5;

        public static GeneratedHazardEnemyPlacementResult Place(
            GeneratedHazardEnemyPlacementRequest request)
        {
            var failures = new List<GeneratedHazardEnemyPlacementFailure>();
            if (request == null)
            {
                failures.Add(Failure(GeneratedHazardEnemyPlacementFailureCode.MissingRequest,
                    "MAP18_04", "REQUEST", "PRESENT", "MISSING", "Request is required."));
                return Result(null, failures);
            }

            ValidateUpstream(request, failures);
            ValidateCandidates(request, failures);
            ValidatePools(request, failures);
            ValidateBudgets(request, failures);
            if (request.AttemptedRuntimeSpawn || request.AttemptedDamage ||
                request.AttemptedPhysics || request.AttemptedEnemyAi || request.AttemptedCombat)
                failures.Add(Failure(
                    GeneratedHazardEnemyPlacementFailureCode
                        .AttemptedRuntimeSpawnDamagePhysicsAiOrCombat,
                    "MAP18_05_OR_LATER", "SIDE_EFFECT_REQUEST", "0/0/0/0/0",
                    string.Join("/", new[]
                    {
                        Flag(request.AttemptedRuntimeSpawn), Flag(request.AttemptedDamage),
                        Flag(request.AttemptedPhysics), Flag(request.AttemptedEnemyAi),
                        Flag(request.AttemptedCombat),
                    }), "MAP18_04 publishes only a logical placement and budget plan."));
            if (failures.Count > 0) return Result(null, failures);

            var occupiedKeys = new HashSet<string>(request.PopulationPlan.OccupiedSurface
                .Select(value => value.ReservationKey), StringComparer.Ordinal);
            var proofs = new List<GeneratedHazardEnemyProtectionProof>();
            var selectedProofs = new List<GeneratedHazardEnemyProtectionProof>();
            foreach (var pool in request.Pools.OrderBy(value => value))
            {
                var poolProofs = request.Candidates.Select(candidate =>
                    GeneratedHazardEnemyProtectionRule.Evaluate(pool, candidate, occupiedKeys))
                    .OrderBy(value => value).ToArray();
                proofs.AddRange(poolProofs);
                var selected = poolProofs.Where(value => value.Accepted)
                    .OrderBy(value => value.DeterministicTicket, StringComparer.Ordinal)
                    .ThenBy(value => value.Candidate).FirstOrDefault();
                if (selected == null)
                {
                    AddProtectionFailures(pool, poolProofs, failures);
                    failures.Add(MissingCandidate(pool));
                    continue;
                }
                selectedProofs.Add(selected);
                occupiedKeys.Add(selected.Candidate.Slot.ReservationKey);
            }
            if (failures.Count > 0) return Result(null, failures);

            ValidateSelected(request, selectedProofs, failures);
            if (failures.Count > 0) return Result(null, failures);

            var remainingByScope = request.BudgetLimits.ToDictionary(value => value.Scope,
                value => value.InitialBudget);
            var spendKeys = new HashSet<string>(request.ExistingBudgetSpendKeys,
                StringComparer.Ordinal);
            var spends = new List<GeneratedHazardEnemyBudgetSpend>();
            foreach (var proof in selectedProofs.OrderBy(value => value))
            {
                var placementKey = PlacementKey(proof);
                var scopeSpends = new List<GeneratedHazardEnemyBudgetScopeSpend>();
                foreach (GeneratedHazardEnemyBudgetScope scope in Enum.GetValues(
                             typeof(GeneratedHazardEnemyBudgetScope)))
                {
                    var spendKey = placementKey + "|" + scope.ToString().ToUpperInvariant();
                    if (!spendKeys.Add(spendKey))
                    {
                        failures.Add(Failure(
                            GeneratedHazardEnemyPlacementFailureCode.DuplicateBudgetSpend,
                            "MAP18_04_BUDGET_LEDGER", spendKey, "UNIQUE", "DUPLICATE",
                            "One placement and scope pair may be spent exactly once."));
                        continue;
                    }
                    var cost = proof.Pool.PressureCost;
                    var remaining = remainingByScope[scope];
                    if (remaining < cost)
                    {
                        failures.Add(Failure(
                            GeneratedHazardEnemyPlacementFailureCode.BudgetOverflow,
                            "MAP18_04_BUDGET_LEDGER", scope.ToString(),
                            ">=" + Number(cost), Number(remaining),
                            "Hierarchical pressure budget cannot become negative."));
                        continue;
                    }
                    remainingByScope[scope] = remaining - cost;
                    scopeSpends.Add(new GeneratedHazardEnemyBudgetScopeSpend(scope,
                        spendKey, cost, proof.Pool.ContentKey));
                }
                spends.Add(new GeneratedHazardEnemyBudgetSpend(placementKey, scopeSpends));
            }
            if (failures.Count > 0) return Result(null, failures);

            var ledger = new GeneratedHazardEnemyBudgetLedger(request.BudgetLimits, spends);
            var entries = selectedProofs.OrderBy(value => value).Select(proof =>
                new GeneratedHazardEnemyPlacementEntry(proof, spends.Single(value =>
                    string.Equals(value.PlacementKey, PlacementKey(proof),
                        StringComparison.Ordinal)))).ToArray();
            var plan = new GeneratedHazardEnemyPlacementPlan(request.PopulationPlan,
                request.Pools, entries, proofs, ledger);
            ValidateExpectedDigests(request, plan, failures);
            return failures.Count == 0 ? Result(plan, failures) : Result(null, failures);
        }

        private static void ValidateUpstream(
            GeneratedHazardEnemyPlacementRequest request,
            ICollection<GeneratedHazardEnemyPlacementFailure> failures)
        {
            var plan = request.PopulationPlan;
            if (plan == null)
            {
                failures.Add(Failure(
                    GeneratedHazardEnemyPlacementFailureCode.MissingPopulationPlan,
                    "MAP18_03", "POPULATION_PLAN", "PRESENT", "MISSING",
                    "Reviewed population placement plan is required."));
                return;
            }
            if (!string.Equals(request.ExpectedPopulationPlanDigest,
                    ExpectedPopulationPlanDigest, StringComparison.Ordinal) ||
                !string.Equals(plan.Digest, ExpectedPopulationPlanDigest,
                    StringComparison.Ordinal))
                failures.Add(Failure(
                    GeneratedHazardEnemyPlacementFailureCode.PopulationPlanDigestMismatch,
                    "MAP18_03", "POPULATION_PLAN_DIGEST", ExpectedPopulationPlanDigest,
                    request.ExpectedPopulationPlanDigest + "/" + plan.Digest,
                    "Population plan differs from reviewed MAP18_03 evidence."));
            if (!string.Equals(request.ExpectedOccupiedSurfaceDigest,
                    ExpectedPopulationOccupiedSurfaceDigest, StringComparison.Ordinal) ||
                !string.Equals(plan.OccupiedSurfaceDigest,
                    ExpectedPopulationOccupiedSurfaceDigest, StringComparison.Ordinal))
                failures.Add(Failure(
                    GeneratedHazardEnemyPlacementFailureCode.OccupiedSurfaceDigestMismatch,
                    "MAP18_03", "OCCUPIED_SURFACE_DIGEST",
                    ExpectedPopulationOccupiedSurfaceDigest,
                    request.ExpectedOccupiedSurfaceDigest + "/" + plan.OccupiedSurfaceDigest,
                    "Occupied surface differs from reviewed MAP18_03 evidence."));
            if (request.ExpectedOccupiedSurfaceCount != ExpectedPopulationOccupiedSurfaceCount ||
                plan.OccupiedSurfaceCount != ExpectedPopulationOccupiedSurfaceCount)
                failures.Add(Failure(
                    GeneratedHazardEnemyPlacementFailureCode.OccupiedSurfaceCountMismatch,
                    "MAP18_03", "OCCUPIED_SURFACE_COUNT",
                    Number(ExpectedPopulationOccupiedSurfaceCount),
                    Number(request.ExpectedOccupiedSurfaceCount) + "/" +
                    Number(plan.OccupiedSurfaceCount),
                    "All MAP18_02 and MAP18_03 reservations must be consumed."));
            if (request.ExpectedRemainingCandidateCount !=
                    ExpectedPopulationRemainingCandidateCount ||
                plan.RemainingCandidateCount != ExpectedPopulationRemainingCandidateCount)
                failures.Add(Failure(
                    GeneratedHazardEnemyPlacementFailureCode.RemainingCandidateCountMismatch,
                    "MAP18_03", "REMAINING_CANDIDATE_COUNT",
                    Number(ExpectedPopulationRemainingCandidateCount),
                    Number(request.ExpectedRemainingCandidateCount) + "/" +
                    Number(plan.RemainingCandidateCount),
                    "Hazard/enemy placement must start from the reviewed five candidates."));
            if (plan.OccupiedSurface.Select(value => value.ReservationKey)
                    .Distinct(StringComparer.Ordinal).Count() != plan.OccupiedSurfaceCount ||
                plan.OccupiedSurface.Select(value => value.StableSpawnId.Value)
                    .Distinct(StringComparer.Ordinal).Count() != plan.OccupiedSurfaceCount)
                failures.Add(Failure(
                    GeneratedHazardEnemyPlacementFailureCode.OccupiedSlotReuse,
                    "MAP18_03", "OCCUPIED_SURFACE", "UNIQUE_RESERVATIONS_AND_IDS",
                    "COLLISION", "Upstream occupied surface must already be collision-free."));
        }

        private static void ValidateCandidates(
            GeneratedHazardEnemyPlacementRequest request,
            ICollection<GeneratedHazardEnemyPlacementFailure> failures)
        {
            if (request.BiomeCatalog == null)
                failures.Add(Failure(
                    GeneratedHazardEnemyPlacementFailureCode.MissingBiomeCatalog,
                    "MAP10_04", "BIOME_PROFILE_CATALOG", "PRESENT", "MISSING",
                    "Typed biome authority is required."));
            if (request.NullCandidateCount > 0)
                failures.Add(Failure(
                    GeneratedHazardEnemyPlacementFailureCode.MissingCandidateProtection,
                    "MAP18_04_PROTECTION_SURFACE", "NULL_CANDIDATES", "0",
                    Number(request.NullCandidateCount),
                    "Candidate protection records cannot contain null."));
            if (request.PopulationPlan == null) return;
            foreach (var group in request.Candidates.Where(value => value.Slot != null)
                         .GroupBy(value => value.Slot.ReservationKey, StringComparer.Ordinal)
                         .Where(value => value.Count() > 1))
                failures.Add(Failure(
                    GeneratedHazardEnemyPlacementFailureCode.DuplicateCandidateProtection,
                    "MAP18_04_PROTECTION_SURFACE", group.Key, "1", Number(group.Count()),
                    "Each source slot requires one protection projection."));
            foreach (var slot in request.PopulationPlan.SourceSlotIndex.Entries)
            {
                var count = request.Candidates.Count(value => value.Slot != null &&
                    string.Equals(value.Slot.ReservationKey, slot.ReservationKey,
                        StringComparison.Ordinal));
                if (count == 0)
                    failures.Add(Failure(
                        GeneratedHazardEnemyPlacementFailureCode.MissingCandidateProtection,
                        "MAP18_04_PROTECTION_SURFACE", slot.ReservationKey, "1", "0",
                        "Every source slot requires explicit route and recovery evidence."));
            }
            foreach (var candidate in request.Candidates)
            {
                var validSlot = candidate.Slot != null &&
                    request.PopulationPlan.SourceSlotIndex.Entries.Any(value =>
                        string.Equals(value.ReservationKey,
                            candidate.Slot.ReservationKey, StringComparison.Ordinal) &&
                        string.Equals(value.StableSpawnId.Value,
                            candidate.Slot.StableSpawnId.Value, StringComparison.Ordinal));
                var validBiome = candidate.Biome.IsDefined && request.BiomeCatalog != null &&
                    request.BiomeCatalog.TryGetProfile(candidate.Biome, out _);
                if (!validSlot || !validBiome || candidate.RouteClearance < 0 ||
                    candidate.SafeRadius < 0 || candidate.NeighborRadius < 0)
                    failures.Add(Failure(
                        GeneratedHazardEnemyPlacementFailureCode.InvalidCandidateProtection,
                        "MAP18_04_PROTECTION_SURFACE", candidate.StableToken,
                        "KNOWN_SLOT_BIOME_AND_NON_NEGATIVE_RADII", "INVALID",
                        "Candidate protection projection is invalid."));
            }
        }

        private static void ValidatePools(
            GeneratedHazardEnemyPlacementRequest request,
            ICollection<GeneratedHazardEnemyPlacementFailure> failures)
        {
            if (request.NullPoolCount > 0)
                failures.Add(Failure(GeneratedHazardEnemyPlacementFailureCode.InvalidPool,
                    "MAP18_04_POOL", "NULL_POOLS", "0", Number(request.NullPoolCount),
                    "Pool entries cannot contain null."));
            foreach (GeneratedHazardEnemyContentKind kind in Enum.GetValues(
                         typeof(GeneratedHazardEnemyContentKind)))
            {
                var count = request.Pools.Count(value => value.Kind == kind);
                if (count == 0) failures.Add(MissingPool(kind));
                if (count > 1) failures.Add(Failure(
                    GeneratedHazardEnemyPlacementFailureCode.DuplicatePool,
                    "MAP18_04_POOL", kind.ToString(), "1", Number(count),
                    "Starter policy accepts exactly one pool per logical group."));
            }
            foreach (var pool in request.Pools)
            {
                if (pool.PoolKey == null || !pool.PoolKey.IsValid)
                    failures.Add(Failure(
                        GeneratedHazardEnemyPlacementFailureCode.InvalidPoolKey,
                        "MAP18_04_POOL", pool.ContentKey,
                        "VALID_NAMESPACE_AND_VERSION",
                        pool.PoolKey == null ? "MISSING" : pool.PoolKey.ToString(),
                        "Hazard/enemy pool keys must be explicit and versioned."));
                var validKind = pool.Kind >= GeneratedHazardEnemyContentKind.Hazard &&
                    pool.Kind <= GeneratedHazardEnemyContentKind.Enemy;
                var validCategories = pool.CompatibleCategories.Count > 0 &&
                    pool.CompatibleCategories.All(value => value ==
                        GeneratedContentSlotCategory.Hazard || value ==
                        GeneratedContentSlotCategory.Enemy);
                var validBiomes = request.BiomeCatalog != null &&
                    pool.BiomeAllowlist.Count > 0 && pool.BiomeAllowlist.All(value =>
                        value.IsDefined && request.BiomeCatalog.TryGetProfile(value, out _));
                if (!validKind || pool.ContentKey.Length == 0 || !validCategories ||
                    !validBiomes || pool.RequiredRouteClearance < 0 ||
                    pool.MinimumSafeRadius < 0 || pool.MinimumNeighborRadius < 0 ||
                    pool.PressureCost <= 0 || pool.MaximumWorldCount <= 0)
                    failures.Add(Failure(
                        GeneratedHazardEnemyPlacementFailureCode.InvalidPool,
                        "MAP18_04_POOL", pool.ContentKey,
                        "TYPED_KIND_CATEGORY_BIOME_CLEARANCE_RADIUS_COST_AND_CAP",
                        "INVALID", "Pool contains an invalid starter pressure rule."));
            }
        }

        private static void ValidateBudgets(
            GeneratedHazardEnemyPlacementRequest request,
            ICollection<GeneratedHazardEnemyPlacementFailure> failures)
        {
            if (request.NullBudgetCount > 0)
                failures.Add(Failure(
                    GeneratedHazardEnemyPlacementFailureCode.InvalidBudgetScope,
                    "MAP18_04_BUDGET_LEDGER", "NULL_BUDGETS", "0",
                    Number(request.NullBudgetCount), "Budget limits cannot contain null."));
            foreach (GeneratedHazardEnemyBudgetScope scope in Enum.GetValues(
                         typeof(GeneratedHazardEnemyBudgetScope)))
            {
                var count = request.BudgetLimits.Count(value => value.Scope == scope);
                if (count == 0)
                    failures.Add(Failure(
                        GeneratedHazardEnemyPlacementFailureCode.MissingBudgetScope,
                        "MAP18_04_BUDGET_LEDGER", scope.ToString(), "1", "0",
                        "World through slot scopes are all mandatory."));
                if (count > 1)
                    failures.Add(Failure(
                        GeneratedHazardEnemyPlacementFailureCode.DuplicateBudgetScope,
                        "MAP18_04_BUDGET_LEDGER", scope.ToString(), "1", Number(count),
                        "Each hierarchical scope requires one starter limit."));
            }
            foreach (var limit in request.BudgetLimits)
                if (limit.Scope < GeneratedHazardEnemyBudgetScope.World ||
                    limit.Scope > GeneratedHazardEnemyBudgetScope.Slot ||
                    limit.InitialBudget < 0)
                    failures.Add(Failure(
                        GeneratedHazardEnemyPlacementFailureCode.InvalidBudgetScope,
                        "MAP18_04_BUDGET_LEDGER", limit.StableToken,
                        "KNOWN_SCOPE_AND_NON_NEGATIVE_INITIAL", "INVALID",
                        "Budget scope or initial pressure is invalid."));
        }

        private static void ValidateSelected(
            GeneratedHazardEnemyPlacementRequest request,
            IReadOnlyCollection<GeneratedHazardEnemyProtectionProof> selected,
            ICollection<GeneratedHazardEnemyPlacementFailure> failures)
        {
            var upstreamKeys = new HashSet<string>(request.PopulationPlan.OccupiedSurface
                .Select(value => value.ReservationKey), StringComparer.Ordinal);
            foreach (var proof in selected)
            {
                var slot = proof.Candidate.Slot;
                if (upstreamKeys.Contains(slot.ReservationKey))
                    failures.Add(Failure(
                        GeneratedHazardEnemyPlacementFailureCode.OccupiedSlotReuse,
                        "MAP18_03_OCCUPIED_SURFACE", slot.ReservationKey,
                        "UNOCCUPIED", "OCCUPIED",
                        "Hazard/enemy placement cannot reuse seven upstream reservations."));
                if (request.ExistingReservationKeys.Contains(slot.ReservationKey,
                        StringComparer.Ordinal))
                    failures.Add(Failure(
                        GeneratedHazardEnemyPlacementFailureCode.ReservationKeyCollision,
                        "MAP18_05_OCCUPIED_SURFACE", slot.ReservationKey,
                        "UNRESERVED", "PRE_EXISTING",
                        "Selected reservation collides with another consumer."));
                if (request.ExistingStableSpawnIds.Contains(slot.StableSpawnId.Value,
                        StringComparer.Ordinal))
                    failures.Add(Failure(
                        GeneratedHazardEnemyPlacementFailureCode.StableSpawnIdCollision,
                        GeneratedStableSpawnIdFactory.Namespace, slot.StableSpawnId.Value,
                        "UNIQUE", "PRE_EXISTING",
                        "Selected stable spawn ID collides with another consumer."));
            }
            foreach (var group in selected.GroupBy(value =>
                         value.Candidate.Slot.ReservationKey, StringComparer.Ordinal)
                         .Where(value => value.Count() > 1))
                failures.Add(Failure(
                    GeneratedHazardEnemyPlacementFailureCode.ReservationKeyCollision,
                    "MAP18_05_OCCUPIED_SURFACE", group.Key, "1", Number(group.Count()),
                    "Hazard and enemy cannot share a physical slot."));
            foreach (var group in selected.GroupBy(value =>
                         value.Candidate.Slot.StableSpawnId.Value, StringComparer.Ordinal)
                         .Where(value => value.Count() > 1))
                failures.Add(Failure(
                    GeneratedHazardEnemyPlacementFailureCode.StableSpawnIdCollision,
                    GeneratedStableSpawnIdFactory.Namespace, group.Key, "1",
                    Number(group.Count()), "Hazard and enemy stable IDs must be unique."));
        }

        private static void AddProtectionFailures(
            GeneratedHazardEnemyPoolEntry pool,
            IEnumerable<GeneratedHazardEnemyProtectionProof> source,
            ICollection<GeneratedHazardEnemyPlacementFailure> failures)
        {
            var relevant = source.Where(value => value.CategoryAccepted &&
                value.BiomeAccepted && value.OccupiedSurfaceAccepted).ToArray();
            if (relevant.Any(value => !value.RouteAccepted))
                failures.Add(Failure(
                    GeneratedHazardEnemyPlacementFailureCode.RouteProtectionViolation,
                    "MANDATORY_ROUTE_AND_TRAVERSAL_PROTECTION", pool.ContentKey,
                    "NO_INTERSECTION_AND_REQUIRED_CLEARANCE", "REJECTED",
                    "Mandatory spine, envelope, landing, entry, or socket is protected."));
            if (relevant.Any(value => !value.RewardAccepted))
                failures.Add(Failure(
                    GeneratedHazardEnemyPlacementFailureCode
                        .RewardApproachProtectionViolation,
                    "REWARD_APPROACH_PROTECTION", pool.ContentKey,
                    "NO_INTERSECTION", "REJECTED",
                    "Reward approach floor is protected."));
            if (relevant.Any(value => !value.RecoveryAccepted))
                failures.Add(Failure(
                    GeneratedHazardEnemyPlacementFailureCode
                        .RecoveryFloorProtectionViolation,
                    "RECOVERY_FLOOR_PROTECTION", pool.ContentKey,
                    "NO_INTERSECTION", "REJECTED",
                    "Drop recovery floor and safe pocket are protected."));
            if (relevant.Any(value => !value.SafeRadiusAccepted))
                failures.Add(Failure(
                    GeneratedHazardEnemyPlacementFailureCode.SafeRadiusViolation,
                    "SAFE_RADIUS_PROTECTION", pool.ContentKey,
                    ">=" + Number(pool.MinimumSafeRadius), "REJECTED",
                    "Candidate is too close to a protected safe surface."));
            if (relevant.Any(value => !value.NeighborRadiusAccepted))
                failures.Add(Failure(
                    GeneratedHazardEnemyPlacementFailureCode.NeighborRadiusViolation,
                    "NEIGHBOR_RADIUS_PROTECTION", pool.ContentKey,
                    ">=" + Number(pool.MinimumNeighborRadius), "REJECTED",
                    "Candidate violates the declared neighbor radius."));
            if (source.Any(value => !value.OccupiedSurfaceAccepted))
                failures.Add(Failure(
                    GeneratedHazardEnemyPlacementFailureCode.OccupiedSlotReuse,
                    "MAP18_03_OCCUPIED_SURFACE", pool.ContentKey,
                    "UNOCCUPIED_CANDIDATE", "OCCUPIED_CANDIDATE_REJECTED",
                    "Upstream occupied reservations are not candidates."));
        }

        private static void ValidateExpectedDigests(
            GeneratedHazardEnemyPlacementRequest request,
            GeneratedHazardEnemyPlacementPlan plan,
            ICollection<GeneratedHazardEnemyPlacementFailure> failures)
        {
            if (!string.IsNullOrEmpty(request.ExpectedPlanDigest) &&
                !string.Equals(request.ExpectedPlanDigest, plan.Digest,
                    StringComparison.Ordinal))
                failures.Add(Failure(
                    GeneratedHazardEnemyPlacementFailureCode.PlanDigestMismatch,
                    "MAP18_04", "PLAN_DIGEST", request.ExpectedPlanDigest,
                    plan.Digest, "Computed plan differs from expected evidence."));
            if (!string.IsNullOrEmpty(request.ExpectedFinalOccupiedSurfaceDigest) &&
                !string.Equals(request.ExpectedFinalOccupiedSurfaceDigest,
                    plan.OccupiedSurfaceDigest, StringComparison.Ordinal))
                failures.Add(Failure(
                    GeneratedHazardEnemyPlacementFailureCode
                        .FinalOccupiedSurfaceDigestMismatch,
                    "MAP18_05_OCCUPIED_SURFACE", "OCCUPIED_SURFACE_DIGEST",
                    request.ExpectedFinalOccupiedSurfaceDigest,
                    plan.OccupiedSurfaceDigest,
                    "Computed MAP18_05 occupied surface differs from expected evidence."));
            if (!string.IsNullOrEmpty(request.ExpectedBudgetLedgerDigest) &&
                !string.Equals(request.ExpectedBudgetLedgerDigest,
                    plan.BudgetLedger.Digest, StringComparison.Ordinal))
                failures.Add(Failure(
                    GeneratedHazardEnemyPlacementFailureCode.BudgetLedgerDigestMismatch,
                    "MAP18_04_BUDGET_LEDGER", "BUDGET_LEDGER_DIGEST",
                    request.ExpectedBudgetLedgerDigest, plan.BudgetLedger.Digest,
                    "Computed budget ledger differs from expected evidence."));
        }

        private static GeneratedHazardEnemyPlacementFailure MissingPool(
            GeneratedHazardEnemyContentKind kind) => Failure(kind ==
                GeneratedHazardEnemyContentKind.Hazard
                    ? GeneratedHazardEnemyPlacementFailureCode.MissingHazardPool
                    : GeneratedHazardEnemyPlacementFailureCode.MissingEnemyPool,
                "MAP18_04_POOL", kind.ToString(), "ONE_POOL", "0",
                "Required logical pool is missing.");

        private static GeneratedHazardEnemyPlacementFailure MissingCandidate(
            GeneratedHazardEnemyPoolEntry pool) => Failure(pool.Kind ==
                GeneratedHazardEnemyContentKind.Hazard
                    ? GeneratedHazardEnemyPlacementFailureCode.MissingHazardCandidate
                    : GeneratedHazardEnemyPlacementFailureCode.MissingEnemyCandidate,
                "MAP18_04_SELECTION", pool.ContentKey, "ONE_SAFE_CANDIDATE", "0",
                "No unoccupied candidate satisfies protection and radius policy.");

        private static string PlacementKey(GeneratedHazardEnemyProtectionProof proof) =>
            "HAZARD_ENEMY_PLACEMENT_KEY_V1|" +
            proof.Pool.Kind.ToString().ToUpperInvariant() + "|" +
            proof.Pool.ContentKey + "|" + proof.Candidate.Slot.ReservationKey;
        private static GeneratedHazardEnemyPlacementFailure Failure(
            GeneratedHazardEnemyPlacementFailureCode code,
            string owner,
            string key,
            string expected,
            string actual,
            string reason) => new GeneratedHazardEnemyPlacementFailure(
                code, owner, key, expected, actual, reason);
        private static GeneratedHazardEnemyPlacementResult Result(
            GeneratedHazardEnemyPlacementPlan plan,
            IEnumerable<GeneratedHazardEnemyPlacementFailure> failures) =>
            new GeneratedHazardEnemyPlacementResult(plan, failures);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Flag(bool value) => value ? "1" : "0";
    }
}
