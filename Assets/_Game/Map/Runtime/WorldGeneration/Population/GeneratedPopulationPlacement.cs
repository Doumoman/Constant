using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.MicroPatterns;

namespace StarNight.Map.WorldGeneration.Population
{
    public enum GeneratedPopulationContentKind
    {
        ShopInventory = 1,
        OptionalResource = 2,
        NeutralMapElement = 3,
    }

    public enum GeneratedPopulationToolRequirement
    {
        None = 0,
        BasicHarvestTool = 1,
        AdvancedHarvestTool = 2,
    }

    public enum GeneratedPopulationFilterRejection
    {
        CategoryMismatch = 1,
        BiomeNotAllowed = 2,
        ResourceRequirementMismatch = 3,
        ToolRequirementMismatch = 4,
        InteractionRadiusMismatch = 5,
        SafeRadiusMismatch = 6,
        NeighborRadiusMismatch = 7,
        ReservedByMandatoryUnique = 8,
        AlreadyOccupied = 9,
    }

    public sealed class GeneratedPopulationPoolEntry : IComparable<GeneratedPopulationPoolEntry>
    {
        private readonly ReadOnlyCollection<GeneratedContentSlotCategory> categories;
        private readonly ReadOnlyCollection<MoonpalaceBiomeId> biomeAllowlist;

        public GeneratedPopulationPoolEntry(
            GeneratedPopulationContentKind kind,
            string contentKey,
            GeneratedContentPoolKey poolKey,
            IEnumerable<GeneratedContentSlotCategory> compatibleCategories,
            IEnumerable<MoonpalaceBiomeId> allowedBiomes,
            string requiredResourceKey,
            GeneratedPopulationToolRequirement requiredTool,
            int minimumInteractionRadius,
            int maximumInteractionRadius,
            int minimumSafeRadius,
            int minimumNeighborRadius,
            string symbolicPriceTierKey = "")
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
            RequiredResourceKey = Normalize(requiredResourceKey);
            RequiredTool = requiredTool;
            MinimumInteractionRadius = minimumInteractionRadius;
            MaximumInteractionRadius = maximumInteractionRadius;
            MinimumSafeRadius = minimumSafeRadius;
            MinimumNeighborRadius = minimumNeighborRadius;
            SymbolicPriceTierKey = Normalize(symbolicPriceTierKey);
            StableToken = string.Join("|", new[]
            {
                "POPULATION_POOL_ENTRY_V1",
                "KIND=" + Kind.ToString().ToUpperInvariant(),
                Field("CONTENT", ContentKey),
                "POOL=" + (PoolKey == null ? "MISSING" : PoolKey.StableToken),
                "CATEGORIES=" + string.Join(",", categories.Select(value =>
                    value.ToString().ToUpperInvariant())),
                "BIOMES=" + string.Join(",", biomeAllowlist.Select(value => value.CanonicalId)),
                Field("RESOURCE", RequiredResourceKey),
                "TOOL=" + RequiredTool.ToString().ToUpperInvariant(),
                "INTERACTION=" + Number(MinimumInteractionRadius) + "-" +
                    Number(MaximumInteractionRadius),
                "SAFE=" + Number(MinimumSafeRadius),
                "NEIGHBOR=" + Number(MinimumNeighborRadius),
                Field("PRICE", SymbolicPriceTierKey),
            });
        }

        public GeneratedPopulationContentKind Kind { get; }
        public string ContentKey { get; }
        public GeneratedContentPoolKey PoolKey { get; }
        public IReadOnlyList<GeneratedContentSlotCategory> CompatibleCategories => categories;
        public IReadOnlyList<MoonpalaceBiomeId> BiomeAllowlist => biomeAllowlist;
        public string RequiredResourceKey { get; }
        public GeneratedPopulationToolRequirement RequiredTool { get; }
        public int MinimumInteractionRadius { get; }
        public int MaximumInteractionRadius { get; }
        public int MinimumSafeRadius { get; }
        public int MinimumNeighborRadius { get; }
        public string SymbolicPriceTierKey { get; }
        public string StableToken { get; }

