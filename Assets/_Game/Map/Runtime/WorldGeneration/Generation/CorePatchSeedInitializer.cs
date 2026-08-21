using System;
using System.Collections.Generic;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class CorePatchSeedInitializer
    {
        public CorePatchInitializationResult Initialize(
            SiteReservationSnapshot siteSnapshot,
            IEnumerable<BiomeTypeDefinition> biomeTypes,
            IEnumerable<BiomePatchRuleDefinition> patchRules)
        {
            try
            {
                var errors = new List<CorePatchInitializationError>();
                var sources = CreateCoreSources();
                ValidateSiteSnapshot(siteSnapshot, sources, errors,
                    out var reservations, out var seeds);
                ValidateDefinitions(biomeTypes, patchRules, sources, seeds, errors,
                    out var biomeLookup, out var ruleLookup);
                ValidateGeneratedPatchIds(sources, errors);

                if (errors.Count != 0)
                    return CorePatchInitializationResult.Invalid(errors);

                return Build(siteSnapshot, sources, reservations, seeds, biomeLookup, ruleLookup);
            }
            catch
            {
                return CorePatchInitializationResult.Invalid(new[]
                {
                    Error(
                        CorePatchInitializationErrorCode.InternalInvariantViolation,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        -1,
                        "Core patch initialization violated an internal model invariant.")
                });
            }
        }

        private static CorePatchInitializationResult Build(
            SiteReservationSnapshot siteSnapshot,
            IReadOnlyList<CoreSource> sources,
            IReadOnlyDictionary<SiteReservationId, SiteReservation> reservations,
            IReadOnlyDictionary<SiteReservationId, CoreBiomeSeed> seeds,
            IReadOnlyDictionary<string, BiomeTypeDefinition> biomeLookup,
            IReadOnlyDictionary<string, BiomePatchRuleDefinition> ruleLookup)
        {
            var factory = new CorePatchIdFactory();
            var patches = new List<BiomePatch>(sources.Count);
            var bindings = new List<BiomePatchSiteBinding>(sources.Count);
            var patchIds = new List<BiomePatchId>(sources.Count);
            var sourceIds = new List<SiteReservationId>(sources.Count);
            var ownership = new BiomeSectorOwnership[WorldGenConstants.SectorCount];
            for (var index = 0; index < ownership.Length; index++)
                ownership[index] = BiomeSectorOwnership.CreateUnassigned(
                    index, WorldGridIndex.ToCoordinate(index));

            var seedCellCount = 0;
            foreach (var source in sources)
            {
                var sourceId = new SiteReservationId(source.ReservationId);
                var reservation = reservations[sourceId];
                var coreSeed = seeds[sourceId];
                var patchId = factory.CreateCorePatchId(sourceId);
                var indices = GetFootprintIndices(reservation);
                var patchSeeds = new List<BiomePatchSeed>(indices.Count);
                foreach (var sectorIndex in indices)
                {
                    var coordinate = WorldGridIndex.ToCoordinate(sectorIndex);
                    patchSeeds.Add(new BiomePatchSeed(
                        sectorIndex, coordinate, BiomePatchRole.Core, sourceId));
                    ownership[sectorIndex] = new BiomeSectorOwnership(
                        sectorIndex,
                        coordinate,
                        coreSeed.BiomeId,
                        string.Empty,
                        patchId);
                }

                patches.Add(new BiomePatch(
                    patchId,
                    biomeLookup[source.BiomeId].BiomeId,
                    ruleLookup[source.PatchRuleId].PatchRuleId,
                    BiomePatchRole.Core,
                    patchSeeds,
                    indices));
                bindings.Add(new BiomePatchSiteBinding(
                    sourceId,
                    patchId,
                    source.BiomeId,
                    indices));
                patchIds.Add(patchId);
                sourceIds.Add(sourceId);
                seedCellCount += indices.Count;
            }

            var snapshot = new BiomePatchSnapshot(
                siteSnapshot.Seed,
                patches,
                ownership,
                bindings);
            var publication = new CorePatchInitializationPublication(
                siteSnapshot,
                snapshot,
                patchIds);
            var diagnostics = new CorePatchInitializationDiagnostics(
                siteSnapshot.Seed,
                sources.Count,
                siteSnapshot.CoreBiomeSeeds.Count,
                patches.Count,
                seedCellCount,
                bindings.Count,
                snapshot.AssignedSectorCount,
                snapshot.UnassignedSectorCount,
                sourceIds,
                patchIds);
            return CorePatchInitializationResult.Completed(publication, diagnostics);
        }

        private static void ValidateSiteSnapshot(
            SiteReservationSnapshot snapshot,
            IReadOnlyList<CoreSource> sources,
            ICollection<CorePatchInitializationError> errors,
            out Dictionary<SiteReservationId, SiteReservation> reservations,
            out Dictionary<SiteReservationId, CoreBiomeSeed> seeds)
        {
            reservations = new Dictionary<SiteReservationId, SiteReservation>();
            seeds = new Dictionary<SiteReservationId, CoreBiomeSeed>();
            if (snapshot == null)
            {
                errors.Add(Error(
                    CorePatchInitializationErrorCode.MissingSiteSnapshot,
                    string.Empty, string.Empty, string.Empty, -1,
                    "A final site reservation snapshot is required."));
                return;
            }

            if (snapshot.Reservations.Count != 7 ||
                snapshot.Sectors.Count != WorldGenConstants.SectorCount)
            {
                errors.Add(Error(
                    CorePatchInitializationErrorCode.InvalidSiteSnapshot,
                    string.Empty, string.Empty, string.Empty, -1,
                    "The site snapshot must contain exactly 7 reservations and 169 sectors."));
            }
            if (snapshot.CoreBiomeSeeds.Count != sources.Count)
            {
                errors.Add(Error(
                    CorePatchInitializationErrorCode.InvalidCoreSeedSet,
                    string.Empty, string.Empty, string.Empty, -1,
                    "The site snapshot must contain exactly four Core biome seeds."));
            }

            foreach (var reservation in snapshot.Reservations)
            {
                if (reservation == null)
                {
                    errors.Add(Error(
                        CorePatchInitializationErrorCode.InvalidReservationSet,
                        string.Empty, string.Empty, string.Empty, -1,
                        "Site reservations cannot contain null."));
                    continue;
                }
                if (!reservations.ContainsKey(reservation.ReservationId))
                    reservations.Add(reservation.ReservationId, reservation);
            }

            var expectedReservations = CreateReservationSources();
            var expectedIds = new HashSet<SiteReservationId>();
            foreach (var expected in expectedReservations)
            {
                var id = new SiteReservationId(expected.ReservationId);
                expectedIds.Add(id);
                if (!reservations.TryGetValue(id, out var reservation))
                {
                    errors.Add(Error(
                        CorePatchInitializationErrorCode.InvalidReservationSet,
                        expected.ReservationId, expected.BiomeId, expected.PatchRuleId, -1,
                        "A required canonical site reservation is missing."));
                    continue;
                }
                if (reservation.Kind != expected.Kind ||
                    reservation.ReservationOrder != expected.Order ||
                    !string.Equals(reservation.SourceDefinitionId, expected.SourceDefinitionId, StringComparison.Ordinal))
                {
                    errors.Add(Error(
                        CorePatchInitializationErrorCode.InvalidReservationSet,
                        expected.ReservationId, expected.BiomeId, expected.PatchRuleId, -1,
                        "A canonical site reservation has mismatched identity, kind, or order."));
                }
            }
            foreach (var pair in reservations)
                if (!expectedIds.Contains(pair.Key))
                    errors.Add(Error(
                        CorePatchInitializationErrorCode.InvalidReservationSet,
                        pair.Key.Value, string.Empty, string.Empty, -1,
                        "An unexpected site reservation is present."));

            foreach (var coreSeed in snapshot.CoreBiomeSeeds)
            {
                if (coreSeed == null)
                {
                    errors.Add(Error(
                        CorePatchInitializationErrorCode.NullCoreSeed,
                        string.Empty, string.Empty, string.Empty, -1,
                        "Core biome seeds cannot contain null."));
                    continue;
                }
                if (seeds.ContainsKey(coreSeed.SourceReservationId))
                {
                    errors.Add(Error(
                        CorePatchInitializationErrorCode.DuplicateCoreSeedSource,
                        coreSeed.SourceReservationId.Value,
                        coreSeed.BiomeId,
                        coreSeed.CorePatchRuleId,
                        WorldGridIndex.ToIndex(coreSeed.SeedSector),
                        "A Core source reservation can provide only one Core biome seed."));
                }
                else
                {
                    seeds.Add(coreSeed.SourceReservationId, coreSeed);
                }
            }

            var occupiedSources = new Dictionary<int, SiteReservationId>();
            foreach (var source in sources)
            {
                var sourceId = new SiteReservationId(source.ReservationId);
                if (!seeds.TryGetValue(sourceId, out var coreSeed))
                {
                    errors.Add(Error(
                        CorePatchInitializationErrorCode.MissingRequiredCoreSeed,
                        source.ReservationId, source.BiomeId, source.PatchRuleId, -1,
                        "A required Core biome seed is missing."));
                }
                if (!reservations.TryGetValue(sourceId, out var reservation))
                {
                    errors.Add(Error(
                        CorePatchInitializationErrorCode.MissingSourceReservation,
                        source.ReservationId, source.BiomeId, source.PatchRuleId, -1,
                        "The Core biome seed source reservation is missing."));
                    continue;
                }

                ValidateCoreReservation(source, reservation, coreSeed, occupiedSources, errors);
            }

            var requiredIds = new HashSet<SiteReservationId>();
            foreach (var source in sources)
                requiredIds.Add(new SiteReservationId(source.ReservationId));
            foreach (var pair in seeds)
                if (!requiredIds.Contains(pair.Key))
                    errors.Add(Error(
                        CorePatchInitializationErrorCode.UnexpectedCoreSeed,
                        pair.Key.Value,
                        pair.Value.BiomeId,
                        pair.Value.CorePatchRuleId,
                        WorldGridIndex.ToIndex(pair.Value.SeedSector),
                        "An unexpected Core biome seed is present."));
        }

        private static void ValidateCoreReservation(
            CoreSource source,
            SiteReservation reservation,
            CoreBiomeSeed coreSeed,
            IDictionary<int, SiteReservationId> occupiedSources,
            ICollection<CorePatchInitializationError> errors)
        {
            if ((reservation.Kind != SiteReservationKind.Forge &&
                 reservation.Kind != SiteReservationKind.CoreResource) ||
                !string.Equals(reservation.PrimaryBiomeId, source.BiomeId, StringComparison.Ordinal) ||
                reservation.OccupiedSectors.Count == 0)
            {
                errors.Add(Error(
                    CorePatchInitializationErrorCode.InvalidSourceReservation,
                    source.ReservationId, source.BiomeId, source.PatchRuleId, -1,
                    "A Core source reservation has invalid kind, biome, or footprint."));
            }

            var indices = GetFootprintIndices(reservation);
            foreach (var sectorIndex in indices)
            {
                if (occupiedSources.TryGetValue(sectorIndex, out var previous))
                {
                    errors.Add(Error(
                        CorePatchInitializationErrorCode.SourceFootprintOverlap,
                        source.ReservationId, source.BiomeId, source.PatchRuleId, sectorIndex,
                        "Core source footprints must not overlap."));
                }
                else
                {
                    occupiedSources.Add(sectorIndex, new SiteReservationId(source.ReservationId));
                }
            }

            if (coreSeed == null) return;
            var seedIndex = WorldGridIndex.ToIndex(coreSeed.SeedSector);
            if (indices.Count == 0 || !Contains(indices, seedIndex) || seedIndex != indices[0])
            {
                errors.Add(Error(
                    CorePatchInitializationErrorCode.SeedOutsideSourceFootprint,
                    source.ReservationId, coreSeed.BiomeId, coreSeed.CorePatchRuleId, seedIndex,
                    "The Core seed sector must be the smallest source footprint sector."));
            }
            if (!string.Equals(coreSeed.BiomeId, source.BiomeId, StringComparison.Ordinal) ||
                !string.Equals(coreSeed.CorePatchRuleId, source.PatchRuleId, StringComparison.Ordinal) ||
                coreSeed.MinimumCoreSectorCount != source.MinimumSectorCount ||
                coreSeed.BufferRingSectors != source.BufferRingSectors)
            {
                errors.Add(Error(
                    CorePatchInitializationErrorCode.InvalidCoreSeedSet,
                    source.ReservationId, coreSeed.BiomeId, coreSeed.CorePatchRuleId, seedIndex,
                    "A Core biome seed does not match its frozen source mapping."));
            }
        }

        private static void ValidateDefinitions(
            IEnumerable<BiomeTypeDefinition> biomeTypes,
            IEnumerable<BiomePatchRuleDefinition> patchRules,
            IReadOnlyList<CoreSource> sources,
            IReadOnlyDictionary<SiteReservationId, CoreBiomeSeed> seeds,
            ICollection<CorePatchInitializationError> errors,
            out Dictionary<string, BiomeTypeDefinition> biomes,
            out Dictionary<string, BiomePatchRuleDefinition> rules)
        {
            biomes = new Dictionary<string, BiomeTypeDefinition>(StringComparer.Ordinal);
            rules = new Dictionary<string, BiomePatchRuleDefinition>(StringComparer.Ordinal);

            if (biomeTypes == null)
            {
                errors.Add(Error(
                    CorePatchInitializationErrorCode.MissingBiomeTypes,
                    string.Empty, string.Empty, string.Empty, -1,
                    "Biome type definitions are required."));
            }
            else
            {
                foreach (var biome in biomeTypes)
                {
                    if (biome == null)
                    {
                        errors.Add(Error(
                            CorePatchInitializationErrorCode.NullBiomeType,
                            string.Empty, string.Empty, string.Empty, -1,
                            "Biome type definitions cannot contain null."));
                        continue;
                    }
                    var id = biome.BiomeId ?? string.Empty;
                    if (!ReservationValidation.IsCanonicalId(id, false))
                    {
                        errors.Add(Error(
                            CorePatchInitializationErrorCode.InvalidBiomeType,
                            string.Empty, id, string.Empty, -1,
                            "A biome type has an invalid canonical ID."));
                        continue;
                    }
                    if (biomes.ContainsKey(id))
                    {
                        errors.Add(Error(
                            CorePatchInitializationErrorCode.DuplicateBiomeTypeId,
                            string.Empty, id, string.Empty, -1,
                            "Biome type IDs must be unique."));
                    }
                    else
                    {
                        biomes.Add(id, biome);
                    }
                }
            }

            if (patchRules == null)
            {
                errors.Add(Error(
                    CorePatchInitializationErrorCode.MissingPatchRules,
                    string.Empty, string.Empty, string.Empty, -1,
                    "Biome patch rule definitions are required."));
            }
            else
            {
                foreach (var rule in patchRules)
                {
                    if (rule == null)
                    {
                        errors.Add(Error(
                            CorePatchInitializationErrorCode.NullPatchRule,
                            string.Empty, string.Empty, string.Empty, -1,
                            "Biome patch rule definitions cannot contain null."));
                        continue;
                    }
                    var id = rule.PatchRuleId ?? string.Empty;
                    if (!ReservationValidation.IsCanonicalId(id, false))
                    {
                        errors.Add(Error(
                            CorePatchInitializationErrorCode.InvalidPatchRule,
                            string.Empty, rule.BiomeId ?? string.Empty, id, -1,
                            "A biome patch rule has an invalid canonical ID."));
                        continue;
                    }
                    if (rules.ContainsKey(id))
                    {
                        errors.Add(Error(
                            CorePatchInitializationErrorCode.DuplicatePatchRuleId,
                            string.Empty, rule.BiomeId ?? string.Empty, id, -1,
                            "Biome patch rule IDs must be unique."));
                    }
                    else
                    {
                        rules.Add(id, rule);
                    }
                }
            }

            foreach (var source in sources)
            {
                if (!biomes.TryGetValue(source.BiomeId, out var biome))
                {
                    errors.Add(Error(
                        CorePatchInitializationErrorCode.MissingRequiredBiomeType,
                        source.ReservationId, source.BiomeId, source.PatchRuleId, -1,
                        "A required Core biome type is missing."));
                }
                else if (!biome.Active || !biome.Required || biome.MinCorePatchCount < 1)
                {
                    errors.Add(Error(
                        CorePatchInitializationErrorCode.InvalidBiomeType,
                        source.ReservationId, source.BiomeId, source.PatchRuleId, -1,
                        "A required Core biome type must be active, required, and permit a Core patch."));
                }

                if (!rules.TryGetValue(source.PatchRuleId, out var rule))
                {
                    errors.Add(Error(
                        CorePatchInitializationErrorCode.MissingRequiredPatchRule,
                        source.ReservationId, source.BiomeId, source.PatchRuleId, -1,
                        "A required Core patch rule is missing."));
                }
                else if (!rule.Active ||
                         !string.Equals(rule.PatchRole, "CORE", StringComparison.Ordinal) ||
                         !string.Equals(rule.BiomeId, source.BiomeId, StringComparison.Ordinal) ||
                         rule.MinSectorCount < 1 ||
                         rule.MinSectorCount > rule.MaxSectorCount ||
                         rule.MaxSectorCount > WorldGenConstants.SectorCount)
                {
                    errors.Add(Error(
                        CorePatchInitializationErrorCode.InvalidPatchRule,
                        source.ReservationId, source.BiomeId, source.PatchRuleId, -1,
                        "A required Core patch rule has invalid state, role, biome, or range."));
                }

                var sourceId = new SiteReservationId(source.ReservationId);
                if (seeds.TryGetValue(sourceId, out var seed) &&
                    (biome == null || rule == null ||
                     !string.Equals(seed.BiomeId, biome.BiomeId, StringComparison.Ordinal) ||
                     !string.Equals(seed.CorePatchRuleId, rule.PatchRuleId, StringComparison.Ordinal) ||
                     seed.MinimumCoreSectorCount != rule.MinSectorCount ||
                     seed.BufferRingSectors != rule.BufferRingSectors))
                {
                    errors.Add(Error(
                        CorePatchInitializationErrorCode.DefinitionIdentityMismatch,
                        source.ReservationId, seed.BiomeId, seed.CorePatchRuleId,
                        WorldGridIndex.ToIndex(seed.SeedSector),
                        "Core seed and typed definition identities must match exactly."));
                }
            }
        }

        private static void ValidateGeneratedPatchIds(
            IReadOnlyList<CoreSource> sources,
            ICollection<CorePatchInitializationError> errors)
        {
            var factory = new CorePatchIdFactory();
            var ids = new HashSet<BiomePatchId>();
            foreach (var source in sources)
            {
                var sourceId = new SiteReservationId(source.ReservationId);
                if (!factory.TryCreateCorePatchId(sourceId, out var patchId) || !patchId.IsValid)
                {
                    errors.Add(Error(
                        CorePatchInitializationErrorCode.InvalidGeneratedPatchId,
                        source.ReservationId, source.BiomeId, source.PatchRuleId, -1,
                        "A generated Core patch ID is invalid."));
                }
                else if (!ids.Add(patchId))
                {
                    errors.Add(Error(
                        CorePatchInitializationErrorCode.DuplicateGeneratedPatchId,
                        source.ReservationId, source.BiomeId, source.PatchRuleId, -1,
                        "Generated Core patch IDs must be unique."));
                }
            }
        }

        private static List<int> GetFootprintIndices(SiteReservation reservation)
        {
            var result = new List<int>(reservation.OccupiedSectors.Count);
            foreach (var coordinate in reservation.OccupiedSectors)
                result.Add(WorldGridIndex.ToIndex(coordinate));
            result.Sort();
            return result;
        }

        private static bool Contains(IReadOnlyList<int> values, int value)
        {
            for (var index = 0; index < values.Count; index++)
                if (values[index] == value) return true;
            return false;
        }

        private static CorePatchInitializationError Error(
            CorePatchInitializationErrorCode code,
            string sourceReservationId,
            string biomeId,
            string patchRuleId,
            int sectorIndex,
            string message)
        {
            return new CorePatchInitializationError(
                code,
                sourceReservationId ?? string.Empty,
                biomeId ?? string.Empty,
                patchRuleId ?? string.Empty,
                sectorIndex,
                message);
        }

        private static IReadOnlyList<CoreSource> CreateCoreSources()
        {
            return new[]
            {
                new CoreSource(2, "RSV_02_SITE_MOON_SEAL_FORGE", "SITE_MOON_SEAL_FORGE",
                    SiteReservationKind.Forge, "BIO_ABANDONED_MILL", "PATCH_MILL_CORE", 4, 1),
                new CoreSource(3, "RSV_03_SITE_CASSIA_SAP_HEART", "SITE_CASSIA_SAP_HEART",
                    SiteReservationKind.CoreResource, "BIO_CASSIA_ROOT", "PATCH_ROOT_CORE", 5, 1),
                new CoreSource(4, "RSV_04_SITE_DEEP_STAR_YEAST", "SITE_DEEP_STAR_YEAST",
                    SiteReservationKind.CoreResource, "BIO_MOON_DOUGH", "PATCH_DOUGH_CORE", 5, 1),
                new CoreSource(5, "RSV_05_SITE_MOON_CORE_METEOR", "SITE_MOON_CORE_METEOR",
                    SiteReservationKind.CoreResource, "BIO_MOON_CRATER", "PATCH_CRATER_CORE", 5, 1)
            };
        }

        private static IReadOnlyList<CoreSource> CreateReservationSources()
        {
            return new[]
            {
                new CoreSource(0, "RSV_00_WORLD_MOONPALACE_V1", "WORLD_MOONPALACE_V1",
                    SiteReservationKind.Start, string.Empty, string.Empty, 0, 0),
                new CoreSource(1, "RSV_01_SITE_MOON_BOSS_VAULT", "SITE_MOON_BOSS_VAULT",
                    SiteReservationKind.Boss, string.Empty, string.Empty, 0, 0),
                new CoreSource(2, "RSV_02_SITE_MOON_SEAL_FORGE", "SITE_MOON_SEAL_FORGE",
                    SiteReservationKind.Forge, "BIO_ABANDONED_MILL", "PATCH_MILL_CORE", 4, 1),
                new CoreSource(3, "RSV_03_SITE_CASSIA_SAP_HEART", "SITE_CASSIA_SAP_HEART",
                    SiteReservationKind.CoreResource, "BIO_CASSIA_ROOT", "PATCH_ROOT_CORE", 5, 1),
                new CoreSource(4, "RSV_04_SITE_DEEP_STAR_YEAST", "SITE_DEEP_STAR_YEAST",
                    SiteReservationKind.CoreResource, "BIO_MOON_DOUGH", "PATCH_DOUGH_CORE", 5, 1),
                new CoreSource(5, "RSV_05_SITE_MOON_CORE_METEOR", "SITE_MOON_CORE_METEOR",
                    SiteReservationKind.CoreResource, "BIO_MOON_CRATER", "PATCH_CRATER_CORE", 5, 1),
                new CoreSource(6, "RSV_06_SITE_PRIMARY_VILLAGE", "SITE_PRIMARY_VILLAGE",
                    SiteReservationKind.Village, string.Empty, string.Empty, 0, 0)
            };
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
                int bufferRingSectors)
            {
                Order = order;
                ReservationId = reservationId;
                SourceDefinitionId = sourceDefinitionId;
                Kind = kind;
                BiomeId = biomeId;
                PatchRuleId = patchRuleId;
                MinimumSectorCount = minimumSectorCount;
                BufferRingSectors = bufferRingSectors;
            }

            public int Order { get; }
            public string ReservationId { get; }
            public string SourceDefinitionId { get; }
            public SiteReservationKind Kind { get; }
            public string BiomeId { get; }
            public string PatchRuleId { get; }
            public int MinimumSectorCount { get; }
            public int BufferRingSectors { get; }
        }
    }
}
