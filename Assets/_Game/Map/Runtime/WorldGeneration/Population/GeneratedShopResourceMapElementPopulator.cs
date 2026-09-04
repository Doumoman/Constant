using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.MicroPatterns;

namespace StarNight.Map.WorldGeneration.Population
{
    public sealed class GeneratedPopulationPlacementRequest
    {
        private readonly ReadOnlyCollection<GeneratedPopulationPoolEntry> pools;
        private readonly ReadOnlyCollection<GeneratedPopulationCandidateContext> contexts;
        private readonly ReadOnlyCollection<string> existingReservationKeys;
        private readonly ReadOnlyCollection<string> existingStableSpawnIds;

        public GeneratedPopulationPlacementRequest(
            GeneratedContentSlotIndex slotIndex,
            GeneratedMandatoryUniquePlacementPlan mandatoryPlan,
            IEnumerable<GeneratedPopulationPoolEntry> sourcePools,
            IEnumerable<GeneratedPopulationCandidateContext> sourceContexts,
            MicroPatternBiomeProfileCatalog biomeCatalog,
            string expectedMandatoryPlanDigest,
            string expectedMandatoryStableIdSetDigest,
            int expectedMandatoryExclusionCount,
            IEnumerable<string> sourceExistingReservationKeys = null,
            IEnumerable<string> sourceExistingStableSpawnIds = null,
            string expectedPopulationPlanDigest = null,
            string expectedOccupiedSurfaceDigest = null,
            bool attemptedRuntimeSpawn = false,
            bool attemptedShopTransaction = false)
        {
            SlotIndex = slotIndex;
            MandatoryPlan = mandatoryPlan;
            pools = Freeze(sourcePools, out var nullPools);
            contexts = Freeze(sourceContexts, out var nullContexts);
            BiomeCatalog = biomeCatalog;
            ExpectedMandatoryPlanDigest = Normalize(expectedMandatoryPlanDigest);
            ExpectedMandatoryStableIdSetDigest = Normalize(expectedMandatoryStableIdSetDigest);
            ExpectedMandatoryExclusionCount = expectedMandatoryExclusionCount;
            existingReservationKeys = FreezeStrings(sourceExistingReservationKeys);
            existingStableSpawnIds = FreezeStrings(sourceExistingStableSpawnIds);
            ExpectedPopulationPlanDigest = Normalize(expectedPopulationPlanDigest);
            ExpectedOccupiedSurfaceDigest = Normalize(expectedOccupiedSurfaceDigest);
            AttemptedRuntimeSpawn = attemptedRuntimeSpawn;
            AttemptedShopTransaction = attemptedShopTransaction;
            NullPoolCount = nullPools;
            NullContextCount = nullContexts;
        }

        public GeneratedContentSlotIndex SlotIndex { get; }
        public GeneratedMandatoryUniquePlacementPlan MandatoryPlan { get; }
        public IReadOnlyList<GeneratedPopulationPoolEntry> Pools => pools;
        public IReadOnlyList<GeneratedPopulationCandidateContext> CandidateContexts => contexts;
        public MicroPatternBiomeProfileCatalog BiomeCatalog { get; }
        public string ExpectedMandatoryPlanDigest { get; }
        public string ExpectedMandatoryStableIdSetDigest { get; }
        public int ExpectedMandatoryExclusionCount { get; }
        public IReadOnlyList<string> ExistingReservationKeys => existingReservationKeys;
        public IReadOnlyList<string> ExistingStableSpawnIds => existingStableSpawnIds;
        public string ExpectedPopulationPlanDigest { get; }
        public string ExpectedOccupiedSurfaceDigest { get; }
        public bool AttemptedRuntimeSpawn { get; }
        public bool AttemptedShopTransaction { get; }
        public int NullPoolCount { get; }
        public int NullContextCount { get; }

        private static ReadOnlyCollection<T> Freeze<T>(IEnumerable<T> source, out int nullCount)
            where T : class, IComparable<T>
        {
            var raw = (source ?? Array.Empty<T>()).ToArray();
            nullCount = raw.Count(value => value == null);
            return new ReadOnlyCollection<T>(raw.Where(value => value != null)
                .OrderBy(value => value).ToArray());
        }