        public int CompareTo(GeneratedPopulationPoolEntry other) => other == null
            ? -1 : Kind.CompareTo(other.Kind);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? string.Empty : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
        private static string Field(string name, string value) =>
            name + "=" + GeneratedContentPoolKey.Part(value);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public static class GeneratedPopulationPoolCatalog
    {
        public static IReadOnlyList<GeneratedPopulationPoolEntry> CreateDefault(
            MicroPatternBiomeProfileCatalog biomeCatalog)
        {
            var known = biomeCatalog == null
                ? Array.Empty<MoonpalaceBiomeId>()
                : biomeCatalog.Profiles.Select(value => value.Biome).ToArray();
            var moonAndCassia = known.Where(value => value == MoonpalaceBiomeId.MoonCrater ||
                value == MoonpalaceBiomeId.CassiaRoot);
            var millAndDough = known.Where(value => value == MoonpalaceBiomeId.AbandonedMill ||
                value == MoonpalaceBiomeId.MoonDough);
            return new ReadOnlyCollection<GeneratedPopulationPoolEntry>(new[]
            {
                new GeneratedPopulationPoolEntry(
                    GeneratedPopulationContentKind.ShopInventory,
                    "SHOP_STOCK_GENERAL",
                    new GeneratedContentPoolKey("POPULATION_SHOP_STOCK", "V1"),
                    new[] { GeneratedContentSlotCategory.Shop }, known,
                    string.Empty, GeneratedPopulationToolRequirement.None,
                    1, 3, 2, 8, "PRICE_TIER_COMMON"),
                new GeneratedPopulationPoolEntry(
                    GeneratedPopulationContentKind.OptionalResource,
                    "OPTIONAL_RESOURCE_GENERIC",
                    new GeneratedContentPoolKey("POPULATION_OPTIONAL_RESOURCE", "V1"),
                    new[] { GeneratedContentSlotCategory.Resource,
                        GeneratedContentSlotCategory.Pickup }, moonAndCassia,
                    "RESOURCE_GENERIC", GeneratedPopulationToolRequirement.BasicHarvestTool,
                    1, 2, 2, 8),
                new GeneratedPopulationPoolEntry(
                    GeneratedPopulationContentKind.NeutralMapElement,
                    "NEUTRAL_MAP_ELEMENT_GENERIC",
                    new GeneratedContentPoolKey("POPULATION_NEUTRAL_ELEMENT", "V1"),
                    new[] { GeneratedContentSlotCategory.Activity,
                        GeneratedContentSlotCategory.Device }, millAndDough,
                    string.Empty, GeneratedPopulationToolRequirement.None,
                    1, 4, 3, 10),
            }.OrderBy(value => value).ToArray());
        }
    }

