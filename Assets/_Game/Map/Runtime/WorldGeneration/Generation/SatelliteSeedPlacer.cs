using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SatelliteSeedPlacer
    {
        private const string GenerationProfileId = "GEN_MOONPALACE_V1";
        private const string WorldProfileId = "WORLD_MOONPALACE_V1";
        private const int BiomeRetryMaximum = 100;

        public SatelliteSeedPlacementResult Place(
            CorePatchGrowthPublication growth,
            GenerationProfileDefinition generationProfile,
            IEnumerable<BiomeTypeDefinition> biomeTypes,
            IEnumerable<BiomePatchRuleDefinition> satelliteRules,
            DeterministicRngStream biomePatchRng)
        {
            try
            {
                var errors = new List<SatelliteSeedPlacementError>();
                var specifications = CreateRuleSpecifications();
                var context = ValidateGrowth(growth, specifications, errors);
                ValidateGenerationProfile(generationProfile, errors);
                ValidateDefinitions(biomeTypes, satelliteRules, specifications, context, errors);
                ValidateRng(biomePatchRng, errors);
                if (errors.Count != 0) return SatelliteSeedPlacementResult.Invalid(errors);

                return Execute(context, generationProfile, specifications, biomePatchRng);
            }
            catch
            {
                return SatelliteSeedPlacementResult.Invalid(new[]
                {
                    Error(
                        SatelliteSeedPlacementErrorCode.InternalInvariantViolation,
                        string.Empty,
                        string.Empty,
                        -1,
                        -1,
                        0,
                        0,
                        "Satellite seed placement violated an internal model invariant.")
                });
            }
        }

        private static ValidationContext ValidateGrowth(
            CorePatchGrowthPublication growth,
            IReadOnlyList<RuleSpecification> specifications,
            ICollection<SatelliteSeedPlacementError> errors)
        {
            var context = new ValidationContext { Growth = growth };
            if (growth == null)
            {
                errors.Add(StructuralError(
                    SatelliteSeedPlacementErrorCode.MissingGrowthPublication,
                    "A completed Core growth publication is required."));
                return context;
            }

            context.Source = growth.SourceSiteSnapshot;
            context.Input = growth.Snapshot;
            if (context.Source == null)
                errors.Add(StructuralError(
                    SatelliteSeedPlacementErrorCode.MissingSourceSiteSnapshot,
                    "The Core growth source site snapshot is required."));
            if (context.Input == null)
                errors.Add(StructuralError(
                    SatelliteSeedPlacementErrorCode.InvalidGrowthPublication,
                    "The Core growth biome snapshot is required."));
            if (context.Source == null || context.Input == null) return context;

            if (growth.SourceInitialization == null ||
                !ReferenceEquals(growth.SourceInitialization.SourceSiteSnapshot, context.Source) ||
                context.Source.Seed != context.Input.Seed ||
                growth.CorePatchCount != specifications.Count ||
                growth.CoreSeedCount != specifications.Count ||
                growth.CoreSiteBindingCount != specifications.Count ||
                growth.AssignedSectorCount != context.Input.AssignedSectorCount ||
                growth.UnassignedSectorCount != context.Input.UnassignedSectorCount ||
                growth.Records == null || growth.Records.Count != specifications.Count ||
                context.Input.IsComplete)
                errors.Add(StructuralError(
                    SatelliteSeedPlacementErrorCode.InvalidGrowthPublication,
                    "Core growth publication summaries or source linkage are invalid."));

            ValidateSourceSnapshot(context.Source, specifications, errors);
            ValidateCoreSnapshot(context, specifications, errors);
            return context;
        }

        private static void ValidateSourceSnapshot(
            SiteReservationSnapshot source,
            IReadOnlyList<RuleSpecification> specifications,
            ICollection<SatelliteSeedPlacementError> errors)
        {
            if (source.Reservations == null || source.Sectors == null || source.CoreBiomeSeeds == null ||
                source.Reservations.Count != 7 ||
                source.Sectors.Count != WorldGenConstants.SectorCount ||
                source.CoreBiomeSeeds.Count != specifications.Count)
            {
                errors.Add(StructuralError(
                    SatelliteSeedPlacementErrorCode.InvalidSourceSiteSnapshot,
                    "Source P01 must contain 7 reservations, 169 sectors, and four Core seeds."));
                return;
            }

            var reservationIds = new HashSet<SiteReservationId>();
            foreach (var reservation in source.Reservations)
                if (reservation == null || !reservation.ReservationId.IsValid ||
                    !reservationIds.Add(reservation.ReservationId))
                    errors.Add(StructuralError(
                        SatelliteSeedPlacementErrorCode.InvalidSourceSiteSnapshot,
                        "Source reservations must be non-null with unique canonical IDs."));

            for (var index = 0; index < source.Sectors.Count; index++)
            {
                var sector = source.Sectors[index];
                if (sector == null || sector.Index != index ||
                    sector.Coordinate != WorldGridIndex.ToCoordinate(index))
                    errors.Add(Error(
                        SatelliteSeedPlacementErrorCode.InvalidReservationState,
                        string.Empty, string.Empty, -1, index, 1, 0,
                        "A P01 sector row has invalid grid identity."));
            }
        }

        private static void ValidateCoreSnapshot(
            ValidationContext context,
            IReadOnlyList<RuleSpecification> specifications,
            ICollection<SatelliteSeedPlacementError> errors)
        {
            var snapshot = context.Input;
            if (snapshot.Patches == null || snapshot.Sectors == null || snapshot.SiteBindings == null ||
                snapshot.Patches.Count != specifications.Count ||
                snapshot.SiteBindings.Count != specifications.Count ||
                snapshot.Sectors.Count != WorldGenConstants.SectorCount)
            {
                errors.Add(StructuralError(
                    SatelliteSeedPlacementErrorCode.InvalidCorePatchState,
                    "Input P02 must contain four Core patches, four bindings, and 169 ownership rows."));
                return;
            }

            var expectedBiomes = new HashSet<string>(
                specifications.Select(value => value.BiomeId), StringComparer.Ordinal);
            foreach (var patch in snapshot.Patches)
            {
                if (patch == null || patch.Role != BiomePatchRole.Core ||
                    !expectedBiomes.Contains(patch.BiomeId) ||
                    patch.Seeds == null || patch.Seeds.Count == 0 ||
                    patch.SectorIndices == null || patch.SectorCount == 0 ||
                    !context.CorePatchesByBiome.TryAdd(patch.BiomeId, patch))
                {
                    errors.Add(StructuralError(
                        SatelliteSeedPlacementErrorCode.InvalidCorePatchState,
                        "Input P02 patches must be the exact unique four Core patches."));
                    continue;
                }
                foreach (var seed in patch.Seeds)
                    if (seed == null || seed.Role != BiomePatchRole.Core ||
                        !seed.SourceSiteReservationId.HasValue ||
                        !patch.ContainsSector(seed.SectorIndex))
                        errors.Add(Error(
                            SatelliteSeedPlacementErrorCode.InvalidCorePatchState,
                            patch.PatchRuleId, patch.BiomeId, -1,
                            seed == null ? -1 : seed.SectorIndex, 1, 0,
                            "A Core seed has invalid role, source, or membership."));
            }

            foreach (var specification in specifications)
                if (!context.CorePatchesByBiome.TryGetValue(specification.BiomeId, out var patch) ||
                    !string.Equals(patch.PatchRuleId, specification.CorePatchRuleId, StringComparison.Ordinal))
                    errors.Add(Error(
                        SatelliteSeedPlacementErrorCode.InvalidCorePatchState,
                        specification.PatchRuleId, specification.BiomeId, -1, -1, 1, 0,
                        "A required same-biome Core patch is missing or has invalid identity."));

            var bindingIds = new HashSet<SiteReservationId>();
            foreach (var binding in snapshot.SiteBindings)
                if (binding == null || !bindingIds.Add(binding.SiteReservationId) ||
                    !snapshot.TryGetPatch(binding.PatchId, out var patch) ||
                    patch.Role != BiomePatchRole.Core ||
                    !string.Equals(binding.BiomeId, patch.BiomeId, StringComparison.Ordinal))
                    errors.Add(StructuralError(
                        SatelliteSeedPlacementErrorCode.InvalidCorePatchState,
                        "Core site bindings must be unique and match Core patches."));

            for (var index = 0; index < snapshot.Sectors.Count; index++)
            {
                var ownership = snapshot.Sectors[index];
                if (ownership == null || ownership.SectorIndex != index ||
                    ownership.Sector != WorldGridIndex.ToCoordinate(index) ||
                    ownership.SecondaryBiomeId == null || ownership.SecondaryBiomeId.Length != 0)
                {
                    errors.Add(Error(
                        SatelliteSeedPlacementErrorCode.InvalidCorePatchState,
                        string.Empty, string.Empty, -1, index, 1, 0,
                        "An input ownership row has invalid grid identity or secondary biome."));
                    continue;
                }
                if (ownership.IsAssigned)
                {
                    if (!ownership.PatchId.HasValue ||
                        !snapshot.TryGetPatch(ownership.PatchId.Value, out var patch) ||
                        patch.Role != BiomePatchRole.Core ||
                        !patch.ContainsSector(index) ||
                        !string.Equals(ownership.PrimaryBiomeId, patch.BiomeId, StringComparison.Ordinal))
                        errors.Add(Error(
                            SatelliteSeedPlacementErrorCode.InvalidCorePatchState,
                            string.Empty, ownership.PrimaryBiomeId ?? string.Empty, -1, index, 1, 0,
                            "Assigned input ownership must match an exact Core patch."));
                }
                else if (ownership.PatchId.HasValue ||
                         !string.IsNullOrEmpty(ownership.PrimaryBiomeId) ||
                         !string.IsNullOrEmpty(ownership.SecondaryBiomeId))
                {
                    errors.Add(Error(
                        SatelliteSeedPlacementErrorCode.InvalidCorePatchState,
                        string.Empty, string.Empty, -1, index, 0, 1,
                        "Unassigned ownership cannot contain partial state."));
                }

                var reservation = context.Source.GetSector(index);
                if (reservation.IsReserved)
                {
                    var coreSource = reservation.Kind == SiteReservationKind.CoreResource ||
                                     reservation.Kind == SiteReservationKind.Forge;
                    if (coreSource != ownership.IsAssigned)
                        errors.Add(Error(
                            SatelliteSeedPlacementErrorCode.InvalidReservationState,
                            string.Empty, ownership.PrimaryBiomeId ?? string.Empty, -1, index, 1, 0,
                            "Reserved P01 ownership does not match Core-source policy."));
                }
            }

            if (snapshot.AssignedSectorCount + snapshot.UnassignedSectorCount != WorldGenConstants.SectorCount)
                errors.Add(StructuralError(
                    SatelliteSeedPlacementErrorCode.InvalidCorePatchState,
                    "Input ownership counts must cover the world."));
        }

        private static void ValidateGenerationProfile(
            GenerationProfileDefinition profile,
            ICollection<SatelliteSeedPlacementError> errors)
        {
            if (profile == null)
            {
                errors.Add(StructuralError(
                    SatelliteSeedPlacementErrorCode.MissingGenerationProfile,
                    "The active Moon Palace generation profile is required."));
                return;
            }
            if (!string.Equals(profile.GenerationProfileId, GenerationProfileId, StringComparison.Ordinal) ||
                !string.Equals(profile.WorldProfileId, WorldProfileId, StringComparison.Ordinal) ||
                !profile.Active || profile.BiomeRetryMax != BiomeRetryMaximum)
                errors.Add(Error(
                    SatelliteSeedPlacementErrorCode.InvalidGenerationProfile,
                    profile.GenerationProfileId ?? string.Empty,
                    string.Empty, -1, -1, BiomeRetryMaximum,
                    Math.Max(0, profile.BiomeRetryMax),
                    "Generation profile identity, activity, or biome retry limit is invalid."));
        }

        private static void ValidateDefinitions(
            IEnumerable<BiomeTypeDefinition> biomeTypes,
            IEnumerable<BiomePatchRuleDefinition> satelliteRules,
            IReadOnlyList<RuleSpecification> specifications,
            ValidationContext context,
            ICollection<SatelliteSeedPlacementError> errors)
        {
            if (biomeTypes == null)
                errors.Add(StructuralError(
                    SatelliteSeedPlacementErrorCode.MissingBiomeTypes,
                    "Biome type definitions are required."));
            else
                foreach (var biome in biomeTypes)
                {
                    if (biome == null)
                    {
                        errors.Add(StructuralError(
                            SatelliteSeedPlacementErrorCode.NullDefinition,
                            "Definition collections cannot contain null."));
                        continue;
                    }
                    var id = biome.BiomeId ?? string.Empty;
                    if (!ReservationValidation.IsCanonicalId(id, false))
                    {
                        errors.Add(StructuralError(
                            SatelliteSeedPlacementErrorCode.InvalidBiomeDefinition,
                            "A biome definition has an invalid canonical ID."));
                        continue;
                    }
                    if (!context.Biomes.TryAdd(id, biome))
                        errors.Add(Error(
                            SatelliteSeedPlacementErrorCode.DuplicateDefinitionId,
                            id, id, -1, -1, 1, 2,
                            "Biome definition IDs must be unique."));
                }

            if (satelliteRules == null)
                errors.Add(StructuralError(
                    SatelliteSeedPlacementErrorCode.MissingSatelliteRules,
                    "Satellite patch rules are required."));
            else
                foreach (var rule in satelliteRules)
                {
                    if (rule == null)
                    {
                        errors.Add(StructuralError(
                            SatelliteSeedPlacementErrorCode.NullDefinition,
                            "Definition collections cannot contain null."));
                        continue;
                    }
                    var id = rule.PatchRuleId ?? string.Empty;
                    if (!ReservationValidation.IsCanonicalId(id, false))
                    {
                        errors.Add(StructuralError(
                            SatelliteSeedPlacementErrorCode.InvalidSatelliteRule,
                            "A Satellite rule has an invalid canonical ID."));
                        continue;
                    }
                    if (!context.Rules.TryAdd(id, rule))
                        errors.Add(Error(
                            SatelliteSeedPlacementErrorCode.DuplicateDefinitionId,
                            id, rule.BiomeId ?? string.Empty, -1, -1, 1, 2,
                            "Satellite rule IDs must be unique."));
                }

            var expectedBiomes = new HashSet<string>(
                specifications.Select(value => value.BiomeId), StringComparer.Ordinal);
            var expectedRules = new HashSet<string>(
                specifications.Select(value => value.PatchRuleId), StringComparer.Ordinal);
            foreach (var pair in context.Biomes)
                if (pair.Value.Active && pair.Value.Required && !expectedBiomes.Contains(pair.Key))
                    errors.Add(Error(
                        SatelliteSeedPlacementErrorCode.UnexpectedBiomeDefinition,
                        pair.Key, pair.Key, -1, -1, 0, 1,
                        "Only the exact four required active biomes are accepted."));
            foreach (var pair in context.Rules)
                if (pair.Value.Active &&
                    string.Equals(pair.Value.PatchRole, "SATELLITE", StringComparison.Ordinal) &&
                    !expectedRules.Contains(pair.Key))
                    errors.Add(Error(
                        SatelliteSeedPlacementErrorCode.UnexpectedSatelliteRule,
                        pair.Key, pair.Value.BiomeId ?? string.Empty, -1, -1, 0, 1,
                        "Only the exact four active Satellite rules are accepted."));

            foreach (var specification in specifications)
            {
                if (!context.Biomes.TryGetValue(specification.BiomeId, out var biome))
                {
                    errors.Add(Error(
                        SatelliteSeedPlacementErrorCode.MissingBiomeDefinition,
                        specification.BiomeId, specification.BiomeId, -1, -1, 1, 0,
                        "A required Satellite biome definition is missing."));
                }
                else
                {
                    if (!biome.Active || !biome.Required || biome.MinPatchCount < 1 ||
                        biome.MaxPatchCount < biome.MinPatchCount || biome.MinCorePatchCount < 1)
                        errors.Add(Error(
                            SatelliteSeedPlacementErrorCode.InvalidBiomeDefinition,
                            biome.BiomeId, biome.BiomeId, -1, -1, 1,
                            Math.Max(0, biome.MaxPatchCount),
                            "A required biome definition has invalid activity or patch ranges."));
                }

                if (!context.Rules.TryGetValue(specification.PatchRuleId, out var rule))
                {
                    errors.Add(Error(
                        SatelliteSeedPlacementErrorCode.MissingSatelliteRule,
                        specification.PatchRuleId, specification.BiomeId, -1, -1, 1, 0,
                        "A required Satellite rule is missing."));
                    continue;
                }
                if (!specification.Matches(rule))
                    errors.Add(Error(
                        SatelliteSeedPlacementErrorCode.InvalidSatelliteRule,
                        specification.PatchRuleId, specification.BiomeId, -1, -1, 1, 0,
                        "A required Satellite rule does not match the frozen contract."));
                if (!string.Equals(rule.BiomeId, specification.BiomeId, StringComparison.Ordinal))
                    errors.Add(Error(
                        SatelliteSeedPlacementErrorCode.DefinitionIdentityMismatch,
                        specification.PatchRuleId, specification.BiomeId, -1, -1, 1, 0,
                        "Satellite rule and biome identities must match exactly."));

                if (biome != null && context.CorePatchesByBiome.TryGetValue(specification.BiomeId, out var core))
                {
                    var existing = context.Input.Patches.Count(value =>
                        string.Equals(value.BiomeId, specification.BiomeId, StringComparison.Ordinal));
                    if (existing != 1 || core.Role != BiomePatchRole.Core)
                        errors.Add(Error(
                            SatelliteSeedPlacementErrorCode.InvalidCorePatchState,
                            specification.PatchRuleId, specification.BiomeId, -1, -1, 1, existing,
                            "Each Satellite biome requires exactly one existing Core patch."));
                    if (existing + specification.SeedCountMaximum > biome.MaxPatchCount)
                        errors.Add(Error(
                            SatelliteSeedPlacementErrorCode.PatchCountLimitExceeded,
                            specification.PatchRuleId, specification.BiomeId, -1, -1,
                            existing + specification.SeedCountMaximum, biome.MaxPatchCount,
                            "Satellite maximum count exceeds the biome patch-count limit."));
                }
            }
        }

        private static void ValidateRng(
            DeterministicRngStream rng,
            ICollection<SatelliteSeedPlacementError> errors)
        {
            if (rng == null)
                errors.Add(StructuralError(
                    SatelliteSeedPlacementErrorCode.MissingBiomePatchRng,
                    "A fresh RNG_BIOME_PATCH stream is required."));
            else if (rng.DrawCount != 0)
                errors.Add(Error(
                    SatelliteSeedPlacementErrorCode.InvalidBiomePatchRngState,
                    WorldGenerationRngStreams.BiomePatchStreamId,
                    string.Empty, -1, -1, 0,
                    rng.DrawCount > int.MaxValue ? int.MaxValue : (int)rng.DrawCount,
                    "Biome patch RNG must have zero prior draws."));
        }

        private static SatelliteSeedPlacementResult Execute(
            ValidationContext context,
            GenerationProfileDefinition profile,
            IReadOnlyList<RuleSpecification> specifications,
            DeterministicRngStream rng)
        {
            var rawCandidates = new List<int>();
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
                if (!context.Input.GetSector(index).IsAssigned &&
                    !context.Source.GetSector(index).IsReserved)
                    rawCandidates.Add(index);

            var works = new List<RuleWork>(specifications.Count);
            foreach (var specification in specifications)
            {
                var desired = rng.NextInt(
                    specification.SeedCountMinimum,
                    specification.SeedCountMaximum + 1);
                var work = new RuleWork(
                    specification,
                    context.Rules[specification.PatchRuleId],
                    desired);
                foreach (var index in context.CorePatchesByBiome[specification.BiomeId].SectorIndices)
                    work.SameBiomeSectors.Add(index);
                works.Add(work);
            }

            var accepted = new HashSet<int>();
            var records = new List<SatelliteSeedPlacementRecord>();
            foreach (var work in works)
            {
                for (var ordinal = 0; ordinal < work.DesiredSeedCount; ordinal++)
                {
                    var pool = rawCandidates.Where(index =>
                        !accepted.Contains(index) && !work.RejectedSectors.Contains(index)).ToList();
                    var limit = Math.Min(profile.BiomeRetryMax, pool.Count);
                    var attempts = 0;
                    var edgeRejections = 0;
                    var distanceRejections = 0;
                    var placed = false;
                    var acceptedRoll = -1;
                    var acceptedSector = -1;
                    var acceptedDistance = -1;

                    while (attempts < limit && pool.Count != 0)
                    {
                        var roll = rng.NextInt(pool.Count);
                        var sectorIndex = pool[roll];
                        pool.RemoveAt(roll);
                        attempts++;
                        work.CandidateMethodCallCount++;
                        work.CandidateAttemptCount++;

                        var coordinate = WorldGridIndex.ToCoordinate(sectorIndex);
                        if (!work.Specification.CanTouchWorldEdge && IsWorldEdge(coordinate))
                        {
                            edgeRejections++;
                            work.EdgeRejectionCount++;
                            work.RejectedSectors.Add(sectorIndex);
                            continue;
                        }

                        var distance = SameBiomeDistance(sectorIndex, work.SameBiomeSectors);
                        if (distance < work.Specification.MinimumSeedDistance)
                        {
                            distanceRejections++;
                            work.DistanceRejectionCount++;
                            work.RejectedSectors.Add(sectorIndex);
                            continue;
                        }

                        placed = true;
                        acceptedRoll = roll;
                        acceptedSector = sectorIndex;
                        acceptedDistance = distance;
                        break;
                    }

                    if (!placed)
                    {
                        work.Exhausted = true;
                        work.FailedSatelliteOrdinal = ordinal;
                        var errors = new[]
                        {
                            Error(
                                SatelliteSeedPlacementErrorCode.CandidateAttemptsExhausted,
                                work.Specification.PatchRuleId,
                                work.Specification.BiomeId,
                                ordinal,
                                -1,
                                1,
                                0,
                                "Candidate redraw attempts were exhausted for a Satellite seed.")
                        };
                        return SatelliteSeedPlacementResult.Retry(
                            BuildDiagnostics(
                                context, works, Array.Empty<SatelliteSeedPlacementRecord>(),
                                rawCandidates.Count, rng.DrawCount, true),
                            errors);
                    }

                    var patchId = new SatellitePatchIdFactory().Create(
                        work.Specification.BiomeId, ordinal);
                    records.Add(new SatelliteSeedPlacementRecord(
                        work.Specification.PatchRuleId,
                        work.Specification.BiomeId,
                        ordinal,
                        patchId,
                        acceptedSector,
                        WorldGridIndex.ToCoordinate(acceptedSector),
                        acceptedDistance,
                        work.Specification.MinimumSeedDistance,
                        acceptedRoll,
                        attempts,
                        edgeRejections,
                        distanceRejections));
                    work.AcceptedSeedCount++;
                    work.SameBiomeSectors.Add(acceptedSector);
                    accepted.Add(acceptedSector);
                }
            }

            var patches = new List<BiomePatch>(context.Input.Patches);
            var ownership = new List<BiomeSectorOwnership>(context.Input.Sectors);
            foreach (var record in records)
            {
                var seed = new BiomePatchSeed(
                    record.SectorIndex,
                    record.Sector,
                    BiomePatchRole.Satellite,
                    null);
                patches.Add(new BiomePatch(
                    record.PatchId,
                    record.BiomeId,
                    record.PatchRuleId,
                    BiomePatchRole.Satellite,
                    new[] { seed },
                    new[] { record.SectorIndex }));
                ownership[record.SectorIndex] = new BiomeSectorOwnership(
                    record.SectorIndex,
                    record.Sector,
                    record.BiomeId,
                    string.Empty,
                    record.PatchId);
            }

            var snapshot = new BiomePatchSnapshot(
                context.Input.Seed,
                patches,
                ownership,
                context.Input.SiteBindings);
            var publication = new SatelliteSeedPlacementPublication(
                context.Growth,
                snapshot,
                records);
            var diagnostics = BuildDiagnostics(
                context, works, records, rawCandidates.Count, rng.DrawCount, false);
            return SatelliteSeedPlacementResult.Completed(publication, diagnostics);
        }

        private static SatelliteSeedPlacementDiagnostics BuildDiagnostics(
            ValidationContext context,
            IReadOnlyList<RuleWork> works,
            IEnumerable<SatelliteSeedPlacementRecord> records,
            int rawCandidateCount,
            ulong rngDrawCountAfter,
            bool rollback)
        {
            var rules = works.Select(work => new SatelliteRulePlacementDiagnostics(
                work.Specification.PatchRuleId,
                work.Specification.BiomeId,
                work.DesiredSeedCount,
                work.DesiredSeedCount,
                work.AcceptedSeedCount,
                work.CandidateMethodCallCount,
                work.CandidateAttemptCount,
                work.EdgeRejectionCount,
                work.DistanceRejectionCount,
                work.Exhausted,
                work.FailedSatelliteOrdinal)).ToArray();
            var desired = works.Sum(value => value.DesiredSeedCount);
            var placed = rollback ? 0 : works.Sum(value => value.AcceptedSeedCount);
            return new SatelliteSeedPlacementDiagnostics(
                context.Input.Seed,
                rules,
                records,
                rawCandidateCount,
                works.Count,
                works.Sum(value => value.CandidateMethodCallCount),
                0,
                rngDrawCountAfter,
                desired,
                placed,
                context.Input.Patches.Count,
                context.Input.AssignedSectorCount,
                rollback ? context.Input.Patches.Count : context.Input.Patches.Count + placed,
                rollback ? context.Input.AssignedSectorCount : context.Input.AssignedSectorCount + placed,
                rollback ? context.Input.UnassignedSectorCount : context.Input.UnassignedSectorCount - placed,
                0,
                0,
                rollback);
        }

        private static int SameBiomeDistance(int candidate, IEnumerable<int> sources)
        {
            var coordinate = WorldGridIndex.ToCoordinate(candidate);
            var best = int.MaxValue;
            foreach (var source in sources)
            {
                var sourceCoordinate = WorldGridIndex.ToCoordinate(source);
                best = Math.Min(best,
                    Math.Abs(coordinate.X - sourceCoordinate.X) +
                    Math.Abs(coordinate.Y - sourceCoordinate.Y));
            }
            if (best == int.MaxValue)
                throw new InvalidOperationException("Same-biome distance source is missing.");
            return best;
        }

        private static bool IsWorldEdge(SectorCoord coordinate)
        {
            return coordinate.X == 0 || coordinate.X == WorldGenConstants.SectorColumns - 1 ||
                   coordinate.Y == 0 || coordinate.Y == WorldGenConstants.SectorRows - 1;
        }

        private static SatelliteSeedPlacementError StructuralError(
            SatelliteSeedPlacementErrorCode code,
            string message)
        {
            return Error(code, string.Empty, string.Empty, -1, -1, 0, 0, message);
        }

        private static SatelliteSeedPlacementError Error(
            SatelliteSeedPlacementErrorCode code,
            string definitionId,
            string biomeId,
            int satelliteOrdinal,
            int sectorIndex,
            int requiredCount,
            int availableCount,
            string message)
        {
            return new SatelliteSeedPlacementError(
                code,
                ReservationValidation.IsCanonicalId(definitionId, true) ? definitionId : string.Empty,
                ReservationValidation.IsCanonicalId(biomeId, true) ? biomeId : string.Empty,
                satelliteOrdinal,
                sectorIndex,
                requiredCount,
                availableCount,
                message);
        }

        private static IReadOnlyList<RuleSpecification> CreateRuleSpecifications()
        {
            return new[]
            {
                new RuleSpecification(
                    "PATCH_CRATER_SAT", "PATCH_CRATER_CORE", "BIO_MOON_CRATER",
                    2, 16, 3, 0, 3, 70f, true),
                new RuleSpecification(
                    "PATCH_DOUGH_SAT", "PATCH_DOUGH_CORE", "BIO_MOON_DOUGH",
                    2, 14, 3, 0, 3, 70f, true),
                new RuleSpecification(
                    "PATCH_MILL_SAT", "PATCH_MILL_CORE", "BIO_ABANDONED_MILL",
                    2, 10, 3, 0, 2, 45f, false),
                new RuleSpecification(
                    "PATCH_ROOT_SAT", "PATCH_ROOT_CORE", "BIO_CASSIA_ROOT",
                    2, 14, 3, 0, 3, 70f, false)
            };
        }

        private sealed class ValidationContext
        {
            public CorePatchGrowthPublication Growth { get; set; }
            public SiteReservationSnapshot Source { get; set; }
            public BiomePatchSnapshot Input { get; set; }
            public Dictionary<string, BiomePatch> CorePatchesByBiome { get; } =
                new Dictionary<string, BiomePatch>(StringComparer.Ordinal);
            public Dictionary<string, BiomeTypeDefinition> Biomes { get; } =
                new Dictionary<string, BiomeTypeDefinition>(StringComparer.Ordinal);
            public Dictionary<string, BiomePatchRuleDefinition> Rules { get; } =
                new Dictionary<string, BiomePatchRuleDefinition>(StringComparer.Ordinal);
        }

        private sealed class RuleSpecification
        {
            public RuleSpecification(
                string patchRuleId,
                string corePatchRuleId,
                string biomeId,
                int minimumSectorCount,
                int maximumSectorCount,
                int minimumSeedDistance,
                int seedCountMinimum,
                int seedCountMaximum,
                float seedWeight,
                bool canTouchWorldEdge)
            {
                PatchRuleId = patchRuleId;
                CorePatchRuleId = corePatchRuleId;
                BiomeId = biomeId;
                MinimumSectorCount = minimumSectorCount;
                MaximumSectorCount = maximumSectorCount;
                MinimumSeedDistance = minimumSeedDistance;
                SeedCountMinimum = seedCountMinimum;
                SeedCountMaximum = seedCountMaximum;
                SeedWeight = seedWeight;
                CanTouchWorldEdge = canTouchWorldEdge;
            }

            public string PatchRuleId { get; }
            public string CorePatchRuleId { get; }
            public string BiomeId { get; }
            public int MinimumSectorCount { get; }
            public int MaximumSectorCount { get; }
            public int MinimumSeedDistance { get; }
            public int SeedCountMinimum { get; }
            public int SeedCountMaximum { get; }
            public float SeedWeight { get; }
            public bool CanTouchWorldEdge { get; }

            public bool Matches(BiomePatchRuleDefinition rule)
            {
                return rule != null && rule.Active &&
                       string.Equals(rule.PatchRuleId, PatchRuleId, StringComparison.Ordinal) &&
                       string.Equals(rule.BiomeId, BiomeId, StringComparison.Ordinal) &&
                       string.Equals(rule.PatchRole, "SATELLITE", StringComparison.Ordinal) &&
                       rule.MinSectorCount == MinimumSectorCount &&
                       rule.MaxSectorCount == MaximumSectorCount &&
                       rule.MinSeedDistance == MinimumSeedDistance &&
                       rule.SeedCountMin == SeedCountMinimum &&
                       rule.SeedCountMax == SeedCountMaximum &&
                       rule.SeedWeight == SeedWeight &&
                       rule.CanTouchWorldEdge == CanTouchWorldEdge &&
                       rule.BufferRingSectors == 0 && !rule.AllowSingleSector;
            }
        }

        private sealed class RuleWork
        {
            public RuleWork(
                RuleSpecification specification,
                BiomePatchRuleDefinition rule,
                int desiredSeedCount)
            {
                Specification = specification;
                Rule = rule;
                DesiredSeedCount = desiredSeedCount;
            }

            public RuleSpecification Specification { get; }
            public BiomePatchRuleDefinition Rule { get; }
            public int DesiredSeedCount { get; }
            public int AcceptedSeedCount { get; set; }
            public int CandidateMethodCallCount { get; set; }
            public int CandidateAttemptCount { get; set; }
            public int EdgeRejectionCount { get; set; }
            public int DistanceRejectionCount { get; set; }
            public bool Exhausted { get; set; }
            public int FailedSatelliteOrdinal { get; set; } = -1;
            public HashSet<int> SameBiomeSectors { get; } = new HashSet<int>();
            public HashSet<int> RejectedSectors { get; } = new HashSet<int>();
        }
    }
}
