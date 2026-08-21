using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MultiSeedBiomeGrower
    {
        private const string RequiredGenerationProfileId = "GEN_MOONPALACE_V1";
        private const string RequiredWorldProfileId = "WORLD_MOONPALACE_V1";
        private const int RequiredBiomeRetryMaximum = 100;
        private const int PatchHardMaximum = 59;
        private const int ShareScale = 1000000;

        public MultiSeedBiomeGrowthResult Grow(
            SatelliteSeedPlacementResult placementResult,
            GenerationProfileDefinition generationProfile,
            IEnumerable<BiomeTypeDefinition> biomeTypes,
            IEnumerable<BiomePatchRuleDefinition> coreAndSatelliteRules,
            DeterministicRngStream biomePatchRng)
        {
            try
            {
                var errors = new List<MultiSeedBiomeGrowthError>();
                var context = new ValidationContext();
                ValidatePlacement(placementResult, context, errors);
                ValidateGenerationProfile(generationProfile, errors);
                ValidateDefinitions(biomeTypes, coreAndSatelliteRules, context, errors);
                ValidatePatchAndReservationState(context, errors);
                ValidateRng(biomePatchRng, context, errors);
                if (errors.Count != 0) return MultiSeedBiomeGrowthResult.Invalid(errors);
                return Execute(context, biomePatchRng);
            }
            catch
            {
                return MultiSeedBiomeGrowthResult.Invalid(new[]
                {
                    Error(
                        MultiSeedBiomeGrowthErrorCode.InternalInvariantViolation,
                        string.Empty, string.Empty, null, -1, 0, 0,
                        "Multi-seed biome growth violated an internal model invariant.")
                });
            }
        }

        private static void ValidatePlacement(
            SatelliteSeedPlacementResult result,
            ValidationContext context,
            ICollection<MultiSeedBiomeGrowthError> errors)
        {
            context.Result = result;
            if (result == null)
            {
                errors.Add(StructuralError(
                    MultiSeedBiomeGrowthErrorCode.MissingPlacementResult,
                    "A Satellite seed placement result is required."));
                return;
            }
            if (result.Status != SatelliteSeedPlacementStatus.Completed)
                errors.Add(StructuralError(
                    MultiSeedBiomeGrowthErrorCode.PlacementNotCompleted,
                    "Satellite seed placement must be completed."));
            if (result.Publication == null)
            {
                errors.Add(StructuralError(
                    MultiSeedBiomeGrowthErrorCode.MissingPlacementPublication,
                    "A completed Satellite publication is required."));
            }
            if (result.Diagnostics == null)
            {
                errors.Add(StructuralError(
                    MultiSeedBiomeGrowthErrorCode.MissingPlacementDiagnostics,
                    "Satellite placement diagnostics are required."));
            }
            if (result.Publication == null) return;

            context.Placement = result.Publication;
            context.Source = result.Publication.SourceSiteSnapshot;
            context.Input = result.Publication.Snapshot;
            if (context.Source == null)
                errors.Add(StructuralError(
                    MultiSeedBiomeGrowthErrorCode.InvalidSourceSiteSnapshot,
                    "The source P01 reservation snapshot is required."));
            if (context.Input == null)
                errors.Add(StructuralError(
                    MultiSeedBiomeGrowthErrorCode.InvalidPlacementPublication,
                    "The source P02 biome snapshot is required."));
            if (context.Source == null || context.Input == null || result.Diagnostics == null) return;

            var diagnostics = result.Diagnostics;
            if (result.Errors == null || result.Errors.Count != 0 || !result.Succeeded ||
                context.Placement.SourceGrowth == null ||
                context.Source.Seed != context.Input.Seed ||
                diagnostics.WorldSeed != context.Input.Seed ||
                diagnostics.FinalPatchCount != context.Input.Patches.Count ||
                diagnostics.FinalAssignedSectorCount != context.Input.AssignedSectorCount ||
                diagnostics.FinalUnassignedSectorCount != context.Input.UnassignedSectorCount ||
                context.Placement.TotalPatchCount != context.Input.Patches.Count ||
                context.Placement.AssignedSectorCount != context.Input.AssignedSectorCount ||
                context.Placement.UnassignedSectorCount != context.Input.UnassignedSectorCount ||
                context.Input.IsComplete)
                errors.Add(StructuralError(
                    MultiSeedBiomeGrowthErrorCode.InvalidPlacementPublication,
                    "Satellite publication linkage or count conservation is invalid."));
        }

        private static void ValidateGenerationProfile(
            GenerationProfileDefinition profile,
            ICollection<MultiSeedBiomeGrowthError> errors)
        {
            if (profile == null)
            {
                errors.Add(StructuralError(
                    MultiSeedBiomeGrowthErrorCode.MissingGenerationProfile,
                    "The active Moon Palace generation profile is required."));
                return;
            }
            if (!string.Equals(profile.GenerationProfileId, RequiredGenerationProfileId, StringComparison.Ordinal) ||
                !string.Equals(profile.WorldProfileId, RequiredWorldProfileId, StringComparison.Ordinal) ||
                !profile.Active || profile.BiomeRetryMax != RequiredBiomeRetryMaximum)
                errors.Add(Error(
                    MultiSeedBiomeGrowthErrorCode.InvalidGenerationProfile,
                    SafeId(profile.GenerationProfileId), string.Empty, null, -1,
                    RequiredBiomeRetryMaximum, Math.Max(0, profile.BiomeRetryMax),
                    "Generation profile identity, activity, or biome retry limit is invalid."));
        }

        private static void ValidateDefinitions(
            IEnumerable<BiomeTypeDefinition> biomeTypes,
            IEnumerable<BiomePatchRuleDefinition> patchRules,
            ValidationContext context,
            ICollection<MultiSeedBiomeGrowthError> errors)
        {
            if (biomeTypes == null)
                errors.Add(StructuralError(
                    MultiSeedBiomeGrowthErrorCode.MissingBiomeTypes,
                    "Biome type definitions are required."));
            else
            {
                foreach (var biome in biomeTypes)
                {
                    if (biome == null)
                    {
                        errors.Add(StructuralError(
                            MultiSeedBiomeGrowthErrorCode.NullDefinition,
                            "Definition collections cannot contain null."));
                        continue;
                    }
                    var id = SafeId(biome.BiomeId);
                    if (id.Length == 0)
                    {
                        errors.Add(StructuralError(
                            MultiSeedBiomeGrowthErrorCode.InvalidBiomeDefinition,
                            "A biome definition has an invalid canonical ID."));
                        continue;
                    }
                    if (!context.Biomes.TryAdd(id, biome))
                        errors.Add(Error(
                            MultiSeedBiomeGrowthErrorCode.DuplicateDefinitionId,
                            id, id, null, -1, 1, 2,
                            "Biome definition IDs must be unique."));
                }
            }

            if (patchRules == null)
                errors.Add(StructuralError(
                    MultiSeedBiomeGrowthErrorCode.MissingPatchRules,
                    "Core and Satellite patch rules are required."));
            else
            {
                foreach (var rule in patchRules)
                {
                    if (rule == null)
                    {
                        errors.Add(StructuralError(
                            MultiSeedBiomeGrowthErrorCode.NullDefinition,
                            "Definition collections cannot contain null."));
                        continue;
                    }
                    var id = SafeId(rule.PatchRuleId);
                    if (id.Length == 0)
                    {
                        errors.Add(StructuralError(
                            MultiSeedBiomeGrowthErrorCode.InvalidPatchRule,
                            "A patch rule has an invalid canonical ID."));
                        continue;
                    }
                    if (!context.Rules.TryAdd(id, rule))
                        errors.Add(Error(
                            MultiSeedBiomeGrowthErrorCode.DuplicateDefinitionId,
                            id, SafeId(rule.BiomeId), null, -1, 1, 2,
                            "Patch rule IDs must be unique."));
                }
            }

            var specifications = CreateRuleSpecifications();
            var expectedBiomes = new HashSet<string>(
                specifications.Select(value => value.BiomeId), StringComparer.Ordinal);
            var expectedRules = new HashSet<string>(
                specifications.Select(value => value.PatchRuleId), StringComparer.Ordinal);

            foreach (var pair in context.Biomes)
            {
                var biome = pair.Value;
                if (biome.Active && biome.Required && !expectedBiomes.Contains(pair.Key))
                    errors.Add(Error(
                        MultiSeedBiomeGrowthErrorCode.UnexpectedBiomeDefinition,
                        pair.Key, pair.Key, null, -1, 0, 1,
                        "Only the exact four required active biomes are accepted."));
            }
            foreach (var pair in context.Rules)
            {
                var rule = pair.Value;
                if (rule.Active &&
                    (string.Equals(rule.PatchRole, "CORE", StringComparison.Ordinal) ||
                     string.Equals(rule.PatchRole, "SATELLITE", StringComparison.Ordinal) ||
                     string.Equals(rule.PatchRole, "INTRUSION", StringComparison.Ordinal)) &&
                    !expectedRules.Contains(pair.Key))
                    errors.Add(Error(
                        MultiSeedBiomeGrowthErrorCode.UnexpectedPatchRule,
                        pair.Key, SafeId(rule.BiomeId), null, -1, 0, 1,
                        "Only the exact eight active Core and Satellite rules are accepted."));
            }

            foreach (var specification in specifications)
            {
                if (!context.Biomes.TryGetValue(specification.BiomeId, out var biome))
                {
                    errors.Add(Error(
                        MultiSeedBiomeGrowthErrorCode.MissingBiomeDefinition,
                        specification.BiomeId, specification.BiomeId, null, -1, 1, 0,
                        "A required biome definition is missing."));
                }
                else
                {
                    if (!specification.MatchesBiome(biome))
                        errors.Add(Error(
                            MultiSeedBiomeGrowthErrorCode.InvalidBiomeDefinition,
                            biome.BiomeId, biome.BiomeId, null, -1, 1, 0,
                            "A required biome definition does not match the frozen contract."));
                }

                if (!context.Rules.TryGetValue(specification.PatchRuleId, out var rule))
                {
                    errors.Add(Error(
                        MultiSeedBiomeGrowthErrorCode.MissingPatchRule,
                        specification.PatchRuleId, specification.BiomeId, null, -1, 1, 0,
                        "A required Core or Satellite patch rule is missing."));
                    continue;
                }
                if (!specification.MatchesRule(rule, out var weights))
                    errors.Add(Error(
                        MultiSeedBiomeGrowthErrorCode.InvalidPatchRule,
                        specification.PatchRuleId, specification.BiomeId, null, -1, 1, 0,
                        "A required patch rule does not match the frozen contract."));
                else
                {
                    context.RuleSpecifications[specification.PatchRuleId] = specification;
                    context.RuleWeights[specification.PatchRuleId] = weights;
                }
                if (!string.Equals(rule.BiomeId, specification.BiomeId, StringComparison.Ordinal))
                    errors.Add(Error(
                        MultiSeedBiomeGrowthErrorCode.DefinitionIdentityMismatch,
                        specification.PatchRuleId, specification.BiomeId, null, -1, 1, 0,
                        "Patch rule and biome identities must match exactly."));
            }
        }

        private static void ValidatePatchAndReservationState(
            ValidationContext context,
            ICollection<MultiSeedBiomeGrowthError> errors)
        {
            if (context.Source == null || context.Input == null) return;
            if (context.Source.Sectors == null || context.Source.Sectors.Count != WorldGenConstants.SectorCount ||
                context.Source.Reservations == null || context.Source.CoreBiomeSeeds == null)
                errors.Add(StructuralError(
                    MultiSeedBiomeGrowthErrorCode.InvalidSourceSiteSnapshot,
                    "Source P01 collections or sector count are invalid."));
            if (context.Input.Patches == null || context.Input.Sectors == null ||
                context.Input.SiteBindings == null ||
                context.Input.Sectors.Count != WorldGenConstants.SectorCount)
            {
                errors.Add(StructuralError(
                    MultiSeedBiomeGrowthErrorCode.InvalidPatchState,
                    "Input P02 collections or sector count are invalid."));
                return;
            }

            var coreCount = 0;
            var intrusionCount = 0;
            var patchIds = new HashSet<BiomePatchId>();
            var biomeCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var patch in context.Input.Patches)
            {
                if (patch == null || !patch.Id.IsValid || !patchIds.Add(patch.Id) ||
                    patch.Seeds == null || patch.Seeds.Count == 0 || patch.SectorIndices == null)
                {
                    errors.Add(StructuralError(
                        MultiSeedBiomeGrowthErrorCode.InvalidPatchState,
                        "Input patches must be non-null with unique IDs, seeds, and sectors."));
                    continue;
                }
                if (patch.Role == BiomePatchRole.Core) coreCount++;
                else if (patch.Role == BiomePatchRole.Intrusion) intrusionCount++;
                else if (patch.Role != BiomePatchRole.Satellite)
                    errors.Add(Error(
                        MultiSeedBiomeGrowthErrorCode.InvalidPatchState,
                        patch.PatchRuleId, patch.BiomeId, patch.Id, -1, 1, 0,
                        "Patch role is undefined."));

                if (!context.Rules.TryGetValue(patch.PatchRuleId, out var rule) ||
                    !context.RuleSpecifications.TryGetValue(patch.PatchRuleId, out var specification) ||
                    !string.Equals(patch.BiomeId, specification.BiomeId, StringComparison.Ordinal) ||
                    patch.Role != specification.Role || patch.SectorCount > Math.Min(rule.MaxSectorCount, PatchHardMaximum))
                    errors.Add(Error(
                        MultiSeedBiomeGrowthErrorCode.InvalidPatchState,
                        SafeId(patch.PatchRuleId), SafeId(patch.BiomeId), patch.Id, -1,
                        1, Math.Max(0, patch.SectorCount),
                        "Patch role, rule, biome, or current size is invalid."));

                foreach (var seed in patch.Seeds)
                    if (seed == null || seed.Role != patch.Role || !patch.ContainsSector(seed.SectorIndex) ||
                        (patch.Role == BiomePatchRole.Core) != seed.SourceSiteReservationId.HasValue)
                        errors.Add(Error(
                            MultiSeedBiomeGrowthErrorCode.InvalidPatchState,
                            SafeId(patch.PatchRuleId), SafeId(patch.BiomeId), patch.Id,
                            seed == null ? -1 : seed.SectorIndex, 1, 0,
                            "Patch seed linkage is invalid."));
                biomeCounts[patch.BiomeId] = GetCount(biomeCounts, patch.BiomeId) + patch.SectorCount;
                context.Patches[patch.Id] = patch;
            }
            if (coreCount != 4 || intrusionCount != 0)
                errors.Add(Error(
                    MultiSeedBiomeGrowthErrorCode.InvalidPatchState,
                    string.Empty, string.Empty, null, -1, 4, coreCount,
                    "Input must contain exact four Core patches and zero Intrusion patches."));
            if (context.Input.SiteBindings.Count != 4)
                errors.Add(Error(
                    MultiSeedBiomeGrowthErrorCode.InvalidPatchState,
                    string.Empty, string.Empty, null, -1, 4, context.Input.SiteBindings.Count,
                    "Input must preserve exact four Core site bindings."));

            var bindingByPatch = new Dictionary<BiomePatchId, BiomePatchSiteBinding>();
            foreach (var binding in context.Input.SiteBindings)
            {
                if (binding == null || !bindingByPatch.TryAdd(binding.PatchId, binding) ||
                    !context.Input.TryGetPatch(binding.PatchId, out var patch) ||
                    patch.Role != BiomePatchRole.Core ||
                    !string.Equals(binding.BiomeId, patch.BiomeId, StringComparison.Ordinal))
                    errors.Add(StructuralError(
                        MultiSeedBiomeGrowthErrorCode.InvalidPatchState,
                        "Core site bindings must be unique and match Core patches."));
            }

            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var reservation = context.Source.GetSector(index);
                var ownership = context.Input.GetSector(index);
                if (reservation == null || reservation.Index != index ||
                    reservation.Coordinate != WorldGridIndex.ToCoordinate(index))
                {
                    errors.Add(Error(
                        MultiSeedBiomeGrowthErrorCode.InvalidReservationState,
                        string.Empty, string.Empty, null, index, 1, 0,
                        "A P01 sector row has invalid grid identity."));
                    continue;
                }
                if (ownership == null || ownership.SectorIndex != index ||
                    ownership.Sector != reservation.Coordinate || ownership.SecondaryBiomeId == null ||
                    ownership.SecondaryBiomeId.Length != 0)
                {
                    errors.Add(Error(
                        MultiSeedBiomeGrowthErrorCode.InvalidPatchState,
                        string.Empty, string.Empty, null, index, 1, 0,
                        "A P02 ownership row has invalid identity or SecondaryBiome."));
                    continue;
                }
                if (!ownership.IsAssigned || !reservation.IsReserved) continue;
                if (!ownership.PatchId.HasValue || !context.Input.TryGetPatch(ownership.PatchId.Value, out var owner) ||
                    owner.Role != BiomePatchRole.Core || !bindingByPatch.TryGetValue(owner.Id, out var binding) ||
                    !reservation.ReservationId.HasValue || binding.SiteReservationId != reservation.ReservationId.Value ||
                    !Contains(binding.OccupiedSectorIndices, index))
                    errors.Add(Error(
                        MultiSeedBiomeGrowthErrorCode.InvalidReservationState,
                        string.Empty, SafeId(ownership.PrimaryBiomeId), ownership.PatchId, index, 1, 0,
                        "Assigned reserved sectors must belong to their exact bound Core patch."));
            }

            foreach (var biome in biomeCounts)
            {
                var capacity = GetBiomeShareCapacity(context, biome.Key);
                if (biome.Value > capacity)
                    errors.Add(Error(
                        MultiSeedBiomeGrowthErrorCode.InvalidPatchState,
                        string.Empty, biome.Key, null, -1, capacity, biome.Value,
                        "Current biome ownership exceeds its world-share capacity."));
            }
        }

        private static void ValidateRng(
            DeterministicRngStream rng,
            ValidationContext context,
            ICollection<MultiSeedBiomeGrowthError> errors)
        {
            if (rng == null)
            {
                errors.Add(StructuralError(
                    MultiSeedBiomeGrowthErrorCode.MissingBiomePatchRng,
                    "The continued RNG_BIOME_PATCH stream is required."));
                return;
            }
            if (context.Result == null || context.Result.Diagnostics == null) return;
            var expected = context.Result.Diagnostics.RngDrawCountAfter;
            if (rng.DrawCount != expected)
                errors.Add(Error(
                    MultiSeedBiomeGrowthErrorCode.InvalidBiomePatchRngState,
                    WorldGenerationRngStreams.BiomePatchStreamId,
                    string.Empty, null, -1,
                    expected > int.MaxValue ? int.MaxValue : (int)expected,
                    rng.DrawCount > int.MaxValue ? int.MaxValue : (int)rng.DrawCount,
                    "Biome patch RNG DrawCount must continue the successful placement attempt."));
        }

        private static MultiSeedBiomeGrowthResult Execute(
            ValidationContext context,
            DeterministicRngStream rng)
        {
            var input = context.Input;
            var targetSectors = new List<int>();
            var hardBlocked = 0;
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                if (input.GetSector(index).IsAssigned) continue;
                if (context.Source.GetSector(index).IsReserved) hardBlocked++;
                else targetSectors.Add(index);
            }

            var targetOwned = WorldGenConstants.SectorCount - hardBlocked;
            var aggregateCapacity = GetAggregateCapacity(context);
            var rngBefore = rng.DrawCount;
            if (aggregateCapacity < targetOwned)
            {
                var capacityDiagnostics = CreateDiagnostics(
                    context, targetSectors.Count, hardBlocked, targetOwned, aggregateCapacity,
                    0, 0, input.AssignedSectorCount, input.UnassignedSectorCount,
                    null, rngBefore, rng.DrawCount, 0, 0, 0,
                    CreatePatchCounts(input), CreateBiomeCounts(input));
                return MultiSeedBiomeGrowthResult.Retry(capacityDiagnostics, new[]
                {
                    Error(
                        MultiSeedBiomeGrowthErrorCode.InsufficientAggregateCapacity,
                        string.Empty, string.Empty, null, -1,
                        targetOwned, aggregateCapacity,
                        "Aggregate legal patch and biome capacity cannot own every target sector.")
                });
            }

            var patches = new List<PatchWork>();
            foreach (var patch in input.Patches.OrderBy(value => value.Id))
                patches.Add(new PatchWork(
                    patch,
                    context.Rules[patch.PatchRuleId],
                    context.RuleSpecifications[patch.PatchRuleId],
                    context.RuleWeights[patch.PatchRuleId]));

            var noiseValues = new List<int>(checked(patches.Count * targetSectors.Count));
            foreach (var patch in patches)
                foreach (var sectorIndex in targetSectors)
                    noiseValues.Add(rng.NextInt(1001));
            var noiseTable = new BiomeGrowthNoiseTable(
                patches.Select(value => value.Patch.Id), targetSectors, noiseValues,
                noiseValues.Count, rngBefore, rng.DrawCount);

            var ownerBySector = new PatchWork[WorldGenConstants.SectorCount];
            foreach (var patch in patches)
                foreach (var sectorIndex in patch.Sectors)
                    ownerBySector[sectorIndex] = patch;
            var biomeCounts = CreateBiomeCounts(input).ToDictionary(
                value => value.Key, value => value.Value, StringComparer.Ordinal);
            var records = new List<MultiSeedBiomeGrowthRecord>();
            var minimumClaims = 0;
            var competitiveClaims = 0;
            var reservationPenaltyClaims = 0;

            while (true)
            {
                var deficits = patches.Where(value => value.Sectors.Count < value.Rule.MinSectorCount).ToArray();
                if (deficits.Length == 0) break;
                var progress = false;
                foreach (var patch in deficits)
                {
                    if (patch.Sectors.Count >= patch.Rule.MinSectorCount) continue;
                    if (!TryFindBestCandidate(
                            patch, ownerBySector, biomeCounts, context, noiseTable,
                            out var sectorIndex, out var cost))
                        continue;
                    Claim(patch, sectorIndex, cost, true, ownerBySector, biomeCounts, records);
                    minimumClaims++;
                    if (cost.ReservationTerm2 != 0) reservationPenaltyClaims++;
                    progress = true;
                }
                if (!progress)
                    return RetrySpatial(
                        context, targetSectors.Count, hardBlocked, targetOwned, aggregateCapacity,
                        noiseTable, rngBefore, rng.DrawCount,
                        MultiSeedBiomeGrowthErrorCode.MinimumGrowthBlocked,
                        "A below-minimum patch has no legal cardinal growth candidate.");
            }

            var heap = new StableMinHeap();
            foreach (var patch in patches)
                EnqueueFrontier(patch, ownerBySector, biomeCounts, context, noiseTable, heap);

            var remaining = 0;
            foreach (var sectorIndex in targetSectors)
                if (ownerBySector[sectorIndex] == null) remaining++;
            while (remaining > 0)
            {
                HeapEntry entry = null;
                while (heap.Count != 0)
                {
                    var candidate = heap.Pop();
                    if (ownerBySector[candidate.SectorIndex] != null) continue;
                    if (candidate.Patch.Revision != candidate.Revision)
                    {
                        if (TryCreateCandidate(
                                candidate.Patch, candidate.SectorIndex, ownerBySector,
                                biomeCounts, context, noiseTable, out var refreshed))
                            heap.Push(refreshed);
                        continue;
                    }
                    if (!TryCreateCandidate(
                            candidate.Patch, candidate.SectorIndex, ownerBySector,
                            biomeCounts, context, noiseTable, out var current))
                        continue;
                    if (current.Cost.TotalCost2 != candidate.Cost.TotalCost2)
                    {
                        heap.Push(current);
                        continue;
                    }
                    entry = current;
                    break;
                }
                if (entry == null)
                    return RetrySpatial(
                        context, targetSectors.Count, hardBlocked, targetOwned, aggregateCapacity,
                        noiseTable, rngBefore, rng.DrawCount,
                        MultiSeedBiomeGrowthErrorCode.GrowthFrontierExhausted,
                        "Stable multi-seed frontier exhausted before every target was owned.");

                Claim(entry.Patch, entry.SectorIndex, entry.Cost, false,
                    ownerBySector, biomeCounts, records);
                competitiveClaims++;
                remaining--;
                if (entry.Cost.ReservationTerm2 != 0) reservationPenaltyClaims++;
                EnqueueFrontier(entry.Patch, ownerBySector, biomeCounts, context, noiseTable, heap);
            }

            var outputPatches = new List<BiomePatch>();
            foreach (var patch in patches)
                outputPatches.Add(new BiomePatch(
                    patch.Patch.Id, patch.Patch.BiomeId, patch.Patch.PatchRuleId,
                    patch.Patch.Role, patch.Patch.Seeds, patch.Sectors));
            var ownership = new List<BiomeSectorOwnership>(WorldGenConstants.SectorCount);
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var sourceOwnership = input.GetSector(index);
                if (sourceOwnership.IsAssigned || ownerBySector[index] == null)
                    ownership.Add(sourceOwnership);
                else
                {
                    var patch = ownerBySector[index].Patch;
                    ownership.Add(new BiomeSectorOwnership(
                        index, WorldGridIndex.ToCoordinate(index), patch.BiomeId,
                        string.Empty, patch.Id));
                }
            }
            var snapshot = new BiomePatchSnapshot(
                input.Seed, outputPatches, ownership, input.SiteBindings);
            var disconnected = CountDisconnected(outputPatches);
            var overlap = CountOverlap(outputPatches);
            if (disconnected != 0 || overlap != 0 ||
                snapshot.AssignedSectorCount != targetOwned ||
                snapshot.UnassignedSectorCount != hardBlocked)
                throw new InvalidOperationException("Final growth invariants are invalid.");

            var publication = new MultiSeedBiomeGrowthPublication(
                context.Placement, snapshot, records,
                input.AssignedSectorCount, hardBlocked);
            var diagnostics = CreateDiagnostics(
                context, targetSectors.Count, hardBlocked, targetOwned, aggregateCapacity,
                minimumClaims, competitiveClaims,
                snapshot.AssignedSectorCount, snapshot.UnassignedSectorCount,
                noiseTable, rngBefore, rng.DrawCount,
                reservationPenaltyClaims, overlap, disconnected,
                CreatePatchCounts(snapshot), CreateBiomeCounts(snapshot));
            return MultiSeedBiomeGrowthResult.Completed(publication, diagnostics);
        }

        private static MultiSeedBiomeGrowthResult RetrySpatial(
            ValidationContext context,
            int targetCount,
            int hardBlocked,
            int targetOwned,
            int aggregateCapacity,
            BiomeGrowthNoiseTable noiseTable,
            ulong rngBefore,
            ulong rngAfter,
            MultiSeedBiomeGrowthErrorCode code,
            string message)
        {
            var input = context.Input;
            var diagnostics = CreateDiagnostics(
                context, targetCount, hardBlocked, targetOwned, aggregateCapacity,
                0, 0, input.AssignedSectorCount, input.UnassignedSectorCount,
                noiseTable, rngBefore, rngAfter, 0, 0, 0,
                CreatePatchCounts(input), CreateBiomeCounts(input));
            return MultiSeedBiomeGrowthResult.Retry(diagnostics, new[]
            {
                Error(code, string.Empty, string.Empty, null, -1,
                    targetCount, 0, message)
            });
        }

        private static bool TryFindBestCandidate(
            PatchWork patch,
            PatchWork[] ownerBySector,
            IDictionary<string, int> biomeCounts,
            ValidationContext context,
            BiomeGrowthNoiseTable noiseTable,
            out int sectorIndex,
            out BiomeGrowthCost cost)
        {
            sectorIndex = -1;
            cost = null;
            foreach (var candidateIndex in EnumerateFrontier(patch, ownerBySector))
            {
                if (!TryCreateCandidate(
                        patch, candidateIndex, ownerBySector, biomeCounts,
                        context, noiseTable, out var candidate))
                    continue;
                if (cost == null || candidate.Cost.TotalCost2 < cost.TotalCost2 ||
                    (candidate.Cost.TotalCost2 == cost.TotalCost2 && candidateIndex < sectorIndex))
                {
                    sectorIndex = candidateIndex;
                    cost = candidate.Cost;
                }
            }
            return cost != null;
        }

        private static bool TryCreateCandidate(
            PatchWork patch,
            int sectorIndex,
            PatchWork[] ownerBySector,
            IDictionary<string, int> biomeCounts,
            ValidationContext context,
            BiomeGrowthNoiseTable noiseTable,
            out HeapEntry entry)
        {
            entry = null;
            if (sectorIndex < 0 || sectorIndex >= WorldGenConstants.SectorCount ||
                ownerBySector[sectorIndex] != null || context.Source.GetSector(sectorIndex).IsReserved ||
                patch.Sectors.Count >= Math.Min(patch.Rule.MaxSectorCount, PatchHardMaximum) ||
                GetCount(biomeCounts, patch.Patch.BiomeId) >= GetBiomeShareCapacity(context, patch.Patch.BiomeId) ||
                !HasCardinalNeighbor(patch.Sectors, sectorIndex))
                return false;

            var coordinate = WorldGridIndex.ToCoordinate(sectorIndex);
            if (!patch.Rule.CanTouchWorldEdge && IsWorldEdge(coordinate)) return false;
            var graphDistance = int.MaxValue;
            foreach (var seed in patch.Patch.Seeds)
            {
                var distance = Math.Abs(seed.Sector.X - coordinate.X) +
                               Math.Abs(seed.Sector.Y - coordinate.Y);
                if (distance < graphDistance) graphDistance = distance;
            }
            var sameNeighbors = CountCardinalNeighbors(patch.Sectors, sectorIndex);
            var altitudeDistance2 = Math.Abs(
                (2 * coordinate.Y) -
                (patch.Specification.PreferredAltitudeMinimum +
                 patch.Specification.PreferredAltitudeMaximum));
            var penalty = HasReservationPenalty(context.Source, sectorIndex);
            var cost = new BiomeGrowthCost(
                graphDistance, altitudeDistance2,
                noiseTable.GetNoise(patch.Patch.Id, sectorIndex), sameNeighbors, penalty,
                patch.Weights.Distance, patch.Weights.Altitude,
                patch.Weights.Noise, patch.Weights.Compactness);
            entry = new HeapEntry(patch, sectorIndex, patch.Revision, cost);
            return true;
        }

        private static void Claim(
            PatchWork patch,
            int sectorIndex,
            BiomeGrowthCost cost,
            bool minimumPhase,
            PatchWork[] ownerBySector,
            IDictionary<string, int> biomeCounts,
            ICollection<MultiSeedBiomeGrowthRecord> records)
        {
            var before = patch.Sectors.Count;
            if (!patch.Sectors.Add(sectorIndex) || ownerBySector[sectorIndex] != null)
                throw new InvalidOperationException("Growth claim overlaps existing ownership.");
            ownerBySector[sectorIndex] = patch;
            patch.Revision++;
            biomeCounts[patch.Patch.BiomeId] = GetCount(biomeCounts, patch.Patch.BiomeId) + 1;
            records.Add(new MultiSeedBiomeGrowthRecord(
                records.Count, patch.Patch.Id, patch.Patch.BiomeId, patch.Patch.Role,
                sectorIndex, WorldGridIndex.ToCoordinate(sectorIndex),
                before, before + 1, minimumPhase, cost));
        }

        private static void EnqueueFrontier(
            PatchWork patch,
            PatchWork[] ownerBySector,
            IDictionary<string, int> biomeCounts,
            ValidationContext context,
            BiomeGrowthNoiseTable noiseTable,
            StableMinHeap heap)
        {
            foreach (var sectorIndex in EnumerateFrontier(patch, ownerBySector))
                if (TryCreateCandidate(
                        patch, sectorIndex, ownerBySector, biomeCounts,
                        context, noiseTable, out var entry))
                    heap.Push(entry);
        }

        private static IEnumerable<int> EnumerateFrontier(PatchWork patch, PatchWork[] ownerBySector)
        {
            var frontier = new SortedSet<int>();
            foreach (var owned in patch.Sectors)
                foreach (var neighbor in GetNeighbors(owned))
                    if (ownerBySector[neighbor] == null) frontier.Add(neighbor);
            return frontier;
        }

        private static int GetAggregateCapacity(ValidationContext context)
        {
            var totals = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var patch in context.Input.Patches)
            {
                var rule = context.Rules[patch.PatchRuleId];
                totals[patch.BiomeId] = checked(
                    GetCount(totals, patch.BiomeId) + Math.Min(rule.MaxSectorCount, PatchHardMaximum));
            }
            var aggregate = 0;
            foreach (var pair in totals)
                aggregate = checked(aggregate + Math.Min(pair.Value, GetBiomeShareCapacity(context, pair.Key)));
            return aggregate;
        }

        private static int GetBiomeShareCapacity(ValidationContext context, string biomeId)
        {
            var share = -1;
            foreach (var rule in context.Rules.Values)
            {
                if (!rule.Active || !string.Equals(rule.BiomeId, biomeId, StringComparison.Ordinal) ||
                    (!string.Equals(rule.PatchRole, "CORE", StringComparison.Ordinal) &&
                     !string.Equals(rule.PatchRole, "SATELLITE", StringComparison.Ordinal)))
                    continue;
                var quantized = QuantizeShare(rule.MaxWorldShare);
                if (share < 0) share = quantized;
                else if (share != quantized) return 0;
            }
            return share < 0 ? 0 :
                (int)(((long)WorldGenConstants.SectorCount * share) / ShareScale);
        }

        private static int QuantizeShare(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f || value > 1f)
                return -1;
            var rounded = Math.Round((double)value * ShareScale, MidpointRounding.AwayFromZero);
            if (Math.Abs((double)value - (rounded / ShareScale)) > 0.000001d)
                return -1;
            return (int)rounded;
        }

        private static bool HasReservationPenalty(SiteReservationSnapshot source, int sectorIndex)
        {
            foreach (var neighbor in GetNeighbors(sectorIndex))
            {
                var row = source.GetSector(neighbor);
                if (row.IsReserved && row.Kind.HasValue &&
                    (row.Kind.Value == SiteReservationKind.Boss ||
                     row.Kind.Value == SiteReservationKind.Village))
                    return true;
            }
            return false;
        }

        private static bool HasCardinalNeighbor(ISet<int> sectors, int sectorIndex)
        {
            foreach (var neighbor in GetNeighbors(sectorIndex))
                if (sectors.Contains(neighbor)) return true;
            return false;
        }

        private static int CountCardinalNeighbors(ISet<int> sectors, int sectorIndex)
        {
            var count = 0;
            foreach (var neighbor in GetNeighbors(sectorIndex))
                if (sectors.Contains(neighbor)) count++;
            return count;
        }

        private static IEnumerable<int> GetNeighbors(int sectorIndex)
        {
            var left = WorldGridIndex.GetLeftIndex(sectorIndex);
            var right = WorldGridIndex.GetRightIndex(sectorIndex);
            var down = WorldGridIndex.GetDownIndex(sectorIndex);
            var up = WorldGridIndex.GetUpIndex(sectorIndex);
            if (left != SectorNeighborIndices.NoNeighbor) yield return left;
            if (right != SectorNeighborIndices.NoNeighbor) yield return right;
            if (down != SectorNeighborIndices.NoNeighbor) yield return down;
            if (up != SectorNeighborIndices.NoNeighbor) yield return up;
        }

        private static bool IsWorldEdge(SectorCoord coordinate)
        {
            return coordinate.X == 0 || coordinate.Y == 0 ||
                   coordinate.X == WorldGenConstants.SectorColumns - 1 ||
                   coordinate.Y == WorldGenConstants.SectorRows - 1;
        }

        private static int CountDisconnected(IEnumerable<BiomePatch> patches)
        {
            var disconnected = 0;
            foreach (var patch in patches)
            {
                var remaining = new HashSet<int>(patch.SectorIndices);
                var queue = new Queue<int>();
                queue.Enqueue(patch.SectorIndices[0]);
                remaining.Remove(patch.SectorIndices[0]);
                while (queue.Count != 0)
                {
                    var current = queue.Dequeue();
                    foreach (var neighbor in GetNeighbors(current))
                        if (remaining.Remove(neighbor)) queue.Enqueue(neighbor);
                }
                if (remaining.Count != 0) disconnected++;
            }
            return disconnected;
        }

        private static int CountOverlap(IEnumerable<BiomePatch> patches)
        {
            var seen = new HashSet<int>();
            var overlap = 0;
            foreach (var patch in patches)
                foreach (var sector in patch.SectorIndices)
                    if (!seen.Add(sector)) overlap++;
            return overlap;
        }

        private static IReadOnlyDictionary<BiomePatchId, int> CreatePatchCounts(BiomePatchSnapshot snapshot)
        {
            var values = new SortedDictionary<BiomePatchId, int>();
            foreach (var patch in snapshot.Patches) values.Add(patch.Id, patch.SectorCount);
            return new ReadOnlyDictionary<BiomePatchId, int>(values);
        }

        private static IReadOnlyDictionary<string, int> CreateBiomeCounts(BiomePatchSnapshot snapshot)
        {
            var values = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var patch in snapshot.Patches)
                values[patch.BiomeId] = GetCount(values, patch.BiomeId) + patch.SectorCount;
            return new ReadOnlyDictionary<string, int>(values);
        }

        private static MultiSeedBiomeGrowthDiagnostics CreateDiagnostics(
            ValidationContext context,
            int targetUnassigned,
            int hardBlocked,
            int targetOwned,
            int aggregateCapacity,
            int minimumClaims,
            int competitiveClaims,
            int finalAssigned,
            int finalUnassigned,
            BiomeGrowthNoiseTable noise,
            ulong rngBefore,
            ulong rngAfter,
            int penaltyClaims,
            int overlap,
            int disconnected,
            IEnumerable<KeyValuePair<BiomePatchId, int>> patchCounts,
            IEnumerable<KeyValuePair<string, int>> biomeCounts)
        {
            return new MultiSeedBiomeGrowthDiagnostics(
                context.Input.Seed, context.Input.Patches.Count,
                context.Input.AssignedSectorCount, targetUnassigned, hardBlocked,
                targetOwned, aggregateCapacity, minimumClaims, competitiveClaims,
                finalAssigned, finalUnassigned, noise, rngBefore, rngAfter,
                penaltyClaims, overlap, disconnected, patchCounts, biomeCounts);
        }

        private static int GetCount<TKey>(IDictionary<TKey, int> values, TKey key)
        {
            return values.TryGetValue(key, out var value) ? value : 0;
        }

        private static bool Contains(IReadOnlyList<int> values, int value)
        {
            for (var index = 0; index < values.Count; index++)
                if (values[index] == value) return true;
            return false;
        }

        private static string SafeId(string value)
        {
            return ReservationValidation.IsCanonicalId(value, true) ? value : string.Empty;
        }

        private static MultiSeedBiomeGrowthError StructuralError(
            MultiSeedBiomeGrowthErrorCode code,
            string message)
        {
            return Error(code, string.Empty, string.Empty, null, -1, 0, 0, message);
        }

        private static MultiSeedBiomeGrowthError Error(
            MultiSeedBiomeGrowthErrorCode code,
            string definitionId,
            string biomeId,
            BiomePatchId? patchId,
            int sectorIndex,
            int requiredCount,
            int availableCount,
            string message)
        {
            return new MultiSeedBiomeGrowthError(
                code, SafeId(definitionId), SafeId(biomeId),
                patchId.HasValue && patchId.Value.IsValid ? patchId : null,
                sectorIndex, Math.Max(0, requiredCount), Math.Max(0, availableCount), message);
        }

        private static IReadOnlyList<RuleSpecification> CreateRuleSpecifications()
        {
            return new[]
            {
                new RuleSpecification("PATCH_CRATER_CORE", "BIO_MOON_CRATER", BiomePatchRole.Core,
                    5, 18, true, 0, 7, 1000, 250, 450, 750),
                new RuleSpecification("PATCH_CRATER_SAT", "BIO_MOON_CRATER", BiomePatchRole.Satellite,
                    2, 16, true, 0, 7, 1000, 250, 600, 650),
                new RuleSpecification("PATCH_DOUGH_CORE", "BIO_MOON_DOUGH", BiomePatchRole.Core,
                    5, 18, true, 0, 7, 1000, 400, 450, 700),
                new RuleSpecification("PATCH_DOUGH_SAT", "BIO_MOON_DOUGH", BiomePatchRole.Satellite,
                    2, 14, true, 0, 7, 1000, 400, 600, 650),
                new RuleSpecification("PATCH_MILL_CORE", "BIO_ABANDONED_MILL", BiomePatchRole.Core,
                    4, 14, false, 1, 11, 1000, 200, 350, 850),
                new RuleSpecification("PATCH_MILL_SAT", "BIO_ABANDONED_MILL", BiomePatchRole.Satellite,
                    2, 10, false, 1, 11, 1000, 200, 500, 800),
                new RuleSpecification("PATCH_ROOT_CORE", "BIO_CASSIA_ROOT", BiomePatchRole.Core,
                    5, 18, false, 2, 12, 1000, 350, 450, 700),
                new RuleSpecification("PATCH_ROOT_SAT", "BIO_CASSIA_ROOT", BiomePatchRole.Satellite,
                    2, 14, false, 2, 12, 1000, 350, 600, 600)
            };
        }

        private sealed class ValidationContext
        {
            public SatelliteSeedPlacementResult Result { get; set; }
            public SatelliteSeedPlacementPublication Placement { get; set; }
            public SiteReservationSnapshot Source { get; set; }
            public BiomePatchSnapshot Input { get; set; }
            public Dictionary<string, BiomeTypeDefinition> Biomes { get; } =
                new Dictionary<string, BiomeTypeDefinition>(StringComparer.Ordinal);
            public Dictionary<string, BiomePatchRuleDefinition> Rules { get; } =
                new Dictionary<string, BiomePatchRuleDefinition>(StringComparer.Ordinal);
            public Dictionary<string, RuleSpecification> RuleSpecifications { get; } =
                new Dictionary<string, RuleSpecification>(StringComparer.Ordinal);
            public Dictionary<string, QuantizedWeights> RuleWeights { get; } =
                new Dictionary<string, QuantizedWeights>(StringComparer.Ordinal);
            public Dictionary<BiomePatchId, BiomePatch> Patches { get; } =
                new Dictionary<BiomePatchId, BiomePatch>();
        }

        private sealed class RuleSpecification
        {
            public RuleSpecification(
                string patchRuleId,
                string biomeId,
                BiomePatchRole role,
                int minimum,
                int maximum,
                bool canTouchWorldEdge,
                int preferredAltitudeMinimum,
                int preferredAltitudeMaximum,
                int distance,
                int altitude,
                int noise,
                int compactness)
            {
                PatchRuleId = patchRuleId;
                BiomeId = biomeId;
                Role = role;
                Minimum = minimum;
                Maximum = maximum;
                CanTouchWorldEdge = canTouchWorldEdge;
                PreferredAltitudeMinimum = preferredAltitudeMinimum;
                PreferredAltitudeMaximum = preferredAltitudeMaximum;
                Weights = new QuantizedWeights(distance, altitude, noise, compactness);
            }

            public string PatchRuleId { get; }
            public string BiomeId { get; }
            public BiomePatchRole Role { get; }
            public int Minimum { get; }
            public int Maximum { get; }
            public bool CanTouchWorldEdge { get; }
            public int PreferredAltitudeMinimum { get; }
            public int PreferredAltitudeMaximum { get; }
            public QuantizedWeights Weights { get; }

            public bool MatchesBiome(BiomeTypeDefinition biome)
            {
                return biome != null && biome.Active && biome.Required &&
                       string.Equals(biome.BiomeId, BiomeId, StringComparison.Ordinal) &&
                       biome.MinPatchCount >= 1 && biome.MaxPatchCount >= biome.MinPatchCount &&
                       biome.MinCorePatchCount >= 1 &&
                       biome.PreferredAltitudeMinSectorY == PreferredAltitudeMinimum &&
                       biome.PreferredAltitudeMaxSectorY == PreferredAltitudeMaximum &&
                       IsPositiveFinite(biome.GrowthWeight);
            }

            public bool MatchesRule(BiomePatchRuleDefinition rule, out QuantizedWeights weights)
            {
                weights = null;
                var roleToken = BiomePatchRoleTokenCodec.ToToken(Role);
                if (rule == null || !rule.Active ||
                    !string.Equals(rule.PatchRuleId, PatchRuleId, StringComparison.Ordinal) ||
                    !string.Equals(rule.BiomeId, BiomeId, StringComparison.Ordinal) ||
                    !string.Equals(rule.PatchRole, roleToken, StringComparison.Ordinal) ||
                    rule.MinSectorCount != Minimum || rule.MaxSectorCount != Maximum ||
                    rule.MaxSectorCount > PatchHardMaximum ||
                    rule.CanTouchWorldEdge != CanTouchWorldEdge ||
                    QuantizeShare(rule.MaxWorldShare) != 350000 ||
                    !IsPositiveFinite(rule.SeedWeight) || !IsPositiveFinite(rule.BranchinessTarget) ||
                    !BiomeGrowthCost.TryQuantizeWeight(rule.DistanceWeight, out var distance) ||
                    !BiomeGrowthCost.TryQuantizeWeight(rule.AltitudeWeight, out var altitude) ||
                    !BiomeGrowthCost.TryQuantizeWeight(rule.NoiseWeight, out var noise) ||
                    !BiomeGrowthCost.TryQuantizeWeight(rule.CompactnessWeight, out var compactness) ||
                    distance != Weights.Distance || altitude != Weights.Altitude ||
                    noise != Weights.Noise || compactness != Weights.Compactness)
                    return false;
                weights = new QuantizedWeights(distance, altitude, noise, compactness);
                return true;
            }

            private static bool IsPositiveFinite(float value)
            {
                return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
            }
        }

        private sealed class QuantizedWeights
        {
            public QuantizedWeights(int distance, int altitude, int noise, int compactness)
            {
                Distance = distance;
                Altitude = altitude;
                Noise = noise;
                Compactness = compactness;
            }
            public int Distance { get; }
            public int Altitude { get; }
            public int Noise { get; }
            public int Compactness { get; }
        }

        private sealed class PatchWork
        {
            public PatchWork(
                BiomePatch patch,
                BiomePatchRuleDefinition rule,
                RuleSpecification specification,
                QuantizedWeights weights)
            {
                Patch = patch;
                Rule = rule;
                Specification = specification;
                Weights = weights;
                Sectors = new SortedSet<int>(patch.SectorIndices);
            }
            public BiomePatch Patch { get; }
            public BiomePatchRuleDefinition Rule { get; }
            public RuleSpecification Specification { get; }
            public QuantizedWeights Weights { get; }
            public SortedSet<int> Sectors { get; }
            public int Revision { get; set; }
        }

        private sealed class HeapEntry
        {
            public HeapEntry(PatchWork patch, int sectorIndex, int revision, BiomeGrowthCost cost)
            {
                Patch = patch;
                SectorIndex = sectorIndex;
                Revision = revision;
                Cost = cost;
            }
            public PatchWork Patch { get; }
            public int SectorIndex { get; }
            public int Revision { get; }
            public BiomeGrowthCost Cost { get; }
        }

        private sealed class StableMinHeap
        {
            private readonly List<HeapEntry> values = new List<HeapEntry>();
            public int Count => values.Count;

            public void Push(HeapEntry value)
            {
                values.Add(value);
                var index = values.Count - 1;
                while (index > 0)
                {
                    var parent = (index - 1) / 2;
                    if (Compare(values[parent], values[index]) <= 0) break;
                    Swap(parent, index);
                    index = parent;
                }
            }

            public HeapEntry Pop()
            {
                if (values.Count == 0) throw new InvalidOperationException("Heap is empty.");
                var result = values[0];
                var last = values[values.Count - 1];
                values.RemoveAt(values.Count - 1);
                if (values.Count == 0) return result;
                values[0] = last;
                var index = 0;
                while (true)
                {
                    var left = (index * 2) + 1;
                    if (left >= values.Count) break;
                    var right = left + 1;
                    var smallest = right < values.Count && Compare(values[right], values[left]) < 0
                        ? right : left;
                    if (Compare(values[index], values[smallest]) <= 0) break;
                    Swap(index, smallest);
                    index = smallest;
                }
                return result;
            }

            private void Swap(int left, int right)
            {
                var value = values[left];
                values[left] = values[right];
                values[right] = value;
            }

            private static int Compare(HeapEntry left, HeapEntry right)
            {
                var value = left.Cost.TotalCost2.CompareTo(right.Cost.TotalCost2);
                if (value != 0) return value;
                value = left.Patch.Patch.Id.CompareTo(right.Patch.Patch.Id);
                if (value != 0) return value;
                value = left.SectorIndex.CompareTo(right.SectorIndex);
                if (value != 0) return value;
                return left.Revision.CompareTo(right.Revision);
            }
        }
    }
}
