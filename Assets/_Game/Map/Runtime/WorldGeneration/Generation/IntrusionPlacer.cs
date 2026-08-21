using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class IntrusionPlacer
    {
        private const string RequiredGenerationProfileId = "GEN_MOONPALACE_V1";
        private const string RequiredWorldProfileId = "WORLD_MOONPALACE_V1";
        private const string TunnelProfileId = "BOUND_TUNNEL";
        private const int RequiredBiomeRetryMaximum = 100;
        private const int RequiredAssignedCount = 165;
        private const int RequiredUnassignedCount = 4;
        private const int PatchHardMaximum = 59;

        public IntrusionPlacementResult Place(
            MultiSeedBiomeGrowthResult growthResult,
            GenerationProfileDefinition generationProfile,
            IEnumerable<BiomeTypeDefinition> biomeTypes,
            IEnumerable<BiomePatchRuleDefinition> allPatchRules,
            IEnumerable<BiomeBoundaryProfileDefinition> boundaryProfiles,
            IEnumerable<BiomeBoundaryPairRuleDefinition> boundaryPairRules,
            DeterministicRngStream biomePatchRng)
        {
            try
            {
                var context = new ValidationContext();
                var errors = new List<IntrusionPlacementError>();
                ValidateGrowth(growthResult, context, errors);
                ValidateGenerationProfile(generationProfile, errors);
                ValidateBiomes(biomeTypes, context, errors);
                ValidatePatchRules(allPatchRules, context, errors);
                ValidateBoundaryProfiles(boundaryProfiles, context, errors);
                ValidateBoundaryPairs(boundaryPairRules, context, errors);
                ValidatePatchAndReservationState(context, errors);
                ValidateRng(biomePatchRng, context, errors);
                if (errors.Count != 0) return IntrusionPlacementResult.Invalid(errors);
                return Execute(context, biomePatchRng);
            }
            catch
            {
                return IntrusionPlacementResult.Invalid(new[]
                {
                    Error(
                        IntrusionPlacementErrorCode.InternalInvariantViolation,
                        string.Empty, string.Empty, string.Empty, -1, -1, 0, 0,
                        "Intrusion placement violated an internal model invariant.")
                });
            }
        }

        private static void ValidateGrowth(
            MultiSeedBiomeGrowthResult result,
            ValidationContext context,
            ICollection<IntrusionPlacementError> errors)
        {
            context.Result = result;
            if (result == null)
            {
                errors.Add(StructuralError(
                    IntrusionPlacementErrorCode.MissingGrowthResult,
                    "A multi-seed biome growth result is required."));
                return;
            }
            if (result.Status != MultiSeedBiomeGrowthStatus.Completed)
                errors.Add(StructuralError(
                    IntrusionPlacementErrorCode.GrowthNotCompleted,
                    "Multi-seed biome growth must be completed."));
            if (result.Publication == null)
                errors.Add(StructuralError(
                    IntrusionPlacementErrorCode.MissingGrowthPublication,
                    "A completed growth publication is required."));
            if (result.Diagnostics == null)
                errors.Add(StructuralError(
                    IntrusionPlacementErrorCode.MissingGrowthDiagnostics,
                    "Growth diagnostics are required."));
            if (result.Publication == null) return;

            context.Growth = result.Publication;
            context.Source = result.Publication.SourceSiteSnapshot;
            context.Input = result.Publication.Snapshot;
            if (context.Source == null)
                errors.Add(StructuralError(
                    IntrusionPlacementErrorCode.InvalidSourceSiteSnapshot,
                    "The source P01 reservation snapshot is required."));
            if (context.Input == null)
                errors.Add(StructuralError(
                    IntrusionPlacementErrorCode.InvalidGrowthPublication,
                    "The source P02 biome snapshot is required."));
            if (context.Source == null || context.Input == null || result.Diagnostics == null) return;

            var diagnostics = result.Diagnostics;
            if (!result.Succeeded || result.Errors == null || result.Errors.Count != 0 ||
                context.Growth.SourcePlacement == null ||
                context.Source.Seed != context.Input.Seed ||
                diagnostics.WorldSeed != context.Input.Seed ||
                diagnostics.FinalAssignedSectorCount != context.Input.AssignedSectorCount ||
                diagnostics.FinalUnassignedSectorCount != context.Input.UnassignedSectorCount ||
                diagnostics.PatchSectorCounts == null ||
                diagnostics.PatchSectorCounts.Count != context.Input.Patches.Count ||
                context.Growth.PatchCount != context.Input.Patches.Count ||
                context.Growth.FinalAssignedSectorCount != context.Input.AssignedSectorCount ||
                context.Growth.FinalUnassignedReservedSectorCount != context.Input.UnassignedSectorCount ||
                context.Input.IsComplete)
                errors.Add(StructuralError(
                    IntrusionPlacementErrorCode.InvalidGrowthPublication,
                    "Growth publication linkage or count conservation is invalid."));
        }

        private static void ValidateGenerationProfile(
            GenerationProfileDefinition profile,
            ICollection<IntrusionPlacementError> errors)
        {
            if (profile == null)
            {
                errors.Add(StructuralError(
                    IntrusionPlacementErrorCode.MissingGenerationProfile,
                    "The active Moon Palace generation profile is required."));
                return;
            }
            if (!string.Equals(profile.GenerationProfileId, RequiredGenerationProfileId, StringComparison.Ordinal) ||
                !string.Equals(profile.WorldProfileId, RequiredWorldProfileId, StringComparison.Ordinal) ||
                !profile.Active || profile.BiomeRetryMax != RequiredBiomeRetryMaximum)
                errors.Add(Error(
                    IntrusionPlacementErrorCode.InvalidGenerationProfile,
                    SafeId(profile.GenerationProfileId), string.Empty, string.Empty, -1, -1,
                    RequiredBiomeRetryMaximum, Math.Max(0, profile.BiomeRetryMax),
                    "Generation profile identity, activity, or biome retry limit is invalid."));
        }

        private static void ValidateBiomes(
            IEnumerable<BiomeTypeDefinition> source,
            ValidationContext context,
            ICollection<IntrusionPlacementError> errors)
        {
            if (source == null)
            {
                errors.Add(StructuralError(
                    IntrusionPlacementErrorCode.MissingBiomeTypes,
                    "Biome type definitions are required."));
                return;
            }
            foreach (var biome in source)
            {
                if (biome == null)
                {
                    errors.Add(StructuralError(
                        IntrusionPlacementErrorCode.NullDefinition,
                        "Definition collections cannot contain null."));
                    continue;
                }
                var id = SafeId(biome.BiomeId);
                if (id.Length == 0)
                {
                    errors.Add(StructuralError(
                        IntrusionPlacementErrorCode.InvalidBiomeDefinition,
                        "A biome definition has an invalid canonical ID."));
                    continue;
                }
                if (!context.Biomes.TryAdd(id, biome))
                    errors.Add(Error(
                        IntrusionPlacementErrorCode.DuplicateDefinitionId,
                        id, id, string.Empty, -1, -1, 1, 2,
                        "Biome definition IDs must be unique."));
            }

            var specifications = CreateBiomeSpecifications();
            var expected = new HashSet<string>(specifications.Select(value => value.BiomeId), StringComparer.Ordinal);
            foreach (var pair in context.Biomes)
                if (pair.Value.Active && pair.Value.Required && !expected.Contains(pair.Key))
                    errors.Add(Error(
                        IntrusionPlacementErrorCode.UnexpectedBiomeDefinition,
                        pair.Key, pair.Key, string.Empty, -1, -1, 0, 1,
                        "Only the exact four active required biomes are accepted."));
            foreach (var specification in specifications)
            {
                if (!context.Biomes.TryGetValue(specification.BiomeId, out var biome))
                    errors.Add(Error(
                        IntrusionPlacementErrorCode.MissingBiomeDefinition,
                        specification.BiomeId, specification.BiomeId, string.Empty, -1, -1, 1, 0,
                        "A required biome definition is missing."));
                else if (!specification.Matches(biome))
                    errors.Add(Error(
                        IntrusionPlacementErrorCode.InvalidBiomeDefinition,
                        specification.BiomeId, specification.BiomeId, string.Empty, -1, -1, 1, 0,
                        "A required biome definition does not match the frozen contract."));
            }
        }

        private static void ValidatePatchRules(
            IEnumerable<BiomePatchRuleDefinition> source,
            ValidationContext context,
            ICollection<IntrusionPlacementError> errors)
        {
            if (source == null)
            {
                errors.Add(StructuralError(
                    IntrusionPlacementErrorCode.MissingPatchRules,
                    "Biome patch rules are required."));
                return;
            }
            foreach (var rule in source)
            {
                if (rule == null)
                {
                    errors.Add(StructuralError(
                        IntrusionPlacementErrorCode.NullDefinition,
                        "Definition collections cannot contain null."));
                    continue;
                }
                var id = SafeId(rule.PatchRuleId);
                if (id.Length == 0)
                {
                    errors.Add(StructuralError(
                        IntrusionPlacementErrorCode.InvalidPatchRule,
                        "A patch rule has an invalid canonical ID."));
                    continue;
                }
                if (!context.Rules.TryAdd(id, rule))
                    errors.Add(Error(
                        IntrusionPlacementErrorCode.DuplicateDefinitionId,
                        id, SafeId(rule.BiomeId), string.Empty, -1, -1, 1, 2,
                        "Patch rule IDs must be unique."));
            }

            var specifications = CreateRuleSpecifications();
            var expected = new HashSet<string>(specifications.Select(value => value.PatchRuleId), StringComparer.Ordinal);
            foreach (var pair in context.Rules)
                if (pair.Value.Active && IsSupportedRole(pair.Value.PatchRole) && !expected.Contains(pair.Key))
                    errors.Add(Error(
                        IntrusionPlacementErrorCode.UnexpectedPatchRule,
                        pair.Key, SafeId(pair.Value.BiomeId), string.Empty, -1, -1, 0, 1,
                        "Only the exact ten active biome patch rules are accepted."));
            foreach (var specification in specifications)
            {
                if (!context.Rules.TryGetValue(specification.PatchRuleId, out var rule))
                {
                    errors.Add(Error(
                        IntrusionPlacementErrorCode.MissingPatchRule,
                        specification.PatchRuleId, specification.BiomeId, string.Empty, -1, -1, 1, 0,
                        "A required patch rule is missing."));
                    continue;
                }
                if (!specification.Matches(rule))
                    errors.Add(Error(
                        IntrusionPlacementErrorCode.InvalidPatchRule,
                        specification.PatchRuleId, specification.BiomeId, string.Empty, -1, -1, 1, 0,
                        "A required patch rule does not match the frozen contract."));
                if (!string.Equals(rule.BiomeId, specification.BiomeId, StringComparison.Ordinal))
                    errors.Add(Error(
                        IntrusionPlacementErrorCode.DefinitionIdentityMismatch,
                        specification.PatchRuleId, specification.BiomeId, string.Empty, -1, -1, 1, 0,
                        "Patch rule and biome identities must match exactly."));
                context.RuleSpecifications[specification.PatchRuleId] = specification;
            }
        }

        private static void ValidateBoundaryProfiles(
            IEnumerable<BiomeBoundaryProfileDefinition> source,
            ValidationContext context,
            ICollection<IntrusionPlacementError> errors)
        {
            if (source == null)
            {
                errors.Add(StructuralError(
                    IntrusionPlacementErrorCode.MissingBoundaryProfiles,
                    "Boundary profiles are required."));
                return;
            }
            foreach (var profile in source)
            {
                if (profile == null)
                {
                    errors.Add(StructuralError(
                        IntrusionPlacementErrorCode.NullDefinition,
                        "Definition collections cannot contain null."));
                    continue;
                }
                var id = SafeId(profile.BoundaryProfileId);
                if (id.Length == 0)
                {
                    errors.Add(StructuralError(
                        IntrusionPlacementErrorCode.InvalidBoundaryProfile,
                        "A boundary profile has an invalid canonical ID."));
                    continue;
                }
                if (!context.Profiles.TryAdd(id, profile))
                    errors.Add(Error(
                        IntrusionPlacementErrorCode.DuplicateDefinitionId,
                        id, string.Empty, string.Empty, -1, -1, 1, 2,
                        "Boundary profile IDs must be unique."));
            }

            var specifications = CreateProfileSpecifications();
            var expected = new HashSet<string>(specifications.Select(value => value.Id), StringComparer.Ordinal);
            foreach (var pair in context.Profiles)
                if (pair.Value.Active && !expected.Contains(pair.Key))
                    errors.Add(Error(
                        IntrusionPlacementErrorCode.UnexpectedBoundaryProfile,
                        pair.Key, string.Empty, string.Empty, -1, -1, 0, 1,
                        "Only the exact six active boundary profiles are accepted."));
            foreach (var specification in specifications)
            {
                if (!context.Profiles.TryGetValue(specification.Id, out var profile))
                    errors.Add(Error(
                        IntrusionPlacementErrorCode.MissingBoundaryProfile,
                        specification.Id, string.Empty, string.Empty, -1, -1, 1, 0,
                        "A required boundary profile is missing."));
                else if (!specification.Matches(profile))
                    errors.Add(Error(
                        IntrusionPlacementErrorCode.InvalidBoundaryProfile,
                        specification.Id, string.Empty, string.Empty, -1, -1, 1, 0,
                        "A required boundary profile does not match the frozen contract."));
            }
        }

        private static void ValidateBoundaryPairs(
            IEnumerable<BiomeBoundaryPairRuleDefinition> source,
            ValidationContext context,
            ICollection<IntrusionPlacementError> errors)
        {
            if (source == null)
            {
                errors.Add(StructuralError(
                    IntrusionPlacementErrorCode.MissingBoundaryPairRules,
                    "Boundary pair rules are required."));
                return;
            }
            foreach (var pair in source)
            {
                if (pair == null)
                {
                    errors.Add(StructuralError(
                        IntrusionPlacementErrorCode.NullDefinition,
                        "Definition collections cannot contain null."));
                    continue;
                }
                var id = SafeId(pair.BoundaryPairRuleId);
                if (id.Length == 0)
                {
                    errors.Add(StructuralError(
                        IntrusionPlacementErrorCode.InvalidBoundaryPairRule,
                        "A boundary pair rule has an invalid canonical ID."));
                    continue;
                }
                if (!context.Pairs.TryAdd(id, pair))
                    errors.Add(Error(
                        IntrusionPlacementErrorCode.DuplicateDefinitionId,
                        id, string.Empty, string.Empty, -1, -1, 1, 2,
                        "Boundary pair rule IDs must be unique."));
            }

            var specifications = CreatePairSpecifications();
            var expected = new HashSet<string>(specifications.Select(value => value.Id), StringComparer.Ordinal);
            foreach (var pair in context.Pairs)
                if (pair.Value.Active && !expected.Contains(pair.Key))
                    errors.Add(Error(
                        IntrusionPlacementErrorCode.UnexpectedBoundaryPairRule,
                        pair.Key, string.Empty, string.Empty, -1, -1, 0, 1,
                        "Only the exact six active boundary pair rules are accepted."));
            foreach (var specification in specifications)
            {
                if (!context.Pairs.TryGetValue(specification.Id, out var pair))
                {
                    errors.Add(Error(
                        IntrusionPlacementErrorCode.MissingBoundaryPairRule,
                        specification.Id, string.Empty, string.Empty, -1, -1, 1, 0,
                        "A required boundary pair rule is missing."));
                    continue;
                }
                if (!specification.Matches(pair))
                    errors.Add(Error(
                        IntrusionPlacementErrorCode.InvalidBoundaryPairRule,
                        specification.Id, string.Empty, string.Empty, -1, -1, 1, 0,
                        "A required boundary pair rule does not match the frozen contract."));
                if (!context.Biomes.ContainsKey(SafeId(pair.BiomeAId)) ||
                    !context.Biomes.ContainsKey(SafeId(pair.BiomeBId)))
                    errors.Add(Error(
                        IntrusionPlacementErrorCode.DefinitionIdentityMismatch,
                        specification.Id, string.Empty, string.Empty, -1, -1, 2, 0,
                        "Boundary pair biome identities must reference active definitions."));
            }

            foreach (var pair in context.Pairs.Values)
            {
                if (!pair.Active || !Contains(pair.AllowedBoundaryProfileIds, TunnelProfileId)) continue;
                AddRelation(context, "BIO_CASSIA_ROOT", pair);
                AddRelation(context, "BIO_ABANDONED_MILL", pair);
            }
        }

        private static void AddRelation(
            ValidationContext context,
            string intruderBiomeId,
            BiomeBoundaryPairRuleDefinition pair)
        {
            string host = null;
            if (string.Equals(pair.BiomeAId, intruderBiomeId, StringComparison.Ordinal)) host = pair.BiomeBId;
            else if (string.Equals(pair.BiomeBId, intruderBiomeId, StringComparison.Ordinal)) host = pair.BiomeAId;
            if (host == null || string.Equals(host, intruderBiomeId, StringComparison.Ordinal)) return;
            context.PairRelations[intruderBiomeId + "|" + host] = pair;
        }

        private static void ValidatePatchAndReservationState(
            ValidationContext context,
            ICollection<IntrusionPlacementError> errors)
        {
            if (context.Source == null || context.Input == null) return;
            if (context.Source.Sectors == null || context.Source.Sectors.Count != WorldGenConstants.SectorCount ||
                context.Source.Reservations == null || context.Source.CoreBiomeSeeds == null)
                errors.Add(StructuralError(
                    IntrusionPlacementErrorCode.InvalidSourceSiteSnapshot,
                    "Source P01 collections or sector count are invalid."));
            if (context.Input.Patches == null || context.Input.Sectors == null ||
                context.Input.SiteBindings == null ||
                context.Input.Sectors.Count != WorldGenConstants.SectorCount)
            {
                errors.Add(StructuralError(
                    IntrusionPlacementErrorCode.InvalidPatchState,
                    "Input P02 collections or sector count are invalid."));
                return;
            }
            if (context.Input.AssignedSectorCount != RequiredAssignedCount ||
                context.Input.UnassignedSectorCount != RequiredUnassignedCount)
                errors.Add(Error(
                    IntrusionPlacementErrorCode.InvalidPatchState,
                    string.Empty, string.Empty, string.Empty, -1, -1,
                    RequiredAssignedCount, Math.Max(0, context.Input.AssignedSectorCount),
                    "Input must preserve the exact 165 assigned and 4 reserved-unassigned sectors."));

            var coreCount = 0;
            var intrusionCount = 0;
            var patchIds = new HashSet<BiomePatchId>();
            foreach (var patch in context.Input.Patches)
            {
                if (patch == null || !patch.Id.IsValid || !patchIds.Add(patch.Id) ||
                    patch.Seeds == null || patch.Seeds.Count == 0 || patch.SectorIndices == null)
                {
                    errors.Add(StructuralError(
                        IntrusionPlacementErrorCode.InvalidPatchState,
                        "Input patches must be non-null with unique IDs, seeds, and sectors."));
                    continue;
                }
                if (patch.Role == BiomePatchRole.Core) coreCount++;
                else if (patch.Role == BiomePatchRole.Intrusion) intrusionCount++;
                else if (patch.Role != BiomePatchRole.Satellite)
                    errors.Add(Error(
                        IntrusionPlacementErrorCode.InvalidPatchState,
                        SafeId(patch.PatchRuleId), SafeId(patch.BiomeId), string.Empty,
                        -1, -1, 1, 0, "Patch role is undefined."));

                if (!context.Rules.TryGetValue(SafeId(patch.PatchRuleId), out var rule) ||
                    !context.RuleSpecifications.TryGetValue(SafeId(patch.PatchRuleId), out var specification) ||
                    !string.Equals(patch.BiomeId, specification.BiomeId, StringComparison.Ordinal) ||
                    patch.Role != specification.Role || patch.SectorCount < rule.MinSectorCount ||
                    patch.SectorCount > Math.Min(rule.MaxSectorCount, PatchHardMaximum) ||
                    !IsConnected(patch.SectorIndices, -1))
                    errors.Add(Error(
                        IntrusionPlacementErrorCode.InvalidPatchState,
                        SafeId(patch.PatchRuleId), SafeId(patch.BiomeId), string.Empty,
                        -1, -1, Math.Max(0, rule == null ? 0 : rule.MinSectorCount),
                        Math.Max(0, patch.SectorCount),
                        "Patch role, rule, biome, size, or cardinal connectivity is invalid."));

                foreach (var seed in patch.Seeds)
                {
                    if (seed == null || seed.Role != patch.Role || !patch.ContainsSector(seed.SectorIndex) ||
                        (patch.Role == BiomePatchRole.Core) != seed.SourceSiteReservationId.HasValue)
                        errors.Add(Error(
                            IntrusionPlacementErrorCode.InvalidPatchState,
                            SafeId(patch.PatchRuleId), SafeId(patch.BiomeId), string.Empty,
                            seed == null ? -1 : seed.SectorIndex, -1, 1, 0,
                            "Patch seed linkage is invalid."));
                    else context.ProtectedSeedSectors.Add(seed.SectorIndex);
                }
                context.InputPatches[patch.Id] = patch;
            }
            if (coreCount != 4 || intrusionCount != 0)
                errors.Add(Error(
                    IntrusionPlacementErrorCode.InvalidPatchState,
                    string.Empty, string.Empty, string.Empty, -1, -1, 4, coreCount,
                    "Input must contain exact four Core patches and zero Intrusion patches."));
            if (context.Input.SiteBindings.Count != 4)
                errors.Add(Error(
                    IntrusionPlacementErrorCode.InvalidPatchState,
                    string.Empty, string.Empty, string.Empty, -1, -1, 4,
                    context.Input.SiteBindings.Count,
                    "Input must preserve exact four Core site bindings."));

            foreach (var binding in context.Input.SiteBindings)
            {
                if (binding == null || !context.Input.TryGetPatch(binding.PatchId, out var patch) ||
                    patch.Role != BiomePatchRole.Core ||
                    !string.Equals(binding.BiomeId, patch.BiomeId, StringComparison.Ordinal))
                {
                    errors.Add(StructuralError(
                        IntrusionPlacementErrorCode.InvalidPatchState,
                        "Core site bindings must match Core patches."));
                    continue;
                }
                foreach (var index in binding.OccupiedSectorIndices)
                    context.ProtectedBindingSectors.Add(index);
            }

            var unassignedReserved = 0;
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var reservation = context.Source.GetSector(index);
                var ownership = context.Input.GetSector(index);
                var coordinate = WorldGridIndex.ToCoordinate(index);
                if (reservation == null || reservation.Index != index || reservation.Coordinate != coordinate)
                {
                    errors.Add(Error(
                        IntrusionPlacementErrorCode.InvalidReservationState,
                        string.Empty, string.Empty, string.Empty, -1, index, 1, 0,
                        "A P01 row has invalid grid identity."));
                    continue;
                }
                if (ownership == null || ownership.SectorIndex != index || ownership.Sector != coordinate ||
                    ownership.SecondaryBiomeId == null || ownership.SecondaryBiomeId.Length != 0)
                {
                    errors.Add(Error(
                        IntrusionPlacementErrorCode.InvalidPatchState,
                        string.Empty, string.Empty, string.Empty, -1, index, 1, 0,
                        "A P02 row has invalid identity or SecondaryBiome."));
                    continue;
                }
                if (ownership.IsAssigned)
                {
                    if (!ownership.PatchId.HasValue ||
                        !context.InputPatches.TryGetValue(ownership.PatchId.Value, out var patch) ||
                        !patch.ContainsSector(index) ||
                        !string.Equals(ownership.PrimaryBiomeId, patch.BiomeId, StringComparison.Ordinal))
                        errors.Add(Error(
                            IntrusionPlacementErrorCode.InvalidPatchState,
                            string.Empty, SafeId(ownership.PrimaryBiomeId), string.Empty,
                            -1, index, 1, 0,
                            "Assigned ownership must match one patch."));
                }
                else
                {
                    if (!reservation.IsReserved || !reservation.Kind.HasValue ||
                        reservation.Kind.Value == SiteReservationKind.CoreResource ||
                        reservation.Kind.Value == SiteReservationKind.Forge)
                        errors.Add(Error(
                            IntrusionPlacementErrorCode.InvalidReservationState,
                            string.Empty, string.Empty, string.Empty, -1, index, 1, 0,
                            "Every unassigned row must be a non-Core P01 reserved footprint."));
                    else unassignedReserved++;
                }
                if (!reservation.IsReserved && !ownership.IsAssigned)
                    errors.Add(Error(
                        IntrusionPlacementErrorCode.InvalidReservationState,
                        string.Empty, string.Empty, string.Empty, -1, index, 1, 0,
                        "Every unreserved P01 row must be assigned in grown P02."));
            }
            if (unassignedReserved != RequiredUnassignedCount)
                errors.Add(Error(
                    IntrusionPlacementErrorCode.InvalidReservationState,
                    string.Empty, string.Empty, string.Empty, -1, -1,
                    RequiredUnassignedCount, unassignedReserved,
                    "The grown snapshot must retain exact four non-Core reserved-unassigned rows."));
        }

        private static void ValidateRng(
            DeterministicRngStream rng,
            ValidationContext context,
            ICollection<IntrusionPlacementError> errors)
        {
            if (rng == null)
            {
                errors.Add(StructuralError(
                    IntrusionPlacementErrorCode.MissingBiomePatchRng,
                    "The continued RNG_BIOME_PATCH stream is required."));
                return;
            }
            if (context.Result == null || context.Result.Diagnostics == null) return;
            var expected = context.Result.Diagnostics.RngDrawCountAfter;
            if (rng.DrawCount != expected)
                errors.Add(Error(
                    IntrusionPlacementErrorCode.InvalidBiomePatchRngState,
                    WorldGenerationRngStreams.BiomePatchStreamId,
                    string.Empty, string.Empty, -1, -1,
                    ClampCount(expected), ClampCount(rng.DrawCount),
                    "Biome patch RNG DrawCount must continue the successful growth attempt."));
        }

        private static IntrusionPlacementResult Execute(
            ValidationContext context,
            DeterministicRngStream rng)
        {
            var before = rng.DrawCount;
            var orderedRules = new[]
            {
                context.Rules["PATCH_MILL_INTRUSION"],
                context.Rules["PATCH_ROOT_INTRUSION"]
            };
            var desired = new int[orderedRules.Length];
            for (var index = 0; index < orderedRules.Length; index++)
                desired[index] = rng.NextInt(
                    orderedRules[index].SeedCountMin,
                    checked(orderedRules[index].SeedCountMax + 1));

            var works = new Dictionary<BiomePatchId, WorkingPatch>();
            var owners = new WorkingPatch[WorldGenConstants.SectorCount];
            foreach (var patch in context.Input.Patches)
            {
                var work = new WorkingPatch(patch);
                works.Add(work.Id, work);
                foreach (var sector in work.Sectors) owners[sector] = work;
            }

            var records = new List<IntrusionPlacementRecord>();
            var states = new RuleState[orderedRules.Length];
            for (var index = 0; index < states.Length; index++)
                states[index] = new RuleState(orderedRules[index], desired[index]);
            var idFactory = new IntrusionPatchIdFactory();

            for (var ruleIndex = 0; ruleIndex < orderedRules.Length; ruleIndex++)
            {
                var rule = orderedRules[ruleIndex];
                var state = states[ruleIndex];
                for (var ordinal = 0; ordinal < desired[ruleIndex]; ordinal++)
                {
                    state.Attempted++;
                    var candidates = EnumerateCandidates(context, rule, ordinal, owners, works, records);
                    state.LastCandidateCount = candidates.Count;
                    if (candidates.Count == 0)
                    {
                        state.Exhausted = true;
                        state.FailedOrdinal = ordinal;
                        return Retry(context, rng, before, states, rule, ordinal);
                    }

                    var roll = rng.NextInt(candidates.Count);
                    state.CandidateCalls++;
                    var candidate = candidates[roll];
                    var donor = candidate.Donor;
                    var donorBefore = donor.Sectors.Count;
                    donor.Sectors.Remove(candidate.SectorIndex);
                    var patchId = idFactory.Create(rule.BiomeId, ordinal);
                    if (works.ContainsKey(patchId))
                        throw new InvalidOperationException("Intrusion patch ID collision.");
                    var intrusion = WorkingPatch.CreateIntrusion(
                        patchId, rule.BiomeId, rule.PatchRuleId, candidate.SectorIndex);
                    works.Add(patchId, intrusion);
                    owners[candidate.SectorIndex] = intrusion;

                    var record = new IntrusionPlacementRecord(
                        records.Count, rule.PatchRuleId, rule.BiomeId, ordinal, patchId,
                        candidate.SectorIndex, WorldGridIndex.ToCoordinate(candidate.SectorIndex),
                        donor.BiomeId, donor.Id, donor.Role, donorBefore, donorBefore - 1,
                        candidate.Pair.BoundaryPairRuleId, TunnelProfileId,
                        candidate.SharedEdgeCount, candidate.AnchorSectorIndex,
                        candidates.Count, roll, candidate.NearestDistance);
                    records.Add(record);
                    state.Accepted++;
                }
            }

            var patches = new List<BiomePatch>();
            foreach (var work in works.Values) patches.Add(work.Build());
            var ownership = new List<BiomeSectorOwnership>(WorldGenConstants.SectorCount);
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                if (owners[index] == null)
                    ownership.Add(BiomeSectorOwnership.CreateUnassigned(index, WorldGridIndex.ToCoordinate(index)));
                else if (owners[index].Original != null &&
                         ReferenceEquals(owners[index], works[owners[index].Id]) &&
                         owners[index].Original.ContainsSector(index))
                {
                    var old = context.Input.GetSector(index);
                    if (old.PatchId.HasValue && old.PatchId.Value == owners[index].Id)
                        ownership.Add(old);
                    else ownership.Add(new BiomeSectorOwnership(
                        index, WorldGridIndex.ToCoordinate(index), owners[index].BiomeId,
                        string.Empty, owners[index].Id));
                }
                else ownership.Add(new BiomeSectorOwnership(
                    index, WorldGridIndex.ToCoordinate(index), owners[index].BiomeId,
                    string.Empty, owners[index].Id));
            }

            var snapshot = new BiomePatchSnapshot(
                context.Input.Seed, patches, ownership, context.Input.SiteBindings);
            var publication = new IntrusionPlacementPublication(context.Growth, snapshot, records);
            var diagnostics = CreateDiagnostics(
                context, states, records, records.Count, snapshot.Patches.Count,
                snapshot.AssignedSectorCount, snapshot.UnassignedSectorCount,
                before, rng.DrawCount, false);
            return IntrusionPlacementResult.Completed(publication, diagnostics);
        }

        private static IntrusionPlacementResult Retry(
            ValidationContext context,
            DeterministicRngStream rng,
            ulong before,
            IReadOnlyList<RuleState> states,
            BiomePatchRuleDefinition rule,
            int ordinal)
        {
            var diagnostics = CreateDiagnostics(
                context, states, Array.Empty<IntrusionPlacementRecord>(), 0,
                context.Input.Patches.Count, context.Input.AssignedSectorCount,
                context.Input.UnassignedSectorCount, before, rng.DrawCount, true);
            return IntrusionPlacementResult.Retry(diagnostics, new[]
            {
                Error(
                    IntrusionPlacementErrorCode.NoLegalIntrusionCandidate,
                    rule.PatchRuleId, rule.BiomeId, string.Empty, ordinal, -1,
                    1, 0, "No legal one-cell Intrusion candidate remains.")
            });
        }

        private static List<Candidate> EnumerateCandidates(
            ValidationContext context,
            BiomePatchRuleDefinition intrusionRule,
            int intrusionOrdinal,
            WorkingPatch[] owners,
            IReadOnlyDictionary<BiomePatchId, WorkingPatch> works,
            IReadOnlyList<IntrusionPlacementRecord> records)
        {
            var candidates = new List<Candidate>();
            var normalShareCap = GetNormalBiomeShareCap(context, intrusionRule.BiomeId);
            var intrusionShareCap = GetShareCapacity(intrusionRule.MaxWorldShare);
            var currentBiomeCount = 0;
            var currentIntrusionCount = 0;
            for (var index = 0; index < owners.Length; index++)
            {
                if (owners[index] == null) continue;
                if (string.Equals(owners[index].BiomeId, intrusionRule.BiomeId, StringComparison.Ordinal))
                    currentBiomeCount++;
                if (owners[index].Role == BiomePatchRole.Intrusion &&
                    string.Equals(owners[index].BiomeId, intrusionRule.BiomeId, StringComparison.Ordinal))
                    currentIntrusionCount++;
            }

            for (var sectorIndex = 0; sectorIndex < WorldGenConstants.SectorCount; sectorIndex++)
            {
                var donor = owners[sectorIndex];
                if (donor == null ||
                    (donor.Role != BiomePatchRole.Core && donor.Role != BiomePatchRole.Satellite) ||
                    string.Equals(donor.BiomeId, intrusionRule.BiomeId, StringComparison.Ordinal))
                    continue;
                if (context.Source.GetSector(sectorIndex).IsReserved) continue;
                var coordinate = WorldGridIndex.ToCoordinate(sectorIndex);
                if (IsWorldEdge(coordinate)) continue;
                if (context.ProtectedSeedSectors.Contains(sectorIndex) ||
                    context.ProtectedBindingSectors.Contains(sectorIndex)) continue;
                if (!context.Rules.TryGetValue(donor.PatchRuleId, out var donorRule) ||
                    donor.Sectors.Count - 1 < donorRule.MinSectorCount) continue;
                if (!IsConnected(donor.Sectors, sectorIndex)) continue;
                if (!context.PairRelations.TryGetValue(
                    intrusionRule.BiomeId + "|" + donor.BiomeId, out var pair)) continue;

                var shared = 0;
                var anchor = int.MaxValue;
                foreach (var neighbor in GetNeighbors(sectorIndex))
                {
                    var owner = owners[neighbor];
                    if (owner == null ||
                        (owner.Role != BiomePatchRole.Core && owner.Role != BiomePatchRole.Satellite) ||
                        !string.Equals(owner.BiomeId, intrusionRule.BiomeId, StringComparison.Ordinal))
                        continue;
                    shared++;
                    if (neighbor < anchor) anchor = neighbor;
                }
                if (shared < pair.MinSharedEdgeCount) continue;

                var nearest = -1;
                foreach (var record in records)
                {
                    if (!string.Equals(record.IntrusionRuleId, intrusionRule.PatchRuleId, StringComparison.Ordinal))
                        continue;
                    var distance = Manhattan(
                        WorldGridIndex.ToCoordinate(record.SectorIndex), coordinate);
                    if (nearest < 0 || distance < nearest) nearest = distance;
                }
                if (nearest >= 0 && nearest < intrusionRule.MinSeedDistance) continue;
                if (currentBiomeCount + 1 > normalShareCap ||
                    currentIntrusionCount + 1 > intrusionShareCap) continue;

                candidates.Add(new Candidate(
                    sectorIndex, donor, pair, shared, anchor, nearest));
            }
            candidates.Sort((left, right) => left.SectorIndex.CompareTo(right.SectorIndex));
            return candidates;
        }

        private static IntrusionPlacementDiagnostics CreateDiagnostics(
            ValidationContext context,
            IReadOnlyList<RuleState> states,
            IEnumerable<IntrusionPlacementRecord> records,
            int placed,
            int finalPatchCount,
            int finalAssigned,
            int finalUnassigned,
            ulong rngBefore,
            ulong rngAfter,
            bool rollback)
        {
            var ruleDiagnostics = new List<IntrusionRulePlacementDiagnostics>();
            var candidateCalls = 0;
            var desired = 0;
            foreach (var state in states)
            {
                desired += state.Desired;
                candidateCalls += state.CandidateCalls;
                ruleDiagnostics.Add(new IntrusionRulePlacementDiagnostics(
                    state.Rule.PatchRuleId, state.Rule.BiomeId, state.Desired,
                    state.Desired, state.Attempted, state.Accepted,
                    state.CandidateCalls, state.LastCandidateCount,
                    state.Exhausted, state.FailedOrdinal));
            }
            return new IntrusionPlacementDiagnostics(
                context.Input.Seed, ruleDiagnostics, records,
                context.Input.Patches.Count, context.Input.AssignedSectorCount,
                context.Input.UnassignedSectorCount, desired, placed,
                finalPatchCount, finalAssigned, finalUnassigned,
                states.Count, candidateCalls, rngBefore, rngAfter,
                0, 0, 0, 0, 0, 0, rollback);
        }

        private static int GetNormalBiomeShareCap(ValidationContext context, string biomeId)
        {
            var result = int.MaxValue;
            foreach (var rule in context.Rules.Values)
            {
                if (!rule.Active || !string.Equals(rule.BiomeId, biomeId, StringComparison.Ordinal) ||
                    string.Equals(rule.PatchRole, "INTRUSION", StringComparison.Ordinal)) continue;
                result = Math.Min(result, GetShareCapacity(rule.MaxWorldShare));
            }
            return result == int.MaxValue ? 0 : result;
        }

        private static int GetShareCapacity(float share)
        {
            if (float.IsNaN(share) || float.IsInfinity(share) || share < 0f) return -1;
            return Math.Min(PatchHardMaximum,
                (int)Math.Floor((WorldGenConstants.SectorCount * (double)share) + 0.000001d));
        }

        private static bool IsConnected(IEnumerable<int> source, int removed)
        {
            var values = new HashSet<int>(source);
            if (removed >= 0) values.Remove(removed);
            if (values.Count == 0) return false;
            var start = values.Min();
            var visited = new HashSet<int> { start };
            var queue = new Queue<int>();
            queue.Enqueue(start);
            while (queue.Count != 0)
            {
                var current = queue.Dequeue();
                foreach (var neighbor in GetNeighbors(current))
                    if (values.Contains(neighbor) && visited.Add(neighbor)) queue.Enqueue(neighbor);
            }
            return visited.Count == values.Count;
        }

        private static IEnumerable<int> GetNeighbors(int sectorIndex)
        {
            var left = WorldGridIndex.GetLeftIndex(sectorIndex);
            if (left >= 0) yield return left;
            var right = WorldGridIndex.GetRightIndex(sectorIndex);
            if (right >= 0) yield return right;
            var up = WorldGridIndex.GetUpIndex(sectorIndex);
            if (up >= 0) yield return up;
            var down = WorldGridIndex.GetDownIndex(sectorIndex);
            if (down >= 0) yield return down;
        }

        private static bool IsWorldEdge(SectorCoord coordinate)
        {
            return coordinate.X == 0 || coordinate.X == WorldGenConstants.SectorColumns - 1 ||
                   coordinate.Y == 0 || coordinate.Y == WorldGenConstants.SectorRows - 1;
        }

        private static int Manhattan(SectorCoord left, SectorCoord right)
        {
            return Math.Abs(left.X - right.X) + Math.Abs(left.Y - right.Y);
        }

        private static bool Contains(IReadOnlyList<string> values, string value)
        {
            if (values == null) return false;
            for (var index = 0; index < values.Count; index++)
                if (string.Equals(values[index], value, StringComparison.Ordinal)) return true;
            return false;
        }

        private static bool SequenceEqual<T>(IReadOnlyList<T> left, IReadOnlyList<T> right)
        {
            if (left == null || right == null || left.Count != right.Count) return false;
            var comparer = EqualityComparer<T>.Default;
            for (var index = 0; index < left.Count; index++)
                if (!comparer.Equals(left[index], right[index])) return false;
            return true;
        }

        private static bool IsSupportedRole(string role)
        {
            return string.Equals(role, "CORE", StringComparison.Ordinal) ||
                   string.Equals(role, "SATELLITE", StringComparison.Ordinal) ||
                   string.Equals(role, "INTRUSION", StringComparison.Ordinal);
        }

        private static bool SameFloat(float left, float right)
        {
            return !float.IsNaN(left) && !float.IsInfinity(left) &&
                   Math.Abs((double)left - right) <= 0.000001d;
        }

        private static int ClampCount(ulong value)
        {
            return value > int.MaxValue ? int.MaxValue : (int)value;
        }

        private static string SafeId(string value)
        {
            return ReservationValidation.IsCanonicalId(value, true) ? value : string.Empty;
        }

        private static IntrusionPlacementError StructuralError(
            IntrusionPlacementErrorCode code,
            string message)
        {
            return Error(code, string.Empty, string.Empty, string.Empty, -1, -1, 0, 0, message);
        }

        private static IntrusionPlacementError Error(
            IntrusionPlacementErrorCode code,
            string definitionId,
            string intruderBiomeId,
            string hostBiomeId,
            int intrusionOrdinal,
            int sectorIndex,
            int requiredCount,
            int availableCount,
            string message)
        {
            return new IntrusionPlacementError(
                code, definitionId, intruderBiomeId, hostBiomeId,
                intrusionOrdinal, sectorIndex, requiredCount, availableCount, message);
        }

        private static IReadOnlyList<BiomeSpecification> CreateBiomeSpecifications()
        {
            return new[]
            {
                new BiomeSpecification("BIO_ABANDONED_MILL", 1, 11),
                new BiomeSpecification("BIO_CASSIA_ROOT", 2, 12),
                new BiomeSpecification("BIO_MOON_CRATER", 0, 7),
                new BiomeSpecification("BIO_MOON_DOUGH", 0, 7)
            };
        }

        private static IReadOnlyList<RuleSpecification> CreateRuleSpecifications()
        {
            return new[]
            {
                new RuleSpecification("PATCH_CRATER_CORE", "BIO_MOON_CRATER", BiomePatchRole.Core, 5, 18, 4, 1, 1, 100f, true, 1, false, .35f, 1f, .25f, .45f, .75f, .45f),
                new RuleSpecification("PATCH_CRATER_SAT", "BIO_MOON_CRATER", BiomePatchRole.Satellite, 2, 16, 3, 0, 3, 70f, true, 0, false, .35f, 1f, .25f, .6f, .65f, .55f),
                new RuleSpecification("PATCH_ROOT_CORE", "BIO_CASSIA_ROOT", BiomePatchRole.Core, 5, 18, 4, 1, 1, 100f, false, 1, false, .35f, 1f, .35f, .45f, .7f, .55f),
                new RuleSpecification("PATCH_ROOT_SAT", "BIO_CASSIA_ROOT", BiomePatchRole.Satellite, 2, 14, 3, 0, 3, 70f, false, 0, false, .35f, 1f, .35f, .6f, .6f, .65f),
                new RuleSpecification("PATCH_MILL_CORE", "BIO_ABANDONED_MILL", BiomePatchRole.Core, 4, 14, 4, 1, 1, 100f, false, 1, false, .35f, 1f, .2f, .35f, .85f, .3f),
                new RuleSpecification("PATCH_MILL_SAT", "BIO_ABANDONED_MILL", BiomePatchRole.Satellite, 2, 10, 3, 0, 2, 45f, false, 0, false, .35f, 1f, .2f, .5f, .8f, .35f),
                new RuleSpecification("PATCH_DOUGH_CORE", "BIO_MOON_DOUGH", BiomePatchRole.Core, 5, 18, 4, 1, 1, 100f, true, 1, false, .35f, 1f, .4f, .45f, .7f, .5f),
                new RuleSpecification("PATCH_DOUGH_SAT", "BIO_MOON_DOUGH", BiomePatchRole.Satellite, 2, 14, 3, 0, 3, 70f, true, 0, false, .35f, 1f, .4f, .6f, .65f, .6f),
                new RuleSpecification("PATCH_MILL_INTRUSION", "BIO_ABANDONED_MILL", BiomePatchRole.Intrusion, 1, 4, 2, 0, 2, 15f, false, 0, true, .1f, 1f, .1f, .8f, .25f, .85f),
                new RuleSpecification("PATCH_ROOT_INTRUSION", "BIO_CASSIA_ROOT", BiomePatchRole.Intrusion, 1, 5, 2, 0, 2, 20f, false, 0, true, .1f, 1f, .3f, .8f, .2f, .9f)
            };
        }

        private static IReadOnlyList<ProfileSpecification> CreateProfileSpecifications()
        {
            return new[]
            {
                new ProfileSpecification("BOUND_SOFT_BLEND", "SOFT_BLEND", new[] { "HORIZONTAL", "VERTICAL" }, 1, 2, 2, true, "NONE", false),
                new ProfileSpecification("BOUND_CLIFF", "CLIFF", new[] { "HORIZONTAL", "VERTICAL" }, 1, 2, 2, true, "NONE", false),
                new ProfileSpecification("BOUND_TUNNEL", "TUNNEL_INTRUSION", new[] { "HORIZONTAL", "VERTICAL" }, 1, 3, 2, true, "NONE", false),
                new ProfileSpecification("BOUND_LAYER", "LAYER", new[] { "VERTICAL" }, 1, 2, 2, true, "NONE", false),
                new ProfileSpecification("BOUND_RUIN", "RUIN", new[] { "HORIZONTAL", "VERTICAL" }, 1, 3, 2, true, "NONE", false),
                new ProfileSpecification("BOUND_HARD_STARSTONE", "HARD_STARSTONE", new[] { "HORIZONTAL", "VERTICAL" }, 1, 1, 1, false, "NONE", true)
            };
        }

        private static IReadOnlyList<PairSpecification> CreatePairSpecifications()
        {
            return new[]
            {
                new PairSpecification("PAIR_CRATER_ROOT", "BIO_MOON_CRATER", "BIO_CASSIA_ROOT", new[] { "BOUND_SOFT_BLEND", "BOUND_CLIFF", "BOUND_TUNNEL" }, new[] { 50, 25, 25 }, "BOUND_SOFT_BLEND"),
                new PairSpecification("PAIR_CRATER_MILL", "BIO_MOON_CRATER", "BIO_ABANDONED_MILL", new[] { "BOUND_RUIN", "BOUND_SOFT_BLEND" }, new[] { 70, 30 }, "BOUND_RUIN"),
                new PairSpecification("PAIR_CRATER_DOUGH", "BIO_MOON_CRATER", "BIO_MOON_DOUGH", new[] { "BOUND_CLIFF", "BOUND_LAYER", "BOUND_SOFT_BLEND" }, new[] { 45, 35, 20 }, "BOUND_CLIFF"),
                new PairSpecification("PAIR_ROOT_MILL", "BIO_CASSIA_ROOT", "BIO_ABANDONED_MILL", new[] { "BOUND_RUIN", "BOUND_TUNNEL", "BOUND_SOFT_BLEND" }, new[] { 45, 35, 20 }, "BOUND_RUIN"),
                new PairSpecification("PAIR_ROOT_DOUGH", "BIO_CASSIA_ROOT", "BIO_MOON_DOUGH", new[] { "BOUND_TUNNEL", "BOUND_LAYER", "BOUND_SOFT_BLEND" }, new[] { 45, 30, 25 }, "BOUND_TUNNEL"),
                new PairSpecification("PAIR_MILL_DOUGH", "BIO_ABANDONED_MILL", "BIO_MOON_DOUGH", new[] { "BOUND_RUIN", "BOUND_LAYER", "BOUND_TUNNEL" }, new[] { 45, 30, 25 }, "BOUND_RUIN")
            };
        }

        private sealed class ValidationContext
        {
            public MultiSeedBiomeGrowthResult Result;
            public MultiSeedBiomeGrowthPublication Growth;
            public SiteReservationSnapshot Source;
            public BiomePatchSnapshot Input;
            public readonly Dictionary<string, BiomeTypeDefinition> Biomes = new Dictionary<string, BiomeTypeDefinition>(StringComparer.Ordinal);
            public readonly Dictionary<string, BiomePatchRuleDefinition> Rules = new Dictionary<string, BiomePatchRuleDefinition>(StringComparer.Ordinal);
            public readonly Dictionary<string, RuleSpecification> RuleSpecifications = new Dictionary<string, RuleSpecification>(StringComparer.Ordinal);
            public readonly Dictionary<string, BiomeBoundaryProfileDefinition> Profiles = new Dictionary<string, BiomeBoundaryProfileDefinition>(StringComparer.Ordinal);
            public readonly Dictionary<string, BiomeBoundaryPairRuleDefinition> Pairs = new Dictionary<string, BiomeBoundaryPairRuleDefinition>(StringComparer.Ordinal);
            public readonly Dictionary<string, BiomeBoundaryPairRuleDefinition> PairRelations = new Dictionary<string, BiomeBoundaryPairRuleDefinition>(StringComparer.Ordinal);
            public readonly Dictionary<BiomePatchId, BiomePatch> InputPatches = new Dictionary<BiomePatchId, BiomePatch>();
            public readonly HashSet<int> ProtectedSeedSectors = new HashSet<int>();
            public readonly HashSet<int> ProtectedBindingSectors = new HashSet<int>();
        }

        private sealed class WorkingPatch
        {
            private WorkingPatch(
                BiomePatchId id, string biomeId, string patchRuleId, BiomePatchRole role,
                IReadOnlyList<BiomePatchSeed> seeds, IEnumerable<int> sectors, BiomePatch original)
            {
                Id = id;
                BiomeId = biomeId;
                PatchRuleId = patchRuleId;
                Role = role;
                Seeds = seeds;
                Sectors = new HashSet<int>(sectors);
                Original = original;
            }

            public WorkingPatch(BiomePatch source)
                : this(source.Id, source.BiomeId, source.PatchRuleId, source.Role,
                    source.Seeds, source.SectorIndices, source)
            {
            }

            public BiomePatchId Id { get; }
            public string BiomeId { get; }
            public string PatchRuleId { get; }
            public BiomePatchRole Role { get; }
            public IReadOnlyList<BiomePatchSeed> Seeds { get; }
            public HashSet<int> Sectors { get; }
            public BiomePatch Original { get; }

            public static WorkingPatch CreateIntrusion(
                BiomePatchId id, string biomeId, string ruleId, int sectorIndex)
            {
                var seed = new BiomePatchSeed(
                    sectorIndex, WorldGridIndex.ToCoordinate(sectorIndex),
                    BiomePatchRole.Intrusion, null);
                return new WorkingPatch(
                    id, biomeId, ruleId, BiomePatchRole.Intrusion,
                    new[] { seed }, new[] { sectorIndex }, null);
            }

            public BiomePatch Build()
            {
                if (Original != null && Sectors.SetEquals(Original.SectorIndices)) return Original;
                return new BiomePatch(Id, BiomeId, PatchRuleId, Role, Seeds, Sectors);
            }
        }

        private sealed class Candidate
        {
            public Candidate(
                int sectorIndex, WorkingPatch donor,
                BiomeBoundaryPairRuleDefinition pair, int sharedEdgeCount,
                int anchorSectorIndex, int nearestDistance)
            {
                SectorIndex = sectorIndex;
                Donor = donor;
                Pair = pair;
                SharedEdgeCount = sharedEdgeCount;
                AnchorSectorIndex = anchorSectorIndex;
                NearestDistance = nearestDistance;
            }
            public int SectorIndex { get; }
            public WorkingPatch Donor { get; }
            public BiomeBoundaryPairRuleDefinition Pair { get; }
            public int SharedEdgeCount { get; }
            public int AnchorSectorIndex { get; }
            public int NearestDistance { get; }
        }

        private sealed class RuleState
        {
            public RuleState(BiomePatchRuleDefinition rule, int desired)
            {
                Rule = rule;
                Desired = desired;
                FailedOrdinal = -1;
            }
            public BiomePatchRuleDefinition Rule { get; }
            public int Desired { get; }
            public int Attempted;
            public int Accepted;
            public int CandidateCalls;
            public int LastCandidateCount;
            public bool Exhausted;
            public int FailedOrdinal;
        }

        private sealed class BiomeSpecification
        {
            public BiomeSpecification(string biomeId, int altitudeMinimum, int altitudeMaximum)
            {
                BiomeId = biomeId;
                AltitudeMinimum = altitudeMinimum;
                AltitudeMaximum = altitudeMaximum;
            }
            public string BiomeId { get; }
            private int AltitudeMinimum { get; }
            private int AltitudeMaximum { get; }
            public bool Matches(BiomeTypeDefinition value)
            {
                return value != null && value.Active && value.Required &&
                       string.Equals(value.BiomeId, BiomeId, StringComparison.Ordinal) &&
                       value.MinPatchCount == 1 && value.MaxPatchCount == 4 &&
                       value.MinCorePatchCount == 1 &&
                       value.PreferredAltitudeMinSectorY == AltitudeMinimum &&
                       value.PreferredAltitudeMaxSectorY == AltitudeMaximum &&
                       SameFloat(value.GrowthWeight,
                           string.Equals(BiomeId, "BIO_ABANDONED_MILL", StringComparison.Ordinal) ? .9f : 1f);
            }
        }

        private sealed class RuleSpecification
        {
            public RuleSpecification(
                string patchRuleId, string biomeId, BiomePatchRole role,
                int minimum, int maximum, int distance, int countMinimum, int countMaximum,
                float seedWeight, bool edge, int buffer, bool single, float share,
                float distanceWeight, float altitudeWeight, float noiseWeight,
                float compactnessWeight, float branchiness)
            {
                PatchRuleId = patchRuleId; BiomeId = biomeId; Role = role;
                Minimum = minimum; Maximum = maximum; Distance = distance;
                CountMinimum = countMinimum; CountMaximum = countMaximum;
                SeedWeight = seedWeight; Edge = edge; Buffer = buffer; Single = single;
                Share = share; DistanceWeight = distanceWeight; AltitudeWeight = altitudeWeight;
                NoiseWeight = noiseWeight; CompactnessWeight = compactnessWeight;
                Branchiness = branchiness;
            }
            public string PatchRuleId { get; }
            public string BiomeId { get; }
            public BiomePatchRole Role { get; }
            private int Minimum { get; }
            private int Maximum { get; }
            private int Distance { get; }
            private int CountMinimum { get; }
            private int CountMaximum { get; }
            private float SeedWeight { get; }
            private bool Edge { get; }
            private int Buffer { get; }
            private bool Single { get; }
            private float Share { get; }
            private float DistanceWeight { get; }
            private float AltitudeWeight { get; }
            private float NoiseWeight { get; }
            private float CompactnessWeight { get; }
            private float Branchiness { get; }
            public bool Matches(BiomePatchRuleDefinition value)
            {
                return value != null && value.Active &&
                       string.Equals(value.PatchRuleId, PatchRuleId, StringComparison.Ordinal) &&
                       string.Equals(value.BiomeId, BiomeId, StringComparison.Ordinal) &&
                       string.Equals(value.PatchRole, BiomePatchRoleTokenCodec.ToToken(Role), StringComparison.Ordinal) &&
                       value.MinSectorCount == Minimum && value.MaxSectorCount == Maximum &&
                       value.MinSeedDistance == Distance && value.SeedCountMin == CountMinimum &&
                       value.SeedCountMax == CountMaximum && SameFloat(value.SeedWeight, SeedWeight) &&
                       value.CanTouchWorldEdge == Edge && value.BufferRingSectors == Buffer &&
                       value.AllowSingleSector == Single && SameFloat(value.MaxWorldShare, Share) &&
                       SameFloat(value.DistanceWeight, DistanceWeight) &&
                       SameFloat(value.AltitudeWeight, AltitudeWeight) &&
                       SameFloat(value.NoiseWeight, NoiseWeight) &&
                       SameFloat(value.CompactnessWeight, CompactnessWeight) &&
                       SameFloat(value.BranchinessTarget, Branchiness);
            }
        }

        private sealed class ProfileSpecification
        {
            public ProfileSpecification(
                string id, string type, IReadOnlyList<string> orientations,
                int widthMin, int widthMax, int warningMin,
                bool mandatory, string tool, bool hard)
            {
                Id = id; Type = type; Orientations = orientations;
                WidthMin = widthMin; WidthMax = widthMax; WarningMin = warningMin;
                Mandatory = mandatory; Tool = tool; Hard = hard;
            }
            public string Id { get; }
            private string Type { get; }
            private IReadOnlyList<string> Orientations { get; }
            private int WidthMin { get; }
            private int WidthMax { get; }
            private int WarningMin { get; }
            private bool Mandatory { get; }
            private string Tool { get; }
            private bool Hard { get; }
            public bool Matches(BiomeBoundaryProfileDefinition value)
            {
                return value != null && value.Active &&
                       string.Equals(value.BoundaryProfileId, Id, StringComparison.Ordinal) &&
                       string.Equals(value.BoundaryType, Type, StringComparison.Ordinal) &&
                       SequenceEqual(value.AllowedOrientations, Orientations) &&
                       value.WidthMicrochunksMin == WidthMin && value.WidthMicrochunksMax == WidthMax &&
                       value.WarningMicrochunksMin == WarningMin && value.MandatoryRouteAllowed == Mandatory &&
                       string.Equals(value.ToolRequirement, Tool, StringComparison.Ordinal) &&
                       value.HardBorder == Hard;
            }
        }

        private sealed class PairSpecification
        {
            public PairSpecification(
                string id, string biomeA, string biomeB,
                IReadOnlyList<string> profiles, IReadOnlyList<int> weights, string defaultProfile)
            {
                Id = id; BiomeA = biomeA; BiomeB = biomeB;
                Profiles = profiles; Weights = weights; DefaultProfile = defaultProfile;
            }
            public string Id { get; }
            private string BiomeA { get; }
            private string BiomeB { get; }
            private IReadOnlyList<string> Profiles { get; }
            private IReadOnlyList<int> Weights { get; }
            private string DefaultProfile { get; }
            public bool Matches(BiomeBoundaryPairRuleDefinition value)
            {
                return value != null && value.Active &&
                       string.Equals(value.BoundaryPairRuleId, Id, StringComparison.Ordinal) &&
                       string.Equals(value.BiomeAId, BiomeA, StringComparison.Ordinal) &&
                       string.Equals(value.BiomeBId, BiomeB, StringComparison.Ordinal) &&
                       SequenceEqual(value.AllowedBoundaryProfileIds, Profiles) &&
                       SequenceEqual(value.BoundaryProfileWeights, Weights) &&
                       string.Equals(value.DefaultBoundaryProfileId, DefaultProfile, StringComparison.Ordinal) &&
                       value.MinSharedEdgeCount == 1;
            }
        }
    }
}
