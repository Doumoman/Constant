using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Boundaries;

namespace StarNight.Map.WorldGeneration.Population
{
    public enum GeneratedHazardEnemyContentKind
    {
        Hazard = 1,
        Enemy = 2,
    }

    public enum GeneratedHazardEnemyBudgetScope
    {
        World = 1,
        Patch = 2,
        Sector = 3,
        Cluster = 4,
        Slot = 5,
    }

    public sealed class GeneratedHazardEnemyPoolEntry :
        IComparable<GeneratedHazardEnemyPoolEntry>
    {
        private readonly ReadOnlyCollection<GeneratedContentSlotCategory> categories;
        private readonly ReadOnlyCollection<MoonpalaceBiomeId> biomeAllowlist;

        public GeneratedHazardEnemyPoolEntry(
            GeneratedHazardEnemyContentKind kind,
            string contentKey,
            GeneratedContentPoolKey poolKey,
            IEnumerable<GeneratedContentSlotCategory> compatibleCategories,
            IEnumerable<MoonpalaceBiomeId> allowedBiomes,
            int requiredRouteClearance,
            int minimumSafeRadius,
            int minimumNeighborRadius,
            int pressureCost,
            int maximumWorldCount)
        {
            Kind = kind;
            ContentKey = Normalize(contentKey);
            PoolKey = poolKey;
            categories = new ReadOnlyCollection<GeneratedContentSlotCategory>(
                (compatibleCategories ?? Array.Empty<GeneratedContentSlotCategory>())
                .Distinct().OrderBy(value => value).ToArray());
            biomeAllowlist = new ReadOnlyCollection<MoonpalaceBiomeId>(
                (allowedBiomes ?? Array.Empty<MoonpalaceBiomeId>())
                .Distinct().OrderBy(value => value.Order).ToArray());
            RequiredRouteClearance = requiredRouteClearance;
            MinimumSafeRadius = minimumSafeRadius;
            MinimumNeighborRadius = minimumNeighborRadius;
            PressureCost = pressureCost;
            MaximumWorldCount = maximumWorldCount;
            StableToken = string.Join("|", new[]
            {
                "HAZARD_ENEMY_POOL_V1",
                "KIND=" + Kind.ToString().ToUpperInvariant(),
                "CONTENT=" + GeneratedContentPoolKey.Part(ContentKey),
                "POOL=" + (PoolKey == null ? "MISSING" : PoolKey.StableToken),
                "CATEGORIES=" + string.Join(",", categories.Select(value =>
                    value.ToString().ToUpperInvariant())),
                "BIOMES=" + string.Join(",", biomeAllowlist.Select(value =>
                    value.CanonicalId)),
                "ROUTE_CLEARANCE=" + Number(RequiredRouteClearance),
                "SAFE_RADIUS=" + Number(MinimumSafeRadius),
                "NEIGHBOR_RADIUS=" + Number(MinimumNeighborRadius),
                "PRESSURE_COST=" + Number(PressureCost),
                "MAX_WORLD_COUNT=" + Number(MaximumWorldCount),
            });
        }

        public GeneratedHazardEnemyContentKind Kind { get; }
        public string ContentKey { get; }
        public GeneratedContentPoolKey PoolKey { get; }
        public IReadOnlyList<GeneratedContentSlotCategory> CompatibleCategories => categories;
        public IReadOnlyList<MoonpalaceBiomeId> BiomeAllowlist => biomeAllowlist;
        public int RequiredRouteClearance { get; }
        public int MinimumSafeRadius { get; }
        public int MinimumNeighborRadius { get; }
        public int PressureCost { get; }
        public int MaximumWorldCount { get; }
        public string StableToken { get; }

        public int CompareTo(GeneratedHazardEnemyPoolEntry other) => other == null
            ? -1 : Kind.CompareTo(other.Kind);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? string.Empty : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public static class GeneratedHazardEnemyPoolCatalog
    {
        public static IReadOnlyList<GeneratedHazardEnemyPoolEntry> CreateDefault(
            IEnumerable<MoonpalaceBiomeId> sourceBiomes)
        {
            var biomes = (sourceBiomes ?? Array.Empty<MoonpalaceBiomeId>())
                .Distinct().OrderBy(value => value.Order).ToArray();
            var pressureBiomes = biomes.Where(value =>
                value == MoonpalaceBiomeId.MoonCrater ||
                value == MoonpalaceBiomeId.AbandonedMill).ToArray();
            return new ReadOnlyCollection<GeneratedHazardEnemyPoolEntry>(new[]
            {
                new GeneratedHazardEnemyPoolEntry(
                    GeneratedHazardEnemyContentKind.Hazard,
                    "HAZARD_PRESSURE_GENERIC",
                    new GeneratedContentPoolKey("POPULATION_HAZARD", "V1"),
                    new[] { GeneratedContentSlotCategory.Hazard }, pressureBiomes,
                    1, 2, 3, 2, 1),
                new GeneratedHazardEnemyPoolEntry(
                    GeneratedHazardEnemyContentKind.Enemy,
                    "ENEMY_PRESSURE_GENERIC",
                    new GeneratedContentPoolKey("POPULATION_ENEMY", "V1"),
                    new[] { GeneratedContentSlotCategory.Enemy }, pressureBiomes,
                    1, 2, 3, 1, 1),
            }.OrderBy(value => value).ToArray());
        }
    }

    public sealed class GeneratedHazardEnemyCandidateProtection :
        IComparable<GeneratedHazardEnemyCandidateProtection>
    {
        public GeneratedHazardEnemyCandidateProtection(
            GeneratedContentSlotIndexEntry slot,
            MoonpalaceBiomeId biome,
            int routeClearance,
            int safeRadius,
            int neighborRadius,
            bool intersectsMandatoryRouteSpine = false,
            bool intersectsTraversalEnvelope = false,
            bool intersectsRequiredLanding = false,
            bool intersectsDropRecoveryFloor = false,
            bool intersectsRewardApproachFloor = false,
            bool intersectsSpecialVillageEntryBuffer = false,
            bool intersectsSafePocket = false,
            bool intersectsCriticalSocketBoundary = false)
        {
            Slot = slot;
            Biome = biome;
            RouteClearance = routeClearance;
            SafeRadius = safeRadius;
            NeighborRadius = neighborRadius;
            IntersectsMandatoryRouteSpine = intersectsMandatoryRouteSpine;
            IntersectsTraversalEnvelope = intersectsTraversalEnvelope;
            IntersectsRequiredLanding = intersectsRequiredLanding;
            IntersectsDropRecoveryFloor = intersectsDropRecoveryFloor;
            IntersectsRewardApproachFloor = intersectsRewardApproachFloor;
            IntersectsSpecialVillageEntryBuffer = intersectsSpecialVillageEntryBuffer;
            IntersectsSafePocket = intersectsSafePocket;
            IntersectsCriticalSocketBoundary = intersectsCriticalSocketBoundary;
            StableToken = string.Join("|", new[]
            {
                "HAZARD_ENEMY_PROTECTION_CONTEXT_V1",
                "SLOT=" + (slot == null ? "MISSING" : slot.StableToken),
                "BIOME=" + biome.CanonicalId,
                "ROUTE_CLEARANCE=" + Number(routeClearance),
                "SAFE_RADIUS=" + Number(safeRadius),
                "NEIGHBOR_RADIUS=" + Number(neighborRadius),
                "MANDATORY_ROUTE=" + Flag(intersectsMandatoryRouteSpine),
                "TRAVERSAL_ENVELOPE=" + Flag(intersectsTraversalEnvelope),
                "REQUIRED_LANDING=" + Flag(intersectsRequiredLanding),
                "DROP_RECOVERY=" + Flag(intersectsDropRecoveryFloor),
                "REWARD_APPROACH=" + Flag(intersectsRewardApproachFloor),
                "SPECIAL_ENTRY_BUFFER=" + Flag(intersectsSpecialVillageEntryBuffer),
                "SAFE_POCKET=" + Flag(intersectsSafePocket),
                "CRITICAL_SOCKET=" + Flag(intersectsCriticalSocketBoundary),
            });
        }

        public GeneratedContentSlotIndexEntry Slot { get; }
        public MoonpalaceBiomeId Biome { get; }
        public int RouteClearance { get; }
        public int SafeRadius { get; }
        public int NeighborRadius { get; }
        public bool IntersectsMandatoryRouteSpine { get; }
        public bool IntersectsTraversalEnvelope { get; }
        public bool IntersectsRequiredLanding { get; }
        public bool IntersectsDropRecoveryFloor { get; }
        public bool IntersectsRewardApproachFloor { get; }
        public bool IntersectsSpecialVillageEntryBuffer { get; }
        public bool IntersectsSafePocket { get; }
        public bool IntersectsCriticalSocketBoundary { get; }
        public string StableToken { get; }

        public int CompareTo(GeneratedHazardEnemyCandidateProtection other) => other == null
            ? -1 : string.Compare(Slot == null ? string.Empty : Slot.DeterministicOrderKey,
                other.Slot == null ? string.Empty : other.Slot.DeterministicOrderKey,
                StringComparison.Ordinal);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Flag(bool value) => value ? "1" : "0";
    }

    public sealed class GeneratedHazardEnemyProtectionProof :
        IComparable<GeneratedHazardEnemyProtectionProof>
    {
        internal GeneratedHazardEnemyProtectionProof(
            GeneratedHazardEnemyPoolEntry pool,
            GeneratedHazardEnemyCandidateProtection candidate,
            bool categoryAccepted,
            bool biomeAccepted,
            bool occupiedSurfaceAccepted,
            bool routeAccepted,
            bool rewardAccepted,
            bool recoveryAccepted,
            bool safeRadiusAccepted,
            bool neighborRadiusAccepted,
            string deterministicTicket)
        {
            Pool = pool;
            Candidate = candidate;
            CategoryAccepted = categoryAccepted;
            BiomeAccepted = biomeAccepted;
            OccupiedSurfaceAccepted = occupiedSurfaceAccepted;
            RouteAccepted = routeAccepted;
            RewardAccepted = rewardAccepted;
            RecoveryAccepted = recoveryAccepted;
            SafeRadiusAccepted = safeRadiusAccepted;
            NeighborRadiusAccepted = neighborRadiusAccepted;
            DeterministicTicket = deterministicTicket ?? string.Empty;
            StableToken = string.Join("|", new[]
            {
                "HAZARD_ENEMY_PROTECTION_PROOF_V1",
                pool == null ? "MISSING_POOL" : pool.StableToken,
                candidate == null ? "MISSING_CANDIDATE" : candidate.StableToken,
                "CATEGORY=" + Flag(categoryAccepted),
                "BIOME=" + Flag(biomeAccepted),
                "OCCUPIED=" + Flag(occupiedSurfaceAccepted),
                "ROUTE=" + Flag(routeAccepted),
                "REWARD=" + Flag(rewardAccepted),
                "RECOVERY=" + Flag(recoveryAccepted),
                "SAFE=" + Flag(safeRadiusAccepted),
                "NEIGHBOR=" + Flag(neighborRadiusAccepted),
                "TICKET=" + DeterministicTicket,
            });
        }

        public GeneratedHazardEnemyPoolEntry Pool { get; }
        public GeneratedHazardEnemyCandidateProtection Candidate { get; }
        public bool CategoryAccepted { get; }
        public bool BiomeAccepted { get; }
        public bool OccupiedSurfaceAccepted { get; }
        public bool RouteAccepted { get; }
        public bool RewardAccepted { get; }
        public bool RecoveryAccepted { get; }
        public bool SafeRadiusAccepted { get; }
        public bool NeighborRadiusAccepted { get; }
        public string DeterministicTicket { get; }
        public string StableToken { get; }
        public bool Accepted => CategoryAccepted && BiomeAccepted &&
            OccupiedSurfaceAccepted && RouteAccepted && RewardAccepted &&
            RecoveryAccepted && SafeRadiusAccepted && NeighborRadiusAccepted;

        public int CompareTo(GeneratedHazardEnemyProtectionProof other)
        {
            if (other == null) return -1;
            var value = Pool.Kind.CompareTo(other.Pool.Kind);
            return value != 0 ? value : Candidate.CompareTo(other.Candidate);
        }

        private static string Flag(bool value) => value ? "1" : "0";
    }

    public sealed class GeneratedHazardEnemyBudgetLimit :
        IComparable<GeneratedHazardEnemyBudgetLimit>
    {
        public GeneratedHazardEnemyBudgetLimit(
            GeneratedHazardEnemyBudgetScope scope,
            int initialBudget)
        {
            Scope = scope;
            InitialBudget = initialBudget;
            StableToken = "HAZARD_ENEMY_BUDGET_LIMIT_V1|" +
                scope.ToString().ToUpperInvariant() + "|" + Number(initialBudget);
        }

        public GeneratedHazardEnemyBudgetScope Scope { get; }
        public int InitialBudget { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedHazardEnemyBudgetLimit other) => other == null
            ? -1 : Scope.CompareTo(other.Scope);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public static class GeneratedHazardEnemyBudgetCatalog
    {
        public static IReadOnlyList<GeneratedHazardEnemyBudgetLimit> CreateStarter()
        {
            return new ReadOnlyCollection<GeneratedHazardEnemyBudgetLimit>(new[]
            {
                new GeneratedHazardEnemyBudgetLimit(GeneratedHazardEnemyBudgetScope.World, 12),
                new GeneratedHazardEnemyBudgetLimit(GeneratedHazardEnemyBudgetScope.Patch, 10),
                new GeneratedHazardEnemyBudgetLimit(GeneratedHazardEnemyBudgetScope.Sector, 8),
                new GeneratedHazardEnemyBudgetLimit(GeneratedHazardEnemyBudgetScope.Cluster, 6),
                new GeneratedHazardEnemyBudgetLimit(GeneratedHazardEnemyBudgetScope.Slot, 4),
            });
        }
    }

    public sealed class GeneratedHazardEnemyBudgetScopeSpend :
        IComparable<GeneratedHazardEnemyBudgetScopeSpend>
    {
        internal GeneratedHazardEnemyBudgetScopeSpend(
            GeneratedHazardEnemyBudgetScope scope,
            string spendKey,
            int amount,
            string reason)
        {
            Scope = scope;
            SpendKey = spendKey ?? string.Empty;
            Amount = amount;
            Reason = reason ?? string.Empty;
            StableToken = string.Join("|", new[]
            {
                "HAZARD_ENEMY_SCOPE_SPEND_V1",
                Scope.ToString().ToUpperInvariant(), SpendKey,
                Number(Amount), Reason,
            });
        }

        public GeneratedHazardEnemyBudgetScope Scope { get; }
        public string SpendKey { get; }
        public int Amount { get; }
        public string Reason { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedHazardEnemyBudgetScopeSpend other) => other == null
            ? -1 : Scope.CompareTo(other.Scope);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedHazardEnemyBudgetSpend :
        IComparable<GeneratedHazardEnemyBudgetSpend>
    {
        private readonly ReadOnlyCollection<GeneratedHazardEnemyBudgetScopeSpend> scopeSpends;

        internal GeneratedHazardEnemyBudgetSpend(
            string placementKey,
            IEnumerable<GeneratedHazardEnemyBudgetScopeSpend> sourceSpends)
        {
            PlacementKey = placementKey ?? string.Empty;
            scopeSpends = new ReadOnlyCollection<GeneratedHazardEnemyBudgetScopeSpend>(
                (sourceSpends ?? Array.Empty<GeneratedHazardEnemyBudgetScopeSpend>())
                .OrderBy(value => value).ToArray());
            StableToken = "HAZARD_ENEMY_BUDGET_SPEND_V1|" + PlacementKey + "|" +
                string.Join("|", scopeSpends.Select(value => value.StableToken));
        }

        public string PlacementKey { get; }
        public IReadOnlyList<GeneratedHazardEnemyBudgetScopeSpend> ScopeSpends => scopeSpends;
        public string StableToken { get; }
        public int CompareTo(GeneratedHazardEnemyBudgetSpend other) => other == null
            ? -1 : string.Compare(PlacementKey, other.PlacementKey, StringComparison.Ordinal);
    }

    public sealed class GeneratedHazardEnemyBudgetBalance :
        IComparable<GeneratedHazardEnemyBudgetBalance>
    {
        internal GeneratedHazardEnemyBudgetBalance(
            GeneratedHazardEnemyBudgetScope scope,
            int initial,
            int spent)
        {
            Scope = scope;
            Initial = initial;
            Spent = spent;
            Remaining = initial - spent;
            StableToken = string.Join("|", new[]
            {
                "HAZARD_ENEMY_BUDGET_BALANCE_V1",
                Scope.ToString().ToUpperInvariant(), Number(Initial),
                Number(Spent), Number(Remaining),
            });
        }

        public GeneratedHazardEnemyBudgetScope Scope { get; }
        public int Initial { get; }
        public int Spent { get; }
        public int Remaining { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedHazardEnemyBudgetBalance other) => other == null
            ? -1 : Scope.CompareTo(other.Scope);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedHazardEnemyBudgetLedger
    {
        private readonly ReadOnlyCollection<GeneratedHazardEnemyBudgetBalance> balances;
        private readonly ReadOnlyCollection<GeneratedHazardEnemyBudgetSpend> spends;

        internal GeneratedHazardEnemyBudgetLedger(
            IEnumerable<GeneratedHazardEnemyBudgetLimit> limits,
            IEnumerable<GeneratedHazardEnemyBudgetSpend> sourceSpends)
        {
            spends = new ReadOnlyCollection<GeneratedHazardEnemyBudgetSpend>(
                (sourceSpends ?? Array.Empty<GeneratedHazardEnemyBudgetSpend>())
                .OrderBy(value => value).ToArray());
            balances = new ReadOnlyCollection<GeneratedHazardEnemyBudgetBalance>(
                (limits ?? Array.Empty<GeneratedHazardEnemyBudgetLimit>())
                .OrderBy(value => value).Select(limit => new GeneratedHazardEnemyBudgetBalance(
                    limit.Scope, limit.InitialBudget, spends.SelectMany(value => value.ScopeSpends)
                        .Where(value => value.Scope == limit.Scope).Sum(value => value.Amount)))
                .ToArray());
            Digest = BakingCanonicalDigest.HashCanonicalLines(
                balances.Select(value => value.StableToken)
                    .Concat(spends.Select(value => value.StableToken)));
        }

        public IReadOnlyList<GeneratedHazardEnemyBudgetBalance> Balances => balances;
        public IReadOnlyList<GeneratedHazardEnemyBudgetSpend> Spends => spends;
        public int ScopeCount => balances.Count;
        public int SpendEntryCount => spends.Count;
        public int ScopeSpendRecordCount => spends.Sum(value => value.ScopeSpends.Count);
        public int DuplicateSpendKeyCount => spends.SelectMany(value => value.ScopeSpends)
            .GroupBy(value => value.SpendKey, StringComparer.Ordinal)
            .Count(value => value.Count() > 1);
        public int NegativeRemainingCount => balances.Count(value => value.Remaining < 0);
        public string Digest { get; }
        public GeneratedHazardEnemyBudgetBalance Balance(
            GeneratedHazardEnemyBudgetScope scope) => balances.Single(value =>
                value.Scope == scope);
    }

    public sealed class GeneratedHazardEnemyPlacementEntry :
        IComparable<GeneratedHazardEnemyPlacementEntry>
    {
        internal GeneratedHazardEnemyPlacementEntry(
            GeneratedHazardEnemyProtectionProof protectionProof,
            GeneratedHazardEnemyBudgetSpend budgetSpend)
        {
            ProtectionProof = protectionProof;
            PoolEntry = protectionProof.Pool;
            Candidate = protectionProof.Candidate;
            SelectedSlot = Candidate.Slot;
            Kind = PoolEntry.Kind;
            ContentKey = PoolEntry.ContentKey;
            StableSpawnId = SelectedSlot.StableSpawnId;
            ReservationKey = SelectedSlot.ReservationKey;
            BudgetSpend = budgetSpend;
            StableToken = string.Join("|", new[]
            {
                "HAZARD_ENEMY_PLACEMENT_V1", PoolEntry.StableToken,
                "SLOT=" + SelectedSlot.StableToken,
                "SPAWN_ID=" + StableSpawnId.Value,
                "RESERVATION=" + ReservationKey,
                "PROTECTION=" + protectionProof.StableToken,
                "BUDGET=" + budgetSpend.StableToken,
            });
        }

        public GeneratedHazardEnemyContentKind Kind { get; }
        public string ContentKey { get; }
        public GeneratedHazardEnemyPoolEntry PoolEntry { get; }
        public GeneratedHazardEnemyCandidateProtection Candidate { get; }
        public GeneratedContentSlotIndexEntry SelectedSlot { get; }
        public GeneratedStableSpawnId StableSpawnId { get; }
        public string ReservationKey { get; }
        public GeneratedHazardEnemyProtectionProof ProtectionProof { get; }
        public GeneratedHazardEnemyBudgetSpend BudgetSpend { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedHazardEnemyPlacementEntry other) => other == null
            ? -1 : Kind.CompareTo(other.Kind);
    }

    public sealed class GeneratedHazardEnemyOccupiedReservation :
        IComparable<GeneratedHazardEnemyOccupiedReservation>
    {
        internal GeneratedHazardEnemyOccupiedReservation(
            string owner,
            string contentKey,
            string reservationKey,
            GeneratedStableSpawnId stableSpawnId)
        {
            Owner = owner ?? string.Empty;
            ContentKey = contentKey ?? string.Empty;
            ReservationKey = reservationKey ?? string.Empty;
            StableSpawnId = stableSpawnId;
            StableToken = string.Join("|", new[]
            {
                "HAZARD_ENEMY_OCCUPIED_RESERVATION_V1", Owner, ContentKey,
                ReservationKey, StableSpawnId == null ? "MISSING" : StableSpawnId.Value,
            });
        }

        public string Owner { get; }
        public string ContentKey { get; }
        public string ReservationKey { get; }
        public GeneratedStableSpawnId StableSpawnId { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedHazardEnemyOccupiedReservation other) => other == null
            ? -1 : string.Compare(ReservationKey, other.ReservationKey, StringComparison.Ordinal);
    }

    public sealed class GeneratedHazardEnemyPlacementPlan
    {
        private readonly ReadOnlyCollection<GeneratedHazardEnemyPoolEntry> pools;
        private readonly ReadOnlyCollection<GeneratedHazardEnemyPlacementEntry> entries;
        private readonly ReadOnlyCollection<GeneratedHazardEnemyProtectionProof> protectionProofs;
        private readonly ReadOnlyCollection<GeneratedHazardEnemyOccupiedReservation> occupiedSurface;
        private readonly ReadOnlyCollection<GeneratedContentSlotIndexEntry> remainingCandidates;

        internal GeneratedHazardEnemyPlacementPlan(
            GeneratedPopulationPlacementPlan populationPlan,
            IEnumerable<GeneratedHazardEnemyPoolEntry> sourcePools,
            IEnumerable<GeneratedHazardEnemyPlacementEntry> sourceEntries,
            IEnumerable<GeneratedHazardEnemyProtectionProof> sourceProofs,
            GeneratedHazardEnemyBudgetLedger budgetLedger)
        {
            PopulationPlan = populationPlan;
            pools = Freeze(sourcePools);
            entries = Freeze(sourceEntries);
            protectionProofs = Freeze(sourceProofs);
            BudgetLedger = budgetLedger;
            occupiedSurface = new ReadOnlyCollection<GeneratedHazardEnemyOccupiedReservation>(
                populationPlan.OccupiedSurface.Select(value =>
                    new GeneratedHazardEnemyOccupiedReservation(value.Owner,
                        value.ContentKey, value.ReservationKey, value.StableSpawnId))
                .Concat(entries.Select(value => new GeneratedHazardEnemyOccupiedReservation(
                    "MAP18_04", value.ContentKey, value.ReservationKey, value.StableSpawnId)))
                .OrderBy(value => value).ToArray());
            var occupiedKeys = new HashSet<string>(occupiedSurface.Select(value =>
                value.ReservationKey), StringComparer.Ordinal);
            remainingCandidates = new ReadOnlyCollection<GeneratedContentSlotIndexEntry>(
                populationPlan.SourceSlotIndex.Entries.Where(value =>
                    !occupiedKeys.Contains(value.ReservationKey)).OrderBy(value => value).ToArray());
            OccupiedSurfaceDigest = BakingCanonicalDigest.HashCanonicalLines(
                occupiedSurface.Select(value => value.StableToken));
            Digest = GeneratedHazardEnemyPlacementDigest.Compute(this);
        }

        public const string PolicyVersion = "MAP18_04_HAZARD_ENEMY_BUDGET_V1";
        public const string DownstreamOwner =
            "MAP18_05_INSTANTIATE_ACTIVITY_AND_EVENT_RUNTIME_STATES";
        public const bool OpensDownstreamTask = false;

        public GeneratedPopulationPlacementPlan PopulationPlan { get; }
        public IReadOnlyList<GeneratedHazardEnemyPoolEntry> PoolEntries => pools;
        public IReadOnlyList<GeneratedHazardEnemyPlacementEntry> Entries => entries;
        public IReadOnlyList<GeneratedHazardEnemyProtectionProof> ProtectionProofs =>
            protectionProofs;
        public GeneratedHazardEnemyBudgetLedger BudgetLedger { get; }
        public IReadOnlyList<GeneratedHazardEnemyOccupiedReservation> OccupiedSurface =>
            occupiedSurface;
        public IReadOnlyList<GeneratedContentSlotIndexEntry> RemainingCandidates =>
            remainingCandidates;
        public int PoolEntryCount => pools.Count;
        public int EntryCount => entries.Count;
        public int LogicalGroupCount => entries.Select(value => value.Kind).Distinct().Count();
        public int HazardEntryCount => entries.Count(value =>
            value.Kind == GeneratedHazardEnemyContentKind.Hazard);
        public int EnemyEntryCount => entries.Count(value =>
            value.Kind == GeneratedHazardEnemyContentKind.Enemy);
        public int UniqueContentKeyCount => entries.Select(value => value.ContentKey)
            .Distinct(StringComparer.Ordinal).Count();
        public int UniqueStableSpawnIdCount => entries.Select(value => value.StableSpawnId.Value)
            .Distinct(StringComparer.Ordinal).Count();
        public int UniqueReservationKeyCount => entries.Select(value => value.ReservationKey)
            .Distinct(StringComparer.Ordinal).Count();
        public int OccupiedSurfaceCount => occupiedSurface.Count;
        public int RemainingCandidateCount => remainingCandidates.Count;
        public int DeterministicTicketSelectionCount => entries.Count;
        public int CriticalRouteViolationCount => entries.Count(value =>
            !value.ProtectionProof.RouteAccepted);
        public int CriticalRewardViolationCount => entries.Count(value =>
            !value.ProtectionProof.RewardAccepted);
        public int CriticalRecoveryViolationCount => entries.Count(value =>
            !value.ProtectionProof.RecoveryAccepted);
        public string OccupiedSurfaceDigest { get; }
        public string Digest { get; }

        public GeneratedHazardEnemyPlacementEntry Entry(
            GeneratedHazardEnemyContentKind kind) => entries.Single(value =>
                value.Kind == kind);
        public bool IsOccupied(string reservationKey) => occupiedSurface.Any(value =>
            string.Equals(value.ReservationKey, reservationKey, StringComparison.Ordinal));

        public int RuntimeHazardPlacementCount => 0;
        public int RuntimeEnemyPlacementCount => 0;
        public int ActualDamageExecutionCount => 0;
        public int ActualCombatEncounterCount => 0;
        public int EnemyAiControllerHookupCount => 0;
        public int HealthComponentCreationCount => 0;
        public int DamageComponentCreationCount => 0;
        public int HitboxComponentCreationCount => 0;
        public int HurtboxComponentCreationCount => 0;
        public int RuntimeObjectSpawnCount => 0;
        public int GameObjectInstantiateCount => 0;
        public int GameObjectEnableCount => 0;
        public int GameObjectDisableCount => 0;
        public int GameObjectDestroyCount => 0;
        public int SystemIoFileWriteCount => 0;
        public int SystemIoFileReadCount => 0;
        public int DiskSaveFileCreateCount => 0;
        public int DiskLoadFileCreateCount => 0;
        public int UserSaveSlotWriteCount => 0;
        public int PlatformStorageWriteCount => 0;
        public int TilemapComponentWriteCount => 0;
        public int TilemapSetTileCallCount => 0;
        public int TilemapSetTilesCallCount => 0;
        public int TilemapSetTilesBlockCallCount => 0;
        public int TilemapClearAllTilesCallCount => 0;
        public int TilemapColliderCreationCount => 0;
        public int CompositeColliderCreationCount => 0;
        public int ColliderCreationCount => 0;
        public int RigidbodyCreationCount => 0;
        public int PhysicsQueryCount => 0;
        public int PhysicsSimulationCount => 0;
        public int NavMeshSetupCount => 0;
        public int PathfindingSetupCount => 0;
        public int SceneMutationCount => 0;
        public int PrefabMutationCount => 0;
        public int TilemapMutationCount => 0;
        public int CameraReadCount => 0;
        public int CameraWriteCount => 0;
        public int AddressablesLoadCount => 0;
        public int ResourcesLoadCount => 0;
        public int AssetDatabaseLoadCount => 0;
        public int AuthoringCsvEditCount => 0;
        public int GeneratedCsvCommitCount => 0;
        public int GeneratedAssetCommitCount => 0;
        public int ProductionSeedApprovalCount => 0;
        public int UnityEngineRandomCallCount => 0;
        public int RandomRangeCallCount => 0;
        public int SystemRandomDirectUsageCount => 0;
        public int HiddenRetryLoopCount => 0;
        public int ImplicitCandidateCreationCount => 0;
        public int CandidateMutationCount => 0;
        public int PriorTaskTestSelectionCount => 0;
        public int Legacy19347SelectionCount => 0;
        public int PlayModeSelectionCount => 0;
        public int UnfilteredTestSelectionCount => 0;
        public int FullRegressionRunCount => 0;
        public bool Map18_05Started => false;

        private static ReadOnlyCollection<T> Freeze<T>(IEnumerable<T> source)
            where T : IComparable<T> => new ReadOnlyCollection<T>(
                (source ?? Array.Empty<T>()).OrderBy(value => value).ToArray());
    }

    public static class GeneratedHazardEnemyPlacementDigest
    {
        public static string Compute(GeneratedHazardEnemyPlacementPlan plan)
        {
            if (plan == null) return string.Empty;
            var lines = new List<string>
            {
                "POLICY|" + GeneratedHazardEnemyPlacementPlan.PolicyVersion,
                "POPULATION|" + plan.PopulationPlan.Digest + "|" +
                    plan.PopulationPlan.OccupiedSurfaceDigest,
                "COUNTS|" + string.Join("|", new[]
                {
                    Number(plan.PoolEntryCount), Number(plan.EntryCount),
                    Number(plan.LogicalGroupCount), Number(plan.HazardEntryCount),
                    Number(plan.EnemyEntryCount), Number(plan.UniqueContentKeyCount),
                    Number(plan.UniqueStableSpawnIdCount),
                    Number(plan.UniqueReservationKeyCount),
                    Number(plan.OccupiedSurfaceCount), Number(plan.RemainingCandidateCount),
                }),
                "BUDGET|" + plan.BudgetLedger.Digest,
                "OCCUPIED|" + plan.OccupiedSurfaceDigest,
                "DOWNSTREAM|" + GeneratedHazardEnemyPlacementPlan.DownstreamOwner + "|0",
            };
            lines.AddRange(plan.PoolEntries.Select(value => value.StableToken));
            lines.AddRange(plan.Entries.Select(value => value.StableToken));
            lines.AddRange(plan.ProtectionProofs.Select(value => value.StableToken));
            lines.AddRange(plan.OccupiedSurface.Select(value => value.StableToken));
            lines.AddRange(plan.RemainingCandidates.Select(value =>
                "REMAINING|" + value.StableToken));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