    public sealed class GeneratedPopulationCandidateContext :
        IComparable<GeneratedPopulationCandidateContext>
    {
        public GeneratedPopulationCandidateContext(
            GeneratedContentSlotIndexEntry slot,
            MoonpalaceBiomeId biome,
            string resourceKey,
            GeneratedPopulationToolRequirement availableTool,
            int interactionRadius,
            int safeRadius,
            int neighborRadius)
        {
            Slot = slot;
            Biome = biome;
            ResourceKey = string.IsNullOrWhiteSpace(resourceKey) ? string.Empty :
                BakingCanonicalDigest.NormalizeLineEndingsToLf(resourceKey).Trim();
            AvailableTool = availableTool;
            InteractionRadius = interactionRadius;
            SafeRadius = safeRadius;
            NeighborRadius = neighborRadius;
            StableToken = string.Join("|", new[]
            {
                "POPULATION_CANDIDATE_CONTEXT_V1",
                "SLOT=" + (slot == null ? "MISSING" : slot.StableToken),
                "BIOME=" + biome.CanonicalId,
                "RESOURCE=" + GeneratedContentPoolKey.Part(ResourceKey),
                "TOOL=" + availableTool.ToString().ToUpperInvariant(),
                "INTERACTION=" + Number(interactionRadius),
                "SAFE=" + Number(safeRadius),
                "NEIGHBOR=" + Number(neighborRadius),
            });
        }

        public GeneratedContentSlotIndexEntry Slot { get; }
        public MoonpalaceBiomeId Biome { get; }
        public string ResourceKey { get; }
        public GeneratedPopulationToolRequirement AvailableTool { get; }
        public int InteractionRadius { get; }
        public int SafeRadius { get; }
        public int NeighborRadius { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedPopulationCandidateContext other) => other == null
            ? -1 : string.Compare(Slot == null ? string.Empty : Slot.DeterministicOrderKey,
                other.Slot == null ? string.Empty : other.Slot.DeterministicOrderKey,
                StringComparison.Ordinal);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedPopulationFilterEvidence :
        IComparable<GeneratedPopulationFilterEvidence>
    {
        private readonly ReadOnlyCollection<GeneratedPopulationFilterRejection> rejections;

        internal GeneratedPopulationFilterEvidence(
            GeneratedPopulationPoolEntry pool,
            GeneratedPopulationCandidateContext context,
            bool categoryAccepted,
            bool biomeAccepted,
            bool resourceAccepted,
            bool toolAccepted,
            bool interactionRadiusAccepted,
            bool safeRadiusAccepted,
            bool neighborRadiusAccepted,
            bool mandatoryExclusionAccepted,
            bool reservationAvailable,
            IEnumerable<GeneratedPopulationFilterRejection> sourceRejections,
            string deterministicTicket)
        {
            Pool = pool;
            Context = context;
            CategoryAccepted = categoryAccepted;
            BiomeAccepted = biomeAccepted;
            ResourceAccepted = resourceAccepted;
            ToolAccepted = toolAccepted;
            InteractionRadiusAccepted = interactionRadiusAccepted;
            SafeRadiusAccepted = safeRadiusAccepted;
            NeighborRadiusAccepted = neighborRadiusAccepted;
            MandatoryExclusionAccepted = mandatoryExclusionAccepted;
            ReservationAvailable = reservationAvailable;
            rejections = new ReadOnlyCollection<GeneratedPopulationFilterRejection>(
                (sourceRejections ?? Array.Empty<GeneratedPopulationFilterRejection>())
                .Distinct().OrderBy(value => value).ToArray());
            DeterministicTicket = deterministicTicket ?? string.Empty;
            StableToken = string.Join("|", new[]
            {
                "POPULATION_FILTER_EVIDENCE_V1",
                pool == null ? "MISSING_POOL" : pool.StableToken,
                context == null ? "MISSING_CONTEXT" : context.StableToken,
                "CATEGORY=" + Flag(categoryAccepted),
                "BIOME=" + Flag(biomeAccepted),
                "RESOURCE=" + Flag(resourceAccepted),
                "TOOL=" + Flag(toolAccepted),
                "INTERACTION=" + Flag(interactionRadiusAccepted),
                "SAFE=" + Flag(safeRadiusAccepted),
                "NEIGHBOR=" + Flag(neighborRadiusAccepted),
                "MANDATORY_EXCLUSION=" + Flag(mandatoryExclusionAccepted),
                "RESERVATION_AVAILABLE=" + Flag(reservationAvailable),
                "REJECTIONS=" + string.Join(",", rejections.Select(value =>
                    value.ToString().ToUpperInvariant())),
                "TICKET=" + DeterministicTicket,
            });
        }

        public GeneratedPopulationPoolEntry Pool { get; }
        public GeneratedPopulationCandidateContext Context { get; }
        public bool CategoryAccepted { get; }
        public bool BiomeAccepted { get; }
        public bool ResourceAccepted { get; }
        public bool ToolAccepted { get; }
        public bool InteractionRadiusAccepted { get; }
        public bool SafeRadiusAccepted { get; }
        public bool NeighborRadiusAccepted { get; }
        public bool MandatoryExclusionAccepted { get; }
        public bool ReservationAvailable { get; }
        public IReadOnlyList<GeneratedPopulationFilterRejection> Rejections => rejections;
        public string DeterministicTicket { get; }
        public string StableToken { get; }
        public bool Accepted => rejections.Count == 0;

        public int CompareTo(GeneratedPopulationFilterEvidence other)
        {
            if (other == null) return -1;
            var value = Pool.Kind.CompareTo(other.Pool.Kind);
            return value != 0 ? value : Context.CompareTo(other.Context);
        }

        private static string Flag(bool value) => value ? "1" : "0";
    }

    public static class GeneratedPopulationFilterRule
    {
        public static GeneratedPopulationFilterEvidence Evaluate(
            GeneratedPopulationPoolEntry pool,
            GeneratedPopulationCandidateContext context,
            ISet<string> mandatoryReservationKeys,
            ISet<string> occupiedReservationKeys)
        {
            var rejections = new List<GeneratedPopulationFilterRejection>();
            var slot = context == null ? null : context.Slot;
            var categoryAccepted = pool != null && slot != null &&
                pool.CompatibleCategories.Contains(slot.Address.Category);
            if (!categoryAccepted) rejections.Add(
                GeneratedPopulationFilterRejection.CategoryMismatch);
            var biomeAccepted = pool != null && context != null &&
                pool.BiomeAllowlist.Contains(context.Biome);
            if (!biomeAccepted) rejections.Add(
                GeneratedPopulationFilterRejection.BiomeNotAllowed);
            var resourceAccepted = pool != null && context != null &&
                (string.IsNullOrEmpty(pool.RequiredResourceKey) || string.Equals(
                    pool.RequiredResourceKey, context.ResourceKey, StringComparison.Ordinal));
            if (!resourceAccepted) rejections.Add(
                GeneratedPopulationFilterRejection.ResourceRequirementMismatch);
            var toolAccepted = pool != null && context != null &&
                context.AvailableTool >= pool.RequiredTool;
            if (!toolAccepted) rejections.Add(
                GeneratedPopulationFilterRejection.ToolRequirementMismatch);
            var interactionAccepted = pool != null && context != null &&
                context.InteractionRadius >= pool.MinimumInteractionRadius &&
                context.InteractionRadius <= pool.MaximumInteractionRadius;
            if (!interactionAccepted) rejections.Add(
                GeneratedPopulationFilterRejection.InteractionRadiusMismatch);
            var safeAccepted = pool != null && context != null &&
                context.SafeRadius >= pool.MinimumSafeRadius;
            if (!safeAccepted) rejections.Add(
                GeneratedPopulationFilterRejection.SafeRadiusMismatch);
            var neighborAccepted = pool != null && context != null &&
                context.NeighborRadius >= pool.MinimumNeighborRadius;
            if (!neighborAccepted) rejections.Add(
                GeneratedPopulationFilterRejection.NeighborRadiusMismatch);
            var reservationKey = slot == null ? string.Empty : slot.ReservationKey;
            var mandatoryAccepted = slot != null && (mandatoryReservationKeys == null ||
                !mandatoryReservationKeys.Contains(reservationKey));
            if (!mandatoryAccepted) rejections.Add(
                GeneratedPopulationFilterRejection.ReservedByMandatoryUnique);
            var reservationAvailable = mandatoryAccepted && (occupiedReservationKeys == null ||
                !occupiedReservationKeys.Contains(reservationKey));
            if (mandatoryAccepted && !reservationAvailable) rejections.Add(
                GeneratedPopulationFilterRejection.AlreadyOccupied);
            var ticket = pool == null || context == null ? string.Empty :
                BakingCanonicalDigest.HashCanonicalLines(new[]
                {
                    "MAP18_03_DETERMINISTIC_TICKET_V1",
                    pool.StableToken,
                    context.StableToken,
                });
            return new GeneratedPopulationFilterEvidence(pool, context, categoryAccepted,
                biomeAccepted, resourceAccepted, toolAccepted, interactionAccepted,
                safeAccepted, neighborAccepted, mandatoryAccepted, reservationAvailable,
                rejections, ticket);
        }
    }

    public sealed class GeneratedPopulationPlacementEntry :
        IComparable<GeneratedPopulationPlacementEntry>
    {
        internal GeneratedPopulationPlacementEntry(GeneratedPopulationFilterEvidence filterProof)
        {
            FilterProof = filterProof;
            PoolEntry = filterProof.Pool;
            Candidate = filterProof.Context;
            SelectedSlot = Candidate.Slot;
            Kind = PoolEntry.Kind;
            ContentKey = PoolEntry.ContentKey;
            StableSpawnId = SelectedSlot.StableSpawnId;
            ReservationKey = SelectedSlot.ReservationKey;
            StableToken = string.Join("|", new[]
            {
                "POPULATION_PLACEMENT_ENTRY_V1",
                PoolEntry.StableToken,
                "SLOT=" + SelectedSlot.StableToken,
                "SPAWN_ID=" + StableSpawnId.Value,
                "RESERVATION=" + ReservationKey,
                "TICKET=" + filterProof.DeterministicTicket,
                "FILTER=" + filterProof.StableToken,
            });
        }

        public GeneratedPopulationContentKind Kind { get; }
        public string ContentKey { get; }
        public GeneratedPopulationPoolEntry PoolEntry { get; }
        public GeneratedPopulationCandidateContext Candidate { get; }
        public GeneratedContentSlotIndexEntry SelectedSlot { get; }
        public GeneratedStableSpawnId StableSpawnId { get; }
        public string ReservationKey { get; }
        public GeneratedPopulationFilterEvidence FilterProof { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedPopulationPlacementEntry other) => other == null
            ? -1 : Kind.CompareTo(other.Kind);
    }

    public sealed class GeneratedPopulationOccupiedReservation :
        IComparable<GeneratedPopulationOccupiedReservation>
    {
        internal GeneratedPopulationOccupiedReservation(
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
                "POPULATION_OCCUPIED_RESERVATION_V1", Owner, ContentKey,
                ReservationKey, StableSpawnId == null ? "MISSING" : StableSpawnId.Value,
            });
        }