        private static ReadOnlyCollection<string> FreezeStrings(IEnumerable<string> source) =>
            new ReadOnlyCollection<string>((source ?? Array.Empty<string>()).Select(Normalize)
                .Where(value => value.Length > 0).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? string.Empty : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
    }

    public enum GeneratedPopulationPlacementFailureCode
    {
        MissingRequest = 1,
        MissingSlotIndex = 2,
        MissingMandatoryPlan = 3,
        MandatoryPlanDigestMismatch = 4,
        MandatoryStableIdSetDigestMismatch = 5,
        MandatoryExclusionCountMismatch = 6,
        MissingBiomeCatalog = 7,
        MissingCandidateContext = 8,
        DuplicateCandidateContext = 9,
        MissingShopInventoryPool = 10,
        MissingOptionalResourcePool = 11,
        MissingNeutralMapElementPool = 12,
        DuplicatePopulationPool = 13,
        InvalidPoolKey = 14,
        InvalidFilterRule = 15,
        MissingShopInventoryCandidate = 16,
        MissingOptionalResourceCandidate = 17,
        MissingNeutralMapElementCandidate = 18,
        FilterMismatch = 19,
        ReservedMandatorySlotReuse = 20,
        NeighborCollision = 21,
        ReservationKeyCollision = 22,
        StableSpawnIdCollision = 23,
        AttemptedRuntimeSpawnOrShopTransaction = 24,
        PopulationPlanDigestMismatch = 25,
        OccupiedSurfaceDigestMismatch = 26,
    }

