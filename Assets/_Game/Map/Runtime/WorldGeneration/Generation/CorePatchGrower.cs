using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class CorePatchGrower
    {
        public CorePatchGrowthResult Grow(
            CorePatchInitializationPublication initialization,
            IEnumerable<BiomeTypeDefinition> biomeTypes,
            IEnumerable<BiomePatchRuleDefinition> patchRules)
        {
            try
            {
                var errors = new List<CorePatchGrowthError>();
                var sources = CreateCoreSources();
                var context = ValidateInitialization(initialization, sources, errors);
                ValidateDefinitions(biomeTypes, patchRules, sources, context, errors);
                if (errors.Count != 0) return CorePatchGrowthResult.Invalid(errors);

                var works = BuildWorks(sources, context);
                foreach (var work in works)
                {
                    BuildMandatoryBuffer(work);
                    if (work.TargetSectorCount > work.Rule.MaxSectorCount)
                    {
                        errors.Add(Error(
                            CorePatchGrowthErrorCode.TargetExceedsMaximum,
                            work,
                            default(SiteReservationId),
                            -1,
                            work.TargetSectorCount,
                            work.Rule.MaxSectorCount,
                            "A Core growth target exceeds its rule maximum."));
                    }
                }
                if (errors.Count != 0) return CorePatchGrowthResult.Invalid(errors);

                var spatialErrors = ValidateSpatialGates(works, context.SourceSiteSnapshot);
                if (spatialErrors.Count != 0)
                    return CorePatchGrowthResult.Retry(
                        BuildRollbackDiagnostics(initialization, works.Count),
                        spatialErrors);

                return ExecuteGrowth(initialization, works);
            }
            catch
            {
                return CorePatchGrowthResult.Invalid(new[]
                {
                    new CorePatchGrowthError(
                        CorePatchGrowthErrorCode.InternalInvariantViolation,
                        default(BiomePatchId),
                        default(SiteReservationId),
                        default(SiteReservationId),
                        -1,
                        0,
                        0,
                        "Core patch growth violated an internal model invariant.")
                });
            }
        }

        private static ValidationContext ValidateInitialization(
            CorePatchInitializationPublication initialization,
            IReadOnlyList<CoreSource> sources,
            ICollection<CorePatchGrowthError> errors)
        {
            var context = new ValidationContext();
            if (initialization == null)
            {
                errors.Add(StructuralError(
                    CorePatchGrowthErrorCode.MissingInitialization,
                    "A completed Core patch initialization publication is required."));
                return context;
            }

            context.Initialization = initialization;
            context.SourceSiteSnapshot = initialization.SourceSiteSnapshot;
            context.InputSnapshot = initialization.Snapshot;
            if (context.SourceSiteSnapshot == null)
            {
                errors.Add(StructuralError(
                    CorePatchGrowthErrorCode.MissingSourceSiteSnapshot,
                    "The initialization source site snapshot is required."));
            }
            if (context.InputSnapshot == null)
            {
                errors.Add(StructuralError(
                    CorePatchGrowthErrorCode.InvalidInitialization,
                    "The initialization biome patch snapshot is required."));
            }
            if (context.SourceSiteSnapshot == null || context.InputSnapshot == null)
                return context;

            if (context.SourceSiteSnapshot.Seed != context.InputSnapshot.Seed ||
                initialization.CorePatchCount != sources.Count ||
                initialization.CorePatchIds == null ||
                initialization.CorePatchIds.Count != sources.Count ||
                initialization.CoreSiteBindingCount != sources.Count ||
                initialization.AssignedSectorCount != context.InputSnapshot.AssignedSectorCount ||
                initialization.UnassignedSectorCount != context.InputSnapshot.UnassignedSectorCount ||
                context.InputSnapshot.IsComplete)
            {
                errors.Add(StructuralError(
                    CorePatchGrowthErrorCode.InvalidInitialization,
                    "The initialization publication summary or seed identity is invalid."));
            }

            ValidateSourceSiteSnapshot(context, sources, errors);
            ValidateInputSnapshot(context, sources, errors);
            return context;
        }

        private static void ValidateSourceSiteSnapshot(
            ValidationContext context,
            IReadOnlyList<CoreSource> sources,
            ICollection<CorePatchGrowthError> errors)
        {
            var snapshot = context.SourceSiteSnapshot;
            if (snapshot.Reservations == null || snapshot.Sectors == null || snapshot.CoreBiomeSeeds == null ||
                snapshot.Reservations.Count != 7 ||
                snapshot.Sectors.Count != WorldGenConstants.SectorCount ||
                snapshot.CoreBiomeSeeds.Count != sources.Count)
            {
                errors.Add(StructuralError(
                    CorePatchGrowthErrorCode.InvalidSourceSiteSnapshot,
                    "The source site snapshot must contain 7 reservations, 169 sectors, and four Core seeds."));
                return;
            }

            var reservations = new Dictionary<SiteReservationId, SiteReservation>();
            foreach (var reservation in snapshot.Reservations)
            {
                if (reservation == null || !reservation.ReservationId.IsValid ||
                    !reservations.TryAdd(reservation.ReservationId, reservation))
                {
                    errors.Add(StructuralError(
                        CorePatchGrowthErrorCode.InvalidSourceSiteSnapshot,
                        "Source site reservations must be non-null with unique valid IDs."));
                    continue;
                }
            }

            foreach (var expected in CreateReservationSources())
            {
                var id = new SiteReservationId(expected.ReservationId);
                if (!reservations.TryGetValue(id, out var reservation) ||
                    reservation.Kind != expected.Kind ||
                    reservation.ReservationOrder != expected.Order ||
                    !string.Equals(reservation.SourceDefinitionId, expected.SourceDefinitionId, StringComparison.Ordinal))
                {
                    errors.Add(new CorePatchGrowthError(
                        CorePatchGrowthErrorCode.InvalidSourceSiteSnapshot,
                        default(BiomePatchId), id, default(SiteReservationId), -1, 1, 0,
                        "A canonical source reservation is missing or has invalid identity."));
                }
            }

            var seeds = new Dictionary<SiteReservationId, CoreBiomeSeed>();
            foreach (var seed in snapshot.CoreBiomeSeeds)
            {
                if (seed == null || !seed.SourceReservationId.IsValid ||
                    !seeds.TryAdd(seed.SourceReservationId, seed))
                {
                    errors.Add(StructuralError(
                        CorePatchGrowthErrorCode.InvalidSourceSiteSnapshot,
                        "Source Core seeds must be non-null with unique valid source IDs."));
                }
            }

            for (var index = 0; index < snapshot.Sectors.Count; index++)
            {
                var sector = snapshot.Sectors[index];
                if (sector == null || sector.Index != index || sector.Coordinate != WorldGridIndex.ToCoordinate(index))
                    errors.Add(new CorePatchGrowthError(
                        CorePatchGrowthErrorCode.InvalidSourceSiteSnapshot,
                        default(BiomePatchId), default(SiteReservationId), default(SiteReservationId),
                        index, 1, 0, "A source sector row has invalid grid identity."));
            }

            foreach (var source in sources)
            {
                var sourceId = new SiteReservationId(source.ReservationId);
                if (!reservations.TryGetValue(sourceId, out var reservation)) continue;
                context.Reservations[sourceId] = reservation;
                var footprint = GetFootprintIndices(reservation);
                if ((reservation.Kind != SiteReservationKind.CoreResource && reservation.Kind != SiteReservationKind.Forge) ||
                    !string.Equals(reservation.PrimaryBiomeId, source.BiomeId, StringComparison.Ordinal) ||
                    footprint.Count == 0 || !IsCardinallyConnected(footprint))
                {
                    errors.Add(new CorePatchGrowthError(
                        CorePatchGrowthErrorCode.InvalidSourceSiteSnapshot,
                        source.PatchId, sourceId, default(SiteReservationId), -1, 1, 0,
                        "A Core source reservation has invalid kind, biome, or footprint."));
                }

                if (!seeds.TryGetValue(sourceId, out var seed))
                {
                    errors.Add(new CorePatchGrowthError(
                        CorePatchGrowthErrorCode.InvalidCoreSeed,
                        source.PatchId, sourceId, default(SiteReservationId), -1, 1, 0,
                        "A required source Core seed is missing."));
                    continue;
                }
                context.CoreSeeds[sourceId] = seed;
                var seedIndex = WorldGridIndex.ToIndex(seed.SeedSector);
                if (!string.Equals(seed.BiomeId, source.BiomeId, StringComparison.Ordinal) ||
                    !string.Equals(seed.CorePatchRuleId, source.PatchRuleId, StringComparison.Ordinal) ||
                    seedIndex != footprint[0])
                {
                    errors.Add(new CorePatchGrowthError(
                        CorePatchGrowthErrorCode.InvalidCoreSeed,
                        source.PatchId, sourceId, default(SiteReservationId), seedIndex, 1, 0,
                        "A source Core seed has invalid identity or sector."));
                }
            }
        }

        private static void ValidateInputSnapshot(
            ValidationContext context,
            IReadOnlyList<CoreSource> sources,
            ICollection<CorePatchGrowthError> errors)
        {
            var snapshot = context.InputSnapshot;
            if (snapshot.Patches == null || snapshot.Sectors == null || snapshot.SiteBindings == null ||
                snapshot.Patches.Count != sources.Count ||
                snapshot.SiteBindings.Count != sources.Count ||
                snapshot.Sectors.Count != WorldGenConstants.SectorCount)
            {
                errors.Add(StructuralError(
                    CorePatchGrowthErrorCode.InvalidInitialization,
                    "The input P02 snapshot must contain four Core patches, four bindings, and 169 ownership rows."));
                return;
            }

            var expectedOwners = new Dictionary<int, CoreSource>();
            foreach (var source in sources)
            {
                var sourceId = new SiteReservationId(source.ReservationId);
                if (!context.Reservations.TryGetValue(sourceId, out var reservation)) continue;
                var footprint = GetFootprintIndices(reservation);
                foreach (var sectorIndex in footprint)
                {
                    if (!expectedOwners.TryAdd(sectorIndex, source))
                        errors.Add(new CorePatchGrowthError(
                            CorePatchGrowthErrorCode.InvalidSourceSiteSnapshot,
                            source.PatchId, sourceId, default(SiteReservationId), sectorIndex, 1, 0,
                            "Core source footprints must not overlap."));
                }

                if (!snapshot.TryGetPatch(source.PatchId, out var patch))
                {
                    errors.Add(new CorePatchGrowthError(
                        CorePatchGrowthErrorCode.MissingCorePatch,
                        source.PatchId, sourceId, default(SiteReservationId), -1, 1, 0,
                        "A required initialized Core patch is missing."));
                }
                else
                {
                    context.Patches[sourceId] = patch;
                    ValidatePatch(source, patch, footprint, errors);
                }

                if (!snapshot.TryGetSiteBinding(sourceId, out var binding))
                {
                    errors.Add(new CorePatchGrowthError(
                        CorePatchGrowthErrorCode.MissingCoreBinding,
                        source.PatchId, sourceId, default(SiteReservationId), -1, 1, 0,
                        "A required initialized Core site binding is missing."));
                }
                else
                {
                    context.Bindings[sourceId] = binding;
                    if (binding.PatchId != source.PatchId ||
                        !string.Equals(binding.BiomeId, source.BiomeId, StringComparison.Ordinal) ||
                        !SequenceEqual(binding.OccupiedSectorIndices, footprint))
                        errors.Add(new CorePatchGrowthError(
                            CorePatchGrowthErrorCode.InvalidCoreBinding,
                            source.PatchId, sourceId, default(SiteReservationId), -1, footprint.Count,
                            binding.OccupiedSectorIndices.Count,
                            "A Core site binding does not match its source footprint."));
                }
            }

            for (var index = 0; index < snapshot.Sectors.Count; index++)
            {
                var ownership = snapshot.Sectors[index];
                if (ownership == null || ownership.SectorIndex != index ||
                    ownership.Sector != WorldGridIndex.ToCoordinate(index))
                {
                    errors.Add(new CorePatchGrowthError(
                        CorePatchGrowthErrorCode.InvalidOwnership,
                        default(BiomePatchId), default(SiteReservationId), default(SiteReservationId),
                        index, 1, 0, "An input ownership row has invalid grid identity."));
                    continue;
                }
                if (ownership.SecondaryBiomeId == null || ownership.SecondaryBiomeId.Length != 0)
                {
                    errors.Add(new CorePatchGrowthError(
                        CorePatchGrowthErrorCode.InvalidOwnership,
                        ownership.PatchId ?? default(BiomePatchId),
                        default(SiteReservationId), default(SiteReservationId), index, 0, 0,
                        "Core growth input cannot contain a secondary biome."));
                }

                if (expectedOwners.TryGetValue(index, out var expected))
                {
                    if (!ownership.IsAssigned || !ownership.PatchId.HasValue ||
                        ownership.PatchId.Value != expected.PatchId ||
                        !string.Equals(ownership.PrimaryBiomeId, expected.BiomeId, StringComparison.Ordinal))
                        errors.Add(new CorePatchGrowthError(
                            CorePatchGrowthErrorCode.InvalidOwnership,
                            expected.PatchId, new SiteReservationId(expected.ReservationId),
                            default(SiteReservationId), index, 1, 0,
                            "A source footprint ownership row is invalid."));
                }
                else if (ownership.IsAssigned || ownership.PatchId.HasValue ||
                         !string.IsNullOrEmpty(ownership.PrimaryBiomeId) ||
                         !string.IsNullOrEmpty(ownership.SecondaryBiomeId))
                {
                    errors.Add(new CorePatchGrowthError(
                        CorePatchGrowthErrorCode.UnexpectedAssignedSector,
                        ownership.PatchId ?? default(BiomePatchId),
                        default(SiteReservationId), default(SiteReservationId), index, 0, 1,
                        "Only exact Core source footprint sectors may be assigned before growth."));
                }
            }

            if (snapshot.AssignedSectorCount != expectedOwners.Count ||
                snapshot.UnassignedSectorCount != WorldGenConstants.SectorCount - expectedOwners.Count)
                errors.Add(StructuralError(
                    CorePatchGrowthErrorCode.InvalidOwnership,
                    "Input ownership counts do not equal the exact source footprint union."));
        }

        private static void ValidatePatch(
            CoreSource source,
            BiomePatch patch,
            IReadOnlyList<int> footprint,
            ICollection<CorePatchGrowthError> errors)
        {
            var sourceId = new SiteReservationId(source.ReservationId);
            if (patch.Id != source.PatchId || patch.Role != BiomePatchRole.Core ||
                !string.Equals(patch.BiomeId, source.BiomeId, StringComparison.Ordinal) ||
                !string.Equals(patch.PatchRuleId, source.PatchRuleId, StringComparison.Ordinal) ||
                !SequenceEqual(patch.SectorIndices, footprint))
                errors.Add(new CorePatchGrowthError(
                    CorePatchGrowthErrorCode.InvalidCorePatch,
                    source.PatchId, sourceId, default(SiteReservationId), -1, footprint.Count,
                    patch.SectorCount, "An initialized Core patch has invalid identity or membership."));

            if (patch.Seeds.Count != footprint.Count)
            {
                errors.Add(new CorePatchGrowthError(
                    CorePatchGrowthErrorCode.InvalidCoreSeed,
                    source.PatchId, sourceId, default(SiteReservationId), -1, footprint.Count,
                    patch.Seeds.Count, "Core seed cells must equal the full source footprint."));
                return;
            }
            for (var index = 0; index < patch.Seeds.Count; index++)
            {
                var seed = patch.Seeds[index];
                if (seed == null || seed.SectorIndex != footprint[index] ||
                    seed.Role != BiomePatchRole.Core || !seed.SourceSiteReservationId.HasValue ||
                    seed.SourceSiteReservationId.Value != sourceId)
                    errors.Add(new CorePatchGrowthError(
                        CorePatchGrowthErrorCode.InvalidCoreSeed,
                        source.PatchId, sourceId, default(SiteReservationId),
                        seed == null ? -1 : seed.SectorIndex, 1, 0,
                        "An initialized Core seed does not match its source footprint."));
            }
        }

        private static void ValidateDefinitions(
            IEnumerable<BiomeTypeDefinition> biomeTypes,
            IEnumerable<BiomePatchRuleDefinition> patchRules,
            IReadOnlyList<CoreSource> sources,
            ValidationContext context,
            ICollection<CorePatchGrowthError> errors)
        {
            if (biomeTypes == null)
            {
                errors.Add(StructuralError(
                    CorePatchGrowthErrorCode.MissingBiomeTypes,
                    "Biome type definitions are required."));
            }
            else
            {
                foreach (var biome in biomeTypes)
                {
                    if (biome == null)
                    {
                        errors.Add(StructuralError(
                            CorePatchGrowthErrorCode.NullDefinition,
                            "Definition collections cannot contain null."));
                        continue;
                    }
                    var id = biome.BiomeId ?? string.Empty;
                    if (!ReservationValidation.IsCanonicalId(id, false))
                    {
                        errors.Add(StructuralError(
                            CorePatchGrowthErrorCode.InvalidBiomeDefinition,
                            "A biome definition has an invalid canonical ID."));
                        continue;
                    }
                    if (!context.Biomes.TryAdd(id, biome))
                        errors.Add(StructuralError(
                            CorePatchGrowthErrorCode.DuplicateDefinitionId,
                            "Definition IDs must be unique within their collection."));
                }
            }

            if (patchRules == null)
            {
                errors.Add(StructuralError(
                    CorePatchGrowthErrorCode.MissingPatchRules,
                    "Biome patch rule definitions are required."));
            }
            else
            {
                foreach (var rule in patchRules)
                {
                    if (rule == null)
                    {
                        errors.Add(StructuralError(
                            CorePatchGrowthErrorCode.NullDefinition,
                            "Definition collections cannot contain null."));
                        continue;
                    }
                    var id = rule.PatchRuleId ?? string.Empty;
                    if (!ReservationValidation.IsCanonicalId(id, false))
                    {
                        errors.Add(StructuralError(
                            CorePatchGrowthErrorCode.InvalidCorePatchRule,
                            "A patch rule definition has an invalid canonical ID."));
                        continue;
                    }
                    if (!context.Rules.TryAdd(id, rule))
                        errors.Add(StructuralError(
                            CorePatchGrowthErrorCode.DuplicateDefinitionId,
                            "Definition IDs must be unique within their collection."));
                }
            }

            var expectedBiomes = new HashSet<string>(sources.Select(item => item.BiomeId), StringComparer.Ordinal);
            var expectedRules = new HashSet<string>(sources.Select(item => item.PatchRuleId), StringComparer.Ordinal);
            foreach (var pair in context.Biomes)
                if (pair.Value.Active && pair.Value.Required && !expectedBiomes.Contains(pair.Key))
                    errors.Add(StructuralError(
                        CorePatchGrowthErrorCode.InvalidBiomeDefinition,
                        "Only the exact four frozen required biomes may be active and required."));
            foreach (var pair in context.Rules)
                if (pair.Value.Active && string.Equals(pair.Value.PatchRole, "CORE", StringComparison.Ordinal) &&
                    !expectedRules.Contains(pair.Key))
                    errors.Add(StructuralError(
                        CorePatchGrowthErrorCode.InvalidCorePatchRule,
                        "Only the exact four frozen Core rules may be active."));

            foreach (var source in sources)
            {
                var sourceId = new SiteReservationId(source.ReservationId);
                if (!context.Biomes.TryGetValue(source.BiomeId, out var biome))
                {
                    errors.Add(new CorePatchGrowthError(
                        CorePatchGrowthErrorCode.MissingBiomeDefinition,
                        source.PatchId, sourceId, default(SiteReservationId), -1, 1, 0,
                        "A required Core biome definition is missing."));
                }
                else if (!biome.Active || !biome.Required || biome.MinCorePatchCount < 1)
                {
                    errors.Add(new CorePatchGrowthError(
                        CorePatchGrowthErrorCode.InvalidBiomeDefinition,
                        source.PatchId, sourceId, default(SiteReservationId), -1, 1, 0,
                        "A required Core biome definition is inactive or invalid."));
                }

                if (!context.Rules.TryGetValue(source.PatchRuleId, out var rule))
                {
                    errors.Add(new CorePatchGrowthError(
                        CorePatchGrowthErrorCode.MissingCorePatchRule,
                        source.PatchId, sourceId, default(SiteReservationId), -1, 1, 0,
                        "A required Core patch rule is missing."));
                    continue;
                }

                if (!rule.Active || !string.Equals(rule.PatchRole, "CORE", StringComparison.Ordinal) ||
                    !string.Equals(rule.BiomeId, source.BiomeId, StringComparison.Ordinal) ||
                    rule.MinSectorCount < 1 || rule.MinSectorCount > rule.MaxSectorCount ||
                    rule.MaxSectorCount > WorldGenConstants.SectorCount ||
                    rule.BufferRingSectors < 0 || rule.BufferRingSectors > 12)
                {
                    errors.Add(new CorePatchGrowthError(
                        CorePatchGrowthErrorCode.InvalidCorePatchRule,
                        source.PatchId, sourceId, default(SiteReservationId), -1,
                        1, Math.Max(0, rule.MinSectorCount),
                        "A required Core patch rule has invalid state, role, biome, or range."));
                }

                if (context.CoreSeeds.TryGetValue(sourceId, out var seed) &&
                    (!string.Equals(seed.BiomeId, rule.BiomeId, StringComparison.Ordinal) ||
                     !string.Equals(seed.CorePatchRuleId, rule.PatchRuleId, StringComparison.Ordinal)))
                    errors.Add(new CorePatchGrowthError(
                        CorePatchGrowthErrorCode.DefinitionIdentityMismatch,
                        source.PatchId, sourceId, default(SiteReservationId),
                        WorldGridIndex.ToIndex(seed.SeedSector), 1, 0,
                        "Core seed and typed rule identities must match exactly."));
            }
        }

        private static List<CoreWork> BuildWorks(
            IReadOnlyList<CoreSource> sources,
            ValidationContext context)
        {
            var works = new List<CoreWork>(sources.Count);
            foreach (var source in sources)
            {
                var sourceId = new SiteReservationId(source.ReservationId);
                works.Add(new CoreWork(
                    source,
                    context.Reservations[sourceId],
                    context.Patches[sourceId],
                    context.Bindings[sourceId],
                    context.Rules[source.PatchRuleId]));
            }
            works.Sort((left, right) => left.SourceId.CompareTo(right.SourceId));
            return works;
        }

        private static void BuildMandatoryBuffer(CoreWork work)
        {
            var outside = new HashSet<GridPoint>();
            foreach (var sectorIndex in work.Footprint)
            {
                var origin = WorldGridIndex.ToCoordinate(sectorIndex);
                for (var deltaX = -work.Rule.BufferRingSectors;
                     deltaX <= work.Rule.BufferRingSectors; deltaX++)
                {
                    var vertical = work.Rule.BufferRingSectors - Math.Abs(deltaX);
                    for (var deltaY = -vertical; deltaY <= vertical; deltaY++)
                    {
                        var x = origin.X + deltaX;
                        var y = origin.Y + deltaY;
                        if (x < 0 || x >= WorldGenConstants.SectorColumns ||
                            y < 0 || y >= WorldGenConstants.SectorRows)
                            outside.Add(new GridPoint(x, y));
                        else
                            work.MandatoryBuffer.Add(WorldGridIndex.ToIndex(new SectorCoord(x, y)));
                    }
                }
            }
            work.OutsideTheoreticalBufferCount = outside.Count;
            work.TargetSectorCount = Math.Max(work.Rule.MinSectorCount, work.MandatoryBuffer.Count);
        }

        private static List<CorePatchGrowthError> ValidateSpatialGates(
            IReadOnlyList<CoreWork> works,
            SiteReservationSnapshot sourceSnapshot)
        {
            var errors = new List<CorePatchGrowthError>();
            foreach (var work in works)
            {
                if (work.OutsideTheoreticalBufferCount > 0 && !work.Rule.CanTouchWorldEdge)
                    errors.Add(Error(
                        CorePatchGrowthErrorCode.BufferOutsideWorld,
                        work,
                        default(SiteReservationId),
                        -1,
                        work.MandatoryBuffer.Count + work.OutsideTheoreticalBufferCount,
                        work.MandatoryBuffer.Count,
                        "The mandatory Core buffer extends outside the world."));

                foreach (var sectorIndex in work.MandatoryBuffer)
                {
                    var reservation = sourceSnapshot.GetSector(sectorIndex);
                    if (reservation.IsReserved &&
                        (!reservation.ReservationId.HasValue || reservation.ReservationId.Value != work.SourceId))
                        errors.Add(Error(
                            CorePatchGrowthErrorCode.BufferBlockedByReservation,
                            work,
                            reservation.ReservationId ?? default(SiteReservationId),
                            sectorIndex,
                            1,
                            0,
                            "The mandatory Core buffer is blocked by another P01 reservation."));
                }
            }

            for (var first = 0; first < works.Count; first++)
            {
                for (var second = first + 1; second < works.Count; second++)
                {
                    foreach (var sectorIndex in Intersection(
                                 works[first].MandatoryBuffer,
                                 works[second].MandatoryBuffer))
                    {
                        errors.Add(Error(
                            CorePatchGrowthErrorCode.MandatoryBufferConflict,
                            works[first],
                            works[second].SourceId,
                            sectorIndex,
                            1,
                            0,
                            "Two Core mandatory buffers claim the same sector."));
                        errors.Add(Error(
                            CorePatchGrowthErrorCode.MandatoryBufferConflict,
                            works[second],
                            works[first].SourceId,
                            sectorIndex,
                            1,
                            0,
                            "Two Core mandatory buffers claim the same sector."));
                    }
                }
            }
            return errors;
        }

        private static CorePatchGrowthResult ExecuteGrowth(
            CorePatchInitializationPublication initialization,
            IReadOnlyList<CoreWork> works)
        {
            var ownerBySector = new int[WorldGenConstants.SectorCount];
            for (var index = 0; index < ownerBySector.Length; index++) ownerBySector[index] = -1;

            for (var owner = 0; owner < works.Count; owner++)
            {
                foreach (var sectorIndex in works[owner].Footprint)
                {
                    ownerBySector[sectorIndex] = owner;
                    works[owner].FinalSectors.Add(sectorIndex);
                }
            }

            for (var owner = 0; owner < works.Count; owner++)
            {
                foreach (var sectorIndex in works[owner].MandatoryBuffer)
                {
                    if (ownerBySector[sectorIndex] < 0)
                    {
                        ownerBySector[sectorIndex] = owner;
                        works[owner].FinalSectors.Add(sectorIndex);
                        works[owner].MandatoryAdded.Add(sectorIndex);
                    }
                }
            }

            while (works.Any(work => work.FinalSectors.Count < work.TargetSectorCount))
            {
                var proposals = new List<Proposal>();
                foreach (var work in works)
                {
                    if (work.FinalSectors.Count >= work.TargetSectorCount) continue;
                    work.GrowthRoundCount++;
                    var proposal = SelectProposal(work, ownerBySector, initialization.SourceSiteSnapshot);
                    if (proposal != null) proposals.Add(proposal);
                }
                proposals.Sort(Proposal.Compare);

                var successful = 0;
                foreach (var proposal in proposals)
                {
                    if (proposal.Work.FinalSectors.Count >= proposal.Work.TargetSectorCount ||
                        ownerBySector[proposal.SectorIndex] >= 0)
                        continue;
                    var owner = IndexOf(works, proposal.Work);
                    ownerBySector[proposal.SectorIndex] = owner;
                    proposal.Work.FinalSectors.Add(proposal.SectorIndex);
                    proposal.Work.SupplementalAdded.Add(proposal.SectorIndex);
                    successful++;
                }

                if (successful == 0)
                {
                    var errors = new List<CorePatchGrowthError>();
                    foreach (var work in works)
                        if (work.FinalSectors.Count < work.TargetSectorCount)
                            errors.Add(Error(
                                CorePatchGrowthErrorCode.InsufficientUnreservedCapacity,
                                work,
                                default(SiteReservationId),
                                -1,
                                work.TargetSectorCount,
                                work.FinalSectors.Count,
                                "No eligible unreserved frontier can satisfy the Core minimum."));
                    return CorePatchGrowthResult.Retry(
                        BuildRollbackDiagnostics(initialization, works.Count), errors);
                }
            }

            return BuildCompleted(initialization, works, ownerBySector);
        }

        private static Proposal SelectProposal(
            CoreWork work,
            IReadOnlyList<int> ownerBySector,
            SiteReservationSnapshot sourceSnapshot)
        {
            var candidates = new HashSet<int>();
            foreach (var sectorIndex in work.FinalSectors)
                foreach (var neighbor in GetNeighbors(sectorIndex))
                    candidates.Add(neighbor);

            Proposal best = null;
            foreach (var sectorIndex in candidates)
            {
                if (ownerBySector[sectorIndex] >= 0 || sourceSnapshot.GetSector(sectorIndex).IsReserved)
                    continue;
                var ownNeighbors = 0;
                foreach (var neighbor in GetNeighbors(sectorIndex))
                    if (work.FinalSectors.Contains(neighbor)) ownNeighbors++;
                var candidate = new Proposal(
                    work,
                    sectorIndex,
                    FootprintDistance(sectorIndex, work.Footprint),
                    4 - 2 * ownNeighbors);
                if (best == null || Proposal.Compare(candidate, best) < 0) best = candidate;
            }
            return best;
        }

        private static CorePatchGrowthResult BuildCompleted(
            CorePatchInitializationPublication initialization,
            IReadOnlyList<CoreWork> works,
            IReadOnlyList<int> ownerBySector)
        {
            var patches = new List<BiomePatch>(works.Count);
            var bindings = new List<BiomePatchSiteBinding>(works.Count);
            var records = new List<CorePatchGrowthRecord>(works.Count);
            var ownership = new List<BiomeSectorOwnership>(WorldGenConstants.SectorCount);
            var mandatoryAdded = 0;
            var supplementalAdded = 0;

            foreach (var work in works)
            {
                var added = new SortedSet<int>(work.MandatoryAdded);
                added.UnionWith(work.SupplementalAdded);
                records.Add(new CorePatchGrowthRecord(
                    work.Patch.Id,
                    work.SourceId,
                    work.Patch.BiomeId,
                    work.Patch.PatchRuleId,
                    work.Footprint.Count,
                    work.OutsideTheoreticalBufferCount,
                    work.Rule.MinSectorCount,
                    work.Rule.MaxSectorCount,
                    work.TargetSectorCount,
                    work.MandatoryAdded.Count,
                    work.SupplementalAdded.Count,
                    work.GrowthRoundCount,
                    work.Footprint,
                    work.MandatoryBuffer,
                    added,
                    work.FinalSectors));
                patches.Add(new BiomePatch(
                    work.Patch.Id,
                    work.Patch.BiomeId,
                    work.Patch.PatchRuleId,
                    BiomePatchRole.Core,
                    work.Patch.Seeds,
                    work.FinalSectors));
                bindings.Add(work.Binding);
                mandatoryAdded += work.MandatoryAdded.Count;
                supplementalAdded += work.SupplementalAdded.Count;
            }

            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var coordinate = WorldGridIndex.ToCoordinate(index);
                var owner = ownerBySector[index];
                if (owner < 0)
                    ownership.Add(BiomeSectorOwnership.CreateUnassigned(index, coordinate));
                else
                    ownership.Add(new BiomeSectorOwnership(
                        index,
                        coordinate,
                        works[owner].Patch.BiomeId,
                        string.Empty,
                        works[owner].Patch.Id));
            }

            var snapshot = new BiomePatchSnapshot(
                initialization.Snapshot.Seed,
                patches,
                ownership,
                bindings);
            var publication = new CorePatchGrowthPublication(initialization, snapshot, records);
            var diagnostics = new CorePatchGrowthDiagnostics(
                snapshot.Seed,
                records,
                works.Count,
                initialization.Snapshot.AssignedSectorCount,
                mandatoryAdded,
                supplementalAdded,
                snapshot.AssignedSectorCount,
                snapshot.UnassignedSectorCount,
                CountReserved(initialization.SourceSiteSnapshot),
                0,
                0);
            return CorePatchGrowthResult.Completed(publication, diagnostics);
        }

        private static CorePatchGrowthDiagnostics BuildRollbackDiagnostics(
            CorePatchInitializationPublication initialization,
            int corePatchCount)
        {
            return new CorePatchGrowthDiagnostics(
                initialization.Snapshot.Seed,
                Array.Empty<CorePatchGrowthRecord>(),
                corePatchCount,
                initialization.Snapshot.AssignedSectorCount,
                0,
                0,
                initialization.Snapshot.AssignedSectorCount,
                initialization.Snapshot.UnassignedSectorCount,
                CountReserved(initialization.SourceSiteSnapshot),
                0,
                0);
        }

        private static int CountReserved(SiteReservationSnapshot snapshot)
        {
            var count = 0;
            foreach (var sector in snapshot.Sectors) if (sector.IsReserved) count++;
            return count;
        }

        private static int FootprintDistance(int sectorIndex, IEnumerable<int> footprint)
        {
            var coordinate = WorldGridIndex.ToCoordinate(sectorIndex);
            var best = int.MaxValue;
            foreach (var footprintIndex in footprint)
            {
                var origin = WorldGridIndex.ToCoordinate(footprintIndex);
                best = Math.Min(best, Math.Abs(coordinate.X - origin.X) + Math.Abs(coordinate.Y - origin.Y));
            }
            return best;
        }

        private static IReadOnlyList<int> GetNeighbors(int sectorIndex)
        {
            var values = new List<int>(4);
            AddNeighbor(values, WorldGridIndex.GetLeftIndex(sectorIndex));
            AddNeighbor(values, WorldGridIndex.GetRightIndex(sectorIndex));
            AddNeighbor(values, WorldGridIndex.GetUpIndex(sectorIndex));
            AddNeighbor(values, WorldGridIndex.GetDownIndex(sectorIndex));
            values.Sort();
            return values;
        }

        private static void AddNeighbor(ICollection<int> values, int sectorIndex)
        {
            if (sectorIndex != SectorNeighborIndices.NoNeighbor) values.Add(sectorIndex);
        }

        private static List<int> GetFootprintIndices(SiteReservation reservation)
        {
            var values = new List<int>();
            foreach (var sector in reservation.OccupiedSectors) values.Add(WorldGridIndex.ToIndex(sector));
            values.Sort();
            return values;
        }

        private static bool IsCardinallyConnected(IReadOnlyList<int> sectors)
        {
            if (sectors.Count == 0) return false;
            var set = new HashSet<int>(sectors);
            var visited = new HashSet<int> { sectors[0] };
            var queue = new Queue<int>();
            queue.Enqueue(sectors[0]);
            while (queue.Count != 0)
                foreach (var neighbor in GetNeighbors(queue.Dequeue()))
                    if (set.Contains(neighbor) && visited.Add(neighbor)) queue.Enqueue(neighbor);
            return visited.Count == set.Count;
        }

        private static bool SequenceEqual(IReadOnlyList<int> left, IReadOnlyList<int> right)
        {
            if (left.Count != right.Count) return false;
            for (var index = 0; index < left.Count; index++)
                if (left[index] != right[index]) return false;
            return true;
        }

        private static IEnumerable<int> Intersection(SortedSet<int> left, SortedSet<int> right)
        {
            foreach (var value in left) if (right.Contains(value)) yield return value;
        }

        private static int IndexOf(IReadOnlyList<CoreWork> works, CoreWork work)
        {
            for (var index = 0; index < works.Count; index++) if (ReferenceEquals(works[index], work)) return index;
            throw new InvalidOperationException("Growth work owner is missing.");
        }

        private static CorePatchGrowthError StructuralError(
            CorePatchGrowthErrorCode code,
            string message)
        {
            return new CorePatchGrowthError(
                code,
                default(BiomePatchId),
                default(SiteReservationId),
                default(SiteReservationId),
                -1,
                0,
                0,
                message);
        }

        private static CorePatchGrowthError Error(
            CorePatchGrowthErrorCode code,
            CoreWork work,
            SiteReservationId otherSourceId,
            int sectorIndex,
            int requiredCount,
            int availableCount,
            string message)
        {
            return new CorePatchGrowthError(
                code,
                work.Patch.Id,
                work.SourceId,
                otherSourceId,
                sectorIndex,
                requiredCount,
                availableCount,
                message);
        }

        private static IReadOnlyList<CoreSource> CreateCoreSources()
        {
            return new[]
            {
                new CoreSource(2, "RSV_02_SITE_MOON_SEAL_FORGE", "SITE_MOON_SEAL_FORGE",
                    SiteReservationKind.Forge, "BIO_ABANDONED_MILL", "PATCH_MILL_CORE", 4, 14, 1, false),
                new CoreSource(3, "RSV_03_SITE_CASSIA_SAP_HEART", "SITE_CASSIA_SAP_HEART",
                    SiteReservationKind.CoreResource, "BIO_CASSIA_ROOT", "PATCH_ROOT_CORE", 5, 18, 1, false),
                new CoreSource(4, "RSV_04_SITE_DEEP_STAR_YEAST", "SITE_DEEP_STAR_YEAST",
                    SiteReservationKind.CoreResource, "BIO_MOON_DOUGH", "PATCH_DOUGH_CORE", 5, 18, 1, true),
                new CoreSource(5, "RSV_05_SITE_MOON_CORE_METEOR", "SITE_MOON_CORE_METEOR",
                    SiteReservationKind.CoreResource, "BIO_MOON_CRATER", "PATCH_CRATER_CORE", 5, 18, 1, true)
            };
        }

        private static IReadOnlyList<ReservationSource> CreateReservationSources()
        {
            return new[]
            {
                new ReservationSource(0, "RSV_00_WORLD_MOONPALACE_V1", "WORLD_MOONPALACE_V1", SiteReservationKind.Start),
                new ReservationSource(1, "RSV_01_SITE_MOON_BOSS_VAULT", "SITE_MOON_BOSS_VAULT", SiteReservationKind.Boss),
                new ReservationSource(2, "RSV_02_SITE_MOON_SEAL_FORGE", "SITE_MOON_SEAL_FORGE", SiteReservationKind.Forge),
                new ReservationSource(3, "RSV_03_SITE_CASSIA_SAP_HEART", "SITE_CASSIA_SAP_HEART", SiteReservationKind.CoreResource),
                new ReservationSource(4, "RSV_04_SITE_DEEP_STAR_YEAST", "SITE_DEEP_STAR_YEAST", SiteReservationKind.CoreResource),
                new ReservationSource(5, "RSV_05_SITE_MOON_CORE_METEOR", "SITE_MOON_CORE_METEOR", SiteReservationKind.CoreResource),
                new ReservationSource(6, "RSV_06_SITE_PRIMARY_VILLAGE", "SITE_PRIMARY_VILLAGE", SiteReservationKind.Village)
            };
        }

        private sealed class ValidationContext
        {
            public CorePatchInitializationPublication Initialization { get; set; }
            public SiteReservationSnapshot SourceSiteSnapshot { get; set; }
            public BiomePatchSnapshot InputSnapshot { get; set; }
            public Dictionary<SiteReservationId, SiteReservation> Reservations { get; } =
                new Dictionary<SiteReservationId, SiteReservation>();
            public Dictionary<SiteReservationId, CoreBiomeSeed> CoreSeeds { get; } =
                new Dictionary<SiteReservationId, CoreBiomeSeed>();
            public Dictionary<SiteReservationId, BiomePatch> Patches { get; } =
                new Dictionary<SiteReservationId, BiomePatch>();
            public Dictionary<SiteReservationId, BiomePatchSiteBinding> Bindings { get; } =
                new Dictionary<SiteReservationId, BiomePatchSiteBinding>();
            public Dictionary<string, BiomeTypeDefinition> Biomes { get; } =
                new Dictionary<string, BiomeTypeDefinition>(StringComparer.Ordinal);
            public Dictionary<string, BiomePatchRuleDefinition> Rules { get; } =
                new Dictionary<string, BiomePatchRuleDefinition>(StringComparer.Ordinal);
        }

        private sealed class CoreSource
        {
            public CoreSource(
                int order,
                string reservationId,
                string sourceDefinitionId,
                SiteReservationKind kind,
                string biomeId,
                string patchRuleId,
                int minimumSectorCount,
                int maximumSectorCount,
                int bufferRingSectors,
                bool canTouchWorldEdge)
            {
                Order = order;
                ReservationId = reservationId;
                SourceDefinitionId = sourceDefinitionId;
                Kind = kind;
                BiomeId = biomeId;
                PatchRuleId = patchRuleId;
                MinimumSectorCount = minimumSectorCount;
                MaximumSectorCount = maximumSectorCount;
                BufferRingSectors = bufferRingSectors;
                CanTouchWorldEdge = canTouchWorldEdge;
                PatchId = new BiomePatchId("PATCHINST_CORE_" + reservationId);
            }

            public int Order { get; }
            public string ReservationId { get; }
            public string SourceDefinitionId { get; }
            public SiteReservationKind Kind { get; }
            public string BiomeId { get; }
            public string PatchRuleId { get; }
            public int MinimumSectorCount { get; }
            public int MaximumSectorCount { get; }
            public int BufferRingSectors { get; }
            public bool CanTouchWorldEdge { get; }
            public BiomePatchId PatchId { get; }
        }

        private sealed class ReservationSource
        {
            public ReservationSource(int order, string reservationId, string sourceDefinitionId, SiteReservationKind kind)
            {
                Order = order;
                ReservationId = reservationId;
                SourceDefinitionId = sourceDefinitionId;
                Kind = kind;
            }

            public int Order { get; }
            public string ReservationId { get; }
            public string SourceDefinitionId { get; }
            public SiteReservationKind Kind { get; }
        }

        private sealed class CoreWork
        {
            public CoreWork(
                CoreSource source,
                SiteReservation reservation,
                BiomePatch patch,
                BiomePatchSiteBinding binding,
                BiomePatchRuleDefinition rule)
            {
                Source = source;
                SourceId = new SiteReservationId(source.ReservationId);
                Reservation = reservation;
                Patch = patch;
                Binding = binding;
                Rule = rule;
                Footprint = GetFootprintIndices(reservation);
            }

            public CoreSource Source { get; }
            public SiteReservationId SourceId { get; }
            public SiteReservation Reservation { get; }
            public BiomePatch Patch { get; }
            public BiomePatchSiteBinding Binding { get; }
            public BiomePatchRuleDefinition Rule { get; }
            public IReadOnlyList<int> Footprint { get; }
            public SortedSet<int> MandatoryBuffer { get; } = new SortedSet<int>();
            public SortedSet<int> MandatoryAdded { get; } = new SortedSet<int>();
            public SortedSet<int> SupplementalAdded { get; } = new SortedSet<int>();
            public SortedSet<int> FinalSectors { get; } = new SortedSet<int>();
            public int OutsideTheoreticalBufferCount { get; set; }
            public int TargetSectorCount { get; set; }
            public int GrowthRoundCount { get; set; }
        }

        private sealed class Proposal
        {
            public Proposal(CoreWork work, int sectorIndex, int footprintDistance, int exposedPerimeterDelta)
            {
                Work = work;
                SectorIndex = sectorIndex;
                FootprintDistance = footprintDistance;
                ExposedPerimeterDelta = exposedPerimeterDelta;
            }

            public CoreWork Work { get; }
            public int SectorIndex { get; }
            public int FootprintDistance { get; }
            public int ExposedPerimeterDelta { get; }

            public static int Compare(Proposal left, Proposal right)
            {
                var value = left.FootprintDistance.CompareTo(right.FootprintDistance);
                if (value != 0) return value;
                value = left.ExposedPerimeterDelta.CompareTo(right.ExposedPerimeterDelta);
                if (value != 0) return value;
                value = left.Work.SourceId.CompareTo(right.Work.SourceId);
                if (value != 0) return value;
                return left.SectorIndex.CompareTo(right.SectorIndex);
            }
        }

        private readonly struct GridPoint : IEquatable<GridPoint>
        {
            public GridPoint(int x, int y)
            {
                X = x;
                Y = y;
            }

            public int X { get; }
            public int Y { get; }

            public bool Equals(GridPoint other) => X == other.X && Y == other.Y;
            public override bool Equals(object obj) => obj is GridPoint other && Equals(other);
            public override int GetHashCode()
            {
                unchecked { return (X * 397) ^ Y; }
            }
        }
    }
}