        public string Owner { get; }
        public string ContentKey { get; }
        public string ReservationKey { get; }
        public GeneratedStableSpawnId StableSpawnId { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedPopulationOccupiedReservation other) => other == null
            ? -1 : string.Compare(ReservationKey, other.ReservationKey, StringComparison.Ordinal);
    }

    public sealed class GeneratedPopulationPlacementPlan
    {
        private readonly ReadOnlyCollection<GeneratedPopulationPoolEntry> poolEntries;
        private readonly ReadOnlyCollection<GeneratedPopulationPlacementEntry> entries;
        private readonly ReadOnlyCollection<GeneratedPopulationFilterEvidence> filterEvidence;
        private readonly ReadOnlyCollection<GeneratedPopulationOccupiedReservation> occupiedSurface;
        private readonly ReadOnlyCollection<GeneratedContentSlotIndexEntry> remainingCandidates;

        internal GeneratedPopulationPlacementPlan(
            GeneratedContentSlotIndex slotIndex,
            GeneratedMandatoryUniquePlacementPlan mandatoryPlan,
            IEnumerable<GeneratedPopulationPoolEntry> sourcePools,
            IEnumerable<GeneratedPopulationPlacementEntry> sourceEntries,
            IEnumerable<GeneratedPopulationFilterEvidence> sourceEvidence,
            IEnumerable<GeneratedContentSlotIndexEntry> sourceRemaining)
        {
            SourceSlotIndex = slotIndex;
            MandatoryPlan = mandatoryPlan;
            poolEntries = Freeze(sourcePools);
            entries = Freeze(sourceEntries);
            filterEvidence = Freeze(sourceEvidence);
            var occupied = mandatoryPlan.Entries.Select(value =>
                    new GeneratedPopulationOccupiedReservation("MAP18_02",
                        value.ContentKey.StableToken, value.ReservationKey, value.StableSpawnId))
                .Concat(entries.Select(value => new GeneratedPopulationOccupiedReservation(
                    "MAP18_03", value.ContentKey, value.ReservationKey, value.StableSpawnId)))
                .OrderBy(value => value).ToArray();
            occupiedSurface = new ReadOnlyCollection<GeneratedPopulationOccupiedReservation>(occupied);
            remainingCandidates = new ReadOnlyCollection<GeneratedContentSlotIndexEntry>(
                (sourceRemaining ?? Array.Empty<GeneratedContentSlotIndexEntry>())
                .OrderBy(value => value).ToArray());
            OccupiedSurfaceDigest = BakingCanonicalDigest.HashCanonicalLines(occupiedSurface
                .Select(value => value.StableToken));
            Digest = GeneratedPopulationPlacementDigest.Compute(this);
        }

        public const string PolicyVersion = "MAP18_03_SHOP_RESOURCE_MAP_ELEMENT_POPULATION_V1";
        public const string DownstreamOwner =
            "MAP18_04_PLACE_HAZARDS_ENEMIES_AND_HIERARCHICAL_BUDGETS";
        public const bool OpensDownstreamTask = false;

        public GeneratedContentSlotIndex SourceSlotIndex { get; }
        public GeneratedMandatoryUniquePlacementPlan MandatoryPlan { get; }
        public IReadOnlyList<GeneratedPopulationPoolEntry> PoolEntries => poolEntries;
        public IReadOnlyList<GeneratedPopulationPlacementEntry> Entries => entries;
        public IReadOnlyList<GeneratedPopulationFilterEvidence> FilterEvidence => filterEvidence;
        public IReadOnlyList<GeneratedPopulationOccupiedReservation> OccupiedSurface => occupiedSurface;
        public IReadOnlyList<GeneratedContentSlotIndexEntry> RemainingCandidates => remainingCandidates;
        public int PoolEntryCount => poolEntries.Count;
        public int EntryCount => entries.Count;
        public int LogicalGroupCount => entries.Select(value => value.Kind).Distinct().Count();
        public int ShopInventoryEntryCount => entries.Count(value =>
            value.Kind == GeneratedPopulationContentKind.ShopInventory);
        public int OptionalResourceEntryCount => entries.Count(value =>
            value.Kind == GeneratedPopulationContentKind.OptionalResource);
        public int NeutralMapElementEntryCount => entries.Count(value =>
            value.Kind == GeneratedPopulationContentKind.NeutralMapElement);
        public int UniqueContentKeyCount => entries.Select(value => value.ContentKey)
            .Distinct(StringComparer.Ordinal).Count();
        public int UniqueStableSpawnIdCount => entries.Select(value => value.StableSpawnId.Value)
            .Distinct(StringComparer.Ordinal).Count();
        public int UniqueReservationKeyCount => entries.Select(value => value.ReservationKey)
            .Distinct(StringComparer.Ordinal).Count();
        public int MandatoryExclusionCount => MandatoryPlan.ExclusionCount;
        public int OccupiedSurfaceCount => occupiedSurface.Count;
        public int RemainingCandidateCount => remainingCandidates.Count;
        public int DeterministicHashTicketSelectionCount => entries.Count;
        public string OccupiedSurfaceDigest { get; }
        public string Digest { get; }

        public GeneratedPopulationPlacementEntry Entry(GeneratedPopulationContentKind kind) =>
            entries.Single(value => value.Kind == kind);
        public bool IsOccupied(string reservationKey) => occupiedSurface.Any(value =>
            string.Equals(value.ReservationKey, reservationKey, StringComparison.Ordinal));

        public int RuntimeContentPlacementCount => 0;
        public int HazardPlacementCount => 0;
        public int EnemyPlacementCount => 0;
        public int HierarchicalCombatBudgetSpendCount => 0;
        public int ActualShopTransactionCount => 0;
        public int PriceExecutionCount => 0;
        public int WalletCurrencyMutationCount => 0;
        public int ItemGrantCount => 0;
        public int ResourcePickupGrantCount => 0;
        public int InventoryMutationCount => 0;
        public int DeviceExecutionCount => 0;
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
        public bool Map18_04Started => false;

        private static ReadOnlyCollection<T> Freeze<T>(IEnumerable<T> source)
            where T : IComparable<T> => new ReadOnlyCollection<T>(
                (source ?? Array.Empty<T>()).OrderBy(value => value).ToArray());
    }

    public static class GeneratedPopulationPlacementDigest
    {
        public static string Compute(GeneratedPopulationPlacementPlan plan)
        {
            if (plan == null) return string.Empty;
            var lines = new List<string>
            {
                "POLICY|" + GeneratedPopulationPlacementPlan.PolicyVersion,
                "SLOT_INDEX|" + plan.SourceSlotIndex.Digest + "|" +
                    plan.SourceSlotIndex.StableIdSetDigest,
                "MANDATORY|" + plan.MandatoryPlan.Digest + "|" +
                    plan.MandatoryPlan.StableIdSetDigest,
                "COUNTS|" + string.Join("|", new[]
                {
                    Number(plan.PoolEntryCount), Number(plan.EntryCount),
                    Number(plan.LogicalGroupCount), Number(plan.ShopInventoryEntryCount),
                    Number(plan.OptionalResourceEntryCount),
                    Number(plan.NeutralMapElementEntryCount),
                    Number(plan.UniqueContentKeyCount),
                    Number(plan.UniqueStableSpawnIdCount),
                    Number(plan.UniqueReservationKeyCount),
                    Number(plan.MandatoryExclusionCount),
                    Number(plan.OccupiedSurfaceCount), Number(plan.RemainingCandidateCount),
                }),
                "OCCUPIED_SURFACE|" + plan.OccupiedSurfaceDigest,
                "DOWNSTREAM|" + GeneratedPopulationPlacementPlan.DownstreamOwner + "|0",
            };
            lines.AddRange(plan.PoolEntries.Select(value => value.StableToken));
            lines.AddRange(plan.Entries.Select(value => value.StableToken));
            lines.AddRange(plan.FilterEvidence.Select(value => value.StableToken));
            lines.AddRange(plan.OccupiedSurface.Select(value => value.StableToken));
            lines.AddRange(plan.RemainingCandidates.Select(value =>
                "REMAINING|" + value.StableToken));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