    public sealed class GeneratedPopulationPlacementFailure :
        IComparable<GeneratedPopulationPlacementFailure>
    {
        public GeneratedPopulationPlacementFailure(
            GeneratedPopulationPlacementFailureCode code,
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

        public GeneratedPopulationPlacementFailureCode Code { get; }
        public string Owner { get; }
        public string OffendingKey { get; }
        public string Expected { get; }
        public string Actual { get; }
        public string Reason { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedPopulationPlacementFailure other) => other == null
            ? -1 : string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
        public override string ToString() => StableToken;
    }

    public sealed class GeneratedPopulationPlacementResult
    {
        private readonly ReadOnlyCollection<GeneratedPopulationPlacementFailure> failures;

        internal GeneratedPopulationPlacementResult(
            GeneratedPopulationPlacementPlan plan,
            IEnumerable<GeneratedPopulationPlacementFailure> sourceFailures)
        {
            Plan = plan;
            failures = new ReadOnlyCollection<GeneratedPopulationPlacementFailure>(
                (sourceFailures ?? Array.Empty<GeneratedPopulationPlacementFailure>())
                .OrderBy(value => value).ToArray());
        }

        public bool Success => Plan != null && failures.Count == 0;
        public GeneratedPopulationPlacementPlan Plan { get; }
        public IReadOnlyList<GeneratedPopulationPlacementFailure> Failures => failures;
        public int PartialEntryCount => Success ? Plan.EntryCount : 0;
        public int PartialMutationCount => 0;
        public int RetryLoopCount => 0;
    }

    public static class GeneratedShopResourceMapElementPopulator
    {
        public const string ExpectedMandatoryPlanDigest =
            "eda7bf7aedb660223927d6e0b36e63f5dbe041761febf91da6fb855f413f200f";
        public const string ExpectedMandatoryStableIdSetDigest =
            "c4c1948c17d8e75e821e3eec4402832635e7773693c4b956bc18a53d7ca15a09";
        public const int ExpectedMandatoryExclusionCount = 4;
        public const int RequiredLogicalGroupCount = 3;

        public static GeneratedPopulationPlacementResult Populate(
            GeneratedPopulationPlacementRequest request)
        {
            var failures = new List<GeneratedPopulationPlacementFailure>();
            if (request == null)
            {
                failures.Add(Failure(GeneratedPopulationPlacementFailureCode.MissingRequest,
                    "MAP18_03", "REQUEST", "PRESENT", "MISSING", "Request is required."));
                return Result(null, failures);
            }

            ValidateUpstream(request, failures);
            ValidateCandidateContexts(request, failures);
            ValidatePools(request, failures);
            if (request.AttemptedRuntimeSpawn || request.AttemptedShopTransaction)
                failures.Add(Failure(
                    GeneratedPopulationPlacementFailureCode.AttemptedRuntimeSpawnOrShopTransaction,
                    "MAP18_04_OR_LATER", "SIDE_EFFECT_REQUEST", "0/0",
                    Flag(request.AttemptedRuntimeSpawn) + "/" +
                    Flag(request.AttemptedShopTransaction),
                    "MAP18_03 publishes only a logical population plan."));
            if (failures.Count > 0) return Result(null, failures);

            var mandatoryKeys = new HashSet<string>(request.MandatoryPlan.Exclusions
                .Select(value => value.ReservationKey), StringComparer.Ordinal);
            var occupiedKeys = new HashSet<string>(StringComparer.Ordinal);
            var evidence = new List<GeneratedPopulationFilterEvidence>();
            var entries = new List<GeneratedPopulationPlacementEntry>();
            foreach (var pool in request.Pools.OrderBy(value => value))
            {
                var poolEvidence = request.CandidateContexts.Select(context =>
                    GeneratedPopulationFilterRule.Evaluate(pool, context,
                        mandatoryKeys, occupiedKeys)).OrderBy(value => value).ToArray();
                evidence.AddRange(poolEvidence);
                var selected = poolEvidence.Where(value => value.Accepted)
                    .OrderBy(value => value.DeterministicTicket, StringComparer.Ordinal)
                    .ThenBy(value => value.Context).FirstOrDefault();
                if (selected == null)
                {
                    if (poolEvidence.Any(IsOnlyMandatoryExclusionRejected))
                        failures.Add(Failure(
                            GeneratedPopulationPlacementFailureCode.ReservedMandatorySlotReuse,
                            "MAP18_02_EXCLUSION_SURFACE", pool.ContentKey,
                            "UNRESERVED_SLOT", "ONLY_RESERVED_MATCH",
                            "A compatible candidate belongs to a mandatory unique reservation."));
                    if (poolEvidence.Any(IsOnlyNeighborRejected))
                        failures.Add(Failure(
                            GeneratedPopulationPlacementFailureCode.NeighborCollision,
                            "MAP18_03_FILTER", pool.ContentKey,
                            ">=" + Number(pool.MinimumNeighborRadius), "BELOW_MINIMUM",
                            "Compatible candidates violate the declared neighbor radius."));
                    failures.Add(Failure(
                        GeneratedPopulationPlacementFailureCode.FilterMismatch,
                        "MAP18_03_FILTER", pool.ContentKey, "ONE_ACCEPTED_CANDIDATE", "0",
                        "All source candidates were rejected by explicit filters."));
                    failures.Add(MissingCandidate(pool.Kind, pool.ContentKey));
                    continue;
                }

                if (request.ExistingReservationKeys.Contains(
                        selected.Context.Slot.ReservationKey, StringComparer.Ordinal))
                    failures.Add(Failure(
                        GeneratedPopulationPlacementFailureCode.ReservationKeyCollision,
                        "MAP18_04_OCCUPIED_SURFACE", selected.Context.Slot.ReservationKey,
                        "UNRESERVED", "PRE_EXISTING",
                        "Selected reservation already belongs to another consumer."));
                if (request.ExistingStableSpawnIds.Contains(
                        selected.Context.Slot.StableSpawnId.Value, StringComparer.Ordinal))
                    failures.Add(Failure(
                        GeneratedPopulationPlacementFailureCode.StableSpawnIdCollision,
                        GeneratedStableSpawnIdFactory.Namespace,
                        selected.Context.Slot.StableSpawnId.Value, "UNIQUE", "PRE_EXISTING",
                        "Selected stable spawn ID already belongs to another placement."));
                var entry = new GeneratedPopulationPlacementEntry(selected);
                entries.Add(entry);
                occupiedKeys.Add(entry.ReservationKey);
            }
            if (failures.Count > 0) return Result(null, failures);

            foreach (var group in entries.GroupBy(value => value.ReservationKey,
                         StringComparer.Ordinal).Where(value => value.Count() > 1))
                failures.Add(Failure(
                    GeneratedPopulationPlacementFailureCode.ReservationKeyCollision,
                    "MAP18_04_OCCUPIED_SURFACE", group.Key, "1", Number(group.Count()),
                    "Population entries cannot share one physical slot."));
            foreach (var group in entries.GroupBy(value => value.StableSpawnId.Value,
                         StringComparer.Ordinal).Where(value => value.Count() > 1))
                failures.Add(Failure(
                    GeneratedPopulationPlacementFailureCode.StableSpawnIdCollision,
                    GeneratedStableSpawnIdFactory.Namespace, group.Key, "1", Number(group.Count()),
                    "Population entries cannot share one stable spawn ID."));
            if (entries.Any(value => mandatoryKeys.Contains(value.ReservationKey)))
                failures.Add(Failure(
                    GeneratedPopulationPlacementFailureCode.ReservedMandatorySlotReuse,
                    "MAP18_02_EXCLUSION_SURFACE", "PLACEMENT_SET", "0", "1+",
                    "Population output reused a mandatory unique reservation."));
            if (failures.Count > 0) return Result(null, failures);

            var allOccupied = new HashSet<string>(mandatoryKeys, StringComparer.Ordinal);
            allOccupied.UnionWith(entries.Select(value => value.ReservationKey));
            var remaining = request.SlotIndex.Entries.Where(value =>
                !allOccupied.Contains(value.ReservationKey));
            var plan = new GeneratedPopulationPlacementPlan(request.SlotIndex,
                request.MandatoryPlan, request.Pools, entries, evidence, remaining);
            if (!string.IsNullOrEmpty(request.ExpectedPopulationPlanDigest) &&
                !string.Equals(request.ExpectedPopulationPlanDigest, plan.Digest,
                    StringComparison.Ordinal))
                failures.Add(Failure(
                    GeneratedPopulationPlacementFailureCode.PopulationPlanDigestMismatch,
                    "MAP18_03", "POPULATION_PLAN_DIGEST",
                    request.ExpectedPopulationPlanDigest, plan.Digest,
                    "Computed population plan digest differs from expected evidence."));
            if (!string.IsNullOrEmpty(request.ExpectedOccupiedSurfaceDigest) &&
                !string.Equals(request.ExpectedOccupiedSurfaceDigest, plan.OccupiedSurfaceDigest,
                    StringComparison.Ordinal))
                failures.Add(Failure(
                    GeneratedPopulationPlacementFailureCode.OccupiedSurfaceDigestMismatch,
                    "MAP18_04_OCCUPIED_SURFACE", "OCCUPIED_SURFACE_DIGEST",
                    request.ExpectedOccupiedSurfaceDigest, plan.OccupiedSurfaceDigest,
                    "Computed occupied surface digest differs from expected evidence."));
            return failures.Count == 0 ? Result(plan, failures) : Result(null, failures);
        }

        private static void ValidateUpstream(
            GeneratedPopulationPlacementRequest request,
            ICollection<GeneratedPopulationPlacementFailure> failures)
        {
            if (request.SlotIndex == null)
                failures.Add(Failure(GeneratedPopulationPlacementFailureCode.MissingSlotIndex,
                    "MAP18_01", "SLOT_INDEX", "PRESENT", "MISSING",
                    "Reviewed content slot index is required."));
            if (request.MandatoryPlan == null)
            {
                failures.Add(Failure(
                    GeneratedPopulationPlacementFailureCode.MissingMandatoryPlan,
                    "MAP18_02", "MANDATORY_PLAN", "PRESENT", "MISSING",
                    "Reviewed mandatory unique preplacement plan is required."));
                return;
            }
            if (!string.Equals(request.ExpectedMandatoryPlanDigest,
                    ExpectedMandatoryPlanDigest, StringComparison.Ordinal) ||
                !string.Equals(request.MandatoryPlan.Digest,
                    ExpectedMandatoryPlanDigest, StringComparison.Ordinal))
                failures.Add(Failure(
                    GeneratedPopulationPlacementFailureCode.MandatoryPlanDigestMismatch,
                    "MAP18_02", "PLACEMENT_PLAN_DIGEST", ExpectedMandatoryPlanDigest,
                    request.ExpectedMandatoryPlanDigest + "/" + request.MandatoryPlan.Digest,
                    "Mandatory plan digest differs from reviewed evidence."));
            if (!string.Equals(request.ExpectedMandatoryStableIdSetDigest,
                    ExpectedMandatoryStableIdSetDigest, StringComparison.Ordinal) ||
                !string.Equals(request.MandatoryPlan.StableIdSetDigest,
                    ExpectedMandatoryStableIdSetDigest, StringComparison.Ordinal))
                failures.Add(Failure(
                    GeneratedPopulationPlacementFailureCode.MandatoryStableIdSetDigestMismatch,
                    "MAP18_02", "PLACEMENT_STABLE_ID_SET_DIGEST",
                    ExpectedMandatoryStableIdSetDigest,
                    request.ExpectedMandatoryStableIdSetDigest + "/" +
                    request.MandatoryPlan.StableIdSetDigest,
                    "Mandatory stable ID set differs from reviewed evidence."));
            if (request.ExpectedMandatoryExclusionCount != ExpectedMandatoryExclusionCount ||
                request.MandatoryPlan.ExclusionCount != ExpectedMandatoryExclusionCount)
                failures.Add(Failure(
                    GeneratedPopulationPlacementFailureCode.MandatoryExclusionCountMismatch,
                    "MAP18_02", "EXCLUSION_COUNT", Number(ExpectedMandatoryExclusionCount),
                    Number(request.ExpectedMandatoryExclusionCount) + "/" +
                    Number(request.MandatoryPlan.ExclusionCount),
                    "Mandatory exclusion count differs from reviewed evidence."));
            if (request.SlotIndex != null && (!string.Equals(request.SlotIndex.Digest,
                    request.MandatoryPlan.SourceIndex.Digest, StringComparison.Ordinal) ||
                !string.Equals(request.SlotIndex.StableIdSetDigest,
                    request.MandatoryPlan.SourceIndex.StableIdSetDigest,
                    StringComparison.Ordinal)))
                failures.Add(Failure(
                    GeneratedPopulationPlacementFailureCode.MandatoryPlanDigestMismatch,
                    "MAP18_01", "SOURCE_INDEX", request.MandatoryPlan.SourceIndex.Digest,
                    request.SlotIndex.Digest,
                    "Population and mandatory plans must use the same slot index identity."));
        }

        private static void ValidateCandidateContexts(
            GeneratedPopulationPlacementRequest request,
            ICollection<GeneratedPopulationPlacementFailure> failures)
        {
            if (request.BiomeCatalog == null)
                failures.Add(Failure(
                    GeneratedPopulationPlacementFailureCode.MissingBiomeCatalog,
                    "MAP10_04", "BIOME_PROFILE_CATALOG", "PRESENT", "MISSING",
                    "Typed biome catalog is required for population filters."));
            if (request.NullContextCount > 0)
                failures.Add(Failure(
                    GeneratedPopulationPlacementFailureCode.MissingCandidateContext,
                    "MAP18_03", "NULL_CONTEXTS", "0", Number(request.NullContextCount),
                    "Candidate contexts cannot contain null entries."));
            if (request.SlotIndex == null) return;
            foreach (var group in request.CandidateContexts.Where(value => value.Slot != null)
                         .GroupBy(value => value.Slot.ReservationKey, StringComparer.Ordinal)
                         .Where(value => value.Count() > 1))
                failures.Add(Failure(
                    GeneratedPopulationPlacementFailureCode.DuplicateCandidateContext,
                    "MAP18_03", group.Key, "1", Number(group.Count()),
                    "Each physical slot requires exactly one filter context."));
            foreach (var slot in request.SlotIndex.Entries)
            {
                var matches = request.CandidateContexts.Count(value => value.Slot != null &&
                    string.Equals(value.Slot.ReservationKey, slot.ReservationKey,
                        StringComparison.Ordinal));
                if (matches == 0)
                    failures.Add(Failure(
                        GeneratedPopulationPlacementFailureCode.MissingCandidateContext,
                        "MAP18_03", slot.ReservationKey, "1", "0",
                        "Every source slot requires explicit filter context."));
            }
            foreach (var context in request.CandidateContexts)
            {
                var validSlot = context.Slot != null && request.SlotIndex.Entries.Any(value =>
                    string.Equals(value.ReservationKey, context.Slot.ReservationKey,
                        StringComparison.Ordinal) && string.Equals(value.StableSpawnId.Value,
                        context.Slot.StableSpawnId.Value, StringComparison.Ordinal));
                var validBiome = context.Biome.IsDefined && request.BiomeCatalog != null &&
                    request.BiomeCatalog.TryGetProfile(context.Biome, out _);
                var validTool = context.AvailableTool >= GeneratedPopulationToolRequirement.None &&
                    context.AvailableTool <= GeneratedPopulationToolRequirement.AdvancedHarvestTool;
                if (!validSlot || !validBiome || !validTool || context.InteractionRadius < 0 ||
                    context.SafeRadius < 0 || context.NeighborRadius < 0)
                    failures.Add(Failure(
                        GeneratedPopulationPlacementFailureCode.InvalidFilterRule,
                        "MAP18_03_CONTEXT", context.StableToken,
                        "KNOWN_SLOT_BIOME_TOOL_AND_NON_NEGATIVE_RADII", "INVALID",
                        "Candidate context contains an invalid typed filter value."));
            }
        }

        private static void ValidatePools(
            GeneratedPopulationPlacementRequest request,
            ICollection<GeneratedPopulationPlacementFailure> failures)
        {
            if (request.NullPoolCount > 0)
                failures.Add(Failure(
                    GeneratedPopulationPlacementFailureCode.InvalidFilterRule,
                    "MAP18_03", "NULL_POOLS", "0", Number(request.NullPoolCount),
                    "Population pools cannot contain null entries."));
            foreach (GeneratedPopulationContentKind kind in Enum.GetValues(
                         typeof(GeneratedPopulationContentKind)))
            {
                var count = request.Pools.Count(value => value.Kind == kind);
                if (count == 0) failures.Add(MissingPool(kind));
                if (count > 1) failures.Add(Failure(
                    GeneratedPopulationPlacementFailureCode.DuplicatePopulationPool,
                    "MAP18_03", kind.ToString(), "1", Number(count),
                    "Each logical content group requires exactly one pool."));
            }
            foreach (var pool in request.Pools)
            {
                if (pool.PoolKey == null || !pool.PoolKey.IsValid)
                    failures.Add(Failure(
                        GeneratedPopulationPlacementFailureCode.InvalidPoolKey,
                        "MAP18_03", pool.ContentKey, "VALID_NAMESPACE_AND_VERSION",
                        pool.PoolKey == null ? "MISSING" : pool.PoolKey.ToString(),
                        "Population pool key must be explicit and versioned."));
                var validKind = pool.Kind >= GeneratedPopulationContentKind.ShopInventory &&
                    pool.Kind <= GeneratedPopulationContentKind.NeutralMapElement;
                var validCategories = pool.CompatibleCategories.Count > 0 &&
                    pool.CompatibleCategories.All(value => value >=
                        GeneratedContentSlotCategory.Resource && value <=
                        GeneratedContentSlotCategory.Special);
                var validBiomes = request.BiomeCatalog != null &&
                    pool.BiomeAllowlist.Count > 0 && pool.BiomeAllowlist.All(value =>
                        value.IsDefined && request.BiomeCatalog.TryGetProfile(value, out _));
                var validTool = pool.RequiredTool >= GeneratedPopulationToolRequirement.None &&
                    pool.RequiredTool <= GeneratedPopulationToolRequirement.AdvancedHarvestTool;
                var validRadii = pool.MinimumInteractionRadius >= 0 &&
                    pool.MaximumInteractionRadius >= pool.MinimumInteractionRadius &&
                    pool.MinimumSafeRadius >= 0 && pool.MinimumNeighborRadius >= 0;
                var validPriceBoundary = pool.Kind == GeneratedPopulationContentKind.ShopInventory
                    ? pool.SymbolicPriceTierKey.Length > 0
                    : pool.SymbolicPriceTierKey.Length == 0;
                if (!validKind || pool.ContentKey.Length == 0 || !validCategories ||
                    !validBiomes || !validTool || !validRadii || !validPriceBoundary)
                    failures.Add(Failure(
                        GeneratedPopulationPlacementFailureCode.InvalidFilterRule,
                        "MAP18_03_POOL", pool.ContentKey,
                        "TYPED_CONTENT_CATEGORY_BIOME_TOOL_RADIUS_PRICE_RULE", "INVALID",
                        "Population pool contains an invalid filter or symbolic price boundary."));
            }
        }

        private static bool IsOnlyMandatoryExclusionRejected(
            GeneratedPopulationFilterEvidence value) => value.CategoryAccepted &&
            value.BiomeAccepted && value.ResourceAccepted && value.ToolAccepted &&
            value.InteractionRadiusAccepted && value.SafeRadiusAccepted &&
            value.NeighborRadiusAccepted && !value.MandatoryExclusionAccepted;

        private static bool IsOnlyNeighborRejected(
            GeneratedPopulationFilterEvidence value) => value.CategoryAccepted &&
            value.BiomeAccepted && value.ResourceAccepted && value.ToolAccepted &&
            value.InteractionRadiusAccepted && value.SafeRadiusAccepted &&
            !value.NeighborRadiusAccepted && value.MandatoryExclusionAccepted &&
            value.ReservationAvailable;

        private static GeneratedPopulationPlacementFailure MissingPool(
            GeneratedPopulationContentKind kind) => Failure(kind ==
                GeneratedPopulationContentKind.ShopInventory
                    ? GeneratedPopulationPlacementFailureCode.MissingShopInventoryPool
                    : kind == GeneratedPopulationContentKind.OptionalResource
                        ? GeneratedPopulationPlacementFailureCode.MissingOptionalResourcePool
                        : GeneratedPopulationPlacementFailureCode.MissingNeutralMapElementPool,
                "MAP18_03", kind.ToString(), "ONE_POOL", "0",
                "Required logical population pool is missing.");

        private static GeneratedPopulationPlacementFailure MissingCandidate(
            GeneratedPopulationContentKind kind,
            string contentKey) => Failure(kind == GeneratedPopulationContentKind.ShopInventory
                ? GeneratedPopulationPlacementFailureCode.MissingShopInventoryCandidate
                : kind == GeneratedPopulationContentKind.OptionalResource
                    ? GeneratedPopulationPlacementFailureCode.MissingOptionalResourceCandidate
                    : GeneratedPopulationPlacementFailureCode.MissingNeutralMapElementCandidate,
                "MAP18_03", contentKey, "ONE_ACCEPTED_CANDIDATE", "0",
                "No unreserved candidate satisfies the typed population filters.");

        private static GeneratedPopulationPlacementFailure Failure(
            GeneratedPopulationPlacementFailureCode code,
            string owner,
            string key,
            string expected,
            string actual,
            string reason) => new GeneratedPopulationPlacementFailure(
                code, owner, key, expected, actual, reason);
        private static GeneratedPopulationPlacementResult Result(
            GeneratedPopulationPlacementPlan plan,
            IEnumerable<GeneratedPopulationPlacementFailure> failures) =>
            new GeneratedPopulationPlacementResult(plan, failures);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Flag(bool value) => value ? "1" : "0";
    }
}
